using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HslCommunication.Core;
using HslCommunication.BasicFramework;
#if !NET20 && !NET35
using System.Threading.Tasks;
#endif

namespace HslCommunication.Profinet.Keyence
{
	/// <summary>
	/// KeyenceNano的基本辅助方法
	/// </summary>
	public class KeyenceNanoHelper
	{
		#region Static Method Helper

		/// <summary>
		/// 连接PLC的命令报文<br />
		/// Command message to connect to PLC
		/// </summary>
		/// <param name="station">当前PLC的站号信息</param>
		/// <param name="useStation">是否启动站号命令</param>
		public static byte[] GetConnectCmd( byte station, bool useStation ) =>
			useStation ? Encoding.ASCII.GetBytes( $"CR {station:D2}\r" ) : Encoding.ASCII.GetBytes( $"CR\r" );

		/// <summary>
		/// 断开PLC连接的命令报文<br />
		/// Command message to disconnect PLC
		/// </summary>
		/// <param name="station">当前PLC的站号信息</param>
		/// <param name="useStation">是否启动站号命令</param>
		public static byte[] GetDisConnectCmd( byte station, bool useStation ) => Encoding.ASCII.GetBytes( $"CQ\r" );

		/// <summary>
		/// 获取当前的地址类型是字数据的倍数关系
		/// </summary>
		/// <param name="type">地址的类型</param>
		/// <returns>倍数关系</returns>
		public static int GetWordAddressMultiple( string type )
		{
			if (type == "CTH" || type == "CTC" || type == "C" || type == "T" || type == "TS" || type == "TC" || type == "CS" || type == "CC" || type == "AT")
				return 2;
			else if (type == "DM" || type == "CM" || type == "TM" || type == "EM" || type == "FM" || type == "Z" || type == "W" || type == "ZF" || type == "VM")
				return 1;
			return 1;
		}

