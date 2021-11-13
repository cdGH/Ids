using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


/*********************************************************************************************************
 * 
 *    说明：
 * 
 *    统一的数据转换中心，目前共有3种转换的机制
 *    1. 常规转换，字节顺序和计算机的顺序一致，小端表示，不需要颠倒，比如三菱PLC的MC协议，Hsl通信协议
 *    2. 反转转换，字节顺序和计算机的相反，大端表示，需要完全颠倒，比如西门子PLC
 *    3. 以2字节为单位颠倒转换，比如Modbus协议
 *    
 *    当你的数据是4个字节甚至更大的时候，就需要进行设置DataFormat属性了，才能解析出正确的数据
 * 
 *********************************************************************************************************/
 


namespace HslCommunication.Core
{
	/// <summary>
	/// 支持转换器的基础接口，规定了实际的数据类型和字节数组进行相互转换的方法。
	/// </summary>
	/// <remarks>
	/// 所有的设备通讯类都内置了该转换的模型，并且已经配置好数据的高地位模式，可以方便的转换信息
	/// </remarks>
	public interface IByteTransform
	{
		#region Get Value From Bytes

		/// <summary>
		/// 从缓存中提取出bool结果
		/// </summary>
		/// <param name="buffer">缓存数据</param>
		/// <param name="index">位的索引</param>
		/// <returns>bool对象</returns>
		bool TransBool( byte[] buffer, int index );

		/// <summary>
		/// 从缓存中提取出bool数组结果
		/// </summary>
		/// <param name="buffer">缓存数据</param>
		/// <param name="index">位的索引</param>
		/// <param name="length">bool长度</param>
		/// <returns>bool数组</returns>
		bool[] TransBool( byte[] buffer, int index, int length );

		/// <summary>
		/// 从缓存中提取byte结果
		/// </summary>
		/// <param name="buffer">缓存数据</param>
		/// <param name="index">索引位置</param>
		/// <returns>byte对象</returns>
		byte TransByte( byte[] buffer, int index );

		/// <summary>
		/// 从缓存中提取byte数组结果
		/// </summary>
		/// <param name="buffer">缓存数据</param>
		/// <param name="index">索引位置</param>
		/// <param name="length">读取的数组长度</param>
		/// <returns>byte数组对象</returns>
		byte[] TransByte( byte[] buffer, int index, int length );

		/// <summary>
		/// 从缓存中提取short结果
		/// </summary>
		/// <param name="buffer">缓存数据</param>
		/// <param name="index">索引位置</param>
		/// <returns>short对象</returns>
		short TransInt16( byte[] buffer, int index );

		/// <summary>
		/// 从缓存中提取short数组结果
		/// </summary>
		/// <param name="buffer">缓存数据</param>
		/// <param name="index">索引位置</param>
		/// <param name="length">读取的数组长度</param>
		/// <returns>short数组对象</returns>
		short[] TransInt16( byte[] buffer, int index, int length );

		/// <summary>
		/// 从缓存中提取short二维数组结果
		/// </summary>
		/// <param name="buffer">缓存数据</param>
		/// <param name="index">索引位置</param>
		/// <param name="row">二维数组行</param>
		/// <param name="col">二维数组列</param>
		/// <returns>二维short数组</returns>
		short[,] TransInt16( byte[] buffer, int index, int row, int col );

		/// <summary>
		/// 从缓存中提取ushort结果
		/// </summary>
		/// <param name="buffer">缓存数据</param>
		/// <param name="index">索引位置</param>
		/// <returns>ushort对象</returns>
		ushort TransUInt16( byte[] buffer, int index );

		/// <summary>
		/// 从缓存中提取ushort数组结果
		/// </summary>
		/// <param name="buffer">缓存数据</param>
		/// <param name="index">索引位置</param>
		/// <param name="length">读取的数组长度</param>
		/// <returns>ushort数组对象</returns>
		ushort[] TransUInt16( byte[] buffer, int index, int length );

		/// <summary>
		/// 从缓存中提取ushort二维数组结果
		/// </summary>
		/// <param name="buffer">缓存数据</param>
		/// <param name="index">索引位置</param>
		/// <param name="row">二维数组行</param>
		/// <param name="col">二维数组列</param>
		/// <returns>二维ushort数组</returns>
		ushort[,] TransUInt16( byte[] buffer, int index, int row, int col );

