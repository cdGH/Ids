using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HslCommunication.BasicFramework;

namespace HslCommunication.Core
{
	/// <summary>
	/// 按照字节错位的数据转换类<br />
	/// Data conversion class according to byte misalignment
	/// </summary>
	public class ReverseWordTransform : ByteTransformBase
	{
		#region Constructor

		/// <inheritdoc cref="ByteTransformBase()"/>
		public ReverseWordTransform( )
		{
			this.DataFormat = DataFormat.ABCD;
		}

		/// <inheritdoc cref="ByteTransformBase(DataFormat)"/>
		public ReverseWordTransform( DataFormat dataFormat ) : base( dataFormat )
		{

		}

		#endregion

		#region Private Method

		/// <summary>
		/// 按照字节错位的方法
		/// </summary>
		/// <param name="buffer">实际的字节数据</param>
		/// <param name="index">起始字节位置</param>
		/// <param name="length">数据长度</param>
		/// <returns>处理过的数据信息</returns>
		private byte[] ReverseBytesByWord( byte[] buffer, int index, int length )
		{
			if (buffer == null) return null;
			return SoftBasic.BytesReverseByWord( buffer.SelectMiddle( index, length ) );
		}

		#endregion

		#region Get Short From Bytes

		/// <inheritdoc cref="IByteTransform.TransInt16(byte[], int)"/>
		public override short TransInt16( byte[] buffer, int index ) => base.TransInt16( ReverseBytesByWord( buffer, index, 2 ), 0 );

		/// <inheritdoc cref="IByteTransform.TransUInt16(byte[], int)"/>
		public override ushort TransUInt16( byte[] buffer, int index ) => base.TransUInt16( ReverseBytesByWord( buffer, index, 2 ), 0 );

		#endregion

		#region Get Bytes From Short

		/// <inheritdoc cref="IByteTransform.TransByte(short[])"/>
		public override byte[] TransByte( short[] values )
		{
			byte[] buffer = base.TransByte( values );
			return SoftBasic.BytesReverseByWord( buffer );
		}

		/// <inheritdoc cref="IByteTransform.TransByte(ushort[])"/>
		public override byte[] TransByte( ushort[] values )
		{
			byte[] buffer = base.TransByte( values );
			return SoftBasic.BytesReverseByWord( buffer );
		}

		#endregion

		/// <inheritdoc cref="IByteTransform.CreateByDateFormat(DataFormat)"/>
		public override IByteTransform CreateByDateFormat( DataFormat dataFormat ) => new ReverseWordTransform( dataFormat ) { IsStringReverseByteWord = this.IsStringReverseByteWord };

		#region Object Override

		/// <inheritdoc/>
		public override string ToString( ) => $"ReverseWordTransform[{DataFormat}]";

		#endregion
	}
}
