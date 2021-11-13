using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HslCommunication.BasicFramework;
using HslCommunication.Core;
using HslCommunication.Core.Address;
using HslCommunication.Core.Net;
using HslCommunication.Serial;
using HslCommunication.Reflection;
#if !NET35 && !NET20
using System.Threading.Tasks;
#endif

namespace HslCommunication.ModBus
{
	/// <inheritdoc cref="ModbusRtu"/>
	public class ModbusRtuOverTcp : NetworkDeviceBase, IModbus
	{
		#region Constructor

		/// <inheritdoc cref="ModbusRtu()"/>
		public ModbusRtuOverTcp( )
		{
			this.ByteTransform  = new ReverseWordTransform( );
			this.WordLength     = 1;
			this.SleepTime      = 20;
		}

		/// <inheritdoc cref="ModbusTcpNet(string,int,byte)"/>
		public ModbusRtuOverTcp( string ipAddress, int port = 502, byte station = 0x01 ) : this( )
		{
			this.IpAddress      = ipAddress;
			this.Port           = port;
			this.station        = station;
		}

		#endregion

		#region Private Member

		private byte station = 0x01;                                 // 本客户端的站号
		private bool isAddressStartWithZero = true;                  // 线圈值的地址值是否从零开始

		#endregion

		#region Public Member

		/// <inheritdoc cref="ModbusTcpNet.AddressStartWithZero"/>
		public bool AddressStartWithZero
		{
			get { return isAddressStartWithZero; }
			set { isAddressStartWithZero = value; }
		}

		/// <inheritdoc cref="ModbusTcpNet.Station"/>
		public byte Station
		{
			get { return station; }
			set { station = value; }
		}

		/// <inheritdoc cref="ModbusTcpNet.DataFormat"/>
		public DataFormat DataFormat
		{
			get { return ByteTransform.DataFormat; }
			set { ByteTransform.DataFormat = value; }
		}

		/// <inheritdoc cref="ModbusTcpNet.IsStringReverse"/>
		public bool IsStringReverse
		{
			get { return ByteTransform.IsStringReverseByteWord; }
			set { ByteTransform.IsStringReverseByteWord = value; }
		}

		/// <inheritdoc cref="IModbus.TranslateToModbusAddress(string,byte)"/>
		public virtual OperateResult<string> TranslateToModbusAddress( string address, byte modbusCode )
		{
			return OperateResult.CreateSuccessResult( address );
		}

		#endregion

		#region Core Interative

		/// <inheritdoc/>
		protected override byte[] PackCommandWithHeader( byte[] command ) => ModbusInfo.PackCommandToRtu( command );

		/// <inheritdoc/>
		protected override OperateResult<byte[]> UnpackResponseContent( byte[] send, byte[] response ) => ModbusHelper.ExtraRtuResponseContent( send, response );

		#endregion

		#region Read Write Support

		/// <inheritdoc cref="ModbusTcpNet.ReadCoil(string)"/>
		public OperateResult<bool> ReadCoil( string address ) => ReadBool( address );

		/// <inheritdoc cref="ModbusTcpNet.ReadCoil(string, ushort)"/>
		public OperateResult<bool[]> ReadCoil( string address, ushort length ) => ReadBool( address, length );

		/// <inheritdoc cref="ModbusTcpNet.ReadDiscrete(string)"/>
		public OperateResult<bool> ReadDiscrete( string address ) => ByteTransformHelper.GetResultFromArray( ReadDiscrete( address, 1 ) );

		/// <inheritdoc cref="ModbusTcpNet.ReadDiscrete(string, ushort)"/>
		public OperateResult<bool[]> ReadDiscrete( string address, ushort length ) => ModbusHelper.ReadBoolHelper( this, address, length, ModbusInfo.ReadDiscrete );

		/// <inheritdoc cref="ModbusTcpNet.Read(string, ushort)"/>
		[HslMqttApi( "ReadByteArray", "" )]
		public override OperateResult<byte[]> Read( string address, ushort length ) => ModbusHelper.Read( this, address, length );


		/// <inheritdoc cref="ModbusTcpNet.Write(string, byte[])"/>
		[HslMqttApi( "WriteByteArray", "" )]
		public override OperateResult Write( string address, byte[] value ) => ModbusHelper.Write( this, address, value );

