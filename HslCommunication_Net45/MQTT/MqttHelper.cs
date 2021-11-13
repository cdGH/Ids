using HslCommunication.BasicFramework;
using HslCommunication.Core;
using HslCommunication.Core.Security;
using HslCommunication.Reflection;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
#if !NET35 && !NET20
using System.Threading.Tasks;
#endif

namespace HslCommunication.MQTT
{
	/// <summary>
	/// Mqtt协议的辅助类，提供了一些协议相关的基础方法，方便客户端和服务器端一起调用。<br />
	/// The auxiliary class of the Mqtt protocol provides some protocol-related basic methods for the client and server to call together.
	/// </summary>
	public class MqttHelper
	{
		#region Static Helper Method

		/// <summary>
		/// 根据数据的总长度，计算出剩余的数据长度信息<br />
		/// According to the total length of the data, calculate the remaining data length information
		/// </summary>
		/// <param name="length">数据的总长度</param>
		/// <returns>计算结果</returns>
		public static OperateResult<byte[]> CalculateLengthToMqttLength( int length )
		{
			if (length > 268_435_455) return new OperateResult<byte[]>( StringResources.Language.MQTTDataTooLong );

			if (length < 128) return OperateResult.CreateSuccessResult( new byte[1] { (byte)length } );

			if (length < 128 * 128)
			{
				byte[] buffer = new byte[2];
				buffer[0] = (byte)(length % 128 + 0x80);
				buffer[1] = (byte)(length / 128);
				return OperateResult.CreateSuccessResult( buffer );
			}

			if (length < 128 * 128 * 128)
			{
				byte[] buffer = new byte[3];
				buffer[0] = (byte)(length % 128 + 0x80);
				buffer[1] = (byte)(length / 128 % 128 + 0x80);
				buffer[2] = (byte)(length / 128 / 128);
				return OperateResult.CreateSuccessResult( buffer );
			}
			else
			{
				byte[] buffer = new byte[4];
				buffer[0] = (byte)(length % 128 + 0x80);
				buffer[1] = (byte)(length / 128 % 128 + 0x80);
				buffer[2] = (byte)(length / 128 / 128 % 128 + 0x80);
				buffer[3] = (byte)(length / 128 / 128 / 128);
				return OperateResult.CreateSuccessResult( buffer );
			}
		}

		/// <summary>
		/// 将一个数据打包成一个mqtt协议的内容<br />
		/// Pack a piece of data into a mqtt protocol
		/// </summary>
		/// <param name="control">控制码</param>
		/// <param name="flags">标记</param>
		/// <param name="variableHeader">可变头的字节内容</param>
		/// <param name="payLoad">负载数据</param>
		/// <param name="aesCryptography">AES数据加密对象</param>
		/// <returns>带有是否成功的结果对象</returns>
		public static OperateResult<byte[]> BuildMqttCommand( byte control, byte flags, byte[] variableHeader, byte[] payLoad, AesCryptography aesCryptography = null )
		{
			control = (byte)(control << 4);
			byte head = (byte)(control | flags);

			return BuildMqttCommand( head, variableHeader, payLoad, aesCryptography );
		}

		/// <summary>
		/// 将一个数据打包成一个mqtt协议的内容<br />
		/// Pack a piece of data into a mqtt protocol
		/// </summary>
		/// <param name="head">控制码加标记码</param>
		/// <param name="variableHeader">可变头的字节内容</param>
		/// <param name="payLoad">负载数据</param>
		/// <param name="aesCryptography">AES数据加密对象</param>
		/// <returns>带有是否成功的结果对象</returns>
		public static OperateResult<byte[]> BuildMqttCommand( byte head, byte[] variableHeader, byte[] payLoad, AesCryptography aesCryptography = null )
		{
			if (variableHeader == null) variableHeader = new byte[0];
			if (payLoad == null) payLoad = new byte[0];

			// 如果需要加密操作，就对payload进行加密
			if (aesCryptography != null) payLoad = aesCryptography.Encrypt( payLoad );

			// 先计算长度
			OperateResult<byte[]> bufferLength = CalculateLengthToMqttLength( variableHeader.Length + payLoad.Length );
			if (!bufferLength.IsSuccess) return bufferLength;

			MemoryStream ms = new MemoryStream( );
			ms.WriteByte( head );
			ms.Write( bufferLength.Content, 0, bufferLength.Content.Length );
			if (variableHeader.Length > 0) ms.Write( variableHeader, 0, variableHeader.Length );
			if (payLoad.Length > 0) ms.Write( payLoad, 0, payLoad.Length );
			return OperateResult.CreateSuccessResult( ms.ToArray( ) );
		}

