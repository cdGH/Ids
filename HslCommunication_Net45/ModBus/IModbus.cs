using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HslCommunication.Core;
using HslCommunication.Core.Net;
#if !NET20 && !NET35
using System.Threading.Tasks;
#endif

namespace HslCommunication.ModBus
{
	/// <summary>
	/// Modbus设备的接口，用来表示Modbus相关的设备对象，<see cref="ModbusTcpNet"/>, <see cref="ModbusRtu"/>,
	/// <see cref="ModbusAscii"/>,<see cref="ModbusRtuOverTcp"/>,<see cref="ModbusUdpNet"/>均实现了该接口信息<br />
	/// Modbus device interface, used to represent Modbus-related device objects, <see cref="ModbusTcpNet"/>, 
	/// <see cref="ModbusRtu"/>,<see cref="ModbusAscii"/>,<see cref="ModbusRtuOverTcp "/>,<see cref="ModbusUdpNet"/> all implement the interface information
	/// </summary>
	public interface IModbus : IReadWriteDevice
	{
		/// <inheritdoc cref="ModbusTcpNet.AddressStartWithZero"/>
		bool AddressStartWithZero { get; set; }

		/// <inheritdoc cref="ModbusTcpNet.Station"/>
		byte Station { get; set; }

		/// <inheritdoc cref="ModbusTcpNet.DataFormat"/>
		DataFormat DataFormat { get; set; }

		/// <inheritdoc cref="ModbusTcpNet.IsStringReverse"/>
		bool IsStringReverse { get; set; }

		/// <summary>
		/// 将当前的地址信息转换成Modbus格式的地址，如果转换失败，返回失败的消息。默认不进行任何的转换。<br />
		/// Convert the current address information into a Modbus format address. If the conversion fails, a failure message will be returned. No conversion is performed by default.
		/// </summary>
		/// <param name="address">传入的地址</param>
		/// <param name="modbusCode">Modbus的功能码</param>
		/// <returns>转换之后Modbus的地址</returns>
		OperateResult<string> TranslateToModbusAddress( string address, byte modbusCode );
	}
}
