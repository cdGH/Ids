using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HslCommunication.Core.Net;
using System.Threading;
using System.Net.Sockets;
using HslCommunication.Core;
#if !NET35 && !NET20
using System.Threading.Tasks;
#endif

namespace HslCommunication.WebSocket
{
	/// <summary>
	/// websocket协议的客户端实现，支持从服务器订阅，发布数据内容信息，详细参考api文档信息<br />
	/// Client implementation of the websocket protocol. It supports subscribing from the server and publishing data content information.
	/// </summary>
	/// <example>
	/// 本客户端使用起来非常的方便，基本就是实例化，绑定一个数据接收的事件即可，如下所示
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\WebSocket\WebSocketClientSample.cs" region="Sample1" title="简单的实例化" />
	/// 假设我们需要发数据给服务端，那么可以参考如下的方式
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\WebSocket\WebSocketClientSample.cs" region="Sample2" title="发送数据" />
	/// 如果我们需要搭配服务器来做订阅推送的功能的话，写法上会稍微有点区别，按照下面的代码来写。
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\WebSocket\WebSocketClientSample.cs" region="Sample3" title="订阅操作" />
	/// 当网络发生异常的时候，我们需要这么来进行重新连接。
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\WebSocket\WebSocketClientSample.cs" region="Sample4" title="异常重连" />
	/// </example>
	public class WebSocketClient : NetworkXBase, IDisposable
	{
		#region Constructor

		/// <summary>
		/// 使用指定的ip，端口来实例化一个默认的对象<br />
		/// Use the specified ip and port to instantiate a default objects
		/// </summary>
		/// <param name="ipAddress">Ip地址信息</param>
		/// <param name="port">端口号信息</param>
		public WebSocketClient( string ipAddress, int port )
		{
			this.IpAddress = ipAddress;
			this.Port      = port;
		}

		/// <summary>
		/// 使用指定的ip，端口，额外的url信息来实例化一个默认的对象<br />
		/// Use the specified ip, port, and additional url information to instantiate a default object
		/// </summary>
		/// <param name="ipAddress">Ip地址信息</param>
		/// <param name="port">端口号信息</param>
		/// <param name="url">额外的信息，比如 /A/B?C=123456</param>
		public WebSocketClient( string ipAddress, int port, string url )
		{
			this.IpAddress = ipAddress;
			this.Port      = port;
			this.url       = url;
		}

		/// <summary>
		/// 使用指定的url来实例化一个默认的对象，例如 ws://127.0.0.1:1883/A/B?C=123456 或是 ws://www.hslcommunication.cn:1883<br />
		/// Use the specified url to instantiate a default object, such as ws://127.0.0.1:1883/A/B?C=123456 or ws://www.hslcommunication.cn:1883s
		/// </summary>
		/// <param name="url">完整的ws地址</param>
		public WebSocketClient( string url )
		{
			// ws://127.0.0.1:1883/dcc/Svr?token=123456
			if (url.StartsWith( "ws://" ))
			{
				url = url.Substring( 5 );
				this.IpAddress = url.Substring( 0, url.IndexOf( ':' ) );
				url = url.Substring( url.IndexOf( ':' ) + 1 );
				if(url.IndexOf( '/' ) < 0)
				{
					this.Port = int.Parse( url );
				}
				else
				{
					this.Port = int.Parse( url.Substring( 0, url.IndexOf( '/' ) ) );
					this.url = url.Substring( url.IndexOf( '/' ) );
				}
			}
			else
			{
				throw new Exception( "Url Must start with ws://" );
			}
		}

		/// <summary>
		/// Mqtt服务器的ip地址<br />
		/// IP address of Mqtt server
		/// </summary>
		public string IpAddress
		{
			get => ipAddress;
			set
			{
				// 正则表达值校验Ip地址
				ipAddress = HslHelper.GetIpAddressFromInput( value );
			}
		}

		/// <summary>
		/// 端口号。默认1883<br />
		/// The port number. Default 1883
		/// </summary>
		public int Port 
		{
			get => port;
			set => port = value;
		}

		#endregion

		#region Connect DisConnect

		/// <summary>
		/// 连接服务器，实例化客户端之后，至少要调用成功一次，如果返回失败，那些请过一段时间后重新调用本方法连接。<br />
		/// After connecting to the server, the client must be called at least once after instantiating the client.
		/// If the return fails, please call this method to connect again after a period of time.
		/// </summary>
		/// <returns>连接是否成功</returns>
		public OperateResult ConnectServer(  ) => ConnectServer( null );

