using HslCommunication.Core;
using HslCommunication.Core.Address;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
#if !NET20 && !NET35
using System.Threading.Tasks;
#endif

namespace HslCommunication.Profinet.Melsec.Helper
{
	/// <summary>
	/// MC协议的辅助类对象，提供了MC协议的读写操作的基本支持
	/// </summary>
	public class McHelper
	{
		/// <summary>
		/// 返回按照字单位读取的最低的长度信息
		/// </summary>
		/// <param name="type">MC协议的类型</param>
		/// <returns>长度信息</returns>
		private static int GetReadWordLength( McType type )
		{
			if (type == McType.McBinary || type == McType.McRBinary) return 950;
			return 460;
		}

		/// <summary>
		/// 返回按照位单位读取的最低的长度信息
		/// </summary>
		/// <param name="type">MC协议的类型</param>
		/// <returns>长度信息</returns>
		private static int GetReadBoolLength( McType type )
		{
			if (type == McType.McBinary || type == McType.McRBinary) return 7168;
			return 3584;
		}

		/// <inheritdoc cref="IReadWriteNet.Read(string, ushort)"/>
		/// <remarks>
		/// 初步支持普通的数据地址之外，还额外支持高级的地址写法，以下是示例（适用于MC协议为二进制和ASCII格式）：<br />
		/// [商业授权] 扩展的数据地址: 表示为 ext=1;W100  访问扩展区域为1的W100的地址信息<br />
		/// [商业授权] 缓冲存储器地址: 表示为 mem=32  访问地址为32的本站缓冲存储器地址<br />
		/// [商业授权] 智能模块地址：表示为 module=3;4106  访问模块号3，偏移地址是4106的数据，偏移地址需要根据模块的详细信息来确认。<br />
		/// [商业授权] 基于标签的地址: 表示位 s=AAA  假如标签的名称为AAA，但是标签的读取是有条件的，详细参照<see cref="McBinaryHelper.ReadTags(IReadWriteMc, string[], ushort[])"/><br />
		/// </remarks>
		public static OperateResult<byte[]> Read( IReadWriteMc mc, string address, ushort length )
		{
			if (mc.McType == McType.McBinary && address.StartsWith( "s=" ) || address.StartsWith( "S=" ))
			{
				return McBinaryHelper.ReadTags( mc, new string[] { address.Substring( 2 ) }, new ushort[] { length } );
			}
			else if ((mc.McType == McType.McBinary || mc.McType == McType.MCAscii) &&
				Regex.IsMatch( address, "ext=[0-9]+;", RegexOptions.IgnoreCase ))
			{
				string extStr = Regex.Match( address, "ext=[0-9]+;" ).Value;
				ushort ext = ushort.Parse( Regex.Match( extStr, "[0-9]+" ).Value );
				return ReadExtend( mc, ext, address.Substring( extStr.Length ), length );
			}
			else if ((mc.McType == McType.McBinary || mc.McType == McType.MCAscii) &&
				Regex.IsMatch( address, "mem=", RegexOptions.IgnoreCase ))
			{
				return ReadMemory( mc, address.Substring( 4 ), length );
			}
			else if ((mc.McType == McType.McBinary || mc.McType == McType.MCAscii) &&
				Regex.IsMatch( address, "module=[0-9]+;", RegexOptions.IgnoreCase ))
			{
				string moduleStr = Regex.Match( address, "module=[0-9]+;" ).Value;
				ushort module = ushort.Parse( Regex.Match( moduleStr, "[0-9]+" ).Value );
				return ReadSmartModule( mc, module, address.Substring( moduleStr.Length ), (ushort)(length * 2) );
			}
			else
			{
				// 分析地址
				OperateResult<McAddressData> addressResult = mc.McAnalysisAddress( address, length );
				if (!addressResult.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( addressResult );

				List<byte> bytesContent = new List<byte>( );
				ushort alreadyFinished = 0;
				while (alreadyFinished < length)
				{
					ushort readLength = (ushort)Math.Min( length - alreadyFinished, GetReadWordLength( mc.McType ) );
					addressResult.Content.Length = readLength;

					// 核心交互
					byte[] command =
						mc.McType == McType.McBinary ? McBinaryHelper.BuildReadMcCoreCommand( addressResult.Content, false ) :
						mc.McType == McType.MCAscii ? McAsciiHelper.BuildAsciiReadMcCoreCommand( addressResult.Content, false ) :
						mc.McType == McType.McRBinary ? MelsecMcRNet.BuildReadMcCoreCommand( addressResult.Content, false ) : null;

					var read = mc.ReadFromCoreServer( command );
					if (!read.IsSuccess) return read;

					bytesContent.AddRange( mc.ExtractActualData( read.Content, false ) );
					alreadyFinished += readLength;

					// 字的话就是正常的偏移位置，如果是位的话，就转到位的数据
					if (addressResult.Content.McDataType.DataType == 0)
						addressResult.Content.AddressStart += readLength;
					else
						addressResult.Content.AddressStart += readLength * 16;
				}
				return OperateResult.CreateSuccessResult( bytesContent.ToArray( ) );
			}
		}

		/// <inheritdoc cref="IReadWriteNet.Write(string, byte[])"/>
		public static OperateResult Write( IReadWriteMc mc, string address, byte[] value )
		{
			// 分析地址
			OperateResult<McAddressData> addressResult = mc.McAnalysisAddress( address, 0 );
			if (!addressResult.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( addressResult );

			// 创建核心报文
			byte[] coreResult =
				mc.McType == McType.McBinary ? McBinaryHelper.BuildWriteWordCoreCommand( addressResult.Content, value ) :
				mc.McType == McType.MCAscii ? McAsciiHelper.BuildAsciiWriteWordCoreCommand( addressResult.Content, value ) :
				mc.McType == McType.McRBinary ? MelsecMcRNet.BuildWriteWordCoreCommand( addressResult.Content, value ) : null;

			// 核心交互
			OperateResult<byte[]> read = mc.ReadFromCoreServer( coreResult );
			if (!read.IsSuccess) return read;

			// 成功
			return OperateResult.CreateSuccessResult( );
		}


		/// <inheritdoc cref="IReadWriteNet.ReadBool(string, ushort)"/>
		/// <remarks>
		/// 当读取的长度过大时，会自动进行切割，对于二进制格式，切割长度为7168，对于ASCII格式协议来说，切割长度则是3584
		/// </remarks>
		public static OperateResult<bool[]> ReadBool( IReadWriteMc mc, string address, ushort length )
		{
			if (address.IndexOf( '.' ) > 0)
			{
				string[] addressSplits = address.SplitDot( );
				int bitIndex = 0;
				try
				{
					bitIndex = Convert.ToInt32( addressSplits[1] );
				}
				catch (Exception ex)
				{
					return new OperateResult<bool[]>( "Bit Index format wrong, " + ex.Message );
				}
				ushort len = (ushort)((length + bitIndex + 15) / 16);

				OperateResult<byte[]> read = mc.Read( addressSplits[0], len );
				if (!read.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( read );

				return OperateResult.CreateSuccessResult( read.Content.ToBoolArray( ).SelectMiddle( bitIndex, length ) );
			}
			else
			{
				// 分析地址
				OperateResult<McAddressData> addressResult = mc.McAnalysisAddress( address, length );
				if (!addressResult.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( addressResult );

				List<bool> boolContent = new List<bool>( );
				ushort alreadyFinished = 0;
				while (alreadyFinished < length)
				{
					ushort readLength = (ushort)Math.Min( length - alreadyFinished, GetReadBoolLength( mc.McType ) );
					addressResult.Content.Length = readLength;

					// 获取指令
					byte[] coreResult =
					mc.McType == McType.McBinary ? McBinaryHelper.BuildReadMcCoreCommand( addressResult.Content, true ) :
					mc.McType == McType.MCAscii ? McAsciiHelper.BuildAsciiReadMcCoreCommand( addressResult.Content, true ) :
					mc.McType == McType.McRBinary ? MelsecMcRNet.BuildReadMcCoreCommand( addressResult.Content, true ) : null;

					// 核心交互
					var read = mc.ReadFromCoreServer( coreResult );
					if (!read.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( read );

					boolContent.AddRange( mc.ExtractActualData( read.Content, true ).Select( m => m == 0x01 ).Take( readLength ).ToArray( ) );
					alreadyFinished += readLength;

					addressResult.Content.AddressStart += readLength;
				}

				// 转化bool数组
				return OperateResult.CreateSuccessResult( boolContent.ToArray( ) );
			}
		}

		/// <inheritdoc cref="IReadWriteNet.Write(string, bool[])"/>
		public static OperateResult Write( IReadWriteMc mc, string address, bool[] values )
		{
			// 分析地址
			OperateResult<McAddressData> addressResult = mc.McAnalysisAddress( address, 0 );
			if (!addressResult.IsSuccess) return addressResult;

			byte[] coreResult =
				mc.McType == McType.McBinary ? McBinaryHelper.BuildWriteBitCoreCommand( addressResult.Content, values ) :
				mc.McType == McType.MCAscii ? McAsciiHelper.BuildAsciiWriteBitCoreCommand( addressResult.Content, values ) :
				mc.McType == McType.McRBinary ? MelsecMcRNet.BuildWriteBitCoreCommand( addressResult.Content, values ) : null;

			// 核心交互
			OperateResult<byte[]> read = mc.ReadFromCoreServer( coreResult );
			if (!read.IsSuccess) return read;

			// 成功
			return OperateResult.CreateSuccessResult( );
		}

#if !NET20 && !NET35
		/// <inheritdoc cref="IReadWriteNet.Read(string, ushort)"/>
		public static async Task<OperateResult<byte[]>> ReadAsync( IReadWriteMc mc, string address, ushort length )
		{
			if (mc.McType == McType.McBinary && address.StartsWith( "s=" ) || address.StartsWith( "S=" ))
			{
				return await McBinaryHelper.ReadTagsAsync( mc, new string[] { address.Substring( 2 ) }, new ushort[] { length } );
			}
			else if ((mc.McType == McType.McBinary || mc.McType == McType.MCAscii) && Regex.IsMatch( address, "ext=[0-9]+;", RegexOptions.IgnoreCase ))
			{
				string extStr = Regex.Match( address, "ext=[0-9]+;" ).Value;
				ushort ext = ushort.Parse( Regex.Match( extStr, "[0-9]+" ).Value );
				return await ReadExtendAsync( mc, ext, address.Substring( extStr.Length ), length );
			}
			else if ((mc.McType == McType.McBinary || mc.McType == McType.MCAscii) && Regex.IsMatch( address, "mem=", RegexOptions.IgnoreCase ))
			{
				return await ReadMemoryAsync( mc, address.Substring( 4 ), length );
			}
			else if ((mc.McType == McType.McBinary || mc.McType == McType.MCAscii) && Regex.IsMatch( address, "module=[0-9]+;", RegexOptions.IgnoreCase ))
			{
				string moduleStr = Regex.Match( address, "module=[0-9]+;" ).Value;
				ushort module = ushort.Parse( Regex.Match( moduleStr, "[0-9]+" ).Value );
				return await ReadSmartModuleAsync( mc, module, address.Substring( moduleStr.Length ), (ushort)(length * 2) );
			}
			else
			{
				// 分析地址
				OperateResult<McAddressData> addressResult = mc.McAnalysisAddress( address, length );
				if (!addressResult.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( addressResult );

				List<byte> bytesContent = new List<byte>( );
				ushort alreadyFinished = 0;
				while (alreadyFinished < length)
				{
					ushort readLength = (ushort)Math.Min( length - alreadyFinished, GetReadWordLength( mc.McType ) );
					addressResult.Content.Length = readLength;

					// 核心交互
					byte[] command =
						mc.McType == McType.McBinary ? McBinaryHelper.BuildReadMcCoreCommand( addressResult.Content, false ) :
						mc.McType == McType.MCAscii ? McAsciiHelper.BuildAsciiReadMcCoreCommand( addressResult.Content, false ) :
						mc.McType == McType.McRBinary ? MelsecMcRNet.BuildReadMcCoreCommand( addressResult.Content, false ) : null;

					var read = await mc.ReadFromCoreServerAsync( command );
					if (!read.IsSuccess) return read;

					bytesContent.AddRange( mc.ExtractActualData( read.Content, false ) );
					alreadyFinished += readLength;

					// 字的话就是正常的偏移位置，如果是位的话，就转到位的数据
					if (addressResult.Content.McDataType.DataType == 0)
						addressResult.Content.AddressStart += readLength;
					else
						addressResult.Content.AddressStart += readLength * 16;
				}
				return OperateResult.CreateSuccessResult( bytesContent.ToArray( ) );
			}
		}

		/// <inheritdoc cref="IReadWriteNet.Write(string, byte[])"/>
		public static async Task<OperateResult> WriteAsync( IReadWriteMc mc, string address, byte[] value )
		{
			// 分析地址
			OperateResult<McAddressData> addressResult = mc.McAnalysisAddress( address, 0 );
			if (!addressResult.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( addressResult );

			// 创建核心报文
			byte[] coreResult =
				mc.McType == McType.McBinary ? McBinaryHelper.BuildWriteWordCoreCommand( addressResult.Content, value ) :
				mc.McType == McType.MCAscii ? McAsciiHelper.BuildAsciiWriteWordCoreCommand( addressResult.Content, value ) :
				mc.McType == McType.McRBinary ? MelsecMcRNet.BuildWriteWordCoreCommand( addressResult.Content, value ) : null;

			// 核心交互
			OperateResult<byte[]> read = await mc.ReadFromCoreServerAsync( coreResult );
			if (!read.IsSuccess) return read;

			// 成功
			return OperateResult.CreateSuccessResult( );
		}

		/// <inheritdoc cref="IReadWriteNet.ReadBool(string, ushort)"/>
		public static async Task<OperateResult<bool[]>> ReadBoolAsync( IReadWriteMc mc, string address, ushort length )
		{
			if (address.IndexOf( '.' ) > 0)
			{
				string[] addressSplits = address.SplitDot( );
				int bitIndex = 0;
				try
				{
					bitIndex = Convert.ToInt32( addressSplits[1] );
				}
				catch (Exception ex)
				{
					return new OperateResult<bool[]>( "Bit Index format wrong, " + ex.Message );
				}
				ushort len = (ushort)((length + bitIndex + 15) / 16);

				OperateResult<byte[]> read = await mc.ReadAsync( addressSplits[0], len );
				if (!read.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( read );

				return OperateResult.CreateSuccessResult( read.Content.ToBoolArray( ).SelectMiddle( bitIndex, length ) );
			}
			else
			{
				// 分析地址
				OperateResult<McAddressData> addressResult = mc.McAnalysisAddress( address, length );
				if (!addressResult.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( addressResult );

				List<bool> boolContent = new List<bool>( );
				ushort alreadyFinished = 0;
				while (alreadyFinished < length)
				{
					ushort readLength = (ushort)Math.Min( length - alreadyFinished, GetReadBoolLength( mc.McType ) );
					addressResult.Content.Length = readLength;

					// 获取指令
					byte[] coreResult =
					mc.McType == McType.McBinary ? McBinaryHelper.BuildReadMcCoreCommand( addressResult.Content, true ) :
					mc.McType == McType.MCAscii ? McAsciiHelper.BuildAsciiReadMcCoreCommand( addressResult.Content, true ) :
					mc.McType == McType.McRBinary ? MelsecMcRNet.BuildReadMcCoreCommand( addressResult.Content, true ) : null;

					// 核心交互
					var read = await mc.ReadFromCoreServerAsync( coreResult );
					if (!read.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( read );

					boolContent.AddRange( mc.ExtractActualData( read.Content, true ).Select( m => m == 0x01 ).Take( readLength ).ToArray( ) );
					alreadyFinished += readLength;

					addressResult.Content.AddressStart += readLength;
				}

				// 转化bool数组
				return OperateResult.CreateSuccessResult( boolContent.ToArray( ) );
			}
		}

		/// <inheritdoc cref="IReadWriteNet.Write(string, bool[])"/>
		public static async Task<OperateResult> WriteAsync( IReadWriteMc mc, string address, bool[] values )
		{
			// 分析地址
			OperateResult<McAddressData> addressResult = mc.McAnalysisAddress( address, 0 );
			if (!addressResult.IsSuccess) return addressResult;

			byte[] coreResult =
				mc.McType == McType.McBinary ? McBinaryHelper.BuildWriteBitCoreCommand( addressResult.Content, values ) :
				mc.McType == McType.MCAscii ? McAsciiHelper.BuildAsciiWriteBitCoreCommand( addressResult.Content, values ) :
				mc.McType == McType.McRBinary ? MelsecMcRNet.BuildWriteBitCoreCommand( addressResult.Content, values ) : null;

			// 核心交互
			OperateResult<byte[]> read = await mc.ReadFromCoreServerAsync( coreResult );
			if (!read.IsSuccess) return read;

			// 成功
			return OperateResult.CreateSuccessResult( );
		}
#endif

		/// <summary>
		/// 随机读取PLC的数据信息，可以跨地址，跨类型组合，但是每个地址只能读取一个word，也就是2个字节的内容。收到结果后，需要自行解析数据<br />
		/// Randomly read PLC data information, which can be combined across addresses and types, but each address can only read one word, 
		/// which is the content of 2 bytes. After receiving the results, you need to parse the data yourself
		/// </summary>
		/// <param name="mc">MC协议通信对象</param>
		/// <param name="address">所有的地址的集合</param>
		/// <remarks>
		/// 访问安装有 Q 系列 C24/E71 的站 QCPU 上位站 经由 Q 系列兼容网络系统 MELSECNET/H MELSECNET/10 Ethernet 的 QCPU 其他站 时
		/// 访问点数········1≦ 字访问点数 双字访问点数 ≦192
		/// <br />
		/// 访问 QnACPU 其他站 经由 QnA 系列兼容网络系统 MELSECNET/10 Ethernet 的 Q/QnACPU 其他站 时访问点数········1≦ 字访问点数 双字访问点数 ≦96
		/// <br />
		/// 访问上述以外的 PLC CPU 其他站 时访问点数········1≦字访问点数≦10
		/// </remarks>
		/// <example>
		/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Profinet\melsecTest.cs" region="ReadExample3" title="随机字读取示例" /></example>
		/// <returns>结果</returns>
		public static OperateResult<byte[]> ReadRandom( IReadWriteMc mc, string[] address )
		{
			McAddressData[] mcAddressDatas = new McAddressData[address.Length];
			for (int i = 0; i < address.Length; i++)
			{
				OperateResult<McAddressData> addressResult = McAddressData.ParseMelsecFrom( address[i], 1 );
				if (!addressResult.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( addressResult );

				mcAddressDatas[i] = addressResult.Content;
			}

			byte[] coreResult =
				mc.McType == McType.McBinary ? McBinaryHelper.BuildReadRandomWordCommand( mcAddressDatas ) :
				mc.McType == McType.MCAscii ? McAsciiHelper.BuildAsciiReadRandomWordCommand( mcAddressDatas ) : null;

			// 核心交互
			var read = mc.ReadFromCoreServer( coreResult );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( read );

			// 数据解析，需要传入是否使用位的参数
			return OperateResult.CreateSuccessResult( mc.ExtractActualData( read.Content, false ) );
		}

		/// <summary>
		/// 随机读取PLC的数据信息，可以跨地址，跨类型组合，每个地址是任意的长度。收到结果后，需要自行解析数据，目前只支持字地址，比如D区，W区，R区，不支持X，Y，M，B，L等等<br />
		/// Read the data information of the PLC randomly. It can be combined across addresses and types. Each address is of any length. After receiving the results, 
		/// you need to parse the data yourself. Currently, only word addresses are supported, such as D area, W area, R area. X, Y, M, B, L, etc
		/// </summary>
		/// <param name="mc">MC协议通信对象</param>
		/// <param name="address">所有的地址的集合</param>
		/// <param name="length">每个地址的长度信息</param>
		/// <remarks>
		/// 实际测试不一定所有的plc都可以读取成功，具体情况需要具体分析
		/// <br />
		/// 1 块数按照下列要求指定 120 ≧ 字软元件块数 + 位软元件块数
		/// <br />
		/// 2 各软元件点数按照下列要求指定 960 ≧ 字软元件各块的合计点数 + 位软元件各块的合计点数
		/// </remarks>
		/// <example>
		/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Profinet\melsecTest.cs" region="ReadExample4" title="随机批量字读取示例" />
		/// </example>
		/// <returns>结果</returns>
		public static OperateResult<byte[]> ReadRandom( IReadWriteMc mc, string[] address, ushort[] length )
		{
			if (length.Length != address.Length) return new OperateResult<byte[]>( StringResources.Language.TwoParametersLengthIsNotSame );

			McAddressData[] mcAddressDatas = new McAddressData[address.Length];
			for (int i = 0; i < address.Length; i++)
			{
				OperateResult<McAddressData> addressResult = McAddressData.ParseMelsecFrom( address[i], length[i] );
				if (!addressResult.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( addressResult );

				mcAddressDatas[i] = addressResult.Content;
			}

			byte[] coreResult =
				mc.McType == McType.McBinary ? McBinaryHelper.BuildReadRandomCommand( mcAddressDatas ) :
				mc.McType == McType.MCAscii ? McAsciiHelper.BuildAsciiReadRandomCommand( mcAddressDatas ) : null;

			// 核心交互
			var read = mc.ReadFromCoreServer( coreResult );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( read );

			// 数据解析，需要传入是否使用位的参数
			return OperateResult.CreateSuccessResult( mc.ExtractActualData( read.Content, false ) );
		}

		/// <summary>
		/// 随机读取PLC的数据信息，可以跨地址，跨类型组合，但是每个地址只能读取一个word，也就是2个字节的内容。收到结果后，自动转换为了short类型的数组<br />
		/// Randomly read PLC data information, which can be combined across addresses and types, but each address can only read one word, 
		/// which is the content of 2 bytes. After receiving the result, it is automatically converted to an array of type short.
		/// </summary>
		/// <param name="mc">MC协议的通信对象</param>
		/// <param name="address">所有的地址的集合</param>
		/// <remarks>
		/// 访问安装有 Q 系列 C24/E71 的站 QCPU 上位站 经由 Q 系列兼容网络系统 MELSECNET/H MELSECNET/10 Ethernet 的 QCPU 其他站 时
		/// 访问点数········1≦ 字访问点数 双字访问点数 ≦192
		/// 
		/// 访问 QnACPU 其他站 经由 QnA 系列兼容网络系统 MELSECNET/10 Ethernet 的 Q/QnACPU 其他站 时访问点数········1≦ 字访问点数 双字访问点数 ≦96
		/// 
		/// 访问上述以外的 PLC CPU 其他站 时访问点数········1≦字访问点数≦10
		/// </remarks>
		/// <returns>包含是否成功的结果对象</returns>
		public static OperateResult<short[]> ReadRandomInt16( IReadWriteMc mc, string[] address )
		{
			OperateResult<byte[]> read = ReadRandom( mc, address );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<short[]>( read );

			return OperateResult.CreateSuccessResult( mc.ByteTransform.TransInt16( read.Content, 0, address.Length ) );
		}

		/// <summary>
		/// 随机读取PLC的数据信息，可以跨地址，跨类型组合，但是每个地址只能读取一个word，也就是2个字节的内容。收到结果后，自动转换为了ushort类型的数组<br />
		/// Randomly read PLC data information, which can be combined across addresses and types, but each address can only read one word, 
		/// which is the content of 2 bytes. After receiving the result, it is automatically converted to an array of type ushort.
		/// </summary>
		/// <param name="mc">MC协议的通信对象</param>
		/// <param name="address">所有的地址的集合</param>
		/// <remarks>
		/// 访问安装有 Q 系列 C24/E71 的站 QCPU 上位站 经由 Q 系列兼容网络系统 MELSECNET/H MELSECNET/10 Ethernet 的 QCPU 其他站 时
		/// 访问点数········1≦ 字访问点数 双字访问点数 ≦192
		/// 
		/// 访问 QnACPU 其他站 经由 QnA 系列兼容网络系统 MELSECNET/10 Ethernet 的 Q/QnACPU 其他站 时访问点数········1≦ 字访问点数 双字访问点数 ≦96
		/// 
		/// 访问上述以外的 PLC CPU 其他站 时访问点数········1≦字访问点数≦10
		/// </remarks>
		/// <returns>包含是否成功的结果对象</returns>
		public static OperateResult<ushort[]> ReadRandomUInt16( IReadWriteMc mc, string[] address )
		{
			OperateResult<byte[]> read = ReadRandom( mc, address );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<ushort[]>( read );

			return OperateResult.CreateSuccessResult( mc.ByteTransform.TransUInt16( read.Content, 0, address.Length ) );
		}

#if !NET20 && !NET35
		/// <inheritdoc cref="ReadRandom(IReadWriteMc, string[])"/>
		public static async Task<OperateResult<byte[]>> ReadRandomAsync( IReadWriteMc mc, string[] address )
		{
			McAddressData[] mcAddressDatas = new McAddressData[address.Length];
			for (int i = 0; i < address.Length; i++)
			{
				OperateResult<McAddressData> addressResult = McAddressData.ParseMelsecFrom( address[i], 1 );
				if (!addressResult.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( addressResult );

				mcAddressDatas[i] = addressResult.Content;
			}

			byte[] coreResult =
				mc.McType == McType.McBinary ? McBinaryHelper.BuildReadRandomWordCommand( mcAddressDatas ) :
				mc.McType == McType.MCAscii ? McAsciiHelper.BuildAsciiReadRandomWordCommand( mcAddressDatas ) : null;

			// 核心交互
			var read = await mc.ReadFromCoreServerAsync( coreResult );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( read );

			// 数据解析，需要传入是否使用位的参数
			return OperateResult.CreateSuccessResult( mc.ExtractActualData( read.Content, false ) );
		}

		/// <inheritdoc cref="ReadRandom(IReadWriteMc, string[], ushort[])"/>
		public static async Task<OperateResult<byte[]>> ReadRandomAsync( IReadWriteMc mc, string[] address, ushort[] length )
		{
			if (length.Length != address.Length) return new OperateResult<byte[]>( StringResources.Language.TwoParametersLengthIsNotSame );

			McAddressData[] mcAddressDatas = new McAddressData[address.Length];
			for (int i = 0; i < address.Length; i++)
			{
				OperateResult<McAddressData> addressResult = McAddressData.ParseMelsecFrom( address[i], length[i] );
				if (!addressResult.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( addressResult );

				mcAddressDatas[i] = addressResult.Content;
			}

			byte[] coreResult =
				mc.McType == McType.McBinary ? McBinaryHelper.BuildReadRandomCommand( mcAddressDatas ) :
				mc.McType == McType.MCAscii ? McAsciiHelper.BuildAsciiReadRandomCommand( mcAddressDatas ) : null;

			// 核心交互
			var read = await mc.ReadFromCoreServerAsync( coreResult );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( read );

			// 数据解析，需要传入是否使用位的参数
			return OperateResult.CreateSuccessResult( mc.ExtractActualData( read.Content, false ) );
		}

		/// <inheritdoc cref="ReadRandomInt16(IReadWriteMc, string[])"/>
		public static async Task<OperateResult<short[]>> ReadRandomInt16Async( IReadWriteMc mc, string[] address )
		{
			OperateResult<byte[]> read = await ReadRandomAsync( mc, address );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<short[]>( read );

			return OperateResult.CreateSuccessResult( mc.ByteTransform.TransInt16( read.Content, 0, address.Length ) );
		}

		/// <inheritdoc cref="ReadRandomUInt16(IReadWriteMc, string[])"/>
		public static async Task<OperateResult<ushort[]>> ReadRandomUInt16Async( IReadWriteMc mc, string[] address )
		{
			OperateResult<byte[]> read = await ReadRandomAsync( mc, address );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<ushort[]>( read );

			return OperateResult.CreateSuccessResult( mc.ByteTransform.TransUInt16( read.Content, 0, address.Length ) );
		}
#endif
		/// <summary>
		/// <b>[商业授权]</b> 读取缓冲寄存器的数据信息，地址直接为偏移地址<br />
		/// <b>[Authorization]</b> Read the data information of the buffer register, the address is directly the offset address
		/// </summary>
		/// <remarks>
		/// 本指令不可以访问下述缓冲存储器:<br />
		/// 1. 本站(SLMP对应设备)上安装的智能功能模块<br />
		/// 2. 其它站缓冲存储器<br />
		/// </remarks>
		/// <param name="mc">MC通信对象</param>
		/// <param name="address">偏移地址</param>
		/// <param name="length">读取长度</param>
		/// <returns>读取的内容</returns>
		public static OperateResult<byte[]> ReadMemory( IReadWriteMc mc, string address, ushort length )
		{
			if (!Authorization.asdniasnfaksndiqwhawfskhfaiw( )) return new OperateResult<byte[]>( StringResources.Language.InsufficientPrivileges );

			OperateResult<byte[]> coreResult =
				mc.McType == McType.McBinary ? McBinaryHelper.BuildReadMemoryCommand( address, length ) :
				mc.McType == McType.MCAscii ? McAsciiHelper.BuildAsciiReadMemoryCommand( address, length ) : null;
			if (!coreResult.IsSuccess) return coreResult;

			// 核心交互
			var read = mc.ReadFromCoreServer( coreResult.Content );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( read );

			// 数据初级解析，需要传入是否使用位的参数
			return OperateResult.CreateSuccessResult( mc.ExtractActualData( read.Content, false ) );
		}

#if !NET35 && !NET20
		/// <inheritdoc cref="ReadMemory(IReadWriteMc, string, ushort)"/>
		public static async Task<OperateResult<byte[]>> ReadMemoryAsync( IReadWriteMc mc, string address, ushort length )
		{
			if (!Authorization.asdniasnfaksndiqwhawfskhfaiw( )) return new OperateResult<byte[]>( StringResources.Language.InsufficientPrivileges );

			OperateResult<byte[]> coreResult =
				mc.McType == McType.McBinary ? McBinaryHelper.BuildReadMemoryCommand( address, length ) :
				mc.McType == McType.MCAscii ? McAsciiHelper.BuildAsciiReadMemoryCommand( address, length ) : null;
			if (!coreResult.IsSuccess) return coreResult;

			// 核心交互
			var read = await mc.ReadFromCoreServerAsync( coreResult.Content );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( read );

			// 数据初级解析，需要传入是否使用位的参数
			return OperateResult.CreateSuccessResult( mc.ExtractActualData( read.Content, false ) );
		}
#endif

		/// <summary>
		/// <b>[商业授权]</b> 读取智能模块的数据信息，需要指定模块地址，偏移地址，读取的字节长度<br />
		/// <b>[Authorization]</b> To read the extended data information, you need to enter the extended value information in addition to the original address and length information
		/// </summary>
		/// <param name="mc">MC通信对象</param>
		/// <param name="module">模块地址</param>
		/// <param name="address">地址</param>
		/// <param name="length">数据长度</param>
		/// <returns>返回结果</returns>
		public static OperateResult<byte[]> ReadSmartModule( IReadWriteMc mc, ushort module, string address, ushort length )
		{
			if (!Authorization.asdniasnfaksndiqwhawfskhfaiw( )) return new OperateResult<byte[]>( StringResources.Language.InsufficientPrivileges );

			OperateResult<byte[]> coreResult =
				mc.McType == McType.McBinary ? McBinaryHelper.BuildReadSmartModule( module, address, length ) :
				mc.McType == McType.MCAscii ? McAsciiHelper.BuildAsciiReadSmartModule( module, address, length ) : null;
			if (!coreResult.IsSuccess) return coreResult;

			// 核心交互
			var read = mc.ReadFromCoreServer( coreResult.Content );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( read );

			// 数据初级解析，需要传入是否使用位的参数
			return OperateResult.CreateSuccessResult( mc.ExtractActualData( read.Content, false ) );
		}
#if !NET20 && !NET35
		/// <inheritdoc cref="ReadSmartModule(IReadWriteMc, ushort, string, ushort)"/>
		public static async Task<OperateResult<byte[]>> ReadSmartModuleAsync( IReadWriteMc mc, ushort module, string address, ushort length )
		{
			if (!Authorization.asdniasnfaksndiqwhawfskhfaiw( )) return new OperateResult<byte[]>( StringResources.Language.InsufficientPrivileges );

			OperateResult<byte[]> coreResult =
				mc.McType == McType.McBinary ? McBinaryHelper.BuildReadSmartModule( module, address, length ) :
				mc.McType == McType.MCAscii ? McAsciiHelper.BuildAsciiReadSmartModule( module, address, length ) : null;
			if (!coreResult.IsSuccess) return coreResult;

			// 核心交互
			var read = await mc.ReadFromCoreServerAsync( coreResult.Content );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( read );

			// 数据初级解析，需要传入是否使用位的参数
			return OperateResult.CreateSuccessResult( mc.ExtractActualData( read.Content, false ) );
		}
#endif
		/// <summary>
		/// <b>[商业授权]</b> 读取扩展的数据信息，需要在原有的地址，长度信息之外，输入扩展值信息<br />
		/// <b>[Authorization]</b> To read the extended data information, you need to enter the extended value information in addition to the original address and length information
		/// </summary>
		/// <param name="mc">MC通信对象</param>
		/// <param name="extend">扩展信息</param>
		/// <param name="address">地址</param>
		/// <param name="length">数据长度</param>
		/// <returns>返回结果</returns>
		public static OperateResult<byte[]> ReadExtend( IReadWriteMc mc, ushort extend, string address, ushort length )
		{
			if (!Authorization.asdniasnfaksndiqwhawfskhfaiw( )) return new OperateResult<byte[]>( StringResources.Language.InsufficientPrivileges );

			OperateResult<McAddressData> addressResult = mc.McAnalysisAddress( address, length );
			if (!addressResult.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( addressResult );

			byte[] coreResult =
				mc.McType == McType.McBinary ? McBinaryHelper.BuildReadMcCoreExtendCommand( addressResult.Content, extend, false ) :
				mc.McType == McType.MCAscii ? McAsciiHelper.BuildAsciiReadMcCoreExtendCommand( addressResult.Content, extend, false ) : null;

			// 核心交互
			var read = mc.ReadFromCoreServer( coreResult );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( read );

			return OperateResult.CreateSuccessResult( mc.ExtractActualData( read.Content, false ) );
		}
#if !NET35 && !NET20
		/// <inheritdoc cref="ReadExtend(IReadWriteMc, ushort, string, ushort)"/>
		public static async Task<OperateResult<byte[]>> ReadExtendAsync( IReadWriteMc mc, ushort extend, string address, ushort length )
		{
			if (!Authorization.asdniasnfaksndiqwhawfskhfaiw( )) return new OperateResult<byte[]>( StringResources.Language.InsufficientPrivileges );

			OperateResult<McAddressData> addressResult = mc.McAnalysisAddress( address, length );
			if (!addressResult.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( addressResult );

			byte[] coreResult =
				mc.McType == McType.McBinary ? McBinaryHelper.BuildReadMcCoreExtendCommand( addressResult.Content, extend, false ) :
				mc.McType == McType.MCAscii ? McAsciiHelper.BuildAsciiReadMcCoreExtendCommand( addressResult.Content, extend, false ) : null;

			// 核心交互
			var read = await mc.ReadFromCoreServerAsync( coreResult );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( read );

			return OperateResult.CreateSuccessResult( mc.ExtractActualData( read.Content, false ) );
		}
#endif

		/// <summary>
		/// 远程Run操作<br />
		/// Remote Run Operation
		/// </summary>
		/// <param name="mc">MC协议通信对象</param>
		/// <returns>是否成功</returns>
		public static OperateResult RemoteRun( IReadWriteMc mc )
		{
			return 
				mc.McType == McType.McBinary ? mc.ReadFromCoreServer( new byte[] { 0x01, 0x10, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00 } ) :
				mc.McType == McType.MCAscii ? mc.ReadFromCoreServer( Encoding.ASCII.GetBytes( "1001000000010000" ) ) :
				new OperateResult<byte[]>( StringResources.Language.NotSupportedFunction );
		}

		/// <summary>
		/// 远程Stop操作<br />
		/// Remote Stop operation
		/// </summary>
		/// <param name="mc">MC协议通信对象</param>
		/// <returns>是否成功</returns>
		public static OperateResult RemoteStop( IReadWriteMc mc )
		{
			return 
				mc.McType == McType.McBinary ? mc.ReadFromCoreServer( new byte[] { 0x02, 0x10, 0x00, 0x00, 0x01, 0x00 } ) :
				mc.McType == McType.MCAscii ? mc.ReadFromCoreServer( Encoding.ASCII.GetBytes( "100200000001" ) ) :
				new OperateResult<byte[]>( StringResources.Language.NotSupportedFunction );
		}

		/// <summary>
		/// 远程Reset操作<br />
		/// Remote Reset Operation
		/// </summary>
		/// <param name="mc">MC协议通信对象</param>
		/// <returns>是否成功</returns>
		public static OperateResult RemoteReset( IReadWriteMc mc )
		{
			return 
				mc.McType == McType.McBinary ? mc.ReadFromCoreServer( new byte[] { 0x06, 0x10, 0x00, 0x00, 0x01, 0x00 } ) :
				mc.McType == McType.MCAscii ? mc.ReadFromCoreServer( Encoding.ASCII.GetBytes( "100600000001" ) ) :
				new OperateResult<byte[]>( StringResources.Language.NotSupportedFunction );
		}

		/// <summary>
		/// 读取PLC的型号信息，例如 Q02HCPU<br />
		/// Read PLC model information, such as Q02HCPU
		/// </summary>
		/// <param name="mc">MC协议通信对象</param>
		/// <returns>返回型号的结果对象</returns>
		public static OperateResult<string> ReadPlcType( IReadWriteMc mc )
		{
			// 核心交互
			OperateResult<byte[]> read =
				mc.McType == McType.McBinary ? mc.ReadFromCoreServer( new byte[] { 0x01, 0x01, 0x00, 0x00 } ) :
				mc.McType == McType.MCAscii ? mc.ReadFromCoreServer(  Encoding.ASCII.GetBytes( "01010000" ) ) :
				new OperateResult<byte[]>( StringResources.Language.NotSupportedFunction );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<string>( read );

			// 成功
			return OperateResult.CreateSuccessResult( Encoding.ASCII.GetString( read.Content, 0, 16 ).TrimEnd( ) );
		}

		/// <summary>
		/// LED 熄灭 出错代码初始化<br />
		/// LED off Error code initialization
		/// </summary>
		/// <param name="mc">MC协议通信对象</param>
		/// <returns>是否成功</returns>
		public static OperateResult ErrorStateReset( IReadWriteMc mc )
		{
			return
				mc.McType == McType.McBinary ? mc.ReadFromCoreServer( new byte[] { 0x17, 0x16, 0x00, 0x00 } ) :
				mc.McType == McType.MCAscii ? mc.ReadFromCoreServer( Encoding.ASCII.GetBytes( "16170000" ) ) :
				new OperateResult<byte[]>( StringResources.Language.NotSupportedFunction );
		}
#if !NET35 && !NET20
		/// <inheritdoc cref="RemoteRun"/>
		public static async Task<OperateResult> RemoteRunAsync( IReadWriteMc mc )
		{
			return
				mc.McType == McType.McBinary ? await mc.ReadFromCoreServerAsync( new byte[] { 0x01, 0x10, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00 } ) :
				mc.McType == McType.MCAscii ? await mc.ReadFromCoreServerAsync( Encoding.ASCII.GetBytes( "1001000000010000" ) ) :
				new OperateResult<byte[]>( StringResources.Language.NotSupportedFunction );
		}

		/// <inheritdoc cref="RemoteStop"/>
		public static async Task<OperateResult> RemoteStopAsync( IReadWriteMc mc )
		{
			return 
				mc.McType == McType.McBinary ? await mc.ReadFromCoreServerAsync( new byte[] { 0x02, 0x10, 0x00, 0x00, 0x01, 0x00 } ) :
				mc.McType == McType.MCAscii ? await mc.ReadFromCoreServerAsync( Encoding.ASCII.GetBytes( "100200000001" ) ) :
				new OperateResult<byte[]>( StringResources.Language.NotSupportedFunction );
		}

		/// <inheritdoc cref="RemoteReset"/>
		public static async Task<OperateResult> RemoteResetAsync( IReadWriteMc mc )
		{
			return
				mc.McType == McType.McBinary ? await mc.ReadFromCoreServerAsync( new byte[] { 0x06, 0x10, 0x00, 0x00, 0x01, 0x00 } ) :
				mc.McType == McType.MCAscii ? await mc.ReadFromCoreServerAsync( Encoding.ASCII.GetBytes( "100600000001" ) ) :
				new OperateResult<byte[]>( StringResources.Language.NotSupportedFunction );
		}

		/// <inheritdoc cref="ReadPlcType"/>
		public static async Task<OperateResult<string>> ReadPlcTypeAsync( IReadWriteMc mc )
		{
			// 核心交互
			OperateResult<byte[]> read =
				mc.McType == McType.McBinary ? await mc.ReadFromCoreServerAsync( new byte[] { 0x01, 0x01, 0x00, 0x00 } ) :
				mc.McType == McType.MCAscii ? await mc.ReadFromCoreServerAsync( Encoding.ASCII.GetBytes( "01010000" ) ) :
				new OperateResult<byte[]>( StringResources.Language.NotSupportedFunction );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<string>( read );

			// 成功
			return OperateResult.CreateSuccessResult( Encoding.ASCII.GetString( read.Content, 0, 16 ).TrimEnd( ) );
		}

		/// <inheritdoc cref="ErrorStateReset"/>
		public static async Task<OperateResult> ErrorStateResetAsync( IReadWriteMc mc )
		{
			return
				mc.McType == McType.McBinary ? await mc.ReadFromCoreServerAsync( new byte[] { 0x17, 0x16, 0x00, 0x00 } ) :
				mc.McType == McType.MCAscii ? await mc.ReadFromCoreServerAsync( Encoding.ASCII.GetBytes( "16170000" ) ) :
				new OperateResult<byte[]>( StringResources.Language.NotSupportedFunction );
		}
#endif
	}
}
