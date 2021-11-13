using HslCommunication.BasicFramework;
using HslCommunication.Core;
using HslCommunication.Core.Address;
using HslCommunication.Core.IMessage;
using HslCommunication.Core.Net;
using HslCommunication.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
#if !NET20 && !NET35
using System.Threading.Tasks;
#endif

namespace HslCommunication.Profinet.Fuji
{
	/// <summary>
	/// <b>[商业授权]</b> 富士的SPH虚拟的PLC，支持M1.0，M3.0，M10.0，I0，Q0的位与字的读写操作。<br />
	/// </summary>
	public class FujiSPHServer : NetworkDataServerBase
	{
		#region Constructor

		/// <summary>
		/// 实例化一个基于SPH协议的虚拟的富士PLC对象，可以用来和<see cref="FujiSPHNet"/>进行通信测试。
		/// </summary>
		public FujiSPHServer( )
		{
			m1Buffer  = new SoftBuffer( DataPoolLength * 2);
			m3Buffer  = new SoftBuffer( DataPoolLength * 2);
			m10Buffer = new SoftBuffer( DataPoolLength * 2);
			iqBuffer  = new SoftBuffer( DataPoolLength * 2);

			ByteTransform = new RegularByteTransform( );
			WordLength = 1;
		}

		#endregion

		#region Data Persistence

		/// <inheritdoc/>
		protected override byte[] SaveToBytes( )
		{
			byte[] buffer = new byte[DataPoolLength * 8];
			m1Buffer  .GetBytes( ).CopyTo( buffer, DataPoolLength * 0 );
			m3Buffer  .GetBytes( ).CopyTo( buffer, DataPoolLength * 2 );
			m10Buffer .GetBytes( ).CopyTo( buffer, DataPoolLength * 4 );
			iqBuffer  .GetBytes( ).CopyTo( buffer, DataPoolLength * 6 );
			return buffer;
		}

		/// <inheritdoc/>
		protected override void LoadFromBytes( byte[] content )
		{
			if (content.Length < DataPoolLength * 8) throw new Exception( "File is not correct" );
			m1Buffer  .SetBytes( content, 0 * DataPoolLength, DataPoolLength * 2 );
			m3Buffer  .SetBytes( content, 2 * DataPoolLength, DataPoolLength * 2 );
			m10Buffer .SetBytes( content, 4 * DataPoolLength, DataPoolLength * 2 );
			iqBuffer  .SetBytes( content, 6 * DataPoolLength, DataPoolLength * 2 );
		}

		#endregion

		#region NetworkDataServerBase Override

		/// <inheritdoc cref="FujiSPHNet.Read(string, ushort)"/>
		[HslMqttApi( "ReadByteArray", "" )]
		public override OperateResult<byte[]> Read( string address, ushort length )
		{
			OperateResult<FujiSPHAddress> analysis = FujiSPHAddress.ParseFrom( address );
			if (!analysis.IsSuccess) return analysis.ConvertFailed<byte[]>( );

			switch (analysis.Content.TypeCode)
			{
				case 0x02: return OperateResult.CreateSuccessResult( m1Buffer.GetBytes(  analysis.Content.AddressStart * 2, length * 2 ) );
				case 0x04: return OperateResult.CreateSuccessResult( m3Buffer.GetBytes(  analysis.Content.AddressStart * 2, length * 2 ) );
				case 0x08: return OperateResult.CreateSuccessResult( m10Buffer.GetBytes( analysis.Content.AddressStart * 2, length * 2 ) );
				case 0x01: return OperateResult.CreateSuccessResult( iqBuffer.GetBytes(  analysis.Content.AddressStart * 2, length * 2 ) );
			}
			return new OperateResult<byte[]>( StringResources.Language.NotSupportedDataType );
		}

		/// <inheritdoc cref="FujiSPHNet.Write(string, byte[])"/>
		[HslMqttApi( "WriteByteArray", "" )]
		public override OperateResult Write( string address, byte[] value )
		{
			OperateResult<FujiSPHAddress> analysis = FujiSPHAddress.ParseFrom( address );
			if (!analysis.IsSuccess) return analysis.ConvertFailed<byte[]>( );

			switch (analysis.Content.TypeCode)
			{
				case 0x02: m1Buffer.SetBytes(  value, analysis.Content.AddressStart * 2 ); break;
				case 0x04: m3Buffer.SetBytes(  value, analysis.Content.AddressStart * 2 ); break;
				case 0x08: m10Buffer.SetBytes( value, analysis.Content.AddressStart * 2 ); break;
				case 0x01: iqBuffer.SetBytes(  value, analysis.Content.AddressStart * 2 ); break;
				default: return new OperateResult<byte[]>( StringResources.Language.NotSupportedDataType );
			}
			return OperateResult.CreateSuccessResult( );
		}

