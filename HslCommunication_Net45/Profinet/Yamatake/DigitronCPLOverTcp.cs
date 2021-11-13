using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HslCommunication.Core;
using HslCommunication.Core.Net;
using System.Net.Sockets;
using HslCommunication.Core.IMessage;
#if !NET20 && !NET35
using System.Threading.Tasks;
#endif

namespace HslCommunication.Profinet.Yamatake
{
	/// <summary>
	/// 山武的数字指定调节器的通信协议，基于CPL转网口的实现，测试型号 SDC40B
	/// </summary>
	public class DigitronCPLOverTcp : NetworkDeviceBase
	{
		#region Constructor

		/// <summary>
		/// 实例化一个默认的对象
		/// </summary>
		public DigitronCPLOverTcp( )
		{
			Station = 1;
			WordLength = 1;
			ByteTransform = new RegularByteTransform( );
		}

		#endregion

		#region Public Properties

		/// <summary>
		/// 获取或设置当前的站号信息
		/// </summary>
		public byte Station { get; set; }

		#endregion

		#region Override

		/// <inheritdoc/>
		protected override OperateResult<byte[]> ReceiveByMessage( Socket socket, int timeOut, INetMessage netMessage, Action<long, long> reportProgress = null )
		{
			return ReceiveCommandLineFromSocket( socket, 0x0D, 0x0A, timeOut );
		}

#if !NET20 && !NET35

		/// <inheritdoc/>
		protected async override Task<OperateResult<byte[]>> ReceiveByMessageAsync( Socket socket, int timeOut, INetMessage netMessage, Action<long, long> reportProgress = null )
		{
			return await ReceiveCommandLineFromSocketAsync( socket, 0x0D, 0x0A, timeOut );
		}
#endif

		#endregion

		#region Read Write

		/// <inheritdoc/>
		public override OperateResult<byte[]> Read( string address, ushort length )
		{
			byte station = (byte)HslHelper.ExtractParameter( ref address, "s", this.Station );
			OperateResult<byte[]> command = Helper.DigitronCPLHelper.BuildReadCommand( station, address, length );
			if (!command.IsSuccess) return command;

			OperateResult<byte[]> read = ReadFromCoreServer( command.Content );
			if (!read.IsSuccess) return read;

			return Helper.DigitronCPLHelper.ExtraActualResponse( read.Content );
		}

		/// <inheritdoc/>
		public override OperateResult Write( string address, byte[] value )
		{
			byte station = (byte)HslHelper.ExtractParameter( ref address, "s", this.Station );
			OperateResult<byte[]> command = Helper.DigitronCPLHelper.BuildWriteCommand( station, address, value );
			if (!command.IsSuccess) return command;

			OperateResult<byte[]> read = ReadFromCoreServer( command.Content );
			if (!read.IsSuccess) return read;

			return Helper.DigitronCPLHelper.ExtraActualResponse( read.Content );
		}

		#endregion

#if !NET20 && !NET35

		/// <inheritdoc/>
		public async override Task<OperateResult<byte[]>> ReadAsync( string address, ushort length )
		{
			byte station = (byte)HslHelper.ExtractParameter( ref address, "s", this.Station );
			OperateResult<byte[]> command = Helper.DigitronCPLHelper.BuildReadCommand( station, address, length );
			if (!command.IsSuccess) return command;

			OperateResult<byte[]> read = await ReadFromCoreServerAsync( command.Content );
			if (!read.IsSuccess) return read;

			return Helper.DigitronCPLHelper.ExtraActualResponse( read.Content );
		}

		/// <inheritdoc/>
		public async override Task<OperateResult> WriteAsync( string address, byte[] value )
		{
			byte station = (byte)HslHelper.ExtractParameter( ref address, "s", this.Station );
			OperateResult<byte[]> command = Helper.DigitronCPLHelper.BuildWriteCommand( station, address, value );
			if (!command.IsSuccess) return command;

			OperateResult<byte[]> read = await ReadFromCoreServerAsync( command.Content );
			if (!read.IsSuccess) return read;

			return Helper.DigitronCPLHelper.ExtraActualResponse( read.Content );
		}
#endif
		#region Object Override

		/// <inheritdoc/>
		public override string ToString( ) => $"DigitronCPLOverTcp[{this.IpAddress}:{this.Port}]";

		#endregion
	}
}