		/// <summary>
		/// 从缓存中提取int结果
		/// </summary>
		/// <param name="buffer">缓存数据</param>
		/// <param name="index">索引位置</param>
		/// <returns>int对象</returns>
		int TransInt32( byte[] buffer, int index );

		/// <summary>
		/// 从缓存中提取int数组结果
		/// </summary>
		/// <param name="buffer">缓存数据</param>
		/// <param name="index">索引位置</param>
		/// <param name="length">读取的数组长度</param>
		/// <returns>int数组对象</returns>
		int[] TransInt32( byte[] buffer, int index, int length );

		/// <summary>
		/// 从缓存中提取int二维数组结果
		/// </summary>
		/// <param name="buffer">缓存数据</param>
		/// <param name="index">索引位置</param>
		/// <param name="row">二维数组行</param>
		/// <param name="col">二维数组列</param>
		/// <returns>二维int数组</returns>
		int[,] TransInt32( byte[] buffer, int index, int row, int col );

		/// <summary>
		/// 从缓存中提取uint结果
		/// </summary>
		/// <param name="buffer">缓存数据</param>
		/// <param name="index">索引位置</param>
		/// <returns>uint对象</returns>
		uint TransUInt32( byte[] buffer, int index );

		/// <summary>
		/// 从缓存中提取uint数组结果
		/// </summary>
		/// <param name="buffer">缓存数据</param>
		/// <param name="index">索引位置</param>
		/// <param name="length">读取的数组长度</param>
		/// <returns>uint数组对象</returns>
		uint[] TransUInt32( byte[] buffer, int index, int length );

		/// <summary>
		/// 从缓存中提取uint二维数组结果
		/// </summary>
		/// <param name="buffer">缓存数据</param>
		/// <param name="index">索引位置</param>
		/// <param name="row">二维数组行</param>
		/// <param name="col">二维数组列</param>
		/// <returns>uint二维数组对象</returns>
		uint[,] TransUInt32( byte[] buffer, int index, int row, int col );

		/// <summary>
		/// 从缓存中提取long结果
		/// </summary>
		/// <param name="buffer">缓存数据</param>
		/// <param name="index">索引位置</param>
		/// <returns>long对象</returns>
		long TransInt64( byte[] buffer, int index );

		/// <summary>
		/// 从缓存中提取long数组结果
		/// </summary>
		/// <param name="buffer">缓存数据</param>
		/// <param name="index">索引位置</param>
		/// <param name="length">读取的数组长度</param>
		/// <returns>long数组对象</returns>
		long[] TransInt64( byte[] buffer, int index, int length );

		/// <summary>
		/// 从缓存中提取long二维数组结果
		/// </summary>
		/// <param name="buffer">缓存数据</param>
		/// <param name="index">索引位置</param>
		/// <param name="row">二维数组行</param>
		/// <param name="col">二维数组列</param>
		/// <returns>long二维数组对象</returns>
		long[,] TransInt64( byte[] buffer, int index, int row, int col );

		/// <summary>
		/// 从缓存中提取ulong结果
		/// </summary>
		/// <param name="buffer">缓存数据</param>
		/// <param name="index">索引位置</param>
		/// <returns>ulong对象</returns>
		ulong TransUInt64( byte[] buffer, int index );

		/// <summary>
		/// 从缓存中提取ulong数组结果
		/// </summary>
		/// <param name="buffer">缓存数据</param>
		/// <param name="index">索引位置</param>
		/// <param name="length">读取的数组长度</param>
		/// <returns>ulong数组对象</returns>
		ulong[] TransUInt64( byte[] buffer, int index, int length );

		/// <summary>
		/// 从缓存中提取ulong二维数组结果
		/// </summary>
		/// <param name="buffer">缓存数据</param>
		/// <param name="index">索引位置</param>
		/// <param name="row">二维数组行</param>
		/// <param name="col">二维数组列</param>
		/// <returns>ulong二维数组对象</returns>
		ulong[,] TransUInt64( byte[] buffer, int index, int row, int col );

		/// <summary>
		/// 从缓存中提取float结果
		/// </summary>
		/// <param name="buffer">缓存对象</param>
		/// <param name="index">索引位置</param>
		/// <returns>float对象</returns>
		float TransSingle( byte[] buffer, int index );

