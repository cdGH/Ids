using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HslCommunication.CNC.Fanuc
{
	/// <summary>
	/// 系统状态信息
	/// </summary>
	public class SysStatusInfo
	{
		/// <summary>
		/// dummy
		/// </summary>
		public short Dummy { get; set; }

		/// <summary>
		/// T/M mode
		/// </summary>
		public short TMMode { get; set; }

		/// <summary>
		/// selected automatic mode
		/// </summary>
		public CNCWorkMode WorkMode { get; set; }

		/// <summary>
		/// running status
		/// </summary>
		public CNCRunStatus RunStatus { get; set; }

		/// <summary>
		/// axis, dwell status
		/// </summary>
		public short Motion { get; set; }

		/// <summary>
		/// m, s, t, b status
		/// </summary>
		public short MSTB { get; set; }

		/// <summary>
		/// emergency stop status，为1就是急停，为0就是正常
		/// </summary>
		public short Emergency { get; set; }

		/// <summary>
		/// alarm status
		/// </summary>
		public short Alarm { get; set; }

		/// <summary>
		/// editting status
		/// </summary>
		public short Edit { get; set; }

		/// <inheritdoc/>
		public override string ToString( )
		{
			return $"Dummy: {Dummy}, TMMode:{TMMode}, WorkMode:{WorkMode}, RunStatus:{RunStatus}, " +
				$"Motion:{Motion}, MSTB:{MSTB}, Emergency:{Emergency}, Alarm:{Alarm}, Edit:{Edit}";
		}
	}
}
