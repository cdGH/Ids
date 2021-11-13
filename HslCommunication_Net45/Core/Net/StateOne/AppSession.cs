using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace HslCommunication.Core.Net
{
	/// <summary>
	/// 当前的网络会话信息，还包含了一些客户端相关的基本的参数信息<br />
	/// The current network session information also contains some basic parameter information related to the client
	/// </summary>
	public class AppSession
	{
		#region Constructor

		/// <summary>
		/// 实例化一个构造方法
		/// </summary>
		public AppSession( )
		{
			ClientUniqueID = Guid.NewGuid( ).ToString( "N" );
			OnlineTime     = DateTime.Now;
		}

		#endregion

		/// <summary>
		/// 传输数据的对象
		/// </summary>
		internal Socket WorkSocket { get; set; }

		/// <summary>
		/// 获取当前的客户端的上线时间<br />
		/// Get the online time of the current client
		/// </summary>
		public DateTime OnlineTime { get; private set; }

		/// <summary>
		/// IP地址
		/// </summary>
		public string IpAddress { get; internal set; }

		/// <summary>
		/// 此连接对象连接的远程客户端
		/// </summary>
		public IPEndPoint IpEndPoint { get; internal set; }

		/// <summary>
		/// 远程对象的别名
		/// </summary>
		public string LoginAlias { get; set; }

		/// <summary>
		/// 心跳验证的时间点
		/// </summary>
		public DateTime HeartTime { get; set; } = DateTime.Now;

		/// <summary>
		/// 客户端唯一的标识
		/// </summary>
		public string ClientUniqueID { get; private set; }

		/// <summary>
		/// UDP通信中的远程端
		/// </summary>
		internal EndPoint UdpEndPoint = null;

		/// <summary>
		/// 指令头缓存
		/// </summary>
		internal byte[] BytesHead { get; set; } = new byte[HslProtocol.HeadByteLength];

		/// <summary>
		/// 已经接收的指令头长度
		/// </summary>
		internal int AlreadyReceivedHead { get; set; }

		/// <summary>
		/// 数据内容缓存
		/// </summary>
		internal byte[] BytesContent { get; set; }

		/// <summary>
		/// 已经接收的数据内容长度
		/// </summary>
		internal int AlreadyReceivedContent { get; set; }

		/// <summary>
		/// 用于关键字分类使用
		/// </summary>
		internal string KeyGroup { get; set; }

		/// <summary>
		/// 清除本次的接收内容
		/// </summary>
		internal void Clear( )
		{
			BytesHead = new byte[HslProtocol.HeadByteLength];
			AlreadyReceivedHead = 0;
			BytesContent = null;
			AlreadyReceivedContent = 0;
		}

		#region Object Override

		/// <inheritdoc/>
		public override bool Equals( object obj )
		{
			return ReferenceEquals( this, obj );
		}

		/// <inheritdoc/>
		public override string ToString( )
		{
			if (string.IsNullOrEmpty( LoginAlias ))
			{
				return $"AppSession[{IpEndPoint}]";
			}
			else
			{
				return $"AppSession[{IpEndPoint}] [{LoginAlias}]";
			}
		}

		/// <inheritdoc/>
		public override int GetHashCode( )
		{
			return base.GetHashCode( );
		}

		#endregion

	}
}
