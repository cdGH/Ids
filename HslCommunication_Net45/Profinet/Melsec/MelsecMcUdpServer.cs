using HslCommunication.Core.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using HslCommunication.Reflection;

namespace HslCommunication.Profinet.Melsec
{
	/// <inheritdoc/>
	public class MelsecMcUdpServer : MelsecMcServer
	{
		#region Constructor

		/// <summary>
		/// 实例化一个默认参数的mc协议的服务器<br />
		/// Instantiate a mc protocol server with default parameters
		/// </summary>
		/// <param name="isBinary">是否是二进制，默认是二进制，否则是ASCII格式</param>
		public MelsecMcUdpServer( bool isBinary = true ) : base( isBinary )
		{
		}

		#endregion

		#region Public Properties

		/// <summary>
		/// 获取或设置一次接收时的数据长度，默认2KB数据长度
		/// </summary>
		public int ReceiveCacheLength { get; set; } = 2048;

		#endregion

		/// <inheritdoc/>
		public override void ServerStart( int port )
		{
			if (!IsStarted)
			{
				StartInitialization( );
				CoreSocket = new Socket( AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp );

				//绑定网络地址
				CoreSocket.Bind( new IPEndPoint( IPAddress.Any, port ) );
				RefreshReceive( );
				IsStarted = true;
				Port = port;
				LogNet?.WriteInfo( ToString( ), StringResources.Language.NetEngineStart );
			}
		}

		/// <summary>
		/// 重新开始接收数据
		/// </summary>
		/// <exception cref="ArgumentNullException"></exception>
		private void RefreshReceive( )
		{
			AppSession session = new AppSession( );
			session.WorkSocket = CoreSocket;
			session.UdpEndPoint = new IPEndPoint( IPAddress.Any, 0 );
			session.BytesContent = new byte[ReceiveCacheLength];
			CoreSocket.BeginReceiveFrom( session.BytesContent, 0, ReceiveCacheLength, SocketFlags.None, ref session.UdpEndPoint, new AsyncCallback( AsyncCallback ), session );
		}

		#region Private Receive Callback

		private void AsyncCallback( IAsyncResult ar )
		{
			if (ar.AsyncState is AppSession session)
			{
				try
				{
					int received = session.WorkSocket.EndReceiveFrom( ar, ref session.UdpEndPoint );

					if (!Authorization.asdniasnfaksndiqwhawfskhfaiw( )) { RemoveClient( session ); return; };

					// 马上开始重新接收，提供性能保障
					RefreshReceive( );
					// 处理数据
					byte[] content = new byte[received];
					Array.Copy( session.BytesContent, 0, content, 0, received );
					byte[] back = null;

					if (IsBinary)
					{
						back = ReadFromMcCore( content.RemoveBegin( 11 ) );
					}
					else
					{
						back = ReadFromMcAsciiCore( content.RemoveBegin( 22 ) );
					}

					LogNet?.WriteDebug( ToString( ), $"Udp {StringResources.Language.Receive}：{(this.IsBinary ? content.ToHexString( ' ' ) : Encoding.ASCII.GetString( content ))}" );

					if (back != null)
					{
						session.WorkSocket.SendTo( back, back.Length, SocketFlags.None, session.UdpEndPoint );
						LogNet?.WriteDebug( ToString( ), $"Udp {StringResources.Language.Send}：{(this.IsBinary ? back.ToHexString( ' ' ) : Encoding.ASCII.GetString( back ))}" );
					}
					else
					{
						RemoveClient( session );
						return;
					}

					RaiseDataReceived( session, content );
				}
				catch (ObjectDisposedException)
				{
					//主程序退出的时候触发
				}
				catch (Exception ex)
				{
					LogNet?.WriteException( ToString( ), StringResources.Language.SocketEndReceiveException, ex );
					//重新接收，此处已经排除掉了对象释放的异常
					RefreshReceive( );
				}
				finally
				{
					//state = null;
				}

			}
		}

		#endregion

		#region Object Override

		/// <inheritdoc/>
		public override string ToString( ) => $"MelsecMcUdpServer[{Port}]";

		#endregion
	}
}
