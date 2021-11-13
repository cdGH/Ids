using HslCommunication.Serial;
using HslCommunication.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HslCommunication.BasicFramework;
using HslCommunication.Reflection;

namespace HslCommunication.Profinet.Melsec
{
	/// <summary>
	/// 三菱计算机链接协议，适用FX3U系列，FX3G，FX3S等等系列，通常在PLC侧连接的是485的接线口<br />
	/// Mitsubishi Computer Link Protocol, suitable for FX3U series, FX3G, FX3S, etc., usually the 485 connection port is connected on the PLC side
	/// </summary>
	/// <remarks>
	/// 关于在PLC侧的配置信息，协议：专用协议  传送控制步骤：格式一  站号设置：0
	/// </remarks>
	/// <example>
	/// <inheritdoc cref="MelsecFxLinksOverTcp" path="example"/>
	/// </example>
	public class MelsecFxLinks : SerialDeviceBase
	{
		#region Constructor

		/// <inheritdoc cref="MelsecFxLinksOverTcp()"/>
		public MelsecFxLinks( )
		{
			ByteTransform = new RegularByteTransform( );
			WordLength = 1;
		}

		#endregion

		#region Public Member

		/// <inheritdoc cref="MelsecFxLinksOverTcp.Station"/>
		public byte Station { get => station; set => station = value; }

		/// <inheritdoc cref="MelsecFxLinksOverTcp.WaittingTime"/>
		public byte WaittingTime
		{
			get => watiingTime;
			set
			{
				if (watiingTime > 0x0F)
				{
					watiingTime = 0x0F;
				}
				else
				{
					watiingTime = value;
				}
			}
		}

		/// <inheritdoc cref="MelsecFxLinksOverTcp.SumCheck"/>
		public bool SumCheck { get => sumCheck; set => sumCheck = value; }

		#endregion

		#region Read Write Support

