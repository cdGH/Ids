using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
#if !NET20 && !NET35
using System.Threading.Tasks;
#endif
using HslCommunication.Core.Net;
using HslCommunication;
using HslCommunication.Core;
using HslCommunication.Core.IMessage;
using HslCommunication.Core.Address;
using HslCommunication.BasicFramework;
using HslCommunication.Reflection;

namespace HslCommunication.Profinet.Fuji
{
	/// <summary>
	/// 富士PLC的SPH通信协议，可以和富士PLC进行通信，<see cref="ConnectionID"/>默认CPU0，需要根据实际进行调整。
	/// </summary>
	/// <remarks>
	/// 地址支持 M1.0, M3.0, M10.0 以及I0, Q0
	/// </remarks>
	/// <example>
	/// 地址支持的列表如下：
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
	///     <term></term>
	///     <term>M</term>
	///     <term>M1.0,M1.100</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term></term>
	///     <term>M</term>
	///     <term>M3.0,M3.100</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term></term>
	///     <term>M</term>
	///     <term>M10.0,M10.100</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term></term>
	///     <term>I</term>
	///     <term>I0,I100</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term></term>
	///     <term>Q</term>
	///     <term>Q0,Q100</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	/// </list>
	/// </example>
	public class FujiSPHNet : NetworkDeviceBase
	{
		#region Contructor

		/// <summary>
		/// 实例化一个默认的对象<br />
		/// Instantiate a default object
		/// </summary>
		public FujiSPHNet( )
		{
			ByteTransform = new RegularByteTransform( );
			WordLength    = 1;
		}

		/// <summary>
		/// 指定IP地址和端口号来实例化一个对象<br />
		/// Specify the IP address and port number to instantiate an object
		/// </summary>
		/// <param name="ipAddress">Ip地址</param>
		/// <param name="port">端口号</param>
		public FujiSPHNet( string ipAddress, int port = 18245 ) : this( )
		{
			IpAddress     = ipAddress;
			Port          = port;
		}

		/// <inheritdoc/>
		protected override INetMessage GetNewNetMessage( ) => new FujiSPHMessage( );

		#endregion

		#region Public Properties

		/// <summary>
		/// 对于 CPU0-CPU7来说是CPU的站号，分为对应 0xFE-0xF7，对于P/PE link, FL-net是模块站号，分别对应0xF6-0xEF<br />
		/// CPU0 to CPU7: SX bus station No. of destination CPU (FEh to F7h); P/PE link, FL-net: SX bus station No. of destination module (F6H to EFH)
		/// </summary>
		public byte ConnectionID { get; set; } = 0xFE;

		#endregion

		#region Read Write

		private OperateResult<byte[]> ReadFujiSPHAddress( FujiSPHAddress address, ushort length )
		{
			OperateResult<List<byte[]>> command = BuildReadCommand( ConnectionID, address, length );
			if (!command.IsSuccess) return command.ConvertFailed<byte[]>( );

			List<byte> array = new List<byte>( );
			for (int i = 0; i < command.Content.Count; i++)
			{
				OperateResult<byte[]> read = ReadFromCoreServer( command.Content[i] );
				if (!read.IsSuccess) return read;

				OperateResult<byte[]> extra = ExtractActualData( read.Content );
				if (!extra.IsSuccess) return extra;

				array.AddRange( extra.Content );
			}
			return OperateResult.CreateSuccessResult( array.ToArray( ) );
		}

		/// <summary>
		/// 批量读取PLC的地址数据，长度单位为字。地址支持M1.1000，M3.1000，M10.1000，返回读取的原始字节数组。<br />
		/// Read PLC address data in batches, the length unit is words. The address supports M1.1000, M3.1000, M10.1000, 
		/// and returns the original byte array read.
		/// </summary>
		/// <param name="address">PLC的地址，支持M1.1000，M3.1000，M10.1000</param>
		/// <param name="length">读取的长度信息，按照字为单位</param>
		/// <returns>包含byte[]的原始字节数据内容</returns>
		[HslMqttApi( "ReadByteArray", "" )]
		public override OperateResult<byte[]> Read( string address, ushort length )
		{
			OperateResult<FujiSPHAddress> analysis = FujiSPHAddress.ParseFrom( address );
			if (!analysis.IsSuccess) return analysis.ConvertFailed<byte[]>( );

			return ReadFujiSPHAddress( analysis.Content, length );
		}

