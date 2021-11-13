using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using HslCommunication.BasicFramework;

namespace HslCommunication.Core
{
	/// <summary>
	/// HslCommunication的一些静态辅助方法<br />
	/// Some static auxiliary methods of HslCommunication
	/// </summary>
	public class HslHelper
	{
		/// <summary>
		/// 解析地址的附加参数方法，比如你的地址是s=100;D100，可以提取出"s"的值的同时，修改地址本身，如果"s"不存在的话，返回给定的默认值<br />
		/// The method of parsing additional parameters of the address, for example, if your address is s=100;D100, you can extract the value of "s" and modify the address itself. If "s" does not exist, return the given default value
		/// </summary>
		/// <param name="address">复杂的地址格式，比如：s=100;D100</param>
		/// <param name="paraName">等待提取的参数名称</param>
		/// <param name="defaultValue">如果提取的参数信息不存在，返回的默认值信息</param>
		/// <returns>解析后的新的数据值或是默认的给定的数据值</returns>
		public static int ExtractParameter( ref string address, string paraName, int defaultValue )
		{
			OperateResult<int> extra = ExtractParameter( ref address, paraName );
			return extra.IsSuccess ? extra.Content : defaultValue;
		}

		/// <summary>
		/// 解析地址的附加参数方法，比如你的地址是s=100;D100，可以提取出"s"的值的同时，修改地址本身，如果"s"不存在的话，返回错误的消息内容<br />
		/// The method of parsing additional parameters of the address, for example, if your address is s=100;D100, you can extract the value of "s" and modify the address itself. 
		/// If "s" does not exist, return the wrong message content
		/// </summary>
		/// <param name="address">复杂的地址格式，比如：s=100;D100</param>
		/// <param name="paraName">等待提取的参数名称</param>
		/// <returns>解析后的参数结果内容</returns>
		public static OperateResult<int> ExtractParameter( ref string address, string paraName )
		{
			try
			{
				Match match = Regex.Match( address, paraName + "=[0-9A-Fa-fx]+;" );
				if (!match.Success) return new OperateResult<int>( $"Address [{address}] can't find [{paraName}] Parameters. for example : {paraName}=1;100" );

				string number = match.Value.Substring( paraName.Length + 1, match.Value.Length - paraName.Length - 2 );
				int value = number.StartsWith( "0x" ) ? Convert.ToInt32( number.Substring( 2 ), 16 ) : number.StartsWith( "0" ) ? Convert.ToInt32( number, 8 ) : Convert.ToInt32( number );

				address = address.Replace( match.Value, "" );
				return OperateResult.CreateSuccessResult( value );
			}
			catch (Exception ex)
			{
				return new OperateResult<int>( $"Address [{address}] Get [{paraName}] Parameters failed: " + ex.Message );
			}
		}

		/// <summary>
		/// 解析地址的起始地址的方法，比如你的地址是 A[1] , 那么将会返回 1，地址修改为 A，如果不存在起始地址，那么就不修改地址，返回 -1<br />
		/// The method of parsing the starting address of the address, for example, if your address is A[1], then it will return 1, 
		/// and the address will be changed to A. If the starting address does not exist, then the address will not be changed and return -1
		/// </summary>
		/// <param name="address">复杂的地址格式，比如：A[0] </param>
		/// <returns>如果存在，就起始位置，不存在就返回 -1</returns>
		public static int ExtractStartIndex( ref string address )
		{
			try
			{
				Match match = Regex.Match( address, "\\[[0-9]+\\]$" );
				if (!match.Success) return -1;

				string number = match.Value.Substring( 1, match.Value.Length - 2 );
				int value = Convert.ToInt32( number );

				address = address.Remove( address.Length - match.Value.Length );
				return value;
			}
			catch
			{
				return -1;
			}
		}

