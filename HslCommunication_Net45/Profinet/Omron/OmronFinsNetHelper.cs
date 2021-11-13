using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HslCommunication.BasicFramework;
using HslCommunication.Core.Address;
using HslCommunication.Core.Net;
using HslCommunication.Core;
#if !NET20 && !NET35
using System.Threading.Tasks;
#endif

namespace HslCommunication.Profinet.Omron
{
	/// <summary>
	/// Omron PLC的FINS协议相关的辅助类，主要是一些地址解析，读写的指令生成。<br />
	/// The auxiliary classes related to the FINS protocol of Omron PLC are mainly some address resolution and the generation of read and write instructions.
	/// </summary>
	public class OmronFinsNetHelper
	{
		#region Static Method Helper

		/// <summary>
		/// 根据读取的地址，长度，是否位读取创建Fins协议的核心报文<br />
		/// According to the read address, length, whether to read the core message that creates the Fins protocol
		/// </summary>
		/// <param name="address">地址，具体格式请参照示例说明</param>
		/// <param name="length">读取的数据长度</param>
		/// <param name="isBit">是否使用位读取</param>
		/// <param name="splitLength">读取的长度切割，默认500</param>
		/// <returns>带有成功标识的Fins核心报文</returns>
		public static OperateResult<List<byte[]>> BuildReadCommand( string address, ushort length, bool isBit, int splitLength = 500 )
		{
			var analysis = OmronFinsAddress.ParseFrom( address, length );
			if (!analysis.IsSuccess) return OperateResult.CreateFailedResult<List<byte[]>>( analysis );

			List<byte[]> cmds = new List<byte[]>( );
			int[] lengths = SoftBasic.SplitIntegerToArray( length, isBit ? int.MaxValue : splitLength );
			for (int i = 0; i < lengths.Length; i++)
			{
				byte[] _PLCCommand = new byte[8];
				_PLCCommand[0] = 0x01;    // 读取存储区数据
				_PLCCommand[1] = 0x01;
				if (isBit)
					_PLCCommand[2] = analysis.Content.BitCode;
				else
					_PLCCommand[2] = analysis.Content.WordCode;
				_PLCCommand[3] = (byte)(analysis.Content.AddressStart / 16 / 256);
				_PLCCommand[4] = (byte)(analysis.Content.AddressStart / 16 % 256);
				_PLCCommand[5] = (byte)(analysis.Content.AddressStart % 16);
				_PLCCommand[6] = (byte)(lengths[i] / 256);                           // 长度
				_PLCCommand[7] = (byte)(lengths[i] % 256);

				cmds.Add( _PLCCommand );
				// 起始地址偏移
				analysis.Content.AddressStart += isBit ? lengths[i] : lengths[i] * 16;
			}

			return OperateResult.CreateSuccessResult( cmds );
		}

