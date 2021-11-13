using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HslCommunication.Core.IMessage;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using HslCommunication.BasicFramework;
using HslCommunication.Enthernet;
using HslCommunication.WebSocket;
#if !NET35 && !NET20
using System.Threading.Tasks;
#endif

namespace HslCommunication.Core.Net
{
	/// <summary>
	/// 包含了主动异步接收的方法实现和文件类异步读写的实现<br />
	/// Contains the implementation of the active asynchronous receiving method and the implementation of asynchronous reading and writing of the file class
	/// </summary>
	public class NetworkXBase : NetworkBase
	{
		#region Constractor

		/// <summary>
		/// 默认的无参构造方法<br />
		/// The default parameterless constructor
		/// </summary>
		public NetworkXBase( ) { }

		#endregion

		#region Special Bytes Send

		/// <summary>
		/// [自校验] 将文件数据发送至套接字，如果结果异常，则结束通讯<br />
		/// [Self-check] Send the file data to the socket. If the result is abnormal, the communication is ended.
		/// </summary>
		/// <param name="socket">网络套接字</param>
		/// <param name="filename">完整的文件路径</param>
		/// <param name="filelength">文件的长度</param>
		/// <param name="report">进度报告器</param>
		/// <returns>是否发送成功</returns>
		protected OperateResult SendFileStreamToSocket( Socket socket, string filename, long filelength, Action<long, long> report = null )
		{
			try
			{
				OperateResult result = new OperateResult( );
				using ( FileStream fs = new FileStream( filename, FileMode.Open, FileAccess.Read ))
				{
					result = SendStreamToSocket( socket, fs, filelength, report, true );
				}
				return result;
			}
			catch (Exception ex)
			{
				socket?.Close( );
				LogNet?.WriteException( ToString( ), ex );
				return new OperateResult( ex.Message );
			}
		}

		/// <summary>
		/// [自校验] 将文件数据发送至套接字，具体发送细节将在继承类中实现，如果结果异常，则结束通讯<br />
		/// [Self-checking] Send the file data to the socket. The specific sending details will be implemented in the inherited class. If the result is abnormal, the communication will end
		/// </summary>
		/// <param name="socket">套接字</param>
		/// <param name="filename">文件名称，文件必须存在</param>
		/// <param name="servername">远程端的文件名称</param>
		/// <param name="filetag">文件的额外标签</param>
		/// <param name="fileupload">文件的上传人</param>
		/// <param name="sendReport">发送进度报告</param>
		/// <returns>是否发送成功</returns>
		protected OperateResult SendFileAndCheckReceive(
			Socket socket,
			string filename,
			string servername,
			string filetag,
			string fileupload,
			Action<long, long> sendReport = null
			)
		{
			// 发送文件名，大小，标签
			FileInfo info = new FileInfo( filename );

			if (!File.Exists( filename ))
			{
				// 如果文件不存在
				OperateResult stringResult = SendStringAndCheckReceive( socket, 0, "" );
				if (!stringResult.IsSuccess) return stringResult;

				socket?.Close( );
				return new OperateResult( StringResources.Language.FileNotExist );
			}

			// 文件存在的情况
			Newtonsoft.Json.Linq.JObject json = new Newtonsoft.Json.Linq.JObject
			{
				{ "FileName", new Newtonsoft.Json.Linq.JValue(servername) },
				{ "FileSize", new Newtonsoft.Json.Linq.JValue(info.Length) },
				{ "FileTag", new Newtonsoft.Json.Linq.JValue(filetag) },
				{ "FileUpload", new Newtonsoft.Json.Linq.JValue(fileupload) }
			};

			// 先发送文件的信息到对方
			OperateResult sendResult = SendStringAndCheckReceive( socket, 1, json.ToString( ) );
			if (!sendResult.IsSuccess) return sendResult;

			// 最后发送
			return SendFileStreamToSocket( socket, filename, info.Length, sendReport );
		}