		/// <summary>
		/// 从缓存中提取float数组结果
		/// </summary>
		/// <param name="buffer">缓存数据</param>
		/// <param name="index">索引位置</param>
		/// <param name="length">读取的数组长度</param>
		/// <returns>float数组</returns>
		float[] TransSingle( byte[] buffer, int index, int length );

		/// <summary>
		/// 从缓存中提取float二维数组结果
		/// </summary>
		/// <param name="buffer">缓存数据</param>
		/// <param name="index">索引位置</param>
		/// <param name="row">二维数组行</param>
		/// <param name="col">二维数组列</param>
		/// <returns>float二维数组对象</returns>
		float[,] TransSingle( byte[] buffer, int index, int row, int col );

		/// <summary>
		/// 从缓存中提取double结果
		/// </summary>
		/// <param name="buffer">缓存对象</param>
		/// <param name="index">索引位置</param>
		/// <returns>double对象</returns>
		double TransDouble( byte[] buffer, int index );

		/// <summary>
		/// 从缓存中提取double数组结果
		/// </summary>
		/// <param name="buffer">缓存对象</param>
		/// <param name="index">索引位置</param>
		/// <param name="length">读取的数组长度</param>
		/// <returns>double数组</returns>
		double[] TransDouble( byte[] buffer, int index, int length );

		/// <summary>
		/// 从缓存中提取double二维数组结果
		/// </summary>
		/// <param name="buffer">缓存数据</param>
		/// <param name="index">索引位置</param>
		/// <param name="row">二维数组行</param>
		/// <param name="col">二维数组列</param>
		/// <returns>double二维数组对象</returns>
		double[,] TransDouble( byte[] buffer, int index, int row, int col );

		/// <summary>
		/// 从缓存中提取string结果，使用指定的编码
		/// </summary>
		/// <param name="buffer">缓存对象</param>
		/// <param name="encoding">字符串的编码</param>
		/// <returns>string对象</returns>
		string TransString( byte[] buffer, Encoding encoding );

		/// <summary>
		/// 从缓存中提取string结果，使用指定的编码
		/// </summary>
		/// <param name="buffer">缓存对象</param>
		/// <param name="index">索引位置</param>
		/// <param name="length">byte数组长度</param>
		/// <param name="encoding">字符串的编码</param>
		/// <returns>string对象</returns>
		string TransString( byte[] buffer, int index, int length, Encoding encoding );

		#endregion

		#region Get Bytes From Value

		/// <summary>
		/// bool变量转化缓存数据
		/// </summary>
		/// <param name="value">等待转化的数据</param>
		/// <returns>buffer数据</returns>
		byte[] TransByte( bool value );
		
		/// <summary>
		/// bool数组变量转化缓存数据
		/// </summary>
		/// <param name="values">等待转化的数组</param>
		/// <returns>buffer数据</returns>
		byte[] TransByte( bool[] values );

		/// <summary>
		/// byte变量转化缓存数据
		/// </summary>
		/// <param name="value">等待转化的数据</param>
		/// <returns>buffer数据</returns>
		byte[] TransByte( byte value );

		/// <summary>
		/// short变量转化缓存数据
		/// </summary>
		/// <param name="value">等待转化的数据</param>
		/// <returns>buffer数据</returns>
		byte[] TransByte( short value );

		/// <summary>
		/// short数组变量转化缓存数据
		/// </summary>
		/// <param name="values">等待转化的数组</param>
		/// <returns>buffer数据</returns>
		byte[] TransByte( short[] values );

		/// <summary>
		/// ushort变量转化缓存数据
		/// </summary>
		/// <param name="value">等待转化的数据</param>
		/// <returns>buffer数据</returns>
		byte[] TransByte( ushort value );

		/// <summary>
		/// ushort数组变量转化缓存数据
		/// </summary>
		/// <param name="values">等待转化的数组</param>
		/// <returns>buffer数据</returns>
		byte[] TransByte( ushort[] values );

		/// <summary>
		/// int变量转化缓存数据
		/// </summary>
		/// <param name="value">等待转化的数据</param>
		/// <returns>buffer数据</returns>
		byte[] TransByte( int value );

