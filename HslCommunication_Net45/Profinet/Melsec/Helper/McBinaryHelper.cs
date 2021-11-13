using HslCommunication.BasicFramework;
using HslCommunication.Core;
using HslCommunication.Core.Address;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
#if !NET20 && !NET35
using System.Threading.Tasks;
#endif

namespace HslCommunication.Profinet.Melsec.Helper
{
	/// <summary>
	/// 三菱PLC，二进制的辅助类对象
	/// </summary>
	public class McBinaryHelper
	{
		#region Static Method Helper

		/// <summary>
		/// 将MC协议的核心报文打包成一个可以直接对PLC进行发送的原始报文
		/// </summary>
		/// <param name="mcCore">MC协议的核心报文</param>
		/// <param name="networkNumber">网络号</param>
		/// <param name="networkStationNumber">网络站号</param>
		/// <returns>原始报文信息</returns>
		public static byte[] PackMcCommand( byte[] mcCore, byte networkNumber = 0, byte networkStationNumber = 0 )
		{
			byte[] _PLCCommand = new byte[11 + mcCore.Length];
			_PLCCommand[0] = 0x50;                                               // 副标题
			_PLCCommand[1] = 0x00;
			_PLCCommand[2] = networkNumber;                                      // 网络号
			_PLCCommand[3] = 0xFF;                                               // PLC编号
			_PLCCommand[4] = 0xFF;                                               // 目标模块IO编号
			_PLCCommand[5] = 0x03;
			_PLCCommand[6] = networkStationNumber;                               // 目标模块站号
			_PLCCommand[7] = (byte)((_PLCCommand.Length - 9) % 256);             // 请求数据长度
			_PLCCommand[8] = (byte)((_PLCCommand.Length - 9) / 256);
			_PLCCommand[9] = 0x0A;                                               // CPU监视定时器
			_PLCCommand[10] = 0x00;
			mcCore.CopyTo( _PLCCommand, 11 );

			return _PLCCommand;
		}

		/// <summary>
		/// 检查从MC返回的数据是否是合法的。
		/// </summary>
		/// <param name="content">数据内容</param>
		/// <returns>是否合法</returns>
		public static OperateResult CheckResponseContentHelper( byte[] content )
		{
			ushort errorCode = BitConverter.ToUInt16( content, 9 );
			if (errorCode != 0) return new OperateResult<byte[]>( errorCode, MelsecHelper.GetErrorDescription( errorCode ) );

			return OperateResult.CreateSuccessResult( );
		}

		#endregion

		/// <summary>
		/// 从三菱地址，是否位读取进行创建读取的MC的核心报文<br />
		/// From the Mitsubishi address, whether to read the core message of the MC for creating and reading
		/// </summary>
		/// <param name="isBit">是否进行了位读取操作</param>
		/// <param name="addressData">三菱Mc协议的数据地址</param>
		/// <returns>带有成功标识的报文对象</returns>
		public static byte[] BuildReadMcCoreCommand( McAddressData addressData, bool isBit )
		{
			byte[] command = new byte[10];
			command[0] = 0x01;                                                      // 批量读取数据命令
			command[1] = 0x04;
			command[2] = isBit ? (byte)0x01 : (byte)0x00;                           // 以点为单位还是字为单位成批读取
			command[3] = 0x00;
			command[4] = BitConverter.GetBytes( addressData.AddressStart )[0];      // 起始地址的地位
			command[5] = BitConverter.GetBytes( addressData.AddressStart )[1];
			command[6] = BitConverter.GetBytes( addressData.AddressStart )[2];
			command[7] = (byte)addressData.McDataType.DataCode;                     // 指明读取的数据
			command[8] = (byte)(addressData.Length % 256);                          // 软元件的长度
			command[9] = (byte)(addressData.Length / 256);

			return command;
		}

		/// <summary>
		/// 以字为单位，创建数据写入的核心报文
		/// </summary>
		/// <param name="addressData">三菱Mc协议的数据地址</param>
		/// <param name="value">实际的原始数据信息</param>
		/// <returns>带有成功标识的报文对象</returns>
		public static byte[] BuildWriteWordCoreCommand( McAddressData addressData, byte[] value )
		{
			if (value == null) value = new byte[0];
			byte[] command = new byte[10 + value.Length];
			command[0] = 0x01;                                                        // 批量写入数据命令
			command[1] = 0x14;
			command[2] = 0x00;                                                        // 以字为单位成批读取
			command[3] = 0x00;
			command[4] = BitConverter.GetBytes( addressData.AddressStart )[0];        // 起始地址的地位
			command[5] = BitConverter.GetBytes( addressData.AddressStart )[1];
			command[6] = BitConverter.GetBytes( addressData.AddressStart )[2];
			command[7] = (byte)addressData.McDataType.DataCode;                       // 指明写入的数据
			command[8] = (byte)(value.Length / 2 % 256);                              // 软元件长度的地位
			command[9] = (byte)(value.Length / 2 / 256);
			value.CopyTo( command, 10 );

			return command;
		}

