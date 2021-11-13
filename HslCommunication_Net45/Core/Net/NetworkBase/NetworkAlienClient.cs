using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using HslCommunication.Core.IMessage;
using System.Security.Cryptography;

namespace HslCommunication.Core.Net
{

	/// <summary>
	/// 异形客户端的基类，提供了基础的异形操作<br />
	/// The base class of the profiled client provides the basic profiled operation
	/// </summary>
	public class NetworkAlienClient : NetworkServerBase, IDisposable
	{
		#region Constructor

		/// <summary>
		/// 默认的无参构造方法<br />
		/// The default parameterless constructor
		/// </summary>
		public NetworkAlienClient( )
		{
			password        = new byte[6];
			trustOnline     = new List<string>( );
			trustLock       = new SimpleHybirdLock( );
		}

		#endregion

		#region NetworkServerBase Override

		/// <summary>
		/// 当接收到了新的请求的时候执行的操作<br />
		/// An action performed when a new request is received
		/// </summary>
		/// <param name="socket">异步对象</param>
		/// <param name="endPoint">终结点</param>
#if NET20 || NET35
		protected override void ThreadPoolLogin( Socket socket, IPEndPoint endPoint )
#else
		protected async override void ThreadPoolLogin( Socket socket, IPEndPoint endPoint )
#endif
		{
			// 注册包 ---- 二进制传输
			// 0x48 0x53 0x4C   0x00   0x19  0x31 0x32 0x33 0x34 0x35 0x36 0x37 0x38 0x39 0x30 0x31 0x00 0x00 0x00 0x00 0x00 0x00 0xC0 0xA8 0x00 0x01 0x17 0x10 0x00 0x00
			// +------------+   +--+   +--+  +----------------------------------------------------+ +---------------------------+ +-----------------+ +-------+ +-------+
			// +  HSL Head  +   length(大端)               DTU码 12345678901 (唯一标识)                 登录密码(不受信的排除)         Ip:192.168.0.1   端口10000    类型
			// +------------+   +--+   +--+  +----------------------------------------------------+ +---------------------------+ +-----------------+ +-------+ +-------+

			// 返回
			// 0x48 0x53 0x4C 0x00 0x01    0x00
			// +------------+ +--+ +--+    +--+
			//   固定消息头    长度(大端)  结果代码
			// +------------+ +--+ +--+    +--+

			// 结果代码 
			// 0x00: 登录成功 
			// 0x01: DTU重复登录 
			// 0x02: DTU禁止登录
			// 0x03: 密码验证失败 

#if NET20 || NET35
			OperateResult<byte[]> check = ReceiveByMessage( socket, 5000, new AlienMessage( ) );
#else
			OperateResult<byte[]> check = await ReceiveByMessageAsync( socket, 5000, new AlienMessage( ) );
#endif
			if (!check.IsSuccess) return;
			// 过滤不授信的连接
			if (check.Content?.Length < 22) { socket?.Close( ); return; }
			if (check.Content[0] != 0x48 ) { socket?.Close( ); return; } // || check.Content[1] != 0x53 || check.Content[2] != 0x4C

			// 获取DTU的唯一ID信息，如果不够11位的，移除空白字符
			string dtu = Encoding.ASCII.GetString( check.Content, 5, 11 ).Trim( '\0', ' ' );

			// 密码验证
			bool isPasswrodRight = true;
			if (isCheckPwd)
			{
				for (int i = 0; i < password.Length; i++)
				{
					if (check.Content[16 + i] != password[i])
					{
						isPasswrodRight = false;
						break;
					}
				}
			}

			// 密码失败的情况
			if (!isPasswrodRight)
			{
				if (isResponseAck)
				{
					OperateResult send = Send( socket, GetResponse( StatusPasswodWrong ) );
					if (send.IsSuccess) socket?.Close( );
				}
				else
					socket?.Close( );
				LogNet?.WriteWarn( ToString( ), "Login Password Wrong, Id:" + dtu );
				return;
			}

			AlienSession session = new AlienSession( )
			{
				DTU = dtu,
				Socket = socket,
				IsStatusOk = true,
				Pwd = check.Content.SelectMiddle( 16, 6 ).ToHexString( )
			};

			// 检测是否禁止登录
			if (!IsClientPermission( session ))
			{
				if (isResponseAck)
				{
					OperateResult send = Send( socket, GetResponse( StatusLoginForbidden ) );
					if (send.IsSuccess) socket?.Close( );
				}
				else
					socket?.Close( );
				LogNet?.WriteWarn( ToString( ), "Login Forbidden, Id:" + session.DTU );
				return;
			}

			// 检测是否重复登录，不重复的话，也就是意味着登录成功了
			int status = IsClientOnline( session );
			if (status != StatusOk)
			{
				if (isResponseAck)
				{
					OperateResult send = Send( socket, GetResponse( StatusLoginRepeat ) );
					if (send.IsSuccess) socket?.Close( );
				}
				else
					socket?.Close( );
				LogNet?.WriteWarn( ToString( ), GetMsgFromCode( session.DTU, status ) );
				return;
			}
			else
			{
				if (isResponseAck)
				{
					OperateResult send = Send( socket, GetResponse( StatusOk ) );
					if (!send.IsSuccess) return;
				}
				LogNet?.WriteWarn( ToString( ), GetMsgFromCode( session.DTU, status ) );
			}

			// 触发上线消息
			OnClientConnected?.Invoke( session );
		}

