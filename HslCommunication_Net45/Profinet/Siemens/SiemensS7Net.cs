using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using HslCommunication.Core;
using HslCommunication.Core.IMessage;
using HslCommunication.Core.Net;
using HslCommunication.Core.Address;
using HslCommunication.BasicFramework;
using HslCommunication.Reflection;
#if !NET35 && !NET20
using System.Threading.Tasks;
#endif

/********************************************************************************
 * 
 *    说明：西门子通讯类，使用S7消息解析规格，和反字节转换规格来实现的
 *    
 *    继承自统一的自定义方法
 *    其中的启动，停止的功能代码参考了开源项目：https://github.com/fbarresi/Sharp7
 * 
 *********************************************************************************/

namespace HslCommunication.Profinet.Siemens
{
	/// <summary>
	/// 一个西门子的客户端类，使用S7协议来进行数据交互，对于s300,s400需要关注<see cref="Slot"/>和<see cref="Rack"/>的设置值，
	/// 对于s200，需要关注<see cref="LocalTSAP"/>和<see cref="DestTSAP"/>的设置值，详细参考demo的设置。 <br />
	/// A Siemens client class uses the S7 protocol for data exchange. For s300 and s400, 
	/// you need to pay attention to the setting values of <see cref="Slot"/> and <see cref="Rack"/>. For s200, 
	/// you need to pay attention to <see cref="Slot"/> and <see cref="Rack"/>. See cref="LocalTSAP"/> and <see cref="DestTSAP"/> settings, 
	/// please refer to the demo settings for details.
	/// </summary>
	/// <remarks>
	/// 暂时不支持bool[]的批量写入操作，请使用 Write(string, byte[]) 替换。<br />
	/// <note type="important">对于200smartPLC的V区，就是DB1.X，例如，V100=DB1.100，当然了你也可以输入V100</note><br />
	/// 如果读取PLC的字符串string数据，可以使用 <see cref="ReadString(string)"/>
	/// </remarks>
	/// <example>
	/// 地址支持的列表如下：
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
	///     <term>中间寄存器</term>
	///     <term>M</term>
	///     <term>M100,M200</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>输入寄存器</term>
	///     <term>I</term>
	///     <term>I100,I200</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>输出寄存器</term>
	///     <term>Q</term>
	///     <term>Q100,Q200</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>DB块寄存器</term>
	///     <term>DB</term>
	///     <term>DB1.100,DB1.200</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>V寄存器</term>
	///     <term>V</term>
	///     <term>V100,V200</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term>V寄存器本质就是DB块1</term>
	///   </item>
	///   <item>
	///     <term>定时器的值</term>
	///     <term>T</term>
	///     <term>T100,T200</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term>仅在200smart测试通过</term>
	///   </item>
	///   <item>
	///     <term>计数器的值</term>
	///     <term>C</term>
	///     <term>C100,C200</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term>仅在200smart测试通过</term>
	///   </item>
	///   <item>
	///     <term>智能输入寄存器</term>
	///     <term>AI</term>
	///     <term>AI100,AI200</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>智能输出寄存器</term>
	///     <term>AQ</term>
	///     <term>AQ100,AQ200</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	/// </list>
	/// <note type="important">对于200smartPLC的V区，就是DB1.X，例如，V100=DB1.100</note>
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Profinet\SiemensS7Net.cs" region="Usage" title="简单的短连接使用" />
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Profinet\SiemensS7Net.cs" region="Usage2" title="简单的长连接使用" />
	/// 
	/// 假设起始地址为M100，M100存储了温度，100.6℃值为1006，M102存储了压力，1.23Mpa值为123，M104，M105，M106，M107存储了产量计数，读取如下：
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Profinet\SiemensS7Net.cs" region="ReadExample2" title="Read示例" />
	/// 以下是读取不同类型数据的示例
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Profinet\SiemensS7Net.cs" region="ReadExample1" title="Read示例" />
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Profinet\SiemensS7Net.cs" region="WriteExample1" title="Write示例" />
	/// 以下是一个复杂的读取示例
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Profinet\SiemensS7Net.cs" region="ReadExample3" title="Read示例" />
	/// 在西门子PLC，字符串分为普通的string，和WString类型，前者为单字节的类型，后者为双字节的字符串类型<br />
	/// 一个字符串除了本身的数据信息，还有字符串的长度信息，比如字符串 "12345"，比如在PLC的地址 DB1.0 存储的字节是 FE 05 31 32 33 34 35, 第一个字节是最大长度，第二个字节是当前长度，后面的才是字符串的数据信息。<br />
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Profinet\SiemensS7Net.cs" region="ReadWriteString" title="字符串读写示例" />
	/// </example>
	public class SiemensS7Net : NetworkDeviceBase
	{
		#region Constructor

		/// <summary>
		/// 实例化一个西门子的S7协议的通讯对象 <br />
		/// Instantiate a communication object for a Siemens S7 protocol
		/// </summary>
		/// <param name="siemens">指定西门子的型号</param>
		public SiemensS7Net( SiemensPLCS siemens )
		{
			Initialization( siemens, string.Empty );
		}

		/// <summary>
		/// 实例化一个西门子的S7协议的通讯对象并指定Ip地址 <br />
		/// Instantiate a communication object for a Siemens S7 protocol and specify an IP address
		/// </summary>
		/// <param name="siemens">指定西门子的型号</param>
		/// <param name="ipAddress">Ip地址</param>
		public SiemensS7Net( SiemensPLCS siemens, string ipAddress )
		{
			Initialization( siemens, ipAddress );
		}

		/// <inheritdoc/>
		protected override INetMessage GetNewNetMessage( ) => new S7Message( );

		/// <summary>
		/// 初始化方法<br />
		/// Initialize method
		/// </summary>
		/// <param name="siemens">指定西门子的型号 -> Designation of Siemens</param>
		/// <param name="ipAddress">Ip地址 -> IpAddress</param>
		private void Initialization( SiemensPLCS siemens, string ipAddress )
		{
			WordLength    = 2;
			IpAddress     = ipAddress;
			Port          = 102;
			CurrentPlc    = siemens;
			ByteTransform = new ReverseBytesTransform( );

			switch (siemens)
			{
				case SiemensPLCS.S1200:    plcHead1[21] = 0; break;
				case SiemensPLCS.S300:     plcHead1[21] = 2; break;
				case SiemensPLCS.S400:     plcHead1[21] = 3; plcHead1[17] = 0x00; break;
				case SiemensPLCS.S1500:    plcHead1[21] = 0; break;
				case SiemensPLCS.S200Smart:
					{
						plcHead1 = plcHead1_200smart;
						plcHead2 = plcHead2_200smart;
						break;
					}
				case SiemensPLCS.S200:
					{
						plcHead1 = plcHead1_200;
						plcHead2 = plcHead2_200;
						break;
					}
				default: plcHead1[18] = 0; break;
			}
		}

		/// <summary>
		/// PLC的槽号，针对S7-400的PLC设置的<br />
		/// The slot number of PLC is set for PLC of s7-400
		/// </summary>
		public byte Slot
		{
			get => plc_slot;
			set
			{
				plc_slot = value;
				if (CurrentPlc != SiemensPLCS.S200 && CurrentPlc != SiemensPLCS.S200Smart)
					plcHead1[21] = (byte)((this.plc_rack * 0x20) + this.plc_slot);
			}
		}

		/// <summary>
		/// PLC的机架号，针对S7-400的PLC设置的<br />
		/// The frame number of the PLC is set for the PLC of s7-400
		/// </summary>
		public byte Rack
		{
			get => plc_rack;
			set
			{
				this.plc_rack = value;
				if (CurrentPlc != SiemensPLCS.S200 && CurrentPlc != SiemensPLCS.S200Smart)
					plcHead1[21] = (byte)((this.plc_rack * 0x20) + this.plc_slot);
			}
		}

		/// <summary>
		/// 获取或设置当前PLC的连接方式，PG: 0x01，OP: 0x02，S7Basic: 0x03...0x10<br />
		/// Get or set the current PLC connection mode, PG: 0x01, OP: 0x02, S7Basic: 0x03...0x10
		/// </summary>
		public byte ConnectionType
		{
			get => this.plcHead1[20];
			set 
			{
				if(CurrentPlc == SiemensPLCS.S200 ||
					CurrentPlc == SiemensPLCS.S200Smart)
				{

				}
				else
				{
					this.plcHead1[20] = value;
				}
			}
		}

		/// <summary>
		/// 西门子相关的本地TSAP参数信息<br />
		/// A parameter information related to Siemens
		/// </summary>
		public int LocalTSAP
		{
			get
			{
				if(CurrentPlc == SiemensPLCS.S200 || CurrentPlc == SiemensPLCS.S200Smart)
					return this.plcHead1[13] * 256 + this.plcHead1[14];
				else
					return this.plcHead1[16] * 256 + this.plcHead1[17];
			}
			set
			{
				if (CurrentPlc == SiemensPLCS.S200 || CurrentPlc == SiemensPLCS.S200Smart)
				{
					this.plcHead1[13] = BitConverter.GetBytes( value )[1];
					this.plcHead1[14] = BitConverter.GetBytes( value )[0];
				}
				else
				{
					this.plcHead1[16] = BitConverter.GetBytes( value )[1];
					this.plcHead1[17] = BitConverter.GetBytes( value )[0];
				}
			}
		}

		/// <summary>
		/// 西门子相关的远程TSAP参数信息<br />
		/// A parameter information related to Siemens
		/// </summary>
		public int DestTSAP
		{
			get
			{
				if (CurrentPlc == SiemensPLCS.S200 || CurrentPlc == SiemensPLCS.S200Smart)
					return this.plcHead1[17] * 256 + this.plcHead1[18];
				else
					return this.plcHead1[20] * 256 + this.plcHead1[21];
			}
			set
			{
				if (CurrentPlc == SiemensPLCS.S200 || CurrentPlc == SiemensPLCS.S200Smart)
				{
					this.plcHead1[17] = BitConverter.GetBytes( value )[1];
					this.plcHead1[18] = BitConverter.GetBytes( value )[0];
				}
				else
				{
					this.plcHead1[20] = BitConverter.GetBytes( value )[1];
					this.plcHead1[21] = BitConverter.GetBytes( value )[0];
				}
			}
		}

