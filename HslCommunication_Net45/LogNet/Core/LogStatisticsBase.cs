using HslCommunication.BasicFramework;
using HslCommunication.Core;
using HslCommunication.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
#if !NET20 && !NET35
using System.Threading.Tasks;
#endif

namespace HslCommunication.LogNet
{
	/// <summary>
	/// 一个按照实际进行数据分割的辅助基类，可以用于实现对某个的API按照每天进行调用次数统计，也可以实现对某个设备数据按照天进行最大最小值均值分析，这些都是需要继承实现。<br />
	/// An auxiliary base class that divides the data according to the actual data can be used to implement statistics on the number of calls per day for a certain API, and it can also implement the maximum and minimum average analysis of a certain device data according to the day. These all need to be inherited. .
	/// </summary>
	/// <typeparam name="T">统计的数据类型</typeparam>
	public class LogStatisticsBase<T>
	{
		/// <summary>
		/// 实例化一个新的数据统计内容，需要指定当前的时间统计方式，按小时，按天，按月等等，还需要指定统计的数据数量，比如按天统计30天。<br />
		/// To instantiate a new data statistics content, you need to specify the current time statistics method, by hour, by day, by month, etc., 
		/// and also need to specify the number of statistics, such as 30 days by day.
		/// </summary>
		/// <param name="generateMode">时间的统计方式</param>
		/// <param name="arrayLength">数据的数量信息</param>
		public LogStatisticsBase( GenerateMode generateMode, int arrayLength )
		{
			this.generateMode         = generateMode;
			this.arrayLength            = arrayLength;
			this.statistics           = new T[arrayLength];
			this.lastDataMark         = GetDataMarkFromDateTime( DateTime.Now );
			this.lockStatistics       = new object( );
		}

		/// <summary>
		/// 获取当前的统计类信息时间统计规则<br />
		/// Get the current statistical information time statistics rule
		/// </summary>
		public GenerateMode GenerateMode => this.generateMode;

		/// <summary>
		/// 获取当前的统计类信息的数据总量<br />
		/// Get the total amount of current statistical information
		/// </summary>
		public int ArrayLength => this.arrayLength;

		/// <summary>
		/// 重置当前的统计信息，需要指定统计的数据内容，最后一个数据的标记信息，本方法主要用于还原统计信息<br />
		/// To reset the current statistical information, you need to specify the content of the statistical data, 
		/// and the tag information of the last data. This method is mainly used to restore statistical information
		/// </summary>
		/// <param name="statistics">统计结果数据信息</param>
		/// <param name="lastDataMark">最后一次标记的内容</param>
		public void Reset( T[] statistics, long lastDataMark )
		{
			if (statistics.Length > this.arrayLength)
			{
				Array.Copy( statistics, statistics.Length - this.arrayLength, this.statistics, 0, this.arrayLength );
			}
			else if (statistics.Length < this.arrayLength)
			{
				Array.Copy( statistics, 0, this.statistics, this.arrayLength - statistics.Length, statistics.Length );
			}
			else
			{
				this.statistics = statistics;
			}

			this.arrayLength = statistics.Length;
			this.lastDataMark = lastDataMark;
		}

		/// <summary>
		/// 新增一个统计信息，将会根据当前的时间来决定插入数据位置，如果数据位置发生了变化，则数据向左发送移动。如果没有移动或是移动完成后，最后一个数进行自定义的数据操作<br />
		/// Adding a new statistical information will determine the position to insert the data according to the current time. If the data position changes, 
		/// the data will be sent to the left. If there is no movement or after the movement is completed, Custom data operations on the last number
		/// </summary>
		/// <param name="newValue">增对最后一个数的自定义操作</param>
		protected void StatisticsCustomAction( Func<T, T> newValue )
		{
			lock (lockStatistics)
			{
				long newDataMark = GetDataMarkFromDateTime( DateTime.Now );
				if (this.lastDataMark != newDataMark)
				{
					int leftMove = (int)(newDataMark - lastDataMark);
					this.statistics = GetLeftMoveTimes( leftMove );

					this.lastDataMark = newDataMark;
				}

				this.statistics[this.statistics.Length - 1] = newValue( this.statistics[this.statistics.Length - 1] );
			}
		}

