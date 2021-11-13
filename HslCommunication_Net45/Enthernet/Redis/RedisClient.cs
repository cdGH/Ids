using HslCommunication.Core;
using HslCommunication.Core.IMessage;
using HslCommunication.Core.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.IO;
using HslCommunication.BasicFramework;
using HslCommunication.Reflection;
using System.Security.AccessControl;
using System.Security.Cryptography;
using HslCommunication.Profinet.Panasonic;
#if !NET35 && !NET20
using System.Threading.Tasks;
#endif

namespace HslCommunication.Enthernet.Redis
{
	/// <summary>
	/// 这是一个redis的客户端类，支持读取，写入，发布订阅，但是不支持订阅，如果需要订阅，请使用另一个类<see cref="RedisSubscribe"/>
	/// </summary>
	/// <remarks>
	/// 本类库的API指令的参考及注释来源：http://doc.redisfans.com/index.html
	/// </remarks>
	/// <example>
	/// 基本的操作如下所示，举例了几个比较常见的指令，更多的需要参考api接口描述
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Enthernet\RedisSample.cs" region="SampleBasic" title="基本操作代码" />
	/// 如下是基于特性的操作，有必要说明以下：
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Enthernet\RedisSample.cs" region="Sample1" title="基础的使用" />
	/// 总的来说，当读取的数据种类比较多的时候，读取的关键字比较多的时候，处理起来就比较的麻烦，此处推荐一个全新的写法，为了更好的对比，我们假设实现一种需求
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Enthernet\RedisSample.cs" region="Sample2" title="同等代码" />
	/// 为此我们只需要实现一个特性类即可。代码如下：(注意，实际是很灵活的，类型都是自动转换的)
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Enthernet\RedisSample.cs" region="SampleClass" title="数据类" />
	/// </example>
	public class RedisClient : NetworkDoubleBase
	{
		#region Constructor

		/// <summary>
		/// 实例化一个客户端的对象，用于和服务器通信
		/// </summary>
		/// <param name="ipAddress">服务器的ip地址</param>
		/// <param name="port">服务器的端口号</param>
		/// <param name="password">密码，如果服务器没有设置，密码设置为null</param>
		public RedisClient( string ipAddress, int port, string password )
		{
			ByteTransform  = new RegularByteTransform( );
			IpAddress      = ipAddress;
			Port           = port;
			ReceiveTimeOut = 30000;
			this.password  = password;
#if !NET20 && !NET35
			redisSubscribe = new Lazy<RedisSubscribe>( ( ) => RedisSubscribeInitialize( ) );
#endif
		}

		/// <summary>
		/// 实例化一个客户端对象，需要手动指定Ip地址和端口
		/// </summary>
		/// <param name="password">密码，如果服务器没有设置，密码设置为null</param>
		public RedisClient( string password )
		{
			ByteTransform  = new RegularByteTransform( );
			ReceiveTimeOut = 30000;
			this.password  = password;
#if !NET20 && !NET35
			redisSubscribe = new Lazy<RedisSubscribe>( ( ) => RedisSubscribeInitialize( ) );
#endif
		}

		#endregion

		#region Override InitializationOnConnect

		/// <inheritdoc/>
		protected override OperateResult InitializationOnConnect( Socket socket )
		{
			if (!string.IsNullOrEmpty( this.password ))
			{
				// 连接到服务器之后第一步先验证密码是否正确
				byte[] command = RedisHelper.PackStringCommand( new string[] { "AUTH", this.password } );

				OperateResult<byte[]> read = ReadFromCoreServer(socket,  command );
				if (!read.IsSuccess) return OperateResult.CreateFailedResult<string>( read );

				string msg = Encoding.UTF8.GetString( read.Content );
				if (!msg.StartsWith( "+" )) return new OperateResult<string>( msg );
			}

			// 如果设置了db块信息，那么就修改db块内容，也即是修复短连接时切换db块无效的bug。
			if (dbBlock > 0)
			{
				byte[] command = RedisHelper.PackStringCommand( new string[] { "SELECT", dbBlock.ToString( ) } );

				OperateResult<byte[]> read = ReadFromCoreServer( socket, command );
				if (!read.IsSuccess) return OperateResult.CreateFailedResult<string>( read );

				string msg = Encoding.UTF8.GetString( read.Content );
				if (!msg.StartsWith( "+" )) return new OperateResult<string>( msg );
			}

			return base.InitializationOnConnect( socket );
		}

