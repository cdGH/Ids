using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HslCommunication.Core.IMessage
{


	/// <summary>
	/// 基于MC协议的Qna兼容3E帧协议的ASCII通讯消息机制
	/// </summary>
	public class MelsecQnA3EAsciiMessage : INetMessage
	{
		/// <inheritdoc cref="INetMessage.ProtocolHeadBytesLength"/>
		public int ProtocolHeadBytesLength => 18;

		/// <inheritdoc cref="INetMessage.GetContentLengthByHeadBytes"/>
		public int GetContentLengthByHeadBytes( )
		{
			byte[] buffer = new byte[4];
			buffer[0] = HeadBytes[14];
			buffer[1] = HeadBytes[15];
			buffer[2] = HeadBytes[16];
			buffer[3] = HeadBytes[17];
			return Convert.ToInt32( Encoding.ASCII.GetString( buffer ), 16 );
		}

		/// <inheritdoc cref="INetMessage.CheckHeadBytesLegal(byte[])"/>
		public bool CheckHeadBytesLegal( byte[] token )
		{
			if (HeadBytes == null) return false;

			if (HeadBytes[0] == (byte)'D' && HeadBytes[1] == (byte)'0' && HeadBytes[2] == (byte)'0' && HeadBytes[3] == (byte)'0')
				return true;
			else
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
