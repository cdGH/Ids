using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using HslCommunication.Core;
using HslCommunication.Core.Net;
#if !NET35 && !NET20
using System.Threading.Tasks;
#endif

namespace HslCommunication.Enthernet
{
	/// <summary>
	/// 文件传输客户端基类，提供上传，下载，删除的基础服务<br />
	/// File transfer client base class, providing basic services for uploading, downloading, and deleting
	/// </summary>
	public abstract class FileClientBase : NetworkXBase
	{
		#region Private Member

		private IPEndPoint m_ipEndPoint = null;

		#endregion

		#region Public Member

		/// <summary>
		/// 文件管理服务器的ip地址及端口<br />
		/// IP address and port of the file management server
		/// </summary>
		public IPEndPoint ServerIpEndPoint
		{
			get { return m_ipEndPoint; }
			set { m_ipEndPoint = value; }
		}

		/// <summary>
		/// 获取或设置连接的超时时间，默认10秒<br />
		/// Gets or sets the connection timeout time. The default is 10 seconds.
		/// </summary>
		public int ConnectTimeOut { get; set; } = 10000;

		#endregion

		#region Send Factory Group Id

		/// <summary>
		/// 发送三个文件分类信息到服务器端，方便后续开展其他的操作。<br />
		/// Send the three file classification information to the server to facilitate subsequent operations.
		/// </summary>
		/// <param name="socket">套接字对象</param>
		/// <param name="factory">一级分类</param>
		/// <param name="group">二级分类</param>
		/// <param name="id">三级分类</param>
		/// <returns>是否成功的结果对象</returns>
		protected OperateResult SendFactoryGroupId(
			Socket socket,
			string factory,
			string group,
			string id
			)
		{
			OperateResult factoryResult = SendStringAndCheckReceive( socket, 1, factory );
			if (!factoryResult.IsSuccess) return factoryResult;

			OperateResult groupResult = SendStringAndCheckReceive( socket, 2, group );
			if (!groupResult.IsSuccess) return groupResult;

			OperateResult idResult = SendStringAndCheckReceive( socket, 3, id );
			if (!idResult.IsSuccess) return idResult; 

			return OperateResult.CreateSuccessResult( ); ;
		}

		#endregion

		#region Async Send Factory Group Id
#if !NET35 && !NET20
		/// <inheritdoc cref="SendFactoryGroupId(Socket, string, string, string)"/>
		protected async Task<OperateResult> SendFactoryGroupIdAsync(
			Socket socket,
			string factory,
			string group,
			string id
			)
		{
			OperateResult factoryResult = await SendStringAndCheckReceiveAsync( socket, 1, factory );
			if (!factoryResult.IsSuccess) return factoryResult;

			OperateResult groupResult = await SendStringAndCheckReceiveAsync( socket, 2, group );
			if (!groupResult.IsSuccess) return groupResult;

			OperateResult idResult = await SendStringAndCheckReceiveAsync( socket, 3, id );
			if (!idResult.IsSuccess) return idResult;

			return OperateResult.CreateSuccessResult( ); ;
		}
#endif
		#endregion

		#region Delete File

		/// <summary>
		/// 删除服务器上的文件，需要传入文件信息，以及文件绑定的分类信息。<br />
		/// To delete a file on the server, you need to pass in the file information and the classification information of the file binding.
		/// </summary>
		/// <param name="fileName">文件的名称</param>
		/// <param name="factory">一级分类</param>
		/// <param name="group">二级分类</param>
		/// <param name="id">三级分类</param>
		/// <returns>是否成功的结果对象</returns>
		protected OperateResult DeleteFileBase( string fileName, string factory, string group, string id )
		{
			// connect server
			OperateResult<Socket> socketResult = CreateSocketAndConnect( ServerIpEndPoint, ConnectTimeOut );
			if (!socketResult.IsSuccess) return socketResult;

			// 发送操作指令
			OperateResult sendString = SendStringAndCheckReceive( socketResult.Content, HslProtocol.ProtocolFileDelete, fileName );
			if (!sendString.IsSuccess) return sendString;

			// 发送文件名以及三级分类信息
			OperateResult sendFileInfo = SendFactoryGroupId( socketResult.Content, factory, group, id );
			if (!sendFileInfo.IsSuccess) return sendFileInfo;

			// 接收服务器操作结果
			OperateResult<int, string> receiveBack = ReceiveStringContentFromSocket( socketResult.Content );
			if (!receiveBack.IsSuccess) return receiveBack;

			OperateResult result = new OperateResult( );

			if (receiveBack.Content1 == 1) result.IsSuccess = true;
			result.Message = receiveBack.Message;

			socketResult.Content?.Close( );
			return result;
		}

