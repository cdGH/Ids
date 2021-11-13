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
using HslCommunication.Reflection;
#if !NET35 && !NET20
using System.Threading.Tasks;
#endif

namespace HslCommunication.Enthernet
{
	/// <summary>
	/// 一个终极文件管理服务器，可以实现对所有的文件分类管理，本服务器支持读写分离，支持同名文件，
	/// 客户端使用<see cref="IntegrationFileClient"/>进行访问，支持上传，下载，删除，请求文件列表，校验文件是否存在操作。<br />
	/// An ultimate file management server, which can realize classified management of all files. This server supports read-write separation, 
	/// supports files with the same name, and the client uses <see cref="IntegrationFileClient"/> to access, 
	/// supports upload, download, delete, and request files List, check whether the file exists operation.
	/// </summary>
	/// <remarks>
	/// 本文件的服务器支持存储文件携带上传人的信息，备注信息，文件名被映射成了新的名称，无法在服务器直接查看文件信息。
	/// </remarks>
	/// <example>
	/// 以下的示例来自Demo项目，创建了一个简单的服务器对象。
	/// <code lang="cs" source="TestProject\FileNetServer\FormFileServer.cs" region="Ultimate Server" title="UltimateFileServer示例" />
	/// </example>
	public class UltimateFileServer : Core.Net.NetworkFileServerBase
	{
		#region Constructor

		/// <summary>
		/// 实例化一个默认的对象<br />
		/// Instantiate a default object
		/// </summary>
		public UltimateFileServer( ) { }

		#endregion

		#region File list Container

		/// <summary>
		/// 获取当前的针对文件夹的文件管理容器的数量<br />
		/// Get the current number of file management containers for the folder
		/// </summary>
		[HslMqttApi( Description = "Get the current number of file management containers for the folder" )]
		public int GroupFileContainerCount( ) => m_dictionary_group_marks.Count;

		/// <summary>
		/// 所有文件组操作的词典锁
		/// </summary>
		internal Dictionary<string, GroupFileContainer> m_dictionary_group_marks = new Dictionary<string, GroupFileContainer>( );

		/// <summary>
		/// 词典的锁
		/// </summary>
		private SimpleHybirdLock hybirdLock = new SimpleHybirdLock( );

		/// <summary>
		/// 获取当前目录的文件列表管理容器，如果没有会自动创建，通过该容器可以实现对当前目录的文件进行访问<br />
		/// Get the file list management container of the current directory. If not, it will be created automatically. 
		/// Through this container, you can access files in the current directory.
		/// </summary>
		/// <param name="filePath">路径信息</param>
		/// <returns>文件管理容器信息</returns>
		public GroupFileContainer GetGroupFromFilePath( string filePath )
		{
			GroupFileContainer groupFile = null;
			// 全部修改为大写
			filePath = filePath.ToUpper( );
			hybirdLock.Enter( );

			// lock operator
			if (m_dictionary_group_marks.ContainsKey( filePath ))
			{
				groupFile = m_dictionary_group_marks[filePath];
			}
			else
			{
				groupFile = new GroupFileContainer( LogNet, filePath );
				m_dictionary_group_marks.Add( filePath, groupFile );
			}

			hybirdLock.Leave( );
			return groupFile;
		}

		/// <summary>
		/// 清除系统中所有空的路径信息
		/// </summary>
		public void DeleteGroupFile( GroupFileContainer groupFile )
		{
			hybirdLock.Enter( );
			if (m_dictionary_group_marks.ContainsKey( groupFile.DirectoryPath ))
				m_dictionary_group_marks.Remove( groupFile.DirectoryPath );
			try
			{
				Directory.Delete( groupFile.DirectoryPath, true );
			}
			catch
			{

			}
			hybirdLock.Leave( );
		}

		#endregion

		#region Receive File And Updata List