		/// <summary>
		/// 批量写入字节数组到PLC的地址里，地址支持M1.1000，M3.1000，M10.1000，返回是否写入成功。<br />
		/// Batch write byte array to PLC address, the address supports M1.1000, M3.1000, M10.1000, 
		/// and return whether the writing is successful.
		/// </summary>
		/// <param name="address">PLC的地址，支持M1.1000，M3.1000，M10.1000</param>
		/// <param name="value">写入的原始字节数据</param>
		/// <returns>是否写入成功</returns>
		[HslMqttApi( "WriteByteArray", "" )]
		public override OperateResult Write( string address, byte[] value )
		{
			OperateResult<byte[]> command = BuildWriteCommand( ConnectionID, address, value );
			if (!command.IsSuccess) return command.ConvertFailed<byte[]>( );

			OperateResult<byte[]> read = ReadFromCoreServer( command.Content );
			if (!read.IsSuccess) return read;

			OperateResult<byte[]> extra = ExtractActualData( read.Content );
			if (!extra.IsSuccess) return extra;

			return OperateResult.CreateSuccessResult( );
		}

		/// <summary>
		/// 批量读取位数据的方法，需要传入位地址，读取的位长度，地址示例：M1.100.5，M3.1000.12，M10.1000.0<br />
		/// To read the bit data in batches, you need to pass in the bit address, the length of the read bit, address examples: M1.100.5, M3.1000.12, M10.1000.0
		/// </summary>
		/// <param name="address">PLC的地址，示例：M1.100.5，M3.1000.12，M10.1000.0</param>
		/// <param name="length">读取的bool长度信息</param>
		/// <returns>包含bool[]的结果对象</returns>
		[HslMqttApi( "ReadBoolArray", "" )]
		public override OperateResult<bool[]> ReadBool( string address, ushort length )
		{
			OperateResult<FujiSPHAddress> analysis = FujiSPHAddress.ParseFrom( address );
			if (!analysis.IsSuccess) return analysis.ConvertFailed<bool[]>( );

			int bitCount = analysis.Content.BitIndex + length;
			int wordLength = bitCount % 16 == 0 ? bitCount / 16 : bitCount / 16 + 1;

			OperateResult<byte[]> read = ReadFujiSPHAddress( analysis.Content, (ushort)wordLength );
			if (!read.IsSuccess) return read.ConvertFailed<bool[]>( );

			return OperateResult.CreateSuccessResult( read.Content.ToBoolArray( ).SelectMiddle( analysis.Content.BitIndex, length ) );
		}

		/// <summary>
		/// 批量写入位数据的方法，需要传入位地址，等待写入的boo[]数据，地址示例：M1.100.5，M3.1000.12，M10.1000.0<br />
		/// To write bit data in batches, you need to pass in the bit address and wait for the boo[] data to be written. Examples of addresses: M1.100.5, M3.1000.12, M10.1000.0
		/// </summary>
		/// <remarks>
		/// [警告] 由于协议没有提供位写入的命令，所有通过字写入间接实现，先读取字数据，修改中间的位，然后写入字数据，所以本质上不是安全的，确保相关的地址只有上位机可以写入。<br />
		/// [Warning] Since the protocol does not provide commands for bit writing, all are implemented indirectly through word writing. First read the word data, 
		/// modify the bits in the middle, and then write the word data, so it is inherently not safe. Make sure that the relevant address is only The host computer can write.
		/// </remarks>
		/// <param name="address">PLC的地址，示例：M1.100.5，M3.1000.12，M10.1000.0</param>
		/// <param name="value">等待写入的bool数组</param>
		/// <returns>是否写入成功的结果对象</returns>
		[HslMqttApi( "WriteBoolArray", "" )]
		public override OperateResult Write( string address, bool[] value )
		{
			OperateResult<FujiSPHAddress> analysis = FujiSPHAddress.ParseFrom( address );
			if (!analysis.IsSuccess) return analysis.ConvertFailed<bool[]>( );

			int bitCount = analysis.Content.BitIndex + value.Length;
			int wordLength = bitCount % 16 == 0 ? bitCount / 16 : bitCount / 16 + 1;

			OperateResult<byte[]> read = ReadFujiSPHAddress( analysis.Content, (ushort)wordLength );
			if (!read.IsSuccess) return read.ConvertFailed<bool[]>( );

			// 修改其中的某几个位，然后一起批量写入操作
			bool[] writeBoolArray = read.Content.ToBoolArray( );
			value.CopyTo( writeBoolArray, analysis.Content.BitIndex );

			OperateResult<byte[]> command = BuildWriteCommand( ConnectionID, address, writeBoolArray.ToByteArray( ) );
			if (!command.IsSuccess) return command.ConvertFailed<byte[]>( );

			OperateResult<byte[]> write = ReadFromCoreServer( command.Content );
			if (!write.IsSuccess) return write;

			OperateResult<byte[]> extra = ExtractActualData( write.Content );
			if (!extra.IsSuccess) return extra;

			return OperateResult.CreateSuccessResult( );
		}
#if !NET20 && !NET35