		/// <inheritdoc cref="ModbusTcpNet.Write(string, short)"/>
		[HslMqttApi( "WriteInt16", "" )]
		public override OperateResult Write( string address, short value ) => ModbusHelper.Write( this, address, value );

		/// <inheritdoc cref="ModbusTcpNet.Write(string, ushort)"/>
		[HslMqttApi( "WriteUInt16", "" )]
		public override OperateResult Write( string address, ushort value ) => ModbusHelper.Write( this, address, value );

		/// <inheritdoc cref="ModbusTcpNet.WriteMask(string, ushort, ushort)"/>
		[HslMqttApi( "WriteMask", "" )]
		public OperateResult WriteMask( string address, ushort andMask, ushort orMask ) => ModbusHelper.WriteMask( this, address, andMask, orMask );

		#endregion

		#region Write One Registe

		/// <inheritdoc cref="Write(string, short)"/>
		public OperateResult WriteOneRegister( string address, short value ) => Write( address, value );

		/// <inheritdoc cref="Write(string, ushort)"/>
		public OperateResult WriteOneRegister( string address, ushort value ) => Write( address, value );

		#endregion

		#region Async Read Write Support
#if !NET35 && !NET20
		/// <inheritdoc cref="ModbusTcpNet.ReadCoilAsync(string)"/>
		public async Task<OperateResult<bool>> ReadCoilAsync( string address ) => await ReadBoolAsync( address );

		/// <inheritdoc cref="ModbusTcpNet.ReadCoilAsync(string, ushort)"/>
		public async Task<OperateResult<bool[]>> ReadCoilAsync( string address, ushort length ) => await ReadBoolAsync( address, length );

		/// <inheritdoc cref="ModbusTcpNet.ReadDiscreteAsync(string)"/>
		public async Task<OperateResult<bool>> ReadDiscreteAsync( string address ) => ByteTransformHelper.GetResultFromArray( await ReadDiscreteAsync( address, 1 ) );

		/// <inheritdoc cref="ModbusTcpNet.ReadDiscreteAsync(string, ushort)"/>
		public async Task<OperateResult<bool[]>> ReadDiscreteAsync( string address, ushort length ) => await ReadBoolHelperAsync( address, length, ModbusInfo.ReadDiscrete );

		/// <inheritdoc cref="ModbusTcpNet.ReadAsync(string, ushort)"/>
		public async override Task<OperateResult<byte[]>> ReadAsync( string address, ushort length ) => await ModbusHelper.ReadAsync( this, address, length );

		/// <inheritdoc cref="ModbusTcpNet.WriteAsync(string, byte[])"/>
		public async override Task<OperateResult> WriteAsync( string address, byte[] value ) => await ModbusHelper.WriteAsync( this, address, value );

		/// <inheritdoc cref="Write(string, short)"/>
		public async override Task<OperateResult> WriteAsync( string address, short value ) => await ModbusHelper.WriteAsync( this, address, value );

		/// <inheritdoc cref="Write(string, ushort)"/>
		public async override Task<OperateResult> WriteAsync( string address, ushort value ) => await ModbusHelper.WriteAsync( this, address, value );

		/// <inheritdoc cref="ModbusTcpNet.WriteOneRegisterAsync(string, short)"/>
		public async Task<OperateResult> WriteOneRegisterAsync( string address, short value ) => await WriteAsync( address, value );

		/// <inheritdoc cref="ModbusTcpNet.WriteOneRegisterAsync(string, ushort)"/>
		public async Task<OperateResult> WriteOneRegisterAsync( string address, ushort value ) => await WriteAsync( address, value );

		/// <inheritdoc cref="WriteMask(string, ushort, ushort)"/>
		public async Task<OperateResult> WriteMaskAsync( string address, ushort andMask, ushort orMask ) => await Task.Run( ( ) => WriteMask( address, andMask, orMask ) );
#endif
		#endregion

		#region Bool Support

