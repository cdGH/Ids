using HslCommunication.BasicFramework;
using HslCommunication.Core;
using HslCommunication.Core.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HslCommunication.Reflection;
using System.Net.Sockets;
using HslCommunication.Core.IMessage;
#if !NET35 && !NET20
using System.Threading.Tasks;
#endif

namespace HslCommunication.Profinet.Omron
{
	/// <summary>
	/// 欧姆龙的HostLink协议的实现，基于Tcp实现，地址支持示例 DM区:D100; CIO区:C100; Work区:W100; Holding区:H100; Auxiliary区: A100<br />
	/// Implementation of Omron's HostLink protocol, based on tcp protocol, address support example DM area: D100; CIO area: C100; Work area: W100; Holding area: H100; Auxiliary area: A100
	/// </summary>
	/// <remarks>
	/// 感谢 深圳～拾忆 的测试，地址可以携带站号信息，例如 s=2;D100 
	/// <br />
	/// <note type="important">
	/// 如果发现串口线和usb同时打开才能通信的情况，需要按照如下的操作：<br />
	/// 串口线不是标准的串口线，电脑的串口线的235引脚分别接PLC的329引脚，45线短接，就可以通讯，感谢 深圳-小君(QQ932507362)提供的解决方案。
	/// </note>
	/// </remarks>
	/// <example>
	/// 欧姆龙的地址参考如下：
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
	///     <term>DM Area</term>
	///     <term>D</term>
	///     <term>D100,D200</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>CIO Area</term>
	///     <term>C</term>
	///     <term>C100,C200</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>Work Area</term>
	///     <term>W</term>
	///     <term>W100,W200</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>Holding Bit Area</term>
	///     <term>H</term>
	///     <term>H100,H200</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>Auxiliary Bit Area</term>
	///     <term>A</term>
	///     <term>A100,A200</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	/// </list>
	/// </example>
	public class OmronHostLinkOverTcp : NetworkDeviceBase
	{
		#region Constructor

		/// <inheritdoc cref="OmronFinsNet()"/>
		public OmronHostLinkOverTcp( )
		{
			this.ByteTransform            = new ReverseWordTransform( );
			this.WordLength               = 1;
			this.ByteTransform.DataFormat = DataFormat.CDAB;
			//this.SleepTime                = 20;
			this.LogMsgFormatBinary       = false;
		}

		/// <inheritdoc cref="OmronCipNet(string,int)"/>
		public OmronHostLinkOverTcp( string ipAddress, int port ) : this( ) 
		{ 
			this.IpAddress = ipAddress; 
			this.Port      = port; 
		}

		#endregion

		#region Pack Unpack Override

		/// <inheritdoc/>
		protected override OperateResult<byte[]> UnpackResponseContent( byte[] send, byte[] response )
		{
			return ResponseValidAnalysis( send, response );
		}

		#endregion

		#region Public Member

		/// <summary>
		/// Specifies whether or not there are network relays. Set “80” (ASCII: 38,30) 
		/// when sending an FINS command to a CPU Unit on a network.Set “00” (ASCII: 30,30) 
		/// when sending to a CPU Unit connected directly to the host computer.
		/// </summary>
		public byte ICF { get; set; } = 0x00;

		/// <inheritdoc cref="OmronFinsNet.DA2"/>
		public byte DA2 { get; set; } = 0x00;

		/// <inheritdoc cref="OmronFinsNet.SA2"/>
		public byte SA2 { get; set; }

		/// <inheritdoc cref="OmronFinsNet.SID"/>
		public byte SID { get; set; } = 0x00;

		/// <summary>
		/// The response wait time sets the time from when the CPU Unit receives a command block until it starts 
		/// to return a response.It can be set from 0 to F in hexadecimal, in units of 10 ms.
		/// If F(15) is set, the response will begin to be returned 150 ms (15 × 10 ms) after the command block was received.
		/// </summary>
		public byte ResponseWaitTime { get; set; } = 0x30;

		/// <summary>
		/// PLC设备的站号信息<br />
		/// PLC device station number information
		/// </summary>
		public byte UnitNumber { get; set; }

