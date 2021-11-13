using HslCommunication.BasicFramework;
using HslCommunication.Core;
using HslCommunication.Core.Address;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
#if !NET35 && !NET20
using System.Threading.Tasks;
#endif

namespace HslCommunication.Profinet.Fuji
{
	/// <summary>
	/// 富士SPB的辅助类
	/// </summary>
	public class FujiSPBHelper
	{
		#region Static Helper

		/// <summary>
		/// 将int数据转换成SPB可识别的标准的数据内容，例如 2转换为0200 , 200转换为0002
		/// </summary>
		/// <param name="address">等待转换的数据内容</param>
		/// <returns>转换之后的数据内容</returns>
		public static string AnalysisIntegerAddress( int address )
		{
			string tmp = address.ToString( "D4" );
			return tmp.Substring( 2 ) + tmp.Substring( 0, 2 );
		}

		/// <summary>
		/// 计算指令的和校验码
		/// </summary>
		/// <param name="data">指令</param>
		/// <returns>校验之后的信息</returns>
		public static string CalculateAcc( string data )
		{
			byte[] buffer = Encoding.ASCII.GetBytes( data );

			int count = 0;
			for (int i = 0; i < buffer.Length; i++)
			{
				count += buffer[i];
			}

			return count.ToString( "X4" ).Substring( 2 );
		}

