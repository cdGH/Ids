using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.IO;
using System.Threading;
using HslCommunication.Core;
using HslCommunication.Core.Net;
using HslCommunication.LogNet;
#if !NET35 && !NET20
using System.Threading.Tasks;
#endif

namespace HslCommunication.Enthernet
{
	/// <summary>
	/// 用于服务器支持软件全自动更新升级的类<br />
	/// Class for server support software full automatic update and upgrade
	/// </summary>
	/// <remarks>
	/// 目前的更新机制是全部文件的更新，没有进行差异化的比较
	/// </remarks>
	public sealed class NetSoftUpdateServer : NetworkServerBase
	{
		#region Constructor

		/// <summary>
		/// 实例化一个默认的对象<br />
		/// Instantiate a default object
		/// </summary>
		/// <param name="updateExeFileName">更新程序的名称</param>
		public NetSoftUpdateServer( string updateExeFileName = "软件自动更新.exe" )
		{
			this.updateExeFileName = updateExeFileName;
		}

		#endregion

		/// <summary>
		/// 系统升级时客户端所在的目录，默认为C:\HslCommunication
		/// </summary>
		public string FileUpdatePath
		{
			get { return m_FilePath; }
			set { m_FilePath = value; }
		}

		/// <summary>
		/// 获取当前在线的客户端数量信息，一般是正在下载中的会话客户端数量。<br />
		/// Get information about the number of currently online clients, generally the number of session clients that are being downloaded.
		/// </summary>
		public int OnlineSessions => sessions.Count;

		private void RemoveAndCloseSession( AppSession session )
		{
			lock (lockSessions)
			{
				if (this.sessions.Remove( session ))
				{
					session.WorkSocket?.Close( );
				}
			}
		}

