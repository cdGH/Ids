using HslCommunication.Reflection;
using System;

namespace HslCommunication.LogNet
{
	/// <summary>
	/// 一个通用的日志接口，支持5个等级的日志消息写入，支持设置当前的消息等级，定义一个消息存储前的触发事件。<br />
	/// A general-purpose log interface, supports the writing of 5 levels of log messages, supports setting the current message level, and defining a trigger event before a message is stored.
	/// </summary>
	/// <remarks>
	/// 本组件的日志核心机制，如果您使用了本组件却不想使用本组件的日志组件功能，可以自己实现新的日志组件，只要继承本接口接口。其他常用的日志组件如下：（都是可以实现的）
	/// <list type="number">
	/// <item>Log4Net</item>
	/// <item>NLog</item>
	/// </list>
	/// </remarks>
	/// <example>
	/// 自己实例化操作，在HslCommunication里面，可选三种类型
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\LogNet\LogNetSingle.cs" region="Example1" title="单文件实例化" />
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\LogNet\LogNetSingle.cs" region="Example2" title="限制文件大小实例化" />
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\LogNet\LogNetSingle.cs" region="Example3" title="日期存储实例化" />
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\LogNet\LogNetSingle.cs" region="Example4" title="基本的使用" />
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\LogNet\LogNetSingle.cs" region="Example5" title="所有日志不存储" />
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\LogNet\LogNetSingle.cs" region="Example6" title="仅存储ERROR等级" />
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\LogNet\LogNetSingle.cs" region="Example7" title="不指定路径" />
	/// Form的示例，存储日志的使用都是一样的，就是实例化的时候不一致，以下示例代码以单文件日志为例
	/// <code lang="cs" source="TestProject\HslCommunicationDemo\Hsl\FormLogNet.cs" region="ILogNet" title="ILogNet示例" />
	/// </example>
	public interface ILogNet : IDisposable
	{
		/// <summary>
		/// 日志存储模式，1:单文件，2:按大小存储，3:按时间存储<br />
		/// Log storage mode, 1: single file, 2: storage by size, 3: storage by time
		/// </summary>
		LogSaveMode LogSaveMode { get; }

		/// <summary>
		/// 获取或设置当前的日志记录统计信息，如果你需要统计最近30天的日志，就需要对其实例化，详细参照<see cref="LogStatistics"/><br />
		/// Get or set the current log record statistics. If you need to count the logs of the last 30 days, 
		/// you need to instantiate it. For details, please refer to <see cref="LogStatistics"/>
		/// </summary>
		LogStatistics LogNetStatistics { get; set; }

		/// <summary>
		/// 获取或设置当前的日志信息在存储的时候是否在控制台进行输出，默认不输出。<br />
		/// Gets or sets whether the current log information is output on the console when it is stored. It is not output by default.
		/// </summary>
		bool ConsoleOutput { get; set; }

		/// <summary>
		/// 存储之前引发的事件，允许额外的操作，比如打印控制台，存储数据库等等<br />
		/// Store previously raised events, allowing additional operations, such as print console, store database, etc.
		/// </summary>
		event EventHandler<HslEventArgs> BeforeSaveToFile;

		/// <summary>
		/// 通过指定消息等级，关键字，日志信息进行消息记录<br />
		/// Record messages by specifying message level, keywords, and log information
		/// </summary>
		/// <param name="degree">消息等级</param>
		/// <param name="keyWord">关键字</param>
		/// <param name="text">日志内容</param>
		void RecordMessage( HslMessageDegree degree, string keyWord, string text );

		/// <summary>
		/// 写入一条解释性的信息，不属于消息等级控制的范畴<br />
		/// Write an explanatory message that is not part of message level control
		/// </summary>
		/// <param name="description">解释文本</param>
		void WriteDescrition( string description );

		/// <summary>
		/// 写入一条调试日志<br />
		/// Write a debug log
		/// </summary>
		/// <param name="text">日志内容</param>
		void WriteDebug( string text );

