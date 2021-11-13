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
	/// 台达PLC的串口通讯类，基于Modbus-Rtu协议开发，按照台达的地址进行实现。<br />
	/// The serial communication class of Delta PLC is developed based on the Modbus-Rtu protocol and implemented according to Delta's address.
	/// </summary>
	/// <remarks>
	/// 适用于DVP-ES/EX/EC/SS型号，DVP-SA/SC/SX/EH型号，地址参考API文档，同时地址可以携带站号信息，举例：[s=2;D100],[s=3;M100]，可以动态修改当前报文的站号信息。<br />
	/// Suitable for DVP-ES/EX/EC/SS models, DVP-SA/SC/SX/EH models, the address refers to the API document, and the address can carry station number information,
	/// for example: [s=2;D100],[s= 3;M100], you can dynamically modify the station number information of the current message.
	/// </remarks>
	/// <example>
	/// 地址的格式如下：
	/// <list type="table">
	///   <listheader>
	///     <term>地址名称</term>
	///     <term>地址代号</term>
	///     <term>ES/EX/SS</term>
	///     <term>SA/SX/SC</term>
	///     <term>EH</term>
	///     <term>地址进制</term>
	///     <term>字操作</term>
	///     <term>位操作</term>
	///     <term>备注</term>
	///   </listheader>
	///   <item>
	///     <term></term>
	///     <term>S</term>
	///     <term>S0-S127</term>
	///     <term>S0-S1023</term>
	///     <term>S0-S1023</term>
	///     <term>10</term>
	///     <term>×</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>输入继电器</term>
	///     <term>X</term>
	///     <term>X0-X177</term>
	///     <term>X0-X177</term>
	///     <term>X0-X377</term>
	///     <term>8</term>
	///     <term>×</term>
	///     <term>√</term>
	///     <term>只读</term>
	///   </item>
	///   <item>
	///     <term>输出继电器</term>
	///     <term>Y</term>
	///     <term>Y0-Y177</term>
	///     <term>Y0-Y177</term>
	///     <term>Y0-Y377</term>
	///     <term>8</term>
	///     <term>×</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>定时器</term>
	///     <term>T</term>
	///     <term>T0-T127</term>
	///     <term>T0-T255</term>
	///     <term>T0-T255</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term>如果是读位，就是通断继电器，如果是读字，就是当前值</term>
	///   </item>
	///   <item>
	///     <term>计数器</term>
	///     <term>C</term>
	///     <term>C0-C127 C232-C255</term>
	///     <term>C0-C199 C200-C255</term>
	///     <term>C0-C199 C200-C255</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term>如果是读位，就是通断继电器，如果是读字，就是当前值</term>
	///   </item>
	///   <item>
	///     <term>内部继电器</term>
	///     <term>M</term>
	///     <term>M0-M1279</term>
	///     <term>M0-M4095</term>
	///     <term>M0-M4095</term>
	///     <term>10</term>
	///     <term>×</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>数据寄存器</term>
	///     <term>D</term>
	///     <term>D0-D1311</term>
	///     <term>D0-D4999</term>
	///     <term>D0-D9999</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>×</term>
	///     <term></term>
	///   </item>
	/// </list>
	/// 除此之外，地址可以携带站号信息，例如 s=2;D100，也是支持的。
	/// </example>
	public class DeltaDvpSerial : ModbusRtu
	{
		#region Constructor

		/// <summary>
		/// 实例化一个默认的对象<br />
		/// Instantiate a default object
		/// </summary>
		public DeltaDvpSerial( ) : base( ) { ByteTransform.DataFormat = DataFormat.CDAB; }

		/// <summary>
		/// 指定客户端自己的站号来初始化<br />
		/// Specify the client's own station number to initialize
		/// </summary>
		/// <param name="station">客户端自身的站号</param>
		public DeltaDvpSerial( byte station = 0x01 ) : base( station ) { ByteTransform.DataFormat = DataFormat.CDAB; }

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
		public override string ToString( ) => $"DeltaDvpSerial[{PortName}:{BaudRate}]";

		#endregion
	}
}
