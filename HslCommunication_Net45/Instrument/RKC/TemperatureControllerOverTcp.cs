using HslCommunication.Core;
using HslCommunication.Core.Net;
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
	/// RKC的CD/CH系列数字式温度控制器的网口透传类对象，可以读取测量值，CT1输入值，CT2输入值等等，地址的地址需要参考API文档的示例<br />
	/// RKC's CD/CH series digital temperature controller's network port transparently transmits objects, which can read measured values, CT1 input values, 
	/// CT2 input values, etc. The address of the address needs to refer to the example of the API document
	/// </summary>
	/// <remarks>
	/// 只能使用ReadDouble(string),Write(string,double)方法来读写数据<br />
	/// 地址支持站号信息，例如 s=2;M1
	/// </remarks>
	/// <example>
	/// 地址示例如下：
	/// <list type="table">
	///   <listheader>
	///     <term>地址名称</term>
	///     <term>地址代号</term>
	///     <term>数据范围</term>
	///     <term>出厂方式</term>
	///     <term>读写方式</term>
	///   </listheader>
	///   <item>
	///     <term>测量值</term>
	///     <term>M1</term>
	///     <term>测量低限到测量高限</term>
	///     <term></term>
	///     <term>Read</term>
	///   </item>
	///   <item>
	///     <term>CT1输入值</term>
	///     <term>M2</term>
	///     <term>0.0到100.0A</term>
	///     <term></term>
	///     <term>Read</term>
	///   </item>
	///   <item>
	///     <term>CT2输入值</term>
	///     <term>M3</term>
	///     <term>0.0到100.0A</term>
	///     <term></term>
	///     <term>Read</term>
	///   </item>
	///   <item>
	///     <term>第一报警输入</term>
	///     <term>AA</term>
	///     <term>0 关 1 开</term>
	///     <term></term>
	///     <term>Read</term>
	///   </item>
	///   <item>
	///     <term>第二报警输入</term>
	///     <term>AB</term>
	///     <term>0 关 1 开</term>
	///     <term></term>
	///     <term>Read</term>
	///   </item>
	///   <item>
	///     <term>熄火</term>
	///     <term>B1</term>
	///     <term>0 关 1 开</term>
	///     <term></term>
	///     <term>Read</term>
	///   </item>
	///   <item>
	///     <term>错误代码</term>
	///     <term>ER</term>
	///     <term>0到255</term>
	///     <term></term>
	///     <term>Read</term>
	///   </item>
	///   <item>
	///     <term>运行/停止转换</term>
	///     <term>SR</term>
	///     <term>0 运行 1 停止</term>
	///     <term>运行</term>
	///     <term>Read/Write</term>
	///   </item>
	///   <item>
	///     <term>PID控制/自整定</term>
	///     <term>G1</term>
	///     <term>0 PID 1 AT</term>
	///     <term>PID控制</term>
	///     <term>Read/Write</term>
	///   </item>
	///   <item>
	///     <term>设定值（SV1）</term>
	///     <term>S1</term>
	///     <term>量程低限到量程高限</term>
	///     <term>0（0.0）</term>
	///     <term>Read/Write</term>
	///   </item>
	///   <item>
	///     <term>第一报警设定</term>
	///     <term>A1</term>
	///     <term>-1999到9999（小数点位置与PV相同）</term>
	///     <term>50（50.0）</term>
	///     <term>Read/Write</term>
	///   </item>
	///   <item>
	///     <term>第二报警设定</term>
	///     <term>A2</term>
	///     <term>-1999到9999（小数点位置与PV相同）</term>
	///     <term>-50（-50.0）</term>
	///     <term>Read/Write</term>
	///   </item>
	///   <item>
	///     <term>第一加热断线报警设定</term>
	///     <term>A3</term>
	///     <term>0.0到100.0A</term>
	///     <term>0.0</term>
	///     <term>Read/Write</term>
	///   </item>
	///   <item>
	///     <term>第二加热断线报警设定</term>
	///     <term>A4</term>
	///     <term>1.0到100.0A</term>
	///     <term>0.0</term>
	///     <term>Read/Write</term>
	///   </item>
	///   <item>
	///     <term>控制回路断线报警设定</term>
	///     <term>A5</term>
	///     <term>0-7200秒</term>
	///     <term>0</term>
	///     <term>Read/Write</term>
	///   </item>
	///   <item>
	///     <term>比例带 加热侧</term>
	///     <term>P1</term>
	///     <term>0-满量程</term>
	///     <term>30（30.0）</term>
	///     <term>Read/Write</term>
	///   </item>
	///   <item>
	///     <term>积分时间</term>
	///     <term>I1</term>
	///     <term>0-3600秒</term>
	///     <term>240</term>
	///     <term>Read/Write</term>
	///   </item>
	///   <item>
	///     <term>微分时间</term>
	///     <term>D1</term>
	///     <term>0-3600秒</term>
	///     <term>60</term>
	///     <term>Read/Write</term>
	///   </item>
	///   <item>
	///     <term>积分饱和带宽</term>
	///     <term>W1</term>
	///     <term>比例带的1%-100%</term>
	///     <term>100</term>
	///     <term>Read/Write</term>
	///   </item>
	///   <item>
	///     <term>制冷侧比例带</term>
	///     <term>P2</term>
	///     <term>比例带的1%-3000%</term>
	///     <term>3000</term>
	///     <term>Read/Write</term>
	///   </item>
	///   <item>
	///     <term>冷热死区</term>
	///     <term>V1</term>
	///     <term>-10.0到10.0</term>
	///     <term>0（0.0）</term>
	///     <term>Read/Write</term>
	///   </item>
	///   <item>
	///     <term>比例周期（输出1）</term>
	///     <term>T0</term>
	///     <term>0-100秒</term>
	///     <term>20</term>
	///     <term>Read/Write</term>
	///   </item>
	///   <item>
	///     <term>比例周期（输出2）</term>
	///     <term>T1</term>
	///     <term>0-100秒</term>
	///     <term>20</term>
	///     <term>Read/Write</term>
	///   </item>
	///   <item>
	///     <term>自主校正</term>
	///     <term>G2</term>
	///     <term>0 自主校正停止 1 自主校正开始</term>
	///     <term>0</term>
	///     <term>Read/Write</term>
	///   </item>
	///   <item>
	///     <term>PV基准</term>
	///     <term>PB</term>
	///     <term>量程低限到量程高, 限温度输入 -1999到+1999 [℉ ]或 -199.9到+999.9 [℉ ]</term>
	///     <term>0(0.0)</term>
	///     <term>Read/Write</term>
	///   </item>
	///   <item>
	///     <term>设定数据锁</term>
	///     <term>LK</term>
	///     <term>0到7</term>
	///     <term>0</term>
	///     <term>Read/Write</term>
	///   </item>
	/// </list>
	/// </example>
	public class TemperatureControllerOverTcp : NetworkDeviceBase
	{
		#region Constructor

		/// <summary>
		/// 实例化默认的构造方法<br />
		/// Instantiate the default constructor
		/// </summary>
		public TemperatureControllerOverTcp( )
		{
			this.WordLength    = 1;
			this.ByteTransform = new RegularByteTransform( );
			this.SleepTime     = 20;
		}

		/// <summary>
		/// 使用指定的ip地址和端口来实例化一个对象<br />
		/// Instantiate an object with the specified IP address and port
		/// </summary>
		/// <param name="ipAddress">设备的Ip地址</param>
		/// <param name="port">设备的端口号</param>
		public TemperatureControllerOverTcp( string ipAddress, int port ) : this( )
		{
			this.IpAddress = ipAddress;
			this.Port = port;
		}

		#endregion

		#region Public Member

		/// <summary>
		/// PLC的站号信息，需要和实际的设置值一致，默认为1<br />
		/// The station number information of the PLC needs to be consistent with the actual setting value. The default is 1.
		/// </summary>
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
			OperateResult<double> read = await Helper.TemperatureControllerHelper.ReadDoubleAsync( this, this.station, address );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<double[]>( read );

			return OperateResult.CreateSuccessResult( new double[] { read.Content } );
		}

		/// <inheritdoc cref="Helper.TemperatureControllerHelper.Write(IReadWriteDevice, byte, string, double)"/>
		public override async Task<OperateResult> WriteAsync( string address, double[] values )
		{
			if (values == null || values.Length == 0) return OperateResult.CreateSuccessResult( );
			return await Helper.TemperatureControllerHelper.WriteAsync( this, this.station, address, values[0] );
		}
#endif

		#region Object Override

		/// <inheritdoc/>
		public override string ToString( ) => $"RkcTemperatureControllerOverTcp[{IpAddress}:{Port}]";

		#endregion


		#region Private Member

		private byte station = 0x01;                 // PLC的站号信息

		#endregion
	}
}
