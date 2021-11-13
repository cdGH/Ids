using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HslCommunication.Enthernet;
using System.Net;
using HslCommunication.Reflection;

#if !NET35 && !NET20
using System.Threading.Tasks;
#endif

namespace HslCommunication.Robot.ABB
{
	/// <summary>
	/// <b>[商业授权]</b> ABB机器人的虚拟服务器，基于WebApi协议构建，可用于读取一些数据信息<br />
	/// <b>[Authorization]</b>The virtual server of ABB robot, built based on the WebApi protocol, can be used to read some data information
	/// </summary>
	/// <remarks>
	/// 本虚拟服务器实例化之后，就可以启动了，需要注意的是，程序需要管理员模式运行，否则启动服务的时候会报错，显示拒绝当前的操作。
	/// 支持和<see cref="ABBWebApiClient"/>进行测试通信。本服务器的运行需要商业授权支持，否则只能运行24小时。
	/// </remarks>
	public class ABBWebApiServer : HttpServer
	{
		/// <summary>
		/// 设置用户的登录信息，用户名和密码信息<br />
		/// Set user login information, user name and password information
		/// </summary>
		/// <param name="name">用户名</param>
		/// <param name="password">密码</param>
		[HslMqttApi]
		public void SetLoginAccount( string name, string password )
		{
			this.userName = name;
			this.password = password;
		}