		/// <summary>
		/// 将字符串打包成utf8编码，并且带有2个字节的表示长度的信息<br />
		/// Pack the string into utf8 encoding, and with 2 bytes of length information
		/// </summary>
		/// <param name="message">文本消息</param>
		/// <returns>打包之后的信息</returns>
		public static byte[] BuildSegCommandByString( string message )
		{
			byte[] buffer = string.IsNullOrEmpty( message ) ? new byte[0] : Encoding.UTF8.GetBytes( message );
			byte[] result = new byte[buffer.Length + 2];
			buffer.CopyTo( result, 2 );
			result[0] = (byte)(buffer.Length / 256);
			result[1] = (byte)(buffer.Length % 256);
			return result;
		}

		/// <summary>
		/// 从MQTT的缓存信息里，提取文本信息<br />
		/// Extract text information from MQTT cache information
		/// </summary>
		/// <param name="buffer">Mqtt的报文</param>
		/// <param name="index">索引</param>
		/// <returns>值</returns>
		public static string ExtraMsgFromBytes( byte[] buffer, ref int index )
		{
			int indexTmp = index;
			int length = buffer[index] * 256 + buffer[index + 1];
			index = index + 2 + length;
			return Encoding.UTF8.GetString( buffer, indexTmp + 2, length );
		}

		/// <summary>
		/// 从MQTT的缓存信息里，提取文本信息<br />
		/// Extract text information from MQTT cache information
		/// </summary>
		/// <param name="buffer">Mqtt的报文</param>
		/// <param name="index">索引</param>
		/// <returns>值</returns>
		public static string ExtraSubscribeMsgFromBytes( byte[] buffer, ref int index )
		{
			int indexTmp = index;
			int length = buffer[index] * 256 + buffer[index + 1];
			index = index + 3 + length;
			return Encoding.UTF8.GetString( buffer, indexTmp + 2, length );
		}

		/// <summary>
		/// 从MQTT的缓存信息里，提取长度信息<br />
		/// Extract length information from MQTT cache information
		/// </summary>
		/// <param name="buffer">Mqtt的报文</param>
		/// <param name="index">索引</param>
		/// <returns>值</returns>
		public static int ExtraIntFromBytes( byte[] buffer, ref int index )
		{
			int length = buffer[index] * 256 + buffer[index + 1];
			index += 2;
			return length;
		}

		/// <summary>
		/// 从MQTT的缓存信息里，提取长度信息<br />
		/// Extract length information from MQTT cache information
		/// </summary>
		/// <param name="data">数据信息</param>
		/// <returns>值</returns>
		public static byte[] BuildIntBytes( int data )
		{
			return new byte[] { BitConverter.GetBytes( data )[1], BitConverter.GetBytes( data )[0] };
		}

		/// <summary>
		/// 创建MQTT连接服务器的报文信息<br />
		/// Create MQTT connection server message information
		/// </summary>
		/// <param name="connectionOptions">连接配置</param>
		/// <param name="protocol">协议的内容</param>
		/// <param name="rsa">数据加密对象</param>
		/// <returns>返回是否成功的信息</returns>
		public static OperateResult<byte[]> BuildConnectMqttCommand( MqttConnectionOptions connectionOptions, string protocol = "MQTT", RSACryptoServiceProvider rsa = null )
		{
			List<byte> variableHeader = new List<byte>( );
			variableHeader.AddRange( new byte[] { 0x00, 0x04 } );                                                 // 
			variableHeader.AddRange( Encoding.ASCII.GetBytes( protocol ) );                                       // 协议名称：MQTT
			variableHeader.Add( 0x04 );                                                                           // 协议版本，3.1.1
			byte connectFlags = 0x00;
			if (connectionOptions.Credentials != null)                                                            // 是否需要验证用户名和密码
			{
				connectFlags = (byte)(connectFlags | 0x80);
				connectFlags = (byte)(connectFlags | 0x40);
			}
			if (connectionOptions.CleanSession)
			{
				connectFlags = (byte)(connectFlags | 0x02);
			}
			variableHeader.Add( connectFlags );
			if (connectionOptions.KeepAlivePeriod.TotalSeconds < 1) connectionOptions.KeepAlivePeriod = TimeSpan.FromSeconds( 1 );
			byte[] keepAlivePeriod = BitConverter.GetBytes( (int)connectionOptions.KeepAlivePeriod.TotalSeconds );
			variableHeader.Add( keepAlivePeriod[1] );
			variableHeader.Add( keepAlivePeriod[0] );

			List<byte> payLoad = new List<byte>( );
			payLoad.AddRange( BuildSegCommandByString( connectionOptions.ClientId ) );                         // 添加客户端的id信息

			if (connectionOptions.Credentials != null)                                                         // 根据需要选择是否添加用户名和密码
			{
				payLoad.AddRange( BuildSegCommandByString( connectionOptions.Credentials.UserName ) );
				payLoad.AddRange( BuildSegCommandByString( connectionOptions.Credentials.Password ) );
			}

			if (rsa == null)
				return BuildMqttCommand( MqttControlMessage.CONNECT, 0x00, variableHeader.ToArray( ), payLoad.ToArray( ) );
			else
				return BuildMqttCommand( MqttControlMessage.CONNECT, 0x00, rsa.EncryptLargeData( variableHeader.ToArray( ) ), rsa.EncryptLargeData( payLoad.ToArray( ) ) );
		}

