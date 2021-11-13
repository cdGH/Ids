using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

/**********************************************************************************************
 * 
 *    说明：一般的转换类
 *    日期：2018年3月14日 17:05:30
 * 
 **********************************************************************************************/

namespace HslCommunication.Core
{
	/// <summary>
	/// 字节倒序的转换类<br />
	/// Byte reverse order conversion class
	/// </summary>
	public class ReverseBytesTransform : ByteTransformBase
	{
		#region Constructor

		/// <inheritdoc cref="ByteTransformBase()"/>
		public ReverseBytesTransform( ) { }

		/// <inheritdoc cref="ByteTransformBase(DataFormat)"/>
		public ReverseBytesTransform( DataFormat dataFormat ) : base( dataFormat ) { }

		#endregion

		#region Get Value From Bytes

		/// <inheritdoc cref="IByteTransform.TransInt16(byte[], int)"/>
		public override short TransInt16( byte[] buffer, int index )
		{
			byte[] tmp = new byte[2];
			tmp[0] = buffer[1 + index];
			tmp[1] = buffer[0 + index];
			return BitConverter.ToInt16( tmp, 0 );
		}

		/// <inheritdoc cref="IByteTransform.TransUInt16(byte[], int)"/>
		public override ushort TransUInt16( byte[] buffer, int index )
		{
			byte[] tmp = new byte[2];
			tmp[0] = buffer[1 + index];
			tmp[1] = buffer[0 + index];
			return BitConverter.ToUInt16( tmp, 0 );
		}

		/// <inheritdoc cref="IByteTransform.TransInt32(byte[], int)"/>
		public override int TransInt32( byte[] buffer, int index )
		{
			byte[] tmp = new byte[4];
			tmp[0] = buffer[3 + index];
			tmp[1] = buffer[2 + index];
			tmp[2] = buffer[1 + index];
			tmp[3] = buffer[0 + index];
			return BitConverter.ToInt32( ByteTransDataFormat4( tmp ), 0 );
		}

		/// <inheritdoc cref="IByteTransform.TransUInt32(byte[], int)"/>
		public override uint TransUInt32( byte[] buffer, int index )
		{
			byte[] tmp = new byte[4];
			tmp[0] = buffer[3 + index];
			tmp[1] = buffer[2 + index];
			tmp[2] = buffer[1 + index];
			tmp[3] = buffer[0 + index];
			return BitConverter.ToUInt32( ByteTransDataFormat4( tmp ), 0 );
		}

		/// <inheritdoc cref="IByteTransform.TransInt64(byte[], int)"/>
		public override long TransInt64( byte[] buffer, int index )
		{
			byte[] tmp = new byte[8];
			tmp[0] = buffer[7 + index];
			tmp[1] = buffer[6 + index];
			tmp[2] = buffer[5 + index];
			tmp[3] = buffer[4 + index];
			tmp[4] = buffer[3 + index];
			tmp[5] = buffer[2 + index];
			tmp[6] = buffer[1 + index];
			tmp[7] = buffer[0 + index];
			return BitConverter.ToInt64( ByteTransDataFormat8( tmp ), 0 );
		}

		/// <inheritdoc cref="IByteTransform.TransUInt64(byte[], int)"/>
		public override ulong TransUInt64( byte[] buffer, int index )
		{
			byte[] tmp = new byte[8];
			tmp[0] = buffer[7 + index];
			tmp[1] = buffer[6 + index];
			tmp[2] = buffer[5 + index];
			tmp[3] = buffer[4 + index];
			tmp[4] = buffer[3 + index];
			tmp[5] = buffer[2 + index];
			tmp[6] = buffer[1 + index];
			tmp[7] = buffer[0 + index];
			return BitConverter.ToUInt64( ByteTransDataFormat8( tmp ), 0 );
		}

		/// <inheritdoc cref="IByteTransform.TransSingle(byte[], int)"/>
		public override float TransSingle( byte[] buffer, int index )
		{
			byte[] tmp = new byte[4];
			tmp[0] = buffer[3 + index];
			tmp[1] = buffer[2 + index];
			tmp[2] = buffer[1 + index];
			tmp[3] = buffer[0 + index];
			return BitConverter.ToSingle( ByteTransDataFormat4( tmp ), 0 );
		}

		/// <inheritdoc cref="IByteTransform.TransDouble(byte[], int)"/>
		public override double TransDouble( byte[] buffer, int index )
		{
			byte[] tmp = new byte[8];
			tmp[0] = buffer[7 + index];
			tmp[1] = buffer[6 + index];
			tmp[2] = buffer[5 + index];
			tmp[3] = buffer[4 + index];
			tmp[4] = buffer[3 + index];
			tmp[5] = buffer[2 + index];
			tmp[6] = buffer[1 + index];
			tmp[7] = buffer[0 + index];
			return BitConverter.ToDouble( ByteTransDataFormat8( tmp ), 0 );
		}

