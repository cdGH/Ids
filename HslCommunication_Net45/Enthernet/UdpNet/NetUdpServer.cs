using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using HslCommunication.Core;
using HslCommunication.Core.Net;
#if !NET35 && !NET20
using System.Threading.Tasks;
#endif

namespace HslCommunication.Enthernet
{
	/// <summary>
	/// Udp网络的服务器端类，您可以使用本类构建一个简单的，高性能的udp服务器，接收来自其他客户端的数据，当然，您也可以自定义返回你要返回的数据<br />
	/// Server-side class of Udp network. You can use this class to build a simple, high-performance udp server that receives data from other clients. Of course, you can also customize the data you want to return.
	/// </summary>
	public class NetUdpServer : NetworkServerBase
	{
		#region Constructor

		/// <inheritdoc/>
		public NetUdpServer( ) { }

		#endregion

		#region Public Properties

		/// <summary>
		/// 获取或设置一次接收时的数据长度，默认2KB数据长度
		/// </summary>
		public int ReceiveCacheLength { get; set; } = 2048;

		#endregion

		#region Start Close

		/// <inheritdoc/>
		public override void ServerStart( int port )
		{
			if (!IsStarted)
			{
				CoreSocket = new Socket( AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp );

				//绑定网络地址
				CoreSocket.Bind( new IPEndPoint( IPAddress.Any, port ) );
				RefreshReceive( );
				LogNet?.WriteInfo( ToString(), StringResources.Language.NetEngineStart );
				IsStarted = true;
			}
		}

		/// <inheritdoc/>
		protected override void CloseAction( )
		{
			AcceptString = null;
			AcceptByte = null;
			base.CloseAction( );
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

		#endregion

		#region Private Receive Callback

		private void AsyncCallback( IAsyncResult ar )
		{
			if (ar.AsyncState is AppSession session)
			{
				try
				{
					int received = session.WorkSocket.EndReceiveFrom( ar, ref session.UdpEndPoint );
					// 马上开始重新接收，提供性能保障
					RefreshReceive( );
					// 处理数据
					if (received >= HslProtocol.HeadByteLength)
					{
						// 检测令牌
						if (CheckRemoteToken( session.BytesContent ))
						{
							session.IpEndPoint = (IPEndPoint)session.UdpEndPoint;
							int contentLength = BitConverter.ToInt32( session.BytesContent, HslProtocol.HeadByteLength - 4 );
							if (contentLength == received - HslProtocol.HeadByteLength)
							{
								byte[] head = new byte[HslProtocol.HeadByteLength];
								byte[] content = new byte[contentLength];

								Array.Copy( session.BytesContent, 0, head, 0, HslProtocol.HeadByteLength );
								if (contentLength > 0)
								{
									Array.Copy( session.BytesContent, 32, content, 0, contentLength );
								}

								// 解析内容
								content = HslProtocol.CommandAnalysis( head, content );

								int protocol = BitConverter.ToInt32( head, 0 );
								int customer = BitConverter.ToInt32( head, 4 );
								// 丢给数据中心处理
								DataProcessingCenter( session, protocol, customer, content );
							}
							else
							{
								// 否则记录到日志
								LogNet?.WriteWarn( ToString(), $"Should Rece：{(BitConverter.ToInt32( session.BytesContent, 4 ) + 8)} Actual：{received}" );
							}
						}
						else
						{
							LogNet?.WriteWarn( ToString( ), StringResources.Language.TokenCheckFailed );
						}
					}
					else
					{
						LogNet?.WriteWarn( ToString( ), $"Receive error, Actual：{received}" );
					}
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

		#region Data Process Center

		/// <summary>
		/// 数据处理中心
		/// </summary>
		/// <param name="session">会话信息</param>
		/// <param name="protocol">暗号</param>
		/// <param name="customer"></param>
		/// <param name="content"></param>
		private void DataProcessingCenter( AppSession session, int protocol, int customer, byte[] content )
		{
			if (protocol == HslProtocol.ProtocolUserBytes)
			{
				AcceptByte?.Invoke( session, customer, content );
			}
			else if (protocol == HslProtocol.ProtocolUserString)
			{
				// 接收到文本数据
				string str = Encoding.Unicode.GetString( content );
				AcceptString?.Invoke( session, customer, str );
			}
		}

		/// <summary>
		/// 向指定的通信对象发送字符串数据
		/// </summary>
		/// <param name="session">通信对象</param>
		/// <param name="customer">用户的指令头</param>
		/// <param name="str">实际发送的字符串数据</param>
		public void SendMessage( AppSession session, int customer, string str ) => SendBytesAsync( session, HslProtocol.CommandBytes( customer, Token, str ) );

		/// <summary>
		/// 向指定的通信对象发送字节数据
		/// </summary>
		/// <param name="session">连接对象</param>
		/// <param name="customer">用户的指令头</param>
		/// <param name="bytes">实际的数据</param>
		public void SendMessage( AppSession session, int customer, byte[] bytes ) => SendBytesAsync( session, HslProtocol.CommandBytes( customer, Token, bytes ) );

		private void SendBytesAsync( AppSession session, byte[] data )
		{
			try
			{
				session.WorkSocket.SendTo( data, data.Length, SocketFlags.None, session.UdpEndPoint );
			}
			catch(Exception ex)
			{
				LogNet?.WriteException( "SendMessage", ex );
			}
		}

		#endregion

		#region Event Handle
		
		/// <summary>
		/// 当接收到文本数据的时候,触发此事件
		/// </summary>
		public event Action<AppSession, NetHandle, string> AcceptString;

		/// <summary>
		/// 当接收到字节数据的时候,触发此事件
		/// </summary>
		public event Action<AppSession, NetHandle, byte[]> AcceptByte;

		#endregion
		
		#region Object Override

		/// <inheritdoc/>
		public override string ToString( ) => $"NetUdpServer[{Port}]";

		#endregion

	}
}
