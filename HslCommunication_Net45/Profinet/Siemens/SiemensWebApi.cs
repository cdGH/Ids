using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HslCommunication;
using HslCommunication.Core.Net;
using HslCommunication.Core;
using Newtonsoft.Json.Linq;
using HslCommunication.BasicFramework;
using System.Net.Http.Headers;
using HslCommunication.Reflection;
#if !NET20 && !NET35
using System.Threading.Tasks;
#endif

namespace HslCommunication.Profinet.Siemens
{
	/// <summary>
	/// 西门子的基于WebApi协议读写数据对象，支持对PLC的标签进行读取，适用于1500系列，该数据标签需要共享开放出来。<br />
	/// Siemens reads and writes data objects based on the WebApi protocol, supports reading PLC tags, 
	/// and is suitable for the 1500 series. The data tags need to be shared and opened.
	/// </summary>
	public class SiemensWebApi : NetworkWebApiDevice
	{
		#region Constructor

		/// <summary>
		/// 实例化一个默认的西门子WebApi通信对象<br />
		/// Instantiate a default Siemens WebApi communication object
		/// </summary>
		public SiemensWebApi( ) : this( "127.0.0.1", 443 )
		{

		}

		/// <summary>
		/// 使用指定的ip地址及端口号来实例化一个对象，端口号默认使用443，如果是http访问，使用80端口号<br />
		/// Use the specified ip address and port number to instantiate an object, the port number is 443 by default, if it is http access, port 80 is used
		/// </summary>
		/// <param name="ipAddress">ip地址信息</param>
		/// <param name="port">端口号信息</param>
		public SiemensWebApi( string ipAddress, int port = 443 ) : base( ipAddress, port )
		{
			WordLength = 2;
			UseHttps = true;
			DefaultContentType = "application/json";
			ByteTransform = new ReverseBytesTransform( );
		}

		#endregion

		#region ConnectServer

		/// <inheritdoc/>
		protected override void AddRequestHeaders( HttpContentHeaders headers )
		{
			if (!string.IsNullOrEmpty( this.token ))
				headers.Add( "X-Auth-Token", this.token );
		}

		/// <summary>
		/// 根据设置好的用户名和密码信息，登录远程的PLC，返回是否登录成功！在读写之前，必须成功调用当前的方法，获取到token，否则无法进行通信。<br />
		/// According to the set user name and password information, log in to the remote PLC, and return whether the login is successful! 
		/// Before reading and writing, the current method must be successfully called to obtain the token, 
		/// otherwise communication cannot be carried out.
		/// </summary>
		/// <returns>是否连接成功</returns>
		public OperateResult ConnectServer( )
		{
			JArray body = BuildConnectBody( incrementCount.GetCurrentValue( ), UserName, Password );

			OperateResult<string> read = Post( rawUrl, body.ToString( ) );
			if (!read.IsSuccess) return read;

			return CheckLoginResult( read.Content );
		}

		/// <summary>
		/// 和PLC断开当前的连接信息，主要是使得Token信息失效。<br />
		/// Disconnecting the current connection information from the PLC mainly makes the token information invalid.
		/// </summary>
		/// <returns>是否断开成功</returns>
		public OperateResult ConnectClose( )
		{
			return Logout( );
		}

#if !NET20 && !NET35
		/// <inheritdoc cref="ConnectServer"/>
		public async Task<OperateResult> ConnectServerAsync( )
		{
			JArray body = BuildConnectBody( incrementCount.GetCurrentValue( ), UserName, Password );

			OperateResult<string> read = await PostAsync( rawUrl, body.ToString( ) );
			if (!read.IsSuccess) return read;

			return CheckLoginResult( read.Content );
		}

		/// <inheritdoc cref="ConnectClose"/>
		public async Task<OperateResult> ConnectCloseAsync( )
		{
			return await LogoutAsync( );
		}
#endif

