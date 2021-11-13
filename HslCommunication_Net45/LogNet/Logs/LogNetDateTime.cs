using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace HslCommunication.LogNet
{
	/// <summary>
	/// 一个日志组件，可以根据时间来区分不同的文件存储<br />
	/// A log component that can distinguish different file storages based on time
	/// </summary>
	/// <remarks>
	/// 此日志实例将根据日期时间来进行分类，支持的时间分类如下：
	/// <list type="number">
	/// <item>分钟</item>
	/// <item>小时</item>
	/// <item>天</item>
	/// <item>周</item>
	/// <item>月份</item>
	/// <item>季度</item>
	/// <item>年份</item>
	/// </list>
	/// 当然你也可以指定允许存在多少个日志文件，比如我允许存在最多10个文件，如果你的日志是根据天来分文件的，那就是10天的数据。
	/// 同理，如果你的日志是根据年来分文件的，那就是10年的日志文件。
	/// </remarks>
	/// <example>
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\LogNet\LogNetSingle.cs" region="Example3" title="日期存储实例化" />
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\LogNet\LogNetSingle.cs" region="Example4" title="基本的使用" />
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\LogNet\LogNetSingle.cs" region="Example5" title="所有日志不存储" />
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\LogNet\LogNetSingle.cs" region="Example6" title="仅存储ERROR等级" />
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\LogNet\LogNetSingle.cs" region="Example7" title="不指定路径" />
	/// </example>
	public class LogNetDateTime : LogPathBase, ILogNet
	{
		#region Contructor

		/// <summary>
		/// 实例化一个根据时间存储的日志组件，需要指定每个文件的存储时间范围<br />
		/// Instantiate a log component based on time, you need to specify the storage time range for each file
		/// </summary>
		/// <param name="filePath">文件存储的路径</param>
		/// <param name="generateMode">存储文件的间隔</param>
		/// <param name="fileQuantity">指定当前的日志文件数量上限，如果小于0，则不限制，文件一直增加，如果设置为10，就限制最多10个文件，会删除最近时间的10个文件之外的文件。</param>
		public LogNetDateTime( string filePath, GenerateMode generateMode = GenerateMode.ByEveryYear, int fileQuantity = -1 )
		{
			this.filePath = filePath;
			this.generateMode = generateMode;
			this.LogSaveMode = LogSaveMode.Time;
			this.controlFileQuantity = fileQuantity;

			if (!string.IsNullOrEmpty( filePath ) && !Directory.Exists( filePath ))
				Directory.CreateDirectory( filePath );
		}

		#endregion

		#region LogNetBase Override

		/// <inheritdoc/>
		protected override string GetFileSaveName( )
		{
			if (string.IsNullOrEmpty( filePath )) return string.Empty;

			switch (generateMode)
			{
				case GenerateMode.ByEveryMinute:
					return Path.Combine( filePath, LogNetManagment.LogFileHeadString + DateTime.Now.ToString( "yyyyMMdd_HHmm" ) + ".txt" );
				case GenerateMode.ByEveryHour:
					return Path.Combine( filePath, LogNetManagment.LogFileHeadString + DateTime.Now.ToString( "yyyyMMdd_HH" ) + ".txt" );
				case GenerateMode.ByEveryDay:
					return Path.Combine( filePath, LogNetManagment.LogFileHeadString + DateTime.Now.ToString( "yyyyMMdd" ) + ".txt" );
				case GenerateMode.ByEveryWeek:
					{
						GregorianCalendar gc = new GregorianCalendar( );
						int weekOfYear = gc.GetWeekOfYear( DateTime.Now, CalendarWeekRule.FirstDay, DayOfWeek.Monday );
						return Path.Combine( filePath, LogNetManagment.LogFileHeadString + DateTime.Now.Year + "_W" + weekOfYear + ".txt" );
					}
				case GenerateMode.ByEveryMonth:
					return Path.Combine( filePath, LogNetManagment.LogFileHeadString + DateTime.Now.ToString( "yyyy_MM" ) + ".txt" );
				case GenerateMode.ByEverySeason:
					return Path.Combine( filePath, LogNetManagment.LogFileHeadString + DateTime.Now.Year + "_Q" + (DateTime.Now.Month / 3 + 1) + ".txt" );
				case GenerateMode.ByEveryYear:
					return Path.Combine( filePath, LogNetManagment.LogFileHeadString + DateTime.Now.Year + ".txt" );
				default: return string.Empty;
			}
		}

		#endregion

		#region Private Member

		private GenerateMode generateMode = GenerateMode.ByEveryYear;             // 文件的存储模式，默认按照年份来存储

		#endregion

		#region Object Override

		/// <inheritdoc/>
		public override string ToString( ) => $"LogNetDateTime[{generateMode}]";

		#endregion
	}
}
