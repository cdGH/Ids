using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Net;
using HslCommunication.LogNet;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.IO;
#if !NET35 && !NET20
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
#endif

namespace HslCommunication.Core.Net
{
	/// <summary>
	/// 基于webapi的数据访问的基类，提供了基本的http接口的交互功能<br />
	/// A base class for data access based on webapi that provides basic HTTP interface interaction
	/// </summary>
	/// <remarks>
	/// 当前的基类在.net framework2.0上存在问题，在.net framework4.5及.net standard上运行稳定而且正常
	/// </remarks>
	public class NetworkWebApiBase
	{
		#region Constrcutor

		/// <summary>
		/// 使用指定的ip地址来初始化对象<br />
		/// Initializes the object using the specified IP address
		/// </summary>
		/// <param name="ipAddress">Ip地址信息</param>
		public NetworkWebApiBase( string ipAddress ) : this( ipAddress, 80, string.Empty, string.Empty )
		{

		}

		/// <summary>
		/// 使用指定的ip地址及端口号来初始化对象<br />
		/// Initializes the object with the specified IP address and port number
		/// </summary>
		/// <param name="ipAddress">Ip地址信息</param>
		/// <param name="port">端口号信息</param>
		public NetworkWebApiBase( string ipAddress, int port ) : this( ipAddress, port, string.Empty, string.Empty )
		{

		}

		/// <summary>
		/// 使用指定的ip地址，端口号，用户名，密码来初始化对象<br />
		/// Initialize the object with the specified IP address, port number, username, and password
		/// </summary>
		/// <param name="ipAddress">Ip地址信息</param>
		/// <param name="port">端口号信息</param>
		/// <param name="name">用户名</param>
		/// <param name="password">密码</param>
		public NetworkWebApiBase( string ipAddress, int port, string name, string password )
		{
			this.ipAddress = HslHelper.GetIpAddressFromInput( ipAddress );
			this.port = port;
			this.name = name;
			this.password = password;
#if !NET35 && !NET20
			if (!string.IsNullOrEmpty( name ) )
			{
				var handler = new HttpClientHandler { Credentials = new NetworkCredential( name, password ) };
				handler.Proxy = null;
				handler.UseProxy = false;
				ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback( TrustAllValidationCallback );
				this.httpClient = new HttpClient( handler );
				// this.httpClient.DefaultRequestHeaders.ConnectionClose = true;
				ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
			}
			else
			{
				ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback( TrustAllValidationCallback );
				this.httpClient = new HttpClient( );
				// this.httpClient.DefaultRequestHeaders.ConnectionClose = true;
				ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
			}
#endif
		}
		private bool TrustAllValidationCallback( object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors )
		{
			return true; // 忽略SSL证书检查
		}
		#endregion

		#region Protect Method

#if !NET35 && !NET20
		/// <summary>
		/// 针对请求的头信息进行额外的处理
		/// </summary>
		/// <param name="headers">头信息</param>
		protected virtual void AddRequestHeaders( HttpContentHeaders headers ) { }
#endif

		#endregion

		#region GET POST Support

		/// <summary>
		/// 使用GET操作从网络中获取到数据信息
		/// </summary>
		/// <param name="rawUrl">除去ip地址和端口的地址</param>
		/// <returns>返回的数据内容</returns>
		public OperateResult<string> Get(string rawUrl )
		{
			string url = $"{(UseHttps ? "https" : "http")}://{ipAddress}:{port}/{ (rawUrl.StartsWith( "/" ) ? rawUrl.Substring( 1 ) : rawUrl) }";

			try
			{
#if !NET35 && !NET20
				using (HttpResponseMessage response = httpClient.GetAsync( url ).Result)
				using (HttpContent content = response.Content)
				{
					response.EnsureSuccessStatusCode( );
					if (UseEncodingISO)
					{
						using (StreamReader sr = new StreamReader( content.ReadAsStreamAsync( ).Result, Encoding.GetEncoding( "iso-8859-1" ) ))
						{
							return OperateResult.CreateSuccessResult( sr.ReadToEnd( ) );
						}
					}
					else
					{
						return OperateResult.CreateSuccessResult( content.ReadAsStringAsync( ).Result );
					}
				}
#else
					WebClient webClient = new WebClient( );
					if (!string.IsNullOrEmpty( name ))
						webClient.Credentials = new NetworkCredential( name, password );

					if (!string.IsNullOrEmpty( DefaultContentType ))
						webClient.Headers.Add( "Content-Type", DefaultContentType );
					byte[] content = webClient.DownloadData( url );
					webClient.Dispose( );
					return OperateResult.CreateSuccessResult( Encoding.UTF8.GetString( content ) );
#endif
			}
			catch (Exception ex)
			{
				return new OperateResult<string>( ex.Message );
			}
		}


