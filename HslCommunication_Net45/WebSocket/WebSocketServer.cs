using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HslCommunication.Core.Net;
using System.Net.Sockets;
using System.Net;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.IO;
using HslCommunication.BasicFramework;
using HslCommunication.Core;
#if !NET35 && !NET20
using System.Threading.Tasks;
#endif

namespace HslCommunication.WebSocket
{
	/// <summary>
	/// WebSocket协议的实现，支持创建自定义的websocket服务器，直接给其他的网页端，客户端，手机端发送数据信息，详细看api文档说明<br />
	/// The implementation of the WebSocket protocol supports the creation of custom websocket servers and sends data information directly to other web pages, clients, and mobile phones. See the API documentation for details.
	/// </summary>
	/// <example>
	/// 使用本组件库可以非常简单方便的构造属于你自己的websocket服务器，从而实现和其他的客户端进行通信，尤其是和网页进行通讯，
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\WebSocket\WebSocketServerSample.cs" region="Sample1" title="简单的实例化" />
	/// 当客户端发送数据给服务器的时候，会发一个事件，并且把当前的会话暴露出来，下面举例打印消息，并且演示一个例子，发送数据给指定的会话。
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\WebSocket\WebSocketServerSample.cs" region="Sample2" title="接触数据" />
	/// 也可以在其他地方发送数据给所有的客户端，只要调用一个方法就可以了。
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\WebSocket\WebSocketServerSample.cs" region="Sample3" title="发送数据" />
	/// 当客户端上线之后也触发了当前的事件，我们可以手动捕获到
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\WebSocket\WebSocketServerSample.cs" region="Sample4" title="捕获上线事件" />
	/// 我们再来看看一个高级的操作，实现订阅，大多数的情况，websocket被设计成了订阅发布的操作。基本本服务器可以扩展出非常复杂功能的系统，我们来看一种最简单的操作。
	/// <br />
	/// 客户端给服务器发的数据都视为主题(topic)，这样服务器就可以辨认出主题信息，并追加主题。如下这么操作。
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\WebSocket\WebSocketServerSample.cs" region="Sample5" title="订阅实现" />
	/// 然后在发布的时候，调用下面的代码。
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\WebSocket\WebSocketServerSample.cs" region="Sample6" title="发布数据" />
	/// 可以看到，我们这里只有订阅操作，如果想要实现更为复杂的操作怎么办？丰富客户端发来的数据，携带命令，数据，就可以区分了。比如json数据。具体的实现需要看各位能力了。
	/// </example>
	public class WebSocketServer : NetworkServerBase, IDisposable
	{
		#region Constructor

		/// <summary>
		/// 实例化一个默认的对象<br />
		/// Instantiate a default object
		/// </summary>
		public WebSocketServer( ) 
		{
			retainKeys = new Dictionary<string, string>( );         // 缓存的消息发送中心
			keysLock   = new object( );
		}

		#endregion

		/// <inheritdoc/>
		public override void ServerStart( int port )
		{
			base.ServerStart( port );
			if (KeepAliveSendInterval.TotalMilliseconds > 0)
				timerHeart = new System.Threading.Timer( ThreadTimerHeartCheck, null, 2000, (int)KeepAliveSendInterval.TotalMilliseconds );
		}

		private void ThreadTimerHeartCheck( object obj )
		{
			WebSocketSession[] snapshoot = null;
			lock (sessionsLock)
				snapshoot = wsSessions.ToArray( );
			if(snapshoot != null && snapshoot.Length > 0)
			{
				for (int i = 0; i < snapshoot.Length; i++)
				{
					if ((DateTime.Now - snapshoot[i].ActiveTime) > KeepAlivePeriod)
					{
						// 心跳超时
						RemoveAndCloseSession( snapshoot[i], $"Heart check timeout[{SoftBasic.GetTimeSpanDescription( DateTime.Now - snapshoot[i].ActiveTime )}]" );
					}
					else
					{
						Send( snapshoot[i].WsSocket, WebSocketHelper.WebScoketPackData( 0x09, false, "Heart Check" ) );
					}
				}
			}
		}

		#region ServerBase Override
#if NET35 || NET20
		/// <inheritdoc/>
		protected override void ThreadPoolLogin( Socket socket, IPEndPoint endPoint )
		{
			OperateResult<byte[]> headResult = Receive( socket, -1, 5000 );
			HandleWebsocketConnection( socket, endPoint, headResult );
		}

