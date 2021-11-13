using HslCommunication.Core;
using HslCommunication.Serial;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HslCommunication.BasicFramework;
using System.IO;
#if !NET35 && !NET20
using System.Threading.Tasks;
#endif

namespace HslCommunication.Profinet.AllenBradley
{
	/// <summary>
	/// AB-PLC的DF1通信协议，基于串口实现，通信机制为半双工，目前适用于 Micro-Logix1000,SLC500,SLC 5/03,SLC 5/04，地址示例：N7:1
	/// </summary>
	public class AllenBradleyDF1Serial : SerialDeviceBase
	{
		#region Constructor

		/// <summary>
		/// Instantiate a communication object for a Allenbradley PLC protocol
		/// </summary>
		public AllenBradleyDF1Serial( )
		{
			WordLength     = 2;
			ByteTransform  = new RegularByteTransform( );
			incrementCount = new SoftIncrementCount( ushort.MaxValue, 0, 1 );
			CheckType      = CheckType.CRC16;
		}

		#endregion

		#region Public Properties

		/// <summary>
		/// 站号信息
		/// </summary>
		public byte Station { get; set; }

		/// <summary>
		/// 目标节点号
		/// </summary>
		public byte DstNode { get; set; }

		/// <summary>
		/// 源节点号
		/// </summary>
		public byte SrcNode { get; set; }

		/// <summary>
		/// 校验方式
		/// </summary>
		public CheckType CheckType { get; set; }

		#endregion

		#region Read Write

		/// <summary>
		/// 读取PLC的原始数据信息，地址示例：N7:0  可以携带站号 s=2;N7:0, 携带 dst 和 src 信息，例如 dst=1;src=2;N7:0
		/// </summary>
		/// <param name="address">PLC的地址信息，支持的类型见类型注释说明</param>
		/// <param name="length">读取的长度，单位，字节</param>
		/// <returns>是否读取成功的结果对象</returns>
		public override OperateResult<byte[]> Read( string address, ushort length )
		{
			byte stat = (byte)HslHelper.ExtractParameter( ref address, "s", this.Station );
			byte dst  = (byte)HslHelper.ExtractParameter( ref address, "dst", this.DstNode );
			byte src  = (byte)HslHelper.ExtractParameter( ref address, "src", this.SrcNode );

			OperateResult<byte[]> build = AllenBradleyDF1Serial.BuildProtectedTypedLogicalRead( dst, src, (int)incrementCount.GetCurrentValue( ), address, length );
			if (!build.IsSuccess) return build;

			OperateResult<byte[]> read = ReadFromCoreServer( PackCommand( stat, build.Content ) );
			if (!read.IsSuccess) return read;

			return ExtractActualData( read.Content );
		}

		/// <summary>
		/// 写入PLC的原始数据信息，地址示例：N7:0  可以携带站号 s=2;N7:0, 携带 dst 和 src 信息，例如 dst=1;src=2;N7:0
		/// </summary>
		/// <param name="address">PLC的地址信息，支持的类型见类型注释说明</param>
		/// <param name="value">原始的数据值</param>
		/// <returns>是否写入成功</returns>
		public override OperateResult Write( string address, byte[] value )
		{
			byte stat = (byte)HslHelper.ExtractParameter( ref address, "s", this.Station );
			byte dst  = (byte)HslHelper.ExtractParameter( ref address, "dst", this.DstNode );
			byte src  = (byte)HslHelper.ExtractParameter( ref address, "src", this.SrcNode );

			OperateResult<byte[]> build = AllenBradleyDF1Serial.BuildProtectedTypedLogicalWrite( dst, src, (int)incrementCount.GetCurrentValue( ), address, value );
			if (!build.IsSuccess) return build;

			OperateResult<byte[]> read = ReadFromCoreServer( PackCommand( stat, build.Content ) );
			if (!read.IsSuccess) return read;

			return ExtractActualData( read.Content );
		}

		#endregion

		#region Private Method

