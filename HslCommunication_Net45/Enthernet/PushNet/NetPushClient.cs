using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HslCommunication.Core.Net;
using System.Net;
using System.Net.Sockets;
using System.Threading;
#if !NET35 && !NET20
using System.Threading.Tasks;
#endif

namespace HslCommunication.Enthernet
{

	/// <summary>
	/// 发布订阅类的客户端，使用指定的关键订阅相关的数据推送信息
	/// </summary>
	/// <remarks>
	/// 详细的使用说明，请参照博客<a href="http://www.cnblogs.com/dathlin/p/8992315.html">http://www.cnblogs.com/dathlin/p/8992315.html</a>
	/// </remarks>
	/// <example>
	/// 此处贴上了Demo项目的服务器配置的示例代码
	/// <code lang="cs" source="TestProject\HslCommunicationDemo\Hsl\FormPushNet.cs" region="FormPushNet" title="NetPushClient示例" />
	/// </example>
	public class NetPushClient : NetworkXBase
	{
		#region Constructor

		/// <summary>
		/// 实例化一个发布订阅类的客户端，需要指定ip地址，端口，及订阅关键字
		/// </summary>
		/// <param name="ipAddress">服务器的IP地址</param>
		/// <param name="port">服务器的端口号</param>
		/// <param name="key">订阅关键字</param>
		public NetPushClient( string ipAddress, int port, string key )
		{
			endPoint = new IPEndPoint( IPAddress.Parse( ipAddress ), port );
			keyWord = key;

			if (string.IsNullOrEmpty( key ))
				throw new Exception( StringResources.Language.KeyIsNotAllowedNull );
		}

		#endregion

		#region Public Method 

		/// <summary>
		/// 创建数据推送服务
		/// </summary>
		/// <param name="pushCallBack">触发数据推送的委托</param>
		/// <returns>是否创建成功</returns>
		public OperateResult CreatePush( Action<NetPushClient, string> pushCallBack )
		{
			action = pushCallBack;
			return CreatePush( );
		}

		/// <summary>
		/// 创建数据推送服务，使用事件绑定的机制实现
		/// </summary>
		/// <returns>是否创建成功</returns>
		public OperateResult CreatePush( )
		{
			CoreSocket?.Close( );

			// 连接服务器
			OperateResult<Socket> connect = CreateSocketAndConnect( endPoint, 5000 );
			if (!connect.IsSuccess) return connect;

			// 发送订阅的关键字
			OperateResult send = SendStringAndCheckReceive( connect.Content, 0, keyWord );
			if (!send.IsSuccess) return send;

			// 确认服务器的反馈
			OperateResult<int, string> receive = ReceiveStringContentFromSocket( connect.Content );
			if (!receive.IsSuccess) return receive;

			// 订阅不存在
			if (receive.Content1 != 0)
			{
				connect.Content?.Close( );
				return new OperateResult( receive.Content2 );
			}

			// 异步接收
			AppSession appSession = new AppSession( );
			CoreSocket = connect.Content;
			appSession.WorkSocket = connect.Content;
			try
			{
				appSession.WorkSocket.BeginReceive( new byte[0], 0, 0, SocketFlags.None, new AsyncCallback( ReceiveCallback ), appSession );
			}
			catch (Exception ex)
			{
				LogNet?.WriteException( ToString( ), StringResources.Language.SocketReceiveException, ex );
				return new OperateResult( ex.Message );
			}

			closed = false;
			return OperateResult.CreateSuccessResult( );
		}
		
		/// <summary>
		/// 关闭消息推送的界面
		/// </summary>
		public void ClosePush()
		{
			action = null;
			closed = true;
			if (CoreSocket != null && CoreSocket.Connected) CoreSocket?.Send( BitConverter.GetBytes( 100 ) );
			System.Threading.Thread.Sleep( 20 );
			CoreSocket?.Close( );
		}

		#endregion

		#region Receive Background