		/// <summary>
		/// 使用POST命令去提交数据内容，然后返回相关的数据信息
		/// </summary>
		/// <param name="rawUrl">已经去除ip地址，端口号的api信息</param>
		/// <param name="body">数据内容</param>
		/// <returns>从服务器返回的内容</returns>
		public OperateResult<string> Post( string rawUrl, string body )
		{
			string url = $"{(UseHttps ? "https" : "http")}://{ipAddress}:{port}/{ (rawUrl.StartsWith( "/" ) ? rawUrl.Substring( 1 ) : rawUrl) }";

			try
			{
#if !NET35 && !NET20
				using (StringContent stringContent = new StringContent( body ))
				{
					if (!string.IsNullOrEmpty( DefaultContentType ))
						stringContent.Headers.ContentType = new MediaTypeHeaderValue( DefaultContentType );
					AddRequestHeaders( stringContent.Headers );
					using (HttpResponseMessage response = httpClient.PostAsync( url, stringContent ).Result)
					using (HttpContent content = response.Content)
					{
						response.EnsureSuccessStatusCode( );
						if (UseEncodingISO)
						{
							using (StreamReader sr = new StreamReader( content.ReadAsStreamAsync( ).Result, Encoding.GetEncoding( "iso-8859-1" ) ))
							{
								return OperateResult.CreateSuccessResult( sr.ReadToEnd( ) );
							}
						}
						else
						{
							return OperateResult.CreateSuccessResult( content.ReadAsStringAsync( ).Result );
						}
					}
				}
#else
					WebClient webClient = new WebClient( );
					webClient.Proxy = null;
					if (!string.IsNullOrEmpty( DefaultContentType ))
						webClient.Headers.Add( "Content-Type", DefaultContentType );
					if (!string.IsNullOrEmpty( name ))
						webClient.Credentials = new NetworkCredential( name, password );

					byte[] content = webClient.UploadData( url, Encoding.UTF8.GetBytes( body ) );
					webClient.Dispose( );
					return OperateResult.CreateSuccessResult( Encoding.UTF8.GetString( content ) );
#endif
			}
			catch (Exception ex)
			{
				return new OperateResult<string>( ex.Message );
			}
		}

		#endregion

		#region Async Read Write Support
#if !NET35 && !NET20

		/// <inheritdoc cref="Get(string)"/>
		public async Task<OperateResult<string>> GetAsync( string rawUrl )
		{
			string url = $"{(UseHttps ? "https" : "http")}://{ipAddress}:{port}/{ (rawUrl.StartsWith( "/" ) ? rawUrl.Substring( 1 ) : rawUrl) }";

			try
			{
				using (HttpResponseMessage response = await httpClient.GetAsync( url ))
				using (HttpContent content = response.Content)
				{
					response.EnsureSuccessStatusCode( );
					if (UseEncodingISO)
					{
						using (StreamReader sr = new StreamReader( await content.ReadAsStreamAsync( ), Encoding.GetEncoding( "iso-8859-1" ) ))
						{
							return OperateResult.CreateSuccessResult( sr.ReadToEnd( ) );
						}
					}
					else
					{
						return OperateResult.CreateSuccessResult( await content.ReadAsStringAsync( ) );
					}
				}
			}
			catch (Exception ex)
			{
				return new OperateResult<string>( ex.Message );
			}
		}

