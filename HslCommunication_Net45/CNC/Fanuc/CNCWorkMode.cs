using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HslCommunication.CNC.Fanuc
{
	/// <summary>
	/// 设备的工作模式
	/// </summary>
	public enum CNCWorkMode
	{
		/// <summary>
		/// 手动输入
		/// </summary>
		MDI = 0,
		/// <summary>
		/// 自动循环
		/// </summary>
		AUTO = 1,
		/// <summary>
		/// 程序编辑
		/// </summary>
		EDIT = 3,
		/// <summary>
		/// ×100
		/// </summary>
		HANDLE = 4,
		/// <summary>
		/// 连续进给
		/// </summary>
		JOG = 5,
		/// <summary>
		/// ???
		/// </summary>
		TeachInJOG = 6,
		/// <summary>
		/// 示教
		/// </summary>
		TeachInHandle = 7,
		/// <summary>
		/// ???
		/// </summary>
		INCfeed = 8,
		/// <summary>
		/// 机床回零
		/// </summary>
		REFerence = 9,
		/// <summary>
		/// ???
		/// </summary>
		ReMoTe = 10,
	}
}
