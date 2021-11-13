using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Net;
using System.Threading;
using HslCommunication.Core.IMessage;
using HslCommunication.BasicFramework;
using System.Net.NetworkInformation;
using HslCommunication.Reflection;
using HslCommunication.LogNet;
#if !NET35 && !NET20
using System.Threading.Tasks;
#endif

namespace HslCommunication.Core.Net
{
	/// <summary>
	/// 支持长连接，短连接两个模式的通用客户端基类 <br />
	/// Universal client base class that supports long connections and short connections to two modes
	/// </summary>
	/// <example>
	/// 无，请使用继承类实例化，然后进行数据交互，当前的类并没有具体的实现。
	/// </example>
	public class NetworkDoubleBase : NetworkBase, IDisposable
	{
		#region Constructor

		/// <summary>
		/// 默认的无参构造函数 <br />
		/// Default no-parameter constructor
		/// </summary>
		public NetworkDoubleBase( )
		{
			InteractiveLock = new SimpleHybirdLock( );                                     // 实例化数据访问锁
			connectionId = SoftBasic.GetUniqueStringByGuidAndRandom( );     // 设备的唯一的编号
		}

		#endregion

		#region Private Protect Member

		private IByteTransform byteTransform;            // 数据变换的接口
		private string ipAddress = "127.0.0.1";          // 连接的IP地址
		private int port = 10000;                        // 端口号
		private int connectTimeOut = 10000;              // 连接超时时间设置
		private string connectionId = string.Empty;      // 当前连接
		private bool isUseSpecifiedSocket = false;       // 指示是否使用指定的网络套接字访问数据

		/// <summary>
		/// 接收数据的超时时间，单位：毫秒
		/// <br />
		/// Timeout for receiving data, unit: millisecond
		/// </summary>
		protected int receiveTimeOut = 5000;
		/// <summary>
		/// 是否是长连接的状态<br />
		/// Whether it is a long connection state
		/// </summary>
		protected bool isPersistentConn = false;
		/// <summary>
		/// 交互的混合锁，保证交互操作的安全性<br />
		/// Interactive hybrid locks to ensure the security of interactive operations
		/// </summary>
		protected SimpleHybirdLock InteractiveLock;
		/// <summary>
		/// 指示长连接的套接字是否处于错误的状态<br />
		/// Indicates if the long-connected socket is in the wrong state
		/// </summary>
		protected bool IsSocketError = false;
		/// <summary>
		/// 设置日志记录报文是否二进制，如果为False，那就使用ASCII码<br />
		/// Set whether the log message is binary, if it is False, then use ASCII code
		/// </summary>
		protected bool LogMsgFormatBinary = true;

		#endregion

		#region virtual Method

		/// <summary>
		/// 获取一个新的消息对象的方法，需要在继承类里面进行重写<br />
		/// The method to get a new message object needs to be overridden in the inheritance class
		/// </summary>
		/// <returns>消息类对象</returns>
		protected virtual INetMessage GetNewNetMessage( ) => null;

		#endregion

		#region Public Properties

		/// <summary>
		/// 当前的数据变换机制，当你需要从字节数据转换类型数据的时候需要。<br />
		/// The current data transformation mechanism is required when you need to convert type data from byte data.
		/// </summary>
		/// <example>
		/// 主要是用来转换数据类型的，下面仅仅演示了2个方法，其他的类型转换，类似处理。
		/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Core\NetworkDoubleBase.cs" region="ByteTransform" title="ByteTransform示例" />
		/// </example>
		public IByteTransform ByteTransform
		{
			get { return byteTransform; }
			set { byteTransform = value; }
		}

		/// <summary>
		/// 获取或设置连接的超时时间，单位是毫秒 <br />
		/// Gets or sets the timeout for the connection, in milliseconds
		/// </summary>
		/// <example>
		/// 设置1秒的超时的示例
		/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Core\NetworkDoubleBase.cs" region="ConnectTimeOutExample" title="ConnectTimeOut示例" />
		/// </example>
		/// <remarks>
		/// 不适用于异形模式的连接。
		/// </remarks>
		[HslMqttApi( HttpMethod = "GET", Description = "Gets or sets the timeout for the connection, in milliseconds" )]
		public virtual int ConnectTimeOut
		{
			get { return connectTimeOut; }
			set { if (value >= 0) connectTimeOut = value; }
		}

		/// <summary>
		/// 获取或设置接收服务器反馈的时间，如果为负数，则不接收反馈 <br />
		/// Gets or sets the time to receive server feedback, and if it is a negative number, does not receive feedback
		/// </summary>
		/// <example>
		/// 设置1秒的接收超时的示例
		/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Core\NetworkDoubleBase.cs" region="ReceiveTimeOutExample" title="ReceiveTimeOut示例" />
		/// </example>
		/// <remarks>
		/// 超时的通常原因是服务器端没有配置好，导致访问失败，为了不卡死软件，所以有了这个超时的属性。
		/// </remarks>
		[HslMqttApi( HttpMethod = "GET", Description = "Gets or sets the time to receive server feedback, and if it is a negative number, does not receive feedback" )]
		public int ReceiveTimeOut
		{
			get { return receiveTimeOut; }
			set { receiveTimeOut = value; }
		}

