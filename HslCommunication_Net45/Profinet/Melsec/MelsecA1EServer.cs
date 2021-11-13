using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using HslCommunication.BasicFramework;
using HslCommunication.Core;
using HslCommunication.Core.Net;
using HslCommunication.Core.IMessage;
using HslCommunication.Core.Address;
using HslCommunication.Reflection;

namespace HslCommunication.Profinet.Melsec
{
	/// <summary>
	/// <b>[商业授权]</b> 三菱MC-A1E协议的虚拟服务器，支持M,X,Y,D,W的数据池读写操作，支持二进制及ASCII格式进行读写操作，需要在实例化的时候指定。<br />
	/// <b>[Authorization]</b> The Mitsubishi MC-A1E protocol virtual server supports M, X, Y, D, W data pool read and write operations, 
	/// and supports binary and ASCII format read and write operations, which need to be specified during instantiation.
	/// </summary>
	/// <remarks>
	/// 本三菱的虚拟PLC仅限商业授权用户使用，感谢支持。
	/// 如果你没有可以测试的三菱PLC，想要测试自己开发的上位机软件，或是想要在本机实现虚拟PLC，然后进行IO的输入输出练习，都可以使用本类来实现，先来说明下地址信息
	/// <br />
	/// 地址的输入的格式说明如下：
	/// <list type="table">
	///   <listheader>
	///     <term>地址名称</term>
	///     <term>地址代号</term>
	///     <term>示例</term>
	///     <term>地址进制</term>
	///     <term>字操作</term>
	///     <term>位操作</term>
	///     <term>备注</term>
	///   </listheader>
	///   <item>
	///     <term>内部继电器</term>
	///     <term>M</term>
	///     <term>M100,M200</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>输入继电器</term>
	///     <term>X</term>
	///     <term>X100,X1A0</term>
	///     <term>16</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>输出继电器</term>
	///     <term>Y</term>
	///     <term>Y100,Y1A0</term>
	///     <term>16</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>数据寄存器</term>
	///     <term>D</term>
	///     <term>D1000,D2000</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>×</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>链接寄存器</term>
	///     <term>W</term>
	///     <term>W100,W1A0</term>
	///     <term>16</term>
	///     <term>√</term>
	///     <term>×</term>
	///     <term></term>
	///   </item>
	/// </list>
	/// </remarks>
	public class MelsecA1EServer : MelsecMcServer
	{
		#region Constructor

		/// <summary>
		/// 实例化一个默认参数的mc协议的服务器<br />
		/// Instantiate a mc protocol server with default parameters
		/// </summary>
		/// <param name="isBinary">是否是二进制，默认是二进制，否则是ASCII格式</param>
		public MelsecA1EServer( bool isBinary = true ) : base( isBinary )
		{

		}

		#endregion

		#region NetServer Override

