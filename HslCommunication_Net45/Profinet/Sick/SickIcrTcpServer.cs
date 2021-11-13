using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using HslCommunication.Core.Net;
using HslCommunication;

namespace HslCommunication.Profinet.Sick
{
	/// <summary>
	/// Sick的扫码器的服务器信息，只要启动服务器之后，扫码器配置将条码发送到PC的指定端口上来即可，就可以持续的接收条码信息，同样也适用于海康，基恩士，DATELOGIC 。<br />
	/// The server information of Sick's code scanner, as long as the server is started, the code scanner is configured to send the barcode to the designated port of the PC, and it can continuously receive the barcode information.
	/// </summary>
	public class SickIcrTcpServer : NetworkServerBase
	{
		#region Constructor

		/// <summary>
		/// 实例化一个默认的服务器对象<br />
		/// Instantiate a default server object
		/// </summary>
		public SickIcrTcpServer( )
		{
			initiativeClients = new List<AppSession>( );
			lockClients       = new object( );
		}

		#endregion

		#region Event Handle

		/// <summary>
		/// 接收条码数据的委托信息<br />
		/// Entrusted information to receive barcode data
		/// </summary>
		/// <param name="ipAddress">Ip地址信息</param>
		/// <param name="barCode">条码信息</param>
		public delegate void ReceivedBarCodeDelegate( string ipAddress, string barCode );

		/// <summary>
		/// 当接收到条码数据的时候触发<br />
		/// Triggered when barcode data is received
		/// </summary>
		public event ReceivedBarCodeDelegate OnReceivedBarCode;

		#endregion

		#region Override Method

		/// <inheritdoc/>
		protected override void ThreadPoolLogin( Socket socket, IPEndPoint endPoint )
		{
			// 开始接收数据信息
			AppSession appSession = new AppSession( );
			appSession.IpEndPoint = endPoint;
			appSession.IpAddress = endPoint.Address.ToString( );
			appSession.WorkSocket = socket;
			try
			{
				socket.BeginReceive( new byte[0], 0, 0, SocketFlags.None, new AsyncCallback( SocketAsyncCallBack ), appSession );
				AddClient( appSession );
			}
			catch
			{
				socket.Close( );
				LogNet?.WriteDebug( ToString( ), string.Format( StringResources.Language.ClientOfflineInfo, endPoint ) );
			}
		}

		private void SocketAsyncCallBack( IAsyncResult ar )
		{
			if (ar.AsyncState is AppSession session)
			{
				try
				{
					session.WorkSocket.EndReceive( ar );

					byte[] buffer = new byte[1024];
					int receiveCount = session.WorkSocket.Receive( buffer );

					if (receiveCount > 0)
					{
						byte[] code = new byte[receiveCount];
						Array.Copy( buffer, 0, code, 0, receiveCount );
						session.WorkSocket.BeginReceive( new byte[0], 0, 0, SocketFlags.None, new AsyncCallback( SocketAsyncCallBack ), session );
						if(Authorization.nzugaydgwadawdibbas( ))
							OnReceivedBarCode?.Invoke( session.IpAddress, TranslateCode( Encoding.ASCII.GetString( code ) ) );
					}
					else
					{
						session.WorkSocket?.Close( );
						LogNet?.WriteDebug( ToString( ), string.Format( StringResources.Language.ClientOfflineInfo, session.IpEndPoint ) );
						RemoveClient( session );
						return;
					}
				}
				catch
				{
					// 关闭连接，记录日志
					session.WorkSocket?.Close( );
					LogNet?.WriteDebug( ToString( ), string.Format( StringResources.Language.ClientOfflineInfo, session.IpEndPoint ) );
					RemoveClient( session );
					return;
				}
			}
		}

		private string TranslateCode( string code )
		{
			StringBuilder temp = new StringBuilder( "" );
			for (int i = 0; i < code.Length; i++)
			{
				if (char.IsLetterOrDigit( code, i ))
				{
					temp.Append( code[i] );
				}
			}
			return temp.ToString( );
		}

		#endregion

		#region Connect Client

