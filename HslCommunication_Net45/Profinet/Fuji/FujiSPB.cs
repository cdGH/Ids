using HslCommunication.BasicFramework;
using HslCommunication.Core;
using HslCommunication.Serial;
using HslCommunication.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HslCommunication.Core.Address;
using System.IO;
#if !NET35 && !NET20
using System.Threading.Tasks;
#endif

namespace HslCommunication.Profinet.Fuji
{
	/// <summary>
	/// 富士PLC的SPB协议，详细的地址信息见api文档说明，地址可以携带站号信息，例如：s=2;D100，PLC侧需要配置无BCC计算，包含0D0A结束码<br />
	/// Fuji PLC's SPB protocol. For detailed address information, see the api documentation, 
	/// The address can carry station number information, for example: s=2;D100, PLC side needs to be configured with no BCC calculation, including 0D0A end code
	/// </summary>
	/// <remarks>
	/// <inheritdoc cref="FujiSPBOverTcp" path="remarks"/>
	/// </remarks>
	public class FujiSPB : SerialDeviceBase
	{
		#region Constructor

		/// <inheritdoc cref="FujiSPBOverTcp()"/>
		public FujiSPB( )
		{
			this.ByteTransform      = new RegularByteTransform( );
			this.WordLength         = 1;
			base.LogMsgFormatBinary = false;
		}

		#endregion

		#region Public Member

		/// <inheritdoc cref="FujiSPBOverTcp.Station"/>
		public byte Station { get => station; set => station = value; }

		/// <inheritdoc/>
		protected override bool CheckReceiveDataComplete( MemoryStream ms )
		{
			return ModBus.ModbusInfo.CheckAsciiReceiveDataComplete( ms.ToArray( ) );
		}

		#endregion

		#region Read Write Support

		/// <inheritdoc cref="FujiSPBOverTcp.Read(string, ushort)"/>
		[HslMqttApi( "ReadByteArray", "" )]
		public override OperateResult<byte[]> Read( string address, ushort length ) => FujiSPBHelper.Read( this, this.station, address, length );

		/// <inheritdoc cref="FujiSPBOverTcp.Write(string, byte[])"/>
		[HslMqttApi( "WriteByteArray", "" )]
		public override OperateResult Write( string address, byte[] value ) => FujiSPBHelper.Write( this, this.station, address, value );

		/// <inheritdoc cref="FujiSPBOverTcp.ReadBool(string, ushort)"/>
		[HslMqttApi( "ReadBoolArray", "" )]
		public override OperateResult<bool[]> ReadBool( string address, ushort length ) => FujiSPBHelper.ReadBool( this, this.station, address, length );

		/// <inheritdoc cref="FujiSPBOverTcp.Write(string, bool)"/>
		[HslMqttApi( "WriteBool", "" )]
		public override OperateResult Write( string address, bool value ) => FujiSPBHelper.Write( this, this.station, address, value );

#if !NET35 && !NET20
		/// <inheritdoc cref="Write(string, bool)"/>
		public async override Task<OperateResult> WriteAsync( string address, bool value ) => await FujiSPBHelper.WriteAsync( this, this.station, address, value );
#endif
		#endregion

		#region Private Member

		private byte station = 0x01;                 // PLC的站号信息

		#endregion

		#region Object Override

		/// <inheritdoc/>
		public override string ToString( ) => $"FujiSPB[{PortName}:{BaudRate}]";

		#endregion
	}
}