		/// <summary>
		/// 解析地址的附加<see cref="DataFormat"/>参数方法，比如你的地址是format=ABCD;D100，可以提取出"format"的值的同时，修改地址本身，如果"format"不存在的话，返回默认的<see cref="IByteTransform"/>对象<br />
		/// Parse the additional <see cref="DataFormat"/> parameter method of the address. For example, if your address is format=ABCD;D100,
		/// you can extract the value of "format" and modify the address itself. If "format" does not exist, 
		/// Return the default <see cref="IByteTransform"/> object
		/// </summary>
		/// <param name="address">复杂的地址格式，比如：format=ABCD;D100</param>
		/// <param name="defaultTransform">默认的数据转换信息</param>
		/// <returns>解析后的参数结果内容</returns>
		public static IByteTransform ExtractTransformParameter( ref string address, IByteTransform defaultTransform )
		{
			if (!Authorization.asdniasnfaksndiqwhawfskhfaiw( )) return defaultTransform;

			try
			{
				string paraName = "format";
				Match match = Regex.Match( address, paraName + "=(ABCD|BADC|DCBA|CDAB);", RegexOptions.IgnoreCase );
				if (!match.Success) return defaultTransform;

				string format = match.Value.Substring( paraName.Length + 1, match.Value.Length - paraName.Length - 2 );
				DataFormat dataFormat = defaultTransform.DataFormat;

				switch (format.ToUpper( ))
				{
					case "ABCD": dataFormat = DataFormat.ABCD; break;
					case "BADC": dataFormat = DataFormat.BADC; break;
					case "DCBA": dataFormat = DataFormat.DCBA; break;
					case "CDAB": dataFormat = DataFormat.CDAB; break;
					default: break;
				}

				address = address.Replace( match.Value, "" );
				if (dataFormat != defaultTransform.DataFormat) return defaultTransform.CreateByDateFormat( dataFormat );
				return defaultTransform;
			}
			catch
			{
				throw;
			}
		}

		/// <summary>
		/// 切割当前的地址数据信息，根据读取的长度来分割成多次不同的读取内容，需要指定地址，总的读取长度，切割读取长度<br />
		/// Cut the current address data information, and divide it into multiple different read contents according to the read length. 
		/// You need to specify the address, the total read length, and the cut read length
		/// </summary>
		/// <param name="address">整数的地址信息</param>
		/// <param name="length">读取长度信息</param>
		/// <param name="segment">切割长度信息</param>
		/// <returns>切割结果</returns>
		public static OperateResult<int[], int[]> SplitReadLength( int address, ushort length, ushort segment )
		{
			int[] segments = SoftBasic.SplitIntegerToArray( length, segment );
			int[] addresses = new int[segments.Length];
			for (int i = 0; i < addresses.Length; i++)
			{
				if (i == 0) addresses[i] = address;
				else addresses[i] = addresses[i - 1] + segments[i - 1];
			}
			return OperateResult.CreateSuccessResult( addresses, segments );
		}

		/// <summary>
		/// 根据指定的长度切割数据数组，返回地址偏移量信息和数据分割信息
		/// </summary>
		/// <typeparam name="T">数组类型</typeparam>
		/// <param name="address">起始的地址</param>
		/// <param name="value">实际的数据信息</param>
		/// <param name="segment">分割的基本长度</param>
		/// <param name="addressLength">一个地址代表的数据长度</param>
		/// <returns>切割结果内容</returns>
		public static OperateResult<int[], List<T[]>> SplitWriteData<T>( int address, T[] value, ushort segment, int addressLength )
		{
			List<T[]> segments = SoftBasic.ArraySplitByLength( value, segment * addressLength );
			int[] addresses = new int[segments.Count];
			for (int i = 0; i < addresses.Length; i++)
			{
				if (i == 0) addresses[i] = address;
				else addresses[i] = addresses[i - 1] + segments[i - 1].Length / addressLength;
			}
			return OperateResult.CreateSuccessResult( addresses, segments );
		}

		/// <summary>
		/// 获取地址信息的位索引，在地址最后一个小数点的位置
		/// </summary>
		/// <param name="address">地址信息</param>
		/// <returns>位索引的位置</returns>
		public static int GetBitIndexInformation( ref string address )
		{
			int bitIndex = 0;
			int lastIndex = address.LastIndexOf( '.' );
			if (lastIndex > 0 && lastIndex < address.Length - 1)
			{
				string bit = address.Substring( lastIndex + 1 );
				if (bit.Contains( "A" ) || bit.Contains( "B" ) || bit.Contains( "C" ) || bit.Contains( "D" ) || bit.Contains( "E" ) || bit.Contains( "F" ))
				{
					bitIndex = Convert.ToInt32( bit, 16 );
				}
				else
				{
					bitIndex = Convert.ToInt32( bit );
				}
				address = address.Substring( 0, lastIndex );
			}
			return bitIndex;
		}