		private async Task<OperateResult<byte[]>> ReadFujiSPHAddressAsync( FujiSPHAddress address, ushort length )
		{
			OperateResult<List<byte[]>> command = BuildReadCommand( ConnectionID, address, length );
			if (!command.IsSuccess) return command.ConvertFailed<byte[]>( );

			List<byte> array = new List<byte>( );
			for (int i = 0; i < command.Content.Count; i++)
			{
				OperateResult<byte[]> read = await ReadFromCoreServerAsync( command.Content[i] );
				if (!read.IsSuccess) return read;

				OperateResult<byte[]> extra = ExtractActualData( read.Content );
				if (!extra.IsSuccess) return extra;

				array.AddRange( extra.Content );
			}
			return OperateResult.CreateSuccessResult( array.ToArray( ) );
		}

		/// <inheritdoc cref="Read(string, ushort)"/>
		public async override Task<OperateResult<byte[]>> ReadAsync( string address, ushort length )
		{
			OperateResult<FujiSPHAddress> analysis = FujiSPHAddress.ParseFrom( address );
			if (!analysis.IsSuccess) return analysis.ConvertFailed<byte[]>( );

			return await ReadFujiSPHAddressAsync( analysis.Content, length );
		}

		/// <inheritdoc cref="Write(string, byte[])"/>
		public async override Task<OperateResult> WriteAsync( string address, byte[] value )
		{
			OperateResult<byte[]> command = BuildWriteCommand( ConnectionID, address, value );
			if (!command.IsSuccess) return command.ConvertFailed<byte[]>( );

			OperateResult<byte[]> read = await ReadFromCoreServerAsync( command.Content );
			if (!read.IsSuccess) return read;

			OperateResult<byte[]> extra = ExtractActualData( read.Content );
			if (!extra.IsSuccess) return extra;

			return OperateResult.CreateSuccessResult( );
		}

		/// <inheritdoc cref="ReadBool(string, ushort)"/>
		public async override Task<OperateResult<bool[]>> ReadBoolAsync( string address, ushort length )
		{
			OperateResult<FujiSPHAddress> analysis = FujiSPHAddress.ParseFrom( address );
			if (!analysis.IsSuccess) return analysis.ConvertFailed<bool[]>( );

			int bitCount = analysis.Content.BitIndex + length;
			int wordLength = bitCount % 16 == 0 ? bitCount / 16 : bitCount / 16 + 1;

			OperateResult<byte[]> read = await ReadFujiSPHAddressAsync( analysis.Content, (ushort)wordLength );
			if (!read.IsSuccess) return read.ConvertFailed<bool[]>( );

			return OperateResult.CreateSuccessResult( read.Content.ToBoolArray( ).SelectMiddle( analysis.Content.BitIndex, length ) );
		}

		/// <inheritdoc cref="Write(string, bool[])"/>
		public async override Task<OperateResult> WriteAsync( string address, bool[] value )
		{
			OperateResult<FujiSPHAddress> analysis = FujiSPHAddress.ParseFrom( address );
			if (!analysis.IsSuccess) return analysis.ConvertFailed<bool[]>( );

			int bitCount = analysis.Content.BitIndex + value.Length;
			int wordLength = bitCount % 16 == 0 ? bitCount / 16 : bitCount / 16 + 1;

			OperateResult<byte[]> read = await ReadFujiSPHAddressAsync( analysis.Content, (ushort)wordLength );
			if (!read.IsSuccess) return read.ConvertFailed<bool[]>( );

			// 修改其中的某几个位，然后一起批量写入操作
			bool[] writeBoolArray = read.Content.ToBoolArray( );
			value.CopyTo( writeBoolArray, analysis.Content.BitIndex );

			OperateResult<byte[]> command = BuildWriteCommand( ConnectionID, address, writeBoolArray.ToByteArray( ) );
			if (!command.IsSuccess) return command.ConvertFailed<byte[]>( );

			OperateResult<byte[]> write = await ReadFromCoreServerAsync( command.Content );
			if (!write.IsSuccess) return write;

			OperateResult<byte[]> extra = ExtractActualData( write.Content );
			if (!extra.IsSuccess) return extra;

			return OperateResult.CreateSuccessResult( );
		}
#endif
		#endregion

