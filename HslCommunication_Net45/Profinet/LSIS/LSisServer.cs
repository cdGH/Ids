using HslCommunication.BasicFramework;
using HslCommunication.Core;
using HslCommunication.Core.IMessage;
using HslCommunication.Core.Net;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO.Ports;
using System.Net.Sockets;
using System.Text;
using HslCommunication.Reflection;

namespace HslCommunication.Profinet.LSIS
{
	/// <summary>
	/// <b>[商业授权]</b> Lsis的虚拟服务器，其中TCP的端口支持Fnet协议，串口支持Cnet协议<br />
	/// <b>[Authorization]</b> LSisServer
	/// </summary>
	public class LSisServer : NetworkDataServerBase
	{
		#region Constructor

		/// <summary>
		/// LSisServer
		/// </summary>
		public LSisServer( string CpuType )
		{
			pBuffer = new SoftBuffer( DataPoolLength );
			qBuffer = new SoftBuffer( DataPoolLength );
			iBuffer = new SoftBuffer( DataPoolLength );
			uBuffer = new SoftBuffer( DataPoolLength );
			mBuffer = new SoftBuffer( DataPoolLength );
			dBuffer = new SoftBuffer( DataPoolLength * 2 );
			tBuffer = new SoftBuffer( DataPoolLength * 2 );
			this.SetCpuType = CpuType;
			WordLength = 2;
			ByteTransform = new RegularByteTransform( );
		}

		#endregion

		/// <summary>
		/// set plc
		/// </summary>
		public string SetCpuType { get; set; }

		#region NetworkDataServerBase Override

