using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HslCommunication.Algorithms.ConnectPool;
#if !NET35 && !NET20
using System.Threading.Tasks;
#endif

namespace HslCommunication.Enthernet.Redis
{
	/// <summary>
	/// <b>[商业授权]</b> Redis客户端的连接池类对象，用于共享当前的连接池，合理的动态调整连接对象，然后进行高效通信的操作，默认连接数无限大。<br />
	/// <b>[Authorization]</b> The connection pool class object of the Redis client is used to share the current connection pool, 
	/// reasonably dynamically adjust the connection object, and then perform efficient communication operations, 
	/// The default number of connections is unlimited
	/// </summary>
	/// <remarks>
	/// 本连接池的实现仅对商业授权用户开放，用于提供服务器端的与Redis的并发读写能力。使用上和普通的 <see cref="RedisClient"/> 没有区别，
	/// 但是在高并发上却高性能的多，占用的连接也更少，这一切都是连接池自动实现的。
	/// </remarks>
	public class RedisClientPool
	{
		/// <summary>
		/// 实例化一个默认的客户端连接池对象，需要指定实例Redis对象时的IP，端口，密码信息<br />
		/// To instantiate a default client connection pool object, you need to specify the IP, port, and password information when the Redis object is instantiated
		/// </summary>
		/// <param name="ipAddress">IP地址信息</param>
		/// <param name="port">端口号信息</param>
		/// <param name="password">密码，如果没有，请输入空字符串</param>
		public RedisClientPool( string ipAddress, int port, string password )
		{
			if (Authorization.asdniasnfaksndiqwhawfskhfaiw( ))
			{
				redisConnectPool = new ConnectPool<IRedisConnector>( ( ) => new IRedisConnector( ) { Redis = new RedisClient( ipAddress, port, password ) } );
				redisConnectPool.MaxConnector = int.MaxValue;
			}
			else
			{
				throw new Exception( StringResources.Language.InsufficientPrivileges );
			}
		}

		/// <summary>
		/// 实例化一个默认的客户端连接池对象，需要指定实例Redis对象时的IP，端口，密码信息，以及可以指定额外的初始化操作<br />
		/// To instantiate a default client connection pool object, you need to specify the IP, port, 
		/// and password information when the Redis object is instantiated, and you can specify additional initialization operations
		/// </summary>
		/// <param name="ipAddress">IP地址信息</param>
		/// <param name="port">端口号信息</param>
		/// <param name="password">密码，如果没有，请输入空字符串</param>
		/// <param name="initialize">额外的初始化信息，比如修改db块的信息。</param>
		public RedisClientPool( string ipAddress, int port, string password, Action<RedisClient> initialize )
		{
			if (Authorization.asdniasnfaksndiqwhawfskhfaiw( ))
			{
				redisConnectPool = new ConnectPool<IRedisConnector>( ( ) => {
					RedisClient redis = new RedisClient( ipAddress, port, password );
					initialize( redis );
					return new IRedisConnector( ) { Redis = redis }; } );
				redisConnectPool.MaxConnector = int.MaxValue;
			}
			else
			{
				throw new Exception( StringResources.Language.InsufficientPrivileges );
			}
		}

		/// <summary>
		/// 获取当前的连接池管理对象信息<br />
		/// Get current connection pool management object information
		/// </summary>
		public ConnectPool<IRedisConnector> GetRedisConnectPool => redisConnectPool;

		/// <inheritdoc cref="ConnectPool{TConnector}.MaxConnector"/>
		public int MaxConnector
		{
			get => redisConnectPool.MaxConnector;
			set => redisConnectPool.MaxConnector = value;
		}

		private OperateResult<T> ConnectPoolExecute<T>( Func<RedisClient, OperateResult<T>> exec )
		{
			if (Authorization.asdniasnfaksndiqwhawfskhfaiw( ))
			{
				IRedisConnector client = redisConnectPool.GetAvailableConnector( );
				OperateResult<T> result = exec( client.Redis );
				redisConnectPool.ReturnConnector( client );
				return result;
			}
			else
			{
				throw new Exception( StringResources.Language.InsufficientPrivileges );
			}
		}

		private OperateResult ConnectPoolExecute( Func<RedisClient, OperateResult> exec )
		{
			if (Authorization.asdniasnfaksndiqwhawfskhfaiw( ))
			{
				IRedisConnector client = redisConnectPool.GetAvailableConnector( );
				OperateResult result = exec( client.Redis );
				redisConnectPool.ReturnConnector( client );
				return result;
			}
			else
			{
				throw new Exception( StringResources.Language.InsufficientPrivileges );
			}
		}
#if !NET35 && !NET20
		private async Task<OperateResult<T>> ConnectPoolExecuteAsync<T>( Func<RedisClient, Task<OperateResult<T>>> execAsync )
		{
			if (Authorization.asdniasnfaksndiqwhawfskhfaiw( ))
			{
				IRedisConnector client = redisConnectPool.GetAvailableConnector( );
				OperateResult<T> result = await execAsync( client.Redis );
				redisConnectPool.ReturnConnector( client );
				return result;
			}
			else
			{
				throw new Exception( StringResources.Language.InsufficientPrivileges );
			}
		}

		private async Task<OperateResult> ConnectPoolExecuteAsync( Func<RedisClient, Task<OperateResult>> execAsync )
		{
			if (Authorization.asdniasnfaksndiqwhawfskhfaiw( ))
			{
				IRedisConnector client = redisConnectPool.GetAvailableConnector( );
				OperateResult result = await execAsync( client.Redis );
				redisConnectPool.ReturnConnector( client );
				return result;
			}
			else
			{
				throw new Exception( StringResources.Language.InsufficientPrivileges );
			}
		}
#endif
		#region Key Operate

		/// <inheritdoc cref="RedisClient.DeleteKey(string[])"/>
		public OperateResult<int> DeleteKey( string[] keys ) => ConnectPoolExecute( m => m.DeleteKey( keys ) );

		/// <inheritdoc cref="RedisClient.DeleteKey(string)"/>
		public OperateResult<int> DeleteKey( string key ) => ConnectPoolExecute( m => m.DeleteKey( key ) );

		/// <inheritdoc cref="RedisClient.ExistsKey(string)"/>
		public OperateResult<int> ExistsKey( string key ) => ConnectPoolExecute( m => m.ExistsKey( key ) );

		/// <inheritdoc cref="RedisClient.ExpireKey(string, int)"/>
		public OperateResult<int> ExpireKey( string key, int seconds ) => ConnectPoolExecute( m => m.ExpireKey( key, seconds ) );

		/// <inheritdoc cref="RedisClient.ReadAllKeys(string)"/>
		public OperateResult<string[]> ReadAllKeys( string pattern ) => ConnectPoolExecute( m => m.ReadAllKeys( pattern ) );

		/// <inheritdoc cref="RedisClient.MoveKey(string, int)"/>
		public OperateResult MoveKey( string key, int db ) => ConnectPoolExecute( m => m.MoveKey( key, db ) );

		/// <inheritdoc cref="RedisClient.PersistKey(string)"/>
		public OperateResult<int> PersistKey( string key ) => ConnectPoolExecute( m => m.PersistKey( key ) );

		/// <inheritdoc cref="RedisClient.ReadRandomKey"/>
		public OperateResult<string> ReadRandomKey( ) => ConnectPoolExecute( m => m.ReadRandomKey( ) );