		/// <inheritdoc/>
		public override OperateResult<byte[]> ReadFromCoreServer( Socket socket, byte[] send, bool hasResponseData = true, bool usePackHeader = true )
		{
			OperateResult sendResult = Send( socket, send );
			if (!sendResult.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( sendResult );

			// 接收超时时间大于0时才允许接收远程的数据
			if (ReceiveTimeOut < 0) return OperateResult.CreateSuccessResult( new byte[0] );

			// 接收数据信息
			return ReceiveRedisCommand( socket );
		}
#if !NET35 && !NET20
		/// <inheritdoc/>
		protected async override Task<OperateResult> InitializationOnConnectAsync( Socket socket )
		{
			if (!string.IsNullOrEmpty( this.password ))
			{
				// 连接到服务器之后第一步先验证密码是否正确
				byte[] command = RedisHelper.PackStringCommand( new string[] { "AUTH", this.password } );

				OperateResult<byte[]> read = await ReadFromCoreServerAsync( socket, command );
				if (!read.IsSuccess) return OperateResult.CreateFailedResult<string>( read );

				string msg = Encoding.UTF8.GetString( read.Content );
				if (!msg.StartsWith( "+" )) return new OperateResult<string>( msg );
			}

			// 如果设置了db块信息，那么就修改db块内容，也即是修复短连接时切换db块无效的bug。
			if (dbBlock > 0)
			{
				byte[] command = RedisHelper.PackStringCommand( new string[] { "SELECT", dbBlock.ToString( ) } );

				OperateResult<byte[]> read = await ReadFromCoreServerAsync( socket, command );
				if (!read.IsSuccess) return OperateResult.CreateFailedResult<string>( read );

				string msg = Encoding.UTF8.GetString( read.Content );
				if (!msg.StartsWith( "+" )) return new OperateResult<string>( msg );
			}

			return base.InitializationOnConnect( socket );
		}
		/// <inheritdoc/>
		public async override Task<OperateResult<byte[]>> ReadFromCoreServerAsync( Socket socket, byte[] send, bool hasResponseData = true, bool usePackHeader = true )
		{
			OperateResult sendResult = await SendAsync( socket, send );
			if (!sendResult.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( sendResult );

			// 接收超时时间大于0时才允许接收远程的数据
			if (ReceiveTimeOut < 0) return OperateResult.CreateSuccessResult( new byte[0] );

			// 接收数据信息
			return await ReceiveRedisCommandAsync( socket );
		}
#endif
		#endregion

		#region Customer

		/// <summary>
		/// 自定义的指令交互方法，该指令用空格分割，举例：LTRIM AAAAA 0 999 就是收缩列表，GET AAA 就是获取键值，需要对返回的数据进行二次分析
		/// </summary>
		/// <param name="command">举例：LTRIM AAAAA 0 999 就是收缩列表，GET AAA 就是获取键值</param>
		/// <returns>从服务器返回的结果数据对象</returns>
		public OperateResult<string> ReadCustomer( string command )
		{
			byte[] byteCommand = RedisHelper.PackStringCommand( command.Split( ' ' ) );

			OperateResult<byte[]> read = ReadFromCoreServer( byteCommand );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<string>( read );

			return OperateResult.CreateSuccessResult( Encoding.UTF8.GetString( read.Content ) );
		}
#if !NET35 && !NET20
		/// <inheritdoc cref="ReadCustomer(string)"/>
		public async Task<OperateResult<string>> ReadCustomerAsync( string command )
		{
			byte[] byteCommand = RedisHelper.PackStringCommand( command.Split( ' ' ) );

			OperateResult<byte[]> read = await ReadFromCoreServerAsync( byteCommand );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<string>( read );

			return OperateResult.CreateSuccessResult( Encoding.UTF8.GetString( read.Content ) );
		}
#endif
		#endregion

		#region Base Operate

		/// <summary>
		/// 向服务器请求指定，并返回数字的结果对象
		/// </summary>
		/// <param name="commands">命令数组</param>
		/// <returns>数字的结果对象</returns>
		public OperateResult<int> OperateNumberFromServer(string[] commands)
		{
			byte[] command = RedisHelper.PackStringCommand( commands );

			OperateResult<byte[]> read = ReadFromCoreServer( command );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<int>( read );

			string msg = Encoding.UTF8.GetString( read.Content );
			if (!msg.StartsWith( ":" )) return new OperateResult<int>( msg );

			return RedisHelper.GetNumberFromCommandLine( read.Content );
		}

		/// <summary>
		/// 向服务器请求指令，并返回long数字的结果对象
		/// </summary>
		/// <param name="commands">命令数组</param>
		/// <returns>long数字的结果对象</returns>
		public OperateResult<long> OperateLongNumberFromServer(string[] commands)
		{
			byte[] command = RedisHelper.PackStringCommand( commands );

			OperateResult<byte[]> read = ReadFromCoreServer( command );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<long>( read );

			string msg = Encoding.UTF8.GetString( read.Content );
			if (!msg.StartsWith( ":" )) return new OperateResult<long>( msg );

			return RedisHelper.GetLongNumberFromCommandLine( read.Content );
		}

		/// <summary>
		/// 向服务器请求指令，并返回字符串的结果对象
		/// </summary>
		/// <param name="commands">命令数组</param>
		/// <returns>字符串的结果对象</returns>
		public OperateResult<string> OperateStringFromServer(string[] commands)
		{
			byte[] command = RedisHelper.PackStringCommand( commands );

			OperateResult<byte[]> read = ReadFromCoreServer( command );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<string>( read );

			return RedisHelper.GetStringFromCommandLine( read.Content );
		}

		/// <summary>
		/// 向服务器请求指令，并返回字符串数组的结果对象
		/// </summary>
		/// <param name="commands">命令数组</param>
		/// <returns>字符串数组的结果对象</returns>
		public OperateResult<string[]> OperateStringsFromServer(string[] commands)
		{
			byte[] command = RedisHelper.PackStringCommand( commands );

			OperateResult<byte[]> read = ReadFromCoreServer( command );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<string[]>( read );

			return RedisHelper.GetStringsFromCommandLine( read.Content );
		}

		/// <summary>
		/// 向服务器请求指令，并返回状态的结果对象，通常用于写入的判断，或是请求类型的判断
		/// </summary>
		/// <param name="commands">命令数组</param>
		/// <returns>是否成功的结果对象</returns>
		public OperateResult<string> OperateStatusFromServer(string[] commands)
		{
			byte[] command = RedisHelper.PackStringCommand( commands );

			OperateResult<byte[]> read = ReadFromCoreServer( command );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<string>( read );

			string msg = Encoding.UTF8.GetString( read.Content );
			if (!msg.StartsWith( "+" )) return new OperateResult<string>( msg );

			return OperateResult.CreateSuccessResult( msg.Substring( 1 ).TrimEnd( '\r', '\n' ) );
		}
#if !NET35 && !NET20
		/// <inheritdoc cref="OperateNumberFromServer(string[])"/>
		public async Task<OperateResult<int>> OperateNumberFromServerAsync( string[] commands )
		{
			byte[] command = RedisHelper.PackStringCommand( commands );

			OperateResult<byte[]> read = await ReadFromCoreServerAsync( command );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<int>( read );

			string msg = Encoding.UTF8.GetString( read.Content );
			if (!msg.StartsWith( ":" )) return new OperateResult<int>( msg );

			return RedisHelper.GetNumberFromCommandLine( read.Content );
		}

		/// <inheritdoc cref="OperateLongNumberFromServer(string[])"/>
		public async Task<OperateResult<long>> OperateLongNumberFromServerAsync( string[] commands )
		{
			byte[] command = RedisHelper.PackStringCommand( commands );

			OperateResult<byte[]> read = await ReadFromCoreServerAsync( command );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<long>( read );

			string msg = Encoding.UTF8.GetString( read.Content );
			if (!msg.StartsWith( ":" )) return new OperateResult<long>( msg );

			return RedisHelper.GetLongNumberFromCommandLine( read.Content );
		}

		/// <inheritdoc cref="OperateStringFromServer(string[])"/>
		public async Task<OperateResult<string>> OperateStringFromServerAsync( string[] commands )
		{
			byte[] command = RedisHelper.PackStringCommand( commands );

			OperateResult<byte[]> read = await ReadFromCoreServerAsync( command );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<string>( read );

			return RedisHelper.GetStringFromCommandLine( read.Content );
		}

		/// <inheritdoc cref="OperateStringsFromServer(string[])"/>
		public async Task<OperateResult<string[]>> OperateStringsFromServerAsync( string[] commands )
		{
			byte[] command = RedisHelper.PackStringCommand( commands );

			OperateResult<byte[]> read = await ReadFromCoreServerAsync( command );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<string[]>( read );

			return RedisHelper.GetStringsFromCommandLine( read.Content );
		}

		/// <inheritdoc cref="OperateStatusFromServer(string[])"/>
		public async Task<OperateResult<string>> OperateStatusFromServerAsync( string[] commands )
		{
			byte[] command = RedisHelper.PackStringCommand( commands );

			OperateResult<byte[]> read = await ReadFromCoreServerAsync( command );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<string>( read );

			string msg = Encoding.UTF8.GetString( read.Content );
			if (!msg.StartsWith( "+" )) return new OperateResult<string>( msg );

			return OperateResult.CreateSuccessResult( msg.Substring( 1 ).TrimEnd( '\r', '\n' ) );
		}
#endif
		#endregion

		#region Key Operate

		/// <summary>
		/// 删除给定的一个或多个 key 。不存在的 key 会被忽略。
		/// </summary>
		/// <param name="keys">关键字</param>
		/// <returns>被删除 key 的数量。</returns>
		public OperateResult<int> DeleteKey( string[] keys ) => OperateNumberFromServer( SoftBasic.SpliceStringArray( "DEL", keys ) );

		/// <summary>
		/// 删除给定的一个或多个 key 。不存在的 key 会被忽略。
		/// </summary>
		/// <param name="key">关键字</param>
		/// <returns>被删除 key 的数量。</returns>
		public OperateResult<int> DeleteKey( string key ) => DeleteKey( new string[] { key } );

		/// <summary>
		/// 检查给定 key 是否存在。若 key 存在，返回 1 ，否则返回 0 。
		/// </summary>
		/// <param name="key">关键字</param>
		/// <returns>若 key 存在，返回 1 ，否则返回 0 。</returns>
		public OperateResult<int> ExistsKey( string key ) => OperateNumberFromServer( new string[] { "EXISTS", key } );

		/// <summary>
		/// 为给定 key 设置生存时间，当 key 过期时(生存时间为 0 )，它会被自动删除。设置成功返回 1 。当 key 不存在或者不能为 key 设置生存时间时，返回 0 。
		/// </summary>
		/// <param name="key">关键字</param>
		/// <param name="seconds">当前key的生存时间，单位为秒</param>
		/// <returns>
		/// 设置成功返回 1 。当 key 不存在或者不能为 key 设置生存时间时，返回 0 。
		/// </returns>
		/// <remarks>
		/// 在 Redis 中，带有生存时间的 key 被称为『易失的』(volatile)。<br />
		/// 生存时间可以通过使用 DEL 命令来删除整个 key 来移除，或者被 SET 和 GETSET 命令覆写( overwrite)，这意味着，如果一个命令只是修改( alter)一个带生存时间的 key 的值而不是用一个新的 key 值来代替( replace)它的话，那么生存时间不会被改变。<br />
		/// 比如说，对一个 key 执行 INCR 命令，对一个列表进行 LPUSH 命令，或者对一个哈希表执行 HSET 命令，这类操作都不会修改 key 本身的生存时间。<br />
		/// 另一方面，如果使用 RENAME 对一个 key 进行改名，那么改名后的 key 的生存时间和改名前一样。<br />
		/// RENAME 命令的另一种可能是，尝试将一个带生存时间的 key 改名成另一个带生存时间的 another_key ，这时旧的 another_key( 以及它的生存时间)会被删除，然后旧的 key 会改名为 another_key ，因此，新的 another_key 的生存时间也和原本的 key 一样。<br />
		/// 使用 PERSIST 命令可以在不删除 key 的情况下，移除 key 的生存时间，让 key 重新成为一个『持久的』(persistent) key 。<br />
		/// 更新生存时间<br />
		/// 可以对一个已经带有生存时间的 key 执行 EXPIRE 命令，新指定的生存时间会取代旧的生存时间。<br />
		/// 过期时间的精确度<br />
		/// 在 Redis 2.4 版本中，过期时间的延迟在 1 秒钟之内 —— 也即是，就算 key 已经过期，但它还是可能在过期之后一秒钟之内被访问到，而在新的 Redis 2.6 版本中，延迟被降低到 1 毫秒之内。<br />
		/// Redis 2.1.3 之前的不同之处<br />
		/// 在 Redis 2.1.3 之前的版本中，修改一个带有生存时间的 key 会导致整个 key 被删除，这一行为是受当时复制( replication)层的限制而作出的，现在这一限制已经被修复。<br />
		/// </remarks>
		public OperateResult<int> ExpireKey( string key, int seconds ) => OperateNumberFromServer( new string[] { "EXPIRE", key, seconds.ToString( ) } );

		/// <summary>
		/// 查找所有符合给定模式 pattern 的 key 。
		/// * 匹配数据库中所有 key。
		/// h?llo 匹配 hello ， hallo 和 hxllo 等。
		/// h[ae]llo 匹配 hello 和 hallo ，但不匹配 hillo 。
		/// </summary>
		/// <param name="pattern">给定模式</param>
		/// <returns>符合给定模式的 key 列表。</returns>
		public OperateResult<string[]> ReadAllKeys( string pattern ) => OperateStringsFromServer( new string[] { "KEYS", pattern } );

		/// <summary>
		/// 将当前数据库的 key 移动到给定的数据库 db 当中。
		/// 如果当前数据库(源数据库)和给定数据库(目标数据库)有相同名字的给定 key ，或者 key 不存在于当前数据库，那么 MOVE 没有任何效果。
		/// 因此，也可以利用这一特性，将 MOVE 当作锁(locking)原语(primitive)。
		/// </summary>
		/// <param name="key">关键字</param>
		/// <param name="db">数据块</param>
		/// <returns>是否移动成功</returns>
		public OperateResult MoveKey( string key, int db ) => OperateStatusFromServer( new string[] { "MOVE", key, db.ToString( ) } );

		/// <summary>
		/// 移除给定 key 的生存时间，将这个 key 从『易失的』(带生存时间 key )转换成『持久的』(一个不带生存时间、永不过期的 key )。
		/// 当生存时间移除成功时，返回 1 .
		/// 如果 key 不存在或 key 没有设置生存时间，返回 0 。
		/// </summary>
		/// <param name="key">关键字</param>
		/// <returns>
		/// 当生存时间移除成功时，返回 1 .
		/// 如果 key 不存在或 key 没有设置生存时间，返回 0 。
		/// </returns>
		public OperateResult<int> PersistKey( string key ) => OperateNumberFromServer( new string[] { "PERSIST", key } );

		/// <summary>
		/// 从当前数据库中随机返回(不删除)一个 key 。
		/// 当数据库不为空时，返回一个 key 。
		/// 当数据库为空时，返回 nil 。
		/// </summary>
		/// <returns>
		/// 当数据库不为空时，返回一个 key 。
		/// 当数据库为空时，返回 nil 。
		/// </returns>
		public OperateResult<string> ReadRandomKey( ) => OperateStringFromServer( new string[] { "RANDOMKEY" } );

		/// <summary>
		/// 将 key 改名为 newkey 。
		/// 当 key 和 newkey 相同，或者 key 不存在时，返回一个错误。
		/// 当 newkey 已经存在时， RENAME 命令将覆盖旧值。
		/// </summary>
		/// <param name="key1">旧的key</param>
		/// <param name="key2">新的key</param>
		/// <returns>
		/// 改名成功时提示 OK ，失败时候返回一个错误。
		/// </returns>
		public OperateResult RenameKey( string key1, string key2 ) => OperateStatusFromServer( new string[] { "RENAME", key1, key2 } );

		/// <summary>
		/// 返回 key 所储存的值的类型。none (key不存在)，string (字符串)，list (列表)，set (集合)，zset (有序集)，hash (哈希表)
		/// </summary>
		/// <param name="key">关键字</param>
		/// <returns>类型</returns>
		public OperateResult<string> ReadKeyType( string key ) => OperateStatusFromServer( new string[] { "TYPE", key } );

		/// <summary>
		/// 以秒为单位，返回给定 key 的剩余生存时间(TTL, time to live)。
		/// </summary>
		/// <param name="key">关键字</param>
		/// <returns>当 key 不存在时，返回 -2 。当 key 存在但没有设置剩余生存时间时，返回 -1 。否则，以秒为单位，返回 key 的剩余生存时间。</returns>
		public OperateResult<int> ReadKeyTTL(string key ) => OperateNumberFromServer( new string[] { "TTL", key } );

		#endregion

		#region Async Key Operate
#if !NET35 && !NET20
		/// <inheritdoc cref="DeleteKey(string[])"/>
		public async Task<OperateResult<int>> DeleteKeyAsync( string[] keys ) => await OperateNumberFromServerAsync( SoftBasic.SpliceStringArray( "DEL", keys ) );

		/// <inheritdoc cref="DeleteKey(string)"/>
		public async Task<OperateResult<int>> DeleteKeyAsync( string key ) => await DeleteKeyAsync( new string[] { key } );

		/// <inheritdoc cref="ExistsKey(string)"/>
		public async Task<OperateResult<int>> ExistsKeyAsync( string key ) => await OperateNumberFromServerAsync( new string[] { "EXISTS", key } );

		/// <inheritdoc cref="ExpireKey(string,int)"/>
		public async Task<OperateResult<int>> ExpireKeyAsync( string key, int seconds ) => await OperateNumberFromServerAsync( new string[] { "EXPIRE", key, seconds.ToString( ) } );

		/// <inheritdoc cref="ReadAllKeys(string)"/>
		public async Task<OperateResult<string[]>> ReadAllKeysAsync( string pattern ) => await OperateStringsFromServerAsync( new string[] { "KEYS", pattern } );

		/// <inheritdoc cref="MoveKey(string, int)"/>
		public async Task<OperateResult> MoveKeyAsync( string key, int db ) => await OperateStatusFromServerAsync( new string[] { "MOVE", key, db.ToString( ) } );

		/// <inheritdoc cref="PersistKey(string)"/>
		public async Task<OperateResult<int>> PersistKeyAsync( string key ) => await OperateNumberFromServerAsync( new string[] { "PERSIST", key } );

		/// <inheritdoc cref="ReadRandomKey"/>
		public async Task<OperateResult<string>> ReadRandomKeyAsync( ) => await OperateStringFromServerAsync( new string[] { "RANDOMKEY" } );

		/// <inheritdoc cref="RenameKey(string, string)"/>
		public async Task<OperateResult> RenameKeyAsync( string key1, string key2 ) => await OperateStatusFromServerAsync( new string[] { "RENAME", key1, key2 } );

		///<inheritdoc cref="ReadKeyType(string)"/>
		public async Task<OperateResult<string>> ReadKeyTypeAsync( string key ) => await OperateStatusFromServerAsync( new string[] { "TYPE", key } );

		/// <inheritdoc cref="ReadKeyTTL(string)"/>
		public async Task<OperateResult<int>> ReadKeyTTLAsync( string key ) => await OperateNumberFromServerAsync( new string[] { "TTL", key } );

#endif
		#endregion

		#region String Operate

		/// <summary>
		/// 如果 key 已经存在并且是一个字符串， APPEND 命令将 value 追加到 key 原来的值的末尾。
		/// 如果 key 不存在， APPEND 就简单地将给定 key 设为 value ，就像执行 SET key value 一样。
		/// 返回追加 value 之后， key 中字符串的长度。
		/// </summary>
		/// <param name="key">关键字</param>
		/// <param name="value">数值</param>
		/// <returns>
		/// 追加 value 之后， key 中字符串的长度。
		/// </returns>
		public OperateResult<int> AppendKey( string key, string value ) => OperateNumberFromServer( new string[] { "APPEND", key, value } );

		/// <summary>
		/// 将 key 中储存的数字值减一。如果 key 不存在，那么 key 的值会先被初始化为 0 ，然后再执行 DECR 操作。
		/// 如果值包含错误的类型，或字符串类型的值不能表示为数字，那么返回一个错误。
		/// 本操作的值限制在 64 位(bit)有符号数字表示之内。
		/// 返回执行 DECR 命令之后 key 的值。
		/// </summary>
		/// <param name="key">关键字</param>
		/// <returns>执行 DECR 命令之后 key 的值。</returns>
		public OperateResult<long> DecrementKey( string key ) => OperateLongNumberFromServer( new string[] { "DECR", key } );

		/// <summary>
		/// 将 key 所储存的值减去减量 decrement 。如果 key 不存在，那么 key 的值会先被初始化为 0 ，然后再执行 DECR 操作。
		/// 如果值包含错误的类型，或字符串类型的值不能表示为数字，那么返回一个错误。
		/// 本操作的值限制在 64 位(bit)有符号数字表示之内。
		/// 返回减去 decrement 之后， key 的值。
		/// </summary>
		/// <param name="key">关键字</param>
		/// <param name="value">操作的值</param>
		/// <returns>返回减去 decrement 之后， key 的值。</returns>
		public OperateResult<long> DecrementKey( string key, long value ) => OperateLongNumberFromServer( new string[] { "DECRBY", key, value.ToString( ) } );

		/// <summary>
		/// 返回 key 所关联的字符串值。如果 key 不存在那么返回特殊值 nil 。
		/// 假如 key 储存的值不是字符串类型，返回一个错误，因为 GET 只能用于处理字符串值。
		/// </summary>
		/// <param name="key">关键字</param>
		/// <returns>当 key 不存在时，返回 nil ，否则，返回 key 的值。</returns>
		public OperateResult<string> ReadKey( string key ) => OperateStringFromServer( new string[] { "GET", key } );

		/// <summary>
		/// 返回 key 中字符串值的子字符串，字符串的截取范围由 start 和 end 两个偏移量决定(包括 start 和 end 在内)。
		/// 负数偏移量表示从字符串最后开始计数， -1 表示最后一个字符， -2 表示倒数第二个，以此类推。
		/// 返回截取得出的子字符串。
		/// </summary>
		/// <param name="key">关键字</param>
		/// <param name="start">截取开始的位置</param>
		/// <param name="end">截取结束的位置</param>
		/// <returns>返回截取得出的子字符串。</returns>
		public OperateResult<string> ReadKeyRange( string key, int start, int end ) => OperateStringFromServer( new string[] { "GETRANGE", key, start.ToString( ), end.ToString( ) } );

		/// <summary>
		/// 将给定 key 的值设为 value ，并返回 key 的旧值(old value)。当 key 存在但不是字符串类型时，返回一个错误。
		/// </summary>
		/// <param name="key">关键字</param>
		/// <param name="value">新的值</param>
		/// <returns>返回给定 key 的旧值。当 key 没有旧值时，也即是， key 不存在时，返回 nil 。</returns>
		public OperateResult<string> ReadAndWriteKey( string key, string value ) => OperateStringFromServer( new string[] { "GETSET", key, value } );

		/// <summary>
		/// 将 key 中储存的数字值增一。如果 key 不存在，那么 key 的值会先被初始化为 0 ，然后再执行 INCR 操作。
		/// 如果值包含错误的类型，或字符串类型的值不能表示为数字，那么返回一个错误。
		/// 返回执行 INCR 命令之后 key 的值。
		/// </summary>
		/// <param name="key">关键字</param>
		/// <returns>返回执行 INCR 命令之后 key 的值。</returns>
		public OperateResult<long> IncrementKey( string key ) => OperateLongNumberFromServer( new string[] { "INCR", key } );

		/// <summary>
		/// 将 key 所储存的值加上增量 increment 。如果 key 不存在，那么 key 的值会先被初始化为 0 ，然后再执行 INCR 操作。
		/// 如果值包含错误的类型，或字符串类型的值不能表示为数字，那么返回一个错误。
		/// </summary>
		/// <param name="key">关键字</param>
		/// <param name="value">增量数据</param>
		/// <returns>加上 increment 之后， key 的值。</returns>
		public OperateResult<long> IncrementKey( string key, long value ) => OperateLongNumberFromServer( new string[] { "INCRBY", key, value.ToString( ) } );

		/// <summary>
		/// 将 key 所储存的值加上增量 increment 。如果 key 不存在，那么 key 的值会先被初始化为 0 ，然后再执行 INCRBYFLOAT 操作。
		/// 如果命令执行成功，那么 key 的值会被更新为（执行加法之后的）新值，并且新值会以字符串的形式返回给调用者
		/// </summary>
		/// <param name="key">关键字</param>
		/// <param name="value">增量数据</param>
		/// <returns>执行命令之后 key 的值。</returns>
		public OperateResult<string> IncrementKey( string key, float value ) => OperateStringFromServer( new string[] { "INCRBYFLOAT", key, value.ToString( ) } );

		/// <summary>
		/// 返回所有(一个或多个)给定 key 的值。
		/// 如果给定的 key 里面，有某个 key 不存在，那么这个 key 返回特殊值 null 。因此，该命令永不失败。
		/// </summary>
		/// <param name="keys">关键字数组</param>
		/// <returns>一个包含所有给定 key 的值的列表。</returns>
		public OperateResult<string[]> ReadKey( string[] keys ) => OperateStringsFromServer( SoftBasic.SpliceStringArray( "MGET", keys ) );

		/// <summary>
		/// 同时设置一个或多个 key-value 对。
		/// 如果某个给定 key 已经存在，那么 MSET 会用新值覆盖原来的旧值，如果这不是你所希望的效果，请考虑使用 MSETNX 命令：它只会在所有给定 key 都不存在的情况下进行设置操作。
		/// </summary>
		/// <param name="keys">关键字数组</param>
		/// <param name="values">值数组</param>
		/// <returns>总是返回 OK (因为 MSET 不可能失败)</returns>
		public OperateResult WriteKey( string[] keys, string[] values )
		{
			if (keys == null) throw new ArgumentNullException( "keys" );
			if (values == null) throw new ArgumentNullException( "values" );
			if (keys.Length != values.Length) throw new ArgumentException( "Two arguement not same length" );

			List<string> list = new List<string>( );
			list.Add( "MSET" );
			for (int i = 0; i < keys.Length; i++)
			{
				list.Add( keys[i] );
				list.Add( values[i] );
			}

			return OperateStatusFromServer( list.ToArray( ) );
		}

		/// <summary>
		/// 将字符串值 value 关联到 key 。
		/// 如果 key 已经持有其他值， SET 就覆写旧值，无视类型。
		/// 对于某个原本带有生存时间（TTL）的键来说， 当 SET 命令成功在这个键上执行时，这个键原有的 TTL 将被清除。
		/// </summary>
		/// <param name="key">关键字</param>
		/// <param name="value">数据值</param>
		/// <returns> SET 在设置操作成功完成时，才返回 OK 。</returns>
		public OperateResult WriteKey( string key, string value ) => OperateStatusFromServer( new string[] { "SET", key, value } );

		/// <summary>
		/// 将字符串值 value 关联到 key 。并发布一个订阅的频道数据，都成功时，才返回成功
		/// </summary>
		/// <param name="key">关键字</param>
		/// <param name="value">数据值</param>
		/// <returns>是否成功的结果对象</returns>
		public OperateResult WriteAndPublishKey(string key, string value )
		{
			OperateResult write = WriteKey( key, value );
			if (!write.IsSuccess) return write;

			return Publish( key, value );
		}

		/// <summary>
		/// 将值 value 关联到 key ，并将 key 的生存时间设为 seconds (以秒为单位)。如果 key 已经存在， SETEX 命令将覆写旧值。
		/// </summary>
		/// <param name="key">关键字</param>
		/// <param name="value">数值</param>
		/// <param name="seconds">生存时间，单位秒</param>
		/// <returns>设置成功时返回 OK 。当 seconds 参数不合法时，返回一个错误。</returns>
		public OperateResult WriteExpireKey( string key, string value, long seconds ) => OperateStatusFromServer( new string[] { "SETEX", key, seconds.ToString( ), value } );

		/// <summary>
		/// 将 key 的值设为 value ，当且仅当 key 不存在。若给定的 key 已经存在，则 SETNX 不做任何动作。设置成功，返回 1 。设置失败，返回 0 。
		/// </summary>
		/// <param name="key">关键字</param>
		/// <param name="value">数据值</param>
		/// <returns>设置成功，返回 1 。设置失败，返回 0 。</returns>
		public OperateResult<int> WriteKeyIfNotExists( string key, string value ) => OperateNumberFromServer( new string[] { "SETNX", key, value } );

		/// <summary>
		/// 用 value 参数覆写(overwrite)给定 key 所储存的字符串值，从偏移量 offset 开始。不存在的 key 当作空白字符串处理。返回被 SETRANGE 修改之后，字符串的长度。
		/// </summary>
		/// <param name="key">关键字</param>
		/// <param name="value">数值</param>
		/// <param name="offset">起始的偏移量</param>
		/// <returns>被 SETRANGE 修改之后，字符串的长度。</returns>
		public OperateResult<int> WriteKeyRange( string key, string value, int offset ) => OperateNumberFromServer( new string[] { "SETRANGE", key, offset.ToString( ), value } );

		/// <summary>
		/// 返回 key 所储存的字符串值的长度。当 key 储存的不是字符串值时，返回一个错误。返回符串值的长度。当 key 不存在时，返回 0 。
		/// </summary>
		/// <param name="key">关键字</param>
		/// <returns>字符串值的长度。当 key 不存在时，返回 0 。</returns>
		public OperateResult<int> ReadKeyLength( string key ) => OperateNumberFromServer( new string[] { "STRLEN", key } );

		#endregion

		#region Async String Operate
#if !NET35 && !NET20
		/// <inheritdoc cref="AppendKey(string, string)"/>
		public async Task<OperateResult<int>> AppendKeyAsync( string key, string value ) => await OperateNumberFromServerAsync( new string[] { "APPEND", key, value } );

		/// <inheritdoc cref="DecrementKey(string)"/>
		public async Task<OperateResult<long>> DecrementKeyAsync( string key ) => await OperateLongNumberFromServerAsync( new string[] { "DECR", key } );

		/// <inheritdoc cref="DecrementKey(string, long)"/>
		public async Task<OperateResult<long>> DecrementKeyAsync( string key, long value ) => await OperateLongNumberFromServerAsync( new string[] { "DECRBY", key, value.ToString( ) } );

		/// <inheritdoc cref="ReadKey(string)"/>
		public async Task<OperateResult<string>> ReadKeyAsync( string key ) => await OperateStringFromServerAsync( new string[] { "GET", key } );

		/// <inheritdoc cref="ReadKeyRange(string, int, int)"/>
		public async Task<OperateResult<string>> ReadKeyRangeAsync( string key, int start, int end ) => await OperateStringFromServerAsync( new string[] { "GETRANGE", key, start.ToString( ), end.ToString( ) } );

		/// <inheritdoc cref="ReadAndWriteKey(string, string)"/>
		public async Task<OperateResult<string>> ReadAndWriteKeyAsync( string key, string value ) => await OperateStringFromServerAsync( new string[] { "GETSET", key, value } );

		/// <inheritdoc cref="IncrementKey(string)"/>
		public async Task<OperateResult<long>> IncrementKeyAsync( string key ) => await OperateLongNumberFromServerAsync( new string[] { "INCR", key } );

		/// <inheritdoc cref="IncrementKey(string, long)"/>
		public async Task<OperateResult<long>> IncrementKeyAsync( string key, long value ) => await OperateLongNumberFromServerAsync( new string[] { "INCRBY", key, value.ToString( ) } );

		/// <inheritdoc cref="IncrementKey(string, float)"/>
		public async Task<OperateResult<string>> IncrementKeyAsync( string key, float value ) => await OperateStringFromServerAsync( new string[] { "INCRBYFLOAT", key, value.ToString( ) } );

		/// <inheritdoc cref="ReadKey(string[])"/>
		public async Task<OperateResult<string[]>> ReadKeyAsync( string[] keys ) => await OperateStringsFromServerAsync( SoftBasic.SpliceStringArray( "MGET", keys ) );

		/// <inheritdoc cref="WriteKey(string[], string[])"/>
		public async Task<OperateResult> WriteKeyAsync( string[] keys, string[] values )
		{
			if (keys == null) throw new ArgumentNullException( "keys" );
			if (values == null) throw new ArgumentNullException( "values" );
			if (keys.Length != values.Length) throw new ArgumentException( "Two arguement not same length" );

			List<string> list = new List<string>( );
			list.Add( "MSET" );
			for (int i = 0; i < keys.Length; i++)
			{
				list.Add( keys[i] );
				list.Add( values[i] );
			}

			return await OperateStatusFromServerAsync( list.ToArray( ) );
		}

		/// <inheritdoc cref="WriteKey(string, string)"/>
		public async Task<OperateResult> WriteKeyAsync( string key, string value ) => await OperateStatusFromServerAsync( new string[] { "SET", key, value } );

		/// <inheritdoc cref="WriteAndPublishKey(string, string)"/>
		public async Task<OperateResult> WriteAndPublishKeyAsync( string key, string value )
		{
			OperateResult write = await WriteKeyAsync( key, value );
			if (!write.IsSuccess) return write;

			return await PublishAsync( key, value );
		}

		/// <inheritdoc cref="WriteExpireKey(string, string, long)"/>
		public async Task<OperateResult> WriteExpireKeyAsync( string key, string value, long seconds ) => await OperateStatusFromServerAsync( new string[] { "SETEX", key, seconds.ToString( ), value } );

		/// <inheritdoc cref="WriteKeyIfNotExists(string, string)"/>
		public async Task<OperateResult<int>> WriteKeyIfNotExistsAsync( string key, string value ) => await OperateNumberFromServerAsync( new string[] { "SETNX", key, value } );

		/// <inheritdoc cref="WriteKeyRange(string, string, int)"/>
		public async Task<OperateResult<int>> WriteKeyRangeAsync( string key, string value, int offset ) => await OperateNumberFromServerAsync( new string[] { "SETRANGE", key, offset.ToString( ), value } );

		/// <inheritdoc cref="ReadKeyLength(string)"/>
		public async Task<OperateResult<int>> ReadKeyLengthAsync( string key ) => await OperateNumberFromServerAsync( new string[] { "STRLEN", key } );
#endif
		#endregion

		#region List Operate

		/// <summary>
		/// 将值 value 插入到列表 key 当中，位于值 pivot 之前。
		/// 当 pivot 不存在于列表 key 时，不执行任何操作。
		/// 当 key 不存在时， key 被视为空列表，不执行任何操作。
		/// 如果 key 不是列表类型，返回一个错误。
		/// </summary>
		/// <param name="key">关键字</param>
		/// <param name="value">数值</param>
		/// <param name="pivot">原先的值</param>
		/// <returns>
		/// 如果命令执行成功，返回插入操作完成之后，列表的长度。
		/// 如果没有找到 pivot ，返回 -1 。
		/// 如果 key 不存在或为空列表，返回 0 。
		/// </returns>
		public OperateResult<int> ListInsertBefore( string key, string value, string pivot ) => OperateNumberFromServer( new string[] { "LINSERT", key, "BEFORE", pivot, value } );

		/// <summary>
		/// 将值 value 插入到列表 key 当中，位于值 pivot 之后。
		/// 当 pivot 不存在于列表 key 时，不执行任何操作。
		/// 当 key 不存在时， key 被视为空列表，不执行任何操作。
		/// 如果 key 不是列表类型，返回一个错误。
		/// </summary>
		/// <param name="key">关键字</param>
		/// <param name="value">数值</param>
		/// <param name="pivot">原先的值</param>
		/// <returns>
		/// 如果命令执行成功，返回插入操作完成之后，列表的长度。
		/// 如果没有找到 pivot ，返回 -1 。
		/// 如果 key 不存在或为空列表，返回 0 。
		/// </returns>
		public OperateResult<int> ListInsertAfter( string key, string value, string pivot ) => OperateNumberFromServer( new string[] { "LINSERT", key, "AFTER", pivot, value } );

		/// <summary>
		/// 返回列表 key 的长度。如果 key 不存在，则 key 被解释为一个空列表，返回 0 .如果 key 不是列表类型，返回一个错误。
		/// </summary>
		/// <param name="key">关键字</param>
		/// <returns>列表 key 的长度。</returns>
		public OperateResult<int> GetListLength( string key ) => OperateNumberFromServer( new string[] { "LLEN", key } );

		/// <summary>
		/// 返回列表 key 中，下标为 index 的元素。下标(index)参数 start 和 stop 都以 0 为底，也就是说，以 0 表示列表的第一个元素，以 1 表示列表的第二个元素，以此类推。
		/// 你也可以使用负数下标，以 -1 表示列表的最后一个元素， -2 表示列表的倒数第二个元素，以此类推。如果 key 不是列表类型，返回一个错误。
		/// </summary>
		/// <param name="key">关键字</param>
		/// <param name="index">索引位置</param>
		/// <returns>列表中下标为 index 的元素。如果 index 参数的值不在列表的区间范围内(out of range)，返回 nil 。</returns>
		public OperateResult<string> ReadListByIndex( string key, long index ) => OperateStringFromServer( new string[] { "LINDEX", key, index.ToString( ) } );

		/// <summary>
		/// 移除并返回列表 key 的头元素。列表的头元素。当 key 不存在时，返回 nil 。
		/// </summary>
		/// <param name="key">关键字信息</param>
		/// <returns>列表的头元素。</returns>
		public OperateResult<string> ListLeftPop( string key ) => OperateStringFromServer( new string[] { "LPOP", key } );

		/// <summary>
		/// 将一个或多个值 value 插入到列表 key 的表头，如果 key 不存在，一个空列表会被创建并执行 LPUSH 操作。当 key 存在但不是列表类型时，返回一个错误。返回执行 LPUSH 命令后，列表的长度。
		/// </summary>
		/// <param name="key">关键字</param>
		/// <param name="value">值</param>
		/// <returns>执行 LPUSH 命令后，列表的长度。</returns>
		public OperateResult<int> ListLeftPush( string key, string value ) => ListLeftPush( key, new string[] { value } );

		/// <summary>
		/// 将一个或多个值 value 插入到列表 key 的表头，如果 key 不存在，一个空列表会被创建并执行 LPUSH 操作。当 key 存在但不是列表类型时，返回一个错误。返回执行 LPUSH 命令后，列表的长度。
		/// </summary>
		/// <param name="key">关键字</param>
		/// <param name="values">值</param>
		/// <returns>执行 LPUSH 命令后，列表的长度。</returns>
		public OperateResult<int> ListLeftPush( string key, string[] values ) => OperateNumberFromServer( SoftBasic.SpliceStringArray( "LPUSH", key, values ) );

		/// <summary>
		/// 将值 value 插入到列表 key 的表头，当且仅当 key 存在并且是一个列表。和 LPUSH 命令相反，当 key 不存在时， LPUSHX 命令什么也不做。
		/// 返回LPUSHX 命令执行之后，表的长度。
		/// </summary>
		/// <param name="key">关键字</param>
		/// <param name="value">值</param>
		/// <returns>是否插入数据成功</returns>
		public OperateResult<int> ListLeftPushX( string key, string value ) => OperateNumberFromServer( new string[] { "LPUSHX", key, value } );

		/// <summary>
		/// 返回列表 key 中指定区间内的元素，区间以偏移量 start 和 stop 指定。
		/// 下标(index)参数 start 和 stop 都以 0 为底，也就是说，以 0 表示列表的第一个元素，以 1 表示列表的第二个元素，以此类推。
		/// 你也可以使用负数下标，以 -1 表示列表的最后一个元素， -2 表示列表的倒数第二个元素，以此类推。
		/// 返回一个列表，包含指定区间内的元素。
		/// </summary>
		/// <param name="key">关键字</param>
		/// <param name="start">开始的索引</param>
		/// <param name="stop">结束的索引</param>
		/// <returns>返回一个列表，包含指定区间内的元素。</returns>
		public OperateResult<string[]> ListRange( string key, long start, long stop ) => OperateStringsFromServer( new string[] { "LRANGE", key, start.ToString( ), stop.ToString( ) } );

		/// <summary>
		/// 根据参数 count 的值，移除列表中与参数 value 相等的元素。count 的值可以是以下几种：
		/// count > 0 : 从表头开始向表尾搜索，移除与 value 相等的元素，数量为 count 。
		/// count &lt; 0 : 从表尾开始向表头搜索，移除与 value 相等的元素，数量为 count 的绝对值。
		/// count = 0 : 移除表中所有与 value 相等的值。
		/// 返回被移除的数量。
		/// </summary>
		/// <param name="key">关键字</param>
		/// <param name="count">移除参数</param>
		/// <param name="value">匹配的值</param>
		/// <returns>被移除元素的数量。因为不存在的 key 被视作空表(empty list)，所以当 key 不存在时， LREM 命令总是返回 0 。</returns>
		public OperateResult<int> ListRemoveElementMatch( string key, long count, string value ) => OperateNumberFromServer( new string[] { "LREM", key, count.ToString( ), value } );

		/// <summary>
		/// 设置数组的某一个索引的数据信息，当 index 参数超出范围，或对一个空列表( key 不存在)进行 LSET 时，返回一个错误。
		/// </summary>
		/// <param name="key">关键字</param>
		/// <param name="index">索引位置</param>
		/// <param name="value">值</param>
		/// <returns>操作成功返回 ok ，否则返回错误信息。</returns>
		public OperateResult ListSet( string key, long index, string value ) => OperateStatusFromServer( new string[] { "LSET", key.ToString( ), index.ToString( ), value } );

		/// <summary>
		/// 对一个列表进行修剪(trim)，就是说，让列表只保留指定区间内的元素，不在指定区间之内的元素都将被删除。
		/// 举个例子，执行命令 LTRIM list 0 2 ，表示只保留列表 list 的前三个元素，其余元素全部删除。
		/// 下标( index)参数 start 和 stop 都以 0 为底，也就是说，以 0 表示列表的第一个元素，以 1 表示列表的第二个元素，以此类推。
		/// 你也可以使用负数下标，以 -1 表示列表的最后一个元素， -2 表示列表的倒数第二个元素，以此类推。
		/// 当 key 不是列表类型时，返回一个错误。
		/// </summary>
		/// <param name="key">关键字信息</param>
		/// <param name="start">起始的索引信息</param>
		/// <param name="end">结束的索引信息</param>
		/// <returns>操作成功返回 ok ，否则返回错误信息。</returns>
		public OperateResult ListTrim( string key, long start, long end ) => OperateStatusFromServer( new string[] { "LTRIM", key, start.ToString( ), end.ToString( ) } );

		/// <summary>
		/// 移除并返回列表 key 的尾元素。当 key 不存在时，返回 nil 。
		/// </summary>
		/// <param name="key">关键字信息</param>
		/// <returns>列表的尾元素。</returns>
		public OperateResult<string> ListRightPop( string key ) => OperateStringFromServer( new string[] { "RPOP", key } );

		/// <summary>
		/// 命令 RPOPLPUSH 在一个原子时间内，执行以下两个动作：<br />
		/// 1. 将列表 source 中的最后一个元素( 尾元素)弹出，并返回给客户端。<br />
		/// 2. 将 source 弹出的元素插入到列表 destination ，作为 destination 列表的的头元素。<br /><br />
		/// 举个例子，你有两个列表 source 和 destination ， source 列表有元素 a, b, c ， destination 列表有元素 x, y, z ，执行 RPOPLPUSH source destination 之后， source 列表包含元素 a, b ， destination 列表包含元素 c, x, y, z ，并且元素 c 会被返回给客户端。
		/// 如果 source 不存在，值 nil 被返回，并且不执行其他动作。
		/// 如果 source 和 destination 相同，则列表中的表尾元素被移动到表头，并返回该元素，可以把这种特殊情况视作列表的旋转( rotation)操作。
		/// </summary>
		/// <param name="key1">第一个关键字</param>
		/// <param name="key2">第二个关键字</param>
		/// <returns>返回的移除的对象</returns>
		public OperateResult<string> ListRightPopLeftPush( string key1, string key2 ) => OperateStringFromServer( new string[] { "RPOPLPUSH", key1, key2 } );

		/// <summary>
		/// 将一个或多个值 value 插入到列表 key 的表尾(最右边)。
		/// 如果 key 不存在，一个空列表会被创建并执行 RPUSH 操作。当 key 存在但不是列表类型时，返回一个错误。
		/// </summary>
		/// <param name="key">关键字</param>
		/// <param name="value">值</param>
		/// <returns>返回执行 RPUSH 操作后，表的长度。</returns>
		public OperateResult<int> ListRightPush( string key, string value ) => ListRightPush( key, new string[] { value } );

		/// <summary>
		/// 将一个或多个值 value 插入到列表 key 的表尾(最右边)。
		/// 如果有多个 value 值，那么各个 value 值按从左到右的顺序依次插入到表尾：比如对一个空列表 mylist 执行 RPUSH mylist a b c ，得出的结果列表为 a b c ，
		/// 如果 key 不存在，一个空列表会被创建并执行 RPUSH 操作。当 key 存在但不是列表类型时，返回一个错误。
		/// 返回执行 RPUSH 操作后，表的长度。
		/// </summary>
		/// <param name="key">关键字</param>
		/// <param name="values">值</param>
		/// <returns>返回执行 RPUSH 操作后，表的长度。</returns>
		public OperateResult<int> ListRightPush( string key, string[] values ) => OperateNumberFromServer( SoftBasic.SpliceStringArray( "RPUSH", key, values ) );

		/// <summary>
		/// 将值 value 插入到列表 key 的表尾，当且仅当 key 存在并且是一个列表。
		/// 和 RPUSH 命令相反，当 key 不存在时， RPUSHX 命令什么也不做。
		/// </summary>
		/// <param name="key">关键字</param>
		/// <param name="value">值</param>
		/// <returns>RPUSHX 命令执行之后，表的长度。</returns>
		public OperateResult<int> ListRightPushX( string key, string value ) => OperateNumberFromServer( new string[] { "RPUSHX", key, value } );

		#endregion

		#region Async List Operate
#if !NET35 && !NET20
		/// <inheritdoc cref="ListInsertBefore(string, string, string)"/>
		public async Task<OperateResult<int>> ListInsertBeforeAsync( string key, string value, string pivot ) => await OperateNumberFromServerAsync( new string[] { "LINSERT", key, "BEFORE", pivot, value } );

		/// <inheritdoc cref="ListInsertAfter(string, string, string)"/>
		public async Task<OperateResult<int>> ListInsertAfterAsync( string key, string value, string pivot ) => await OperateNumberFromServerAsync( new string[] { "LINSERT", key, "AFTER", pivot, value } );

		/// <inheritdoc cref="GetListLength(string)"/>
		public async Task<OperateResult<int>> GetListLengthAsync( string key ) => await OperateNumberFromServerAsync( new string[] { "LLEN", key } );

		/// <inheritdoc cref="ReadListByIndex(string, long)"/>
		public async Task<OperateResult<string>> ReadListByIndexAsync( string key, long index ) => await OperateStringFromServerAsync( new string[] { "LINDEX", key, index.ToString( ) } );

		/// <inheritdoc cref="ListLeftPop(string)"/>
		public async Task<OperateResult<string>> ListLeftPopAsync( string key ) => await OperateStringFromServerAsync( new string[] { "LPOP", key } );

		/// <inheritdoc cref="ListLeftPush(string, string)"/>
		public async Task<OperateResult<int>> ListLeftPushAsync( string key, string value ) => await ListLeftPushAsync( key, new string[] { value } );

		/// <inheritdoc cref="ListLeftPush(string, string[])"/>
		public async Task<OperateResult<int>> ListLeftPushAsync( string key, string[] values ) => await OperateNumberFromServerAsync( SoftBasic.SpliceStringArray( "LPUSH", key, values ) );

		/// <inheritdoc cref="ListLeftPushX(string, string)"/>
		public async Task<OperateResult<int>> ListLeftPushXAsync( string key, string value ) => await OperateNumberFromServerAsync( new string[] { "LPUSHX", key, value } );

		/// <inheritdoc cref="ListRange(string, long, long)"/>
		public async Task<OperateResult<string[]>> ListRangeAsync( string key, long start, long stop ) => await OperateStringsFromServerAsync( new string[] { "LRANGE", key, start.ToString( ), stop.ToString( ) } );

		/// <inheritdoc cref="ListRemoveElementMatch(string, long, string)"/>
		public async Task<OperateResult<int>> ListRemoveElementMatchAsync( string key, long count, string value ) => await OperateNumberFromServerAsync( new string[] { "LREM", key, count.ToString( ), value } );

		/// <inheritdoc cref="ListSet(string, long, string)"/>
		public async Task<OperateResult> ListSetAsync( string key, long index, string value ) => await OperateStatusFromServerAsync( new string[] { "LSET", key.ToString( ), index.ToString( ), value } );

		/// <inheritdoc cref="ListTrim(string, long, long)"/>
		public async Task<OperateResult> ListTrimAsync( string key, long start, long end ) => await OperateStatusFromServerAsync( new string[] { "LTRIM", key, start.ToString( ), end.ToString( ) } );

		/// <inheritdoc cref="ListRightPop(string)"/>
		public async Task<OperateResult<string>> ListRightPopAsync( string key ) => await OperateStringFromServerAsync( new string[] { "RPOP", key } );

		/// <inheritdoc cref="ListRightPopLeftPush(string, string)"/>
		public async Task<OperateResult<string>> ListRightPopLeftPushAsync( string key1, string key2 ) => await OperateStringFromServerAsync( new string[] { "RPOPLPUSH", key1, key2 } );

		/// <inheritdoc cref="ListRightPush(string, string)"/>
		public async Task<OperateResult<int>> ListRightPushAsync( string key, string value ) => await ListRightPushAsync( key, new string[] { value } );

		/// <inheritdoc cref="ListRightPush(string, string[])"/>
		public async Task<OperateResult<int>> ListRightPushAsync( string key, string[] values ) => await OperateNumberFromServerAsync( SoftBasic.SpliceStringArray( "RPUSH", key, values ) );

		/// <inheritdoc cref="ListRightPushX(string, string)"/>
		public async Task<OperateResult<int>> ListRightPushXAsync( string key, string value ) => await OperateNumberFromServerAsync( new string[] { "RPUSHX", key, value } );
#endif
		#endregion

		#region Hash Operate

		/// <summary>
		/// 删除哈希表 key 中的一个或多个指定域，不存在的域将被忽略。
		/// </summary>
		/// <param name="key">关键字</param>
		/// <param name="field">域</param>
		/// <returns>被成功移除的域的数量，不包括被忽略的域。</returns>
		public OperateResult<int> DeleteHashKey( string key, string field ) => DeleteHashKey( key, new string[] { field } );

		/// <summary>
		/// 删除哈希表 key 中的一个或多个指定域，不存在的域将被忽略。返回被成功移除的域的数量，不包括被忽略的域。
		/// </summary>
		/// <param name="key">关键字</param>
		/// <param name="fields">所有的域</param>
		/// <returns>返回被成功移除的域的数量，不包括被忽略的域。</returns>
		public OperateResult<int> DeleteHashKey( string key, string[] fields ) => OperateNumberFromServer( SoftBasic.SpliceStringArray( "HDEL", key, fields ) );

		/// <summary>
		/// 查看哈希表 key 中，给定域 field 是否存在。如果哈希表含有给定域，返回 1 。
		/// 如果哈希表不含有给定域，或 key 不存在，返回 0 。
		/// </summary>
		/// <param name="key">关键字</param>
		/// <param name="field">域</param>
		/// <returns>如果哈希表含有给定域，返回 1 。如果哈希表不含有给定域，或 key 不存在，返回 0 。</returns>
		public OperateResult<int> ExistsHashKey( string key, string field ) => OperateNumberFromServer( new string[] { "HEXISTS", key, field } );

		/// <summary>
		/// 返回哈希表 key 中给定域 field 的值。当给定域不存在或是给定 key 不存在时，返回 nil 
		/// </summary>
		/// <param name="key">关键值</param>
		/// <param name="field">域</param>
		/// <returns>
		/// 给定域的值。
		/// 当给定域不存在或是给定 key 不存在时，返回 nil 。
		/// </returns>
		public OperateResult<string> ReadHashKey( string key, string field ) => OperateStringFromServer( new string[] { "HGET", key, field } );

		/// <summary>
		/// 返回哈希表 key 中，所有的域和值。在返回值里，紧跟每个域名(field name)之后是域的值(value)，所以返回值的长度是哈希表大小的两倍。
		/// </summary>
		/// <param name="key">关键值</param>
		/// <returns>
		/// 以列表形式返回哈希表的域和域的值。
		/// 若 key 不存在，返回空列表。
		/// </returns>
		public OperateResult<string[]> ReadHashKeyAll( string key ) => OperateStringsFromServer( new string[] { "HGETALL", key } );

		/// <summary>
		/// 为哈希表 key 中的域 field 的值加上增量 increment 。增量也可以为负数，相当于对给定域进行减法操作。
		/// 如果 key 不存在，一个新的哈希表被创建并执行 HINCRBY 命令。返回执行 HINCRBY 命令之后，哈希表 key 中域 field 的值。
		/// </summary>
		/// <param name="key">关键字</param>
		/// <param name="field">域</param>
		/// <param name="value">增量值</param>
		/// <returns>返回执行 HINCRBY 命令之后，哈希表 key 中域 field 的值。</returns>
		public OperateResult<long> IncrementHashKey( string key, string field, long value ) => OperateLongNumberFromServer( new string[] { "HINCRBY", key, field, value.ToString( ) } );

		/// <summary>
		/// 为哈希表 key 中的域 field 的值加上增量 increment 。增量也可以为负数，相当于对给定域进行减法操作。
		/// 如果 key 不存在，一个新的哈希表被创建并执行 HINCRBY 命令。返回执行 HINCRBY 命令之后，哈希表 key 中域 field 的值。
		/// </summary>
		/// <param name="key">关键字</param>
		/// <param name="field">域</param>
		/// <param name="value">增量值</param>
		/// <returns>返回执行 HINCRBY 命令之后，哈希表 key 中域 field 的值。</returns>
		public OperateResult<string> IncrementHashKey( string key, string field, float value ) => OperateStringFromServer( new string[] { "HINCRBYFLOAT", key, field, value.ToString( ) } );

		/// <summary>
		/// 返回哈希表 key 中的所有域。当 key 不存在时，返回一个空表。
		/// </summary>
		/// <param name="key">关键值</param>
		/// <returns>
		/// 一个包含哈希表中所有域的表。
		/// 当 key 不存在时，返回一个空表。
		/// </returns>
		public OperateResult<string[]> ReadHashKeys( string key ) => OperateStringsFromServer( new string[] { "HKEYS", key } );

		/// <summary>
		/// 返回哈希表 key 中域的数量。
		/// </summary>
		/// <param name="key">关键字</param>
		/// <returns>哈希表中域的数量。当 key 不存在时，返回 0 。</returns>
		public OperateResult<int> ReadHashKeyLength( string key ) => OperateNumberFromServer( new string[] { "HLEN", key } );

		/// <summary>
		/// 返回哈希表 key 中，一个或多个给定域的值。如果给定的域不存在于哈希表，那么返回一个 nil 值。
		/// 因为不存在的 key 被当作一个空哈希表来处理，所以对一个不存在的 key 进行 HMGET 操作将返回一个只带有 nil 值的表。
		/// </summary>
		/// <param name="key">关键值</param>
		/// <param name="fields">指定的域</param>
		/// <returns>
		/// 一个包含多个给定域的关联值的表，表值的排列顺序和给定域参数的请求顺序一样。
		/// </returns>
		public OperateResult<string[]> ReadHashKey( string key, string[] fields ) => OperateStringsFromServer( SoftBasic.SpliceStringArray( "HMGET", key, fields ) );

		/// <summary>
		/// 将哈希表 key 中的域 field 的值设为 value 。
		/// 如果 key 不存在，一个新的哈希表被创建并进行 HSET 操作。
		/// 如果域 field 已经存在于哈希表中，旧值将被覆盖。
		/// 如果 field 是哈希表中的一个新建域，并且值设置成功，返回 1 。
		/// 如果哈希表中域 field 已经存在且旧值已被新值覆盖，返回 0 。
		/// </summary>
		/// <param name="key">关键字</param>
		/// <param name="field">域</param>
		/// <param name="value">数据值</param>
		/// <returns>
		/// 如果 field 是哈希表中的一个新建域，并且值设置成功，返回 1 。
		/// 如果哈希表中域 field 已经存在且旧值已被新值覆盖，返回 0 。
		/// </returns>
		public OperateResult<int> WriteHashKey( string key, string field, string value ) => OperateNumberFromServer( new string[] { "HSET", key, field, value } );

		/// <summary>
		/// 同时将多个 field-value (域-值)对设置到哈希表 key 中。
		/// 此命令会覆盖哈希表中已存在的域。
		/// 如果 key 不存在，一个空哈希表被创建并执行 HMSET 操作。
		/// </summary>
		/// <param name="key">关键字</param>
		/// <param name="fields">域</param>
		/// <param name="values">数据值</param>
		/// <returns>
		/// 如果命令执行成功，返回 OK 。
		/// 当 key 不是哈希表(hash)类型时，返回一个错误
		/// </returns>
		public OperateResult WriteHashKey( string key, string[] fields, string[] values )
		{
			if (fields == null) throw new ArgumentNullException( "fields" );
			if (values == null) throw new ArgumentNullException( "values" );
			if (fields.Length != values.Length) throw new ArgumentException( "Two arguement not same length" );

			List<string> list = new List<string>( );
			list.Add( "HMSET" );
			list.Add( key );
			for (int i = 0; i < fields.Length; i++)
			{
				list.Add( fields[i] );
				list.Add( values[i] );
			}

			return OperateStatusFromServer( list.ToArray( ) );
		}

		/// <summary>
		/// 将哈希表 key 中的域 field 的值设置为 value ，当且仅当域 field 不存在。若域 field 已经存在，该操作无效。
		/// 设置成功，返回 1 。如果给定域已经存在且没有操作被执行，返回 0 。
		/// </summary>
		/// <param name="key">关键字</param>
		/// <param name="field">域</param>
		/// <param name="value">数据值</param>
		/// <returns>设置成功，返回 1 。如果给定域已经存在且没有操作被执行，返回 0 。</returns>
		public OperateResult<int> WriteHashKeyNx( string key, string field, string value ) => OperateNumberFromServer( new string[] { "HSETNX", key, field, value } );

		/// <summary>
		/// 返回哈希表 key 中所有域的值。当 key 不存在时，返回一个空表。
		/// </summary>
		/// <param name="key">关键值</param>
		/// <returns>
		/// 返回哈希表 key 中所有域的值。
		/// 当 key 不存在时，返回一个空表。
		/// </returns>
		public OperateResult<string[]> ReadHashValues( string key ) => OperateStringsFromServer( new string[] { "HVALS", key } );

		#endregion

		#region Async Hash Operate
#if !NET35 && !NET20
		/// <inheritdoc cref="DeleteHashKey(string, string)"/>
		public async Task<OperateResult<int>> DeleteHashKeyAsync( string key, string field ) => await DeleteHashKeyAsync( key, new string[] { field } );

		/// <inheritdoc cref="DeleteHashKey(string, string[])"/>
		public async Task<OperateResult<int>> DeleteHashKeyAsync( string key, string[] fields ) => await OperateNumberFromServerAsync( SoftBasic.SpliceStringArray( "HDEL", key, fields ) );

		/// <inheritdoc cref="ExistsHashKey(string, string)"/>
		public async Task<OperateResult<int>> ExistsHashKeyAsync( string key, string field ) => await OperateNumberFromServerAsync( new string[] { "HEXISTS", key, field } );

		/// <inheritdoc cref="ReadHashKey(string, string)"/>
		public async Task<OperateResult<string>> ReadHashKeyAsync( string key, string field ) => await OperateStringFromServerAsync( new string[] { "HGET", key, field } );

		/// <inheritdoc cref="ReadHashKeyAll(string)"/>
		public async Task<OperateResult<string[]>> ReadHashKeyAllAsync( string key ) => await OperateStringsFromServerAsync( new string[] { "HGETALL", key } );

		/// <inheritdoc cref="IncrementHashKey(string, string, long)"/>
		public async Task<OperateResult<long>> IncrementHashKeyAsync( string key, string field, long value ) => await OperateLongNumberFromServerAsync( new string[] { "HINCRBY", key, field, value.ToString( ) } );

		/// <inheritdoc cref="IncrementHashKey(string, string, float)"/>
		public async Task<OperateResult<string>> IncrementHashKeyAsync( string key, string field, float value ) => await OperateStringFromServerAsync( new string[] { "HINCRBYFLOAT", key, field, value.ToString( ) } );

		/// <inheritdoc cref="ReadHashKeys(string)"/>
		public async Task<OperateResult<string[]>> ReadHashKeysAsync( string key ) => await OperateStringsFromServerAsync( new string[] { "HKEYS", key } );

		/// <inheritdoc cref="ReadHashKeyLength(string)"/>
		public async Task<OperateResult<int>> ReadHashKeyLengthAsync( string key ) => await OperateNumberFromServerAsync( new string[] { "HLEN", key } );

		/// <inheritdoc cref="ReadHashKey(string, string[])"/>
		public async Task<OperateResult<string[]>> ReadHashKeyAsync( string key, string[] fields ) => await OperateStringsFromServerAsync( SoftBasic.SpliceStringArray( "HMGET", key, fields ) );

		/// <inheritdoc cref="WriteHashKey(string, string, string)"/>
		public async Task<OperateResult<int>> WriteHashKeyAsync( string key, string field, string value ) => await OperateNumberFromServerAsync( new string[] { "HSET", key, field, value } );

		/// <inheritdoc cref="WriteHashKey(string, string[], string[])"/>
		public async Task<OperateResult> WriteHashKeyAsync( string key, string[] fields, string[] values )
		{
			if (fields == null) throw new ArgumentNullException( "fields" );
			if (values == null) throw new ArgumentNullException( "values" );
			if (fields.Length != values.Length) throw new ArgumentException( "Two arguement not same length" );

			List<string> list = new List<string>( );
			list.Add( "HMSET" );
			list.Add( key );
			for (int i = 0; i < fields.Length; i++)
			{
				list.Add( fields[i] );
				list.Add( values[i] );
			}

			return await OperateStatusFromServerAsync( list.ToArray( ) );
		}

		/// <inheritdoc cref="WriteHashKeyNx(string, string, string)"/>
		public async Task<OperateResult<int>> WriteHashKeyNxAsync( string key, string field, string value ) => await OperateNumberFromServerAsync( new string[] { "HSETNX", key, field, value } );

		/// <inheritdoc cref="ReadHashValues(string)"/>
		public async Task<OperateResult<string[]>> ReadHashValuesAsync( string key ) => await OperateStringsFromServerAsync( new string[] { "HVALS", key } );
#endif
		#endregion

		#region Set Operate

		/// <summary>
		/// 将一个member 元素加入到集合 key 当中，已经存在于集合的 member 元素将被忽略。假如 key 不存在，则创建一个只包含 member 元素作成员的集合。当 key 不是集合类型时，返回一个错误。
		/// </summary>
		/// <param name="key">关键字信息</param>
		/// <param name="member">等待添加的元素</param>
		/// <returns>被添加到集合中的新元素的数量，不包括被忽略的元素。</returns>
		public OperateResult<int> SetAdd( string key, string member ) => SetAdd( key, new string[] { member } );

		/// <summary>
		/// 将一个或多个 member 元素加入到集合 key 当中，已经存在于集合的 member 元素将被忽略。假如 key 不存在，则创建一个只包含 member 元素作成员的集合。当 key 不是集合类型时，返回一个错误。
		/// </summary>
		/// <param name="key">关键字信息</param>
		/// <param name="members">等待添加的元素</param>
		/// <returns>被添加到集合中的新元素的数量，不包括被忽略的元素。</returns>
		public OperateResult<int> SetAdd( string key, string[] members ) => OperateNumberFromServer( SoftBasic.SpliceStringArray( "SADD", key, members ) );

		/// <summary>
		/// 返回集合 key 的基数(集合中元素的数量)。当 key 不存在时，返回 0 。
		/// </summary>
		/// <param name="key">集合 key 的名称</param>
		/// <returns>集合的基数。</returns>
		public OperateResult<int> SetCard( string key ) => OperateNumberFromServer( new string[] { "SCARD", key } );

		/// <summary>
		/// 返回一个集合的全部成员，该集合是所有给定集合之间的差集。
		/// </summary>
		/// <param name="key">集合关键字</param>
		/// <param name="diffKey">集合关键字</param>
		/// <returns>交集成员的列表。</returns>
		/// <inheritdoc cref="SetDiff(string, string[])"/>
		public OperateResult<string[]> SetDiff( string key, string diffKey ) => SetDiff( key, new string[] { diffKey } );

		/// <summary>
		/// 返回一个集合的全部成员，该集合是所有给定集合之间的差集。
		/// </summary>
		/// <param name="key">集合关键字</param>
		/// <param name="diffKeys">集合关键字</param>
		/// <returns>交集成员的列表。</returns>
		public OperateResult<string[]> SetDiff( string key, string[] diffKeys ) => OperateStringsFromServer( SoftBasic.SpliceStringArray( "SDIFF", key, diffKeys ) );

		/// <summary>
		/// 这个命令的作用和 SDIFF 类似，但它将结果保存到 destination 集合，而不是简单地返回结果集。如果 destination 集合已经存在，则将其覆盖。destination 可以是 key 本身。
		/// </summary>
		/// <param name="destination">目标集合</param>
		/// <param name="key">等待操作的集合</param>
		/// <param name="diffKey">运算的集合</param>
		/// <returns>结果集中的元素数量。</returns>
		public OperateResult<int> SetDiffStore( string destination, string key, string diffKey ) => SetDiffStore( destination, key, new string[] { diffKey } );

		/// <summary>
		/// 这个命令的作用和 SDIFF 类似，但它将结果保存到 destination 集合，而不是简单地返回结果集。如果 destination 集合已经存在，则将其覆盖。destination 可以是 key 本身。
		/// </summary>
		/// <param name="destination">目标集合</param>
		/// <param name="key">等待操作的集合</param>
		/// <param name="diffKeys">运算的集合</param>
		/// <returns>结果集中的元素数量。</returns>
		public OperateResult<int> SetDiffStore( string destination, string key, string[] diffKeys ) => OperateNumberFromServer( SoftBasic.SpliceStringArray( "SDIFFSTORE", destination, key, diffKeys ) );

		/// <summary>
		/// 返回一个集合的全部成员，该集合是所有给定集合的交集。不存在的 key 被视为空集。当给定集合当中有一个空集时，结果也为空集(根据集合运算定律)。
		/// </summary>
		/// <param name="key">集合关键字</param>
		/// <param name="interKey">运算的集合</param>
		/// <returns>交集成员的列表。</returns>
		public OperateResult<string[]> SetInter( string key, string interKey ) => SetInter( key, new string[] { interKey } );

		/// <summary>
		/// 返回一个集合的全部成员，该集合是所有给定集合的交集。不存在的 key 被视为空集。当给定集合当中有一个空集时，结果也为空集(根据集合运算定律)。
		/// </summary>
		/// <param name="key">集合关键字</param>
		/// <param name="interKeys">运算的集合</param>
		/// <returns>交集成员的列表。</returns>
		public OperateResult<string[]> SetInter( string key, string[] interKeys ) => OperateStringsFromServer( SoftBasic.SpliceStringArray( "SINTER", key, interKeys ) );

		/// <summary>
		/// 这个命令类似于 SINTER 命令，但它将结果保存到 destination 集合，而不是简单地返回结果集。如果 destination 集合已经存在，则将其覆盖。destination 可以是 key 本身。
		/// </summary>
		/// <param name="destination">目标集合</param>
		/// <param name="key">等待操作的集合</param>
		/// <param name="interKey">运算的集合</param>
		/// <returns>结果集中的成员数量。</returns>
		public OperateResult<int> SetInterStore( string destination, string key, string interKey ) => SetInterStore( destination, key, new string[] { interKey } );

		/// <summary>
		/// 这个命令类似于 SINTER 命令，但它将结果保存到 destination 集合，而不是简单地返回结果集。如果 destination 集合已经存在，则将其覆盖。destination 可以是 key 本身。
		/// </summary>
		/// <param name="destination">目标集合</param>
		/// <param name="key">等待操作的集合</param>
		/// <param name="interKeys">运算的集合</param>
		/// <returns>结果集中的成员数量。</returns>
		public OperateResult<int> SetInterStore( string destination, string key, string[] interKeys ) => OperateNumberFromServer( SoftBasic.SpliceStringArray( "SINTERSTORE", destination, key, interKeys ) );

		/// <summary>
		/// 判断 member 元素是否集合 key 的成员。如果 member 元素是集合的成员，返回 1 。如果 member 元素不是集合的成员，或 key 不存在，返回 0 。
		/// </summary>
		/// <param name="key">集合key</param>
		/// <param name="member">元素</param>
		/// <returns>如果 member 元素是集合的成员，返回 1 。如果 member 元素不是集合的成员，或 key 不存在，返回 0 。</returns>
		public OperateResult<int> SetIsMember( string key, string member ) => OperateNumberFromServer( new string[] { "SISMEMBER", key, member } );

		/// <summary>
		/// 返回集合 key 中的所有成员。不存在的 key 被视为空集合。
		/// </summary>
		/// <param name="key">集合key</param>
		/// <returns>集合中的所有成员。</returns>
		public OperateResult<string[]> SetMembers( string key ) => OperateStringsFromServer( new string[] { "SMEMBERS", key } );

		/// <summary>
		/// 将 member 元素从 source 集合移动到 destination 集合。如果 source 集合不存在或不包含指定的 member 元素，则 SMOVE 命令不执行任何操作，仅返回 0 。
		/// 否则， member 元素从 source 集合中被移除，并添加到 destination 集合中去。当 destination 集合已经包含 member 元素时， SMOVE 命令只是简单地将 source 集合中的 member 元素删除。
		/// 当 source 或 destination 不是集合类型时，返回一个错误。
		/// </summary>
		/// <param name="source">原集合</param>
		/// <param name="destination">目标集合</param>
		/// <param name="member">元素</param>
		/// <returns>如果 member 元素被成功移除，返回 1 。如果 member 元素不是 source 集合的成员，并且没有任何操作对 destination 集合执行，那么返回 0 。</returns>
		public OperateResult<int> SetMove( string source, string destination, string member ) => OperateNumberFromServer( new string[] { "SMOVE", source, destination, member } );

		/// <summary>
		/// 移除并返回集合中的一个随机元素。如果只想获取一个随机元素，但不想该元素从集合中被移除的话，可以使用 SRANDMEMBER 命令。
		/// </summary>
		/// <param name="key">集合关键字</param>
		/// <returns>被移除的随机元素。当 key 不存在或 key 是空集时，返回 nil 。</returns>
		public OperateResult<string> SetPop( string key ) => OperateStringFromServer( new string[] { "SPOP", key } );

		/// <summary>
		/// 那么返回集合中的一个随机元素。
		/// </summary>
		/// <param name="key">集合关键字</param>
		/// <returns>返回一个元素；如果集合为空，返回 nil 。</returns>
		public OperateResult<string> SetRandomMember( string key ) => OperateStringFromServer( new string[] { "SRANDMEMBER", key } );

		/// <summary>
		/// 返回集合中的多个随机元素。<br />
		/// 如果 count 为正数，且小于集合基数，那么命令返回一个包含 count 个元素的数组，数组中的元素各不相同。如果 count 大于等于集合基数，那么返回整个集合。<br />
		/// 如果 count 为负数，那么命令返回一个数组，数组中的元素可能会重复出现多次，而数组的长度为 count 的绝对值。
		/// </summary>
		/// <param name="key">集合关键字</param>
		/// <param name="count">元素个数</param>
		/// <returns>返回一个数组；如果集合为空，返回空数组。</returns>
		public OperateResult<string[]> SetRandomMember( string key, int count ) => OperateStringsFromServer( new string[] { "SRANDMEMBER", key, count.ToString( ) } );

		/// <summary>
		/// 移除集合 key 中的一个元素，不存在的 member 元素会被忽略。
		/// </summary>
		/// <param name="key">集合关键字</param>
		/// <param name="member">等待移除的元素</param>
		/// <returns>被成功移除的元素的数量，不包括被忽略的元素。</returns>
		public OperateResult<int> SetRemove( string key, string member ) => SetRemove( key, new string[] { member } );

		/// <summary>
		/// 移除集合 key 中的一个或多个 member 元素，不存在的 member 元素会被忽略。
		/// </summary>
		/// <param name="key">集合关键字</param>
		/// <param name="members">等待移除的元素</param>
		/// <returns>被成功移除的元素的数量，不包括被忽略的元素。</returns>
		public OperateResult<int> SetRemove( string key, string[] members ) => OperateNumberFromServer( SoftBasic.SpliceStringArray( "SREM", key, members ) );

		/// <summary>
		/// 返回一个集合的全部成员，该集合是所有给定集合的并集。不存在的 key 被视为空集。
		/// </summary>
		/// <param name="key">集合关键字</param>
		/// <param name="unionKey">并集的集合</param>
		/// <returns>并集成员的列表。</returns>
		public OperateResult<string[]> SetUnion( string key, string unionKey ) => SetUnion( key, new string[] { unionKey } );

		/// <summary>
		/// 返回一个或多个集合的全部成员，该集合是所有给定集合的并集。不存在的 key 被视为空集。
		/// </summary>
		/// <param name="key">集合关键字</param>
		/// <param name="unionKeys">并集的集合</param>
		/// <returns>并集成员的列表。</returns>
		public OperateResult<string[]> SetUnion( string key, string[] unionKeys ) => OperateStringsFromServer( SoftBasic.SpliceStringArray( "SUNION", key, unionKeys ) );

		/// <summary>
		/// 这个命令类似于 SUNION 命令，但它将结果保存到 destination 集合，而不是简单地返回结果集。如果 destination 已经存在，则将其覆盖。destination 可以是 key 本身。
		/// </summary>
		/// <param name="destination">存储的目标集合</param>
		/// <param name="key">集合关键字</param>
		/// <param name="unionKey">并集的集合</param>
		/// <returns>结果集中的元素数量。</returns>
		public OperateResult<int> SetUnionStore( string destination, string key, string unionKey ) => SetUnionStore( destination, key, unionKey );

		/// <summary>
		/// 这个命令类似于 SUNION 命令，但它将结果保存到 destination 集合，而不是简单地返回结果集。如果 destination 已经存在，则将其覆盖。destination 可以是 key 本身。
		/// </summary>
		/// <param name="destination">存储的目标集合</param>
		/// <param name="key">集合关键字</param>
		/// <param name="unionKeys">并集的集合</param>
		/// <returns>结果集中的元素数量。</returns>
		public OperateResult<int> SetUnionStore( string destination, string key, string[] unionKeys ) => OperateNumberFromServer( SoftBasic.SpliceStringArray( "SUNIONSTORE", destination, key, unionKeys ) );

		#endregion

		#region Async Set Operate
#if !NET35 && !NET20
		/// <inheritdoc cref="SetAdd(string, string)"/>
		public async Task<OperateResult<int>> SetAddAsync( string key, string member ) => await SetAddAsync( key, new string[] { member } );

		/// <inheritdoc cref="SetAdd(string, string[])"/>
		public async Task<OperateResult<int>> SetAddAsync( string key, string[] members ) => await OperateNumberFromServerAsync( SoftBasic.SpliceStringArray( "SADD", key, members ) );

		/// <inheritdoc cref="SetCard(string)"/>
		public async Task<OperateResult<int>> SetCardAsync( string key ) => await OperateNumberFromServerAsync( new string[] { "SCARD", key } );

		/// <inheritdoc cref="SetDiff(string, string)"/>
		public async Task<OperateResult<string[]>> SetDiffAsync( string key, string diffKey ) => await SetDiffAsync( key, new string[] { diffKey } );

		/// <inheritdoc cref="SetDiff(string, string[])"/>
		public async Task<OperateResult<string[]>> SetDiffAsync( string key, string[] diffKeys ) => await OperateStringsFromServerAsync( SoftBasic.SpliceStringArray( "SDIFF", key, diffKeys ) );

		/// <inheritdoc cref="SetDiffStore(string, string, string)"/>
		public async Task<OperateResult<int>> SetDiffStoreAsync( string destination, string key, string diffKey ) => await SetDiffStoreAsync( destination, key, new string[] { diffKey } );

		/// <inheritdoc cref="SetDiffStore(string, string, string[])"/>
		public async Task<OperateResult<int>> SetDiffStoreAsync( string destination, string key, string[] diffKeys ) => await OperateNumberFromServerAsync( SoftBasic.SpliceStringArray( "SDIFFSTORE", destination, key, diffKeys ) );

		/// <inheritdoc cref="SetInter(string, string)"/>
		public async Task<OperateResult<string[]>> SetInterAsync( string key, string interKey ) => await SetInterAsync( key, new string[] { interKey } );

		/// <inheritdoc cref="SetInter(string, string[])"/>
		public async Task<OperateResult<string[]>> SetInterAsync( string key, string[] interKeys ) => await OperateStringsFromServerAsync( SoftBasic.SpliceStringArray( "SINTER", key, interKeys ) );

		/// <inheritdoc cref="SetInterStore(string, string, string)"/>
		public async Task<OperateResult<int>> SetInterStoreAsync( string destination, string key, string interKey ) => await SetInterStoreAsync( destination, key, new string[] { interKey } );

		/// <inheritdoc cref="SetInterStore(string, string, string[])"/>
		public async Task<OperateResult<int>> SetInterStoreAsync( string destination, string key, string[] interKeys ) => await OperateNumberFromServerAsync( SoftBasic.SpliceStringArray( "SINTERSTORE", destination, key, interKeys ) );

		/// <inheritdoc cref="SetIsMember(string, string)"/>
		public async Task<OperateResult<int>> SetIsMemberAsync( string key, string member ) => await OperateNumberFromServerAsync( new string[] { "SISMEMBER", key, member } );

		/// <inheritdoc cref="SetMembers(string)"/>
		public async Task<OperateResult<string[]>> SetMembersAsync( string key ) => await OperateStringsFromServerAsync( new string[] { "SMEMBERS", key } );

		/// <inheritdoc cref="SetMove(string, string, string)"/>
		public async Task<OperateResult<int>> SetMoveAsync( string source, string destination, string member ) => await OperateNumberFromServerAsync( new string[] { "SMOVE", source, destination, member } );

		/// <inheritdoc cref="SetPop(string)"/>
		public async Task<OperateResult<string>> SetPopAsync( string key ) => await OperateStringFromServerAsync( new string[] { "SPOP", key } );

		/// <inheritdoc cref="SetRandomMember(string)"/>
		public async Task<OperateResult<string>> SetRandomMemberAsync( string key ) => await OperateStringFromServerAsync( new string[] { "SRANDMEMBER", key } );

		/// <inheritdoc cref="SetRandomMember(string, int)"/>
		public async Task<OperateResult<string[]>> SetRandomMemberAsync( string key, int count ) => await OperateStringsFromServerAsync( new string[] { "SRANDMEMBER", key, count.ToString( ) } );

		/// <inheritdoc cref="SetRemove(string, string)"/>
		public async Task<OperateResult<int>> SetRemoveAsync( string key, string member ) => await SetRemoveAsync( key, new string[] { member } );

		/// <inheritdoc cref="SetRemove(string, string[])"/>
		public async Task<OperateResult<int>> SetRemoveAsync( string key, string[] members ) => await OperateNumberFromServerAsync( SoftBasic.SpliceStringArray( "SREM", key, members ) );

		/// <inheritdoc cref="SetUnion(string, string)"/>
		public async Task<OperateResult<string[]>> SetUnionAsync( string key, string unionKey ) => await SetUnionAsync( key, new string[] { unionKey } );

		/// <inheritdoc cref="SetUnion(string, string[])"/>
		public async Task<OperateResult<string[]>> SetUnionAsync( string key, string[] unionKeys ) => await OperateStringsFromServerAsync( SoftBasic.SpliceStringArray( "SUNION", key, unionKeys ) );

		/// <inheritdoc cref="SetUnionStore(string, string, string)"/>
		public async Task<OperateResult<int>> SetUnionStoreAsync( string destination, string key, string unionKey ) => await SetUnionStoreAsync( destination, key, unionKey );

		/// <inheritdoc cref="SetUnionStore(string, string, string[])"/>
		public async Task<OperateResult<int>> SetUnionStoreAsync( string destination, string key, string[] unionKeys ) => await OperateNumberFromServerAsync( SoftBasic.SpliceStringArray( "SUNIONSTORE", destination, key, unionKeys ) );
#endif
		#endregion

		#region  Sorted Set

		/// <summary>
		/// 将一个 member 元素及其 score 值加入到有序集 key 当中。如果某个 member 已经是有序集的成员，那么更新这个 member 的 score 值，并通过重新插入这个 member 元素，来保证该 member 在正确的位置上。
		/// score 值可以是整数值或双精度浮点数。<br />
		/// 如果 key 不存在，则创建一个空的有序集并执行 ZADD 操作。当 key 存在但不是有序集类型时，返回一个错误。
		/// </summary>
		/// <param name="key">有序集合的关键字</param>
		/// <param name="member">有序集合的元素</param>
		/// <param name="score">每个元素的得分</param>
		/// <returns>被成功添加的新成员的数量，不包括那些被更新的、已经存在的成员。</returns>
		public OperateResult<int> ZSetAdd( string key, string member, double score ) => ZSetAdd( key, new string[] { member }, new double[] { score } );

		/// <summary>
		/// 将一个或多个 member 元素及其 score 值加入到有序集 key 当中。如果某个 member 已经是有序集的成员，那么更新这个 member 的 score 值，并通过重新插入这个 member 元素，来保证该 member 在正确的位置上。
		/// score 值可以是整数值或双精度浮点数。<br />
		/// 如果 key 不存在，则创建一个空的有序集并执行 ZADD 操作。当 key 存在但不是有序集类型时，返回一个错误。
		/// </summary>
		/// <param name="key">有序集合的关键字</param>
		/// <param name="members">有序集合的元素</param>
		/// <param name="scores">每个元素的得分</param>
		/// <returns>被成功添加的新成员的数量，不包括那些被更新的、已经存在的成员。</returns>
		public OperateResult<int> ZSetAdd( string key, string[] members, double[] scores )
		{
			if (members.Length != scores.Length) throw new Exception( StringResources.Language.TwoParametersLengthIsNotSame );
			List<string> lists = new List<string>( );
			lists.Add( "ZADD" );
			lists.Add( key );
			for (int i = 0; i < members.Length; i++)
			{
				lists.Add( scores[i].ToString( ) );
				lists.Add( members[i] );
			}
			return OperateNumberFromServer( lists.ToArray( ) );
		}

		/// <summary>
		/// 返回有序集 key 的基数。
		/// </summary>
		/// <param name="key">有序集合的关键字</param>
		/// <returns>当 key 存在且是有序集类型时，返回有序集的基数。当 key 不存在时，返回 0 。</returns>
		public OperateResult<int> ZSetCard( string key ) => OperateNumberFromServer( new string[] { "ZCARD", key } );

		/// <summary>
		/// 返回有序集 key 中， score 值在 min 和 max 之间(默认包括 score 值等于 min 或 max )的成员的数量。
		/// </summary>
		/// <param name="key">有序集合的关键字</param>
		/// <param name="min">最小值，包含</param>
		/// <param name="max">最大值，包含</param>
		/// <returns>score 值在 min 和 max 之间的成员的数量。</returns>
		public OperateResult<int> ZSetCount( string key, double min, double max ) => OperateNumberFromServer( new string[] { "ZCOUNT", key, min.ToString( ), max.ToString( ) } );

		/// <summary>
		/// 为有序集 key 的成员 member 的 score 值加上增量 increment 。可以通过传递一个负数值 increment ，让 score 减去相应的值，比如 ZINCRBY key -5 member ，就是让 member 的 score 值减去 5 。
		/// 当 key 不存在，或 member 不是 key 的成员时， ZINCRBY key increment member 等同于 ZADD key increment member 。当 key 不是有序集类型时，返回一个错误。
		/// </summary>
		/// <param name="key">有序集合的关键字</param>
		/// <param name="member">成员名称</param>
		/// <param name="increment">增量数据，可以为负数</param>
		/// <returns>member 成员的新 score 值，以字符串形式表示。</returns>
		public OperateResult<string> ZSetIncreaseBy( string key, string member, double increment ) => OperateStringFromServer( new string[] { "ZINCRBY", key, increment.ToString( ), member } );

		/// <summary>
		/// 返回有序集 key 中，指定区间内的成员。其中成员的位置按 score 值递增(从小到大)来排序。具有相同 score 值的成员按字典序来排列。
		/// 下标参数 start 和 stop 都以 0 为底，也就是说，以 0 表示有序集第一个成员，以 1 表示有序集第二个成员，以此类推。你也可以使用负数下标，以 -1 表示最后一个成员， -2 表示倒数第二个成员，以此类推。
		/// </summary>
		/// <remarks>
		/// 超出范围的下标并不会引起错误。比如说，当 start 的值比有序集的最大下标还要大，或是 start > stop 时， ZRANGE 命令只是简单地返回一个空列表。另一方面，假如 stop 参数的值比有序集的最大下标还要大，那么 Redis 将 stop 当作最大下标来处理。
		/// 可以通过使用 WITHSCORES 选项，来让成员和它的 score 值一并返回，返回列表以 value1,score1, ..., valueN,scoreN 的格式表示。客户端库可能会返回一些更复杂的数据类型，比如数组、元组等。
		/// </remarks>
		/// <param name="key">有序集合的关键字</param>
		/// <param name="start">起始的下标</param>
		/// <param name="stop">结束的下标</param>
		/// <param name="withScore">是否带有 score 返回</param>
		/// <returns>指定区间内，根据参数 withScore 来决定是否带 score 值的有序集成员的列表。</returns>
		public OperateResult<string[]> ZSetRange( string key, int start, int stop, bool withScore = false )
		{
			if (withScore)
				return OperateStringsFromServer( new string[] { "ZRANGE", key, start.ToString( ), stop.ToString( ), "WITHSCORES" } );
			else
				return OperateStringsFromServer( new string[] { "ZRANGE", key, start.ToString( ), stop.ToString( ) } );
		}

		/// <summary>
		/// 返回有序集 key 中，所有 score 值介于 min 和 max 之间(包括等于 min 或 max )的成员。有序集成员按 score 值递增(从小到大)次序排列。
		/// min 和 max 可以是 -inf 和 +inf ，这样一来，你就可以在不知道有序集的最低和最高 score 值的情况下，使用 ZRANGEBYSCORE 这类命令。
		/// 默认情况下，区间的取值使用闭区间 (小于等于或大于等于)，你也可以通过给参数前增加 "(" 符号来使用可选的开区间 (小于或大于)。"(5"代表不包含5
		/// </summary>
		/// <param name="key">有序集合的关键字</param>
		/// <param name="min">最小值，可以为-inf，代表最高，如果为5，代表大于等于5，如果是(5，代表大于5</param>
		/// <param name="max">最大值，可以为+inf，代表最低，如果为10，代表小于等于5，如果是(10，代表小于10</param>
		/// <param name="withScore">是否带有 score 返回</param>
		/// <returns>指定区间内，带有 score 值(根据参数 withScore 来决定)的有序集成员的列表。</returns>
		public OperateResult<string[]> ZSetRangeByScore( string key, string min, string max, bool withScore = false )
		{
			if (withScore)
				return OperateStringsFromServer( new string[] { "ZRANGEBYSCORE", key, min, max, "WITHSCORES" } );
			else
				return OperateStringsFromServer( new string[] { "ZRANGEBYSCORE", key, min, max } );
		}

		/// <summary>
		/// 返回有序集 key 中成员 member 的排名。其中有序集成员按 score 值递增(从小到大)顺序排列。排名以 0 为底，也就是说， score 值最小的成员排名为 0 。
		/// </summary>
		/// <param name="key">有序集合的关键字</param>
		/// <param name="member">成员 member 的名称</param>
		/// <returns>如果 member 是有序集 key 的成员，返回 member 的排名。如果 member 不是有序集 key 的成员，返回 nil 。</returns>
		public OperateResult<int> ZSetRank( string key, string member ) => OperateNumberFromServer( new string[] { "ZRANK", key, member } );

		/// <summary>
		/// 移除有序集 key 中的指定成员，不存在的成员将被忽略。当 key 存在但不是有序集类型时，返回一个错误。
		/// </summary>
		/// <param name="key">有序集合的关键字</param>
		/// <param name="member">等待被移除的成员</param>
		/// <returns>被成功移除的成员的数量，不包括被忽略的成员。</returns>
		public OperateResult<int> ZSetRemove( string key, string member ) => ZSetRemove( key, new string[] { member } );

		/// <summary>
		/// 移除有序集 key 中的一个或多个成员，不存在的成员将被忽略。当 key 存在但不是有序集类型时，返回一个错误。
		/// </summary>
		/// <param name="key">有序集合的关键字</param>
		/// <param name="members">等待被移除的成员</param>
		/// <returns>被成功移除的成员的数量，不包括被忽略的成员。</returns>
		public OperateResult<int> ZSetRemove( string key, string[] members ) => OperateNumberFromServer( SoftBasic.SpliceStringArray( "ZREM", key, members ) );

		/// <summary>
		/// 移除有序集 key 中，指定排名(rank)区间内的所有成员。区间分别以下标参数 start 和 stop 指出，包含 start 和 stop 在内。
		/// 下标参数 start 和 stop 都以 0 为底，也就是说，以 0 表示有序集第一个成员，以 1 表示有序集第二个成员，以此类推。你也可以使用负数下标，以 -1 表示最后一个成员， -2 表示倒数第二个成员，以此类推。
		/// </summary>
		/// <param name="key">有序集合的关键</param>
		/// <param name="start">开始的下标</param>
		/// <param name="stop">结束的下标</param>
		/// <returns>被移除成员的数量。</returns>
		public OperateResult<int> ZSetRemoveRangeByRank( string key, int start, int stop ) => OperateNumberFromServer( new string[] { "ZREMRANGEBYRANK", key, start.ToString( ), stop.ToString( ) } );

		/// <summary>
		/// 移除有序集 key 中，所有 score 值介于 min 和 max 之间(包括等于 min 或 max )的成员。
		/// min 和 max 可以是 -inf 和 +inf ，这样一来，你就可以在不知道有序集的最低和最高 score 值的情况下，使用 ZRANGEBYSCORE 这类命令。
		/// 默认情况下，区间的取值使用闭区间 (小于等于或大于等于)，你也可以通过给参数前增加 "(" 符号来使用可选的开区间 (小于或大于)。例如"(5"代表不包括5
		/// </summary>
		/// <param name="key">有序集合的关键字</param>
		/// <param name="min">最小值，可以为-inf，代表最低，如果为5，代表大于等于5，如果是(5，代表大于5</param>
		/// <param name="max">最大值，可以为+inf，代表最低，如果为10，代表小于等于5，如果是(10，代表小于10</param>
		/// <returns>被移除成员的数量。</returns>
		public OperateResult<int> ZSetRemoveRangeByScore( string key, string min, string max ) => OperateNumberFromServer( new string[] { "ZREMRANGEBYSCORE", key, min, max } );

		/// <summary>
		/// 返回有序集 key 中，指定区间内的成员。其中成员的位置按 score 值递减(从大到小)来排列。具有相同 score 值的成员按字典序来排列。
		/// 下标参数 start 和 stop 都以 0 为底，也就是说，以 0 表示有序集第一个成员，以 1 表示有序集第二个成员，以此类推。你也可以使用负数下标，以 -1 表示最后一个成员， -2 表示倒数第二个成员，以此类推。
		/// </summary>
		/// <remarks>
		/// 超出范围的下标并不会引起错误。比如说，当 start 的值比有序集的最大下标还要大，或是 start > stop 时， ZRANGE 命令只是简单地返回一个空列表。另一方面，假如 stop 参数的值比有序集的最大下标还要大，那么 Redis 将 stop 当作最大下标来处理。
		/// 可以通过使用 WITHSCORES 选项，来让成员和它的 score 值一并返回，返回列表以 value1,score1, ..., valueN,scoreN 的格式表示。客户端库可能会返回一些更复杂的数据类型，比如数组、元组等。
		/// </remarks>
		/// <param name="key">有序集合的关键字</param>
		/// <param name="start">起始的下标</param>
		/// <param name="stop">结束的下标</param>
		/// <param name="withScore">是否带有 score 返回</param>
		/// <returns>指定区间内，根据参数 withScore 来决定是否带 score 值的有序集成员的列表。</returns>
		public OperateResult<string[]> ZSetReverseRange( string key, int start, int stop, bool withScore = false )
		{
			if (withScore)
				return OperateStringsFromServer( new string[] { "ZREVRANGE", key, start.ToString( ), stop.ToString( ), "WITHSCORES" } );
			else
				return OperateStringsFromServer( new string[] { "ZREVRANGE", key, start.ToString( ), stop.ToString( ) } );
		}

		/// <summary>
		/// 返回有序集 key 中，所有 score 值介于 min 和 max 之间(包括等于 min 或 max )的成员。序集成员按 score 值递减(从大到小)的次序排列。
		/// min 和 max 可以是 -inf 和 +inf ，这样一来，你就可以在不知道有序集的最低和最高 score 值的情况下，使用 ZRANGEBYSCORE 这类命令。
		/// 默认情况下，区间的取值使用闭区间 (小于等于或大于等于)，你也可以通过给参数前增加 ( 符号来使用可选的开区间 (小于或大于)。(5代表不包含5
		/// </summary>
		/// <param name="key">有序集合的关键字</param>
		/// <param name="max">最大值，可以为+inf，代表最高，如果为10，代表小于等于5，如果是(10，代表小于10</param>
		/// <param name="min">最小值，可以为-inf，代表最低，如果为5，代表大于等于5，如果是(5，代表大于5</param>
		/// <param name="withScore">是否带有 score 返回</param>
		/// <returns>指定区间内，带有 score 值(根据参数 withScore 来决定)的有序集成员的列表。</returns>
		public OperateResult<string[]> ZSetReverseRangeByScore( string key, string max, string min, bool withScore = false )
		{
			if (withScore)
				return OperateStringsFromServer( new string[] { "ZREVRANGEBYSCORE", key, max, min, "WITHSCORES" } );
			else
				return OperateStringsFromServer( new string[] { "ZREVRANGEBYSCORE", key, max, min } );
		}

		/// <summary>
		/// 返回有序集 key 中成员 member 的排名。其中有序集成员按 score 值递减(从大到小)排序。排名以 0 为底，也就是说，score 值最大的成员排名为 0 。
		/// </summary>
		/// <param name="key">有序集合的关键字</param>
		/// <param name="member">成员 member 的名称</param>
		/// <returns>如果 member 是有序集 key 的成员，返回 member 的排名。如果 member 不是有序集 key 的成员，返回 nil 。</returns>
		public OperateResult<int> ZSetReverseRank( string key, string member ) => OperateNumberFromServer( new string[] { "ZREVRANK", key, member } );

		/// <summary>
		/// 返回有序集 key 中，成员 member 的 score 值。如果 member 元素不是有序集 key 的成员，或 key 不存在，返回 nil 。
		/// </summary>
		/// <param name="key">有序集合的关键字</param>
		/// <param name="member">成员的名称</param>
		/// <returns>member 成员的 score 值，以字符串形式表示。</returns>
		public OperateResult<string> ZSetScore( string key, string member ) => OperateStringFromServer( new string[] { "ZSCORE", key, member } );

		#endregion

		#region Async Sorted Set
#if !NET35 && !NET20
		/// <inheritdoc cref="ZSetAdd(string, string, double)"/>
		public async Task<OperateResult<int>> ZSetAddAsync( string key, string member, double score ) => await ZSetAddAsync( key, new string[] { member }, new double[] { score } );

		/// <inheritdoc cref="ZSetAdd(string, string[], double[])"/>
		public async Task<OperateResult<int>> ZSetAddAsync( string key, string[] members, double[] scores )
		{
			if (members.Length != scores.Length) throw new Exception( StringResources.Language.TwoParametersLengthIsNotSame );
			List<string> lists = new List<string>( );
			lists.Add( "ZADD" );
			lists.Add( key );
			for (int i = 0; i < members.Length; i++)
			{
				lists.Add( scores[i].ToString( ) );
				lists.Add( members[i] );
			}
			return await OperateNumberFromServerAsync( lists.ToArray( ) );
		}

		/// <inheritdoc cref="ZSetCard(string)"/>
		public async Task<OperateResult<int>> ZSetCardAsync( string key ) => await OperateNumberFromServerAsync( new string[] { "ZCARD", key } );

		/// <inheritdoc cref="ZSetCount(string, double, double)"/>
		public async Task<OperateResult<int>> ZSetCountAsync( string key, double min, double max ) => await OperateNumberFromServerAsync( new string[] { "ZCOUNT", key, min.ToString( ), max.ToString( ) } );

		/// <inheritdoc cref="ZSetIncreaseBy(string, string, double)"/>
		public async Task<OperateResult<string>> ZSetIncreaseByAsync( string key, string member, double increment ) => await OperateStringFromServerAsync( new string[] { "ZINCRBY", key, increment.ToString( ), member } );

		/// <inheritdoc cref="ZSetRange(string, int, int, bool)"/>
		public async Task<OperateResult<string[]>> ZSetRangeAsync( string key, int start, int stop, bool withScore = false )
		{
			if (withScore)
				return await OperateStringsFromServerAsync( new string[] { "ZRANGE", key, start.ToString( ), stop.ToString( ), "WITHSCORES" } );
			else
				return await OperateStringsFromServerAsync( new string[] { "ZRANGE", key, start.ToString( ), stop.ToString( ) } );
		}

		/// <inheritdoc cref="ZSetRangeByScore(string, string, string, bool)"/>
		public async Task<OperateResult<string[]>> ZSetRangeByScoreAsync( string key, string min, string max, bool withScore = false )
		{
			if (withScore)
				return await OperateStringsFromServerAsync( new string[] { "ZRANGEBYSCORE", key, min, max, "WITHSCORES" } );
			else
				return await OperateStringsFromServerAsync( new string[] { "ZRANGEBYSCORE", key, min, max } );
		}

		/// <inheritdoc cref="ZSetRank(string, string)"/>
		public async Task<OperateResult<int>> ZSetRankAsync( string key, string member ) => await OperateNumberFromServerAsync( new string[] { "ZRANK", key, member } );

		/// <inheritdoc cref="ZSetRemove(string, string)"/>
		public async Task<OperateResult<int>> ZSetRemoveAsync( string key, string member ) => await ZSetRemoveAsync( key, new string[] { member } );

		/// <inheritdoc cref="ZSetRemove(string, string[])"/>
		public async Task<OperateResult<int>> ZSetRemoveAsync( string key, string[] members ) => await OperateNumberFromServerAsync( SoftBasic.SpliceStringArray( "ZREM", key, members ) );

		/// <inheritdoc cref="ZSetRemoveRangeByRank(string, int, int)"/>
		public async Task<OperateResult<int>> ZSetRemoveRangeByRankAsync( string key, int start, int stop ) => await OperateNumberFromServerAsync( new string[] { "ZREMRANGEBYRANK", key, start.ToString( ), stop.ToString( ) } );

		/// <inheritdoc cref="ZSetRemoveRangeByScore(string, string, string)"/>
		public async Task<OperateResult<int>> ZSetRemoveRangeByScoreAsync( string key, string min, string max ) => await OperateNumberFromServerAsync( new string[] { "ZREMRANGEBYSCORE", key, min, max } );

		/// <inheritdoc cref="ZSetReverseRange(string, int, int, bool)"/>
		public async Task<OperateResult<string[]>> ZSetReverseRangeAsync( string key, int start, int stop, bool withScore = false )
		{
			if (withScore)
				return await OperateStringsFromServerAsync( new string[] { "ZREVRANGE", key, start.ToString( ), stop.ToString( ), "WITHSCORES" } );
			else
				return await OperateStringsFromServerAsync( new string[] { "ZREVRANGE", key, start.ToString( ), stop.ToString( ) } );
		}

		/// <inheritdoc cref="ZSetReverseRangeByScore(string, string, string, bool)"/>
		public async Task<OperateResult<string[]>> ZSetReverseRangeByScoreAsync( string key, string max, string min, bool withScore = false )
		{
			if (withScore)
				return await OperateStringsFromServerAsync( new string[] { "ZREVRANGEBYSCORE", key, max, min, "WITHSCORES" } );
			else
				return await OperateStringsFromServerAsync( new string[] { "ZREVRANGEBYSCORE", key, max, min } );
		}

		/// <inheritdoc cref="ZSetReverseRank(string, string)"/>
		public async Task<OperateResult<int>> ZSetReverseRankAsync( string key, string member ) => await OperateNumberFromServerAsync( new string[] { "ZREVRANK", key, member } );

		/// <inheritdoc cref="ZSetScore(string, string)"/>
		public async Task<OperateResult<string>> ZSetScoreAsync( string key, string member ) => await OperateStringFromServerAsync( new string[] { "ZSCORE", key, member } );
#endif
		#endregion

		#region Reflection Read Write

		/// <summary>
		/// 从设备里读取支持Hsl特性的数据内容，
		/// 该特性为<see cref="HslRedisKeyAttribute"/>，<see cref="HslRedisListItemAttribute"/>，
		/// <see cref="HslRedisListAttribute"/>，<see cref="HslRedisHashFieldAttribute"/>
		/// 详细参考代码示例的操作说明。
		/// </summary>
		/// <typeparam name="T">自定义的数据类型对象</typeparam>
		/// <returns>包含是否成功的结果对象</returns>
		/// <example>
		/// 我们来说明下这个方法到底是怎么用的，当我们需要读取redis好几个数据的时候，我们很可能写如下的代码：
		/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Enthernet\RedisSample.cs" region="Sample1" title="基础的使用" />
		/// 总的来说，当读取的数据种类比较多的时候，读取的关键字比较多的时候，处理起来就比较的麻烦，此处推荐一个全新的写法，为了更好的对比，我们假设实现一种需求
		/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Enthernet\RedisSample.cs" region="Sample2" title="同等代码" />
		/// 为此我们只需要实现一个特性类即可。代码如下：(注意，实际是很灵活的，类型都是自动转换的)
		/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Enthernet\RedisSample.cs" region="SampleClass" title="数据类" />
		/// 当然了，异步也是一样的，异步的代码就不重复介绍了。
		/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Enthernet\RedisSample.cs" region="Sample3" title="异步示例" />
		/// </example>
		public OperateResult<T> Read<T>( ) where T : class, new() => HslReflectionHelper.Read<T>( this );

		/// <summary>
		/// 从设备里写入支持Hsl特性的数据内容，
		/// 该特性为<see cref="HslRedisKeyAttribute"/> ，<see cref="HslRedisHashFieldAttribute"/>
		/// 需要注意的是写入并不支持<see cref="HslRedisListAttribute"/>，<see cref="HslRedisListItemAttribute"/>特性，详细参考代码示例的操作说明。
		/// </summary>
		/// <typeparam name="T">自定义的数据类型对象</typeparam>
		/// <param name="data">等待写入的数据参数</param>
		/// <returns>包含是否成功的结果对象</returns>
		/// <example>
		/// <inheritdoc cref="Read{T}" path="example"/>
		/// </example>
		public OperateResult Write<T>( T data ) where T : class, new() => HslReflectionHelper.Write( data, this );
#if !NET35 && !NET20
		/// <inheritdoc cref="Read{T}"/>
		public async Task<OperateResult<T>> ReadAsync<T>( ) where T : class, new() => await HslReflectionHelper.ReadAsync<T>( this );

		/// <inheritdoc cref="Write{T}(T)"/>
		public async Task<OperateResult> WriteAsync<T>( T data ) where T : class, new() => await HslReflectionHelper.WriteAsync( data, this );
#endif
		#endregion

		#region Server Operate

		/// <summary>
		/// SAVE 命令执行一个同步保存操作，将当前 Redis 实例的所有数据快照(snapshot)以 RDB 文件的形式保存到硬盘。
		/// </summary>
		/// <returns>保存成功时返回 OK 。</returns>
		public OperateResult Save( ) => OperateStatusFromServer( new string[] { "SAVE" } );

		/// <summary>
		/// 在后台异步(Asynchronously)保存当前数据库的数据到磁盘。
		/// BGSAVE 命令执行之后立即返回 OK ，然后 Redis fork 出一个新子进程，原来的 Redis 进程(父进程)继续处理客户端请求，而子进程则负责将数据保存到磁盘，然后退出。
		/// </summary>
		/// <returns>反馈信息。</returns>
		public OperateResult SaveAsync( ) => OperateStatusFromServer( new string[] { "BGSAVE" } );

		/// <summary>
		/// 获取服务器的时间戳信息，可用于本地时间的数据同步问题
		/// </summary>
		/// <returns>带有服务器时间的结果对象</returns>
		public OperateResult<DateTime> ReadServerTime( )
		{
			OperateResult<string[]> times = OperateStringsFromServer( new string[] { "TIME" } );
			if (!times.IsSuccess) return OperateResult.CreateFailedResult<DateTime>( times );

			long timeTick = long.Parse( times.Content[0] );
			DateTime dateTime = new DateTime( 1970, 1, 1, 8, 0, 0 ).AddSeconds( timeTick );
			return OperateResult.CreateSuccessResult( dateTime );
		}

		/// <summary>
		/// 向服务器进行PING的操作，服务器会返回PONG操作
		/// </summary>
		/// <returns>是否成功</returns>
		public OperateResult Ping( ) => OperateStatusFromServer( new string[] { "PING" } );

		/// <summary>
		/// 返回当前数据库的 key 的数量。
		/// </summary>
		/// <returns>当前数据库的 key 的数量。</returns>
		public OperateResult<long> DBSize( ) => OperateLongNumberFromServer( new string[] { "DBSIZE" } );

		/// <summary>
		/// 清空当前的数据库的key信息
		/// </summary>
		/// <returns>总是返回 OK 。</returns>
		public OperateResult FlushDB( ) => OperateStatusFromServer( new string[] { "FLUSHDB" } );

		/// <summary>
		/// 修改Redis的密码信息，如果不需要密码，则传入空字符串即可
		/// </summary>
		/// <param name="password">密码信息</param>
		/// <returns>是否更新了密码信息</returns>
		public OperateResult ChangePassword(string password ) => OperateStatusFromServer( new string[] { "CONFIG", "SET", "requirepass", password } );

		#endregion

		#region Async Server Operate
#if !NET35 && !NET20
		/// <inheritdoc cref="ReadServerTime"/>
		public async Task<OperateResult<DateTime>> ReadServerTimeAsync( )
		{
			OperateResult<string[]> times = await OperateStringsFromServerAsync( new string[] { "TIME" } );
			if (!times.IsSuccess) return OperateResult.CreateFailedResult<DateTime>( times );

			long timeTick = long.Parse( times.Content[0] );
			DateTime dateTime = new DateTime( 1970, 1, 1, 8, 0, 0 ).AddSeconds( timeTick );
			return OperateResult.CreateSuccessResult( dateTime );
		}

		/// <inheritdoc cref="Ping"/>
		public async Task<OperateResult> PingAsync( ) => await OperateStringsFromServerAsync( new string[] { "PING" } );

		/// <inheritdoc cref="DBSize"/>
		public async Task<OperateResult<long>> DBSizeAsync( ) => await OperateLongNumberFromServerAsync( new string[] { "DBSIZE" } );

		/// <inheritdoc cref="FlushDB"/>
		public async Task<OperateResult> FlushDBAsync( ) => await OperateStatusFromServerAsync( new string[] { "FLUSHDB" } );

		/// <inheritdoc cref="ChangePassword(string)"/>
		public async Task<OperateResult> ChangePasswordAsync( string password ) => await OperateStatusFromServerAsync( new string[] { "CONFIG", "SET", "requirepass", password } );

#endif
		#endregion

		#region Publish

		/// <summary>
		/// 将信息 message 发送到指定的频道 channel，返回接收到信息 message 的订阅者数量。
		/// </summary>
		/// <param name="channel">频道，和关键字不是一回事</param>
		/// <param name="message">消息</param>
		/// <returns>接收到信息 message 的订阅者数量。</returns>
		public OperateResult<int> Publish( string channel, string message ) => OperateNumberFromServer( new string[] { "PUBLISH", channel, message } );

		#endregion

		#region Async Publish
#if !NET35 && !NET20
		/// <inheritdoc cref="Publish(string, string)"/>
		public async Task<OperateResult<int>> PublishAsync( string channel, string message ) => await OperateNumberFromServerAsync( new string[] { "PUBLISH", channel, message } );
#endif
		#endregion

		#region DB Block

		/// <summary>
		/// 切换到指定的数据库，数据库索引号 index 用数字值指定，以 0 作为起始索引值。默认使用 0 号数据库。
		/// </summary>
		/// <param name="db">索引值</param>
		/// <returns>是否切换成功</returns>
		public OperateResult SelectDB( int db )
		{
			OperateResult select = OperateStatusFromServer( new string[] { "SELECT", db.ToString( ) } );
			if (select.IsSuccess) dbBlock = db;

			return select;
		}

		#endregion

		#region Async DB Block
#if !NET35 && !NET20
		/// <inheritdoc cref="SelectDB(int)"/>
		public async Task<OperateResult> SelectDBAsync( int db )
		{
			OperateResult select = await OperateStatusFromServerAsync( new string[] { "SELECT", db.ToString( ) } );
			if (select.IsSuccess) dbBlock = db;

			return select;
		}
#endif
		#endregion

		#region Event Handle

		/// <summary>
		/// 当接收到Redis订阅的信息的时候触发<br />
		/// Triggered when receiving Redis subscription information
		/// </summary>
		/// <param name="topic">主题信息</param>
		/// <param name="message">数据信息</param>
		public delegate void RedisMessageReceiveDelegate( string topic, string message );

		/// <summary>
		/// 当接收到Redis订阅的信息的时候触发
		/// </summary>
		public event RedisMessageReceiveDelegate OnRedisMessageReceived;

		#endregion

		#region Subscribe Message

		private RedisSubscribe RedisSubscribeInitialize( )
		{
			RedisSubscribe subscribe = new RedisSubscribe( IpAddress, Port );
			subscribe.Password = password;
			subscribe.OnRedisMessageReceived += ( string topic, string message ) => OnRedisMessageReceived?.Invoke( topic, message );
			return subscribe;
		}

		/// <summary>
		/// 从Redis服务器订阅一个或多个主题信息<br />
		/// Subscribe to one or more topics from the redis server
		/// </summary>
		/// <param name="topic">主题信息</param>
		/// <returns>订阅结果</returns>
		public OperateResult SubscribeMessage( string topic ) => SubscribeMessage( new string[] { topic } );
#if NET20 || NET35
		/// <inheritdoc cref="SubscribeMessage(string)"/>
		public OperateResult SubscribeMessage( string[] topics )
		{
			if(redisSubscribe == null)
			{
				redisSubscribe = RedisSubscribeInitialize( );
			}
			return redisSubscribe.SubscribeMessage( topics );
		}
#else
		/// <inheritdoc cref="SubscribeMessage(string)"/>
		public OperateResult SubscribeMessage( string[] topics ) => redisSubscribe.Value.SubscribeMessage( topics );
#endif

		/// <summary>
		/// 取消订阅一个或多个主题信息，取消之后，当前的订阅数据就不在接收到。<br />
		/// Unsubscribe from multiple topic information. After cancellation, the current subscription data will not be received.
		/// </summary>
		/// <param name="topic">主题信息</param>
		/// <returns>取消订阅结果</returns>
		public OperateResult UnSubscribeMessage( string topic ) => UnSubscribeMessage( new string[] { topic } );
#if NET20 || NET35
		/// <inheritdoc cref="UnSubscribeMessage(string)"/>
		public OperateResult UnSubscribeMessage( string[] topics )
		{
			if (redisSubscribe == null)
			{
				redisSubscribe = RedisSubscribeInitialize( );
			}
			return redisSubscribe.UnSubscribeMessage( topics );
		}
#else
		/// <inheritdoc cref="UnSubscribeMessage(string)"/>
		public OperateResult UnSubscribeMessage( string[] topics ) => redisSubscribe.Value.UnSubscribeMessage( topics );
#endif
		#endregion

		#region Private Member

		private string password = string.Empty;                 // 密码信息
		private int dbBlock = 0;                                // 数据块
#if NET20 || NET35
		private RedisSubscribe redisSubscribe;                  // 订阅时候的客户端信息
#else
		private Lazy<RedisSubscribe> redisSubscribe;            // 订阅时候的客户端信息
#endif

		#endregion

		#region Object Override

		/// <inheritdoc/>
		public override string ToString( ) => $"RedisClient[{IpAddress}:{Port}]";

#endregion
	}
}
