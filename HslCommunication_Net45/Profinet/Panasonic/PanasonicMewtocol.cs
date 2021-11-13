using HslCommunication.Core;
using HslCommunication.Serial;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HslCommunication.Reflection;
using System.IO;
#if !NET35 && !NET20
using System.Threading.Tasks;
#endif

namespace HslCommunication.Profinet.Panasonic
{
	/// <summary>
	/// 松下PLC的数据交互协议，采用Mewtocol协议通讯，支持的地址列表参考api文档<br />
	/// The data exchange protocol of Panasonic PLC adopts Mewtocol protocol for communication. For the list of supported addresses, refer to the api document.
	/// </summary>
	/// <remarks>
	/// 地址支持携带站号的访问方式，例如：s=2;D100
	/// </remarks>
	/// <example>
	/// <inheritdoc cref="PanasonicMewtocolOverTcp" path="example"/>
	/// </example>
	public class PanasonicMewtocol : SerialDeviceBase
	{
		#region Constructor

		/// <inheritdoc cref="PanasonicMewtocolOverTcp(byte)"/>
		public PanasonicMewtocol( byte station = 238 )
		{
			this.ByteTransform            = new RegularByteTransform( );
			this.Station                  = station;
			this.ByteTransform.DataFormat = DataFormat.DCBA;
		}

		#endregion

		#region Public Properties
		
		/// <inheritdoc cref="PanasonicMewtocolOverTcp.Station"/>
		public byte Station { get; set; }

		/// <inheritdoc/>
		protected override bool CheckReceiveDataComplete( MemoryStream ms )
		{
			byte[] buffer = ms.ToArray( );
			if (buffer.Length > 5) return buffer[buffer.Length - 1] == 0x0D;
			return false;
		}
		#endregion

		#region Read Write Override

		/// <inheritdoc cref="PanasonicMewtocolOverTcp.Read(string, ushort)"/>
		[HslMqttApi( "ReadByteArray", "" )]
		public override OperateResult<byte[]> Read( string address, ushort length )
		{
			byte station = (byte)HslHelper.ExtractParameter( ref address, "s", this.Station );

			// 创建指令
			OperateResult<byte[]> command = PanasonicHelper.BuildReadCommand( station, address, length );
			if (!command.IsSuccess) return command;

			// 数据交互
			OperateResult<byte[]> read = ReadFromCoreServer( command.Content );
			if (!read.IsSuccess) return read;

			// 提取数据
			return PanasonicHelper.ExtraActualData( read.Content );
		}

		/// <inheritdoc cref="PanasonicMewtocolOverTcp.Write(string, byte[])"/>
		[HslMqttApi( "WriteByteArray", "" )]
		public override OperateResult Write( string address, byte[] value )
		{
			byte station = (byte)HslHelper.ExtractParameter( ref address, "s", this.Station );

			// 创建指令
			OperateResult<byte[]> command = PanasonicHelper.BuildWriteCommand( station, address, value );
			if (!command.IsSuccess) return command;

			// 数据交互
			OperateResult<byte[]> read = ReadFromCoreServer( command.Content );
			if (!read.IsSuccess) return read;

			// 提取结果
			return PanasonicHelper.ExtraActualData( read.Content );
		}

		#endregion

		#region Read Write Bool

