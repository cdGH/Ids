using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.IO;
using System.Security.Cryptography;
using System.Drawing;
using HslCommunication.BasicFramework;
using HslCommunication.LogNet;
using HslCommunication.Core;
using System.Runtime.InteropServices;
#if !NET35 && !NET20
using System.Threading.Tasks;
#endif

namespace HslCommunication.Enthernet
{
	/// <summary>
	/// 文件管理类服务器，负责服务器所有分类文件的管理，特点是不支持文件附加数据，但是支持直接访问文件名
	/// </summary>
	/// <remarks>
	/// 本文件的服务器不支持存储文件携带的额外信息，是直接将文件存放在服务器指定目录下的，文件名不更改，特点是服务器查看方便。
	/// </remarks>
	/// <example>
	/// 以下的示例来自Demo项目，创建了一个简单的服务器对象。
	/// <code lang="cs" source="TestProject\FileNetServer\FormFileServer.cs" region="Advanced Server" title="AdvancedFileServer示例" />
	/// </example>
	public class AdvancedFileServer : HslCommunication.Core.Net.NetworkFileServerBase
	{
		#region Constructor

		/// <summary>
		/// 实例化一个对象
		/// </summary>
		public AdvancedFileServer( )
		{

		}

		#endregion

		#region Override Method
#if NET35 || NET20
		/// <inheritdoc/>
		protected override void ThreadPoolLogin( Socket socket, IPEndPoint endPoint )
		{
			OperateResult result = new OperateResult( );
			// 获取ip地址
			string IpAddress = ((IPEndPoint)(socket.RemoteEndPoint)).Address.ToString( );

			// 接收操作信息
			OperateResult<FileGroupInfo> infoResult = ReceiveInformationHead( socket );
			if (!infoResult.IsSuccess) return;

			int customer = infoResult.Content.Command;
			string Factory = infoResult.Content.Factory;
			string Group = infoResult.Content.Group;
			string Identify = infoResult.Content.Identify;
			string fileName = infoResult.Content.FileName;

			string relativeName = GetRelativeFileName( Factory, Group, Identify, fileName );

			// 操作分流

			if (customer == HslProtocol.ProtocolFileDownload)
			{
				string fullFileName = ReturnAbsoluteFileName( Factory, Group, Identify, fileName );

				// 发送文件数据
				OperateResult sendFile = SendFileAndCheckReceive( socket, fullFileName, fileName, "", "" );
				if (!sendFile.IsSuccess)
				{
					LogNet?.WriteError( ToString( ), $"{StringResources.Language.FileDownloadFailed}:{relativeName} ip:{IpAddress} reason：{sendFile.Message}" );
					return;
				}
				else
				{
					socket?.Close( );
					LogNet?.WriteInfo( ToString( ), StringResources.Language.FileDownloadSuccess + ":" + relativeName );
				}
			}
			else if (customer == HslProtocol.ProtocolFileUpload)
			{
				string tempFileName = Path.Combine( FilesDirectoryPathTemp, CreateRandomFileName( ) );
				string fullFileName = ReturnAbsoluteFileName( Factory, Group, Identify, fileName );

				// 上传文件
				CheckFolderAndCreate( );

				// 创建新的文件夹
				try
				{
					FileInfo info = new FileInfo( fullFileName );
					if (!Directory.Exists( info.DirectoryName ))
					{
						Directory.CreateDirectory( info.DirectoryName );
					}
				}
				catch (Exception ex)
				{
					LogNet?.WriteException( ToString( ), StringResources.Language.FilePathCreateFailed + fullFileName, ex );
					socket?.Close( );
					return;
				}

				OperateResult<FileBaseInfo> receiveFile = ReceiveFileFromSocketAndMoveFile(
					socket,                                              // 网络套接字
					tempFileName,                                        // 临时保存文件路径
					fullFileName                                         // 最终保存文件路径
					);

				if (receiveFile.IsSuccess)
				{
					socket?.Close( );
					OnFileUpload( new FileServerInfo( )
					{
						ActualFileFullName = fullFileName,
						Name = receiveFile.Content.Name,
						Size = receiveFile.Content.Size,
						Tag = receiveFile.Content.Tag,
						Upload = receiveFile.Content.Upload
					} );
					LogNet?.WriteInfo( ToString( ), StringResources.Language.FileUploadSuccess + ":" + relativeName );
				}
				else
				{
					LogNet?.WriteInfo( ToString( ), StringResources.Language.FileUploadFailed + ":" + relativeName + " " + StringResources.Language.TextDescription + receiveFile.Message );
				}
			}
			else if (customer == HslProtocol.ProtocolFileDelete)
			{
				string fullFileName = ReturnAbsoluteFileName( Factory, Group, Identify, fileName );

				bool deleteResult = DeleteFileByName( fullFileName );

				// 回发消息
				if (SendStringAndCheckReceive(
					socket,                                                                // 网络套接字
					deleteResult ? 1 : 0,                                                  // 是否移动成功
					deleteResult ? StringResources.Language.FileDeleteSuccess : StringResources.Language.FileDeleteFailed
					).IsSuccess)
				{
					socket?.Close( );
				}

				if (deleteResult) LogNet?.WriteInfo( ToString( ), StringResources.Language.FileDeleteSuccess + ":" + relativeName );
			}
			else if (customer == HslProtocol.ProtocolFilesDelete)
			{
				bool deleteResult = true;
				foreach (var item in infoResult.Content.FileNames)
				{
					string fullFileName = ReturnAbsoluteFileName( Factory, Group, Identify, item );

					deleteResult = DeleteFileByName( fullFileName );
					if (deleteResult) LogNet?.WriteInfo( ToString( ), StringResources.Language.FileDeleteSuccess + ":" + relativeName );
					else
					{
						deleteResult = false;
						break;
					}
				}

				// 回发消息
				if (SendStringAndCheckReceive(
					socket,                                                                // 网络套接字
					deleteResult ? 1 : 0,                                                  // 是否移动成功
					deleteResult ? StringResources.Language.FileDeleteSuccess : StringResources.Language.FileDeleteFailed
					).IsSuccess)
				{
					socket?.Close( );
				}
			}
			else if (customer == HslProtocol.ProtocolFolderDelete)
			{
				string fullPath = ReturnAbsoluteFileName( Factory, Group, Identify, string.Empty );

				DirectoryInfo info = new DirectoryInfo( fullPath );
				bool deleteResult = false;
				try
				{
					if(info.Exists) info.Delete( true );
					deleteResult = true;
				}
				catch(Exception ex)
				{
					LogNet?.WriteInfo( ToString( ), StringResources.Language.FileDeleteFailed + $" [{fullPath}] " + ex.Message );
				}

				// 回发消息
				if (SendStringAndCheckReceive(
					socket,                                                                // 网络套接字
					deleteResult ? 1 : 0,                                                  // 是否移动成功
					deleteResult ? StringResources.Language.FileDeleteSuccess : StringResources.Language.FileDeleteFailed
					).IsSuccess)
				{
					socket?.Close( );
				}

				if (deleteResult) LogNet?.WriteInfo( ToString( ), StringResources.Language.FileDeleteSuccess + ":" + fullPath );
			}
			else if (customer == HslProtocol.ProtocolEmptyFolderDelete)
			{
				string fullPath = ReturnAbsoluteFileName( Factory, Group, Identify, string.Empty );

				DirectoryInfo info = new DirectoryInfo( fullPath );
				bool deleteResult = false;
				try
				{
					foreach (var path in info.GetDirectories( ))
					{
						if (path.GetFiles( )?.Length == 0)
						{
							if (path.Exists) path.Delete( true );
						}
					}
					deleteResult = true;
				}
				catch (Exception ex)
				{
					LogNet?.WriteInfo( ToString( ), StringResources.Language.FileDeleteFailed + $" [{fullPath}] " + ex.Message );
				}

				// 回发消息
				if (SendStringAndCheckReceive(
					socket,                                                                // 网络套接字
					deleteResult ? 1 : 0,                                                  // 是否移动成功
					deleteResult ? StringResources.Language.FileDeleteSuccess : StringResources.Language.FileDeleteFailed
					).IsSuccess)
				{
					socket?.Close( );
				}

				if (deleteResult) LogNet?.WriteInfo( ToString( ), StringResources.Language.FileDeleteSuccess + ":" + fullPath );
			}
			else if (customer == HslProtocol.ProtocolFileDirectoryFiles)
			{
				List<GroupFileItem> fileNames = new List<GroupFileItem>( );
				foreach (var m in GetDirectoryFiles( Factory, Group, Identify ))
				{
					FileInfo fileInfo = new FileInfo( m );
					fileNames.Add( new GroupFileItem( )
					{
						FileName = fileInfo.Name,
						FileSize = fileInfo.Length,
					} );
				}

				Newtonsoft.Json.Linq.JArray jArray = Newtonsoft.Json.Linq.JArray.FromObject( fileNames.ToArray( ) );
				if (SendStringAndCheckReceive(
					socket,
					HslProtocol.ProtocolFileDirectoryFiles,
					jArray.ToString( ) ).IsSuccess)
				{
					socket?.Close( );
				}
			}
			else if (customer == HslProtocol.ProtocolFileDirectories)
			{
				List<string> folders = new List<string>( );
				foreach (var m in GetDirectories( Factory, Group, Identify ))
				{
					DirectoryInfo directory = new DirectoryInfo( m );
					folders.Add( directory.Name );
				}

				Newtonsoft.Json.Linq.JArray jArray = Newtonsoft.Json.Linq.JArray.FromObject( folders.ToArray( ) );
				if (SendStringAndCheckReceive(
					socket,
					HslProtocol.ProtocolFileDirectoryFiles,
					jArray.ToString( ) ).IsSuccess)
				{
					socket?.Close( );
				}
			}
			else if (customer == HslProtocol.ProtocolFileExists)
			{
				string fullFileName = ReturnAbsoluteFileName( Factory, Group, Identify, fileName );
				bool isExists = File.Exists( fullFileName );

				if (SendStringAndCheckReceive(
					socket,                                           // 网络套接字
					isExists ? 1 : 0,                                 // 是否存在
					StringResources.Language.FileNotExist
					).IsSuccess)
				{
					socket?.Close( );
				}
			}
			else
			{
				socket?.Close( );
			}
		}
#else
		///<inheritdoc/>
		protected async override void ThreadPoolLogin( Socket socket, IPEndPoint endPoint )
		{
			OperateResult result = new OperateResult( );
			// 获取ip地址
			string IpAddress = endPoint.Address.ToString( );

			// 接收操作信息
			OperateResult<FileGroupInfo> infoResult = await ReceiveInformationHeadAsync( socket );
			if (!infoResult.IsSuccess) return;

			int customer = infoResult.Content.Command;
			string Factory = infoResult.Content.Factory;
			string Group = infoResult.Content.Group;
			string Identify = infoResult.Content.Identify;
			string fileName = infoResult.Content.FileName;

			string relativeName = GetRelativeFileName( Factory, Group, Identify, fileName );

			// 操作分流

			if (customer == HslProtocol.ProtocolFileDownload)
			{
				string fullFileName = ReturnAbsoluteFileName( Factory, Group, Identify, fileName );

				// 发送文件数据
				OperateResult sendFile = await SendFileAndCheckReceiveAsync( socket, fullFileName, fileName, "", "" );
				if (!sendFile.IsSuccess)
				{
					LogNet?.WriteError( ToString( ), $"{StringResources.Language.FileDownloadFailed}:{relativeName} ip:{IpAddress} reason：{sendFile.Message}" );
					return;
				}
				else
				{
					socket?.Close( );
					LogNet?.WriteInfo( ToString( ), StringResources.Language.FileDownloadSuccess + ":" + relativeName );
				}
			}
			else if (customer == HslProtocol.ProtocolFileUpload)
			{
				string tempFileName = Path.Combine( FilesDirectoryPathTemp, CreateRandomFileName( ) );
				string fullFileName = ReturnAbsoluteFileName( Factory, Group, Identify, fileName );

				// 上传文件
				CheckFolderAndCreate( );

				// 创建新的文件夹
				try
				{
					FileInfo info = new FileInfo( fullFileName );
					if (!Directory.Exists( info.DirectoryName ))
					{
						Directory.CreateDirectory( info.DirectoryName );
					}
				}
				catch (Exception ex)
				{
					LogNet?.WriteException( ToString( ), StringResources.Language.FilePathCreateFailed + fullFileName, ex );
					socket?.Close( );
					return;
				}

				OperateResult<FileBaseInfo> receiveFile = await ReceiveFileFromSocketAndMoveFileAsync(
					socket,                                              // 网络套接字
					tempFileName,                                        // 临时保存文件路径
					fullFileName                                         // 最终保存文件路径
					);

				if (receiveFile.IsSuccess)
				{
					socket?.Close( );
					OnFileUpload( new FileServerInfo( )
					{
						ActualFileFullName = fullFileName,
						Name = receiveFile.Content.Name,
						Size = receiveFile.Content.Size,
						Tag = receiveFile.Content.Tag,
						Upload = receiveFile.Content.Upload
					} );
					LogNet?.WriteInfo( ToString( ), StringResources.Language.FileUploadSuccess + ":" + relativeName );
				}
				else
				{
					LogNet?.WriteInfo( ToString( ), StringResources.Language.FileUploadFailed + ":" + relativeName + " " + StringResources.Language.TextDescription + receiveFile.Message );
				}
			}
			else if (customer == HslProtocol.ProtocolFileDelete)
			{
				string fullFileName = ReturnAbsoluteFileName( Factory, Group, Identify, fileName );

				bool deleteResult = DeleteFileByName( fullFileName );

				// 回发消息
				if ((await SendStringAndCheckReceiveAsync(
					socket,                                                                // 网络套接字
					deleteResult ? 1 : 0,                                                  // 是否移动成功
					deleteResult ? StringResources.Language.FileDeleteSuccess : StringResources.Language.FileDeleteFailed
					)).IsSuccess)
				{
					socket?.Close( );
				}

				if (deleteResult) LogNet?.WriteInfo( ToString( ), StringResources.Language.FileDeleteSuccess + ":" + relativeName );
			}
			else if (customer == HslProtocol.ProtocolFilesDelete)
			{
				bool deleteResult = true;
				foreach (var item in infoResult.Content.FileNames)
				{
					string fullFileName = ReturnAbsoluteFileName( Factory, Group, Identify, item );

					deleteResult = DeleteFileByName( fullFileName );
					if (deleteResult) LogNet?.WriteInfo( ToString( ), StringResources.Language.FileDeleteSuccess + ":" + relativeName );
					else
					{
						deleteResult = false;
						break;
					}
				}

				// 回发消息
				if ((await SendStringAndCheckReceiveAsync(
					socket,                                                                // 网络套接字
					deleteResult ? 1 : 0,                                                  // 是否移动成功
					deleteResult ? StringResources.Language.FileDeleteSuccess : StringResources.Language.FileDeleteFailed
					)).IsSuccess)
				{
					socket?.Close( );
				}
			}
			else if (customer == HslProtocol.ProtocolFolderDelete)
			{
				string fullPath = ReturnAbsoluteFileName( Factory, Group, Identify, string.Empty );

				DirectoryInfo info = new DirectoryInfo( fullPath );
				bool deleteResult = false;
				try
				{
					if (info.Exists) info.Delete( true );
					deleteResult = true;
				}
				catch (Exception ex)
				{
					LogNet?.WriteInfo( ToString( ), StringResources.Language.FileDeleteFailed + $" [{fullPath}] " + ex.Message );
				}

				// 回发消息
				if ((await SendStringAndCheckReceiveAsync(
					socket,                                                                // 网络套接字
					deleteResult ? 1 : 0,                                                  // 是否移动成功
					deleteResult ? StringResources.Language.FileDeleteSuccess : StringResources.Language.FileDeleteFailed
					)).IsSuccess)
				{
					socket?.Close( );
				}

				if (deleteResult) LogNet?.WriteInfo( ToString( ), StringResources.Language.FileDeleteSuccess + ":" + fullPath );
			}
			else if (customer == HslProtocol.ProtocolEmptyFolderDelete)
			{
				string fullPath = ReturnAbsoluteFileName( Factory, Group, Identify, string.Empty );

				DirectoryInfo info = new DirectoryInfo( fullPath );
				bool deleteResult = false;
				try
				{
					foreach(var path in info.GetDirectories( ))
					{
						if(path.GetFiles()?.Length == 0)
						{
							if (path.Exists) path.Delete( true );
						}
					}
					deleteResult = true;
				}
				catch (Exception ex)
				{
					LogNet?.WriteInfo( ToString( ), StringResources.Language.FileDeleteFailed + $" [{fullPath}] " + ex.Message );
				}

				// 回发消息
				if ((await SendStringAndCheckReceiveAsync(
					socket,                                                                // 网络套接字
					deleteResult ? 1 : 0,                                                  // 是否移动成功
					deleteResult ? StringResources.Language.FileDeleteSuccess : StringResources.Language.FileDeleteFailed
					)).IsSuccess)
				{
					socket?.Close( );
				}

				if (deleteResult) LogNet?.WriteInfo( ToString( ), StringResources.Language.FileDeleteSuccess + ":" + fullPath );
			}
			else if (customer == HslProtocol.ProtocolFileDirectoryFiles)
			{
				List<GroupFileItem> fileNames = new List<GroupFileItem>( );
				foreach (var m in GetDirectoryFiles( Factory, Group, Identify ))
				{
					FileInfo fileInfo = new FileInfo( m );
					fileNames.Add( new GroupFileItem( )
					{
						FileName = fileInfo.Name,
						FileSize = fileInfo.Length,
					} );
				}

				Newtonsoft.Json.Linq.JArray jArray = Newtonsoft.Json.Linq.JArray.FromObject( fileNames.ToArray( ) );
				if ((await SendStringAndCheckReceiveAsync(
					socket,
					HslProtocol.ProtocolFileDirectoryFiles,
					jArray.ToString( ) )).IsSuccess)
				{
					socket?.Close( );
				}
			}
			else if (customer == HslProtocol.ProtocolFileDirectories)
			{
				List<string> folders = new List<string>( );
				foreach (var m in GetDirectories( Factory, Group, Identify ))
				{
					DirectoryInfo directory = new DirectoryInfo( m );
					folders.Add( directory.Name );
				}

				Newtonsoft.Json.Linq.JArray jArray = Newtonsoft.Json.Linq.JArray.FromObject( folders.ToArray( ) );
				if ((await SendStringAndCheckReceiveAsync(
					socket,
					HslProtocol.ProtocolFileDirectoryFiles,
					jArray.ToString( ) )).IsSuccess)
				{
					socket?.Close( );
				}
			}
			else if (customer == HslProtocol.ProtocolFileExists)
			{
				string fullFileName = ReturnAbsoluteFileName( Factory, Group, Identify, fileName );
				bool isExists = File.Exists( fullFileName );

				if ((await SendStringAndCheckReceiveAsync(
					socket,                                           // 网络套接字
					isExists ? 1 : 0,                                 // 是否存在
					StringResources.Language.FileNotExist
					)).IsSuccess)
				{
					socket?.Close( );
				}
			}
			else
			{
				socket?.Close( );
			}
		}
#endif