		private OperateResult CheckLoginResult( string response )
		{
			JArray resultArray = JArray.Parse( response );
			JObject json = (JObject)resultArray[0];

			OperateResult checkResult = CheckErrorResult( json );
			if (!checkResult.IsSuccess) return OperateResult.CreateFailedResult<JToken>( checkResult );

			if (json.ContainsKey( "result" ))
			{
				JObject result = json["result"] as JObject;
				this.token = result.Value<string>( "token" );
				return OperateResult.CreateSuccessResult( );
			}
			return new OperateResult( "Can't find result key and none token, login failed:" + Environment.NewLine + response );
		}

		/// <summary>
		/// 当前PLC的通信令牌，当调用<see cref="ConnectServer"/>时，会自动获取，当然你也可以手动赋值一个合法的令牌，跳过<see cref="ConnectServer"/>直接进行读写操作。<br />
		/// The communication token of the current PLC will be automatically obtained when <see cref="ConnectServer"/> is called. Of course, 
		/// you can also manually assign a valid token, skip <see cref="ConnectServer"/> and read it directly Write operation.
		/// </summary>
		public string Token
		{
			get => this.token;
			set => this.token = value;
		}

		#endregion

		/// <summary>
		/// 从PLC中根据输入的数据标签名称，读取出原始的字节数组信息，长度参数无效，需要二次解析<br />
		/// According to the input data tag name, read the original byte array information from the PLC, 
		/// the length parameter is invalid, and a second analysis is required
		/// </summary>
		/// <param name="address">标签的地址信息，例如 "全局DB".Static_21</param>
		/// <param name="length">无效的参数</param>
		/// <returns>原始的字节数组信息</returns>
		[HslMqttApi( "ReadByteArray", "" )]
		public override OperateResult<byte[]> Read( string address, ushort length )
		{
			JArray body = BuildReadRawBody( incrementCount.GetCurrentValue( ), address );

			OperateResult<string> read = Post( rawUrl, body.ToString( ) );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( read );

			return CheckReadRawResult( read.Content );
		}

		/// <summary>
		/// 从PLC中根据输入的数据标签名称，按照类型byte进行读取出数据信息<br />
		/// According to the input data tag name from the PLC, read the data information according to the type byte
		/// </summary>
		/// <param name="address">标签的地址信息，例如 "全局DB".Static_21</param>
		/// <returns>包含是否成功的结果对象</returns>
		[HslMqttApi( "ReadByte", "" )]
		public OperateResult<byte> ReadByte( string address ) => ByteTransformHelper.GetResultFromArray( Read( address, 1 ) );

		/// <inheritdoc/>
		[HslMqttApi( "ReadBoolArray", "" )]
		public override OperateResult<bool[]> ReadBool( string address, ushort length )
		{
			OperateResult<byte[]> read = Read( address, 0 );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( read );

			return OperateResult.CreateSuccessResult( read.Content.ToBoolArray( ).SelectBegin( length ) );
		}

		/// <summary>
		/// 将原始的字节数组信息写入到PLC的指定的数据标签里，写入方式为raw写入，是否写入成功取决于PLC的返回信息<br />
		/// Write the original byte array information to the designated data tag of the PLC. 
		/// The writing method is raw writing. Whether the writing is successful depends on the return information of the PLC.
		/// </summary>
		/// <param name="address">标签的地址信息，例如 "全局DB".Static_21</param>
		/// <param name="value">原始的字节数据信息</param>
		/// <returns>是否成功写入PLC的结果对象</returns>
		[HslMqttApi( "WriteByteArray", "" )]
		public override OperateResult Write( string address, byte[] value )
		{
			JArray body = BuildWriteRawBody( incrementCount.GetCurrentValue( ), address, value );

			OperateResult<string> read = Post( rawUrl, body.ToString( ) );
			if (!read.IsSuccess) return read;

			return CheckWriteResult( read.Content );
		}

		/// <summary>
		/// 写入<see cref="byte"/>数组数据，返回是否成功<br />
		/// Write <see cref="byte"/> array data, return whether the write was successful
		/// </summary>
		/// <param name="address">标签的地址信息，例如 "全局DB".Static_21</param>
		/// <param name="value">写入值</param>
		/// <returns>是否写入成功</returns>
		[HslMqttApi( "WriteByte", "" )]
		public OperateResult Write( string address, byte value )
		{
			return Write( address, new byte[] { value } );
		}

