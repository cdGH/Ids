using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HslCommunication.Core;
using HslCommunication.Core.IMessage;
using HslCommunication.Core.Net;
using HslCommunication.Reflection;
#if !NET35 && !NET20
using System.Threading.Tasks;
#endif

namespace HslCommunication.Profinet.Melsec
{
	/// <summary>
	/// 三菱PLC通讯协议，采用A兼容1E帧协议实现，使用二进制码通讯，请根据实际型号来进行选取<br />
	/// Mitsubishi PLC communication protocol, implemented using A compatible 1E frame protocol, using binary code communication, please choose according to the actual model
	/// </summary>
	/// <remarks>
	/// 本类适用于的PLC列表
	/// <list type="number">
	/// <item>FX3U(C) PLC   测试人sandy_liao</item>
	/// </list>
	/// <note type="important">本通讯类由CKernal推送，感谢</note>
	/// </remarks>
	/// <example>
	/// 数据地址支持的格式如下：
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
	///     <term>动态</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term>地址前面带0就是8进制比如X010，不带则是16进制，X40</term>
	///   </item>
	///   <item>
	///     <term>输出继电器</term>
	///     <term>Y</term>
	///     <term>Y10,Y20</term>
	///     <term>动态</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term>地址前面带0就是8进制比如Y020，不带则是16进制，Y40</term>
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
	///     <term>报警器</term>
	///     <term>F</term>
	///     <term>F100,F200</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>链接继电器</term>
	///     <term>B</term>
	///     <term>B1A0,B2A0</term>
	///     <term>16</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>定时器触点</term>
	///     <term>TS</term>
	///     <term>TS0,TS100</term>
	///     <term>10</term>
	///     <term>×</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>定时器线圈</term>
	///     <term>TC</term>
	///     <term>TC0,TC100</term>
	///     <term>10</term>
	///     <term>×</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>定时器当前值</term>
	///     <term>TN</term>
	///     <term>TN0,TN100</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>×</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>计数器器触点</term>
	///     <term>CS</term>
	///     <term>CS0,CS100</term>
	///     <term>10</term>
	///     <term>×</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>计数器线圈</term>
	///     <term>CC</term>
	///     <term>CC0,CC100</term>
	///     <term>10</term>
	///     <term>×</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>计数器当前值</term>
	///     <term>CN</term>
	///     <term>CN0,CN100</term>
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
	///     <term>链接寄存器</term>
	///     <term>W</term>
	///     <term>W0,W1A0</term>
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
	/// </list>
	/// <see cref="ReadBool(string, ushort)"/> 方法一次读取的最多点数是256点。
	/// </example>
	public class MelsecA1ENet : NetworkDeviceBase
	{
		#region Constructor

		/// <summary>
		/// 实例化一个默认的对象<br />
		/// Instantiate a default object
		/// </summary>
		public MelsecA1ENet( )
		{
			this.WordLength    = 1;
			this.ByteTransform = new RegularByteTransform( );
		}

		/// <summary>
		/// 指定ip地址和端口来来实例化一个默认的对象<br />
		/// Specify the IP address and port to instantiate a default object
		/// </summary>
		/// <param name="ipAddress">PLC的Ip地址</param>
		/// <param name="port">PLC的端口</param>
		public MelsecA1ENet( string ipAddress, int port ) : this( )
		{
			this.IpAddress     = ipAddress;
			this.Port          = port;
		}

		/// <inheritdoc/>
		protected override INetMessage GetNewNetMessage( ) => new MelsecA1EBinaryMessage( );

		#endregion

		#region Public Member

		/// <summary>
		/// PLC编号，默认为0xFF<br />
		/// PLC number, default is 0xFF
		/// </summary>
		public byte PLCNumber { get; set; } = 0xFF;

		#endregion

		#region Read Write Support