		/// <summary>
		/// [自校验] 将流数据发送至套接字，具体发送细节将在继承类中实现，如果结果异常，则结束通讯<br />
		/// [Self-checking] Send stream data to the socket. The specific sending details will be implemented in the inherited class. 
		/// If the result is abnormal, the communication will be terminated
		/// </summary>
		/// <param name="socket">套接字</param>
		/// <param name="stream">文件名称，文件必须存在</param>
		/// <param name="servername">远程端的文件名称</param>
		/// <param name="filetag">文件的额外标签</param>
		/// <param name="fileupload">文件的上传人</param>
		/// <param name="sendReport">发送进度报告</param>
		/// <returns>是否成功的结果对象</returns>
		protected OperateResult SendFileAndCheckReceive(
			Socket socket,
			Stream stream,
			string servername,
			string filetag,
			string fileupload,
			Action<long, long> sendReport = null
			)
		{
			// 文件存在的情况
			Newtonsoft.Json.Linq.JObject json = new Newtonsoft.Json.Linq.JObject
			{
				{ "FileName", new Newtonsoft.Json.Linq.JValue(servername) },
				{ "FileSize", new Newtonsoft.Json.Linq.JValue(stream.Length) },
				{ "FileTag", new Newtonsoft.Json.Linq.JValue(filetag) },
				{ "FileUpload", new Newtonsoft.Json.Linq.JValue(fileupload) }
			};
			
			// 发送文件信息
			OperateResult fileResult = SendStringAndCheckReceive( socket, 1, json.ToString( ) );
			if (!fileResult.IsSuccess) return fileResult;
			
			return SendStreamToSocket( socket, stream, stream.Length, sendReport, true );
		}

		/// <summary>
		/// [自校验] 从套接字中接收文件头信息<br />
		/// [Self-checking] Receive file header information from socket
		/// </summary>
		/// <param name="socket">套接字的网络</param>
		/// <returns>包含文件信息的结果对象</returns>
		protected OperateResult<FileBaseInfo> ReceiveFileHeadFromSocket( Socket socket )
		{
			// 先接收文件头信息
			OperateResult<int, string> receiveString = ReceiveStringContentFromSocket( socket );
			if (!receiveString.IsSuccess) return OperateResult.CreateFailedResult<FileBaseInfo>( receiveString );
			
			// 判断文件是否存在
			if (receiveString.Content1 == 0)
			{
				socket?.Close( );
				LogNet?.WriteWarn( ToString( ), StringResources.Language.FileRemoteNotExist );
				return new OperateResult<FileBaseInfo>( StringResources.Language.FileNotExist );
			}

			OperateResult<FileBaseInfo> result = new OperateResult<FileBaseInfo>
			{
				Content = new FileBaseInfo( )
			};
			try
			{
				// 提取信息
				Newtonsoft.Json.Linq.JObject json  = Newtonsoft.Json.Linq.JObject.Parse( receiveString.Content2 );
				result.Content.Name                = SoftBasic.GetValueFromJsonObject( json, "FileName", "" );
				result.Content.Size                = SoftBasic.GetValueFromJsonObject( json, "FileSize", 0L );
				result.Content.Tag                 = SoftBasic.GetValueFromJsonObject( json, "FileTag", "" );
				result.Content.Upload              = SoftBasic.GetValueFromJsonObject( json, "FileUpload", "" );
				result.IsSuccess                   = true;
			}
			catch (Exception ex)
			{
				socket?.Close( );
				result.Message = "Extra File Head Wrong:" + ex.Message;
			}

			return result;
		}

		/// <summary>
		/// [自校验] 从网络中接收一个文件，如果结果异常，则结束通讯<br />
		/// [Self-checking] Receive a file from the network. If the result is abnormal, the communication ends.
		/// </summary>
		/// <param name="socket">网络套接字</param>
		/// <param name="savename">接收文件后保存的文件名</param>
		/// <param name="receiveReport">接收进度报告</param>
		/// <returns>包含文件信息的结果对象</returns>
		protected OperateResult<FileBaseInfo> ReceiveFileFromSocket( Socket socket, string savename, Action<long, long> receiveReport )
		{
			// 先接收文件头信息
			OperateResult<FileBaseInfo> fileResult = ReceiveFileHeadFromSocket( socket );
			if (!fileResult.IsSuccess) return fileResult;

			try
			{
				OperateResult write = null;
				using (FileStream fs = new FileStream( savename, FileMode.Create, FileAccess.Write ))
				{
					write = WriteStreamFromSocket( socket, fs, fileResult.Content.Size, receiveReport, true );
				}

				if (!write.IsSuccess)
				{
					if (File.Exists( savename )) File.Delete( savename );
					return OperateResult.CreateFailedResult<FileBaseInfo>( write );
				}

				return fileResult;
			}
			catch (Exception ex)
			{
				LogNet?.WriteException( ToString( ), ex );
				socket?.Close( );
				return new OperateResult<FileBaseInfo>( )
				{
					Message = ex.Message
				};
			}
		}

