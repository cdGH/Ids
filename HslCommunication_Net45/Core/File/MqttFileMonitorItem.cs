using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace HslCommunication.Core
{
	/// <summary>
	/// 单个的监控文件对象，用来监控客户端的信息以及上传下载的速度
	/// </summary>
	public class MqttFileMonitorItem
	{
		/// <summary>
		/// 实例化一个默认的对象
		/// </summary>
		public MqttFileMonitorItem( )
		{
			StartTime      = DateTime.Now;
			LastUpdateTime = DateTime.Now;
			UniqueId       = System.Threading.Interlocked.Increment( ref uniqueIdCreate );
		}

		/// <summary>
		/// 当前对象的唯一ID信息
		/// </summary>
		public long UniqueId { get; private set; }

		/// <summary>
		/// 当前对象的远程的IP及端口
		/// </summary>
		public IPEndPoint EndPoint { get; set; }

		/// <summary>
		/// 当前对象的客户端ID
		/// </summary>
		public string ClientId { get; set; }

		/// <summary>
		/// 当前对象的客户端用户名
		/// </summary>
		public string UserName { get; set; }

		/// <summary>
		/// 当前的操作，Upload是上传，Download是下载
		/// </summary>
		public string Operate { get; set; }

		/// <summary>
		/// 当前的操作的目录信息
		/// </summary>
		public string Groups { get; set; }

		/// <summary>
		/// 当前操作的文件名
		/// </summary>
		public string FileName { get; set; }

		/// <summary>
		/// 上传或下载的速度
		/// </summary>
		public long SpeedSecond { get; set; }

		/// <summary>
		/// 当前操作的起始时间
		/// </summary>
		public DateTime StartTime { get; set; }

		/// <summary>
		/// 最后一次更新数据的时间
		/// </summary>
		public DateTime LastUpdateTime { get; set; }

		/// <summary>
		/// 文件的总大小
		/// </summary>
		public long TotalSize { get; set; }

		/// <summary>
		/// 上一次的更新数据时的进度信息
		/// </summary>
		public long LastUpdateProgress { get; set; }

		/// <summary>
		/// 更新当前的文件的状态
		/// </summary>
		/// <param name="progress">当前的进度信息</param>
		/// <param name="total">当前的总大小</param>
		public void UpdateProgress( long progress, long total )
		{
			TotalSize = total;
			TimeSpan ts = DateTime.Now - LastUpdateTime;
			if (ts.TotalSeconds >= 0.2d)
			{
				long size = progress - LastUpdateProgress;
				if (size <= 0) { SpeedSecond = 0; return; }

				SpeedSecond = (long)(size / ts.TotalSeconds);
				LastUpdateTime = DateTime.Now;
				LastUpdateProgress = progress;
			}
		}


		private static long uniqueIdCreate = 0;
	}
}
