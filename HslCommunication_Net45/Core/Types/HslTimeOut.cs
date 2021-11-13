using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using HslCommunication.Core;
using System.Runtime.InteropServices;
using HslCommunication.Reflection;
using Newtonsoft.Json;

namespace HslCommunication
{
	/****************************************************************************
	 * 
	 *    应用于一些操作超时请求的判断功能 
	 * 
	 *    When applied to a network connection request timeouts
	 * 
	 ****************************************************************************/

	/// <summary>
	/// 超时操作的类<br />
	/// a class use to indicate the time-out of the connection
	/// </summary>
	/// <remarks>
	/// 本类自动启动一个静态线程来处理
	/// </remarks>
	public class HslTimeOut
	{
		/// <summary>
		/// 实例化一个默认的对象<br />
		/// Instantiate a default object
		/// </summary>
		public HslTimeOut( )
		{
			this.UniqueId     = Interlocked.Increment( ref hslTimeoutId );
			this.StartTime    = DateTime.Now;
			this.IsSuccessful = false;
			this.IsTimeout    = false;
		}

		/// <summary>
		/// 当前超时对象的唯一ID信息，没实例化一个对象，id信息就会自增1<br />
		/// The unique ID information of the current timeout object. If an object is not instantiated, the id information will increase by 1
		/// </summary>
		public long UniqueId { get; private set; }

		/// <summary>
		/// 操作的开始时间<br />
		/// Start time of operation
		/// </summary>
		public DateTime StartTime { get; set; }

		/// <summary>
		/// 操作是否成功，当操作完成的时候，需要设置为<c>True</c>，超时检测自动结束。如果一直为<c>False</c>，超时检测到超时，设置<see cref="IsTimeout"/>为<c>True</c><br />
		/// Whether the operation is successful, when the operation is completed, it needs to be set to <c>True</c>, 
		/// and the timeout detection will automatically end. If it is always <c>False</c>, 
		/// the timeout is detected by the timeout, set <see cref="IsTimeout"/> to <c>True</c>
		/// </summary>
		public bool IsSuccessful { get; set; }

		/// <summary>
		/// 延时的时间，单位毫秒<br />
		/// Delay time, in milliseconds
		/// </summary>
		public int DelayTime { get; set; }

		/// <summary>
		/// 连接超时用的Socket，本超时对象主要针对套接字的连接，接收数据的超时检测，也可以设置为空，用作其他用途的超时检测。<br />
		/// Socket used for connection timeout. This timeout object is mainly for socket connection and timeout detection of received data. 
		/// It can also be set to empty for other purposes.
		/// </summary>
		[JsonIgnore]
		public Socket WorkSocket { get; set; }

		/// <summary>
		/// 是否发生了超时的操作，当调用方因为异常结束的时候，需要对<see cref="IsTimeout"/>进行判断，是否因为发送了超时导致的异常<br />
		/// Whether a timeout operation has occurred, when the caller ends abnormally, 
		/// it needs to judge <see cref="IsTimeout"/>, whether it is an exception caused by a timeout sent
		/// </summary>
		public bool IsTimeout { get; set; }

		/// <summary>
		/// 获取到目前为止所花费的时间<br />
		/// Get the time spent so far
		/// </summary>
		/// <returns>时间信息</returns>
		public TimeSpan GetConsumeTime( ) => DateTime.Now - StartTime;

		/// <inheritdoc/>
		public override string ToString( ) => $"HslTimeOut[{DelayTime}]";

		#region TimeOut Pool

		private static long hslTimeoutId = 0;
		private static List<HslTimeOut> WaitHandleTimeOut = new List<HslTimeOut>( 128 );
		private static object listLock = new object( );
		private static Thread threadCheckTimeOut;
		private static long threadUniqueId = 0;
		private static DateTime threadActiveTime;                                                  // 线程的活跃时间
		private static int activeDisableCount = 0;                                                 // 每不活跃60秒就增加一次计数，超过两次重启线程

		/// <summary>
		/// 新增一个超时检测的对象，当操作完成的时候，需要自行标记<see cref="HslTimeOut"/>对象的<see cref="HslTimeOut.IsSuccessful"/>为<c>True</c><br />
		/// Add a new object for timeout detection. When the operation is completed, 
		/// you need to mark the <see cref="HslTimeOut.IsSuccessful"/> of the <see cref="HslTimeOut"/> object as <c>True</c>
		/// </summary>
		/// <param name="timeOut">超时对象</param>
		public static void HandleTimeOutCheck( HslTimeOut timeOut)
		{
			lock (listLock)
			{
				// 先检测线程是否活跃，若超过60秒不活跃，则尝试重启线程
				if ((DateTime.Now - threadActiveTime).TotalSeconds > 60)
				{
					threadActiveTime = DateTime.Now;
					if (Interlocked.Increment( ref activeDisableCount ) >= 2)
					{
						CreateTimeoutCheckThread( );
					}
				}
				WaitHandleTimeOut.Add( timeOut );
			}
		}

