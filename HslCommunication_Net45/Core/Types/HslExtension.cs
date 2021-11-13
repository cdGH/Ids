using HslCommunication.BasicFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Linq.Expressions;
using Newtonsoft.Json.Linq;
using System.Security.Cryptography;

namespace HslCommunication
{
	/// <summary>
	/// 扩展的辅助类方法
	/// </summary>
	public static class HslExtension
	{
		/// <inheritdoc cref="SoftBasic.ByteToHexString(byte[])"/>
		public static string ToHexString( this byte[] InBytes ) => SoftBasic.ByteToHexString( InBytes );

		/// <inheritdoc cref="SoftBasic.ByteToHexString(byte[], char)"/>
		public static string ToHexString( this byte[] InBytes, char segment ) => SoftBasic.ByteToHexString( InBytes, segment );

		/// <inheritdoc cref="SoftBasic.ByteToHexString(byte[], char, int)"/>
		public static string ToHexString( this byte[] InBytes, char segment, int newLineCount ) => SoftBasic.ByteToHexString( InBytes, segment, newLineCount );

		/// <inheritdoc cref="SoftBasic.HexStringToBytes( string )"/>
		public static byte[] ToHexBytes( this string value ) => SoftBasic.HexStringToBytes( value );

		/// <inheritdoc cref="SoftBasic.BoolArrayToByte"/>
		public static byte[] ToByteArray( this bool[] array ) => SoftBasic.BoolArrayToByte( array );

		/// <inheritdoc cref="SoftBasic.ByteToBoolArray(byte[],int)"/>
		public static bool[] ToBoolArray( this byte[] InBytes, int length ) => SoftBasic.ByteToBoolArray( InBytes, length );

		/// <summary>
		/// 获取当前数组的倒序数组，这是一个新的实例，不改变原来的数组值<br />
		/// Get the reversed array of the current byte array, this is a new instance, does not change the original array value
		/// </summary>
		/// <param name="value">输入的原始数组</param>
		/// <returns>反转之后的数组信息</returns>
		public static T[] ReverseNew<T>( this T[] value )
		{
			T[] buffer = value.CopyArray( );
			Array.Reverse( buffer );
			return buffer;
		}

		/// <inheritdoc cref="SoftBasic.ByteToBoolArray(byte[])"/>
		public static bool[] ToBoolArray( this byte[] InBytes ) => SoftBasic.ByteToBoolArray( InBytes );

		/// <summary>
		/// 获取Byte数组的第 bytIndex 个位置的，boolIndex偏移的bool值<br />
		/// Get the bool value of the bytIndex position of the Byte array and the boolIndex offset
		/// </summary>
		/// <param name="bytes">字节数组信息</param>
		/// <param name="bytIndex">字节的偏移位置</param>
		/// <param name="boolIndex">指定字节的位偏移</param>
		/// <returns>bool值</returns>
		public static bool GetBoolValue( this byte[] bytes, int bytIndex, int boolIndex )
		{
			return SoftBasic.BoolOnByteIndex( bytes[bytIndex], boolIndex );
		}

		/// <summary>
		/// 获取Byte数组的第 boolIndex 偏移的bool值，这个偏移值可以为 10，就是第 1 个字节的 第3位 <br />
		/// Get the bool value of the boolIndex offset of the Byte array. The offset value can be 10, which is the third bit of the first byte
		/// </summary>
		/// <param name="bytes">字节数组信息</param>
		/// <param name="boolIndex">指定字节的位偏移</param>
		/// <returns>bool值</returns>
		public static bool GetBoolByIndex( this byte[] bytes, int boolIndex )
		{
			return SoftBasic.BoolOnByteIndex( bytes[boolIndex / 8], boolIndex % 8 );
		}

		/// <summary>
		/// 获取Byte的第 boolIndex 偏移的bool值，比如3，就是第4位 <br />
		/// Get the bool value of Byte's boolIndex offset, such as 3, which is the 4th bit
		/// </summary>
		/// <param name="byt">字节信息</param>
		/// <param name="boolIndex">指定字节的位偏移</param>
		/// <returns>bool值</returns>
		public static bool GetBoolByIndex( this byte byt, int boolIndex )
		{
			return SoftBasic.BoolOnByteIndex( byt, boolIndex % 8 );
		}