		/// <summary>
		/// 新增一个统计信息，将会根据当前的时间来决定插入数据位置，如果数据位置发生了变化，则数据向左发送移动。如果没有移动或是移动完成后，最后一个数进行自定义的数据操作<br />
		/// Adding a new statistical information will determine the position to insert the data according to the current time. If the data position changes, 
		/// the data will be sent to the left. If there is no movement or after the movement is completed, Custom data operations on the last number
		/// </summary>
		/// <param name="newValue">增对最后一个数的自定义操作</param>
		/// <param name="time">增加的时间信息</param>
		protected void StatisticsCustomAction( Func<T, T> newValue, DateTime time )
		{
			lock (lockStatistics)
			{
				long newDataMark = GetDataMarkFromDateTime( DateTime.Now );
				if (this.lastDataMark != newDataMark)
				{
					int leftMove = (int)(newDataMark - lastDataMark);
					this.statistics = GetLeftMoveTimes( leftMove );

					this.lastDataMark = newDataMark;
				}

				long dataMark = GetDataMarkFromDateTime( time );
				if (dataMark <= newDataMark)
				{
					int index = (int)(dataMark - (newDataMark - this.statistics.Length + 1));
					if (index >= 0 && index < this.statistics.Length)
					{
						this.statistics[index] = newValue( this.statistics[index] );
					}
				}
			}
		}

		/// <summary>
		/// 获取当前的统计信息的数据快照，这是数据的副本，修改了里面的值不影响<br />
		/// Get a data snapshot of the current statistics. This is a copy of the data. Modifying the value inside does not affect
		/// </summary>
		/// <returns>实际的统计数据信息</returns>
		[HslMqttApi( Description = "Get a data snapshot of the current statistics" )]
		public T[] GetStatisticsSnapshot( )
		{
			return GetStatisticsSnapAndDataMark( ).Content2;
		}

		/// <summary>
		/// 根据指定的时间范围来获取统计的数据信息快照，包含起始时间，包含结束时间，这是数据的副本，修改了里面的值不影响<br />
		/// Get a snapshot of statistical data information according to the specified time range, including the start time, 
		/// also the end time. This is a copy of the data. Modifying the value inside does not affect
		/// </summary>
		/// <param name="start">起始时间</param>
		/// <param name="finish">结束时间</param>
		/// <returns>指定实际范围内的数据副本</returns>
		[HslMqttApi( Description = "Get a snapshot of statistical data information according to the specified time range" )]
		public T[] GetStatisticsSnapshotByTime( DateTime start, DateTime finish )
		{
			if (finish <= start) return new T[0];
			lock (lockStatistics)
			{
				long newDataMark = GetDataMarkFromDateTime( DateTime.Now );
				if (this.lastDataMark != newDataMark)
				{
					int leftMove = (int)(newDataMark - lastDataMark);
					this.statistics = GetLeftMoveTimes( leftMove );

					this.lastDataMark = newDataMark;
				}

				long dataMarkStart = newDataMark - statistics.Length + 1;

				long markStart = GetDataMarkFromDateTime( start );
				long markEnd   = GetDataMarkFromDateTime( finish );
				if (markStart < dataMarkStart) markStart = dataMarkStart;
				if (markEnd > newDataMark)     markEnd = newDataMark;

				int index  = (int)(markStart - dataMarkStart);
				int length = (int)(markEnd - markStart + 1);

				if (markStart == markEnd) return new T[] { this.statistics[index] };

				T[] newArray = new T[length];
				for (int i = 0; i < length; i++)
				{
					newArray[i] = this.statistics[index + i];
				}
				return newArray;
			}
		}