		/// <summary>
		/// 删除服务器上的文件列表，需要传入文件信息，以及文件绑定的分类信息。<br />
		/// To delete a file on the server, you need to pass in the file information and the classification information of the file binding.
		/// </summary>
		/// <param name="fileNames">所有等待删除的文件的名称</param>
		/// <param name="factory">一级分类</param>
		/// <param name="group">二级分类</param>
		/// <param name="id">三级分类</param>
		/// <returns>是否成功的结果对象</returns>
		protected OperateResult DeleteFileBase( string[] fileNames, string factory, string group, string id )
		{
			// connect server
			OperateResult<Socket> socketResult = CreateSocketAndConnect( ServerIpEndPoint, ConnectTimeOut );
			if (!socketResult.IsSuccess) return socketResult;

			// 发送操作指令
			OperateResult sendString = SendStringAndCheckReceive( socketResult.Content, HslProtocol.ProtocolFilesDelete, fileNames );
			if (!sendString.IsSuccess) return sendString;

			// 发送文件名以及三级分类信息
			OperateResult sendFileInfo = SendFactoryGroupId( socketResult.Content, factory, group, id );
			if (!sendFileInfo.IsSuccess) return sendFileInfo;

			// 接收服务器操作结果
			OperateResult<int, string> receiveBack = ReceiveStringContentFromSocket( socketResult.Content );
			if (!receiveBack.IsSuccess) return receiveBack;

			OperateResult result = new OperateResult( );

			if (receiveBack.Content1 == 1) result.IsSuccess = true;
			result.Message = receiveBack.Message;

			socketResult.Content?.Close( );
			return result;
		}

		/// <summary>
		/// 删除服务器上的指定目录的所有文件，需要传入分类信息。<br />
		/// To delete all files in the specified directory on the server, you need to input classification information
		/// </summary>
		/// <param name="factory">一级分类</param>
		/// <param name="group">二级分类</param>
		/// <param name="id">三级分类</param>
		/// <returns>是否成功的结果对象</returns>
		protected OperateResult DeleteFolderBase( string factory, string group, string id )
		{
			// connect server
			OperateResult<Socket> socketResult = CreateSocketAndConnect( ServerIpEndPoint, ConnectTimeOut );
			if (!socketResult.IsSuccess) return socketResult;

			// 发送操作指令
			OperateResult sendString = SendStringAndCheckReceive( socketResult.Content, HslProtocol.ProtocolFolderDelete, "" );
			if (!sendString.IsSuccess) return sendString;

			// 发送文件名以及三级分类信息
			OperateResult sendFileInfo = SendFactoryGroupId( socketResult.Content, factory, group, id );
			if (!sendFileInfo.IsSuccess) return sendFileInfo;

			// 接收服务器操作结果
			OperateResult<int, string> receiveBack = ReceiveStringContentFromSocket( socketResult.Content );
			if (!receiveBack.IsSuccess) return receiveBack;

			OperateResult result = new OperateResult( );

			if (receiveBack.Content1 == 1) result.IsSuccess = true;
			result.Message = receiveBack.Message;

			socketResult.Content?.Close( );
			return result;
		}

