using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HslCommunication.Core
{
	/// <summary>
	/// 文件的基础信息
	/// </summary>
	public class FileBaseInfo
	{
		/// <summary>
		/// 文件名称
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// 文件大小
		/// </summary>
		public long Size { get; set; }

		/// <summary>
		/// 文件的标识，注释
		/// </summary>
		public string Tag { get; set; }

		/// <summary>
		/// 文件上传人的名称
		/// </summary>
		public string Upload { get; set; }
	}

	/// <summary>
	/// 文件的分类信息
	/// </summary>
	public class FileGroupInfo
	{
		/// <summary>
		/// 命令码
		/// </summary>
		public int Command { get; set; }

		/// <summary>
		/// 文件名
		/// </summary>
		public string FileName { get; set; }
		
		/// <summary>
		/// 文件名列表
		/// </summary>
		public string[] FileNames { get; set; }

		/// <summary>
		/// 第一级分类信息
		/// </summary>
		public string Factory { get; set; }

		/// <summary>
		/// 第二级分类信息
		/// </summary>
		public string Group { get; set; }

		/// <summary>
		/// 第三级分类信息
		/// </summary>
		public string Identify { get; set; }
	}

	/// <summary>
	/// 文件在服务器上的信息
	/// </summary>
	public class FileServerInfo : FileBaseInfo
	{
		/// <summary>
		/// 文件的真实路径
		/// </summary>
		public string ActualFileFullName { get; set; }
	}
}