		/// <summary>
		/// 批量写入<see cref="bool"/>数组数据，返回是否成功<br />
		/// Batch write <see cref="bool"/> array data, return whether the write was successful
		/// </summary>
		/// <param name="address">标签的地址信息，例如 "全局DB".Static_21</param>
		/// <param name="value">写入值</param>
		/// <returns>是否写入成功</returns>
		[HslMqttApi( "WriteBoolArray", "" )]
		public override OperateResult Write( string address, bool[] value )
		{
			byte[] buffer = value.ToByteArray( );
			return Write( address, buffer );
		}

		/// <summary>
		/// 从PLC中读取字符串内容，需要指定数据标签名称，使用JSON方式读取，所以无论字符串是中英文都是支持读取的。<br />
		/// To read the string content from the PLC, you need to specify the data tag name and use JSON to read it, 
		/// so no matter whether the string is in Chinese or English, it supports reading.
		/// </summary>
		/// <param name="address">标签的地址信息，例如 "全局DB".Static_21</param>
		/// <param name="length">无效参数</param>
		/// <returns>包含是否成功的结果对象</returns>
		[HslMqttApi( "ReadString", "读取字符串" )]
		public override OperateResult<string> ReadString( string address, ushort length )
		{
			JArray body = BuildReadJTokenBody( incrementCount.GetCurrentValue( ), address );

			OperateResult<string> read = Post( rawUrl, body.ToString( ) );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<string>( read );

			OperateResult<JToken> extra = CheckAndExtraOneJsonResult( read.Content );
			if (!extra.IsSuccess) return OperateResult.CreateFailedResult<string>( extra );

			return OperateResult.CreateSuccessResult( extra.Content.Value<string>( ) );
		}

		/// <summary>
		/// 将字符串信息写入到PLC中，需要指定数据标签名称，如果PLC指定了类型为WString，才支持写入中文，否则会出现乱码。<br />
		/// To write string information into the PLC, you need to specify the data tag name. 
		/// If the PLC specifies the type as WString, it supports writing in Chinese, otherwise garbled characters will appear.
		/// </summary>
		/// <param name="address">标签的地址信息，例如 "全局DB".Static_21</param>
		/// <param name="value">字符串数据信息</param>
		/// <returns>是否成功写入</returns>
		[HslMqttApi( "WriteString", "" )]
		public override OperateResult Write( string address, string value )
		{
			JArray body = BuildWriteJTokenBody( incrementCount.GetCurrentValue( ), address, new JValue( value ) );

			OperateResult<string> read = Post( rawUrl, body.ToString( ) );
			if (!read.IsSuccess) return read;

			return CheckWriteResult( read.Content );
		}

#if !NET20 && !NET35

		/// <inheritdoc cref="Read(string, ushort)"/>
		public async override Task<OperateResult<byte[]>> ReadAsync( string address, ushort length )
		{
			JArray body = BuildReadRawBody( incrementCount.GetCurrentValue( ), address );
			OperateResult<string> read = await PostAsync( rawUrl, body.ToString( ) );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( read );

			return CheckReadRawResult( read.Content );
		}

		/// <inheritdoc cref="ReadByte(string)"/>
		public async Task<OperateResult<byte>> ReadByteAsync( string address ) => ByteTransformHelper.GetResultFromArray( await ReadAsync( address, 1 ) );

		/// <inheritdoc cref="ReadBool(string, ushort)"/>
		public async override Task<OperateResult<bool[]>> ReadBoolAsync( string address, ushort length )
		{
			OperateResult<byte[]> read = await ReadAsync( address, 0 );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( read );

			return OperateResult.CreateSuccessResult( read.Content.ToBoolArray( ).SelectBegin( length ) );
		}

		/// <inheritdoc cref="ReadString(string, ushort)"/>
		public async override Task<OperateResult<string>> ReadStringAsync( string address, ushort length )
		{
			JArray body = BuildReadJTokenBody( incrementCount.GetCurrentValue( ), address );
			OperateResult<string> read = await PostAsync( rawUrl, body.ToString( ) );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<string>( read );

			OperateResult<JToken> extra = CheckAndExtraOneJsonResult( read.Content );
			if (!extra.IsSuccess) return OperateResult.CreateFailedResult<string>( extra );

			return OperateResult.CreateSuccessResult( extra.Content.Value<string>( ) );
		}

