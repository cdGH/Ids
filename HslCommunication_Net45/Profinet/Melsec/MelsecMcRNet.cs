using HslCommunication.Core;
using HslCommunication.Core.Address;
using HslCommunication.Core.IMessage;
using HslCommunication.Core.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HslCommunication.Reflection;
using HslCommunication.Profinet.Melsec.Helper;
#if !NET35 && !NET20
using System.Threading.Tasks;
#endif

namespace HslCommunication.Profinet.Melsec
{
	/// <summary>
	/// 三菱的R系列的MC协议，支持的地址类型和 <see cref="MelsecMcNet"/> 有区别，详细请查看对应的API文档说明
	/// </summary>
	public class MelsecMcRNet : NetworkDeviceBase, IReadWriteMc
	{
		#region Constructor

		/// <summary>
		/// 实例化三菱R系列的Qna兼容3E帧协议的通讯对象<br />
		/// Instantiate the communication object of Mitsubishi's Qna compatible 3E frame protocol
		/// </summary>
		public MelsecMcRNet( )
		{
			this.WordLength    = 1;
			this.ByteTransform = new RegularByteTransform( );
		}

		/// <summary>
		/// 指定ip地址和端口号来实例化一个默认的对象<br />
		/// Specify the IP address and port number to instantiate a default object
		/// </summary>
		/// <param name="ipAddress">PLC的Ip地址</param>
		/// <param name="port">PLC的端口</param>
		public MelsecMcRNet( string ipAddress, int port )
		{
			this.WordLength    = 1;
			this.IpAddress     = ipAddress;
			this.Port          = port;
			this.ByteTransform = new RegularByteTransform( );
		}

		/// <inheritdoc/>
		protected override INetMessage GetNewNetMessage( ) => new MelsecQnA3EBinaryMessage( );

		/// <inheritdoc cref="IReadWriteMc.McType"/>
		public McType McType => McType.McBinary;

		#endregion

		#region Public Member

		/// <inheritdoc cref="MelsecMcNet.NetworkNumber"/>
		public byte NetworkNumber { get; set; } = 0x00;

		/// <inheritdoc cref="MelsecMcNet.NetworkStationNumber"/>
		public byte NetworkStationNumber { get; set; } = 0x00;

		#endregion

		#region Virtual Address Analysis

		/// <inheritdoc cref="IReadWriteMc.McAnalysisAddress(string, ushort)"/>
		public virtual OperateResult<McAddressData> McAnalysisAddress( string address, ushort length ) => McAddressData.ParseMelsecRFrom( address, length );

		/// <inheritdoc/>
		protected override byte[] PackCommandWithHeader( byte[] command )
		{
			return McBinaryHelper.PackMcCommand( command, this.NetworkNumber, this.NetworkStationNumber );
		}

