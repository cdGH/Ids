using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HslCommunication.Profinet.Melsec
{
	/// <summary>
	/// 在三菱的PLC通信的MC协议中，分为串行通信的报文和以太网接口的报文。<br />
	/// 在串口通信中，共有以下几种帧，其中1C,2C,3C帧支持格式1，2，3，4，在C帧里支持格式5通信<br />
	/// <list type="number">
	/// <item>4C帧，QnA系列串行通信模块专用协议（Qna扩展帧）</item>
	/// <item>3C帧，QnA系列串行通信模块专用协议（Qna帧）</item>
	/// <item>2C帧，QnA系列串行通信模块专用协议（Qna简易帧）</item>
	/// <item>1C帧，A系列计算机链接模块专用协议</item>
	/// </list>
	/// 在以太网通信中，共有以下几种帧，每种帧支持二进制和ASCII格式
	/// <list type="number">
	/// <item>4E帧，是3E帧上附加了“序列号”。</item>
	/// <item>3E帧，QnA系列以太网接口模块的报文格式，兼容SLMP的报文格式</item>
	/// <item>1E帧，A系列以太网接口模块的报文格式</item>
	/// </list>
	/// 在以太网通信里，HSL主要针对1E帧协议和3E帧协议进行实现，大概说一下怎么选择通信类对象，对于三菱PLC而言，需要事先在PLC侧的网络配置中进行
	/// 相关的配置操作，具体是配置二进制格式还是ASCII格式，然后配置端口，配置TCP还是UDP协议。<br />
	/// <see cref="MelsecMcNet"/>，<see cref="MelsecMcAsciiNet"/>，<see cref="MelsecMcUdp"/>, <see cref="MelsecMcAsciiUdp"/> 这四个类都是MC协议的Qna兼容3E帧实现，分别
	/// 是TCP二进制，TCP的ASCII，UDP的二进制，UDP的ASCI格式。适用Q系列，L系列，FX5U系列，还有以太网模块QJ71E71。<br />
	/// <see cref="MelsecA1ENet"/>, <see cref="MelsecA1EAsciiNet"/> 这两个类是MC协议的Qna兼容1E协议实现，
	/// 分别是二进制和ASCII格式的实现，主要适用A系列的PLC，Fx3u，已经有些老的PLC，使用了北辰模块实现了通信。<br />
	/// <see cref="MelsecA3CNet"/> 是MC协议的3C帧的实现，主要支持串行通信的模块的实现。<br />
	/// <see cref="MelsecFxSerial"/> 是FX编程口的协议的实现，测试不太稳定。具体支持的系列需要参照类的说明。<br />
	/// <br />
	/// 三菱Q系列PLC带以太网模块，使用MC 二进制协议通讯，在网络中断情况下无法正常连接的情况，解决方案如下：<br />
	/// 1. 生存确认选择确认模式；<br />
	/// 2. 初始设置中将"对象目标生存确认开始间隔定时器"从1200改为12<br />
	/// </summary>
	[System.Runtime.CompilerServices.CompilerGeneratedAttribute( )]
	public class NamespaceDoc
	{
	}
}
