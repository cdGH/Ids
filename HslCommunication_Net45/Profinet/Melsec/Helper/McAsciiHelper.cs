using HslCommunication.BasicFramework;
using HslCommunication.Core.Address;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
#if !NET20 && !NET35
using System.Threading.Tasks;
#endif

namespace HslCommunication.Profinet.Melsec.Helper
{
	/// <summary>
	/// 基于MC协议的ASCII格式的辅助类
	/// </summary>
	public class McAsciiHelper
	{
		/// <summary>
		/// 将MC协议的核心报文打包成一个可以直接对PLC进行发送的原始报文
		/// </summary>
		/// <param name="mcCore">MC协议的核心报文</param>
		/// <param name="networkNumber">网络号</param>
		/// <param name="networkStationNumber">网络站号</param>
		/// <returns>原始报文信息</returns>
		public static byte[] PackMcCommand( byte[] mcCore, byte networkNumber = 0, byte networkStationNumber = 0 )
		{
			byte[] plcCommand = new byte[22 + mcCore.Length];
			plcCommand[ 0] = 0x35;                                                                        // 副标题
			plcCommand[ 1] = 0x30;
			plcCommand[ 2] = 0x30;
			plcCommand[ 3] = 0x30;
			plcCommand[ 4] = SoftBasic.BuildAsciiBytesFrom( networkNumber )[0];                         // 网络号
			plcCommand[ 5] = SoftBasic.BuildAsciiBytesFrom( networkNumber )[1];
			plcCommand[ 6] = 0x46;                                                                        // PLC编号
			plcCommand[ 7] = 0x46;
			plcCommand[ 8] = 0x30;                                                                        // 目标模块IO编号
			plcCommand[ 9] = 0x33;
			plcCommand[10] = 0x46;
			plcCommand[11] = 0x46;
			plcCommand[12] = SoftBasic.BuildAsciiBytesFrom( networkStationNumber )[0];                  // 目标模块站号
			plcCommand[13] = SoftBasic.BuildAsciiBytesFrom( networkStationNumber )[1];
			plcCommand[14] = SoftBasic.BuildAsciiBytesFrom( (ushort)(plcCommand.Length - 18) )[0];     // 请求数据长度
			plcCommand[15] = SoftBasic.BuildAsciiBytesFrom( (ushort)(plcCommand.Length - 18) )[1];
			plcCommand[16] = SoftBasic.BuildAsciiBytesFrom( (ushort)(plcCommand.Length - 18) )[2];
			plcCommand[17] = SoftBasic.BuildAsciiBytesFrom( (ushort)(plcCommand.Length - 18) )[3];
			plcCommand[18] = 0x30;                                                                        // CPU监视定时器
			plcCommand[19] = 0x30;
			plcCommand[20] = 0x31;
			plcCommand[21] = 0x30;
			mcCore.CopyTo( plcCommand, 22 );

			return plcCommand;
		}
		
		/// <summary>
		/// 从PLC反馈的数据中提取出实际的数据内容，需要传入反馈数据，是否位读取
		/// </summary>
		/// <param name="response">反馈的数据内容</param>
		/// <param name="isBit">是否位读取</param>
		/// <returns>解析后的结果对象</returns>
		public static byte[] ExtractActualDataHelper( byte[] response, bool isBit )
		{
			if (isBit)
				return response.Select( m => m == 0x30 ? (byte)0x00 : (byte)0x01 ).ToArray( );
			else
				return MelsecHelper.TransAsciiByteArrayToByteArray( response );
		}

		/// <summary>
		/// 检查反馈的内容是否正确的
		/// </summary>
		/// <param name="content">MC的反馈的内容</param>
		/// <returns>是否正确</returns>
		public static OperateResult CheckResponseContent( byte[] content )
		{
			ushort errorCode = Convert.ToUInt16( Encoding.ASCII.GetString( content, 18, 4 ), 16 );
			if (errorCode != 0) return new OperateResult( errorCode, MelsecHelper.GetErrorDescription( errorCode ) );

			return OperateResult.CreateSuccessResult( );
		}

