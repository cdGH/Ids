using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Reflection;
using HslCommunication.Enthernet.Redis;
using HslCommunication.Core;
using Newtonsoft.Json.Linq;
using HslCommunication.MQTT;
#if !NET35 && !NET20
using System.Threading.Tasks;
#endif

namespace HslCommunication.Reflection
{
	/// <summary>
	/// 反射的辅助类
	/// </summary>
	public class HslReflectionHelper
	{
		/// <summary>
		/// 从设备里读取支持Hsl特性的数据内容，该特性为<see cref="HslDeviceAddressAttribute"/>，详细参考论坛的操作说明。
		/// </summary>
		/// <typeparam name="T">自定义的数据类型对象</typeparam>
		/// <param name="readWrite">读写接口的实现</param>
		/// <returns>包含是否成功的结果对象</returns>
		public static OperateResult<T> Read<T>( IReadWriteNet readWrite ) where T : class, new()
		{
			var type = typeof( T );
			// var constrcuor = type.GetConstructors( System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic );
			var obj = type.Assembly.CreateInstance( type.FullName );

			var properties = type.GetProperties( System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public );
			foreach (var property in properties)
			{
				var attribute = property.GetCustomAttributes( typeof( HslDeviceAddressAttribute ), false );
				if (attribute == null) continue;

				HslDeviceAddressAttribute hslAttribute = null;
				for (int i = 0; i < attribute.Length; i++)
				{
					HslDeviceAddressAttribute tmp = (HslDeviceAddressAttribute)attribute[i];
					if (tmp.DeviceType != null && tmp.DeviceType == readWrite.GetType( ))
					{
						hslAttribute = tmp;
						break;
					}
				}

				if (hslAttribute == null)
				{
					for (int i = 0; i < attribute.Length; i++)
					{
						HslDeviceAddressAttribute tmp = (HslDeviceAddressAttribute)attribute[i];
						if (tmp.DeviceType == null)
						{
							hslAttribute = tmp;
							break;
						}
					}
				}

				if (hslAttribute == null) continue;

				Type propertyType = property.PropertyType;
				if (propertyType == typeof( byte ))
				{
					MethodInfo readByteMethod = readWrite.GetType( ).GetMethod( "ReadByte", new Type[] { typeof( string ) } );
					if (readByteMethod == null) return new OperateResult<T>( $"{readWrite.GetType( ).Name} not support read byte value. " );

					OperateResult<byte> valueResult = (OperateResult<byte>)readByteMethod.Invoke( readWrite, new object[] { hslAttribute.Address } );
					if (!valueResult.IsSuccess) return OperateResult.CreateFailedResult<T>( valueResult );

					property.SetValue( obj, valueResult.Content, null );
				}
				else if (propertyType == typeof( short ))
				{
					OperateResult<short> valueResult = readWrite.ReadInt16( hslAttribute.Address );
					if (!valueResult.IsSuccess) return OperateResult.CreateFailedResult<T>( valueResult );

					property.SetValue( obj, valueResult.Content, null );
				}
				else if (propertyType == typeof( short[] ))
				{
					OperateResult<short[]> valueResult = readWrite.ReadInt16( hslAttribute.Address, (ushort)(hslAttribute.Length > 0 ? hslAttribute.Length : 1) );
					if (!valueResult.IsSuccess) return OperateResult.CreateFailedResult<T>( valueResult );

					property.SetValue( obj, valueResult.Content, null );
				}
				else if (propertyType == typeof( ushort ))
				{
					OperateResult<ushort> valueResult = readWrite.ReadUInt16( hslAttribute.Address );
					if (!valueResult.IsSuccess) return OperateResult.CreateFailedResult<T>( valueResult );

					property.SetValue( obj, valueResult.Content, null );
				}
				else if (propertyType == typeof( ushort[] ))
				{
					OperateResult<ushort[]> valueResult = readWrite.ReadUInt16( hslAttribute.Address, (ushort)(hslAttribute.Length > 0 ? hslAttribute.Length : 1) );
					if (!valueResult.IsSuccess) return OperateResult.CreateFailedResult<T>( valueResult );

					property.SetValue( obj, valueResult.Content, null );
				}
				else if (propertyType == typeof( int ))
				{
					OperateResult<int> valueResult = readWrite.ReadInt32( hslAttribute.Address );
					if (!valueResult.IsSuccess) return OperateResult.CreateFailedResult<T>( valueResult );

					property.SetValue( obj, valueResult.Content, null );
				}
				else if (propertyType == typeof( int[] ))
				{
					OperateResult<int[]> valueResult = readWrite.ReadInt32( hslAttribute.Address, (ushort)(hslAttribute.Length > 0 ? hslAttribute.Length : 1) );
					if (!valueResult.IsSuccess) return OperateResult.CreateFailedResult<T>( valueResult );

					property.SetValue( obj, valueResult.Content, null );
				}
				else if (propertyType == typeof( uint ))
				{
					OperateResult<uint> valueResult = readWrite.ReadUInt32( hslAttribute.Address );
					if (!valueResult.IsSuccess) return OperateResult.CreateFailedResult<T>( valueResult );

					property.SetValue( obj, valueResult.Content, null );
				}
				else if (propertyType == typeof( uint[] ))
				{
					OperateResult<uint[]> valueResult = readWrite.ReadUInt32( hslAttribute.Address, (ushort)(hslAttribute.Length > 0 ? hslAttribute.Length : 1) );
					if (!valueResult.IsSuccess) return OperateResult.CreateFailedResult<T>( valueResult );

					property.SetValue( obj, valueResult.Content, null );
				}
				else if (propertyType == typeof( long ))
				{
					OperateResult<long> valueResult = readWrite.ReadInt64( hslAttribute.Address );
					if (!valueResult.IsSuccess) return OperateResult.CreateFailedResult<T>( valueResult );

					property.SetValue( obj, valueResult.Content, null );
				}
				else if (propertyType == typeof( long[] ))
				{
					OperateResult<long[]> valueResult = readWrite.ReadInt64( hslAttribute.Address, (ushort)(hslAttribute.Length > 0 ? hslAttribute.Length : 1) );
					if (!valueResult.IsSuccess) return OperateResult.CreateFailedResult<T>( valueResult );

					property.SetValue( obj, valueResult.Content, null );
				}
				else if (propertyType == typeof( ulong ))
				{
					OperateResult<ulong> valueResult = readWrite.ReadUInt64( hslAttribute.Address );
					if (!valueResult.IsSuccess) return OperateResult.CreateFailedResult<T>( valueResult );

					property.SetValue( obj, valueResult.Content, null );
				}
				else if (propertyType == typeof( ulong[] ))
				{
					OperateResult<ulong[]> valueResult = readWrite.ReadUInt64( hslAttribute.Address, (ushort)(hslAttribute.Length > 0 ? hslAttribute.Length : 1) );
					if (!valueResult.IsSuccess) return OperateResult.CreateFailedResult<T>( valueResult );

					property.SetValue( obj, valueResult.Content, null );
				}
				else if (propertyType == typeof( float ))
				{
					OperateResult<float> valueResult = readWrite.ReadFloat( hslAttribute.Address );
					if (!valueResult.IsSuccess) return OperateResult.CreateFailedResult<T>( valueResult );

					property.SetValue( obj, valueResult.Content, null );
				}
				else if (propertyType == typeof( float[] ))
				{
					OperateResult<float[]> valueResult = readWrite.ReadFloat( hslAttribute.Address, (ushort)(hslAttribute.Length > 0 ? hslAttribute.Length : 1) );
					if (!valueResult.IsSuccess) return OperateResult.CreateFailedResult<T>( valueResult );

					property.SetValue( obj, valueResult.Content, null );
				}
				else if (propertyType == typeof( double ))
				{
					OperateResult<double> valueResult = readWrite.ReadDouble( hslAttribute.Address );
					if (!valueResult.IsSuccess) return OperateResult.CreateFailedResult<T>( valueResult );

					property.SetValue( obj, valueResult.Content, null );
				}
				else if (propertyType == typeof( double[] ))
				{
					OperateResult<double[]> valueResult = readWrite.ReadDouble( hslAttribute.Address, (ushort)(hslAttribute.Length > 0 ? hslAttribute.Length : 1) );
					if (!valueResult.IsSuccess) return OperateResult.CreateFailedResult<T>( valueResult );

					property.SetValue( obj, valueResult.Content, null );
				}
				else if (propertyType == typeof( string ))
				{
					OperateResult<string> valueResult = readWrite.ReadString( hslAttribute.Address, (ushort)(hslAttribute.Length > 0 ? hslAttribute.Length : 1) );
					if (!valueResult.IsSuccess) return OperateResult.CreateFailedResult<T>( valueResult );

					property.SetValue( obj, valueResult.Content, null );
				}
				else if (propertyType == typeof( byte[] ))
				{
					OperateResult<byte[]> valueResult = readWrite.Read( hslAttribute.Address, (ushort)(hslAttribute.Length > 0 ? hslAttribute.Length : 1) );
					if (!valueResult.IsSuccess) return OperateResult.CreateFailedResult<T>( valueResult );

					property.SetValue( obj, valueResult.Content, null );
				}
				else if (propertyType == typeof( bool ))
				{
					OperateResult<bool> valueResult = readWrite.ReadBool( hslAttribute.Address );
					if (!valueResult.IsSuccess) return OperateResult.CreateFailedResult<T>( valueResult );

					property.SetValue( obj, valueResult.Content, null );
				}
				else if (propertyType == typeof( bool[] ))
				{
					OperateResult<bool[]> valueResult = readWrite.ReadBool( hslAttribute.Address, (ushort)(hslAttribute.Length > 0 ? hslAttribute.Length : 1) );
					if (!valueResult.IsSuccess) return OperateResult.CreateFailedResult<T>( valueResult );

					property.SetValue( obj, valueResult.Content, null );
				}
			}

			return OperateResult.CreateSuccessResult( (T)obj );
		}