		/// <summary>
		/// 连接服务器，实例化客户端之后，至少要调用成功一次，如果返回失败，那些请过一段时间后重新调用本方法连接。<br />
		/// After connecting to the server, the client must be called at least once after instantiating the client.
		/// If the return fails, please call this method to connect again after a period of time.
		/// </summary>
		/// <param name="subscribes">订阅的消息</param>
		/// <returns>连接是否成功</returns>
		public OperateResult ConnectServer( string[] subscribes )
		{
			subcribeTopics = subscribes;
			// 开启连接
			CoreSocket?.Close( );
			OperateResult<Socket> connect = CreateSocketAndConnect( this.ipAddress, this.port, connectTimeOut );
			if (!connect.IsSuccess) return connect;

			CoreSocket = connect.Content;

			byte[] command = WebSocketHelper.BuildWsSubRequest( this.ipAddress, this.port, url, subcribeTopics );
			// 发送连接的报文信息
			OperateResult send = Send( CoreSocket, command );
			if (!send.IsSuccess) return send;

			// 接收服务器反馈的信息
			OperateResult<byte[]> rece = Receive( CoreSocket, -1, 10_000 );
			if (!rece.IsSuccess) return rece;

			try
			{
				CoreSocket.BeginReceive( new byte[0], 0, 0, SocketFlags.None, new AsyncCallback( ReceiveAsyncCallback ), CoreSocket );
			}
			catch (Exception ex)
			{
				return new OperateResult( ex.Message );
			}

			// 重置关闭状态
			this.closed = false;
			OnClientConnected?.Invoke( );
			this.timerCheck?.Dispose( );
			this.timerCheck = new Timer( new TimerCallback( TimerCheckServer ), null, 2000, 30_000 );
			return OperateResult.CreateSuccessResult( );
		}

		/// <summary>
		/// 关闭Mqtt服务器的连接。<br />
		/// Close the connection to the Mqtt server.
		/// </summary>
		public void ConnectClose( )
		{
			if (!closed)
			{
				Send( CoreSocket, WebSocketHelper.WebScoketPackData( 0x08, true, "Closed" ) );
				closed = true;
				Thread.Sleep( 20 );
				CoreSocket?.Close( );
			}
		}

		#endregion

		#region Async Connect DisConnect
#if !NET35 && !NET20
		/// <inheritdoc cref="ConnectServer()"/>
		public async Task<OperateResult> ConnectServerAsync( ) => await ConnectServerAsync( null );

		/// <inheritdoc cref="ConnectServer(string[])"/>
		public async Task<OperateResult> ConnectServerAsync( string[] subscribes )
		{
			subcribeTopics = subscribes;
			// 开启连接
			CoreSocket?.Close( );
			OperateResult<Socket> connect = await CreateSocketAndConnectAsync( this.ipAddress, this.port, connectTimeOut );
			if (!connect.IsSuccess) return connect;

			CoreSocket = connect.Content;

			byte[] command = WebSocketHelper.BuildWsSubRequest( this.ipAddress, this.port, url, subcribeTopics );
			// 发送连接的报文信息
			OperateResult send = await SendAsync( CoreSocket, command );
			if (!send.IsSuccess) return send;

			// 接收服务器反馈的信息
			OperateResult<byte[]> rece = await ReceiveAsync( CoreSocket, -1, 10_000 );
			if (!rece.IsSuccess) return rece;

			try
			{
				CoreSocket.BeginReceive( new byte[0], 0, 0, SocketFlags.None, new AsyncCallback( ReceiveAsyncCallback ), CoreSocket );
			}
			catch (Exception ex)
			{
				return new OperateResult( ex.Message );
			}

			// 重置关闭状态
			this.closed = false;
			OnClientConnected?.Invoke( );
			this.timerCheck?.Dispose( );
			this.timerCheck = new Timer( new TimerCallback( TimerCheckServer ), null, 2000, 30_000 );
			return OperateResult.CreateSuccessResult( );
		}

		/// <inheritdoc cref="ConnectClose"/>
		public async Task ConnectCloseAsync( )
		{
			if (!closed)
			{
				await SendAsync( CoreSocket, WebSocketHelper.WebScoketPackData( 0x08, true, "Closed" ) );
				closed = true;
				Thread.Sleep( 20 );
				CoreSocket?.Close( );
			}
		}
#endif
		#endregion