		/// <summary>
		/// 根据服务器返回的信息判断当前的连接是否是可用的<br />
		/// According to the information returned by the server to determine whether the current connection is available
		/// </summary>
		/// <param name="code">功能码</param>
		/// <param name="data">数据内容</param>
		/// <returns>是否可用的连接</returns>
		public static OperateResult CheckConnectBack( byte code, byte[] data )
		{
			if (code >> 4 != MqttControlMessage.CONNACK) return new OperateResult( "MQTT Connection Back Is Wrong: " + code );
			if (data.Length < 2) return new OperateResult( "MQTT Connection Data Is Short: " + SoftBasic.ByteToHexString( data, ' ' ) );
			int returnCode = data[1];
			int acknowledgeFlags = data[0]; // session信息在服务器已保持，置1；未保存，置0。

			if (returnCode > 0 ) return new OperateResult( returnCode, GetMqttCodeText( returnCode ) );
			return OperateResult.CreateSuccessResult( );
		}

		/// <summary>
		/// 获取当前的错误的描述信息<br />
		/// Get a description of the current error
		/// </summary>
		/// <param name="status">状态信息</param>
		/// <returns>描述信息</returns>
		public static string GetMqttCodeText( int status )
		{
			switch (status)
			{
				case 1: return StringResources.Language.MQTTStatus01;
				case 2: return StringResources.Language.MQTTStatus02;
				case 3: return StringResources.Language.MQTTStatus03;
				case 4: return StringResources.Language.MQTTStatus04;
				case 5: return StringResources.Language.MQTTStatus05;
				default: return StringResources.Language.UnknownError;
			}
		}

		/// <summary>
		/// 创建Mqtt发送消息的命令<br />
		/// Create Mqtt command to send messages
		/// </summary>
		/// <param name="message">封装后的消息内容</param>
		/// <param name="aesCryptography">AES数据加密对象</param>
		/// <returns>结果内容</returns>
		public static OperateResult<byte[]> BuildPublishMqttCommand( MqttPublishMessage message, AesCryptography aesCryptography = null )
		{
			byte flag = 0x00;
			if (!message.IsSendFirstTime) flag = (byte)(flag | 0x08);
			if (message.Message.Retain)   flag = (byte)(flag | 0x01);
			if      (message.Message.QualityOfServiceLevel == MqttQualityOfServiceLevel.AtLeastOnce)  flag = (byte)(flag | 0x02);
			else if (message.Message.QualityOfServiceLevel == MqttQualityOfServiceLevel.ExactlyOnce)  flag = (byte)(flag | 0x04);
			else if (message.Message.QualityOfServiceLevel == MqttQualityOfServiceLevel.OnlyTransfer) flag = (byte)(flag | 0x06);

			List<byte> variableHeader = new List<byte>( );
			variableHeader.AddRange( BuildSegCommandByString( message.Message.Topic ) );
			if (message.Message.QualityOfServiceLevel != MqttQualityOfServiceLevel.AtMostOnce)
			{
				variableHeader.Add( BitConverter.GetBytes( message.Identifier )[1] );
				variableHeader.Add( BitConverter.GetBytes( message.Identifier )[0] );
			}

			return BuildMqttCommand( MqttControlMessage.PUBLISH, flag, variableHeader.ToArray( ), message.Message.Payload, aesCryptography );
		}

		/// <summary>
		/// 创建Mqtt发送消息的命令<br />
		/// Create Mqtt command to send messages
		/// </summary>
		/// <param name="topic">主题消息内容</param>
		/// <param name="payload">数据负载</param>
		/// <param name="aesCryptography">AES数据加密对象</param>
		/// <returns>结果内容</returns>
		public static OperateResult<byte[]> BuildPublishMqttCommand( string topic, byte[] payload, AesCryptography aesCryptography = null )
		{
			return BuildMqttCommand( MqttControlMessage.PUBLISH, 0x00, BuildSegCommandByString( topic ), payload, aesCryptography );
		}

