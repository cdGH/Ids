using HslCommunication.BasicFramework;
using HslCommunication.Core.Net;
using HslCommunication.Profinet.Melsec.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.IO;
using System.IO.Ports;
#if !NET20 && !NET35
using System.Threading.Tasks;
#endif

namespace HslCommunication.Profinet.Melsec
{
	/// <summary>
	/// <b>[商业授权]</b> 基于MC协议的A3C格式虚拟服务器，可以模拟A3C格式的PLC，支持格式1，2，3，4，具体可以使用<see cref="Format"/>来设置，
	/// 支持设置是否校验，同时支持网口或是串口的访问<br />
	/// <b>[Authorization]</b> A3C format virtual server based on MC protocol can simulate A3C format PLC, support format 1, 2, 3, 4, 
	/// specifically you can use <see cref="Format"/> to set, support whether to verify the setting, 
	/// and also support network Port or serial port access
	/// </summary>
	/// <remarks>
	/// 可访问的地址支持M,X,Y,B,D,W,R,ZR地址，其中 M,X,Y,B 支持位访问
	/// </remarks>
	public class MelsecA3CServer : MelsecMcServer
	{
		#region Constructor

		/// <summary>
		/// 实例化一个虚拟的A3C服务器
		/// </summary>
		public MelsecA3CServer( ) : base( false )
		{

		}

		#endregion

		#region Public Properties

		/// <inheritdoc cref="IReadWriteA3C.Station"/>
		public byte Station { get; set; }

		/// <inheritdoc cref="IReadWriteA3C.SumCheck"/>
		public bool SumCheck { get; set; } = true;

		/// <inheritdoc cref="IReadWriteA3C.Format"/>
		public int Format { get; set; } = 1;

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
					OperateResult<byte[]> read1;

					if (!Authorization.asdniasnfaksndiqwhawfskhfaiw( )) { RemoveClient( session ); return; };

#if NET20 || NET35
					read1 = ReceiveByMessage( session.WorkSocket, 5000, null );
#else
					read1 = await ReceiveByMessageAsync( session.WorkSocket, 5000, null );
#endif
					if (!read1.IsSuccess) { RemoveClient( session ); return; };


