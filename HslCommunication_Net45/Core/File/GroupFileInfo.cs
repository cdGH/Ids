using HslCommunication.BasicFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HslCommunication.Core
{
	/// <summary>
	/// 文件服务器的目录管理类的基本信息
	/// </summary>
	public class GroupFileInfo
	{
		/// <summary>
		/// 文件目录的名称信息
		/// </summary>
		public string PathName { get; set; }

		/// <summary>
		/// 获取或设置文件的总大小
		/// </summary>
		public long FileTotalSize { get; set; }

		/// <summary>
		/// 获取或设置文件的总数量
		/// </summary>
		public int FileCount { get; set; }

		/// <summary>
		/// 获取或设置最后一次文件更新的时间，如果不存在文件，则为理论最小值
		/// </summary>
		public DateTime LastModifyTime { get; set; }

		/// <inheritdoc/>
		public override string ToString( )
		{
			return $"Count: {FileCount} TotalSize: {FileTotalSize} [{SoftBasic.GetSizeDescription( FileTotalSize ) }] ModifyTime:{LastModifyTime:yyyy-MM-dd HH:mm:ss}";
		}
	}
}