		/// <inheritdoc cref="Write(string, byte[])"/>
		public async override Task<OperateResult> WriteAsync( string address, byte[] value )
		{
			JArray body = BuildWriteRawBody( incrementCount.GetCurrentValue( ), address, value );

			OperateResult<string> read = await PostAsync( rawUrl, body.ToString( ) );
			if (!read.IsSuccess) return read;

			return CheckWriteResult( read.Content );
		}

		/// <inheritdoc cref="Write(string, byte)"/>
		public async Task<OperateResult> WriteAsync( string address, byte value )
		{
			return await WriteAsync( address, new byte[] { value } );
		}

		/// <inheritdoc cref="Write(string, bool[])"/>
		public async override Task<OperateResult> WriteAsync( string address, bool[] value )
		{
			byte[] buffer = value.ToByteArray( );
			return await WriteAsync( address, buffer );
		}

		/// <inheritdoc cref="Write(string, string)"/>
		public async override Task<OperateResult> WriteAsync( string address, string value )
		{
			JArray body = BuildWriteJTokenBody( incrementCount.GetCurrentValue( ), address, new JValue( value ) );

			OperateResult<string> read = await PostAsync( rawUrl, body.ToString( ) );
			if (!read.IsSuccess) return read;

			return CheckWriteResult( read.Content );
		}
#endif

		#region Advance Funtion
		/// <summary>
		/// 读取当前PLC的操作模式，如果读取成功，结果将会是如下值之一：STOP, STARTUP, RUN, HOLD, -<br />
		/// Read the current operating mode of the PLC. If the reading is successful, 
		/// the result will be one of the following values: STOP, STARTUP, RUN, HOLD,-
		/// </summary>
		/// <returns>结果对象</returns>
		[HslMqttApi( "读取当前PLC的操作模式，如果读取成功，结果将会是如下值之一：STOP, STARTUP, RUN, HOLD, -" )]
		public OperateResult<string> ReadOperatingMode( )
		{
			JArray body = BuildRequestBody( "Plc.ReadOperatingMode", null, incrementCount.GetCurrentValue( ) );
			OperateResult<string> read = Post( rawUrl, body.ToString( ) );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<string>( read );

			OperateResult<JToken> extra = CheckAndExtraOneJsonResult( read.Content );
			if (!extra.IsSuccess) return OperateResult.CreateFailedResult<string>( extra );

			return OperateResult.CreateSuccessResult( extra.Content.Value<string>( ) );
		}

#if !NET20 && !NET35
		/// <inheritdoc cref="ReadOperatingMode"/>
		public async Task<OperateResult<string>> ReadOperatingModeAsync( )
		{
			JArray body = BuildRequestBody( "Plc.ReadOperatingMode", null, incrementCount.GetCurrentValue( ) );
			OperateResult<string> read = await PostAsync( rawUrl, body.ToString( ) );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<string>( read );

			OperateResult<JToken> extra = CheckAndExtraOneJsonResult( read.Content );
			if (!extra.IsSuccess) return OperateResult.CreateFailedResult<string>( extra );

			return OperateResult.CreateSuccessResult( extra.Content.Value<string>( ) );
		}
#endif
		/// <summary>
		/// <b>[商业授权]</b> 从PLC读取多个地址的数据信息，每个地址的数据类型可以不一致，需要自动从<see cref="JToken"/>中提取出正确的数据<br />
		/// <b>[Authorization]</b> Read the data information of multiple addresses from the PLC, the data type of each address can be inconsistent, 
		/// you need to automatically extract the correct data from <see cref="JToken"/>
		/// </summary>
		/// <remarks>
		/// 一旦中间有一个地址失败了，本方法就会返回失败，所以在调用本方法时，需要确保所有的地址正确。
		/// </remarks>
		/// <param name="address">"全局DB".Static_21</param>
		/// <returns>返回是否读取成功的结果对象</returns>
		[HslMqttApi("ReadJTokens", "从PLC读取多个地址的数据信息，每个地址的数据类型可以不一致" )]
		public OperateResult<JToken[]> Read(string[] address )
		{
			if (!Authorization.asdniasnfaksndiqwhawfskhfaiw( )) return new OperateResult<JToken[]>( StringResources.Language.InsufficientPrivileges );

			JArray body = BuildReadJTokenBody( incrementCount.GetCurrentValue( ), address );

			OperateResult<string> read = Post( rawUrl, body.ToString( ) );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<JToken[]>( read );

			return CheckAndExtraJsonResult( read.Content );
		}
#if !NET20 && !NET35
		/// <inheritdoc cref="Read(string[])"/>
		public async Task<OperateResult<JToken[]>> ReadAsync( string[] address )
		{
			if (!Authorization.asdniasnfaksndiqwhawfskhfaiw( )) return new OperateResult<JToken[]>( StringResources.Language.InsufficientPrivileges );

			JArray body = BuildReadJTokenBody( incrementCount.GetCurrentValue( ), address );

			OperateResult<string> read = await PostAsync( rawUrl, body.ToString( ) );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<JToken[]>( read );

			return CheckAndExtraJsonResult( read.Content );
		}
#endif
		#region ReadWrite DateTime