		/// <inheritdoc cref="RedisClient.RenameKey(string, string)"/>
		public OperateResult RenameKey( string key1, string key2 ) => ConnectPoolExecute( m => m.RenameKey( key1, key2 ) );

		/// <inheritdoc cref="RedisClient.ReadKeyType(string)"/>
		public OperateResult<string> ReadKeyType( string key ) => ConnectPoolExecute( m => m.ReadKeyType( key ) );

		/// <inheritdoc cref="RedisClient.ReadKeyTTL(string)"/>
		public OperateResult<int> ReadKeyTTL( string key ) => ConnectPoolExecute( m => m.ReadKeyTTL( key ) );

		#endregion

		#region Async Key Operate
#if !NET35 && !NET20
		/// <inheritdoc cref="DeleteKey(string[])"/>
		public async Task<OperateResult<int>> DeleteKeyAsync( string[] keys ) => await ConnectPoolExecuteAsync( m => m.DeleteKeyAsync( keys ) );

		/// <inheritdoc cref="DeleteKey(string)"/>
		public async Task<OperateResult<int>> DeleteKeyAsync( string key ) => await DeleteKeyAsync( new string[] { key } );

		/// <inheritdoc cref="ExistsKey(string)"/>
		public async Task<OperateResult<int>> ExistsKeyAsync( string key ) => await ConnectPoolExecuteAsync( m => m.ExistsKeyAsync( key ) );

		/// <inheritdoc cref="ExpireKey(string,int)"/>
		public async Task<OperateResult<int>> ExpireKeyAsync( string key, int seconds ) => await ConnectPoolExecuteAsync( m => m.ExpireKeyAsync( key, seconds ) );

		/// <inheritdoc cref="ReadAllKeys(string)"/>
		public async Task<OperateResult<string[]>> ReadAllKeysAsync( string pattern ) => await ConnectPoolExecuteAsync( m => m.ReadAllKeysAsync( pattern ) );

		/// <inheritdoc cref="MoveKey(string, int)"/>
		public async Task<OperateResult> MoveKeyAsync( string key, int db ) => await ConnectPoolExecuteAsync( m => m.MoveKeyAsync( key, db ) );

		/// <inheritdoc cref="PersistKey(string)"/>
		public async Task<OperateResult<int>> PersistKeyAsync( string key ) => await ConnectPoolExecuteAsync( m => m.PersistKeyAsync( key ) );

		/// <inheritdoc cref="ReadRandomKey"/>
		public async Task<OperateResult<string>> ReadRandomKeyAsync( ) => await ConnectPoolExecuteAsync( m => m.ReadRandomKeyAsync( ) );

		/// <inheritdoc cref="RenameKey(string, string)"/>
		public async Task<OperateResult> RenameKeyAsync( string key1, string key2 ) => await ConnectPoolExecuteAsync( m => m.RenameKeyAsync( key1, key2 ) );

		///<inheritdoc cref="ReadKeyType(string)"/>
		public async Task<OperateResult<string>> ReadKeyTypeAsync( string key ) => await ConnectPoolExecuteAsync( m => m.ReadKeyTypeAsync( key ) );

		/// <inheritdoc cref="ReadKeyTTL(string)"/>
		public async Task<OperateResult<int>> ReadKeyTTLAsync( string key ) => await ConnectPoolExecuteAsync( m => m.ReadKeyTTLAsync( key ) );
#endif
		#endregion

		#region String Operate

		/// <inheritdoc cref="RedisClient.AppendKey(string, string)"/>
		public OperateResult<int> AppendKey( string key, string value ) => ConnectPoolExecute( m => m.AppendKey( key, value ) );

		/// <inheritdoc cref="RedisClient.DecrementKey(string)"/>
		public OperateResult<long> DecrementKey( string key ) => ConnectPoolExecute( m => m.DecrementKey( key ) );

		/// <inheritdoc cref="RedisClient.DecrementKey(string, long)"/>
		public OperateResult<long> DecrementKey( string key, long value ) => ConnectPoolExecute( m => m.DecrementKey( key, value ) );

		/// <inheritdoc cref="RedisClient.ReadKey(string)"/>
		public OperateResult<string> ReadKey( string key ) => ConnectPoolExecute( m => m.ReadKey( key ) );

		/// <inheritdoc cref="RedisClient.ReadKeyRange(string, int, int)"/>
		public OperateResult<string> ReadKeyRange( string key, int start, int end ) => ConnectPoolExecute( m => m.ReadKeyRange( key, start, end ) );

		/// <inheritdoc cref="RedisClient.ReadAndWriteKey(string, string)"/>
		public OperateResult<string> ReadAndWriteKey( string key, string value ) => ConnectPoolExecute( m => m.ReadAndWriteKey( key, value ) );

		/// <inheritdoc cref="RedisClient.IncrementKey(string)"/>
		public OperateResult<long> IncrementKey( string key ) => ConnectPoolExecute( m => m.IncrementKey( key ) );

		/// <inheritdoc cref="RedisClient.IncrementKey(string, long)"/>
		public OperateResult<long> IncrementKey( string key, long value ) => ConnectPoolExecute( m => m.IncrementKey( key, value ) );

		/// <inheritdoc cref="RedisClient.IncrementKey(string, float)"/>
		public OperateResult<string> IncrementKey( string key, float value ) => ConnectPoolExecute( m => m.IncrementKey( key, value ) );

		/// <inheritdoc cref="RedisClient.ReadKey(string[])"/>
		public OperateResult<string[]> ReadKey( string[] keys ) => ConnectPoolExecute( m => m.ReadKey( keys ) );

		/// <inheritdoc cref="RedisClient.WriteKey(string[], string[])"/>
		public OperateResult WriteKey( string[] keys, string[] values ) => ConnectPoolExecute( m => m.WriteKey( keys, values ) );

		/// <inheritdoc cref="RedisClient.WriteKey(string, string)"/>
		public OperateResult WriteKey( string key, string value ) => ConnectPoolExecute( m => m.WriteKey( key, value ) );

		/// <inheritdoc cref="RedisClient.WriteAndPublishKey(string, string)"/>
		public OperateResult WriteAndPublishKey( string key, string value ) => ConnectPoolExecute( m => m.WriteAndPublishKey( key, value ) );

		/// <inheritdoc cref="RedisClient.WriteExpireKey(string, string, long)"/>
		public OperateResult WriteExpireKey( string key, string value, long seconds ) => ConnectPoolExecute( m => m.WriteExpireKey( key, value, seconds ) );

		/// <inheritdoc cref="RedisClient.WriteKeyIfNotExists(string, string)"/>
		public OperateResult<int> WriteKeyIfNotExists( string key, string value ) => ConnectPoolExecute( m => m.WriteKeyIfNotExists( key, value ) );

		/// <inheritdoc cref="RedisClient.WriteKeyRange(string, string, int)"/>
		public OperateResult<int> WriteKeyRange( string key, string value, int offset ) => ConnectPoolExecute( m => m.WriteKeyRange( key, value, offset ) );

		/// <inheritdoc cref="RedisClient.ReadKeyLength(string)"/>
		public OperateResult<int> ReadKeyLength( string key ) => ConnectPoolExecute( m => m.ReadKeyLength( key ) );

		#endregion

		#region Async String Operate
#if !NET35 && !NET20
		/// <inheritdoc cref="AppendKey(string, string)"/>
		public async Task<OperateResult<int>> AppendKeyAsync( string key, string value ) => await ConnectPoolExecuteAsync( m => m.AppendKeyAsync( key, value ) );