		/// <inheritdoc cref="IReadWriteNet.ReadBool(string, ushort)"/>
		[HslMqttApi( "ReadBoolArray", "" )]
		public override OperateResult<bool[]> ReadBool( string address, ushort length )
		{
			OperateResult<FujiSPHAddress> analysis = FujiSPHAddress.ParseFrom( address );
			if (!analysis.IsSuccess) return analysis.ConvertFailed<bool[]>( );

			int bitCount = analysis.Content.BitIndex + length;
			int wordLength = bitCount % 16 == 0 ? bitCount / 16 : bitCount / 16 + 1;

			OperateResult<byte[]> read = Read( address, (ushort)wordLength );
			if (!read.IsSuccess) return read.ConvertFailed<bool[]>( );

			return OperateResult.CreateSuccessResult( read.Content.ToBoolArray( ).SelectMiddle( analysis.Content.BitIndex, length ) );
		}

		/// <inheritdoc cref="IReadWriteNet.Write(string, bool[])"/>
		[HslMqttApi( "WriteBoolArray", "" )]
		public override OperateResult Write( string address, bool[] value )
		{
			OperateResult<FujiSPHAddress> analysis = FujiSPHAddress.ParseFrom( address );
			if (!analysis.IsSuccess) return analysis.ConvertFailed<bool[]>( );
			
			switch (analysis.Content.TypeCode)
			{
				case 0x02: m1Buffer.SetBool(  value, analysis.Content.AddressStart * 16 + analysis.Content.BitIndex ); break;
				case 0x04: m3Buffer.SetBool(  value, analysis.Content.AddressStart * 16 + analysis.Content.BitIndex ); break;
				case 0x08: m10Buffer.SetBool( value, analysis.Content.AddressStart * 16 + analysis.Content.BitIndex ); break;
				case 0x01: iqBuffer.SetBool(  value, analysis.Content.AddressStart * 16 + analysis.Content.BitIndex ); break;
				default: return new OperateResult( StringResources.Language.NotSupportedDataType );
			}
			return OperateResult.CreateSuccessResult( );
		}

		#endregion

		#region NetServer Override

		/// <inheritdoc/>
		protected override void ThreadPoolLoginAfterClientCheck( Socket socket, System.Net.IPEndPoint endPoint )
		{
			// 开始接收数据信息
			AppSession appSession = new AppSession( );
			appSession.IpEndPoint = endPoint;
			appSession.WorkSocket = socket;

			if (socket.BeginReceiveResult( SocketAsyncCallBack, appSession ).IsSuccess)
				AddClient( appSession );
			else
				LogNet?.WriteDebug( ToString( ), string.Format( StringResources.Language.ClientOfflineInfo, endPoint ) );

		}

#if NET20 || NET35
		private void SocketAsyncCallBack( IAsyncResult ar )
#else
		private async void SocketAsyncCallBack( IAsyncResult ar )
#endif
		{
			if (ar.AsyncState is AppSession session)
			{
				if (!session.WorkSocket.EndReceiveResult( ar ).IsSuccess) { RemoveClient( session ); return; }
#if NET20 || NET35
				OperateResult<byte[]> read1 = ReceiveByMessage( session.WorkSocket, 2000, new FujiSPHMessage( ) );
#else
				OperateResult<byte[]> read1 = await ReceiveByMessageAsync( session.WorkSocket, 2000, new FujiSPHMessage( ) );
#endif
				if (!read1.IsSuccess) { RemoveClient( session ); return; };

				if (!Authorization.asdniasnfaksndiqwhawfskhfaiw( )) { RemoveClient( session ); return; };

				if (read1.Content[0] != 0xFB || read1.Content[1] != 0x80) { RemoveClient( session ); return; }

				LogNet?.WriteDebug( ToString( ), $"[{session.IpEndPoint}] Tcp {StringResources.Language.Receive}：{read1.Content.ToHexString( ' ' )}" );

				byte[] back = ReadFromSPBCore( read1.Content );

				if (back == null) { RemoveClient( session ); return; }
				if (!Send( session.WorkSocket, back ).IsSuccess) { RemoveClient( session ); return; }

				LogNet?.WriteDebug( ToString( ), $"[{session.IpEndPoint}] Tcp {StringResources.Language.Send}：{back.ToHexString( ' ' )}" );

				session.HeartTime = DateTime.Now;
				RaiseDataReceived( session, read1.Content );
				if (!session.WorkSocket.BeginReceiveResult( SocketAsyncCallBack, session ).IsSuccess) RemoveClient( session );
			}
		}

		#endregion

		#region Core Read

