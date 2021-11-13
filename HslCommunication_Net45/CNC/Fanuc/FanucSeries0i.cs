using HslCommunication.BasicFramework;
using HslCommunication.Core;
using HslCommunication.Core.IMessage;
using HslCommunication.Core.Net;
using HslCommunication;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using HslCommunication.Reflection;
#if !NET35 && !NET20
using System.Threading.Tasks;
#endif

namespace HslCommunication.CNC.Fanuc
{
	/// <summary>
	/// 一个FANUC的机床通信类对象
	/// </summary>
	public class FanucSeries0i : NetworkDoubleBase
	{
		#region Constructor

		/// <summary>
		/// 根据IP及端口来实例化一个对象内容
		/// </summary>
		/// <param name="ipAddress">Ip地址信息</param>
		/// <param name="port">端口号</param>
		public FanucSeries0i( string ipAddress, int port = 8193 )
		{
			this.IpAddress      = ipAddress;
			this.Port           = port;
			this.ByteTransform  = new ReverseBytesTransform( );
			this.encoding       = Encoding.Default;
			this.receiveTimeOut = 30_000;
		}

		/// <inheritdoc/>
		protected override INetMessage GetNewNetMessage( ) => new CNCFanucSeriesMessage( );

		/// <summary>
		/// 获取或设置当前的文本的字符编码信息，如果你不清楚，可以调用<see cref="ReadLanguage"/>方法来自动匹配。<br />
		/// Get or set the character encoding information of the current text. 
		/// If you are not sure, you can call the <see cref="ReadLanguage"/> method to automatically match.
		/// </summary>
		public Encoding TextEncoding
		{
			get => this.encoding;
			set => this.encoding = value;
		}

		#endregion

		#region NetworkDoubleBase Override

		/// <inheritdoc/>
		protected override OperateResult InitializationOnConnect( Socket socket )
		{
			OperateResult<byte[]> read1 = ReadFromCoreServer( socket, "a0 a0 a0 a0 00 01 01 01 00 02 00 02".ToHexBytes( ) );
			if (!read1.IsSuccess) return read1;

			OperateResult<byte[]> read2 = ReadFromCoreServer( socket, "a0 a0 a0 a0 00 01 21 01 00 1e 00 01 00 1c 00 01 00 01 00 18 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00".ToHexBytes( ) );
			if (!read2.IsSuccess) return read2;

			return OperateResult.CreateSuccessResult( );
		}
		/// <inheritdoc/>
		protected override OperateResult ExtraOnDisconnect( Socket socket )
		{
			return ReadFromCoreServer( socket, "a0 a0 a0 a0 00 01 02 01 00 00".ToHexBytes( ) );
		}
#if !NET35 && !NET20
		/// <inheritdoc/>
		protected async override Task<OperateResult> InitializationOnConnectAsync( Socket socket )
		{
			OperateResult<byte[]> read1 = await ReadFromCoreServerAsync( socket, "a0 a0 a0 a0 00 01 01 01 00 02 00 02".ToHexBytes( ) );
			if (!read1.IsSuccess) return read1;

			OperateResult<byte[]> read2 = await ReadFromCoreServerAsync( socket, BuildReadArray( BuildReadSingle( 0x18, 0, 0, 0, 0, 0 ) ) );
			if (!read2.IsSuccess) return read2;

			return OperateResult.CreateSuccessResult( );
		}

		/// <inheritdoc/>
		protected async override Task<OperateResult> ExtraOnDisconnectAsync( Socket socket )
		{
			return await ReadFromCoreServerAsync( socket, "a0 a0 a0 a0 00 01 02 01 00 00".ToHexBytes( ) );
		}
#endif
		#endregion

		private double GetFanucDouble( byte[] content, int index )
		{
			return GetFanucDouble( content, index, 1 )[0];
		}

		private double[] GetFanucDouble( byte[] content, int index, int length )
		{
			double[] buffer = new double[length];
			for (int i = 0; i < length; i++)
			{
				int data = ByteTransform.TransInt32( content, index + 8 * i );
				int decs = ByteTransform.TransInt16( content, index + 8 * i + 6 );

				if (data == 0)
					buffer[i] = 0;
				else
					buffer[i] = Math.Round( data * Math.Pow( 0.1d, decs ), decs );
			}
			return buffer;
		}

		private byte[] CreateFromFanucDouble( double value )
		{
			byte[] buffer = new byte[8];
			int interge = (int)(value * 1000);
			ByteTransform.TransByte( interge ).CopyTo( buffer, 0 );
			buffer[5] = 0x0A;
			buffer[7] = 0x03;
			return buffer;
		}

		private void ChangeTextEncoding( ushort code )
		{
			switch (code)
			{
				case 0x00: this.encoding = Encoding.Default; break;
				case 0x01:
				case 0x04: this.encoding = Encoding.GetEncoding( "shift_jis", EncoderFallback.ReplacementFallback, new DecoderReplacementFallback( ) ); break;
				case 0x06: this.encoding = Encoding.GetEncoding( "ks_c_5601-1987" ); break;
				case 0x0F: this.encoding = Encoding.Default; break;
				case 0x10: this.encoding = Encoding.GetEncoding( "windows-1251" ); break;
				case 0x11: this.encoding = Encoding.GetEncoding( "windows-1254" ); break;
			}
		}

		#region Read Write Support

		/// <summary>
		/// 主轴转速及进给倍率<br />
		/// Spindle speed and feedrate override
		/// </summary>
		/// <returns>主轴转速及进给倍率</returns>
		[HslMqttApi( Description = "Spindle speed and feedrate override" )]
		public OperateResult<double, double> ReadSpindleSpeedAndFeedRate( )
		{
			OperateResult<byte[]> read = ReadFromCoreServer( BuildReadArray(
				BuildReadSingle( 0xA4, 3, 0, 0, 0, 0 ),
				BuildReadSingle( 0x8A, 1, 0, 0, 0, 0 ),
				BuildReadSingle( 0x88, 3, 0, 0, 0, 0 ),
				BuildReadSingle( 0x88, 4, 0, 0, 0, 0 ),
				BuildReadSingle( 0x24, 0, 0, 0, 0, 0 ),
				BuildReadSingle( 0x25, 0, 0, 0, 0, 0 ),
				BuildReadSingle( 0xA4, 3, 0, 0, 0, 0 )
				) );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<double, double>( read );

			List<byte[]> result = ExtraContentArray( read.Content.RemoveBegin( 10 ) );
			return OperateResult.CreateSuccessResult( GetFanucDouble( result[5], 14 ), GetFanucDouble( result[4], 14 ) );
		}

		/// <summary>
		/// 读取程序名及程序号<br />
		/// Read program name and program number
		/// </summary>
		/// <returns>程序名及程序号</returns>
		[HslMqttApi( Description = "Read program name and program number" )]
		public OperateResult<string, int> ReadSystemProgramCurrent( )
		{
			OperateResult<byte[]> read = ReadFromCoreServer( BuildReadArray(
				BuildReadSingle( 0xCF, 0, 0, 0, 0, 0 )
				) );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<string, int>( read );

			List<byte[]> result = ExtraContentArray( read.Content.RemoveBegin( 10 ) );
			int number = ByteTransform.TransInt32( result[0], 14 );
			string name = this.encoding.GetString( result[0].SelectMiddle( 18, 36 ) ).TrimEnd( '\0' );
			return OperateResult.CreateSuccessResult( name, number );
		}

		/// <summary>
		/// 读取机床的语言设定信息，具体值的含义参照API文档说明<br />
		/// Read the language setting information of the machine tool, refer to the API documentation for the meaning of the specific values
		/// </summary>
		/// <remarks>此处举几个常用值 0: 英语 1: 日语 2: 德语 3: 法语 4: 中文繁体 6: 韩语 15: 中文简体 16: 俄语 17: 土耳其语</remarks>
		/// <returns>返回的语言代号</returns>
		[HslMqttApi( Description = "Read the language setting information of the machine tool" )]
		public OperateResult<ushort> ReadLanguage( )
		{
			OperateResult<byte[]> read = ReadFromCoreServer( BuildReadArray(
				BuildReadSingle( 0x8D, 0x0CD1, 0x0CD1, 0, 0, 0 )
				) );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<ushort>( read );
			List<byte[]> result = ExtraContentArray( read.Content.RemoveBegin( 10 ) );

			ushort code = ByteTransform.TransUInt16( result[0], 24 );
			ChangeTextEncoding( code );
			return OperateResult.CreateSuccessResult( code );
		}

		/// <summary>
		/// 读取宏变量，可以用来读取刀具号<br />
		/// Read macro variable, can be used to read tool number
		/// </summary>
		/// <param name="number">刀具号</param>
		/// <returns>读宏变量信息</returns>
		[HslMqttApi( Description = "Read macro variable, can be used to read tool number" )]
		public OperateResult<double> ReadSystemMacroValue( int number )
		{
			return ByteTransformHelper.GetResultFromArray( ReadSystemMacroValue( number, 1 ) );
		}

		/// <summary>
		/// 读取宏变量，可以用来读取刀具号<br />
		/// Read macro variable, can be used to read tool number
		/// </summary>
		/// <param name="number">宏变量地址</param>
		/// <param name="length">读取的长度信息</param>
		/// <returns>是否成功</returns>
		[HslMqttApi( ApiTopic = "ReadSystemMacroValueArray", Description = "Read macro variable, can be used to read tool number" )]
		public OperateResult<double[]> ReadSystemMacroValue( int number, int length )
		{
			// 拆分5个5个读
			int[] lenArray = SoftBasic.SplitIntegerToArray( length, 5 );
			int index = number;
			List<byte> result = new List<byte>( );

			for (int i = 0; i < lenArray.Length; i++)
			{
				OperateResult<byte[]> read = ReadFromCoreServer( BuildReadArray(
					BuildReadSingle( 0x15, index, index + lenArray[i] - 1, 0, 0, 0 ) ) );
				if (!read.IsSuccess) return OperateResult.CreateFailedResult<double[]>( read );

				result.AddRange( ExtraContentArray( read.Content.RemoveBegin( 10 ) )[0].RemoveBegin( 14 ) );
				index += lenArray[i];
			}

			try
			{
				return OperateResult.CreateSuccessResult( GetFanucDouble( result.ToArray( ), 0, length ) );
			}
			catch(Exception ex)
			{
				return new OperateResult<double[]>( ex.Message + " Source:" + result.ToArray( ).ToHexString( ' ' ) );
			}
		}

