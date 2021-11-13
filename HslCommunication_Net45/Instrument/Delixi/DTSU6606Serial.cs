using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HslCommunication.ModBus;
using HslCommunication.Reflection;
#if !NET20 && !NET35
using System.Threading.Tasks;
#endif

namespace HslCommunication.Instrument.Delixi
{
	/// <summary>
	/// DTSU6606型三相四线电子式电能表的Modbus-RTU通信协议
	/// </summary>
	public class DTSU6606Serial : ModbusRtu
	{
		#region Constructor

		/// <summary>
		/// 实例化一个Modbus-Rtu协议的客户端对象<br />
		/// Instantiate a client object of the Modbus-Rtu protocol
		/// </summary>
		public DTSU6606Serial( ) :base( )
		{

		}

		/// <summary>
		/// 指定客户端自己的站号来初始化<br />
		/// Specify the client's own station number to initialize
		/// </summary>
		/// <param name="station">客户端自身的站号</param>
		public DTSU6606Serial( byte station = 0x01 ) : base( station )
		{
		}

		#endregion

		/// <summary>
		/// 读取电表的电参数类，主要包含电压，电流，频率，有功功率，无功功率，视在功率，功率因数<br />
		/// Read the electrical parameters of the meter, including voltage, current, frequency, active power, reactive power, apparent power, and power factor
		/// </summary>
		/// <returns>包含是否成功的电表结果对象</returns>
		[HslMqttApi( ApiTopic = "ReadElectricalParameters", Description = "读取电表的电参数类，主要包含电压，电流，频率，有功功率，无功功率，视在功率，功率因数" )]
		public OperateResult<ElectricalParameters> ReadElectricalParameters( )
		{
			OperateResult<byte[]> read = Read( "768", 23 );
			if (!read.IsSuccess) return read.ConvertFailed<ElectricalParameters>( );

			try
			{
				return OperateResult.CreateSuccessResult( ElectricalParameters.ParseFromDelixi( read.Content, this.ByteTransform ) );
			}
			catch(Exception ex)
			{
				return new OperateResult<ElectricalParameters>( ex.Message );
			}
		}

		/// <inheritdoc/>
		public override string ToString( ) => $"DTSU6606Serial[{PortName}]";
	}
}
