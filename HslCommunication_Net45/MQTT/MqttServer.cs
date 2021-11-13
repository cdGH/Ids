using HslCommunication.BasicFramework;
using HslCommunication.Core;
using HslCommunication.Core.Net;
using HslCommunication.Enthernet;
using HslCommunication.LogNet;
using HslCommunication.Reflection;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using HslCommunication.Core.Security;
#if !NET35 && !NET20
using System.Threading.Tasks;
#endif

namespace HslCommunication.MQTT
{
	/// <summary>
	/// 一个Mqtt的服务器类对象，本服务器支持发布订阅操作，支持从服务器强制推送数据，支持往指定的客户端推送，支持基于一问一答的远程过程调用（RPC）的数据交互，支持文件上传下载。根据这些功能从而定制化出满足各个场景的服务器，详细的使用说明可以参见代码api文档示例。<br />
	/// An Mqtt server class object. This server supports publish and subscribe operations, supports forced push data from the server, 
	/// supports push to designated clients, supports data interaction based on one-question-one-answer remote procedure calls (RPC), 
	/// and supports file upload and download . According to these functions, the server can be customized to meet various scenarios. 
	/// For detailed instructions, please refer to the code api document example.
	/// </summary>
	/// <remarks>
	/// 本MQTT服务器功能丰富，可以同时实现，用户名密码验证，在线客户端的管理，数据订阅推送，单纯的数据收发，心跳检测，订阅通配符，同步数据访问，文件上传，下载，删除，遍历，详细参照下面的示例说明<br />
	/// 通配符请查看<see cref="TopicWildcard"/>属性，规则参考：http://public.dhe.ibm.com/software/dw/webservices/ws-mqtt/mqtt-v3r1.html#appendix-a
	/// </remarks>
	/// <example>
	/// 最简单的使用，就是实例化，启动服务即可
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\MQTT\MqttServerSample.cs" region="Sample1" title="简单的实例化" />
	/// 当然了，我们可以稍微的复杂一点，加一个功能，验证连接的客户端操作
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\MQTT\MqttServerSample.cs" region="Sample2" title="增加验证" />
	/// 我们可以对ClientID，用户名，密码进行验证，那么我们可以动态修改client id么？比如用户名密码验证成功后，client ID我想设置为权限等级。
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\MQTT\MqttServerSample.cs" region="Sample2_1" title="动态修改Client ID" />
	/// 如果我想强制该客户端不能主动发布主题，可以这么操作。
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\MQTT\MqttServerSample.cs" region="Sample2_2" title="禁止发布主题" />
	/// 你也可以对clientid进行过滤验证，只要结果返回不是0，就可以了。接下来我们实现一个功能，所有客户端的发布的消息在控制台打印出来,
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\MQTT\MqttServerSample.cs" region="Sample3" title="打印所有发布" />
	/// 捕获客户端刚刚上线的时候，方便我们进行一些额外的操作信息。下面的意思就是返回一个数据，将数据发送到指定的会话内容上去
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\MQTT\MqttServerSample.cs" region="Sample4" title="客户端上线信息" />
	/// 下面演示如何从服务器端发布数据信息，包括多种发布的方法，消息是否驻留，详细看说明即可
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\MQTT\MqttServerSample.cs" region="Sample5" title="服务器发布" />
	/// 下面演示如何支持同步网络访问，当客户端是同步网络访问时，协议内容会变成HUSL，即被视为同步客户端，进行相关的操作，主要进行远程调用RPC，以及查询MQTT的主题列表。
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\MQTT\MqttServerSample.cs" region="Sample6" title="同步访问支持" />
	/// 如果需要查看在线信息，可以随时获取<see cref="OnlineCount"/>属性，如果需要查看报文信息，可以实例化日志，参考日志的说明即可。<br /><br />
	/// 针对上面同步网络访问，虽然比较灵活，但是什么都要自己控制，无疑增加了代码的复杂度，举个例子，当你的topic分类很多的时候，已经客户端协议多个参数的时候，需要大量的手动解析的代码，
	/// 影响代码美观，而且让代码更加的杂乱，除此之外，还有个巨大的麻烦，服务器提供了很多的topic处理程序（可以换个称呼，暴露的API接口），
	/// 客户端没法清晰的浏览到，需要查找服务器代码才能知晓，而且服务器更新了接口，客户端有需要同步查看服务器的代码才行，以及做权限控制也很麻烦。<br />
	/// 所以在Hsl里面的MQTT服务器，提供了注册API接口的功能，只需要一行注册代码，你的类的方法自动就会变为API解析，所有的参数都是同步解析的，如果你返回的是
	/// OperateResult&lt;T&gt;类型对象，还支持是否成功的结果报告，否则一律视为json字符串，返回给调用方。
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\MQTT\MqttServerSample.cs" region="Sample7" title="基于MQTT的RPC接口实现" />
	/// 如果需要查看在线信息，可以随时获取<see cref="OnlineCount"/>属性，如果需要查看报文信息，可以实例化日志，参考日志的说明即可。<br /><br />
	/// 最后介绍一下文件管理服务是如何启动的，在启动了文件管理服务之后，其匹配的客户端 <see cref="MqttSyncClient"/> 就可以上传下载，遍历文件了。
	/// 而服务器端做的就是启用服务，如果你需要一些更加自由的权限控制，比如某个账户只能下载，不能其他操作，都是可以实现的。更加多的示例参考DEMO程序。
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\MQTT\MqttServerSample.cs" region="Sample8" title="基于MQTT的文件管理服务启动" />
	/// </example>
	public class MqttServer : NetworkServerBase, IDisposable
	{
		#region Constructor

		/// <summary>
		/// 实例化一个MQTT协议的服务器<br />
		/// Instantiate a MQTT protocol server
		/// </summary>
		public MqttServer( RSACryptoServiceProvider providerServer = null )
		{
			statisticsDict        = new LogStatisticsDict( GenerateMode.ByEveryDay, 60 );
			retainKeys            = new Dictionary<string, MqttClientApplicationMessage>( );
			apiTopicServiceDict   = new Dictionary<string, MqttRpcApiInfo>( );
			keysLock              = new object( );
			rpcApiLock            = new object( );
			timerHeart            = new System.Threading.Timer( ThreadTimerHeartCheck, null, 2000, 10000 );

			// 文件相关
			dictionaryFilesMarks  = new Dictionary<string, FileMarkId>( );
			dictHybirdLock        = new object( );

			// 如果没有指定RSA密钥，就自动随机生成
			this.providerServer = providerServer ?? new RSACryptoServiceProvider( );
			// 生成随机AES密钥
			Random random = new Random( );
			byte[] buffer = new byte[16];
			random.NextBytes( buffer );
			string key = buffer.ToHexString( );
			this.aesCryptography = new AesCryptography( key );
		}

		#endregion

		#region NetServer Override
		/// <inheritdoc/>
#if NET35 || NET20
		protected override void ThreadPoolLogin( Socket socket, IPEndPoint endPoint )
#else
		protected async override void ThreadPoolLogin( Socket socket, IPEndPoint endPoint )
#endif
		{
#if NET35 || NET20
			OperateResult<byte, byte[]> readMqtt = ReceiveMqttMessage( socket, 10_000 );
#else
			OperateResult<byte, byte[]> readMqtt = await ReceiveMqttMessageAsync( socket, 10_000 );
#endif
			if (!readMqtt.IsSuccess) return;

			RSACryptoServiceProvider clientKey = null;
			if (readMqtt.Content1 == 0xFF)    // 客户端使用加密通信，需要事先进行交换密钥
			{
				try
				{
					clientKey = RSAHelper.CreateRsaProviderFromPublicKey( HslSecurity.ByteDecrypt( readMqtt.Content2 ) );

					// 交换RSA公共钥匙
					OperateResult send = Send( socket, MqttHelper.BuildMqttCommand( 0xFF, null, 
						HslSecurity.ByteEncrypt( clientKey.EncryptLargeData( providerServer.GetPEMPublicKey( ) ) ) ).Content );
					if (!send.IsSuccess) return;
				}
				catch( Exception ex )
				{
					LogNet?.WriteError( "创建客户端的公钥发生了异常！" + ex.Message );
					socket?.Close( );
					return;
				}

				// 正式的接收连接的账户密码数据信息
#if NET35 || NET20
				readMqtt = ReceiveMqttMessage( socket, 10_000 );
#else
				readMqtt = await ReceiveMqttMessageAsync( socket, 10_000 );
#endif
				if (!readMqtt.IsSuccess) return;
			}

			HandleMqttConnection( socket, endPoint, readMqtt, clientKey );
		}
#if NET35 || NET20
		private void SocketReceiveCallback( IAsyncResult ar )
#else
		private async void SocketReceiveCallback( IAsyncResult ar )
#endif
		{
			if (ar.AsyncState is MqttSession mqttSession)
			{
				try
				{
					mqttSession.MqttSocket.EndReceive( ar );
				}
				catch (Exception ex)
				{
					RemoveAndCloseSession( mqttSession, $"Socket EndReceive -> {ex.Message}" );
					return;
				}

				if(mqttSession.Protocol == "FILE") 
				{
					if (fileServerEnabled)
					{
#if NET35 || NET20
						HandleFileMessage( mqttSession );
#else
						await HandleFileMessageAsync( mqttSession );
#endif
					}
					RemoveAndCloseSession( mqttSession, string.Empty );
					return; 
				}

				OperateResult<byte, byte[]> readMqtt = null;
				if (mqttSession.Protocol == "MQTT")
				{
#if NET35 || NET20
					readMqtt = ReceiveMqttMessage( mqttSession.MqttSocket, 60_000 );
#else
					readMqtt = await ReceiveMqttMessageAsync( mqttSession.MqttSocket, 60_000 );
#endif
				}
				else
				{
#if NET35 || NET20
					readMqtt = ReceiveMqttMessage( mqttSession.MqttSocket, 60_000, new Action<long, long>( ( already, total ) =>
						SyncMqttReceiveProgressBack( mqttSession.MqttSocket, already, total ) ) );
#else
					readMqtt = await ReceiveMqttMessageAsync( mqttSession.MqttSocket, 60_000, new Action<long, long>( ( already, total ) => 
						SyncMqttReceiveProgressBack( mqttSession.MqttSocket, already, total ) ) );
#endif

				}
#if NET35 || NET20
				HandleWithReceiveMqtt( mqttSession, readMqtt );
#else
				await HandleWithReceiveMqtt( mqttSession, readMqtt );
#endif
			}
		}

