using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using HslCommunication.BasicFramework;
using HslCommunication.Core;
using HslCommunication.Core.Address;
using HslCommunication.Serial;

namespace HslCommunication.ModBus
{

	/// <summary>
	/// Modbus协议相关的一些信息，包括功能码定义，报文的生成的定义等等信息<br />
	/// Some information related to Modbus protocol, including function code definition, definition of message generation, etc.
	/// </summary>
	public class ModbusInfo
	{
		#region Function Declaration
		
		/// <summary>
		/// 读取线圈
		/// </summary>
		public const byte ReadCoil = 0x01;

		/// <summary>
		/// 读取离散量
		/// </summary>
		public const byte ReadDiscrete = 0x02;

		/// <summary>
		/// 读取寄存器
		/// </summary>
		public const byte ReadRegister = 0x03;

		/// <summary>
		/// 读取输入寄存器
		/// </summary>
		public const byte ReadInputRegister = 0x04;

		/// <summary>
		/// 写单个线圈
		/// </summary>
		public const byte WriteOneCoil = 0x05;

		/// <summary>
		/// 写单个寄存器
		/// </summary>
		public const byte WriteOneRegister = 0x06;

		/// <summary>
		/// 写多个线圈
		/// </summary>
		public const byte WriteCoil = 0x0F;

		/// <summary>
		/// 写多个寄存器
		/// </summary>
		public const byte WriteRegister = 0x10;

		/// <summary>
		/// 使用掩码的方式写入寄存器
		/// </summary>
		public const byte WriteMaskRegister = 0x16;

		#endregion

		#region ErrCode Declaration

		/// <summary>
		/// 不支持该功能码
		/// </summary>
		public const byte FunctionCodeNotSupport = 0x01;
		/// <summary>
		/// 该地址越界
		/// </summary>
		public const byte FunctionCodeOverBound = 0x02;
		/// <summary>
		/// 读取长度超过最大值
		/// </summary>
		public const byte FunctionCodeQuantityOver = 0x03;
		/// <summary>
		/// 读写异常
		/// </summary>
		public const byte FunctionCodeReadWriteException = 0x04;

		#endregion
		
		#region Static Helper Method

		private static void CheckModbusAddressStart(ModbusAddress mAddress, bool isStartWithZero )
		{
			if (!isStartWithZero)
			{
				if (mAddress.Address < 1) throw new Exception( StringResources.Language.ModbusAddressMustMoreThanOne );
				mAddress.Address = (ushort)(mAddress.Address - 1);
			}
		}

		/// <summary>
		/// 构建Modbus读取数据的核心报文，需要指定地址，长度，站号，是否起始地址0，默认的功能码应该根据bool或是字来区分<br />
		/// To construct the core message of Modbus reading data, you need to specify the address, length, station number, 
		/// whether the starting address is 0, and the default function code should be distinguished according to bool or word
		/// </summary>
		/// <param name="address">Modbus的富文本地址</param>
		/// <param name="length">读取的数据长度</param>
		/// <param name="station">默认的站号信息</param>
		/// <param name="isStartWithZero">起始地址是否从0开始</param>
		/// <param name="defaultFunction">默认的功能码</param>
		/// <returns>包含最终命令的结果对象</returns>
		public static OperateResult<byte[][]> BuildReadModbusCommand( string address, ushort length, byte station, bool isStartWithZero, byte defaultFunction )
		{
			try
			{
				ModbusAddress mAddress = new ModbusAddress( address, station, defaultFunction );
				CheckModbusAddressStart( mAddress, isStartWithZero );

				return BuildReadModbusCommand( mAddress, length );
			}
			catch(Exception ex)
			{
				return new OperateResult<byte[][]>( ex.Message );
			}
		}