		/// <inheritdoc cref="Post(string, string)"/>
		public async Task<OperateResult<string>> PostAsync( string rawUrl, string body )
		{
			string url = $"{(UseHttps ? "https" : "http")}://{ipAddress}:{port}/{ (rawUrl.StartsWith( "/" ) ? rawUrl.Substring( 1 ) : rawUrl) }";

			try
			{
				using (StringContent stringContent = new StringContent( body ))
				{
					if (!string.IsNullOrEmpty( DefaultContentType ))
						stringContent.Headers.ContentType = new MediaTypeHeaderValue( DefaultContentType );
					AddRequestHeaders( stringContent.Headers );
					using (HttpResponseMessage response = await httpClient.PostAsync( url, stringContent ))
					using (HttpContent content = response.Content)
					{
						response.EnsureSuccessStatusCode( );
						if (UseEncodingISO)
						{
							using(StreamReader sr = new StreamReader(await content.ReadAsStreamAsync(), Encoding.GetEncoding( "iso-8859-1" ) ))
							{
								return OperateResult.CreateSuccessResult( sr.ReadToEnd( ) );
							}
						}
						else
						{
							return OperateResult.CreateSuccessResult( await content.ReadAsStringAsync( ) );
						}
					}
				}
			}
			catch (Exception ex)
			{
				return new OperateResult<string>( ex.Message );
			}
		}
#endif
		#endregion

		#region Public Properties

		/// <summary>
		/// 获取或设置远程服务器的IP地址<br />
		/// Gets or sets the IP address of the remote server
		/// </summary>
		public string IpAddress
		{
			get => ipAddress;
			set => ipAddress = value;
		}

		/// <summary>
		/// 获取或设置远程服务器的端口号信息<br />
		/// Gets or sets the port number information for the remote server
		/// </summary>
		public int Port
		{
			get => port;
			set => port = value;
		}

		/// <summary>
		/// 获取或设置当前的用户名<br />
		/// Get or set the current username
		/// </summary>
		public string UserName
		{
			get => this.name;
			set => this.name = value;
		}

		/// <summary>
		/// 获取或设置当前的密码<br />
		/// Get or set the current password
		/// </summary>
		public string Password
		{
			get => this.password;
			set => this.password = value;
		}

		/// <inheritdoc cref="NetworkBase.LogNet"/>
		public ILogNet LogNet { get; set; }

		/// <summary>
		/// 是否启用Https的协议访问，对于Https来说，端口号默认为 443<br />
		/// Whether to enable Https protocol access, for Https, the port number defaults to 443
		/// </summary>
		public bool UseHttps { get; set; }

		/// <summary>
		/// 默认的内容类型，如果为空，则不进行设置操作。例如设置为 "text/plain", "application/json", "text/html" 等等。<br />
		/// The default content type, if it is empty, no setting operation will be performed. For example, set to "text/plain", "application/json", "text/html" and so on.
		/// </summary>
		public string DefaultContentType { get; set; }

		/// <summary>
		/// 获取或设置是否使用ISO的编码信息，默认为 False<br />
		/// Get or set whether to use ISO encoding information, the default is False
		/// </summary>
		/// <remarks>
		/// 在访问某些特殊的API的时候，会发生异常"The character set provided in ContentType is invalid...."，这时候，只需要将本属性设置为 True 即可。
		/// </remarks>
		public bool UseEncodingISO { get; set; } = false;

#if !NET20 && !NET35
		/// <summary>
		/// 获取当前的HttpClinet的客户端<br />
		/// Get the current HttpClinet client
		/// </summary>
		public HttpClient Client => httpClient;
#endif

		#endregion

		#region Private Member

		private string ipAddress = "127.0.0.1";
		private int port = 80;
		private string name = string.Empty;
		private string password = string.Empty;

#if !NET35 && !NET20
		private HttpClient httpClient;
#endif
		#endregion

		#region Object Override

		/// <inheritdoc/>
		public override string ToString( ) => $"NetworkWebApiBase[{ipAddress}:{port}]";

		#endregion
	}
}