		// 进度报告部分的内容
		private void SyncMqttReceiveProgressBack( Socket socket, long already, long total )
		{
			string topic = total > 0 ? (already * 100 / total).ToString( ) : "100";
			byte[] payload = new byte[16];
			BitConverter.GetBytes( already ).CopyTo( payload, 0 );
			BitConverter.GetBytes( total ).CopyTo(   payload, 8 );

			Send( socket, MqttHelper.BuildMqttCommand( MqttControlMessage.REPORTPROGRESS, 0x00, MqttHelper.BuildSegCommandByString( topic ), payload ).Content );
		}

		private void HandleMqttConnection( Socket socket, IPEndPoint endPoint, OperateResult<byte, byte[]> readMqtt, RSACryptoServiceProvider providerClient )
		{
			if (!readMqtt.IsSuccess) return;

			byte[] encrypt = readMqtt.Content2;
			if (providerClient != null)   // 如果使用了加密的模式，就先进行解密的操作
			{
				try
				{
					encrypt = providerServer.DecryptLargeData( encrypt );
				}
				catch (Exception ex)
				{
					LogNet?.WriteError( ToString( ), $"[{endPoint}] 解密客户端的登录数据异常！" + ex.Message ); ;
					socket?.Close( );
					return;
				}
			}

			OperateResult<int, MqttSession> check = CheckMqttConnection( readMqtt.Content1, encrypt, socket, endPoint );
			if (!check.IsSuccess)
			{
				LogNet?.WriteInfo( ToString( ), check.Message );
				socket?.Close( );
				return;
			}

			if (check.Content1 != 0)
			{
				Send( socket, MqttHelper.BuildMqttCommand( MqttControlMessage.CONNACK, 0x00, null, new byte[] { 0x00, (byte)check.Content1 } ).Content );
				socket?.Close( );
				return;
			}
			else
			{
				check.Content2.AesCryptography = providerClient != null; // 标记该客户端是否加密
				if (providerClient == null)
					Send( socket, MqttHelper.BuildMqttCommand( MqttControlMessage.CONNACK, 0x00, null, new byte[] { 0x00, 0x00 } ).Content );
				else
				{
					// 传送回AES的密钥信息
					byte[] aesKey = providerClient.Encrypt( Encoding.UTF8.GetBytes( aesCryptography.Key ), false );
					Send( socket, MqttHelper.BuildMqttCommand( MqttControlMessage.CONNACK, 0x00, new byte[] { 0x00, 0x00 }, aesKey ).Content );
				}
			}

			try
			{
				socket.BeginReceive( new byte[0], 0, 0, SocketFlags.None, new AsyncCallback( SocketReceiveCallback ), check.Content2 );
				AddMqttSession( check.Content2 );
			}
			catch (Exception ex)
			{
				LogNet?.WriteDebug( ToString( ), $"Client Online Exception : " + ex.Message );
				return;
			}

			if (check.Content2.Protocol == "MQTT") OnClientConnected?.Invoke( check.Content2 );
		}

		private OperateResult<int, MqttSession> CheckMqttConnection(byte mqttCode, byte[] content, Socket socket, IPEndPoint endPoint )
		{
			if (mqttCode >> 4 != MqttControlMessage.CONNECT) return new OperateResult<int, MqttSession>( "Client Send Faied, And Close!" );
			if (content.Length < 10) return new OperateResult<int, MqttSession>( $"Receive Data Too Short:{SoftBasic.ByteToHexString( content, ' ' )}" );

			string protocol = Encoding.ASCII.GetString( content, 2, 4 );
			if (!(protocol == "MQTT" || protocol == "HUSL" || protocol == "FILE")) return new OperateResult<int, MqttSession>( $"Not Mqtt Client Connection" );

			try
			{
				int    index        = 10;
				string clientId     = MqttHelper.ExtraMsgFromBytes( content, ref index );
				string willTopic    = ((content[7] & 0x04) == 0x04) ? MqttHelper.ExtraMsgFromBytes( content, ref index ) : string.Empty;
				string willMessage  = ((content[7] & 0x04) == 0x04) ? MqttHelper.ExtraMsgFromBytes( content, ref index ) : string.Empty;
				string userName     = ((content[7] & 0x80) == 0x80) ? MqttHelper.ExtraMsgFromBytes( content, ref index ) : string.Empty;
				string password     = ((content[7] & 0x40) == 0x40) ? MqttHelper.ExtraMsgFromBytes( content, ref index ) : string.Empty;
				int    keepAlive    =   content[8] * 256 + content[9];

				MqttSession mqttSession = new MqttSession( endPoint, protocol )
				{
					MqttSocket = socket,
					ClientId = clientId,
					UserName = userName,
				};
				int returnCode = ClientVerification != null ? ClientVerification( mqttSession, clientId, userName, password ) : 0;

				if (keepAlive > 0) mqttSession.ActiveTimeSpan = TimeSpan.FromSeconds( keepAlive );
				return OperateResult.CreateSuccessResult( returnCode, mqttSession );
			}
			catch (Exception ex)
			{
				return new OperateResult<int, MqttSession>( $"Client Online Exception : " + ex.Message );
			}
		}
#if NET20 || NET35
		private void HandleWithReceiveMqtt( MqttSession mqttSession, OperateResult<byte, byte[]> readMqtt )
#else
		private async Task HandleWithReceiveMqtt( MqttSession mqttSession, OperateResult<byte, byte[]> readMqtt )
#endif
		{
			if (!readMqtt.IsSuccess) { RemoveAndCloseSession( mqttSession, readMqtt.Message ); return; }        // 接收失败，下线

			byte   code = readMqtt.Content1;
			byte[] data = readMqtt.Content2;

			try
			{
				if (code >> 4 != MqttControlMessage.DISCONNECT) mqttSession.MqttSocket.BeginReceive( new byte[0], 0, 0, SocketFlags.None, new AsyncCallback( SocketReceiveCallback ), mqttSession );
				else { RemoveAndCloseSession( mqttSession, string.Empty ); return; }
			}
			catch(Exception ex)
			{
				RemoveAndCloseSession( mqttSession, "HandleWithReceiveMqtt:" + ex.Message ); return;               // 发生了异常
			}

			mqttSession.ActiveTime = DateTime.Now;                         // 更新会话激活时间

			// 处理数据
#if NET20 || NET35
			if (mqttSession.Protocol != "MQTT") DealWithPublish( mqttSession, code, data );
#else
			if (mqttSession.Protocol != "MQTT") await DealWithPublish( mqttSession, code, data );
#endif
			else
			{
				if (code >> 4 == MqttControlMessage.PUBLISH)
				{
#if NET20 || NET35
					DealWithPublish( mqttSession, code, data );
#else
					await DealWithPublish( mqttSession, code, data );
#endif
				}
				else if (code >> 4 == MqttControlMessage.PUBACK) { }   // Qos1发布确认
				else if (code >> 4 == MqttControlMessage.PUBREC) { }   // 发布收到
				else if (code >> 4 == MqttControlMessage.PUBREL)       // 发布释放
					Send( mqttSession.MqttSocket, MqttHelper.BuildMqttCommand( MqttControlMessage.PUBCOMP, 0x00, null, data ).Content );
				else if (code >> 4 == MqttControlMessage.SUBSCRIBE)    // 订阅消息
					DealWithSubscribe( mqttSession, code, data );
				else if (code >> 4 == MqttControlMessage.UNSUBSCRIBE)  // 取消订阅
					DealWithUnSubscribe( mqttSession, code, data );
				else if (code >> 4 == MqttControlMessage.PINGREQ)      // 心跳请求
					Send( mqttSession.MqttSocket, MqttHelper.BuildMqttCommand( MqttControlMessage.PINGRESP, 0x00, null, null ).Content );
			}
		}

		/// <inheritdoc/>
		protected override void StartInitialization( )
		{

		}

		/// <inheritdoc/>
		protected override void CloseAction( )
		{
			base.CloseAction( );

			lock (sessionsLock)
			{
				for (int i = 0; i < mqttSessions.Count; i++)
				{
					mqttSessions[i].MqttSocket?.Close( );
				}
				mqttSessions.Clear( );
			}
		}

