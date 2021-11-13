﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using System.Xml.Linq;
using HslCommunication.Core;
using HslCommunication.Core.Net;
using HslCommunication.Profinet.Siemens;
using HslCommunicationDemo.Control;

namespace HslCommunicationDemo
{
	public partial class FormSelect : Form
	{
		public static Color ThemeColor = Color.FromArgb( 64, 64, 64 );

		public FormSelect( )
		{
			InitializeComponent( );
			Form = this;

			imageList = new ImageList( );
			imageList.Images.Add( "Method_636",            Properties.Resources.Method_636 );        // 0
			imageList.Images.Add( "ab",                    Properties.Resources.ab );                // 1
			imageList.Images.Add( "fujifilm",              Properties.Resources.fujifilm );          // 2
			imageList.Images.Add( "HslCommunication",      Properties.Resources.HslCommunication );  // 3
			imageList.Images.Add( "idcard",                Properties.Resources.idcard );            // 4
			imageList.Images.Add( "inovance",              Properties.Resources.inovance );          // 5
			imageList.Images.Add( "keyence",               Properties.Resources.keyence );           // 6
			imageList.Images.Add( "ls",                    Properties.Resources.ls );                // 7
			imageList.Images.Add( "melsec",                Properties.Resources.melsec );            // 8
			imageList.Images.Add( "modbus",                Properties.Resources.modbus );            // 9
			imageList.Images.Add( "omron",                 Properties.Resources.omron );             // 10
			imageList.Images.Add( "panasonic",             Properties.Resources.panasonic );         // 11
			imageList.Images.Add( "redis",                 Properties.Resources.redis );             // 12
			imageList.Images.Add( "schneider",             Properties.Resources.schneider );         // 13
			imageList.Images.Add( "siemens",               Properties.Resources.siemens );           // 14
			imageList.Images.Add( "debug",                 Properties.Resources.debug );             // 15
			imageList.Images.Add( "barcode",               Properties.Resources.barcode );           // 16
			imageList.Images.Add( "mqtt",                  Properties.Resources.mqtt );              // 17
			imageList.Images.Add( "toledo",                Properties.Resources.toledo );            // 18
			imageList.Images.Add( "robot",                 Properties.Resources.robot );             // 19
			imageList.Images.Add( "beckhoff",              Properties.Resources.beckhoff );          // 20
			imageList.Images.Add( "abb",                   Properties.Resources.abb );               // 21
			imageList.Images.Add( "fatek",                 Properties.Resources.fatek );             // 22
			imageList.Images.Add( "kuka",                  Properties.Resources.kuka );              // 23
			imageList.Images.Add( "efort",                 Properties.Resources.efort );             // 24
			imageList.Images.Add( "fanuc",                 Properties.Resources.fanuc );             // 25
			imageList.Images.Add( "Class_489",             Properties.Resources.Class_489 );         // 26
			imageList.Images.Add( "zkt",                   Properties.Resources.zkt );               // 27
			imageList.Images.Add( "websocket",             Properties.Resources.websocket );         // 28
			imageList.Images.Add( "yaskawa",               Properties.Resources.yaskawa );           // 29
			imageList.Images.Add( "xinje",                 Properties.Resources.xinje );             // 30
			imageList.Images.Add( "yokogawa",              Properties.Resources.yokogawa );          // 31
			imageList.Images.Add( "delta",                 Properties.Resources.delta );             // 32
			imageList.Images.Add( "ge",                    Properties.Resources.ge );                // 33
			imageList.Images.Add( "yamatake",              Properties.Resources.Yamatake );          // 34
			imageList.Images.Add( "rkc",                   Properties.Resources.rkc );               // 35


			treeView1.ImageList = imageList;
			treeView2.ImageList = imageList;
		}