		/// <summary>
		/// 获取或是设置远程服务器的IP地址，如果是本机测试，那么需要设置为127.0.0.1 <br />
		/// Get or set the IP address of the remote server. If it is a local test, then it needs to be set to 127.0.0.1
		/// </summary>
		/// <remarks>
		/// 最好实在初始化的时候进行指定，当使用短连接的时候，支持动态更改，切换；当使用长连接后，无法动态更改
		/// </remarks>
		/// <example>
		/// 以下举例modbus-tcp的短连接及动态更改ip地址的示例
		/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Core\NetworkDoubleBase.cs" region="IpAddressExample" title="IpAddress示例" />
		/// </example>
		[HslMqttApi( HttpMethod = "GET", Description = "Get or set the IP address of the remote server. If it is a local test, then it needs to be set to 127.0.0.1" )]
		public virtual string IpAddress
		{
			get
			{
				return ipAddress;
			}
			set
			{
				ipAddress = HslHelper.GetIpAddressFromInput( value );
			}
		}

		/// <summary>
		/// 获取或设置服务器的端口号，具体的值需要取决于对方的配置<br />
		/// Gets or sets the port number of the server. The specific value depends on the configuration of the other party.
		/// </summary>
		/// <remarks>
		/// 最好实在初始化的时候进行指定，当使用短连接的时候，支持动态更改，切换；当使用长连接后，无法动态更改
		/// </remarks>
		/// <example>
		/// 动态更改请参照IpAddress属性的更改。
		/// </example>
		[HslMqttApi( HttpMethod = "GET", Description = "Gets or sets the port number of the server. The specific value depends on the configuration of the other party." )]
		public virtual int Port
		{
			get { return port; }
			set { port = value; }
		}

		/// <inheritdoc cref="IReadWriteNet.ConnectionId"/>
		[HslMqttApi( HttpMethod = "GET", Description = "The unique ID number of the current connection. The default is a 20-digit guid code plus a random number." )]
		public string ConnectionId
		{
			get { return connectionId; }
			set { connectionId = value; }
		}

		/// <summary>
		/// 获取或设置在正式接收对方返回数据前的时候，需要休息的时间，当设置为0的时候，不需要休息。<br />
		/// Get or set the time required to rest before officially receiving the data from the other party. When it is set to 0, no rest is required.
		/// </summary>
		[HslMqttApi( HttpMethod = "GET", Description = "Get or set the time required to rest before officially receiving the data from the other party. When it is set to 0, no rest is required." )]
		public int SleepTime { get; set; }

		/// <summary>
		/// 获取或设置绑定的本地的IP地址和端口号信息，如果端口设置为0，代表任何可用的端口<br />
		/// Get or set the bound local IP address and port number information, if the port is set to 0, it means any available port
		/// </summary>
		public IPEndPoint LocalBinding { get; set; }

		/// <summary>
		/// 当前的异形连接对象，如果设置了异形连接的话，仅用于异形模式的情况使用<br />
		/// The current alien connection object, if alien connection is set, is only used in the case of alien mode
		/// </summary>
		/// <remarks>
		/// 具体的使用方法请参照Demo项目中的异形modbus实现。
		/// </remarks>
		public AlienSession AlienSession { get; set; }

		#endregion

		#region Public Method

		/// <summary>
		/// 在读取数据之前可以调用本方法将客户端设置为长连接模式，相当于跳过了ConnectServer的结果验证，对异形客户端无效，当第一次进行通信时再进行创建连接请求。<br />
		/// Before reading the data, you can call this method to set the client to the long connection mode, which is equivalent to skipping the result verification of ConnectServer, 
		/// and it is invalid for the alien client. When the first communication is performed, the connection creation request is performed.
		/// </summary>
		/// <example>
		/// 以下的方式演示了另一种长连接的机制
		/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Core\NetworkDoubleBase.cs" region="SetPersistentConnectionExample" title="SetPersistentConnection示例" />
		/// </example>
		public void SetPersistentConnection( ) => isPersistentConn = true;

		/// <summary>
		/// 对当前设备的IP地址进行PING的操作，返回PING的结果，正常来说，返回<see cref="IPStatus.Success"/><br />
		/// PING the IP address of the current device and return the PING result. Normally, it returns <see cref="IPStatus.Success"/>
		/// </summary>
		/// <returns>返回PING的结果</returns>
		public IPStatus IpAddressPing( )
		{
#if NET20 || NET35
			Ping ping = new Ping( );
			return ping.Send( IpAddress ).Status;
#else
			return ping.Value.Send( IpAddress ).Status;
#endif
		}

#endregion

#region Connect Close