		/// <summary>
		/// 进行字读取的时候对于超长的情况按照本属性进行切割，默认260。<br />
		/// When reading words, it is cut according to this attribute for the case of overlength. The default is 260.
		/// </summary>
		public int ReadSplits { get; set; } = 260;

		#endregion

		/// <inheritdoc/>
		protected override OperateResult<byte[]> ReceiveByMessage( Socket socket, int timeOut, INetMessage netMessage, Action<long, long> reportProgress = null )
		{
			return ReceiveCommandLineFromSocket( socket, 0x0D, timeOut );
		}
#if !NET20 && !NET35
		/// <inheritdoc/>
		protected override Task<OperateResult<byte[]>> ReceiveByMessageAsync( Socket socket, int timeOut, INetMessage netMessage, Action<long, long> reportProgress = null )
		{
			return ReceiveCommandLineFromSocketAsync( socket, 0x0D, timeOut );
		}
#endif
		#region Read Write Support

		/// <inheritdoc cref="OmronFinsNet.Read(string, ushort)"/>
		[HslMqttApi( "ReadByteArray", "" )]
		public override OperateResult<byte[]> Read( string address, ushort length )
		{
			byte station = (byte)HslHelper.ExtractParameter( ref address, "s", this.UnitNumber );

			// 解析地址
			var command = OmronFinsNetHelper.BuildReadCommand( address, length, false, ReadSplits );
			if (!command.IsSuccess) return command.ConvertFailed<byte[]>( );

			List<byte> contentArray = new List<byte>( );
			for (int i = 0; i < command.Content.Count; i++)
			{
				// 核心交互
				OperateResult<byte[]> read = ReadFromCoreServer( PackCommand( station, command.Content[i] ) );
				if (!read.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( read );

				// 读取到了正确的数据
				contentArray.AddRange( read.Content );
			}

			return OperateResult.CreateSuccessResult( contentArray.ToArray( ) );
		}

		/// <inheritdoc cref="OmronFinsNet.Write(string, byte[])"/>
		[HslMqttApi( "WriteByteArray", "" )]
		public override OperateResult Write( string address, byte[] value )
		{
			byte station = (byte)HslHelper.ExtractParameter( ref address, "s", this.UnitNumber );

			// 获取指令
			var command = OmronFinsNetHelper.BuildWriteWordCommand( address, value, false ); ;
			if (!command.IsSuccess) return command;

			// 核心数据交互
			OperateResult<byte[]> read = ReadFromCoreServer( PackCommand( station, command.Content ) );
			if (!read.IsSuccess) return read;

			// 成功
			return OperateResult.CreateSuccessResult( );
		}

		#endregion

		#region Async Read Write Support
#if !NET35 && !NET20
		/// <inheritdoc cref="OmronFinsNet.Read(string, ushort)"/>
		public override async Task<OperateResult<byte[]>> ReadAsync( string address, ushort length )
		{
			byte station = (byte)HslHelper.ExtractParameter( ref address, "s", this.UnitNumber );

			// 解析地址
			var command = OmronFinsNetHelper.BuildReadCommand( address, length, false, ReadSplits );
			if (!command.IsSuccess) return command.ConvertFailed<byte[]>( );

			List<byte> contentArray = new List<byte>( );
			for (int i = 0; i < command.Content.Count; i++)
			{
				// 核心交互
				OperateResult<byte[]> read = await ReadFromCoreServerAsync( PackCommand( station, command.Content[i] ) );
				if (!read.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( read );

				// 读取到了正确的数据
				contentArray.AddRange( read.Content );
			}

			return OperateResult.CreateSuccessResult( contentArray.ToArray( ) );
		}

		/// <inheritdoc cref="OmronFinsNet.Write(string, byte[])"/>
		public override async Task<OperateResult> WriteAsync( string address, byte[] value )
		{
			byte station = (byte)HslHelper.ExtractParameter( ref address, "s", this.UnitNumber );

			// 获取指令
			var command = OmronFinsNetHelper.BuildWriteWordCommand( address, value, false ); ;
			if (!command.IsSuccess) return command;

			// 核心数据交互
			OperateResult<byte[]> read = await ReadFromCoreServerAsync( PackCommand( station, command.Content ) );
			if (!read.IsSuccess) return read;

			// 成功
			return OperateResult.CreateSuccessResult( );
		}
#endif
		#endregion

		#region Read Write Bool

		/// <inheritdoc cref="OmronFinsNet.ReadBool(string, ushort)"/>
		[HslMqttApi( "ReadBoolArray", "" )]
		public override OperateResult<bool[]> ReadBool( string address, ushort length )
		{
			byte station = (byte)HslHelper.ExtractParameter( ref address, "s", this.UnitNumber );

			// 获取指令
			var command = OmronFinsNetHelper.BuildReadCommand( address, length, true );
			if (!command.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( command );

			List<bool> contentArray = new List<bool>( );
			for (int i = 0; i < command.Content.Count; i++)
			{
				// 核心交互
				OperateResult<byte[]> read = ReadFromCoreServer( PackCommand( station, command.Content[i] ) );
				if (!read.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( read );

				// 返回正确的数据信息
				if (read.Content.Length == 0) return new OperateResult<bool[]>( "Data is empty." );
				contentArray.AddRange( read.Content.Select( m => m != 0x00 ) );
			}
			return OperateResult.CreateSuccessResult( contentArray.ToArray( ) );
		}

		/// <inheritdoc cref="OmronFinsNet.Write(string, bool[])"/>
		[HslMqttApi( "WriteBoolArray", "" )]
		public override OperateResult Write( string address, bool[] values )
		{
			byte station = (byte)HslHelper.ExtractParameter( ref address, "s", this.UnitNumber );

			// 获取指令
			var command = OmronFinsNetHelper.BuildWriteWordCommand( address, values.Select( m => m ? (byte)0x01 : (byte)0x00 ).ToArray( ), true ); ;
			if (!command.IsSuccess) return command;

			// 核心数据交互
			OperateResult<byte[]> read = ReadFromCoreServer( PackCommand( station, command.Content ) );
			if (!read.IsSuccess) return read;

			// 成功
			return OperateResult.CreateSuccessResult( );
		}

		#endregion

		#region Async Read Write Bool
#if !NET35 && !NET20
		/// <inheritdoc cref="OmronFinsNet.ReadBool(string, ushort)"/>
		public override async Task<OperateResult<bool[]>> ReadBoolAsync( string address, ushort length )
		{
			byte station = (byte)HslHelper.ExtractParameter( ref address, "s", this.UnitNumber );

			// 获取指令
			var command = OmronFinsNetHelper.BuildReadCommand( address, length, true );
			if (!command.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( command );

			List<bool> contentArray = new List<bool>( );
			for (int i = 0; i < command.Content.Count; i++)
			{
				// 核心交互
				OperateResult<byte[]> read = await ReadFromCoreServerAsync( PackCommand( station, command.Content[i] ) );
				if (!read.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( read );

				// 返回正确的数据信息
				contentArray.AddRange( read.Content.Select( m => m != 0x00 ) );
			}
			return OperateResult.CreateSuccessResult( contentArray.ToArray( ) );
		}

		/// <inheritdoc cref="OmronFinsNet.Write(string, bool[])"/>
		public override async Task<OperateResult> WriteAsync( string address, bool[] values )
		{
			byte station = (byte)HslHelper.ExtractParameter( ref address, "s", this.UnitNumber );

			// 获取指令
			var command = OmronFinsNetHelper.BuildWriteWordCommand( address, values.Select( m => m ? (byte)0x01 : (byte)0x00 ).ToArray( ), true ); ;
			if (!command.IsSuccess) return command;

			// 核心数据交互
			OperateResult<byte[]> read = await ReadFromCoreServerAsync( PackCommand( station, command.Content ) );
			if (!read.IsSuccess) return read;

			// 成功
			return OperateResult.CreateSuccessResult( );
		}
#endif
		#endregion

		#region Object Override

		/// <inheritdoc/>
		public override string ToString( ) => $"OmronHostLinkOverTcp[{IpAddress}:{Port}]";

		#endregion

		#region Build Command

		/// <summary>
		/// 将普通的指令打包成完整的指令
		/// </summary>
		/// <param name="station">PLC的站号信息</param>
		/// <param name="cmd">fins指令</param>
		/// <returns>完整的质量</returns>
		private byte[] PackCommand( byte station, byte[] cmd )
		{
			cmd = SoftBasic.BytesToAsciiBytes( cmd );

			byte[] buffer = new byte[18 + cmd.Length];
			buffer[ 0] = (byte)'@';
			buffer[ 1] = SoftBasic.BuildAsciiBytesFrom( station )[0];
			buffer[ 2] = SoftBasic.BuildAsciiBytesFrom( station )[1];
			buffer[ 3] = (byte)'F';
			buffer[ 4] = (byte)'A';
			buffer[ 5] = ResponseWaitTime;
			buffer[ 6] = SoftBasic.BuildAsciiBytesFrom( this.ICF )[0];
			buffer[ 7] = SoftBasic.BuildAsciiBytesFrom( this.ICF )[1];
			buffer[ 8] = SoftBasic.BuildAsciiBytesFrom( this.DA2 )[0];
			buffer[ 9] = SoftBasic.BuildAsciiBytesFrom( this.DA2 )[1];
			buffer[10] = SoftBasic.BuildAsciiBytesFrom( this.SA2 )[0];
			buffer[11] = SoftBasic.BuildAsciiBytesFrom( this.SA2 )[1];
			buffer[12] = SoftBasic.BuildAsciiBytesFrom( this.SID )[0];
			buffer[13] = SoftBasic.BuildAsciiBytesFrom( this.SID )[1];
			buffer[buffer.Length - 2] = (byte)'*';
			buffer[buffer.Length - 1] = 0x0D;
			cmd.CopyTo( buffer, 14 );
			// 计算FCS
			int tmp = buffer[0];
			for (int i = 1; i < buffer.Length - 4; i++)
			{
				tmp ^= buffer[i];
			}
			buffer[buffer.Length - 4] = SoftBasic.BuildAsciiBytesFrom( (byte)tmp )[0];
			buffer[buffer.Length - 3] = SoftBasic.BuildAsciiBytesFrom( (byte)tmp )[1];
			return buffer;
		}

		/// <summary>
		/// 验证欧姆龙的Fins-TCP返回的数据是否正确的数据，如果正确的话，并返回所有的数据内容
		/// </summary>
		/// <param name="send">发送的报文信息</param>
		/// <param name="response">来自欧姆龙返回的数据内容</param>
		/// <returns>带有是否成功的结果对象</returns>
		public static OperateResult<byte[]> ResponseValidAnalysis( byte[] send, byte[] response )
		{
			// 数据有效性分析
			// @00FA00400000000102000040*\cr
			if (response.Length >= 27)
			{
				string commandSend = Encoding.ASCII.GetString( send, 14, 4 );
				string commandReceive = Encoding.ASCII.GetString( response, 15, 4 );
				if (commandReceive != commandSend)
					return new OperateResult<byte[]>( $"Send Command [{commandSend}] not the same as receive command [{commandReceive}]" );
				// 提取错误码
				int err = Convert.ToInt32( Encoding.ASCII.GetString( response, 19, 4 ), 16 );
				byte[] content = new byte[0];
				if (response.Length > 27) content = SoftBasic.HexStringToBytes( Encoding.ASCII.GetString( response, 23, response.Length - 27 ) );

				if (err > 0) return new OperateResult<byte[]>( )
				{
					ErrorCode = err,
					Content = content,
					Message = GetErrorText( err )
				};
				else
					return OperateResult.CreateSuccessResult( content );
			}

			return new OperateResult<byte[]>( StringResources.Language.OmronReceiveDataError + " Source Data: " + response.ToHexString( ' ' ) );
		}

		/// <summary>
		/// 根据错误信息获取当前的文本描述信息
		/// </summary>
		/// <param name="error">错误代号</param>
		/// <returns>文本消息</returns>
		public static string GetErrorText( int error )
		{
			switch (error)
			{
				case 0x0001: return "Service was canceled.";
				case 0x0101: return "Local node is not participating in the network.";
				case 0x0102: return "Token does not arrive.";
				case 0x0103: return "Send was not possible during the specified number of retries.";
				case 0x0104: return "Cannot send because maximum number of event frames exceeded.";
				case 0x0105: return "Node address setting error occurred.";
				case 0x0106: return "The same node address has been set twice in the same network.";
				case 0x0201: return "The destination node is not in the network.";
				case 0x0202: return "There is no Unit with the specified unit address.";
				case 0x0203: return "The third node does not exist.";
				case 0x0204: return "The destination node is busy.";
				case 0x0205: return "The message was destroyed by noise";
				case 0x0301: return "An error occurred in the communications controller.";
				case 0x0302: return "A CPU error occurred in the destination CPU Unit.";
				case 0x0303: return "A response was not returned because an error occurred in the Board.";
				case 0x0304: return "The unit number was set incorrectly";
				case 0x0401: return "The Unit/Board does not support the specified command code.";
				case 0x0402: return "The command cannot be executed because the model or version is incorrect";
				case 0x0501: return "The destination network or node address is not set in the routing tables.";
				case 0x0502: return "Relaying is not possible because there are no routing tables";
				case 0x0503: return "There is an error in the routing tables.";
				case 0x0504: return "An attempt was made to send to a network that was over 3 networks away";
					// Command format error
				case 0x1001: return "The command is longer than the maximum permissible length.";
				case 0x1002: return "The command is shorter than the minimum permissible length.";
				case 0x1003: return "The designated number of elements differs from the number of write data items.";
				case 0x1004: return "An incorrect format was used.";
				case 0x1005: return "Either the relay table in the local node or the local network table in the relay node is incorrect.";
					// Parameter error
				case 0x1101: return "The specified word does not exist in the memory area or there is no EM Area.";
				case 0x1102: return "The access size specification is incorrect or an odd word address is specified.";
				case 0x1103: return "The start address in command process is beyond the accessible area";
				case 0x1104: return "The end address in command process is beyond the accessible area.";
				case 0x1106: return "FFFF hex was not specified.";
				case 0x1109: return "A large–small relationship in the elements in the command data is incorrect.";
				case 0x110B: return "The response format is longer than the maximum permissible length.";
				case 0x110C: return "There is an error in one of the parameter settings.";
					// Read Not Possible
				case 0x2002: return "The program area is protected.";
				case 0x2003: return "A table has not been registered.";
				case 0x2004: return "The search data does not exist.";
				case 0x2005: return "A non-existing program number has been specified.";
				case 0x2006: return "The file does not exist at the specified file device.";
				case 0x2007: return "A data being compared is not the same.";
					// Write not possible
				case 0x2101: return "The specified area is read-only.";
				case 0x2102: return "The program area is protected.";
				case 0x2103: return "The file cannot be created because the limit has been exceeded.";
				case 0x2105: return "A non-existing program number has been specified.";
				case 0x2106: return "The file does not exist at the specified file device.";
				case 0x2107: return "A file with the same name already exists in the specified file device.";
				case 0x2108: return "The change cannot be made because doing so would create a problem.";
					// Not executable in current mode
				case 0x2201: 
				case 0x2202: 
				case 0x2208: return "The mode is incorrect.";
				case 0x2203: return "The PLC is in PROGRAM mode.";
				case 0x2204: return "The PLC is in DEBUG mode.";
				case 0x2205: return "The PLC is in MONITOR mode.";
				case 0x2206: return "The PLC is in RUN mode.";
				case 0x2207: return "The specified node is not the polling node.";
					//  No such device
				case 0x2301: return "The specified memory does not exist as a file device.";
				case 0x2302: return "There is no file memory.";
				case 0x2303: return "There is no clock.";
				case 0x2401: return "The data link tables have not been registered or they contain an error.";
				default: return StringResources.Language.UnknownError;
			}
		}

		#endregion
	}
}