		/// <summary>
		/// 构建Modbus读取数据的核心报文，需要指定地址，长度，站号，是否起始地址0，默认的功能码应该根据bool或是字来区分<br />
		/// To construct the core message of Modbus reading data, you need to specify the address, length, station number, 
		/// whether the starting address is 0, and the default function code should be distinguished according to bool or word
		/// </summary>
		/// <param name="mAddress">Modbus的富文本地址</param>
		/// <param name="length">读取的数据长度</param>
		/// <returns>包含最终命令的结果对象</returns>
		public static OperateResult<byte[][]> BuildReadModbusCommand( ModbusAddress mAddress, ushort length )
		{
			List<byte[]> commands = new List<byte[]>( );
			if (mAddress.Function == ReadCoil || 
				mAddress.Function == ReadDiscrete || 
				mAddress.Function == ReadRegister || 
				mAddress.Function == ReadInputRegister ||
				Authorization.asdniasnfaksndiqwhawfskhfaiw( ))
			{
				// 支持自动切割读取，字读取 120 个字，位读取 2000 个位
				OperateResult<int[], int[]> bytes = HslHelper.SplitReadLength( mAddress.Address, length,
					(mAddress.Function == ReadCoil || mAddress.Function == ReadDiscrete ) ? (ushort)2000 : (ushort)120);
				for (int i = 0; i < bytes.Content1.Length; i++)
				{
					byte[] buffer = new byte[6];
					buffer[0] = (byte)mAddress.Station;
					buffer[1] = (byte)mAddress.Function;
					buffer[2] = BitConverter.GetBytes( bytes.Content1[i] )[1];
					buffer[3] = BitConverter.GetBytes( bytes.Content1[i] )[0];
					buffer[4] = BitConverter.GetBytes( bytes.Content2[i] )[1];
					buffer[5] = BitConverter.GetBytes( bytes.Content2[i] )[0];
					commands.Add( buffer );
				}
			}
			else
			{
				byte[] buffer = new byte[6];
				buffer[0] = (byte)mAddress.Station;
				buffer[1] = (byte)mAddress.Function;
				buffer[2] = BitConverter.GetBytes( mAddress.Address )[1];
				buffer[3] = BitConverter.GetBytes( mAddress.Address )[0];
				buffer[4] = BitConverter.GetBytes( length )[1];
				buffer[5] = BitConverter.GetBytes( length )[0];
				commands.Add( buffer );
			}
			return OperateResult.CreateSuccessResult( commands.ToArray( ) );
		}

		/// <summary>
		/// 构建Modbus写入bool数据的核心报文，需要指定地址，长度，站号，是否起始地址0，默认的功能码<br />
		/// To construct the core message that Modbus writes to bool data, you need to specify the address, length,
		/// station number, whether the starting address is 0, and the default function code
		/// </summary>
		/// <param name="address">Modbus的富文本地址</param>
		/// <param name="values">bool数组的信息</param>
		/// <param name="station">默认的站号信息</param>
		/// <param name="isStartWithZero">起始地址是否从0开始</param>
		/// <param name="defaultFunction">默认的功能码</param>
		/// <returns>包含最终命令的结果对象</returns>
		public static OperateResult<byte[]> BuildWriteBoolModbusCommand( string address, bool[] values, byte station, bool isStartWithZero, byte defaultFunction )
		{
			try
			{
				ModbusAddress mAddress = new ModbusAddress( address, station, defaultFunction );
				CheckModbusAddressStart( mAddress, isStartWithZero );

				return BuildWriteBoolModbusCommand( mAddress, values );
			}
			catch (Exception ex)
			{
				return new OperateResult<byte[]>( ex.Message );
			}
		}


