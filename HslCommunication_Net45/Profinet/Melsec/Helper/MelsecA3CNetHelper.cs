using HslCommunication.BasicFramework;
using HslCommunication.Core.Address;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HslCommunication.Core;
using System.IO;
#if !NET20 && !NET35
using System.Threading.Tasks;
#endif

namespace HslCommunication.Profinet.Melsec.Helper
{
	/// <summary>
	/// MelsecA3CNet1协议通信的辅助类
	/// </summary>
	public class MelsecA3CNetHelper
	{
		/// <summary>
		/// 将命令进行打包传送，可选站号及是否和校验机制
		/// </summary>
		/// <param name="plc">PLC设备通信对象</param>
		/// <param name="mcCommand">mc协议的命令</param>
		/// <param name="station">PLC的站号</param>
		/// <returns>最终的原始报文信息</returns>
		public static byte[] PackCommand( IReadWriteA3C plc, byte[] mcCommand, byte station = 0 )
		{
			MemoryStream ms = new MemoryStream( );
			if (plc.Format != 3)
				ms.WriteByte( 0x05 );                                           // STX
			else
				ms.WriteByte( 0x02 );
			if (plc.Format == 2)
			{
				ms.WriteByte( 0x30 );                                       // 格式二的块编号
				ms.WriteByte( 0x30 );
			}
			ms.WriteByte( 0x46 );                                           // 帧识别F9
			ms.WriteByte( 0x39 );
			ms.WriteByte( SoftBasic.BuildAsciiBytesFrom( station )[0] );    // 站号
			ms.WriteByte( SoftBasic.BuildAsciiBytesFrom( station )[1] );
			ms.WriteByte( 0x30 );                                           // 网络编号
			ms.WriteByte( 0x30 );
			ms.WriteByte( 0x46 );                                           // 可编程控制器编号
			ms.WriteByte( 0x46 );
			ms.WriteByte( 0x30 );                                           // 本站编号
			ms.WriteByte( 0x30 );
			ms.Write( mcCommand, 0, mcCommand.Length );
			if (plc.Format == 3)
			{
				ms.WriteByte( 0x03 );
			}
			// 计算和校验
			if (plc.SumCheck)
			{
				byte[] cmd = ms.ToArray( );
				int sum = 0;
				for (int i = 1; i < cmd.Length; i++)
				{
					sum += cmd[i];
				}
				ms.WriteByte( SoftBasic.BuildAsciiBytesFrom( (byte)sum )[0] );
				ms.WriteByte( SoftBasic.BuildAsciiBytesFrom( (byte)sum )[1] );
			}
			if (plc.Format == 4)
			{
				ms.WriteByte( 0x0D );    // CR
				ms.WriteByte( 0x0A );    // LF
			}
			byte[] buffer = ms.ToArray( );
			ms.Dispose( );
			return buffer;
		}

		private static int GetErrorCodeOrDataStartIndex( IReadWriteA3C plc )
		{
			int start = 11;
			switch (plc.Format)
			{
				case 1: start = 11; break;
				case 2: start = 13; break;
				case 3: start = 15; break;
				case 4: start = 11; break;
			}
			return start;
		}