		private void ReceiveCallback( IAsyncResult ar )
		{
			if (ar.AsyncState is WebSocketSession session)
			{
				try
				{
					session.WsSocket.EndReceive( ar );
				}
				catch (Exception ex)
				{
					session.WsSocket?.Close( );
					LogNet?.WriteDebug( ToString( ), "ReceiveCallback Failed:" + ex.Message );
					RemoveAndCloseSession( session );
					return;
				}

				OperateResult<WebSocketMessage> read = ReceiveWebSocketPayload( session.WsSocket );
				HandleWebsocketMessage( session, read );
			}
		}
#else
		/// <inheritdoc/>
		protected async override void ThreadPoolLogin( Socket socket, IPEndPoint endPoint )
		{
			OperateResult<byte[]> headResult = await ReceiveAsync( socket, -1, 5000 );
			HandleWebsocketConnection( socket, endPoint, headResult );
		}

		private async void ReceiveCallback( IAsyncResult ar )
		{
			if (ar.AsyncState is WebSocketSession session)
			{
				try
				{
					session.WsSocket.EndReceive( ar );
				}
				catch (Exception ex)
				{
					session.WsSocket?.Close( );
					LogNet?.WriteDebug( ToString( ), "ReceiveCallback Failed:" + ex.Message );
					RemoveAndCloseSession( session );
					return;
				}

				OperateResult<WebSocketMessage> read = await ReceiveWebSocketPayloadAsync( session.WsSocket );
				HandleWebsocketMessage( session, read );
			}
		}
#endif
		#endregion

		#region Private Method

		private void HandleWebsocketConnection( Socket socket, IPEndPoint endPoint,  OperateResult<byte[]> headResult )
		{
			if (!headResult.IsSuccess) return;

			string http = Encoding.UTF8.GetString( headResult.Content );
			OperateResult check = WebSocketHelper.CheckWebSocketLegality( http );
			if (!check.IsSuccess) { socket?.Close( ); LogNet?.WriteDebug( ToString( ), $"[{endPoint}] WebScoket Check Failed:" + check.Message + Environment.NewLine + http ); return; }

			OperateResult<byte[]> response = WebSocketHelper.GetResponse( http );
			if (!response.IsSuccess) { socket?.Close( ); LogNet?.WriteDebug( ToString( ), $"[{endPoint}] GetResponse Failed:" + response.Message ); return; }

			OperateResult send = Send( socket, response.Content );
			if (!send.IsSuccess) return;

			WebSocketSession session = new WebSocketSession( )
			{
				ActiveTime = DateTime.Now,
				Remote = endPoint,
				WsSocket = socket,
				IsQASession = http.Contains( "HslRequestAndAnswer: true" ) || http.Contains( "HslRequestAndAnswer:true" )
			};

			Match match = Regex.Match( http, @"GET [\S\s]+ HTTP/1", RegexOptions.IgnoreCase );
			if (match.Success) session.Url = match.Value.Substring( 4, match.Value.Length - 11 );

			try
			{
				string[] sub = WebSocketHelper.GetWebSocketSubscribes( http );
				if (sub != null)
				{
					session.Topics = new List<string>( sub );
					if (isRetain)
						lock (keysLock)
						{
							for (int i = 0; i < session.Topics.Count; i++)
							{
								if (retainKeys.ContainsKey( session.Topics[i] ))
								{
									send = Send( socket, WebSocketHelper.WebScoketPackData( 0x01, false, retainKeys[session.Topics[i]] ) );
									if (!send.IsSuccess) return;
								}
							}
						}
				}
				socket.BeginReceive( new byte[0], 0, 0, SocketFlags.None, new AsyncCallback( ReceiveCallback ), session );
				AddWsSession( session );
			}
			catch (Exception ex)
			{
				socket?.Close( );
				LogNet?.WriteDebug( ToString( ), $"[{session.Remote}] BeginReceive Failed: {ex.Message}" );
				return;
			}

			OnClientConnected?.Invoke( session );
		}