		/// <inheritdoc/>
#if NET35 || NET20
		protected override void ThreadPoolLogin( Socket socket, IPEndPoint endPoint )
#else
		protected async override void ThreadPoolLogin( Socket socket, IPEndPoint endPoint )
#endif
		{
			string fileUpdatePath = FileUpdatePath;
#if NET35 || NET20
			OperateResult<byte[]> receive = Receive( socket, 4, 10_000 );
#else
			OperateResult<byte[]> receive = await ReceiveAsync( socket, 4, 10_000 );
#endif
			if (!receive.IsSuccess) { LogNet?.WriteError( ToString( ), "Receive Failed: " + receive.Message ); return; }
			int protocol = BitConverter.ToInt32( receive.Content, 0 );

			if(!Directory.Exists( fileUpdatePath ) || (protocol != 0x1001 && protocol != 0x1002 && protocol != 0x2001) )
			{
				// 兼容原先版本的更新，新的验证方式无需理会
#if NET35 || NET20
				Send( socket, BitConverter.GetBytes( 10000f ) );
#else
				await SendAsync( socket, BitConverter.GetBytes( 10000f ) );
#endif
				socket?.Close( );
				return;
			}

			if (protocol == 0x2001)
			{
				// 新版的更新，数据切割发送，每一小块数据都确认返回，文件携带MD5码，服务器校验后确认是否需要下载，提高下载速度
				List<string> files = GetAllFiles( fileUpdatePath, LogNet );
				AppSession session = new AppSession( );
				session.WorkSocket = socket;
				lock (lockSessions) sessions.Add( session );
				// 发送文件个数
#if NET35 || NET20
				Send( socket, BitConverter.GetBytes( files.Count ) );
#else
				await SendAsync( socket, BitConverter.GetBytes( files.Count ) );
#endif
				foreach (string fileName in files)
				{
					FileInfo finfo = new FileInfo( fileName );
					string fileShortName = finfo.FullName.Replace( fileUpdatePath, "" );
					if (fileShortName.StartsWith( "\\" )) fileShortName = fileShortName.Substring( 1 );

					// 先发送文件的相关信息，文件名，文件大小，文件MD5码
					byte[] buffer = TranslateSourceData( new string[]
					{
						fileShortName,
						finfo.Length.ToString(),
						GetMD5(finfo)
					} );
					Send( socket, BitConverter.GetBytes( buffer.Length ) );
					Send( socket, buffer );

					// 接收客户端的反馈数据
#if NET35 || NET20
					OperateResult<byte[]> receiveCheck = Receive( socket, 4, 10_000 );
#else
					OperateResult<byte[]> receiveCheck = await ReceiveAsync( socket, 4, 10_000 );
#endif
					if (!receiveCheck.IsSuccess) { RemoveAndCloseSession( session ); return; }

					if (BitConverter.ToInt32( receiveCheck.Content, 0 ) == 0x01) continue; // 表示客户已经有该文件了，跳过请求

					// 开始发送文件
					using (FileStream fs = new FileStream( fileName, FileMode.Open, FileAccess.Read ))
					{
						buffer = new byte[4096 * 10];
						int sended = 0;
						while (sended < fs.Length)
						{
#if NET35 || NET20
							int count = fs.Read( buffer, 0, buffer.Length );
							OperateResult sendFile = Send( socket, buffer, 0, count );
#else
							int count = await fs.ReadAsync( buffer, 0, buffer.Length );
							OperateResult sendFile = await SendAsync( socket, buffer, 0, count );
#endif
							if (!sendFile.IsSuccess) { RemoveAndCloseSession( session ); return; }
							sended += count;
						}
					}

					// 确认对方收到
					while (true)
					{
#if NET35 || NET20
						receiveCheck = Receive( socket, 4, 60_000 );
#else
						receiveCheck = await ReceiveAsync( socket, 4, 60_000 );
#endif
						if (!receiveCheck.IsSuccess) { RemoveAndCloseSession( session ); return; }
						if (BitConverter.ToInt32( receiveCheck.Content, 0 ) >= finfo.Length) break; // 接收完成
					}
				}

				// 所有文件发送完成
				RemoveAndCloseSession( session );
			}
			else
			{
				// 旧版的更新方式，不管多少文件，都是直接从服务器下载下来，最后提供一个完成的信号返回
				AppSession session = new AppSession( );
				session.WorkSocket = socket;
				lock (lockSessions) sessions.Add( session );
				try
				{
					// 安装系统和更新系统
					if (protocol == 0x1001)
						LogNet?.WriteInfo( ToString( ), StringResources.Language.SystemInstallOperater + ((IPEndPoint)socket.RemoteEndPoint).Address.ToString( ) );
					else
						LogNet?.WriteInfo( ToString( ), StringResources.Language.SystemUpdateOperater + ((IPEndPoint)socket.RemoteEndPoint).Address.ToString( ) );

					List<string> Files = GetAllFiles( fileUpdatePath, LogNet );
					for (int i = Files.Count - 1; i >= 0; i--)
					{
						FileInfo finfo = new FileInfo( Files[i] );
						if (finfo.Length > 200000000)
						{
							Files.RemoveAt( i );
						}
						if (protocol == 0x1002)
						{
							if (finfo.Name == this.updateExeFileName)
							{
								Files.RemoveAt( i );
							}
						}
					}
					string[] files = Files.ToArray( );

					socket.BeginReceive( new byte[4], 0, 4, SocketFlags.None, new AsyncCallback( ReceiveCallBack ), session );
#if NET35 || NET20
					Send( socket, BitConverter.GetBytes( files.Length ) );
#else
					await SendAsync( socket, BitConverter.GetBytes( files.Length ) );
#endif
					for (int i = 0; i < files.Length; i++)
					{
						// 传送数据包含了本次数据大小，文件数据大小，文件名（带后缀）
						FileInfo finfo = new FileInfo( files[i] );
						string fileName = finfo.FullName.Replace( fileUpdatePath, "" );
						if (fileName.StartsWith( "\\" )) fileName = fileName.Substring( 1 );

						byte[] firstSend = GetFirstSendFileHead( fileName, (int)finfo.Length );
#if NET35 || NET20
						OperateResult sendFirst =  Send( socket, firstSend );
#else
						OperateResult sendFirst = await SendAsync( socket, firstSend );
#endif
						if (!sendFirst.IsSuccess) { RemoveAndCloseSession( session ); return; }
						Thread.Sleep( 10 );

						using (FileStream fs = new FileStream( files[i], FileMode.Open, FileAccess.Read ))
						{
							byte[] buffer = new byte[4096 * 10];
							int sended = 0;
							while (sended < fs.Length)
							{
#if NET35 || NET20
								int count = fs.Read( buffer, 0, buffer.Length );
								OperateResult sendFile = Send( socket, buffer, 0, count );
#else
								int count = await fs.ReadAsync( buffer, 0, buffer.Length );
								OperateResult sendFile = await SendAsync( socket, buffer, 0, count );
#endif
								if (!sendFile.IsSuccess) { RemoveAndCloseSession( session ); return; }
								sended += count;
							}
						}
						Thread.Sleep( 20 );
					}
				}
				catch (Exception ex)
				{
					RemoveAndCloseSession( session );
					LogNet?.WriteException( ToString( ), StringResources.Language.FileSendClientFailed, ex );
				}
			}
		}

