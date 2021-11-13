using HslCommunication.BasicFramework;
using HslCommunication.Core;
using HslCommunication.Core.Net;
using HslCommunication.Reflection;
using HslCommunication.Profinet.Beckhoff;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using HslCommunication.Core.IMessage;
#if !NET20 && !NET35
using System.Threading.Tasks;
#endif

namespace HslCommunication.Profinet.Beckhoff
{
	/// <summary>
	/// 倍福Ads协议的虚拟服务器
	/// </summary>
	public class BeckhoffAdsServer : NetworkDataServerBase
	{
		#region Constructor

		/// <summary>
		/// 实例化一个基于ADS协议的虚拟的倍福PLC对象，可以用来和<see cref="BeckhoffAdsNet"/>进行通信测试。
		/// </summary>
		public BeckhoffAdsServer( )
		{
			mBuffer = new SoftBuffer( DataPoolLength );
			iBuffer = new SoftBuffer( DataPoolLength );
			qBuffer = new SoftBuffer( DataPoolLength );

			ByteTransform = new RegularByteTransform( );
			WordLength = 2;
		}

		#endregion

		#region Data Persistence

		/// <inheritdoc/>
		protected override byte[] SaveToBytes( )
		{
			byte[] buffer = new byte[DataPoolLength * 3];
			mBuffer.GetBytes( ).CopyTo( buffer, DataPoolLength * 0 );
			iBuffer.GetBytes( ).CopyTo( buffer, DataPoolLength * 1 );
			qBuffer.GetBytes( ).CopyTo( buffer, DataPoolLength * 2 );
			return buffer;
		}

		/// <inheritdoc/>
		protected override void LoadFromBytes( byte[] content )
		{
			if (content.Length < DataPoolLength * 3) throw new Exception( "File is not correct" );
			mBuffer.SetBytes( content, 0 * DataPoolLength, DataPoolLength * 1 );
			iBuffer.SetBytes( content, 1 * DataPoolLength, DataPoolLength * 1 );
			qBuffer.SetBytes( content, 2 * DataPoolLength, DataPoolLength * 1 );
		}

		#endregion

		#region NetworkDataServerBase Override

		/// <inheritdoc cref="BeckhoffAdsNet.Read(string, ushort)"/>
		[HslMqttApi( "ReadByteArray", "" )]
		public override OperateResult<byte[]> Read( string address, ushort length )
		{
			OperateResult<uint, uint> analysis = BeckhoffAdsNet.AnalysisAddress( address, false );
			if (!analysis.IsSuccess) return analysis.ConvertFailed<byte[]>( );

			switch (analysis.Content1)
			{
				case 0x4020: return OperateResult.CreateSuccessResult( mBuffer.GetBytes( (int)analysis.Content2, length ) );
				case 0xF020: return OperateResult.CreateSuccessResult( iBuffer.GetBytes( (int)analysis.Content2, length ) );
				case 0xF030: return OperateResult.CreateSuccessResult( qBuffer.GetBytes( (int)analysis.Content2, length ) );
			}
			return new OperateResult<byte[]>( StringResources.Language.NotSupportedDataType );
		}

		/// <inheritdoc cref="BeckhoffAdsNet.Write(string, byte[])"/>
		[HslMqttApi( "WriteByteArray", "" )]
		public override OperateResult Write( string address, byte[] value )
		{
			OperateResult<uint, uint> analysis = BeckhoffAdsNet.AnalysisAddress( address, false );
			if (!analysis.IsSuccess) return analysis.ConvertFailed<byte[]>( );

			switch (analysis.Content1)
			{
				case 0x4020: mBuffer.SetBytes( value, (int)analysis.Content2 ); break;
				case 0xF020: iBuffer.SetBytes( value, (int)analysis.Content2 ); break;
				case 0xF030: qBuffer.SetBytes( value, (int)analysis.Content2 ); break;
				default: return new OperateResult<byte[]>( StringResources.Language.NotSupportedDataType );
			}
			return OperateResult.CreateSuccessResult( );
		}

		/// <inheritdoc cref="IReadWriteNet.ReadBool(string, ushort)"/>
		[HslMqttApi( "ReadBoolArray", "" )]
		public override OperateResult<bool[]> ReadBool( string address, ushort length )
		{
			OperateResult<uint, uint> analysis = BeckhoffAdsNet.AnalysisAddress( address, true );
			if (!analysis.IsSuccess) return analysis.ConvertFailed<bool[]>( );

			switch (analysis.Content1)
			{
				case 0x4021: return OperateResult.CreateSuccessResult( mBuffer.GetBool( (int)analysis.Content2, length ) );
				case 0xF021: return OperateResult.CreateSuccessResult( iBuffer.GetBool( (int)analysis.Content2, length ) );
				case 0xF031: return OperateResult.CreateSuccessResult( qBuffer.GetBool( (int)analysis.Content2, length ) );
			}
			return new OperateResult<bool[]>( StringResources.Language.NotSupportedDataType );
		}