		/// <summary>
		/// 删除服务器上的指定目录的所有空文件目录，需要传入分类信息。<br />
		/// Delete all the empty file directories in the specified directory on the server, need to input classification information
		/// </summary>
		/// <param name="factory">一级分类</param>
		/// <param name="group">二级分类</param>
		/// <param name="id">三级分类</param>
		/// <returns>是否成功的结果对象</returns>
		protected OperateResult DeleteEmptyFoldersBase( string factory, string group, string id )
		{
			// connect server
			OperateResult<Socket> socketResult = CreateSocketAndConnect( ServerIpEndPoint, ConnectTimeOut );
			if (!socketResult.IsSuccess) return socketResult;

			// 发送操作指令
			OperateResult sendString = SendStringAndCheckReceive( socketResult.Content, HslProtocol.ProtocolEmptyFolderDelete, "" );
			if (!sendString.IsSuccess) return sendString;

			// 发送文件名以及三级分类信息
			OperateResult sendFileInfo = SendFactoryGroupId( socketResult.Content, factory, group, id );
			if (!sendFileInfo.IsSuccess) return sendFileInfo;

			// 接收服务器操作结果
			OperateResult<int, string> receiveBack = ReceiveStringContentFromSocket( socketResult.Content );
			if (!receiveBack.IsSuccess) return receiveBack;

			OperateResult result = new OperateResult( );

			if (receiveBack.Content1 == 1) result.IsSuccess = true;
			result.Message = receiveBack.Message;

			socketResult.Content?.Close( );
			return result;
		}
		#endregion

		#region Async Delete File
#if !NET35 && !NET20
		/// <inheritdoc cref="DeleteFileBase(string, string, string, string)"/>
		protected async Task<OperateResult> DeleteFileBaseAsync( string fileName, string factory, string group, string id )
		{
			// connect server
			OperateResult<Socket> socketResult = await CreateSocketAndConnectAsync( ServerIpEndPoint, ConnectTimeOut );
			if (!socketResult.IsSuccess) return socketResult;

			// 发送操作指令
			OperateResult sendString = await SendStringAndCheckReceiveAsync( socketResult.Content, HslProtocol.ProtocolFileDelete, fileName );
			if (!sendString.IsSuccess) return sendString;

			// 发送文件名以及三级分类信息
			OperateResult sendFileInfo = await SendFactoryGroupIdAsync( socketResult.Content, factory, group, id );
			if (!sendFileInfo.IsSuccess) return sendFileInfo;

			// 接收服务器操作结果
			OperateResult<int, string> receiveBack = await ReceiveStringContentFromSocketAsync( socketResult.Content );
			if (!receiveBack.IsSuccess) return receiveBack;

			OperateResult result = new OperateResult( );

			if (receiveBack.Content1 == 1) result.IsSuccess = true;
			result.Message = receiveBack.Message;

			socketResult.Content?.Close( );
			return result;
		}

		/// <inheritdoc cref="DeleteFileBase(string[], string, string, string)"/>
		protected async Task<OperateResult> DeleteFileBaseAsync( string[] fileNames, string factory, string group, string id )
		{
			// connect server
			OperateResult<Socket> socketResult = await CreateSocketAndConnectAsync( ServerIpEndPoint, ConnectTimeOut );
			if (!socketResult.IsSuccess) return socketResult;

			// 发送操作指令
			OperateResult sendString = await SendStringAndCheckReceiveAsync( socketResult.Content, HslProtocol.ProtocolFilesDelete, fileNames );
			if (!sendString.IsSuccess) return sendString;

			// 发送文件名以及三级分类信息
			OperateResult sendFileInfo = await SendFactoryGroupIdAsync( socketResult.Content, factory, group, id );
			if (!sendFileInfo.IsSuccess) return sendFileInfo;

			// 接收服务器操作结果
			OperateResult<int, string> receiveBack = await ReceiveStringContentFromSocketAsync( socketResult.Content );
			if (!receiveBack.IsSuccess) return receiveBack;

			OperateResult result = new OperateResult( );

			if (receiveBack.Content1 == 1) result.IsSuccess = true;
			result.Message = receiveBack.Message;

			socketResult.Content?.Close( );
			return result;
		}

