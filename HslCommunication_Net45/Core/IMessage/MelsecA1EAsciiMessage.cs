using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HslCommunication.Core.IMessage
{
	/// <summary>
	/// 三菱的A兼容1E帧ASCII协议解析规则
	/// </summary>
	public class MelsecA1EAsciiMessage : INetMessage
	{
		/// <inheritdoc cref="INetMessage.ProtocolHeadBytesLength"/>
		public int ProtocolHeadBytesLength => 4;

		/// <inheritdoc cref="INetMessage.GetContentLengthByHeadBytes"/>
		public int GetContentLengthByHeadBytes( )
		{
			if (HeadBytes[2] == 0x35 && HeadBytes[3] == 0x42) return 4;   // 异常代码 + 0x00
			else if (HeadBytes[2] == 0x30 && HeadBytes[3] == 0x30)
			{
				int length = Convert.ToInt32( Encoding.ASCII.GetString( SendBytes, 20, 2 ), 16 );
				if (length == 0) length = 256;
				switch (HeadBytes[1])
				{
					case 0x30: return length % 2 == 1 ? length + 1 : length;   // 位单位成批读出后，回复副标题
					case 0x31: return length * 4;                              // 字单位成批读出后，回复副标题
					case 0x32:                                                 // 位单位成批写入后，回复副标题
					case 0x33: return 0;                                       // 字单位成批写入后，回复副标题
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
			if (HeadBytes != null) return ((HeadBytes[0] - SendBytes[0]) == 0x08);
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
