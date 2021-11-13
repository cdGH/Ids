using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using HslCommunication.Core;
using HslCommunication.BasicFramework;
using HslCommunication.Reflection;
#if !NET35 && !NET20
using System.Threading.Tasks;
#endif
using HslCommunication.Core.Net;

namespace HslCommunication.Profinet.Geniitek
{
	/// <summary>
	/// Geniitek-VB31 型号的智能无线振动传感器，来自苏州捷杰传感器技术有限公司
	/// </summary>
	public class VibrationSensorClient : NetworkXBase
	{
		#region Constructor

		/// <summary>
		/// 使用指定的ip，端口来实例化一个默认的对象
		/// </summary>
		/// <param name="ipAddress">Ip地址信息</param>
		/// <param name="port">端口号信息</param>
		public VibrationSensorClient( string ipAddress = "192.168.1.1", int port = 3001 )
		{
			this.ipAddress = HslHelper.GetIpAddressFromInput( ipAddress );
			this.port = port;
			this.byteTransform = new ReverseBytesTransform( );
		}

		#endregion

		#region Connect DisConnect

		/// <summary>
		/// 连接服务器，实例化客户端之后，至少要调用成功一次，如果返回失败，那些请过一段时间后重新调用本方法连接。<br />
		/// After connecting to the server, the client must be called at least once after instantiating the client.
		/// If the return fails, please call this method to connect again after a period of time.
		/// </summary>
		/// <returns>连接是否成功</returns>
		public OperateResult ConnectServer( )
		{
			// 开启连接
			CoreSocket?.Close( );
			OperateResult<Socket> connect = CreateSocketAndConnect( this.ipAddress, this.port, connectTimeOut );
			if (!connect.IsSuccess) return connect;

			CoreSocket = connect.Content;

			try
			{
				CoreSocket.BeginReceive( new byte[0], 0, 0, SocketFlags.None, new AsyncCallback( ReceiveAsyncCallback ), CoreSocket );
			}
			catch (Exception ex)
			{
				return new OperateResult( ex.Message );
			}

			// 重置关闭状态
			this.closed = false;
			OnClientConnected?.Invoke( );
			this.timerCheck?.Dispose( );
			this.timerCheck = new Timer( new TimerCallback( TimerCheckServer ), null, 2_000, 5_000 );
			return OperateResult.CreateSuccessResult( );
		}

		/// <summary>
		/// 关闭Mqtt服务器的连接。<br />
		/// Close the connection to the Mqtt server.
		/// </summary>
		public void ConnectClose( )
		{
			if (!closed)
			{
				closed = true;
				Thread.Sleep( 20 );
				CoreSocket?.Close( );
				this.timerCheck?.Dispose( );
			}
		}

		#endregion

		#region Async Connect DisConnect
#if !NET35 && !NET20
		/// <inheritdoc cref="ConnectServer()"/>
		public async Task<OperateResult> ConnectServerAsync( )
		{
			// 开启连接
			CoreSocket?.Close( );
			OperateResult<Socket> connect = await CreateSocketAndConnectAsync( this.ipAddress, this.port, connectTimeOut );
			if (!connect.IsSuccess) return connect;

			CoreSocket = connect.Content;

			try
			{
				CoreSocket.BeginReceive( new byte[0], 0, 0, SocketFlags.None, new AsyncCallback( ReceiveAsyncCallback ), CoreSocket );
			}
			catch (Exception ex)
			{
				return new OperateResult( ex.Message );
			}

			// 重置关闭状态
			this.closed = false;
			OnClientConnected?.Invoke( );
			this.timerCheck?.Dispose( );
			this.timerCheck = new Timer( new TimerCallback( TimerCheckServer ), null, 2000, 5_000 );
			return OperateResult.CreateSuccessResult( );

		}

#endif
		#endregion

		#region Private Method