		/// <inheritdoc cref="DeleteFolderBase(string, string, string)"/>
		protected async Task<OperateResult> DeleteFolderBaseAsync( string factory, string group, string id )
		{
			// connect server
			OperateResult<Socket> socketResult = await CreateSocketAndConnectAsync( ServerIpEndPoint, ConnectTimeOut );
			if (!socketResult.IsSuccess) return socketResult;

			// 发送操作指令
			OperateResult sendString = await SendStringAndCheckReceiveAsync( socketResult.Content, HslProtocol.ProtocolFolderDelete, "" );
			if (!sendString.IsSuccess) return sendString;

			// 发送文件名以及三级分类信息
			OperateResult sendFileInfo = await SendFactoryGroupIdAsync( socketResult.Content, factory, group, id );
			if (!sendFileInfo.IsSuccess) return sendFileInfo;

			// 接收服务器操作结果
			OperateResult<int, string> receiveBack = await ReceiveStringContentFromSocketAsync( socketResult.Content );
			if (!receiveBack.IsSuccess) return receiveBack;

			OperateResult result = new OperateResult( );

			if (receiveBack.Content1 == 1) result.IsSuccess = true;
			result.Message = receiveBack.Message;

			socketResult.Content?.Close( );
			return result;
		}
		/// <inheritdoc cref="DeleteEmptyFoldersBase(string, string, string)"/>
		protected async Task<OperateResult> DeleteEmptyFoldersBaseAsync( string factory, string group, string id )
		{
			// connect server
			OperateResult<Socket> socketResult = await CreateSocketAndConnectAsync( ServerIpEndPoint, ConnectTimeOut );
			if (!socketResult.IsSuccess) return socketResult;

			// 发送操作指令
			OperateResult sendString = await SendStringAndCheckReceiveAsync( socketResult.Content, HslProtocol.ProtocolEmptyFolderDelete, "" );
			if (!sendString.IsSuccess) return sendString;

			// 发送文件名以及三级分类信息
			OperateResult sendFileInfo = await SendFactoryGroupIdAsync( socketResult.Content, factory, group, id );
			if (!sendFileInfo.IsSuccess) return sendFileInfo;

			// 接收服务器操作结果
			OperateResult<int, string> receiveBack = await ReceiveStringContentFromSocketAsync( socketResult.Content );
			if (!receiveBack.IsSuccess) return receiveBack;

			OperateResult result = new OperateResult( );

			if (receiveBack.Content1 == 1) result.IsSuccess = true;
			result.Message = receiveBack.Message;

			socketResult.Content?.Close( );
			return result;
		}
#endif
		#endregion

		#region Download File

		/// <summary>
		/// 下载服务器的文件数据，并且存储到对应的内容里去。<br />
		/// Download the file data of the server and store it in the corresponding content.
		/// </summary>
		/// <param name="factory">一级分类</param>
		/// <param name="group">二级分类</param>
		/// <param name="id">三级分类</param>
		/// <param name="fileName">服务器的文件名称</param>
		/// <param name="processReport">下载的进度报告，第一个数据是已完成总接字节数，第二个数据是总字节数。</param>
		/// <param name="source">数据源信息，决定最终存储到哪里去</param>
		/// <returns>是否成功的结果对象</returns>
		protected OperateResult DownloadFileBase(
			string factory,
			string group,
			string id,
			string fileName,
			Action<long, long> processReport,
			object source
			)
		{
			// connect server
			OperateResult<Socket> socketResult = CreateSocketAndConnect( ServerIpEndPoint, ConnectTimeOut );
			if (!socketResult.IsSuccess) return socketResult;

			// 发送操作指令
			OperateResult sendString = SendStringAndCheckReceive( socketResult.Content, HslProtocol.ProtocolFileDownload, fileName );
			if (!sendString.IsSuccess) return sendString;

			// 发送三级分类
			OperateResult sendClass = SendFactoryGroupId( socketResult.Content, factory, group, id );
			if (!sendClass.IsSuccess) return sendClass;

			// 根据数据源分析
			if (source is string fileSaveName)
			{
				OperateResult result = ReceiveFileFromSocket( socketResult.Content, fileSaveName, processReport );
				if (!result.IsSuccess) return result;
			}
			else if (source is Stream stream)
			{
				OperateResult result = ReceiveFileFromSocket( socketResult.Content, stream, processReport );
				if (!result.IsSuccess) return result;
			}
			else
			{
				socketResult.Content?.Close( );
				LogNet?.WriteError( ToString(), StringResources.Language.NotSupportedDataType );
				return new OperateResult( StringResources.Language.NotSupportedDataType );
			}

			socketResult.Content?.Close( );
			return OperateResult.CreateSuccessResult( );
		}

