using HslCommunication.Core;
using HslCommunication.Reflection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
#if !NET20 && !NET35
using System.Threading.Tasks;
#endif

namespace HslCommunication.LogNet
{
	/// <summary>
	/// <seealso cref="LogStatistics"/>的词典集合类，用于多个数据的统计信息，例如可以统计多个规格的产量信息，统计多个方法的调用次数信息<br />
	/// The dictionary collection class of <seealso cref="LogStatistics"/> is used for the statistical information of multiple data, for example, 
	/// the output information of multiple specifications can be counted, and the number of calls of multiple methods can be counted.
	/// </summary>
	public class LogStatisticsDict
	{
		/// <summary>
		/// 根据指定的存储模式，数据个数来实例化一个对象<br />
		/// According to the specified storage mode, the number of data to instantiate an object
		/// </summary>
		/// <param name="generateMode">当前的数据存储模式</param>
		/// <param name="arrayLength">准备存储的数据总个数</param>
		public LogStatisticsDict( GenerateMode generateMode, int arrayLength )
		{
			this.generateMode    = generateMode;
			this.arrayLength     = arrayLength;
			dictLock             = new object( );
			dict                 = new Dictionary<string, LogStatistics>( 128 );
			logStat              = new LogStatistics( generateMode, arrayLength );
		}

		/// <summary>
		/// 根据给定的关键字信息，获取相关的 <seealso cref="LogStatistics"/> 对象，进而执行很多的操作<br />
		/// According to the given keyword information, obtain related <seealso cref="LogStatistics"/> objects, and then perform many operations
		/// </summary>
		/// <param name="key">关键字</param>
		/// <returns>日志对象，如果当前的日志对象不存在，就返回为NULL</returns>
		public LogStatistics GetLogStatistics( string key )
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
		/// 手动新增一个<seealso cref="LogStatistics"/>对象，需要指定相关的关键字<br />
		/// Manually add a <seealso cref="LogStatistics"/> object, you need to specify related keywords
		/// </summary>
		/// <param name="key">关键字信息</param>
		/// <param name="logStatistics">日志统计对象</param>
		public void AddLogStatistics( string key, LogStatistics logStatistics )
		{
			lock (dictLock)
			{
				if (dict.ContainsKey( key ))
				{
					dict[key] = logStatistics;
				}
				else
				{
					dict.Add( key, logStatistics );
				}
			}
		}

		/// <summary>
		/// 移除一个<seealso cref="LogStatistics"/>对象，需要指定相关的关键字，如果关键字本来就存在，返回 <c>True</c>, 如果不存在，返回 <c>False</c> <br />
		/// To remove a <seealso cref="LogStatistics"/> object, you need to specify the relevant keyword. If the keyword already exists, return <c>True</c>, if it does not exist, return <c>False</c >
		/// </summary>
		/// <param name="key">关键字信息</param>
		/// <returns>如果关键字本来就存在，返回 <c>True</c>, 如果不存在，返回 <c>False</c> </returns>
		public bool RemoveLogStatistics( string key )
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
		/// 新增一个统计信息，将会根据当前的时间来决定插入数据位置，如果数据位置发生了变化，则数据向左发送移动。如果没有移动或是移动完成后，最后一个数进行新增数据 frequency 次<br />
		/// Adding a new statistical information will determine the position to insert the data according to the current time. If the data position changes, 
		/// the data will be sent to the left. If there is no movement or after the movement is completed, add data to the last number frequency times
		/// </summary>
		/// <param name="key">当前选择的关键字</param>
		/// <param name="frequency">新增的次数信息，默认为1</param>
		[HslMqttApi( Description = "Adding a new statistical information will determine the position to insert the data according to the current time" )]
		public void StatisticsAdd( string key, long frequency = 1 )
		{
			logStat.StatisticsAdd( frequency );
			LogStatistics logStatistics = GetLogStatistics( key );
			if (logStatistics == null)
			{
				lock (dictLock)
				{
					if (!dict.ContainsKey( key ))
					{
						logStatistics = new LogStatistics( this.generateMode, this.arrayLength );
						dict.Add( key, logStatistics );
					}
					else
					{
						logStatistics = dict[key];
					}
				}
			}
			logStatistics?.StatisticsAdd( frequency );
		}

		/// <summary>
		/// 新增一个统计信息，将会根据当前的时间来决定插入数据位置，如果数据位置发生了变化，则数据向左发送移动。如果没有移动或是移动完成后，最后一个数进行新增数据 frequency 次<br />
		/// Adding a new statistical information will determine the position to insert the data according to the current time. If the data position changes, 
		/// the data will be sent to the left. If there is no movement or after the movement is completed, add data to the last number frequency times
		/// </summary>
		/// <param name="key">当前的关键字</param>
		/// <param name="frequency">新增的次数信息</param>
		/// <param name="time">新增的次数的时间</param>
		[HslMqttApi( Description = "Adding a new statistical information will determine the position to insert the data according to the specified time" )]
		public void StatisticsAddByTime( string key, long frequency, DateTime time )
		{
			logStat.StatisticsAddByTime( frequency, time );
			LogStatistics logStatistics = GetLogStatistics( key );
			if (logStatistics == null)
			{
				lock (dictLock)
				{
					if (!dict.ContainsKey( key ))
					{
						logStatistics = new LogStatistics( this.generateMode, this.arrayLength );
						dict.Add( key, logStatistics );
					}
					else
					{
						logStatistics = dict[key];
					}
				}
			}
			logStatistics?.StatisticsAddByTime( frequency, time );
		}

		/// <summary>
		/// 获取当前的统计信息的数据快照，这是数据的副本，修改了里面的值不影响<br />
		/// Get a data snapshot of the current statistics. This is a copy of the data. Modifying the value inside does not affect
		/// </summary>
		/// <param name="key">当前的关键字的信息</param>
		/// <returns>实际的统计数据信息</returns>
		[HslMqttApi( Description = "Get a data snapshot of the current statistics" )]
		public long[] GetStatisticsSnapshot( string key )
		{
			return GetLogStatistics( key )?.GetStatisticsSnapshot( );
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
		public long[] GetStatisticsSnapshotByTime( string key, DateTime start, DateTime finish )
		{
			return GetLogStatistics( key )?.GetStatisticsSnapshotByTime( start, finish );
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
					LogStatistics logStatistics = GetLogStatistics( key );
					if (logStatistics != null)
					{
						HslHelper.WriteStringToStream( sw, key );
						HslHelper.WriteBinaryToStream( sw, logStatistics.SaveToBinary( ) );
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

						LogStatistics logStatistics = new LogStatistics( this.generateMode, this.arrayLength );
						logStatistics.LoadFromBinary( content );

						AddLogStatistics( key, logStatistics );
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
		/// 获取当前词典类自身的日志统计对象，统计所有的元素的统计信息<br />
		/// Get the log statistics object of the current dictionary class itself, and count the statistics of all elements
		/// </summary>
		[HslMqttApi( PropertyUnfold = true, Description = "Get the log statistics object of the current dictionary class itself, and count the statistics of all elements" )]
		public LogStatistics LogStat => logStat;

		#region Private Member

		private GenerateMode generateMode = GenerateMode.ByEveryDay;     // 数据更新的频率
		private int arrayLength = 30;                                    // 实际数据的数量
		private Dictionary<string, LogStatistics> dict;                  // 存储数据的核心词典
		private object dictLock;                                         // 词典的数据锁
		private LogStatistics logStat;                                   // 总的数据分析器

		#endregion

	}
}