		private void OnVibrationSensorClientNetworkError( )
		{
			if (closed) return;

			if (Interlocked.CompareExchange( ref isReConnectServer, 1, 0 ) == 0)
			{
				try
				{
					if (OnNetworkError == null)
					{
						// 网络异常，系统准备在10秒后自动重新连接。
						LogNet?.WriteInfo( "The network is abnormal, and the system is ready to automatically reconnect after 10 seconds." );
						while (true)
						{
							// 每隔10秒重连
							for (int i = 0; i < 10; i++)
							{
								Thread.Sleep( 1_000 );
								LogNet?.WriteInfo( $"Wait for {10 - i} second to connect to the server ..." );
							}
							OperateResult connect = ConnectServer( );
							if (connect.IsSuccess)
							{
								// 连接成功后，可以在下方break之前进行订阅，或是数据初始化操作
								LogNet?.WriteInfo( "Successfully connected to the server!" );
								break;
							}
							LogNet?.WriteInfo( "The connection failed. Prepare to reconnect after 10 seconds." );
						}
					}
					else
					{
						OnNetworkError?.Invoke( this, new EventArgs( ) );
					}
					activeTime = DateTime.Now;
					Interlocked.Exchange( ref isReConnectServer, 0 );
				}
				catch
				{
					Interlocked.Exchange( ref isReConnectServer, 0 );
					throw;
				}
			}
		}

#if NET35 || NET20
		private void ReceiveAsyncCallback( IAsyncResult ar )
		{
			if (ar.AsyncState is Socket socket)
			{
				try
				{
					socket.EndReceive( ar );
				}
				catch (ObjectDisposedException)
				{
					socket?.Close( );
					LogNet?.WriteDebug( ToString( ), "Closed" );
					return;
				}
				catch (Exception ex)
				{
					socket?.Close( );
					LogNet?.WriteDebug( ToString( ), "ReceiveCallback Failed:" + ex.Message );
					OnVibrationSensorClientNetworkError( );
					return;
				}

				if (closed)
				{
					LogNet?.WriteDebug( ToString( ), "Closed" );
					return;
				}

				OperateResult<byte[]> read = Receive( socket, 9 );
				if (!read.IsSuccess) { OnVibrationSensorClientNetworkError( ); return; }

				if (read.Content[0] == 0xAA && read.Content[1] == 0x55 && read.Content[2] == 0x7F && read.Content[7] == 0x00)
				{
					// 长数据
					OperateResult<byte[]> read2 = Receive( socket, 3 );
					if (!read.IsSuccess) { OnVibrationSensorClientNetworkError( ); return; }

					int length = read2.Content[1] * 256 + read2.Content[2];
					OperateResult<byte[]> read3 = Receive( socket, length + 4 );
					if (!read.IsSuccess) { OnVibrationSensorClientNetworkError( ); return; }

					if (read.Content[5] == 0x01)
					{
						// 动态调整站号信息
						Address = this.byteTransform.TransUInt16( read.Content, 3 );
						LogNet?.WriteDebug( "Receive: " + SoftBasic.SpliceArray( read.Content, read2.Content, read3.Content ).ToHexString( ' ' ) );

						VibrationSensorPeekValue peekValue = new VibrationSensorPeekValue( );
						peekValue.AcceleratedSpeedX = (read3.Content[1] * 256 + read3.Content[0]) / 100f;
						peekValue.AcceleratedSpeedY = (read3.Content[3] * 256 + read3.Content[2]) / 100f;
						peekValue.AcceleratedSpeedZ = (read3.Content[5] * 256 + read3.Content[4]) / 100f;
						peekValue.SpeedX            = (read3.Content[7] * 256 + read3.Content[6]) / 100f;
						peekValue.SpeedY            = (read3.Content[9] * 256 + read3.Content[8]) / 100f;
						peekValue.SpeedZ            = (read3.Content[11] * 256 + read3.Content[10]) / 100f;
						peekValue.OffsetX           = read3.Content[13] * 256 + read3.Content[12];
						peekValue.OffsetY           = read3.Content[15] * 256 + read3.Content[14];
						peekValue.OffsetZ           = read3.Content[17] * 256 + read3.Content[16];
						peekValue.Temperature       = BitConverter.ToInt16( read3.Content, 18 ) * 0.02f - 273.15f;
						peekValue.Voltage           = read3.Content[21] * 256 + read3.Content[20];
						peekValue.SendingInterval   = read3.Content[23] * 256 + read3.Content[22];
						OnPeekValueReceive?.Invoke( peekValue );
					}
				}
				else
				{
					// 短数据
					VibrationSensorActualValue actualValue = new VibrationSensorActualValue( )
					{
						AcceleratedSpeedX = (read.Content[1] * 256 + read.Content[2]) / 100f,
						AcceleratedSpeedY = (read.Content[3] * 256 + read.Content[4]) / 100f,
						AcceleratedSpeedZ = (read.Content[5] * 256 + read.Content[6]) / 100f,
					};
					OnActualValueReceive?.Invoke( actualValue );
				}

				activeTime = DateTime.Now;

				try
				{
					socket.BeginReceive( new byte[0], 0, 0, SocketFlags.None, new AsyncCallback( ReceiveAsyncCallback ), socket );
				}
				catch (Exception ex)
				{
					socket?.Close( );
					LogNet?.WriteDebug( ToString( ), "BeginReceive Failed:" + ex.Message );
					OnVibrationSensorClientNetworkError( );
				}
			}
		}

#else
		private async void ReceiveAsyncCallback( IAsyncResult ar )
		{
			if (ar.AsyncState is Socket socket)
			{
				try
				{
					socket.EndReceive( ar );
				}
				catch (ObjectDisposedException)
				{
					socket?.Close( );
					LogNet?.WriteDebug( ToString( ), "Closed" );
					return;
				}
				catch (Exception ex)
				{
					socket?.Close( );
					LogNet?.WriteDebug( ToString( ), "ReceiveCallback Failed:" + ex.Message );
					OnVibrationSensorClientNetworkError( );
					return;
				}

				if (closed)
				{
					LogNet?.WriteDebug( ToString( ), "Closed" );
					return;
				}

				OperateResult<byte[]> read = await ReceiveAsync( socket, 9 );
				if (!read.IsSuccess) { OnVibrationSensorClientNetworkError( ); return; }

				if (read.Content[0] == 0xAA && read.Content[1] == 0x55 && read.Content[2] == 0x7F && read.Content[7] == 0x00)
				{
					// 长数据
					OperateResult<byte[]> read2 = await ReceiveAsync( socket, 3 );
					if (!read2.IsSuccess) { OnVibrationSensorClientNetworkError( ); return; }

					int length = read2.Content[1] * 256 + read2.Content[2];
					OperateResult<byte[]> read3 = await ReceiveAsync( socket, length + 4 );
					if (!read3.IsSuccess) { OnVibrationSensorClientNetworkError( ); return; }

					if(read.Content[5] == 0x01)
					{
						// 动态调整站号信息
						Address = this.byteTransform.TransUInt16( read.Content, 3 );
						LogNet?.WriteDebug( "Receive: " + SoftBasic.SpliceArray( read.Content, read2.Content, read3.Content ).ToHexString( ' ' ) );

						VibrationSensorPeekValue peekValue = new VibrationSensorPeekValue( );
						peekValue.AcceleratedSpeedX = BitConverter.ToInt16( read3.Content, 0) / 100f;
						peekValue.AcceleratedSpeedY = BitConverter.ToInt16( read3.Content, 2 ) / 100f;
						peekValue.AcceleratedSpeedZ = BitConverter.ToInt16( read3.Content, 4 ) / 100f;
						peekValue.SpeedX            = BitConverter.ToInt16( read3.Content, 6 ) / 100f;
						peekValue.SpeedY            = BitConverter.ToInt16( read3.Content, 8 ) / 100f;
						peekValue.SpeedZ            = BitConverter.ToInt16( read3.Content, 10 ) / 100f;
						peekValue.OffsetX           = BitConverter.ToInt16( read3.Content, 12 );
						peekValue.OffsetY           = BitConverter.ToInt16( read3.Content, 14 );
						peekValue.OffsetZ           = BitConverter.ToInt16( read3.Content, 16 );
						peekValue.Temperature       = BitConverter.ToInt16( read3.Content, 18 ) * 0.02f - 273.15f;
						peekValue.Voltage           = BitConverter.ToInt16( read3.Content, 20 ) / 100f;
						peekValue.SendingInterval   = BitConverter.ToInt32( read3.Content, 22 );
						OnPeekValueReceive?.Invoke( peekValue );
					}
				}
				else if(read.Content[0] == 0xAA)
				{
					// 短数据
					VibrationSensorActualValue actualValue = new VibrationSensorActualValue( )
					{
						AcceleratedSpeedX = byteTransform.TransInt16( read.Content, 1 ) / 100f,
						AcceleratedSpeedY = byteTransform.TransInt16( read.Content, 3 ) / 100f,
						AcceleratedSpeedZ = byteTransform.TransInt16( read.Content, 5 ) / 100f,
					};
					OnActualValueReceive?.Invoke( actualValue );
				}
				else
				{
					// 数据发生了错位
					OperateResult<byte[]> read2 = await ReceiveAsync( socket, 9 );
					if (!read2.IsSuccess) { OnVibrationSensorClientNetworkError( ); return; }

					byte[] array = SoftBasic.SpliceArray<byte>( read.Content, read2.Content );
					for (int i = 0; i < array.Length; i++)
					{
						if(array[i] == 0xAA)
						{
							if (i < 9)
							{
								if (array[i + 9] == 0xAA)
								{
									await ReceiveAsync( socket, i );
									break;
								}
							}
							else
							{
								await ReceiveAsync( socket, i - 9 );
								break;
							}
						}
					}
				}

				activeTime = DateTime.Now;
				try
				{
					socket.BeginReceive( new byte[0], 0, 0, SocketFlags.None, new AsyncCallback( ReceiveAsyncCallback ), socket );
				}
				catch (Exception ex)
				{
					socket?.Close( );
					LogNet?.WriteDebug( ToString( ), "BeginReceive Failed:" + ex.Message );
					OnVibrationSensorClientNetworkError( );
				}
			}
		}
#endif