		/// <inheritdoc cref="MelsecFxLinksOverTcp.Read(string, ushort)"/>
		[HslMqttApi( "ReadByteArray", "" )]
		public override OperateResult<byte[]> Read( string address, ushort length )
		{
			byte stat = (byte)HslHelper.ExtractParameter( ref address, "s", this.station );

			// 解析指令
			OperateResult<byte[]> command = MelsecFxLinksOverTcp.BuildReadCommand( stat, address, length, false, sumCheck, watiingTime );
			if (!command.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( command );

			// 核心交互
			OperateResult<byte[]> read = ReadFromCoreServer( command.Content );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( read );

			// 结果验证
			if (read.Content[0] != 0x02) return new OperateResult<byte[]>( read.Content[0], "Read Faild:" + BasicFramework.SoftBasic.ByteToHexString( read.Content, ' ' ) );

			// 提取结果
			byte[] Content = new byte[length * 2];
			for (int i = 0; i < Content.Length / 2; i++)
			{
				ushort tmp = Convert.ToUInt16( Encoding.ASCII.GetString( read.Content, i * 4 + 5, 4 ), 16 );
				BitConverter.GetBytes( tmp ).CopyTo( Content, i * 2 );
			}
			return OperateResult.CreateSuccessResult( Content );
		}

		/// <inheritdoc cref="MelsecFxLinksOverTcp.Write(string, byte[])"/>
		[HslMqttApi( "WriteByteArray", "" )]
		public override OperateResult Write( string address, byte[] value )
		{
			byte stat = (byte)HslHelper.ExtractParameter( ref address, "s", this.station );

			// 解析指令
			OperateResult<byte[]> command = MelsecFxLinksOverTcp.BuildWriteByteCommand( stat, address, value, sumCheck, watiingTime );
			if (!command.IsSuccess) return command;

			// 核心交互
			OperateResult<byte[]> read = ReadFromCoreServer( command.Content );
			if (!read.IsSuccess) return read;
			
			// 结果验证
			if (read.Content[0] != 0x06) return new OperateResult( read.Content[0], "Write Faild:" + BasicFramework.SoftBasic.ByteToHexString( read.Content, ' ' ) );

			// 提取结果
			return OperateResult.CreateSuccessResult( );
		}

		#endregion

		#region Bool Read Write

		/// <inheritdoc cref="MelsecFxLinksOverTcp.ReadBool(string, ushort)"/>
		[HslMqttApi( "ReadBoolArray", "" )]
		public override OperateResult<bool[]> ReadBool( string address, ushort length )
		{
			byte stat = (byte)HslHelper.ExtractParameter( ref address, "s", this.station );

			// 解析指令
			OperateResult<byte[]> command = MelsecFxLinksOverTcp.BuildReadCommand( stat, address, length, true, sumCheck, watiingTime );
			if (!command.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( command );

			// 核心交互
			OperateResult<byte[]> read = ReadFromCoreServer( command.Content );
			if(!read.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( read );

			// 结果验证
			if (read.Content[0] != 0x02) return new OperateResult<bool[]>( read.Content[0], "Read Faild:" + BasicFramework.SoftBasic.ByteToHexString( read.Content, ' ' ) );

			// 提取结果
			byte[] buffer = new byte[length];
			Array.Copy( read.Content, 5, buffer, 0, length );
			return OperateResult.CreateSuccessResult( buffer.Select( m => m == 0x31 ).ToArray( ) );
		}

		/// <inheritdoc cref="MelsecFxLinksOverTcp.Write(string, bool[])"/>
		[HslMqttApi( "WriteBoolArray", "" )]
		public override OperateResult Write( string address, bool[] value )
		{
			byte stat = (byte)HslHelper.ExtractParameter( ref address, "s", this.station );

			// 解析指令
			OperateResult<byte[]> command = MelsecFxLinksOverTcp.BuildWriteBoolCommand( stat, address, value, sumCheck, watiingTime );
			if (!command.IsSuccess) return command;

			// 核心交互
			OperateResult<byte[]> read = ReadFromCoreServer( command.Content );
			if (!read.IsSuccess) return read;

			// 结果验证
			if (read.Content[0] != 0x06) return new OperateResult( read.Content[0], "Write Faild:" + BasicFramework.SoftBasic.ByteToHexString( read.Content, ' ' ) );

			// 提取结果
			return OperateResult.CreateSuccessResult( );
		}

		#endregion

		#region Start Stop

		/// <inheritdoc cref="MelsecFxLinksOverTcp.StartPLC(string)"/>
		[HslMqttApi( Description = "Start the PLC operation, you can carry additional parameter information and specify the station number. Example: s=2; Note: The semicolon is required." )]
		public OperateResult StartPLC( string parameter = "" )
		{
			byte stat = (byte)HslHelper.ExtractParameter( ref parameter, "s", this.station );

			// 解析指令
			OperateResult<byte[]> command = MelsecFxLinksOverTcp.BuildStart( stat, sumCheck, watiingTime );
			if (!command.IsSuccess) return command;

			// 核心交互
			OperateResult<byte[]> read = ReadFromCoreServer( command.Content );
			if (!read.IsSuccess) return read;

			// 结果验证
			if (read.Content[0] != 0x06) return new OperateResult( read.Content[0], "Start Faild:" + SoftBasic.ByteToHexString( read.Content, ' ' ) );

			// 提取结果
			return OperateResult.CreateSuccessResult( );
		}

		/// <inheritdoc cref="MelsecFxLinksOverTcp.StopPLC(string)"/>
		[HslMqttApi( Description = "Stop PLC operation, you can carry additional parameter information and specify the station number. Example: s=2; Note: The semicolon is required." )]
		public OperateResult StopPLC( string parameter = "" )
		{
			byte stat = (byte)HslHelper.ExtractParameter( ref parameter, "s", this.station );

			// 解析指令
			OperateResult<byte[]> command = MelsecFxLinksOverTcp.BuildStop( stat, sumCheck, watiingTime );
			if (!command.IsSuccess) return command;

			// 核心交互
			OperateResult<byte[]> read = ReadFromCoreServer( command.Content );
			if (!read.IsSuccess) return read;

			// 结果验证
			if (read.Content[0] != 0x06) return new OperateResult( read.Content[0], "Stop Faild:" + SoftBasic.ByteToHexString( read.Content, ' ' ) );

			// 提取结果
			return OperateResult.CreateSuccessResult( );
		}

		/// <inheritdoc cref="MelsecFxLinksOverTcp.ReadPlcType(string)"/>
		[HslMqttApi( Description = "Read the PLC model information, you can carry additional parameter information, and specify the station number. Example: s=2; Note: The semicolon is required." )]
		public OperateResult<string> ReadPlcType( string parameter = "" )
		{
			byte stat = (byte)HslHelper.ExtractParameter( ref parameter, "s", this.station );

			// 解析指令
			OperateResult<byte[]> command = MelsecFxLinksOverTcp.BuildReadPlcType( stat, sumCheck, watiingTime );
			if (!command.IsSuccess) return OperateResult.CreateFailedResult<string>( command );

			// 核心交互
			OperateResult<byte[]> read = ReadFromCoreServer( command.Content );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<string>( read );

			// 结果验证
			if (read.Content[0] != 0x06) return new OperateResult<string>( read.Content[0], "ReadPlcType Faild:" + SoftBasic.ByteToHexString( read.Content, ' ' ) );

			// 提取结果
			return MelsecFxLinksOverTcp.GetPlcTypeFromCode( Encoding.ASCII.GetString( read.Content, 5, 2 ) );
		}

		#endregion

		#region Private Member

		private byte station = 0x00;                 // PLC的站号信息
		private byte watiingTime = 0x00;             // 报文的等待时间，设置为0-15
		private bool sumCheck = true;                // 是否启用和校验

		#endregion

		#region Object Override

		/// <inheritdoc/>
		public override string ToString( ) => $"MelsecFxLinks[{PortName}:{BaudRate}]";

		#endregion
	}
}