		/// <inheritdoc cref="SiemensS7Net.ReadDateTime(string)"/>
		[HslMqttApi( "ReadDateTime", "读取PLC的时间格式的数据，这个格式是s7格式的一种" )]
		public OperateResult<DateTime> ReadDateTime( string address ) => ByteTransformHelper.GetResultFromBytes( Read( address, 8 ), SiemensDateTime.FromByteArray );

		/// <inheritdoc cref="SiemensS7Net.Write(string, DateTime)"/>
		[HslMqttApi( "WriteDateTime", "写入PLC的时间格式的数据，这个格式是s7格式的一种" )]
		public OperateResult Write( string address, DateTime dateTime ) => Write( address, SiemensDateTime.ToByteArray( dateTime ) );

#if !NET20 && !NET35

		/// <inheritdoc cref="ReadDateTime(string)"/>
		public async Task<OperateResult<DateTime>> ReadDateTimeAsync( string address ) => ByteTransformHelper.GetResultFromBytes( await ReadAsync( address, 8 ), SiemensDateTime.FromByteArray );

		/// <inheritdoc cref="Write(string, DateTime)"/>
		public async Task<OperateResult> WriteAsync( string address, DateTime dateTime ) => await WriteAsync( address, SiemensDateTime.ToByteArray( dateTime ) );
#endif
		/// <summary>
		/// <b>[商业授权]</b> 读取PLC的RPC接口的版本号信息<br />
		/// <b>[Authorization]</b> Read the version number information of the PLC's RPC interface
		/// </summary>
		/// <returns>包含是否成功的结果对象</returns>
		[HslMqttApi( "读取PLC的RPC接口的版本号信息" )]
		public OperateResult<double> ReadVersion( )
		{
			if (!Authorization.asdniasnfaksndiqwhawfskhfaiw( )) return new OperateResult<double>( StringResources.Language.InsufficientPrivileges );

			JArray body = BuildRequestBody( "Api.Version", null, incrementCount.GetCurrentValue( ) );

			OperateResult<string> read = Post( rawUrl, body.ToString( ) );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<double>( read );

			OperateResult<JToken> extra = CheckAndExtraOneJsonResult( read.Content );
			if (!extra.IsSuccess) return OperateResult.CreateFailedResult<double>( extra );

			return OperateResult.CreateSuccessResult( extra.Content.Value<double>( ) );
		}
#if !NET20 && !NET35
		/// <inheritdoc cref="ReadVersion"/>
		public async Task<OperateResult<double>> ReadVersionAsync( )
		{
			if (!Authorization.asdniasnfaksndiqwhawfskhfaiw( )) return new OperateResult<double>( StringResources.Language.InsufficientPrivileges );

			JArray body = BuildRequestBody( "Api.Version", null, incrementCount.GetCurrentValue( ) );

			OperateResult<string> read = await PostAsync( rawUrl, body.ToString( ) );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<double>( read );

			OperateResult<JToken> extra = CheckAndExtraOneJsonResult( read.Content );
			if (!extra.IsSuccess) return OperateResult.CreateFailedResult<double>( extra );

			return OperateResult.CreateSuccessResult( extra.Content.Value<double>( ) );
		}
#endif
		/// <summary>
		/// <b>[商业授权]</b> 对PLC对象进行PING操作<br />
		/// <b>[Authorization]</b> PING the PLC object
		/// </summary>
		/// <returns>是否PING成功</returns>
		[HslMqttApi( "对PLC对象进行PING操作" )]
		public OperateResult ReadPing( )
		{
			if (!Authorization.asdniasnfaksndiqwhawfskhfaiw( )) return new OperateResult<double>( StringResources.Language.InsufficientPrivileges );

			JArray body = BuildRequestBody( "Api.Ping", null, incrementCount.GetCurrentValue( ) );

			OperateResult<string> read = Post( rawUrl, body.ToString( ) );
			if (!read.IsSuccess) return read;

			return CheckErrorResult( JArray.Parse( read.Content )[0] as JObject );
		}
#if !NET20 && !NET35
		/// <inheritdoc cref="ReadPing"/>
		public async Task<OperateResult> ReadPingAsync( )
		{
			if (!Authorization.asdniasnfaksndiqwhawfskhfaiw( )) return new OperateResult<double>( StringResources.Language.InsufficientPrivileges );

			JArray body = BuildRequestBody( "Api.Ping", null, incrementCount.GetCurrentValue( ) );

			OperateResult<string> read = await PostAsync( rawUrl, body.ToString( ) );
			if (!read.IsSuccess) return read;

			return CheckErrorResult( JArray.Parse( read.Content )[0] as JObject );
		}
#endif
		/// <summary>
		/// 从PLC退出登录，当前的token信息失效，需要再次调用<see cref="ConnectServer"/>获取新的token信息才可以。<br />
		/// Log out from the PLC, the current token information is invalid, you need to call <see cref="ConnectServer"/> again to get the new token information.
		/// </summary>
		/// <returns>是否成功</returns>
		[HslMqttApi( "从PLC退出登录，当前的token信息失效，需要再次调用ConnectServer获取新的token信息才可以" )]
		public OperateResult Logout( )
		{
			JArray body = BuildRequestBody( "Api.Logout", null, incrementCount.GetCurrentValue( ) );

			OperateResult<string> read = Post( rawUrl, body.ToString( ) );
			if (!read.IsSuccess) return read;

			return CheckErrorResult( JArray.Parse( read.Content )[0] as JObject );
		}

#if !NET20 && !NET35
		/// <inheritdoc cref="Logout"/>
		public async Task<OperateResult> LogoutAsync( )
		{
			JArray body = BuildRequestBody( "Api.Logout", null, incrementCount.GetCurrentValue( ) );

			OperateResult<string> read = await PostAsync( rawUrl, body.ToString( ) );
			if (!read.IsSuccess) return read;

			return CheckErrorResult( JArray.Parse( read.Content )[0] as JObject );
		}
#endif
		#endregion