		/// <summary>
		/// 获取当前检查超时对象的个数<br />
		/// Get the number of current check timeout objects
		/// </summary>
		[HslMqttApi( Description = "Get the number of current check timeout objects", HttpMethod = "GET" )]
		public static int TimeOutCheckCount => WaitHandleTimeOut.Count;

		/// <summary>
		/// 获取当前的所有的等待超时检查对象列表，请勿手动更改对象的属性值<br />
		/// Get the current list of all waiting timeout check objects, do not manually change the property value of the object
		/// </summary>
		/// <returns>HslTimeOut数组，请勿手动更改对象的属性值</returns>
		[HslMqttApi( Description = "Get the current list of all waiting timeout check objects, do not manually change the property value of the object", HttpMethod = "GET" )]
		public static HslTimeOut[] GetHslTimeOutsSnapShoot( )
		{
			lock (listLock)
			{
				return WaitHandleTimeOut.ToArray( );
			}
		}

		/// <summary>
		/// 新增一个超时检测的对象，需要指定socket，超时时间，返回<see cref="HslTimeOut"/>对象，用作标记完成信息<br />
		/// Add a new object for timeout detection, you need to specify the socket, the timeout period, 
		/// and return the <see cref="HslTimeOut"/> object for marking completion information
		/// </summary>
		/// <param name="socket">网络套接字</param>
		/// <param name="timeout">超时时间，单位为毫秒<br />Timeout period, in milliseconds</param>
		public static HslTimeOut HandleTimeOutCheck( Socket socket, int timeout )
		{
			HslTimeOut hslTimeOut = new HslTimeOut( )
			{
				DelayTime    = timeout,
				IsSuccessful = false,
				StartTime    = DateTime.Now,
				WorkSocket   = socket,
			};
			if (timeout > 0) HslTimeOut.HandleTimeOutCheck( hslTimeOut );
			return hslTimeOut;
		}

		static HslTimeOut( )
		{
			CreateTimeoutCheckThread( );
		}

		private static void CreateTimeoutCheckThread( )
		{
			threadActiveTime = DateTime.Now;
			threadCheckTimeOut?.Abort( );
			threadCheckTimeOut = new Thread( new ParameterizedThreadStart( CheckTimeOut ) );
			threadCheckTimeOut.IsBackground = true;
			threadCheckTimeOut.Priority = ThreadPriority.AboveNormal;
			threadCheckTimeOut.Start( Interlocked.Increment( ref threadUniqueId ) );
		}

		/// <summary>
		/// 整个HslCommunication的检测超时的核心方法，由一个单独的线程运行，线程的优先级很高，当前其他所有的超时信息都可以放到这里处理<br />
		/// The core method of detecting the timeout of th e entire HslCommunication is run by a separate thread. 
		/// The priority of the thread is very high. All other timeout information can be processed here.
		/// </summary>
		/// <param name="obj">需要传入线程的id信息</param>
		private static void CheckTimeOut( object obj )
		{
			long threadId = (long)obj;
			while (true)
			{
				Thread.Sleep( 100 );
				if (threadId != threadUniqueId) break;                      // 如果线程版本号发生了变化，说明有新的线程已经启动，本次线程返回回收
				threadActiveTime    = DateTime.Now;                         // 如果线程处于激活并执行的状态，就更新线程的时间信息
				activeDisableCount  = 0;
				lock (listLock)
				{
					for (int i = HslTimeOut.WaitHandleTimeOut.Count - 1; i >= 0; i--)
					{
						HslTimeOut timeout = HslTimeOut.WaitHandleTimeOut[i];
						if (timeout.IsSuccessful)
						{
							HslTimeOut.WaitHandleTimeOut.RemoveAt( i );
							continue;
						}

						if ((DateTime.Now - timeout.StartTime).TotalMilliseconds > timeout.DelayTime)
						{
							// 连接超时或是验证超时
							if (!timeout.IsSuccessful)
							{
								timeout.WorkSocket?.Close( );
								timeout.IsTimeout = true;
							}

							HslTimeOut.WaitHandleTimeOut.RemoveAt( i );
							continue;
						}
					}
				}
			}
		}
		#endregion
	}
}
