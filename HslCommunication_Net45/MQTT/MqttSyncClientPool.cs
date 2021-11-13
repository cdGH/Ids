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
using HslCommunication.Algorithms.ConnectPool;
#if !NET35 && !NET20
using System.Threading.Tasks;
#endif

namespace HslCommunication.MQTT
{
	/// <summary>
	/// <b>[商业授权]</b> MqttSyncClient客户端的连接池类对象，用于共享当前的连接池，合理的动态调整连接对象，然后进行高效通信的操作，默认连接数无限大。<br />
	/// <b>[Authorization]</b> The connection pool class object of the MqttSyncClient is used to share the current connection pool, 
	/// reasonably dynamically adjust the connection object, and then perform efficient communication operations, 
	/// The default number of connections is unlimited
	/// </summary>
	/// <remarks>
	/// 本连接池用于提供高并发的读写性能，仅对商业授权用户开放。使用起来和<see cref="MqttSyncClient"/>一致，但是更加的高性能，在密集型数据交互时，优势尤为明显。
	/// </remarks>
	public class MqttSyncClientPool
	{
		/// <summary>
		/// 通过MQTT连接参数实例化一个对象<br />
		/// Instantiate an object through MQTT connection parameters
		/// </summary>
		/// <param name="options">MQTT的连接参数信息</param>
		public MqttSyncClientPool( MqttConnectionOptions options )
		{
			this.connectionOptions = options;
			if (Authorization.asdniasnfaksndiqwhawfskhfaiw( ))
			{
				mqttConnectPool = new ConnectPool<IMqttSyncConnector>( ( ) => new IMqttSyncConnector( options ) );
				mqttConnectPool.MaxConnector = int.MaxValue;
			}
			else
			{
				throw new Exception( StringResources.Language.InsufficientPrivileges );
			}
		}

		/// <summary>
		/// 通过MQTT连接参数以及自定义的初始化方法来实例化一个对象<br />
		/// Instantiate an object through MQTT connection parameters and custom initialization methods
		/// </summary>
		/// <param name="options">MQTT的连接参数信息</param>
		/// <param name="initialize">自定义的初始化方法</param>
		public MqttSyncClientPool( MqttConnectionOptions options, Action<MqttSyncClient> initialize )
		{
			this.connectionOptions = options;
			if (Authorization.asdniasnfaksndiqwhawfskhfaiw( ))
			{
				mqttConnectPool = new ConnectPool<IMqttSyncConnector>( ( ) => {
					MqttSyncClient client = new MqttSyncClient( options );
					initialize( client );
					return new IMqttSyncConnector( ) { SyncClient = client }; } );
				mqttConnectPool.MaxConnector = int.MaxValue;
			}
			else
			{
				throw new Exception( StringResources.Language.InsufficientPrivileges );
			}
		}

		/// <summary>
		/// 获取当前的连接池管理对象信息<br />
		/// Get current connection pool management object information
		/// </summary>
		public ConnectPool<IMqttSyncConnector> GetMqttSyncConnectPool => mqttConnectPool;

		/// <inheritdoc cref="ConnectPool{TConnector}.MaxConnector"/>
		public int MaxConnector
		{
			get => mqttConnectPool.MaxConnector;
			set => mqttConnectPool.MaxConnector = value;
		}
		private OperateResult<T> ConnectPoolExecute<T>( Func<MqttSyncClient, OperateResult<T>> exec )
		{
			if (Authorization.asdniasnfaksndiqwhawfskhfaiw( ))
			{
				IMqttSyncConnector client = mqttConnectPool.GetAvailableConnector( );
				OperateResult<T> result = exec( client.SyncClient );
				mqttConnectPool.ReturnConnector( client );
				return result;
			}
			else
			{
				throw new Exception( StringResources.Language.InsufficientPrivileges );
			}
		}

		private OperateResult<T1, T2> ConnectPoolExecute<T1, T2>( Func<MqttSyncClient, OperateResult<T1, T2>> exec )
		{
			if (Authorization.asdniasnfaksndiqwhawfskhfaiw( ))
			{
				IMqttSyncConnector client = mqttConnectPool.GetAvailableConnector( );
				OperateResult<T1, T2> result = exec( client.SyncClient );
				mqttConnectPool.ReturnConnector( client );
				return result;
			}
			else
			{
				throw new Exception( StringResources.Language.InsufficientPrivileges );
			}
		}

#if !NET35 && !NET20
		private async Task<OperateResult<T>> ConnectPoolExecuteAsync<T>( Func<MqttSyncClient, Task<OperateResult<T>>> exec )
		{
			if (Authorization.asdniasnfaksndiqwhawfskhfaiw( ))
			{
				IMqttSyncConnector client = mqttConnectPool.GetAvailableConnector( );
				OperateResult<T> result = await exec( client.SyncClient );
				mqttConnectPool.ReturnConnector( client );
				return result;
			}
			else
			{
				throw new Exception( StringResources.Language.InsufficientPrivileges );
			}
		}
		private async Task<OperateResult<T1, T2>> ConnectPoolExecuteAsync<T1, T2>( Func<MqttSyncClient, Task<OperateResult<T1, T2>>> execAsync )
		{
			if (Authorization.asdniasnfaksndiqwhawfskhfaiw( ))
			{
				IMqttSyncConnector client = mqttConnectPool.GetAvailableConnector( );
				OperateResult<T1, T2> result = await execAsync( client.SyncClient );
				mqttConnectPool.ReturnConnector( client );
				return result;
			}
			else
			{
				throw new Exception( StringResources.Language.InsufficientPrivileges );
			}
		}
#endif
		#region Public Method

