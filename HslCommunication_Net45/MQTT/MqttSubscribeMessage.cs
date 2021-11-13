using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using HslCommunication.BasicFramework;

namespace HslCommunication.MQTT
{
	/// <summary>
	/// 订阅的消息类，用于客户端向服务器请求订阅的信息<br />
	/// Subscribed message class, used by the client to request subscription information from the server
	/// </summary>
	public class MqttSubscribeMessage
	{
		/// <summary>
		/// 实例化一个默认的对象<br />
		/// Instantiate a default object
		/// </summary>
		public MqttSubscribeMessage( )
		{
			QualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce;
			Identifier            = 1;
		}

		/// <summary>
		/// 这个字段表示应用消息分发的服务质量等级保证。分为，最多一次，最少一次，正好一次<br />
		/// This field indicates the quality of service guarantee for application message distribution. Divided into, at most once, at least once, exactly once
		/// </summary>
		/// <remarks>
		/// 在实际的开发中的情况下，最多一次是最省性能的，正好一次是最消耗性能的，如果应有场景为推送实时的数据，那么，最多一次的性能是最高的
		/// </remarks>
		public MqttQualityOfServiceLevel QualityOfServiceLevel { get; set; }

		/// <summary>
		/// 当前的消息的标识符，当质量等级为0的时候，不需要重发以及考虑标识情况<br />
		/// The identifier of the current message, when the quality level is 0, do not need to retransmit and consider the identification situation
		/// </summary>
		public int Identifier { get; set; }

		/// <summary>
		/// 当前订阅的所有的主题的数组信息<br />
		/// Array information of all topics currently subscribed
		/// </summary>
		public string[] Topics { get; set; }

		#region Object Override

		/// <inheritdoc/>
		public override string ToString( ) => $"MqttSubcribeMessage{SoftBasic.ArrayFormat( Topics )}";

		#endregion
	}
}
