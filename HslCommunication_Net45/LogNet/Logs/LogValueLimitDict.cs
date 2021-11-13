using HslCommunication.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HslCommunication.Core;
using System.IO;
#if !NET20 && !NET35
using System.Threading.Tasks;
#endif

namespace HslCommunication.LogNet
{
	/// <summary>
	/// <seealso cref="LogValueLimit"/> 的词典集合类，用于多个数据的统计信息，例如可以统计多个温度变量的最大值，最小值，平均值<br />
	/// <seealso cref="LogValueLimit"/> The dictionary collection class, used for statistical information of multiple data, 
	/// for example, it can count the maximum, minimum, and average values of multiple temperature variables
	/// </summary>
	public class LogValueLimitDict
	{
		/// <summary>
		/// 根据指定的存储模式，数据个数来实例化一个对象<br />
		/// According to the specified storage mode, the number of data to instantiate an object
		/// </summary>
		/// <param name="generateMode">当前的数据存储模式</param>
		/// <param name="arrayLength">准备存储的数据总个数</param>
		public LogValueLimitDict( GenerateMode generateMode, int arrayLength )
		{
			this.generateMode     = generateMode;
			this.arrayLength      = arrayLength;
			dictLock              = new object( );
			dict                  = new Dictionary<string, LogValueLimit>( 128 );
			logStat               = new LogStatistics( generateMode, arrayLength );
		}

		/// <summary>
		/// 根据给定的关键字信息，获取相关的 <see cref="LogValueLimit"/> 对象，进而执行很多的操作<br />
		/// According to the given keyword information, obtain related <see cref="LogValueLimit"/> objects, and then perform many operations
		/// </summary>
		/// <param name="key">关键字</param>
		/// <returns>日志对象，如果当前的日志对象不存在，就返回为NULL</returns>
		public LogValueLimit GetLogValueLimit( string key )
		{
			lock (dictLock)
			{
				if (dict.ContainsKey( key ))
				{
					return dict[key];
				}
				return null;
			}
		}

		/// <summary>
		/// 手动新增一个 <see cref="LogValueLimit"/> 对象，需要指定相关的关键字<br />
		/// Manually add a <see cref="LogValueLimit"/> object, you need to specify related keywords
		/// </summary>
		/// <param name="key">关键字信息</param>
		/// <param name="logValueLimit">日志数据分析对象</param>
		public void AddLogValueLimit( string key, LogValueLimit logValueLimit )
		{
			lock (dictLock)
			{
				if (dict.ContainsKey( key ))
				{
					dict[key] = logValueLimit;
				}
				else
				{
					dict.Add( key, logValueLimit );
				}
			}
		}

		/// <summary>
		/// 移除一个<seealso cref="LogValueLimit"/>对象，需要指定相关的关键字，如果关键字本来就存在，返回 <c>True</c>, 如果不存在，返回 <c>False</c> <br />
		/// To remove a <seealso cref="LogValueLimit"/> object, you need to specify the relevant keyword. If the keyword already exists, return <c>True</c>, if it does not exist, return <c>False</c >
		/// </summary>
		/// <param name="key">关键字信息</param>
		/// <returns>如果关键字本来就存在，返回 <c>True</c>, 如果不存在，返回 <c>False</c> </returns>
		public bool RemoveLogValueLimit( string key )
		{
			lock (dictLock)
			{
				if (dict.ContainsKey( key ))
				{
					dict.Remove( key );
					return true;
				}
				return false;
			}
		}