		#region Advanced Api

		/// <summary>
		/// <b>[Authorization]</b> This command is used to start all the CPUs that exist in a configuration in a batch. 
		/// Each CPU is cold-started or warm-started,depending on its condition. If a CPU is already started up, 
		/// or if the key switch is set at "RUN" position, the CPU does not perform processing for startup, 
		/// which, however, does not result in an error, and a response is returned normally
		/// </summary>
		/// <returns>是否启动成功</returns>
		[HslMqttApi]
		public OperateResult CpuBatchStart( )
		{
			if (!Authorization.asdniasnfaksndiqwhawfskhfaiw( )) return new OperateResult<byte[]>( StringResources.Language.InsufficientPrivileges );

			return ReadFromCoreServer( PackCommand( ConnectionID, 0x04, 0x00, null ) ).Check( ExtractActualData );
		}

		/// <summary>
		/// <b>[Authorization]</b> This command is used to initialize and start all the CPUs that exist in a configuration in a batch. Each CPU is cold-started.
		/// If a CPU is already started up, or if the key switch is set at "RUN" position, the CPU does not perform processing for initialization 
		/// and startup, which, however, does not result in an error, and a response is returned normally.
		/// </summary>
		/// <returns>是否启动成功</returns>
		[HslMqttApi]
		public OperateResult CpuBatchInitializeAndStart( )
		{
			if (!Authorization.asdniasnfaksndiqwhawfskhfaiw( )) return new OperateResult<byte[]>( StringResources.Language.InsufficientPrivileges );

			return ReadFromCoreServer( PackCommand( ConnectionID, 0x04, 0x01, null ) ).Check( ExtractActualData );
		}

		/// <summary>
		/// <b>[Authorization]</b> This command is used to stop all the CPUs that exist in a configuration in a batch.
		/// If a CPU is already stopped, or if the key switch is set at "RUN" position, the CPU does not perform processing for stop, which,
		/// however, does not result in an error, and a response is returned normally.
		/// </summary>
		/// <returns>是否停止成功</returns>
		[HslMqttApi]
		public OperateResult CpuBatchStop( )
		{
			if (!Authorization.asdniasnfaksndiqwhawfskhfaiw( )) return new OperateResult<byte[]>( StringResources.Language.InsufficientPrivileges );
			
			return ReadFromCoreServer( PackCommand( ConnectionID, 0x04, 0x02, null ) ).Check( ExtractActualData );
		}

		/// <summary>
		/// <b>[Authorization]</b> This command is used to stop all the CPUs that exist in a configuration in a batch.
		/// If a CPU is already stopped, or if the key switch is set at "RUN" position, the CPU does not perform processing for stop, which,
		/// however, does not result in an error, and a response is returned normally.
		/// </summary>
		/// <returns>是否复位成功</returns>
		[HslMqttApi]
		public OperateResult CpuBatchReset( )
		{
			if (!Authorization.asdniasnfaksndiqwhawfskhfaiw( )) return new OperateResult<byte[]>( StringResources.Language.InsufficientPrivileges );

			return ReadFromCoreServer( PackCommand( ConnectionID, 0x04, 0x03, null ) ).Check( ExtractActualData );
		}

		/// <summary>
		/// <b>[Authorization]</b> This command is used to start an arbitrary CPU existing in a configuration by specifying it. The CPU may be cold-started or
		/// warm-started, depending on its condition. An error occurs if the CPU is already started. A target CPU is specified by a connection
		/// mode and connection ID.
		/// </summary>
		/// <returns>是否启动成功</returns>
		[HslMqttApi]
		public OperateResult CpuIndividualStart( )
		{
			if (!Authorization.asdniasnfaksndiqwhawfskhfaiw( )) return new OperateResult<byte[]>( StringResources.Language.InsufficientPrivileges );

			return ReadFromCoreServer( PackCommand( ConnectionID, 0x04, 0x04, null ) ).Check( ExtractActualData );
		}

