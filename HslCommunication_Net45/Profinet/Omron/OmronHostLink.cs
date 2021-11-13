using HslCommunication.BasicFramework;
using HslCommunication.Core;
using HslCommunication.Serial;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HslCommunication.Reflection;
using System.IO;

namespace HslCommunication.Profinet.Omron
{
	/// <summary>
	/// 欧姆龙的HostLink协议的实现，地址支持示例 DM区:D100; CIO区:C100; Work区:W100; Holding区:H100; Auxiliary区: A100<br />
	/// Implementation of Omron's HostLink protocol, address support example DM area: D100; CIO area: C100; Work area: W100; Holding area: H100; Auxiliary area: A100
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
	/// <inheritdoc cref="OmronHostLinkOverTcp" path="example"/>
	/// </example>
	public class OmronHostLink : SerialDeviceBase
	{
		#region Constructor

		/// <inheritdoc cref="OmronFinsNet()"/>
		public OmronHostLink( )
		{
			this.ByteTransform                         = new ReverseWordTransform( );
			this.WordLength                            = 1;
			this.ByteTransform.DataFormat              = DataFormat.CDAB;
			this.ByteTransform.IsStringReverseByteWord = true;
			this.LogMsgFormatBinary                    = false;
		}

		#endregion

		#region Public Member

		/// <inheritdoc cref="OmronHostLinkOverTcp.ICF"/>
		public byte ICF { get; set; } = 0x00;

		/// <inheritdoc cref="OmronHostLinkOverTcp.DA2"/>
		public byte DA2 { get; set; } = 0x00;

		/// <inheritdoc cref="OmronHostLinkOverTcp.SA2"/>
		public byte SA2 { get; set; }

		/// <inheritdoc cref="OmronHostLinkOverTcp.SID"/>
		public byte SID { get; set; } = 0x00;

		/// <inheritdoc cref="OmronHostLinkOverTcp.ResponseWaitTime"/>
		public byte ResponseWaitTime { get; set; } = 0x30;

		/// <inheritdoc cref="OmronHostLinkOverTcp.UnitNumber"/>
		public byte UnitNumber { get; set; }

		/// <inheritdoc cref="OmronHostLinkOverTcp.ReadSplits"/>
		public int ReadSplits { get; set; } = 260;

		#endregion

		#region Pack Unpack Override

		/// <inheritdoc/>
		protected override OperateResult<byte[]> UnpackResponseContent( byte[] send, byte[] response )
		{
			return OmronHostLinkOverTcp.ResponseValidAnalysis( send, response );
		}

		/// <inheritdoc/>
		protected override bool CheckReceiveDataComplete( MemoryStream ms )
		{
			byte[] buffer = ms.ToArray( );
			if (buffer.Length > 1) return buffer[buffer.Length - 1] == 0x0D;
			return false;
		}

		/// <summary>
		/// 初始化串口信息，9600波特率，7位数据位，1位停止位，偶校验<br />
		/// Initial serial port information, 9600 baud rate, 7 data bits, 1 stop bit, even parity
		/// </summary>
		/// <param name="portName">端口号信息，例如"COM3"</param>
		public override void SerialPortInni( string portName )
		{
			base.SerialPortInni( portName );
		}

		/// <summary>
		/// 初始化串口信息，波特率，7位数据位，1位停止位，偶校验<br />
		/// Initializes serial port information, baud rate, 7-bit data bit, 1-bit stop bit, even parity
		/// </summary>
		/// <param name="portName">端口号信息，例如"COM3"</param>
		/// <param name="baudRate">波特率</param>
		public override void SerialPortInni( string portName, int baudRate )
		{
			base.SerialPortInni( portName, baudRate, 7, System.IO.Ports.StopBits.One, System.IO.Ports.Parity.Even);
		}
		#endregion

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

		#region Bool Read Write

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
				contentArray.AddRange( read.Content.Select( m => m != 0x00 ? true : false ) );
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

		#region Object Override

		/// <inheritdoc/>
		public override string ToString( ) => $"OmronHostLink[{PortName}:{BaudRate}]";

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
			cmd = BasicFramework.SoftBasic.BytesToAsciiBytes( cmd );

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
				tmp = (tmp ^ buffer[i]);
			}
			buffer[buffer.Length - 4] = SoftBasic.BuildAsciiBytesFrom( (byte)tmp )[0];
			buffer[buffer.Length - 3] = SoftBasic.BuildAsciiBytesFrom( (byte)tmp )[1];
			return buffer;
		}

		#endregion
	}
}
