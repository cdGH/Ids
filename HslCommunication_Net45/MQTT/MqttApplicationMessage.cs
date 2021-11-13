using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HslCommunication.MQTT
{
	/// <summary>
	/// Mqtt的一次完整消息内容，包含主题，负载数据，消息等级。<br />
	/// Mqtt's complete message content, including subject, payload data, message level.
	/// </summary>
	public class MqttApplicationMessage
	{
		/// <summary>
		/// 这个字段表示应用消息分发的服务质量等级保证。分为，最多一次，最少一次，正好一次，只发不推送。<br />
		/// This field indicates the quality of service level guarantee for application message distribution. Divided into, at most once, at least once, exactly once
		/// </summary>
		/// <remarks>
		/// 在实际的开发中的情况下，最多一次是最省性能的，正好一次是最消耗性能的，如果应有场景为推送实时的数据，那么，最多一次的性能是最高的
		/// </remarks>
		public MqttQualityOfServiceLevel QualityOfServiceLevel { get; set; }

		/// <summary>
		/// 主题名（Topic Name）用于识别有效载荷数据应该被发布到哪一个信息通道。<br />
		/// The Topic Name is used to identify which information channel the payload data should be published to.
		/// </summary>
		/// <remarks>
		/// UTF-8编码字符串中的字符数据必须是按照Unicode规范 [Unicode] 定义的和在RFC3629 [RFC3629] 中重申的有效的UTF-8格式。特别需要指出的是，
		/// 这些数据不能包含字符码在U+D800和U+DFFF之间的数据。如果服务端或客户端收到了一个包含无效UTF-8字符的控制报文，它必须关闭网络连接 [MQTT-1.5.3-1].
		/// 
		/// PUBLISH报文中的主题名不能包含通配符 [MQTT-3.3.2-2]。
		/// </remarks>
		public string Topic { get; set; }

		/// <summary>
		/// 有效载荷包含将被发布的应用消息。数据的内容和格式是应用特定的。<br />
		/// The payload contains application messages to be published. The content and format of the data is application specific.
		/// </summary>
		public byte[] Payload { get; set; }

		/// <summary>
		/// 该消息是否在服务器端进行保留，详细的说明参照文档的备注<br />
		/// Whether the message is retained on the server. For details, refer to the remarks of the document.
		/// </summary>
		/// <remarks>
		/// 如果客户端发给服务端的PUBLISH报文的保留（RETAIN）标志被设置为1，服务端必须存储这个应用消息和它的服务质量等级（QoS），
		/// 以便它可以被分发给未来的主题名匹配的订阅者 [MQTT-3.3.1-5]。一个新的订阅建立时，对每个匹配的主题名
		/// ，如果存在最近保留的消息，它必须被发送给这个订阅者 [MQTT-3.3.1-6]。如果服务端收到一条保留（RETAIN）标志为1的QoS 0消息，
		/// 它必须丢弃之前为那个主题保留的任何消息。它应该将这个新的QoS 0消息当作那个主题的新保留消息，但是任何时候都可以选择丢弃它 — 如果这种情况发生了，
		/// 那个主题将没有保留消息 [MQTT-3.3.1-7]
		/// </remarks>
		public bool Retain { get; set; }

		/// <inheritdoc/>
		public override string ToString( ) => $"{Topic}:{Encoding.UTF8.GetString( Payload )}";
	}
}
