using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace HslCommunication.Core.Address
{
	/// <summary>
	/// 富士CommandSettingsType的协议信息
	/// </summary>
	public class FujiCommandSettingTypeAddress : DeviceAddressDataBase
	{

		/// <summary>
		/// 数据的代号信息
		/// </summary>
		public byte DataCode { get; set; }

		/// <summary>
		/// 地址的头信息，缓存的情况
		/// </summary>
		public string AddressHeader { get; set; }

		/// <inheritdoc/>
		public override void Parse( string address, ushort length )
		{
			base.Parse( address, length );
		}

		/// <inheritdoc/>
		public override string ToString( ) => AddressHeader + AddressStart;

		/// <summary>
		/// 从字符串地址解析fuji的实际地址信息，如果解析成功，则 <see cref="OperateResult.IsSuccess"/> 为 True，取 <see cref="OperateResult{T}.Content"/> 值即可。
		/// </summary>
		/// <param name="address">字符串地址</param>
		/// <param name="length">读取的长度信息</param>
		/// <returns>是否解析成功</returns>
		public static OperateResult<FujiCommandSettingTypeAddress> ParseFrom( string address, ushort length )
		{
			try
			{
				FujiCommandSettingTypeAddress fujiAddress = new FujiCommandSettingTypeAddress( );
				string addType = string.Empty;
				string addOffset = string.Empty;
				if (address.IndexOf( '.' ) < 0)
				{
					Match match = Regex.Match( address, "^[A-F]+[^0-9]" );
					if (!match.Success) return new OperateResult<FujiCommandSettingTypeAddress>( StringResources.Language.NotSupportedDataType );

					addType = match.Value;
					addOffset = address.Substring( addType.Length );
				}
				else
				{
					string[] splits = address.Split( new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries );
					if (splits[0][0] != 'W') return new OperateResult<FujiCommandSettingTypeAddress>( StringResources.Language.NotSupportedDataType );

					addType = splits[0] + ".";
					addOffset = splits[1];
				}
				fujiAddress.AddressHeader = addType;
				fujiAddress.AddressStart = Convert.ToInt32( addOffset );
				fujiAddress.Length = length;

				if      (addType == "TS") fujiAddress.DataCode = 0x0A;
				else if (addType == "TR") fujiAddress.DataCode = 0x0B;
				else if (addType == "CS") fujiAddress.DataCode = 0x0C;
				else if (addType == "CR") fujiAddress.DataCode = 0x0D;
				else if (addType == "BD") fujiAddress.DataCode = 0x0E;
				else if (addType == "WL") fujiAddress.DataCode = 0x14;
				else if (addType == "B")  fujiAddress.DataCode = 0x00;
				else if (addType == "M")  fujiAddress.DataCode = 0x01;
				else if (addType == "K")  fujiAddress.DataCode = 0x02;
				else if (addType == "F")  fujiAddress.DataCode = 0x03;
				else if (addType == "A")  fujiAddress.DataCode = 0x04;
				else if (addType == "D")  fujiAddress.DataCode = 0x05;
				else if (addType == "S")  fujiAddress.DataCode = 0x08;
				else if (addType.StartsWith( "W" ))
				{
					int add = Convert.ToInt32( addType.Substring( 1 ) );
					if      (add == 9) fujiAddress.DataCode = 0x09;
					else if (add >= 21 && add <= 26) fujiAddress.DataCode = (byte)add;
					else if (add >= 30 && add <= 109) fujiAddress.DataCode = (byte)add;
					else if (add >= 120 && add <= 123) fujiAddress.DataCode = (byte)add;
					else if (add == 125) fujiAddress.DataCode = (byte)add;
					else return new OperateResult<FujiCommandSettingTypeAddress>( StringResources.Language.NotSupportedDataType );
				}

				//switch (fujiAddress.DataCode)
				//{
				//	case 0x09:
				//	case 0x0A:
				//	case 0x0B:
				//	case 0x0C:
				//	case 0x0D:
				//	case 0x0E:
				//	case 0x19:
				//		{
				//			fujiAddress.Length = (ushort)(length > 1 ? length / 2 : length);
				//			break;
				//		}
				//	case 0x08:
				//		{
				//			fujiAddress.Length = (ushort)(length == 0 ? 1 : length * 2);
				//			break;
				//		}
				//}

				return OperateResult.CreateSuccessResult( fujiAddress );
			}
			catch( Exception ex)
			{
				return new OperateResult<FujiCommandSettingTypeAddress>( ex.Message );
			}
		}
	}
}
