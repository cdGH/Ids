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
using HslCommunication.Profinet.Melsec;
using HslCommunication;
using HslCommunication.Profinet.Keyence;
using System.Xml.Linq;

namespace HslCommunicationDemo
{
    public partial class FormKeyenceNanoSerialOverTcp : HslFormContent
    {
        public FormKeyenceNanoSerialOverTcp( )
        {
            InitializeComponent( );
            keyence = new KeyenceNanoSerialOverTcp( );
        }


        private KeyenceNanoSerialOverTcp keyence = null;


        private void FormSiemens_Load( object sender, EventArgs e )
        {
            panel2.Enabled = false;

            Language( Program.Language );
        }


        private void Language( int language )
        {
            if (language == 2)
            {
                Text = "Keyence Read PLC Demo";

                label27.Text = "Ip:";
                label26.Text = "Port:";
                button1.Text = "Connect";
                button2.Text = "Disconnect";
                label21.Text = "Address:";
                label11.Text = "Address:";
                label12.Text = "length:";
                button25.Text = "Bulk Read";
                label13.Text = "Results:";
                label16.Text = "Message:";
                label14.Text = "Results:";
                button26.Text = "Read";
                label1.Text = "Station:";
                button4.Text = "Annotation";

                groupBox3.Text = "Bulk Read test";
                groupBox4.Text = "Message reading test, hex string needs to be filled in";
            }
        }

        private void FormSiemens_FormClosing( object sender, FormClosingEventArgs e )
        {

        }
        
        #region Connect And Close

        private void button1_Click( object sender, EventArgs e )
        {
            if (!int.TryParse( textBox2.Text, out int port ))
            {
                MessageBox.Show( DemoUtils.PortInputWrong );
                return;
            }

            if (!byte.TryParse( textBox3.Text, out byte station ))
            {
                MessageBox.Show( "Station int wrong, it needs 0 - 255" );
                return;
            }


            keyence?.ConnectClose( );
            keyence = new KeyenceNanoSerialOverTcp( );
            keyence.IpAddress = textBox1.Text;
            keyence.Port = port;
            keyence.Station = station;
            keyence.UseStation = checkBox1.Checked;

            try
            {
                OperateResult connect = keyence.ConnectServer( );
                if (connect.IsSuccess)
                {
                    MessageBox.Show( StringResources.Language.ConnectedSuccess );
                    button2.Enabled = true;
                    button1.Enabled = false;
                    panel2.Enabled = true;

                    userControlReadWriteOp1.SetReadWriteNet( keyence, "DM0", false );
                    userControlCurve1.ReadWriteNet = keyence;
                }
                else
                {
                    MessageBox.Show( HslCommunication.StringResources.Language.ConnectedFailed );
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show( ex.Message );
            }
        }

        private void button2_Click( object sender, EventArgs e )
        {
            // 断开连接
            keyence.ConnectClose( );
            button2.Enabled = false;
            button1.Enabled = true;
            panel2.Enabled = false;
        }



        #endregion

        #region 批量读取测试

        private void button25_Click( object sender, EventArgs e )
        {
            try
            {
                OperateResult<byte[]> read = keyence.Read( textBox6.Text, ushort.Parse( textBox9.Text ) );
                if (read.IsSuccess)
                {
                    textBox10.Text = "Result：" + read.Content.ToHexString( ' ' );
                }
                else
                {
                    MessageBox.Show( "Read Failed：" + read.ToMessageShowString( ) );
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show( "Read Failed：" + ex.Message );
            }
        }

        #endregion

        #region 报文读取测试

        private void button26_Click( object sender, EventArgs e )
        {
            OperateResult<byte[]> read = keyence.ReadFromCoreServer( HslCommunication.BasicFramework.SoftBasic.HexStringToBytes( textBox13.Text ) );
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

        private void button3_Click( object sender, EventArgs e )
        {
            OperateResult<KeyencePLCS> read = keyence.ReadPlcType( );
            if (read.IsSuccess)
            {
                textBox10.Text = read.Content.ToString( );
            }
            else
            {
                MessageBox.Show( "Read Failed:" + read.ToMessageShowString( ) );
            }
        }
        private void button4_Click( object sender, EventArgs e )
        {
            OperateResult<string> read = keyence.ReadAddressAnnotation( textBox6.Text );
            if (read.IsSuccess)
            {
                textBox10.Text = read.Content;
            }
            else
            {
                MessageBox.Show( "Read Failed:" + read.ToMessageShowString( ) );
            }
        }

        public override void SaveXmlParameter( XElement element )
        {
            element.SetAttributeValue( DemoDeviceList.XmlIpAddress, textBox1.Text );
            element.SetAttributeValue( DemoDeviceList.XmlPort, textBox2.Text );
        }

        public override void LoadXmlParameter( XElement element )
        {
            base.LoadXmlParameter( element );
            textBox1.Text = element.Attribute( DemoDeviceList.XmlIpAddress ).Value;
            textBox2.Text = element.Attribute( DemoDeviceList.XmlPort ).Value;
        }

        private void userControlHead1_SaveConnectEvent_1( object sender, EventArgs e )
        {
            userControlHead1_SaveConnectEvent( sender, e );
        }

	}
}