		private void TimerCheckServer( object obj )
		{
			if (CoreSocket != null)
			{
				if (!closed)
				{
					if ((DateTime.Now - activeTime).TotalSeconds > checkSeconds)
					{
						if (CheckTimeoutCount == 0) LogNet?.WriteDebug( StringResources.Language.NetHeartCheckTimeout );
						CheckTimeoutCount = 1;
						OnVibrationSensorClientNetworkError( );
					}
					else
					{
						CheckTimeoutCount = 0;
					}
				}
			}
		}

		private OperateResult SendPre( byte[] send )
		{
			LogNet?.WriteDebug( "Send " + send.ToHexString( ' ' ) );
			return Send( CoreSocket, send );
		}

		#endregion

		#region Public Method

		/// <summary>
		/// 设置读取震动传感器的状态数据<br />
		/// Set to read the status data of the shock sensor
		/// </summary>
		/// <returns>是否发送成功</returns>
		[HslMqttApi]
		public OperateResult SetReadStatus( ) => SendPre( BulidLongMessage( address, 0x01, null ) );

		/// <summary>
		/// 设置读取震动传感器的实时加速度<br />
		/// Set the real-time acceleration of the vibration sensor
		/// </summary>
		/// <returns>是否发送成功</returns>
		[HslMqttApi]
		public OperateResult SetReadActual( ) => SendPre( BulidLongMessage( address, 0x02, null ) );