		/// <inheritdoc cref="IReadWriteNet.Write(string, bool[])"/>
		[HslMqttApi( "WriteBoolArray", "" )]
		public override OperateResult Write( string address, bool[] value )
		{
			OperateResult<uint, uint> analysis = BeckhoffAdsNet.AnalysisAddress( address, true );
			if (!analysis.IsSuccess) return analysis.ConvertFailed<bool[]>( );

			switch (analysis.Content1)
			{
				case 0x4021: mBuffer.SetBool( value, (int)analysis.Content2 ); break;
				case 0xF021: iBuffer.SetBool( value, (int)analysis.Content2 ); break;
				case 0xF031: qBuffer.SetBool( value, (int)analysis.Content2 ); break;
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
				OperateResult<byte[]> read1 = ReceiveByMessage( session.WorkSocket, 2000, new AdsNetMessage( ) );
#else
				OperateResult<byte[]> read1 = await ReceiveByMessageAsync( session.WorkSocket, 2000, new AdsNetMessage( ) );
#endif
				if (!read1.IsSuccess) { RemoveClient( session ); return; };

				if (!Authorization.asdniasnfaksndiqwhawfskhfaiw( )) { RemoveClient( session ); return; };

				LogNet?.WriteDebug( ToString( ), $"[{session.IpEndPoint}] Tcp {StringResources.Language.Receive}：{read1.Content.ToHexString( ' ' )}" );

				byte[] back = ReadFromAdsCore( read1.Content );

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

		private byte[] PackCommand( byte[] cmd, int err, byte[] data )
		{
			if (data == null) data = new byte[0];
			byte[] buffer = new byte[32 + data.Length];
			Array.Copy( cmd, 0, buffer, 0, 32 );
			byte[] amsTarget = buffer.SelectBegin( 8 );
			byte[] amsSource = buffer.SelectMiddle( 8, 8 );

			amsTarget.CopyTo( buffer, 8 );
			amsSource.CopyTo( buffer, 0 );
			buffer[18] = 0x05;
			buffer[19] = 0x00;
			BitConverter.GetBytes( data.Length ).CopyTo( buffer, 20 );
			BitConverter.GetBytes( err ).CopyTo( buffer, 24 );
			buffer[11] = 0x00;
			if (data.Length > 0) data.CopyTo( buffer, 32 );
			return BeckhoffAdsNet.PackAmsTcpHelper( AmsTcpHeaderFlags.Command, buffer );
		}

		private byte[] PackDataResponse( int err, byte[] data )
		{
			if (data != null)
			{
				byte[] buffer = new byte[8 + data.Length];
				BitConverter.GetBytes( err ).CopyTo( buffer, 0 );
				BitConverter.GetBytes( data.Length ).CopyTo( buffer, 4 );
				if (data.Length > 0) data.CopyTo( buffer, 8 );
				return buffer;
			}
			else
			{
				return BitConverter.GetBytes( err );
			}
		}

		private byte[] ReadFromAdsCore( byte[] receive )
		{
			AmsTcpHeaderFlags ams = (AmsTcpHeaderFlags)BitConverter.ToUInt16( receive, 0 );
			if(ams == AmsTcpHeaderFlags.Command)
			{
				receive = receive.RemoveBegin( 6 );
				LogNet?.WriteDebug( $"TargetId:{BeckhoffAdsNet.GetAmsNetIdString( receive, 0 )} SenderId:{BeckhoffAdsNet.GetAmsNetIdString( receive, 8 )}" );
				short commandId = BitConverter.ToInt16( receive, 16 );
				if (commandId == 0x02) return ReadByCommand( receive );
				if (commandId == 0x03) return WriteByCommand( receive );
				if (commandId == 0x09) return ReadWriteByCommand( receive );
				return PackCommand( receive, 0x20, null );
			}
			else if (ams == AmsTcpHeaderFlags.GetLocalNetId)
			{
				return BeckhoffAdsNet.PackAmsTcpHelper( AmsTcpHeaderFlags.GetLocalNetId, BeckhoffAdsNet.StrToAMSNetId( "192.168.163.8.1.1" ) );
			}
			else if (ams == AmsTcpHeaderFlags.PortConnect)
			{
				return BeckhoffAdsNet.PackAmsTcpHelper( AmsTcpHeaderFlags.PortConnect, BeckhoffAdsNet.StrToAMSNetId( "192.168.163.8.1.1:32957" ) );
			}
			return null;
		}

		private byte[] ReadByCommand( byte[] command )
		{
			try
			{
				int indexGroup = BitConverter.ToInt32( command, 32 );
				int address    = BitConverter.ToInt32( command, 36 );
				int length     = BitConverter.ToInt32( command, 40 );

				switch (indexGroup)
				{
					case 0x4020: return PackCommand( command, 0x00, PackDataResponse( 0, mBuffer.GetBytes( address, length ) ) );
					case 0xF020: return PackCommand( command, 0x00, PackDataResponse( 0, iBuffer.GetBytes( address, length ) ) );
					case 0xF030: return PackCommand( command, 0x00, PackDataResponse( 0, qBuffer.GetBytes( address, length ) ) );
					case 0x4021: return PackCommand( command, 0x00, PackDataResponse( 0, mBuffer.GetBool( address, length ).ToByteArray( ) ) );
					case 0xF021: return PackCommand( command, 0x00, PackDataResponse( 0, iBuffer.GetBool( address, length ).ToByteArray( ) ) );
					case 0xF031: return PackCommand( command, 0x00, PackDataResponse( 0, qBuffer.GetBool( address, length ).ToByteArray( ) ) );
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
				int indexGroup = BitConverter.ToInt32( command, 32 );
				int address    = BitConverter.ToInt32( command, 36 );
				int length     = BitConverter.ToInt32( command, 40 );
				byte[] data = command.RemoveBegin( 44 );

				switch (indexGroup)
				{
					case 0x4020: mBuffer.SetBytes( data, address ); return PackCommand( command, 0x00, PackDataResponse( 0, null ) );
					case 0xF020: iBuffer.SetBytes( data, address ); return PackCommand( command, 0x00, PackDataResponse( 0, null ) );
					case 0xF030: qBuffer.SetBytes( data, address ); return PackCommand( command, 0x00, PackDataResponse( 0, null ) );
					case 0x4021: mBuffer.SetBytes( data, address ); return PackCommand( command, 0x00, PackDataResponse( 0, null ) );
					case 0xF021: iBuffer.SetBytes( data, address ); return PackCommand( command, 0x00, PackDataResponse( 0, null ) );
					case 0xF031: qBuffer.SetBytes( data, address ); return PackCommand( command, 0x00, PackDataResponse( 0, null ) );
				}

				return PackCommand( command, 0x40, null );
			}
			catch
			{
				return PackCommand( command, 0xA4, null );
			}
		}

		private byte[] ReadWriteByCommand( byte[] command )
		{
			try
			{ 
				int indexGroup  = BitConverter.ToInt32( command, 32 );
				int address     = BitConverter.ToInt32( command, 36 );
				int readLength  = BitConverter.ToInt32( command, 40 );
				int writeLength = BitConverter.ToInt32( command, 44 );
				byte[] data = command.RemoveBegin( 48 );

				switch (indexGroup)
				{
					case 0x4020: mBuffer.SetBytes( data, address ); return PackCommand( command, 0x00, PackDataResponse( 0, null ) );
					case 0xF020: iBuffer.SetBytes( data, address ); return PackCommand( command, 0x00, PackDataResponse( 0, null ) );
					case 0xF030: qBuffer.SetBytes( data, address ); return PackCommand( command, 0x00, PackDataResponse( 0, null ) );
					case 0x4021: mBuffer.SetBytes( data, address ); return PackCommand( command, 0x00, PackDataResponse( 0, null ) );
					case 0xF021: iBuffer.SetBytes( data, address ); return PackCommand( command, 0x00, PackDataResponse( 0, null ) );
					case 0xF031: qBuffer.SetBytes( data, address ); return PackCommand( command, 0x00, PackDataResponse( 0, null ) );
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
				mBuffer.Dispose( );
				iBuffer.Dispose( );
				qBuffer.Dispose( );
			}
			base.Dispose( disposing );
		}

		#endregion

		#region Object Override

		/// <inheritdoc/>
		public override string ToString( ) => $"BeckhoffAdsServer[{Port}]";

		#endregion

		#region Private Member

		private SoftBuffer mBuffer;       // 寄存器的数据池
		private SoftBuffer iBuffer;       // 寄存器的数据池
		private SoftBuffer qBuffer;       // 寄存器的数据池

		private const int DataPoolLength = 65536;     // 数据的长度

		#endregion
	}
}
