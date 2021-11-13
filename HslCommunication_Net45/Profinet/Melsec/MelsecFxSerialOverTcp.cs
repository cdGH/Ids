using HslCommunication.BasicFramework;
using HslCommunication.Core;
using HslCommunication.Core.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HslCommunication.Reflection;
using System.Net.Sockets;
#if !NET35 && !NET20
using System.Threading.Tasks;
#endif

namespace HslCommunication.Profinet.Melsec
{
	/// <summary>
	/// 三菱串口协议的网络版
	/// </summary>
	/// <remarks>
	/// 字读写地址支持的列表如下：
	/// <list type="table">
	///   <listheader>
	///     <term>地址名称</term>
	///     <term>地址代号</term>
	///     <term>示例</term>
	///     <term>地址范围</term>
	///     <term>地址进制</term>
	///     <term>备注</term>
	///   </listheader>
	///   <item>
	///     <term>数据寄存器</term>
	///     <term>D</term>
	///     <term>D100,D200</term>
	///     <term>D0-D511,D8000-D8255</term>
	///     <term>10</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>定时器的值</term>
	///     <term>TN</term>
	///     <term>TN10,TN20</term>
	///     <term>TN0-TN255</term>
	///     <term>10</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>计数器的值</term>
	///     <term>CN</term>
	///     <term>CN10,CN20</term>
	///     <term>CN0-CN199,CN200-CN255</term>
	///     <term>10</term>
	///     <term></term>
	///   </item>
	/// </list>
	/// 位地址支持的列表如下：
	/// <list type="table">
	///   <listheader>
	///     <term>地址名称</term>
	///     <term>地址代号</term>
	///     <term>示例</term>
	///     <term>地址范围</term>
	///     <term>地址进制</term>
	///     <term>备注</term>
	///   </listheader>
	///   <item>
	///     <term>内部继电器</term>
	///     <term>M</term>
	///     <term>M100,M200</term>
	///     <term>M0-M1023,M8000-M8255</term>
	///     <term>10</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>输入继电器</term>
	///     <term>X</term>
	///     <term>X1,X20</term>
	///     <term>X0-X177</term>
	///     <term>8</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>输出继电器</term>
	///     <term>Y</term>
	///     <term>Y10,Y20</term>
	///     <term>Y0-Y177</term>
	///     <term>8</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>步进继电器</term>
	///     <term>S</term>
	///     <term>S100,S200</term>
	///     <term>S0-S999</term>
	///     <term>10</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>定时器触点</term>
	///     <term>TS</term>
	///     <term>TS10,TS20</term>
	///     <term>TS0-TS255</term>
	///     <term>10</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>定时器线圈</term>
	///     <term>TC</term>
	///     <term>TC10,TC20</term>
	///     <term>TC0-TC255</term>
	///     <term>10</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>计数器触点</term>
	///     <term>CS</term>
	///     <term>CS10,CS20</term>
	///     <term>CS0-CS255</term>
	///     <term>10</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>计数器线圈</term>
	///     <term>CC</term>
	///     <term>CC10,CC20</term>
	///     <term>CC0-CC255</term>
	///     <term>10</term>
	///     <term></term>
	///   </item>
	/// </list>
	/// </remarks>
	/// <example>
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Profinet\MelsecFxSerial.cs" region="Usage" title="简单的使用" />
	/// </example>
	public class MelsecFxSerialOverTcp : NetworkDeviceBase
	{
		#region Constructor

		/// <summary>
		/// 实例化网络版的三菱的串口协议的通讯对象<br />
		/// Instantiate the communication object of Mitsubishi's serial protocol on the network
		/// </summary>
		public MelsecFxSerialOverTcp( )
		{
			this.WordLength    = 1;
			this.ByteTransform = new RegularByteTransform( );
			this.IsNewVersion  = true;
			this.ByteTransform.IsStringReverseByteWord = true;
			this.SleepTime     = 20;
		}

		/// <summary>
		/// 指定ip地址及端口号来实例化三菱的串口协议的通讯对象<br />
		/// Specify the IP address and port number to instantiate the communication object of Mitsubishi's serial protocol
		/// </summary>
		/// <param name="ipAddress">Ip地址</param>
		/// <param name="port">端口号</param>
		public MelsecFxSerialOverTcp( string ipAddress, int port ) : this( )
		{
			this.IpAddress     = ipAddress;
			this.Port          = port;
		}

		#endregion

		#region ReadFromServer

