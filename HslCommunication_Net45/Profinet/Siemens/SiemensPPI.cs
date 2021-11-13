using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HslCommunication.Serial;
using HslCommunication.Core;
using HslCommunication.Reflection;

namespace HslCommunication.Profinet.Siemens
{
	/// <summary>
	/// 西门子的PPI协议，适用于s7-200plc，注意，由于本类库的每次通讯分成2次操作，内部增加了一个同步锁，所以单次通信时间比较久，另外，地址支持携带站号，例如：s=2;M100<br />
	/// Siemens' PPI protocol is suitable for s7-200plc. Note that since each communication of this class library is divided into two operations, 
	/// and a synchronization lock is added inside, the single communication time is relatively long. In addition, 
	/// the address supports carrying the station number, for example : S=2;M100
	/// </summary>
	/// <remarks>
	/// 适用于西门子200的通信，非常感谢 合肥-加劲 的测试，让本类库圆满完成。注意：M地址范围有限 0-31地址<br />
	/// 在本类的<see cref="SiemensPPIOverTcp"/>实现类里，如果使用了Async的异步方法，没有增加同步锁，多线程调用可能会引发数据错乱的情况。<br />
	/// In the <see cref="SiemensPPIOverTcp"/> implementation class of this class, if the asynchronous method of Async is used, 
	/// the synchronization lock is not added, and multi-threaded calls may cause data disorder.
	/// </remarks>
	public class SiemensPPI : SerialDeviceBase
	{
		#region Constructor

		/// <summary>
		/// 实例化一个西门子的PPI协议对象<br />
		/// Instantiate a Siemens PPI protocol object
		/// </summary>
		public SiemensPPI( )
		{
			this.ByteTransform     = new ReverseBytesTransform( );
			this.WordLength        = 2;
			this.communicationLock = new object( );
		}

		#endregion

		#region Public Properties

		/// <summary>
		/// 西门子PLC的站号信息<br />
		/// Siemens PLC station number information
		/// </summary>
		[HslMqttApi( )]
		public byte Station { get => station; set => station = value; }

		#endregion

		#region Read Write Override

		/// <inheritdoc cref="Helper.SiemensPPIHelper.Read(IReadWriteDevice, string, ushort, byte, object)"/>
		[HslMqttApi( "ReadByteArray", "" )]
		public override OperateResult<byte[]> Read( string address, ushort length ) => Helper.SiemensPPIHelper.Read( this, address, length, this.Station, this.communicationLock );

		/// <inheritdoc cref="Helper.SiemensPPIHelper.ReadBool(IReadWriteDevice, string, ushort, byte, object)"/>
		[HslMqttApi( "ReadBoolArray", "" )]
		public override OperateResult<bool[]> ReadBool( string address, ushort length ) => Helper.SiemensPPIHelper.ReadBool( this, address, length, this.Station, this.communicationLock );

		/// <inheritdoc cref="Helper.SiemensPPIHelper.Write(IReadWriteDevice, string, byte[], byte, object)"/>
		[HslMqttApi( "WriteByteArray", "" )]
		public override OperateResult Write( string address, byte[] value ) => Helper.SiemensPPIHelper.Write( this, address, value, this.Station, this.communicationLock );

		/// <inheritdoc cref="Helper.SiemensPPIHelper.Write(IReadWriteDevice, string, bool[], byte, object)"/>
		[HslMqttApi( "WriteBoolArray", "" )]
		public override OperateResult Write(string address, bool[] value ) => Helper.SiemensPPIHelper.Write( this, address, value, this.Station, this.communicationLock );

		#endregion

		#region Byte Read Write

		/// <summary>
		/// 从西门子的PLC中读取byte数据信息，地址为"M100","AI100","I0","Q0","V100","S100"等，详细请参照API文档<br />
		/// Read byte data information from Siemens PLC. The addresses are "M100", "AI100", "I0", "Q0", "V100", "S100", etc. Please refer to the API documentation for details.
		/// </summary>
		/// <param name="address">西门子的地址数据信息</param>
		/// <returns>带返回结果的结果对象</returns>
		[HslMqttApi( "ReadByte", "" )]
		public OperateResult<byte> ReadByte( string address ) => ByteTransformHelper.GetResultFromArray( Read( address, 1 ) );

		/// <summary>
		/// 向西门子的PLC中写入byte数据，地址为"M100","AI100","I0","Q0","V100","S100"等，详细请参照API文档<br />
		/// Write byte data from Siemens PLC with addresses "M100", "AI100", "I0", "Q0", "V100", "S100", etc. For details, please refer to the API documentation
		/// </summary>
		/// <param name="address">西门子的地址数据信息</param>
		/// <param name="value">数据长度</param>
		/// <returns>带返回结果的结果对象</returns>
		[HslMqttApi( "WriteByte", "向西门子的PLC中写入byte数据，地址为\"M100\",\"AI100\",\"I0\",\"Q0\",\"V100\",\"S100\"等，详细请参照API文档" )]
		public OperateResult Write(string address, byte value ) => Write( address, new byte[] { value } );

		#endregion

		#region Start Stop

		/// <inheritdoc cref="Helper.SiemensPPIHelper.Start(IReadWriteDevice, string, byte, object)"/>
		[HslMqttApi]
		public OperateResult Start( string parameter = "" ) => Helper.SiemensPPIHelper.Start( this, parameter, this.Station, this.communicationLock );

		/// <inheritdoc cref="Helper.SiemensPPIHelper.Stop(IReadWriteDevice, string, byte, object)"/>
		[HslMqttApi]
		public OperateResult Stop( string parameter = "" ) => Helper.SiemensPPIHelper.Stop( this, parameter, this.Station, this.communicationLock );

		#endregion

		#region Private Member

		private byte station = 0x02;            // PLC的站号信息
		private object communicationLock;       // 通讯锁

		#endregion

		#region Object Override

		/// <inheritdoc/>
		public override string ToString( ) => $"SiemensPPI[{PortName}:{BaudRate}]";

		#endregion
	}
}
