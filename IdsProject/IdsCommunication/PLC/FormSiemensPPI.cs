﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using HslCommunication.Profinet;
using System.Threading;
using HslCommunication.Profinet.Siemens;
using HslCommunication;
using System.IO.Ports;
using System.Xml.Linq;

namespace HslCommunicationDemo
{
	public partial class FormSiemensPPI : HslFormContent
	{
		public FormSiemensPPI( )
		{
			InitializeComponent( );
			siemensPPI = new SiemensPPI( );
		}


		private SiemensPPI siemensPPI = null;

		private void FormSiemens_Load( object sender, EventArgs e )
		{
			panel2.Enabled = false;

			Language( Program.Language );
			comboBox3.DataSource = SerialPort.GetPortNames( );
			try
			{
				comboBox3.SelectedIndex = 0;
			}
			catch
			{
				comboBox3.Text = "COM3";
			}
			comboBox1.SelectedIndex = 2;
		}

		private void Language( int language )
		{
			if (language == 2)
			{
				Text = "Siemens Read PLC Demo";


				label1.Text = "parity:";
				label3.Text = "Stop bits";
				label27.Text = "Com:";
				label26.Text = "BaudRate";
				label25.Text = "Data bits";
				button1.Text = "Connect";
				button2.Text = "Disconnect";
				label21.Text = "Address:";

				label11.Text = "Address:";
				label12.Text = "length:";
				button25.Text = "Bulk Read";
				label13.Text = "Results:";


				groupBox3.Text = "Bulk Read test";
				groupBox4.Text = "Special function test";
			}
		}
		private void FormSiemens_FormClosing( object sender, FormClosingEventArgs e )
		{

		}
		
		#region Connect And Close



		private void button1_Click( object sender, EventArgs e )
		{
			if (!int.TryParse( textBox2.Text, out int baudRate ))
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


			siemensPPI?.Close( );
			siemensPPI = new SiemensPPI( );

			try
			{
				siemensPPI.SerialPortInni( sp =>
				{
					sp.PortName = comboBox3.Text;
					sp.BaudRate = baudRate;
					sp.DataBits = dataBits;
					sp.StopBits = stopBits == 0 ? System.IO.Ports.StopBits.None : (stopBits == 1 ? System.IO.Ports.StopBits.One : System.IO.Ports.StopBits.Two);
					sp.Parity = comboBox1.SelectedIndex == 0 ? System.IO.Ports.Parity.None : (comboBox1.SelectedIndex == 1 ? System.IO.Ports.Parity.Odd : System.IO.Ports.Parity.Even);
				} );
				siemensPPI.Open( );
				siemensPPI.Station = byte.Parse( textBox15.Text );
				siemensPPI.RtsEnable = true;

				button2.Enabled = true;
				button1.Enabled = false;
				panel2.Enabled = true;

				userControlReadWriteOp1.SetReadWriteNet( siemensPPI, "V100", false );
			}
			catch (Exception ex)
			{
				MessageBox.Show( ex.Message );
			}
		}

		private void button2_Click( object sender, EventArgs e )
		{
			// 断开连接
			siemensPPI.Close( );
			button2.Enabled = false;
			button1.Enabled = true;
			panel2.Enabled = false;
		}
		

		#endregion

		#region 批量读取测试

		private void button25_Click( object sender, EventArgs e )
		{
			DemoUtils.BulkReadRenderResult( siemensPPI, textBox6, textBox9, textBox10 );
		}

		

		#endregion

		#region 报文读取测试

		


		#endregion

		#region 测试功能代码


		private void Test1( )
		{
			OperateResult<bool> read = siemensPPI.ReadBool( "M100.4" );
			if (read.IsSuccess)
			{
				bool m100_4 = read.Content;
			}
			else
			{
				// failed
				string err = read.Message;
			}

			OperateResult write = siemensPPI.Write( "M100.4", true );
			if (write.IsSuccess)
			{
				// success
			}
			else
			{
				// failed
				string err = write.Message;
			}
		}