		/// <summary>
		/// 从设备里读取支持Hsl特性的数据内容，该特性为<see cref="HslDeviceAddressAttribute"/>，详细参考论坛的操作说明。
		/// </summary>
		/// <typeparam name="T">自定义的数据类型对象</typeparam>
		/// <param name="data">自定义的数据对象</param>
		/// <param name="readWrite">数据读写对象</param>
		/// <returns>包含是否成功的结果对象</returns>
		/// <exception cref="ArgumentNullException"></exception>
		public static OperateResult Write<T>( T data, IReadWriteNet readWrite ) where T : class, new()
		{
			if (data == null) throw new ArgumentNullException( nameof( data ) );

			var type = typeof( T );
			var obj = data;

			var properties = type.GetProperties( System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public );
			foreach (var property in properties)
			{
				var attribute = property.GetCustomAttributes( typeof( HslDeviceAddressAttribute ), false );
				if (attribute == null) continue;

				HslDeviceAddressAttribute hslAttribute = null;
				for (int i = 0; i < attribute.Length; i++)
				{
					HslDeviceAddressAttribute tmp = (HslDeviceAddressAttribute)attribute[i];
					if (tmp.DeviceType != null && tmp.DeviceType == readWrite.GetType( ))
					{
						hslAttribute = tmp;
						break;
					}
				}

				if (hslAttribute == null)
				{
					for (int i = 0; i < attribute.Length; i++)
					{
						HslDeviceAddressAttribute tmp = (HslDeviceAddressAttribute)attribute[i];
						if (tmp.DeviceType == null)
						{
							hslAttribute = tmp;
							break;
						}
					}
				}

				if (hslAttribute == null) continue;


				Type propertyType = property.PropertyType;
				if (propertyType == typeof( byte ))
				{
					MethodInfo method = readWrite.GetType( ).GetMethod( "Write", new Type[] { typeof( string ), typeof(byte) } );
					if (method == null) return new OperateResult<T>( $"{readWrite.GetType( ).Name} not support write byte value. " );

					byte value = (byte)property.GetValue( obj, null );

					OperateResult valueResult = (OperateResult)method.Invoke( readWrite, new object[] { hslAttribute.Address, value } );
					if (!valueResult.IsSuccess) return OperateResult.CreateFailedResult<T>( valueResult );
				}
				else if (propertyType == typeof( short ))
				{
					short value = (short)property.GetValue( obj, null );

					OperateResult writeResult = readWrite.Write( hslAttribute.Address, value );
					if (!writeResult.IsSuccess) return writeResult;
				}
				else if (propertyType == typeof( short[] ))
				{
					short[] value = (short[])property.GetValue( obj, null );

					OperateResult writeResult = readWrite.Write( hslAttribute.Address, value );
					if (!writeResult.IsSuccess) return writeResult;
				}
				else if (propertyType == typeof( ushort ))
				{
					ushort value = (ushort)property.GetValue( obj, null );

					OperateResult writeResult = readWrite.Write( hslAttribute.Address, value );
					if (!writeResult.IsSuccess) return writeResult;
				}
				else if (propertyType == typeof( ushort[] ))
				{
					ushort[] value = (ushort[])property.GetValue( obj, null );

					OperateResult writeResult = readWrite.Write( hslAttribute.Address, value );
					if (!writeResult.IsSuccess) return writeResult;
				}
				else if (propertyType == typeof( int ))
				{
					int value = (int)property.GetValue( obj, null );

					OperateResult writeResult = readWrite.Write( hslAttribute.Address, value );
					if (!writeResult.IsSuccess) return writeResult;
				}
				else if (propertyType == typeof( int[] ))
				{
					int[] value = (int[])property.GetValue( obj, null );

					OperateResult writeResult = readWrite.Write( hslAttribute.Address, value );
					if (!writeResult.IsSuccess) return writeResult;
				}
				else if (propertyType == typeof( uint ))
				{
					uint value = (uint)property.GetValue( obj, null );

					OperateResult writeResult = readWrite.Write( hslAttribute.Address, value );
					if (!writeResult.IsSuccess) return writeResult;
				}
				else if (propertyType == typeof( uint[] ))
				{
					uint[] value = (uint[])property.GetValue( obj, null );

					OperateResult writeResult = readWrite.Write( hslAttribute.Address, value );
					if (!writeResult.IsSuccess) return writeResult;
				}
				else if (propertyType == typeof( long ))
				{
					long value = (long)property.GetValue( obj, null );

					OperateResult writeResult = readWrite.Write( hslAttribute.Address, value );
					if (!writeResult.IsSuccess) return writeResult;
				}
				else if (propertyType == typeof( long[] ))
				{
					long[] value = (long[])property.GetValue( obj, null );

					OperateResult writeResult = readWrite.Write( hslAttribute.Address, value );
					if (!writeResult.IsSuccess) return writeResult;
				}
				else if (propertyType == typeof( ulong ))
				{
					ulong value = (ulong)property.GetValue( obj, null );

					OperateResult writeResult = readWrite.Write( hslAttribute.Address, value );
					if (!writeResult.IsSuccess) return writeResult;
				}
				else if (propertyType == typeof( ulong[] ))
				{
					ulong[] value = (ulong[])property.GetValue( obj, null );

					OperateResult writeResult = readWrite.Write( hslAttribute.Address, value );
					if (!writeResult.IsSuccess) return writeResult;
				}
				else if (propertyType == typeof( float ))
				{
					float value = (float)property.GetValue( obj, null );

					OperateResult writeResult = readWrite.Write( hslAttribute.Address, value );
					if (!writeResult.IsSuccess) return writeResult;
				}
				else if (propertyType == typeof( float[] ))
				{
					float[] value = (float[])property.GetValue( obj, null );

					OperateResult writeResult = readWrite.Write( hslAttribute.Address, value );
					if (!writeResult.IsSuccess) return writeResult;
				}
				else if (propertyType == typeof( double ))
				{
					double value = (double)property.GetValue( obj, null );

					OperateResult writeResult = readWrite.Write( hslAttribute.Address, value );
					if (!writeResult.IsSuccess) return writeResult;
				}
				else if (propertyType == typeof( double[] ))
				{
					double[] value = (double[])property.GetValue( obj, null );

					OperateResult writeResult = readWrite.Write( hslAttribute.Address, value );
					if (!writeResult.IsSuccess) return writeResult;
				}
				else if (propertyType == typeof( string ))
				{
					string value = (string)property.GetValue( obj, null );

					OperateResult writeResult = readWrite.Write( hslAttribute.Address, value );
					if (!writeResult.IsSuccess) return writeResult;
				}
				else if (propertyType == typeof( byte[] ))
				{
					byte[] value = (byte[])property.GetValue( obj, null );

					OperateResult writeResult = readWrite.Write( hslAttribute.Address, value );
					if (!writeResult.IsSuccess) return writeResult;
				}
				else if (propertyType == typeof( bool ))
				{
					bool value = (bool)property.GetValue( obj, null );

					OperateResult writeResult = readWrite.Write( hslAttribute.Address, value );
					if (!writeResult.IsSuccess) return writeResult;
				}
				else if (propertyType == typeof( bool[] ))
				{
					bool[] value = (bool[])property.GetValue( obj, null );

					OperateResult writeResult = readWrite.Write( hslAttribute.Address, value );
					if (!writeResult.IsSuccess) return writeResult;
				}
			}

			return OperateResult.CreateSuccessResult( (T)obj );
		}

		/// <summary>
		/// 使用表达式树的方式来给一个属性赋值
		/// </summary>
		/// <param name="propertyInfo">属性信息</param>
		/// <param name="obj">对象信息</param>
		/// <param name="objValue">实际的值</param>
		public static void SetPropertyExp<T,K>(PropertyInfo propertyInfo, T obj, K objValue )
		{
			// propertyInfo.SetValue( obj, objValue, null );  下面就是实现这句话
			var invokeObjExpr = Expression.Parameter( typeof( T ), "obj" );
			var propValExpr = Expression.Parameter( propertyInfo.PropertyType, "objValue" );
			var setMethodExp = Expression.Call( invokeObjExpr, propertyInfo.GetSetMethod( ), propValExpr );
			var lambda = Expression.Lambda<Action<T,K>>( setMethodExp, invokeObjExpr, propValExpr );
			lambda.Compile( )( obj, objValue );
		}

#if !NET35 && !NET20

