using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
#if !NET35 && !NET20
using System.Net.Http;
using System.Threading.Tasks;
#endif
using System.Text;
using HslCommunication.Core.Net;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using System.Xml.Linq;
using HslCommunication.Reflection;

namespace HslCommunication.Robot.ABB
{
	/// <summary>
	/// ABB机器人的web api接口的客户端，可以方便快速的获取到abb机器人的一些数据信息<br />
	/// The client of ABB robot's web API interface can easily and quickly obtain some data information of ABB robot
	/// </summary>
	/// <remarks>
	/// 参考的界面信息是：http://developercenter.robotstudio.com/webservice/api_reference
	/// 
	/// 关于额外的地址说明，如果想要查看，可以调用<see cref="GetSelectStrings"/> 返回字符串列表来看看。
	/// </remarks>
	public class ABBWebApiClient : NetworkWebApiRobotBase, IRobotNet
	{
		#region Constrcutor

		/// <summary>
		/// 使用指定的ip地址来初始化对象<br />
		/// Initializes the object using the specified IP address
		/// </summary>
		/// <param name="ipAddress">Ip地址信息</param>
		public ABBWebApiClient( string ipAddress ) : base( ipAddress ) { }

		/// <summary>
		/// 使用指定的ip地址和端口号来初始化对象<br />
		/// Initializes the object with the specified IP address and port number
		/// </summary>
		/// <param name="ipAddress">Ip地址信息</param>
		/// <param name="port">端口号信息</param>
		public ABBWebApiClient( string ipAddress, int port ) : base( ipAddress, port ) { }

		/// <summary>
		/// 使用指定的ip地址，端口号，用户名，密码来初始化对象<br />
		/// Initialize the object with the specified IP address, port number, username, and password
		/// </summary>
		/// <param name="ipAddress">Ip地址信息</param>
		/// <param name="port">端口号信息</param>
		/// <param name="name">用户名</param>
		/// <param name="password">密码</param>
		public ABBWebApiClient( string ipAddress, int port, string name, string password ) : base( ipAddress, port, name, password ) { }

		#endregion

		#region Read Write Support

		/// <inheritdoc/>
		[HslMqttApi( ApiTopic = "ReadRobotByte", Description = "Read the other side of the data information, usually designed for the GET method information.If you start with url=, you are using native address access")]
		public override OperateResult<byte[]> Read(string address)
		{
			return base.Read(address);
		}

		/// <inheritdoc/>
		[HslMqttApi( ApiTopic = "ReadRobotString", Description = "The string data information that reads the other party information, usually designed for the GET method information.If you start with url=, you are using native address access")]
		public override OperateResult<string> ReadString(string address)
		{
			return base.ReadString(address);
		}

		/// <inheritdoc/>
		[HslMqttApi( ApiTopic = "WriteRobotByte", Description = "Using POST to request data information from the other party, we need to start with url= to indicate that we are using native address access")]
		public override OperateResult Write(string address, byte[] value)
		{
			return base.Write(address, value);
		}

		/// <inheritdoc/>
		[HslMqttApi( ApiTopic = "WriteRobotString", Description = "Using POST to request data information from the other party, we need to start with url= to indicate that we are using native address access")]
		public override OperateResult Write(string address, string value)
		{
			return base.Write(address, value);
		}

