using HslCommunication.BasicFramework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace HslCommunication.Core.Security
{
	/**************************************************************************************************
	 * 
	 *   部分代码参考博客：https://www.cnblogs.com/dudu/p/csharp-openssl-encrypt-decrypt.html
	 * 
	 * 
	 *****************************************************************************************************/

	/// <summary>
	/// RSA加密解密算法的辅助方法，可以用PEM格式的密钥创建公钥，或是私钥对象，然后用来加解密操作。
	/// </summary>
	public class RSAHelper
	{
		private const string privateKeyHead = "-----BEGIN RSA PRIVATE KEY-----";
		private const string privateKeyEnd = "-----END RSA PRIVATE KEY-----";
		private const string publicKeyHead = "-----BEGIN PUBLIC KEY-----";
		private const string publicKeyEnd = "-----END PUBLIC KEY-----";
		private static readonly byte[] SeqOID = new byte[] { 0x30, 0x0D, 0x06, 0x09, 0x2A, 0x86, 0x48, 0x86, 0xF7, 0x0D, 0x01, 0x01, 0x01, 0x05, 0x00 };


		/// <summary>
		/// 使用 PEM 格式基于base64编码的私钥来创建一个 RSA 算法加密解密的对象，可以直接用于加密解密操作<br />
		/// Use the PEM format based on the base64-encoded private key to create an RSA algorithm encryption and decryption object, 
		/// which can be directly used for encryption and decryption operations
		/// </summary>
		/// <param name="privateKeyString">私钥</param>
		/// <returns>RSA 算法的加密解密对象</returns>
		public static RSACryptoServiceProvider CreateRsaProviderFromPrivateKey( string privateKeyString )
		{
			privateKeyString = privateKeyString.Trim( );
			if (privateKeyString.StartsWith( privateKeyHead )) privateKeyString = privateKeyString.Replace( privateKeyHead, string.Empty );
			if (privateKeyString.EndsWith( privateKeyEnd )) privateKeyString = privateKeyString.Replace( privateKeyEnd, string.Empty );

			var privateKeyBits = Convert.FromBase64String( privateKeyString );
			return CreateRsaProviderFromPrivateKey( privateKeyBits );
		}

		/// <summary>
		/// 使用原始的私钥数据（PEM格式）来创建一个 RSA 算法加密解密的对象，可以直接用于加密解密操作<br />
		/// Use the original private key data (PEM format) to create an RSA algorithm encryption and decryption object, 
		/// which can be directly used for encryption and decryption operations
		/// </summary>
		/// <param name="privateKey">原始的私钥数据</param>
		/// <returns>RSA 算法的加密解密对象</returns>
		public static RSACryptoServiceProvider CreateRsaProviderFromPrivateKey( byte[] privateKey )
		{
			var RSA = new RSACryptoServiceProvider( );
			var RSAparams = new RSAParameters( );

			using (BinaryReader binr = new BinaryReader( new MemoryStream( privateKey ) ))
			{
				byte bt = 0;
				ushort twobytes = 0;
				twobytes = binr.ReadUInt16( );
				if (twobytes == 0x8130)
					binr.ReadByte( );
				else if (twobytes == 0x8230)
					binr.ReadInt16( );
				else
					throw new Exception( "Unexpected value read binr.ReadUInt16()" );

				twobytes = binr.ReadUInt16( );
				if (twobytes != 0x0102)
					throw new Exception( "Unexpected version" );

				bt = binr.ReadByte( );
				if (bt != 0x00)
					throw new Exception( "Unexpected value read binr.ReadByte()" );

				RSAparams.Modulus = binr.ReadBytes( GetIntegerSize( binr ) );
				RSAparams.Exponent = binr.ReadBytes( GetIntegerSize( binr ) );
				RSAparams.D = binr.ReadBytes( GetIntegerSize( binr ) );
				RSAparams.P = binr.ReadBytes( GetIntegerSize( binr ) );
				RSAparams.Q = binr.ReadBytes( GetIntegerSize( binr ) );
				RSAparams.DP = binr.ReadBytes( GetIntegerSize( binr ) );
				RSAparams.DQ = binr.ReadBytes( GetIntegerSize( binr ) );
				RSAparams.InverseQ = binr.ReadBytes( GetIntegerSize( binr ) );
			}

			RSA.ImportParameters( RSAparams );
			return RSA;
		}

		private static int GetIntegerSize( BinaryReader binr )
		{
			byte bt = 0;
			byte lowbyte = 0x00;
			byte highbyte = 0x00;
			int count = 0;
			bt = binr.ReadByte( );
			if (bt != 0x02) return 0;

			bt = binr.ReadByte( );
			if (bt == 0x81) count = binr.ReadByte( );
			else if (bt == 0x82)
			{
				highbyte = binr.ReadByte( );
				lowbyte = binr.ReadByte( );
				byte[] modint = { lowbyte, highbyte, 0x00, 0x00 };
				count = BitConverter.ToInt32( modint, 0 );
			}
			else
			{
				count = bt;
			}

			while (binr.ReadByte( ) == 0x00)
			{
				count -= 1;
			}
			binr.BaseStream.Seek( -1, SeekOrigin.Current );
			return count;
		}


		private static byte[] PackKeyHead(byte[] content )
		{
			if (content.Length < 256)
			{
				byte[] buffer = SoftBasic.SpliceArray( "30 81 00".ToHexBytes( ), content );
				buffer[2] = BitConverter.GetBytes( buffer.Length - 3 )[0];
				return buffer;
			}
			else
			{
				byte[] buffer = SoftBasic.SpliceArray( "30 82 00 00".ToHexBytes( ), content );
				buffer[2] = BitConverter.GetBytes( buffer.Length - 4 )[1];
				buffer[3] = BitConverter.GetBytes( buffer.Length - 4 )[0];
				return buffer;
			}
		}

		/// <summary>
		/// 从RSA的算法对象里，获取到PEM格式的原始私钥数据，如果需要存储，或是显示，只需要 Convert.ToBase64String 方法<br />
		/// Obtain the original private key data in PEM format from the RSA algorithm object. If you need to store or display it, 
		/// you only need the Convert.ToBase64String method
		/// </summary>
		/// <param name="rsa">RSA 算法的加密解密对象</param>
		/// <returns>原始的私钥数据</returns>
		public static byte[] GetPrivateKeyFromRSA( RSACryptoServiceProvider rsa )
		{
			RSAParameters parameters = rsa.ExportParameters( true );
			byte[] modulus  = parameters.Modulus;
			byte[] exponent = parameters.Exponent;
			byte[] d        = parameters.D;
			byte[] p        = parameters.P;
			byte[] q        = parameters.Q;
			byte[] dp       = parameters.DP;
			byte[] dq       = parameters.DQ;
			byte[] inverseQ = parameters.InverseQ;

			MemoryStream ms = new MemoryStream( );
			ms.Write( new byte[] { 0x02, 0x01, 0x00 }, 0, 3 );
			WriteByteStream( ms, modulus );
			WriteByteStream( ms, exponent );
			WriteByteStream( ms, d );
			WriteByteStream( ms, p );
			WriteByteStream( ms, q );
			WriteByteStream( ms, dp );
			WriteByteStream( ms, dq );
			WriteByteStream( ms, inverseQ );
			return PackKeyHead( ms.ToArray( ) );
		}

		private static void WriteByteStream( MemoryStream ms, byte[] data )
		{
			bool addZero = data[0] > 0x7F;
			int length = addZero ? data.Length + 1 : data.Length;
			ms.WriteByte( 0x02 );
			if (length < 0x80)
			{
				ms.WriteByte( (byte)length );
			}
			else if (length < 0x100)
			{
				ms.WriteByte( 0x81 );
				ms.WriteByte( (byte)length );
			}
			else
			{
				ms.WriteByte( 0x82 );
				ms.WriteByte( BitConverter.GetBytes( length )[1] );
				ms.WriteByte( BitConverter.GetBytes( length )[0] );
			}

			if (addZero) ms.WriteByte( 0x00 );
			ms.Write( data, 0, data.Length );
		}

		/// <summary>
		/// 从RSA的算法对象里，获取到PEM格式的原始公钥数据，如果需要存储，或是显示，只需要 Convert.ToBase64String 方法<br />
		/// Obtain the original public key data in PEM format from the RSA algorithm object. If you need to store or display it, 
		/// you only need the Convert.ToBase64String method
		/// </summary>
		/// <param name="rsa">RSA 算法的加密解密对象</param>
		/// <returns>原始的公钥数据</returns>
		public static byte[] GetPublicKeyFromRSA( RSACryptoServiceProvider rsa )
		{
			RSAParameters parameters = rsa.ExportParameters( false );
			byte[] modulus  = parameters.Modulus;
			byte[] exponent = parameters.Exponent;

			MemoryStream ms = new MemoryStream( );
			WriteByteStream( ms, modulus );
			WriteByteStream( ms, exponent );

			byte[] content = PackKeyHead( SoftBasic.SpliceArray( new byte[] { 0x00 }, PackKeyHead( ms.ToArray( ) ) ) );
			content[0] = 0x03;

			return PackKeyHead( SoftBasic.SpliceArray( SeqOID, content ) );
		}

		/// <summary>
		/// PEM 格式基于base64编码的公钥来创建一个 RSA 算法加密解密的对象，可以直接用于加密或是验证签名操作<br />
		/// Use the original public key data (PEM format) to create an RSA algorithm encryption and decryption object, 
		/// which can be directly used for encryption or signature verification
		/// </summary>
		/// <param name="publicKeyString">公钥</param>
		/// <returns>RSA 算法的加密解密对象</returns>
		public static RSACryptoServiceProvider CreateRsaProviderFromPublicKey( string publicKeyString )
		{
			publicKeyString = publicKeyString.Trim( );
			if (publicKeyString.StartsWith( publicKeyHead )) publicKeyString = publicKeyString.Replace( publicKeyHead, string.Empty );
			if (publicKeyString.EndsWith( publicKeyEnd )) publicKeyString = publicKeyString.Replace( publicKeyEnd, string.Empty );

			return CreateRsaProviderFromPublicKey( Convert.FromBase64String( publicKeyString ) );
		}

		/// <summary>
		/// 对原始字节的数据进行加密，不限制长度，因为RSA本身限制了117字节，所以此处进行数据切割加密。<br />
		/// Encrypt the original byte data without limiting the length, because RSA itself limits 117 bytes, so the data is cut and encrypted here.
		/// </summary>
		/// <param name="provider">RSA公钥对象</param>
		/// <param name="data">等待加密的原始数据</param>
		/// <returns>加密之后的结果信息</returns>
		public static byte[] EncryptLargeDataByRSA( RSACryptoServiceProvider provider, byte[] data )
		{
			MemoryStream ms = new MemoryStream( );
			List<byte[]> splits = SoftBasic.ArraySplitByLength( data, 110 );
			for (int i = 0; i < splits.Count; i++)
			{
				byte[] encrypt = provider.Encrypt( splits[i], false );
				ms.Write( encrypt, 0, encrypt.Length );
			}
			return ms.ToArray( );
		}

		/// <summary>
		/// 对超过117字节限制的加密数据进行加密，因为RSA本身限制了117字节，所以此处进行数据切割解密。<br />
		/// </summary>
		/// <param name="provider">RSA私钥对象</param>
		/// <param name="data">等待解密的数据</param>
		/// <returns>解密之后的结果数据</returns>
		public static byte[] DecryptLargeDataByRSA( RSACryptoServiceProvider provider, byte[] data )
		{
			MemoryStream ms = new MemoryStream( );
			List<byte[]> splits = SoftBasic.ArraySplitByLength( data, 128 );
			for (int i = 0; i < splits.Count; i++)
			{
				byte[] decrypt = provider.Decrypt( splits[i], false );
				ms.Write( decrypt, 0, decrypt.Length );
			}
			return ms.ToArray( );
		}

		/// <summary>
		/// 使用原始的公钥数据（PEM格式）来创建一个 RSA 算法加密解密的对象，可以直接用于加密或是验证签名操作<br />
		/// Use the original public key data (PEM format) to create an RSA algorithm encryption and decryption object, 
		/// which can be directly used for encryption or signature verification
		/// </summary>
		/// <param name="publicKey">公钥</param>
		/// <returns>RSA 算法的加密解密对象</returns>
		public static RSACryptoServiceProvider CreateRsaProviderFromPublicKey( byte[] publicKey )
		{
			// encoded OID sequence for  PKCS #1 rsaEncryption szOID_RSA_RSA = "1.2.840.113549.1.1.1"
			byte[] x509key;
			byte[] seq = new byte[15];
			int x509size;

			x509key = publicKey;
			x509size = x509key.Length;

			// ---------  Set up stream to read the asn.1 encoded SubjectPublicKeyInfo blob  ------
			using (MemoryStream mem = new MemoryStream( x509key ))
			{
				using (BinaryReader binr = new BinaryReader( mem ))  //wrap Memory Stream with BinaryReader for easy reading
				{
					byte bt = 0;
					ushort twobytes = 0;

					twobytes = binr.ReadUInt16( );
					if (twobytes == 0x8130) //data read as little endian order (actual data order for Sequence is 30 81)
						binr.ReadByte( );    //advance 1 byte
					else if (twobytes == 0x8230)
						binr.ReadInt16( );   //advance 2 bytes
					else
						return null;

					seq = binr.ReadBytes( 15 );       //read the Sequence OID
					if (!CompareBytearrays( seq, SeqOID ))    //make sure Sequence for OID is correct
						return null;

					twobytes = binr.ReadUInt16( );
					if (twobytes == 0x8103) //data read as little endian order (actual data order for Bit String is 03 81)
						binr.ReadByte( );    //advance 1 byte
					else if (twobytes == 0x8203)
						binr.ReadInt16( );   //advance 2 bytes
					else
						return null;

					bt = binr.ReadByte( );
					if (bt != 0x00)     //expect null byte next
						return null;

					twobytes = binr.ReadUInt16( );
					if (twobytes == 0x8130) //data read as little endian order (actual data order for Sequence is 30 81)
						binr.ReadByte( );    //advance 1 byte
					else if (twobytes == 0x8230)
						binr.ReadInt16( );   //advance 2 bytes
					else
						return null;

					twobytes = binr.ReadUInt16( );
					byte lowbyte = 0x00;
					byte highbyte = 0x00;

					if (twobytes == 0x8102) //data read as little endian order (actual data order for Integer is 02 81)
						lowbyte = binr.ReadByte( );  // read next bytes which is bytes in modulus
					else if (twobytes == 0x8202)
					{
						highbyte = binr.ReadByte( ); //advance 2 bytes
						lowbyte = binr.ReadByte( );
					}
					else
						return null;
					byte[] modint = { lowbyte, highbyte, 0x00, 0x00 };   //reverse byte order since asn.1 key uses big endian order
					int modsize = BitConverter.ToInt32( modint, 0 );

					int firstbyte = binr.PeekChar( );
					if (firstbyte == 0x00)
					{   //if first byte (highest order) of modulus is zero, don't include it
						binr.ReadByte( );    //skip this null byte
						modsize -= 1;   //reduce modulus buffer size by 1
					}

					byte[] modulus = binr.ReadBytes( modsize );   //read the modulus bytes

					if (binr.ReadByte( ) != 0x02)            //expect an Integer for the exponent data
						return null;
					int expbytes = (int)binr.ReadByte( );        // should only need one byte for actual exponent data (for all useful values)
					byte[] exponent = binr.ReadBytes( expbytes );

					// ------- create RSACryptoServiceProvider instance and initialize with public key -----
					RSACryptoServiceProvider RSA = new RSACryptoServiceProvider( );
					RSAParameters RSAKeyInfo = new RSAParameters( );
					RSAKeyInfo.Modulus = modulus;
					RSAKeyInfo.Exponent = exponent;
					RSA.ImportParameters( RSAKeyInfo );

					return RSA;
				}

			}
		}

		private static bool CompareBytearrays( byte[] a, byte[] b )
		{
			if (a.Length != b.Length)
				return false;
			int i = 0;
			foreach (byte c in a)
			{
				if (c != b[i])
					return false;
				i++;
			}
			return true;
		}

	}
}
