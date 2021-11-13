using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HslCommunication.CNC.Fanuc
{
	/// <summary>
	/// 当前机床的报警信息
	/// </summary>
	public class SysAlarm
	{
		/// <summary>
		/// 当前报警的ID信息
		/// </summary>
		public int AlarmId { get; set; }

		/// <summary>
		/// 当前的报警类型
		/// </summary>
		public short Type { get; set; }

		/// <summary>
		/// 报警的轴信息
		/// </summary>
		public short Axis { get; set; }

		/// <summary>
		/// 报警的消息
		/// </summary>
		public string Message { get; set; }
	}
}
