using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HslCommunication;
using HslCommunication.Core;
using HslCommunication.Core.Net;
using HslCommunication.Core.IMessage;
using HslCommunication.Core.Address;
using HslCommunication.BasicFramework;
using HslCommunication.Reflection;
#if !NET20 && !NET35
using System.Threading.Tasks;
#endif

namespace HslCommunication.Profinet.Yokogawa
{
	/// <summary>
	/// 横河PLC的二进制通信类，支持X,Y,I,E,M,T,C,L继电器类型的数据读写，支持D,B,F,R,V,Z,W,TN,CN寄存器类型的数据读写，还支持一些高级的信息读写接口，详细参考API文档。<br />
	/// Yokogawa PLC's binary communication type, supports X, Y, I, E, M, T, C, L relay type data read and write, 
	/// supports D, B, F, R, V, Z, W, TN, CN registers Types of data reading and writing, and some advanced information reading and writing interfaces are also supported. 
	/// Please refer to the API documentation for details.
	/// </summary>
	/// <remarks>
	/// 基础的数据读写面向VIP用户开放，高级的读写随机数据，启动停止命令，读取程序状态，
	/// 系统信息，PLC时间，读写特殊的模块数据需要商业用户授权，读取的数据长度，读取的随机地址长度，在商业授权下，长度不受限制，可以无限大。
	/// </remarks>
	/// <example>
	/// 地址示例如下：
	/// <list type="table">
	///   <listheader>
	///     <term>地址名称</term>
	///     <term>地址代号</term>
	///     <term>示例</term>
	///     <term>字操作</term>
	///     <term>位操作</term>
	///     <term>备注</term>
	///   </listheader>
	///   <item>
	///     <term>Input relay</term>
	///     <term>X</term>
	///     <term>X100,X200</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term>只能读，不能写</term>
	///   </item>
	///   <item>
	///     <term>Output relay</term>
	///     <term>Y</term>
	///     <term>Y100,Y200</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>Internal relay</term>
	///     <term>I</term>
	///     <term>I100,I200</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>Share relay</term>
	///     <term>E</term>
	///     <term>E100,E200</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>Special relay</term>
	///     <term>M</term>
	///     <term>M100,M200</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>Time relay</term>
	///     <term>T</term>
	///     <term>T100,T200</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>Counter relay</term>
	///     <term>C</term>
	///     <term>C100,C200</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>link relay</term>
	///     <term>L</term>
	///     <term>L100, L200</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>Data register</term>
	///     <term>D</term>
	///     <term>D100,D200</term>
	///     <term>√</term>
	///     <term>×</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>File register</term>
	///     <term>B</term>
	///     <term>B100,B200</term>
	///     <term>√</term>
	///     <term>×</term>
	///     <term>Only available for sequence CPU modules F3SP22, F3SP25, F3SP28, F3SP35, F3SP38, F3SP53, F3SP58, F3SP59, F3SP66, F3SP67, F3SP71 and F3SP76</term>
	///   </item>
	///   <item>
	///     <term>Cache register</term>
	///     <term>F</term>
	///     <term>F100,F200</term>
	///     <term>√</term>
	///     <term>×</term>
	///     <term>Only available for sequence CPU modules F3SP71 and F3SP76</term>
	///   </item>
	///   <item>
	///     <term>Shared register</term>
	///     <term>R</term>
	///     <term>R100,R200</term>
	///     <term>√</term>
	///     <term>×</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>Index register</term>
	///     <term>V</term>
	///     <term>V100,V200</term>
	///     <term>√</term>
	///     <term>×</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>Special register</term>
	///     <term>Z</term>
	///     <term>Z100,Z200</term>
	///     <term>√</term>
	///     <term>×</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>Link register</term>
	///     <term>W</term>
	///     <term>W100,W200</term>
	///     <term>√</term>
	///     <term>×</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>Timer current value</term>
	///     <term>TN</term>
	///     <term>TN100,TN200</term>
	///     <term>√</term>
	///     <term>×</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>Counter current value</term>
	///     <term>CN</term>
	///     <term>CN100,CN200</term>
	///     <term>√</term>
	///     <term>×</term>
	///     <term></term>
	///   </item>
	/// </list>
	/// 例如我们正常读取一个D100的数据如下：
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Profinet\YokogawaLinkTcpSample.cs" region="Sample1" title="Read示例" />
	/// 我们在读取的时候可以动态的变更cpu信息，参考下面的代码
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Profinet\YokogawaLinkTcpSample.cs" region="Sample2" title="Read示例" />
	/// 关于随机读写的代码示例，可以读写地址分布很散的地址，参考下面的代码
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Profinet\YokogawaLinkTcpSample.cs" region="Sample3" title="Read示例" />
	/// 最后看一下读取特殊模块的数据，可以读取基本的字节数据，也可以使用富文本的地址读取
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Profinet\YokogawaLinkTcpSample.cs" region="Sample4" title="Read示例" />
	/// </example>
	public class YokogawaLinkTcp : NetworkDeviceBase
	{
		#region Contructor

		/// <summary>
		/// 实例化一个默认的对象<br />
		/// Instantiate a default object
		/// </summary>
		public YokogawaLinkTcp( ) 
		{
			ByteTransform = new ReverseWordTransform( );
			ByteTransform.DataFormat = DataFormat.CDAB;
			CpuNumber     = 0x01;
		}

		/// <summary>
		/// 指定IP地址和端口号来实例化一个对象<br />
		/// Specify the IP address and port number to instantiate an object
		/// </summary>
		/// <param name="ipAddress">Ip地址</param>
		/// <param name="port">端口号</param>
		public YokogawaLinkTcp(string ipAddress, int port ) : this( )
		{
			IpAddress     = ipAddress;
			Port          = port;
		}

		#endregion

		/// <inheritdoc/>
		protected override INetMessage GetNewNetMessage( ) => new YokogawaLinkBinaryMessage( );

		/// <summary>
		/// 获取或设置当前的CPU Number，默认值为1<br />
		/// Get or set the current CPU Number, the default value is 1
		/// </summary>
		public byte CpuNumber { get; set; }

		#region Read Write Override

