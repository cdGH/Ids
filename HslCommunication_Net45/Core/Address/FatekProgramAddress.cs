using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HslCommunication.Core.Address
{
	/// <summary>
	/// 永宏编程口的地址类对象
	/// </summary>
	public class FatekProgramAddress : DeviceAddressDataBase
	{
		/// <summary>
		/// 数据的类型
		/// </summary>
		public string DataCode { get; set; }

		/// <inheritdoc/>
		public override void Parse( string address, ushort length )
		{
			OperateResult<FatekProgramAddress> addressData = ParseFrom( address, length );
			if (addressData.IsSuccess)
			{
				AddressStart = addressData.Content.AddressStart;
				Length       = addressData.Content.Length;
				DataCode     = addressData.Content.DataCode;
			}
		}

		/// <inheritdoc/>
		public override string ToString( )
		{
			if( DataCode == "X" || 
				DataCode == "Y" ||
				DataCode == "M" ||
				DataCode == "S" ||
				DataCode == "T" ||
				DataCode == "C" ||
				DataCode == "RT" ||
				DataCode == "RC"
				) return DataCode + AddressStart.ToString( "D4" );
			else return DataCode + AddressStart.ToString( "D5" );
		}

		/// <summary>
		/// 从普通的PLC的地址转换为HSL标准的地址信息
		/// </summary>
		/// <param name="address">地址信息</param>
		/// <param name="length">数据长度</param>
		/// <returns>是否成功的地址结果</returns>
		public static OperateResult<FatekProgramAddress> ParseFrom( string address, ushort length )
		{
			try
			{
				FatekProgramAddress programAddress = new FatekProgramAddress( );
				switch (address[0])
				{
					case 'X':
					case 'x':
						{
							programAddress.DataCode = "X";
							programAddress.AddressStart = Convert.ToUInt16( address.Substring( 1 ), 10 );
							break;
						}
					case 'Y':
					case 'y':
						{
							programAddress.DataCode = "Y";
							programAddress.AddressStart = Convert.ToUInt16( address.Substring( 1 ), 10 );
							break;
						}
					case 'M':
					case 'm':
						{
							programAddress.DataCode = "M";
							programAddress.AddressStart = Convert.ToUInt16( address.Substring( 1 ), 10 );
							break;
						}
					case 'S':
					case 's':
						{
							programAddress.DataCode = "S";
							programAddress.AddressStart = Convert.ToUInt16( address.Substring( 1 ), 10 );
							break;
						}
					case 'T':
					case 't':
						{
							programAddress.DataCode = "T";
							programAddress.AddressStart = Convert.ToUInt16( address.Substring( 1 ), 10 );
							break;
						}
					case 'C':
					case 'c':
						{
							programAddress.DataCode = "C";
							programAddress.AddressStart = Convert.ToUInt16( address.Substring( 1 ), 10 );
							break;
						}
					case 'D':
					case 'd':
						{
							programAddress.DataCode = "D";
							programAddress.AddressStart = Convert.ToUInt16( address.Substring( 1 ), 10 );
							break;
						}
					case 'R':
					case 'r':
						{
							if (address[1] == 'T' || address[1] == 't')
							{
								programAddress.DataCode = "RT";
								programAddress.AddressStart = Convert.ToUInt16( address.Substring( 2 ), 10 );
							}
							else if (address[1] == 'C' || address[1] == 'c')
							{
								programAddress.DataCode = "RC";
								programAddress.AddressStart = Convert.ToUInt16( address.Substring( 2 ), 10 );
							}
							else
							{
								programAddress.DataCode = "R";
								programAddress.AddressStart = Convert.ToUInt16( address.Substring( 1 ), 10 );
							}
							break;
						}
					default: throw new Exception( StringResources.Language.NotSupportedDataType );
				}

				return OperateResult.CreateSuccessResult( programAddress );
			}
			catch (Exception ex)
			{
				return new OperateResult<FatekProgramAddress>( ex.Message );
			}
		}
	}
}
