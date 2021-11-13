using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HslCommunication.LogNet;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.Net;
using HslCommunication.Core.IMessage;
using HslCommunication.BasicFramework;
using HslCommunication.WebSocket;
using HslCommunication.Enthernet.Redis;
using HslCommunication.MQTT;
using System.Runtime.InteropServices;
using HslCommunication.Enthernet;
using HslCommunication.Core.Security;

#if !NET35 && !NET20
using System.Threading.Tasks;
#endif

/*************************************************************************************
 * 
 *    说明：
 *    本组件的所有网络类的基类。提供了一些基础的操作实现，部分实现需要集成实现
 *    
 *    重构日期：2018年3月8日 21:22:05
 *    
 *    重构日期：2020年1月9日 新增基于全异步的实现方式
 * 
 *************************************************************************************/

namespace HslCommunication.Core.Net
{
	/// <summary>
	/// 本系统所有网络类的基类，该类为抽象类，无法进行实例化，如果想使用里面的方法来实现自定义的网络通信，请通过继承使用。<br />
	/// The base class of all network classes in this system. This class is an abstract class and cannot be instantiated. 
	/// If you want to use the methods inside to implement custom network communication, please use it through inheritance.
	/// </summary>
	/// <remarks>
	/// 本类提供了丰富的底层数据的收发支持，包含<see cref="INetMessage"/>消息的接收，<c>MQTT</c>以及<c>Redis</c>,<c>websocket</c>协议的实现
	/// </remarks>
	public abstract class NetworkBase
	{
		#region Constructor

		/// <summary>
		/// 实例化一个NetworkBase对象，令牌的默认值为空，都是0x00<br />
		/// Instantiate a NetworkBase object, the default value of the token is empty, both are 0x00
		/// </summary>
		public NetworkBase( ) { Token = Guid.Empty; Authorization.oasjodaiwfsodopsdjpasjpf( ); }

		#endregion

		#region Public Properties

		/// <summary>
		/// 组件的日志工具，支持日志记录，只要实例化后，当前网络的基本信息，就以<see cref="HslCommunication.LogNet.HslMessageDegree.DEBUG"/>等级进行输出<br />
		/// The component's logging tool supports logging. As long as the instantiation of the basic network information, the output will be output at <see cref="HslCommunication.LogNet.HslMessageDegree.DEBUG"/>
		/// </summary>
		/// <remarks>
		/// 只要实例化即可以记录日志，实例化的对象需要实现接口 <see cref="ILogNet"/> ，本组件提供了三个日志记录类，你可以实现基于 <see cref="ILogNet"/>  的对象。</remarks>
		/// <example>
		/// 如下的实例化适用于所有的Network及其派生类，以下举两个例子，三菱的设备类及服务器类
		/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Core\NetworkBase.cs" region="LogNetExample1" title="LogNet示例" />
		/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Core\NetworkBase.cs" region="LogNetExample2" title="LogNet示例" />
		/// </example>
		public ILogNet LogNet { get; set; }

		/// <summary>
		/// 网络类的身份令牌，在hsl协议的模式下会有效，在和设备进行通信的时候是无效的<br />
		/// Network-type identity tokens will be valid in the hsl protocol mode and will not be valid when communicating with the device
		/// </summary>
		/// <remarks>
		/// 适用于Hsl协议相关的网络通信类，不适用于设备交互类。
		/// </remarks>
		/// <example>
		/// 此处以 <see cref="Enthernet.NetSimplifyServer"/> 服务器类及 <see cref="Enthernet.NetSimplifyClient"/> 客户端类的令牌设置举例
		/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Core\NetworkBase.cs" region="TokenClientExample" title="Client示例" />
		/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Core\NetworkBase.cs" region="TokenServerExample" title="Server示例" />
		/// </example>
		public Guid Token { get; set; }

		#endregion

		#region Protect Member

		/// <summary>
		/// 对客户端而言是的通讯用的套接字，对服务器来说是用于侦听的套接字<br />
		/// A communication socket for the client, or a listening socket for the server
		/// </summary>
		protected Socket CoreSocket = null;

		/// <summary>
		/// 文件传输的时候的缓存大小，直接影响传输的速度，值越大，传输速度越快，越占内存，默认为100K大小<br />
		/// The size of the cache during file transfer directly affects the speed of the transfer. The larger the value, the faster the transfer speed and the more memory it takes. The default size is 100K.
		/// </summary>
		protected int fileCacheSize = 1024 * 100;

		#endregion

		/*****************************************************************************
		 * 
		 *    说明：
		 *    下面的三个模块代码指示了如何接收数据，如何发送数据，如何连接网络
		 * 
		 ********************************************************************************/

		#region Receive Content

		/// <summary>
		/// 接收固定长度的字节数组，允许指定超时时间，默认为60秒，当length大于0时，接收固定长度的数据内容，当length小于0时，buffer长度的缓存数据<br />
		/// Receiving a fixed-length byte array, allowing a specified timeout time. The default is 60 seconds. When length is greater than 0, 
		/// fixed-length data content is received. When length is less than 0, random data information of a length not greater than 2048 is received.
		/// </summary>
		/// <param name="socket">网络通讯的套接字<br />Network communication socket</param>
		/// <param name="buffer">等待接收的数据缓存信息</param>
		/// <param name="offset">开始接收数据的偏移地址</param>
		/// <param name="length">准备接收的数据长度，当length大于0时，接收固定长度的数据内容，当length小于0时，接收不大于1024长度的随机数据信息</param>
		/// <param name="timeOut">单位：毫秒，超时时间，默认为60秒，如果设置小于0，则不检查超时时间</param>
		/// <param name="reportProgress">当前接收数据的进度报告，有些协议支持传输非常大的数据内容，可以给与进度提示的功能</param>
		/// <returns>包含了字节数据的结果类</returns>
		protected OperateResult<int> Receive( Socket socket, byte[] buffer, int offset, int length, int timeOut = 60_000, Action<long, long> reportProgress = null )
		{
			if (length == 0) return OperateResult.CreateSuccessResult( 0 );
			if (!Authorization.nzugaydgwadawdibbas( )) return new OperateResult<int>( StringResources.Language.AuthorizationFailed );

			try
			{
				socket.ReceiveTimeout = timeOut;
				if (length > 0)
				{
					NetSupport.ReceiveBytesFromSocket( socket, buffer, offset, length, reportProgress );
					return OperateResult.CreateSuccessResult( length );
				}
				else
				{
					int count = socket.Receive( buffer, offset, buffer.Length - offset, SocketFlags.None );
					if (count == 0) throw new RemoteCloseException( );
					return OperateResult.CreateSuccessResult( count );
				}
			}
			catch (RemoteCloseException)
			{
				socket?.Close( );
				if (connectErrorCount < 10_0000_0000) connectErrorCount++;
				return new OperateResult<int>( -connectErrorCount, $"Socket Exception -> {StringResources.Language.RemoteClosedConnection}" );
			}
			catch (Exception ex)
			{
				socket?.Close( );
				if (connectErrorCount < 10_0000_0000) connectErrorCount++;
				return new OperateResult<int>( -connectErrorCount, $"Socket Exception -> {ex.Message}" );
			}
		}