		/// <summary>
		/// 创建一条读取的指令信息，需要指定一些参数，单次读取最大105个字
		/// </summary>
		/// <param name="station">PLC的站号</param>
		/// <param name="address">地址信息</param>
		/// <param name="length">数据长度</param>
		/// <returns>是否成功的结果对象</returns>
		public static OperateResult<byte[]> BuildReadCommand( byte station, string address, ushort length )
		{
			station = (byte)HslHelper.ExtractParameter( ref address, "s", station );

			OperateResult<FujiSPBAddress> addressAnalysis = FujiSPBAddress.ParseFrom( address );
			if (!addressAnalysis.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( addressAnalysis );

			return BuildReadCommand( station, addressAnalysis.Content, length );
		}

		/// <summary>
		/// 创建一条读取的指令信息，需要指定一些参数，单次读取最大105个字
		/// </summary>
		/// <param name="station">PLC的站号</param>
		/// <param name="address">地址信息</param>
		/// <param name="length">数据长度</param>
		/// <returns>是否成功的结果对象</returns>
		public static OperateResult<byte[]> BuildReadCommand( byte station, FujiSPBAddress address, ushort length )
		{
			StringBuilder stringBuilder = new StringBuilder( );
			stringBuilder.Append( ':' );
			stringBuilder.Append( station.ToString( "X2" ) );
			stringBuilder.Append( "09" );
			stringBuilder.Append( "FFFF" );
			stringBuilder.Append( "00" );
			stringBuilder.Append( "00" );
			stringBuilder.Append( address.GetWordAddress( ) );
			stringBuilder.Append( AnalysisIntegerAddress( length ) );
			stringBuilder.Append( "\r\n" );
			return OperateResult.CreateSuccessResult( Encoding.ASCII.GetBytes( stringBuilder.ToString( ) ) );
		}

		/// <summary>
		/// 创建一条读取多个地址的指令信息，需要指定一些参数，单次读取最大105个字
		/// </summary>
		/// <param name="station">PLC的站号</param>
		/// <param name="address">地址信息</param>
		/// <param name="length">数据长度</param>
		/// <param name="isBool">是否位读取</param>
		/// <returns>是否成功的结果对象</returns>
		public static OperateResult<byte[]> BuildReadCommand( byte station, string[] address, ushort[] length, bool isBool )
		{
			if (address == null || length == null) return new OperateResult<byte[]>( "Parameter address or length can't be null" );
			if (address.Length != length.Length) return new OperateResult<byte[]>( StringResources.Language.TwoParametersLengthIsNotSame );

			StringBuilder stringBuilder = new StringBuilder( );
			stringBuilder.Append( ':' );
			stringBuilder.Append( station.ToString( "X2" ) );
			stringBuilder.Append( (6 + address.Length * 4).ToString( "X2" ) );
			stringBuilder.Append( "FFFF" );
			stringBuilder.Append( "00" );
			stringBuilder.Append( "04" );
			stringBuilder.Append( "00" );
			stringBuilder.Append( address.Length.ToString( "X2" ) );
			for (int i = 0; i < address.Length; i++)
			{
				station = (byte)HslHelper.ExtractParameter( ref address[i], "s", station );

				OperateResult<FujiSPBAddress> addressAnalysis = FujiSPBAddress.ParseFrom( address[i] );
				if (!addressAnalysis.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( addressAnalysis );

				stringBuilder.Append( addressAnalysis.Content.TypeCode );
				stringBuilder.Append( length[i].ToString( "X2" ) );
				stringBuilder.Append( AnalysisIntegerAddress( addressAnalysis.Content.AddressStart ) );
			}
			stringBuilder[1] = station.ToString( "X2" )[0];
			stringBuilder[2] = station.ToString( "X2" )[1];
			stringBuilder.Append( "\r\n" );
			return OperateResult.CreateSuccessResult( Encoding.ASCII.GetBytes( stringBuilder.ToString( ) ) );
		}

		/// <summary>
		/// 创建一条别入byte数据的指令信息，需要指定一些参数，按照字单位，单次写入最大103个字
		/// </summary>
		/// <param name="station">站号</param>
		/// <param name="address">地址</param>
		/// <param name="value">数组值</param>
		/// <returns>是否创建成功</returns>
		public static OperateResult<byte[]> BuildWriteByteCommand( byte station, string address, byte[] value )
		{
			station = (byte)HslHelper.ExtractParameter( ref address, "s", station );

			OperateResult<FujiSPBAddress> addressAnalysis = FujiSPBAddress.ParseFrom( address );
			if (!addressAnalysis.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( addressAnalysis );

			StringBuilder stringBuilder = new StringBuilder( );
			stringBuilder.Append( ':' );
			stringBuilder.Append( station.ToString( "X2" ) );
			stringBuilder.Append( "00" );
			stringBuilder.Append( "FFFF" );
			stringBuilder.Append( "01" );
			stringBuilder.Append( "00" );
			stringBuilder.Append( addressAnalysis.Content.GetWordAddress( ) );
			stringBuilder.Append( AnalysisIntegerAddress( value.Length / 2 ) );
			stringBuilder.Append( value.ToHexString( ) );

			stringBuilder[3] = ((stringBuilder.Length - 5) / 2).ToString( "X2" )[0];
			stringBuilder[4] = ((stringBuilder.Length - 5) / 2).ToString( "X2" )[1];
			stringBuilder.Append( "\r\n" );
			return OperateResult.CreateSuccessResult( Encoding.ASCII.GetBytes( stringBuilder.ToString( ) ) );
		}

		/// <summary>
		/// 创建一条别入byte数据的指令信息，需要指定一些参数，按照字单位，单次写入最大103个字
		/// </summary>
		/// <param name="station">站号</param>
		/// <param name="address">地址</param>
		/// <param name="value">数组值</param>
		/// <returns>是否创建成功</returns>
		public static OperateResult<byte[]> BuildWriteBoolCommand( byte station, string address, bool value )
		{
			station = (byte)HslHelper.ExtractParameter( ref address, "s", station );

			OperateResult<FujiSPBAddress> addressAnalysis = FujiSPBAddress.ParseFrom( address );
			if (!addressAnalysis.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( addressAnalysis );

			if (address.StartsWith( "X" ) ||
				address.StartsWith( "Y" ) ||
				address.StartsWith( "M" ) ||
				address.StartsWith( "L" ) ||
				address.StartsWith( "TC" ) ||
				address.StartsWith( "CC" ))
			{
				if (address.IndexOf( '.' ) < 0)
				{
					// 当是M1000这种地址的时候，需要进行转换一下字地址
					addressAnalysis.Content.BitIndex = addressAnalysis.Content.AddressStart % 16;
					addressAnalysis.Content.AddressStart = (ushort)(addressAnalysis.Content.AddressStart / 16);
				}
			}

			StringBuilder stringBuilder = new StringBuilder( );
			stringBuilder.Append( ':' );
			stringBuilder.Append( station.ToString( "X2" ) );
			stringBuilder.Append( "00" );
			stringBuilder.Append( "FFFF" );
			stringBuilder.Append( "01" );
			stringBuilder.Append( "02" );
			stringBuilder.Append( addressAnalysis.Content.GetWriteBoolAddress( ) );
			stringBuilder.Append( value ? "01" : "00" );

			stringBuilder[3] = ((stringBuilder.Length - 5) / 2).ToString( "X2" )[0];
			stringBuilder[4] = ((stringBuilder.Length - 5) / 2).ToString( "X2" )[1];
			stringBuilder.Append( "\r\n" );
			return OperateResult.CreateSuccessResult( Encoding.ASCII.GetBytes( stringBuilder.ToString( ) ) );
		}

		/// <summary>
		/// 检查反馈的数据信息，是否包含了错误码，如果没有包含，则返回成功
		/// </summary>
		/// <param name="content">原始的报文返回</param>
		/// <returns>是否成功的结果对象</returns>
		public static OperateResult<byte[]> CheckResponseData( byte[] content )
		{
			if (content[0] != ':') return new OperateResult<byte[]>( content[0], "Read Faild:" + SoftBasic.ByteToHexString( content, ' ' ) );
			string code = Encoding.ASCII.GetString( content, 9, 2 );

			if (code != "00") return new OperateResult<byte[]>( Convert.ToInt32( code, 16 ), GetErrorDescriptionFromCode( code ) );
			if (content[content.Length - 2] == 0x0D && content[content.Length - 1] == 0x0A) content = content.RemoveLast( 2 );

			return OperateResult.CreateSuccessResult( content.RemoveBegin( 11 ) );
		}

		/// <summary>
		/// 根据错误码获取到真实的文本信息
		/// </summary>
		/// <param name="code">错误码</param>
		/// <returns>错误的文本描述</returns>
		public static string GetErrorDescriptionFromCode( string code )
		{
			switch (code)
			{
				case "01": return StringResources.Language.FujiSpbStatus01;
				case "02": return StringResources.Language.FujiSpbStatus02;
				case "03": return StringResources.Language.FujiSpbStatus03;
				case "04": return StringResources.Language.FujiSpbStatus04;
				case "05": return StringResources.Language.FujiSpbStatus05;
				case "06": return StringResources.Language.FujiSpbStatus06;
				case "07": return StringResources.Language.FujiSpbStatus07;
				case "09": return StringResources.Language.FujiSpbStatus09;
				case "0C": return StringResources.Language.FujiSpbStatus0C;
				default: return StringResources.Language.UnknownError;
			}
		}

		#endregion

		/// <summary>
		/// 批量读取PLC的数据，以字为单位，支持读取X,Y,L,M,D,TN,CN,TC,CC,R,W具体的地址范围需要根据PLC型号来确认，地址可以携带站号信息，例如：s=2;D100<br />
		/// Read PLC data in batches, in units of words. Supports reading X, Y, L, M, D, TN, CN, TC, CC, R, W. 
		/// The specific address range needs to be confirmed according to the PLC model, The address can carry station number information, for example: s=2;D100
		/// </summary>
		/// <param name="device">PLC设备通信对象</param>
		/// <param name="station">当前的站号信息</param>
		/// <param name="address">地址信息</param>
		/// <param name="length">数据长度</param>
		/// <returns>读取结果信息</returns>
		/// <remarks>
		/// 单次读取的最大的字数为105，如果读取的字数超过这个值，请分批次读取。
		/// </remarks>
		public static OperateResult<byte[]> Read( IReadWriteDevice device, byte station, string address, ushort length )
		{
			// 解析指令
			OperateResult<byte[]> command = BuildReadCommand( station, address, length );
			if (!command.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( command );

			// 核心交互
			OperateResult<byte[]> read = device.ReadFromCoreServer( command.Content );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( read );

			// 结果验证
			OperateResult<byte[]> check = CheckResponseData( read.Content );
			if (!check.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( check );

			// 提取结果
			return OperateResult.CreateSuccessResult( Encoding.ASCII.GetString( check.Content.RemoveBegin( 4 ) ).ToHexBytes( ) );
		}

		/// <summary>
		/// 批量写入PLC的数据，以字为单位，也就是说最少2个字节信息，支持读取X,Y,L,M,D,TN,CN,TC,CC,R具体的地址范围需要根据PLC型号来确认，地址可以携带站号信息，例如：s=2;D100<br />
		/// The data written to the PLC in batches, in units of words, that is, a minimum of 2 bytes of information. It supports reading X, Y, L, M, D, TN, CN, TC, CC, and R. 
		/// The specific address range needs to be based on PLC model to confirm, The address can carry station number information, for example: s=2;D100
		/// </summary>
		/// <param name="device">PLC设备通信对象</param>
		/// <param name="station">当前的站号信息</param>
		/// <param name="address">地址信息，举例，D100，R200，TN100，CN200</param>
		/// <param name="value">数据值</param>
		/// <returns>是否写入成功</returns>
		/// <remarks>
		/// 单次写入的最大的字数为103个字，如果写入的数据超过这个长度，请分批次写入
		/// </remarks>
		public static OperateResult Write( IReadWriteDevice device, byte station, string address, byte[] value )
		{
			// 解析指令
			OperateResult<byte[]> command = BuildWriteByteCommand( station, address, value );
			if (!command.IsSuccess) return command;

			// 核心交互
			OperateResult<byte[]> read = device.ReadFromCoreServer( command.Content );
			if (!read.IsSuccess) return read;

			// 结果验证
			return CheckResponseData( read.Content );
		}

		/// <summary>
		/// 批量读取PLC的Bool数据，以位为单位，支持读取X,Y,L,M,D,TN,CN,TC,CC,R,W，例如 M100, 如果是寄存器地址，可以使用D10.12来访问第10个字的12位，地址可以携带站号信息，例如：s=2;M100<br />
		/// Read PLC's Bool data in batches, in units of bits, support reading X, Y, L, M, D, TN, CN, TC, CC, R, W, such as M100, if it is a register address, 
		/// you can use D10. 12 to access the 12 bits of the 10th word, the address can carry station number information, for example: s=2;M100
		/// </summary>
		/// <param name="device">PLC设备通信对象</param>
		/// <param name="station">当前的站号信息</param>
		/// <param name="address">地址信息，举例：M100, D10.12</param>
		/// <param name="length">读取的bool长度信息</param>
		/// <returns>Bool[]的结果对象</returns>
		public static OperateResult<bool[]> ReadBool( IReadWriteDevice device, byte station, string address, ushort length )
		{
			byte stat = (byte)HslHelper.ExtractParameter( ref address, "s", station );

			OperateResult<FujiSPBAddress> addressAnalysis = FujiSPBAddress.ParseFrom( address );
			if (!addressAnalysis.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( addressAnalysis );

			if (address.StartsWith( "X" ) ||
				address.StartsWith( "Y" ) ||
				address.StartsWith( "M" ) ||
				address.StartsWith( "L" ) ||
				address.StartsWith( "TC" ) ||
				address.StartsWith( "CC" ))
			{
				if (address.IndexOf( '.' ) < 0)
				{
					// 当是M1000这种地址的时候，需要进行转换一下字地址
					addressAnalysis.Content.BitIndex = addressAnalysis.Content.AddressStart % 16;
					addressAnalysis.Content.AddressStart = (ushort)(addressAnalysis.Content.AddressStart / 16);
				}
			}

			ushort len = (ushort)((addressAnalysis.Content.GetBitIndex( ) + length - 1) / 16 - addressAnalysis.Content.GetBitIndex( ) / 16 + 1);
			// 解析指令
			OperateResult<byte[]> command = BuildReadCommand( stat, addressAnalysis.Content, len );
			if (!command.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( command );

			// 核心交互
			OperateResult<byte[]> read = device.ReadFromCoreServer( command.Content );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( read );

			// 结果验证
			OperateResult<byte[]> check = CheckResponseData( read.Content );
			if (!check.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( check );

			// 提取结果
			return OperateResult.CreateSuccessResult( Encoding.ASCII.GetString( check.Content.RemoveBegin( 4 ) ).ToHexBytes( ).ToBoolArray( ).SelectMiddle( addressAnalysis.Content.BitIndex, length ) );
		}

		/// <summary>
		/// 写入一个Bool值到一个地址里，地址可以是线圈地址，也可以是寄存器地址，例如：M100, D10.12，地址可以携带站号信息，例如：s=2;D10.12<br />
		/// Write a Bool value to an address. The address can be a coil address or a register address, for example: M100, D10.12. 
		/// The address can carry station number information, for example: s=2;D10.12
		/// </summary>
		/// <param name="device">PLC设备通信对象</param>
		/// <param name="station">当前的站号信息</param>
		/// <param name="address">地址信息，举例：M100, D10.12</param>
		/// <param name="value">写入的bool值</param>
		/// <returns>是否写入成功的结果对象</returns>
		public static OperateResult Write( IReadWriteDevice device, byte station, string address, bool value )
		{
			// 解析指令
			OperateResult<byte[]> command = BuildWriteBoolCommand( station, address, value );
			if (!command.IsSuccess) return command;

			// 核心交互
			OperateResult<byte[]> read = device.ReadFromCoreServer( command.Content );
			if (!read.IsSuccess) return read;

			// 结果验证
			return CheckResponseData( read.Content );
		}

		//public OperateResult<byte[]> ReadRandom( string[] address, ushort[] length )
		//{
		//	// 解析指令
		//	OperateResult<byte[]> command = BuildReadCommand( this.station, address, length, false );
		//	if (!command.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( command );

		//	// 核心交互
		//	OperateResult<byte[]> read = ReadFromCoreServer( command.Content );
		//	if (!read.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( read );

		//	// 结果验证
		//	if (read.Content[0] != ':') return new OperateResult<byte[]>( read.Content[0], "Read Faild:" + SoftBasic.ByteToHexString( read.Content, ' ' ) );
		//	if (Encoding.ASCII.GetString( read.Content, 9, 2 ) != "00") return new OperateResult<byte[]>( read.Content[5], GetErrorDescriptionFromCode( Encoding.ASCII.GetString( read.Content, 9, 2 ) ) );

		//	// 提取结果
		//	byte[] Content = new byte[length * 2];
		//	for (int i = 0; i < Content.Length / 2; i++)
		//	{
		//		ushort tmp = Convert.ToUInt16( Encoding.ASCII.GetString( read.Content, i * 4 + 6, 4 ), 16 );
		//		BitConverter.GetBytes( tmp ).CopyTo( Content, i * 2 );
		//	}
		//	return OperateResult.CreateSuccessResult( Content );
		//}


#if !NET35 && !NET20
		/// <inheritdoc cref="Read(IReadWriteDevice, byte, string, ushort)"/>
		public static async Task<OperateResult<byte[]>> ReadAsync( IReadWriteDevice device, byte station, string address, ushort length )
		{
			// 解析指令
			OperateResult<byte[]> command = BuildReadCommand( station, address, length );
			if (!command.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( command );

			// 核心交互
			OperateResult<byte[]> read = await device.ReadFromCoreServerAsync( command.Content );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( read );

			// 结果验证
			OperateResult<byte[]> check = CheckResponseData( read.Content );
			if (!check.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( check );

			// 提取结果
			return OperateResult.CreateSuccessResult( Encoding.ASCII.GetString( check.Content.RemoveBegin( 4 ) ).ToHexBytes( ) );
		}

		/// <inheritdoc cref="Write(IReadWriteDevice, byte, string, byte[])"/>
		public static async Task<OperateResult> WriteAsync( IReadWriteDevice device, byte station, string address, byte[] value )
		{
			// 解析指令
			OperateResult<byte[]> command = BuildWriteByteCommand( station, address, value );
			if (!command.IsSuccess) return command;

			// 核心交互
			OperateResult<byte[]> read = await device.ReadFromCoreServerAsync( command.Content );
			if (!read.IsSuccess) return read;

			// 结果验证
			return CheckResponseData( read.Content );
		}

		/// <inheritdoc cref="ReadBool(IReadWriteDevice, byte, string, ushort)"/>
		public static async Task<OperateResult<bool[]>> ReadBoolAsync( IReadWriteDevice device, byte station, string address, ushort length )
		{
			byte stat = (byte)HslHelper.ExtractParameter( ref address, "s", station );

			OperateResult<FujiSPBAddress> addressAnalysis = FujiSPBAddress.ParseFrom( address );
			if (!addressAnalysis.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( addressAnalysis );

			if (address.StartsWith( "X" ) ||
				address.StartsWith( "Y" ) ||
				address.StartsWith( "M" ) ||
				address.StartsWith( "L" ) ||
				address.StartsWith( "TC" ) ||
				address.StartsWith( "CC" ))
			{
				if (address.IndexOf( '.' ) < 0)
				{
					// 当是M1000这种地址的时候，需要进行转换一下字地址
					addressAnalysis.Content.BitIndex = addressAnalysis.Content.AddressStart % 16;
					addressAnalysis.Content.AddressStart = (ushort)(addressAnalysis.Content.AddressStart / 16);
				}
			}

			ushort len = (ushort)((addressAnalysis.Content.GetBitIndex( ) + length - 1) / 16 - addressAnalysis.Content.GetBitIndex( ) / 16 + 1);
			// 解析指令
			OperateResult<byte[]> command = BuildReadCommand( stat, addressAnalysis.Content, len );
			if (!command.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( command );

			// 核心交互
			OperateResult<byte[]> read = await device.ReadFromCoreServerAsync( command.Content );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( read );

			// 结果验证
			OperateResult<byte[]> check = CheckResponseData( read.Content );
			if (!check.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( check );

			// 提取结果
			return OperateResult.CreateSuccessResult( Encoding.ASCII.GetString( check.Content.RemoveBegin( 4 ) ).ToHexBytes( ).ToBoolArray( ).SelectMiddle( addressAnalysis.Content.BitIndex, length ) );
		}

		/// <inheritdoc cref="Write(IReadWriteDevice, byte, string, bool)"/>
		public static async Task<OperateResult> WriteAsync( IReadWriteDevice device, byte station, string address, bool value )
		{
			// 解析指令
			OperateResult<byte[]> command = BuildWriteBoolCommand( station, address, value );
			if (!command.IsSuccess) return command;

			// 核心交互
			OperateResult<byte[]> read = await device.ReadFromCoreServerAsync( command.Content );
			if (!read.IsSuccess) return read;

			// 结果验证
			return CheckResponseData( read.Content );
		}
#endif
	}
}
