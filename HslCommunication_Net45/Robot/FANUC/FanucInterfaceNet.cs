using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HslCommunication.Core;
using HslCommunication.Core.Net;
using HslCommunication.Core.IMessage;
using HslCommunication.Reflection;
using System.Net.Sockets;
using HslCommunication.BasicFramework;
#if !NET35 && !NET20
using System.Threading.Tasks;
#endif

namespace HslCommunication.Robot.FANUC
{
	/// <summary>
	/// Fanuc机器人的PC Interface实现，在R-30iB mate plus型号上测试通过，支持读写任意的数据，写入操作务必谨慎调用，写入数据不当造成生命财产损失，作者概不负责。读写任意的地址见api文档信息<br />
	/// The Fanuc robot's PC Interface implementation has been tested on R-30iB mate plus models. It supports reading and writing arbitrary data. The writing operation must be called carefully. 
	/// Improper writing of data will cause loss of life and property. The author is not responsible. Read and write arbitrary addresses see api documentation information
	/// </summary>
	/// <remarks>
	/// 如果使用绝对地址进行访问的话，支持的地址格式如下：
	/// <list type="table">
	///   <listheader>
	///     <term>地址名称</term>
	///     <term>地址代号</term>
	///     <term>示例</term>
	///     <term>地址进制</term>
	///     <term>字操作</term>
	///     <term>位操作</term>
	///     <term>备注</term>
	///   </listheader>
	///   <item>
	///     <term>数据寄存器</term>
	///     <term>D</term>
	///     <term>D100,D200</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>×</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>R寄存器</term>
	///     <term>R</term>
	///     <term>R1-R10</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>×</term>
	///     <term>R1-R5为int类型，R6-R10为float类型，本质还是数据寄存器</term>
	///   </item>
	///   <item>
	///     <term>输入寄存器</term>
	///     <term>AI</term>
	///     <term>AI100,AI200</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>×</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>输出寄存器</term>
	///     <term>AQ</term>
	///     <term>AQ100,Q200</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>×</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>输入继电器</term>
	///     <term>I</term>
	///     <term>I100,I200</term>
	///     <term>10</term>
	///     <term>×</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>输出继电器</term>
	///     <term>Q</term>
	///     <term>Q100,Q200</term>
	///     <term>10</term>
	///     <term>×</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>中间继电器</term>
	///     <term>M</term>
	///     <term>M100,M200</term>
	///     <term>10</term>
	///     <term>×</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	/// </list>
	/// </remarks>
	/// <example>
	/// 我们先来看看简单的情况
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Robot\FANUC\FanucInterfaceNetSample.cs" region="Sample1" title="简单的读取" />
	/// 读取fanuc部分数据
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Robot\FANUC\FanucInterfaceNetSample.cs" region="Sample2" title="属性读取" />
	/// 最后是比较高级的任意数据读写
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Robot\FANUC\FanucInterfaceNetSample.cs" region="Sample3" title="复杂读取" />
	/// </example>
	public class FanucInterfaceNet : NetworkDeviceBase, IRobotNet, IReadWriteNet
	{
		#region Constructor

		/// <summary>
		/// 实例化一个默认的对象<br />
		/// Instantiate a default object
		/// </summary>
		public FanucInterfaceNet( ) 
		{
			WordLength    = 1;
			ByteTransform = new RegularByteTransform( );
		}

		/// <summary>
		/// 指定ip及端口来实例化一个默认的对象，端口默认60008<br />
		/// Specify the IP and port to instantiate a default object, the port defaults to 60008
		/// </summary>
		/// <param name="ipAddress">ip地址</param>
		/// <param name="port">端口号</param>
		public FanucInterfaceNet( string ipAddress, int port = 60008 )
		{
			WordLength    = 1;
			IpAddress     = ipAddress;
			Port          = port;
			ByteTransform = new RegularByteTransform( );
		}

		/// <inheritdoc/>
		protected override INetMessage GetNewNetMessage( ) => new FanucRobotMessage( );

		#endregion

		#region Public Properties

		/// <summary>
		/// 获取或设置当前客户端的ID信息，默认为1024<br />
		/// Gets or sets the ID information of the current client. The default is 1024.
		/// </summary>
		public int ClientId { get; private set; } = 1024;

		/// <summary>
		/// 获取或设置缓存的Fanuc数据的有效时间，对<see cref="ReadString(string)"/>方法有效，默认为100，单位毫秒。也即是在100ms内频繁读取机器人的属性数据的时候，优先读取缓存值，提高读取效率。<br />
		/// Gets or sets the valid time of the cached Fanuc data. It is valid for the <see cref="ReadString(string)" /> method. The default is 100, in milliseconds. 
		/// That is, when the attribute data of the robot is frequently read within 100ms, the cache value is preferentially read to improve the reading efficiency.
		/// </summary>
		public int FanucDataRetainTime { get; set; } = 100;

		#endregion

		#region NetworkDouble Override

		private OperateResult ReadCommandFromRobot( Socket socket, string[] cmds )
		{
			for (int i = 0; i < cmds.Length; i++)
			{
				byte[] buffer = Encoding.ASCII.GetBytes( cmds[i] );
				OperateResult<byte[]> write = ReadFromCoreServer( socket, FanucHelper.BuildWriteData( FanucHelper.SELECTOR_G, 1, buffer, buffer.Length ) );
				if (!write.IsSuccess) return write;
			}

			return OperateResult.CreateSuccessResult( );
		}

		/// <inheritdoc/>
		protected override OperateResult InitializationOnConnect( Socket socket )
		{
			BitConverter.GetBytes( ClientId ).CopyTo( connect_req, 1 );

			OperateResult<byte[]> receive = ReadFromCoreServer( socket, connect_req );
			if (!receive.IsSuccess) return receive;

			receive = ReadFromCoreServer( socket, session_req );
			if (!receive.IsSuccess) return receive;

			return ReadCommandFromRobot( socket, FanucHelper.GetFanucCmds( ) );
		}

		#endregion

		#region Async NetworkDouble Override
#if !NET35 && !NET20
		private async Task<OperateResult> ReadCommandFromRobotAsync( Socket socket, string[] cmds )
		{
			for (int i = 0; i < cmds.Length; i++)
			{
				byte[] buffer = Encoding.ASCII.GetBytes( cmds[i] );
				OperateResult<byte[]> write = await ReadFromCoreServerAsync( socket, FanucHelper.BuildWriteData( FanucHelper.SELECTOR_G, 1, buffer, buffer.Length ) );
				if (!write.IsSuccess) return write;
			}

			return OperateResult.CreateSuccessResult( );
		}

