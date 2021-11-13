using HslCommunication.Core.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using HslCommunication.Core;
using System.Net;



/**********************************************************************************
 * 
 *    发布订阅类的服务器类
 *    
 *    实现从客户端进行数据的订阅操作
 * 
 *********************************************************************************/




namespace HslCommunication.Enthernet
{
	/// <summary>
	/// 发布订阅服务器的类，支持按照关键字进行数据信息的订阅
	/// </summary>
	/// <remarks>
	/// 详细的使用说明，请参照博客<a href="http://www.cnblogs.com/dathlin/p/8992315.html">http://www.cnblogs.com/dathlin/p/8992315.html</a>
	/// </remarks>
	/// <example>
	/// 此处贴上了Demo项目的服务器配置的示例代码
	/// <code lang="cs" source="TestProject\PushNetServer\FormServer.cs" region="NetPushServer" title="NetPushServer示例" />
	/// </example>
	public class NetPushServer : NetworkServerBase
	{
		#region Constructor

		/// <summary>
		/// 实例化一个对象
		/// </summary>
		public NetPushServer()
		{
			dictPushClients        = new Dictionary<string, PushGroupClient>( );
			dictSendHistory        = new Dictionary<string, string>( );
			dicHybirdLock          = new object( );
			dicSendCacheLock       = new object( );
			sendAction             = new Action<AppSession, string>( SendString );

			pushClientsLock        = new object( );
			pushClients            = new List<NetPushClient>( );
		}

		#endregion

		#region Server Override

		/// <inheritdoc/>
		protected override void ThreadPoolLogin( Socket socket, IPEndPoint endPoint )
		{
			// 接收一条信息，指定当前请求的数据订阅信息的关键字
			OperateResult<int, string> receive = ReceiveStringContentFromSocket( socket );
			if (!receive.IsSuccess) return;

			// 判断当前的关键字在服务器是否有消息
			// 确认订阅的信息
			OperateResult check = SendStringAndCheckReceive( socket, 0, "" );
			if (!check.IsSuccess) return;

			// 允许发布订阅信息
			AppSession session = new AppSession
			{
				KeyGroup = receive.Content2,
				WorkSocket = socket
			};

			session.IpEndPoint = endPoint;
			session.IpAddress = endPoint.Address.ToString( );
			try
			{
				socket.BeginReceive( session.BytesHead, 0, session.BytesHead.Length, SocketFlags.None, new AsyncCallback( ReceiveCallback ), session );
			}
			catch (Exception ex)
			{
				LogNet?.WriteException( ToString( ), StringResources.Language.SocketReceiveException, ex );
				return;
			}

			LogNet?.WriteDebug( ToString( ), string.Format( StringResources.Language.ClientOnlineInfo, session.IpEndPoint ) );
			PushGroupClient push = GetPushGroupClient( receive.Content2 );
			if (push != null)
			{
				System.Threading.Interlocked.Increment( ref onlineCount );
				push.AddPushClient( session );

				lock (dicSendCacheLock)
					if (dictSendHistory.ContainsKey( receive.Content2 ))
					{
						if (isPushCacheAfterConnect) SendString( session, dictSendHistory[receive.Content2] );
					}
			}
		}

		/// <inheritdoc/>
		public override void ServerClose( )
		{
			base.ServerClose( );
		}

		#endregion

		#region Public Method

		/// <summary>
		/// 主动推送数据内容
		/// </summary>
		/// <param name="key">关键字</param>
		/// <param name="content">数据内容</param>
		public void PushString( string key, string content )
		{
			lock (dicSendCacheLock)
			{
				if (dictSendHistory.ContainsKey( key ))
				{
					dictSendHistory[key] = content;
				}
				else
				{
					dictSendHistory.Add( key, content );
				}
			}

			AddPushKey( key );
			GetPushGroupClient( key )?.PushString( content, sendAction );
		}

		/// <summary>
		/// 移除关键字信息，通常应用于一些特殊临时用途的关键字
		/// </summary>
		/// <param name="key">关键字</param>
		public void RemoveKey( string key )
		{
			lock (dicHybirdLock)
				if (dictPushClients.ContainsKey( key ))
				{
					int count = dictPushClients[key].RemoveAllClient( );
					for (int i = 0; i < count; i++)
					{
						System.Threading.Interlocked.Decrement( ref onlineCount );
					}
					dictPushClients.Remove( key );
				}
		}

