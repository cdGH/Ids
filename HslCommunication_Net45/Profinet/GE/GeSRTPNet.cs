using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HslCommunication;
using HslCommunication.Core;
using HslCommunication.Core.IMessage;
using HslCommunication.Core.Net;
using System.Net.Sockets;
using HslCommunication.BasicFramework;
using HslCommunication.Core.Address;
using HslCommunication.Reflection;
#if !NET20 && !NET35
using System.Threading.Tasks;
#endif

namespace HslCommunication.Profinet.GE
{
	/// <summary>
	/// Ge(通用电气)集团的SRTP协议实现的客户端，支持 I,Q,M,T,SA,SB,SC,S,G 的位和字节读写，支持 AI,AQ,R 的字读写操作，支持读取PLC时间，程序名操作<br />
	/// The client implemented by Ge (General Electric) Group's SRTP protocol, supports I, Q, M, T, SA, SB, SC, S, G bit and byte read and write, 
	/// supports AI, AQ, R word read and write Operation, support reading PLC time, program name operation
	/// </summary>
	/// <remarks>
	/// PLC的端口号默认18245，其中读取R，AI，AQ寄存器的原始字节时，传入的长度参数为字节长度。<br />
	/// 对其他寄存器而言，M1-M8的位读取，相当于 M1的字节读取。写入也是同理。
	/// </remarks>
	/// <example>
	/// 地址支持的列表如下：
	/// <list type="table">
	///   <listheader>
	///     <term>地址名称</term>
	///     <term>地址代号</term>
	///     <term>示例</term>
	///     <term>地址进制</term>
	///     <term>位操作</term>
	///     <term>字节操作</term>
	///     <term>备注</term>
	///   </listheader>
	///   <item>
	///     <term>Discrete Inputs</term>
	///     <term>I</term>
	///     <term>I1,I100</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term>注意：读位和字节，属于不同的地址</term>
	///   </item>
	///   <item>
	///     <term>Discrete Outputs</term>
	///     <term>Q</term>
	///     <term>Q1,Q100</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term>注意：读位和字节，属于不同的地址</term>
	///   </item>
	///   <item>
	///     <term>Discrete Internals</term>
	///     <term>M</term>
	///     <term>M1,M100</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term>注意：读位和字节，属于不同的地址</term>
	///   </item>
	///   <item>
	///     <term>Discrete Temporaries</term>
	///     <term>T</term>
	///     <term>T1,T100</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term>注意：读位和字节，属于不同的地址</term>
	///   </item>
	///   <item>
	///     <term>SA Discrete</term>
	///     <term>SA</term>
	///     <term>SA1,SA100</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term>注意：读位和字节，属于不同的地址</term>
	///   </item>
	///   <item>
	///     <term>SB Discrete</term>
	///     <term>SB</term>
	///     <term>SB1,SB100</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term>注意：读位和字节，属于不同的地址</term>
	///   </item>
	///   <item>
	///     <term>SC Discrete</term>
	///     <term>SC</term>
	///     <term>SC1,SC100</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term>注意：读位和字节，属于不同的地址</term>
	///   </item>
	///   <item>
	///     <term>S Discrete</term>
	///     <term>S</term>
	///     <term>S1,S100</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term>注意：读位和字节，属于不同的地址</term>
	///   </item>
	///   <item>
	///     <term>Genius Global Data</term>
	///     <term>G</term>
	///     <term>G1,G100</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term>注意：读位和字节，属于不同的地址</term>
	///   </item>
	///   <item>
	///     <term>Analog Inputs </term>
	///     <term>AI</term>
	///     <term>AI1,AI100</term>
	///     <term>10</term>
	///     <term>×</term>
	///     <term>√</term>
	///     <term>虽然读取的时候，长度是字节，但是实际是字单位的，所以不支持 ReadByte</term>
	///   </item>
	///   <item>
	///     <term>Analog Outputs </term>
	///     <term>QI</term>
	///     <term>QI1,QI100</term>
	///     <term>10</term>
	///     <term>×</term>
	///     <term>√</term>
	///     <term>虽然读取的时候，长度是字节，但是实际是字单位的，所以不支持 ReadByte</term>
	///   </item>
	///   <item>
	///     <term>Registers </term>
	///     <term>R</term>
	///     <term>R1,R100</term>
	///     <term>10</term>
	///     <term>×</term>
	///     <term>√</term>
	///     <term>虽然读取的时候，长度是字节，但是实际是字单位的，所以不支持 ReadByte</term>
	///   </item>
	/// </list>
	/// <note type="important">对于 AI,AQ,R 类型的数据来说，底层是以字为单位的，但是调用 <see cref="Read(string, ushort)"/> 来读取原始数据时，需要传入2被的字长度，对于其他方法不影响</note>
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Profinet\GeSRTPNetSample.cs" region="Usage" title="简单的短连接使用" />
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Profinet\GeSRTPNetSample.cs" region="Usage2" title="简单的长连接使用" />
	/// 
	/// 假设起始地址为R1，R1存储了温度，100.6℃值为1006，R2存储了压力，1.23Mpa值为123，R3-R4存储了产量计数，读取如下：
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Profinet\GeSRTPNetSample.cs" region="ReadExample2" title="Read示例" />
	/// 以下是读取不同类型数据的示例
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Profinet\GeSRTPNetSample.cs" region="ReadExample1" title="Read示例" />
	/// </example>
	public class GeSRTPNet : NetworkDeviceBase
	{
		#region Contructor

