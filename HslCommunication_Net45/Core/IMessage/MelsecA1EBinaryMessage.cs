using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HslCommunication.Core.IMessage
{
	/// <summary>
	/// 三菱的A兼容1E帧协议解析规则
	/// </summary>
	public class MelsecA1EBinaryMessage : INetMessage
	{
		/// <inheritdoc cref="INetMessage.ProtocolHeadBytesLength"/>
		public int ProtocolHeadBytesLength => 2;

		/// <inheritdoc cref="INetMessage.GetContentLengthByHeadBytes"/>
		public int GetContentLengthByHeadBytes( )
		{
			if (HeadBytes[1] == 0x5B) return 2;                           // 异常代码 + 0x00
			else if (HeadBytes[1] == 0x00)
			{
				switch (HeadBytes[0])
				{
					case 0x80: return SendBytes[10] != 0x00 ? ( SendBytes[10] + 1) / 2 : 128;            // 位单位成批读出后，回复副标题
					case 0x81: return SendBytes[10] * 2;                                                 // 字单位成批读出后，回复副标题
					case 0x82:                                                                           // 位单位成批写入后，回复副标题
					case 0x83: return 0;                                                                 // 字单位成批写入后，回复副标题
					default: return 0;
				}
			}
			else
				return 0;

			//在A兼容1E协议中，写入值后，若不发生异常，只返回副标题 + 结束代码(0x00)
			//这已经在协议头部读取过了，后面要读取的长度为0（contentLength=0）
		}

		/// <inheritdoc cref="INetMessage.CheckHeadBytesLegal(byte[])"/>
		public bool CheckHeadBytesLegal( byte[] token )
		{
			if (HeadBytes != null) return ((HeadBytes[0] - SendBytes[0]) == 0x80);
			return false;
		}

		/// <inheritdoc cref="INetMessage.GetHeadBytesIdentity"/>
		public int GetHeadBytesIdentity( ) => 0;

		/// <inheritdoc cref="INetMessage.HeadBytes"/>
		public byte[] HeadBytes { get; set; }

		/// <inheritdoc cref="INetMessage.ContentBytes"/>
		public byte[] ContentBytes { get; set; }

		/// <inheritdoc cref="INetMessage.SendBytes"/>
		public byte[] SendBytes { get; set; }

	}

}