		/// <summary>
		/// 设置Byte的第 boolIndex 位的bool值，可以强制为 true 或是 false, 不影响其他的位<br />
		/// Set the bool value of the boolIndex bit of Byte, which can be forced to true or false, without affecting other bits
		/// </summary>
		/// <param name="byt">字节信息</param>
		/// <param name="boolIndex">指定字节的位偏移</param>
		/// <param name="value">bool的值</param>
		/// <returns>修改之后的byte值</returns>
		public static byte SetBoolByIndex( this byte byt, int boolIndex, bool value )
		{
			return SoftBasic.SetBoolOnByteIndex( byt, boolIndex, value );
		}

		/// <inheritdoc cref="SoftBasic.ArrayRemoveDouble"/>
		public static T[] RemoveDouble<T>( this T[] value, int leftLength, int rightLength ) => SoftBasic.ArrayRemoveDouble( value, leftLength, rightLength );

		/// <inheritdoc cref="SoftBasic.ArrayRemoveBegin"/>
		public static T[] RemoveBegin<T>( this T[] value, int length ) => SoftBasic.ArrayRemoveBegin( value, length );

		/// <inheritdoc cref="SoftBasic.ArrayRemoveLast"/>
		public static T[] RemoveLast<T>( this T[] value, int length ) => SoftBasic.ArrayRemoveLast( value, length );

		/// <inheritdoc cref="SoftBasic.ArraySelectMiddle"/>
		public static T[] SelectMiddle<T>( this T[] value, int index, int length ) => SoftBasic.ArraySelectMiddle( value, index, length );

		/// <inheritdoc cref="SoftBasic.ArraySelectBegin"/>
		public static T[] SelectBegin<T>( this T[] value, int length ) => SoftBasic.ArraySelectBegin( value, length );

		/// <inheritdoc cref="SoftBasic.ArraySelectLast"/>
		public static T[] SelectLast<T>( this T[] value, int length ) => SoftBasic.ArraySelectLast( value, length );

		/// <inheritdoc cref="SoftBasic.GetValueFromJsonObject{T}(JObject, string, T)"/>
		public static T GetValueOrDefault<T>( JObject jObject, string name, T defaultValue ) => SoftBasic.GetValueFromJsonObject<T>( jObject, name, defaultValue );

		/// <inheritdoc cref="SoftBasic.SpliceArray{T}(T[][])"/>
		public static T[] SpliceArray<T>( this T[] value, params T[][] arrays )
		{
			List<T[]> list = new List<T[]>( arrays.Length + 1 );
			list.Add( value );
			list.AddRange( arrays );
			return SoftBasic.SpliceArray( list.ToArray( ) );
		}

		/// <summary>
		/// 移除指定字符串数据的最后 length 个字符。如果字符串本身的长度不足 length，则返回为空字符串。<br />
		/// Remove the last "length" characters of the specified string data. If the length of the string itself is less than length, 
		/// an empty string is returned.
		/// </summary>
		/// <param name="value">等待操作的字符串数据</param>
		/// <param name="length">准备移除的长度信息</param>
		/// <returns>移除之后的数据信息</returns>
		public static string RemoveLast( this string value, int length )
		{
			if (value == null) return null;
			if (value.Length < length) return string.Empty;
			return value.Remove( value.Length - length );
		}

		/// <summary>
		/// 将指定的数据添加到数组的每个元素上去，使用表达式树的形式实现，将会修改原数组。不适用byte类型
		/// </summary>
		/// <typeparam name="T">数组的类型</typeparam>
		/// <param name="array">原始数据</param>
		/// <param name="value">数据值</param>
		/// <returns>返回的结果信息</returns>
		public static T[] IncreaseBy<T>( this T[] array, T value )
		{
			if (typeof( T ) == typeof( byte ))
			{
				ParameterExpression firstArg = Expression.Parameter( typeof( int ), "first" );
				ParameterExpression secondArg = Expression.Parameter( typeof( int ), "second" );
				Expression body = Expression.Add( firstArg, secondArg );

				Expression<Func<int, int, int>> adder = Expression.Lambda<Func<int, int, int>>( body, firstArg, secondArg );
				Func<int, int, int> addDelegate = adder.Compile( );
				for (int i = 0; i < array.Length; i++)
				{
					array[i] = (T)(object)(byte)addDelegate( Convert.ToInt32( array[i] ), Convert.ToInt32( value ) );
				}
			}
			else
			{
				ParameterExpression firstArg = Expression.Parameter( typeof( T ), "first" );
				ParameterExpression secondArg = Expression.Parameter( typeof( T ), "second" );
				Expression body = Expression.Add( firstArg, secondArg );

				Expression<Func<T, T, T>> adder = Expression.Lambda<Func<T, T, T>>( body, firstArg, secondArg );
				Func<T, T, T> addDelegate = adder.Compile( );
				for (int i = 0; i < array.Length; i++)
				{
					array[i] = addDelegate( array[i], value );
				}
			}

			return array;
		}