		/// <summary>
		/// 实例化一个默认的对象<br />
		/// Instantiate a default object
		/// </summary>
		public GeSRTPNet( )
		{
			ByteTransform = new RegularByteTransform( );
			WordLength    = 2;
		}

		/// <summary>
		/// 指定IP地址和端口号来实例化一个对象<br />
		/// Specify the IP address and port number to instantiate an object
		/// </summary>
		/// <param name="ipAddress">Ip地址</param>
		/// <param name="port">端口号</param>
		public GeSRTPNet( string ipAddress, int port = 18245 ) : this( )
		{
			IpAddress     = ipAddress;
			Port          = port;
		}

		#endregion

		/// <inheritdoc/>
		protected override INetMessage GetNewNetMessage( ) => new GeSRTPMessage( );

		/// <inheritdoc/>
		protected override OperateResult InitializationOnConnect( Socket socket )
		{
			OperateResult<byte[]> read = ReadFromCoreServer( socket, new byte[56] );
			if (!read.IsSuccess) return read;

			return OperateResult.CreateSuccessResult( );
		}

#if !NET20 && !NET35
		/// <inheritdoc/>
		protected async override Task<OperateResult> InitializationOnConnectAsync( Socket socket )
		{
			OperateResult<byte[]> read = await ReadFromCoreServerAsync( socket, new byte[56] );
			if (!read.IsSuccess) return read;

			return OperateResult.CreateSuccessResult( );
		}
#endif
		#region Read Write

		/// <summary>
		/// 批量读取字节数组信息，需要指定地址和长度，返回原始的字节数组，支持 I,Q,M,T,SA,SB,SC,S,G 的位和字节读写，支持 AI,AQ,R 的字读写操作，地址示例：R1,M1<br />
		/// Batch read byte array information, need to specify the address and length, return the original byte array.
		/// Support I, Q, M, T, SA, SB, SC, S, G bit and byte read and write, support AI, AQ, R word read and write operations, address examples: R1, M1
		/// </summary>
		/// <remarks>
		/// 其中读取R，AI，AQ寄存器的原始字节时，传入的长度参数为字节长度。长度为10，返回10个字节数组信息，如果返回长度不满6个字节的，一律返回6个字节的数据
		/// </remarks>
		/// <param name="address">数据地址</param>
		/// <param name="length">数据长度</param>
		/// <returns>带有成功标识的byte[]数组</returns>
		[HslMqttApi( "ReadByteArray", "" )]
		public override OperateResult<byte[]> Read( string address, ushort length )
		{
			OperateResult<byte[]> build = GeHelper.BuildReadCommand( incrementCount.GetCurrentValue( ), address, length, false );
			if (!build.IsSuccess) return build;

			OperateResult<byte[]> read = ReadFromCoreServer( build.Content );
			if (!read.IsSuccess) return read;

			return GeHelper.ExtraResponseContent( read.Content );
		}

		/// <inheritdoc/>
		[HslMqttApi( "WriteByteArray", "" )]
		public override OperateResult Write( string address, byte[] value )
		{
			OperateResult<byte[]> build = GeHelper.BuildWriteCommand( incrementCount.GetCurrentValue( ), address, value );
			if (!build.IsSuccess) return build;

			OperateResult<byte[]> read = ReadFromCoreServer( build.Content );
			if (!read.IsSuccess) return read;

			return GeHelper.ExtraResponseContent( read.Content );
		}

