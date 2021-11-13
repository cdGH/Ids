using HslCommunication.BasicFramework;
using HslCommunication.Core;
using HslCommunication.Core.IMessage;
using HslCommunication.Core.Net;
using System;
using System.Collections;
using HslCommunication.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
#if !NET35 && !NET20
using System.Threading.Tasks;
#endif

namespace HslCommunication.Profinet.LSIS
{
	/// <summary>
	/// XGB Fast Enet I/F module supports open Ethernet. It provides network configuration that is to connect LSIS and other company PLC, PC on network
	/// </summary>
	/// <remarks>
	/// Address example likes the follow
	/// <list type="table">
	///   <listheader>
	///     <term>地址名称</term>
	///     <term>地址代号</term>
	///     <term>示例</term>
	///     <term>地址进制</term>
	///     <term>字操作</term>
	///     <term>位操作</term>
	///     <term>备注</term>
	///   </listheader>
	///   <item>
	///     <term>*</term>
	///     <term>P</term>
	///     <term>PX100,PB100,PW100,PD100,PL100</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>*</term>
	///     <term>M</term>
	///     <term>MX100,MB100,MW100,MD100,ML100</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>*</term>
	///     <term>L</term>
	///     <term>LX100,LB100,LW100,LD100,LL100</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>*</term>
	///     <term>K</term>
	///     <term>KX100,KB100,KW100,KD100,KL100</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>*</term>
	///     <term>F</term>
	///     <term>FX100,FB100,FW100,FD100,FL100</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term></term>
	///     <term>T</term>
	///     <term>TX100,TB100,TW100,TD100,TL100</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term></term>
	///     <term>C</term>
	///     <term>CX100,CB100,CW100,CD100,CL100</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term></term>
	///     <term>D</term>
	///     <term>DX100,DB100,DW100,DD100,DL100</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term></term>
	///     <term>S</term>
	///     <term>SX100,SB100,SW100,SD100,SL100</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term></term>
	///     <term>Q</term>
	///     <term>QX100,QB100,QW100,QD100,QL100</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term></term>
	///     <term>I</term>
	///     <term>IX100,IB100,IW100,ID100,IL100</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term></term>
	///     <term>N</term>
	///     <term>NX100,NB100,NW100,ND100,NL100</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term></term>
	///     <term>U</term>
	///     <term>UX100,UB100,UW100,UD100,UL100</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term></term>
	///     <term>Z</term>
	///     <term>ZX100,ZB100,ZW100,ZD100,ZL100</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term></term>
	///     <term>R</term>
	///     <term>RX100,RB100,RW100,RD100,RL100</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	/// </list>
	/// </remarks>
	public class XGBFastEnet : NetworkDeviceBase
	{
		#region Constractor

		/// <summary>
		/// Instantiate a Default object
		/// </summary>
		public XGBFastEnet( )
		{
			WordLength         = 2;
			IpAddress          = "127.0.0.1";
			Port               = 2004;
			this.ByteTransform = new RegularByteTransform( );
		}

		/// <summary>
		/// Instantiate a object by ipaddress and port
		/// </summary>
		/// <param name="ipAddress">the ip address of the plc</param>
		/// <param name="port">the port of the plc, default is 2004</param>
		public XGBFastEnet(string ipAddress, int port) : this( )
		{
			IpAddress          = ipAddress;
			Port               = port;
		}

		/// <summary>
		/// Instantiate a object by ipaddress, port, cpuType, slotNo
		/// </summary>
		/// <param name="CpuType">CpuType</param>
		/// <param name="ipAddress">the ip address of the plc</param>
		/// <param name="port">he port of the plc, default is 2004</param>
		/// <param name="slotNo">slot number</param>
		public XGBFastEnet( string CpuType, string ipAddress, int port, byte slotNo ) : this( ipAddress, port)
		{
			this.SetCpuType    = CpuType;
			this.slotNo        = slotNo;
		}

		/// <inheritdoc/>
		protected override INetMessage GetNewNetMessage( ) => new LsisFastEnetMessage( );

		#endregion

		#region Public Properties

		/// <summary>
		/// set plc
		/// </summary>
		public string SetCpuType { get; set; }

