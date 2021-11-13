using HslCommunication.BasicFramework;
using HslCommunication.Core;
using HslCommunication.Core.Address;
using HslCommunication.Core.Net;
using HslCommunication.Reflection;
using HslCommunication.Core.IMessage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;


namespace HslCommunication.Profinet.Yokogawa
{
	/// <summary>
	/// <b>[商业授权]</b> 横河PLC的虚拟服务器，支持X,Y,I,E,M,T,C,L继电器类型的数据读写，支持D,B,F,R,V,Z,W,TN,CN寄存器类型的数据读写，可以用来测试横河PLC的二进制通信类型<br />
	/// <b>[Authorization]</b> Yokogawa PLC's virtual server, supports X, Y, I, E, M, T, C, L relay type data read and write, 
	/// supports D, B, F, R, V, Z, W, TN, CN register types The data read and write can be used to test the binary communication type of Yokogawa PLC
	/// </summary>
	/// <remarks>
	/// 其中的X继电器可以在服务器进行读写操作，但是远程的PLC只能进行读取，所有的数据读写的最大的范围按照协议进行了限制。
	/// </remarks>
	/// <example>
	/// 地址示例如下：
	/// <list type="table">
	///   <listheader>
	///     <term>地址名称</term>
	///     <term>地址代号</term>
	///     <term>示例</term>
	///     <term>字操作</term>
	///     <term>位操作</term>
	///     <term>备注</term>
	///   </listheader>
	///   <item>
	///     <term>Input relay</term>
	///     <term>X</term>
	///     <term>X100,X200</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term>服务器端可读可写</term>
	///   </item>
	///   <item>
	///     <term>Output relay</term>
	///     <term>Y</term>
	///     <term>Y100,Y200</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>Internal relay</term>
	///     <term>I</term>
	///     <term>I100,I200</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>Share relay</term>
	///     <term>E</term>
	///     <term>E100,E200</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>Special relay</term>
	///     <term>M</term>
	///     <term>M100,M200</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>Time relay</term>
	///     <term>T</term>
	///     <term>T100,T200</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>Counter relay</term>
	///     <term>C</term>
	///     <term>C100,C200</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>link relay</term>
	///     <term>L</term>
	///     <term>L100, L200</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>Data register</term>
	///     <term>D</term>
	///     <term>D100,D200</term>
	///     <term>√</term>
	///     <term>×</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>File register</term>
	///     <term>B</term>
	///     <term>B100,B200</term>
	///     <term>√</term>
	///     <term>×</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>Cache register</term>
	///     <term>F</term>
	///     <term>F100,F200</term>
	///     <term>√</term>
	///     <term>×</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>Shared register</term>
	///     <term>R</term>
	///     <term>R100,R200</term>
	///     <term>√</term>
	///     <term>×</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>Index register</term>
	///     <term>V</term>
	///     <term>V100,V200</term>
	///     <term>√</term>
	///     <term>×</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>Special register</term>
	///     <term>Z</term>
	///     <term>Z100,Z200</term>
	///     <term>√</term>
	///     <term>×</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>Link register</term>
	///     <term>W</term>
	///     <term>W100,W200</term>
	///     <term>√</term>
	///     <term>×</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>Timer current value</term>
	///     <term>TN</term>
	///     <term>TN100,TN200</term>
	///     <term>√</term>
	///     <term>×</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>Counter current value</term>
	///     <term>CN</term>
	///     <term>CN100,CN200</term>
	///     <term>√</term>
	///     <term>×</term>
	///     <term></term>
	///   </item>
	/// </list>
	/// 你可以很快速并且简单的创建一个虚拟的横河服务器
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Profinet\YokogawaLinkServerSample.cs" region="UseExample1" title="简单的创建服务器" />
	/// 当然如果需要高级的服务器，指定日志，限制客户端的IP地址，获取客户端发送的信息，在服务器初始化的时候就要参照下面的代码：
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Profinet\YokogawaLinkServerSample.cs" region="UseExample4" title="定制服务器" />
	/// 服务器创建好之后，我们就可以对服务器进行一些读写的操作了，下面的代码是基础的BCL类型的读写操作。
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Profinet\YokogawaLinkServerSample.cs" region="ReadWriteExample" title="基础的读写示例" />
	/// 高级的对于byte数组类型的数据进行批量化的读写操作如下：   
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Profinet\YokogawaLinkServerSample.cs" region="BytesReadWrite" title="字节的读写示例" />
	/// 更高级操作请参见源代码。
	/// </example>
	public class YokogawaLinkServer : NetworkDataServerBase
	{
		#region Constructor