		/// <summary>
		/// 根据指定的地址来读取一个字节的数据，按照字节为单位，例如 M1 字节，就是指 M1-M8 位组成的字节，M2 字节就是 M9-M16 组成的字节。不支持对 AI,AQ,R 寄存器的字节读取<br />
		/// A byte of data is read according to the specified address, and the unit is byte. For example, M1 byte refers to a byte composed of M1-M8 bits, 
		/// and M2 byte is a byte composed of M9-M16. Does not support byte reading of AI, AQ, R registers
		/// </summary>
		/// <param name="address">地址信息</param>
		/// <returns>带有成功标识的 <see cref="byte"/> 数据</returns>
		[HslMqttApi( "ReadByte", "" )]
		public OperateResult<byte> ReadByte( string address )
		{
			OperateResult<GeSRTPAddress> analysis = GeSRTPAddress.ParseFrom( address, 1, true );
			if (!analysis.IsSuccess) return OperateResult.CreateFailedResult<byte>( analysis );

			if (analysis.Content.DataCode == 0x0A ||
				analysis.Content.DataCode == 0x0C ||
				analysis.Content.DataCode == 0x08)
			{
				return new OperateResult<byte>( StringResources.Language.GeSRTPNotSupportByteReadWrite );
			}

			return ByteTransformHelper.GetResultFromArray( Read( address, 1 ) );
		}

		/// <summary>
		/// 向PLC中写入byte数据，返回是否写入成功<br />
		/// Write byte data to PLC and return whether the writing is successful
		/// </summary>
		/// <param name="address">起始地址，格式为I100，M100，Q100，DB20.100 -> Starting address, formatted as I100,mM100,Q100,DB20.100</param>
		/// <param name="value">byte数据 -> Byte data</param>
		/// <returns>是否写入成功的结果对象 -> Whether to write a successful result object</returns>
		[HslMqttApi( "WriteByte", "" )]
		public OperateResult Write( string address, byte value )
		{
			OperateResult<GeSRTPAddress> analysis = GeSRTPAddress.ParseFrom( address, 1, true );
			if (!analysis.IsSuccess) return OperateResult.CreateFailedResult<byte>( analysis );

			if (analysis.Content.DataCode == 0x0A ||
				analysis.Content.DataCode == 0x0C ||
				analysis.Content.DataCode == 0x08)
			{
				return new OperateResult<byte>( StringResources.Language.GeSRTPNotSupportByteReadWrite );
			}

			return Write( address, new byte[] { value } );
		}

