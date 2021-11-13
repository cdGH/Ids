using HslCommunication.BasicFramework;
using HslCommunication.Core;
using HslCommunication.Core.IMessage;
using HslCommunication.Core.Net;
using HslCommunication.Reflection;
using HslCommunication.Core.Address;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
#if !NET20 && !NET35
using System.Threading.Tasks;
#endif

namespace HslCommunication.Profinet.Fuji
{
	/// <summary>
	/// 富士Command-Setting-type协议实现的虚拟服务器，支持的地址为 B,M,K,D,W9,BD,F,A,WL,W21
	/// </summary>
	public class FujiCommandSettingTypeServer : NetworkDataServerBase
	{
		#region Constructor

		/// <summary>
		/// 实例化一个富士的服务器<br />
		/// </summary>
		public FujiCommandSettingTypeServer( )
		{
			bBuffer     = new SoftBuffer( DataPoolLength );
			mBuffer     = new SoftBuffer( DataPoolLength );
			kBuffer     = new SoftBuffer( DataPoolLength );
			dBuffer     = new SoftBuffer( DataPoolLength );
			sBuffer     = new SoftBuffer( DataPoolLength );
			w9Buffer    = new SoftBuffer( DataPoolLength );
			bdBuffer    = new SoftBuffer( DataPoolLength );
			fBuffer     = new SoftBuffer( DataPoolLength );
			aBuffer     = new SoftBuffer( DataPoolLength );
			wlBuffer    = new SoftBuffer( DataPoolLength );
			w21Buffer   = new SoftBuffer( DataPoolLength );

			WordLength  = 2;
			ByteTransform = new ReverseBytesTransform( );
		}

		#endregion

		#region Public Propertes

		/// <inheritdoc cref="FujiCommandSettingType.DataSwap"/>
		public bool DataSwap
		{
			get => this.dataSwap;
			set
			{
				this.dataSwap = value;
				if (value)
					this.ByteTransform = new RegularByteTransform( );
				else
					this.ByteTransform = new ReverseBytesTransform( );
			}
		}

		#endregion

		#region NetworkDataServerBase Override

		/// <inheritdoc/>
		[HslMqttApi( "ReadByteArray", "" )]
		public override OperateResult<byte[]> Read( string address, ushort length )
		{
			try
			{
				OperateResult<byte[]> build = FujiCommandSettingType.BuildReadCommand( address, length );
				if (!build.IsSuccess) return build;

				byte[] read = ReadByMessage( build.Content );
				return FujiCommandSettingType.UnpackResponseContentHelper( build.Content, read );
			}
			catch(Exception ex)
			{
				return new OperateResult<byte[]>( ex.Message );
			}
		}

		/// <inheritdoc/>
		[HslMqttApi( "WriteByteArray", "" )]
		public override OperateResult Write( string address, byte[] value )
		{
			try
			{
				OperateResult<byte[]> build = FujiCommandSettingType.BuildWriteCommand( address, value );
				if (!build.IsSuccess) return build;

				byte[] read = WriteByMessage( build.Content );
				return FujiCommandSettingType.UnpackResponseContentHelper( build.Content, read );
			}
			catch (Exception ex)
			{
				return new OperateResult<byte[]>( ex.Message );
			}
		}

		#endregion

		#region Byte Read Write Operate

		/// <summary>
		/// 从PLC读取byte类型的数据信息，通常针对步进寄存器，也就是 S100 的地址
		/// </summary>
		/// <param name="address">PLC地址数据，例如 S100</param>
		/// <returns>是否读取成功结果对象</returns>
		[HslMqttApi( "ReadByte", "" )]
		public OperateResult<byte> ReadByte( string address ) => ByteTransformHelper.GetResultFromArray( Read( address, 1 ) );

		/// <summary>
		/// 将Byte输入写入到PLC之中，通常针对步进寄存器，也就是 S100 的地址
		/// </summary>
		/// <param name="address">PLC地址数据，例如 S100</param>
		/// <param name="value">数据信息</param>
		/// <returns>是否写入成功</returns>
		[HslMqttApi( "WriteByte", "" )]
		public OperateResult Write( string address, byte value ) => Write( address, new byte[] { value } );

		#endregion

		#region Bool Read Write Operate

		/// <inheritdoc/>
		[HslMqttApi( "ReadBool", "" )]
		public override OperateResult<bool> ReadBool( string address )
		{
			return base.ReadBool( address );
		}

		/// <inheritdoc/>
		[HslMqttApi( "WriteBool", "" )]
		public override OperateResult Write( string address, bool value )
		{
			return base.Write( address, value );
		}

		#endregion

		#region NetServer Override