		private void ReconnectServer(object obj )
		{
			// 发生异常的时候需要进行重新连接
			while (true)
			{
				if (closed) return;
				Console.WriteLine( StringResources.Language.ReConnectServerAfterTenSeconds );
				Thread.Sleep( this.reconnectTime );

				if (closed) return;
				if (CreatePush( ).IsSuccess)
				{
					Console.WriteLine( StringResources.Language.ReConnectServerSuccess );
					break;
				}
			}
		}
#if NET35 || NET20
		private void ReceiveCallback( IAsyncResult ar )
		{
			if (ar.AsyncState is AppSession appSession)
			{
				try
				{
					appSession.WorkSocket.EndReceive( ar );
				}
				catch
				{
					ThreadPool.QueueUserWorkItem( new WaitCallback( ReconnectServer ), null );
					return;
				}

				OperateResult<int, int, byte[]> read = ReceiveHslMessage( appSession.WorkSocket );
				if (!read.IsSuccess)
				{
					ThreadPool.QueueUserWorkItem( new WaitCallback( ReconnectServer ), null );
					return;
				}

				int protocol = read.Content1;
				int customer = read.Content2;
				byte[] content = read.Content3;

				if (protocol == HslProtocol.ProtocolUserString)
				{
					action?.Invoke( this, Encoding.Unicode.GetString( content ) );
					OnReceived?.Invoke( this, Encoding.Unicode.GetString( content ) );
				}
				else if (protocol == HslProtocol.ProtocolCheckSecends)
				{
					Send( appSession.WorkSocket, HslProtocol.CommandBytes( HslProtocol.ProtocolCheckSecends, 0, Token, new byte[0] ) );
				}

				try
				{
					appSession.WorkSocket.BeginReceive( new byte[0], 0, 0, SocketFlags.None, new AsyncCallback( ReceiveCallback ), appSession );
				}
				catch
				{
					ThreadPool.QueueUserWorkItem( new WaitCallback( ReconnectServer ), null );
				}
			}
		}
#else
		private async void ReceiveCallback( IAsyncResult ar )
		{
			if (ar.AsyncState is AppSession appSession)
			{
				try
				{
					appSession.WorkSocket.EndReceive( ar );
				}
				catch
				{
					ThreadPool.QueueUserWorkItem( new WaitCallback( ReconnectServer ), null );
					return;
				}

				OperateResult<int, int, byte[]> read = await ReceiveHslMessageAsync( appSession.WorkSocket );
				if (!read.IsSuccess)
				{
					ThreadPool.QueueUserWorkItem( new WaitCallback( ReconnectServer ), null );
					return;
				}

				int protocol = read.Content1;
				int customer = read.Content2;
				byte[] content = read.Content3;

				if (protocol == HslProtocol.ProtocolUserString)
				{
					action?.Invoke( this, Encoding.Unicode.GetString( content ) );
					OnReceived?.Invoke( this, Encoding.Unicode.GetString( content ) );
				}
				else if (protocol == HslProtocol.ProtocolCheckSecends)
				{
					Send( appSession.WorkSocket, HslProtocol.CommandBytes( HslProtocol.ProtocolCheckSecends, 0, Token, new byte[0] ) );
				}

				try
				{
					appSession.WorkSocket.BeginReceive( new byte[0], 0, 0, SocketFlags.None, new AsyncCallback( ReceiveCallback ), appSession );
				}
				catch
				{
					ThreadPool.QueueUserWorkItem( new WaitCallback( ReconnectServer ), null );
				}
			}
		}
#endif

		#endregion

		#region Public Properties

		/// <summary>
		/// 本客户端的关键字
		/// </summary>
		public string KeyWord => keyWord;

		/// <summary>
		/// 获取或设置重连服务器的间隔时间，单位：毫秒
		/// </summary>
		public int ReConnectTime { set => reconnectTime = value; get => reconnectTime; }

		#endregion

		#region Public Event

		/// <summary>
		/// 当接收到数据的事件信息，接收到数据的时候触发。
		/// </summary>
		public event Action<NetPushClient, string> OnReceived;

		#endregion

		#region Private Member

		private readonly IPEndPoint endPoint;                           // 服务器的地址及端口信息
		private readonly string keyWord = string.Empty;                 // 缓存的订阅关键字
		private Action<NetPushClient, string> action;                   // 服务器推送后的回调方法
		private int reconnectTime = 10000;                              // 重连服务器的时间
		private bool closed = false;

		#endregion

		#region Object Override

		/// <inheritdoc/>
		public override string ToString( ) => $"NetPushClient[{endPoint}]";

		#endregion
	}
}