		/// <summary>
		/// 从设备里读取支持Hsl特性的数据内容，该特性为<see cref="HslDeviceAddressAttribute"/>，详细参考论坛的操作说明。
		/// </summary>
		/// <typeparam name="T">自定义的数据类型对象</typeparam>
		/// <param name="readWrite">读写接口的实现</param>
		/// <returns>包含是否成功的结果对象</returns>
		public static async Task<OperateResult<T>> ReadAsync<T>( IReadWriteNet readWrite ) where T : class, new()
		{
			var type = typeof( T );
			// var constrcuor = type.GetConstructors( System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic );
			var obj = type.Assembly.CreateInstance( type.FullName );

			var properties = type.GetProperties( System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public );
			foreach (var property in properties)
			{
				var attribute = property.GetCustomAttributes( typeof( HslDeviceAddressAttribute ), false );
				if (attribute == null) continue;

				HslDeviceAddressAttribute hslAttribute = null;
				for (int i = 0; i < attribute.Length; i++)
				{
					HslDeviceAddressAttribute tmp = (HslDeviceAddressAttribute)attribute[i];
					if (tmp.DeviceType != null && tmp.DeviceType == readWrite.GetType( ))
					{
						hslAttribute = tmp;
						break;
					}
				}

				if (hslAttribute == null)
				{
					for (int i = 0; i < attribute.Length; i++)
					{
						HslDeviceAddressAttribute tmp = (HslDeviceAddressAttribute)attribute[i];
						if (tmp.DeviceType == null)
						{
							hslAttribute = tmp;
							break;
						}
					}
				}

				if (hslAttribute == null) continue;

				Type propertyType = property.PropertyType;
				if (propertyType == typeof( byte ))
				{
					MethodInfo readByteMethod = readWrite.GetType( ).GetMethod( "ReadByteAsync", new Type[] { typeof( string ) } );
					if (readByteMethod == null) return new OperateResult<T>( $"{readWrite.GetType( ).Name} not support read byte value. " );

					Task readByteTask = readByteMethod.Invoke( readWrite, new object[] { hslAttribute.Address } ) as Task;
					if (readByteTask == null) return new OperateResult<T>( $"{readWrite.GetType( ).Name} not task type result. " );

					await readByteTask;
					OperateResult<byte> valueResult = readByteTask.GetType( ).GetProperty( "Result" ).GetValue( readByteTask, null ) as OperateResult<byte>;
					if (!valueResult.IsSuccess) return OperateResult.CreateFailedResult<T>( valueResult );

					property.SetValue( obj, valueResult.Content, null );
				}
				else if (propertyType == typeof( short ))
				{
					OperateResult<short> valueResult = await readWrite.ReadInt16Async( hslAttribute.Address );
					if (!valueResult.IsSuccess) return OperateResult.CreateFailedResult<T>( valueResult );

					property.SetValue( obj, valueResult.Content, null );
				}
				else if (propertyType == typeof( short[] ))
				{
					OperateResult<short[]> valueResult = await readWrite.ReadInt16Async( hslAttribute.Address, (ushort)(hslAttribute.Length > 0 ? hslAttribute.Length : 1) );
					if (!valueResult.IsSuccess) return OperateResult.CreateFailedResult<T>( valueResult );

					property.SetValue( obj, valueResult.Content, null );
				}
				else if (propertyType == typeof( ushort ))
				{
					OperateResult<ushort> valueResult = await readWrite.ReadUInt16Async( hslAttribute.Address );
					if (!valueResult.IsSuccess) return OperateResult.CreateFailedResult<T>( valueResult );

					property.SetValue( obj, valueResult.Content, null );
				}
				else if (propertyType == typeof( ushort[] ))
				{
					OperateResult<ushort[]> valueResult = await readWrite.ReadUInt16Async( hslAttribute.Address, (ushort)(hslAttribute.Length > 0 ? hslAttribute.Length : 1) );
					if (!valueResult.IsSuccess) return OperateResult.CreateFailedResult<T>( valueResult );

					property.SetValue( obj, valueResult.Content, null );
				}
				else if (propertyType == typeof( int ))
				{
					OperateResult<int> valueResult = await readWrite.ReadInt32Async( hslAttribute.Address );
					if (!valueResult.IsSuccess) return OperateResult.CreateFailedResult<T>( valueResult );

					property.SetValue( obj, valueResult.Content, null );
				}
				else if (propertyType == typeof( int[] ))
				{
					OperateResult<int[]> valueResult = await readWrite.ReadInt32Async( hslAttribute.Address, (ushort)(hslAttribute.Length > 0 ? hslAttribute.Length : 1) );
					if (!valueResult.IsSuccess) return OperateResult.CreateFailedResult<T>( valueResult );

					property.SetValue( obj, valueResult.Content, null );
				}
				else if (propertyType == typeof( uint ))
				{
					OperateResult<uint> valueResult = await readWrite.ReadUInt32Async( hslAttribute.Address );
					if (!valueResult.IsSuccess) return OperateResult.CreateFailedResult<T>( valueResult );

					property.SetValue( obj, valueResult.Content, null );
				}
				else if (propertyType == typeof( uint[] ))
				{
					OperateResult<uint[]> valueResult = await readWrite.ReadUInt32Async( hslAttribute.Address, (ushort)(hslAttribute.Length > 0 ? hslAttribute.Length : 1) );
					if (!valueResult.IsSuccess) return OperateResult.CreateFailedResult<T>( valueResult );

					property.SetValue( obj, valueResult.Content, null );
				}
				else if (propertyType == typeof( long ))
				{
					OperateResult<long> valueResult = await readWrite.ReadInt64Async( hslAttribute.Address );
					if (!valueResult.IsSuccess) return OperateResult.CreateFailedResult<T>( valueResult );

					property.SetValue( obj, valueResult.Content, null );
				}
				else if (propertyType == typeof( long[] ))
				{
					OperateResult<long[]> valueResult = await readWrite.ReadInt64Async( hslAttribute.Address, (ushort)(hslAttribute.Length > 0 ? hslAttribute.Length : 1) );
					if (!valueResult.IsSuccess) return OperateResult.CreateFailedResult<T>( valueResult );

					property.SetValue( obj, valueResult.Content, null );
				}
				else if (propertyType == typeof( ulong ))
				{
					OperateResult<ulong> valueResult = await readWrite.ReadUInt64Async( hslAttribute.Address );
					if (!valueResult.IsSuccess) return OperateResult.CreateFailedResult<T>( valueResult );

					property.SetValue( obj, valueResult.Content, null );
				}
				else if (propertyType == typeof( ulong[] ))
				{
					OperateResult<ulong[]> valueResult = await readWrite.ReadUInt64Async( hslAttribute.Address, (ushort)(hslAttribute.Length > 0 ? hslAttribute.Length : 1) );
					if (!valueResult.IsSuccess) return OperateResult.CreateFailedResult<T>( valueResult );

					property.SetValue( obj, valueResult.Content, null );
				}
				else if (propertyType == typeof( float ))
				{
					OperateResult<float> valueResult = await readWrite.ReadFloatAsync( hslAttribute.Address );
					if (!valueResult.IsSuccess) return OperateResult.CreateFailedResult<T>( valueResult );

					property.SetValue( obj, valueResult.Content, null );
				}
				else if (propertyType == typeof( float[] ))
				{
					OperateResult<float[]> valueResult = await readWrite.ReadFloatAsync( hslAttribute.Address, (ushort)(hslAttribute.Length > 0 ? hslAttribute.Length : 1) );
					if (!valueResult.IsSuccess) return OperateResult.CreateFailedResult<T>( valueResult );

					property.SetValue( obj, valueResult.Content, null );
				}
				else if (propertyType == typeof( double ))
				{
					OperateResult<double> valueResult = await readWrite.ReadDoubleAsync( hslAttribute.Address );
					if (!valueResult.IsSuccess) return OperateResult.CreateFailedResult<T>( valueResult );

					property.SetValue( obj, valueResult.Content, null );
				}
				else if (propertyType == typeof( double[] ))
				{
					OperateResult<double[]> valueResult = await readWrite.ReadDoubleAsync( hslAttribute.Address, (ushort)(hslAttribute.Length > 0 ? hslAttribute.Length : 1) );
					if (!valueResult.IsSuccess) return OperateResult.CreateFailedResult<T>( valueResult );

					property.SetValue( obj, valueResult.Content, null );
				}
				else if (propertyType == typeof( string ))
				{
					OperateResult<string> valueResult = await readWrite.ReadStringAsync( hslAttribute.Address, (ushort)(hslAttribute.Length > 0 ? hslAttribute.Length : 1) );
					if (!valueResult.IsSuccess) return OperateResult.CreateFailedResult<T>( valueResult );

					property.SetValue( obj, valueResult.Content, null );
				}
				else if (propertyType == typeof( byte[] ))
				{
					OperateResult<byte[]> valueResult = await readWrite.ReadAsync( hslAttribute.Address, (ushort)(hslAttribute.Length > 0 ? hslAttribute.Length : 1) );
					if (!valueResult.IsSuccess) return OperateResult.CreateFailedResult<T>( valueResult );

					property.SetValue( obj, valueResult.Content, null );
				}
				else if (propertyType == typeof( bool ))
				{
					OperateResult<bool> valueResult = await readWrite.ReadBoolAsync( hslAttribute.Address );
					if (!valueResult.IsSuccess) return OperateResult.CreateFailedResult<T>( valueResult );

					property.SetValue( obj, valueResult.Content, null );
				}
				else if (propertyType == typeof( bool[] ))
				{
					OperateResult<bool[]> valueResult = await readWrite.ReadBoolAsync( hslAttribute.Address, (ushort)(hslAttribute.Length > 0 ? hslAttribute.Length : 1) );
					if (!valueResult.IsSuccess) return OperateResult.CreateFailedResult<T>( valueResult );

					property.SetValue( obj, valueResult.Content, null );
				}
			}

			return OperateResult.CreateSuccessResult( (T)obj );
		}

