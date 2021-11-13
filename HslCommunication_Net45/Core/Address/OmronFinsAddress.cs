using HslCommunication.Profinet.Omron;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HslCommunication.Core.Address
{
	/// <summary>
	/// 欧姆龙的Fins协议的地址类对象
	/// </summary>
	public class OmronFinsAddress : DeviceAddressDataBase
	{
		/// <summary>
		/// 进行位操作的指令
		/// </summary>
		public byte BitCode { get; set; }

		/// <summary>
		/// 进行字操作的指令
		/// </summary>
		public byte WordCode { get; set; }

		/// <summary>
		/// 从指定的地址信息解析成真正的设备地址信息
		/// </summary>
		/// <param name="address">地址信息</param>
		/// <param name="length">数据长度</param>
		public override void Parse( string address, ushort length )
		{
			OperateResult<OmronFinsAddress> addressData = ParseFrom( address, length );
			if (addressData.IsSuccess)
			{
				AddressStart = addressData.Content.AddressStart;
				Length       = addressData.Content.Length;
				BitCode      = addressData.Content.BitCode;
				WordCode     = addressData.Content.WordCode;
			}
		}

		#region Static Method

		/// <summary>
		/// 从实际的欧姆龙的地址里面解析出地址对象<br />
		/// Resolve the address object from the actual Omron address
		/// </summary>
		/// <param name="address">欧姆龙的地址数据信息</param>
		/// <returns>是否成功的结果对象</returns>
		public static OperateResult<OmronFinsAddress> ParseFrom( string address )
		{
			return ParseFrom( address, 0 );
		}

		/// <summary>
		/// 从实际的欧姆龙的地址里面解析出地址对象<br />
		/// Resolve the address object from the actual Omron address
		/// </summary>
		/// <param name="address">欧姆龙的地址数据信息</param>
		/// <param name="length">读取的数据长度</param>
		/// <returns>是否成功的结果对象</returns>
		public static OperateResult<OmronFinsAddress> ParseFrom( string address, ushort length )
		{
			OmronFinsAddress addressData = new OmronFinsAddress( );
			try
			{
				addressData.Length = length;
				switch (address[0])
				{
					case 'D':
					case 'd':
						{
							// DM区数据
							addressData.BitCode  = OmronFinsDataType.DM.BitCode;
							addressData.WordCode = OmronFinsDataType.DM.WordCode;
							break;
						}
					case 'C':
					case 'c':
						{
							// CIO区数据
							addressData.BitCode  = OmronFinsDataType.CIO.BitCode;
							addressData.WordCode = OmronFinsDataType.CIO.WordCode;
							break;
						}
					case 'W':
					case 'w':
						{
							// WR区
							addressData.BitCode  = OmronFinsDataType.WR.BitCode;
							addressData.WordCode = OmronFinsDataType.WR.WordCode;
							break;
						}
					case 'H':
					case 'h':
						{
							// HR区
							addressData.BitCode  = OmronFinsDataType.HR.BitCode;
							addressData.WordCode = OmronFinsDataType.HR.WordCode;
							break;
						}
					case 'A':
					case 'a':
						{
							// AR区
							addressData.BitCode  = OmronFinsDataType.AR.BitCode;
							addressData.WordCode = OmronFinsDataType.AR.WordCode;
							break;
						}
					case 'E':
					case 'e':
						{
							// E区，比较复杂，需要专门的计算
							string[] splits = address.SplitDot( );
							int block = Convert.ToInt32( splits[0].Substring( 1 ), 16 );
							if (block < 16)
							{
								addressData.BitCode  = (byte)(0x20 + block);
								addressData.WordCode = (byte)(0xA0 + block);
							}
							else
							{
								addressData.BitCode  = (byte)(0xE0 + block - 16);
								addressData.WordCode = (byte)(0x60 + block - 16);
							}
							break;
						}
					default: throw new Exception( StringResources.Language.NotSupportedDataType );
				}

				if (address[0] == 'E' || address[0] == 'e')
				{
					string[] splits = address.SplitDot( );
					int addr = ushort.Parse( splits[1] ) * 16;
					// 包含位的情况，例如 E1.100.F
					if (splits.Length > 2) addr += HslHelper.CalculateBitStartIndex( splits[2] );
					addressData.AddressStart = addr;
				}
				else
				{
					string[] splits = address.Substring( 1 ).SplitDot();
					int addr = ushort.Parse( splits[0] ) * 16;
					// 包含位的情况，例如 D100.F
					if (splits.Length > 1) addr += HslHelper.CalculateBitStartIndex( splits[1] );
					addressData.AddressStart = addr;
				}
			}
			catch (Exception ex)
			{
				return new OperateResult<OmronFinsAddress>( ex.Message );
			}

			return OperateResult.CreateSuccessResult( addressData );
		}

		#endregion
	}
}