		/// <summary>
		/// [自校验] 从网络中接收一个文件，写入数据流，如果结果异常，则结束通讯，参数顺序文件名，文件大小，文件标识，上传人<br />
		/// [Self-checking] Receive a file from the network. If the result is abnormal, the communication ends.
		/// </summary>
		/// <param name="socket">网络套接字</param>
		/// <param name="stream">等待写入的数据流</param>
		/// <param name="receiveReport">接收进度报告</param>
		/// <returns>文件头结果</returns>
		protected OperateResult<FileBaseInfo> ReceiveFileFromSocket( Socket socket, Stream stream, Action<long, long> receiveReport )
		{
			// 先接收文件头信息
			OperateResult<FileBaseInfo> fileResult = ReceiveFileHeadFromSocket( socket );
			if (!fileResult.IsSuccess) return fileResult;

			try
			{
				WriteStreamFromSocket( socket, stream, fileResult.Content.Size, receiveReport, true );
				return fileResult;
			}
			catch (Exception ex)
			{
				LogNet?.WriteException( ToString( ), ex );
				socket?.Close( );
				return new OperateResult<FileBaseInfo>( )
				{
					Message = ex.Message
				};
			}
		}

		#endregion

		#region Async Special Bytes Send
#if !NET35 && !NET20
		/// <inheritdoc cref="SendFileStreamToSocket(Socket, string, long, Action{long, long})"/>
		protected async Task<OperateResult> SendFileStreamToSocketAsync( Socket socket, string filename, long filelength, Action<long, long> report = null )
		{
			try
			{
				OperateResult result = new OperateResult( );
				using (FileStream fs = new FileStream( filename, FileMode.Open, FileAccess.Read ))
				{
					result = await SendStreamToSocketAsync( socket, fs, filelength, report, true );
				}
				return result;
			}
			catch (Exception ex)
			{
				socket?.Close( );
				LogNet?.WriteException( ToString( ), ex );
				return new OperateResult( ex.Message );
			}
		}

		/// <inheritdoc cref="SendFileAndCheckReceive(Socket, string, string, string, string, Action{long, long})"/>
		protected async Task<OperateResult> SendFileAndCheckReceiveAsync(
			Socket socket,
			string filename,
			string servername,
			string filetag,
			string fileupload,
			Action<long, long> sendReport = null
			)
		{
			// 发送文件名，大小，标签
			FileInfo info = new FileInfo( filename );

			if (!File.Exists( filename ))
			{
				// 如果文件不存在
				OperateResult stringResult = await SendStringAndCheckReceiveAsync( socket, 0, "" );
				if (!stringResult.IsSuccess) return stringResult;

				socket?.Close( );
				return new OperateResult( StringResources.Language.FileNotExist );
			}

			// 文件存在的情况
			Newtonsoft.Json.Linq.JObject json = new Newtonsoft.Json.Linq.JObject
			{
				{ "FileName",   new Newtonsoft.Json.Linq.JValue(servername) },
				{ "FileSize",   new Newtonsoft.Json.Linq.JValue(info.Length) },
				{ "FileTag",    new Newtonsoft.Json.Linq.JValue(filetag) },
				{ "FileUpload", new Newtonsoft.Json.Linq.JValue(fileupload) }
			};

			// 先发送文件的信息到对方
			OperateResult sendResult = await SendStringAndCheckReceiveAsync( socket, 1, json.ToString( ) );
			if (!sendResult.IsSuccess) return sendResult;

			// 最后发送
			return await SendFileStreamToSocketAsync( socket, filename, info.Length, sendReport );
		}

		/// <inheritdoc cref="SendFileAndCheckReceive(Socket, Stream, string, string, string, Action{long, long})"/>
		protected async Task<OperateResult> SendFileAndCheckReceiveAsync(
			Socket socket,
			Stream stream,
			string servername,
			string filetag,
			string fileupload,
			Action<long, long> sendReport = null
			)
		{
			// 文件存在的情况
			Newtonsoft.Json.Linq.JObject json = new Newtonsoft.Json.Linq.JObject
			{
				{ "FileName", new Newtonsoft.Json.Linq.JValue(servername) },
				{ "FileSize", new Newtonsoft.Json.Linq.JValue(stream.Length) },
				{ "FileTag", new Newtonsoft.Json.Linq.JValue(filetag) },
				{ "FileUpload", new Newtonsoft.Json.Linq.JValue(fileupload) }
			};

			// 发送文件信息
			OperateResult fileResult = await SendStringAndCheckReceiveAsync( socket, 1, json.ToString( ) );
			if (!fileResult.IsSuccess) return fileResult;

			return await SendStreamToSocketAsync( socket, stream, stream.Length, sendReport, true );
		}

