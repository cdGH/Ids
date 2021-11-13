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

namespace HslCommunication.Robot.KUKA
{
	/// <summary>
	/// Kuka机器人的数据交互类，通讯支持的条件为KUKA 的 KRC4 控制器中运行KUKAVARPROXY 这个第三方软件，端口通常为7000<br />
	/// The data interaction class of Kuka robot is supported by the third-party software KUKAVARPROXY running in the KRC4 controller of Kuka. The port is usually 7000
	/// </summary>
	/// <remarks>
	/// 非常感谢 昆山-LT 网友的测试和意见反馈。<br />
	/// 其中KUKAVARPROXY 这个第三方软件在来源地址：
	/// https://github.com/ImtsSrl/KUKAVARPROXY <br />
	/// 如果想要更详细的了解配置，连接，数据读取操作，请点击下面的连接：http://blog.davidrobot.com/2019/03/hsl_for_kuka.html?tdsourcetag=s_pctim_aiomsg
	/// </remarks>
	public class KukaAvarProxyNet : NetworkDoubleBase, IRobotNet
	{
		#region Constructor

		/// <summary>
		/// 实例化一个默认的对象<br />
		/// Instantiate a default object
		/// </summary>
		public KukaAvarProxyNet( )
		{
			softIncrementCount = new SoftIncrementCount( ushort.MaxValue );
			ByteTransform      = new ReverseWordTransform( );
		}

		/// <summary>
		/// 实例化一个默认的Kuka机器人对象，并指定IP地址和端口号，端口号通常为7000<br />
		/// Instantiate a default Kuka robot object and specify the IP address and port number, usually 7000
		/// </summary>
		/// <param name="ipAddress">Ip地址</param>
		/// <param name="port">端口号</param>
		public KukaAvarProxyNet( string ipAddress, int port )
		{
			IpAddress = ipAddress;
			Port = port;

			softIncrementCount = new SoftIncrementCount( ushort.MaxValue );
			ByteTransform      = new ReverseWordTransform( );
		}

		/// <inheritdoc/>
		protected override INetMessage GetNewNetMessage( ) => new KukaVarProxyMessage( );

		#endregion

		#region IRobotNet Support

		/// <summary>
		/// 读取Kuka机器人的数据内容，根据输入的变量名称来读取<br />
		/// Read the data content of the Kuka robot according to the input variable name
		/// </summary>
		/// <param name="address">地址数据</param>
		/// <returns>带有成功标识的byte[]数组</returns>
		[HslMqttApi( ApiTopic = "ReadRobotByte", Description = "Read the data content of the Kuka robot according to the input variable name" )]
		public OperateResult<byte[]> Read( string address ) => ByteTransformHelper.GetResultFromOther( ReadFromCoreServer( PackCommand( BuildReadValueCommand( address ) ) ), ExtractActualData );

		/// <summary>
		/// 读取Kuka机器人的所有的数据信息，返回字符串信息，解码方式为ANSI，需要指定变量名称<br />
		/// Read all the data information of the Kuka robot, return the string information, decode by ANSI, need to specify the variable name
		/// </summary>
		/// <param name="address">地址信息</param>
		/// <returns>带有成功标识的字符串数据</returns>
		[HslMqttApi( ApiTopic = "ReadRobotString", Description = "Read all the data information of the Kuka robot, return the string information, decode by ANSI, need to specify the variable name" )]
		public OperateResult<string> ReadString( string address ) => ByteTransformHelper.GetSuccessResultFromOther( Read( address ), Encoding.Default.GetString );

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
		/// 根据Kuka机器人的变量名称，写入ANSI编码的字符串数据信息<br />
		/// Writes ansi-encoded string data information based on the variable name of the Kuka robot
		/// </summary>
		/// <param name="address">变量名称</param>
		/// <param name="value">ANSI编码的字符串</param>
		/// <returns>是否成功的写入</returns>
		[HslMqttApi( ApiTopic = "WriteRobotString", Description = "Writes ansi-encoded string data information based on the variable name of the Kuka robot" )]
		public OperateResult Write( string address, string value ) => ByteTransformHelper.GetResultFromOther( ReadFromCoreServer( PackCommand( BuildWriteValueCommand( address, value ) ) ), ExtractActualData );