		/// <summary>
		/// 实例化一个横河PLC的服务器，支持X,Y,I,E,M,T,C,L继电器类型的数据读写，支持D,B,F,R,V,Z,W,TN,CN寄存器类型的数据读写<br />
		/// Instantiate a Yokogawa PLC server, support X, Y, I, E, M, T, C, L relay type data read and write, 
		/// support D, B, F, R, V, Z, W, TN, CN Register type data reading and writing
		/// </summary>
		public YokogawaLinkServer( )
		{
			// 四个数据池初始化，输入寄存器，输出寄存器，中间寄存器，DB块寄存器
			xBuffer       = new SoftBuffer( DataPoolLength );
			yBuffer       = new SoftBuffer( DataPoolLength );
			iBuffer       = new SoftBuffer( DataPoolLength );
			eBuffer       = new SoftBuffer( DataPoolLength );
			mBuffer       = new SoftBuffer( DataPoolLength );
			lBuffer       = new SoftBuffer( DataPoolLength );
			dBuffer       = new SoftBuffer( DataPoolLength * 2 );
			bBuffer       = new SoftBuffer( DataPoolLength * 2 );
			fBuffer       = new SoftBuffer( DataPoolLength * 2 );
			rBuffer       = new SoftBuffer( DataPoolLength * 2 );
			vBuffer       = new SoftBuffer( DataPoolLength * 2 );
			zBuffer       = new SoftBuffer( DataPoolLength * 2 );
			wBuffer       = new SoftBuffer( DataPoolLength * 2 );
			specialBuffer = new SoftBuffer( DataPoolLength * 2 );

			WordLength               = 2;
			ByteTransform            = new ReverseWordTransform( );
			ByteTransform.DataFormat = DataFormat.CDAB;
			transform                = new ReverseBytesTransform( );
		}

		#endregion

		#region NetworkDataServerBase Override

		private OperateResult<SoftBuffer> GetDataAreaFromYokogawaAddress( YokogawaLinkAddress yokogawaAddress, bool isBit )
		{
			if (isBit)
			{
				switch (yokogawaAddress.DataCode)
				{
					case 0x18: return OperateResult.CreateSuccessResult( xBuffer );
					case 0x19: return OperateResult.CreateSuccessResult( yBuffer );
					case 0x09: return OperateResult.CreateSuccessResult( iBuffer );
					case 0x05: return OperateResult.CreateSuccessResult( eBuffer );
					case 0x0D: return OperateResult.CreateSuccessResult( mBuffer );
					case 0x0C: return OperateResult.CreateSuccessResult( lBuffer );
					default: return new OperateResult<SoftBuffer>( StringResources.Language.NotSupportedDataType );
				}
			}
			else
			{
				switch (yokogawaAddress.DataCode)
				{
					case 0x18: return OperateResult.CreateSuccessResult( xBuffer );
					case 0x19: return OperateResult.CreateSuccessResult( yBuffer );
					case 0x09: return OperateResult.CreateSuccessResult( iBuffer );
					case 0x05: return OperateResult.CreateSuccessResult( eBuffer );
					case 0x0D: return OperateResult.CreateSuccessResult( mBuffer );
					case 0x0C: return OperateResult.CreateSuccessResult( lBuffer );

					case 0x04: return OperateResult.CreateSuccessResult( dBuffer );
					case 0x02: return OperateResult.CreateSuccessResult( bBuffer );
					case 0x06: return OperateResult.CreateSuccessResult( fBuffer );
					case 0x12: return OperateResult.CreateSuccessResult( rBuffer );
					case 0x16: return OperateResult.CreateSuccessResult( vBuffer );
					case 0x1A: return OperateResult.CreateSuccessResult( zBuffer );
					case 0x17: return OperateResult.CreateSuccessResult( wBuffer );
					default: return new OperateResult<SoftBuffer>( StringResources.Language.NotSupportedDataType );
				}
			}
		}