		/// <inheritdoc/>
		protected override void StartInitialization( )
		{
			if (string.IsNullOrEmpty( FilesDirectoryPathTemp ))
			{
				throw new ArgumentNullException( "FilesDirectoryPathTemp", "No saved path is specified" );
			}

			base.StartInitialization( );
		}

		/// <inheritdoc/>
		protected override void CheckFolderAndCreate( )
		{
			if (!Directory.Exists( FilesDirectoryPathTemp ))
			{
				Directory.CreateDirectory( FilesDirectoryPathTemp );
			}

			base.CheckFolderAndCreate( );
		}

		/// <summary>
		/// 从网络套接字接收文件并移动到目标的文件夹中，如果结果异常，则结束通讯
		/// </summary>
		/// <param name="socket"></param>
		/// <param name="savename"></param>
		/// <param name="fileNameNew"></param>
		/// <returns></returns>
		private OperateResult<FileBaseInfo> ReceiveFileFromSocketAndMoveFile( Socket socket, string savename, string fileNameNew )
		{
			// 先接收文件
			OperateResult<FileBaseInfo> fileInfo = ReceiveFileFromSocket( socket, savename, null );
			if (!fileInfo.IsSuccess)
			{
				DeleteFileByName( savename );
				return OperateResult.CreateFailedResult<FileBaseInfo>( fileInfo );
			}

			// 标记移动文件，失败尝试三次
			int customer = 0;
			int times = 0;
			while (times < 3)
			{
				times++;
				if (MoveFileToNewFile( savename, fileNameNew ))
				{
					customer = 1;
					break;
				}
				else
				{
					Thread.Sleep( 500 );
				}
			}

			if (customer == 0)
			{
				DeleteFileByName( savename );
			}

			// 回发消息
			OperateResult sendString = SendStringAndCheckReceive( socket, customer, "success" );
			if (!sendString.IsSuccess) return OperateResult.CreateFailedResult<FileBaseInfo>( sendString );

			return OperateResult.CreateSuccessResult( fileInfo.Content );
		}
#if !NET35 && !NET20