		/// <summary>
		/// 新增一个主动连接的请求，将不会收到是否连接成功的信息，当网络中断及奔溃之后，会自动重新连接。<br />
		/// A new active connection request will not receive a message whether the connection is successful. When the network is interrupted and crashed, it will automatically reconnect.
		/// </summary>
		/// <param name="ipAddress">对方的Ip地址</param>
		/// <param name="port">端口号</param>
		public void AddConnectBarcodeScan( string ipAddress, int port )
		{
			IPEndPoint endPoint = new IPEndPoint( IPAddress.Parse( ipAddress ), port );
			AppSession appSession = new AppSession( );
			appSession.IpEndPoint = endPoint;
			appSession.IpAddress = endPoint.Address.ToString( );

			System.Threading.ThreadPool.QueueUserWorkItem( new System.Threading.WaitCallback( ConnectBarcodeScan ), appSession );
		}

		private void ConnectBarcodeScan(object obj )
		{
			if (obj is AppSession session)
			{
				OperateResult<Socket> connect = CreateSocketAndConnect( session.IpEndPoint, 5000 );
				if (!connect.IsSuccess)
				{
					System.Threading.Thread.Sleep( 1000 );
					System.Threading.ThreadPool.QueueUserWorkItem( new System.Threading.WaitCallback( ConnectBarcodeScan ), session );
				}
				else
				{
					session.WorkSocket = connect.Content;
					try
					{
						session.WorkSocket.BeginReceive( new byte[0], 0, 0, SocketFlags.None, new AsyncCallback( InitiativeSocketAsyncCallBack ), session );
						AddClient( session );
					}
					catch
					{
						session.WorkSocket.Close( );
						System.Threading.ThreadPool.QueueUserWorkItem( new System.Threading.WaitCallback( ConnectBarcodeScan ), session );
					}
				}
			}
		}

		private void InitiativeSocketAsyncCallBack( IAsyncResult ar )
		{
			if (ar.AsyncState is AppSession session)
			{
				try
				{
					session.WorkSocket.EndReceive( ar );

					byte[] buffer = new byte[1024];
					int receiveCount = session.WorkSocket.Receive( buffer );

					if (receiveCount > 0)
					{
						byte[] code = new byte[receiveCount];
						Array.Copy( buffer, 0, code, 0, receiveCount );
						session.WorkSocket.BeginReceive( new byte[0], 0, 0, SocketFlags.None, new AsyncCallback( InitiativeSocketAsyncCallBack ), session );
						if (Authorization.nzugaydgwadawdibbas( ))
							OnReceivedBarCode?.Invoke( session.IpAddress, TranslateCode( Encoding.ASCII.GetString( code ) ) );
					}
					else
					{
						session.WorkSocket?.Close( );
						LogNet?.WriteDebug( ToString( ), string.Format( StringResources.Language.ClientOfflineInfo, session.IpEndPoint ) );
						RemoveClient( session );
						return;
					}
				}
				catch
				{
					// 关闭连接，记录日志
					session.WorkSocket?.Close( );
					LogNet?.WriteDebug( ToString( ), string.Format( StringResources.Language.ClientOfflineInfo, session.IpEndPoint ) );
					RemoveClient( session );
					if(IsStarted) ConnectBarcodeScan( session );
				}
			}
		}

		#endregion

		#region Client Managment

		/// <summary>
		/// 获取当前在线的客户端数量<br />
		/// Get the number of clients currently online
		/// </summary>
		public int OnlineCount => clientCount;

		private void AddClient(AppSession session )
		{
			lock (lockClients)
			{
				clientCount++;
				initiativeClients.Add( session );
			}
		}

		private void RemoveClient( AppSession session )
		{
			lock (lockClients)
			{
				clientCount--;
				initiativeClients.Remove( session );
			}
		}

		#endregion

		#region Object Override

		/// <inheritdoc/>
		protected override void CloseAction( )
		{
			lock (lockClients)
			{
				for (int i = 0; i < initiativeClients.Count; i++)
				{
					initiativeClients[i].WorkSocket?.Close( );
				}
				initiativeClients.Clear( );
			}
		}

		/// <inheritdoc/>
		public override string ToString( ) => $"SickIcrTcpServer[{Port}]";

		#endregion

		#region Private Member

		private int clientCount = 0;                              // 客户端在线的数量信息
		private List<AppSession> initiativeClients;               // 主动连接的客户端信息
		private object lockClients;

		#endregion
	}
}