		/// <inheritdoc cref="ModbusTcpNet.ReadBool(string, ushort)"/>
		[HslMqttApi( "ReadBoolArray", "" )]
		public override OperateResult<bool[]> ReadBool( string address, ushort length ) => ModbusHelper.ReadBoolHelper( this, address, length, ModbusInfo.ReadCoil );

		/// <inheritdoc cref="ModbusTcpNet.Write(string, bool[])"/>
		[HslMqttApi( "WriteBoolArray", "" )]
		public override OperateResult Write( string address, bool[] values ) => ModbusHelper.Write( this, address, values );

		/// <inheritdoc cref="ModbusTcpNet.Write(string, bool)"/>
		[HslMqttApi( "WriteBool", "" )]
		public override OperateResult Write( string address, bool value ) => ModbusHelper.Write( this, address, value );

		#endregion

		#region Async Bool Support
#if !NET35 && !NET20

		private async Task<OperateResult<bool[]>> ReadBoolHelperAsync( string address, ushort length, byte function ) => await ModbusHelper.ReadBoolHelperAsync( this, address, length, function );

		/// <inheritdoc cref="ModbusTcpNet.ReadBoolAsync(string, ushort)"/>
		public override async Task<OperateResult<bool[]>> ReadBoolAsync( string address, ushort length ) => await ReadBoolHelperAsync( address, length, ModbusInfo.ReadCoil );

		/// <inheritdoc cref="ModbusTcpNet.WriteAsync(string, bool[])"/>
		public override async Task<OperateResult> WriteAsync( string address, bool[] values ) => await ModbusHelper.WriteAsync( this, address, values );

		/// <inheritdoc cref="ModbusTcpNet.WriteAsync(string, bool)"/>
		public override async Task<OperateResult> WriteAsync( string address, bool value ) => await ModbusHelper.WriteAsync( this, address, value );
#endif
		#endregion

		#region DataFormat Support

		/// <inheritdoc cref="IReadWriteNet.ReadInt32(string, ushort)"/>
		[HslMqttApi( "ReadInt32Array", "" )]
		public override OperateResult<int[]> ReadInt32( string address, ushort length )
		{
			IByteTransform transform = HslHelper.ExtractTransformParameter( ref address, this.ByteTransform );
			return ByteTransformHelper.GetResultFromBytes( Read( address, (ushort)(length * WordLength * 2) ), m => transform.TransInt32( m, 0, length ) );
		}

		/// <inheritdoc cref="IReadWriteNet.ReadUInt32(string, ushort)"/>
		[HslMqttApi( "ReadUInt32Array", "" )]
		public override OperateResult<uint[]> ReadUInt32( string address, ushort length )
		{
			IByteTransform transform = HslHelper.ExtractTransformParameter( ref address, this.ByteTransform );
			return ByteTransformHelper.GetResultFromBytes( Read( address, (ushort)(length * WordLength * 2) ), m => transform.TransUInt32( m, 0, length ) );
		}

		/// <inheritdoc cref="IReadWriteNet.ReadFloat(string, ushort)"/>
		[HslMqttApi( "ReadFloatArray", "" )]
		public override OperateResult<float[]> ReadFloat( string address, ushort length )
		{
			IByteTransform transform = HslHelper.ExtractTransformParameter( ref address, this.ByteTransform );
			return ByteTransformHelper.GetResultFromBytes( Read( address, (ushort)(length * WordLength * 2) ), m => transform.TransSingle( m, 0, length ) );
		}

		/// <inheritdoc cref="IReadWriteNet.ReadInt64(string, ushort)"/>
		[HslMqttApi( "ReadInt64Array", "" )]
		public override OperateResult<long[]> ReadInt64( string address, ushort length )
		{
			IByteTransform transform = HslHelper.ExtractTransformParameter( ref address, this.ByteTransform );
			return ByteTransformHelper.GetResultFromBytes( Read( address, (ushort)(length * WordLength * 4) ), m => transform.TransInt64( m, 0, length ) );
		}

		/// <inheritdoc cref="IReadWriteNet.ReadUInt64(string, ushort)"/>
		[HslMqttApi( "ReadUInt64Array", "" )]
		public override OperateResult<ulong[]> ReadUInt64( string address, ushort length )
		{
			IByteTransform transform = HslHelper.ExtractTransformParameter( ref address, this.ByteTransform );
			return ByteTransformHelper.GetResultFromBytes( Read( address, (ushort)(length * WordLength * 4) ), m => transform.TransUInt64( m, 0, length ) );
		}