		/// <summary>
		/// 设置当前的震动传感器的数据发送间隔为指定的时间，单位为秒<br />
		/// Set the current vibration sensor data transmission interval to the specified time in seconds
		/// </summary>
		/// <param name="seconds">时间信息，单位为秒</param>
		/// <returns>是否发送成功</returns>
		[HslMqttApi]
		public OperateResult SetReadStatusInterval( int seconds )
		{
			byte[] data = new byte[6];
			data[0] = BitConverter.GetBytes( address )[0];
			data[1] = BitConverter.GetBytes( address )[1];
			BitConverter.GetBytes( seconds ).CopyTo( data, 2 );

			return SendPre( BulidLongMessage( address, 0x10, data ) );
		}

		#endregion

		#region Event Handler

		/// <summary>
		/// 震动传感器峰值数据事件委托<br />
		/// Shock sensor peak data event delegation
		/// </summary>
		/// <param name="peekValue">峰值信息</param>
		public delegate void OnPeekValueReceiveDelegate( VibrationSensorPeekValue peekValue );

		/// <summary>
		/// 接收到震动传感器峰值数据时触发<br />
		/// Triggered when peak data of vibration sensor is received
		///</summary>
		public event OnPeekValueReceiveDelegate OnPeekValueReceive;

		/// <summary>
		/// 震动传感器实时数据事件委托<br />
		/// Vibration sensor real-time data event delegation
		/// </summary>
		/// <param name="actualValue">实际信息</param>
		public delegate void OnActualValueReceiveDelegate( VibrationSensorActualValue actualValue );

		/// <summary>
		/// 接收到震动传感器实时数据时触发<br />
		/// Triggered when real-time data from shock sensor is received
		///</summary>
		public event OnActualValueReceiveDelegate OnActualValueReceive;

		/// <summary>
		/// 连接服务器成功的委托<br />
		/// Connection server successfully delegated
		/// </summary>
		public delegate void OnClientConnectedDelegate( );