		/// <summary>
		/// 创建Mqtt订阅消息的命令<br />
		/// Command to create Mqtt subscription message
		/// </summary>
		/// <param name="message">订阅的主题</param>
		/// <returns>结果内容</returns>
		public static OperateResult<byte[]> BuildSubscribeMqttCommand( MqttSubscribeMessage message )
		{
			List<byte> variableHeader = new List<byte>( );
			List<byte> payLoad = new List<byte>( );

			variableHeader.Add( BitConverter.GetBytes( message.Identifier )[1] );
			variableHeader.Add( BitConverter.GetBytes( message.Identifier )[0] );

			for (int i = 0; i < message.Topics.Length; i++)
			{
				payLoad.AddRange( BuildSegCommandByString( message.Topics[i] ) );

				if (message.QualityOfServiceLevel == MqttQualityOfServiceLevel.AtMostOnce)
					payLoad.AddRange( new byte[] { 0x00 } );
				else if (message.QualityOfServiceLevel == MqttQualityOfServiceLevel.AtLeastOnce)
					payLoad.AddRange( new byte[] { 0x01 } );
				else
					payLoad.AddRange( new byte[] { 0x02 } );
			}

			return BuildMqttCommand( MqttControlMessage.SUBSCRIBE, 0x02, variableHeader.ToArray( ), payLoad.ToArray( ) );
		}

		/// <summary>
		/// 创建Mqtt取消订阅消息的命令<br />
		/// Create Mqtt unsubscribe message command
		/// </summary>
		/// <param name="message">订阅的主题</param>
		/// <returns>结果内容</returns>
		public static OperateResult<byte[]> BuildUnSubscribeMqttCommand( MqttSubscribeMessage message )
		{
			List<byte> variableHeader = new List<byte>( );
			List<byte> payLoad = new List<byte>( );

			variableHeader.Add( BitConverter.GetBytes( message.Identifier )[1] );
			variableHeader.Add( BitConverter.GetBytes( message.Identifier )[0] );

			for (int i = 0; i < message.Topics.Length; i++)
				payLoad.AddRange( BuildSegCommandByString( message.Topics[i] ) );

			return BuildMqttCommand( MqttControlMessage.UNSUBSCRIBE, 0x02, variableHeader.ToArray( ), payLoad.ToArray( ) );
		}

		internal static OperateResult<MqttClientApplicationMessage> ParseMqttClientApplicationMessage( MqttSession session, byte code, byte[] data, AesCryptography aesCryptography = null )
		{
			bool dup = (code & 0x08) == 0x08;
			int qos = ((code & 0x04) == 0x04 ? 2 : 0) + ((code & 0x02) == 0x02 ? 1 : 0);

			MqttQualityOfServiceLevel mqttQuality = MqttQualityOfServiceLevel.AtMostOnce;
			if      (qos == 1) mqttQuality = MqttQualityOfServiceLevel.AtLeastOnce;
			else if (qos == 2) mqttQuality = MqttQualityOfServiceLevel.ExactlyOnce;
			else if (qos == 3) mqttQuality = MqttQualityOfServiceLevel.OnlyTransfer;

			bool         retain  = (code & 0x01) == 0x01;
			int          msgId   = 0;
			int          index   = 0;
			string       topic   = MqttHelper.ExtraMsgFromBytes( data, ref index );
			if (qos > 0) msgId   = MqttHelper.ExtraIntFromBytes( data, ref index );
			byte[]       payload = SoftBasic.ArrayRemoveBegin( data, index );

			if (session.AesCryptography) // 如果是加密的客户端信息，先进行解密操作
			{
				try
				{
					if (payload.Length > 0)
					{
						payload = aesCryptography.Decrypt( payload );
					}
				}
				catch (Exception ex)
				{
					return new OperateResult<MqttClientApplicationMessage>( "AES Decrypt failed: " + ex.Message );
				}
			}

			MqttClientApplicationMessage mqttClientApplicationMessage = new MqttClientApplicationMessage( )
			{
				ClientId              = session.ClientId,
				QualityOfServiceLevel = mqttQuality,
				Retain                = retain,
				Topic                 = topic,
				UserName              = session.UserName,
				Payload               = payload,
				MsgID                 = msgId,
			};

			return OperateResult.CreateSuccessResult( mqttClientApplicationMessage );
		}

