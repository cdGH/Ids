using HslCommunication.BasicFramework;
using HslCommunication.Core;
using HslCommunication.Serial;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HslCommunication.Reflection;
using System.IO;
#if !NET20 && !NET35
using System.Threading.Tasks;
#endif

namespace HslCommunication.Profinet.Keyence
{
	/// <summary>
	/// 基恩士KV上位链路串口通信的对象,适用于Nano系列串口数据,KV1000以及L20V通信模块，地址格式参考api文档<br />
	/// Keyence KV upper link serial communication object, suitable for Nano series serial data, and L20V communication module, please refer to api document for address format
	/// </summary>
	/// <remarks>
	/// 位读写的数据类型为 R,B,MR,LR,CR,VB,以及读定时器的计数器的触点，字读写的数据类型为 DM,EM,FM,ZF,W,TM,Z,AT,CM,VM 双字读写为T,C,TC,CC,TS,CS。如果想要读写扩展的缓存器，地址示例：unit=2;1000  前面的是单元编号，后面的是偏移地址<br />
	/// 注意：在端口 2 以多分支连接 KV-L21V 时，请一定加上站号。在将端口 2 设定为使用 RS-422A、 RS-485 时， KV-L21V 即使接收本站以外的带站号的指令，也将变为无应答，不返回响应消息。
	/// </remarks>
	/// <example>
	/// <inheritdoc cref="KeyenceNanoSerialOverTcp" path="example"/>
	/// </example>
	public class KeyenceNanoSerial : SerialDeviceBase
	{
		#region Constructor

		/// <summary>
		/// 实例化基恩士的串口协议的通讯对象<br />
		/// Instantiate the communication object of Keyence's serial protocol
		/// </summary>
		public KeyenceNanoSerial( )
		{
			this.ByteTransform = new RegularByteTransform( );
			this.WordLength    = 1;
		}

		/// <inheritdoc/>
		protected override OperateResult InitializationOnOpen( )
		{
			// 建立通讯连接{CR/r}
			var result = ReadFromCoreServer( KeyenceNanoHelper.GetConnectCmd( Station, UseStation ) );
			if (!result.IsSuccess) return result;

			if (result.Content.Length > 2)
				if (result.Content[0] == 0x43 && result.Content[1] == 0x43)
					return OperateResult.CreateSuccessResult( );

			return new OperateResult( "Check Failed: " + SoftBasic.ByteToHexString( result.Content, ' ' ) );
		}

		/// <inheritdoc/>
		protected override OperateResult ExtraOnClose( )
		{
			// 断开通讯连接{CR/r}
			var result = ReadFromCoreServer( KeyenceNanoHelper.GetDisConnectCmd( Station, UseStation ) );
			if (!result.IsSuccess) return result;

			if (result.Content.Length > 2)
				if (result.Content[0] == 0x43 && result.Content[1] == 0x46)
					return OperateResult.CreateSuccessResult( );

			return new OperateResult( "Check Failed: " + SoftBasic.ByteToHexString( result.Content, ' ' ) );
		}

		/// <inheritdoc/>
		protected override bool CheckReceiveDataComplete( MemoryStream ms )
		{
			byte[] buffer = ms.ToArray( );
			if(buffer.Length > 2) return buffer[buffer.Length - 2] == 0x0D && buffer[buffer.Length - 1] == 0x0A;

			return base.CheckReceiveDataComplete( ms );
		}

		#endregion

		#region Public Properties

		/// <inheritdoc cref="KeyenceNanoSerialOverTcp.Station"/>
		public byte Station { get; set; }

		/// <inheritdoc cref="KeyenceNanoSerialOverTcp.UseStation"/>
		public bool UseStation { get; set; }

		#endregion

		#region Read Write Support

		/// <inheritdoc/>
		[HslMqttApi( "ReadByteArray", "" )]
		public override OperateResult<byte[]> Read( string address, ushort length ) => KeyenceNanoHelper.Read( this, address, length );

		/// <inheritdoc/>
		[HslMqttApi( "WriteByteArray", "" )]
		public override OperateResult Write( string address, byte[] value ) => KeyenceNanoHelper.Write( this, address, value );

		#endregion

		#region Read Write Bool

		/// <inheritdoc/>
		[HslMqttApi( "ReadBoolArray", "" )]
		public override OperateResult<bool[]> ReadBool( string address, ushort length ) => KeyenceNanoHelper.ReadBool( this, address, length );

		/// <inheritdoc/>
		[HslMqttApi( "WriteBool", "" )]
		public override OperateResult Write( string address, bool value ) => KeyenceNanoHelper.Write( this, address, value );

		/// <inheritdoc/>
		[HslMqttApi( "WriteBoolArray", "" )]
		public override OperateResult Write( string address, bool[] value ) => KeyenceNanoHelper.Write( this, address, value );

#if !NET20 && !NET35
		/// <inheritdoc cref="Write(string, bool)"/>
		public async override Task<OperateResult> WriteAsync( string address, bool value ) => await Task.Run( ( ) => Write( address, value ) );
#endif
		#endregion

		#region Advance Api

		/// <inheritdoc cref="KeyenceNanoHelper.ReadPlcType(IReadWriteDevice)"/>
		[HslMqttApi( "查询PLC的型号信息" )]
		public OperateResult<KeyencePLCS> ReadPlcType( ) => KeyenceNanoHelper.ReadPlcType( this );

		/// <inheritdoc cref="KeyenceNanoHelper.ReadPlcMode(IReadWriteDevice)"/>
		[HslMqttApi( "读取当前PLC的模式，如果是0，代表 PROG模式或者梯形图未登录，如果为1，代表RUN模式" )]
		public OperateResult<int> ReadPlcMode( ) => KeyenceNanoHelper.ReadPlcMode( this );

		/// <inheritdoc cref="KeyenceNanoHelper.SetPlcDateTime(IReadWriteDevice, DateTime)"/>
		[HslMqttApi( "设置PLC的时间" )]
		public OperateResult SetPlcDateTime( DateTime dateTime ) => KeyenceNanoHelper.SetPlcDateTime( this, dateTime );

		/// <inheritdoc cref="KeyenceNanoHelper.ReadAddressAnnotation(IReadWriteDevice, string)"/>
		[HslMqttApi( "读取指定软元件的注释信息" )]
		public OperateResult<string> ReadAddressAnnotation( string address ) => KeyenceNanoHelper.ReadAddressAnnotation( this, address );

		/// <inheritdoc cref="KeyenceNanoHelper.ReadExpansionMemory(IReadWriteDevice, byte, ushort, ushort)"/>
		[HslMqttApi( "从扩展单元缓冲存储器连续读取指定个数的数据，单位为字" )]
		public OperateResult<byte[]> ReadExpansionMemory( byte unit, ushort address, ushort length ) => KeyenceNanoHelper.ReadExpansionMemory( this, unit, address, length );

		/// <inheritdoc cref="KeyenceNanoHelper.WriteExpansionMemory(IReadWriteDevice, byte, ushort, byte[])"/>
		[HslMqttApi( "将原始字节数据写入到扩展的缓冲存储器，需要指定单元编号，偏移地址，写入的数据" )]
		public OperateResult WriteExpansionMemory( byte unit, ushort address, byte[] value ) => KeyenceNanoHelper.WriteExpansionMemory( this, unit, address, value );

		#endregion

		#region Object Override

		/// <inheritdoc/>
		public override string ToString( ) => $"KeyenceNanoSerial[{PortName}:{BaudRate}]";

		#endregion

	}
}