		/// <summary>
		/// 从当前的字符串信息获取IP地址数据，如果是ip地址直接返回，如果是域名，会自动解析IP地址，否则抛出异常<br />
		/// Get the IP address data from the current string information, if it is an ip address, return directly, 
		/// if it is a domain name, it will automatically resolve the IP address, otherwise an exception will be thrown
		/// </summary>
		/// <param name="value">输入的字符串信息</param>
		/// <returns>真实的IP地址信息</returns>
		public static string GetIpAddressFromInput( string value )
		{
			if (!string.IsNullOrEmpty( value ))
			{
				// 正则表达值校验Ip地址
				if (Regex.IsMatch( value, @"^[0-9]+\.[0-9]+\.[0-9]+\.[0-9]+$" ))
				{
					if (!IPAddress.TryParse( value, out IPAddress address ))
					{
						throw new Exception( StringResources.Language.IpAddressError );
					}
					return value;
				}
				else
				{
					IPHostEntry host = Dns.GetHostEntry( value );
					IPAddress[] iPs = host.AddressList;
					if (iPs.Length > 0) return iPs[0].ToString( );
				}
			}
			return "127.0.0.1";
		}

		/// <summary>
		/// 从流中接收指定长度的字节数组
		/// </summary>
		/// <param name="stream">流</param>
		/// <param name="length">数据长度</param>
		/// <returns>二进制的字节数组</returns>
		public static byte[] ReadSpecifiedLengthFromStream( Stream stream, int length )
		{
			byte[] buffer = new byte[length];
			int receive = 0;
			while (receive < length)
			{
				int count = stream.Read( buffer, receive, buffer.Length - receive );
				receive += count;
				if (count == 0) break;
			}
			return buffer;
		}

		/// <summary>
		/// 将字符串的内容写入到流中去
		/// </summary>
		/// <param name="stream">数据流</param>
		/// <param name="value">字符串内容</param>
		public static void WriteStringToStream( Stream stream, string value )
		{
			byte[] buffer = string.IsNullOrEmpty( value ) ? new byte[0] : Encoding.UTF8.GetBytes( value );
			WriteBinaryToStream( stream, buffer );
		}

		/// <summary>
		/// 从流中读取一个字符串内容
		/// </summary>
		/// <param name="stream">数据流</param>
		/// <returns>字符串信息</returns>
		public static string ReadStringFromStream( Stream stream )
		{
			byte[] buffer = ReadBinaryFromStream( stream );
			return Encoding.UTF8.GetString( buffer );
		}

		/// <summary>
		/// 将二进制的内容写入到数据流之中
		/// </summary>
		/// <param name="stream">数据流</param>
		/// <param name="value">原始字节数组</param>
		public static void WriteBinaryToStream( Stream stream, byte[] value )
		{
			stream.Write( BitConverter.GetBytes( value.Length ), 0, 4 );
			stream.Write( value, 0, value.Length );
		}

		/// <summary>
		/// 从流中读取二进制的内容
		/// </summary>
		/// <param name="stream">数据流</param>
		/// <returns>字节数组</returns>
		public static byte[] ReadBinaryFromStream( Stream stream )
		{
			byte[] lengthBuffer = ReadSpecifiedLengthFromStream( stream, 4 );
			int length = BitConverter.ToInt32( lengthBuffer, 0 );
			if (length <= 0) return new byte[0];
			return ReadSpecifiedLengthFromStream( stream, length );
		}

		/// <summary>
		/// 从字符串的内容提取UTF8编码的字节，加了对空的校验
		/// </summary>
		/// <param name="message">字符串内容</param>
		/// <returns>结果</returns>
		public static byte[] GetUTF8Bytes( string message )
		{
			return string.IsNullOrEmpty( message ) ? new byte[0] : Encoding.UTF8.GetBytes( message );
		}