		/// <summary>
		/// 获取当前西门子的PDU的长度信息，不同型号PLC的值会不一样。<br />
		/// Get the length information of the current Siemens PDU, the value of different types of PLC will be different.
		/// </summary>
		public int PDULength => pdu_length;

		#endregion

		#region NetworkDoubleBase Override

		/// <inheritdoc/>
		public override OperateResult<byte[]> ReadFromCoreServer( Socket socket, byte[] send, bool hasResponseData = true, bool usePackHeader = true )
		{
			while (true)
			{
				OperateResult<byte[]> read = base.ReadFromCoreServer( socket, send, hasResponseData, usePackHeader );
				if (!read.IsSuccess) return read;

				if ((read.Content[2] * 256 + read.Content[3]) != 0x07) return read;
			}
		}

		/// <inheritdoc/>
		protected override OperateResult InitializationOnConnect( Socket socket )
		{
			// 第一次握手 -> First handshake
			OperateResult<byte[]> read_first = ReadFromCoreServer( socket, plcHead1 );
			if (!read_first.IsSuccess) return read_first;

			// 第二次握手 -> Second handshake
			OperateResult<byte[]> read_second = ReadFromCoreServer( socket, plcHead2 );
			if (!read_second.IsSuccess) return read_second;

			// 调整单次接收的pdu长度信息
			pdu_length = ByteTransform.TransUInt16( read_second.Content.SelectLast( 2 ), 0 ) - 28;
			if (pdu_length < 200) pdu_length = 200;

			// 返回成功的信号 -> Return a successful signal
			return OperateResult.CreateSuccessResult( );
		}

#if !NET35 && !NET20
		/// <inheritdoc/>
		public async override Task<OperateResult<byte[]>> ReadFromCoreServerAsync( Socket socket, byte[] send, bool hasResponseData = true, bool usePackHeader = true )
		{
			while (true)
			{
				OperateResult<byte[]> read = await base.ReadFromCoreServerAsync( socket, send, hasResponseData, usePackHeader );
				if (!read.IsSuccess) return read;

				if ((read.Content[2] * 256 + read.Content[3]) != 0x07) return read;
			}
		}

		/// <inheritdoc/>
		protected override async Task<OperateResult> InitializationOnConnectAsync( Socket socket )
		{
			// 第一次握手 -> First handshake
			OperateResult<byte[]> read_first = await ReadFromCoreServerAsync( socket, plcHead1 );
			if (!read_first.IsSuccess) return read_first;

			// 第二次握手 -> Second handshake
			OperateResult<byte[]> read_second = await ReadFromCoreServerAsync( socket, plcHead2 );
			if (!read_second.IsSuccess) return read_second;

			// 调整单次接收的pdu长度信息
			pdu_length = ByteTransform.TransUInt16( read_second.Content.SelectLast( 2 ), 0 ) - 28;
			if (pdu_length < 200) pdu_length = 200;

			// 返回成功的信号 -> Return a successful signal
			return OperateResult.CreateSuccessResult( );
		}
#endif
		#endregion

		#region Read OrderNumber

		/// <summary>
		/// 从PLC读取订货号信息<br />
		/// Reading order number information from PLC
		/// </summary>
		/// <returns>CPU的订货号信息 -> Order number information for the CPU</returns>
		[HslMqttApi( "ReadOrderNumber", "获取到PLC的订货号信息" )]
		public OperateResult<string> ReadOrderNumber( ) => ByteTransformHelper.GetSuccessResultFromOther( ReadFromCoreServer( plcOrderNumber ), m => Encoding.ASCII.GetString( m, 71, 20 ) );

#if !NET35 && !NET20
		/// <inheritdoc cref="ReadOrderNumber()"/>
		public async Task<OperateResult<string>> ReadOrderNumberAsync( ) => ByteTransformHelper.GetSuccessResultFromOther( await ReadFromCoreServerAsync( plcOrderNumber ), m => Encoding.ASCII.GetString( m, 71, 20 ) );
#endif
		#endregion

		#region Start Stop

		private OperateResult CheckStartResult(byte[] content )
		{
			if (content.Length < 19) return new OperateResult( "Receive error" );

			if (content[19] != pduStart) return new OperateResult( "Can not start PLC" );
			else if (content[20] != pduAlreadyStarted) return new OperateResult( "Can not start PLC" );

			return OperateResult.CreateSuccessResult( );
		}

		private OperateResult CheckStopResult( byte[] content )
		{
			if (content.Length < 19) return new OperateResult( "Receive error" );

			if (content[19] != pduStop) return new OperateResult( "Can not stop PLC" );
			else if (content[20] != pduAlreadyStopped) return new OperateResult( "Can not stop PLC" );

			return OperateResult.CreateSuccessResult( );
		}

		/// <summary>
		/// 对PLC进行热启动，目前仅适用于200smart型号<br />
		/// Hot start for PLC, currently only applicable to 200smart model
		/// </summary>
		/// <returns>是否启动成功的结果对象</returns>
		[HslMqttApi]
		public OperateResult HotStart( ) => ByteTransformHelper.GetResultFromOther( ReadFromCoreServer( S7_HOT_START ), CheckStartResult );

		/// <summary>
		/// 对PLC进行冷启动，目前仅适用于200smart型号<br />
		/// Cold start for PLC, currently only applicable to 200smart model
		/// </summary>
		/// <returns>是否启动成功的结果对象</returns>
		[HslMqttApi]
		public OperateResult ColdStart( ) => ByteTransformHelper.GetResultFromOther( ReadFromCoreServer( S7_COLD_START ), CheckStartResult );

		/// <summary>
		/// 对PLC进行停止，目前仅适用于200smart型号<br />
		/// Stop the PLC, currently only applicable to the 200smart model
		/// </summary>
		/// <returns>是否启动成功的结果对象</returns>
		[HslMqttApi]
		public OperateResult Stop( ) => ByteTransformHelper.GetResultFromOther( ReadFromCoreServer( S7_STOP ), CheckStopResult );

		#endregion

		#region Async Start Stop
#if !NET35 && !NET20
		/// <inheritdoc cref="HotStart( )"/>
		public async Task<OperateResult> HotStartAsync( ) => ByteTransformHelper.GetResultFromOther( await ReadFromCoreServerAsync( S7_HOT_START ), CheckStartResult );

		/// <inheritdoc cref="ColdStart( )"/>
		public async Task<OperateResult> ColdStartAsync( ) => ByteTransformHelper.GetResultFromOther( await ReadFromCoreServerAsync( S7_COLD_START ), CheckStartResult );

		/// <inheritdoc cref="Stop( )"/>
		public async Task<OperateResult> StopAsync( ) => ByteTransformHelper.GetResultFromOther( await ReadFromCoreServerAsync( S7_STOP ), CheckStopResult );
#endif
		#endregion

		#region Read Write Support