		/// <inheritdoc cref="IReadWriteNet.ReadDouble(string, ushort)"/>
		[HslMqttApi( "ReadDoubleArray", "" )]
		public override OperateResult<double[]> ReadDouble( string address, ushort length )
		{
			IByteTransform transform = HslHelper.ExtractTransformParameter( ref address, this.ByteTransform );
			return ByteTransformHelper.GetResultFromBytes( Read( address, (ushort)(length * WordLength * 4) ), m => transform.TransDouble( m, 0, length ) );
		}

		/// <inheritdoc cref="IReadWriteNet.Write(string, int[])"/>
		[HslMqttApi( "WriteInt32Array", "" )]
		public override OperateResult Write( string address, int[] values )
		{
			IByteTransform transform = HslHelper.ExtractTransformParameter( ref address, this.ByteTransform );
			return Write( address, transform.TransByte( values ) );
		}

		/// <inheritdoc cref="IReadWriteNet.Write(string, uint[])"/>
		[HslMqttApi( "WriteUInt32Array", "" )]
		public override OperateResult Write( string address, uint[] values )
		{
			IByteTransform transform = HslHelper.ExtractTransformParameter( ref address, this.ByteTransform );
			return Write( address, transform.TransByte( values ) );
		}

		/// <inheritdoc cref="IReadWriteNet.Write(string, float[])"/>
		[HslMqttApi( "WriteFloatArray", "" )]
		public override OperateResult Write( string address, float[] values )
		{
			IByteTransform transform = HslHelper.ExtractTransformParameter( ref address, this.ByteTransform );
			return Write( address, transform.TransByte( values ) );
		}

		/// <inheritdoc cref="IReadWriteNet.Write(string, long[])"/>
		[HslMqttApi( "WriteInt64Array", "" )]
		public override OperateResult Write( string address, long[] values )
		{
			IByteTransform transform = HslHelper.ExtractTransformParameter( ref address, this.ByteTransform );
			return Write( address, transform.TransByte( values ) );
		}

		/// <inheritdoc cref="IReadWriteNet.Write(string, ulong[])"/>
		[HslMqttApi( "WriteUInt64Array", "" )]
		public override OperateResult Write( string address, ulong[] values )
		{
			IByteTransform transform = HslHelper.ExtractTransformParameter( ref address, this.ByteTransform );
			return Write( address, transform.TransByte( values ) );
		}

		/// <inheritdoc cref="IReadWriteNet.Write(string, double[])"/>
		[HslMqttApi( "WriteDoubleArray", "" )]
		public override OperateResult Write( string address, double[] values )
		{
			IByteTransform transform = HslHelper.ExtractTransformParameter( ref address, this.ByteTransform );
			return Write( address, transform.TransByte( values ) );
		}

#if !NET35 && !NET20
		/// <inheritdoc cref="IReadWriteNet.ReadInt32Async(string, ushort)"/>
		public override async Task<OperateResult<int[]>> ReadInt32Async( string address, ushort length )
		{
			IByteTransform transform = HslHelper.ExtractTransformParameter( ref address, this.ByteTransform );
			return ByteTransformHelper.GetResultFromBytes( await ReadAsync( address, (ushort)(length * WordLength * 2) ), m => transform.TransInt32( m, 0, length ) );
		}

		/// <inheritdoc cref="IReadWriteNet.ReadUInt32Async(string, ushort)"/>
		public override async Task<OperateResult<uint[]>> ReadUInt32Async( string address, ushort length )
		{
			IByteTransform transform = HslHelper.ExtractTransformParameter( ref address, this.ByteTransform );
			return ByteTransformHelper.GetResultFromBytes( await ReadAsync( address, (ushort)(length * WordLength * 2) ), m => transform.TransUInt32( m, 0, length ) );
		}