		/// <summary>
		/// <b>[Authorization]</b> This command is used to initialize and start an arbitrary CPU existing in a configuration by specifying it. The CPU is cold-started.
		/// An error occurs if the CPU is already started or if the key switch is set at "RUN" or "STOP" position. A target CPU is specified by
		/// a connection mode and connection ID.
		/// </summary>
		/// <returns>是否启动成功</returns>
		[HslMqttApi]
		public OperateResult CpuIndividualInitializeAndStart( )
		{
			if (!Authorization.asdniasnfaksndiqwhawfskhfaiw( )) return new OperateResult<byte[]>( StringResources.Language.InsufficientPrivileges );

			return ReadFromCoreServer( PackCommand( ConnectionID, 0x04, 0x05, null ) ).Check( ExtractActualData );
		}

		/// <summary>
		/// <b>[Authorization]</b> This command is used to stop an arbitrary CPU existing in a configuration by specifying it. An error occurs if the CPU is already
		/// stopped or if the key switch is set at "RUN" or "STOP" position. A target CPU is specified by a connection mode and connection ID.
		/// </summary>
		/// <returns>是否停止成功</returns>
		[HslMqttApi]
		public OperateResult CpuIndividualStop( )
		{
			if (!Authorization.asdniasnfaksndiqwhawfskhfaiw( )) return new OperateResult<byte[]>( StringResources.Language.InsufficientPrivileges );

			return ReadFromCoreServer( PackCommand( ConnectionID, 0x04, 0x06, null ) ).Check( ExtractActualData );
		}

		/// <summary>
		/// <b>[Authorization]</b> This command is used to reset an arbitrary CPU existing in a configuration by specifying it. An error occurs if the key switch is
		/// set at "RUN" or "STOP" position. A target CPU is specified by a connection mode and connection ID.
		/// </summary>
		/// <returns>是否复位成功</returns>
		[HslMqttApi]
		public OperateResult CpuIndividualReset( )
		{
			if (!Authorization.asdniasnfaksndiqwhawfskhfaiw( )) return new OperateResult<byte[]>( StringResources.Language.InsufficientPrivileges );

			return ReadFromCoreServer( PackCommand( ConnectionID, 0x04, 0x07, null ) ).Check( ExtractActualData );
		}

#if !NET20 && !NET35

		/// <inheritdoc cref="CpuBatchStart"/>
		public async Task<OperateResult> CpuBatchStartAsync( )
		{
			if (!Authorization.asdniasnfaksndiqwhawfskhfaiw( )) return new OperateResult<byte[]>( StringResources.Language.InsufficientPrivileges );

			return (await ReadFromCoreServerAsync( PackCommand( ConnectionID, 0x04, 0x00, null ) )).Check( ExtractActualData );
		}

		/// <inheritdoc cref="CpuBatchInitializeAndStart"/>
		public async Task<OperateResult> CpuBatchInitializeAndStartAsync( )
		{
			if (!Authorization.asdniasnfaksndiqwhawfskhfaiw( )) return new OperateResult<byte[]>( StringResources.Language.InsufficientPrivileges );

			return (await ReadFromCoreServerAsync( PackCommand( ConnectionID, 0x04, 0x01, null ) )).Check( ExtractActualData );
		}

		/// <inheritdoc cref="CpuBatchStop"/>
		public async Task<OperateResult> CpuBatchStopAsync( )
		{
			if (!Authorization.asdniasnfaksndiqwhawfskhfaiw( )) return new OperateResult<byte[]>( StringResources.Language.InsufficientPrivileges );

			return (await ReadFromCoreServerAsync( PackCommand( ConnectionID, 0x04, 0x02, null ) )).Check( ExtractActualData );
		}

		/// <inheritdoc cref="CpuBatchReset"/>
		public async Task<OperateResult> CpuBatchResetAsync( )
		{
			if (!Authorization.asdniasnfaksndiqwhawfskhfaiw( )) return new OperateResult<byte[]>( StringResources.Language.InsufficientPrivileges );

			return (await ReadFromCoreServerAsync( PackCommand( ConnectionID, 0x04, 0x03, null ) )).Check( ExtractActualData );
		}