		/// <summary>
		/// CPU TYPE
		/// </summary>
		public string CpuType { get; private set; }

		/// <summary>
		/// Cpu is error
		/// </summary>
		public bool CpuError { get; private set; }

		/// <summary>
		/// RUN, STOP, ERROR, DEBUG
		/// </summary>
		public LSCpuStatus LSCpuStatus { get; private set; }

		/// <summary>
		/// FEnet I/F module’s Base No.
		/// </summary>
		public byte BaseNo
		{
			get => baseNo;
			set => baseNo = value;
		}

		/// <summary>
		/// FEnet I/F module’s Slot No.
		/// </summary>
		public byte SlotNo
		{
			get => slotNo;
			set => slotNo = value;
		}
		/// <summary>
		/// 
		/// </summary>
		public LSCpuInfo CpuInfo { get => cpuInfo; set => cpuInfo = value; }
		/// <summary>
		/// 
		/// </summary>
		public string CompanyID { get => CompanyID1; set => CompanyID1 = value; }

		#endregion

		#region Read Write Support

		/// <inheritdoc/>
		[HslMqttApi( "ReadByteArray", "" )]
		public override OperateResult<byte[]> Read(string address, ushort length)
		{
			// build read command
			OperateResult<byte[]> coreResult = BuildReadByteCommand(address, length);
			if (!coreResult.IsSuccess) return coreResult;

			// communication
			var read = ReadFromCoreServer(PackCommand(coreResult.Content));
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<byte[]>(read);

			// analysis read result
			return ExtractActualData(read.Content);
		}

		/// <inheritdoc/>
		[HslMqttApi( "WriteByteArray", "" )]
		public override OperateResult Write(string address, byte[] value)
		{
			// build write command
			OperateResult<byte[]> coreResult = BuildWriteByteCommand(address, value);
			if (!coreResult.IsSuccess) return coreResult;

			// communication
			var read = ReadFromCoreServer(PackCommand(coreResult.Content));
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<byte[]>(read);

			// analysis read result
			return ExtractActualData(read.Content);
		}

		#endregion

		#region Async Read Write Support
#if !NET35 && !NET20
		/// <inheritdoc/>
		public override async Task<OperateResult<byte[]>> ReadAsync( string address, ushort length )
		{
			// build read command
			OperateResult<byte[]> coreResult = BuildReadByteCommand( address, length );
			if (!coreResult.IsSuccess) return coreResult;

			// communication
			var read = await ReadFromCoreServerAsync( PackCommand( coreResult.Content ) );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( read );

			// analysis read result
			return ExtractActualData( read.Content );
		}

		/// <inheritdoc/>
		public override async Task<OperateResult> WriteAsync( string address, byte[] value )
		{
			// build write command
			OperateResult<byte[]> coreResult = BuildWriteByteCommand( address, value );
			if (!coreResult.IsSuccess) return coreResult;

			// communication
			var read = await ReadFromCoreServerAsync( PackCommand( coreResult.Content ) );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( read );

			// analysis read result
			return ExtractActualData( read.Content );
		}
#endif
		#endregion

		#region Read Write Byte

		/// <inheritdoc/>
		[HslMqttApi( "ReadBoolArray", "" )]
		public override OperateResult<bool[]> ReadBool(string address, ushort length)
		{
			// build read command
			OperateResult<byte[]> coreResult = BuildReadByteCommand(address, length);
			if (!coreResult.IsSuccess) return OperateResult.CreateFailedResult<bool[]>(coreResult);

			// communication
			var read = ReadFromCoreServer(PackCommand(coreResult.Content));
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<bool[]>(read);

			OperateResult<byte[]> extract = ExtractActualData(read.Content);
			if (!extract.IsSuccess) return OperateResult.CreateFailedResult<bool[]>(extract);

			return OperateResult.CreateSuccessResult(SoftBasic.ByteToBoolArray(extract.Content, length));
		}

