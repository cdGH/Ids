using HslCommunication.Core.Net;
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

namespace HslCommunication.Enthernet
{
	/// <summary>
	/// 消息处理服务器，主要用来实现接收客户端信息并进行消息反馈的操作，适用于客户端进行远程的调用，要求服务器反馈数据。<br />
	/// The message processing server is mainly used to implement the operation of receiving client information and performing message feedback. It is applicable to remote calls made by clients and requires the server to feedback data.
	/// </summary>
	/// <remarks>
	/// 详细的使用说明，请参照博客<a href="http://www.cnblogs.com/dathlin/p/7697782.html">http://www.cnblogs.com/dathlin/p/7697782.html</a>
	/// </remarks>
	/// <example>
	/// 此处贴上了Demo项目的服务器配置的示例代码
	/// <code lang="cs" source="TestProject\SimplifyNetTest\FormServer.cs" region="Simplify Net" title="NetSimplifyServer示例" />
	/// </example>
	public class NetSimplifyServer : NetworkAuthenticationServerBase
	{
		#region Constructor

		/// <summary>
		/// 实例化一个默认的对象<br />
		/// Instantiate a default object
		/// </summary>
		public NetSimplifyServer( ) { }

		#endregion

		#region Event Handle

		/// <summary>
		/// 接收字符串信息的事件
		/// </summary>
		public event Action<AppSession, NetHandle, string> ReceiveStringEvent;

		/// <summary>
		/// 接收字符串数组信息的事件
		/// </summary>
		public event Action<AppSession, NetHandle, string[]> ReceiveStringArrayEvent;

		/// <summary>
		/// 接收字节信息的事件
		/// </summary>
		public event Action<AppSession, NetHandle, byte[]> ReceivedBytesEvent;

		#endregion

		#region Public Method

		/// <summary>
		/// 向指定的通信对象发送字符串数据
		/// </summary>
		/// <param name="session">通信对象</param>
		/// <param name="customer">用户的指令头</param>
		/// <param name="str">实际发送的字符串数据</param>
		public void SendMessage( AppSession session, int customer, string str ) => Send( session.WorkSocket, HslProtocol.CommandBytes( customer, Token, str ) );

		/// <summary>
		/// 向指定的通信对象发送字符串数组
		/// </summary>
		/// <param name="session">通信对象</param>
		/// <param name="customer">用户的指令头</param>
		/// <param name="str">实际发送的字符串数组</param>
		public void SendMessage( AppSession session, int customer, string[] str ) => Send( session.WorkSocket, HslProtocol.CommandBytes( customer, Token, str ) );

		/// <summary>
		/// 向指定的通信对象发送字节数据
		/// </summary>
		/// <param name="session">连接对象</param>
		/// <param name="customer">用户的指令头</param>
		/// <param name="bytes">实际的数据</param>
		public void SendMessage( AppSession session, int customer, byte[] bytes ) => Send( session.WorkSocket, HslProtocol.CommandBytes( customer, Token, bytes ) );

		#endregion

		#region Start Close

		/// <summary>
		/// 关闭网络的操作
		/// </summary>
		protected override void CloseAction()
		{
			ReceivedBytesEvent = null;
			ReceiveStringEvent = null;
			base.CloseAction( );
		}

		/// <summary>
		/// 当接收到了新的请求的时候执行的操作
		/// </summary>
		/// <param name="socket">异步对象</param>
		/// <param name="endPoint">终结点</param>
		protected override void ThreadPoolLogin( Socket socket, IPEndPoint endPoint )
		{
			AppSession session = new AppSession( );

			session.WorkSocket = socket;
			try
			{
				session.IpEndPoint = endPoint;
				session.IpAddress = session.IpEndPoint.Address.ToString( );
			}
			catch (Exception ex)
			{
				LogNet?.WriteException( ToString( ), StringResources.Language.GetClientIpAddressFailed, ex );
			}

			LogNet?.WriteDebug( ToString( ), string.Format( StringResources.Language.ClientOnlineInfo, session.IpEndPoint ) );

			try
			{
				session.WorkSocket.BeginReceive( new byte[0], 0, 0, SocketFlags.None, new AsyncCallback( ReceiveCallback ), session );
				Interlocked.Increment( ref clientCount );
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
				if (!appSession.WorkSocket.EndReceiveResult( ar ).IsSuccess) { AppSessionRemoteClose( appSession ); return; }

				// 接收hsl协议的消息文本
#if NET35 || NET20
				OperateResult<int, int, byte[]> read = ReceiveHslMessage( appSession.WorkSocket );
#else
				OperateResult<int, int, byte[]> read = await ReceiveHslMessageAsync( appSession.WorkSocket );
#endif
				if (!read.IsSuccess) { AppSessionRemoteClose( appSession ); return; }

				int protocol = read.Content1;
				int customer = read.Content2;
				byte[] content = read.Content3;

				//接收数据完成，进行事件通知，优先进行解密操作
				if (protocol == HslProtocol.ProtocolCheckSecends)
				{
					// 初始化时候的测试消息
					appSession.HeartTime = DateTime.Now;
					SendMessage( appSession, customer, content );
					LogNet?.WriteDebug( ToString( ), string.Format( "Heart Check From {0}", appSession.IpEndPoint ) );
				}
				else if (protocol == HslProtocol.ProtocolUserBytes)
				{
					// 字节数据
					ReceivedBytesEvent?.Invoke( appSession, customer, content );
				}
				else if (protocol == HslProtocol.ProtocolUserString)
				{
					// 字符串数据
					ReceiveStringEvent?.Invoke( appSession, customer, Encoding.Unicode.GetString( content ) );
				}
				else if (protocol == HslProtocol.ProtocolUserStringArray)
				{
					// 字符串数组
					ReceiveStringArrayEvent?.Invoke( appSession, customer, HslProtocol.UnPackStringArrayFromByte( content ) );
				}
				else if (protocol == HslProtocol.ProtocolClientQuit)
				{
					// 退出系统
					AppSessionRemoteClose( appSession );
					return;
				}
				else
				{
					// 数据异常
					AppSessionRemoteClose( appSession );
					return;
				}

				if (!appSession.WorkSocket.BeginReceiveResult( ReceiveCallback, appSession ).IsSuccess) AppSessionRemoteClose( appSession );
			}
		}

		/// <summary>
		/// 让客户端正常下线，调用本方法即可自由控制会话客户端强制下线操作。
		/// </summary>
		/// <param name="session">会话对象</param>
		public void AppSessionRemoteClose( AppSession session )
		{
			session.WorkSocket?.Close( );
			Interlocked.Decrement( ref clientCount );
			LogNet?.WriteDebug( ToString( ), string.Format( StringResources.Language.ClientOfflineInfo, session.IpEndPoint ) );
		}

		#endregion

		#region Public Properties

		/// <summary>
		/// 当前在线的客户端数量
		/// </summary>
		public int ClientCount => clientCount;

		#endregion

		#region Private Member

		private int clientCount = 0;                                    // 在线客户端的数量

		#endregion

		#region Object Override

		/// <inheritdoc/>
		public override string ToString( ) => $"NetSimplifyServer[{Port}]";

		#endregion
	}
}