		/// <inheritdoc cref="CpuIndividualStart"/>
		public async Task<OperateResult> CpuIndividualStartAsync( )
		{
			if (!Authorization.asdniasnfaksndiqwhawfskhfaiw( )) return new OperateResult<byte[]>( StringResources.Language.InsufficientPrivileges );

			return (await ReadFromCoreServerAsync( PackCommand( ConnectionID, 0x04, 0x04, null ) )).Check( ExtractActualData );
		}

		/// <inheritdoc cref="CpuIndividualInitializeAndStartAsync"/>
		public async Task<OperateResult> CpuIndividualInitializeAndStartAsync( )
		{
			if (!Authorization.asdniasnfaksndiqwhawfskhfaiw( )) return new OperateResult<byte[]>( StringResources.Language.InsufficientPrivileges );

			return (await ReadFromCoreServerAsync( PackCommand( ConnectionID, 0x04, 0x05, null ) )).Check( ExtractActualData );
		}

		/// <inheritdoc cref="CpuIndividualStop"/>
		public async Task<OperateResult> CpuIndividualStopAsync( )
		{
			if (!Authorization.asdniasnfaksndiqwhawfskhfaiw( )) return new OperateResult<byte[]>( StringResources.Language.InsufficientPrivileges );

			return (await ReadFromCoreServerAsync( PackCommand( ConnectionID, 0x04, 0x06, null ) )).Check( ExtractActualData );
		}

		/// <inheritdoc cref="CpuIndividualReset"/>
		public async Task<OperateResult> CpuIndividualResetAsync( )
		{
			if (!Authorization.asdniasnfaksndiqwhawfskhfaiw( )) return new OperateResult<byte[]>( StringResources.Language.InsufficientPrivileges );

			return (await ReadFromCoreServerAsync( PackCommand( ConnectionID, 0x04, 0x07, null ) )).Check( ExtractActualData );
		}

#endif
		#endregion

		#region Object Override

		/// <inheritdoc/>
		public override string ToString( ) => $"FujiSPHNet[{IpAddress}:{Port}]";

		#endregion

		#region Static Helper

		/// <summary>
		/// 根据错误代号获取详细的错误描述信息
		/// </summary>
		/// <param name="code">错误码</param>
		/// <returns>错误的描述文本</returns>
		public static string GetErrorDescription( byte code )
		{
			switch (code)
			{
				case 0x10: return "Command cannot be executed because an error occurred in the CPU.";
				case 0x11: return "Command cannot be executed because the CPU is running.";
				case 0x12: return "Command cannot be executed due to the key switch condition of the CPU.";
				case 0x20: return "CPU received undefined command or mode.";
				case 0x22: return "Setting error was found in command header part.";
				case 0x23: return "Transmission is interlocked by a command from another device.";
				case 0x28: return "Requested command cannot be executed because another command is now being executed.";
				case 0x2B: return "Requested command cannot be executed because the loader is now performing another processing( including program change).";
				case 0x2F: return "Requested command cannot be executed because the system is now being initialized.";
				case 0x40: return "Invalid data type or number was specified.";
				case 0x41: return "Specified data cannot be found.";
				case 0x44: return "Specified address exceeds the valid range.";
				case 0x45: return "Address + the number of read/write words exceed the valid range.";
				case 0xA0: return "No module exists at specified destination station No.";
				case 0xA2: return "No response data is returned from the destination module.";
				case 0xA4: return "Command cannot be communicated because an error occurred in the SX bus.";
				case 0xA5: return "Command cannot be communicated because NAK occurred while sending data via the SX bus.";
				default: return StringResources.Language.UnknownError;
			}
		}

		private static byte[] PackCommand( byte connectionId, byte command, byte mode, byte[] data )
		{
			if (data == null) data = new byte[0];
			byte[] buffer = new byte[20 + data.Length];
			buffer[ 0] = 0xFB;
			buffer[ 1] = 0x80;
			buffer[ 2] = 0x80;
			buffer[ 3] = 0x00;
			buffer[ 4] = 0xFF;
			buffer[ 5] = 0x7B;
			buffer[ 6] = connectionId;  // connection id
			buffer[ 7] = 0x00;
			buffer[ 8] = 0x11;
			buffer[ 9] = 0x00;
			buffer[10] = 0x00;
			buffer[11] = 0x00;
			buffer[12] = 0x00;
			buffer[13] = 0x00;
			buffer[14] = command;       // command
			buffer[15] = mode;          // mode
			buffer[16] = 0x00;
			buffer[17] = 0x01;
			buffer[18] = BitConverter.GetBytes( data.Length )[0];  // length
			buffer[19] = BitConverter.GetBytes( data.Length )[1];
			if (data.Length > 0) data.CopyTo( buffer, 20 );
			return buffer;
		}
		
