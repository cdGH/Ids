using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HslCommunication.Profinet.Melsec.Helper
{
	/// <summary>
	/// MC协议的类型
	/// </summary>
	public enum McType
	{
		/// <summary>
		/// 基于二进制的MC协议
		/// </summary>
		McBinary,
		/// <summary>
		/// 基于ASCII格式的MC协议
		/// </summary>
		MCAscii,
		/// <summary>
		/// 基于R系列的二进制的MC协议
		/// </summary>
		McRBinary,
		/// <summary>
		/// 基于R系列的ASCII格式的MC协议
		/// </summary>
		McRAscii,
	}
}