		/// <summary>
		/// 建立读取PLC数据的指令，需要传入地址数据，以及读取的长度，地址示例参照类的说明文档<br />
		/// To create a command to read PLC data, you need to pass in the address data, and the length of the read. For an example of the address, refer to the class documentation
		/// </summary>
		/// <param name="address">软元件地址</param>
		/// <param name="length">读取长度</param>
		/// <returns>是否建立成功</returns>
		public static OperateResult<byte[]> BuildReadCommand( string address, ushort length )
		{
			var addressResult = KvAnalysisAddress( address );
			if (!addressResult.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( addressResult );

			if (length > 1) length = (ushort)(length / GetWordAddressMultiple( addressResult.Content1 ));

			StringBuilder cmd = new StringBuilder( );
			cmd.Append( "RDS" );                               // 批量读取
			cmd.Append( " " );                                 // 空格符
			cmd.Append( addressResult.Content1 );              // 软元件类型，如DM
			cmd.Append( addressResult.Content2.ToString( ) );  // 软元件的地址，如1000
			cmd.Append( " " );                                 // 空格符
			cmd.Append( length.ToString( ) );
			cmd.Append( "\r" );                                //结束符

			byte[] _PLCCommand = Encoding.ASCII.GetBytes( cmd.ToString( ) );
			return OperateResult.CreateSuccessResult( _PLCCommand );
		}

		/// <summary>
		/// 建立写入PLC数据的指令，需要传入地址数据，以及写入的数据信息，地址示例参照类的说明文档<br />
		/// To create a command to write PLC data, you need to pass in the address data and the written data information. For an example of the address, refer to the class documentation
		/// </summary>
		/// <param name="address">软元件地址</param>
		/// <param name="value">转换后的数据</param>
		/// <returns>是否成功的信息</returns>
		public static OperateResult<byte[]> BuildWriteCommand( string address, byte[] value )
		{
			var addressResult = KvAnalysisAddress( address );
			if (!addressResult.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( addressResult );

			StringBuilder cmd = new StringBuilder( );
			cmd.Append( "WRS" );                         // 批量读取
			cmd.Append( " " );                           // 空格符
			cmd.Append( addressResult.Content1 );        // 软元件地址
			cmd.Append( addressResult.Content2 );        // 软元件地址
			cmd.Append( " " );                           // 空格符
			int length = value.Length / (GetWordAddressMultiple( addressResult.Content1 ) * 2);
			cmd.Append( length.ToString( ) );
			for (int i = 0; i < length; i++)
			{
				cmd.Append( " " );
				cmd.Append( BitConverter.ToUInt16( value, i * GetWordAddressMultiple( addressResult.Content1 ) * 2 ) );
			}
			cmd.Append( "\r" );

			return OperateResult.CreateSuccessResult( Encoding.ASCII.GetBytes( cmd.ToString( ) ) );
		}

		/// <summary>
		/// 构建写入扩展单元缓冲寄存器的报文命令，需要传入单元编号，地址，写入的数据，实际写入的数据格式才有无符号的方式<br />
		/// To construct a message command to write to the buffer register of the expansion unit, the unit number, address, 
		/// and data to be written need to be passed in, and the format of the actually written data is unsigned.
		/// </summary>
		/// <param name="unit">单元编号0~48</param>
		/// <param name="address">地址0~32767</param>
		/// <param name="value">写入的数据信息，单次交互最大256个字</param>
		/// <returns>包含是否成功的报文对象</returns>
		public static OperateResult<byte[]> BuildWriteExpansionMemoryCommand( byte unit, ushort address, byte[] value )
		{
			StringBuilder cmd = new StringBuilder( );
			cmd.Append( "UWR" );                         // 批量读取
			cmd.Append( " " );                           // 空格符
			cmd.Append( unit );                          // 单元编号
			cmd.Append( " " );                           // 空格符
			cmd.Append( address );                       // 地址
			cmd.Append( ".U" );                          // 数据格式
			cmd.Append( " " );                           // 空格符
			int length = value.Length / 2;
			cmd.Append( length.ToString( ) );
			for (int i = 0; i < length; i++)
			{
				cmd.Append( " " );
				cmd.Append( BitConverter.ToUInt16( value, i * 2 ) );
			}
			cmd.Append( "\r" );

			return OperateResult.CreateSuccessResult( Encoding.ASCII.GetBytes( cmd.ToString( ) ) );
		}

		/// <summary>
		/// 建立写入bool数据的指令，针对地址类型为 R,CR,MR,LR<br />
		/// Create instructions to write bool data, address type is R, CR, MR, LR
		/// </summary>
		/// <param name="address">软元件地址</param>
		/// <param name="value">转换后的数据</param>
		/// <returns>是否成功的信息</returns>
		public static OperateResult<byte[]> BuildWriteCommand( string address, bool value )
		{
			var addressResult = KvAnalysisAddress( address );
			if (!addressResult.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( addressResult );

			StringBuilder cmd = new StringBuilder( );
			if (value)
				cmd.Append( "ST" );                      // 置位
			else
				cmd.Append( "RS" );                      // 复位
			cmd.Append( " " );                           // 空格符
			cmd.Append( addressResult.Content1 );        // 软元件地址
			cmd.Append( addressResult.Content2 );        // 软元件地址
			cmd.Append( "\r" );                          // 空格符
			return OperateResult.CreateSuccessResult( Encoding.ASCII.GetBytes( cmd.ToString( ) ) );
		}

		/// <summary>
		/// 批量写入数据位到plc地址，针对地址格式为 R,B,CR,MR,LR,VB<br />
		/// Write data bits in batches to the plc address, and the address format is R, B, CR, MR, LR, VB
		/// </summary>
		/// <param name="address">PLC的地址</param>
		/// <param name="value">等待写入的bool数组</param>
		/// <returns>写入bool数组的命令报文</returns>
		public static OperateResult<byte[]> BuildWriteCommand( string address, bool[] value )
		{
			var addressResult = KvAnalysisAddress( address );
			if (!addressResult.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( addressResult );

			StringBuilder cmd = new StringBuilder( );
			cmd.Append( "WRS" );
			cmd.Append( " " );                           // 空格符
			cmd.Append( addressResult.Content1 );        // 软元件地址
			cmd.Append( addressResult.Content2 );        // 软元件地址
			cmd.Append( " " );                           // 空格符
			cmd.Append( value.Length.ToString( ) );      // 写入的数据长度
			for (int i = 0; i < value.Length; i++)
			{
				cmd.Append( " " );                           // 空格符
				cmd.Append( value[i] ? "1" : "0" );
			}
			cmd.Append( "\r" );                          // 空格符
			return OperateResult.CreateSuccessResult( Encoding.ASCII.GetBytes( cmd.ToString( ) ) );
		}

		private static string GetErrorText( string err )
		{
			if (err.StartsWith( "E0" )) return StringResources.Language.KeyenceNanoE0;
			if (err.StartsWith( "E1" )) return StringResources.Language.KeyenceNanoE1;
			if (err.StartsWith( "E2" )) return StringResources.Language.KeyenceNanoE2;
			if (err.StartsWith( "E4" )) return StringResources.Language.KeyenceNanoE4;
			if (err.StartsWith( "E5" )) return StringResources.Language.KeyenceNanoE5;
			if (err.StartsWith( "E6" )) return StringResources.Language.KeyenceNanoE6;
			return StringResources.Language.UnknownError + " " + err;
		}

		/// <summary>
		/// 校验读取返回数据状态，主要返回的第一个字节是不是E<br />
		/// Check the status of the data returned from reading, whether the first byte returned is E
		/// </summary>
		/// <param name="ack">反馈信息</param>
		/// <returns>是否成功的信息</returns>
		public static OperateResult CheckPlcReadResponse( byte[] ack )
		{
			if (ack.Length == 0) return new OperateResult( StringResources.Language.MelsecFxReceiveZero );
			if (ack[0] == 0x45) return new OperateResult( GetErrorText( Encoding.ASCII.GetString( ack ) ) );
			if ((ack[ack.Length - 1] != 0x0A) && (ack[ack.Length - 2] != 0x0D)) return new OperateResult( StringResources.Language.MelsecFxAckWrong + " Actual: " + SoftBasic.ByteToHexString( ack, ' ' ) );
			return OperateResult.CreateSuccessResult( );
		}

		/// <summary>
		/// 校验写入返回数据状态，检测返回的数据是不是OK<br />
		/// Verify the status of the returned data written and check whether the returned data is OK
		/// </summary>
		/// <param name="ack">反馈信息</param>
		/// <returns>是否成功的信息</returns>
		public static OperateResult CheckPlcWriteResponse( byte[] ack )
		{
			if (ack.Length == 0) return new OperateResult( StringResources.Language.MelsecFxReceiveZero );
			if (ack[0] == 0x4F && ack[1] == 0x4B) return OperateResult.CreateSuccessResult( );
			return new OperateResult( GetErrorText( Encoding.ASCII.GetString( ack ) ) );
		}

		/// <summary>
		/// 从PLC反馈的数据进行提炼Bool操作<br />
		/// Refine Bool operation from data fed back from PLC
		/// </summary>
		/// <param name="addressType">地址的数据类型</param>
		/// <param name="response">PLC反馈的真实数据</param>
		/// <returns>数据提炼后的真实数据</returns>
		public static OperateResult<bool[]> ExtractActualBoolData( string addressType, byte[] response )
		{
			try
			{
				if (string.IsNullOrEmpty( addressType )) addressType = "R";

				string strResponse = Encoding.Default.GetString( response.RemoveLast( 2 ) );
				if (addressType == "R" || addressType == "CR" || addressType == "MR" || addressType == "LR" || addressType == "B" || addressType == "VB")
				{
					return OperateResult.CreateSuccessResult( strResponse.Split( new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries ).Select( m => m == "1" ).ToArray( ) );
				}
				else if (addressType == "T" || addressType == "C" || addressType == "CTH" || addressType == "CTC")
				{
					return OperateResult.CreateSuccessResult(
						strResponse.Split( new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries ).Select( m => m.StartsWith( "1" ) ).ToArray( ) );
				}
				else
				{
					return new OperateResult<bool[]>( StringResources.Language.NotSupportedDataType );
				}
			}
			catch (Exception ex)
			{
				return new OperateResult<bool[]>( )
				{
					Message = "Extract Msg：" + ex.Message + Environment.NewLine +
					"Data: " + SoftBasic.ByteToHexString( response )
				};
			}
		}

		/// <summary>
		/// 从PLC反馈的数据进行提炼操作<br />
		/// Refining operation from data fed back from PLC
		/// </summary>
		/// <param name="addressType">地址的数据类型</param>
		/// <param name="response">PLC反馈的真实数据</param>
		/// <returns>数据提炼后的真实数据</returns>
		public static OperateResult<byte[]> ExtractActualData( string addressType, byte[] response )
		{
			try
			{
				if (string.IsNullOrEmpty( addressType )) addressType = "R";

				string strResponse = Encoding.Default.GetString( response.RemoveLast( 2 ) );
				string[] splits = strResponse.Split( new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries );
				if (addressType == "DM" || addressType == "EM" || addressType == "FM" || addressType == "ZF" || addressType == "W" ||
					addressType == "TM" || addressType == "Z" || addressType == "CM" || addressType == "VM")
				{
					byte[] buffer = new byte[splits.Length * 2];
					for (int i = 0; i < splits.Length; i++)
					{
						BitConverter.GetBytes( ushort.Parse( splits[i] ) ).CopyTo( buffer, i * 2 );
					}
					return OperateResult.CreateSuccessResult( buffer );
				}
				else if (addressType == "AT" || addressType == "TC" || addressType == "CC" || addressType == "TS" || addressType == "CS")
				{
					byte[] buffer = new byte[splits.Length * 4];
					for (int i = 0; i < splits.Length; i++)
					{
						BitConverter.GetBytes( uint.Parse( splits[i] ) ).CopyTo( buffer, i * 4 );
					}
					return OperateResult.CreateSuccessResult( buffer );
				}
				else if (addressType == "T" || addressType == "C" || addressType == "CTH" || addressType == "CTC")
				{
					byte[] buffer = new byte[splits.Length * 4];
					for (int i = 0; i < splits.Length; i++)
					{
						string[] datas = splits[i].Split( new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries );
						BitConverter.GetBytes( uint.Parse( datas[1] ) ).CopyTo( buffer, i * 4 );
					}
					return OperateResult.CreateSuccessResult( buffer );
				}
				else
				{
					return new OperateResult<byte[]>( StringResources.Language.NotSupportedDataType );
				}
			}
			catch (Exception ex)
			{
				return new OperateResult<byte[]>( )
				{
					Message = "Extract Msg：" + ex.Message + Environment.NewLine +
					"Data: " + SoftBasic.ByteToHexString( response )
				};
			}
		}

		/// <summary>
		/// 解析数据地址成不同的Keyence地址类型<br />
		/// Parse data addresses into different keyence address types
		/// </summary>
		/// <param name="address">数据地址</param>
		/// <returns>地址结果对象</returns>
		public static OperateResult<string, int> KvAnalysisAddress( string address )
		{
			try
			{
				if (address.StartsWith( "CTH" ) || address.StartsWith( "cth" ))
					return OperateResult.CreateSuccessResult( "CTH", int.Parse( address.Substring( 3 ) ) );
				else if (address.StartsWith( "CTC" ) || address.StartsWith( "ctc" ))
					return OperateResult.CreateSuccessResult( "CTC", int.Parse( address.Substring( 3 ) ) );
				else if (address.StartsWith( "CR" ) || address.StartsWith( "cr" ))
					return OperateResult.CreateSuccessResult( "CR", int.Parse( address.Substring( 2 ) ) );
				else if (address.StartsWith( "MR" ) || address.StartsWith( "mr" ))
					return OperateResult.CreateSuccessResult( "MR", int.Parse( address.Substring( 2 ) ) );
				else if (address.StartsWith( "LR" ) || address.StartsWith( "lr" ))
					return OperateResult.CreateSuccessResult( "LR", int.Parse( address.Substring( 2 ) ) );
				else if (address.StartsWith( "DM" ) || address.StartsWith( "DM" ))
					return OperateResult.CreateSuccessResult( "DM", int.Parse( address.Substring( 2 ) ) );
				else if (address.StartsWith( "CM" ) || address.StartsWith( "cm" ))
					return OperateResult.CreateSuccessResult( "CM", int.Parse( address.Substring( 2 ) ) );
				else if (address.StartsWith( "W" ) || address.StartsWith( "w" ))
					return OperateResult.CreateSuccessResult( "W", int.Parse( address.Substring( 1 ) ) );
				else if (address.StartsWith( "TM" ) || address.StartsWith( "tm" ))
					return OperateResult.CreateSuccessResult( "TM", int.Parse( address.Substring( 2 ) ) );
				else if (address.StartsWith( "VM" ) || address.StartsWith( "vm" ))
					return OperateResult.CreateSuccessResult( "VM", int.Parse( address.Substring( 2 ) ) );
				else if (address.StartsWith( "EM" ) || address.StartsWith( "em" ))
					return OperateResult.CreateSuccessResult( "EM", int.Parse( address.Substring( 2 ) ) );
				else if (address.StartsWith( "FM" ) || address.StartsWith( "fm" ))
					return OperateResult.CreateSuccessResult( "EM", int.Parse( address.Substring( 2 ) ) );
				else if (address.StartsWith( "ZF" ) || address.StartsWith( "zf" ))
					return OperateResult.CreateSuccessResult( "ZF", int.Parse( address.Substring( 2 ) ) );
				else if (address.StartsWith( "AT" ) || address.StartsWith( "at" ))
					return OperateResult.CreateSuccessResult( "AT", int.Parse( address.Substring( 2 ) ) );
				else if (address.StartsWith( "TS" ) || address.StartsWith( "ts" ))
					return OperateResult.CreateSuccessResult( "TS", int.Parse( address.Substring( 2 ) ) );
				else if (address.StartsWith( "TC" ) || address.StartsWith( "tc" ))
					return OperateResult.CreateSuccessResult( "TC", int.Parse( address.Substring( 2 ) ) );
				else if (address.StartsWith( "CC" ) || address.StartsWith( "cc" ))
					return OperateResult.CreateSuccessResult( "CC", int.Parse( address.Substring( 2 ) ) );
				else if (address.StartsWith( "CS" ) || address.StartsWith( "cs" ))
					return OperateResult.CreateSuccessResult( "CS", int.Parse( address.Substring( 2 ) ) );
				else if (address.StartsWith( "Z" ) || address.StartsWith( "z" ))
					return OperateResult.CreateSuccessResult( "Z", int.Parse( address.Substring( 1 ) ) );
				else if (address.StartsWith( "R" ) || address.StartsWith( "r" ))
					return OperateResult.CreateSuccessResult( "", int.Parse( address.Substring( 1 ) ) );
				else if (address.StartsWith( "B" ) || address.StartsWith( "b" ))
					return OperateResult.CreateSuccessResult( "B", int.Parse( address.Substring( 1 ) ) );
				else if (address.StartsWith( "T" ) || address.StartsWith( "t" ))
					return OperateResult.CreateSuccessResult( "T", int.Parse( address.Substring( 1 ) ) );
				else if (address.StartsWith( "C" ) || address.StartsWith( "c" ))
					return OperateResult.CreateSuccessResult( "C", int.Parse( address.Substring( 1 ) ) );
				else
					throw new Exception( StringResources.Language.NotSupportedDataType );
			}
			catch (Exception ex)
			{
				return new OperateResult<string, int>( ex.Message );
			}
		}

		#endregion

		/// <inheritdoc cref="IReadWriteNet.Read(string, ushort)"/>
		public static OperateResult<byte[]> Read( IReadWriteDevice keyence, string address, ushort length )
		{
			if (address.StartsWith( "unit=" ))
			{
				byte unit = (byte)HslHelper.ExtractParameter( ref address, "unit", 0 );
				if (!ushort.TryParse( address, out ushort offset )) return new OperateResult<byte[]>( "Address is not right, convert ushort wrong!" );

				return ReadExpansionMemory( keyence, unit, ushort.Parse( address ), length );
			}

			// 获取指令
			OperateResult<byte[]> command = BuildReadCommand( address, length );
			if (!command.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( command );

			// 核心交互
			OperateResult<byte[]> read = keyence.ReadFromCoreServer( command.Content );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( read );

			// 反馈检查
			OperateResult ackResult = CheckPlcReadResponse( read.Content );
			if (!ackResult.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( ackResult );

			var addressResult = KvAnalysisAddress( address );
			if (!addressResult.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( addressResult );

			// 数据提炼
			return ExtractActualData( addressResult.Content1, read.Content );
		}

		/// <inheritdoc cref="IReadWriteNet.Write(string, byte[])"/>
		public static OperateResult Write( IReadWriteDevice keyence, string address, byte[] value )
		{
			// 获取写入
			OperateResult<byte[]> command = BuildWriteCommand( address, value );
			if (!command.IsSuccess) return command;

			// 核心交互
			OperateResult<byte[]> read = keyence.ReadFromCoreServer( command.Content );
			if (!read.IsSuccess) return read;

			// 结果验证
			OperateResult checkResult = CheckPlcWriteResponse( read.Content );
			if (!checkResult.IsSuccess) return checkResult;

			return OperateResult.CreateSuccessResult( );
		}

		#region Async Read Write Support
#if !NET35 && !NET20
		/// <inheritdoc cref="Read(IReadWriteDevice, string, ushort)"/>
		public static async Task<OperateResult<byte[]>> ReadAsync( IReadWriteDevice keyence, string address, ushort length )
		{
			if (address.StartsWith( "unit=" ))
			{
				byte unit = (byte)HslHelper.ExtractParameter( ref address, "unit", 0 );
				if (!ushort.TryParse( address, out ushort offset )) return new OperateResult<byte[]>( "Address is not right, convert ushort wrong!" );

				return await ReadExpansionMemoryAsync( keyence, unit, ushort.Parse( address ), length );
			}

			// 获取指令
			OperateResult<byte[]> command = BuildReadCommand( address, length );
			if (!command.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( command );

			// 核心交互
			OperateResult<byte[]> read = await keyence.ReadFromCoreServerAsync( command.Content );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( read );

			// 反馈检查
			OperateResult ackResult = CheckPlcReadResponse( read.Content );
			if (!ackResult.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( ackResult );

			var addressResult = KvAnalysisAddress( address );
			if (!addressResult.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( addressResult );

			// 数据提炼
			return ExtractActualData( addressResult.Content1, read.Content );
		}

		/// <inheritdoc cref="Write(IReadWriteDevice, string, byte[])"/>
		public static async Task<OperateResult> WriteAsync( IReadWriteDevice keyence, string address, byte[] value )
		{
			// 获取写入
			OperateResult<byte[]> command = BuildWriteCommand( address, value );
			if (!command.IsSuccess) return command;

			// 核心交互
			OperateResult<byte[]> read = await keyence.ReadFromCoreServerAsync( command.Content );
			if (!read.IsSuccess) return read;

			// 结果验证
			OperateResult checkResult = CheckPlcWriteResponse( read.Content );
			if (!checkResult.IsSuccess) return checkResult;

			return OperateResult.CreateSuccessResult( );
		}
#endif
		#endregion

		#region Read Write Bool

		/// <inheritdoc/>
		public static OperateResult<bool[]> ReadBool( IReadWriteDevice keyence, string address, ushort length )
		{
			// 获取指令
			OperateResult<byte[]> command = BuildReadCommand( address, length );
			if (!command.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( command );

			// 核心交互
			OperateResult<byte[]> read = keyence.ReadFromCoreServer( command.Content );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( read );

			// 反馈检查
			OperateResult ackResult = CheckPlcReadResponse( read.Content );
			if (!ackResult.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( ackResult );

			var addressResult = KvAnalysisAddress( address );
			if (!addressResult.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( addressResult );

			// 数据提炼
			return ExtractActualBoolData( addressResult.Content1, read.Content );
		}

		/// <inheritdoc/>
		public static OperateResult Write( IReadWriteDevice keyence, string address, bool value )
		{
			// 获取写入
			OperateResult<byte[]> command = BuildWriteCommand( address, value );
			if (!command.IsSuccess) return command;

			// 核心交互
			OperateResult<byte[]> read = keyence.ReadFromCoreServer( command.Content );
			if (!read.IsSuccess) return read;

			// 结果验证
			OperateResult checkResult = CheckPlcWriteResponse( read.Content );
			if (!checkResult.IsSuccess) return checkResult;

			return OperateResult.CreateSuccessResult( );
		}

		/// <inheritdoc cref="IReadWriteNet.Write(string, bool[])"/>
		public static OperateResult Write( IReadWriteDevice keyence, string address, bool[] value )
		{
			// 获取写入
			OperateResult<byte[]> command = BuildWriteCommand( address, value );
			if (!command.IsSuccess) return command;

			// 核心交互
			OperateResult<byte[]> read = keyence.ReadFromCoreServer( command.Content );
			if (!read.IsSuccess) return read;

			// 结果验证
			OperateResult checkResult = CheckPlcWriteResponse( read.Content );
			if (!checkResult.IsSuccess) return checkResult;

			return OperateResult.CreateSuccessResult( );
		}

		#endregion

		#region Async Read Write Bool
#if !NET35 && !NET20
		/// <inheritdoc cref="IReadWriteNet.ReadBool(string, ushort)"/>
		public static async Task<OperateResult<bool[]>> ReadBoolAsync( IReadWriteDevice keyence, string address, ushort length )
		{
			// 获取指令
			OperateResult<byte[]> command = BuildReadCommand( address, length );
			if (!command.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( command );

			// 核心交互
			OperateResult<byte[]> read = await keyence.ReadFromCoreServerAsync( command.Content );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( read );

			// 反馈检查
			OperateResult ackResult = CheckPlcReadResponse( read.Content );
			if (!ackResult.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( ackResult );

			var addressResult = KvAnalysisAddress( address );
			if (!addressResult.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( addressResult );

			// 数据提炼
			return ExtractActualBoolData( addressResult.Content1, read.Content );
		}

		/// <inheritdoc cref="IReadWriteNet.Write(string, bool)"/>
		public static async Task<OperateResult> WriteAsync( IReadWriteDevice keyence, string address, bool value )
		{
			// 获取写入
			OperateResult<byte[]> command = BuildWriteCommand( address, value );
			if (!command.IsSuccess) return command;

			// 核心交互
			OperateResult<byte[]> read = await keyence.ReadFromCoreServerAsync( command.Content );
			if (!read.IsSuccess) return read;

			// 结果验证
			OperateResult checkResult = CheckPlcWriteResponse( read.Content );
			if (!checkResult.IsSuccess) return checkResult;

			return OperateResult.CreateSuccessResult( );
		}

		/// <inheritdoc cref="IReadWriteNet.Write(string, bool[])"/>
		public async static Task<OperateResult> WriteAsync( IReadWriteDevice keyence, string address, bool[] value )
		{
			// 获取写入
			OperateResult<byte[]> command = BuildWriteCommand( address, value );
			if (!command.IsSuccess) return command;

			// 核心交互
			OperateResult<byte[]> read = await keyence.ReadFromCoreServerAsync( command.Content );
			if (!read.IsSuccess) return read;

			// 结果验证
			OperateResult checkResult = CheckPlcWriteResponse( read.Content );
			if (!checkResult.IsSuccess) return checkResult;

			return OperateResult.CreateSuccessResult( );
		}
#endif
		#endregion

		/// <summary>
		/// <c>[商业授权]</c> 查询PLC的型号信息<br />
		/// <b>[Authorization]</b> Query PLC model information
		/// </summary>
		/// <param name="keyence">PLC通信对象</param>
		/// <returns>包含型号的结果对象</returns>
		internal static OperateResult<KeyencePLCS> ReadPlcType( IReadWriteDevice keyence )
		{
			if (!Authorization.asdniasnfaksndiqwhawfskhfaiw( )) return new OperateResult<KeyencePLCS>( StringResources.Language.InsufficientPrivileges );

			var read = keyence.ReadFromCoreServer( Encoding.ASCII.GetBytes( "?K\r" ) );
			if (!read.IsSuccess) return read.ConvertFailed<KeyencePLCS>( );

			var check = CheckPlcReadResponse( read.Content );
			if (!check.IsSuccess) return check.ConvertFailed<KeyencePLCS>( );

			string type = Encoding.ASCII.GetString( read.Content.RemoveLast( 2 ) );
			switch (type)
			{
				case "48":
				case "49": return OperateResult.CreateSuccessResult( KeyencePLCS.KV700 );
				case "50": return OperateResult.CreateSuccessResult( KeyencePLCS.KV1000 );
				case "51": return OperateResult.CreateSuccessResult( KeyencePLCS.KV3000 );
				case "52": return OperateResult.CreateSuccessResult( KeyencePLCS.KV5000 );
				case "53": return OperateResult.CreateSuccessResult( KeyencePLCS.KV5500 );
				default: return new OperateResult<KeyencePLCS>( $"Unknow type:" + type );
			}
		}
#if !NET20 && !NET35
		/// <inheritdoc cref="ReadPlcType(IReadWriteDevice)"/>
		internal static async Task<OperateResult<KeyencePLCS>> ReadPlcTypeAsync( IReadWriteDevice keyence )
		{
			if (!Authorization.asdniasnfaksndiqwhawfskhfaiw( )) return new OperateResult<KeyencePLCS>( StringResources.Language.InsufficientPrivileges );

			var read = await keyence.ReadFromCoreServerAsync( Encoding.ASCII.GetBytes( "?K\r" ) );
			if (!read.IsSuccess) return read.ConvertFailed<KeyencePLCS>( );

			var check = CheckPlcReadResponse( read.Content );
			if (!check.IsSuccess) return check.ConvertFailed<KeyencePLCS>( );

			string type = Encoding.ASCII.GetString( read.Content.RemoveLast( 2 ) );
			switch (type)
			{
				case "48":
				case "49": return OperateResult.CreateSuccessResult( KeyencePLCS.KV700 );
				case "50": return OperateResult.CreateSuccessResult( KeyencePLCS.KV1000 );
				case "51": return OperateResult.CreateSuccessResult( KeyencePLCS.KV3000 );
				case "52": return OperateResult.CreateSuccessResult( KeyencePLCS.KV5000 );
				case "53": return OperateResult.CreateSuccessResult( KeyencePLCS.KV5500 );
				default: return new OperateResult<KeyencePLCS>( $"Unknow type:" + type );
			}
		}
#endif
		/// <summary>
		/// <c>[商业授权]</c> 读取当前PLC的模式，如果是0，代表 PROG模式或者梯形图未登录，如果为1，代表RUN模式<br />
		/// <b>[Authorization]</b> Read the current PLC mode, if it is 0, it means PROG mode or the ladder diagram is not registered, if it is 1, it means RUN mode
		/// </summary>
		/// <param name="keyence">PLC通信对象</param>
		/// <returns>包含模式的结果对象</returns>
		internal static OperateResult<int> ReadPlcMode( IReadWriteDevice keyence )
		{
			if (!Authorization.asdniasnfaksndiqwhawfskhfaiw( )) return new OperateResult<int>( StringResources.Language.InsufficientPrivileges );

			var read = keyence.ReadFromCoreServer( Encoding.ASCII.GetBytes( "?M\r" ) );
			if (!read.IsSuccess) return read.ConvertFailed<int>( );

			var check = CheckPlcReadResponse( read.Content );
			if (!check.IsSuccess) return check.ConvertFailed<int>( );

			string type = Encoding.ASCII.GetString( read.Content.RemoveLast( 2 ) );
			if (type == "0") return OperateResult.CreateSuccessResult( 0 );
			return OperateResult.CreateSuccessResult( 1 );
		}
#if !NET20 && !NET35
		/// <inheritdoc cref="ReadPlcMode(IReadWriteDevice)"/>
		internal static async Task<OperateResult<int>> ReadPlcModeAsync( IReadWriteDevice keyence )
		{
			if (!Authorization.asdniasnfaksndiqwhawfskhfaiw( )) return new OperateResult<int>( StringResources.Language.InsufficientPrivileges );

			var read = await keyence.ReadFromCoreServerAsync( Encoding.ASCII.GetBytes( "?M\r" ) );
			if (!read.IsSuccess) return read.ConvertFailed<int>( );

			var check = CheckPlcReadResponse( read.Content );
			if (!check.IsSuccess) return check.ConvertFailed<int>( );

			string type = Encoding.ASCII.GetString( read.Content.RemoveLast( 2 ) );
			if (type == "0") return OperateResult.CreateSuccessResult( 0 );
			return OperateResult.CreateSuccessResult( 1 );
		}
#endif
		/// <summary>
		/// <c>[商业授权]</c> 设置PLC的时间<br />
		/// <b>[Authorization]</b> Set PLC time
		/// </summary>
		/// <param name="keyence">PLC通信对象</param>
		/// <param name="dateTime">时间数据</param>
		/// <returns>是否设置成功</returns>
		public static OperateResult SetPlcDateTime( IReadWriteDevice keyence, DateTime dateTime )
		{
			if (!Authorization.asdniasnfaksndiqwhawfskhfaiw( )) return new OperateResult<int>( StringResources.Language.InsufficientPrivileges );

			var read = keyence.ReadFromCoreServer( Encoding.ASCII.GetBytes( $"WRT {dateTime.Year - 2000:D2} {dateTime.Month:D2} {dateTime.Day:D2} " +
				$"{dateTime.Hour:D2} {dateTime.Minute:D2} {dateTime.Second:D2} {(int)dateTime.DayOfWeek}\r" ) );
			if (!read.IsSuccess) return read.ConvertFailed<int>( );

			return CheckPlcWriteResponse( read.Content );
		}

#if !NET20 && !NET35
		/// <inheritdoc cref="SetPlcDateTime(IReadWriteDevice, DateTime)"/>
		public static async Task<OperateResult> SetPlcDateTimeAsync( IReadWriteDevice keyence, DateTime dateTime )
		{
			if (!Authorization.asdniasnfaksndiqwhawfskhfaiw( )) return new OperateResult<int>( StringResources.Language.InsufficientPrivileges );

			var read = await keyence.ReadFromCoreServerAsync( Encoding.ASCII.GetBytes( $"WRT {dateTime.Year - 2000:D2} {dateTime.Month:D2} {dateTime.Day:D2} " +
				$"{dateTime.Hour:D2} {dateTime.Minute:D2} {dateTime.Second:D2} {(int)dateTime.DayOfWeek}\r" ) );
			if (!read.IsSuccess) return read;

			return CheckPlcWriteResponse( read.Content );
		}
#endif
		/// <summary>
		/// <c>[商业授权]</c> 读取指定软元件的注释信息<br />
		/// <b>[Authorization]</b> Read the comment information of the specified device
		/// </summary>
		/// <param name="keyence">PLC通信对象</param>
		/// <param name="address">软元件的地址</param>
		/// <returns>软元件的注释信息</returns>
		public static OperateResult<string> ReadAddressAnnotation( IReadWriteDevice keyence, string address )
		{
			if (!Authorization.asdniasnfaksndiqwhawfskhfaiw( )) return new OperateResult<string>( StringResources.Language.InsufficientPrivileges );

			var read = keyence.ReadFromCoreServer( Encoding.ASCII.GetBytes( $"RDC {address}\r" ) );
			if (!read.IsSuccess) return read.ConvertFailed<string>( );

			var check = CheckPlcReadResponse( read.Content );
			if (!check.IsSuccess) return check.ConvertFailed<string>( );

			return OperateResult.CreateSuccessResult( Encoding.ASCII.GetString( read.Content.RemoveLast( 2 ) ).Trim( ' ' ) );
		}

#if !NET20 && !NET35
		/// <inheritdoc cref="ReadAddressAnnotation(IReadWriteDevice, string)"/>
		public static async Task<OperateResult<string>> ReadAddressAnnotationAsync( IReadWriteDevice keyence, string address )
		{
			if (!Authorization.asdniasnfaksndiqwhawfskhfaiw( )) return new OperateResult<string>( StringResources.Language.InsufficientPrivileges );

			var read = await keyence.ReadFromCoreServerAsync( Encoding.ASCII.GetBytes( $"RDC {address}\r" ) );
			if (!read.IsSuccess) return read.ConvertFailed<string>( );

			var check = CheckPlcReadResponse( read.Content );
			if (!check.IsSuccess) return check.ConvertFailed<string>( );

			return OperateResult.CreateSuccessResult( Encoding.ASCII.GetString( read.Content.RemoveLast( 2 ) ).Trim( ' ' ) );
		}
#endif
		/// <summary>
		/// <c>[商业授权]</c> 从扩展单元缓冲存储器连续读取指定个数的数据，单位为字<br />
		/// <b>[Authorization]</b> Continuously read the specified number of data from the expansion unit buffer memory, the unit is word
		/// </summary>
		/// <param name="keyence">PLC的通信对象</param>
		/// <param name="unit">单元编号</param>
		/// <param name="address">偏移地址</param>
		/// <param name="length">读取的长度，单位为字</param>
		/// <returns>包含是否成功的原始字节数组</returns>
		public static OperateResult<byte[]> ReadExpansionMemory( IReadWriteDevice keyence, byte unit, ushort address, ushort length )
		{
			if (!Authorization.asdniasnfaksndiqwhawfskhfaiw( )) return new OperateResult<byte[]>( StringResources.Language.InsufficientPrivileges );

			var read = keyence.ReadFromCoreServer( Encoding.ASCII.GetBytes( $"URD {unit} {address}.U {length}\r" ) );
			if (!read.IsSuccess) return read.ConvertFailed<byte[]>( );

			var check = CheckPlcReadResponse( read.Content );
			if (!check.IsSuccess) return check.ConvertFailed<byte[]>( );

			return ExtractActualData( "DM", read.Content ); // 按照DM来解析，因为上面读取的命令是.U格式的，而DM默认的也是.U
		}

#if !NET20 && !NET35
		/// <inheritdoc cref="ReadExpansionMemory(IReadWriteDevice, byte, ushort, ushort)"/>
		public static async Task<OperateResult<byte[]>> ReadExpansionMemoryAsync( IReadWriteDevice keyence, byte unit, ushort address, ushort length )
		{
			if (!Authorization.asdniasnfaksndiqwhawfskhfaiw( )) return new OperateResult<byte[]>( StringResources.Language.InsufficientPrivileges );

			var read = await keyence.ReadFromCoreServerAsync( Encoding.ASCII.GetBytes( $"URD {unit} {address}.U {length}\r" ) );
			if (!read.IsSuccess) return read.ConvertFailed<byte[]>( );

			var check = CheckPlcReadResponse( read.Content );
			if (!check.IsSuccess) return check.ConvertFailed<byte[]>( );

			return ExtractActualData( "DM", read.Content ); // 按照DM来解析，因为上面读取的命令是.U格式的，而DM默认的也是.U
		}
#endif
		/// <summary>
		///<c>[商业授权]</c> 将原始字节数据写入到扩展的缓冲存储器，需要指定单元编号，偏移地址，写入的数据<br />
		/// <b>[Authorization]</b> To write the original byte data to the extended buffer memory, you need to specify the unit number, offset address, and write data
		/// </summary>
		/// <param name="keyence">PLC通信对象信息</param>
		/// <param name="unit">单元编号</param>
		/// <param name="address">偏移地址</param>
		/// <param name="value">等待写入的原始字节数据</param>
		/// <returns>是否写入成功的结果对象</returns>
		public static OperateResult WriteExpansionMemory( IReadWriteDevice keyence, byte unit, ushort address, byte[] value )
		{
			if (!Authorization.asdniasnfaksndiqwhawfskhfaiw( )) return new OperateResult<byte[]>( StringResources.Language.InsufficientPrivileges );

			var read = keyence.ReadFromCoreServer( BuildWriteExpansionMemoryCommand( unit, address, value ).Content );
			if (!read.IsSuccess) return read.ConvertFailed<byte[]>( );

			return CheckPlcWriteResponse( read.Content );
		}
#if !NET20 && !NET35
		/// <inheritdoc cref="WriteExpansionMemory(IReadWriteDevice, byte, ushort, byte[])"/>
		public static async Task<OperateResult> WriteExpansionMemoryAsync( IReadWriteDevice keyence, byte unit, ushort address, byte[] value )
		{
			if (!Authorization.asdniasnfaksndiqwhawfskhfaiw( )) return new OperateResult<byte[]>( StringResources.Language.InsufficientPrivileges );

			var read = await keyence.ReadFromCoreServerAsync( BuildWriteExpansionMemoryCommand( unit, address, value ).Content );
			if (!read.IsSuccess) return read.ConvertFailed<byte[]>( );

			return CheckPlcWriteResponse( read.Content );
		}
#endif
	}
}
