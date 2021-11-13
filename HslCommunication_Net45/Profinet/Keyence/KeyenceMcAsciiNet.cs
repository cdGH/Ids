using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HslCommunication.Core.Address;
using HslCommunication.Profinet.Melsec;


namespace HslCommunication.Profinet.Keyence
{
	/// <summary>
	/// 基恩士PLC的数据通信类，使用QnA兼容3E帧的通信协议实现，使用ASCII的格式，地址格式需要进行转换成三菱的格式，详细参照备注说明<br />
	/// Keyence PLC's data communication class is implemented using QnA compatible 3E frame communication protocol. 
	/// It uses ascii format. The address format needs to be converted to Mitsubishi format.
	/// </summary>
	/// <remarks>
	/// <inheritdoc cref="KeyenceMcNet" path="remarks"/>
	/// </remarks>
	public class KeyenceMcAsciiNet : MelsecMcAsciiNet
	{
		#region Constructor

		/// <inheritdoc cref="KeyenceMcNet()"/>
		public KeyenceMcAsciiNet() : base( ) { }

		/// <inheritdoc cref="KeyenceMcNet(string, int)"/>
		public KeyenceMcAsciiNet( string ipAddress, int port ) : base( ipAddress, port ) { }

		#endregion

		#region Address Overeride

		/// <inheritdoc/>
		public override OperateResult<McAddressData> McAnalysisAddress( string address, ushort length ) => McAddressData.ParseKeyenceFrom( address, length );

		#endregion

		#region Object Override

		/// <inheritdoc/>
		public override string ToString( ) => $"KeyenceMcAsciiNet[{IpAddress}:{Port}]";

		#endregion
	}
}