		/// <summary>
		/// 构建Modbus写入bool数据的核心报文，需要指定地址，长度，站号，是否起始地址0，默认的功能码<br />
		/// To construct the core message that Modbus writes to bool data, you need to specify the address, length, station number, whether the starting address is 0, and the default function code
		/// </summary>
		/// <param name="address">Modbus的富文本地址</param>
		/// <param name="value">bool的信息</param>
		/// <param name="station">默认的站号信息</param>
		/// <param name="isStartWithZero">起始地址是否从0开始</param>
		/// <param name="defaultFunction">默认的功能码</param>
		/// <returns>包含最终命令的结果对象</returns>
		public static OperateResult<byte[]> BuildWriteBoolModbusCommand( string address, bool value, byte station, bool isStartWithZero, byte defaultFunction )
		{
			try
			{
				if (address.IndexOf( '.' ) <= 0)
				{
					ModbusAddress mAddress = new ModbusAddress( address, station, defaultFunction );
					CheckModbusAddressStart( mAddress, isStartWithZero );

					return BuildWriteBoolModbusCommand( mAddress, value );
				}
				else
				{
					// 当为掩码写入时，需要商业授权操作
					if (!Authorization.asdniasnfaksndiqwhawfskhfaiw( )) return new OperateResult<byte[]>( StringResources.Language.InsufficientPrivileges );

					int bitIndex = Convert.ToInt32( address.Substring( address.IndexOf( '.' ) + 1 ) );
					if (bitIndex < 0 || bitIndex > 15) return new OperateResult<byte[]>( StringResources.Language.ModbusBitIndexOverstep );

					int orMask = 1 << bitIndex;
					int andMask = ~orMask;
					if (!value) orMask = 0;

					return BuildWriteMaskModbusCommand( address.Substring( 0, address.IndexOf( '.' ) ), (ushort)andMask, (ushort)orMask, station, isStartWithZero, ModbusInfo.WriteMaskRegister );
				}
			}
			catch (Exception ex)
			{
				return new OperateResult<byte[]>( ex.Message );
			}
		}

		/// <summary>
		/// 构建Modbus写入bool数组的核心报文，需要指定地址，长度，站号，是否起始地址0，默认的功能码<br />
		/// To construct the core message that Modbus writes to the bool array, you need to specify the address, length, 
		/// station number, whether the starting address is 0, and the default function code
		/// </summary>
		/// <param name="mAddress">Modbus的富文本地址</param>
		/// <param name="values">bool数组的信息</param>
		/// <returns>包含最终命令的结果对象</returns>
		public static OperateResult<byte[]> BuildWriteBoolModbusCommand( ModbusAddress mAddress, bool[] values )
		{
			try
			{
				byte[] data = SoftBasic.BoolArrayToByte( values );
				byte[] content = new byte[7 + data.Length];
				content[0] = (byte)mAddress.Station;
				content[1] = (byte)mAddress.Function;
				content[2] = BitConverter.GetBytes( mAddress.Address )[1];
				content[3] = BitConverter.GetBytes( mAddress.Address )[0];
				content[4] = (byte)(values.Length / 256);
				content[5] = (byte)(values.Length % 256);
				content[6] = (byte)(data.Length);
				data.CopyTo( content, 7 );
				return OperateResult.CreateSuccessResult( content );
			}
			catch (Exception ex)
			{
				return new OperateResult<byte[]>( ex.Message );
			}
		}

		/// <summary>
		/// 构建Modbus写入bool数据的核心报文，需要指定地址，长度，站号，是否起始地址0，默认的功能码<br />
		/// To construct the core message that Modbus writes to bool data, you need to specify the address, length, station number, whether the starting address is 0, and the default function code
		/// </summary>
		/// <param name="mAddress">Modbus的富文本地址</param>
		/// <param name="value">bool数据的信息</param>
		/// <returns>包含最终命令的结果对象</returns>
		public static OperateResult<byte[]> BuildWriteBoolModbusCommand( ModbusAddress mAddress, bool value )
		{
			byte[] content = new byte[6];
			content[0] = (byte)mAddress.Station;
			content[1] = (byte)mAddress.Function;
			content[2] = BitConverter.GetBytes( mAddress.Address )[1];
			content[3] = BitConverter.GetBytes( mAddress.Address )[0];
			if (value)
			{
				content[4] = 0xFF;
				content[5] = 0x00;
			}
			else
			{
				content[4] = 0x00;
				content[5] = 0x00;
			}
			return OperateResult.CreateSuccessResult( content );
		}

