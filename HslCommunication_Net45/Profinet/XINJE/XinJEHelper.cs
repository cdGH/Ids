using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HslCommunication.Core;
using HslCommunication.ModBus;

namespace HslCommunication.Profinet.XINJE
{
	/// <summary>
	/// 信捷PLC的相关辅助类
	/// </summary>
	public class XinJEHelper
	{
		private static int CalculateXinJEStartAddress( string address )
		{
			if (address.IndexOf( '.' ) < 0)
				return Convert.ToInt32( address, 8 );
			else
			{
				string[] splits = address.Split( new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries );
				return Convert.ToInt32( splits[0], 8 ) * 8 + int.Parse( splits[1] );
			}
		}

		/// <summary>
		/// 根据信捷PLC的地址，解析出转换后的modbus协议信息
		/// </summary>
		/// <param name="series">PLC的系列信息</param>
		/// <param name="address">汇川plc的地址信息</param>
		/// <param name="modbusCode">原始的对应的modbus信息</param>
		/// <returns>还原后的modbus地址</returns>
		public static OperateResult<string> PraseXinJEAddress( XinJESeries series, string address, byte modbusCode )
		{
			if (series == XinJESeries.XC) return PraseXinJEXCAddress( address, modbusCode );
			return PraseXinJEXD1XD2XD3XL1XL3Address( address, modbusCode );
		}

		/// <summary>
		/// 根据信捷PLC的地址，解析出转换后的modbus协议信息，适用XC系列
		/// </summary>
		/// <param name="address">安川plc的地址信息</param>
		/// <param name="modbusCode">原始的对应的modbus信息</param>
		/// <returns>还原后的modbus地址</returns>
		public static OperateResult<string> PraseXinJEXCAddress( string address, byte modbusCode )
		{
			try
			{
				string station = string.Empty;
				OperateResult<int> stationPara = HslHelper.ExtractParameter( ref address, "s" );
				if (stationPara.IsSuccess) station = $"s={stationPara.Content};";

				if (modbusCode == ModbusInfo.ReadCoil || modbusCode == ModbusInfo.WriteCoil || modbusCode == ModbusInfo.WriteOneCoil)
				{
					if (address.StartsWith( "X" ) || address.StartsWith( "x" ))
						return OperateResult.CreateSuccessResult( station + (CalculateXinJEStartAddress( address.Substring( 1 ) ) + 0x4000).ToString( ) );
					else if (address.StartsWith( "Y" ) || address.StartsWith( "y" ))
						return OperateResult.CreateSuccessResult( station + (CalculateXinJEStartAddress( address.Substring( 1 ) ) + 0x4800).ToString( ) );
					else if (address.StartsWith( "S" ) || address.StartsWith( "s" ))
						return OperateResult.CreateSuccessResult( station + (Convert.ToInt32( address.Substring( 1 ) ) + 0x5000).ToString( ) );
					else if (address.StartsWith( "T" ) || address.StartsWith( "t" ))
						return OperateResult.CreateSuccessResult( station + (Convert.ToInt32( address.Substring( 1 ) ) + 0x6400).ToString( ) );
					else if (address.StartsWith( "C" ) || address.StartsWith( "c" ))
						return OperateResult.CreateSuccessResult( station + (Convert.ToInt32( address.Substring( 1 ) ) + 0x6C00).ToString( ) );
					else if (address.StartsWith( "M" ) || address.StartsWith( "m" ))
					{
						int add = Convert.ToInt32( address.Substring( 1 ) );
						if (add >= 8000)
							return OperateResult.CreateSuccessResult( station + (add - 8000 + 0x6000).ToString( ) );
						else
							return OperateResult.CreateSuccessResult( station + add.ToString( ) );
					}
				}
				else
				{
					if (address.StartsWith( "D" ) || address.StartsWith( "d" ))
					{
						int add = Convert.ToInt32( address.Substring( 1 ) );
						if (add >= 8000)
							return OperateResult.CreateSuccessResult( station + (add - 8000 + 0x4000).ToString( ) );
						else
							return OperateResult.CreateSuccessResult( station + add.ToString( ) );
					}
					else if (address.StartsWith( "F" ) || address.StartsWith( "f" ))
					{
						int add = Convert.ToInt32( address.Substring( 1 ) );
						if (add >= 8000)
							return OperateResult.CreateSuccessResult( station + (add - 8000 + 0x6800).ToString( ) );
						else
							return OperateResult.CreateSuccessResult( station + (add + 0x4800).ToString( ) );
					}
					else if (address.StartsWith( "E" ) || address.StartsWith( "e" ))
						return OperateResult.CreateSuccessResult( station + (Convert.ToInt32( address.Substring( 1 ) ) + 0x7000).ToString( ) );
					else if (address.StartsWith( "T" ) || address.StartsWith( "t" ))
						return OperateResult.CreateSuccessResult( station + (Convert.ToInt32( address.Substring( 1 ) ) + 0x3000).ToString( ) );
					else if (address.StartsWith( "C" ) || address.StartsWith( "c" ))
						return OperateResult.CreateSuccessResult( station + (Convert.ToInt32( address.Substring( 1 ) ) + 0x3800).ToString( ) );
				}

				return new OperateResult<string>( StringResources.Language.NotSupportedDataType );
			}
			catch (Exception ex)
			{
				return new OperateResult<string>( ex.Message );
			}
		}