		private void ThreadTimerHeartCheck( object obj )
		{
			MqttSession[] snapshoot = null;
			lock (sessionsLock)
				snapshoot = mqttSessions.ToArray( );
			if (snapshoot != null && snapshoot.Length > 0)
			{
				for (int i = 0; i < snapshoot.Length; i++)
				{
					if (snapshoot[i].Protocol == "MQTT" && ( DateTime.Now - snapshoot[i].ActiveTime) > snapshoot[i].ActiveTimeSpan)
					{
						// 心跳超时
						RemoveAndCloseSession( snapshoot[i], $"Thread Timer Heart Check failed:" + SoftBasic.GetTimeSpanDescription( DateTime.Now - snapshoot[i].ActiveTime ) );
					}
				}
			}

		}
#if NET20 || NET35
		private void DealWithPublish( MqttSession session, byte code, byte[] data )
#else
		private async Task DealWithPublish( MqttSession session, byte code, byte[] data )
#endif
		{
			OperateResult<MqttClientApplicationMessage> messageResult = MqttHelper.ParseMqttClientApplicationMessage( session, code, data, this.aesCryptography );
			if (!messageResult.IsSuccess) { RemoveAndCloseSession( session, messageResult.Message ); return; }

			MqttClientApplicationMessage mqttClientApplicationMessage = messageResult.Content;
			if (session.Protocol == "MQTT")
			{
				MqttQualityOfServiceLevel mqttQuality = mqttClientApplicationMessage.QualityOfServiceLevel;
				if (mqttQuality == MqttQualityOfServiceLevel.AtLeastOnce)
					Send( session.MqttSocket, MqttHelper.BuildMqttCommand( MqttControlMessage.PUBACK, 0x00, null, MqttHelper.BuildIntBytes( mqttClientApplicationMessage.MsgID ) ).Content );
				else if (mqttQuality == MqttQualityOfServiceLevel.ExactlyOnce)
					Send( session.MqttSocket, MqttHelper.BuildMqttCommand( MqttControlMessage.PUBREC, 0x00, null, MqttHelper.BuildIntBytes( mqttClientApplicationMessage.MsgID ) ).Content );

				if (session.ForbidPublishTopic) return;
				OnClientApplicationMessageReceive?.Invoke( session, mqttClientApplicationMessage );

				if (mqttQuality != MqttQualityOfServiceLevel.OnlyTransfer && !mqttClientApplicationMessage.IsCancelPublish)
				{
					PublishTopicPayload( mqttClientApplicationMessage.Topic, mqttClientApplicationMessage.Payload, false );
					if (mqttClientApplicationMessage.Retain) RetainTopicPayload( mqttClientApplicationMessage.Topic, mqttClientApplicationMessage );
				}
			}
			else
			{
				if (code >> 4 == MqttControlMessage.PUBLISH)
				{
					// 先检查有没有注册的服务
					string apiName = mqttClientApplicationMessage.Topic.Trim( '/' );
					MqttRpcApiInfo apiInformation = GetMqttRpcApiInfo( apiName );
					if (apiInformation == null)
					{
						OnClientApplicationMessageReceive?.Invoke( session, mqttClientApplicationMessage );
					}
					else
					{
						// 存在相关的服务，优先调度服务
						DateTime dateTime = DateTime.Now;
#if NET20 || NET35
						OperateResult<string> result = MqttHelper.HandleObjectMethod( session, mqttClientApplicationMessage, apiInformation );
#else
						OperateResult<string> result = await MqttHelper.HandleObjectMethod( session, mqttClientApplicationMessage, apiInformation );
#endif
						double timeSpend = Math.Round( (DateTime.Now - dateTime).TotalSeconds, 5 );
						apiInformation.CalledCountAddOne( (long)(timeSpend * 100_000) );
						this.statisticsDict.StatisticsAdd( apiInformation.ApiTopic, 1 );
						LogNet?.WriteDebug( ToString( ), $"{session} RPC: [{mqttClientApplicationMessage.Topic}] Spend:[{timeSpend * 1000:F2} ms] Count:[{apiInformation.CalledCount}] Return:[{result.IsSuccess}]" );
						ReportOperateResult( session, result );
					}
				}
				else if (code >> 4 == MqttControlMessage.SUBSCRIBE)
				{
					// 当为RPC同步网络访问的时候，就是请求API接口列表内容
					ReportOperateResult( session, OperateResult.CreateSuccessResult( JArray.FromObject( GetAllMqttRpcApiInfo( ) ).ToString( ) ) );
				}
				else if (code >> 4 == MqttControlMessage.PUBACK)
				{
					// 当为RPC同步网络访问的时候，就是获取服务器的发布过的Topic列表
					PublishTopicPayload( session, "", HslProtocol.PackStringArrayToByte( GetAllRetainTopics( ) ) );
				}
				else if (code >> 4 == MqttControlMessage.PUBREL)
				{
					// 当为RPC同步网络访问的时候，就是获取某个API调用次数的数组信息
					long[] logs = string.IsNullOrEmpty( mqttClientApplicationMessage.Topic ) ?
						LogStatistics.LogStat.GetStatisticsSnapshot( ) :
						LogStatistics.GetStatisticsSnapshot( mqttClientApplicationMessage.Topic );
					if (logs == null)
						ReportOperateResult( session, new OperateResult<string>( $"{session} RPC:{mqttClientApplicationMessage.Topic} has no data or not exist." ) );
					else
						ReportOperateResult( session, OperateResult.CreateSuccessResult( logs.ToArrayString( ) ) );
				}
				else if (code >> 4 == MqttControlMessage.PUBREC)
				{
					// 当为RPC同步网络访问的时候，读取指定的Topic信息
					lock (keysLock)
					{
						if (retainKeys.ContainsKey( mqttClientApplicationMessage.Topic ))
						{
							byte[] responsePayload = Encoding.UTF8.GetBytes( retainKeys[mqttClientApplicationMessage.Topic].ToJsonString( ) );
							PublishTopicPayload( session, mqttClientApplicationMessage.Topic, responsePayload );
						}
						else
						{
							ReportOperateResult( session, StringResources.Language.KeyIsNotExist );
						}
					}
				}
			}
		}

		/// <summary>
		/// 将消息进行驻留到内存词典，方便进行其他的功能操作。
		/// </summary>
		/// <param name="topic">消息的主题</param>
		/// <param name="payload">当前的数据负载</param>
		private void RetainTopicPayload( string topic, byte[] payload )
		{
			MqttClientApplicationMessage mqttClientApplicationMessage = new MqttClientApplicationMessage( )
			{
				ClientId              = "MqttServer",
				QualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce,
				Retain                = true,
				Topic                 = topic,
				UserName              = "MqttServer",
				Payload               = payload,
			};

			lock (keysLock)
			{
				if (retainKeys.ContainsKey( topic ))
				{
					retainKeys[topic] = mqttClientApplicationMessage;
				}
				else
				{
					retainKeys.Add( topic, mqttClientApplicationMessage );
				}
			}
		}

		/// <summary>
		/// 将消息进行驻留到内存词典，方便进行其他的功能操作。
		/// </summary>
		/// <param name="topic">消息的主题</param>
		/// <param name="message">当前的Mqtt消息</param>
		private void RetainTopicPayload( string topic, MqttClientApplicationMessage message )
		{
			lock (keysLock)
			{
				if (retainKeys.ContainsKey( topic ))
				{
					retainKeys[topic] = message;
				}
				else
				{
					retainKeys.Add( topic, message );
				}
			}
		}

		private void DealWithSubscribe( MqttSession session, byte code, byte[] data )
		{
			int msgId = 0;
			int index = 0;

			msgId = MqttHelper.ExtraIntFromBytes( data, ref index );
			List<string> topics = new List<string>( );
			try
			{
				while (index < data.Length - 1)
				{
					topics.Add( MqttHelper.ExtraSubscribeMsgFromBytes( data, ref index ) );
				}
			}
			catch(Exception ex)
			{
				this.LogNet?.WriteError( ToString( ), $"{session} DealWithSubscribe exception: " + ex.Message );
				return;
			}

			// 返回订阅成功
			if (index < data.Length)
			{
				Send( session.MqttSocket, MqttHelper.BuildMqttCommand( MqttControlMessage.SUBACK, 0x00, MqttHelper.BuildIntBytes( msgId ), new byte[] { data[index] } ).Content );
			}
			else
			{
				Send( session.MqttSocket, MqttHelper.BuildMqttCommand( MqttControlMessage.SUBACK, 0x00, null, MqttHelper.BuildIntBytes( msgId ) ).Content );
			}

			lock (keysLock)
			{
				if (topicWildcard)
				{
					foreach (var item in retainKeys)
					{
						for (int i = 0; i < topics.Count; i++)
						{
							if(MqttHelper.CheckMqttTopicWildcards( item.Key, topics[i] ))
							{
								Send( session.MqttSocket, MqttHelper.BuildPublishMqttCommand( item.Key, item.Value.Payload,
									session.AesCryptography ? this.aesCryptography : null ).Content );
							}
						}
					}
				}
				else
				{
					for (int i = 0; i < topics.Count; i++)
					{
						if (retainKeys.ContainsKey( topics[i] ))
							Send( session.MqttSocket, MqttHelper.BuildPublishMqttCommand( topics[i], retainKeys[topics[i]].Payload,
								session.AesCryptography ? this.aesCryptography : null ).Content );
					}
				}
			}
			// 添加订阅信息
			session.AddSubscribe( topics.ToArray( ) );
			LogNet?.WriteDebug( ToString( ), session.ToString( ) + " Subscribe: " + topics.ToArray( ).ToArrayString( ) );
		}

		private void DealWithUnSubscribe( MqttSession session, byte code, byte[] data )
		{
			int msgId = 0;
			int index = 0;

			msgId = MqttHelper.ExtraIntFromBytes( data, ref index );
			List<string> topics = new List<string>( );
			while (index < data.Length)
			{
				topics.Add( MqttHelper.ExtraMsgFromBytes( data, ref index ) );
			}

			// 返回取消订阅成功
			Send( session.MqttSocket, MqttHelper.BuildMqttCommand( MqttControlMessage.UNSUBACK, 0x00, null, MqttHelper.BuildIntBytes( msgId ) ).Content );

			// 添加订阅信息
			session.RemoveSubscribe( topics.ToArray( ) );
			LogNet?.WriteDebug( ToString( ), session.ToString( ) + " UnSubscribe: " + topics.ToArray( ).ToArrayString( ) );
		}

#endregion

#region Publish Message