		/// <summary>
		/// 从设备里读取支持Hsl特性的数据内容，该特性为<see cref="HslDeviceAddressAttribute"/>，详细参考论坛的操作说明。
		/// </summary>
		/// <typeparam name="T">自定义的数据类型对象</typeparam>
		/// <param name="data">自定义的数据对象</param>
		/// <param name="readWrite">数据读写对象</param>
		/// <returns>包含是否成功的结果对象</returns>
		/// <exception cref="ArgumentNullException"></exception>
		public static async Task<OperateResult> WriteAsync<T>( T data, IReadWriteNet readWrite ) where T : class, new()
		{
			if (data == null) throw new ArgumentNullException( nameof( data ) );

			var type = typeof( T );
			var obj = data;

			var properties = type.GetProperties( BindingFlags.Instance | BindingFlags.Public );
			foreach (var property in properties)
			{
				var attribute = property.GetCustomAttributes( typeof( HslDeviceAddressAttribute ), false );
				if (attribute == null) continue;

				HslDeviceAddressAttribute hslAttribute = null;
				for (int i = 0; i < attribute.Length; i++)
				{
					HslDeviceAddressAttribute tmp = (HslDeviceAddressAttribute)attribute[i];
					if (tmp.DeviceType != null && tmp.DeviceType == readWrite.GetType( ))
					{
						hslAttribute = tmp;
						break;
					}
				}

				if (hslAttribute == null)
				{
					for (int i = 0; i < attribute.Length; i++)
					{
						HslDeviceAddressAttribute tmp = (HslDeviceAddressAttribute)attribute[i];
						if (tmp.DeviceType == null)
						{
							hslAttribute = tmp;
							break;
						}
					}
				}

				if (hslAttribute == null) continue;


				Type propertyType = property.PropertyType;
				if (propertyType == typeof( byte ))
				{
					MethodInfo method = readWrite.GetType( ).GetMethod( "WriteAsync", new Type[] { typeof( string ), typeof( byte ) } );
					if (method == null) return new OperateResult<T>( $"{readWrite.GetType( ).Name} not support write byte value. " );

					byte value = (byte)property.GetValue( obj, null );

					Task writeTask = method.Invoke( readWrite, new object[] { hslAttribute.Address, value } ) as Task;
					if (writeTask == null) return new OperateResult( $"{readWrite.GetType( ).Name} not task type result. " );

					await writeTask;
					OperateResult valueResult = writeTask.GetType( ).GetProperty( "Result" ).GetValue( writeTask, null ) as OperateResult;
					if (!valueResult.IsSuccess) return OperateResult.CreateFailedResult<T>( valueResult );
				}
				else if (propertyType == typeof( short ))
				{
					short value = (short)property.GetValue( obj, null );

					OperateResult writeResult = await readWrite.WriteAsync( hslAttribute.Address, value );
					if (!writeResult.IsSuccess) return writeResult;
				}
				else if (propertyType == typeof( short[] ))
				{
					short[] value = (short[])property.GetValue( obj, null );

					OperateResult writeResult = await readWrite.WriteAsync( hslAttribute.Address, value );
					if (!writeResult.IsSuccess) return writeResult;
				}
				else if (propertyType == typeof( ushort ))
				{
					ushort value = (ushort)property.GetValue( obj, null );

					OperateResult writeResult = await readWrite.WriteAsync( hslAttribute.Address, value );
					if (!writeResult.IsSuccess) return writeResult;
				}
				else if (propertyType == typeof( ushort[] ))
				{
					ushort[] value = (ushort[])property.GetValue( obj, null );

					OperateResult writeResult = await readWrite.WriteAsync( hslAttribute.Address, value );
					if (!writeResult.IsSuccess) return writeResult;
				}
				else if (propertyType == typeof( int ))
				{
					int value = (int)property.GetValue( obj, null );

					OperateResult writeResult = await readWrite.WriteAsync( hslAttribute.Address, value );
					if (!writeResult.IsSuccess) return writeResult;
				}
				else if (propertyType == typeof( int[] ))
				{
					int[] value = (int[])property.GetValue( obj, null );

					OperateResult writeResult = await readWrite.WriteAsync( hslAttribute.Address, value );
					if (!writeResult.IsSuccess) return writeResult;
				}
				else if (propertyType == typeof( uint ))
				{
					uint value = (uint)property.GetValue( obj, null );

					OperateResult writeResult = await readWrite.WriteAsync( hslAttribute.Address, value );
					if (!writeResult.IsSuccess) return writeResult;
				}
				else if (propertyType == typeof( uint[] ))
				{
					uint[] value = (uint[])property.GetValue( obj, null );

					OperateResult writeResult = await readWrite.WriteAsync( hslAttribute.Address, value );
					if (!writeResult.IsSuccess) return writeResult;
				}
				else if (propertyType == typeof( long ))
				{
					long value = (long)property.GetValue( obj, null );

					OperateResult writeResult = await readWrite.WriteAsync( hslAttribute.Address, value );
					if (!writeResult.IsSuccess) return writeResult;
				}
				else if (propertyType == typeof( long[] ))
				{
					long[] value = (long[])property.GetValue( obj, null );

					OperateResult writeResult = await readWrite.WriteAsync( hslAttribute.Address, value );
					if (!writeResult.IsSuccess) return writeResult;
				}
				else if (propertyType == typeof( ulong ))
				{
					ulong value = (ulong)property.GetValue( obj, null );

					OperateResult writeResult = await readWrite.WriteAsync( hslAttribute.Address, value );
					if (!writeResult.IsSuccess) return writeResult;
				}
				else if (propertyType == typeof( ulong[] ))
				{
					ulong[] value = (ulong[])property.GetValue( obj, null );

					OperateResult writeResult = await readWrite.WriteAsync( hslAttribute.Address, value );
					if (!writeResult.IsSuccess) return writeResult;
				}
				else if (propertyType == typeof( float ))
				{
					float value = (float)property.GetValue( obj, null );

					OperateResult writeResult = await readWrite.WriteAsync( hslAttribute.Address, value );
					if (!writeResult.IsSuccess) return writeResult;
				}
				else if (propertyType == typeof( float[] ))
				{
					float[] value = (float[])property.GetValue( obj, null );

					OperateResult writeResult = await readWrite.WriteAsync( hslAttribute.Address, value );
					if (!writeResult.IsSuccess) return writeResult;
				}
				else if (propertyType == typeof( double ))
				{
					double value = (double)property.GetValue( obj, null );

					OperateResult writeResult = await readWrite.WriteAsync( hslAttribute.Address, value );
					if (!writeResult.IsSuccess) return writeResult;
				}
				else if (propertyType == typeof( double[] ))
				{
					double[] value = (double[])property.GetValue( obj, null );

					OperateResult writeResult = await readWrite.WriteAsync( hslAttribute.Address, value );
					if (!writeResult.IsSuccess) return writeResult;
				}
				else if (propertyType == typeof( string ))
				{
					string value = (string)property.GetValue( obj, null );

					OperateResult writeResult = await readWrite.WriteAsync( hslAttribute.Address, value );
					if (!writeResult.IsSuccess) return writeResult;
				}
				else if (propertyType == typeof( byte[] ))
				{
					byte[] value = (byte[])property.GetValue( obj, null );

					OperateResult writeResult = await readWrite.WriteAsync( hslAttribute.Address, value );
					if (!writeResult.IsSuccess) return writeResult;
				}
				else if (propertyType == typeof( bool ))
				{
					bool value = (bool)property.GetValue( obj, null );

					OperateResult writeResult = await readWrite.WriteAsync( hslAttribute.Address, value );
					if (!writeResult.IsSuccess) return writeResult;
				}
				else if (propertyType == typeof( bool[] ))
				{
					bool[] value = (bool[])property.GetValue( obj, null );

					OperateResult writeResult = await readWrite.WriteAsync( hslAttribute.Address, value );
					if (!writeResult.IsSuccess) return writeResult;
				}
			}

			return OperateResult.CreateSuccessResult( (T)obj );
		}

#endif
		internal static void SetPropertyObjectValue( PropertyInfo property, object obj, string value )
		{
			Type propertyType = property.PropertyType;
			if (propertyType == typeof( short ))
				property.SetValue( obj, short.Parse( value ), null );
			else if (propertyType == typeof( ushort ))
				property.SetValue( obj, ushort.Parse( value ), null );
			else if (propertyType == typeof( int ))
				property.SetValue( obj, int.Parse( value ), null );
			else if (propertyType == typeof( uint ))
				property.SetValue( obj, uint.Parse( value ), null );
			else if (propertyType == typeof( long ))
				property.SetValue( obj, long.Parse( value ), null );
			else if (propertyType == typeof( ulong ))
				property.SetValue( obj, ulong.Parse( value ), null );
			else if (propertyType == typeof( float ))
				property.SetValue( obj, float.Parse( value ), null );
			else if (propertyType == typeof( double ))
				property.SetValue( obj, double.Parse( value ), null );
			else if (propertyType == typeof( string ))
				property.SetValue( obj, value, null );
			else if (propertyType == typeof( byte ))
				property.SetValue( obj, byte.Parse( value ), null );
			else if (propertyType == typeof( bool ))
				property.SetValue( obj, bool.Parse( value ), null );
			else
				property.SetValue( obj, value, null );
		}

		internal static void SetPropertyObjectValueArray( PropertyInfo property, object obj, string[] values )
		{
			Type propertyType = property.PropertyType;
			if (propertyType == typeof( short[] ))
				property.SetValue( obj, values.Select( m => short.Parse( m ) ).ToArray( ), null );
			else if (propertyType == typeof( List<short> ))
				property.SetValue( obj, values.Select( m => short.Parse( m ) ).ToList( ), null );
			else if (propertyType == typeof( ushort[] ))
				property.SetValue( obj, values.Select( m => ushort.Parse( m ) ).ToArray( ), null );
			else if (propertyType == typeof( List<ushort> ))
				property.SetValue( obj, values.Select( m => ushort.Parse( m ) ).ToList( ), null );
			else if (propertyType == typeof( int[] ))
				property.SetValue( obj, values.Select( m => int.Parse( m ) ).ToArray( ), null );
			else if (propertyType == typeof( List<int> ))
				property.SetValue( obj, values.Select( m => int.Parse( m ) ).ToList( ), null );
			else if (propertyType == typeof( uint[] ))
				property.SetValue( obj, values.Select( m => uint.Parse( m ) ).ToArray( ), null );
			else if (propertyType == typeof( List<uint> ))
				property.SetValue( obj, values.Select( m => uint.Parse( m ) ).ToList( ), null );
			else if (propertyType == typeof( long[] ))
				property.SetValue( obj, values.Select( m => long.Parse( m ) ).ToArray( ), null );
			else if (propertyType == typeof( List<long> ))
				property.SetValue( obj, values.Select( m => long.Parse( m ) ).ToList( ), null );
			else if (propertyType == typeof( ulong[] ))
				property.SetValue( obj, values.Select( m => ulong.Parse( m ) ).ToArray( ), null );
			else if (propertyType == typeof( List<ulong> ))
				property.SetValue( obj, values.Select( m => ulong.Parse( m ) ).ToList( ), null );
			else if (propertyType == typeof( float[] ))
				property.SetValue( obj, values.Select( m => float.Parse( m ) ).ToArray( ), null );
			else if (propertyType == typeof( List<float> ))
				property.SetValue( obj, values.Select( m => float.Parse( m ) ).ToList( ), null );
			else if (propertyType == typeof( double[] ))
				property.SetValue( obj, values.Select( m => double.Parse( m ) ).ToArray( ), null );
			else if (propertyType == typeof( double[] ))
				property.SetValue( obj, values.Select( m => double.Parse( m ) ).ToList( ), null );
			else if (propertyType == typeof( string[] ))
				property.SetValue( obj, values, null );
			else if (propertyType == typeof( List<string> ))
				property.SetValue( obj, new List<string>( values ), null );
			else if (propertyType == typeof( byte[] ))
				property.SetValue( obj, values.Select( m => byte.Parse( m ) ).ToArray( ), null );
			else if (propertyType == typeof( List<byte> ))
				property.SetValue( obj, values.Select( m => byte.Parse( m ) ).ToList( ), null );
			else if (propertyType == typeof( bool[] ))
				property.SetValue( obj, values.Select( m => bool.Parse( m ) ).ToArray( ), null );
			else if (propertyType == typeof( List<bool> ))
				property.SetValue( obj, values.Select( m => bool.Parse( m ) ).ToList( ), null );
			else
				property.SetValue( obj, values, null );
		}