		/// <summary>
		/// 尝试连接远程的服务器，如果连接成功，就切换短连接模式到长连接模式，后面的每次请求都共享一个通道，使得通讯速度更快速<br />
		/// Try to connect to a remote server. If the connection is successful, switch the short connection mode to the long connection mode. 
		/// Each subsequent request will share a channel, making the communication speed faster.
		/// </summary>
		/// <returns>返回连接结果，如果失败的话（也即IsSuccess为False），包含失败信息</returns>
		/// <example>
		///   简单的连接示例，调用该方法后，连接设备，创建一个长连接的对象，后续的读写操作均公用一个连接对象。
		///   <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Core\NetworkDoubleBase.cs" region="Connect1" title="连接设备" />
		///   如果想知道是否连接成功，请参照下面的代码。
		///   <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Core\NetworkDoubleBase.cs" region="Connect2" title="判断连接结果" />
		/// </example> 
		public OperateResult ConnectServer( )
		{
			isPersistentConn = true;
			// 重新连接之前，先将旧的数据进行清空
			CoreSocket?.Close( );

			OperateResult<Socket> rSocket = CreateSocketAndInitialication( );

			if (!rSocket.IsSuccess)
			{
				IsSocketError = true;
				rSocket.Content = null;
			}
			else
			{
				CoreSocket = rSocket.Content;
				LogNet?.WriteDebug( ToString( ), StringResources.Language.NetEngineStart );
			}
			return rSocket;
		}

		/// <summary>
		/// 使用指定的套接字创建异形客户端，在异形客户端的模式下，网络通道需要被动创建。<br />
		/// Use the specified socket to create the alien client. In the alien client mode, the network channel needs to be created passively.
		/// </summary>
		/// <param name="session">异形客户端对象，查看<seealso cref="NetworkAlienClient"/>类型创建的客户端</param>
		/// <returns>通常都为成功</returns>
		/// <example>
		///   简单的创建示例。
		///   <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Core\NetworkDoubleBase.cs" region="AlienConnect1" title="连接设备" />
		///   如果想知道是否创建成功。通常都是成功。
		///   <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Core\NetworkDoubleBase.cs" region="AlienConnect2" title="判断连接结果" />
		/// </example> 
		/// <remarks>
		/// 不能和之前的长连接和短连接混用，详细参考 Demo程序 
		/// </remarks>
		public OperateResult ConnectServer( AlienSession session )
		{
			isPersistentConn = true;
			isUseSpecifiedSocket = true;

			if (session != null)
			{
				AlienSession?.Socket?.Close( );

				if (string.IsNullOrEmpty( ConnectionId ))
				{
					ConnectionId = session.DTU;
				}

				if (ConnectionId == session.DTU)
				{
					if (session.IsStatusOk)
					{
						OperateResult ini = InitializationOnConnect( session.Socket );
						if (ini.IsSuccess)
						{
							CoreSocket = session.Socket;
							IsSocketError = !session.IsStatusOk;
							AlienSession = session;
						}
						else
						{
							IsSocketError = true;
						}
						return ini;
					}
					else return new OperateResult( );
				}
				else
				{
					IsSocketError = true;
					return new OperateResult( );
				}
			}
			else
			{
				IsSocketError = true;
				return new OperateResult( );
			}
		}

		/// <summary>
		/// 手动断开与远程服务器的连接，如果当前是长连接模式，那么就会切换到短连接模式<br />
		/// Manually disconnect from the remote server, if it is currently in long connection mode, it will switch to short connection mode
		/// </summary>
		/// <returns>关闭连接，不需要查看IsSuccess属性查看</returns>
		/// <example>
		/// 直接关闭连接即可，基本上是不需要进行成功的判定
		/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Core\NetworkDoubleBase.cs" region="ConnectCloseExample" title="关闭连接结果" />
		/// </example>
		public OperateResult ConnectClose( )
		{
			OperateResult result = new OperateResult( );
			isPersistentConn = false;

			InteractiveLock.Enter( );
			try
			{
				// 额外操作
				result = ExtraOnDisconnect( CoreSocket );
				// 关闭信息
				CoreSocket?.Close( );
				CoreSocket = null;
				InteractiveLock.Leave( );
			}
			catch 
			{ 
				InteractiveLock.Leave( ); 
				throw; 
			}

			LogNet?.WriteDebug( ToString( ), StringResources.Language.NetEngineClose );
			return result;
		}

#endregion

#region Initialization And Extra

		/// <summary>
		/// 根据实际的协议选择是否重写本方法，有些协议在创建连接之后，需要进行一些初始化的信号握手，才能最终建立网络通道。<br />
		/// Whether to rewrite this method is based on the actual protocol. Some protocols require some initial signal handshake to establish a network channel after the connection is created.
		/// </summary>
		/// <param name="socket">网络套接字</param>
		/// <returns>是否初始化成功，依据具体的协议进行重写</returns>
		/// <example>
		/// 有些协议不需要握手信号，比如三菱的MC协议，Modbus协议，西门子和欧姆龙就存在握手信息，此处的例子是继承本类后重写的西门子的协议示例
		/// <code lang="cs" source="HslCommunication_Net45\Profinet\Siemens\SiemensS7Net.cs" region="NetworkDoubleBase Override" title="西门子重连示例" />
		/// </example>
		protected virtual OperateResult InitializationOnConnect( Socket socket ) => OperateResult.CreateSuccessResult( );

