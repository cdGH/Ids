using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HslCommunication.MQTT
{
	/// <summary>
	/// Mqtt消息的质量等级<br />
	/// Mqtt message quality level
	/// </summary>
	public enum MqttQualityOfServiceLevel
	{
		/// <summary>
		/// 最多一次
		/// </summary>
		AtMostOnce = 0,

		/// <summary>
		/// 最少一次
		/// </summary>
		AtLeastOnce = 1,

		/// <summary>
		/// 只有一次
		/// </summary>
		ExactlyOnce = 2,

		/// <summary>
		/// 消息只发送到服务器而不触发发布订阅，该消息质量等级只对HSL的MQTT服务器有效<br />
		/// The message is only sent to the server without triggering publish and subscribe, the message quality level is only valid for the HSL MQTT server
		/// </summary>
		OnlyTransfer = 3
	}
}
