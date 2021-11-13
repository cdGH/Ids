using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HslCommunication.ModBus;
using HslCommunication.Reflection;
using HslCommunication.Core;
#if !NET20 && !NET35
using System.Threading.Tasks;
#endif

namespace HslCommunication.Profinet.Delta
{
	/// <summary>
	/// 台达PLC的串口通讯类，基于Modbus-Ascii协议开发，按照台达的地址进行实现。<br />
	/// The serial communication class of Delta PLC is developed based on the Modbus-Ascii protocol and implemented according to Delta's address.
	/// </summary>
	/// <remarks>
	/// 适用于DVP-ES/EX/EC/SS型号，DVP-SA/SC/SX/EH型号，地址参考API文档，同时地址可以携带站号信息，举例：[s=2;D100],[s=3;M100]，可以动态修改当前报文的站号信息。<br />
	/// Suitable for DVP-ES/EX/EC/SS models, DVP-SA/SC/SX/EH models, the address refers to the API document, and the address can carry station number information,
	/// for example: [s=2;D100],[s= 3;M100], you can dynamically modify the station number information of the current message.
	/// </remarks>
	/// <example>
	/// <inheritdoc cref="DeltaDvpSerial" path="example"/>
	/// </example>
	public class DeltaDvpSerialAscii : ModbusAscii
	{
		#region Constructor

		/// <inheritdoc cref="DeltaDvpSerial()"/>
		public DeltaDvpSerialAscii( ) : base( ) { ByteTransform.DataFormat = DataFormat.CDAB; }

		/// <inheritdoc cref="DeltaDvpSerial(byte)"/>
		public DeltaDvpSerialAscii( byte station = 0x01 ) : base( station ) { ByteTransform.DataFormat = DataFormat.CDAB; }

		#endregion

		#region Override

		/// <inheritdoc/>
		public override OperateResult<string> TranslateToModbusAddress( string address, byte modbusCode )
		{
			return DeltaHelper.PraseDeltaDvpAddress( address, modbusCode );
		}

		#endregion

		#region ReadWrite

		/// <inheritdoc/>
		public override OperateResult<bool[]> ReadBool( string address, ushort length )
		{
			return DeltaHelper.ReadBool( base.ReadBool, address, length );
		}

		/// <inheritdoc/>
		public override OperateResult Write( string address, bool[] values )
		{
			return DeltaHelper.Write( base.Write, address, values );
		}

		/// <inheritdoc/>
		public override OperateResult<byte[]> Read( string address, ushort length )
		{
			return DeltaHelper.Read( base.Read, address, length );
		}

		/// <inheritdoc/>
		public override OperateResult Write( string address, byte[] value )
		{
			return DeltaHelper.Write( base.Write, address, value );
		}
		#endregion

		#region Object Override

		/// <inheritdoc/>
		public override string ToString( ) => $"DeltaDvpSerialAscii[{PortName}:{BaudRate}]";

		#endregion
	}
}