		#endregion

		private string rawUrl = "api/jsonrpc";
		private string token = string.Empty;
		private SoftIncrementCount incrementCount = new SoftIncrementCount( ushort.MaxValue, 1, 1 );

		#region Static Helper

		private static JObject GetJsonRpc( string method, JObject paramsJson, long id )
		{
			JObject json = new JObject( );
			json.Add( "jsonrpc", new JValue( "2.0" ) );
			json.Add( "method", new JValue( method ) );
			json.Add( "id", new JValue( id ) );
			if (paramsJson != null)
				json.Add( "params", paramsJson );
			return json;
		}

		private static JArray BuildRequestBody( string method, JObject paramsJson, long id )
		{
			return new JArray( ) { GetJsonRpc( method, paramsJson, id ) };
		}

		private static JArray BuildConnectBody( long id, string name, string password )
		{
			JObject paras = new JObject( );
			paras.Add( "user", new JValue( name ) );
			paras.Add( "password", new JValue( password ) );
			return BuildRequestBody( "Api.Login", paras, id );
		}


		private static JArray BuildReadRawBody( long id, string address )
		{
			JObject paras = new JObject( );
			paras.Add( "var", new JValue( address ) );
			paras.Add( "mode", new JValue( "raw" ) );
			return BuildRequestBody( "PlcProgram.Read", paras, id );
		}