		/// <summary>
		/// 构建Modbus写入字数据的核心报文，需要指定地址，长度，站号，是否起始地址0，默认的功能码<br />
		/// To construct the core message of Modbus writing word data, you need to specify the address, length, 
		/// station number, whether the starting address is 0, and the default function code
		/// </summary>
		/// <param name="address">Modbus的富文本地址</param>
		/// <param name="values">bool数组的信息</param>
		/// <param name="station">默认的站号信息</param>
		/// <param name="isStartWithZero">起始地址是否从0开始</param>
		/// <param name="defaultFunction">默认的功能码</param>
		/// <returns>包含最终命令的结果对象</returns>
		public static OperateResult<byte[]> BuildWriteWordModbusCommand( string address, byte[] values, byte station, bool isStartWithZero, byte defaultFunction )
		{
			try
			{
				ModbusAddress mAddress = new ModbusAddress( address, station, defaultFunction );
				if (mAddress.Function == ModbusInfo.ReadRegister) mAddress.Function = defaultFunction;
				CheckModbusAddressStart( mAddress, isStartWithZero );

				return BuildWriteWordModbusCommand( mAddress, values );
			}
			catch (Exception ex)
			{
				return new OperateResult<byte[]>( ex.Message );
			}
		}

		/// <summary>
		/// 构建Modbus写入字数据的核心报文，需要指定地址，长度，站号，是否起始地址0，默认的功能码<br />
		/// To construct the core message of Modbus writing word data, you need to specify the address, length, 
		/// station number, whether the starting address is 0, and the default function code
		/// </summary>
		/// <param name="address">Modbus的富文本地址</param>
		/// <param name="value">short数据信息</param>
		/// <param name="station">默认的站号信息</param>
		/// <param name="isStartWithZero">起始地址是否从0开始</param>
		/// <param name="defaultFunction">默认的功能码</param>
		/// <returns>包含最终命令的结果对象</returns>
		public static OperateResult<byte[]> BuildWriteWordModbusCommand( string address, short value, byte station, bool isStartWithZero, byte defaultFunction )
		{
			try
			{
				ModbusAddress mAddress = new ModbusAddress( address, station, defaultFunction );
				if (mAddress.Function == ModbusInfo.ReadRegister) mAddress.Function = defaultFunction;
				CheckModbusAddressStart( mAddress, isStartWithZero );

				return BuildWriteOneRegisterModbusCommand( mAddress, value );
			}
			catch (Exception ex)
			{
				return new OperateResult<byte[]>( ex.Message );
			}
		}

		/// <summary>
		/// 构建Modbus写入字数据的核心报文，需要指定地址，长度，站号，是否起始地址0，默认的功能码<br />
		/// To construct the core message of Modbus writing word data, you need to specify the address, length, 
		/// station number, whether the starting address is 0, and the default function code
		/// </summary>
		/// <param name="address">Modbus的富文本地址</param>
		/// <param name="value">bool数组的信息</param>
		/// <param name="station">默认的站号信息</param>
		/// <param name="isStartWithZero">起始地址是否从0开始</param>
		/// <param name="defaultFunction">默认的功能码</param>
		/// <returns>包含最终命令的结果对象</returns>
		public static OperateResult<byte[]> BuildWriteWordModbusCommand( string address, ushort value, byte station, bool isStartWithZero, byte defaultFunction )
		{
			try
			{
				ModbusAddress mAddress = new ModbusAddress( address, station, defaultFunction );
				if (mAddress.Function == ModbusInfo.ReadRegister) mAddress.Function = defaultFunction;
				CheckModbusAddressStart( mAddress, isStartWithZero );

				return BuildWriteOneRegisterModbusCommand( mAddress, value );
			}
			catch (Exception ex)
			{
				return new OperateResult<byte[]>( ex.Message );
			}
		}