		#region Private Method

		private void OnWebsocketNetworkError( )
		{
			if (closed) return;

			if (Interlocked.CompareExchange( ref isReConnectServer, 1, 0 ) == 0)
			{
				try
				{
					if (OnNetworkError == null)
					{
						// 网络异常，系统准备在10秒后自动重新连接。
						LogNet?.WriteInfo( ToString( ), $"The network is abnormal, and the system is ready to automatically reconnect after 10 seconds." );
						while (true)
						{
							// 每隔10秒重连
							for (int i = 0; i < 10; i++)
							{
								if (closed) { Interlocked.Exchange( ref isReConnectServer, 0 ); return; }
								Thread.Sleep( 1_000 );
								LogNet?.WriteInfo( ToString( ), $"Wait for {10 - i} second to connect to the server ..." );
							}

							if (closed) { Interlocked.Exchange( ref isReConnectServer, 0 ); return; }
							OperateResult connect = ConnectServer( );
							if (connect.IsSuccess)
							{
								// 连接成功后，可以在下方break之前进行订阅，或是数据初始化操作
								LogNet?.WriteInfo( ToString( ), "Successfully connected to the server!" );
								break;
							}
							LogNet?.WriteInfo( ToString( ), "The connection failed. Prepare to reconnect after 10 seconds." );
						}
					}
					else
					{
						OnNetworkError?.Invoke( this, new EventArgs( ) );
					}
					Interlocked.Exchange( ref isReConnectServer, 0 );
				}
				catch
				{
					Interlocked.Exchange( ref isReConnectServer, 0 );
					throw;
				}
			}
		}


#if NET35 || NET20
		private void ReceiveAsyncCallback( IAsyncResult ar )
#else
		private async void ReceiveAsyncCallback( IAsyncResult ar )
#endif
		{
			if (ar.AsyncState is Socket socket)
			{
				try
				{
					socket.EndReceive( ar );
				}
				catch (ObjectDisposedException)
				{
					socket?.Close( );
					LogNet?.WriteDebug( ToString( ), "Closed" );
					return;
				}
				catch (Exception ex)
				{
					socket?.Close( );
					LogNet?.WriteDebug( ToString( ), "ReceiveCallback Failed:" + ex.Message );
					OnWebsocketNetworkError( );
					return;
				}

				if (closed)
				{
					LogNet?.WriteDebug( ToString( ), "Closed" );
					return;
				}
#if NET35 || NET20
				OperateResult<WebSocketMessage> read = ReceiveWebSocketPayload( socket );
#else
				OperateResult<WebSocketMessage> read = await ReceiveWebSocketPayloadAsync( socket );
#endif
				if (!read.IsSuccess)
				{
					OnWebsocketNetworkError( );
					return;
				}

				if (read.Content.OpCode == 0x09)
				{
					Send( socket, WebSocketHelper.WebScoketPackData( 0x0A, true, read.Content.Payload ) );
					LogNet?.WriteDebug( ToString( ), read.Content.ToString( ) );
				}
				else if (read.Content.OpCode == 0x0A)
				{
					LogNet?.WriteDebug( ToString( ), read.Content.ToString( ) );
				}
				else
				{
					OnClientApplicationMessageReceive?.Invoke( read.Content );
				}

				try
				{
					socket.BeginReceive( new byte[0], 0, 0, SocketFlags.None, new AsyncCallback( ReceiveAsyncCallback ), socket );
				}
				catch (Exception ex)
				{
					socket?.Close( );
					LogNet?.WriteDebug( ToString( ), "BeginReceive Failed:" + ex.Message );
					OnWebsocketNetworkError( );
				}
			}
		}


		private void TimerCheckServer( object obj )
		{
			if (CoreSocket != null)
			{
				//if ((DateTime.Now - activeTime).TotalSeconds > this.connectionOptions.KeepAliveSendInterval.TotalSeconds * 3)
				//{
				//	// 3个心跳周期没有接收到数据
				//	ThreadPool.QueueUserWorkItem( new WaitCallback( BeginReconnectServer ), null );
				//}
				//else
				//{
				//	if (!Send( CoreSocket, BuildMqttCommand( MqttControlMessage.PINGREQ, 0x00, new byte[0], new byte[0] ).Content ).IsSuccess)
				//		ThreadPool.QueueUserWorkItem( new WaitCallback( BeginReconnectServer ), null );
				//}
			}
		}
		#endregion

