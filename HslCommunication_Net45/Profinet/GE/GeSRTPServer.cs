using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HslCommunication;
using HslCommunication.Core;
using HslCommunication.Core.Net;
using HslCommunication.Core.Address;
using HslCommunication.BasicFramework;
using HslCommunication.Reflection;
using System.Net.Sockets;
using HslCommunication.Core.IMessage;
#if !NET20 && !NET35
using System.Threading.Tasks;
#endif

namespace HslCommunication.Profinet.GE
{
	/// <summary>
	/// <b>[商业授权]</b> Ge的SRTP协议实现的虚拟PLC，支持I,Q,M,T,SA,SB,SC,S,G的位和字节读写，支持AI,AQ,R的字读写操作，支持读取当前时间及程序名称。<br />
	/// <b>[Authorization]</b> Virtual PLC implemented by Ge's SRTP protocol, supports bit and byte read and write of I, Q, M, T, SA, SB, SC, S, G, 
	/// supports word read and write operations of AI, AQ, R, and supports reading Current time and program name.
	/// </summary>
	/// <remarks>
	/// 实例化之后，直接调用 <see cref="NetworkServerBase.ServerStart(int)"/> 方法就可以通信及交互，所有的地址都是从1开始的，地址示例：M1,M100, R1，
	/// 具体的用法参考 HslCommunicationDemo 相关界面的源代码。
	/// </remarks>
	/// <example>
	/// 地址的示例，参考 <see cref="GeSRTPNet"/> 相关的示例说明
	/// </example>
	public class GeSRTPServer : NetworkDataServerBase
	{
		#region Constructor

		/// <summary>
		/// 实例化一个默认的对象<br />
		/// Instantiate a default object
		/// </summary>
		public GeSRTPServer( )
		{
			iBuffer  = new SoftBuffer( DataPoolLength );
			qBuffer  = new SoftBuffer( DataPoolLength );
			mBuffer  = new SoftBuffer( DataPoolLength );
			tBuffer  = new SoftBuffer( DataPoolLength );
			saBuffer = new SoftBuffer( DataPoolLength );
			sbBuffer = new SoftBuffer( DataPoolLength );
			scBuffer = new SoftBuffer( DataPoolLength );
			sBuffer  = new SoftBuffer( DataPoolLength );
			gBuffer  = new SoftBuffer( DataPoolLength );
			aiBuffer = new SoftBuffer( DataPoolLength * 2 );
			aqBuffer = new SoftBuffer( DataPoolLength * 2 );
			rBuffer  = new SoftBuffer( DataPoolLength * 2 );

			WordLength               = 2;
			ByteTransform            = new RegularByteTransform( );
		}

		#endregion

		#region NetworkDataServerBase Override