		private void HandleWebsocketMessage( WebSocketSession session, OperateResult<WebSocketMessage> read )
		{
			if (!read.IsSuccess) { RemoveAndCloseSession( session ); return; }
			session.ActiveTime = DateTime.Now;

			if (read.Content.OpCode == 0x08)           // 客户端关闭了连接
			{
				session.WsSocket?.Close( );
				RemoveAndCloseSession( session, Encoding.UTF8.GetString( read.Content.Payload ) );
				return;
			}
			else if (read.Content.OpCode == 0x09)       // 心跳检查，来自客户端的心跳
			{
				LogNet?.WriteDebug( ToString( ), $"[{session.Remote}] PING: {read.Content }" );
				OperateResult send = Send( session.WsSocket, WebSocketHelper.WebScoketPackData( 0x0A, false, read.Content.Payload ) );
				if (!send.IsSuccess)
				{
					RemoveAndCloseSession( session, $"HandleWebsocketMessage -> 09 opCode send back exception -> {send.Message}" );
					return;
				}
			}
			else if (read.Content.OpCode == 0x0A)       // 接收客户端的PONG操作
				LogNet?.WriteDebug( ToString( ), $"[{session.Remote}] PONG: {read.Content }" );
			else
				OnClientApplicationMessageReceive?.Invoke( session, read.Content );

			try
			{
				session.WsSocket.BeginReceive( new byte[0], 0, 0, SocketFlags.None, new AsyncCallback( ReceiveCallback ), session );
			}
			catch (Exception ex)
			{
				session.WsSocket?.Close( );
				RemoveAndCloseSession( session, "BeginReceive Exception -> " + ex.Message );
			}
		}

		#endregion

		#region Event Handler

		/// <summary>
		/// websocket的消息收到委托<br />
		/// websocket message received delegate
		/// </summary>
		/// <param name="session">当前的会话对象</param>
		/// <param name="message">websocket的消息</param>
		public delegate void OnClientApplicationMessageReceiveDelegate( WebSocketSession session, WebSocketMessage message );

		/// <summary>
		/// websocket的消息收到时触发<br />
		/// Triggered when a websocket message is received
		///</summary>
		public event OnClientApplicationMessageReceiveDelegate OnClientApplicationMessageReceive;

		/// <summary>
		/// 当前websocket连接上服务器的事件委托<br />
		/// Event delegation of the server on the current websocket connection
		/// </summary>
		/// <param name="session">当前的会话对象</param>
		public delegate void OnClientConnectedDelegate( WebSocketSession session );

		/// <summary>
		/// Websocket的客户端连接上来时触发<br />
		/// Triggered when a Websocket client connects
		/// </summary>
		public event OnClientConnectedDelegate OnClientConnected;

		/// <summary>
		/// Websocket的客户端下线时触发<br />
		/// Triggered when Websocket client connects
		/// </summary>
		public event OnClientConnectedDelegate OnClientDisConnected;

		#endregion

		#region Clients Mang

		/// <inheritdoc/>
		protected override void StartInitialization( )
		{

		}

		/// <inheritdoc/>
		protected override void CloseAction( )
		{
			base.CloseAction( );
			CleanWsSession( );
		}

		#endregion

		#region Publish Message

		private void PublishSessionList( List<WebSocketSession> sessions, string payload )
		{
			for (int i = 0; i < sessions.Count; i++)
			{
				OperateResult send = Send( sessions[i].WsSocket, WebSocketHelper.WebScoketPackData( 0x01, false, payload ) );
				if (!send.IsSuccess)
					LogNet?.WriteError( ToString( ), $"[{sessions[i].Remote}] Send Failed: {send.Message}" );
			}
		}

		/// <summary>
		/// 向所有的客户端强制发送消息<br />
		/// Force message to all clients
		/// </summary>
		/// <param name="payload">消息内容</param>
		public void PublishAllClientPayload( string payload )
		{
			List<WebSocketSession> sessions = new List<WebSocketSession>( );
			lock (sessionsLock)
			{
				for (int i = 0; i < wsSessions.Count; i++)
				{
					if (wsSessions[i].IsQASession) continue;
					sessions.Add( wsSessions[i] );
				}
			}

			PublishSessionList( sessions, payload );
		}

