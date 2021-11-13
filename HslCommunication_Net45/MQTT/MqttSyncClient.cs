using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HslCommunication.Core.Net;
using HslCommunication.Core.IMessage;
using HslCommunication.Core;
using System.Net.Sockets;
using HslCommunication.BasicFramework;
using System.Net;
using HslCommunication.Profinet.Panasonic;
using HslCommunication.Reflection;
using Newtonsoft.Json.Linq;
using System.IO;
using HslCommunication.Enthernet;
using System.Security.Cryptography;
using HslCommunication.Core.Security;
#if NET20 || NET35 || NET451
using System.Drawing;
#endif
#if !NET35 && !NET20
using System.Threading.Tasks;
#endif

namespace HslCommunication.MQTT
{
	/// <summary>
	/// 基于MQTT协议的同步访问的客户端程序，支持以同步的方式访问服务器的数据信息，并及时的反馈结果，当服务器启动文件功能时，也支持文件的上传，下载，删除操作等。<br />
	/// The client program based on MQTT protocol for synchronous access supports synchronous access to the server's data information and timely feedback of results,
	/// When the server starts the file function, it also supports file upload, download, and delete operations.
	/// </summary>
	/// <remarks>
	/// 在最新的V10.2.0及以上版本中，本客户端支持加密模式，启用加密模式后，就无法通过抓包的报文来分析出用户名密码，以及通信的数据细节，详细可以参考API文档。
	/// </remarks>
	/// <example>
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\MQTT\MqttSyncClientSample.cs" region="Test" title="简单的实例化" />
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\MQTT\MqttSyncClientSample.cs" region="Test2" title="带用户名密码的实例化" />
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\MQTT\MqttSyncClientSample.cs" region="Test3" title="连接示例" />
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\MQTT\MqttSyncClientSample.cs" region="Test4" title="读取数据示例" />
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\MQTT\MqttSyncClientSample.cs" region="Test5" title="带进度报告示例" />
	/// 当MqttServer注册了远程RPC接口的时候，例如将一个plc对象注册是接口对象，或是自定义的接口内容
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\MQTT\MqttSyncClientSample.cs" region="Test11" title="RPC接口读取" />
	/// 服务器都有什么RPC接口呢？可以通过下面的方式知道
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\MQTT\MqttSyncClientSample.cs" region="Test12" title="RPC接口列表" />
	/// 关于加密模式，在不加密的情况下，用户名及密码，还有请求的数据信息会被第三方软件窃取，从而泄露一些关键的数据信息，如果使用了HslCommunicationV10.2.0版本以上创建的MQTTServer，
	/// 那么可以在客户端使用加密模式，加密使用RSA+AES混合加密，密钥动态生成，在保证效率的同时，具有很高的安全性。客户端使用加密如下：
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\MQTT\MqttSyncClientSample.cs" region="Test13" title="加密举例" />
	/// 下面演示文件部分的功能的接口方法，主要包含，上传，下载，删除，遍历操作
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\MQTT\MqttSyncClientSample.cs" region="Test6" title="下载文件功能" />
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\MQTT\MqttSyncClientSample.cs" region="Test7" title="上传文件功能" />
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\MQTT\MqttSyncClientSample.cs" region="Test8" title="删除文件功能" />
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\MQTT\MqttSyncClientSample.cs" region="Test9" title="遍历指定目录的文件名功能" />
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\MQTT\MqttSyncClientSample.cs" region="Test10" title="遍历指定目录的所有子目录" />
	/// 上述的两个遍历的方法，就可以遍历出服务器的所有目录和文件了，具体可以参考 Demo 的MQTT文件客户端的演示界面。
	/// </example>
	public class MqttSyncClient : NetworkDoubleBase
	{
		#region Constructor

		/// <summary>
		/// 实例化一个MQTT的同步客户端<br />
		/// Instantiate an MQTT synchronization client
		/// </summary>
		/// <param name="options">连接的参数信息，可以指定IP地址，端口，账户名，密码，客户端ID信息</param>
		public MqttSyncClient( MqttConnectionOptions options )
		{
			this.ByteTransform     = new RegularByteTransform( );
			this.connectionOptions = options;
			this.IpAddress         = options.IpAddress;
			this.Port              = options.Port;
			this.incrementCount    = new SoftIncrementCount( ushort.MaxValue, 1 );
			this.ConnectTimeOut    = options.ConnectTimeout;
			this.receiveTimeOut    = 60_000;
		}

		/// <summary>
		/// 通过指定的ip地址及端口来实例化一个同步的MQTT客户端<br />
		/// Instantiate a synchronized MQTT client with the specified IP address and port
		/// </summary>
		/// <param name="ipAddress">IP地址信息</param>
		/// <param name="port">端口号信息</param>
		public MqttSyncClient( string ipAddress, int port )
		{
			this.connectionOptions = new MqttConnectionOptions( ) { 
				IpAddress = ipAddress,
				Port = port
			};
			this.ByteTransform     = new RegularByteTransform( );
			this.IpAddress         = ipAddress;
			this.Port              = port;
			this.incrementCount    = new SoftIncrementCount( ushort.MaxValue, 1 );
			this.receiveTimeOut    = 60_000;
		}

		/// <summary>
		/// 通过指定的ip地址及端口来实例化一个同步的MQTT客户端<br />
		/// Instantiate a synchronized MQTT client with the specified IP address and port
		/// </summary>
		/// <param name="ipAddress">IP地址信息</param>
		/// <param name="port">端口号信息</param>
		public MqttSyncClient( IPAddress ipAddress, int port )
		{
			this.connectionOptions = new MqttConnectionOptions( )
			{
				IpAddress = ipAddress.ToString( ),
				Port = port
			};
			this.ByteTransform = new RegularByteTransform( );
			this.IpAddress = ipAddress.ToString( );
			this.Port = port;
			this.incrementCount = new SoftIncrementCount( ushort.MaxValue, 1 );
		}

		#endregion

		#region InitializationOnConnect

		private OperateResult InitializationMqttSocket( Socket socket, string protocol )
		{
			// 连接对象加密处理，和服务器进行交换密钥处理
			RSACryptoServiceProvider rsa = null;
			if (this.connectionOptions.UseRSAProvider)
			{
				cryptoServiceProvider = new RSACryptoServiceProvider( );

				OperateResult sendKey = Send( socket, MqttHelper.BuildMqttCommand( 0xFF, null, HslSecurity.ByteEncrypt( cryptoServiceProvider.GetPEMPublicKey( ) ) ).Content );
				if (!sendKey.IsSuccess) return sendKey;

				OperateResult<byte, byte[]> key = ReceiveMqttMessage( socket, ReceiveTimeOut );
				if (!key.IsSuccess) return key;

				try
				{
					byte[] serverPublicToken = cryptoServiceProvider.DecryptLargeData( HslSecurity.ByteDecrypt( key.Content2 ) );
					rsa = RSAHelper.CreateRsaProviderFromPublicKey( serverPublicToken );
				}
				catch (Exception ex)
				{
					socket?.Close( );
					return new OperateResult( "RSA check failed: " + ex.Message );
				}
			}

			OperateResult<byte[]> command = MqttHelper.BuildConnectMqttCommand( this.connectionOptions, protocol, rsa );
			if (!command.IsSuccess) return command;

			// 发送连接的报文信息
			OperateResult send = Send( socket, command.Content );
			if (!send.IsSuccess) return send;

			// 接收服务器端注册返回的报文信息
			OperateResult<byte, byte[]> receive = ReceiveMqttMessage( socket, ReceiveTimeOut );
			if (!receive.IsSuccess) return receive;

			// 检查连接的返回状态是否正确
			OperateResult check = MqttHelper.CheckConnectBack( receive.Content1, receive.Content2 );
			if (!check.IsSuccess) { socket?.Close( ); return check; }

			if (this.connectionOptions.UseRSAProvider)
			{
				string key = Encoding.UTF8.GetString( cryptoServiceProvider.Decrypt( receive.Content2.RemoveBegin( 2 ), false ) );
				this.aesCryptography = new AesCryptography( key );
			}

			return OperateResult.CreateSuccessResult( );
		}

		/// <inheritdoc/>
		protected override OperateResult InitializationOnConnect( Socket socket )
		{
			OperateResult ini = InitializationMqttSocket( socket, "HUSL" );
			if (!ini.IsSuccess) return ini;

			this.incrementCount.ResetCurrentValue( );          // 重置消息计数
			return OperateResult.CreateSuccessResult( );
		}

		#endregion

		#region InitializationOnConnect Async
#if !NET35 && !NET20

