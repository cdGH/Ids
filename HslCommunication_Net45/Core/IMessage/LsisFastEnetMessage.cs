using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HslCommunication.Core.IMessage
{
	/// <summary>
	/// LSIS的PLC的FastEnet的消息定义
	/// </summary>
	public class LsisFastEnetMessage : INetMessage
	{
		/// <inheritdoc cref="INetMessage.ProtocolHeadBytesLength"/>
		public int ProtocolHeadBytesLength => 20;

		/// <inheritdoc cref="INetMessage.HeadBytes"/>
		public byte[] HeadBytes { get; set; }

		/// <inheritdoc cref="INetMessage.ContentBytes"/>
		public byte[] ContentBytes { get; set; }

		/// <inheritdoc cref="INetMessage.CheckHeadBytesLegal(byte[])"/>
		public bool CheckHeadBytesLegal( byte[] token )
		{
			if (HeadBytes == null) return false;

			return HeadBytes[0] == 0x4C;
		}

		/// <inheritdoc cref="INetMessage.GetContentLengthByHeadBytes"/>
		public int GetContentLengthByHeadBytes( )
		{
			if (HeadBytes?.Length >= 20)
				return BitConverter.ToUInt16( HeadBytes, 16 );
			else
				return 0;
		}

		/// <inheritdoc cref="INetMessage.GetHeadBytesIdentity"/>
		public int GetHeadBytesIdentity( ) => BitConverter.ToUInt16( HeadBytes, 14 );

		/// <inheritdoc cref="INetMessage.SendBytes"/>
		public byte[] SendBytes { get; set; }
	}
}