		/// <summary>
		/// 新增一个数据用于分析，将会根据当前的时间来决定插入数据位置，如果数据位置发生了变化，则数据向左发送移动。如果没有移动或是移动完成后，最后一个数进行数据更新，包括最大值，最小值，平均值。<br />
		/// Add a new data for analysis, and will determine the position to insert the data according to the current time. If the data position changes, 
		/// the data will be sent to the left. If there is no movement or after the movement is completed, data update for the last number, including maximum, minimum, and average.
		/// </summary>
		/// <param name="key">当前的关键字</param>
		/// <param name="value">当前的新的数据值</param>
		[HslMqttApi( Description = "Add a new data for analysis" )]
		public void AnalysisNewValue( string key, double value )
		{
			logStat.StatisticsAdd( 1 );
			LogValueLimit logValueLimit = GetLogValueLimit( key );
			if (logValueLimit == null)
			{
				lock (dictLock)
				{
					if (!dict.ContainsKey( key ))
					{
						logValueLimit = new LogValueLimit( this.generateMode, this.arrayLength );
						dict.Add( key, logValueLimit );
					}
					else
					{
						logValueLimit = dict[key];
					}
				}
			}
			logValueLimit?.AnalysisNewValue( value );
		}

		/// <summary>
		/// 新增一个数据用于分析，将会指定的时间来决定插入数据位置，如果数据位置发生了变化，则数据向左发送移动。如果没有移动或是移动完成后，最后一个数进行数据更新，包括最大值，最小值，平均值。<br />
		/// dd a new data for analysis, and will determine the position to insert the data according to the specified time. If the data position changes, 
		/// the data will be sent to the left. If there is no movement or after the movement is completed, data update for the last number, including maximum, minimum, and average.
		/// </summary>
		/// <param name="key">关键字信息</param>
		/// <param name="value">当前的新的数据值</param>
		/// <param name="time">指定的时间信息</param>
		[HslMqttApi( Description = "Add a new data for analysis" )]
		public void AnalysisNewValueByTime( string key, double value, DateTime time )
		{
			logStat.StatisticsAddByTime( 1, time );
			LogValueLimit logValueLimit = GetLogValueLimit( key );
			if (logValueLimit == null)
			{
				lock (dictLock)
				{
					if (!dict.ContainsKey( key ))
					{
						logValueLimit = new LogValueLimit( this.generateMode, this.arrayLength );
						dict.Add( key, logValueLimit );
					}
					else
					{
						logValueLimit = dict[key];
					}
				}
			}
			logValueLimit?.AnalysisNewValueByTime( value, time );
		}

		/// <summary>
		/// 获取当前的统计信息的数据快照，这是数据的副本，修改了里面的值不影响<br />
		/// Get a data snapshot of the current statistics. This is a copy of the data. Modifying the value inside does not affect
		/// </summary>
		/// <param name="key">当前的关键字的信息</param>
		/// <returns>实际的统计数据信息</returns>
		[HslMqttApi( Description = "Get a data snapshot of the current statistics" )]
		public ValueLimit[] GetStatisticsSnapshot( string key )
		{
			return GetLogValueLimit( key )?.GetStatisticsSnapshot( );
		}

		/// <summary>
		/// 根据指定的时间范围来获取统计的数据信息快照，包含起始时间，包含结束时间，这是数据的副本，修改了里面的值不影响<br />
		/// Get a snapshot of statistical data information according to the specified time range, including the start time, 
		/// also the end time. This is a copy of the data. Modifying the value inside does not affect
		/// </summary>
		/// <param name="key">当前的关键字信息</param>
		/// <param name="start">起始时间</param>
		/// <param name="finish">结束时间</param>
		/// <returns>指定实际范围内的数据副本</returns>
		[HslMqttApi( Description = "Get a snapshot of statistical data information according to the specified time range" )]
		public ValueLimit[] GetStatisticsSnapshotByTime( string key, DateTime start, DateTime finish )
		{
			return GetLogValueLimit( key )?.GetStatisticsSnapshotByTime( start, finish );
		}

		/// <summary>
		/// 获取所有的关键字的数据信息<br />
		/// Get data information of all keywords
		/// </summary>
		/// <returns>字符串数组</returns>
		[HslMqttApi( Description = "Get data information of all keywords" )]
		public string[] GetKeys( )
		{
			lock (dictLock)
			{
				return dict.Keys.ToArray( );
			}
		}