		/// <inheritdoc/>
		protected async override Task<OperateResult> InitializationOnConnectAsync( Socket socket )
		{
			BitConverter.GetBytes( ClientId ).CopyTo( connect_req, 1 );

			OperateResult<byte[]> receive = await ReadFromCoreServerAsync( socket, connect_req );
			if (!receive.IsSuccess) return receive;

			receive = await ReadFromCoreServerAsync( socket, session_req );
			if (!receive.IsSuccess) return receive;

			return await ReadCommandFromRobotAsync( socket, FanucHelper.GetFanucCmds( ) );
		}
#endif
		#endregion

		#region IRobotNet Support

		/// <inheritdoc cref="IRobotNet.Read(string)"/>
		[HslMqttApi( ApiTopic = "ReadRobotByte", Description = "Read the robot's original byte data information according to the address" )]
		public OperateResult<byte[]> Read( string address ) => Read( FanucHelper.SELECTOR_D, 1, 6130 );

		/// <inheritdoc cref="IRobotNet.ReadString(string)"/>
		[HslMqttApi( ApiTopic = "ReadRobotString", Description = "Read the string data information of the robot based on the address" )]
		public OperateResult<string> ReadString( string address )
		{
			if (string.IsNullOrEmpty( address ))
			{
				OperateResult<FanucData> read = ReadFanucData( );
				if (!read.IsSuccess) return OperateResult.CreateFailedResult<string>( read );

				fanucDataRetain = read.Content;
				fanucDataRefreshTime = DateTime.Now;
				return OperateResult.CreateSuccessResult( Newtonsoft.Json.JsonConvert.SerializeObject( read.Content, Newtonsoft.Json.Formatting.Indented ) );
			}
			else
			{
				if ((DateTime.Now - fanucDataRefreshTime).TotalMilliseconds > FanucDataRetainTime || fanucDataRetain == null)
				{
					// 100ms外请求，需要刷新数据缓存
					OperateResult<FanucData> read = ReadFanucData( );
					if (!read.IsSuccess) return OperateResult.CreateFailedResult<string>( read );

					fanucDataRetain = read.Content;
					fanucDataRefreshTime = DateTime.Now;
				}

				foreach (var item in fanucDataPropertyInfo)
				{
					if(item.Name == address)
					{
						return OperateResult.CreateSuccessResult( Newtonsoft.Json.JsonConvert.SerializeObject( item.GetValue( fanucDataRetain, null ), Newtonsoft.Json.Formatting.Indented ) );
					}
				}
				return new OperateResult<string>( StringResources.Language.NotSupportedDataType );
			}
		}

		#endregion

		#region Async IRobotNet Support
#if !NET35 && !NET20
		/// <inheritdoc cref="Read(string)"/>
		public async Task<OperateResult<byte[]>> ReadAsync( string address ) => await ReadAsync( FanucHelper.SELECTOR_D, 1, 6130 );

		/// <inheritdoc cref="ReadString(string)"/>
		public async Task<OperateResult<string>> ReadStringAsync( string address )
		{
			if (string.IsNullOrEmpty( address ))
			{
				OperateResult<FanucData> read = await ReadFanucDataAsync( );
				if (!read.IsSuccess) return OperateResult.CreateFailedResult<string>( read );

				fanucDataRetain = read.Content;
				fanucDataRefreshTime = DateTime.Now;
				return OperateResult.CreateSuccessResult( Newtonsoft.Json.JsonConvert.SerializeObject( read.Content, Newtonsoft.Json.Formatting.Indented ) );
			}
			else
			{
				if ((DateTime.Now - fanucDataRefreshTime).TotalMilliseconds > FanucDataRetainTime || fanucDataRetain == null)
				{
					// 100ms外请求，需要刷新数据缓存
					OperateResult<FanucData> read = await ReadFanucDataAsync( );
					if (!read.IsSuccess) return OperateResult.CreateFailedResult<string>( read );

					fanucDataRetain = read.Content;
					fanucDataRefreshTime = DateTime.Now;
				}

				foreach (var item in fanucDataPropertyInfo)
				{
					if (item.Name == address)
					{
						return OperateResult.CreateSuccessResult( Newtonsoft.Json.JsonConvert.SerializeObject( item.GetValue( fanucDataRetain, null ), Newtonsoft.Json.Formatting.Indented ) );
					}
				}
				return new OperateResult<string>( StringResources.Language.NotSupportedDataType );
			}
		}
#endif
		#endregion

		#region IReadWriteNet Support

