using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HslCommunication.Profinet.Inovance
{
	/// <summary>
	/// 汇川的系列枚举信息
	/// </summary>
	public enum InovanceSeries
	{
		/// <summary>
		/// 适用于AM400、 AM400_800、 AC800 等系列
		/// </summary>
		AM,

		/// <summary>
		/// 适用于H3U, XP 等系列
		/// </summary>
		H3U,

		/// <summary>
		/// 适用于H5U 系列
		/// </summary>
		H5U,
	}
}