		/// <inheritdoc cref="PanasonicMewtocolOverTcp.ReadBool(string, ushort)"/>
		[HslMqttApi( "ReadBoolArray", "" )]
		public override OperateResult<bool[]> ReadBool( string address, ushort length )
		{
			byte station = (byte)HslHelper.ExtractParameter( ref address, "s", this.Station );

			OperateResult<string, int> analysis = PanasonicHelper.AnalysisAddress( address );
			if (!analysis.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( analysis );

			// 创建指令
			OperateResult<byte[]> command = PanasonicHelper.BuildReadCommand( station, address, length );
			if (!command.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( command );

			// 数据交互
			OperateResult<byte[]> read = ReadFromCoreServer( command.Content );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( read );

			// 提取数据
			OperateResult<byte[]> extra = PanasonicHelper.ExtraActualData( read.Content );
			if (!extra.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( extra );

			// 提取bool
			return OperateResult.CreateSuccessResult( BasicFramework.SoftBasic.ByteToBoolArray(
				extra.Content ).SelectMiddle( analysis.Content2 % 16, length ) );
		}

		/// <inheritdoc cref="PanasonicMewtocolOverTcp.ReadBool(string)"/>
		[HslMqttApi( "ReadBool", "" )]
		public override OperateResult<bool> ReadBool( string address )
		{
			byte station = (byte)HslHelper.ExtractParameter( ref address, "s", this.Station );

			// 创建指令
			OperateResult<byte[]> command = PanasonicHelper.BuildReadOneCoil( station, address );
			if (!command.IsSuccess) return OperateResult.CreateFailedResult<bool>( command );

			// 数据交互
			OperateResult<byte[]> read = ReadFromCoreServer( command.Content );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<bool>( read );

			// 提取数据
			return PanasonicHelper.ExtraActualBool( read.Content );
		}

		/// <inheritdoc cref="PanasonicMewtocolOverTcp.Write(string, bool[])"/>
		[HslMqttApi( "WriteBoolArray", "" )]
		public override OperateResult Write( string address, bool[] values )
		{
			byte station = (byte)HslHelper.ExtractParameter( ref address, "s", this.Station );

			// 强制地址从字单位开始，强制写入长度为16个长度
			OperateResult<string, int> analysis = PanasonicHelper.AnalysisAddress( address );
			if (!analysis.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( analysis );

			if (analysis.Content2 % 16 != 0) return new OperateResult( StringResources.Language.PanasonicAddressBitStartMulti16 );
			if (values.Length % 16 != 0) return new OperateResult( StringResources.Language.PanasonicBoolLengthMulti16 );

			// 计算字节数据
			byte[] buffer = BasicFramework.SoftBasic.BoolArrayToByte( values );

			// 创建指令
			OperateResult<byte[]> command = PanasonicHelper.BuildWriteCommand( station, address, buffer );
			if (!command.IsSuccess) return command;

			// 数据交互
			OperateResult<byte[]> read = ReadFromCoreServer( command.Content );
			if (!read.IsSuccess) return read;

			// 提取结果
			return PanasonicHelper.ExtraActualData( read.Content );
		}

		/// <inheritdoc cref="PanasonicMewtocolOverTcp.Write(string, bool)"/>
		[HslMqttApi( "WriteBool", "" )]
		public override OperateResult Write( string address, bool value )
		{
			byte station = (byte)HslHelper.ExtractParameter( ref address, "s", this.Station );

			// 创建指令
			OperateResult<byte[]> command = PanasonicHelper.BuildWriteOneCoil( station, address, value );
			if (!command.IsSuccess) return command;

			// 数据交互
			OperateResult<byte[]> read = ReadFromCoreServer( command.Content );
			if (!read.IsSuccess) return read;

			// 提取结果
			return PanasonicHelper.ExtraActualData( read.Content );
		}

		#endregion

		#region Async Read Write Bool
#if !NET35 && !NET20
		/// <inheritdoc cref="ReadBool(string)"/>
		public async override Task<OperateResult<bool>> ReadBoolAsync( string address ) => await Task.Run( ( ) => ReadBool( address ) );

		/// <inheritdoc cref="Write(string, bool)"/>
		public async override Task<OperateResult> WriteAsync( string address, bool value ) => await Task.Run( ( ) => Write( address, value ) );
#endif
		#endregion

		#region Object Override

		/// <inheritdoc/>
		public override string ToString( ) => $"Panasonic Mewtocol[{PortName}:{BaudRate}]";

		#endregion
	}
}
