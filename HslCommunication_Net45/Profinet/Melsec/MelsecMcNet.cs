using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HslCommunication.BasicFramework;
using HslCommunication.Core;
using HslCommunication.Core.IMessage;
using HslCommunication.Core.Net;
using HslCommunication.Core.Address;
using HslCommunication.Reflection;
using HslCommunication.Profinet.Melsec.Helper;
#if !NET35 && !NET20
using System.Threading.Tasks;
#endif

namespace HslCommunication.Profinet.Melsec
{
	/// <summary>
	/// 三菱PLC通讯类，采用Qna兼容3E帧协议实现，需要在PLC侧先的以太网模块先进行配置，必须为二进制通讯<br />
	/// Mitsubishi PLC communication class is implemented using Qna compatible 3E frame protocol. 
	/// The Ethernet module on the PLC side needs to be configured first. It must be binary communication.
	/// </summary>
	/// <remarks>
	/// 支持读写的数据类型详细参考API文档，支持高级的数据读取，例如读取智能模块，缓冲存储器等等。
	/// </remarks>
	/// <list type="number">
	/// 目前组件测试通过的PLC型号列表，有些来自于网友的测试
	/// <item>Q06UDV PLC  感谢hwdq0012</item>
	/// <item>fx5u PLC  感谢山楂</item>
	/// <item>Q02CPU PLC </item>
	/// <item>L02CPU PLC </item>
	/// </list>
	/// 地址的输入的格式支持多种复杂的地址表示方式：
	/// <list type="number">
	/// <item>[商业授权] 扩展的数据地址: 表示为 ext=1;W100  访问扩展区域为1的W100的地址信息</item>
	/// <item>[商业授权] 缓冲存储器地址: 表示为 mem=32  访问地址为32的本站缓冲存储器地址</item>
	/// <item>[商业授权] 智能模块地址：表示为 module=3;4106  访问模块号3，偏移地址是4106的数据，偏移地址需要根据模块的详细信息来确认。</item>
	/// <item>[商业授权] 基于标签的地址: 表示位 s=AAA  假如标签的名称为AAA，但是标签的读取是有条件的，详细参照<see cref="ReadTags(string, ushort)"/></item>
	/// <item>普通的数据地址，参照下面的信息</item>
	/// </list>
	/// <example><list type="table">
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
	///     <term>8进制用0开头，X017</term>
	///   </item>
	///   <item>
	///     <term>输出继电器</term>
	///     <term>Y</term>
	///     <term>Y100,Y1A0</term>
	///     <term>16</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term>8进制用0开头，Y017</term>
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
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Profinet\melsecTest.cs" region="Usage" title="简单的短连接使用" />
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Profinet\melsecTest.cs" region="Usage2" title="简单的长连接使用" />
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Profinet\melsecTest.cs" region="ReadExample1" title="基本的读取示例" />
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Profinet\melsecTest.cs" region="ReadExample2" title="批量读取示例" />
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Profinet\melsecTest.cs" region="ReadExample3" title="随机字读取示例" />
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Profinet\melsecTest.cs" region="ReadExample4" title="随机批量字读取示例" />
	/// </example>
	public class MelsecMcNet : NetworkDeviceBase, IReadWriteMc
	{
		#region Constructor

		/// <summary>
		/// 实例化三菱的Qna兼容3E帧协议的通讯对象<br />
		/// Instantiate the communication object of Mitsubishi's Qna compatible 3E frame protocol
		/// </summary>
		public MelsecMcNet( )
		{
			this.WordLength    = 1;
			this.ByteTransform = new RegularByteTransform( );
		}

		/// <summary>
		/// 指定ip地址和端口号来实例化一个默认的对象<br />
		/// Specify the IP address and port number to instantiate a default object
		/// </summary>
		/// <param name="ipAddress">PLC的Ip地址</param>
		/// <param name="port">PLC的端口</param>
		public MelsecMcNet( string ipAddress, int port ) : this( )
		{
			this.IpAddress     = ipAddress;
			this.Port          = port;
		}

		/// <inheritdoc/>
		protected override INetMessage GetNewNetMessage( ) => new MelsecQnA3EBinaryMessage( );