		/// <summary>
		/// 解析从MQTT接受的客户端信息，解析成实际的Topic数据及Payload数据<br />
		/// Parse the client information received from MQTT and parse it into actual Topic data and Payload data
		/// </summary>
		/// <param name="mqttCode">MQTT的命令码</param>
		/// <param name="data">接收的MQTT原始的消息内容</param>
		/// <param name="aesCryptography">AES数据加密信息</param>
		/// <returns>解析的数据结果信息</returns>
		public static OperateResult<string, byte[]> ExtraMqttReceiveData( byte mqttCode, byte[] data , AesCryptography aesCryptography = null )
		{
			if (data.Length < 2) return new OperateResult<string, byte[]>( StringResources.Language.ReceiveDataLengthTooShort + data.Length );

			int topicLength = data[0] * 256 + data[1];
			if (data.Length < 2 + topicLength) return new OperateResult<string, byte[]>( $"Code[{mqttCode:X2}] ExtraMqttReceiveData Error: {SoftBasic.ByteToHexString( data, ' ' )}" );

			string topic = topicLength > 0 ? Encoding.UTF8.GetString( data, 2, topicLength ) : string.Empty;
			byte[] payload = new byte[data.Length - topicLength - 2];
			Array.Copy( data, topicLength + 2, payload, 0, payload.Length );

			if(aesCryptography != null)
			{
				try
				{
					payload = aesCryptography.Decrypt( payload );
				}
				catch( Exception ex )
				{
					return new OperateResult<string, byte[]>( "AES Decrypt failed: " + ex.Message );
				}
			}
			return OperateResult.CreateSuccessResult( topic, payload );
		}

		/// <summary>
		/// 使用指定的对象来返回网络的API接口，前提是传入的数据为json参数，返回的数据为json数据，详细参照说明<br />
		/// Use the specified object to return the API interface of the network, 
		/// provided that the incoming data is json parameters and the returned data is json data, 
		/// please refer to the description for details
		/// </summary>
		/// <param name="mqttSession">当前的对话状态</param>
		/// <param name="message">当前传入的消息内容</param>
		/// <param name="obj">等待解析的api解析的对象</param>
		/// <returns>等待返回客户的结果</returns>
#if NET20 || NET35
		public static OperateResult<string> HandleObjectMethod( MqttSession mqttSession, MqttClientApplicationMessage message, object obj )
#else
		public static async Task<OperateResult<string>> HandleObjectMethod( MqttSession mqttSession, MqttClientApplicationMessage message, object obj )
#endif
		{
			string method = message.Topic; 
			if (method.LastIndexOf( '/' ) >= 0) method = method.Substring( method.LastIndexOf( '/' ) + 1 );

			MethodInfo methodInfo = obj.GetType( ).GetMethod( method );
			if (methodInfo == null) return new OperateResult<string>( $"Current MqttSync Api ：[{method}] not exsist" );

			var apiResult = GetMqttSyncServicesApiFromMethod( "", methodInfo, obj );
			if (!apiResult.IsSuccess) return OperateResult.CreateFailedResult<string>( apiResult );

#if NET20 || NET35
			return HandleObjectMethod( mqttSession, message, apiResult.Content );
#else
			return await HandleObjectMethod( mqttSession, message, apiResult.Content );
#endif
		}

		/// <summary>
		/// 使用指定的对象来返回网络的API接口，前提是传入的数据为json参数，返回的数据为json数据，详细参照说明<br />
		/// Use the specified object to return the API interface of the network, 
		/// provided that the incoming data is json parameters and the returned data is json data, 
		/// please refer to the description for details
		/// </summary>
		/// <param name="mqttSession">当前的对话状态</param>
		/// <param name="message">当前传入的消息内容</param>
		/// <param name="apiInformation">当前已经解析好的Api内容对象</param>
		/// <returns>等待返回客户的结果</returns>
#if NET20 || NET35
		public static OperateResult<string> HandleObjectMethod( MqttSession mqttSession, MqttClientApplicationMessage message, MqttRpcApiInfo apiInformation )
#else
		public static async Task<OperateResult<string>> HandleObjectMethod( MqttSession mqttSession, MqttClientApplicationMessage message, MqttRpcApiInfo apiInformation )
#endif
		{
			JObject jObject;
			object[] paras;
			object retObject = null;

			if (apiInformation.PermissionAttribute != null)
			{
				if (Authorization.asdniasnfaksndiqwhawfskhfaiw( ))
				{
					if (!apiInformation.PermissionAttribute.CheckClientID( mqttSession.ClientId ))
						return new OperateResult<string>( $"Mqtt RPC Api ：[{apiInformation.ApiTopic}] Check ClientID[{mqttSession.ClientId}] failed, access not permission" );

					if (!apiInformation.PermissionAttribute.CheckUserName( mqttSession.UserName ))
						return new OperateResult<string>( $"Mqtt RPC Api ：[{apiInformation.ApiTopic}] Check Username[{mqttSession.UserName}] failed, access not permission" );
				}
				else
				{
					return new OperateResult<string>( $"Permission function need authorization ：{StringResources.Language.InsufficientPrivileges}" );
				}
			}

			try
			{
				if (apiInformation.Method != null)
				{
					string json = Encoding.UTF8.GetString( message.Payload );
					jObject = string.IsNullOrEmpty( json ) ? new JObject( ) : JObject.Parse( json );

					paras = HslReflectionHelper.GetParametersFromJson( mqttSession, apiInformation.Method.GetParameters( ), json );
#if NET20 || NET35
					retObject = apiInformation.Method.Invoke( apiInformation.SourceObject, paras );
#else
					object obj = apiInformation.Method.Invoke( apiInformation.SourceObject, paras );
					if(obj is Task task)
					{
						await task;
						retObject = task.GetType( ).GetProperty( "Result" )?.GetValue( task, null );
					}
					else
					{
						retObject = obj;
					}
#endif
				}
				else if (apiInformation.Property != null)
				{
					retObject = apiInformation.Property.GetValue( apiInformation.SourceObject, null );
				}
			}
			catch (Exception ex)
			{
				return new OperateResult<string>( $"Mqtt RPC Api ：[{apiInformation.ApiTopic}] Wrong，Reason：" + ex.Message );
			}

			return HslReflectionHelper.GetOperateResultJsonFromObj( retObject );
		}