		/// <inheritdoc/>
		protected override void ThreadPoolLoginAfterClientCheck( Socket socket, System.Net.IPEndPoint endPoint )
		{
			// 开始接收数据信息
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
					OperateResult<byte[]> read1 = ReceiveByMessage( session.WorkSocket, 5000, new FujiCommandSettingTypeMessage( ) );
#else
					OperateResult<byte[]> read1 = await ReceiveByMessageAsync( session.WorkSocket, 5000, new FujiCommandSettingTypeMessage( ) );
#endif
					if (!read1.IsSuccess) { RemoveClient( session ); return; };

					if (!Authorization.asdniasnfaksndiqwhawfskhfaiw( )) { RemoveClient( session ); return; };

					LogNet?.WriteDebug( ToString( ), $"[{session.IpEndPoint}] Tcp {StringResources.Language.Receive}：{read1.Content.ToHexString( ' ' )}" );

					byte[] receive = read1.Content;
					byte[] back = null;
					if      (receive[0] == 0x00 && receive[2] == 0x00) back = ReadByMessage( receive );    // 读取数据
					else if (receive[0] == 0x01 && receive[2] == 0x00) back = WriteByMessage( receive );   // 写入数据else
					else back = PackResponseResult( receive, 0x20, null );

					session.WorkSocket.Send( back );
					LogNet?.WriteDebug( ToString( ), $"[{session.IpEndPoint}] Tcp {StringResources.Language.Send}：{back.ToHexString( ' ' )}" );

					session.HeartTime = DateTime.Now;
					RaiseDataReceived( session, receive );
					session.WorkSocket.BeginReceive( new byte[0], 0, 0, SocketFlags.None, new AsyncCallback( SocketAsyncCallBack ), session );
				}
				catch
				{
					RemoveClient( session );
				}
			}
		}

		private byte[] PackResponseResult( byte[] command, byte err, byte[] value )
		{
			if(err > 0 || command[0] == 0x01)
			{
				byte[] back = new byte[9];
				Array.Copy( command, 0, back, 0, 9 );
				back[1] = err;
				back[4] = 0x04;
				return back;
			}
			else
			{
				if (value == null) value = new byte[0];
				byte[] back = new byte[10 + value.Length];
				Array.Copy( command, 0, back, 0, 9 );
				back[4] = (byte)(0x05 + value.Length);
				value.CopyTo( back, 10 );
				return back;
			}
		}

		private byte[] ReadByMessage( byte[] command )
		{
			int address = command[5] + command[6] * 256;
			int length = command[7] + command[8] * 256;

			if      (command[3] == 0x00) return PackResponseResult( command, 0x00, bBuffer.GetBytes( address * 2, length ) );
			else if (command[3] == 0x01) return PackResponseResult( command, 0x00, mBuffer.GetBytes( address * 2, length ) );
			else if (command[3] == 0x02) return PackResponseResult( command, 0x00, kBuffer.GetBytes( address * 2, length ) );
			else if (command[3] == 0x03) return PackResponseResult( command, 0x00, fBuffer.GetBytes( address * 2, length ) );
			else if (command[3] == 0x04) return PackResponseResult( command, 0x00, aBuffer.GetBytes( address * 2, length ) );
			else if (command[3] == 0x05) return PackResponseResult( command, 0x00, dBuffer.GetBytes( address * 2, length ) );
			else if (command[3] == 0x08) return PackResponseResult( command, 0x00, sBuffer.GetBytes( address, length ) );
			else if (command[3] == 0x09) return PackResponseResult( command, 0x00, w9Buffer.GetBytes( address * 4, length ) );
			else if (command[3] == 0x0E) return PackResponseResult( command, 0x00, bdBuffer.GetBytes( address * 4, length ) );
			else if (command[3] == 0x14) return PackResponseResult( command, 0x00, wlBuffer.GetBytes( address * 2, length ) );
			else if (command[3] == 0x15) return PackResponseResult( command, 0x00, w21Buffer.GetBytes( address * 2, length ) );
			else return PackResponseResult( command, 0x24, null );
		}

		private byte[] WriteByMessage( byte[] command )
		{
			// 先判断是否有写入的权利，没有的话，直接返回写入异常
			if (!EnableWrite) return PackResponseResult( command, 0x22, null );

			int address = command[5] + command[6] * 256;
			int length = command[7] + command[8] * 256;
			byte[] value = command.RemoveBegin( 9 );
			
			if      (command[3] == 0x00) bBuffer.SetBytes(   value, address * 2 );
			else if (command[3] == 0x01) mBuffer.SetBytes(   value, address * 2 );
			else if (command[3] == 0x02) kBuffer.SetBytes(   value, address * 2 );
			else if (command[3] == 0x03) fBuffer.SetBytes(   value, address * 2 );
			else if (command[3] == 0x04) aBuffer.SetBytes(   value, address * 2 );
			else if (command[3] == 0x05) dBuffer.SetBytes(   value, address * 2 );
			else if (command[3] == 0x08) sBuffer.SetBytes(   value, address );
			else if (command[3] == 0x09) w9Buffer.SetBytes(  value, address * 4 );
			else if (command[3] == 0x0E) bdBuffer.SetBytes(  value, address * 4 );
			else if (command[3] == 0x14) wlBuffer.SetBytes(  value, address * 2 );
			else if (command[3] == 0x15) w21Buffer.SetBytes( value, address * 2 ); 
			else return PackResponseResult( command, 0x24, null );

			return PackResponseResult( command, 0x00, null );
		}

		#endregion

		#region Data Save Load Override

		/// <inheritdoc/>
		protected override void LoadFromBytes( byte[] content )
		{
			if (content.Length < DataPoolLength * 11) throw new Exception( "File is not correct" );

			bBuffer.  SetBytes( content, DataPoolLength * 0, 0, DataPoolLength );
			mBuffer.  SetBytes( content, DataPoolLength * 1, 0, DataPoolLength );
			kBuffer.  SetBytes( content, DataPoolLength * 2, 0, DataPoolLength );
			fBuffer.  SetBytes( content, DataPoolLength * 3, 0, DataPoolLength );
			aBuffer.  SetBytes( content, DataPoolLength * 4, 0, DataPoolLength );
			dBuffer.  SetBytes( content, DataPoolLength * 5, 0, DataPoolLength );
			sBuffer.  SetBytes( content, DataPoolLength * 6, 0, DataPoolLength );
			w9Buffer. SetBytes( content, DataPoolLength * 7, 0, DataPoolLength );
			bdBuffer. SetBytes( content, DataPoolLength * 8, 0, DataPoolLength );
			wlBuffer. SetBytes( content, DataPoolLength * 9, 0, DataPoolLength );
			w21Buffer.SetBytes( content, DataPoolLength * 10, 0, DataPoolLength );
		}

		/// <inheritdoc/>
		protected override byte[] SaveToBytes( )
		{
			byte[] buffer = new byte[DataPoolLength * 11];
			Array.Copy( bBuffer.  GetBytes( ), 0, buffer, DataPoolLength * 0, DataPoolLength );
			Array.Copy( mBuffer.  GetBytes( ), 0, buffer, DataPoolLength * 1, DataPoolLength );
			Array.Copy( kBuffer.  GetBytes( ), 0, buffer, DataPoolLength * 2, DataPoolLength );
			Array.Copy( fBuffer.  GetBytes( ), 0, buffer, DataPoolLength * 3, DataPoolLength );
			Array.Copy( aBuffer.  GetBytes( ), 0, buffer, DataPoolLength * 4, DataPoolLength );
			Array.Copy( dBuffer.  GetBytes( ), 0, buffer, DataPoolLength * 5, DataPoolLength );
			Array.Copy( sBuffer.  GetBytes( ), 0, buffer, DataPoolLength * 6, DataPoolLength );
			Array.Copy( w9Buffer. GetBytes( ), 0, buffer, DataPoolLength * 7, DataPoolLength );
			Array.Copy( bdBuffer. GetBytes( ), 0, buffer, DataPoolLength * 8, DataPoolLength );
			Array.Copy( wlBuffer. GetBytes( ), 0, buffer, DataPoolLength * 9, DataPoolLength );
			Array.Copy( w21Buffer.GetBytes( ), 0, buffer, DataPoolLength * 10, DataPoolLength );

			return buffer;
		}

		#endregion

		#region IDisposable Support

		/// <inheritdoc/>
		protected override void Dispose( bool disposing )
		{
			if (disposing)
			{
				 bBuffer?.  Dispose( );
				 mBuffer?.  Dispose( );
				 kBuffer?.  Dispose( );
				 fBuffer?.  Dispose( );
				 aBuffer?.  Dispose( );
				 dBuffer?.  Dispose( );
				 sBuffer?.  Dispose( );
				 w9Buffer?. Dispose( );
				 bdBuffer?. Dispose( );
				 wlBuffer?. Dispose( );
				 w21Buffer?.Dispose( );
			}
			base.Dispose( disposing );
		}

		#endregion

		#region Private Member

		private bool dataSwap = false;              // 是否发生了数据交换
		private SoftBuffer bBuffer;                 // io relay
		private SoftBuffer mBuffer;                 // auxiliary relay
		private SoftBuffer kBuffer;                 // keep relay
		private SoftBuffer fBuffer;                 // special relay
		private SoftBuffer aBuffer;                 // announce relay
		private SoftBuffer dBuffer;                 // differential relay
		private SoftBuffer sBuffer;                 // step control
		private SoftBuffer w9Buffer;                // current value of 0.1-sec timer
		private SoftBuffer bdBuffer;                // data memory
		private SoftBuffer wlBuffer;                // No.1 block
		private SoftBuffer w21Buffer;               // No.2 block
		private const int DataPoolLength = 65536;   // 数据的长度

		#endregion

		#region Object Override

		/// <inheritdoc/>
		public override string ToString( ) => $"FujiCommandSettingTypeServer[{Port}]";

		#endregion
	}
}
