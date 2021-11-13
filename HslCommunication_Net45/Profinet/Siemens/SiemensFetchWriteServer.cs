using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HslCommunication.Core.Net;
using HslCommunication.BasicFramework;
using HslCommunication.Core;
using HslCommunication.Reflection;
using HslCommunication.Core.Address;
using System.Net.Sockets;
using HslCommunication.Core.IMessage;
#if !NET20 && !NET35
using System.Threading.Tasks;
#endif

namespace HslCommunication.Profinet.Siemens
{
	/// <summary>
	/// <b>[商业授权]</b> 西门子的Fetch/Write协议的虚拟PLC，可以用来调试通讯，也可以实现一个虚拟的PLC功能，从而开发一套带虚拟环境的上位机系统，可以用来演示，测试。<br />
	/// <b>[Authorization]</b> The virtual PLC of Siemens Fetch/Write protocol can be used for debugging communication, and can also realize a virtual PLC function, so as to develop a set of upper computer system with virtual environment, which can be used for demonstration and testing.
	/// </summary>
	/// <remarks>
	/// 本虚拟服务器的使用需要企业商业授权，否则只能运行24小时。本协议实现的虚拟PLC服务器，主要支持I,Q,M,DB块的数据读写操作，例如 M100, DB1.100，服务器端也可以对位进行读写操作，例如M100.1，DB1.100.2；
	/// 但是不支持连接的远程客户端对位进行操作。
	/// </remarks>
	/// <example>
	/// 地址支持的列表如下：
	/// <list type="table">
	///   <listheader>
	///     <term>地址名称</term>
	///     <term>地址代号</term>
	///     <term>示例</term>
	///     <term>地址进制</term>
	///     <term>字操作</term>
	///     <term>位操作</term>
	///     <term>备注</term>
	///   </listheader>
	///   <item>
	///     <term>中间寄存器</term>
	///     <term>M</term>
	///     <term>M100,M200</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>输入寄存器</term>
	///     <term>I</term>
	///     <term>I100,I200</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>输出寄存器</term>
	///     <term>Q</term>
	///     <term>Q100,Q200</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>DB块寄存器</term>
	///     <term>DB</term>
	///     <term>DB1.100,DB1.200</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>V寄存器</term>
	///     <term>V</term>
	///     <term>V100,V200</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term>V寄存器本质就是DB块1</term>
	///   </item>
	/// </list>
	/// 本虚拟的PLC共有4个DB块，DB1.X, DB2.X, DB3.X, 和其他DB块。对于远程客户端的读写长度，暂时没有限制。
	/// </example>
	public class SiemensFetchWriteServer : NetworkDataServerBase
	{
		#region Constructor

		/// <summary>
		/// 实例化一个S7协议的服务器，支持I，Q，M，DB1.X, DB2.X, DB3.X 数据区块的读写操作<br />
		/// Instantiate a server with S7 protocol, support I, Q, M, DB1.X data block read and write operations
		/// </summary>
		public SiemensFetchWriteServer( )
		{
			// 四个数据池初始化，输入寄存器，输出寄存器，中间寄存器，DB块寄存器
			inputBuffer             = new SoftBuffer( DataPoolLength );
			outputBuffer            = new SoftBuffer( DataPoolLength );
			memeryBuffer            = new SoftBuffer( DataPoolLength );
			db1BlockBuffer          = new SoftBuffer( DataPoolLength );
			db2BlockBuffer          = new SoftBuffer( DataPoolLength );
			db3BlockBuffer          = new SoftBuffer( DataPoolLength );
			dbOtherBlockBuffer      = new SoftBuffer( DataPoolLength );
			counterBuffer           = new SoftBuffer( DataPoolLength );
			timerBuffer             = new SoftBuffer( DataPoolLength );

			WordLength              = 2;
			ByteTransform           = new ReverseBytesTransform( );
		}

		#endregion

		#region NetworkDataServerBase Override