		/// <summary>
		/// 以位为单位，创建数据写入的核心报文
		/// </summary>
		/// <param name="addressData">三菱Mc协议的数据地址</param>
		/// <param name="value">原始的bool数组数据</param>
		/// <returns>带有成功标识的报文对象</returns>
		public static byte[] BuildWriteBitCoreCommand( McAddressData addressData, bool[] value )
		{
			if (value == null) value = new bool[0];
			byte[] buffer = MelsecHelper.TransBoolArrayToByteData( value );
			byte[] command = new byte[10 + buffer.Length];
			command[0] = 0x01;                                                        // 批量写入数据命令
			command[1] = 0x14;
			command[2] = 0x01;                                                        // 以位为单位成批写入
			command[3] = 0x00;
			command[4] = BitConverter.GetBytes( addressData.AddressStart )[0];        // 起始地址的地位
			command[5] = BitConverter.GetBytes( addressData.AddressStart )[1];
			command[6] = BitConverter.GetBytes( addressData.AddressStart )[2];
			command[7] = (byte)addressData.McDataType.DataCode;                       // 指明写入的数据
			command[8] = (byte)(value.Length % 256);                                  // 软元件长度的地位
			command[9] = (byte)(value.Length / 256);
			buffer.CopyTo( command, 10 );

			return command;
		}

		/// <summary>
		/// 从三菱扩展地址，是否位读取进行创建读取的MC的核心报文
		/// </summary>
		/// <param name="isBit">是否进行了位读取操作</param>
		/// <param name="extend">扩展指定</param>
		/// <param name="addressData">三菱Mc协议的数据地址</param>
		/// <returns>带有成功标识的报文对象</returns>
		public static byte[] BuildReadMcCoreExtendCommand( McAddressData addressData, ushort extend, bool isBit )
		{
			byte[] command = new byte[17];
			command[ 0] = 0x01;                                                      // 批量读取数据命令
			command[ 1] = 0x04;
			command[ 2] = isBit ? (byte)0x81 : (byte)0x80;                           // 以点为单位还是字为单位成批读取
			command[ 3] = 0x00;
			command[ 4] = 0x00;
			command[ 5] = 0x00;
			command[ 6] = BitConverter.GetBytes( addressData.AddressStart )[0];      // 起始地址的地位
			command[ 7] = BitConverter.GetBytes( addressData.AddressStart )[1];
			command[ 8] = BitConverter.GetBytes( addressData.AddressStart )[2];
			command[ 9] = (byte)addressData.McDataType.DataCode;                     // 指明读取的数据
			command[10] = 0x00;
			command[11] = 0x00;
			command[12] = BitConverter.GetBytes( extend )[0];
			command[13] = BitConverter.GetBytes( extend )[1];
			command[14] = 0xF9;
			command[15] = (byte)(addressData.Length % 256);                          // 软元件的长度
			command[16] = (byte)(addressData.Length / 256);

			return command;
		}

		/// <summary>
		/// 按字为单位随机读取的指令创建
		/// </summary>
		/// <param name="address">地址数组</param>
		/// <returns>指令</returns>
		public static byte[] BuildReadRandomWordCommand( McAddressData[] address )
		{
			byte[] command = new byte[6 + address.Length * 4];
			command[0] = 0x03;                                                                  // 批量读取数据命令
			command[1] = 0x04;
			command[2] = 0x00;
			command[3] = 0x00;
			command[4] = (byte)address.Length;                                                  // 访问的字点数
			command[5] = 0x00;                                                                  // 双字访问点数
			for (int i = 0; i < address.Length; i++)
			{
				command[i * 4 + 6] = BitConverter.GetBytes( address[i].AddressStart )[0];       // 软元件起始地址
				command[i * 4 + 7] = BitConverter.GetBytes( address[i].AddressStart )[1];
				command[i * 4 + 8] = BitConverter.GetBytes( address[i].AddressStart )[2];
				command[i * 4 + 9] = (byte)address[i].McDataType.DataCode;                      // 软元件代号
			}
			return command;
		}

