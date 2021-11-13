using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HslCommunication.BasicFramework;
using HslCommunication.Profinet.AllenBradley;
using HslCommunication.Reflection;
using HslCommunication.Core;
#if !NET35 && !NET20
using System.Threading.Tasks;
#endif

namespace HslCommunication.Profinet.Omron
{
	/// <summary>
	/// 欧姆龙PLC的CIP协议的类，支持NJ,NX,NY系列PLC，支持tag名的方式读写数据，假设你读取的是局部变量，那么使用 Program:MainProgram.变量名<br />
	/// Omron PLC's CIP protocol class, support NJ, NX, NY series PLC, support tag name read and write data, assuming you read local variables, then use Program: MainProgram. Variable name
	/// </summary>
	public class OmronCipNet : AllenBradleyNet
	{
		#region Constructor

		/// <summary>
		/// Instantiate a communication object for a OmronCipNet PLC protocol
		/// </summary>
		public OmronCipNet( ) : base( ) { }

		/// <summary>
		/// Specify the IP address and port to instantiate a communication object for a OmronCipNet PLC protocol
		/// </summary>
		/// <param name="ipAddress">PLC IpAddress</param>
		/// <param name="port">PLC Port</param>
		public OmronCipNet( string ipAddress, int port = 44818 ) : base( ipAddress, port ) { }

		#endregion

		#region Read Write Override

		/// <inheritdoc/>
		[HslMqttApi( "ReadByteArray", "" )]
		public override OperateResult<byte[]> Read( string address, ushort length )
		{
			if (length > 1)
				return Read( new string[] { address }, new int[] { 1 } );
			else
				return Read( new string[] { address }, new int[] { length } );
		}

		/// <inheritdoc/>
		[HslMqttApi( "ReadInt16Array", "" )]
		public override OperateResult<short[]> ReadInt16( string address, ushort length )
		{
			if(length == 1) return ByteTransformHelper.GetResultFromBytes( Read( address, 1 ), m => ByteTransform.TransInt16( m, 0, length ) );
			
			int startIndex = HslHelper.ExtractStartIndex( ref address );
			return ByteTransformHelper.GetResultFromBytes( Read( address, 1 ), m => ByteTransform.TransInt16( m, startIndex < 0 ? 0 : startIndex * 2, length ) );
		}

		/// <inheritdoc/>
		[HslMqttApi( "ReadUInt16Array", "" )]
		public override OperateResult<ushort[]> ReadUInt16( string address, ushort length )
		{
			if (length == 1) return ByteTransformHelper.GetResultFromBytes( Read( address, 1 ), m => ByteTransform.TransUInt16( m, 0, length ) );

			int startIndex = HslHelper.ExtractStartIndex( ref address );
			return ByteTransformHelper.GetResultFromBytes( Read( address, 1 ), m => ByteTransform.TransUInt16( m, startIndex < 0 ? 0 : startIndex * 2, length ) );
		}

		/// <inheritdoc/>
		[HslMqttApi( "ReadInt32Array", "" )]
		public override OperateResult<int[]> ReadInt32( string address, ushort length )
		{
			if(length == 1) return ByteTransformHelper.GetResultFromBytes( Read( address, 1 ), m => ByteTransform.TransInt32( m, 0, length ) );

			int startIndex = HslHelper.ExtractStartIndex( ref address );
			return ByteTransformHelper.GetResultFromBytes( Read( address, 1 ), m => ByteTransform.TransInt32( m, startIndex < 0 ? 0 : startIndex * 4, length ) );
		}

		/// <inheritdoc/>
		[HslMqttApi( "ReadUInt32Array", "" )]
		public override OperateResult<uint[]> ReadUInt32( string address, ushort length )
		{
			if(length == 1) return ByteTransformHelper.GetResultFromBytes( Read( address, 1 ), m => ByteTransform.TransUInt32( m, 0, length ) );

			int startIndex = HslHelper.ExtractStartIndex( ref address );
			return ByteTransformHelper.GetResultFromBytes( Read( address, 1 ), m => ByteTransform.TransUInt32( m, startIndex < 0 ? 0 : startIndex * 4, length ) );
		}

		/// <inheritdoc/>
		[HslMqttApi( "ReadFloatArray", "" )]
		public override OperateResult<float[]> ReadFloat( string address, ushort length )
		{
			if(length == 1) return ByteTransformHelper.GetResultFromBytes( Read( address, 1 ), m => ByteTransform.TransSingle( m, 0, length ) );

			int startIndex = HslHelper.ExtractStartIndex( ref address );
			return ByteTransformHelper.GetResultFromBytes( Read( address, 1 ), m => ByteTransform.TransSingle( m, startIndex < 0 ? 0 : startIndex * 4, length ) );
		}