		/// <summary>
		/// 从套接字接收文件并保存，更新文件列表
		/// </summary>
		/// <param name="socket">套接字</param>
		/// <param name="savename">保存的文件名</param>
		/// <returns>是否成功的结果对象</returns>
		private OperateResult<FileBaseInfo> ReceiveFileFromSocketAndUpdateGroup( Socket socket, string savename )
		{
			FileInfo info = new FileInfo( savename );
			string guidName = CreateRandomFileName( );

			string fileName = Path.Combine( info.DirectoryName, guidName );

			OperateResult<FileBaseInfo> receive = ReceiveFileFromSocket( socket, fileName, null );
			if(!receive.IsSuccess)
			{
				DeleteFileByName( fileName );
				return receive;
			}

			// 更新操作
			GroupFileContainer fileManagment = GetGroupFromFilePath( info.DirectoryName );
			string oldName = fileManagment.UpdateFileMappingName(
				info.Name,
				receive.Content.Size,
				guidName,
				receive.Content.Upload,
				receive.Content.Tag
				);

			// 删除旧的文件
			DeleteExsistingFile( info.DirectoryName, oldName );

			// 回发消息
			OperateResult sendBack = SendStringAndCheckReceive( socket, 1, StringResources.Language.SuccessText );
			if (!sendBack.IsSuccess) return OperateResult.CreateFailedResult<FileBaseInfo>( sendBack );

			return OperateResult.CreateSuccessResult( receive.Content );
		}

		#endregion

		#region Async Receive File And Updata List
#if !NET35 && !NET20
		/// <summary>
		/// 从套接字接收文件并保存，更新文件列表
		/// </summary>
		/// <param name="socket">套接字</param>
		/// <param name="savename">保存的文件名</param>
		/// <returns>是否成功的结果对象</returns>
		private async Task<OperateResult<FileBaseInfo>> ReceiveFileFromSocketAndUpdateGroupAsync( Socket socket, string savename )
		{
			FileInfo info = new FileInfo( savename );
			string guidName = CreateRandomFileName( );

			string fileName = Path.Combine( info.DirectoryName, guidName );

			OperateResult<FileBaseInfo> receive = await ReceiveFileFromSocketAsync( socket, fileName, null );
			if (!receive.IsSuccess)
			{
				DeleteFileByName( fileName );
				return receive;
			}

			// 更新操作
			GroupFileContainer fileManagment = GetGroupFromFilePath( info.DirectoryName );
			string oldName = fileManagment.UpdateFileMappingName(
				info.Name,
				receive.Content.Size,
				guidName,
				receive.Content.Upload,
				receive.Content.Tag
				);

			// 删除旧的文件
			DeleteExsistingFile( info.DirectoryName, oldName );

			// 回发消息
			OperateResult sendBack = await SendStringAndCheckReceiveAsync( socket, 1, StringResources.Language.SuccessText );
			if (!sendBack.IsSuccess) return OperateResult.CreateFailedResult<FileBaseInfo>( sendBack );

			return OperateResult.CreateSuccessResult( receive.Content );
		}
#endif
		#endregion

		#region Private Method

		/// <summary>
		/// 根据文件的显示名称转化为真实存储的名称，例如 123.txt 获取到在文件服务器里映射的文件名称，例如返回 b35a11ec533147ca80c7f7d1713f015b7909
		/// </summary>
		/// <param name="factory">第一大类</param>
		/// <param name="group">第二大类</param>
		/// <param name="id">第三大类</param>
		/// <param name="fileName">文件显示名称</param>
		/// <returns>是否成功的结果对象</returns>
		private string TransformFactFileName( string factory, string group, string id, string fileName )
		{
			string path = ReturnAbsoluteFilePath( factory, group, id );
			GroupFileContainer fileManagment = GetGroupFromFilePath( path );
			return fileManagment.GetCurrentFileMappingName( fileName );
		}