		/// <summary>
		/// 根据实际的协议选择是否重写本方法，有些协议在断开连接之前，需要发送一些报文来关闭当前的网络通道<br />
		/// Select whether to rewrite this method according to the actual protocol. Some protocols need to send some packets to close the current network channel before disconnecting.
		/// </summary>
		/// <param name="socket">网络套接字</param>
		/// <example>
		/// 目前暂无相关的示例，组件支持的协议都不用实现这个方法。
		/// </example>
		/// <returns>当断开连接时额外的操作结果</returns>
		protected virtual OperateResult ExtraOnDisconnect( Socket socket ) => OperateResult.CreateSuccessResult( );

		/// <summary>
		/// 和服务器交互完成的时候调用的方法，可以根据读写结果进行一些额外的操作，具体的操作需要根据实际的需求来重写实现<br />
		/// The method called when the interaction with the server is completed can perform some additional operations based on the read and write results. 
		/// The specific operations need to be rewritten according to actual needs.
		/// </summary>
		/// <param name="read">读取结果</param>
		protected virtual void ExtraAfterReadFromCoreServer( OperateResult read ) { }

#endregion

#region Account Control

		/************************************************************************************************
		 * 
		 *    这部分的内容是为了实现账户控制的，如果服务器按照hsl协议强制socket账户登录的话，本客户端类就需要额外指定账户密码
		 *    
		 *    The content of this part is for account control. If the server forces the socket account to log in according to the hsl protocol,
		 *    the client class needs to specify the account password.
		 *    
		 *    适用于hsl实现的modbus服务器，三菱及西门子，NetSimplify服务器类等
		 *    
		 *    Modbus server for hsl implementation, Mitsubishi and Siemens, NetSimplify server class, etc.
		 * 
		 ************************************************************************************************/

		/// <summary>
		/// 是否使用账号登录，这个账户登录的功能是<c>HSL</c>组件创建的服务器特有的功能。<br />
		/// Whether to log in using an account. The function of this account login is a server-specific function created by the <c> HSL </c> component.
		/// </summary>
		protected bool isUseAccountCertificate = false;
		private string userName = string.Empty;
		private string password = string.Empty;

		/// <summary>
		/// 设置当前的登录的账户名和密码信息，并启用账户验证的功能，账户名为空时设置不生效<br />
		/// Set the current login account name and password information, and enable the account verification function. The account name setting will not take effect when it is empty
		/// </summary>
		/// <param name="userName">账户名</param>
		/// <param name="password">密码</param>
		public void SetLoginAccount( string userName, string password )
		{
			if (!string.IsNullOrEmpty( userName.Trim( ) ))
			{
				isUseAccountCertificate = true;
				this.userName = userName;
				this.password = password;
			}
			else
			{
				isUseAccountCertificate = false;
			}
		}

		/// <summary>
		/// 认证账号，根据已经设置的用户名和密码，进行发送服务器进行账号认证。<br />
		/// Authentication account, according to the user name and password that have been set, sending server for account authentication.
		/// </summary>
		/// <param name="socket">套接字</param>
		/// <returns>认证结果</returns>
		protected OperateResult AccountCertificate( Socket socket )
		{
			OperateResult send = SendAccountAndCheckReceive( socket, 1, this.userName, this.password );
			if (!send.IsSuccess) return send;

			OperateResult<int, string[]> read = ReceiveStringArrayContentFromSocket( socket );
			if (!read.IsSuccess) return read;

			if (read.Content1 == 0) return new OperateResult( read.Content2[0] );
			return OperateResult.CreateSuccessResult( );
		}

#endregion

#region Async Await Support
#if !NET35 && !NET20
		/// <inheritdoc cref="AccountCertificate(Socket)"/>
		protected async Task<OperateResult> AccountCertificateAsync( Socket socket )
		{
			OperateResult send = await SendAccountAndCheckReceiveAsync( socket, 1, this.userName, this.password );
			if (!send.IsSuccess) return send;

			OperateResult<int, string[]> read = await ReceiveStringArrayContentFromSocketAsync( socket );
			if (!read.IsSuccess) return read;

			if (read.Content1 == 0) return new OperateResult( read.Content2[0] );
			return OperateResult.CreateSuccessResult( );
		}

		/// <inheritdoc cref="InitializationOnConnect(Socket)"/>
#pragma warning disable CS1998 // 异步方法缺少 "await" 运算符，将以同步方式运行
		protected async virtual Task<OperateResult> InitializationOnConnectAsync( Socket socket ) => OperateResult.CreateSuccessResult( );
#pragma warning restore CS1998 // 异步方法缺少 "await" 运算符，将以同步方式运行

