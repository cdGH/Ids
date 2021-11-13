using HslCommunication.Core.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace HslCommunication.Enthernet.Redis
{
	/// <summary>
	/// Redis协议的订阅操作，一个对象订阅一个或是多个频道的信息，当发生网络异常的时候，内部会进行自动重连，并恢复之前的订阅信息。<br />
	/// In the subscription operation of the Redis protocol, an object subscribes to the information of one or more channels. 
	/// When a network abnormality occurs, the internal will automatically reconnect and restore the previous subscription information.
	/// </summary>
	public class RedisSubscribe : NetworkXBase
	{
		#region Constructor

		/// <summary>
		/// 实例化一个发布订阅类的客户端，需要指定ip地址，端口。<br />
		/// To instantiate a publish and subscribe client, you need to specify the ip address and port.
		/// </summary>
		/// <param name="ipAddress">服务器的IP地址</param>
		/// <param name="port">服务器的端口号</param>
		public RedisSubscribe( string ipAddress, int port )
		{
			endPoint = new IPEndPoint( IPAddress.Parse( ipAddress ), port );
			keyWords = new List<string>( );
		}

		/// <summary>
		/// 实例化一个发布订阅类的客户端，需要指定ip地址，端口，及订阅关键字。<br />
		/// To instantiate a publish-subscribe client, you need to specify the ip address, port, and subscription keyword.
		/// </summary>
		/// <param name="ipAddress">服务器的IP地址</param>
		/// <param name="port">服务器的端口号</param>
		/// <param name="keys">订阅关键字</param>
		public RedisSubscribe( string ipAddress, int port, string[] keys )
		{
			endPoint = new IPEndPoint( IPAddress.Parse( ipAddress ), port );
			keyWords = new List<string>( keys );
		}

		/// <summary>
		/// 实例化一个发布订阅类的客户端，需要指定ip地址，端口，及订阅关键字。<br />
		/// To instantiate a publish-subscribe client, you need to specify the ip address, port, and subscription keyword.
		/// </summary>
		/// <param name="ipAddress">服务器的IP地址</param>
		/// <param name="port">服务器的端口号</param>
		/// <param name="key">订阅关键字</param>
		public RedisSubscribe( string ipAddress, int port, string key )
		{
			endPoint = new IPEndPoint( IPAddress.Parse( ipAddress ), port );
			keyWords = new List<string>( ) { key };
		}

		#endregion

		#region Private Method

		private OperateResult CreatePush( )
		{
			CoreSocket?.Close( );

			OperateResult<Socket> connect = CreateSocketAndConnect( endPoint, connectTimeOut );
			if (!connect.IsSuccess) return connect;

			// 密码的验证
			if (!string.IsNullOrEmpty( this.Password ))
			{
				OperateResult check = Send( connect.Content, RedisHelper.PackStringCommand( new string[] { "AUTH", this.Password } ) );
				if (!check.IsSuccess) return check;

				OperateResult<byte[]> checkResult = ReceiveRedisCommand( connect.Content );
				if (!checkResult.IsSuccess) return checkResult;

				string msg = Encoding.UTF8.GetString( checkResult.Content );
				if (!msg.StartsWith( "+OK" )) return new OperateResult( msg );
			}

			if (keyWords?.Count > 0)
			{
				OperateResult send = Send( connect.Content, RedisHelper.PackSubscribeCommand( keyWords.ToArray( ) ) );
				if (!send.IsSuccess) return send;
			}
			
			CoreSocket = connect.Content;

			try
			{
				connect.Content.BeginReceive( new byte[0], 0, 0, SocketFlags.None, new AsyncCallback( ReceiveCallBack ), connect.Content );
			}
			catch(Exception ex)
			{
				return new OperateResult( ex.Message );
			}

			return OperateResult.CreateSuccessResult( );
		}


		private void ReceiveCallBack( IAsyncResult ar )
		{
			if(ar.AsyncState is Socket socket)
			{
				try
				{
					int receive = socket.EndReceive( ar );
				}
				catch (ObjectDisposedException)
				{
					// 通常是主动退出
					LogNet?.WriteWarn( "Socket Disposed!" );
					return;
				}
				catch(Exception ex)
				{
					SocketReceiveException( ex );
					return;
				}

				OperateResult<byte[]> read = ReceiveRedisCommand( socket );
				if (!read.IsSuccess)
				{
					SocketReceiveException( null );
					return;
				}

				try
				{
					socket.BeginReceive( new byte[0], 0, 0, SocketFlags.None, new AsyncCallback( ReceiveCallBack ), socket );
				}
				catch(Exception ex)
				{
					SocketReceiveException( ex );
					return;
				}

				OperateResult<string[]> data = RedisHelper.GetStringsFromCommandLine( read.Content );
				if (!data.IsSuccess)
				{
					LogNet?.WriteWarn( data.Message );
					return;
				}

				if (data.Content[0].ToUpper( ) == "SUBSCRIBE")
				{
					return;
				}
				else if (data.Content[0].ToUpper( ) == "MESSAGE")
				{
					OnRedisMessageReceived?.Invoke( data.Content[1], data.Content[2] );
				}
				else
				{
					LogNet?.WriteWarn( data.Content[0] );
				}

			}
		}

		private void SocketReceiveException( Exception ex )
		{
			// 发生异常的时候需要进行重新连接
			while (true)
			{
				if (ex != null) LogNet?.WriteException( "Offline", ex );

				Console.WriteLine( StringResources.Language.ReConnectServerAfterTenSeconds );
				System.Threading.Thread.Sleep( this.reconnectTime );

				if (CreatePush( ).IsSuccess)
				{
					Console.WriteLine( StringResources.Language.ReConnectServerSuccess );
					break;
				}
			}
		}

		private void AddSubTopics( string[] topics )
		{
			lock (listLock)
			{
				for (int i = 0; i < topics.Length; i++)
				{
					if (!keyWords.Contains( topics[i] ))
					{
						keyWords.Add( topics[i] );
					}
				}
			}
		}

		private void RemoveSubTopics( string[] topics )
		{
			lock (listLock)
			{
				for (int i = 0; i < topics.Length; i++)
				{
					if (keyWords.Contains( topics[i] ))
					{
						keyWords.Remove( topics[i] );
					}
				}
			}
		}

		#endregion

		#region Public Properties

		/// <summary>
		/// 如果Redis服务器设置了密码，此处就需要进行设置。必须在 <see cref="ConnectServer"/> 方法调用前设置。<br />
		/// If the Redis server has set a password, it needs to be set here. Must be set before the <see cref="ConnectServer"/> method is called.
		/// </summary>
		public string Password { get; set; }

		/// <summary>
		/// 获取或设置当前连接超时时间，主要对 <see cref="ConnectServer"/> 方法有影响，默认值为 5000，也即是5秒。<br />
		/// Get or set the current connection timeout period, which mainly affects the <see cref="ConnectServer"/> method. The default value is 5000, which is 5 seconds.
		/// </summary>
		public int ConnectTimeOut
		{
			get => connectTimeOut;
			set => connectTimeOut = value;
		}

		#endregion

		#region Subscribe Message

		/// <summary>
		/// 从Redis服务器订阅一个或多个主题信息<br />
		/// Subscribe to one or more topics from the redis server
		/// </summary>
		/// <param name="topic">主题信息</param>
		/// <returns>订阅结果</returns>
		public OperateResult SubscribeMessage( string topic ) => SubscribeMessage( new string[] { topic } );

		/// <inheritdoc cref="SubscribeMessage(string)"/>
		public OperateResult SubscribeMessage( string[] topics )
		{
			if (topics == null) return OperateResult.CreateSuccessResult( );
			if (topics.Length == 0) return OperateResult.CreateSuccessResult( );

			if(CoreSocket == null)
			{
				OperateResult connect = ConnectServer( );
				if (!connect.IsSuccess) return connect;
			}

			OperateResult send = Send( CoreSocket, RedisHelper.PackSubscribeCommand( topics ) );
			if (!send.IsSuccess) return send;

			AddSubTopics( topics );
			return OperateResult.CreateSuccessResult( );
		}

		/// <summary>
		/// 取消订阅多个主题信息，取消之后，当前的订阅数据就不在接收到。<br />
		/// Unsubscribe from multiple topic information. After cancellation, the current subscription data will not be received.
		/// </summary>
		/// <param name="topics">主题信息</param>
		/// <returns>取消订阅结果</returns>
		public OperateResult UnSubscribeMessage( string[] topics )
		{
			if (CoreSocket == null)
			{
				OperateResult connect = ConnectServer( );
				if (!connect.IsSuccess) return connect;
			}

			OperateResult send = Send( CoreSocket, RedisHelper.PackUnSubscribeCommand( topics ) );
			if (!send.IsSuccess) return send;

			RemoveSubTopics( topics );
			return OperateResult.CreateSuccessResult( );
		}

		/// <summary>
		/// 取消已经订阅的主题信息
		/// </summary>
		/// <param name="topic">主题信息</param>
		/// <returns>取消订阅结果</returns>
		public OperateResult UnSubscribeMessage( string topic ) => UnSubscribeMessage( new string[] { topic } );

		#endregion

		#region Connect DisConnect 

		/// <summary>
		/// 连接Redis的服务器，如果已经初始化了订阅的Topic信息，那么就会直接进行订阅操作。
		/// </summary>
		/// <returns>是否创建成功</returns>
		public OperateResult ConnectServer( )
		{
			return CreatePush( );
		}

		/// <summary>
		/// 关闭消息推送的界面
		/// </summary>
		public void ConnectClose( )
		{
			CoreSocket?.Close( );
			lock (listLock)
			{
				keyWords.Clear( );
			}
		}

		#endregion

		#region Event Handle

		/// <summary>
		/// 当接收到Redis订阅的信息的时候触发<br />
		/// Triggered when receiving Redis subscription information
		/// </summary>
		/// <param name="topic">主题信息</param>
		/// <param name="message">数据信息</param>
		public delegate void RedisMessageReceiveDelegate( string topic, string message );

		/// <summary>
		/// 当接收到Redis订阅的信息的时候触发
		/// </summary>
		public event RedisMessageReceiveDelegate OnRedisMessageReceived;

		#endregion

		#region Private Member

		private IPEndPoint endPoint;                           // 服务器的地址及端口信息
		private List<string> keyWords = null;                  // 缓存的订阅关键字
		private object listLock = new object( );               // 缓存的订阅关键字的列表锁
		private int reconnectTime = 10000;                     // 重连服务器的时间
		private int connectTimeOut = 5000;                     // 连接服务器的超时时间

		#endregion

		#region Object Override

		/// <inheritdoc/>
		public override string ToString( ) => $"RedisSubscribe[{endPoint}]";

		#endregion
	}
}
