using HslCommunication.BasicFramework;
using HslCommunication.Core;
using HslCommunication.Core.Net;
using HslCommunication.ModBus;
using HslCommunication.Reflection;
using HslCommunication.Core.IMessage;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Net.Sockets;
using System.Text;
#if !NET20 && !NET35
using System.Threading.Tasks;
#endif

namespace HslCommunication.Profinet.Fuji
{
	/// <summary>
	/// <b>[商业授权]</b> 富士的SPB虚拟的PLC，线圈支持X,Y,M的读写，其中X只能远程读，寄存器支持D,R,W的读写操作。<br />
	/// <b>[Authorization]</b> Fuji's SPB virtual PLC, the coil supports X, Y, M read and write, 
	/// X can only be read remotely, and the register supports D, R, W read and write operations.
	/// </summary>
	public class FujiSPBServer : NetworkDataServerBase
	{
		#region Constructor

		/// <summary>
		/// 实例化一个富士SPB的网口和串口服务器，支持数据读写操作
		/// </summary>
		public FujiSPBServer( )
		{
			// 四个数据池初始化，线圈，输入线圈，寄存器，只读寄存器
			xBuffer = new SoftBuffer( DataPoolLength );
			yBuffer = new SoftBuffer( DataPoolLength );
			mBuffer = new SoftBuffer( DataPoolLength );
			dBuffer = new SoftBuffer( DataPoolLength * 2 );
			rBuffer = new SoftBuffer( DataPoolLength * 2 );
			wBuffer = new SoftBuffer( DataPoolLength * 2 );

			ByteTransform = new RegularByteTransform( );
			ByteTransform.DataFormat = DataFormat.CDAB;
			WordLength = 1;
		}

		#endregion

		#region Public Members

		/// <inheritdoc cref="ModbusTcpNet.DataFormat"/>
		public DataFormat DataFormat
		{
			get { return ByteTransform.DataFormat; }
			set { ByteTransform.DataFormat = value; }
		}

		/// <inheritdoc cref="ModbusTcpNet.IsStringReverse"/>
		public bool IsStringReverse
		{
			get { return ByteTransform.IsStringReverseByteWord; }
			set { ByteTransform.IsStringReverseByteWord = value; }
		}

		/// <inheritdoc cref="FujiSPBOverTcp.Station"/>
		public int Station
		{
			get { return station; }
			set { station = value; }
		}

		#endregion

		#region Data Persistence

		/// <inheritdoc/>
		protected override byte[] SaveToBytes( )
		{
			byte[] buffer = new byte[DataPoolLength * 9];
			xBuffer.GetBytes( ).CopyTo( buffer, DataPoolLength * 0 );
			yBuffer.GetBytes( ).CopyTo( buffer, DataPoolLength * 1 );
			mBuffer.GetBytes( ).CopyTo( buffer, DataPoolLength * 2 );
			dBuffer.GetBytes( ).CopyTo( buffer, DataPoolLength * 3 );
			rBuffer.GetBytes( ).CopyTo( buffer, DataPoolLength * 5 );
			wBuffer.GetBytes( ).CopyTo( buffer, DataPoolLength * 7 );
			return buffer;
		}

		/// <inheritdoc/>
		protected override void LoadFromBytes( byte[] content )
		{
			if (content.Length < DataPoolLength * 9) throw new Exception( "File is not correct" );
			xBuffer.SetBytes( content, 0 * DataPoolLength, DataPoolLength );
			yBuffer.SetBytes( content, 1 * DataPoolLength, DataPoolLength );
			mBuffer.SetBytes( content, 2 * DataPoolLength, DataPoolLength );
			dBuffer.SetBytes( content, 3 * DataPoolLength, DataPoolLength * 2 );
			rBuffer.SetBytes( content, 5 * DataPoolLength, DataPoolLength * 2 );
			wBuffer.SetBytes( content, 7 * DataPoolLength, DataPoolLength * 2 );
		}

		#endregion

		#region NetworkDataServerBase Override