		/// <inheritdoc/>
		protected override OperateResult<string> ReadByAddress( string address )
		{
			if      (address.ToUpper( ) == "ErrorState".ToUpper( ))     return GetErrorState( );
			else if (address.ToUpper( ) == "jointtarget".ToUpper( ))    return GetJointTarget( );
			else if (address.ToUpper( ) == "PhysicalJoints".ToUpper( )) return GetJointTarget( );
			else if (address.ToUpper( ) == "SpeedRatio".ToUpper( ))     return GetSpeedRatio( );
			else if (address.ToUpper( ) == "OperationMode".ToUpper( ))  return GetOperationMode( );
			else if (address.ToUpper( ) == "CtrlState".ToUpper( ))      return GetCtrlState( );
			else if (address.ToUpper( ) == "ioin".ToUpper( ))           return GetIOIn( );
			else if (address.ToUpper( ) == "ioout".ToUpper( ))          return GetIOOut( );
			else if (address.ToUpper( ) == "io2in".ToUpper( ))          return GetIO2In( );
			else if (address.ToUpper( ) == "io2out".ToUpper( ))         return GetIO2Out( );
			else if (address.ToUpper( ).StartsWith( "log".ToUpper( ) ))
			{
				if (address.Length > 3)
				{
					if(int.TryParse( address.Substring( 3 ) , out int length )) return GetLog( length );
				}
				return GetLog( );
			}
			else if (address.ToUpper( ) == "system".ToUpper( )) return GetSystem( );
			else if (address.ToUpper( ) == "robtarget".ToUpper( )) return GetRobotTarget( );
			else if (address.ToUpper( ) == "ServoEnable".ToUpper( )) return GetServoEnable( );
			else if (address.ToUpper( ) == "RapidExecution".ToUpper( )) return GetRapidExecution( );
			else if (address.ToUpper( ) == "RapidTasks".ToUpper( )) return GetRapidTasks( );
			else return base.ReadByAddress( address );
		}
#if !NET35 && !NET20
		/// <inheritdoc/>
		protected async override Task<OperateResult<string>> ReadByAddressAsync( string address )
		{
			if (address.ToUpper( ) == "ErrorState".ToUpper( )) return await GetErrorStateAsync( );
			else if (address.ToUpper( ) == "jointtarget".ToUpper( )) return await GetJointTargetAsync( );
			else if (address.ToUpper( ) == "PhysicalJoints".ToUpper( )) return await GetJointTargetAsync( );
			else if (address.ToUpper( ) == "SpeedRatio".ToUpper( )) return await GetSpeedRatioAsync( );
			else if (address.ToUpper( ) == "OperationMode".ToUpper( )) return await GetOperationModeAsync( );
			else if (address.ToUpper( ) == "CtrlState".ToUpper( )) return await GetCtrlStateAsync( );
			else if (address.ToUpper( ) == "ioin".ToUpper( )) return await GetIOInAsync( );
			else if (address.ToUpper( ) == "ioout".ToUpper( )) return await GetIOOutAsync( );
			else if (address.ToUpper( ) == "io2in".ToUpper( )) return await GetIO2InAsync( );
			else if (address.ToUpper( ) == "io2out".ToUpper( )) return await GetIO2OutAsync( );
			else if (address.ToUpper( ).StartsWith( "log".ToUpper( ) ))
			{
				if (address.Length > 3)
				{
					if (int.TryParse( address.Substring( 3 ), out int length )) return await GetLogAsync( length );
				}
				return await GetLogAsync( );
			}
			else if (address.ToUpper( ) == "system".ToUpper( )) return await GetSystemAsync( );
			else if (address.ToUpper( ) == "robtarget".ToUpper( )) return await GetRobotTargetAsync( );
			else if (address.ToUpper( ) == "ServoEnable".ToUpper( )) return await GetServoEnableAsync( );
			else if (address.ToUpper( ) == "RapidExecution".ToUpper( )) return await GetRapidExecutionAsync( );
			else if (address.ToUpper( ) == "RapidTasks".ToUpper( )) return await GetRapidTasksAsync( );
			else return await base.ReadByAddressAsync( address );
		}
#endif
		/// <summary>
		/// 获取当前支持的读取的地址列表<br />
		/// Gets a list of addresses for currently supported reads
		/// </summary>
		/// <returns>数组信息</returns>
		public static List<string> GetSelectStrings( )
		{
			return new List<string>( )
			{
				"ErrorState",
				"jointtarget",
				"PhysicalJoints",
				"SpeedRatio",
				"OperationMode",
				"CtrlState",
				"ioin",
				"ioout",
				"io2in",
				"io2out",
				"log",
				"system",
				"robtarget",
				"ServoEnable",
				"RapidExecution",
				"RapidTasks"
			};
		}

		#endregion

		#region Private Member

		private OperateResult<string> AnalysisClassAttribute(string content, string[] atts )
		{
			JObject jObject = new JObject( );

			for (int i = 0; i < atts.Length; i++)
			{
				Match match = Regex.Match( content, "<span class=\"" + atts[i] + "\">[^<]*" );
				if (!match.Success) return new OperateResult<string>( content );
				jObject.Add( atts[i], new JValue( match.Value.Substring( 15 + atts[i].Length ) ) );
			}
			return OperateResult.CreateSuccessResult( jObject.ToString( ) );
		}

		private OperateResult<string> AnalysisSystem( string content )
		{
			return AnalysisClassAttribute( content,
				   new string[] { "major", "minor", "build", "title", "type", "description",
				   "date", "mctimestamp", "name", "sysid", "starttm"} );
		}