		/// <inheritdoc/>
		protected override void ThreadPoolLoginAfterClientCheck( Socket socket, System.Net.IPEndPoint endPoint )
		{
			// 开始接收数据信息
			AppSession appSession = new AppSession( );
			appSession.IpEndPoint = endPoint;
			appSession.WorkSocket = socket;
			try
			{
				socket.BeginReceive( new byte[0], 0, 0, SocketFlags.None, new AsyncCallback( SocketAsyncCallBack ), appSession );
				AddClient( appSession );
			}
			catch
			{
				socket.Close( );
				LogNet?.WriteDebug( ToString( ), string.Format( StringResources.Language.ClientOfflineInfo, endPoint ) );
			}
		}
#if NET20 || NET35
		private void SocketAsyncCallBack( IAsyncResult ar )
#else
		private async void SocketAsyncCallBack( IAsyncResult ar )
#endif
		{
			if (ar.AsyncState is AppSession session)
			{
				try
				{
					int receiveCount = session.WorkSocket.EndReceive( ar );
					byte[] back = null;

					if (!Authorization.asdniasnfaksndiqwhawfskhfaiw( )) { RemoveClient( session ); return; };
#if NET20 || NET35
					OperateResult<byte[]> read1 = ReceiveByMessage( session.WorkSocket, 5000, null );
#else
					OperateResult<byte[]> read1 = await ReceiveByMessageAsync( session.WorkSocket, 5000, null );
#endif

					if (!read1.IsSuccess) { RemoveClient( session ); return; };
					if (IsBinary)
						back = ReadFromMcCore( read1.Content );
					else
						back = ReadFromMcAsciiCore( read1.Content );

					LogNet?.WriteDebug( ToString( ), $"[{session.IpEndPoint}] Tcp {StringResources.Language.Receive}：{(this.IsBinary ? read1.Content.ToHexString( ' ' ) : SoftBasic.GetAsciiStringRender( read1.Content ))}" );

					if (back != null)
					{
						session.WorkSocket.Send( back );
						LogNet?.WriteDebug( ToString( ), $"[{session.IpEndPoint}] Tcp {StringResources.Language.Send}：{(this.IsBinary ? back.ToHexString( ' ' ) : SoftBasic.GetAsciiStringRender( back ))}" );
					}
					else
					{
						RemoveClient( session );
						return;
					}

					session.HeartTime = DateTime.Now;
					RaiseDataReceived( session, read1.Content );
					session.WorkSocket.BeginReceive( new byte[0], 0, 0, SocketFlags.None, new AsyncCallback( SocketAsyncCallBack ), session );
				}
				catch( Exception ex )
				{
					RemoveClient( session, $"SocketAsyncCallBack -> {ex.Message}" );
				}
			}
		}

		private byte[] PackResponseCommand( byte[] mcCore, byte err, byte code, byte[] data )
		{
			byte[] head = new byte[] { (byte)(mcCore[0] + 0x80), err };
			if(err != 0)
			{
				if (err == 0x5B) return SoftBasic.SpliceArray( head, new byte[] { code } );
				return head;
			}
			if (data == null) return head;
			return SoftBasic.SpliceArray( head, data );
		}

		private byte[] PackResponseCommand( byte[] mcCore, byte err, byte code, bool[] data )
		{
			byte[] head = new byte[] { (byte)(mcCore[0] + 0x80), err };
			if (err != 0)
			{
				if (err == 0x5B) return SoftBasic.SpliceArray( head, new byte[] { code } );
				return head;
			}
			if (data == null) return head;
			return SoftBasic.SpliceArray( head, MelsecHelper.TransBoolArrayToByteData( data ) );
		}

		private string GetAddressFromDataCode( ushort dataCode, int address )
		{
			if      (dataCode == MelsecA1EDataType.M.DataCode) return "M" + address;
			else if (dataCode == MelsecA1EDataType.X.DataCode) return "X" + address.ToString( "X" );
			else if (dataCode == MelsecA1EDataType.Y.DataCode) return "Y" + address.ToString( "X" );
			else if (dataCode == MelsecA1EDataType.S.DataCode) return "S" + address;
			else if (dataCode == MelsecA1EDataType.F.DataCode) return "F" + address;
			else if (dataCode == MelsecA1EDataType.B.DataCode) return "B" + address.ToString( "X" );
			else if (dataCode == MelsecA1EDataType.D.DataCode) return "D" + address;
			else if (dataCode == MelsecA1EDataType.R.DataCode) return "R" + address;
			else if (dataCode == MelsecA1EDataType.W.DataCode) return "W" + address.ToString( "X" );
			else return string.Empty;
		}

