using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using HslCommunication.Reflection;
using Newtonsoft.Json;

namespace HslCommunication.MQTT
{
	/// <summary>
	/// Mqtt的同步网络服务的单Api信息描述类<br />
	/// Single Api information description class of Mqtt's synchronous network service
	/// </summary>
	public class MqttRpcApiInfo
	{
		/// <summary>
		/// 当前的Api的路由信息，对于注册服务来说，是类名/方法名
		/// </summary>
		public string ApiTopic { get; set; }

		/// <summary>
		/// 当前的Api的路由说明
		/// </summary>
		public string Description { get; set; }

		/// <summary>
		/// 当前方法的签名
		/// </summary>
		public string MethodSignature { get; set; }

		/// <summary>
		/// 当前Api的调用次数
		/// </summary>
		public long CalledCount { get => calledCount; set => calledCount = value; }

		/// <summary>
		/// 示例的
		/// </summary>
		public string ExamplePayload { get; set; }

		/// <summary>
		/// 当前Api的调用总耗时，单位是秒
		/// </summary>
		public double SpendTotalTime { get => spendTotalTime / 100000d; set => spendTotalTime = (long)(value * 100000); }

		/// <summary>
		/// 当前Api是否为方法，如果是方法，就为true，否则为false
		/// </summary>
		public bool IsMethodApi { get; set; }

		/// <summary>
		/// 如果当前的API接口是支持Http的请求方式，当前属性有效，例如GET,POST
		/// </summary>
		public string HttpMethod { get; set; } = "GET";

		/// <summary>
		/// 当前的Api的方法是否是异步的Task类型
		/// </summary>
		[JsonIgnore]
		public bool IsOperateResultApi { get; set; }

		/// <summary>
		/// 当前的Api关联的方法反射，本属性在JSON中将会忽略
		/// </summary>
		[JsonIgnore]
		public MethodInfo Method { get => method; set { method = value; IsMethodApi = true; } }

		/// <summary>
		/// 当前的Api关联的方法反射，本属性在JSON中将会忽略
		/// </summary>
		[JsonIgnore]
		public PropertyInfo Property { get => property; set { property = value; IsMethodApi = false; } }

		/// <summary>
		/// 当前Api的方法的权限访问反射，本属性在JSON中将会忽略
		/// </summary>
		[JsonIgnore]
		public HslMqttPermissionAttribute PermissionAttribute { get; set; }

		/// <summary>
		/// 当前Api绑定的对象的，实际的接口请求，将会从对象进行调用，本属性在JSON中将会忽略
		/// </summary>
		[JsonIgnore]
		public object SourceObject { get; set; }

		private long calledCount = 0;           // 当前的API的总的调用次数
		private long spendTotalTime = 0;        // 当前的API的总的花费时间，单位，100倍毫秒
		private MethodInfo method;
		private PropertyInfo property;

		/// <summary>
		/// 使用原子的操作增加一次调用次数的数据信息，需要传入当前的消耗的时间，单位为100倍毫秒
		/// </summary>
		/// <param name="timeSpend">当前调用花费的时间，单位为100倍毫秒</param>
		public void CalledCountAddOne( long timeSpend )
		{
			System.Threading.Interlocked.Increment( ref calledCount );
			System.Threading.Interlocked.Add( ref spendTotalTime, timeSpend );
		}

		/// <inheritdoc/>
		public override string ToString( ) => this.ApiTopic;
    }
}
