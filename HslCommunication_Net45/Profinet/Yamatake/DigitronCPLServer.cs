using HslCommunication.BasicFramework;
using HslCommunication.Core.Net;
using HslCommunication.Core;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Net.Sockets;
using System.Text;
#if !NET20 && !NET35
using System.Threading.Tasks;
#endif

namespace HslCommunication.Profinet.Yamatake
{
	/// <summary>
	/// <b>[商业授权]</b> 山武的数字指示调节器的虚拟设备，支持和HSL本身进行数据通信测试<br />
	/// <b>[Authorization]</b> Yamatake’s digital indicating regulator is a virtual device that supports data communication testing with HSL itself
	/// </summary>
	public class DigitronCPLServer : NetworkDataServerBase
	{
		/// <summary>
		/// 实例化一个默认的对象<br />
		/// Instantiate a default object
		/// </summary>
		public DigitronCPLServer( )
		{
			softBuffer    = new SoftBuffer( DataPoolLength * 2 );
			ByteTransform = new RegularByteTransform( );
			Station       = 1;
		}

		/// <summary>
		/// 获取或设置当前虚拟仪表的站号信息，如果站号不一致，将不予访问<br />
		/// Get or set the station number information of the current virtual instrument. If the station number is inconsistent, it will not be accessed
		/// </summary>
		public byte Station { get; set; }

		#region Read Write

		/// <inheritdoc/>
		public override OperateResult<byte[]> Read( string address, ushort length )
		{
			try
			{
				ushort add = ushort.Parse( address );
				return OperateResult.CreateSuccessResult( softBuffer.GetBytes( add * 2, length * 2 ) );
			}
			catch( Exception ex)
			{
				return new OperateResult<byte[]>( "Read Failed: " + ex.Message );
			}
		}

		/// <inheritdoc/>
		public override OperateResult Write( string address, byte[] value )
		{
			try
			{
				ushort add = ushort.Parse( address );
				softBuffer.SetBytes( value, add * 2 );
				return OperateResult.CreateSuccessResult( );
			}
			catch (Exception ex)
			{
				return new OperateResult( "Write Failed: " + ex.Message );
			}
		}

		#endregion


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
					OperateResult<byte[]> read1 = ReceiveCommandLineFromSocket( session.WorkSocket, 0x0A, 5000 );
#else
					OperateResult<byte[]> read1 = await ReceiveCommandLineFromSocketAsync( session.WorkSocket, 0x0A, 5000 );
#endif
					if (!read1.IsSuccess) { RemoveClient( session ); return; };

					if (!Authorization.asdniasnfaksndiqwhawfskhfaiw( )) { RemoveClient( session ); return; };

					LogNet?.WriteDebug( ToString( ), $"[{session.IpEndPoint}] Tcp {StringResources.Language.Receive}：{SoftBasic.GetAsciiStringRender( read1.Content )}" );

					byte[] back = ReadFromCore( read1.Content );
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

		private byte[] ReadFromCore( byte[] command )
		{
			try
			{
				int endIndex = 9;
				for (int i = 9; i < command.Length; i++)
				{
					if (command[i] == 0x03)
					{
						endIndex = i;
						break;
					}
				}

				byte station = Convert.ToByte( Encoding.ASCII.GetString( command, 1, 2 ), 16 );
				if(station != this.Station) return Helper.DigitronCPLHelper.PackResponseContent( this.Station, 40, null, 0x57 );

				string[] datas = Encoding.ASCII.GetString( command, 9, endIndex - 9 ).Split( new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries );
				string cmd = Encoding.ASCII.GetString( command, 6, 2 );
				int address = int.Parse( datas[0].Substring( 0, datas[0].Length - 1 ) );
				byte dataType = datas[0].EndsWith( "W" ) ? (byte)0x57 : (byte)0x53;

				if (address >= 65536 || address < 0) return Helper.DigitronCPLHelper.PackResponseContent( this.Station, 42, null, dataType );
				
				if (cmd == "RS")
				{
					int length = int.Parse( datas[1] );
					if ((address + length) > 65535 ) return Helper.DigitronCPLHelper.PackResponseContent( this.Station, 42, null, dataType );

					if (length > 16) return Helper.DigitronCPLHelper.PackResponseContent( this.Station, 41, null, dataType );
					return Helper.DigitronCPLHelper.PackResponseContent( this.Station, 0,
						softBuffer.GetBytes( address * 2, length * 2 ), dataType );
				}
				else if (cmd == "WS")
				{
					if (!EnableWrite) return Helper.DigitronCPLHelper.PackResponseContent( this.Station, 46, null, dataType );

					if (datas.Length > 17) return Helper.DigitronCPLHelper.PackResponseContent( this.Station, 41, null, dataType );
					byte[] buffer = new byte[(datas.Length - 1) * 2];
					for (int i = 1; i < datas.Length; i++)
					{
						if (dataType == 0x57)
							BitConverter.GetBytes( short.Parse( datas[i] ) ).CopyTo( buffer, i * 2 - 2 );
						else
							BitConverter.GetBytes( ushort.Parse( datas[i] ) ).CopyTo( buffer, i * 2 - 2 );
					}
					softBuffer.SetBytes( buffer, address * 2 );
					return Helper.DigitronCPLHelper.PackResponseContent( this.Station, 0, null, dataType );
				}
				else
					return Helper.DigitronCPLHelper.PackResponseContent( this.Station, 40, null, dataType );
			}
			catch
			{
				return null;
			}
		}

		#endregion

		#region Serial Support

		/// <inheritdoc/>
		protected override bool CheckSerialReceiveDataComplete( byte[] buffer, int receivedLength )
		{
			if (receivedLength > 5) return buffer[receivedLength - 2] == 0x0D && buffer[receivedLength - 1] == 0x0A;
			return base.CheckSerialReceiveDataComplete( buffer, receivedLength );
		}

		/// <inheritdoc/>
		protected override OperateResult<byte[]> DealWithSerialReceivedData( byte[] data )
		{
			LogNet?.WriteDebug( ToString( ), $"[{GetSerialPort().PortName}] {StringResources.Language.Receive}：{SoftBasic.GetAsciiStringRender( data )}" );

			byte[] back = ReadFromCore( data );

			LogNet?.WriteDebug( ToString( ), $"[{GetSerialPort( ).PortName}] {StringResources.Language.Send}：{SoftBasic.GetAsciiStringRender( back )}" );

			return OperateResult.CreateSuccessResult( back );
		}

		#endregion

		#region Private Member

		private SoftBuffer softBuffer;                 // 输入寄存器的数据池
		private const int DataPoolLength = 65536;      // 数据的长度

		#endregion

		#region Object Override

		/// <inheritdoc/>
		public override string ToString( ) => $"DigitronCPLServer[{Port}]";

		#endregion
	}
}