		private byte[] CalculateCheckResult( byte station, byte[] command )
		{
			if (CheckType == CheckType.BCC)
			{
				int sum = station;
				for (int i = 0; i < command.Length; i++)
				{
					sum += command[i];
				}
				sum = (byte)~sum;
				sum += 1;
				return new byte[] { (byte)sum };
			}
			else
			{
				byte[] buffer = SoftBasic.SpliceArray(
					new byte[] { station },
					new byte[] { 0x02 },
					command,
					new byte[] { 0x03 } );
				return SoftCRC16.CRC16( buffer, 0xA0, 0x01, 0x00, 0x00 ).SelectLast( 2 );
			}
		}

		/// <summary>
		/// 打包命令的操作，加站号进行打包成完整的数据内容，命令内容为原始命令，打包后会自动补充0x10的值
		/// </summary>
		/// <param name="station">站号信息</param>
		/// <param name="command">等待发送的命令</param>
		/// <returns>打包之后的数据内容</returns>
		public byte[] PackCommand( byte station, byte[] command )
		{
			byte[] check = CalculateCheckResult( station, command );

			MemoryStream ms = new MemoryStream( );
			ms.WriteByte( 0x10 );
			ms.WriteByte( 0x01 );
			ms.WriteByte( station );
			if(station == 0x10) ms.WriteByte( station );
			ms.WriteByte( 0x10 );
			ms.WriteByte( 0x02 );
			for (int i = 0; i < command.Length; i++)
			{
				ms.WriteByte( command[i] );
				if (command[i] == 0x10) ms.WriteByte( command[i] );
			}
			ms.WriteByte( 0x10 );
			ms.WriteByte( 0x03 );
			ms.Write( check, 0, check.Length );

			return ms.ToArray( );
		}

		#endregion

		#region Private Member

		private SoftIncrementCount incrementCount;          // 消息ID信息，每次自增

		#endregion

		#region Static Helper

