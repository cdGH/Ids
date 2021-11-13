using HslCommunication.Core;
using HslCommunication.Core.Address;
using HslCommunication.Serial;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HslCommunication.BasicFramework;
using HslCommunication.Reflection;
using HslCommunication.Profinet.Melsec.Helper;

namespace HslCommunication.Profinet.Melsec
{
	/// <summary>
	/// 基于Qna 兼容3C帧的格式一的通讯，具体的地址需要参照三菱的基本地址<br />
	/// Based on Qna-compatible 3C frame format one communication, the specific address needs to refer to the basic address of Mitsubishi.
	/// </summary>
	/// <remarks>
	/// <inheritdoc cref="MelsecA3CNetOverTcp" path="remarks"/>
	/// </remarks>
	/// <example>
	/// <inheritdoc cref="MelsecA3CNetOverTcp" path="example"/>
	/// </example>
	public class MelsecA3CNet : SerialDeviceBase, IReadWriteA3C
	{
		#region Constructor

		/// <inheritdoc cref="MelsecA3CNetOverTcp()"/>
		public MelsecA3CNet( )
		{
			ByteTransform = new RegularByteTransform( );
			WordLength    = 1;
		}

		#endregion

		#region Public Member

		/// <inheritdoc cref="IReadWriteA3C.Station"/>
		public byte Station { get => station; set => station = value; }

		/// <inheritdoc cref="IReadWriteA3C.SumCheck"/>
		public bool SumCheck { get; set; } = true;

		/// <inheritdoc cref="IReadWriteA3C.Format"/>
		public int Format { get; set; } = 1;

		#endregion

		#region Read Write Support

		/// <inheritdoc cref="MelsecA3CNetHelper.Read(IReadWriteA3C, string, ushort)"/>
		[HslMqttApi( "ReadByteArray", "" )]
		public override OperateResult<byte[]> Read( string address, ushort length ) => MelsecA3CNetHelper.Read( this, address, length );

		/// <inheritdoc cref="MelsecA3CNetHelper.Write(IReadWriteA3C, string, byte[])"/>
		[HslMqttApi( "WriteByteArray", "" )]
		public override OperateResult Write( string address, byte[] value ) => MelsecA3CNetHelper.Write( this, address, value );

		#endregion

		#region Bool Read Write

		/// <inheritdoc cref="MelsecA3CNetHelper.ReadBool(IReadWriteA3C, string, ushort)"/>
		[HslMqttApi( "ReadBoolArray", "" )]
		public override OperateResult<bool[]> ReadBool( string address, ushort length ) => MelsecA3CNetHelper.ReadBool( this, address, length );

		/// <inheritdoc cref="MelsecA3CNetOverTcp.Write(string, bool[])"/>
		[HslMqttApi( "WriteBoolArray", "" )]
		public override OperateResult Write( string address, bool[] value ) => MelsecA3CNetHelper.Write( this, address, value );

		#endregion

		#region Remote Operate

		/// <inheritdoc cref="MelsecA3CNetHelper.RemoteRun"/>
		[HslMqttApi]
		public OperateResult RemoteRun( ) => MelsecA3CNetHelper.RemoteRun( this );

		/// <inheritdoc cref="MelsecA3CNetHelper.RemoteStop"/>
		[HslMqttApi]
		public OperateResult RemoteStop( ) => MelsecA3CNetHelper.RemoteStop( this );

		/// <inheritdoc cref="MelsecA3CNetHelper.ReadPlcType"/>
		[HslMqttApi]
		public OperateResult<string> ReadPlcType( ) => MelsecA3CNetHelper.ReadPlcType( this );

		#endregion

		#region Object Override

		/// <inheritdoc/>
		public override string ToString() => $"MelsecA3CNet[{PortName}:{BaudRate}]";

		#endregion

		#region Private Member

		private byte station = 0x00;                 // PLC的站号信息

		#endregion

	}
}
