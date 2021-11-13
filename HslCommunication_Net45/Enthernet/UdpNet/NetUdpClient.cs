using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using HslCommunication.Core;
using System.Text;
#if !NET35 && !NET20
using System.Threading.Tasks;
#endif

namespace HslCommunication.Enthernet
{
	/// <summary>
	/// UDP客户端的类，负责发送数据到服务器，然后从服务器接收对应的数据信息，该数据经过HSL封装<br />
	/// UDP client class, responsible for sending data to the server, and then receiving the corresponding data information from the server, the data is encapsulated by HSL
	/// </summary>
	public class NetUdpClient : Core.Net.NetworkUdpBase
	{
		/// <summary>
		/// 实例化对象，指定发送的服务器地址和端口号<br />
		/// Instantiated object, specifying the server address and port number to send
		/// </summary>
		/// <param name="ipAddress">服务器的Ip地址</param>
		/// <param name="port">端口号</param>
		public NetUdpClient( string ipAddress,int port )
		{
			IpAddress = ipAddress;
			Port = port;
		}

		/// <summary>
		/// 客户端向服务器进行请求，请求字符串数据，忽略了自定义消息反馈<br />
		/// The client makes a request to the server, requesting string data, and ignoring custom message feedback
		/// </summary>
		/// <param name="customer">用户的指令头</param>
		/// <param name="send">发送数据</param>
		/// <returns>带返回消息的结果对象</returns>
		public OperateResult<string> ReadFromServer( NetHandle customer, string send = null )
		{
			var read = ReadFromServerBase( HslProtocol.CommandBytes( customer, Token, send ) );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<string>( read );

			return OperateResult.CreateSuccessResult( Encoding.Unicode.GetString( read.Content ) );
		}

		/// <summary>
		/// 客户端向服务器进行请求，请求字节数据<br />
		/// The client makes a request to the server, requesting byte data
		/// </summary>
		/// <param name="customer">用户的指令头</param>
		/// <param name="send">发送的字节内容</param>
		/// <returns>带返回消息的结果对象</returns>
		public OperateResult<byte[]> ReadFromServer( NetHandle customer, byte[] send ) => ReadFromServerBase( HslProtocol.CommandBytes( customer, Token, send ) );

		/// <summary>
		/// 客户端向服务器进行请求，请求字符串数据，并返回状态信息<br />
		/// The client makes a request to the server, requests string data, and returns status information
		/// </summary>
		/// <param name="customer">用户的指令头</param>
		/// <param name="send">发送数据</param>
		/// <returns>带返回消息的结果对象</returns>
		public OperateResult<NetHandle, string> ReadCustomerFromServer( NetHandle customer, string send = null )
		{
			var read = ReadCustomerFromServerBase( HslProtocol.CommandBytes( customer, Token, send ) );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<NetHandle, string>( read );

			return OperateResult.CreateSuccessResult( read.Content1, Encoding.Unicode.GetString( read.Content2 ) );
		}

		/// <summary>
		/// 客户端向服务器进行请求，请求字节数据，并返回状态信息<br />
		/// The client makes a request to the server, requests byte data, and returns status information
		/// </summary>
		/// <param name="customer">用户的指令头</param>
		/// <param name="send">发送数据</param>
		/// <returns>带返回消息的结果对象</returns>
		public OperateResult<NetHandle, byte[]> ReadCustomerFromServer( NetHandle customer, byte[] send ) => ReadCustomerFromServerBase( HslProtocol.CommandBytes( customer, Token, send ) );

		/// <summary>
		/// 发送的底层数据，然后返回结果数据<br />
		/// Send the underlying data and then return the result data
		/// </summary>
		/// <param name="send">需要发送的底层数据</param>
		/// <returns>带返回消息的结果对象</returns>
		private OperateResult<byte[]> ReadFromServerBase( byte[] send )
		{
			var read = ReadCustomerFromServerBase( send );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( read );

			return OperateResult.CreateSuccessResult( read.Content2 );
		}

		/// <summary>
		/// 发送的底层数据，然后返回结果数据，该结果是带Handle信息的。<br />
		/// Send the underlying data, and then return the result data, the result is with Handle information.
		/// </summary>
		/// <param name="send">需要发送的底层数据</param>
		/// <returns>带返回消息的结果对象</returns>
		private OperateResult<NetHandle, byte[]> ReadCustomerFromServerBase( byte[] send )
		{
			// 核心数据交互
			var read = ReadFromCoreServer( send );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<NetHandle, byte[]>( read );

			// 提炼数据信息
			return HslProtocol.ExtractHslData( read.Content );
		}

		#region Object Override

		/// <inheritdoc/>
		public override string ToString( ) => $"NetUdpClient[{IpAddress}:{Port}]";

		#endregion

	}
}