					OperateResult<byte[]> extra = ExtraMcCore( read1.Content );
					if (!extra.IsSuccess)
					{
						LogNet?.WriteDebug( ToString( ), $"[{session.IpEndPoint}] Tcp {extra.Message} : {SoftBasic.GetAsciiStringRender( read1.Content )}" );
					}
					else
					{
						back = ReadFromMcAsciiCore( extra.Content );

						LogNet?.WriteDebug( ToString( ), $"[{session.IpEndPoint}] Tcp {StringResources.Language.Receive}：{SoftBasic.GetAsciiStringRender( read1.Content )}" );

						if (back != null)
						{
							session.WorkSocket.Send( back );
							LogNet?.WriteDebug( ToString( ), $"[{session.IpEndPoint}] Tcp {StringResources.Language.Send}：{ SoftBasic.GetAsciiStringRender( back )}" );
						}
						else
						{
							RemoveClient( session );
							return;
						}

					}
					session.HeartTime = DateTime.Now;
					RaiseDataReceived( session, read1.Content );
					session.WorkSocket.BeginReceive( new byte[0], 0, 0, SocketFlags.None, new AsyncCallback( SocketAsyncCallBack ), session );
				}
				catch (Exception ex)
				{
					RemoveClient( session, $"SocketAsyncCallBack -> {ex.Message}" );
				}
			}
		}

		#endregion

		private void SetSumCheck( byte[] command, int startLength, int endLength )
		{
			int sum = 0;
			for (int i = startLength; i < command.Length - endLength; i++)
			{
				sum += command[i];
			}
			byte[] check = SoftBasic.BuildAsciiBytesFrom( (byte)sum );
			command[command.Length - endLength] = check[0];
			command[command.Length - endLength + 1] = check[1];
		}

		private bool CalculatSumCheck( byte[] command, int startLength, int endLength )
		{
			int sum = 0;
			for (int i = startLength; i < command.Length - endLength; i++)
			{
				sum += command[i];
			}
			byte[] check = SoftBasic.BuildAsciiBytesFrom( (byte)sum );
			if (command[command.Length - endLength] != check[0] ||
				command[command.Length - endLength + 1] != check[1])
				return false;
			return true;
		}

		private OperateResult<byte[]> ExtraMcCore( byte[] command )
		{
			byte station = byte.Parse( Encoding.ASCII.GetString( command, Format == 2 ? 5 : 3, 2 ) );
			if (this.Station != station) return new OperateResult<byte[]>( $"Station Not Match, need: {this.Station}  but: {station}" );
			// 格式信息及和校验检测
			if (Format == 1)
			{
				if (command[0] != 0x05) return new OperateResult<byte[]>( "First Byte Must Start with 0x05" );
				if (SumCheck)
				{
					if (!CalculatSumCheck( command, 1, 2 )) return new OperateResult<byte[]>( "Sum Check Failed!" );
					return OperateResult.CreateSuccessResult( command.SelectMiddle( 11, command.Length - 13 ) );
				}
				else
				{
					return OperateResult.CreateSuccessResult( command.SelectMiddle( 11, command.Length - 11 ) );
				}
			}
			else if (Format == 2)
			{
				if (command[0] != 0x05) return new OperateResult<byte[]>( "First Byte Must Start with 0x05" );
				if (SumCheck)
				{
					if (!CalculatSumCheck( command, 1, 2 )) return new OperateResult<byte[]>( "Sum Check Failed!" );
					return OperateResult.CreateSuccessResult( command.SelectMiddle( 13, command.Length - 15 ) );
				}
				else
				{
					return OperateResult.CreateSuccessResult( command.SelectMiddle( 13, command.Length - 13 ) );
				}
			}
			else if (Format == 3)
			{
				if (command[0] != 0x02) return new OperateResult<byte[]>( "First Byte Must Start with 0x02" );
				if (SumCheck)
				{
					if (command[command.Length - 3] != 0x03) return new OperateResult<byte[]>( "The last three Byte Must be 0x03" );
					if (!CalculatSumCheck( command, 1, 2 )) return new OperateResult<byte[]>( "Sum Check Failed!" );
					return OperateResult.CreateSuccessResult( command.SelectMiddle( 11, command.Length - 14 ) );
				}
				else
				{
					if (command[command.Length - 1] != 0x03) return new OperateResult<byte[]>( "The last Byte Must be 0x03" );
					return OperateResult.CreateSuccessResult( command.SelectMiddle( 11, command.Length - 12 ) );
				}
			}
			else if (Format == 4)
			{
				if (command[0] != 0x05) return new OperateResult<byte[]>( "First Byte Must Start with 0x05" );
				if (command[command.Length - 1] != 0x0A) return new OperateResult<byte[]>( "The last Byte must be 0x0D,0x0A" );
				if (command[command.Length - 2] != 0x0D) return new OperateResult<byte[]>( "The last Byte must be 0x0D,0x0A" );
				if (SumCheck)
				{
					if (!CalculatSumCheck( command, 1, 4 )) return new OperateResult<byte[]>( "Sum Check Failed!" );
					return OperateResult.CreateSuccessResult( command.SelectMiddle( 11, command.Length - 15 ) );
				}
				else
				{
					return OperateResult.CreateSuccessResult( command.SelectMiddle( 11, command.Length - 13 ) );
				}
			}
			else
				return new OperateResult<byte[]>( "Not Support Format:" + Format );
		}

		/// <inheritdoc/>
		protected override byte[] PackCommand( ushort status, byte[] data )
		{
			if (data == null) data = new byte[0];
			if(data.Length == 0)
			{
				// 写入操作
				if(Format == 1)
				{
					if (status == 0)
					{
						byte[] buffer = Encoding.ASCII.GetBytes( "\u0006F90000FF00" );
						SoftBasic.BuildAsciiBytesFrom( Station ).CopyTo( buffer, 3 );
						return buffer;
					}
					else
					{
						byte[] buffer = Encoding.ASCII.GetBytes( "\u0015F90000FF000000" );
						SoftBasic.BuildAsciiBytesFrom( Station ).CopyTo( buffer, 3 );
						SoftBasic.BuildAsciiBytesFrom( status ).CopyTo( buffer, buffer.Length - 4 );
						return buffer;
					}
				}
				else if(Format == 2)
				{
					if (status == 0)
					{
						byte[] buffer = Encoding.ASCII.GetBytes( "\u000600F90000FF00" );
						SoftBasic.BuildAsciiBytesFrom( Station ).CopyTo( buffer, 5 );
						return buffer;
					}
					else
					{
						byte[] buffer = Encoding.ASCII.GetBytes( "\u001500F90000FF000000" );
						SoftBasic.BuildAsciiBytesFrom( Station ).CopyTo( buffer, 5 );
						SoftBasic.BuildAsciiBytesFrom( status ).CopyTo( buffer, buffer.Length - 4 );
						return buffer;
					}
				}
				else if(Format == 3)
				{
					if (status == 0)
					{
						byte[] buffer = Encoding.ASCII.GetBytes( "\u0002F90000FF00QACK\u0003" );
						SoftBasic.BuildAsciiBytesFrom( Station ).CopyTo( buffer, 3 );
						return buffer;
					}
					else
					{
						byte[] buffer = Encoding.ASCII.GetBytes( "\u0002F90000FF00QNAK0000\u0003" );
						SoftBasic.BuildAsciiBytesFrom( Station ).CopyTo( buffer, 3 );
						SoftBasic.BuildAsciiBytesFrom( status ).CopyTo( buffer, buffer.Length - 5 );
						return buffer;
					}
				}
				else if(Format == 4)
				{
					if (status == 0)
					{
						byte[] buffer = Encoding.ASCII.GetBytes( "\u0002F90000FF00" );
						SoftBasic.BuildAsciiBytesFrom( Station ).CopyTo( buffer, 3 );
						return buffer;
					}
					else
					{
						byte[] buffer = Encoding.ASCII.GetBytes( "\u0015F90000FF000000\u000D\u000A" );
						SoftBasic.BuildAsciiBytesFrom( Station ).CopyTo( buffer, 3 );
						SoftBasic.BuildAsciiBytesFrom( status ).CopyTo( buffer, buffer.Length - 6 );
						return buffer;
					}
				}
				return null;
			}
			else
			{
				// 读取操作
				if(Format == 1)
				{
					if (status != 0)
					{
						byte[] buffer = Encoding.ASCII.GetBytes( "\u0015F90000FF000000" );
						SoftBasic.BuildAsciiBytesFrom( Station ).CopyTo( buffer, 3 );
						SoftBasic.BuildAsciiBytesFrom( status ).CopyTo( buffer, buffer.Length - 4 );
						return buffer;
					}
					else
					{
						byte[] buffer = new byte[(SumCheck ? 14 : 12) + data.Length];
						Encoding.ASCII.GetBytes( "\u0002F90000FF00" ).CopyTo( buffer, 0 );
						SoftBasic.BuildAsciiBytesFrom( Station ).CopyTo( buffer, 3 );
						data.CopyTo( buffer, 11 );
						buffer[buffer.Length - (SumCheck ? 3 : 1)] = 0x03;
						if (SumCheck) SetSumCheck( buffer, 1, 2 );
						return buffer;
					}
				}
				else if(Format == 2)
				{
					if (status != 0)
					{
						byte[] buffer = Encoding.ASCII.GetBytes( "\u001500F90000FF000000" );
						SoftBasic.BuildAsciiBytesFrom( Station ).CopyTo( buffer, 5 );
						SoftBasic.BuildAsciiBytesFrom( status ).CopyTo( buffer, buffer.Length - 4 );
						return buffer;
					}
					else
					{
						byte[] buffer = new byte[(SumCheck ? 16 : 14) + data.Length];
						Encoding.ASCII.GetBytes( "\u000200F90000FF00" ).CopyTo( buffer, 0 );
						SoftBasic.BuildAsciiBytesFrom( Station ).CopyTo( buffer, 5 );
						data.CopyTo( buffer, 13 );
						buffer[buffer.Length - (SumCheck ? 3 : 1)] = 0x03;
						if (SumCheck) SetSumCheck( buffer, 1, 2 );
						return buffer;
					}
				}
				else if (Format == 3)
				{
					if (status != 0)
					{
						byte[] buffer = Encoding.ASCII.GetBytes( "\u0002F90000FF00QNAK0000\u0003" );
						SoftBasic.BuildAsciiBytesFrom( Station ).CopyTo( buffer, 3 );
						SoftBasic.BuildAsciiBytesFrom( status ).CopyTo( buffer, buffer.Length - 5 );
						return buffer;
					}
					else
					{
						byte[] buffer = new byte[(SumCheck ? 18 : 16) + data.Length];
						Encoding.ASCII.GetBytes( "\u0002F90000FF00QACK" ).CopyTo( buffer, 0 );
						SoftBasic.BuildAsciiBytesFrom( Station ).CopyTo( buffer, 3 );
						buffer[buffer.Length - (SumCheck ? 3 : 1)] = 0x03;
						data.CopyTo( buffer, 15 );
						if (SumCheck) SetSumCheck( buffer, 1, 2 );
						return buffer;
					}
				}
				else if (Format == 4)
				{
					if (status != 0)
					{
						byte[] buffer = Encoding.ASCII.GetBytes( "\u0015F90000FF000000\u000D\u000A" );
						SoftBasic.BuildAsciiBytesFrom( Station ).CopyTo( buffer, 3 );
						SoftBasic.BuildAsciiBytesFrom( status ).CopyTo( buffer, buffer.Length - 6 );
						return buffer;
					}
					else
					{
						byte[] buffer = new byte[(SumCheck ? 16 : 14) + data.Length];
						Encoding.ASCII.GetBytes( "\u0002F90000FF00" ).CopyTo( buffer, 0 );
						SoftBasic.BuildAsciiBytesFrom( Station ).CopyTo( buffer, 3 );
						buffer[buffer.Length - (SumCheck ? 5 : 3)] = 0x03;
						data.CopyTo( buffer, 11 );
						if(SumCheck) SetSumCheck( buffer, 1, 4 );
						buffer[buffer.Length - 2] = 0x0D;
						buffer[buffer.Length - 1] = 0x0A;
						return buffer;
					}
				}
				else
				{
					return null;
				}
			}
		}

		#region Serial Support

		/// <inheritdoc/>
		protected override OperateResult<byte[]> DealWithSerialReceivedData( byte[] data )
		{
			if (data.Length < 3) return new OperateResult<byte[]>( $"[{GetSerialPort( ).PortName}] Uknown Data：{data.ToHexString( ' ' )}" );

			OperateResult<byte[]> extra = ExtraMcCore( data );
			if (!extra.IsSuccess)
			{
				return new OperateResult<byte[]>( $"[{GetSerialPort( ).PortName}] {extra.Message} : {SoftBasic.GetAsciiStringRender( data )}" );
			}
			else
			{
				LogNet?.WriteDebug( ToString( ), $"[{GetSerialPort( ).PortName}] {StringResources.Language.Receive}：{SoftBasic.GetAsciiStringRender( data )}" );

				byte[] back = ReadFromMcAsciiCore( extra.Content );

				LogNet?.WriteDebug( ToString( ), $"[{GetSerialPort( ).PortName}] {StringResources.Language.Send}：{ SoftBasic.GetAsciiStringRender( back )}" );

				return OperateResult.CreateSuccessResult( back );
			}
		}

		#endregion

		#region Object Override

		/// <inheritdoc/>
		public override string ToString( ) => $"MelsecA3CServer[{Port}]";

		#endregion
	}
}