		/// <summary>
		/// 构建Modbus写入掩码的核心报文，需要指定地址，长度，站号，是否起始地址0，默认的功能码<br />
		/// To construct the Modbus write mask core message, you need to specify the address, length, 
		/// station number, whether the starting address is 0, and the default function code
		/// </summary>
		/// <param name="address">Modbus的富文本地址</param>
		/// <param name="andMask">进行与操作的掩码信息</param>
		/// <param name="orMask">进行或操作的掩码信息</param>
		/// <param name="station">默认的站号信息</param>
		/// <param name="isStartWithZero">起始地址是否从0开始</param>
		/// <param name="defaultFunction">默认的功能码</param>
		/// <returns>包含最终命令的结果对象</returns>
		public static OperateResult<byte[]> BuildWriteMaskModbusCommand( string address, ushort andMask, ushort orMask, byte station, bool isStartWithZero, byte defaultFunction )
		{
			try
			{
				ModbusAddress mAddress = new ModbusAddress( address, station, defaultFunction );
				if (mAddress.Function == ModbusInfo.ReadRegister) mAddress.Function = defaultFunction;
				CheckModbusAddressStart( mAddress, isStartWithZero );

				return BuildWriteMaskModbusCommand( mAddress, andMask, orMask );
			}
			catch (Exception ex)
			{
				return new OperateResult<byte[]>( ex.Message );
			}
		}

		/// <summary>
		/// 构建Modbus写入字数据的核心报文，需要指定地址，长度，站号，是否起始地址0，默认的功能码<br />
		/// To construct the core message of Modbus writing word data, you need to specify the address, length, 
		/// station number, whether the starting address is 0, and the default function code
		/// </summary>
		/// <param name="mAddress">Modbus的富文本地址</param>
		/// <param name="values">bool数组的信息</param>
		/// <returns>包含最终命令的结果对象</returns>
		public static OperateResult<byte[]> BuildWriteWordModbusCommand( ModbusAddress mAddress, byte[] values )
		{
			byte[] content = new byte[7 + values.Length];
			content[0] = (byte)mAddress.Station;
			content[1] = (byte)mAddress.Function;
			content[2] = BitConverter.GetBytes( mAddress.Address )[1];
			content[3] = BitConverter.GetBytes( mAddress.Address )[0];
			content[4] = (byte)(values.Length / 2 / 256);
			content[5] = (byte)(values.Length / 2 % 256);
			content[6] = (byte)(values.Length);
			values.CopyTo( content, 7 );
			return OperateResult.CreateSuccessResult( content );
		}

		/// <summary>
		/// 构建Modbus写入掩码数据的核心报文，需要指定地址，长度，站号，是否起始地址0，默认的功能码<br />
		/// To construct the core message of Modbus writing mask data, you need to specify the address, length, 
		/// station number, whether the starting address is 0, and the default function code
		/// </summary>
		/// <param name="mAddress">Modbus的富文本地址</param>
		/// <param name="andMask">等待进行与操作的掩码</param>
		/// <param name="orMask">等待进行或操作的掩码</param>
		/// <returns>包含最终命令的结果对象</returns>
		public static OperateResult<byte[]> BuildWriteMaskModbusCommand( ModbusAddress mAddress, ushort andMask, ushort orMask )
		{
			byte[] content = new byte[8];
			content[0] = (byte)mAddress.Station;
			content[1] = (byte)mAddress.Function;
			content[2] = BitConverter.GetBytes( mAddress.Address )[1];
			content[3] = BitConverter.GetBytes( mAddress.Address )[0];
			content[4] = BitConverter.GetBytes( andMask )[1];
			content[5] = BitConverter.GetBytes( andMask )[0];
			content[6] = BitConverter.GetBytes( orMask )[1];
			content[7] = BitConverter.GetBytes( orMask )[0];
			return OperateResult.CreateSuccessResult( content );
		}

		/// <summary>
		/// 构建Modbus写入字数据的核心报文，需要指定地址，长度，站号，是否起始地址0，默认的功能码<br />
		/// To construct the core message of Modbus writing word data, you need to specify the address, length, 
		/// station number, whether the starting address is 0, and the default function code
		/// </summary>
		/// <param name="mAddress">Modbus的富文本地址</param>
		/// <param name="value">short的值</param>
		/// <returns>包含最终命令的结果对象</returns>
		public static OperateResult<byte[]> BuildWriteOneRegisterModbusCommand( ModbusAddress mAddress, short value )
		{
			byte[] content = new byte[6];
			content[0] = (byte)mAddress.Station;
			content[1] = (byte)mAddress.Function;
			content[2] = BitConverter.GetBytes( mAddress.Address )[1];
			content[3] = BitConverter.GetBytes( mAddress.Address )[0];
			content[4] = BitConverter.GetBytes( value )[1];
			content[5] = BitConverter.GetBytes( value )[0];
			return OperateResult.CreateSuccessResult( content );
		}

