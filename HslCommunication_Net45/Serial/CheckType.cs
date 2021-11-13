using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HslCommunication.Serial
{
	/// <summary>
	/// 校验方式
	/// </summary>
	public enum CheckType
	{
		/// <summary>
		/// 和校验
		/// </summary>
		BCC,

		/// <summary>
		/// CRC校验的方式
		/// </summary>
		CRC16,
	}
}
