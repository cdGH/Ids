﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using HslCommunication.Profinet;
using HslCommunication;
using System.Threading;
using System.IO.Ports;
using HslCommunication.Profinet.Inovance;
using System.Xml.Linq;
using HslCommunication.BasicFramework;

namespace HslCommunicationDemo
{
    public partial class FormInovanceSerial : HslFormContent
    {
        public FormInovanceSerial( )
        {
            InitializeComponent( );
        }

        private InovanceSerial inovance = null;

        private void FormSiemens_Load( object sender, EventArgs e )
        {
            panel2.Enabled = false;
            comboBox1.SelectedIndex = 0;


            comboBox4.DataSource = SoftBasic.GetEnumValues<InovanceSeries>( );
            comboBox2.SelectedIndex = 0;
            comboBox2.SelectedIndexChanged += ComboBox2_SelectedIndexChanged;
            checkBox3.CheckedChanged += CheckBox3_CheckedChanged;

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
                Text = "InovanceSerial Read Demo";

                label1.Text = "Com:";
                label2.Text = "Series:";
                label3.Text = "baudRate:";
                label22.Text = "DataBit";
                label23.Text = "StopBit";
                label24.Text = "parity";
                label21.Text = "station";
                checkBox1.Text = "address from 0";
                checkBox3.Text = "string reverse";
                button1.Text = "Connect";
                button2.Text = "Disconnect";

                label11.Text = "Address:";
                label12.Text = "length:";
                button25.Text = "Bulk Read";
                label13.Text = "Results:";
                label16.Text = "Message:";
                label14.Text = "Results:";
                button26.Text = "Read";

                groupBox3.Text = "Bulk Read test";
                groupBox4.Text = "Message reading test, hex string needs to be filled in,without crc";
                groupBox5.Text = "Special function test";

                comboBox1.DataSource = new string[] { "None", "Odd", "Even" };
            }
        }

        private void CheckBox3_CheckedChanged( object sender, EventArgs e )
        {
            if (inovance != null)
            {
                inovance.IsStringReverse = checkBox3.Checked;
            }
        }

        private void ComboBox2_SelectedIndexChanged( object sender, EventArgs e )
        {
            if (inovance != null)
            {
                switch (comboBox2.SelectedIndex)
                {
                    case 0: inovance.DataFormat = HslCommunication.Core.DataFormat.ABCD; break;
                    case 1: inovance.DataFormat = HslCommunication.Core.DataFormat.BADC; break;
                    case 2: inovance.DataFormat = HslCommunication.Core.DataFormat.CDAB; break;
                    case 3: inovance.DataFormat = HslCommunication.Core.DataFormat.DCBA; break;
                    default: break;
                }
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
                MessageBox.Show( "Station input wrong！" );
                return;
            }

            inovance?.Close( );
            inovance = new InovanceSerial( station );
            inovance.AddressStartWithZero = checkBox1.Checked;
            inovance.Series = (InovanceSeries)comboBox4.SelectedItem;

            ComboBox2_SelectedIndexChanged( null, new EventArgs( ) );
            inovance.IsStringReverse = checkBox3.Checked;


            try
            {
                inovance.SerialPortInni( sp =>
                 {
                     sp.PortName = comboBox3.Text;
                     sp.BaudRate = baudRate;
                     sp.DataBits = dataBits;
                     sp.StopBits = stopBits == 0 ? StopBits.None : (stopBits == 1 ? StopBits.One : StopBits.Two);
                     sp.Parity = comboBox1.SelectedIndex == 0 ? Parity.None : (comboBox1.SelectedIndex == 1 ? Parity.Odd : Parity.Even);
                 } );
                inovance.RtsEnable = checkBox5.Checked;
                inovance.Open( );

                button2.Enabled = true;
                button1.Enabled = false;
                panel2.Enabled = true;

                userControlReadWriteOp1.SetReadWriteNet( inovance, "M100", false );
            }
            catch (Exception ex)
            {
                MessageBox.Show( ex.Message );
            }
        }

        private void button2_Click( object sender, EventArgs e )
        {
            // 断开连接
            inovance.Close( );
            button2.Enabled = false;
            button1.Enabled = true;
            panel2.Enabled = false;
        }
        
        #endregion

        #region 批量读取测试

        private void button25_Click( object sender, EventArgs e )
        {
            DemoUtils.BulkReadRenderResult( inovance, textBox6, textBox9, textBox10 );
        }

        #endregion

        #region 报文读取测试


        private void button26_Click( object sender, EventArgs e )
        {
            OperateResult<byte[]> read = inovance.ReadFromCoreServer( HslCommunication.Serial.SoftCRC16.CRC16( HslCommunication.BasicFramework.SoftBasic.HexStringToBytes( textBox13.Text ) ) );
            if (read.IsSuccess)
            {
                textBox11.Text = "Result：" + HslCommunication.BasicFramework.SoftBasic.ByteToHexString( read.Content );
            }
            else
            {
                MessageBox.Show( "Read Failed：" + read.ToMessageShowString( ) );
            }
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
            element.SetAttributeValue( DemoDeviceList.XmlAddressStartWithZero, checkBox1.Checked );
            element.SetAttributeValue( DemoDeviceList.XmlDataFormat, comboBox2.SelectedIndex );
            element.SetAttributeValue( DemoDeviceList.XmlStringReverse, checkBox3.Checked );
            element.SetAttributeValue( DemoDeviceList.XmlRtsEnable, checkBox5.Checked );
            element.SetAttributeValue( nameof( InovanceSeries ), (InovanceSeries)comboBox4.SelectedItem );
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
            checkBox1.Checked = bool.Parse( element.Attribute( DemoDeviceList.XmlAddressStartWithZero ).Value );
            comboBox2.SelectedIndex = int.Parse( element.Attribute( DemoDeviceList.XmlDataFormat ).Value );
            checkBox3.Checked = bool.Parse( element.Attribute( DemoDeviceList.XmlStringReverse ).Value );
            checkBox5.Checked = bool.Parse( element.Attribute( DemoDeviceList.XmlRtsEnable ).Value );
            if (element.Attribute( nameof( InovanceSeries ) ) != null)
                comboBox4.SelectedItem = SoftBasic.GetEnumFromString<InovanceSeries>( element.Attribute( nameof( InovanceSeries ) ).Value );
        }

        private void userControlHead1_SaveConnectEvent_1( object sender, EventArgs e )
        {
            userControlHead1_SaveConnectEvent( sender, e );
        }
    }
}