		/// <inheritdoc/>
		[HslMqttApi( "ReadInt64Array", "" )]
		public override OperateResult<long[]> ReadInt64( string address, ushort length )
		{
			if (length == 1) return ByteTransformHelper.GetResultFromBytes( Read( address, 1 ), m => ByteTransform.TransInt64( m, 0, length ) );

			int startIndex = HslHelper.ExtractStartIndex( ref address );
			return ByteTransformHelper.GetResultFromBytes( Read( address, 1 ), m => ByteTransform.TransInt64( m, startIndex < 0 ? 0 : startIndex * 8, length ) );
		}

		/// <inheritdoc/>
		[HslMqttApi( "ReadUInt64Array", "" )]
		public override OperateResult<ulong[]> ReadUInt64( string address, ushort length )
		{
			if( length == 1 ) return ByteTransformHelper.GetResultFromBytes( Read( address, 1 ), m => ByteTransform.TransUInt64( m, 0, length ) );

			int startIndex = HslHelper.ExtractStartIndex( ref address );
			return ByteTransformHelper.GetResultFromBytes( Read( address, 1 ), m => ByteTransform.TransUInt64( m, startIndex < 0 ? 0 : startIndex * 8, length ) );
		}

		/// <inheritdoc/>
		[HslMqttApi( "ReadDoubleArray", "" )]
		public override OperateResult<double[]> ReadDouble( string address, ushort length )
		{
			if( length == 1 ) return ByteTransformHelper.GetResultFromBytes( Read( address, 1 ), m => ByteTransform.TransDouble( m, 0, length ) );

			int startIndex = HslHelper.ExtractStartIndex( ref address );
			return ByteTransformHelper.GetResultFromBytes( Read( address, 1 ), m => ByteTransform.TransDouble( m, startIndex < 0 ? 0 : startIndex * 8, length ) );
		}

		/// <inheritdoc/>
		public override OperateResult<string> ReadString( string address, ushort length, Encoding encoding )
		{
			OperateResult<byte[]> read = Read( address, length );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<string>( read );

			int strLen = ByteTransform.TransUInt16( read.Content, 0 );
			return OperateResult.CreateSuccessResult( encoding.GetString( read.Content, 2, strLen ) );
		}

		/// <inheritdoc/>
		[HslMqttApi( "WriteInt16Array", "" )]
		public override OperateResult Write( string address, short[] values ) => WriteTag( address, AllenBradleyHelper.CIP_Type_Word, ByteTransform.TransByte( values ), values.Length );

		/// <inheritdoc/>
		[HslMqttApi( "WriteUInt16Array", "" )]
		public override OperateResult Write( string address, ushort[] values ) => WriteTag( address, AllenBradleyHelper.CIP_Type_UInt, ByteTransform.TransByte( values ), 1 );

		/// <inheritdoc/>
		[HslMqttApi( "WriteInt32Array", "" )]
		public override OperateResult Write( string address, int[] values ) => WriteTag( address, AllenBradleyHelper.CIP_Type_DWord, ByteTransform.TransByte( values ), 1 );

		/// <inheritdoc/>
		[HslMqttApi( "WriteUInt32Array", "" )]
		public override OperateResult Write( string address, uint[] values ) => WriteTag( address, AllenBradleyHelper.CIP_Type_UDint, ByteTransform.TransByte( values ), 1 );

		/// <inheritdoc/>
		[HslMqttApi( "WriteFloatArray", "" )]
		public override OperateResult Write( string address, float[] values ) => WriteTag( address, AllenBradleyHelper.CIP_Type_Real, ByteTransform.TransByte( values ), 1 );

		/// <inheritdoc/>
		[HslMqttApi( "WriteInt64Array", "" )]
		public override OperateResult Write( string address, long[] values ) => WriteTag( address, AllenBradleyHelper.CIP_Type_LInt, ByteTransform.TransByte( values ), 1 );

		/// <inheritdoc/>
		[HslMqttApi( "WriteUInt64Array", "" )]
		public override OperateResult Write( string address, ulong[] values ) => WriteTag( address, AllenBradleyHelper.CIP_Type_ULint, ByteTransform.TransByte( values ), 1 );

		/// <inheritdoc/>
		[HslMqttApi( "WriteDoubleArray", "" )]
		public override OperateResult Write( string address, double[] values ) => WriteTag( address, AllenBradleyHelper.CIP_Type_Double, ByteTransform.TransByte( values ), 1 );

