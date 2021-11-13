using HslCommunication.Core;
using HslCommunication.Serial;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
#if !NET20 && !NET35
using System.Threading.Tasks;
#endif

namespace HslCommunication.Instrument.RKC
{
	/// <summary>
	/// RKC的CD/CH系列数字式温度控制器的串口类对象，可以读取测量值，CT1输入值，CT2输入值等等，地址的地址需要参考API文档的示例<br />
	/// The serial port object of RKC's CD/CH series digital temperature controller can read the measured value, CT1 input value, 
	/// CT2 input value, etc. The address of the address needs to refer to the example of the API document
	/// </summary>
	/// <remarks>
	/// 只能使用ReadDouble(string),Write(string,double)方法来读写数据，设备的串口默认参数为 8-1-N,8 个数据位，一个停止位，无奇偶校验<br />
	/// 地址支持站号信息，例如 s=2;M1
	/// </remarks>
	/// <example>
	/// <inheritdoc cref="TemperatureControllerOverTcp" path="example"/>
	/// </example>
	public class TemperatureController : SerialDeviceBase
	{
		#region Constructor

		/// <inheritdoc cref="TemperatureControllerOverTcp.TemperatureControllerOverTcp()"/>
		public TemperatureController( )
		{
			ByteTransform = new RegularByteTransform( );
			WordLength    = 1;
		}

		#endregion

		#region Public Member

		/// <inheritdoc cref="TemperatureControllerOverTcp.Station"/>
		public byte Station { get => station; set => station = value; }

		#endregion

		#region Read Write Override

		/// <inheritdoc cref="Helper.TemperatureControllerHelper.ReadDouble(IReadWriteDevice, byte, string)"/>
		public override OperateResult<double[]> ReadDouble( string address, ushort length )
		{
			OperateResult<double> read = Helper.TemperatureControllerHelper.ReadDouble( this, this.station, address );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<double[]>( read );

			return OperateResult.CreateSuccessResult( new double[] { read.Content } );
		}

		/// <inheritdoc cref="Helper.TemperatureControllerHelper.Write(IReadWriteDevice, byte, string, double)"/>
		public override OperateResult Write( string address, double[] values )
		{
			if (values == null || values.Length == 0) return OperateResult.CreateSuccessResult( );
			return Helper.TemperatureControllerHelper.Write( this, this.station, address, values[0] );
		}

		#endregion

#if !NET20 && !NET35

		/// <inheritdoc cref="Helper.TemperatureControllerHelper.ReadDouble(IReadWriteDevice, byte, string)"/>
		public override async Task<OperateResult<double[]>> ReadDoubleAsync( string address, ushort length )
		{
			return await Task.Run( ( ) => ReadDouble( address, length ) );
		}

		/// <inheritdoc cref="Helper.TemperatureControllerHelper.Write(IReadWriteDevice, byte, string, double)"/>
		public override async Task<OperateResult> WriteAsync( string address, double[] values )
		{
			return await Task.Run( ( ) => Write( address, values ) );
		}
#endif

		#region Object Override

		/// <inheritdoc/>
		public override string ToString( ) => $"RkcTemperatureController[{PortName}:{BaudRate}]";

		#endregion


		#region Private Member

		private byte station = 0x01;                 // PLC的站号信息

		#endregion
	}
}