		/// <summary>
		/// 写宏变量，需要指定地址及写入的数据<br />
		/// Write macro variable, need to specify the address and write data
		/// </summary>
		/// <param name="number">地址</param>
		/// <param name="values">数据值</param>
		/// <returns>是否成功</returns>
		[HslMqttApi( Description = "Write macro variable, need to specify the address and write data" )]
		public OperateResult WriteSystemMacroValue( int number, double[] values )
		{
			OperateResult<byte[]> read = ReadFromCoreServer( BuildReadArray(
				BuildWriteSingle( 0x16, number, number + values.Length - 1, 0, 0, values )
				) );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<string, int>( read );

			List<byte[]> result = ExtraContentArray( read.Content.RemoveBegin( 10 ) );
			if (ByteTransform.TransUInt16( result[0], 6 ) == 0)
			{
				return OperateResult.CreateSuccessResult( );
			}
			else
			{
				return new OperateResult( ByteTransform.TransUInt16( result[0], 6 ), "Unknown Error" );
			}
		}

		/// <summary>
		/// 根据刀具号写入长度形状补偿，刀具号为1-24<br />
		/// Write length shape compensation according to the tool number, the tool number is 1-24
		/// </summary>
		/// <param name="cutter">刀具号，范围为1-24</param>
		/// <param name="offset">补偿值</param>
		/// <returns>是否写入成功</returns>
		[HslMqttApi( Description = "Write length shape compensation according to the tool number, the tool number is 1-24" )]
		public OperateResult WriteCutterLengthShapeOffset( int cutter, double offset ) => WriteSystemMacroValue( 11000 + cutter, new double[] { offset } );

		/// <summary>
		/// 根据刀具号写入长度磨损补偿，刀具号为1-24<br />
		/// Write length wear compensation according to the tool number, the tool number is 1-24
		/// </summary>
		/// <param name="cutter">刀具号，范围为1-24</param>
		/// <param name="offset">补偿值</param>
		/// <returns>是否写入成功</returns>
		[HslMqttApi( Description = "Write length wear compensation according to the tool number, the tool number is 1-24" )]
		public OperateResult WriteCutterLengthWearOffset( int cutter, double offset ) => WriteSystemMacroValue( 10000 + cutter, new double[] { offset } );

		/// <summary>
		/// 根据刀具号写入半径形状补偿，刀具号为1-24<br />
		/// Write radius shape compensation according to the tool number, the tool number is 1-24
		/// </summary>
		/// <param name="cutter">刀具号，范围为1-24</param>
		/// <param name="offset">补偿值</param>
		/// <returns>是否写入成功</returns>
		[HslMqttApi( Description = "Write radius shape compensation according to the tool number, the tool number is 1-24" )]
		public OperateResult WriteCutterRadiusShapeOffset( int cutter, double offset ) => WriteSystemMacroValue( 13000 + cutter, new double[] { offset } );

		/// <summary>
		/// 根据刀具号写入半径磨损补偿，刀具号为1-24<br />
		/// Write radius wear compensation according to the tool number, the tool number is 1-24
		/// </summary>
		/// <param name="cutter">刀具号，范围为1-24</param>
		/// <param name="offset">补偿值</param>
		/// <returns>是否写入成功</returns>
		[HslMqttApi( Description = "Write radius wear compensation according to the tool number, the tool number is 1-24" )]
		public OperateResult WriteCutterRadiusWearOffset( int cutter, double offset ) => WriteSystemMacroValue( 12000 + cutter, new double[] { offset } );

		/// <summary>
		/// 读取伺服负载<br />
		/// Read servo load
		/// </summary>
		/// <returns>轴负载</returns>
		[HslMqttApi( Description = "Read servo load" )]
		public OperateResult<double[]> ReadFanucAxisLoad( )
		{
			OperateResult<byte[]> read = ReadFromCoreServer( BuildReadArray(
				BuildReadSingle( 0xA4, 2, 0, 0, 0, 0 ),
				BuildReadSingle( 0x89, 0, 0, 0, 0, 0 ),
				BuildReadSingle( 0x56, 1, 0, 0, 0, 0 ),
				BuildReadSingle( 0xA4, 2, 0, 0, 0, 0 )
				) );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<double[]>( read );

			List<byte[]> result = ExtraContentArray( read.Content.RemoveBegin( 10 ) );
			int length = ByteTransform.TransUInt16( result[0], 14 );

			return OperateResult.CreateSuccessResult( GetFanucDouble( result[2], 14, length ) );
		}

		/// <summary>
		/// 读取机床的坐标，包括机械坐标，绝对坐标，相对坐标<br />
		/// Read the coordinates of the machine tool, including mechanical coordinates, absolute coordinates, and relative coordinates
		/// </summary>
		/// <returns>数控机床的坐标信息，包括机械坐标，绝对坐标，相对坐标</returns>
		[HslMqttApi( Description = "Read the coordinates of the machine tool, including mechanical coordinates, absolute coordinates, and relative coordinates" )]
		public OperateResult<SysAllCoors> ReadSysAllCoors( )
		{
			OperateResult<byte[]> read = ReadFromCoreServer( BuildReadArray(
				BuildReadSingle( 0xA4, 0, 0, 0, 0, 0 ),
				BuildReadSingle( 0x89, -1, 0, 0, 0, 0 ),
				BuildReadSingle( 0x88, 1, 0, 0, 0, 0 ),
				BuildReadSingle( 0x88, 2, 0, 0, 0, 0 ),
				BuildReadSingle( 0xA3, 0, -1, 0, 0, 0 ),
				BuildReadSingle( 0x26, 0, -1, 0, 0, 0 ),
				BuildReadSingle( 0x26, 1, -1, 0, 0, 0 ),
				BuildReadSingle( 0x26, 2, -1, 0, 0, 0 ),
				BuildReadSingle( 0x26, 3, -1, 0, 0, 0 ),
				BuildReadSingle( 0xA4, 0, 0, 0, 0, 0 )
				) );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<SysAllCoors>( read );

			List<byte[]> result = ExtraContentArray( read.Content.RemoveBegin( 10 ) );
			int length = ByteTransform.TransUInt16( result[0], 14 );

			SysAllCoors allCoors = new SysAllCoors( );

			allCoors.Absolute = GetFanucDouble( result[5], 14, length );
			allCoors.Machine = GetFanucDouble( result[6], 14, length );
			allCoors.Relative = GetFanucDouble( result[7], 14, length );

			return OperateResult.CreateSuccessResult( allCoors );
		}

		/// <summary>
		/// 读取报警信息<br />
		/// Read alarm information
		/// </summary>
		/// <returns>机床的当前的所有的报警信息</returns>
		[HslMqttApi( Description = "Read alarm information" )]
		public OperateResult<SysAlarm[]> ReadSystemAlarm( )
		{
			OperateResult<byte[]> read = ReadFromCoreServer( BuildReadArray(
				BuildReadSingle( 0x23, -1, 10, 2, 64, 0 )
				) );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<SysAlarm[]>( read );

			List<byte[]> result = ExtraContentArray( read.Content.RemoveBegin( 10 ) );
			if (ByteTransform.TransUInt16( result[0], 12 ) > 0)
			{
				int length = ByteTransform.TransUInt16( result[0], 12 ) / 80;
				SysAlarm[] alarms = new SysAlarm[length];
				for (int i = 0; i < alarms.Length; i++)
				{
					alarms[i] = new SysAlarm( );
					alarms[i].AlarmId = ByteTransform.TransInt32( result[0], 14 + 80 * i );
					alarms[i].Type = ByteTransform.TransInt16( result[0], 20 + 80 * i );
					alarms[i].Axis = ByteTransform.TransInt16( result[0], 24 + 80 * i );

					ushort msgLength = ByteTransform.TransUInt16( result[0], 28 + 80 * i );
					alarms[i].Message = this.encoding.GetString( result[0], 30 + 80 * i, msgLength );
				}
				return OperateResult.CreateSuccessResult( alarms );
			}
			else
				return OperateResult.CreateSuccessResult( new SysAlarm[0] );
		}

		/// <summary>
		/// 读取fanuc机床的时间，0是开机时间，1是运行时间，2是切割时间，3是循环时间，4是空闲时间，返回秒为单位的信息<br />
		/// Read the time of the fanuc machine tool, 0 is the boot time, 1 is the running time, 2 is the cutting time, 
		/// 3 is the cycle time, 4 is the idle time, and returns the information in seconds.
		/// </summary>
		/// <param name="timeType">读取的时间类型</param>
		/// <returns>秒为单位的结果</returns>
		[HslMqttApi( Description = "Read the time of the fanuc machine tool, 0 is the boot time, 1 is the running time, 2 is the cutting time, 3 is the cycle time, 4 is the idle time, and returns the information in seconds." )]
		public OperateResult<long> ReadTimeData( int timeType )
		{
			OperateResult<byte[]> read = ReadFromCoreServer( BuildReadArray(
				BuildReadSingle( 0x0120, timeType, 0, 0, 0, 0 )
				) );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<long>( read );

			List<byte[]> result = ExtraContentArray( read.Content.RemoveBegin( 10 ) );
			int millisecond = ByteTransform.TransInt32( result[0], 18 );
			long munite = ByteTransform.TransInt32( result[0], 14 );

			if (millisecond < 0 || millisecond > 60000)
			{
				millisecond = BitConverter.ToInt32( result[0], 18 );
				munite = BitConverter.ToInt32( result[0], 14 );
			}
			
			long seconds = millisecond / 1000;

			return OperateResult.CreateSuccessResult( munite * 60 + seconds );
		}

