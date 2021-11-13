using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HslCommunication.Profinet.AllenBradley
{
	/// <summary>
	/// AB PLC的数据
	/// </summary>
	public class AllenBradleyItemValue
	{
		/// <summary>
		/// 真实的数组缓存
		/// </summary>
		public byte[] Buffer { get; set; }

		/// <summary>
		/// 是否是数组的数据
		/// </summary>
		public bool IsArray { get; set; }

		/// <summary>
		/// 单个单位的数据长度信息
		/// </summary>
		public int TypeLength { get; set; } = 1;
	}
}
