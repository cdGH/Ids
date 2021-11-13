using HslCommunication.Algorithms.ConnectPool;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HslCommunication.MQTT
{
	/// <summary>
	/// 关于MqttSyncClient实现的接口<see cref="IConnector"/>，从而实现了数据连接池的操作信息
	/// </summary>
	public class IMqttSyncConnector : IConnector
	{
		/// <summary>
		/// 根据连接的MQTT参数，实例化一个默认的对象<br />
		/// According to the connected MQTT parameters, instantiate a default object
		/// </summary>
		/// <param name="options">连接的参数信息</param>
		public IMqttSyncConnector( MqttConnectionOptions options )
		{
			this.connectionOptions = options;
			SyncClient = new MqttSyncClient( options );
		}

		/// <summary>
		/// 实例化一个默认的对象<br />
		/// Instantiate a default object
		/// </summary>
		public IMqttSyncConnector( )
		{

		}

		/// <inheritdoc cref="IConnector.IsConnectUsing"/>
		public bool IsConnectUsing { get; set; }

		/// <inheritdoc cref="IConnector.GuidToken"/>
		public string GuidToken { get; set; }

		/// <inheritdoc cref="IConnector.LastUseTime"/>
		public DateTime LastUseTime { get; set; }

		/// <summary>
		/// MQTT的连接对象
		/// </summary>
		public MqttSyncClient SyncClient { get; set; }

		/// <inheritdoc cref="IConnector.Close"/>
		public void Close( )
		{
			SyncClient?.ConnectClose( );
		}

		/// <inheritdoc cref="IConnector.Open"/>
		public void Open( )
		{
			SyncClient?.SetPersistentConnection( );
		}

		private MqttConnectionOptions connectionOptions;
	}
}