		/// <summary>
		/// 读取报警状态信息<br />
		/// Read alarm status information
		/// </summary>
		/// <returns>报警状态数据</returns>
		[HslMqttApi( Description = "Read alarm status information" )]
		public OperateResult<int> ReadAlarmStatus( )
		{
			OperateResult<byte[]> read = ReadFromCoreServer( BuildReadArray(
				BuildReadSingle( 0x1A, 0, 0, 0, 0, 0 )
				) );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<int>( read );

			List<byte[]> result = ExtraContentArray( read.Content.RemoveBegin( 10 ) );
			return OperateResult.CreateSuccessResult( (int)ByteTransform.TransUInt16( result[0], 16 ) );
		}

		/// <summary>
		/// 读取系统的基本信息状态，工作模式，运行状态，是否急停等等操作<br />
		/// Read the basic information status of the system, working mode, running status, emergency stop, etc.
		/// </summary>
		/// <returns>结果信息数据</returns>
		[HslMqttApi( Description = "Read the basic information status of the system, working mode, running status, emergency stop, etc." )]
		public OperateResult<SysStatusInfo> ReadSysStatusInfo( )
		{
			OperateResult<byte[]> read = ReadFromCoreServer( BuildReadArray(
				BuildReadSingle( 0x19, 0, 0, 0, 0, 0 ),
				BuildReadSingle( 0xE1, 0, 0, 0, 0, 0 ),
				BuildReadSingle( 0x98, 0, 0, 0, 0, 0 )
				) );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<SysStatusInfo>( read );

			List<byte[]> result = ExtraContentArray( read.Content.RemoveBegin( 10 ) );
			SysStatusInfo statusInfo = new SysStatusInfo( );
			statusInfo.Dummy = ByteTransform.TransInt16( result[1], 14 );
			statusInfo.TMMode = result[2].Length >= 16 ? ByteTransform.TransInt16( result[2], 14 ) : (short)0;
			statusInfo.WorkMode = (CNCWorkMode)ByteTransform.TransInt16( result[0], 14 );
			statusInfo.RunStatus = (CNCRunStatus)ByteTransform.TransInt16( result[0], 16 );
			statusInfo.Motion = ByteTransform.TransInt16( result[0], 18 );
			statusInfo.MSTB = ByteTransform.TransInt16( result[0], 20 );
			statusInfo.Emergency = ByteTransform.TransInt16( result[0], 22 );
			statusInfo.Alarm = ByteTransform.TransInt16( result[0], 24 );
			statusInfo.Edit = ByteTransform.TransInt16( result[0], 26 );

			return OperateResult.CreateSuccessResult( statusInfo );
		}

		/// <summary>
		/// 读取设备的程序列表<br />
		/// Read the program list of the device
		/// </summary>
		/// <returns>读取结果信息</returns>
		[HslMqttApi( Description = "Read the program list of the device" )]
		public OperateResult<int[]> ReadProgramList( )
		{
			OperateResult<byte[]> read = ReadFromCoreServer( BuildReadArray(
				BuildReadSingle( 0x06, 0x01, 0x13, 0, 0, 0 )
				) );
			OperateResult<byte[]> check = ReadFromCoreServer( BuildReadArray(
				BuildReadSingle( 0x06, 0x1A0B, 0x13, 0, 0, 0 )
				) );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<int[]>( read );
			if (!check.IsSuccess) return OperateResult.CreateFailedResult<int[]>( read );

			List<byte[]> result = ExtraContentArray( read.Content.RemoveBegin( 10 ) );
			int length = (result[0].Length - 14) / 72;
			int[] programs = new int[length];
			for (int i = 0; i < length; i++)
			{
				programs[i] = ByteTransform.TransInt32( result[0], 14 + 72 * i );
			}
			return OperateResult.CreateSuccessResult( programs );
		}

		/// <summary>
		/// 读取当前的刀具补偿信息<br />
		/// Read current tool compensation information
		/// </summary>
		/// <param name="cutterNumber">刀具数量</param>
		/// <returns>结果内容</returns>
		[HslMqttApi( Description = "Read current tool compensation information" )]
		public OperateResult<CutterInfo[]> ReadCutterInfos( int cutterNumber = 24 )
		{
			OperateResult<byte[]> read1 = ReadFromCoreServer( BuildReadArray( BuildReadSingle( 0x08, 1, cutterNumber, 0, 0, 0 ) ) );
			if (!read1.IsSuccess) return OperateResult.CreateFailedResult<CutterInfo[]>( read1 );

			OperateResult<byte[]> read2 = ReadFromCoreServer( BuildReadArray( BuildReadSingle( 0x08, 1, cutterNumber, 1, 0, 0 ) ) );
			if (!read2.IsSuccess) return OperateResult.CreateFailedResult<CutterInfo[]>( read2 );

			OperateResult<byte[]> read3 = ReadFromCoreServer( BuildReadArray( BuildReadSingle( 0x08, 1, cutterNumber, 2, 0, 0 ) ) );
			if (!read3.IsSuccess) return OperateResult.CreateFailedResult<CutterInfo[]>( read3 );

			OperateResult<byte[]> read4 = ReadFromCoreServer( BuildReadArray( BuildReadSingle( 0x08, 1, cutterNumber, 3, 0, 0 ) ) );
			if (!read4.IsSuccess) return OperateResult.CreateFailedResult<CutterInfo[]>( read4 );

			return ExtraCutterInfos( read1.Content, read2.Content, read3.Content, read4.Content, cutterNumber );
		}

		/// <summary>
		/// 读取当前的正在使用的刀具号<br />
		/// Read the tool number currently in use
		/// </summary>
		/// <returns>刀具号信息</returns>
		[HslMqttApi( Description = "Read the tool number currently in use" )]
		public OperateResult<int> ReadCutterNumber( )
		{
			OperateResult<double[]> read = ReadSystemMacroValue( 4120, 1 );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<int>( read );

			return OperateResult.CreateSuccessResult( Convert.ToInt32( read.Content[0] ) );
		}

		/// <summary>
		/// 读取R数据，需要传入起始地址和结束地址，返回byte[]数据信息<br />
		/// To read R data, you need to pass in the start address and end address, and return byte[] data information
		/// </summary>
		/// <param name="start">起始地址</param>
		/// <param name="end">结束地址</param>
		/// <returns>读取结果</returns>
		[HslMqttApi( Description = "To read R data, you need to pass in the start address and end address, and return byte[] data information" )]
		public OperateResult<byte[]> ReadRData( int start, int end )
		{
			OperateResult<byte[]> read1 = ReadFromCoreServer( 
				BuildReadArray( BuildReadMulti( 0x02, 0x8001, start, end, 0x05, 0, 0 ) ) );
			if (!read1.IsSuccess) return read1;

			List<byte[]> result = ExtraContentArray( read1.Content.RemoveBegin( 10 ) );
			int length = this.ByteTransform.TransUInt16( result[0], 12 );
			return OperateResult.CreateSuccessResult( result[0].SelectMiddle( 14, length ) );
		}

		/// <summary>
		/// 读取工件尺寸<br />
		/// Read workpiece size
		/// </summary>
		/// <returns>结果数据信息</returns>
		[HslMqttApi( Description = "Read workpiece size" )]
		public OperateResult<double[]> ReadDeviceWorkPiecesSize( ) => ReadSystemMacroValue( 601, 20 );

		/// <summary>
		/// 读取当前的程序内容，只能读取程序的片段，返回程序内容。<br />
		/// Read the current program content, only read the program fragments, and return the program content.
		/// </summary>
		/// <returns>程序内容</returns>
		[HslMqttApi( Description = "Read the current program content, only read the program fragments, and return the program content." )]
		public OperateResult<string> ReadCurrentProgram( )
		{
			OperateResult<byte[]> read = ReadFromCoreServer(
				BuildReadArray( BuildReadSingle( 0x20, 0x0594, 0x00, 0x00, 0x00, 0x00 ) ) );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<string>( read );

			byte[] result = ExtraContentArray( read.Content.RemoveBegin( 10 ) )[0];
			return OperateResult.CreateSuccessResult( Encoding.ASCII.GetString( result, 18, result.Length - 18 ) );
		}

		/// <summary>
		/// 设置指定的程序号为当前的主程序，如果程序号不存在，返回错误信息<br />
		/// Set the specified program number as the current main program, if the program number does not exist, an error message will be returned
		/// </summary>
		/// <param name="programNum">程序号信息</param>
		/// <returns>是否设置成功</returns>
		[HslMqttApi( Description = "Set the specified program number as the current main program, if the program number does not exist, an error message will be returned." )]
		public OperateResult SetCurrentProgram( ushort programNum )
		{
			OperateResult<byte[]> read = ReadFromCoreServer(
				BuildReadArray( BuildReadSingle( 0x03, programNum, 0, 0, 0, 0 ) ) );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<int, string>( read );

			byte[] result = ExtraContentArray( read.Content.RemoveBegin( 10 ) )[0];
			short err = this.ByteTransform.TransInt16( result, 6 );

			if (err == 0) return OperateResult.CreateSuccessResult( );
			else return new OperateResult( err, StringResources.Language.UnknownError );
		}