		/// <summary>
		/// 向订阅了topic主题的客户端发送消息<br />
		/// Send messages to clients subscribed to topic
		/// </summary>
		/// <param name="topic">主题</param>
		/// <param name="payload">消息内容</param>
		public void PublishClientPayload( string topic, string payload )
		{
			List<WebSocketSession> sessions = new List<WebSocketSession>( );
			lock (sessionsLock)
			{
				for (int i = 0; i < wsSessions.Count; i++)
				{
					if (wsSessions[i].IsQASession) continue;
					if (wsSessions[i].IsClientSubscribe( topic ))
					{
						sessions.Add( wsSessions[i] );
					}
				}
			}

			PublishSessionList( sessions, payload );
			if (isRetain) AddTopicRetain( topic, payload );
		}

#if !NET20 && !NET35
		private async Task PublishSessionListAsync( List<WebSocketSession> sessions, string payload )
		{
			for (int i = 0; i < sessions.Count; i++)
			{
				OperateResult send = await SendAsync( sessions[i].WsSocket, WebSocketHelper.WebScoketPackData( 0x01, false, payload ) );
				if (!send.IsSuccess)
					LogNet?.WriteError( ToString( ), $"[{sessions[i].Remote}] Send Failed: {send.Message}" );
			}
		}

		/// <inheritdoc cref="PublishAllClientPayload(string)"/>
		public async Task PublishAllClientPayloadAsync( string payload )
		{
			List<WebSocketSession> sessions = new List<WebSocketSession>( );
			lock (sessionsLock)
			{
				for (int i = 0; i < wsSessions.Count; i++)
				{
					if (wsSessions[i].IsQASession) continue;
					sessions.Add( wsSessions[i] );
				}
			}

			await PublishSessionListAsync( sessions, payload );
		}

		/// <inheritdoc cref="PublishClientPayload(string, string)"/>
		public async Task PublishClientPayloadAsync( string topic, string payload )
		{
			List<WebSocketSession> sessions = new List<WebSocketSession>( );
			lock (sessionsLock)
			{
				for (int i = 0; i < wsSessions.Count; i++)
				{
					if (wsSessions[i].IsQASession) continue;
					if (wsSessions[i].IsClientSubscribe( topic ))
					{
						sessions.Add( wsSessions[i] );
					}
				}
			}

			await PublishSessionListAsync( sessions, payload );
			if (isRetain) AddTopicRetain( topic, payload );
		}
#endif
		/// <summary>
		/// 向指定的客户端发送数据<br />
		/// Send data to the specified client
		/// </summary>
		/// <param name="session">会话内容</param>
		/// <param name="payload">消息内容</param>
		public void SendClientPayload(WebSocketSession session, string payload ) => Send( session.WsSocket, WebSocketHelper.WebScoketPackData( 0x01, false, payload ) );

		/// <summary>
		/// 给一个当前的会话信息动态添加订阅的主题<br />
		/// Dynamically add subscribed topics to a current session message
		/// </summary>
		/// <param name="session">会话内容</param>
		/// <param name="topic">主题信息</param>
		public void AddSessionTopic( WebSocketSession session, string topic )
		{
			session.AddTopic( topic );
			PublishSessionTopic( session, topic );
		}

		#endregion

		#region Public Properties

		/// <summary>
		/// 获取当前的在线的客户端数量<br />
		/// Get the current number of online clients
		/// </summary>
		public int OnlineCount => wsSessions.Count;

		/// <summary>
		/// 获取或设置当前的服务器是否对订阅主题信息缓存，方便订阅客户端立即收到结果，默认开启<br />
		/// Gets or sets whether the current server caches the topic information of the subscription, so that the subscription client can receive the results immediately. It is enabled by default.
		/// </summary>
		public bool IsTopicRetain { get => isRetain; set => isRetain = value; }

		/// <summary>
		/// 获取当前的在线的客户端信息，可以用于额外的分析或是显示。
		/// </summary>
		public WebSocketSession[] OnlineSessions
		{
			get
			{
				WebSocketSession[] snapshoot = null;
				lock (sessionsLock)
					snapshoot = wsSessions.ToArray( );
				return snapshoot;
			}
		}

		/// <summary>
		/// 设置的参数，最小单位为1s，当超过设置的时间间隔必须回复PONG报文，否则服务器认定为掉线。默认120秒<br />
		/// Set the minimum unit of the parameter is 1s. When the set time interval is exceeded, the PONG packet must be returned, otherwise the server considers it to be offline. 120 seconds by default
		/// </summary>
		/// <remarks>
		/// 保持连接（Keep Alive）是一个以秒为单位的时间间隔，它是指客户端返回一个PONG报文到下一次返回PONG报文的时候，
		/// 两者之间允许空闲的最大时间间隔。客户端负责保证控制报文发送的时间间隔不超过保持连接的值。
		/// </remarks>
		public TimeSpan KeepAlivePeriod { get; set; } = TimeSpan.FromSeconds( 120 );