		/// <summary>
		/// 根据PLC返回的数据信息，获取到实际的数据内容
		/// </summary>
		/// <param name="plc">PLC设备通信对象</param>
		/// <param name="response">PLC返回的数据信息</param>
		/// <returns>带有是否成功的读取结果对象内容</returns>
		public static OperateResult<byte[]> ExtraReadActualResponse( IReadWriteA3C plc, byte[] response )
		{
			try
			{
				int start = GetErrorCodeOrDataStartIndex( plc );
				// 结果验证
				if (plc.Format == 1 || plc.Format == 2 || plc.Format == 4)
				{
					if (response[0] == 0x15)
					{
						int errorCode = Convert.ToInt32( Encoding.ASCII.GetString( response, start, 4 ), 16 );
						return new OperateResult<byte[]>( errorCode, MelsecHelper.GetErrorDescription( errorCode ) );
					}
					if (response[0] != 0x02)
						return new OperateResult<byte[]>( response[0], "Read Faild:" + SoftBasic.GetAsciiStringRender( response ) );
				}
				else if (plc.Format == 3)
				{
					string ending = Encoding.ASCII.GetString( response, 11, 4 );
					if (ending == "QNAK")
					{
						int errorCode = Convert.ToInt32( Encoding.ASCII.GetString( response, start, 4 ), 16 );
						return new OperateResult<byte[]>( errorCode, MelsecHelper.GetErrorDescription( errorCode ) );
					}
					if (ending != "QACK")
						return new OperateResult<byte[]>( response[0], "Read Faild:" + SoftBasic.GetAsciiStringRender( response ) );
				}

				int end = -1;
				for (int i = start; i < response.Length; i++)
				{
					if (response[i] == 0x03)
					{
						end = i;
						break;
					}
				}
				if (end == -1) end = response.Length;
				return OperateResult.CreateSuccessResult( response.SelectMiddle( start, end - start ) );
			}
			catch(Exception ex)
			{
				return new OperateResult<byte[]>( "ExtraReadActualResponse Wrong:" + ex.Message + Environment.NewLine +
					"Source: " + response.ToHexString( ' ' ) );
			}
		}

		private static OperateResult CheckWriteResponse( IReadWriteA3C plc, byte[] response )
		{
			int start = GetErrorCodeOrDataStartIndex( plc );

			// 结果验证
			if (plc.Format == 1 || plc.Format == 2 )
			{
				if (response[0] == 0x15)
				{
					int errorCode = Convert.ToInt32( Encoding.ASCII.GetString( response, start, 4 ), 16 );
					return new OperateResult<byte[]>( errorCode, MelsecHelper.GetErrorDescription( errorCode ) );
				}
				if (response[0] != 0x06)
					return new OperateResult<byte[]>( response[0], "Write Faild:" + SoftBasic.GetAsciiStringRender( response ) );
			}
			else if (plc.Format == 3)
			{
				if (response[0] != 0x02)
					return new OperateResult<byte[]>( response[0], "Write Faild:" + SoftBasic.GetAsciiStringRender( response ) );
				string ending = Encoding.ASCII.GetString( response, 11, 4 );
				if (ending == "QNAK")
				{
					int errorCode = Convert.ToInt32( Encoding.ASCII.GetString( response, start, 4 ), 16 );
					return new OperateResult<byte[]>( errorCode, MelsecHelper.GetErrorDescription( errorCode ) );
				}
				if (ending != "QACK")
					return new OperateResult<byte[]>( response[0], "Write Faild:" + SoftBasic.GetAsciiStringRender( response ) );
			}
			else if (plc.Format == 4)
			{
				if (response[0] == 0x15)
				{
					int errorCode = Convert.ToInt32( Encoding.ASCII.GetString( response, start, 4 ), 16 );
					return new OperateResult<byte[]>( errorCode, MelsecHelper.GetErrorDescription( errorCode ) );
				}
				if (response[0] != 0x02)
					return new OperateResult<byte[]>( response[0], "Write Faild:" + SoftBasic.GetAsciiStringRender( response ) );
			}
			return OperateResult.CreateSuccessResult( );
		}

		#region Static Helper