		private void ReceiveCallBack(IAsyncResult ir)
		{
			if (ir.AsyncState is AppSession session)
			{
				try
				{
					session.WorkSocket.EndReceive(ir);
				}
				catch(Exception ex)
				{
					LogNet?.WriteException( ToString( ), ex);
				}
				finally
				{
					RemoveAndCloseSession( session );
				}
			}
		}

		private byte[] GetFirstSendFileHead( string relativeFileName, int fileLength )
		{
			byte[] byteName = Encoding.Unicode.GetBytes( relativeFileName );
			byte[] firstSend = new byte[4 + 4 + byteName.Length];

			Array.Copy( BitConverter.GetBytes( firstSend.Length ), 0, firstSend, 0, 4 );
			Array.Copy( BitConverter.GetBytes( fileLength ), 0, firstSend, 4, 4 );
			Array.Copy( byteName, 0, firstSend, 8, byteName.Length );
			return firstSend;
		}

		private byte[] TranslateSourceData( string[] parameters )
		{
			if (parameters == null) return new byte[0];
			MemoryStream ms = new MemoryStream( );
			foreach (string item in parameters)
			{
				byte[] buffer = string.IsNullOrEmpty( item ) ? new byte[0] : Encoding.UTF8.GetBytes( item );
				ms.Write( BitConverter.GetBytes( buffer.Length ), 0, 4 );
				if (buffer.Length > 0)
					ms.Write( buffer, 0, buffer.Length );
			}
			return ms.ToArray( );
		}

		private string[] TranslateFromSourceData( byte[] source )
		{
			if (source == null) return new string[0];
			List<string> list = new List<string>( );
			int index = 0;
			while(index < source.Length)
			{
				try
				{
					int length = BitConverter.ToInt32( source, index );
					index += 4;
					string data = length > 0 ? Encoding.UTF8.GetString( source, index, length ) : string.Empty;
					index += length;
					list.Add( data );
				}
				catch
				{
					return list.ToArray( );
				}
			}
			return list.ToArray( );
		}

		private string GetMD5( FileInfo fileInfo )
		{
			lock (lockMd5)
			{
				if (fileMd5.ContainsKey( fileInfo.FullName ))
				{
					if (fileInfo.LastWriteTime == fileMd5[fileInfo.FullName].ModifiTime)
						return fileMd5[fileInfo.FullName].MD5;
					else
					{
						fileMd5[fileInfo.FullName].MD5 = HslCommunication.BasicFramework.SoftBasic.CalculateFileMD5( fileInfo.FullName );
						return fileMd5[fileInfo.FullName].MD5;
					}
				}
				else
				{
					FileInfoExtension infoExtension = new FileInfoExtension( );
					infoExtension.FullName = fileInfo.FullName;
					infoExtension.ModifiTime = fileInfo.LastWriteTime;
					infoExtension.MD5 = HslCommunication.BasicFramework.SoftBasic.CalculateFileMD5( fileInfo.FullName );
					fileMd5.Add( infoExtension.FullName, infoExtension );
					return infoExtension.MD5;
				}
			}
		}

		/// <summary>
		/// 获取所有的文件信息，包括所有的子目录的文件信息<br />
		/// Get all file information, including file information of all subdirectories
		/// </summary>
		/// <param name="dircPath">目标路径</param>
		/// <param name="logNet">日志信息</param>
		/// <returns>文件名的列表</returns>
		public static List<string> GetAllFiles( string dircPath, ILogNet logNet )
		{
			List<string> fileList = new List<string>( );

			try
			{
				fileList.AddRange( Directory.GetFiles( dircPath ) );
			}
			catch(Exception ex)
			{
				logNet?.WriteWarn( "GetAllFiles", ex.Message );
			}
			foreach (var item in Directory.GetDirectories( dircPath ))
				fileList.AddRange( GetAllFiles( item, logNet ) );
			return fileList;
		}

		#region Private Member

		private string m_FilePath = @"C:\HslCommunication";
		private string updateExeFileName;                     // 软件更新的声明
		private List<AppSession> sessions = new List<AppSession>( );
		private object lockSessions = new object( );
		private object lockMd5      = new object( );
		private Dictionary<string, FileInfoExtension> fileMd5 = new Dictionary<string, FileInfoExtension>( );

		#endregion

		#region Object Override

		/// <inheritdoc/>
		public override string ToString( ) => $"NetSoftUpdateServer[{Port}]";

		#endregion

	}
}
