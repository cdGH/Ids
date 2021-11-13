using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
#if !NET35 && !NET20
using System.Threading.Tasks;
#endif

namespace HslCommunication.WebSocket
{
	/// <summary>
	/// websocket的相关辅助的方法
	/// </summary>
	public class WebSocketHelper
	{
		#region Static Helper

		/// <summary>
		/// 计算websocket返回得令牌
		/// </summary>
		/// <param name="webSocketKey">请求的令牌</param>
		/// <returns>返回的令牌</returns>
		public static string CalculateWebscoketSha1( string webSocketKey )
		{
			SHA1 sha1 = new SHA1CryptoServiceProvider( );
			byte[] bytes_sha1_out = sha1.ComputeHash( Encoding.UTF8.GetBytes( webSocketKey + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11" ) );
#if !NET35 && !NET20
			sha1.Dispose( );
#endif
			return Convert.ToBase64String( bytes_sha1_out );
		}

		/// <summary>
		/// 根据http网页的信息，计算出返回的安全令牌
		/// </summary>
		/// <param name="httpGet">网页信息</param>
		/// <returns>返回的安全令牌</returns>
		public static string GetSecKeyAccetp( string httpGet )
		{
			string key = string.Empty;
			Regex r = new Regex( @"Sec\-WebSocket\-Key:(.*?)\r\n" );
			Match m = r.Match( httpGet );
			if (m.Success) key = Regex.Replace( m.Value, @"Sec\-WebSocket\-Key:(.*?)\r\n", "$1" ).Trim( );
			return CalculateWebscoketSha1( key );
		}

		/// <summary>
		/// 检测当前的反馈对象是否是标准的websocket请求
		/// </summary>
		/// <param name="httpGet">http的请求内容</param>
		/// <returns>是否验证成功</returns>
		public static OperateResult CheckWebSocketLegality( string httpGet )
		{
			if (Regex.IsMatch( httpGet, "Connection:[ ]*Upgrade", RegexOptions.IgnoreCase ) && 
				Regex.IsMatch( httpGet, "Upgrade:[ ]*websocket", RegexOptions.IgnoreCase ))
				return OperateResult.CreateSuccessResult( );
			else
				return new OperateResult( "Can't find Connection: Upgrade or Upgrade: websocket" );
		}

		/// <summary>
		/// 检测当前的反馈对象是否是标准的websocket请求
		/// </summary>
		/// <param name="httpGet">http的请求内容</param>
		/// <returns>是否验证成功</returns>
		public static string[] GetWebSocketSubscribes( string httpGet )
		{
			Regex r = new Regex( @"HslSubscribes:[^\r\n]+" );
			Match m = r.Match( httpGet );
			if (!m.Success) return null;

			return m.Value.Substring( 14 ).Replace(" ", "").Split( new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries );
		}

		/// <summary>
		/// 获取初步握手的时候的完整返回的数据信息
		/// </summary>
		/// <param name="httpGet">请求的网页信息</param>
		/// <returns>完整的返回信息</returns>
		public static OperateResult<byte[]> GetResponse( string httpGet )
		{
			try
			{
				var sb = new StringBuilder( );
				sb.Append( "HTTP/1.1 101 Switching Protocols" + Environment.NewLine );
				sb.Append( "Connection: Upgrade" + Environment.NewLine );
				sb.Append( "Upgrade: websocket" + Environment.NewLine );
				sb.Append( "Server:hsl websocket server" + Environment.NewLine );
				sb.Append( "Access-Control-Allow-Credentials:true" + Environment.NewLine );
				sb.Append( "Access-Control-Allow-Headers:content-type" + Environment.NewLine );
				sb.Append( "Sec-WebSocket-Accept: " + GetSecKeyAccetp( httpGet ) + Environment.NewLine + Environment.NewLine );

				return OperateResult.CreateSuccessResult( Encoding.UTF8.GetBytes( sb.ToString( ) ) );
			}
			catch (Exception ex)
			{
				return new OperateResult<byte[]>( ex.Message );
			}
		}

		/// <summary>
		/// 创建连接服务器的http请求，输入订阅的主题信息
		/// </summary>
		/// <param name="ipAddress">远程服务器的ip地址</param>
		/// <param name="port">远程服务器的端口号</param>
		/// <param name="url">参数信息</param>
		/// <param name="subscribes">通知hsl的服务器，需要订阅的topic信息</param>
		/// <returns>报文信息</returns>
		public static byte[] BuildWsSubRequest( string ipAddress, int port, string url, string[] subscribes )
		{
			StringBuilder sb = new StringBuilder( );
			if (subscribes != null)
			{
				sb.Append( "HslSubscribes: " );
				for (int i = 0; i < subscribes.Length; i++)
				{
					sb.Append( subscribes[i] );
					if (i != (subscribes.Length - 1)) sb.Append( "," );
				}
			}
			return BuildWsRequest( ipAddress, port, url, sb.ToString( ) );
		}

		/// <summary>
		/// 创建连接服务器的http请求，采用问答的机制
		/// </summary>
		/// <param name="ipAddress">远程服务器的ip地址</param>
		/// <param name="port">远程服务器的端口号</param>
		/// <returns>报文信息</returns>
		public static byte[] BuildWsQARequest( string ipAddress, int port )
		{
			return BuildWsRequest( ipAddress, port, string.Empty, "HslRequestAndAnswer: true" );
		}

		/// <summary>
		/// 根据额外的参数信息，创建新的websocket的请求信息
		/// </summary>
		/// <param name="ipAddress">ip地址</param>
		/// <param name="port">端口号</param>
		/// <param name="url">跟在端口号后面的额外的参数信息</param>
		/// <param name="extra">额外的参数信息</param>
		/// <returns>报文信息</returns>
		public static byte[] BuildWsRequest( string ipAddress, int port, string url, string extra )
		{
			if (!url.StartsWith( "/" )) url = "/" + url;

			StringBuilder sb = new StringBuilder( );
			sb.Append( $"GET ws://{ipAddress}:{port}{url} HTTP/1.1" );                                                       sb.Append( Environment.NewLine );
			sb.Append( $"Host: {ipAddress}:{port}" );                                                 sb.Append( Environment.NewLine );
			sb.Append( "Connection: Upgrade" );                                                       sb.Append( Environment.NewLine );
			sb.Append( "Pragma: no-cache" );                                                          sb.Append( Environment.NewLine );
			sb.Append( "Cache-Control: no-cache" );                                                   sb.Append( Environment.NewLine );
			sb.Append( "Upgrade: websocket" );                                                        sb.Append( Environment.NewLine );
			sb.Append( $"Origin: http://{ipAddress}:{port}" );                                        sb.Append( Environment.NewLine );
			sb.Append( "Sec-WebSocket-Version: 13" );                                                 sb.Append( Environment.NewLine );
			sb.Append( "User-Agent: Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/65.0.3314.0 Safari/537.36 SE 2.X MetaSr 1.0" ); sb.Append( Environment.NewLine );
			sb.Append( "Accept-Encoding: gzip, deflate, br" );                                        sb.Append( Environment.NewLine );
			sb.Append( "Accept-Language: zh-CN,zh;q=0.9" );                                           sb.Append( Environment.NewLine );
			sb.Append( "Sec-WebSocket-Key: ia36apzXapB4YVxRfVyTuw==" );                               sb.Append( Environment.NewLine );
			sb.Append( "Sec-WebSocket-Extensions: permessage-deflate; client_max_window_bits" );      sb.Append( Environment.NewLine );
			if (!string.IsNullOrEmpty( extra ))
			{
				sb.Append( extra );
				sb.Append( Environment.NewLine );
			}
			sb.Append( Environment.NewLine );
			return Encoding.UTF8.GetBytes( sb.ToString( ) );
		}

		/// <summary>
		/// 将普通的文本信息转换成websocket的报文
		/// </summary>
		/// <param name="opCode">操作信息码</param>
		/// <param name="isMask">是否使用掩码</param>
		/// <param name="message">等待转换的数据信息</param>
		/// <returns>数据包</returns>
		public static byte[] WebScoketPackData( int opCode, bool isMask, string message )
		{
			return WebScoketPackData( opCode, isMask, string.IsNullOrEmpty( message ) ? new byte[0] : Encoding.UTF8.GetBytes( message ) );
		}

		/// <summary>
		/// 将普通的文本信息转换成websocket的报文
		/// </summary>
		/// <param name="opCode">操作信息码</param>
		/// <param name="isMask">是否使用掩码</param>
		/// <param name="payload">等待转换的数据信息</param>
		/// <returns>数据包</returns>
		public static byte[] WebScoketPackData( int opCode, bool isMask, byte[] payload )
		{
			if (payload == null) payload = new byte[0];
			byte[] data = payload.CopyArray( );

			MemoryStream ms = new MemoryStream( );
			byte[] mask = new byte[4] { 0x9B, 0x03, 0xA1, 0xA8 };
			if (isMask)
			{
				Random random = new Random( );
				random.NextBytes( mask );
				for (int i = 0; i < data.Length; i++)
				{
					data[i] = (byte)(data[i] ^ mask[i % 4]);
				}
			}

			ms.WriteByte( (byte)(0x80 | opCode) );
			if (data.Length < 126)
				ms.WriteByte( (byte)(data.Length + (isMask ? 0x80 : 0x00)) );
			else if (data.Length <= 0xFFFF)
			{
				ms.WriteByte( (byte)(126 + (isMask ? 0x80 : 0x00)) );
				byte[] length = BitConverter.GetBytes( (ushort)data.Length );
				Array.Reverse( length );
				ms.Write( length, 0, length.Length );
			}
			else
			{
				ms.WriteByte( (byte)(127 + (isMask ? 0x80 : 0x00)) );
				byte[] length = BitConverter.GetBytes( (ulong)data.Length );
				Array.Reverse( length );
				ms.Write( length, 0, length.Length );
			}

			if (isMask) ms.Write( mask, 0, mask.Length );
			ms.Write( data, 0, data.Length );
			byte[] buffer = ms.ToArray( );
			ms.Dispose( );
			return buffer;
		}
		#endregion
	}
}
