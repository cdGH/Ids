using HslCommunication.BasicFramework;
using HslCommunication.Core;
using HslCommunication.Serial;
using HslCommunication.Reflection;
#if !NET35 && !NET20
using System.Threading.Tasks;
#endif

namespace HslCommunication.Profinet.LSIS
{
	/// <summary>
	/// XGB Cnet I/F module supports Serial Port. The address can carry station number information, for example: s=2;D100
	/// </summary>
	/// <remarks>
	/// XGB 主机的通道 0 仅支持 1:1 通信。 对于具有主从格式的 1:N 系统，在连接 XGL-C41A 模块的通道 1 或 XGB 主机中使用 RS-485 通信。 XGL-C41A 模块支持 RS-422/485 协议。
	/// </remarks>
	/// <example>
	/// <inheritdoc cref="XGBCnetOverTcp" path="example"/>
	/// </example>
	public class XGBCnet : SerialDeviceBase
	{
		#region Constructor

		/// <summary>
		/// Instantiate a Default object
		/// </summary>
		public XGBCnet()
		{
			ByteTransform = new RegularByteTransform( );
			WordLength    = 2;
		}

		#endregion

		#region Message Pack Unpack

		/// <inheritdoc/>
		protected override OperateResult<byte[]> UnpackResponseContent( byte[] send, byte[] response )
		{
			return Helper.XGBCnetHelper.UnpackResponseContent( send, response );
		}

		#endregion

		#region Public Member

		/// <inheritdoc cref="XGBCnetOverTcp.Station"/>
		public byte Station { get; set; } = 0x05;

		#endregion

		#region Read Write Byte

		/// <inheritdoc cref="XGBCnetOverTcp.ReadByte(string)"/>
		[HslMqttApi( "ReadByte", "" )]
		public OperateResult<byte> ReadByte( string address ) => ByteTransformHelper.GetResultFromArray( Read( address, 1 ) );

		/// <inheritdoc cref="XGBCnetOverTcp.Write(string, byte)"/>
		[HslMqttApi( "WriteByte", "" )]
		public OperateResult Write(string address, byte value) => Write(address, new byte[] { value });

		#endregion

		#region Read Write Bool

		/// <inheritdoc cref="Helper.XGBCnetHelper.ReadBool(IReadWriteDevice, int, string)"/>
		[HslMqttApi( "ReadBoolArray", "" )]
		public override OperateResult<bool> ReadBool( string address )
		{
			return Helper.XGBCnetHelper.ReadBool( this, this.Station, address );
		}

		/// <inheritdoc cref="XGBCnetOverTcp.ReadCoil(string)"/>
		public OperateResult<bool> ReadCoil( string address ) => ReadBool( address );

		/// <inheritdoc cref="XGBCnetOverTcp.ReadCoil(string, ushort)"/>
		public OperateResult<bool[]> ReadCoil( string address, ushort length ) => ReadBool( address, length );

		/// <inheritdoc cref="XGBCnetOverTcp.WriteCoil(string, bool)"/>
		public OperateResult WriteCoil( string address, bool value ) => Write( address, value );

		/// <inheritdoc cref="Helper.XGBCnetHelper.Write(IReadWriteDevice, int, string, bool)"/>
		[HslMqttApi( "WriteBool", "" )]
		public override OperateResult Write( string address, bool value )
		{
			return Helper.XGBCnetHelper.Write( this, this.Station, address, value );
		}

		#endregion

		#region Async Read Write Bool
#if !NET35 && !NET20
		/// <inheritdoc cref="ReadBool(string)"/>
		public async override Task<OperateResult<bool>> ReadBoolAsync( string address )
		{
			return await Helper.XGBCnetHelper.ReadBoolAsync( this, this.Station, address );
		}

		/// <inheritdoc cref="ReadCoil(string)"/>
		public async Task<OperateResult<bool>> ReadCoilAsync( string address ) => await ReadBoolAsync( address );

		/// <inheritdoc cref="ReadCoil(string, ushort)"/>
		public async Task<OperateResult<bool[]>> ReadCoilAsync( string address, ushort length ) => await ReadBoolAsync( address, length );

		/// <inheritdoc cref="WriteCoil(string, bool)"/>
		public async Task<OperateResult> WriteCoilAsync( string address, bool value ) => await WriteAsync( address, value );

		/// <inheritdoc cref="Write(string, bool)"/>
		public async override Task<OperateResult> WriteAsync( string address, bool value )
		{
			return await Helper.XGBCnetHelper.WriteAsync( this, this.Station, address, value );
		}
#endif
		#endregion

		#region Read Write Support

		/// <inheritdoc cref="Helper.XGBCnetHelper.Read(IReadWriteDevice, int, string, ushort)"/>
		[HslMqttApi( "ReadByteArray", "" )]
		public override OperateResult<byte[]> Read( string address, ushort length )
		{
			return Helper.XGBCnetHelper.Read( this, this.Station, address, length );
		}

		/// <inheritdoc cref="Helper.XGBCnetHelper.Read(IReadWriteDevice, int, string[])"/>
		public OperateResult<byte[]> Read(string[] address )
		{
			return Helper.XGBCnetHelper.Read( this, this.Station, address );
		}

		/// <inheritdoc cref="Helper.XGBCnetHelper.Write(IReadWriteDevice, int, string, byte[])"/>
		[HslMqttApi( "WriteByteArray", "" )]
		public override OperateResult Write( string address, byte[] value )
		{
			return Helper.XGBCnetHelper.Write( this, this.Station, address, value );
		}

		#endregion

		#region Async Read Write Support
#if !NET35 && !NET20
		/// <inheritdoc cref="Read(string, ushort)"/>
		public override async Task<OperateResult<byte[]>> ReadAsync( string address, ushort length )
		{
			return await Helper.XGBCnetHelper.ReadAsync( this, this.Station, address, length );
		}

		/// <inheritdoc cref="Read(string[])"/>
		public async Task<OperateResult<byte[]>> ReadAsync( string[] address )
		{
			return await Helper.XGBCnetHelper.ReadAsync( this, this.Station, address );
		}

		/// <inheritdoc cref="Write(string, byte[])"/>
		public override async Task<OperateResult> WriteAsync( string address, byte[] value )
		{
			return await Helper.XGBCnetHelper.WriteAsync( this, this.Station, address, value );
		}
#endif
		#endregion

		#region Object Override

		/// <inheritdoc/>
		public override string ToString() => $"XGBCnet[{PortName}:{BaudRate}]";

		#endregion
	}
}
