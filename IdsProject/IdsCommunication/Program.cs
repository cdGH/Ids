﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace HslCommunicationDemo
{
	static class Program
	{
		/// <summary>
		/// 1代表中文，2代表英文
		/// </summary>
		public static int Language = 1;

		/// <summary>
		/// 是否显示相关的信息
		/// </summary>
		public static bool ShowAuthorInfomation = true;

		public static bool IsActive { get; private set; }

		public static DateTime StartTime = DateTime.Now;

		public static string SystemName = "工业设备联网调试系统";

		/// <summary>
		/// 应用程序的主入口点。
		/// </summary>
		[STAThread]
		static void Main( )
		{
			AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
			// 授权示例
			if(!HslCommunication.Authorization.SetAuthorizationCode( "Your Code" ))
			{
				// active failed
				// MessageBox.Show( "授权失败！当前程序只能使用8小时！" );
				// return;
			}
			else
			{
				IsActive = true;
			}


			Application.EnableVisualStyles( );
			Application.SetCompatibleTextRenderingDefault( false );


			System.Threading.ThreadPool.SetMaxThreads( 2000, 800 );
			Application.Run( new FormFatekPrograme( ) ); // FormSelect
		}

		private static void CurrentDomain_UnhandledException( object sender, UnhandledExceptionEventArgs e )
		{
			Exception ex = e.ExceptionObject as Exception;
			System.IO.File.WriteAllText( "123.txt", HslCommunication.BasicFramework.SoftBasic.GetExceptionMessage( ex ) );
		}
	}
}
