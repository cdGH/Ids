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
	/// 一个用于数值范围记录的类，可以按照时间进行分类统计，比如计算一个温度值的每天的开始值，结束值，最大值，最小值，平均值信息。详细见API文档信息。<br />
	/// A class used to record the value range, which can be classified according to time, such as calculating the start value, end value, 
	/// maximum value, minimum value, and average value of a temperature value. See the API documentation for details.
	/// </summary>
	/// <example>
	/// 我们来举个例子：我们需要对一个温度数据进行分析，分析60天之内的最大值最小值等等信息
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\LogNet\LogValueLimitSample.cs" region="Sample1" title="简单的记录调用次数" />
	/// 因为这个数据是保存在内存里的，程序重新运行就丢失了，如果希望让这个数据一直在程序的话，在软件退出的时候需要存储文件，在软件启动的时候，加载文件数据
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\LogNet\LogValueLimitSample.cs" region="Sample2" title="存储与加载" />
	/// </example>
	public class LogValueLimit : LogStatisticsBase<ValueLimit>
	{
		/// <inheritdoc cref="LogStatisticsBase{T}.LogStatisticsBase(GenerateMode, int)"/>
		public LogValueLimit( GenerateMode generateMode, int dataCount ) : base( generateMode, dataCount ) 
		{
			this.byteTransform = new RegularByteTransform( );
		}

		/// <summary>
		/// 新增一个数据用于分析，将会根据当前的时间来决定插入数据位置，如果数据位置发生了变化，则数据向左发送移动。如果没有移动或是移动完成后，最后一个数进行数据更新，包括最大值，最小值，平均值。<br />
		/// Add a new data for analysis, and will determine the position to insert the data according to the current time. If the data position changes, 
		/// the data will be sent to the left. If there is no movement or after the movement is completed, data update for the last number, including maximum, minimum, and average.
		/// </summary>
		/// <param name="value">当前的新的数据值</param>
		[HslMqttApi( Description = "Add a new data for analysis" )]
		public void AnalysisNewValue( double value )
		{
			Interlocked.Increment( ref valueCount );
			StatisticsCustomAction( m => m.SetNewValue( value ) );
		}

		/// <summary>
		/// 新增一个数据用于分析，将会指定的时间来决定插入数据位置，如果数据位置发生了变化，则数据向左发送移动。如果没有移动或是移动完成后，最后一个数进行数据更新，包括最大值，最小值，平均值。<br />
		/// dd a new data for analysis, and will determine the position to insert the data according to the specified time. If the data position changes, 
		/// the data will be sent to the left. If there is no movement or after the movement is completed, data update for the last number, including maximum, minimum, and average.
		/// </summary>
		/// <param name="value">当前的新的数据值</param>
		/// <param name="time">指定的时间信息</param>
		[HslMqttApi( Description = "Add a new data for analysis" )]
		public void AnalysisNewValueByTime( double value, DateTime time )
		{
			Interlocked.Increment( ref valueCount );
			StatisticsCustomAction( m => m.SetNewValue( value ), time );
		}

		/// <summary>
		/// 当前设置数据的总的数量<br />
		/// The total amount of data currently set
		/// </summary>
		[HslMqttApi( HttpMethod = "GET", Description = "The total amount of data currently set" )]
		public long ValueCount => this.valueCount;

		/// <summary>
		/// 将当前所有的数据都写入到二进制的内存里去，可以用来写入文件或是网络发送。<br />
		/// Write all current data into binary memory, which can be used to write files or send over the network.
		/// </summary>
		/// <returns>二进制的byte数组</returns>
		public byte[] SaveToBinary( )
		{
			// 存储采用文件头 + 数据内容的形式: 文件头（可变长度+数据表述）
			OperateResult<long, ValueLimit[]> dataSnap = GetStatisticsSnapAndDataMark( );
			int headLength = 1024;
			int itemLength = 44 + 20;
			byte[] buffer = new byte[dataSnap.Content2.Length * itemLength + headLength];

			// 文件描述头
			BitConverter.GetBytes( 0x12345679 ).CopyTo( buffer, 0 );                            // 文件头的标记信息
			BitConverter.GetBytes( (ushort)headLength ).CopyTo( buffer, 4 );                    // 文件头的长度
			BitConverter.GetBytes( (ushort)GenerateMode ).CopyTo( buffer, 6 );                  // 文件的存储类型
			BitConverter.GetBytes( dataSnap.Content2.Length ).CopyTo( buffer, 8 );              // 存储的元素的个数
			BitConverter.GetBytes( dataSnap.Content1 ).CopyTo( buffer, 12 );                    // 最后一次标记信息
			BitConverter.GetBytes( valueCount ).CopyTo( buffer, 20 );                           // 所有的数据和信息
			BitConverter.GetBytes( itemLength ).CopyTo( buffer, 28 );                           // 每个元素的数据长度

			for (int i = 0; i < dataSnap.Content2.Length; i++)
			{
				byteTransform.TransByte( dataSnap.Content2[i].StartValue ).CopyTo( buffer, i * itemLength + headLength + 0 );
				byteTransform.TransByte( dataSnap.Content2[i].Current ).   CopyTo( buffer, i * itemLength + headLength + 8 );
				byteTransform.TransByte( dataSnap.Content2[i].MaxValue ).  CopyTo( buffer, i * itemLength + headLength + 16 );
				byteTransform.TransByte( dataSnap.Content2[i].MinValue ).  CopyTo( buffer, i * itemLength + headLength + 24 );
				byteTransform.TransByte( dataSnap.Content2[i].Average ).   CopyTo( buffer, i * itemLength + headLength + 32 );
				byteTransform.TransByte( dataSnap.Content2[i].Count ).     CopyTo( buffer, i * itemLength + headLength + 40 );
			}
			return buffer;
		}

		/// <summary>
		/// 将当前的统计信息及数据内容写入到指定的文件里面，需要指定文件的路径名称<br />
		/// Write the current statistical information and data content to the specified file, you need to specify the path name of the file
		/// </summary>
		/// <param name="fileName">文件的完整的路径名称</param>
		public void SaveToFile( string fileName )
		{
			File.WriteAllBytes( fileName, SaveToBinary( ) );
		}

		/// <summary>
		/// 从二进制的数据内容加载，会对数据的合法性进行检查，如果数据不匹配，会报异常<br />
		/// Loading from the binary data content will check the validity of the data. If the data does not match, an exception will be reported
		/// </summary>
		/// <param name="buffer">等待加载的二进制数据</param>
		/// <exception cref="Exception"></exception>
		public void LoadFromBinary( byte[] buffer )
		{
			int fileCode = BitConverter.ToInt32( buffer, 0 );
			if (fileCode != 0x12345679)
			{
				throw new Exception( $"File is not LogValueLimit file, can't load data." );
			}

			int headLength = BitConverter.ToUInt16( buffer, 4 );
			GenerateMode mode = (GenerateMode)BitConverter.ToUInt16( buffer, 6 );
			int contentLength = BitConverter.ToInt32( buffer, 8 );
			long dataMark = BitConverter.ToInt64( buffer, 12 );
			long totalSum = BitConverter.ToInt64( buffer, 20 );
			int itemLength = BitConverter.ToInt32( buffer, 28 );
			this.generateMode = mode;
			this.valueCount = totalSum;
			ValueLimit[] temp = new ValueLimit[contentLength];
			for (int i = 0; i < temp.Length; i++)
			{
				temp[i].StartValue = byteTransform.TransDouble( buffer, i * itemLength + headLength + 0 );
				temp[i].Current    = byteTransform.TransDouble( buffer, i * itemLength + headLength + 8 );
				temp[i].MaxValue   = byteTransform.TransDouble( buffer, i * itemLength + headLength + 16 );
				temp[i].MinValue   = byteTransform.TransDouble( buffer, i * itemLength + headLength + 24 );
				temp[i].Average    = byteTransform.TransDouble( buffer, i * itemLength + headLength + 32 );
				temp[i].Count      = byteTransform.TransInt32(  buffer, i * itemLength + headLength + 40 );
			}

			Reset( temp, dataMark );
		}

		/// <summary>
		/// 从指定的文件加载对应的统计信息，通常是调用<see cref="SaveToFile(string)"/>方法存储的文件，如果文件不存在，将会跳过加载<br />
		/// Load the corresponding statistical information from the specified file, usually the file stored by calling the <see cref="SaveToFile(string)"/> method. 
		/// If the file does not exist, the loading will be skipped
		/// </summary>
		/// <param name="fileName">文件的完整的路径名称</param>
		public void LoadFromFile( string fileName )
		{
			if (File.Exists( fileName )) LoadFromBinary( File.ReadAllBytes( fileName ) );
		}

		/// <inheritdoc/>
		public override string ToString( ) => $"LogValueLimit[{GenerateMode}:{ArrayLength}]";

		private RegularByteTransform byteTransform;                      // 数据转换的对象
		private long valueCount = 0;                                     // 所有的处理的数据的总和
	}
}