		private OperateResult<string> AnalysisRobotTarget( string content )
		{
			return AnalysisClassAttribute( content,
				new string[] { "x", "y", "z", "q1", "q2", "q3" } );
		}

		#endregion

		#region Public Method

		/// <summary>
		/// 获取当前的控制状态，Content属性就是机器人的控制信息<br />
		/// Get the current control state. The Content attribute is the control information of the robot
		/// </summary>
		/// <returns>带有状态信息的结果类对象</returns>
		[HslMqttApi( Description = "Get the current control state. The Content attribute is the control information of the robot")]
		public OperateResult<string> GetCtrlState( )
		{
			OperateResult<string> read = ReadString( "url=/rw/panel/ctrlstate" );
			if (!read.IsSuccess) return read;

			Match match = Regex.Match( read.Content, "<span class=\"ctrlstate\">[^<]+" );
			if (!match.Success) return new OperateResult<string>( read.Content );

			return OperateResult.CreateSuccessResult( match.Value.Substring( 24 ) );
		}

		/// <summary>
		/// 获取当前的错误状态，Content属性就是机器人的状态信息<br />
		/// Gets the current error state. The Content attribute is the state information of the robot
		/// </summary>
		/// <returns>带有状态信息的结果类对象</returns>
		[HslMqttApi( Description = "Gets the current error state. The Content attribute is the state information of the robot")]
		public OperateResult<string> GetErrorState( )
		{
			OperateResult<string> read = ReadString( "url=/rw/motionsystem/errorstate" );
			if (!read.IsSuccess) return read;

			Match match = Regex.Match( read.Content, "<span class=\"err-state\">[^<]+" );
			if (!match.Success) return new OperateResult<string>( read.Content );

			return OperateResult.CreateSuccessResult( match.Value.Substring( 24 ) );
		}

		/// <summary>
		/// 获取当前机器人的物理关节点信息，返回json格式的关节信息<br />
		/// Get the physical node information of the current robot and return the joint information in json format
		/// </summary>
		/// <returns>带有关节信息的结果类对象</returns>
		[HslMqttApi( Description = "Get the physical node information of the current robot and return the joint information in json format")]
		public OperateResult<string> GetJointTarget( )
		{
			OperateResult<string> read = ReadString( "url=/rw/motionsystem/mechunits/ROB_1/jointtarget" );
			if (!read.IsSuccess) return read;

			MatchCollection mc = Regex.Matches( read.Content, "<span class=\"rax[^<]*" );
			if (mc.Count != 6) return new OperateResult<string>( read.Content );

			double[] joints = new double[6];
			for (int i = 0; i < mc.Count; i++)
			{
				if(mc[i].Length > 17)
				{
					joints[i] = double.Parse( mc[i].Value.Substring( 20 ) );
				}
			}
			return OperateResult.CreateSuccessResult( JArray.FromObject( joints ).ToString( Newtonsoft.Json.Formatting.None ) );
		}

		/// <summary>
		/// 获取当前机器人的速度配比信息<br />
		/// Get the speed matching information of the current robot
		/// </summary>
		/// <returns>带有速度信息的结果类对象</returns>
		[HslMqttApi( Description = "Get the speed matching information of the current robot")]
		public OperateResult<string> GetSpeedRatio( )
		{
			OperateResult<string> read = ReadString( "url=/rw/panel/speedratio" );
			if (!read.IsSuccess) return read;

			Match match = Regex.Match( read.Content, "<span class=\"speedratio\">[^<]*" );
			if (!match.Success) return new OperateResult<string>( read.Content );

			return OperateResult.CreateSuccessResult( match.Value.Substring( 25 ) );
		}

		/// <summary>
		/// 获取当前机器人的工作模式<br />
		/// Gets the current working mode of the robot
		/// </summary>
		/// <returns>带有工作模式信息的结果类对象</returns>
		[HslMqttApi( Description = "Gets the current working mode of the robot")]
		public OperateResult<string> GetOperationMode( )
		{
			OperateResult<string> read = ReadString( "url=/rw/panel/opmode" );
			if (!read.IsSuccess) return read;

			Match match = Regex.Match( read.Content, "<span class=\"opmode\">[^<]*" );
			if (!match.Success) return new OperateResult<string>( read.Content );

			return OperateResult.CreateSuccessResult( match.Value.Substring( 21 ) );
		}

