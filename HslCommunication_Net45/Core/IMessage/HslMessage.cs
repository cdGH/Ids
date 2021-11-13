using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HslCommunication.Core.IMessage
{

	/// <summary>
	/// 本组件系统使用的默认的消息规则，说明解析和反解析规则的
	/// </summary>
	public class HslMessage : INetMessage
	{
		/// <inheritdoc cref="INetMessage.ProtocolHeadBytesLength"/>
		public int ProtocolHeadBytesLength => 32;

		/// <inheritdoc cref="INetMessage.HeadBytes"/>
		public byte[] HeadBytes { get ; set; }

		/// <inheritdoc cref="INetMessage.ContentBytes"/>
		public byte[] ContentBytes { get ; set ; }

		/// <inheritdoc cref="INetMessage.CheckHeadBytesLegal(byte[])"/>
		public bool CheckHeadBytesLegal( byte[] token )
		{
			if (HeadBytes == null) return false;

			if (HeadBytes?.Length >= 32)
				return BasicFramework.SoftBasic.IsTwoBytesEquel( HeadBytes, 12, token, 0, 16 );
			else
				return false;
		}

		/// <inheritdoc cref="INetMessage.GetContentLengthByHeadBytes"/>
		public int GetContentLengthByHeadBytes( )
		{
			if (HeadBytes?.Length >= 32)
				return BitConverter.ToInt32( HeadBytes, 28 );
			else
				return 0;
		}

		/// <inheritdoc cref="INetMessage.GetHeadBytesIdentity"/>
		public int GetHeadBytesIdentity()
		{
			if (HeadBytes?.Length >= 32)
				return BitConverter.ToInt32(HeadBytes, 4);
			else
				return 0;
		}

		/// <inheritdoc cref="INetMessage.SendBytes"/>
		public byte[] SendBytes { get; set; }
	}
}