		/// <summary>
		/// 创建一个远程服务器的数据推送操作，以便推送给子客户端
		/// </summary>
		/// <param name="ipAddress">远程的IP地址</param>
		/// <param name="port">远程的端口号</param>
		/// <param name="key">订阅的关键字</param>
		public OperateResult CreatePushRemote( string ipAddress, int port, string key )
		{
			OperateResult result;

			lock (pushClientsLock)
			{
				if (pushClients.Find( m => m.KeyWord == key ) == null)
				{
					NetPushClient pushClient = new NetPushClient( ipAddress, port, key );
					result = pushClient.CreatePush( GetPushFromServer );
					if (result.IsSuccess) pushClients.Add( pushClient );
				}
				else
				{
					result = new OperateResult( StringResources.Language.KeyIsExistAlready );
				}
			}

			return result;
		}

		#endregion

		#region Public Properties

		/// <summary>
		/// 在线客户端的数量
		/// </summary>
		public int OnlineCount => onlineCount;

		/// <summary>
		/// 在客户端上线之后，是否推送缓存的数据，默认设置为true
		/// </summary>
		public bool PushCacheAfterConnect
		{
			get { return isPushCacheAfterConnect; }
			set { isPushCacheAfterConnect = value; }
		}

		#endregion

		#region Private Method

		private void ReceiveCallback( IAsyncResult ar )
		{
			if (ar.AsyncState is AppSession session)
			{
				try
				{
					Socket client = session.WorkSocket;
					int bytesRead = client.EndReceive( ar );

					if (bytesRead <= 4)
					{
						// 正常下线退出
						LogNet?.WriteDebug( ToString( ), string.Format( StringResources.Language.ClientOfflineInfo, session.IpEndPoint ) );
						RemoveGroupOnline( session.KeyGroup, session.ClientUniqueID );
					}
					else
					{
						session.HeartTime = DateTime.Now;
					}
				}
				catch (Exception ex)
				{
					if (ex.Message.Contains( StringResources.Language.SocketRemoteCloseException ))
					{
						// 正常下线
						LogNet?.WriteDebug( ToString( ), string.Format( StringResources.Language.ClientOfflineInfo, session.IpEndPoint ) );
						RemoveGroupOnline( session.KeyGroup, session.ClientUniqueID );
					}
					else
					{
						LogNet?.WriteException( ToString( ), string.Format( StringResources.Language.ClientOfflineInfo, session.IpEndPoint ), ex );
						RemoveGroupOnline( session.KeyGroup, session.ClientUniqueID );
					}
				}
			}
		}

		private void AddPushKey(string key)
		{
			lock (dicHybirdLock)
			{
				if (!dictPushClients.ContainsKey( key ))
				{
					dictPushClients.Add( key, new PushGroupClient( ) );
				}
			}
		}

		private PushGroupClient GetPushGroupClient(string key)
		{
			PushGroupClient result = null;
			lock (dicHybirdLock)
			{
				if (dictPushClients.ContainsKey( key ))
				{
					result = dictPushClients[key];
				}
				else
				{
					result = new PushGroupClient( );
					dictPushClients.Add( key, result );
				}
			}
			return result;
		}

		/// <summary>
		/// 移除客户端的数据信息
		/// </summary>
		/// <param name="key">指定的客户端</param>
		/// <param name="clientID">指定的客户端唯一的id信息</param>
		private void RemoveGroupOnline( string key, string clientID )
		{
			PushGroupClient push = GetPushGroupClient( key );
			if (push != null)
			{
				if (push.RemovePushClient( clientID ))
				{
					// 移除成功
					System.Threading.Interlocked.Decrement( ref onlineCount );
				}
			}
		}

		private void SendString( AppSession appSession, string content )
		{
			if( !Send( appSession.WorkSocket, HslProtocol.CommandBytes( 0, Token, content ) ).IsSuccess)
			{
				RemoveGroupOnline( appSession.KeyGroup, appSession.ClientUniqueID );
			}
		}

		private void GetPushFromServer( NetPushClient pushClient, string data )
		{
			// 推送给其他的客户端，当然也有可能是工作站
			PushString( pushClient.KeyWord, data );
		}

		#endregion

		#region Private Member

		private Dictionary<string, string> dictSendHistory;                  // 词典缓存的数据发送对象
		private Dictionary<string, PushGroupClient> dictPushClients;         // 系统的数据词典
		private readonly object dicHybirdLock;                               // 词典锁
		private readonly object dicSendCacheLock;                            // 缓存数据的锁
		private Action<AppSession, string> sendAction;                       // 发送数据的委托
		private int onlineCount = 0;                                         // 在线客户端的数量，用于监视显示
		private List<NetPushClient> pushClients;                             // 客户端列表
		private object pushClientsLock;                                      // 客户端列表的锁
		private bool isPushCacheAfterConnect = true;                         // 在客户端上线之后，是否推送缓存的数据

		#endregion

		#region Object Override

		/// <inheritdoc/>
		public override string ToString() => $"NetPushServer[{Port}]";

		#endregion
	}

}