		/// <summary>
		/// 获取当前机器人的本机的输入IO<br />
		/// Gets the input IO of the current robot's native
		/// </summary>
		/// <returns>带有IO信息的结果类对象</returns>
		[HslMqttApi( Description = "Gets the input IO of the current robot's native")]
		public OperateResult<string> GetIOIn( )
		{
			OperateResult<string> read = ReadString( "url=/rw/iosystem/devices/D652_10" );
			if (!read.IsSuccess) return read;

			Match match = Regex.Match( read.Content, "<span class=\"indata\">[^<]*" );
			if (!match.Success) return new OperateResult<string>( read.Content );

			return OperateResult.CreateSuccessResult( match.Value.Substring( 21 ) );
		}

		/// <summary>
		/// 获取当前机器人的本机的输出IO<br />
		/// Gets the output IO of the current robot's native
		/// </summary>
		/// <returns>带有IO信息的结果类对象</returns>
		[HslMqttApi( Description = "Gets the output IO of the current robot's native")]
		public OperateResult<string> GetIOOut( )
		{
			OperateResult<string> read = ReadString( "url=/rw/iosystem/devices/D652_10" );
			if (!read.IsSuccess) return read;

			Match match = Regex.Match( read.Content, "<span class=\"outdata\">[^<]*" );
			if (!match.Success) return new OperateResult<string>( read.Content );

			return OperateResult.CreateSuccessResult( match.Value.Substring( 22 ) );
		}

		/// <inheritdoc cref="GetIOIn"/>
		[HslMqttApi( Description = "Gets the input IO2 of the current robot's native")]
		public OperateResult<string> GetIO2In( )
		{
			OperateResult<string> read = ReadString( "url=/rw/iosystem/devices/BK5250" );
			if (!read.IsSuccess) return read;

			Match match = Regex.Match( read.Content, "<span class=\"indata\">[^<]*" );
			if (!match.Success) return new OperateResult<string>( read.Content );

			return OperateResult.CreateSuccessResult( match.Value.Substring( 21 ) );
		}

		/// <inheritdoc cref="GetIOOut"/>
		[HslMqttApi( Description = "Gets the output IO2 of the current robot's native")]
		public OperateResult<string> GetIO2Out( )
		{
			OperateResult<string> read = ReadString( "url=/rw/iosystem/devices/BK5250" );
			if (!read.IsSuccess) return read;

			Match match = Regex.Match( read.Content, "<span class=\"outdata\">[^<]*" );
			if (!match.Success) return new OperateResult<string>( read.Content );

			return OperateResult.CreateSuccessResult( match.Value.Substring( 22 ) );
		}

		/// <summary>
		/// 获取当前机器人的日志记录，默认记录为10条<br />
		/// Gets the log record for the current robot, which is 10 by default
		/// </summary>
		/// <param name="logCount">读取的最大的日志总数</param>
		/// <returns>带有IO信息的结果类对象</returns>
		[HslMqttApi( Description = "Gets the log record for the current robot, which is 10 by default")]
		public OperateResult<string> GetLog( int logCount = 10 )
		{
			OperateResult<string> read = ReadString( "url=/rw/elog/0?lang=zh&amp;resource=title" );
			if (!read.IsSuccess) return read;

			MatchCollection matchs = Regex.Matches( read.Content, "<li class=\"elog-message-li\" title=\"/rw/elog/0/[0-9]+\">[\\S\\s]+?</li>" );
			JArray jArray = new JArray( );

			for (int i = 0; i < matchs.Count; i++)
			{
				if (i >= logCount) break;

				Match id = Regex.Match( matchs[i].Value, "[0-9]+\"" );
				JObject json = new JObject( );
				json["id"] = id.Value.TrimEnd( '"' );

				foreach (var item in XElement.Parse( matchs[i].Value ).Elements( "span" ))
				{
					json[item.Attribute( "class" ).Value] = item.Value;
				}
				jArray.Add( json );
			}

			return OperateResult.CreateSuccessResult( jArray.ToString( ) );
		}

		/// <summary>
		/// 获取当前机器人的系统信息，版本号，唯一ID等信息<br />
		/// Get the current robot's system information, version number, unique ID and other information
		/// </summary>
		/// <returns>系统的基本信息</returns>
		[HslMqttApi( Description = "Get the current robot's system information, version number, unique ID and other information")]
		public OperateResult<string> GetSystem( )
		{
			OperateResult<string> read = ReadString( "url=/rw/system" );
			if (!read.IsSuccess) return read;

			return AnalysisSystem( read.Content );
		}