		/// <summary>
		/// 解析信捷的XD1,XD2,XD3,XL1,XL3系列的PLC的Modbus地址和内部软元件的对照
		/// </summary>
		/// <param name="address">PLC内部的软元件的地址</param>
		/// <param name="modbusCode">默认的Modbus功能码</param>
		/// <returns>解析后的Modbus地址</returns>
		public static OperateResult<string> PraseXinJEXD1XD2XD3XL1XL3Address( string address, byte modbusCode )
		{
			try
			{
				string station = string.Empty;
				OperateResult<int> stationPara = HslHelper.ExtractParameter( ref address, "s" );
				if (stationPara.IsSuccess) station = $"s={stationPara.Content};";

				if (modbusCode == ModbusInfo.ReadCoil || modbusCode == ModbusInfo.WriteCoil || modbusCode == ModbusInfo.WriteOneCoil)
				{
					if (address.StartsWith( "X" ) || address.StartsWith( "x" ))
					{
						int start = CalculateXinJEStartAddress( address.Substring( 1 ) );
						if (start < 0x1000) return OperateResult.CreateSuccessResult( station + (start - 0x0000 + 0x5000).ToString( ) ); // X0 - X77
						if (start < 0x2000) return OperateResult.CreateSuccessResult( station + (start - 0x1000 + 0x5100).ToString( ) ); // X10000 - X11177  10个模块
						if (start < 0x3000) return OperateResult.CreateSuccessResult( station + (start - 0x2000 + 0x58D0).ToString( ) ); // X20000 - X20177  2个模块
						return OperateResult.CreateSuccessResult( station + (start - 0x3000 + 0x5BF0).ToString( ) ); // #1 ED
					}
					else if (address.StartsWith( "Y" ) || address.StartsWith( "y" ))
					{
						int start = CalculateXinJEStartAddress( address.Substring( 1 ) );
						if (start < 0x1000) return OperateResult.CreateSuccessResult( station + (start - 0x0000 + 0x6000).ToString( ) ); // Y0 - Y77
						if (start < 0x2000) return OperateResult.CreateSuccessResult( station + (start - 0x1000 + 0x6100).ToString( ) ); // Y10000 - Y11177  10个模块
						if (start < 0x3000) return OperateResult.CreateSuccessResult( station + (start - 0x2000 + 0x68D0).ToString( ) ); // Y20000 - Y20177  2个模块
						return OperateResult.CreateSuccessResult( station + (start - 0x3000 + 0x6BF0).ToString( ) ); // #1 ED
					}
					else if (address.StartsWith( "SEM" ) || address.StartsWith( "sem" ))
						return OperateResult.CreateSuccessResult( station + (Convert.ToInt32( address.Substring( 3 ) ) + 0xC080).ToString( ) );
					else if (address.StartsWith( "HSC" ) || address.StartsWith( "hsc" ))
						return OperateResult.CreateSuccessResult( station + (Convert.ToInt32( address.Substring( 3 ) ) + 0xE900).ToString( ) );
					else if (address.StartsWith( "SM" ) || address.StartsWith( "sm" ))
						return OperateResult.CreateSuccessResult( station + (Convert.ToInt32( address.Substring( 2 ) ) + 0x9000).ToString( ) );
					else if (address.StartsWith( "ET" ) || address.StartsWith( "et" ))
						return OperateResult.CreateSuccessResult( station + (Convert.ToInt32( address.Substring( 2 ) ) + 0xC000).ToString( ) );
					else if (address.StartsWith( "HM" ) || address.StartsWith( "hm" ))
						return OperateResult.CreateSuccessResult( station + (Convert.ToInt32( address.Substring( 2 ) ) + 0xC100).ToString( ) );
					else if (address.StartsWith( "HS" ) || address.StartsWith( "hs" ))
						return OperateResult.CreateSuccessResult( station + (Convert.ToInt32( address.Substring( 2 ) ) + 0xD900).ToString( ) );
					else if (address.StartsWith( "HT" ) || address.StartsWith( "ht" ))
						return OperateResult.CreateSuccessResult( station + (Convert.ToInt32( address.Substring( 2 ) ) + 0xE100).ToString( ) );
					else if (address.StartsWith( "HC" ) || address.StartsWith( "hc" ))
						return OperateResult.CreateSuccessResult( station + (Convert.ToInt32( address.Substring( 2 ) ) + 0xE500).ToString( ) );
					else if (address.StartsWith( "S" ) || address.StartsWith( "s" ))
						return OperateResult.CreateSuccessResult( station + (Convert.ToInt32( address.Substring( 1 ) ) + 0x7000).ToString( ) );
					else if (address.StartsWith( "T" ) || address.StartsWith( "t" ))
						return OperateResult.CreateSuccessResult( station + (Convert.ToInt32( address.Substring( 1 ) ) + 0xA000).ToString( ) );
					else if (address.StartsWith( "C" ) || address.StartsWith( "c" ))
						return OperateResult.CreateSuccessResult( station + (Convert.ToInt32( address.Substring( 1 ) ) + 0xB000).ToString( ) );
					else if (address.StartsWith( "M" ) || address.StartsWith( "m" ))
						return OperateResult.CreateSuccessResult( station + (Convert.ToInt32( address.Substring( 1 ) ) + 0x0000).ToString( ) );
				}
				else
				{
					if (address.StartsWith( "ID" ) || address.StartsWith( "id" ))
					{
						int start = Convert.ToInt32( address.Substring( 2 ) );
						if (start < 10000) return OperateResult.CreateSuccessResult( station + (start + 0x5000).ToString( ) ); // ID0 - ID99
						if (start < 20000) return OperateResult.CreateSuccessResult( station + (start - 10000 + 0x5100).ToString( ) ); // ID10000 - ID10999
						if (start < 30000) return OperateResult.CreateSuccessResult( station + (start - 20000 + 0x58D0).ToString( ) ); // ID20000 - ID20199
						return OperateResult.CreateSuccessResult( station + (start - 30000 + 0x5BF0).ToString( ) ); // ID30000 - ID30099
					}
					else if (address.StartsWith( "QD" ) || address.StartsWith( "qd" ))
					{
						int start = Convert.ToInt32( address.Substring( 2 ) );
						if (start < 10000) return OperateResult.CreateSuccessResult( station + (start + 0x6000).ToString( ) ); // QD0 - QD99
						if (start < 20000) return OperateResult.CreateSuccessResult( station + (start - 10000 + 0x6100).ToString( ) ); // QD10000 - QD10999
						if (start < 30000) return OperateResult.CreateSuccessResult( station + (start - 20000 + 0x68D0).ToString( ) ); // QD20000 - QD20199
						return OperateResult.CreateSuccessResult( station + (start - 30000 + 0x6BF0).ToString( ) ); // QD30000 - QD30099
					}
					else if (address.StartsWith( "HSCD" ) || address.StartsWith( "hscd" ))
						return OperateResult.CreateSuccessResult( station + (Convert.ToInt32( address.Substring( 4 ) ) + 0xC480).ToString( ) );
					else if (address.StartsWith( "ETD" ) || address.StartsWith( "etd" ))
						return OperateResult.CreateSuccessResult( station + (Convert.ToInt32( address.Substring( 3 ) ) + 0xA000).ToString( ) );
					else if (address.StartsWith( "HSD" ) || address.StartsWith( "hsd" ))
						return OperateResult.CreateSuccessResult( station + (Convert.ToInt32( address.Substring( 3 ) ) + 0xB880).ToString( ) );
					else if (address.StartsWith( "HTD" ) || address.StartsWith( "htd" ))
						return OperateResult.CreateSuccessResult( station + (Convert.ToInt32( address.Substring( 3 ) ) + 0xBC80).ToString( ) );
					else if (address.StartsWith( "HCD" ) || address.StartsWith( "hcd" ))
						return OperateResult.CreateSuccessResult( station + (Convert.ToInt32( address.Substring( 3 ) ) + 0xC080).ToString( ) );
					else if (address.StartsWith( "SFD" ) || address.StartsWith( "sfd" ))
						return OperateResult.CreateSuccessResult( station + (Convert.ToInt32( address.Substring( 3 ) ) + 0xE4C0).ToString( ) );
					else if (address.StartsWith( "SD" ) || address.StartsWith( "sd" ))
						return OperateResult.CreateSuccessResult( station + (Convert.ToInt32( address.Substring( 2 ) ) + 0x7000).ToString( ) );
					else if (address.StartsWith( "TD" ) || address.StartsWith( "td" ))
						return OperateResult.CreateSuccessResult( station + (Convert.ToInt32( address.Substring( 2 ) ) + 0x8000).ToString( ) );
					else if (address.StartsWith( "CD" ) || address.StartsWith( "cd" ))
						return OperateResult.CreateSuccessResult( station + (Convert.ToInt32( address.Substring( 2 ) ) + 0x9000).ToString( ) );
					else if (address.StartsWith( "HD" ) || address.StartsWith( "hd" ))
						return OperateResult.CreateSuccessResult( station + (Convert.ToInt32( address.Substring( 2 ) ) + 0xA080).ToString( ) );
					else if (address.StartsWith( "FD" ) || address.StartsWith( "fd" ))
						return OperateResult.CreateSuccessResult( station + (Convert.ToInt32( address.Substring( 2 ) ) + 0xC4C0).ToString( ) );
					else if (address.StartsWith( "FS" ) || address.StartsWith( "fs" ))
						return OperateResult.CreateSuccessResult( station + (Convert.ToInt32( address.Substring( 2 ) ) + 0xF4C0).ToString( ) );
					else if (address.StartsWith( "D" ) || address.StartsWith( "d" ))
						return OperateResult.CreateSuccessResult( station + (Convert.ToInt32( address.Substring( 1 ) ) + 0x0000).ToString( ) );
				}

				return new OperateResult<string>( StringResources.Language.NotSupportedDataType );
			}
			catch (Exception ex)
			{
				return new OperateResult<string>( ex.Message );
			}
		}

	}
}