		/// <inheritdoc cref="ReceiveFileHeadFromSocket(Socket)"/>
		protected async Task<OperateResult<FileBaseInfo>> ReceiveFileHeadFromSocketAsync( Socket socket )
		{
			// 先接收文件头信息
			OperateResult<int, string> receiveString = await ReceiveStringContentFromSocketAsync( socket );
			if (!receiveString.IsSuccess) return OperateResult.CreateFailedResult<FileBaseInfo>( receiveString );

			// 判断文件是否存在
			if (receiveString.Content1 == 0)
			{
				socket?.Close( );
				LogNet?.WriteWarn( ToString( ), StringResources.Language.FileRemoteNotExist );
				return new OperateResult<FileBaseInfo>( StringResources.Language.FileNotExist );
			}

			OperateResult<FileBaseInfo> result = new OperateResult<FileBaseInfo>
			{
				Content = new FileBaseInfo( )
			};
			try
			{
				// 提取信息
				Newtonsoft.Json.Linq.JObject json = Newtonsoft.Json.Linq.JObject.Parse( receiveString.Content2 );
				result.Content.Name = SoftBasic.GetValueFromJsonObject( json, "FileName", "" );
				result.Content.Size = SoftBasic.GetValueFromJsonObject( json, "FileSize", 0L );
				result.Content.Tag = SoftBasic.GetValueFromJsonObject( json, "FileTag", "" );
				result.Content.Upload = SoftBasic.GetValueFromJsonObject( json, "FileUpload", "" );
				result.IsSuccess = true;
			}
			catch (Exception ex)
			{
				socket?.Close( );
				result.Message = "Extra File Head Wrong:" + ex.Message;
			}

			return result;
		}

		/// <inheritdoc cref="ReceiveFileFromSocket(Socket, string, Action{long, long})"/>
		protected async Task<OperateResult<FileBaseInfo>> ReceiveFileFromSocketAsync( Socket socket, string savename, Action<long, long> receiveReport )
		{
			// 先接收文件头信息
			OperateResult<FileBaseInfo> fileResult = await ReceiveFileHeadFromSocketAsync( socket );
			if (!fileResult.IsSuccess) return fileResult;

			try
			{
				OperateResult write = null;

				using (FileStream fs = new FileStream( savename, FileMode.Create, FileAccess.Write ))
				{
					write = await WriteStreamFromSocketAsync( socket, fs, fileResult.Content.Size, receiveReport, true );
				}

				if (!write.IsSuccess)
				{
					if (File.Exists( savename )) File.Delete( savename );
					return OperateResult.CreateFailedResult<FileBaseInfo>( write );
				}

				return fileResult;
			}
			catch (Exception ex)
			{
				LogNet?.WriteException( ToString( ), ex );
				socket?.Close( );
				return new OperateResult<FileBaseInfo>( ex.Message );
			}
		}

		/// <inheritdoc cref="ReceiveFileFromSocket(Socket, Stream, Action{long, long})"/>
		protected async Task<OperateResult<FileBaseInfo>> ReceiveFileFromSocketAsync( Socket socket, Stream stream, Action<long, long> receiveReport )
		{
			// 先接收文件头信息
			OperateResult<FileBaseInfo> fileResult = await ReceiveFileHeadFromSocketAsync( socket );
			if (!fileResult.IsSuccess) return fileResult;

			try
			{
				await WriteStreamFromSocketAsync( socket, stream, fileResult.Content.Size, receiveReport, true );
				return fileResult;
			}
			catch (Exception ex)
			{
				LogNet?.WriteException( ToString( ), ex );
				socket?.Close( );
				return new OperateResult<FileBaseInfo>( ex.Message );
			}
		}
#endif
		#endregion

		#region Object Override

		/// <inheritdoc/>
		public override string ToString() => "NetworkXBase";

		#endregion
	}
}
