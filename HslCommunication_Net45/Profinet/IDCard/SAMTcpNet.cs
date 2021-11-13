using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HslCommunication;
using HslCommunication.Core.IMessage;
using HslCommunication.Core.Net;
using HslCommunication.Core;
using HslCommunication.Reflection;
#if !NET35 && !NET20
using System.Threading.Tasks;
#endif

namespace HslCommunication.Profinet.IDCard
{
	/// <summary>
	/// 基于SAM协议的Tcp实现的网络类，支持读取身份证的数据信息，通过透传的形式实现，除了初始化和串口类不一致，调用方法是几乎一模一样的，详细参见API文档<br />
	/// The network class implemented by Tcp based on the SAM protocol supports reading ID card data information and is implemented in the form of transparent transmission. 
	/// Except for the inconsistency between the initialization and the serial port class, the calling method is almost the same. 
	/// See the API documentation for details
	/// </summary>
	/// <example>
	/// 在使用之前需要实例化当前的对象，然后根据实际的情况填写好串口的信息，否则连接不上去。
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Profinet\SAMSerialSample.cs" region="Sample3" title="实例化操作" />
	/// 在实际的读取，我们一般放在后台进行循环扫描的操作，参见下面的代码
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Profinet\SAMSerialSample.cs" region="Sample4" title="基本的读取操作" />
	/// 当然也支持全异步的操作了，就是方法的名称改改
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Profinet\SAMSerialSample.cs" region="Sample5" title="实例化操作" />
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Profinet\SAMSerialSample.cs" region="Sample6" title="基本的读取操作" />
	/// </example>
	public class SAMTcpNet : NetworkDoubleBase
	{
		#region Constructor

		/// <summary>
		/// 实例化一个默认的对象<br />
		/// Instantiate a default object
		/// </summary>
		public SAMTcpNet( ) 
		{
			this.ByteTransform = new RegularByteTransform( );
		}

		/// <summary>
		/// 通过指定的ip地址以及端口来实例化对象<br />
		/// Instantiate the object with the specified IP address and port
		/// </summary>
		/// <param name="ipAddress">ip地址</param>
		/// <param name="port">端口号</param>
		public SAMTcpNet(string ipAddress, int port )
		{
			this.IpAddress     = ipAddress;
			this.Port          = port;
			this.ByteTransform = new RegularByteTransform( );
		}

		/// <inheritdoc/>
		protected override INetMessage GetNewNetMessage( ) => new SAMMessage( );

		#endregion

		/// <summary>
		/// 读取身份证设备的安全模块号<br />
		/// Read the security module number of the ID device
		/// </summary>
		/// <returns>结果数据内容</returns>
		[HslMqttApi]
		public OperateResult<string> ReadSafeModuleNumber( )
		{
			byte[] command = SAMSerial.PackToSAMCommand( SAMSerial.BuildReadCommand( 0x12, 0xFF, null ) );

			OperateResult<byte[]> read = ReadFromCoreServer( command );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<string>( read );

			OperateResult check = SAMSerial.CheckADSCommandAndSum( read.Content );
			if (!check.IsSuccess) return OperateResult.CreateFailedResult<string>( check );

			return SAMSerial.ExtractSafeModuleNumber( read.Content );
		}

		/// <summary>
		/// 检测安全模块状态<br />
		/// Detecting Security Module Status
		/// </summary>
		/// <returns>返回是否检测成功</returns>
		[HslMqttApi]
		public OperateResult CheckSafeModuleStatus( )
		{
			byte[] command = SAMSerial.PackToSAMCommand( SAMSerial.BuildReadCommand( 0x12, 0xFF, null ) );

			OperateResult<byte[]> read = ReadFromCoreServer( command );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<string>( read );

			OperateResult check = SAMSerial.CheckADSCommandAndSum( read.Content );
			if (!check.IsSuccess) return OperateResult.CreateFailedResult<string>( check );

			if (read.Content[9] != 0x90) return new OperateResult( SAMSerial.GetErrorDescription( read.Content[9] ) );
			return OperateResult.CreateSuccessResult( );
		}

		/// <summary>
		/// 寻找卡片，并返回是否成功<br />
		/// Find cards and return success
		/// </summary>
		/// <returns>是否寻找成功</returns>
		[HslMqttApi]
		public OperateResult SearchCard( )
		{
			byte[] command = SAMSerial.PackToSAMCommand( SAMSerial.BuildReadCommand( 0x20, 0x01, null ) );

			OperateResult<byte[]> read = ReadFromCoreServer( command );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<string>( read );

			OperateResult check = SAMSerial.CheckADSCommandAndSum( read.Content );
			if (!check.IsSuccess) return OperateResult.CreateFailedResult<string>( check );

			if (read.Content[9] != 0x9F) return new OperateResult( SAMSerial.GetErrorDescription( read.Content[9] ) );
			return OperateResult.CreateSuccessResult( );
		}

