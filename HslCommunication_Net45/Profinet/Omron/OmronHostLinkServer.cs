using HslCommunication.BasicFramework;
using HslCommunication.Core.Net;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Net.Sockets;
using System.Text;
#if !NET20 && !NET35
using System.Threading.Tasks;
#endif

namespace HslCommunication.Profinet.Omron
{
	/// <summary>
	/// <b>[商业授权]</b> 欧姆龙的HostLink虚拟服务器，支持DM区，CIO区，Work区，Hold区，Auxiliary区，可以方便的进行测试<br />
	/// <b>[Authorization]</b> Omron's HostLink virtual server supports DM area, CIO area, Work area, Hold area, and Auxiliary area, which can be easily tested
	/// </summary>
	/// <remarks>
	/// 支持TCP的接口以及串口，方便客户端进行测试，或是开发用于教学的虚拟服务器对象
	/// </remarks>
	public class OmronHostLinkServer : OmronFinsServer
	{
		/// <inheritdoc cref="OmronFinsServer.OmronFinsServer()"/>
		public OmronHostLinkServer( )
		{

		}

		/// <inheritdoc cref="OmronHostLink.UnitNumber"/>
		public byte UnitNumber { get; set; }

		#region NetServer Override

		/// <inheritdoc/>
		protected override void ThreadPoolLoginAfterClientCheck( Socket socket, System.Net.IPEndPoint endPoint )
		{
			AppSession appSession = new AppSession( );
			appSession.IpEndPoint = endPoint;
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

#if NET20 || NET35
		private void SocketAsyncCallBack( IAsyncResult ar )
#else
		private async void SocketAsyncCallBack( IAsyncResult ar )
#endif
		{
			if (ar.AsyncState is AppSession session)
			{
				try
				{
					int receiveCount = session.WorkSocket.EndReceive( ar );
#if NET20 || NET35
					OperateResult<byte[]> read1 = ReceiveCommandLineFromSocket( session.WorkSocket, 0x0d, 5000 );
#else
					OperateResult<byte[]> read1 = await ReceiveCommandLineFromSocketAsync( session.WorkSocket, 0x0d, 5000 );
#endif
					if (!read1.IsSuccess) { RemoveClient( session ); return; };

					if (!Authorization.asdniasnfaksndiqwhawfskhfaiw( )) { RemoveClient( session ); return; };

					LogNet?.WriteDebug( ToString( ), $"[{session.IpEndPoint}] Tcp {StringResources.Language.Receive}：{SoftBasic.GetAsciiStringRender( read1.Content )}" );

					string hexFinsCore = Encoding.ASCII.GetString( read1.Content, 14, read1.Content.Length - 18 );
					byte[] back = ReadFromFinsCore( SoftBasic.HexStringToBytes( hexFinsCore ) );
					if (back != null)
					{
						session.WorkSocket.Send( back );
						LogNet?.WriteDebug( ToString( ), $"[{session.IpEndPoint}] Tcp {StringResources.Language.Send}：{SoftBasic.GetAsciiStringRender( back )}" );
					}
					else
					{
						RemoveClient( session );
						return;
					}

					session.HeartTime = DateTime.Now;
					RaiseDataReceived( session, read1.Content );
					session.WorkSocket.BeginReceive( new byte[0], 0, 0, SocketFlags.None, new AsyncCallback( SocketAsyncCallBack ), session );
				}
				catch
				{
					RemoveClient( session );
				}
			}
		}

		/// <inheritdoc/>
		protected override byte[] PackCommand( int status, byte[] finsCore, byte[] data )
		{
			if (data == null) data = new byte[0];
			data = SoftBasic.BytesToAsciiBytes( data );

			byte[] back = new byte[27 + data.Length];
			Encoding.ASCII.GetBytes( "@00FA0040000000" ).CopyTo( back, 0 );
			Encoding.ASCII.GetBytes( UnitNumber.ToString( "X2" ) ).CopyTo( back, 1 );

			if (data.Length > 0) data.CopyTo( back, 23 );
			Encoding.ASCII.GetBytes( finsCore.SelectBegin( 2 ).ToHexString( ) ).CopyTo( back, 15 );
			Encoding.ASCII.GetBytes( status.ToString( "X4" ) ).CopyTo( back, 19 );
			// 计算FCS
			int tmp = back[0];
			for (int i = 1; i < back.Length - 4; i++)
			{
				tmp ^= back[i];
			}
			SoftBasic.BuildAsciiBytesFrom( (byte)tmp ).CopyTo( back, back.Length - 4 );
			back[back.Length - 2] = (byte)'*';
			back[back.Length - 1] = 0x0D;
			return back;
		}

		#endregion


		#region Serial Support

		/// <inheritdoc/>
		protected override bool CheckSerialReceiveDataComplete( byte[] buffer, int receivedLength )
		{
			if (receivedLength > 1) return buffer[receivedLength - 1] == 0x0D;
			return false;
		}

		/// <inheritdoc/>
		protected override OperateResult<byte[]> DealWithSerialReceivedData( byte[] data )
		{
			if (data.Length < 22) return new OperateResult<byte[]>( $"[{GetSerialPort().PortName}] Uknown Data：{data.ToHexString( ' ' )}" );

			LogNet?.WriteDebug( ToString( ), $"[{GetSerialPort( ).PortName}] {StringResources.Language.Receive}：{SoftBasic.GetAsciiStringRender( data )}" );

			string hexFinsCore = Encoding.ASCII.GetString( data, 14, data.Length - 18 );
			byte[] back = ReadFromFinsCore( SoftBasic.HexStringToBytes( hexFinsCore ) );

			LogNet?.WriteDebug( ToString( ), $"[{GetSerialPort( ).PortName}] {StringResources.Language.Send}：{SoftBasic.GetAsciiStringRender( back )}" );
			return OperateResult.CreateSuccessResult( back );
		}

		#endregion

		#region Object Override

		/// <inheritdoc/>
		public override string ToString( ) => $"OmronHostLinkServer[{Port}]";

		#endregion
	}
}
