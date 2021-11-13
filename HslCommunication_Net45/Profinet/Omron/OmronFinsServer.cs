using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using HslCommunication.BasicFramework;
using HslCommunication.Core;
using HslCommunication.Core.Net;
using HslCommunication.Core.IMessage;
using HslCommunication.Reflection;
using HslCommunication.Core.Address;

namespace HslCommunication.Profinet.Omron
{
	/// <summary>
	/// <b>[商业授权]</b> 欧姆龙的虚拟服务器，支持DM区，CIO区，Work区，Hold区，Auxiliary区，可以方便的进行测试<br />
	/// <b>[Authorization]</b> Omron's virtual server supports DM area, CIO area, Work area, Hold area, and Auxiliary area, which can be easily tested
	/// </summary>
	public class OmronFinsServer : NetworkDataServerBase
	{
		#region Constructor

		/// <summary>
		/// 实例化一个Fins协议的服务器<br />
		/// Instantiate a Fins protocol server
		/// </summary>
		public OmronFinsServer( )
		{
			// 共计使用了六个数据池
			dBuffer     = new SoftBuffer( DataPoolLength * 2 );
			cioBuffer   = new SoftBuffer( DataPoolLength * 2 );
			wBuffer     = new SoftBuffer( DataPoolLength * 2 );
			hBuffer     = new SoftBuffer( DataPoolLength * 2 );
			arBuffer    = new SoftBuffer( DataPoolLength * 2 );
			emBuffer    = new SoftBuffer( DataPoolLength * 2 );

			dBuffer     .IsBoolReverseByWord = true;
			cioBuffer   .IsBoolReverseByWord = true;
			wBuffer     .IsBoolReverseByWord = true;
			hBuffer     .IsBoolReverseByWord = true;
			arBuffer    .IsBoolReverseByWord = true;
			emBuffer    .IsBoolReverseByWord = true;

			WordLength = 1;                                                             // 每个地址占2个字节的数据
			ByteTransform = new ReverseWordTransform( );                                // 解析数据的类型
			ByteTransform.DataFormat = DataFormat.CDAB;                                 // 解析数据的颠倒格式
		}

		#endregion

		#region Public Properties

		/// <inheritdoc cref="ByteTransformBase.DataFormat"/>
		public DataFormat DataFormat
		{
			get => ByteTransform.DataFormat;
			set => ByteTransform.DataFormat = value;
		}

		#endregion

		#region NetworkDataServerBase Override

