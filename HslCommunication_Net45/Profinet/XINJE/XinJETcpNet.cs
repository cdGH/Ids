using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HslCommunication.ModBus;

namespace HslCommunication.Profinet.XINJE
{
	/// <summary>
	/// 信捷PLC的XC,XD,XL系列的网口通讯类，底层使用ModbusTcp协议实现，每个系列支持的地址类型及范围不一样，详细参考API文档<br />
	/// XC, XD, XL series of Xinje PLC's network port communication class, the bottom layer is realized by ModbusTcp protocol, 
	/// each series supports different address types and ranges, please refer to the API document for details
	/// </summary>
	/// <remarks>
	/// 对于XC系列适用于XC1/XC2/XC3/XC5/XCM/XCC系列，线圈支持X,Y,S,M,T,C，寄存器支持D,F,E,T,C<br />
	/// 对于XD,XL系列适用于XD1/XD2/XD3/XD5/XDM/XDC/XD5E/XDME/XDH/XL1/XL3/XL5/XL5E/XLME，
	/// 线圈支持X,Y,S,M,SM,T,C,ET,SEM,HM,HS,HT,HC,HSC 寄存器支持D,ID,QD,SD,TD,CD,ETD,HD,HSD,HTD,HCD,HSCD,FD,SFD,FS<br />
	/// </remarks>
	/// <example>
	/// <inheritdoc cref="XinJESerial" path="example"/>
	/// </example>
	public class XinJETcpNet : ModbusTcpNet
	{
		#region Constructor

		/// <summary>
		/// 实例化一个默认的对象
		/// </summary>
		public XinJETcpNet( ) : base( ) { Series = XinJESeries.XC; }

		/// <summary>
		/// 通过指定站号，ip地址，端口号来实例化一个新的对象
		/// </summary>
		/// <param name="ipAddress">Ip地址</param>
		/// <param name="port">端口号</param>
		/// <param name="station">站号信息</param>
		public XinJETcpNet( string ipAddress, int port = 502, byte station = 0x01 ) : base( ipAddress, port, station )
		{
			Series = XinJESeries.XC;
		}

		/// <summary>
		/// 通过指定站号，IP地址，端口以及PLC的系列来实例化一个新的对象<br />
		/// Instantiate a new object by specifying the station number and PLC series
		/// </summary>
		/// <param name="series">PLC的系列</param>
		/// <param name="ipAddress">Ip地址</param>
		/// <param name="port">端口号</param>
		/// <param name="station">站号信息</param>
		public XinJETcpNet( XinJESeries series, string ipAddress, int port = 502, byte station = 0x01 ) : base( ipAddress, port, station )
		{
			Series = series;
		}

		#endregion

		#region Public Properties

		/// <summary>
		/// 获取或设置当前的信捷PLC的系列，默认XC系列
		/// </summary>
		public XinJESeries Series { get; set; }

		#endregion

		#region Override

		/// <inheritdoc/>
		public override OperateResult<string> TranslateToModbusAddress( string address, byte modbusCode )
		{
			return XinJEHelper.PraseXinJEAddress( this.Series, address, modbusCode );
		}

		#endregion

		#region Object Override

		/// <inheritdoc/>
		public override string ToString( ) => $"XinJETcpNet<{Series}>[{IpAddress}:{Port}]";

		#endregion
	}
}
