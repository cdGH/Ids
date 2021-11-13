using HslCommunication.Core.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using HslCommunication.LogNet;

namespace HslCommunication.Enthernet
{
	/// <summary>
	/// 一个基于明文的socket中心
	/// </summary>
	public class NetPlainSocket : NetworkXBase
	{
		#region Constructor

		/// <summary>
		/// 实例化一个默认的对象
		/// </summary>
		public NetPlainSocket(  )
		{
			buffer = new byte[bufferLength];
			encoding = Encoding.UTF8;
		}

		/// <summary>
		/// 使用指定的ip地址和端口号来实例化这个对象
		/// </summary>
		/// <param name="ipAddress">Ip地址</param>
		/// <param name="port">端口号</param>
		public NetPlainSocket( string ipAddress, int port )
		{
			buffer = new byte[bufferLength];
			encoding = Encoding.UTF8;
			this.ipAddress = ipAddress;
			this.port = port;
		}

		#endregion

		#region Connect Disconnect

		/// <summary>
		/// 连接服务器
		/// </summary>
		/// <returns>返回是否连接成功</returns>
		public OperateResult ConnectServer( )
		{
			CoreSocket?.Close( );

			OperateResult<Socket> connect = CreateSocketAndConnect( ipAddress, port, 5_000 );
			if (!connect.IsSuccess) return connect;

			try
			{
				CoreSocket = connect.Content;
				CoreSocket.BeginReceive( buffer, 0, bufferLength, SocketFlags.None, new AsyncCallback( ReceiveCallBack ), CoreSocket );
				return OperateResult.CreateSuccessResult( );
			}
			catch (Exception ex)
			{
				return new OperateResult( ex.Message );
			}
		}

		/// <summary>
		/// 关闭当前的连接对象
		/// </summary>
		/// <returns>错误信息</returns>
		public OperateResult ConnectClose( )
		{
			try
			{
				CoreSocket?.Close( );
				return OperateResult.CreateSuccessResult( );
			}
			catch(Exception ex)
			{
				return new OperateResult( ex.Message );
			}
		}

		/// <summary>
		/// 发送字符串到网络上去
		/// </summary>
		/// <param name="text">文本信息</param>
		/// <returns>发送是否成功</returns>
		public OperateResult SendString(string text )
		{
			if (string.IsNullOrEmpty( text )) return OperateResult.CreateSuccessResult( );

			return Send( CoreSocket, encoding.GetBytes( text ) );
		}

		#endregion

		#region Receive Background

		private void ReceiveCallBack( IAsyncResult ar )
		{
			if (ar.AsyncState is Socket socket)
			{
				byte[] data = null;
				try
				{
					int length = socket.EndReceive( ar );
					socket.BeginReceive( buffer, 0, bufferLength, SocketFlags.None, new AsyncCallback( ReceiveCallBack ), socket );

					if (length == 0)
					{
						// 对方关闭的网络
						CoreSocket?.Close( );
						return;
					}

					data = new byte[length];
					Array.Copy( buffer, 0, data, 0, length );
				}
				catch (ObjectDisposedException)
				{

				}
				catch (Exception ex)
				{
					// 断开服务器，准备重新连接
					LogNet?.WriteWarn( StringResources.Language.SocketContentReceiveException + ":" + ex.Message );
					ThreadPool.QueueUserWorkItem( new WaitCallback( ReConnectServer ), null );
				}

				if (data != null) ReceivedString?.Invoke( encoding.GetString( data ) );
			}
		}

		/// <summary>
		/// 是否是处于重连的状态
		/// </summary>
		/// <param name="obj">无用的对象</param>
		private void ReConnectServer( object obj )
		{
			LogNet?.WriteWarn( StringResources.Language.ReConnectServerAfterTenSeconds );
			for (int i = 0; i < 10; i++)
			{
				Thread.Sleep( 1000 );
				LogNet?.WriteWarn( $"Wait for connecting server after {9 - i} seconds" );
			}

			OperateResult<Socket> connect = CreateSocketAndConnect( ipAddress, port, 5_000 );
			if (!connect.IsSuccess)
			{
				ThreadPool.QueueUserWorkItem( new WaitCallback( ReConnectServer ), obj );
				return;
			}

			lock (connectLock)
			{
				try
				{
					CoreSocket?.Close( );
					CoreSocket = connect.Content;
					CoreSocket.BeginReceive( buffer, 0, bufferLength, SocketFlags.None, new AsyncCallback( ReceiveCallBack ), CoreSocket );
					LogNet?.WriteWarn( StringResources.Language.ReConnectServerSuccess );
				}
				catch (Exception ex)
				{
					LogNet?.WriteWarn( StringResources.Language.RemoteClosedConnection + ":" + ex.Message );
					ThreadPool.QueueUserWorkItem( new WaitCallback( ReConnectServer ), obj );
				}
			}
		}

		#endregion

		#region Public Properties

		/// <summary>
		/// 当接收到字符串时候的触发事件
		/// </summary>
		public event Action<string> ReceivedString;

		/// <summary>
		/// 当前的编码器
		/// </summary>
		public Encoding Encoding
		{
			get => encoding;
			set => encoding = value;
		}

		#endregion

		#region Private Member

		private Encoding encoding;
		private object connectLock = new object( );
		private string ipAddress = "127.0.0.1";
		private int port = 10000;
		private int bufferLength = 2048;
		private byte[] buffer = null;

		#endregion

		#region Object Override

		/// <summary>
		/// 返回表示当前对象的字符串
		/// </summary>
		/// <returns>字符串</returns>
		public override string ToString( ) => $"NetPlainSocket[{ipAddress}:{port}]";

		#endregion
	}
}