		/// <summary>
		/// 向指定的客户端发送主题及负载数据<br />
		/// Sends the topic and payload data to the specified client
		/// </summary>
		/// <param name="session">会话内容</param>
		/// <param name="topic">主题</param>
		/// <param name="payload">消息内容</param>
		public void PublishTopicPayload( MqttSession session, string topic, byte[] payload )
		{
			OperateResult send = Send( session.MqttSocket, MqttHelper.BuildPublishMqttCommand( topic, payload, session.AesCryptography ? this.aesCryptography : null ).Content );
			if (!send.IsSuccess) LogNet?.WriteError( ToString( ), $"{session} PublishTopicPayload Failed:" + send.Message );
		}


		private void PublishTopicPayloadHelper( string topic, byte[] payload, bool retain, Func<MqttSession, bool> check )
		{
			lock (sessionsLock)
			{
				for (int i = 0; i < mqttSessions.Count; i++)
				{
					MqttSession session = mqttSessions[i];
					// 向订阅消息的客户端进行发送数据，构建数据缓存，不再重复创建报文，提升发布性能
					byte[] sendData = null;
					byte[] sendEncrypt = null;
					if (session.Protocol == "MQTT" && check( session ))
					{
						if (session.AesCryptography)
						{
							if (sendEncrypt == null) sendEncrypt = MqttHelper.BuildPublishMqttCommand( topic, payload, this.aesCryptography ).Content;
						}
						else
						{
							if (sendData == null) sendData = MqttHelper.BuildPublishMqttCommand( topic, payload, null ).Content;
						}
						OperateResult send = Send( session.MqttSocket, session.AesCryptography ? sendEncrypt : sendData );
						if (!send.IsSuccess) LogNet?.WriteError( ToString( ), $"{session} PublishTopicPayload Failed:" + send.Message );
					}
				}
			}
			if (retain) RetainTopicPayload( topic, payload );
		}

		/// <summary>
		/// 从服务器向订阅了指定的主题的客户端发送消息，默认消息不驻留<br />
		/// Sends a message from the server to a client that subscribes to the specified topic; the default message does not retain
		/// </summary>
		/// <param name="topic">主题</param>
		/// <param name="payload">消息内容</param>
		/// <param name="retain">指示消息是否驻留</param>
		public void PublishTopicPayload( string topic, byte[] payload, bool retain = true )
		{
			PublishTopicPayloadHelper( topic, payload, retain, ( session ) => session.IsClientSubscribe( topic, topicWildcard ) );
		}

		/// <summary>
		/// 向所有的客户端强制发送主题及负载数据，默认消息不驻留<br />
		/// Send subject and payload data to all clients compulsively, and the default message does not retain
		/// </summary>
		/// <param name="topic">主题</param>
		/// <param name="payload">消息内容</param>
		/// <param name="retain">指示消息是否驻留</param>
		public void PublishAllClientTopicPayload( string topic, byte[] payload, bool retain = false )
		{
			PublishTopicPayloadHelper( topic, payload, retain, ( session ) => true );
		}

		/// <summary>
		/// 向指定的客户端ID强制发送消息，默认消息不驻留<br />
		/// Forces a message to the specified client ID, and the default message does not retain
		/// </summary>
		/// <param name="clientId">指定的客户端ID信息</param>
		/// <param name="topic">主题</param>
		/// <param name="payload">消息内容</param>
		/// <param name="retain">指示消息是否驻留</param>
		public void PublishTopicPayload( string clientId, string topic, byte[] payload, bool retain = false )
		{
			PublishTopicPayloadHelper( topic, payload, retain, ( session ) => session.ClientId == clientId );
		}

#endregion

#region Report Progress

		/// <summary>
		/// 向客户端发布一个进度报告的信息，仅用于同步网络的时候才支持进度报告，将进度及消息发送给客户端，比如你的服务器需要分成5个部分完成，可以按照百分比提示给客户端当前服务器发生了什么<br />
		/// Publish the information of a progress report to the client. The progress report is only supported when the network is synchronized. 
		/// The progress and the message are sent to the client. For example, your server needs to be divided into 5 parts to complete. 
		/// You can prompt the client according to the percentage. What happened to the server
		/// </summary>
		/// <param name="session">当前的网络会话</param>
		/// <param name="topic">回发客户端的关键数据，可以是百分比字符串，甚至是自定义的任意功能</param>
		/// <param name="payload">数据消息</param>
		public void ReportProgress( MqttSession session, string topic, string payload )
		{
			if (session.Protocol == "HUSL")
			{
				payload = payload ?? string.Empty;
				OperateResult send = Send( session.MqttSocket, MqttHelper.BuildMqttCommand( MqttControlMessage.REPORTPROGRESS, 0x00, MqttHelper.BuildSegCommandByString( topic ), Encoding.UTF8.GetBytes( payload ) ).Content );
				if (!send.IsSuccess)
					LogNet?.WriteError( ToString( ), $"{session} PublishTopicPayload Failed:" + send.Message );
			}
			else
			{
				throw new Exception( "ReportProgress only support sync communication" );
			}
		}

		/// <summary>
		/// 向客户端发布一个失败的操作信息，仅用于同步网络的时候反馈失败结果，将错误的信息反馈回客户端，客户端就知道服务器发生了什么，为什么反馈失败。<br />
		/// Publish a failed operation information to the client, which is only used to feed back the failure result when synchronizing the network. 
		/// If the error information is fed back to the client, the client will know what happened to the server and why the feedback failed.
		/// </summary>
		/// <param name="session">当前的网络会话</param>
		/// <param name="message">错误的消息文本信息</param>
		public void ReportOperateResult( MqttSession session, string message )
		{
			ReportOperateResult( session, new OperateResult<string>( message ) );
		}

		/// <summary>
		/// 向客户端发布一个操作结果的信息，仅用于同步网络的时候反馈操作结果，该操作可能成功，可能失败，客户端就知道服务器发生了什么，以及结果如何。<br />
		/// Publish an operation result information to the client, which is only used to feed back the operation result when synchronizing the network. 
		/// The operation may succeed or fail, and the client knows what happened to the server and the result.
		/// </summary>
		/// <param name="session">当前的网络会话</param>
		/// <param name="result">结果对象内容</param>
		public void ReportOperateResult( MqttSession session, OperateResult<string> result )
		{
			if (session.Protocol == "HUSL")
			{
				if (result.IsSuccess)
				{
					byte[] back = string.IsNullOrEmpty( result.Content ) ? new byte[0] : Encoding.UTF8.GetBytes( result.Content );
					PublishTopicPayload( session, result.ErrorCode.ToString( ), back );
				}
				else
				{
					OperateResult send = Send( session.MqttSocket, MqttHelper.BuildMqttCommand(
						MqttControlMessage.FAILED, 0x00, MqttHelper.BuildSegCommandByString( result.ErrorCode.ToString( ) ), 
						string.IsNullOrEmpty( result.Message ) ? new byte[0] : Encoding.UTF8.GetBytes( result.Message ),
						session.AesCryptography ? this.aesCryptography : null ).Content );
					if (!send.IsSuccess) LogNet?.WriteError( ToString( ), $"{session} PublishTopicPayload Failed:" + send.Message );
				}
			}
			else
			{
				throw new Exception( "Report Result Message only support sync communication, client is MqttSyncClient" );
			}
		}

		/// <summary>
		/// 使用指定的对象来返回网络的API接口，前提是传入的数据为json参数，返回的数据为 <c>OperateResult&lt;string&gt;</c> 数据，详细参照说明<br />
		/// Use the specified object to return the API interface of the network, 
		/// provided that the incoming data is json parameters and the returned data is <c>OperateResult&lt;string&gt;</c> data, 
		/// please refer to the description for details
		/// </summary>
		/// <param name="session">当前的会话内容</param>
		/// <param name="message">客户端发送的消息，其中的payload将会解析为一个json字符串，然后提取参数信息。</param>
		/// <param name="apiObject">当前的对象的内容信息</param>
#if NET20 || NET35
		public void ReportObjectApiMethod( MqttSession session, MqttClientApplicationMessage message, object apiObject )
#else
		public async Task ReportObjectApiMethod( MqttSession session, MqttClientApplicationMessage message, object apiObject )
#endif
		{
			if (session.Protocol == "HUSL")
			{
#if NET20 || NET35
				ReportOperateResult( session, MqttHelper.HandleObjectMethod( session, message, apiObject ) );
#else
				ReportOperateResult( session, await MqttHelper.HandleObjectMethod( session, message, apiObject ) );
#endif
			}
			else
			{
				throw new Exception( "Report Result Message only support sync communication, client is MqttSyncClient" );
			}
		}

#endregion

#region Mqtt RPC Support

		private Dictionary<string, MqttRpcApiInfo> apiTopicServiceDict;
		private object rpcApiLock;

		private MqttRpcApiInfo GetMqttRpcApiInfo( string apiTopic )
		{
			MqttRpcApiInfo apiInformation = null;
			lock (rpcApiLock)
			{
				if (apiTopicServiceDict.ContainsKey( apiTopic )) apiInformation = apiTopicServiceDict[apiTopic];
			}
			return apiInformation;
		}

		/// <summary>
		/// 获取当前所有注册的RPC接口信息，将返回一个数据列表。<br />
		/// Get all currently registered RPC interface information, and a data list will be returned.
		/// </summary>
		/// <returns>信息列表</returns>
		public MqttRpcApiInfo[] GetAllMqttRpcApiInfo( )
		{
			MqttRpcApiInfo[] array = null;
			lock (rpcApiLock)
			{
				array = apiTopicServiceDict.Values.ToArray( );
			}
			return array;
		}

		/// <summary>
		/// 注册一个RPC的服务接口，可以指定当前的控制器名称，以及提供RPC服务的原始对象，指定统一的权限控制。<br />
		/// Register an RPC service interface, you can specify the current controller name, 
		/// and the original object that provides the RPC service, Specify unified access control
		/// </summary>
		/// <param name="api">前置的接口信息，可以理解为MVC模式的控制器</param>
		/// <param name="obj">原始对象信息</param>
		/// <param name="permissionAttribute">统一的权限访问配置，将会覆盖单个方法的权限控制。</param>
		public void RegisterMqttRpcApi( string api, object obj, HslMqttPermissionAttribute permissionAttribute )
		{
			lock (rpcApiLock)
			{
				foreach (var item in MqttHelper.GetSyncServicesApiInformationFromObject( api, obj, permissionAttribute ))
				{
					apiTopicServiceDict.Add( item.ApiTopic, item );
				}
			}
		}