		private OperateResult<SoftBuffer> GetDataAreaFromS7Address( S7AddressData s7Address )
		{
			switch (s7Address.DataCode)
			{
				case 0x81: return OperateResult.CreateSuccessResult( inputBuffer );
				case 0x82: return OperateResult.CreateSuccessResult( outputBuffer );
				case 0x83: return OperateResult.CreateSuccessResult( memeryBuffer );
				case 0x84:
					if      (s7Address.DbBlock == 1) return OperateResult.CreateSuccessResult( db1BlockBuffer );
					else if (s7Address.DbBlock == 2) return OperateResult.CreateSuccessResult( db2BlockBuffer );
					else if (s7Address.DbBlock == 3) return OperateResult.CreateSuccessResult( db3BlockBuffer );
					else return OperateResult.CreateSuccessResult( dbOtherBlockBuffer );

				default: return new OperateResult<SoftBuffer>( StringResources.Language.NotSupportedDataType );
			}
		}

		/// <inheritdoc cref="SiemensS7Net.Read(string, ushort)"/>
		[HslMqttApi( "ReadByteArray", "" )]
		public override OperateResult<byte[]> Read( string address, ushort length )
		{
			OperateResult<S7AddressData> analysis = S7AddressData.ParseFrom( address, length );
			if (!analysis.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( analysis );

			OperateResult<SoftBuffer> buffer = GetDataAreaFromS7Address( analysis.Content );
			if(!buffer.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( buffer );

			return OperateResult.CreateSuccessResult( buffer.Content.GetBytes( analysis.Content.AddressStart / 8, length ) );
		}

		/// <inheritdoc cref="SiemensS7Net.Write(string, byte[])"/>
		[HslMqttApi( "WriteByteArray", "" )]
		public override OperateResult Write( string address, byte[] value )
		{
			OperateResult<S7AddressData> analysis = S7AddressData.ParseFrom( address );
			if (!analysis.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( analysis );

			OperateResult<SoftBuffer> buffer = GetDataAreaFromS7Address( analysis.Content );
			if (!buffer.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( buffer );

			buffer.Content.SetBytes( value, analysis.Content.AddressStart / 8 );
			return OperateResult.CreateSuccessResult( );
		}

		#endregion

		#region Byte Read Write Operate

		/// <inheritdoc cref="SiemensS7Net.ReadByte(string)"/>
		[HslMqttApi( "ReadByte", "" )]
		public OperateResult<byte> ReadByte( string address ) => ByteTransformHelper.GetResultFromArray( Read( address, 1 ) );

		/// <inheritdoc cref="SiemensS7Net.Write(string, byte)"/>
		[HslMqttApi( "WriteByte", "" )]
		public OperateResult Write(string address, byte value ) => Write( address, new byte[] { value } );

		#endregion

		#region Bool Read Write Operate

		/// <inheritdoc cref="SiemensS7Net.ReadBool(string)"/>
		[HslMqttApi( "ReadBool", "" )]
		public override OperateResult<bool> ReadBool(string address )
		{
			OperateResult<S7AddressData> analysis = S7AddressData.ParseFrom( address );
			if (!analysis.IsSuccess) return OperateResult.CreateFailedResult<bool>( analysis );

			OperateResult<SoftBuffer> buffer = GetDataAreaFromS7Address( analysis.Content );
			if (!buffer.IsSuccess) return OperateResult.CreateFailedResult<bool>( buffer );

			return OperateResult.CreateSuccessResult( buffer.Content.GetBool( analysis.Content.AddressStart ) );
		}

		/// <inheritdoc cref="SiemensS7Net.Write(string, bool)"/>
		[HslMqttApi( "WriteBool", "" )]
		public override OperateResult Write(string address, bool value )
		{
			OperateResult<S7AddressData> analysis = S7AddressData.ParseFrom( address );
			if (!analysis.IsSuccess) return analysis;

			OperateResult<SoftBuffer> buffer = GetDataAreaFromS7Address( analysis.Content );
			if (!buffer.IsSuccess) return buffer;

			buffer.Content.SetBool( value, analysis.Content.AddressStart ); 
			return OperateResult.CreateSuccessResult( );
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
					OperateResult<byte[]> read1 = ReceiveByMessage( session.WorkSocket, 5000, new FetchWriteMessage( ) );
#else
					OperateResult<byte[]> read1 = await ReceiveByMessageAsync( session.WorkSocket, 5000, new FetchWriteMessage( ) );
#endif
					if (!read1.IsSuccess) { RemoveClient( session ); return; };

					if (!Authorization.asdniasnfaksndiqwhawfskhfaiw( )) { RemoveClient( session ); return; };

					LogNet?.WriteDebug( ToString( ), $"[{session.IpEndPoint}] Tcp {StringResources.Language.Receive}：{read1.Content.ToHexString( ' ' )}" );

					byte[] receive = read1.Content;
					byte[] back = null;
					if      (receive[5] == 0x03) back = WriteByMessage( receive );    // 写入数据
					else if (receive[5] == 0x05) back = ReadByMessage(  receive );    // 读取数据
					else
					{
						RemoveClient( session );
						return;
					}

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

		private SoftBuffer GetBufferFromCommand( byte[] command )
		{
			if (command[8] == 0x02) return memeryBuffer;
			if (command[8] == 0x03) return inputBuffer;
			if (command[8] == 0x04) return outputBuffer;
			if (command[8] == 0x01)
			{
				if (command[9] == 0x01) return db1BlockBuffer;
				if (command[9] == 0x02) return db2BlockBuffer; 
				if (command[9] == 0x03) return db3BlockBuffer;
				return dbOtherBlockBuffer;
			}
			if (command[8] == 0x06) return counterBuffer;
			if (command[8] == 0x07) return timerBuffer;
			return null;
		}

		private byte[] ReadByMessage( byte[] command )
		{
			SoftBuffer softBuffer = GetBufferFromCommand( command );
			int address = command[10] * 256 + command[11];
			int length  = command[12] * 256 + command[13];

			if (softBuffer == null) return PackCommandResponse( 0x06, 0x01, null );
			if (command[8] == 0x01 || command[8] == 0x06 || command[8] == 0x07)
			{
				return PackCommandResponse( 0x06, 0x00, softBuffer.GetBytes( address, length * 2 ) );
			}
			else
			{
				return PackCommandResponse( 0x06, 0x00, softBuffer.GetBytes( address, length ) );
			}
		}

		private byte[] WriteByMessage( byte[] command )
		{
			// 先判断是否有写入的权利，没有的话，直接返回写入异常
			if (!EnableWrite) return PackCommandResponse( 0x04, 0x01, null );

			SoftBuffer softBuffer = GetBufferFromCommand( command );
			int address = command[10] * 256 + command[11];
			int length  = command[12] * 256 + command[13];

			if (softBuffer == null) return PackCommandResponse( 0x04, 0x01, null );
			if (command[8] == 0x01 || command[8] == 0x06 || command[8] == 0x07)
			{
				if (length != (command.Length - 16) / 2) return PackCommandResponse( 0x04, 0x01, null );
				softBuffer.SetBytes( command.RemoveBegin( 16 ), address );
				return PackCommandResponse( 0x04, 0x00, null );
			}
			else
			{
				if (length != command.Length - 16) return PackCommandResponse( 0x04, 0x01, null );
				softBuffer.SetBytes( command.RemoveBegin( 16 ), address );
				return PackCommandResponse( 0x04, 0x00, null );
			}
		}

		private byte[] PackCommandResponse( byte opCode, byte err, byte[] data )
		{
			if (data == null) data = new byte[0];
			byte[] buffer = new byte[16 + data.Length];
			buffer[0] = 0x53;
			buffer[1] = 0x35;
			buffer[2] = 0x10;
			buffer[3] = 0x01;
			buffer[4] = 0x03;
			buffer[5] = opCode;
			buffer[6] = 0x0f;
			buffer[7] = 0x03;
			buffer[8] = err;
			buffer[9] = 0xff;
			buffer[10] = 0x07;
			if (data.Length > 0) data.CopyTo( buffer, 16 );
			return buffer;
		}

		#endregion

		#region Data Save Load Override

		/// <inheritdoc/>
		protected override  void LoadFromBytes( byte[] content )
		{
			if (content.Length < DataPoolLength * 9) throw new Exception( "File is not correct" );

			inputBuffer.SetBytes(        content, DataPoolLength * 0, 0, DataPoolLength );
			outputBuffer.SetBytes(       content, DataPoolLength * 1, 0, DataPoolLength );
			memeryBuffer.SetBytes(       content, DataPoolLength * 2, 0, DataPoolLength );
			db1BlockBuffer.SetBytes(     content, DataPoolLength * 3, 0, DataPoolLength );
			db2BlockBuffer.SetBytes(     content, DataPoolLength * 4, 0, DataPoolLength );
			db3BlockBuffer.SetBytes(     content, DataPoolLength * 5, 0, DataPoolLength );
			dbOtherBlockBuffer.SetBytes( content, DataPoolLength * 6, 0, DataPoolLength );
			counterBuffer.SetBytes(      content, DataPoolLength * 7, 0, DataPoolLength );
			timerBuffer.SetBytes(        content, DataPoolLength * 8, 0, DataPoolLength );
		}

		/// <inheritdoc/>
		protected override byte[] SaveToBytes( )
		{
			byte[] buffer = new byte[DataPoolLength * 9];
			Array.Copy( inputBuffer.GetBytes( ),           0, buffer, DataPoolLength * 0, DataPoolLength );
			Array.Copy( outputBuffer.GetBytes( ),          0, buffer, DataPoolLength * 1, DataPoolLength );
			Array.Copy( memeryBuffer.GetBytes( ),          0, buffer, DataPoolLength * 2, DataPoolLength );
			Array.Copy( db1BlockBuffer.GetBytes( ),        0, buffer, DataPoolLength * 3, DataPoolLength);
			Array.Copy( db2BlockBuffer.GetBytes( ),        0, buffer, DataPoolLength * 4, DataPoolLength );
			Array.Copy( db3BlockBuffer.GetBytes( ),        0, buffer, DataPoolLength * 5, DataPoolLength );
			Array.Copy( dbOtherBlockBuffer.GetBytes( ),    0, buffer, DataPoolLength * 6, DataPoolLength );
			Array.Copy( counterBuffer.GetBytes( ),         0, buffer, DataPoolLength * 7, DataPoolLength );
			Array.Copy( timerBuffer.GetBytes( ),           0, buffer, DataPoolLength * 8, DataPoolLength );

			return buffer;
		}

		#endregion

		#region IDisposable Support

		/// <inheritdoc/>
		protected override void Dispose( bool disposing )
		{
			if (disposing)
			{
				inputBuffer?.Dispose( );
				outputBuffer?.Dispose( );
				memeryBuffer?.Dispose( );
				db1BlockBuffer?.Dispose( );
				db2BlockBuffer?.Dispose( );
				db3BlockBuffer?.Dispose( );
				dbOtherBlockBuffer?.Dispose( );
			}
			base.Dispose( disposing );
		}

		#endregion

		#region Private Member

		private SoftBuffer inputBuffer;                // 输入寄存器的数据池
		private SoftBuffer outputBuffer;               // 输出寄存器数据池
		private SoftBuffer memeryBuffer;               // 特殊寄存器的数据池
		private SoftBuffer counterBuffer;              // 计数器寄存器数据池
		private SoftBuffer timerBuffer;                // 定时器寄存器的数据池
		private SoftBuffer db1BlockBuffer;             // 数据寄存器的数据池
		private SoftBuffer db2BlockBuffer;             // 数据寄存器的数据池
		private SoftBuffer db3BlockBuffer;             // 数据寄存器的数据池
		private SoftBuffer dbOtherBlockBuffer;         // 数据寄存器的数据池
		private const int DataPoolLength = 65536;      // 数据的长度

		#endregion

		#region Object Override

		/// <inheritdoc/>
		public override string ToString( ) => $"SiemensFetchWriteServer[{Port}]";

		#endregion
	}
}