		/// <summary>
		/// 获取或是设置用于保持连接的心跳时间的发送间隔。默认30秒钟，需要在服务启动之前设置<br />
		/// Gets or sets the sending interval of the heartbeat time used to keep the connection. 30 seconds by default, need to be set before the service starts
		/// </summary>
		public TimeSpan KeepAliveSendInterval { get; set; } = TimeSpan.FromSeconds( 30 );

		#endregion

		#region Session Method

		private void CleanWsSession( )
		{
			lock (sessionsLock)
			{
				for (int i = 0; i < wsSessions.Count; i++)
				{
					wsSessions[i].WsSocket?.Close( );
				}
				wsSessions.Clear( );
			}
		}

		private void AddWsSession( WebSocketSession session )
		{
			lock (sessionsLock)
			{
				wsSessions.Add( session );
			}
			LogNet?.WriteDebug( ToString( ), $"Client[{session.Remote}] Online" );
		}

		/// <summary>
		/// 让Websocket客户端正常下线，调用本方法即可自由控制会话客户端强制下线操作。<br />
		/// Let the Websocket client go offline normally. Call this method to freely control the session client to force offline operation.
		/// </summary>
		/// <param name="session">当前的会话信息</param>
		/// <param name="reason">下线的原因，默认为空</param>
		public void RemoveAndCloseSession( WebSocketSession session, string reason = null )
		{
			lock (sessionsLock)
			{
				wsSessions.Remove( session );
			}
			session.WsSocket?.Close( );
			LogNet?.WriteDebug( ToString( ), $"Client[{session.Remote}]  Offline {reason}" );
			OnClientDisConnected?.Invoke( session );
		}

		#endregion

		#region RetainKeys

		private void AddTopicRetain(string topic, string payload )
		{
			lock (keysLock)
			{
				if (retainKeys.ContainsKey( topic ))
					retainKeys[topic] = payload;
				else
					retainKeys.Add( topic, payload );
			}
		}

		private void PublishSessionTopic( WebSocketSession session, string topic )
		{
			bool hasKey = false;
			string payload = string.Empty;
			lock (keysLock)
			{
				if (retainKeys.ContainsKey( topic ))
				{
					hasKey = true;
					payload = retainKeys[topic];
				}
			}
			if (hasKey) Send( session.WsSocket, WebSocketHelper.WebScoketPackData( 0x01, false, payload ) );
		}

		#endregion

		#region Private Member

		private readonly Dictionary<string, string> retainKeys;
		private readonly object keysLock;                                                                // 驻留的消息的词典锁
		private bool isRetain = true;                                                                    // 是否驻留发布的消息，以便让订阅的客户端立即收到最新一次发布的数据

		private readonly List<WebSocketSession> wsSessions = new List<WebSocketSession>( );              // websocket的客户端信息
		private readonly object sessionsLock = new object( );
		private System.Threading.Timer timerHeart;
		private bool disposedValue;

		#endregion

		#region IDispose

		/// <summary>
		/// 释放当前的对象
		/// </summary>
		/// <param name="disposing"></param>
		protected virtual void Dispose( bool disposing )
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					// TODO: 释放托管状态(托管对象)
					OnClientApplicationMessageReceive = null;
					OnClientConnected = null;
					OnClientDisConnected = null;
				}

				// TODO: 释放未托管的资源(未托管的对象)并重写终结器
				// TODO: 将大型字段设置为 null
				disposedValue = true;
			}
		}

		// // TODO: 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
		// ~WebSocketServer()
		// {
		//     // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
		//     Dispose(disposing: false);
		// }

		/// <inheritdoc cref="IDisposable.Dispose"/>
		public void Dispose( )
		{
			// 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
			Dispose( disposing: true );
			GC.SuppressFinalize( this );
		}

		#endregion

		#region Object Override

		/// <inheritdoc/>
		public override string ToString( ) => $"WebSocketServer[{Port}]";

		#endregion
	}
}