		/// <inheritdoc cref="GetSyncServicesApiInformationFromObject(string, object, HslMqttPermissionAttribute)"/>
		public static List<MqttRpcApiInfo> GetSyncServicesApiInformationFromObject( object obj )
		{
			if(obj is Type type)
				return GetSyncServicesApiInformationFromObject( type.Name, type );
			else
				return GetSyncServicesApiInformationFromObject( obj.GetType( ).Name, obj );
		}

		/// <summary>
		/// 根据当前的对象定义的方法信息，获取到所有支持ApiTopic的方法列表信息，包含API名称，示例参数数据，描述信息。<br />
		/// According to the method information defined by the current object, the list information of all methods that support ApiTopic is obtained, 
		/// including the API name, sample parameter data, and description information.
		/// </summary>
		/// <param name="api">指定的ApiTopic的前缀，可以理解为控制器，如果为空，就不携带控制器。</param>
		/// <param name="obj">实际的等待解析的对象</param>
		/// <param name="permissionAttribute">默认的权限特性</param>
		/// <returns>返回所有API说明的列表，类型为<see cref="MqttRpcApiInfo"/></returns>
		public static List<MqttRpcApiInfo> GetSyncServicesApiInformationFromObject( string api, object obj, HslMqttPermissionAttribute permissionAttribute = null )
		{
			Type objType = null;
			if (obj is Type type)   // 表示注册的是静态的方法或是静态属性
			{
				objType = type;
				obj = null;
			}
			else
			{ 
				objType = obj.GetType( ); 
			}

			MethodInfo[] methodInfos = objType.GetMethods( );
			List<MqttRpcApiInfo> mqttSyncServices = new List<MqttRpcApiInfo>( );
			foreach (var method in methodInfos)
			{
				var apiResult = GetMqttSyncServicesApiFromMethod( api, method, obj, permissionAttribute );
				if (!apiResult.IsSuccess) continue;

				mqttSyncServices.Add( apiResult.Content );
			}
			PropertyInfo[] propertyInfos = objType.GetProperties( );
			foreach (var property in propertyInfos)
			{
				var apiResult = GetMqttSyncServicesApiFromProperty( api, property, obj, permissionAttribute );
				if (!apiResult.IsSuccess) continue;

				if(!apiResult.Content1.PropertyUnfold)
					mqttSyncServices.Add( apiResult.Content2 );
				else
				{
					if (property.GetValue( obj, null ) == null) continue;
					var apis = GetSyncServicesApiInformationFromObject( apiResult.Content2.ApiTopic, property.GetValue( obj, null ), permissionAttribute );
					mqttSyncServices.AddRange( apis );
				}
			}

			return mqttSyncServices;
		}

		private static string GetReturnTypeDescription( Type returnType )
		{
			if (returnType.IsSubclassOf( typeof( OperateResult ) ) )
			{
				if (returnType == typeof( OperateResult )) return returnType.Name;
				if (returnType.GetProperty( "Content" ) != null)
				{
					return $"OperateResult<{returnType.GetProperty( "Content" ).PropertyType.Name}>";
				}
				else
				{
					StringBuilder sb = new StringBuilder( "OperateResult<" );
					for (int i = 1; i <= 10; i++)
					{
						if (returnType.GetProperty( "Content" + i.ToString( ) ) != null)
						{
							if (i != 1) sb.Append( "," );
							sb.Append( returnType.GetProperty( "Content" + i.ToString( ) ).PropertyType.Name );
						}
						else
						{
							break;
						}
					}
					sb.Append( ">" );
					return sb.ToString( );
				}
			}
			else
			{
				return returnType.Name;
			}
		}

