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

namespace HslCommunication.Profinet.Melsec
{
	/// <summary>
	/// 三菱PLC通讯类，采用UDP的协议实现，采用Qna兼容3E帧协议实现，需要在PLC侧先的以太网模块先进行配置，必须为二进制通讯<br />
	/// Mitsubishi PLC communication class is implemented using UDP protocol and Qna compatible 3E frame protocol. 
	/// The Ethernet module needs to be configured first on the PLC side, and it must be binary communication.
	/// </summary>
	/// <remarks>
	/// <inheritdoc cref="MelsecMcNet" path="remarks"/>
	/// </remarks>
	/// <example>
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Profinet\melsecTest.cs" region="Usage" title="简单的短连接使用" />
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Profinet\melsecTest.cs" region="Usage2" title="简单的长连接使用" />
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Profinet\melsecTest.cs" region="ReadExample1" title="基本的读取示例" />
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Profinet\melsecTest.cs" region="ReadExample2" title="批量读取示例" />
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Profinet\melsecTest.cs" region="ReadExample3" title="随机字读取示例" />
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Profinet\melsecTest.cs" region="ReadExample4" title="随机批量字读取示例" />
	/// </example>
	public class MelsecMcUdp : NetworkUdpDeviceBase, IReadWriteMc
	{
		#region Constructor

		/// <inheritdoc cref="MelsecMcNet()"/>
		public MelsecMcUdp( )
		{
			this.WordLength    = 1;
			this.ByteTransform = new RegularByteTransform( );
		}

		/// <inheritdoc cref="MelsecMcNet(string,int)"/>
		public MelsecMcUdp( string ipAddress, int port ) : this( )
		{
			this.IpAddress     = ipAddress;
			this.Port          = port;
		}

		/// <inheritdoc cref="IReadWriteMc.McType"/>
		public McType McType => McType.McBinary;

		#endregion

		#region Public Member

		/// <inheritdoc cref="MelsecMcNet.NetworkNumber"/>
		public byte NetworkNumber { get; set; } = 0x00;

		/// <inheritdoc cref="MelsecMcNet.NetworkStationNumber"/>
		public byte NetworkStationNumber { get; set; } = 0x00;

		#endregion

		#region Virtual Address Analysis

		/// <inheritdoc cref="MelsecMcNet.McAnalysisAddress(string, ushort)"/>
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

		#endregion

		#region Bool Operate Support

		/// <inheritdoc/>
		[HslMqttApi( "ReadBoolArray", "" )]
		public override OperateResult<bool[]> ReadBool( string address, ushort length ) => McHelper.ReadBool( this, address, length );

		/// <inheritdoc/>
		[HslMqttApi( "WriteBoolArray", "" )]
		public override OperateResult Write( string address, bool[] values ) => McHelper.Write( this, address, values );

		#endregion

		#region Tag Read Write

		/// <inheritdoc cref="McBinaryHelper.ReadTags(IReadWriteMc, string[], ushort[])"/>
		[HslMqttApi( ApiTopic = "ReadTag", Description = "读取PLC的标签信息，需要传入标签的名称，读取的字长度，标签举例：A; label[1]; bbb[10,10,10]" )]
		public OperateResult<byte[]> ReadTags( string tag, ushort length ) => ReadTags( new string[] { tag }, new ushort[] { length } );

		/// <inheritdoc cref="ReadTags(string, ushort)"/>
		[HslMqttApi( ApiTopic = "ReadTags", Description = "批量读取PLC的标签信息，需要传入标签的名称，读取的字长度，标签举例：A; label[1]; bbb[10,10,10]" )]
		public OperateResult<byte[]> ReadTags( string[] tags, ushort[] length ) => McBinaryHelper.ReadTags( this, tags, length );

		#endregion

		#region Extend Read Write

		/// <inheritdoc cref="McHelper.ReadExtend(IReadWriteMc, ushort, string, ushort)"/>
		[HslMqttApi( ApiTopic = "ReadExtend", Description = "读取扩展的数据信息，需要在原有的地址，长度信息之外，输入扩展值信息" )]
		public OperateResult<byte[]> ReadExtend( ushort extend, string address, ushort length ) => McHelper.ReadExtend( this, extend, address, length );

		#endregion

		#region Memory Read Write

		/// <inheritdoc cref="McHelper.ReadMemory(IReadWriteMc, string, ushort)"/>
		[HslMqttApi( ApiTopic = "ReadMemory", Description = "读取缓冲寄存器的数据信息，地址直接为偏移地址" )]
		public OperateResult<byte[]> ReadMemory( string address, ushort length ) => McHelper.ReadMemory( this, address, length );

		#endregion

		#region Smart Module ReadWrite

		/// <inheritdoc cref="McHelper.ReadSmartModule(IReadWriteMc, ushort, string, ushort)"/>
		[HslMqttApi( ApiTopic = "ReadSmartModule", Description = "读取智能模块的数据信息，需要指定模块地址，偏移地址，读取的字节长度" )]
		public OperateResult<byte[]> ReadSmartModule( ushort module, string address, ushort length ) => McHelper.ReadSmartModule( this, module, address, length );
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

		#endregion

		#region Object Override

		/// <inheritdoc/>
		public override string ToString() => $"MelsecMcUdp[{IpAddress}:{Port}]";

		#endregion
	}
}