		/// <summary>
		/// 从PLC读取原始的字节数据，地址格式为I100，Q100，DB20.100，M100，长度参数以字节为单位<br />
		/// Read the original byte data from the PLC, the address format is I100, Q100, DB20.100, M100, length parameters in bytes
		/// </summary>
		/// <param name="address">起始地址，格式为I100，M100，Q100，DB20.100<br />
		/// Starting address, formatted as I100,M100,Q100,DB20.100</param>
		/// <param name="length">读取的数量，以字节为单位<br />
		/// The number of reads, in bytes</param>
		/// <returns>
		/// 是否读取成功的结果对象 <br />
		/// Whether to read the successful result object</returns>
		/// <remarks>
		/// <inheritdoc cref="SiemensS7Net" path="note"/>
		/// </remarks>
		/// <example>
		/// 假设起始地址为M100，M100存储了温度，100.6℃值为1006，M102存储了压力，1.23Mpa值为123，M104，M105，M106，M107存储了产量计数，读取如下：
		/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Profinet\SiemensS7Net.cs" region="ReadExample2" title="Read示例" />
		/// 以下是读取不同类型数据的示例
		/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Profinet\SiemensS7Net.cs" region="ReadExample1" title="Read示例" />
		/// </example>
		[HslMqttApi( "ReadByteArray", "" )]
		public override OperateResult<byte[]> Read( string address, ushort length )
		{
			OperateResult<S7AddressData> addressResult = S7AddressData.ParseFrom( address, length );
			if (!addressResult.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( addressResult );

			// 如果长度超过 pdu_length，分批次读取 -> If the length is more than pdu_length, read in batches
			List<byte> bytesContent = new List<byte>( );
			ushort alreadyFinished = 0;
			while (alreadyFinished < length)
			{
				ushort readLength = (ushort)Math.Min( length - alreadyFinished, pdu_length );
				addressResult.Content.Length = readLength;
				OperateResult<byte[]> read = Read( new S7AddressData[] { addressResult.Content } );
				if (!read.IsSuccess) return read;

				bytesContent.AddRange( read.Content );
				alreadyFinished += readLength;
				if (addressResult.Content.DataCode == 0x1F || addressResult.Content.DataCode == 0x1E)
					addressResult.Content.AddressStart += readLength / 2;
				else
					addressResult.Content.AddressStart += readLength * 8;
			}

			return OperateResult.CreateSuccessResult( bytesContent.ToArray( ) );
		}

		/// <summary>
		/// 从PLC读取数据，地址格式为I100，Q100，DB20.100，M100，以位为单位 ->
		/// Read the data from the PLC, the address format is I100，Q100，DB20.100，M100, in bits units
		/// </summary>
		/// <param name="address">起始地址，格式为I100，M100，Q100，DB20.100 ->
		/// Starting address, formatted as I100,M100,Q100,DB20.100</param>
		/// <returns>是否读取成功的结果对象 -> Whether to read the successful result object</returns>
		private OperateResult<byte[]> ReadBitFromPLC( string address )
		{
			// 指令生成 -> Build bit read command
			OperateResult<byte[]> command = BuildBitReadCommand( address );
			if (!command.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( command );

			// 核心交互 ->  Core interactive
			OperateResult<byte[]> read = ReadFromCoreServer( command.Content );
			if (!read.IsSuccess) return read;

			// 分析结果 -> Analysis read result
			return AnalysisReadBit( read.Content );
		}

		/// <summary>
		/// 一次性从PLC获取所有的数据，按照先后顺序返回一个统一的Buffer，需要按照顺序处理，两个数组长度必须一致，数组长度无限制<br />
		/// One-time from the PLC to obtain all the data, in order to return a unified buffer, need to be processed sequentially, two array length must be consistent
		/// </summary>
		/// <param name="address">起始地址，格式为I100，M100，Q100，DB20.100<br />
		/// Starting address, formatted as I100,M100,Q100,DB20.100</param>
		/// <param name="length">数据长度数组<br />
		/// Array of data Lengths</param>
		/// <returns>是否读取成功的结果对象 -> Whether to read the successful result object</returns>
		/// <exception cref="NullReferenceException"></exception>
		/// <remarks>
		/// <note type="warning">原先的批量的长度为19，现在已经内部自动处理整合，目前的长度为任意和长度。</note>
		/// </remarks>
		/// <example>
		/// 以下是一个高级的读取示例
		/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Profinet\SiemensS7Net.cs" region="ReadExample3" title="Read示例" />
		/// </example>
		[HslMqttApi( "ReadAddressArray", "一次性从PLC获取所有的数据，按照先后顺序返回一个统一的Buffer，需要按照顺序处理，两个数组长度必须一致，数组长度无限制" )]
		public OperateResult<byte[]> Read( string[] address, ushort[] length )
		{
			S7AddressData[] addressResult = new S7AddressData[address.Length];
			for (int i = 0; i < address.Length; i++)
			{
				OperateResult<S7AddressData> tmp = S7AddressData.ParseFrom( address[i], length[i] ) ;
				if (!tmp.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( tmp );

				addressResult[i] = tmp.Content;
			}

			return Read( addressResult );
		}

		/// <summary>
		/// 读取西门子的地址数据信息，支持任意个数的数据读取<br />
		/// Read Siemens address data information, support any number of data reading
		/// </summary>
		/// <param name="s7Addresses">
		/// 西门子的数据地址<br />
		/// Siemens data address</param>
		/// <returns>返回的结果对象信息 -> Whether to read the successful result object</returns>
		public OperateResult<byte[]> Read( S7AddressData[] s7Addresses )
		{
			if (s7Addresses.Length > 19)
			{
				List<byte> bytes = new List<byte>( );
				List<S7AddressData[]> groups = SoftBasic.ArraySplitByLength<S7AddressData>( s7Addresses, 19 );
				for (int i = 0; i < groups.Count; i++)
				{
					OperateResult<byte[]> read = Read( groups[i] );
					if (!read.IsSuccess) return read;

					bytes.AddRange( read.Content );
				}
				return OperateResult.CreateSuccessResult( bytes.ToArray( ) );
			}
			else
			{
				return ReadS7AddressData( s7Addresses );
			}
		}

		/// <summary>
		/// 单次的读取，只能读取最多19个数组的长度，所以不再对外公开该方法
		/// </summary>
		/// <param name="s7Addresses">西门子的地址对象</param>
		/// <returns>返回的结果对象信息</returns>
		private OperateResult<byte[]> ReadS7AddressData( S7AddressData[] s7Addresses )
		{
			// 构建指令 -> Build read command
			OperateResult<byte[]> command = BuildReadCommand( s7Addresses );
			if (!command.IsSuccess) return command;

			// 核心交互 -> Core Interactions
			OperateResult<byte[]> read = ReadFromCoreServer( command.Content );
			if (!read.IsSuccess) return read;

			// 分析结果 -> Analysis results
			return AnalysisReadByte( s7Addresses, read.Content );
		}

		/// <summary>
		/// 基础的写入数据的操作支持<br />
		/// Operational support for the underlying write data
		/// </summary>
		/// <param name="entireValue">完整的字节数据 -> Full byte data</param>
		/// <returns>是否写入成功的结果对象 -> Whether to write a successful result object</returns>
		private OperateResult WriteBase( byte[] entireValue ) => ByteTransformHelper.GetResultFromOther( ReadFromCoreServer( entireValue ), AnalysisWrite );

		/// <summary>
		/// 将数据写入到PLC数据，地址格式为I100，Q100，DB20.100，M100，以字节为单位<br />
		/// Writes data to the PLC data, in the address format I100,Q100,DB20.100,M100, in bytes
		/// </summary>
		/// <param name="address">起始地址，格式为I100，M100，Q100，DB20.100 ->
		/// Starting address, formatted as I100,M100,Q100,DB20.100</param>
		/// <param name="value">写入的原始数据 -> Raw data written to</param>
		/// <returns>是否写入成功的结果对象 -> Whether to write a successful result object</returns>
		/// <example>
		/// 假设起始地址为M100，M100,M101存储了温度，100.6℃值为1006，M102,M103存储了压力，1.23Mpa值为123，M104-M107存储了产量计数，写入如下：
		/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Profinet\SiemensS7Net.cs" region="WriteExample2" title="Write示例" />
		/// 以下是写入不同类型数据的示例
		/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Profinet\SiemensS7Net.cs" region="WriteExample1" title="Write示例" />
		/// </example>
		[HslMqttApi( "WriteByteArray", "" )]
		public override OperateResult Write( string address, byte[] value )
		{
			OperateResult<S7AddressData> analysis = S7AddressData.ParseFrom( address );
			if (!analysis.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( analysis );
			
			int length = value.Length;
			ushort alreadyFinished = 0;
			while (alreadyFinished < length)
			{
				ushort writeLength = (ushort)Math.Min( length - alreadyFinished, pdu_length );
				byte[] buffer = ByteTransform.TransByte( value, alreadyFinished, writeLength );

				OperateResult<byte[]> command = BuildWriteByteCommand( analysis, buffer );
				if (!command.IsSuccess) return command;

				OperateResult write = WriteBase( command.Content );
				if (!write.IsSuccess) return write;
				
				alreadyFinished += writeLength;
				analysis.Content.AddressStart += writeLength * 8;
			}

			return OperateResult.CreateSuccessResult( );
		}

		#endregion

		#region Async Read Write Support
#if !NET35 && !NET20
		/// <inheritdoc cref="Read(string, ushort)"/>
		public override async Task<OperateResult<byte[]>> ReadAsync( string address, ushort length )
		{
			OperateResult<S7AddressData> addressResult = S7AddressData.ParseFrom( address, length );
			if (!addressResult.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( addressResult );

			// 如果长度超过 pdu_length，分批次读取 -> If the length is more than pdu_length, read in batches
			List<byte> bytesContent = new List<byte>( );
			ushort alreadyFinished = 0;
			while (alreadyFinished < length)
			{
				ushort readLength = (ushort)Math.Min( length - alreadyFinished, 200 );
				addressResult.Content.Length = readLength;
				OperateResult<byte[]> read = await ReadAsync( new S7AddressData[] { addressResult.Content } );
				if (!read.IsSuccess) return read;

				bytesContent.AddRange( read.Content );
				alreadyFinished += readLength;
				if (addressResult.Content.DataCode == 0x1F || addressResult.Content.DataCode == 0x1E)
					addressResult.Content.AddressStart += readLength / 2;
				else
					addressResult.Content.AddressStart += readLength * 8;
			}

			return OperateResult.CreateSuccessResult( bytesContent.ToArray( ) );
		}

		/// <inheritdoc cref="ReadBitFromPLC(string)"/>
		private async Task<OperateResult<byte[]>> ReadBitFromPLCAsync( string address )
		{
			// 指令生成 -> Build bit read command
			OperateResult<byte[]> command = BuildBitReadCommand( address );
			if (!command.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( command );

			// 核心交互 ->  Core interactive
			OperateResult<byte[]> read = await ReadFromCoreServerAsync( command.Content );
			if (!read.IsSuccess) return read;

			// 分析结果 -> Analysis read result
			return AnalysisReadBit( read.Content );
		}

		/// <inheritdoc cref="Read(string[], ushort[])"/>
		public async Task<OperateResult<byte[]>> ReadAsync( string[] address, ushort[] length )
		{
			S7AddressData[] addressResult = new S7AddressData[address.Length];
			for (int i = 0; i < address.Length; i++)
			{
				OperateResult<S7AddressData> tmp = S7AddressData.ParseFrom( address[i], length[i] );
				if (!tmp.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( tmp );

				addressResult[i] = tmp.Content;
			}

			return await ReadAsync( addressResult );
		}

		/// <inheritdoc cref="Read(S7AddressData[])"/>
		public async Task<OperateResult<byte[]>> ReadAsync( S7AddressData[] s7Addresses )
		{
			// 这部分可以再度优化，根据19个长度或总长度达到200为止进行切割，如果大于200的，直接走普通的采集
			if (s7Addresses.Length > 19)
			{
				List<byte> bytes = new List<byte>( );
				List<S7AddressData[]> groups = SoftBasic.ArraySplitByLength<S7AddressData>( s7Addresses, 19 );
				for (int i = 0; i < groups.Count; i++)
				{
					OperateResult<byte[]> read = await ReadAsync( groups[i] );
					if (!read.IsSuccess) return read;

					bytes.AddRange( read.Content );
				}
				return OperateResult.CreateSuccessResult( bytes.ToArray( ) );
			}
			else
			{
				return await ReadS7AddressDataAsync( s7Addresses );
			}
		}

		/// <inheritdoc cref="ReadS7AddressData(S7AddressData[])"/>
		private async Task<OperateResult<byte[]>> ReadS7AddressDataAsync( S7AddressData[] s7Addresses )
		{
			// 构建指令 -> Build read command
			OperateResult<byte[]> command = BuildReadCommand( s7Addresses );
			if (!command.IsSuccess) return command;

			// 核心交互 -> Core Interactions
			OperateResult<byte[]> read = await ReadFromCoreServerAsync( command.Content );
			if (!read.IsSuccess) return read;

			// 分析结果 -> Analysis results
			return AnalysisReadByte( s7Addresses, read.Content );
		}

		/// <inheritdoc cref="WriteBase(byte[])" />
		private async Task<OperateResult> WriteBaseAsync( byte[] entireValue ) => ByteTransformHelper.GetResultFromOther( await ReadFromCoreServerAsync( entireValue ), AnalysisWrite );

		/// <inheritdoc cref="Write(string, byte[])"/>
		public override async Task<OperateResult> WriteAsync( string address, byte[] value )
		{
			OperateResult<S7AddressData> analysis = S7AddressData.ParseFrom( address );
			if (!analysis.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( analysis );

			int length = value.Length;
			ushort alreadyFinished = 0;
			while (alreadyFinished < length)
			{
				ushort writeLength = (ushort)Math.Min( length - alreadyFinished, 200 );
				byte[] buffer = ByteTransform.TransByte( value, alreadyFinished, writeLength );

				OperateResult<byte[]> command = BuildWriteByteCommand( analysis, buffer );
				if (!command.IsSuccess) return command;

				OperateResult write = await WriteBaseAsync( command.Content );
				if (!write.IsSuccess) return write;

				alreadyFinished += writeLength;
				analysis.Content.AddressStart += writeLength * 8;
			}

			return OperateResult.CreateSuccessResult( );
		}
#endif
		#endregion

		#region Read Write Bool

		/********************************************************************************************************************
		 * 
		 * Warnning: 非常可惜的是，西门子的S7协议没有发现bool变量的批量读写操作。
		 * 读操作：此处使用了替代方法实现，批量读取byte，转换成自己需要的bool数组，达到模拟的效果。
		 * 写操作：批量写的方法是存在风险的，从一开始就本不应该提供这个api接口的
		 * 
		 ********************************************************************************************************************/

		/// <summary>
		/// 读取指定地址的bool数据，地址格式为I100，M100，Q100，DB20.100<br />
		/// reads bool data for the specified address in the format I100，M100，Q100，DB20.100
		/// </summary>
		/// <param name="address">起始地址，格式为I100，M100，Q100，DB20.100 ->
		/// Starting address, formatted as I100,M100,Q100,DB20.100</param>
		/// <returns>是否读取成功的结果对象 -> Whether to read the successful result object</returns>
		/// <remarks>
		/// <note type="important">
		/// 对于200smartPLC的V区，就是DB1.X，例如，V100=DB1.100
		/// </note>
		/// </remarks>
		/// <example>
		/// 假设读取M100.0的位是否通断
		/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Profinet\SiemensS7Net.cs" region="ReadBool" title="ReadBool示例" />
		/// </example>
		[HslMqttApi( "ReadBool", "" )]
		public override OperateResult<bool> ReadBool( string address ) => ByteTransformHelper.GetResultFromBytes( ReadBitFromPLC( address ), m => m[0] != 0x00 );

		/// <summary>
		/// 读取指定地址的bool数组，地址格式为I100，M100，Q100，DB20.100<br />
		/// reads bool array data for the specified address in the format I100，M100，Q100，DB20.100
		/// </summary>
		/// <param name="address">起始地址，格式为I100，M100，Q100，DB20.100 ->
		/// Starting address, formatted as I100,M100,Q100,DB20.100</param>
		/// <param name="length">读取的长度信息</param>
		/// <returns>是否读取成功的结果对象 -> Whether to read the successful result object</returns>
		/// <remarks>
		/// <note type="important">
		/// 对于200smartPLC的V区，就是DB1.X，例如，V100=DB1.100
		/// </note>
		/// </remarks>
		[HslMqttApi( "ReadBoolArray", "")]
		public override OperateResult<bool[]> ReadBool( string address, ushort length )
		{
			OperateResult<S7AddressData> analysis = S7AddressData.ParseFrom( address );
			if (!analysis.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( analysis );

			HslHelper.CalculateStartBitIndexAndLength( analysis.Content.AddressStart, length, out int newStart, out ushort byteLength, out int offset );
			analysis.Content.AddressStart = newStart;
			analysis.Content.Length = byteLength;

			OperateResult<byte[]> read = Read( new S7AddressData[] { analysis.Content } );
			if(!read.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( read );

			return OperateResult.CreateSuccessResult( read.Content.ToBoolArray( ).SelectMiddle( offset, length ) );
		}

		/// <summary>
		/// 写入PLC的一个位，例如"M100.6"，"I100.7"，"Q100.0"，"DB20.100.0"，如果只写了"M100"默认为"M100.0"<br />
		/// Write a bit of PLC, for example  "M100.6",  "I100.7",  "Q100.0",  "DB20.100.0", if only write  "M100" defaults to  "M100.0"
		/// </summary>
		/// <param name="address">起始地址，格式为"M100.6",  "I100.7",  "Q100.0",  "DB20.100.0" ->
		/// Start address, format  "M100.6",  "I100.7",  "Q100.0",  "DB20.100.0"</param>
		/// <param name="value">写入的数据，True或是False -> Writes the data, either True or False</param>
		/// <returns>是否写入成功的结果对象 -> Whether to write a successful result object</returns>
		/// <example>
		/// 假设写入M100.0的位是否通断
		/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Profinet\SiemensS7Net.cs" region="WriteBool" title="WriteBool示例" />
		/// </example>
		[HslMqttApi( "WriteBool", "" )]
		public override OperateResult Write( string address, bool value )
		{
			// 生成指令 -> Build Command
			OperateResult<byte[]> command = BuildWriteBitCommand( address, value );
			if (!command.IsSuccess) return command;

			return WriteBase( command.Content );
		}

		/// <summary>
		/// [危险] 向PLC中写入bool数组，比如你写入M100,那么data[0]对应M100.0<br />
		/// [Danger] Write the bool array to the PLC, for example, if you write M100, then data[0] corresponds to M100.0
		/// </summary>
		/// <param name="address">起始地址，格式为I100，M100，Q100，DB20.100 -> Starting address, formatted as I100,mM100,Q100,DB20.100</param>
		/// <param name="values">要写入的bool数组，长度为8的倍数 -> The bool array to write, a multiple of 8 in length</param>
		/// <returns>是否写入成功的结果对象 -> Whether to write a successful result object</returns>
		/// <remarks>
		/// <note type="warning">
		/// 批量写入bool数组存在一定的风险，原因是只能批量写入长度为8的倍数的数组，否则会影响其他的位的数据，请谨慎使用。<br />
		/// There is a certain risk in batch writing to bool arrays, because you can only batch write arrays whose length is a multiple of 8, 
		/// otherwise it will affect other bit data. Please use it with caution.
		/// </note>
		/// </remarks>
		[HslMqttApi( "WriteBoolArray", "" )]
		public override OperateResult Write( string address, bool[] values ) => Write( address, SoftBasic.BoolArrayToByte( values ) );

		#endregion

		#region Async Read Write Bool
#if !NET35 && !NET20
		/********************************************************************************************************************
		 * 
		 * Warnning: 非常可惜的是，西门子的S7协议没有发现bool变量的批量读写操作。
		 * 读操作：此处使用了替代方法实现，批量读取byte，转换成自己需要的bool数组，达到模拟的效果。
		 * 写操作：批量写的方法是存在风险的，从一开始就本不应该提供这个api接口的
		 * 
		 ********************************************************************************************************************/

		/// <inheritdoc cref="ReadBool(string)"/>
		public override async Task<OperateResult<bool>> ReadBoolAsync( string address ) => ByteTransformHelper.GetResultFromBytes( await ReadBitFromPLCAsync( address ), m => m[0] != 0x00 );

		/// <inheritdoc cref="ReadBool(string, ushort)"/>
		public async override Task<OperateResult<bool[]>> ReadBoolAsync( string address, ushort length )
		{
			OperateResult<S7AddressData> analysis = S7AddressData.ParseFrom( address );
			if (!analysis.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( analysis );

			HslHelper.CalculateStartBitIndexAndLength( analysis.Content.AddressStart, length, out int newStart, out ushort byteLength, out int offset );
			analysis.Content.AddressStart = newStart;
			analysis.Content.Length = byteLength;

			OperateResult<byte[]> read = await ReadAsync( new S7AddressData[] { analysis.Content } );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( read );

			return OperateResult.CreateSuccessResult( read.Content.ToBoolArray( ).SelectMiddle( offset, length ) );
		}

		/// <inheritdoc cref="Write(string, bool)"/>
		public override async Task<OperateResult> WriteAsync( string address, bool value )
		{
			// 生成指令 -> Build Command
			OperateResult<byte[]> command = BuildWriteBitCommand( address, value );
			if (!command.IsSuccess) return command;

			return await WriteBaseAsync( command.Content );
		}

		/// <inheritdoc cref="Write(string, bool[])"/>
		public override async Task<OperateResult> WriteAsync( string address, bool[] values ) => await WriteAsync( address, SoftBasic.BoolArrayToByte( values ) );
#endif
		#endregion

		#region Read Write Byte

		/// <summary>
		/// 读取指定地址的byte数据，地址格式I100，M100，Q100，DB20.100<br />
		/// Reads the byte data of the specified address, the address format I100,Q100,DB20.100,M100
		/// </summary>
		/// <param name="address">起始地址，格式为I100，M100，Q100，DB20.100 ->
		/// Starting address, formatted as I100,M100,Q100,DB20.100</param>
		/// <returns>是否读取成功的结果对象 -> Whether to read the successful result object</returns>
		/// <example>参考<see cref="Read(string, ushort)"/>的注释</example>
		[HslMqttApi( "ReadByte", "" )]
		public OperateResult<byte> ReadByte( string address ) => ByteTransformHelper.GetResultFromArray( Read( address, 1 ) );

		/// <summary>
		/// 向PLC中写入byte数据，返回值说明<br />
		/// Write byte data to the PLC, return value description
		/// </summary>
		/// <param name="address">起始地址，格式为I100，M100，Q100，DB20.100 -> Starting address, formatted as I100,mM100,Q100,DB20.100</param>
		/// <param name="value">byte数据 -> Byte data</param>
		/// <returns>是否写入成功的结果对象 -> Whether to write a successful result object</returns>
		[HslMqttApi( "WriteByte", "" )]
		public OperateResult Write( string address, byte value ) => Write( address, new byte[] { value } );

		#endregion

		#region Async Read Write Byte
#if !NET35 && !NET20
		/// <inheritdoc cref="ReadByte(string)"/>
		public async Task<OperateResult<byte>> ReadByteAsync( string address ) => ByteTransformHelper.GetResultFromArray( await ReadAsync( address, 1 ) );

		/// <inheritdoc cref="Write(string, byte)"/>
		public async Task<OperateResult> WriteAsync( string address, byte value ) => await WriteAsync( address, new byte[] { value } );
#endif
		#endregion

		#region ReadWrite String

		/// <inheritdoc/>
		public override OperateResult Write( string address, string value, Encoding encoding )
		{
			if (value == null) value = string.Empty;

			byte[] buffer = encoding.GetBytes( value );
			if (encoding == Encoding.Unicode) buffer = SoftBasic.BytesReverseByWord( buffer );

			if (CurrentPlc != SiemensPLCS.S200Smart)
			{
				// need read one time
				OperateResult<byte[]> readLength = Read( address, 2 );
				if (!readLength.IsSuccess) return readLength;

				if (readLength.Content[0] == 255) return new OperateResult<string>( "Value in plc is not string type" );
				if (readLength.Content[0] == 0) readLength.Content[0] = 254; // allow to create new string
				if (value.Length > readLength.Content[0]) return new OperateResult<string>( "String length is too long than plc defined" );

				return Write( address, SoftBasic.SpliceArray( new byte[] { readLength.Content[0], (byte)value.Length }, buffer ) );
			}
			else
			{
				return Write( address, SoftBasic.SpliceArray( new byte[] { (byte)value.Length }, buffer ) );
			}
		}

		/// <summary>
		/// 使用双字节编码的方式，将字符串以 Unicode 编码写入到PLC的地址里，可以使用中文。<br />
		/// Use the double-byte encoding method to write the character string to the address of the PLC in Unicode encoding. Chinese can be used.
		/// </summary>
		/// <param name="address">起始地址，格式为I100，M100，Q100，DB20.100 -> Starting address, formatted as I100,mM100,Q100,DB20.100</param>
		/// <param name="value">字符串的值</param>
		/// <returns>是否写入成功的结果对象</returns>
		[HslMqttApi( ApiTopic = "WriteWString", Description = "写入unicode编码的字符串，支持中文" )]
		public OperateResult WriteWString( string address, string value )
		{
			//await WriteAsync( address, value, Encoding.Unicode );
			if (CurrentPlc != SiemensPLCS.S200Smart)
			{
				if (value == null) value = string.Empty;
				byte[] buffer = Encoding.Unicode.GetBytes( value );
				buffer = SoftBasic.BytesReverseByWord( buffer );

				// need read one time
				OperateResult<byte[]> readLength = Read( address, 4 );
				if (!readLength.IsSuccess) return readLength;

				int defineLength = readLength.Content[0] * 256 + readLength.Content[1];
				if (value.Length > defineLength) return new OperateResult<string>( "String length is too long than plc defined" );

				byte[] write = new byte[buffer.Length + 4];
				write[0] = readLength.Content[0];
				write[1] = readLength.Content[1];
				write[2] = BitConverter.GetBytes( value.Length )[1];
				write[3] = BitConverter.GetBytes( value.Length )[0];
				buffer.CopyTo( write, 4 );
				return Write( address, write );
			}
			else
			{
				return Write( address, value, Encoding.Unicode );
			}
		}

		/// <inheritdoc/>
		public override OperateResult<string> ReadString( string address, ushort length, Encoding encoding )
		{
			if (length == 0) return ReadString( address, encoding );
			return base.ReadString( address, length, encoding );
		}

		/// <inheritdoc cref="ReadString(string, Encoding)"/>
		[HslMqttApi( "ReadS7String", "读取S7格式的字符串" )]
		public OperateResult<string> ReadString( string address )
		{
			return ReadString( address, Encoding.ASCII );
		}

		/// <summary>
		/// 读取西门子的地址的字符串信息，这个信息是和西门子绑定在一起，长度随西门子的信息动态变化的<br />
		/// Read the Siemens address string information. This information is bound to Siemens and its length changes dynamically with the Siemens information
		/// </summary>
		/// <remarks>
		/// 如果指定编码，一般<see cref="Encoding.ASCII"/>即可，中文需要 Encoding.GetEncoding("gb2312")
		/// </remarks>
		/// <param name="address">数据地址，具体的格式需要参照类的说明文档</param>
		/// <param name="encoding">自定的编码信息，一般<see cref="Encoding.ASCII"/>即可，中文需要 Encoding.GetEncoding("gb2312")</param>
		/// <returns>带有是否成功的字符串结果类对象</returns>
		public OperateResult<string> ReadString( string address, Encoding encoding )
		{
			if (CurrentPlc != SiemensPLCS.S200Smart)
			{
				var read = Read( address, 2 );
				if (!read.IsSuccess) return OperateResult.CreateFailedResult<string>( read );

				if (read.Content[0] == 0 || read.Content[0] == 255) return new OperateResult<string>( "Value in plc is not string type" );    // max string length can't be zero

				var readString = Read( address, (ushort)(2 + read.Content[1]) );
				if (!readString.IsSuccess) return OperateResult.CreateFailedResult<string>( readString );

				return OperateResult.CreateSuccessResult( encoding.GetString( readString.Content, 2, readString.Content.Length - 2 ) );
			}
			else
			{
				var read = Read( address, 1 );
				if (!read.IsSuccess) return OperateResult.CreateFailedResult<string>( read );

				var readString = Read( address, (ushort)(1 + read.Content[0]) );
				if (!readString.IsSuccess) return OperateResult.CreateFailedResult<string>( readString );

				return OperateResult.CreateSuccessResult( encoding.GetString( readString.Content, 1, readString.Content.Length - 1 ) );
			}
		}

		/// <summary>
		/// 读取西门子的地址的字符串信息，这个信息是和西门子绑定在一起，长度随西门子的信息动态变化的<br />
		/// Read the Siemens address string information. This information is bound to Siemens and its length changes dynamically with the Siemens information
		/// </summary>
		/// <param name="address">数据地址，具体的格式需要参照类的说明文档</param>
		/// <returns>带有是否成功的字符串结果类对象</returns>
		[HslMqttApi( "ReadWString", "读取S7格式的双字节字符串" )]
		public OperateResult<string> ReadWString( string address )
		{
			if (CurrentPlc != SiemensPLCS.S200Smart)
			{
				var read = Read( address, 4 ); // 2
				if (!read.IsSuccess) return OperateResult.CreateFailedResult<string>( read );

				var readString = Read( address, (ushort)(4 + (read.Content[2] * 256 + read.Content[3]) * 2) );
				if (!readString.IsSuccess) return OperateResult.CreateFailedResult<string>( readString );

				return OperateResult.CreateSuccessResult( Encoding.Unicode.GetString( SoftBasic.BytesReverseByWord( readString.Content.RemoveBegin( 4 ) ) ) );
			}
			else
			{
				var read = Read( address, 1 );
				if (!read.IsSuccess) return OperateResult.CreateFailedResult<string>( read );

				var readString = Read( address, (ushort)(1 + read.Content[0] * 2) );
				if (!readString.IsSuccess) return OperateResult.CreateFailedResult<string>( readString );

				return OperateResult.CreateSuccessResult( Encoding.Unicode.GetString( readString.Content, 1, readString.Content.Length - 1 ) );
			}
		}

		#endregion

		#region Async ReadWrite String
#if !NET35 && !NET20

		/// <inheritdoc/>
		public override async Task<OperateResult> WriteAsync( string address, string value, Encoding encoding )
		{
			if (value == null) value = string.Empty;

			byte[] buffer = encoding.GetBytes( value );
			if (encoding == Encoding.Unicode) buffer = SoftBasic.BytesReverseByWord( buffer );

			if (CurrentPlc != SiemensPLCS.S200Smart)
			{
				// need read one time
				OperateResult<byte[]> readLength = await ReadAsync( address, 2 );
				if (!readLength.IsSuccess) return readLength;

				if (readLength.Content[0] == 255) return new OperateResult<string>( "Value in plc is not string type" );
				if (readLength.Content[0] == 0) readLength.Content[0] = 254; // allow to create new string
				if (value.Length > readLength.Content[0]) return new OperateResult<string>( "String length is too long than plc defined" );

				return await WriteAsync( address, SoftBasic.SpliceArray( new byte[] { readLength.Content[0], (byte)buffer.Length }, buffer ) );
			}
			else
			{
				return await WriteAsync( address, SoftBasic.SpliceArray( new byte[] { (byte)buffer.Length }, buffer ) );
			}
		}

		/// <inheritdoc cref="WriteWString(string, string)"/>
		public async Task<OperateResult> WriteWStringAsync( string address, string value )
		{
			//await WriteAsync( address, value, Encoding.Unicode );
			if (CurrentPlc != SiemensPLCS.S200Smart)
			{
				if (value == null) value = string.Empty;
				byte[] buffer = Encoding.Unicode.GetBytes( value );
				buffer = SoftBasic.BytesReverseByWord( buffer );

				// need read one time
				OperateResult<byte[]> readLength = await ReadAsync( address, 4 );
				if (!readLength.IsSuccess) return readLength;

				int defineLength = readLength.Content[0] * 256 + readLength.Content[1];
				if (value.Length > defineLength) return new OperateResult<string>( "String length is too long than plc defined" );

				byte[] write = new byte[buffer.Length + 4];
				write[0] = readLength.Content[0];
				write[1] = readLength.Content[1];
				write[2] = BitConverter.GetBytes( value.Length )[1];
				write[3] = BitConverter.GetBytes( value.Length )[0];
				buffer.CopyTo( write, 4 );
				return await WriteAsync( address, write );
			}
			else
			{
				return await WriteAsync( address, value, Encoding.Unicode );
			}
		}

		/// <inheritdoc/>
		public async override Task<OperateResult<string>> ReadStringAsync( string address, ushort length, Encoding encoding )
		{
			if (length == 0) return await ReadStringAsync( address, encoding );
			return await base.ReadStringAsync( address, length, encoding );
		}

		/// <inheritdoc cref="ReadString(string)"/>
		public async Task<OperateResult<string>> ReadStringAsync( string address )
		{
			return await ReadStringAsync( address, Encoding.ASCII );
		}

		/// <inheritdoc cref="ReadString(string, Encoding)"/>
		public async Task<OperateResult<string>> ReadStringAsync( string address, Encoding encoding )
		{
			if (CurrentPlc != SiemensPLCS.S200Smart)
			{
				var read = await ReadAsync( address, 2 );
				if (!read.IsSuccess) return OperateResult.CreateFailedResult<string>( read );

				if (read.Content[0] == 0 || read.Content[0] == 255) return new OperateResult<string>( "Value in plc is not string type" );    // max string length can't be zero

				var readString = await ReadAsync( address, (ushort)(2 + read.Content[1]) );
				if (!readString.IsSuccess) return OperateResult.CreateFailedResult<string>( readString );

				return OperateResult.CreateSuccessResult( encoding.GetString( readString.Content, 2, readString.Content.Length - 2 ) );
			}
			else
			{
				var read = await ReadAsync( address, 1 );
				if (!read.IsSuccess) return OperateResult.CreateFailedResult<string>( read );

				var readString = await ReadAsync( address, (ushort)(1 + read.Content[0]) );
				if (!readString.IsSuccess) return OperateResult.CreateFailedResult<string>( readString );

				return OperateResult.CreateSuccessResult( encoding.GetString( readString.Content, 1, readString.Content.Length - 1 ) );
			}
		}

		/// <inheritdoc cref="ReadWString(string)"/>
		public async Task<OperateResult<string>> ReadWStringAsync( string address )
		{
			if (CurrentPlc != SiemensPLCS.S200Smart)
			{
				var read = await ReadAsync( address, 4 ); // 2
				if (!read.IsSuccess) return OperateResult.CreateFailedResult<string>( read );

				var readString = await ReadAsync( address, (ushort)(4 + (read.Content[2] * 256 + read.Content[3]) * 2) );
				if (!readString.IsSuccess) return OperateResult.CreateFailedResult<string>( readString );

				return OperateResult.CreateSuccessResult( Encoding.Unicode.GetString( SoftBasic.BytesReverseByWord( readString.Content.RemoveBegin( 4 ) ) ) );
			}
			else
			{
				var read = await ReadAsync( address, 1 );
				if (!read.IsSuccess) return OperateResult.CreateFailedResult<string>( read );

				var readString = await ReadAsync( address, (ushort)(1 + read.Content[0] * 2) );
				if (!readString.IsSuccess) return OperateResult.CreateFailedResult<string>( readString );

				return OperateResult.CreateSuccessResult( Encoding.Unicode.GetString( readString.Content, 1, readString.Content.Length - 1 ) );
			}
		}

#endif
		#endregion

		#region ReadWrite DateTime

		/// <summary>
		/// 从PLC中读取时间格式的数据<br />
		/// Read time format data from PLC
		/// </summary>
		/// <param name="address">地址</param>
		/// <returns>时间对象</returns>
		[HslMqttApi( "ReadDateTime", "读取PLC的时间格式的数据，这个格式是s7格式的一种" )]
		public OperateResult<DateTime> ReadDateTime( string address ) => ByteTransformHelper.GetResultFromBytes( Read( address, 8 ), SiemensDateTime.FromByteArray );

		/// <summary>
		/// 向PLC中写入时间格式的数据<br />
		/// Writes data in time format to the PLC
		/// </summary>
		/// <param name="address">地址</param>
		/// <param name="dateTime">时间</param>
		/// <returns>是否写入成功</returns>
		[HslMqttApi( "WriteDateTime", "写入PLC的时间格式的数据，这个格式是s7格式的一种" )]
		public OperateResult Write(string address, DateTime dateTime ) => Write( address, SiemensDateTime.ToByteArray( dateTime ) );

		#endregion

		#region Async ReadWrite DateTime
#if !NET35 && !NET20
		/// <inheritdoc cref="ReadDateTime(string)"/>
		public async Task<OperateResult<DateTime>> ReadDateTimeAsync( string address ) => ByteTransformHelper.GetResultFromBytes( await ReadAsync( address, 8 ), SiemensDateTime.FromByteArray );

		/// <inheritdoc cref="Write(string, DateTime)"/>
		public async Task<OperateResult> WriteAsync( string address, DateTime dateTime ) => await WriteAsync( address, SiemensDateTime.ToByteArray( dateTime ) );
#endif
		#endregion

		#region Head Codes

		//private byte[] plcHead1 = "03 00 00 16 11 E0 00 00 00 02 00 C1 02 01 00 C2 02 01 02 C0 01 0A".ToHexBytes( );
		//private byte[] plcHead2 = "03 00 00 19 02 F0 80 32 01 00 00 02 00 00 08 00 00 F0 00 00 01 00 01 01 E0".ToHexBytes( );
		private byte[] plcHead1 = new byte[22]
		{
			0x03,0x00,0x00,0x16,0x11,0xE0,0x00,0x00,0x00,0x01,0x00,0xC0,0x01,0x0A,0xC1,0x02,
			0x01,0x02,0xC2,0x02,0x01,0x00 
		};
		private byte[] plcHead2 = new byte[25]
		{
			0x03,0x00,0x00,0x19,0x02,0xF0,0x80,0x32,0x01,0x00,0x00,0x04,0x00,0x00,0x08,0x00,
			0x00,0xF0,0x00,0x00,0x01,0x00,0x01,0x01,0xE0
		};
		private byte[] plcOrderNumber = new byte[]
		{
			0x03,0x00,0x00,0x21,0x02,0xF0,0x80,0x32,0x07,0x00,0x00,0x00,0x01,0x00,0x08,0x00,
			0x08,0x00,0x01,0x12,0x04,0x11,0x44,0x01,0x00,0xFF,0x09,0x00,0x04,0x00,0x11,0x00,
			0x00
		};
		private SiemensPLCS CurrentPlc = SiemensPLCS.S1200;
		private byte[] plcHead1_200smart = new byte[22]
		{
			0x03,0x00,0x00,0x16,0x11,0xE0,0x00,0x00,0x00,0x01,0x00,0xC1,0x02,0x10,0x00,0xC2,
			0x02,0x03,0x00,0xC0,0x01,0x0A 
		};
		private byte[] plcHead2_200smart = new byte[25]
		{
			0x03,0x00,0x00,0x19,0x02,0xF0,0x80,0x32,0x01,0x00,0x00,0xCC,0xC1,0x00,0x08,0x00,
			0x00,0xF0,0x00,0x00,0x01,0x00,0x01,0x03,0xC0
		};

		private byte[] plcHead1_200 = new byte[22]
		{
			0x03,0x00,0x00,0x16,0x11,0xE0,0x00,0x00,0x00,0x01,0x00,0xC1,0x02,0x4D,0x57,0xC2,
			0x02,0x4D,0x57,0xC0,0x01,0x09
		};
		private byte[] plcHead2_200 = new byte[25]
		{
			0x03,0x00,0x00,0x19,0x02,0xF0,0x80,0x32,0x01,0x00,0x00,0x00,0x00,0x00,0x08,0x00,
			0x00,0xF0,0x00,0x00,0x01,0x00,0x01,0x03,0xC0
		};

		byte[] S7_STOP = {
			0x03, 0x00, 0x00, 0x21, 0x02, 0xf0, 0x80, 0x32, 0x01, 0x00, 0x00, 0x0e, 0x00, 0x00, 0x10, 0x00,
			0x00, 0x29, 0x00, 0x00, 0x00, 0x00, 0x00, 0x09, 0x50, 0x5f, 0x50, 0x52, 0x4f, 0x47, 0x52, 0x41,
			0x4d
		};
		
		byte[] S7_HOT_START = {
			0x03, 0x00, 0x00, 0x25, 0x02, 0xf0, 0x80, 0x32, 0x01, 0x00, 0x00, 0x0c, 0x00, 0x00, 0x14, 0x00,
			0x00, 0x28, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xfd, 0x00, 0x00, 0x09, 0x50, 0x5f, 0x50, 0x52,
			0x4f, 0x47, 0x52, 0x41, 0x4d
		};
		
		byte[] S7_COLD_START = {
			0x03, 0x00, 0x00, 0x27, 0x02, 0xf0, 0x80, 0x32, 0x01, 0x00, 0x00, 0x0f, 0x00, 0x00, 0x16, 0x00,
			0x00, 0x28, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xfd, 0x00, 0x02, 0x43, 0x20, 0x09, 0x50, 0x5f,
			0x50, 0x52, 0x4f, 0x47, 0x52, 0x41, 0x4d
		};

		#endregion

		#region Private Member

		private byte plc_rack = 0x00;
		private byte plc_slot = 0x00;
		private int pdu_length = 0;

		const byte pduStart = 0x28;            // CPU start
		const byte pduStop = 0x29;             // CPU stop
		const byte pduAlreadyStarted = 0x02;   // CPU already in run mode
		const byte pduAlreadyStopped = 0x07;   // CPU already in stop mode

		#endregion

		#region Object Override

		/// <inheritdoc/>
		public override string ToString() => $"SiemensS7Net {CurrentPlc}[{IpAddress}:{Port}]";

		#endregion

		#region Build Command
		
		/// <summary>
		/// A general method for generating a command header to read a Word data
		/// </summary>
		/// <param name="s7Addresses">siemens address</param>
		/// <returns>Message containing the result object</returns>
		public static OperateResult<byte[]> BuildReadCommand( S7AddressData[] s7Addresses )
		{
			if (s7Addresses == null) throw new NullReferenceException( "s7Addresses" );
			if (s7Addresses.Length > 19) throw new Exception( StringResources.Language.SiemensReadLengthCannotLargerThan19 );

			int readCount = s7Addresses.Length;
			byte[] _PLCCommand = new byte[19 + readCount * 12];
			// ======================================================================================
			_PLCCommand[ 0] = 0x03;                                                // 报文头 -> Head
			_PLCCommand[ 1] = 0x00;
			_PLCCommand[ 2] = (byte)(_PLCCommand.Length / 256);                    // 长度 -> Length
			_PLCCommand[ 3] = (byte)(_PLCCommand.Length % 256);
			_PLCCommand[ 4] = 0x02;                                                // 固定 -> Fixed
			_PLCCommand[ 5] = 0xF0;
			_PLCCommand[ 6] = 0x80;
			_PLCCommand[ 7] = 0x32;                                                // 协议标识 -> Protocol identification
			_PLCCommand[ 8] = 0x01;                                                // 命令：发 -> Command: Send
			_PLCCommand[ 9] = 0x00;                                                // redundancy identification (reserved): 0x0000;
			_PLCCommand[10] = 0x00;
			_PLCCommand[11] = 0x00;                                                // protocol data unit reference; it’s increased by request event;
			_PLCCommand[12] = 0x01;
			_PLCCommand[13] = (byte)((_PLCCommand.Length - 17) / 256);             // 参数命令数据总长度 -> Parameter command Data total length
			_PLCCommand[14] = (byte)((_PLCCommand.Length - 17) % 256);
			_PLCCommand[15] = 0x00;                                                // 读取内部数据时为00，读取CPU型号为Data数据长度 -> Read internal data is 00, read CPU model is data length
			_PLCCommand[16] = 0x00;
			// =====================================================================================
			_PLCCommand[17] = 0x04;                                                // 读写指令，04读，05写 -> Read-write instruction, 04 read, 05 Write
			_PLCCommand[18] = (byte)readCount;                                     // 读取数据块个数 -> Number of data blocks read

			for (int ii = 0; ii < readCount; ii++)
			{
				//===========================================================================================
				// 指定有效值类型 -> Specify a valid value type
				_PLCCommand[19 + ii * 12] = 0x12;
				// 接下来本次地址访问长度 -> The next time the address access length
				_PLCCommand[20 + ii * 12] = 0x0A;
				// 语法标记，ANY -> Syntax tag, any
				_PLCCommand[21 + ii * 12] = 0x10;
				// 按字为单位 -> by word
				if (s7Addresses[ii].DataCode == 0x1E || s7Addresses[ii].DataCode == 0x1F)
				{
					_PLCCommand[22 + ii * 12] = s7Addresses[ii].DataCode;
					// 访问数据的个数 -> Number of Access data
					_PLCCommand[23 + ii * 12] = (byte)(s7Addresses[ii].Length / 2 / 256);
					_PLCCommand[24 + ii * 12] = (byte)(s7Addresses[ii].Length / 2 % 256);
				}
				else
				{
					if(s7Addresses[ii].DataCode == 0x06 | s7Addresses[ii].DataCode == 0x07)
					{
						// 访问数据的个数 -> Number of Access data
						_PLCCommand[22 + ii * 12] = 0x04;
						_PLCCommand[23 + ii * 12] = (byte)(s7Addresses[ii].Length / 2 / 256);
						_PLCCommand[24 + ii * 12] = (byte)(s7Addresses[ii].Length / 2 % 256);
					}
					else
					{
						_PLCCommand[22 + ii * 12] = 0x02;
						// 访问数据的个数 -> Number of Access data
						_PLCCommand[23 + ii * 12] = (byte)(s7Addresses[ii].Length / 256);
						_PLCCommand[24 + ii * 12] = (byte)(s7Addresses[ii].Length % 256);
					}
				}
				// DB块编号，如果访问的是DB块的话 -> DB block number, if you are accessing a DB block
				_PLCCommand[25 + ii * 12] = (byte)(s7Addresses[ii].DbBlock / 256);
				_PLCCommand[26 + ii * 12] = (byte)(s7Addresses[ii].DbBlock % 256);
				// 访问数据类型 -> Accessing data types
				_PLCCommand[27 + ii * 12] = s7Addresses[ii].DataCode;
				// 偏移位置 -> Offset position
				_PLCCommand[28 + ii * 12] = (byte)(s7Addresses[ii].AddressStart / 256 / 256 % 256);
				_PLCCommand[29 + ii * 12] = (byte)(s7Addresses[ii].AddressStart / 256 % 256);
				_PLCCommand[30 + ii * 12] = (byte)(s7Addresses[ii].AddressStart % 256);
			}

			return OperateResult.CreateSuccessResult( _PLCCommand );
		}

		/// <summary>
		/// 生成一个位读取数据指令头的通用方法 ->
		/// A general method for generating a bit-read-Data instruction header
		/// </summary>
		/// <param name="address">起始地址，例如M100.0，I0.1，Q0.1，DB2.100.2 ->
		/// Start address, such as M100.0,I0.1,Q0.1,DB2.100.2
		/// </param>
		/// <returns>包含结果对象的报文 -> Message containing the result object</returns>
		public static OperateResult<byte[]> BuildBitReadCommand( string address )
		{
			OperateResult<S7AddressData> analysis = S7AddressData.ParseFrom( address );
			if (!analysis.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( analysis );
			
			byte[] _PLCCommand = new byte[31];
			_PLCCommand[0] = 0x03;
			_PLCCommand[1] = 0x00;
			// 长度 -> Length
			_PLCCommand[2] = (byte)(_PLCCommand.Length / 256);
			_PLCCommand[3] = (byte)(_PLCCommand.Length % 256);
			// 固定 -> Fixed
			_PLCCommand[4] = 0x02;
			_PLCCommand[5] = 0xF0;
			_PLCCommand[6] = 0x80;
			_PLCCommand[7] = 0x32;
			// 命令：发 -> command to send
			_PLCCommand[8] = 0x01;
			// 标识序列号
			_PLCCommand[9] = 0x00;
			_PLCCommand[10] = 0x00;
			_PLCCommand[11] = 0x00;
			_PLCCommand[12] = 0x01;
			// 命令数据总长度 -> Identification serial Number
			_PLCCommand[13] = (byte)((_PLCCommand.Length - 17) / 256);
			_PLCCommand[14] = (byte)((_PLCCommand.Length - 17) % 256);

			_PLCCommand[15] = 0x00;
			_PLCCommand[16] = 0x00;

			// 命令起始符 -> Command start character
			_PLCCommand[17] = 0x04;
			// 读取数据块个数 -> Number of data blocks read
			_PLCCommand[18] = 0x01;

			//===========================================================================================
			// 读取地址的前缀 -> Read the prefix of the address
			_PLCCommand[19] = 0x12;
			_PLCCommand[20] = 0x0A;
			_PLCCommand[21] = 0x10;
			// 读取的数据时位 -> Data read-time bit
			_PLCCommand[22] = 0x01;
			// 访问数据的个数 -> Number of Access data
			_PLCCommand[23] = 0x00;
			_PLCCommand[24] = 0x01;
			// DB块编号，如果访问的是DB块的话 -> DB block number, if you are accessing a DB block
			_PLCCommand[25] = (byte)(analysis.Content.DbBlock / 256);
			_PLCCommand[26] = (byte)(analysis.Content.DbBlock % 256);
			// 访问数据类型 -> Types of reading data
			_PLCCommand[27] = analysis.Content.DataCode;
			// 偏移位置 -> Offset position
			_PLCCommand[28] = (byte)(analysis.Content.AddressStart / 256 / 256 % 256);
			_PLCCommand[29] = (byte)(analysis.Content.AddressStart / 256 % 256);
			_PLCCommand[30] = (byte)(analysis.Content.AddressStart % 256);

			return OperateResult.CreateSuccessResult( _PLCCommand );
		}

		/// <summary>
		/// 生成一个写入字节数据的指令 -> Generate an instruction to write byte data
		/// </summary>
		/// <param name="analysis">起始地址，示例M100,I100,Q100,DB1.100 -> Start Address, example M100,I100,Q100,DB1.100</param>
		/// <param name="data">原始的字节数据 -> Raw byte data</param>
		/// <returns>包含结果对象的报文 -> Message containing the result object</returns>
		public static OperateResult<byte[]> BuildWriteByteCommand( OperateResult<S7AddressData> analysis, byte[] data )
		{
			byte[] _PLCCommand = new byte[35 + data.Length];
			_PLCCommand[0] = 0x03;
			_PLCCommand[1] = 0x00;
			// 长度 -> Length
			_PLCCommand[2] = (byte)((35 + data.Length) / 256);
			_PLCCommand[3] = (byte)((35 + data.Length) % 256);
			// 固定 -> Fixed
			_PLCCommand[4] = 0x02;
			_PLCCommand[5] = 0xF0;
			_PLCCommand[6] = 0x80;
			_PLCCommand[7] = 0x32;
			// 命令 发 -> command to send
			_PLCCommand[8] = 0x01;
			// 标识序列号 -> Identification serial Number
			_PLCCommand[9] = 0x00;
			_PLCCommand[10] = 0x00;
			_PLCCommand[11] = 0x00;
			_PLCCommand[12] = 0x01;
			// 固定 -> Fixed
			_PLCCommand[13] = 0x00;
			_PLCCommand[14] = 0x0E;
			// 写入长度+4 -> Write Length +4
			_PLCCommand[15] = (byte)((4 + data.Length) / 256);
			_PLCCommand[16] = (byte)((4 + data.Length) % 256);
			// 读写指令 -> Read and write instructions
			_PLCCommand[17] = 0x05;
			// 写入数据块个数 -> Number of data blocks written
			_PLCCommand[18] = 0x01;
			// 固定，返回数据长度 -> Fixed, return data length
			_PLCCommand[19] = 0x12;
			_PLCCommand[20] = 0x0A;
			_PLCCommand[21] = 0x10;
			if(analysis.Content.DataCode == 0x06 || analysis.Content.DataCode == 0x07)
			{
				// 写入方式，1是按位，2是按字 -> Write mode, 1 is bitwise, 2 is by byte, 4 is by word
				_PLCCommand[22] = 0x04;
				// 写入数据的个数 -> Number of Write Data
				_PLCCommand[23] = (byte)(data.Length / 2 / 256);
				_PLCCommand[24] = (byte)(data.Length / 2 % 256);
			}
			else
			{
				// 写入方式，1是按位，2是按字 -> Write mode, 1 is bitwise, 2 is by word
				_PLCCommand[22] = 0x02;
				// 写入数据的个数 -> Number of Write Data
				_PLCCommand[23] = (byte)(data.Length / 256);
				_PLCCommand[24] = (byte)(data.Length % 256);
			}
			// DB块编号，如果访问的是DB块的话 -> DB block number, if you are accessing a DB block
			_PLCCommand[25] = (byte)(analysis.Content.DbBlock / 256);
			_PLCCommand[26] = (byte)(analysis.Content.DbBlock % 256);
			// 写入数据的类型 -> Types of writing data
			_PLCCommand[27] = analysis.Content.DataCode;
			// 偏移位置 -> Offset position
			_PLCCommand[28] = (byte)(analysis.Content.AddressStart / 256 / 256 % 256); ;
			_PLCCommand[29] = (byte)(analysis.Content.AddressStart / 256 % 256);
			_PLCCommand[30] = (byte)(analysis.Content.AddressStart % 256);
			// 按字写入 -> Write by Word
			_PLCCommand[31] = 0x00;
			_PLCCommand[32] = 0x04;
			// 按位计算的长度 -> The length of the bitwise calculation
			_PLCCommand[33] = (byte)(data.Length * 8 / 256);
			_PLCCommand[34] = (byte)(data.Length * 8 % 256);

			data.CopyTo( _PLCCommand, 35 );

			return OperateResult.CreateSuccessResult( _PLCCommand );
		}

		/// <summary>
		/// 生成一个写入位数据的指令 -> Generate an instruction to write bit data
		/// </summary>
		/// <param name="address">起始地址，示例M100,I100,Q100,DB1.100 -> Start Address, example M100,I100,Q100,DB1.100</param>
		/// <param name="data">是否通断 -> Power on or off</param>
		/// <returns>包含结果对象的报文 -> Message containing the result object</returns>
		public static OperateResult<byte[]> BuildWriteBitCommand( string address, bool data )
		{
			OperateResult<S7AddressData> analysis = S7AddressData.ParseFrom( address );
			if (!analysis.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( analysis );

			byte[] buffer = new byte[1];
			buffer[0] = data ? (byte)0x01 : (byte)0x00;

			byte[] _PLCCommand = new byte[35 + buffer.Length];
			_PLCCommand[0] = 0x03;
			_PLCCommand[1] = 0x00;
			// 长度 -> length
			_PLCCommand[2] = (byte)((35 + buffer.Length) / 256);
			_PLCCommand[3] = (byte)((35 + buffer.Length) % 256);
			// 固定 -> fixed
			_PLCCommand[4] = 0x02;
			_PLCCommand[5] = 0xF0;
			_PLCCommand[6] = 0x80;
			_PLCCommand[7] = 0x32;
			// 命令 发 -> command to send
			_PLCCommand[8] = 0x01;
			// 标识序列号 -> Identification serial Number
			_PLCCommand[9] = 0x00;
			_PLCCommand[10] = 0x00;
			_PLCCommand[11] = 0x00;
			_PLCCommand[12] = 0x01;
			// 固定 -> fixed
			_PLCCommand[13] = 0x00;
			_PLCCommand[14] = 0x0E;
			// 写入长度+4 -> Write Length +4
			_PLCCommand[15] = (byte)((4 + buffer.Length) / 256);
			_PLCCommand[16] = (byte)((4 + buffer.Length) % 256);
			// 命令起始符 -> Command start character
			_PLCCommand[17] = 0x05;
			// 写入数据块个数 -> Number of data blocks written
			_PLCCommand[18] = 0x01;
			_PLCCommand[19] = 0x12;
			_PLCCommand[20] = 0x0A;
			_PLCCommand[21] = 0x10;
			// 写入方式，1是按位，2是按字 -> Write mode, 1 is bitwise, 2 is by word
			_PLCCommand[22] = 0x01;
			// 写入数据的个数 -> Number of Write Data
			_PLCCommand[23] = (byte)(buffer.Length / 256);
			_PLCCommand[24] = (byte)(buffer.Length % 256);
			// DB块编号，如果访问的是DB块的话 -> DB block number, if you are accessing a DB block
			_PLCCommand[25] = (byte)(analysis.Content.DbBlock / 256);
			_PLCCommand[26] = (byte)(analysis.Content.DbBlock % 256);
			// 写入数据的类型 -> Types of writing data
			_PLCCommand[27] = analysis.Content.DataCode;
			// 偏移位置 -> Offset position
			_PLCCommand[28] = (byte)(analysis.Content.AddressStart / 256 / 256);
			_PLCCommand[29] = (byte)(analysis.Content.AddressStart / 256);
			_PLCCommand[30] = (byte)(analysis.Content.AddressStart % 256);
			// 按位写入 -> Bitwise Write
			if (analysis.Content.DataCode == 0x1C)
			{
				_PLCCommand[31] = 0x00;
				_PLCCommand[32] = 0x09;
			}
			else
			{
				_PLCCommand[31] = 0x00;
				_PLCCommand[32] = 0x03;
			}
			// 按位计算的长度 -> The length of the bitwise calculation
			_PLCCommand[33] = (byte)(buffer.Length / 256);
			_PLCCommand[34] = (byte)(buffer.Length % 256);

			buffer.CopyTo( _PLCCommand, 35 );

			return OperateResult.CreateSuccessResult( _PLCCommand );
		}


		private static OperateResult<byte[]> AnalysisReadByte( S7AddressData[] s7Addresses, byte[] content )
		{
			// 分析结果 -> Analysis results
			int receiveCount = 0;
			for (int i = 0; i < s7Addresses.Length; i++)
			{
				if (s7Addresses[i].DataCode == 0x1F || s7Addresses[i].DataCode == 0x1E)
					receiveCount += s7Addresses[i].Length * 2;
				else
					receiveCount += s7Addresses[i].Length;
			}

			if (content.Length >= 21 && content[20] == s7Addresses.Length)
			{
				byte[] buffer = new byte[receiveCount];
				int kk = 0;
				int ll = 0;
				for (int ii = 21; ii < content.Length; ii++)
				{
					if ((ii + 1) < content.Length)
					{
						if (content[ii] == 0xFF && content[ii + 1] == 0x04)
						{
							Array.Copy( content, ii + 4, buffer, ll, s7Addresses[kk].Length );
							ii += s7Addresses[kk].Length + 3;
							ll += s7Addresses[kk].Length;
							kk++;
						}
						else if (content[ii] == 0xFF && content[ii + 1] == 0x09)
						{
							int count = content[ii + 2] * 256 + content[ii + 3];
							if (count % 3 == 0)
							{
								for (int i = 0; i < count / 3; i++)
								{
									Array.Copy( content, ii + 5 + 3 * i, buffer, ll, 2 );
									ll += 2;
								}
							}
							else
							{
								for (int i = 0; i < count / 5; i++)
								{
									Array.Copy( content, ii + 7 + 5 * i, buffer, ll, 2 );
									ll += 2;
								}
							}
							ii += count + 4;
							kk++;
						}
						else if (content[ii] == 0x05 &&
							content[ii + 1] == 0x00)
						{
							return new OperateResult<byte[]>( content[ii], StringResources.Language.SiemensReadLengthOverPlcAssign );
						}
						else if (content[ii] == 0x06 &&
							content[ii + 1] == 0x00)
						{
							return new OperateResult<byte[]>( content[ii], StringResources.Language.SiemensError0006 );
						}
						else if (content[ii] == 0x0A &&
							content[ii + 1] == 0x00)
						{
							return new OperateResult<byte[]>( content[ii], StringResources.Language.SiemensError000A );
						}
					}
				}
				return OperateResult.CreateSuccessResult( buffer );
			}
			else
			{
				return new OperateResult<byte[]>( StringResources.Language.SiemensDataLengthCheckFailed + " Msg:" + SoftBasic.ByteToHexString( content, ' ' ) );
			}
		}

		private static OperateResult<byte[]> AnalysisReadBit(byte[] content )
		{
			int receiveCount = 1;
			if (content.Length >= 21 && content[20] == 1)
			{
				byte[] buffer = new byte[receiveCount];
				if (22 < content.Length)
				{
					if (content[21] == 0xFF &&
						content[22] == 0x03)
					{
						buffer[0] = content[25];
					}
				}

				return OperateResult.CreateSuccessResult( buffer );
			}
			else
			{
				return new OperateResult<byte[]>( StringResources.Language.SiemensDataLengthCheckFailed );
			}
		}

		private static OperateResult AnalysisWrite(byte[] content )
		{
			byte code = content[content.Length - 1];
			if (code != 0xFF)
				return new OperateResult( code, StringResources.Language.SiemensWriteError + code + " Msg:" + SoftBasic.ByteToHexString( content, ' ' ) );
			else
				return OperateResult.CreateSuccessResult( );
		}
		
		#endregion

	}
}

// 连接软PLC时的报文对比
// [调试] 2020-12-29 08:39:40.234 Thread [003] SiemensS7Server[102] : 客户端 [ 127.0.0.1:64454 ] 上线
// [调试] 2020-12-29 08:39:40.236 Thread [003] First: 
// 03 00 00 16 11 E0 00 00 00 01 00 C0 01 0A C1 02 01 02 C2 02 01 00  // HSL报文
// 03 00 00 16 11 E0 00 00 00 02 00 C1 02 01 00 C2 02 01 02 C0 01 0A  // 实际抓包报文
// 03 00 00 16 11 E0 00 00 00 01 00 C1 02 10 00 C2 02 03 00 C0 01 0A  // 200smart
   
// [调试] 2020-12-29 08:39:40.236 Thread [003] Second: 
// 03 00 00 19 02 F0 80 32 01 00 00 04 00 00 08 00 00 F0 00 00 01 00 01 01 E0  // HSL报文
// 03 00 00 19 02 F0 80 32 01 00 00 02 00 00 08 00 00 F0 00 00 01 00 01 01 E0  // 实际抓包报文
// 03 00 00 19 02 F0 80 32 01 00 00 CC C1 00 08 00 00 F0 00 00 01 00 01 03 C0  // 200smart
   
// [调试] 2020-12-29 10:06:59.373 Thread [012] SiemensS7Server[102] : Tcp 接收：
// 03 00 00 1F 02 F0 80 32 01 00 00 00 01 00 0E 00 00 04 01 12 0A 10 02 00 01 00 00 83 00 00 00  // 读取M0
// 03 00 00 1A 02 F0 80 32 03 00 00 00 01 00 02 00 1F 00 00 04 01 FF 04 00 01 00                 // 返回报文
// 03 00 00 13 02 F0 80 32 02 00 00 00 01 00 00 00 00 82 00                                      // 抓包返回的报文

