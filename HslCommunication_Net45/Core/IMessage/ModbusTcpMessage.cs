using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HslCommunication.Core.IMessage
{
	/// <summary>
	/// Modbus-Tcp协议支持的消息解析类
	/// </summary>
	public class ModbusTcpMessage : INetMessage
	{
		/// <inheritdoc cref="INetMessage.ProtocolHeadBytesLength"/>
		public int ProtocolHeadBytesLength => 8;

		/// <inheritdoc cref="INetMessage.GetContentLengthByHeadBytes"/>
		public int GetContentLengthByHeadBytes( )
		{
			/************************************************************************
			 * 
			 *    说明：为了应对有些特殊的设备，在整个指令的开端会增加一个额外的数据的时候
			 * 
			 ************************************************************************/
			 
			if (HeadBytes?.Length >= ProtocolHeadBytesLength)
			{
				int length = HeadBytes[4] * 256 + HeadBytes[5];
				if (length == 0)
				{
					byte[] buffer = new byte[ProtocolHeadBytesLength - 1];
					for (int i = 0; i < buffer.Length; i++)
					{
						buffer[i] = HeadBytes[i + 1];
					}
					HeadBytes = buffer;
					return HeadBytes[5] * 256 + HeadBytes[6] - 1;
				}
				else
				{
					return length - 2;
				}
			}
			else
				return 0;
		}

		/// <inheritdoc cref="INetMessage.CheckHeadBytesLegal(byte[])"/>
		public bool CheckHeadBytesLegal( byte[] token )
		{
			if (IsCheckMessageId)
			{
				if (HeadBytes == null) return false;
				if (SendBytes[0] != HeadBytes[0] || SendBytes[1] != HeadBytes[1]) return false;
				return HeadBytes[2] == 0x00 && HeadBytes[3] == 0x00;
			}
			else
			{
				return true;
			}
		}

		/// <inheritdoc cref="INetMessage.GetHeadBytesIdentity"/>
		public int GetHeadBytesIdentity( ) => HeadBytes[0] * 256 + HeadBytes[1];

		/// <inheritdoc cref="INetMessage.HeadBytes"/>
		public byte[] HeadBytes { get; set; }

		/// <inheritdoc cref="INetMessage.ContentBytes"/>
		public byte[] ContentBytes { get; set; }

		/// <inheritdoc cref="INetMessage.SendBytes"/>
		public byte[] SendBytes { get; set; }

		/// <summary>
		/// 获取或设置是否进行检查返回的消息ID和发送的消息ID是否一致，默认为true，也就是检查<br />
		/// Get or set whether to check whether the returned message ID is consistent with the sent message ID, the default is true, that is, check
		/// </summary>
		public bool IsCheckMessageId { get; set; } = true;
	}
}