		/// <summary>
		/// 从三菱地址，是否位读取进行创建读取Ascii格式的MC的核心报文
		/// </summary>
		/// <param name="addressData">三菱Mc协议的数据地址</param>
		/// <param name="isBit">是否进行了位读取操作</param>
		/// <returns>带有成功标识的报文对象</returns>
		public static byte[] BuildAsciiReadMcCoreCommand( McAddressData addressData, bool isBit )
		{
			byte[] command = new byte[20];
			command[ 0] = 0x30;                                                               // 批量读取数据命令
			command[ 1] = 0x34;
			command[ 2] = 0x30;
			command[ 3] = 0x31;
			command[ 4] = 0x30;                                                               // 以点为单位还是字为单位成批读取
			command[ 5] = 0x30;
			command[ 6] = 0x30;
			command[ 7] = isBit ? (byte)0x31 : (byte)0x30;
			command[ 8] = Encoding.ASCII.GetBytes( addressData.McDataType.AsciiCode )[0];          // 软元件类型
			command[ 9] = Encoding.ASCII.GetBytes( addressData.McDataType.AsciiCode )[1];
			command[10] = MelsecHelper.BuildBytesFromAddress( addressData.AddressStart, addressData.McDataType )[0];            // 起始地址的地位
			command[11] = MelsecHelper.BuildBytesFromAddress( addressData.AddressStart, addressData.McDataType )[1];
			command[12] = MelsecHelper.BuildBytesFromAddress( addressData.AddressStart, addressData.McDataType )[2];
			command[13] = MelsecHelper.BuildBytesFromAddress( addressData.AddressStart, addressData.McDataType )[3];
			command[14] = MelsecHelper.BuildBytesFromAddress( addressData.AddressStart, addressData.McDataType )[4];
			command[15] = MelsecHelper.BuildBytesFromAddress( addressData.AddressStart, addressData.McDataType )[5];
			command[16] = SoftBasic.BuildAsciiBytesFrom( addressData.Length )[0];                                             // 软元件点数
			command[17] = SoftBasic.BuildAsciiBytesFrom( addressData.Length )[1];
			command[18] = SoftBasic.BuildAsciiBytesFrom( addressData.Length )[2];
			command[19] = SoftBasic.BuildAsciiBytesFrom( addressData.Length )[3];

			return command;
		}

		/// <summary>
		/// 从三菱扩展地址，是否位读取进行创建读取的MC的核心报文
		/// </summary>
		/// <param name="isBit">是否进行了位读取操作</param>
		/// <param name="extend">扩展指定</param>
		/// <param name="addressData">三菱Mc协议的数据地址</param>
		/// <returns>带有成功标识的报文对象</returns>
		public static byte[] BuildAsciiReadMcCoreExtendCommand( McAddressData addressData, ushort extend, bool isBit )
		{
			byte[] command = new byte[32];
			command[ 0] = 0x30;                                                               // 批量读取数据命令
			command[ 1] = 0x34;
			command[ 2] = 0x30;
			command[ 3] = 0x31;
			command[ 4] = 0x30;                                                               // 以点为单位还是字为单位成批读取
			command[ 5] = 0x30;
			command[ 6] = 0x38;
			command[ 7] = isBit ? (byte)0x31 : (byte)0x30;
			command[ 8] = 0x30;
			command[ 9] = 0x30;
			command[10] = 0x4A;                                                              // 扩展指定
			command[11] = SoftBasic.BuildAsciiBytesFrom( extend )[1];
			command[12] = SoftBasic.BuildAsciiBytesFrom( extend )[2];
			command[13] = SoftBasic.BuildAsciiBytesFrom( extend )[3];
			command[14] = 0x30;
			command[15] = 0x30;
			command[16] = 0x30;
			command[17] = Encoding.ASCII.GetBytes( addressData.McDataType.AsciiCode )[0];          // 软元件类型
			command[18] = Encoding.ASCII.GetBytes( addressData.McDataType.AsciiCode )[1];
			command[19] = MelsecHelper.BuildBytesFromAddress( addressData.AddressStart, addressData.McDataType )[0];            // 起始地址的地位
			command[20] = MelsecHelper.BuildBytesFromAddress( addressData.AddressStart, addressData.McDataType )[1];
			command[21] = MelsecHelper.BuildBytesFromAddress( addressData.AddressStart, addressData.McDataType )[2];
			command[22] = MelsecHelper.BuildBytesFromAddress( addressData.AddressStart, addressData.McDataType )[3];
			command[23] = MelsecHelper.BuildBytesFromAddress( addressData.AddressStart, addressData.McDataType )[4];
			command[24] = MelsecHelper.BuildBytesFromAddress( addressData.AddressStart, addressData.McDataType )[5];
			command[25] = 0x30;
			command[26] = 0x30;
			command[27] = 0x30;
			command[28] = SoftBasic.BuildAsciiBytesFrom( addressData.Length )[0];                                             // 软元件点数
			command[29] = SoftBasic.BuildAsciiBytesFrom( addressData.Length )[1];
			command[30] = SoftBasic.BuildAsciiBytesFrom( addressData.Length )[2];
			command[31] = SoftBasic.BuildAsciiBytesFrom( addressData.Length )[3];

			return command;
		}

