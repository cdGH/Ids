using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HslCommunication.Serial;
using HslCommunication.Core;
using HslCommunication.Reflection;
#if !NET35 && !NET20
using System.Threading.Tasks;
#endif

namespace HslCommunication.Profinet.Freedom
{
	/// <summary>
	/// 基于串口的自由协议，需要在地址里传入报文信息，也可以传入数据偏移信息，<see cref="SerialDeviceBase.ByteTransform"/>默认为<see cref="RegularByteTransform"/>
	/// </summary>
	/// <example>
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Profinet\FreedomExample.cs" region="Sample5" title="实例化" />
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Profinet\FreedomExample.cs" region="Sample6" title="读取" />
	/// </example>
	public class FreedomSerial : SerialDeviceBase
	{
		#region Constrcutor

		/// <summary>
		/// 实例化一个默认的对象
		/// </summary>
		public FreedomSerial( )
		{
			this.ByteTransform = new RegularByteTransform( );
		}

		#endregion

		#region Read Write

		/// <inheritdoc/>
		[HslMqttApi( "ReadByteArray", "特殊的地址格式，需要采用解析包起始地址的报文，例如 modbus 协议为 stx=9;00 00 00 00 00 06 01 03 00 64 00 01" )]
		public override OperateResult<byte[]> Read( string address, ushort length )
		{
			OperateResult<byte[], int> analysis = FreedomTcpNet.AnalysisAddress( address );
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

		#region Object Override

		/// <inheritdoc/>
		public override string ToString( ) => $"FreedomSerial<{ByteTransform.GetType( )}>[{PortName}:{BaudRate}]";

		#endregion
	}
}
