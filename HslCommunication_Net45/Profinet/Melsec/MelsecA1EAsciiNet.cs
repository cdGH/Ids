using HslCommunication.Core;
using HslCommunication.Core.IMessage;
using HslCommunication.Core.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HslCommunication.BasicFramework;
using HslCommunication.Reflection;
#if !NET35 && !NET20
using System.Threading.Tasks;
#endif

namespace HslCommunication.Profinet.Melsec
{
	/// <summary>
	/// 三菱PLC通讯协议，采用A兼容1E帧协议实现，使用ASCII码通讯，请根据实际型号来进行选取<br />
	/// Mitsubishi PLC communication protocol, implemented using A compatible 1E frame protocol, using ascii code communication, please choose according to the actual model
	/// </summary>
	/// <remarks>
	/// <inheritdoc cref="MelsecA1ENet" path="remarks"/>
	/// </remarks>
	public class MelsecA1EAsciiNet : NetworkDeviceBase
	{
		#region Constructor

		/// <summary>
		/// 实例化一个默认的对象<br />
		/// Instantiate a default object
		/// </summary>
		public MelsecA1EAsciiNet( )
		{
			WordLength          = 1;
			LogMsgFormatBinary  = false;
			ByteTransform       = new RegularByteTransform( );
		}

		/// <summary>
		/// 指定ip地址和端口来来实例化一个默认的对象<br />
		/// Specify the IP address and port to instantiate a default object
		/// </summary>
		/// <param name="ipAddress">PLC的Ip地址</param>
		/// <param name="port">PLC的端口</param>
		public MelsecA1EAsciiNet( string ipAddress, int port )
		{
			WordLength          = 1;
			IpAddress           = ipAddress;
			Port                = port;
			LogMsgFormatBinary  = false;
			ByteTransform       = new RegularByteTransform( );
		}

		/// <inheritdoc/>
		protected override INetMessage GetNewNetMessage( ) => new MelsecA1EAsciiMessage( );

		#endregion

		#region Public Member

		/// <inheritdoc cref="MelsecA1ENet.PLCNumber"/>
		public byte PLCNumber { get; set; } = 0xFF;

		#endregion

		#region Read Write Support