		/// <inheritdoc/>
		protected override byte[] ReadFromMcCore( byte[] mcCore )
		{
			try
			{
				int addressStart = BitConverter.ToInt32(  mcCore, 4 );
				ushort dataCode  = BitConverter.ToUInt16( mcCore, 8 );
				ushort length    = BitConverter.ToUInt16( mcCore, 10 );
				string address   = GetAddressFromDataCode( dataCode, addressStart );
				if (mcCore[0] == 0x00) // 位读取
				{
					if (dataCode == MelsecA1EDataType.M.DataCode ||
						dataCode == MelsecA1EDataType.X.DataCode ||
						dataCode == MelsecA1EDataType.Y.DataCode ||
						dataCode == MelsecA1EDataType.S.DataCode ||
						dataCode == MelsecA1EDataType.F.DataCode ||
						dataCode == MelsecA1EDataType.B.DataCode)
					{
						OperateResult<bool[]> read = ReadBool( address, length );
						if (!read.IsSuccess) return PackResponseCommand( mcCore, 0x10, 0x00, new bool[0] );

						return PackResponseCommand( mcCore, 0x00, 0x00, read.Content );
					}
				}
				else if (mcCore[0] == 0x01) // 字读取
				{
					if (dataCode == MelsecA1EDataType.M.DataCode ||
						dataCode == MelsecA1EDataType.X.DataCode ||
						dataCode == MelsecA1EDataType.Y.DataCode ||
						dataCode == MelsecA1EDataType.S.DataCode ||
						dataCode == MelsecA1EDataType.F.DataCode ||
						dataCode == MelsecA1EDataType.B.DataCode ||
						dataCode == MelsecA1EDataType.D.DataCode ||
						dataCode == MelsecA1EDataType.R.DataCode ||
						dataCode == MelsecA1EDataType.W.DataCode)
					{
						OperateResult<byte[]> read = Read( address, length );
						if (!read.IsSuccess) return PackResponseCommand( mcCore, 0x10, 0x00, new bool[0] );

						return PackResponseCommand( mcCore, 0x00, 0x00, read.Content );
					}
				}
				else if (mcCore[0] == 0x02) // 位成批写入
				{
					bool[] value = MelsecHelper.TransByteArrayToBoolData( mcCore, 12, length );
					if (dataCode == MelsecA1EDataType.M.DataCode ||
						dataCode == MelsecA1EDataType.X.DataCode ||
						dataCode == MelsecA1EDataType.Y.DataCode ||
						dataCode == MelsecA1EDataType.S.DataCode ||
						dataCode == MelsecA1EDataType.F.DataCode ||
						dataCode == MelsecA1EDataType.B.DataCode)
					{
						OperateResult write = Write( address, value );
						if (!write.IsSuccess) return PackResponseCommand( mcCore, 0x10, 0x00, new byte[0] );

						return PackResponseCommand( mcCore, 0x00, 0x00, new byte[0] );
					}
				}
				else if (mcCore[0] == 0x03) // 字成批写入
				{
					byte[] value = mcCore.RemoveBegin( 12 );
					if (dataCode == MelsecA1EDataType.M.DataCode ||
						dataCode == MelsecA1EDataType.X.DataCode ||
						dataCode == MelsecA1EDataType.Y.DataCode ||
						dataCode == MelsecA1EDataType.S.DataCode ||
						dataCode == MelsecA1EDataType.F.DataCode ||
						dataCode == MelsecA1EDataType.B.DataCode ||
						dataCode == MelsecA1EDataType.D.DataCode ||
						dataCode == MelsecA1EDataType.R.DataCode ||
						dataCode == MelsecA1EDataType.W.DataCode)
					{
						OperateResult write = Write( address, value );
						if (!write.IsSuccess) return PackResponseCommand( mcCore, 0x10, 0x00, new byte[0] );

						return PackResponseCommand( mcCore, 0x00, 0x00, new byte[0] );
					}
				}
				return null;
			}
			catch (Exception ex)
			{
				LogNet?.WriteException( ToString( ), ex );
				return null;
			}
		}

		private byte[] PackAsciiResponseCommand( byte[] mcCore, byte[] data )
		{
			byte[] head = new byte[] { (byte)(mcCore[0] + 0x08), mcCore[1], 0x30, 0x30 };
			if (data == null) return head;
			return SoftBasic.SpliceArray( head, MelsecHelper.TransByteArrayToAsciiByteArray( data ) );
		}

		private byte[] PackAsciiResponseCommand( byte[] mcCore, bool[] data )
		{
			byte[] head = new byte[] { (byte)(mcCore[0] + 0x08), mcCore[1], 0x30, 0x30 };
			if (data == null) return head;
			if (data.Length % 2 == 1) data = SoftBasic.ArrayExpandToLength( data, data.Length + 1 );
			return SoftBasic.SpliceArray( head, data.Select( m => m ? (byte)0x31 : (byte)0x30 ).ToArray( ) );
		}