		/// <summary>
		/// 以字为单位，创建ASCII数据写入的核心报文
		/// </summary>
		/// <param name="addressData">三菱Mc协议的数据地址</param>
		/// <param name="value">实际的原始数据信息</param>
		/// <returns>带有成功标识的报文对象</returns>
		public static byte[] BuildAsciiWriteWordCoreCommand( McAddressData addressData, byte[] value )
		{
			value = MelsecHelper.TransByteArrayToAsciiByteArray( value );

			byte[] command = new byte[20 + value.Length];
			command[ 0] = 0x31;                                                                                         // 批量写入的命令
			command[ 1] = 0x34;
			command[ 2] = 0x30;
			command[ 3] = 0x31;
			command[ 4] = 0x30;                                                                                         // 子命令
			command[ 5] = 0x30;
			command[ 6] = 0x30;
			command[ 7] = 0x30;
			command[ 8] = Encoding.ASCII.GetBytes( addressData.McDataType.AsciiCode )[0];                               // 软元件类型
			command[ 9] = Encoding.ASCII.GetBytes( addressData.McDataType.AsciiCode )[1];
			command[10] = MelsecHelper.BuildBytesFromAddress( addressData.AddressStart, addressData.McDataType )[0];    // 起始地址的地位
			command[11] = MelsecHelper.BuildBytesFromAddress( addressData.AddressStart, addressData.McDataType )[1];
			command[12] = MelsecHelper.BuildBytesFromAddress( addressData.AddressStart, addressData.McDataType )[2];
			command[13] = MelsecHelper.BuildBytesFromAddress( addressData.AddressStart, addressData.McDataType )[3];
			command[14] = MelsecHelper.BuildBytesFromAddress( addressData.AddressStart, addressData.McDataType )[4];
			command[15] = MelsecHelper.BuildBytesFromAddress( addressData.AddressStart, addressData.McDataType )[5];
			command[16] = SoftBasic.BuildAsciiBytesFrom( (ushort)(value.Length / 4) )[0];                               // 软元件点数
			command[17] = SoftBasic.BuildAsciiBytesFrom( (ushort)(value.Length / 4) )[1];
			command[18] = SoftBasic.BuildAsciiBytesFrom( (ushort)(value.Length / 4) )[2];
			command[19] = SoftBasic.BuildAsciiBytesFrom( (ushort)(value.Length / 4) )[3];
			value.CopyTo( command, 20 );

			return command;
		}