		private static JArray BuildWriteRawBody( long id, string address, byte[] value )
		{
			JObject paras = new JObject( );
			paras.Add( "var", new JValue( address ) );
			paras.Add( "mode", new JValue( "raw" ) );
			paras.Add( "value", new JArray( value.Select( m => (int)m ).ToArray( ) ) );
			return BuildRequestBody( "PlcProgram.Write", paras, id );
		}

		private static JArray BuildWriteJTokenBody( long id, string address, JToken value )
		{
			JObject paras = new JObject( );
			paras.Add( "var", new JValue( address ) );
			paras.Add( "value", value );
			return BuildRequestBody( "PlcProgram.Write", paras, id );
		}

		private static JArray BuildReadJTokenBody( long id, string address )
		{
			JObject paras = new JObject( );
			paras.Add( "var", new JValue( address ) );
			return BuildRequestBody( "PlcProgram.Read", paras, id );
		}
		private static JArray BuildReadJTokenBody( long id, string[] address )
		{
			JArray jArray = new JArray( );
			for (int i = 0; i < address.Length; i++)
			{
				JObject paras = new JObject( );
				paras.Add( "var", new JValue( address[i] ) );
				jArray.Add( GetJsonRpc( "PlcProgram.Read", paras, id + i ) );
			}
			return jArray;
		}


		private static OperateResult CheckErrorResult( JObject json )
		{
			if (json.ContainsKey( "error" ))
			{
				JObject error = json["error"] as JObject;
				int code = error.Value<int>( "code" );
				string message = error.Value<string>( "message" );
				return new OperateResult( code, message );
			}
			return OperateResult.CreateSuccessResult( );
		}

		private static OperateResult<byte[]> CheckReadRawResult( string response )
		{
			OperateResult<JToken> checkJson = CheckAndExtraOneJsonResult( response );
			if (!checkJson.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( checkJson );

			JArray content = checkJson.Content as JArray;
			return OperateResult.CreateSuccessResult( content.Select( m => m.Value<byte>( ) ).ToArray( ) );

		}

		private static OperateResult CheckWriteResult( string response )
		{
			JArray resultArray = JArray.Parse( response );
			JObject json = (JObject)resultArray[0];

			OperateResult checkResult = CheckErrorResult( json );
			if (!checkResult.IsSuccess) return OperateResult.CreateFailedResult<JToken>( checkResult );

			if (json.ContainsKey( "result" ))
			{
				bool value = json["result"].Value<bool>( );
				return value ? OperateResult.CreateSuccessResult( ) : new OperateResult( json.ToString( ) );
			}

			return new OperateResult<JToken>( "Can't find result key and none token, login failed:" + Environment.NewLine + response );
		}

		private static OperateResult<JToken> CheckAndExtraOneJsonResult( string response )
		{
			OperateResult<JToken[]> extra = CheckAndExtraJsonResult( response );
			if (!extra.IsSuccess) return OperateResult.CreateFailedResult<JToken>( extra );

			return OperateResult.CreateSuccessResult( extra.Content[0] );
		}

		private static OperateResult<JToken[]> CheckAndExtraJsonResult(string response )
		{
			JArray jArray = JArray.Parse( response );
			List<JToken> results = new List<JToken>( );
			for (int i = 0; i < jArray.Count; i++)
			{
				JObject json = jArray[i] as JObject;
				if (json == null) continue;

				OperateResult checkResult = CheckErrorResult( json );
				if (!checkResult.IsSuccess) return OperateResult.CreateFailedResult<JToken[]>( checkResult );

				if (json.ContainsKey( "result" )) results.Add( json["result"] );
				else
					return new OperateResult<JToken[]>( "Can't find result key and none token, login failed:" + Environment.NewLine + response );
			}
			return OperateResult.CreateSuccessResult( results.ToArray( ) );
		}

		#endregion
	}
}