		/// <summary>
		/// 启动加工程序<br />
		/// Start the processing program
		/// </summary>
		/// <returns>是否启动成功</returns>
		[HslMqttApi( Description = "Start the processing program" )]
		public OperateResult StartProcessing( )
		{
			OperateResult<byte[]> read = ReadFromCoreServer(
				BuildReadArray( BuildReadSingle( 0x01, 0, 0, 0, 0, 0 ) ) );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<int, string>( read );

			byte[] result = ExtraContentArray( read.Content.RemoveBegin( 10 ) )[0];
			short err = this.ByteTransform.TransInt16( result, 6 );

			if (err == 0) return OperateResult.CreateSuccessResult( );
			else return new OperateResult( err, StringResources.Language.UnknownError );
		}

		/// <summary>
		/// <b>[商业授权]</b> 将指定文件的NC加工程序，下载到数控机床里，返回是否下载成功<br />
		/// <b>[Authorization]</b> Download the NC machining program of the specified file to the CNC machine tool, and return whether the download is successful
		/// </summary>
		/// <remarks>
		/// 程序文件的内容必须%开始，%结束，下面是一个非常简单的例子：<br />
		/// %<br />
		/// O0006<br />
		/// G90G10L2P1<br />
		/// M30<br />
		/// %
		/// </remarks>
		/// <param name="file">程序文件的路径</param>
		/// <returns>是否下载成功</returns>
		[HslMqttApi( Description = "Download the NC machining program of the specified file to the CNC machine tool, and return whether the download is successful" )]
		public OperateResult WriteProgramFile( string file )
		{
			string content = File.ReadAllText( file );
			return WriteProgramContent( content );
		}

		/// <summary>
		/// <b>[商业授权]</b> 将指定程序内容的NC加工程序，写入到数控机床里，返回是否下载成功<br />
		/// <b>[Authorization]</b> Download the NC machining program to the CNC machine tool, and return whether the download is successful
		/// </summary>
		/// <remarks>
		/// 程序文件的内容必须%开始，%结束，下面是一个非常简单的例子：<br />
		/// %<br />
		/// O0006<br />
		/// G90G10L2P1<br />
		/// M30<br />
		/// %
		/// </remarks>
		/// <param name="program">程序内容信息</param>
		/// <param name="everyWriteSize">每次写入的长度信息</param>
		/// <returns>是否下载成功</returns>
		[HslMqttApi( Description = "Download the NC machining program to the CNC machine tool, and return whether the download is successful" )]
		public OperateResult WriteProgramContent( string program, int everyWriteSize = 512 )
		{
			if (!Authorization.asdniasnfaksndiqwhawfskhfaiw( )) return new OperateResult( StringResources.Language.InsufficientPrivileges );

			OperateResult<Socket> socket = CreateSocketAndConnect( IpAddress, Port, ConnectTimeOut );
			if (!socket.IsSuccess) return socket.ConvertFailed<int>( );

			OperateResult<byte[]> ini1 = ReadFromCoreServer( socket.Content, "a0 a0 a0 a0 00 01 01 01 00 02 00 01".ToHexBytes( ) );
			if (!ini1.IsSuccess) return ini1;

			OperateResult<byte[]> read1 = ReadFromCoreServer( socket.Content, BulidWriteProgramFilePre( ) );
			if (!read1.IsSuccess) return read1;

			List<byte[]> contents = BulidWriteProgram( Encoding.ASCII.GetBytes( program ), everyWriteSize );
			for (int i = 0; i < contents.Count; i++)
			{
				OperateResult<byte[]> read2 = ReadFromCoreServer( socket.Content, contents[i], false );
				if (!read2.IsSuccess) return read2;
			}

			OperateResult<byte[]> read3 = ReadFromCoreServer( socket.Content, new byte[] { 0xa0, 0xa0, 0xa0, 0xa0, 0x00, 0x01, 0x13, 0x01, 0x00, 0x00 } );
			if (!read3.IsSuccess) return read3;

			socket.Content?.Close( );
			if (read3.Content.Length >= 14)
			{
				int err = this.ByteTransform.TransInt16( read3.Content, 12 );
				if (err != 0) return new OperateResult<string>( err, StringResources.Language.UnknownError );
			}

			return OperateResult.CreateSuccessResult( );
		}

		/// <summary>
		/// <b>[商业授权]</b> 读取指定程序号的程序内容<br />
		/// <b>[Authorization]</b> Read the program content of the specified program number
		/// </summary>
		/// <param name="program">程序号</param>
		/// <returns>程序内容</returns>
		[HslMqttApi( Description = "Read the program content of the specified program number" )]
		public OperateResult<string> ReadProgram( int program )
		{
			if (!Authorization.asdniasnfaksndiqwhawfskhfaiw( )) return new OperateResult<string>( StringResources.Language.InsufficientPrivileges );

			OperateResult<Socket> socket = CreateSocketAndConnect( IpAddress, Port, ConnectTimeOut );
			if (!socket.IsSuccess) return socket.ConvertFailed<string>( );

			OperateResult<byte[]> ini1 = ReadFromCoreServer( socket.Content, "a0 a0 a0 a0 00 01 01 01 00 02 00 01".ToHexBytes( ) );
			if (!ini1.IsSuccess) return OperateResult.CreateFailedResult<string>( ini1 );

			OperateResult<byte[]> read1 = ReadFromCoreServer( socket.Content, BuildReadProgramPre( program ) );
			if (!read1.IsSuccess) return OperateResult.CreateFailedResult<string>( read1 );

			// 检测错误信息
			int err = read1.Content[12] * 256 + read1.Content[13];
			if (err != 0)
			{
				socket.Content?.Close( );
				return new OperateResult<string>( err, StringResources.Language.UnknownError );
			}

			StringBuilder sb = new StringBuilder( );
			while (true)
			{
				OperateResult<byte[]> read2 = ReadFromCoreServer( socket.Content, null );
				if (!read2.IsSuccess) return OperateResult.CreateFailedResult<string>( read2 );

				if (read2.Content[6] == 0x16)
					sb.Append( Encoding.ASCII.GetString( read2.Content, 10, read2.Content.Length - 10 ) );
				else if (read2.Content[6] == 0x17)
					break;
			}

			OperateResult send = Send( socket.Content, new byte[] { 0xa0, 0xa0, 0xa0, 0xa0, 0x00, 0x01, 0x17, 0x02, 0x00, 0x00 } );
			if (!send.IsSuccess) return OperateResult.CreateFailedResult<string>( send );

			socket.Content?.Close( );
			return OperateResult.CreateSuccessResult( sb.ToString( ) );
		}

		/// <summary>
		/// 根据指定的程序号信息，删除当前的程序信息<br />
		/// According to the designated program number information, delete the current program information
		/// </summary>
		/// <param name="program">程序号</param>
		/// <returns>是否删除成功</returns>
		[HslMqttApi( Description = "According to the designated program number information, delete the current program information" )]
		public OperateResult DeleteProgram( int program )
		{
			OperateResult<byte[]> read = ReadFromCoreServer(
				BuildReadArray( BuildReadSingle( 0x05, program, 0, 0, 0, 0 ) ) );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<int, string>( read );

			byte[] result = ExtraContentArray( read.Content.RemoveBegin( 10 ) )[0];
			short err = this.ByteTransform.TransInt16( result, 6 );

			if (err == 0) return OperateResult.CreateSuccessResult( );
			else return new OperateResult( err, StringResources.Language.UnknownError );
		}

		/// <summary>
		/// 读取当前程序的前台路径<br />
		/// Read the foreground path of the current program
		/// </summary>
		/// <returns>程序的路径信息</returns>
		[HslMqttApi( Description = "Read the foreground path of the current program" )]
		public OperateResult<string> ReadCurrentForegroundDir( )
		{
			OperateResult<byte[]> read = ReadFromCoreServer( BuildReadArray( BuildReadSingle( 0xB0, 1, 0, 0, 0, 0 ) ) );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<string>( read );

			List<byte[]> result = ExtraContentArray( read.Content.RemoveBegin( 10 ) );
			int index = 0;
			for (int i = 14; i < result[0].Length; i++)
			{
				if (result[0][i] == 0x00)
				{
					index = i;
					break;
				}
			}
			if (index == 0) index = result[0].Length;
			return OperateResult.CreateSuccessResult( Encoding.ASCII.GetString( result[0], 14, index - 14 ) );
		}

		/// <summary>
		/// 设置指定路径为当前路径<br />
		/// Set the specified path as the current path
		/// </summary>
		/// <param name="programName">程序名</param>
		/// <returns>结果信息</returns>
		[HslMqttApi( Description = "Set the specified path as the current path" )]
		public OperateResult SetDeviceProgsCurr( string programName )
		{
			OperateResult<string> path = ReadCurrentForegroundDir( );
			if (!path.IsSuccess) return path;

			byte[] buffer = new byte[256];
			Encoding.ASCII.GetBytes( path.Content + programName ).CopyTo( buffer, 0 );

			OperateResult<byte[]> read = ReadFromCoreServer( BuildReadArray( BuildWriteSingle( 0xBA, 0, 0, 0, 0, buffer ) ) );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<string>( read );

			List<byte[]> result = ExtraContentArray( read.Content.RemoveBegin( 10 ) );
			int status = result[0][10] * 256 + result[0][11];

			if (status == 0) return OperateResult.CreateSuccessResult( );
			else return new OperateResult( status, StringResources.Language.UnknownError );
		}

