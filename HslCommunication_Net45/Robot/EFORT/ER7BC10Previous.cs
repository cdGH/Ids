using HslCommunication.BasicFramework;
using HslCommunication.Core;
using HslCommunication.Core.IMessage;
using HslCommunication.Core.Net;
using HslCommunication.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
#if !NET35 && !NET20
using System.Threading.Tasks;
#endif

namespace HslCommunication.Robot.EFORT
{
	/// <summary>
	/// 埃夫特机器人对应型号为ER7B-C10，此协议为旧版的定制版，报文未对齐的版本<br />
	/// The corresponding model of the efort robot is er7b-c10. This protocol is a customized version of the old version, and the message is not aligned
	/// </summary>
	public class ER7BC10Previous : NetworkDoubleBase, IRobotNet
	{
		#region Constructor

		/// <summary>
		/// 实例化一个默认的对象，并指定IP地址和端口号，端口号通常为8008<br />
		/// Instantiate a default object and specify the IP address and port number, usually 8008
		/// </summary>
		/// <param name="ipAddress">Ip地址</param>
		/// <param name="port">端口号</param>
		public ER7BC10Previous( string ipAddress, int port )
		{
			IpAddress          = ipAddress;
			Port               = port;
			ByteTransform      = new RegularByteTransform( );
			softIncrementCount = new SoftIncrementCount( ushort.MaxValue );
		}

		/// <inheritdoc/>
		protected override INetMessage GetNewNetMessage( ) => new EFORTMessagePrevious( );

		#endregion

		#region Request Create

		/// <summary>
		/// 获取发送的消息的命令<br />
		/// Gets the command to send the message
		/// </summary>
		/// <returns>字节数组命令</returns>
		public byte[] GetReadCommand( )
		{
			byte[] command = new byte[36];

			Encoding.ASCII.GetBytes( "MessageHead" ).CopyTo( command, 0 );
			BitConverter.GetBytes( (ushort)command.Length ).CopyTo( command, 15 );
			BitConverter.GetBytes( (ushort)1001 ).CopyTo( command, 17 );
			BitConverter.GetBytes( (ushort)softIncrementCount.GetCurrentValue( ) ).CopyTo( command, 19 );
			Encoding.ASCII.GetBytes( "MessageTail" ).CopyTo( command, 21 );

			return command;
		}

		#endregion

		#region IRobotNet Support

		/// <inheritdoc cref="IRobotNet.Read(string)"/>
		[HslMqttApi( ApiTopic = "ReadRobotByte", Description = "Read the robot's original byte data information according to the address")]
		public OperateResult<byte[]> Read( string address ) => ReadFromCoreServer( GetReadCommand( ) );

		/// <inheritdoc cref="IRobotNet.ReadString(string)"/>
		[HslMqttApi( ApiTopic = "ReadRobotString", Description = "Read the string data information of the robot based on the address")]
		public OperateResult<string> ReadString( string address )
		{
			OperateResult<EfortData> read = ReadEfortData( );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<string>( read );

			return OperateResult.CreateSuccessResult( Newtonsoft.Json.JsonConvert.SerializeObject( read.Content, Newtonsoft.Json.Formatting.Indented ) );
		}

		/// <summary>
		/// 本机器人不支持该方法操作，将永远返回失败，无效的操作<br />
		/// This robot does not support this method operation, will always return failed, invalid operation
		/// </summary>
		/// <param name="address">指定的地址信息，有些机器人可能不支持</param>
		/// <param name="value">原始的字节数据信息</param>
		/// <returns>是否成功的写入</returns>
		[HslMqttApi( ApiTopic = "WriteRobotByte", Description = "This robot does not support this method operation, will always return failed, invalid operation")]
		public OperateResult Write( string address, byte[] value ) => new OperateResult( StringResources.Language.NotSupportedFunction );

		/// <summary>
		/// 本机器人不支持该方法操作，将永远返回失败，无效的操作<br />
		/// This robot does not support this method operation, will always return failed, invalid operation
		/// </summary>
		/// <param name="address">指定的地址信息，有些机器人可能不支持</param>
		/// <param name="value">字符串的数据信息</param>
		/// <returns>是否成功的写入</returns>
		[HslMqttApi( ApiTopic = "WriteRobotString", Description = "This robot does not support this method operation, will always return failed, invalid operation")]
		public OperateResult Write( string address, string value ) => new OperateResult( StringResources.Language.NotSupportedFunction );

		/// <summary>
		/// 读取机器人的详细信息<br />
		/// Read the details of the robot
		/// </summary>
		/// <returns>结果数据信息</returns>
		[HslMqttApi( Description = "Read the details of the robot")]
		public OperateResult<EfortData> ReadEfortData( )
		{
			OperateResult<byte[]> read = Read( "" );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<EfortData>( read );

			return EfortData.PraseFromPrevious( read.Content );
		}

		#endregion

		#region Async IRobotNet Support
#if !NET35 && !NET20
		/// <inheritdoc cref="Read(string)"/>
		public async Task<OperateResult<byte[]>> ReadAsync( string address ) => await ReadFromCoreServerAsync( GetReadCommand( ) );

		/// <inheritdoc cref="ReadString(string)"/>
		public async Task<OperateResult<string>> ReadStringAsync( string address )
		{
			OperateResult<EfortData> read = await ReadEfortDataAsync( );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<string>( read );

			return OperateResult.CreateSuccessResult( Newtonsoft.Json.JsonConvert.SerializeObject( read.Content, Newtonsoft.Json.Formatting.Indented ) );
		}

		/// <inheritdoc cref="Write(string, byte[])"/>
#pragma warning disable CS1998 // 异步方法缺少 "await" 运算符，将以同步方式运行
		public async Task<OperateResult> WriteAsync( string address, byte[] value ) => new OperateResult( StringResources.Language.NotSupportedFunction );
#pragma warning restore CS1998 // 异步方法缺少 "await" 运算符，将以同步方式运行

		/// <inheritdoc cref="Write(string, string)"/>
#pragma warning disable CS1998 // 异步方法缺少 "await" 运算符，将以同步方式运行
		public async Task<OperateResult> WriteAsync( string address, string value ) => new OperateResult( StringResources.Language.NotSupportedFunction );
#pragma warning restore CS1998 // 异步方法缺少 "await" 运算符，将以同步方式运行

		/// <inheritdoc cref="ReadEfortData"/>
		public async Task<OperateResult<EfortData>> ReadEfortDataAsync( )
		{
			OperateResult<byte[]> read = await ReadAsync( "" );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<EfortData>( read );

			return EfortData.PraseFromPrevious( read.Content );
		}
#endif
		#endregion

		#region Private Member

		private SoftIncrementCount softIncrementCount;              // 自增消息的对象

		#endregion

		#region Object Override

		/// <inheritdoc/>
		public override string ToString( ) => $"ER7BC10 Pre Robot[{IpAddress}:{Port}]";

		#endregion
	}
}
