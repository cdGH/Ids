using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HslCommunication.Core.IMessage
{
	/// <summary>
	/// 用于和 AllenBradley PLC 交互的消息协议类
	/// </summary>
	public class AllenBradleyMessage : INetMessage
	{
		/// <inheritdoc cref="INetMessage.ProtocolHeadBytesLength"/>
		public int ProtocolHeadBytesLength => 24;

		/// <inheritdoc cref="INetMessage.GetContentLengthByHeadBytes"/>
		public int GetContentLengthByHeadBytes( ) => BitConverter.ToUInt16( HeadBytes, 2 );

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