		/// <inheritdoc cref="IReadWriteMc.McType"/>
		public McType McType => McType.McBinary;

		#endregion

		#region Public Member

		/// <inheritdoc cref="IReadWriteMc.NetworkNumber"/>
		public byte NetworkNumber { get; set; } = 0x00;

		/// <inheritdoc cref="IReadWriteMc.NetworkStationNumber"/>
		public byte NetworkStationNumber { get; set; } = 0x00;

		#endregion

		#region Virtual Address Analysis

		/// <inheritdoc cref="IReadWriteMc.McAnalysisAddress(string, ushort)"/>
		public virtual OperateResult<McAddressData> McAnalysisAddress( string address, ushort length ) => McAddressData.ParseMelsecFrom( address, length );

		/// <inheritdoc/>
		protected override byte[] PackCommandWithHeader( byte[] command )
		{
			return McBinaryHelper.PackMcCommand( command, this.NetworkNumber, this.NetworkStationNumber );
		}

		/// <inheritdoc/>
		protected override OperateResult<byte[]> UnpackResponseContent( byte[] send, byte[] response )
		{
			OperateResult check = McBinaryHelper.CheckResponseContentHelper( response );
			if (!check.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( check );

			return OperateResult.CreateSuccessResult( response.RemoveBegin( 11 ) );
		}

		/// <inheritdoc cref="IReadWriteMc.ExtractActualData(byte[], bool)"/>
		public byte[] ExtractActualData( byte[] response, bool isBit ) => McBinaryHelper.ExtractActualDataHelper( response, isBit );

		#endregion

		#region Read Write Support

		/// <inheritdoc/>
		[HslMqttApi( "ReadByteArray", "" )]
		public override OperateResult<byte[]> Read( string address, ushort length ) => McHelper.Read( this, address, length );

		/// <inheritdoc/>
		[HslMqttApi( "WriteByteArray", "" )]
		public override OperateResult Write( string address, byte[] value ) => McHelper.Write( this, address, value );

#if !NET35 && !NET20
		/// <inheritdoc/>
		public override async Task<OperateResult<byte[]>> ReadAsync( string address, ushort length ) => await McHelper.ReadAsync( this, address, length );

		/// <inheritdoc/>
		public override async Task<OperateResult> WriteAsync( string address, byte[] value ) => await McHelper.WriteAsync( this, address, value );
#endif
		#endregion

		#region Read Random

		/// <inheritdoc cref="McHelper.ReadRandom(IReadWriteMc, string[])"/>
		[HslMqttApi( "随机读取PLC的数据信息，可以跨地址，跨类型组合，但是每个地址只能读取一个word，也就是2个字节的内容。收到结果后，需要自行解析数据" )]
		public OperateResult<byte[]> ReadRandom( string[] address ) => McHelper.ReadRandom( this, address );

		/// <inheritdoc cref="McHelper.ReadRandom(IReadWriteMc, string[], ushort[])"/>
		[HslMqttApi( ApiTopic = "ReadRandoms", Description = "随机读取PLC的数据信息，可以跨地址，跨类型组合，每个地址是任意的长度。收到结果后，需要自行解析数据，目前只支持字地址，比如D区，W区，R区，不支持X，Y，M，B，L等等" )]
		public OperateResult<byte[]> ReadRandom( string[] address, ushort[] length ) => McHelper.ReadRandom( this, address, length );

		/// <inheritdoc cref="McHelper.ReadRandomInt16(IReadWriteMc, string[])"/>
		public OperateResult<short[]> ReadRandomInt16( string[] address ) => McHelper.ReadRandomInt16( this, address );

		/// <inheritdoc cref="McHelper.ReadRandomUInt16(IReadWriteMc, string[])"/>
		public OperateResult<ushort[]> ReadRandomUInt16( string[] address ) => McHelper.ReadRandomUInt16( this, address );

#if !NET35 && !NET20
		/// <inheritdoc cref="ReadRandom(string[])"/>
		public async Task<OperateResult<byte[]>> ReadRandomAsync( string[] address ) => await McHelper.ReadRandomAsync( this, address );

		/// <inheritdoc cref="ReadRandom(string[], ushort[])"/>
		public async Task<OperateResult<byte[]>> ReadRandomAsync( string[] address, ushort[] length ) => await McHelper.ReadRandomAsync( this, address, length );

		/// <inheritdoc cref="ReadRandomInt16(string[])"/>
		public async Task<OperateResult<short[]>> ReadRandomInt16Async( string[] address ) => await McHelper.ReadRandomInt16Async( this, address );

		/// <inheritdoc cref="ReadRandomUInt16(string[])"/>
		public async Task<OperateResult<ushort[]>> ReadRandomUInt16Async( string[] address ) => await McHelper.ReadRandomUInt16Async( this, address );
#endif
		#endregion

		#region Bool Operate Support

		/// <inheritdoc/>
		[HslMqttApi( "ReadBoolArray", "" )]
		public override OperateResult<bool[]> ReadBool( string address, ushort length ) => McHelper.ReadBool( this, address, length );

		/// <inheritdoc/>
		[HslMqttApi( "WriteBoolArray", "" )]
		public override OperateResult Write( string address, bool[] values ) => McHelper.Write( this, address, values );

#if !NET35 && !NET20
		/// <inheritdoc cref="ReadBool(string, ushort)"/>
		public override async Task<OperateResult<bool[]>> ReadBoolAsync( string address, ushort length ) => await McHelper.ReadBoolAsync( this, address, length );

		/// <inheritdoc cref="Write(string, bool[])"/>
		public override async Task<OperateResult> WriteAsync( string address, bool[] values ) => await McHelper.WriteAsync( this, address, values );
#endif
		#endregion

		#region Tag Read Write

		/// <inheritdoc cref="McBinaryHelper.ReadTags(IReadWriteMc, string[], ushort[])"/>
		/// <param name="tag">数据标签</param>
		/// <param name="length">读取的数据长度</param>
		[HslMqttApi( ApiTopic = "ReadTag", Description = "读取PLC的标签信息，需要传入标签的名称，读取的字长度，标签举例：A; label[1]; bbb[10,10,10]" )]
		public OperateResult<byte[]> ReadTags( string tag, ushort length ) => ReadTags( new string[] { tag }, new ushort[] { length } );

		/// <inheritdoc cref="ReadTags(string, ushort)"/>
		[HslMqttApi( ApiTopic = "ReadTags", Description = "批量读取PLC的标签信息，需要传入标签的名称，读取的字长度，标签举例：A; label[1]; bbb[10,10,10]" )]
		public OperateResult<byte[]> ReadTags( string[] tags, ushort[] length ) => McBinaryHelper.ReadTags( this, tags, length );

#if !NET35 && !NET20
		/// <inheritdoc cref="ReadTags(string, ushort)"/>
		public async Task<OperateResult<byte[]>> ReadTagsAsync( string tag, ushort length ) => await ReadTagsAsync( new string[] { tag }, new ushort[] { length } );

		/// <inheritdoc cref="ReadTags(string, ushort)"/>
		public async Task<OperateResult<byte[]>> ReadTagsAsync( string[] tags, ushort[] length ) => await McBinaryHelper.ReadTagsAsync( this, tags, length );
#endif
		#endregion

		#region Extend Read Write

		/// <inheritdoc cref="McHelper.ReadExtend(IReadWriteMc, ushort, string, ushort)"/>
		[HslMqttApi( ApiTopic = "ReadExtend", Description = "读取扩展的数据信息，需要在原有的地址，长度信息之外，输入扩展值信息" )]
		public OperateResult<byte[]> ReadExtend( ushort extend, string address, ushort length ) => McHelper.ReadExtend( this, extend, address, length );

#if !NET35 && !NET20
		/// <inheritdoc cref="ReadExtend(ushort, string, ushort)"/>
		public async Task<OperateResult<byte[]>> ReadExtendAsync( ushort extend, string address, ushort length ) => await McHelper.ReadExtendAsync( this, extend, address, length );
#endif
		#endregion

		#region Memory Read Write

		/// <inheritdoc cref="McHelper.ReadMemory(IReadWriteMc, string, ushort)"/>
		[HslMqttApi( ApiTopic = "ReadMemory", Description = "读取缓冲寄存器的数据信息，地址直接为偏移地址" )]
		public OperateResult<byte[]> ReadMemory( string address, ushort length ) => McHelper.ReadMemory( this, address, length );

#if !NET35 && !NET20
		/// <inheritdoc cref="ReadMemory(string, ushort)"/>
		public async Task<OperateResult<byte[]>> ReadMemoryAsync( string address, ushort length ) => await McHelper.ReadMemoryAsync( this, address, length );
#endif
		#endregion

		#region Smart Module ReadWrite

		/// <inheritdoc cref="McHelper.ReadSmartModule(IReadWriteMc, ushort, string, ushort)"/>
		[HslMqttApi( ApiTopic = "ReadSmartModule", Description = "读取智能模块的数据信息，需要指定模块地址，偏移地址，读取的字节长度" )]
		public OperateResult<byte[]> ReadSmartModule( ushort module, string address, ushort length ) => McHelper.ReadSmartModule( this, module, address, length );
#if !NET20 && !NET35
		/// <inheritdoc cref="ReadSmartModule(ushort, string, ushort)"/>
		public async Task<OperateResult<byte[]>> ReadSmartModuleAsync( ushort module, string address, ushort length ) => await McHelper.ReadSmartModuleAsync( this, module, address, length );
#endif
		#endregion

		#region Remote Operate

		/// <inheritdoc cref="McHelper.RemoteRun(IReadWriteMc)"/>
		[HslMqttApi( ApiTopic = "RemoteRun", Description = "远程Run操作" )]
		public OperateResult RemoteRun( ) => McHelper.RemoteRun( this );

		/// <inheritdoc cref="McHelper.RemoteStop(IReadWriteMc)"/>
		[HslMqttApi( ApiTopic = "RemoteStop", Description = "远程Stop操作" )]
		public OperateResult RemoteStop( ) => McHelper.RemoteStop( this );

		/// <inheritdoc cref="McHelper.RemoteReset(IReadWriteMc)"/>
		[HslMqttApi( ApiTopic = "RemoteReset", Description = "LED 熄灭 出错代码初始化" )]
		public OperateResult RemoteReset( ) => McHelper.RemoteReset( this );

		/// <inheritdoc cref="McHelper.ReadPlcType(IReadWriteMc)"/>
		[HslMqttApi( ApiTopic = "ReadPlcType", Description = "读取PLC的型号信息，例如 Q02HCPU" )]
		public OperateResult<string> ReadPlcType( ) => McHelper.ReadPlcType( this );

		/// <inheritdoc cref="McHelper.ErrorStateReset(IReadWriteMc)"/>
		[HslMqttApi( ApiTopic = "ErrorStateReset", Description = "LED 熄灭 出错代码初始化" )]
		public OperateResult ErrorStateReset( ) => McHelper.ErrorStateReset( this );

#if !NET35 && !NET20
		/// <inheritdoc cref="RemoteRun"/>
		public async Task<OperateResult> RemoteRunAsync( ) => await McHelper.RemoteRunAsync( this );

		/// <inheritdoc cref="RemoteStop"/>
		public async Task<OperateResult> RemoteStopAsync( ) => await McHelper.RemoteStopAsync( this );

		/// <inheritdoc cref="RemoteReset"/>
		public async Task<OperateResult> RemoteResetAsync( ) => await McHelper.RemoteResetAsync( this );

		/// <inheritdoc cref="ReadPlcType"/>
		public async Task<OperateResult<string>> ReadPlcTypeAsync( ) => await McHelper.ReadPlcTypeAsync( this );

		/// <inheritdoc cref="ErrorStateReset"/>
		public async Task<OperateResult> ErrorStateResetAsync( ) => await McHelper.ErrorStateResetAsync( this );
#endif
		#endregion

		#region Object Override

		/// <inheritdoc/>
		public override string ToString() => $"MelsecMcNet[{IpAddress}:{Port}]";

		#endregion
	}
}
