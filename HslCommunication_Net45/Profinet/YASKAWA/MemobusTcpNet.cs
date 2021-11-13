using HslCommunication.BasicFramework;
using HslCommunication.Core;
using HslCommunication.Core.IMessage;
using HslCommunication.Core.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
#if !NET20 && !NET35
using System.Threading.Tasks;
#endif

namespace HslCommunication.Profinet.YASKAWA
{
	/// <summary>
	/// 扩展的Memobus协议信息
	/// </summary>
	public class MemobusTcpNet : NetworkDeviceBase
	{
		#region Constructor

		/// <summary>
		/// 实例化一个Memobus-Tcp协议的客户端对象<br />
		/// Instantiate a client object of the Memobus-Tcp protocol
		/// </summary>
		public MemobusTcpNet( )
		{
			this.softIncrementCount = new SoftIncrementCount( byte.MaxValue );
			this.WordLength = 1;
			this.ByteTransform = new ReverseWordTransform( );
		}

		/// <summary>
		/// 指定服务器地址，端口号，客户端自己的站号来初始化<br />
		/// Specify the server address, port number, and client's own station number to initialize
		/// </summary>
		/// <param name="ipAddress">服务器的Ip地址</param>
		/// <param name="port">服务器的端口号</param>
		public MemobusTcpNet( string ipAddress, int port = 502 ) : this( )
		{
			this.IpAddress = ipAddress;
			this.Port = port;
		}

		/// <inheritdoc/>
		protected override INetMessage GetNewNetMessage( ) => new MemobusMessage( );

		/// <inheritdoc/>
		protected override byte[] PackCommandWithHeader( byte[] command )
		{
			byte[] buffer = new byte[12 + command.Length];
			buffer[0] = 0x11;      // 0x11:Memobus指令  0x12:通用信息  0x19:Memobus响应
			buffer[1] = (byte)this.softIncrementCount.GetCurrentValue( );
			buffer[6] = BitConverter.GetBytes( buffer.Length )[0];
			buffer[7] = BitConverter.GetBytes( buffer.Length )[1];
			command.CopyTo( buffer, 12 );
			return buffer;
		}

		/// <inheritdoc/>
		protected override OperateResult<byte[]> UnpackResponseContent( byte[] send, byte[] response )
		{
			return OperateResult.CreateSuccessResult( response.RemoveBegin( 12 ) );
		}

		#endregion

		#region Read Write

		/// <inheritdoc/>
		public override OperateResult<bool[]> ReadBool( string address, ushort length )
		{
			byte sfc = (byte)HslHelper.ExtractParameter( ref address, "x", 0x01 );
			byte[] command = BulidReadCommand( 0x20, sfc, 0x02, 0x01, ushort.Parse( address ), length );

			OperateResult<byte[]> read = ReadFromCoreServer( command );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( read );

			return OperateResult.CreateSuccessResult( read.Content.RemoveBegin( 5 ).ToBoolArray( ).SelectBegin( length ) );
		}

		/// <inheritdoc/>
		public override OperateResult<byte[]> Read( string address, ushort length )
		{
			byte sfc = (byte)HslHelper.ExtractParameter( ref address, "x", 0x03 );
			byte[] command = BulidReadCommand( 0x20, sfc, 0x02, 0x01, ushort.Parse( address ), length );

			OperateResult<byte[]> read = ReadFromCoreServer( command );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( read );

			return OperateResult.CreateSuccessResult( read.Content.RemoveBegin( 5 ) );
		}

		#endregion

		#region Private Member

		private readonly SoftIncrementCount softIncrementCount;              // 自增消息的对象

		#endregion

		#region Object Override

		/// <inheritdoc/>
		public override string ToString( ) => $"MemobusTcpNet[{IpAddress}:{Port}]";

		#endregion

		/// <summary>
		/// 构建读取的命令报文
		/// </summary>
		/// <param name="mfc">主功能码</param>
		/// <param name="sfc">子功能码</param>
		/// <param name="cpuTo">目标的CPU编号</param>
		/// <param name="cpuFrom">发送源CPU编号</param>
		/// <param name="address">起始地址</param>
		/// <param name="length">读取地址长度</param>
		/// <returns>结果报文信息</returns>
		public static byte[] BulidReadCommand( byte mfc, byte sfc, byte cpuTo, byte cpuFrom, ushort address, ushort length )
		{
			byte[] buffer = new byte[9];
			buffer[0] = 0x07;
			buffer[1] = 0x00;
			buffer[2] = mfc;
			buffer[3] = sfc;
			buffer[4] = (byte)((cpuTo << 4) + cpuFrom);
			buffer[5] = BitConverter.GetBytes( address )[1];
			buffer[6] = BitConverter.GetBytes( address )[0];
			buffer[7] = BitConverter.GetBytes( length )[1];
			buffer[8] = BitConverter.GetBytes( length )[0];
			return buffer;
		}


	}
}