		private void Test2( )
		{
			byte m100_byte = siemensPPI.ReadByte( "M100" ).Content;
			short m100_short = siemensPPI.ReadInt16( "M100" ).Content;
			ushort m100_ushort = siemensPPI.ReadUInt16( "M100" ).Content;
			int m100_int = siemensPPI.ReadInt32( "M100" ).Content;
			uint m100_uint = siemensPPI.ReadUInt32( "M100" ).Content;
			float m100_float = siemensPPI.ReadFloat( "M100" ).Content;
			double m100_double = siemensPPI.ReadDouble( "M100" ).Content;
			string m100_string = siemensPPI.ReadString( "M100", 10 ).Content;

			HslCommunication.Core.IByteTransform ByteTransform = new HslCommunication.Core.ReverseBytesTransform( );

		}

		private void Test3()
		{
			// 读取操作，这里的M100可以替换成I100,Q100,DB20.100效果时一样的
			bool M100_7 = siemensPPI.ReadBool( "M100.7" ).Content;  // 读取M100.7是否通断，注意M100.0等同于M100
			byte byte_M100 = siemensPPI.ReadByte( "M100" ).Content; // 读取M100的值
			short short_M100 = siemensPPI.ReadInt16( "M100" ).Content; // 读取M100-M101组成的字
			ushort ushort_M100 = siemensPPI.ReadUInt16( "M100" ).Content; // 读取M100-M101组成的无符号的值
			int int_M100 = siemensPPI.ReadInt32( "M100" ).Content;         // 读取M100-M103组成的有符号的数据
			uint uint_M100 = siemensPPI.ReadUInt32( "M100" ).Content;      // 读取M100-M103组成的无符号的值
			float float_M100 = siemensPPI.ReadFloat( "M100" ).Content;   // 读取M100-M103组成的单精度值
			long long_M100 = siemensPPI.ReadInt64( "M100" ).Content;      // 读取M100-M107组成的大数据值
			ulong ulong_M100 = siemensPPI.ReadUInt64( "M100" ).Content;   // 读取M100-M107组成的无符号大数据
			double double_M100 = siemensPPI.ReadDouble( "M100" ).Content; // 读取M100-M107组成的双精度值
			string str_M100 = siemensPPI.ReadString( "M100", 10 ).Content;// 读取M100-M109组成的ASCII字符串数据

			// 写入操作，这里的M100可以替换成I100,Q100,DB20.100效果时一样的
			siemensPPI.Write( "M100.7", true );                // 写位，注意M100.0等同于M100
			siemensPPI.Write( "M100", (short)(byte)0x33 );            // 写单个字节
			siemensPPI.Write( "M100", (short)12345 );          // 写双字节有符号
			siemensPPI.Write( "M100", (ushort)45678 );         // 写双字节无符号
			siemensPPI.Write( "M100", 123456789 );             // 写双字有符号
			siemensPPI.Write( "M100", (uint)3456789123 );      // 写双字无符号
			siemensPPI.Write( "M100", 123.456f );              // 写单精度
			siemensPPI.Write( "M100", 1234556434534545L );     // 写大整数有符号
			siemensPPI.Write( "M100", 523434234234343UL );     // 写大整数无符号
			siemensPPI.Write( "M100", 123.456d );              // 写双精度
			siemensPPI.Write( "M100", "K123456789" );// 写ASCII字符串

			OperateResult<byte[]> read = siemensPPI.Read( "M100", 10 );
			{
				if(read.IsSuccess)
				{
					byte m100 = read.Content[0];
					byte m101 = read.Content[1];
					byte m102 = read.Content[2];
					byte m103 = read.Content[3];
					byte m104 = read.Content[4];
					byte m105 = read.Content[5];
					byte m106 = read.Content[6];
					byte m107 = read.Content[7];
					byte m108 = read.Content[8];
					byte m109 = read.Content[9];
				}
				else
				{
					// 发生了异常
				}
			}
		}

		#endregion

		private void button3_Click_1( object sender, EventArgs e )
		{
			OperateResult start = siemensPPI.Start( );
			if (start.IsSuccess) MessageBox.Show( "Start Success!" );
			else MessageBox.Show( start.Message );
		}

		private void button4_Click( object sender, EventArgs e )
		{
			OperateResult stop = siemensPPI.Stop( );
			if (stop.IsSuccess) MessageBox.Show( "Stop Success!" );
			else MessageBox.Show( stop.Message );
		}


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
	}
}
