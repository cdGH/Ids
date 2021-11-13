using HslCommunication.BasicFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace HslCommunication.Core.Net
{
	/// <summary>
	/// 异形客户端的连接对象
	/// </summary>
	public class AlienSession
	{
		/// <summary>
		/// 实例化一个默认的参数
		/// </summary>
		public AlienSession( )
		{
			IsStatusOk  = true;
			OnlineTime  = DateTime.Now;
			OfflineTime = DateTime.MinValue;
		}

		/// <summary>
		/// 网络套接字
		/// </summary>
		public Socket Socket { get; set; }

		/// <summary>
		/// 唯一的标识
		/// </summary>
		public string DTU { get; set; }

		/// <summary>
		/// 密码信息
		/// </summary>
		public string Pwd { get; set; }

		/// <summary>
		/// 指示当前的网络状态
		/// </summary>
		public bool IsStatusOk { get; set; }

		/// <summary>
		/// 上线时间
		/// </summary>
		public DateTime OnlineTime { get; set; }

		/// <summary>
		/// 最后一次下线的时间
		/// </summary>
		public DateTime OfflineTime { get; set; }

		/// <summary>
		/// 进行下线操作
		/// </summary>
		public void Offline( )
		{
			if (IsStatusOk == true)
			{
				IsStatusOk = false;
				OfflineTime = DateTime.Now;
			}
		}

		#region Object Override

		/// <inheritdoc/>
		public override string ToString( )
		{
			StringBuilder sb = new StringBuilder( );
			sb.Append( $"DtuSession[{DTU}] [{(IsStatusOk ? "Online" : "Offline")}]" );
			if (IsStatusOk)
				sb.Append( $" [{SoftBasic.GetTimeSpanDescription( DateTime.Now - OnlineTime )}]" );
			else if (OfflineTime == DateTime.MinValue)
				sb.Append( $" [----]" );
			else
				sb.Append( $" [{SoftBasic.GetTimeSpanDescription( DateTime.Now - OfflineTime )}]" );
			return sb.ToString( );
		}

		#endregion
	}
}
