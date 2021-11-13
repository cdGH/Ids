﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
#if !NET35 && !NET20
using System.Threading.Tasks;
#endif

namespace HslCommunication.Core.Net
{
	/// <summary>
	/// 带登录认证的服务器类，可以对连接的客户端进行筛选，放行用户名密码正确的连接<br />
	/// Server class with login authentication, which can filter connected clients and allow connections with correct username and password
	/// </summary>
	public class NetworkAuthenticationServerBase : NetworkServerBase, IDisposable
	{
		#region ServerBase Override

		/// <summary>
		/// 当客户端的socket登录的时候额外检查的信息，检查当前会话的用户名和密码<br />
		/// Additional check information when the client's socket logs in, check the username and password of the current session
		/// </summary>
		/// <param name="socket">套接字</param>
		/// <param name="endPoint">终结点</param>
		/// <returns>验证的结果</returns>
		protected override OperateResult SocketAcceptExtraCheck( Socket socket, IPEndPoint endPoint )
		{
			if (IsUseAccountCertificate)
			{
				OperateResult<byte[], byte[]> receive = ReceiveAndCheckBytes( socket, 2000 );
				if (!receive.IsSuccess) return new OperateResult( string.Format( "Client login failed[{0}]", endPoint ) );

				if (BitConverter.ToInt32( receive.Content1, 0 ) != HslProtocol.ProtocolAccountLogin)
				{
					// 拒绝登录
					LogNet?.WriteError( ToString( ), StringResources.Language.NetClientAccountTimeout );
					// Send( socket, HslProtocol.CommandBytes( HslProtocol.ProtocolAccountRejectLogin, 0, Token, Encoding.Unicode.GetBytes( "Authentication failed" ) ) );
					socket?.Close( );
					return new OperateResult( string.Format( "Authentication failed[{0}]", endPoint ) );
				}

				string[] infos = HslProtocol.UnPackStringArrayFromByte( receive.Content2 );
				string ret = CheckAccountLegal( infos );
				SendStringAndCheckReceive( socket, ret == "success" ? 1 : 0, new string[] { ret } );

				if (ret != "success")
				{
					return new OperateResult( string.Format( "Client login failed[{0}]:{1} {2}", endPoint, ret, BasicFramework.SoftBasic.ArrayFormat( infos ) ) );
				}

				LogNet?.WriteDebug( ToString( ), string.Format( "Account Login:{0} Endpoint:[{1}]", infos[0], endPoint ) );
			}
			return OperateResult.CreateSuccessResult( );
		}

		#endregion

		#region Account Certification

		/// <summary>
		/// 获取或设置是否对客户端启动账号认证<br />
		/// Gets or sets whether to enable account authentication on the client
		/// </summary>
		public bool IsUseAccountCertificate { get; set; }

		private Dictionary<string, string> accounts = new Dictionary<string, string>( );
		private SimpleHybirdLock lockLoginAccount = new SimpleHybirdLock( );

		/// <summary>
		/// 新增账户，如果想要启动账户登录，必须将<see cref="IsUseAccountCertificate"/>设置为<c>True</c>。<br />
		/// Add an account. If you want to activate account login, you must set <see cref="IsUseAccountCertificate"/> to <c> True </c>
		/// </summary>
		/// <param name="userName">账户名称</param>
		/// <param name="password">账户名称</param>
		public void AddAccount( string userName, string password )
		{
			if (!string.IsNullOrEmpty( userName ))
			{
				lockLoginAccount.Enter( );
				if (accounts.ContainsKey( userName ))
				{
					accounts[userName] = password;
				}
				else
				{
					accounts.Add( userName, password );
				}
				lockLoginAccount.Leave( );
			}
		}

		/// <summary>
		/// 删除一个账户的信息<br />
		/// Delete an account's information
		/// </summary>
		/// <param name="userName">账户名称</param>
		public void DeleteAccount( string userName )
		{
			lockLoginAccount.Enter( );
			if (accounts.ContainsKey( userName ))
			{
				accounts.Remove( userName );
			}
			lockLoginAccount.Leave( );
		}

		private string CheckAccountLegal( string[] infos )
		{
			if (infos?.Length < 2) return "User Name input wrong";
			string ret = "";
			lockLoginAccount.Enter( );
			if (!accounts.ContainsKey( infos[0] ))
			{
				ret = "User Name input wrong";
			}
			else
			{
				if (accounts[infos[0]] != infos[1])
				{
					ret = "Password is not corrent";
				}
				else
				{
					ret = "success";
				}
			}
			lockLoginAccount.Leave( );
			return ret;
		}

		#endregion

		#region IDisposable Support

		private bool disposedValue = false; // 要检测冗余调用

		/// <summary>
		/// 释放当前的对象
		/// </summary>
		/// <param name="disposing">是否托管对象</param>
		protected virtual void Dispose( bool disposing )
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					ServerClose( );
					lockLoginAccount?.Dispose( );
					// TODO: 释放托管状态(托管对象)。
				}

				// TODO: 释放未托管的资源(未托管的对象)并在以下内容中替代终结器。
				// TODO: 将大型字段设置为 null。

				disposedValue = true;
			}
		}

		// TODO: 仅当以上 Dispose(bool disposing) 拥有用于释放未托管资源的代码时才替代终结器。
		// ~NetworkDataServerBase()
		// {
		//   // 请勿更改此代码。将清理代码放入以上 Dispose(bool disposing) 中。
		//   Dispose(false);
		// }

		// 添加此代码以正确实现可处置模式。

		/// <inheritdoc cref="IDisposable.Dispose()"/>
		public void Dispose( )
		{
			// 请勿更改此代码。将清理代码放入以上 Dispose(bool disposing) 中。
			Dispose( true );
			// TODO: 如果在以上内容中替代了终结器，则取消注释以下行。
			// GC.SuppressFinalize(this);
		}

		#endregion

		#region Object Override

		/// <inheritdoc/>
		public override string ToString( ) => $"NetworkAuthenticationServerBase[{Port}]";

		#endregion
	}
}