		/// <summary>
		/// 按照位为单位，批量从指定的地址里读取 bool 数组数据，不支持 AI，AQ，R 地址类型，地址比如从1开始，例如 I1,Q1,M1,T1,SA1,SB1,SC1,S1,G1<br />
		/// In units of bits, read bool array data from the specified address in batches. AI, AQ, 
		/// R address types are not supported. For example, the address starts from 1, such as I1, Q1, M1, T1, SA1, SB1, SC1, S1, G1
		/// </summary>
		/// <param name="address">PLC的地址信息，例如 M1, G1</param>
		/// <param name="length">读取的长度信息，按照位为单位</param>
		/// <returns>包含是否读取成功的bool数组</returns>
		[HslMqttApi( "ReadBoolArray", "" )]
		public override OperateResult<bool[]> ReadBool( string address, ushort length )
		{
			OperateResult<GeSRTPAddress> analysis = GeSRTPAddress.ParseFrom( address, length, true );
			if (!analysis.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( analysis );

			OperateResult<byte[]> build = GeHelper.BuildReadCommand( incrementCount.GetCurrentValue( ), analysis.Content );
			if (!build.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( build );

			OperateResult<byte[]> read = ReadFromCoreServer( build.Content );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( read );

			OperateResult<byte[]> extra = GeHelper.ExtraResponseContent( read.Content );
			if (!extra.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( extra );

			return OperateResult.CreateSuccessResult( extra.Content.ToBoolArray( ).SelectMiddle( analysis.Content.AddressStart % 8, length ) );
		}

		/// <summary>
		/// 按照位为单位，批量写入 bool 数组到指定的地址里，不支持 AI，AQ，R 地址类型，地址比如从1开始，例如 I1,Q1,M1,T1,SA1,SB1,SC1,S1,G1<br />
		/// In units of bits, write bool arrays in batches to the specified addresses. AI, AQ, and R address types are not supported. For example, 
		/// the address starts from 1, such as I1, Q1, M1, T1, SA1, SB1, SC1, S1, G1
		/// </summary>
		/// <param name="address">PLC的地址信息，例如 M1, G1</param>
		/// <param name="value">bool 数组</param>
		/// <returns>是否写入成功的结果对象</returns>
		[HslMqttApi( ApiTopic = "WriteBoolArray", Description = "In units of bits, write bool arrays in batches to the specified addresses" )]
		public override OperateResult Write( string address, bool[] value )
		{
			OperateResult<byte[]> build = GeHelper.BuildWriteCommand( incrementCount.GetCurrentValue( ), address, value );
			if (!build.IsSuccess) return build;

			OperateResult<byte[]> read = ReadFromCoreServer( build.Content );
			if (!read.IsSuccess) return read;

			return GeHelper.ExtraResponseContent( read.Content );
		}

#if !NET20 && !NET35

		/// <inheritdoc cref="Read(string, ushort)"/>
		public async override Task<OperateResult<byte[]>> ReadAsync( string address, ushort length )
		{
			OperateResult<byte[]> build = GeHelper.BuildReadCommand( incrementCount.GetCurrentValue( ), address, length, false );
			if (!build.IsSuccess) return build;

			OperateResult<byte[]> read = await ReadFromCoreServerAsync( build.Content );
			if (!read.IsSuccess) return read;

			return GeHelper.ExtraResponseContent( read.Content );
		}

		/// <inheritdoc cref="Write(string, byte[])"/>
		public async override Task<OperateResult> WriteAsync( string address, byte[] value )
		{
			OperateResult<byte[]> build = GeHelper.BuildWriteCommand( incrementCount.GetCurrentValue( ), address, value );
			if (!build.IsSuccess) return build;

			OperateResult<byte[]> read = await ReadFromCoreServerAsync( build.Content );
			if (!read.IsSuccess) return read;

			return GeHelper.ExtraResponseContent( read.Content );
		}

		/// <inheritdoc cref="ReadByte(string)"/>
		public async Task<OperateResult<byte>> ReadByteAsync( string address )
		{
			OperateResult<GeSRTPAddress> analysis = GeSRTPAddress.ParseFrom( address, 1, true );
			if (!analysis.IsSuccess) return OperateResult.CreateFailedResult<byte>( analysis );

			if (analysis.Content.DataCode == 0x0A ||
				analysis.Content.DataCode == 0x0C ||
				analysis.Content.DataCode == 0x08)
			{
				return new OperateResult<byte>( StringResources.Language.GeSRTPNotSupportByteReadWrite );
			}

			return ByteTransformHelper.GetResultFromArray( await ReadAsync( address, 1 ) );
		}

		/// <inheritdoc cref="Write(string, byte)"/>
		public async Task<OperateResult> WriteAsync( string address, byte value )
		{
			OperateResult<GeSRTPAddress> analysis = GeSRTPAddress.ParseFrom( address, 1, true );
			if (!analysis.IsSuccess) return OperateResult.CreateFailedResult<byte>( analysis );

			if (analysis.Content.DataCode == 0x0A ||
				analysis.Content.DataCode == 0x0C ||
				analysis.Content.DataCode == 0x08)
			{
				return new OperateResult<byte>( StringResources.Language.GeSRTPNotSupportByteReadWrite );
			}

			return await WriteAsync( address, new byte[] { value } );
		}

		/// <inheritdoc cref="ReadBool(string, ushort)"/>
		public async override Task<OperateResult<bool[]>> ReadBoolAsync( string address, ushort length )
		{
			OperateResult<GeSRTPAddress> analysis = GeSRTPAddress.ParseFrom( address, length, true );
			if (!analysis.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( analysis );

			OperateResult<byte[]> build = GeHelper.BuildReadCommand( incrementCount.GetCurrentValue( ), analysis.Content );
			if (!build.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( build );

			OperateResult<byte[]> read = await ReadFromCoreServerAsync( build.Content );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( read );

			OperateResult<byte[]> extra = GeHelper.ExtraResponseContent( read.Content );
			if (!extra.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( extra );

			return OperateResult.CreateSuccessResult( extra.Content.ToBoolArray( ).SelectMiddle( analysis.Content.AddressStart % 8, length ) );
		}

		/// <inheritdoc cref="Write(string, bool[])"/>
		public async override Task<OperateResult> WriteAsync( string address, bool[] value )
		{
			OperateResult<byte[]> build = GeHelper.BuildWriteCommand( incrementCount.GetCurrentValue( ), address, value );
			if (!build.IsSuccess) return build;

			OperateResult<byte[]> read = await ReadFromCoreServerAsync( build.Content );
			if (!read.IsSuccess) return read;

			return GeHelper.ExtraResponseContent( read.Content );
		}
#endif
		#endregion

		#region Advanced Read

		/// <summary>
		/// <b>[商业授权]</b> 读取PLC当前的时间，这个时间可能是不包含时区的，需要自己转换成本地的时间。<br />
		/// <b>[Authorization]</b> Read the current time of the PLC, this time may not include the time zone, you need to convert the local time yourself.
		/// </summary>
		/// <returns>包含是否成功的时间信息</returns>
		[HslMqttApi( Description = "Read the current time of the PLC" )]
		public OperateResult<DateTime> ReadPLCTime( )
		{
			if (!Authorization.asdniasnfaksndiqwhawfskhfaiw( )) return new OperateResult<DateTime>( StringResources.Language.InsufficientPrivileges );

			OperateResult<byte[]> build = GeHelper.BuildReadCoreCommand( incrementCount.GetCurrentValue( ), 0x25, new byte[] { 0x00, 0x00, 0x00, 0x02, 0x00 } );
			if (!build.IsSuccess) return OperateResult.CreateFailedResult<DateTime>( build );

			OperateResult<byte[]> read = ReadFromCoreServer( build.Content );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<DateTime>( read );

			OperateResult<byte[]> extra = GeHelper.ExtraResponseContent( read.Content );
			if (!extra.IsSuccess) return OperateResult.CreateFailedResult<DateTime>( extra );

			return GeHelper.ExtraDateTime( extra.Content );
		}

		/// <summary>
		/// <b>[商业授权]</b> 读取PLC当前的程序的名称<br />
		/// <b>[Authorization]</b> Read the name of the current program of the PLC
		/// </summary>
		/// <returns>包含是否成的程序名称信息</returns>
		[HslMqttApi( Description = "Read the name of the current program of the PLC" )]
		public OperateResult<string> ReadProgramName( )
		{
			if (!Authorization.asdniasnfaksndiqwhawfskhfaiw( )) return new OperateResult<string>( StringResources.Language.InsufficientPrivileges );

			OperateResult<byte[]> build = GeHelper.BuildReadCoreCommand( incrementCount.GetCurrentValue( ), 0x01, new byte[] { 0x00, 0x00, 0x00, 0x02, 0x00 } );
			if (!build.IsSuccess) return OperateResult.CreateFailedResult<string>( build );

			OperateResult<byte[]> read = ReadFromCoreServer( build.Content );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<string>( read );

			OperateResult<byte[]> extra = GeHelper.ExtraResponseContent( read.Content );
			if (!extra.IsSuccess) return OperateResult.CreateFailedResult<string>( extra );

			return GeHelper.ExtraProgramName( extra.Content );
		}

#if !NET20 && !NET35
		/// <inheritdoc cref="ReadPLCTime"/>
		public async Task<OperateResult<DateTime>> ReadPLCTimeAsync( )
		{
			if (!Authorization.asdniasnfaksndiqwhawfskhfaiw( )) return new OperateResult<DateTime>( StringResources.Language.InsufficientPrivileges );

			OperateResult<byte[]> build = GeHelper.BuildReadCoreCommand( incrementCount.GetCurrentValue( ), 0x25, new byte[] { 0x00, 0x00, 0x00, 0x02, 0x00 } );
			if (!build.IsSuccess) return OperateResult.CreateFailedResult<DateTime>( build );

			OperateResult<byte[]> read = await ReadFromCoreServerAsync( build.Content );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<DateTime>( read );

			OperateResult<byte[]> extra = GeHelper.ExtraResponseContent( read.Content );
			if (!extra.IsSuccess) return OperateResult.CreateFailedResult<DateTime>( extra );

			return GeHelper.ExtraDateTime( extra.Content );
		}

		/// <inheritdoc cref="ReadProgramName"/>
		public async Task<OperateResult<string>> ReadProgramNameAsync( )
		{
			if (!Authorization.asdniasnfaksndiqwhawfskhfaiw( )) return new OperateResult<string>( StringResources.Language.InsufficientPrivileges );

			OperateResult<byte[]> build = GeHelper.BuildReadCoreCommand( incrementCount.GetCurrentValue( ), 0x01, new byte[] { 0x00, 0x00, 0x00, 0x02, 0x00 } );
			if (!build.IsSuccess) return OperateResult.CreateFailedResult<string>( build );

			OperateResult<byte[]> read = await ReadFromCoreServerAsync( build.Content );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<string>( read );

			OperateResult<byte[]> extra = GeHelper.ExtraResponseContent( read.Content );
			if (!extra.IsSuccess) return OperateResult.CreateFailedResult<string>( extra );

			return GeHelper.ExtraProgramName( extra.Content );
		}
#endif
		#endregion

		#region PrivateMember

		private SoftIncrementCount incrementCount = new SoftIncrementCount( 65535 );

		#endregion

		#region Object Override

		/// <inheritdoc/>
		public override string ToString( ) => $"GeSRTPNet[{IpAddress}:{Port}]";

		#endregion
	}
}