		/// <summary>
		/// 注册一个RPC的服务接口，可以指定当前的控制器名称，以及提供RPC服务的原始对象<br />
		/// Register an RPC service interface, you can specify the current controller name, 
		/// and the original object that provides the RPC service
		/// </summary>
		/// <param name="api">前置的接口信息，可以理解为MVC模式的控制器</param>
		/// <param name="obj">原始对象信息</param>
		public void RegisterMqttRpcApi( string api, object obj )
		{
			lock (rpcApiLock)
			{
				foreach (var item in MqttHelper.GetSyncServicesApiInformationFromObject( api, obj ))
				{
					apiTopicServiceDict.Add( item.ApiTopic, item );
				}
			}
		}

		/// <inheritdoc cref="RegisterMqttRpcApi(string, object)"/>
		public void RegisterMqttRpcApi( object obj )
		{
			lock (rpcApiLock)
			{
				foreach (var item in MqttHelper.GetSyncServicesApiInformationFromObject( obj ))
				{
					apiTopicServiceDict.Add( item.ApiTopic, item );
				}
			}
		}

#endregion

#region Mqtt File Server

		private readonly Dictionary<string, FileMarkId> dictionaryFilesMarks;                 // 所有文件操作的词典锁
		private readonly object dictHybirdLock;                                               // 词典的锁
		private string filesDirectoryPath = null;                                             // 文件的存储路径
		private bool fileServerEnabled = false;                                               // 文件引擎是否启动，如果不启动，则无法使用
		private Dictionary<string, GroupFileContainer> m_dictionary_group_marks = new Dictionary<string, GroupFileContainer>( );
		private SimpleHybirdLock group_marks_lock = new SimpleHybirdLock( );
		private MqttFileMonitor fileMonitor = new MqttFileMonitor( );

		/// <summary>
		/// 启动文件服务功能，协议头为FILE，需要指定服务器存储的文件路径<br />
		/// Start the file service function, the protocol header is FILE, you need to specify the file path stored by the server
		/// </summary>
		/// <param name="filePath">文件的存储路径</param>
		public void UseFileServer( string filePath )
		{
			filesDirectoryPath   = filePath;
			fileServerEnabled    = true;
			CheckFolderAndCreate( );
		}

		/// <summary>
		/// 关闭文件服务功能
		/// </summary>
		public void CloseFileServer( )
		{
			fileServerEnabled = false;
		}

		/// <summary>
		/// 获取当前的针对文件夹的文件管理容器的数量<br />
		/// Get the current number of file management containers for the folder
		/// </summary>
		[HslMqttApi( Description = "Get the current number of file management containers for the folder" )]
		public int GroupFileContainerCount( ) => m_dictionary_group_marks.Count;

		/// <summary>
		/// 获取当前实时的文件上传下载的监控信息，操作的客户端信息，文件分类，文件名，上传或下载的速度等<br />
		/// Obtain current real-time file upload and download monitoring information, operating client information, file classification, file name, upload or download speed, etc.
		/// </summary>
		/// <returns>文件的监控信息</returns>
		[HslMqttApi( Description = "Obtain current real-time file upload and download monitoring information, operating client information, file classification, file name, upload or download speed, etc." )]
		public MqttFileMonitorItem[] GetMonitorItemsSnapShoot( ) => fileMonitor.GetMonitorItemsSnapShoot( );

		/// <summary>
		/// 当客户端进行文件操作时，校验客户端合法性的委托，操作码具体查看<seealso cref="MqttControlMessage"/>的常量值<br />
		/// When client performing file operations, verify the legitimacy of the client, and check the constant value of <seealso cref="MqttControlMessage"/> for the operation code.
		/// </summary>
		/// <param name="session">会话状态</param>
		/// <param name="code">操作码</param>
		/// <param name="groups">分类信息</param>
		/// <param name="fileNames">文件名</param>
		/// <returns>是否成功</returns>
		public delegate OperateResult FileOperateVerificationDelegate( MqttSession session, byte code, string[] groups, string[] fileNames );

		/// <summary>
		/// 当客户端进行文件操作时，校验客户端合法性的事件，操作码具体查看<seealso cref="MqttControlMessage"/>的常量值<br />
		/// When client performing file operations, it is an event to verify the legitimacy of the client. For the operation code, check the constant value of <seealso cref="MqttControlMessage"/>
		/// </summary>
		public event FileOperateVerificationDelegate FileOperateVerification;

