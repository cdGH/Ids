using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace HslCommunication.Core.Security
{
	/// <summary>
	/// DES加密解密的对象
	/// </summary>
	public class DesCryptography : ICryptography
	{
		/// <summary>
		/// 使用指定的密钥来实例化一个加密对象，该密钥右8位的字符和数字组成，例如 12345678
		/// </summary>
		/// <param name="key">密钥</param>
		public DesCryptography( string key )
		{
			this.key         = key;
			des              = new DESCryptoServiceProvider( );
			des.Key          = Encoding.ASCII.GetBytes( key );
			des.IV           = Encoding.ASCII.GetBytes( key );
			encryptTransform = des.CreateEncryptor( );
			decryptTransform = des.CreateDecryptor( );
		}

		/// <inheritdoc cref="ICryptography.Encrypt(byte[])"/>
		public byte[] Encrypt( byte[] data )
		{
			if (data == null) return null;
			//MemoryStream ms = new MemoryStream( );
			//CryptoStream cs = new CryptoStream( ms, encryptTransform, CryptoStreamMode.Write );
			//cs.Write( data, 0, data.Length );
			//cs.FlushFinalBlock( );
			//cs.Dispose( );
			//return ms.ToArray( );
			return encryptTransform.TransformFinalBlock( data, 0, data.Length );
		}

		/// <inheritdoc cref="ICryptography.Decrypt(byte[])"/>
		public byte[] Decrypt( byte[] data )
		{
			if (data == null) return null;
			//MemoryStream ms = new MemoryStream( );
			//CryptoStream cs = new CryptoStream( ms, decryptTransform, CryptoStreamMode.Write );
			//cs.Write( data, 0, data.Length );
			//cs.FlushFinalBlock( );
			//cs.Dispose( );
			//return ms.ToArray( );
			return decryptTransform.TransformFinalBlock( data, 0, data.Length );
		}

		/// <inheritdoc cref="ICryptography.Key"/>
		public string Key => this.key;

		private ICryptoTransform encryptTransform;    // 加密转换对象
		private ICryptoTransform decryptTransform;    // 解密转换对象
		private DESCryptoServiceProvider des;       // DES加密对象
		private string key;
	}
}
