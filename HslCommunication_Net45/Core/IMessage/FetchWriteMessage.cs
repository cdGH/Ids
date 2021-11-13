using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HslCommunication.Core.IMessage
{
	/// <summary>
	/// 西门子Fetch/Write消息解析协议
	/// </summary>
	public class FetchWriteMessage : INetMessage
	{
		/// <inheritdoc cref="INetMessage.ProtocolHeadBytesLength"/>
		public int ProtocolHeadBytesLength => 16;

		/// <inheritdoc cref="INetMessage.GetContentLengthByHeadBytes"/>
		public int GetContentLengthByHeadBytes( )
		{
			if (HeadBytes[5] == 0x05 || HeadBytes[5] == 0x04) return 0;
			if (HeadBytes[5] == 0x06)
			{
				if (SendBytes == null) return 0;
				if (HeadBytes[8] != 0x00) return 0; // 发生了错误
				if (SendBytes[8] == 0x01 || SendBytes[8] == 0x06 || SendBytes[8] == 0x07)
					return (SendBytes[12] * 256 + SendBytes[13]) * 2;
				return SendBytes[12] * 256 + SendBytes[13];
			}
			else if (HeadBytes[5] == 0x03)
			{
				if (HeadBytes[8] == 0x01 || HeadBytes[8] == 0x06 || HeadBytes[8] == 0x07)
					return (HeadBytes[12] * 256 + HeadBytes[13]) * 2;
				return HeadBytes[12] * 256 + HeadBytes[13];
			}
			return 0;
		}

		/// <inheritdoc cref="INetMessage.CheckHeadBytesLegal(byte[])"/>
		public bool CheckHeadBytesLegal( byte[] token )
		{
			if (HeadBytes == null) return false;

			if (HeadBytes[0] == 0x53 && HeadBytes[1] == 0x35)
				return true;
			else
				return false;
		}

		/// <inheritdoc cref="INetMessage.GetHeadBytesIdentity"/>
		public int GetHeadBytesIdentity( ) => HeadBytes[3];

		/// <inheritdoc cref="INetMessage.HeadBytes"/>
		public byte[] HeadBytes { get; set; }

		/// <inheritdoc cref="INetMessage.ContentBytes"/>
		public byte[] ContentBytes { get; set; }

		/// <inheritdoc cref="INetMessage.SendBytes"/>
		public byte[] SendBytes { get; set; }
	}
}
