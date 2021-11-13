using System;
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
		}



		private void FormLoad_Load( object sender, EventArgs e )
		{

			dockPanel1.Theme = vS2015BlueTheme1;



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

			TreeViewIni( );

			new FormIndex( ).Show( dockPanel1 );
			//new FormHslMap( ).Show( dockPanel1 );

			LoadDeviceList( );

			timer = new Timer( );
			timer.Interval = 1000;
			timer.Tick += Timer_Tick;
			timer.Start( );

			deleteDeviceToolStripMenuItem.Click += DeleteDeviceToolStripMenuItem_Click;
		}

		private void TreeView2_MouseClick( object sender, MouseEventArgs e )
		{
			if(e.Button == MouseButtons.Right)
			{
			}
		}

		private void Timer_Tick( object sender, EventArgs e )
		{
			if(curpcp != null)
			{
				string RamInfo = (curpcp.NextValue( ) / MB_DIV).ToString( "F1" ) + "MB";
			}
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
				Text = "山进智能";
				tabPage1.Text = "所有设备列表";
			}
			else
			{
				Text = "SG Test Tool";
				tabPage1.Text = "All Devices";
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

			// Panasonic PLC
			TreeNode panasonicPlc = new TreeNode( "Panasonic Plc[松下]", 11, 11 );
			panasonicPlc.Nodes.Add( GetTreeNodeByIndex( "Mewtocol",               11, typeof( FormPanasonicMew ) ) );
			treeView1.Nodes.Add( panasonicPlc );
		
			// Fatek 永宏PLC
			TreeNode fatekNode = new TreeNode( "Fatek Plc[永宏]", 22, 22 );
			fatekNode.Nodes.Add( GetTreeNodeByIndex( "programe [编程口]", 22, typeof( FormFatekPrograme ) ) );
			treeView1.Nodes.Add( fatekNode );

			
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
			foreach (var item in deviceList.GetDevices.Elements( ))
			{
				string name = item.Attribute( "Name" ).Value;
			}
		}

		private void DeleteDeviceToolStripMenuItem_Click( object sender, EventArgs e )
		{
			
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
			
		}

		private void FormSelect_FormClosing( object sender, FormClosingEventArgs e )
		{
			mqttClient?.ConnectClose( );
		}
	}

	
	
}
