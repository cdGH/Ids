using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HslCommunication.Core.Net;
using HslCommunication.Core.IMessage;
using HslCommunication.Core;
using System.Net.Sockets;

namespace HslCommunication.Profinet.OpenProtocol
{
	/// <summary>
	/// 开放以太网协议，仍然在开发中<br />
	/// Open Ethernet protocol, still under development
	/// </summary>
	public class OpenProtocolNet : NetworkDoubleBase
	{
		#region Constructor

		/// <summary>
		/// 实例化一个默认的对象<br />
		/// Instantiate a default object
		/// </summary>
		public OpenProtocolNet( ) 
		{
			this.ByteTransform = new RegularByteTransform( );
		}

		/// <summary>
		/// 使用指定的IP地址及端口来初始化对象<br />
		/// Use the specified IP address and port to initialize the object
		/// </summary>
		/// <param name="ipAddress">Ip地址</param>
		/// <param name="port">端口号</param>
		public OpenProtocolNet( string ipAddress, int port )
		{
			this.IpAddress     = ipAddress;
			this.Port          = port;
			this.ByteTransform = new RegularByteTransform( );
		}

		/// <inheritdoc/>
		protected override INetMessage GetNewNetMessage( ) => new OpenProtocolMessage( );

		#endregion

		#region Override NetworkDoubleBase

		/// <inheritdoc/>
		protected override OperateResult InitializationOnConnect( Socket socket )
		{
			// 此处的 revison 应该等于多少？
			OperateResult<string> open = ReadCustomer( 1, 0, 0, 0, null );
			if (!open.IsSuccess) return open;

			if (open.Content.Substring( 4, 4 ) == "0002")
				return OperateResult.CreateSuccessResult( );
			else
				return new OperateResult( "Failed:" + open.Content.Substring( 4, 4 ) );
		}

		#endregion

		/// <summary>
		/// 自定义的命令读取
		/// </summary>
		/// <param name="mid"></param>
		/// <param name="revison"></param>
		/// <param name="stationId"></param>
		/// <param name="spindleId"></param>
		/// <param name="parameters"></param>
		/// <returns></returns>
		public OperateResult<string> ReadCustomer( int mid, int revison, int stationId, int spindleId, List<string> parameters )
		{
			if (parameters != null) parameters = new List<string>( );
			OperateResult<byte[]> command = BuildReadCommand( mid, revison, stationId, spindleId, parameters );
			if (!command.IsSuccess) return OperateResult.CreateFailedResult<string>( command );

			OperateResult<byte[]> read = ReadFromCoreServer( command.Content );
			if(!read.IsSuccess) return OperateResult.CreateFailedResult<string>( read );

			return OperateResult.CreateSuccessResult( Encoding.ASCII.GetString( read.Content ) );
		}

		#region Object Override

		/// <inheritdoc/>
		public override string ToString( )
		{
			return $"OpenProtocolNet[{IpAddress}:{Port}]";
		}

		#endregion

		/// <summary>
		/// 构建一个读取的初始报文
		/// </summary>
		/// <param name="mid"></param>
		/// <param name="revison"></param>
		/// <param name="stationId"></param>
		/// <param name="spindleId"></param>
		/// <param name="parameters"></param>
		/// <returns></returns>
		public static OperateResult<byte[]> BuildReadCommand( int mid, int revison, int stationId, int spindleId, List<string> parameters )
		{
			if (mid < 0 || mid > 9999) return new OperateResult<byte[]>( "Mid must be between 0 - 9999" );
			if (revison < 0 || revison > 999) return new OperateResult<byte[]>( "revison must be between 0 - 999" );
			if (stationId < 0 || stationId > 9) return new OperateResult<byte[]>( "stationId must be between 0 - 9" );
			if (spindleId < 0 || spindleId > 99) return new OperateResult<byte[]>( "spindleId must be between 0 - 99" );

			int count = 0;
			if (parameters != null)
				parameters.ForEach( m => count += m.Length );

			StringBuilder sb = new StringBuilder( );
			sb.Append( (20 + count).ToString( "D4" ) );
			sb.Append( mid.ToString( "D4" ) );
			sb.Append( revison.ToString( "D3" ) );
			sb.Append( '\0' );
			sb.Append( stationId.ToString( "D1" ) );
			sb.Append( spindleId.ToString( "D2" ) );
			sb.Append( '\0' );
			sb.Append( '\0' );
			sb.Append( '\0' );
			sb.Append( '\0' );
			sb.Append( '\0' );

			if (parameters != null)
				for (int i = 0; i < parameters.Count; i++)
				{
					sb.Append( parameters[i] );
				}
			sb.Append( '\0' );
			return OperateResult.CreateSuccessResult( Encoding.ASCII.GetBytes( sb.ToString( ) ) );
		}

	}
}
