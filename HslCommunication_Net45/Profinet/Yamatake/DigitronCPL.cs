using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HslCommunication.Serial;
using HslCommunication.Core;
using System.IO;
#if !NET20 && !NET35
using System.Threading.Tasks;
#endif

namespace HslCommunication.Profinet.Yamatake
{
	/// <summary>
	/// 日本山武的数字指示调节器，目前适配SDC40B
	/// </summary>
	public class DigitronCPL : SerialDeviceBase
	{
		#region Constructor

		/// <summary>
		/// 实例化一个默认的对象
		/// </summary>
		public DigitronCPL( )
		{
			Station       = 1;
			WordLength    = 1;
			ByteTransform = new RegularByteTransform( );
		}

		#endregion

		#region Public Properties

		/// <summary>
		/// 获取或设置当前的站号信息
		/// </summary>
		public byte Station { get; set; }

		/// <inheritdoc/>
		protected override bool CheckReceiveDataComplete( MemoryStream ms )
		{
			byte[] buffer = ms.ToArray( );
			if (buffer.Length > 5) return buffer[buffer.Length - 2] == 0x0D && buffer[buffer.Length - 1] == 0x0A;
			return base.CheckReceiveDataComplete( ms );
		}

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

		#region Object Override

		/// <inheritdoc/>
		public override string ToString( ) => $"DigitronCPL[{this.PortName}:{this.BaudRate}]";

		#endregion
	}
}
