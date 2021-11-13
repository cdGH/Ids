using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HslCommunication.CNC.Fanuc
{
	/// <summary>
	/// CNC的运行状态
	/// </summary>
	public enum CNCRunStatus
	{
		/// <summary>
		/// 重置
		/// </summary>
		RESET = 0,
		/// <summary>
		/// 停止
		/// </summary>
		STOP = 1,
		/// <summary>
		/// 等待
		/// </summary>
		HOLD = 2,
		/// <summary>
		/// 启动
		/// </summary>
		START = 3,
		/// <summary>
		/// MSTR
		/// </summary>
		MSTR = 4,
	}
}