		/// <inheritdoc cref="MelsecA1ENet.Read(string, ushort)"/>
		[HslMqttApi( "ReadByteArray", "" )]
		public override OperateResult<byte[]> Read( string address, ushort length )
		{
			// 获取指令
			var command = BuildReadCommand( address, length, false, PLCNumber );
			if (!command.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( command );

			// 核心交互
			var read = ReadFromCoreServer( command.Content );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( read );

			// 错误代码验证
			OperateResult check = CheckResponseLegal( read.Content );
			if (!check.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( check );

			// 数据解析，需要传入是否使用位的参数
			return ExtractActualData( read.Content, false );
		}

		/// <inheritdoc cref="MelsecA1ENet.Write(string, byte[])"/>
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
		/// <inheritdoc cref="Read(string, ushort)"/>
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

		/// <inheritdoc cref="Write(string, byte[])"/>
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

		/// <inheritdoc cref="MelsecA1ENet.ReadBool(string, ushort)"/>
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

		/// <inheritdoc cref="MelsecA1ENet.Write(string, bool[])"/>
		[HslMqttApi( "WriteBoolArray", "" )]
		public override OperateResult Write( string address, bool[] values )
		{
			// 解析指令
			OperateResult<byte[]> command = BuildWriteBoolCommand( address, values, PLCNumber );
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
		public static OperateResult<byte[]> BuildReadCommand( string address, ushort length, bool isBit, byte plcNumber )
		{
			var analysis = MelsecHelper.McA1EAnalysisAddress( address );
			if (!analysis.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( analysis );

			// 默认信息----注意：高低字节交错
			// byte subtitle = analysis.Content1.DataType == 0x01 ? (byte)0x00 : (byte)0x01;
			byte subtitle = isBit ? (byte)0x00 : (byte)0x01;

			byte[] _PLCCommand = new byte[24];
			_PLCCommand[ 0] = SoftBasic.BuildAsciiBytesFrom( subtitle )[0];                // 副标题
			_PLCCommand[ 1] = SoftBasic.BuildAsciiBytesFrom( subtitle )[1];
			_PLCCommand[ 2] = SoftBasic.BuildAsciiBytesFrom( plcNumber )[0];               // PLC号
			_PLCCommand[ 3] = SoftBasic.BuildAsciiBytesFrom( plcNumber )[1];
			_PLCCommand[ 4] = 0x30;                                                        // 监视定时器，10*250ms=2.5秒
			_PLCCommand[ 5] = 0x30;
			_PLCCommand[ 6] = 0x30;
			_PLCCommand[ 7] = 0x41;
			_PLCCommand[ 8] = SoftBasic.BuildAsciiBytesFrom( BitConverter.GetBytes( analysis.Content1.DataCode )[1] )[0];
			_PLCCommand[ 9] = SoftBasic.BuildAsciiBytesFrom( BitConverter.GetBytes( analysis.Content1.DataCode )[1] )[1];
			_PLCCommand[10] = SoftBasic.BuildAsciiBytesFrom( BitConverter.GetBytes( analysis.Content1.DataCode )[0] )[0];
			_PLCCommand[11] = SoftBasic.BuildAsciiBytesFrom( BitConverter.GetBytes( analysis.Content1.DataCode )[0] )[1];
			_PLCCommand[12] = SoftBasic.BuildAsciiBytesFrom( BitConverter.GetBytes( analysis.Content2 )[3] )[0];
			_PLCCommand[13] = SoftBasic.BuildAsciiBytesFrom( BitConverter.GetBytes( analysis.Content2 )[3] )[1];
			_PLCCommand[14] = SoftBasic.BuildAsciiBytesFrom( BitConverter.GetBytes( analysis.Content2 )[2] )[0];
			_PLCCommand[15] = SoftBasic.BuildAsciiBytesFrom( BitConverter.GetBytes( analysis.Content2 )[2] )[1];
			_PLCCommand[16] = SoftBasic.BuildAsciiBytesFrom( BitConverter.GetBytes( analysis.Content2 )[1] )[0];
			_PLCCommand[17] = SoftBasic.BuildAsciiBytesFrom( BitConverter.GetBytes( analysis.Content2 )[1] )[1];
			_PLCCommand[18] = SoftBasic.BuildAsciiBytesFrom( BitConverter.GetBytes( analysis.Content2 )[0] )[0];
			_PLCCommand[19] = SoftBasic.BuildAsciiBytesFrom( BitConverter.GetBytes( analysis.Content2 )[0] )[1];
			_PLCCommand[20] = SoftBasic.BuildAsciiBytesFrom( BitConverter.GetBytes( length % 256 )[0] )[0];
			_PLCCommand[21] = SoftBasic.BuildAsciiBytesFrom( BitConverter.GetBytes( length % 256 )[0] )[1];
			_PLCCommand[22] = 0x30;
			_PLCCommand[23] = 0x30;
			return OperateResult.CreateSuccessResult( _PLCCommand );
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

			value = MelsecHelper.TransByteArrayToAsciiByteArray( value );
			byte[] _PLCCommand = new byte[24 + value.Length];
			_PLCCommand[ 0] = 0x30;                                                       // 副标题
			_PLCCommand[ 1] = 0x33;
			_PLCCommand[ 2] = SoftBasic.BuildAsciiBytesFrom( plcNumber )[0];               // PLC号
			_PLCCommand[ 3] = SoftBasic.BuildAsciiBytesFrom( plcNumber )[1];
			_PLCCommand[ 4] = 0x30;                                                        // 监视定时器，10*250ms=2.5秒
			_PLCCommand[ 5] = 0x30;
			_PLCCommand[ 6] = 0x30;
			_PLCCommand[ 7] = 0x41;
			_PLCCommand[ 8] = SoftBasic.BuildAsciiBytesFrom( BitConverter.GetBytes( analysis.Content1.DataCode )[1] )[0];
			_PLCCommand[ 9] = SoftBasic.BuildAsciiBytesFrom( BitConverter.GetBytes( analysis.Content1.DataCode )[1] )[1];
			_PLCCommand[10] = SoftBasic.BuildAsciiBytesFrom( BitConverter.GetBytes( analysis.Content1.DataCode )[0] )[0];
			_PLCCommand[11] = SoftBasic.BuildAsciiBytesFrom( BitConverter.GetBytes( analysis.Content1.DataCode )[0] )[1];
			_PLCCommand[12] = SoftBasic.BuildAsciiBytesFrom( BitConverter.GetBytes( analysis.Content2 )[3] )[0];
			_PLCCommand[13] = SoftBasic.BuildAsciiBytesFrom( BitConverter.GetBytes( analysis.Content2 )[3] )[1];
			_PLCCommand[14] = SoftBasic.BuildAsciiBytesFrom( BitConverter.GetBytes( analysis.Content2 )[2] )[0];
			_PLCCommand[15] = SoftBasic.BuildAsciiBytesFrom( BitConverter.GetBytes( analysis.Content2 )[2] )[1];
			_PLCCommand[16] = SoftBasic.BuildAsciiBytesFrom( BitConverter.GetBytes( analysis.Content2 )[1] )[0];
			_PLCCommand[17] = SoftBasic.BuildAsciiBytesFrom( BitConverter.GetBytes( analysis.Content2 )[1] )[1];
			_PLCCommand[18] = SoftBasic.BuildAsciiBytesFrom( BitConverter.GetBytes( analysis.Content2 )[0] )[0];
			_PLCCommand[19] = SoftBasic.BuildAsciiBytesFrom( BitConverter.GetBytes( analysis.Content2 )[0] )[1];
			_PLCCommand[20] = SoftBasic.BuildAsciiBytesFrom( BitConverter.GetBytes( value.Length / 4 )[0] )[0];
			_PLCCommand[21] = SoftBasic.BuildAsciiBytesFrom( BitConverter.GetBytes( value.Length / 4 )[0] )[1];
			_PLCCommand[22] = 0x30;
			_PLCCommand[23] = 0x30;
			value.CopyTo( _PLCCommand, 24 );
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

			byte[] buffer = value.Select( m => m ? (byte)0x31 : (byte)0x30 ).ToArray( );
			if (buffer.Length % 2 == 1) buffer = SoftBasic.SpliceArray<byte>( buffer, new byte[] { 0x30 } );

			byte[] _PLCCommand = new byte[24 + buffer.Length];
			_PLCCommand[ 0] = 0x30;                                                       // 副标题
			_PLCCommand[ 1] = 0x32;
			_PLCCommand[ 2] = SoftBasic.BuildAsciiBytesFrom( plcNumber )[0];               // PLC号
			_PLCCommand[ 3] = SoftBasic.BuildAsciiBytesFrom( plcNumber )[1];
			_PLCCommand[ 4] = 0x30;                                                        // 监视定时器，10*250ms=2.5秒
			_PLCCommand[ 5] = 0x30;
			_PLCCommand[ 6] = 0x30;
			_PLCCommand[ 7] = 0x41;
			_PLCCommand[ 8] = SoftBasic.BuildAsciiBytesFrom( BitConverter.GetBytes( analysis.Content1.DataCode )[1] )[0];
			_PLCCommand[ 9] = SoftBasic.BuildAsciiBytesFrom( BitConverter.GetBytes( analysis.Content1.DataCode )[1] )[1];
			_PLCCommand[10] = SoftBasic.BuildAsciiBytesFrom( BitConverter.GetBytes( analysis.Content1.DataCode )[0] )[0];
			_PLCCommand[11] = SoftBasic.BuildAsciiBytesFrom( BitConverter.GetBytes( analysis.Content1.DataCode )[0] )[1];
			_PLCCommand[12] = SoftBasic.BuildAsciiBytesFrom( BitConverter.GetBytes( analysis.Content2 )[3] )[0];
			_PLCCommand[13] = SoftBasic.BuildAsciiBytesFrom( BitConverter.GetBytes( analysis.Content2 )[3] )[1];
			_PLCCommand[14] = SoftBasic.BuildAsciiBytesFrom( BitConverter.GetBytes( analysis.Content2 )[2] )[0];
			_PLCCommand[15] = SoftBasic.BuildAsciiBytesFrom( BitConverter.GetBytes( analysis.Content2 )[2] )[1];
			_PLCCommand[16] = SoftBasic.BuildAsciiBytesFrom( BitConverter.GetBytes( analysis.Content2 )[1] )[0];
			_PLCCommand[17] = SoftBasic.BuildAsciiBytesFrom( BitConverter.GetBytes( analysis.Content2 )[1] )[1];
			_PLCCommand[18] = SoftBasic.BuildAsciiBytesFrom( BitConverter.GetBytes( analysis.Content2 )[0] )[0];
			_PLCCommand[19] = SoftBasic.BuildAsciiBytesFrom( BitConverter.GetBytes( analysis.Content2 )[0] )[1];
			_PLCCommand[20] = SoftBasic.BuildAsciiBytesFrom( BitConverter.GetBytes( value.Length )[0] )[0];
			_PLCCommand[21] = SoftBasic.BuildAsciiBytesFrom( BitConverter.GetBytes( value.Length )[0] )[1];
			_PLCCommand[22] = 0x30;
			_PLCCommand[23] = 0x30;
			buffer.CopyTo( _PLCCommand, 24 );
			return OperateResult.CreateSuccessResult( _PLCCommand );
		}

		/// <summary>
		/// 检测反馈的消息是否合法
		/// </summary>
		/// <param name="response">接收的报文</param>
		/// <returns>是否成功</returns>
		public static OperateResult CheckResponseLegal( byte[] response )
		{
			if (response.Length < 4) return new OperateResult( StringResources.Language.ReceiveDataLengthTooShort );
			if (response[2] == 0x30 && response[3] == 0x30) return OperateResult.CreateSuccessResult( );
			if (response[2] == 0x35 && response[3] == 0x42) return new OperateResult( Convert.ToInt32( Encoding.ASCII.GetString( response, 4, 2 ), 16 ), StringResources.Language.MelsecPleaseReferToManualDocument );
			return new OperateResult( Convert.ToInt32( Encoding.ASCII.GetString( response, 2, 2 ), 16 ), StringResources.Language.MelsecPleaseReferToManualDocument );
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
				return OperateResult.CreateSuccessResult( response.RemoveBegin( 4 ).Select( m => m == 0x30 ? (byte)0x00 : (byte)0x01 ).ToArray( ) );
			else
				return OperateResult.CreateSuccessResult( MelsecHelper.TransAsciiByteArrayToByteArray( response.RemoveBegin( 4 ) ) );
		}

		#endregion
	}
}