		/// <inheritdoc cref="ExtraOnDisconnect(Socket)"/>
#pragma warning disable CS1998 // 异步方法缺少 "await" 运算符，将以同步方式运行
		protected async virtual Task<OperateResult> ExtraOnDisconnectAsync( Socket socket ) => OperateResult.CreateSuccessResult( );
#pragma warning restore CS1998 // 异步方法缺少 "await" 运算符，将以同步方式运行

		/// <inheritdoc cref="CreateSocketAndInitialication"/>
		private async Task<OperateResult<Socket>> CreateSocketAndInitialicationAsync( )
		{
			OperateResult<Socket> result = await CreateSocketAndConnectAsync( new IPEndPoint( IPAddress.Parse( ipAddress ), port ), connectTimeOut, LocalBinding );
			if (result.IsSuccess)
			{
				// 初始化
				OperateResult initi = await InitializationOnConnectAsync( result.Content );
				if (!initi.IsSuccess)
				{
					result.Content?.Close( );
					result.IsSuccess = initi.IsSuccess;
					result.CopyErrorFromOther( initi );
				}
			}
			return result;
		}

		/// <inheritdoc cref="GetAvailableSocket"/>
		protected async Task<OperateResult<Socket>> GetAvailableSocketAsync( )
		{
			if (isPersistentConn)
			{
				// 如果是异形模式
				if (isUseSpecifiedSocket)
				{
					if (IsSocketError)
						return new OperateResult<Socket>( StringResources.Language.ConnectionIsNotAvailable );
					else
						return OperateResult.CreateSuccessResult( CoreSocket );
				}
				else
				{
					// 长连接模式
					if (IsSocketError || CoreSocket == null)
					{
						OperateResult connect = await ConnectServerAsync( );
						if (!connect.IsSuccess)
						{
							IsSocketError = true;
							return OperateResult.CreateFailedResult<Socket>( connect );
						}
						else
						{
							IsSocketError = false;
							return OperateResult.CreateSuccessResult( CoreSocket );
						}
					}
					else
						return OperateResult.CreateSuccessResult( CoreSocket );
				}
			}
			else
				return await CreateSocketAndInitialicationAsync( );
		}

		/// <inheritdoc cref="ConnectServer()"/>
		public async Task<OperateResult> ConnectServerAsync( )
		{
			isPersistentConn = true;
			// 重新连接之前，先将旧的数据进行清空
			CoreSocket?.Close( );

			OperateResult<Socket> rSocket = await CreateSocketAndInitialicationAsync( );

			if (!rSocket.IsSuccess)
			{
				IsSocketError = true;
				rSocket.Content = null;
				return rSocket;
			}
			else
			{
				CoreSocket = rSocket.Content;
				LogNet?.WriteDebug( ToString( ), StringResources.Language.NetEngineStart );
				return rSocket;
			}
		}

		/// <inheritdoc cref="ConnectClose"/>
		public async Task<OperateResult> ConnectCloseAsync( )
		{
			OperateResult result = new OperateResult( );
			isPersistentConn = false;

			await Task.Run( new Action( ( ) => InteractiveLock.Enter( ) ) );
			try
			{
				// 额外操作
				result = await ExtraOnDisconnectAsync( CoreSocket );
				// 关闭信息
				CoreSocket?.Close( );
				CoreSocket = null;
				InteractiveLock.Leave( );
			}
			catch
			{
				InteractiveLock.Leave( );
				throw;
			}

			LogNet?.WriteDebug( ToString( ), StringResources.Language.NetEngineClose );
			return result;
		}

