using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HslCommunication.Core
{
	/// <summary>
	/// 监控上传和下载文件的信息
	/// </summary>
	public class MqttFileMonitor
	{
		/// <summary>
		/// 实例化一个默认的对象
		/// </summary>
		public MqttFileMonitor( )
		{
			dicLock = new object( );
			fileMonitors = new Dictionary<long, MqttFileMonitorItem>( );
		}

		private Dictionary<long, MqttFileMonitorItem> fileMonitors;
		private object dicLock;

		/// <summary>
		/// 增加一个文件监控的对象信息
		/// </summary>
		/// <param name="monitorItem">文件监控对象</param>
		public void Add( MqttFileMonitorItem monitorItem )
		{
			lock (dicLock)
			{
				if (fileMonitors.ContainsKey( monitorItem.UniqueId ))
					fileMonitors[monitorItem.UniqueId] = monitorItem;
				else
					fileMonitors.Add( monitorItem.UniqueId, monitorItem );
			}
		}

		/// <summary>
		/// 根据唯一的ID信息，移除相关的文件监控对象
		/// </summary>
		/// <param name="uniqueId"></param>
		public void Remove( long uniqueId )
		{
			lock (dicLock)
			{
				if (fileMonitors.ContainsKey( uniqueId ))
					fileMonitors.Remove( uniqueId );
			}
		}

		/// <summary>
		/// 获取当前所有的监控文件数据的快照
		/// </summary>
		/// <returns>文件监控列表</returns>
		public MqttFileMonitorItem[] GetMonitorItemsSnapShoot( )
		{
			lock (dicLock)
			{
				return fileMonitors.Values.ToArray( );
			}
		}
	}
}