		/// <inheritdoc/>
		[HslMqttApi( "ReadBool", "" )]
		public override OperateResult<bool> ReadBool( string address )
		{
			// build read command
			OperateResult<byte[]> coreResult = BuildReadIndividualCommand( 0x00, address );
			if (!coreResult.IsSuccess) return OperateResult.CreateFailedResult<bool>( coreResult );

			// communication
			var read = ReadFromCoreServer( PackCommand( coreResult.Content ) );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<bool>( read );

			OperateResult<byte[]> extract = ExtractActualData( read.Content );
			if (!extract.IsSuccess) return OperateResult.CreateFailedResult<bool>( extract );

			return OperateResult.CreateSuccessResult( SoftBasic.ByteToBoolArray( extract.Content, 1 )[0] );
		}

		/// <summary>
		/// ReadCoil
		/// </summary>
		/// <param name="address">Start address</param>
		/// <returns>Whether to read the successful</returns>
		public OperateResult<bool> ReadCoil(string address) => ReadBool(address);

		/// <summary>
		/// ReadCoil
		/// </summary>
		/// <param name="address">Start address</param>
		/// <param name="length">read address length</param>
		/// <returns>Whether to read the successful</returns>
		public OperateResult<bool[]> ReadCoil(string address, ushort length) => ReadBool(address, length);

		/// <summary>
		/// Read single byte value from plc
		/// </summary>
		/// <param name="address">Start address</param>
		/// <returns>Whether to write the successful</returns>
		[HslMqttApi( "ReadByte", "" )]
		public OperateResult<byte> ReadByte( string address ) => ByteTransformHelper.GetResultFromArray( Read( address, 1 ) );

		/// <summary>
		/// Write single byte value to plc
		/// </summary>
		/// <param name="address">Start address</param>
		/// <param name="value">value</param>
		/// <returns>Whether to write the successful</returns>
		[HslMqttApi( "WriteByte", "" )]
		public OperateResult Write(string address, byte value) => Write(address, new byte[] { value });

		/// <summary>
		/// WriteCoil
		/// </summary>
		/// <param name="address">Start address</param>
		/// <param name="value">bool value</param>
		/// <returns>Whether to write the successful</returns>
		public OperateResult WriteCoil(string address, bool value) => Write(address, new byte[] { (byte)(value == true ? 0x01 : 0x00), 0x00 });

		/// <summary>
		/// WriteCoil
		/// </summary>
		/// <param name="address">Start address</param>
		/// <param name="value">bool value</param>
		/// <returns>Whether to write the successful</returns>
		[HslMqttApi( "WriteBool", "" )]
		public override OperateResult Write( string address, bool value ) => WriteCoil( address, value );

		#endregion