		/// <summary>
		/// 构建Modbus写入字数据的核心报文，需要指定地址，长度，站号，是否起始地址0，默认的功能码<br />
		/// To construct the core message of Modbus writing word data, you need to specify the address, length, 
		/// station number, whether the starting address is 0, and the default function code
		/// </summary>
		/// <param name="mAddress">Modbus的富文本地址</param>
		/// <param name="value">ushort的值</param>
		/// <returns>包含最终命令的结果对象</returns>
		public static OperateResult<byte[]> BuildWriteOneRegisterModbusCommand( ModbusAddress mAddress, ushort value )
		{
			byte[] content = new byte[6];
			content[0] = (byte)mAddress.Station;
			content[1] = (byte)mAddress.Function;
			content[2] = BitConverter.GetBytes( mAddress.Address )[1];
			content[3] = BitConverter.GetBytes( mAddress.Address )[0];
			content[4] = BitConverter.GetBytes( value )[1];
			content[5] = BitConverter.GetBytes( value )[0];
			return OperateResult.CreateSuccessResult( content );
		}

		/// <summary>
		/// 从返回的modbus的书内容中，提取出真实的数据，适用于写入和读取操作<br />
		/// Extract real data from the content of the returned modbus book, suitable for writing and reading operations
		/// </summary>
		/// <param name="response">返回的核心modbus报文信息</param>
		/// <returns>结果数据内容</returns>
		public static OperateResult<byte[]> ExtractActualData( byte[] response )
		{
			try
			{
				if (response[1] >= 0x80)
					return new OperateResult<byte[]>( ModbusInfo.GetDescriptionByErrorCode( response[2] ) );
				else if (response.Length > 3)
					return OperateResult.CreateSuccessResult( SoftBasic.ArrayRemoveBegin( response, 3 ) );
				else
					return OperateResult.CreateSuccessResult( new byte[0] );
			}
			catch(Exception ex)
			{
				return new OperateResult<byte[]>( ex.Message );
			}
		}

		/// <summary>
		/// 将modbus指令打包成Modbus-Tcp指令，需要指定ID信息来添加6个字节的报文头<br />
		/// Pack the Modbus command into Modbus-Tcp command, you need to specify the ID information to add a 6-byte message header
		/// </summary>
		/// <param name="modbus">Modbus核心指令</param>
		/// <param name="id">消息的序号</param>
		/// <returns>Modbus-Tcp指令</returns>
		public static byte[] PackCommandToTcp( byte[] modbus, ushort id )
		{
			byte[] buffer = new byte[modbus.Length + 6];
			buffer[0] = BitConverter.GetBytes( id )[1];
			buffer[1] = BitConverter.GetBytes( id )[0];

			buffer[4] = BitConverter.GetBytes( modbus.Length )[1];
			buffer[5] = BitConverter.GetBytes( modbus.Length )[0];

			modbus.CopyTo( buffer, 6 );
			return buffer;
		}

		/// <summary>
		/// 将modbus-tcp的报文数据重新还原成modbus指令，移除6个字节的报文头数据<br />
		/// Re-modify the message data of modbus-tcp into the modbus command, remove the 6-byte message header data
		/// </summary>
		/// <param name="modbusTcp">modbus-tcp的报文</param>
		/// <returns>modbus数据报文</returns>
		public static byte[] ExplodeTcpCommandToCore( byte[] modbusTcp ) => modbusTcp.RemoveBegin( 6 );

		/// <summary>
		/// 将modbus-rtu的数据重新还原成modbus数据，移除CRC校验的内容<br />
		/// Restore the data of modbus-rtu to modbus data again, remove the content of CRC check
		/// </summary>
		/// <param name="modbusRtu">modbus-rtu的报文</param>
		/// <returns>modbus数据报文</returns>
		public static byte[] ExplodeRtuCommandToCore( byte[] modbusRtu ) => modbusRtu.RemoveLast( 2 );

