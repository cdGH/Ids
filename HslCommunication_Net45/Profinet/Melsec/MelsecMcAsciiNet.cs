using HslCommunication.BasicFramework;
using HslCommunication.Core;
using HslCommunication.Core.Address;
using HslCommunication.Core.IMessage;
using HslCommunication.Core.Net;
using HslCommunication.Reflection;
using HslCommunication.Profinet.Melsec.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
#if !NET35 && !NET20
using System.Threading.Tasks;
#endif

namespace HslCommunication.Profinet.Melsec
{
	/// <summary>
	/// 三菱PLC通讯类，采用Qna兼容3E帧协议实现，需要在PLC侧先的以太网模块先进行配置，必须为ASCII通讯格式<br />
	/// Mitsubishi PLC communication class is implemented using Qna compatible 3E frame protocol. 
	/// The Ethernet module on the PLC side needs to be configured first. It must be ascii communication.
	/// </summary>
	/// <remarks>
	/// <inheritdoc cref="MelsecMcNet" path="remarks"/>
	/// </remarks>
	/// <example>
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Profinet\MelsecAscii.cs" region="Usage" title="简单的短连接使用" />
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Profinet\MelsecAscii.cs" region="Usage2" title="简单的长连接使用" />
	/// </example>
	public class MelsecMcAsciiNet : NetworkDeviceBase, IReadWriteMc
	{
		#region Constructor

		/// <inheritdoc cref="MelsecMcNet()"/>
		public MelsecMcAsciiNet( )
		{
			WordLength         = 1;
			LogMsgFormatBinary = false;
			this.ByteTransform = new RegularByteTransform( );
		}

		/// <inheritdoc cref="MelsecMcNet(string,int)"/>
		public MelsecMcAsciiNet( string ipAddress, int port ) : this( )
		{
			IpAddress          = ipAddress;
			Port               = port;
		}

		/// <inheritdoc/>
		protected override INetMessage GetNewNetMessage( ) => new MelsecQnA3EAsciiMessage( );

		/// <inheritdoc cref="IReadWriteMc.McType"/>
		public McType McType => McType.MCAscii;

		#endregion

		#region Public Member

		/// <inheritdoc cref="MelsecMcNet.NetworkNumber"/>
		public byte NetworkNumber { get; set; } = 0x00;

		/// <inheritdoc cref="MelsecMcNet.NetworkStationNumber"/>
		public byte NetworkStationNumber { get; set; } = 0x00;

		#endregion

		#region Address Analysis

		/// <inheritdoc cref="MelsecMcNet.McAnalysisAddress(string, ushort)"/>
		public virtual OperateResult<McAddressData> McAnalysisAddress( string address, ushort length ) => McAddressData.ParseMelsecFrom( address, length );

		/// <inheritdoc/>
		protected override byte[] PackCommandWithHeader( byte[] command )
		{
			return McAsciiHelper.PackMcCommand( command, this.NetworkNumber, this.NetworkStationNumber );
		}

		/// <inheritdoc/>
		protected override OperateResult<byte[]> UnpackResponseContent( byte[] send, byte[] response )
		{
			OperateResult check = McAsciiHelper.CheckResponseContent( response );
			if (!check.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( check );

			return OperateResult.CreateSuccessResult( response.RemoveBegin( 22 ) );
		}

		/// <inheritdoc cref="IReadWriteMc.ExtractActualData(byte[], bool)"/>
		public byte[] ExtractActualData( byte[] response, bool isBit ) => McAsciiHelper.ExtractActualDataHelper( response, isBit );

		#endregion

		#region Read Write Override

		/// <inheritdoc/>
		[HslMqttApi( "ReadByteArray", "" )]
		public override OperateResult<byte[]> Read( string address, ushort length ) => McHelper.Read( this, address, length );

		/// <inheritdoc/>
		[HslMqttApi( "WriteByteArray", "" )]
		public override OperateResult Write( string address, byte[] value ) => McHelper.Write( this, address, value );

		#endregion

		#region Async Read Write Override
#if !NET35 && !NET20
		/// <inheritdoc/>
		public override async Task<OperateResult<byte[]>> ReadAsync( string address, ushort length ) => await McHelper.ReadAsync( this, address, length );

		/// <inheritdoc/>
		public override async Task<OperateResult> WriteAsync( string address, byte[] value ) => await McHelper.WriteAsync( this, address, value );
#endif
		#endregion

		#region Read Random

		/// <inheritdoc cref="MelsecMcNet.ReadRandom(string[])"/>
		[HslMqttApi( "随机读取PLC的数据信息，可以跨地址，跨类型组合，但是每个地址只能读取一个word，也就是2个字节的内容。收到结果后，需要自行解析数据" )]
		public OperateResult<byte[]> ReadRandom( string[] address ) => McHelper.ReadRandom( this, address );

		/// <inheritdoc cref="MelsecMcNet.ReadRandom(string[], ushort[])"/>
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

		#endregion

		#region Async Bool Operate Support
#if !NET35 && !NET20
		/// <inheritdoc/>
		public override async Task<OperateResult<bool[]>> ReadBoolAsync( string address, ushort length ) => await McHelper.ReadBoolAsync( this, address, length );

		/// <inheritdoc/>
		public override async Task<OperateResult> WriteAsync( string address, bool[] values ) => await McHelper.WriteAsync( this, address, values );
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
		public override string ToString( ) => $"MelsecMcAsciiNet[{IpAddress}:{Port}]";

		#endregion

	}
}