		/// <inheritdoc/>
		protected override byte[] ReadFromMcAsciiCore( byte[] mcCore )
		{
			try
			{
				byte command     = Convert.ToByte(   Encoding.ASCII.GetString( mcCore, 0,  2 ), 16 );
				int addressStart = Convert.ToInt32(  Encoding.ASCII.GetString( mcCore, 12, 8 ), 16 );
				ushort dataCode  = Convert.ToUInt16( Encoding.ASCII.GetString( mcCore, 8,  4 ), 16 );
				ushort length    = Convert.ToUInt16( Encoding.ASCII.GetString( mcCore, 20, 2 ), 16 );
				string address   = GetAddressFromDataCode( dataCode, addressStart );
				if (command == 0x00) // 位读取
				{
					if (dataCode == MelsecA1EDataType.M.DataCode ||
						dataCode == MelsecA1EDataType.X.DataCode ||
						dataCode == MelsecA1EDataType.Y.DataCode ||
						dataCode == MelsecA1EDataType.S.DataCode ||
						dataCode == MelsecA1EDataType.F.DataCode ||
						dataCode == MelsecA1EDataType.B.DataCode)
					{
						return PackAsciiResponseCommand( mcCore, ReadBool( address, length ).Content );
					}
				}
				else if (command == 0x01) // 字读取
				{
					if (dataCode == MelsecA1EDataType.M.DataCode ||
						dataCode == MelsecA1EDataType.X.DataCode ||
						dataCode == MelsecA1EDataType.Y.DataCode ||
						dataCode == MelsecA1EDataType.S.DataCode ||
						dataCode == MelsecA1EDataType.F.DataCode ||
						dataCode == MelsecA1EDataType.B.DataCode ||
						dataCode == MelsecA1EDataType.D.DataCode ||
						dataCode == MelsecA1EDataType.R.DataCode ||
						dataCode == MelsecA1EDataType.W.DataCode)
					{
						return PackAsciiResponseCommand( mcCore, Read( address, length ).Content );
					}
				}
				else if (command == 0x02) // 位成批写入
				{
					bool[] value = mcCore.SelectMiddle( 24, length ).Select( m => m == 0x31 ).ToArray( );
					if (dataCode == MelsecA1EDataType.M.DataCode ||
						dataCode == MelsecA1EDataType.X.DataCode ||
						dataCode == MelsecA1EDataType.Y.DataCode ||
						dataCode == MelsecA1EDataType.S.DataCode ||
						dataCode == MelsecA1EDataType.F.DataCode ||
						dataCode == MelsecA1EDataType.B.DataCode)
					{
						Write( address, value ); 
						return PackAsciiResponseCommand( mcCore, new byte[0] );
					}
				}
				else if (command == 0x03) // 字成批写入
				{
					byte[] value = MelsecHelper.TransAsciiByteArrayToByteArray( mcCore.RemoveBegin( 24 ) );
					if (dataCode == MelsecA1EDataType.M.DataCode ||
						dataCode == MelsecA1EDataType.X.DataCode ||
						dataCode == MelsecA1EDataType.Y.DataCode ||
						dataCode == MelsecA1EDataType.S.DataCode ||
						dataCode == MelsecA1EDataType.F.DataCode ||
						dataCode == MelsecA1EDataType.B.DataCode ||
						dataCode == MelsecA1EDataType.D.DataCode ||
						dataCode == MelsecA1EDataType.R.DataCode ||
						dataCode == MelsecA1EDataType.W.DataCode)
					{
						Write( address, value ); 
						return PackAsciiResponseCommand( mcCore, new byte[0] );
					}
				}
				return null;
			}
			catch (Exception ex)
			{
				LogNet?.WriteException( ToString( ), ex );
				return null;
			}
		}

		#endregion

		#region Object Override

		/// <inheritdoc/>
		public override string ToString( ) => $"MelsecA1EServer[{Port}]";

		#endregion
	}
}