		private void FormLoad_Load( object sender, EventArgs e )
		{

			dockPanel1.Theme = vS2015BlueTheme1;


			ThemeColor = menuStrip1.BackColor;
			verisonToolStripMenuItem.Text = "Version: " + HslCommunication.BasicFramework.SoftBasic.FrameworkVersion.ToString( );

			if (Settings1.Default.language == 1)
			{
				if (System.Globalization.CultureInfo.CurrentCulture.ToString( ).StartsWith( "zh" ))
				{
					Program.Language = 1;
					Language( Program.Language );
				}
				else
				{
					HslCommunication.StringResources.SeteLanguageEnglish( );
					Program.Language = 2;
					Language( Program.Language );
				}
			}
			else
			{
				Program.Language = 2;
				HslCommunication.StringResources.SeteLanguageEnglish( );
				Language( Program.Language );
			}

			support赞助ToolStripMenuItem.Click += Support赞助ToolStripMenuItem_Click;
			TreeViewIni( );

			new FormIndex( ).Show( dockPanel1 );
			//new FormHslMap( ).Show( dockPanel1 );

			LoadDeviceList( );

			timer = new Timer( );
			timer.Interval = 1000;
			timer.Tick += Timer_Tick;
			timer.Start( );

			treeView2.MouseClick += TreeView2_MouseClick;
			deleteDeviceToolStripMenuItem.Click += DeleteDeviceToolStripMenuItem_Click;
		}

		private void TreeView2_MouseClick( object sender, MouseEventArgs e )
		{
			if(e.Button == MouseButtons.Right)
			{
				treeView2.SelectedNode = treeView2.GetNodeAt( e.Location );
				contextMenuStrip1.Show( treeView2, e.Location );
			}
		}

		private void Timer_Tick( object sender, EventArgs e )
		{
			if(curpcp != null)
			{
				string RamInfo = (curpcp.NextValue( ) / MB_DIV).ToString( "F1" ) + "MB";
				label2.Text = "Ram: " + RamInfo;
			}
			label1.Text = $"Timeout:{HslCommunication.HslTimeOut.TimeOutCheckCount}  Lock:{SimpleHybirdLock.SimpleHybirdLockCount}  Wait:{SimpleHybirdLock.SimpleHybirdLockWaitCount}";
		}

		private HslCommunication.MQTT.MqttClient mqttClient;
		private System.Windows.Forms.Timer timer;
		private Process cur = null;
		private PerformanceCounter curpcp = null;
		private const int MB_DIV = 1024 * 1024;

		private void Support赞助ToolStripMenuItem_Click( object sender, EventArgs e )
		{
			using (HslCommunication.BasicFramework.FormSupport form = new HslCommunication.BasicFramework.FormSupport( ))
			{
				form.ShowDialog( );
			}
		}

		private void Language( int language )
		{
			if (language == 1)
			{
				Text = "HslCommunication 测试工具";
				免责条款ToolStripMenuItem.Text = "全国使用分布";
				论坛toolStripMenuItem.Text = "博客";
				日志ToolStripMenuItem.Text = "API 文档";
				//授权ToolStripMenuItem.Text = "授权";
				tabPage1.Text = "所有设备列表";
				tabPage2.Text = "保存列表";
				deleteDeviceToolStripMenuItem.Text = "删除设备";
			}
			else
			{
				Text = "HslCommunication Test Tool";
				论坛toolStripMenuItem.Text = "Blog";
				免责条款ToolStripMenuItem.Text = "China Map";
				日志ToolStripMenuItem.Text = "API Doc";
				//授权ToolStripMenuItem.Text = "Authorize";
				tabPage1.Text = "All Devices";
				tabPage2.Text = "Save Devices";
			}
		}

