using HslCommunication.BasicFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HslCommunication.Core
{
	/// <summary>
	/// 数据转换类的基础，提供了一些基础的方法实现.<br />
	/// The basis of the data conversion class provides some basic method implementations.
	/// </summary>
	public class ByteTransformBase : IByteTransform
	{
		#region Constructor

		/// <summary>
		/// 实例化一个默认的对象<br />
		/// Instantiate a default object
		/// </summary>
		public ByteTransformBase( )
		{
			DataFormat = DataFormat.DCBA;
		}

		/// <summary>
		/// 使用指定的数据解析来实例化对象<br />
		/// Instantiate the object using the specified data parsing
		/// </summary>
		/// <param name="dataFormat">数据规则</param>
		public ByteTransformBase( DataFormat dataFormat )
		{
			this.DataFormat = dataFormat;
		}

		#endregion

		#region Get Value From Bytes

		/// <inheritdoc cref="IByteTransform.TransBool(byte[], int)"/>
		public virtual bool TransBool( byte[] buffer, int index ) => (buffer[index] & 0x01) == 0x01;

		/// <inheritdoc cref="IByteTransform.TransBool(byte[], int, int)"/>
		public bool[] TransBool( byte[] buffer, int index, int length )
		{
			byte[] tmp = new byte[length];
			Array.Copy( buffer, index, tmp, 0, length );
			return SoftBasic.ByteToBoolArray( tmp, length * 8 );
		}

		/// <inheritdoc cref="IByteTransform.TransByte(byte[], int)"/>
		public virtual byte TransByte( byte[] buffer, int index ) => buffer[index];

		/// <inheritdoc cref="IByteTransform.TransByte(byte[], int, int)"/>
		public virtual byte[] TransByte( byte[] buffer, int index, int length )
		{
			byte[] tmp = new byte[length];
			Array.Copy( buffer, index, tmp, 0, length );
			return tmp;
		}

		/// <inheritdoc cref="IByteTransform.TransInt16(byte[], int)"/>
		public virtual short TransInt16( byte[] buffer, int index ) => BitConverter.ToInt16( buffer, index );

		/// <inheritdoc cref="IByteTransform.TransInt16(byte[], int, int)"/>
		public virtual short[] TransInt16( byte[] buffer, int index, int length )
		{
			short[] tmp = new short[length];
			for (int i = 0; i < length; i++)
			{
				tmp[i] = TransInt16( buffer, index + 2 * i );
			}
			return tmp;
		}

		/// <inheritdoc cref="IByteTransform.TransInt16(byte[], int, int, int)"/>
		public short[,] TransInt16( byte[] buffer, int index, int row, int col )
		{
			return HslHelper.CreateTwoArrayFromOneArray( TransInt16( buffer, index, row * col ), row, col );
		}

		/// <inheritdoc cref="IByteTransform.TransUInt16(byte[], int)"/>
		public virtual ushort TransUInt16( byte[] buffer, int index ) => BitConverter.ToUInt16( buffer, index );

		/// <inheritdoc cref="IByteTransform.TransUInt16(byte[], int, int)"/>
		public virtual ushort[] TransUInt16( byte[] buffer, int index, int length )
		{
			ushort[] tmp = new ushort[length];
			for (int i = 0; i < length; i++)
			{
				tmp[i] = TransUInt16( buffer, index + 2 * i );
			}
			return tmp;
		}

		/// <inheritdoc cref="IByteTransform.TransUInt16(byte[], int, int, int)"/>
		public ushort[,] TransUInt16( byte[] buffer, int index, int row, int col )
		{
			return HslHelper.CreateTwoArrayFromOneArray( TransUInt16( buffer, index, row * col ), row, col );
		}

		/// <inheritdoc cref="IByteTransform.TransInt32(byte[], int)"/>
		public virtual int TransInt32( byte[] buffer, int index ) => BitConverter.ToInt32( ByteTransDataFormat4( buffer, index ), 0 );

		/// <inheritdoc cref="IByteTransform.TransInt32(byte[], int, int)"/>
		public virtual int[] TransInt32( byte[] buffer, int index, int length )
		{
			int[] tmp = new int[length];
			for (int i = 0; i < length; i++)
			{
				tmp[i] = TransInt32( buffer, index + 4 * i );
			}
			return tmp;
		}

		/// <inheritdoc cref="IByteTransform.TransInt32(byte[], int, int, int)"/>
		public int[,] TransInt32( byte[] buffer, int index, int row, int col )
		{
			return HslHelper.CreateTwoArrayFromOneArray( TransInt32( buffer, index, row * col ), row, col );
		}

		/// <inheritdoc cref="IByteTransform.TransUInt32(byte[], int)"/>
		public virtual uint TransUInt32( byte[] buffer, int index ) => BitConverter.ToUInt32( ByteTransDataFormat4( buffer, index ), 0 );

		/// <inheritdoc cref="IByteTransform.TransUInt32(byte[], int, int)"/>
		public virtual uint[] TransUInt32( byte[] buffer, int index, int length )
		{
			uint[] tmp = new uint[length];
			for (int i = 0; i < length; i++)
			{
				tmp[i] = TransUInt32( buffer, index + 4 * i );
			}
			return tmp;
		}

		/// <inheritdoc cref="IByteTransform.TransUInt32(byte[], int, int, int)"/>
		public uint[,] TransUInt32( byte[] buffer, int index, int row, int col )
		{
			return HslHelper.CreateTwoArrayFromOneArray( TransUInt32( buffer, index, row * col ), row, col );
		}


		/// <inheritdoc cref="IByteTransform.TransInt64(byte[], int)"/>
		public virtual long TransInt64( byte[] buffer, int index ) => BitConverter.ToInt64( ByteTransDataFormat8( buffer, index ), 0 );

		/// <inheritdoc cref="IByteTransform.TransInt64(byte[], int, int)"/>
		public virtual long[] TransInt64( byte[] buffer, int index, int length )
		{
			long[] tmp = new long[length];
			for (int i = 0; i < length; i++)
			{
				tmp[i] = TransInt64( buffer, index + 8 * i );
			}
			return tmp;
		}

		/// <inheritdoc cref="IByteTransform.TransInt64(byte[], int, int, int)"/>
		public long[,] TransInt64( byte[] buffer, int index, int row, int col )
		{
			return HslHelper.CreateTwoArrayFromOneArray( TransInt64( buffer, index, row * col ), row, col );
		}

		/// <inheritdoc cref="IByteTransform.TransUInt64(byte[], int)"/>
		public virtual ulong TransUInt64( byte[] buffer, int index ) => BitConverter.ToUInt64( ByteTransDataFormat8( buffer, index ), 0 );

		/// <inheritdoc cref="IByteTransform.TransUInt64(byte[], int, int)"/>
		public virtual ulong[] TransUInt64( byte[] buffer, int index, int length )
		{
			ulong[] tmp = new ulong[length];
			for (int i = 0; i < length; i++)
			{
				tmp[i] = TransUInt64( buffer, index + 8 * i );
			}
			return tmp;
		}

		/// <inheritdoc cref="IByteTransform.TransUInt64(byte[], int, int, int)"/>
		public ulong[,] TransUInt64( byte[] buffer, int index, int row, int col )
		{
			return HslHelper.CreateTwoArrayFromOneArray( TransUInt64( buffer, index, row * col ), row, col );
		}

		/// <inheritdoc cref="IByteTransform.TransSingle(byte[], int)"/>
		public virtual float TransSingle( byte[] buffer, int index ) => BitConverter.ToSingle( ByteTransDataFormat4( buffer, index ), 0 );

		/// <inheritdoc cref="IByteTransform.TransSingle(byte[], int, int)"/>
		public virtual float[] TransSingle( byte[] buffer, int index, int length )
		{
			float[] tmp = new float[length];
			for (int i = 0; i < length; i++)
			{
				tmp[i] = TransSingle( buffer, index + 4 * i );
			}
			return tmp;
		}

		/// <inheritdoc cref="IByteTransform.TransSingle(byte[], int, int, int)"/>
		public float[,] TransSingle( byte[] buffer, int index, int row, int col )
		{
			return HslHelper.CreateTwoArrayFromOneArray( TransSingle( buffer, index, row * col ), row, col );
		}

		/// <inheritdoc cref="IByteTransform.TransDouble(byte[], int)"/>
		public virtual double TransDouble( byte[] buffer, int index ) => BitConverter.ToDouble( ByteTransDataFormat8( buffer, index ), 0 );

		/// <inheritdoc cref="IByteTransform.TransDouble(byte[], int, int)"/>
		public virtual double[] TransDouble( byte[] buffer, int index, int length )
		{
			double[] tmp = new double[length];
			for (int i = 0; i < length; i++)
			{
				tmp[i] = TransDouble( buffer, index + 8 * i );
			}
			return tmp;
		}

		/// <inheritdoc cref="IByteTransform.TransDouble(byte[], int, int, int)"/>
		public double[,] TransDouble( byte[] buffer, int index, int row, int col )
		{
			return HslHelper.CreateTwoArrayFromOneArray( TransDouble( buffer, index, row * col ), row, col );
		}

		/// <inheritdoc cref="IByteTransform.TransString(byte[], int, int, Encoding)"/>
		public virtual string TransString( byte[] buffer, int index, int length, Encoding encoding )
		{
			byte[] tmp = TransByte( buffer, index, length );
			if (IsStringReverseByteWord)
				return encoding.GetString( SoftBasic.BytesReverseByWord( tmp ) );
			else
				return encoding.GetString( tmp );
		}

		/// <inheritdoc cref="IByteTransform.TransString(byte[], Encoding)"/>
		public virtual string TransString( byte[] buffer, Encoding encoding ) => encoding.GetString( buffer );

		#endregion

		#region Get Bytes From Value

		/// <inheritdoc cref="IByteTransform.TransByte(bool)"/>
		public virtual byte[] TransByte( bool value ) => TransByte( new bool[] { value } );

		/// <inheritdoc cref="IByteTransform.TransByte(bool[])"/>
		public virtual byte[] TransByte( bool[] values ) => values == null ? null : SoftBasic.BoolArrayToByte( values );

		/// <inheritdoc cref="IByteTransform.TransByte(byte)"/>
		public virtual byte[] TransByte( byte value ) => new byte[] { value };

		/// <inheritdoc cref="IByteTransform.TransByte(short)"/>
		public virtual byte[] TransByte( short value ) => TransByte( new short[] { value } );

		/// <inheritdoc cref="IByteTransform.TransByte(short[])"/>
		public virtual byte[] TransByte( short[] values )
		{
			if (values == null) return null;
			byte[] buffer = new byte[values.Length * 2];
			for (int i = 0; i < values.Length; i++)
			{
				BitConverter.GetBytes( values[i] ).CopyTo( buffer, 2 * i );
			}
			return buffer;
		}

		/// <inheritdoc cref="IByteTransform.TransByte(ushort)"/>
		public virtual byte[] TransByte( ushort value ) => TransByte( new ushort[] { value } );

		/// <inheritdoc cref="IByteTransform.TransByte(ushort[])"/>
		public virtual byte[] TransByte( ushort[] values )
		{
			if (values == null) return null;

			byte[] buffer = new byte[values.Length * 2];
			for (int i = 0; i < values.Length; i++)
			{
				BitConverter.GetBytes( values[i] ).CopyTo( buffer, 2 * i );
			}

			return buffer;
		}

		/// <inheritdoc cref="IByteTransform.TransByte(int)"/>
		public virtual byte[] TransByte( int value ) => TransByte( new int[] { value } );

		/// <inheritdoc cref="IByteTransform.TransByte(int[])"/>
		public virtual byte[] TransByte( int[] values )
		{
			if (values == null) return null;

			byte[] buffer = new byte[values.Length * 4];
			for (int i = 0; i < values.Length; i++)
			{
				ByteTransDataFormat4( BitConverter.GetBytes( values[i] ) ).CopyTo( buffer, 4 * i );
			}

			return buffer;
		}

		/// <inheritdoc cref="IByteTransform.TransByte(uint)"/>
		public virtual byte[] TransByte( uint value ) => TransByte( new uint[] { value } );

		/// <inheritdoc cref="IByteTransform.TransByte(uint[])"/>
		public virtual byte[] TransByte( uint[] values )
		{
			if (values == null) return null;

			byte[] buffer = new byte[values.Length * 4];
			for (int i = 0; i < values.Length; i++)
			{
				ByteTransDataFormat4( BitConverter.GetBytes( values[i] ) ).CopyTo( buffer, 4 * i );
			}

			return buffer;
		}

		/// <inheritdoc cref="IByteTransform.TransByte(long)"/>
		public virtual byte[] TransByte( long value ) => TransByte( new long[] { value } );

		/// <inheritdoc cref="IByteTransform.TransByte(long[])"/>
		public virtual byte[] TransByte( long[] values )
		{
			if (values == null) return null;

			byte[] buffer = new byte[values.Length * 8];
			for (int i = 0; i < values.Length; i++)
			{
				ByteTransDataFormat8( BitConverter.GetBytes( values[i] ) ).CopyTo( buffer, 8 * i );
			}

			return buffer;
		}

		/// <inheritdoc cref="IByteTransform.TransByte(ulong)"/>
		public virtual byte[] TransByte( ulong value ) => TransByte( new ulong[] { value } );

		/// <inheritdoc cref="IByteTransform.TransByte(ulong[])"/>
		public virtual byte[] TransByte( ulong[] values )
		{
			if (values == null) return null;

			byte[] buffer = new byte[values.Length * 8];
			for (int i = 0; i < values.Length; i++)
			{
				ByteTransDataFormat8( BitConverter.GetBytes( values[i] ) ).CopyTo( buffer, 8 * i );
			}

			return buffer;
		}

		/// <inheritdoc cref="IByteTransform.TransByte(float)"/>
		public virtual byte[] TransByte( float value ) => TransByte( new float[] { value } );

		/// <inheritdoc cref="IByteTransform.TransByte(float[])"/>
		public virtual byte[] TransByte( float[] values )
		{
			if (values == null) return null;

			byte[] buffer = new byte[values.Length * 4];
			for (int i = 0; i < values.Length; i++)
			{
				ByteTransDataFormat4( BitConverter.GetBytes( values[i] ) ).CopyTo( buffer, 4 * i );
			}

			return buffer;
		}

		/// <inheritdoc cref="IByteTransform.TransByte(double)"/>
		public virtual byte[] TransByte( double value ) => TransByte( new double[] { value } );

		/// <inheritdoc cref="IByteTransform.TransByte(double[])"/>
		public virtual byte[] TransByte( double[] values )
		{
			if (values == null) return null;

			byte[] buffer = new byte[values.Length * 8];
			for (int i = 0; i < values.Length; i++)
			{
				ByteTransDataFormat8( BitConverter.GetBytes( values[i] ) ).CopyTo( buffer, 8 * i );
			}

			return buffer;
		}

		/// <inheritdoc cref="IByteTransform.TransByte(string, Encoding)"/>
		public virtual byte[] TransByte( string value, Encoding encoding )
		{
			if (value == null) return null;
			byte[] buffer = encoding.GetBytes( value );
			return IsStringReverseByteWord ? SoftBasic.BytesReverseByWord( buffer ) : buffer;
		}

		/// <inheritdoc cref="IByteTransform.TransByte(string, int, Encoding)"/>
		public virtual byte[] TransByte( string value, int length, Encoding encoding )
		{
			if (value == null) return null;
			byte[] buffer = encoding.GetBytes( value );
			return IsStringReverseByteWord ? SoftBasic.ArrayExpandToLength( SoftBasic.BytesReverseByWord( buffer ), length ) : SoftBasic.ArrayExpandToLength( buffer, length );
		}

		#endregion

		#region DataFormat Support

		/// <summary>
		/// 反转多字节的数据信息
		/// </summary>
		/// <param name="value">数据字节</param>
		/// <param name="index">起始索引，默认值为0</param>
		/// <returns>实际字节信息</returns>
		protected byte[] ByteTransDataFormat4( byte[] value, int index = 0 )
		{
			byte[] buffer = new byte[4];
			switch (DataFormat)
			{
				case DataFormat.ABCD:
					{
						buffer[0] = value[index + 3];
						buffer[1] = value[index + 2];
						buffer[2] = value[index + 1];
						buffer[3] = value[index + 0];
						break;
					}
				case DataFormat.BADC:
					{
						buffer[0] = value[index + 2];
						buffer[1] = value[index + 3];
						buffer[2] = value[index + 0];
						buffer[3] = value[index + 1];
						break;
					}

				case DataFormat.CDAB:
					{
						buffer[0] = value[index + 1];
						buffer[1] = value[index + 0];
						buffer[2] = value[index + 3];
						buffer[3] = value[index + 2];
						break;
					}
				case DataFormat.DCBA:
					{
						buffer[0] = value[index + 0];
						buffer[1] = value[index + 1];
						buffer[2] = value[index + 2];
						buffer[3] = value[index + 3];
						break;
					}
			}
			return buffer;
		}


		/// <summary>
		/// 反转多字节的数据信息
		/// </summary>
		/// <param name="value">数据字节</param>
		/// <param name="index">起始索引，默认值为0</param>
		/// <returns>实际字节信息</returns>
		protected byte[] ByteTransDataFormat8( byte[] value, int index = 0 )
		{
			byte[] buffer = new byte[8];
			switch (DataFormat)
			{
				case DataFormat.ABCD:
					{
						buffer[0] = value[index + 7];
						buffer[1] = value[index + 6];
						buffer[2] = value[index + 5];
						buffer[3] = value[index + 4];
						buffer[4] = value[index + 3];
						buffer[5] = value[index + 2];
						buffer[6] = value[index + 1];
						buffer[7] = value[index + 0];
						break;
					}
				case DataFormat.BADC:
					{
						buffer[0] = value[index + 6];
						buffer[1] = value[index + 7];
						buffer[2] = value[index + 4];
						buffer[3] = value[index + 5];
						buffer[4] = value[index + 2];
						buffer[5] = value[index + 3];
						buffer[6] = value[index + 0];
						buffer[7] = value[index + 1];
						break;
					}

				case DataFormat.CDAB:
					{
						buffer[0] = value[index + 1];
						buffer[1] = value[index + 0];
						buffer[2] = value[index + 3];
						buffer[3] = value[index + 2];
						buffer[4] = value[index + 5];
						buffer[5] = value[index + 4];
						buffer[6] = value[index + 7];
						buffer[7] = value[index + 6];
						break;
					}
				case DataFormat.DCBA:
					{
						buffer[0] = value[index + 0];
						buffer[1] = value[index + 1];
						buffer[2] = value[index + 2];
						buffer[3] = value[index + 3];
						buffer[4] = value[index + 4];
						buffer[5] = value[index + 5];
						buffer[6] = value[index + 6];
						buffer[7] = value[index + 7];
						break;
					}
			}
			return buffer;
		}

		#endregion

		#region Public Properties

		/// <inheritdoc cref="IByteTransform.DataFormat"/>
		public DataFormat DataFormat { get; set; }

		/// <inheritdoc cref="IByteTransform.IsStringReverseByteWord"/>
		public bool IsStringReverseByteWord { get; set; }

		/// <inheritdoc cref="IByteTransform.CreateByDateFormat(DataFormat)"/>
		public virtual IByteTransform CreateByDateFormat( DataFormat dataFormat ) { return this; }

		#endregion

		#region Object Override

		/// <inheritdoc/>
		public override string ToString( ) => $"ByteTransformBase[{DataFormat}]";

		#endregion
	}
}