		/// <summary>
		/// 按照字为单位批量读取设备的原始数据，需要指定地址及长度，地址示例：D1，AI1，AQ1，共计3个区的数据，注意地址的起始为1<br />
		/// Read the raw data of the device in batches in units of words. You need to specify the address and length. Example addresses: D1, AI1, AQ1, a total of 3 areas of data. Note that the start of the address is 1.
		/// </summary>
		/// <param name="address">起始地址，地址示例：D1，AI1，AQ1，共计3个区的数据，注意起始的起始为1</param>
		/// <param name="length">读取的长度，字为单位</param>
		/// <returns>返回的数据信息结果</returns>
		[HslMqttApi( "ReadByteArray", "" )]
		public override OperateResult<byte[]> Read( string address, ushort length )
		{
			OperateResult<byte, ushort> analysis = FanucHelper.AnalysisFanucAddress( address );
			if (!analysis.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( analysis );

			if (analysis.Content1 == FanucHelper.SELECTOR_D || analysis.Content1 == FanucHelper.SELECTOR_AI || analysis.Content1 == FanucHelper.SELECTOR_AQ)
				return Read( analysis.Content1, analysis.Content2, length );
			else
				return new OperateResult<byte[]>( StringResources.Language.NotSupportedDataType );
		}

		/// <summary>
		/// 写入原始的byte数组数据到指定的地址，返回是否写入成功，地址示例：D1，AI1，AQ1，共计3个区的数据，注意起始的起始为1<br />
		/// Write the original byte array data to the specified address, and return whether the write was successful. Example addresses: D1, AI1, AQ1, a total of 3 areas of data. Note that the start of the address is 1.
		/// </summary>
		/// <param name="address">起始地址，地址示例：D1，AI1，AQ1，共计3个区的数据，注意起始的起始为1</param>
		/// <param name="value">写入值</param>
		/// <returns>带有成功标识的结果类对象</returns>
		[HslMqttApi( "WriteByteArray", "" )]
		public override OperateResult Write( string address, byte[] value )
		{
			OperateResult<byte, ushort> analysis = FanucHelper.AnalysisFanucAddress( address );
			if (!analysis.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( analysis );

			if (analysis.Content1 == FanucHelper.SELECTOR_D || analysis.Content1 == FanucHelper.SELECTOR_AI || analysis.Content1 == FanucHelper.SELECTOR_AQ)
				return Write( analysis.Content1, analysis.Content2, value );
			else
				return new OperateResult<byte[]>( StringResources.Language.NotSupportedDataType );
		}

		/// <summary>
		/// 按照位为单位批量读取设备的原始数据，需要指定地址及长度，地址示例：M1，I1，Q1，共计3个区的数据，注意地址的起始为1<br />
		/// Read the raw data of the device in batches in units of boolean. You need to specify the address and length. Example addresses: M1，I1，Q1, a total of 3 areas of data. Note that the start of the address is 1.
		/// </summary>
		/// <param name="address">起始地址，地址示例：M1，I1，Q1，共计3个区的数据，注意地址的起始为1</param>
		/// <param name="length">读取的长度，位为单位</param>
		/// <returns>返回的数据信息结果</returns>
		[HslMqttApi( "ReadBoolArray", "" )]
		public override OperateResult<bool[]> ReadBool( string address, ushort length )
		{
			OperateResult<byte, ushort> analysis = FanucHelper.AnalysisFanucAddress( address );
			if (!analysis.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( analysis );

			if (analysis.Content1 == FanucHelper.SELECTOR_I || analysis.Content1 == FanucHelper.SELECTOR_Q || analysis.Content1 == FanucHelper.SELECTOR_M)
				return ReadBool( analysis.Content1, analysis.Content2, length );
			else
				return new OperateResult<bool[]>( StringResources.Language.NotSupportedDataType );
		}

		/// <summary>
		/// 批量写入<see cref="bool"/>数组数据，返回是否写入成功，需要指定起始地址，地址示例：M1，I1，Q1，共计3个区的数据，注意地址的起始为1<br />
		/// Write <see cref="bool"/> array data in batches. If the write success is returned, you need to specify the starting address. Example address: M1, I1, Q1, a total of 3 areas of data. Note that the starting address is 1.
		/// </summary>
		/// <param name="address">起始地址，地址示例：M1，I1，Q1，共计3个区的数据，注意地址的起始为1</param>
		/// <param name="value">等待写入的数据值</param>
		/// <returns>是否写入成功</returns>
		[HslMqttApi( "WriteBoolArray", "" )]
		public override OperateResult Write( string address, bool[] value )
		{
			OperateResult<byte, ushort> analysis = FanucHelper.AnalysisFanucAddress( address );
			if (!analysis.IsSuccess) return analysis;

			if (analysis.Content1 == FanucHelper.SELECTOR_I || analysis.Content1 == FanucHelper.SELECTOR_Q || analysis.Content1 == FanucHelper.SELECTOR_M)
				return WriteBool( analysis.Content1, analysis.Content2, value );
			else
				return new OperateResult( StringResources.Language.NotSupportedDataType );
		}

		#endregion

		#region Async IReadWriteNet Support
#if !NET35 && !NET20
		/// <inheritdoc cref="Read(string, ushort)"/>
		public async override Task<OperateResult<byte[]>> ReadAsync( string address, ushort length )
		{
			OperateResult<byte, ushort> analysis = FanucHelper.AnalysisFanucAddress( address );
			if (!analysis.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( analysis );

			if (analysis.Content1 == FanucHelper.SELECTOR_D || analysis.Content1 == FanucHelper.SELECTOR_AI || analysis.Content1 == FanucHelper.SELECTOR_AQ)
				return await ReadAsync( analysis.Content1, analysis.Content2, length );
			else
				return new OperateResult<byte[]>( StringResources.Language.NotSupportedDataType );
		}

		/// <inheritdoc cref="Write(string, byte[])"/>
		public async override Task<OperateResult> WriteAsync( string address, byte[] value )
		{
			OperateResult<byte, ushort> analysis = FanucHelper.AnalysisFanucAddress( address );
			if (!analysis.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( analysis );

			if (analysis.Content1 == FanucHelper.SELECTOR_D || analysis.Content1 == FanucHelper.SELECTOR_AI || analysis.Content1 == FanucHelper.SELECTOR_AQ)
				return await WriteAsync( analysis.Content1, analysis.Content2, value );
			else
				return new OperateResult<byte[]>( StringResources.Language.NotSupportedDataType );
		}

		/// <inheritdoc cref="ReadBool(string, ushort)"/>
		public async override Task<OperateResult<bool[]>> ReadBoolAsync( string address, ushort length )
		{
			OperateResult<byte, ushort> analysis = FanucHelper.AnalysisFanucAddress( address );
			if (!analysis.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( analysis );

			if (analysis.Content1 == FanucHelper.SELECTOR_I || analysis.Content1 == FanucHelper.SELECTOR_Q || analysis.Content1 == FanucHelper.SELECTOR_M)
				return await ReadBoolAsync( analysis.Content1, analysis.Content2, length );
			else
				return new OperateResult<bool[]>( StringResources.Language.NotSupportedDataType );
		}

		/// <inheritdoc cref="Write(string, bool[])"/>
		public async override Task<OperateResult> WriteAsync( string address, bool[] value )
		{
			OperateResult<byte, ushort> analysis = FanucHelper.AnalysisFanucAddress( address );
			if (!analysis.IsSuccess) return analysis;

			if (analysis.Content1 == FanucHelper.SELECTOR_I || analysis.Content1 == FanucHelper.SELECTOR_Q || analysis.Content1 == FanucHelper.SELECTOR_M)
				return await WriteBoolAsync( analysis.Content1, analysis.Content2, value );
			else
				return new OperateResult( StringResources.Language.NotSupportedDataType );
		}
#endif
		#endregion

		#region Select Read Write

		/// <summary>
		/// 按照字为单位批量读取设备的原始数据，需要指定数据块地址，偏移地址及长度，主要针对08, 10, 12的数据块，注意地址的起始为1<br />
		/// Read the raw data of the device in batches in units of words. You need to specify the data block address, offset address, and length. It is mainly for data blocks of 08, 10, and 12. Note that the start of the address is 1.
		/// </summary>
		/// <param name="select">数据块信息</param>
		/// <param name="address">偏移地址</param>
		/// <param name="length">读取的长度，字为单位</param>
		public OperateResult<byte[]> Read( byte select, ushort address, ushort length )
		{
			byte[] send = FanucHelper.BulidReadData( select, address, length );

			OperateResult<byte[]> read = ReadFromCoreServer( send );
			if (!read.IsSuccess) return read;

			if (read.Content[31] == 148)
				return OperateResult.CreateSuccessResult( SoftBasic.ArrayRemoveBegin( read.Content, 56 ) );
			else if (read.Content[31] == 212)
				return OperateResult.CreateSuccessResult( SoftBasic.ArraySelectMiddle( read.Content, 44, length * 2 ) );
			else
				return new OperateResult<byte[]>( read.Content[31], "Error" );
		}

		/// <summary>
		/// 写入原始的byte数组数据到指定的地址，返回是否写入成功，，需要指定数据块地址，偏移地址，主要针对08, 10, 12的数据块，注意起始的起始为1<br />
		/// Write the original byte array data to the specified address, and return whether the writing is successful. You need to specify the data block address and offset address, 
		/// which are mainly for the data blocks of 08, 10, and 12. Note that the start of the start is 1.
		/// </summary>
		/// <param name="select">数据块信息</param>
		/// <param name="address">偏移地址</param>
		/// <param name="value">原始数据内容</param>
		public OperateResult Write( byte select, ushort address, byte[] value )
		{
			byte[] send = FanucHelper.BuildWriteData( select, address, value, value.Length / 2 );
			OperateResult<byte[]> read = ReadFromCoreServer( send );
			if (!read.IsSuccess) return read;

			if (read.Content[31] == 212)
				return OperateResult.CreateSuccessResult( );
			else
				return new OperateResult<byte[]>( read.Content[31], "Error" );
		}

		/// <summary>
		/// 按照位为单位批量读取设备的原始数据，需要指定数据块地址，偏移地址及长度，主要针对70, 72, 76的数据块，注意地址的起始为1<br />
		/// </summary>
		/// <param name="select">数据块信息</param>
		/// <param name="address">偏移地址</param>
		/// <param name="length">读取的长度，字为单位</param>
		public OperateResult<bool[]> ReadBool( byte select, ushort address, ushort length )
		{
			int byteStartIndex = address - 1 - (address - 1) % 8 + 1;
			int byteEndIndex = (address + length - 1) % 8 == 0 ? (address + length - 1) : ((address + length - 1) / 8 * 8 + 8);
			int byteLength = (byteEndIndex - byteStartIndex + 1) / 8;

			byte[] send = FanucHelper.BulidReadData( select, address, (ushort)(byteLength * 8) );
			LogNet?.WriteDebug( ToString( ), StringResources.Language.Send + SoftBasic.ByteToHexString( send ) );

			OperateResult<byte[]> read = ReadFromCoreServer( send );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( read );

			LogNet?.WriteDebug( ToString( ), StringResources.Language.Receive + SoftBasic.ByteToHexString( read.Content ) );
			if (read.Content[31] == 148)
			{
				bool[] array = SoftBasic.ByteToBoolArray( SoftBasic.ArrayRemoveBegin( read.Content, 56 ) );
				bool[] buffer = new bool[length];
				Array.Copy( array, address - byteStartIndex, buffer, 0, length );
				return OperateResult.CreateSuccessResult( buffer );

			}
			else if (read.Content[31] == 212)
			{
				bool[] array = SoftBasic.ByteToBoolArray( SoftBasic.ArraySelectMiddle( read.Content, 44, byteLength ) );
				bool[] buffer = new bool[length];
				Array.Copy( array, address - byteStartIndex, buffer, 0, length );
				return OperateResult.CreateSuccessResult( buffer );
			}
			else
			{
				return new OperateResult<bool[]>( read.Content[31], "Error" );
			}
		}

		/// <summary>
		/// 批量写入<see cref="bool"/>数组数据，返回是否写入成功，需要指定数据块地址，偏移地址，主要针对70, 72, 76的数据块，注意起始的起始为1
		/// </summary>
		/// <param name="select">数据块信息</param>
		/// <param name="address">偏移地址</param>
		/// <param name="value">原始的数据内容</param>
		/// <returns>是否写入成功</returns>
		public OperateResult WriteBool( byte select, ushort address, bool[] value )
		{
			int byteStartIndex = address - 1 - (address - 1) % 8 + 1;
			int byteEndIndex = (address + value.Length - 1) % 8 == 0 ? (address + value.Length - 1) : ((address + value.Length - 1) / 8 * 8 + 8);
			int byteLength = (byteEndIndex - byteStartIndex + 1) / 8;

			bool[] buffer = new bool[byteLength * 8];
			Array.Copy( value, 0, buffer, address - byteStartIndex, value.Length );
			byte[] send = FanucHelper.BuildWriteData( select, address, ByteTransform.TransByte( buffer ), value.Length );

			OperateResult<byte[]> read = ReadFromCoreServer( send );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<short[]>( read );

			if (read.Content[31] == 212)
				return OperateResult.CreateSuccessResult( );
			else
				return new OperateResult( );
		}

		#endregion

		#region Async Select Read Write
#if !NET35 && !NET20
		/// <inheritdoc cref="Read(byte, ushort, ushort)"/>
		public async Task<OperateResult<byte[]>> ReadAsync( byte select, ushort address, ushort length )
		{
			byte[] send = FanucHelper.BulidReadData( select, address, length );

			OperateResult<byte[]> read = await ReadFromCoreServerAsync( send );
			if (!read.IsSuccess) return read;

			if (read.Content[31] == 148)
				return OperateResult.CreateSuccessResult( SoftBasic.ArrayRemoveBegin( read.Content, 56 ) );
			else if (read.Content[31] == 212)
				return OperateResult.CreateSuccessResult( SoftBasic.ArraySelectMiddle( read.Content, 44, length * 2 ) );
			else
				return new OperateResult<byte[]>( read.Content[31], "Error" );
		}

		/// <inheritdoc cref="Write(byte, ushort, byte[])"/>
		public async Task<OperateResult> WriteAsync( byte select, ushort address, byte[] value )
		{
			byte[] send = FanucHelper.BuildWriteData( select, address, value, value.Length / 2 );
			OperateResult<byte[]> read = await ReadFromCoreServerAsync( send );
			if (!read.IsSuccess) return read;

			if (read.Content[31] == 212)
				return OperateResult.CreateSuccessResult( );
			else
				return new OperateResult<byte[]>( read.Content[31], "Error" );
		}

		/// <inheritdoc cref="ReadBool(byte, ushort, ushort)"/>
		public async Task<OperateResult<bool[]>> ReadBoolAsync( byte select, ushort address, ushort length )
		{
			int byteStartIndex = address - 1 - (address - 1) % 8 + 1;
			int byteEndIndex = (address + length - 1) % 8 == 0 ? (address + length - 1) : ((address + length - 1) / 8 * 8 + 8);
			int byteLength = (byteEndIndex - byteStartIndex + 1) / 8;

			byte[] send = FanucHelper.BulidReadData( select, address, (ushort)(byteLength * 8) );
			LogNet?.WriteDebug( ToString( ), StringResources.Language.Send + SoftBasic.ByteToHexString( send ) );

			OperateResult<byte[]> read = await ReadFromCoreServerAsync( send );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( read );

			LogNet?.WriteDebug( ToString( ), StringResources.Language.Receive + SoftBasic.ByteToHexString( read.Content ) );
			if (read.Content[31] == 148)
			{
				bool[] array = SoftBasic.ByteToBoolArray( SoftBasic.ArrayRemoveBegin( read.Content, 56 ) );
				bool[] buffer = new bool[length];
				Array.Copy( array, address - byteStartIndex, buffer, 0, length );
				return OperateResult.CreateSuccessResult( buffer );

			}
			else if (read.Content[31] == 212)
			{
				bool[] array = SoftBasic.ByteToBoolArray( SoftBasic.ArraySelectMiddle( read.Content, 44, byteLength ) );
				bool[] buffer = new bool[length];
				Array.Copy( array, address - byteStartIndex, buffer, 0, length );
				return OperateResult.CreateSuccessResult( buffer );
			}
			else
			{
				return new OperateResult<bool[]>( read.Content[31], "Error" );
			}
		}

		/// <inheritdoc cref="WriteBool(byte, ushort, bool[])"/>
		public async Task<OperateResult> WriteBoolAsync( byte select, ushort address, bool[] value )
		{
			int byteStartIndex = address - 1 - (address - 1) % 8 + 1;
			int byteEndIndex = (address + value.Length - 1) % 8 == 0 ? (address + value.Length - 1) : ((address + value.Length - 1) / 8 * 8 + 8);
			int byteLength = (byteEndIndex - byteStartIndex + 1) / 8;

			bool[] buffer = new bool[byteLength * 8];
			Array.Copy( value, 0, buffer, address - byteStartIndex, value.Length );
			byte[] send = FanucHelper.BuildWriteData( select, address, ByteTransform.TransByte( buffer ), value.Length );

			OperateResult<byte[]> read = await ReadFromCoreServerAsync( send );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<short[]>( read );

			if (read.Content[31] == 212)
				return OperateResult.CreateSuccessResult( );
			else
				return new OperateResult( );
		}
#endif
		#endregion

		#region Extension Method

		/// <summary>
		/// 读取机器人的详细信息，返回解析后的数据类型<br />
		/// Read the details of the robot and return the resolved data type
		/// </summary>
		/// <returns>结果数据信息</returns>
		[HslMqttApi( Description = "Read the details of the robot and return the resolved data type" )]
		public OperateResult<FanucData> ReadFanucData( )
		{
			OperateResult<byte[]> read = Read( "" );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<FanucData>( read );

			return FanucData.PraseFrom( read.Content );
		}

		/// <summary>
		/// 读取机器人的SDO信息<br />
		/// Read the SDO information of the robot
		/// </summary>
		/// <param name="address">偏移地址</param>
		/// <param name="length">读取的长度</param>
		/// <returns>结果数据</returns>
		[HslMqttApi( Description = "Read the SDO information of the robot" )]
		public OperateResult<bool[]> ReadSDO( ushort address, ushort length )
		{
			if (address < 11001)
				return ReadBool( FanucHelper.SELECTOR_I, address, length );
			else
				return ReadPMCR2( (ushort)(address - 11000), length );
		}

		/// <summary>
		/// 写入机器人的SDO信息<br />
		/// Write the SDO information of the robot
		/// </summary>
		/// <param name="address">偏移地址</param>
		/// <param name="value">数据值</param>
		/// <returns>是否写入成功</returns>
		[HslMqttApi( Description = "Write the SDO information of the robot" )]
		public OperateResult WriteSDO( ushort address, bool[] value )
		{
			if (address < 11001)
				return WriteBool( FanucHelper.SELECTOR_I, address, value );
			else
				return WritePMCR2( (ushort)(address - 11000), value );
		}

		/// <summary>
		/// 读取机器人的SDI信息<br />
		/// Read the SDI information of the robot
		/// </summary>
		/// <param name="address">偏移地址</param>
		/// <param name="length">读取长度</param>
		/// <returns>结果内容</returns>
		[HslMqttApi( Description = "Read the SDI information of the robot" )]
		public OperateResult<bool[]> ReadSDI( ushort address, ushort length ) => ReadBool( FanucHelper.SELECTOR_Q, address, length );

		/// <summary>
		/// 写入机器人的SDI信息<br />
		/// Write the SDI information of the robot
		/// </summary>
		/// <param name="address">偏移地址</param>
		/// <param name="value">数据值</param>
		/// <returns>是否写入成功</returns>
		[HslMqttApi( Description = "Write the SDI information of the robot" )]
		public OperateResult WriteSDI( ushort address, bool[] value ) => WriteBool( FanucHelper.SELECTOR_Q, address, value );

		/// <summary>
		/// 读取机器人的RDI信息
		/// </summary>
		/// <param name="address">偏移地址</param>
		/// <param name="length">读取长度</param>
		/// <returns>结果信息</returns>
		[HslMqttApi]
		public OperateResult<bool[]> ReadRDI( ushort address, ushort length ) => ReadBool( FanucHelper.SELECTOR_Q, (ushort)(address + 5000), length );

		/// <summary>
		/// 写入机器人的RDI信息
		/// </summary>
		/// <param name="address">偏移地址</param>
		/// <param name="value">数据值</param>
		/// <returns>是否写入成功</returns>
		[HslMqttApi]
		public OperateResult WriteRDI( ushort address, bool[] value ) => WriteBool( FanucHelper.SELECTOR_Q, (ushort)(address + 5000), value );

		/// <summary>
		/// 读取机器人的UI信息
		/// </summary>
		/// <param name="address">偏移地址</param>
		/// <param name="length">读取长度</param>
		/// <returns>结果信息</returns>
		[HslMqttApi]
		public OperateResult<bool[]> ReadUI( ushort address, ushort length ) => ReadBool( FanucHelper.SELECTOR_Q, (ushort)(address + 6000), length );

		/// <summary>
		/// 读取机器人的UO信息
		/// </summary>
		/// <param name="address">偏移地址</param>
		/// <param name="length">读取长度</param>
		/// <returns>结果信息</returns>
		[HslMqttApi]
		public OperateResult<bool[]> ReadUO( ushort address, ushort length ) => ReadBool( FanucHelper.SELECTOR_I, (ushort)(address + 6000), length );

		/// <summary>
		/// 写入机器人的UO信息
		/// </summary>
		/// <param name="address">偏移地址</param>
		/// <param name="value">数据值</param>
		/// <returns>是否写入成功</returns>
		[HslMqttApi]
		public OperateResult WriteUO( ushort address, bool[] value ) => WriteBool( FanucHelper.SELECTOR_I, (ushort)(address + 6000), value );

		/// <summary>
		/// 读取机器人的SI信息
		/// </summary>
		/// <param name="address">偏移地址</param>
		/// <param name="length">读取长度</param>
		/// <returns>结果信息</returns>
		[HslMqttApi]
		public OperateResult<bool[]> ReadSI( ushort address, ushort length ) => ReadBool( FanucHelper.SELECTOR_Q, (ushort)(address + 7000), length );

		/// <summary>
		/// 读取机器人的SO信息
		/// </summary>
		/// <param name="address">偏移地址</param>
		/// <param name="length">读取长度</param>
		/// <returns>结果信息</returns>
		[HslMqttApi]
		public OperateResult<bool[]> ReadSO( ushort address, ushort length ) => ReadBool( FanucHelper.SELECTOR_I, (ushort)(address + 7000), length );

		/// <summary>
		/// 写入机器人的SO信息
		/// </summary>
		/// <param name="address">偏移地址</param>
		/// <param name="value">数据值</param>
		/// <returns>是否写入成功</returns>
		[HslMqttApi]
		public OperateResult WriteSO( ushort address, bool[] value ) => WriteBool( FanucHelper.SELECTOR_I, (ushort)(address + 7000), value );

		/// <summary>
		/// 读取机器人的GI信息
		/// </summary>
		/// <param name="address">偏移地址</param>
		/// <param name="length">数据长度</param>
		/// <returns>结果信息</returns>
		[HslMqttApi]
		public OperateResult<ushort[]> ReadGI( ushort address, ushort length ) => ByteTransformHelper.GetSuccessResultFromOther( Read( FanucHelper.SELECTOR_AQ, address, length ), m => ByteTransform.TransUInt16( m, 0, length ) );

		/// <summary>
		/// 写入机器人的GI信息
		/// </summary>
		/// <param name="address">偏移地址</param>
		/// <param name="value">数据值</param>
		/// <returns>是否写入成功</returns>
		[HslMqttApi]
		public OperateResult WriteGI( ushort address, ushort[] value ) => Write( FanucHelper.SELECTOR_AQ, address, ByteTransform.TransByte( value ) );

		/// <summary>
		/// 读取机器人的GO信息
		/// </summary>
		/// <param name="address">偏移地址</param>
		/// <param name="length">读取长度</param>
		/// <returns>结果信息</returns>
		[HslMqttApi]
		public OperateResult<ushort[]> ReadGO( ushort address, ushort length )
		{
			if (address >= 10001) address -= 6000;

			return ByteTransformHelper.GetSuccessResultFromOther( Read( FanucHelper.SELECTOR_AI, address, length ), m => ByteTransform.TransUInt16( m, 0, length ) );
		}

		/// <summary>
		/// 写入机器人的GO信息
		/// </summary>
		/// <param name="address">偏移地址</param>
		/// <param name="value">数据值</param>
		/// <returns>写入结果</returns>
		[HslMqttApi]
		public OperateResult WriteGO( ushort address, ushort[] value )
		{
			if (address >= 10001) address -= 6000;
			return Write( FanucHelper.SELECTOR_AI, address, ByteTransform.TransByte( value ) );
		}

		/// <summary>
		/// 读取机器人的PMCR2信息
		/// </summary>
		/// <param name="address">偏移地址</param>
		/// <param name="length">读取长度</param>
		/// <returns>结果信息</returns>
		[HslMqttApi]
		public OperateResult<bool[]> ReadPMCR2( ushort address, ushort length ) => ReadBool( FanucHelper.SELECTOR_M, address, length );

		/// <summary>
		/// 写入机器人的PMCR2信息
		/// </summary>
		/// <param name="address">偏移信息</param>
		/// <param name="value">数据值</param>
		/// <returns>是否写入成功</returns>
		[HslMqttApi]
		public OperateResult WritePMCR2( ushort address, bool[] value ) => WriteBool( FanucHelper.SELECTOR_M, address, value );

		/// <summary>
		/// 读取机器人的RDO信息
		/// </summary>
		/// <param name="address">偏移地址</param>
		/// <param name="length">读取长度</param>
		/// <returns>结果信息</returns>
		[HslMqttApi]
		public OperateResult<bool[]> ReadRDO( ushort address, ushort length ) => ReadBool( FanucHelper.SELECTOR_I, (ushort)(address + 5000), length );

		/// <summary>
		/// 写入机器人的RDO信息
		/// </summary>
		/// <param name="address">偏移地址</param>
		/// <param name="value">数据值</param>
		/// <returns>是否写入成功</returns>
		[HslMqttApi]
		public OperateResult WriteRDO( ushort address, bool[] value ) => WriteBool( FanucHelper.SELECTOR_I, (ushort)(address + 5000), value );

		/// <summary>
		/// 写入机器人的Rxyzwpr信息，谨慎调用，
		/// </summary>
		/// <param name="Address">偏移地址</param>
		/// <param name="Xyzwpr">姿态信息</param>
		/// <param name="Config">设置信息</param>
		/// <param name="UserFrame">参考系</param>
		/// <param name="UserTool">工具</param>
		/// <returns>是否写入成功</returns>
		[HslMqttApi]
		public OperateResult WriteRXyzwpr( ushort Address, float[] Xyzwpr, short[] Config, short UserFrame, short UserTool )
		{
			int num = Xyzwpr.Length * 4 + Config.Length * 2 + 2;
			byte[] robotBuffer = new byte[num];
			ByteTransform.TransByte( Xyzwpr ).CopyTo( robotBuffer, 0 );
			ByteTransform.TransByte( Config ).CopyTo( robotBuffer, 36 );

			OperateResult write = Write( FanucHelper.SELECTOR_D, Address, robotBuffer );
			if (!write.IsSuccess) return write;

			if (0 <= UserFrame && UserFrame <= 15)
			{
				if (0 <= UserTool && UserTool <= 15)
				{
					write = Write( FanucHelper.SELECTOR_D, (ushort)(Address + 45), ByteTransform.TransByte( new short[] { UserFrame, UserTool } ) );
					if (!write.IsSuccess) return write;
				}
				else
				{
					write = Write( FanucHelper.SELECTOR_D, (ushort)(Address + 45), ByteTransform.TransByte( new short[] { UserFrame } ) );
					if (!write.IsSuccess) return write;
				}
			}
			else if (0 <= UserTool && UserTool <= 15)
			{
				write = Write( FanucHelper.SELECTOR_D, (ushort)(Address + 46), ByteTransform.TransByte( new short[] { UserTool } ) );
				if (!write.IsSuccess) return write;
			}
			return OperateResult.CreateSuccessResult( );
		}

		/// <summary>
		/// 写入机器人的Joint信息
		/// </summary>
		/// <param name="address">偏移地址</param>
		/// <param name="joint">关节坐标</param>
		/// <param name="UserFrame">参考系</param>
		/// <param name="UserTool">工具</param>
		/// <returns>是否写入成功</returns>
		[HslMqttApi]
		public OperateResult WriteRJoint( ushort address, float[] joint, short UserFrame, short UserTool )
		{
			OperateResult write = Write( FanucHelper.SELECTOR_D, (ushort)(address + 26), ByteTransform.TransByte( joint ) );
			if (!write.IsSuccess) return write;
			if (0 <= UserFrame && UserFrame <= 15)
			{
				if (0 <= UserTool && UserTool <= 15)
				{
					write = Write( FanucHelper.SELECTOR_D, (ushort)(address + 44), ByteTransform.TransByte( new short[] { 0, UserFrame, UserTool } ) );
					if (!write.IsSuccess) return write;
				}
				else
				{
					write = Write( FanucHelper.SELECTOR_D, (ushort)(address + 44), ByteTransform.TransByte( new short[] { 0, UserFrame } ) );
					if (!write.IsSuccess) return write;
				}
			}
			else
			{
				write = Write( FanucHelper.SELECTOR_D, (ushort)(address + 44), ByteTransform.TransByte( new short[] { 0 } ) );
				if (!write.IsSuccess) return write;

				if (0 <= UserTool && UserTool <= 15)
				{
					write = Write( FanucHelper.SELECTOR_D, (ushort)(address + 44), ByteTransform.TransByte( new short[] { 0, UserTool } ) );
					if (!write.IsSuccess) return write;
				}
			}
			return OperateResult.CreateSuccessResult( );
		}

		#endregion

		#region Async Extension Method
#if !NET35 && !NET20
		/// <inheritdoc cref="ReadFanucData"/>
		public async Task<OperateResult<FanucData>> ReadFanucDataAsync( )
		{
			OperateResult<byte[]> read = await ReadAsync( "" );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<FanucData>( read );

			return FanucData.PraseFrom( read.Content );
		}

		/// <inheritdoc cref="ReadSDO(ushort, ushort)"/>
		public async Task<OperateResult<bool[]>> ReadSDOAsync( ushort address, ushort length )
		{
			if (address < 11001)
				return ReadBool( FanucHelper.SELECTOR_I, address, length );
			else
				return await ReadPMCR2Async( (ushort)(address - 11000), length );
		}

		/// <inheritdoc cref="WriteSDO(ushort, bool[])"/>
		public async Task<OperateResult> WriteSDOAsync( ushort address, bool[] value )
		{
			if (address < 11001)
				return WriteBool( FanucHelper.SELECTOR_I, address, value );
			else
				return await WritePMCR2Async( (ushort)(address - 11000), value );
		}

		/// <inheritdoc cref="ReadSDI(ushort, ushort)"/>
		public async Task<OperateResult<bool[]>> ReadSDIAsync( ushort address, ushort length ) => await ReadBoolAsync( FanucHelper.SELECTOR_Q, address, length );

		/// <inheritdoc cref="WriteSDI(ushort, bool[])"/>
		public async Task<OperateResult> WriteSDIAsync( ushort address, bool[] value ) => await WriteBoolAsync( FanucHelper.SELECTOR_Q, address, value );

		/// <inheritdoc cref="ReadRDI(ushort, ushort)"/>
		public async Task<OperateResult<bool[]>> ReadRDIAsync( ushort address, ushort length ) => await ReadBoolAsync( FanucHelper.SELECTOR_Q, (ushort)(address + 5000), length );

		/// <inheritdoc cref="WriteRDI(ushort, bool[])"/>
		public async Task<OperateResult> WriteRDIAsync( ushort address, bool[] value ) => await WriteBoolAsync( FanucHelper.SELECTOR_Q, (ushort)(address + 5000), value );

		/// <inheritdoc cref="ReadUI(ushort, ushort)"/>
		public async Task<OperateResult<bool[]>> ReadUIAsync( ushort address, ushort length ) => await ReadBoolAsync( FanucHelper.SELECTOR_Q, (ushort)(address + 6000), length );

		/// <inheritdoc cref="ReadUO(ushort, ushort)"/>
		public async Task<OperateResult<bool[]>> ReadUOAsync( ushort address, ushort length ) => await ReadBoolAsync( FanucHelper.SELECTOR_I, (ushort)(address + 6000), length );

		/// <inheritdoc cref="WriteUO(ushort, bool[])"/>
		public async Task<OperateResult> WriteUOAsync( ushort address, bool[] value ) => await WriteBoolAsync( FanucHelper.SELECTOR_I, (ushort)(address + 6000), value );

		/// <inheritdoc cref="ReadSI(ushort, ushort)"/>
		public async Task<OperateResult<bool[]>> ReadSIAsync( ushort address, ushort length ) => await ReadBoolAsync( FanucHelper.SELECTOR_Q, (ushort)(address + 7000), length );

		/// <inheritdoc cref="ReadSO(ushort, ushort)"/>
		public async Task<OperateResult<bool[]>> ReadSOAsync( ushort address, ushort length ) => await ReadBoolAsync( FanucHelper.SELECTOR_I, (ushort)(address + 7000), length );

		/// <inheritdoc cref="WriteSO(ushort, bool[])"/>
		public async Task<OperateResult> WriteSOAsync( ushort address, bool[] value ) => await WriteBoolAsync( FanucHelper.SELECTOR_I, (ushort)(address + 7000), value );

		/// <inheritdoc cref="ReadGI(ushort, ushort)"/>
		public async Task<OperateResult<ushort[]>> ReadGIAsync( ushort address, ushort length ) => ByteTransformHelper.GetSuccessResultFromOther( await ReadAsync( FanucHelper.SELECTOR_AQ, address, length ), m => ByteTransform.TransUInt16( m, 0, length ) );

		/// <inheritdoc cref="WriteGI(ushort, ushort[])"/>
		public async Task<OperateResult> WriteGIAsync( ushort address, ushort[] value ) => await WriteAsync( FanucHelper.SELECTOR_AQ, address, ByteTransform.TransByte( value ) );

		/// <inheritdoc cref="ReadGO(ushort, ushort)"/>
		public async Task<OperateResult<ushort[]>> ReadGOAsync( ushort address, ushort length )
		{
			if (address >= 10001) address -= 6000;

			return ByteTransformHelper.GetSuccessResultFromOther( await ReadAsync( FanucHelper.SELECTOR_AI, address, length ), m => ByteTransform.TransUInt16( m, 0, length ) );
		}

		/// <inheritdoc cref="WriteGO(ushort, ushort[])"/>
		public async Task<OperateResult> WriteGOAsync( ushort address, ushort[] value )
		{
			if (address >= 10001) address -= 6000;
			return await WriteAsync( FanucHelper.SELECTOR_AI, address, ByteTransform.TransByte( value ) );
		}

		/// <inheritdoc cref="ReadPMCR2(ushort, ushort)"/>
		public async Task<OperateResult<bool[]>> ReadPMCR2Async( ushort address, ushort length ) => await ReadBoolAsync( FanucHelper.SELECTOR_M, address, length );

		/// <inheritdoc cref="WritePMCR2(ushort, bool[])"/>
		public async Task<OperateResult> WritePMCR2Async( ushort address, bool[] value ) => await WriteBoolAsync( FanucHelper.SELECTOR_M, address, value );

		/// <inheritdoc cref="ReadRDO(ushort, ushort)"/>
		public async Task<OperateResult<bool[]>> ReadRDOAsync( ushort address, ushort length ) => await ReadBoolAsync( FanucHelper.SELECTOR_I, (ushort)(address + 5000), length );

		/// <inheritdoc cref="WriteRDO(ushort, bool[])"/>
		public async Task<OperateResult> WriteRDOAsync( ushort address, bool[] value ) => await WriteBoolAsync( FanucHelper.SELECTOR_I, (ushort)(address + 5000), value );

		/// <inheritdoc cref="WriteRXyzwpr(ushort, float[], short[], short, short)"/>
		public async Task<OperateResult> WriteRXyzwprAsync( ushort Address, float[] Xyzwpr, short[] Config, short UserFrame, short UserTool )
		{
			int num = Xyzwpr.Length * 4 + Config.Length * 2 + 2;
			byte[] robotBuffer = new byte[num];
			ByteTransform.TransByte( Xyzwpr ).CopyTo( robotBuffer, 0 );
			ByteTransform.TransByte( Config ).CopyTo( robotBuffer, 36 );

			OperateResult write = await WriteAsync( FanucHelper.SELECTOR_D, Address, robotBuffer );
			if (!write.IsSuccess) return write;

			if (0 <= UserFrame && UserFrame <= 15)
			{
				if (0 <= UserTool && UserTool <= 15)
				{
					write = await WriteAsync( FanucHelper.SELECTOR_D, (ushort)(Address + 45), ByteTransform.TransByte( new short[] { UserFrame, UserTool } ) );
					if (!write.IsSuccess) return write;
				}
				else
				{
					write = await WriteAsync( FanucHelper.SELECTOR_D, (ushort)(Address + 45), ByteTransform.TransByte( new short[] { UserFrame } ) );
					if (!write.IsSuccess) return write;
				}
			}
			else if (0 <= UserTool && UserTool <= 15)
			{
				write = await WriteAsync( FanucHelper.SELECTOR_D, (ushort)(Address + 46), ByteTransform.TransByte( new short[] { UserTool } ) );
				if (!write.IsSuccess) return write;
			}
			return OperateResult.CreateSuccessResult( );
		}

		/// <inheritdoc cref="WriteRJoint(ushort, float[], short, short)"/>
		public async Task<OperateResult> WriteRJointAsync( ushort address, float[] joint, short UserFrame, short UserTool )
		{
			OperateResult write = await WriteAsync( FanucHelper.SELECTOR_D, (ushort)(address + 26), ByteTransform.TransByte( joint ) );
			if (!write.IsSuccess) return write;
			if (0 <= UserFrame && UserFrame <= 15)
			{
				if (0 <= UserTool && UserTool <= 15)
				{
					write = await WriteAsync( FanucHelper.SELECTOR_D, (ushort)(address + 44), ByteTransform.TransByte( new short[] { 0, UserFrame, UserTool } ) );
					if (!write.IsSuccess) return write;
				}
				else
				{
					write = await WriteAsync( FanucHelper.SELECTOR_D, (ushort)(address + 44), ByteTransform.TransByte( new short[] { 0, UserFrame } ) );
					if (!write.IsSuccess) return write;
				}
			}
			else
			{
				write = await WriteAsync( FanucHelper.SELECTOR_D, (ushort)(address + 44), ByteTransform.TransByte( new short[] { 0 } ) );
				if (!write.IsSuccess) return write;

				if (0 <= UserTool && UserTool <= 15)
				{
					write = await WriteAsync( FanucHelper.SELECTOR_D, (ushort)(address + 44), ByteTransform.TransByte( new short[] { 0, UserTool } ) );
					if (!write.IsSuccess) return write;
				}
			}
			return OperateResult.CreateSuccessResult( );
		}
#endif
		#endregion

		#region Object Override

		/// <inheritdoc/>
		public override string ToString( ) => $"FanucInterfaceNet[{IpAddress}:{Port}]";

		#endregion

		#region Private Member

		private FanucData fanucDataRetain = null;
		private DateTime fanucDataRefreshTime = DateTime.Now.AddSeconds( -10 );
		private System.Reflection.PropertyInfo[] fanucDataPropertyInfo = typeof( FanucData ).GetProperties( );

		private byte[] connect_req = new byte[56];
		private byte[] session_req = new byte[56] { 8, 0, 1, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 192, 0, 0, 0, 0, 16, 14, 0, 0, 1, 1, 79, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

		#endregion
	}
}
