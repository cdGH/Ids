using HslCommunication.BasicFramework;
using HslCommunication.Core.Address;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HslCommunication.Profinet.GE
{
	/// <summary>
	/// GE plc相关的辅助类对象
	/// </summary>
	public class GeHelper
	{
		/// <summary>
		/// 构建一个读取数据的报文信息，需要指定操作的数据代码，读取的参数信息<br />
		/// To construct a message information for reading data, you need to specify the data code of the operation and the parameter information to be read
		/// </summary>
		/// <param name="id">消息号</param>
		/// <param name="code">操作代码</param>
		/// <param name="data">数据参数</param>
		/// <returns>包含是否成功的报文信息</returns>
		public static OperateResult<byte[]> BuildReadCoreCommand( long id, byte code, byte[] data )
		{
			byte[] buffer = new byte[56];
			buffer[ 0] = 0x02;
			buffer[ 1] = 0x00;
			buffer[ 2] = BitConverter.GetBytes( id )[0];
			buffer[ 3] = BitConverter.GetBytes( id )[1];
			buffer[ 4] = 0x00;        // Length
			buffer[ 5] = 0x00;
			buffer[ 9] = 0x01;
			buffer[17] = 0x01;
			buffer[18] = 0x00;
			buffer[30] = 0x06;
			buffer[31] = 0xC0;
			buffer[36] = 0x10;
			buffer[37] = 0x0E;
			buffer[40] = 0x01;
			buffer[41] = 0x01;
			buffer[42] = code;        // read system memory
			data.CopyTo( buffer, 43 );
			return OperateResult.CreateSuccessResult( buffer );
		}

		/// <summary>
		/// 构建一个读取数据的报文命令，需要指定消息号，读取的 GE 地址信息<br />
		/// To construct a message command to read data, you need to specify the message number and read GE address information
		/// </summary>
		/// <param name="id">消息号</param>
		/// <param name="address">GE 的地址</param>
		/// <returns>包含是否成功的报文信息</returns>
		public static OperateResult<byte[]> BuildReadCommand( long id, GeSRTPAddress address )
		{
			if( address.DataCode == 0x0A ||
				address.DataCode == 0x0C ||
				address.DataCode == 0x08)
			{
				address.Length /= 2;
			}

			byte[] buffer = new byte[5];
			buffer[0] = address.DataCode;
			buffer[1] = BitConverter.GetBytes( address.AddressStart )[0];
			buffer[2] = BitConverter.GetBytes( address.AddressStart )[1];
			buffer[3] = BitConverter.GetBytes( address.Length )[0];
			buffer[4] = BitConverter.GetBytes( address.Length )[1];
			return BuildReadCoreCommand( id, 0x04, buffer );
		}

		/// <summary>
		/// 构建一个读取数据的报文命令，需要指定消息号，地址，长度，是否位读取，返回完整的报文信息。<br />
		/// To construct a message command to read data, you need to specify the message number, 
		/// address, length, whether to read in bits, and return the complete message information.
		/// </summary>
		/// <param name="id">消息号</param>
		/// <param name="address">地址</param>
		/// <param name="length">读取的长度</param>
		/// <param name="isBit"></param>
		/// <returns>包含是否成功的报文对象</returns>
		public static OperateResult<byte[]> BuildReadCommand( long id, string address, ushort length, bool isBit )
		{
			OperateResult<GeSRTPAddress> analysis = GeSRTPAddress.ParseFrom( address, length, isBit );
			if (!analysis.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( analysis );

			return BuildReadCommand( id, analysis.Content );
		}

		/// <inheritdoc cref="BuildWriteCommand(long, string, byte[])"/>
		public static OperateResult<byte[]> BuildWriteCommand( long id, GeSRTPAddress address, byte[] value )
		{
			int length = address.Length;
			if (address.DataCode == 0x0A ||
				address.DataCode == 0x0C ||
				address.DataCode == 0x08)
			{
				length /= 2;
			}

			byte[] buffer = new byte[56 + value.Length];
			buffer[ 0] = 0x02;
			buffer[ 1] = 0x00;
			buffer[ 2] = BitConverter.GetBytes( id )[0];
			buffer[ 3] = BitConverter.GetBytes( id )[1];
			buffer[ 4] = BitConverter.GetBytes( value.Length )[0];        // Length
			buffer[ 5] = BitConverter.GetBytes( value.Length )[1];
			buffer[ 9] = 0x02;
			buffer[17] = 0x02;
			buffer[18] = 0x00;
			buffer[30] = 0x09;
			buffer[31] = 0x80;
			buffer[36] = 0x10;
			buffer[37] = 0x0E;
			buffer[40] = 0x01;
			buffer[41] = 0x01;
			buffer[42] = 0x02;
			buffer[48] = 0x01;
			buffer[49] = 0x01;
			buffer[50] = 0x07;   // 写入数据
			buffer[51] = address.DataCode;
			buffer[52] = BitConverter.GetBytes( address.AddressStart )[0];
			buffer[53] = BitConverter.GetBytes( address.AddressStart )[1];
			buffer[54] = BitConverter.GetBytes( length )[0];
			buffer[55] = BitConverter.GetBytes( length )[1];
			value.CopyTo( buffer, 56 );
			return OperateResult.CreateSuccessResult( buffer );
		}

		/// <summary>
		/// 构建一个批量写入 byte 数组变量的报文，需要指定消息号，写入的地址，地址参照 <see cref="GeSRTPNet"/> 说明。<br />
		/// To construct a message to be written into byte array variables in batches, 
		/// you need to specify the message number and write address. For the address, refer to the description of <see cref="GeSRTPNet"/>.
		/// </summary>
		/// <param name="id">消息的序号</param>
		/// <param name="address">地址信息</param>
		/// <param name="value">byte数组的原始数据</param>
		/// <returns>包含结果信息的报文内容</returns>
		public static OperateResult<byte[]> BuildWriteCommand( long id, string address, byte[] value )
		{
			OperateResult<GeSRTPAddress> analysis = GeSRTPAddress.ParseFrom( address, (ushort)value.Length, false );
			if (!analysis.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( analysis );

			return BuildWriteCommand( id, analysis.Content, value );
		}

		/// <summary>
		/// 构建一个批量写入 bool 数组变量的报文，需要指定消息号，写入的地址，地址参照 <see cref="GeSRTPNet"/> 说明。<br />
		/// To construct a message to be written into bool array variables in batches, 
		/// you need to specify the message number and write address. For the address, refer to the description of <see cref="GeSRTPNet"/>.
		/// </summary>
		/// <param name="id">消息的序号</param>
		/// <param name="address">地址信息</param>
		/// <param name="value">bool数组</param>
		/// <returns>包含结果信息的报文内容</returns>
		public static OperateResult<byte[]> BuildWriteCommand( long id, string address, bool[] value )
		{
			OperateResult<GeSRTPAddress> analysis = GeSRTPAddress.ParseFrom( address, (ushort)value.Length, true );
			if (!analysis.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( analysis );

			bool[] boolArray = new bool[analysis.Content.AddressStart % 8 + value.Length];
			value.CopyTo( boolArray, analysis.Content.AddressStart % 8 );
			return BuildWriteCommand( id, analysis.Content, SoftBasic.BoolArrayToByte( boolArray ) );
		}

		/// <summary>
		/// 从PLC返回的数据中，提取出实际的数据内容，最少6个字节的数据。超出实际的数据长度的部分没有任何意义。<br />
		/// From the data returned by the PLC, extract the actual data content, at least 6 bytes of data. The part beyond the actual data length has no meaning.
		/// </summary>
		/// <param name="content">PLC返回的数据信息</param>
		/// <returns>解析后的实际数据内容</returns>
		public static OperateResult<byte[]> ExtraResponseContent( byte[] content )
		{
			try
			{
				if (content[ 0] != 0x03) return new OperateResult<byte[]>( content[0], StringResources.Language.UnknownError + " Source:" + content.ToHexString( ' ' ) );
				if (content[31] == 0xD4)
				{
					ushort status = BitConverter.ToUInt16( content, 42 );
					if (status != 0) return new OperateResult<byte[]>( status, StringResources.Language.UnknownError );
					return OperateResult.CreateSuccessResult( content.SelectMiddle( 44, 6 ) );
				}
				if (content[31] == 0x94) return OperateResult.CreateSuccessResult( content.RemoveBegin( 56 ) );
				return new OperateResult<byte[]>( "Extra Wrong:" + StringResources.Language.UnknownError + " Source:" + content.ToHexString( ' ' ) );
			}
			catch(Exception ex)
			{
				return new OperateResult<byte[]>( "Extra Wrong:" + ex.Message + " Source:" + content.ToHexString( ' ' ) );
			}
		}

		/// <summary>
		/// 从实际的时间的字节数组里解析出C#格式的时间对象，这个时间可能是时区0的时间，需要自行转化本地时间。<br />
		/// Analyze the time object in C# format from the actual time byte array. 
		/// This time may be the time in time zone 0, and you need to convert the local time yourself.
		/// </summary>
		/// <param name="content">字节数组</param>
		/// <returns>包含是否成功的结果对象</returns>
		public static OperateResult<DateTime> ExtraDateTime( byte[] content )
		{
			if (!Authorization.asdniasnfaksndiqwhawfskhfaiw( )) return new OperateResult<DateTime>( StringResources.Language.InsufficientPrivileges );

			try
			{
				return OperateResult.CreateSuccessResult( new DateTime(
					int.Parse( content[5].ToString( "X2" ) ) + 2000,
					int.Parse( content[4].ToString( "X2" ) ),
					int.Parse( content[3].ToString( "X2" ) ),
					int.Parse( content[2].ToString( "X2" ) ),
					int.Parse( content[1].ToString( "X2" ) ),
					int.Parse( content[0].ToString( "X2" ) ) ) );
			}
			catch(Exception ex)
			{
				return new OperateResult<DateTime>( ex.Message + " Source:" + content.ToHexString( ' ' ) );
			}
		}


		//0000   03 00 07 00 2a 00 00 00 00 00 00 00 00 00 00 00 
		//0010   00 01 00 00 00 00 00 00 00 00 00 00 00 00 06 94 
		//0020   00 0e 00 00 00 62 01 a0 00 00 2a 00 00 18 00 00 
		//0030   01 01 ff 02 03 00 5c 01 00 00 00 00 00 00 00 00 
		//0040   01 00 00 00 00 00 00 00 00 00 50 41 43 34 30 30 
		//0050   00 00 00 00 00 00 00 00 00 00 03 00 01 50 05 18 
		//0060   01 21

		/// <summary>
		/// 从实际的时间的字节数组里解析出PLC的程序的名称。<br />
		/// Parse the name of the PLC program from the actual time byte array
		/// </summary>
		/// <param name="content">字节数组</param>
		/// <returns>包含是否成功的结果对象</returns>
		public static OperateResult<string> ExtraProgramName( byte[] content )
		{
			if (!Authorization.asdniasnfaksndiqwhawfskhfaiw( )) return new OperateResult<string>( StringResources.Language.InsufficientPrivileges );

			try
			{
				return OperateResult.CreateSuccessResult( Encoding.UTF8.GetString( content, 18, 16 ).Trim( '\0' ) );
			}
			catch (Exception ex)
			{
				return new OperateResult<string>( ex.Message + " Source:" + content.ToHexString( ' ' ) );
			}
		}
	}
}