		/// <inheritdoc/>
		public override OperateResult Write( string address, string value, Encoding encoding )
		{
			if (string.IsNullOrEmpty( value )) value = string.Empty;

			byte[] data = SoftBasic.SpliceArray( new byte[2], SoftBasic.ArrayExpandToLengthEven( encoding.GetBytes( value ) ) );
			data[0] = BitConverter.GetBytes( data.Length - 2 )[0];
			data[1] = BitConverter.GetBytes( data.Length - 2 )[1];
			return base.WriteTag( address, AllenBradleyHelper.CIP_Type_String, data, 1 );
		}

		/// <inheritdoc/>
		[HslMqttApi( "WriteByte", "" )]
		public override OperateResult Write( string address, byte value )
		{
			return WriteTag( address, 0xD1, new byte[] { value, 0x00 } );
		}

		#endregion

		#region Read Write Override Async
#if !NET35 && !NET20
		/// <inheritdoc cref="Read(string, ushort)"/>
		public override async Task<OperateResult<byte[]>> ReadAsync( string address, ushort length )
		{
			if (length > 1)
				return await ReadAsync( new string[] { address }, new int[] { 1 } );
			else
				return await ReadAsync( new string[] { address }, new int[] { length } );
		}
		/// <inheritdoc/>
		public override async Task<OperateResult<short[]>> ReadInt16Async( string address, ushort length )
		{
			if (length == 1) return ByteTransformHelper.GetResultFromBytes( await ReadAsync( address, 1 ), m => ByteTransform.TransInt16( m, 0, length ) );

			int startIndex = HslHelper.ExtractStartIndex( ref address );
			return ByteTransformHelper.GetResultFromBytes( await ReadAsync( address, 1 ), m => ByteTransform.TransInt16( m, startIndex < 0 ? 0 : startIndex * 2, length ) );
		}

		/// <inheritdoc/>
		public override async Task<OperateResult<ushort[]>> ReadUInt16Async( string address, ushort length )
		{
			if (length == 1) return ByteTransformHelper.GetResultFromBytes( await ReadAsync( address, 1 ), m => ByteTransform.TransUInt16( m, 0, length ) );

			int startIndex = HslHelper.ExtractStartIndex( ref address );
			return ByteTransformHelper.GetResultFromBytes( await ReadAsync( address, 1 ), m => ByteTransform.TransUInt16( m, startIndex < 0 ? 0 : startIndex * 2, length ) );
		}

		/// <inheritdoc/>
		public override async Task<OperateResult<int[]>> ReadInt32Async( string address, ushort length )
		{
			if(length == 1) return ByteTransformHelper.GetResultFromBytes( await ReadAsync( address, 1 ), m => ByteTransform.TransInt32( m, 0, length ) );

			int startIndex = HslHelper.ExtractStartIndex( ref address );
			return ByteTransformHelper.GetResultFromBytes( await ReadAsync( address, 1 ), m => ByteTransform.TransInt32( m, startIndex < 0 ? 0 : startIndex * 4, length ) );
		}

		/// <inheritdoc/>
		public override async Task<OperateResult<uint[]>> ReadUInt32Async( string address, ushort length )
		{
			if (length == 1) return ByteTransformHelper.GetResultFromBytes( await ReadAsync( address, 1 ), m => ByteTransform.TransUInt32( m, 0, length ) );

			int startIndex = HslHelper.ExtractStartIndex( ref address );
			return ByteTransformHelper.GetResultFromBytes( await ReadAsync( address, 1 ), m => ByteTransform.TransUInt32( m, startIndex < 0 ? 0 : startIndex * 4, length ) );
		}

		/// <inheritdoc/>
		public override async Task<OperateResult<float[]>> ReadFloatAsync( string address, ushort length )
		{
			if (length == 1) return ByteTransformHelper.GetResultFromBytes( await ReadAsync( address, 1 ), m => ByteTransform.TransSingle( m, 0, length ) );

			int startIndex = HslHelper.ExtractStartIndex( ref address );
			return ByteTransformHelper.GetResultFromBytes( await ReadAsync( address, 1 ), m => ByteTransform.TransSingle( m, startIndex < 0 ? 0 : startIndex * 4, length ) );
		}

		/// <inheritdoc/>
		public override async Task<OperateResult<long[]>> ReadInt64Async( string address, ushort length )
		{
			if (length == 1) return ByteTransformHelper.GetResultFromBytes( await ReadAsync( address, 1 ), m => ByteTransform.TransInt64( m, 0, length ) );

			int startIndex = HslHelper.ExtractStartIndex( ref address );
			return ByteTransformHelper.GetResultFromBytes( await ReadAsync( address, 1 ), m => ByteTransform.TransInt64( m, startIndex < 0 ? 0 : startIndex * 8, length ) );
		}

