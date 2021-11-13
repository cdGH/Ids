using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace HslCommunication.Core.Security
{
	/// <summary>
	/// 实例化一个AES加密解密的对象，默认 <see cref="CipherMode.ECB"/> 模式的对象
	/// </summary>
	public class AesCryptography : ICryptography
	{
		/// <summary>
		/// 使用指定的密钥实例化一个AES加密解密的对象，密钥由32位数字或字母组成，例如 12345678123456781234567812345678
		/// </summary>
		/// <param name="key">密钥</param>
		/// <param name="mode">加密的模式，默认为 <see cref="CipherMode.ECB"/></param>
		public AesCryptography( string key, CipherMode mode = CipherMode.ECB )
		{
			this.key = key;
			rijndael = new RijndaelManaged
			{
				Key     = Encoding.UTF8.GetBytes( key ),
				Mode    = mode,
				Padding = PaddingMode.PKCS7
			};
			encryptTransform = rijndael.CreateEncryptor( );
			decryptTransform = rijndael.CreateDecryptor( );
		}

		/// <inheritdoc cref="ICryptography.Encrypt(byte[])"/>
		public byte[] Encrypt( byte[] data )
		{
			if (data == null) return null;
			return encryptTransform.TransformFinalBlock( data, 0, data.Length );
		}

		/// <inheritdoc cref="ICryptography.Decrypt(byte[])"/>
		public byte[] Decrypt( byte[] data )
		{
			if (data == null) return null;
			return decryptTransform.TransformFinalBlock( data, 0, data.Length );
		}

		/// <inheritdoc cref="ICryptography.Key"/>
		public string Key => this.key;

		private ICryptoTransform encryptTransform;    // 加密转换对象
		private ICryptoTransform decryptTransform;    // 解密转换对象
		private RijndaelManaged rijndael;             // AES加密的对象
		private string key;
	}
}
