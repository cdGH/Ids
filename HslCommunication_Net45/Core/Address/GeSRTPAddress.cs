using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HslCommunication.Core.Address
{
	/// <summary>
	/// GE的SRTP协议的地址内容，主要包含一个数据代码信息，还有静态的解析地址的方法<br />
	/// The address content of GE's SRTP protocol mainly includes a data code information, as well as a static method of address resolution
	/// </summary>
	public class GeSRTPAddress : DeviceAddressDataBase
	{
		/// <summary>
		/// 获取或设置等待读取的数据的代码<br />
		/// Get or set the code of the data waiting to be read
		/// </summary>
		public byte DataCode { get; set; }

		/// <inheritdoc/>
		public override void Parse( string address, ushort length )
		{
			OperateResult<GeSRTPAddress> addressData = ParseFrom( address, length, false );
			if (addressData.IsSuccess)
			{
				AddressStart = addressData.Content.AddressStart;
				Length = addressData.Content.Length;
				DataCode = addressData.Content.DataCode;
			}
		}

		#region Static Method

		/// <inheritdoc cref="ParseFrom(string, ushort, bool)"/>
		public static OperateResult<GeSRTPAddress> ParseFrom( string address, bool isBit )
		{
			return ParseFrom( address, 0, isBit );
		}

		/// <summary>
		/// 从GE的地址里，解析出实际的带数据码的 <see cref="GeSRTPAddress"/> 地址信息，起始地址会自动减一，和实际的地址相匹配
		/// </summary>
		/// <param name="address">实际的地址数据</param>
		/// <param name="length">读取的长度信息</param>
		/// <param name="isBit">是否位操作</param>
		/// <returns>是否成功的GE地址对象</returns>
		public static OperateResult<GeSRTPAddress> ParseFrom( string address, ushort length, bool isBit )
		{
			GeSRTPAddress addressData = new GeSRTPAddress( );
			try
			{
				addressData.Length = length;
				if (address.StartsWith( "AI" ) || address.StartsWith( "ai" ))
				{
					if (isBit) return new OperateResult<GeSRTPAddress>( StringResources.Language.GeSRTPNotSupportBitReadWrite );
					addressData.DataCode = 0x0A;
					addressData.AddressStart = Convert.ToInt32( address.Substring( 2 ) );
				}
				else if (address.StartsWith( "AQ" ) || address.StartsWith( "aq" ))
				{
					if (isBit) return new OperateResult<GeSRTPAddress>( StringResources.Language.GeSRTPNotSupportBitReadWrite );
					addressData.DataCode = 0x0C;
					addressData.AddressStart = Convert.ToInt32( address.Substring( 2 ) );
				}
				else if (address.StartsWith( "R" ) || address.StartsWith( "r" ))
				{
					if (isBit) return new OperateResult<GeSRTPAddress>( StringResources.Language.GeSRTPNotSupportBitReadWrite );
					addressData.DataCode = 0x08;
					addressData.AddressStart = Convert.ToInt32( address.Substring( 1 ) );
				}
				else if (address.StartsWith( "SA" ) || address.StartsWith( "sa" ))
				{
					addressData.DataCode = isBit ? (byte)0x4E : (byte)0x18;
					addressData.AddressStart = Convert.ToInt32( address.Substring( 2 ) );
				}
				else if (address.StartsWith( "SB" ) || address.StartsWith( "sb" ))
				{
					addressData.DataCode = isBit ? (byte)0x50 : (byte)0x1A;
					addressData.AddressStart = Convert.ToInt32( address.Substring( 2 ) );
				}
				else if (address.StartsWith( "SC" ) || address.StartsWith( "sc" ))
				{
					addressData.DataCode = isBit ? (byte)0x52 : (byte)0x1C;
					addressData.AddressStart = Convert.ToInt32( address.Substring( 2 ) );
				}
				else
				{
					if      (address[0] == 'I' || address[0] == 'i') addressData.DataCode = isBit ? (byte)0x46 : (byte)0x10;
					else if (address[0] == 'Q' || address[0] == 'q') addressData.DataCode = isBit ? (byte)0x48 : (byte)0x12;
					else if (address[0] == 'M' || address[0] == 'm') addressData.DataCode = isBit ? (byte)0x4C : (byte)0x16;
					else if (address[0] == 'T' || address[0] == 't') addressData.DataCode = isBit ? (byte)0x4A : (byte)0x14;
					else if (address[0] == 'S' || address[0] == 's') addressData.DataCode = isBit ? (byte)0x54 : (byte)0x1E;
					else if (address[0] == 'G' || address[0] == 'g') addressData.DataCode = isBit ? (byte)0x56 : (byte)0x38;
					else throw new Exception(StringResources.Language.NotSupportedDataType);

					addressData.AddressStart = Convert.ToInt32( address.Substring( 1 ) );
				}
			}
			catch (Exception ex)
			{
				return new OperateResult<GeSRTPAddress>( ex.Message );
			}

			if (addressData.AddressStart == 0) return new OperateResult<GeSRTPAddress>( StringResources.Language.GeSRTPAddressCannotBeZero );
			if (addressData.AddressStart > 0) addressData.AddressStart--;
			return OperateResult.CreateSuccessResult( addressData );
		}

		#endregion
	}
}
