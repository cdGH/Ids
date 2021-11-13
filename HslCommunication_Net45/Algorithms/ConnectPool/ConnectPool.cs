using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HslCommunication.ModBus;

namespace HslCommunication.Algorithms.ConnectPool
{
	/// <summary>
	/// 一个连接池管理器，负责维护多个可用的连接，并且自动清理，扩容，用于快速读写服务器或是PLC时使用。<br />
	/// A connection pool manager is responsible for maintaining multiple available connections, 
	/// and automatically cleans up, expands, and is used to quickly read and write servers or PLCs.
	/// </summary>
	/// <typeparam name="TConnector">管理的连接类，需要支持IConnector接口</typeparam>
	/// <remarks>
	/// 需要先实现 <see cref="IConnector"/> 接口的对象，然后就可以实现真正的连接池了，理论上可以实现任意的连接对象，包括modbus连接对象，各种PLC连接对象，数据库连接对象，redis连接对象，SimplifyNet连接对象等等。下面的示例就是modbus-tcp的实现
	/// <note type="warning">要想真正的支持连接池访问，还需要服务器支持一个端口的多连接操作，三菱PLC的端口就不支持，如果要测试示例代码的连接池对象，需要使用本组件的<see cref="ModbusTcpServer"/>来创建服务器对象</note>
	/// </remarks>
	/// <example>
	/// 下面举例实现一个modbus的连接池对象，先实现接口化的操作
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Algorithms\ConnectPool.cs" region="IConnector Example" title="IConnector示例" />
	/// 然后就可以实现真正的连接池了
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Algorithms\ConnectPool.cs" region="ConnectPoolExample" title="ConnectPool示例" />
	/// </example>
	public class ConnectPool<TConnector> where TConnector : IConnector
	{
		#region Constructor

		/// <summary>
		/// 实例化一个连接池对象，需要指定如果创建新实例的方法<br />
		/// To instantiate a connection pool object, you need to specify how to create a new instance
		/// </summary>
		/// <param name="createConnector">创建连接对象的委托</param>
		public ConnectPool( Func<TConnector> createConnector )
		{
			this.CreateConnector = createConnector;
			listLock = new object( );
			connectors = new List<TConnector>( );

			timerCheck = new System.Threading.Timer( TimerCheckBackground, null, 10000, 30000 );
		}

		#endregion

		#region Public Method

		/// <summary>
		/// 获取一个可用的连接对象，如果已经达到上限，就进行阻塞等待。当使用完连接对象的时候，需要调用<see cref="ReturnConnector(TConnector)"/>方法归还连接对象。<br />
		/// Get an available connection object, if the upper limit has been reached, block waiting. When the connection object is used up, 
		/// you need to call the <see cref="ReturnConnector(TConnector)"/> method to return the connection object.
		/// </summary>
		/// <returns>可用的连接对象</returns>
		public TConnector GetAvailableConnector( )
		{
			while (!canGetConnector)
			{
				System.Threading.Thread.Sleep( 20 );
			}

			TConnector result = default( TConnector );
			lock (listLock)
			{
				for (int i = 0; i < connectors.Count; i++)
				{
					if (!connectors[i].IsConnectUsing)
					{
						connectors[i].IsConnectUsing = true;
						result = connectors[i];
						break;
					}
				}

				if (result == null)
				{
					// 创建新的连接
					result = CreateConnector( );
					result.IsConnectUsing = true;
					result.LastUseTime = DateTime.Now;
					result.Open( );
					connectors.Add( result );
					usedConnector = connectors.Count;
					if (usedConnector > usedConnectorMax) usedConnectorMax = usedConnector;

					if (usedConnector == maxConnector) canGetConnector = false;
				}


				result.LastUseTime = DateTime.Now;
			}
			return result;
		}

		/// <summary>
		/// 使用完之后需要通知连接池的管理器，本方法调用之前需要获取到连接对象信息。<br />
		/// After using it, you need to notify the manager of the connection pool, and you need to get the connection object information before calling this method.
		/// </summary>
		/// <param name="connector">连接对象</param>
		public void ReturnConnector( TConnector connector )
		{
			lock (listLock)
			{
				int index = connectors.IndexOf( connector );
				if (index != -1)
				{
					connectors[index].IsConnectUsing = false;
				}
			}
		}

		/// <summary>
		/// 将目前连接中的所有对象进行关闭，然后移除队列。<br />
		/// Close all objects in the current connection, and then remove the queue.
		/// </summary>
		public void ResetAllConnector( )
		{
			lock (listLock)
			{
				for (int i = connectors.Count - 1; i >= 0; i--)
				{
					// 指定时间未使用了，就要删除掉
					connectors[i].Close( );
					connectors.RemoveAt( i );
				}
			}
		}

		#endregion

		#region Public Properties

		/// <summary>
		/// 获取或设置最大的连接数，当实际的连接数超过最大的连接数的时候，就会进行阻塞，直到有新的连接对象为止。<br />
		/// Get or set the maximum number of connections. When the actual number of connections exceeds the maximum number of connections, 
		/// it will block until there is a new connection object.
		/// </summary>
		public int MaxConnector
		{
			get { return maxConnector; }
			set { maxConnector = value; }
		}

		/// <summary>
		/// 获取或设置当前连接过期的时间，单位秒，默认30秒，也就是说，当前的连接在设置的时间段内未被使用，就进行释放连接，减少内存消耗。<br />
		/// Get or set the expiration time of the current connection, in seconds, the default is 30 seconds, that is, 
		/// if the current connection is not used within the set time period, the connection will be released to reduce memory consumption.
		/// </summary>
		public int ConectionExpireTime
		{
			get { return expireTime; }
			set { expireTime = value; }
		}

		/// <summary>
		/// 当前已经使用的连接数，会根据使用的频繁程度进行动态的变化。<br />
		/// The number of currently used connections will dynamically change according to the frequency of use.
		/// </summary>
		public int UsedConnector => usedConnector;

		/// <summary>
		/// 当前已经使用的连接数的峰值，可以用来衡量当前系统的适用的连接池上限。<br />
		/// The current peak value of the number of connections used can be used to measure the upper limit of the applicable connection pool of the current system.
		/// </summary>
		public int UseConnectorMax => usedConnectorMax;

		#endregion

		#region Clear Timer

		private void TimerCheckBackground( object obj )
		{
			// 清理长久不用的连接对象
			lock (listLock)
			{
				for (int i = connectors.Count - 1; i >= 0; i--)
				{
					if ((DateTime.Now - connectors[i].LastUseTime).TotalSeconds > expireTime && !connectors[i].IsConnectUsing)
					{
						// 指定时间未使用了，就要删除掉
						connectors[i].Close( );
						connectors.RemoveAt( i );
					}
				}

				usedConnector = connectors.Count;
				if (usedConnector < MaxConnector) canGetConnector = true;
			}
		}

		#endregion

		#region Private Member

		private Func<TConnector> CreateConnector = null;                   // 创建新的连接对象的委托
		private int maxConnector = 10;                                     // 最大的连接数
		private int usedConnector = 0;                                     // 已经使用的连接
		private int usedConnectorMax = 0;                                  // 已经使用的连接数量的峰值
		private int expireTime = 30;                                       // 连接的过期时间，单位秒
		private bool canGetConnector = true;                               // 是否可以获取连接
		private System.Threading.Timer timerCheck = null;                  // 对象列表检查的时间间隔
		private object listLock;                                           // 列表操作的锁
		private List<TConnector> connectors = null;                        // 所有连接的列表

		#endregion
	}
}