		/// <summary>
		/// 将当前的统计信息及数据内容写入到指定的文件里面，需要指定文件的路径名称<br />
		/// Write the current statistical information and data content to the specified file, you need to specify the path name of the file
		/// </summary>
		/// <param name="fileName">文件的完整的路径名称</param>
		public void SaveToFile( string fileName )
		{
			using (FileStream sw = new FileStream( fileName, FileMode.Create, FileAccess.Write ))
			{
				// 存储采用文件头 + 数据内容的形式: 文件头（可变长度+数据表述）
				byte[] head = new byte[1024];
				BitConverter.GetBytes( 0x12345682 ).CopyTo( head, 0 );                            // 文件头的标记信息
				BitConverter.GetBytes( (ushort)head.Length ).CopyTo( head, 4 );                   // 文件头的长度
				BitConverter.GetBytes( (ushort)GenerateMode ).CopyTo( head, 6 );                  // 文件的存储类型
				string[] keys = GetKeys( );
				BitConverter.GetBytes( keys.Length ).CopyTo( head, 8 );
				sw.Write( head, 0, head.Length );

				foreach (var key in keys)
				{
					LogValueLimit logValueLimit = GetLogValueLimit( key );
					if (logValueLimit != null)
					{
						HslHelper.WriteStringToStream( sw, key );
						HslHelper.WriteBinaryToStream( sw, logValueLimit.SaveToBinary( ) );
					}
				}
			}
		}

		/// <summary>
		/// 从指定的文件加载对应的统计信息，通常是调用<see cref="SaveToFile(string)"/>方法存储的文件，如果文件不存在，将会跳过加载<br />
		/// Load the corresponding statistical information from the specified file, usually the file stored by calling the <see cref="SaveToFile(string)"/> method. 
		/// If the file does not exist, the loading will be skipped
		/// </summary>
		/// <param name="fileName">文件的完整的路径名称</param>
		/// <exception cref="Exception">当文件的模式和当前的模式设置不一样的时候，会引发异常</exception>
		public void LoadFromFile( string fileName )
		{
			if (File.Exists( fileName ))
			{
				using (FileStream sw = new FileStream( fileName, FileMode.Open, FileAccess.Read ))
				{
					byte[] head = HslHelper.ReadSpecifiedLengthFromStream( sw, 1024 );
					this.generateMode = (GenerateMode)BitConverter.ToUInt16( head, 6 );
					int count = BitConverter.ToInt32( head, 8 );
					for (int i = 0; i < count; i++)
					{
						string key = HslHelper.ReadStringFromStream( sw );
						byte[] content = HslHelper.ReadBinaryFromStream( sw );

						LogValueLimit logValueLimit = new LogValueLimit( this.generateMode, this.arrayLength );
						logValueLimit.LoadFromBinary( content );

						AddLogValueLimit( key, logValueLimit );
					}
				}
			}
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
		[HslMqttApi( HttpMethod = "GET", Description = "Get the total amount of current statistical information" )]
		public int ArrayLength => this.arrayLength;

		/// <summary>
		/// 获取当前词典类自身的日志统计对象，统计所有的元素的数据分析次数<br />
		/// Get the log statistics object of the current dictionary class itself, count the data analysis times of all elements
		/// </summary>
		[HslMqttApi( PropertyUnfold = true, Description = "Get the log statistics object of the current dictionary class itself, and count the statistics of all elements" )]
		public LogStatistics LogStat => logStat;

		#region Private Member

		private GenerateMode generateMode = GenerateMode.ByEveryDay;     // 数据更新的频率
		private int arrayLength = 30;                                    // 实际数据的数量
		private Dictionary<string, LogValueLimit> dict;                  // 存储数据的核心词典
		private object dictLock;                                         // 词典的数据锁
		private LogStatistics logStat;                                   // 总的数据分析器

		#endregion

	}
}
