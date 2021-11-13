using HslCommunication.Core.IMessage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
#if !NET35 && !NET20
using System.Threading.Tasks;
#endif

namespace HslCommunication.Core.Net
{
	/// <summary>
	/// Udp服务器程序的基础类，提供了启动服务器的基本实现，方便后续的扩展操作。<br />
	/// The basic class of the udp server program provides the basic implementation of starting the server to facilitate subsequent expansion operations.
	/// </summary>
	public class NetworkUdpServerBase : NetworkXBase
	{
		#region Constructor

		/// <summary>
		/// 实例化一个默认的对象<br />
		/// Instantiate a default object
		/// </summary>
		public NetworkUdpServerBase( )
		{
			IsStarted = false;
			Port = 0;
		}

		#endregion

		#region Public Properties

		/// <summary>
		/// 服务器引擎是否启动<br />
		/// Whether the server engine is started
		/// </summary>
		public bool IsStarted { get; protected set; }

		/// <summary>
		/// 获取或设置服务器的端口号，如果是设置，需要在服务器启动前设置完成，才能生效。<br />
		/// Gets or sets the port number of the server. If it is set, it needs to be set before the server starts to take effect.
		/// </summary>
		/// <remarks>需要在服务器启动之前设置为有效</remarks>
		public int Port { get; set; }

		#endregion

		#region Protect Method

		/// <summary>
		/// 后台接收数据的线程
		/// </summary>
		protected virtual void ThreadReceiveCycle( )
		{
			IPEndPoint ipep = new IPEndPoint( IPAddress.Any, 0 );
			EndPoint Remote = (EndPoint)ipep;
			while (IsStarted)
			{
				byte[] data = new byte[1024];
				int length = 0;
				try
				{
					length = CoreSocket.ReceiveFrom( data, ref Remote );
				}
				catch (Exception ex)
				{
					LogNet?.WriteException( "ThreadReceiveCycle", ex );
				}

				Console.WriteLine( DateTime.Now.ToString() + " :ReceiveData" );
			}
		}

		#endregion

		#region Start And Close

		/// <summary>
		/// 当客户端的socket登录的时候额外检查的操作，并返回操作的结果信息。<br />
		/// The operation is additionally checked when the client's socket logs in, and the result information of the operation is returned.
		/// </summary>
		/// <param name="socket">套接字</param>
		/// <param name="endPoint">终结点</param>
		/// <returns>验证的结果</returns>
		protected virtual OperateResult SocketAcceptExtraCheck( Socket socket, IPEndPoint endPoint ) => OperateResult.CreateSuccessResult( );

		/// <summary>
		/// 服务器启动时额外的初始化信息，可以用于启动一些额外的服务的操作。<br />
		/// The extra initialization information when the server starts can be used to start some additional service operations.
		/// </summary>
		/// <remarks>需要在派生类中重写</remarks>
		protected virtual void StartInitialization( ) { }

		/// <summary>
		/// 指定端口号来启动服务器的引擎<br />
		/// Specify the port number to start the server's engine
		/// </summary>
		/// <param name="port">指定一个端口号</param>
		public virtual void ServerStart( int port )
		{
			if (!IsStarted)
			{
				StartInitialization( );

				CoreSocket = new Socket( AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp );
				CoreSocket.Bind( new IPEndPoint( IPAddress.Any, port ) );
				threadReceive = new Thread( new ThreadStart( ThreadReceiveCycle ) ) { IsBackground = true };
				threadReceive.Start( );
				IsStarted = true;
				Port = port;

				LogNet?.WriteNewLine( );
				LogNet?.WriteInfo( ToString(), StringResources.Language.NetEngineStart );
			}
		}

		/// <summary>
		/// 使用已经配置好的端口启动服务器的引擎<br />
		/// Use the configured port to start the server's engine
		/// </summary>
		public void ServerStart( ) => ServerStart( Port );

		/// <summary>
		/// 服务器关闭的时候需要做的事情<br />
		/// Things to do when the server is down
		/// </summary>
		protected virtual void CloseAction( ) { }

		/// <summary>
		/// 关闭服务器的引擎<br />
		/// Shut down the server's engine
		/// </summary>
		public virtual void ServerClose( )
		{
			if (IsStarted)
			{
				CloseAction( );
				CoreSocket?.Close( );
				IsStarted = false;
				LogNet?.WriteInfo( ToString(), StringResources.Language.NetEngineClose );
			}
		}

		#endregion

		#region Private Member

		private Thread threadReceive;

		#endregion

		#region Object Override

		/// <inheritdoc/>
		public override string ToString( ) => $"NetworkUdpServerBase[{Port}]";

		#endregion
	}
}