		/// <inheritdoc cref="XGBFastEnet.Read(string, ushort)"/>
		[HslMqttApi( "ReadByteArray", "" )]
		public override OperateResult<byte[]> Read( string address, ushort length )
		{
			OperateResult<string> analysis = AnalysisAddressToByteUnit( address, false );
			if (!analysis.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( analysis );

			int startIndex = int.Parse( analysis.Content.Substring( 1 ) );
			switch (analysis.Content[0])
			{
				case 'P': return OperateResult.CreateSuccessResult( pBuffer.GetBytes( startIndex, length ) );
				case 'Q': return OperateResult.CreateSuccessResult( qBuffer.GetBytes( startIndex, length ) );
				case 'M': return OperateResult.CreateSuccessResult( mBuffer.GetBytes( startIndex, length ) );
				case 'I': return OperateResult.CreateSuccessResult( iBuffer.GetBytes( startIndex, length ) );
				case 'U': return OperateResult.CreateSuccessResult( uBuffer.GetBytes( startIndex, length ) );
				case 'D': return OperateResult.CreateSuccessResult( dBuffer.GetBytes( startIndex, length ) );
				case 'T': return OperateResult.CreateSuccessResult( tBuffer.GetBytes( startIndex, length ) );
				default: return new OperateResult<byte[]>( StringResources.Language.NotSupportedDataType );
			}
		}

		/// <inheritdoc cref="XGBFastEnet.Write(string, byte[])"/>
		[HslMqttApi( "WriteByteArray", "" )]
		public override OperateResult Write( string address, byte[] value )
		{
			OperateResult<string> analysis = AnalysisAddressToByteUnit( address, false );
			if (!analysis.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( analysis );

			int startIndex = int.Parse( analysis.Content.Substring( 1 ) );
			switch (analysis.Content[0])
			{
				case 'P': pBuffer.SetBytes( value, startIndex ); break;
				case 'Q': qBuffer.SetBytes( value, startIndex ); break;
				case 'M': mBuffer.SetBytes( value, startIndex ); break;
				case 'I': iBuffer.SetBytes( value, startIndex ); break;
				case 'U': uBuffer.SetBytes( value, startIndex ); break;
				case 'D': dBuffer.SetBytes( value, startIndex ); break;
				case 'T': tBuffer.SetBytes( value, startIndex ); break;
				default: return new OperateResult<byte[]>( StringResources.Language.NotSupportedDataType );
			}
			return OperateResult.CreateSuccessResult( );
		}

		#endregion

		#region Byte Read Write Operate

		/// <inheritdoc cref="XGBFastEnet.ReadByte(string)"/>
		[HslMqttApi( "ReadByte", "" )]
		public OperateResult<byte> ReadByte( string address ) => ByteTransformHelper.GetResultFromArray( Read( address, 1 ) );

		/// <inheritdoc cref="LSIS.XGBFastEnet.Write(string, byte)"/>
		[HslMqttApi( "WriteByte", "" )]
		public OperateResult Write( string address, byte value ) => Write( address, new byte[] { value } );

		#endregion

		#region Bool Read Write Operate

		/// <inheritdoc/>
		[HslMqttApi( "ReadBoolArray", "" )]
		public override OperateResult<bool[]> ReadBool( string address, ushort length )
		{
			OperateResult<string> analysis = AnalysisAddressToByteUnit( address, true );
			if (!analysis.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( analysis );

			int startIndex = int.Parse( analysis.Content.Substring( 1 ) );
			switch (analysis.Content[0])
			{
				case 'P': return OperateResult.CreateSuccessResult( pBuffer.GetBool( startIndex, length ) );
				case 'Q': return OperateResult.CreateSuccessResult( qBuffer.GetBool( startIndex, length ) );
				case 'M': return OperateResult.CreateSuccessResult( mBuffer.GetBool( startIndex, length ) );
				case 'I': return OperateResult.CreateSuccessResult( iBuffer.GetBool( startIndex, length ) );
				case 'U': return OperateResult.CreateSuccessResult( uBuffer.GetBool( startIndex, length ) );
				default: return new OperateResult<bool[]>( StringResources.Language.NotSupportedDataType );
			}
		}

		/// <inheritdoc/>
		[HslMqttApi( "WriteBoolArray", "" )]
		public override OperateResult Write( string address, bool[] value )
		{
			OperateResult<string> analysis = AnalysisAddressToByteUnit( address, true );
			if (!analysis.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( analysis );

			int startIndex = int.Parse( analysis.Content.Substring( 1 ) );
			switch (analysis.Content[0])
			{
				case 'P': pBuffer.SetBool( value, startIndex ); return OperateResult.CreateSuccessResult( );
				case 'Q': qBuffer.SetBool( value, startIndex ); return OperateResult.CreateSuccessResult( );
				case 'M': mBuffer.SetBool( value, startIndex ); return OperateResult.CreateSuccessResult( );
				case 'I': iBuffer.SetBool( value, startIndex ); return OperateResult.CreateSuccessResult( );
				case 'U': uBuffer.SetBool( value, startIndex ); return OperateResult.CreateSuccessResult( );
				default: return new OperateResult( StringResources.Language.NotSupportedDataType );
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
					OperateResult<byte[]> read1 = ReceiveByMessage( session.WorkSocket, 5000, new LsisFastEnetMessage( ) );
#else
					OperateResult<byte[]> read1 = await ReceiveByMessageAsync( session.WorkSocket, 5000, new LsisFastEnetMessage( ) );
#endif
					if (!read1.IsSuccess) { RemoveClient( session ); return; };

					if (!Authorization.asdniasnfaksndiqwhawfskhfaiw( )) { RemoveClient( session ); return; };

					LogNet?.WriteDebug( ToString( ), $"[{session.IpEndPoint}] Tcp {StringResources.Language.Receive}：{read1.Content.ToHexString( ' ' )}" );

					byte[] receive = read1.Content;
					byte[] SendData = null;
					if (receive[20] == 0x54) SendData = ReadByMessage( receive );               // r
					else if (receive[20] == 0x58) SendData = WriteByMessage( receive );         // w
					else
					{
						RaiseDataReceived( session, SendData );
						RemoveClient( session );
						return;
					}

					if (SendData == null) { RemoveClient( session ); return; }

					RaiseDataReceived( session, SendData );
					session.WorkSocket.Send( SendData );

					LogNet?.WriteDebug( ToString( ), $"[{session.IpEndPoint}] Tcp {StringResources.Language.Send}：{SendData.ToHexString( ' ' )}" );

					session.HeartTime = DateTime.Now;
					RaiseDataSend( receive );
					session.WorkSocket.BeginReceive( new byte[0], 0, 0, SocketFlags.None, new AsyncCallback( SocketAsyncCallBack ), session );
				}
				catch( Exception ex )
				{
					RemoveClient( session, $"SocketAsyncCallBack -> " + ex.Message );
				}
			}
		}

		private byte[] ReadByMessage( byte[] packCommand )
		{
			List<byte> content = new List<byte>( );

			content.AddRange( ReadByCommand( packCommand ) );
			//var testD = SoftBasic.HexStringToBytes(@"4C 53 49 53 2D 58 47 54 00 00 04 01 A0 11 01 00 16 00 02 2A 55 00 02 00 00 00 00 00 03 00 02 00 D4 26 02 00 3C 34 02 00 72 33");
			//var testM = SoftBasic.HexStringToBytes(@"4C 53 49 53 2D 58 47 54 00 00 04 01 A0 11 01 00 13 00 02 27 55 00 00 00 00 00 00 00 03 00 01 00 00 01 00 01 01 00 01");
			//var testT = SoftBasic.HexStringToBytes(@"4C 53 49 53 2D 58 47 54 00 00 04 01 A0 11 01 00 16 00 02 2A 55 00 02 00 00 00 00 00 03 00 02 00 00 00 02 00 00 00 02 00 00 00");

			return content.ToArray();
		}

		private byte[] ReadByCommand( byte[] command )
		{
			//                                                             Read  Type  ----  block  len  %  MW100          连续模式
			// 0  1  2  3  4  5  6  7  8  9  10 11 12 13 14 15 16 17 18 19 20 21 22 23 24 25 26 27 28 29 30 31 32 33 34 35  36 37
			// 4C 53 49 53 2D 58 47 54 00 00 00 00 A0 33 01 00 10 00 03 00 54 00 01 00 00 00 01 00 06 00 25 4D 42 31 30 30 [05 1C]
			var result = new List<byte>( );
			result.AddRange( command.SelectBegin( 20 ) );
			result[ 9] = 0x11;
			result[10] = 0x01;
			result[12] = 0xA0;
			result[13] = 0x11;
			result[18] = 0x03;
			result.AddRange( new byte[] { 0x55, 0x00, command[22], command[23], 0x08, 0x01, 0x00, 0x00, 0x01, 0x00 } );
			int NameLength = command[28];
			string deviceAddress = Encoding.ASCII.GetString( command, 31, NameLength - 1 );

			byte[] data;
			if (command[22] == 0x00)
			{
				int offset = Convert.ToInt32( deviceAddress.Substring( 2 ) );
				data = ReadBool( deviceAddress.Substring( 0, 2 ) + (offset / 16).ToString( ) + (offset % 16).ToString( "X1" ) ).Content ? new byte[1] { 0x01 } : new byte[1] { 0x00 };
			}
			else if (command[22] == 0x01) data = Read( deviceAddress, 1 ).Content;
			else if (command[22] == 0x02) data = Read( deviceAddress, 2 ).Content;
			else if (command[22] == 0x03) data = Read( deviceAddress, 4 ).Content;
			else if (command[22] == 0x04) data = Read( deviceAddress, 8 ).Content;
			else if (command[22] == 0x14)
			{
				ushort RequestCount = BitConverter.ToUInt16( command, 30 + NameLength );
				data = Read( deviceAddress, RequestCount ).Content;
			}
			else data = Read( deviceAddress, 1 ).Content;

			result.AddRange( BitConverter.GetBytes( (ushort)data.Length ) );
			result.AddRange( data );
			result[16] = (byte)(result.Count - 20);
			return result.ToArray( );
		}
		private byte[] WriteByMessage( byte[] packCommand )
		{
			// 先判断是否有写入的权利，没有的话，直接返回写入异常
			if (!this.EnableWrite) return null;

			// 0  1  2  3  4  5  6  7  8  9  10 11 12 13 14 15 16 17 18 19 20 21 22 23 24 25 26 27 28 29 30 31 32 33 34 35  36 37 38 39
			// 4C 53 49 53 2D 58 47 54 00 00 00 00 A0 33 01 00 14 00 03 00 58 00 00 00 00 00 01 00 06 00 25 4D 58 31 36 30 [02 00 00 00]
			var result = new List<byte>( );
			result.AddRange( packCommand.SelectBegin( 20 ) );
			result[ 9] = 0x11;
			result[10] = 0x01;
			result[12] = 0xA0;
			result[13] = 0x11;
			result[18] = 0x03;
			result.AddRange( new byte[] { 0x59, 0x00, 0x14, 0x00, 0x08, 0x01, 0x00, 0x00, 0x01, 0x00 } );

			int NameLength = packCommand[28];
			var deviceAddress = Encoding.ASCII.GetString( packCommand, 31, NameLength - 1 );
			int RequestCount = BitConverter.ToUInt16( packCommand, 30 + NameLength );

			byte[] data = ByteTransform.TransByte( packCommand, 32 + NameLength, RequestCount );
			if (packCommand[22] == 0x00)
			{
				int offset = Convert.ToInt32( deviceAddress.Substring( 2 ) );
				Write( deviceAddress.Substring( 0, 2 ) + (offset / 16).ToString( ) + (offset % 16).ToString( "X1" ), packCommand[37] != 0x00 );
			}
			else
			{
				Write( deviceAddress, data );
			}
			result[16] = (byte)(result.Count - 20);
			return result.ToArray( );
		}

		#endregion

		#region Data Save Load Override

		/// <inheritdoc/>
		protected override void LoadFromBytes( byte[] content )
		{
			if (content.Length < DataPoolLength * 4) throw new Exception( "File is not correct" );

			pBuffer.SetBytes( content, DataPoolLength * 0, 0, DataPoolLength );
			qBuffer.SetBytes( content, DataPoolLength * 1, 0, DataPoolLength );
			mBuffer.SetBytes( content, DataPoolLength * 2, 0, DataPoolLength );
			dBuffer.SetBytes( content, DataPoolLength * 3, 0, DataPoolLength );
		}

		/// <inheritdoc/>
		protected override byte[] SaveToBytes( )
		{
			byte[] buffer = new byte[DataPoolLength * 4];
			Array.Copy( pBuffer.GetBytes( ), 0, buffer, 0, DataPoolLength );
			Array.Copy( qBuffer.GetBytes( ), 0, buffer, DataPoolLength, DataPoolLength );
			Array.Copy( mBuffer.GetBytes( ), 0, buffer, DataPoolLength * 2, DataPoolLength );
			Array.Copy( dBuffer.GetBytes( ), 0, buffer, DataPoolLength * 3, DataPoolLength );

			return buffer;
		}

		/// <summary>
		/// NumberStyles HexNumber
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		static bool IsHex(string value)
		{
			if (string.IsNullOrEmpty(value))
				return false;

			var state = false;

			for (var i = 0; i < value.Length; i++)
			{
				switch (value[i])
				{
					case 'A':
					case 'B':
					case 'C':
					case 'D':
					case 'E':
					case 'F':
					case 'a':
					case 'b':
					case 'c':
					case 'd':
					case 'e':
					case 'f':
						state = true;
						break;
				}
			}

			return state;
		}

		/// <summary>
		/// Check the intput string address
		/// </summary>
		/// <param name="address"></param>
		/// <returns></returns>
		public static int CheckAddress(string address)
		{
			int bitSelacdetAddress = 0;
			if (IsHex(address))
			{
				int v;

				if (Int32.TryParse(address, NumberStyles.HexNumber, CultureInfo.CurrentCulture, out v))
					bitSelacdetAddress = v;
			}
			else
			{
				bitSelacdetAddress = int.Parse(address);
			}

			return bitSelacdetAddress;
		}

		#endregion

		#region Private Member

		private SoftBuffer pBuffer;                    // p data type
		private SoftBuffer qBuffer;                    // q data type
		private SoftBuffer mBuffer;                    // 寄存器的数据池
		private SoftBuffer iBuffer;                    // i寄存器的数据池
		private SoftBuffer uBuffer;                    // u寄存器的数据池
		private SoftBuffer dBuffer;                    // 寄存器的数据池
		private SoftBuffer tBuffer;                    // t寄存器的数据池
		private const int DataPoolLength = 65536;      // 数据的长度

		#endregion

		#region Serial Support

		private int station = 1;

		/// <inheritdoc/>
		protected override bool CheckSerialReceiveDataComplete( byte[] buffer, int receivedLength )
		{
			if (receivedLength > 5) return buffer[receivedLength - 3] == 0x04;
			return base.CheckSerialReceiveDataComplete( buffer, receivedLength );
		}

		/// <inheritdoc/>
		protected override OperateResult<byte[]> DealWithSerialReceivedData( byte[] data )
		{
			LogNet?.WriteDebug( ToString( ), $"[{GetSerialPort( ).PortName}] {StringResources.Language.Receive}：{SoftBasic.GetAsciiStringRender( data )}" );

			try
			{
				byte[] back = null;
				if (data[3] == 0x72 || data[3] == 0x52) back = ReadSerialByCommand( data ); // READ
				else if (data[3] == 0x77 || data[3] == 0x57) back = WriteSerialByMessage( data ); // Write

				LogNet?.WriteDebug( ToString( ), $"[{GetSerialPort( ).PortName}] {StringResources.Language.Send}：{SoftBasic.GetAsciiStringRender( back )}" );
				return OperateResult.CreateSuccessResult( back );
			}
			catch (Exception ex)
			{
				return new OperateResult<byte[]>( $"[{GetSerialPort( ).PortName}] {ex.Message} Source: " + data.ToHexString( ' ' ) );
			}
		}

		private byte[] PackReadSerialResponse( byte[] receive, short err, List<byte[]> data )
		{
			var result = new List<byte>( 24 );
			if(err == 0)
				result.Add( 0x06 );    // ENQ
			else
				result.Add( 0x15 );    // NAK
			result.AddRange( SoftBasic.BuildAsciiBytesFrom( (byte)station ) );
			result.Add( receive[3] );    // command r / w


			result.Add( receive[4] );    // command type: SB  / SS
			result.Add( receive[5] );
			if (err == 0)
			{
				if (data != null)
				{
					if (Encoding.ASCII.GetString( receive, 4, 2 ) == "SS")
						result.AddRange( SoftBasic.BuildAsciiBytesFrom( (byte)data.Count ) );  // number of blocks
					else if (Encoding.ASCII.GetString( receive, 4, 2 ) == "SB")
						result.AddRange( Encoding.ASCII.GetBytes( "01" ) );
					for (int i = 0; i < data.Count; i++)
					{
						result.AddRange( SoftBasic.BuildAsciiBytesFrom( (byte)data[i].Length ) );     // number of data
						result.AddRange( SoftBasic.BytesToAsciiBytes( data[i] ) );                    // data
					}
				}
			}
			else
			{
				result.AddRange( SoftBasic.BuildAsciiBytesFrom( err ) );
			}

			result.Add( 0x03 );    // ETX
			int sum1 = 0;
			for (int i = 0; i < result.Count; i++)
			{
				sum1 += result[i];
			}
			result.AddRange( SoftBasic.BuildAsciiBytesFrom( (byte)sum1 ) );
			return result.ToArray( );
		}

		private byte[] ReadSerialByCommand( byte[] command )
		{
			// \0501rSS0106%MB100\04          // XGK
			// \0501rSB06%MB20002\047F        // XGB ReadContinus
			string tmp = SoftBasic.GetAsciiStringRender( command );
			if(Encoding.ASCII.GetString( command, 4, 2 ) == "SS")
			{
				int requestCount = int.Parse( Encoding.ASCII.GetString( command, 6, 2 ) );
				if (requestCount > 16) return PackReadSerialResponse( command, 4, null );

				List<byte[]> readData = new List<byte[]>( );
				int byteIndex = 8;
				for (int i = 0; i < requestCount; i++)
				{
					int nameLength = Convert.ToInt32( Encoding.ASCII.GetString( command, byteIndex, 2 ), 16 );
					string deviceAddress = Encoding.ASCII.GetString( command, byteIndex + 2 + 1, nameLength - 1 );

					if(deviceAddress[1] != 'X')
					{
						OperateResult<byte[]> read = Read( deviceAddress, AnalysisAddressLength( deviceAddress ) );
						if (!read.IsSuccess) return PackReadSerialResponse( command, 1, null );

						readData.Add( read.Content );
					}
					else
					{
						OperateResult<bool> read = ReadBool( deviceAddress );
						if (!read.IsSuccess) return PackReadSerialResponse( command, 1, null );

						readData.Add( read.Content ? new byte[] { 0x01 } : new byte[] { 0x00 } );
					}
					byteIndex += 2 + nameLength;
				}
				
				return PackReadSerialResponse( command, 0, readData );
			}
			else if(Encoding.ASCII.GetString( command, 4, 2 ) == "SB")
			{
				int nameLength = Convert.ToInt32( Encoding.ASCII.GetString( command, 6, 2 ), 16 );

				string deviceAddress = Encoding.ASCII.GetString( command, 8 + 1, nameLength - 1 );
				ushort length = Convert.ToUInt16( Encoding.ASCII.GetString( command, 8 + nameLength, 2 ) );

				ushort byteLength = (ushort)(length * AnalysisAddressLength( deviceAddress ));
				if (byteLength > 120) return PackReadSerialResponse( command, 0x1232, null );    // 读取不能超过60个字

				OperateResult<byte[]> read = Read( deviceAddress, byteLength );
				if (!read.IsSuccess) return PackReadSerialResponse( command, 1, null );

				return PackReadSerialResponse( command, 0, new List<byte[]>( ) { read.Content } );
			}
			else
			{
				return PackReadSerialResponse( command, 1, null );
			}
		}

		private byte[] WriteSerialByMessage( byte[] command )
		{
			// 先判断是否有写入的权利，没有的话，直接返回写入异常
			if (!this.EnableWrite) return null;

			if (Encoding.ASCII.GetString( command, 4, 2 ) == "SS")
			{
				int requestCount = int.Parse( Encoding.ASCII.GetString( command, 6, 2 ) );
				int byteIndex = 8;
				if (requestCount > 16) return PackReadSerialResponse( command, 4, null );

				for (int i = 0; i < requestCount; i++)
				{
					int nameLength = Convert.ToInt32( Encoding.ASCII.GetString( command, byteIndex, 2 ), 16);
					string deviceAddress = Encoding.ASCII.GetString( command, byteIndex + 2 + 1, nameLength - 1 );

					switch (deviceAddress[1])
					{
						case 'B':
						case 'W':
						case 'D':
						case 'L':
							{
								byte[] data = Encoding.ASCII.GetString( command, byteIndex + 2 + nameLength,
									AnalysisAddressLength( deviceAddress ) * 2 ).ToHexBytes( );
								OperateResult write = Write( deviceAddress, data );
								if (!write.IsSuccess) return PackReadSerialResponse( command, 1, null );
								byteIndex += 2 + nameLength + AnalysisAddressLength( deviceAddress ) * 2;
								break;
							}
						case 'X':
							{
								// \0501wSS0103%MX101\04A8
								OperateResult write = Write( deviceAddress, Convert.ToByte( Encoding.ASCII.GetString( command, byteIndex + 2 + nameLength, 2 ), 16 ) != 0x00 );
								if (!write.IsSuccess) return PackReadSerialResponse( command, 1, null );

								byteIndex += 2 + nameLength + 2;
								break;
							}
					}
				}
				return PackReadSerialResponse( command, 0, null );
			}
			else if (Encoding.ASCII.GetString( command, 4, 2 ) == "SB")
			{
				int nameLength = Convert.ToInt32( Encoding.ASCII.GetString( command, 6, 2 ), 16 );
				string deviceAddress = Encoding.ASCII.GetString( command, 9, nameLength - 1 );

				if (deviceAddress[1] == 'X') return PackReadSerialResponse( command, 0x1132, null );
				ushort length = Convert.ToUInt16( Encoding.ASCII.GetString( command, 8 + nameLength, 2 ) );

				int byteLength = length * AnalysisAddressLength( deviceAddress );
				OperateResult write = Write( deviceAddress, Encoding.ASCII.GetString( command, 10 + nameLength, byteLength * 2 ).ToHexBytes( ) );
				if (!write.IsSuccess) return PackReadSerialResponse( command, 1, null );
				return PackReadSerialResponse( command, 0, null );
			}
			else
			{
				return PackReadSerialResponse( command, 0x1132, null );
			}
		}

		#endregion

		#region Object Override

		/// <inheritdoc/>
		public override string ToString( ) => $"LSisServer[{Port}]";

		#endregion

		#region Public Static Method

		private static ushort AnalysisAddressLength( string address )
		{
			switch (address[1])
			{
				case 'X': return 1;
				case 'B': return 1;
				case 'W': return 2;
				case 'D': return 4;
				case 'L': return 8;
				default: return 1;
			}
		}

		/// <summary>
		/// 将带有数据类型的地址，转换成实际的byte数组的地址信息，例如 MW100 转成 M200
		/// </summary>
		/// <param name="address">带有类型的地址</param>
		/// <param name="isBit">是否是位操作</param>
		/// <returns>最终的按照字节为单位的地址信息</returns>
		public OperateResult<string> AnalysisAddressToByteUnit( string address, bool isBit )
		{
			if (!XGBFastEnet.AddressTypes.Contains( address.Substring( 0, 1 ) ))
				return new OperateResult<string>( StringResources.Language.NotSupportedDataType );

			int addressStart;
			try
			{
				if(address[0] == 'D' || address[0] == 'T')
				{
					switch (address[1])
					{
						case 'B': addressStart = Convert.ToInt32( address.Substring( 2 ) ); break;
						case 'W': addressStart = Convert.ToInt32( address.Substring( 2 ) ) * 2; break;
						case 'D': addressStart = Convert.ToInt32( address.Substring( 2 ) ) * 4; break;
						case 'L': addressStart = Convert.ToInt32( address.Substring( 2 ) ) * 8; break;
						default: addressStart = Convert.ToInt32( address.Substring( 1 ) ) * 2; break;
					}
				}
				else
				{
					if (isBit)
					{
						switch (address[1])
						{
							case 'X': addressStart = Panasonic.PanasonicHelper.CalculateComplexAddress( address.Substring( 2 ) ); break;
							default: addressStart = Panasonic.PanasonicHelper.CalculateComplexAddress( address.Substring( 1 ) ); break;
						}
					}
					else
					{
						switch (address[1])
						{
							case 'X': addressStart = Convert.ToInt32( address.Substring( 2 ) ); break;
							case 'B': addressStart = Convert.ToInt32( address.Substring( 2 ) ); break;
							case 'W': addressStart = Convert.ToInt32( address.Substring( 2 ) ) * 2; break;
							case 'D': addressStart = Convert.ToInt32( address.Substring( 2 ) ) * 4; break;
							case 'L': addressStart = Convert.ToInt32( address.Substring( 2 ) ) * 8; break;
							default: addressStart = Convert.ToInt32( address.Substring( 1 ) ) * (isBit ? 1 : 2); break;
						}
					}
				}

				return OperateResult.CreateSuccessResult( address.Substring( 0, 1 ) + addressStart.ToString( ) );
			}
			catch(Exception ex)
			{
				return new OperateResult<string>( "AnalysisAddress Failed: " + ex.Message + " Source: " + address );
			}
		}

		#endregion
	}
}