		/// <summary>
		/// 获取机器人的目标坐标信息<br />
		/// Get the current robot's target information
		/// </summary>
		/// <returns>系统的基本信息</returns>
		[HslMqttApi( Description = "Get the current robot's target information" )]
		public OperateResult<string> GetRobotTarget( )
		{
			OperateResult<string> read = ReadString( "url=/rw/motionsystem/mechunits/ROB_1/robtarget" );
			if (!read.IsSuccess) return read;

			return AnalysisRobotTarget( read.Content );
		}

		/// <summary>
		/// 获取当前机器人的伺服使能状态<br />
		/// Get the current robot servo enable state
		/// </summary>
		/// <returns>机器人的伺服使能状态</returns>
		[HslMqttApi( Description = "Get the current robot servo enable state")]
		public OperateResult<string> GetServoEnable( )
		{
			OperateResult<string> read = ReadString( "url=/rw/iosystem/signals/Local/DRV_1/DRV1K1" );
			if (!read.IsSuccess) return read;

			Match match = Regex.Match( read.Content, "<li class=\"ios-signal\"[\\S\\s]+?</li>" );
			if (!match.Success) return new OperateResult<string>( read.Content );

			JObject json = new JObject( );
			foreach (var item in XElement.Parse( match.Value ).Elements( "span" ))
			{
				json[item.Attribute( "class" ).Value] = item.Value;
			}
			return OperateResult.CreateSuccessResult( json.ToString( ) );
		}

		/// <summary>
		/// 获取当前机器人的当前程序运行状态<br />
		/// Get the current program running status of the current robot
		/// </summary>
		/// <returns>机器人的当前的程序运行状态</returns>
		[HslMqttApi( Description = "Get the current program running status of the current robot")]
		public OperateResult<string> GetRapidExecution( )
		{
			OperateResult<string> read = ReadString( "url=/rw/rapid/execution" );
			if (!read.IsSuccess) return read;

			Match match = Regex.Match( read.Content, "<li class=\"rap-execution\"[\\S\\s]+?</li>" );
			if (!match.Success) return new OperateResult<string>( read.Content );

			JObject json = new JObject( );
			foreach (var item in XElement.Parse( match.Value ).Elements( "span" ))
			{
				json[item.Attribute( "class" ).Value] = item.Value;
			}
			return OperateResult.CreateSuccessResult( json.ToString( ) );
		}

		/// <summary>
		/// 获取当前机器人的任务列表<br />
		/// Get the task list of the current robot
		/// </summary>
		/// <returns>任务信息的列表</returns>
		[HslMqttApi( Description = "Get the task list of the current robot")]
		public OperateResult<string> GetRapidTasks( )
		{
			OperateResult<string> read = ReadString( "url=/rw/rapid/tasks" );
			if (!read.IsSuccess) return read;

			MatchCollection matchs = Regex.Matches( read.Content, "<li class=\"rap-task-li\" [\\S\\s]+?</li>" );
			JArray jArray = new JArray( );

			for (int i = 0; i < matchs.Count; i++)
			{
				JObject json = new JObject( );
				foreach (var item in XElement.Parse( matchs[i].Value ).Elements( "span" ))
				{
					json[item.Attribute( "class" ).Value] = item.Value;
				}
				jArray.Add( json );
			}

			return OperateResult.CreateSuccessResult( jArray.ToString( ) );
		}

		#endregion

		#region Async Public Method
#if !NET35 && !NET20
		/// <inheritdoc cref="GetCtrlState"/>
		public async Task<OperateResult<string>> GetCtrlStateAsync( )
		{
			OperateResult<string> read = await ReadStringAsync( "url=/rw/panel/ctrlstate" );
			if (!read.IsSuccess) return read;

			Match match = Regex.Match( read.Content, "<span class=\"ctrlstate\">[^<]+" );
			if (!match.Success) return new OperateResult<string>( read.Content );

			return OperateResult.CreateSuccessResult( match.Value.Substring( 24 ) );
		}