		/// <inheritdoc cref="DecrementKey(string)"/>
		public async Task<OperateResult<long>> DecrementKeyAsync( string key ) => await ConnectPoolExecuteAsync( m => m.DecrementKeyAsync( key ) );

		/// <inheritdoc cref="DecrementKey(string, long)"/>
		public async Task<OperateResult<long>> DecrementKeyAsync( string key, long value ) => await ConnectPoolExecuteAsync( m => m.DecrementKeyAsync( key, value ) );

		/// <inheritdoc cref="ReadKey(string)"/>
		public async Task<OperateResult<string>> ReadKeyAsync( string key ) => await ConnectPoolExecuteAsync( m => m.ReadKeyAsync( key ) );

		/// <inheritdoc cref="ReadKeyRange(string, int, int)"/>
		public async Task<OperateResult<string>> ReadKeyRangeAsync( string key, int start, int end ) => await ConnectPoolExecuteAsync( m => m.ReadKeyRangeAsync( key, start, end ) );

		/// <inheritdoc cref="ReadAndWriteKey(string, string)"/>
		public async Task<OperateResult<string>> ReadAndWriteKeyAsync( string key, string value ) => await ConnectPoolExecuteAsync( m => m.ReadAndWriteKeyAsync( key, value ) );

		/// <inheritdoc cref="IncrementKey(string)"/>
		public async Task<OperateResult<long>> IncrementKeyAsync( string key ) => await ConnectPoolExecuteAsync( m => m.IncrementKeyAsync( key ) );

		/// <inheritdoc cref="IncrementKey(string, long)"/>
		public async Task<OperateResult<long>> IncrementKeyAsync( string key, long value ) => await ConnectPoolExecuteAsync( m => m.IncrementKeyAsync( key, value ) );

		/// <inheritdoc cref="IncrementKey(string, float)"/>
		public async Task<OperateResult<string>> IncrementKeyAsync( string key, float value ) => await ConnectPoolExecuteAsync( m => m.IncrementKeyAsync( key, value ) );

		/// <inheritdoc cref="ReadKey(string[])"/>
		public async Task<OperateResult<string[]>> ReadKeyAsync( string[] keys ) => await ConnectPoolExecuteAsync( m => m.ReadKeyAsync( keys ) );

		/// <inheritdoc cref="WriteKey(string[], string[])"/>
		public async Task<OperateResult> WriteKeyAsync( string[] keys, string[] values ) => await ConnectPoolExecuteAsync( m => m.WriteKeyAsync( keys, values ) );

		/// <inheritdoc cref="WriteKey(string, string)"/>
		public async Task<OperateResult> WriteKeyAsync( string key, string value ) => await ConnectPoolExecuteAsync( m => m.WriteKeyAsync( key, value ) );

		/// <inheritdoc cref="WriteAndPublishKey(string, string)"/>
		public async Task<OperateResult> WriteAndPublishKeyAsync( string key, string value ) => await ConnectPoolExecuteAsync( m => m.WriteAndPublishKeyAsync( key, value ) );

		/// <inheritdoc cref="WriteExpireKey(string, string, long)"/>
		public async Task<OperateResult> WriteExpireKeyAsync( string key, string value, long seconds ) => await ConnectPoolExecuteAsync( m => m.WriteExpireKeyAsync( key, value, seconds ) );

		/// <inheritdoc cref="WriteKeyIfNotExists(string, string)"/>
		public async Task<OperateResult<int>> WriteKeyIfNotExistsAsync( string key, string value ) => await ConnectPoolExecuteAsync( m => m.WriteKeyIfNotExistsAsync( key, value ) );

		/// <inheritdoc cref="WriteKeyRange(string, string, int)"/>
		public async Task<OperateResult<int>> WriteKeyRangeAsync( string key, string value, int offset ) => await ConnectPoolExecuteAsync( m => m.WriteKeyRangeAsync( key, value, offset ) );

		/// <inheritdoc cref="ReadKeyLength(string)"/>
		public async Task<OperateResult<int>> ReadKeyLengthAsync( string key ) => await ConnectPoolExecuteAsync( m => m.ReadKeyLengthAsync( key ) );
#endif
		#endregion

		#region List Operate

		/// <inheritdoc cref="RedisClient.ListInsertBefore(string, string, string)"/>
		public OperateResult<int> ListInsertBefore( string key, string value, string pivot ) => ConnectPoolExecute( m => m.ListInsertBefore( key, value, pivot ) );

		/// <inheritdoc cref="RedisClient.ListInsertAfter(string, string, string)"/>
		public OperateResult<int> ListInsertAfter( string key, string value, string pivot ) => ConnectPoolExecute( m => m.ListInsertAfter( key, value, pivot ) );

		/// <inheritdoc cref="RedisClient.GetListLength(string)"/>
		public OperateResult<int> GetListLength( string key ) => ConnectPoolExecute( m => m.GetListLength( key ) );

		/// <inheritdoc cref="RedisClient.ReadListByIndex(string, long)"/>
		public OperateResult<string> ReadListByIndex( string key, long index ) => ConnectPoolExecute( m => m.ReadListByIndex( key, index ) );

		/// <inheritdoc cref="RedisClient.ListLeftPop(string)"/>
		public OperateResult<string> ListLeftPop( string key ) => ConnectPoolExecute( m => m.ListLeftPop( key ) );

		/// <inheritdoc cref="RedisClient.ListLeftPush(string, string)"/>
		public OperateResult<int> ListLeftPush( string key, string value ) => ConnectPoolExecute( m => m.ListLeftPush( key, value ) );

		/// <inheritdoc cref="RedisClient.ListLeftPush(string, string[])"/>
		public OperateResult<int> ListLeftPush( string key, string[] values ) => ConnectPoolExecute( m => m.ListLeftPush( key, values ) );

		/// <inheritdoc cref="RedisClient.ListLeftPushX(string, string)"/>
		public OperateResult<int> ListLeftPushX( string key, string value ) => ConnectPoolExecute( m => m.ListLeftPushX( key, value ) );

		/// <inheritdoc cref="RedisClient.ListRange(string, long, long)"/>
		public OperateResult<string[]> ListRange( string key, long start, long stop ) => ConnectPoolExecute( m => m.ListRange( key, start, stop ) );

		/// <inheritdoc cref="RedisClient.ListRemoveElementMatch(string, long, string)"/>
		public OperateResult<int> ListRemoveElementMatch( string key, long count, string value ) => ConnectPoolExecute( m => m.ListRemoveElementMatch( key, count, value ) );

		/// <inheritdoc cref="RedisClient.ListSet(string, long, string)"/>
		public OperateResult ListSet( string key, long index, string value ) => ConnectPoolExecute( m => m.ListSet( key, index, value ) );

		/// <inheritdoc cref="RedisClient.ListTrim(string, long, long)"/>
		public OperateResult ListTrim( string key, long start, long end ) => ConnectPoolExecute( m => m.ListTrim( key, start, end ) );

		/// <inheritdoc cref="RedisClient.ListRightPop(string)"/>
		public OperateResult<string> ListRightPop( string key ) => ConnectPoolExecute( m => m.ListRightPop( key ) );

		/// <inheritdoc cref="RedisClient.ListRightPopLeftPush(string, string)"/>
		public OperateResult<string> ListRightPopLeftPush( string key1, string key2 ) => ConnectPoolExecute( m => m.ListRightPopLeftPush( key1, key2 ) );

