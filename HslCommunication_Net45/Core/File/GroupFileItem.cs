using HslCommunication.BasicFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HslCommunication.Core
{
	/// <summary>
	/// 单个文件的存储信息，用来表示在文件目录管理器里表示所有的文件信息的<br />
	/// Storage information of a single file, used to represent all file information in the file directory manager
	/// </summary>
	public class GroupFileItem
	{
		#region Public Members

		/// <summary>
		/// 文件的名称
		/// </summary>
		public string FileName { get; set; }

		/// <summary>
		/// 文件的大小
		/// </summary>
		public long FileSize { get; set; }

		/// <summary>
		/// 文件的映射名称
		/// </summary>
		public string MappingName { get; set; }

		/// <summary>
		/// 文件的下载次数
		/// </summary>
		public long DownloadTimes { get; set; }

		/// <summary>
		/// 文件的上传时间
		/// </summary>
		public DateTime UploadTime { get; set; }

		/// <summary>
		/// 文件的上传人，拥有者
		/// </summary>
		public string Owner { get; set; }

		/// <summary>
		/// 文件的额外描述
		/// </summary>
		public string Description { get; set; }

		#endregion

		#region Get Size Text

		/// <summary>
		/// 获取文件的大小描述<br />
		/// Get file size description
		/// </summary>
		/// <returns>文件大小的字符串描述形式</returns>
		public string GetTextFromFileSize( ) => SoftBasic.GetSizeDescription( FileSize );

		#endregion

		#region Object Override

		/// <inheritdoc/>
		public override string ToString( ) => $"GroupFileItem[{FileName}:{MappingName}]";

		#endregion
	}
}
