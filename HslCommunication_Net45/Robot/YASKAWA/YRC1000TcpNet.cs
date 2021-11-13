using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using HslCommunication.Core;
using HslCommunication.Core.IMessage;
using HslCommunication.Core.Net;
using HslCommunication.Reflection;
#if !NET35 && !NET20
using System.Threading.Tasks;
#endif

namespace HslCommunication.Robot.YASKAWA
{
	/// <summary>
	/// 安川机器人的Ethernet 服务器功能的通讯类<br />
	/// Yaskawa robot's Ethernet server features a communication class
	/// </summary>
	public class YRC1000TcpNet : NetworkDoubleBase, IRobotNet
	{
		#region Constructor

		/// <summary>
		/// 指定机器人的ip地址及端口号来实例化对象<br />
		/// Specify the robot's IP address and port number to instantiate the object
		/// </summary>
		/// <param name="ipAddress">Ip地址</param>
		/// <param name="port">端口号</param>
		public YRC1000TcpNet( string ipAddress, int port )
		{
			IpAddress     = ipAddress;
			Port          = port;
			ByteTransform = new ReverseWordTransform( );
		}

		#endregion

		#region IRobot Interface

		/// <inheritdoc cref="IRobotNet.Read(string)"/>
		[HslMqttApi( ApiTopic = "ReadRobotByte", Description = "Read the robot's original byte data information according to the address" )]
		public OperateResult<byte[]> Read( string address )
		{
			OperateResult<string> read = ReadString( address );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( read );

			return OperateResult.CreateSuccessResult( Encoding.ASCII.GetBytes( read.Content ) );
		}

		/// <inheritdoc cref="IRobotNet.ReadString(string)"/>
		[HslMqttApi( ApiTopic = "ReadRobotString", Description = "Read the string data information of the robot based on the address" )]
		public OperateResult<string> ReadString( string address )
		{
			if (address.IndexOf( '.' ) >= 0 || address.IndexOf( ':' ) >= 0 || address.IndexOf( ';' ) >= 0)
			{
				string[] commands = address.Split( new char[] { '.', ':', ';' } );
				return ReadByCommand( commands[0], commands[1] );
			}
			else
			{
				return ReadByCommand( address, null );
			}
		}

		/// <inheritdoc cref="IRobotNet.Write(string, byte[])"/>
		[HslMqttApi( ApiTopic = "WriteRobotByte", Description = "According to the address, to write the device related bytes data" )]
		public OperateResult Write( string address, byte[] value ) => Write( address, Encoding.ASCII.GetString( value ) );

		/// <inheritdoc cref="IRobotNet.Write(string, string)"/>
		[HslMqttApi( ApiTopic = "WriteRobotString", Description = "According to the address, to write the device related string data" )]
		public OperateResult Write( string address, string value ) => ReadByCommand( address, value );

		#endregion

		#region Async IRobot Interface
#if !NET35 && !NET20
		/// <inheritdoc cref="IRobotNet.ReadAsync(string)"/>
		public async Task<OperateResult<byte[]>> ReadAsync( string address )
		{
			OperateResult<string> read = await ReadStringAsync( address );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( read );

			return OperateResult.CreateSuccessResult( Encoding.ASCII.GetBytes( read.Content ) );
		}

		/// <inheritdoc cref="IRobotNet.ReadStringAsync(string)"/>
		public async Task<OperateResult<string>> ReadStringAsync( string address )
		{
			if (address.IndexOf( '.' ) >= 0 || address.IndexOf( ':' ) >= 0 || address.IndexOf( ';' ) >= 0)
			{
				string[] commands = address.Split( new char[] { '.', ':', ';' } );
				return await ReadByCommandAsync( commands[0], commands[1] );
			}
			else
			{
				return await ReadByCommandAsync( address, null );
			}
		}

		/// <inheritdoc cref="IRobotNet.WriteAsync(string, byte[])"/>
		public async Task<OperateResult> WriteAsync( string address, byte[] value ) => await WriteAsync( address, Encoding.ASCII.GetString( value ) );

		/// <inheritdoc cref="IRobotNet.WriteAsync(string, string)"/>
		public async Task<OperateResult> WriteAsync( string address, string value ) => await ReadByCommandAsync( address, value );
#endif
		#endregion

		#region Initialization Override