		/// <summary>
		/// 拷贝当前的实例数组，是基于引用层的浅拷贝，如果类型为值类型，那就是深度拷贝，如果类型为引用类型，就是浅拷贝
		/// </summary>
		/// <typeparam name="T">类型对象</typeparam>
		/// <param name="value">数组对象</param>
		/// <returns>拷贝的结果内容</returns>
		public static T[] CopyArray<T>( this T[] value )
		{
			if (value == null) return null;
			T[] buffer = new T[value.Length];
			Array.Copy( value, buffer, value.Length );
			return buffer;
		}

		/// <inheritdoc cref="SoftBasic.ArrayFormat{T}(T[])"/>
		public static string ToArrayString<T>( this T[] value ) => SoftBasic.ArrayFormat( value );

		/// <inheritdoc cref="SoftBasic.ArrayFormat{T}(T, string)"/>
		public static string ToArrayString<T>( this T[] value, string format ) => SoftBasic.ArrayFormat( value, format );

		/// <summary>
		/// 将字符串数组转换为实际的数据数组。例如字符串格式[1,2,3,4,5]，可以转成实际的数组对象<br />
		/// Converts a string array into an actual data array. For example, the string format [1,2,3,4,5] can be converted into an actual array object
		/// </summary>
		/// <typeparam name="T">类型对象</typeparam>
		/// <param name="value">字符串数据</param>
		/// <param name="selector">转换方法</param>
		/// <returns>实际的数组</returns>
		public static T[] ToStringArray<T>( this string value, Func<string, T> selector )
		{
			if (value.IndexOf( '[' ) >= 0) value = value.Replace( "[", "" );
			if (value.IndexOf( ']' ) >= 0) value = value.Replace( "]", "" );

			string[] splits = value.Split( new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries );
			return splits.Select( selector ).ToArray( );
		}

		/// <summary>
		/// 将字符串数组转换为实际的数据数组。支持byte,sbyte,bool,short,ushort,int,uint,long,ulong,float,double，使用默认的十进制，例如字符串格式[1,2,3,4,5]，可以转成实际的数组对象<br />
		/// Converts a string array into an actual data array. Support byte, sbyte, bool, short, ushort, int, uint, long, ulong, float, double, use the default decimal, 
		/// such as the string format [1,2,3,4,5], which can be converted into an actual array Object
		/// </summary>
		/// <typeparam name="T">类型对象</typeparam>
		/// <param name="value">字符串数据</param>
		/// <returns>实际的数组</returns>
		public static T[] ToStringArray<T>( this string value )
		{
			Type type = typeof( T );
			if (type == typeof( byte )) return (T[])(object)value.ToStringArray( byte.Parse );
			else if (type == typeof( sbyte )) return (T[])(object)value.ToStringArray( sbyte.Parse );
			else if (type == typeof( bool )) return (T[])(object)value.ToStringArray( bool.Parse );
			else if (type == typeof( short )) return (T[])(object)value.ToStringArray( short.Parse );
			else if (type == typeof( ushort )) return (T[])(object)value.ToStringArray( ushort.Parse );
			else if (type == typeof( int )) return (T[])(object)value.ToStringArray( int.Parse );
			else if (type == typeof( uint )) return (T[])(object)value.ToStringArray( uint.Parse );
			else if (type == typeof( long )) return (T[])(object)value.ToStringArray( long.Parse );
			else if (type == typeof( ulong )) return (T[])(object)value.ToStringArray( ulong.Parse );
			else if (type == typeof( float )) return (T[])(object)value.ToStringArray( float.Parse );
			else if (type == typeof( double )) return (T[])(object)value.ToStringArray( double.Parse );
			else if (type == typeof( DateTime )) return (T[])(object)value.ToStringArray( DateTime.Parse );
#if !NET20 && !NET35
			else if (type == typeof( Guid )) return (T[])(object)value.ToStringArray( Guid.Parse );
#endif
			else if (type == typeof( string )) return (T[])(object)value.ToStringArray( m => m );
			else throw new Exception( "use ToArray<T>(Func<string,T>) method instead" );
		}

