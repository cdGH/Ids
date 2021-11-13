using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
#if !NET35 && !NET20
using System.Threading.Tasks;
#endif

namespace HslCommunication.WebSocket
{
	/// <summary>
	/// websocket 协议下的单个消息的数据对象<br />
	/// Data object for a single message under the websocket protocol
	/// </summary>
	public class WebSocketMessage
	{
		/// <summary>
		/// 是否存在掩码<br />
		/// Whether a mask exists
		/// </summary>
		public bool HasMask { get; set; }

		/// <summary>
		/// 当前的websocket的操作码<br />
		/// The current websocket opcode
		/// </summary>
		public int OpCode { get; set; }

		/// <summary>
		/// 负载数据
		/// </summary>
		public byte[] Payload { get; set; }

		/// <inheritdoc/>
		public override string ToString( ) => $"OpCode[{OpCode}] HasMask[{HasMask}] Payload: {Encoding.UTF8.GetString(Payload)}";
	}
}