		/// <summary>
		/// before read data , the connection should be Initialized
		/// </summary>
		/// <param name="socket">connected socket</param>
		/// <returns>whether is the Initialization is success.</returns>
		protected override OperateResult InitializationOnConnect( Socket socket )
		{
			var read = ReadFromCoreServer( socket, "CONNECT Robot_access KeepAlive:-1\r\n" );
			if (!read.IsSuccess) return read;

			if (read.Content == "OK:YR Information Server(Ver) Keep-Alive:-1.\r\n") return OperateResult.CreateSuccessResult();

			// 检查命令是否返回成功的状态
			if (!read.Content.StartsWith("OK:")) return new OperateResult(read.Content);

			// 不是长连接模式
			isPersistentConn = false;
			return OperateResult.CreateSuccessResult( );
		}
#if !NET35 && !NET20
		/// <summary>
		/// before read data , the connection should be Initialized
		/// </summary>
		/// <param name="socket">connected socket</param>
		/// <returns>whether is the Initialization is success.</returns>
		protected override async Task<OperateResult> InitializationOnConnectAsync( Socket socket )
		{
			var read = await ReadFromCoreServerAsync( socket, "CONNECT Robot_access KeepAlive:-1\r\n" );
			if (!read.IsSuccess) return read;

			if (read.Content == "OK:YR Information Server(Ver) Keep-Alive:-1.\r\n") return OperateResult.CreateSuccessResult( );

			// 检查命令是否返回成功的状态
			if (!read.Content.StartsWith( "OK:" )) return new OperateResult( read.Content );

			// 不是长连接模式
			isPersistentConn = false;
			return OperateResult.CreateSuccessResult( );
		}
#endif
		#endregion

		#region Override Read

		/// <inheritdoc/>
		public override OperateResult<byte[]> ReadFromCoreServer( Socket socket, byte[] send, bool hasResponseData = true, bool usePackHeader = true )
		{
			LogNet?.WriteDebug( ToString( ), StringResources.Language.Send + " : " + BasicFramework.SoftBasic.ByteToHexString( send, ' ' ) );

			// send
			OperateResult sendResult = Send( socket, send );
			if (!sendResult.IsSuccess)
			{
				socket?.Close( );
				return OperateResult.CreateFailedResult<byte[]>( sendResult );
			}

			if (ReceiveTimeOut < 0) return OperateResult.CreateSuccessResult( new byte[0] );

			// receive msg
			OperateResult<byte[]> resultReceive = ReceiveCommandLineFromSocket( socket, (byte)'\r', (byte)'\n', ReceiveTimeOut );
			if (!resultReceive.IsSuccess) return new OperateResult<byte[]>( StringResources.Language.ReceiveDataTimeout + ReceiveTimeOut );

			LogNet?.WriteDebug( ToString( ), StringResources.Language.Receive + " : " + BasicFramework.SoftBasic.ByteToHexString( resultReceive.Content, ' ' ) );

			// Success
			return OperateResult.CreateSuccessResult( resultReceive.Content );
		}

		/// <summary>
		/// Read string value from socket
		/// </summary>
		/// <param name="socket">connected socket</param>
		/// <param name="send">string value</param>
		/// <returns>received string value with is successfully</returns>
		protected OperateResult<string> ReadFromCoreServer( Socket socket, string send )
		{
			var read = ReadFromCoreServer( socket, Encoding.Default.GetBytes( send ) );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<string>( read );

			return OperateResult.CreateSuccessResult( Encoding.Default.GetString( read.Content ) );
		}

