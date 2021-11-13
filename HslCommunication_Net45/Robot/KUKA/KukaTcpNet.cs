using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HslCommunication.Core.Net;
using HslCommunication.Core;
using System.Net.Sockets;
using HslCommunication.BasicFramework;
using HslCommunication.Profinet.Panasonic;
using HslCommunication.Reflection;
#if !NET35 && !NET20
using System.Threading.Tasks;
#endif

namespace HslCommunication.Robot.KUKA
{
	/// <summary>
	/// Kuka机器人的数据交互类，通讯支持的条件为KUKA 的 TCP通讯
	/// </summary>
	public class KukaTcpNet : NetworkDoubleBase, IRobotNet
	{
		#region Constructor

		/// <summary>
		/// 实例化一个默认的对象<br />
		/// Instantiate a default object
		/// </summary>
		public KukaTcpNet( )
		{
			ByteTransform = new ReverseWordTransform( );
			LogMsgFormatBinary = false;
		}

		/// <summary>
		/// 实例化一个默认的Kuka机器人对象，并指定IP地址和端口号，端口号通常为9999<br />
		/// Instantiate a default Kuka robot object and specify the IP address and port number, usually 9999
		/// </summary>
		/// <param name="ipAddress">Ip地址</param>
		/// <param name="port">端口号</param>
		public KukaTcpNet( string ipAddress, int port )
		{
			IpAddress = ipAddress;
			Port = port;

			ByteTransform = new ReverseWordTransform( );
			LogMsgFormatBinary = false;
		}