		/// <summary>
		/// 接收固定长度的字节数组，允许指定超时时间，默认为60秒，当length大于0时，接收固定长度的数据内容，当length小于0时，接收不大于2048长度的随机数据信息<br />
		/// Receiving a fixed-length byte array, allowing a specified timeout time. The default is 60 seconds. When length is greater than 0, 
		/// fixed-length data content is received. When length is less than 0, random data information of a length not greater than 2048 is received.
		/// </summary>
		/// <param name="socket">网络通讯的套接字<br />Network communication socket</param>
		/// <param name="length">准备接收的数据长度，当length大于0时，接收固定长度的数据内容，当length小于0时，接收不大于1024长度的随机数据信息</param>
		/// <param name="timeOut">单位：毫秒，超时时间，默认为60秒，如果设置小于0，则不检查超时时间</param>
		/// <param name="reportProgress">当前接收数据的进度报告，有些协议支持传输非常大的数据内容，可以给与进度提示的功能</param>
		/// <returns>包含了字节数据的结果类</returns>
		protected OperateResult<byte[]> Receive( Socket socket, int length, int timeOut = 60_000, Action<long, long> reportProgress = null )
		{
			if (length == 0) return OperateResult.CreateSuccessResult( new byte[0] );
			if (!Authorization.nzugaydgwadawdibbas( )) return new OperateResult<byte[]>( StringResources.Language.AuthorizationFailed );

			byte[] buffer = length > 0 ? new byte[length] : new byte[2048];
			OperateResult<int> receive = Receive( socket, buffer, 0, length, timeOut, reportProgress );
			if (!receive.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( receive );

			return OperateResult.CreateSuccessResult( length > 0 ? buffer : buffer.SelectBegin( receive.Content ) );
		}

		#endregion

		#region Receive CommandLine

		/// <summary>
		/// 接收一行命令数据，需要自己指定这个结束符，默认超时时间为60秒，也即是60000，单位是毫秒<br />
		/// To receive a line of command data, you need to specify the terminator yourself. The default timeout is 60 seconds, which is 60,000, in milliseconds.
		/// </summary>
		/// <param name="socket">网络套接字</param>
		/// <param name="endCode">结束符信息</param>
		/// <param name="timeout">超时时间，默认为60000，单位为毫秒，也就是60秒</param>
		/// <returns>带有结果对象的数据信息</returns>
		protected OperateResult<byte[]> ReceiveCommandLineFromSocket( Socket socket, byte endCode, int timeout = 60_000 )
		{
			List<byte> bufferArray = new List<byte>( 128 );
			try
			{
				DateTime st = DateTime.Now;
				bool bOK = false;

				// 接收到endCode为止，此处的超时是针对是否接收到endCode为止的
				while ((DateTime.Now - st).TotalMilliseconds < timeout)
				{
					if (socket.Poll( timeout, SelectMode.SelectRead ))
					{
						OperateResult<byte[]> headResult = Receive( socket, 1, timeout );
						if (!headResult.IsSuccess) return headResult;

						bufferArray.AddRange( headResult.Content );
						if (headResult.Content[0] == endCode)
						{
							bOK = true;
							break;
						}
					}
				}

				if (!bOK) return new OperateResult<byte[]>( StringResources.Language.ReceiveDataTimeout );

				// 指令头已经接收完成
				return OperateResult.CreateSuccessResult( bufferArray.ToArray( ) );
			}
			catch (Exception ex)
			{
				socket?.Close( );
				return new OperateResult<byte[]>( ex.Message );
			}
		}

		/// <summary>
		/// 接收一行命令数据，需要自己指定这个结束符，默认超时时间为60秒，也即是60000，单位是毫秒<br />
		/// To receive a line of command data, you need to specify the terminator yourself. The default timeout is 60 seconds, which is 60,000, in milliseconds.
		/// </summary>
		/// <param name="socket">网络套接字</param>
		/// <param name="endCode1">结束符1信息</param>
		/// <param name="endCode2">结束符2信息</param>
		/// /// <param name="timeout">超时时间，默认无穷大，单位毫秒</param>
		/// <returns>带有结果对象的数据信息</returns>
		protected OperateResult<byte[]> ReceiveCommandLineFromSocket( Socket socket, byte endCode1, byte endCode2, int timeout = 60_000 )
		{
			List<byte> bufferArray = new List<byte>( 128 );
			try
			{
				DateTime st = DateTime.Now;
				bool bOK = false;
				// 接收到endCode为止
				while ((DateTime.Now - st).TotalMilliseconds < timeout)
				{
					if (socket.Poll( timeout, SelectMode.SelectRead ))
					{
						OperateResult<byte[]> headResult = Receive( socket, 1, timeout );
						if (!headResult.IsSuccess) return headResult;

						bufferArray.AddRange( headResult.Content );
						if (headResult.Content[0] == endCode2)
						{
							if (bufferArray.Count > 1 && bufferArray[bufferArray.Count - 2] == endCode1)
							{
								bOK = true;
								break;
							}
						}
					}
				}

				if (!bOK) return new OperateResult<byte[]>( StringResources.Language.ReceiveDataTimeout );

				// 指令头已经接收完成
				return OperateResult.CreateSuccessResult( bufferArray.ToArray( ) );
			}
			catch (Exception ex)
			{
				socket?.Close( );
				return new OperateResult<byte[]>( ex.Message );
			}
		}

		#endregion

		#region Receive Message

		/// <summary>
		/// 接收一条完整的 <seealso cref="INetMessage"/> 数据内容，需要指定超时时间，单位为毫秒。 <br />
		/// Receive a complete <seealso cref="INetMessage"/> data content, Need to specify a timeout period in milliseconds
		/// </summary>
		/// <param name="socket">网络的套接字</param>
		/// <param name="timeOut">超时时间，单位：毫秒</param>
		/// <param name="netMessage">消息的格式定义</param>
		/// <param name="reportProgress">接收消息的时候的进度报告</param>
		/// <returns>带有是否成功的byte数组对象</returns>
		protected virtual OperateResult<byte[]> ReceiveByMessage( Socket socket, int timeOut, INetMessage netMessage, Action<long, long> reportProgress = null )
		{
			if (netMessage == null) return Receive( socket, -1, timeOut );

			// 接收指令头
			OperateResult<byte[]> headResult = Receive( socket, netMessage.ProtocolHeadBytesLength, timeOut );
			if (!headResult.IsSuccess) return headResult;

			netMessage.HeadBytes = headResult.Content;
			int contentLength = netMessage.GetContentLengthByHeadBytes( );
			if (contentLength <= 0) return OperateResult.CreateSuccessResult( headResult.Content );

			byte[] result = new byte[netMessage.ProtocolHeadBytesLength + contentLength];
			headResult.Content.CopyTo( result, 0 );

			OperateResult contentResult = Receive( socket, result, netMessage.ProtocolHeadBytesLength, contentLength, timeOut, reportProgress );
			if (!contentResult.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( contentResult );

			return OperateResult.CreateSuccessResult( result );
		}

		#endregion

		#region Send Content

		/// <summary>
		/// 发送消息给套接字，直到完成的时候返回，经过测试，本方法是线程安全的。<br />
		/// Send a message to the socket until it returns when completed. After testing, this method is thread-safe.
		/// </summary>
		/// <param name="socket">网络套接字</param>
		/// <param name="data">字节数据</param>
		/// <returns>发送是否成功的结果</returns>
		protected OperateResult Send( Socket socket, byte[] data )
		{
			if (data == null) return OperateResult.CreateSuccessResult( );
			return Send( socket, data, 0, data.Length );
		}

		/// <summary>
		/// 发送消息给套接字，直到完成的时候返回，经过测试，本方法是线程安全的。<br />
		/// Send a message to the socket until it returns when completed. After testing, this method is thread-safe.
		/// </summary>
		/// <param name="socket">网络套接字</param>
		/// <param name="data">字节数据</param>
		/// <param name="offset">偏移的位置信息</param>
		/// <param name="size">发送的数据总数</param>
		/// <returns>发送是否成功的结果</returns>
		protected OperateResult Send( Socket socket, byte[] data, int offset, int size )
		{
			if (data == null) return OperateResult.CreateSuccessResult( );

			if (!Authorization.nzugaydgwadawdibbas( )) return new OperateResult<byte[]>( StringResources.Language.AuthorizationFailed );

			try
			{
				int sendCount = 0;
				while (true)
				{
					int count = socket.Send( data, offset, size - sendCount, SocketFlags.None );
					sendCount += count;
					offset += count;

					if (sendCount >= size) break;
				}
				return OperateResult.CreateSuccessResult( );
			}
			catch (Exception ex)
			{
				socket?.Close( );
				if (connectErrorCount < 10_0000_0000) connectErrorCount++;
				return new OperateResult<byte[]>( -connectErrorCount, ex.Message );
			}
		}

		#endregion

		#region Socket Connect

		/// <summary>
		/// 创建一个新的socket对象并连接到远程的地址，默认超时时间为10秒钟，需要指定ip地址以及端口号信息<br />
		/// Create a new socket object and connect to the remote address. The default timeout is 10 seconds. You need to specify the IP address and port number.
		/// </summary>
		/// <param name="ipAddress">Ip地址</param>
		/// <param name="port">端口号</param>
		/// <returns>返回套接字的封装结果对象</returns>
		/// <example>
		/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Core\NetworkBase.cs" region="CreateSocketAndConnectExample" title="创建连接示例" />
		/// </example>
		protected OperateResult<Socket> CreateSocketAndConnect( string ipAddress, int port ) => CreateSocketAndConnect( new IPEndPoint( IPAddress.Parse( ipAddress ), port ), 10000 );

		/// <summary>
		/// 创建一个新的socket对象并连接到远程的地址，需要指定ip地址以及端口号信息，还有超时时间，单位是毫秒<br />
		/// To create a new socket object and connect to a remote address, you need to specify the IP address and port number information, and the timeout period in milliseconds
		/// </summary>
		/// <param name="ipAddress">Ip地址</param>
		/// <param name="port">端口号</param>
		/// <param name="timeOut">连接的超时时间</param>
		/// <returns>返回套接字的封装结果对象</returns>
		/// <example>
		/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Core\NetworkBase.cs" region="CreateSocketAndConnectExample" title="创建连接示例" />
		/// </example>
		protected OperateResult<Socket> CreateSocketAndConnect( string ipAddress, int port, int timeOut ) => CreateSocketAndConnect( new IPEndPoint( IPAddress.Parse( ipAddress ), port ), timeOut );

		/// <summary>
		/// 创建一个新的socket对象并连接到远程的地址，需要指定远程终结点，超时时间（单位是毫秒），如果需要绑定本地的IP或是端口，传入 local对象<br />
		/// To create a new socket object and connect to the remote address, you need to specify the remote endpoint, 
		/// the timeout period (in milliseconds), if you need to bind the local IP or port, pass in the local object
		/// </summary>
		/// <param name="endPoint">连接的目标终结点</param>
		/// <param name="timeOut">连接的超时时间</param>
		/// <param name="local">如果需要绑定本地的IP地址，就需要设置当前的对象</param>
		/// <returns>返回套接字的封装结果对象</returns>
		/// <example>
		/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Core\NetworkBase.cs" region="CreateSocketAndConnectExample" title="创建连接示例" />
		/// </example>
		protected OperateResult<Socket> CreateSocketAndConnect( IPEndPoint endPoint, int timeOut, IPEndPoint local = null )
		{
			int connectCount = 0;
			while (true)
			{
				connectCount++;
				var socket = new Socket( AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp );
				HslTimeOut connectTimeout = HslTimeOut.HandleTimeOutCheck( socket, timeOut );
				try
				{
					if (local != null) socket.Bind( local );
					socket.Connect( endPoint );
					connectErrorCount = 0;
					connectTimeout.IsSuccessful = true;
					return OperateResult.CreateSuccessResult( socket );
				}
				catch (Exception ex)
				{
					// 如果连接一次出现了立即失败的情况，那就马上立即重试一次连接
					// If the connection fails immediately, try to retry the connection immediately
					socket?.Close( );
					connectTimeout.IsSuccessful = true;
					if (connectErrorCount < 10_0000_0000) connectErrorCount++;
					if (connectTimeout.GetConsumeTime( ) < TimeSpan.FromMilliseconds( 500 ) && connectCount < 2) { Thread.Sleep( 100 ); continue; }

					if (connectTimeout.IsTimeout)
					{
						return new OperateResult<Socket>( -connectErrorCount, string.Format( StringResources.Language.ConnectTimeout, endPoint, timeOut ) + " ms" );
					}
					else
					{
						return new OperateResult<Socket>( -connectErrorCount, $"Socket Connect {endPoint} Exception -> " + ex.Message );
					}
				}
			}
		}

		#endregion

		/*****************************************************************************
		 * 
		 *    说明：
		 *    下面的两个模块代码指示了如何读写文件
		 * 
		 ********************************************************************************/

		#region Read Write Stream

		/// <summary>
		/// 读取流中的数据到缓存区，读取的长度需要按照实际的情况来判断<br />
		/// Read the data in the stream to the buffer area. The length of the read needs to be determined according to the actual situation.
		/// </summary>
		/// <param name="stream">数据流</param>
		/// <param name="buffer">缓冲区</param>
		/// <returns>带有成功标志的读取数据长度</returns>
		protected OperateResult<int> ReadStream( Stream stream, byte[] buffer )
		{
			ManualResetEvent WaitDone = new ManualResetEvent( false );
			FileStateObject stateObject = new FileStateObject
			{
				WaitDone = WaitDone,
				Stream = stream,
				DataLength = buffer.Length,
				Buffer = buffer
			};

			try
			{
				stream.BeginRead( buffer, 0, stateObject.DataLength, new AsyncCallback( ReadStreamCallBack ), stateObject );
			}
			catch (Exception ex)
			{
				stateObject = null;
				WaitDone.Close( );
				return new OperateResult<int>( $"stream.BeginRead Exception -> {ex.Message}" );
			}

			WaitDone.WaitOne( );
			WaitDone.Close( );
			return stateObject.IsError ? new OperateResult<int>( stateObject.ErrerMsg ) : OperateResult.CreateSuccessResult( stateObject.AlreadyDealLength );
		}

		private void ReadStreamCallBack( IAsyncResult ar )
		{
			if (ar.AsyncState is FileStateObject stateObject)
			{
				try
				{
					stateObject.AlreadyDealLength += stateObject.Stream.EndRead( ar );
					stateObject.WaitDone.Set( );
				}
				catch (Exception ex)
				{
					stateObject.IsError = true;
					stateObject.ErrerMsg = ex.Message;
					stateObject.WaitDone.Set( );
				}
			}
		}

		/// <summary>
		/// 将缓冲区的数据写入到流里面去<br />
		/// Write the buffer data to the stream
		/// </summary>
		/// <param name="stream">数据流</param>
		/// <param name="buffer">缓冲区</param>
		/// <returns>是否写入成功</returns>
		protected OperateResult WriteStream( Stream stream, byte[] buffer )
		{
			ManualResetEvent WaitDone = new ManualResetEvent( false );
			FileStateObject stateObject = new FileStateObject
			{
				WaitDone = WaitDone,
				Stream = stream
			};

			try
			{
				stream.BeginWrite( buffer, 0, buffer.Length, new AsyncCallback( WriteStreamCallBack ), stateObject );
			}
			catch (Exception ex)
			{
				stateObject = null;
				WaitDone.Close( );
				return new OperateResult( $"stream.BeginWrite Exception -> {ex.Message}" );
			}

			WaitDone.WaitOne( );
			WaitDone.Close( );
			if (stateObject.IsError)
			{
				return new OperateResult( )
				{
					Message = stateObject.ErrerMsg
				};
			}
			else
			{
				return OperateResult.CreateSuccessResult( );
			}
		}

		private void WriteStreamCallBack( IAsyncResult ar )
		{
			if (ar.AsyncState is FileStateObject stateObject)
			{
				try
				{
					stateObject.Stream.EndWrite( ar );
				}
				catch (Exception ex)
				{
					stateObject.IsError = true;
					stateObject.ErrerMsg = ex.Message;
				}
				finally
				{
					stateObject.WaitDone.Set( );
				}
			}
		}

		#endregion

		#region Token Check

		/// <summary>
		/// 检查当前的头子节信息的令牌是否是正确的，仅用于某些特殊的协议实现<br />
		/// Check whether the token of the current header subsection information is correct, only for some special protocol implementations
		/// </summary>
		/// <param name="headBytes">头子节数据</param>
		/// <returns>令牌是验证成功</returns>
		protected bool CheckRemoteToken( byte[] headBytes ) => SoftBasic.IsByteTokenEquel( headBytes, Token );

		#endregion

		#region Special Bytes Send

		/// <summary>
		/// [自校验] 发送字节数据并确认对方接收完成数据，如果结果异常，则结束通讯<br />
		/// [Self-check] Send the byte data and confirm that the other party has received the completed data. If the result is abnormal, the communication ends.
		/// </summary>
		/// <param name="socket">网络套接字</param>
		/// <param name="headCode">头指令</param>
		/// <param name="customer">用户指令</param>
		/// <param name="send">发送的数据</param>
		/// <returns>是否发送成功</returns>
		protected OperateResult SendBaseAndCheckReceive( Socket socket, int headCode, int customer, byte[] send )
		{
			// 数据处理
			send = HslProtocol.CommandBytes( headCode, customer, Token, send );

			// 发送数据
			OperateResult sendResult = Send( socket, send );
			if (!sendResult.IsSuccess) return sendResult;

			// 检查对方接收完成
			OperateResult<long> checkResult = ReceiveLong( socket );
			if (!checkResult.IsSuccess) return checkResult;

			// 检查长度接收
			if (checkResult.Content != send.Length)
			{
				socket?.Close( );
				return new OperateResult( StringResources.Language.CommandLengthCheckFailed );
			}

			return checkResult;
		}

		/// <summary>
		/// [自校验] 发送字节数据并确认对方接收完成数据，如果结果异常，则结束通讯<br />
		/// [Self-check] Send the byte data and confirm that the other party has received the completed data. If the result is abnormal, the communication ends.
		/// </summary>
		/// <param name="socket">网络套接字</param>
		/// <param name="customer">用户指令</param>
		/// <param name="send">发送的数据</param>
		/// <returns>是否发送成功</returns>
		protected OperateResult SendBytesAndCheckReceive( Socket socket, int customer, byte[] send ) => SendBaseAndCheckReceive( socket, HslProtocol.ProtocolUserBytes, customer, send );

		/// <summary>
		/// [自校验] 直接发送字符串数据并确认对方接收完成数据，如果结果异常，则结束通讯<br />
		/// [Self-checking] Send string data directly and confirm that the other party has received the completed data. If the result is abnormal, the communication ends.
		/// </summary>
		/// <param name="socket">网络套接字</param>
		/// <param name="customer">用户指令</param>
		/// <param name="send">发送的数据</param>
		/// <returns>是否发送成功</returns>
		protected OperateResult SendStringAndCheckReceive( Socket socket, int customer, string send )
		{
			byte[] data = string.IsNullOrEmpty( send ) ? null : Encoding.Unicode.GetBytes( send );

			return SendBaseAndCheckReceive( socket, HslProtocol.ProtocolUserString, customer, data );
		}

		/// <summary>
		/// [自校验] 直接发送字符串数组并确认对方接收完成数据，如果结果异常，则结束通讯<br />
		/// [Self-check] Send string array directly and confirm that the other party has received the completed data. If the result is abnormal, the communication ends.
		/// </summary>
		/// <param name="socket">网络套接字</param>
		/// <param name="customer">用户指令</param>
		/// <param name="sends">发送的字符串数组</param>
		/// <returns>是否发送成功</returns>
		protected OperateResult SendStringAndCheckReceive( Socket socket, int customer, string[] sends )
		{
			return SendBaseAndCheckReceive( socket, HslProtocol.ProtocolUserStringArray, customer, HslProtocol.PackStringArrayToByte( sends ) );
		}

		/// <summary>
		/// [自校验] 直接发送字符串数组并确认对方接收完成数据，如果结果异常，则结束通讯<br />
		/// [Self-check] Send string array directly and confirm that the other party has received the completed data. If the result is abnormal, the communication ends.
		/// </summary>
		/// <param name="socket">网络套接字</param>
		/// <param name="customer">用户指令</param>
		/// <param name="name">用户名</param>
		/// <param name="pwd">密码</param>
		/// <returns>是否发送成功</returns>
		protected OperateResult SendAccountAndCheckReceive( Socket socket, int customer, string name, string pwd )
		{
			return SendBaseAndCheckReceive( socket, HslProtocol.ProtocolAccountLogin, customer, HslProtocol.PackStringArrayToByte( new string[] { name, pwd } ) );
		}

		/// <summary>
		/// [自校验] 接收一条完整的同步数据，包含头子节和内容字节，基础的数据，如果结果异常，则结束通讯<br />
		/// [Self-checking] Receive a complete synchronization data, including header subsection and content bytes, basic data, if the result is abnormal, the communication ends
		/// </summary>
		/// <param name="socket">套接字</param>
		/// <param name="timeOut">超时时间设置，如果为负数，则不检查超时</param>
		/// <returns>包含是否成功的结果对象</returns>
		/// <exception cref="ArgumentNullException">result</exception>
		protected OperateResult<byte[], byte[]> ReceiveAndCheckBytes( Socket socket, int timeOut )
		{
			// 接收头指令
			OperateResult<byte[]> headResult = Receive( socket, HslProtocol.HeadByteLength, timeOut );
			if (!headResult.IsSuccess) return headResult.ConvertFailed<byte[], byte[]>( );

			// 检查令牌
			if (!CheckRemoteToken( headResult.Content ))
			{
				socket?.Close( );
				return new OperateResult<byte[], byte[]>( StringResources.Language.TokenCheckFailed );
			}

			int contentLength = BitConverter.ToInt32( headResult.Content, HslProtocol.HeadByteLength - 4 );
			// 接收内容
			OperateResult<byte[]> contentResult = Receive( socket, contentLength, timeOut );
			if (!contentResult.IsSuccess) return contentResult.ConvertFailed<byte[], byte[]>( );

			// 返回成功信息
			OperateResult checkResult = SendLong( socket, HslProtocol.HeadByteLength + contentLength );
			if (!checkResult.IsSuccess) return checkResult.ConvertFailed<byte[], byte[]>( );

			byte[] head = headResult.Content;
			byte[] content = contentResult.Content;
			content = HslProtocol.CommandAnalysis( head, content );
			return OperateResult.CreateSuccessResult( head, content );
		}

		/// <summary>
		/// [自校验] 从网络中接收一个字符串数据，如果结果异常，则结束通讯<br />
		/// [Self-checking] Receive a string of data from the network. If the result is abnormal, the communication ends.
		/// </summary>
		/// <param name="socket">套接字</param>
		/// <param name="timeOut">接收数据的超时时间</param>
		/// <returns>包含是否成功的结果对象</returns>
		protected OperateResult<int, string> ReceiveStringContentFromSocket( Socket socket, int timeOut = 30_000 )
		{
			OperateResult<byte[], byte[]> receive = ReceiveAndCheckBytes( socket, timeOut );
			if (!receive.IsSuccess) return OperateResult.CreateFailedResult<int, string>( receive );

			// 检查是否是字符串信息
			if (BitConverter.ToInt32( receive.Content1, 0 ) != HslProtocol.ProtocolUserString)
			{
				socket?.Close( );
				return new OperateResult<int, string>( StringResources.Language.CommandHeadCodeCheckFailed );
			}

			if (receive.Content2 == null) receive.Content2 = new byte[0];
			// 分析数据
			return OperateResult.CreateSuccessResult( BitConverter.ToInt32( receive.Content1, 4 ), Encoding.Unicode.GetString( receive.Content2 ) );
		}

		/// <summary>
		/// [自校验] 从网络中接收一个字符串数组，如果结果异常，则结束通讯<br />
		/// [Self-check] Receive an array of strings from the network. If the result is abnormal, the communication ends.
		/// </summary>
		/// <param name="socket">套接字</param>
		/// <param name="timeOut">接收数据的超时时间</param>
		/// <returns>包含是否成功的结果对象</returns>
		protected OperateResult<int, string[]> ReceiveStringArrayContentFromSocket( Socket socket, int timeOut = 30_000 )
		{
			OperateResult<byte[], byte[]> receive = ReceiveAndCheckBytes( socket, timeOut );
			if (!receive.IsSuccess) return OperateResult.CreateFailedResult<int, string[]>( receive );

			// 检查是否是字符串信息
			if (BitConverter.ToInt32( receive.Content1, 0 ) != HslProtocol.ProtocolUserStringArray)
			{
				socket?.Close( );
				return new OperateResult<int, string[]>( StringResources.Language.CommandHeadCodeCheckFailed );
			}

			if (receive.Content2 == null) receive.Content2 = new byte[4];
			return OperateResult.CreateSuccessResult( BitConverter.ToInt32( receive.Content1, 4 ), HslProtocol.UnPackStringArrayFromByte( receive.Content2 ) );
		}

		/// <summary>
		/// [自校验] 从网络中接收一串字节数据，如果结果异常，则结束通讯<br />
		/// [Self-checking] Receive a string of byte data from the network. If the result is abnormal, the communication ends.
		/// </summary>
		/// <param name="socket">套接字的网络</param>
		/// <param name="timeout">超时时间</param>
		/// <returns>包含是否成功的结果对象</returns>
		protected OperateResult<int, byte[]> ReceiveBytesContentFromSocket( Socket socket, int timeout = 30_000 )
		{
			OperateResult<byte[], byte[]> receive = ReceiveAndCheckBytes( socket, timeout );
			if (!receive.IsSuccess) return OperateResult.CreateFailedResult<int, byte[]>( receive );

			// 检查是否是字节信息
			if (BitConverter.ToInt32( receive.Content1, 0 ) != HslProtocol.ProtocolUserBytes)
			{
				socket?.Close( );
				return new OperateResult<int, byte[]>( StringResources.Language.CommandHeadCodeCheckFailed );
			}

			// 分析数据
			return OperateResult.CreateSuccessResult( BitConverter.ToInt32( receive.Content1, 4 ), receive.Content2 );
		}

		/// <summary>
		/// 从网络中接收Long数据<br />
		/// Receive Long data from the network
		/// </summary>
		/// <param name="socket">套接字网络</param>
		/// <returns>long数据结果</returns>
		private OperateResult<long> ReceiveLong( Socket socket )
		{
			OperateResult<byte[]> read = Receive( socket, 8, -1 );
			if (read.IsSuccess)
				return OperateResult.CreateSuccessResult( BitConverter.ToInt64( read.Content, 0 ) );
			else
				return OperateResult.CreateFailedResult<long>( read );
		}

		/// <summary>
		/// 将long数据发送到套接字<br />
		/// Send long data to the socket
		/// </summary>
		/// <param name="socket">网络套接字</param>
		/// <param name="value">long数据</param>
		/// <returns>是否发送成功</returns>
		private OperateResult SendLong( Socket socket, long value ) => Send( socket, BitConverter.GetBytes( value ) );

		#endregion

		#region Stream Socket Write Read

		/// <summary>
		/// 发送一个流的所有数据到指定的网络套接字，需要指定发送的数据长度，支持按照百分比的进度报告<br />
		/// Send all the data of a stream to the specified network socket. You need to specify the length of the data to be sent. It supports the progress report in percentage.
		/// </summary>
		/// <param name="socket">套接字</param>
		/// <param name="stream">内存流</param>
		/// <param name="receive">发送的数据长度</param>
		/// <param name="report">进度报告的委托</param>
		/// <param name="reportByPercent">进度报告是否按照百分比报告</param>
		/// <returns>是否成功的结果对象</returns>
		protected OperateResult SendStreamToSocket( Socket socket, Stream stream, long receive, Action<long, long> report, bool reportByPercent )
		{
			byte[] buffer = new byte[fileCacheSize]; // 100K的数据缓存池
			long SendTotal = 0;
			long percent = 0;
			stream.Position = 0;
			while (SendTotal < receive)
			{
				// 先从流中接收数据
				OperateResult<int> read = ReadStream( stream, buffer );
				if (!read.IsSuccess)
				{
					socket?.Close( );
					return read;
				}

				SendTotal += read.Content;
				// 然后再异步写到socket中
				byte[] newBuffer = new byte[read.Content];
				Array.Copy( buffer, 0, newBuffer, 0, newBuffer.Length );
				OperateResult write = SendBytesAndCheckReceive( socket, read.Content, newBuffer );
				if (!write.IsSuccess)
				{
					socket?.Close( );
					return write;
				}
				// 报告进度
				if (reportByPercent)
				{
					long percentCurrent = SendTotal * 100 / receive;
					if (percent != percentCurrent)
					{
						percent = percentCurrent;
						report?.Invoke( SendTotal, receive );
					}
				}
				else
				{
					// 报告进度
					report?.Invoke( SendTotal, receive );
				}
			}

			return OperateResult.CreateSuccessResult( );
		}

		/// <summary>
		/// 从套接字中接收所有的数据然后写入到指定的流当中去，需要指定数据的长度，支持按照百分比进行进度报告<br />
		/// Receives all data from the socket and writes it to the specified stream. The length of the data needs to be specified, and progress reporting is supported in percentage.
		/// </summary>
		/// <param name="socket">套接字</param>
		/// <param name="stream">数据流</param>
		/// <param name="totalLength">所有数据的长度</param>
		/// <param name="report">进度报告</param>
		/// <param name="reportByPercent">进度报告是否按照百分比</param>
		/// <returns>是否成功的结果对象</returns>
		protected OperateResult WriteStreamFromSocket( Socket socket, Stream stream, long totalLength, Action<long, long> report, bool reportByPercent )
		{
			long count_receive = 0;
			long percent = 0;
			while (count_receive < totalLength)
			{
				// 先从流中异步接收数据
				OperateResult<int, byte[]> read = ReceiveBytesContentFromSocket( socket, 60_000 );
				if (!read.IsSuccess) return read;

				count_receive += read.Content1;
				// 开始写入文件流
				OperateResult write = WriteStream( stream, read.Content2 );
				if (!write.IsSuccess)
				{
					socket?.Close( );
					return write;
				}

				// 报告进度
				if (reportByPercent)
				{
					long percentCurrent = count_receive * 100 / totalLength;
					if (percent != percentCurrent)
					{
						percent = percentCurrent;
						report?.Invoke( count_receive, totalLength );
					}
				}
				else
				{
					report?.Invoke( count_receive, totalLength );
				}
			}
			return OperateResult.CreateSuccessResult( );
		}

		#endregion


		/*********************************************************************************
		 * 
		 * 异步方法的实现区域
		 * 
		 * 
		 **********************************************************************************/

		#region Async Await Support
#if !NET35 && !NET20
		/// <inheritdoc cref="CreateSocketAndConnect(IPEndPoint, int, IPEndPoint)"/>
		protected async Task<OperateResult<Socket>> CreateSocketAndConnectAsync( IPEndPoint endPoint, int timeOut, IPEndPoint local = null )
		{
			int connectCount = 0;
			while (true)
			{
				connectCount++;
				var socket = new Socket( AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp );
				HslTimeOut connectTimeout = HslTimeOut.HandleTimeOutCheck( socket, timeOut );

				try
				{
					if (local != null) socket.Bind( local );
					await Task.Factory.FromAsync( socket.BeginConnect( endPoint, null, socket ), socket.EndConnect );
					connectErrorCount = 0;
					connectTimeout.IsSuccessful = true;
					return OperateResult.CreateSuccessResult( socket );
				}
				catch (Exception ex)
				{
					connectTimeout.IsSuccessful = true;
					socket?.Close( );
					if (connectErrorCount < 10_0000_0000) connectErrorCount++;

					// 如果连接一次出现了立即失败的情况，那就马上立即重试一次连接
					// If the connection fails immediately, try to retry the connection immediately
					if (connectTimeout.GetConsumeTime( ) < TimeSpan.FromMilliseconds( 500 ) && connectCount < 2) { await Task.Delay( 100 ); continue; }

					if (connectTimeout.IsTimeout)
					{
						return new OperateResult<Socket>( -connectErrorCount, string.Format( StringResources.Language.ConnectTimeout, endPoint, timeOut ) + " ms" );
					}
					else
					{
						return new OperateResult<Socket>( -connectErrorCount, $"Socket Exception -> {ex.Message}" );
					}
				}
			}
		}

		/// <inheritdoc cref="CreateSocketAndConnect(string, int)"/>
		protected async Task<OperateResult<Socket>> CreateSocketAndConnectAsync( string ipAddress, int port ) => await CreateSocketAndConnectAsync( new IPEndPoint( IPAddress.Parse( ipAddress ), port ), 10000 );

		/// <inheritdoc cref="CreateSocketAndConnect(string, int, int)"/>
		protected async Task<OperateResult<Socket>> CreateSocketAndConnectAsync( string ipAddress, int port, int timeOut ) => await CreateSocketAndConnectAsync( new IPEndPoint( IPAddress.Parse( ipAddress ), port ), timeOut );

		/// <inheritdoc cref="Receive(Socket, int, int, Action{long, long})"/>
		protected async Task<OperateResult<byte[]>> ReceiveAsync( Socket socket, int length, int timeOut = 60_000, Action<long, long> reportProgress = null )
		{
			if (length == 0) return OperateResult.CreateSuccessResult( new byte[0] );
			if (!Authorization.nzugaydgwadawdibbas( )) new OperateResult<byte[]>( StringResources.Language.AuthorizationFailed );

			byte[] buffer = length > 0 ? new byte[length] : new byte[2048];
			OperateResult<int> receive = await ReceiveAsync( socket, buffer, 0, length, timeOut, reportProgress );
			if (!receive.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( receive );

			return OperateResult.CreateSuccessResult( length > 0 ? buffer : buffer.SelectBegin( receive.Content ) );
		}

		/// <inheritdoc cref="Receive(Socket, byte[], int, int, int, Action{long, long})"/>
		protected async Task<OperateResult<int>> ReceiveAsync( Socket socket, byte[] buffer, int offset, int length, int timeOut = 60_000, Action<long, long> reportProgress = null )
		{
			if (length == 0) return OperateResult.CreateSuccessResult( length );
			if (!Authorization.nzugaydgwadawdibbas( )) new OperateResult<int>( StringResources.Language.AuthorizationFailed );

			HslTimeOut hslTimeOut = HslTimeOut.HandleTimeOutCheck( socket, timeOut );

			try
			{
				if (length > 0)
				{
					int alreadyCount = 0;
					while (true)
					{
						int currentReceiveLength = (length - alreadyCount) > NetSupport.SocketBufferSize ? NetSupport.SocketBufferSize : (length - alreadyCount);
						int count = await Task.Factory.FromAsync( socket.BeginReceive( buffer, alreadyCount + offset, currentReceiveLength, SocketFlags.None, null, socket ), socket.EndReceive );
						alreadyCount += count;

						if (count > 0) hslTimeOut.StartTime = DateTime.Now;
						else throw new RemoteCloseException( );

						reportProgress?.Invoke( alreadyCount, length );
						if (alreadyCount >= length) break;
					}

					hslTimeOut.IsSuccessful = true;
					return OperateResult.CreateSuccessResult( length );
				}
				else
				{
					int count = await Task.Factory.FromAsync( socket.BeginReceive( buffer, offset, buffer.Length - offset, SocketFlags.None, null, socket ), socket.EndReceive );

					if (count == 0) throw new RemoteCloseException( );
					hslTimeOut.IsSuccessful = true;
					return OperateResult.CreateSuccessResult( count );
				}
			}
			catch (RemoteCloseException)
			{
				socket?.Close( );
				if (connectErrorCount < 10_0000_0000) connectErrorCount++;
				hslTimeOut.IsSuccessful = true;
				return new OperateResult<int>( -connectErrorCount, StringResources.Language.RemoteClosedConnection );
			}
			catch (Exception ex)
			{
				socket?.Close( );
				hslTimeOut.IsSuccessful = true;
				if (connectErrorCount < 10_0000_0000) connectErrorCount++;
				if (hslTimeOut.IsTimeout)
				{
					return new OperateResult<int>( -connectErrorCount, StringResources.Language.ReceiveDataTimeout + hslTimeOut.DelayTime );
				}
				else
				{
					return new OperateResult<int>( -connectErrorCount, $"Socket Exception -> {ex.Message}" );
				}
			}
		}
		/// <inheritdoc cref="ReceiveCommandLineFromSocket(Socket, byte, int)"/>
		protected async Task<OperateResult<byte[]>> ReceiveCommandLineFromSocketAsync( Socket socket, byte endCode, int timeout = int.MaxValue )
		{
			List<byte> bufferArray = new List<byte>( 128 );
			try
			{
				DateTime st = DateTime.Now;
				bool bOK = false;

				// 接收到endCode为止
				while ((DateTime.Now - st).TotalMilliseconds < timeout)
				{
					if (socket.Poll( timeout, SelectMode.SelectRead ))
					{
						OperateResult<byte[]> headResult = await ReceiveAsync( socket, 1, timeout );
						if (!headResult.IsSuccess) return headResult;

						bufferArray.AddRange( headResult.Content );
						if (headResult.Content[0] == endCode)
						{
							bOK = true;
							break;
						}
					}
				}

				if (!bOK) return new OperateResult<byte[]>( StringResources.Language.ReceiveDataTimeout );

				// 指令头已经接收完成
				return OperateResult.CreateSuccessResult( bufferArray.ToArray( ) );
			}
			catch (Exception ex)
			{
				socket?.Close( );
				return new OperateResult<byte[]>( ex.Message );
			}
		}

		/// <inheritdoc cref="ReceiveCommandLineFromSocket(Socket, byte, byte, int)"/>
		protected async Task<OperateResult<byte[]>> ReceiveCommandLineFromSocketAsync( Socket socket, byte endCode1, byte endCode2, int timeout = 60_000 )
		{
			List<byte> bufferArray = new List<byte>( 128 );
			try
			{
				DateTime st = DateTime.Now;
				bool bOK = false;
				// 接收到endCode为止
				while ((DateTime.Now - st).TotalMilliseconds < timeout)
				{
					if (socket.Poll( timeout, SelectMode.SelectRead ))
					{
						OperateResult<byte[]> headResult = await ReceiveAsync( socket, 1, timeout );
						if (!headResult.IsSuccess) return headResult;

						bufferArray.AddRange( headResult.Content );
						if (headResult.Content[0] == endCode2)
						{
							if (bufferArray.Count > 1 && bufferArray[bufferArray.Count - 2] == endCode1)
							{
								bOK = true;
								break;
							}
						}
					}
				}

				if (!bOK) return new OperateResult<byte[]>( StringResources.Language.ReceiveDataTimeout );

				// 指令头已经接收完成
				return OperateResult.CreateSuccessResult( bufferArray.ToArray( ) );
			}
			catch (Exception ex)
			{
				socket?.Close( );
				return new OperateResult<byte[]>( ex.Message );
			}
		}

		/// <inheritdoc cref="Send(Socket, byte[])"/>
		protected async Task<OperateResult> SendAsync( Socket socket, byte[] data )
		{
			if (data == null) return OperateResult.CreateSuccessResult( );
			return await SendAsync( socket, data, 0, data.Length );
		}

		/// <inheritdoc cref="Send(Socket, byte[], int, int)"/>
		protected async Task<OperateResult> SendAsync( Socket socket, byte[] data, int offset, int size )
		{
			if (data == null) return OperateResult.CreateSuccessResult( );

			if (!Authorization.nzugaydgwadawdibbas( )) return new OperateResult( StringResources.Language.AuthorizationFailed );

			int alreadyCount = 0;
			try
			{
				while (true)
				{
					int count = await Task.Factory.FromAsync( socket.BeginSend( data, offset, size - alreadyCount, SocketFlags.None, null, socket ), socket.EndSend );
					alreadyCount += count;
					offset += count;

					if (alreadyCount >= size) break;
				}

				return OperateResult.CreateSuccessResult( );
			}
			catch (Exception ex)
			{
				socket?.Close( );
				if (connectErrorCount < 10_0000_0000) connectErrorCount++;
				return new OperateResult<byte[]>( -connectErrorCount, ex.Message );
			}
		}

		/// <inheritdoc cref="ReceiveByMessage(Socket, int, INetMessage, Action{long, long})"/>
		protected virtual async Task<OperateResult<byte[]>> ReceiveByMessageAsync( Socket socket, int timeOut, INetMessage netMessage, Action<long, long> reportProgress = null )
		{
			if (netMessage == null) return await ReceiveAsync( socket, -1, timeOut );

			// 接收指令头
			OperateResult<byte[]> headResult = await ReceiveAsync( socket, netMessage.ProtocolHeadBytesLength, timeOut );
			if (!headResult.IsSuccess) return headResult;

			// 获取剩余接收的数据长度
			netMessage.HeadBytes = headResult.Content;
			int contentLength = netMessage.GetContentLengthByHeadBytes( );
			if (contentLength <= 0) return OperateResult.CreateSuccessResult( headResult.Content );

			// 生成结果报文信息
			byte[] buffer = new byte[netMessage.ProtocolHeadBytesLength + contentLength];
			headResult.Content.CopyTo( buffer, 0 );

			// 接收剩余的数据长度信息
			OperateResult<int> contentResult = await ReceiveAsync( socket, buffer, netMessage.ProtocolHeadBytesLength, contentLength, timeOut, reportProgress );
			if (!contentResult.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( contentResult );

			return OperateResult.CreateSuccessResult( buffer );
		}

		/// <inheritdoc cref="ReadStream(Stream, byte[])"/>
		protected async Task<OperateResult<int>> ReadStreamAsync( Stream stream, byte[] buffer )
		{
			if (!Authorization.nzugaydgwadawdibbas( )) return new OperateResult<int>( StringResources.Language.AuthorizationFailed );

			try
			{
				int count = await stream.ReadAsync( buffer, 0, buffer.Length );
				return OperateResult.CreateSuccessResult( count );
			}
			catch (Exception ex)
			{
				stream?.Close( );
				return new OperateResult<int>( ex.Message );
			}
		}

		/// <inheritdoc cref="WriteStream(Stream, byte[])"/>
		protected async Task<OperateResult> WriteStreamAsync( Stream stream, byte[] buffer )
		{
			if (!Authorization.nzugaydgwadawdibbas( )) return new OperateResult( StringResources.Language.AuthorizationFailed );

			int alreadyCount = 0;
			try
			{
				await stream.WriteAsync( buffer, alreadyCount, buffer.Length - alreadyCount );
				return OperateResult.CreateSuccessResult( alreadyCount );
			}
			catch (Exception ex)
			{
				stream?.Close( );
				return new OperateResult<int>( ex.Message );
			}
		}

		/// <inheritdoc cref="ReceiveLong(Socket)"/>
		private async Task<OperateResult<long>> ReceiveLongAsync( Socket socket )
		{
			OperateResult<byte[]> read = await ReceiveAsync( socket, 8, -1 );
			if (read.IsSuccess)
				return OperateResult.CreateSuccessResult( BitConverter.ToInt64( read.Content, 0 ) );
			else
				return OperateResult.CreateFailedResult<long>( read );
		}

		/// <inheritdoc cref="SendLong(Socket, long)"/>
		private async Task<OperateResult> SendLongAsync( Socket socket, long value ) => await SendAsync( socket, BitConverter.GetBytes( value ) );

		/// <inheritdoc cref="SendBaseAndCheckReceive(Socket, int, int, byte[])"/>
		protected async Task<OperateResult> SendBaseAndCheckReceiveAsync( Socket socket, int headCode, int customer, byte[] send )
		{
			// 数据处理
			send = HslProtocol.CommandBytes( headCode, customer, Token, send );

			// 发送数据
			OperateResult sendResult = await SendAsync( socket, send );
			if (!sendResult.IsSuccess) return sendResult;

			// 检查对方接收完成
			OperateResult<long> checkResult = await ReceiveLongAsync( socket );
			if (!checkResult.IsSuccess) return checkResult;

			// 检查长度接收
			if (checkResult.Content != send.Length)
			{
				socket?.Close( );
				return new OperateResult( StringResources.Language.CommandLengthCheckFailed );
			}

			return checkResult;
		}

		/// <inheritdoc cref="SendBytesAndCheckReceive(Socket, int, byte[])"/>
		protected async Task<OperateResult> SendBytesAndCheckReceiveAsync( Socket socket, int customer, byte[] send )
		{
			return await SendBaseAndCheckReceiveAsync( socket, HslProtocol.ProtocolUserBytes, customer, send );
		}

		/// <inheritdoc cref="SendStringAndCheckReceive(Socket, int, string)"/>
		protected async Task<OperateResult> SendStringAndCheckReceiveAsync( Socket socket, int customer, string send )
		{
			byte[] data = string.IsNullOrEmpty( send ) ? null : Encoding.Unicode.GetBytes( send );

			return await SendBaseAndCheckReceiveAsync( socket, HslProtocol.ProtocolUserString, customer, data );
		}

		/// <inheritdoc cref="SendStringAndCheckReceive(Socket, int, string[])"/>
		protected async Task<OperateResult> SendStringAndCheckReceiveAsync( Socket socket, int customer, string[] sends )
		{
			return await SendBaseAndCheckReceiveAsync( socket, HslProtocol.ProtocolUserStringArray, customer, HslProtocol.PackStringArrayToByte( sends ) );
		}

		/// <inheritdoc cref="SendAccountAndCheckReceive(Socket, int, string, string)"/>
		protected async Task<OperateResult> SendAccountAndCheckReceiveAsync( Socket socket, int customer, string name, string pwd )
		{
			return await SendBaseAndCheckReceiveAsync( socket, HslProtocol.ProtocolAccountLogin, customer, HslProtocol.PackStringArrayToByte( new string[] { name, pwd } ) );
		}

		/// <inheritdoc cref="ReceiveAndCheckBytes(Socket, int)"/>
		protected async Task<OperateResult<byte[], byte[]>> ReceiveAndCheckBytesAsync( Socket socket, int timeout )
		{
			// 接收头指令
			OperateResult<byte[]> headResult = await ReceiveAsync( socket, HslProtocol.HeadByteLength, timeout );
			if (!headResult.IsSuccess) return OperateResult.CreateFailedResult<byte[], byte[]>( headResult );

			// 检查令牌
			if (!CheckRemoteToken( headResult.Content ))
			{
				socket?.Close( );
				return new OperateResult<byte[], byte[]>( StringResources.Language.TokenCheckFailed );
			}

			int contentLength = BitConverter.ToInt32( headResult.Content, HslProtocol.HeadByteLength - 4 );
			// 接收内容
			OperateResult<byte[]> contentResult = await ReceiveAsync( socket, contentLength, timeout );
			if (!contentResult.IsSuccess) return OperateResult.CreateFailedResult<byte[], byte[]>( contentResult );

			// 返回成功信息
			OperateResult checkResult = await SendLongAsync( socket, HslProtocol.HeadByteLength + contentLength );
			if (!checkResult.IsSuccess) return OperateResult.CreateFailedResult<byte[], byte[]>( checkResult );

			byte[] head = headResult.Content;
			byte[] content = contentResult.Content;
			content = HslProtocol.CommandAnalysis( head, content );
			return OperateResult.CreateSuccessResult( head, content );
		}

		/// <inheritdoc cref="ReceiveStringContentFromSocket(Socket, int)"/>
		protected async Task<OperateResult<int, string>> ReceiveStringContentFromSocketAsync( Socket socket, int timeOut = 30_000 )
		{
			OperateResult<byte[], byte[]> receive = await ReceiveAndCheckBytesAsync( socket, timeOut );
			if (!receive.IsSuccess) return OperateResult.CreateFailedResult<int, string>( receive );

			// 检查是否是字符串信息
			if (BitConverter.ToInt32( receive.Content1, 0 ) != HslProtocol.ProtocolUserString)
			{
				socket?.Close( );
				return new OperateResult<int, string>( StringResources.Language.CommandHeadCodeCheckFailed );
			}

			if (receive.Content2 == null) receive.Content2 = new byte[0];
			// 分析数据
			return OperateResult.CreateSuccessResult( BitConverter.ToInt32( receive.Content1, 4 ), Encoding.Unicode.GetString( receive.Content2 ) );
		}

		/// <inheritdoc cref="ReceiveStringArrayContentFromSocket(Socket, int)"/>
		protected async Task<OperateResult<int, string[]>> ReceiveStringArrayContentFromSocketAsync( Socket socket, int timeOut = 30_000 )
		{
			OperateResult<byte[], byte[]> receive = await ReceiveAndCheckBytesAsync( socket, timeOut );
			if (!receive.IsSuccess) return OperateResult.CreateFailedResult<int, string[]>( receive );

			// 检查是否是字符串信息
			if (BitConverter.ToInt32( receive.Content1, 0 ) != HslProtocol.ProtocolUserStringArray)
			{
				socket?.Close( );
				return new OperateResult<int, string[]>( StringResources.Language.CommandHeadCodeCheckFailed );
			}

			if (receive.Content2 == null) receive.Content2 = new byte[4];
			return OperateResult.CreateSuccessResult( BitConverter.ToInt32( receive.Content1, 4 ), HslProtocol.UnPackStringArrayFromByte( receive.Content2 ) );
		}

		/// <inheritdoc cref="ReceiveBytesContentFromSocket(Socket, int)"/>
		protected async Task<OperateResult<int, byte[]>> ReceiveBytesContentFromSocketAsync( Socket socket, int timeout = 30_000 )
		{
			OperateResult<byte[], byte[]> receive = await ReceiveAndCheckBytesAsync( socket, timeout );
			if (!receive.IsSuccess) return OperateResult.CreateFailedResult<int, byte[]>( receive );

			// 检查是否是字节信息
			if (BitConverter.ToInt32( receive.Content1, 0 ) != HslProtocol.ProtocolUserBytes)
			{
				socket?.Close( );
				return new OperateResult<int, byte[]>( StringResources.Language.CommandHeadCodeCheckFailed );
			}

			// 分析数据
			return OperateResult.CreateSuccessResult( BitConverter.ToInt32( receive.Content1, 4 ), receive.Content2 );
		}

		/// <inheritdoc cref="SendStreamToSocket(Socket, Stream, long, Action{long, long}, bool)"/>
		protected async Task<OperateResult> SendStreamToSocketAsync( Socket socket, Stream stream, long receive, Action<long, long> report, bool reportByPercent )
		{
			byte[] buffer = new byte[fileCacheSize]; // 100K的数据缓存池
			long SendTotal = 0;
			long percent = 0;
			stream.Position = 0;
			while (SendTotal < receive)
			{
				// 先从流中接收数据
				OperateResult<int> read = await ReadStreamAsync( stream, buffer );
				if (!read.IsSuccess)
				{
					socket?.Close( );
					return read;
				}

				SendTotal += read.Content;
				// 然后再异步写到socket中
				byte[] newBuffer = new byte[read.Content];
				Array.Copy( buffer, 0, newBuffer, 0, newBuffer.Length );
				OperateResult write = await SendBytesAndCheckReceiveAsync( socket, read.Content, newBuffer );
				if (!write.IsSuccess)
				{
					socket?.Close( );
					return write;
				}
				// 报告进度
				if (reportByPercent)
				{
					long percentCurrent = SendTotal * 100 / receive;
					if (percent != percentCurrent)
					{
						percent = percentCurrent;
						report?.Invoke( SendTotal, receive );
					}
				}
				else
				{
					// 报告进度
					report?.Invoke( SendTotal, receive );
				}
			}

			return OperateResult.CreateSuccessResult( );
		}

		/// <inheritdoc cref="WriteStreamFromSocket(Socket, Stream, long, Action{long, long}, bool)"/>
		protected async Task<OperateResult> WriteStreamFromSocketAsync( Socket socket, Stream stream, long totalLength, Action<long, long> report, bool reportByPercent )
		{
			long count_receive = 0;
			long percent = 0;
			while (count_receive < totalLength)
			{
				// 先从流中异步接收数据
				OperateResult<int, byte[]> read = await ReceiveBytesContentFromSocketAsync( socket, 60_000 );
				if (!read.IsSuccess) return read;
				count_receive += read.Content1;

				// 开始写入文件流
				OperateResult write = await WriteStreamAsync( stream, read.Content2 );
				if (!write.IsSuccess)
				{
					socket?.Close( );
					return write;
				}

				// 报告进度
				if (reportByPercent)
				{
					long percentCurrent = count_receive * 100 / totalLength;
					if (percent != percentCurrent)
					{
						percent = percentCurrent;
						report?.Invoke( count_receive, totalLength );
					}
				}
				else
				{
					report?.Invoke( count_receive, totalLength );
				}
			}
			return OperateResult.CreateSuccessResult( );
		}
#endif
		#endregion

		/*********************************************************************************
		 * 
		 * 特殊方法的实现区域，比如websocket, mqtt, redis
		 * 
		 * 
		 **********************************************************************************/

		#region WebSocket Receive

		/// <summary>
		/// 从socket接收一条完整的websocket数据，返回<see cref="WebSocketMessage"/>的数据信息<br />
		/// Receive a complete websocket data from the socket, return the data information of the <see cref="WebSocketMessage"/>
		/// </summary>
		/// <param name="socket">网络套接字</param>
		/// <returns>包含websocket消息的结果内容</returns>
		protected OperateResult<WebSocketMessage> ReceiveWebSocketPayload( Socket socket )
		{
			List<byte> data = new List<byte>( );
			while (true)
			{
				OperateResult<WebSocketMessage, bool> read = ReceiveFrameWebSocketPayload( socket );
				if (!read.IsSuccess) return OperateResult.CreateFailedResult<WebSocketMessage>( read );

				data.AddRange( read.Content1.Payload );
				if (read.Content2) return OperateResult.CreateSuccessResult( new WebSocketMessage( )
				{
					HasMask = read.Content1.HasMask,
					OpCode = read.Content1.OpCode,
					Payload = data.ToArray( )
				} );
			}
		}

		/// <summary>
		/// 从socket接收一条<see cref="WebSocketMessage"/>片段数据，返回<see cref="WebSocketMessage"/>的数据信息和是否最后一条数据内容<br />
		/// Receive a piece of <see cref = "WebSocketMessage" /> fragment data from the socket, return the data information of <see cref = "WebSocketMessage" /> and whether the last data content
		/// </summary>
		/// <param name="socket">网络套接字</param>
		/// <returns>包含websocket消息的结果内容</returns>
		protected OperateResult<WebSocketMessage, bool> ReceiveFrameWebSocketPayload( Socket socket )
		{
			OperateResult<byte[]> head = Receive( socket, 2, 5_000 );
			if (!head.IsSuccess) return OperateResult.CreateFailedResult<WebSocketMessage, bool>( head );

			bool isEof = (head.Content[0] & 0x80) == 0x80;
			bool hasMask = (head.Content[1] & 0x80) == 0x80;
			int opCode = head.Content[0] & 0x0F;
			byte[] mask = null;
			int length = head.Content[1] & 0x7F;
			if (length == 126)
			{
				OperateResult<byte[]> extended = Receive( socket, 2, 5_000 );
				if (!extended.IsSuccess) return OperateResult.CreateFailedResult<WebSocketMessage, bool>( extended );

				Array.Reverse( extended.Content );
				length = BitConverter.ToUInt16( extended.Content, 0 );
			}
			else if (length == 127)
			{
				OperateResult<byte[]> extended = Receive( socket, 8, 5_000 );
				if (!extended.IsSuccess) return OperateResult.CreateFailedResult<WebSocketMessage, bool>( extended );

				Array.Reverse( extended.Content );
				length = (int)BitConverter.ToUInt64( extended.Content, 0 );
			}

			if (hasMask)
			{
				OperateResult<byte[]> maskResult = Receive( socket, 4, 5_000 );
				if (!maskResult.IsSuccess) return OperateResult.CreateFailedResult<WebSocketMessage, bool>( maskResult );

				mask = maskResult.Content;
			}

			OperateResult<byte[]> payload = Receive( socket, length );
			if (!payload.IsSuccess) return OperateResult.CreateFailedResult<WebSocketMessage, bool>( payload );

			if (hasMask)
				for (int i = 0; i < payload.Content.Length; i++)
					payload.Content[i] = (byte)(payload.Content[i] ^ mask[i % 4]);

			return OperateResult.CreateSuccessResult( new WebSocketMessage( )
			{
				HasMask = hasMask,
				OpCode = opCode,
				Payload = payload.Content
			}, isEof );
		}
#if !NET35 && !NET20
		/// <inheritdoc cref="ReceiveWebSocketPayload(Socket)"/>
		protected async Task<OperateResult<WebSocketMessage>> ReceiveWebSocketPayloadAsync( Socket socket )
		{
			List<byte> data = new List<byte>( );
			while (true)
			{
				OperateResult<WebSocketMessage, bool> read = await ReceiveFrameWebSocketPayloadAsync( socket );
				if (!read.IsSuccess) return OperateResult.CreateFailedResult<WebSocketMessage>( read );

				data.AddRange( read.Content1.Payload );
				if (read.Content2) return OperateResult.CreateSuccessResult( new WebSocketMessage( )
				{
					HasMask = read.Content1.HasMask,
					OpCode = read.Content1.OpCode,
					Payload = data.ToArray( )
				} );
			}
		}

		/// <inheritdoc cref="ReceiveFrameWebSocketPayload(Socket)"/>
		protected async Task<OperateResult<WebSocketMessage, bool>> ReceiveFrameWebSocketPayloadAsync( Socket socket )
		{
			OperateResult<byte[]> head = await ReceiveAsync( socket, 2, 10_000 );
			if (!head.IsSuccess) return OperateResult.CreateFailedResult<WebSocketMessage, bool>( head );

			bool isEof   = (head.Content[0] & 0x80) == 0x80;
			bool hasMask = (head.Content[1] & 0x80) == 0x80;
			int opCode   =  head.Content[0] & 0x0F;
			byte[] mask = null;
			int length   = head.Content[1] & 0x7F;
			if (length == 126)
			{
				OperateResult<byte[]> extended = await ReceiveAsync( socket, 2, 30_000 );
				if (!extended.IsSuccess) return OperateResult.CreateFailedResult<WebSocketMessage, bool>( extended );

				Array.Reverse( extended.Content );
				length = BitConverter.ToUInt16( extended.Content, 0 );
			}
			else if (length == 127)
			{
				OperateResult<byte[]> extended = await ReceiveAsync( socket, 8, 30_000 );
				if (!extended.IsSuccess) return OperateResult.CreateFailedResult<WebSocketMessage, bool>( extended );

				Array.Reverse( extended.Content );
				length = (int)BitConverter.ToUInt64( extended.Content, 0 );
			}

			if (hasMask)
			{
				OperateResult<byte[]> maskResult = await ReceiveAsync( socket, 4, 30_000 );
				if (!maskResult.IsSuccess) return OperateResult.CreateFailedResult<WebSocketMessage, bool>( maskResult );

				mask = maskResult.Content;
			}

			OperateResult<byte[]> payload = await ReceiveAsync( socket, length, 60_000 );
			if (!payload.IsSuccess) return OperateResult.CreateFailedResult<WebSocketMessage, bool>( payload );

			if (hasMask)
				for (int i = 0; i < payload.Content.Length; i++)
					payload.Content[i] = (byte)(payload.Content[i] ^ mask[i % 4]);

			return OperateResult.CreateSuccessResult( new WebSocketMessage( )
			{
				HasMask = hasMask,
				OpCode = opCode,
				Payload = payload.Content
			}, isEof );
		}
#endif
		#endregion

		#region Mqtt Receive

		/// <summary>
		/// 基于MQTT协议，从网络套接字中接收剩余的数据长度<br />
		/// Receives the remaining data length from the network socket based on the MQTT protocol
		/// </summary>
		/// <param name="socket">网络套接字</param>
		/// <returns>网络中剩余的长度数据</returns>
		private OperateResult<int> ReceiveMqttRemainingLength( Socket socket )
		{
			List<byte> buffer = new List<byte>( );
			while (true)
			{
				OperateResult<byte[]> read = Receive( socket, 1, 5_000 );
				if (!read.IsSuccess) return OperateResult.CreateFailedResult<int>( read );

				buffer.Add( read.Content[0] );
				if (read.Content[0] < 0x80) break;
				if (buffer.Count >= 4) break;
			}

			if (buffer.Count > 4) return new OperateResult<int>( "Receive Length is too long!" );
			if (buffer.Count == 1) return OperateResult.CreateSuccessResult( (int)buffer[0] );
			if (buffer.Count == 2) return OperateResult.CreateSuccessResult( buffer[0] - 128 + buffer[1] * 128 );
			if (buffer.Count == 3) return OperateResult.CreateSuccessResult( buffer[0] - 128 + (buffer[1] - 128) * 128 + buffer[2] * 128 * 128 );
			return OperateResult.CreateSuccessResult( (buffer[0] - 128) + (buffer[1] - 128) * 128 + (buffer[2] - 128) * 128 * 128 + buffer[3] * 128 * 128 * 128 );
		}

		/// <summary>
		/// 接收一条完整的MQTT协议的报文信息，包含控制码和负载数据<br />
		/// Receive a message of a completed MQTT protocol, including control code and payload data
		/// </summary>
		/// <param name="socket">网络套接字</param>
		/// <param name="timeOut">超时时间</param>
		/// <param name="reportProgress">进度报告，第一个参数是已完成的字节数量，第二个参数是总字节数量。</param>
		/// <returns>结果数据内容</returns>
		protected OperateResult<byte, byte[]> ReceiveMqttMessage( Socket socket, int timeOut, Action<long, long> reportProgress = null )
		{
			OperateResult<byte[]> readCode = Receive( socket, 1, timeOut );
			if (!readCode.IsSuccess) return OperateResult.CreateFailedResult<byte, byte[]>( readCode );

			OperateResult<int> readContentLength = ReceiveMqttRemainingLength( socket );
			if (!readContentLength.IsSuccess) return OperateResult.CreateFailedResult<byte, byte[]>( readContentLength );

			if (readCode.Content[0] >> 4 == MqttControlMessage.REPORTPROGRESS) reportProgress = null;
			if (readCode.Content[0] >> 4 == MqttControlMessage.FAILED) reportProgress = null;

			OperateResult<byte[]> readContent = Receive( socket, readContentLength.Content, reportProgress: reportProgress );
			if (!readContent.IsSuccess) return OperateResult.CreateFailedResult<byte, byte[]>( readContent );

			return OperateResult.CreateSuccessResult( readCode.Content[0], readContent.Content );
		}

		/// <summary>
		/// 使用MQTT协议从socket接收指定长度的字节数组，然后全部写入到流中，可以指定进度报告<br />
		/// Use the MQTT protocol to receive a byte array of specified length from the socket, and then write all of them to the stream, and you can specify a progress report
		/// </summary>
		/// <param name="socket">网络套接字</param>
		/// <param name="stream">数据流</param>
		/// <param name="fileSize">数据大小</param>
		/// <param name="timeOut">超时时间</param>
		/// <param name="reportProgress">进度报告，第一个参数是已完成的字节数量，第二个参数是总字节数量。</param>
		/// <param name="aesCryptography">AES数据加密对象，如果为空，则不进行加密</param>
		/// <returns>是否操作成功</returns>
		protected OperateResult ReceiveMqttStream( Socket socket, Stream stream, long fileSize, int timeOut, Action<long, long> reportProgress = null, AesCryptography aesCryptography = null )
		{
			long already = 0;
			while (already < fileSize)
			{
				OperateResult<byte, byte[]> receive = ReceiveMqttMessage( socket, timeOut, null );
				if (!receive.IsSuccess) return receive;

				if (receive.Content1 == MqttControlMessage.FAILED) { socket?.Close( ); return new OperateResult( Encoding.UTF8.GetString( receive.Content2 ) ); }
				if(aesCryptography != null)
				{
					try
					{
						receive.Content2 = aesCryptography.Decrypt( receive.Content2 );
					}
					catch(Exception ex)
					{
						socket?.Close( );
						return new OperateResult( "AES Decrypt file stream failed: " + ex.Message );
					}
				}

				OperateResult write = WriteStream( stream, receive.Content2 );
				if (!write.IsSuccess) return write;

				already += receive.Content2.Length;
				byte[] ack = new byte[16];
				BitConverter.GetBytes( already ).CopyTo( ack, 0 );
				BitConverter.GetBytes( fileSize ).CopyTo( ack, 8 );

				OperateResult send = Send( socket, MqttHelper.BuildMqttCommand( MqttControlMessage.FileNoSense, null, ack ).Content );
				if (!send.IsSuccess) return send;

				reportProgress?.Invoke( already, fileSize );
			}

			return OperateResult.CreateSuccessResult( );
		}

		/// <summary>
		/// 使用MQTT协议将流中的数据读取到字节数组，然后都写入到socket里面，可以指定进度报告，主要用于将文件发送到网络。<br />
		/// Use the MQTT protocol to read the data in the stream into a byte array, and then write them all into the socket. 
		/// You can specify a progress report, which is mainly used to send files to the network.
		/// </summary>
		/// <param name="socket">网络套接字</param>
		/// <param name="stream">流</param>
		/// <param name="fileSize">总的数据大小</param>
		/// <param name="timeOut">超时信息</param>
		/// <param name="reportProgress">进度报告，第一个参数是已完成的字节数量，第二个参数是总字节数量。</param>
		/// <param name="aesCryptography">AES数据加密对象，如果为空，则不进行加密</param>
		/// <returns>是否操作成功</returns>
		protected OperateResult SendMqttStream( Socket socket, Stream stream, long fileSize, int timeOut, Action<long, long> reportProgress = null, AesCryptography aesCryptography = null )
		{
			byte[] buffer = new byte[fileCacheSize]; // 100K的数据缓存池
			long already = 0;
			stream.Position = 0;
			while (already < fileSize)
			{
				// 先从流中接收数据
				OperateResult<int> read = ReadStream( stream, buffer );
				if (!read.IsSuccess) { socket?.Close( ); return read; }

				already += read.Content;
				// 然后再异步写到socket中
				OperateResult write = Send( socket, MqttHelper.BuildMqttCommand( MqttControlMessage.FileNoSense, null, buffer.SelectBegin( read.Content ), aesCryptography ).Content );
				if (!write.IsSuccess) { socket?.Close( ); return write; }

				// 等待对方接收完成
				OperateResult<byte, byte[]> receive = ReceiveMqttMessage( socket, timeOut, null );
				if (!receive.IsSuccess) return receive;

				// 报告进度
				reportProgress?.Invoke( already, fileSize );
			}

			return OperateResult.CreateSuccessResult( );
		}

		/// <summary>
		/// 使用MQTT协议将一个文件发送到网络上去，需要指定文件名，保存的文件名，可选指定文件描述信息，进度报告<br />
		/// To send a file to the network using the MQTT protocol, you need to specify the file name, the saved file name, 
		/// optionally specify the file description information, and the progress report
		/// </summary>
		/// <param name="socket">网络套接字</param>
		/// <param name="filename">文件名称</param>
		/// <param name="servername">对方接收后保存的文件名</param>
		/// <param name="filetag">文件的描述信息</param>
		/// <param name="reportProgress">进度报告，第一个参数是已完成的字节数量，第二个参数是总字节数量。</param>
		/// <param name="aesCryptography">AES数据加密对象，如果为空，则不进行加密</param>
		/// <returns>是否操作成功</returns>
		protected OperateResult SendMqttFile( Socket socket, string filename, string servername, string filetag, Action<long, long> reportProgress = null, AesCryptography aesCryptography = null )
		{
			// 发送文件名，大小，标签
			FileInfo info = new FileInfo( filename );
			if (!File.Exists( filename ))
			{
				// 如果文件不存在
				OperateResult notFoundResult = Send( socket, MqttHelper.BuildMqttCommand( MqttControlMessage.FAILED, null, Encoding.UTF8.GetBytes( StringResources.Language.FileNotExist ) ).Content );
				if (!notFoundResult.IsSuccess) return notFoundResult;

				socket?.Close( );
				return new OperateResult( StringResources.Language.FileNotExist );
			}

			// 文件存在的情况，就先发送文件信息
			string[] array = new string[] { servername, info.Length.ToString( ), filetag };
			OperateResult sendResult = Send( socket, MqttHelper.BuildMqttCommand( MqttControlMessage.FileNoSense, null, HslProtocol.PackStringArrayToByte( array ) ).Content );
			if (!sendResult.IsSuccess) return sendResult;

			// 确认对方是否准备接收了
			OperateResult<byte, byte[]> check = ReceiveMqttMessage( socket, 60_000, null );
			if (!check.IsSuccess) return check;
			if (check.Content1 == MqttControlMessage.FAILED) { socket?.Close( ); return new OperateResult( Encoding.UTF8.GetString( check.Content2 ) ); }

			try
			{
				OperateResult result = new OperateResult( );
				using (FileStream fs = new FileStream( filename, FileMode.Open, FileAccess.Read ))
				{
					result = SendMqttStream( socket, fs, info.Length, 60_000, reportProgress, aesCryptography );
				}
				return result;
			}
			catch (Exception ex)
			{
				socket?.Close( );
				return new OperateResult( $"SendMqttStream Exception -> " + ex.Message );
			}
		}

		/// <summary>
		/// 使用MQTT协议将一个数据流发送到网络上去，需要保存的文件名，可选指定文件描述信息，进度报告<br />
		/// Use the MQTT protocol to send a data stream to the network, the file name that needs to be saved, optional file description information, progress report
		/// </summary>
		/// <param name="socket">网络套接字</param>
		/// <param name="stream">数据流</param>
		/// <param name="servername">对方接收后保存的文件名</param>
		/// <param name="filetag">文件的描述信息</param>
		/// <param name="reportProgress">进度报告，第一个参数是已完成的字节数量，第二个参数是总字节数量。</param>
		/// <param name="aesCryptography">AES数据加密对象，如果为空，则不进行加密</param>
		/// <returns>是否操作成功</returns>
		protected OperateResult SendMqttFile( Socket socket, Stream stream, string servername, string filetag, Action<long, long> reportProgress = null, AesCryptography aesCryptography = null )
		{
			// 文件存在的情况，就先发送文件信息
			string[] array = new string[] { servername, stream.Length.ToString( ), filetag };
			OperateResult sendResult = Send( socket, MqttHelper.BuildMqttCommand( MqttControlMessage.FileNoSense, null, HslProtocol.PackStringArrayToByte( array ) ).Content );
			if (!sendResult.IsSuccess) return sendResult;

			// 确认对方是否准备接收了
			OperateResult<byte, byte[]> check = ReceiveMqttMessage( socket, 60_000, null );
			if (!check.IsSuccess) return check;
			if (check.Content1 == MqttControlMessage.FAILED) { socket?.Close( ); return new OperateResult( Encoding.UTF8.GetString( check.Content2 ) ); }

			try
			{
				return SendMqttStream( socket, stream, stream.Length, 60_000, reportProgress, aesCryptography );
			}
			catch (Exception ex)
			{
				socket?.Close( );
				return new OperateResult( $"SendMqttStream Exception -> " + ex.Message );
			}
		}

		/// <summary>
		/// 使用MQTT协议从网络接收字节数组，然后写入文件或流中，支持进度报告<br />
		/// Use MQTT protocol to receive byte array from the network, and then write it to file or stream, support progress report
		/// </summary>
		/// <param name="socket">网络套接字</param>
		/// <param name="source">文件名或是流</param>
		/// <param name="reportProgress">进度报告</param>
		/// <param name="aesCryptography">AES数据加密对象，如果为空，则不进行加密</param>
		/// <returns>是否操作成功，如果成功，携带文件基本信息</returns>
		protected OperateResult<FileBaseInfo> ReceiveMqttFile( Socket socket, object source, Action<long, long> reportProgress = null, AesCryptography aesCryptography = null )
		{
			// 先接收文件头信息
			OperateResult<byte, byte[]> receiveFileInfo = ReceiveMqttMessage( socket, 60_000, null );
			if (!receiveFileInfo.IsSuccess) return OperateResult.CreateFailedResult<FileBaseInfo>( receiveFileInfo );

			// 判断文件是否存在
			if (receiveFileInfo.Content1 == MqttControlMessage.FAILED)
			{
				socket?.Close( );
				return new OperateResult<FileBaseInfo>( Encoding.UTF8.GetString( receiveFileInfo.Content2 ) );
			}

			FileBaseInfo fileBaseInfo = new FileBaseInfo( );
			string[] array = HslProtocol.UnPackStringArrayFromByte( receiveFileInfo.Content2 );
			fileBaseInfo.Name = array[0];
			fileBaseInfo.Size = long.Parse( array[1] );
			fileBaseInfo.Tag = array[2];

			// 回发一条数据确认接收，如果限定了文件大小，则通知 FAILED
			Send( socket, MqttHelper.BuildMqttCommand( MqttControlMessage.FileNoSense, null, null ).Content );

			try
			{
				OperateResult write = null;
				if (source is string savename)
				{
					using (FileStream fs = new FileStream( savename, FileMode.Create, FileAccess.Write ))
						write = ReceiveMqttStream( socket, fs, fileBaseInfo.Size, 60_000, reportProgress, aesCryptography );

					if (!write.IsSuccess)
					{
						if (File.Exists( savename )) File.Delete( savename );
						return OperateResult.CreateFailedResult<FileBaseInfo>( write );
					}
				}
				else if (source is Stream stream)
				{
					write = ReceiveMqttStream( socket, stream, fileBaseInfo.Size, 60_000, reportProgress, aesCryptography );
				}
				else
				{
					throw new Exception( "Not Supported Type" );
				}

				return OperateResult.CreateSuccessResult( fileBaseInfo );
			}
			catch (Exception ex)
			{
				socket?.Close( );
				return new OperateResult<FileBaseInfo>( ex.Message );
			}
		}
#if !NET35 && !NET20
		/// <inheritdoc cref="ReceiveMqttRemainingLength(Socket)"/>
		private async Task<OperateResult<int>> ReceiveMqttRemainingLengthAsync( Socket socket )
		{
			List<byte> buffer = new List<byte>( );
			while (true)
			{
				OperateResult<byte[]> rece = await ReceiveAsync( socket, 1, 5_000 );
				if (!rece.IsSuccess) return OperateResult.CreateFailedResult<int>( rece );

				buffer.Add( rece.Content[0] );
				if (rece.Content[0] < 0x80) break;
				if (buffer.Count >= 4) break;
			}

			if (buffer.Count > 4)  return new OperateResult<int>( "Receive Length is too long!" );
			if (buffer.Count == 1) return OperateResult.CreateSuccessResult( (int)buffer[0] );
			if (buffer.Count == 2) return OperateResult.CreateSuccessResult( buffer[0] - 128 + buffer[1] * 128 );
			if (buffer.Count == 3) return OperateResult.CreateSuccessResult( buffer[0] - 128 + (buffer[1] - 128) * 128 + buffer[2] * 128 * 128 );
			return OperateResult.CreateSuccessResult( (buffer[0] - 128) + (buffer[1] - 128) * 128 + (buffer[2] - 128) * 128 * 128 + buffer[3] * 128 * 128 * 128 );
		}

		/// <inheritdoc cref="ReceiveMqttMessage(Socket, int, Action{long, long})"/>
		protected async Task<OperateResult<byte, byte[]>> ReceiveMqttMessageAsync( Socket socket, int timeOut,  Action<long, long> reportProgress = null )
		{
			OperateResult<byte[]> readCode = await ReceiveAsync( socket, 1, timeOut );
			if (!readCode.IsSuccess) return OperateResult.CreateFailedResult<byte, byte[]>( readCode );

			OperateResult<int> readContentLength = await ReceiveMqttRemainingLengthAsync( socket );
			if (!readContentLength.IsSuccess) return OperateResult.CreateFailedResult<byte, byte[]>( readContentLength );

			if (readCode.Content[0] >> 4 == MqttControlMessage.REPORTPROGRESS) reportProgress = null;
			if (readCode.Content[0] >> 4 == MqttControlMessage.FAILED) reportProgress = null;

			OperateResult<byte[]> readContent = await ReceiveAsync( socket, readContentLength.Content, timeOut, reportProgress: reportProgress );
			if (!readContent.IsSuccess) return OperateResult.CreateFailedResult<byte, byte[]>( readContent );

			return OperateResult.CreateSuccessResult( readCode.Content[0], readContent.Content );
		}

		/// <inheritdoc cref="ReceiveMqttStream(Socket, Stream, long, int, Action{long, long}, AesCryptography)"/>
		protected async Task<OperateResult> ReceiveMqttStreamAsync( Socket socket, Stream stream, long fileSize, int timeOut, Action<long, long> reportProgress = null, AesCryptography aesCryptography = null )
		{
			long already = 0;
			while(already < fileSize)
			{
				OperateResult<byte, byte[]> receive = await ReceiveMqttMessageAsync( socket, timeOut, null );
				if (!receive.IsSuccess) return receive;

				if (receive.Content1 == MqttControlMessage.FAILED) { socket?.Close( ); return new OperateResult( Encoding.UTF8.GetString( receive.Content2 ) ); }
				if (aesCryptography != null)
				{
					try
					{
						receive.Content2 = aesCryptography.Decrypt( receive.Content2 );
					}
					catch (Exception ex)
					{
						socket?.Close( );
						return new OperateResult( "AES Decrypt file stream failed: " + ex.Message );
					}
				}

				OperateResult write = await WriteStreamAsync( stream, receive.Content2 );
				if (!write.IsSuccess) return write;

				already += receive.Content2.Length;
				byte[] ack = new byte[16];

				BitConverter.GetBytes( already  ).CopyTo( ack, 0 );
				BitConverter.GetBytes( fileSize ).CopyTo( ack, 8 );

				OperateResult send = await SendAsync( socket, MqttHelper.BuildMqttCommand( MqttControlMessage.FileNoSense, null, ack ).Content );
				if (!send.IsSuccess) return send;
				reportProgress?.Invoke( already, fileSize );
			}

			return OperateResult.CreateSuccessResult( );
		}

		/// <inheritdoc cref="SendMqttStream(Socket, Stream, long, int, Action{long, long}, AesCryptography)"/>
		protected async Task<OperateResult> SendMqttStreamAsync( Socket socket, Stream stream, long fileSize, int timeOut, Action<long, long> reportProgress = null, AesCryptography aesCryptography = null )
		{
			byte[] buffer = new byte[fileCacheSize]; // 100K的数据缓存池
			long already = 0;
			stream.Position = 0;
			while (already < fileSize)
			{
				// 先从流中接收数据
				OperateResult<int> read = await ReadStreamAsync( stream, buffer );
				if (!read.IsSuccess) { socket?.Close( ); return read; }

				already += read.Content;
				// 然后再异步写到socket中
				OperateResult write = await SendAsync( socket, MqttHelper.BuildMqttCommand( MqttControlMessage.FileNoSense, null, buffer.SelectBegin( read.Content ), aesCryptography ).Content );
				if (!write.IsSuccess) { socket?.Close( ); return write; }

				// 等待对方接收完成
				OperateResult<byte, byte[]> receive = await ReceiveMqttMessageAsync( socket, timeOut, null );
				if (!receive.IsSuccess) return receive;

				// 报告进度
				reportProgress?.Invoke( already, fileSize );
			}

			return OperateResult.CreateSuccessResult( );
		}

		/// <inheritdoc cref="SendMqttFile(Socket, string, string, string, Action{long, long}, AesCryptography)"/>
		protected async Task<OperateResult> SendMqttFileAsync( Socket socket, string filename, string servername, string filetag, Action<long, long> reportProgress = null, AesCryptography aesCryptography = null )
		{
			// 发送文件名，大小，标签
			FileInfo info = new FileInfo( filename );
			if (!File.Exists( filename ))
			{
				// 如果文件不存在
				OperateResult notFoundResult = await SendAsync( socket, MqttHelper.BuildMqttCommand( MqttControlMessage.FAILED, null, Encoding.UTF8.GetBytes( StringResources.Language.FileNotExist ) ).Content );
				if (!notFoundResult.IsSuccess) return notFoundResult;

				socket?.Close( );
				return new OperateResult( StringResources.Language.FileNotExist );
			}

			// 文件存在的情况，就先发送文件信息
			string[] array = new string[] { servername, info.Length.ToString( ), filetag };
			OperateResult sendResult = await SendAsync( socket, MqttHelper.BuildMqttCommand( MqttControlMessage.FileNoSense, null, HslProtocol.PackStringArrayToByte( array ) ).Content );
			if (!sendResult.IsSuccess) return sendResult;

			// 确认对方是否准备接收了
			OperateResult<byte, byte[]> check = await ReceiveMqttMessageAsync( socket, 60_000, null );
			if (!check.IsSuccess) return check;
			if( check.Content1 == MqttControlMessage.FAILED) { socket?.Close( ); return new OperateResult( Encoding.UTF8.GetString( check.Content2 ) ); }

			try
			{
				OperateResult result = new OperateResult( );
				using (FileStream fs = new FileStream( filename, FileMode.Open, FileAccess.Read ))
				{
					result = await SendMqttStreamAsync( socket, fs, info.Length, 60_000, reportProgress, aesCryptography );
				}
				return result;
			}
			catch (Exception ex)
			{
				socket?.Close( );
				return new OperateResult( $"SendMqttStreamAsync Exception -> " + ex.Message );
			}
		}

		/// <inheritdoc cref="SendMqttFile(Socket, Stream, string, string, Action{long, long}, AesCryptography)"/>
		protected async Task<OperateResult> SendMqttFileAsync( Socket socket, Stream stream, string servername, string filetag, Action<long, long> reportProgress = null, AesCryptography aesCryptography = null )
		{
			// 文件存在的情况，就先发送文件信息
			string[] array = new string[] { servername, stream.Length.ToString( ), filetag };
			OperateResult sendResult = await SendAsync( socket, MqttHelper.BuildMqttCommand( MqttControlMessage.FileNoSense, null, HslProtocol.PackStringArrayToByte( array ) ).Content );
			if (!sendResult.IsSuccess) return sendResult;

			// 确认对方是否准备接收了
			OperateResult<byte, byte[]> check = await ReceiveMqttMessageAsync( socket, 60_000, null );
			if (!check.IsSuccess) return check;
			if (check.Content1 == MqttControlMessage.FAILED) { socket?.Close( ); return new OperateResult( Encoding.UTF8.GetString( check.Content2 ) ); }

			try
			{
				return await SendMqttStreamAsync( socket, stream, stream.Length, 60_000, reportProgress, aesCryptography );
			}
			catch (Exception ex)
			{
				socket?.Close( );
				return new OperateResult( $"SendMqttStreamAsync Exception -> " + ex.Message );
			}
		}

		/// <inheritdoc cref="ReceiveMqttFile(Socket, object, Action{long, long}, AesCryptography)"/>
		protected async Task<OperateResult<FileBaseInfo>> ReceiveMqttFileAsync( Socket socket, object source, Action<long, long> reportProgress = null, AesCryptography aesCryptography = null )
		{
			// 先接收文件头信息
			OperateResult<byte, byte[]> receiveFileInfo = await ReceiveMqttMessageAsync( socket, 60_000, null );
			if (!receiveFileInfo.IsSuccess) return OperateResult.CreateFailedResult<FileBaseInfo>( receiveFileInfo );

			// 判断文件是否存在
			if (receiveFileInfo.Content1 == MqttControlMessage.FAILED)
			{
				socket?.Close( );
				return new OperateResult<FileBaseInfo>( Encoding.UTF8.GetString( receiveFileInfo.Content2 ) );
			}

			FileBaseInfo fileBaseInfo = new FileBaseInfo( );
			string[] array = HslProtocol.UnPackStringArrayFromByte( receiveFileInfo.Content2 );
			if (array.Length < 3) { socket?.Close( ); return new OperateResult<FileBaseInfo>( "FileBaseInfo Check failed: " + array.ToArrayString( ) ); }

			fileBaseInfo.Name = array[0];
			fileBaseInfo.Size = long.Parse( array[1] );
			fileBaseInfo.Tag = array[2];

			// 回发一条数据确认接收，如果限定了文件大小，则通知Filed
			await SendAsync( socket, MqttHelper.BuildMqttCommand( MqttControlMessage.FileNoSense, null, null ).Content );

			try
			{
				OperateResult write = null;
				if(source is string savename)
				{
					using (FileStream fs = new FileStream( savename, FileMode.Create, FileAccess.Write ))
						write = await ReceiveMqttStreamAsync( socket, fs, fileBaseInfo.Size, 60_000, reportProgress, aesCryptography );

					if (!write.IsSuccess)
					{
						if (File.Exists( savename )) File.Delete( savename );
						return OperateResult.CreateFailedResult<FileBaseInfo>( write );
					}
				}
				else if( source is Stream stream)
				{
					write = await ReceiveMqttStreamAsync( socket, stream, fileBaseInfo.Size, 60_000, reportProgress, aesCryptography );
				}
				else
				{
					throw new Exception( "Not Supported Type" );
				}

				return OperateResult.CreateSuccessResult( fileBaseInfo );
			}
			catch (Exception ex)
			{
				socket?.Close( );
				return new OperateResult<FileBaseInfo>( ex.Message );
			}
		}
#endif
		#endregion

		#region Redis Receive

		/// <summary>
		/// 接收一行基于redis协议的字符串的信息，需要指定固定的长度<br />
		/// Receive a line of information based on the redis protocol string, you need to specify a fixed length
		/// </summary>
		/// <param name="socket">网络套接字</param>
		/// <param name="length">字符串的长度</param>
		/// <returns>带有结果对象的数据信息</returns>
		protected OperateResult<byte[]> ReceiveRedisCommandString( Socket socket, int length )
		{
			List<byte> bufferArray = new List<byte>( );
			OperateResult<byte[]> receive = Receive( socket, length );
			if (!receive.IsSuccess) return receive;

			bufferArray.AddRange( receive.Content );

			OperateResult<byte[]> commandTail = ReceiveCommandLineFromSocket( socket, (byte)'\n' );
			if (!commandTail.IsSuccess) return commandTail;

			bufferArray.AddRange( commandTail.Content );
			return OperateResult.CreateSuccessResult( bufferArray.ToArray( ) );
		}

		/// <summary>
		/// 从网络接收一条完整的redis报文的消息<br />
		/// Receive a complete redis message from the network
		/// </summary>
		/// <param name="socket">网络套接字</param>
		/// <returns>接收的结果对象</returns>
		protected OperateResult<byte[]> ReceiveRedisCommand( Socket socket )
		{
			List<byte> bufferArray = new List<byte>( );

			OperateResult<byte[]> readCommandLine = ReceiveCommandLineFromSocket( socket, (byte)'\n' );
			if (!readCommandLine.IsSuccess) return readCommandLine;

			bufferArray.AddRange( readCommandLine.Content );
			if (readCommandLine.Content[0] == '+' || readCommandLine.Content[0] == '-' || readCommandLine.Content[0] == ':')
			{
				return OperateResult.CreateSuccessResult( bufferArray.ToArray( ) );   // 状态回复，错误回复，整数回复
			}
			else if (readCommandLine.Content[0] == '$')
			{
				// 批量回复，允许最大512M字节
				OperateResult<int> lengthResult = RedisHelper.GetNumberFromCommandLine( readCommandLine.Content );
				if (!lengthResult.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( lengthResult );

				if (lengthResult.Content < 0) return OperateResult.CreateSuccessResult( bufferArray.ToArray( ) );

				// 接收字符串信息
				OperateResult<byte[]> receiveContent = ReceiveRedisCommandString( socket, lengthResult.Content );
				if (!receiveContent.IsSuccess) return receiveContent;

				bufferArray.AddRange( receiveContent.Content );
				return OperateResult.CreateSuccessResult( bufferArray.ToArray( ) );
			}
			else if (readCommandLine.Content[0] == '*')
			{
				// 多参数的回复
				OperateResult<int> lengthResult = RedisHelper.GetNumberFromCommandLine( readCommandLine.Content );
				if (!lengthResult.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( lengthResult );

				for (int i = 0; i < lengthResult.Content; i++)
				{
					OperateResult<byte[]> receiveCommand = ReceiveRedisCommand( socket );
					if (!receiveCommand.IsSuccess) return receiveCommand;

					bufferArray.AddRange( receiveCommand.Content );
				}

				return OperateResult.CreateSuccessResult( bufferArray.ToArray( ) );
			}
			else
			{
				return new OperateResult<byte[]>( "Not Supported HeadCode: " + readCommandLine.Content[0] );
			}
		}
#if !NET35 && !NET20
		/// <inheritdoc cref="ReceiveRedisCommandString(Socket, int)"/>
		protected async Task<OperateResult<byte[]>> ReceiveRedisCommandStringAsync( Socket socket, int length )
		{
			List<byte> bufferArray = new List<byte>( );
			OperateResult<byte[]> receive = await ReceiveAsync( socket, length );
			if (!receive.IsSuccess) return receive;

			bufferArray.AddRange( receive.Content );

			OperateResult<byte[]> commandTail = await ReceiveCommandLineFromSocketAsync( socket, (byte)'\n' );
			if (!commandTail.IsSuccess) return commandTail;

			bufferArray.AddRange( commandTail.Content );
			return OperateResult.CreateSuccessResult( bufferArray.ToArray( ) );
		}

		/// <inheritdoc cref="ReceiveRedisCommand(Socket)"/>
		protected async Task<OperateResult<byte[]>> ReceiveRedisCommandAsync( Socket socket )
		{
			List<byte> bufferArray = new List<byte>( );

			OperateResult<byte[]> readCommandLine = await ReceiveCommandLineFromSocketAsync( socket, (byte)'\n' );
			if (!readCommandLine.IsSuccess) return readCommandLine;

			bufferArray.AddRange( readCommandLine.Content );
			if (readCommandLine.Content[0] == '+' || readCommandLine.Content[0] == '-' || readCommandLine.Content[0] == ':')
			{
				return OperateResult.CreateSuccessResult( bufferArray.ToArray( ) );   // 状态回复，错误回复，整数回复
			}
			else if (readCommandLine.Content[0] == '$')
			{
				// 批量回复，允许最大512M字节
				OperateResult<int> lengthResult = RedisHelper.GetNumberFromCommandLine( readCommandLine.Content );
				if (!lengthResult.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( lengthResult );

				if (lengthResult.Content < 0) return OperateResult.CreateSuccessResult( bufferArray.ToArray( ) );

				// 接收字符串信息
				OperateResult<byte[]> receiveContent = await ReceiveRedisCommandStringAsync( socket, lengthResult.Content );
				if (!receiveContent.IsSuccess) return receiveContent;

				bufferArray.AddRange( receiveContent.Content );
				return OperateResult.CreateSuccessResult( bufferArray.ToArray( ) );
			}
			else if (readCommandLine.Content[0] == '*')
			{
				// 多参数的回复
				OperateResult<int> lengthResult = RedisHelper.GetNumberFromCommandLine( readCommandLine.Content );
				if (!lengthResult.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( lengthResult );

				for (int i = 0; i < lengthResult.Content; i++)
				{
					OperateResult<byte[]> receiveCommand = await ReceiveRedisCommandAsync( socket );
					if (!receiveCommand.IsSuccess) return receiveCommand;

					bufferArray.AddRange( receiveCommand.Content );
				}

				return OperateResult.CreateSuccessResult( bufferArray.ToArray( ) );
			}
			else
			{
				return new OperateResult<byte[]>( "Not Supported HeadCode: " + readCommandLine.Content[0] );
			}
		}
#endif
		#endregion

		#region HslMessage Receive

		/// <summary>
		/// 接收一条hsl协议的数据信息，自动解析，解压，解码操作，获取最后的实际的数据，接收结果依次为暗号，用户码，负载数据<br />
		/// Receive a piece of hsl protocol data information, automatically parse, decompress, and decode operations to obtain the last actual data. 
		/// The result is a opCode, user code, and payload data in order.
		/// </summary>
		/// <param name="socket">网络套接字</param>
		/// <returns>接收结果，依次为暗号，用户码，负载数据</returns>
		protected OperateResult<int, int, byte[]> ReceiveHslMessage( Socket socket )
		{
			OperateResult<byte[]> receiveHead = Receive( socket, HslProtocol.HeadByteLength, 10_000 );
			if (!receiveHead.IsSuccess) return OperateResult.CreateFailedResult<int, int, byte[]>( receiveHead );

			int receive_length = BitConverter.ToInt32( receiveHead.Content, receiveHead.Content.Length - 4 );
			OperateResult<byte[]> receiveContent = Receive( socket, receive_length );
			if (!receiveContent.IsSuccess) return OperateResult.CreateFailedResult<int, int, byte[]>( receiveContent );

			byte[] Content = HslProtocol.CommandAnalysis( receiveHead.Content, receiveContent.Content );
			int protocol = BitConverter.ToInt32( receiveHead.Content, 0 );
			int customer = BitConverter.ToInt32( receiveHead.Content, 4 );
			return OperateResult.CreateSuccessResult( protocol, customer, Content );
		}
#if !NET35 && !NET20
		/// <inheritdoc cref="ReceiveHslMessage(Socket)"/>
		protected async Task<OperateResult<int, int, byte[]>> ReceiveHslMessageAsync( Socket socket )
		{
			OperateResult<byte[]> receiveHead = await ReceiveAsync( socket, HslProtocol.HeadByteLength, 10_000 );
			if (!receiveHead.IsSuccess) return OperateResult.CreateFailedResult<int, int, byte[]>( receiveHead );

			int receive_length = BitConverter.ToInt32( receiveHead.Content, receiveHead.Content.Length - 4 );
			OperateResult<byte[]> receiveContent = await ReceiveAsync( socket, receive_length, 60_000 );
			if (!receiveContent.IsSuccess) return OperateResult.CreateFailedResult<int, int, byte[]>( receiveContent );

			byte[] Content = HslProtocol.CommandAnalysis( receiveHead.Content, receiveContent.Content );
			int protocol = BitConverter.ToInt32( receiveHead.Content, 0 );
			int customer = BitConverter.ToInt32( receiveHead.Content, 4 );
			return OperateResult.CreateSuccessResult( protocol, customer, Content );
		}
#endif
		#endregion

		#region File Operate

		/// <summary>
		/// 删除文件的操作<br />
		/// Delete file operation
		/// </summary>
		/// <param name="filename">完整的真实的文件路径</param>
		/// <returns>是否删除成功</returns>
		protected bool DeleteFileByName( string filename )
		{
			try
			{
				if (!File.Exists( filename )) return true;
				File.Delete( filename );
				return true;
			}
			catch (Exception ex)
			{
				LogNet?.WriteException( ToString( ), "delete file failed:" + filename, ex );
				return false;
			}
		}

		/// <summary>
		/// 预处理文件夹的名称，除去文件夹名称最后一个'\'或'/'，如果有的话<br />
		/// Preprocess the name of the folder, removing the last '\' or '/' in the folder name
		/// </summary>
		/// <param name="folder">文件夹名称</param>
		/// <returns>返回处理之后的名称</returns>
		protected string PreprocessFolderName( string folder )
		{
			if (folder.EndsWith( @"\" ) || folder.EndsWith( @"/" ))        // 兼容windows和linux的系统
				return folder.Substring( 0, folder.Length - 1 );
			else
				return folder;
		}

		#endregion

		#region Private Member

		private int connectErrorCount = 0;                // 连接错误次数

		#endregion

		#region Object Override

		/// <inheritdoc/>
		public override string ToString( ) => "NetworkBase";

		#endregion

		#region Static Member

		/// <summary>
		/// 通过主机名或是IP地址信息，获取到真实的IP地址信息<br />
		/// Obtain the real IP address information through the host name or IP address information
		/// </summary>
		/// <param name="hostName">主机名或是IP地址</param>
		/// <returns>IP地址信息</returns>
		public static string GetIpAddressHostName( string hostName )
		{
			IPHostEntry host = Dns.GetHostEntry( hostName );
			IPAddress ip = host.AddressList[0];
			return ip.ToString( );
		}

		#endregion
	}
}
