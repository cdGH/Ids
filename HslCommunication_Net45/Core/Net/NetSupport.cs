using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using HslCommunication.BasicFramework;
using HslCommunication.Enthernet;
using HslCommunication.LogNet;
#if !NET35 && !NET20
using System.Threading.Tasks;
#endif

namespace HslCommunication.Core
{
	/*******************************************************************************
	 * 
	 *    网络通信类的基础类，提供所有相关的基础方法和功能
	 *
	 *    Network communication base class of the class, provides the basis of all relevant methods and functions
	 * 
	 *******************************************************************************/

	#region Network Helper

	/// <summary>
	/// 静态的方法支持类，提供一些网络的静态支持，支持从套接字从同步接收指定长度的字节数据，并支持报告进度。<br />
	/// The static method support class provides some static support for the network, supports receiving byte data of a specified length from the socket from synchronization, and supports reporting progress.
	/// </summary>
	/// <remarks>
	/// 在接收指定数量的字节数据的时候，如果一直接收不到，就会发生假死的状态。接收的数据时保存在内存里的，不适合大数据块的接收。
	/// </remarks>
	public static class NetSupport
	{
		/// <summary>
		/// Socket传输中的缓冲池大小<br />
		/// Buffer pool size in socket transmission
		/// </summary>
		internal const int SocketBufferSize = 16 * 1024;

		/// <summary>
		/// 从socket的网络中读取数据内容，需要指定数据长度和超时的时间，为了防止数据太大导致接收失败，所以此处接收到新的数据之后就更新时间。<br />
		/// To read the data content from the socket network, you need to specify the data length and timeout period. In order to prevent the data from being too large and cause the reception to fail, the time is updated after new data is received here.
		/// </summary>
		/// <param name="socket">网络套接字</param>
		/// <param name="receive">接收的长度</param>
		/// <param name="reportProgress">当前接收数据的进度报告，有些协议支持传输非常大的数据内容，可以给与进度提示的功能</param>
		/// <returns>最终接收的指定长度的byte[]数据</returns>
		internal static byte[] ReadBytesFromSocket( Socket socket, int receive, Action<long, long> reportProgress = null )
		{
			byte[] bytes_receive = new byte[receive];
			ReceiveBytesFromSocket( socket, bytes_receive, 0, receive, reportProgress );
			return bytes_receive;
		}

		/// <summary>
		/// 从socket的网络中读取数据内容，需要指定数据长度和超时的时间，为了防止数据太大导致接收失败，所以此处接收到新的数据之后就更新时间。<br />
		/// To read the data content from the socket network, you need to specify the data length and timeout period. In order to prevent the data from being too large and cause the reception to fail, the time is updated after new data is received here.
		/// </summary>
		/// <param name="socket">网络套接字</param>
		/// <param name="buffer">缓存的字节数组</param>
		/// <param name="offset">偏移信息</param>
		/// <param name="length">接收长度</param>
		/// <param name="reportProgress">当前接收数据的进度报告，有些协议支持传输非常大的数据内容，可以给与进度提示的功能</param>
		internal static void ReceiveBytesFromSocket( Socket socket, byte[] buffer, int offset, int length, Action<long, long> reportProgress = null )
		{
			int count_receive = 0;
			while (count_receive < length)
			{
				// 分割成8KB来接收数据
				int receive_length = Math.Min( length - count_receive, SocketBufferSize );
				int count = socket.Receive( buffer, count_receive + offset, receive_length, SocketFlags.None );
				count_receive += count;

				if (count == 0) throw new RemoteCloseException( );
				reportProgress?.Invoke( count_receive, length );
			}
		}

	}

	#endregion
	
}