		/// <summary>
		/// 读取机床的当前时间信息<br />
		/// Read the current time information of the machine tool
		/// </summary>
		/// <returns>时间信息</returns>
		[HslMqttApi( Description = "Read the current time information of the machine tool" )]
		public OperateResult<DateTime> ReadCurrentDateTime( )
		{
			OperateResult<double> read1 = ReadSystemMacroValue( 3011 );
			if (!read1.IsSuccess) return OperateResult.CreateFailedResult<DateTime>( read1 );

			OperateResult<double> read2 = ReadSystemMacroValue( 3012 );
			if (!read2.IsSuccess) return OperateResult.CreateFailedResult<DateTime>( read2 );

			string date = Convert.ToInt32( read1.Content ).ToString( );
			string time = Convert.ToInt32( read2.Content ).ToString( ).PadLeft( 6, '0' );

			return OperateResult.CreateSuccessResult( new DateTime(
				int.Parse( date.Substring( 0, 4 ) ), int.Parse( date.Substring( 4, 2 ) ), int.Parse( date.Substring( 6 ) ),
				int.Parse( time.Substring( 0, 2 ) ), int.Parse( time.Substring( 2, 2 ) ), int.Parse( time.Substring( 4 ) ) ) );
		}

		/// <summary>
		/// 读取当前的已加工的零件数量<br />
		/// Read the current number of processed parts
		/// </summary>
		/// <returns>已经加工的零件数量</returns>
		[HslMqttApi( Description = "Read the current number of processed parts" )]
		public OperateResult<int> ReadCurrentProduceCount( )
		{
			OperateResult<double> read = ReadSystemMacroValue( 3901 );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<int>( read );

			return OperateResult.CreateSuccessResult( Convert.ToInt32( read.Content ) );
		}

		/// <summary>
		/// 读取期望的加工的零件数量<br />
		/// Read the expected number of processed parts
		/// </summary>
		/// <returns>期望的加工的零件数量</returns>
		[HslMqttApi( Description = "Read the expected number of processed parts" )]
		public OperateResult<int> ReadExpectProduceCount( )
		{
			OperateResult<double> read = ReadSystemMacroValue( 3902 );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<int>( read );

			return OperateResult.CreateSuccessResult( Convert.ToInt32( read.Content ) );
		}

		#endregion

		#region Async Read Write Support
#if !NET20 && !NET35
		/// <inheritdoc cref="ReadSpindleSpeedAndFeedRate"/>
		public async Task<OperateResult<double, double>> ReadSpindleSpeedAndFeedRateAsync( )
		{
			OperateResult<byte[]> read = await ReadFromCoreServerAsync( BuildReadArray(
				BuildReadSingle( 0xA4, 3, 0, 0, 0, 0 ),
				BuildReadSingle( 0x8A, 1, 0, 0, 0, 0 ),
				BuildReadSingle( 0x88, 3, 0, 0, 0, 0 ),
				BuildReadSingle( 0x88, 4, 0, 0, 0, 0 ),
				BuildReadSingle( 0x24, 0, 0, 0, 0, 0 ),
				BuildReadSingle( 0x25, 0, 0, 0, 0, 0 ),
				BuildReadSingle( 0xA4, 3, 0, 0, 0, 0 )
				) );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<double, double>( read );

			List<byte[]> result = ExtraContentArray( read.Content.RemoveBegin( 10 ) );
			return OperateResult.CreateSuccessResult( GetFanucDouble( result[5], 14 ), GetFanucDouble( result[4], 14 ) );
		}

		/// <inheritdoc cref="ReadSystemProgramCurrent"/>
		public async Task<OperateResult<string, int>> ReadSystemProgramCurrentAsync( )
		{
			OperateResult<byte[]> read = await ReadFromCoreServerAsync( BuildReadArray(
				BuildReadSingle( 0xCF, 0, 0, 0, 0, 0 )
				) );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<string, int>( read );

			List<byte[]> result = ExtraContentArray( read.Content.RemoveBegin( 10 ) );
			int number = ByteTransform.TransInt32( result[0], 14 );
			string name = this.encoding.GetString( result[0].SelectMiddle( 18, 36 ) ).TrimEnd( '\0' );
			return OperateResult.CreateSuccessResult( name, number );
		}

		/// <inheritdoc cref="ReadLanguage"/>
		public async Task<OperateResult<ushort>> ReadLanguageAsync( )
		{
			OperateResult<byte[]> read = await ReadFromCoreServerAsync( BuildReadArray(
				BuildReadSingle( 0x8D, 0x0CD1, 0x0CD1, 0, 0, 0 )
				) );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<ushort>( read );
			List<byte[]> result = ExtraContentArray( read.Content.RemoveBegin( 10 ) );

			ushort code = ByteTransform.TransUInt16( result[0], 24 );
			ChangeTextEncoding( code );
			return OperateResult.CreateSuccessResult( code );
		}
		/// <inheritdoc cref="ReadSystemMacroValue(int)"/>
		public async Task<OperateResult<double>> ReadSystemMacroValueAsync( int number )
		{
			return ByteTransformHelper.GetResultFromArray( await ReadSystemMacroValueAsync( number, 1 ) );
		}

		/// <inheritdoc cref="ReadSystemMacroValue(int, int)"/>
		public async Task<OperateResult<double[]>> ReadSystemMacroValueAsync( int number, int length )
		{
			// 拆分5个5个读
			int[] lenArray = SoftBasic.SplitIntegerToArray( length, 5 );
			int index = number;
			List<byte> result = new List<byte>( );

			for (int i = 0; i < lenArray.Length; i++)
			{
				OperateResult<byte[]> read = await ReadFromCoreServerAsync( BuildReadArray(
					BuildReadSingle( 0x15, index, index + lenArray[i] - 1, 0, 0, 0 ) ) );
				if (!read.IsSuccess) return OperateResult.CreateFailedResult<double[]>( read );

				result.AddRange( ExtraContentArray( read.Content.RemoveBegin( 10 ) )[0].RemoveBegin( 14 ) );
				index += lenArray[i];
			}

			try
			{
				return OperateResult.CreateSuccessResult( GetFanucDouble( result.ToArray( ), 0, length ) );
			}
			catch (Exception ex)
			{
				return new OperateResult<double[]>( ex.Message + " Source:" + result.ToArray( ).ToHexString( ' ' ) );
			}
		}

		/// <inheritdoc cref="ReadCutterNumber"/>
		public async Task<OperateResult<int>> ReadCutterNumberAsync( )
		{
			OperateResult<double[]> read = await ReadSystemMacroValueAsync( 4120, 1 );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<int>( read );

			return OperateResult.CreateSuccessResult( Convert.ToInt32( read.Content[0] ) );
		}

		/// <inheritdoc cref="WriteSystemMacroValue(int, double[])"/>
		public async Task<OperateResult> WriteSystemMacroValueAsync( int number, double[] values )
		{
			OperateResult<byte[]> read = await ReadFromCoreServerAsync( BuildReadArray(
				BuildWriteSingle( 0x16, number, number + values.Length - 1, 0, 0, values )
				) );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<string, int>( read );

			List<byte[]> result = ExtraContentArray( read.Content.RemoveBegin( 10 ) );
			if (ByteTransform.TransUInt16( result[0], 6 ) == 0)
			{
				return OperateResult.CreateSuccessResult( );
			}
			else
			{
				return new OperateResult( ByteTransform.TransUInt16( result[0], 6 ), "Unknown Error" );
			}
		}

		/// <inheritdoc cref="WriteCutterLengthShapeOffset(int, double)"/>
		public async Task<OperateResult> WriteCutterLengthSharpOffsetAsync( int cutter, double offset ) => await WriteSystemMacroValueAsync( 11000 + cutter, new double[] { offset } );

		/// <inheritdoc cref="WriteCutterLengthWearOffset(int, double)"/>
		public async Task<OperateResult> WriteCutterLengthWearOffsetAsync( int cutter, double offset ) => await WriteSystemMacroValueAsync( 10000 + cutter, new double[] { offset } );

		/// <inheritdoc cref="WriteCutterRadiusShapeOffset(int, double)"/>
		public async Task<OperateResult> WriteCutterRadiusSharpOffsetAsync( int cutter, double offset ) => await WriteSystemMacroValueAsync( 13000 + cutter, new double[] { offset } );

		/// <inheritdoc cref="WriteCutterRadiusWearOffset(int, double)"/>
		public async Task<OperateResult> WriteCutterRadiusWearOffsetAsync( int cutter, double offset ) => await WriteSystemMacroValueAsync( 12000 + cutter, new double[] { offset } );

		/// <inheritdoc cref="ReadFanucAxisLoad"/>
		public async Task<OperateResult<double[]>> ReadFanucAxisLoadAsync( )
		{
			OperateResult<byte[]> read = await ReadFromCoreServerAsync( BuildReadArray(
				BuildReadSingle( 0xA4, 2, 0, 0, 0, 0 ),
				BuildReadSingle( 0x89, 0, 0, 0, 0, 0 ),
				BuildReadSingle( 0x56, 1, 0, 0, 0, 0 ),
				BuildReadSingle( 0xA4, 2, 0, 0, 0, 0 )
				) );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<double[]>( read );

			List<byte[]> result = ExtraContentArray( read.Content.RemoveBegin( 10 ) );
			int length = ByteTransform.TransUInt16( result[0], 14 );

			return OperateResult.CreateSuccessResult( GetFanucDouble( result[2], 14, length ) );
		}