		/// <inheritdoc/>
		[HslMqttApi( "ReadByteArray", "" )]
		public override OperateResult<byte[]> Read( string address, ushort length )
		{
			// 获取指令
			var command = BuildReadCommand(address, length, false, PLCNumber);
			if (!command.IsSuccess) return OperateResult.CreateFailedResult<byte[]>(command);

			// 核心交互
			var read = ReadFromCoreServer(command.Content);
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<byte[]>(read);

			// 错误代码验证
			OperateResult check = CheckResponseLegal( read.Content );
			if (!check.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( check );

			// 数据解析，需要传入是否使用位的参数
			return ExtractActualData(read.Content, false);
		}

		/// <inheritdoc/>
		[HslMqttApi( "WriteByteArray", "" )]
		public override OperateResult Write( string address, byte[] value )
		{
			// 解析指令
			OperateResult<byte[]> command = BuildWriteWordCommand( address, value, PLCNumber );
			if (!command.IsSuccess) return command;

			// 核心交互
			OperateResult<byte[]> read = ReadFromCoreServer( command.Content );
			if (!read.IsSuccess) return read;

			// 错误码校验 (在A兼容1E协议中，结束代码后面紧跟的是异常信息的代码)
			OperateResult check = CheckResponseLegal( read.Content );
			if (!check.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( check );

			// 成功
			return OperateResult.CreateSuccessResult( );
		}

		#endregion

		#region Async Read Write Support
#if !NET35 && !NET20
		/// <inheritdoc/>
		public override async Task<OperateResult<byte[]>> ReadAsync( string address, ushort length )
		{
			// 获取指令
			var command = BuildReadCommand( address, length, false, PLCNumber );
			if (!command.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( command );

			// 核心交互
			var read = await ReadFromCoreServerAsync( command.Content );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( read );

			// 错误代码验证
			OperateResult check = CheckResponseLegal( read.Content );
			if (!check.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( check );

			// 数据解析，需要传入是否使用位的参数
			return ExtractActualData( read.Content, false );
		}

		/// <inheritdoc/>
		public override async Task<OperateResult> WriteAsync( string address, byte[] value )
		{
			// 解析指令
			OperateResult<byte[]> command = BuildWriteWordCommand( address, value, PLCNumber );
			if (!command.IsSuccess) return command;

			// 核心交互
			OperateResult<byte[]> read = await ReadFromCoreServerAsync( command.Content );
			if (!read.IsSuccess) return read;

			// 错误码校验 (在A兼容1E协议中，结束代码后面紧跟的是异常信息的代码)
			OperateResult check = CheckResponseLegal( read.Content );
			if (!check.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( check );

			// 成功
			return OperateResult.CreateSuccessResult( );
		}
#endif
		#endregion

		#region Read Write bool[]

		/// <summary>
		/// 批量读取<see cref="bool"/>数组信息，需要指定地址和长度，地址示例M100，S100，B1A，如果是X,Y, X017就是8进制地址，Y10就是16进制地址。<br />
		/// Batch read <see cref="bool"/> array information, need to specify the address and length, return <see cref="bool"/> array. 
		/// Examples of addresses M100, S100, B1A, if it is X, Y, X017 is an octal address, Y10 is a hexadecimal address.
		/// </summary>
		/// <remarks>
		/// 根据协议的规范，最多读取256长度的bool数组信息，如果需要读取更长的bool信息，需要按字为单位进行读取的操作。
		/// </remarks>
		/// <param name="address">数据地址</param>
		/// <param name="length">数据长度</param>
		/// <returns>带有成功标识的byte[]数组</returns>
		[HslMqttApi( "ReadBoolArray", "" )]
		public override OperateResult<bool[]> ReadBool( string address, ushort length )
		{
			// 获取指令
			var command = BuildReadCommand( address, length, true, PLCNumber );
			if (!command.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( command );

			// 核心交互
			var read = ReadFromCoreServer( command.Content );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( read );

			// 错误代码验证
			OperateResult check = CheckResponseLegal( read.Content );
			if (!check.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( check );

			// 数据解析，需要传入是否使用位的参数
			var extract = ExtractActualData( read.Content, true );
			if (!extract.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( extract );

			// 转化bool数组
			return OperateResult.CreateSuccessResult( extract.Content.Select( m => m == 0x01 ).Take( length ).ToArray( ) );
		}

		/// <summary>
		/// 批量写入<see cref="bool"/>数组数据，返回是否成功，地址示例M100，S100，B1A，如果是X,Y, X017就是8进制地址，Y10就是16进制地址。<br />
		/// Batch write <see cref="bool"/> array data, return whether the write was successful. 
		/// Examples of addresses M100, S100, B1A, if it is X, Y, X017 is an octal address, Y10 is a hexadecimal address.
		/// </summary>
		/// <param name="address">起始地址</param>
		/// <param name="value">写入值</param>
		/// <returns>带有成功标识的结果类对象</returns>
		[HslMqttApi( "WriteBoolArray", "" )]
		public override OperateResult Write(string address, bool[] value )
		{
			// 解析指令
			OperateResult<byte[]> command = BuildWriteBoolCommand( address, value, PLCNumber );
			if (!command.IsSuccess) return command;

			// 核心交互
			OperateResult<byte[]> read = ReadFromCoreServer( command.Content );
			if (!read.IsSuccess) return read;

			// 错误码校验 (在A兼容1E协议中，结束代码后面紧跟的是异常信息的代码)
			return CheckResponseLegal( read.Content );
		}

		#endregion

		#region Async Read Write bool[]
#if !NET35 && !NET20
		/// <inheritdoc cref="ReadBool(string, ushort)"/>
		public override async Task<OperateResult<bool[]>> ReadBoolAsync( string address, ushort length )
		{
			// 获取指令
			var command = BuildReadCommand( address, length, true, PLCNumber );
			if (!command.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( command );

			// 核心交互
			var read = await ReadFromCoreServerAsync( command.Content );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( read );

			// 错误代码验证
			OperateResult check = CheckResponseLegal( read.Content );
			if (!check.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( check );

			// 数据解析，需要传入是否使用位的参数
			var extract = ExtractActualData( read.Content, true );
			if (!extract.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( extract );

			// 转化bool数组
			return OperateResult.CreateSuccessResult( extract.Content.Select( m => m == 0x01 ).Take( length ).ToArray( ) );
		}

		/// <inheritdoc cref="Write(string, bool[])"/>
		public override async Task<OperateResult> WriteAsync( string address, bool[] values )
		{
			// 解析指令
			OperateResult<byte[]> command = BuildWriteBoolCommand( address, values, PLCNumber );
			if (!command.IsSuccess) return command;

			// 核心交互
			OperateResult<byte[]> read = await ReadFromCoreServerAsync( command.Content );
			if (!read.IsSuccess) return read;

			// 错误码校验 (在A兼容1E协议中，结束代码后面紧跟的是异常信息的代码)
			return CheckResponseLegal( read.Content );
		}
#endif
		#endregion

		#region Object Override

		/// <inheritdoc/>
		public override string ToString( ) => $"MelsecA1ENet[{IpAddress}:{Port}]";

		#endregion

		#region Static Method Helper

		/// <summary>
		/// 根据类型地址长度确认需要读取的指令头
		/// </summary>
		/// <param name="address">起始地址</param>
		/// <param name="length">长度</param>
		/// <param name="isBit">指示是否按照位成批的读出</param>
		/// <param name="plcNumber">PLC编号</param>
		/// <returns>带有成功标志的指令数据</returns>
		public static OperateResult<byte[]> BuildReadCommand(string address, ushort length, bool isBit, byte plcNumber )
		{
			var analysis = MelsecHelper.McA1EAnalysisAddress(address);
			if (!analysis.IsSuccess) return OperateResult.CreateFailedResult<byte[]>(analysis);

			// 默认信息----注意：高低字节交错
			// byte subtitle = analysis.Content1.DataType == 0x01 ? (byte)0x00 : (byte)0x01;
			byte subtitle = isBit ? (byte)0x00 : (byte)0x01;

			byte[] _PLCCommand = new byte[12];
			_PLCCommand[ 0] = subtitle;                                                  // 副标题
			_PLCCommand[ 1] = plcNumber;                                                 // PLC号
			_PLCCommand[ 2] = 0x0A;                                                      // CPU监视定时器（L）这里设置为0x00,0x0A，等待CPU返回的时间为10*250ms=2.5秒
			_PLCCommand[ 3] = 0x00;                                                      // CPU监视定时器（H）
			_PLCCommand[ 4] = BitConverter.GetBytes( analysis.Content2 )[0];             // 起始软元件（开始读取的地址）
			_PLCCommand[ 5] = BitConverter.GetBytes( analysis.Content2 )[1];
			_PLCCommand[ 6] = BitConverter.GetBytes( analysis.Content2 )[2];
			_PLCCommand[ 7] = BitConverter.GetBytes( analysis.Content2 )[3];
			_PLCCommand[ 8] = BitConverter.GetBytes( analysis.Content1.DataCode )[0];    // 软元件代码（L）
			_PLCCommand[ 9] = BitConverter.GetBytes( analysis.Content1.DataCode )[1];    // 软元件代码（H）
			_PLCCommand[10] = BitConverter.GetBytes( length )[0];                        // 软元件点数
			_PLCCommand[11] = BitConverter.GetBytes( length )[1];

			return OperateResult.CreateSuccessResult(_PLCCommand);
		}

		/// <summary>
		/// 根据类型地址以及需要写入的数据来生成指令头
		/// </summary>
		/// <param name="address">起始地址</param>
		/// <param name="value">数据值</param>
		/// <param name="plcNumber">PLC编号</param>
		/// <returns>带有成功标志的指令数据</returns>
		public static OperateResult<byte[]> BuildWriteWordCommand( string address, byte[] value, byte plcNumber )
		{
			var analysis = MelsecHelper.McA1EAnalysisAddress( address );
			if (!analysis.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( analysis );

			byte[] _PLCCommand = new byte[12 + value.Length];
			_PLCCommand[ 0] = 03;                                                 // 副标题，字单位成批写入
			_PLCCommand[ 1] = plcNumber;                                          // PLC号
			_PLCCommand[ 2] = 0x0A;                                               // CPU监视定时器（L）这里设置为0x00,0x0A，等待CPU返回的时间为10*250ms=2.5秒
			_PLCCommand[ 3] = 0x00;                                               // CPU监视定时器（H）
			_PLCCommand[ 4] = BitConverter.GetBytes( analysis.Content2 )[0];      // 起始软元件（开始读取的地址）
			_PLCCommand[ 5] = BitConverter.GetBytes( analysis.Content2 )[1];
			_PLCCommand[ 6] = BitConverter.GetBytes( analysis.Content2 )[2];
			_PLCCommand[ 7] = BitConverter.GetBytes( analysis.Content2 )[3];
			_PLCCommand[ 8] = BitConverter.GetBytes( analysis.Content1.DataCode )[0];    // 软元件代码（L）
			_PLCCommand[ 9] = BitConverter.GetBytes( analysis.Content1.DataCode )[1];    // 软元件代码（H）
			_PLCCommand[10] = BitConverter.GetBytes( value.Length / 2 )[0];       // 软元件点数
			_PLCCommand[11] = BitConverter.GetBytes( value.Length / 2 )[1];
			Array.Copy( value, 0, _PLCCommand, 12, value.Length );                  // 将具体的要写入的数据附加到写入命令后面
			return OperateResult.CreateSuccessResult( _PLCCommand );
		}

		/// <summary>
		/// 根据类型地址以及需要写入的数据来生成指令头
		/// </summary>
		/// <param name="address">起始地址</param>
		/// <param name="value">数据值</param>
		/// <param name="plcNumber">PLC编号</param>
		/// <returns>带有成功标志的指令数据</returns>
		public static OperateResult<byte[]> BuildWriteBoolCommand( string address, bool[] value, byte plcNumber )
		{
			var analysis = MelsecHelper.McA1EAnalysisAddress( address );
			if (!analysis.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( analysis );

			byte[] buffer = MelsecHelper.TransBoolArrayToByteData( value );
			byte[] _PLCCommand = new byte[12 + buffer.Length];
			_PLCCommand[ 0] = 02;                                                 // 副标题，位单位成批写入
			_PLCCommand[ 1] = plcNumber;                                          // PLC号
			_PLCCommand[ 2] = 0x0A;                                               // CPU监视定时器（L）这里设置为0x00,0x0A，等待CPU返回的时间为10*250ms=2.5秒
			_PLCCommand[ 3] = 0x00;                                               // CPU监视定时器（H）
			_PLCCommand[ 4] = BitConverter.GetBytes( analysis.Content2 )[0];      // 起始软元件（开始读取的地址）
			_PLCCommand[ 5] = BitConverter.GetBytes( analysis.Content2 )[1];
			_PLCCommand[ 6] = BitConverter.GetBytes( analysis.Content2 )[2];
			_PLCCommand[ 7] = BitConverter.GetBytes( analysis.Content2 )[3];
			_PLCCommand[ 8] = BitConverter.GetBytes( analysis.Content1.DataCode )[0];    // 软元件代码（L）
			_PLCCommand[ 9] = BitConverter.GetBytes( analysis.Content1.DataCode )[1];    // 软元件代码（H）
			_PLCCommand[10] = BitConverter.GetBytes( value.Length )[0];           // 软元件点数
			_PLCCommand[11] = BitConverter.GetBytes( value.Length )[1];
			Array.Copy( buffer, 0, _PLCCommand, 12, buffer.Length );              // 将具体的要写入的数据附加到写入命令后面
			return OperateResult.CreateSuccessResult( _PLCCommand );
		}

		/// <summary>
		/// 检测反馈的消息是否合法
		/// </summary>
		/// <param name="response">接收的报文</param>
		/// <returns>是否成功</returns>
		public static OperateResult CheckResponseLegal( byte[] response )
		{
			if (response.Length < 2) return new OperateResult( StringResources.Language.ReceiveDataLengthTooShort );
			if (response[1] == 0) return OperateResult.CreateSuccessResult( );
			if (response[1] == 0x5B) return new OperateResult( response[2], StringResources.Language.MelsecPleaseReferToManualDocument );
			return new OperateResult( response[1], StringResources.Language.MelsecPleaseReferToManualDocument );
		}

		/// <summary>
		/// 从PLC反馈的数据中提取出实际的数据内容，需要传入反馈数据，是否位读取
		/// </summary>
		/// <param name="response">反馈的数据内容</param>
		/// <param name="isBit">是否位读取</param>
		/// <returns>解析后的结果对象</returns>
		public static OperateResult<byte[]> ExtractActualData( byte[] response, bool isBit )
		{
			if (isBit)
			{
				// 位读取
				byte[] Content = new byte[(response.Length - 2) * 2];
				for (int i = 2; i < response.Length; i++)
				{
					if ((response[i] & 0x10) == 0x10) Content[(i - 2) * 2 + 0] = 0x01;
					if ((response[i] & 0x01) == 0x01) Content[(i - 2) * 2 + 1] = 0x01;
				}
				return OperateResult.CreateSuccessResult( Content );
			}
			else
				return OperateResult.CreateSuccessResult( response.RemoveBegin( 2 ) );
		}

		#endregion
	}
}

