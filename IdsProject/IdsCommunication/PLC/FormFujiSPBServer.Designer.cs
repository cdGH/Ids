﻿namespace HslCommunicationDemo
{
    partial class FormFujiSPBServer
	{
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose( bool disposing )
        {
            if (disposing && (components != null))
            {
                components.Dispose( );
            }
            base.Dispose( disposing );
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent( )
        {
			this.panel1 = new System.Windows.Forms.Panel();
			this.label2 = new System.Windows.Forms.Label();
			this.textBox1 = new System.Windows.Forms.TextBox();
			this.label1 = new System.Windows.Forms.Label();
			this.button11 = new System.Windows.Forms.Button();
			this.checkBox3 = new System.Windows.Forms.CheckBox();
			this.button5 = new System.Windows.Forms.Button();
			this.textBox10 = new System.Windows.Forms.TextBox();
			this.label14 = new System.Windows.Forms.Label();
			this.button1 = new System.Windows.Forms.Button();
			this.textBox2 = new System.Windows.Forms.TextBox();
			this.label3 = new System.Windows.Forms.Label();
			this.panel2 = new System.Windows.Forms.Panel();
			this.userControlReadWriteServer1 = new HslCommunicationDemo.DemoControl.UserControlReadWriteServer();
			this.userControlHead1 = new HslCommunicationDemo.DemoControl.UserControlHead();
			this.panel1.SuspendLayout();
			this.panel2.SuspendLayout();
			this.SuspendLayout();
			// 
			// panel1
			// 
			this.panel1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.panel1.Controls.Add(this.label2);
			this.panel1.Controls.Add(this.textBox1);
			this.panel1.Controls.Add(this.label1);
			this.panel1.Controls.Add(this.button11);
			this.panel1.Controls.Add(this.checkBox3);
			this.panel1.Controls.Add(this.button5);
			this.panel1.Controls.Add(this.textBox10);
			this.panel1.Controls.Add(this.label14);
			this.panel1.Controls.Add(this.button1);
			this.panel1.Controls.Add(this.textBox2);
			this.panel1.Controls.Add(this.label3);
			this.panel1.Location = new System.Drawing.Point(3, 35);
			this.panel1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size(997, 54);
			this.panel1.TabIndex = 0;
			this.panel1.Paint += new System.Windows.Forms.PaintEventHandler(this.panel1_Paint);
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.ForeColor = System.Drawing.Color.Gray;
			this.label2.Location = new System.Drawing.Point(556, 30);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(91, 17);
			this.label2.TabIndex = 31;
			this.label2.Text = "9600,8,1,None";
			// 
			// textBox1
			// 
			this.textBox1.Location = new System.Drawing.Point(199, 14);
			this.textBox1.Name = "textBox1";
			this.textBox1.Size = new System.Drawing.Size(65, 23);
			this.textBox1.TabIndex = 30;
			this.textBox1.Text = "0";
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(145, 17);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(44, 17);
			this.label1.TabIndex = 29;
			this.label1.Text = "站号：";
			// 
			// button11
			// 
			this.button11.Enabled = false;
			this.button11.Location = new System.Drawing.Point(372, 11);
			this.button11.Name = "button11";
			this.button11.Size = new System.Drawing.Size(83, 28);
			this.button11.TabIndex = 28;
			this.button11.Text = "关闭服务";
			this.button11.UseVisualStyleBackColor = true;
			this.button11.Click += new System.EventHandler(this.button11_Click);
			// 
			// checkBox3
			// 
			this.checkBox3.AutoSize = true;
			this.checkBox3.Location = new System.Drawing.Point(763, 6);
			this.checkBox3.Name = "checkBox3";
			this.checkBox3.Size = new System.Drawing.Size(87, 21);
			this.checkBox3.TabIndex = 27;
			this.checkBox3.Text = "字符串颠倒";
			this.checkBox3.UseVisualStyleBackColor = true;
			// 
			// button5
			// 
			this.button5.Location = new System.Drawing.Point(643, 1);
			this.button5.Name = "button5";
			this.button5.Size = new System.Drawing.Size(91, 28);
			this.button5.TabIndex = 9;
			this.button5.Text = "启动串口";
			this.button5.UseVisualStyleBackColor = true;
			this.button5.Click += new System.EventHandler(this.button5_Click);
			// 
			// textBox10
			// 
			this.textBox10.Location = new System.Drawing.Point(559, 4);
			this.textBox10.Name = "textBox10";
			this.textBox10.Size = new System.Drawing.Size(61, 23);
			this.textBox10.TabIndex = 8;
			this.textBox10.Text = "COM1";
			// 
			// label14
			// 
			this.label14.AutoSize = true;
			this.label14.Location = new System.Drawing.Point(492, 7);
			this.label14.Name = "label14";
			this.label14.Size = new System.Drawing.Size(44, 17);
			this.label14.TabIndex = 7;
			this.label14.Text = "串口：";
			// 
			// button1
			// 
			this.button1.Location = new System.Drawing.Point(282, 11);
			this.button1.Name = "button1";
			this.button1.Size = new System.Drawing.Size(83, 28);
			this.button1.TabIndex = 4;
			this.button1.Text = "启动服务";
			this.button1.UseVisualStyleBackColor = true;
			this.button1.Click += new System.EventHandler(this.button1_Click);
			// 
			// textBox2
			// 
			this.textBox2.Location = new System.Drawing.Point(74, 14);
			this.textBox2.Name = "textBox2";
			this.textBox2.Size = new System.Drawing.Size(65, 23);
			this.textBox2.TabIndex = 3;
			this.textBox2.Text = "9600";
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(20, 17);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(56, 17);
			this.label3.TabIndex = 2;
			this.label3.Text = "端口号：";
			// 
			// panel2
			// 
			this.panel2.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.panel2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.panel2.Controls.Add(this.userControlReadWriteServer1);
			this.panel2.Location = new System.Drawing.Point(3, 92);
			this.panel2.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
			this.panel2.Name = "panel2";
			this.panel2.Size = new System.Drawing.Size(997, 549);
			this.panel2.TabIndex = 1;
			// 
			// userControlReadWriteServer1
			// 
			this.userControlReadWriteServer1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.userControlReadWriteServer1.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
			this.userControlReadWriteServer1.Location = new System.Drawing.Point(3, 0);
			this.userControlReadWriteServer1.Name = "userControlReadWriteServer1";
			this.userControlReadWriteServer1.Size = new System.Drawing.Size(989, 544);
			this.userControlReadWriteServer1.TabIndex = 0;
			// 
			// userControlHead1
			// 
			this.userControlHead1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
			this.userControlHead1.Dock = System.Windows.Forms.DockStyle.Top;
			this.userControlHead1.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
			this.userControlHead1.HelpLink = "http://www.cnblogs.com/dathlin/p/7782315.html";
			this.userControlHead1.Location = new System.Drawing.Point(0, 0);
			this.userControlHead1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
			this.userControlHead1.MinimumSize = new System.Drawing.Size(800, 32);
			this.userControlHead1.Name = "userControlHead1";
			this.userControlHead1.ProtocolInfo = "FujiSPB Tcp + Serial";
			this.userControlHead1.Size = new System.Drawing.Size(1004, 32);
			this.userControlHead1.TabIndex = 2;
			this.userControlHead1.SaveConnectEvent += new System.EventHandler<System.EventArgs>(this.userControlHead1_SaveConnectEvent_1);
			// 
			// FormFujiSPBServer
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.AutoScroll = true;
			this.ClientSize = new System.Drawing.Size(1004, 645);
			this.Controls.Add(this.userControlHead1);
			this.Controls.Add(this.panel2);
			this.Controls.Add(this.panel1);
			this.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
			this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
			this.Name = "FormFujiSPBServer";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "富士SPB虚拟PLC【支持网口和串口】";
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FormSiemens_FormClosing);
			this.Load += new System.EventHandler(this.FormSiemens_Load);
			this.panel1.ResumeLayout(false);
			this.panel1.PerformLayout();
			this.panel2.ResumeLayout(false);
			this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.TextBox textBox2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button button5;
        private System.Windows.Forms.TextBox textBox10;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.CheckBox checkBox3;
        private System.Windows.Forms.Button button11;
        private DemoControl.UserControlHead userControlHead1;
		private DemoControl.UserControlReadWriteServer userControlReadWriteServer1;
		private System.Windows.Forms.TextBox textBox1;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
	}
}