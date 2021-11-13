using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HslCommunication.WebSocket
{
	/// <summary>
	/// websocket 协议的 op的枚举信息
	/// </summary>
	public enum WSOpCode
	{
		/// <summary>
		/// 连续消息分片
		/// </summary>
		ContinuousMessageFragment = 0x00,

		/// <summary>
		/// 文本消息分片
		/// </summary>
		TextMessageFragment = 0x01,

		/// <summary>
		/// 二进制消息分片
		/// </summary>
		BinaryMessageFragment = 0x02,

		/// <summary>
		/// 连接关闭
		/// </summary>
		ConnectionClose = 0x08,

		/// <summary>
		/// 心跳检查
		/// </summary>
		HeartbeatPing = 0x09,

		/// <summary>
		/// 心跳检查
		/// </summary>
		HeartbeatPong = 0x0A
	}
}
