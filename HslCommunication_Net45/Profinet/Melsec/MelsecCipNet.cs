using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HslCommunication.Profinet.AllenBradley;
using HslCommunication;
using HslCommunication.Reflection;
using HslCommunication.Core;

#if !NET35 && !NET20
using System.Threading.Tasks;
#endif

namespace HslCommunication.Profinet.Melsec
{
	/// <summary>
	/// 三菱PLC的EIP协议的实现，当PLC使用了 QJ71EIP71 模块时就需要使用本类来访问
	/// </summary>
	public class MelsecCipNet : AllenBradleyNet
	{
		#region Constructor

		/// <inheritdoc cref="AllenBradleyNet.AllenBradleyNet()"/>
		public MelsecCipNet( ) : base( ) { }

		/// <inheritdoc cref="AllenBradleyNet.AllenBradleyNet(string, int)"/>
		public MelsecCipNet( string ipAddress, int port = 44818 ) : base( ipAddress, port ) { }

		#endregion

		/// <summary>
		/// Read data information, data length for read array length information
		/// </summary>
		/// <param name="address">Address format of the node</param>
		/// <param name="length">In the case of arrays, the length of the array </param>
		/// <returns>Result data with result object </returns>
		[HslMqttApi( "ReadByteArray", "" )]
		public override OperateResult<byte[]> Read( string address, ushort length )
		{
			return Read( new string[] { address }, new int[] { length } );
		}

#if !NET35 && !NET20
		/// <inheritdoc cref="Read(string, ushort)"/>
		public override async Task<OperateResult<byte[]>> ReadAsync( string address, ushort length )
		{
			return await ReadAsync( new string[] { address }, new int[] { length } );
		}
#endif
	}
}
