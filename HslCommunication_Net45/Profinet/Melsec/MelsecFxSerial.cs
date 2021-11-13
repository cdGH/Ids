using HslCommunication.Core;
using HslCommunication.Serial;
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
	/// 三菱的串口通信的对象，适用于读取FX系列的串口数据，支持的类型参考文档说明<br />
	/// Mitsubishi's serial communication object is suitable for reading serial data of the FX series. Refer to the documentation for the supported types.
	/// </summary>
	/// <remarks>
	/// <inheritdoc cref="MelsecFxSerialOverTcp" path="remarks"/>
	/// </remarks>
	/// <example>
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Profinet\MelsecFxSerial.cs" region="Usage" title="简单的使用" />
	/// </example>
	public class MelsecFxSerial : SerialDeviceBase
	{
		#region Constructor

		/// <summary>
		/// 实例化一个默认的对象
		/// </summary>
		public MelsecFxSerial( )
		{
			this.ByteTransform                         = new RegularByteTransform( );
			this.WordLength                            = 1;
			this.IsNewVersion                          = true;
			this.ByteTransform.IsStringReverseByteWord = true;
			this.AtLeastReceiveLength                  = 2;      // 至少接收2个字节的数据
		}

		#endregion

		/// <inheritdoc cref="MelsecFxSerialOverTcp.IsNewVersion"/>
		public bool IsNewVersion { get; set; }

		#region Read Write Byte

		/// <inheritdoc cref="MelsecFxSerialOverTcp.Read(string, ushort)"/>
		[HslMqttApi( "ReadByteArray", "" )]
		public override OperateResult<byte[]> Read( string address, ushort length ) => 
			Helper.MelsecFxSerialHelper.Read( this, address, length, IsNewVersion );

		/// <inheritdoc cref="MelsecFxSerialOverTcp.Write(string, byte[])"/>
		[HslMqttApi( "WriteByteArray", "" )]
		public override OperateResult Write( string address, byte[] value ) => 
			Helper.MelsecFxSerialHelper.Write( this, address, value, IsNewVersion );

		#endregion

		#region Read Write Bool

		/// <inheritdoc cref="MelsecFxSerialOverTcp.ReadBool(string, ushort)"/>
		[HslMqttApi( "ReadBoolArray", "" )]
		public override OperateResult<bool[]> ReadBool( string address, ushort length ) => 
			Helper.MelsecFxSerialHelper.ReadBool( this, address, length );

		/// <inheritdoc cref="MelsecFxSerialOverTcp.Write(string, bool)"/>
		[HslMqttApi( "WriteBool", "" )]
		public override OperateResult Write( string address, bool value ) => 
			Helper.MelsecFxSerialHelper.Write( this, address, value );

		#endregion

		#region Async Read Write Bool
#if !NET35 && !NET20
		/// <inheritdoc/>
		public override async Task<OperateResult> WriteAsync( string address, bool value ) => await Task.Run( ( ) => Write( address, value ) );
#endif
		#endregion

		#region Object Override

		/// <inheritdoc/>
		public override string ToString( ) => $"MelsecFxSerial[{PortName}:{BaudRate}]";

		#endregion
	}
}