		/// <summary>
		/// 将modbus指令打包成Modbus-Rtu指令，在报文的末尾添加CRC16的校验码<br />
		/// Pack the modbus instruction into Modbus-Rtu instruction, add CRC16 check code at the end of the message
		/// </summary>
		/// <param name="modbus">Modbus指令</param>
		/// <returns>Modbus-Rtu指令</returns>
		public static byte[] PackCommandToRtu( byte[] modbus ) => SoftCRC16.CRC16( modbus );

		/// <summary>
		/// 将一个modbus核心的数据报文，转换成modbus-ascii的数据报文，增加LRC校验，增加首尾标记数据<br />
		/// Convert a Modbus core data message into a Modbus-ascii data message, add LRC check, and add head and tail tag data
		/// </summary>
		/// <param name="modbus">modbus-rtu的完整报文，携带相关的校验码</param>
		/// <returns>可以用于直接发送的modbus-ascii的报文</returns>
		public static byte[] TransModbusCoreToAsciiPackCommand( byte[] modbus )
		{
			// add LRC check
			byte[] modbus_lrc = SoftLRC.LRC( modbus );

			// Translate to ascii information
			byte[] modbus_ascii = SoftBasic.BytesToAsciiBytes( modbus_lrc );

			// add head and end informarion
			return SoftBasic.SpliceArray( new byte[] { 0x3A }, modbus_ascii, new byte[] { 0x0D, 0x0A } );
		}

		/// <summary>
		/// 将一个modbus-ascii的数据报文，转换成的modbus核心数据报文，移除首尾标记，移除LRC校验<br />
		/// Convert a Modbus-ascii data message into a Modbus core data message, remove the first and last tags, and remove the LRC check
		/// </summary>
		/// <param name="modbusAscii">modbus-ascii的完整报文，携带相关的校验码</param>
		/// <returns>可以用于直接发送的modbus的报文</returns>
		public static OperateResult<byte[]> TransAsciiPackCommandToCore( byte[] modbusAscii )
		{
			try
			{
				// response check
				if (modbusAscii[0] != 0x3A || modbusAscii[modbusAscii.Length - 2] != 0x0D || modbusAscii[modbusAscii.Length - 1] != 0x0A)
					return new OperateResult<byte[]>( ) { Message = StringResources.Language.ModbusAsciiFormatCheckFailed + modbusAscii.ToHexString( ' ' ) };

				// get modbus core
				byte[] modbus_core = SoftBasic.AsciiBytesToBytes( modbusAscii.RemoveDouble( 1, 2 ) );

				if (!Serial.SoftLRC.CheckLRC( modbus_core ))
					return new OperateResult<byte[]>( ) { Message = StringResources.Language.ModbusLRCCheckFailed + modbus_core.ToHexString( ' ' ) };

				// remove the last info
				return OperateResult.CreateSuccessResult( modbus_core.RemoveLast( 1 ) );
			}
			catch(Exception ex)
			{
				return new OperateResult<byte[]>( ) { Message = ex.Message + modbusAscii.ToHexString( ' ' ) };
			}
		}

		/// <summary>
		/// 分析Modbus协议的地址信息，该地址适应于tcp及rtu模式<br />
		/// Analysis of the address information of Modbus protocol, the address is adapted to tcp and rtu mode
		/// </summary>
		/// <param name="address">带格式的地址，比如"100"，"x=4;100"，"s=1;100","s=1;x=4;100"</param>
		/// <param name="defaultStation">默认的站号信息</param>
		/// <param name="isStartWithZero">起始地址是否从0开始</param>
		/// <param name="defaultFunction">默认的功能码信息</param>
		/// <returns>转换后的地址信息</returns>
		public static OperateResult<ModbusAddress> AnalysisAddress( string address, byte defaultStation, bool isStartWithZero, byte defaultFunction )
		{
			try
			{
				ModbusAddress mAddress = new ModbusAddress( address, defaultStation, defaultFunction );
				if (!isStartWithZero)
				{
					if (mAddress.Address < 1) throw new Exception( StringResources.Language.ModbusAddressMustMoreThanOne );
					mAddress.Address = (ushort)(mAddress.Address - 1);
				}
				return OperateResult.CreateSuccessResult( mAddress );
			}
			catch (Exception ex)
			{
				return new OperateResult<ModbusAddress>( ) { Message = ex.Message };
			}
		}