		/// <summary>
		/// 根据当前的方法的委托信息和类对象，生成<see cref="MqttRpcApiInfo"/>的API对象信息。
		/// </summary>
		/// <param name="api">Api头信息</param>
		/// <param name="method">方法的委托</param>
		/// <param name="obj">当前注册的API的源对象</param>
		/// <param name="permissionAttribute">默认的权限特性</param>
		/// <returns>返回是否成功的结果对象</returns>
		public static OperateResult<MqttRpcApiInfo> GetMqttSyncServicesApiFromMethod( string api, MethodInfo method, object obj, HslMqttPermissionAttribute permissionAttribute = null )
		{
			object[] attrs = method.GetCustomAttributes( typeof( HslMqttApiAttribute ), false );
			if (attrs == null || attrs.Length == 0) return new OperateResult<MqttRpcApiInfo>( $"Current Api ：[{method}] not support Api attribute" );

			HslMqttApiAttribute apiAttribute = (HslMqttApiAttribute)attrs[0];
			MqttRpcApiInfo apiInformation = new MqttRpcApiInfo( );
			apiInformation.SourceObject = obj;
			apiInformation.Method = method;
			apiInformation.Description = apiAttribute.Description;
			apiInformation.HttpMethod = apiAttribute.HttpMethod.ToUpper( );
			if (string.IsNullOrEmpty( apiAttribute.ApiTopic )) apiAttribute.ApiTopic = method.Name;

			if (permissionAttribute == null)
			{
				attrs = method.GetCustomAttributes( typeof( HslMqttPermissionAttribute ), false );
				if (attrs?.Length > 0) apiInformation.PermissionAttribute = (HslMqttPermissionAttribute)attrs[0];
			}
			else
			{
				apiInformation.PermissionAttribute = permissionAttribute;
			}

			if (string.IsNullOrEmpty( api ))
				apiInformation.ApiTopic = apiAttribute.ApiTopic;
			else
				apiInformation.ApiTopic = api + "/" + apiAttribute.ApiTopic;
			var parameters = method.GetParameters( );
			StringBuilder sb = new StringBuilder( );
#if NET20 || NET35
			sb.Append( GetReturnTypeDescription( method.ReturnType ) );
#else
			if (method.ReturnType.IsSubclassOf( typeof( Task ) ))
				sb.Append( $"Task<{GetReturnTypeDescription(method.ReturnType.GetProperty( "Result" ).PropertyType)}>" );
			else
				sb.Append( GetReturnTypeDescription( method.ReturnType ) );
#endif
			sb.Append( " " );
			sb.Append( apiInformation.ApiTopic );
			sb.Append( "(" );
			for (int i = 0; i < parameters.Length; i++)
			{
				if (parameters[i].ParameterType != typeof( ISessionContext ))
				{
					sb.Append( parameters[i].ParameterType.Name );
					sb.Append( " " );
					sb.Append( parameters[i].Name );
					if (i != parameters.Length - 1)
						sb.Append( "," );
				}
			}
			sb.Append( ")" );
			apiInformation.MethodSignature = sb.ToString( );

			apiInformation.ExamplePayload = HslReflectionHelper.GetParametersFromJson( method, parameters ).ToString( );
			return OperateResult.CreateSuccessResult( apiInformation );
		}