		/// <summary>
		/// 以位为单位，创建ASCII数据写入的核心报文
		/// </summary>
		/// <param name="addressData">三菱Mc协议的数据地址</param>
		/// <param name="value">原始的bool数组数据</param>
		/// <returns>带有成功标识的报文对象</returns>
		public static byte[] BuildAsciiWriteBitCoreCommand( McAddressData addressData, bool[] value )
		{
			if (value == null) value = new bool[0];
			byte[] buffer = value.Select( m => m ? (byte)0x31 : (byte)0x30 ).ToArray( );

			byte[] command = new byte[20 + buffer.Length];
			command[ 0] = 0x31;                                                                              // 批量写入的命令
			command[ 1] = 0x34;
			command[ 2] = 0x30;
			command[ 3] = 0x31;
			command[ 4] = 0x30;                                                                              // 子命令
			command[ 5] = 0x30;
			command[ 6] = 0x30;
			command[ 7] = 0x31;
			command[ 8] = Encoding.ASCII.GetBytes( addressData.McDataType.AsciiCode )[0];                         // 软元件类型
			command[ 9] = Encoding.ASCII.GetBytes( addressData.McDataType.AsciiCode )[1];
			command[10] = MelsecHelper.BuildBytesFromAddress( addressData.AddressStart, addressData.McDataType )[0];     // 起始地址的地位
			command[11] = MelsecHelper.BuildBytesFromAddress( addressData.AddressStart, addressData.McDataType )[1];
			command[12] = MelsecHelper.BuildBytesFromAddress( addressData.AddressStart, addressData.McDataType )[2];
			command[13] = MelsecHelper.BuildBytesFromAddress( addressData.AddressStart, addressData.McDataType )[3];
			command[14] = MelsecHelper.BuildBytesFromAddress( addressData.AddressStart, addressData.McDataType )[4];
			command[15] = MelsecHelper.BuildBytesFromAddress( addressData.AddressStart, addressData.McDataType )[5];
			command[16] = SoftBasic.BuildAsciiBytesFrom( (ushort)(value.Length) )[0];              // 软元件点数
			command[17] = SoftBasic.BuildAsciiBytesFrom( (ushort)(value.Length) )[1];
			command[18] = SoftBasic.BuildAsciiBytesFrom( (ushort)(value.Length) )[2];
			command[19] = SoftBasic.BuildAsciiBytesFrom( (ushort)(value.Length) )[3];
			buffer.CopyTo( command, 20 );

			return command;
		}

		/// <summary>
		/// 按字为单位随机读取的指令创建
		/// </summary>
		/// <param name="address">地址数组</param>
		/// <returns>指令</returns>
		public static byte[] BuildAsciiReadRandomWordCommand( McAddressData[] address )
		{
			byte[] command = new byte[12 + address.Length * 8];
			command[ 0] = 0x30;                                                               // 批量读取数据命令
			command[ 1] = 0x34;
			command[ 2] = 0x30;
			command[ 3] = 0x33;
			command[ 4] = 0x30;                                                               // 以点为单位还是字为单位成批读取
			command[ 5] = 0x30;
			command[ 6] = 0x30;
			command[ 7] = 0x30;
			command[ 8] = SoftBasic.BuildAsciiBytesFrom( (byte)address.Length )[0];
			command[ 9] = SoftBasic.BuildAsciiBytesFrom( (byte)address.Length )[1];
			command[10] = 0x30;
			command[11] = 0x30;
			for (int i = 0; i < address.Length; i++)
			{
				command[i * 8 + 12] = Encoding.ASCII.GetBytes( address[i].McDataType.AsciiCode )[0];          // 软元件类型
				command[i * 8 + 13] = Encoding.ASCII.GetBytes( address[i].McDataType.AsciiCode )[1];
				command[i * 8 + 14] = MelsecHelper.BuildBytesFromAddress( address[i].AddressStart, address[i].McDataType )[0];            // 起始地址的地位
				command[i * 8 + 15] = MelsecHelper.BuildBytesFromAddress( address[i].AddressStart, address[i].McDataType )[1];
				command[i * 8 + 16] = MelsecHelper.BuildBytesFromAddress( address[i].AddressStart, address[i].McDataType )[2];
				command[i * 8 + 17] = MelsecHelper.BuildBytesFromAddress( address[i].AddressStart, address[i].McDataType )[3];
				command[i * 8 + 18] = MelsecHelper.BuildBytesFromAddress( address[i].AddressStart, address[i].McDataType )[4];
				command[i * 8 + 19] = MelsecHelper.BuildBytesFromAddress( address[i].AddressStart, address[i].McDataType )[5];
			}
			return command;
		}