		private async Task<OperateResult> InitializationMqttSocketAsync( Socket socket, string protocol )
		{
			// 连接对象加密处理，和服务器进行交换密钥处理
			RSACryptoServiceProvider rsa = null;
			if (this.connectionOptions.UseRSAProvider)
			{
				cryptoServiceProvider = new RSACryptoServiceProvider( );

				OperateResult sendKey = await SendAsync( socket, MqttHelper.BuildMqttCommand( 0xFF, null, HslSecurity.ByteEncrypt( cryptoServiceProvider.GetPEMPublicKey( ) ) ).Content );
				if (!sendKey.IsSuccess) return sendKey;

				OperateResult<byte, byte[]> key = await ReceiveMqttMessageAsync( socket, ReceiveTimeOut );
				if (!key.IsSuccess) return key;

				try
				{
					byte[] serverPublicToken = cryptoServiceProvider.DecryptLargeData( HslSecurity.ByteDecrypt( key.Content2 ) );
					rsa = RSAHelper.CreateRsaProviderFromPublicKey( serverPublicToken );
				}
				catch (Exception ex)
				{
					socket?.Close( );
					return new OperateResult( "RSA check failed: " + ex.Message );
				}
			}

			OperateResult<byte[]> command = MqttHelper.BuildConnectMqttCommand( this.connectionOptions, protocol, rsa );
			if (!command.IsSuccess) return command;

			// 发送连接的报文信息
			OperateResult send = await SendAsync( socket, command.Content );
			if (!send.IsSuccess) return send;

			// 接收服务器端注册返回的报文信息
			OperateResult<byte, byte[]> receive = await ReceiveMqttMessageAsync( socket, ReceiveTimeOut );
			if (!receive.IsSuccess) return receive;

			// 检查连接的返回状态是否正确
			OperateResult check = MqttHelper.CheckConnectBack( receive.Content1, receive.Content2 );
			if (!check.IsSuccess) { socket?.Close( ); return check; }

			if (this.connectionOptions.UseRSAProvider)
			{
				string key = Encoding.UTF8.GetString( cryptoServiceProvider.Decrypt( receive.Content2.RemoveBegin( 2 ), false ) );
				this.aesCryptography = new AesCryptography( key );
			}

			return OperateResult.CreateSuccessResult( );
		}

		/// <inheritdoc/>
		protected async override Task<OperateResult> InitializationOnConnectAsync( Socket socket )
		{
			OperateResult ini = await InitializationMqttSocketAsync( socket, "HUSL" );
			if (!ini.IsSuccess) return ini;

			this.incrementCount.ResetCurrentValue( );          // 重置消息计数
			return OperateResult.CreateSuccessResult( );
		}
#endif
		#endregion

		#region NetworkDoubleBase Override

		/// <inheritdoc/>
		public override OperateResult<byte[]> ReadFromCoreServer( Socket socket, byte[] send, bool hasResponseData = true, bool usePackHeader = true )
		{
			var read = ReadMqttFromCoreServer( socket, send, null, null, null );
			if (read.IsSuccess) return OperateResult.CreateSuccessResult( read.Content2 );

			return OperateResult.CreateFailedResult<byte[]>( read );
		}

		private OperateResult<byte, byte[]> ReadMqttFromCoreServer( Socket socket, byte[] send, Action<long, long> sendProgress, Action<string, string> handleProgress, Action<long, long> receiveProgress )
		{
			OperateResult sendResult = Send( socket, send );
			if (!sendResult.IsSuccess) return OperateResult.CreateFailedResult<byte, byte[]>( sendResult );

			// 先确认对方是否接收完数据
			while (true)
			{
				OperateResult<byte, byte[]> server_receive = ReceiveMqttMessage( socket, ReceiveTimeOut );
				if (!server_receive.IsSuccess) return server_receive;

				OperateResult<string, byte[]> server_back = MqttHelper.ExtraMqttReceiveData( server_receive.Content1, server_receive.Content2 );
				if (!server_back.IsSuccess) return OperateResult.CreateFailedResult<byte, byte[]>( server_back );

				if (server_back.Content2.Length != 16) return new OperateResult<byte, byte[]>( StringResources.Language.ReceiveDataLengthTooShort );
				long already = BitConverter.ToInt64( server_back.Content2, 0 );
				long total = BitConverter.ToInt64( server_back.Content2, 8 );
				sendProgress?.Invoke( already, total );
				if (already == total) break;
			}

			// 如果接收到进度报告，就继续接收，直到不是进度报告的数据为止
			while (true)
			{
				OperateResult<byte, byte[]> receive = ReceiveMqttMessage( socket, ReceiveTimeOut, receiveProgress );
				if (!receive.IsSuccess) return receive;

				if (receive.Content1 >> 4 == MqttControlMessage.REPORTPROGRESS)
				{
					OperateResult<string, byte[]> extra = MqttHelper.ExtraMqttReceiveData( receive.Content1, receive.Content2 );
					handleProgress?.Invoke( extra.Content1, Encoding.UTF8.GetString( extra.Content2 ) );
				}
				else
				{
					return OperateResult.CreateSuccessResult( receive.Content1, receive.Content2 );
				}
			}
		}

		private OperateResult<byte[]> ReadMqttFromCoreServer( byte control, byte flags, byte[] variableHeader, byte[] payLoad, 
			Action<long, long> sendProgress, 
			Action<string, string> handleProgress, 
			Action<long, long> receiveProgress )
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

				OperateResult<byte[]> command = MqttHelper.BuildMqttCommand( control, flags, variableHeader, payLoad, this.aesCryptography );
				if (!command.IsSuccess)
				{
					InteractiveLock.Leave( );
					result.CopyErrorFromOther( command );
					return result;
				}