		/// <inheritdoc/>
		public override OperateResult<byte[]> ReadFromCoreServer( Socket socket, byte[] send, bool hasResponseData = true, bool usePackAndUnpack = true )
		{
			OperateResult<byte[]> read = base.ReadFromCoreServer( socket, send, hasResponseData, usePackAndUnpack );
			if (!read.IsSuccess) return read;

			if (read.Content == null) return read;
			if (read.Content.Length > 2) return read;

			OperateResult<byte[]> read2 = base.ReadFromCoreServer( socket, send, hasResponseData, usePackAndUnpack );
			if (!read2.IsSuccess) return read2;

			return OperateResult.CreateSuccessResult( SoftBasic.SpliceArray( read.Content, read2.Content ) );
		}
#if !NET20 && !NET35
		/// <inheritdoc/>
		public override async Task<OperateResult<byte[]>> ReadFromCoreServerAsync( Socket socket, byte[] send, bool hasResponseData = true, bool usePackAndUnpack = true )
		{
			OperateResult<byte[]> read = await base.ReadFromCoreServerAsync( socket, send, hasResponseData, usePackAndUnpack );
			if (!read.IsSuccess) return read;

			if (read.Content == null) return read;
			if (read.Content.Length > 2) return read;

			OperateResult<byte[]> read2 = await base.ReadFromCoreServerAsync( socket, send, hasResponseData, usePackAndUnpack );
			if (!read2.IsSuccess) return read2;

			return OperateResult.CreateSuccessResult( SoftBasic.SpliceArray( read.Content, read2.Content ) );
		}
#endif
		#endregion

		/// <summary>
		/// 当前的编程口协议是否为新版，默认为新版，如果无法读取，切换旧版再次尝试<br />
		/// Whether the current programming port protocol is the new version, the default is the new version, 
		/// if it cannot be read, switch to the old version and try again
		/// </summary>
		public bool IsNewVersion { get; set; }

		#region Read Write Byte

		/// <inheritdoc cref="Helper.MelsecFxSerialHelper.Read(IReadWriteDevice, string, ushort, bool)"/>
		[HslMqttApi( "ReadByteArray", "" )]
		public override OperateResult<byte[]> Read( string address, ushort length ) => 
			Helper.MelsecFxSerialHelper.Read( this, address, length, IsNewVersion );

		/// <inheritdoc cref="Helper.MelsecFxSerialHelper.Write(IReadWriteDevice, string, byte[], bool)"/>
		[HslMqttApi( "WriteByteArray", "" )]
		public override OperateResult Write( string address, byte[] value ) => 
			Helper.MelsecFxSerialHelper.Write( this, address, value, IsNewVersion );

		#endregion

		#region Async Read Write Byte
#if !NET35 && !NET20
		/// <inheritdoc cref="Read(string, ushort)"/>
		public override async Task<OperateResult<byte[]>> ReadAsync( string address, ushort length ) =>
			await Helper.MelsecFxSerialHelper.ReadAsync( this, address, length, IsNewVersion );

		/// <inheritdoc cref="Write(string, byte[])"/>
		public override async Task<OperateResult> WriteAsync( string address, byte[] value ) =>
			await Helper.MelsecFxSerialHelper.WriteAsync( this, address, value, IsNewVersion );
#endif
		#endregion

		#region Read Write Bool

		/// <inheritdoc cref="Helper.MelsecFxSerialHelper.ReadBool(IReadWriteDevice, string, ushort)"/>
		[HslMqttApi( "ReadBoolArray", "" )]
		public override OperateResult<bool[]> ReadBool( string address, ushort length ) => 
			Helper.MelsecFxSerialHelper.ReadBool( this, address, length );

		/// <inheritdoc cref="Helper.MelsecFxSerialHelper.Write(IReadWriteDevice, string, bool)"/>
		[HslMqttApi( "WriteBool", "" )]
		public override OperateResult Write( string address, bool value ) =>
			Helper.MelsecFxSerialHelper.Write( this, address, value );

		#endregion

		#region Async Read Write Bool
#if !NET35 && !NET20
		/// <inheritdoc cref="ReadBool(string, ushort)"/>
		public override async Task<OperateResult<bool[]>> ReadBoolAsync( string address, ushort length ) =>
			await Helper.MelsecFxSerialHelper.ReadBoolAsync( this, address, length );

		/// <inheritdoc cref="Write(string, bool)"/>
		public override async Task<OperateResult> WriteAsync( string address, bool value ) =>
			await Helper.MelsecFxSerialHelper.WriteAsync( this, address, value );
#endif
		#endregion

		#region Object Override

		///<inheritdoc/>
		public override string ToString( ) => $"MelsecFxSerialOverTcp[{IpAddress}:{Port}]";

		#endregion

	}
}