		/// <inheritdoc/>
		protected override OperateResult<byte[]> UnpackResponseContent( byte[] send, byte[] response )
		{
			OperateResult check = McBinaryHelper.CheckResponseContentHelper( response );
			if (!check.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( check );

			return OperateResult.CreateSuccessResult( response.RemoveBegin( 11 ) );
		}

		/// <inheritdoc cref="IReadWriteMc.ExtractActualData(byte[], bool)"/>
		public byte[] ExtractActualData( byte[] response, bool isBit ) => McBinaryHelper.ExtractActualDataHelper( response, isBit );

		#endregion

		#region Read Write Support

		/// <inheritdoc/>
		[HslMqttApi( "ReadByteArray", "" )]
		public override OperateResult<byte[]> Read( string address, ushort length ) => McHelper.Read( this, address, length );

		/// <inheritdoc/>
		[HslMqttApi( "WriteByteArray", "" )]
		public override OperateResult Write( string address, byte[] value ) => McHelper.Write( this, address, value );

#if !NET35 && !NET20
		/// <inheritdoc/>
		public override async Task<OperateResult<byte[]>> ReadAsync( string address, ushort length ) => await McHelper.ReadAsync( this, address, length );

		/// <inheritdoc/>
		public override async Task<OperateResult> WriteAsync( string address, byte[] value ) => await McHelper.WriteAsync( this, address, value );
#endif
		#endregion

		#region Read Random

		/// <inheritdoc cref="McHelper.ReadRandom(IReadWriteMc, string[])"/>
		[HslMqttApi( "随机读取PLC的数据信息，可以跨地址，跨类型组合，但是每个地址只能读取一个word，也就是2个字节的内容。收到结果后，需要自行解析数据" )]
		public OperateResult<byte[]> ReadRandom( string[] address ) => McHelper.ReadRandom( this, address );

		/// <inheritdoc cref="McHelper.ReadRandom(IReadWriteMc, string[], ushort[])"/>
		[HslMqttApi( ApiTopic = "ReadRandoms", Description = "随机读取PLC的数据信息，可以跨地址，跨类型组合，每个地址是任意的长度。收到结果后，需要自行解析数据，目前只支持字地址，比如D区，W区，R区，不支持X，Y，M，B，L等等" )]
		public OperateResult<byte[]> ReadRandom( string[] address, ushort[] length ) => McHelper.ReadRandom( this, address, length );

		/// <inheritdoc cref="McHelper.ReadRandomInt16(IReadWriteMc, string[])"/>
		public OperateResult<short[]> ReadRandomInt16( string[] address ) => McHelper.ReadRandomInt16( this, address );

		/// <inheritdoc cref="McHelper.ReadRandomUInt16(IReadWriteMc, string[])"/>
		public OperateResult<ushort[]> ReadRandomUInt16( string[] address ) => McHelper.ReadRandomUInt16( this, address );

#if !NET35 && !NET20
		/// <inheritdoc cref="ReadRandom(string[])"/>
		public async Task<OperateResult<byte[]>> ReadRandomAsync( string[] address ) => await McHelper.ReadRandomAsync( this, address );

		/// <inheritdoc cref="ReadRandom(string[], ushort[])"/>
		public async Task<OperateResult<byte[]>> ReadRandomAsync( string[] address, ushort[] length ) => await McHelper.ReadRandomAsync( this, address, length );

		/// <inheritdoc cref="ReadRandomInt16(string[])"/>
		public async Task<OperateResult<short[]>> ReadRandomInt16Async( string[] address ) => await McHelper.ReadRandomInt16Async( this, address );

		/// <inheritdoc cref="ReadRandomUInt16(string[])"/>
		public async Task<OperateResult<ushort[]>> ReadRandomUInt16Async( string[] address ) => await McHelper.ReadRandomUInt16Async( this, address );
#endif
		#endregion

		#region Bool Operate Support

		/// <inheritdoc/>
		[HslMqttApi( "ReadBoolArray", "" )]
		public override OperateResult<bool[]> ReadBool( string address, ushort length ) => McHelper.ReadBool( this, address, length );

		/// <inheritdoc/>
		[HslMqttApi( "WriteBoolArray", "" )]
		public override OperateResult Write( string address, bool[] values ) => McHelper.Write( this, address, values );

#if !NET35 && !NET20
		/// <inheritdoc cref="ReadBool(string, ushort)"/>
		public override async Task<OperateResult<bool[]>> ReadBoolAsync( string address, ushort length ) => await McHelper.ReadBoolAsync( this, address, length );

		/// <inheritdoc cref="Write(string, bool[])"/>
		public override async Task<OperateResult> WriteAsync( string address, bool[] values ) => await McHelper.WriteAsync( this, address, values );
#endif
		#endregion

		#region Object Override

		/// <inheritdoc/>
		public override string ToString( ) => $"MelsecMcRNet[{IpAddress}:{Port}]";

		#endregion

		#region Static Helper

		/// <summary>
		/// 分析三菱R系列的地址，并返回解析后的数据对象
		/// </summary>
		/// <param name="address">字符串地址</param>
		/// <returns>是否解析成功</returns>
		public static OperateResult<MelsecMcDataType,int> AnalysisAddress( string address )
		{
			try
			{
				if      (address.StartsWith( "LSTS" )) return OperateResult.CreateSuccessResult( MelsecMcDataType.R_LSTS, Convert.ToInt32( address.Substring( 4 ), MelsecMcDataType.R_LSTS.FromBase ) );
				else if (address.StartsWith( "LSTC" )) return OperateResult.CreateSuccessResult( MelsecMcDataType.R_LSTC, Convert.ToInt32( address.Substring( 4 ), MelsecMcDataType.R_LSTC.FromBase ) );
				else if (address.StartsWith( "LSTN" )) return OperateResult.CreateSuccessResult( MelsecMcDataType.R_LSTN, Convert.ToInt32( address.Substring( 4 ), MelsecMcDataType.R_LSTN.FromBase ) );
				else if (address.StartsWith( "STS" ))  return OperateResult.CreateSuccessResult( MelsecMcDataType.R_STS,  Convert.ToInt32( address.Substring( 3 ), MelsecMcDataType.R_STS.FromBase ) );
				else if (address.StartsWith( "STC" ))  return OperateResult.CreateSuccessResult( MelsecMcDataType.R_STC,  Convert.ToInt32( address.Substring( 3 ), MelsecMcDataType.R_STC.FromBase ) );
				else if (address.StartsWith( "STN" ))  return OperateResult.CreateSuccessResult( MelsecMcDataType.R_STN,  Convert.ToInt32( address.Substring( 3 ), MelsecMcDataType.R_STN.FromBase ) );
				else if (address.StartsWith( "LTS" ))  return OperateResult.CreateSuccessResult( MelsecMcDataType.R_LTS,  Convert.ToInt32( address.Substring( 3 ), MelsecMcDataType.R_LTS.FromBase ) );
				else if (address.StartsWith( "LTC" ))  return OperateResult.CreateSuccessResult( MelsecMcDataType.R_LTC,  Convert.ToInt32( address.Substring( 3 ), MelsecMcDataType.R_LTC.FromBase ) );
				else if (address.StartsWith( "LTN" ))  return OperateResult.CreateSuccessResult( MelsecMcDataType.R_LTN,  Convert.ToInt32( address.Substring( 3 ), MelsecMcDataType.R_LTN.FromBase ) );
				else if (address.StartsWith( "LCS" ))  return OperateResult.CreateSuccessResult( MelsecMcDataType.R_LCS,  Convert.ToInt32( address.Substring( 3 ), MelsecMcDataType.R_LCS.FromBase ) );
				else if (address.StartsWith( "LCC" ))  return OperateResult.CreateSuccessResult( MelsecMcDataType.R_LCC,  Convert.ToInt32( address.Substring( 3 ), MelsecMcDataType.R_LCC.FromBase ) );
				else if (address.StartsWith( "LCN" ))  return OperateResult.CreateSuccessResult( MelsecMcDataType.R_LCN,  Convert.ToInt32( address.Substring( 3 ), MelsecMcDataType.R_LCN.FromBase ) );
				else if (address.StartsWith( "TS" ))   return OperateResult.CreateSuccessResult( MelsecMcDataType.R_TS,   Convert.ToInt32( address.Substring( 2 ), MelsecMcDataType.R_TS.FromBase ) );
				else if (address.StartsWith( "TC" ))   return OperateResult.CreateSuccessResult( MelsecMcDataType.R_TC,   Convert.ToInt32( address.Substring( 2 ), MelsecMcDataType.R_TC.FromBase ) );
				else if (address.StartsWith( "TN" ))   return OperateResult.CreateSuccessResult( MelsecMcDataType.R_TN,   Convert.ToInt32( address.Substring( 2 ), MelsecMcDataType.R_TN.FromBase ) );
				else if (address.StartsWith( "CS" ))   return OperateResult.CreateSuccessResult( MelsecMcDataType.R_CS,   Convert.ToInt32( address.Substring( 2 ), MelsecMcDataType.R_CS.FromBase ) );
				else if (address.StartsWith( "CC" ))   return OperateResult.CreateSuccessResult( MelsecMcDataType.R_CC,   Convert.ToInt32( address.Substring( 2 ), MelsecMcDataType.R_CC.FromBase ) );
				else if (address.StartsWith( "CN" ))   return OperateResult.CreateSuccessResult( MelsecMcDataType.R_CN,   Convert.ToInt32( address.Substring( 2 ), MelsecMcDataType.R_CN.FromBase ) );
				else if (address.StartsWith( "SM" ))   return OperateResult.CreateSuccessResult( MelsecMcDataType.R_SM,   Convert.ToInt32( address.Substring( 2 ), MelsecMcDataType.R_SM.FromBase ) );
				else if (address.StartsWith( "SB" ))   return OperateResult.CreateSuccessResult( MelsecMcDataType.R_SB,   Convert.ToInt32( address.Substring( 2 ), MelsecMcDataType.R_SB.FromBase ) );
				else if (address.StartsWith( "DX" ))   return OperateResult.CreateSuccessResult( MelsecMcDataType.R_DX,   Convert.ToInt32( address.Substring( 2 ), MelsecMcDataType.R_DX.FromBase ) );
				else if (address.StartsWith( "DY" ))   return OperateResult.CreateSuccessResult( MelsecMcDataType.R_DY,   Convert.ToInt32( address.Substring( 2 ), MelsecMcDataType.R_DY.FromBase ) );
				else if (address.StartsWith( "SD" ))   return OperateResult.CreateSuccessResult( MelsecMcDataType.R_SD,   Convert.ToInt32( address.Substring( 2 ), MelsecMcDataType.R_SD.FromBase ) );
				else if (address.StartsWith( "SW" ))   return OperateResult.CreateSuccessResult( MelsecMcDataType.R_SW,   Convert.ToInt32( address.Substring( 2 ), MelsecMcDataType.R_SW.FromBase ) );
				else if (address.StartsWith( "X" ))    return OperateResult.CreateSuccessResult( MelsecMcDataType.R_X,    Convert.ToInt32( address.Substring( 1 ), MelsecMcDataType.R_X.FromBase ) );
				else if (address.StartsWith( "Y" ))    return OperateResult.CreateSuccessResult( MelsecMcDataType.R_Y,    Convert.ToInt32( address.Substring( 1 ), MelsecMcDataType.R_Y.FromBase ) );
				else if (address.StartsWith( "M" ))    return OperateResult.CreateSuccessResult( MelsecMcDataType.R_M,    Convert.ToInt32( address.Substring( 1 ), MelsecMcDataType.R_M.FromBase ) );
				else if (address.StartsWith( "L" ))    return OperateResult.CreateSuccessResult( MelsecMcDataType.R_L,    Convert.ToInt32( address.Substring( 1 ), MelsecMcDataType.R_L.FromBase ) );
				else if (address.StartsWith( "F" ))    return OperateResult.CreateSuccessResult( MelsecMcDataType.R_F,    Convert.ToInt32( address.Substring( 1 ), MelsecMcDataType.R_F.FromBase ) );
				else if (address.StartsWith( "V" ))    return OperateResult.CreateSuccessResult( MelsecMcDataType.R_V,    Convert.ToInt32( address.Substring( 1 ), MelsecMcDataType.R_V.FromBase ) );
				else if (address.StartsWith( "S" ))    return OperateResult.CreateSuccessResult( MelsecMcDataType.R_S,    Convert.ToInt32( address.Substring( 1 ), MelsecMcDataType.R_S.FromBase ) );
				else if (address.StartsWith( "B" ))    return OperateResult.CreateSuccessResult( MelsecMcDataType.R_B,    Convert.ToInt32( address.Substring( 1 ), MelsecMcDataType.R_B.FromBase ) );
				else if (address.StartsWith( "D" ))    return OperateResult.CreateSuccessResult( MelsecMcDataType.R_D,    Convert.ToInt32( address.Substring( 1 ), MelsecMcDataType.R_D.FromBase ) );
				else if (address.StartsWith( "W" ))    return OperateResult.CreateSuccessResult( MelsecMcDataType.R_W,    Convert.ToInt32( address.Substring( 1 ), MelsecMcDataType.R_W.FromBase ) );
				else if (address.StartsWith( "R" ))    return OperateResult.CreateSuccessResult( MelsecMcDataType.R_R,    Convert.ToInt32( address.Substring( 1 ), MelsecMcDataType.R_R.FromBase ) );
				else if (address.StartsWith( "Z" ))    return OperateResult.CreateSuccessResult( MelsecMcDataType.R_Z,    Convert.ToInt32( address.Substring( 1 ), MelsecMcDataType.R_Z.FromBase ) );
				else return new OperateResult<MelsecMcDataType, int>( StringResources.Language.NotSupportedDataType );
			}
			catch(Exception ex)
			{
				return new OperateResult<MelsecMcDataType, int>( ex.Message );
			}
		}
		
		/// <summary>
		/// 从三菱地址，是否位读取进行创建读取的MC的核心报文
		/// </summary>
		/// <param name="address">地址数据</param>
		/// <param name="isBit">是否进行了位读取操作</param>
		/// <returns>带有成功标识的报文对象</returns>
		public static byte[] BuildReadMcCoreCommand( McAddressData address, bool isBit )
		{
			byte[] command = new byte[12];
			command[ 0] = 0x01;                                                      // 批量读取数据命令
			command[ 1] = 0x04;
			command[ 2] = isBit ? (byte)0x01 : (byte)0x00;                           // 以点为单位还是字为单位成批读取
			command[ 3] = 0x00;
			command[ 4] = BitConverter.GetBytes( address.AddressStart )[0];          // 起始地址的地位
			command[ 5] = BitConverter.GetBytes( address.AddressStart )[1];
			command[ 6] = BitConverter.GetBytes( address.AddressStart )[2];
			command[ 7] = BitConverter.GetBytes( address.AddressStart )[3];
			command[ 8] = BitConverter.GetBytes( address.McDataType.DataCode )[0];     // 指明读取的数据
			command[ 9] = BitConverter.GetBytes( address.McDataType.DataCode )[1];
			command[10] = (byte)(address.Length % 256);                              // 软元件的长度
			command[11] = (byte)(address.Length / 256);

			return command;
		}

		/// <summary>
		/// 以字为单位，创建数据写入的核心报文
		/// </summary>
		/// <param name="address">三菱的数据地址</param>
		/// <param name="value">实际的原始数据信息</param>
		/// <returns>带有成功标识的报文对象</returns>
		public static byte[] BuildWriteWordCoreCommand( McAddressData address, byte[] value )
		{
			if (value == null) value = new byte[0];
			byte[] command = new byte[12 + value.Length];
			command[ 0] = 0x01;                                                        // 批量写入数据命令
			command[ 1] = 0x14;
			command[ 2] = 0x00;                                                        // 以字为单位成批读取
			command[ 3] = 0x00;
			command[ 4] = BitConverter.GetBytes( address.AddressStart )[0];            // 起始地址的地位
			command[ 5] = BitConverter.GetBytes( address.AddressStart )[1];
			command[ 6] = BitConverter.GetBytes( address.AddressStart )[2];
			command[ 7] = BitConverter.GetBytes( address.AddressStart )[3];
			command[ 8] = BitConverter.GetBytes( address.McDataType.DataCode )[0];     // 指明读取的数据
			command[ 9] = BitConverter.GetBytes( address.McDataType.DataCode )[1];
			command[10] = (byte)(value.Length / 2 % 256);                              // 软元件长度的地位
			command[11] = (byte)(value.Length / 2 / 256);
			value.CopyTo( command, 12 );

			return command;
		}

		/// <summary>
		/// 以位为单位，创建数据写入的核心报文
		/// </summary>
		/// <param name="address">三菱的地址信息</param>
		/// <param name="value">原始的bool数组数据</param>
		/// <returns>带有成功标识的报文对象</returns>
		public static byte[] BuildWriteBitCoreCommand( McAddressData address, bool[] value )
		{
			if (value == null) value = new bool[0];
			byte[] buffer = MelsecHelper.TransBoolArrayToByteData( value );
			byte[] command = new byte[12 + buffer.Length];
			command[ 0] = 0x01;                                                        // 批量写入数据命令
			command[ 1] = 0x14;
			command[ 2] = 0x01;                                                        // 以位为单位成批写入
			command[ 3] = 0x00;
			command[ 4] = BitConverter.GetBytes( address.AddressStart )[0];            // 起始地址的地位
			command[ 5] = BitConverter.GetBytes( address.AddressStart )[1];
			command[ 6] = BitConverter.GetBytes( address.AddressStart )[2];
			command[ 7] = BitConverter.GetBytes( address.AddressStart )[3];
			command[ 8] = BitConverter.GetBytes( address.McDataType.DataCode )[0];     // 指明读取的数据
			command[ 9] = BitConverter.GetBytes( address.McDataType.DataCode )[1];
			command[10] = (byte)(value.Length % 256);                                  // 软元件长度的地位
			command[11] = (byte)(value.Length / 256);
			buffer.CopyTo( command, 12 );

			return command;
		}

		#endregion
	}
}