		#region Public Method

		/// <summary>
		/// 发送数据到WebSocket的服务器<br />
		/// Send data to WebSocket server
		/// </summary>
		/// <param name="message">消息</param>
		/// <returns>是否发送成功</returns>
		public OperateResult SendServer(string message )
		{
			return Send( CoreSocket, WebSocketHelper.WebScoketPackData( 0x01, true, message ) );
		}

		/// <summary>
		/// 发送数据到WebSocket的服务器，可以指定是否进行掩码操作<br />
		/// Send data to the WebSocket server, you can specify whether to perform a mask operation
		/// </summary>
		/// <param name="mask">是否进行掩码操作</param>
		/// <param name="message">消息</param>
		/// <returns>是否发送成功</returns>
		public OperateResult SendServer( bool mask, string message )
		{
			return Send( CoreSocket, WebSocketHelper.WebScoketPackData( 0x01, mask, message ) );
		}

		/// <summary>
		/// 发送自定义的命令到WebSocket服务器，可以指定操作码，是否掩码操作，原始字节数据<br />
		/// Send custom commands to the WebSocket server, you can specify the operation code, whether to mask operation, raw byte data
		/// </summary>
		/// <param name="opCode">操作码</param>
		/// <param name="mask">是否进行掩码操作</param>
		/// <param name="payload">原始字节数据</param>
		/// <returns>是否发送成功</returns>
		public OperateResult SendServer( int opCode, bool mask, byte[] payload )
		{
			return Send( CoreSocket, WebSocketHelper.WebScoketPackData( opCode, mask, payload ) );
		}

		#endregion

		#region Event Handler

		/// <summary>
		/// websocket的消息收到委托<br />
		/// websocket message received delegate
		/// </summary>
		/// <param name="message">websocket的消息</param>
		public delegate void OnClientApplicationMessageReceiveDelegate( WebSocketMessage message );

		/// <summary>
		/// websocket的消息收到时触发<br />
		/// Triggered when a websocket message is received
		///</summary>
		public event OnClientApplicationMessageReceiveDelegate OnClientApplicationMessageReceive;

		/// <summary>
		/// 连接服务器成功的委托<br />
		/// Connection server successfully delegated
		/// </summary>
		public delegate void OnClientConnectedDelegate( );

		/// <summary>
		/// 当客户端连接成功触发事件，就算是重新连接服务器后，也是会触发的<br />
		/// The event is triggered when the client is connected successfully, even after reconnecting to the server.
		/// </summary>
		public event OnClientConnectedDelegate OnClientConnected;

		/// <summary>
		/// 当网络发生异常的时候触发的事件，用户应该在事件里进行重连服务器
		/// </summary>
		public event EventHandler OnNetworkError;

		#endregion

		#region Public Properties

		/// <summary>
		/// 获取或设置当前客户端的连接超时时间，默认10,000毫秒，单位ms<br />
		/// Gets or sets the connection timeout of the current client. The default is 10,000 milliseconds. The unit is ms.
		/// </summary>
		public int ConnectTimeOut { get => connectTimeOut; set => connectTimeOut = value; }

		/// <summary>
		/// 获取当前的客户端状态是否关闭了连接，当自己手动处理网络异常事件的时候，在重连之前就需要判断是否关闭了连接。<br />
		/// Obtain whether the current client status has closed the connection. When manually handling network abnormal events, you need to determine whether the connection is closed before reconnecting.
		/// </summary>
		public bool IsClosed => closed;

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
					this.OnClientApplicationMessageReceive = null;
					this.OnClientConnected = null;
					this.OnNetworkError = null;
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

		#region Private Member

		private int isReConnectServer = 0;                                    // 是否重连服务器中
		private string[] subcribeTopics;                                      // 缓存的等待处理的订阅的消息内容
		private bool closed = false;                                          // 客户端是否关闭
		private string ipAddress = string.Empty;                              // Ip地址
		private int port = 1883;                                              // 端口号
		private int connectTimeOut = 10000;                                   // 连接的超时时间
		private Timer timerCheck;                                             // 定时器，用来心跳校验的
		private string url = string.Empty;                                    // url信息
		private bool disposedValue;                                           // 当前的对象是否已经释放

		#endregion

		#region Object Override

		/// <inheritdoc/>
		public override string ToString( ) => $"WebSocketClient[{ipAddress}:{port}]";

		#endregion
	}
}
