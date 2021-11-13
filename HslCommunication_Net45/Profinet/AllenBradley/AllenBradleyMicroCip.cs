using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
#if !NET35 && !NET20
using System.Threading.Tasks;
#endif

namespace HslCommunication.Profinet.AllenBradley
{
	/// <summary>
	/// AB PLC的cip通信实现类，适用Micro800系列控制系统<br />
	/// AB PLC's cip communication implementation class, suitable for Micro800 series control system
	/// </summary>
	public class AllenBradleyMicroCip : AllenBradleyNet
	{
		#region Constructor

		/// <inheritdoc cref="AllenBradleyNet()"/>
		public AllenBradleyMicroCip( ) : base( ) { }

		/// <inheritdoc cref="AllenBradleyNet(string, int)"/>
		public AllenBradleyMicroCip( string ipAddress, int port = 44818 ) : base( ipAddress, port ) { }

		#endregion

		/// <inheritdoc/>
		protected override byte[] PackCommandService( byte[] portSlot, params byte[][] cips ) => AllenBradleyHelper.PackCleanCommandService( portSlot, cips );

		/// <inheritdoc/>
		public override string ToString( ) => $"AllenBradleyMicroCip[{IpAddress}:{Port}]";
	}
}