		/// <summary>
		/// 从设备里读取支持Hsl特性的数据内容，
		/// 该特性为<see cref="HslRedisKeyAttribute"/>，<see cref="HslRedisListItemAttribute"/>，
		/// <see cref="HslRedisListAttribute"/>，<see cref="HslRedisHashFieldAttribute"/>
		/// 详细参考代码示例的操作说明。
		/// </summary>
		/// <typeparam name="T">自定义的数据类型对象</typeparam>
		/// <param name="redis">Redis的数据对象</param>
		/// <returns>包含是否成功的结果对象</returns>
		public static OperateResult<T> Read<T>( RedisClient redis ) where T : class, new()
		{
			var type = typeof( T );
			// var constrcuor = type.GetConstructors( System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic );
			var obj = type.Assembly.CreateInstance( type.FullName );

			var properties = type.GetProperties( System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public );
			List<PropertyInfoKeyName> keyPropertyInfos = new List<PropertyInfoKeyName>( );
			List<PropertyInfoHashKeyName> propertyInfoHashKeys = new List<PropertyInfoHashKeyName>( );
			foreach (var property in properties)
			{
				var attributes = property.GetCustomAttributes( typeof( HslRedisKeyAttribute ), false );
				if (attributes?.Length > 0)
				{
					HslRedisKeyAttribute attribute = (HslRedisKeyAttribute)attributes[0];
					keyPropertyInfos.Add( new PropertyInfoKeyName( property, attribute.KeyName ) );
					continue;
				}

				attributes = property.GetCustomAttributes( typeof( HslRedisListItemAttribute ), false );
				if (attributes?.Length > 0)
				{
					HslRedisListItemAttribute attribute = (HslRedisListItemAttribute)attributes[0];

					OperateResult<string> read = redis.ReadListByIndex( attribute.ListKey, attribute.Index );
					if (!read.IsSuccess) return OperateResult.CreateFailedResult<T>( read );

					SetPropertyObjectValue( property, obj, read.Content );
					continue;
				}

				attributes = property.GetCustomAttributes( typeof( HslRedisListAttribute ), false );
				if (attributes?.Length > 0)
				{
					HslRedisListAttribute attribute = (HslRedisListAttribute)attributes[0];

					OperateResult<string[]> read = redis.ListRange( attribute.ListKey, attribute.StartIndex, attribute.EndIndex );
					if (!read.IsSuccess) return OperateResult.CreateFailedResult<T>( read );

					SetPropertyObjectValueArray( property, obj, read.Content );
					continue;
				}

				attributes = property.GetCustomAttributes( typeof( HslRedisHashFieldAttribute ), false );
				if (attributes?.Length > 0)
				{
					HslRedisHashFieldAttribute attribute = (HslRedisHashFieldAttribute)attributes[0];
					propertyInfoHashKeys.Add( new PropertyInfoHashKeyName( property, attribute.HaskKey, attribute.Field ) );
					continue;
				}
			}

			if (keyPropertyInfos.Count > 0)
			{
				OperateResult<string[]> readKeys = redis.ReadKey( keyPropertyInfos.Select( m => m.KeyName ).ToArray( ) );
				if (!readKeys.IsSuccess) return OperateResult.CreateFailedResult<T>( readKeys );

				for (int i = 0; i < keyPropertyInfos.Count; i++)
					SetPropertyObjectValue( keyPropertyInfos[i].PropertyInfo, obj, readKeys.Content[i] );
			}

			if(propertyInfoHashKeys.Count > 0)
			{
				var tmp = from m in propertyInfoHashKeys
						  group m by m.KeyName into g
						  select new { g.Key, Values = g.ToArray( ) };
				foreach (var item in tmp)
				{
					if (item.Values.Length == 1)
					{
						OperateResult<string> readKey = redis.ReadHashKey( item.Key, item.Values[0].Field );
						if (!readKey.IsSuccess) return OperateResult.CreateFailedResult<T>( readKey );

						SetPropertyObjectValue( item.Values[0].PropertyInfo, obj, readKey.Content );
					}
					else
					{
						OperateResult<string[]> readKeys = redis.ReadHashKey( item.Key, item.Values.Select( m => m.Field ).ToArray( ) );
						if (!readKeys.IsSuccess) return OperateResult.CreateFailedResult<T>( readKeys );

						for (int i = 0; i < item.Values.Length; i++)
							SetPropertyObjectValue( item.Values[i].PropertyInfo, obj, readKeys.Content[i] );
					}
				}
			}

			return OperateResult.CreateSuccessResult( (T)obj );
		}

		/// <summary>
		/// 从设备里写入支持Hsl特性的数据内容，
		/// 该特性为<see cref="HslRedisKeyAttribute"/> ，<see cref="HslRedisHashFieldAttribute"/>
		/// 需要注意的是写入并不支持<see cref="HslRedisListAttribute"/>，<see cref="HslRedisListItemAttribute"/>特性，详细参考代码示例的操作说明。
		/// </summary>
		/// <typeparam name="T">自定义的数据类型对象</typeparam>
		/// <param name="data">等待写入的数据参数</param>
		/// <param name="redis">Redis的数据对象</param>
		/// <returns>包含是否成功的结果对象</returns>
		public static OperateResult Write<T>( T data, RedisClient redis ) where T : class, new()
		{
			var type = typeof( T );
			var obj = data;

			var properties = type.GetProperties( System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public );
			List<PropertyInfoKeyName> keyPropertyInfos = new List<PropertyInfoKeyName>( );
			List<PropertyInfoHashKeyName> propertyInfoHashKeys = new List<PropertyInfoHashKeyName>( );
			foreach (var property in properties)
			{
				var attributes = property.GetCustomAttributes( typeof( HslRedisKeyAttribute ), false );
				if (attributes?.Length > 0)
				{
					HslRedisKeyAttribute attribute = (HslRedisKeyAttribute)attributes[0];
					keyPropertyInfos.Add( new PropertyInfoKeyName( property, attribute.KeyName, property.GetValue( obj, null ).ToString( ) ) );
					continue;
				}

				//attributes = property.GetCustomAttributes( typeof( HslRedisListItemAttribute ), false );
				//if (attributes?.Length > 0)
				//{
				//	HslRedisListItemAttribute attribute = (HslRedisListItemAttribute)attributes[0];
				//	string value = property.GetValue( obj, null ).ToString( );

				//	OperateResult writeResult = redis.ListSet( attribute.ListKey, attribute.Index, value );
				//	if (!writeResult.IsSuccess) return writeResult;
				//	continue;
				//}

				attributes = property.GetCustomAttributes( typeof( HslRedisHashFieldAttribute ), false );
				if (attributes?.Length > 0)
				{
					HslRedisHashFieldAttribute attribute = (HslRedisHashFieldAttribute)attributes[0];
					propertyInfoHashKeys.Add( new PropertyInfoHashKeyName( property, attribute.HaskKey, attribute.Field, property.GetValue( obj, null ).ToString( ) ) );
					continue;
				}
			}

			if (keyPropertyInfos.Count > 0)
			{
				OperateResult writeResult = redis.WriteKey( keyPropertyInfos.Select( m => m.KeyName ).ToArray( ), keyPropertyInfos.Select( m => m.Value ).ToArray( ) );
				if (!writeResult.IsSuccess) return writeResult;
			}

			if (propertyInfoHashKeys.Count > 0)
			{
				var tmp = from m in propertyInfoHashKeys
						  group m by m.KeyName into g
						  select new { g.Key, Values = g.ToArray( ) };
				foreach (var item in tmp)
				{
					if (item.Values.Length == 1)
					{
						OperateResult writeResult = redis.WriteHashKey( item.Key, item.Values[0].Field, item.Values[0].Value );
						if (!writeResult.IsSuccess) return writeResult;
					}
					else
					{
						OperateResult writeResult = redis.WriteHashKey( item.Key, item.Values.Select( m => m.Field ).ToArray( ), item.Values.Select( m => m.Value ).ToArray( ) );
						if (!writeResult.IsSuccess) return writeResult;
					}
				}
			}

			return OperateResult.CreateSuccessResult( );
		}
#if !NET35 && !NET20
		/// <summary>
		/// 从设备里读取支持Hsl特性的数据内容，
		/// 该特性为<see cref="HslRedisKeyAttribute"/>，<see cref="HslRedisListItemAttribute"/>，
		/// <see cref="HslRedisListAttribute"/>，<see cref="HslRedisHashFieldAttribute"/>
		/// 详细参考代码示例的操作说明。
		/// </summary>
		/// <typeparam name="T">自定义的数据类型对象</typeparam>
		/// <param name="redis">Redis的数据对象</param>
		/// <returns>包含是否成功的结果对象</returns>
		public static async Task<OperateResult<T>> ReadAsync<T>( RedisClient redis ) where T : class, new()
		{
			var type = typeof( T );
			// var constrcuor = type.GetConstructors( System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic );
			var obj = type.Assembly.CreateInstance( type.FullName );

			var properties = type.GetProperties( System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public );
			List<PropertyInfoKeyName> keyPropertyInfos = new List<PropertyInfoKeyName>( );
			List<PropertyInfoHashKeyName> propertyInfoHashKeys = new List<PropertyInfoHashKeyName>( );
			foreach (var property in properties)
			{
				var attributes = property.GetCustomAttributes( typeof( HslRedisKeyAttribute ), false );
				if (attributes?.Length > 0)
				{
					HslRedisKeyAttribute attribute = (HslRedisKeyAttribute)attributes[0];
					keyPropertyInfos.Add( new PropertyInfoKeyName( property, attribute.KeyName ) );
					continue;
				}

				attributes = property.GetCustomAttributes( typeof( HslRedisListItemAttribute ), false );
				if (attributes?.Length > 0)
				{
					HslRedisListItemAttribute attribute = (HslRedisListItemAttribute)attributes[0];

					OperateResult<string> read = await redis.ReadListByIndexAsync( attribute.ListKey, attribute.Index );
					if (!read.IsSuccess) return OperateResult.CreateFailedResult<T>( read );

					SetPropertyObjectValue( property, obj, read.Content );
					continue;
				}

				attributes = property.GetCustomAttributes( typeof( HslRedisListAttribute ), false );
				if (attributes?.Length > 0)
				{
					HslRedisListAttribute attribute = (HslRedisListAttribute)attributes[0];

					OperateResult<string[]> read = await redis.ListRangeAsync( attribute.ListKey, attribute.StartIndex, attribute.EndIndex );
					if (!read.IsSuccess) return OperateResult.CreateFailedResult<T>( read );

					SetPropertyObjectValueArray( property, obj, read.Content );
					continue;
				}

				attributes = property.GetCustomAttributes( typeof( HslRedisHashFieldAttribute ), false );
				if (attributes?.Length > 0)
				{
					HslRedisHashFieldAttribute attribute = (HslRedisHashFieldAttribute)attributes[0];
					propertyInfoHashKeys.Add( new PropertyInfoHashKeyName( property, attribute.HaskKey, attribute.Field ) );
					continue;
				}
			}

			if (keyPropertyInfos.Count > 0)
			{
				OperateResult<string[]> readKeys = await redis.ReadKeyAsync( keyPropertyInfos.Select( m => m.KeyName ).ToArray( ) );
				if (!readKeys.IsSuccess) return OperateResult.CreateFailedResult<T>( readKeys );

				for (int i = 0; i < keyPropertyInfos.Count; i++)
					SetPropertyObjectValue( keyPropertyInfos[i].PropertyInfo, obj, readKeys.Content[i] );
			}

			if (propertyInfoHashKeys.Count > 0)
			{
				var tmp = from m in propertyInfoHashKeys
						  group m by m.KeyName into g
						  select new { g.Key, Values = g.ToArray( ) };
				foreach (var item in tmp)
				{
					if (item.Values.Length == 1)
					{
						OperateResult<string> readKey = await redis.ReadHashKeyAsync( item.Key, item.Values[0].Field );
						if (!readKey.IsSuccess) return OperateResult.CreateFailedResult<T>( readKey );

						SetPropertyObjectValue( item.Values[0].PropertyInfo, obj, readKey.Content );
					}
					else
					{
						OperateResult<string[]> readKeys = await redis.ReadHashKeyAsync( item.Key, item.Values.Select( m => m.Field ).ToArray( ) );
						if (!readKeys.IsSuccess) return OperateResult.CreateFailedResult<T>( readKeys );

						for (int i = 0; i < item.Values.Length; i++)
							SetPropertyObjectValue( item.Values[i].PropertyInfo, obj, readKeys.Content[i] );
					}
				}
			}

			return OperateResult.CreateSuccessResult( (T)obj );
		}