		private byte[] PackCommand( byte[] cmd, byte err, byte[] data )
		{
			if (data == null) data = new byte[0];
			byte[] buffer = new byte[26 + data.Length];
			buffer[ 0] = 0xFB;
			buffer[ 1] = 0x80;
			buffer[ 2] = 0x80;
			buffer[ 3] = 0x00;
			buffer[ 4] = err;
			buffer[ 5] = 0x7B;
			buffer[ 6] = cmd[6];       // connection id
			buffer[ 7] = 0x00;
			buffer[ 8] = 0x11;
			buffer[ 9] = 0x00;
			buffer[10] = 0x00;
			buffer[11] = 0x00;
			buffer[12] = 0x00;
			buffer[13] = 0x00;
			buffer[14] = cmd[14];       // command
			buffer[15] = cmd[15];       // mode
			buffer[16] = 0x00;
			buffer[17] = 0x01;
			buffer[18] = BitConverter.GetBytes( data.Length + 6 )[0];  // length
			buffer[19] = BitConverter.GetBytes( data.Length + 6 )[1];
			Array.Copy( cmd, 20, buffer, 20, 6 );
			if (data.Length > 0) data.CopyTo( buffer, 26 );
			return buffer;
		}

		private byte[] ReadFromSPBCore( byte[] receive )
		{
			if (receive.Length < 20) return PackCommand( receive, 0x10, null );
			if (receive[14] == 0x00 && receive[15] == 0x00) return ReadByCommand( receive );
			else if (receive[14] == 0x01 && receive[15] == 0x00) return WriteByCommand( receive );
			return PackCommand( receive, 0x20, null );
		}

		private byte[] ReadByCommand( byte[] command )
		{
			try
			{
				byte typeCode = command[20];
				int address = command[23] * 256 * 256 + command[22] * 256 + command[21];
				int length = command[25] * 256 + command[24];

				if (address + length > ushort.MaxValue) return PackCommand( command, 0x45, null );
				switch (typeCode)
				{
					case 0x02: return PackCommand( command, 0x00, m1Buffer.GetBytes(  address * 2, length * 2 ) );
					case 0x04: return PackCommand( command, 0x00, m3Buffer.GetBytes(  address * 2, length * 2 ) );
					case 0x08: return PackCommand( command, 0x00, m10Buffer.GetBytes( address * 2, length * 2 ) );
					case 0x01: return PackCommand( command, 0x00, iqBuffer.GetBytes(  address * 2, length * 2 ) );
				}

				return PackCommand( command, 0x40, null );
			}
			catch
			{
				return PackCommand( command, 0xA4, null );
			}
		}

		private byte[] WriteByCommand( byte[] command )
		{
			// 先判断是否有写入的权利，没有的话，直接返回写入异常
			if (!this.EnableWrite) return PackCommand( command, 0x10, null );

			try
			{
				byte typeCode = command[20];
				int address = command[23] * 256 * 256 + command[22] * 256 + command[21];
				int length = command[25] * 256 + command[24];
				byte[] value = command.RemoveBegin( 26 );

				// 对地址的长度，以及写入的长度信息进行检查
				if (address + length > ushort.MaxValue) return PackCommand( command, 0x45, null );
				if (length * 2 != value.Length) return PackCommand( command, 0x45, null );
				switch (typeCode)
				{
					case 0x02: m1Buffer.SetBytes(  value, address * 2 ); return PackCommand( command, 0x00, null );
					case 0x04: m3Buffer.SetBytes(  value, address * 2 ); return PackCommand( command, 0x00, null );
					case 0x08: m10Buffer.SetBytes( value, address * 2 ); return PackCommand( command, 0x00, null );
					case 0x01: iqBuffer.SetBytes(  value, address * 2 ); return PackCommand( command, 0x00, null );
				}

				return PackCommand( command, 0x40, null );
			}
			catch
			{
				return PackCommand( command, 0xA4, null );
			}
		}

		#endregion

		#region IDisposable Support

		/// <inheritdoc/>
		protected override void Dispose( bool disposing )
		{
			if (disposing)
			{
				m1Buffer.Dispose( );
				m3Buffer.Dispose( );
				m10Buffer.Dispose( );
				iqBuffer.Dispose( );
			}
			base.Dispose( disposing );
		}

		#endregion

		#region Object Override

		/// <inheritdoc/>
		public override string ToString( ) => $"FujiSPHServer[{Port}]";

		#endregion

		#region Private Member

		private SoftBuffer m1Buffer;       // 寄存器的数据池
		private SoftBuffer m3Buffer;        // 寄存器的数据池
		private SoftBuffer m10Buffer;       // 寄存器的数据池
		private SoftBuffer iqBuffer;       // 寄存器的数据池

		private const int DataPoolLength = 65536;     // 数据的长度

		#endregion
	}
}