		/// <summary>
		/// 通过错误码来获取到对应的文本消息<br />
		/// Get the corresponding text message through the error code
		/// </summary>
		/// <param name="code">错误码</param>
		/// <returns>错误的文本描述</returns>
		public static string GetDescriptionByErrorCode( byte code )
		{
			switch (code)
			{
				case ModbusInfo.FunctionCodeNotSupport:               return StringResources.Language.ModbusTcpFunctionCodeNotSupport;
				case ModbusInfo.FunctionCodeOverBound:                return StringResources.Language.ModbusTcpFunctionCodeOverBound;
				case ModbusInfo.FunctionCodeQuantityOver:             return StringResources.Language.ModbusTcpFunctionCodeQuantityOver;
				case ModbusInfo.FunctionCodeReadWriteException:       return StringResources.Language.ModbusTcpFunctionCodeReadWriteException;
				default:                                              return StringResources.Language.UnknownError;
			}
		}

		/// <inheritdoc cref="SerialBase.CheckReceiveDataComplete(MemoryStream)"/>
		public static bool CheckRtuReceiveDataComplete( byte[] response )
		{
			if (response.Length > 2)
			{
				if (response[1] == ModbusInfo.WriteOneRegister ||
					response[1] == ModbusInfo.WriteRegister ||
					response[1] == ModbusInfo.WriteCoil ||
					response[1] == ModbusInfo.WriteOneCoil)
					return response.Length >= (6 + 2);
				else if (
					response[1] == ModbusInfo.ReadCoil ||
					response[1] == ModbusInfo.ReadDiscrete ||
					response[1] == ModbusInfo.ReadRegister ||
					response[1] == ModbusInfo.ReadInputRegister)
					return response.Length >= (response[2] + 3 + 2);
				else if (response[1] == ModbusInfo.WriteMaskRegister)
					return response.Length >= (8 + 2);
			}
			return false;
		}

		/// <inheritdoc cref="SerialBase.CheckReceiveDataComplete(MemoryStream)"/>
		public static bool CheckServerRtuReceiveDataComplete( byte[] receive )
		{
			if (receive.Length > 2)
			{
				if (receive[1] == ModbusInfo.WriteOneRegister ||
					receive[1] == ModbusInfo.WriteCoil
					)
					return receive.Length > 8 ? (receive.Length >= (receive[6] + 7 + 2)) : false;
				else if (
					receive[1] == ModbusInfo.ReadCoil ||
					receive[1] == ModbusInfo.ReadDiscrete ||
					receive[1] == ModbusInfo.ReadRegister ||
					receive[1] == ModbusInfo.ReadInputRegister ||
					receive[1] == ModbusInfo.WriteRegister ||
					receive[1] == ModbusInfo.WriteOneCoil)
					return receive.Length >= 8;
				else if (receive[1] == ModbusInfo.WriteMaskRegister)
					return receive.Length >= (8 + 2);
			}
			return false;
		}

		/// <inheritdoc cref="SerialBase.CheckReceiveDataComplete(MemoryStream)"/>
		public static bool CheckAsciiReceiveDataComplete( byte[] modbusAscii )
		{
			return CheckAsciiReceiveDataComplete( modbusAscii, modbusAscii.Length );
		}

		/// <inheritdoc cref="SerialBase.CheckReceiveDataComplete(MemoryStream)"/>
		public static bool CheckAsciiReceiveDataComplete( byte[] modbusAscii, int length )
		{
			if (length > 5)
				return modbusAscii[0] == 0x3A &&
					modbusAscii[length - 2] == 0x0D &&
					modbusAscii[length - 1] == 0x0A;
			else
				return false;
		}

		#endregion
	}
}