		/// <inheritdoc cref="HttpServer.HandleRequest(HttpListenerRequest, HttpListenerResponse, string)"/>

#if NET20 || NET35
		protected override string HandleRequest( HttpListenerRequest request, HttpListenerResponse response, string data )
#else
		protected override async Task<string> HandleRequest( HttpListenerRequest request, HttpListenerResponse response, string data )
#endif
		{
			if (!Authorization.asdniasnfaksndiqwhawfskhfaiw()) { return StringResources.Language.InsufficientPrivileges; };

			string[] values = request.Headers.GetValues( "Authorization" );
			if (values == null || values.Length < 1 || string.IsNullOrEmpty( values[0] ))
			{
				response.StatusCode = 401;
				response.AddHeader( "WWW-Authenticate", "Basic realm=\"Secure Area\"" );
				return "";
			}

			string base64String = values[0].Split( new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries )[1];
			string accountString = Encoding.UTF8.GetString( Convert.FromBase64String( base64String ) );
			string[] account = accountString.Split( new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries );
			if (account.Length < 2)
			{
				response.StatusCode = 401;
				response.AddHeader( "WWW-Authenticate", "Basic realm=\"Secure Area\"" );
				return "";
			}

			if(userName != account[0] || password != account[1])
			{
				response.StatusCode = 401;
				response.AddHeader( "WWW-Authenticate", "Basic realm=\"Secure Area\"" );

				LogNet?.WriteDebug( $"Account Check Failed:{account[0]}:{account[1]}" );
				return "";
			}


			if(request.RawUrl == "/rw/motionsystem/mechunits/ROB_1/jointtarget")
			{
				return @"<?xml version=""1.0"" encoding=""UTF-8""?>
<html>
<head>
<title>motionsystem</title>
<base href= ""http://localhost/rw/motionsystem/mechunits/ROB_1/jointtarget/"" />
</head>
<body>
<div class=""state"">
<ul>
<li class=""ms-jointtarget"" title=""ROB_1"">
<span class=""rax_1"">0</span>
<span class=""rax_2"">0</span>
<span class=""rax_3"">0</span>
<span class=""rax_4"">0</span>
<span class=""rax_5"">0</span>
<span class=""rax_6"">0</span>
<span class=""eax_a"">0</span>
<span class=""eax_b"">0</span>
<span class=""eax_c"">0</span>
<span class=""eax_d"">0</span>
<span class=""eax_e"">0</span>
<span class=""eax_f"">0</span>
</li>
</ul>
</div>
</body>
</html>";
			}
			else if(request.RawUrl == "/rw/motionsystem/errorstate")
			{
				return @"<?xml version=""1.0"" encoding=""UTF-8""?>
<html>
	<head>
		<title>motionsystem</title>
		<base href= ""http://localhost/rw/motionsystem/"" />
	</head>
	<body>
		<div class=""state"">
			<a href= ""errorstate"" rel=""self""/>
			<ul>
				<li class=""ms-errorstate"" title=""errorstate"">
					<span class=""err-state"">HPJ_OK</span>
					<span class=""err-count"">0</span>
				</li>
			</ul>
		</div>
	</body>
</html>";
			}
			else if(request.RawUrl == "/rw/motionsystem/mechunits/ROB_1/robtarget")
			{
				return @"<?xml version=""1.0"" encoding=""UTF-8""?>
<html>
<head>
<title>motionsystem</title>
<base href= ""http://localhost/rw/motionsystem/mechunits/ROB_1/robtarget/"" />
</head>
<body>
<div class=""state"">
<ul>
<li class=""ms-robtargets"" title=""ROB_1"">
<span class=""x"">515</span>
<span class=""y"">0</span>
<span class=""z"">712</span>
<span class=""q1"">0.7071068</span>
<span class=""q2"">0</span>
<span class=""q3"">0.7071068</span>
<span class=""q4"">0</span>
<span class=""cf1"">0</span>
<span class=""cf4"">0</span>
<span class=""cf6"">0</span>
<span class=""cfx"">0</span>
<span class=""eax_a"">8.999999e+009</span>
<span class=""eax_b"">8.999999e+009</span>
<span class=""eax_c"">8.999999e+009</span>
<span class=""eax_d"">8.999999e+009</span>
<span class=""eax_e"">8.999999e+009</span>
<span class=""eax_f"">8.999999e+009</span>
</li>
</ul>
</div>
</body>
</html>";
			}
			else if(request.RawUrl == "/rw/system")
			{
				return @"<?xml version=""1.0"" encoding=""UTF-8""?>
<html xmlns=""http://www.w3.org/1999/xhtml"">
<head>
<title>system</title>
<base href=""http://localhost/rw/system/""/>
</head>
<body>
<div class=""state"">
<a href="""" rel=""self""/>
<ul>
<li class=""sys-system-li"" title=""system"">
<span class=""major"">6</span>
<span class=""minor"">05</span>
<span class=""build"">0047</span>
<span class=""revision"">00</span>
<span class=""sub-revision"">00</span>
<span class=""buildtag"">Internal build 0047</span>
<span class=""robapi-compatibility-revision"">0</span>
<span class=""title"">RobotWare</span>
<span class=""type"">RobotWare</span>
<span class=""description"">Controller Software</span>
<span class=""date"">2016-11-25</span>
<span class=""mctimestamp"">#20:14:43/nov 24 2016#</span>
<span class=""name"">6.05.0047</span>
<span class=""rwversion"">6.05.0047</span>
<span class=""sysid"">{14C798C8-3C47-4E4C-8CD4-040EC9483A10}</span>
<span class=""starttm"">2016-12-09 T 17:47:42</span>
<span class=""rwversionname"">6.05.00.00 Internal build 0047</span>
</li>
<li class=""sys-options-li"" title=""options"">
<a href=""options"" rel=""self"" />
<ul>
<li class=""sys-option-li"" title=""0"">
<span class=""option"">RobotWare Base</span>
</li>
<li class=""sys-option-li"" title=""1"">
<span class=""option"">English</span>
</li>
<li class=""sys-option-li"" title=""2"">
<span class=""option"">614-1 FTP and NFS client</span>
</li>
<li class=""sys-option-li"" title=""3"">
<span class=""option"">616-1 PC Interface</span>
</li>
<li class=""sys-option-li"" title=""4"">
<span class=""option"">617-1 FlexPendant Interface</span>
</li>
<li class=""sys-option-li"" title=""5"">
<span class=""option"">623-1 Multitasking</span>
</li>
<li class=""sys-option-li"" title=""6"">
<span class=""option"">608-1 World Zones</span>
</li>
<li class=""sys-option-li"" title=""7"">
<span class=""option"">Motor Commutation</span>
</li>
<li class=""sys-option-li"" title=""8"">
<span class=""option"">Service Info System</span>
</li>
<li class=""sys-option-li"" title=""9"">
<span class=""option"">Calib. Pendelum RAPID</span>
</li>
<li class=""sys-option-li"" title=""10"">
<span class=""option"">Drive System 120/140/260/360/1200/1400/1520/1600</span>
</li>
<li class=""sys-option-li"" title=""11"">
<span class=""option"">IRB 140-5/0.8 Type A</span>
</li>
<li class=""sys-option-li"" title=""12"">
<span class=""option"">810-2 SafeMove</span>
</li>
<li class=""sys-energy-li"" title=""energy"">
<a href=""energy"" rel=""self""/>
</li>
<li class=""sys-license-li"" title=""license"">
<a href=""license"" rel=""self""/>
</li>
<li class=""sys-products-li"" title=""products"">
<a href=""products"" rel=""self""/>
</li>
</ul>
</li>
</ul>
</div>
</body>
</html>";
			}
			else if(request.RawUrl == "/rw/panel/speedratio")
			{
				return @"<?xml version=""1.0"" encoding=""UTF-8""?>
<html xmlns=""http://www.w3.org/1999/xhtml"">
<head>
<title>panel</title>
<base href= ""http://localhost/rw/panel/speedratio/"" />
</head>
<body>
<div class=""state"">
<a href= """" rel=""self""/>
<a href= ""?action=show"" rel=""action""/>
<ul>
<li class=""pnl-speedratio"" title=""speedratio"">
<span class=""speedratio"">100</span>
</li>
</ul>
</div>
</body>
</html>";
			}
			else if(request.RawUrl == "/rw/panel/opmode")
			{
				return @"<?xml version=""1.0"" encoding=""UTF-8""?>
<html xmlns=""http://www.w3.org/1999/xhtml"">
<head>
<title>panel</title>
<base href= ""http://localhost/rw/panel/opmode/"" />
</head>
<body>
<div class=""state"">
<a href= """" rel=""self""/>
<a href= ""?action=show"" rel=""action""/>
<ul>
<li class=""pnl-opmode"" title=""opmode"">
<span class=""opmode"">MANR</span>
</li>
</ul>
</div>
</body>
</html>";
			}
			else if(request.RawUrl == "/rw/panel/ctrlstate")
			{
				return @"<?xml version=""1.0"" encoding=""UTF-8""?>
<html xmlns=""http://www.w3.org/1999/xhtml"">
<head>
<title>panel</title>
<base href= ""http://localhost/rw/panel/ctrlstate/"" />
</head>
<body>
<div class=""state"">
<a href= """" rel=""self""/>
<a href= ""?action=show"" rel=""action""/>
<ul>
<li class=""pnl-ctrlstate"" title=""ctrlstate"">
<span class=""ctrlstate"">motoron</span>
</li>
</ul>
</div>
</body>
</html>";
			}
			else if(request.RawUrl == "/rw/iosystem/devices/D652_10" || request.RawUrl == "/rw/iosystem/devices/BK5250")
			{
				return @"<?xml version=""1.0"" encoding=""UTF-8""?>
<html xmlns=""http://www.w3.org/1999/xhtml"" xml:lang=""en"" lang=""en"">
	<head><title>io</title>
	<base href=""http://localhost/rw/iosystem/""/>
</head>
<body>
<div class=""state"">
	<a href=""devices/Local/PANEL"" rel=""self""></a>
	<a href= ""devices/Local/PANEL?action=show"" rel=""action""></a>
	<ul>
	<li class=""ios-device"" title=""Local/PANEL"">
		<a href=""networks/Local""  rel=""network""/>
		<span class=""name"">PANEL</span>
		<span class=""lstate"">enabled</span>
		<span class=""pstate"">running</span>
		<span class=""address"">-</span>
		<span class=""indata"">1FFFE063</span>
		<span class=""inmask"">FFFFFFFF</span>
		<span class=""outdata"">0000000E</span>
		<span class=""outmask"">FFFFFFFF</span>
	</li>
	</ul>
</div>
</body>
</html>";
			}
			else if(request.RawUrl == "/rw/elog/0?lang=zh&amp;resource=title")
			{
				return @"<?xml version=""1.0"" encoding=""utf-8""?>
<html xmlns=""http://www.w3.org/1999/xhtml"">
  <head>
	<title>Elog</title>
	<base href=""http://localhost/rw/elog/""/>
  </head>
  <body>

	<div class=""state"">      
	  <a href=""0"" rel=""self""></a>    
	  <a href= ""0?action=show"" rel=""action""/>
	  <ul>                        
		<li class=""elog-message-li"" title=""/rw/elog/0/5"">
			<a href=""0/5"" rel=""self""></a>          
			<span class=""msgtype"">1</span>
			<span class=""code"">10015</span>
			<span class=""src-name"">MC0</span>
			<span class=""tstamp"">2013-09-08 T 11:22:09</span>
			<span class=""argc"">0</span> 
		</li>
		<li class=""elog-message-li"" title=""/rw/elog/0/4"">
			<a href=""0/4"" rel=""self""></a>          
			<span class=""msgtype"">1</span>
			<span class=""code"">10013</span>
			<span class=""src-name"">MC0</span>
			<span class=""tstamp"">2013-09-08 T 11:22:09</span>
			<span class=""argc"">0</span> 
		</li>
		<li class=""elog-message-li"" title=""/rw/elog/0/3"">
			<a href=""0/3"" rel=""self""></a>          
			<span class=""msg-type"">1</span>
			<span class=""code"">10002</span>
			<span class=""src-name"">MC0</span>
			<span class=""tstamp"">2013-09-08 T 11:22:07</span>
			<span class=""argc"">2</span> 
			<span class=""arg1"" type=""string"">TRAFO</span>
			<span class=""arg2"" type=""string"">trafo_dm1</span>
		</li>
	  </ul>       
	</div>
  </body>
</html>";
			}
			else if(request.RawUrl == "/rw/iosystem/signals/Local/DRV_1/DRV1K1")
			{
				return @"<?xml version=""1.0"" encoding=""UTF-8""?>
<html
  xmlns=""http://www.w3.org/1999/xhtml"">
  <head>
	<title>io</title>
	<base href=""http://localhost/rw/iosystem/""/>
  </head>
  <body>
	<div class=""state"">
	  <a href=""signals/Local/DRV_1/DRV1K1"" rel=""self""></a>
	  <a href=""signals/Local/DRV_1/DRV1K1?action=show"" rel=""action""></a>
	  <ul>
		<li class=""ios-signal"" title=""Local/DRV_1/DRV1K1"">
		  <a href=""devices/DRV_1"" rel=""device""></a>
		  <span class=""name"">DRV1K1</span>
		  <span class=""type"">DO</span>
		  <span class=""category"">safety</span>
		  <span class=""lvalue"">0</span>
		  <span class=""lstate"">not simulated</span>
		  <span class=""unitnm"">DRV_1</span>
		  <span class=""phstate"">valid</span>
		  <span class=""pvalue"">0</span>
		  <span class=""ltime-sec"">0</span>
		  <span class=""ltime-microsec"">0</span>
		  <span class=""ptime-sec"">0</span>
		  <span class=""ptime-microsec"">0</span>
		  <span class=""quality"">1</span>
		</li>
	  </ul>
	</div>
  </body>
</html>";
			}
			else if(request.RawUrl == "/rw/rapid/execution")
			{
				return @"    <?xml version=""1.0"" encoding=""UTF-8""?>
	<html xmlns=""http://www.w3.org/1999/xhtml"">
	<head>
		<title>rapid</title>
		<base href=""http://localhost/rw/rapid/""/>
	</head>
	<body>
		<div class=""state"">
			<a href=""execution"" rel=""self""></a>
			<a href=""execution?action=show"" rel=""action""></a>
			<ul>
				<li class=""rap-execution"" title=""execution"">
					<span class=""ctrlexecstate"">stopped</span>
					<span class=""cycle"">forever</span>
				</li>
			</ul>
		</div>
	</body>
	</html>";
			}
			else if(request.RawUrl == "/rw/rapid/tasks")
			{
				return @"<?xml version=""1.0"" encoding=""UTF-8""?>
<html xmlns=""http://www.w3.org/1999/xhtml"">
<head>
<title>rapid</title>
<base href=""http://127.0.0.1/rw/rapid/""/>
</head>
<body>
<div class=""state"">
<a href=""tasks"" rel=""self""/>
<a href= ""tasks?action=show"" rel=""action""/>
<ul>
<li class=""rap-task-li"" title=""T_ROB1"">
<a href= ""tasks/T_ROB1"" rel=""self""/>
<span class=""name"">T_ROB1</span>
<span class=""type"">norm</span>
<span class=""taskstate"">link</span>
<span class=""excstate"">read</span>
<span class=""active"">On</span>
<span class=""motiontask"">TRUE</span>
</li>
<li class=""rap-task-li"" title=""T_ROB2"">
<a href= ""tasks/T_ROB2"" rel=""self""/>
<span class=""name"">T_ROB2</span>
<span class=""type"">norm</span>
<span class=""taskstate"">link</span>
<span class=""excstate"">read</span>
<span class=""active"">On</span>
<span class=""motiontask"">TRUE</span>
</li>
</ul>
</div>
</body>
</html>";
			}


#if NET20 || NET35
			return base.HandleRequest( request, response, data );
#else
			return await base.HandleRequest( request, response, data );
#endif
		}


		private string userName = "Default User";
		private string password = "robotics";

		/// <inheritdoc/>
		public override string ToString( ) => $"ABBWebApiServer[{Port}]";

	}
}