		/// <summary>
		/// 根据指令来读取设备的信息，如果命令数据为空，则传入null即可，注意，所有的命令不带换行符<br />
		/// Read the device information according to the instructions. If the command data is empty, pass in null. Note that all commands do not have a newline character
		/// </summary>
		/// <param name="command">命令的内容</param>
		/// <param name="commandData">命令数据内容</param>
		/// <returns>最终的结果内容，需要对IsSuccess进行验证</returns>
		[HslMqttApi( Description = "Read the device information according to the instructions. If the command data is empty, pass in null. Note that all commands do not have a newline character" )]
		public OperateResult<string> ReadByCommand( string command, string commandData )
		{
			InteractiveLock.Enter( );

			// 获取有用的网络通道，如果没有，就建立新的连接
			OperateResult<Socket> resultSocket = GetAvailableSocket( );
			if (!resultSocket.IsSuccess)
			{
				IsSocketError = true;
				AlienSession?.Offline( );
				InteractiveLock.Leave( );
				return OperateResult.CreateFailedResult<string>( resultSocket );
			}

			// 先发送命令
			string sendCommand = string.IsNullOrEmpty( commandData ) ? $"HOSTCTRL_REQUEST {command} 0\r\n" : $"HOSTCTRL_REQUEST {command} {commandData.Length+1}\r\n";
			OperateResult<string> readCommand = ReadFromCoreServer( resultSocket.Content, sendCommand );
			if (!readCommand.IsSuccess)
			{
				IsSocketError = true;
				AlienSession?.Offline( );
				InteractiveLock.Leave( );
				return OperateResult.CreateFailedResult<string>( readCommand );
			}

			// 检查命令是否返回成功的状态
			if (!readCommand.Content.StartsWith( "OK:" ))
			{
				if (!isPersistentConn) resultSocket.Content?.Close( );
				InteractiveLock.Leave( );
				return new OperateResult<string>( readCommand.Content.Remove( readCommand.Content.Length - 2 ) );
			}

			// 在必要的情况下发送命令数据
			if(!string.IsNullOrEmpty( commandData ))
			{
				byte[] send2 = Encoding.ASCII.GetBytes( $"{commandData}\r" );
				LogNet?.WriteDebug( ToString( ), StringResources.Language.Send + " : " + BasicFramework.SoftBasic.ByteToHexString( send2, ' ' ) );

				OperateResult sendResult2 = Send( resultSocket.Content, send2 );
				if (!sendResult2.IsSuccess)
				{
					resultSocket.Content?.Close( );
					IsSocketError = true;
					AlienSession?.Offline( );
					InteractiveLock.Leave( );
					return OperateResult.CreateFailedResult<string>( sendResult2 );
				}
			}

			// 接收数据信息，先接收到\r为止，再根据实际情况决定是否接收\r
			OperateResult<byte[]> resultReceive2 = ReceiveCommandLineFromSocket( resultSocket.Content, (byte)'\r', ReceiveTimeOut);
			if (!resultReceive2.IsSuccess)
			{
				IsSocketError = true;
				AlienSession?.Offline( );
				InteractiveLock.Leave( );
				return OperateResult.CreateFailedResult<string>( resultReceive2 );
			}

			string commandDataReturn = Encoding.ASCII.GetString( resultReceive2.Content );
			if (commandDataReturn.StartsWith( "ERROR:" ))
			{
				if (!isPersistentConn) resultSocket.Content?.Close( );
				InteractiveLock.Leave( );
				Receive( resultSocket.Content, 1 );

				return new OperateResult<string>( commandDataReturn );
			}
			else if (commandDataReturn.StartsWith( "0000\r" ))
			{
				if (!isPersistentConn) resultSocket.Content?.Close( );
				Receive( resultSocket.Content, 1 );

				InteractiveLock.Leave( );
				return OperateResult.CreateSuccessResult( "0000" );
			}
			else
			{
				if (!isPersistentConn) resultSocket.Content?.Close( );

				InteractiveLock.Leave( );
				return OperateResult.CreateSuccessResult( commandDataReturn.Remove( commandDataReturn.Length - 1 ) );
			}
		}

		#endregion

		#region Async Override Read
#if !NET35 && !NET20
		/// <inheritdoc/>
		public async override Task<OperateResult<byte[]>> ReadFromCoreServerAsync( Socket socket, byte[] send, bool hasResponseData = true, bool usePackHeader = true )
		{
			LogNet?.WriteDebug( ToString( ), StringResources.Language.Send + " : " + BasicFramework.SoftBasic.ByteToHexString( send, ' ' ) );

			// send
			OperateResult sendResult = await SendAsync( socket, send );
			if (!sendResult.IsSuccess)
			{
				socket?.Close( );
				return OperateResult.CreateFailedResult<byte[]>( sendResult );
			}

			if (ReceiveTimeOut < 0) return OperateResult.CreateSuccessResult( new byte[0] );

			// receive msg
			OperateResult<byte[]> resultReceive = await ReceiveCommandLineFromSocketAsync( socket, (byte)'\r', (byte)'\n', ReceiveTimeOut );
			if (!resultReceive.IsSuccess) return new OperateResult<byte[]>( StringResources.Language.ReceiveDataTimeout + ReceiveTimeOut );

			LogNet?.WriteDebug( ToString( ), StringResources.Language.Receive + " : " + BasicFramework.SoftBasic.ByteToHexString( resultReceive.Content, ' ' ) );

			// Success
			return OperateResult.CreateSuccessResult( resultReceive.Content );
		}

		/// <inheritdoc cref="ReadFromCoreServer(Socket, string)"/>
		protected async Task<OperateResult<string>> ReadFromCoreServerAsync( Socket socket, string send )
		{
			var read = await ReadFromCoreServerAsync( socket, Encoding.Default.GetBytes( send ) );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<string>( read );

			return OperateResult.CreateSuccessResult( Encoding.Default.GetString( read.Content ) );
		}

