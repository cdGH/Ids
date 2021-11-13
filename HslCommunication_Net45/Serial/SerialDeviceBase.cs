using HslCommunication.BasicFramework;
using HslCommunication.Core;
using HslCommunication.Core.Net;
using HslCommunication.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
#if !NET35 && !NET20
using System.Threading.Tasks;
#endif

namespace HslCommunication.Serial
{
#pragma warning disable CS1574 // XML 注释中有无法解析的 cref 特性
	/// <summary>
	/// 串口设备交互类的基类，实现了<see cref="IReadWriteDevice"/>接口的基础方法方法，需要使用继承重写来实现字节读写，bool读写操作。<br />
	/// The base class of the serial device interaction class, which implements the basic methods of the <see cref="IReadWriteDevice"/> interface, 
	/// requires inheritance rewriting to implement byte read and write, and bool read and write operations.
	/// </summary>
	/// <remarks>
	/// 本类实现了不同的数据类型的读写交互的api，继承自本类，重写下面的四个方法将可以实现你自己的设备通信对象
	/// <list type="number">
	/// <item>
	/// <see cref="Read(string, ushort)"/> 方法，读取字节数组的方法。
	/// </item>
	/// <item>
	/// <see cref="Write(string, byte[])"/> 方法，写入字节数组的方法。
	/// </item>
	/// <item>
	/// <see cref="ReadBool(string, ushort)"/> 方法，读取bool数组的方法。
	/// </item>
	/// <item>
	/// <see cref="Write(string, bool[])"/> 方法，写入bool数组的方法。
	/// </item>
	/// </list>
	/// 如果需要实现异步的方法。那就需要重写下面的四个方法。
	/// <list type="number">
	/// <item>
	/// <see cref="ReadAsync(string, ushort)"/> 方法，读取字节数组的方法。
	/// </item>
	/// <item>
	/// <see cref="WriteAsync(string, byte[])"/> 方法，写入字节数组的方法。
	/// </item>
	/// <item>
	/// <see cref="ReadBoolAsync(string, ushort)"/> 方法，读取bool数组的方法。
	/// </item>
	/// <item>
	/// <see cref="WriteAsync(string, bool[])"/> 方法，写入bool数组的方法。
	/// </item>
	/// </list>
	/// </remarks>
	public class SerialDeviceBase : SerialBase, IReadWriteDevice
#pragma warning restore CS1574 // XML 注释中有无法解析的 cref 特性
	{
		#region Constructor

		/// <summary>
		/// 默认的构造方法实现的设备信息
		/// </summary>
		public SerialDeviceBase( )
		{
			connectionId = SoftBasic.GetUniqueStringByGuidAndRandom( );
		}

		#endregion

		#region Public Member

		/// <inheritdoc cref="HslCommunication.Core.Net.NetworkDoubleBase.ByteTransform"/>
		public IByteTransform ByteTransform
		{
			get { return byteTransform; }
			set { byteTransform = value; }
		}

		/// <inheritdoc cref="HslCommunication.Core.Net.NetworkDoubleBase.ConnectionId"/>
		public string ConnectionId
		{
			get { return connectionId; }
			set { connectionId = value; }
		}

		#endregion

		#region Protect Member

		/// <inheritdoc cref="HslCommunication.Core.Net.NetworkDeviceBase.WordLength"/>
		protected ushort WordLength { get; set; } = 1;

		#endregion

		#region Private Member

		private IByteTransform byteTransform;                // 数据变换的接口
		private string connectionId = string.Empty;          // 当前连接

		#endregion

		#region Object Override

		/// <inheritdoc/>
		public override string ToString( ) => $"SerialDeviceBase<{byteTransform.GetType( )}>";

		#endregion

		// 以下内容和 NetworkDoubleBase 相同，因为不存在多继承，所以暂时选择了复制代码

		#region Read Write Bytes Bool

		/**************************************************************************************************
		 * 
		 *    说明：子类中需要重写基础的读取和写入方法，来支持不同的数据访问规则
		 *    
		 *    此处没有将读写位纳入进来，因为各种设备的支持不尽相同，比较麻烦
		 * 
		 **************************************************************************************************/

		/// <inheritdoc cref="IReadWriteNet.Read(string, ushort)"/>
		[HslMqttApi( "ReadByteArray", "" )]
		public virtual OperateResult<byte[]> Read( string address, ushort length ) => new OperateResult<byte[]>( StringResources.Language.NotSupportedFunction );

		/// <inheritdoc cref="IReadWriteNet.Write(string, byte[])"/>
		[HslMqttApi( "WriteByteArray", "" )]
		public virtual OperateResult Write( string address, byte[] value ) => new OperateResult( StringResources.Language.NotSupportedFunction );

		/*************************************************************************************************************
		 * 
		 * Bool类型的读写，不一定所有的设备都实现，比如西门子，就没有实现bool[]的读写，Siemens的fetch/write没有实现bool操作
		 * 
		 ************************************************************************************************************/

		/// <inheritdoc cref="IReadWriteNet.ReadBool(string, ushort)"/>
		[HslMqttApi( "ReadBoolArray", "" )]
		public virtual OperateResult<bool[]> ReadBool( string address, ushort length ) => new OperateResult<bool[]>( StringResources.Language.NotSupportedFunction );

		/// <inheritdoc cref="IReadWriteNet.ReadBool(string)"/>
		[HslMqttApi( "ReadBool", "" )]
		public virtual OperateResult<bool> ReadBool( string address ) => ByteTransformHelper.GetResultFromArray( ReadBool( address, 1 ) );

		/// <inheritdoc cref="IReadWriteNet.Write(string, bool[])"/>
		[HslMqttApi( "WriteBoolArray", "" )]
		public virtual OperateResult Write( string address, bool[] value ) => new OperateResult( StringResources.Language.NotSupportedFunction );

		/// <inheritdoc cref="IReadWriteNet.Write(string, bool)"/>
		[HslMqttApi( "WriteBool", "" )]
		public virtual OperateResult Write( string address, bool value ) => Write( address, new bool[] { value } );

		#endregion

		#region Customer Read Write

		/// <inheritdoc cref="IReadWriteNet.ReadCustomer{T}(string)"/>
		public OperateResult<T> ReadCustomer<T>( string address ) where T : IDataTransfer, new() => ReadWriteNetHelper.ReadCustomer<T>( this, address );

		/// <inheritdoc cref="IReadWriteNet.ReadCustomer{T}(string, T)"/>
		public OperateResult<T> ReadCustomer<T>( string address, T obj ) where T : IDataTransfer, new() => ReadWriteNetHelper.ReadCustomer( this, address, obj );

		/// <inheritdoc cref="IReadWriteNet.WriteCustomer{T}(string, T)"/>
		public OperateResult WriteCustomer<T>( string address, T data ) where T : IDataTransfer, new() => ReadWriteNetHelper.WriteCustomer( this, address, data );

		#endregion

		#region Reflection Read Write

		/// <inheritdoc cref="IReadWriteNet.Read{T}"/>
		public virtual OperateResult<T> Read<T>( ) where T : class, new() => HslReflectionHelper.Read<T>( this );

		/// <inheritdoc cref="IReadWriteNet.Write{T}(T)"/>
		public virtual OperateResult Write<T>( T data ) where T : class, new() => HslReflectionHelper.Write<T>( data, this );

		#endregion

		#region Read Support

		/// <inheritdoc cref="IReadWriteNet.ReadInt16(string)"/>
		[HslMqttApi( "ReadInt16", "" )]
		public OperateResult<short> ReadInt16( string address ) => ByteTransformHelper.GetResultFromArray( ReadInt16( address, 1 ) );

		/// <inheritdoc cref="IReadWriteNet.ReadInt16(string, ushort)"/>
		[HslMqttApi( "ReadInt16Array", "" )]
		public virtual OperateResult<short[]> ReadInt16( string address, ushort length ) => ByteTransformHelper.GetResultFromBytes( Read( address, (ushort)(length * WordLength) ), m => ByteTransform.TransInt16( m, 0, length ) );

		/// <inheritdoc cref="IReadWriteNet.ReadUInt16(string)"/>
		[HslMqttApi( "ReadUInt16", "" )]
		public OperateResult<ushort> ReadUInt16( string address ) => ByteTransformHelper.GetResultFromArray( ReadUInt16( address, 1 ) );

		/// <inheritdoc cref="IReadWriteNet.ReadUInt16(string, ushort)"/>
		[HslMqttApi( "ReadUInt16Array", "" )]
		public virtual OperateResult<ushort[]> ReadUInt16( string address, ushort length ) => ByteTransformHelper.GetResultFromBytes( Read( address, (ushort)(length * WordLength) ), m => ByteTransform.TransUInt16( m, 0, length ) );

		/// <inheritdoc cref="IReadWriteNet.ReadInt32(string)"/>
		[HslMqttApi( "ReadInt32", "" )]
		public OperateResult<int> ReadInt32( string address ) => ByteTransformHelper.GetResultFromArray( ReadInt32( address, 1 ) );

		/// <inheritdoc cref="IReadWriteNet.ReadInt32(string, ushort)"/>
		[HslMqttApi( "ReadInt32Array", "" )]
		public virtual OperateResult<int[]> ReadInt32( string address, ushort length ) => ByteTransformHelper.GetResultFromBytes( Read( address, (ushort)(length * WordLength * 2) ), m => ByteTransform.TransInt32( m, 0, length ) );

		/// <inheritdoc cref="IReadWriteNet.ReadUInt32(string)"/>
		[HslMqttApi( "ReadUInt32", "" )]
		public OperateResult<uint> ReadUInt32( string address ) => ByteTransformHelper.GetResultFromArray( ReadUInt32( address, 1 ) );

		/// <inheritdoc cref="IReadWriteNet.ReadUInt32(string, ushort)"/>
		[HslMqttApi( "ReadUInt32Array", "" )]
		public virtual OperateResult<uint[]> ReadUInt32( string address, ushort length ) => ByteTransformHelper.GetResultFromBytes( Read( address, (ushort)(length * WordLength * 2) ), m => ByteTransform.TransUInt32( m, 0, length ) );

		/// <inheritdoc cref="IReadWriteNet.ReadFloat(string)"/>
		[HslMqttApi( "ReadFloat", "" )]
		public OperateResult<float> ReadFloat( string address ) => ByteTransformHelper.GetResultFromArray( ReadFloat( address, 1 ) );

		/// <inheritdoc cref="IReadWriteNet.ReadFloat(string, ushort)"/>
		[HslMqttApi( "ReadFloatArray", "" )]
		public virtual OperateResult<float[]> ReadFloat( string address, ushort length ) => ByteTransformHelper.GetResultFromBytes( Read( address, (ushort)(length * WordLength * 2) ), m => ByteTransform.TransSingle( m, 0, length ) );

		/// <inheritdoc cref="IReadWriteNet.ReadInt64(string)"/>
		[HslMqttApi( "ReadInt64", "" )]
		public OperateResult<long> ReadInt64( string address ) => ByteTransformHelper.GetResultFromArray( ReadInt64( address, 1 ) );

		/// <inheritdoc cref="IReadWriteNet.ReadInt64(string, ushort)"/>
		[HslMqttApi( "ReadInt64Array", "" )]
		public virtual OperateResult<long[]> ReadInt64( string address, ushort length ) => ByteTransformHelper.GetResultFromBytes( Read( address, (ushort)(length * WordLength * 4) ), m => ByteTransform.TransInt64( m, 0, length ) );

		/// <inheritdoc cref="IReadWriteNet.ReadUInt64(string)"/>
		[HslMqttApi( "ReadUInt64", "" )]
		public OperateResult<ulong> ReadUInt64( string address ) => ByteTransformHelper.GetResultFromArray( ReadUInt64( address, 1 ) );

		/// <inheritdoc cref="IReadWriteNet.ReadUInt64(string, ushort)"/>
		[HslMqttApi( "ReadUInt64Array", "" )]
		public virtual OperateResult<ulong[]> ReadUInt64( string address, ushort length ) => ByteTransformHelper.GetResultFromBytes( Read( address, (ushort)(length * WordLength * 4) ), m => ByteTransform.TransUInt64( m, 0, length ) );

		/// <inheritdoc cref="IReadWriteNet.ReadDouble(string)"/>
		[HslMqttApi( "ReadDouble", "" )]
		public OperateResult<double> ReadDouble( string address ) => ByteTransformHelper.GetResultFromArray( ReadDouble( address, 1 ) );

		/// <inheritdoc cref="IReadWriteNet.ReadDouble(string, ushort)"/>
		[HslMqttApi( "ReadDoubleArray", "" )]
		public virtual OperateResult<double[]> ReadDouble( string address, ushort length ) => ByteTransformHelper.GetResultFromBytes( Read( address, (ushort)(length * WordLength * 4) ), m => ByteTransform.TransDouble( m, 0, length ) );

		/// <inheritdoc cref="IReadWriteNet.ReadString(string, ushort)"/>
		[HslMqttApi( "ReadString", "" )]
		public OperateResult<string> ReadString( string address, ushort length ) => ReadString( address, length, Encoding.ASCII );

		/// <inheritdoc cref="IReadWriteNet.ReadString(string, ushort, Encoding)"/>
		public virtual OperateResult<string> ReadString( string address, ushort length, Encoding encoding ) => ByteTransformHelper.GetResultFromBytes( Read( address, length ), m => ByteTransform.TransString( m, 0, m.Length, encoding ) );

		#endregion

		#region Write Support

		/// <inheritdoc cref="IReadWriteNet.Write(string, short[])"/>
		[HslMqttApi( "WriteInt16Array", "" )]
		public virtual OperateResult Write( string address, short[] values ) => Write( address, ByteTransform.TransByte( values ) );

		/// <inheritdoc cref="IReadWriteNet.Write(string, short)"/>
		[HslMqttApi( "WriteInt16", "" )]
		public virtual OperateResult Write( string address, short value ) => Write( address, new short[] { value } );

		/// <inheritdoc cref="IReadWriteNet.Write(string, ushort[])"/>
		[HslMqttApi( "WriteUInt16Array", "" )]
		public virtual OperateResult Write( string address, ushort[] values ) => Write( address, ByteTransform.TransByte( values ) );

		/// <inheritdoc cref="IReadWriteNet.Write(string, ushort)"/>
		[HslMqttApi( "WriteUInt16", "" )]
		public virtual OperateResult Write( string address, ushort value ) => Write( address, new ushort[] { value } );

		/// <inheritdoc cref="IReadWriteNet.Write(string, int[])"/>
		[HslMqttApi( "WriteInt32Array", "" )]
		public virtual OperateResult Write( string address, int[] values ) => Write( address, ByteTransform.TransByte( values ) );

		/// <inheritdoc cref="IReadWriteNet.Write(string, int)"/>
		[HslMqttApi( "WriteInt32", "" )]
		public OperateResult Write( string address, int value ) => Write( address, new int[] { value } );

		/// <inheritdoc cref="IReadWriteNet.Write(string, uint[])"/>
		[HslMqttApi( "WriteUInt32Array", "" )]
		public virtual OperateResult Write( string address, uint[] values ) => Write( address, ByteTransform.TransByte( values ) );

		/// <inheritdoc cref="IReadWriteNet.Write(string, uint)"/>
		[HslMqttApi( "WriteUInt32", "" )]
		public OperateResult Write( string address, uint value ) => Write( address, new uint[] { value } );

		/// <inheritdoc cref="IReadWriteNet.Write(string, float[])"/>
		[HslMqttApi( "WriteFloatArray", "" )]
		public virtual OperateResult Write( string address, float[] values ) => Write( address, ByteTransform.TransByte( values ) );

		/// <inheritdoc cref="IReadWriteNet.Write(string, float)"/>
		[HslMqttApi( "WriteFloat", "" )]
		public OperateResult Write( string address, float value ) => Write( address, new float[] { value } );

		/// <inheritdoc cref="IReadWriteNet.Write(string, long[])"/>
		[HslMqttApi( "WriteInt64Array", "" )]
		public virtual OperateResult Write( string address, long[] values ) => Write( address, ByteTransform.TransByte( values ) );

		/// <inheritdoc cref="IReadWriteNet.Write(string, long)"/>
		[HslMqttApi( "WriteInt64", "" )]
		public OperateResult Write( string address, long value ) => Write( address, new long[] { value } );

		/// <inheritdoc cref="IReadWriteNet.Write(string, ulong[])"/>
		[HslMqttApi( "WriteUInt64Array", "" )]
		public virtual OperateResult Write( string address, ulong[] values ) => Write( address, ByteTransform.TransByte( values ) );

		/// <inheritdoc cref="IReadWriteNet.Write(string, ulong)"/>
		[HslMqttApi( "WriteUInt64", "" )]
		public OperateResult Write( string address, ulong value ) => Write( address, new ulong[] { value } );

		/// <inheritdoc cref="IReadWriteNet.Write(string, double[])"/>
		[HslMqttApi( "WriteDoubleArray", "" )]
		public virtual OperateResult Write( string address, double[] values ) => Write( address, ByteTransform.TransByte( values ) );

		/// <inheritdoc cref="IReadWriteNet.Write(string, double)"/>
		[HslMqttApi( "WriteDouble", "" )]
		public OperateResult Write( string address, double value ) => Write( address, new double[] { value } );

		/// <inheritdoc cref="IReadWriteNet.Write(string, string)"/>
		[HslMqttApi( "WriteString", "" )]
		public virtual OperateResult Write( string address, string value ) => Write( address, value, Encoding.ASCII );

		/// <inheritdoc cref="IReadWriteNet.Write(string, string, int)"/>
		public virtual OperateResult Write( string address, string value, int length ) => Write( address, value, length, Encoding.ASCII );

		/// <inheritdoc cref="IReadWriteNet.Write(string, string, Encoding)"/>
		public virtual OperateResult Write( string address, string value, Encoding encoding )
		{
			byte[] temp = ByteTransform.TransByte( value, encoding );
			if (WordLength == 1) temp = SoftBasic.ArrayExpandToLengthEven( temp );
			return Write( address, temp );
		}

		/// <inheritdoc cref="IReadWriteNet.Write(string, string, int, Encoding)"/>
		public virtual OperateResult Write( string address, string value, int length, Encoding encoding )
		{
			byte[] temp = ByteTransform.TransByte( value, encoding );
			if (WordLength == 1) temp = SoftBasic.ArrayExpandToLengthEven( temp );
			temp = SoftBasic.ArrayExpandToLength( temp, length );
			return Write( address, temp );
		}

		#endregion

		#region Wait Support

		/// <inheritdoc cref="IReadWriteNet.Wait(string, bool, int, int)"/>
		[HslMqttApi( "WaitBool", "" )]
		public OperateResult<TimeSpan> Wait( string address, bool waitValue, int readInterval = 100, int waitTimeout = -1 ) => ReadWriteNetHelper.Wait( this, address, waitValue, readInterval, waitTimeout );

		/// <inheritdoc cref="IReadWriteNet.Wait(string, short, int, int)"/>
		[HslMqttApi( "WaitInt16", "" )]
		public OperateResult<TimeSpan> Wait( string address, short waitValue, int readInterval = 100, int waitTimeout = -1 ) => ReadWriteNetHelper.Wait( this, address, waitValue, readInterval, waitTimeout );

		/// <inheritdoc cref="IReadWriteNet.Wait(string, ushort, int, int)"/>
		[HslMqttApi( "WaitUInt16", "" )]
		public OperateResult<TimeSpan> Wait( string address, ushort waitValue, int readInterval = 100, int waitTimeout = -1 ) => ReadWriteNetHelper.Wait( this, address, waitValue, readInterval, waitTimeout );

		/// <inheritdoc cref="IReadWriteNet.Wait(string, int, int, int)"/>
		[HslMqttApi( "WaitInt32", "" )]
		public OperateResult<TimeSpan> Wait( string address, int waitValue, int readInterval = 100, int waitTimeout = -1 ) => ReadWriteNetHelper.Wait( this, address, waitValue, readInterval, waitTimeout );

		/// <inheritdoc cref="IReadWriteNet.Wait(string, uint, int, int)"/>
		[HslMqttApi( "WaitUInt32", "" )]
		public OperateResult<TimeSpan> Wait( string address, uint waitValue, int readInterval = 100, int waitTimeout = -1 ) => ReadWriteNetHelper.Wait( this, address, waitValue, readInterval, waitTimeout );

		/// <inheritdoc cref="IReadWriteNet.Wait(string, long, int, int)"/>
		[HslMqttApi( "WaitInt64", "" )]
		public OperateResult<TimeSpan> Wait( string address, long waitValue, int readInterval = 100, int waitTimeout = -1 ) => ReadWriteNetHelper.Wait( this, address, waitValue, readInterval, waitTimeout );

		/// <inheritdoc cref="IReadWriteNet.Wait(string, ulong, int, int)"/>
		[HslMqttApi( "WaitUInt64", "" )]
		public OperateResult<TimeSpan> Wait( string address, ulong waitValue, int readInterval = 100, int waitTimeout = -1 ) => ReadWriteNetHelper.Wait( this, address, waitValue, readInterval, waitTimeout );
#if !NET20 && !NET35

		/// <inheritdoc cref="IReadWriteNet.Wait(string, bool, int, int)"/>
		public async Task<OperateResult<TimeSpan>> WaitAsync( string address, bool waitValue, int readInterval = 100, int waitTimeout = -1 ) => await ReadWriteNetHelper.WaitAsync( this, address, waitValue, readInterval, waitTimeout );

		/// <inheritdoc cref="IReadWriteNet.Wait(string, short, int, int)"/>
		public async Task<OperateResult<TimeSpan>> WaitAsync( string address, short waitValue, int readInterval = 100, int waitTimeout = -1 ) => await ReadWriteNetHelper.WaitAsync( this, address, waitValue, readInterval, waitTimeout );

		/// <inheritdoc cref="IReadWriteNet.Wait(string, ushort, int, int)"/>
		public async Task<OperateResult<TimeSpan>> WaitAsync( string address, ushort waitValue, int readInterval = 100, int waitTimeout = -1 ) => await ReadWriteNetHelper.WaitAsync( this, address, waitValue, readInterval, waitTimeout );

		/// <inheritdoc cref="IReadWriteNet.Wait(string, int, int, int)"/>
		public async Task<OperateResult<TimeSpan>> WaitAsync( string address, int waitValue, int readInterval = 100, int waitTimeout = -1 ) => await ReadWriteNetHelper.WaitAsync( this, address, waitValue, readInterval, waitTimeout );

		/// <inheritdoc cref="IReadWriteNet.Wait(string, uint, int, int)"/>
		public async Task<OperateResult<TimeSpan>> WaitAsync( string address, uint waitValue, int readInterval = 100, int waitTimeout = -1 ) => await ReadWriteNetHelper.WaitAsync( this, address, waitValue, readInterval, waitTimeout );

		/// <inheritdoc cref="IReadWriteNet.Wait(string, long, int, int)"/>
		public async Task<OperateResult<TimeSpan>> WaitAsync( string address, long waitValue, int readInterval = 100, int waitTimeout = -1 ) => await ReadWriteNetHelper.WaitAsync( this, address, waitValue, readInterval, waitTimeout );

		/// <inheritdoc cref="IReadWriteNet.Wait(string, ulong, int, int)"/>
		public async Task<OperateResult<TimeSpan>> WaitAsync( string address, ulong waitValue, int readInterval = 100, int waitTimeout = -1 ) => await ReadWriteNetHelper.WaitAsync( this, address, waitValue, readInterval, waitTimeout );
#endif
		#endregion

		#region Asycn Read Write Bytes Bool
#if !NET35 && !NET20
		/// <inheritdoc cref="IReadWriteNet.ReadAsync(string, ushort)"/>
		public virtual async Task<OperateResult<byte[]>> ReadAsync( string address, ushort length ) => await Task.Run( ( ) => Read( address, length ) );

		/// <inheritdoc cref="IReadWriteNet.WriteAsync(string, byte[])"/>
		public virtual async Task<OperateResult> WriteAsync( string address, byte[] value ) => await Task.Run( ( ) => Write( address, value ) );


		/*************************************************************************************************************
		 * 
		 * Bool类型的读写，不一定所有的设备都实现，比如西门子，就没有实现bool[]的读写，Siemens的fetch/write没有实现bool操作
		 * 假设不进行任何的实现，那么就将使用同步代码来封装异步操作
		 * 
		 ************************************************************************************************************/

		/// <inheritdoc cref="IReadWriteNet.ReadBoolAsync(string, ushort)"/>
		public virtual async Task<OperateResult<bool[]>> ReadBoolAsync( string address, ushort length ) => await Task.Run( ( ) => ReadBool( address, length ) );

		/// <inheritdoc cref="IReadWriteNet.ReadBoolAsync(string)"/>
		public virtual async Task<OperateResult<bool>> ReadBoolAsync( string address ) => ByteTransformHelper.GetResultFromArray( await ReadBoolAsync( address, 1 ) );

		/// <inheritdoc cref="IReadWriteNet.WriteAsync(string, bool[])"/>
		public virtual async Task<OperateResult> WriteAsync( string address, bool[] value ) => await Task.Run( ( ) => Write( address, value ) );

		/// <inheritdoc cref="IReadWriteNet.WriteAsync(string, bool)"/>
		public virtual async Task<OperateResult> WriteAsync( string address, bool value ) => await WriteAsync( address, new bool[] { value } );
#endif
		#endregion

		#region Async Customer Read Write
#if !NET35 && !NET20
		/// <inheritdoc cref="IReadWriteNet.ReadCustomerAsync{T}(string)"/>
		public async Task<OperateResult<T>> ReadCustomerAsync<T>( string address ) where T : IDataTransfer, new() => await ReadWriteNetHelper.ReadCustomerAsync<T>( this, address );

		/// <inheritdoc cref="IReadWriteNet.ReadCustomerAsync{T}(string, T)"/>
		public async Task<OperateResult<T>> ReadCustomerAsync<T>( string address, T obj ) where T : IDataTransfer, new() => await ReadWriteNetHelper.ReadCustomerAsync( this, address, obj );

		/// <inheritdoc cref="IReadWriteNet.WriteCustomerAsync{T}(string, T)"/>
		public async Task<OperateResult> WriteCustomerAsync<T>( string address, T data ) where T : IDataTransfer, new() => await ReadWriteNetHelper.WriteCustomerAsync( this, address, data );
#endif
		#endregion

		#region Async Reflection Read Write
#if !NET35 && !NET20
		/// <inheritdoc cref="IReadWriteNet.ReadAsync{T}"/>
		public virtual async Task<OperateResult<T>> ReadAsync<T>( ) where T : class, new() => await HslReflectionHelper.ReadAsync<T>( this );

		/// <inheritdoc cref="IReadWriteNet.WriteAsync{T}(T)"/>
		public virtual async Task<OperateResult> WriteAsync<T>( T data ) where T : class, new() => await HslReflectionHelper.WriteAsync<T>( data, this );
#endif
		#endregion

		#region Async Read Support
#if !NET35 && !NET20
		/// <inheritdoc cref="IReadWriteNet.ReadInt16Async(string)"/>
		public async Task<OperateResult<short>> ReadInt16Async( string address ) => ByteTransformHelper.GetResultFromArray( await ReadInt16Async( address, 1 ) );

		/// <inheritdoc cref="IReadWriteNet.ReadInt16Async(string, ushort)"/>
		public virtual async Task<OperateResult<short[]>> ReadInt16Async( string address, ushort length ) => ByteTransformHelper.GetResultFromBytes( await ReadAsync( address, (ushort)(length * WordLength) ), m => ByteTransform.TransInt16( m, 0, length ) );

		/// <inheritdoc cref="IReadWriteNet.ReadUInt16Async(string)"/>
		public async Task<OperateResult<ushort>> ReadUInt16Async( string address ) => ByteTransformHelper.GetResultFromArray( await ReadUInt16Async( address, 1 ) );

		/// <inheritdoc cref="IReadWriteNet.ReadUInt16Async(string, ushort)"/>
		public virtual async Task<OperateResult<ushort[]>> ReadUInt16Async( string address, ushort length ) => ByteTransformHelper.GetResultFromBytes( await ReadAsync( address, (ushort)(length * WordLength) ), m => ByteTransform.TransUInt16( m, 0, length ) );

		/// <inheritdoc cref="IReadWriteNet.ReadInt32Async(string)"/>
		public async Task<OperateResult<int>> ReadInt32Async( string address ) => ByteTransformHelper.GetResultFromArray( await ReadInt32Async( address, 1 ) );

		/// <inheritdoc cref="IReadWriteNet.ReadInt32Async(string, ushort)"/>
		public virtual async Task<OperateResult<int[]>> ReadInt32Async( string address, ushort length ) => ByteTransformHelper.GetResultFromBytes( await ReadAsync( address, (ushort)(length * WordLength * 2) ), m => ByteTransform.TransInt32( m, 0, length ) );

		/// <inheritdoc cref="IReadWriteNet.ReadUInt32Async(string)"/>
		public async Task<OperateResult<uint>> ReadUInt32Async( string address ) => ByteTransformHelper.GetResultFromArray( await ReadUInt32Async( address, 1 ) );

		/// <inheritdoc cref="IReadWriteNet.ReadUInt32Async(string, ushort)"/>
		public virtual async Task<OperateResult<uint[]>> ReadUInt32Async( string address, ushort length ) => ByteTransformHelper.GetResultFromBytes( await ReadAsync( address, (ushort)(length * WordLength * 2) ), m => ByteTransform.TransUInt32( m, 0, length ) );

		/// <inheritdoc cref="IReadWriteNet.ReadFloatAsync(string)"/>
		public async Task<OperateResult<float>> ReadFloatAsync( string address ) => ByteTransformHelper.GetResultFromArray( await ReadFloatAsync( address, 1 ) );

		/// <inheritdoc cref="IReadWriteNet.ReadFloatAsync(string, ushort)"/>
		public virtual async Task<OperateResult<float[]>> ReadFloatAsync( string address, ushort length ) => ByteTransformHelper.GetResultFromBytes( await ReadAsync( address, (ushort)(length * WordLength * 2) ), m => ByteTransform.TransSingle( m, 0, length ) );

		/// <inheritdoc cref="IReadWriteNet.ReadInt64Async(string)"/>
		public async Task<OperateResult<long>> ReadInt64Async( string address ) => ByteTransformHelper.GetResultFromArray( await ReadInt64Async( address, 1 ) );

		/// <inheritdoc cref="IReadWriteNet.ReadInt64Async(string, ushort)"/>
		public virtual async Task<OperateResult<long[]>> ReadInt64Async( string address, ushort length ) => ByteTransformHelper.GetResultFromBytes( await ReadAsync( address, (ushort)(length * WordLength * 4) ), m => ByteTransform.TransInt64( m, 0, length ) );

		/// <inheritdoc cref="IReadWriteNet.ReadUInt64Async(string)"/>
		public async Task<OperateResult<ulong>> ReadUInt64Async( string address ) => ByteTransformHelper.GetResultFromArray( await ReadUInt64Async( address, 1 ) );

		/// <inheritdoc cref="IReadWriteNet.ReadUInt64Async(string, ushort)"/>
		public virtual async Task<OperateResult<ulong[]>> ReadUInt64Async( string address, ushort length ) => ByteTransformHelper.GetResultFromBytes( await ReadAsync( address, (ushort)(length * WordLength * 4) ), m => ByteTransform.TransUInt64( m, 0, length ) );

		/// <inheritdoc cref="IReadWriteNet.ReadDoubleAsync(string)"/>
		public async Task<OperateResult<double>> ReadDoubleAsync( string address ) => ByteTransformHelper.GetResultFromArray( await ReadDoubleAsync( address, 1 ) );

		/// <inheritdoc cref="IReadWriteNet.ReadDoubleAsync(string, ushort)"/>
		public virtual async Task<OperateResult<double[]>> ReadDoubleAsync( string address, ushort length ) => ByteTransformHelper.GetResultFromBytes( await ReadAsync( address, (ushort)(length * WordLength * 4) ), m => ByteTransform.TransDouble( m, 0, length ) );

		/// <inheritdoc cref="IReadWriteNet.ReadStringAsync(string, ushort)"/>
		public async Task<OperateResult<string>> ReadStringAsync( string address, ushort length ) => await ReadStringAsync( address, length, Encoding.ASCII );

		/// <inheritdoc cref="IReadWriteNet.ReadStringAsync(string, ushort, Encoding)"/>
		public virtual async Task<OperateResult<string>> ReadStringAsync( string address, ushort length, Encoding encoding ) => ByteTransformHelper.GetResultFromBytes( await ReadAsync( address, length ), m => ByteTransform.TransString( m, 0, m.Length, encoding ) );
#endif
		#endregion

		#region Async Write Support
#if !NET35 && !NET20
		/// <inheritdoc cref="IReadWriteNet.WriteAsync(string, short[])"/>
		public virtual async Task<OperateResult> WriteAsync( string address, short[] values ) => await WriteAsync( address, ByteTransform.TransByte( values ) );

		/// <inheritdoc cref="IReadWriteNet.WriteAsync(string, short)"/>
		public virtual async Task<OperateResult> WriteAsync( string address, short value ) => await WriteAsync( address, new short[] { value } );

		/// <inheritdoc cref="IReadWriteNet.WriteAsync(string, ushort[])"/>
		public virtual async Task<OperateResult> WriteAsync( string address, ushort[] values ) => await WriteAsync( address, ByteTransform.TransByte( values ) );

		/// <inheritdoc cref="IReadWriteNet.WriteAsync(string, ushort)"/>
		public virtual async Task<OperateResult> WriteAsync( string address, ushort value ) => await WriteAsync( address, new ushort[] { value } );

		/// <inheritdoc cref="IReadWriteNet.WriteAsync(string, int[])"/>
		public virtual async Task<OperateResult> WriteAsync( string address, int[] values ) => await WriteAsync( address, ByteTransform.TransByte( values ) );

		/// <inheritdoc cref="IReadWriteNet.WriteAsync(string, int)"/>
		public async Task<OperateResult> WriteAsync( string address, int value ) => await WriteAsync( address, new int[] { value } );

		/// <inheritdoc cref="IReadWriteNet.WriteAsync(string, uint[])"/>
		public virtual async Task<OperateResult> WriteAsync( string address, uint[] values ) => await WriteAsync( address, ByteTransform.TransByte( values ) );

		/// <inheritdoc cref="IReadWriteNet.WriteAsync(string, uint)"/>
		public async Task<OperateResult> WriteAsync( string address, uint value ) => await WriteAsync( address, new uint[] { value } );

		/// <inheritdoc cref="IReadWriteNet.WriteAsync(string, float[])"/>
		public virtual async Task<OperateResult> WriteAsync( string address, float[] values ) => await WriteAsync( address, ByteTransform.TransByte( values ) );

		/// <inheritdoc cref="IReadWriteNet.WriteAsync(string, float)"/>
		public async Task<OperateResult> WriteAsync( string address, float value ) => await WriteAsync( address, new float[] { value } );

		/// <inheritdoc cref="IReadWriteNet.WriteAsync(string, long[])"/>
		public virtual async Task<OperateResult> WriteAsync( string address, long[] values ) => await WriteAsync( address, ByteTransform.TransByte( values ) );

		/// <inheritdoc cref="IReadWriteNet.WriteAsync(string, long)"/>
		public async Task<OperateResult> WriteAsync( string address, long value ) => await WriteAsync( address, new long[] { value } );

		/// <inheritdoc cref="IReadWriteNet.WriteAsync(string, ulong[])"/>
		public virtual async Task<OperateResult> WriteAsync( string address, ulong[] values ) => await WriteAsync( address, ByteTransform.TransByte( values ) );

		/// <inheritdoc cref="IReadWriteNet.WriteAsync(string, ulong)"/>
		public async Task<OperateResult> WriteAsync( string address, ulong value ) => await WriteAsync( address, new ulong[] { value } );

		/// <inheritdoc cref="IReadWriteNet.WriteAsync(string, double[])"/>
		public virtual async Task<OperateResult> WriteAsync( string address, double[] values ) => await WriteAsync( address, ByteTransform.TransByte( values ) );

		/// <inheritdoc cref="IReadWriteNet.WriteAsync(string, double)"/>
		public async Task<OperateResult> WriteAsync( string address, double value ) => await WriteAsync( address, new double[] { value } );

		/// <inheritdoc cref="IReadWriteNet.WriteAsync(string, string)" />
		public virtual async Task<OperateResult> WriteAsync( string address, string value ) => await WriteAsync( address, value, Encoding.ASCII );

		/// <inheritdoc cref="IReadWriteNet.WriteAsync(string, string, Encoding)"/>
		public virtual async Task<OperateResult> WriteAsync( string address, string value, Encoding encoding )
		{
			byte[] temp = ByteTransform.TransByte( value, encoding );
			if (WordLength == 1) temp = SoftBasic.ArrayExpandToLengthEven( temp );
			return await WriteAsync( address, temp );
		}

		/// <inheritdoc cref="IReadWriteNet.WriteAsync(string, string, int)"/>
		public virtual async Task<OperateResult> WriteAsync( string address, string value, int length ) => await WriteAsync( address, value, length, Encoding.ASCII );

		/// <inheritdoc cref="IReadWriteNet.WriteAsync(string, string, int, Encoding)"/>
		public virtual async Task<OperateResult> WriteAsync( string address, string value, int length, Encoding encoding )
		{
			byte[] temp = ByteTransform.TransByte( value, encoding );
			if (WordLength == 1) temp = SoftBasic.ArrayExpandToLengthEven( temp );
			temp = SoftBasic.ArrayExpandToLength( temp, length );
			return await WriteAsync( address, temp );
		}
#endif
		#endregion
	}
}
