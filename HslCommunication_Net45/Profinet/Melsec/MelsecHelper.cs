using HslCommunication.BasicFramework;
using HslCommunication.Core.Address;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using HslCommunication.Core;

namespace HslCommunication.Profinet.Melsec
{
	/// <summary>
	/// 所有三菱通讯类的通用辅助工具类，包含了一些通用的静态方法，可以使用本类来获取一些原始的报文信息。详细的操作参见例子<br />
	/// All general auxiliary tool classes of Mitsubishi communication class include some general static methods. 
	/// You can use this class to get some primitive message information. See the example for detailed operation
	/// </summary>
	public class MelsecHelper
	{
		#region Melsec Mc Address

		/// <summary>
		/// 解析A1E协议数据地址<br />
		/// Parse A1E protocol data address
		/// </summary>
		/// <param name="address">数据地址</param>
		/// <returns>结果对象</returns>
		public static OperateResult<MelsecA1EDataType, int> McA1EAnalysisAddress(string address)
		{
			var result = new OperateResult<MelsecA1EDataType, int>();
			try
			{
				switch (address[0])
				{
					case 'T':
					case 't':
						{
							if(address[1] == 'S' || address[1] == 's')
							{
								result.Content1 = MelsecA1EDataType.TS;
								result.Content2 = Convert.ToInt32( address.Substring( 2 ), MelsecA1EDataType.TS.FromBase );
							}
							else if (address[1] == 'C' || address[1] == 'c')
							{
								result.Content1 = MelsecA1EDataType.TC;
								result.Content2 = Convert.ToInt32( address.Substring( 2 ), MelsecA1EDataType.TC.FromBase );
							}
							else if (address[1] == 'N' || address[1] == 'n')
							{
								result.Content1 = MelsecA1EDataType.TN;
								result.Content2 = Convert.ToInt32( address.Substring( 2 ), MelsecA1EDataType.TN.FromBase );
							}
							else
							{
								throw new Exception( StringResources.Language.NotSupportedDataType );
							}
							break;
						}
					case 'C':
					case 'c':
						{
							if (address[1] == 'S' || address[1] == 's')
							{
								result.Content1 = MelsecA1EDataType.CS;
								result.Content2 = Convert.ToInt32( address.Substring( 2 ), MelsecA1EDataType.CS.FromBase );
							}
							else if (address[1] == 'C' || address[1] == 'c')
							{
								result.Content1 = MelsecA1EDataType.CC;
								result.Content2 = Convert.ToInt32( address.Substring( 2 ), MelsecA1EDataType.CC.FromBase );
							}
							else if (address[1] == 'N' || address[1] == 'n')
							{
								result.Content1 = MelsecA1EDataType.CN;
								result.Content2 = Convert.ToInt32( address.Substring( 2 ), MelsecA1EDataType.CN.FromBase );
							}
							else
							{
								throw new Exception( StringResources.Language.NotSupportedDataType );
							}
							break;
						}
					case 'X':
					case 'x':
						{
							result.Content1 = MelsecA1EDataType.X;
							address = address.Substring( 1 );
							if (address.StartsWith( "0" ))
								result.Content2 = Convert.ToInt32( address, 8 );
							else
								result.Content2 = Convert.ToInt32( address, MelsecA1EDataType.X.FromBase );
							break;
						}
					case 'Y':
					case 'y':
						{
							result.Content1 = MelsecA1EDataType.Y;
							address = address.Substring( 1 );
							if (address.StartsWith( "0" ))
								result.Content2 = Convert.ToInt32( address, 8 );
							else
								result.Content2 = Convert.ToInt32( address, MelsecA1EDataType.Y.FromBase );
							break;
						}
					case 'M':
					case 'm':
						{
							result.Content1 = MelsecA1EDataType.M;
							result.Content2 = Convert.ToInt32( address.Substring(1), MelsecA1EDataType.M.FromBase);
							break;
						}
					case 'S':
					case 's':
						{
							result.Content1 = MelsecA1EDataType.S;
							result.Content2 = Convert.ToInt32( address.Substring(1), MelsecA1EDataType.S.FromBase);
							break;
						}
					case 'F':
					case 'f':
						{
							result.Content1 = MelsecA1EDataType.F;
							result.Content2 = Convert.ToInt32( address.Substring( 1 ), MelsecA1EDataType.F.FromBase );
							break;
						}
					case 'B':
					case 'b':
						{
							result.Content1 = MelsecA1EDataType.B;
							result.Content2 = Convert.ToInt32( address.Substring( 1 ), MelsecA1EDataType.B.FromBase );
							break;
						}
					case 'D':
					case 'd':
						{
							result.Content1 = MelsecA1EDataType.D;
							result.Content2 = Convert.ToInt32( address.Substring(1), MelsecA1EDataType.D.FromBase);
							break;
						}
					case 'R':
					case 'r':
						{
							result.Content1 = MelsecA1EDataType.R;
							result.Content2 = Convert.ToInt32( address.Substring(1), MelsecA1EDataType.R.FromBase);
							break;
						}
					case 'W':
					case 'w':
						{
							result.Content1 = MelsecA1EDataType.W;
							result.Content2 = Convert.ToInt32( address.Substring( 1 ), MelsecA1EDataType.W.FromBase );
							break;
						}
					default: throw new Exception(StringResources.Language.NotSupportedDataType);
				}
			}
			catch (Exception ex)
			{
				result.Message = ex.Message;
				return result;
			}

			result.IsSuccess = true;
			return result;
		}