		/// <inheritdoc cref="RedisClient.ListRightPush(string, string)"/>
		public OperateResult<int> ListRightPush( string key, string value ) => ConnectPoolExecute( m => m.ListRightPush( key, value ) );

		/// <inheritdoc cref="RedisClient.ListRightPush(string, string[])"/>
		public OperateResult<int> ListRightPush( string key, string[] values ) => ConnectPoolExecute( m => m.ListRightPush( key, values ) );

		/// <inheritdoc cref="RedisClient.ListRightPushX(string, string)"/>
		public OperateResult<int> ListRightPushX( string key, string value ) => ConnectPoolExecute( m => m.ListRightPushX( key, value ) );

		#endregion

		#region Async List Operate
#if !NET35 && !NET20
		/// <inheritdoc cref="ListInsertBefore(string, string, string)"/>
		public async Task<OperateResult<int>> ListInsertBeforeAsync( string key, string value, string pivot ) => await ConnectPoolExecuteAsync( m => m.ListInsertBeforeAsync( key, value, pivot ) );

		/// <inheritdoc cref="ListInsertAfter(string, string, string)"/>
		public async Task<OperateResult<int>> ListInsertAfterAsync( string key, string value, string pivot ) => await ConnectPoolExecuteAsync( m => m.ListInsertAfterAsync( key, value, pivot ) );

		/// <inheritdoc cref="GetListLength(string)"/>
		public async Task<OperateResult<int>> GetListLengthAsync( string key ) => await ConnectPoolExecuteAsync( m => m.GetListLengthAsync( key ) );

		/// <inheritdoc cref="ReadListByIndex(string, long)"/>
		public async Task<OperateResult<string>> ReadListByIndexAsync( string key, long index ) => await ConnectPoolExecuteAsync( m => m.ReadListByIndexAsync( key, index ) );

		/// <inheritdoc cref="ListLeftPop(string)"/>
		public async Task<OperateResult<string>> ListLeftPopAsync( string key ) => await ConnectPoolExecuteAsync( m => m.ListLeftPopAsync( key ) );

		/// <inheritdoc cref="ListLeftPush(string, string)"/>
		public async Task<OperateResult<int>> ListLeftPushAsync( string key, string value ) => await ListLeftPushAsync( key, new string[] { value } );

		/// <inheritdoc cref="ListLeftPush(string, string[])"/>
		public async Task<OperateResult<int>> ListLeftPushAsync( string key, string[] values ) => await ConnectPoolExecuteAsync( m => m.ListLeftPushAsync( key, values ) );

		/// <inheritdoc cref="ListLeftPushX(string, string)"/>
		public async Task<OperateResult<int>> ListLeftPushXAsync( string key, string value ) => await ConnectPoolExecuteAsync( m => m.ListLeftPushXAsync( key, value ) );

		/// <inheritdoc cref="ListRange(string, long, long)"/>
		public async Task<OperateResult<string[]>> ListRangeAsync( string key, long start, long stop ) => await ConnectPoolExecuteAsync( m => m.ListRangeAsync( key, start, stop ) );

		/// <inheritdoc cref="ListRemoveElementMatch(string, long, string)"/>
		public async Task<OperateResult<int>> ListRemoveElementMatchAsync( string key, long count, string value ) => await ConnectPoolExecuteAsync( m => m.ListRemoveElementMatchAsync( key, count, value ) );

		/// <inheritdoc cref="ListSet(string, long, string)"/>
		public async Task<OperateResult> ListSetAsync( string key, long index, string value ) => await ConnectPoolExecuteAsync( m => m.ListSetAsync( key, index, value ) );

		/// <inheritdoc cref="ListTrim(string, long, long)"/>
		public async Task<OperateResult> ListTrimAsync( string key, long start, long end ) => await ConnectPoolExecuteAsync( m => m.ListTrimAsync( key, start, end ) );

		/// <inheritdoc cref="ListRightPop(string)"/>
		public async Task<OperateResult<string>> ListRightPopAsync( string key ) => await ConnectPoolExecuteAsync( m => m.ListRightPopAsync( key ) );

		/// <inheritdoc cref="ListRightPopLeftPush(string, string)"/>
		public async Task<OperateResult<string>> ListRightPopLeftPushAsync( string key1, string key2 ) => await ConnectPoolExecuteAsync( m => m.ListRightPopLeftPushAsync( key1, key2 ) );

		/// <inheritdoc cref="ListRightPush(string, string)"/>
		public async Task<OperateResult<int>> ListRightPushAsync( string key, string value ) => await ListRightPushAsync( key, new string[] { value } );

		/// <inheritdoc cref="ListRightPush(string, string[])"/>
		public async Task<OperateResult<int>> ListRightPushAsync( string key, string[] values ) => await ConnectPoolExecuteAsync( m => m.ListRightPushAsync( key, values ) );

		/// <inheritdoc cref="ListRightPushX(string, string)"/>
		public async Task<OperateResult<int>> ListRightPushXAsync( string key, string value ) => await ConnectPoolExecuteAsync( m => m.ListRightPushXAsync( key, value ) );
#endif
		#endregion

		#region Hash Operate

		/// <inheritdoc cref="RedisClient.DeleteHashKey(string, string)"/>
		public OperateResult<int> DeleteHashKey( string key, string field ) => ConnectPoolExecute( m => m.DeleteHashKey( key, field ) );

		/// <inheritdoc cref="RedisClient.DeleteHashKey(string, string[])"/>
		public OperateResult<int> DeleteHashKey( string key, string[] fields ) => ConnectPoolExecute( m => m.DeleteHashKey( key, fields ) );

		/// <inheritdoc cref="RedisClient.ExistsHashKey(string, string)"/>
		public OperateResult<int> ExistsHashKey( string key, string field ) => ConnectPoolExecute( m => m.ExistsHashKey( key, field ) );

		/// <inheritdoc cref="RedisClient.ReadHashKey(string, string)"/>
		public OperateResult<string> ReadHashKey( string key, string field ) => ConnectPoolExecute( m => m.ReadHashKey( key, field ) );

		/// <inheritdoc cref="RedisClient.ReadHashKeyAll(string)"/>
		public OperateResult<string[]> ReadHashKeyAll( string key ) => ConnectPoolExecute( m => m.ReadHashKeyAll( key ) );

		/// <inheritdoc cref="RedisClient.IncrementHashKey(string, string, long)"/>
		public OperateResult<long> IncrementHashKey( string key, string field, long value ) => ConnectPoolExecute( m => m.IncrementHashKey( key, field, value ) );

		/// <inheritdoc cref="RedisClient.IncrementHashKey(string, string, float)"/>
		public OperateResult<string> IncrementHashKey( string key, string field, float value ) => ConnectPoolExecute( m => m.IncrementHashKey( key, field, value ) );

		/// <inheritdoc cref="RedisClient.ReadHashKeys(string)"/>
		public OperateResult<string[]> ReadHashKeys( string key ) => ConnectPoolExecute( m => m.ReadHashKeys( key ) );

		/// <inheritdoc cref="RedisClient.ReadHashKeyLength(string)"/>
		public OperateResult<int> ReadHashKeyLength( string key ) => ConnectPoolExecute( m => m.ReadHashKeyLength( key ) );

		/// <inheritdoc cref="RedisClient.ReadHashKey(string, string[])"/>
		public OperateResult<string[]> ReadHashKey( string key, string[] fields ) => ConnectPoolExecute( m => m.ReadHashKey( key, fields ) );