				OperateResult<byte, byte[]> read = ReadMqttFromCoreServer( resultSocket.Content, command.Content, sendProgress, handleProgress, receiveProgress );
				if (read.IsSuccess)
				{
					IsSocketError = false;
					if (read.Content1 >> 4 == MqttControlMessage.FAILED)
					{
						OperateResult<string, byte[]> extra = MqttHelper.ExtraMqttReceiveData( read.Content1, read.Content2, this.aesCryptography );
						result.IsSuccess = false;
						result.ErrorCode = int.Parse( extra.Content1 );
						result.Message = Encoding.UTF8.GetString( extra.Content2 );
					}
					else
					{
						result.IsSuccess = read.IsSuccess;
						result.Content = read.Content2;
						result.Message = StringResources.Language.SuccessText;
					}
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

#if !NET35 && !NET20
		/// <inheritdoc/>
		public async override Task<OperateResult<byte[]>> ReadFromCoreServerAsync( Socket socket, byte[] send, bool hasResponseData = true, bool usePackHeader = true )
		{
			var read = await ReadMqttFromCoreServerAsync( socket, send, null, null, null );
			if (read.IsSuccess) return OperateResult.CreateSuccessResult( read.Content2 );

			return OperateResult.CreateFailedResult<byte[]>( read );
		}

		private async Task<OperateResult<byte, byte[]>> ReadMqttFromCoreServerAsync( Socket socket, byte[] send, Action<long, long> sendProgress, Action<string, string> handleProgress, Action<long, long> receiveProgress )
		{
			OperateResult sendResult = await SendAsync( socket, send );
			if (!sendResult.IsSuccess) return OperateResult.CreateFailedResult<byte, byte[]>( sendResult );

			// 先确认对方是否接收完数据
			while (true)
			{
				OperateResult<byte, byte[]> server_receive = await ReceiveMqttMessageAsync( socket, ReceiveTimeOut );
				if (!server_receive.IsSuccess) return server_receive;

				OperateResult<string, byte[]> server_back = MqttHelper.ExtraMqttReceiveData( server_receive.Content1, server_receive.Content2 );
				if (!server_back.IsSuccess) return OperateResult.CreateFailedResult<byte, byte[]>( server_back );

				if (server_back.Content2.Length != 16) return new OperateResult<byte, byte[]>( StringResources.Language.ReceiveDataLengthTooShort );
				long already = BitConverter.ToInt64( server_back.Content2, 0 );
				long total   = BitConverter.ToInt64( server_back.Content2, 8 );
				sendProgress?.Invoke( already, total );
				if (already == total) break;
			}

			// 如果接收到进度报告，就继续接收，直到不是进度报告的数据为止
			while (true)
			{
				OperateResult<byte, byte[]> receive = await ReceiveMqttMessageAsync( socket, ReceiveTimeOut, receiveProgress );
				if (!receive.IsSuccess) return receive;

				if (receive.Content1 >> 4 == MqttControlMessage.REPORTPROGRESS)
				{
					OperateResult<string, byte[]> extra = MqttHelper.ExtraMqttReceiveData( receive.Content1, receive.Content2 );
					handleProgress?.Invoke( extra.Content1, Encoding.UTF8.GetString( extra.Content2 ) );
				}
				else
				{
					return OperateResult.CreateSuccessResult( receive.Content1, receive.Content2 );
				}
			}
		}

		private async Task<OperateResult<byte[]>> ReadMqttFromCoreServerAsync( byte control, byte flags, byte[] variableHeader, byte[] payLoad, 
			Action<long, long> sendProgress, Action<string, string> handleProgress, Action<long, long> receiveProgress )
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

				OperateResult<byte[]> command = MqttHelper.BuildMqttCommand( control, flags, variableHeader, payLoad, this.aesCryptography );
				if (!command.IsSuccess)
				{
					InteractiveLock.Leave( );
					result.CopyErrorFromOther( command );
					return result;
				}

				OperateResult<byte, byte[]> read = await ReadMqttFromCoreServerAsync( resultSocket.Content, command.Content, sendProgress, handleProgress, receiveProgress );
				if (read.IsSuccess)
				{
					IsSocketError = false;
					if (read.Content1 >> 4 == MqttControlMessage.FAILED)
					{
						OperateResult<string, byte[]> extra = MqttHelper.ExtraMqttReceiveData( read.Content1, read.Content2, this.aesCryptography );
						result.IsSuccess = false;
						result.ErrorCode = int.Parse( extra.Content1 );
						result.Message = Encoding.UTF8.GetString( extra.Content2 );
					}
					else
					{
						result.IsSuccess = read.IsSuccess;
						result.Content = read.Content2;
						result.Message = StringResources.Language.SuccessText;
					}
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

		#region Public Method

		/// <summary>
		/// 从MQTT服务器同步读取数据，将payload发送到服务器，然后从服务器返回相关的数据，支持数据发送进度报告，服务器执行进度报告，接收数据进度报告操作<br />
		/// Synchronously read data from the MQTT server, send the payload to the server, and then return relevant data from the server, 
		/// support data transmission progress report, the server executes the progress report, and receives the data progress report
		/// </summary>
		/// <remarks>
		/// 进度报告可以实现一个比较有意思的功能，可以用来数据的上传和下载，提供一个友好的进度条，因为网络的好坏通常是不确定的。
		/// </remarks>
		/// <param name="topic">主题信息</param>
		/// <param name="payload">负载数据</param>
		/// <param name="sendProgress">发送数据给服务器时的进度报告，第一个参数为已发送数据，第二个参数为总发送数据</param>
		/// <param name="handleProgress">服务器处理数据的进度报告，第一个参数Topic自定义，通常用来传送操作百分比，第二个参数自定义，通常用来表示服务器消息</param>
		/// <param name="receiveProgress">从服务器接收数据的进度报告，第一个参数为已接收数据，第二个参数为总接收数据</param>
		/// <returns>服务器返回的数据信息</returns>
		public OperateResult<string, byte[]> Read( string topic, byte[] payload,
			Action<long, long> sendProgress = null,
			Action<string, string> handleProgress = null,
			Action<long, long> receiveProgress = null )
		{
			OperateResult<byte[]> read = ReadMqttFromCoreServer( MqttControlMessage.PUBLISH, 0x00, MqttHelper.BuildSegCommandByString( topic ), payload,
				sendProgress, handleProgress, receiveProgress );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<string, byte[]>( read );

			return MqttHelper.ExtraMqttReceiveData( MqttControlMessage.PUBLISH, read.Content, this.aesCryptography );
		}

		/// <summary>
		/// 从MQTT服务器同步读取数据，将指定编码的字符串payload发送到服务器，然后从服务器返回相关的数据，并转换为指定编码的字符串，支持数据发送进度报告，服务器执行进度报告，接收数据进度报告操作<br />
		/// Synchronously read data from the MQTT server, send the specified encoded string payload to the server, 
		/// and then return the data from the server, and convert it to the specified encoded string,
		/// support data transmission progress report, the server executes the progress report, and receives the data progress report
		/// </summary>
		/// <param name="topic">主题信息</param>
		/// <param name="payload">负载数据</param>
		/// <param name="sendProgress">发送数据给服务器时的进度报告，第一个参数为已发送数据，第二个参数为总发送数据</param>
		/// <param name="handleProgress">服务器处理数据的进度报告，第一个参数Topic自定义，通常用来传送操作百分比，第二个参数自定义，通常用来表示服务器消息</param>
		/// <param name="receiveProgress">从服务器接收数据的进度报告，第一个参数为已接收数据，第二个参数为总接收数据</param>
		/// <returns>服务器返回的数据信息</returns>
		public OperateResult<string, string> ReadString( string topic, string payload,
			Action<long, long> sendProgress = null,
			Action<string, string> handleProgress = null,
			Action<long, long> receiveProgress = null )
		{
			OperateResult<string, byte[]> read = Read( topic, string.IsNullOrEmpty( payload ) ? null : stringEncoding.GetBytes( payload ),
				sendProgress, handleProgress, receiveProgress );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<string, string>( read );

			return OperateResult.CreateSuccessResult( read.Content1, stringEncoding.GetString( read.Content2 ) );
		}

		/// <summary>
		/// 读取MQTT服务器注册的RPC接口，忽略返回的Topic数据，直接将结果转换为泛型对象，如果JSON转换失败，将返回错误，参数传递主题和数据负载，
		/// 数据负载示例："{\"address\": \"100\",\"length\": 10}" 本质是一个字符串。<br />
		/// Read the RPC interface registered by the MQTT server, ignore the returned Topic data, and directly convert the result into a generic object. 
		/// If the JSON conversion fails, an error will be returned. The parameter passes the topic and the data payload. 
		/// The data payload example: "{\"address\ ": \"100\",\"length\": 10}" is essentially a string.
		/// </summary>
		/// <remarks>
		/// 关于类型对象，需要和服务器返回的类型一致，如果服务器返回了 <see cref="string"/>, 这里也是 <see cref="string"/>, 如果是自定义对象，客户端没有该类型，可以使用 <see cref="JObject"/>
		/// </remarks>
		/// <typeparam name="T">泛型对象，需要和返回的数据匹配，如果返回的是 int 数组，那么这里就是 int[]，务必和服务器侧定义的返回类型一致</typeparam>
		/// <param name="topic">主题信息，也是服务器的 RPC 接口信息</param>
		/// <param name="payload">传递的参数信息，示例："{\"address\": \"100\",\"length\": 10}" 本质是一个字符串。</param>
		/// <returns>服务器返回的数据信息</returns>
		public OperateResult<T> ReadRpc<T>(string topic, string payload )
		{
			OperateResult<string, string> read = ReadString( topic, payload );
			if (!read.IsSuccess) return read.ConvertFailed<T>( );

			try
			{
				return OperateResult.CreateSuccessResult( Newtonsoft.Json.JsonConvert.DeserializeObject<T>( read.Content2 ) );
			}
			catch(Exception ex)
			{
				return new OperateResult<T>( "JSON failed: " + ex.Message + Environment.NewLine + "Source Data: " + read.Content2 );
			}
		}

		/// <summary>
		/// 读取MQTT服务器注册的RPC接口，忽略返回的Topic数据，直接将结果转换为泛型对象，如果JSON转换失败，将返回错误，参数传递主题和数据负载，
		/// 数据负载示例：new { address = "", length = 0 } 本质是一个匿名对象。<br />
		/// Read the RPC interface registered by the MQTT server, ignore the returned Topic data, and directly convert the result into a generic object. 
		/// If the JSON conversion fails, an error will be returned. The parameter passes the topic and the data payload. 
		/// The data payload example: new { address = "", length = 0 } is essentially an anonymous object.
		/// </summary>
		/// <remarks>
		/// 关于类型对象，需要和服务器返回的类型一致，如果服务器返回了 <see cref="string"/>, 这里也是 <see cref="string"/>, 如果是自定义对象，客户端没有该类型，可以使用 <see cref="JObject"/>
		/// </remarks>
		/// <typeparam name="T">泛型对象，需要和返回的数据匹配，如果返回的是 int 数组，那么这里就是 int[]</typeparam>
		/// <param name="topic">主题信息，也是服务器的 RPC 接口信息</param>
		/// <param name="payload">传递的参数信息，示例：new { address = "", length = 0 } 本质是一个匿名对象。</param>
		/// <returns>服务器返回的数据信息</returns>
		public OperateResult<T> ReadRpc<T>( string topic, object payload ) => ReadRpc<T>( topic, payload == null ? "{}" : payload.ToJsonString( ) );

		/// <summary>
		/// 读取服务器的已经注册的API信息列表，将返回API的主题路径，注释信息，示例的传入的数据信息。<br />
		/// Read the registered API information list of the server, and return the API subject path, annotation information, and sample incoming data information.
		/// </summary>
		/// <returns>包含是否成功的api信息的列表</returns>
		public OperateResult<MqttRpcApiInfo[]> ReadRpcApis( )
		{
			OperateResult<byte[]> read = ReadMqttFromCoreServer( MqttControlMessage.SUBSCRIBE, 0x00, MqttHelper.BuildSegCommandByString( "" ), null, null, null, null );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<MqttRpcApiInfo[]>( read );

			OperateResult<string, byte[]> mqtt = MqttHelper.ExtraMqttReceiveData( MqttControlMessage.PUBLISH, read.Content, this.aesCryptography );
			if(!mqtt.IsSuccess) return OperateResult.CreateFailedResult<MqttRpcApiInfo[]>( mqtt );

			return OperateResult.CreateSuccessResult( JArray.Parse( Encoding.UTF8.GetString( mqtt.Content2 ) ).ToObject<MqttRpcApiInfo[]>( ) );
		}

		/// <summary>
		/// 读取服务器的指定的API接口的每天的调用次数，如果API接口不存在，或是还没有调用数据，则返回失败。<br />
		/// Read the number of calls per day of the designated API interface of the server. 
		/// If the API interface does not exist or the data has not been called yet, it returns a failure.
		/// </summary>
		/// <remarks>
		/// 如果api的参数为空字符串，就是请求所有的接口的调用的统计信息。
		/// </remarks>
		/// <param name="api">等待请求的API的接口信息，如果为空，就是请求所有的接口的调用的统计信息。</param>
		/// <returns>最近几日的连续的调用情况，例如[1,2,3]，表示前提调用1次，昨天调用2次，今天3次</returns>
		public OperateResult<long[]> ReadRpcApiLog( string api )
		{
			OperateResult<byte[]> read = ReadMqttFromCoreServer( MqttControlMessage.PUBREL, 0x00, MqttHelper.BuildSegCommandByString( api ), null, null, null, null );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<long[]>( read );

			OperateResult<string, byte[]> mqtt = MqttHelper.ExtraMqttReceiveData( MqttControlMessage.PUBLISH, read.Content, this.aesCryptography );
			if (!mqtt.IsSuccess) return OperateResult.CreateFailedResult<long[]>( mqtt );

			string content = Encoding.UTF8.GetString( mqtt.Content2 );
			return OperateResult.CreateSuccessResult( content.ToStringArray<long>( ) );
		}

		/// <summary>
		/// 读取服务器的已经驻留的所有消息的主题列表<br />
		/// Read the topic list of all messages that have resided on the server
		/// </summary>
		/// <returns>消息列表对象</returns>
		public OperateResult<string[]> ReadRetainTopics( )
		{
			OperateResult<byte[]> read = ReadMqttFromCoreServer( MqttControlMessage.PUBACK, 0x00, MqttHelper.BuildSegCommandByString( "" ), null, null, null, null );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<string[]>( read );

			OperateResult<string, byte[]> mqtt = MqttHelper.ExtraMqttReceiveData( MqttControlMessage.PUBLISH, read.Content, this.aesCryptography );
			if (!mqtt.IsSuccess) return OperateResult.CreateFailedResult<string[]>( mqtt );

			return OperateResult.CreateSuccessResult( HslProtocol.UnPackStringArrayFromByte( mqtt.Content2 ) );
		}

		/// <summary>
		/// 读取服务器的已经驻留的指定主题的消息内容<br />
		/// Read the topic list of all messages that have resided on the server
		/// </summary>
		/// <param name="topic">指定的主题消息</param>
		/// <param name="receiveProgress">结果进度报告</param>
		/// <returns>消息列表对象</returns>
		public OperateResult<MqttClientApplicationMessage> ReadTopicPayload( string topic, Action<long, long> receiveProgress = null )
		{
			OperateResult<byte[]> read = ReadMqttFromCoreServer( MqttControlMessage.PUBREC, 0x00, MqttHelper.BuildSegCommandByString( topic ), null, null, null, receiveProgress );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<MqttClientApplicationMessage>( read );

			OperateResult<string, byte[]> mqtt = MqttHelper.ExtraMqttReceiveData( MqttControlMessage.PUBLISH, read.Content, this.aesCryptography );
			if (!mqtt.IsSuccess) return OperateResult.CreateFailedResult<MqttClientApplicationMessage>( mqtt );

			return OperateResult.CreateSuccessResult( JObject.Parse( Encoding.UTF8.GetString( mqtt.Content2 ) ).ToObject<MqttClientApplicationMessage>( ) );
		}

		#endregion

		#region Public Method Async
#if !NET35 && !NET20
		/// <inheritdoc cref="Read(string, byte[], Action{long, long}, Action{string, string}, Action{long, long})"/>
		public async Task<OperateResult<string, byte[]>> ReadAsync( string topic, byte[] payload,
			Action<long, long> sendProgress = null,
			Action<string, string> handleProgress = null,
			Action<long, long> receiveProgress = null )
		{
			OperateResult<byte[]> read = await ReadMqttFromCoreServerAsync( MqttControlMessage.PUBLISH, 0x00, MqttHelper.BuildSegCommandByString( topic ), payload, 
				sendProgress, handleProgress, receiveProgress );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<string, byte[]>( read );

			return MqttHelper.ExtraMqttReceiveData( MqttControlMessage.PUBLISH, read.Content, this.aesCryptography );
		}

		/// <inheritdoc cref="ReadString(string, string, Action{long, long}, Action{string, string}, Action{long, long})"/>
		public async Task<OperateResult<string, string>> ReadStringAsync( string topic, string payload,
			Action<long, long> sendProgress = null,
			Action<string, string> handleProgress = null,
			Action<long, long> receiveProgress = null )
		{
			OperateResult<string, byte[]> read = await ReadAsync( topic, string.IsNullOrEmpty( payload ) ? null : stringEncoding.GetBytes( payload ),
				sendProgress, handleProgress, receiveProgress );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<string, string>( read );

			return OperateResult.CreateSuccessResult( read.Content1, stringEncoding.GetString( read.Content2 ) );
		}

		/// <inheritdoc cref="ReadRpc{T}(string, string)"/>
		public async Task<OperateResult<T>> ReadRpcAsync<T>( string topic, string payload )
		{
			OperateResult<string, string> read = await ReadStringAsync( topic, payload );
			if (!read.IsSuccess) return read.ConvertFailed<T>( );

			try
			{
				return OperateResult.CreateSuccessResult( Newtonsoft.Json.JsonConvert.DeserializeObject<T>( read.Content2 ) );
			}
			catch (Exception ex)
			{
				return new OperateResult<T>( "JSON failed: " + ex.Message + Environment.NewLine + "Source Data: " + read.Content2 );
			}
		}

		/// <inheritdoc cref="ReadRpc{T}(string, object)"/>
		public async Task<OperateResult<T>> ReadRpcAsync<T>( string topic, object payload ) => await ReadRpcAsync<T>( topic, payload == null ? "{}" : payload.ToJsonString( ) );

		/// <inheritdoc cref="ReadRpcApis"/>
		public async Task<OperateResult<MqttRpcApiInfo[]>> ReadRpcApisAsync( )
		{
			OperateResult<byte[]> read = await ReadMqttFromCoreServerAsync( MqttControlMessage.SUBSCRIBE, 0x00, MqttHelper.BuildSegCommandByString( "" ), null, null, null, null );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<MqttRpcApiInfo[]>( read );

			OperateResult<string, byte[]> mqtt = MqttHelper.ExtraMqttReceiveData( MqttControlMessage.PUBLISH, read.Content, this.aesCryptography );
			if (!mqtt.IsSuccess) return OperateResult.CreateFailedResult<MqttRpcApiInfo[]>( mqtt );

			return OperateResult.CreateSuccessResult( JArray.Parse( Encoding.UTF8.GetString( mqtt.Content2 ) ).ToObject<MqttRpcApiInfo[]>( ) );
		}

		/// <inheritdoc cref="ReadRpcApiLog(string)"/>
		public async Task<OperateResult<long[]>> ReadRpcApiLogAsync( string api )
		{
			OperateResult<byte[]> read = await ReadMqttFromCoreServerAsync( MqttControlMessage.PUBREL, 0x00, MqttHelper.BuildSegCommandByString( api ), null, null, null, null );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<long[]>( read );

			OperateResult<string, byte[]> mqtt = MqttHelper.ExtraMqttReceiveData( MqttControlMessage.PUBLISH, read.Content, this.aesCryptography );
			if (!mqtt.IsSuccess) return OperateResult.CreateFailedResult<long[]>( mqtt );

			string content = Encoding.UTF8.GetString( mqtt.Content2 );
			return OperateResult.CreateSuccessResult( content.ToStringArray<long>( ) );
		}

		/// <inheritdoc cref="ReadRetainTopics"/>
		public async Task<OperateResult<string[]>> ReadRetainTopicsAsync( )
		{
			OperateResult<byte[]> read = await ReadMqttFromCoreServerAsync( MqttControlMessage.PUBACK, 0x00, MqttHelper.BuildSegCommandByString( "" ), null, null, null, null );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<string[]>( read );

			OperateResult<string, byte[]> mqtt = MqttHelper.ExtraMqttReceiveData( MqttControlMessage.PUBLISH, read.Content, this.aesCryptography );
			if (!mqtt.IsSuccess) return OperateResult.CreateFailedResult<string[]>( mqtt );

			return OperateResult.CreateSuccessResult( HslProtocol.UnPackStringArrayFromByte( mqtt.Content2 ) );
		}

		/// <inheritdoc cref="ReadTopicPayload(string, Action{long, long})"/>
		public async Task<OperateResult<MqttClientApplicationMessage>> ReadTopicPayloadAsync( string topic, Action<long, long> receiveProgress = null )
		{
			OperateResult<byte[]> read = await ReadMqttFromCoreServerAsync( MqttControlMessage.PUBREC, 0x00, MqttHelper.BuildSegCommandByString( topic ), null, null, null, receiveProgress );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<MqttClientApplicationMessage>( read );

			OperateResult<string, byte[]> mqtt = MqttHelper.ExtraMqttReceiveData( MqttControlMessage.PUBLISH, read.Content, this.aesCryptography );
			if (!mqtt.IsSuccess) return OperateResult.CreateFailedResult<MqttClientApplicationMessage>( mqtt );

			return OperateResult.CreateSuccessResult( JObject.Parse( Encoding.UTF8.GetString( mqtt.Content2 ) ).ToObject<MqttClientApplicationMessage>( ) );
		}

#endif
		#endregion

		#region File Download Upload


		private OperateResult<Socket> ConnectFileServer( byte code, string groups, string[] fileNames )
		{
			// 创建连接对象
			OperateResult<Socket> socketResult = CreateSocketAndConnect( this.IpAddress, this.Port, this.ConnectTimeOut );
			if (!socketResult.IsSuccess) return socketResult;

			// 账户登录
			OperateResult ini = InitializationMqttSocket( socketResult.Content, "FILE" );
			if (!ini.IsSuccess) return OperateResult.CreateFailedResult<Socket>( ini );

			// 发送文件分类
			OperateResult sendClass = Send( socketResult.Content, MqttHelper.BuildMqttCommand( code, null,
				HslProtocol.PackStringArrayToByte( string.IsNullOrEmpty( groups ) ? null : groups.Split( new char[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries ) ) ).Content );
			if (!sendClass.IsSuccess) return OperateResult.CreateFailedResult<Socket>( sendClass );

			// 发送操作指令
			OperateResult sendString = Send( socketResult.Content, MqttHelper.BuildMqttCommand( code, null, HslProtocol.PackStringArrayToByte( fileNames ) ).Content );
			if (!sendString.IsSuccess) return OperateResult.CreateFailedResult<Socket>( sendString );

			// 返回服务器操作的合法性
			OperateResult<byte, byte[]> legal = ReceiveMqttMessage( socketResult.Content, 60_000 );
			if (!legal.IsSuccess) return OperateResult.CreateFailedResult<Socket>( legal );

			if (legal.Content1 == MqttControlMessage.FAILED) { socketResult.Content?.Close( ); return new OperateResult<Socket>( Encoding.UTF8.GetString( legal.Content2 ) ); }

			return OperateResult.CreateSuccessResult( socketResult.Content );
		}

		private OperateResult DownloadFileBase( string groups, string fileName, Action<long, long> processReport, object source )
		{
			// connect server
			OperateResult<Socket> socketResult = ConnectFileServer( MqttControlMessage.FileDownload, groups, new string[] { fileName } );
			if (!socketResult.IsSuccess) return socketResult;

			// 根据数据源分析
			OperateResult result = ReceiveMqttFile( socketResult.Content, source, processReport, this.aesCryptography );
			if (!result.IsSuccess) return result;

			socketResult.Content?.Close( );
			return OperateResult.CreateSuccessResult( );
		}

		/// <summary>
		/// 从远程服务器下载一个文件到本地，需要指定文件类别，文件名，进度报告，本地保存的文件名<br />
		/// To download a file from a remote server to the local, you need to specify the file category, file name, progress report, and file name saved locally
		/// </summary>
		/// <param name="groups">文件的类别，例如 Files/Personal/Admin 按照斜杠来区分 </param>
		/// <param name="fileName">文件名称，例如 123.txt</param>
		/// <param name="processReport">进度报告，第一个参数是已完成字节数，第二个参数是总字节数</param>
		/// <param name="fileSaveName">本地保存的文件名</param>
		/// <returns>是否下载成功</returns>
		public OperateResult DownloadFile( string groups, string fileName, Action<long, long> processReport, string fileSaveName )
		{
			return DownloadFileBase( groups, fileName, processReport, fileSaveName );
		}

		/// <summary>
		/// 从远程服务器下载一个文件到流中，需要指定文件类别，文件名，进度报告，本地保存的文件名<br />
		/// To download a file from a remote server to the stream, you need to specify the file category, file name, progress report, and file name saved locally
		/// </summary>
		/// <param name="groups">文件的类别，例如 Files/Personal/Admin 按照斜杠来区分 </param>
		/// <param name="fileName">文件名称，例如 123.txt</param>
		/// <param name="processReport">进度报告，第一个参数是已完成字节数，第二个参数是总字节数</param>
		/// <param name="stream">数据流</param>
		/// <returns>是否下载成功</returns>
		public OperateResult DownloadFile( string groups, string fileName, Action<long, long> processReport, Stream stream )
		{
			return DownloadFileBase( groups, fileName, processReport, stream );
		}

#if NET20 || NET35 || NET451
		/// <summary>
		/// 从远程服务器下载一个文件，生成一个Bitmap图片对象，需要指定文件类别，文件名，进度报告，可用于用户头像的存储<br />
		/// Download a file from a remote server and generate a Bitmap image object. You need to specify the file category, file name, and progress report, which can be used to store the user's avatar
		/// </summary>
		/// <param name="groups">文件的类别，例如 Files/Personal/Admin 按照斜杠来区分</param>
		/// <param name="fileName">文件名称，例如 123.txt</param>
		/// <param name="processReport">进度报告，第一个参数是已完成字节数，第二个参数是总字节数</param>
		/// <returns>如果下载成功，则携带图片资源对象</returns>
		public OperateResult<Bitmap> DownloadBitmap( string groups, string fileName, Action<long, long> processReport )
		{
			MemoryStream stream = new MemoryStream( );
			Bitmap bitmap = null;
			OperateResult result = DownloadFileBase( groups, fileName, processReport, stream );
			if (!result.IsSuccess)
			{
				stream.Dispose( );
				return OperateResult.CreateFailedResult<Bitmap>( result );
			}

			bitmap = new Bitmap( stream );
			stream.Dispose( );
			return OperateResult.CreateSuccessResult( bitmap );
		}
		/// <summary>
		/// 上传一个Bitmap图片对象到服务器指定的分类下面，需要指定分类信息，服务器保存的文件名，描述信息，支持进度报告<br />
		/// Upload a Bitmap image object to the category specified by the server, you need to specify the category information, 
		/// the file name saved by the server, description information, and support for progress reports
		/// </summary>
		/// <param name="bitmap">图片对象</param>
		/// <param name="groups">文件的类别，例如 Files/Personal/Admin 按照斜杠来区分</param>
		/// <param name="serverName">在服务器保存的文件名称</param>
		/// <param name="fileTag">文件的额外的描述信息</param>
		/// <param name="processReport">进度报告，第一个参数是已完成字节数，第二个参数是总字节数</param>
		/// <returns>是否上传成功</returns>
		public OperateResult UploadFile(
			Bitmap bitmap,
			string groups,
			string serverName,
			string fileTag,
			Action<long, long> processReport )
		{
			MemoryStream stream = new MemoryStream( );
			if (bitmap.RawFormat != null) bitmap.Save( stream, bitmap.RawFormat );
			else bitmap.Save( stream, System.Drawing.Imaging.ImageFormat.Bmp );
			OperateResult result = UploadFileBase( stream, groups, serverName, fileTag, processReport );
			stream.Dispose( );
			return result;
		}
#endif
		/// <summary>
		/// 上传文件给服务器，需要指定上传的数据内容，上传到服务器的分类信息，支持进度汇报功能。<br />
		/// To upload files to the server, you need to specify the content of the uploaded data, 
		/// the classification information uploaded to the server, and support the progress report function.
		/// </summary>
		/// <param name="source">数据源，可以是文件名，也可以是数据流</param>
		/// <param name="serverName">在服务器保存的文件名，不包含驱动器路径</param>
		/// <param name="groups">文件的类别，例如 Files/Personal/Admin 按照斜杠来区分</param>
		/// <param name="fileTag">文件的额外的描述信息</param>
		/// <param name="processReport">进度报告，第一个参数是已完成字节数，第二个参数是总字节数</param>
		/// <returns>是否成功的结果对象</returns>
		private OperateResult UploadFileBase(
			object source,
			string groups,
			string serverName,
			string fileTag,
			Action<long, long> processReport )
		{
			OperateResult<Socket> socketResult = ConnectFileServer( MqttControlMessage.FileUpload, groups, new string[] { serverName } );
			if (!socketResult.IsSuccess) return socketResult;

			// 判断数据源格式
			if (source is string fileName)
			{
				OperateResult result = SendMqttFile( socketResult.Content, fileName, serverName, fileTag, processReport, this.aesCryptography );
				if (!result.IsSuccess) return result;
			}
			else if (source is Stream stream)
			{
				OperateResult result = SendMqttFile( socketResult.Content, stream, serverName, fileTag, processReport, this.aesCryptography );
				if (!result.IsSuccess) return result;
			}
			else
			{
				socketResult.Content?.Close( );
				LogNet?.WriteError( ToString( ), StringResources.Language.DataSourceFormatError );
				return new OperateResult( StringResources.Language.DataSourceFormatError );
			}

			// 确认服务器文件保存状态
			OperateResult<byte, byte[]> resultCheck = ReceiveMqttMessage( socketResult.Content, 60_000 );
			if (!resultCheck.IsSuccess) return resultCheck;

			socketResult.Content?.Close( );
			return resultCheck.Content1 != MqttControlMessage.FAILED ? OperateResult.CreateSuccessResult( ) : new OperateResult( Encoding.UTF8.GetString( resultCheck.Content2 ) );
		}

		/// <summary>
		/// 上传文件给服务器，需要指定上传文件的路径信息，服务器保存的名字，以及上传到服务器的分类信息，支持进度汇报功能。<br />
		/// To upload a file to the server, you need to specify the path information of the uploaded file, the name saved by the server, 
		/// and the classification information uploaded to the server to support the progress report function.
		/// </summary>
		/// <param name="fileName">文件名，需要指定完整的路径信息，文件必须存在，否则发送失败</param>
		/// <param name="groups">文件的类别，例如 Files/Personal/Admin 按照斜杠来区分</param>
		/// <param name="serverName">服务器端保存的文件名</param>
		/// <param name="fileTag">文件的额外的描述信息</param>
		/// <param name="processReport">进度报告，第一个参数是已完成字节数，第二个参数是总字节数</param>
		/// <returns>是否上传成功的结果对象</returns>
		public OperateResult UploadFile(
			string fileName,
			string groups,
			string serverName,
			string fileTag,
			Action<long, long> processReport )
		{
			if (!File.Exists( fileName )) return new OperateResult( StringResources.Language.FileNotExist );

			return UploadFileBase( fileName, groups, serverName, fileTag, processReport );
		}

		/// <summary>
		/// 上传文件给服务器，需要指定上传文件的路径信息(服务器保存的名称就是文件名)，以及上传到服务器的分类信息，支持进度汇报功能。<br />
		/// To upload a file to the server, you need to specify the path information of the uploaded file (the name saved by the server is the file name), 
		/// as well as the classification information uploaded to the server, to support the progress report function.
		/// </summary>
		/// <param name="fileName">文件名，需要指定完整的路径信息，文件必须存在，否则发送失败</param>
		/// <param name="groups">文件的类别，例如 Files/Personal/Admin 按照斜杠来区分</param>
		/// <param name="fileTag">文件的额外的描述信息</param>
		/// <param name="processReport">进度报告，第一个参数是已完成字节数，第二个参数是总字节数</param>
		/// <returns>是否上传成功的结果对象</returns>
		public OperateResult UploadFile(
			string fileName,
			string groups,
			string fileTag,
			Action<long, long> processReport )
		{
			if (!File.Exists( fileName )) return new OperateResult( StringResources.Language.FileNotExist );

			FileInfo fileInfo = new FileInfo( fileName );
			return UploadFileBase( fileName, groups, fileInfo.Name, fileTag, processReport );
		}

		private OperateResult<T[]> DownloadStringArrays<T>( byte protocol, string groups, string[] fileNames )
		{
			// connect server
			OperateResult<Socket> socketResult = ConnectFileServer( protocol, groups, fileNames );
			if (!socketResult.IsSuccess) return OperateResult.CreateFailedResult<T[]>( socketResult );

			// 接收数据信息
			OperateResult<byte, byte[]> receive = ReceiveMqttMessage( socketResult.Content, 60_000 );
			if (!receive.IsSuccess) return OperateResult.CreateFailedResult<T[]>( receive );
			socketResult.Content?.Close( );

			// 数据转化
			try
			{
				return OperateResult.CreateSuccessResult( JArray.Parse( Encoding.UTF8.GetString( receive.Content2 ) ).ToObject<T[]>( ) );
			}
			catch (Exception ex)
			{
				return new OperateResult<T[]>( ex.Message );
			}

		}

		/// <summary>
		/// 下载指定分类信息的所有的文件描述信息，需要指定分类信息，例如：Files/Personal/Admin<br />
		/// To download all the file description information of the specified classification information, 
		/// you need to specify the classification information, for example: Files/Personal/Admin
		/// </summary>
		/// <param name="groups">文件的类别，例如 Files/Personal/Admin 按照斜杠来区分</param>
		/// <returns>当前分类下所有的文件描述信息</returns>
		public OperateResult<GroupFileItem[]> DownloadPathFileNames( string groups )
		{
			return DownloadStringArrays<GroupFileItem>( MqttControlMessage.FileFolderFiles, groups, null );
		}

		/// <summary>
		/// 下载指定分类信息的全部子分类信息<br />
		/// Download all sub-category information of the specified category information
		/// </summary>
		/// <param name="groups">文件的类别，例如 Files/Personal/Admin 按照斜杠来区分</param>
		/// <returns>当前分类下所有的子分类信息</returns>
		public OperateResult<string[]> DownloadPathFolders( string groups )
		{
			return DownloadStringArrays<string>( MqttControlMessage.FileFolderPaths, groups, null );
		}

		/// <summary>
		/// 请求服务器指定分类是否存在指定的文件名，需要指定分类信息，文件名<br />
		/// Request the server to specify whether the specified file name exists in the specified category, need to specify the category information, file name
		/// </summary>
		/// <param name="groups">文件的类别，例如 Files/Personal/Admin 按照斜杠来区分</param>
		/// <param name="fileName">文件名信息，例如 123.txt</param>
		/// <returns>Content为True表示存在，否则为不存在</returns>
		public OperateResult<bool> IsFileExists( string groups, string fileName )
		{
			// connect server
			OperateResult<Socket> socketResult = ConnectFileServer( MqttControlMessage.FileExists, groups, new string[] { fileName } );
			if (!socketResult.IsSuccess) return OperateResult.CreateFailedResult<bool>( socketResult );

			// 接收服务器操作结果
			OperateResult<byte, byte[]> receiveBack = ReceiveMqttMessage( socketResult.Content, 60_000 );
			if (!receiveBack.IsSuccess) return OperateResult.CreateFailedResult<bool>( receiveBack );

			OperateResult<bool> result = OperateResult.CreateSuccessResult( receiveBack.Content1 == 1 );
			socketResult.Content?.Close( );
			return result;
		}

		/// <summary>
		/// 删除服务器的指定的文件名，需要指定分类信息，文件名<br />
		/// Delete the specified file name of the server, need to specify the classification information, file name
		/// </summary>
		/// <param name="groups">文件的类别，例如 Files/Personal/Admin 按照斜杠来区分</param>
		/// <param name="fileName">文件名信息</param>
		/// <returns>是否删除成功</returns>
		public OperateResult DeleteFile( string groups, string fileName ) => DeleteFile( groups, new string[] { fileName } );

		/// <inheritdoc cref="DeleteFile(string, string)"/>
		public OperateResult DeleteFile( string groups, string[] fileNames )
		{
			// connect server
			OperateResult<Socket> socketResult = ConnectFileServer( MqttControlMessage.FileDelete, groups, fileNames );
			if (!socketResult.IsSuccess) return OperateResult.CreateFailedResult<bool>( socketResult );

			// 接收服务器操作结果
			OperateResult<byte, byte[]> receiveBack = ReceiveMqttMessage( socketResult.Content, 60_000 );
			if (!receiveBack.IsSuccess) return OperateResult.CreateFailedResult<bool>( receiveBack );

			socketResult.Content?.Close( );
			return OperateResult.CreateSuccessResult( );
		}

		/// <summary>
		/// 删除服务器上指定的分类信息及管理的所有的文件，包含所有的子分类信息，不可逆操作，谨慎操作。<br />
		/// Delete the specified classification information and all files managed on the server, 
		/// including all sub-classification information, irreversible operation, and careful operation.
		/// </summary>
		/// <param name="groups">文件的类别，例如 Files/Personal/Admin 按照斜杠来区分</param>
		/// <returns>是否删除成功</returns>
		public OperateResult DeleteFolderFiles( string groups )
		{
			// connect server
			OperateResult<Socket> socketResult = ConnectFileServer( MqttControlMessage.FileFolderDelete, groups, null );
			if (!socketResult.IsSuccess) return OperateResult.CreateFailedResult<bool>( socketResult );

			// 接收服务器操作结果
			OperateResult<byte, byte[]> receiveBack = ReceiveMqttMessage( socketResult.Content, 60_000 );
			if (!receiveBack.IsSuccess) return OperateResult.CreateFailedResult<bool>( receiveBack );

			socketResult.Content?.Close( );
			return OperateResult.CreateSuccessResult( );
		}

		/// <summary>
		/// 获取服务器文件夹的指定目录的文件统计信息，包括文件数量，总大小，最后更新时间<br />
		/// Get the file statistics of the specified directory of the server folder, including the number of files, the total size, and the last update time
		/// </summary>
		/// <param name="groups">文件的类别，例如 Files/Personal/Admin 按照斜杠来区分</param>
		/// <returns>服务器文件大小的结果对象，单位：字节数</returns>
		public OperateResult<GroupFileInfo> GetGroupFileInfo( string groups )
		{
			// connect server
			OperateResult<Socket> socketResult = ConnectFileServer( MqttControlMessage.FileFolderInfo, groups, null );
			if (!socketResult.IsSuccess) return OperateResult.CreateFailedResult<GroupFileInfo>( socketResult );

			// 接收服务器操作结果
			OperateResult<byte, byte[]> receiveBack = ReceiveMqttMessage( socketResult.Content, 60_000 );
			if (!receiveBack.IsSuccess) return OperateResult.CreateFailedResult<GroupFileInfo>( receiveBack );

			socketResult.Content?.Close( );
			return OperateResult.CreateSuccessResult( JObject.Parse( Encoding.UTF8.GetString( receiveBack.Content2 ) ).ToObject<GroupFileInfo>( ) );
		}

		/// <summary>
		/// 获取服务器文件夹的指定目录的所有子目录的文件信息，包括每个子目录的文件数量，总大小，最后更新时间<br />
		/// Get the file information of all subdirectories of the specified directory of the server folder, including the number of files in each subdirectory, the total size, and the last update time
		/// </summary>
		/// <param name="groups">文件的类别，例如 Files/Personal/Admin 按照斜杠来区分</param>
		/// <returns>服务器文件大小的结果对象，单位：字节数</returns>
		public OperateResult<GroupFileInfo[]> GetSubGroupFileInfos( string groups )
		{
			// connect server
			OperateResult<Socket> socketResult = ConnectFileServer( MqttControlMessage.FileFolderInfos, groups, null );
			if (!socketResult.IsSuccess) return OperateResult.CreateFailedResult<GroupFileInfo[]>( socketResult );

			// 接收服务器操作结果
			OperateResult<byte, byte[]> receiveBack = ReceiveMqttMessage( socketResult.Content, 60_000 );
			if (!receiveBack.IsSuccess) return OperateResult.CreateFailedResult<GroupFileInfo[]>( receiveBack );

			socketResult.Content?.Close( );
			return OperateResult.CreateSuccessResult( JArray.Parse( Encoding.UTF8.GetString( receiveBack.Content2 ) ).ToObject<GroupFileInfo[]>( ) );
		}

#if !NET20 && !NET35
		private async Task<OperateResult<Socket>> ConnectFileServerAsync( byte code, string groups, string[] fileNames )
		{
			// 创建连接对象
			OperateResult<Socket> socketResult = await CreateSocketAndConnectAsync( this.IpAddress, this.Port, this.ConnectTimeOut );
			if (!socketResult.IsSuccess) return socketResult;

			// 账户登录
			OperateResult ini = await InitializationMqttSocketAsync( socketResult.Content, "FILE" );
			if (!ini.IsSuccess) return OperateResult.CreateFailedResult<Socket>( ini );

			// 发送文件分类
			OperateResult sendClass = await SendAsync( socketResult.Content, MqttHelper.BuildMqttCommand( code, null, 
				HslProtocol.PackStringArrayToByte( string.IsNullOrEmpty( groups ) ? null : groups.Split( new char[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries ) ) ).Content );
			if (!sendClass.IsSuccess) return OperateResult.CreateFailedResult<Socket>( sendClass );

			// 发送操作指令
			OperateResult sendString = await SendAsync( socketResult.Content, MqttHelper.BuildMqttCommand( code, null, HslProtocol.PackStringArrayToByte( fileNames ) ).Content );
			if (!sendString.IsSuccess) return OperateResult.CreateFailedResult<Socket>( sendString );

			// 返回服务器操作的合法性
			OperateResult<byte, byte[]> legal = await ReceiveMqttMessageAsync( socketResult.Content, 60_000 );
			if (!legal.IsSuccess) return OperateResult.CreateFailedResult<Socket>( legal );

			if(legal.Content1 == MqttControlMessage.FAILED) { socketResult.Content?.Close( ); return new OperateResult<Socket>( Encoding.UTF8.GetString( legal.Content2 ) ); }

			return OperateResult.CreateSuccessResult( socketResult.Content );
		}

		private async Task<OperateResult> DownloadFileBaseAsync( string groups, string fileName, Action<long, long> processReport, object source )
		{
			// connect server
			OperateResult<Socket> socketResult = await ConnectFileServerAsync( MqttControlMessage.FileDownload , groups, new string[] { fileName } );
			if (!socketResult.IsSuccess) return socketResult;

			// 根据数据源分析
			OperateResult result = await ReceiveMqttFileAsync( socketResult.Content, source, processReport, this.aesCryptography );
			if (!result.IsSuccess) return result;

			socketResult.Content?.Close( );
			return OperateResult.CreateSuccessResult( );
		}

		/// <inheritdoc cref="DownloadFile(string, string, Action{long, long}, string)"/>
		public async Task<OperateResult> DownloadFileAsync( string groups, string fileName, Action<long, long> processReport, string fileSaveName )
		{
			return await DownloadFileBaseAsync( groups, fileName, processReport, fileSaveName );
		}

		/// <inheritdoc cref="DownloadFile(string, string, Action{long, long}, Stream)"/>
		public async Task<OperateResult> DownloadFileAsync( string groups, string fileName, Action<long, long> processReport, Stream stream )
		{
			return await DownloadFileBaseAsync( groups, fileName, processReport, stream );
		}

#if NET20 || NET35 || NET451
		/// <inheritdoc cref="DownloadBitmap(string, string, Action{long, long})"/>
		public async Task<OperateResult<Bitmap>> DownloadBitmapAsync( string groups, string fileName, Action<long, long> processReport )
		{
			MemoryStream stream = new MemoryStream( );
			Bitmap bitmap = null;
			OperateResult result = await DownloadFileBaseAsync( groups, fileName, processReport, stream );
			if (!result.IsSuccess)
			{
				stream.Dispose( );
				return OperateResult.CreateFailedResult<Bitmap>( result );
			}

			bitmap = new Bitmap( stream );
			stream.Dispose( );
			return OperateResult.CreateSuccessResult( bitmap );
		}

		/// <inheritdoc cref="UploadFile(Bitmap, string, string, string, Action{long, long})"/>
		public async Task<OperateResult> UploadFileAsync( Bitmap bitmap, string groups, string serverName, string fileTag, Action<long, long> processReport )
		{
			MemoryStream stream = new MemoryStream( );
			if (bitmap.RawFormat != null) bitmap.Save( stream, bitmap.RawFormat );
			else bitmap.Save( stream, System.Drawing.Imaging.ImageFormat.Bmp );
			OperateResult result = await UploadFileBaseAsync( stream, groups, serverName, fileTag, processReport );
			stream.Dispose( );
			return result;
		}
#endif

		/// <inheritdoc cref="UploadFileBase(object, string, string, string, Action{long, long})"/>
		private async Task<OperateResult> UploadFileBaseAsync(
			object source,
			string groups,
			string serverName,
			string fileTag,
			Action<long, long> processReport )
		{
			OperateResult<Socket> socketResult = await ConnectFileServerAsync( MqttControlMessage.FileUpload, groups, new string[] { serverName } );
			if (!socketResult.IsSuccess) return socketResult;

			// 判断数据源格式
			if (source is string fileName)
			{
				OperateResult result = await SendMqttFileAsync( socketResult.Content, fileName, serverName, fileTag, processReport, this.aesCryptography );
				if (!result.IsSuccess) return result;
			}
			else if (source is Stream stream)
			{
				OperateResult result = await SendMqttFileAsync( socketResult.Content, stream, serverName, fileTag, processReport, this.aesCryptography );
				if (!result.IsSuccess) return result;
			}
			else
			{
				socketResult.Content?.Close( );
				LogNet?.WriteError( ToString( ), StringResources.Language.DataSourceFormatError );
				return new OperateResult( StringResources.Language.DataSourceFormatError );
			}

			// 确认服务器文件保存状态
			OperateResult<byte, byte[]> resultCheck = await ReceiveMqttMessageAsync( socketResult.Content, 60_000 );
			if (!resultCheck.IsSuccess) return resultCheck;

			socketResult.Content?.Close( );
			return resultCheck.Content1 != MqttControlMessage.FAILED ? OperateResult.CreateSuccessResult( ) : new OperateResult( Encoding.UTF8.GetString( resultCheck.Content2 ) );
		}

		/// <inheritdoc cref="UploadFile(string, string, string, string, Action{long, long})"/>
		public async Task<OperateResult> UploadFileAsync(
			string fileName,
			string groups,
			string serverName,
			string fileTag,
			Action<long, long> processReport )
		{
			if (!File.Exists( fileName )) return new OperateResult( StringResources.Language.FileNotExist );

			return await UploadFileBaseAsync( fileName, groups, serverName, fileTag, processReport );
		}

		/// <inheritdoc cref="UploadFile(string, string, string, Action{long, long})"/>
		public async Task<OperateResult> UploadFileAsync(
			string fileName,
			string groups,
			string fileTag,
			Action<long, long> processReport )
		{
			if (!File.Exists( fileName )) return new OperateResult( StringResources.Language.FileNotExist );

			FileInfo fileInfo = new FileInfo( fileName );
			return await UploadFileBaseAsync( fileName, groups, fileInfo.Name, fileTag, processReport );
		}

		private async Task<OperateResult<T[]>> DownloadStringArraysAsync<T>( byte protocol, string groups, string[] fileNames )
		{
			// connect server
			OperateResult<Socket> socketResult = await ConnectFileServerAsync( protocol, groups, fileNames );
			if (!socketResult.IsSuccess) return OperateResult.CreateFailedResult<T[]>( socketResult );

			// 接收数据信息
			OperateResult<byte, byte[]> receive = await ReceiveMqttMessageAsync( socketResult.Content, 60_000 );
			if (!receive.IsSuccess) return OperateResult.CreateFailedResult<T[]>( receive );
			socketResult.Content?.Close( );

			// 数据转化
			try
			{
				return OperateResult.CreateSuccessResult( JArray.Parse( Encoding.UTF8.GetString( receive.Content2 ) ).ToObject<T[]>( ) );
			}
			catch (Exception ex)
			{
				return new OperateResult<T[]>( ex.Message );
			}

		}

		/// <inheritdoc cref="DownloadPathFileNames(string)"/>
		public async Task<OperateResult<GroupFileItem[]>> DownloadPathFileNamesAsync( string groups )
		{
			return await DownloadStringArraysAsync<GroupFileItem>( MqttControlMessage.FileFolderFiles, groups, null );
		}

		/// <inheritdoc cref="DownloadPathFolders(string)"/>
		public async Task<OperateResult<string[]>> DownloadPathFoldersAsync( string groups )
		{
			return await DownloadStringArraysAsync<string>( MqttControlMessage.FileFolderPaths, groups, null );
		}

		/// <inheritdoc cref="IsFileExists(string, string)"/>
		public async Task<OperateResult<bool>> IsFileExistsAsync( string groups, string fileName )
		{
			// connect server
			OperateResult<Socket> socketResult = await ConnectFileServerAsync( MqttControlMessage.FileExists, groups, new string[] { fileName } );
			if (!socketResult.IsSuccess) return OperateResult.CreateFailedResult<bool>( socketResult );

			// 接收服务器操作结果
			OperateResult<byte, byte[]> receiveBack = await ReceiveMqttMessageAsync( socketResult.Content, 60_000 );
			if (!receiveBack.IsSuccess) return OperateResult.CreateFailedResult<bool>( receiveBack );

			OperateResult<bool> result = OperateResult.CreateSuccessResult( receiveBack.Content1 == 1 );
			socketResult.Content?.Close( );
			return result;
		}

		/// <inheritdoc cref="DeleteFile(string, string)"/>
		public async Task<OperateResult> DeleteFileAsync( string groups, string fileName ) => await DeleteFileAsync( groups, new string[] { fileName } );
		
		/// <inheritdoc cref="DeleteFile(string, string[])"/>
		public async Task<OperateResult> DeleteFileAsync( string groups, string[] fileNames)
		{
			// connect server
			OperateResult<Socket> socketResult = await ConnectFileServerAsync( MqttControlMessage.FileDelete, groups, fileNames );
			if (!socketResult.IsSuccess) return OperateResult.CreateFailedResult<bool>( socketResult );

			// 接收服务器操作结果
			OperateResult<byte, byte[]> receiveBack = await ReceiveMqttMessageAsync( socketResult.Content, 60_000 );
			if (!receiveBack.IsSuccess) return OperateResult.CreateFailedResult<bool>( receiveBack );

			socketResult.Content?.Close( );
			return OperateResult.CreateSuccessResult( );
		}

		/// <inheritdoc cref="DeleteFolderFiles(string)"/>
		public async Task<OperateResult> DeleteFolderFilesAsync( string groups )
		{
			// connect server
			OperateResult<Socket> socketResult = await ConnectFileServerAsync( MqttControlMessage.FileFolderDelete, groups, null );
			if (!socketResult.IsSuccess) return OperateResult.CreateFailedResult<bool>( socketResult );

			// 接收服务器操作结果
			OperateResult<byte, byte[]> receiveBack = await ReceiveMqttMessageAsync( socketResult.Content, 60_000 );
			if (!receiveBack.IsSuccess) return OperateResult.CreateFailedResult<bool>( receiveBack );

			socketResult.Content?.Close( );
			return OperateResult.CreateSuccessResult( );
		}

		/// <inheritdoc cref="GetGroupFileInfo(string)"/>
		public async Task<OperateResult<GroupFileInfo>> GetGroupFileInfoAsync( string groups )
		{
			// connect server
			OperateResult<Socket> socketResult = await ConnectFileServerAsync( MqttControlMessage.FileFolderInfo, groups, null );
			if (!socketResult.IsSuccess) return OperateResult.CreateFailedResult<GroupFileInfo>( socketResult );

			// 接收服务器操作结果
			OperateResult<byte, byte[]> receiveBack = await ReceiveMqttMessageAsync( socketResult.Content, 60_000 );
			if (!receiveBack.IsSuccess) return OperateResult.CreateFailedResult<GroupFileInfo>( receiveBack );

			socketResult.Content?.Close( );
			return OperateResult.CreateSuccessResult( JObject.Parse( Encoding.UTF8.GetString( receiveBack.Content2 ) ).ToObject<GroupFileInfo>( ) );
		}

		/// <inheritdoc cref="GetSubGroupFileInfos(string)"/>
		public async Task<OperateResult<GroupFileInfo[]>> GetSubGroupFileInfosAsync( string groups )
		{
			// connect server
			OperateResult<Socket> socketResult = await ConnectFileServerAsync( MqttControlMessage.FileFolderInfos, groups, null );
			if (!socketResult.IsSuccess) return OperateResult.CreateFailedResult<GroupFileInfo[]>( socketResult );

			// 接收服务器操作结果
			OperateResult<byte, byte[]> receiveBack = await ReceiveMqttMessageAsync( socketResult.Content, 60_000 );
			if (!receiveBack.IsSuccess) return OperateResult.CreateFailedResult<GroupFileInfo[]>( receiveBack );

			socketResult.Content?.Close( );
			return OperateResult.CreateSuccessResult( JArray.Parse( Encoding.UTF8.GetString( receiveBack.Content2 ) ).ToObject<GroupFileInfo[]>( ) );
		}
#endif
		#endregion

		#region Public Properties

		/// <summary>
		/// 获取或设置当前的连接信息，客户端将根据这个连接配置进行连接服务器，在连接之前需要设置相关的信息才有效。<br />
		/// To obtain or set the current connection information, the client will connect to the server according to this connection configuration. 
		/// Before connecting, the relevant information needs to be set to be effective.
		/// </summary>
		public MqttConnectionOptions ConnectionOptions
		{
			get => connectionOptions;
			set => connectionOptions = value;
		}

		/// <summary>
		/// 获取或设置使用字符串访问的时候，使用的编码信息，默认为UT8编码<br />
		/// Get or set the encoding information used when accessing with a string, the default is UT8 encoding
		/// </summary>
		public Encoding StringEncoding
		{
			get => stringEncoding;
			set => stringEncoding = value;
		}

		#endregion

		#region Private Member

		private SoftIncrementCount incrementCount;                            // 自增的数据id对象
		private MqttConnectionOptions connectionOptions;                      // 连接服务器时的配置信息
		private Encoding stringEncoding = Encoding.UTF8;                      // 使用字符串通信时的编码
		private RSACryptoServiceProvider cryptoServiceProvider = null;        // 账户验证加密对象
		private AesCryptography aesCryptography = null;                       // 数据请求加密对象

		#endregion

		#region Object Override

		/// <inheritdoc/>
		public override string ToString( ) => $"MqttSyncClient[{this.connectionOptions.IpAddress}:{this.connectionOptions.Port}]";

		#endregion
	}
}