		/// <summary>
		/// 启动接收数据，需要传入回调方法，传递对象<br />
		/// To start receiving data, you need to pass in a callback method and pass an object
		/// </summary>
		/// <param name="socket">socket对象</param>
		/// <param name="callback">回调方法</param>
		/// <param name="obj">数据对象</param>
		/// <returns>是否启动成功</returns>
		public static OperateResult BeginReceiveResult( this Socket socket, AsyncCallback callback, object obj )
		{
			try
			{
				socket.BeginReceive( new byte[0], 0, 0, SocketFlags.None, callback, obj );
				return OperateResult.CreateSuccessResult( );
			}
			catch (Exception ex)
			{
				socket?.Close( );
				return new OperateResult( ex.Message );
			}
		}

		/// <summary>
		/// 启动接收数据，需要传入回调方法，传递对象默认为socket本身<br />
		/// To start receiving data, you need to pass in a callback method. The default object is the socket itself.
		/// </summary>
		/// <param name="socket">socket对象</param>
		/// <param name="callback">回调方法</param>
		/// <returns>是否启动成功</returns>
		public static OperateResult BeginReceiveResult( this Socket socket, AsyncCallback callback )
		{
			return BeginReceiveResult( socket, callback, socket );
		}

		/// <summary>
		/// 结束挂起的异步读取，返回读取的字节数，如果成功的情况。<br />
		/// Ends the pending asynchronous read and returns the number of bytes read, if successful.
		/// </summary>
		/// <param name="socket">socket对象</param>
		/// <param name="ar">回调方法</param>
		/// <returns>是否启动成功</returns>
		public static OperateResult<int> EndReceiveResult( this Socket socket, IAsyncResult ar )
		{
			try
			{
				return OperateResult.CreateSuccessResult( socket.EndReceive( ar ) );
			}
			catch (Exception ex)
			{
				socket?.Close( );
				return new OperateResult<int>( ex.Message );
			}
		}

		/// <summary>
		/// 根据英文小数点进行切割字符串，去除空白的字符<br />
		/// Cut the string according to the English decimal point and remove the blank characters
		/// </summary>
		/// <param name="str">字符串本身</param>
		/// <returns>切割好的字符串数组，例如输入 "100.5"，返回 "100", "5"</returns>
		public static string[] SplitDot( this string str ) => str.Split( new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries );

		/// <summary>
		/// 获取当前对象的JSON格式表示的字符串。<br />
		/// Gets the string represented by the JSON format of the current object.
		/// </summary>
		/// <returns>字符串对象</returns>
		public static string ToJsonString( this object obj, Newtonsoft.Json.Formatting formatting = Newtonsoft.Json.Formatting.Indented ) => Newtonsoft.Json.JsonConvert.SerializeObject( obj, formatting );

		/// <inheritdoc cref="HslCommunication.Core.Security.RSAHelper.GetPrivateKeyFromRSA"/>
		public static byte[] GetPEMPrivateKey( this RSACryptoServiceProvider rsa )
		{
			return HslCommunication.Core.Security.RSAHelper.GetPrivateKeyFromRSA( rsa );
		}

		/// <inheritdoc cref="HslCommunication.Core.Security.RSAHelper.GetPublicKeyFromRSA"/>
		public static byte[] GetPEMPublicKey( this RSACryptoServiceProvider rsa )
		{
			return HslCommunication.Core.Security.RSAHelper.GetPublicKeyFromRSA( rsa );
		}

		/// <inheritdoc cref="HslCommunication.Core.Security.RSAHelper.EncryptLargeDataByRSA(RSACryptoServiceProvider, byte[])"/>
		public static byte[] EncryptLargeData( this RSACryptoServiceProvider rsa, byte[] data )
		{
			return HslCommunication.Core.Security.RSAHelper.EncryptLargeDataByRSA( rsa, data );
		}

		/// <inheritdoc cref="HslCommunication.Core.Security.RSAHelper.DecryptLargeDataByRSA(RSACryptoServiceProvider, byte[])"/>
		public static byte[] DecryptLargeData( this RSACryptoServiceProvider rsa, byte[] data )
		{
			return HslCommunication.Core.Security.RSAHelper.DecryptLargeDataByRSA( rsa, data );
		}
	}
}