		/// <inheritdoc cref="FujiSPBOverTcp.Read(string, ushort)"/>
		[HslMqttApi( "ReadByteArray", "" )]
		public override OperateResult<byte[]> Read( string address, ushort length )
		{
			var result = new OperateResult<byte[]>( );
			try
			{
				switch (address[0])
				{
					case 'X':
					case 'x': return OperateResult.CreateSuccessResult( xBuffer.GetBytes( Convert.ToInt32( address.Substring( 1 ) ) * 2, length * 2 ) );
					case 'Y':
					case 'y': return OperateResult.CreateSuccessResult( yBuffer.GetBytes( Convert.ToInt32( address.Substring( 1 ) ) * 2, length * 2 ) );
					case 'M':
					case 'm': return OperateResult.CreateSuccessResult( mBuffer.GetBytes( Convert.ToInt32( address.Substring( 1 ) ) * 2, length * 2 ) );
					case 'D':
					case 'd': return OperateResult.CreateSuccessResult( dBuffer.GetBytes( Convert.ToInt32( address.Substring( 1 ) ) * 2, length * 2 ) );
					case 'R': 
					case 'r': return OperateResult.CreateSuccessResult( rBuffer.GetBytes( Convert.ToInt32( address.Substring( 1 ) ) * 2, length * 2 ) );
					case 'W':
					case 'w': return OperateResult.CreateSuccessResult( wBuffer.GetBytes( Convert.ToInt32( address.Substring( 1 ) ) * 2, length * 2 ) );
					default: throw new Exception( StringResources.Language.NotSupportedDataType );
				}
			}
			catch (Exception ex)
			{
				result.Message = ex.Message;
				return result;
			}
		}

		/// <inheritdoc cref="FujiSPBOverTcp.Write(string, byte[])"/>
		[HslMqttApi( "WriteByteArray", "" )]
		public override OperateResult Write( string address, byte[] value )
		{
			var result = new OperateResult<byte[]>( );
			try
			{
				switch (address[0])
				{
					case 'X':
					case 'x': xBuffer.SetBytes( value, Convert.ToInt32( address.Substring( 1 ) ) * 2 ); return OperateResult.CreateSuccessResult( );
					case 'Y':
					case 'y': yBuffer.SetBytes( value, Convert.ToInt32( address.Substring( 1 ) ) * 2 ); return OperateResult.CreateSuccessResult( );
					case 'M':
					case 'm': mBuffer.SetBytes( value, Convert.ToInt32( address.Substring( 1 ) ) * 2 ); return OperateResult.CreateSuccessResult( );
					case 'D':
					case 'd': dBuffer.SetBytes( value, Convert.ToInt32( address.Substring( 1 ) ) * 2 ); return OperateResult.CreateSuccessResult( );
					case 'R':
					case 'r': rBuffer.SetBytes( value, Convert.ToInt32( address.Substring( 1 ) ) * 2 ); return OperateResult.CreateSuccessResult( );
					case 'W':
					case 'w': wBuffer.SetBytes( value, Convert.ToInt32( address.Substring( 1 ) ) * 2 ); return OperateResult.CreateSuccessResult( );
					default: throw new Exception( StringResources.Language.NotSupportedDataType );
				}
			}
			catch (Exception ex)
			{
				result.Message = ex.Message;
				return result;
			}
		}

		/// <inheritdoc cref="FujiSPBOverTcp.ReadBool(string, ushort)"/>
		[HslMqttApi( "ReadBoolArray", "" )]
		public override OperateResult<bool[]> ReadBool( string address, ushort length )
		{
			try
			{
				int bitIndex = 0;
				if (address.LastIndexOf( '.' ) > 0)
				{
					bitIndex = HslHelper.GetBitIndexInformation( ref address );
					bitIndex = Convert.ToInt32( address.Substring( 1 ) ) * 16 + bitIndex;
				}
				else
				{
					if( address[0] == 'X' || address[0] == 'x' ||
						address[0] == 'Y' || address[0] == 'y' ||
						address[0] == 'M' || address[0] == 'm')
					{
						bitIndex = Convert.ToInt32( address.Substring( 1 ) );
					}
				}
				switch (address[0])
				{
					case 'X':
					case 'x': return OperateResult.CreateSuccessResult( xBuffer.GetBool( bitIndex, length ) );
					case 'Y':
					case 'y': return OperateResult.CreateSuccessResult( yBuffer.GetBool( bitIndex, length ) );
					case 'M':
					case 'm': return OperateResult.CreateSuccessResult( mBuffer.GetBool( bitIndex, length ) );
					case 'D':
					case 'd': return OperateResult.CreateSuccessResult( dBuffer.GetBool( bitIndex, length ) );
					case 'R':
					case 'r': return OperateResult.CreateSuccessResult( rBuffer.GetBool( bitIndex, length ) );
					case 'W':
					case 'w': return OperateResult.CreateSuccessResult( wBuffer.GetBool( bitIndex, length ) );
					default: throw new Exception( StringResources.Language.NotSupportedDataType );
				}
			}
			catch (Exception ex)
			{
				return new OperateResult<bool[]>( ex.Message );
			}
		}