		/// <summary>
		/// 随机读取的指令创建
		/// </summary>
		/// <param name="address">地址数组</param>
		/// <returns>指令</returns>
		public static byte[] BuildReadRandomCommand( McAddressData[] address )
		{
			byte[] command = new byte[6 + address.Length * 6];
			command[0] = 0x06;                                                                  // 批量读取数据命令
			command[1] = 0x04;
			command[2] = 0x00;                                                                  // 子命令
			command[3] = 0x00;
			command[4] = (byte)address.Length;                                                  // 字软元件的块数
			command[5] = 0x00;                                                                  // 位软元件的块数
			for (int i = 0; i < address.Length; i++)
			{
				command[i * 6 + 6] = BitConverter.GetBytes( address[i].AddressStart )[0];      // 字软元件的编号
				command[i * 6 + 7] = BitConverter.GetBytes( address[i].AddressStart )[1];
				command[i * 6 + 8] = BitConverter.GetBytes( address[i].AddressStart )[2];
				command[i * 6 + 9] = (byte)address[i].McDataType.DataCode;                      // 字软元件的代码
				command[i * 6 + 10] = (byte)(address[i].Length % 256);                          // 软元件的长度
				command[i * 6 + 11] = (byte)(address[i].Length / 256);
			}
			return command;
		}

		/// <summary>
		/// 创建批量读取标签的报文数据信息
		/// </summary>
		/// <param name="tags">标签名</param>
		/// <param name="lengths">长度信息</param>
		/// <returns>报文名称</returns>
		public static byte[] BuildReadTag( string[] tags, ushort[] lengths )
		{
			if (tags.Length != lengths.Length) throw new Exception( StringResources.Language.TwoParametersLengthIsNotSame );

			MemoryStream command = new MemoryStream( );
			command.WriteByte( 0x1A );                                                          // 批量读取标签的指令
			command.WriteByte( 0x04 );
			command.WriteByte( 0x00 );                                                          // 子命令
			command.WriteByte( 0x00 );
			command.WriteByte( BitConverter.GetBytes( tags.Length )[0] );                       // 排列点数
			command.WriteByte( BitConverter.GetBytes( tags.Length )[1] );
			command.WriteByte( 0x00 );                                                          // 省略指定
			command.WriteByte( 0x00 );
			for (int i = 0; i < tags.Length; i++)
			{
				byte[] tagBuffer = Encoding.Unicode.GetBytes( tags[i] );
				command.WriteByte( BitConverter.GetBytes( tagBuffer.Length / 2 )[0] );          // 标签长度
				command.WriteByte( BitConverter.GetBytes( tagBuffer.Length / 2 )[1] );
				command.Write( tagBuffer, 0, tagBuffer.Length );                                // 标签名称
				command.WriteByte( 0x01 );                                                      // 单位指定
				command.WriteByte( 0x00 );                                                      // 固定值
				command.WriteByte( BitConverter.GetBytes( lengths[i] * 2 )[0] );                // 排列数据长
				command.WriteByte( BitConverter.GetBytes( lengths[i] * 2 )[1] );
			}
			byte[] buffer = command.ToArray( );
			command.Dispose( );
			return buffer;
		}

		/// <summary>
		/// 读取本站缓冲寄存器的数据信息，需要指定寄存器的地址，和读取的长度
		/// </summary>
		/// <param name="address">寄存器的地址</param>
		/// <param name="length">数据长度</param>
		/// <returns>结果内容</returns>
		public static OperateResult<byte[]> BuildReadMemoryCommand( string address, ushort length )
		{
			try
			{
				uint add = uint.Parse( address );
				byte[] command = new byte[10];
				command[0] = 0x13;                                                      // 读取缓冲数据命令
				command[1] = 0x06;
				command[2] = 0x00;
				command[3] = 0x00;
				command[4] = BitConverter.GetBytes( add )[0];                           // 起始地址的地位
				command[5] = BitConverter.GetBytes( add )[1];
				command[6] = BitConverter.GetBytes( add )[2];
				command[7] = BitConverter.GetBytes( add )[3];
				command[8] = (byte)(length % 256);                                      // 软元件的长度
				command[9] = (byte)(length / 256);

				return OperateResult.CreateSuccessResult( command );
			}
			catch (Exception ex)
			{
				return new OperateResult<byte[]>( ex.Message );
			}
		}
		/// <summary>
		/// 构建读取智能模块的命令，需要指定模块编号，起始地址，读取的长度，注意，该长度以字节为单位。
		/// </summary>
		/// <param name="module">模块编号</param>
		/// <param name="address">智能模块的起始地址</param>
		/// <param name="length">读取的字长度</param>
		/// <returns>报文的结果内容</returns>
		public static OperateResult<byte[]> BuildReadSmartModule( ushort module, string address, ushort length )
		{
			try
			{
				uint add = uint.Parse( address );
				byte[] command = new byte[12];
				command[0] = 0x01;                                                      // 读取智能缓冲数据命令
				command[1] = 0x06;
				command[2] = 0x00;
				command[3] = 0x00;
				command[4] = BitConverter.GetBytes( add )[0];                           // 起始地址的地位
				command[5] = BitConverter.GetBytes( add )[1];
				command[6] = BitConverter.GetBytes( add )[2];
				command[7] = BitConverter.GetBytes( add )[3];
				command[8] = (byte)(length % 256);                                      // 地址的长度
				command[9] = (byte)(length / 256);
				command[10] = BitConverter.GetBytes( module )[0];                        // 模块号
				command[11] = BitConverter.GetBytes( module )[1];
				return OperateResult.CreateSuccessResult( command );
			}
			catch (Exception ex)
			{
				return new OperateResult<byte[]>( ex.Message );
			}
		}