		/// <summary>
		/// 删除已经存在的文件信息，文件的名称需要是guid名称，例如 b35a11ec533147ca80c7f7d1713f015b7909
		/// </summary>
		/// <param name="path">文件的路径</param>
		/// <param name="fileName">文件的guid名称，例如 b35a11ec533147ca80c7f7d1713f015b7909</param>
		private void DeleteExsistingFile( string path, string fileName ) => DeleteExsistingFile( path, new List<string>( ) { fileName } );

		/// <summary>
		/// 删除已经存在的文件信息，文件的名称需要是guid名称，例如 b35a11ec533147ca80c7f7d1713f015b7909
		/// </summary>
		/// <param name="path">文件的路径</param>
		/// <param name="fileNames">文件的guid名称，例如 b35a11ec533147ca80c7f7d1713f015b7909</param>
		private void DeleteExsistingFile( string path, List<string> fileNames )
		{
			foreach (var fileName in fileNames)
			{
				if (!string.IsNullOrEmpty( fileName ))
				{
					string fileUltimatePath = Path.Combine( path, fileName );
					FileMarkId fileMarkId = GetFileMarksFromDictionaryWithFileName( fileName );

					fileMarkId.AddOperation( ( ) =>
					{
						if (!DeleteFileByName( fileUltimatePath ))
						{
							LogNet?.WriteInfo( ToString( ), StringResources.Language.FileDeleteFailed + fileUltimatePath );
						}
						else
						{
							LogNet?.WriteInfo( ToString( ), StringResources.Language.FileDeleteSuccess + fileUltimatePath );
						}
					} );
				}
			}
		}

		#endregion

