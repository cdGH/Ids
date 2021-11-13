using HslCommunication.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HslCommunication.Profinet.Melsec.Helper
{
	/// <summary>
	/// 三菱的A3C协议类接口对象，具有站号，是否和校验的属性<br />
	/// Mitsubishi's A3C protocol interface object, which has the attributes of station number, and checksum
	/// </summary>
	public interface IReadWriteA3C : IReadWriteDevice
	{
		/// <summary>
		/// 当前A3C协议的站编号信息<br />
		/// Station number information of the current A3C protocol
		/// </summary>
		byte Station { get; set; }

		/// <summary>
		/// 当前的A3C协议是否使用和校验，默认使用<br />
		/// Whether the current A3C protocol uses sum check, it is used by default
		/// </summary>
		bool SumCheck { get; set; }

		/// <summary>
		/// 当前的A3C协议的格式信息，可选格式1，2，3，4，默认格式1<br />
		/// Format information of the current A3C protocol, optional format 1, 2, 3, 4, default format 1
		/// </summary>
		int Format { get; set; }
	}
}