		#endregion

		#region Get Bytes From Value

		/// <inheritdoc cref="IByteTransform.TransByte(short[])"/>
		public override byte[] TransByte( short[] values )
		{
			if (values == null) return null;

			byte[] buffer = new byte[values.Length * 2];
			for (int i = 0; i < values.Length; i++)
			{
				byte[] tmp = BitConverter.GetBytes( values[i] );
				Array.Reverse( tmp );
				tmp.CopyTo( buffer, 2 * i );
			}

			return buffer;
		}

		/// <inheritdoc cref="IByteTransform.TransByte(ushort[])"/>
		public override byte[] TransByte( ushort[] values )
		{
			if (values == null) return null;

			byte[] buffer = new byte[values.Length * 2];
			for (int i = 0; i < values.Length; i++)
			{
				byte[] tmp = BitConverter.GetBytes( values[i] );
				Array.Reverse( tmp );
				tmp.CopyTo( buffer, 2 * i );
			}

			return buffer;
		}

		/// <inheritdoc cref="IByteTransform.TransByte(int[])"/>
		public override byte[] TransByte( int[] values )
		{
			if (values == null) return null;

			byte[] buffer = new byte[values.Length * 4];
			for (int i = 0; i < values.Length; i++)
			{
				byte[] tmp = BitConverter.GetBytes( values[i] );
				Array.Reverse( tmp );
				ByteTransDataFormat4( tmp ).CopyTo( buffer, 4 * i );
			}

			return buffer;
		}

		/// <inheritdoc cref="IByteTransform.TransByte(uint[])"/>
		public override byte[] TransByte( uint[] values )
		{
			if (values == null) return null;

			byte[] buffer = new byte[values.Length * 4];
			for (int i = 0; i < values.Length; i++)
			{
				byte[] tmp = BitConverter.GetBytes( values[i] );
				Array.Reverse( tmp );
				ByteTransDataFormat4( tmp ).CopyTo( buffer, 4 * i );
			}

			return buffer;
		}

		/// <inheritdoc cref="IByteTransform.TransByte(long[])"/>
		public override byte[] TransByte( long[] values )
		{
			if (values == null) return null;

			byte[] buffer = new byte[values.Length * 8];
			for (int i = 0; i < values.Length; i++)
			{
				byte[] tmp = BitConverter.GetBytes( values[i] );
				Array.Reverse( tmp );
				ByteTransDataFormat8( tmp ).CopyTo( buffer, 8 * i );
			}

			return buffer;
		}

		/// <inheritdoc cref="IByteTransform.TransByte(ulong[])"/>
		public override byte[] TransByte( ulong[] values )
		{
			if (values == null) return null;

			byte[] buffer = new byte[values.Length * 8];
			for (int i = 0; i < values.Length; i++)
			{
				byte[] tmp = BitConverter.GetBytes( values[i] );
				Array.Reverse( tmp );
				ByteTransDataFormat8( tmp ).CopyTo( buffer, 8 * i );
			}

			return buffer;
		}

		/// <inheritdoc cref="IByteTransform.TransByte(float[])"/>
		public override byte[] TransByte( float[] values )
		{
			if (values == null) return null;

			byte[] buffer = new byte[values.Length * 4];
			for (int i = 0; i < values.Length; i++)
			{
				byte[] tmp = BitConverter.GetBytes( values[i] );
				Array.Reverse( tmp );
				ByteTransDataFormat4( tmp ).CopyTo( buffer, 4 * i );
			}

			return buffer;
		}

		/// <inheritdoc cref="IByteTransform.TransByte(double[])"/>
		/// <returns>buffer数据</returns>
		public override byte[] TransByte( double[] values )
		{
			if (values == null) return null;

			byte[] buffer = new byte[values.Length * 8];
			for (int i = 0; i < values.Length; i++)
			{
				byte[] tmp = BitConverter.GetBytes( values[i] );
				Array.Reverse( tmp );
				ByteTransDataFormat8( tmp ).CopyTo( buffer, 8 * i );
			}

			return buffer;
		}

		#endregion

		/// <inheritdoc cref="IByteTransform.CreateByDateFormat(DataFormat)"/>
		public override IByteTransform CreateByDateFormat( DataFormat dataFormat ) => new ReverseBytesTransform( dataFormat ) { IsStringReverseByteWord = this.IsStringReverseByteWord };

		#region Object Override

		///<inheritdoc/>
		public override string ToString( ) => $"ReverseBytesTransform[{DataFormat}]";

		#endregion
	}
}