		/// <inheritdoc cref="NetworkDeviceBase.Write(string, bool[])"/>
		[HslMqttApi( "WriteBoolArray", "" )]
		public override OperateResult Write( string address, bool[] value )
		{
			try
			{
				int bitIndex = 0;
				if (address.LastIndexOf( '.' ) > 0)
				{
					HslHelper.GetBitIndexInformation( ref address );
					bitIndex = Convert.ToInt32( address.Substring( 1 ) ) * 16 + bitIndex;
				}
				else
				{
					if (address[0] == 'X' || address[0] == 'x' ||
						address[0] == 'Y' || address[0] == 'y' ||
						address[0] == 'M' || address[0] == 'm')
					{
						bitIndex = Convert.ToInt32( address.Substring( 1 ) );
					}
				}
				switch (address[0])
				{
					case 'X':
					case 'x': xBuffer.SetBool( value, bitIndex ); return OperateResult.CreateSuccessResult( );
					case 'Y':
					case 'y': yBuffer.SetBool( value, bitIndex ); return OperateResult.CreateSuccessResult( );
					case 'M':
					case 'm': mBuffer.SetBool( value, bitIndex ); return OperateResult.CreateSuccessResult( );
					case 'D':
					case 'd': dBuffer.SetBool( value, bitIndex ); return OperateResult.CreateSuccessResult( );
					case 'R':
					case 'r': rBuffer.SetBool( value, bitIndex ); return OperateResult.CreateSuccessResult( );
					case 'W':
					case 'w': wBuffer.SetBool( value, bitIndex ); return OperateResult.CreateSuccessResult( );
					default: throw new Exception( StringResources.Language.NotSupportedDataType );
				}
			}
			catch (Exception ex)
			{
				return new OperateResult<bool[]>( ex.Message );
			}
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
				OperateResult<byte[]> read1 = ReceiveByMessage( session.WorkSocket, 2000, new FujiSPBMessage( ) );
#else
				OperateResult<byte[]> read1 = await ReceiveByMessageAsync( session.WorkSocket, 2000, new FujiSPBMessage( ) );
#endif
				if (!read1.IsSuccess) { RemoveClient( session ); return; };

				if (!Authorization.asdniasnfaksndiqwhawfskhfaiw( )) { RemoveClient( session ); return; };

				if ( read1.Content[0] != 0x3A ) { RemoveClient( session ); return; }

				LogNet?.WriteDebug( ToString( ), $"[{session.IpEndPoint}] Tcp {StringResources.Language.Receive}：{Encoding.ASCII.GetString( read1.Content.RemoveLast( 2 ) )}" );

				byte[] back = ReadFromSPBCore( read1.Content );

				if (back == null) { RemoveClient( session ); return; }
				if (!Send( session.WorkSocket, back ).IsSuccess) { RemoveClient( session ); return; }

				LogNet?.WriteDebug( ToString( ), $"[{session.IpEndPoint}] Tcp {StringResources.Language.Send}：{Encoding.ASCII.GetString( back.RemoveLast( 2 ) )}" );

				session.HeartTime = DateTime.Now;
				RaiseDataReceived( session, read1.Content );
				if (!session.WorkSocket.BeginReceiveResult( SocketAsyncCallBack, session ).IsSuccess) RemoveClient( session );
			}
		}

		#endregion

		#region Core Read

