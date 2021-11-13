using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using HslCommunication;
using HslCommunication.Profinet.Panasonic;
using System.Threading;
using System.IO.Ports;
using System.Xml.Linq;

namespace HslCommunicationDemo
{
	public partial class FormPanasonicMew : HslFormContent
	{
		public FormPanasonicMew( )
		{
			InitializeComponent( );
		}


		private PanasonicMewtocol panasonicMewtocol = null;


		private void FormSiemens_Load( object sender, EventArgs e )
		{
			panel2.Enabled = false;
			comboBox1.SelectedIndex = 0;


			

			comboBox3.DataSource = SerialPort.GetPortNames( );
			try
			{
				comboBox3.SelectedIndex = 0;
			}
			catch
			{
				comboBox3.Text = "COM3";
			}

			Language( Program.Language );
		}

		private void Language( int language )
		{
			if (language == 2)
			{
				Text = "Panasonic Read PLC Demo";

				label1.Text = "Com:";
				label3.Text = "baudRate:";
				label22.Text = "DataBit";
				label23.Text = "StopBit";
				label24.Text = "parity";
				label21.Text = "station";
				button1.Text = "Connect";
				button2.Text = "Disconnect";

				comboBox1.DataSource = new string[] { "None", "Odd", "Even" };
			}
		}



		private void FormSiemens_FormClosing( object sender, FormClosingEventArgs e )
		{

		}
		

		#region Connect And Close



		private void button1_Click( object sender, EventArgs e )
		{
			if(!int.TryParse(textBox2.Text,out int baudRate ))
			{
				MessageBox.Show( DemoUtils.BaudRateInputWrong );
				return;
			}

			if (!int.TryParse( textBox16.Text, out int dataBits ))
			{
				MessageBox.Show( DemoUtils.DataBitsInputWrong );
				return;
			}

			if (!int.TryParse( textBox17.Text, out int stopBits ))
			{
				MessageBox.Show( DemoUtils.StopBitInputWrong );
				return;
			}


			if (!byte.TryParse(textBox15.Text,out byte station))
			{
				MessageBox.Show( "Plc Station input wrong!" );
				return;
			}

			panasonicMewtocol?.Close( );
			panasonicMewtocol = new PanasonicMewtocol( station );

			
			try
			{
				panasonicMewtocol.SerialPortInni( sp =>
				 {
					 sp.PortName = comboBox3.Text;
					 sp.BaudRate = baudRate;
					 sp.DataBits = dataBits;
					 sp.StopBits = stopBits == 0 ? System.IO.Ports.StopBits.None : (stopBits == 1 ? System.IO.Ports.StopBits.One : System.IO.Ports.StopBits.Two);
					 sp.Parity = comboBox1.SelectedIndex == 0 ? System.IO.Ports.Parity.None : (comboBox1.SelectedIndex == 1 ? System.IO.Ports.Parity.Odd : System.IO.Ports.Parity.Even);
				 } );
				panasonicMewtocol.Open( );

				button2.Enabled = true;
				button1.Enabled = false;
				panel2.Enabled = true;
				userControlReadWriteOp1.SetReadWriteNet( panasonicMewtocol, "R0", false );
			}
			catch (Exception ex)
			{
				MessageBox.Show( ex.Message );
			}
		}

		private void button2_Click( object sender, EventArgs e )
		{
			// 断开连接
			panasonicMewtocol.Close( );
			button2.Enabled = false;
			button1.Enabled = true;
			panel2.Enabled = false;
		}
		

		

		#endregion
		
	
		#region Test Function
		
		private void Test1()
		{
			OperateResult<bool[]> read = panasonicMewtocol.ReadBool( "100", 10 );
			if(read.IsSuccess)
			{
				bool coil_100 = read.Content[0];
				// and so on 
				bool coil_109 = read.Content[9];
			}
			else
			{
				// failed
				string err = read.Message;
			}
		}


		private void Test2()
		{
			bool[] values = new bool[] { true, false, false, false, true, true, false, true, false, false };
			OperateResult write = panasonicMewtocol.Write( "100", values );
			if (write.IsSuccess)
			{
				// success
			}
			else
			{
				// failed
				string err = write.Message;
			}

			HslCommunication.Core.IByteTransform ByteTransform = new HslCommunication.Core.ReverseWordTransform( );
		}


		#endregion



		public override void SaveXmlParameter( XElement element )
		{
			element.SetAttributeValue( DemoDeviceList.XmlCom, comboBox3.Text );
			element.SetAttributeValue( DemoDeviceList.XmlBaudRate, textBox2.Text );
			element.SetAttributeValue( DemoDeviceList.XmlDataBits, textBox16.Text );
			element.SetAttributeValue( DemoDeviceList.XmlStopBit, textBox17.Text );
			element.SetAttributeValue( DemoDeviceList.XmlParity, comboBox1.SelectedIndex );
			element.SetAttributeValue( DemoDeviceList.XmlStation, textBox15.Text );
		}

		public override void LoadXmlParameter( XElement element )
		{
			base.LoadXmlParameter( element );
			comboBox3.Text = element.Attribute( DemoDeviceList.XmlCom ).Value;
			textBox2.Text = element.Attribute( DemoDeviceList.XmlBaudRate ).Value;
			textBox16.Text = element.Attribute( DemoDeviceList.XmlDataBits ).Value;
			textBox17.Text = element.Attribute( DemoDeviceList.XmlStopBit ).Value;
			comboBox1.SelectedIndex = int.Parse( element.Attribute( DemoDeviceList.XmlParity ).Value );
			textBox15.Text = element.Attribute( DemoDeviceList.XmlStation ).Value;
		}

		private void userControlHead1_SaveConnectEvent_1( object sender, EventArgs e )
		{
			userControlHead1_SaveConnectEvent( sender, e );
		}

        private void userControlReadWriteOp1_Load(object sender, EventArgs e)
        {

        }
    }
}