		/// <summary>
		/// 从设备里写入支持Hsl特性的数据内容，
		/// 该特性为<see cref="HslRedisKeyAttribute"/> ，<see cref="HslRedisHashFieldAttribute"/>
		/// 需要注意的是写入并不支持<see cref="HslRedisListAttribute"/>，<see cref="HslRedisListItemAttribute"/>特性，详细参考代码示例的操作说明。
		/// </summary>
		/// <typeparam name="T">自定义的数据类型对象</typeparam>
		/// <param name="data">等待写入的数据参数</param>
		/// <param name="redis">Redis的数据对象</param>
		/// <returns>包含是否成功的结果对象</returns>
		public static async Task<OperateResult> WriteAsync<T>( T data, RedisClient redis ) where T : class, new()
		{
			var type = typeof( T );
			var obj = data;

			var properties = type.GetProperties( System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public );
			List<PropertyInfoKeyName> keyPropertyInfos = new List<PropertyInfoKeyName>( );
			List<PropertyInfoHashKeyName> propertyInfoHashKeys = new List<PropertyInfoHashKeyName>( );
			foreach (var property in properties)
			{
				var attributes = property.GetCustomAttributes( typeof( HslRedisKeyAttribute ), false );
				if (attributes?.Length > 0)
				{
					HslRedisKeyAttribute attribute = (HslRedisKeyAttribute)attributes[0];
					keyPropertyInfos.Add( new PropertyInfoKeyName( property, attribute.KeyName, property.GetValue( obj, null ).ToString( ) ) );
					continue;
				}

				//attributes = property.GetCustomAttributes( typeof( HslRedisListItemAttribute ), false );
				//if (attributes?.Length > 0)
				//{
				//	HslRedisListItemAttribute attribute = (HslRedisListItemAttribute)attributes[0];
				//	string value = property.GetValue( obj, null ).ToString( );

				//	OperateResult writeResult = redis.ListSet( attribute.ListKey, attribute.Index, value );
				//	if (!writeResult.IsSuccess) return writeResult;
				//	continue;
				//}

				attributes = property.GetCustomAttributes( typeof( HslRedisHashFieldAttribute ), false );
				if (attributes?.Length > 0)
				{
					HslRedisHashFieldAttribute attribute = (HslRedisHashFieldAttribute)attributes[0];
					propertyInfoHashKeys.Add( new PropertyInfoHashKeyName( property, attribute.HaskKey, attribute.Field, property.GetValue( obj, null ).ToString( ) ) );
					continue;
				}
			}

			if (keyPropertyInfos.Count > 0)
			{
				OperateResult writeResult = await redis.WriteKeyAsync( keyPropertyInfos.Select( m => m.KeyName ).ToArray( ), keyPropertyInfos.Select( m => m.Value ).ToArray( ) );
				if (!writeResult.IsSuccess) return writeResult;
			}

			if (propertyInfoHashKeys.Count > 0)
			{
				var tmp = from m in propertyInfoHashKeys
						  group m by m.KeyName into g
						  select new { g.Key, Values = g.ToArray( ) };
				foreach (var item in tmp)
				{
					if (item.Values.Length == 1)
					{
						OperateResult writeResult = await redis.WriteHashKeyAsync( item.Key, item.Values[0].Field, item.Values[0].Value );
						if (!writeResult.IsSuccess) return writeResult;
					}
					else
					{
						OperateResult writeResult = await redis.WriteHashKeyAsync( item.Key, item.Values.Select( m => m.Field ).ToArray( ), item.Values.Select( m => m.Value ).ToArray( ) );
						if (!writeResult.IsSuccess) return writeResult;
					}
				}
			}

			return OperateResult.CreateSuccessResult( );
		}
#endif


		#region Parameters From json

		/// <summary>
		/// 从Json数据里解析出真实的数据信息，根据方法参数列表的类型进行反解析，然后返回实际的数据数组<br />
		/// Analyze the real data information from the Json data, perform de-analysis according to the type of the method parameter list, 
		/// and then return the actual data array
		/// </summary>
		/// <param name="context">当前的会话内容</param>
		/// <param name="parameters">提供的参数列表信息</param>
		/// <param name="json">参数变量信息</param>
		/// <returns>已经填好的实际数据的参数数组对象</returns>
		public static object[] GetParametersFromJson( ISessionContext context, ParameterInfo[] parameters, string json )
		{
			JObject jObject = string.IsNullOrEmpty( json ) ? new JObject( ) : JObject.Parse( json );
			object[] paras = new object[parameters.Length];
			for (int i = 0; i < parameters.Length; i++)
			{
				if      (parameters[i].ParameterType == typeof( byte ))            paras[i] = jObject.Value<byte>(     parameters[i].Name );
				else if (parameters[i].ParameterType == typeof( short ))           paras[i] = jObject.Value<short>(    parameters[i].Name );
				else if (parameters[i].ParameterType == typeof( ushort ))          paras[i] = jObject.Value<ushort>(   parameters[i].Name );
				else if (parameters[i].ParameterType == typeof( int ))             paras[i] = jObject.Value<int>(      parameters[i].Name );
				else if (parameters[i].ParameterType == typeof( uint ))            paras[i] = jObject.Value<uint>(     parameters[i].Name );
				else if (parameters[i].ParameterType == typeof( long ))            paras[i] = jObject.Value<long>(     parameters[i].Name );
				else if (parameters[i].ParameterType == typeof( ulong ))           paras[i] = jObject.Value<ulong>(    parameters[i].Name );
				else if (parameters[i].ParameterType == typeof( double ))          paras[i] = jObject.Value<double>(   parameters[i].Name );
				else if (parameters[i].ParameterType == typeof( float ))           paras[i] = jObject.Value<float>(    parameters[i].Name );
				else if (parameters[i].ParameterType == typeof( bool ))            paras[i] = jObject.Value<bool>(     parameters[i].Name );
				else if (parameters[i].ParameterType == typeof( string ))          paras[i] = jObject.Value<string>(   parameters[i].Name );
				else if (parameters[i].ParameterType == typeof( DateTime ))        paras[i] = jObject.Value<DateTime>( parameters[i].Name );
				else if (parameters[i].ParameterType == typeof( byte[] ))          paras[i] = jObject.Value<string>(   parameters[i].Name ).ToHexBytes( );
				else if (parameters[i].ParameterType == typeof( short[] ))         paras[i] = jObject[parameters[i].Name].ToArray( ).Select( m => m.Value<short>( ) ).ToArray( );
				else if (parameters[i].ParameterType == typeof( ushort[] ))        paras[i] = jObject[parameters[i].Name].ToArray( ).Select( m => m.Value<ushort>( ) ).ToArray( );
				else if (parameters[i].ParameterType == typeof( int[] ))           paras[i] = jObject[parameters[i].Name].ToArray( ).Select( m => m.Value<int>( ) ).ToArray( );
				else if (parameters[i].ParameterType == typeof( uint[] ))          paras[i] = jObject[parameters[i].Name].ToArray( ).Select( m => m.Value<uint>( ) ).ToArray( );
				else if (parameters[i].ParameterType == typeof( long[] ))          paras[i] = jObject[parameters[i].Name].ToArray( ).Select( m => m.Value<long>( ) ).ToArray( );
				else if (parameters[i].ParameterType == typeof( ulong[] ))         paras[i] = jObject[parameters[i].Name].ToArray( ).Select( m => m.Value<ulong>( ) ).ToArray( );
				else if (parameters[i].ParameterType == typeof( float[] ))         paras[i] = jObject[parameters[i].Name].ToArray( ).Select( m => m.Value<float>( ) ).ToArray( );
				else if (parameters[i].ParameterType == typeof( double[] ))        paras[i] = jObject[parameters[i].Name].ToArray( ).Select( m => m.Value<double>( ) ).ToArray( );
				else if (parameters[i].ParameterType == typeof( bool[] ))          paras[i] = jObject[parameters[i].Name].ToArray( ).Select( m => m.Value<bool>( ) ).ToArray( );
				else if (parameters[i].ParameterType == typeof( string[] ))        paras[i] = jObject[parameters[i].Name].ToArray( ).Select( m => m.Value<string>( ) ).ToArray( );
				else if (parameters[i].ParameterType == typeof( DateTime[] ))      paras[i] = jObject[parameters[i].Name].ToArray( ).Select( m => m.Value<DateTime>( ) ).ToArray( );
				else if (parameters[i].ParameterType == typeof( ISessionContext )) paras[i] = context;
				else paras[i] = jObject[parameters[i].Name].ToObject( parameters[i].ParameterType );
				//else throw new Exception( $"Can't support parameter [{parameters[i].Name}] type : {parameters[i].ParameterType}"  );
			}
			return paras;
		}

		/// <summary>
		/// 从url数据里解析出真实的数据信息，根据方法参数列表的类型进行反解析，然后返回实际的数据数组<br />
		/// Analyze the real data information from the url data, perform de-analysis according to the type of the method parameter list, 
		/// and then return the actual data array
		/// </summary>
		/// <param name="context">当前的会话内容</param>
		/// <param name="parameters">提供的参数列表信息</param>
		/// <param name="url">参数变量信息</param>
		/// <returns>已经填好的实际数据的参数数组对象</returns>
		public static object[] GetParametersFromUrl( ISessionContext context, ParameterInfo[] parameters, string url )
		{
			if (url.IndexOf( '?' ) > 0) url = url.Substring( url.IndexOf( '?' ) + 1 );
			string[] splits = url.Split( new char[] { '&' }, StringSplitOptions.RemoveEmptyEntries );
			Dictionary<string, string> dict = new Dictionary<string, string>( splits.Length );
			for (int i = 0; i < splits.Length; i++)
			{
				if (!string.IsNullOrEmpty( splits[i] ))
				{
					if (splits[i].IndexOf( '=' ) > 0)
					{
						dict.Add( splits[i].Substring( 0, splits[i].IndexOf( '=' ) ).Trim(' '), splits[i].Substring( splits[i].IndexOf( '=' ) + 1 ) );
					}
				}
			}