		#region Async Read Write Byte
#if !NET35 && !NET20
		/// <inheritdoc/>
		public override async Task<OperateResult<bool[]>> ReadBoolAsync( string address, ushort length )
		{
			// build read command
			OperateResult<byte[]> coreResult = BuildReadByteCommand( address, length );
			if (!coreResult.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( coreResult );

			// communication
			var read = await ReadFromCoreServerAsync( PackCommand( coreResult.Content ) );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( read );

			OperateResult<byte[]> extract = ExtractActualData( read.Content );
			if (!extract.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( extract );

			return OperateResult.CreateSuccessResult( SoftBasic.ByteToBoolArray( extract.Content, length ) );

		}

		/// <inheritdoc/>
		public async override Task<OperateResult<bool>> ReadBoolAsync( string address )
		{
			// build read command
			OperateResult<byte[]> coreResult = BuildReadIndividualCommand( 0x00, address );
			if (!coreResult.IsSuccess) return OperateResult.CreateFailedResult<bool>( coreResult );

			// communication
			var read = await ReadFromCoreServerAsync( PackCommand( coreResult.Content ) );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<bool>( read );

			OperateResult<byte[]> extract = ExtractActualData( read.Content );
			if (!extract.IsSuccess) return OperateResult.CreateFailedResult<bool>( extract );

			return OperateResult.CreateSuccessResult( SoftBasic.ByteToBoolArray( extract.Content, 1 )[0] );
		}

		/// <inheritdoc cref="ReadCoil(string)"/>
		public async Task<OperateResult<bool>> ReadCoilAsync( string address ) => await ReadBoolAsync( address );

		/// <inheritdoc cref="ReadCoil(string, ushort)"/>
		public async Task<OperateResult<bool[]>> ReadCoilAsync( string address, ushort length ) => await ReadBoolAsync( address, length );

		/// <inheritdoc cref="ReadByte(string)"/>
		public async Task<OperateResult<byte>> ReadByteAsync( string address ) => ByteTransformHelper.GetResultFromArray( await ReadAsync( address, 1 ) );

		/// <inheritdoc cref="Write(string, byte)"/>
		public async Task<OperateResult> WriteAsync( string address, byte value ) => await WriteAsync( address, new byte[] { value } );

		/// <inheritdoc cref="WriteCoil(string, bool)"/>
		public async Task<OperateResult> WriteCoilAsync( string address, bool value ) => await WriteAsync( address, new byte[] { (byte)(value == true ? 0x01 : 0x00), 0x00 } );

		/// <inheritdoc cref="Write(string, bool)"/>
		public override async Task<OperateResult> WriteAsync( string address, bool value ) => await WriteCoilAsync( address, value );
#endif
		#endregion

		#region Private Method

		private byte[] PackCommand(byte[] coreCommand)
		{
			byte[] command = new byte[coreCommand.Length + 20];
			Encoding.ASCII.GetBytes(CompanyID).CopyTo(command, 0);
			switch (cpuInfo)
			{
				case LSCpuInfo.XGK: command[12] = 0xA0; break;
				case LSCpuInfo.XGI: command[12] = 0xA4; break;
				case LSCpuInfo.XGR: command[12] = 0xA8; break;
				case LSCpuInfo.XGB_MK: command[12] = 0xB0; break;
				case LSCpuInfo.XGB_IEC: command[12] = 0xB4; break;
				default: break;
			}
			command[13] = 0x33;
			BitConverter.GetBytes((short)coreCommand.Length).CopyTo(command, 16);
			command[18] = (byte)(baseNo * 16 + slotNo);

			int count = 0;
			for (int i = 0; i < 19; i++)
			{
				count += command[i];
			}
			command[19] = (byte)count;

			coreCommand.CopyTo(command, 20);

			string hex = SoftBasic.ByteToHexString(command, ' ');
			return command;
		}

		#endregion

		#region Override

		/// <inheritdoc/>
		public override string ToString( ) => $"XGBFastEnet[{IpAddress}:{Port}]";

		#endregion

		#region Const Value

		private string CompanyID1 = "LSIS-XGT";
		// private string CompanyID2 = "LGIS-GLOGA";
		private LSCpuInfo cpuInfo = LSCpuInfo.XGK;

		private byte baseNo = 0;
		private byte slotNo = 3;

		#endregion

		#region Static Helper

		/// <summary>
		/// 需要传入 MX100.2 的 100.2 部分，返回的是
		/// AnalysisAddress IX0.0.0 QX0.0.0  MW1.0  MB1.0
		/// </summary>
		/// <param name="address">start address</param>
		/// <param name="QI">is Q or I data</param>
		/// <returns>int address</returns>
		public static int CalculateAddressStarted( string address, bool QI = false )
		{
			if (address.IndexOf( '.' ) < 0)
			{
				return Convert.ToInt32( address );
			}
			else
			{
				string[] temp = address.Split( '.' );
				if (!QI)
					return Convert.ToInt32( temp[0] );
				else if (temp.Length >= 4)
					return Convert.ToInt32( temp[3] );
				else
					return Convert.ToInt32( temp[2] );
			}
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
		/// 所有支持的地址信息
		/// </summary>
		public static string AddressTypes = "PMLKFTCDSQINUZR";

		/// <summary>
		/// AnalysisAddress
		/// </summary>
		/// <param name="address">start address</param>
		/// <param name="IsReadWrite">is read or write operate</param>
		/// <returns>analysis result</returns>
		public static OperateResult<string> AnalysisAddress( string address, bool IsReadWrite )
		{
			// P,M,L,K,F,T
			// P,M,L,K,F,T,C,D,S
			StringBuilder sb = new StringBuilder( );
			try
			{
				sb.Append( "%" );
				bool exsist = false;
				if (IsReadWrite)
				{
					for (int i = 0; i < AddressTypes.Length; i++)
					{
						if (AddressTypes[i] == address[0])
						{
							sb.Append( AddressTypes[i] );
							switch (address[1])
							{
								case 'X':
									sb.Append( "X" );
									if (address[0] == 'I' || address[0] == 'Q' || address[0] == 'U')
									{
										sb.Append( CalculateAddressStarted( address.Substring( 2 ), true ) );
									}
									else
									{
										if (IsHex( address.Substring( 2 ) )) { sb.Append( address.Substring( 2 ) ); }
										else sb.Append( CalculateAddressStarted( address.Substring( 2 ) ) );
									}

									break;
								default:
									sb.Append( "B" );
									int startIndex = 0;
									if (address[1] == 'B')
									{
										if (address[0] == 'I' || address[0] == 'Q' || address[0] == 'U')
										{
											startIndex = CalculateAddressStarted( address.Substring( 2 ), true );
										}
										else
										{
											startIndex = CalculateAddressStarted( address.Substring( 2 ) );
										}
										sb.Append( startIndex == 0 ? startIndex : startIndex *= 2 );
									}
									else if (address[1] == 'W')
									{
										if (address[0] == 'I' || address[0] == 'Q' || address[0] == 'U')
										{
											startIndex = CalculateAddressStarted( address.Substring( 2 ), true );
										}
										else
										{
											startIndex = CalculateAddressStarted( address.Substring( 2 ) );
										}
										sb.Append( startIndex == 0 ? startIndex : startIndex *= 2 );
									}
									else if (address[1] == 'D')
									{
										startIndex = CalculateAddressStarted( address.Substring( 2 ) );
										sb.Append( startIndex == 0 ? startIndex : startIndex *= 4 );
									}
									else if (address[1] == 'L')
									{
										startIndex = CalculateAddressStarted( address.Substring( 2 ) );
										sb.Append( startIndex == 0 ? startIndex : startIndex *= 8 );
									}
									else
									{
										if (address[0] == 'I' || address[0] == 'Q' || address[0] == 'U')
										{
											sb.Append( CalculateAddressStarted( address.Substring( 1 ), true ) );
										}
										else
										{
											if (IsHex( address.Substring( 1 ) )) { sb.Append( address.Substring( 1 ) ); }
											else sb.Append( CalculateAddressStarted( address.Substring( 1 ) ) );
										}

									}

									break;
							}
							exsist = true;
							break;
						}
					}
				}
				else
				{
					sb.Append( address );
					exsist = true;
				}
				if (!exsist) throw new Exception( StringResources.Language.NotSupportedDataType );
			}
			catch (Exception ex)
			{
				return new OperateResult<string>( ex.Message );
			}

			return OperateResult.CreateSuccessResult( sb.ToString( ) );
		}

		/// <summary>
		/// Get DataType to Address
		/// </summary>
		/// <param name="address">address</param>
		/// <returns>dataType</returns>
		public static OperateResult<string> GetDataTypeToAddress(string address)
		{
			string lSDataType = string.Empty; ;
			try
			{
				char[] types = new char[] { 'P', 'M', 'L', 'K', 'F', 'T', 'C', 'D', 'S', 'Q', 'I', 'R' };
				bool exsist = false;

				for (int i = 0; i < types.Length; i++)
				{
					if (types[i] == address[0])
					{
						switch (address[1])
						{
							case 'X':
								lSDataType = "Bit";
								break;
							case 'W':
								lSDataType = "Word";
								break;
							case 'D':
								lSDataType = "DWord";
								break;
							case 'L':
								lSDataType = "LWord";
								break;
							case 'B':
								lSDataType = "Continuous";
								break;

							default:
								lSDataType = "Continuous";
								break;
						}

						exsist = true;
						break;
					}
				}

				if (!exsist) throw new Exception(StringResources.Language.NotSupportedDataType);
			}
			catch (Exception ex)
			{
				return new OperateResult<string>(ex.Message);
			}

			return OperateResult.CreateSuccessResult(lSDataType);

		}

		/// <inheritdoc cref="BuildReadIndividualCommand(byte,string[])"/>
		public static OperateResult<byte[]> BuildReadIndividualCommand( byte dataType, string address )
		{
			return BuildReadIndividualCommand( dataType, new string[] { address } );
		}

		/// <summary>
		/// Multi reading address Type of Read Individual
		/// </summary>
		/// <param name="dataType">dataType bit:0x04, byte:0x01, word:0x02, dword:0x03, lword:0x04, continuous:0x14</param>
		/// <param name="addresses">address, for example: MX100, PX100</param>
		/// <returns>Read Individual Command</returns>
		public static OperateResult<byte[]> BuildReadIndividualCommand( byte dataType, string[] addresses )
		{
			MemoryStream ms = new MemoryStream( );
			ms.WriteByte( 0x54 );    // read
			ms.WriteByte( 0x00 );
			ms.WriteByte( dataType );
			ms.WriteByte( 0x00 );
			ms.WriteByte( 0x00 );    // Reserved
			ms.WriteByte( 0x00 );
			ms.WriteByte( (byte)addresses.Length );    // Block No         ?? i don't know what is the meaning
			ms.WriteByte( 0x00 );
			foreach (string address in addresses)
			{
				var analysisResult = AnalysisAddress( address, true );
				if (!analysisResult.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( analysisResult );

				ms.WriteByte( (byte)analysisResult.Content.Length );    //  Variable Length
				ms.WriteByte( 0x00 );
				byte[] buffer = Encoding.ASCII.GetBytes( analysisResult.Content );
				ms.Write( buffer, 0, buffer.Length );
			}
			return OperateResult.CreateSuccessResult( ms.ToArray( ) );
		}

		private static OperateResult<byte[]> BuildReadByteCommand( string address, ushort length )
		{
			var analysisResult = AnalysisAddress( address, true );
			if (!analysisResult.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( analysisResult );
			var DataTypeResult = GetDataTypeToAddress( address );
			if (!DataTypeResult.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( DataTypeResult );

			byte[] command = new byte[12 + analysisResult.Content.Length];

			switch (DataTypeResult.Content)
			{
				case "Bit":
					command[2] = 0x00; break;
				case "Word":
				case "DWord":
				case "LWord":
				case "Continuous":
					command[2] = 0x14; break; // continuous reading
				default: break;
			}
			command[0] = 0x54;    // read
			command[1] = 0x00;
			command[2] = 0x00;
			command[3] = 0x00;
			command[4] = 0x00;    // Reserved
			command[5] = 0x00;
			command[6] = 0x01;    // Block No         ?? i don't know what is the meaning
			command[7] = 0x00;
			command[8] = (byte)analysisResult.Content.Length;    //  Variable Length
			command[9] = 0x00;

			Encoding.ASCII.GetBytes( analysisResult.Content ).CopyTo( command, 10 );
			BitConverter.GetBytes( length ).CopyTo( command, command.Length - 2 );

			return OperateResult.CreateSuccessResult( command );
		}

		private OperateResult<byte[]> BuildWriteByteCommand(string address, byte[] data)
		{
			OperateResult<string> analysisResult;
			switch (SetCpuType)
			{
				case "XGK":
					// PLC XGK WriteByte And ReadByte
					analysisResult = AnalysisAddress(address, true);
					break;
				case "XGB":
					//PLC XGB WriteWord And ReadByte
					analysisResult = AnalysisAddress(address, false);
					break;
				default:
					// PLC XGK WriteByte And ReadByte
					analysisResult = AnalysisAddress(address, true);
					break;
			}
			if (!analysisResult.IsSuccess) return OperateResult.CreateFailedResult<byte[]>(analysisResult);
			var DataTypeResult = GetDataTypeToAddress(address);
			if (!DataTypeResult.IsSuccess) return OperateResult.CreateFailedResult<byte[]>(DataTypeResult);

			byte[] command = new byte[12 + analysisResult.Content.Length + data.Length];

			switch (DataTypeResult.Content)
			{
				case "Bit":
				case "Byte":
					command[2] = 0x01; break;
				case "Word":
					command[2] = 0x02; break;
				case "DWord": command[2] = 0x03; break;
				case "LWord": command[2] = 0x04; break;
				case "Continuous": command[2] = 0x14; break;
				default: break;
			}
			command[0] = 0x58;    // write
			command[1] = 0x00;
			command[3] = 0x00;
			command[4] = 0x00;    // Reserved
			command[5] = 0x00;
			command[6] = 0x01;    // Block No         ?? i don't know what is the meaning
			command[7] = 0x00;
			command[8] = (byte)analysisResult.Content.Length;    //  Variable Length
			command[9] = 0x00;

			Encoding.ASCII.GetBytes(analysisResult.Content).CopyTo(command, 10);
			BitConverter.GetBytes(data.Length).CopyTo(command, command.Length - 2 - data.Length);
			data.CopyTo(command, command.Length - data.Length);

			return OperateResult.CreateSuccessResult(command);
		}

		/// <summary>
		/// Returns true data content, supports read and write returns
		/// </summary>
		/// <param name="response">response data</param>
		/// <returns>real data</returns>
		public OperateResult<byte[]> ExtractActualData(byte[] response)
		{
			if (response.Length < 20) return new OperateResult<byte[]>("Length is less than 20:" + SoftBasic.ByteToHexString(response));

			ushort plcInfo = BitConverter.ToUInt16(response, 10);
			BitArray array_plcInfo = new BitArray(BitConverter.GetBytes(plcInfo));
			var p = plcInfo % 32;
			switch (plcInfo % 32)
			{
				case 1: CpuType = "XGK/R-CPUH"; break;
				case 2: CpuType = "XGK-CPUS"; break;
				case 4: CpuType = "XGK-CPUE"; break;
				case 5: CpuType = "XGK/R-CPUH"; break;
				case 6: CpuType = "XGB/XBCU"; break;
			}

			CpuError = array_plcInfo[7];
			if (array_plcInfo[8]) LSCpuStatus = LSCpuStatus.RUN;
			if (array_plcInfo[9]) LSCpuStatus = LSCpuStatus.STOP;
			if (array_plcInfo[10]) LSCpuStatus = LSCpuStatus.ERROR;
			if (array_plcInfo[11]) LSCpuStatus = LSCpuStatus.DEBUG;

			if (response.Length < 28) return new OperateResult<byte[]>("Length is less than 28:" + SoftBasic.ByteToHexString(response));
			ushort error = BitConverter.ToUInt16(response, 26);
			if (error > 0) return new OperateResult<byte[]>(response[28], "Error:" + GetErrorDesciption(response[28]));

			if (response[20] == 0x59) return OperateResult.CreateSuccessResult(new byte[0]);  // write

			if (response[20] == 0x55)  // read
			{
				try
				{
					ushort length = BitConverter.ToUInt16(response, 30);
					byte[] content = new byte[length];
					Array.Copy(response, 32, content, 0, length);
					return OperateResult.CreateSuccessResult(content);
				}
				catch (Exception ex)
				{
					return new OperateResult<byte[]>(ex.Message);
				}
			}

			return new OperateResult<byte[]>(StringResources.Language.NotSupportedFunction);
		}

		/// <summary>
		/// get the description of the error code meanning
		/// </summary>
		/// <param name="code">code value</param>
		/// <returns>string information</returns>
		public static string GetErrorDesciption(byte code)
		{
			switch (code)
			{
				case 0: return "Normal";
				case 1: return "Physical layer error (TX, RX unavailable)";
				case 3: return "There is no identifier of Function Block to receive in communication channel";
				case 4: return "Mismatch of data type";
				case 5: return "Reset is received from partner station";
				case 6: return "Communication instruction of partner station is not ready status";
				case 7: return "Device status of remote station is not desirable status";
				case 8: return "Access to some target is not available";
				case 9: return "Can’ t deal with communication instruction of partner station by too many reception";
				case 10: return "Time Out error";
				case 11: return "Structure error";
				case 12: return "Abort";
				case 13: return "Reject(local/remote)";
				case 14: return "Communication channel establishment error (Connect/Disconnect)";
				case 15: return "High speed communication and connection service error";
				case 33: return "Can’t find variable identifier";
				case 34: return "Address error";
				case 50: return "Response error";
				case 113: return "Object Access Unsupported";
				case 187: return "Unknown error code (communication code of other company) is received";
				default: return "Unknown error";
			}
		}

		#endregion

	}
}
