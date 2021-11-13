using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HslCommunication.Core;
using HslCommunication.Core.IMessage;
using System.Net.Sockets;
using HslCommunication.Core.Net;
using HslCommunication.BasicFramework;
using HslCommunication.Reflection;
#if !NET35 && !NET20
using System.Threading.Tasks;
#endif


/********************************************************************************
 * 
 *    说明：西门子通讯类，使用Fetch/Write消息解析规格，和反字节转换规格来实现的
 *    
 *    继承自统一的自定义方法，需要在PLC端进行相关的数据配置
 * 
 * 
 *********************************************************************************/

namespace HslCommunication.Profinet.Siemens
{
	/// <summary>
	/// 使用了Fetch/Write协议来和西门子进行通讯，该种方法需要在PLC侧进行一些配置<br />
	/// Using the Fetch/write protocol to communicate with Siemens, this method requires some configuration on the PLC side
	/// </summary>
	/// <remarks>
	/// 配置的参考文章地址：https://www.cnblogs.com/dathlin/p/8685855.html
	/// <br />
	/// 与S7协议相比较而言，本协议不支持对单个的点位的读写操作。如果读取M100.0，需要读取M100的值，然后进行提取位数据。
	/// 
	/// 如果需要写入位地址的数据，可以读取plc的byte值，然后进行与或非，然后写入到plc之中。
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
	///     <term>×</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>输入寄存器</term>
	///     <term>I</term>
	///     <term>I100,I200</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>×</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>输出寄存器</term>
	///     <term>Q</term>
	///     <term>Q100,Q200</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>×</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>DB块寄存器</term>
	///     <term>DB</term>
	///     <term>DB1.100,DB1.200</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>×</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>定时器的值</term>
	///     <term>T</term>
	///     <term>T100,T200</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>×</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>计数器的值</term>
	///     <term>C</term>
	///     <term>C100,C200</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>×</term>
	///     <term></term>
	///   </item>
	/// </list>
	/// </remarks>
	/// <example>
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Profinet\SiemensFetchWriteNet.cs" region="Usage" title="简单的短连接使用" />
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Profinet\SiemensFetchWriteNet.cs" region="Usage2" title="简单的长连接使用" />
	/// </example>
	public class SiemensFetchWriteNet : NetworkDeviceBase
	{
		#region Constructor

		/// <summary>
		/// 实例化一个西门子的Fetch/Write协议的通讯对象<br />
		/// Instantiate a communication object for a Siemens Fetch/write protocol
		/// </summary>
		public SiemensFetchWriteNet( )
		{
			this.WordLength    = 2;
			this.ByteTransform = new ReverseBytesTransform( );
		}

		/// <summary>
		/// 实例化一个西门子的Fetch/Write协议的通讯对象<br />
		/// Instantiate a communication object for a Siemens Fetch/write protocol
		/// </summary>
		/// <param name="ipAddress">PLC的Ip地址 -> Specify IP Address</param>
		/// <param name="port">PLC的端口 -> Specify IP Port</param>
		public SiemensFetchWriteNet( string ipAddress, int port )
		{
			this.WordLength    = 2;
			this.IpAddress     = ipAddress;
			this.Port          = port;
			this.ByteTransform = new ReverseBytesTransform( );
		}

		/// <inheritdoc/>
		protected override INetMessage GetNewNetMessage( ) => new FetchWriteMessage( );

		#endregion

		#region Read Write Support

