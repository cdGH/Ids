using HslCommunication.BasicFramework;
using HslCommunication.Core;
using HslCommunication.ModBus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
#if !NET20 && !NET35
using System.Threading.Tasks;
#endif

namespace HslCommunication.Profinet.Delta
{
	/// <summary>
	/// 台达PLC的相关的帮助类，公共的地址解析的方法。<br />
	/// Delta PLC related help classes, public address resolution methods.
	/// </summary>
	public class DeltaHelper
	{
		/// <summary>
		/// 根据台达PLC的地址，解析出转换后的modbus协议信息，适用DVP系列，当前的地址仍然支持站号指定，例如s=2;D100<br />
		/// According to the address of Delta PLC, the converted modbus protocol information is parsed out, applicable to DVP series, 
		/// the current address still supports station number designation, such as s=2;D100
		/// </summary>
		/// <param name="address">台达plc的地址信息</param>
		/// <param name="modbusCode">原始的对应的modbus信息</param>
		/// <returns>还原后的modbus地址</returns>
		public static OperateResult<string> PraseDeltaDvpAddress( string address, byte modbusCode )
		{
			try
			{
				string station = string.Empty;
				OperateResult<int> stationPara = HslHelper.ExtractParameter( ref address, "s" );
				if (stationPara.IsSuccess) station = $"s={stationPara.Content};";

				if (modbusCode == ModbusInfo.ReadCoil || modbusCode == ModbusInfo.WriteCoil || modbusCode == ModbusInfo.WriteOneCoil)
				{
					if (address.StartsWith( "S" ) || address.StartsWith( "s" ))
						return OperateResult.CreateSuccessResult( station + Convert.ToInt32( address.Substring( 1 ) ).ToString( ) );
					else if (address.StartsWith( "X" ) || address.StartsWith( "x" ))
						return OperateResult.CreateSuccessResult( station + "x=2;" + (Convert.ToInt32( address.Substring( 1 ), 8 ) + 0x400).ToString( ) );
					else if (address.StartsWith( "Y" ) || address.StartsWith( "y" ))
						return OperateResult.CreateSuccessResult( station + (Convert.ToInt32( address.Substring( 1 ), 8 ) + 0x500).ToString( ) );
					else if (address.StartsWith( "T" ) || address.StartsWith( "t" ))
						return OperateResult.CreateSuccessResult( station + (Convert.ToInt32( address.Substring( 1 ) ) + 0x600).ToString( ) );
					else if (address.StartsWith( "C" ) || address.StartsWith( "c" ))
						return OperateResult.CreateSuccessResult( station + (Convert.ToInt32( address.Substring( 1 ) ) + 0xE00).ToString( ) );
					else if (address.StartsWith( "M" ) || address.StartsWith( "m" ))
					{
						int add = Convert.ToInt32( address.Substring( 1 ) );
						if (add >= 1536)
							return OperateResult.CreateSuccessResult( station + (add - 1536 + 0xB000).ToString( ) );
						else
							return OperateResult.CreateSuccessResult( station + (add + 0x800).ToString( ) );
					}
				}
				else
				{
					if (address.StartsWith( "D" ) || address.StartsWith( "d" ))
					{
						int add = Convert.ToInt32( address.Substring( 1 ) );
						if (add >= 4096)
							return OperateResult.CreateSuccessResult( station + (add - 4096 + 0x9000).ToString( ) );
						else
							return OperateResult.CreateSuccessResult( station + (add + 0x1000).ToString( ) );
					}
					else if (address.StartsWith( "C" ) || address.StartsWith( "c" ))
					{
						int add = Convert.ToInt32( address.Substring( 1 ) );
						if (add >= 200)
							return OperateResult.CreateSuccessResult( station + (add - 200 + 0x0EC8).ToString( ) );
						else
							return OperateResult.CreateSuccessResult( station + (add + 0x0E00).ToString( ) );
					}
					else if (address.StartsWith( "T" ) || address.StartsWith( "t" ))
						return OperateResult.CreateSuccessResult( station + (Convert.ToInt32( address.Substring( 1 ) ) + 0x600).ToString( ) );
				}

				return new OperateResult<string>( StringResources.Language.NotSupportedDataType );
			}
			catch (Exception ex)
			{
				return new OperateResult<string>( ex.Message );
			}
		}