		/// <summary>
		/// 根据三菱的错误码去查找对象描述信息
		/// </summary>
		/// <param name="code">错误码</param>
		/// <returns>描述信息</returns>
		public static string GetErrorDescription( int code )
		{
			switch (code)
			{
				case 0x0002: return StringResources.Language.MelsecError02;
				case 0x0051: return StringResources.Language.MelsecError51;
				case 0x0052: return StringResources.Language.MelsecError52;
				case 0x0054: return StringResources.Language.MelsecError54;
				case 0x0055: return StringResources.Language.MelsecError55;
				case 0x0056: return StringResources.Language.MelsecError56;
				case 0x0058: return StringResources.Language.MelsecError58;
				case 0x0059: return StringResources.Language.MelsecError59;
				case 0xC04D: return StringResources.Language.MelsecErrorC04D;
				case 0xC050: return StringResources.Language.MelsecErrorC050;
				case 0xC051:
				case 0xC052:
				case 0xC053:
				case 0xC054: return StringResources.Language.MelsecErrorC051_54;
				case 0xC055: return StringResources.Language.MelsecErrorC055;
				case 0xC056: return StringResources.Language.MelsecErrorC056;
				case 0xC057: return StringResources.Language.MelsecErrorC057;
				case 0xC058: return StringResources.Language.MelsecErrorC058;
				case 0xC059: return StringResources.Language.MelsecErrorC059;
				case 0xC05A:
				case 0xC05B: return StringResources.Language.MelsecErrorC05A_B;
				case 0xC05C: return StringResources.Language.MelsecErrorC05C;
				case 0xC05D: return StringResources.Language.MelsecErrorC05D;
				case 0xC05E: return StringResources.Language.MelsecErrorC05E;
				case 0xC05F: return StringResources.Language.MelsecErrorC05F;
				case 0xC060: return StringResources.Language.MelsecErrorC060;
				case 0xC061: return StringResources.Language.MelsecErrorC061;
				case 0xC062: return StringResources.Language.MelsecErrorC062;
				case 0xC070: return StringResources.Language.MelsecErrorC070;
				case 0xC072: return StringResources.Language.MelsecErrorC072;
				case 0xC074: return StringResources.Language.MelsecErrorC074;
				default: return StringResources.Language.MelsecPleaseReferToManualDocument;
			}
		}

		#endregion

