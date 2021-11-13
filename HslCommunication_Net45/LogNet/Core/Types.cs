using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace HslCommunication.LogNet
{
	#region Log EventArgs

	/// <summary>
	/// 带有日志消息的事件
	/// </summary>
	public class HslEventArgs : EventArgs
	{
		/// <summary>
		/// 消息信息
		/// </summary>
		public HslMessageItem HslMessage { get; set; }

	}

	/// <summary>
	/// 日志存储回调的异常信息
	/// </summary>
	public class LogNetException : Exception
	{
		/// <summary>
		/// 使用其他的异常信息来初始化日志异常
		/// </summary>
		/// <param name="innerException">异常信息</param>
		public LogNetException( Exception innerException ) : base( innerException.Message, innerException ) { }
	}

	#endregion

	#region MyRegion

	/// <summary>
	/// 日志文件的存储模式
	/// </summary>
	public enum LogSaveMode
	{
		/// <summary>
		/// 单个文件的存储模式
		/// </summary>
		SingleFile = 1,

		/// <summary>
		/// 根据文件的大小来存储，固定一个大小，不停的生成文件
		/// </summary>
		FileFixedSize,

		/// <summary>
		/// 根据时间来存储，可以设置年，季，月，日，小时等等
		/// </summary>
		Time
	}

	#endregion

	#region Log Output Format

	/// <summary>
	/// 日志文件输出模式
	/// </summary>
	public enum GenerateMode
	{
		/// <summary>
		/// 按每分钟生成日志文件
		/// </summary>
		ByEveryMinute = 1,

		/// <summary>
		/// 按每个小时生成日志文件
		/// </summary>
		ByEveryHour = 2,

		/// <summary>
		/// 按每天生成日志文件
		/// </summary>
		ByEveryDay = 3,

		/// <summary>
		/// 按每个周生成日志文件
		/// </summary>
		ByEveryWeek = 4,

		/// <summary>
		/// 按每个月生成日志文件
		/// </summary>
		ByEveryMonth = 5,

		/// <summary>
		/// 按每季度生成日志文件
		/// </summary>
		ByEverySeason = 6,

		/// <summary>
		/// 按每年生成日志文件
		/// </summary>
		ByEveryYear = 7,
	}

	#endregion

	#region Message Degree

	/// <summary>
	/// 记录消息的等级
	/// </summary>
	public enum HslMessageDegree
	{
		/// <summary>
		/// 一条消息都不记录
		/// </summary>
		None = 1,

		/// <summary>
		/// 记录致命等级及以上日志的消息
		/// </summary>
		FATAL = 2,
		/// <summary>
		/// 记录异常等级及以上日志的消息
		/// </summary>
		ERROR = 3,

		/// <summary>
		/// 记录警告等级及以上日志的消息
		/// </summary>
		WARN = 4,

		/// <summary>
		/// 记录信息等级及以上日志的消息
		/// </summary>
		INFO = 5,

		/// <summary>
		/// 记录调试等级及以上日志的信息
		/// </summary>
		DEBUG = 6
	}

	#endregion

	#region LogMessage

	/// <summary>
	/// 单条日志的记录信息，包含了消息等级，线程号，关键字，文本信息<br />
	/// Record information of a single log, including message level, thread number, keywords, text information
	/// </summary>
	public class HslMessageItem
	{
		private static long IdNumber = 0;

		/// <summary>
		/// 实例化一个默认的对象<br />
		/// Instantiate a default object
		/// </summary>
		public HslMessageItem( )
		{
			Id    = Interlocked.Increment( ref IdNumber );
			Time  = DateTime.Now;
		}

		/// <summary>
		/// 单个记录信息的标识ID，程序重新运行时清空，代表程序从运行以来的日志计数，不管存储的或是未存储的<br />
		/// The ID of a single record of information. It is cleared when the program is re-run. 
		/// It represents the log count of the program since it was run, whether stored or unstored.
		/// </summary>
		public long Id { get; private set; }

		/// <summary>
		/// 消息的等级，包括DEBUG，INFO，WARN，ERROR，FATAL，NONE共计六个等级<br />
		/// Message levels, including DEBUG, INFO, WARN, ERROR, FATAL, NONE total six levels
		/// </summary>
		public HslMessageDegree Degree { get; set; } = HslMessageDegree.DEBUG;

		/// <summary>
		/// 线程ID，发生异常时的线程号<br />
		/// Thread ID, the thread number when the exception occurred
		/// </summary>
		public int ThreadId { get; set; }

		/// <summary>
		/// 消息文本，记录日志的时候给定<br />
		/// Message text, given when logging
		/// </summary>
		public string Text { get; set; }

		/// <summary>
		/// 记录日志的时间，而非存储日志的时间<br />
		/// The time the log was recorded, not the time it was stored
		/// </summary>
		public DateTime Time { get; set; }

		/// <summary>
		/// 消息的关键字<br />
		/// Keyword of the message
		/// </summary>
		public string KeyWord { get; set; }

		/// <summary>
		/// 是否取消写入到文件中去，在事件 <see cref="LogNetBase.BeforeSaveToFile"/> 触发的时候捕获即可设置。<br />
		/// Whether to cancel writing to the file, can be set when the event <see cref="LogNetBase.BeforeSaveToFile"/> is triggered.
		/// </summary>
		public bool Cancel { get; set; }

		/// <inheritdoc/>
		public override string ToString( )
		{
			if (Degree != HslMessageDegree.None)
			{
				if (string.IsNullOrEmpty( KeyWord ))
					return $"[{LogNetManagment.GetDegreeDescription( Degree )}] {Time:yyyy-MM-dd HH:mm:ss.fff} Thread [{ThreadId:D3}] {Text}";
				else
					return $"[{LogNetManagment.GetDegreeDescription( Degree )}] {Time:yyyy-MM-dd HH:mm:ss.fff} Thread [{ThreadId:D3}] {KeyWord} : {Text}";
			}
			else
			{
				return Text;
			}
		}

		/// <summary>
		/// 返回表示当前对象的字符串，剔除了关键字<br />
		/// Returns a string representing the current object, excluding keywords
		/// </summary>
		/// <returns>字符串信息</returns>
		public string ToStringWithoutKeyword( )
		{
			if (Degree != HslMessageDegree.None)
				return $"[{LogNetManagment.GetDegreeDescription( Degree )}] {Time:yyyy-MM-dd HH:mm:ss.fff} Thread [{ThreadId:D3}] {Text}";
			else
				return Text;
		}
	}

	#endregion

}