		/// <summary>
		/// 写入一条带关键字的调试日志<br />
		/// Write a debug log with keywords
		/// </summary>
		/// <param name="keyWord">关键字</param>
		/// <param name="text">日志内容</param>
		void WriteDebug( string keyWord, string text );

		/// <summary>
		/// 写入一条错误日志<br />
		/// Write an error log
		/// </summary>
		/// <param name="text">日志内容</param>
		void WriteError( string text );

		/// <summary>
		/// 写入一条带关键字的错误日志<br />
		/// Write an error log with keywords
		/// </summary>
		/// <param name="keyWord">关键字</param>
		/// <param name="text">日志内容</param>
		void WriteError( string keyWord, string text );

		/// <summary>
		/// 写入一条带关键字的异常信息<br />
		/// Write an exception log with keywords
		/// </summary>
		/// <param name="keyWord">关键字</param>
		/// <param name="ex">异常</param>
		void WriteException( string keyWord, Exception ex );

		/// <summary>
		/// 写入一条带关键字和描述信息的异常信息<br />
		/// Write an exception log with keywords and text
		/// </summary>
		/// <param name="keyWord">关键字</param>
		/// <param name="text">内容</param>
		/// <param name="ex">异常</param>
		void WriteException( string keyWord, string text, Exception ex );

		/// <summary>
		/// 写入一条致命日志<br />
		/// Write an fatal log
		/// </summary>
		/// <param name="text">日志内容</param>
		void WriteFatal( string text );

		/// <summary>
		/// 写入一条带关键字的致命日志<br />
		/// Write an fatal log with keywords
		/// </summary>
		/// <param name="keyWord">关键字</param>
		/// <param name="text">日志内容</param>
		void WriteFatal( string keyWord, string text );

		/// <summary>
		/// 写入一条普通日志<br />
		/// Write an infomation log
		/// </summary>
		/// <param name="text">日志内容</param>
		void WriteInfo( string text );

		/// <summary>
		/// 写入一条带关键字的普通日志<br />
		/// Write an information log with keywords
		/// </summary>
		/// <param name="keyWord">关键字</param>
		/// <param name="text">日志内容</param>
		void WriteInfo( string keyWord, string text );

		/// <summary>
		/// 写入一行换行符<br />
		/// Write a newline
		/// </summary>
		void WriteNewLine( );

		/// <summary>
		/// 写入任意字符串<br />
		/// Write arbitrary string
		/// </summary>
		/// <param name="text">文本</param>
		void WriteAnyString( string text );

		/// <summary>
		/// 写入一条警告日志<br />
		/// Write an warn log
		/// </summary>
		/// <param name="text">日志内容</param>
		void WriteWarn( string text );

		/// <summary>
		/// 写入一条带关键字的警告日志<br />
		/// Write an warn log  with keywords
		/// </summary>
		/// <param name="keyWord">关键字</param>
		/// <param name="text">日志内容</param>
		void WriteWarn( string keyWord, string text );

		/// <summary>
		/// 设置日志的存储等级，高于该等级的才会被存储<br />
		/// Set the storage level of the logs. Only the logs above this level will be stored.
		/// </summary>
		/// <param name="degree">登记信息</param>
		void SetMessageDegree( HslMessageDegree degree );

		/// <summary>
		/// 获取已存在的日志文件名称<br />
		/// Get the name of an existing log file
		/// </summary>
		/// <returns>文件列表</returns>
		string[] GetExistLogFileNames( );

		/// <summary>
		/// 过滤掉指定的关键字的日志，该信息不存储，但仍然触发<see cref="BeforeSaveToFile"/>事件<br />
		/// Filter out the logs of the specified keywords, the information is not stored, but the <see cref="BeforeSaveToFile" /> event is still triggered
		/// </summary>
		/// <param name="keyword">关键字</param>
		void FiltrateKeyword( string keyword );

		/// <summary>
		/// 移除过滤的关键字存储<br />
		/// Remove filtered keyword storage
		/// </summary>
		/// <param name="keyword">关键字</param>
		void RemoveFiltrate( string keyword );
	}
}
