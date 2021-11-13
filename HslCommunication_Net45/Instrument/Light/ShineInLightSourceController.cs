using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HslCommunication.Serial;
using HslCommunication.Reflection;
using System.IO.Ports;

namespace HslCommunication.Instrument.Light
{
	/// <summary>
	/// 昱行智造科技（深圳）有限公司的光源控制器，可以控制灯的亮暗，控制灯的颜色，通道等信息。
	/// </summary>
	public class ShineInLightSourceController : SerialBase
	{
		#region Constructor

		/// <summary>
		/// 实例化一个默认的对象
		/// </summary>
		public ShineInLightSourceController( )
		{

		}

		#endregion

		#region Override Method

		/// <summary>
		/// 初始化串口信息，波特率，8位数据位，1位停止位，偶校验<br />
		/// Initializes serial port information, baud rate, 8-bit data bit, 1-bit stop bit, even parity
		/// </summary>
		/// <param name="portName">端口号信息，例如"COM3"</param>
		/// <param name="baudRate">波特率</param>
		public override void SerialPortInni( string portName, int baudRate )
		{
			SerialPortInni( portName, baudRate, 8, StopBits.One, Parity.Even );
		}

		/// <summary>
		/// 初始化串口信息，57600波特率，8位数据位，1位停止位，偶校验<br />
		/// Initial serial port information, 57600 baud rate, 8 data bits, 1 stop bit, even parity
		/// </summary>
		/// <param name="portName">端口号信息，例如"COM3"</param>
		public override void SerialPortInni( string portName )
		{
			SerialPortInni( portName, 57600 );
		}

		#endregion

		#region Read Write

		/// <summary>
		/// 读取光源控制器的参数信息，需要传入通道号信息，读取到详细的内容参照<see cref="ShineInLightData"/>的值
		/// </summary>
		/// <param name="channel">读取的通道信息</param>
		/// <returns>读取的参数值</returns>
		[HslMqttApi(ApiTopic = "Read", Description = "读取光源控制器的参数信息，需要传入通道号信息，返回 ShineInLightData 对象" )]
		public OperateResult<ShineInLightData> Read( byte channel )
		{
			OperateResult<byte[]> read = ReadFromCoreServer( BuildReadCommand( channel ) );
			if (!read.IsSuccess) return read.ConvertFailed<ShineInLightData>( );

			OperateResult<byte[]> extra = ExtractActualData( read.Content );
			if (!extra.IsSuccess) return extra.ConvertFailed<ShineInLightData>( );

			return OperateResult.CreateSuccessResult( new ShineInLightData( extra.Content ) );
		}

		/// <summary>
		/// 将光源控制器的数据写入到设备，返回是否写入成功
		/// </summary>
		/// <param name="data">光源数据</param>
		/// <returns>是否写入成功</returns>
		[HslMqttApi( ApiTopic = "Write", Description = "将光源控制器的数据写入到设备，返回是否写入成功" )]
		public OperateResult Write( ShineInLightData data )
		{
			OperateResult<byte[]> read = ReadFromCoreServer( BuildWriteCommand( data ) );
			if (!read.IsSuccess) return read.ConvertFailed<ShineInLightData>( );

			return ExtractActualData( read.Content );
		}

		#endregion

		#region Object Override

		/// <inheritdoc/>
		public override string ToString( ) => $"ShineInLightSourceController[{PortName}]";

		#endregion

		#region Static Helper

		/// <summary>
		/// 将命令和数据打包成用于发送的报文
		/// </summary>
		/// <param name="cmd">命令</param>
		/// <param name="data">命令数据</param>
		/// <returns>可用于发送的报文</returns>
		public static byte[] PackCommand( byte cmd, byte[] data )
		{
			if (data == null) data = new byte[0];
			byte[] buffer = new byte[data.Length + 8];
			buffer[0] = 0x2f;     // /
			buffer[1] = 0x2a;     // *
			buffer[2] = 0xf0;     // computer to controll
			buffer[3] = cmd;      // command
			buffer[4] = (byte)(buffer.Length - 4);
			data.CopyTo( buffer, 5 );
			buffer[buffer.Length - 2] = 0x2a;  // *
			buffer[buffer.Length - 1] = 0x2f;  // /

			int tmp = buffer[2];
			for (int i = 3; i < buffer.Length - 3; i++)
			{
				tmp ^= buffer[i];
			}
			buffer[buffer.Length - 3] = (byte)tmp;
			return buffer;
		}