		/// <summary>
		/// 将多个路径合成一个更完整的路径，这个方法是多平台适用的
		/// </summary>
		/// <param name="paths">路径的集合</param>
		/// <returns>总路径信息</returns>
		public static string PathCombine( params string[] paths )
		{
#if NET20 || NET35
			if (paths == null) return string.Empty;
			if (paths.Length == 0) return string.Empty;
			string path = paths[0];
			for (int i = 1; i < paths.Length; i++)
			{
				if (!string.IsNullOrEmpty( paths[i] )) path = Path.Combine( path, paths[i] );
			}
			return path;
#else
			return Path.Combine( paths );
#endif
		}

		/// <summary>
		/// <b>[商业授权]</b> 将原始的字节数组，转换成实际的结构体对象，需要事先定义好结构体内容，否则会转换失败<br />
		/// <b>[Authorization]</b> To convert the original byte array into an actual structure object, 
		/// the structure content needs to be defined in advance, otherwise the conversion will fail
		/// </summary>
		/// <typeparam name="T">自定义的结构体</typeparam>
		/// <param name="content">原始的字节内容</param>
		/// <returns>是否成功的结果对象</returns>
		public static OperateResult<T> ByteArrayToStruct<T>( byte[] content ) where T : struct
		{
			if (!Authorization.asdniasnfaksndiqwhawfskhfaiw( )) return new OperateResult<T>( StringResources.Language.InsufficientPrivileges );

			int size = Marshal.SizeOf( typeof( T ) );
			IntPtr ptr = Marshal.AllocHGlobal( size );

			try
			{
				Marshal.Copy( content, 0, ptr, size );
#if NET20 || NET35
				T obj = (T)Marshal.PtrToStructure( ptr, typeof( T ) );
#else
				T obj = Marshal.PtrToStructure<T>( ptr );
#endif
				Marshal.FreeHGlobal( ptr );
				return OperateResult.CreateSuccessResult( obj );
			}
			catch (Exception ex)
			{
				Marshal.FreeHGlobal( ptr );
				return new OperateResult<T>( ex.Message );
			}
		}

		/// <summary>
		/// 根据当前的位偏移地址及读取位长度信息，计算出实际的字节索引，字节数，字节位偏移
		/// </summary>
		/// <param name="addressStart">起始地址</param>
		/// <param name="length">读取的长度</param>
		/// <param name="newStart">返回的新的字节的索引，仍然按照位单位</param>
		/// <param name="byteLength">字节长度</param>
		/// <param name="offset">当前偏移的信息</param>
		public static void CalculateStartBitIndexAndLength( int addressStart, ushort length, out int newStart, out ushort byteLength, out int offset )
		{
			byteLength = (ushort)((addressStart + length - 1) / 8 - addressStart / 8 + 1);
			offset = addressStart % 8;
			newStart = addressStart - offset;
		}

		/// <summary>
		/// 根据字符串内容，获取当前的位索引地址，例如输入 6,返回6，输入15，返回15，输入B，返回11
		/// </summary>
		/// <param name="bit">位字符串</param>
		/// <returns>结束数据</returns>
		public static int CalculateBitStartIndex( string bit )
		{
			if (bit.Contains( "A" ) || bit.Contains( "B" ) || bit.Contains( "C" ) || bit.Contains( "D" ) || bit.Contains( "E" ) || bit.Contains( "F" ))
			{
				return Convert.ToInt32( bit, 16 );
			}
			else
			{
				return Convert.ToInt32( bit );
			}
		}

		/// <summary>
		/// 将一个一维数组中的所有数据按照行列信息拷贝到二维数组里，返回当前的二维数组
		/// </summary>
		/// <typeparam name="T">数组的类型对象</typeparam>
		/// <param name="array">一维数组信息</param>
		/// <param name="row">行</param>
		/// <param name="col">列</param>
		public static T[,] CreateTwoArrayFromOneArray<T>( T[] array, int row, int col )
		{
			T[,] twoArray = new T[row, col];
			int count = 0;
			for (int i = 0; i < row; i++)
			{
				for (int j = 0; j < col; j++)
				{
					twoArray[i, j] = array[count];
					count++;
				}
			}
			return twoArray;
		}


	}
}