		/// <inheritdoc cref="GetErrorState"/>
		public async Task<OperateResult<string>> GetErrorStateAsync( )
		{
			OperateResult<string> read = await ReadStringAsync( "url=/rw/motionsystem/errorstate" );
			if (!read.IsSuccess) return read;

			Match match = Regex.Match( read.Content, "<span class=\"err-state\">[^<]+" );
			if (!match.Success) return new OperateResult<string>( read.Content );

			return OperateResult.CreateSuccessResult( match.Value.Substring( 24 ) );
		}

		/// <inheritdoc cref="GetJointTarget"/>
		public async Task<OperateResult<string>> GetJointTargetAsync( )
		{
			OperateResult<string> read = await ReadStringAsync( "url=/rw/motionsystem/mechunits/ROB_1/jointtarget" );
			if (!read.IsSuccess) return read;

			MatchCollection mc = Regex.Matches( read.Content, "<span class=\"rax[^<]*" );
			if (mc.Count != 6) return new OperateResult<string>( read.Content );

			double[] joints = new double[6];
			for (int i = 0; i < mc.Count; i++)
			{
				if (mc[i].Length > 17)
				{
					joints[i] = double.Parse( mc[i].Value.Substring( 20 ) );
				}
			}
			return OperateResult.CreateSuccessResult( JArray.FromObject( joints ).ToString( Newtonsoft.Json.Formatting.None ) );
		}

		/// <inheritdoc cref="GetSpeedRatio"/>
		public async Task<OperateResult<string>> GetSpeedRatioAsync( )
		{
			OperateResult<string> read = await ReadStringAsync( "url=/rw/panel/speedratio" );
			if (!read.IsSuccess) return read;

			Match match = Regex.Match( read.Content, "<span class=\"speedratio\">[^<]*" );
			if (!match.Success) return new OperateResult<string>( read.Content );

			return OperateResult.CreateSuccessResult( match.Value.Substring( 25 ) );
		}

		/// <inheritdoc cref="GetOperationMode"/>
		public async Task<OperateResult<string>> GetOperationModeAsync( )
		{
			OperateResult<string> read = await ReadStringAsync( "url=/rw/panel/opmode" );
			if (!read.IsSuccess) return read;

			Match match = Regex.Match( read.Content, "<span class=\"opmode\">[^<]*" );
			if (!match.Success) return new OperateResult<string>( read.Content );

			return OperateResult.CreateSuccessResult( match.Value.Substring( 21 ) );
		}

		/// <inheritdoc cref="GetIOIn"/>
		public async Task<OperateResult<string>> GetIOInAsync( )
		{
			OperateResult<string> read = await ReadStringAsync( "url=/rw/iosystem/devices/D652_10" );
			if (!read.IsSuccess) return read;

			Match match = Regex.Match( read.Content, "<span class=\"indata\">[^<]*" );
			if (!match.Success) return new OperateResult<string>( read.Content );

			return OperateResult.CreateSuccessResult( match.Value.Substring( 21 ) );
		}

		/// <inheritdoc cref="GetIOOut"/>
		public async Task<OperateResult<string>> GetIOOutAsync( )
		{
			OperateResult<string> read = await ReadStringAsync( "url=/rw/iosystem/devices/D652_10" );
			if (!read.IsSuccess) return read;

			Match match = Regex.Match( read.Content, "<span class=\"outdata\">[^<]*" );
			if (!match.Success) return new OperateResult<string>( read.Content );

			return OperateResult.CreateSuccessResult( match.Value.Substring( 22 ) );
		}


		/// <inheritdoc cref="GetIOIn"/>
		public async Task<OperateResult<string>> GetIO2InAsync( )
		{
			OperateResult<string> read = await ReadStringAsync( "url=/rw/iosystem/devices/BK5250" );
			if (!read.IsSuccess) return read;

			Match match = Regex.Match( read.Content, "<span class=\"indata\">[^<]*" );
			if (!match.Success) return new OperateResult<string>( read.Content );

			return OperateResult.CreateSuccessResult( match.Value.Substring( 21 ) );
		}

		/// <inheritdoc cref="GetIOOut"/>
		public async Task<OperateResult<string>> GetIO2OutAsync( )
		{
			OperateResult<string> read = await ReadStringAsync( "url=/rw/iosystem/devices/BK5250" );
			if (!read.IsSuccess) return read;

			Match match = Regex.Match( read.Content, "<span class=\"outdata\">[^<]*" );
			if (!match.Success) return new OperateResult<string>( read.Content );

			return OperateResult.CreateSuccessResult( match.Value.Substring( 22 ) );
		}

