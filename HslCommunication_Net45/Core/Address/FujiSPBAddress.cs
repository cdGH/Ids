using HslCommunication.Profinet.Fuji;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HslCommunication.Core.Address
{
	/// <summary>
	/// FujiSPB的地址信息，可以携带数据类型，起始地址操作
	/// </summary>
	public class FujiSPBAddress : DeviceAddressDataBase
	{
		/// <summary>
		/// 数据的类型代码
		/// </summary>
		public string TypeCode { get; set; }

		/// <summary>
		/// 当是位地址的时候，用于标记的信息
		/// </summary>
		public int BitIndex { get; set; }

		/// <summary>
		/// 获取读写字数据的时候的地址信息内容
		/// </summary>
		/// <returns>报文信息</returns>
		public string GetWordAddress( )
		{
			return $"{TypeCode}{FujiSPBHelper.AnalysisIntegerAddress( AddressStart )}";
		}

		/// <summary>
		/// 获取命令，写入字地址的某一位的命令内容
		/// </summary>
		/// <returns>报文信息</returns>
		public string GetWriteBoolAddress( )
		{
			int byteIndex = AddressStart * 2;
			int bitIndex = BitIndex;
			if (bitIndex >= 8)
			{
				byteIndex++;
				bitIndex -= 8;
			}

			return $"{TypeCode}{FujiSPBHelper.AnalysisIntegerAddress( byteIndex )}{bitIndex:X2}";
		}

		/// <summary>
		/// 按照位为单位获取相关的索引信息
		/// </summary>
		/// <returns>位数据信息</returns>
		public int GetBitIndex( )
		{
			return AddressStart * 16 + BitIndex;
		}

		#region Static Method

		/// <summary>
		/// 从实际的Fuji的地址里面解析出地址对象<br />
		/// Resolve the address object from the actual Fuji address
		/// </summary>
		/// <param name="address">富士的地址数据信息</param>
		/// <returns>是否成功的结果对象</returns>
		public static OperateResult<FujiSPBAddress> ParseFrom( string address )
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
		public static OperateResult<FujiSPBAddress> ParseFrom( string address, ushort length )
		{
			FujiSPBAddress addressData = new FujiSPBAddress( );
			addressData.Length = length;
			try
			{
				addressData.BitIndex = HslHelper.GetBitIndexInformation( ref address );

				switch (address[0])
				{
					case 'X':
					case 'x':
						{
							addressData.TypeCode = "01";
							addressData.AddressStart = Convert.ToUInt16( address.Substring( 1 ), 10 );
							break;
						}
					case 'Y':
					case 'y':
						{
							addressData.TypeCode = "00";
							addressData.AddressStart = Convert.ToUInt16( address.Substring( 1 ), 10 );
							break;
						}
					case 'M':
					case 'm':
						{
							addressData.TypeCode = "02";
							addressData.AddressStart = Convert.ToUInt16( address.Substring( 1 ), 10 );
							break;
						}
					case 'L':
					case 'l':
						{
							addressData.TypeCode = "03";
							addressData.AddressStart = Convert.ToUInt16( address.Substring( 1 ), 10 );
							break;
						}
					case 'T':
					case 't':
						{
							if (address[1] == 'N' || address[1] == 'n')
							{
								addressData.TypeCode = "0A";
								addressData.AddressStart = Convert.ToUInt16( address.Substring( 2 ), 10 );
								break;
							}
							else if (address[1] == 'C' || address[1] == 'c')
							{
								addressData.TypeCode = "04";
								addressData.AddressStart = Convert.ToUInt16( address.Substring( 2 ), 10 );
								break;
							}
							else
							{
								throw new Exception( StringResources.Language.NotSupportedDataType );
							}
						}
					case 'C':
					case 'c':
						{
							if (address[1] == 'N' || address[1] == 'n')
							{
								addressData.TypeCode = "0B";
								addressData.AddressStart = Convert.ToUInt16( address.Substring( 2 ), 10 );
								break;
							}
							else if (address[1] == 'C' || address[1] == 'c')
							{
								addressData.TypeCode = "05";
								addressData.AddressStart = Convert.ToUInt16( address.Substring( 2 ), 10 );
								break;
							}
							else
							{
								throw new Exception( StringResources.Language.NotSupportedDataType );
							}
						}
					case 'D':
					case 'd':
						{
							addressData.TypeCode = "0C";
							addressData.AddressStart = Convert.ToUInt16( address.Substring( 1 ), 10 );
							break;
						}
					case 'R':
					case 'r':
						{
							addressData.TypeCode = "0D";
							addressData.AddressStart = Convert.ToUInt16( address.Substring( 1 ), 10 );
							break;
						}
					case 'W':
					case 'w':
						{
							addressData.TypeCode = "0E";
							addressData.AddressStart = Convert.ToUInt16( address.Substring( 1 ), 10 );
							break;
						}
					default: throw new Exception( StringResources.Language.NotSupportedDataType );
				}
			}
			catch (Exception ex)
			{
				return new OperateResult<FujiSPBAddress>( ex.Message );
			}
			return OperateResult.CreateSuccessResult( addressData );
		}

		#endregion
	}
}
