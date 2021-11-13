using HslCommunication.BasicFramework;
using HslCommunication.Core;
using HslCommunication.Core.Net;
using HslCommunication.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
#if !NET35 && !NET20
using System.Threading.Tasks;
#endif

namespace HslCommunication.Robot.YAMAHA
{
	/// <summary>
	/// 雅马哈机器人的数据访问类
	/// </summary>
	public class YamahaRCX : NetworkDoubleBase
	{
		#region Constrcutor

		/// <summary>
		/// 实例化一个默认的对象
		/// </summary>
		public YamahaRCX( )
		{
			ByteTransform = new RegularByteTransform( );
			ReceiveTimeOut = 30000;
		}

		/// <summary>
		/// 指定IP地址和端口来实例化一个对象
		/// </summary>
		/// <param name="ipAddress">IP地址</param>
		/// <param name="port">端口号</param>
		public YamahaRCX(string ipAddress, int port )
		{
			ByteTransform = new RegularByteTransform( );
			IpAddress = ipAddress;
			Port = port;
			ReceiveTimeOut = 30000;
		}

		#endregion

		#region Override InitializationOnConnect
		/// <inheritdoc/>
		public override OperateResult<byte[]> ReadFromCoreServer( Socket socket, byte[] send, bool hasResponseData = true, bool usePackHeader = true )
		{
			OperateResult sendResult = Send( socket, send );
			if (!sendResult.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( sendResult );

			// 接收超时时间大于0时才允许接收远程的数据
			if (ReceiveTimeOut < 0) return OperateResult.CreateSuccessResult( new byte[0] );

			// 接收数据信息
			return ReceiveCommandLineFromSocket( socket, 0x0D, 0x0A, 60000 );
		}

		/// <summary>
		/// 发送命令行到socket, 并从机器人读取指定的命令行
		/// </summary>
		/// <param name="send">等待发送的数据</param>
		/// <param name="lines">接收的行数</param>
		/// <returns>结果的结果数据内容</returns>
		public OperateResult<string[]> ReadFromServer(byte[] send, int lines )
		{
			var result = new OperateResult<string[]>( );
			OperateResult<Socket> resultSocket = null;
			InteractiveLock.Enter( );

			try
			{
				// 获取有用的网络通道，如果没有，就建立新的连接
				resultSocket = GetAvailableSocket( );
				if (!resultSocket.IsSuccess)
				{
					IsSocketError = true;
					AlienSession?.Offline( );
					InteractiveLock.Leave( );
					result.CopyErrorFromOther( resultSocket );
					return result;
				}

				List<string> buffers = new List<string>( );
				bool isError = false;
				for (int i = 0; i < lines; i++)
				{
					OperateResult<byte[]> read = ReadFromCoreServer( resultSocket.Content, send );
					if (!read.IsSuccess)
					{
						isError = true;
						IsSocketError = true;
						AlienSession?.Offline( );
						result.CopyErrorFromOther( read );
						break;
					}
					else
					{
						buffers.Add( Encoding.ASCII.GetString( read.Content.RemoveLast( 2 ) ) );
					}
				}
				if (!isError)
				{
					IsSocketError = false;
					result.IsSuccess = true;
					result.Content = buffers.ToArray( );
					result.Message = StringResources.Language.SuccessText;
				}

				ExtraAfterReadFromCoreServer( new OperateResult( ) { IsSuccess = !isError } );
				InteractiveLock.Leave( );
			}
			catch
			{
				InteractiveLock.Leave( );
				throw;
			}

			if (!isPersistentConn) resultSocket?.Content?.Close( );
			return result;
		}
#if !NET35 && !NET20
		/// <inheritdoc/>
		public async override Task<OperateResult<byte[]>> ReadFromCoreServerAsync( Socket socket, byte[] send, bool hasResponseData = true, bool usePackHeader = true )
		{
			OperateResult sendResult = await SendAsync( socket, send );
			if (!sendResult.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( sendResult );

			// 接收超时时间大于0时才允许接收远程的数据
			if (ReceiveTimeOut < 0) return OperateResult.CreateSuccessResult( new byte[0] );

			// 接收数据信息
			return await ReceiveCommandLineFromSocketAsync( socket, 0x0D, 0x0A, 60000 );
		}

		/// <inheritdoc cref="ReadFromServer(byte[], int)"/>
		public async Task<OperateResult<string[]>> ReadFromServerAsync( byte[] send, int lines )
		{
			var result = new OperateResult<string[]>( );
			OperateResult<Socket> resultSocket = null;
			await Task.Run( new Action( ( ) => InteractiveLock.Enter( ) ) );

			try
			{
				// 获取有用的网络通道，如果没有，就建立新的连接
				resultSocket = await GetAvailableSocketAsync( );
				if (!resultSocket.IsSuccess)
				{
					IsSocketError = true;
					AlienSession?.Offline( );
					InteractiveLock.Leave( );
					result.CopyErrorFromOther( resultSocket );
					return result;
				}

				List<string> buffers = new List<string>( );
				bool isError = false;
				for (int i = 0; i < lines; i++)
				{
					OperateResult<byte[]> read = await ReadFromCoreServerAsync( resultSocket.Content, send );
					if (!read.IsSuccess)
					{
						isError = true;
						IsSocketError = true;
						AlienSession?.Offline( );
						result.CopyErrorFromOther( read );
						break;
					}
					else
					{
						buffers.Add( Encoding.ASCII.GetString( read.Content.RemoveLast( 2 ) ) );
					}
				}
				if (!isError)
				{
					IsSocketError = false;
					result.IsSuccess = true;
					result.Content = buffers.ToArray( );
					result.Message = StringResources.Language.SuccessText;
				}

				ExtraAfterReadFromCoreServer( new OperateResult( ) { IsSuccess = !isError } );
				InteractiveLock.Leave( );
			}
			catch
			{
				InteractiveLock.Leave( );
				throw;
			}

			if (!isPersistentConn) resultSocket?.Content?.Close( );
			return result;
		}

		/// <inheritdoc cref="ReadCommand(string, int)"/>
		public async Task<OperateResult<string[]>> ReadCommandAsync( string command, int lines )
		{
			byte[] buffer = SoftBasic.SpliceArray<byte>( Encoding.ASCII.GetBytes( command ), new byte[] { 0x0D, 0x0A } );
			return await ReadFromServerAsync( buffer, lines );
		}

		/// <inheritdoc cref="Reset"/>
		public async Task<OperateResult> ResetAsync( )
		{
			OperateResult<string[]> read = await ReadCommandAsync( "@ RESET ", 1 );
			if (!read.IsSuccess) return read;

			return CheckResponseOk( read.Content[0] );
		}

		/// <inheritdoc cref="Run"/>
		public async Task<OperateResult> RunAsync( )
		{
			OperateResult<string[]> read = await ReadCommandAsync( "@ RUN ", 1 );
			if (!read.IsSuccess) return read;

			return CheckResponseOk( read.Content[0] );
		}

		/// <inheritdoc cref="Stop"/>
		public async Task<OperateResult> StopAsync( )
		{
			OperateResult<string[]> read = await ReadCommandAsync( "@ STOP ", 1 );
			if (!read.IsSuccess) return read;

			return CheckResponseOk( read.Content[0] );
		}

		/// <inheritdoc cref="ReadMotorStatus"/>
		public async Task<OperateResult<int>> ReadMotorStatusAsync( )
		{
			OperateResult<string[]> read = await ReadCommandAsync( "@?MOTOR ", 2 );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<int>( read );

			OperateResult check = CheckResponseOk( read.Content[1] );
			if (!check.IsSuccess) return OperateResult.CreateFailedResult<int>( check );

			return OperateResult.CreateSuccessResult( Convert.ToInt32( read.Content[0] ) );
		}

		/// <inheritdoc cref="ReadModeStatus"/>
		public async Task<OperateResult<int>> ReadModeStatusAsync( )
		{
			OperateResult<string[]> read = await ReadCommandAsync( "@?MODE ", 2 );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<int>( read );

			OperateResult check = CheckResponseOk( read.Content[1] );
			if (!check.IsSuccess) return OperateResult.CreateFailedResult<int>( check );

			return OperateResult.CreateSuccessResult( Convert.ToInt32( read.Content[0] ) );
		}

		/// <inheritdoc cref="ReadJoints"/>
		public async Task<OperateResult<float[]>> ReadJointsAsync( )
		{
			OperateResult<string[]> read = await ReadCommandAsync( "@?WHERE ", 1 );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<float[]>( read );

			return OperateResult.CreateSuccessResult( read.Content[0].Split( new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries ).Select( m => Convert.ToSingle( m ) ).ToArray( ) );
		}

		/// <inheritdoc cref="ReadEmergencyStatus"/>
		public async Task<OperateResult<int>> ReadEmergencyStatusAsync( )
		{
			OperateResult<string[]> read = await ReadCommandAsync( "@?EMG ", 2 );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<int>( read );

			OperateResult check = CheckResponseOk( read.Content[1] );
			if (!check.IsSuccess) return OperateResult.CreateFailedResult<int>( check );

			return OperateResult.CreateSuccessResult( Convert.ToInt32( read.Content[0] ) );
		}
#endif
		#endregion

		/// <summary>
		/// 读取指定的命令的方法，需要指定命令，和接收命令的行数信息<br />
		/// The method of reading the specified command requires the specified command and the line number information of the received command
		/// </summary>
		/// <param name="command">命令</param>
		/// <param name="lines">接收的行数信息</param>
		/// <returns>接收的命令</returns>
		[HslMqttApi( Description = "The method of reading the specified command requires the specified command and the line number information of the received command" )]
		public OperateResult<string[]> ReadCommand(string command, int lines )
		{
			byte[] buffer = SoftBasic.SpliceArray<byte>( Encoding.ASCII.GetBytes( command ), new byte[] { 0x0D, 0x0A } );
			return ReadFromServer( buffer, lines );
		}

		private OperateResult CheckResponseOk( string msg )
		{
			if (msg.StartsWith( "OK" )) return OperateResult.CreateSuccessResult( );
			else return new OperateResult( msg );
		}

		/// <summary>
		/// 指定程序复位信息，对所有的程序进行复位。当重新启动了程序时，从主程序或者任务 1 中最后执行的程序开头开始执行。<br />
		/// Specify the program reset information to reset all programs. When the program is restarted, 
		/// execution starts from the beginning of the main program or the last executed program in task 1.
		/// </summary>
		/// <returns>执行结果是否成功</returns>
		[HslMqttApi( Description = "Specify the program reset information to reset all programs. When the program is restarted" )]
		public OperateResult Reset( )
		{
			OperateResult<string[]> read = ReadCommand( "@ RESET ", 1 );
			if (!read.IsSuccess) return read;

			return CheckResponseOk( read.Content[0] );
		}

		/// <summary>
		/// 执行程序运行。执行所有的 RUN 状态程序。<br />
		/// Execute the program to run. Execute all RUN state programs.
		/// </summary>
		/// <returns>执行结果是否成功</returns>
		[HslMqttApi( Description = "Execute the program to run. Execute all RUN state programs." )]
		public OperateResult Run( )
		{
			OperateResult<string[]> read = ReadCommand( "@ RUN ", 1 );
			if (!read.IsSuccess) return read;

			return CheckResponseOk( read.Content[0] );
		}

		/// <summary>
		/// 执行程序停止。执行所有的 STOP 状态程序。<br />
		/// The execution program stops. Execute all STOP state programs.
		/// </summary>
		/// <returns>执行结果是否成功</returns>
		[HslMqttApi( Description = "The execution program stops. Execute all STOP state programs." )]
		public OperateResult Stop( )
		{
			OperateResult<string[]> read = ReadCommand( "@ STOP ", 1 );
			if (!read.IsSuccess) return read;

			return CheckResponseOk( read.Content[0] );
		}

		/// <summary>
		/// 获取马达电源状态，返回的0:马达电源关闭; 1:马达电源开启; 2:马达电源开启＋所有机器人伺服开启<br />
		/// Get the motor power status, return 0: motor power off; 1: motor power on; 2: motor power on + all robot servos on
		/// </summary>
		/// <returns>返回的0:马达电源关闭; 1:马达电源开启; 2:马达电源开启＋所有机器人伺服开启</returns>
		[HslMqttApi( Description = "Get the motor power status, return 0: motor power off; 1: motor power on; 2: motor power on + all robot servos on" )]
		public OperateResult<int> ReadMotorStatus( )
		{
			OperateResult<string[]> read = ReadCommand( "@?MOTOR ", 2 );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<int>(read);

			OperateResult check = CheckResponseOk( read.Content[1] );
			if (!check.IsSuccess) return OperateResult.CreateFailedResult<int>( check );

			return OperateResult.CreateSuccessResult( Convert.ToInt32( read.Content[0] ) );
		}

		/// <summary>
		/// 读取模式状态<br />
		/// Read mode status
		/// </summary>
		/// <returns>模式的状态信息</returns>
		[HslMqttApi( Description = "Read mode status" )]
		public OperateResult<int> ReadModeStatus( )
		{
			OperateResult<string[]> read = ReadCommand( "@?MODE ", 2 );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<int>( read );

			OperateResult check = CheckResponseOk( read.Content[1] );
			if (!check.IsSuccess) return OperateResult.CreateFailedResult<int>( check );

			return OperateResult.CreateSuccessResult( Convert.ToInt32( read.Content[0] ) );
		}

		/// <summary>
		/// 读取关节的基本数据信息<br />
		/// Read the basic data information of the joint
		/// </summary>
		/// <returns>关节信息</returns>
		[HslMqttApi( Description = "Read the basic data information of the joint" )]
		public OperateResult<float[]> ReadJoints( )
		{
			OperateResult<string[]> read = ReadCommand( "@?WHERE ", 1 );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<float[]>( read );

			return OperateResult.CreateSuccessResult( read.Content[0].Split( new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries ).Select( m => Convert.ToSingle( m ) ).ToArray( ) );
		}

		/// <summary>
		/// 读取紧急停止状态，0 ：正常状态、1 ：紧急停止状态<br />
		/// Read emergency stop state, 0: normal state, 1: emergency stop state
		/// </summary>
		/// <returns>0 ：正常状态、1 ：紧急停止状态</returns>
		[HslMqttApi( Description = "Read emergency stop state, 0: normal state, 1: emergency stop state" )]
		public OperateResult<int> ReadEmergencyStatus( )
		{
			OperateResult<string[]> read = ReadCommand( "@?EMG ", 2 );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<int>( read );

			OperateResult check = CheckResponseOk( read.Content[1] );
			if (!check.IsSuccess) return OperateResult.CreateFailedResult<int>( check );

			return OperateResult.CreateSuccessResult( Convert.ToInt32( read.Content[0] ) );
		}

	}
}