		/// <inheritdoc/>
		public override async Task<OperateResult<ulong[]>> ReadUInt64Async( string address, ushort length )
		{
			if (length == 1) return ByteTransformHelper.GetResultFromBytes( await ReadAsync( address, 1 ), m => ByteTransform.TransUInt64( m, 0, length ) );

			int startIndex = HslHelper.ExtractStartIndex( ref address );
			return ByteTransformHelper.GetResultFromBytes( await ReadAsync( address, 1 ), m => ByteTransform.TransUInt64( m, startIndex < 0 ? 0 : startIndex * 8, length ) );
		}

		/// <inheritdoc/>
		public override async Task<OperateResult<double[]>> ReadDoubleAsync( string address, ushort length )
		{
			if (length == 1) return ByteTransformHelper.GetResultFromBytes( await ReadAsync( address, 1 ), m => ByteTransform.TransDouble( m, 0, length ) );

			int startIndex = HslHelper.ExtractStartIndex( ref address );
			return ByteTransformHelper.GetResultFromBytes( await ReadAsync( address, 1 ), m => ByteTransform.TransDouble( m, startIndex < 0 ? 0 : startIndex * 8, length ) );
		}

		/// <inheritdoc/>
		public override async Task<OperateResult<string>> ReadStringAsync( string address, ushort length, Encoding encoding )
		{
			OperateResult<byte[]> read = await ReadAsync( address, length );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<string>( read );

			int strLen = ByteTransform.TransUInt16( read.Content, 0 );
			return OperateResult.CreateSuccessResult( encoding.GetString( read.Content, 2, strLen ) );
		}

		/// <inheritdoc cref="Write(string, short[])"/>
		public override async Task<OperateResult> WriteAsync( string address, short[] values ) => await WriteTagAsync( address, AllenBradleyHelper.CIP_Type_Word, ByteTransform.TransByte( values ), 1 );

		/// <inheritdoc cref="Write(string, ushort[])"/>
		public override async Task<OperateResult> WriteAsync( string address, ushort[] values ) => await WriteTagAsync( address, AllenBradleyHelper.CIP_Type_UInt, ByteTransform.TransByte( values ), 1 );

		/// <inheritdoc cref="Write(string, int[])"/>
		public override async Task<OperateResult> WriteAsync( string address, int[] values ) => await WriteTagAsync( address, AllenBradleyHelper.CIP_Type_DWord, ByteTransform.TransByte( values ), 1 );

		/// <inheritdoc cref="Write(string, uint[])"/>
		public override async Task<OperateResult> WriteAsync( string address, uint[] values ) => await WriteTagAsync( address, AllenBradleyHelper.CIP_Type_UDint, ByteTransform.TransByte( values ), 1 );

		/// <inheritdoc cref="Write(string, float[])"/>
		public override async Task<OperateResult> WriteAsync( string address, float[] values ) => await WriteTagAsync( address, AllenBradleyHelper.CIP_Type_Real, ByteTransform.TransByte( values ), 1 );

		/// <inheritdoc cref="Write(string, long[])"/>
		public override async Task<OperateResult> WriteAsync( string address, long[] values ) => await WriteTagAsync( address, AllenBradleyHelper.CIP_Type_LInt, ByteTransform.TransByte( values ), 1 );

		/// <inheritdoc cref="Write(string, ulong[])"/>
		public override async Task<OperateResult> WriteAsync( string address, ulong[] values ) => await WriteTagAsync( address, AllenBradleyHelper.CIP_Type_ULint, ByteTransform.TransByte( values ), 1 );

		/// <inheritdoc cref="Write(string, double[])"/>
		public override async Task<OperateResult> WriteAsync( string address, double[] values ) => await WriteTagAsync( address, AllenBradleyHelper.CIP_Type_Double, ByteTransform.TransByte( values ), 1 );

		/// <inheritdoc/>
		public override async Task<OperateResult> WriteAsync( string address, string value, Encoding encoding )
		{
			if (string.IsNullOrEmpty( value )) value = string.Empty;

			byte[] data = SoftBasic.SpliceArray( new byte[2], SoftBasic.ArrayExpandToLengthEven( Encoding.ASCII.GetBytes( value ) ) );
			data[0] = BitConverter.GetBytes( data.Length - 2 )[0];
			data[1] = BitConverter.GetBytes( data.Length - 2 )[1];
			return await WriteTagAsync( address, AllenBradleyHelper.CIP_Type_String, data, 1 );
		}

		/// <inheritdoc cref="Write(string, byte)"/>
		public async override Task<OperateResult> WriteAsync( string address, byte value )
		{
			return await WriteTagAsync( address, 0xD1, new byte[] { value, 0x00 } );
		}


#endif
		#endregion

		#region Object Override

		/// <inheritdoc/>
		public override string ToString( ) => $"OmronCipNet[{IpAddress}:{Port}]";

		#endregion
	}
}