		/// <summary>
		/// 批量读取PLC的数据，以字为单位，支持读取X,Y,M,S,D,T,C，具体的地址范围需要根据PLC型号来确认
		/// </summary>
		/// <param name="plc">PLC设备通信对象</param>
		/// <param name="address">地址信息</param>
		/// <param name="length">数据长度</param>
		/// <returns>读取结果信息</returns>
		public static OperateResult<byte[]> Read( IReadWriteA3C plc, string address, ushort length )
		{
			byte stat = (byte)HslHelper.ExtractParameter( ref address, "s", plc.Station );
			// 分析地址
			OperateResult<McAddressData> addressResult = McAddressData.ParseMelsecFrom( address, length );
			if (!addressResult.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( addressResult );

			// 解析指令
			byte[] command = McAsciiHelper.BuildAsciiReadMcCoreCommand( addressResult.Content, false );

			// 核心交互
			OperateResult<byte[]> read = plc.ReadFromCoreServer( PackCommand( plc, command, stat ) );
			if (!read.IsSuccess) return read;

			// 结果验证
			OperateResult<byte[]> check = ExtraReadActualResponse( plc, read.Content );
			if (!check.IsSuccess) return check;

			// 提取结果
			return OperateResult.CreateSuccessResult( MelsecHelper.TransAsciiByteArrayToByteArray( check.Content ) );
		}

#if !NET20 && !NET35
		/// <inheritdoc cref="Read(IReadWriteA3C, string, ushort)"/>
		public static async Task<OperateResult<byte[]>> ReadAsync( IReadWriteA3C plc, string address, ushort length )
		{
			byte stat = (byte)HslHelper.ExtractParameter( ref address, "s", plc.Station );
			// 分析地址
			OperateResult<McAddressData> addressResult = McAddressData.ParseMelsecFrom( address, length );
			if (!addressResult.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( addressResult );

			// 解析指令
			byte[] command = McAsciiHelper.BuildAsciiReadMcCoreCommand( addressResult.Content, false );

			// 核心交互
			OperateResult<byte[]> read = await plc.ReadFromCoreServerAsync( PackCommand( plc, command, stat ) );
			if (!read.IsSuccess) return read;

			// 结果验证
			OperateResult<byte[]> check = ExtraReadActualResponse( plc, read.Content );
			if (!check.IsSuccess) return check;

			// 提取结果
			return OperateResult.CreateSuccessResult( MelsecHelper.TransAsciiByteArrayToByteArray( check.Content ) );
		}
#endif

		/// <summary>
		/// 批量写入PLC的数据，以字为单位，也就是说最少2个字节信息，支持X,Y,M,S,D,T,C，具体的地址范围需要根据PLC型号来确认
		/// </summary>
		/// <param name="plc">PLC设备通信对象</param>
		/// <param name="address">地址信息</param>
		/// <param name="value">数据值</param>
		/// <returns>是否写入成功</returns>
		public static OperateResult Write( IReadWriteA3C plc, string address, byte[] value )
		{
			byte stat = (byte)HslHelper.ExtractParameter( ref address, "s", plc.Station );
			// 分析地址
			OperateResult<McAddressData> addressResult = McAddressData.ParseMelsecFrom( address, 0 );
			if (!addressResult.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( addressResult );

			// 解析指令
			byte[] command = McAsciiHelper.BuildAsciiWriteWordCoreCommand( addressResult.Content, value );

			// 核心交互
			OperateResult<byte[]> read = plc.ReadFromCoreServer( PackCommand( plc, command, stat ) );
			if (!read.IsSuccess) return read;

			// 结果验证
			return CheckWriteResponse( plc, read.Content );
		}

#if !NET20 && !NET35
		/// <inheritdoc cref="Write(IReadWriteA3C, string, byte[])"/>
		public static async Task<OperateResult> WriteAsync( IReadWriteA3C plc, string address, byte[] value )
		{
			byte stat = (byte)HslHelper.ExtractParameter( ref address, "s", plc.Station );
			// 分析地址
			OperateResult<McAddressData> addressResult = McAddressData.ParseMelsecFrom( address, 0 );
			if (!addressResult.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( addressResult );

			// 解析指令
			byte[] command = McAsciiHelper.BuildAsciiWriteWordCoreCommand( addressResult.Content, value );

			// 核心交互
			OperateResult<byte[]> read = await plc.ReadFromCoreServerAsync( PackCommand( plc, command, stat ) );
			if (!read.IsSuccess) return read;

			// 结果验证
			return CheckWriteResponse( plc, read.Content );
		}
#endif
		/// <summary>
		/// 批量读取bool类型数据，支持的类型为X,Y,S,T,C，具体的地址范围取决于PLC的类型
		/// </summary>
		/// <param name="plc">PLC设备通信对象</param>
		/// <param name="address">地址信息，比如X10,Y17，注意X，Y的地址是8进制的</param>
		/// <param name="length">读取的长度</param>
		/// <returns>读取结果信息</returns>
		public static OperateResult<bool[]> ReadBool( IReadWriteA3C plc, string address, ushort length )
		{
			byte stat = (byte)HslHelper.ExtractParameter( ref address, "s", plc.Station );
			// 分析地址
			OperateResult<McAddressData> addressResult = McAddressData.ParseMelsecFrom( address, length );
			if (!addressResult.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( addressResult );

			// 解析指令
			byte[] command = McAsciiHelper.BuildAsciiReadMcCoreCommand( addressResult.Content, true );

			// 核心交互
			OperateResult<byte[]> read = plc.ReadFromCoreServer( PackCommand( plc, command, stat ) );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( read );

			// 结果验证
			OperateResult<byte[]> check = ExtraReadActualResponse( plc, read.Content );
			if (!check.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( check );

			// 提取结果
			return OperateResult.CreateSuccessResult( check.Content.Select( m => m == 0x31 ).ToArray( ) );
		}

#if !NET20 && !NET35
		/// <inheritdoc cref="ReadBool(IReadWriteA3C, string, ushort)"/>
		public static async Task<OperateResult<bool[]>> ReadBoolAsync( IReadWriteA3C plc, string address, ushort length )
		{
			byte stat = (byte)HslHelper.ExtractParameter( ref address, "s", plc.Station );
			// 分析地址
			OperateResult<McAddressData> addressResult = McAddressData.ParseMelsecFrom( address, length );
			if (!addressResult.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( addressResult );

			// 解析指令
			byte[] command = McAsciiHelper.BuildAsciiReadMcCoreCommand( addressResult.Content, true );

			// 核心交互
			OperateResult<byte[]> read = await plc.ReadFromCoreServerAsync( PackCommand( plc, command, stat ) );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( read );

			// 结果验证
			OperateResult<byte[]> check = ExtraReadActualResponse( plc, read.Content );
			if (!check.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( check );

			// 提取结果
			return OperateResult.CreateSuccessResult( check.Content.Select( m => m == 0x31 ).ToArray( ) );
		}
#endif
		/// <summary>
		/// 批量写入bool类型的数组，支持的类型为X,Y,S,T,C，具体的地址范围取决于PLC的类型
		/// </summary>
		/// <param name="plc">PLC设备通信对象</param>
		/// <param name="address">PLC的地址信息</param>
		/// <param name="value">数据信息</param>
		/// <returns>是否写入成功</returns>
		public static OperateResult Write( IReadWriteA3C plc, string address, bool[] value )
		{
			byte stat = (byte)HslHelper.ExtractParameter( ref address, "s", plc.Station );

			// 分析地址
			OperateResult<McAddressData> addressResult = McAddressData.ParseMelsecFrom( address, 0 );
			if (!addressResult.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( addressResult );

			// 解析指令
			byte[] command = McAsciiHelper.BuildAsciiWriteBitCoreCommand( addressResult.Content, value );

			// 核心交互
			OperateResult<byte[]> read = plc.ReadFromCoreServer( PackCommand( plc, command, stat ) );
			if (!read.IsSuccess) return read;

			// 结果验证
			return CheckWriteResponse( plc, read.Content );
		}

#if !NET20 && !NET35
		/// <inheritdoc cref="Write(IReadWriteA3C, string, bool[])"/>
		public static async Task<OperateResult> WriteAsync( IReadWriteA3C plc, string address, bool[] value )
		{
			byte stat = (byte)HslHelper.ExtractParameter( ref address, "s", plc.Station );

			// 分析地址
			OperateResult<McAddressData> addressResult = McAddressData.ParseMelsecFrom( address, 0 );
			if (!addressResult.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( addressResult );

			// 解析指令
			byte[] command = McAsciiHelper.BuildAsciiWriteBitCoreCommand( addressResult.Content, value );

			// 核心交互
			OperateResult<byte[]> read = await plc.ReadFromCoreServerAsync( PackCommand( plc, command, stat ) );
			if (!read.IsSuccess) return read;

			// 结果验证
			return CheckWriteResponse( plc, read.Content );
		}
#endif
	/// <summary>
	/// 远程Run操作
	/// </summary>
	/// <param name="plc">PLC设备通信对象</param>
	/// <returns>是否成功</returns>
	public static OperateResult RemoteRun( IReadWriteA3C plc )
		{
			// 核心交互
			OperateResult<byte[]> read = plc.ReadFromCoreServer( PackCommand( plc, Encoding.ASCII.GetBytes( "1001000000010000" ), plc.Station ) );
			if (!read.IsSuccess) return read;

			// 结果验证
			return CheckWriteResponse( plc, read.Content );
		}

		/// <summary>
		/// 远程Stop操作
		/// </summary>
		/// <param name="plc">PLC设备通信对象</param>
		/// <returns>是否成功</returns>
		public static OperateResult RemoteStop( IReadWriteA3C plc )
		{
			// 核心交互
			OperateResult<byte[]> read = plc.ReadFromCoreServer( PackCommand( plc, Encoding.ASCII.GetBytes( "100200000001" ), plc.Station ) );
			if (!read.IsSuccess) return read;

			// 结果验证
			return CheckWriteResponse( plc, read.Content );
		}

		/// <summary>
		/// 读取PLC的型号信息
		/// </summary>
		/// <param name="plc">PLC设备通信对象</param>
		/// <returns>返回型号的结果对象</returns>
		public static OperateResult<string> ReadPlcType( IReadWriteA3C plc )
		{
			// 核心交互
			OperateResult<byte[]> read = plc.ReadFromCoreServer( PackCommand( plc, Encoding.ASCII.GetBytes( "01010000" ), plc.Station ) );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<string>( read );

			// 结果验证
			OperateResult<byte[]> check = ExtraReadActualResponse( plc, read.Content );
			if (!check.IsSuccess) return OperateResult.CreateFailedResult<string>( check );

			// 成功
			return OperateResult.CreateSuccessResult( Encoding.ASCII.GetString( check.Content, 0, 16 ).TrimEnd( ) );
		}

#if !NET20 && !NET35
		/// <inheritdoc cref="RemoteRun(IReadWriteA3C)"/>
		public static async Task<OperateResult> RemoteRunAsync( IReadWriteA3C plc )
		{
			// 核心交互
			OperateResult<byte[]> read = await plc.ReadFromCoreServerAsync( PackCommand( plc, Encoding.ASCII.GetBytes( "1001000000010000" ), plc.Station ) );
			if (!read.IsSuccess) return read;

			// 结果验证
			return CheckWriteResponse( plc, read.Content );
		}

		/// <inheritdoc cref="RemoteStop(IReadWriteA3C)"/>
		public static async Task<OperateResult> RemoteStopAsync( IReadWriteA3C plc )
		{
			// 核心交互
			OperateResult<byte[]> read = await plc.ReadFromCoreServerAsync( PackCommand( plc, Encoding.ASCII.GetBytes( "100200000001" ), plc.Station ) );
			if (!read.IsSuccess) return read;

			// 结果验证
			return CheckWriteResponse( plc, read.Content );
		}

		/// <inheritdoc cref="ReadPlcType(IReadWriteA3C)"/>
		public static async Task<OperateResult<string>> ReadPlcTypeAsync( IReadWriteA3C plc )
		{
			// 核心交互
			OperateResult<byte[]> read = await plc.ReadFromCoreServerAsync( PackCommand( plc, Encoding.ASCII.GetBytes( "01010000" ), plc.Station ) );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<string>( read );

			// 结果验证
			OperateResult<byte[]> check = ExtraReadActualResponse( plc, read.Content );
			if (!check.IsSuccess) return OperateResult.CreateFailedResult<string>( check );

			// 成功
			return OperateResult.CreateSuccessResult( Encoding.ASCII.GetString( check.Content, 0, 16 ).TrimEnd( ) );
		}
#endif
		#endregion
	}
}