		/// <summary>
		/// 读取台达PLC的bool变量，重写了读M地址时，跨区域读1536地址时，将会分割多次读取
		/// </summary>
		/// <param name="readBoolFunc">底层基础的读取方法</param>
		/// <param name="address">PLC的地址信息</param>
		/// <param name="length">读取的长度信息</param>
		/// <returns>读取的结果</returns>
		public static OperateResult<bool[]> ReadBool( Func<string, ushort, OperateResult<bool[]>> readBoolFunc, string address, ushort length )
		{
			string station = string.Empty;
			OperateResult<int> stationPara = HslHelper.ExtractParameter( ref address, "s" );
			if (stationPara.IsSuccess) station = $"s={stationPara.Content};";

			if (address.StartsWith( "M" ))
			{
				if (int.TryParse( address.Substring( 1 ), out int add ))
				{
					if (add < 1536 && (add + length > 1536))
					{
						// 跨区域读取了，这时候要进行数据切割
						ushort len1 = (ushort)(1536 - add);
						ushort len2 = (ushort)(length - len1);
						OperateResult<bool[]> read1 = readBoolFunc( address, len1 );
						if (!read1.IsSuccess) return read1;

						OperateResult<bool[]> read2 = readBoolFunc( station + "M1536", len2 );
						if (!read2.IsSuccess) return read2;

						return OperateResult.CreateSuccessResult( SoftBasic.SpliceArray( read1.Content, read2.Content ) );
					}
				}
			}
			return readBoolFunc( address, length );
		}

		/// <summary>
		/// 写入台达PLC的bool数据，当发现是M类型的数据，并且地址出现跨1536时，进行切割写入操作
		/// </summary>
		/// <param name="writeBoolFunc">底层的写入操作方法</param>
		/// <param name="address">PLC的起始地址信息</param>
		/// <param name="value">等待写入的数据信息</param>
		/// <returns>是否写入成功</returns>
		public static OperateResult Write( Func<string, bool[], OperateResult> writeBoolFunc, string address, bool[] value )
		{
			string station = string.Empty;
			OperateResult<int> stationPara = HslHelper.ExtractParameter( ref address, "s" );
			if (stationPara.IsSuccess) station = $"s={stationPara.Content};";

			if (address.StartsWith( "M" ))
			{
				if (int.TryParse( address.Substring( 1 ), out int add ))
				{
					if (add < 1536 && (add + value.Length) > 1536)
					{
						// 跨区域写入了，这时候要进行数据切割
						ushort len1 = (ushort)(1536 - add);
						OperateResult write1 = writeBoolFunc( address, value.SelectBegin( len1 ) );
						if (!write1.IsSuccess) return write1;

						OperateResult write2 = writeBoolFunc( station + "M1536", value.RemoveBegin( len1 ) );
						if (!write2.IsSuccess) return write2;

						return OperateResult.CreateSuccessResult( );
					}
				}
			}
			return writeBoolFunc( address, value );
		}

		/// <summary>
		/// 读取台达PLC的原始字节变量，重写了读D地址时，跨区域读4096地址时，将会分割多次读取
		/// </summary>
		/// <param name="readFunc">底层基础的读取方法</param>
		/// <param name="address">PLC的地址信息</param>
		/// <param name="length">读取的长度信息</param>
		/// <returns>读取的结果</returns>
		public static OperateResult<byte[]> Read( Func<string, ushort, OperateResult<byte[]>> readFunc, string address, ushort length )
		{
			string station = string.Empty;
			OperateResult<int> stationPara = HslHelper.ExtractParameter( ref address, "s" );
			if (stationPara.IsSuccess) station = $"s={stationPara.Content};";

			if (address.StartsWith( "D" ))
			{
				if (int.TryParse( address.Substring( 1 ), out int add ))
				{
					if (add < 4096 && (add + length > 4096))
					{
						// 跨区域读取了，这时候要进行数据切割
						ushort len1 = (ushort)(4096 - add);
						ushort len2 = (ushort)(length - len1);
						OperateResult<byte[]> read1 = readFunc( address, len1 );
						if (!read1.IsSuccess) return read1;

						OperateResult<byte[]> read2 = readFunc( station + "D4096", len2 );
						if (!read2.IsSuccess) return read2;

						return OperateResult.CreateSuccessResult( SoftBasic.SpliceArray( read1.Content, read2.Content ) );
					}
				}
			}
			return readFunc( address, length );
		}

