using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HslCommunication.Core.Net;
using HslCommunication.Core.IMessage;
using HslCommunication.Core;
using HslCommunication;
using HslCommunication.Reflection;
#if !NET35 && !NET20
using System.Threading.Tasks;
#endif

namespace HslCommunication.Profinet.Freedom
{
	/// <summary>
	/// 基于TCP/IP协议的自由协议，需要在地址里传入报文信息，也可以传入数据偏移信息，<see cref="NetworkDoubleBase.ByteTransform"/>默认为<see cref="RegularByteTransform"/>
	/// </summary>
	/// <example>
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Profinet\FreedomExample.cs" region="Sample1" title="实例化" />
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Profinet\FreedomExample.cs" region="Sample2" title="连接及读取" />
	/// </example>
	public class FreedomTcpNet : NetworkDeviceBase
	{
		#region Constrcutor

		/// <summary>
		/// 实例化一个默认的对象
		/// </summary>
		public FreedomTcpNet( )
		{
			this.ByteTransform = new RegularByteTransform( );
		}

		/// <summary>
		/// 指定IP地址及端口号来实例化自由的TCP协议
		/// </summary>
		/// <param name="ipAddress">Ip地址</param>
		/// <param name="port">端口</param>
		public FreedomTcpNet(string ipAddress, int port )
		{
			this.IpAddress     = ipAddress;
			this.Port          = port;
			this.ByteTransform = new RegularByteTransform( );
		}

		#endregion

		#region Read Write

		/// <inheritdoc/>
		[HslMqttApi( "ReadByteArray", "特殊的地址格式，需要采用解析包起始地址的报文，例如 modbus 协议为 stx=9;00 00 00 00 00 06 01 03 00 64 00 01" )]
		public override OperateResult<byte[]> Read( string address, ushort length )
		{
			OperateResult<byte[], int> analysis = AnalysisAddress( address );
			if (!analysis.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( analysis );

			OperateResult<byte[]> read = ReadFromCoreServer( analysis.Content1 );
			if (!read.IsSuccess) return read;

			if (analysis.Content2 >= read.Content.Length) return new OperateResult<byte[]>( StringResources.Language.ReceiveDataLengthTooShort );
			return OperateResult.CreateSuccessResult( read.Content.RemoveBegin( analysis.Content2 ) );
		}

		/// <inheritdoc/>
		public override OperateResult Write( string address, byte[] value )
		{
			return Read( address, 0 );
		}

		#endregion

		#region Read Write Async
#if !NET35 && !NET20
		/// <inheritdoc/>
		public async override Task<OperateResult<byte[]>> ReadAsync( string address, ushort length )
		{
			OperateResult<byte[], int> analysis = AnalysisAddress( address );
			if (!analysis.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( analysis );

			OperateResult<byte[]> read = await ReadFromCoreServerAsync( analysis.Content1 );
			if (!read.IsSuccess) return read;

			if (analysis.Content2 >= read.Content.Length) return new OperateResult<byte[]>( StringResources.Language.ReceiveDataLengthTooShort );
			return OperateResult.CreateSuccessResult( read.Content.RemoveBegin( analysis.Content2 ) );
		}

		/// <inheritdoc/>
		public async override Task<OperateResult> WriteAsync( string address, byte[] value )
		{
			return await ReadAsync( address, 0 );
		}
#endif
		#endregion

		#region Object Override

		/// <inheritdoc/>
		public override string ToString( ) => $"FreedomTcpNet<{ByteTransform.GetType( )}>[{IpAddress}:{Port}]";

		#endregion

		#region Static Helper

		/// <summary>
		/// 分析地址的方法，会转换成一个数据报文和数据结果偏移的信息
		/// </summary>
		/// <param name="address">地址信息</param>
		/// <returns>报文结果内容</returns>
		public static OperateResult<byte[], int> AnalysisAddress(string address )
		{
			try
			{
				int index = 0;
				byte[] buffer = null;
				if (address.IndexOf( ';' ) > 0)
				{
					string[] splits = address.Split( new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries );
					for (int i = 0; i < splits.Length; i++)
					{
						if (splits[i].StartsWith( "stx=" ))
						{
							index = Convert.ToInt32( splits[i].Substring( 4 ) );
						}
						else
						{
							buffer = splits[i].ToHexBytes( );
						}
					}
				}
				else
				{
					buffer = address.ToHexBytes( );
				}

				return OperateResult.CreateSuccessResult( buffer, index );
			}
			catch(Exception ex)
			{
				return new OperateResult<byte[], int>( ex.Message );
			}
		}

		#endregion
	}
}
