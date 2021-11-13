using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HslCommunication.Core.IMessage
{
	/// <summary>
	/// 横河PLC的以太网的二进制协议
	/// </summary>
	public class YokogawaLinkBinaryMessage : INetMessage
	{
		/// <inheritdoc cref="INetMessage.ProtocolHeadBytesLength"/>
		public int ProtocolHeadBytesLength => 4;

		/// <inheritdoc cref="INetMessage.GetContentLengthByHeadBytes"/>
		public int GetContentLengthByHeadBytes( ) => HeadBytes[2] * 256 + HeadBytes[3];

		/// <inheritdoc cref="INetMessage.CheckHeadBytesLegal(byte[])"/>
		public bool CheckHeadBytesLegal( byte[] token ) => true;

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