		private byte[] CreateResponseBack( byte err, string command, byte[] data, bool addLength = true )
		{
			StringBuilder sb = new StringBuilder( );
			sb.Append( ':' );
			sb.Append( Station.ToString( "X2" ) );
			sb.Append( "00" );
			sb.Append( command.Substring( 9, 4 ) );
			sb.Append( err.ToString( "X2" ) );
			if(err == 0 && data != null)
			{
				if (addLength == true) sb.Append( FujiSPBHelper.AnalysisIntegerAddress( data.Length / 2 ) );
				sb.Append( data.ToHexString( ) );
			}
			sb[3] = ((sb.Length - 5) / 2).ToString( "X2" )[0];
			sb[4] = ((sb.Length - 5) / 2).ToString( "X2" )[1];
			sb.Append( "\r\n" );
			return Encoding.ASCII.GetBytes( sb.ToString( ) );
		}

		private int AnalysisAddress( string address )
		{
			string tmp = address.Substring( 2 ) + address.Substring( 0, 2 );
			return Convert.ToInt32( tmp );
		}

		private byte[] ReadFromSPBCore( byte[] receive )
		{
			if (receive.Length < 15) return null;
			if (receive[receive.Length - 2] == 0x0D && receive[receive.Length - 1] == 0x0A)
				receive = receive.RemoveLast( 2 );
			string command = Encoding.ASCII.GetString( receive );
			int commandLength = Convert.ToInt32( command.Substring( 3, 2 ), 16 );
			if (commandLength != (command.Length - 5) / 2) return CreateResponseBack( 3, command, null );
			if      (command.Substring( 9, 4 ) == "0000") return ReadByCommand( command );
			else if (command.Substring( 9, 4 ) == "0100") return WriteByCommand( command );
			else if (command.Substring( 9, 4 ) == "0102") return WriteBitByCommand( command );
			return null;
		}

		private byte[] ReadByCommand( string command )
		{
			string model = command.Substring( 13, 2 );
			int address = AnalysisAddress( command.Substring( 15, 4 ) );
			int length = AnalysisAddress( command.Substring( 19, 4 ) );

			if (length > 105) CreateResponseBack( 3, command, null );
			if      (model == "0C") return CreateResponseBack( 0, command, dBuffer.GetBytes( address * 2, length * 2 ) );
			else if (model == "0D") return CreateResponseBack( 0, command, rBuffer.GetBytes( address * 2, length * 2 ) );
			else if (model == "0E") return CreateResponseBack( 0, command, wBuffer.GetBytes( address * 2, length * 2 ) );
			else if (model == "01") return CreateResponseBack( 0, command, xBuffer.GetBytes( address * 2, length * 2 ) );
			else if (model == "00") return CreateResponseBack( 0, command, yBuffer.GetBytes( address * 2, length * 2 ) );
			else if (model == "02") return CreateResponseBack( 0, command, mBuffer.GetBytes( address * 2, length * 2 ) );
			else return CreateResponseBack( 2, command, null );             // 接收了未定义的命令或无法处理的命令
		}

		private byte[] WriteByCommand( string command )
		{
			// 先判断是否有写入的权利，没有的话，直接返回写入异常
			if (!this.EnableWrite) return CreateResponseBack( 2, command, null );

			string model = command.Substring( 13, 2 );
			int address = AnalysisAddress( command.Substring( 15, 4 ) );
			int length = AnalysisAddress( command.Substring( 19, 4 ) );

			if(length * 4 != command.Length - 23) return CreateResponseBack( 3, command, null );   // 数据部分有矛盾
			byte[] buffer = command.Substring( 23 ).ToHexBytes( );
			if      (model == "0C") { dBuffer.SetBytes( buffer, address * 2 ); return CreateResponseBack( 0, command, null ); }
			else if (model == "0D") { rBuffer.SetBytes( buffer, address * 2 ); return CreateResponseBack( 0, command, null ); }
			else if (model == "0E") { wBuffer.SetBytes( buffer, address * 2 ); return CreateResponseBack( 0, command, null ); }
			else if (model == "00") { yBuffer.SetBytes( buffer, address * 2 ); return CreateResponseBack( 0, command, null ); }
			else if (model == "02") { mBuffer.SetBytes( buffer, address * 2 ); return CreateResponseBack( 0, command, null ); }
			else return CreateResponseBack( 2, command, null );             // 接收了未定义的命令或无法处理的命令
		}