		/// <inheritdoc cref="RedisClient.WriteHashKey(string, string, string)"/>
		public OperateResult<int> WriteHashKey( string key, string field, string value ) => ConnectPoolExecute( m => m.WriteHashKey( key, field, value ) );

		/// <inheritdoc cref="RedisClient.WriteHashKey(string, string[], string[])"/>
		public OperateResult WriteHashKey( string key, string[] fields, string[] values ) => ConnectPoolExecute( m => m.WriteHashKey( key, fields, values ) );

		/// <inheritdoc cref="RedisClient.WriteHashKeyNx(string, string, string)"/>
		public OperateResult<int> WriteHashKeyNx( string key, string field, string value ) => ConnectPoolExecute( m => m.WriteHashKeyNx( key, field, value ) );

		/// <inheritdoc cref="RedisClient.ReadHashValues(string)"/>
		public OperateResult<string[]> ReadHashValues( string key ) => ConnectPoolExecute( m => m.ReadHashValues( key ) );

		#endregion

		#region Async Hash Operate
#if !NET35 && !NET20
		/// <inheritdoc cref="DeleteHashKey(string, string)"/>
		public async Task<OperateResult<int>> DeleteHashKeyAsync( string key, string field ) => await DeleteHashKeyAsync( key, new string[] { field } );

		/// <inheritdoc cref="DeleteHashKey(string, string[])"/>
		public async Task<OperateResult<int>> DeleteHashKeyAsync( string key, string[] fields ) => await ConnectPoolExecuteAsync( m => m.DeleteHashKeyAsync( key, fields ) );

		/// <inheritdoc cref="ExistsHashKey(string, string)"/>
		public async Task<OperateResult<int>> ExistsHashKeyAsync( string key, string field ) => await ConnectPoolExecuteAsync( m => m.ExistsHashKeyAsync( key, field ) );

		/// <inheritdoc cref="ReadHashKey(string, string)"/>
		public async Task<OperateResult<string>> ReadHashKeyAsync( string key, string field ) => await ConnectPoolExecuteAsync( m => m.ReadHashKeyAsync( key, field ) );

		/// <inheritdoc cref="ReadHashKeyAll(string)"/>
		public async Task<OperateResult<string[]>> ReadHashKeyAllAsync( string key ) => await ConnectPoolExecuteAsync( m => m.ReadHashKeyAllAsync( key ) );

		/// <inheritdoc cref="IncrementHashKey(string, string, long)"/>
		public async Task<OperateResult<long>> IncrementHashKeyAsync( string key, string field, long value ) => await ConnectPoolExecuteAsync( m => m.IncrementHashKeyAsync( key, field, value ) );

		/// <inheritdoc cref="IncrementHashKey(string, string, float)"/>
		public async Task<OperateResult<string>> IncrementHashKeyAsync( string key, string field, float value ) => await ConnectPoolExecuteAsync( m => m.IncrementHashKeyAsync( key, field, value ) );

		/// <inheritdoc cref="ReadHashKeys(string)"/>
		public async Task<OperateResult<string[]>> ReadHashKeysAsync( string key ) => await ConnectPoolExecuteAsync( m => m.ReadHashKeysAsync( key ) );

		/// <inheritdoc cref="ReadHashKeyLength(string)"/>
		public async Task<OperateResult<int>> ReadHashKeyLengthAsync( string key ) => await ConnectPoolExecuteAsync( m => m.ReadHashKeyLengthAsync( key ) );

		/// <inheritdoc cref="ReadHashKey(string, string[])"/>
		public async Task<OperateResult<string[]>> ReadHashKeyAsync( string key, string[] fields ) => await ConnectPoolExecuteAsync( m => m.ReadHashKeyAsync( key, fields ) );

		/// <inheritdoc cref="WriteHashKey(string, string, string)"/>
		public async Task<OperateResult<int>> WriteHashKeyAsync( string key, string field, string value ) => await ConnectPoolExecuteAsync( m => m.WriteHashKeyAsync( key, field, value ) );

		/// <inheritdoc cref="WriteHashKey(string, string[], string[])"/>
		public async Task<OperateResult> WriteHashKeyAsync( string key, string[] fields, string[] values ) => await ConnectPoolExecuteAsync( m => m.WriteHashKeyAsync( key, fields, values ) );

		/// <inheritdoc cref="WriteHashKeyNx(string, string, string)"/>
		public async Task<OperateResult<int>> WriteHashKeyNxAsync( string key, string field, string value ) => await ConnectPoolExecuteAsync( m => m.WriteHashKeyNxAsync( key, field, value ) );

		/// <inheritdoc cref="ReadHashValues(string)"/>
		public async Task<OperateResult<string[]>> ReadHashValuesAsync( string key ) => await ConnectPoolExecuteAsync( m => m.ReadHashValuesAsync( key ) );
#endif
		#endregion

		#region Set Operate

		/// <inheritdoc cref="RedisClient.SetAdd(string, string)"/>
		public OperateResult<int> SetAdd( string key, string member ) => ConnectPoolExecute( m => m.SetAdd( key, member ) );

		/// <inheritdoc cref="RedisClient.SetAdd(string, string[])"/>
		public OperateResult<int> SetAdd( string key, string[] members ) => ConnectPoolExecute( m => m.SetAdd( key, members ) );

		/// <inheritdoc cref="RedisClient.SetCard(string)"/>
		public OperateResult<int> SetCard( string key ) => ConnectPoolExecute( m => m.SetCard( key ) );

		/// <inheritdoc cref="RedisClient.SetDiff(string, string)"/>
		public OperateResult<string[]> SetDiff( string key, string diffKey ) => ConnectPoolExecute( m => m.SetDiff( key, diffKey ) );

		/// <inheritdoc cref="RedisClient.SetDiff(string, string[])"/>
		public OperateResult<string[]> SetDiff( string key, string[] diffKeys ) => ConnectPoolExecute( m => m.SetDiff( key, diffKeys ) );

		/// <inheritdoc cref="RedisClient.SetDiffStore(string, string, string)"/>
		public OperateResult<int> SetDiffStore( string destination, string key, string diffKey ) => ConnectPoolExecute( m => m.SetDiffStore( destination, key, diffKey ) );

		/// <inheritdoc cref="RedisClient.SetDiffStore(string, string, string[])"/>
		public OperateResult<int> SetDiffStore( string destination, string key, string[] diffKeys ) => ConnectPoolExecute( m => m.SetDiffStore( destination, key, diffKeys ) );

		/// <inheritdoc cref="RedisClient.SetInter(string, string)"/>
		public OperateResult<string[]> SetInter( string key, string interKey ) => ConnectPoolExecute( m => m.SetInter( key, interKey) );

		/// <inheritdoc cref="RedisClient.SetInter(string, string[])"/>
		public OperateResult<string[]> SetInter( string key, string[] interKeys ) => ConnectPoolExecute( m => m.SetInter( key, interKeys ) );

		/// <inheritdoc cref="RedisClient.SetInterStore(string, string, string)"/>
		public OperateResult<int> SetInterStore( string destination, string key, string interKey ) => ConnectPoolExecute( m => m.SetInterStore( destination, key, interKey ) );

		/// <inheritdoc cref="RedisClient.SetInterStore(string, string, string[])"/>
		public OperateResult<int> SetInterStore( string destination, string key, string[] interKeys ) => ConnectPoolExecute( m => m.SetInterStore( destination, key, interKeys ) );