		/// <summary>
		/// 当客户端连接成功触发事件，就算是重新连接服务器后，也是会触发的<br />
		/// The event is triggered when the client is connected successfully, even after reconnecting to the server.
		/// </summary>
		public event OnClientConnectedDelegate OnClientConnected;

		/// <summary>
		/// 当网络发生异常的时候触发的事件，用户应该在事件里进行重连服务器
		/// </summary>
		public event EventHandler OnNetworkError;

		#endregion

		#region Public Properties

		/// <summary>
		/// 获取或设置当前客户端的连接超时时间，默认10,000毫秒，单位ms<br />
		/// Gets or sets the connection timeout of the current client. The default is 10,000 milliseconds. The unit is ms.
		/// </summary>
		public int ConnectTimeOut { get => connectTimeOut; set => connectTimeOut = value; }
		
		/// <summary>
		/// 获取或设置当前的客户端假死超时检查时间，单位为秒，默认60秒，60秒内没有接收到传感器的数据，则强制重连。
		/// </summary>
		public int CheckSeconds { get => checkSeconds; set => checkSeconds = value; }

		/// <summary>
		/// 当前设备的地址信息
		/// </summary>
		public ushort Address
		{
			get => address;
			set => address = value;
		}

		#endregion

		#region Private Member

		private int isReConnectServer = 0;                                    // 是否重连服务器中
		private bool closed = false;                                          // 客户端是否关闭
		private string ipAddress = string.Empty;                              // Ip地址
		private int port = 1883;                                              // 端口号
		private int connectTimeOut = 10000;                                   // 连接的超时时间
		private Timer timerCheck;                                             // 定时器，用来心跳校验的
		private DateTime activeTime = DateTime.Now;                           // 客户端的激活时间，30秒检查一次
		private int checkSeconds = 60;                                        // 检查时间方法，绝对每隔多久检查一次超时
		private int CheckTimeoutCount = 0;                                    // 决定超时时只触发一次日志记录信息
		private ushort address = 1;                                           // 设备通信地址
		private IByteTransform byteTransform;

		#endregion

		#region Object Override

		/// <inheritdoc/>
		public override string ToString( ) => $"VibrationSensorClient[{ipAddress}:{port}]";

		#endregion

		#region Static Helper

		/// <summary>
		/// 根据地址，命令，数据，创建向传感器发送的数据信息
		/// </summary>
		/// <param name="address">设备地址</param>
		/// <param name="cmd">命令</param>
		/// <param name="data">数据信息</param>
		/// <returns>原始的数据内容</returns>
		public static byte[] BulidLongMessage( ushort address, byte cmd, byte[] data )
		{
			if (data == null) data = new byte[0];
			byte[] buffer = new byte[16 + data.Length];
			buffer[ 0] = 0xAA;                                    // 固定头
			buffer[ 1] = 0x55;
			buffer[ 2] = 0x7F;
			buffer[ 3] = BitConverter.GetBytes( address )[1];     // 设备地址
			buffer[ 4] = BitConverter.GetBytes( address )[0];
			buffer[ 5] = cmd;                                     // 帧命令
			buffer[ 6] = 0x01;                                    // 传感器接收的数据
			buffer[ 7] = 0x00;                                    // 版本号
			buffer[ 8] = 0x01;
			buffer[ 9] = 0x01;                                    // 帧状态
			buffer[10] = BitConverter.GetBytes( data.Length )[1]; // 数据长度
			buffer[11] = BitConverter.GetBytes( data.Length )[0];
			data.CopyTo( buffer, 12 );

			int xor = buffer[3];
			for (int i = 4; i < buffer.Length - 4; i++)
			{
				xor ^= buffer[i];
			}
			buffer[buffer.Length - 4] = (byte)xor;
			buffer[buffer.Length - 3] = 0x7F;
			buffer[buffer.Length - 2] = 0xAA;
			buffer[buffer.Length - 1] = 0xED;
			return buffer;
		}

		/// <summary>
		/// 检查当前的数据是否XOR校验成功
		/// </summary>
		/// <param name="data">数据信息</param>
		/// <returns>校验结果</returns>
		public static bool CheckXor( byte[] data )
		{
			int xor = data[3];
			for (int i = 4; i < data.Length - 4; i++)
			{
				xor ^= data[i];
			}
			return BitConverter.GetBytes( xor )[0] == data[data.Length - 4];
		}

		#endregion
	}
}