		/// <inheritdoc cref="MqttSyncClient.Read(string, byte[], Action{long, long}, Action{string, string}, Action{long, long})"/>
		public OperateResult<string, byte[]> Read( string topic, byte[] payload,
			Action<long, long> sendProgress = null,
			Action<string, string> handleProgress = null,
			Action<long, long> receiveProgress = null ) => ConnectPoolExecute( m => m.Read( topic, payload, sendProgress, handleProgress, receiveProgress ) );

		/// <inheritdoc cref="MqttSyncClient.ReadString(string, string, Action{long, long}, Action{string, string}, Action{long, long})"/>
		public OperateResult<string, string> ReadString( string topic, string payload,
			Action<long, long> sendProgress = null,
			Action<string, string> handleProgress = null,
			Action<long, long> receiveProgress = null ) => ConnectPoolExecute( m => m.ReadString( topic, payload, sendProgress, handleProgress, receiveProgress ) );

		/// <inheritdoc cref="MqttSyncClient.ReadRpc{T}(string, string)"/>
		public OperateResult<T> ReadRpc<T>( string topic, string payload ) => ConnectPoolExecute( m => m.ReadRpc<T>( topic, payload ) );

		/// <inheritdoc cref="MqttSyncClient.ReadRpc{T}(string, object)"/>
		public OperateResult<T> ReadRpc<T>( string topic, object payload ) => ConnectPoolExecute( m => m.ReadRpc<T>( topic, payload ) );

		/// <inheritdoc cref="MqttSyncClient.ReadRpcApis"/>
		public OperateResult<MqttRpcApiInfo[]> ReadRpcApis( ) => ConnectPoolExecute( m => m.ReadRpcApis(  ) );

		/// <inheritdoc cref="MqttSyncClient.ReadRpcApiLog(string)"/>
		public OperateResult<long[]> ReadRpcApiLog( string api ) => ConnectPoolExecute( m => m.ReadRpcApiLog( api ) );

		/// <inheritdoc cref="MqttSyncClient.ReadRetainTopics"/>
		public OperateResult<string[]> ReadRetainTopics( ) => ConnectPoolExecute( m => m.ReadRetainTopics( ) );

		/// <inheritdoc cref="MqttSyncClient.ReadTopicPayload(string, Action{long, long})"/>
		public OperateResult<MqttClientApplicationMessage> ReadTopicPayload( string topic, Action<long, long> receiveProgress = null ) => ConnectPoolExecute( m => m.ReadTopicPayload( topic, receiveProgress ) );
		#endregion

		#region Public Method Async
#if !NET35 && !NET20
		/// <inheritdoc cref="Read(string, byte[], Action{long, long}, Action{string, string}, Action{long, long})"/>
		public async Task<OperateResult<string, byte[]>> ReadAsync( string topic, byte[] payload,
			Action<long, long> sendProgress = null,
			Action<string, string> handleProgress = null,
			Action<long, long> receiveProgress = null ) => await ConnectPoolExecuteAsync( m => m.ReadAsync( topic, payload, sendProgress, handleProgress, receiveProgress ) );

		/// <inheritdoc cref="ReadString(string, string, Action{long, long}, Action{string, string}, Action{long, long})"/>
		public async Task<OperateResult<string, string>> ReadStringAsync( string topic, string payload,
			Action<long, long> sendProgress = null,
			Action<string, string> handleProgress = null,
			Action<long, long> receiveProgress = null ) => await ConnectPoolExecuteAsync( m => m.ReadStringAsync( topic, payload, sendProgress, handleProgress, receiveProgress ) );

		/// <inheritdoc cref="ReadRpc{T}(string, string)"/>
		public async Task<OperateResult<T>> ReadRpcAsync<T>( string topic, string payload ) => await ConnectPoolExecuteAsync( m => m.ReadRpcAsync<T>( topic, payload ) );

		/// <inheritdoc cref="ReadRpc{T}(string, object)"/>
		public async Task<OperateResult<T>> ReadRpcAsync<T>( string topic, object payload ) => await ConnectPoolExecuteAsync( m => m.ReadRpcAsync<T>( topic, payload ) );

		/// <inheritdoc cref="ReadRpcApis"/>
		public async Task<OperateResult<MqttRpcApiInfo[]>> ReadRpcApisAsync( ) => await ConnectPoolExecuteAsync( m => m.ReadRpcApisAsync( ) );

		/// <inheritdoc cref="ReadRpcApiLog(string)"/>
		public async Task<OperateResult<long[]>> ReadRpcApiLogAsync( string api ) => await ConnectPoolExecuteAsync( m => m.ReadRpcApiLogAsync(api ) );

		/// <inheritdoc cref="MqttSyncClient.ReadRetainTopics"/>
		public async Task<OperateResult<string[]>> ReadRetainTopicsAsync( ) => await ConnectPoolExecuteAsync( m => m.ReadRetainTopicsAsync( ) );

		/// <inheritdoc cref="MqttSyncClient.ReadTopicPayload(string, Action{long, long})"/>
		public async Task<OperateResult<MqttClientApplicationMessage>> ReadTopicPayloadAsync( string topic, Action<long, long> receiveProgress = null ) => await ConnectPoolExecuteAsync( m => m.ReadTopicPayloadAsync( topic, receiveProgress ) );
#endif
		#endregion

		#region Private Member

		private MqttConnectionOptions connectionOptions;                                   // 连接的MQTT的基本信息
		private ConnectPool<IMqttSyncConnector> mqttConnectPool;                           // 连接池对象

		#endregion

		#region Object Override

		/// <inheritdoc/>
		public override string ToString( ) => $"MqttSyncClientPool[{mqttConnectPool.MaxConnector}]";

		#endregion
	}
}