		#endregion

		#region Async IRobotNet Support
#if !NET35 && !NET20
		/// <inheritdoc cref="Read(string)"/>
		public async Task<OperateResult<byte[]>> ReadAsync( string address ) => ByteTransformHelper.GetResultFromOther( await ReadFromCoreServerAsync( PackCommand( BuildReadValueCommand( address ) ) ), ExtractActualData );

		/// <inheritdoc cref="ReadString(string)"/>
		public async Task<OperateResult<string>> ReadStringAsync( string address ) => ByteTransformHelper.GetSuccessResultFromOther( await ReadAsync( address ), Encoding.Default.GetString );

		/// <inheritdoc cref="Write(string, byte[])"/>
		public async Task<OperateResult> WriteAsync( string address, byte[] value ) => await WriteAsync( address, Encoding.Default.GetString( value ) );

		/// <inheritdoc cref="Write(string, string)"/>
		public async Task<OperateResult> WriteAsync( string address, string value ) => ByteTransformHelper.GetResultFromOther( await ReadFromCoreServerAsync( PackCommand( BuildWriteValueCommand( address, value ) ) ), ExtractActualData );

#endif
		#endregion

		#region Command Build

		/// <summary>
		/// 将核心的指令打包成一个可用于发送的消息对象<br />
		/// Package the core instructions into a message object that can be sent
		/// </summary>
		/// <param name="commandCore">核心命令</param>
		/// <returns>最终实现的可以发送的机器人的字节数据</returns>
		private byte[] PackCommand( byte[] commandCore )
		{
			byte[] buffer = new byte[commandCore.Length + 4];
			ByteTransform.TransByte( (ushort)softIncrementCount.GetCurrentValue( ) ).CopyTo( buffer, 0 );
			ByteTransform.TransByte( (ushort)commandCore.Length ).CopyTo( buffer, 2 );
			commandCore.CopyTo( buffer, 4 );

			return buffer;
		}

		private OperateResult<byte[]> ExtractActualData(byte[] response )
		{
			try
			{
				if(response[response.Length - 1] != 0x01) return new OperateResult<byte[]>( response[response.Length - 1], "Wrong: " + SoftBasic.ByteToHexString( response, ' ' ) );

				int length = response[5] * 256 + response[6];
				byte[] buffer = new byte[length];
				Array.Copy( response, 7, buffer, 0, length );
				return OperateResult.CreateSuccessResult( buffer );
			}
			catch(Exception ex)
			{
				return new OperateResult<byte[]>( "Wrong:" + ex.Message + " Code:" + SoftBasic.ByteToHexString( response, ' ' ) );
			}
		}

		private byte[] BuildCommands(byte function, string[] commands )
		{
			List<byte> buffer = new List<byte>( );
			buffer.Add( function );
			for (int i = 0; i < commands.Length; i++)
			{
				byte[] buffer_command = Encoding.Default.GetBytes( commands[i] );
				buffer.AddRange( ByteTransform.TransByte( (ushort)buffer_command.Length ) );
				buffer.AddRange( buffer_command );
			}
			return buffer.ToArray( );
		}

		private byte[] BuildReadValueCommand( string address ) => BuildCommands( 0x00, new string[] { address } );

		private byte[] BuildWriteValueCommand( string address, string value ) => BuildCommands( 0x01, new string[] { address, value } );

		#endregion

		#region Private Member

		private SoftIncrementCount softIncrementCount;              // 自增消息的对象

		#endregion

		#region Object Override

		/// <inheritdoc/>
		public override string ToString( ) => $"KukaAvarProxyNet Robot[{IpAddress}:{Port}]";

		#endregion
	}
}
