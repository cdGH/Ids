using HslCommunication.BasicFramework;
using HslCommunication.Core.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace HslCommunication.Profinet.Omron
{
	/// <inheritdoc cref="OmronFinsServer"/>
	public class OmronFinsUdpServer : OmronFinsServer
	{
		/// <summary>
		/// 实例化一个默认的对象<br />
		/// Instantiate a default object
		/// </summary>
		public OmronFinsUdpServer( )
		{

		}

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

		/// <inheritdoc/>
		protected override byte[] PackCommand( int status, byte[] finsCore, byte[] data )
		{
			if (data == null) data = new byte[0];

			byte[] back = new byte[14 + data.Length];
			SoftBasic.HexStringToBytes( "00 00 00 00 00 00 00 00 00 00 00 00 00 00" ).CopyTo( back, 0 );
			if (data.Length > 0) data.CopyTo( back, 14 );
			back[10] = finsCore[0];
			back[11] = finsCore[1];
			back[12] = BitConverter.GetBytes( status )[1];
			back[13] = BitConverter.GetBytes( status )[0];
			return back;
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

					LogNet?.WriteDebug( ToString( ), $"Udp {StringResources.Language.Receive}：{content.ToHexString( ' ' )}" );

					byte[] back = ReadFromFinsCore( content.RemoveBegin( 10 ) );
					if (back != null)
					{
						session.WorkSocket.SendTo( back, back.Length, SocketFlags.None, session.UdpEndPoint );
						LogNet?.WriteDebug( ToString( ), $"Udp {StringResources.Language.Send}：{back.ToHexString( ' ' )}" );
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
		public override string ToString( ) => $"OmronFinsUdpServer[{Port}]";

		#endregion
	}
}