		/// <summary>
		/// 选择卡片，并返回是否成功<br />
		/// Select card and return success
		/// </summary>
		/// <returns>是否寻找成功</returns>
		[HslMqttApi]
		public OperateResult SelectCard( )
		{
			byte[] command = SAMSerial.PackToSAMCommand( SAMSerial.BuildReadCommand( 0x20, 0x02, null ) );

			OperateResult<byte[]> read = ReadFromCoreServer( command );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<string>( read );

			OperateResult check = SAMSerial.CheckADSCommandAndSum( read.Content );
			if (!check.IsSuccess) return OperateResult.CreateFailedResult<string>( check );

			if (read.Content[9] != 0x90) return new OperateResult( SAMSerial.GetErrorDescription( read.Content[9] ) );
			return OperateResult.CreateSuccessResult( );
		}

		/// <summary>
		/// 读取卡片，如果成功的话，就返回身份证的所有的信息<br />
		/// Read the card, if successful, return all the information of the ID cards
		/// </summary>
		/// <returns>是否寻找成功</returns>
		[HslMqttApi]
		public OperateResult<IdentityCard> ReadCard( )
		{
			byte[] command = SAMSerial.PackToSAMCommand( SAMSerial.BuildReadCommand( 0x30, 0x01, null ) );

			OperateResult<byte[]> read = ReadFromCoreServer( command );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<IdentityCard>( read );

			OperateResult check = SAMSerial.CheckADSCommandAndSum( read.Content );
			if (!check.IsSuccess) return OperateResult.CreateFailedResult<IdentityCard>( check );

			return SAMSerial.ExtractIdentityCard( read.Content );
		}

#if !NET35 && !NET20

		/// <inheritdoc cref="ReadSafeModuleNumber"/>
		public async Task<OperateResult<string>> ReadSafeModuleNumberAsync( )
		{
			byte[] command = SAMSerial.PackToSAMCommand( SAMSerial.BuildReadCommand( 0x12, 0xFF, null ) );

			OperateResult<byte[]> read = await ReadFromCoreServerAsync( command );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<string>( read );

			OperateResult check = SAMSerial.CheckADSCommandAndSum( read.Content );
			if (!check.IsSuccess) return OperateResult.CreateFailedResult<string>( check );

			return SAMSerial.ExtractSafeModuleNumber( read.Content );
		}

		/// <inheritdoc cref="CheckSafeModuleStatus"/>
		public async Task<OperateResult> CheckSafeModuleStatusAsync( )
		{
			byte[] command = SAMSerial.PackToSAMCommand( SAMSerial.BuildReadCommand( 0x12, 0xFF, null ) );

			OperateResult<byte[]> read = await ReadFromCoreServerAsync( command );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<string>( read );

			OperateResult check = SAMSerial.CheckADSCommandAndSum( read.Content );
			if (!check.IsSuccess) return OperateResult.CreateFailedResult<string>( check );

			if (read.Content[9] != 0x90) return new OperateResult( SAMSerial.GetErrorDescription( read.Content[9] ) );
			return OperateResult.CreateSuccessResult( );
		}

		/// <inheritdoc cref="SearchCard"/>
		public async Task<OperateResult> SearchCardAsync( )
		{
			byte[] command = SAMSerial.PackToSAMCommand( SAMSerial.BuildReadCommand( 0x20, 0x01, null ) );

			OperateResult<byte[]> read = await ReadFromCoreServerAsync( command );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<string>( read );

			OperateResult check = SAMSerial.CheckADSCommandAndSum( read.Content );
			if (!check.IsSuccess) return OperateResult.CreateFailedResult<string>( check );

			if (read.Content[9] != 0x9F) return new OperateResult( SAMSerial.GetErrorDescription( read.Content[9] ) );
			return OperateResult.CreateSuccessResult( );
		}

		/// <inheritdoc cref="SelectCard"/>
		public async Task<OperateResult> SelectCardAsync( )
		{
			byte[] command = SAMSerial.PackToSAMCommand( SAMSerial.BuildReadCommand( 0x20, 0x02, null ) );

			OperateResult<byte[]> read = await ReadFromCoreServerAsync( command );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<string>( read );

			OperateResult check = SAMSerial.CheckADSCommandAndSum( read.Content );
			if (!check.IsSuccess) return OperateResult.CreateFailedResult<string>( check );

			if (read.Content[9] != 0x90) return new OperateResult( SAMSerial.GetErrorDescription( read.Content[9] ) );
			return OperateResult.CreateSuccessResult( );
		}

		///<inheritdoc cref="ReadCard"/>
		public async Task<OperateResult<IdentityCard>> ReadCardAsync( )
		{
			byte[] command = SAMSerial.PackToSAMCommand( SAMSerial.BuildReadCommand( 0x30, 0x01, null ) );

			OperateResult<byte[]> read = await ReadFromCoreServerAsync( command );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<IdentityCard>( read );

			OperateResult check = SAMSerial.CheckADSCommandAndSum( read.Content );
			if (!check.IsSuccess) return OperateResult.CreateFailedResult<IdentityCard>( check );

			return SAMSerial.ExtractIdentityCard( read.Content );
		}
#endif

		#region Object Override

		/// <inheritdoc/>
		public override string ToString( ) => $"SAMTcpNet[{IpAddress}:{Port}]";

		#endregion
	}
}