		private bool CheckPathAndFilenameLegal( string input )
		{
			return input.Contains( ":" ) || input.Contains( "?" ) || input.Contains( "*" ) ||
				input.Contains( "/" ) || input.Contains( "\\" ) || input.Contains( "\"" ) ||
				input.Contains( "<" ) || input.Contains( ">" ) || input.Contains( "|" );
		}

#if NET20 || NET35
		private void HandleFileMessage( MqttSession session )
#else
		private async Task HandleFileMessageAsync( MqttSession session )
#endif
		{
			// 接收文件路径分类
#if NET20 || NET35
			OperateResult<byte, byte[]> receiveGroupInfo = ReceiveMqttMessage( session.MqttSocket, 60_000, null );
#else
			OperateResult<byte, byte[]> receiveGroupInfo = await ReceiveMqttMessageAsync( session.MqttSocket, 60_000, null );
#endif
			if (!receiveGroupInfo.IsSuccess) return;
			string[] groupInfo = HslProtocol.UnPackStringArrayFromByte( receiveGroupInfo.Content2 );

			// 接收操作命令和文件名称信息
#if NET20 || NET35
			OperateResult<byte, byte[]> receiveFileNames = ReceiveMqttMessage( session.MqttSocket, 60_000, null );
#else
			OperateResult<byte, byte[]> receiveFileNames = await ReceiveMqttMessageAsync( session.MqttSocket, 60_000, null );
#endif
			if (!receiveFileNames.IsSuccess) return;
			string[] fileNames = HslProtocol.UnPackStringArrayFromByte( receiveFileNames.Content2 );

			// 校验路径和文件名，不能带有特殊字符，防止路径攻击
			for (int i = 0; i < groupInfo.Length; i++)
			{
				if (CheckPathAndFilenameLegal( groupInfo[i] ))
				{
					Send( session.MqttSocket, MqttHelper.BuildMqttCommand( MqttControlMessage.FAILED, null, HslHelper.GetUTF8Bytes( "Path Invalid, not include ':', '?'" ) ).Content );
					RemoveAndCloseSession( session, "CheckPathAndFilenameLegal:" + groupInfo[i] );
					return;
				}
			}
			for (int i = 0; i < fileNames.Length; i++)
			{
				if (CheckPathAndFilenameLegal( fileNames[i] ))
				{
					Send( session.MqttSocket, MqttHelper.BuildMqttCommand( MqttControlMessage.FAILED, null, HslHelper.GetUTF8Bytes( "FileName Invalid, not include '\\/:*?\"<>|'" ) ).Content );
					RemoveAndCloseSession( session, "CheckPathAndFilenameLegal:" + fileNames[i] );
					return;
				}
			}

			// 当前的操作进行校验
			OperateResult opLegal = FileOperateVerification?.Invoke( session, receiveFileNames.Content1, groupInfo, fileNames );
			if (opLegal == null) opLegal = OperateResult.CreateSuccessResult( );

			// 校验失败，或是回发消息失败都关闭连接
#if NET20 || NET35
			OperateResult sendLegal = Send( session.MqttSocket, MqttHelper.BuildMqttCommand(
				opLegal.IsSuccess ? MqttControlMessage.FileNoSense : MqttControlMessage.FAILED, null, HslHelper.GetUTF8Bytes( opLegal.Message ) ).Content );
#else
			OperateResult sendLegal = await SendAsync( session.MqttSocket, MqttHelper.BuildMqttCommand(
				opLegal.IsSuccess ? MqttControlMessage.FileNoSense : MqttControlMessage.FAILED, null, HslHelper.GetUTF8Bytes( opLegal.Message ) ).Content );
#endif
			if (!opLegal.IsSuccess) { RemoveAndCloseSession( session, "FileOperateVerification:" + opLegal.Message ); return; }
			if (!sendLegal.IsSuccess) { RemoveAndCloseSession( session, "FileOperate SendLegal:" + sendLegal.Message ); return; }

			string relativeName = GetRelativeFileName( groupInfo, fileNames?.Length > 0 ? fileNames[0] : string.Empty );
			if (receiveFileNames.Content1 == MqttControlMessage.FileDownload)
			{
				string fileName = fileNames[0];
				// 先获取文件的真实名称
				string guidName = TransformFactFileName( groupInfo, fileName );
				// 获取文件操作锁
				FileMarkId fileMarkId = GetFileMarksFromDictionaryWithFileName( guidName );
				fileMarkId.EnterReadOperator( );
				DateTime dateTimeStart = DateTime.Now;
				// 监控文件的下载信息
				MqttFileMonitorItem monitorItem = new MqttFileMonitorItem
				{
					EndPoint = session.EndPoint,
					ClientId = session.ClientId,
					UserName = session.UserName,
					FileName = fileName,
					Operate  = "Download",
					Groups   = HslHelper.PathCombine( groupInfo )
				};
				fileMonitor.Add( monitorItem );
				// 发送文件数据
#if NET20 || NET35
				OperateResult send = SendMqttFile( session.MqttSocket, ReturnAbsoluteFileName( groupInfo, guidName ), fileName, "", 
					monitorItem.UpdateProgress, session.AesCryptography ? this.aesCryptography : null );
#else
				OperateResult send = await SendMqttFileAsync( session.MqttSocket, ReturnAbsoluteFileName( groupInfo, guidName ), fileName, "", 
					monitorItem.UpdateProgress, session.AesCryptography ? this.aesCryptography : null );
#endif
				fileMarkId.LeaveReadOperator( );
				fileMonitor.Remove( monitorItem.UniqueId );
				OnFileChangedEvent?.Invoke( session, new MqttFileOperateInfo( )
				{
					Groups    = HslHelper.PathCombine( groupInfo ),
					FileNames = fileNames,
					Operate   = "Download",
					TimeCost  = DateTime.Now - dateTimeStart
				} );

				if (!send.IsSuccess)
					LogNet?.WriteError( ToString( ), $"{session} {StringResources.Language.FileDownloadFailed} : {send.Message} :{relativeName} Name:{session.UserName}" + " Spend:" + SoftBasic.GetTimeSpanDescription( DateTime.Now - dateTimeStart ) );
				else
					LogNet?.WriteInfo( ToString( ), $"{session} {StringResources.Language.FileDownloadSuccess} : { relativeName} Spend:{ SoftBasic.GetTimeSpanDescription( DateTime.Now - dateTimeStart ) }" );
			}
			else if (receiveFileNames.Content1 == MqttControlMessage.FileUpload)
			{
				string fileName = fileNames[0];
				string fullFileName = ReturnAbsoluteFileName( groupInfo, fileName );
				// 上传文件
				CheckFolderAndCreate( );
				FileInfo info = new FileInfo( fullFileName );

				try
				{
					if (!Directory.Exists( info.DirectoryName )) Directory.CreateDirectory( info.DirectoryName );
				}
				catch (Exception ex)
				{
					LogNet?.WriteException( ToString( ), StringResources.Language.FilePathCreateFailed + fullFileName, ex );
					return;
				}

				// 监控信息
				DateTime dateTimeStart = DateTime.Now;
				MqttFileMonitorItem monitorItem = new MqttFileMonitorItem
				{
					EndPoint = session.EndPoint,
					ClientId = session.ClientId,
					UserName = session.UserName,
					FileName = fileName,
					Operate  = "Upload",
					Groups   = HslHelper.PathCombine( groupInfo )
				};
				fileMonitor.Add( monitorItem );

				// 接收文件并回发消息
#if NET20 || NET35
				OperateResult<FileBaseInfo> receive = ReceiveMqttFileAndUpdateGroup( session, info, monitorItem.UpdateProgress );
#else
				OperateResult<FileBaseInfo> receive = await ReceiveMqttFileAndUpdateGroupAsync( session, info, monitorItem.UpdateProgress );
#endif
				fileMonitor.Remove( monitorItem.UniqueId );

				if (receive.IsSuccess)
				{
					OnFileChangedEvent?.Invoke( session, new MqttFileOperateInfo( )
					{
						Groups    = HslHelper.PathCombine( groupInfo ),
						FileNames = fileNames,
						Operate   = "Upload",
						TimeCost  = DateTime.Now - dateTimeStart
					} );
					LogNet?.WriteInfo( ToString( ), $"{session} {StringResources.Language.FileUploadSuccess}:{relativeName} Spend:{SoftBasic.GetTimeSpanDescription( DateTime.Now - dateTimeStart )}" );
				}
				else
				{
					LogNet?.WriteError( ToString( ), $"{session} {StringResources.Language.FileUploadFailed}:{relativeName} Spend:{SoftBasic.GetTimeSpanDescription( DateTime.Now - dateTimeStart )}" );
				}
			}
			else if (receiveFileNames.Content1 == MqttControlMessage.FileDelete)
			{
				// 删除多个文件
				DateTime dateTimeStart = DateTime.Now;
				foreach (var item in fileNames)
				{
					string fullFileName = ReturnAbsoluteFileName( groupInfo, item );

					FileInfo info = new FileInfo( fullFileName );
					GroupFileContainer fileManagment = GetGroupFromFilePath( info.DirectoryName );

					// 新增删除的任务
					DeleteExsistingFile( info.DirectoryName, fileManagment.DeleteFile( info.Name ) );

					relativeName = GetRelativeFileName( groupInfo, item );
					LogNet?.WriteInfo( ToString( ), $"{session} {StringResources.Language.FileDeleteSuccess}:{relativeName}" );
				}

				// 回发消息，1和success没有什么含义
#if NET20 || NET35
				Send( session.MqttSocket, MqttHelper.BuildMqttCommand( MqttControlMessage.FileDelete, null, null ).Content );
#else
				await SendAsync( session.MqttSocket, MqttHelper.BuildMqttCommand( MqttControlMessage.FileDelete, null, null ).Content );
#endif

				OnFileChangedEvent?.Invoke( session, new MqttFileOperateInfo( )
				{
					Groups    = HslHelper.PathCombine( groupInfo ),
					FileNames = fileNames,
					Operate   = "Delete",
					TimeCost  = DateTime.Now - dateTimeStart
				} );
			}
			else if (receiveFileNames.Content1 == MqttControlMessage.FileFolderDelete)
			{
				DateTime dateTimeStart = DateTime.Now;
				string fullFileName = ReturnAbsoluteFileName( groupInfo, "123.txt" );

				FileInfo info = new FileInfo( fullFileName );
				GroupFileContainer fileManagment = GetGroupFromFilePath( info.DirectoryName );

				// 新增删除的任务
				DeleteExsistingFile( info.DirectoryName, fileManagment.ClearAllFiles( ) );

				// 回发消息，1和success没有什么含义
#if NET20 || NET35
				Send( session.MqttSocket, MqttHelper.BuildMqttCommand( MqttControlMessage.FileFolderDelete, null, null ).Content );
#else
				await SendAsync( session.MqttSocket, MqttHelper.BuildMqttCommand( MqttControlMessage.FileFolderDelete, null, null ).Content );
#endif

				OnFileChangedEvent?.Invoke( session, new MqttFileOperateInfo( )
				{
					Groups    = HslHelper.PathCombine( groupInfo ),
					FileNames = null,
					Operate   = "DeleteFolder",
					TimeCost  = DateTime.Now - dateTimeStart
				} );
				LogNet?.WriteInfo( ToString( ), session.ToString( ) + "FolderDelete : " + relativeName );
			}
			else if (receiveFileNames.Content1 == MqttControlMessage.FileFolderFiles)
			{
				GroupFileContainer fileManagment = GetGroupFromFilePath( ReturnAbsoluteFilePath( groupInfo ) );
#if NET20 || NET35
				Send( session.MqttSocket, MqttHelper.BuildMqttCommand( MqttControlMessage.FileFolderDelete, null, Encoding.UTF8.GetBytes( fileManagment.JsonArrayContent ) ).Content );
#else
				await SendAsync( session.MqttSocket, MqttHelper.BuildMqttCommand( MqttControlMessage.FileFolderDelete, null, Encoding.UTF8.GetBytes( fileManagment.JsonArrayContent ) ).Content );
#endif
			}
			else if (receiveFileNames.Content1 == MqttControlMessage.FileFolderInfo)
			{
				GroupFileContainer fileManagment = GetGroupFromFilePath( ReturnAbsoluteFilePath( groupInfo ) );
#if NET20 || NET35
				Send( session.MqttSocket, MqttHelper.BuildMqttCommand( MqttControlMessage.FileFolderInfo, null, Encoding.UTF8.GetBytes( fileManagment.GetGroupFileInfo( ).ToJsonString( ) ) ).Content );
#else
				await SendAsync( session.MqttSocket, MqttHelper.BuildMqttCommand( MqttControlMessage.FileFolderInfo, null, Encoding.UTF8.GetBytes( fileManagment.GetGroupFileInfo( ).ToJsonString( ) ) ).Content );
#endif
			}
			else if (receiveFileNames.Content1 == MqttControlMessage.FileFolderInfos)
			{
				List<GroupFileInfo> folders = new List<GroupFileInfo>( );
				foreach (var m in GetDirectories( groupInfo ))
				{
					DirectoryInfo directory = new DirectoryInfo( m );
					List<string> path = new List<string>( groupInfo );
					path.Add( directory.Name );

					GroupFileContainer fileManagment = GetGroupFromFilePath( ReturnAbsoluteFilePath( path.ToArray( ) ) );
					GroupFileInfo groupFileInfo = fileManagment.GetGroupFileInfo( );
					groupFileInfo.PathName = directory.Name;
					folders.Add( groupFileInfo );
				}
#if NET20 || NET35
				Send( session.MqttSocket, MqttHelper.BuildMqttCommand( MqttControlMessage.FileFolderInfo, null, Encoding.UTF8.GetBytes( folders.ToJsonString( ) ) ).Content );
#else
				await SendAsync( session.MqttSocket, MqttHelper.BuildMqttCommand( MqttControlMessage.FileFolderInfo, null, Encoding.UTF8.GetBytes( folders.ToJsonString( ) ) ).Content );
#endif
			}
			else if (receiveFileNames.Content1 == MqttControlMessage.FileFolderPaths)
			{
				List<string> folders = new List<string>( );
				foreach (var m in GetDirectories( groupInfo ))
				{
					DirectoryInfo directory = new DirectoryInfo( m );
					folders.Add( directory.Name );
				}

				JArray jArray = JArray.FromObject( folders.ToArray( ) );
#if NET20 || NET35
				Send( session.MqttSocket, MqttHelper.BuildMqttCommand( MqttControlMessage.FileFolderPaths, null, Encoding.UTF8.GetBytes( jArray.ToString( ) ) ).Content );
#else
				await SendAsync( session.MqttSocket, MqttHelper.BuildMqttCommand( MqttControlMessage.FileFolderPaths, null, Encoding.UTF8.GetBytes( jArray.ToString( ) ) ).Content );
#endif
			}
			else if (receiveFileNames.Content1 == MqttControlMessage.FileExists)
			{
				string fileName = fileNames[0];
				string fullPath = ReturnAbsoluteFilePath( groupInfo );
				GroupFileContainer fileManagment = GetGroupFromFilePath( fullPath );

				bool isExists = fileManagment.FileExists( fileName );
#if NET20 || NET35
				Send( session.MqttSocket, MqttHelper.BuildMqttCommand( isExists ? (byte)1 : (byte)0, null, Encoding.UTF8.GetBytes( StringResources.Language.FileNotExist ) ).Content );
#else
				await SendAsync( session.MqttSocket, MqttHelper.BuildMqttCommand( isExists ? (byte)1 : (byte)0, null, Encoding.UTF8.GetBytes( StringResources.Language.FileNotExist ) ).Content );
#endif
			}
			else
			{

			}
		}

