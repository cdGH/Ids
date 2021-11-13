using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HslCommunication.BasicFramework;

namespace HslCommunication.Core.Address
{
	/// <summary>
	/// 西门子的地址数据信息，主要包含数据代码，DB块，偏移地址（偏移地址对于不是CT类型而已，是位为单位的），当处于写入时，Length无效<br />
	/// Address data information of Siemens, mainly including data code, DB block, offset address, when writing, Length is invalid
	/// </summary>
	public class S7AddressData : DeviceAddressDataBase
	{
		/// <summary>
		/// 获取或设置等待读取的数据的代码<br />
		/// Get or set the code of the data waiting to be read
		/// </summary>
		public byte DataCode { get; set; }

		/// <summary>
		/// 获取或设置PLC的DB块数据信息<br />
		/// Get or set PLC DB data information
		/// </summary>
		public ushort DbBlock { get; set; }

		/// <summary>
		/// 从指定的地址信息解析成真正的设备地址信息
		/// </summary>
		/// <param name="address">地址信息</param>
		/// <param name="length">数据长度</param>
		public override void Parse( string address, ushort length )
		{
			OperateResult<S7AddressData> addressData = ParseFrom( address, length );
			if (addressData.IsSuccess)
			{
				AddressStart = addressData.Content.AddressStart;
				Length       = addressData.Content.Length;
				DataCode     = addressData.Content.DataCode;
				DbBlock      = addressData.Content.DbBlock;
			}
		}

		/// <inheritdoc/>
		public override string ToString( )
		{
			if (DataCode == 0x1F) return "T" + AddressStart.ToString( );
			if (DataCode == 0x1E) return "C" + AddressStart.ToString( );
			if (DataCode == 0x06) return "AI" + GetActualStringAddress( AddressStart );
			if (DataCode == 0x07) return "AQ" + GetActualStringAddress( AddressStart );
			if (DataCode == 0x81) return "I" + GetActualStringAddress( AddressStart );
			if (DataCode == 0x82) return "Q" + GetActualStringAddress( AddressStart );
			if (DataCode == 0x83) return "M" + GetActualStringAddress( AddressStart );
			if (DataCode == 0x84) return "DB" + DbBlock + "." + GetActualStringAddress( AddressStart );
			return AddressStart.ToString( );
		}

		#region Static Method

		private static string GetActualStringAddress(int addressStart )
		{
			if (addressStart % 8 == 0)
				return (addressStart / 8).ToString( );
			else
				return $"{addressStart / 8}.{addressStart % 8}";
		}

		/// <summary>
		/// 计算特殊的地址信息<br />
		/// Calculate Special Address information
		/// </summary>
		/// <param name="address">字符串地址 -> String address</param>
		/// <param name="isCT">是否是定时器和计数器的地址</param>
		/// <returns>实际值 -> Actual value</returns>
		public static int CalculateAddressStarted( string address, bool isCT = false )
		{
			if (address.IndexOf( '.' ) < 0)
			{
				if (isCT)
					return Convert.ToInt32( address );
				else
					return Convert.ToInt32( address ) * 8;
			}
			else
			{
				string[] temp = address.Split( '.' );
				return Convert.ToInt32( temp[0] ) * 8 + Convert.ToInt32( temp[1] );
			}
		}

		/// <summary>
		/// 从实际的西门子的地址里面解析出地址对象<br />
		/// Resolve the address object from the actual Siemens address
		/// </summary>
		/// <param name="address">西门子的地址数据信息</param>
		/// <returns>是否成功的结果对象</returns>
		public static OperateResult<S7AddressData> ParseFrom( string address )
		{
			return ParseFrom( address, 0 );
		}

		/// <summary>
		/// 从实际的西门子的地址里面解析出地址对象<br />
		/// Resolve the address object from the actual Siemens address
		/// </summary>
		/// <param name="address">西门子的地址数据信息</param>
		/// <param name="length">读取的数据长度</param>
		/// <returns>是否成功的结果对象</returns>
		public static OperateResult<S7AddressData> ParseFrom( string address, ushort length )
		{
			S7AddressData addressData = new S7AddressData( );
			try
			{
				addressData.Length = length;
				addressData.DbBlock = 0;
				if (address.StartsWith( "AI" ) || address.StartsWith( "ai" ))
				{
					addressData.DataCode = 0x06;
					addressData.AddressStart = CalculateAddressStarted( address.Substring( 2 ) );
				}
				else if (address.StartsWith( "AQ" ) || address.StartsWith( "aq" ))
				{
					addressData.DataCode = 0x07;
					addressData.AddressStart = CalculateAddressStarted( address.Substring( 2 ) );
				}
				else if (address[0] == 'I')
				{
					addressData.DataCode = 0x81;
					addressData.AddressStart = CalculateAddressStarted( address.Substring( 1 ) );
				}
				else if (address[0] == 'Q')
				{
					addressData.DataCode = 0x82;
					addressData.AddressStart = CalculateAddressStarted( address.Substring( 1 ) );
				}
				else if (address[0] == 'M')
				{
					addressData.DataCode = 0x83;
					addressData.AddressStart = CalculateAddressStarted( address.Substring( 1 ) );
				}
				else if (address[0] == 'D' || address.Substring( 0, 2 ) == "DB")
				{
					addressData.DataCode = 0x84;
					string[] adds = address.Split( '.' );
					if (address[1] == 'B')
					{
						addressData.DbBlock = Convert.ToUInt16( adds[0].Substring( 2 ) );
					}
					else
					{
						addressData.DbBlock = Convert.ToUInt16( adds[0].Substring( 1 ) );
					}

					string addTemp = address.Substring( address.IndexOf( '.' ) + 1 );
					if (addTemp.StartsWith( "DBX" ) || addTemp.StartsWith( "DBB" ) || addTemp.StartsWith( "DBW" ) || addTemp.StartsWith( "DBD" ))
						addTemp = addTemp.Substring( 3 );
					addressData.AddressStart = CalculateAddressStarted( addTemp );
				}
				else if (address[0] == 'T')
				{
					addressData.DataCode = 0x1F;
					addressData.AddressStart = CalculateAddressStarted( address.Substring( 1 ), true );
				}
				else if (address[0] == 'C')
				{
					addressData.DataCode = 0x1E;
					addressData.AddressStart = CalculateAddressStarted( address.Substring( 1 ), true );
				}
				else if (address[0] == 'V')
				{
					addressData.DataCode = 0x84;
					addressData.DbBlock = 1;
					if (address.StartsWith( "VB" ) || address.StartsWith( "VW" ) || address.StartsWith( "VD" ) || address.StartsWith( "VX" ))
						addressData.AddressStart = CalculateAddressStarted( address.Substring( 2 ) );
					else
						addressData.AddressStart = CalculateAddressStarted( address.Substring( 1 ) );
				}
				else
				{
					return new OperateResult<S7AddressData>( StringResources.Language.NotSupportedDataType );
				}
			}
			catch (Exception ex)
			{
				return new OperateResult<S7AddressData>( ex.Message );
			}

			return OperateResult.CreateSuccessResult( addressData );
		}

		#endregion
	}
}