		/// <inheritdoc cref="ReceiveFileFromSocketAndMoveFile(Socket, string, string)"/>
		private async Task<OperateResult<FileBaseInfo>> ReceiveFileFromSocketAndMoveFileAsync( Socket socket, string savename, string fileNameNew )
		{
			// 先接收文件
			OperateResult<FileBaseInfo> fileInfo = await ReceiveFileFromSocketAsync( socket, savename, null );
			if (!fileInfo.IsSuccess)
			{
				DeleteFileByName( savename );
				return OperateResult.CreateFailedResult<FileBaseInfo>( fileInfo );
			}

			// 标记移动文件，失败尝试三次
			int customer = 0;
			int times = 0;
			while (times < 3)
			{
				times++;
				if (MoveFileToNewFile( savename, fileNameNew ))
				{
					customer = 1;
					break;
				}
				else
				{
					Thread.Sleep( 500 );
				}
			}

			if (customer == 0)
			{
				DeleteFileByName( savename );
			}

			// 回发消息
			OperateResult sendString = await SendStringAndCheckReceiveAsync( socket, customer, "success" );
			if (!sendString.IsSuccess) return OperateResult.CreateFailedResult<FileBaseInfo>( sendString );

			return OperateResult.CreateSuccessResult( fileInfo.Content );
		}
#endif
		#endregion

		#region Public Properties

		/// <summary>
		/// 用于接收上传文件时的临时文件夹，临时文件使用结束后会被删除<br />
		/// Used to receive the temporary folder when uploading files. The temporary files will be deleted after use
		/// </summary>
		public string FilesDirectoryPathTemp
		{
			get { return m_FilesDirectoryPathTemp; }
			set { m_FilesDirectoryPathTemp = PreprocessFolderName( value ); }
		}

		#endregion

		#region Private Member

		private string m_FilesDirectoryPathTemp = null;

		#endregion

		#region Object Override

		/// <inheritdoc/>
		public override string ToString( ) => $"AdvancedFileServer[{Port}]";

		#endregion

	}
}
