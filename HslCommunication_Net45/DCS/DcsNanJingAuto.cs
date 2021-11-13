using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HslCommunication.ModBus;
using System.Net.Sockets;
using HslCommunication.Core.IMessage;
using System.Threading;
using HslCommunication.BasicFramework;
#if !NET20 && !NET35
using System.Threading.Tasks;
#endif

namespace HslCommunication.DCS
{
	/// <summary>
	/// 南京自动化研究所的DCS系统，基于modbus实现，但是不是标准的实现
	/// </summary>
	public class DcsNanJingAuto : ModbusTcpNet
	{
		#region Constructor

		/// <summary>
		/// 实例化一个默认的对象<br />
		/// Instantiate a default object
		/// </summary>
		public DcsNanJingAuto( ) : base( ) { }

		/// <inheritdoc/>
		public DcsNanJingAuto( string ipAddress, int port = 502, byte station = 0x01 ) : base( ipAddress, port, station ) { }

		/// <inheritdoc/>
		protected override INetMessage GetNewNetMessage( ) => new DcsNanJingAutoMessage( );

		#endregion

		#region Override Method

		/// <inheritdoc/>
		protected override OperateResult InitializationOnConnect( Socket socket )
		{
			MessageId.ResetCurrentValue( 0 );

			headCommand[6] = Station;
			OperateResult send = Send( socket, headCommand );
			if (!send.IsSuccess) return send;

			OperateResult<byte[]> receive = Receive( socket, -1, 3000 );
			if (!receive.IsSuccess) return receive;

			return CheckResponseStatus( receive.Content ) ? base.InitializationOnConnect( socket ) : new OperateResult( "Check Status Response failed: " + receive.Content.ToHexString( ' ' ) );
		}
#if !NET35 && !NET20
		/// <inheritdoc/>
		protected override async Task<OperateResult> InitializationOnConnectAsync( Socket socket )
		{
			MessageId.ResetCurrentValue( 0 );

			headCommand[6] = Station;
			OperateResult send = await SendAsync( socket, headCommand );
			if (!send.IsSuccess) return send;

			OperateResult<byte[]> receive = await ReceiveAsync( socket, -1, 3000 );
			if (!receive.IsSuccess) return receive;

			return CheckResponseStatus( receive.Content ) ? OperateResult.CreateSuccessResult( ) : new OperateResult( "Check Status Response failed: " + receive.Content.ToHexString( ' ' ) );
		}
#endif
		/// <inheritdoc/>
		public override OperateResult<byte[]> ReadFromCoreServer( Socket socket, byte[] send, bool hasResponseData = true, bool usePackHeader = true )
		{
			LogNet?.WriteDebug( ToString( ), StringResources.Language.Send + " : " + (LogMsgFormatBinary ? send.ToHexString( ' ' ) : Encoding.ASCII.GetString( send )) );

			INetMessage netMessage = GetNewNetMessage( );
			if (netMessage != null) netMessage.SendBytes = send;

			// send
			OperateResult sendResult = Send( socket, send );
			if (!sendResult.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( sendResult );
			if (receiveTimeOut < 0)    return OperateResult.CreateSuccessResult( new byte[0] );
			if (!hasResponseData)      return OperateResult.CreateSuccessResult( new byte[0] );
			if (SleepTime > 0) Thread.Sleep( SleepTime );

			// receive msg
			OperateResult<byte[]> resultReceive = ReceiveByMessage( socket, receiveTimeOut, netMessage );
			if (!resultReceive.IsSuccess) return resultReceive;

			LogNet?.WriteDebug( ToString( ), StringResources.Language.Receive + " : " + (LogMsgFormatBinary ? resultReceive.Content.ToHexString( ' ' ) : Encoding.ASCII.GetString( resultReceive.Content )) );

			if (resultReceive.Content.Length == 0x06 && CheckResponseStatus( resultReceive.Content ))
				resultReceive = ReceiveByMessage( socket, receiveTimeOut, netMessage );

			// check
			if (netMessage != null && !netMessage.CheckHeadBytesLegal( Token.ToByteArray( ) ))
			{
				socket?.Close( );
				return new OperateResult<byte[]>( StringResources.Language.CommandHeadCodeCheckFailed + Environment.NewLine +
					StringResources.Language.Send + ": " + SoftBasic.ByteToHexString( send, ' ' ) + Environment.NewLine +
					StringResources.Language.Receive + ": " + SoftBasic.ByteToHexString( resultReceive.Content, ' ' ) );
			}

			// Success
			return OperateResult.CreateSuccessResult( resultReceive.Content );
		}
#if !NET20 && !NET35
		/// <inheritdoc/>
		public override async Task<OperateResult<byte[]>> ReadFromCoreServerAsync( Socket socket, byte[] send, bool hasResponseData = true, bool usePackHeader = true )
		{
			byte[] sendValue = usePackHeader ? PackCommandWithHeader( send ) : send;
			LogNet?.WriteDebug( ToString( ), StringResources.Language.Send + " : " + (LogMsgFormatBinary ? sendValue.ToHexString( ' ' ) : Encoding.ASCII.GetString( sendValue )) );

			INetMessage netMessage = GetNewNetMessage( );
			if (netMessage != null) netMessage.SendBytes = sendValue;

			// send
			OperateResult sendResult = await SendAsync( socket, sendValue );
			if (!sendResult.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( sendResult );
			if (receiveTimeOut < 0)    return OperateResult.CreateSuccessResult( new byte[0] );
			if (!hasResponseData)      return OperateResult.CreateSuccessResult( new byte[0] );
			if (SleepTime > 0) await Task.Delay( SleepTime );

			// receive msg
			OperateResult<byte[]> resultReceive = await ReceiveByMessageAsync( socket, receiveTimeOut, netMessage );
			if (!resultReceive.IsSuccess) return resultReceive;

			LogNet?.WriteDebug( ToString( ), StringResources.Language.Receive + " : " + (LogMsgFormatBinary ? resultReceive.Content.ToHexString( ' ' ) : Encoding.ASCII.GetString( resultReceive.Content )) );

			if (resultReceive.Content.Length == 0x06 && CheckResponseStatus( resultReceive.Content ))
				resultReceive = await ReceiveByMessageAsync( socket, receiveTimeOut, netMessage );

			// check
			if (netMessage != null && !netMessage.CheckHeadBytesLegal( Token.ToByteArray( ) ))
			{
				socket?.Close( );
				return new OperateResult<byte[]>( StringResources.Language.CommandHeadCodeCheckFailed + Environment.NewLine +
					StringResources.Language.Send + ": " + SoftBasic.ByteToHexString( send, ' ' ) + Environment.NewLine +
					StringResources.Language.Receive + ": " + SoftBasic.ByteToHexString( resultReceive.Content, ' ' ) );
			}

			// extra check
			return UnpackResponseContent( sendValue, resultReceive.Content );
		}
#endif
		#endregion

		#region Private Method

		private bool CheckResponseStatus( byte[] content )
		{
			if (content.Length < 6) return false;
			for (int i = content.Length - 4; i < content.Length; i++)
			{
				if (content[i] != 0x00) return false;
			}
			return true;
		}

		#endregion

		#region Private Member

		private byte[] headCommand = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x06, 0x01, 0x03, 0x00, 0x00, 0x00, 0x01 };

		#endregion
	}
}