		/// <summary>
		/// 写入台达PLC的原始字节数据，当发现是D类型的数据，并且地址出现跨4096时，进行切割写入操作
		/// </summary>
		/// <param name="writeFunc">底层的写入操作方法</param>
		/// <param name="address">PLC的起始地址信息</param>
		/// <param name="value">等待写入的数据信息</param>
		/// <returns>是否写入成功</returns>
		public static OperateResult Write( Func<string, byte[], OperateResult> writeFunc, string address, byte[] value )
		{
			string station = string.Empty;
			OperateResult<int> stationPara = HslHelper.ExtractParameter( ref address, "s" );
			if (stationPara.IsSuccess) station = $"s={stationPara.Content};";

			if (address.StartsWith( "D" ))
			{
				if (int.TryParse( address.Substring( 1 ), out int add ))
				{
					if (add < 4096 && (add + (value.Length / 2) ) > 4096)
					{
						// 跨区域写入了，这时候要进行数据切割
						ushort len1 = (ushort)(4096 - add);
						OperateResult write1 = writeFunc( address, value.SelectBegin( len1 * 2 ) );
						if (!write1.IsSuccess) return write1;

						OperateResult write2 = writeFunc( station + "D4096", value.RemoveBegin( len1 * 2 ) );
						if (!write2.IsSuccess) return write2;

						return OperateResult.CreateSuccessResult( );
					}
				}
			}
			return writeFunc( address, value );
		}

#if !NET20 && !NET35
		/// <inheritdoc cref="ReadBool(Func{string, ushort, OperateResult{bool[]}}, string, ushort)"/>
		public static async Task<OperateResult<bool[]>> ReadBoolAsync( Func<string, ushort, Task<OperateResult<bool[]>>> readBoolFunc, string address, ushort length )
		{
			string station = string.Empty;
			OperateResult<int> stationPara = HslHelper.ExtractParameter( ref address, "s" );
			if (stationPara.IsSuccess) station = $"s={stationPara.Content};";

			if (address.StartsWith( "M" ))
			{
				if (int.TryParse( address.Substring( 1 ), out int add ))
				{
					if (add < 1536 && (add + length > 1536))
					{
						// 跨区域读取了，这时候要进行数据切割
						ushort len1 = (ushort)(1536 - add);
						ushort len2 = (ushort)(length - len1);
						OperateResult<bool[]> read1 = await readBoolFunc( address, len1 );
						if (!read1.IsSuccess) return read1;

						OperateResult<bool[]> read2 = await readBoolFunc( station + "M1536", len2 );
						if (!read2.IsSuccess) return read2;

						return OperateResult.CreateSuccessResult( SoftBasic.SpliceArray( read1.Content, read2.Content ) );
					}
				}
			}
			return await readBoolFunc( address, length );
		}

		/// <inheritdoc cref="Write(Func{string, bool[], OperateResult}, string, bool[])"/>
		public static async Task<OperateResult> WriteAsync( Func<string, bool[], Task<OperateResult>> writeBoolFunc, string address, bool[] value )
		{
			string station = string.Empty;
			OperateResult<int> stationPara = HslHelper.ExtractParameter( ref address, "s" );
			if (stationPara.IsSuccess) station = $"s={stationPara.Content};";

			if (address.StartsWith( "M" ))
			{
				if (int.TryParse( address.Substring( 1 ), out int add ))
				{
					if (add < 1536 && (add + value.Length > 1536))
					{
						// 跨区域写入了，这时候要进行数据切割
						ushort len1 = (ushort)(1536 - add);
						OperateResult write1 = await writeBoolFunc( address, value.SelectBegin( len1 ) );
						if (!write1.IsSuccess) return write1;

						OperateResult write2 = await writeBoolFunc( station + "M1536", value.RemoveBegin( len1 ) );
						if (!write2.IsSuccess) return write2;

						return OperateResult.CreateSuccessResult( );
					}
				}
			}
			return await writeBoolFunc( address, value );
		}

		/// <inheritdoc cref="Read(Func{string, ushort, OperateResult{byte[]}}, string, ushort)"/>
		public static async Task<OperateResult<byte[]>> ReadAsync( Func<string, ushort, Task<OperateResult<byte[]>>> readFunc, string address, ushort length )
		{
			string station = string.Empty;
			OperateResult<int> stationPara = HslHelper.ExtractParameter( ref address, "s" );
			if (stationPara.IsSuccess) station = $"s={stationPara.Content};";

			if (address.StartsWith( "D" ))
			{
				if (int.TryParse( address.Substring( 1 ), out int add ))
				{
					if (add < 4096 && (add + length > 4096))
					{
						// 跨区域读取了，这时候要进行数据切割
						ushort len1 = (ushort)(4096 - add);
						ushort len2 = (ushort)(length - len1);
						OperateResult<byte[]> read1 = await readFunc( address, len1 );
						if (!read1.IsSuccess) return read1;

						OperateResult<byte[]> read2 = await readFunc( station + "D4096", len2 );
						if (!read2.IsSuccess) return read2;

						return OperateResult.CreateSuccessResult( SoftBasic.SpliceArray( read1.Content, read2.Content ) );
					}
				}
			}
			return await readFunc( address, length );
		}


		/// <inheritdoc cref="Write(Func{string, byte[], OperateResult}, string, byte[])"/>
		public static async Task<OperateResult> WriteAsync( Func<string, byte[], Task<OperateResult>> writeFunc, string address, byte[] value )
		{
			string station = string.Empty;
			OperateResult<int> stationPara = HslHelper.ExtractParameter( ref address, "s" );
			if (stationPara.IsSuccess) station = $"s={stationPara.Content};";

			if (address.StartsWith( "D" ))
			{
				if (int.TryParse( address.Substring( 1 ), out int add ))
				{
					if (add < 4096 && (add + (value.Length / 2)) > 4096)
					{
						// 跨区域写入了，这时候要进行数据切割
						ushort len1 = (ushort)(4096 - add);
						OperateResult write1 = await writeFunc( address, value.SelectBegin( len1 * 2 ) );
						if (!write1.IsSuccess) return write1;

						OperateResult write2 = await writeFunc( station + "D4096", value.RemoveBegin( len1 * 2 ) );
						if (!write2.IsSuccess) return write2;

						return OperateResult.CreateSuccessResult( );
					}
				}
			}
			return await writeFunc( address, value );
		}
#endif
	}
}