		/// <inheritdoc cref="YokogawaLinkTcp.Read(string, ushort)"/>
		[HslMqttApi( "ReadByteArray", "" )]
		public override OperateResult<byte[]> Read( string address, ushort length )
		{
			if (address.StartsWith( "Special:" ) || address.StartsWith( "special:" ))
			{
				address = address.Substring( 8 );

				OperateResult<int> unit = HslHelper.ExtractParameter( ref address, "unit" );
				OperateResult<int> slot = HslHelper.ExtractParameter( ref address, "slot" );

				try
				{
					return OperateResult.CreateSuccessResult( specialBuffer.GetBytes( ushort.Parse( address ) * 2, length * 2 ) );
				}
				catch (Exception ex)
				{
					return new OperateResult<byte[]>( "Address format wrong: " + ex.Message );
				}
			}

			OperateResult<YokogawaLinkAddress> analysis = YokogawaLinkAddress.ParseFrom( address, length );
			if (!analysis.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( analysis );

			OperateResult<SoftBuffer> buffer = GetDataAreaFromYokogawaAddress( analysis.Content, false );
			if(!buffer.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( buffer );

			if (analysis.Content.DataCode == 0x18 ||
				analysis.Content.DataCode == 0x19 ||
				analysis.Content.DataCode == 0x09 ||
				analysis.Content.DataCode == 0x05 ||
				analysis.Content.DataCode == 0x0D ||
				analysis.Content.DataCode == 0x0C)
			{
				return OperateResult.CreateSuccessResult(
					buffer.Content.GetBytes( analysis.Content.AddressStart, length * 16 ).Select( m => m != 0x00 ).ToArray( ).ToByteArray( ) );
			}
			return OperateResult.CreateSuccessResult( buffer.Content.GetBytes( analysis.Content.AddressStart * 2, length * 2 ) );
		}

		/// <inheritdoc cref="YokogawaLinkTcp.Write(string, byte[])"/>
		[HslMqttApi( "WriteByteArray", "" )]
		public override OperateResult Write( string address, byte[] value )
		{
			if (address.StartsWith( "Special:" ) || address.StartsWith( "special:" ))
			{
				address = address.Substring( 8 );

				OperateResult<int> unit = HslHelper.ExtractParameter( ref address, "unit" );
				OperateResult<int> slot = HslHelper.ExtractParameter( ref address, "slot" );

				try
				{
					specialBuffer.SetBytes( value, ushort.Parse( address ) * 2 );
					return OperateResult.CreateSuccessResult( );
				}
				catch (Exception ex)
				{
					return new OperateResult( "Address format wrong: " + ex.Message );
				}
			}

			OperateResult<YokogawaLinkAddress> analysis = YokogawaLinkAddress.ParseFrom( address, 0 );
			if (!analysis.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( analysis );

			OperateResult<SoftBuffer> buffer = GetDataAreaFromYokogawaAddress( analysis.Content, false );
			if (!buffer.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( buffer );

			if (analysis.Content.DataCode == 0x18 ||
				analysis.Content.DataCode == 0x19 ||
				analysis.Content.DataCode == 0x09 ||
				analysis.Content.DataCode == 0x05 ||
				analysis.Content.DataCode == 0x0D ||
				analysis.Content.DataCode == 0x0C)
			{
				buffer.Content.SetBytes( value.ToBoolArray( ).Select( m => m ? (byte)0x01 : (byte)0x00 ).ToArray( ), analysis.Content.AddressStart );
			}
			else
				buffer.Content.SetBytes( value, analysis.Content.AddressStart * 2 );
			return OperateResult.CreateSuccessResult( );
		}

		#endregion

		#region Bool Read Write Operate

		/// <inheritdoc cref="YokogawaLinkTcp.ReadBool(string,ushort)"/>
		[HslMqttApi( "ReadBoolArray", "" )]
		public override OperateResult<bool[]> ReadBool(string address, ushort length )
		{
			OperateResult<YokogawaLinkAddress> analysis = YokogawaLinkAddress.ParseFrom( address, length );
			if (!analysis.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( analysis );

			OperateResult<SoftBuffer> buffer = GetDataAreaFromYokogawaAddress( analysis.Content, true );
			if (!buffer.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( buffer );

			return OperateResult.CreateSuccessResult( mBuffer.GetBytes( analysis.Content.AddressStart, length ).Select( m => m != 0x00 ).ToArray( ) );
		}

		/// <inheritdoc cref="YokogawaLinkTcp.Write(string, bool[])"/>
		[HslMqttApi( "WriteBoolArray", "" )]
		public override OperateResult Write(string address, bool[] value )
		{
			OperateResult<YokogawaLinkAddress> analysis = YokogawaLinkAddress.ParseFrom( address, 0 );
			if (!analysis.IsSuccess) return analysis;

			OperateResult<SoftBuffer> buffer = GetDataAreaFromYokogawaAddress( analysis.Content, true );
			if (!buffer.IsSuccess) return buffer;

			buffer.Content.SetBytes( value.Select( m => m ? (byte)1 : (byte)0 ).ToArray( ), analysis.Content.AddressStart );
			return OperateResult.CreateSuccessResult( );
		}

		#endregion

		#region Start Stop

		/// <summary>
		/// 如果未执行程序，则开始执行程序<br />
		/// Starts executing a program if it is not being executed
		/// </summary>
		[HslMqttApi( Description = "Starts executing a program if it is not being executed" )]
		public void StartProgram( )
		{
			isProgramStarted = true;
		}

		/// <summary>
		/// 停止当前正在执行程序<br />
		/// Stops the executing program.
		/// </summary>
		[HslMqttApi( Description = "Stops the executing program." )]
		public void StopProgram( )
		{
			isProgramStarted = false;
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
					OperateResult<byte[]> read1 = ReceiveByMessage( session.WorkSocket, 5000, new YokogawaLinkBinaryMessage( ) );
#else
					OperateResult<byte[]> read1 = await ReceiveByMessageAsync( session.WorkSocket, 5000, new YokogawaLinkBinaryMessage( ) );
#endif
					if (!read1.IsSuccess) { RemoveClient( session ); return; };

					if (!Authorization.asdniasnfaksndiqwhawfskhfaiw( )) { RemoveClient( session ); return; };

					LogNet?.WriteDebug( ToString( ), $"[{session.IpEndPoint}] Tcp {StringResources.Language.Receive}：{read1.Content.ToHexString( ' ' )}" );

					byte[] receive = read1.Content;
					byte[] back = null;
					if      (receive[0] == 0x01) back = ReadBoolByCommand(        receive );
					else if (receive[0] == 0x02) back = WriteBoolByCommand(       receive );
					else if (receive[0] == 0x04) back = ReadRandomBoolByCommand(  receive );
					else if (receive[0] == 0x05) back = WriteRandomBoolByCommand( receive );
					else if (receive[0] == 0x11) back = ReadWordByCommand(        receive );
					else if (receive[0] == 0x12) back = WriteWordByCommand(       receive );
					else if (receive[0] == 0x14) back = ReadRandomWordByCommand(  receive );
					else if (receive[0] == 0x15) back = WriteRandomWordByCommand( receive );
					else if (receive[0] == 0x31) back = ReadSpecialModule(        receive );
					else if (receive[0] == 0x32) back = WriteSpecialModule(       receive );
					else if (receive[0] == 0x45) back = StartByCommand(           receive );
					else if (receive[0] == 0x46) back = StopByCommand(            receive );
					else if (receive[0] == 0x61) throw new RemoteCloseException( );
					else if (receive[0] == 0x62) back = ReadSystemByCommand(      receive );
					else if (receive[0] == 0x63) back = ReadSystemDateTime(       receive );
					else back = PackCommandBack( receive[0], 0x03, null );

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

		private byte[] ReadBoolByCommand( byte[] command )
		{
			int address = transform.TransInt32( command, 6 );
			int length = transform.TransUInt16( command, 10 );

			if (address > 65535 || address < 0) return PackCommandBack( command[0], 0x04, null );
			if (length > 256)                   return PackCommandBack( command[0], 0x05, null );
			if (address + length > 65535)       return PackCommandBack( command[0], 0x05, null );
			switch (command[5])
			{
				case 0x18: return PackCommandBack( command[0], 0x00, xBuffer.GetBytes( address, length ) );
				case 0x19: return PackCommandBack( command[0], 0x00, yBuffer.GetBytes( address, length ) );
				case 0x09: return PackCommandBack( command[0], 0x00, iBuffer.GetBytes( address, length ) );
				case 0x05: return PackCommandBack( command[0], 0x00, eBuffer.GetBytes( address, length ) );
				case 0x0D: return PackCommandBack( command[0], 0x00, mBuffer.GetBytes( address, length ) );
				case 0x0C: return PackCommandBack( command[0], 0x00, lBuffer.GetBytes( address, length ) );
				default:   return PackCommandBack( command[0], 0x03, null );
			}
		}

		private byte[] WriteBoolByCommand( byte[] command )
		{
			// 先判断是否有写入的权利，没有的话，直接返回写入异常
			if (!EnableWrite) return PackCommandBack( command[0], 0x03, null );

			int address = transform.TransInt32( command, 6 );
			int length = transform.TransUInt16( command, 10 );

			if (address > 65535 || address < 0)  return PackCommandBack( command[0], 0x04, null );
			if (length > 256)                    return PackCommandBack( command[0], 0x05, null );
			if (address + length > 65535)        return PackCommandBack( command[0], 0x05, null );
			if (length != (command.Length - 12)) return PackCommandBack( command[0], 0x05, null );
			switch (command[5])
			{
				case 0x18: return PackCommandBack( command[0], 0x03, null ); // X不允许写入
				case 0x19: yBuffer.SetBytes( command.RemoveBegin( 12 ), address ); return PackCommandBack( command[0], 0x00, null );
				case 0x09: iBuffer.SetBytes( command.RemoveBegin( 12 ), address ); return PackCommandBack( command[0], 0x00, null );
				case 0x05: eBuffer.SetBytes( command.RemoveBegin( 12 ), address ); return PackCommandBack( command[0], 0x00, null );
				case 0x0D: mBuffer.SetBytes( command.RemoveBegin( 12 ), address ); return PackCommandBack( command[0], 0x00, null );
				case 0x0C: lBuffer.SetBytes( command.RemoveBegin( 12 ), address ); return PackCommandBack( command[0], 0x00, null );
				default:   return PackCommandBack( command[0], 0x03, null );
			}
		}

		private byte[] ReadRandomBoolByCommand( byte[] command )
		{
			int length = transform.TransUInt16( command, 4 );
			if (length > 32)                      return PackCommandBack( command[0], 0x05, null );
			if (length * 6 != command.Length - 6) return PackCommandBack( command[0], 0x05, null );

			byte[] buffer = new byte[length];
			for (int i = 0; i < length; i++)
			{
				int address = transform.TransInt32( command, 8 + 6 * i );
				if (address > 65535 || address < 0) return PackCommandBack( command[0], 0x04, null );

				switch (command[7 + i * 6])
				{
					case 0x18: buffer[i] = xBuffer.GetBytes( address, 1 )[0]; break;
					case 0x19: buffer[i] = yBuffer.GetBytes( address, 1 )[0]; break;
					case 0x09: buffer[i] = iBuffer.GetBytes( address, 1 )[0]; break;
					case 0x05: buffer[i] = eBuffer.GetBytes( address, 1 )[0]; break;
					case 0x0D: buffer[i] = mBuffer.GetBytes( address, 1 )[0]; break;
					case 0x0C: buffer[i] = lBuffer.GetBytes( address, 1 )[0]; break;
					default: return PackCommandBack( command[0], 0x03, null );
				}
			}
			return PackCommandBack( command[0], 0x00, buffer );
		}

		private byte[] WriteRandomBoolByCommand( byte[] command )
		{
			// 先判断是否有写入的权利，没有的话，直接返回写入异常
			if (!EnableWrite) return PackCommandBack( command[0], 0x03, null );

			int length = transform.TransUInt16( command, 4 );
			if (length > 32) return PackCommandBack( command[0], 0x05, null );
			if (length * 8 - 1 != command.Length - 6) return PackCommandBack( command[0], 0x05, null );
			for (int i = 0; i < length; i++)
			{
				int address = transform.TransInt32( command, 8 + 8 * i );
				if (address > 65535 || address < 0) return PackCommandBack( command[0], 0x04, null );

				switch (command[7 + i * 8])
				{
					case 0x18: return PackCommandBack( command[0], 0x03, null );  // X type not allowed write
					case 0x19: yBuffer.SetValue( command[12 + 8 * i], address ); break;
					case 0x09: iBuffer.SetValue( command[12 + 8 * i], address ); break;
					case 0x05: eBuffer.SetValue( command[12 + 8 * i], address ); break;
					case 0x0D: mBuffer.SetValue( command[12 + 8 * i], address ); break;
					case 0x0C: lBuffer.SetValue( command[12 + 8 * i], address ); break;
					default: return PackCommandBack( command[0], 0x03, null );
				}
			}

			return PackCommandBack( command[0], 0x00, null );
		}

		private byte[] ReadWordByCommand( byte[] command )
		{
			int address = transform.TransInt32( command, 6 );
			int length = transform.TransUInt16( command, 10 );

			if (address > 65535 || address < 0)        return PackCommandBack( command[0], 0x04, null );
			if (length > 64)                           return PackCommandBack( command[0], 0x05, null );
			if (address + length > 65535)              return PackCommandBack( command[0], 0x05, null );
			switch (command[5])
			{
				case 0x18: return PackCommandBack( command[0], 0x00, xBuffer.GetBytes( address, length * 16 ).Select( m => m != 0x00 ).ToArray( ).ToByteArray( ) );
				case 0x19: return PackCommandBack( command[0], 0x00, yBuffer.GetBytes( address, length * 16 ).Select( m => m != 0x00 ).ToArray( ).ToByteArray( ) );
				case 0x09: return PackCommandBack( command[0], 0x00, iBuffer.GetBytes( address, length * 16 ).Select( m => m != 0x00 ).ToArray( ).ToByteArray( ) );
				case 0x05: return PackCommandBack( command[0], 0x00, eBuffer.GetBytes( address, length * 16 ).Select( m => m != 0x00 ).ToArray( ).ToByteArray( ) );
				case 0x0D: return PackCommandBack( command[0], 0x00, mBuffer.GetBytes( address, length * 16 ).Select( m => m != 0x00 ).ToArray( ).ToByteArray( ) );
				case 0x0C: return PackCommandBack( command[0], 0x00, lBuffer.GetBytes( address, length * 16 ).Select( m => m != 0x00 ).ToArray( ).ToByteArray( ) );

				case 0x04: return PackCommandBack( command[0], 0x00, dBuffer.GetBytes( address * 2, length * 2 ) );
				case 0x02: return PackCommandBack( command[0], 0x00, bBuffer.GetBytes( address * 2, length * 2 ) );
				case 0x06: return PackCommandBack( command[0], 0x00, fBuffer.GetBytes( address * 2, length * 2 ) );
				case 0x12: return PackCommandBack( command[0], 0x00, rBuffer.GetBytes( address * 2, length * 2 ) );
				case 0x16: return PackCommandBack( command[0], 0x00, vBuffer.GetBytes( address * 2, length * 2 ) );
				case 0x1A: return PackCommandBack( command[0], 0x00, zBuffer.GetBytes( address * 2, length * 2 ) );
				case 0x17: return PackCommandBack( command[0], 0x00, wBuffer.GetBytes( address * 2, length * 2 ) );
				default:   return PackCommandBack( command[0], 0x03, null );
			}
		}

		private byte[] WriteWordByCommand( byte[] command )
		{
			// 先判断是否有写入的权利，没有的话，直接返回写入异常
			if (!EnableWrite) return PackCommandBack( command[0], 0x03, null );

			int address = transform.TransInt32( command, 6 );
			int length = transform.TransUInt16( command, 10 );

			if (address > 65535 || address < 0)        return PackCommandBack( command[0], 0x04, null );
			if (length > 64)                           return PackCommandBack( command[0], 0x05, null );
			if (address + length > 65535)              return PackCommandBack( command[0], 0x05, null );
			if ((length * 2) != (command.Length - 12)) return PackCommandBack( command[0], 0x05, null );
			switch (command[5])
			{
				case 0x18: return PackCommandBack( command[0], 0x03, null );
				case 0x19: yBuffer.SetBytes( command.RemoveBegin( 12 ).ToBoolArray( ).Select( m => m ? (byte)0x01 : (byte)0x00 ).ToArray( ), address ); return PackCommandBack( command[0], 0x00, null );
				case 0x09: iBuffer.SetBytes( command.RemoveBegin( 12 ).ToBoolArray( ).Select( m => m ? (byte)0x01 : (byte)0x00 ).ToArray( ), address ); return PackCommandBack( command[0], 0x00, null );
				case 0x05: eBuffer.SetBytes( command.RemoveBegin( 12 ).ToBoolArray( ).Select( m => m ? (byte)0x01 : (byte)0x00 ).ToArray( ), address ); return PackCommandBack( command[0], 0x00, null );
				case 0x0D: mBuffer.SetBytes( command.RemoveBegin( 12 ).ToBoolArray( ).Select( m => m ? (byte)0x01 : (byte)0x00 ).ToArray( ), address ); return PackCommandBack( command[0], 0x00, null );
				case 0x0C: lBuffer.SetBytes( command.RemoveBegin( 12 ).ToBoolArray( ).Select( m => m ? (byte)0x01 : (byte)0x00 ).ToArray( ), address ); return PackCommandBack( command[0], 0x00, null );

				case 0x04: dBuffer.SetBytes( command.RemoveBegin( 12 ), address * 2 ); return PackCommandBack( command[0], 0x00, null );
				case 0x02: bBuffer.SetBytes( command.RemoveBegin( 12 ), address * 2 ); return PackCommandBack( command[0], 0x00, null );
				case 0x06: fBuffer.SetBytes( command.RemoveBegin( 12 ), address * 2 ); return PackCommandBack( command[0], 0x00, null );
				case 0x12: rBuffer.SetBytes( command.RemoveBegin( 12 ), address * 2 ); return PackCommandBack( command[0], 0x00, null );
				case 0x16: vBuffer.SetBytes( command.RemoveBegin( 12 ), address * 2 ); return PackCommandBack( command[0], 0x00, null );
				case 0x1A: zBuffer.SetBytes( command.RemoveBegin( 12 ), address * 2 ); return PackCommandBack( command[0], 0x00, null );
				case 0x17: wBuffer.SetBytes( command.RemoveBegin( 12 ), address * 2 ); return PackCommandBack( command[0], 0x00, null );
				default: return PackCommandBack( command[0], 0x03, null );
			}
		}

		private byte[] ReadRandomWordByCommand( byte[] command )
		{
			int length = transform.TransUInt16( command, 4 );
			if (length > 32) return PackCommandBack( command[0], 0x05, null );
			if (length * 6 != command.Length - 6) return PackCommandBack( command[0], 0x05, null );

			byte[] buffer = new byte[length * 2];
			for (int i = 0; i < length; i++)
			{
				int address = transform.TransInt32( command, 8 + 6 * i );
				if (address > 65535 || address < 0) return PackCommandBack( command[0], 0x04, null );

				switch (command[7 + i * 6])
				{
					case 0x18: xBuffer.GetBytes( address, 16 ).Select( m => m != 0x00 ).ToArray( ).ToByteArray( ).CopyTo( buffer, i * 2 ); break;
					case 0x19: yBuffer.GetBytes( address, 16 ).Select( m => m != 0x00 ).ToArray( ).ToByteArray( ).CopyTo( buffer, i * 2 ); break;
					case 0x09: iBuffer.GetBytes( address, 16 ).Select( m => m != 0x00 ).ToArray( ).ToByteArray( ).CopyTo( buffer, i * 2 ); break;
					case 0x05: eBuffer.GetBytes( address, 16 ).Select( m => m != 0x00 ).ToArray( ).ToByteArray( ).CopyTo( buffer, i * 2 ); break;
					case 0x0D: mBuffer.GetBytes( address, 16 ).Select( m => m != 0x00 ).ToArray( ).ToByteArray( ).CopyTo( buffer, i * 2 ); break;
					case 0x0C: lBuffer.GetBytes( address, 16 ).Select( m => m != 0x00 ).ToArray( ).ToByteArray( ).CopyTo( buffer, i * 2 ); break;

					case 0x04: dBuffer.GetBytes( address * 2, 2 ).CopyTo( buffer, i * 2 ); break;
					case 0x02: bBuffer.GetBytes( address * 2, 2 ).CopyTo( buffer, i * 2 ); break;
					case 0x06: fBuffer.GetBytes( address * 2, 2 ).CopyTo( buffer, i * 2 ); break;
					case 0x12: rBuffer.GetBytes( address * 2, 2 ).CopyTo( buffer, i * 2 ); break;
					case 0x16: vBuffer.GetBytes( address * 2, 2 ).CopyTo( buffer, i * 2 ); break;
					case 0x1A: zBuffer.GetBytes( address * 2, 2 ).CopyTo( buffer, i * 2 ); break;
					case 0x17: wBuffer.GetBytes( address * 2, 2 ).CopyTo( buffer, i * 2 ); break;
					default: return PackCommandBack( command[0], 0x03, null );
				}
			}

			return PackCommandBack( command[0], 0x00, buffer );
		}

		private byte[] WriteRandomWordByCommand( byte[] command )
		{
			// 先判断是否有写入的权利，没有的话，直接返回写入异常
			if (!EnableWrite) return PackCommandBack( command[0], 0x03, null );

			int length = transform.TransUInt16( command, 4 );
			if (length > 32) return PackCommandBack( command[0], 0x05, null );
			if (length * 8 != command.Length - 6) return PackCommandBack( command[0], 0x05, null );

			for (int i = 0; i < length; i++)
			{
				int address = transform.TransInt32( command, 8 + 8 * i );

				if (address > 65535 || address < 0)        return PackCommandBack( command[0], 0x04, null );
				switch (command[7 + i * 8])
				{
					case 0x18: return PackCommandBack( command[0], 0x03, null );
					case 0x19: yBuffer.SetBytes( command.SelectMiddle( 12 + 8 * i, 2).ToBoolArray( ).Select( m => m ? (byte)0x01 : (byte)0x00 ).ToArray( ), address ); break;
					case 0x09: iBuffer.SetBytes( command.SelectMiddle( 12 + 8 * i, 2).ToBoolArray( ).Select( m => m ? (byte)0x01 : (byte)0x00 ).ToArray( ), address ); break;
					case 0x05: eBuffer.SetBytes( command.SelectMiddle( 12 + 8 * i, 2).ToBoolArray( ).Select( m => m ? (byte)0x01 : (byte)0x00 ).ToArray( ), address ); break;
					case 0x0D: mBuffer.SetBytes( command.SelectMiddle( 12 + 8 * i, 2).ToBoolArray( ).Select( m => m ? (byte)0x01 : (byte)0x00 ).ToArray( ), address ); break;
					case 0x0C: lBuffer.SetBytes( command.SelectMiddle( 12 + 8 * i, 2).ToBoolArray( ).Select( m => m ? (byte)0x01 : (byte)0x00 ).ToArray( ), address ); break;

					case 0x04: dBuffer.SetBytes( command.SelectMiddle( 12 + 8 * i, 2), address * 2 ); break;
					case 0x02: bBuffer.SetBytes( command.SelectMiddle( 12 + 8 * i, 2), address * 2 ); break;
					case 0x06: fBuffer.SetBytes( command.SelectMiddle( 12 + 8 * i, 2), address * 2 ); break;
					case 0x12: rBuffer.SetBytes( command.SelectMiddle( 12 + 8 * i, 2), address * 2 ); break;
					case 0x16: vBuffer.SetBytes( command.SelectMiddle( 12 + 8 * i, 2), address * 2 ); break;
					case 0x1A: zBuffer.SetBytes( command.SelectMiddle( 12 + 8 * i, 2), address * 2 ); break;
					case 0x17: wBuffer.SetBytes( command.SelectMiddle( 12 + 8 * i, 2), address * 2 ); break;
					default: return PackCommandBack( command[0], 0x03, null );
				}
			}
			return PackCommandBack( command[0], 0x00, null );
		}

		private byte[] StartByCommand( byte[] command )
		{
			isProgramStarted = true;
			return PackCommandBack( command[0], 0x00, null ); ;
		}

		private byte[] StopByCommand( byte[] command )
		{
			isProgramStarted = false;
			return PackCommandBack( command[0], 0x00, null ); ;
		}

		private byte[] ReadSystemByCommand( byte[] command )
		{
			if (command[5] == 0x01)
			{
				byte[] buffer = new byte[2];
				buffer[1] = isProgramStarted ? (byte)0x01 : (byte)0x02;
				return PackCommandBack( command[0], 0x00, buffer );
			}
			else if(command[5] == 0x02)
			{
				byte[] buffer = new byte[28];
				Encoding.ASCII.GetBytes( "F3SP38-6N" ).CopyTo( buffer, 0 );
				Encoding.ASCII.GetBytes( "12345" ).CopyTo( buffer, 16 );
				buffer[25] = 0x11;
				buffer[26] = 0x02;
				buffer[27] = 0x03;
				return PackCommandBack( command[0], 0x00, buffer );
			}
			else
			{
				return PackCommandBack( command[0], 0x03, null );
			}
		}

		private byte[] ReadSystemDateTime( byte[] command )
		{
			byte[] buffer = new byte[16];
			DateTime now = DateTime.Now;
			buffer[ 0] = BitConverter.GetBytes( now.Year - 2000 )[1];
			buffer[ 1] = BitConverter.GetBytes( now.Year - 2000 )[0];
			buffer[ 2] = BitConverter.GetBytes( now.Month )[1];
			buffer[ 3] = BitConverter.GetBytes( now.Month )[0];
			buffer[ 4] = BitConverter.GetBytes( now.Day )[1];
			buffer[ 5] = BitConverter.GetBytes( now.Day )[0];
			buffer[ 6] = BitConverter.GetBytes( now.Hour )[1];
			buffer[ 7] = BitConverter.GetBytes( now.Hour )[0];
			buffer[ 8] = BitConverter.GetBytes( now.Minute )[1];
			buffer[ 9] = BitConverter.GetBytes( now.Minute )[0];
			buffer[10] = BitConverter.GetBytes( now.Second )[1];
			buffer[11] = BitConverter.GetBytes( now.Second )[0];
			uint unitSeconds = (uint)(now - new DateTime( now.Year, 1, 1 )).TotalSeconds;
			buffer[12] = BitConverter.GetBytes( unitSeconds )[3];
			buffer[13] = BitConverter.GetBytes( unitSeconds )[2];
			buffer[14] = BitConverter.GetBytes( unitSeconds )[1];
			buffer[15] = BitConverter.GetBytes( unitSeconds )[0];
			return PackCommandBack( command[0], 0x00, buffer );
		}

		private byte[] ReadSpecialModule( byte[] command )
		{
			if(command[4] != 0x00 || command[5]!=0x01) return PackCommandBack( command[0], 0x03, null );
			ushort address = transform.TransUInt16( command, 6 );
			ushort length = transform.TransUInt16( command, 8 );

			return PackCommandBack( command[0], 0x00, specialBuffer.GetBytes( address * 2, length * 2 ) );
		}

		private byte[] WriteSpecialModule( byte[] command )
		{
			// 先判断是否有写入的权利，没有的话，直接返回写入异常
			if (!EnableWrite) return PackCommandBack( command[0], 0x03, null );

			if (command[4] != 0x00 || command[5] != 0x01) return PackCommandBack( command[0], 0x03, null );
			ushort address = transform.TransUInt16( command, 6 );
			ushort length = transform.TransUInt16( command, 8 );
			if(length * 2 != command.Length - 10 ) return PackCommandBack( command[0], 0x05, null );

			specialBuffer.SetBytes( command.RemoveBegin( 10 ), address * 2 );
			return PackCommandBack( command[0], 0x00, null );
		}

		private byte[] PackCommandBack( byte cmd, byte err, byte[] result )
		{
			if (result == null) result = new byte[0];

			byte[] back = new byte[4 + result.Length];
			back[0] = (byte)(cmd + 0x80);
			back[1] = err;
			back[2] = BitConverter.GetBytes( result.Length )[1];
			back[3] = BitConverter.GetBytes( result.Length )[0];
			result.CopyTo( back, 4 );
			return back;
		}

		#endregion

		#region Data Save Load Override

		/// <inheritdoc/>
		protected override  void LoadFromBytes( byte[] content )
		{
			if (content.Length < DataPoolLength * 20) throw new Exception( "File is not correct" );

			xBuffer.SetBytes( content, DataPoolLength * 0, 0, DataPoolLength );
			yBuffer.SetBytes( content, DataPoolLength * 1, 0, DataPoolLength );
			iBuffer.SetBytes( content, DataPoolLength * 2, 0, DataPoolLength );
			eBuffer.SetBytes( content, DataPoolLength * 3, 0, DataPoolLength );
			mBuffer.SetBytes( content, DataPoolLength * 4, 0, DataPoolLength );
			lBuffer.SetBytes( content, DataPoolLength * 5, 0, DataPoolLength );
			dBuffer.SetBytes( content, DataPoolLength * 6, 0, DataPoolLength );
			bBuffer.SetBytes( content, DataPoolLength * 8, 0, DataPoolLength );
			fBuffer.SetBytes( content, DataPoolLength * 10, 0, DataPoolLength );
			rBuffer.SetBytes( content, DataPoolLength * 12, 0, DataPoolLength );
			vBuffer.SetBytes( content, DataPoolLength * 14, 0, DataPoolLength );
			zBuffer.SetBytes( content, DataPoolLength * 16, 0, DataPoolLength );
			wBuffer.SetBytes( content, DataPoolLength * 18, 0, DataPoolLength );
		}

		/// <inheritdoc/>
		protected override byte[] SaveToBytes( )
		{
			byte[] buffer = new byte[DataPoolLength * 20];
			Array.Copy( xBuffer.GetBytes( ), 0, buffer, DataPoolLength * 0, DataPoolLength );
			Array.Copy( yBuffer.GetBytes( ), 0, buffer, DataPoolLength * 1, DataPoolLength );
			Array.Copy( iBuffer.GetBytes( ), 0, buffer, DataPoolLength * 2, DataPoolLength );
			Array.Copy( eBuffer.GetBytes( ), 0, buffer, DataPoolLength * 3, DataPoolLength);
			Array.Copy( mBuffer.GetBytes( ), 0, buffer, DataPoolLength * 4, DataPoolLength );
			Array.Copy( lBuffer.GetBytes( ), 0, buffer, DataPoolLength * 5, DataPoolLength );
			Array.Copy( dBuffer.GetBytes( ), 0, buffer, DataPoolLength * 6, DataPoolLength );
			Array.Copy( bBuffer.GetBytes( ), 0, buffer, DataPoolLength * 8, DataPoolLength );
			Array.Copy( fBuffer.GetBytes( ), 0, buffer, DataPoolLength * 10, DataPoolLength );
			Array.Copy( rBuffer.GetBytes( ), 0, buffer, DataPoolLength * 12, DataPoolLength );
			Array.Copy( vBuffer.GetBytes( ), 0, buffer, DataPoolLength * 14, DataPoolLength );
			Array.Copy( zBuffer.GetBytes( ), 0, buffer, DataPoolLength * 16, DataPoolLength );
			Array.Copy( wBuffer.GetBytes( ), 0, buffer, DataPoolLength * 18, DataPoolLength );

			return buffer;
		}

		#endregion

		#region IDisposable Support

		/// <inheritdoc/>
		protected override void Dispose( bool disposing )
		{
			if (disposing)
			{
				xBuffer?.Dispose( );
				yBuffer?.Dispose( );
				iBuffer?.Dispose( );
				eBuffer?.Dispose( );
				mBuffer?.Dispose( );
				lBuffer?.Dispose( );
				dBuffer?.Dispose( );
				bBuffer?.Dispose( );
				fBuffer?.Dispose( );
				rBuffer?.Dispose( );
				vBuffer?.Dispose( );
				zBuffer?.Dispose( );
				wBuffer?.Dispose( );
				specialBuffer?.Dispose( );
			}
			base.Dispose( disposing );
		}

		#endregion

		#region Private Member

		private SoftBuffer xBuffer;                    // 输入继电器的数据池
		private SoftBuffer yBuffer;                    // 输出继电器的数据池
		private SoftBuffer iBuffer;                    // 内部继电器的数据池
		private SoftBuffer eBuffer;                    // 共享继电器的数据池
		private SoftBuffer mBuffer;                    // 特殊继电器的数据池
		private SoftBuffer lBuffer;                    // 链接继电器的数据池
		private SoftBuffer dBuffer;                    // 数据寄存器的数据池
		private SoftBuffer bBuffer;                    // 文件寄存器的数据池
		private SoftBuffer fBuffer;                    // 缓存寄存器的数据池
		private SoftBuffer rBuffer;                    // 共享寄存器的数据池
		private SoftBuffer vBuffer;                    // 索引寄存器的数据池
		private SoftBuffer zBuffer;                    // 特殊寄存器的数据池
		private SoftBuffer wBuffer;                    // 链接寄存器的数据池
		private SoftBuffer specialBuffer;              // 特殊寄存器的数据池
		private const int DataPoolLength = 65536;      // 数据的长度
		private IByteTransform transform;
		private bool isProgramStarted = false;         // 当前的程序是否启动

		#endregion

		#region Object Override

		/// <inheritdoc/>
		public override string ToString( ) => $"YokogawaLinkServer[{Port}]";

		#endregion
	}
}