		/// <summary>
		/// 构建读取数据的命令报文
		/// </summary>
		/// <param name="connectionId">连接ID</param>
		/// <param name="address">读取的PLC的地址</param>
		/// <param name="length">读取的长度信息，按照字为单位</param>
		/// <returns>构建成功的读取报文命令</returns>
		public static OperateResult<List<byte[]>> BuildReadCommand( byte connectionId, string address, ushort length )
		{
			OperateResult<FujiSPHAddress> analysis = FujiSPHAddress.ParseFrom( address );
			if (!analysis.IsSuccess) return analysis.ConvertFailed<List<byte[]>>( );

			return BuildReadCommand( connectionId, analysis.Content, length );
		}

		/// <summary>
		/// 构建读取数据的命令报文
		/// </summary>
		/// <param name="connectionId">连接ID</param>
		/// <param name="address">读取的PLC的地址</param>
		/// <param name="length">读取的长度信息，按照字为单位</param>
		/// <returns>构建成功的读取报文命令</returns>
		public static OperateResult<List<byte[]>> BuildReadCommand( byte connectionId, FujiSPHAddress address, ushort length )
		{
			// Up to 486 bytes (234 words) of data can be read or written at a time
			List<byte[]> array = new List<byte[]>( );
			int[] splits = SoftBasic.SplitIntegerToArray( length, 230 );
			for (int i = 0; i < splits.Length; i++)
			{
				byte[] buffer = new byte[6];
				buffer[0] = address.TypeCode;
				buffer[1] = BitConverter.GetBytes( address.AddressStart )[0];
				buffer[2] = BitConverter.GetBytes( address.AddressStart )[1];
				buffer[3] = BitConverter.GetBytes( address.AddressStart )[2];
				buffer[4] = BitConverter.GetBytes( splits[i] )[0];
				buffer[5] = BitConverter.GetBytes( splits[i] )[1];

				array.Add( PackCommand( connectionId, 0x00, 0x00, buffer ) );
				address.AddressStart += splits[i];
			}
			return OperateResult.CreateSuccessResult( array );
		}

		/// <summary>
		/// 构建写入数据的命令报文
		/// </summary>
		/// <param name="connectionId">连接ID</param>
		/// <param name="address">写入的PLC的地址</param>
		/// <param name="data">原始数据内容</param>
		/// <returns>报文信息</returns>
		public static OperateResult<byte[]> BuildWriteCommand( byte connectionId, string address, byte[] data )
		{
			OperateResult<FujiSPHAddress> analysis = FujiSPHAddress.ParseFrom( address );
			if (!analysis.IsSuccess) return analysis.ConvertFailed<byte[]>( );
			int length = data.Length / 2;

			byte[] buffer = new byte[6 + data.Length];
			buffer[0] = analysis.Content.TypeCode;
			buffer[1] = BitConverter.GetBytes( analysis.Content.AddressStart )[0];
			buffer[2] = BitConverter.GetBytes( analysis.Content.AddressStart )[1];
			buffer[3] = BitConverter.GetBytes( analysis.Content.AddressStart )[2];
			buffer[4] = BitConverter.GetBytes( length )[0];
			buffer[5] = BitConverter.GetBytes( length )[1];
			data.CopyTo( buffer, 6 );
			return OperateResult.CreateSuccessResult( PackCommand( connectionId, 0x01, 0x00, buffer ) );
		}

		/// <summary>
		/// 从PLC返回的报文里解析出实际的数据内容，如果发送了错误，则返回失败信息
		/// </summary>
		/// <param name="response">PLC返回的报文信息</param>
		/// <returns>是否成功的结果对象</returns>
		public static OperateResult<byte[]> ExtractActualData( byte[] response )
		{
			try
			{
				if (response[4] != 0x00) return new OperateResult<byte[]>( response[4], GetErrorDescription( response[4] ) );
				if (response.Length > 26) return OperateResult.CreateSuccessResult( response.RemoveBegin( 26 ) );
				return OperateResult.CreateSuccessResult( new byte[0] );
			}
			catch(Exception ex)
			{
				return new OperateResult<byte[]>( ex.Message + " Source: " + response.ToHexString( ' ' ) );
			}
		}

		#endregion
	}
}
