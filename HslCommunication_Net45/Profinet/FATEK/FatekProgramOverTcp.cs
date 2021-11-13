using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HslCommunication.BasicFramework;
using HslCommunication.Core;
using HslCommunication.Core.Net;
using HslCommunication.Reflection;
using HslCommunication.Core.Address;
#if !NET35 && !NET20
using System.Threading.Tasks;
#endif

namespace HslCommunication.Profinet.FATEK
{
	/// <summary>
	/// 台湾永宏公司的编程口协议，此处是基于tcp的实现，地址信息请查阅api文档信息，地址可以携带站号信息，例如 s=2;D100<br />
	/// The programming port protocol of Taiwan Yonghong company, here is the implementation based on TCP, 
	/// please refer to the API information for the address information, The address can carry station number information, such as s=2;D100
	/// </summary>
	/// <remarks>
	/// 支持位访问：M,X,Y,S,T(触点),C(触点)，字访问：RT(当前值),RC(当前值)，D，R；具体参照API文档
	/// </remarks>
	/// <example>
	/// 其所支持的地址形式如下：
	/// <list type="table">
	///   <listheader>
	///     <term>地址名称</term>
	///     <term>地址代号</term>
	///     <term>示例</term>
	///     <term>地址进制</term>
	///     <term>字操作</term>
	///     <term>位操作</term>
	///     <term>备注</term>
	///   </listheader>
	///   <item>
	///     <term>内部继电器</term>
	///     <term>M</term>
	///     <term>M100,M200</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>输入继电器</term>
	///     <term>X</term>
	///     <term>X10,X20</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>输出继电器</term>
	///     <term>Y</term>
	///     <term>Y10,Y20</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>步进继电器</term>
	///     <term>S</term>
	///     <term>S100,S200</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>定时器的触点</term>
	///     <term>T</term>
	///     <term>T100,T200</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>定时器的当前值</term>
	///     <term>RT</term>
	///     <term>RT100,RT200</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>×</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>计数器的触点</term>
	///     <term>C</term>
	///     <term>C100,C200</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>计数器的当前</term>
	///     <term>RC</term>
	///     <term>RC100,RC200</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>×</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>数据寄存器</term>
	///     <term>D</term>
	///     <term>D1000,D2000</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>×</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>文件寄存器</term>
	///     <term>R</term>
	///     <term>R100,R200</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>×</term>
	///     <term></term>
	///   </item>
	/// </list>
	/// </example>
	public class FatekProgramOverTcp : NetworkDeviceBase
	{
		#region Constructor

		/// <summary>
		/// 实例化默认的构造方法<br />
		/// Instantiate the default constructor
		/// </summary>
		public FatekProgramOverTcp( )
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
		public FatekProgramOverTcp( string ipAddress, int port ) : this( )
		{
			this.IpAddress     = ipAddress;
			this.Port          = port;
		}

		#endregion

		#region Public Member

		/// <summary>
		/// PLC的站号信息，需要和实际的设置值一致，默认为1<br />
		/// The station number information of the PLC needs to be consistent with the actual setting value. The default is 1.
		/// </summary>
		public byte Station { get => station; set => station = value; }

		#endregion

		#region Read Write Support

		/// <inheritdoc cref="FatekProgramHelper.Read(IReadWriteDevice, byte, string, ushort)"/>
		[HslMqttApi( "ReadByteArray", "" )]
		public override OperateResult<byte[]> Read( string address, ushort length ) => FatekProgramHelper.Read( this, this.station, address, length );

		/// <inheritdoc cref="FatekProgramHelper.Write(IReadWriteDevice, byte, string, byte[])"/>
		[HslMqttApi( "WriteByteArray", "" )]
		public override OperateResult Write( string address, byte[] value ) => FatekProgramHelper.Write( this, this.station, address, value );

		#endregion

		#region Async Read Write Support
#if !NET35 && !NET20
		/// <inheritdoc cref="Read(string, ushort)"/>
		public override async Task<OperateResult<byte[]>> ReadAsync( string address, ushort length ) => await FatekProgramHelper.ReadAsync( this, this.station, address, length );

		/// <inheritdoc cref="Write(string, byte[])"/>
		public override async Task<OperateResult> WriteAsync( string address, byte[] value ) => await FatekProgramHelper.WriteAsync( this, this.station, address, value );
#endif
		#endregion

		#region Bool Read Write

		/// <inheritdoc cref="FatekProgramHelper.ReadBool(IReadWriteDevice, byte, string, ushort)"/>
		[HslMqttApi( "ReadBoolArray", "" )]
		public override OperateResult<bool[]> ReadBool( string address, ushort length ) => FatekProgramHelper.ReadBool( this, this.station, address, length );

		/// <inheritdoc cref="FatekProgramHelper.Write(IReadWriteDevice, byte, string, bool[])"/>
		[HslMqttApi( "WriteBoolArray", "" )]
		public override OperateResult Write( string address, bool[] value ) => FatekProgramHelper.Write( this, this.station, address, value );

		#endregion

		#region Async Bool Read Write
#if !NET35 && !NET20
		/// <inheritdoc cref="ReadBool(string, ushort)"/>
		public override async Task<OperateResult<bool[]>> ReadBoolAsync( string address, ushort length ) => await FatekProgramHelper.ReadBoolAsync( this, this.station, address, length );

		/// <inheritdoc cref="Write(string, bool[])"/>
		public override async Task<OperateResult> WriteAsync( string address, bool[] value ) => await FatekProgramHelper.WriteAsync( this, this.station, address, value );
#endif
		#endregion

		#region Object Override

		/// <inheritdoc/>
		public override string ToString( ) => $"FatekProgramOverTcp[{IpAddress}:{Port}]";

		#endregion

		#region Private Member

		private byte station = 0x01;                 // PLC的站号信息

		#endregion

	}
}