		/// <inheritdoc cref="ReadSysAllCoors"/>
		public async Task<OperateResult<SysAllCoors>> ReadSysAllCoorsAsync( )
		{
			OperateResult<byte[]> read = await ReadFromCoreServerAsync( BuildReadArray(
				BuildReadSingle( 0xA4, 0, 0, 0, 0, 0 ),
				BuildReadSingle( 0x89, -1, 0, 0, 0, 0 ),
				BuildReadSingle( 0x88, 1, 0, 0, 0, 0 ),
				BuildReadSingle( 0x88, 2, 0, 0, 0, 0 ),
				BuildReadSingle( 0xA3, 0, -1, 0, 0, 0 ),
				BuildReadSingle( 0x26, 0, -1, 0, 0, 0 ),
				BuildReadSingle( 0x26, 1, -1, 0, 0, 0 ),
				BuildReadSingle( 0x26, 2, -1, 0, 0, 0 ),
				BuildReadSingle( 0x26, 3, -1, 0, 0, 0 ),
				BuildReadSingle( 0xA4, 0, 0, 0, 0, 0 )
				) );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<SysAllCoors>( read );

			List<byte[]> result = ExtraContentArray( read.Content.RemoveBegin( 10 ) );
			int length = ByteTransform.TransUInt16( result[0], 14 );

			SysAllCoors allCoors = new SysAllCoors( );

			allCoors.Absolute = GetFanucDouble( result[5], 14, length );
			allCoors.Machine  = GetFanucDouble( result[6], 14, length );
			allCoors.Relative = GetFanucDouble( result[7], 14, length );

			return OperateResult.CreateSuccessResult( allCoors );
		}

		/// <inheritdoc cref="ReadSystemAlarm"/>
		public async Task<OperateResult<SysAlarm[]>> ReadSystemAlarmAsync( )
		{
			OperateResult<byte[]> read = await ReadFromCoreServerAsync( BuildReadArray(
				BuildReadSingle( 0x23, -1, 10, 2, 64, 0 )
				) );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<SysAlarm[]>( read );

			List<byte[]> result = ExtraContentArray( read.Content.RemoveBegin( 10 ) );
			if (ByteTransform.TransUInt16( result[0], 12 ) > 0)
			{
				int length = ByteTransform.TransUInt16( result[0], 12 ) / 80;
				SysAlarm[] alarms = new SysAlarm[length];
				for (int i = 0; i < alarms.Length; i++)
				{
					alarms[i] = new SysAlarm( );
					alarms[i].AlarmId = ByteTransform.TransInt32( result[0], 14 + 80 * i );
					alarms[i].Type = ByteTransform.TransInt16( result[0], 20 + 80 * i );
					alarms[i].Axis = ByteTransform.TransInt16( result[0], 24 + 80 * i );

					ushort msgLength = ByteTransform.TransUInt16( result[0], 28 + 80 * i );
					alarms[i].Message = this.encoding.GetString( result[0], 30 + 80 * i, msgLength );
				}
				return OperateResult.CreateSuccessResult( alarms );
			}
			else
				return OperateResult.CreateSuccessResult( new SysAlarm[0] );
		}

		/// <inheritdoc cref="ReadTimeData(int)"/>
		public async Task<OperateResult<long>> ReadTimeDataAsync( int timeType )
		{
			OperateResult<byte[]> read = await ReadFromCoreServerAsync( BuildReadArray(
				BuildReadSingle( 0x0120, timeType, 0, 0, 0, 0 )
				) );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<long>( read );

			List<byte[]> result = ExtraContentArray( read.Content.RemoveBegin( 10 ) );
			int millisecond = ByteTransform.TransInt32( result[0], 18 );
			long munite = ByteTransform.TransInt32( result[0], 14 );

			if (millisecond < 0 || millisecond > 60000)
			{
				millisecond = BitConverter.ToInt32( result[0], 18 );
				munite = BitConverter.ToInt32( result[0], 14 );
			}

			long seconds = millisecond / 1000;

			return OperateResult.CreateSuccessResult( munite * 60 + seconds );
		}

		/// <inheritdoc cref="ReadAlarmStatus"/>
		public async Task<OperateResult<int>> ReadAlarmStatusAsync( )
		{
			OperateResult<byte[]> read = await ReadFromCoreServerAsync( BuildReadArray(
				BuildReadSingle( 0x1A, 0, 0, 0, 0, 0 )
				) );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<int>( read );

			List<byte[]> result = ExtraContentArray( read.Content.RemoveBegin( 10 ) );
			return OperateResult.CreateSuccessResult( (int)ByteTransform.TransUInt16( result[0], 16 ) );
		}

		/// <inheritdoc cref="ReadSysStatusInfo"/>
		public async Task<OperateResult<SysStatusInfo>> ReadSysStatusInfoAsync( )
		{
			OperateResult<byte[]> read = await ReadFromCoreServerAsync( BuildReadArray(
				BuildReadSingle( 0x19, 0, 0, 0, 0, 0 ),
				BuildReadSingle( 0xE1, 0, 0, 0, 0, 0 ),
				BuildReadSingle( 0x98, 0, 0, 0, 0, 0 )
				) );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<SysStatusInfo>( read );

			List<byte[]> result = ExtraContentArray( read.Content.RemoveBegin( 10 ) );
			SysStatusInfo statusInfo = new SysStatusInfo( );
			statusInfo.Dummy     = ByteTransform.TransInt16( result[1], 14 );
			statusInfo.TMMode    = result[2].Length >= 16 ? ByteTransform.TransInt16( result[2], 14 ) : (short)0;
			statusInfo.WorkMode  = (CNCWorkMode)ByteTransform.TransInt16( result[0], 14 );
			statusInfo.RunStatus = (CNCRunStatus)ByteTransform.TransInt16( result[0], 16 );
			statusInfo.Motion    = ByteTransform.TransInt16( result[0], 18 );
			statusInfo.MSTB      = ByteTransform.TransInt16( result[0], 20 );
			statusInfo.Emergency = ByteTransform.TransInt16( result[0], 22 );
			statusInfo.Alarm     = ByteTransform.TransInt16( result[0], 24 );
			statusInfo.Edit      = ByteTransform.TransInt16( result[0], 26 );

			return OperateResult.CreateSuccessResult( statusInfo );
		}

		/// <inheritdoc cref="ReadProgramList"/>
		public async Task<OperateResult<int[]>> ReadProgramListAsync( )
		{
			OperateResult<byte[]> read = await ReadFromCoreServerAsync( BuildReadArray(
				BuildReadSingle( 0x06, 0x01, 0x13, 0, 0, 0 )
				) );
			OperateResult<byte[]> check = await ReadFromCoreServerAsync( BuildReadArray(
				BuildReadSingle( 0x06, 0x1A0B, 0x13, 0, 0, 0 )
				) );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<int[]>( read );
			if (!check.IsSuccess) return OperateResult.CreateFailedResult<int[]>( read );

			List<byte[]> result = ExtraContentArray( read.Content.RemoveBegin( 10 ) );
			int length = (result[0].Length - 14) / 72;
			int[] programs = new int[length];
			for (int i = 0; i < length; i++)
			{
				programs[i] = ByteTransform.TransInt32( result[0], 14 + 72 * i );
			}
			return OperateResult.CreateSuccessResult( programs );
		}

		/// <inheritdoc cref="ReadCutterInfos(int)"/>
		public async Task<OperateResult<CutterInfo[]>> ReadCutterInfosAsync( int cutterNumber = 24 )
		{
			OperateResult<byte[]> read1 = await ReadFromCoreServerAsync( BuildReadArray( BuildReadSingle( 0x08, 1, cutterNumber, 0, 0, 0 ) ) );
			if (!read1.IsSuccess) return OperateResult.CreateFailedResult<CutterInfo[]>( read1 );

			OperateResult<byte[]> read2 = await ReadFromCoreServerAsync( BuildReadArray( BuildReadSingle( 0x08, 1, cutterNumber, 1, 0, 0 ) ) );
			if (!read2.IsSuccess) return OperateResult.CreateFailedResult<CutterInfo[]>( read2 );

			OperateResult<byte[]> read3 = await ReadFromCoreServerAsync( BuildReadArray( BuildReadSingle( 0x08, 1, cutterNumber, 2, 0, 0 ) ) );
			if (!read3.IsSuccess) return OperateResult.CreateFailedResult<CutterInfo[]>( read3 );

			OperateResult<byte[]> read4 = await ReadFromCoreServerAsync( BuildReadArray( BuildReadSingle( 0x08, 1, cutterNumber, 3, 0, 0 ) ) );
			if (!read4.IsSuccess) return OperateResult.CreateFailedResult<CutterInfo[]>( read4 );

			return ExtraCutterInfos( read1.Content, read2.Content, read3.Content, read4.Content, cutterNumber );
		}

		/// <inheritdoc cref="ReadRData(int, int)"/>
		public async Task<OperateResult<byte[]>> ReadRDataAsync( int start, int end )
		{
			OperateResult<byte[]> read1 = await ReadFromCoreServerAsync(
				BuildReadArray( BuildReadMulti( 0x02, 0x8001, start, end, 0x05, 0, 0 ) ) );
			if (!read1.IsSuccess) return read1;

			List<byte[]> result = ExtraContentArray( read1.Content.RemoveBegin( 10 ) );
			int length = this.ByteTransform.TransUInt16( result[0], 12 );
			return OperateResult.CreateSuccessResult( result[0].SelectMiddle( 14, length ) );
		}

		/// <inheritdoc cref="ReadDeviceWorkPiecesSize"/>
		public async Task<OperateResult<double[]>> ReadDeviceWorkPiecesSizeAsync( ) => await ReadSystemMacroValueAsync( 601, 20 );


		/// <inheritdoc cref="ReadCurrentForegroundDir"/>
		public async Task<OperateResult<string>> ReadCurrentForegroundDirAsync( )
		{
			OperateResult<byte[]> read = await ReadFromCoreServerAsync( BuildReadArray( BuildReadSingle( 0xB0, 1, 0, 0, 0, 0 ) ) );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<string>( read );

			List<byte[]> result = ExtraContentArray( read.Content.RemoveBegin( 10 ) );
			int index = 0;
			for (int i = 14; i < result[0].Length; i++)
			{
				if (result[0][i] == 0x00)
				{
					index = i;
					break;
				}
			}
			if (index == 0) index = result[0].Length;
			return OperateResult.CreateSuccessResult( Encoding.ASCII.GetString( result[0], 14, index - 14 ) );
		}