		/// <inheritdoc cref="RedisClient.SetIsMember(string, string)"/>
		public OperateResult<int> SetIsMember( string key, string member ) => ConnectPoolExecute( m => m.SetIsMember( key, member ) );

		/// <inheritdoc cref="RedisClient.SetMembers(string)"/>
		public OperateResult<string[]> SetMembers( string key ) => ConnectPoolExecute( m => m.SetMembers( key ) );

		/// <inheritdoc cref="RedisClient.SetMove(string, string, string)"/>
		public OperateResult<int> SetMove( string source, string destination, string member ) => ConnectPoolExecute( m => m.SetMove( source, destination, member ) );

		/// <inheritdoc cref="RedisClient.SetPop(string)"/>
		public OperateResult<string> SetPop( string key ) => ConnectPoolExecute( m => m.SetPop( key ) );

		/// <inheritdoc cref="RedisClient.SetRandomMember(string)"/>
		public OperateResult<string> SetRandomMember( string key ) => ConnectPoolExecute( m => m.SetRandomMember( key ) );

		/// <inheritdoc cref="RedisClient.SetRandomMember(string, int)"/>
		public OperateResult<string[]> SetRandomMember( string key, int count ) => ConnectPoolExecute( m => m.SetRandomMember( key, count ) );

		/// <inheritdoc cref="RedisClient.SetRemove(string, string)"/>
		public OperateResult<int> SetRemove( string key, string member ) => ConnectPoolExecute( m => m.SetRemove( key, member ) );

		/// <inheritdoc cref="RedisClient.SetRemove(string, string[])"/>
		public OperateResult<int> SetRemove( string key, string[] members ) => ConnectPoolExecute( m => m.SetRemove( key, members ) );

		/// <inheritdoc cref="RedisClient.SetUnion(string, string)"/>
		public OperateResult<string[]> SetUnion( string key, string unionKey ) => ConnectPoolExecute( m => m.SetUnion( key, unionKey ) );

		/// <inheritdoc cref="RedisClient.SetUnion(string, string[])"/>
		public OperateResult<string[]> SetUnion( string key, string[] unionKeys ) => ConnectPoolExecute( m => m.SetUnion( key, unionKeys ) );

		/// <inheritdoc cref="RedisClient.SetUnionStore(string, string, string)"/>
		public OperateResult<int> SetUnionStore( string destination, string key, string unionKey ) => ConnectPoolExecute( m => m.SetUnionStore( destination, key, unionKey ) );

		/// <inheritdoc cref="RedisClient.SetUnionStore(string, string, string[])"/>
		public OperateResult<int> SetUnionStore( string destination, string key, string[] unionKeys ) => ConnectPoolExecute( m => m.SetUnionStore( destination, key, unionKeys ) );

		#endregion

		#region Async Set Operate
#if !NET35 && !NET20
		/// <inheritdoc cref="SetAdd(string, string)"/>
		public async Task<OperateResult<int>> SetAddAsync( string key, string member ) => await ConnectPoolExecuteAsync( m => m.SetAddAsync( key, member ) );

		/// <inheritdoc cref="SetAdd(string, string[])"/>
		public async Task<OperateResult<int>> SetAddAsync( string key, string[] members ) => await ConnectPoolExecuteAsync( m => m.SetAddAsync( key, members ) );

		/// <inheritdoc cref="SetCard(string)"/>
		public async Task<OperateResult<int>> SetCardAsync( string key ) => await ConnectPoolExecuteAsync( m => m.SetCardAsync( key ) );

		/// <inheritdoc cref="SetDiff(string, string)"/>
		public async Task<OperateResult<string[]>> SetDiffAsync( string key, string diffKey ) => await ConnectPoolExecuteAsync( m => m.SetDiffAsync( key, diffKey ) );

		/// <inheritdoc cref="SetDiff(string, string[])"/>
		public async Task<OperateResult<string[]>> SetDiffAsync( string key, string[] diffKeys ) => await ConnectPoolExecuteAsync( m => m.SetDiffAsync( key, diffKeys ) );

		/// <inheritdoc cref="SetDiffStore(string, string, string)"/>
		public async Task<OperateResult<int>> SetDiffStoreAsync( string destination, string key, string diffKey ) => await ConnectPoolExecuteAsync( m => m.SetDiffStoreAsync( destination, key, diffKey ) );

		/// <inheritdoc cref="SetDiffStore(string, string, string[])"/>
		public async Task<OperateResult<int>> SetDiffStoreAsync( string destination, string key, string[] diffKeys ) => await ConnectPoolExecuteAsync( m => m.SetDiffStoreAsync( destination, key, diffKeys ) );

		/// <inheritdoc cref="SetInter(string, string)"/>
		public async Task<OperateResult<string[]>> SetInterAsync( string key, string interKey ) => await ConnectPoolExecuteAsync( m => m.SetInterAsync( key, interKey ) );

		/// <inheritdoc cref="SetInter(string, string[])"/>
		public async Task<OperateResult<string[]>> SetInterAsync( string key, string[] interKeys ) => await ConnectPoolExecuteAsync( m => m.SetInterAsync( key, interKeys ) );

		/// <inheritdoc cref="SetInterStore(string, string, string)"/>
		public async Task<OperateResult<int>> SetInterStoreAsync( string destination, string key, string interKey ) => await ConnectPoolExecuteAsync( m => m.SetInterStoreAsync( destination, key, interKey ) );

		/// <inheritdoc cref="SetInterStore(string, string, string[])"/>
		public async Task<OperateResult<int>> SetInterStoreAsync( string destination, string key, string[] interKeys ) => await ConnectPoolExecuteAsync( m => m.SetInterStoreAsync( destination, key, interKeys ) );

		/// <inheritdoc cref="SetIsMember(string, string)"/>
		public async Task<OperateResult<int>> SetIsMemberAsync( string key, string member ) => await ConnectPoolExecuteAsync( m => m.SetIsMemberAsync( key, member ) );

		/// <inheritdoc cref="SetMembers(string)"/>
		public async Task<OperateResult<string[]>> SetMembersAsync( string key ) => await ConnectPoolExecuteAsync( m => m.SetMembersAsync( key ) );

		/// <inheritdoc cref="SetMove(string, string, string)"/>
		public async Task<OperateResult<int>> SetMoveAsync( string source, string destination, string member ) => await ConnectPoolExecuteAsync( m => m.SetMoveAsync( source, destination, member ) );

		/// <inheritdoc cref="SetPop(string)"/>
		public async Task<OperateResult<string>> SetPopAsync( string key ) => await ConnectPoolExecuteAsync( m => m.SetPopAsync( key ) );

		/// <inheritdoc cref="SetRandomMember(string)"/>
		public async Task<OperateResult<string>> SetRandomMemberAsync( string key ) => await ConnectPoolExecuteAsync( m => m.SetRandomMemberAsync( key ) );

		/// <inheritdoc cref="SetRandomMember(string, int)"/>
		public async Task<OperateResult<string[]>> SetRandomMemberAsync( string key, int count ) => await ConnectPoolExecuteAsync( m => m.SetRandomMemberAsync( key, count ) );

		/// <inheritdoc cref="SetRemove(string, string)"/>
		public async Task<OperateResult<int>> SetRemoveAsync( string key, string member ) => await ConnectPoolExecuteAsync( m => m.SetRemoveAsync( key, member ) );