		/// <summary>
		/// 获取当前的统计信息的数据快照，这是数据的副本，修改了里面的值不影响<br />
		/// Get a data snapshot of the current statistics. This is a copy of the data. Modifying the value inside does not affect
		/// </summary>
		/// <returns>实际的统计数据信息</returns>
		public OperateResult<long, T[]> GetStatisticsSnapAndDataMark( )
		{
			lock (lockStatistics)
			{
				long newDataMark = GetDataMarkFromDateTime( DateTime.Now );
				if (this.lastDataMark != newDataMark)
				{
					int leftMove = (int)(newDataMark - lastDataMark);
					this.statistics = GetLeftMoveTimes( leftMove );

					this.lastDataMark = newDataMark;
				}
				return OperateResult.CreateSuccessResult( newDataMark, this.statistics.CopyArray( ) );
			}
		}

		/// <summary>
		/// 根据当前数据统计的时间模式，获取最新的数据标记信息<br />
		/// Obtain the latest data mark information according to the time mode of current data statistics
		/// </summary>
		/// <returns>数据标记</returns>
		[HslMqttApi( Description = "Obtain the latest data mark information according to the time mode of current data statistics" )]
		public long GetDataMarkFromTimeNow( ) => GetDataMarkFromDateTime( DateTime.Now );

		/// <summary>
		/// 根据指定的时间，获取到该时间指定的数据标记信息<br />
		/// According to the specified time, get the data mark information specified at that time
		/// </summary>
		/// <param name="dateTime">指定的时间</param>
		/// <returns>数据标记</returns>
		[HslMqttApi( Description = "According to the specified time, get the data mark information specified at that time" )]
		public long GetDataMarkFromDateTime( DateTime dateTime )
		{
			switch (this.generateMode)
			{
				case GenerateMode.ByEveryMinute:     return GetMinuteFromTime(   dateTime );
				case GenerateMode.ByEveryHour:       return GetHourFromTime(     dateTime );
				case GenerateMode.ByEveryDay:        return GetDayFromTime(      dateTime );
				case GenerateMode.ByEveryWeek:       return GetWeekFromTime(     dateTime );
				case GenerateMode.ByEveryMonth:      return GetMonthFromTime(    dateTime );
				case GenerateMode.ByEverySeason:     return GetSeasonFromTime(   dateTime );
				case GenerateMode.ByEveryYear:       return GetYearFromTime(     dateTime );
				default:                             return GetDayFromTime(      dateTime );
			}
		}

		private long GetMinuteFromTime( DateTime dateTime ) => (dateTime.Date - new DateTime( 1970, 1, 1 )).Days * 24L * 60 + dateTime.Hour * 60 + dateTime.Minute;
		private long GetHourFromTime(   DateTime dateTime ) => (dateTime.Date - new DateTime( 1970, 1, 1 )).Days * 24L + dateTime.Hour;
		private long GetDayFromTime(    DateTime dateTime ) => (dateTime.Date - new DateTime( 1970, 1, 1 )).Days;
		private long GetWeekFromTime(   DateTime dateTime ) => ((dateTime.Date - new DateTime( 1970, 1, 1 )).Days + 3L) / 7;
		private long GetMonthFromTime(  DateTime dateTime ) => (dateTime.Year - 1970) * 12L + (dateTime.Month - 1);
		private long GetSeasonFromTime( DateTime dateTime ) => (dateTime.Year - 1970) * 4L + (dateTime.Month - 1) / 3;
		private long GetYearFromTime(   DateTime dateTime ) => dateTime.Year - 1970;
		private T[] GetLeftMoveTimes( int times )
		{
			if (times >= statistics.Length) return new T[this.arrayLength];
			T[] newArray = new T[this.arrayLength];
			Array.Copy( statistics, times, newArray, 0, statistics.Length - times );
			return newArray;
		}

		private T[] statistics = null;                                   // 实际存储的数据信息
		/// <summary>
		/// 当前的实际模式
		/// </summary>
		protected GenerateMode generateMode = GenerateMode.ByEveryDay;   // 数据更新的频率
		private int arrayLength = 30;                                    // 实际数据的数量
		private long lastDataMark = -1;                                  // 上一次数据的标记信息
		private object lockStatistics;                                   // 列表的锁
	}
}
