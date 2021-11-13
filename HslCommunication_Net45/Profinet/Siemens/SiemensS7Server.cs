using HslCommunication.BasicFramework;
using HslCommunication.Core;
using HslCommunication.Core.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using HslCommunication.Core.IMessage;
using HslCommunication.Core.Address;
using HslCommunication.Reflection;

namespace HslCommunication.Profinet.Siemens
{
	/// <summary>
	/// <b>[商业授权]</b> 西门子S7协议的虚拟服务器，支持TCP协议，模拟的是1200的PLC进行通信，在客户端进行操作操作的时候，最好是选择1200的客户端对象进行通信。<br />
	/// <b>[Authorization]</b> The virtual server of Siemens S7 protocol supports TCP protocol. It simulates 1200 PLC for communication. When the client is operating, it is best to select the 1200 client object for communication.
	/// </summary>
	/// <remarks>
	/// 本西门子的虚拟PLC仅限商业授权用户使用，感谢支持。
	/// <note type="important">对于200smartPLC的V区，就是DB1.X，例如，V100=DB1.100</note>
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
	///   <item>
	///     <term>定时器的值</term>
	///     <term>T</term>
	///     <term>T100,T200</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term>未测试通过</term>
	///   </item>
	///   <item>
	///     <term>计数器的值</term>
	///     <term>C</term>
	///     <term>C100,C200</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term>未测试通过</term>
	///   </item>
	/// </list>
	/// 你可以很快速并且简单的创建一个虚拟的s7服务器
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Profinet\SiemensS7ServerExample.cs" region="UseExample1" title="简单的创建服务器" />
	/// 当然如果需要高级的服务器，指定日志，限制客户端的IP地址，获取客户端发送的信息，在服务器初始化的时候就要参照下面的代码：
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Profinet\SiemensS7ServerExample.cs" region="UseExample4" title="定制服务器" />
	/// 服务器创建好之后，我们就可以对服务器进行一些读写的操作了，下面的代码是基础的BCL类型的读写操作。
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Profinet\SiemensS7ServerExample.cs" region="ReadWriteExample" title="基础的读写示例" />
	/// 高级的对于byte数组类型的数据进行批量化的读写操作如下：   
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Profinet\SiemensS7ServerExample.cs" region="BytesReadWrite" title="字节的读写示例" />
	/// 更高级操作请参见源代码。
	/// </example>
	public class SiemensS7Server : NetworkDataServerBase
	{
		#region Constructor