		/// <inheritdoc/>
		/// <remarks>
		/// 读取的线圈地址支持X,Y,I,E,M,T,C,L，寄存器地址支持D,B,F,R,V,Z,W,TN,CN，举例：D100；也可以携带CPU进行访问，举例：cpu=2;D100<br />
		/// <b>[商业授权]</b> 如果想要读取特殊模块的数据，需要使用 <b>Special:</b> 开头标记，举例：Special:unit=0;slot=1;100<br />
		/// The read coil address supports X, Y, I, E, M, T, C, L, and the register address supports D, B, F, R, V, Z, W, TN, CN, for example: D100; 
		/// it can also be carried CPU access, for example: cpu=2;D100. <br />
		/// <b>[Authorization]</b> If you want to read the data of a special module, you need to use the <b>Special:</b> beginning tag, for example: Special:unit=0;slot=1;100
		/// </remarks>
		[HslMqttApi( "ReadByteArray", "Supports X,Y,I,E,M,T,C,L,D,B,F,R,V,Z,W,TN,CN, for example: D100; or cpu=2;D100 or Special:unit=0;slot=1;100" )]
		public override OperateResult<byte[]> Read( string address, ushort length )
		{
			OperateResult<List<byte[]>> command;
			if (address.StartsWith( "Special:" ) || address.StartsWith( "special:" ))
			{
				if (!Authorization.asdniasnfaksndiqwhawfskhfaiw( )) return new OperateResult<byte[]>( StringResources.Language.InsufficientPrivileges );
				command = BuildReadSpecialModule( CpuNumber, address, length );
			}
			else
			{
				command = BuildReadCommand( CpuNumber, address, length, false );
			}
			if (!command.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( command );

			List<byte> content = new List<byte>( );
			for (int i = 0; i < command.Content.Count; i++)
			{
				OperateResult<byte[]> read = ReadFromCoreServer( command.Content[i] );
				if (!read.IsSuccess) return read;

				OperateResult<byte[]> check = CheckContent( read.Content );
				content.AddRange( check.Content );
			}

			return OperateResult.CreateSuccessResult( content.ToArray( ) );
		}

		/// <inheritdoc/>
		/// <remarks>
		/// 写入的线圈地址支持Y,I,E,M,T,C,L，寄存器地址支持D,B,F,R,V,Z,W,TN,CN，举例：D100；也可以携带CPU进行访问，举例：cpu=2;D100<br />
		/// 如果想要写入特殊模块的数据，需要使用 <b>Special:</b> 开头标记，举例：Special:unit=0;slot=1;100<br />
		/// The read coil address supports Y, I, E, M, T, C, L, and the register address supports D, B, F, R, V, Z, W, TN, CN, for example: D100; 
		/// it can also be carried CPU access, for example: cpu=2;D100.
		/// If you want to read the data of a special module, you need to use the <b>Special:</b> beginning tag, for example: Special:unit=0;slot=1;100
		/// </remarks>
		[HslMqttApi( "WriteByteArray", "Supports Y,I,E,M,T,C,L,D,B,F,R,V,Z,W,TN,CN, for example: D100; or cpu=2;D100 or Special:unit=0;slot=1;100" )]
		public override OperateResult Write( string address, byte[] value )
		{
			OperateResult<byte[]> command;
			if (address.StartsWith( "Special:" ) || address.StartsWith( "special:" ))
			{
				if (!Authorization.asdniasnfaksndiqwhawfskhfaiw( )) return new OperateResult( StringResources.Language.InsufficientPrivileges );
				command = BuildWriteSpecialModule( CpuNumber, address, value );
			}
			else
			{
				command = BuildWriteWordCommand( CpuNumber, address, value );
			}
			if (!command.IsSuccess) return command;

			OperateResult<byte[]> read = ReadFromCoreServer( command.Content );
			if (!read.IsSuccess) return read;

			return CheckContent( read.Content );
		}

		/// <inheritdoc/>
		/// <remarks>
		/// 读取的线圈地址支持X,Y,I,E,M,T,C,L，举例：Y100；也可以携带CPU进行访问，举例：cpu=2;Y100<br />
		/// The read coil address supports X, Y, I, E, M, T, C, L, for example: Y100; you can also carry the CPU for access, for example: cpu=2;Y100
		/// </remarks>
		[HslMqttApi( "ReadBoolArray", "Read coil address supports X, Y, I, E, M, T, C, L, for example: Y100; or cpu=2;Y100" )]
		public override OperateResult<bool[]> ReadBool( string address, ushort length )
		{
			OperateResult<List<byte[]>> command = BuildReadCommand( CpuNumber, address, length, true );
			if (!command.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( command );

			List<byte> content = new List<byte>( );
			for (int i = 0; i < command.Content.Count; i++)
			{
				OperateResult<byte[]> read = ReadFromCoreServer( command.Content[i] );
				if (!read.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( read );

				OperateResult<byte[]> check = CheckContent( read.Content );
				if (!check.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( check );

				content.AddRange( check.Content );
			}

			return OperateResult.CreateSuccessResult( content.ToArray( ).Select( m => m != 0x00 ).ToArray( ) );
		}

		/// <inheritdoc/>
		/// <remarks>
		/// 写入的线圈地址支持Y,I,E,M,T,C,L，举例：Y100；也可以携带CPU进行访问，举例：cpu=2;Y100<br />
		/// The write coil address supports Y, I, E, M, T, C, L, for example: Y100; you can also carry the CPU for access, for example: cpu=2;Y100
		/// </remarks>
		[HslMqttApi( "WriteBoolArray", "The write coil address supports Y, I, E, M, T, C, L, for example: Y100; or cpu=2;Y100" )]
		public override OperateResult Write( string address, bool[] value )
		{
			OperateResult<byte[]> command = BuildWriteBoolCommand( CpuNumber, address, value );
			if (!command.IsSuccess) return command;

			OperateResult<byte[]> read = ReadFromCoreServer( command.Content );
			if (!read.IsSuccess) return read;

			return CheckContent( read.Content );
		}

		#endregion

		#region Read Write Random

		/// <summary>
		/// <b>[商业授权]</b> 随机读取<see cref="bool"/>数组信息，主需要出传入<see cref="bool"/>数组地址信息，就可以返回批量<see cref="bool"/>值<br />
		/// <b>[Authorization]</b> Random read <see cref="bool"/> array information, the master needs to pass in the <see cref="bool"/> array address information, and then the batch can be returned to <see cref="bool"/> value
		/// </summary> 
		/// <param name="address">批量地址信息</param>
		/// <remarks>
		/// 读取的线圈地址支持X,Y,I,E,M,T,C,L，举例：Y100；也可以携带CPU进行访问，举例：cpu=2;Y100<br />
		/// The read coil address supports X, Y, I, E, M, T, C, L, for example: Y100; you can also carry the CPU for access, for example: cpu=2;Y100
		/// </remarks>
		/// <returns>带有成功标志的Bool数组信息</returns>
		[HslMqttApi(Description = "Read random relay, supports X, Y, I, E, M, T, C, L, for example: Y100;" )]
		public OperateResult<bool[]> ReadRandomBool( string[] address )
		{
			if (!Authorization.asdniasnfaksndiqwhawfskhfaiw( )) return new OperateResult<bool[]>( StringResources.Language.InsufficientPrivileges );

			OperateResult<List<byte[]>> command = BuildReadRandomCommand( CpuNumber, address, true );
			if (!command.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( command );

			List<bool> lists = new List<bool>( );
			foreach (var content in command.Content)
			{
				OperateResult<byte[]> read = ReadFromCoreServer( content );
				if (!read.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( read );

				OperateResult<byte[]> check = CheckContent( read.Content );
				if (!check.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( check );

				lists.AddRange( check.Content.Select( m => m != 0x00 ) );
			}
			return OperateResult.CreateSuccessResult( lists.ToArray( ) );
		}

		/// <summary>
		/// <b>[商业授权]</b> 随机写入<see cref="bool"/>数组信息，主需要出传入<see cref="bool"/>数组地址信息，以及对应的<see cref="bool"/>数组值<br />
		/// <b>[Authorization]</b> Randomly write the <see cref="bool"/> array information, the main need to pass in the <see cref="bool"/> array address information, 
		/// and the corresponding <see cref="bool"/> array value
		/// </summary>
		/// <param name="address">批量地址信息</param>
		/// <param name="value">批量的数据值信息</param>
		/// <remarks>
		/// 写入的线圈地址支持Y,I,E,M,T,C,L，举例：Y100；也可以携带CPU进行访问，举例：cpu=2;Y100<br />
		/// The write coil address supports Y, I, E, M, T, C, L, for example: Y100; you can also carry the CPU for access, for example: cpu=2;Y100
		/// </remarks>
		/// <returns>是否写入成功</returns>
		[HslMqttApi(Description = "Write random relay, supports Y, I, E, M, T, C, L, for example: Y100;" )]
		public OperateResult WriteRandomBool(string[] address, bool[] value )
		{
			if (!Authorization.asdniasnfaksndiqwhawfskhfaiw( )) return new OperateResult<bool[]>( StringResources.Language.InsufficientPrivileges );

			if (address.Length != value.Length) return new OperateResult( StringResources.Language.TwoParametersLengthIsNotSame );
			
			OperateResult<byte[]> command = BuildWriteRandomBoolCommand( CpuNumber, address, value );
			if (!command.IsSuccess) return command;

			OperateResult<byte[]> read = ReadFromCoreServer( command.Content );
			if (!read.IsSuccess) return read;

			OperateResult<byte[]> check = CheckContent( read.Content );
			if (!check.IsSuccess) return check;

			return OperateResult.CreateSuccessResult( );
		}

		/// <summary>
		/// <b>[商业授权]</b> 随机读取<see cref="byte"/>数组信息，主需要出传入<see cref="byte"/>数组地址信息，就可以返回批量<see cref="byte"/>值<br />
		/// <b>[Authorization]</b> Random read <see cref="byte"/> array information, the master needs to pass in the <see cref="byte"/> array address information, and then the batch can be returned to <see cref="byte"/> value
		/// </summary> 
		/// <param name="address">批量地址信息</param>
		/// <remarks>
		/// </remarks>
		/// <returns>带有成功标志的Bool数组信息</returns>
		[HslMqttApi(Description = "Read random register, supports D,B,F,R,V,Z,W,TN,CN，example: D100" )]
		public OperateResult<byte[]> ReadRandom(string[] address )
		{
			if (!Authorization.asdniasnfaksndiqwhawfskhfaiw( )) return new OperateResult<byte[]>( StringResources.Language.InsufficientPrivileges );

			OperateResult<List<byte[]>> command = BuildReadRandomCommand( CpuNumber, address, false );
			if (!command.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( command );

			List<byte> lists = new List<byte>( );
			foreach (var content in command.Content)
			{
				OperateResult<byte[]> read = ReadFromCoreServer( content );
				if (!read.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( read );

				OperateResult<byte[]> check = CheckContent( read.Content );
				if (!check.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( check );

				lists.AddRange( check.Content );
			}
			return OperateResult.CreateSuccessResult( lists.ToArray( ) );
		}

		/// <summary>
		/// <b>[商业授权]</b> 随机读取<see cref="short"/>数组信息，主需要出传入<see cref="short"/>数组地址信息，就可以返回批量<see cref="short"/>值<br />
		/// <b>[Authorization]</b> Random read <see cref="short"/> array information, the master needs to pass in the <see cref="short"/> array address information, and then the batch can be returned to <see cref="short"/> value
		/// </summary> 
		/// <param name="address">批量地址信息</param>
		/// <returns>带有成功标志的Bool数组信息</returns>
		[HslMqttApi(Description = "Read random register, and get short array, supports D, B, F, R, V, Z, W, TN, CN，example: D100")]
		public OperateResult<short[]> ReadRandomInt16( string[] address ) => ReadRandom( address ).Then( m => OperateResult.CreateSuccessResult( ByteTransform.TransInt16( m, 0, address.Length ) ) );

		/// <summary>
		/// <b>[商业授权]</b> 随机读取<see cref="ushort"/>数组信息，主需要出传入<see cref="ushort"/>数组地址信息，就可以返回批量<see cref="ushort"/>值<br />
		/// <b>[Authorization]</b> Random read <see cref="ushort"/> array information, the master needs to pass in the <see cref="ushort"/> array address information, and then the batch can be returned to <see cref="ushort"/> value
		/// </summary> 
		/// <param name="address">批量地址信息</param>
		/// <returns>带有成功标志的Bool数组信息</returns>
		[HslMqttApi( Description = "Read random register, and get ushort array, supports D, B, F, R, V, Z, W, TN, CN，example: D100" )]
		public OperateResult<ushort[]> ReadRandomUInt16( string[] address ) => ReadRandom( address ).Then( m => OperateResult.CreateSuccessResult( ByteTransform.TransUInt16( m, 0, address.Length ) ) );

		/// <summary>
		/// <b>[商业授权]</b> 随机写入<see cref="byte"/>数组信息，主需要出传入<see cref="byte"/>数组地址信息，以及对应的<see cref="byte"/>数组值<br />
		/// <b>[Authorization]</b> Randomly write the <see cref="byte"/> array information, the main need to pass in the <see cref="byte"/> array address information, 
		/// and the corresponding <see cref="byte"/> array value
		/// </summary>
		/// <param name="address">批量地址信息</param>
		/// <param name="value">批量的数据值信息</param>
		/// <returns>是否写入成功</returns>
		[HslMqttApi(ApiTopic = "WriteRandom", Description = "Randomly write the byte array information, the main need to pass in the byte array address information" )]
		public OperateResult WriteRandom(string[] address, byte[] value )
		{
			if (!Authorization.asdniasnfaksndiqwhawfskhfaiw( )) return new OperateResult<bool[]>( StringResources.Language.InsufficientPrivileges );

			if (address.Length * 2 != value.Length) return new OperateResult( StringResources.Language.TwoParametersLengthIsNotSame );

			OperateResult<byte[]> command = BuildWriteRandomWordCommand( CpuNumber, address, value );
			if (!command.IsSuccess) return command;

			OperateResult<byte[]> read = ReadFromCoreServer( command.Content );
			if (!read.IsSuccess) return read;

			OperateResult<byte[]> check = CheckContent( read.Content );
			if (!check.IsSuccess) return check;

			return OperateResult.CreateSuccessResult( );
		}

		/// <summary>
		/// <b>[商业授权]</b> 随机写入<see cref="short"/>数组信息，主需要出传入<see cref="short"/>数组地址信息，以及对应的<see cref="short"/>数组值<br />
		/// <b>[Authorization]</b> Randomly write the <see cref="short"/> array information, the main need to pass in the <see cref="short"/> array address information, 
		/// and the corresponding <see cref="short"/> array value
		/// </summary>
		/// <param name="address">批量地址信息</param>
		/// <param name="value">批量的数据值信息</param>
		/// <returns>是否写入成功</returns>
		[HslMqttApi( ApiTopic = "WriteRandomInt16", Description = "Randomly write the short array information, the main need to pass in the short array address information" )]
		public OperateResult WriteRandom( string[] address, short[] value ) => WriteRandom( address, ByteTransform.TransByte( value ) );

		/// <summary>
		/// <b>[商业授权]</b> 随机写入<see cref="ushort"/>数组信息，主需要出传入<see cref="ushort"/>数组地址信息，以及对应的<see cref="ushort"/>数组值<br />
		/// <b>[Authorization]</b> Randomly write the <see cref="ushort"/> array information, the main need to pass in the <see cref="ushort"/> array address information, 
		/// and the corresponding <see cref="ushort"/> array value
		/// </summary>
		/// <param name="address">批量地址信息</param>
		/// <param name="value">批量的数据值信息</param>
		/// <returns>是否写入成功</returns>
		[HslMqttApi( ApiTopic = "WriteRandomUInt16", Description = "Randomly write the ushort array information, the main need to pass in the ushort array address information" )]
		public OperateResult WriteRandom( string[] address, ushort[] value ) => WriteRandom( address, ByteTransform.TransByte( value ) );
#if !NET20 && !NET35

#endif
		#endregion

		#region Async Read Write Override
#if !NET20 && !NET35
		/// <inheritdoc/>
		public async override Task<OperateResult<byte[]>> ReadAsync( string address, ushort length )
		{
			OperateResult<List<byte[]>> command;
			if (address.StartsWith( "Special:" ) || address.StartsWith( "special:" ))
			{
				if (!Authorization.asdniasnfaksndiqwhawfskhfaiw( )) return new OperateResult<byte[]>( StringResources.Language.InsufficientPrivileges );
				command = BuildReadSpecialModule( CpuNumber, address, length );
			}
			else
			{
				command = BuildReadCommand( CpuNumber, address, length, false );
			}
			if (!command.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( command );

			List<byte> content = new List<byte>( );
			for (int i = 0; i < command.Content.Count; i++)
			{
				OperateResult<byte[]> read = await ReadFromCoreServerAsync( command.Content[i] );
				if (!read.IsSuccess) return read;

				OperateResult<byte[]> check = CheckContent( read.Content );
				content.AddRange( check.Content );
			}

			return OperateResult.CreateSuccessResult( content.ToArray( ) );
		}

		/// <inheritdoc/>
		public async override Task<OperateResult> WriteAsync( string address, byte[] value )
		{
			OperateResult<byte[]> command;
			if (address.StartsWith( "Special:" ) || address.StartsWith( "special:" ))
			{
				if (!Authorization.asdniasnfaksndiqwhawfskhfaiw( )) return new OperateResult( StringResources.Language.InsufficientPrivileges );
				command = BuildWriteSpecialModule( CpuNumber, address, value );
			}
			else
			{
				command = BuildWriteWordCommand( CpuNumber, address, value );
			}
			if (!command.IsSuccess) return command;

			OperateResult<byte[]> read = await ReadFromCoreServerAsync( command.Content );
			if (!read.IsSuccess) return read;

			return CheckContent( read.Content );
		}

		/// <inheritdoc/>
		public async override Task<OperateResult<bool[]>> ReadBoolAsync( string address, ushort length )
		{
			OperateResult<List<byte[]>> command = BuildReadCommand( CpuNumber, address, length, true );
			if (!command.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( command );

			List<byte> content = new List<byte>( );
			for (int i = 0; i < command.Content.Count; i++)
			{
				OperateResult<byte[]> read = await ReadFromCoreServerAsync( command.Content[i] );
				if (!read.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( read );

				OperateResult<byte[]> check = CheckContent( read.Content );
				if (!check.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( check );

				content.AddRange( check.Content );
			}

			return OperateResult.CreateSuccessResult( content.ToArray( ).Select( m => m != 0x00 ).ToArray( ) );
		}

		/// <inheritdoc/>
		public async override Task<OperateResult> WriteAsync( string address, bool[] value )
		{
			OperateResult<byte[]> command = BuildWriteBoolCommand( CpuNumber, address, value );
			if (!command.IsSuccess) return command;

			OperateResult<byte[]> read = await ReadFromCoreServerAsync( command.Content );
			if (!read.IsSuccess) return read;

			return CheckContent( read.Content );
		}
#endif
		#endregion

		#region Async Read Write Random
#if !NET20 && !NET35
		/// <inheritdoc cref="ReadRandomBool(string[])"/>
		public async Task<OperateResult<bool[]>> ReadRandomBoolAsync( string[] address )
		{
			if (!Authorization.asdniasnfaksndiqwhawfskhfaiw( )) return new OperateResult<bool[]>( StringResources.Language.InsufficientPrivileges );

			OperateResult<List<byte[]>> command = BuildReadRandomCommand( CpuNumber, address, true );
			if (!command.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( command );

			List<bool> lists = new List<bool>( );
			foreach (var content in command.Content)
			{
				OperateResult<byte[]> read = await ReadFromCoreServerAsync( content );
				if (!read.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( read );

				OperateResult<byte[]> check = CheckContent( read.Content );
				if (!check.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( check );

				lists.AddRange( check.Content.Select( m => m != 0x00 ) );
			}
			return OperateResult.CreateSuccessResult( lists.ToArray( ) );
		}

		/// <inheritdoc cref="WriteRandomBool(string[], bool[])"/>
		public async Task<OperateResult> WriteRandomBoolAsync( string[] address, bool[] value )
		{
			if (!Authorization.asdniasnfaksndiqwhawfskhfaiw( )) return new OperateResult<bool[]>( StringResources.Language.InsufficientPrivileges );

			if (address.Length != value.Length) return new OperateResult( StringResources.Language.TwoParametersLengthIsNotSame );

			OperateResult<byte[]> command = BuildWriteRandomBoolCommand( CpuNumber, address, value );
			if (!command.IsSuccess) return command;

			OperateResult<byte[]> read = await ReadFromCoreServerAsync( command.Content );
			if (!read.IsSuccess) return read;

			OperateResult<byte[]> check = CheckContent( read.Content );
			if (!check.IsSuccess) return check;

			return OperateResult.CreateSuccessResult( );
		}

		/// <inheritdoc cref="ReadRandom(string[])"/>
		public async Task<OperateResult<byte[]>> ReadRandomAsync( string[] address )
		{
			if (!Authorization.asdniasnfaksndiqwhawfskhfaiw( )) return new OperateResult<byte[]>( StringResources.Language.InsufficientPrivileges );

			OperateResult<List<byte[]>> command = BuildReadRandomCommand( CpuNumber, address, false );
			if (!command.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( command );

			List<byte> lists = new List<byte>( );
			foreach (var content in command.Content)
			{
				OperateResult<byte[]> read = await ReadFromCoreServerAsync( content );
				if (!read.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( read );

				OperateResult<byte[]> check = CheckContent( read.Content );
				if (!check.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( check );

				lists.AddRange( check.Content );
			}
			return OperateResult.CreateSuccessResult( lists.ToArray( ) );
		}

		/// <inheritdoc cref="ReadRandomInt16(string[])"/>
		public async Task<OperateResult<short[]>> ReadRandomInt16Async( string[] address ) => (await ReadRandomAsync( address )).Then( m => OperateResult.CreateSuccessResult( ByteTransform.TransInt16( m, 0, address.Length ) ) );

		/// <inheritdoc cref="ReadRandomUInt16(string[])"/>
		public async Task<OperateResult<ushort[]>> ReadRandomUInt16Async( string[] address ) => (await ReadRandomAsync( address )).Then( m => OperateResult.CreateSuccessResult( ByteTransform.TransUInt16( m, 0, address.Length ) ) );

		/// <inheritdoc cref="WriteRandom(string[], byte[])"/>
		public async Task<OperateResult> WriteRandomAsync( string[] address, byte[] value )
		{
			if (!Authorization.asdniasnfaksndiqwhawfskhfaiw( )) return new OperateResult<bool[]>( StringResources.Language.InsufficientPrivileges );

			if (address.Length * 2 != value.Length) return new OperateResult( StringResources.Language.TwoParametersLengthIsNotSame );

			OperateResult<byte[]> command = BuildWriteRandomWordCommand( CpuNumber, address, value );
			if (!command.IsSuccess) return command;

			OperateResult<byte[]> read = await ReadFromCoreServerAsync( command.Content );
			if (!read.IsSuccess) return read;

			OperateResult<byte[]> check = CheckContent( read.Content );
			if (!check.IsSuccess) return check;

			return OperateResult.CreateSuccessResult( );
		}

		/// <inheritdoc cref="WriteRandom(string[], short[])"/>
		public async Task<OperateResult> WriteRandomAsync( string[] address, short[] value ) => await WriteRandomAsync( address, ByteTransform.TransByte( value ) );

		/// <inheritdoc cref="WriteRandom(string[], ushort[])"/>
		public async Task<OperateResult> WriteRandomAsync( string[] address, ushort[] value ) => await WriteRandomAsync( address, ByteTransform.TransByte( value ) );
#endif
		#endregion

		#region Advanced Operation

		/// <summary>
		/// <b>[商业授权]</b> 如果未执行程序，则开始执行程序<br />
		/// <b>[Authorization]</b> Starts executing a program if it is not being executed
		/// </summary>
		/// <remarks>
		/// This command will be ignored if it is executed while a program is being executed.<br />
		/// Refer to the users manual for the individual modules for the response formats that are used at error times.
		/// </remarks>
		/// <returns>是否启动成功</returns>
		[HslMqttApi(Description = "Starts executing a program if it is not being executed" )]
		public OperateResult Start( )
		{
			if (!Authorization.asdniasnfaksndiqwhawfskhfaiw( )) return new OperateResult( StringResources.Language.InsufficientPrivileges );

			OperateResult<byte[]> command = BuildStartCommand( CpuNumber );
			if (!command.IsSuccess) return command;

			OperateResult<byte[]> read = ReadFromCoreServer( command.Content );
			if (!read.IsSuccess) return read;

			OperateResult<byte[]> check = CheckContent( read.Content );
			if (!check.IsSuccess) return check;

			return OperateResult.CreateSuccessResult( );
		}

		/// <summary>
		/// <b>[商业授权]</b> 停止当前正在执行程序<br />
		/// <b>[Authorization]</b> Stops the executing program.
		/// </summary>
		/// <remarks>
		/// This command will be ignored if it is executed when no program is being executed.<br />
		/// Refer to the users manual for the individual modules for the response formats that are used at error times.
		/// </remarks>
		/// <returns>是否启动成功</returns>
		[HslMqttApi(Description = "Stops the executing program." )]
		public OperateResult Stop( )
		{
			if (!Authorization.asdniasnfaksndiqwhawfskhfaiw( )) return new OperateResult( StringResources.Language.InsufficientPrivileges );

			OperateResult<byte[]> command = BuildStopCommand( CpuNumber );
			if (!command.IsSuccess) return command;

			OperateResult<byte[]> read = ReadFromCoreServer( command.Content );
			if (!read.IsSuccess) return read;

			OperateResult<byte[]> check = CheckContent( read.Content );
			if (!check.IsSuccess) return check;

			return OperateResult.CreateSuccessResult( );
		}

		/// <summary>
		/// <b>[商业授权]</b> 重置当前的模块，当前打开的连接被强制关闭。 模块中所做的设置也将被清除。然后当前对象需要重连PLC。<br />
		/// <b>[Authorization]</b> When this command is executed via an Ethernet interface module or an Ethernet connection of an F3SP66, F3SP67, 
		/// F3SP71 or F3SP76 sequence CPU module, the connection which is currently open is forced to close. 
		/// The settings made in the modules are also cleared. Then the current object needs to reconnect to the PLC.
		/// </summary>
		/// <returns>是否重置成功</returns>
		[HslMqttApi(Description = "Reset the connection which is currently open is forced to close" )]
		public OperateResult ModuleReset( )
		{
			if (!Authorization.asdniasnfaksndiqwhawfskhfaiw( )) return new OperateResult( StringResources.Language.InsufficientPrivileges );

			OperateResult<byte[]> read = ReadFromCoreServer( new byte[] { 0x61, CpuNumber, 0x00, 0x00 }, false );         // 不需要返回
			if (!read.IsSuccess) return read;

			return OperateResult.CreateSuccessResult( );
		}

		/// <summary>
		/// <b>[商业授权]</b> 读取当前PLC的程序状态，返回1：RUN；2：Stop；3：Debug；255：ROM writer<br />
		/// <b>[Authorization]</b> Read the program status. return code 1:RUN; 2:Stop; 3:Debug; 255:ROM writer
		/// </summary>
		/// <returns>当前PLC的程序状态，返回1：RUN；2：Stop；3：Debug；255：ROM writer</returns>
		[HslMqttApi(Description = "Read the program status. return code 1:RUN; 2:Stop; 3:Debug; 255:ROM writer" )]
		public OperateResult<int> ReadProgramStatus( )
		{
			if (!Authorization.asdniasnfaksndiqwhawfskhfaiw( )) return new OperateResult<int>( StringResources.Language.InsufficientPrivileges );

			OperateResult<byte[]> read = ReadFromCoreServer( new byte[] { 0x62, CpuNumber, 0x00, 0x02, 0x00, 0x01 } );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<int>( read );

			OperateResult<byte[]> check = CheckContent( read.Content );
			if (!check.IsSuccess) return OperateResult.CreateFailedResult<int>( check ); ;

			return OperateResult.CreateSuccessResult( (int)check.Content[1] );
		}

		/// <summary>
		/// <b>[商业授权]</b> 读取当前PLC的系统状态，系统的ID，CPU类型，程序大小信息<br />
		/// <b>[Authorization]</b> Read current PLC system status, system ID, CPU type, program size information
		/// </summary>
		/// <returns>系统信息的结果对象</returns>
		[HslMqttApi( Description = "Read current PLC system status, system ID, CPU type, program size information" )]
		public OperateResult<YokogawaSystemInfo> ReadSystemInfo( )
		{
			if (!Authorization.asdniasnfaksndiqwhawfskhfaiw( )) return new OperateResult<YokogawaSystemInfo>( StringResources.Language.InsufficientPrivileges );

			OperateResult<byte[]> read = ReadFromCoreServer( new byte[] { 0x62, CpuNumber, 0x00, 0x02, 0x00, 0x02 } );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<YokogawaSystemInfo>( read );

			OperateResult<byte[]> check = CheckContent( read.Content );
			if (!check.IsSuccess) return OperateResult.CreateFailedResult<YokogawaSystemInfo>( check ); ;

			return YokogawaSystemInfo.Prase( check.Content );
		}

		/// <summary>
		/// <b>[商业授权]</b> 读取当前PLC的时间信息，包含年月日时分秒<br />
		/// <b>[Authorization]</b> Read current PLC time information, including year, month, day, hour, minute, and second
		/// </summary>
		/// <returns>PLC的当前的时间信息</returns>
		[HslMqttApi( Description = "Read current PLC time information, including year, month, day, hour, minute, and second" )]
		public OperateResult<DateTime> ReadDateTime( )
		{
			if (!Authorization.asdniasnfaksndiqwhawfskhfaiw( )) return new OperateResult<DateTime>( StringResources.Language.InsufficientPrivileges );

			OperateResult<byte[]> read = ReadFromCoreServer( new byte[] { 0x63, CpuNumber, 0x00, 0x00 } );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<DateTime>( read );

			OperateResult<byte[]> check = CheckContent( read.Content );
			if (!check.IsSuccess) return OperateResult.CreateFailedResult<DateTime>( check ); ;

			return OperateResult.CreateSuccessResult( new DateTime(
				2000 + ByteTransform.TransUInt16( check.Content, 0 ),
				ByteTransform.TransUInt16( check.Content, 2 ),
				ByteTransform.TransUInt16( check.Content, 4 ),
				ByteTransform.TransUInt16( check.Content, 6 ),
				ByteTransform.TransUInt16( check.Content, 8 ),
				ByteTransform.TransUInt16( check.Content, 10 ) ) );
		}

		/// <summary>
		/// <b>[商业授权]</b> 读取特殊模块的数据信息，需要指定模块单元号，模块站号，数据地址，长度信息。<br />
		/// <b>[Authorization]</b> To read the data information of a special module, you need to specify the module unit number, module slot number, data address, and length information.
		/// </summary>
		/// <param name="moduleUnit">模块的单元号</param>
		/// <param name="moduleSlot">模块的站号</param>
		/// <param name="dataPosition">模块的数据地址</param>
		/// <param name="length">长度信息</param>
		/// <returns>带有成功标识的byte[]，可以自行解析出所需要的各种类型的数据</returns>
		[HslMqttApi( Description = "Read the data information of a special module, you need to specify the module unit number, module slot number, data address, and length information." )]
		public OperateResult<byte[]> ReadSpecialModule( byte moduleUnit, byte moduleSlot, ushort dataPosition, ushort length )
		{
			if (!Authorization.asdniasnfaksndiqwhawfskhfaiw( )) return new OperateResult<byte[]>( StringResources.Language.InsufficientPrivileges );

			List<byte> content = new List<byte>( );
			List<byte[]> commands = BuildReadSpecialModule( CpuNumber, moduleUnit, moduleSlot, dataPosition, length );

			for (int i = 0; i < commands.Count; i++)
			{
				OperateResult<byte[]> read = ReadFromCoreServer( commands[i] );
				if (!read.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( read );

				OperateResult<byte[]> check = CheckContent( read.Content );
				if (!check.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( check ); ;

				content.AddRange( check.Content );
			}

			return OperateResult.CreateSuccessResult( content.ToArray( ) );
		}
		#endregion

		#region Advanced Operation
#if !NET20 && !NET35
		/// <inheritdoc cref="Start"/>
		public async Task<OperateResult> StartAsync( )
		{
			if (!Authorization.asdniasnfaksndiqwhawfskhfaiw( )) return new OperateResult( StringResources.Language.InsufficientPrivileges );

			OperateResult<byte[]> command = BuildStartCommand( CpuNumber );
			if (!command.IsSuccess) return command;

			OperateResult<byte[]> read = await ReadFromCoreServerAsync( command.Content );
			if (!read.IsSuccess) return read;

			OperateResult<byte[]> check = CheckContent( read.Content );
			if (!check.IsSuccess) return check;

			return OperateResult.CreateSuccessResult( );
		}

		/// <inheritdoc cref="Stop"/>
		public async Task<OperateResult> StopAsync( )
		{
			if (!Authorization.asdniasnfaksndiqwhawfskhfaiw( )) return new OperateResult( StringResources.Language.InsufficientPrivileges );

			OperateResult<byte[]> command = BuildStopCommand( CpuNumber );
			if (!command.IsSuccess) return command;

			OperateResult<byte[]> read = await ReadFromCoreServerAsync( command.Content );
			if (!read.IsSuccess) return read;

			OperateResult<byte[]> check = CheckContent( read.Content );
			if (!check.IsSuccess) return check;

			return OperateResult.CreateSuccessResult( );
		}

		/// <inheritdoc cref="ModuleReset"/>
		public async Task<OperateResult> ModuleResetAsync( )
		{
			if (!Authorization.asdniasnfaksndiqwhawfskhfaiw( )) return new OperateResult( StringResources.Language.InsufficientPrivileges );

			OperateResult<byte[]> read = await ReadFromCoreServerAsync( new byte[] { 0x61, CpuNumber, 0x00, 0x00 }, false );         // 不需要返回
			if (!read.IsSuccess) return read;

			return OperateResult.CreateSuccessResult( );
		}

		/// <inheritdoc cref="ReadProgramStatus"/>
		public async Task<OperateResult<int>> ReadProgramStatusAsync( )
		{
			if (!Authorization.asdniasnfaksndiqwhawfskhfaiw( )) return new OperateResult<int>( StringResources.Language.InsufficientPrivileges );

			OperateResult<byte[]> read = await ReadFromCoreServerAsync( new byte[] { 0x62, CpuNumber, 0x00, 0x02, 0x00, 0x01 } );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<int>( read );

			OperateResult<byte[]> check = CheckContent( read.Content );
			if (!check.IsSuccess) return OperateResult.CreateFailedResult<int>( check ); ;

			return OperateResult.CreateSuccessResult( (int)check.Content[1] );
		}

		/// <inheritdoc cref="ReadSystemInfo"/>
		public async Task<OperateResult<YokogawaSystemInfo>> ReadSystemInfoAsync( )
		{
			if (!Authorization.asdniasnfaksndiqwhawfskhfaiw( )) return new OperateResult<YokogawaSystemInfo>( StringResources.Language.InsufficientPrivileges );

			OperateResult<byte[]> read = await ReadFromCoreServerAsync( new byte[] { 0x62, CpuNumber, 0x00, 0x02, 0x00, 0x02 } );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<YokogawaSystemInfo>( read );

			OperateResult<byte[]> check = CheckContent( read.Content );
			if (!check.IsSuccess) return OperateResult.CreateFailedResult<YokogawaSystemInfo>( check ); ;

			return YokogawaSystemInfo.Prase( check.Content );
		}

		/// <inheritdoc cref="ReadDateTime"/>
		public async Task<OperateResult<DateTime>> ReadDateTimeAsync( )
		{
			if (!Authorization.asdniasnfaksndiqwhawfskhfaiw( )) return new OperateResult<DateTime>( StringResources.Language.InsufficientPrivileges );

			OperateResult<byte[]> read = await ReadFromCoreServerAsync( new byte[] { 0x63, CpuNumber, 0x00, 0x00 } );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<DateTime>( read );

			OperateResult<byte[]> check = CheckContent( read.Content );
			if (!check.IsSuccess) return OperateResult.CreateFailedResult<DateTime>( check ); ;

			return OperateResult.CreateSuccessResult( new DateTime(
				2000 + ByteTransform.TransUInt16( check.Content, 0 ),
				ByteTransform.TransUInt16( check.Content, 2 ),
				ByteTransform.TransUInt16( check.Content, 4 ),
				ByteTransform.TransUInt16( check.Content, 6 ),
				ByteTransform.TransUInt16( check.Content, 8 ),
				ByteTransform.TransUInt16( check.Content, 10 ) ) );
		}

		/// <inheritdoc cref="ReadSpecialModule(byte, byte, ushort, ushort)"/>
		public async Task<OperateResult<byte[]>> ReadSpecialModuleAsync( byte moduleUnit, byte moduleSlot, ushort dataPosition, ushort length )
		{
			if (!Authorization.asdniasnfaksndiqwhawfskhfaiw( )) return new OperateResult<byte[]>( StringResources.Language.InsufficientPrivileges );

			List<byte> content = new List<byte>( );
			List<byte[]> commands = BuildReadSpecialModule( CpuNumber, moduleUnit, moduleSlot, dataPosition, length );

			for (int i = 0; i < commands.Count; i++)
			{
				OperateResult<byte[]> read = await ReadFromCoreServerAsync( commands[i] );
				if (!read.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( read );

				OperateResult<byte[]> check = CheckContent( read.Content );
				if (!check.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( check ); ;

				content.AddRange( check.Content );
			}

			return OperateResult.CreateSuccessResult( content.ToArray( ) );
		}
#endif
		#endregion

		#region Static Helper

		/// <summary>
		/// 检查当前的反馈内容，如果没有发生错误，就解析出实际的数据内容。<br />
		/// Check the current feedback content, if there is no error, parse out the actual data content.
		/// </summary>
		/// <param name="content">原始的数据内容</param>
		/// <returns>解析之后的数据内容</returns>
		public static OperateResult<byte[]> CheckContent( byte[] content )
		{
			if (content[1] != 0x00) return new OperateResult<byte[]>( YokogawaLinkHelper.GetErrorMsg( content[1] ) );
			if (content.Length > 4)
				return OperateResult.CreateSuccessResult( content.RemoveBegin( 4 ) );
			else
				return OperateResult.CreateSuccessResult( new byte[0] );
		}

		/// <summary>
		/// 构建读取命令的原始报文信息
		/// </summary>
		/// <param name="cpu">Cpu Number</param>
		/// <param name="address">地址数据信息</param>
		/// <param name="length">数据长度信息</param>
		/// <param name="isBit">是否位访问</param>
		/// <returns>实际的读取的报文信息</returns>
		public static OperateResult<List<byte[]>> BuildReadCommand( byte cpu, string address, ushort length, bool isBit )
		{
			cpu = (byte)HslHelper.ExtractParameter( ref address, "cpu", cpu );

			OperateResult<YokogawaLinkAddress> analysis = YokogawaLinkAddress.ParseFrom( address, length );
			if (!analysis.IsSuccess) return OperateResult.CreateFailedResult<List<byte[]>>( analysis );

			OperateResult<int[], int[]> splits;
			if (Authorization.asdniasnfaksndiqwhawfskhfaiw( ))
			{
				if (isBit) splits = HslHelper.SplitReadLength( analysis.Content.AddressStart, length, 256 );
				else splits = HslHelper.SplitReadLength( analysis.Content.AddressStart, length, 64 );
			}
			else
			{
				splits = HslHelper.SplitReadLength( analysis.Content.AddressStart, length, ushort.MaxValue );
			}

			List<byte[]> lists = new List<byte[]>( );
			for (int i = 0; i < splits.Content1.Length; i++)
			{
				analysis.Content.AddressStart = splits.Content1[i];

				byte[] buffer = new byte[12];
				buffer[0] = isBit ? (byte)0x01 : (byte)0x11;
				buffer[1] = cpu;
				buffer[2] = 0x00;
				buffer[3] = 0x08;
				analysis.Content.GetAddressBinaryContent( ).CopyTo( buffer, 4 );
				buffer[10] = BitConverter.GetBytes( splits.Content2[i] )[1];
				buffer[11] = BitConverter.GetBytes( splits.Content2[i] )[0];

				lists.Add( buffer );
			}
			return OperateResult.CreateSuccessResult( lists );
		}

		/// <summary>
		/// 构建随机读取的原始报文的初始命令
		/// </summary>
		/// <param name="cpu">Cpu Number</param>
		/// <param name="address">实际的数据地址信息</param>
		/// <param name="isBit">是否是位读取</param>
		/// <returns>实际的读取的报文信息</returns>
		public static OperateResult<List<byte[]>> BuildReadRandomCommand(byte cpu, string[] address, bool isBit )
		{
			List<string[]> splits = SoftBasic.ArraySplitByLength( address, 32 );
			List<byte[]> lists = new List<byte[]>( );

			foreach(string[] addressSplit in splits)
			{
				byte[] buffer = new byte[6 + 6 * addressSplit.Length];
				buffer[0] = isBit ? (byte)0x04 : (byte)0x14;
				buffer[1] = cpu;
				buffer[2] = BitConverter.GetBytes( buffer.Length - 4 )[1];
				buffer[3] = BitConverter.GetBytes( buffer.Length - 4 )[0];
				buffer[4] = BitConverter.GetBytes( addressSplit.Length )[1];
				buffer[5] = BitConverter.GetBytes( addressSplit.Length )[0];

				for (int i = 0; i < addressSplit.Length; i++)
				{
					buffer[1] = (byte)HslHelper.ExtractParameter( ref addressSplit[i], "cpu", cpu );

					OperateResult<YokogawaLinkAddress> analysis = YokogawaLinkAddress.ParseFrom( addressSplit[i], 1 );
					if (!analysis.IsSuccess) return OperateResult.CreateFailedResult<List<byte[]>>( analysis );

					analysis.Content.GetAddressBinaryContent( ).CopyTo( buffer, 6 * i + 6 );
				}

				lists.Add( buffer );
			}
			return OperateResult.CreateSuccessResult( lists );
		}

		/// <summary>
		/// 构建批量写入Bool数组的命令，需要指定CPU Number信息和设备地址信息
		/// </summary>
		/// <param name="cpu">Cpu Number</param>
		/// <param name="address">设备地址数据</param>
		/// <param name="value">实际的bool数组</param>
		/// <returns>构建的写入指令</returns>
		public static OperateResult<byte[]> BuildWriteBoolCommand(byte cpu, string address, bool[] value )
		{
			cpu = (byte)HslHelper.ExtractParameter( ref address, "cpu", cpu );

			OperateResult<YokogawaLinkAddress> analysis = YokogawaLinkAddress.ParseFrom( address, 0 );
			if (!analysis.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( analysis );

			byte[] buffer = new byte[12 + value.Length];
			buffer[0] = 0x02;
			buffer[1] = cpu;
			buffer[2] = 0x00;
			buffer[3] = (byte)(0x08 + value.Length);
			analysis.Content.GetAddressBinaryContent( ).CopyTo( buffer, 4 );
			buffer[10] = BitConverter.GetBytes( value.Length )[1];
			buffer[11] = BitConverter.GetBytes( value.Length )[0];
			for (int i = 0; i < value.Length; i++)
			{
				buffer[12 + i] = value[i] ? (byte)0x01 : (byte)0x00;
			}
			return OperateResult.CreateSuccessResult( buffer );
		}

		/// <summary>
		/// 构建批量随机写入Bool数组的命令，需要指定CPU Number信息和设备地址信息
		/// </summary>
		/// <param name="cpu">Cpu Number</param>
		/// <param name="address">设备地址数据</param>
		/// <param name="value">实际的bool数组</param>
		/// <returns>构建的写入指令</returns>
		public static OperateResult<byte[]> BuildWriteRandomBoolCommand( byte cpu, string[] address, bool[] value )
		{
			if (address.Length != value.Length) return new OperateResult<byte[]>( StringResources.Language.TwoParametersLengthIsNotSame );

			byte[] buffer = new byte[6 + address.Length * 8 - 1];
			buffer[0] = 0x05;
			buffer[1] = cpu;
			buffer[2] = BitConverter.GetBytes( buffer.Length - 4 )[1];
			buffer[3] = BitConverter.GetBytes( buffer.Length - 4 )[0];
			buffer[4] = BitConverter.GetBytes( address.Length )[1];
			buffer[5] = BitConverter.GetBytes( address.Length )[0];

			for (int i = 0; i < address.Length; i++)
			{
				buffer[1] = (byte)HslHelper.ExtractParameter( ref address[i], "cpu", cpu );

				OperateResult<YokogawaLinkAddress> analysis = YokogawaLinkAddress.ParseFrom( address[i], 0 );
				if (!analysis.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( analysis );

				analysis.Content.GetAddressBinaryContent( ).CopyTo( buffer, 6 + 8 * i );
				buffer[12 + 8 * i] = value[i] ? (byte)0x01 : (byte)0x00;
			}
			return OperateResult.CreateSuccessResult( buffer );
		}

		/// <summary>
		/// 构建字写入的命令报文信息，需要指定设备地址
		/// </summary>
		/// <param name="cpu">Cpu Number</param>
		/// <param name="address">地址</param>
		/// <param name="value">原始的数据值</param>
		/// <returns>原始的报文命令</returns>
		public static OperateResult<byte[]> BuildWriteWordCommand( byte cpu, string address, byte[] value )
		{
			cpu = (byte)HslHelper.ExtractParameter( ref address, "cpu", cpu );

			OperateResult<YokogawaLinkAddress> analysis = YokogawaLinkAddress.ParseFrom( address, 0 );
			if (!analysis.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( analysis );

			byte[] buffer = new byte[12 + value.Length];
			buffer[0] = 0x12;
			buffer[1] = cpu;
			buffer[2] = 0x00;
			buffer[3] = (byte)(0x08 + value.Length);
			analysis.Content.GetAddressBinaryContent( ).CopyTo( buffer, 4 );
			buffer[10] = BitConverter.GetBytes( value.Length / 2 )[1];
			buffer[11] = BitConverter.GetBytes( value.Length / 2 )[0];
			value.CopyTo( buffer, 12 );
			return OperateResult.CreateSuccessResult( buffer );
		}

		/// <summary>
		/// 构建随机写入字的命令的报文
		/// </summary>
		/// <param name="cpu">Cpu Number</param>
		/// <param name="address">地址</param>
		/// <param name="value">原始的数据值</param>
		/// <returns>原始的报文命令</returns>
		public static OperateResult<byte[]> BuildWriteRandomWordCommand( byte cpu, string[] address, byte[] value )
		{
			if (address.Length * 2 != value.Length) return new OperateResult<byte[]>( StringResources.Language.TwoParametersLengthIsNotSame );

			byte[] buffer = new byte[6 + address.Length * 8];
			buffer[0] = 0x15;
			buffer[1] = cpu;
			buffer[2] = BitConverter.GetBytes( buffer.Length - 4 )[1];
			buffer[3] = BitConverter.GetBytes( buffer.Length - 4 )[0];
			buffer[4] = BitConverter.GetBytes( address.Length )[1];
			buffer[5] = BitConverter.GetBytes( address.Length )[0];

			for (int i = 0; i < address.Length; i++)
			{
				buffer[1] = (byte)HslHelper.ExtractParameter( ref address[i], "cpu", cpu );

				OperateResult<YokogawaLinkAddress> analysis = YokogawaLinkAddress.ParseFrom( address[i], 0 );
				if (!analysis.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( analysis );

				analysis.Content.GetAddressBinaryContent( ).CopyTo( buffer, 6 + 8 * i );
				buffer[12 + 8 * i] = value[i * 2 + 0];
				buffer[13 + 8 * i] = value[i * 2 + 1];
			}
			return OperateResult.CreateSuccessResult( buffer );
		}

		/// <summary>
		/// 构建启动PLC的命令报文
		/// </summary>
		/// <param name="cpu">Cpu Number</param>
		/// <returns>原始的报文命令</returns>
		public static OperateResult<byte[]> BuildStartCommand( byte cpu )
		{
			return OperateResult.CreateSuccessResult( new byte[] { 0x45, cpu, 0x00, 0x00 } );
		}

		/// <summary>
		/// 构建停止PLC的命令报文
		/// </summary>
		/// <param name="cpu">Cpu Number</param>
		/// <returns>原始的报文命令</returns>
		public static OperateResult<byte[]> BuildStopCommand( byte cpu )
		{
			return OperateResult.CreateSuccessResult( new byte[] { 0x46, cpu, 0x00, 0x00 } );
		}

		/// <summary>
		/// 构建读取特殊模块的命令报文
		/// </summary>
		/// <param name="cpu">Cpu Number</param>
		/// <param name="moduleUnit">模块单元号</param>
		/// <param name="moduleSlot">模块站号</param>
		/// <param name="dataPosition">数据位置</param>
		/// <param name="length">长度信息</param>
		/// <returns>原始的报文命令</returns>
		public static List<byte[]> BuildReadSpecialModule( byte cpu, byte moduleUnit, byte moduleSlot, ushort dataPosition, ushort length )
		{
			List<byte[]> lists = new List<byte[]>( );
			OperateResult<int[], int[]> splits = HslHelper.SplitReadLength( dataPosition, length, 64 );
			for (int i = 0; i < splits.Content1.Length; i++)
			{
				// length 的基本单位为64
				byte[] buffer = new byte[10];
				buffer[0] = 0x31;
				buffer[1] = cpu;
				buffer[2] = BitConverter.GetBytes( buffer.Length - 4 )[1];
				buffer[3] = BitConverter.GetBytes( buffer.Length - 4 )[0];
				buffer[4] = moduleUnit;
				buffer[5] = moduleSlot;
				buffer[6] = BitConverter.GetBytes( splits.Content1[i] )[1];
				buffer[7] = BitConverter.GetBytes( splits.Content1[i] )[0];
				buffer[8] = BitConverter.GetBytes( splits.Content2[i] )[1];
				buffer[9] = BitConverter.GetBytes( splits.Content2[i] )[0];

				lists.Add( buffer );
			}
			return lists;
		}

		/// <summary>
		/// 构建读取特殊模块的命令报文，需要传入高级地址，必须以 <b>Special:</b> 开头表示特殊模块地址，示例：Special:cpu=1;unit=0;slot=1;100<br />
		/// To construct a command message to read a special module, the advanced address needs to be passed in. 
		/// It must start with <b>Special:</b> to indicate the address of the special module, for example: Special:cpu=1;unit=0;slot=1;100
		/// </summary>
		/// <param name="cpu">Cpu Number</param>
		/// <param name="address">高级的混合地址，除了Cpu可以不携带，例如：Special:unit=0;slot=1;100</param>
		/// <param name="length">长度信息</param>
		/// <returns>原始的报文命令</returns>
		public static OperateResult<List<byte[]>> BuildReadSpecialModule( byte cpu, string address, ushort length )
		{
			if (address.StartsWith( "Special:" ) || address.StartsWith( "special:" ))
			{
				address = address.Substring( 8 );
				cpu = (byte)HslHelper.ExtractParameter( ref address, "cpu", cpu );

				OperateResult<int> unit = HslHelper.ExtractParameter( ref address, "unit" );
				if(!unit.IsSuccess) return OperateResult.CreateFailedResult<List<byte[]>>( unit );

				OperateResult<int> slot = HslHelper.ExtractParameter( ref address, "slot" );
				if (!slot.IsSuccess) return OperateResult.CreateFailedResult<List<byte[]>>( slot );

				try
				{
					return OperateResult.CreateSuccessResult( BuildReadSpecialModule( cpu, (byte)unit.Content, (byte)slot.Content, ushort.Parse( address ), length ) );
				}
				catch(Exception ex)
				{
					return new OperateResult<List<byte[]>>( "Address format wrong: " + ex.Message );
				}
			}
			else
			{
				return new OperateResult<List<byte[]>>( "Special module address must start with Special:" );
			}
		}

		/// <summary>
		/// 构建读取特殊模块的命令报文
		/// </summary>
		/// <param name="cpu">Cpu Number</param>
		/// <param name="moduleUnit">模块单元号</param>
		/// <param name="moduleSlot">模块站号</param>
		/// <param name="dataPosition">数据位置</param>
		/// <param name="data">数据内容</param>
		/// <returns>原始的报文命令</returns>
		public static byte[] BuildWriteSpecialModule( byte cpu, byte moduleUnit, byte moduleSlot, ushort dataPosition, byte[] data )
		{
			// length 的基本单位为64
			byte[] buffer = new byte[10 + data.Length];
			buffer[0] = 0x32;
			buffer[1] = cpu;
			buffer[2] = BitConverter.GetBytes( buffer.Length - 4 )[1];
			buffer[3] = BitConverter.GetBytes( buffer.Length - 4 )[0];
			buffer[4] = moduleUnit;
			buffer[5] = moduleSlot;
			buffer[6] = BitConverter.GetBytes( dataPosition )[1];
			buffer[7] = BitConverter.GetBytes( dataPosition )[0];
			buffer[8] = BitConverter.GetBytes( data.Length / 2 )[1];
			buffer[9] = BitConverter.GetBytes( data.Length / 2 )[0];
			data.CopyTo( buffer, 10 );
			return buffer;
		}

		/// <summary>
		/// 构建写入特殊模块的命令报文，需要传入高级地址，必须以 <b>Special:</b> 开头表示特殊模块地址，示例：Special:cpu=1;unit=0;slot=1;100<br />
		/// To construct a command message to write a special module, the advanced address needs to be passed in. 
		/// It must start with <b>Special:</b> to indicate the address of the special module, for example: Special:cpu=1;unit=0;slot=1;100
		/// </summary>
		/// <param name="cpu">Cpu Number</param>
		/// <param name="address">高级的混合地址，除了Cpu可以不携带，例如：Special:unit=0;slot=1;100</param>
		/// <param name="data">写入的原始数据内容</param>
		/// <returns>原始的报文命令</returns>
		public static OperateResult<byte[]> BuildWriteSpecialModule( byte cpu, string address, byte[] data )
		{
			OperateResult<List<byte[]>> analysis = BuildReadSpecialModule( cpu, address, 0 );
			if (!analysis.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( analysis );

			byte[] buffer = new byte[10 + data.Length];
			buffer[0] = 0x32;
			buffer[1] = analysis.Content[0][1];
			buffer[2] = BitConverter.GetBytes( buffer.Length - 4 )[1];
			buffer[3] = BitConverter.GetBytes( buffer.Length - 4 )[0];
			buffer[4] = analysis.Content[0][4];
			buffer[5] = analysis.Content[0][5];
			buffer[6] = analysis.Content[0][6];
			buffer[7] = analysis.Content[0][7];
			buffer[8] = BitConverter.GetBytes( data.Length / 2 )[1];
			buffer[9] = BitConverter.GetBytes( data.Length / 2 )[0];
			data.CopyTo( buffer, 10 );
			return OperateResult.CreateSuccessResult( buffer );
		}

		#endregion

		/// <inheritdoc/>
		public override string ToString( ) => $"YokogawaLinkTcp[{IpAddress}:{Port}]";
	}
}