		private void 论坛toolStripMenuItem_Click(object sender, EventArgs e)
		{
			try
			{
				System.Diagnostics.Process.Start( "http://blog.hslcommunication.cn/" );
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
		}


		private void OpenWebside( string url )
		{
			try
			{
				System.Diagnostics.Process.Start( url );
			}
			catch (Exception ex)
			{
				MessageBox.Show( ex.Message );
			}
		}

		private void blogsToolStripMenuItem_Click( object sender, EventArgs e )
		{
			OpenWebside( "http://www.cnblogs.com/dathlin/p/7703805.html" );
		}

		private void webSideToolStripMenuItem_Click( object sender, EventArgs e )
		{
			OpenWebside( "http://www.hslcommunication.cn/" );
		}

		private void toolStripMenuItem1_Click( object sender, EventArgs e )
		{
			OpenWebside( "http://www.hslcommunication.cn/MesDemo" );
		}
		private void 简体中文ToolStripMenuItem_Click( object sender, EventArgs e )
		{
			// 简体中文
			HslCommunication.StringResources.SetLanguageChinese( );
			Program.Language = 1;
			Settings1.Default.language = Program.Language;
			Settings1.Default.Save( );
			Language( Program.Language );
			MessageBox.Show( "已选择中文" );
		}

		private void englishToolStripMenuItem_Click( object sender, EventArgs e )
		{
			// English
			HslCommunication.StringResources.SeteLanguageEnglish( );
			Program.Language = 2;
			Settings1.Default.language = Program.Language;
			Settings1.Default.Save( );
			Language( Program.Language );
			MessageBox.Show( "Select English!" );
		}

		private void 日志ToolStripMenuItem_Click( object sender, EventArgs e )
		{
			OpenWebside( "http://api.hslcommunication.cn" );
		}


		private void FormLoad_Shown( object sender, EventArgs e )
		{
			System.Threading.ThreadPool.QueueUserWorkItem( new System.Threading.WaitCallback( ThreadPoolCheckVersion ), null );
		}

		private void ThreadPoolCheckVersion( object obj )
		{
			System.Threading.Thread.Sleep( 100 );
			mqttClient = new HslCommunication.MQTT.MqttClient( new HslCommunication.MQTT.MqttConnectionOptions( )
			{
				IpAddress = "118.24.36.220",
				Port = 1883,
				ClientId = "HslDemo"
			} );
			mqttClient.ConnectServer( );
			HslCommunication.Enthernet.NetSimplifyClient simplifyClient = new HslCommunication.Enthernet.NetSimplifyClient( "118.24.36.220", 18467 );
			HslCommunication.OperateResult<HslCommunication.NetHandle, string> read = simplifyClient.ReadCustomerFromServer( 1, HslCommunication.BasicFramework.SoftBasic.FrameworkVersion.ToString( ) );
			if (read.IsSuccess)
			{
				HslCommunication.BasicFramework.SystemVersion version = new HslCommunication.BasicFramework.SystemVersion( read.Content2 );
				if (version > HslCommunication.BasicFramework.SoftBasic.FrameworkVersion)
				{
					// 有更新
					Invoke( new Action( ( ) =>
					 {
						 if (MessageBox.Show( "New version on server：" + read.Content2 + Environment.NewLine + " Start update?", "Version Check", MessageBoxButtons.YesNo ) == DialogResult.Yes)
						 {
							 try
							 {
								 System.Diagnostics.Process.Start( Application.StartupPath + "\\AutoUpdate.exe" );
								 System.Threading.Thread.Sleep( 50 );
								 Close( );
							 }
							 catch(Exception ex)
							 {
								 MessageBox.Show( "更新软件丢失，无法启动更新： " + ex.Message );
							 }
						 }
					 } ) );
				}
			}

			try
			{
				cur = Process.GetCurrentProcess( );
				curpcp = new PerformanceCounter( "Process", "Working Set - Private", cur.ProcessName );
			}
			catch
			{

			}
		}

		private void 免责条款ToolStripMenuItem_Click( object sender, EventArgs e )
		{
			
		}

		private void authorization授权ToolStripMenuItem_Click( object sender, EventArgs e )
		{
		}

		private ImageList imageList;
		private Dictionary<string, int> formIconImageIndex = new Dictionary<string, int>( );

		private TreeNode GetTreeNodeByIndex( string name, int index, Type form )
		{
			formIconImageIndex.Add( form.Name, index );
			return new TreeNode( name, index, index )
			{
				Tag = form
			};
		}

		private void TreeViewIni( )
		{
			// 三菱PLC相关
			TreeNode melsecNode = new TreeNode( "Melsec Plc [三菱]",   8, 8 );
			melsecNode.Nodes.Add( GetTreeNodeByIndex( "EtherNet/IP(CIP)",    8, typeof( FormMelsecCipNet ) ) );
			melsecNode.Nodes.Add( GetTreeNodeByIndex( "A-1E (Binary)",       8, typeof( FormMelsec1EBinary ) ) );
			melsecNode.Nodes.Add( GetTreeNodeByIndex( "A-1E (ASCII)",        8, typeof( FormMelsec1EAscii ) ) );
			melsecNode.Nodes.Add( GetTreeNodeByIndex( "A-1E Server",         8, typeof( FormMcA1EServer ) ) );
			melsecNode.Nodes.Add( GetTreeNodeByIndex( "MC (Binary)",         8, typeof( FormMelsecBinary ) ) );
			melsecNode.Nodes.Add( GetTreeNodeByIndex( "MC Udp(Binary)",      8, typeof( FormMelsecUdp ) ) );
			melsecNode.Nodes.Add( GetTreeNodeByIndex( "MC (ASCII)",          8, typeof( FormMelsecAscii ) ) );
			melsecNode.Nodes.Add( GetTreeNodeByIndex( "MC Udp(ASCII)",       8, typeof( FormMelsecAsciiUdp ) ) );
			melsecNode.Nodes.Add( GetTreeNodeByIndex( "MC-R (Binary)",       8, typeof( FormMelsecRBinary ) ) );
			melsecNode.Nodes.Add( GetTreeNodeByIndex( "Fx Serial【编程口】",  8, typeof( FormMelsecSerial ) ) );
			melsecNode.Nodes.Add( GetTreeNodeByIndex( "Fx Serial OverTcp",   8, typeof( FormMelsecSerialOverTcp ) ) );
			melsecNode.Nodes.Add( GetTreeNodeByIndex( "Fx Links【485】",     8, typeof( FormMelsecLinks ) ) );
			melsecNode.Nodes.Add( GetTreeNodeByIndex( "Fx Links OverTcp",    8, typeof( FormMelsecLinksOverTcp ) ) );
			melsecNode.Nodes.Add( GetTreeNodeByIndex( "A-3C (串口)",         8, typeof( FormMelsec3C ) ) );
			melsecNode.Nodes.Add( GetTreeNodeByIndex( "A-3C OverTcp",        8, typeof( FormMelsec3COverTcp ) ) );
			melsecNode.Nodes.Add( GetTreeNodeByIndex( "A-3C Server",         8, typeof( FormMcA3CServer ) ) );
			melsecNode.Nodes.Add( GetTreeNodeByIndex( "Mc Virtual Server",   8, typeof( FormMcServer ) ) );
			melsecNode.Nodes.Add( GetTreeNodeByIndex( "Mc Udp Server",       8, typeof( FormMcUdpServer ) ) );
			treeView1.Nodes.Add( melsecNode );

			// 西门子PLC相关
			TreeNode siemensNode = new TreeNode( "Siemens Plc [西门子]", 14, 14 );
			siemensNode.Nodes.Add( GetTreeNodeByIndex( "S7-S1200",           14, typeof( FormSiemensS1200 ) ) );
			siemensNode.Nodes.Add( GetTreeNodeByIndex( "S7-S1500",           14, typeof( FormSiemensS1500 ) ) );
			siemensNode.Nodes.Add( GetTreeNodeByIndex( "S7-S300",            14, typeof( FormSiemensS300 ) ) );
			siemensNode.Nodes.Add( GetTreeNodeByIndex( "S7-S400",            14, typeof( FormSiemensS400 ) ) );
			siemensNode.Nodes.Add( GetTreeNodeByIndex( "S7-S200",            14, typeof( FormSiemensS200 ) ) );
			siemensNode.Nodes.Add( GetTreeNodeByIndex( "S7-S200 smart",      14, typeof( FormSiemensS200Smart ) ) );
			siemensNode.Nodes.Add( GetTreeNodeByIndex( "Fetch/Write",        14, typeof( FormSiemensFW ) ) );
			siemensNode.Nodes.Add( GetTreeNodeByIndex( "WebApi",             14, typeof( FormSiemensWebApi ) ) );
			siemensNode.Nodes.Add( GetTreeNodeByIndex( "PPI",                14, typeof( FormSiemensPPI ) ) );
			siemensNode.Nodes.Add( GetTreeNodeByIndex( "PPI OverTcp",        14, typeof( FormSiemensPPIOverTcp ) ) );
			siemensNode.Nodes.Add( GetTreeNodeByIndex( "MPI",                14, typeof( FormSiemensMPI ) ) );
			siemensNode.Nodes.Add( GetTreeNodeByIndex( "S7 Virtual Server",  14, typeof( FormS7Server ) ) );
			siemensNode.Nodes.Add( GetTreeNodeByIndex( "Fetch Write Server", 14, typeof( FormFetchWriteServer ) ) );
			siemensNode.Nodes.Add( GetTreeNodeByIndex( "Siemens DTU",        14, typeof( FormSiemensDTU ) ) );
			treeView1.Nodes.Add( siemensNode );

			// 欧姆龙PLC相关
			TreeNode omronNode = new TreeNode( "Omron Plc[欧姆龙]", 10, 10 );
			omronNode.Nodes.Add( GetTreeNodeByIndex( "Fins Tcp",                       10, typeof( FormOmron ) ) );
			omronNode.Nodes.Add( GetTreeNodeByIndex( "Fins Udp",                       10, typeof( FormOmronUdp ) ) );
			omronNode.Nodes.Add( GetTreeNodeByIndex( "EtherNet/IP(CIP)",               10, typeof( FormOmronCip ) ) );
			omronNode.Nodes.Add( GetTreeNodeByIndex( "Connected CIP",                  10, typeof( FormOmronConnectedCip ) ) );
			omronNode.Nodes.Add( GetTreeNodeByIndex( "HostLink 【串口】",              10, typeof( FormOmronHostLink ) ) );
			omronNode.Nodes.Add( GetTreeNodeByIndex( "HostLink OverTcp",               10, typeof( FormOmronHostLinkOverTcp ) ) );
			omronNode.Nodes.Add( GetTreeNodeByIndex( "HostLink C-Mode",                10, typeof( FormOmronHostLinkCMode ) ) );
			omronNode.Nodes.Add( GetTreeNodeByIndex( "C-Mode OverTcp",                 10, typeof( FormOmronHostLinkCModeOverTcp ) ) );
			omronNode.Nodes.Add( GetTreeNodeByIndex( "Fins Virtual Server",            10, typeof( FormOmronServer ) ) );
			omronNode.Nodes.Add( GetTreeNodeByIndex( "Fins Udp Server",                10, typeof( FormOmronUdpServer ) ) );
			omronNode.Nodes.Add( GetTreeNodeByIndex( "HostLink Server",                10, typeof( FormOmronHostLinkServer ) ) );
			treeView1.Nodes.Add( omronNode );


			// Keyence PLC
			TreeNode keyencePlc = new TreeNode( "Keyence Plc[基恩士]", 6, 6 );
			keyencePlc.Nodes.Add( GetTreeNodeByIndex( "MC-3E (Binary)",        6, typeof( FormKeyenceBinary ) ) );
			keyencePlc.Nodes.Add( GetTreeNodeByIndex( "MC-3E (ASCII)",         6, typeof( FormKeyenceAscii ) ) );
			keyencePlc.Nodes.Add( GetTreeNodeByIndex( "Nano (ASCII)",          6, typeof( FormKeyenceNanoSerial ) ) );
			keyencePlc.Nodes.Add( GetTreeNodeByIndex( "Nano OverTcp",          6, typeof( FormKeyenceNanoSerialOverTcp ) ) );
			keyencePlc.Nodes.Add( GetTreeNodeByIndex( "Nano Server",           6, typeof( FormKeyenceNanoServer ) ) );
			keyencePlc.Nodes.Add( GetTreeNodeByIndex( "SR2000 [读码]",         6, typeof( FormKeyenceSR2000 ) ) );
			treeView1.Nodes.Add( keyencePlc );

			// Panasonic PLC
			TreeNode panasonicPlc = new TreeNode( "Panasonic Plc[松下]", 11, 11 );
			panasonicPlc.Nodes.Add( GetTreeNodeByIndex( "MC-3E (Binary)",         11, typeof( FormPanasonicBinary ) ) );
			panasonicPlc.Nodes.Add( GetTreeNodeByIndex( "Mewtocol",               11, typeof( FormPanasonicMew ) ) );
			panasonicPlc.Nodes.Add( GetTreeNodeByIndex( "Mewtocol OverTcp",       11, typeof( FormPanasonicMewOverTcp ) ) );
			panasonicPlc.Nodes.Add( GetTreeNodeByIndex( "Mewtocol Server",        11, typeof( FormPanasonicMewtocolServer ) ) );
			treeView1.Nodes.Add( panasonicPlc );
		
			// Fatek 永宏PLC
			TreeNode fatekNode = new TreeNode( "Fatek Plc[永宏]", 22, 22 );
			fatekNode.Nodes.Add( GetTreeNodeByIndex( "programe [编程口]", 22, typeof( FormFatekPrograme ) ) );
			fatekNode.Nodes.Add( GetTreeNodeByIndex( "programe OverTcp", 22, typeof( FormFatekProgrameOverTcp ) ) );
			treeView1.Nodes.Add( fatekNode );

			// Fuji Plc
			TreeNode fujiNode = new TreeNode( "Fuji Plc[富士]", 2, 2 );
			fujiNode.Nodes.Add( GetTreeNodeByIndex( "SPB [编程口]", 2, typeof( FormFujiSPB ) ) );
			fujiNode.Nodes.Add( GetTreeNodeByIndex( "SPB OverTcp", 2, typeof( FormFujiSPBOverTcp ) ) );
			fujiNode.Nodes.Add( GetTreeNodeByIndex( "SPB Server", 2, typeof( FormFujiSPBServer ) ) );
			fujiNode.Nodes.Add( GetTreeNodeByIndex( "SPH Net", 2, typeof( FormFujiSPHNet ) ) );
			fujiNode.Nodes.Add( GetTreeNodeByIndex( "SPH Server", 2, typeof( FormFujiSPHServer ) ) );
			fujiNode.Nodes.Add( GetTreeNodeByIndex( "CommandST", 2, typeof( FormFujiCSTNet ) ) );
			fujiNode.Nodes.Add( GetTreeNodeByIndex( "CommandST Server", 2, typeof( FormFujiCSTServer ) ) );
			treeView1.Nodes.Add( fujiNode );

			// XinJE Plc
			TreeNode xinjeNode = new TreeNode( "XinJE Plc[信捷]", 30, 30 );
			xinjeNode.Nodes.Add( GetTreeNodeByIndex( "XinJE Serial", 30, typeof( FormXinJEXCSerial ) ) );
			xinjeNode.Nodes.Add( GetTreeNodeByIndex( "XinJE Serial OverTcp", 30, typeof( FormXinJESerialOverTcp ) ) );
			xinjeNode.Nodes.Add( GetTreeNodeByIndex( "XinJE TCP", 30, typeof( FormXinJETcpNet ) ) );
			treeView1.Nodes.Add( xinjeNode );

			// delta Plc
			TreeNode deltaNode = new TreeNode( "Delta Plc[台达]", 32, 32 );
			deltaNode.Nodes.Add( GetTreeNodeByIndex( "Dvp Serial", 32, typeof( FormDeltaDvpSerial ) ) );
			deltaNode.Nodes.Add( GetTreeNodeByIndex( "Dvp Serial Ascii", 32, typeof( FormDeltaDvpSerialAscii ) ) );
			deltaNode.Nodes.Add( GetTreeNodeByIndex( "Dvp Tcp Net", 32, typeof( FormDeltaDvpTcpNet ) ) );
			treeView1.Nodes.Add( deltaNode );
		
		}

		private void TreeView1_DoubleClick( object sender, EventArgs e )
		{
			TreeNode treeNode = treeView1.SelectedNode;
			if (treeNode == null) return;
			if (treeNode.Tag == null) return;

			if (treeNode.Tag is Type type)
			{
				HslFormContent hslForm = (HslFormContent)type.GetConstructors( )[0].Invoke( null );
				if (treeNode.ImageIndex >= 0)
					hslForm.Icon = Icon.FromHandle( ((Bitmap)imageList.Images[treeNode.ImageIndex]).GetHicon( ) );
				else
					hslForm.Icon = Icon.FromHandle( Properties.Resources.Method_636.GetHicon( ) );
				if (hslForm != null) hslForm.Show( dockPanel1 );
			}
		}

		private void treeView1_MouseClick( object sender, MouseEventArgs e )
		{
			if(e.Button == MouseButtons.Right)
			{
				TreeNode treeNode = treeView1.GetNodeAt(e.Location);
				if (treeNode != null) treeView1.SelectedNode = treeNode;
			}
		}

		private DemoDeviceList deviceList = new DemoDeviceList( );

		public void LoadDeviceList( )
		{
			if(File.Exists( Path.Combine( Application.StartupPath, "devices.xml" ) ))
			{
				deviceList.SetDevices( XElement.Load( Path.Combine( Application.StartupPath, "devices.xml" ) ) );
				RefreshSaveDevices( );
				tabControl1.SelectedTab = tabPage2;
			}
		}

		public void AddDeviceList(XElement element )
		{
			deviceList.AddDevice( element );
			RefreshSaveDevices( );
			File.WriteAllText( Path.Combine( Application.StartupPath, "devices.xml" ), deviceList.GetDevices.ToString( ) );
		}

		public void RefreshSaveDevices( )
		{
			treeView2.Nodes.Clear( );
			foreach (var item in deviceList.GetDevices.Elements( ))
			{
				string name = item.Attribute( "Name" ).Value;
				AddTreeNode(  treeView2, null, item, name );
			}
			treeView2.ExpandAll( );
		}

		private void DeleteDeviceToolStripMenuItem_Click( object sender, EventArgs e )
		{
			if (treeView2.SelectedNode == null) return;
			if (treeView2.SelectedNode.Tag == null) return;
			if (treeView2.SelectedNode.Tag is XElement element)
			{
				deviceList.DeleteDevice( element );
				RefreshSaveDevices( );
				File.WriteAllText( Path.Combine( Application.StartupPath, "devices.xml" ), deviceList.GetDevices.ToString( ) );
				MessageBox.Show( "Delete Success!" );
			}
		}


		private void AddTreeNode( TreeView treeView, TreeNode parent, XElement element, string key )
		{
			int index = key.IndexOf( ':' );
			if (index <= 0)
			{
				// 不存在冒号
				TreeNode node = new TreeNode( key );
				node.Tag = element;
				string type = element.Attribute( DemoDeviceList.XmlType ).Value;
				if (formIconImageIndex.ContainsKey( type ))
				{
					node.ImageIndex = formIconImageIndex[type];
					node.SelectedImageIndex = formIconImageIndex[type];
				}
				else
				{
					node.ImageIndex = 0;
					node.SelectedImageIndex = 0;
				}

				if (parent == null)
				{
					treeView.Nodes.Add( node );
				}
				else
				{
					parent.Nodes.Add( node );
				}
			}
			else
			{
				TreeNode parentNode = null;
				if (parent == null)
				{
					for (int i = 0; i < treeView.Nodes.Count; i++)
					{
						if (treeView.Nodes[i].Text == key.Substring( 0, index ))
						{
							parentNode = treeView.Nodes[i];
							break;
						}
					}
				}
				else
				{
					for (int i = 0; i < parent.Nodes.Count; i++)
					{
						if (parent.Nodes[i].Text == key.Substring( 0, index ))
						{
							parentNode = parent.Nodes[i];
							break;
						}
					}
				}


				if (parentNode == null)
				{
					parentNode = new TreeNode( key.Substring( 0, index ) );
					parentNode.ImageKey = "Class_489";
					parentNode.SelectedImageKey = "Class_489";
					AddTreeNode( treeView, parentNode, element, key.Substring( index + 1 ) );

					if(parent == null)
					{
						treeView.Nodes.Add( parentNode );
					}
					else
					{
						parent.Nodes.Add( parentNode );
					}
				}
				else
				{
					AddTreeNode( treeView, parentNode, element, key.Substring( index + 1 ) );
				}
			}
		}

		public static FormSelect Form { get; set; }
		public static Type[] formTypes = Assembly.GetExecutingAssembly( ).GetTypes( );

		private void treeView2_MouseDoubleClick( object sender, MouseEventArgs e )
		{
			TreeNode treeNode = treeView2.SelectedNode;
			if (treeNode == null) return;
			if (treeNode.Tag == null) return;

			if (treeNode.Tag is XElement element)
			{
				string type = element.Attribute( DemoDeviceList.XmlType ).Value;
				HslFormContent hslForm = null;
				// 读取类型
				foreach (var item in formTypes)
				{
					if(item.Name == type)
					{
						hslForm = (HslFormContent)item.GetConstructors( )[0].Invoke( null );
						break;
					}
				}

				if (hslForm != null)
				{
					if (treeNode.ImageIndex >= 0)
						hslForm.Icon = Icon.FromHandle( ((Bitmap)imageList.Images[treeNode.ImageIndex]).GetHicon( ) );
					else
						hslForm.Icon = Icon.FromHandle( Properties.Resources.Method_636.GetHicon( ) );

					hslForm.Show( dockPanel1 );
					hslForm.LoadXmlParameter( element );
				}
			}
		}

		private void FormSelect_FormClosing( object sender, FormClosingEventArgs e )
		{
			mqttClient?.ConnectClose( );
		}
	}