		/// <inheritdoc cref="GetLog(int)"/>
		public async Task<OperateResult<string>> GetLogAsync( int logCount = 10 )
		{
			OperateResult<string> read = await ReadStringAsync( "url=/rw/elog/0?lang=zh&amp;resource=title" );
			if (!read.IsSuccess) return read;

			MatchCollection matchs = Regex.Matches( read.Content, "<li class=\"elog-message-li\" title=\"/rw/elog/0/[0-9]+\">[\\S\\s]+?</li>" );
			JArray jArray = new JArray( );

			for (int i = 0; i < matchs.Count; i++)
			{
				if (i >= logCount) break;

				Match id = Regex.Match( matchs[i].Value, "[0-9]+\"" );
				JObject json = new JObject( );
				json["id"] = id.Value.TrimEnd( '"' );

				foreach (var item in XElement.Parse( matchs[i].Value ).Elements( "span" ))
				{
					json[item.Attribute( "class" ).Value] = item.Value;
				}
				jArray.Add( json );
			}

			return OperateResult.CreateSuccessResult( jArray.ToString( ) );
		}

		/// <inheritdoc cref="GetSystem"/>
		public async Task<OperateResult<string>> GetSystemAsync( )
		{
			OperateResult<string> read = await ReadStringAsync( "url=/rw/system" );
			if (!read.IsSuccess) return read;

			return AnalysisSystem( read.Content );
		}

		/// <inheritdoc cref="GetRobotTarget"/>
		public async Task<OperateResult<string>> GetRobotTargetAsync( )
		{
			OperateResult<string> read = await ReadStringAsync( "url=/rw/motionsystem/mechunits/ROB_1/robtarget" );
			if (!read.IsSuccess) return read;

			return AnalysisRobotTarget( read.Content );
		}

		/// <inheritdoc cref="GetServoEnable"/>
		public async Task<OperateResult<string>> GetServoEnableAsync( )
		{
			OperateResult<string> read = await ReadStringAsync( "url=/rw/iosystem/signals/Local/DRV_1/DRV1K1" );
			if (!read.IsSuccess) return read;

			Match match = Regex.Match( read.Content, "<li class=\"ios-signal\"[\\S\\s]+?</li>" );
			if (!match.Success) return new OperateResult<string>( read.Content );

			JObject json = new JObject( );
			foreach (var item in XElement.Parse( match.Value ).Elements( "span" ))
			{
				json[item.Attribute( "class" ).Value] = item.Value;
			}
			return OperateResult.CreateSuccessResult( json.ToString( ) );
		}

		/// <inheritdoc cref="GetRapidExecution"/>
		public async Task<OperateResult<string>> GetRapidExecutionAsync( )
		{
			OperateResult<string> read = await ReadStringAsync( "url=/rw/rapid/execution" );
			if (!read.IsSuccess) return read;

			Match match = Regex.Match( read.Content, "<li class=\"rap-execution\"[\\S\\s]+?</li>" );
			if (!match.Success) return new OperateResult<string>( read.Content );

			JObject json = new JObject( );
			foreach (var item in XElement.Parse( match.Value ).Elements( "span" ))
			{
				json[item.Attribute( "class" ).Value] = item.Value;
			}
			return OperateResult.CreateSuccessResult( json.ToString( ) );
		}

		/// <inheritdoc cref="GetRapidTasks"/>
		public async Task<OperateResult<string>> GetRapidTasksAsync( )
		{
			OperateResult<string> read = await ReadStringAsync( "url=/rw/rapid/tasks" );
			if (!read.IsSuccess) return read;

			MatchCollection matchs = Regex.Matches( read.Content, "<li class=\"rap-task-li\" [\\S\\s]+?</li>" );
			JArray jArray = new JArray( );

			for (int i = 0; i < matchs.Count; i++)
			{
				JObject json = new JObject( );
				foreach (var item in XElement.Parse( matchs[i].Value ).Elements( "span" ))
				{
					json[item.Attribute( "class" ).Value] = item.Value;
				}
				jArray.Add( json );
			}

			return OperateResult.CreateSuccessResult( jArray.ToString( ) );
		}

#endif
		#endregion

		#region Object Override

		/// <inheritdoc/>
		public override string ToString( ) => $"ABBWebApiClient[{IpAddress}:{Port}]";

		#endregion
	}
}