		/// <summary>
		/// 根据当前的方法的委托信息和类对象，生成<see cref="MqttRpcApiInfo"/>的API对象信息。
		/// </summary>
		/// <param name="api">Api头信息</param>
		/// <param name="property">方法的委托</param>
		/// <param name="obj">当前注册的API的源对象</param>
		/// <param name="permissionAttribute">默认的权限特性</param>
		/// <returns>返回是否成功的结果对象</returns>
		public static OperateResult<HslMqttApiAttribute, MqttRpcApiInfo> GetMqttSyncServicesApiFromProperty( string api, PropertyInfo property, object obj, HslMqttPermissionAttribute permissionAttribute = null )
		{
			object[] attrs = property.GetCustomAttributes( typeof( HslMqttApiAttribute ), false );
			if (attrs == null || attrs.Length == 0) return new OperateResult<HslMqttApiAttribute, MqttRpcApiInfo>( $"Current Api ：[{property}] not support Api attribute" );

			HslMqttApiAttribute apiAttribute = (HslMqttApiAttribute)attrs[0];
			MqttRpcApiInfo apiInformation = new MqttRpcApiInfo( );
			apiInformation.SourceObject = obj;
			apiInformation.Property = property;
			apiInformation.Description = apiAttribute.Description;
			apiInformation.HttpMethod = apiAttribute.HttpMethod.ToUpper( );
			if (string.IsNullOrEmpty( apiAttribute.ApiTopic )) apiAttribute.ApiTopic = property.Name;

			if (permissionAttribute == null)
			{
				attrs = property.GetCustomAttributes( typeof( HslMqttPermissionAttribute ), false );
				if (attrs?.Length > 0) apiInformation.PermissionAttribute = (HslMqttPermissionAttribute)attrs[0];
			}
			else
			{
				apiInformation.PermissionAttribute = permissionAttribute;
			}

			if (string.IsNullOrEmpty( api ))
				apiInformation.ApiTopic = apiAttribute.ApiTopic;
			else
				apiInformation.ApiTopic = api + "/" + apiAttribute.ApiTopic;

			StringBuilder sb = new StringBuilder( );
			sb.Append( GetReturnTypeDescription( property.PropertyType ) );
			sb.Append( " " );
			sb.Append( apiInformation.ApiTopic );
			sb.Append( " { " );
			if (property.CanRead) sb.Append( "get; " );
			if (property.CanWrite) sb.Append( "set; " );
			sb.Append( "}" );
			apiInformation.MethodSignature = sb.ToString( );
			apiInformation.ExamplePayload = string.Empty;
			return OperateResult.CreateSuccessResult( apiAttribute, apiInformation );
		}

		#endregion

		/// <summary>
		/// 判断当前服务器的实际的 topic 的主题，是否满足通配符格式的订阅主题 subTopic
		/// </summary>
		/// <param name="topic">服务器的实际的主题信息</param>
		/// <param name="subTopic">客户端订阅的基于通配符的格式</param>
		/// <returns>如果返回True, 说明当前匹配成功，应该发送订阅操作</returns>
		public static bool CheckMqttTopicWildcards( string topic, string subTopic )
		{
			if (subTopic == "#") return true;
			if (subTopic.EndsWith( "/#" ))
			{
				if (subTopic.Contains( "/+/" ))   // finance/+/ibm/#
				{
					subTopic = subTopic.Replace( "[", "\\[" );
					subTopic = subTopic.Replace( "]", "\\]" );
					subTopic = subTopic.Replace( ".", "\\." );
					subTopic = subTopic.Replace( "*", "\\*" );
					subTopic = subTopic.Replace( "{", "\\{" );
					subTopic = subTopic.Replace( "}", "\\}" );
					subTopic = subTopic.Replace( "?", "\\?" );
					subTopic = subTopic.Replace( "$", "\\$" );

					subTopic = subTopic.Replace( "/+", "/[^/]+" );
					subTopic = subTopic.RemoveLast( 2 );
					subTopic = subTopic + @"(/[\S\s]+$|$)";
					return Regex.IsMatch( topic, subTopic );
				}
				else      // finance/stock/#
				{
					if (subTopic.Length == 2) return false;
					if (topic == subTopic.RemoveLast( 2 )) return true;
					if (topic.StartsWith( subTopic.RemoveLast( 1 ) )) return true;
					return false;
				}
			}
			if (subTopic == "+") return !topic.Contains( "/" );
			if (subTopic.EndsWith( "/+" ))     // finance/stock/+
			{
				if (subTopic.Length == 2) return false;
				if (!topic.StartsWith( subTopic.RemoveLast( 1 ) )) return false;
				if (topic.Length == subTopic.Length - 1) return false;
				if (topic.Substring( subTopic.Length - 1 ).Contains( "/" )) return false;
				return true;
			}
			else if (subTopic.Contains( "/+/" ))
			{
				// finance/stock/+/currentprice
				subTopic = subTopic.Replace( "[", "\\[" );
				subTopic = subTopic.Replace( "]", "\\]" );
				subTopic = subTopic.Replace( ".", "\\." );
				subTopic = subTopic.Replace( "*", "\\*" );
				subTopic = subTopic.Replace( "{", "\\{" );
				subTopic = subTopic.Replace( "}", "\\}" );
				subTopic = subTopic.Replace( "?", "\\?" );
				subTopic = subTopic.Replace( "$", "\\$" );

				subTopic = subTopic.Replace( "/+", "/[^/]+" );
				return Regex.IsMatch( topic, subTopic );
			}
			return topic == subTopic;
		}

	}
}