		/// <inheritdoc cref="ReadFromCoreServer(Socket, byte[], bool, bool)"/>
		public virtual async Task<OperateResult<byte[]>> ReadFromCoreServerAsync( Socket socket, byte[] send, bool hasResponseData = true, bool usePackAndUnpack = true )
		{
			byte[] sendValue = usePackAndUnpack ? PackCommandWithHeader( send ) : send;
			LogNet?.WriteDebug( ToString( ), StringResources.Language.Send + " : " + (LogMsgFormatBinary ? sendValue.ToHexString( ' ' ) : Encoding.ASCII.GetString( sendValue )) );

			INetMessage netMessage = GetNewNetMessage( );
			if (netMessage != null) netMessage.SendBytes = sendValue;

			// send
			OperateResult sendResult = await SendAsync( socket, sendValue );
			if (!sendResult.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( sendResult );
			if (receiveTimeOut < 0)    return OperateResult.CreateSuccessResult( new byte[0] );
			if (!hasResponseData)      return OperateResult.CreateSuccessResult( new byte[0] );
			if (SleepTime > 0) Thread.Sleep( SleepTime );

			// receive msg
			OperateResult<byte[]> resultReceive = await ReceiveByMessageAsync( socket, receiveTimeOut, netMessage );
			if (!resultReceive.IsSuccess) return resultReceive;

			LogNet?.WriteDebug( ToString( ), StringResources.Language.Receive + " : " + (LogMsgFormatBinary ? resultReceive.Content.ToHexString( ' ' ) : Encoding.ASCII.GetString( resultReceive.Content )) );

			// check
			if (netMessage != null && !netMessage.CheckHeadBytesLegal( Token.ToByteArray( ) ))
			{
				socket?.Close( );
				return new OperateResult<byte[]>( StringResources.Language.CommandHeadCodeCheckFailed + Environment.NewLine +
					StringResources.Language.Send + ": " + SoftBasic.ByteToHexString( sendValue, ' ' ) + Environment.NewLine +
					StringResources.Language.Receive + ": " + SoftBasic.ByteToHexString( resultReceive.Content, ' ' ) );
			}

			// extra check
			return usePackAndUnpack ? UnpackResponseContent( sendValue, resultReceive.Content ) : resultReceive;
		}

		/// <inheritdoc cref="ReadFromCoreServer(byte[], bool, bool)"/>
		public async Task<OperateResult<byte[]>> ReadFromCoreServerAsync( byte[] send ) => await ReadFromCoreServerAsync( send, true );

		/// <inheritdoc cref="ReadFromCoreServer(byte[], bool, bool)"/>
		public async Task<OperateResult<byte[]>> ReadFromCoreServerAsync( byte[] send, bool hasResponseData, bool usePackAndUnpack = true )
		{
			var result = new OperateResult<byte[]>( );
			OperateResult<Socket> resultSocket = null;
			await Task.Run( new Action( ( ) => InteractiveLock.Enter( ) ) );

			try
			{
				// 获取有用的网络通道，如果没有，就建立新的连接
				resultSocket = await GetAvailableSocketAsync( );
				if (!resultSocket.IsSuccess)
				{
					IsSocketError = true;
					AlienSession?.Offline( );
					InteractiveLock.Leave( );
					result.CopyErrorFromOther( resultSocket );
					return result;
				}

				OperateResult<byte[]> read = await ReadFromCoreServerAsync( resultSocket.Content, send, hasResponseData, usePackAndUnpack );

				if (read.IsSuccess)
				{
					IsSocketError = false;
					result.IsSuccess = read.IsSuccess;
					result.Content = read.Content;
					result.Message = StringResources.Language.SuccessText;
				}
				else
				{
					IsSocketError = true;
					AlienSession?.Offline( );
					result.CopyErrorFromOther( read );
				}

				ExtraAfterReadFromCoreServer( read );
				InteractiveLock.Leave( );
			}
			catch
			{
				InteractiveLock.Leave( );
				throw;
			}

			if (!isPersistentConn) resultSocket?.Content?.Close( );
			return result;
		}
#endif
		#endregion

		#region Core Communication

		/// <summary>
		/// 对当前的命令进行打包处理，通常是携带命令头内容，标记当前的命令的长度信息，需要进行重写，否则默认不打包<br />
		/// The current command is packaged, usually carrying the content of the command header, marking the length of the current command, 
		/// and it needs to be rewritten, otherwise it is not packaged by default
		/// </summary>
		/// <param name="command">发送的数据命令内容</param>
		/// <returns>打包之后的数据结果信息</returns>
		protected virtual byte[] PackCommandWithHeader( byte[] command ) => command;

		/// <summary>
		/// 根据对方返回的报文命令，对命令进行基本的拆包，例如各种Modbus协议拆包为统一的核心报文，还支持对报文的验证<br />
		/// According to the message command returned by the other party, the command is basically unpacked, for example, 
		/// various Modbus protocols are unpacked into a unified core message, and the verification of the message is also supported
		/// </summary>
		/// <param name="send">发送的原始报文数据</param>
		/// <param name="response">设备方反馈的原始报文内容</param>
		/// <returns>返回拆包之后的报文信息，默认不进行任何的拆包操作</returns>
		protected virtual OperateResult<byte[]> UnpackResponseContent( byte[] send, byte[] response ) => OperateResult.CreateSuccessResult( response );

		/***************************************************************************************
		 * 
		 *    主要的数据交互分为4步
		 *    1. 连接服务器，或是获取到旧的使用的网络信息
		 *    2. 发送数据信息
		 *    3. 接收反馈的数据信息
		 *    4. 关闭网络连接，如果是短连接的话
		 * 
		 **************************************************************************************/

		/// <summary>
		/// 获取本次操作的可用的网络通道，如果是短连接，就重新生成一个新的网络通道，如果是长连接，就复用当前的网络通道。<br />
		/// Obtain the available network channels for this operation. If it is a short connection, a new network channel is regenerated. 
		/// If it is a long connection, the current network channel is reused.
		/// </summary>
		/// <returns>是否成功，如果成功，使用这个套接字</returns>
		protected OperateResult<Socket> GetAvailableSocket( )
		{
			if (isPersistentConn)
			{
				// 如果是异形模式
				if (isUseSpecifiedSocket)
				{
					if (IsSocketError)
						return new OperateResult<Socket>( StringResources.Language.ConnectionIsNotAvailable );
					else
						return OperateResult.CreateSuccessResult( CoreSocket );
				}
				else
				{
					// 长连接模式
					if (IsSocketError || CoreSocket == null)
					{
						OperateResult connect = ConnectServer( );
						if (!connect.IsSuccess)
						{
							IsSocketError = true;
							return OperateResult.CreateFailedResult<Socket>( connect );
						}
						else
						{
							IsSocketError = false;
							return OperateResult.CreateSuccessResult( CoreSocket );
						}
					}
					else
					{
						return OperateResult.CreateSuccessResult( CoreSocket );
					}
				}
			}
			else
			{
				// 短连接模式
				return CreateSocketAndInitialication( );
			}
		}

		/// <summary>
		/// 尝试连接服务器，如果成功，并执行<see cref="InitializationOnConnect(Socket)"/>的初始化方法，并返回最终的结果。<br />
		/// Attempt to connect to the server, if successful, and execute the initialization method of <see cref = "InitializationOnConnect (Socket)" />, and return the final result.
		/// </summary>
		/// <returns>带有socket的结果对象</returns>
		private OperateResult<Socket> CreateSocketAndInitialication( )
		{
			OperateResult<Socket> result = CreateSocketAndConnect( new IPEndPoint( IPAddress.Parse( ipAddress ), port ), connectTimeOut, LocalBinding );
			if (result.IsSuccess)
			{
				// 初始化
				OperateResult initi = InitializationOnConnect( result.Content );
				if (!initi.IsSuccess)
				{
					result.Content?.Close( );
					result.IsSuccess = initi.IsSuccess;
					result.CopyErrorFromOther( initi );
				}
			}
			return result;
		}

		/// <summary>
		/// 将数据报文发送指定的网络通道上，根据当前指定的<see cref="INetMessage"/>类型，返回一条完整的数据指令<br />
		/// Sends a data message to the specified network channel, and returns a complete data command according to the currently specified <see cref = "INetMessage" /> type
		/// </summary>
		/// <param name="socket">指定的套接字</param>
		/// <param name="send">发送的完整的报文信息</param>
		/// <param name="hasResponseData">是否有等待的数据返回，默认为 true</param>
		/// <param name="usePackAndUnpack">是否需要对命令重新打包，在重写<see cref="PackCommandWithHeader(byte[])"/>方法后才会有影响</param>
		/// <remarks>
		/// 无锁的基于套接字直接进行叠加协议的操作。
		/// </remarks>
		/// <example>
		/// 假设你有一个自己的socket连接了设备，本组件可以直接基于该socket实现modbus读取，三菱读取，西门子读取等等操作，前提是该服务器支持多协议，虽然这个需求听上去比较变态，但本组件支持这样的操作。
		/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Core\NetworkDoubleBase.cs" region="ReadFromCoreServerExample1" title="ReadFromCoreServer示例" />
		/// </example>
		/// <returns>接收的完整的报文信息</returns>
		public virtual OperateResult<byte[]> ReadFromCoreServer( Socket socket, byte[] send, bool hasResponseData = true, bool usePackAndUnpack = true )
		{
			byte[] sendValue = usePackAndUnpack ? PackCommandWithHeader( send ) : send;
			LogNet?.WriteDebug( ToString( ), StringResources.Language.Send + " : " + (LogMsgFormatBinary ? sendValue.ToHexString( ' ' ) : Encoding.ASCII.GetString( sendValue )) );

			INetMessage netMessage = GetNewNetMessage( );
			if (netMessage != null) netMessage.SendBytes = sendValue;

			// send
			OperateResult sendResult = Send( socket, sendValue );
			if (!sendResult.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( sendResult );
			if (receiveTimeOut < 0)    return OperateResult.CreateSuccessResult( new byte[0] );
			if (!hasResponseData)      return OperateResult.CreateSuccessResult( new byte[0] );
			if (SleepTime > 0) Thread.Sleep( SleepTime );

			// receive msg
			OperateResult<byte[]> resultReceive = ReceiveByMessage( socket, receiveTimeOut, netMessage );
			if (!resultReceive.IsSuccess) return resultReceive;

			LogNet?.WriteDebug( ToString( ), StringResources.Language.Receive + " : " + (LogMsgFormatBinary ? resultReceive.Content.ToHexString( ' ' ) : Encoding.ASCII.GetString( resultReceive.Content )) );

			// check
			if (netMessage != null && !netMessage.CheckHeadBytesLegal( Token.ToByteArray( ) ))
			{
				socket?.Close( );
				return new OperateResult<byte[]>( StringResources.Language.CommandHeadCodeCheckFailed + Environment.NewLine +
					StringResources.Language.Send + ": " + SoftBasic.ByteToHexString( sendValue, ' ' ) + Environment.NewLine +
					StringResources.Language.Receive + ": " + SoftBasic.ByteToHexString( resultReceive.Content, ' ' ) );
			}

			// extro check
			return usePackAndUnpack ? UnpackResponseContent( sendValue, resultReceive.Content ) : OperateResult.CreateSuccessResult( resultReceive.Content );
		}

		/// <inheritdoc cref="ReadFromCoreServer(byte[], bool, bool)"/>
		public OperateResult<byte[]> ReadFromCoreServer( byte[] send ) => ReadFromCoreServer( send, true, true);

		/// <summary>
		/// 将数据发送到当前的网络通道中，并从网络通道中接收一个<see cref="INetMessage"/>指定的完整的报文，网络通道将根据<see cref="GetAvailableSocket"/>方法自动获取，本方法是线程安全的。<br />
		/// Send data to the current network channel and receive a complete message specified by <see cref = "INetMessage" /> from the network channel. 
		/// The network channel will be automatically obtained according to the <see cref = "GetAvailableSocket" /> method This method is thread-safe.
		/// </summary>
		/// <param name="send">发送的完整的报文信息</param>
		/// <param name="hasResponseData">是否有等待的数据返回，默认为 true</param>
		/// <param name="usePackAndUnpack">是否需要对命令重新打包，在重写<see cref="PackCommandWithHeader(byte[])"/>方法后才会有影响</param>
		/// <returns>接收的完整的报文信息</returns>
		/// <remarks>
		/// 本方法用于实现本组件还未实现的一些报文功能，例如有些modbus服务器会有一些特殊的功能码支持，需要收发特殊的报文，详细请看示例
		/// </remarks>
		/// <example>
		/// 此处举例有个modbus服务器，有个特殊的功能码0x09，后面携带子数据0x01即可，发送字节为 0x00 0x00 0x00 0x00 0x00 0x03 0x01 0x09 0x01
		/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Core\NetworkDoubleBase.cs" region="ReadFromCoreServerExample2" title="ReadFromCoreServer示例" />
		/// </example>
		public OperateResult<byte[]> ReadFromCoreServer( byte[] send, bool hasResponseData, bool usePackAndUnpack = true )
		{
			var result = new OperateResult<byte[]>( );
			OperateResult<Socket> resultSocket = null;

			InteractiveLock.Enter( );
			try
			{
				// 获取有用的网络通道，如果没有，就建立新的连接
				resultSocket = GetAvailableSocket( );
				if (!resultSocket.IsSuccess)
				{
					IsSocketError = true;
					AlienSession?.Offline( );
					InteractiveLock.Leave( );
					result.CopyErrorFromOther( resultSocket );
					return result;
				}

				OperateResult<byte[]> read = ReadFromCoreServer( resultSocket.Content, send, hasResponseData, usePackAndUnpack );

				if (read.IsSuccess)
				{
					IsSocketError = false;
					result.IsSuccess = read.IsSuccess;
					result.Content = read.Content;
					result.Message = StringResources.Language.SuccessText;
				}
				else
				{
					IsSocketError = true;
					AlienSession?.Offline( );
					result.CopyErrorFromOther( read );
				}

				ExtraAfterReadFromCoreServer( read );
				InteractiveLock.Leave( );
			}
			catch
			{
				InteractiveLock.Leave( );
				throw;
			}

			if (!isPersistentConn) resultSocket?.Content?.Close( );
			return result;
		}

#endregion

#region IDisposable Support

		private bool disposedValue = false; // 要检测冗余调用

		/// <summary>
		/// 释放当前的资源，并自动关闭长连接，如果设置了的话
		/// </summary>
		/// <param name="disposing">是否释放托管的资源信息</param>
		protected virtual void Dispose( bool disposing )
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					// TODO: 释放托管状态(托管对象)。
					ConnectClose( );
					InteractiveLock?.Dispose( );
				}

				// TODO: 释放未托管的资源(未托管的对象)并在以下内容中替代终结器。
				// TODO: 将大型字段设置为 null。

				disposedValue = true;
			}
		}

		// TODO: 仅当以上 Dispose(bool disposing) 拥有用于释放未托管资源的代码时才替代终结器。
		// ~NetworkDoubleBase()
		// {
		//   // 请勿更改此代码。将清理代码放入以上 Dispose(bool disposing) 中。
		//   Dispose(false);
		// }

		// 添加此代码以正确实现可处置模式。
		/// <summary>
		/// 释放当前的资源
		/// </summary>
		public void Dispose( )
		{
			// 请勿更改此代码。将清理代码放入以上 Dispose(bool disposing) 中。
			Dispose( true );
			// TODO: 如果在以上内容中替代了终结器，则取消注释以下行。
			// GC.SuppressFinalize(this);
		}
#endregion

#region Private Member

#if !NET35 && !NET20
		private Lazy<Ping> ping = new Lazy<Ping>( ( ) => new Ping( ) );
#endif

#endregion

#region Object Override

		/// <inheritdoc/>
		public override string ToString( ) => $"NetworkDoubleBase<{ GetNewNetMessage( ).GetType( )}, { ByteTransform.GetType( ) }>[{IpAddress}:{Port}]";

#endregion
	}
}
