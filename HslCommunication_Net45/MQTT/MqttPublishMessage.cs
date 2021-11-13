using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace HslCommunication.MQTT
{
	/// <summary>
	/// Mqtt发送的消息封装对象，是对 <see cref="MqttApplicationMessage"/> 对象的封装，添加了序号，还有是否重发的信息<br />
	/// The message encapsulation object sent by Mqtt is an encapsulation of the <see cref="MqttApplicationMessage"/> object, with the serial number added, and whether to retransmit
	/// </summary>
	public class MqttPublishMessage
	{
		/// <summary>
		/// 实例化一个默认的对象<br />
		/// Instantiate a default object
		/// </summary>
		public MqttPublishMessage( )
		{
			IsSendFirstTime = true;
		}

		/// <summary>
		/// 是否第一次发送数据信息<br />
		/// Whether to send data information for the first time
		/// </summary>
		public bool IsSendFirstTime { get; set; }

		/// <summary>
		/// 当前的消息的标识符，当质量等级为0的时候，不需要重发以及考虑标识情况<br />
		/// The identifier of the current message, when the quality level is 0, do not need to retransmit and consider the identification situation
		/// </summary>
		public int Identifier { get; set; }

		/// <summary>
		/// 当前发布消息携带的mqtt的应用消息，包含主题，消息等级，负载。<br />
		/// The application message of mqtt carried in the current published message, including the subject, message level, and load.
		/// </summary>
		public MqttApplicationMessage Message { get; set; }

	}
}