		/// <inheritdoc cref="SetRemove(string, string[])"/>
		public async Task<OperateResult<int>> SetRemoveAsync( string key, string[] members ) => await ConnectPoolExecuteAsync( m => m.SetRemoveAsync( key, members ) );

		/// <inheritdoc cref="SetUnion(string, string)"/>
		public async Task<OperateResult<string[]>> SetUnionAsync( string key, string unionKey ) => await ConnectPoolExecuteAsync( m => m.SetUnionAsync( key, unionKey ) );

		/// <inheritdoc cref="SetUnion(string, string[])"/>
		public async Task<OperateResult<string[]>> SetUnionAsync( string key, string[] unionKeys ) => await ConnectPoolExecuteAsync( m => m.SetUnionAsync( key, unionKeys ) );

		/// <inheritdoc cref="SetUnionStore(string, string, string)"/>
		public async Task<OperateResult<int>> SetUnionStoreAsync( string destination, string key, string unionKey ) => await ConnectPoolExecuteAsync( m => m.SetUnionStoreAsync( destination, key, unionKey ) );

		/// <inheritdoc cref="SetUnionStore(string, string, string[])"/>
		public async Task<OperateResult<int>> SetUnionStoreAsync( string destination, string key, string[] unionKeys ) => await ConnectPoolExecuteAsync( m => m.SetUnionStoreAsync( destination, key, unionKeys ) );
#endif
		#endregion

		#region  Sorted Set

		/// <inheritdoc cref="RedisClient.ZSetAdd(string, string, double)"/>
		public OperateResult<int> ZSetAdd( string key, string member, double score ) => ConnectPoolExecute( m => m.ZSetAdd( key, member, score ) );

		/// <inheritdoc cref="RedisClient.ZSetAdd(string, string[], double[])"/>
		public OperateResult<int> ZSetAdd( string key, string[] members, double[] scores ) => ConnectPoolExecute( m => m.ZSetAdd( key, members, scores ) );

		/// <inheritdoc cref="RedisClient.ZSetCard(string)"/>
		public OperateResult<int> ZSetCard( string key ) => ConnectPoolExecute( m => m.ZSetCard( key ) );

		/// <inheritdoc cref="RedisClient.ZSetCount(string, double, double)"/>
		public OperateResult<int> ZSetCount( string key, double min, double max ) => ConnectPoolExecute( m => m.ZSetCount( key, min, max ) );

		/// <inheritdoc cref="RedisClient.ZSetIncreaseBy(string, string, double)"/>
		public OperateResult<string> ZSetIncreaseBy( string key, string member, double increment ) => ConnectPoolExecute( m => m.ZSetIncreaseBy( key, member, increment ) );

		/// <inheritdoc cref="RedisClient.ZSetRange(string, int, int, bool)"/>
		public OperateResult<string[]> ZSetRange( string key, int start, int stop, bool withScore = false ) => ConnectPoolExecute( m => m.ZSetRange( key, start, stop, withScore ) );

		/// <inheritdoc cref="RedisClient.ZSetRangeByScore(string, string, string, bool)"/>
		public OperateResult<string[]> ZSetRangeByScore( string key, string min, string max, bool withScore = false ) => ConnectPoolExecute( m => m.ZSetRangeByScore( key, min, max, withScore ) );

		/// <inheritdoc cref="RedisClient.ZSetRank(string, string)"/>
		public OperateResult<int> ZSetRank( string key, string member ) => ConnectPoolExecute( m => m.ZSetRank( key, member ) );

		/// <inheritdoc cref="RedisClient.ZSetRemove(string, string)"/>
		public OperateResult<int> ZSetRemove( string key, string member ) => ConnectPoolExecute( m => m.ZSetRemove( key, member ) );

		/// <inheritdoc cref="RedisClient.ZSetRemove(string, string[])"/>
		public OperateResult<int> ZSetRemove( string key, string[] members ) => ConnectPoolExecute( m => m.ZSetRemove( key, members ) );

		/// <inheritdoc cref="RedisClient.ZSetRemoveRangeByRank(string, int, int)"/>
		public OperateResult<int> ZSetRemoveRangeByRank( string key, int start, int stop ) => ConnectPoolExecute( m => m.ZSetRemoveRangeByRank( key, start, stop ) );

		/// <inheritdoc cref="RedisClient.ZSetRemoveRangeByScore(string, string, string)"/>
		public OperateResult<int> ZSetRemoveRangeByScore( string key, string min, string max ) => ConnectPoolExecute( m => m.ZSetRemoveRangeByScore( key, min, max ) );

		/// <inheritdoc cref="RedisClient.ZSetReverseRange(string, int, int, bool)"/>
		public OperateResult<string[]> ZSetReverseRange( string key, int start, int stop, bool withScore = false ) => ConnectPoolExecute( m => m.ZSetReverseRange( key, start, stop, withScore ) );

		/// <inheritdoc cref="RedisClient.ZSetReverseRangeByScore(string, string, string, bool)"/>
		public OperateResult<string[]> ZSetReverseRangeByScore( string key, string max, string min, bool withScore = false ) => ConnectPoolExecute( m => m.ZSetReverseRangeByScore( key, max, min, withScore ) );

		/// <inheritdoc cref="RedisClient.ZSetReverseRank(string, string)"/>
		public OperateResult<int> ZSetReverseRank( string key, string member ) => ConnectPoolExecute( m => m.ZSetReverseRank( key, member ) );

		/// <inheritdoc cref="RedisClient.ZSetScore(string, string)"/>
		public OperateResult<string> ZSetScore( string key, string member ) => ConnectPoolExecute( m => m.ZSetScore( key, member ) );

		#endregion

		#region Async Sorted Set
#if !NET35 && !NET20
		/// <inheritdoc cref="ZSetAdd(string, string, double)"/>
		public async Task<OperateResult<int>> ZSetAddAsync( string key, string member, double score ) => await ConnectPoolExecuteAsync( m => m.ZSetAddAsync( key, member, score ) );

		/// <inheritdoc cref="ZSetAdd(string, string[], double[])"/>
		public async Task<OperateResult<int>> ZSetAddAsync( string key, string[] members, double[] scores ) => await ConnectPoolExecuteAsync( m => m.ZSetAddAsync( key, members, scores ) );

		/// <inheritdoc cref="ZSetCard(string)"/>
		public async Task<OperateResult<int>> ZSetCardAsync( string key ) => await ConnectPoolExecuteAsync( m => m.ZSetCardAsync( key ) );

		/// <inheritdoc cref="ZSetCount(string, double, double)"/>
		public async Task<OperateResult<int>> ZSetCountAsync( string key, double min, double max ) => await ConnectPoolExecuteAsync( m => m.ZSetCountAsync( key, min, max ) );

		/// <inheritdoc cref="ZSetIncreaseBy(string, string, double)"/>
		public async Task<OperateResult<string>> ZSetIncreaseByAsync( string key, string member, double increment ) => await ConnectPoolExecuteAsync( m => m.ZSetIncreaseByAsync( key, member, increment ) );

		/// <inheritdoc cref="ZSetRange(string, int, int, bool)"/>
		public async Task<OperateResult<string[]>> ZSetRangeAsync( string key, int start, int stop, bool withScore = false ) => await ConnectPoolExecuteAsync( m => m.ZSetRangeAsync( key, start, stop, withScore ) );

		/// <inheritdoc cref="ZSetRangeByScore(string, string, string, bool)"/>
		public async Task<OperateResult<string[]>> ZSetRangeByScoreAsync( string key, string min, string max, bool withScore = false ) => await ConnectPoolExecuteAsync( m => m.ZSetRangeByScoreAsync( key, min, max, withScore ) );