		/// <summary>
		/// 解析出标签读取的数据内容
		/// </summary>
		/// <param name="content">返回的数据信息</param>
		/// <returns>解析结果</returns>
		public static OperateResult<byte[]> ExtraTagData( byte[] content )
		{
			try
			{
				int count = BitConverter.ToUInt16( content, 0 );
				int index = 2;
				List<byte> array = new List<byte>( 20 );
				for (int i = 0; i < count; i++)
				{
					int length = BitConverter.ToUInt16( content, index + 2 );
					array.AddRange( SoftBasic.ArraySelectMiddle( content, index + 4, length ) );
					index += 4 + length;
				}
				return OperateResult.CreateSuccessResult( array.ToArray( ) );
			}
			catch (Exception ex)
			{
				return new OperateResult<byte[]>( ex.Message + " Source:" + SoftBasic.ByteToHexString( content, ' ' ) );
			}
		}

		/// <inheritdoc cref="IReadWriteMc.ExtractActualData(byte[], bool)"/>
		public static byte[] ExtractActualDataHelper( byte[] response, bool isBit )
		{
			if (isBit)
			{
				// 位读取
				byte[] Content = new byte[response.Length * 2];
				for (int i = 0; i < response.Length; i++)
				{
					if ((response[i] & 0x10) == 0x10)
					{
						Content[i * 2 + 0] = 0x01;
					}

					if ((response[i] & 0x01) == 0x01)
					{
						Content[i * 2 + 1] = 0x01;
					}
				}

				return Content;
			}
			else
			{
				// 字读取
				return response;
			}
		}

		#region Advanced Function

		/// <summary>
		/// <b>[商业授权]</b> 读取PLC的标签信息，需要传入标签的名称，读取的字长度，标签举例：A; label[1]; bbb[10,10,10]<br />
		/// <b>[Authorization]</b> To read the label information of the PLC, you need to pass in the name of the label, 
		/// the length of the word read, and an example of the label: A; label [1]; bbb [10,10,10]
		/// </summary>
		/// <param name="mc">MC协议通信对象</param>
		/// <param name="tags">标签名</param>
		/// <param name="length">读取长度</param>
		/// <returns>是否成功</returns>
		/// <remarks>
		///  不可以访问局部标签。<br />
		///  不可以访问通过GX Works2设置的全局标签。<br />
		///  为了访问全局标签，需要通过GX Works3的全局标签设置编辑器将“来自于外部设备的访问”的设置项目置为有效。(默认为无效。)<br />
		///  以ASCII代码进行数据通信时，由于需要从UTF-16将标签名转换为ASCII代码，因此报文容量将增加
		/// </remarks>
		public static OperateResult<byte[]> ReadTags( IReadWriteMc mc, string[] tags, ushort[] length )
		{
			if (!Authorization.asdniasnfaksndiqwhawfskhfaiw( )) return new OperateResult<byte[]>( StringResources.Language.InsufficientPrivileges );
			byte[] coreResult = BuildReadTag( tags, length );
			// 核心交互
			var read = mc.ReadFromCoreServer( coreResult );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( read );

			return ExtraTagData( mc.ExtractActualData( read.Content, false ) );
		}
#if !NET35 && !NET20
		/// <inheritdoc cref="ReadTags(IReadWriteMc, string[], ushort[])"/>
		public static async Task<OperateResult<byte[]>> ReadTagsAsync( IReadWriteMc mc, string[] tags, ushort[] length )
		{
			if (!Authorization.asdniasnfaksndiqwhawfskhfaiw( )) return new OperateResult<byte[]>( StringResources.Language.InsufficientPrivileges );

			byte[] coreResult = BuildReadTag( tags, length );

			// 核心交互
			var read = await mc.ReadFromCoreServerAsync( coreResult );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( read );

			return ExtraTagData( mc.ExtractActualData( read.Content, false ) );
		}
#endif
		#endregion

	}
}