		/// <inheritdoc cref="IReadWriteNet.ReadFloatAsync(string, ushort)"/>
		public override async Task<OperateResult<float[]>> ReadFloatAsync( string address, ushort length )
		{
			IByteTransform transform = HslHelper.ExtractTransformParameter( ref address, this.ByteTransform );
			return ByteTransformHelper.GetResultFromBytes( await ReadAsync( address, (ushort)(length * WordLength * 2) ), m => transform.TransSingle( m, 0, length ) );
		}

		/// <inheritdoc cref="IReadWriteNet.ReadInt64Async(string, ushort)"/>
		public override async Task<OperateResult<long[]>> ReadInt64Async( string address, ushort length )
		{
			IByteTransform transform = HslHelper.ExtractTransformParameter( ref address, this.ByteTransform );
			return ByteTransformHelper.GetResultFromBytes( await ReadAsync( address, (ushort)(length * WordLength * 4) ), m => transform.TransInt64( m, 0, length ) );
		}

		/// <inheritdoc cref="IReadWriteNet.ReadUInt64Async(string, ushort)"/>
		public override async Task<OperateResult<ulong[]>> ReadUInt64Async( string address, ushort length )
		{
			IByteTransform transform = HslHelper.ExtractTransformParameter( ref address, this.ByteTransform );
			return ByteTransformHelper.GetResultFromBytes( await ReadAsync( address, (ushort)(length * WordLength * 4) ), m => transform.TransUInt64( m, 0, length ) );
		}

		/// <inheritdoc cref="IReadWriteNet.ReadDoubleAsync(string, ushort)"/>
		public override async Task<OperateResult<double[]>> ReadDoubleAsync( string address, ushort length )
		{
			IByteTransform transform = HslHelper.ExtractTransformParameter( ref address, this.ByteTransform );
			return ByteTransformHelper.GetResultFromBytes( await ReadAsync( address, (ushort)(length * WordLength * 4) ), m => transform.TransDouble( m, 0, length ) );
		}

		/// <inheritdoc cref="IReadWriteNet.WriteAsync(string, int[])"/>
		public override async Task<OperateResult> WriteAsync( string address, int[] values )
		{
			IByteTransform transform = HslHelper.ExtractTransformParameter( ref address, this.ByteTransform );
			return await WriteAsync( address, transform.TransByte( values ) );
		}

		/// <inheritdoc cref="IReadWriteNet.WriteAsync(string, uint[])"/>
		public override async Task<OperateResult> WriteAsync( string address, uint[] values )
		{
			IByteTransform transform = HslHelper.ExtractTransformParameter( ref address, this.ByteTransform );
			return await WriteAsync( address, transform.TransByte( values ) );
		}

		/// <inheritdoc cref="IReadWriteNet.WriteAsync(string, float[])"/>
		public override async Task<OperateResult> WriteAsync( string address, float[] values )
		{
			IByteTransform transform = HslHelper.ExtractTransformParameter( ref address, this.ByteTransform );
			return await WriteAsync( address, transform.TransByte( values ) );
		}

		/// <inheritdoc cref="IReadWriteNet.WriteAsync(string, long[])"/>
		public override async Task<OperateResult> WriteAsync( string address, long[] values )
		{
			IByteTransform transform = HslHelper.ExtractTransformParameter( ref address, this.ByteTransform );
			return await WriteAsync( address, transform.TransByte( values ) );
		}

		/// <inheritdoc cref="IReadWriteNet.WriteAsync(string, ulong[])"/>
		public override async Task<OperateResult> WriteAsync( string address, ulong[] values )
		{
			IByteTransform transform = HslHelper.ExtractTransformParameter( ref address, this.ByteTransform );
			return await WriteAsync( address, transform.TransByte( values ) );
		}

		/// <inheritdoc cref="IReadWriteNet.WriteAsync(string, double[])"/>
		public override async Task<OperateResult> WriteAsync( string address, double[] values )
		{
			IByteTransform transform = HslHelper.ExtractTransformParameter( ref address, this.ByteTransform );
			return await WriteAsync( address, transform.TransByte( values ) );
		}
#endif
		#endregion

		#region Object Override

		/// <inheritdoc/>
		public override string ToString( ) => $"ModbusRtuOverTcp[{IpAddress}:{Port}]";

		#endregion
	}
}