		/// <inheritdoc cref="ZSetRank(string, string)"/>
		public async Task<OperateResult<int>> ZSetRankAsync( string key, string member ) => await ConnectPoolExecuteAsync( m => m.ZSetRankAsync( key, member ) );

		/// <inheritdoc cref="ZSetRemove(string, string)"/>
		public async Task<OperateResult<int>> ZSetRemoveAsync( string key, string member ) => await ConnectPoolExecuteAsync( m => m.ZSetRemoveAsync( key, member ) );

		/// <inheritdoc cref="ZSetRemove(string, string[])"/>
		public async Task<OperateResult<int>> ZSetRemoveAsync( string key, string[] members ) => await ConnectPoolExecuteAsync( m => m.ZSetRemoveAsync( key, members ) );

		/// <inheritdoc cref="ZSetRemoveRangeByRank(string, int, int)"/>
		public async Task<OperateResult<int>> ZSetRemoveRangeByRankAsync( string key, int start, int stop ) => await ConnectPoolExecuteAsync( m => m.ZSetRemoveRangeByRankAsync( key, start, stop ) );

		/// <inheritdoc cref="ZSetRemoveRangeByScore(string, string, string)"/>
		public async Task<OperateResult<int>> ZSetRemoveRangeByScoreAsync( string key, string min, string max ) => await ConnectPoolExecuteAsync( m => m.ZSetRemoveRangeByScoreAsync( key, min, max ) );

		/// <inheritdoc cref="ZSetReverseRange(string, int, int, bool)"/>
		public async Task<OperateResult<string[]>> ZSetReverseRangeAsync( string key, int start, int stop, bool withScore = false ) => await ConnectPoolExecuteAsync( m => m.ZSetReverseRangeAsync( key, start, stop, withScore ) );

		/// <inheritdoc cref="ZSetReverseRangeByScore(string, string, string, bool)"/>
		public async Task<OperateResult<string[]>> ZSetReverseRangeByScoreAsync( string key, string max, string min, bool withScore = false ) => await ConnectPoolExecuteAsync( m => m.ZSetReverseRangeByScoreAsync( key, max, min, withScore ) );

		/// <inheritdoc cref="ZSetReverseRank(string, string)"/>
		public async Task<OperateResult<int>> ZSetReverseRankAsync( string key, string member ) => await ConnectPoolExecuteAsync( m => m.ZSetReverseRankAsync( key, member ) );

		/// <inheritdoc cref="ZSetScore(string, string)"/>
		public async Task<OperateResult<string>> ZSetScoreAsync( string key, string member ) => await ConnectPoolExecuteAsync( m => m.ZSetScoreAsync( key, member ) );
#endif
		#endregion

		#region Reflection Read Write

		/// <inheritdoc cref="RedisClient.Read{T}"/>
		public OperateResult<T> Read<T>( ) where T : class, new() => ConnectPoolExecute( m => m.Read<T>( ) );

		/// <inheritdoc cref="RedisClient.Write{T}(T)"/>
		public OperateResult Write<T>( T data ) where T : class, new() => ConnectPoolExecute( m => m.Write( data ) );
#if !NET35 && !NET20
		/// <inheritdoc cref="Read{T}"/>
		public async Task<OperateResult<T>> ReadAsync<T>( ) where T : class, new() => await ConnectPoolExecuteAsync( m => m.ReadAsync<T>( ) );

		/// <inheritdoc cref="Write{T}(T)"/>
		public async Task<OperateResult> WriteAsync<T>( T data ) where T : class, new() => await ConnectPoolExecuteAsync( m => m.WriteAsync( data ) );
#endif
		#endregion

		#region Server Operate

		/// <inheritdoc cref="RedisClient.Save"/>
		public OperateResult Save( ) => ConnectPoolExecute( m => m.Save( ) );

		/// <inheritdoc cref="RedisClient.SaveAsync"/>
		public OperateResult SaveAsync( ) => ConnectPoolExecute( m => m.SaveAsync( ) );

		/// <inheritdoc cref="RedisClient.ReadServerTime"/>
		public OperateResult<DateTime> ReadServerTime( ) => ConnectPoolExecute( m => m.ReadServerTime( ) );

		/// <inheritdoc cref="RedisClient.Ping"/>
		public OperateResult Ping( ) => ConnectPoolExecute( m => m.Ping( ) );

		/// <inheritdoc cref="RedisClient.DBSize"/>
		public OperateResult<long> DBSize( ) => ConnectPoolExecute( m => m.DBSize( ) );

		/// <inheritdoc cref="RedisClient.FlushDB"/>
		public OperateResult FlushDB( ) => ConnectPoolExecute( m => m.FlushDB( ) );

		/// <inheritdoc cref="RedisClient.ChangePassword(string)"/>
		public OperateResult ChangePassword( string password ) => ConnectPoolExecute( m => m.ChangePassword( password ) );

		#endregion

		#region Async Server Operate
#if !NET35 && !NET20
		/// <inheritdoc cref="ReadServerTime"/>
		public async Task<OperateResult<DateTime>> ReadServerTimeAsync( ) => await ConnectPoolExecuteAsync( m => m.ReadServerTimeAsync( ) );

		/// <inheritdoc cref="Ping"/>
		public async Task<OperateResult> PingAsync( ) => await ConnectPoolExecuteAsync( m => m.PingAsync( ) );

		/// <inheritdoc cref="DBSize"/>
		public async Task<OperateResult<long>> DBSizeAsync( ) => await ConnectPoolExecuteAsync( m => m.DBSizeAsync( ) );

		/// <inheritdoc cref="FlushDB"/>
		public async Task<OperateResult> FlushDBAsync( ) => await ConnectPoolExecuteAsync( m => m.FlushDBAsync( ) );

		/// <inheritdoc cref="ChangePassword(string)"/>
		public async Task<OperateResult> ChangePasswordAsync( string password ) => await ConnectPoolExecuteAsync( m => m.ChangePasswordAsync( password ) );

#endif
		#endregion

		#region Publish

		/// <inheritdoc cref="RedisClient.Publish(string, string)"/>
		public OperateResult<int> Publish( string channel, string message ) => ConnectPoolExecute( m => m.Publish( channel, message ) );

		#endregion

		#region Async Publish
#if !NET35 && !NET20
		/// <inheritdoc cref="Publish(string, string)"/>
		public async Task<OperateResult<int>> PublishAsync( string channel, string message ) => await ConnectPoolExecuteAsync( m => m.PublishAsync( channel, message ) );
#endif
		#endregion

		#region DB Block

		/// <inheritdoc cref="RedisClient.SelectDB(int)"/>
		public OperateResult SelectDB( int db ) => ConnectPoolExecute( m => m.SelectDB( db ) );

		#endregion

		#region Async DB Block
#if !NET35 && !NET20
		/// <inheritdoc cref="SelectDB(int)"/>
		public async Task<OperateResult> SelectDBAsync( int db ) => await ConnectPoolExecuteAsync( m => m.SelectDBAsync( db ) );
#endif
		#endregion

		#region Private Member

		private ConnectPool<IRedisConnector> redisConnectPool;                           // 连接池对象

		#endregion

		#region Object Override

		/// <inheritdoc/>
		public override string ToString( ) => $"RedisConnectPool[{redisConnectPool.MaxConnector}]";

		#endregion
	}
}
