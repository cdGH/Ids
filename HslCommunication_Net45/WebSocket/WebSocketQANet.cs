using HslCommunication.Core;
using HslCommunication.Core.IMessage;
using HslCommunication.Core.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
#if !NET35 && !NET20
using System.Threading.Tasks;
#endif

namespace HslCommunication.WebSocket
{
	/// <summary>
	/// WebSocket的问答机制的客户端，本客户端将会在请求头上追加 RequestAndAnswer: true，本客户端将会请求服务器的信息，然后等待服务器的返回<br />
	/// Client of WebSocket Q &amp; A mechanism, this client will append RequestAndAnswer: true to the request header, this client will request the server information, and then wait for the server to return
	/// </summary>
	public class WebSocketQANet : NetworkDoubleBase
	{
		#region Constructor

		/// <summary>
		/// 根据指定的ip地址及端口号，实例化一个默认的对象<br />
		/// Instantiates a default object based on the specified IP address and port number
		/// </summary>
		/// <param name="ipAddress">远程服务器的ip地址</param>
		/// <param name="port">端口号信息</param>
		public WebSocketQANet( string ipAddress, int port )
		{
			IpAddress     = HslHelper.GetIpAddressFromInput( ipAddress );
			Port          = port;
			ByteTransform = new RegularByteTransform( );
		}

		#endregion

		#region Override InitializationOnConnect

		/// <inheritdoc/>
		protected override OperateResult InitializationOnConnect( Socket socket )
		{
			byte[] command = WebSocketHelper.BuildWsQARequest( this.IpAddress, this.Port );
			// 发送连接的报文信息
			OperateResult send = Send( socket, command );
			if (!send.IsSuccess) return send;

			// 接收服务器反馈的信息
			OperateResult<byte[]> rece = Receive( socket, -1, 10_000 );
			if (!rece.IsSuccess) return rece;

			return OperateResult.CreateSuccessResult( );
		}

		///<inheritdoc/>
		public override OperateResult<byte[]> ReadFromCoreServer( Socket socket, byte[] send, bool hasResponseData = true, bool usePackHeader = true )
		{
			LogNet?.WriteDebug( ToString( ), StringResources.Language.Send + " : " + BasicFramework.SoftBasic.ByteToHexString( send, ' ' ) );

			OperateResult sendResult = Send( socket, send );
			if (!sendResult.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( sendResult );

			// 接收超时时间大于0时才允许接收远程的数据
			if (ReceiveTimeOut < 0) return OperateResult.CreateSuccessResult( new byte[0] );

			OperateResult<WebSocketMessage> read = ReceiveWebSocketPayload( socket );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( read );

			LogNet?.WriteDebug( ToString( ), $"{StringResources.Language.Receive} : OpCode[{read.Content.OpCode}] Mask[{read.Content.HasMask}] {BasicFramework.SoftBasic.ByteToHexString( read.Content.Payload, ' ' )}" );

			// 接收数据信息
			return OperateResult.CreateSuccessResult( read.Content.Payload );
		}

		#endregion

		#region Override InitializationOnConnect Async
#if !NET35 && !NET20
		/// <inheritdoc/>
		protected async override Task<OperateResult> InitializationOnConnectAsync( Socket socket )
		{
			byte[] command = WebSocketHelper.BuildWsQARequest( this.IpAddress, this.Port );
			// 发送连接的报文信息
			OperateResult send = await SendAsync( socket, command );
			if (!send.IsSuccess) return send;

			// 接收服务器反馈的信息
			OperateResult<byte[]> rece = await ReceiveAsync( socket, -1, 10_000 );
			if (!rece.IsSuccess) return rece;

			return OperateResult.CreateSuccessResult( );
		}

		/// <inheritdoc/>
		public async override Task<OperateResult<byte[]>> ReadFromCoreServerAsync( Socket socket, byte[] send, bool hasResponseData = true, bool usePackHeader = true )
		{
			LogNet?.WriteDebug( ToString( ), StringResources.Language.Send + " : " + BasicFramework.SoftBasic.ByteToHexString( send, ' ' ) );

			OperateResult sendResult = await SendAsync( socket, send );
			if (!sendResult.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( sendResult );

			// 接收超时时间大于0时才允许接收远程的数据
			if (ReceiveTimeOut < 0) return OperateResult.CreateSuccessResult( new byte[0] );

			OperateResult<WebSocketMessage> read = await ReceiveWebSocketPayloadAsync( socket );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( read );

			LogNet?.WriteDebug( ToString( ), $"{StringResources.Language.Receive} : OpCode[{read.Content.OpCode}] Mask[{read.Content.HasMask}] {BasicFramework.SoftBasic.ByteToHexString( read.Content.Payload, ' ' )}" );

			// 接收数据信息
			return OperateResult.CreateSuccessResult( read.Content.Payload );
		}
#endif
		#endregion

		#region Read From Server

		/// <summary>
		/// 和websocket的服务器交互，将负载数据发送到服务器端，然后等待接收服务器的数据<br />
		/// Interact with the websocket server, send the load data to the server, and then wait to receive data from the server
		/// </summary>
		/// <param name="payload">数据负载</param>
		/// <returns>返回的结果数据</returns>
		public OperateResult<string> ReadFromServer( string payload ) => ByteTransformHelper.GetSuccessResultFromOther( ReadFromCoreServer( WebSocketHelper.WebScoketPackData( 0x01, true, payload ) ), Encoding.UTF8.GetString );
#if !NET35 && !NET20
		/// <inheritdoc cref="ReadFromServer(string)"/>
		public async Task<OperateResult<string>> ReadFromServerAsync( string payload ) => ByteTransformHelper.GetSuccessResultFromOther( await ReadFromCoreServerAsync( WebSocketHelper.WebScoketPackData( 0x01, true, payload ) ), Encoding.UTF8.GetString );
#endif
		#endregion

		#region Object Override

		/// <inheritdoc/>
		public override string ToString( ) => $"WebSocketQANet[{IpAddress}:{Port}]";

		#endregion
	}
}