		#endregion

		#region Async Download File
#if !NET35 && !NET20
		/// <inheritdoc cref="DownloadFileBase(string, string, string, string, Action{long, long}, object)"/>
		protected async Task<OperateResult> DownloadFileBaseAsync(
			string factory,
			string group,
			string id,
			string fileName,
			Action<long, long> processReport,
			object source
			)
		{
			// connect server
			OperateResult<Socket> socketResult = await CreateSocketAndConnectAsync( ServerIpEndPoint, ConnectTimeOut );
			if (!socketResult.IsSuccess) return socketResult;

			// 发送操作指令
			OperateResult sendString = await SendStringAndCheckReceiveAsync( socketResult.Content, HslProtocol.ProtocolFileDownload, fileName );
			if (!sendString.IsSuccess) return sendString;

			// 发送三级分类
			OperateResult sendClass = await SendFactoryGroupIdAsync( socketResult.Content, factory, group, id );
			if (!sendClass.IsSuccess) return sendClass;


			// 根据数据源分析
			if (source is string fileSaveName)
			{
				OperateResult result = await ReceiveFileFromSocketAsync( socketResult.Content, fileSaveName, processReport );
				if (!result.IsSuccess) return result;
			}
			else if (source is Stream stream)
			{
				OperateResult result = await ReceiveFileFromSocketAsync( socketResult.Content, stream, processReport );
				if (!result.IsSuccess) return result;
			}
			else
			{
				socketResult.Content?.Close( );
				LogNet?.WriteError( ToString( ), StringResources.Language.NotSupportedDataType );
				return new OperateResult( StringResources.Language.NotSupportedDataType );
			}

			socketResult.Content?.Close( );
			return OperateResult.CreateSuccessResult( );
		}
#endif
		#endregion

		#region Upload File

