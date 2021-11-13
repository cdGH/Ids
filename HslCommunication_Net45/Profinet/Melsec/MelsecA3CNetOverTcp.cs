using HslCommunication.BasicFramework;
using HslCommunication.Core;
using HslCommunication.Core.Address;
using HslCommunication.Core.Net;
using System;
using System.Collections.Generic;
using HslCommunication.Reflection;
using System.Linq;
using System.Text;
using HslCommunication.Profinet.Melsec.Helper;
#if !NET35 && !NET20
using System.Threading.Tasks;
#endif

namespace HslCommunication.Profinet.Melsec
{
	/// <summary>
	/// 基于Qna 兼容3C帧的格式一的通讯，具体的地址需要参照三菱的基本地址，本类是基于tcp通讯的实现<br />
	/// Based on Qna-compatible 3C frame format one communication, the specific address needs to refer to the basic address of Mitsubishi. This class is based on TCP communication.
	/// </summary>
	/// <remarks>
	/// 地址可以携带站号信息，例如：s=2;D100
	/// </remarks>
	/// <example>
	/// 地址的输入的格式说明如下：
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
	///     <term>X100,X1A0</term>
	///     <term>16</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>输出继电器</term>
	///     <term>Y</term>
	///     <term>Y100,Y1A0</term>
	///     <term>16</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///    <item>
	///     <term>锁存继电器</term>
	///     <term>L</term>
	///     <term>L100,L200</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>报警器</term>
	///     <term>F</term>
	///     <term>F100,F200</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>边沿继电器</term>
	///     <term>V</term>
	///     <term>V100,V200</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>链接继电器</term>
	///     <term>B</term>
	///     <term>B100,B1A0</term>
	///     <term>16</term>
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
	///     <term>数据寄存器</term>
	///     <term>D</term>
	///     <term>D1000,D2000</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>×</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>链接寄存器</term>
	///     <term>W</term>
	///     <term>W100,W1A0</term>
	///     <term>16</term>
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
	///   <item>
	///     <term>ZR文件寄存器</term>
	///     <term>ZR</term>
	///     <term>ZR100,ZR2A0</term>
	///     <term>16</term>
	///     <term>√</term>
	///     <term>×</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>变址寄存器</term>
	///     <term>Z</term>
	///     <term>Z100,Z200</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>×</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>定时器的触点</term>
	///     <term>TS</term>
	///     <term>TS100,TS200</term>
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
	///     <term>累计定时器的触点</term>
	///     <term>SS</term>
	///     <term>SS100,SS200</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>累计定时器的线圈</term>
	///     <term>SC</term>
	///     <term>SC100,SC200</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>累计定时器的当前值</term>
	///     <term>SN</term>
	///     <term>SN100,SN200</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>×</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>计数器的触点</term>
	///     <term>CS</term>
	///     <term>CS100,CS200</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
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
	///     <term>计数器的当前值</term>
	///     <term>CN</term>
	///     <term>CN100,CN200</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>×</term>
	///     <term></term>
	///   </item>
	/// </list>
	/// </example>
	public class MelsecA3CNetOverTcp : NetworkDeviceBase, IReadWriteA3C
	{
		#region Constructor

		/// <summary>
		/// 实例化默认的对象<br />
		/// Instantiate the default object
		/// </summary>
		public MelsecA3CNetOverTcp( )
		{
			this.WordLength    = 1;
			this.ByteTransform = new RegularByteTransform( );
			this.SleepTime     = 20;
		}

		/// <summary>
		/// 指定ip地址和端口号来实例化对象<br />
		/// Specify the IP address and port number to instantiate the object
		/// </summary>
		/// <param name="ipAddress">Ip地址信息</param>
		/// <param name="port">端口号信息</param>
		public MelsecA3CNetOverTcp( string ipAddress, int port ) : this( )
		{
			this.IpAddress     = ipAddress;
			this.Port          = port;
		}

		#endregion

		#region Public Member

		/// <inheritdoc cref="IReadWriteA3C.Station"/>
		public byte Station { get => station; set => station = value; }

		/// <inheritdoc cref="IReadWriteA3C.SumCheck"/>
		public bool SumCheck { get; set; } = true;

		/// <inheritdoc cref="IReadWriteA3C.Format"/>
		public int Format { get; set; } = 1;

		#endregion

		#region Read Write Support

		/// <summary>
		/// 批量读取PLC的数据，以字为单位，支持读取X,Y,M,S,D,T,C，具体的地址范围需要根据PLC型号来确认<br />
		/// Read PLC data in batches, in units of words, supports reading X, Y, M, S, D, T, C. The specific address range needs to be confirmed according to the PLC model
		/// </summary>
		/// <param name="address">地址信息</param>
		/// <param name="length">数据长度</param>
		/// <returns>读取结果信息</returns>
		[HslMqttApi( "ReadByteArray", "" )]
		public override OperateResult<byte[]> Read( string address, ushort length ) => MelsecA3CNetHelper.Read( this, address, length );

