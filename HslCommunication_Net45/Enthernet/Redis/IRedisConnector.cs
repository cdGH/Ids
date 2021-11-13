using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HslCommunication.Algorithms.ConnectPool;

namespace HslCommunication.Enthernet.Redis
{
	/// <summary>
	/// 关于Redis实现的接口<see cref="IConnector"/>，从而实现了数据连接池的操作信息
	/// </summary>
	public class IRedisConnector : IConnector
	{
		/// <inheritdoc cref="IConnector.IsConnectUsing"/>
		public bool IsConnectUsing { get; set; }

		/// <inheritdoc cref="IConnector.GuidToken"/>
		public string GuidToken { get; set; }

		/// <inheritdoc cref="IConnector.LastUseTime"/>
		public DateTime LastUseTime { get; set; }

		/// <summary>
		/// Redis的连接对象
		/// </summary>
		public RedisClient Redis { get; set; }

		/// <inheritdoc cref="IConnector.Close"/>
		public void Close( )
		{
			Redis?.ConnectClose( );
		}

		/// <inheritdoc cref="IConnector.Open"/>
		public void Open( )
		{
			Redis?.SetPersistentConnection( );
		}
	}
}