	public class FormSiemensS1200 : FormSiemens
	{
		public FormSiemensS1200( ) : base( SiemensPLCS.S1200 )
		{

		}

		protected override void OnLoad( EventArgs e )
		{
			base.OnLoad( e );
			this.userControlHead1.ProtocolInfo = "s7-1200";
		}
	}
	public class FormSiemensS1500 : FormSiemens
	{
		public FormSiemensS1500( ) : base( SiemensPLCS.S1500 )
		{

		}

		protected override void OnLoad( EventArgs e )
		{
			base.OnLoad( e );
			this.userControlHead1.ProtocolInfo = "s7-1500";
		}
	}
	public class FormSiemensS300 : FormSiemens
	{
		public FormSiemensS300( ) : base( SiemensPLCS.S300 )
		{

		}

		protected override void OnLoad( EventArgs e )
		{
			base.OnLoad( e );
			this.userControlHead1.ProtocolInfo = "s7-300";
		}
	}
	public class FormSiemensS400 : FormSiemens
	{
		public FormSiemensS400( ) : base( SiemensPLCS.S400 )
		{

		}

		protected override void OnLoad( EventArgs e )
		{
			base.OnLoad( e );
			this.userControlHead1.ProtocolInfo = "s7-400";
		}
	}
	public class FormSiemensS200 : FormSiemens200
	{
		public FormSiemensS200( ) : base( SiemensPLCS.S200 )
		{

		}

		protected override void OnLoad( EventArgs e )
		{
			base.OnLoad( e );
			this.userControlHead1.ProtocolInfo = "s7-200";
		}
	}
	public class FormSiemensS200Smart : FormSiemens200
	{
		public FormSiemensS200Smart( ) : base( SiemensPLCS.S200Smart )
		{

		}

		protected override void OnLoad( EventArgs e )
		{
			base.OnLoad( e );
			this.userControlHead1.ProtocolInfo = "s7-200Smart";
		}
	}
}