		private byte[] WriteBitByCommand( string command )
		{
			// 先判断是否有写入的权利，没有的话，直接返回写入异常
			if (!this.EnableWrite) return CreateResponseBack( 2, command, null );

			string model = command.Substring( 13, 2 );
			int address = AnalysisAddress( command.Substring( 15, 4 ) );
			int bit = Convert.ToInt32( command.Substring( 19, 2 ) );
			bool value = command.Substring( 21, 2 ) != "00";
			
			if      (model == "0C") { dBuffer.SetBool( value, address * 8 + bit ); return CreateResponseBack( 0, command, null ); }
			else if (model == "0D") { rBuffer.SetBool( value, address * 8 + bit ); return CreateResponseBack( 0, command, null ); }
			else if (model == "0E") { wBuffer.SetBool( value, address * 8 + bit ); return CreateResponseBack( 0, command, null ); }
			else if (model == "00") { yBuffer.SetBool( value, address * 8 + bit ); return CreateResponseBack( 0, command, null ); }
			else if (model == "02") { mBuffer.SetBool( value, address * 8 + bit ); return CreateResponseBack( 0, command, null ); }
			else return CreateResponseBack( 2, command, null );             // 接收了未定义的命令或无法处理的命令
		}

		#endregion

		#region Serial Support

		/// <inheritdoc/>
		protected override bool CheckSerialReceiveDataComplete( byte[] buffer, int receivedLength )
		{
			return ModBus.ModbusInfo.CheckAsciiReceiveDataComplete( buffer, receivedLength );
		}

		/// <inheritdoc/>
		protected override OperateResult<byte[]> DealWithSerialReceivedData( byte[] data )
		{
			if (data.Length < 5) return new OperateResult<byte[]>( $"[{GetSerialPort().PortName}] Uknown Data：{data.ToHexString( ' ' )}" );

			if (data[0] != 0x3A) return new OperateResult<byte[]>( $"[{GetSerialPort( ).PortName}] not 0x3A Start Data：{data.ToHexString( ' ' )}" );
			LogNet?.WriteDebug( ToString( ), $"[{GetSerialPort( ).PortName}] Ascii {StringResources.Language.Receive}：{SoftBasic.GetAsciiStringRender( data )}" );

			if (Encoding.ASCII.GetString( data, 1, 2 ) != station.ToString( "X2" ))
				return new OperateResult<byte[]>(  $"[{GetSerialPort( ).PortName}] Station not match , Except: {station:X2} , Actual: {SoftBasic.GetAsciiStringRender( data )}" );

			byte[] back = ReadFromSPBCore( data );

			LogNet?.WriteDebug( ToString( ), $"[{GetSerialPort( ).PortName}] Ascii {StringResources.Language.Send}：{SoftBasic.GetAsciiStringRender( back )}" );
			return OperateResult.CreateSuccessResult( back );
		}

		#endregion

		#region IDisposable Support

		/// <inheritdoc/>
		protected override void Dispose( bool disposing )
		{
			if (disposing)
			{
				xBuffer.Dispose( );
				yBuffer.Dispose( );
				mBuffer.Dispose( );
				dBuffer.Dispose( );
				rBuffer.Dispose( );
				wBuffer.Dispose( );
			}
			base.Dispose( disposing );
		}

		#endregion

		#region Private Member

		private SoftBuffer xBuffer;       // 输入继电器的数据池
		private SoftBuffer yBuffer;       // 输出继电器的数据池
		private SoftBuffer mBuffer;       // 中间继电器的数据池
		private SoftBuffer dBuffer;       // 数据寄存器的数据池
		private SoftBuffer rBuffer;       // 文件寄存器的数据池
		private SoftBuffer wBuffer;       // 链路寄存器的数据池

		private const int DataPoolLength = 65536;     // 数据的长度
		private int station = 1;                      // 服务器的站号数据，对于tcp无效，对于串口来说，如果小于0，则忽略站号信息

		#endregion

		#region Object Override

		/// <inheritdoc/>
		public override string ToString( ) => $"FujiSPBServer[{Port}]";

		#endregion
	}
}