		/// <summary>
		/// 构建写入数据的报文命令
		/// </summary>
		/// <param name="shineInLightData">准备写入的数据</param>
		/// <returns>报文命令</returns>
		public static byte[] BuildWriteCommand( ShineInLightData shineInLightData )
		{
			return PackCommand( 0x01, shineInLightData.GetSourceData( ) );
		}

		/// <summary>
		/// 构建读取数据的报文命令
		/// </summary>
		/// <param name="channel">通道信息</param>
		/// <returns>构建读取的命令</returns>
		public static byte[] BuildReadCommand( byte channel )
		{
			return PackCommand( 0x02, new byte[] { channel } );
		}

		/// <summary>
		/// 把服务器反馈的数据解析成实际的命令
		/// </summary>
		/// <param name="response">反馈的数据</param>
		/// <returns>结果内容</returns>
		public static OperateResult<byte[]> ExtractActualData( byte[] response )
		{
			if (response.Length < 9) return new OperateResult<byte[]>( "Receive Data is too short; source:" + response.ToHexString( ' ' ) );
			if (response[0] != 0x2F || response[1] != 0x2A || response[response.Length - 2] != 0x2A || response[response.Length - 1] != 0x2F)
				return new OperateResult<byte[]>( "Receive Data not start with /* or end with */; source:" + response.ToHexString( ) );
			if (response[3] == 0x01)
				return response[5] == 0xAA ? OperateResult.CreateSuccessResult( new byte[0] ) : new OperateResult<byte[]>( response[5], "set not success" );
			else
				return OperateResult.CreateSuccessResult( response.SelectMiddle( 5, response.Length - 8 ) );
		}

		#endregion

	}

	#region ShineInLightData

	/// <summary>
	/// 光源的数据信息
	/// </summary>
	public class ShineInLightData
	{
		/// <summary>
		/// 实例化一个默认的对象
		/// </summary>
		public ShineInLightData( )
		{
			Color       = 4;
			LightDegree = 1;
			PulseWidth  = 1;
		}

		/// <summary>
		/// 使用指定的原始数据来获取当前的对象
		/// </summary>
		/// <param name="data">原始数据</param>
		public ShineInLightData( byte[] data ) : this( )
		{
			ParseFrom( data );
		}

		/// <summary>
		/// 光源颜色信息，1:红色  2:绿色  3:蓝色  4:白色(默认)
		/// </summary>
		public byte Color { get; set; }

		/// <summary>
		/// 光源的亮度信息，00-FF，值越大，亮度越大
		/// </summary>
		public byte Light { get; set; }

		/// <summary>
		/// 光源的亮度等级，1-3
		/// </summary>
		public byte LightDegree { get; set; }

		/// <summary>
		/// 光源的工作模式，00:延时常亮  01:通道一频闪  02:通道二频闪  03:通道一二频闪  04:普通常亮  05:关闭
		/// </summary>
		public byte WorkMode { get; set; }

		/// <summary>
		/// 控制器的地址选择位
		/// </summary>
		public byte Address { get; set; }

		/// <summary>
		/// 脉冲宽度，01-14H
		/// </summary>
		public byte PulseWidth { get; set; }

		/// <summary>
		/// 通道数据，01-08H的值
		/// </summary>
		public byte Channel { get; set; }

		/// <summary>
		/// 获取原始的数据信息
		/// </summary>
		/// <returns>原始的字节信息</returns>
		public byte[] GetSourceData( )
		{
			return new byte[] { Color, Light, LightDegree, WorkMode, Address, PulseWidth, Channel };
		}

		/// <summary>
		/// 从原始的信息解析光源的数据
		/// </summary>
		/// <param name="data">原始的数据信息</param>
		public void ParseFrom( byte[] data )
		{
			if (data?.Length < 7) return;
			this.Color       = data[0];
			this.Light       = data[1];
			this.LightDegree = data[2];
			this.WorkMode    = data[3];
			this.Address     = data[4];
			this.PulseWidth  = data[5];
			this.Channel     = data[6];
		}

		/// <inheritdoc/>
		public override string ToString( ) => $"ShineInLightData[{Color}]";
	}

	#endregion
}
