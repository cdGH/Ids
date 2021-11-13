using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HslCommunication.Core.Address
{
	/// <summary>
	/// 横河PLC的地址表示类<br />
	/// Yokogawa PLC address display class
	/// </summary>
	public class YokogawaLinkAddress : DeviceAddressDataBase
	{
		/// <summary>
		/// 获取或设置等待读取的数据的代码<br />
		/// Get or set the code of the data waiting to be read
		/// </summary>
		public int DataCode { get; set; }

		/// <summary>
		/// 获取当前横河PLC的地址的二进制表述方式<br />
		/// Obtain the binary representation of the current Yokogawa PLC address
		/// </summary>
		/// <returns>二进制数据信息</returns>
		public byte[] GetAddressBinaryContent( )
		{
			byte[] buffer = new byte[6];
			buffer[0] = BitConverter.GetBytes( DataCode )[1];
			buffer[1] = BitConverter.GetBytes( DataCode )[0];
			buffer[2] = BitConverter.GetBytes( AddressStart )[3];
			buffer[3] = BitConverter.GetBytes( AddressStart )[2];
			buffer[4] = BitConverter.GetBytes( AddressStart )[1];
			buffer[5] = BitConverter.GetBytes( AddressStart )[0];

			return buffer;
		}

		/// <inheritdoc/>
		public override void Parse( string address, ushort length )
		{
			OperateResult<YokogawaLinkAddress> addressData = ParseFrom( address, length );
			if (addressData.IsSuccess)
			{
				AddressStart = addressData.Content.AddressStart;
				Length = addressData.Content.Length;
				DataCode = addressData.Content.DataCode;
			}
		}

		/// <inheritdoc/>
		public override string ToString( )
		{
			switch (DataCode)
			{
				case 0x31: return "CN" + AddressStart.ToString( );
				case 0x21: return "TN" + AddressStart.ToString( );
				case 0x18: return "X" + AddressStart.ToString( );
				case 0x19: return "Y" + AddressStart.ToString( );
				case 0x09: return "I" + AddressStart.ToString( );
				case 0x05: return "E" + AddressStart.ToString( );
				case 0x0D: return "M" + AddressStart.ToString( );
				case 0x14: return "T" + AddressStart.ToString( );
				case 0x03: return "C" + AddressStart.ToString( );
				case 0x0C: return "L" + AddressStart.ToString( );
				case 0x04: return "D" + AddressStart.ToString( );
				case 0x02: return "B" + AddressStart.ToString( );
				case 0x06: return "F" + AddressStart.ToString( );
				case 0x12: return "R" + AddressStart.ToString( );
				case 0x16: return "V" + AddressStart.ToString( );
				case 0x1A: return "Z" + AddressStart.ToString( );
				case 0x17: return "W" + AddressStart.ToString( );
				default: return AddressStart.ToString( );
			}
		}

		/// <summary>
		/// 从普通的PLC的地址转换为HSL标准的地址信息
		/// </summary>
		/// <param name="address">地址信息</param>
		/// <param name="length">数据长度</param>
		/// <returns>是否成功的地址结果</returns>
		public static OperateResult<YokogawaLinkAddress> ParseFrom( string address, ushort length )
		{
			try
			{
				int type = 0;
				int offset = 0;
				if      (address.StartsWith( "CN" ) || address.StartsWith( "cn" )) { type = 0x31; offset = int.Parse( address.Substring( 2 ) ); }
				else if (address.StartsWith( "TN" ) || address.StartsWith( "tn" )) { type = 0x21; offset = int.Parse( address.Substring( 2 ) ); }
				else if (address.StartsWith( "X" )  || address.StartsWith( "x" ))  { type = 0x18; offset = int.Parse( address.Substring( 1 ) ); }
				else if (address.StartsWith( "Y" )  || address.StartsWith( "y" ))  { type = 0x19; offset = int.Parse( address.Substring( 1 ) ); }
				else if (address.StartsWith( "I" )  || address.StartsWith( "i" ))  { type = 0x09; offset = int.Parse( address.Substring( 1 ) ); }
				else if (address.StartsWith( "E" )  || address.StartsWith( "e" ))  { type = 0x05; offset = int.Parse( address.Substring( 1 ) ); }
				else if (address.StartsWith( "M" )  || address.StartsWith( "m" ))  { type = 0x0D; offset = int.Parse( address.Substring( 1 ) ); }
				else if (address.StartsWith( "T" )  || address.StartsWith( "t" ))  { type = 0x14; offset = int.Parse( address.Substring( 1 ) ); }
				else if (address.StartsWith( "C" )  || address.StartsWith( "c" ))  { type = 0x03; offset = int.Parse( address.Substring( 1 ) ); }
				else if (address.StartsWith( "L" )  || address.StartsWith( "l" ))  { type = 0x0C; offset = int.Parse( address.Substring( 1 ) ); }
				else if (address.StartsWith( "D" )  || address.StartsWith( "d" ))  { type = 0x04; offset = int.Parse( address.Substring( 1 ) ); }
				else if (address.StartsWith( "B" )  || address.StartsWith( "b" ))  { type = 0x02; offset = int.Parse( address.Substring( 1 ) ); }
				else if (address.StartsWith( "F" )  || address.StartsWith( "f" ))  { type = 0x06; offset = int.Parse( address.Substring( 1 ) ); }
				else if (address.StartsWith( "R" )  || address.StartsWith( "r" ))  { type = 0x12; offset = int.Parse( address.Substring( 1 ) ); }
				else if (address.StartsWith( "V" )  || address.StartsWith( "v" ))  { type = 0x16; offset = int.Parse( address.Substring( 1 ) ); }
				else if (address.StartsWith( "Z" )  || address.StartsWith( "z" ))  { type = 0x1A; offset = int.Parse( address.Substring( 1 ) ); }
				else if (address.StartsWith( "W" )  || address.StartsWith( "w" ))  { type = 0x17; offset = int.Parse( address.Substring( 1 ) ); }
				else { throw new Exception( StringResources.Language.NotSupportedDataType ); }

				return OperateResult.CreateSuccessResult( new YokogawaLinkAddress( ) 
				{ 
					DataCode = type,
					AddressStart = offset,
					Length = length
				} );
			}
			catch (Exception ex)
			{
				return new OperateResult<YokogawaLinkAddress>( ex.Message );
			}
		}
	}
}