		/// <inheritdoc cref="GeSRTPNet.Read(string, ushort)"/>
		[HslMqttApi( "ReadByteArray", "" )]
		public override OperateResult<byte[]> Read( string address, ushort length )
		{
			OperateResult<GeSRTPAddress> analysis = GeSRTPAddress.ParseFrom( address, length, false );
			if (!analysis.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( analysis );

			return OperateResult.CreateSuccessResult( ReadByCommand( analysis.Content.DataCode, (ushort)analysis.Content.AddressStart, analysis.Content.Length ) );
		}

		/// <inheritdoc cref="GeSRTPNet.Write(string, byte[])"/>
		[HslMqttApi( "WriteByteArray", "" )]
		public override OperateResult Write( string address, byte[] value )
		{
			OperateResult<GeSRTPAddress> analysis = GeSRTPAddress.ParseFrom( address, (ushort)value.Length, false );
			if (!analysis.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( analysis );

			return WriteByCommand( analysis.Content.DataCode, (ushort)analysis.Content.AddressStart, analysis.Content.Length, value );
		}

		#endregion

		#region Bool Read Write Operate

		/// <inheritdoc cref="GeSRTPNet.ReadBool(string,ushort)"/>
		[HslMqttApi( "ReadBoolArray", "" )]
		public override OperateResult<bool[]> ReadBool(string address, ushort length )
		{
			OperateResult<GeSRTPAddress> analysis = GeSRTPAddress.ParseFrom( address, length, true );
			if (!analysis.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( analysis );

			return OperateResult.CreateSuccessResult( GetSoftBufferFromDataCode( analysis.Content.DataCode, out bool isBit ).GetBool( analysis.Content.AddressStart, length ) );
		}

		/// <inheritdoc cref="GeSRTPNet.Write(string, bool[])"/>
		[HslMqttApi( "WriteBoolArray", "" )]
		public override OperateResult Write(string address, bool[] value )
		{
			OperateResult<GeSRTPAddress> analysis = GeSRTPAddress.ParseFrom( address, (ushort)value.Length, true );
			if (!analysis.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( analysis );

			GetSoftBufferFromDataCode( analysis.Content.DataCode, out bool isBit ).SetBool( value, analysis.Content.AddressStart );
			return OperateResult.CreateSuccessResult( );
		}

		#endregion

		#region NetServer Override

		/// <inheritdoc/>
#if NET20 || NET35
		protected override void ThreadPoolLoginAfterClientCheck( Socket socket, System.Net.IPEndPoint endPoint )
#else
		protected async override void ThreadPoolLoginAfterClientCheck( Socket socket, System.Net.IPEndPoint endPoint )
#endif
		{
			// 先接收注册信息，返回注册结果
#if NET20 || NET35
			OperateResult<byte[]> read = ReceiveByMessage( socket, 5000, new GeSRTPMessage( ) );
#else
			OperateResult<byte[]> read = await ReceiveByMessageAsync( socket, 5000, new GeSRTPMessage( ) );
#endif
			if (!read.IsSuccess) { socket?.Close(); return; };

			byte[] back = new byte[56];
			back[0] = 0x01;
			back[8] = 0x0f;
			OperateResult send = Send( socket, back );
			if(!send.IsSuccess) { socket?.Close( ); return; };

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
					OperateResult<byte[]> read1 = ReceiveByMessage( session.WorkSocket, 5000, new GeSRTPMessage( ) );
#else
					OperateResult<byte[]> read1 = await ReceiveByMessageAsync( session.WorkSocket, 5000, new GeSRTPMessage( ) );
#endif
					if (!read1.IsSuccess) { RemoveClient( session ); return; };

					if (!Authorization.asdniasnfaksndiqwhawfskhfaiw( )) { RemoveClient( session ); return; };

					LogNet?.WriteDebug( ToString( ), $"[{session.IpEndPoint}] Tcp {StringResources.Language.Receive}：{read1.Content.ToHexString( ' ' )}" );

					byte[] receive = read1.Content;
					byte[] back = null;
					if      (receive[42] == 0x04) back = ReadByCommand(             receive );
					else if (receive[42] == 0x01) back = ReadProgramNameByCommand(  receive );
					else if (receive[42] == 0x25) back = ReadDateTimeByCommand(     receive );
					else if (receive[50] == 0x07) back = WriteByCommand(            receive );
					else back = null;

					if(back == null)
					{
						RemoveClient( session );
						return;
					}

					session.WorkSocket.Send( back );
					LogNet?.WriteDebug( ToString( ), $"[{session.IpEndPoint}] Tcp {StringResources.Language.Send}：{back.ToHexString( ' ' )}" );

					session.HeartTime = DateTime.Now;
					RaiseDataReceived( session, receive );
					session.WorkSocket.BeginReceive( new byte[0], 0, 0, SocketFlags.None, new AsyncCallback( SocketAsyncCallBack ), session );
				}
				catch
				{
					RemoveClient( session );
				}
			}
		}

		private SoftBuffer GetSoftBufferFromDataCode( byte code, out bool isBit )
		{
			switch (code)
			{
				case 0x46: isBit = true;  return iBuffer;
				case 0x10: isBit = false; return iBuffer;
				case 0x48: isBit = true;  return qBuffer;
				case 0x42: isBit = false; return qBuffer;
				case 0x4c: isBit = true;  return mBuffer;
				case 0x16: isBit = false; return mBuffer;
				case 0x4A: isBit = true;  return tBuffer;
				case 0x14: isBit = false; return tBuffer;
				case 0x4E: isBit = true;  return saBuffer;
				case 0x18: isBit = false; return saBuffer;
				case 0x50: isBit = true;  return sbBuffer;
				case 0x1A: isBit = false; return sbBuffer;
				case 0x52: isBit = true;  return scBuffer;
				case 0x1C: isBit = false; return scBuffer;
				case 0x54: isBit = true;  return tBuffer;
				case 0x1E: isBit = false; return tBuffer;
				case 0x56: isBit = true;  return gBuffer;
				case 0x38: isBit = false; return gBuffer;
				case 0x0A: isBit = false; return aiBuffer;
				case 0x0C: isBit = false; return aqBuffer;
				case 0x08: isBit = false; return rBuffer;
				default: isBit = false; return null;
			}
		}

		private byte[] ReadByCommand( byte dataCode, ushort address, ushort length )
		{
			SoftBuffer buffer = GetSoftBufferFromDataCode( dataCode, out bool isBit );
			if (buffer == null) return null;

			if (isBit)
			{
				HslHelper.CalculateStartBitIndexAndLength( address, length, out int newStart, out ushort byteLength, out int offset );
				return buffer.GetBytes( newStart / 8, byteLength );
			}
			else if(dataCode == 0x0A || dataCode == 0x0C || dataCode == 0x08)
			{
				return buffer.GetBytes( address * 2, length * 2 );
			}
			else
			{
				return buffer.GetBytes( address, length );
			}
		}

		private byte[] ReadByCommand( byte[] command )
		{
			byte[] read = ReadByCommand( command[43], BitConverter.ToUInt16( command, 44 ), BitConverter.ToUInt16( command, 46 ) );
			if (read == null) return null;

			if(read.Length < 7)
			{
				byte[] back = @"
03 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
00 01 00 00 00 00 00 00 00 00 00 00 00 00 06 d4
00 0e 00 00 00 60 01 a0 01 01 00 00 00 00 00 00
00 00 ff 02 03 00 5c 01".ToHexBytes( );
				read.CopyTo( back, 44 );
				command.SelectMiddle( 2, 2 ).CopyTo( back, 2 );
				return back;
			}
			else
			{
				byte[] back = new byte[56 + read.Length];
				@"03 00 03 00 00 00 00 00 00 00 00 00 00 00 00 00 00 01 00 00 00 00 00 00 00 00 00 00 00 00 06 94
				00 0e 00 00 00 60 01 a0 00 00 0c 00 00 18 00 00 01 01 ff 02 03 00 5c 01".ToHexBytes( ).CopyTo( back, 0 );
				command.SelectMiddle( 2, 2 ).CopyTo( back, 2 );
				read.CopyTo( back, 56 );
				BitConverter.GetBytes( (ushort)read.Length ).CopyTo( back, 4 );
				return back;
			}
		}

		private OperateResult WriteByCommand( byte dataCode, ushort address, ushort length, byte[] value )
		{
			SoftBuffer buffer = GetSoftBufferFromDataCode( dataCode, out bool isBit );
			if (buffer == null) return new OperateResult(StringResources.Language.NotSupportedDataType); ;

			if (isBit)
			{
				HslHelper.CalculateStartBitIndexAndLength( address, length, out int newStart, out ushort byteLength, out int offset );
				buffer.SetBool( value.ToBoolArray( ).SelectMiddle( address % 8, length ), address );
			}
			else if (dataCode == 0x0A || dataCode == 0x0C || dataCode == 0x08)
			{
				if (value.Length % 2 == 1) return new OperateResult(StringResources.Language.GeSRTPWriteLengthMustBeEven);
				buffer.SetBytes( value, address * 2 );
			}
			else
			{
				buffer.SetBytes( value, address );
			}
			return OperateResult.CreateSuccessResult( );
		}

		private byte[] WriteByCommand( byte[] command )
		{
			// 先判断是否有写入的权利，没有的话，直接返回写入异常
			if (!this.EnableWrite) return null;

			OperateResult write = WriteByCommand( command[51], BitConverter.ToUInt16( command, 52 ), BitConverter.ToUInt16( command, 54 ), command.RemoveBegin( 56 ) );
			if (!write.IsSuccess) return null;

			byte[] back = @"03 00 01 00 00 00 00 00 00 00 00 00 00 00 00 00
00 02 00 00 00 00 00 00 00 00 00 00 00 00 09 d4
00 0e 00 00 00 60 01 a0 01 01 00 00 00 00 00 00
00 00 ff 02 03 00 5c 01".ToHexBytes( );
			command.SelectMiddle( 2, 2 ).CopyTo( back, 2 );
			return back;
		}

		private byte[] ReadDateTimeByCommand( byte[] command )
		{
			byte[] back = @"03 00 03 00 07 00 00 00 00 00 00 00 00 00 00 00 00 01 00 00 00 00 00 00 00 00 00 00 00 00 06 94
				00 0e 00 00 00 60 01 a0 00 00 0c 00 00 18 00 00 01 01 ff 02 03 00 5c 01 00 00 00 00 00 00 03".ToHexBytes( );
			DateTime dateTime = DateTime.Now;
			dateTime.Second.ToString( "D2" ).ToHexBytes( ).CopyTo( back, 56 );
			dateTime.Minute.ToString( "D2" ).ToHexBytes( ).CopyTo( back, 57 );
			dateTime.Hour.ToString(   "D2" ).ToHexBytes( ).CopyTo( back, 58 );
			dateTime.Day.ToString(    "D2" ).ToHexBytes( ).CopyTo( back, 59 );
			dateTime.Month.ToString(  "D2" ).ToHexBytes( ).CopyTo( back, 60 );
			(dateTime.Year - 2000).ToString( "D2" ).ToHexBytes( ).CopyTo( back, 61 );

			command.SelectMiddle( 2, 2 ).CopyTo( back, 2 );
			return back;
		}

		private byte[] ReadProgramNameByCommand( byte[] command )
		{
			byte[] back = @"
03 00 07 00 2a 00 00 00 00 00 00 00 00 00 00 00 
00 01 00 00 00 00 00 00 00 00 00 00 00 00 06 94 
00 0e 00 00 00 62 01 a0 00 00 2a 00 00 18 00 00 
01 01 ff 02 03 00 5c 01 00 00 00 00 00 00 00 00 
01 00 00 00 00 00 00 00 00 00 50 41 43 34 30 30 
00 00 00 00 00 00 00 00 00 00 03 00 01 50 05 18 
01 21".ToHexBytes( );
			command.SelectMiddle( 2, 2 ).CopyTo( back, 2 );
			return back;
		}

		#endregion

		#region Data Save Load Override

		/// <inheritdoc/>
		protected override  void LoadFromBytes( byte[] content )
		{
			if (content.Length < DataPoolLength * 15) throw new Exception( "File is not correct" );

			iBuffer .SetBytes( content, DataPoolLength * 0, 0, DataPoolLength );
			qBuffer .SetBytes( content, DataPoolLength * 1, 0, DataPoolLength );
			mBuffer .SetBytes( content, DataPoolLength * 2, 0, DataPoolLength );
			tBuffer .SetBytes( content, DataPoolLength * 3, 0, DataPoolLength );
			saBuffer.SetBytes( content, DataPoolLength * 4, 0, DataPoolLength );
			sbBuffer.SetBytes( content, DataPoolLength * 5, 0, DataPoolLength );
			scBuffer.SetBytes( content, DataPoolLength * 6, 0, DataPoolLength );
			sBuffer .SetBytes( content, DataPoolLength * 7, 0, DataPoolLength );
			gBuffer .SetBytes( content, DataPoolLength * 8, 0, DataPoolLength );
			aiBuffer.SetBytes( content, DataPoolLength * 9, 0, DataPoolLength * 2 );
			aqBuffer.SetBytes( content, DataPoolLength * 11, 0, DataPoolLength * 2 );
			rBuffer .SetBytes( content, DataPoolLength * 13, 0, DataPoolLength * 2 );
		}

		/// <inheritdoc/>
		protected override byte[] SaveToBytes( )
		{
			byte[] buffer = new byte[DataPoolLength * 15];
			Array.Copy( iBuffer .GetBytes( ), 0, buffer, DataPoolLength * 0, DataPoolLength );
			Array.Copy( qBuffer .GetBytes( ), 0, buffer, DataPoolLength * 1, DataPoolLength );
			Array.Copy( mBuffer .GetBytes( ), 0, buffer, DataPoolLength * 2, DataPoolLength );
			Array.Copy( tBuffer .GetBytes( ), 0, buffer, DataPoolLength * 3, DataPoolLength);
			Array.Copy( saBuffer.GetBytes( ), 0, buffer, DataPoolLength * 4, DataPoolLength );
			Array.Copy( sbBuffer.GetBytes( ), 0, buffer, DataPoolLength * 5, DataPoolLength );
			Array.Copy( scBuffer.GetBytes( ), 0, buffer, DataPoolLength * 6, DataPoolLength );
			Array.Copy( sBuffer .GetBytes( ), 0, buffer, DataPoolLength * 7, DataPoolLength );
			Array.Copy( gBuffer .GetBytes( ), 0, buffer, DataPoolLength * 8, DataPoolLength );
			Array.Copy( aiBuffer.GetBytes( ), 0, buffer, DataPoolLength * 9, DataPoolLength * 2 );
			Array.Copy( aqBuffer.GetBytes( ), 0, buffer, DataPoolLength * 11, DataPoolLength * 2 );
			Array.Copy( rBuffer .GetBytes( ), 0, buffer, DataPoolLength * 13, DataPoolLength * 2 );

			return buffer;
		}

		#endregion

		#region IDisposable Support

		/// <inheritdoc/>
		protected override void Dispose( bool disposing )
		{
			if (disposing)
			{
				iBuffer .Dispose( );
				qBuffer .Dispose( );
				mBuffer .Dispose( );
				tBuffer .Dispose( );
				saBuffer.Dispose( );
				sbBuffer.Dispose( );
				scBuffer.Dispose( );
				sBuffer .Dispose( );
				gBuffer .Dispose( );
				aiBuffer.Dispose( );
				aqBuffer.Dispose( );
				rBuffer .Dispose( );
			}
			base.Dispose( disposing );
		}

		#endregion

		#region Private Member

		private SoftBuffer iBuffer;
		private SoftBuffer qBuffer;
		private SoftBuffer mBuffer;
		private SoftBuffer tBuffer;
		private SoftBuffer saBuffer;
		private SoftBuffer sbBuffer;
		private SoftBuffer scBuffer;
		private SoftBuffer sBuffer;
		private SoftBuffer gBuffer;
		private SoftBuffer aiBuffer;
		private SoftBuffer aqBuffer;
		private SoftBuffer rBuffer;
		private const int DataPoolLength = 65536;      // 数据的长度

		#endregion

		#region Object Override

		/// <inheritdoc/>
		public override string ToString( ) => $"GeSRTPServer[{Port}]";

		#endregion

	}
}