		/// <summary>
		/// 从PLC读取数据，地址格式为I100，Q100，DB20.100，M100，T100，C100，以字节为单位<br />
		/// Read data from PLC, address format I100,Q100,DB20.100,M100,T100,C100, in bytes
		/// </summary>
		/// <param name="address">起始地址，格式为I100，M100，Q100，DB20.100，T100，C100 ->
		/// Starting address, formatted as I100,M100,Q100,DB20.100,T100,C100
		/// </param>
		/// <param name="length">读取的数量，以字节为单位 -> The number of reads, in bytes</param>
		/// <returns>带有成功标志的字节信息 -> Byte information with a success flag</returns>
		/// <example>
		/// 假设起始地址为M100，M100存储了温度，100.6℃值为1006，M102存储了压力，1.23Mpa值为123，M104，M105，M106，M107存储了产量计数，读取如下：
		/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Profinet\SiemensFetchWriteNet.cs" region="ReadExample2" title="Read示例" />
		/// 以下是读取不同类型数据的示例
		/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Profinet\SiemensFetchWriteNet.cs" region="ReadExample1" title="Read示例" />
		/// </example>
		[HslMqttApi( "ReadByteArray", "" )]
		public override OperateResult<byte[]> Read( string address, ushort length )
		{
			// 指令解析 -> Instruction parsing
			OperateResult<byte[]> command = BuildReadCommand( address, length );
			if (!command.IsSuccess) return command;

			// 核心交互 -> Core Interactions
			OperateResult<byte[]> read = ReadFromCoreServer( command.Content );
			if (!read.IsSuccess) return read;

			// 错误码验证 -> Error code Verification
			OperateResult check = CheckResponseContent( read.Content );
			if (!check.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( check );

			// 读取正确 -> Read Right
			return OperateResult.CreateSuccessResult( SoftBasic.ArrayRemoveBegin( read.Content, 16 ) );
		}

		/// <summary>
		/// 将数据写入到PLC数据，地址格式为I100，Q100，DB20.100，M100，以字节为单位<br />
		/// Writes data to the PLC data, in the address format i100,q100,db20.100,m100, in bytes
		/// </summary>
		/// <param name="address">起始地址，格式为M100,I100,Q100,DB1.100 -> Starting address, formatted as M100,I100,Q100,DB1.100</param>
		/// <param name="value">要写入的实际数据 -> The actual data to write</param>
		/// <returns>是否写入成功的结果对象 -> Whether to write a successful result object</returns>
		/// <example>
		/// 假设起始地址为M100，M100,M101存储了温度，100.6℃值为1006，M102,M103存储了压力，1.23Mpa值为123，M104-M107存储了产量计数，写入如下：
		/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Profinet\SiemensFetchWriteNet.cs" region="WriteExample2" title="Write示例" />
		/// 以下是写入不同类型数据的示例
		/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Profinet\SiemensFetchWriteNet.cs" region="WriteExample1" title="Write示例" />
		/// </example>
		[HslMqttApi( "WriteByteArray", "" )]
		public override OperateResult Write( string address, byte[] value )
		{
			// 指令解析 -> Instruction parsing
			OperateResult<byte[]> command = BuildWriteCommand( address, value );
			if (!command.IsSuccess) return command;

			// 核心交互 -> Core Interactions
			OperateResult<byte[]> write = ReadFromCoreServer( command.Content );
			if (!write.IsSuccess) return write;

			// 错误码验证 -> Error code Verification
			OperateResult check = CheckResponseContent( write.Content );
			if (!check.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( check );

			// 写入成功 -> Write Right
			return OperateResult.CreateSuccessResult( );

		}

		#endregion

		#region Async Read Write Support
#if !NET35 && !NET20
		/// <inheritdoc cref="Read(string, ushort)"/>
		public override async Task<OperateResult<byte[]>> ReadAsync( string address, ushort length )
		{
			// 指令解析 -> Instruction parsing
			OperateResult<byte[]> command = BuildReadCommand( address, length );
			if (!command.IsSuccess) return command;

			// 核心交互 -> Core Interactions
			OperateResult<byte[]> read = await ReadFromCoreServerAsync( command.Content );
			if (!read.IsSuccess) return read;

			// 错误码验证 -> Error code Verification
			OperateResult check = CheckResponseContent( read.Content );
			if (!check.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( check );

			// 读取正确 -> Read Right
			return OperateResult.CreateSuccessResult( SoftBasic.ArrayRemoveBegin( read.Content, 16 ) );
		}

		/// <inheritdoc cref="Write(string, byte[])"/>
		public override async Task<OperateResult> WriteAsync( string address, byte[] value )
		{
			// 指令解析 -> Instruction parsing
			OperateResult<byte[]> command = BuildWriteCommand( address, value );
			if (!command.IsSuccess) return command;

			// 核心交互 -> Core Interactions
			OperateResult<byte[]> write = await ReadFromCoreServerAsync( command.Content );
			if (!write.IsSuccess) return write;

			// 错误码验证 -> Error code Verification
			OperateResult check = CheckResponseContent( write.Content );
			if (!check.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( check );

			// 写入成功 -> Write Right
			return OperateResult.CreateSuccessResult( );
		}
#endif
		#endregion

		#region Read Write Byte

		/// <summary>
		/// 读取指定地址的byte数据<br />
		/// Reads the byte data for the specified address
		/// </summary>
		/// <param name="address">起始地址，格式为M100,I100,Q100,DB1.100 -> Starting address, formatted as M100,I100,Q100,DB1.100</param>
		/// <returns>byte类型的结果对象 -> Result object of type Byte</returns>
		/// <remarks>
		/// <note type="warning">
		/// 不适用于DB块，定时器，计数器的数据读取，会提示相应的错误，读取长度必须为偶数
		/// </note>
		/// </remarks>
		[HslMqttApi( "ReadByte", "" )]
		public OperateResult<byte> ReadByte( string address ) => ByteTransformHelper.GetResultFromArray( Read( address, 1 ) );

		/// <summary>
		/// 向PLC中写入byte数据，返回是否写入成功<br />
		/// Writes byte data to the PLC and returns whether the write succeeded
		/// </summary>
		/// <param name="address">起始地址，格式为M100,I100,Q100,DB1.100 -> Starting address, formatted as M100,I100,Q100,DB1.100</param>
		/// <param name="value">要写入的实际数据 -> The actual data to write</param>
		/// <returns>是否写入成功的结果对象 -> Whether to write a successful result object</returns>
		[HslMqttApi( "WriteByte", "" )]
		public OperateResult Write(string address, byte value) => Write( address, new byte[] { value } );

		#endregion

		#region Async Read Write Byte
#if !NET35 && !NET20
		/// <inheritdoc cref="ReadByte(string)"/>
		public async Task<OperateResult<byte>> ReadByteAsync( string address ) => ByteTransformHelper.GetResultFromArray( await ReadAsync( address, 1 ) );

		/// <inheritdoc cref="Write(string, byte)"/>
		public async Task<OperateResult> WriteAsync( string address, byte value ) => await WriteAsync( address, new byte[] { value } );
#endif
		#endregion

		#region Object Override

		/// <inheritdoc/>
		public override string ToString() => $"SiemensFetchWriteNet[{IpAddress}:{Port}]";

		#endregion

		#region Static Method Helper

		/// <summary>
		/// 计算特殊的地址信息<br />
		/// Calculate special address information
		/// </summary>
		/// <param name="address">字符串信息</param>
		/// <returns>实际值</returns>
		private static int CalculateAddressStarted( string address )
		{
			if (address.IndexOf( '.' ) < 0)
			{
				return Convert.ToInt32( address );
			}
			else
			{
				string[] temp = address.Split( '.' );
				return Convert.ToInt32( temp[0] );
			}
		}

		private static OperateResult CheckResponseContent(byte[] content )
		{
			if (content[8] != 0x00) return new OperateResult( content[8], StringResources.Language.SiemensWriteError + content[8] );

			return OperateResult.CreateSuccessResult( );
		}

		/// <summary>
		/// 解析数据地址，解析出地址类型，起始地址，DB块的地址<br />
		/// Parse data address, parse out address type, start address, db block address
		/// </summary>
		/// <param name="address">起始地址，格式为M100,I100,Q100,DB1.100 -> Starting address, formatted as M100,I100,Q100,DB1.100</param>
		/// <returns>解析出地址类型，起始地址，DB块的地址 -> Resolves address type, start address, db block address</returns>
		private static OperateResult<byte, int, ushort> AnalysisAddress( string address )
		{
			var result = new OperateResult<byte, int, ushort>( );
			try
			{
				result.Content3 = 0;
				if (address[0] == 'I')
				{
					result.Content1 = 0x03;
					result.Content2 = CalculateAddressStarted( address.Substring( 1 ) );
				}
				else if (address[0] == 'Q')
				{
					result.Content1 = 0x04;
					result.Content2 = CalculateAddressStarted( address.Substring( 1 ) );
				}
				else if (address[0] == 'M')
				{
					result.Content1 = 0x02;
					result.Content2 = CalculateAddressStarted( address.Substring( 1 ) );
				}
				else if (address[0] == 'D' || address.Substring( 0, 2 ) == "DB")
				{
					result.Content1 = 0x01;
					string[] adds = address.Split( '.' );
					if (address[1] == 'B')
					{
						result.Content3 = Convert.ToUInt16( adds[0].Substring( 2 ) );
					}
					else
					{
						result.Content3 = Convert.ToUInt16( adds[0].Substring( 1 ) );
					}

					if (result.Content3 > 255)
					{
						result.Message = StringResources.Language.SiemensDBAddressNotAllowedLargerThan255;
						return result;
					}

					result.Content2 = CalculateAddressStarted( address.Substring( address.IndexOf( '.' ) + 1 ) );
				}
				else if (address[0] == 'T')
				{
					result.Content1 = 0x07;
					result.Content2 = CalculateAddressStarted( address.Substring( 1 ) );
				}
				else if (address[0] == 'C')
				{
					result.Content1 = 0x06;
					result.Content2 = CalculateAddressStarted( address.Substring( 1 ) );
				}
				else
				{
					result.Message = StringResources.Language.NotSupportedDataType;
					result.Content1 = 0;
					result.Content2 = 0;
					result.Content3 = 0;
					return result;
				}
			}
			catch (Exception ex)
			{
				result.Message = ex.Message;
				return result;
			}

			result.IsSuccess = true;
			return result;
		}
		
		#endregion

		#region Build Command

		/// <summary>
		/// 生成一个读取字数据指令头的通用方法<br />
		/// A general method for generating a command header to read a Word data
		/// </summary>
		/// <param name="address">起始地址，格式为M100,I100,Q100,DB1.100 -> Starting address, formatted as M100,I100,Q100,DB1.100</param>
		/// <param name="count">读取数据个数 -> Number of Read data</param>
		/// <returns>带结果对象的报文数据 -> Message data with a result object</returns>
		public static OperateResult<byte[]> BuildReadCommand( string address, ushort count )
		{
			OperateResult<byte, int, ushort> analysis = AnalysisAddress( address );
			if (!analysis.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( analysis );

			byte[] _PLCCommand = new byte[16];
			_PLCCommand[0] = 0x53;
			_PLCCommand[1] = 0x35;
			_PLCCommand[2] = 0x10;
			_PLCCommand[3] = 0x01;
			_PLCCommand[4] = 0x03;
			_PLCCommand[5] = 0x05;
			_PLCCommand[6] = 0x03;
			_PLCCommand[7] = 0x08;

			// 指定数据区 -> Specify Data area
			_PLCCommand[8] = analysis.Content1;
			_PLCCommand[9] = (byte)analysis.Content3;

			// 指定数据地址 -> Specify Data address
			_PLCCommand[10] = (byte)(analysis.Content2 / 256);
			_PLCCommand[11] = (byte)(analysis.Content2 % 256);

			// DB块，定时器，计数器读取长度按照字为单位，1代表2个字节，I，Q，M的1代表1个字节 ->
			// DB block, timer, counter read length per word, 1 for 2 bytes, i,q,m 1 for 1 bytes
			if (analysis.Content1 == 0x01 || analysis.Content1 == 0x06 || analysis.Content1 == 0x07)
			{
				if (count % 2 != 0)
				{
					return new OperateResult<byte[]>( StringResources.Language.SiemensReadLengthMustBeEvenNumber );
				}
				else
				{
					_PLCCommand[12] = BitConverter.GetBytes( count / 2 )[1];
					_PLCCommand[13] = BitConverter.GetBytes( count / 2 )[0];
				}
			}
			else
			{
				_PLCCommand[12] = BitConverter.GetBytes( count )[1];
				_PLCCommand[13] = BitConverter.GetBytes( count )[0];
			}

			_PLCCommand[14] = 0xff;
			_PLCCommand[15] = 0x02;

			return OperateResult.CreateSuccessResult( _PLCCommand );
		}
		
		/// <summary>
		/// 生成一个写入字节数据的指令<br />
		/// Generate an instruction to write byte data
		/// </summary>
		/// <param name="address">起始地址，格式为M100,I100,Q100,DB1.100 -> Starting address, formatted as M100,I100,Q100,DB1.100</param>
		/// <param name="data">实际的写入的内容 -> The actual content of the write</param>
		/// <returns>带结果对象的报文数据 -> Message data with a result object</returns>
		public static OperateResult<byte[]> BuildWriteCommand( string address, byte[] data )
		{
			if (data == null) data = new byte[0];

			OperateResult<byte, int, ushort> analysis = AnalysisAddress( address );
			if (!analysis.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( analysis );
			
			byte[] _PLCCommand = new byte[16 + data.Length];
			_PLCCommand[0] = 0x53;
			_PLCCommand[1] = 0x35;
			_PLCCommand[2] = 0x10;
			_PLCCommand[3] = 0x01;
			_PLCCommand[4] = 0x03;
			_PLCCommand[5] = 0x03;
			_PLCCommand[6] = 0x03;
			_PLCCommand[7] = 0x08;

			// 指定数据区 -> Specify Data area
			_PLCCommand[8] = analysis.Content1;
			_PLCCommand[9] = (byte)analysis.Content3;

			// 指定数据地址 -> Specify Data address
			_PLCCommand[10] = (byte)(analysis.Content2 / 256);
			_PLCCommand[11] = (byte)(analysis.Content2 % 256);

			if (analysis.Content1 == 0x01 || analysis.Content1 == 0x06 || analysis.Content1 == 0x07)
			{
				if (data.Length % 2 != 0)
				{
					return new OperateResult<byte[]>( StringResources.Language.SiemensReadLengthMustBeEvenNumber );
				}
				else
				{
					// 指定数据长度 -> Specify data length
					_PLCCommand[12] = BitConverter.GetBytes( data.Length / 2 )[1];
					_PLCCommand[13] = BitConverter.GetBytes( data.Length / 2 )[0];
				}
			}
			else
			{
				// 指定数据长度 -> Specify data length
				_PLCCommand[12] = BitConverter.GetBytes( data.Length )[1];
				_PLCCommand[13] = BitConverter.GetBytes( data.Length )[0];
			}
			_PLCCommand[14] = 0xff;
			_PLCCommand[15] = 0x02;

			// 放置数据 -> Placing data
			Array.Copy( data, 0, _PLCCommand, 16, data.Length );

			return OperateResult.CreateSuccessResult( _PLCCommand );
		}
		
		#endregion

	}
}
