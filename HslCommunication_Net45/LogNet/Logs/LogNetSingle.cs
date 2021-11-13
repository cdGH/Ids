using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace HslCommunication.LogNet
{
	/// <summary>
	/// 单日志文件对象，所有的日志信息的记录都会写入一个文件里面去。文件名指定为空的时候，自动不存储文件。<br />
	/// Single log file object, all log information records will be written to a file. When the file name is specified as empty, the file is not stored automatically.
	/// </summary>
	/// <remarks>
	/// 此日志实例化需要指定一个完整的文件路径，当需要记录日志的时候调用方法，会使得日志越来越大，对于写入的性能没有太大影响，但是会影响文件读取。
	/// </remarks>
	/// <example>
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\LogNet\LogNetSingle.cs" region="Example1" title="单文件实例化" />
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\LogNet\LogNetSingle.cs" region="Example4" title="基本的使用" />
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\LogNet\LogNetSingle.cs" region="Example5" title="所有日志不存储" />
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\LogNet\LogNetSingle.cs" region="Example6" title="仅存储ERROR等级" />
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\LogNet\LogNetSingle.cs" region="Example7" title="不指定路径" />
	/// </example>
	public class LogNetSingle : LogNetBase, ILogNet
	{
		#region Constructor

		/// <summary>
		/// 实例化一个单文件日志的对象，如果日志的路径为空，那么就不存储数据，只触发<see cref="LogNetBase.BeforeSaveToFile"/>事件<br />
		/// Instantiate a single file log object. If the log path is empty, then no data is stored and only the <see cref="LogNetBase.BeforeSaveToFile"/> event is triggered.
		/// </summary>
		/// <param name="filePath">文件的路径</param>
		public LogNetSingle( string filePath )
		{
			fileName              = filePath;
			LogSaveMode           = LogSaveMode.SingleFile;
			if (!string.IsNullOrEmpty( fileName ))
			{
				FileInfo fileInfo = new FileInfo( filePath );
				if (!Directory.Exists( fileInfo.DirectoryName ))
				{
					Directory.CreateDirectory( fileInfo.DirectoryName );
				}
			}
		}

		#endregion

		#region Public Method

		/// <summary>
		/// 单日志文件允许清空日志内容<br />
		/// Single log file allows clearing log contents
		/// </summary>
		public void ClearLog( )
		{
			m_fileSaveLock.Enter( );
			try
			{
				if (!string.IsNullOrEmpty( fileName ))
				{
					File.Create( fileName ).Dispose( );
				}
			}
			catch
			{
				throw;
			}
			finally
			{
				m_fileSaveLock.Leave( );
			}
		}

		/// <summary>
		/// 获取单日志文件的所有保存记录<br />
		/// Get all saved records of a single log file
		/// </summary>
		/// <returns>字符串信息</returns>
		public string GetAllSavedLog( )
		{
			string result = string.Empty;
			m_fileSaveLock.Enter( );
			try
			{
				if (!string.IsNullOrEmpty( fileName ))
				{
					if (File.Exists( fileName ))
					{
						StreamReader stream = new StreamReader( fileName, Encoding.UTF8 );
						result = stream.ReadToEnd( );
						stream.Dispose( );
					}
				}
			}
			catch
			{
				throw;
			}
			finally
			{
				m_fileSaveLock.Leave( );
			}
			return result;
		}

		/// <summary>
		/// 获取所有的日志文件数组，对于单日志文件来说就只有一个<br />
		/// Get all log file arrays, only one for a single log file
		/// </summary>
		/// <returns>字符串数组，包含了所有的存在的日志数据</returns>
		public string[] GetExistLogFileNames( ) => new string[] { fileName };

		#endregion

		#region LogNetBase Override

		/// <inheritdoc/>
		protected override string GetFileSaveName( ) => fileName;

		#endregion

		#region Private Member

		private readonly string fileName = string.Empty;                               // 文件的保存目录

		#endregion

		#region Object Override

		/// <inheritdoc/>
		public override string ToString( ) => $"LogNetSingle[{fileName}]";

		#endregion
	}
}
