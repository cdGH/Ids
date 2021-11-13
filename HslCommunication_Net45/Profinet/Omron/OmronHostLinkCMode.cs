using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HslCommunication.BasicFramework;
using HslCommunication.Core;
using HslCommunication.Serial;
using HslCommunication.Reflection;

namespace HslCommunication.Profinet.Omron
{
	/// <summary>
	/// 欧姆龙的HostLink的C-Mode实现形式，地址支持携带站号信息，例如：s=2;D100<br />
	/// Omron's HostLink C-Mode implementation form, the address supports carrying station number information, for example: s=2;D100
	/// </summary>
	/// <remarks>
	/// 暂时只支持的字数据的读写操作，不支持位的读写操作。另外本模式下，程序要在监视模式运行才能写数据，欧姆龙官方回复的。
	/// </remarks>
	public class OmronHostLinkCMode : SerialDeviceBase
	{
		#region Constructor

		/// <inheritdoc cref="OmronFinsNet()"/>
		public OmronHostLinkCMode( )
		{
			this.ByteTransform = new ReverseWordTransform( );
			this.WordLength = 1;
			this.ByteTransform.DataFormat = DataFormat.CDAB;
			this.ByteTransform.IsStringReverseByteWord = true;
		}

		#endregion

		#region Public Member

		/// <inheritdoc cref="OmronHostLinkOverTcp.UnitNumber"/>
		public byte UnitNumber { get; set; }

		#endregion

		#region Read Write Support

