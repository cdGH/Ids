using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HslCommunication.Core.Net;
using System.Net.Sockets;
using System.Net;
#if !NET35 && !NET20
using System.Threading.Tasks;
#endif

namespace HslCommunication.Profinet.Toledo
{
	/// <summary>
	/// 托利多电子秤的TCP服务器，启动服务器后，等待电子秤的数据连接。
	/// </summary>
	public class ToledoTcpServer : NetworkServerBase
	{
		#region Constructor

		/// <summary>
		/// 实例化一个默认的对象
		/// </summary>
		public ToledoTcpServer( )
		{

		}

		#endregion

		#region Public Properties

		/// <summary>
		/// 获取或设置当前的报文否是含有校验的，默认为含有校验
		/// </summary>
		public bool HasChk { get; set; } = true;

		#endregion

		#region Override NetworkServerBase

		/// <inheritdoc/>
		protected override void ThreadPoolLogin( Socket socket, IPEndPoint endPoint )
		{
			AppSession session = new AppSession( );
			session.WorkSocket = socket;

			LogNet?.WriteDebug( ToString( ), string.Format( StringResources.Language.ClientOnlineInfo, session.IpEndPoint ) );

			if(!session.WorkSocket.BeginReceiveResult( ReceiveCallBack , session).IsSuccess)
				LogNet?.WriteError( ToString( ), StringResources.Language.NetClientLoginFailed );
		}
#if NET20 || NET35
		private void ReceiveCallBack(IAsyncResult ar )
#else
		private async void ReceiveCallBack( IAsyncResult ar )
#endif
		{
			if (ar.AsyncState is AppSession appSession)
			{
				if (!appSession.WorkSocket.EndReceiveResult( ar ).IsSuccess) return;
#if NET20 || NET35
				OperateResult<byte[]> read = Receive( appSession.WorkSocket, HasChk ? 18 : 17 );
#else
				OperateResult<byte[]> read = await ReceiveAsync( appSession.WorkSocket, HasChk ? 18 : 17 );
#endif
				if (!read.IsSuccess)
				{
					LogNet?.WriteDebug( ToString( ), string.Format( StringResources.Language.ClientOfflineInfo, appSession.IpEndPoint ) );
					appSession.WorkSocket?.Close( );
					return;
				}

				OnToledoStandardDataReceived?.Invoke( this, new ToledoStandardData( read.Content ) );

				if(!appSession.WorkSocket.BeginReceiveResult( ReceiveCallBack, appSession ).IsSuccess)
					LogNet?.WriteDebug( ToString( ), string.Format( StringResources.Language.ClientOfflineInfo, appSession.IpEndPoint ) );
			}
		}

		#endregion

		#region Event Handle

		/// <summary>
		/// 托利多数据接收时的委托
		/// </summary>
		/// <param name="sender">数据发送对象</param>
		/// <param name="toledoStandardData">数据对象</param>
		public delegate void ToledoStandardDataReceivedDelegate( object sender, ToledoStandardData toledoStandardData );

		/// <summary>
		/// 当接收到一条新的托利多的数据的时候触发
		/// </summary>
		public event ToledoStandardDataReceivedDelegate OnToledoStandardDataReceived;

		#endregion

		#region Object Override

		/// <inheritdoc/>
		public override string ToString( ) => $"ToledoTcpServer[{Port}]";

		#endregion
	}
}