		/// <summary>
		/// 实例化一个S7协议的服务器，支持I，Q，M，DB1.X, DB2.X, DB3.X 数据区块的读写操作<br />
		/// Instantiate a server with S7 protocol, support I, Q, M, DB1.X data block read and write operations
		/// </summary>
		public SiemensS7Server( )
		{
			// 四个数据池初始化，输入寄存器，输出寄存器，中间寄存器，DB块寄存器
			inputBuffer             = new SoftBuffer( DataPoolLength );
			outputBuffer            = new SoftBuffer( DataPoolLength );
			memeryBuffer            = new SoftBuffer( DataPoolLength );
			db1BlockBuffer          = new SoftBuffer( DataPoolLength );
			db2BlockBuffer          = new SoftBuffer( DataPoolLength );
			db3BlockBuffer          = new SoftBuffer( DataPoolLength );
			dbOtherBlockBuffer      = new SoftBuffer( DataPoolLength );
			countBuffer             = new SoftBuffer( DataPoolLength * 2 );
			timerBuffer             = new SoftBuffer( DataPoolLength * 2 );
			aiBuffer                = new SoftBuffer( DataPoolLength );
			aqBuffer                = new SoftBuffer( DataPoolLength );

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
				case 0x1E: return OperateResult.CreateSuccessResult( countBuffer );
				case 0x1F: return OperateResult.CreateSuccessResult( timerBuffer );
				case 0x06: return OperateResult.CreateSuccessResult( aiBuffer );
				case 0x07: return OperateResult.CreateSuccessResult( aqBuffer );
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

			if (analysis.Content.DataCode == 0x1E || analysis.Content.DataCode == 0x1F)
				return OperateResult.CreateSuccessResult( buffer.Content.GetBytes( analysis.Content.AddressStart * 2, length * 2 ) );
			else
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

			if (analysis.Content.DataCode == 0x1E || analysis.Content.DataCode == 0x1F)
				buffer.Content.SetBytes( value, analysis.Content.AddressStart * 2 );
			else
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
			// 接收2次的握手协议
			S7Message s7Message = new S7Message( );
			OperateResult<byte[]> read1 = ReceiveByMessage( socket, 5000, s7Message );
			if (!read1.IsSuccess) return;

			OperateResult send1 = Send( socket, SoftBasic.HexStringToBytes( @"03 00 00 16 02 D0 80 32 01 00 00 02 00 00 08 00 00 f0 00 00 01 00" ) );
			if (!send1.IsSuccess) return;

			OperateResult<byte[]> read2 = ReceiveByMessage( socket, 5000, s7Message );
			if (!read2.IsSuccess) return;

			OperateResult send2 = Send( socket, SoftBasic.HexStringToBytes( @"03 00 00 1B 02 f0 80 32 01 00 00 02 00 00 08 00 00 00 00 00 01 00 01 00 f0 00 f0" ) );
			if (!send2.IsSuccess) return;

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
					OperateResult<byte[]> read1 = ReceiveByMessage( session.WorkSocket, 5000, new S7Message( ) );
#else
					OperateResult<byte[]> read1 = await ReceiveByMessageAsync( session.WorkSocket, 5000, new S7Message( ) );
#endif
					if (!read1.IsSuccess) { RemoveClient( session ); return; };

					if (!Authorization.asdniasnfaksndiqwhawfskhfaiw( )) { RemoveClient( session ); return; };

					LogNet?.WriteDebug( ToString( ), $"[{session.IpEndPoint}] Tcp {StringResources.Language.Receive}：{read1.Content.ToHexString( ' ' )}" );

					byte[] receive = read1.Content;
					byte[] back = null;
					if      (receive[17] == 0x04) back = ReadByMessage( receive );    // 读取数据
					else if (receive[17] == 0x05) back = WriteByMessage( receive );   // 写入数据
					else if (receive[17] == 0x00) back = SoftBasic.HexStringToBytes( "03 00 00 7D 02 F0 80 32 07 00 00 00 01 00 0C 00 60 00 01 12 08 12 84 01 01 00 00 00 00 FF" +
							" 09 00 5C 00 11 00 00 00 1C 00 03 00 01 36 45 53 37 20 32 31 35 2D 31 41 47 34 30 2D 30 58 42 30 20 00 00 00 06 20 20 00 06 36 45 53 37 20" +
							" 32 31 35 2D 31 41 47 34 30 2D 30 58 42 30 20 00 00 00 06 20 20 00 07 36 45 53 37 20 32 31 35 2D 31 41 47 34 30 2D 30 58 42 30 20 00 00 56 04 02 01" );
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

		private byte[] ReadByMessage( byte[] packCommand )
		{
			List<byte> content = new List<byte>( );
			int count = packCommand[18];
			int index = 19;
			for (int i = 0; i < count; i++)
			{
				byte length = packCommand[index + 1];
				byte[] command = packCommand.SelectMiddle( index, length + 2 );
				index += length + 2;

				content.AddRange( ReadByCommand( command ) );
			}

			byte[] back = new byte[21 + content.Count];
			SoftBasic.HexStringToBytes( "03 00 00 1A 02 F0 80 32 03 00 00 00 01 00 02 00 05 00 00 04 01" ).CopyTo( back, 0 );
			back[2] = (byte)(back.Length / 256);
			back[3] = (byte)(back.Length % 256);
			back[15] = (byte)(content.Count / 256);
			back[16] = (byte)(content.Count % 256);
			back[20] = packCommand[18];
			content.CopyTo( back, 21 );
			return back;
		}

		private byte[] ReadByCommand(byte[] command )
		{
			if(command[3] == 0x01)
			{
				// 位读取
				int startIndex = command[9] * 65536 + command[10] * 256 + command[11];
				ushort dbBlock = ByteTransform.TransUInt16( command, 6 );

				OperateResult<SoftBuffer> buffer = GetDataAreaFromS7Address( new S7AddressData( ) { AddressStart = startIndex, DataCode = command[8], DbBlock = dbBlock, Length = 1 } );
				if (!buffer.IsSuccess) throw new Exception( buffer.Message );

				return PackReadBitCommandBack( buffer.Content.GetBool( startIndex ) );
			}
			else if (command[3] == 0x1E || command[3] == 0x1F)
			{
				// 定时器，计数器读取
				ushort length = ByteTransform.TransUInt16( command, 4 );
				int startIndex = command[9] * 65536 + command[10] * 256 + command[11];

				OperateResult<SoftBuffer> buffer = GetDataAreaFromS7Address( new S7AddressData( ) { AddressStart = startIndex, DataCode = command[8], DbBlock = 0, Length = length } );
				if (!buffer.IsSuccess) throw new Exception( buffer.Message );

				return PackReadCTCommandBack( buffer.Content.GetBytes( startIndex * 2, length * 2 ), command[3] == 0x1E ? 0x03 : 0x05 );
			}
			else
			{
				// 字读取
				ushort length = ByteTransform.TransUInt16( command, 4 );
				if (command[3] == 0x04) length *= 2;
				ushort dbBlock = ByteTransform.TransUInt16( command, 6 );
				int startIndex = (command[9] * 65536 + command[10] * 256 + command[11]) / 8;

				OperateResult<SoftBuffer> buffer = GetDataAreaFromS7Address( new S7AddressData( ) { AddressStart = startIndex, DataCode = command[8], DbBlock = dbBlock, Length = length } );
				if (!buffer.IsSuccess) throw new Exception( buffer.Message );
				
				return PackReadWordCommandBack( buffer.Content.GetBytes( startIndex, length ) );
			}
		}

		private byte[] PackReadWordCommandBack( byte[] result )
		{
			byte[] back = new byte[4 + result.Length];
			back[0] = 0xFF;
			back[1] = 0x04;

			ByteTransform.TransByte( (ushort)result.Length ).CopyTo( back, 2 );
			result.CopyTo( back, 4 );
			return back;
		}

		private byte[] PackReadCTCommandBack( byte[] result, int dataLength )
		{
			byte[] back = new byte[4 + result.Length * dataLength / 2];
			back[0] = 0xFF;
			back[1] = 0x09;

			ByteTransform.TransByte( (ushort)(back.Length - 4 ) ).CopyTo( back, 2 );
			for (int i = 0; i < result.Length / 2; i++)
			{
				result.SelectMiddle( i * 2, 2 ).CopyTo( back, 4 + dataLength - 2 + i * dataLength );
			}
			return back;
		}

		private byte[] PackReadBitCommandBack( bool value )
		{
			byte[] back = new byte[5];
			back[0] = 0xFF;
			back[1] = 0x03;
			back[2] = 0x00;
			back[3] = 0x01;
			back[4] = (byte)(value ? 0x01 : 0x00);
			return back;
		}

		private byte[] WriteByMessage( byte[] packCommand )
		{
			// 先判断是否有写入的权利，没有的话，直接返回写入异常
			if (!EnableWrite) return SoftBasic.HexStringToBytes( "03 00 00 16 02 F0 80 32 03 00 00 00 01 00 02 00 01 00 00 05 01 04" );

			if (packCommand[22] == 0x02 || packCommand[22] == 0x04)
			{
				// 字写入
				ushort dbBlock = ByteTransform.TransUInt16( packCommand, 25 );
				int count = ByteTransform.TransInt16( packCommand, 23 );
				if (packCommand[22] == 0x04) count *= 2;
				int startIndex = (packCommand[28] * 65536 + packCommand[29] * 256 + packCommand[30]) / 8;
				byte[] data = ByteTransform.TransByte( packCommand, 35, count );

				OperateResult<SoftBuffer> buffer = GetDataAreaFromS7Address( new S7AddressData( ) { DataCode = packCommand[27], DbBlock = dbBlock, Length = 1 } );
				if (!buffer.IsSuccess) throw new Exception( buffer.Message );

				buffer.Content.SetBytes( data, startIndex );
				return SoftBasic.HexStringToBytes( "03 00 00 16 02 F0 80 32 03 00 00 00 01 00 02 00 01 00 00 05 01 FF" );
			}
			else
			{
				// 位写入
				ushort dbBlock = ByteTransform.TransUInt16( packCommand, 25 );
				int startIndex = packCommand[28] * 65536 + packCommand[29] * 256 + packCommand[30];
				bool value = packCommand[35] != 0x00;

				OperateResult<SoftBuffer> buffer = GetDataAreaFromS7Address( new S7AddressData( ) { DataCode = packCommand[27], DbBlock = dbBlock, Length = 1 } );
				if (!buffer.IsSuccess) throw new Exception( buffer.Message );

				buffer.Content.SetBool( value, startIndex );
				return SoftBasic.HexStringToBytes( "03 00 00 16 02 F0 80 32 03 00 00 00 01 00 02 00 01 00 00 05 01 FF" );
			}
		}

		#endregion

		#region Data Save Load Override

		/// <inheritdoc/>
		protected override  void LoadFromBytes( byte[] content )
		{
			if (content.Length < DataPoolLength * 7) throw new Exception( "File is not correct" );

			inputBuffer.SetBytes(        content, DataPoolLength * 0, 0, DataPoolLength );
			outputBuffer.SetBytes(       content, DataPoolLength * 1, 0, DataPoolLength );
			memeryBuffer.SetBytes(       content, DataPoolLength * 2, 0, DataPoolLength );
			db1BlockBuffer.SetBytes(     content, DataPoolLength * 3, 0, DataPoolLength );
			db2BlockBuffer.SetBytes(     content, DataPoolLength * 4, 0, DataPoolLength );
			db3BlockBuffer.SetBytes(     content, DataPoolLength * 5, 0, DataPoolLength );
			dbOtherBlockBuffer.SetBytes( content, DataPoolLength * 6, 0, DataPoolLength );

			if (content.Length < DataPoolLength * 11) return;
			countBuffer.SetBytes(        content, DataPoolLength * 7, 0, DataPoolLength * 2 );
			timerBuffer.SetBytes(        content, DataPoolLength * 9, 0, DataPoolLength * 2 );
		}

		/// <inheritdoc/>
		protected override byte[] SaveToBytes( )
		{
			byte[] buffer = new byte[DataPoolLength * 11];
			Array.Copy( inputBuffer.GetBytes( ),           0, buffer, DataPoolLength * 0, DataPoolLength );
			Array.Copy( outputBuffer.GetBytes( ),          0, buffer, DataPoolLength * 1, DataPoolLength );
			Array.Copy( memeryBuffer.GetBytes( ),          0, buffer, DataPoolLength * 2, DataPoolLength );
			Array.Copy( db1BlockBuffer.GetBytes( ),        0, buffer, DataPoolLength * 3, DataPoolLength);
			Array.Copy( db2BlockBuffer.GetBytes( ),        0, buffer, DataPoolLength * 4, DataPoolLength );
			Array.Copy( db3BlockBuffer.GetBytes( ),        0, buffer, DataPoolLength * 5, DataPoolLength );
			Array.Copy( dbOtherBlockBuffer.GetBytes( ),    0, buffer, DataPoolLength * 6, DataPoolLength );
			Array.Copy( countBuffer.GetBytes( ),           0, buffer, DataPoolLength * 7, DataPoolLength * 2 );
			Array.Copy( timerBuffer.GetBytes( ),           0, buffer, DataPoolLength * 9, DataPoolLength * 2 );

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
		private SoftBuffer outputBuffer;               // 离散输入的数据池
		private SoftBuffer memeryBuffer;               // 寄存器的数据池
		private SoftBuffer countBuffer;                // 计数器的数据池
		private SoftBuffer timerBuffer;                // 定时器的数据池
		private SoftBuffer db1BlockBuffer;             // 输入寄存器的数据池
		private SoftBuffer db2BlockBuffer;             // 输入寄存器的数据池
		private SoftBuffer db3BlockBuffer;             // 输入寄存器的数据池
		private SoftBuffer dbOtherBlockBuffer;         // 输入寄存器的数据池
		private SoftBuffer aiBuffer;                   // AI缓存数据池
		private SoftBuffer aqBuffer;                   // AQ缓存数据池
		private const int DataPoolLength = 65536;      // 数据的长度

		#endregion

		#region Object Override

		/// <inheritdoc/>
		public override string ToString( ) => $"SiemensS7Server[{Port}]";

		#endregion
	}
}