		/// <inheritdoc cref="OmronFinsNet.Read(string, ushort)"/>
		[HslMqttApi( "ReadByteArray", "" )]
		public override OperateResult<byte[]> Read( string address, ushort length )
		{
			byte station = (byte)HslHelper.ExtractParameter( ref address, "s", this.UnitNumber );

			// 解析地址
			var command = BuildReadCommand( address, length, false );
			if (!command.IsSuccess) return command;

			// 核心交互
			OperateResult<byte[]> read = ReadFromCoreServer( PackCommand( command.Content, station ) );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( read );

			// 数据有效性分析
			OperateResult<byte[]> valid = ResponseValidAnalysis( read.Content, true );
			if (!valid.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( valid );

			// 读取到了正确的数据
			return OperateResult.CreateSuccessResult( valid.Content );
		}

		/// <inheritdoc cref="OmronFinsNet.Write(string, byte[])"/>
		[HslMqttApi( "WriteByteArray", "" )]
		public override OperateResult Write( string address, byte[] value )
		{
			byte station = (byte)HslHelper.ExtractParameter( ref address, "s", this.UnitNumber );

			// 获取指令
			var command = BuildWriteWordCommand( address, value ); ;
			if (!command.IsSuccess) return command;

			// 核心数据交互
			OperateResult<byte[]> read = ReadFromCoreServer( PackCommand( command.Content, station ) );
			if (!read.IsSuccess) return read;

			// 数据有效性分析
			OperateResult<byte[]> valid = ResponseValidAnalysis( read.Content, false );
			if (!valid.IsSuccess) return valid;

			// 成功
			return OperateResult.CreateSuccessResult( );
		}

		#endregion

		#region Bool Read Write


		#endregion

		#region Public Method

		/// <summary>
		/// 读取PLC的当前的型号信息
		/// </summary>
		/// <returns>型号</returns>
		[HslMqttApi]
		public OperateResult<string> ReadPlcModel( )
		{
			// 核心数据交互
			OperateResult<byte[]> read = ReadFromCoreServer( PackCommand( Encoding.ASCII.GetBytes( "MM" ), UnitNumber ) );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<string>( read );

			// 数据有效性分析
			int err = Convert.ToInt32( Encoding.ASCII.GetString( read.Content, 5, 2 ), 16 );
			if (err > 0) return new OperateResult<string>( err, "Unknown Error" );

			// 成功
			string model = Encoding.ASCII.GetString( read.Content, 7, 2 );
			return GetModelText( model );
		}

		#endregion

		#region Object Override

		/// <inheritdoc/>
		public override string ToString( ) => $"OmronHostLinkCMode[{PortName}:{BaudRate}]";

		#endregion

		#region Build Command

		/// <summary>
		/// 解析欧姆龙的数据地址，参考来源是Omron手册第188页，比如D100， E1.100<br />
		/// Analyze Omron's data address, the reference source is page 188 of the Omron manual, such as D100, E1.100
		/// </summary>
		/// <param name="address">数据地址</param>
		/// <param name="isBit">是否是位地址</param>
		/// <param name="isRead">是否读取</param>
		/// <returns>解析后的结果地址对象</returns>
		public static OperateResult<string, string> AnalysisAddress( string address, bool isBit, bool isRead )
		{
			var result = new OperateResult<string, string>( );
			try
			{
				switch (address[0])
				{
					case 'D':
					case 'd':
						{
							// DM区数据
							result.Content1 = isRead ? "RD" : "WD";
							break;
						}
					case 'C':
					case 'c':
						{
							// CIO区数据
							result.Content1 = isRead ? "RR" : "WR";
							break;
						}
					case 'H':
					case 'h':
						{
							// HR区
							result.Content1 = isRead ? "RH" : "WH";
							break;
						}
					case 'A':
					case 'a':
						{
							// AR区
							result.Content1 = isRead ? "RJ" : "WJ";
							break;
						}
					case 'E':
					case 'e':
						{
							// E区，比较复杂，需要专门的计算
							string[] splits = address.Split( new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries );
							int block = Convert.ToInt32( splits[0].Substring( 1 ), 16 );
							result.Content1 = (isRead ? "RE" : "WE") + Encoding.ASCII.GetString( SoftBasic.BuildAsciiBytesFrom( (byte)block ) );
							break;
						}
					default: throw new Exception( StringResources.Language.NotSupportedDataType );
				}

				if (address[0] == 'E' || address[0] == 'e')
				{
					string[] splits = address.Split( new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries );
					if (isBit)
					{
						// 位操作
						//ushort addr = ushort.Parse( splits[1] );
						//result.Content2 = new byte[3];
						//result.Content2[0] = BitConverter.GetBytes( addr )[1];
						//result.Content2[1] = BitConverter.GetBytes( addr )[0];

						//if (splits.Length > 2)
						//{
						//	result.Content2[2] = byte.Parse( splits[2] );
						//	if (result.Content2[2] > 15)
						//	{
						//		throw new Exception( StringResources.Language.OmronAddressMustBeZeroToFiveteen );
						//	}
						//}
					}
					else
					{
						// 字操作
						ushort addr = ushort.Parse( splits[1] );
						result.Content2 = addr.ToString( "D4" );
					}
				}
				else
				{
					if (isBit)
					{
						// 位操作
						//string[] splits = address.Substring( 1 ).Split( new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries );
						//ushort addr = ushort.Parse( splits[0] );
						//result.Content2 = new byte[3];
						//result.Content2[0] = BitConverter.GetBytes( addr )[1];
						//result.Content2[1] = BitConverter.GetBytes( addr )[0];

						//if (splits.Length > 1)
						//{
						//	result.Content2[2] = byte.Parse( splits[1] );
						//	if (result.Content2[2] > 15)
						//	{
						//		throw new Exception( StringResources.Language.OmronAddressMustBeZeroToFiveteen );
						//	}
						//}
					}
					else
					{
						// 字操作
						ushort addr = ushort.Parse( address.Substring( 1 ) );
						result.Content2 = addr.ToString( "D4" );
					}
				}
			}
			catch (Exception ex)
			{
				result.Message = ex.Message;
				return result;
			}

			result.IsSuccess = true;
			return result;
		}

		/// <summary>
		/// 根据读取的地址，长度，是否位读取创建Fins协议的核心报文<br />
		/// According to the read address, length, whether to read the core message that creates the Fins protocol
		/// </summary>
		/// <param name="address">地址，具体格式请参照示例说明</param>
		/// <param name="length">读取的数据长度</param>
		/// <param name="isBit">是否使用位读取</param>
		/// <returns>带有成功标识的Fins核心报文</returns>
		public static OperateResult<byte[]> BuildReadCommand( string address, ushort length, bool isBit )
		{
			var analysis = AnalysisAddress( address, isBit, true );
			if (!analysis.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( analysis );

			StringBuilder sb = new StringBuilder( );
			sb.Append( analysis.Content1 );
			sb.Append( analysis.Content2 );
			sb.Append( length.ToString( "D4" ) );

			return OperateResult.CreateSuccessResult( Encoding.ASCII.GetBytes( sb.ToString( ) ) );
		}

		/// <summary>
		/// 根据读取的地址，长度，是否位读取创建Fins协议的核心报文<br />
		/// According to the read address, length, whether to read the core message that creates the Fins protocol
		/// </summary>
		/// <param name="address">地址，具体格式请参照示例说明</param>
		/// <param name="value">等待写入的数据</param>
		/// <returns>带有成功标识的Fins核心报文</returns>
		public static OperateResult<byte[]> BuildWriteWordCommand( string address, byte[] value )
		{
			var analysis = AnalysisAddress( address, false, false );
			if (!analysis.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( analysis );

			StringBuilder sb = new StringBuilder( );
			sb.Append( analysis.Content1 );
			sb.Append( analysis.Content2 );
			for (int i = 0; i < value.Length / 2; i++)
			{
				sb.Append( BitConverter.ToUInt16( value, i * 2 ).ToString( "X4" ) );
			}

			return OperateResult.CreateSuccessResult( Encoding.ASCII.GetBytes( sb.ToString( ) ) );
		}


		/// <summary>
		/// 验证欧姆龙的Fins-TCP返回的数据是否正确的数据，如果正确的话，并返回所有的数据内容
		/// </summary>
		/// <param name="response">来自欧姆龙返回的数据内容</param>
		/// <param name="isRead">是否读取</param>
		/// <returns>带有是否成功的结果对象</returns>
		public static OperateResult<byte[]> ResponseValidAnalysis( byte[] response, bool isRead )
		{
			// 数据有效性分析
			if (response.Length >= 11)
			{
				// 提取错误码
				int err = Convert.ToInt32( Encoding.ASCII.GetString( response, 5, 2 ), 16 );
				byte[] Content = null;

				if (response.Length > 11)
				{
					byte[] buffer = new byte[(response.Length - 11) / 2];
					for (int i = 0; i < buffer.Length / 2; i++)
					{
						BitConverter.GetBytes( Convert.ToUInt16( Encoding.ASCII.GetString( response, 7 + 4 * i, 4 ), 16 ) ).CopyTo( buffer, i * 2 );
					}
					Content = buffer;
				}

				if (err > 0) return new OperateResult<byte[]>( )
				{
					ErrorCode = err,
					Content = Content
				};
				else
				{
					return OperateResult.CreateSuccessResult( Content );
				}
			}

			return new OperateResult<byte[]>( StringResources.Language.OmronReceiveDataError );
		}

		/// <summary>
		/// 将普通的指令打包成完整的指令
		/// </summary>
		/// <param name="cmd">fins指令</param>
		/// <param name="unitNumber">站号信息</param>
		/// <returns>完整的质量</returns>
		public static byte[] PackCommand( byte[] cmd, byte unitNumber )
		{
			byte[] buffer = new byte[7 + cmd.Length];

			buffer[0] = (byte)'@';
			buffer[1] = SoftBasic.BuildAsciiBytesFrom( unitNumber )[0];
			buffer[2] = SoftBasic.BuildAsciiBytesFrom( unitNumber )[1];
			buffer[buffer.Length - 2] = (byte)'*';
			buffer[buffer.Length - 1] = 0x0D;
			cmd.CopyTo( buffer, 3 );
			// 计算FCS
			int tmp = buffer[0];
			for (int i = 1; i < buffer.Length - 4; i++)
			{
				tmp = (tmp ^ buffer[i]);
			}
			buffer[buffer.Length - 4] = SoftBasic.BuildAsciiBytesFrom( (byte)tmp )[0];
			buffer[buffer.Length - 3] = SoftBasic.BuildAsciiBytesFrom( (byte)tmp )[1];
			string output = Encoding.ASCII.GetString( buffer );
			Console.WriteLine( output );
			return buffer;
		}

		/// <summary>
		/// 获取model的字符串描述信息
		/// </summary>
		/// <param name="model">型号代码</param>
		/// <returns>是否解析成功</returns>
		public static OperateResult<string> GetModelText( string model )
		{
			switch (model)
			{
				case "30": return OperateResult.CreateSuccessResult( "CS/CJ" );
				case "01": return OperateResult.CreateSuccessResult( "C250" );
				case "02": return OperateResult.CreateSuccessResult( "C500" );
				case "03": return OperateResult.CreateSuccessResult( "C120/C50" );
				case "09": return OperateResult.CreateSuccessResult( "C250F" );
				case "0A": return OperateResult.CreateSuccessResult( "C500F" );
				case "0B": return OperateResult.CreateSuccessResult( "C120F" );
				case "0E": return OperateResult.CreateSuccessResult( "C2000" );
				case "10": return OperateResult.CreateSuccessResult( "C1000H" );
				case "11": return OperateResult.CreateSuccessResult( "C2000H/CQM1/CPM1" );
				case "12": return OperateResult.CreateSuccessResult( "C20H/C28H/C40H, C200H, C200HS, C200HX/HG/HE (-ZE)" );
				case "20": return OperateResult.CreateSuccessResult( "CV500" );
				case "21": return OperateResult.CreateSuccessResult( "CV1000" );
				case "22": return OperateResult.CreateSuccessResult( "CV2000" );
				case "40": return OperateResult.CreateSuccessResult( "CVM1-CPU01-E" );
				case "41": return OperateResult.CreateSuccessResult( "CVM1-CPU11-E" );
				case "42": return OperateResult.CreateSuccessResult( "CVM1-CPU21-E" );
				default: return new OperateResult<string>( "Unknown model, model code:" + model );
			}
		}
		#endregion
	}
}
