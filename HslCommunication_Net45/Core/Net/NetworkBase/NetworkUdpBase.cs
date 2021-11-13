using HslCommunication.BasicFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using HslCommunication.Reflection;
#if !NET35 && !NET20
using System.Threading.Tasks;
#endif

namespace HslCommunication.Core.Net
{
	/// <summary>
	/// 基于Udp的应答式通信类<br />
	/// Udp - based responsive communication class
	/// </summary>
	public class NetworkUdpBase : NetworkBase
	{
		#region Constructor

		/// <summary>
		/// 实例化一个默认的方法<br />
		/// Instantiate a default method
		/// </summary>
		public NetworkUdpBase( )
		{
			hybirdLock = new SimpleHybirdLock( );                                          // 当前交互的数据锁
			ReceiveTimeout = 5000;                                                         // 当前接收的超时时间
			ConnectionId = SoftBasic.GetUniqueStringByGuidAndRandom( );                    // 设备的唯一的编号
		}

		#endregion

		#region Public Properties

		/// <inheritdoc cref="NetworkDoubleBase.IpAddress"/>
		public virtual string IpAddress
		{
			get => ipAddress;
			set
			{
				ipAddress = HslHelper.GetIpAddressFromInput( value );
			}
		}

		/// <inheritdoc cref="NetworkDoubleBase.Port"/>
		public virtual int Port { get; set; }

		/// <inheritdoc cref="NetworkDoubleBase.ReceiveTimeOut"/>
		public int ReceiveTimeout { get; set; }

		/// <inheritdoc cref="NetworkDoubleBase.ConnectionId"/>
		public string ConnectionId { get; set; }

		/// <summary>
		/// 获取或设置一次接收时的数据长度，默认2KB数据长度，特殊情况的时候需要调整<br />
		/// Gets or sets the length of data received at a time. The default length is 2KB
		/// </summary>
		public int ReceiveCacheLength { get; set; } = 2048;

		/// <inheritdoc cref="NetworkDoubleBase.LocalBinding"/>
		public IPEndPoint LocalBinding { get; set; }

		/// <inheritdoc cref="NetworkDoubleBase.LogMsgFormatBinary"/>
		protected bool LogMsgFormatBinary = true;

		#endregion

		#region Core Read

		/// <inheritdoc cref="NetworkDoubleBase.PackCommandWithHeader(byte[])"/>
		protected virtual byte[] PackCommandWithHeader( byte[] command ) => command;

		/// <inheritdoc cref="NetworkDoubleBase.UnpackResponseContent(byte[], byte[])"/>
		protected virtual OperateResult<byte[]> UnpackResponseContent( byte[] send, byte[] response ) => OperateResult.CreateSuccessResult( response );

		/// <inheritdoc cref="ReadFromCoreServer(byte[], bool, bool)"/>
		public virtual OperateResult<byte[]> ReadFromCoreServer( byte[] send )
		{
			return ReadFromCoreServer( send, true, true );
		}

		/// <summary>
		/// 核心的数据交互读取，发数据发送到通道上去，然后从通道上接收返回的数据<br />
		/// The core data is read interactively, the data is sent to the serial port, and the returned data is received from the serial port
		/// </summary>
		/// <param name="send">完整的报文内容</param>
		/// <param name="hasResponseData">是否有等待的数据返回，默认为 true</param>
		/// <param name="usePackAndUnpack">是否需要对命令重新打包，在重写<see cref="PackCommandWithHeader(byte[])"/>方法后才会有影响</param>
		/// <returns>是否成功的结果对象</returns>
		public virtual OperateResult<byte[]> ReadFromCoreServer( byte[] send, bool hasResponseData, bool usePackAndUnpack )
		{
			if (!Authorization.nzugaydgwadawdibbas( )) return new OperateResult<byte[]>( StringResources.Language.AuthorizationFailed );

			byte[] sendValue = usePackAndUnpack ? PackCommandWithHeader( send ) : send;
			LogNet?.WriteDebug( ToString( ), StringResources.Language.Send + " : " +
				(LogMsgFormatBinary ? SoftBasic.ByteToHexString( sendValue ) : Encoding.ASCII.GetString( sendValue )) );
			hybirdLock.Enter( );
			try
			{
				IPEndPoint endPoint = new IPEndPoint( IPAddress.Parse( IpAddress ), Port );
				Socket server = new Socket( AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp );
				if (LocalBinding != null) server.Bind( LocalBinding );

				server.SendTo( sendValue, sendValue.Length, SocketFlags.None, endPoint );

				if (ReceiveTimeout < 0)
				{
					hybirdLock.Leave( );
					return OperateResult.CreateSuccessResult( new byte[0] );
				}

				if (!hasResponseData)
				{
					hybirdLock.Leave( );
					return OperateResult.CreateSuccessResult( new byte[0] );
				}

				// 对于不存在的IP地址，加入此行代码后，可以在指定时间内解除阻塞模式限制
				server.SetSocketOption( SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, ReceiveTimeout );
				IPEndPoint sender = new IPEndPoint( IPAddress.Any, 0 );
				EndPoint Remote = (EndPoint)sender;
				byte[] buffer = new byte[ReceiveCacheLength];
				int recv = server.ReceiveFrom( buffer, ref Remote );
				byte[] receive = buffer.SelectBegin( recv );

				hybirdLock.Leave( );
				LogNet?.WriteDebug( ToString( ), StringResources.Language.Receive + " : " +
					(LogMsgFormatBinary ? SoftBasic.ByteToHexString( receive ) : Encoding.ASCII.GetString( receive )) );
				connectErrorCount = 0;

				return usePackAndUnpack ? UnpackResponseContent( sendValue, receive ) : OperateResult.CreateSuccessResult( receive );
			}
			catch (Exception ex)
			{
				hybirdLock.Leave( );
				if (connectErrorCount < 1_0000_0000) connectErrorCount++;
				return new OperateResult<byte[]>( -connectErrorCount, ex.Message );
			}
		}

#if !NET20 && !NET35
		/// <inheritdoc cref="ReadFromCoreServer(byte[], bool, bool)"/>
		public async Task<OperateResult<byte[]>> ReadFromCoreServerAsync( byte[] value )
		{
			return await Task.Run( ( ) => ReadFromCoreServer( value ) );
		}
#endif
		#endregion

		#region Public Method

		/// <inheritdoc cref="NetworkDoubleBase.IpAddressPing"/>
		public IPStatus IpAddressPing( )
		{
			Ping ping = new Ping( );
			return ping.Send( IpAddress ).Status;
		}

		#endregion

		#region Private Member

		private SimpleHybirdLock hybirdLock = null;       // 数据锁
		private int connectErrorCount = 0;                // 连接错误次数
		private string ipAddress = "127.0.0.1";           // ip地址信息

		#endregion

		#region Object Override

		/// <inheritdoc/>
		public override string ToString( ) => $"NetworkUdpBase[{IpAddress}:{Port}]";

		#endregion
	}
}