		/// <summary>
		/// 构建0F-A2命令码的报文读取指令，用来读取文件数据。适用 Micro-Logix1000,SLC500,SLC 5/03,SLC 5/04，地址示例：N7:1<br />
		/// Construct a message read instruction of 0F-A2 command code to read file data. Applicable to Micro-Logix1000, SLC500, SLC 5/03, SLC 5/04, address example: N7:1
		/// </summary>
		/// <param name="dstNode">目标节点号</param>
		/// <param name="srcNode">原节点号</param>
		/// <param name="tns">消息号</param>
		/// <param name="address">PLC的地址信息</param>
		/// <param name="length">读取的数据长度</param>
		/// <returns>初步的报文信息</returns>
		/// <remarks>
		/// 对于SLC 5/01或SLC 5/02而言，一次最多读取82个字节。对于 03 或是 04 为225，236字节取决于是否应用DF1驱动
		/// </remarks>
		public static OperateResult<byte[]> BuildProtectedTypedLogicalRead( byte dstNode, byte srcNode, int tns, string address, ushort length )
		{
			var analysis = AllenBradleySLCNet.AnalysisAddress( address );
			if (!analysis.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( analysis );

			// AB PLC DF1手册 Page 104
			byte[] buffer = new byte[12];
			buffer[ 0] = dstNode;
			buffer[ 1] = srcNode;
			buffer[ 2] = 0x0F;                                            // Command
			buffer[ 3] = 0x00;                                            // STS
			buffer[ 4] = BitConverter.GetBytes( tns )[0];
			buffer[ 5] = BitConverter.GetBytes( tns )[1];
			buffer[ 6] = 0xA2;                                            // Function
			buffer[ 7] = BitConverter.GetBytes( length )[0];              // Bytes Length
			buffer[ 8] = analysis.Content2;                               // File Number
			buffer[ 9] = analysis.Content1;                               // File Type
			buffer[10] = BitConverter.GetBytes( analysis.Content3 )[0];   // Offset
			buffer[11] = BitConverter.GetBytes( analysis.Content3 )[1];

			return OperateResult.CreateSuccessResult( buffer );
		}

		/// <summary>
		/// 构建0F-AA命令码的写入读取指令，用来写入文件数据。适用 Micro-Logix1000,SLC500,SLC 5/03,SLC 5/04，地址示例：N7:1<br />
		/// Construct a write and read command of 0F-AA command code to write file data. Applicable to Micro-Logix1000, SLC500, SLC 5/03, SLC 5/04, address example: N7:1
		/// </summary>
		/// <param name="dstNode">目标节点号</param>
		/// <param name="srcNode">原节点号</param>
		/// <param name="tns">消息号</param>
		/// <param name="address">PLC的地址信息</param>
		/// <param name="data">写入的数据内容</param>
		/// <returns>初步的报文信息</returns>
		/// <remarks>
		/// 对于SLC 5/01或SLC 5/02而言，一次最多读取82个字节。对于 03 或是 04 为225，236字节取决于是否应用DF1驱动
		/// </remarks>
		public static OperateResult<byte[]> BuildProtectedTypedLogicalWrite( byte dstNode, byte srcNode, int tns, string address, byte[] data )
		{
			var analysis = AllenBradleySLCNet.AnalysisAddress( address );
			if (!analysis.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( analysis );

			// AB PLC DF1手册 Page 104
			byte[] buffer = new byte[12 + data.Length];
			buffer[ 0] = dstNode;
			buffer[ 1] = srcNode;
			buffer[ 2] = 0x0F;                                            // Command
			buffer[ 3] = 0x00;                                            // STS
			buffer[ 4] = BitConverter.GetBytes( tns )[0];
			buffer[ 5] = BitConverter.GetBytes( tns )[1];
			buffer[ 6] = 0xAA;                                            // Function
			buffer[ 7] = BitConverter.GetBytes( data.Length )[0];         // Bytes Length
			buffer[ 8] = analysis.Content2;                               // File Number
			buffer[ 9] = analysis.Content1;                               // File Type
			buffer[10] = BitConverter.GetBytes( analysis.Content3 )[0];   // Offset
			buffer[11] = BitConverter.GetBytes( analysis.Content3 )[1];

			data.CopyTo( buffer, 12 );
			return OperateResult.CreateSuccessResult( buffer );
		}

		/// <summary>
		/// 提取返回报文的数据内容，将其转换成实际的数据内容，如果PLC返回了错误信息，则结果对象为失败。<br />
		/// Extract the data content of the returned message and convert it into the actual data content. If the PLC returns an error message, the result object is a failure.
		/// </summary>
		/// <param name="content">PLC返回的报文信息</param>
		/// <returns>结果对象内容</returns>
		public static OperateResult<byte[]> ExtractActualData( byte[] content )
		{
			try
			{
				int startIndex = -1;
				for (int i = 0; i < content.Length; i++)
				{
					if (content[i] == 0x10 && content[i + 1] == 0x02)
					{
						startIndex = i + 2;
						break;
					}
				}

				if (startIndex < 0 || startIndex >= content.Length - 6)
					return new OperateResult<byte[]>( "Message must start with '10 02', source: " + content.ToHexString( ' ' ) );

				// 提炼真实的数据内容，寻找 10 03的结束边界
				MemoryStream ms = new MemoryStream( );
				for (int i = startIndex; i < content.Length - 1; i++)
				{
					if (content[i] == 0x10 && content[i + 1] == 0x10)
					{
						ms.WriteByte( content[i] );
						i++;
						continue;
					}
					if (content[i] == 0x10 && content[i + 1] == 0x03)
					{
						break;
					}
					ms.WriteByte( content[i] );
				}

				content = ms.ToArray( );
				if (content[3] == 0xF0) return new OperateResult<byte[]>( GetExtStatusDescription( content[6] ) );
				if (content[3] != 0x00) return new OperateResult<byte[]>( GetStatusDescription( content[3] ) );

				if (content.Length > 6)
					return OperateResult.CreateSuccessResult( content.RemoveBegin( 6 ) );
				else
					return OperateResult.CreateSuccessResult( new byte[0] );
			}
			catch(Exception ex)
			{
				return new OperateResult<byte[]>( ex.Message + " Source:" + content.ToHexString( ' ' ) );
			}
		}

		/// <summary>
		/// 根据错误代码，来获取错误的具体描述文本
		/// </summary>
		/// <param name="code">错误的代码，非0</param>
		/// <returns>错误的描述文本信息</returns>
		public static string GetStatusDescription( byte code )
		{
			byte low = (byte)(code & 0x0f);
			byte hig = (byte)(code & 0xf0);
			switch (low)
			{
				case 0x01: return "DST node is out of buffer space";
				case 0x02: return "Cannot guarantee delivery: link layer(The remote node specified does not ACK command.)";
				case 0x03: return "Duplicate token holder detected";
				case 0x04: return "Local port is disconnected";
				case 0x05: return "Application layer timed out waiting for a response";
				case 0x06: return "Duplicate node detected";
				case 0x07: return "Station is offline";
				case 0x08: return "Hardware fault";
			}
			switch (hig)
			{
				case 0x10: return "Illegal command or format";
				case 0x20: return "Host has a problem and will not communicate";
				case 0x30: return "Remote node host is missing, disconnected, or shut down";
				case 0x40: return "Host could not complete function due to hardware fault";
				case 0x50: return "Addressing problem or memory protect rungs";
				case 0x60: return "Function not allowed due to command protection selection";
				case 0x70: return "Processor is in Program mode";
				case 0x80: return "Compatibility mode file missing or communication zone problem";
				case 0x90: return "Remote node cannot buffer command";
				case 0xA0: return "Wait ACK (1775KA buffer full)";
				case 0xB0: return "Remote node problem due to download";
				case 0xC0: return "Wait ACK (1775KA buffer full)";
				case 0xF0: return "Error code in the EXT STS byte";
			}
			return StringResources.Language.UnknownError;
		}

		/// <summary>
		/// 根据错误代码，来获取错误的具体描述文本
		/// </summary>
		/// <param name="code">错误的代码，非0</param>
		/// <returns>错误的描述文本信息</returns>
		public static string GetExtStatusDescription( byte code )
		{
			switch (code)
			{
				case 0x01: return "A field has an illegal value";
				case 0x02: return "Less levels specified in address than minimum for any address";
				case 0x03: return "More levels specified in address than system supports";
				case 0x04: return "Symbol not found";
				case 0x05: return "Symbol is of improper format";
				case 0x06: return "Address doesn’t point to something usable";
				case 0x07: return "File is wrong size";
				case 0x08: return "Cannot complete request, situation has changed since the start of the command";
				case 0x09: return "Data or file is too large";
				case 0x0A: return "Transaction size plus word address is too large";
				case 0x0B: return "Access denied, improper privilege";
				case 0x0C: return "Condition cannot be generated  resource is not available";
				case 0x0D: return "Condition already exists  resource is already available";
				case 0x0E: return "Command cannot be executed";
				case 0x0F: return "Histogram overflow";
				case 0x10: return "No access";
				case 0x11: return "Illegal data type";
				case 0x12: return "Invalid parameter or invalid data";
				case 0x13: return "Address reference exists to deleted area";
				case 0x14: return "Command execution failure for unknown reason; possible PLC3 histogram overflow";
				case 0x15: return "Data conversion error";
				case 0x16: return "Scanner not able to communicate with 1771 rack adapter";
				case 0x17: return "Type mismatch";
				case 0x18: return "1771 module response was not valid";
				case 0x19: return "Duplicated label";
				case 0x1A: return "File is open; another node owns it";
				case 0x1B: return "Another node is the program owner";
				case 0x1C: return "Reserved";
				case 0x1D: return "Reserved";
				case 0x1E: return "Data table element protection violation";
				case 0x1F: return "Temporary internal problem";
				case 0x22: return "Remote rack fault";
				case 0x23: return "Timeout";
				case 0x24: return "Unknown error";
				default: return StringResources.Language.UnknownError;
			}
		}
		#endregion

		#region Object Override

		/// <inheritdoc/>
		public override string ToString( ) => $"AllenBradleyDF1Serial[{PortName}:{BaudRate}]";

		#endregion
	}
}
