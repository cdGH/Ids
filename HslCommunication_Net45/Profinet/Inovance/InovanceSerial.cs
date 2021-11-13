using HslCommunication.ModBus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HslCommunication.Profinet.Inovance
{
	/// <summary>
	/// 汇川的串口通信协议，A适用于AM400、 AM400_800、 AC800、H3U, XP, H5U 等系列底层走的是MODBUS-RTU协议，地址说明参见标记<br />
	/// Huichuan's serial communication protocol is applicable to AM400, AM400_800, AC800 and other series. The bottom layer is MODBUS-RTU protocol. For the address description, please refer to the mark
	/// </summary>
	/// <remarks>
	/// AM400_800 的元件有 Q 区，I 区，M 区这三种，分别都可以按位，按字节，按字和按双字进行访问，在本组件的条件下，仅支持按照位，字访问。<br />
	/// 位地址支持 Q, I, M 地址类型，字地址支持 SM, SD，支持对字地址的位访问，例如 ReadBool("SD0.5");
	/// H3U 系列控制器支持 M/SM/S/T/C/X/Y 等 bit 型变量（也称线圈） 的访问、 D/SD/R/T/C 等 word 型变量的访问；<br />
	/// H5U 系列控制器支持 M/B/S/X/Y 等 bit 型变量（也称线圈） 的访问、 D/R 等 word 型变量的访问；内部 W 元件，不支持通信访问。<br />
	/// </remarks>
	/// <example>
	/// 对于AM400_800系列的地址表如下：
	/// <list type="table">
	///   <listheader>
	///     <term>地址名称</term>
	///     <term>地址代号</term>
	///     <term>地址范围</term>
	///     <term>地址进制</term>
	///     <term>备注</term>
	///   </listheader>
	///   <item>
	///     <term>输出</term>
	///     <term>Q</term>
	///     <term>Q0.0-Q8191.7 或是 Q0-Q65535</term>
	///     <term>8 或是 10</term>
	///     <term>位读写</term>
	///   </item>
	///   <item>
	///     <term>输入</term>
	///     <term>I</term>
	///     <term>IX0.0-IX8191.7 或是 I0-I65535</term>
	///     <term>8 或是 10</term>
	///     <term>位读写</term>
	///   </item>
	///   <item>
	///     <term>M寄存器</term>
	///     <term>M</term>
	///     <term>MW0-MW65535</term>
	///     <term>10</term>
	///     <term>按照字访问的</term>
	///   </item>
	/// </list>
	/// 针对AM600的TCP还支持下面的两种地址读写
	/// <list type="table">
	///   <listheader>
	///     <term>地址名称</term>
	///     <term>地址代号</term>
	///     <term>地址范围</term>
	///     <term>地址进制</term>
	///     <term>备注</term>
	///   </listheader>
	///   <item>
	///     <term></term>
	///     <term>SM</term>
	///     <term>SM0.0-SM8191.7 或是 SM0-SM65535</term>
	///     <term>10</term>
	///     <term>位读写</term>
	///   </item>
	///   <item>
	///     <term></term>
	///     <term>SD</term>
	///     <term>SDW0-SDW65535</term>
	///     <term>10</term>
	///     <term>字读写</term>
	///   </item>
	/// </list>
	/// 我们再来看看H3U系列的线圈、 位元件、位变量地址定义
	/// <list type="table">
	///   <listheader>
	///     <term>地址名称</term>
	///     <term>地址代号</term>
	///     <term>地址范围</term>
	///     <term>地址进制</term>
	///     <term>备注</term>
	///   </listheader>
	///   <item>
	///     <term>中间寄电器</term>
	///     <term>M</term>
	///     <term>M0-M7679，M8000-M8511</term>
	///     <term>10</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term></term>
	///     <term>SM</term>
	///     <term>SM0-SM1023</term>
	///     <term>10</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term></term>
	///     <term>S</term>
	///     <term>S0-S4095</term>
	///     <term>10</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>定时器</term>
	///     <term>T</term>
	///     <term>T0-T511</term>
	///     <term>10</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>计数器</term>
	///     <term>C</term>
	///     <term>C0-C255</term>
	///     <term>10</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>输入</term>
	///     <term>X</term>
	///     <term>X0-X377 或者X0.0-X37.7</term>
	///     <term>8</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>输出</term>
	///     <term>Y</term>
	///     <term>Y0-Y377 或者Y0.0-Y37.7</term>
	///     <term>8</term>
	///     <term></term>
	///   </item>
	/// </list>
	/// 寄存器、 字元件、字变量地址定义：
	/// <list type="table">
	///   <listheader>
	///     <term>地址名称</term>
	///     <term>地址代号</term>
	///     <term>地址范围</term>
	///     <term>地址进制</term>
	///     <term>备注</term>
	///   </listheader>
	///   <item>
	///     <term>数据寄存器</term>
	///     <term>D</term>
	///     <term>D0-D8511</term>
	///     <term>10</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>数据寄存器</term>
	///     <term>SD</term>
	///     <term>SD0-SD1023</term>
	///     <term>10</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term></term>
	///     <term>R</term>
	///     <term>R0-R32767</term>
	///     <term>10</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>定时器</term>
	///     <term>T</term>
	///     <term>T0-T511</term>
	///     <term>10</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>计数器</term>
	///     <term>C</term>
	///     <term>C0-C199,C200-C255</term>
	///     <term>10</term>
	///     <term>其实C200-C255的计数器是32位的</term>
	///   </item>
	/// </list>
	/// <c>我们再来看看XP系列，就是少了一点访问的数据类型，然后，地址范围也不一致</c><br />
	/// 线圈、 位元件、位变量地址定义
	/// <list type="table">
	///   <listheader>
	///     <term>地址名称</term>
	///     <term>地址代号</term>
	///     <term>地址范围</term>
	///     <term>地址进制</term>
	///     <term>备注</term>
	///   </listheader>
	///   <item>
	///     <term>中间寄电器</term>
	///     <term>M</term>
	///     <term>M0-M3071，M8000-M8511</term>
	///     <term>10</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term></term>
	///     <term>S</term>
	///     <term>S0-S999</term>
	///     <term>10</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>定时器</term>
	///     <term>T</term>
	///     <term>T0-T255</term>
	///     <term>10</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>计数器</term>
	///     <term>C</term>
	///     <term>C0-C255</term>
	///     <term>10</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>输入</term>
	///     <term>X</term>
	///     <term>X0-X377 或者X0.0-X37.7</term>
	///     <term>8</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>输出</term>
	///     <term>Y</term>
	///     <term>Y0-Y377 或者Y0.0-Y37.7</term>
	///     <term>8</term>
	///     <term></term>
	///   </item>
	/// </list>
	/// 寄存器、 字元件、字变量地址定义：
	/// <list type="table">
	///   <listheader>
	///     <term>地址名称</term>
	///     <term>地址代号</term>
	///     <term>地址范围</term>
	///     <term>地址进制</term>
	///     <term>备注</term>
	///   </listheader>
	///   <item>
	///     <term>数据寄存器</term>
	///     <term>D</term>
	///     <term>D0-D8511</term>
	///     <term>10</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>定时器</term>
	///     <term>T</term>
	///     <term>T0-T255</term>
	///     <term>10</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>计数器</term>
	///     <term>C</term>
	///     <term>C0-C199,C200-C255</term>
	///     <term>10</term>
	///     <term>其实C200-C255的计数器是32位的</term>
	///   </item>
	/// </list>
	/// </example>
	public class InovanceSerial : ModbusRtu
	{
		#region Constructor

		/// <summary>
		/// 实例化一个默认的对象
		/// </summary>
		public InovanceSerial( ) : base( ) { Series = InovanceSeries.AM; }

		/// <summary>
		/// 指定服务器地址，端口号，客户端自己的站号来初始化<br />
		/// Specify the server address, port number, and client's own station number to initialize
		/// </summary>
		/// <param name="station">客户端自身的站号</param>
		public InovanceSerial( byte station = 0x01 ) : base( station ) { Series = InovanceSeries.AM; }

		/// <summary>
		/// 指定服务器地址，端口号，客户端自己的站号来初始化<br />
		/// Specify the server address, port number, and client's own station number to initialize
		/// </summary>
		/// <param name="series">PLC的系列选择</param>
		/// <param name="station">客户端自身的站号</param>
		public InovanceSerial( InovanceSeries series, byte station = 0x01 ) : base( station ) { Series = series; }

		#endregion

		#region Public Properties

		/// <summary>
		/// 获取或设置汇川的系列，默认为AM系列
		/// </summary>
		public InovanceSeries Series { get; set; }

		#endregion

		#region Override

		/// <inheritdoc/>
		public override OperateResult<string> TranslateToModbusAddress( string address, byte modbusCode )
		{
			return InovanceHelper.PraseInovanceAddress( Series, address, modbusCode );
		}

		#endregion

		#region Object Override

		/// <inheritdoc/>
		public override string ToString( ) => $"InovanceSerial<{Series}>[{PortName}:{BaudRate}]";

		#endregion
	}
}
