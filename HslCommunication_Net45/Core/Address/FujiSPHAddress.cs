using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
namespace HslCommunication.Core.Address
{
	/// <summary>
	/// 富士SPH地址类对象
	/// </summary>
	public class FujiSPHAddress : DeviceAddressDataBase
	{
		/// <summary>
		/// 数据的类型代码
		/// </summary>
		public byte TypeCode { get; set; }

		/// <summary>
		/// 当前地址的位索引信息
		/// </summary>
		public int BitIndex { get; set; }

		#region Static Method

		/// <summary>
		/// 从实际的Fuji的地址里面解析出地址对象<br />
		/// Resolve the address object from the actual Fuji address
		/// </summary>
		/// <param name="address">富士的地址数据信息</param>
		/// <returns>是否成功的结果对象</returns>
		public static OperateResult<FujiSPHAddress> ParseFrom( string address )
		{
			return ParseFrom( address, 0 );
		}

		/// <summary>
		/// 从实际的Fuji的地址里面解析出地址对象<br />
		/// Resolve the address object from the actual Fuji address
		/// </summary>
		/// <param name="address">富士的地址数据信息</param>
		/// <param name="length">读取的数据长度</param>
		/// <returns>是否成功的结果对象</returns>
		public static OperateResult<FujiSPHAddress> ParseFrom( string address, ushort length )
		{
			FujiSPHAddress addressData = new FujiSPHAddress( );
			try
			{
				switch (address[0])
				{
					case 'M':
					case 'm':
						{
							string[] splits = address.SplitDot( );
							int datablock = int.Parse( splits[0].Substring( 1 ) );
							if      (datablock == 0x01) addressData.TypeCode = 0x02;
							else if (datablock == 0x03) addressData.TypeCode = 0x04;
							else if (datablock == 0x0A) addressData.TypeCode = 0x08;
							else throw new Exception( StringResources.Language.NotSupportedDataType );

							addressData.AddressStart = Convert.ToInt32( splits[1] );
							if (splits.Length > 2) addressData.BitIndex = HslHelper.CalculateBitStartIndex( splits[2] );
							break;
						}
					case 'Q':
					case 'q':
					case 'I':
					case 'i':
						{
							string[] splits = address.SplitDot( );
							addressData.TypeCode = 0x01;
							addressData.AddressStart = Convert.ToInt32( splits[0].Substring( 1 ) );
							if (splits.Length > 1) addressData.BitIndex = HslHelper.CalculateBitStartIndex( splits[1] );
							break;
						}
					default: throw new Exception( StringResources.Language.NotSupportedDataType );
				}
			}
			catch (Exception ex)
			{
				return new OperateResult<FujiSPHAddress>( ex.Message );
			}
			return OperateResult.CreateSuccessResult( addressData );
		}

		#endregion
	}
}