		/// <summary>
		/// 上传文件给服务器，需要指定上传的数据内容，上传到服务器的分类信息，支持进度汇报功能。<br />
		/// To upload files to the server, you need to specify the content of the uploaded data, 
		/// the classification information uploaded to the server, and support the progress report function.
		/// </summary>
		/// <param name="source">数据源，可以是文件名，也可以是数据流</param>
		/// <param name="serverName">在服务器保存的文件名，不包含驱动器路径</param>
		/// <param name="factory">一级分类</param>
		/// <param name="group">二级分类</param>
		/// <param name="id">三级分类</param>
		/// <param name="fileTag">文件的描述</param>
		/// <param name="fileUpload">文件的上传人</param>
		/// <param name="processReport">汇报进度，第一个数据是已完成总接字节数，第二个数据是总字节数。</param>
		/// <returns>是否成功的结果对象</returns>
		protected OperateResult UploadFileBase(
			object source,
			string serverName,
			string factory,
			string group,
			string id,
			string fileTag,
			string fileUpload,
			Action<long, long> processReport )
		{
			// 创建套接字并连接服务器
			OperateResult<Socket> socketResult = CreateSocketAndConnect( ServerIpEndPoint, ConnectTimeOut );
			if (!socketResult.IsSuccess) return socketResult;

			// 上传操作暗号的文件名
			OperateResult sendString = SendStringAndCheckReceive( socketResult.Content, HslProtocol.ProtocolFileUpload, serverName );
			if (!sendString.IsSuccess) return sendString;

			// 发送三级分类
			OperateResult sendClass = SendFactoryGroupId( socketResult.Content, factory, group, id );
			if (!sendClass.IsSuccess) return sendClass;

			// 判断数据源格式
			if (source is string fileName)
			{
				OperateResult result = SendFileAndCheckReceive( socketResult.Content, fileName, serverName, fileTag, fileUpload, processReport );
				if (!result.IsSuccess) return result;
			}
			else if (source is Stream stream)
			{
				OperateResult result = SendFileAndCheckReceive( socketResult.Content, stream, serverName, fileTag, fileUpload, processReport );
				if (!result.IsSuccess) return result;
			}
			else
			{
				socketResult.Content?.Close( );
				LogNet?.WriteError( ToString( ), StringResources.Language.DataSourceFormatError );
				return new OperateResult( StringResources.Language.DataSourceFormatError );
			}

			// 确认服务器文件保存状态
			OperateResult<int, string> resultCheck = ReceiveStringContentFromSocket( socketResult.Content );
			if (!resultCheck.IsSuccess) return resultCheck;

			return resultCheck.Content1 == 1 ? OperateResult.CreateSuccessResult( ) : new OperateResult( StringResources.Language.ServerFileCheckFailed );
		}

		#endregion

		#region Async Upload File
#if !NET35 && !NET20
		/// <inheritdoc cref="UploadFileBase(object, string, string, string, string, string, string, Action{long, long})"/>
		protected async Task<OperateResult> UploadFileBaseAsync(
			object source,
			string serverName,
			string factory,
			string group,
			string id,
			string fileTag,
			string fileUpload,
			Action<long, long> processReport )
		{
			// 创建套接字并连接服务器
			OperateResult<Socket> socketResult = await CreateSocketAndConnectAsync( ServerIpEndPoint, ConnectTimeOut );
			if (!socketResult.IsSuccess) return socketResult;

			// 上传操作暗号的文件名
			OperateResult sendString = await SendStringAndCheckReceiveAsync( socketResult.Content, HslProtocol.ProtocolFileUpload, serverName );
			if (!sendString.IsSuccess) return sendString;

			// 发送三级分类
			OperateResult sendClass = await SendFactoryGroupIdAsync( socketResult.Content, factory, group, id );
			if (!sendClass.IsSuccess) return sendClass;

			// 判断数据源格式
			if (source is string fileName)
			{
				OperateResult result = await SendFileAndCheckReceiveAsync( socketResult.Content, fileName, serverName, fileTag, fileUpload, processReport );
				if (!result.IsSuccess) return result;
			}
			else if (source is Stream stream)
			{
				OperateResult result = await SendFileAndCheckReceiveAsync( socketResult.Content, stream, serverName, fileTag, fileUpload, processReport );
				if (!result.IsSuccess) return result;
			}
			else
			{
				socketResult.Content?.Close( );
				LogNet?.WriteError( ToString( ), StringResources.Language.DataSourceFormatError );
				return new OperateResult( StringResources.Language.DataSourceFormatError );
			}

			// 确认服务器文件保存状态
			OperateResult<int, string> resultCheck = await ReceiveStringContentFromSocketAsync( socketResult.Content );
			if (!resultCheck.IsSuccess) return resultCheck;

			if (resultCheck.Content1 == 1) return OperateResult.CreateSuccessResult( );
			else return new OperateResult( StringResources.Language.ServerFileCheckFailed );
		}
#endif
		#endregion

		#region Object Override

		/// <inheritdoc/>
		public override string ToString() => $"FileClientBase[{m_ipEndPoint}]";

		#endregion
	}
}
