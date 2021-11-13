using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HslCommunication.Core.Net;
using HslCommunication.Core;
using System.Net;
using HslCommunication.Reflection;
#if !NET35 && !NET20
using System.Threading.Tasks;
#endif

namespace HslCommunication.Profinet.Omron
{
	/// <summary>
	/// 欧姆龙的Udp协议的实现类，地址类型和Fins-TCP一致，无连接的实现，可靠性不如<see cref="OmronFinsNet"/><br />
	/// Omron's Udp protocol implementation class, the address type is the same as Fins-TCP, 
	/// and the connectionless implementation is not as reliable as <see cref="OmronFinsNet"/>
	/// </summary>
	/// <remarks>
	/// <inheritdoc cref="OmronFinsNet" path="remarks"/>
	/// </remarks>
	public class OmronFinsUdp : NetworkUdpDeviceBase
	{
		#region Constructor

		/// <inheritdoc cref="OmronFinsNet(string, int)"/>
		public OmronFinsUdp(string ipAddress, int port ) : this( )
		{
			this.IpAddress                             = ipAddress;
			this.Port                                  = port;
		}

		/// <inheritdoc cref="OmronFinsNet()"/>
		public OmronFinsUdp( )
		{
			this.WordLength                            = 1;
			this.ByteTransform                         = new ReverseWordTransform( );
			this.ByteTransform.DataFormat              = DataFormat.CDAB;
			this.ByteTransform.IsStringReverseByteWord = true;
		}

		#endregion

		#region IpAddress Override

		/// <inheritdoc/>
		public override string IpAddress {
			get => base.IpAddress;
			set
			{
				base.IpAddress = value;
				DA1 = Convert.ToByte( base.IpAddress.Substring( base.IpAddress.LastIndexOf( "." ) + 1 ) );
			}
		}

		#endregion

		#region Public Member

		/// <inheritdoc cref="OmronFinsNet.ICF"/>
		public byte ICF { get; set; } = 0x80;

		/// <inheritdoc cref="OmronFinsNet.RSV"/>
		public byte RSV { get; private set; } = 0x00;

		/// <inheritdoc cref="OmronFinsNet.GCT"/>
		public byte GCT { get; set; } = 0x02;

		/// <inheritdoc cref="OmronFinsNet.DNA"/>
		public byte DNA { get; set; } = 0x00;

		/// <inheritdoc cref="OmronFinsNet.DA1"/>
		public byte DA1 { get; set; } = 0x13;

		/// <inheritdoc cref="OmronFinsNet.DA2"/>
		public byte DA2 { get; set; } = 0x00;

		/// <inheritdoc cref="OmronFinsNet.SNA"/>
		public byte SNA { get; set; } = 0x00;

		/// <inheritdoc cref="OmronFinsNet.SA1"/>
		public byte SA1 { get; set; } = 13;

		/// <inheritdoc cref="OmronFinsNet.SA2"/>
		public byte SA2 { get; set; }

		/// <inheritdoc cref="OmronFinsNet.SID"/>
		public byte SID { get; set; } = 0x00;

		/// <inheritdoc cref="OmronFinsNet.ReadSplits"/>
		public int ReadSplits { get; set; } = 500;

		#endregion

		#region Build Command

		/// <inheritdoc cref="OmronFinsNet.PackCommand(byte[])"/>
		private byte[] PackCommand( byte[] cmd )
		{
			byte[] buffer = new byte[10 + cmd.Length];
			buffer[0] = ICF;
			buffer[1] = RSV;
			buffer[2] = GCT;
			buffer[3] = DNA;
			buffer[4] = DA1;
			buffer[5] = DA2;
			buffer[6] = SNA;
			buffer[7] = SA1;
			buffer[8] = SA2;
			buffer[9] = SID;
			cmd.CopyTo( buffer, 10 );

			return buffer;
		}

		#endregion

		#region Message Pack Extra

		/// <inheritdoc/>
		protected override byte[] PackCommandWithHeader( byte[] command ) => PackCommand( command );

		/// <inheritdoc/>
		protected override OperateResult<byte[]> UnpackResponseContent( byte[] send, byte[] response ) => OmronFinsNetHelper.UdpResponseValidAnalysis( response );

		#endregion

		#region Read Write Support

		/// <inheritdoc cref="OmronFinsNet.Read(string, ushort)"/>
		[HslMqttApi( "ReadByteArray", "" )]
		public override OperateResult<byte[]> Read( string address, ushort length ) => OmronFinsNetHelper.Read( this, address, length, ReadSplits );

		/// <inheritdoc cref="OmronFinsNet.Write(string, byte[])"/>
		[HslMqttApi( "WriteByteArray", "" )]
		public override OperateResult Write( string address, byte[] value ) => OmronFinsNetHelper.Write( this, address, value );

		/// <inheritdoc/>
		[HslMqttApi( "ReadString", "" )]
		public override OperateResult<string> ReadString( string address, ushort length )
		{
			return base.ReadString( address, length, Encoding.UTF8 );
		}

		/// <inheritdoc/>
		[HslMqttApi( "WriteString", "" )]
		public override OperateResult Write( string address, string value )
		{
			return base.Write( address, value, Encoding.UTF8 );
		}
		#endregion

		#region Read Write bool

		/// <inheritdoc cref="OmronFinsNet.ReadBool(string, ushort)"/>
		[HslMqttApi( "ReadBoolArray", "" )]
		public override OperateResult<bool[]> ReadBool( string address, ushort length ) => OmronFinsNetHelper.ReadBool( this, address, length, ReadSplits );

		/// <inheritdoc cref="OmronFinsNet.Write(string, bool[])"/>
		[HslMqttApi( "WriteBoolArray", "" )]
		public override OperateResult Write( string address, bool[] values ) => OmronFinsNetHelper.Write( this, address, values );

		#endregion

		#region Advanced Api

		/// <inheritdoc cref="OmronFinsNetHelper.Run(IReadWriteDevice)"/>
		[HslMqttApi( ApiTopic = "Run", Description = "将CPU单元的操作模式更改为RUN，从而使PLC能够执行其程序。" )]
		public OperateResult Run( ) => OmronFinsNetHelper.Run( this );

		/// <inheritdoc cref="OmronFinsNetHelper.Stop(IReadWriteDevice)"/>
		[HslMqttApi( ApiTopic = "Stop", Description = "将CPU单元的操作模式更改为PROGRAM，停止程序执行。" )]
		public OperateResult Stop( ) => OmronFinsNetHelper.Stop( this );

		/// <inheritdoc cref="OmronFinsNetHelper.ReadCpuUnitData(IReadWriteDevice)"/>
		[HslMqttApi( ApiTopic = "ReadCpuUnitData", Description = "读取CPU的一些数据信息，主要包含型号，版本，一些数据块的大小。" )]
		public OperateResult<OmronCpuUnitData> ReadCpuUnitData( ) => OmronFinsNetHelper.ReadCpuUnitData( this );

		/// <inheritdoc cref="OmronFinsNetHelper.ReadCpuUnitStatus(IReadWriteDevice)"/>
		[HslMqttApi( ApiTopic = "ReadCpuUnitStatus", Description = "读取CPU单元的一些操作状态数据，主要包含运行状态，工作模式，错误信息等。" )]
		public OperateResult<OmronCpuUnitStatus> ReadCpuUnitStatus( ) => OmronFinsNetHelper.ReadCpuUnitStatus( this );

		#endregion

		#region Object Override

		/// <inheritdoc/>
		public override string ToString( ) => $"OmronFinsUdp[{IpAddress}:{Port}]";
 
		#endregion
	}
}