		/// <inheritdoc cref="ReadByCommand(string, string)"/>
		public async Task<OperateResult<string>> ReadByCommandAsync( string command, string commandData )
		{
			await Task.Run( new Action( ( ) => InteractiveLock.Enter( ) ) );

			// 获取有用的网络通道，如果没有，就建立新的连接
			OperateResult<Socket> resultSocket = await GetAvailableSocketAsync( );
			if (!resultSocket.IsSuccess)
			{
				IsSocketError = true;
				AlienSession?.Offline( );
				InteractiveLock.Leave( );
				return OperateResult.CreateFailedResult<string>( resultSocket );
			}

			// 先发送命令
			string sendCommand = string.IsNullOrEmpty( commandData ) ? $"HOSTCTRL_REQUEST {command} 0\r\n" : $"HOSTCTRL_REQUEST {command} {commandData.Length + 1}\r\n";
			OperateResult<string> readCommand = await ReadFromCoreServerAsync( resultSocket.Content, sendCommand );
			if (!readCommand.IsSuccess)
			{
				IsSocketError = true;
				AlienSession?.Offline( );
				InteractiveLock.Leave( );
				return OperateResult.CreateFailedResult<string>( readCommand );
			}

			// 检查命令是否返回成功的状态
			if (!readCommand.Content.StartsWith( "OK:" ))
			{
				if (!isPersistentConn) resultSocket.Content?.Close( );
				InteractiveLock.Leave( );
				return new OperateResult<string>( readCommand.Content.Remove( readCommand.Content.Length - 2 ) );
			}

			// 在必要的情况下发送命令数据
			if (!string.IsNullOrEmpty( commandData ))
			{
				byte[] send2 = Encoding.ASCII.GetBytes( $"{commandData}\r" );
				LogNet?.WriteDebug( ToString( ), StringResources.Language.Send + " : " + BasicFramework.SoftBasic.ByteToHexString( send2, ' ' ) );

				OperateResult sendResult2 = await SendAsync( resultSocket.Content, send2 );
				if (!sendResult2.IsSuccess)
				{
					resultSocket.Content?.Close( );
					IsSocketError = true;
					AlienSession?.Offline( );
					InteractiveLock.Leave( );
					return OperateResult.CreateFailedResult<string>( sendResult2 );
				}
			}

			// 接收数据信息，先接收到\r为止，再根据实际情况决定是否接收\r
			OperateResult<byte[]> resultReceive2 = await ReceiveCommandLineFromSocketAsync( resultSocket.Content, (byte)'\r', ReceiveTimeOut );
			if (!resultReceive2.IsSuccess)
			{
				IsSocketError = true;
				AlienSession?.Offline( );
				InteractiveLock.Leave( );
				return OperateResult.CreateFailedResult<string>( resultReceive2 );
			}

			string commandDataReturn = Encoding.ASCII.GetString( resultReceive2.Content );
			if (commandDataReturn.StartsWith( "ERROR:" ))
			{
				if (!isPersistentConn) resultSocket.Content?.Close( );
				InteractiveLock.Leave( );
				await ReceiveAsync( resultSocket.Content, 1 );

				return new OperateResult<string>( commandDataReturn );
			}
			else if (commandDataReturn.StartsWith( "0000\r" ))
			{
				if (!isPersistentConn) resultSocket.Content?.Close( );
				await ReceiveAsync( resultSocket.Content, 1 );

				InteractiveLock.Leave( );
				return OperateResult.CreateSuccessResult( "0000" );
			}
			else
			{
				if (!isPersistentConn) resultSocket.Content?.Close( );

				InteractiveLock.Leave( );
				return OperateResult.CreateSuccessResult( commandDataReturn.Remove( commandDataReturn.Length - 1 ) );
			}
		}
#endif
		#endregion

		#region Public Method

		/// <summary>
		/// 读取机器人的报警信息<br />
		/// Read the alarm information of the robot
		/// </summary>
		/// <returns>原始的报警信息</returns>
		[HslMqttApi( Description = "Read the alarm information of the robot" )]
		public OperateResult<string> ReadRALARM( ) => ReadByCommand( "RALARM", null );

		/// <summary>
		/// 读取机器人的坐标数据信息<br />
		/// Read the coordinate data information of the robot
		/// </summary>
		/// <returns>原始的报警信息</returns>
		[HslMqttApi( Description = "Read the coordinate data information of the robot" )]
		public OperateResult<string> ReadRPOSJ( ) => ReadByCommand( "RPOSJ", null );

		#endregion

		#region Async Public Method
#if !NET35 && !NET20
		/// <inheritdoc cref="ReadRALARM"/>
		public async Task<OperateResult<string>> ReadRALARMAsync( ) => await ReadByCommandAsync( "RALARM", null );

		/// <inheritdoc cref="ReadRPOSJ"/>
		public async Task<OperateResult<string>> ReadRPOSJAsync( ) => await ReadByCommandAsync( "RPOSJ", null );
#endif
		#endregion

		#region Object Override

		/// <inheritdoc/>
		public override string ToString( ) => $"YRC1000TcpNet Robot[{IpAddress}:{Port}]";

		#endregion
	}
}