		#region Protect Override
#if NET35 || NET20
		/// <inheritdoc/>
		protected override void ThreadPoolLogin( Socket socket, IPEndPoint endPoint )
		{
			// 获取ip地址
			string IpAddress = endPoint.Address.ToString( );

			// 接收操作信息
			OperateResult<FileGroupInfo> infoResult = ReceiveInformationHead( socket );
			if (!infoResult.IsSuccess) return;

			int customer    = infoResult.Content.Command;
			string Factory  = infoResult.Content.Factory;
			string Group    = infoResult.Content.Group;
			string Identify = infoResult.Content.Identify;
			string fileName = infoResult.Content.FileName;

			string relativeName = GetRelativeFileName( Factory, Group, Identify, fileName );

			if (customer == HslProtocol.ProtocolFileDownload)
			{
				// 先获取文件的真实名称
				string guidName = TransformFactFileName( Factory, Group, Identify, fileName );
				// 获取文件操作锁
				FileMarkId fileMarkId = GetFileMarksFromDictionaryWithFileName( guidName );
				fileMarkId.EnterReadOperator( );
				// 发送文件数据
				OperateResult send = SendFileAndCheckReceive( socket, ReturnAbsoluteFileName( Factory, Group, Identify, guidName ), fileName, "", "", null );
				if (!send.IsSuccess)
				{
					fileMarkId.LeaveReadOperator( );
					LogNet?.WriteError( ToString( ), $"{StringResources.Language.FileDownloadFailed} : {send.Message} :{relativeName} ip:{IpAddress}" );
					return;
				}
				else
				{
					LogNet?.WriteInfo( ToString( ), StringResources.Language.FileDownloadSuccess + ":" + relativeName );
				}

				fileMarkId.LeaveReadOperator( );
				// 关闭连接
				socket?.Close( );
			}
			else if (customer == HslProtocol.ProtocolFileUpload)
			{
				string fullFileName = ReturnAbsoluteFileName( Factory, Group, Identify, fileName );
				// 上传文件
				CheckFolderAndCreate( );
				FileInfo info = new FileInfo( fullFileName );

				try
				{
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

				// 接收文件并回发消息
				OperateResult<FileBaseInfo> receive = ReceiveFileFromSocketAndUpdateGroup( socket, fullFileName );

				if (receive.IsSuccess)
				{
					socket?.Close( );
					OnFileUpload( new FileServerInfo( )
					{
						ActualFileFullName = fullFileName,
						Name = receive.Content.Name,
						Size = receive.Content.Size,
						Tag = receive.Content.Tag,
						Upload = receive.Content.Upload
					} );
					LogNet?.WriteInfo( ToString( ), StringResources.Language.FileUploadSuccess + ":" + relativeName );
				}
				else
				{
					LogNet?.WriteInfo( ToString( ), StringResources.Language.FileUploadFailed + ":" + relativeName );
				}
			}
			else if (customer == HslProtocol.ProtocolFileDelete)
			{
				string fullFileName = ReturnAbsoluteFileName( Factory, Group, Identify, fileName );

				FileInfo info = new FileInfo( fullFileName );
				GroupFileContainer fileManagment = GetGroupFromFilePath( info.DirectoryName );

				// 新增删除的任务
				DeleteExsistingFile( info.DirectoryName, fileManagment.DeleteFile( info.Name ) );

				// 回发消息
				if ((SendStringAndCheckReceive( socket, 1, "success" )).IsSuccess) socket?.Close( );

				LogNet?.WriteInfo( ToString( ), StringResources.Language.FileDeleteSuccess + ":" + relativeName );
			}
			else if (customer == HslProtocol.ProtocolFilesDelete)
			{
				// 删除多个文件
				foreach (var item in infoResult.Content.FileNames)
				{
					string fullFileName = ReturnAbsoluteFileName( Factory, Group, Identify, item );

					FileInfo info = new FileInfo( fullFileName );
					GroupFileContainer fileManagment = GetGroupFromFilePath( info.DirectoryName );

					// 新增删除的任务
					DeleteExsistingFile( info.DirectoryName, fileManagment.DeleteFile( info.Name ) );

					relativeName = GetRelativeFileName( Factory, Group, Identify, fileName );
					LogNet?.WriteInfo( ToString( ), StringResources.Language.FileDeleteSuccess + ":" + relativeName );
				}

				// 回发消息，1和success没有什么含义
				if ((SendStringAndCheckReceive( socket, 1, "success" )).IsSuccess) socket?.Close( );
			}
			else if (customer == HslProtocol.ProtocolFolderDelete)
			{
				string fullFileName = ReturnAbsoluteFileName( Factory, Group, Identify, "123.txt" );

				FileInfo info = new FileInfo( fullFileName );
				GroupFileContainer fileManagment = GetGroupFromFilePath( info.DirectoryName );

				// 新增删除的任务
				// DeleteExsistingFile( info.DirectoryName, fileManagment.ClearAllFiles( ) );
				DeleteGroupFile( fileManagment );

				// 回发消息，1和success没有什么含义
				if (SendStringAndCheckReceive( socket, 1, "success" ).IsSuccess) socket?.Close( );
				LogNet?.WriteInfo( ToString( ), "FolderDelete : " + relativeName );
			}
			else if (customer == HslProtocol.ProtocolEmptyFolderDelete)
			{
				foreach (var m in GetDirectories( Factory, Group, Identify ))
				{
					DirectoryInfo directory = new DirectoryInfo( m );
					GroupFileContainer fileManagment = GetGroupFromFilePath( directory.FullName );
					if (fileManagment.FileCount == 0)
						DeleteGroupFile( fileManagment );
				}
				if (SendStringAndCheckReceive( socket, 1, "success" ).IsSuccess) socket?.Close( );
				LogNet?.WriteInfo( ToString( ), "FolderEmptyDelete : " + relativeName );
			}
			else if (customer == HslProtocol.ProtocolFileDirectoryFiles)
			{
				GroupFileContainer fileManagment = GetGroupFromFilePath( ReturnAbsoluteFilePath( Factory, Group, Identify ) );

				if (SendStringAndCheckReceive(
					socket,
					HslProtocol.ProtocolFileDirectoryFiles,
					fileManagment.JsonArrayContent ).IsSuccess)
				{
					socket?.Close( );
				}
			}
			else if (customer == HslProtocol.ProtocolFolderInfo)
			{
				GroupFileContainer fileManagment = GetGroupFromFilePath( ReturnAbsoluteFilePath( Factory, Group, Identify ) );

				if (SendStringAndCheckReceive(
					socket,
					HslProtocol.ProtocolFolderInfo,
					fileManagment.GetGroupFileInfo( ).ToJsonString( ) ).IsSuccess)
				{
					socket?.Close( );
				}
			}
			else if (customer == HslProtocol.ProtocolFolderInfos)
			{
				List<GroupFileInfo> folders = new List<GroupFileInfo>( );
				foreach (var m in GetDirectories( Factory, Group, Identify ))
				{
					DirectoryInfo directory = new DirectoryInfo( m );
					if (string.IsNullOrEmpty( Factory ))
					{
						GroupFileContainer fileManagment = GetGroupFromFilePath( ReturnAbsoluteFilePath( directory.Name, string.Empty, string.Empty ) );
						GroupFileInfo groupFileInfo = fileManagment.GetGroupFileInfo( );
						groupFileInfo.PathName = directory.Name;
						folders.Add( groupFileInfo );
					}
					else if (string.IsNullOrEmpty( Group ))
					{
						GroupFileContainer fileManagment = GetGroupFromFilePath( ReturnAbsoluteFilePath( Factory, directory.Name, string.Empty ) );
						GroupFileInfo groupFileInfo = fileManagment.GetGroupFileInfo( );
						groupFileInfo.PathName = directory.Name;
						folders.Add( groupFileInfo );
					}
					else
					{
						GroupFileContainer fileManagment = GetGroupFromFilePath( ReturnAbsoluteFilePath( Factory, Group, directory.Name ) );
						GroupFileInfo groupFileInfo = fileManagment.GetGroupFileInfo( );
						groupFileInfo.PathName = directory.Name;
						folders.Add( groupFileInfo );
					}
				}
				if (SendStringAndCheckReceive(
					socket,
					HslProtocol.ProtocolFolderInfos,
					folders.ToJsonString( ) ).IsSuccess)
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
				string fullPath = ReturnAbsoluteFilePath( Factory, Group, Identify );
				GroupFileContainer fileManagment = GetGroupFromFilePath( fullPath );

				bool isExists = fileManagment.FileExists( fileName );
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
				// close not supported client
				socket?.Close( );
			}
		}
#endif
		#endregion

		#region Async Protect Override
#if !NET35 && !NET20
		/// <inheritdoc/>
		protected override async void ThreadPoolLogin( Socket socket, IPEndPoint endPoint )
		{
			// 获取ip地址
			string IpAddress = endPoint.Address.ToString( );

			// 接收操作信息
			OperateResult<FileGroupInfo> infoResult = await ReceiveInformationHeadAsync( socket );
			if (!infoResult.IsSuccess) return;

			int customer     = infoResult.Content.Command;
			string Factory   = infoResult.Content.Factory;
			string Group     = infoResult.Content.Group;
			string Identify  = infoResult.Content.Identify;
			string fileName  = infoResult.Content.FileName;

			string relativeName = GetRelativeFileName( Factory, Group, Identify, fileName );

			if (customer == HslProtocol.ProtocolFileDownload)
			{
				// 先获取文件的真实名称
				string guidName = TransformFactFileName( Factory, Group, Identify, fileName );
				// 获取文件操作锁
				FileMarkId fileMarkId = GetFileMarksFromDictionaryWithFileName( guidName );
				fileMarkId.EnterReadOperator( );
				// 发送文件数据
				OperateResult send = await SendFileAndCheckReceiveAsync( socket, ReturnAbsoluteFileName( Factory, Group, Identify, guidName ), fileName, "", "", null );
				if (!send.IsSuccess)
				{
					fileMarkId.LeaveReadOperator( );
					LogNet?.WriteError( ToString( ), $"{StringResources.Language.FileDownloadFailed} : {send.Message} :{relativeName} ip:{IpAddress}" );
					return;
				}
				else
				{
					LogNet?.WriteInfo( ToString( ), StringResources.Language.FileDownloadSuccess + ":" + relativeName );
				}

				fileMarkId.LeaveReadOperator( );
				// 关闭连接
				socket?.Close( );
			}
			else if (customer == HslProtocol.ProtocolFileUpload)
			{
				string fullFileName = ReturnAbsoluteFileName( Factory, Group, Identify, fileName );
				// 上传文件
				CheckFolderAndCreate( );
				FileInfo info = new FileInfo( fullFileName );

				try
				{
					if (!Directory.Exists( info.DirectoryName )) Directory.CreateDirectory( info.DirectoryName );
				}
				catch (Exception ex)
				{
					LogNet?.WriteException( ToString( ), StringResources.Language.FilePathCreateFailed + fullFileName, ex );
					socket?.Close( );
					return;
				}

				// 接收文件并回发消息
				OperateResult<FileBaseInfo> receive = await ReceiveFileFromSocketAndUpdateGroupAsync( socket, fullFileName );

				if (receive.IsSuccess)
				{
					socket?.Close( );
					OnFileUpload( new FileServerInfo( )
					{
						ActualFileFullName = fullFileName,
						Name = receive.Content.Name,
						Size = receive.Content.Size,
						Tag = receive.Content.Tag,
						Upload = receive.Content.Upload
					} );
					LogNet?.WriteInfo( ToString( ), StringResources.Language.FileUploadSuccess + ":" + relativeName );
				}
				else
				{
					LogNet?.WriteInfo( ToString( ), StringResources.Language.FileUploadFailed + ":" + relativeName );
				}
			}
			else if (customer == HslProtocol.ProtocolFileDelete)
			{
				string fullFileName = ReturnAbsoluteFileName( Factory, Group, Identify, fileName );

				FileInfo info = new FileInfo( fullFileName );
				GroupFileContainer fileManagment = GetGroupFromFilePath( info.DirectoryName );

				// 新增删除的任务
				DeleteExsistingFile( info.DirectoryName, fileManagment.DeleteFile( info.Name ) );

				// 回发消息，1和success没有什么含义
				if ((await SendStringAndCheckReceiveAsync( socket, 1, "success" )).IsSuccess) socket?.Close( );

				LogNet?.WriteInfo( ToString( ), StringResources.Language.FileDeleteSuccess + ":" + relativeName );
			}
			else if (customer == HslProtocol.ProtocolFilesDelete)
			{
				// 删除多个文件
				foreach (var item in infoResult.Content.FileNames)
				{
					string fullFileName = ReturnAbsoluteFileName( Factory, Group, Identify, item );

					FileInfo info = new FileInfo( fullFileName );
					GroupFileContainer fileManagment = GetGroupFromFilePath( info.DirectoryName );

					// 新增删除的任务
					DeleteExsistingFile( info.DirectoryName, fileManagment.DeleteFile( info.Name ) );

					relativeName = GetRelativeFileName( Factory, Group, Identify, fileName );
					LogNet?.WriteInfo( ToString( ), StringResources.Language.FileDeleteSuccess + ":" + relativeName );
				}

				// 回发消息，1和success没有什么含义
				if ((await SendStringAndCheckReceiveAsync( socket, 1, "success" )).IsSuccess) socket?.Close( );
			}
			else if (customer == HslProtocol.ProtocolFolderDelete)
			{
				string fullFileName = ReturnAbsoluteFileName( Factory, Group, Identify, "123.txt" );

				FileInfo info = new FileInfo( fullFileName );
				GroupFileContainer fileManagment = GetGroupFromFilePath( info.DirectoryName );

				// 新增删除的任务
				// DeleteExsistingFile( info.DirectoryName, fileManagment.ClearAllFiles( ) );
				DeleteGroupFile( fileManagment );

				// 回发消息，1和success没有什么含义
				if ((await SendStringAndCheckReceiveAsync( socket, 1, "success" )).IsSuccess) socket?.Close( );
				LogNet?.WriteInfo( ToString( ), "FolderDelete : " + relativeName );
			}
			else if (customer == HslProtocol.ProtocolEmptyFolderDelete)
			{
				foreach (var m in GetDirectories( Factory, Group, Identify ))
				{
					DirectoryInfo directory = new DirectoryInfo( m );
					GroupFileContainer fileManagment = GetGroupFromFilePath( directory.FullName );
					if (fileManagment.FileCount == 0)
						DeleteGroupFile( fileManagment );
				}
				if ((await SendStringAndCheckReceiveAsync( socket, 1, "success" )).IsSuccess) socket?.Close( );
				LogNet?.WriteInfo( ToString( ), "FolderEmptyDelete : " + relativeName );
			}
			else if (customer == HslProtocol.ProtocolFileDirectoryFiles)
			{
				GroupFileContainer fileManagment = GetGroupFromFilePath( ReturnAbsoluteFilePath( Factory, Group, Identify ) );

				if ((await SendStringAndCheckReceiveAsync(
					socket,
					HslProtocol.ProtocolFileDirectoryFiles,
					fileManagment.JsonArrayContent )).IsSuccess)
				{
					socket?.Close( );
				}
			}
			else if (customer == HslProtocol.ProtocolFolderInfo)
			{
				GroupFileContainer fileManagment = GetGroupFromFilePath( ReturnAbsoluteFilePath( Factory, Group, Identify ) );

				if ((await SendStringAndCheckReceiveAsync(
					socket,
					HslProtocol.ProtocolFileDirectoryFiles,
					fileManagment.GetGroupFileInfo( ).ToJsonString( ) )).IsSuccess)
				{
					socket?.Close( );
				}
			}
			else if (customer == HslProtocol.ProtocolFolderInfos)
			{
				List<GroupFileInfo> folders = new List<GroupFileInfo>( );
				foreach (var m in GetDirectories( Factory, Group, Identify ))
				{
					DirectoryInfo directory = new DirectoryInfo( m );
					if (string.IsNullOrEmpty( Factory ))
					{
						GroupFileContainer fileManagment = GetGroupFromFilePath( ReturnAbsoluteFilePath( directory.Name, string.Empty, string.Empty ) );
						GroupFileInfo groupFileInfo = fileManagment.GetGroupFileInfo( );
						groupFileInfo.PathName = directory.Name;
						folders.Add( groupFileInfo );
					}
					else if (string.IsNullOrEmpty( Group ))
					{
						GroupFileContainer fileManagment = GetGroupFromFilePath( ReturnAbsoluteFilePath( Factory, directory.Name, string.Empty ) );
						GroupFileInfo groupFileInfo = fileManagment.GetGroupFileInfo( );
						groupFileInfo.PathName = directory.Name;
						folders.Add( groupFileInfo );
					}
					else
					{
						GroupFileContainer fileManagment = GetGroupFromFilePath( ReturnAbsoluteFilePath( Factory, Group, directory.Name ) );
						GroupFileInfo groupFileInfo = fileManagment.GetGroupFileInfo( );
						groupFileInfo.PathName = directory.Name;
						folders.Add( groupFileInfo );
					}
				}
				if ((await SendStringAndCheckReceiveAsync(
					socket,
					HslProtocol.ProtocolFolderInfos,
					folders.ToJsonString( ) )).IsSuccess)
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
					HslProtocol.ProtocolFolderInfo,
					jArray.ToString( ) )).IsSuccess)
				{
					socket?.Close( );
				}
			}
			else if (customer == HslProtocol.ProtocolFileExists)
			{
				string fullPath = ReturnAbsoluteFilePath( Factory, Group, Identify );
				GroupFileContainer fileManagment = GetGroupFromFilePath( fullPath );

				bool isExists = fileManagment.FileExists( fileName );
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
				// close not supported client
				socket?.Close( );
			}
		}
#endif
		#endregion

		#region Object Override

		/// <inheritdoc/>
		public override string ToString( ) => $"UltimateFileServer[{Port}]";

#endregion
	}
}