		#region Common Logic

		/// <summary>
		/// 从三菱的地址中构建MC协议的6字节的ASCII格式的地址
		/// </summary>
		/// <param name="address">三菱地址</param>
		/// <param name="type">三菱的数据类型</param>
		/// <returns>6字节的ASCII格式的地址</returns>
		internal static byte[] BuildBytesFromAddress( int address, MelsecMcDataType type )
		{
			return Encoding.ASCII.GetBytes( address.ToString( type.FromBase == 10 ? "D6" : "X6" ) );
		}

		/// <summary>
		/// 将0，1，0，1的字节数组压缩成三菱格式的字节数组来表示开关量的
		/// </summary>
		/// <param name="value">原始的数据字节</param>
		/// <returns>压缩过后的数据字节</returns>
		internal static byte[] TransBoolArrayToByteData( byte[] value ) => TransBoolArrayToByteData( value.Select( m => m != 0x00 ).ToArray( ) );

		internal static bool[] TransByteArrayToBoolData( byte[] value, int offset, int length )
		{
			bool[] result = new bool[length > (value.Length - offset) * 2 ? (value.Length - offset) * 2 : length];
			for (int i = 0; i < result.Length; i++)
			{
				if( i % 2 == 0)
					result[i] = (value[offset + i / 2] & 0x10) == 0x10;
				else
					result[i] = (value[offset + i / 2] & 0x01) == 0x01;
			}
			return result;
		}

		/// <summary>
		/// 将bool的组压缩成三菱格式的字节数组来表示开关量的
		/// </summary>
		/// <param name="value">原始的数据字节</param>
		/// <returns>压缩过后的数据字节</returns>
		internal static byte[] TransBoolArrayToByteData( bool[] value )
		{
			int length = (value.Length + 1) / 2;
			byte[] buffer = new byte[length];

			for (int i = 0; i < length; i++)
			{
				if (value[i * 2 + 0]) buffer[i] += 0x10;
				if ((i * 2 + 1) < value.Length)
				{
					if (value[i * 2 + 1]) buffer[i] += 0x01;
				}
			}
			return buffer;
		}

		internal static byte[] TransByteArrayToAsciiByteArray(byte[] value )
		{
			if (value == null) return new byte[0];

			byte[] buffer = new byte[value.Length * 2];
			for (int i = 0; i < value.Length / 2; i++)
			{
				SoftBasic.BuildAsciiBytesFrom( BitConverter.ToUInt16( value, i * 2 ) ).CopyTo( buffer, 4 * i );
			}
			return buffer;
		}

		internal static byte[] TransAsciiByteArrayToByteArray( byte[] value )
		{
			byte[] Content = new byte[value.Length / 2];
			for (int i = 0; i < Content.Length / 2; i++)
			{
				ushort tmp = Convert.ToUInt16( Encoding.ASCII.GetString( value, i * 4 , 4 ), 16 );
				BitConverter.GetBytes( tmp ).CopyTo( Content, i * 2 );
			}
			return Content;
		}

		#endregion

		#region CRC Check

		/// <summary>
		/// 计算Fx协议指令的和校验信息
		/// </summary>
		/// <param name="data">字节数据</param>
		/// <returns>校验之后的数据</returns>
		internal static byte[] FxCalculateCRC( byte[] data )
		{
			int sum = 0;
			for (int i = 1; i < data.Length - 2; i++)
			{
				sum += data[i];
			}
			return SoftBasic.BuildAsciiBytesFrom( (byte)sum );
		}

		/// <summary>
		/// 检查指定的和校验是否是正确的
		/// </summary>
		/// <param name="data">字节数据</param>
		/// <returns>是否成功</returns>
		internal static bool CheckCRC( byte[] data )
		{
			byte[] crc = FxCalculateCRC( data );
			if (crc[0] != data[data.Length - 2]) return false;
			if (crc[1] != data[data.Length - 1]) return false;
			return true;
		}

		#endregion

	}
}
