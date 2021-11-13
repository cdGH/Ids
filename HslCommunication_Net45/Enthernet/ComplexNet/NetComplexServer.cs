using HslCommunication.Core;
using HslCommunication.Core.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace HslCommunication.Enthernet
{
	/// <summary>
	/// 高性能的异步网络服务器类，适合搭建局域网聊天程序，消息推送程序
	/// </summary>
	/// <remarks>
	/// 详细的使用说明，请参照博客<a href="http://www.cnblogs.com/dathlin/p/8097897.html">http://www.cnblogs.com/dathlin/p/8097897.html</a>
	/// </remarks>
	/// <example>
	/// 此处贴上了Demo项目的服务器配置的示例代码
	/// <code lang="cs" source="TestProject\ComplexNetServer\FormServer.cs" region="NetComplexServer" title="NetComplexServer示例" />
	/// </example>
	public class NetComplexServer : NetworkServerBase
	{
		#region Constructor

		/// <summary>
		/// 实例化一个网络服务器类对象
		/// </summary>
		public NetComplexServer( )
		{
			appSessions = new List<AppSession>( );
			lockSessions = new object( );
		}

		#endregion

		#region Public Properties

		/// <summary>
		/// 所支持的同时在线客户端的最大数量，默认为10000个
		/// </summary>
		public int ConnectMax { get => connectMaxClient; set => connectMaxClient = value; }

		/// <summary>
		/// 获取或设置服务器是否记录客户端上下线信息，默认为true
		/// </summary>
		public bool IsSaveLogClientLineChange { get; set; } = true;

		/// <summary>
		/// 所有在线客户端的数量
		/// </summary>
		public int ClientCount => appSessions.Count;

		#endregion

		#region NetworkServerBase Override

		/// <summary>
		/// 初始化操作
		/// </summary>
		protected override void StartInitialization( )
		{
			Thread_heart_check = new Thread( new ThreadStart( ThreadHeartCheck ) )
			{
				IsBackground = true,
				Priority = ThreadPriority.AboveNormal
			};
			Thread_heart_check.Start( );
			base.StartInitialization( );
		}

		/// <summary>
		/// 关闭网络时的操作
		/// </summary>
		protected override void CloseAction( )
		{
			Thread_heart_check?.Abort( );
			ClientOffline = null;
			ClientOnline = null;
			AcceptString = null;
			AcceptByte = null;

			//关闭所有的网络
			lock (lockSessions)
				appSessions.ForEach( m => m.WorkSocket?.Close( ) );
			base.CloseAction( );
		}

		#endregion

		#region Client Online Offline

		private void TcpStateUpLine( AppSession session )
		{
			lock (lockSessions)
				appSessions.Add( session );

			// 提示上线
			ClientOnline?.Invoke( session );
			AllClientsStatusChange?.Invoke( ClientCount );
			// 是否保存上线信息
			if (IsSaveLogClientLineChange) LogNet?.WriteDebug( ToString( ), $"[{session.IpEndPoint}] Name:{ session?.LoginAlias } { StringResources.Language.NetClientOnline }" );
		}

		private void TcpStateDownLine( AppSession session, bool regular, bool logSave = true )
		{
			lock (lockSessions)
			{
				bool success = appSessions.Remove( session );
				if (!success) return;
			}
			// 关闭连接
			session.WorkSocket?.Close( );
			// 判断是否正常下线
			string str = regular ? StringResources.Language.NetClientOffline : StringResources.Language.NetClientBreak;
			ClientOffline?.Invoke( session, str );
			AllClientsStatusChange?.Invoke( ClientCount );
			// 是否保存上线信息
			if (IsSaveLogClientLineChange && logSave) LogNet?.WriteInfo( ToString( ), $"[{session.IpEndPoint}] Name:{ session?.LoginAlias } { str }" );
		}

		/// <summary>
		/// 让客户端正常下线，调用本方法即可自由控制会话客户端强制下线操作。
		/// </summary>
		/// <param name="session">会话对象</param>
		public void AppSessionRemoteClose( AppSession session ) => TcpStateDownLine( session, true, true );

		#endregion

		#region Event Handle

		/// <summary>
		/// 客户端的上下限状态变更时触发，仅作为在线客户端识别
		/// </summary>
		public event Action<int> AllClientsStatusChange;

		/// <summary>
		/// 当客户端上线的时候，触发此事件
		/// </summary>
		public event Action<AppSession> ClientOnline;

		/// <summary>
		/// 当客户端下线的时候，触发此事件
		/// </summary>
		public event Action<AppSession, string> ClientOffline;

		/// <summary>
		/// 当接收到文本数据的时候,触发此事件
		/// </summary>
		public event Action<AppSession, NetHandle, string> AcceptString;

		/// <summary>
		/// 当接收到字节数据的时候,触发此事件
		/// </summary>
		public event Action<AppSession, NetHandle, byte[]> AcceptByte;

		#endregion

		#region Login Server

		/// <summary>
		/// 当接收到了新的请求的时候执行的操作
		/// </summary>
		/// <param name="socket">异步对象</param>
		/// <param name="endPoint">终结点</param>
		protected override void ThreadPoolLogin( Socket socket, IPEndPoint endPoint )
		{
			// 判断连接数是否超出规定
			if (appSessions.Count > ConnectMax)
			{
				socket?.Close( );
				LogNet?.WriteWarn( ToString( ), StringResources.Language.NetClientFull );
				return;
			}

			// 接收用户别名并验证令牌
			OperateResult<int, string> readResult = ReceiveStringContentFromSocket( socket );
			if (!readResult.IsSuccess) return;

			// 登录成功
			AppSession session = new AppSession( )
			{
				WorkSocket = socket,
				LoginAlias = readResult.Content2,
			};

			session.IpEndPoint = endPoint;
			session.IpAddress = endPoint == null ? string.Empty : endPoint.Address.ToString( );

			try
			{
				session.WorkSocket.BeginReceive( new byte[0], 0, 0, SocketFlags.None, new AsyncCallback( ReceiveCallback ), session );
				TcpStateUpLine( session );
				Thread.Sleep( 20 );// 留下一些时间进行反应
			}
			catch (Exception ex)
			{
				// 登录前已经出错
				session.WorkSocket?.Close( );
				LogNet?.WriteException( ToString( ), StringResources.Language.NetClientLoginFailed, ex );
			}
		}
#if NET35 || NET20
		private void ReceiveCallback( IAsyncResult ar )
#else
		private async void ReceiveCallback( IAsyncResult ar )
#endif
		{
			if (ar.AsyncState is AppSession appSession)
			{
				try
				{
					appSession.WorkSocket.EndReceive( ar );
				}
				catch
				{
					TcpStateDownLine( appSession, false );
					return;
				}
#if NET35 || NET20
				OperateResult<int, int, byte[]> read = ReceiveHslMessage( appSession.WorkSocket );
#else
				OperateResult<int, int, byte[]> read = await ReceiveHslMessageAsync( appSession.WorkSocket );
#endif
				if (!read.IsSuccess)
				{
					TcpStateDownLine( appSession, false );
					return;
				}

				try
				{
					appSession.WorkSocket.BeginReceive( new byte[0], 0, 0, SocketFlags.None, new AsyncCallback( ReceiveCallback ), appSession );
				}
				catch
				{
					TcpStateDownLine( appSession, false );
					return;
				}

				int protocol = read.Content1;
				int customer = read.Content2;
				byte[] content = read.Content3;

				if (protocol == HslProtocol.ProtocolCheckSecends)
				{
					// 心跳检查
					BitConverter.GetBytes( DateTime.Now.Ticks ).CopyTo( content, 8 );
					LogNet?.WriteDebug( ToString( ), string.Format( "Heart Check From {0}", appSession.IpEndPoint ) );
					if (Send( appSession.WorkSocket, HslProtocol.CommandBytes( HslProtocol.ProtocolCheckSecends, customer, Token, content ) ).IsSuccess)
						appSession.HeartTime = DateTime.Now;
				}
				else if (protocol == HslProtocol.ProtocolClientQuit)
				{
					// 下线的消息
					TcpStateDownLine( appSession, true );
					return;
				}
				else if (protocol == HslProtocol.ProtocolUserBytes)
				{
					// 接收到字节数据
					AcceptByte?.Invoke( appSession, customer, content );
				}
				else if (protocol == HslProtocol.ProtocolUserString)
				{
					// 接收到文本数据
					string str = Encoding.Unicode.GetString( content );
					AcceptString?.Invoke( appSession, customer, str );
				}
				else
				{
					// 其他一概不处理
				}
			}
		}

		#endregion

		#region Send Support

		/// <summary>
		/// 服务器端用于数据发送文本的方法
		/// </summary>
		/// <param name="session">数据发送对象</param>
		/// <param name="customer">用户自定义的数据对象，如不需要，赋值为0</param>
		/// <param name="str">发送的文本</param>
		public void Send( AppSession session, NetHandle customer, string str ) => Send( session.WorkSocket, HslProtocol.CommandBytes( customer, Token, str ) );

		/// <summary>
		/// 服务器端用于发送字节的方法
		/// </summary>
		/// <param name="session">数据发送对象</param>
		/// <param name="customer">用户自定义的数据对象，如不需要，赋值为0</param>
		/// <param name="bytes">实际发送的数据</param>
		public void Send( AppSession session, NetHandle customer, byte[] bytes ) => Send( session.WorkSocket, HslProtocol.CommandBytes( customer, Token, bytes ) );

		/// <summary>
		/// 服务端用于发送所有数据到所有的客户端
		/// </summary>
		/// <param name="customer">用户自定义的命令头</param>
		/// <param name="str">需要传送的实际的数据</param>
		public void SendAllClients( NetHandle customer, string str )
		{
			lock (lockSessions)
			{
				for (int i = 0; i < appSessions.Count; i++)
					Send( appSessions[i], customer, str );
			}
		}

		/// <summary>
		/// 服务端用于发送所有数据到所有的客户端
		/// </summary>
		/// <param name="customer">用户自定义的命令头</param>
		/// <param name="data">需要群发客户端的字节数据</param>
		public void SendAllClients( NetHandle customer, byte[] data )
		{
			lock (lockSessions)
			{
				for (int i = 0; i < appSessions.Count; i++)
				{
					Send( appSessions[i], customer, data );
				}
			}
		}

		/// <summary>
		/// 根据客户端设置的别名进行发送消息
		/// </summary>
		/// <param name="Alias">客户端上线的别名</param>
		/// <param name="customer">用户自定义的命令头</param>
		/// <param name="str">需要传送的实际的数据</param>
		public void SendClientByAlias( string Alias, NetHandle customer, string str )
		{
			lock (lockSessions)
			{
				for (int i = 0; i < appSessions.Count; i++)
				{
					if (appSessions[i].LoginAlias == Alias)
					{
						Send( appSessions[i], customer, str );
					}
				}
			}
		}

		/// <summary>
		/// 根据客户端设置的别名进行发送消息
		/// </summary>
		/// <param name="Alias">客户端上线的别名</param>
		/// <param name="customer">用户自定义的命令头</param>
		/// <param name="data">需要传送的实际的数据</param>
		public void SendClientByAlias( string Alias, NetHandle customer, byte[] data )
		{
			lock (lockSessions)
			{
				for (int i = 0; i < appSessions.Count; i++)
				{
					if (appSessions[i].LoginAlias == Alias)
					{
						Send( appSessions[i], customer, data );
					}
				}
			}
		}

		#endregion

		#region Heart Check

		private Thread Thread_heart_check { get; set; } = null;

		private void ThreadHeartCheck()
		{
			while (true)
			{
				Thread.Sleep( 2000 );

				try
				{
					AppSession[] sessions = null;
					lock (lockSessions)
						sessions = appSessions.ToArray( );       // 获得客户端的快照

					for (int i = sessions.Length - 1; i >= 0; i--)
					{
						if (sessions[i] == null) continue;

						if ((DateTime.Now - sessions[i].HeartTime).TotalSeconds > 30d) // 30秒没有收到失去联系
						{
							LogNet?.WriteWarn( ToString( ), StringResources.Language.NetHeartCheckTimeout + sessions[i].IpAddress.ToString( ) );
							TcpStateDownLine( sessions[i], false, false );
							continue;
						}
					}
				}
				catch (Exception ex)
				{
					LogNet?.WriteException( ToString( ), StringResources.Language.NetHeartCheckFailed, ex );
				}


				if (!IsStarted) break;
			}
		}

		#endregion

		#region Private Member

		private int connectMaxClient = 10000;                                  // 允许同时登录的最大客户端数量
		private readonly List<AppSession> appSessions = null;                  // 所有客户端连接的对象信息   
		private readonly object lockSessions = null;                           // 对象列表操作的锁

		#endregion

		#region Object Override

		/// <inheritdoc/>
		public override string ToString( ) => $"NetComplexServer[{Port}]";

		#endregion
	}
}