		/// <inheritdoc cref="OmronFinsNet.Read(string, ushort)"/>
		[HslMqttApi( "ReadByteArray", "" )]
		public override OperateResult<byte[]> Read( string address, ushort length )
		{
			// 分析地址
			var analysis = OmronFinsAddress.ParseFrom( address );
			if (!analysis.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( analysis );

			if (analysis.Content.WordCode == OmronFinsDataType.DM.WordCode)
				return OperateResult.CreateSuccessResult( dBuffer.GetBytes( analysis.Content.AddressStart / 16 * 2, length * 2 ) );
			else if (analysis.Content.WordCode == OmronFinsDataType.CIO.WordCode)
				return OperateResult.CreateSuccessResult( cioBuffer.GetBytes( analysis.Content.AddressStart / 16 * 2, length * 2 ) );
			else if (analysis.Content.WordCode == OmronFinsDataType.WR.WordCode)
				return OperateResult.CreateSuccessResult( wBuffer.GetBytes( analysis.Content.AddressStart / 16 * 2, length * 2 ) );
			else if (analysis.Content.WordCode == OmronFinsDataType.HR.WordCode)
				return OperateResult.CreateSuccessResult( hBuffer.GetBytes( analysis.Content.AddressStart / 16 * 2, length * 2 ) );
			else if (analysis.Content.WordCode == OmronFinsDataType.AR.WordCode)
				return OperateResult.CreateSuccessResult( arBuffer.GetBytes( analysis.Content.AddressStart / 16 * 2, length * 2 ) );
			else
				return OperateResult.CreateSuccessResult( emBuffer.GetBytes( analysis.Content.AddressStart / 16 * 2, length * 2 ) );
			//    return OperateResult.CreateSuccessResult( emBuffer.GetBytes( (analysis.Content2[0] * 256 + analysis.Content2[1]) * 2, length * 2 ) );
		}

		/// <inheritdoc cref="OmronFinsNet.Write(string, byte[])"/>
		[HslMqttApi( "WriteByteArray", "" )]
		public override OperateResult Write( string address, byte[] value )
		{
			// 分析地址
			var analysis = OmronFinsAddress.ParseFrom( address );
			if (!analysis.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( analysis );

			if (analysis.Content.WordCode == OmronFinsDataType.DM.WordCode)
				dBuffer.SetBytes( value, analysis.Content.AddressStart / 16 * 2 );
			else if (analysis.Content.WordCode == OmronFinsDataType.CIO.WordCode)
				cioBuffer.SetBytes( value, analysis.Content.AddressStart / 16 * 2 );
			else if (analysis.Content.WordCode == OmronFinsDataType.WR.WordCode)
				wBuffer.SetBytes( value, analysis.Content.AddressStart / 16 * 2 );
			else if (analysis.Content.WordCode == OmronFinsDataType.HR.WordCode)
				hBuffer.SetBytes( value, analysis.Content.AddressStart / 16 * 2 );
			else if (analysis.Content.WordCode == OmronFinsDataType.AR.WordCode)
				arBuffer.SetBytes( value, analysis.Content.AddressStart / 16 * 2 );
			else
				emBuffer.SetBytes( value, analysis.Content.AddressStart / 16 * 2 );

			return OperateResult.CreateSuccessResult( );
		}

		#endregion

		#region Bool Read Write Operate

		/// <inheritdoc cref="OmronFinsNet.ReadBool(string, ushort)"/>
		[HslMqttApi( "ReadBoolArray", "" )]
		public override OperateResult<bool[]> ReadBool( string address, ushort length )
		{
			// 分析地址
			var analysis = OmronFinsAddress.ParseFrom( address );
			if (!analysis.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( analysis );

			if (analysis.Content.BitCode == OmronFinsDataType.DM.BitCode)
				return OperateResult.CreateSuccessResult( dBuffer.GetBool( analysis.Content.AddressStart, length ) );
			else if (analysis.Content.BitCode == OmronFinsDataType.CIO.BitCode)
				return OperateResult.CreateSuccessResult( cioBuffer.GetBool( analysis.Content.AddressStart, length ) );
			else if (analysis.Content.BitCode == OmronFinsDataType.WR.BitCode)
				return OperateResult.CreateSuccessResult( wBuffer.GetBool( analysis.Content.AddressStart, length ) );
			else if (analysis.Content.BitCode == OmronFinsDataType.HR.BitCode)
				return OperateResult.CreateSuccessResult( hBuffer.GetBool( analysis.Content.AddressStart, length ) );
			else if (analysis.Content.BitCode == OmronFinsDataType.AR.BitCode)
				return OperateResult.CreateSuccessResult( arBuffer.GetBool( analysis.Content.AddressStart, length ) );
			else
				return OperateResult.CreateSuccessResult( emBuffer.GetBool( analysis.Content.AddressStart, length ) );
		}

		/// <inheritdoc cref="OmronFinsNet.Write(string, bool[])"/>
		[HslMqttApi( "WriteBoolArray", "" )]
		public override OperateResult Write( string address, bool[] value )
		{
			// 分析地址
			var analysis = OmronFinsAddress.ParseFrom( address );
			if (!analysis.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( analysis );

			if (analysis.Content.BitCode == OmronFinsDataType.DM.BitCode)
				dBuffer.SetBool( value, analysis.Content.AddressStart );
			else if (analysis.Content.BitCode == OmronFinsDataType.CIO.BitCode)
				cioBuffer.SetBool( value, analysis.Content.AddressStart );
			else if (analysis.Content.BitCode == OmronFinsDataType.WR.BitCode)
				wBuffer.SetBool( value, analysis.Content.AddressStart );
			else if (analysis.Content.BitCode == OmronFinsDataType.HR.BitCode)
				hBuffer.SetBool( value, analysis.Content.AddressStart );
			else if (analysis.Content.BitCode == OmronFinsDataType.AR.BitCode)
				arBuffer.SetBool( value, analysis.Content.AddressStart );
			else
				emBuffer.SetBool( value, analysis.Content.AddressStart );

			return OperateResult.CreateSuccessResult( );
		}

		#endregion

		#region NetServer Override

		/// <inheritdoc/>
		protected override void ThreadPoolLoginAfterClientCheck( Socket socket, System.Net.IPEndPoint endPoint )
		{
			// 开始接收数据信息
			FinsMessage finsMessage = new FinsMessage( );
			OperateResult<byte[]> read1 = ReceiveByMessage( socket, 5000, finsMessage );
			if (!read1.IsSuccess) return;

			OperateResult send1 = Send( socket, SoftBasic.HexStringToBytes( "46 49 4E 53 00 00 00 10 00 00 00 00 00 00 00 00 00 00 00 01 00 00 00 01" ) );
			if (!send1.IsSuccess) return;

			AppSession appSession = new AppSession( );
			appSession.IpEndPoint = endPoint;
			appSession.WorkSocket = socket;
			try
			{
				socket.BeginReceive( new byte[0], 0, 0, SocketFlags.None, new AsyncCallback( SocketAsyncCallBack ), appSession );
				AddClient( appSession );
			}
			catch
			{
				socket.Close( );
				LogNet?.WriteDebug( ToString( ), string.Format( StringResources.Language.ClientOfflineInfo, endPoint ) );
			}
		}
#if NET20 || NET35
		private void SocketAsyncCallBack( IAsyncResult ar )
#else
		private async void SocketAsyncCallBack( IAsyncResult ar )
#endif
		{
			if (ar.AsyncState is AppSession session)
			{
				try
				{
					int receiveCount = session.WorkSocket.EndReceive( ar );
#if NET20 || NET35
					OperateResult<byte[]> read1 = ReceiveByMessage( session.WorkSocket, 5000, new FinsMessage( ) );
#else
					OperateResult<byte[]> read1 = await ReceiveByMessageAsync( session.WorkSocket, 5000, new FinsMessage( ) );
#endif
					if (!read1.IsSuccess) { RemoveClient( session ); return; };

					if (!Authorization.asdniasnfaksndiqwhawfskhfaiw( )) { RemoveClient( session ); return; };

					LogNet?.WriteDebug( ToString( ), $"[{session.IpEndPoint}] Tcp {StringResources.Language.Receive}：{read1.Content.ToHexString( ' ' )}" );

					byte[] back = ReadFromFinsCore( read1.Content.RemoveBegin( 16 + 10 ) );
					if (back != null)
					{
						session.WorkSocket.Send( back );
						LogNet?.WriteDebug( ToString( ), $"[{session.IpEndPoint}] Tcp {StringResources.Language.Send}：{back.ToHexString( ' ' )}" );
					}
					else
					{
						RemoveClient( session );
						return;
					}

					session.HeartTime = DateTime.Now;
					RaiseDataReceived( session, read1.Content );
					session.WorkSocket.BeginReceive( new byte[0], 0, 0, SocketFlags.None, new AsyncCallback( SocketAsyncCallBack ), session );
				}
				catch
				{
					RemoveClient( session );
				}
			}
		}

		/// <summary>
		/// 当收到mc协议的报文的时候应该触发的方法，允许继承重写，来实现自定义的返回，或是数据监听。<br />
		/// The method that should be triggered when a message of the mc protocol is received is allowed to be inherited and rewritten to achieve a custom return or data monitoring.
		/// </summary>
		/// <param name="finsCore">mc报文</param>
		/// <returns>返回的报文信息</returns>
		protected virtual byte[] ReadFromFinsCore( byte[] finsCore )
		{
			if (finsCore[0] == 0x01 && finsCore[1] == 0x01)
			{
				// 读数据
				byte[] read = ReadByCommand( finsCore );
				return PackCommand( read == null ? 0x02 : 0x00, finsCore, read );
			}
			else if (finsCore[0] == 0x01 && finsCore[1] == 0x02)
			{
				// 先判断是否有写入的权利，没有的话，直接返回写入异常
				if (!EnableWrite) return PackCommand( 0x03, finsCore, null );

				// 写数据
				return PackCommand( 0, finsCore, WriteByMessage( finsCore ) );
			}
			else if (finsCore[0] == 0x04 && finsCore[1] == 0x01) return PackCommand( 0, finsCore, null );  // RUN
			else if (finsCore[0] == 0x04 && finsCore[1] == 0x02) return PackCommand( 0, finsCore, null );  // STOP
			else
			{
				return null;
			}
		}

		/// <summary>
		/// 将核心报文打包的方法，追加报文头<br />
		/// The method of packing the core message, adding the message header
		/// </summary>
		/// <param name="status">错误码</param>
		/// <param name="finsCore">Fins的核心报文</param>
		/// <param name="data">核心的内容</param>
		/// <returns>完整的报文信息</returns>
		protected virtual byte[] PackCommand( int status, byte[] finsCore, byte[] data )
		{
			if (data == null) data = new byte[0];

			byte[] back = new byte[30 + data.Length];
			SoftBasic.HexStringToBytes( 
				"46 49 4E 53 00 00 00 00" + 
				"00 00 00 00 00 00 00 00" + 
				"00 00 00 00 00 00 00 00 00 00 00 00 00 00").CopyTo( back, 0 );
			if (data.Length > 0) data.CopyTo( back, 30 );

			back[26] = finsCore[0];
			back[27] = finsCore[1];
			BitConverter.GetBytes( back.Length - 8 ).ReverseNew().CopyTo( back, 4 );
			BitConverter.GetBytes( status ).ReverseNew( ).CopyTo( back, 12 );
			return back;
		}

		private byte[] ReadByCommand( byte[] command )
		{
			if (command[2] == OmronFinsDataType.DM.BitCode || 
				command[2] == OmronFinsDataType.CIO.BitCode || 
				command[2] == OmronFinsDataType.WR.BitCode || 
				command[2] == OmronFinsDataType.HR.BitCode || 
				command[2] == OmronFinsDataType.AR.BitCode ||
				(0x20 <= command[2] && command[2] < 0x30) || (0xD0 <= command[2] && command[2] < 0xE0))
			{
				// 位读取
				ushort length = (ushort)(command[6] * 256 + command[7]);
				int startIndex = (command[3] * 256 + command[4]) * 16 + command[5];

				if (command[2] == OmronFinsDataType.DM.BitCode)
					return dBuffer.GetBool( startIndex, length ).Select( m => m ? (byte)0x01 : (byte)0x00 ).ToArray( );
				else if (command[2] == OmronFinsDataType.CIO.BitCode)
					return cioBuffer.GetBool( startIndex, length ).Select( m => m ? (byte)0x01 : (byte)0x00 ).ToArray( );
				else if (command[2] == OmronFinsDataType.WR.BitCode)
					return wBuffer.GetBool( startIndex, length ).Select( m => m ? (byte)0x01 : (byte)0x00 ).ToArray( );
				else if (command[2] == OmronFinsDataType.HR.BitCode)
					return hBuffer.GetBool( startIndex, length ).Select( m => m ? (byte)0x01 : (byte)0x00 ).ToArray( );
				else if (command[2] == OmronFinsDataType.AR.BitCode)
					return arBuffer.GetBool( startIndex, length ).Select( m => m ? (byte)0x01 : (byte)0x00 ).ToArray( );
				else if ((0x20 <= command[2] && command[2] < 0x30) || (0xD0 <= command[2] && command[2] < 0xE0))
					return emBuffer.GetBool( startIndex, length ).Select( m => m ? (byte)0x01 : (byte)0x00 ).ToArray( );
				else
					throw new Exception( StringResources.Language.NotSupportedDataType );
			}
			else if( 
				command[2] == OmronFinsDataType.DM.WordCode ||
				command[2] == OmronFinsDataType.CIO.WordCode ||
				command[2] == OmronFinsDataType.WR.WordCode ||
				command[2] == OmronFinsDataType.HR.WordCode ||
				command[2] == OmronFinsDataType.AR.WordCode ||
				(0xA0 <= command[2] && command[2] < 0xB0) || (0x50 <= command[2] && command[2] < 0x60))
			{
				// 字读取
				ushort length = (ushort)(command[6] * 256 + command[7]);
				int startIndex = (command[3] * 256 + command[4]);

				// 不能大于999字的读取，直接返回失败信息
				if (length > 999) return null;

				if (command[2] == OmronFinsDataType.DM.WordCode)
					return dBuffer.GetBytes( startIndex * 2, length * 2 );
				else if (command[2] == OmronFinsDataType.CIO.WordCode)
					return cioBuffer.GetBytes( startIndex * 2, length * 2 );
				else if (command[2] == OmronFinsDataType.WR.WordCode)
					return wBuffer.GetBytes( startIndex * 2, length * 2 );
				else if (command[2] == OmronFinsDataType.HR.WordCode)
					return hBuffer.GetBytes( startIndex * 2, length * 2 );
				else if (command[2] == OmronFinsDataType.AR.WordCode)
					return arBuffer.GetBytes( startIndex * 2, length * 2 );
				else if ((0xA0 <= command[2] && command[2] < 0xB0) || (0x50 <= command[2] && command[2] < 0x60))
					return emBuffer.GetBytes( startIndex * 2, length * 2 );
				else
					throw new Exception( StringResources.Language.NotSupportedDataType );
			}
			else
				return new byte[0];
		}

		private byte[] WriteByMessage( byte[] command )
		{
			if (command[2] == OmronFinsDataType.DM.BitCode ||
				command[2] == OmronFinsDataType.CIO.BitCode ||
				command[2] == OmronFinsDataType.WR.BitCode ||
				command[2] == OmronFinsDataType.HR.BitCode ||
				command[2] == OmronFinsDataType.AR.BitCode ||
				(0x20 <= command[2] && command[2] < 0x30) || (0xD0 <= command[2] && command[2] < 0xE0))
			{
				// 位写入
				ushort length = (ushort)(command[6] * 256 + command[7]);
				int startIndex = (command[3] * 256 + command[4]) * 16 + command[5];
				bool[] buffer = SoftBasic.ArrayRemoveBegin( command, 8 ).Select( m => m == 0x01 ).ToArray( );

				if (command[2] == OmronFinsDataType.DM.BitCode)
					dBuffer.SetBool( buffer, startIndex );
				else if (command[2] == OmronFinsDataType.CIO.BitCode)
					cioBuffer.SetBool( buffer, startIndex );
				else if (command[2] == OmronFinsDataType.WR.BitCode)
					wBuffer.SetBool( buffer, startIndex );
				else if (command[2] == OmronFinsDataType.HR.BitCode)
					hBuffer.SetBool( buffer, startIndex );
				else if (command[2] == OmronFinsDataType.AR.BitCode)
					arBuffer.SetBool( buffer, startIndex );
				else if ((0x20 <= command[2] && command[2] < 0x30) || (0xD0 <= command[2] && command[2] < 0xE0))
					emBuffer.SetBool( buffer, startIndex );
				else
					throw new Exception( StringResources.Language.NotSupportedDataType );
				return new byte[0];
			}
			else
			{
				// 字写入
				ushort length = (ushort)(command[6] * 256 + command[7]);
				int startIndex = (command[3] * 256 + command[4]);
				byte[] buffer = SoftBasic.ArrayRemoveBegin( command, 8 );

				if (command[2] == OmronFinsDataType.DM.WordCode)
					dBuffer.SetBytes( buffer, startIndex * 2 );
				else if (command[2] == OmronFinsDataType.CIO.WordCode)
					cioBuffer.SetBytes( buffer, startIndex * 2 );
				else if (command[2] == OmronFinsDataType.WR.WordCode)
					wBuffer.SetBytes( buffer, startIndex * 2 );
				else if (command[2] == OmronFinsDataType.HR.WordCode)
					hBuffer.SetBytes( buffer, startIndex * 2 );
				else if (command[2] == OmronFinsDataType.AR.WordCode)
					arBuffer.SetBytes( buffer, startIndex * 2 );
				else if ((0xA0 <= command[2] && command[2] < 0xB0) || (0x50 <= command[2] && command[2] < 0x60))
					emBuffer.SetBytes( buffer, startIndex * 2 );
				else
					throw new Exception( StringResources.Language.NotSupportedDataType );
				return new byte[0];
			}
		}

		#endregion

		#region Data Save Load Override

		/// <inheritdoc/>
		protected override void LoadFromBytes( byte[] content )
		{
			if (content.Length < DataPoolLength * 12) throw new Exception( "File is not correct" );

			dBuffer.SetBytes(     content, DataPoolLength *  0, 0, DataPoolLength * 2 );
			cioBuffer.SetBytes(   content, DataPoolLength *  2, 0, DataPoolLength * 2 );
			wBuffer.SetBytes(     content, DataPoolLength *  4, 0, DataPoolLength * 2 );
			hBuffer.SetBytes(     content, DataPoolLength *  6, 0, DataPoolLength * 2 );
			arBuffer.SetBytes(    content, DataPoolLength *  8, 0, DataPoolLength * 2 );
			emBuffer.SetBytes(    content, DataPoolLength * 10, 0, DataPoolLength * 2 );
		}

		/// <inheritdoc/>
		protected override byte[] SaveToBytes( )
		{
			byte[] buffer = new byte[DataPoolLength * 12];
			Array.Copy( dBuffer.GetBytes( ),     0, buffer, DataPoolLength *  0, DataPoolLength * 2 );
			Array.Copy( cioBuffer.GetBytes( ),   0, buffer, DataPoolLength *  2, DataPoolLength * 2 );
			Array.Copy( wBuffer.GetBytes( ),     0, buffer, DataPoolLength *  4, DataPoolLength * 2 );
			Array.Copy( hBuffer.GetBytes( ),     0, buffer, DataPoolLength *  6, DataPoolLength * 2 );
			Array.Copy( arBuffer.GetBytes( ),    0, buffer, DataPoolLength *  8, DataPoolLength * 2 );
			Array.Copy( emBuffer.GetBytes( ),    0, buffer, DataPoolLength * 10, DataPoolLength * 2 );

			return buffer;
		}

		#endregion

		#region IDisposable Support

		/// <inheritdoc cref="IDisposable.Dispose()"/>
		protected override void Dispose( bool disposing )
		{
			if (disposing)
			{
				dBuffer?.Dispose( );
				cioBuffer?.Dispose( );
				wBuffer?.Dispose( );
				hBuffer?.Dispose( );
				arBuffer?.Dispose( );
				//emBuffer?.Dispose( );
			}
			base.Dispose( disposing );
		}

		#endregion

		#region Private Member

		private SoftBuffer dBuffer;                      // d寄存器的数据池
		private SoftBuffer cioBuffer;                    // cio寄存器的数据池
		private SoftBuffer wBuffer;                      // w寄存器的数据池
		private SoftBuffer hBuffer;                      // h寄存器的数据池
		private SoftBuffer arBuffer;                     // ar寄存器的数据池
		private SoftBuffer emBuffer;                     // em寄存器的数据池

		private const int DataPoolLength = 65536;      // 数据的长度

		#endregion

		#region Object Override

		/// <inheritdoc/>
		public override string ToString( ) => $"OmronFinsServer[{Port}]";

		#endregion
	}
}