		/// <summary>
		/// 批量写入PLC的数据，以字为单位，也就是说最少2个字节信息，支持X,Y,M,S,D,T,C，具体的地址范围需要根据PLC型号来确认<br />
		/// The data written to the PLC in batches is in units of words, that is, at least 2 bytes of information. It supports X, Y, M, S, D, T, and C. The specific address range needs to be confirmed according to the PLC model.
		/// </summary>
		/// <param name="address">地址信息</param>
		/// <param name="value">数据值</param>
		/// <returns>是否写入成功</returns>
		[HslMqttApi( "WriteByteArray", "" )]
		public override OperateResult Write( string address, byte[] value ) => MelsecA3CNetHelper.Write( this, address, value );

		#endregion

		#region Async Read Write Support
#if !NET35 && !NET20
		/// <inheritdoc cref="Read(string, ushort)"/>
		public override async Task<OperateResult<byte[]>> ReadAsync( string address, ushort length ) => await MelsecA3CNetHelper.ReadAsync( this, address, length );

		/// <inheritdoc cref="Write(string, byte[])"/>
		public override async Task<OperateResult> WriteAsync( string address, byte[] value ) => await MelsecA3CNetHelper.WriteAsync( this, address, value );
#endif
		#endregion

		#region Bool Read Write

		/// <summary>
		/// 批量读取bool类型数据，支持的类型为X,Y,S,T,C，具体的地址范围取决于PLC的类型<br />
		/// Read bool data in batches. The supported types are X, Y, S, T, C. The specific address range depends on the type of PLC.
		/// </summary>
		/// <param name="address">地址信息，比如X10,Y17，注意X，Y的地址是8进制的</param>
		/// <param name="length">读取的长度</param>
		/// <returns>读取结果信息</returns>
		[HslMqttApi( "ReadBoolArray", "" )]
		public override OperateResult<bool[]> ReadBool( string address, ushort length ) => MelsecA3CNetHelper.ReadBool( this, address, length );

		/// <summary>
		/// 批量写入bool类型的数组，支持的类型为X,Y,S,T,C，具体的地址范围取决于PLC的类型<br />
		/// Write arrays of type bool in batches. The supported types are X, Y, S, T, C. The specific address range depends on the type of PLC.
		/// </summary>
		/// <param name="address">PLC的地址信息</param>
		/// <param name="value">数据信息</param>
		/// <returns>是否写入成功</returns>
		[HslMqttApi( "WriteBoolArray", "" )]
		public override OperateResult Write( string address, bool[] value ) => MelsecA3CNetHelper.Write( this, address, value );

		#endregion

		#region Async Bool Read Write
#if !NET35 && !NET20
		/// <inheritdoc cref="ReadBool(string, ushort)"/>
		public override async Task<OperateResult<bool[]>> ReadBoolAsync( string address, ushort length ) => await MelsecA3CNetHelper.ReadBoolAsync( this, address, length );

		/// <inheritdoc cref="Write(string, bool[])"/>
		public override async Task<OperateResult> WriteAsync( string address, bool[] value ) => await MelsecA3CNetHelper.WriteAsync( this, address, value );
#endif
		#endregion

		#region Remote Operate

		/// <inheritdoc cref="MelsecA3CNetHelper.RemoteRun"/>
		[HslMqttApi]
		public OperateResult RemoteRun( ) => MelsecA3CNetHelper.RemoteRun( this );

		/// <inheritdoc cref="MelsecA3CNetHelper.RemoteStop"/>
		[HslMqttApi]
		public OperateResult RemoteStop( ) => MelsecA3CNetHelper.RemoteStop( this );

		/// <inheritdoc cref="MelsecA3CNetHelper.ReadPlcType"/>
		[HslMqttApi]
		public OperateResult<string> ReadPlcType( ) => MelsecA3CNetHelper.ReadPlcType( this );

		#endregion

		#region Async Remote Operate
#if !NET35 && !NET20
		/// <inheritdoc cref="RemoteRun"/>
		public async Task<OperateResult> RemoteRunAsync( ) => await MelsecA3CNetHelper.RemoteRunAsync( this );

		/// <inheritdoc cref="RemoteStop"/>
		public async Task<OperateResult> RemoteStopAsync( ) => await MelsecA3CNetHelper.RemoteStopAsync( this );

		/// <inheritdoc cref="ReadPlcType"/>
		public async Task<OperateResult<string>> ReadPlcTypeAsync( ) => await MelsecA3CNetHelper.ReadPlcTypeAsync( this );
#endif
		#endregion

		#region Object Override

		/// <inheritdoc/>
		public override string ToString( ) => $"MelsecA3CNetOverTcp[{IpAddress}:{Port}]";

		#endregion

		#region Private Member

		private byte station = 0x00;                 // PLC的站号信息

		#endregion

	}
}