		#endregion

		#region Public Properties

		/// <summary>
		/// 是否返回响应，默认为 <c>True</c><br />
		/// The default is <c>True</c>
		/// </summary>
		public bool IsResponseAck
		{
			get => isResponseAck;
			set => isResponseAck = value;
		}

		/// <summary>
		/// 是否统一检查密码，如果每个会话需要自己检查密码，就需要设置为false<br />
		/// Whether to check the password uniformly, if each session needs to check the password by itself, it needs to be set to false
		/// </summary>
		public bool IsCheckPwd
		{
			get => isCheckPwd;
			set => isCheckPwd = value;
		}

		#endregion

		#region Client Event

		/// <summary>
		/// 客户上线的委托事件
		/// </summary>
		/// <param name="session">异形客户端的会话信息</param>
		public delegate void OnClientConnectedDelegate( AlienSession session);

		/// <summary>
		/// 当有服务器连接上来的时候触发<br />
		/// Triggered when a server is connected
		/// </summary>
		public event OnClientConnectedDelegate OnClientConnected = null;

		#endregion

		#region Private Method

		/// <summary>
		/// 获取返回的命令信息
		/// </summary>
		/// <param name="status">状态</param>
		/// <returns>回发的指令信息</returns>
		private byte[] GetResponse(byte status) => new byte[] { 0x48,0x73,0x6E,0x00,0x01,status };

		/// <summary>
		/// 检测当前的DTU是否在线
		/// </summary>
		/// <param name="session">当前的会话信息</param>
		/// <returns>当前的会话是否在线</returns>
		public virtual int IsClientOnline( AlienSession session ) => 0;

		/// <summary>
		/// 检测当前的dtu是否允许登录
		/// </summary>
		/// <param name="session">当前的会话信息</param>
		/// <returns>当前的id是否可允许登录</returns>
		private bool IsClientPermission( AlienSession session )
		{
			bool result = false;

			trustLock.Enter( );

			if (trustOnline.Count == 0)
			{
				result = true;
			}
			else
			{
				for (int i = 0; i < trustOnline.Count; i++)
				{
					if (trustOnline[i] == session.DTU)
					{
						result = true;
						break;
					}
				}
			}

			trustLock.Leave( );
			return result;
		}

		#endregion

		#region Public Method

		/// <summary>
		/// 设置密码，需要传入长度为6的字节数组<br />
		/// To set the password, you need to pass in an array of bytes of length 6
		/// </summary>
		/// <param name="password">密码信息</param>
		public void SetPassword(byte[] password)
		{
			if(password?.Length == 6)
			{
				password.CopyTo( this.password, 0 );
			}
		}

		/// <summary>
		/// 设置可信任的客户端列表，传入一个DTU的列表信息<br />
		/// Set up the list of trusted clients, passing in the list information for a DTU
		/// </summary>
		/// <param name="clients">客户端列表</param>
		public void SetTrustClients(string[] clients)
		{
			trustOnline = new List<string>( clients );
		}

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
					this.trustLock?.Dispose( );
					this.OnClientConnected = null;
				}

				// TODO: 释放未托管的资源(未托管的对象)并重写终结器
				// TODO: 将大型字段设置为 null
				disposedValue = true;
			}
		}

		// // TODO: 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
		// ~NetworkAlienClient()
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

		private byte[] password;                     // 密码设置
		private List<string> trustOnline;            // 禁止登录的客户端信息
		private SimpleHybirdLock trustLock;          // 禁止登录的锁
		private bool isResponseAck = true;           // 是否返回数据结果
		private bool isCheckPwd = true;              // 是否统一检查密码，如果每个会话需要自己检查密码，就需要设置为false
		private bool disposedValue;

		#endregion

		#region Object Override

		/// <inheritdoc/>
		public override string ToString( ) => "NetworkAlienBase";

		#endregion

		#region Const Value

		/// <summary>
		/// 状态登录成功
		/// </summary>
		public const byte StatusOk = 0x00;

		/// <summary>
		/// 重复登录
		/// </summary>
		public const byte StatusLoginRepeat = 0x01;

		/// <summary>
		/// 禁止登录
		/// </summary>
		public const byte StatusLoginForbidden = 0x02;

		/// <summary>
		/// 密码错误
		/// </summary>
		public const byte StatusPasswodWrong = 0x03;

		/// <summary>
		/// 获取错误的描述信息
		/// </summary>
		/// <param name="dtu">dtu信息</param>
		/// <param name="code">错误码</param>
		/// <returns>错误信息</returns>
		public static string GetMsgFromCode(string dtu, int code )
		{
			if      (code == StatusOk)             return "Login Success, Id:" + dtu;
			else if (code == StatusLoginRepeat)    return "Login Repeat, Id:" + dtu;
			else if (code == StatusLoginForbidden) return "Login Forbidden, Id:" + dtu;
			else if (code == StatusPasswodWrong)   return "Login Passwod Wrong, Id:" + dtu;
			else return "Login Unknow reason, Id:" + dtu;
		}

		#endregion
	}
}