		/// <inheritdoc cref="SetDeviceProgsCurr(string)"/>
		public async Task<OperateResult> SetDeviceProgsCurrAsync( string programName )
		{
			OperateResult<string> path = await ReadCurrentForegroundDirAsync( );
			if (!path.IsSuccess) return path;

			byte[] buffer = new byte[256];
			Encoding.ASCII.GetBytes( path.Content + programName ).CopyTo( buffer, 0 );

			OperateResult<byte[]> read = await ReadFromCoreServerAsync( BuildReadArray( BuildWriteSingle( 0xBA, 0, 0, 0, 0, buffer ) ) );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<string>( read );

			List<byte[]> result = ExtraContentArray( read.Content.RemoveBegin( 10 ) );
			int status = result[0][10] * 256 + result[0][11];

			if (status == 0) return OperateResult.CreateSuccessResult( );
			else return new OperateResult( status, StringResources.Language.UnknownError );
		}

		/// <inheritdoc cref="ReadCurrentDateTime"/>
		public async Task<OperateResult<DateTime>> ReadCurrentDateTimeAsync( )
		{
			OperateResult<double> read1 = await ReadSystemMacroValueAsync( 3011 );
			if (!read1.IsSuccess) return OperateResult.CreateFailedResult<DateTime>( read1 );

			OperateResult<double> read2 = await ReadSystemMacroValueAsync( 3012 );
			if (!read2.IsSuccess) return OperateResult.CreateFailedResult<DateTime>( read2 );

			string date = Convert.ToInt32( read1.Content ).ToString( );
			string time = Convert.ToInt32( read2.Content ).ToString( ).PadLeft( 6, '0' );

			return OperateResult.CreateSuccessResult( new DateTime(
				int.Parse( date.Substring( 0, 4 ) ), int.Parse( date.Substring( 4, 2 ) ), int.Parse( date.Substring( 6 ) ),
				int.Parse( time.Substring( 0, 2 ) ), int.Parse( time.Substring( 2, 2 ) ), int.Parse( time.Substring( 4 ) ) ) );
		}

		/// <inheritdoc cref="ReadCurrentProduceCount"/>
		public async Task<OperateResult<int>> ReadCurrentProduceCountAsync( )
		{
			OperateResult<double> read = await ReadSystemMacroValueAsync( 3901 );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<int>( read );

			return OperateResult.CreateSuccessResult( Convert.ToInt32( read.Content ) );
		}

		/// <inheritdoc cref="ReadExpectProduceCount"/>
		public async Task<OperateResult<int>> ReadExpectProduceCountAsync( )
		{
			OperateResult<double> read = await ReadSystemMacroValueAsync( 3902 );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<int>( read );

			return OperateResult.CreateSuccessResult( Convert.ToInt32( read.Content ) );
		}

		/// <inheritdoc cref="ReadCurrentProgram"/>
		public async Task<OperateResult<string>> ReadCurrentProgramAsync( )
		{
			OperateResult<byte[]> read = await ReadFromCoreServerAsync(
				BuildReadArray( BuildReadSingle( 0x20, 0x0594, 0x00, 0x00, 0x00, 0x00 ) ) );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<string>( read );

			byte[] result = ExtraContentArray( read.Content.RemoveBegin( 10 ) )[0];
			return OperateResult.CreateSuccessResult( Encoding.ASCII.GetString( result, 18, result.Length - 18 ) );
		}

		/// <inheritdoc cref="SetCurrentProgram(ushort)"/>
		public async Task<OperateResult> SetCurrentProgramAsync( ushort programNum )
		{
			OperateResult<byte[]> read = await ReadFromCoreServerAsync(
				BuildReadArray( BuildReadSingle( 0x03, programNum, 0, 0, 0, 0 ) ) );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<int, string>( read );

			byte[] result = ExtraContentArray( read.Content.RemoveBegin( 10 ) )[0];
			short err = this.ByteTransform.TransInt16( result, 6 );

			if (err == 0) return OperateResult.CreateSuccessResult( );
			else return new OperateResult( err, StringResources.Language.UnknownError );
		}

		/// <inheritdoc cref="StartProcessing"/>
		public async Task<OperateResult> StartProcessingAsync( )
		{
			OperateResult<byte[]> read = await ReadFromCoreServerAsync(
				BuildReadArray( BuildReadSingle( 0x01, 0, 0, 0, 0, 0 ) ) );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<int, string>( read );

			byte[] result = ExtraContentArray( read.Content.RemoveBegin( 10 ) )[0];
			short err = this.ByteTransform.TransInt16( result, 6 );

			if (err == 0) return OperateResult.CreateSuccessResult( );
			else return new OperateResult( err, StringResources.Language.UnknownError );
		}

		/// <inheritdoc cref="WriteProgramFile(string)"/>
		public async Task<OperateResult> WriteProgramFileAsync( string file )
		{
			string content = File.ReadAllText( file );
			return await WriteProgramContentAsync( content );
		}

		/// <inheritdoc cref="WriteProgramContent(string, int)"/>
		public async Task<OperateResult> WriteProgramContentAsync( string program, int everyWriteSize = 512 )
		{
			if (!Authorization.asdniasnfaksndiqwhawfskhfaiw( )) return new OperateResult( StringResources.Language.InsufficientPrivileges );

			OperateResult<Socket> socket = await CreateSocketAndConnectAsync( IpAddress, Port, ConnectTimeOut );
			if (!socket.IsSuccess) return socket.ConvertFailed<int>( );

			OperateResult<byte[]> ini1 = await ReadFromCoreServerAsync( socket.Content, "a0 a0 a0 a0 00 01 01 01 00 02 00 01".ToHexBytes( ) );
			if (!ini1.IsSuccess) return ini1;

			OperateResult<byte[]> read1 = await ReadFromCoreServerAsync( socket.Content, BulidWriteProgramFilePre( ) );
			if (!read1.IsSuccess) return read1;

			List<byte[]> contents = BulidWriteProgram( Encoding.ASCII.GetBytes( program ), everyWriteSize );
			for (int i = 0; i < contents.Count; i++)
			{
				OperateResult<byte[]> read2 = await ReadFromCoreServerAsync( socket.Content, contents[i], false );
				if (!read2.IsSuccess) return read2;
			}

			OperateResult<byte[]> read3 = await ReadFromCoreServerAsync( socket.Content, new byte[] { 0xa0, 0xa0, 0xa0, 0xa0, 0x00, 0x01, 0x13, 0x01, 0x00, 0x00 } );
			if (!read3.IsSuccess) return read3;

			socket.Content?.Close( );
			if(read3.Content.Length >= 14)
			{
				int err = this.ByteTransform.TransInt16( read3.Content, 12 );
				if (err != 0) return new OperateResult<string>( err, StringResources.Language.UnknownError );
			}

			return OperateResult.CreateSuccessResult( );
		}

		/// <inheritdoc cref="ReadProgram(int)"/>
		public async Task<OperateResult<string>> ReadProgramAsync( int program )
		{
			if (!Authorization.asdniasnfaksndiqwhawfskhfaiw( )) return new OperateResult<string>( StringResources.Language.InsufficientPrivileges );

			OperateResult<Socket> socket = await CreateSocketAndConnectAsync( IpAddress, Port, ConnectTimeOut );
			if (!socket.IsSuccess) return socket.ConvertFailed<string>( );

			OperateResult<byte[]> ini1 = await ReadFromCoreServerAsync( socket.Content, "a0 a0 a0 a0 00 01 01 01 00 02 00 01".ToHexBytes( ) );
			if (!ini1.IsSuccess) return OperateResult.CreateFailedResult<string>( ini1 );

			OperateResult<byte[]> read1 = await ReadFromCoreServerAsync( socket.Content, BuildReadProgramPre( program ) );
			if (!read1.IsSuccess) return OperateResult.CreateFailedResult<string>( read1 );

			// 检测错误信息
			int err = read1.Content[12] * 256 + read1.Content[13];
			if (err != 0)
			{
				socket.Content?.Close( );
				return new OperateResult<string>( err, StringResources.Language.UnknownError );
			}

			StringBuilder sb = new StringBuilder( );
			while (true)
			{
				OperateResult<byte[]> read2 = await ReadFromCoreServerAsync( socket.Content, null );
				if (!read2.IsSuccess) return OperateResult.CreateFailedResult<string>( read2 );

				if (read2.Content[6] == 0x16)
					sb.Append( Encoding.ASCII.GetString( read2.Content, 10, read2.Content.Length - 10 ) );
				else if (read2.Content[6] == 0x17)
					break;
			}

			OperateResult send = await SendAsync( socket.Content, new byte[] { 0xa0, 0xa0, 0xa0, 0xa0, 0x00, 0x01, 0x17, 0x02, 0x00, 0x00 } );
			if (!send.IsSuccess) return OperateResult.CreateFailedResult<string>( send );

			socket.Content?.Close( );
			return OperateResult.CreateSuccessResult( sb.ToString( ) );
		}

		/// <inheritdoc cref="DeleteProgram(int)"/>
		public async Task<OperateResult> DeleteProgramAsync( int program )
		{
			OperateResult<byte[]> read = await ReadFromCoreServerAsync(
				BuildReadArray( BuildReadSingle( 0x05, program, 0, 0, 0, 0 ) ) );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<int, string>( read );

			byte[] result = ExtraContentArray( read.Content.RemoveBegin( 10 ) )[0];
			short err = this.ByteTransform.TransInt16( result, 6 );

			if (err == 0) return OperateResult.CreateSuccessResult( );
			else return new OperateResult( err, StringResources.Language.UnknownError );
		}

#endif
		#endregion

		#region Build Command

