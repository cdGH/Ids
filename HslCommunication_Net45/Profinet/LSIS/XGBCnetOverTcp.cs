using HslCommunication.BasicFramework;
using HslCommunication.Core;
using HslCommunication.Core.Net;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using HslCommunication.Reflection;
#if !NET35 && !NET20
using System.Threading.Tasks;
#endif

namespace HslCommunication.Profinet.LSIS
{
	/// <summary>
	/// XGB Cnet I/F module supports Serial Port. On Tcp/ip implementation, The address can carry station number information, for example: s=2;D100
	/// </summary>
	/// <remarks>
	/// XGB 主机的通道 0 仅支持 1:1 通信。 对于具有主从格式的 1:N 系统，在连接 XGL-C41A 模块的通道 1 或 XGB 主机中使用 RS-485 通信。 XGL-C41A 模块支持 RS-422/485 协议。
	/// </remarks>
	/// <example>
	/// Address example likes the follow
	/// <list type="table">
	///   <listheader>
	///     <term>地址名称</term>
	///     <term>地址代号</term>
	///     <term>示例</term>
	///     <term>地址进制</term>
	///     <term>字操作</term>
	///     <term>位操作</term>
	///     <term>备注</term>
	///   </listheader>
	///   <item>
	///     <term>*</term>
	///     <term>P</term>
	///     <term>PX100,PB100,PW100,PD100,PL100</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>*</term>
	///     <term>M</term>
	///     <term>MX100,MB100,MW100,MD100,ML100</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>*</term>
	///     <term>L</term>
	///     <term>LX100,LB100,LW100,LD100,LL100</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>*</term>
	///     <term>K</term>
	///     <term>KX100,KB100,KW100,KD100,KL100</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>*</term>
	///     <term>F</term>
	///     <term>FX100,FB100,FW100,FD100,FL100</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term></term>
	///     <term>T</term>
	///     <term>TX100,TB100,TW100,TD100,TL100</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term></term>
	///     <term>C</term>
	///     <term>CX100,CB100,CW100,CD100,CL100</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term></term>
	///     <term>D</term>
	///     <term>DX100,DB100,DW100,DD100,DL100</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term></term>
	///     <term>S</term>
	///     <term>SX100,SB100,SW100,SD100,SL100</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term></term>
	///     <term>Q</term>
	///     <term>QX100,QB100,QW100,QD100,QL100</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term></term>
	///     <term>I</term>
	///     <term>IX100,IB100,IW100,ID100,IL100</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term></term>
	///     <term>N</term>
	///     <term>NX100,NB100,NW100,ND100,NL100</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term></term>
	///     <term>U</term>
	///     <term>UX100,UB100,UW100,UD100,UL100</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term></term>
	///     <term>Z</term>
	///     <term>ZX100,ZB100,ZW100,ZD100,ZL100</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term></term>
	///     <term>R</term>
	///     <term>RX100,RB100,RW100,RD100,RL100</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	/// </list>
	/// </example>
	public class XGBCnetOverTcp : NetworkDeviceBase
	{
		#region Constructor

		/// <summary>
		/// Instantiate a Default object
		/// </summary>
		public XGBCnetOverTcp( )
		{
			this.WordLength    = 2;
			this.ByteTransform = new RegularByteTransform( );
			this.SleepTime     = 20;
		}

		/// <summary>
		/// Instantiate a Default object
		/// </summary>
		/// <param name="ipAddress">Ip Address</param>
		/// <param name="port">Ip port</param>
		public XGBCnetOverTcp( string ipAddress, int port ) : this( )
		{
			this.IpAddress = ipAddress;
			this.Port = port;
		}

		#endregion

		#region Public Member

		/// <summary>
		/// PLC Station No.
		/// </summary>
		public byte Station { get; set; } = 0x05;

		#endregion

		#region Message Pack Unpack

		/// <inheritdoc/>
		protected override OperateResult<byte[]> UnpackResponseContent( byte[] send, byte[] response )
		{
			return Helper.XGBCnetHelper.UnpackResponseContent( send, response );
		}

		#endregion

		#region Read Write Byte

		/// <summary>
		/// Read single byte value from plc
		/// </summary>
		/// <param name="address">Start address</param>
		/// <returns>result</returns>
		[HslMqttApi( "ReadByte", "" )]
		public OperateResult<byte> ReadByte( string address ) => ByteTransformHelper.GetResultFromArray( Read( address, 1 ) );

		/// <summary>
		/// Write single byte value to plc
		/// </summary>
		/// <param name="address">Start address</param>
		/// <param name="value">value</param>
		/// <returns>Whether to write the successful</returns>
		[HslMqttApi( "WriteByte", "" )]
		public OperateResult Write( string address, byte value ) => Write( address, new byte[] { value } );

		#endregion

		#region Async Read Write Byte
#if !NET35 && !NET20
		/// <summary>
		/// Read single byte value from plc
		/// </summary>
		/// <param name="address">Start address</param>
		/// <returns>read result</returns>
		public async Task<OperateResult<byte>> ReadByteAsync( string address ) => ByteTransformHelper.GetResultFromArray( await ReadAsync( address, 2 ) );

		/// <summary>
		/// Write single byte value to plc
		/// </summary>
		/// <param name="address">Start address</param>
		/// <param name="value">value</param>
		/// <returns>Whether to write the successful</returns>
		public async Task<OperateResult> WriteAsync( string address, byte value ) => await WriteAsync( address, new byte[] { value } );

#endif
		#endregion

		#region Read Write Bool

		/// <inheritdoc cref="Helper.XGBCnetHelper.ReadBool(IReadWriteDevice, int, string)"/>
		[HslMqttApi( "ReadBoolArray", "" )]
		public override OperateResult<bool> ReadBool( string address )
		{
			return Helper.XGBCnetHelper.ReadBool( this, this.Station, address );
		}

		/// <summary>
		/// ReadCoil, same as ReadBool
		/// </summary>
		/// <param name="address">address, for example: MX100, PX100</param>
		/// <returns>Result</returns>
		public OperateResult<bool> ReadCoil( string address ) => ReadBool( address );

		/// <summary>
		/// ReadCoil, same as ReadBool
		/// </summary>
		/// <param name="address">address, for example: MX100, PX100</param>
		/// <param name="length">array length</param>
		/// <returns>result</returns>
		public OperateResult<bool[]> ReadCoil( string address, ushort length ) => ReadBool( address, length );

		/// <summary>
		/// WriteCoil
		/// </summary>
		/// <param name="address">Start Address</param>
		/// <param name="value">value for write</param>
		/// <returns>whether write is success</returns>
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
		public override OperateResult<byte[]> Read(string address, ushort length)
		{
			return Helper.XGBCnetHelper.Read( this, this.Station, address, length );
		}

		/// <inheritdoc cref="Helper.XGBCnetHelper.Read(IReadWriteDevice, int, string[])"/>
		public OperateResult<byte[]> Read( string[] address )
		{
			return Helper.XGBCnetHelper.Read( this, this.Station, address );
		}

		/// <inheritdoc cref="Helper.XGBCnetHelper.Write(IReadWriteDevice, int, string, byte[])"/>
		[HslMqttApi( "WriteByteArray", "" )]
		public override OperateResult Write(string address, byte[] value)
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
		public override string ToString() => $"XGBCnetOverTcp[{IpAddress}:{Port}]";

		#endregion

	}
}
