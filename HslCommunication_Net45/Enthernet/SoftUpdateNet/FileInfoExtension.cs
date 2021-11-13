using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace HslCommunication.Enthernet
{
	/// <summary>
	/// 文件的扩展信息
	/// </summary>
	public class FileInfoExtension
	{
		/// <summary>
		/// 文件的完整名称
		/// </summary>
		public string FullName { get; set; }

		/// <summary>
		/// 文件的修改时间
		/// </summary>
		public DateTime ModifiTime { get; set; }

		/// <summary>
		/// 文件的MD5码
		/// </summary>
		public string MD5 { get; set; }
	}
}