		/// <summary>
		/// 构建读取一个命令的数据内容
		/// </summary>
		/// <param name="code">命令码</param>
		/// <param name="a">第一个参数内容</param>
		/// <param name="b">第二个参数内容</param>
		/// <param name="c">第三个参数内容</param>
		/// <param name="d">第四个参数内容</param>
		/// <param name="e">第五个参数内容</param>
		/// <returns>总报文信息</returns>
		private byte[] BuildReadSingle( ushort code, int a, int b, int c, int d, int e )
		{
			return BuildReadMulti( 0x01, code, a, b, c, d, e );
		}

		/// <summary>
		/// 构建读取多个命令的数据内容
		/// </summary>
		/// <param name="mode">模式</param>
		/// <param name="code">命令码</param>
		/// <param name="a">第一个参数内容</param>
		/// <param name="b">第二个参数内容</param>
		/// <param name="c">第三个参数内容</param>
		/// <param name="d">第四个参数内容</param>
		/// <param name="e">第五个参数内容</param>
		/// <returns>总报文信息</returns>
		private byte[] BuildReadMulti( ushort mode, ushort code, int a, int b, int c, int d, int e )
		{
			byte[] buffer = new byte[28];
			buffer[1] = 0x1C;
			this.ByteTransform.TransByte( mode ).CopyTo( buffer, 2 );
			buffer[5] = 0x01;
			this.ByteTransform.TransByte( code ).CopyTo( buffer, 6 );
			this.ByteTransform.TransByte( a ).CopyTo( buffer, 8 );
			this.ByteTransform.TransByte( b ).CopyTo( buffer, 12 );
			this.ByteTransform.TransByte( c ).CopyTo( buffer, 16 );
			this.ByteTransform.TransByte( d ).CopyTo( buffer, 20 );
			this.ByteTransform.TransByte( e ).CopyTo( buffer, 24 );
			return buffer;
		}

		/// <summary>
		/// 创建写入byte[]数组的报文信息
		/// </summary>
		/// <param name="code">命令码</param>
		/// <param name="a">第一个参数内容</param>
		/// <param name="b">第二个参数内容</param>
		/// <param name="c">第三个参数内容</param>
		/// <param name="d">第四个参数内容</param>
		/// <param name="data">等待写入的byte数组信息</param>
		/// <returns>总报文信息</returns>
		private byte[] BuildWriteSingle( ushort code, int a, int b, int c, int d, byte[] data )
		{
			byte[] buffer = new byte[28 + data.Length];
			this.ByteTransform.TransByte( (ushort)buffer.Length ).CopyTo( buffer, 0 );
			buffer[3] = 0x01;
			buffer[5] = 0x01;
			this.ByteTransform.TransByte( code ).CopyTo( buffer, 6 );
			this.ByteTransform.TransByte( a ).CopyTo( buffer, 8 );
			this.ByteTransform.TransByte( b ).CopyTo( buffer, 12 );
			this.ByteTransform.TransByte( c ).CopyTo( buffer, 16 );
			this.ByteTransform.TransByte( d ).CopyTo( buffer, 20 );
			this.ByteTransform.TransByte( data.Length ).CopyTo( buffer, 24 );
			if (data.Length > 0) data.CopyTo( buffer, 28 );
			return buffer;
		}

		/// <summary>
		/// 创建写入单个double数组的报文信息
		/// </summary>
		/// <param name="code">功能码</param>
		/// <param name="a">第一个参数内容</param>
		/// <param name="b">第二个参数内容</param>
		/// <param name="c">第三个参数内容</param>
		/// <param name="d">第四个参数内容</param>
		/// <param name="data">等待写入的double数组信息</param>
		/// <returns>总报文信息</returns>
		private byte[] BuildWriteSingle( ushort code, int a, int b, int c, int d, double[] data )
		{
			byte[] buffer = new byte[data.Length * 8];
			for (int i = 0; i < data.Length; i++)
			{
				CreateFromFanucDouble( data[i] ).CopyTo( buffer, 0 );
			}
			return BuildWriteSingle( code, a, b, c, d, buffer );
		}

		/// <summary>
		/// 创建多个命令报文的总报文信息
		/// </summary>
		/// <param name="commands">报文命令的数组</param>
		/// <returns>总报文信息</returns>
		private byte[] BuildReadArray( params byte[][] commands )
		{
			MemoryStream ms = new MemoryStream( );
			ms.Write( new byte[] { 0xa0, 0xa0, 0xa0, 0xa0, 0x00, 0x01, 0x21, 0x01, 0x00, 0x1e }, 0, 10 );
			ms.Write( ByteTransform.TransByte( (ushort)commands.Length ), 0, 2 );
			for (int i = 0; i < commands.Length; i++)
			{
				ms.Write( commands[i], 0, commands[i].Length );
			}
			byte[] buffer = ms.ToArray( );
			this.ByteTransform.TransByte( (ushort)(buffer.Length - 10) ).CopyTo( buffer, 8 );
			return buffer;
		}

		private byte[] BulidWriteProgramFilePre( )
		{
			MemoryStream ms = new MemoryStream( );
			ms.Write( new byte[] { 0xa0, 0xa0, 0xa0, 0xa0, 0x00, 0x01, 0x11, 0x01, 0x02, 0x04 }, 0, 10 );
			ms.Write( new byte[] { 0x00, 0x00, 0x00, 0x01 }, 0, 4 );
			for (int i = 0; i < 512; i++)
			{
				ms.WriteByte( 0x00 );
			}
			return ms.ToArray( );
		}

		/// <summary>
		/// 创建读取运行程序的报文信息
		/// </summary>
		/// <param name="program">程序号</param>
		/// <returns>总报文</returns>
		private byte[] BuildReadProgramPre( int program )
		{
			MemoryStream ms = new MemoryStream( );
			ms.Write( new byte[] { 0xa0, 0xa0, 0xa0, 0xa0, 0x00, 0x01, 0x15, 0x01, 0x02, 0x04 }, 0, 10 );
			ms.Write( new byte[] { 0x00, 0x00, 0x00, 0x01 }, 0, 4 );
			for (int i = 0; i < 512; i++)
			{
				ms.WriteByte( 0x00 );
			}
			byte[] buffer = ms.ToArray( );
			string pro = "O" + program + "-" + "O" + program;
			Encoding.ASCII.GetBytes( pro ).CopyTo( buffer, 14 );
			return buffer;
		}

		private List<byte[]> BulidWriteProgram( byte[] program, int everyWriteSize )
		{
			List<byte[]> list = new List<byte[]>( );
			int[] lengths = SoftBasic.SplitIntegerToArray( program.Length, everyWriteSize );
			int index = 0;
			for (int i = 0; i < lengths.Length; i++)
			{
				MemoryStream ms = new MemoryStream( );
				ms.Write( new byte[] { 0xa0, 0xa0, 0xa0, 0xa0, 0x00, 0x01, 0x12, 0x04, 0x00, 0x00 }, 0, 10 );
				ms.Write( program, index, lengths[i] );
				byte[] buffer = ms.ToArray( );
				this.ByteTransform.TransByte( (ushort)(buffer.Length - 10) ).CopyTo( buffer, 8 );

				list.Add( buffer );
				index += lengths[i];
			}
			return list;
		}

		/// <summary>
		/// 从机床返回的数据里解析出实际的数据内容，去除了一些多余的信息报文。
		/// </summary>
		/// <param name="content">返回的报文信息</param>
		/// <returns>解析之后的报文信息</returns>
		private List<byte[]> ExtraContentArray( byte[] content )
		{
			List<byte[]> list = new List<byte[]>( );
			int count = ByteTransform.TransUInt16( content, 0 );
			int index = 2;

			for (int i = 0; i < count; i++)
			{
				ushort length = ByteTransform.TransUInt16( content, index );
				list.Add( content.SelectMiddle( index + 2, length - 2 ) );
				index += length;
			}
			return list;
		}

		private OperateResult<CutterInfo[]> ExtraCutterInfos( byte[] content1, byte[] content2, byte[] content3, byte[] content4, int cutterNumber )
		{
			// 先提取出各个数据信息
			List<byte[]> result1 = ExtraContentArray( content1.RemoveBegin( 10 ) );
			List<byte[]> result2 = ExtraContentArray( content2.RemoveBegin( 10 ) );
			List<byte[]> result3 = ExtraContentArray( content3.RemoveBegin( 10 ) );
			List<byte[]> result4 = ExtraContentArray( content4.RemoveBegin( 10 ) );

			// 校验数据是否有效
			bool check1 = this.ByteTransform.TransInt16( result1[0], 6 ) == 0x00;
			bool check2 = this.ByteTransform.TransInt16( result2[0], 6 ) == 0x00;
			bool check3 = this.ByteTransform.TransInt16( result3[0], 6 ) == 0x00;
			bool check4 = this.ByteTransform.TransInt16( result4[0], 6 ) == 0x00;

			// 如果数据有效，则显示出来
			CutterInfo[] cutters = new CutterInfo[cutterNumber];
			for (int i = 0; i < cutters.Length; i++)
			{
				cutters[i] = new CutterInfo( );
				cutters[i].LengthSharpOffset = check1 ? GetFanucDouble( result1[0], 14 + 8 * i ) : double.NaN;
				cutters[i].LengthWearOffset  = check2 ? GetFanucDouble( result2[0], 14 + 8 * i ) : double.NaN;
				cutters[i].RadiusSharpOffset = check3 ? GetFanucDouble( result3[0], 14 + 8 * i ) : double.NaN;
				cutters[i].RadiusWearOffset  = check4 ? GetFanucDouble( result4[0], 14 + 8 * i ) : double.NaN;
			}

			return OperateResult.CreateSuccessResult( cutters );
		}

		#endregion

		#region Private Member

		private Encoding encoding;

		#endregion

		#region Object Override

		/// <inheritdoc/>
		public override string ToString( ) => $"FanucSeries0i[{IpAddress}:{Port}]";

		#endregion
	}
}