		/// <summary>
		/// 从套接字接收文件并保存，更新文件列表
		/// </summary>
		/// <param name="session">当前的会话信息</param>
		/// <param name="info">保存的信息</param>
		/// <param name="reportProgress">当前的委托信息</param>
		/// <returns>是否成功的结果对象</returns>

#if NET20 || NET35
		private OperateResult<FileBaseInfo> ReceiveMqttFileAndUpdateGroup( MqttSession session, FileInfo info, Action<long, long> reportProgress )
#else
		private async Task<OperateResult<FileBaseInfo>> ReceiveMqttFileAndUpdateGroupAsync( MqttSession session, FileInfo info, Action<long, long> reportProgress )
#endif
		{
			string guidName = SoftBasic.GetUniqueStringByGuidAndRandom( );
			string fileName = Path.Combine( info.DirectoryName, guidName );

#if NET20 || NET35
			OperateResult<FileBaseInfo> receive = ReceiveMqttFile( session.MqttSocket, fileName, reportProgress, session.AesCryptography ? this.aesCryptography : null);
#else
			OperateResult<FileBaseInfo> receive = await ReceiveMqttFileAsync( session.MqttSocket, fileName, reportProgress, session.AesCryptography ? this.aesCryptography : null );
#endif
			if (!receive.IsSuccess)
			{
				DeleteFileByName( fileName );
				return receive;
			}

			// 更新操作
			GroupFileContainer fileManagment = GetGroupFromFilePath( info.DirectoryName );
			string oldName = fileManagment.UpdateFileMappingName( info.Name, receive.Content.Size, guidName, session.UserName, receive.Content.Tag );

			// 删除旧的文件
			DeleteExsistingFile( info.DirectoryName, oldName );

			// 回发消息
#if NET20 || NET35
			OperateResult sendBack = Send( session.MqttSocket, MqttHelper.BuildMqttCommand( MqttControlMessage.FileNoSense, null, Encoding.UTF8.GetBytes( StringResources.Language.SuccessText ) ).Content );
#else
			OperateResult sendBack = await SendAsync( session.MqttSocket, MqttHelper.BuildMqttCommand( MqttControlMessage.FileNoSense, null, Encoding.UTF8.GetBytes( StringResources.Language.SuccessText ) ).Content );
#endif
			if (!sendBack.IsSuccess) return OperateResult.CreateFailedResult<FileBaseInfo>( sendBack );

			return OperateResult.CreateSuccessResult( receive.Content );
		}

		/// <summary>
		/// 返回相对路径的名称
		/// </summary>
		/// <param name="groups">文件的分类路径信息</param>
		/// <param name="fileName">文件名</param>
		/// <returns>是否成功的结果对象</returns>
		private string GetRelativeFileName( string[] groups, string fileName )
		{
			string result = "";
			for (int i = 0; i < groups.Length; i++)
				if (!string.IsNullOrEmpty( groups[i] )) result = Path.Combine( result, groups[i] );
			return Path.Combine( result, fileName );
		}

		/// <summary>
		/// 返回服务器的绝对路径，包含根目录的信息  [Root Dir][A][B][C]... 信息
		/// </summary>
		/// <param name="groups">文件的路径分类信息</param>
		/// <returns>是否成功的结果对象</returns>
		private string ReturnAbsoluteFilePath( string[] groups )
		{
#if NET20 || NET35
			string result = filesDirectoryPath;
			for (int i = 0; i < groups.Length; i++)
				if (!string.IsNullOrEmpty( groups[i] )) result = Path.Combine( result, groups[i] );
			return result;
#else
			return Path.Combine( filesDirectoryPath, Path.Combine( groups ) );
#endif
		}

		/// <summary>
		/// 返回服务器的绝对路径，包含根目录的信息  [Root Dir][A][B][C]...[FileName] 信息
		/// </summary>
		/// <param name="groups">路径分类信息</param>
		/// <param name="fileName">文件名</param>
		/// <returns>是否成功的结果对象</returns>
		protected string ReturnAbsoluteFileName( string[] groups, string fileName )
		{
			return Path.Combine( ReturnAbsoluteFilePath( groups ), fileName );
		}

		/// <summary>
		/// 根据文件的显示名称转化为真实存储的名称，例如 123.txt 获取到在文件服务器里映射的文件名称，例如返回 b35a11ec533147ca80c7f7d1713f015b7909
		/// </summary>
		/// <param name="groups">文件的分类信息</param>
		/// <param name="fileName">文件显示名称</param>
		/// <returns>是否成功的结果对象</returns>
		private string TransformFactFileName( string[] groups, string fileName )
		{
			string path = ReturnAbsoluteFilePath( groups );
			GroupFileContainer fileManagment = GetGroupFromFilePath( path );
			return fileManagment.GetCurrentFileMappingName( fileName );
		}

		/// <summary>
		/// 获取当前目录的文件列表管理容器，如果没有会自动创建，通过该容器可以实现对当前目录的文件进行访问<br />
		/// Get the file list management container of the current directory. If not, it will be created automatically. 
		/// Through this container, you can access files in the current directory.
		/// </summary>
		/// <param name="filePath">路径信息</param>
		/// <returns>文件管理容器信息</returns>
		private GroupFileContainer GetGroupFromFilePath( string filePath )
		{
			GroupFileContainer groupFile = null;
			// 全部修改为大写
			filePath = filePath.ToUpper( );
			group_marks_lock.Enter( );

			// lock operator
			if (m_dictionary_group_marks.ContainsKey( filePath ))
			{
				groupFile = m_dictionary_group_marks[filePath];
			}
			else
			{
				groupFile = new GroupFileContainer( LogNet, filePath );
				m_dictionary_group_marks.Add( filePath, groupFile );
			}

			group_marks_lock.Leave( );
			return groupFile;
		}

		/// <summary>
		/// 获取文件夹的所有文件夹列表
		/// </summary>
		/// <param name="groups">分类信息</param>
		/// <returns>文件夹列表</returns>
		private string[] GetDirectories( string[] groups )
		{
			if (string.IsNullOrEmpty( filesDirectoryPath )) return new string[0];

			string absolutePath = ReturnAbsoluteFilePath( groups );

			// 如果文件夹不存在
			if (!Directory.Exists( absolutePath )) return new string[0];
			// 返回文件列表
			return Directory.GetDirectories( absolutePath );
		}

		/// <summary>
		/// 获取当前文件的读写锁，如果没有会自动创建，文件名应该是guid文件名，例如 b35a11ec533147ca80c7f7d1713f015b7909<br />
		/// Acquire the read-write lock of the current file. If not, it will be created automatically. 
		/// The file name should be the guid file name, for example, b35a11ec533147ca80c7f7d1713f015b7909
		/// </summary>
		/// <param name="fileName">完整的文件路径</param>
		/// <returns>返回携带文件信息的读写锁</returns>
		private FileMarkId GetFileMarksFromDictionaryWithFileName( string fileName )
		{
			FileMarkId fileMarkId;
			lock (dictHybirdLock)
			{
				if (dictionaryFilesMarks.ContainsKey( fileName ))
				{
					fileMarkId = dictionaryFilesMarks[fileName];
				}
				else
				{
					fileMarkId = new FileMarkId( LogNet, fileName );
					dictionaryFilesMarks.Add( fileName, fileMarkId );
				}
			}
			return fileMarkId;
		}

		/// <summary>
		/// 检查文件夹是否存在，不存在就创建
		/// </summary>
		private void CheckFolderAndCreate( )
		{
			if (!Directory.Exists( filesDirectoryPath ))
				Directory.CreateDirectory( filesDirectoryPath );
		}

		/// <summary>
		/// 删除已经存在的文件信息，文件的名称需要是guid名称，例如 b35a11ec533147ca80c7f7d1713f015b7909
		/// </summary>
		/// <param name="path">文件的路径</param>
		/// <param name="fileName">文件的guid名称，例如 b35a11ec533147ca80c7f7d1713f015b7909</param>
		private void DeleteExsistingFile( string path, string fileName ) => DeleteExsistingFile( path, new List<string>( ) { fileName } );

		/// <summary>
		/// 删除已经存在的文件信息，文件的名称需要是guid名称，例如 b35a11ec533147ca80c7f7d1713f015b7909
		/// </summary>
		/// <param name="path">文件的路径</param>
		/// <param name="fileNames">文件的guid名称，例如 b35a11ec533147ca80c7f7d1713f015b7909</param>
		private void DeleteExsistingFile( string path, List<string> fileNames )
		{
			foreach (var fileName in fileNames)
			{
				if (!string.IsNullOrEmpty( fileName ))
				{
					string fileUltimatePath = Path.Combine( path, fileName );
					FileMarkId fileMarkId = GetFileMarksFromDictionaryWithFileName( fileName );

					fileMarkId.AddOperation( ( ) =>
					{
						if (!DeleteFileByName( fileUltimatePath ))
							LogNet?.WriteInfo( ToString( ), StringResources.Language.FileDeleteFailed + fileUltimatePath );
						else
							LogNet?.WriteInfo( ToString( ), StringResources.Language.FileDeleteSuccess + fileUltimatePath );
					} );
				}
			}
		}

