using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HslCommunication.BasicFramework;
using HslCommunication.Core;
using HslCommunication.Core.Net;
using HslCommunication.Reflection;
using HslCommunication.Core.IMessage;
using HslCommunication.Core.Address;
#if !NET35 && !NET20
using System.Threading.Tasks;
#endif

namespace HslCommunication.Profinet.Fuji
{
	/// <summary>
	/// 富士PLC的SPB协议，详细的地址信息见api文档说明，地址可以携带站号信息，例如：s=2;D100，PLC侧需要配置无BCC计算，包含0D0A结束码<br />
	/// Fuji PLC's SPB protocol. For detailed address information, see the api documentation, 
	/// The address can carry station number information, for example: s=2;D100, PLC side needs to be configured with no BCC calculation, including 0D0A end code
	/// </summary>
	/// <remarks>
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
	///     <term>读写字单位的时候，M2代表位的M32</term>
	///   </item>
	///   <item>
	///     <term>输入继电器</term>
	///     <term>X</term>
	///     <term>X10,X20</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term>读取字单位的时候，X2代表位的X32</term>
	///   </item>
	///   <item>
	///     <term>输出继电器</term>
	///     <term>Y</term>
	///     <term>Y10,Y20</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term>读写字单位的时候，Y2代表位的Y32</term>
	///   </item>
	///   <item>
	///     <term>锁存继电器</term>
	///     <term>L</term>
	///     <term>L100,L200</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>定时器的线圈</term>
	///     <term>TC</term>
	///     <term>TC100,TC200</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>定时器的当前值</term>
	///     <term>TN</term>
	///     <term>TN100,TN200</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>×</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>计数器的线圈</term>
	///     <term>CC</term>
	///     <term>CC100,CC200</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>计数器的当前</term>
	///     <term>CN</term>
	///     <term>CN100,CN200</term>
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
	///     <term>√</term>
	///     <term>读位的时候，D10.15代表第10个字的第15位</term>
	///   </item>
	///   <item>
	///     <term>文件寄存器</term>
	///     <term>R</term>
	///     <term>R100,R200</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term>读位的时候，R10.15代表第10个字的第15位</term>
	///   </item>
	///   <item>
	///     <term>链接寄存器</term>
	///     <term>W</term>
	///     <term>W100,W200</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term>读位的时候，W10.15代表第10个字的第15位</term>
	///   </item>
	/// </list>
	/// </remarks>
	public class FujiSPBOverTcp : NetworkDeviceBase
	{
		#region Constructor

		/// <summary>
		/// 使用默认的构造方法实例化对象<br />
		/// Instantiate the object using the default constructor
		/// </summary>
		public FujiSPBOverTcp( )
		{
			this.WordLength         = 1;
			base.LogMsgFormatBinary = false;
			this.ByteTransform      = new RegularByteTransform( );
			this.SleepTime          = 20;
		}

		/// <summary>
		/// 使用指定的ip地址和端口来实例化一个对象<br />
		/// Instantiate an object with the specified IP address and port
		/// </summary>
		/// <param name="ipAddress">设备的Ip地址</param>
		/// <param name="port">设备的端口号</param>
		public FujiSPBOverTcp( string ipAddress, int port ) : this( )
		{
			this.IpAddress          = ipAddress;
			this.Port               = port;
		}

		/// <inheritdoc/>
		protected override INetMessage GetNewNetMessage( ) => new FujiSPBMessage( );

		#endregion

		#region Public Member

		/// <summary>
		/// PLC的站号信息<br />
		/// PLC station number information
		/// </summary>
		public byte Station { get => station; set => station = value; }

		#endregion

		#region Read Write Support

		/// <inheritdoc cref="FujiSPBHelper.Read(IReadWriteDevice, byte, string, ushort)"/>
		[HslMqttApi( "ReadByteArray", "" )]
		public override OperateResult<byte[]> Read( string address, ushort length ) => FujiSPBHelper.Read( this, this.station, address, length );

		/// <inheritdoc cref="FujiSPBHelper.Write(IReadWriteDevice, byte, string, byte[])"/>
		[HslMqttApi( "WriteByteArray", "" )]
		public override OperateResult Write( string address, byte[] value ) => FujiSPBHelper.Write( this, this.station, address, value );

		/// <inheritdoc cref="FujiSPBHelper.ReadBool(IReadWriteDevice, byte, string, ushort)"/>
		[HslMqttApi( "ReadBoolArray", "" )]
		public override OperateResult<bool[]> ReadBool( string address, ushort length ) => FujiSPBHelper.ReadBool( this, this.station, address, length );

		/// <inheritdoc cref="FujiSPBHelper.Write(IReadWriteDevice, byte, string, bool)"/>
		[HslMqttApi( "WriteBool", "" )]
		public override OperateResult Write( string address, bool value ) => FujiSPBHelper.Write( this, this.station, address, value );

		#endregion

		#region Async Read Write Support
#if !NET35 && !NET20
		/// <inheritdoc cref="Read(string, ushort)"/>
		public override async Task<OperateResult<byte[]>> ReadAsync( string address, ushort length ) => await FujiSPBHelper.ReadAsync( this, this.station, address, length );

		/// <inheritdoc cref="Write(string, byte[])"/>
		public override async Task<OperateResult> WriteAsync( string address, byte[] value ) => await FujiSPBHelper.WriteAsync( this, this.station, address, value );

		/// <inheritdoc cref="ReadBool(string, ushort)"/>
		public async override Task<OperateResult<bool[]>> ReadBoolAsync( string address, ushort length ) => await FujiSPBHelper.ReadBoolAsync( this, this.station, address, length );

		/// <inheritdoc cref="Write(string, bool)"/>
		public async override Task<OperateResult> WriteAsync( string address, bool value ) => await FujiSPBHelper.WriteAsync( this, this.station, address, value );
#endif
		#endregion

		#region Private Member

		private byte station = 0x01;                 // PLC的站号信息

		#endregion

		#region Object Override

		/// <inheritdoc/>
		public override string ToString( ) => $"FujiSPBOverTcp[{IpAddress}:{Port}]";

		#endregion

	}
}