		/// <summary>
		/// int数组变量转化缓存数据
		/// </summary>
		/// <param name="values">等待转化的数组</param>
		/// <returns>buffer数据</returns>
		byte[] TransByte( int[] values );

		/// <summary>
		/// uint变量转化缓存数据
		/// </summary>
		/// <param name="value">等待转化的数据</param>
		/// <returns>buffer数据</returns>
		byte[] TransByte( uint value );

		/// <summary>
		/// uint数组变量转化缓存数据
		/// </summary>
		/// <param name="values">等待转化的数组</param>
		/// <returns>buffer数据</returns>
		byte[] TransByte( uint[] values );

		/// <summary>
		/// long变量转化缓存数据
		/// </summary>
		/// <param name="value">等待转化的数据</param>
		/// <returns>buffer数据</returns>
		byte[] TransByte( long value );

		/// <summary>
		/// long数组变量转化缓存数据
		/// </summary>
		/// <param name="values">等待转化的数组</param>
		/// <returns>buffer数据</returns>
		byte[] TransByte( long[] values );

		/// <summary>
		/// ulong变量转化缓存数据
		/// </summary>
		/// <param name="value">等待转化的数据</param>
		/// <returns>buffer数据</returns>
		byte[] TransByte( ulong value );

		/// <summary>
		/// ulong数组变量转化缓存数据
		/// </summary>
		/// <param name="values">等待转化的数组</param>
		/// <returns>buffer数据</returns>
		byte[] TransByte( ulong[] values );

		/// <summary>
		/// float变量转化缓存数据
		/// </summary>
		/// <param name="value">等待转化的数据</param>
		/// <returns>buffer数据</returns>
		byte[] TransByte( float value );

		/// <summary>
		/// float数组变量转化缓存数据
		/// </summary>
		/// <param name="values">等待转化的数组</param>
		/// <returns>buffer数据</returns>
		byte[] TransByte( float[] values );

		/// <summary>
		/// double变量转化缓存数据
		/// </summary>
		/// <param name="value">等待转化的数据</param>
		/// <returns>buffer数据</returns>
		byte[] TransByte( double value );

		/// <summary>
		/// double数组变量转化缓存数据
		/// </summary>
		/// <param name="values">等待转化的数组</param>
		/// <returns>buffer数据</returns>
		byte[] TransByte( double[] values );

		/// <summary>
		/// 使用指定的编码字符串转化缓存数据
		/// </summary>
		/// <param name="value">等待转化的数据</param>
		/// <param name="encoding">字符串的编码方式</param>
		/// <returns>buffer数据</returns>
		byte[] TransByte( string value, Encoding encoding );

		/// <summary>
		/// 使用指定的编码字符串转化缓存数据
		/// </summary>
		/// <param name="value">等待转化的数据</param>
		/// <param name="length">转换之后的数据长度</param>
		/// <param name="encoding">字符串的编码方式</param>
		/// <returns>buffer数据</returns>
		byte[] TransByte( string value, int length, Encoding encoding );

		#endregion

		#region Public Properties

		/// <summary>
		/// 获取或设置数据解析的格式，可选ABCD, BADC，CDAB，DCBA格式，对int,uint,float,double,long,ulong类型有作用<br />
		/// Get or set the format of the data analysis, optional ABCD, BADC, CDAB, DCBA format, effective for int, uint, float, double, long, ulong type
		/// </summary>
		DataFormat DataFormat { get; set; }

		/// <summary>
		/// 获取或设置在解析字符串的时候是否将字节按照字单位反转<br />
		/// Gets or sets whether to reverse the bytes in word units when parsing strings
		/// </summary>
		bool IsStringReverseByteWord { get; set; }

		/// <summary>
		/// 根据指定的<see cref="DataFormat"/>格式，来实例化一个新的对象，除了<see cref="DataFormat"/>不同，其他都相同<br />
		/// According to the specified <see cref="DataFormat"/> format, to instantiate a new object, except that <see cref="DataFormat"/> is different, everything else is the same
		/// </summary>
		/// <param name="dataFormat">数据格式</param>
		/// <returns>新的<see cref="IByteTransform"/>对象</returns>
		IByteTransform CreateByDateFormat( DataFormat dataFormat );

		#endregion

	}

}