		/// <inheritdoc/>
		public override OperateResult<byte[]> ReadFromCoreServer( Socket socket, byte[] send, bool hasResponseData = true, bool usePackHeader = true )
		{
			LogNet?.WriteDebug( ToString( ), StringResources.Language.Send + " : " + (LogMsgFormatBinary ? send.ToHexString( ' ' ) : Encoding.ASCII.GetString( send )) );

			// send
			OperateResult sendResult = Send( socket, send );
			if (!sendResult.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( sendResult );

			if (receiveTimeOut < 0) return OperateResult.CreateSuccessResult( new byte[0] );

			// receive msg
			OperateResult<byte[]> resultReceive = Receive( socket, -1, receiveTimeOut );
			if (!resultReceive.IsSuccess) return resultReceive;

			LogNet?.WriteDebug( ToString( ), StringResources.Language.Receive + " : " + (LogMsgFormatBinary ? resultReceive.Content.ToHexString( ' ' ) : Encoding.ASCII.GetString( resultReceive.Content )) );

			// Success
			return OperateResult.CreateSuccessResult( resultReceive.Content );
		}
#if !NET35 && !NET20
		/// <inheritdoc/>
		public async override Task<OperateResult<byte[]>> ReadFromCoreServerAsync( Socket socket, byte[] send, bool hasResponseData = true, bool usePackHeader = true )
		{
			byte[] sendValue = usePackHeader ? PackCommandWithHeader( send ) : send;
			LogNet?.WriteDebug( ToString( ), StringResources.Language.Send + " : " + (LogMsgFormatBinary ? sendValue.ToHexString( ' ' ) : Encoding.ASCII.GetString( sendValue )) );

			// send
			OperateResult sendResult = await SendAsync( socket, sendValue );
			if (!sendResult.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( sendResult );

			if (receiveTimeOut < 0) return OperateResult.CreateSuccessResult( new byte[0] );

			// receive msg
			OperateResult<byte[]> resultReceive = await ReceiveAsync( socket, -1, receiveTimeOut );
			if (!resultReceive.IsSuccess) return resultReceive;

			LogNet?.WriteDebug( ToString( ), StringResources.Language.Receive + " : " + (LogMsgFormatBinary ? resultReceive.Content.ToHexString( ' ' ) : Encoding.ASCII.GetString( resultReceive.Content )) );

			// extra check
			return UnpackResponseContent( sendValue, resultReceive.Content );
		}
#endif
		#endregion

		#region IRobotNet Support

		/// <summary>
		/// 读取Kuka机器人的数据内容，根据输入的变量名称来读取<br />
		/// Read the data content of the Kuka robot according to the input variable name
		/// </summary>
		/// <param name="address">地址数据</param>
		/// <returns>带有成功标识的byte[]数组</returns>
		[HslMqttApi( ApiTopic = "ReadRobotByte", Description = "Read the data content of the Kuka robot according to the input variable name" )]
		public OperateResult<byte[]> Read( string address ) => ByteTransformHelper.GetResultFromOther(
			ReadFromCoreServer( Encoding.UTF8.GetBytes( BuildReadCommands( address ) ) ), ExtractActualData );

		/// <summary>
		/// 读取Kuka机器人的所有的数据信息，返回字符串信息，解码方式为UTF8，需要指定变量名称<br />
		/// Read all the data information of the Kuka robot, return the string information, decode by ANSI, need to specify the variable name
		/// </summary>
		/// <param name="address">地址信息</param>
		/// <returns>带有成功标识的字符串数据</returns>
		[HslMqttApi( ApiTopic = "ReadRobotString", Description = "Read all the data information of the Kuka robot, return the string information, decode by ANSI, need to specify the variable name" )]
		public OperateResult<string> ReadString( string address ) => ByteTransformHelper.GetSuccessResultFromOther(
			Read( address ), Encoding.Default.GetString );

		/// <summary>
		/// 根据Kuka机器人的变量名称，写入原始的数据内容<br />
		/// Write the original data content according to the variable name of the Kuka robot
		/// </summary>
		/// <param name="address">变量名称</param>
		/// <param name="value">原始的字节数据信息</param>
		/// <returns>是否成功的写入</returns>
		[HslMqttApi( ApiTopic = "WriteRobotByte", Description = "Write the original data content according to the variable name of the Kuka robot" )]
		public OperateResult Write( string address, byte[] value ) => Write( address, Encoding.Default.GetString( value ) );

		/// <summary>
		/// 根据Kuka机器人的变量名称，写入UTF8编码的字符串数据信息<br />
		/// Writes ansi-encoded string data information based on the variable name of the Kuka robot
		/// </summary>
		/// <param name="address">变量名称</param>
		/// <param name="value">ANSI编码的字符串</param>
		/// <returns>是否成功的写入</returns>
		[HslMqttApi( ApiTopic = "WriteRobotString", Description = "Writes ansi-encoded string data information based on the variable name of the Kuka robot" )]
		public OperateResult Write( string address, string value ) => Write( new string[] { address }, new string[] { value } );

		/// <summary>
		/// 根据Kuka机器人的变量名称，写入多个UTF8编码的字符串数据信息<br />
		/// Write multiple UTF8 encoded string data information according to the variable name of the Kuka robot
		/// </summary>
		/// <param name="address">变量名称</param>
		/// <param name="value">ANSI编码的字符串</param>
		/// <returns>是否成功的写入</returns>
		[HslMqttApi( ApiTopic = "WriteRobotStrings", Description = "Write multiple UTF8 encoded string data information according to the variable name of the Kuka robot" )]
		public OperateResult Write( string[] address, string[] value ) => ReadCmd( BuildWriteCommands( address, value ) );

		private OperateResult ReadCmd( string cmd )
		{
			OperateResult<byte[]> write = ReadFromCoreServer( Encoding.UTF8.GetBytes( cmd ) );
			if (!write.IsSuccess) return write;

			string msg = Encoding.UTF8.GetString( write.Content );
			if (msg.Contains( "err" ))
				return new OperateResult( "Result contains err: " + msg );

			return OperateResult.CreateSuccessResult( );
		}

		/// <summary>
		/// 启动机器人的指定的程序<br />
		/// Start the specified program of the robot
		/// </summary>
		/// <param name="program">程序的名字</param>
		/// <returns>是否启动成功</returns>
		[HslMqttApi( Description = "Start the specified program of the robot" )]
		public OperateResult StartProgram( string program ) => ReadCmd( "03" + program );

		/// <summary>
		/// 复位当前的程序<br />
		/// Reset current program
		/// </summary>
		/// <returns>复位结果</returns>
		[HslMqttApi( Description = "Reset current program" )]
		public OperateResult ResetProgram( ) => ReadCmd( "0601" );

		/// <summary>
		/// 停止当前的程序<br />
		/// Stop current program
		/// </summary>
		/// <returns>复位结果</returns>
		[HslMqttApi( Description = "Stop current program" )]
		public OperateResult StopProgram( ) => ReadCmd( "0621" );

		#endregion

		#region Async IRobotNet Support
#if !NET35 && !NET20
		/// <inheritdoc cref="Read(string)"/>
		public async Task<OperateResult<byte[]>> ReadAsync( string address ) => ByteTransformHelper.GetResultFromOther(
			await ReadFromCoreServerAsync( Encoding.UTF8.GetBytes( BuildReadCommands( address ) ) ), ExtractActualData );

		/// <inheritdoc cref="ReadString(string)"/>
		public async Task<OperateResult<string>> ReadStringAsync( string address ) => ByteTransformHelper.GetSuccessResultFromOther(
			await ReadAsync( address ), Encoding.Default.GetString );

		/// <inheritdoc cref="Write(string, byte[])"/>
		public async Task<OperateResult> WriteAsync( string address, byte[] value ) => await WriteAsync( address, Encoding.Default.GetString( value ) );

		/// <inheritdoc cref="Write(string, string)"/>
		public async Task<OperateResult> WriteAsync( string address, string value ) => await WriteAsync( new string[] { address }, new string[] { value } );

		/// <inheritdoc cref="Write(string[], string[])"/>
		public async Task<OperateResult> WriteAsync( string[] address, string[] value ) => await ReadCmdAsync( BuildWriteCommands( address, value ) );

		private async Task<OperateResult> ReadCmdAsync( string cmd )
		{
			OperateResult<byte[]> write = await ReadFromCoreServerAsync( Encoding.UTF8.GetBytes( cmd ) );
			if (!write.IsSuccess) return write;

			string msg = Encoding.UTF8.GetString( write.Content );
			if (msg.Contains( "err" ))
				return new OperateResult( "Result contains err: " + msg );

			return OperateResult.CreateSuccessResult( );
		}

		/// <inheritdoc cref="StartProgram(string)"/>
		public async Task<OperateResult> StartProgramAsync( string program ) => await ReadCmdAsync( "03" + program );

		/// <inheritdoc cref="ResetProgram"/>
		public async Task<OperateResult> ResetProgramAsync( ) => await ReadCmdAsync( "0601" );

		/// <inheritdoc cref="StopProgram"/>
		public async Task<OperateResult> StopProgramAsync( ) => await ReadCmdAsync( "0621" );
#endif
		#endregion

		#region Command Build

		private OperateResult<byte[]> ExtractActualData( byte[] response )
		{
			return OperateResult.CreateSuccessResult( response );
		}

		#endregion

		#region Object Override

		/// <inheritdoc/>
		public override string ToString( ) => $"KukaTcpNet[{IpAddress}:{Port}]";

		#endregion

		#region Static Method

		/// <summary>
		/// 构建读取变量的报文命令
		/// </summary>
		/// <param name="address">地址信息</param>
		/// <returns>报文内容</returns>
		public static string BuildReadCommands( string[] address )
		{
			if (address == null) return string.Empty;
			StringBuilder sb = new StringBuilder( "00" );

			for (int i = 0; i < address.Length; i++)
			{
				sb.Append( $"{address[i]}" );
				if (i != address.Length - 1) sb.Append( "," );
			}
			return sb.ToString( );
		}

		/// <summary>
		/// 构建读取变量的报文命令
		/// </summary>
		/// <param name="address">地址信息</param>
		/// <returns>报文内容</returns>
		public static string BuildReadCommands( string address )
		{
			return BuildReadCommands( new string[] { address } );
		}

		/// <summary>
		/// 构建写入变量的报文命令
		/// </summary>
		/// <param name="address">地址信息</param>
		/// <param name="values">数据信息</param>
		/// <returns>字符串信息</returns>
		public static string BuildWriteCommands(string[] address, string[] values )
		{
			if (address == null || values == null) return string.Empty;
			if (address.Length != values.Length) throw new Exception( StringResources.Language.TwoParametersLengthIsNotSame );

			StringBuilder sb = new StringBuilder( "01" );

			for (int i = 0; i < address.Length; i++)
			{
				sb.Append( $"{address[i]}=" );
				sb.Append( $"{values[i]}" );
				if (i != address.Length - 1) sb.Append( "," );
			}
			return sb.ToString( );
		}

		/// <summary>
		/// 构建写入变量的报文命令
		/// </summary>
		/// <param name="address">地址信息</param>
		/// <param name="value">数据信息</param>
		/// <returns>字符串信息</returns>
		public static string BuildWriteCommands( string address, string value )
		{
			return BuildWriteCommands( new string[] { address }, new string[] { value } );
		}

		#endregion
	}
}