		/// <summary>
		/// 文件变化的委托信息
		/// </summary>
		/// <param name="session">当前的会话信息，包含用户的基本信息</param>
		/// <param name="operateInfo">当前的文件操作信息，具体指示上传，下载，删除操作</param>
		public delegate void FileChangedDelegate( MqttSession session, MqttFileOperateInfo operateInfo );

		/// <summary>
		/// 文件变化的事件，当文件上传的时候，文件下载的时候，文件被删除的时候触发。<br />
		/// The file change event is triggered when the file is uploaded, when the file is downloaded, or when the file is deleted.
		/// </summary>
		public event FileChangedDelegate OnFileChangedEvent;

#endregion

#region Session Method

		private void AddMqttSession( MqttSession session )
		{
			lock (sessionsLock)
			{
				mqttSessions.Add( session );
			}
			LogNet?.WriteDebug( ToString( ), $"{session} Online" );
		}

		/// <summary>
		/// 让MQTT客户端正常下线，调用本方法即可自由控制会话客户端强制下线操作。
		/// </summary>
		/// <param name="session">当前的会话信息</param>
		/// <param name="reason">当前下线的原因，如果没有，代表正常下线</param>
		public void RemoveAndCloseSession( MqttSession session, string reason )
		{
			bool removeTrue = false;
			lock (sessionsLock)
			{
				removeTrue = mqttSessions.Remove( session );
			}
			session.MqttSocket?.Close( );
			if (removeTrue) LogNet?.WriteDebug( ToString( ), $"{session} Offline {reason}" );
			if (session.Protocol == "MQTT") OnClientDisConnected?.Invoke( session );
		}

#endregion

#region Event Handler

		/// <summary>
		/// Mqtt的消息收到委托
		/// </summary>
		/// <param name="session">当前会话的内容</param>
		/// <param name="message">Mqtt的消息</param>
		public delegate void OnClientApplicationMessageReceiveDelegate( MqttSession session, MqttClientApplicationMessage message );

		/// <summary>
		/// 当收到客户端发来的<see cref="MqttClientApplicationMessage"/>消息时触发<br />
		/// Triggered when a <see cref="MqttClientApplicationMessage"/> message is received from the client
		///</summary>
		public event OnClientApplicationMessageReceiveDelegate OnClientApplicationMessageReceive;

		/// <summary>
		/// 当前mqtt客户端连接上服务器的事件委托
		/// </summary>
		/// <param name="session">当前的会话对象</param>
		public delegate void OnClientConnectedDelegate( MqttSession session );

		/// <summary>
		/// Mqtt的客户端连接上来时触发<br />
		/// Triggered when Mqtt client connects
		/// </summary>
		public event OnClientConnectedDelegate OnClientConnected;

		/// <summary>
		/// Mqtt的客户端下线时触发<br />
		/// Triggered when Mqtt client connects
		/// </summary>
		public event OnClientConnectedDelegate OnClientDisConnected;

		/// <summary>
		/// 验证的委托
		/// </summary>
		/// <param name="mqttSession">当前的MQTT的会话内容</param>
		/// <param name="clientId">客户端的id</param>
		/// <param name="userName">用户名</param>
		/// <param name="passwrod">密码</param>
		/// <returns>0则是通过，否则，就是连接失败</returns>
		public delegate int ClientVerificationDelegate( MqttSession mqttSession, string clientId, string userName, string passwrod );

		/// <summary>
		/// 当客户端连接时，触发的验证事件<br />
		/// Validation event triggered when the client connects
		/// </summary>
		public event ClientVerificationDelegate ClientVerification;                                                   // 验证的委托信息

#endregion

#region Public Properties

		/// <inheritdoc cref="HttpServer.LogStatistics"/>
		public LogStatisticsDict LogStatistics => this.statisticsDict;

		/// <summary>
		/// 获取或设置是否启用订阅主题通配符的功能，默认为 False<br />
		/// Gets or sets whether to enable the function of subscribing to the topic wildcard, the default is False
		/// </summary>
		/// <remarks>
		/// 启动之后，通配符示例：finance/stock/ibm/#; finance/+; '#' 是匹配所有主题，'+' 是匹配一级主题树。<br />
		/// 通配符的规则参考如下的网址：http://public.dhe.ibm.com/software/dw/webservices/ws-mqtt/mqtt-v3r1.html#appendix-a
		/// </remarks>
		public bool TopicWildcard { get => this.topicWildcard; set => this.topicWildcard = value; }

		/// <summary>
		/// 获取当前的在线的客户端数量<br />
		/// Gets the number of clients currently online
		/// </summary>
		public int OnlineCount => mqttSessions.Count;

		/// <summary>
		/// 获得当前所有的在线的MQTT客户端信息，包括异步的客户端及同步请求的客户端。<br />
		/// Obtain all current online MQTT client information, including asynchronous client and synchronous request client.
		/// </summary>
		public MqttSession[] OnlineSessions 
		{ 
			get
			{
				MqttSession[] snapshoot = null;
				lock (sessionsLock)
					snapshoot = mqttSessions.ToArray( );
				return snapshoot;
			} 
		}

		/// <summary>
		/// 获得当前异步客户端在线的MQTT客户端信息。<br />
		/// Get the MQTT client information of the current asynchronous client online.
		/// </summary>
		public MqttSession[] MqttOnlineSessions
		{
			get
			{
				MqttSession[] snapshoot = null;
				lock (sessionsLock)
					snapshoot = mqttSessions.Where( m => m.Protocol == "MQTT" ).ToArray( );
				return snapshoot;
			}
		}

		/// <summary>
		/// 获得当前同步客户端在线的MQTT客户端信息，如果客户端是短连接，将难以捕获在在线信息。<br />
		/// Obtain the MQTT client information of the current synchronization client online. If the client is a short connection, it will be difficult to capture the online information. <br />
		/// </summary>
		public MqttSession[] SyncOnlineSessions
		{
			get
			{
				MqttSession[] snapshoot = null;
				lock (sessionsLock)
					snapshoot = mqttSessions.Where( m => m.Protocol == "HUSL" ).ToArray( );
				return snapshoot;
			}
		}

#endregion

#region Public Method

		/// <summary>
		/// 删除服务器里的指定主题的驻留消息。<br />
		/// Delete the resident message of the specified topic in the server.
		/// </summary>
		/// <param name="topic">等待删除的主题关键字</param>
		public void DeleteRetainTopic( string topic )
		{
			lock (keysLock)
			{
				if (retainKeys.ContainsKey( topic ))
				{
					retainKeys.Remove( topic );
				}
			}
		}

		/// <summary>
		/// 获取所有的驻留的消息的主题，如果消息发布的时候没有使用Retain属性，就无法通过本方法查到<br />
		/// Get the subject of all resident messages. If the Retain attribute is not used when the message is published, it cannot be found by this method
		/// </summary>
		/// <returns>主题的数组</returns>
		public string[] GetAllRetainTopics( )
		{
			string[] keys = null;
			lock (keysLock)
			{
				keys = retainKeys.Select( m => m.Key ).ToArray( );
			}
			return keys;
		}

		/// <summary>
		/// 获取订阅了某个主题的所有的会话列表信息<br />
		/// Get all the conversation list information subscribed to a topic
		/// </summary>
		/// <param name="topic">主题信息</param>
		/// <returns>会话列表</returns>
		public MqttSession[] GetMqttSessionsByTopic( string topic )
		{
			MqttSession[] snapshoot = null;
			lock (sessionsLock)
				snapshoot = mqttSessions.Where( m => m.Protocol == "MQTT" && m.IsClientSubscribe( topic, topicWildcard ) ).ToArray( );
			return snapshoot;
		}

#endregion

#region IDispose

		/// <summary>
		/// 释放当前的对象
		/// </summary>
		/// <param name="disposing"></param>
		protected virtual void Dispose( bool disposing )
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					// TODO: 释放托管状态(托管对象)
					this.timerHeart?.Dispose( );
					this.group_marks_lock?.Dispose( );
					this.ClientVerification = null;
					this.FileOperateVerification = null;
					this.OnClientApplicationMessageReceive = null;
					this.OnClientConnected = null;
					this.OnClientDisConnected = null;
					this.OnFileChangedEvent = null;
				}

				// TODO: 释放未托管的资源(未托管的对象)并重写终结器
				// TODO: 将大型字段设置为 null
				disposedValue = true;
			}
		}

		// // TODO: 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
		// ~MqttServer()
		// {
		//     // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
		//     Dispose(disposing: false);
		// }

		/// <inheritdoc cref="IDisposable.Dispose"/>
		public void Dispose( )
		{
			// 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
			Dispose( disposing: true );
			GC.SuppressFinalize( this );
		}

#endregion

#region Private Member

		private readonly Dictionary<string, MqttClientApplicationMessage> retainKeys;
		private readonly object keysLock;                                                                            // 驻留的消息的词典锁

		private readonly List<MqttSession> mqttSessions = new List<MqttSession>( );                                  // MQTT的客户端信息
		private readonly object sessionsLock = new object( );
		private System.Threading.Timer timerHeart;
		private LogStatisticsDict statisticsDict;                                                                    // 所有的API请求的数量统计
		private bool disposedValue;
		private RSACryptoServiceProvider providerServer = null;                                                      // 服务器的私钥
		private AesCryptography aesCryptography = null;                                                              // 数据交互时候的AES
		private bool topicWildcard = false;                                                                          // 是否用订阅通配符
#endregion

#region Object Override

		/// <inheritdoc/>
		public override string ToString( ) => $"MqttServer[{Port}]";

#endregion
	}
}