		/// <summary>
		/// 根据写入的地址，数据，是否位写入生成Fins协议的核心报文<br />
		/// According to the written address, data, whether the bit is written to generate the core message of the Fins protocol
		/// </summary>
		/// <param name="address">地址内容，具体格式请参照示例说明</param>
		/// <param name="value">实际的数据</param>
		/// <param name="isBit">是否位数据</param>
		/// <returns>带有成功标识的Fins核心报文</returns>
		public static OperateResult<byte[]> BuildWriteWordCommand( string address, byte[] value, bool isBit )
		{
			var analysis = OmronFinsAddress.ParseFrom( address, 0 );
			if (!analysis.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( analysis );

			byte[] _PLCCommand = new byte[8 + value.Length];
			_PLCCommand[0] = 0x01;
			_PLCCommand[1] = 0x02;

			if (isBit)
				_PLCCommand[2] = analysis.Content.BitCode;
			else
				_PLCCommand[2] = analysis.Content.WordCode;

			_PLCCommand[3] = (byte)(analysis.Content.AddressStart / 16 / 256);
			_PLCCommand[4] = (byte)(analysis.Content.AddressStart / 16 % 256);
			_PLCCommand[5] = (byte)(analysis.Content.AddressStart % 16);
			if (isBit)
			{
				_PLCCommand[6] = (byte)(value.Length / 256);
				_PLCCommand[7] = (byte)(value.Length % 256);
			}
			else
			{
				_PLCCommand[6] = (byte)(value.Length / 2 / 256);
				_PLCCommand[7] = (byte)(value.Length / 2 % 256);
			}

			value.CopyTo( _PLCCommand, 8 );

			return OperateResult.CreateSuccessResult( _PLCCommand );
		}



		/// <summary>
		/// 验证欧姆龙的Fins-TCP返回的数据是否正确的数据，如果正确的话，并返回所有的数据内容<br />
		/// Verify that the data returned by Omron's Fins-TCP is correct data, if correct, and return all data content
		/// </summary>
		/// <param name="response">来自欧姆龙返回的数据内容</param>
		/// <returns>带有是否成功的结果对象</returns>
		public static OperateResult<byte[]> ResponseValidAnalysis( byte[] response )
		{
			if (response.Length >= 16)
			{
				// 提取错误码 -> Extracting error Codes
				byte[] buffer = new byte[4];
				buffer[0] = response[15];
				buffer[1] = response[14];
				buffer[2] = response[13];
				buffer[3] = response[12];

				int err = BitConverter.ToInt32( buffer, 0 );
				if (err > 0) return new OperateResult<byte[]>( err, GetStatusDescription( err ) );

				return UdpResponseValidAnalysis( response.RemoveBegin( 16 ) );
			}

			return new OperateResult<byte[]>( StringResources.Language.OmronReceiveDataError );
		}


		/// <summary>
		/// 验证欧姆龙的Fins-Udp返回的数据是否正确的数据，如果正确的话，并返回所有的数据内容<br />
		/// Verify that the data returned by Omron's Fins-Udp is correct data, if correct, and return all data content
		/// </summary>
		/// <param name="response">来自欧姆龙返回的数据内容</param>
		/// <returns>带有是否成功的结果对象</returns>
		public static OperateResult<byte[]> UdpResponseValidAnalysis( byte[] response )
		{
			if (response.Length >= 14)
			{
				int err = response[12] * 256 + response[13];
				// if (err > 0) return new OperateResult<byte[]>( err, StringResources.Language.OmronReceiveDataError );

				if( (response[10] == 0x01 & response[11] == 0x01) ||
					(response[10] == 0x01 & response[11] == 0x04) ||
					(response[10] == 0x02 & response[11] == 0x01) ||
					(response[10] == 0x03 & response[11] == 0x06) ||
					(response[10] == 0x05 & response[11] == 0x01) ||
					(response[10] == 0x05 & response[11] == 0x02) ||
					(response[10] == 0x06 & response[11] == 0x01) ||
					(response[10] == 0x06 & response[11] == 0x20) ||
					(response[10] == 0x07 & response[11] == 0x01) ||
					(response[10] == 0x09 & response[11] == 0x20) ||
					(response[10] == 0x21 & response[11] == 0x02) ||
					(response[10] == 0x22 & response[11] == 0x02))
				{
					// Read 操作
					byte[] content = new byte[response.Length - 14];
					if (content.Length > 0) Array.Copy( response, 14, content, 0, content.Length );

					OperateResult<byte[]> success = OperateResult.CreateSuccessResult( content );
					if (content.Length == 0) success.IsSuccess = false;
					success.ErrorCode = err;
					success.Message = GetStatusDescription( err ) + " Received:" + SoftBasic.ByteToHexString( response, ' ' );
					return success;
				}
				else
				{
					// Write 操作
					OperateResult<byte[]> success = OperateResult.CreateSuccessResult( new byte[0] );
					success.ErrorCode = err;
					success.Message = GetStatusDescription( err ) + " Received:" + SoftBasic.ByteToHexString( response, ' ' );
					return success;
				}
			}

			return new OperateResult<byte[]>( StringResources.Language.OmronReceiveDataError );
		}

		/// <summary>
		/// 根据欧姆龙返回的错误码，获取错误信息的字符串描述文本<br />
		/// According to the error code returned by Omron, get the string description text of the error message
		/// </summary>
		/// <param name="err">错误码</param>
		/// <returns>文本描述</returns>
		public static string GetStatusDescription( int err )
		{
			switch (err)
			{
				case 0x00: return StringResources.Language.OmronStatus0;
				case 0x01: return StringResources.Language.OmronStatus1;
				case 0x02: return StringResources.Language.OmronStatus2;
				case 0x03: return StringResources.Language.OmronStatus3;
				case 0x20: return StringResources.Language.OmronStatus20;
				case 0x21: return StringResources.Language.OmronStatus21;
				case 0x22: return StringResources.Language.OmronStatus22;
				case 0x23: return StringResources.Language.OmronStatus23;
				case 0x24: return StringResources.Language.OmronStatus24;
				case 0x25: return StringResources.Language.OmronStatus25;
				default: return StringResources.Language.UnknownError;
			}
		}

		#endregion

		#region ReadWriteHelper

		/// <summary>
		/// 从欧姆龙PLC中读取想要的数据，返回读取结果，读取长度的单位为字，地址格式为"D100","C100","W100","H100","A100"<br />
		/// Read the desired data from the Omron PLC and return the read result. The unit of the read length is word. The address format is "D100", "C100", "W100", "H100", "A100"
		/// </summary>
		/// <param name="omron">PLC设备的连接对象</param>
		/// <param name="address">读取地址，格式为"D100","C100","W100","H100","A100"</param>
		/// <param name="length">读取的数据长度</param>
		/// <param name="splits">分割信息</param>
		/// <returns>带成功标志的结果数据对象</returns>
		public static OperateResult<byte[]> Read( IReadWriteDevice omron, string address, ushort length, int splits )
		{
			// 获取指令
			var command = OmronFinsNetHelper.BuildReadCommand( address, length, false, splits );
			if (!command.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( command );

			List<byte> contentArray = new List<byte>( );
			for (int i = 0; i < command.Content.Count; i++)
			{
				// 核心数据交互
				OperateResult<byte[]> read = omron.ReadFromCoreServer( command.Content[i] );
				if (!read.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( read );

				// 读取到了正确的数据
				contentArray.AddRange( read.Content );
			}

			return OperateResult.CreateSuccessResult( contentArray.ToArray( ) );
		}

		/// <summary>
		/// 向PLC写入数据，数据格式为原始的字节类型，地址格式为"D100","C100","W100","H100","A100"<br />
		/// Write data to PLC, the data format is the original byte type, and the address format is "D100", "C100", "W100", "H100", "A100"
		/// </summary>
		/// <param name="omron">PLC设备的连接对象</param>
		/// <param name="address">初始地址</param>
		/// <param name="value">原始的字节数据</param>
		/// <returns>结果</returns>
		public static OperateResult Write( IReadWriteDevice omron, string address, byte[] value )
		{
			// 获取指令
			var command = OmronFinsNetHelper.BuildWriteWordCommand( address, value, false );
			if (!command.IsSuccess) return command;

			// 核心数据交互
			OperateResult<byte[]> read = omron.ReadFromCoreServer( command.Content );
			if (!read.IsSuccess) return read;

			// 成功
			return OperateResult.CreateSuccessResult( );
		}
#if !NET35 && !NET20
		/// <inheritdoc cref="Read(IReadWriteDevice, string, ushort, int)"/>
		public static async Task<OperateResult<byte[]>> ReadAsync( IReadWriteDevice omron, string address, ushort length, int splits )
		{
			// 获取指令
			var command = OmronFinsNetHelper.BuildReadCommand( address, length, false, splits );
			if (!command.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( command );

			List<byte> contentArray = new List<byte>( );
			for (int i = 0; i < command.Content.Count; i++)
			{
				// 核心数据交互
				OperateResult<byte[]> read = await omron.ReadFromCoreServerAsync( command.Content[i] );
				if (!read.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( read );

				// 读取到了正确的数据
				contentArray.AddRange( read.Content );
			}

			return OperateResult.CreateSuccessResult( contentArray.ToArray( ) );
		}

		/// <inheritdoc cref="Write(IReadWriteDevice, string, byte[])"/>
		public static async Task<OperateResult> WriteAsync( IReadWriteDevice omron, string address, byte[] value )
		{
			// 获取指令
			var command = OmronFinsNetHelper.BuildWriteWordCommand( address, value, false );
			if (!command.IsSuccess) return command;

			// 核心数据交互
			OperateResult<byte[]> read = await omron.ReadFromCoreServerAsync( command.Content );
			if (!read.IsSuccess) return read;

			// 成功
			return OperateResult.CreateSuccessResult( );
		}
#endif

		/// <summary>
		/// 从欧姆龙PLC中批量读取位软元件，地址格式为"D100.0","C100.0","W100.0","H100.0","A100.0"<br />
		/// Read bit devices in batches from Omron PLC with address format "D100.0", "C100.0", "W100.0", "H100.0", "A100.0"
		/// </summary>
		/// <param name="omron">PLC设备的连接对象</param>
		/// <param name="address">读取地址，格式为"D100","C100","W100","H100","A100"</param>
		/// <param name="length">读取的长度</param>
		/// <param name="splits">分割信息</param>
		/// <returns>带成功标志的结果数据对象</returns>
		public static OperateResult<bool[]> ReadBool( IReadWriteDevice omron, string address, ushort length, int splits )
		{
			// 获取指令
			var command = OmronFinsNetHelper.BuildReadCommand( address, length, true, splits );
			if (!command.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( command );

			List<bool> contentArray = new List<bool>( );
			for (int i = 0; i < command.Content.Count; i++)
			{
				// 核心数据交互
				OperateResult<byte[]> read = omron.ReadFromCoreServer( command.Content[i] );
				if (!read.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( read );

				// 返回正确的数据信息
				contentArray.AddRange( read.Content.Select( m => m != 0x00 ) );
			}

			return OperateResult.CreateSuccessResult( contentArray.ToArray( ) );
		}

		/// <summary>
		/// 向PLC中位软元件写入bool数组，返回是否写入成功，比如你写入D100,values[0]对应D100.0，地址格式为"D100.0","C100.0","W100.0","H100.0","A100.0"<br />
		/// Write the bool array to the PLC's median device and return whether the write was successful. For example, if you write D100, values [0] corresponds to D100.0 
		/// and the address format is "D100.0", "C100.0", "W100. 0 "," H100.0 "," A100.0 "
		/// </summary>
		/// <param name="omron">PLC设备的连接对象</param>
		/// <param name="address">要写入的数据地址</param>
		/// <param name="values">要写入的实际数据，可以指定任意的长度</param>
		/// <returns>返回写入结果</returns>
		public static OperateResult Write( IReadWriteDevice omron, string address, bool[] values )
		{
			// 获取指令
			var command = OmronFinsNetHelper.BuildWriteWordCommand( address, values.Select( m => m ? (byte)0x01 : (byte)0x00 ).ToArray( ), true );
			if (!command.IsSuccess) return command;

			// 核心数据交互
			OperateResult<byte[]> read = omron.ReadFromCoreServer( command.Content );
			if (!read.IsSuccess) return read;

			// 写入成功
			return OperateResult.CreateSuccessResult( );
		}

#if !NET35 && !NET20
		/// <inheritdoc cref="ReadBool(IReadWriteDevice, string, ushort, int)"/>
		public static async Task<OperateResult<bool[]>> ReadBoolAsync( IReadWriteDevice omron, string address, ushort length, int splits )
		{
			// 获取指令
			var command = OmronFinsNetHelper.BuildReadCommand( address, length, true, splits );
			if (!command.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( command );

			List<bool> contentArray = new List<bool>( );
			for (int i = 0; i < command.Content.Count; i++)
			{
				// 核心数据交互
				OperateResult<byte[]> read = await omron.ReadFromCoreServerAsync( command.Content[i] );
				if (!read.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( read );

				// 返回正确的数据信息
				contentArray.AddRange( read.Content.Select( m => m != 0x00 ) );
			}

			return OperateResult.CreateSuccessResult( contentArray.ToArray( ) );
		}

		/// <inheritdoc cref="Write(IReadWriteDevice, string, bool[])"/>
		public static async Task<OperateResult> WriteAsync( IReadWriteDevice omron, string address, bool[] values )
		{
			// 获取指令
			var command = OmronFinsNetHelper.BuildWriteWordCommand( address, values.Select( m => m ? (byte)0x01 : (byte)0x00 ).ToArray( ), true );
			if (!command.IsSuccess) return command;

			// 核心数据交互
			OperateResult<byte[]> read = await omron.ReadFromCoreServerAsync( command.Content );
			if (!read.IsSuccess) return read;

			// 写入成功
			return OperateResult.CreateSuccessResult( );
		}
#endif
		#endregion

		#region Advanced Operate

		/// <summary>
		/// 将CPU单元的操作模式更改为RUN，从而使PLC能够执行其程序。<br />
		/// Changes the CPU Unit’s operating mode to RUN, enabling the PLC to execute its program.
		/// </summary>
		/// <remarks>
		/// 当执行RUN时，CPU单元将开始运行。 在执行RUN之前，您必须确认系统的安全性。 启用“禁止覆盖受保护程序”设置时，无法执行此命令。<br />
		/// The CPU Unit will start operation when RUN is executed. You must confirm the safety of the system before executing RUN.
		/// When the “prohibit overwriting of protected program” setting is enabled, this command cannot be executed.
		/// </remarks>
		/// <param name="omron">PLC设备的连接对象</param>
		/// <returns>是否启动成功</returns>
		public static OperateResult Run( IReadWriteDevice omron ) => omron.ReadFromCoreServer( new byte[] { 0x04, 0x01, 0xFF, 0xFF, 0x04 } );
#if !NET20 && !NET35
		/// <inheritdoc cref="Run(IReadWriteDevice)"/>
		public static async Task<OperateResult> RunAsync( IReadWriteDevice omron ) => await omron.ReadFromCoreServerAsync( new byte[] { 0x04, 0x01, 0xFF, 0xFF, 0x04 } );
#endif
		/// <summary>
		/// 将CPU单元的操作模式更改为PROGRAM，停止程序执行。<br />
		/// Changes the CPU Unit’s operating mode to PROGRAM, stopping program execution.
		/// </summary>
		/// <remarks>
		/// 当执行STOP时，CPU单元将停止操作。 在执行STOP之前，您必须确认系统的安全性。<br />
		/// The CPU Unit will stop operation when STOP is executed. You must confirm the safety of the system before executing STOP.
		/// </remarks>
		/// <param name="omron">PLC设备的连接对象</param>
		/// <returns>是否停止成功</returns>
		public static OperateResult Stop( IReadWriteDevice omron ) => omron.ReadFromCoreServer( new byte[] { 0x04, 0x02, 0xFF, 0xFF } );
#if !NET20 && !NET35
		/// <inheritdoc cref="Stop(IReadWriteDevice)"/>
		public static async Task<OperateResult> StopAsync( IReadWriteDevice omron ) => await omron.ReadFromCoreServerAsync( new byte[] { 0x04, 0x02, 0xFF, 0xFF } );
#endif
		/// <summary>
		/// <b>[商业授权]</b> 读取CPU的一些数据信息，主要包含型号，版本，一些数据块的大小<br />
		/// <b>[Authorization]</b> Read some data information of the CPU, mainly including the model, version, and the size of some data blocks
		/// </summary>
		/// <param name="omron">PLC设备的连接对象</param>
		/// <returns>是否读取成功</returns>
		public static OperateResult<OmronCpuUnitData> ReadCpuUnitData( IReadWriteDevice omron )
		{
			if (!Authorization.asdniasnfaksndiqwhawfskhfaiw( )) return new OperateResult<OmronCpuUnitData>( StringResources.Language.InsufficientPrivileges );
			return omron.ReadFromCoreServer( new byte[] { 0x05, 0x01, 0x00 } ).Then( m => OperateResult.CreateSuccessResult( new OmronCpuUnitData( m ) ) );
		}

#if !NET20 && !NET35
		/// <inheritdoc cref="ReadCpuUnitData(IReadWriteDevice)"/>
		public static async Task<OperateResult<OmronCpuUnitData>> ReadCpuUnitDataAsync( IReadWriteDevice omron )
		{
			if (!Authorization.asdniasnfaksndiqwhawfskhfaiw( )) return new OperateResult<OmronCpuUnitData>( StringResources.Language.InsufficientPrivileges );
			return (await omron.ReadFromCoreServerAsync( new byte[] { 0x05, 0x01, 0x00 } )).Then( m => OperateResult.CreateSuccessResult( new OmronCpuUnitData( m ) ) );
		}
#endif
		/// <summary>
		/// <b>[商业授权]</b> 读取CPU单元的一些操作状态数据，主要包含运行状态，工作模式，错误信息等。<br />
		/// <b>[Authorization]</b> Read some operating status data of the CPU unit, mainly including operating status, working mode, error information, etc.
		/// </summary>
		/// <param name="omron">PLC设备的连接对象</param>
		/// <returns>是否读取成功</returns>
		public static OperateResult<OmronCpuUnitStatus> ReadCpuUnitStatus( IReadWriteDevice omron )
		{
			if (!Authorization.asdniasnfaksndiqwhawfskhfaiw( )) return new OperateResult<OmronCpuUnitStatus>( StringResources.Language.InsufficientPrivileges );
			return omron.ReadFromCoreServer( new byte[] { 0x06, 0x01 } ).Then( m => OperateResult.CreateSuccessResult( new OmronCpuUnitStatus( m ) ) );
		}

#if !NET20 && !NET35
		/// <inheritdoc cref="ReadCpuUnitStatus(IReadWriteDevice)"/>
		public static async Task<OperateResult<OmronCpuUnitStatus>> ReadCpuUnitStatusAsync( IReadWriteDevice omron )
		{
			if (!Authorization.asdniasnfaksndiqwhawfskhfaiw( )) return new OperateResult<OmronCpuUnitStatus>( StringResources.Language.InsufficientPrivileges );
			return (await omron.ReadFromCoreServerAsync( new byte[] { 0x06, 0x01 } )).Then( m => OperateResult.CreateSuccessResult( new OmronCpuUnitStatus( m ) ) );
		}
#endif
		#endregion
	}
}