		/// <summary>
		/// 随机读取的指令创建
		/// </summary>
		/// <param name="address">地址数组</param>
		/// <returns>指令</returns>
		public static byte[] BuildAsciiReadRandomCommand( McAddressData[] address )
		{
			byte[] command = new byte[12 + address.Length * 12];
			command[ 0] = 0x30;                                                               // 批量读取数据命令
			command[ 1] = 0x34;
			command[ 2] = 0x30;
			command[ 3] = 0x36;
			command[ 4] = 0x30;                                                               // 以点为单位还是字为单位成批读取
			command[ 5] = 0x30;
			command[ 6] = 0x30;
			command[ 7] = 0x30;
			command[ 8] = SoftBasic.BuildAsciiBytesFrom( (byte)address.Length )[0];
			command[ 9] = SoftBasic.BuildAsciiBytesFrom( (byte)address.Length )[1];
			command[10] = 0x30;
			command[11] = 0x30;
			for (int i = 0; i < address.Length; i++)
			{
				command[i * 12 + 12] = Encoding.ASCII.GetBytes( address[i].McDataType.AsciiCode )[0];          // 软元件类型
				command[i * 12 + 13] = Encoding.ASCII.GetBytes( address[i].McDataType.AsciiCode )[1];
				command[i * 12 + 14] = MelsecHelper.BuildBytesFromAddress( address[i].AddressStart, address[i].McDataType )[0];            // 起始地址的地位
				command[i * 12 + 15] = MelsecHelper.BuildBytesFromAddress( address[i].AddressStart, address[i].McDataType )[1];
				command[i * 12 + 16] = MelsecHelper.BuildBytesFromAddress( address[i].AddressStart, address[i].McDataType )[2];
				command[i * 12 + 17] = MelsecHelper.BuildBytesFromAddress( address[i].AddressStart, address[i].McDataType )[3];
				command[i * 12 + 18] = MelsecHelper.BuildBytesFromAddress( address[i].AddressStart, address[i].McDataType )[4];
				command[i * 12 + 19] = MelsecHelper.BuildBytesFromAddress( address[i].AddressStart, address[i].McDataType )[5];
				command[i * 12 + 20] = SoftBasic.BuildAsciiBytesFrom( address[i].Length )[0];
				command[i * 12 + 21] = SoftBasic.BuildAsciiBytesFrom( address[i].Length )[1];
				command[i * 12 + 22] = SoftBasic.BuildAsciiBytesFrom( address[i].Length )[2];
				command[i * 12 + 23] = SoftBasic.BuildAsciiBytesFrom( address[i].Length )[3];
			}
			return command;
		}

		/// <inheritdoc  cref="McBinaryHelper.BuildReadMemoryCommand(string, ushort)"/>
		public static OperateResult<byte[]> BuildAsciiReadMemoryCommand( string address, ushort length )
		{
			try
			{
				uint add = uint.Parse( address );
				byte[] command = new byte[20];
				command[0] = 0x30;                                                      // 读取缓冲数据命令
				command[1] = 0x36;
				command[2] = 0x31;
				command[3] = 0x33;
				command[4] = 0x30;
				command[5] = 0x30;
				command[6] = 0x30;
				command[7] = 0x30;
				SoftBasic.BuildAsciiBytesFrom( add ).CopyTo( command, 8 );              // 起始地址信息
				SoftBasic.BuildAsciiBytesFrom( length ).CopyTo( command, 16 );          // 软元件的长度

				return OperateResult.CreateSuccessResult( command );
			}
			catch (Exception ex)
			{
				return new OperateResult<byte[]>( ex.Message );
			}
		}

		/// <inheritdoc cref="McBinaryHelper.BuildReadSmartModule(ushort, string, ushort)"/>
		public static OperateResult<byte[]> BuildAsciiReadSmartModule( ushort module, string address, ushort length )
		{
			try
			{
				uint add = uint.Parse( address );
				byte[] command = new byte[24];
				command[ 0] = 0x30;                                                          // 读取智能缓冲数据命令
				command[ 1] = 0x36;
				command[ 2] = 0x30;
				command[ 3] = 0x31;
				command[ 4] = 0x30;
				command[ 5] = 0x30;
				command[ 6] = 0x30;
				command[ 7] = 0x30;
				SoftBasic.BuildAsciiBytesFrom( add ).CopyTo(command, 8);                     // 起始地址的地位
				SoftBasic.BuildAsciiBytesFrom( length ).CopyTo( command, 16 ); // 地址的长度
				SoftBasic.BuildAsciiBytesFrom( module ).CopyTo( command, 20);                // 模块号
				return OperateResult.CreateSuccessResult( command );
			}
			catch (Exception ex)
			{
				return new OperateResult<byte[]>( ex.Message );
			}
		}

	}
}
