using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace HslCommunication.LogNet
{
	/// <summary>
	/// 基于路径实现的日志类的基类，提供几个基础的方法信息。<br />
	/// The base class of the log class implemented based on the path provides several basic method information.
	/// </summary>
	public abstract class LogPathBase : LogNetBase
	{
		#region LogNetBase Override

		/// <inheritdoc/>
		protected override void OnWriteCompleted( )
		{
			// 如果配置最大的文件数量，就进行删除操作，需要注意的是提高删除的速度
			if (controlFileQuantity > 1)
			{
				try
				{
					string[] files = GetExistLogFileNames( );
					if (files.Length > controlFileQuantity)
					{
						List<FileInfo> fileInfos = new List<FileInfo>( );
						for (int i = 0; i < files.Length; i++)
						{
							fileInfos.Add( new FileInfo( files[i] ) );
						}

						fileInfos.Sort( new Comparison<FileInfo>( ( m, n ) =>
						{
							return m.CreationTime.CompareTo( n.CreationTime );
						} ) );


						for (int i = 0; i < fileInfos.Count - controlFileQuantity; i++)
						{
							File.Delete( fileInfos[i].FullName );
						}
					}
				}
				catch
				{

				}
			}
		}

		#endregion

		#region Public Method

		/// <summary>
		/// 返回所有的日志文件名称，返回一个列表<br />
		/// Returns all log file names, returns a list
		/// </summary>
		/// <returns>所有的日志文件信息</returns>
		public string[] GetExistLogFileNames( )
		{
			if (!string.IsNullOrEmpty( filePath ))
				return Directory.GetFiles( filePath, LogNetManagment.LogFileHeadString + "*.txt" );
			else
				return new string[] { };
		}

		#endregion

		#region Protect Member

		/// <summary>
		/// 当前正在存储的文件名<br />
		/// File name currently being stored
		/// </summary>
		protected string fileName = string.Empty;

		/// <summary>
		/// 存储文件的路径，如果设置为空，就不进行存储。<br />
		/// The path for storing the file. If it is set to empty, it will not be stored.
		/// </summary>
		protected string filePath = string.Empty;

		/// <summary>
		/// 控制文件的数量，小于1则不进行任何操作，当设置为10的时候，就限制文件数量为10。<br />
		/// Control the number of files. If it is less than 1, no operation is performed. When it is set to 10, the number of files is limited to 10.
		/// </summary>
		protected int controlFileQuantity = -1;

		#endregion

		#region Object Override

		/// <inheritdoc/>
		public override string ToString( ) => $"LogPathBase";

		#endregion
	}
}