			object[] paras = new object[parameters.Length];
			for (int i = 0; i < parameters.Length; i++)
			{
				if      (parameters[i].ParameterType == typeof( byte ))            paras[i] = byte.Parse(     dict[parameters[i].Name] );
				else if (parameters[i].ParameterType == typeof( short ))           paras[i] = short.Parse(    dict[parameters[i].Name] );
				else if (parameters[i].ParameterType == typeof( ushort ))          paras[i] = ushort.Parse(   dict[parameters[i].Name] );
				else if (parameters[i].ParameterType == typeof( int ))             paras[i] = int.Parse(      dict[parameters[i].Name] );
				else if (parameters[i].ParameterType == typeof( uint ))            paras[i] = uint.Parse(     dict[parameters[i].Name] );
				else if (parameters[i].ParameterType == typeof( long ))            paras[i] = long.Parse(     dict[parameters[i].Name] );
				else if (parameters[i].ParameterType == typeof( ulong ))           paras[i] = ulong.Parse(    dict[parameters[i].Name] );
				else if (parameters[i].ParameterType == typeof( double ))          paras[i] = double.Parse(   dict[parameters[i].Name] );
				else if (parameters[i].ParameterType == typeof( float ))           paras[i] = float.Parse(    dict[parameters[i].Name] );
				else if (parameters[i].ParameterType == typeof( bool ))            paras[i] = bool.Parse(     dict[parameters[i].Name] );
				else if (parameters[i].ParameterType == typeof( string ))          paras[i] =                 dict[parameters[i].Name];
				else if (parameters[i].ParameterType == typeof( DateTime ))        paras[i] = DateTime.Parse( dict[parameters[i].Name] );
				else if (parameters[i].ParameterType == typeof( byte[] ))          paras[i] = dict[parameters[i].Name].ToHexBytes( );
				else if (parameters[i].ParameterType == typeof( short[] ))         paras[i] = dict[parameters[i].Name].ToStringArray<short>( );
				else if (parameters[i].ParameterType == typeof( ushort[] ))        paras[i] = dict[parameters[i].Name].ToStringArray<ushort>( );
				else if (parameters[i].ParameterType == typeof( int[] ))           paras[i] = dict[parameters[i].Name].ToStringArray<int>( );
				else if (parameters[i].ParameterType == typeof( uint[] ))          paras[i] = dict[parameters[i].Name].ToStringArray<uint>( );
				else if (parameters[i].ParameterType == typeof( long[] ))          paras[i] = dict[parameters[i].Name].ToStringArray<long>( );
				else if (parameters[i].ParameterType == typeof( ulong[] ))         paras[i] = dict[parameters[i].Name].ToStringArray<ulong>( );
				else if (parameters[i].ParameterType == typeof( float[] ))         paras[i] = dict[parameters[i].Name].ToStringArray<float>( );
				else if (parameters[i].ParameterType == typeof( double[] ))        paras[i] = dict[parameters[i].Name].ToStringArray<double>( );
				else if (parameters[i].ParameterType == typeof( bool[] ))          paras[i] = dict[parameters[i].Name].ToStringArray<bool>( );
				else if (parameters[i].ParameterType == typeof( string[] ))        paras[i] = dict[parameters[i].Name].ToStringArray<string>( );
				else if (parameters[i].ParameterType == typeof( DateTime[] ))      paras[i] = dict[parameters[i].Name].ToStringArray<DateTime>( );
				else if (parameters[i].ParameterType == typeof( ISessionContext )) paras[i] = context;
				else paras[i] = JToken.Parse(dict[parameters[i].Name]).ToObject( parameters[i].ParameterType );
				//else throw new Exception( $"Can't support parameter [{parameters[i].Name}] type : {parameters[i].ParameterType}"  );
			}
			return paras;
		}
		/// <summary>
		/// 从方法的参数列表里，提取出实际的示例参数信息，返回一个json对象，注意：该数据是示例的数据，具体参数的限制参照服务器返回的数据声明。<br />
		/// From the parameter list of the method, extract the actual example parameter information, and return a json object. Note: The data is the example data, 
		/// and the specific parameter restrictions refer to the data declaration returned by the server.
		/// </summary>
		/// <param name="method">当前需要解析的方法名称</param>
		/// <param name="parameters">当前的参数列表信息</param>
		/// <returns>当前的参数对象信息</returns>
		public static JObject GetParametersFromJson( MethodInfo method, ParameterInfo[] parameters )
		{
			JObject jObject = new JObject( );
			for (int i = 0; i < parameters.Length; i++)
			{
#if NET20 || NET35
				if      (parameters[i].ParameterType == typeof( byte ))       jObject.Add( parameters[i].Name, new JValue( default( byte ) ) );
				else if (parameters[i].ParameterType == typeof( short ))      jObject.Add( parameters[i].Name, new JValue( default( short ) ) );
				else if (parameters[i].ParameterType == typeof( ushort ))     jObject.Add( parameters[i].Name, new JValue( default( ushort ) ) );
				else if (parameters[i].ParameterType == typeof( int ))        jObject.Add( parameters[i].Name, new JValue( default( int ) ) );
				else if (parameters[i].ParameterType == typeof( uint ))       jObject.Add( parameters[i].Name, new JValue( default( uint ) ) );
				else if (parameters[i].ParameterType == typeof( long ))       jObject.Add( parameters[i].Name, new JValue( default( long ) ) );
				else if (parameters[i].ParameterType == typeof( ulong ))      jObject.Add( parameters[i].Name, new JValue( default( ulong ) ) );
				else if (parameters[i].ParameterType == typeof( double ))     jObject.Add( parameters[i].Name, new JValue( default( double ) ) );
				else if (parameters[i].ParameterType == typeof( float ))      jObject.Add( parameters[i].Name, new JValue( default( float ) ) );
				else if (parameters[i].ParameterType == typeof( bool ))       jObject.Add( parameters[i].Name, new JValue( default( bool ) ) );
				else if (parameters[i].ParameterType == typeof( string ))     jObject.Add( parameters[i].Name, new JValue( "" ) );
				else if (parameters[i].ParameterType == typeof( DateTime ))   jObject.Add( parameters[i].Name, new JValue( DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") ) );
				else if (parameters[i].ParameterType == typeof( byte[] ))     jObject.Add( parameters[i].Name, new JValue( "00 1A 2B 3C 4D" ) );
				else if (parameters[i].ParameterType == typeof( short[] ))    jObject.Add( parameters[i].Name, new JArray( new int[] { 1, 2, 3 } ) );
				else if (parameters[i].ParameterType == typeof( ushort[] ))   jObject.Add( parameters[i].Name, new JArray( new int[] { 1, 2, 3 } ) );
				else if (parameters[i].ParameterType == typeof( int[] ))      jObject.Add( parameters[i].Name, new JArray( new int[] { 1, 2, 3 } ) );
				else if (parameters[i].ParameterType == typeof( uint[] ))     jObject.Add( parameters[i].Name, new JArray( new int[] { 1, 2, 3 } ) );
				else if (parameters[i].ParameterType == typeof( long[] ))     jObject.Add( parameters[i].Name, new JArray( new int[] { 1, 2, 3 } ) );
				else if (parameters[i].ParameterType == typeof( ulong[] ))    jObject.Add( parameters[i].Name, new JArray( new int[] { 1, 2, 3 } ) );
				else if (parameters[i].ParameterType == typeof( float[] ))    jObject.Add( parameters[i].Name, new JArray( new float[] { 1f, 2f, 3f } ) );
				else if (parameters[i].ParameterType == typeof( double[] ))   jObject.Add( parameters[i].Name, new JArray( new double[] { 1d, 2d, 3d } ) );
				else if (parameters[i].ParameterType == typeof( bool[] ))     jObject.Add( parameters[i].Name, new JArray( new bool[] { true, false, false } ) );
				else if (parameters[i].ParameterType == typeof( string[] ))   jObject.Add( parameters[i].Name, new JArray( new string[] { "1", "2", "3" } ) );
				else if (parameters[i].ParameterType == typeof( DateTime[] )) jObject.Add( parameters[i].Name, new JArray( new string[] { DateTime.Now.ToString( "yyyy-MM-dd HH:mm:ss" ) } ) );
				else if (parameters[i].ParameterType == typeof( ISessionContext )) continue;
				else jObject.Add( parameters[i].Name, JToken.FromObject( Activator.CreateInstance( parameters[i].ParameterType ) ) );
				//else throw new Exception( $"Can't support parameter [{parameters[i].Name}] type : {parameters[i].ParameterType}" );
#else
				if      (parameters[i].ParameterType == typeof( byte ))       jObject.Add( parameters[i].Name, new JValue( parameters[i].HasDefaultValue ? (byte)parameters[i].DefaultValue : default( byte ) ) );
				else if (parameters[i].ParameterType == typeof( short ))      jObject.Add( parameters[i].Name, new JValue( parameters[i].HasDefaultValue ? (short)parameters[i].DefaultValue : default( short ) ) );
				else if (parameters[i].ParameterType == typeof( ushort ))     jObject.Add( parameters[i].Name, new JValue( parameters[i].HasDefaultValue ? (ushort)parameters[i].DefaultValue : default( ushort ) ) );
				else if (parameters[i].ParameterType == typeof( int ))        jObject.Add( parameters[i].Name, new JValue( parameters[i].HasDefaultValue ? (int)parameters[i].DefaultValue : default( int ) ) );
				else if (parameters[i].ParameterType == typeof( uint ))       jObject.Add( parameters[i].Name, new JValue( parameters[i].HasDefaultValue ? (uint)parameters[i].DefaultValue : default( uint ) ) );
				else if (parameters[i].ParameterType == typeof( long ))       jObject.Add( parameters[i].Name, new JValue( parameters[i].HasDefaultValue ? (long)parameters[i].DefaultValue : default( long ) ) );
				else if (parameters[i].ParameterType == typeof( ulong ))      jObject.Add( parameters[i].Name, new JValue( parameters[i].HasDefaultValue ? (ulong)parameters[i].DefaultValue : default( ulong ) ) );
				else if (parameters[i].ParameterType == typeof( double ))     jObject.Add( parameters[i].Name, new JValue( parameters[i].HasDefaultValue ? (double)parameters[i].DefaultValue : default( double ) ) );
				else if (parameters[i].ParameterType == typeof( float ))      jObject.Add( parameters[i].Name, new JValue( parameters[i].HasDefaultValue ? (float)parameters[i].DefaultValue : default( float ) ) );
				else if (parameters[i].ParameterType == typeof( bool ))       jObject.Add( parameters[i].Name, new JValue( parameters[i].HasDefaultValue ? (bool)parameters[i].DefaultValue : default( bool ) ) );
				else if (parameters[i].ParameterType == typeof( string ))     jObject.Add( parameters[i].Name, new JValue( parameters[i].HasDefaultValue ? (string)parameters[i].DefaultValue : "" ) );
				else if (parameters[i].ParameterType == typeof( DateTime ))   jObject.Add( parameters[i].Name, new JValue( parameters[i].HasDefaultValue ? ((DateTime)parameters[i].DefaultValue).ToString( "yyyy-MM-dd HH:mm:ss" ) : DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") ) );
				else if (parameters[i].ParameterType == typeof( byte[] ))     jObject.Add( parameters[i].Name, new JValue( parameters[i].HasDefaultValue ? ((byte[])parameters[i].DefaultValue).ToHexString( ) : "00 1A 2B 3C 4D" ) );
				else if (parameters[i].ParameterType == typeof( short[] ))    jObject.Add( parameters[i].Name, new JArray( parameters[i].HasDefaultValue ? (short[])parameters[i].DefaultValue : new short[] { 1, 2, 3 } ) );
				else if (parameters[i].ParameterType == typeof( ushort[] ))   jObject.Add( parameters[i].Name, new JArray( parameters[i].HasDefaultValue ? (ushort[])parameters[i].DefaultValue : new ushort[] { 1, 2, 3 } ) );
				else if (parameters[i].ParameterType == typeof( int[] ))      jObject.Add( parameters[i].Name, new JArray( parameters[i].HasDefaultValue ? (int[])parameters[i].DefaultValue : new int[] { 1, 2, 3 } ) );
				else if (parameters[i].ParameterType == typeof( uint[] ))     jObject.Add( parameters[i].Name, new JArray( parameters[i].HasDefaultValue ? (uint[])parameters[i].DefaultValue : new uint[] { 1, 2, 3 } ) );
				else if (parameters[i].ParameterType == typeof( long[] ))     jObject.Add( parameters[i].Name, new JArray( parameters[i].HasDefaultValue ? (long[])parameters[i].DefaultValue : new long[] { 1, 2, 3 } ) );
				else if (parameters[i].ParameterType == typeof( ulong[] ))    jObject.Add( parameters[i].Name, new JArray( parameters[i].HasDefaultValue ? (ulong[])parameters[i].DefaultValue : new ulong[] { 1, 2, 3 } ) );
				else if (parameters[i].ParameterType == typeof( float[] ))    jObject.Add( parameters[i].Name, new JArray( parameters[i].HasDefaultValue ? (float[])parameters[i].DefaultValue : new float[] { 1f, 2f, 3f } ) );
				else if (parameters[i].ParameterType == typeof( double[] ))   jObject.Add( parameters[i].Name, new JArray( parameters[i].HasDefaultValue ? (double[])parameters[i].DefaultValue : new double[] { 1d, 2d, 3d } ) );
				else if (parameters[i].ParameterType == typeof( bool[] ))     jObject.Add( parameters[i].Name, new JArray( parameters[i].HasDefaultValue ? (bool[])parameters[i].DefaultValue : new bool[] { true, false, false } ) );
				else if (parameters[i].ParameterType == typeof( string[] ))   jObject.Add( parameters[i].Name, new JArray( parameters[i].HasDefaultValue ? (string[])parameters[i].DefaultValue : new string[] { "1", "2", "3" } ) );
				else if (parameters[i].ParameterType == typeof( DateTime[] )) jObject.Add( parameters[i].Name, new JArray( parameters[i].HasDefaultValue ? ((DateTime[])parameters[i].DefaultValue).Select(m=>m.ToString( "yyyy-MM-dd HH:mm:ss" )).ToArray() : new string[] { DateTime.Now.ToString( "yyyy-MM-dd HH:mm:ss" ) } ) );
				else if (parameters[i].ParameterType == typeof( ISessionContext )) continue;
				else jObject.Add( parameters[i].Name, JToken.FromObject( parameters[i].HasDefaultValue ? parameters[i].DefaultValue : Activator.CreateInstance( parameters[i].ParameterType ) ) );
				// else throw new Exception( $"Can't support parameter [{parameters[i].Name}] type : {parameters[i].ParameterType}" );
#endif
			}
			return jObject;
		}

		/// <summary>
		/// 将一个对象转换成 <see cref="OperateResult{T}"/> 的string 类型的对象，用于远程RPC的数据交互 
		/// </summary>
		/// <param name="obj">自定义的对象</param>
		/// <returns>转换之后的结果对象</returns>
		public static OperateResult<string> GetOperateResultJsonFromObj( object obj )
		{
			if (obj is OperateResult result)
			{
				OperateResult<string> ret = new OperateResult<string>( );
				ret.IsSuccess = result.IsSuccess;
				ret.ErrorCode = result.ErrorCode;
				ret.Message = result.Message;

				if (result.IsSuccess)
				{
					var property = obj.GetType( ).GetProperty( "Content" );
					if (property != null)
					{
						var retObject = property.GetValue( obj, null );
						if (retObject != null) ret.Content = retObject.ToJsonString( );
						return ret;
					}

					var propertyContent1 = obj.GetType( ).GetProperty( "Content1" );
					if (propertyContent1 == null) return ret;

					var propertyContent2 = obj.GetType( ).GetProperty( "Content2" );
					if (propertyContent2 == null) 
					{ 
						ret.Content = new { Content1 = propertyContent1.GetValue( obj, null ) }.ToJsonString( );
						return ret;
					}

					var propertyContent3 = obj.GetType( ).GetProperty( "Content3" );
					if (propertyContent3 == null)
					{
						ret.Content = new { 
							Content1 = propertyContent1.GetValue( obj, null ),
							Content2 = propertyContent2.GetValue( obj, null ),
						}.ToJsonString( );
						return ret;
					}

					var propertyContent4 = obj.GetType( ).GetProperty( "Content4" );
					if (propertyContent4 == null)
					{
						ret.Content = new
						{
							Content1 = propertyContent1.GetValue( obj, null ),
							Content2 = propertyContent2.GetValue( obj, null ),
							Content3 = propertyContent3.GetValue( obj, null ),
						}.ToJsonString( );
						return ret;
					}

					var propertyContent5 = obj.GetType( ).GetProperty( "Content5" );
					if (propertyContent5 == null)
					{
						ret.Content = new
						{
							Content1 = propertyContent1.GetValue( obj, null ),
							Content2 = propertyContent2.GetValue( obj, null ),
							Content3 = propertyContent3.GetValue( obj, null ),
							Content4 = propertyContent4.GetValue( obj, null ),
						}.ToJsonString( );
						return ret;
					}

					var propertyContent6 = obj.GetType( ).GetProperty( "Content6" );
					if (propertyContent6 == null)
					{
						ret.Content = new
						{
							Content1 = propertyContent1.GetValue( obj, null ),
							Content2 = propertyContent2.GetValue( obj, null ),
							Content3 = propertyContent3.GetValue( obj, null ),
							Content4 = propertyContent4.GetValue( obj, null ),
							Content5 = propertyContent5.GetValue( obj, null ),
						}.ToJsonString( );
						return ret;
					}

					var propertyContent7 = obj.GetType( ).GetProperty( "Content7" );
					if (propertyContent7 == null)
					{
						ret.Content = new
						{
							Content1 = propertyContent1.GetValue( obj, null ),
							Content2 = propertyContent2.GetValue( obj, null ),
							Content3 = propertyContent3.GetValue( obj, null ),
							Content4 = propertyContent4.GetValue( obj, null ),
							Content5 = propertyContent5.GetValue( obj, null ),
							Content6 = propertyContent6.GetValue( obj, null ),
						}.ToJsonString( );
						return ret;
					}

					var propertyContent8 = obj.GetType( ).GetProperty( "Content8" );
					if (propertyContent8 == null)
					{
						ret.Content = new
						{
							Content1 = propertyContent1.GetValue( obj, null ),
							Content2 = propertyContent2.GetValue( obj, null ),
							Content3 = propertyContent3.GetValue( obj, null ),
							Content4 = propertyContent4.GetValue( obj, null ),
							Content5 = propertyContent5.GetValue( obj, null ),
							Content6 = propertyContent6.GetValue( obj, null ),
							Content7 = propertyContent7.GetValue( obj, null ),
						}.ToJsonString( );
						return ret;
					}

					var propertyContent9 = obj.GetType( ).GetProperty( "Content9" );
					if (propertyContent9 == null)
					{
						ret.Content = new
						{
							Content1 = propertyContent1.GetValue( obj, null ),
							Content2 = propertyContent2.GetValue( obj, null ),
							Content3 = propertyContent3.GetValue( obj, null ),
							Content4 = propertyContent4.GetValue( obj, null ),
							Content5 = propertyContent5.GetValue( obj, null ),
							Content6 = propertyContent6.GetValue( obj, null ),
							Content7 = propertyContent7.GetValue( obj, null ),
							Content8 = propertyContent8.GetValue( obj, null ),
						}.ToJsonString( );
						return ret;
					}

					var propertyContent10 = obj.GetType( ).GetProperty( "Content10" );
					if (propertyContent10 == null)
					{
						ret.Content = new
						{
							Content1 = propertyContent1.GetValue( obj, null ),
							Content2 = propertyContent2.GetValue( obj, null ),
							Content3 = propertyContent3.GetValue( obj, null ),
							Content4 = propertyContent4.GetValue( obj, null ),
							Content5 = propertyContent5.GetValue( obj, null ),
							Content6 = propertyContent6.GetValue( obj, null ),
							Content7 = propertyContent7.GetValue( obj, null ),
							Content8 = propertyContent8.GetValue( obj, null ),
							Content9 = propertyContent9.GetValue( obj, null ),
						}.ToJsonString( );
						return ret;
					}
					else
					{
						ret.Content = new
						{
							Content1 = propertyContent1.GetValue( obj, null ),
							Content2 = propertyContent2.GetValue( obj, null ),
							Content3 = propertyContent3.GetValue( obj, null ),
							Content4 = propertyContent4.GetValue( obj, null ),
							Content5 = propertyContent5.GetValue( obj, null ),
							Content6 = propertyContent6.GetValue( obj, null ),
							Content7 = propertyContent7.GetValue( obj, null ),
							Content8 = propertyContent8.GetValue( obj, null ),
							Content9 = propertyContent9.GetValue( obj, null ),
							Content10 = propertyContent10.GetValue( obj, null ),
						}.ToJsonString( );
						return ret;
					}
				}
				return ret;
			}
			else
			{
				return OperateResult.CreateSuccessResult( obj == null ? string.Empty : obj.ToJsonString( ) );
			}
		}

		#endregion

	}
}
