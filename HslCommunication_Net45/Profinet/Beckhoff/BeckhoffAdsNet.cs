using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HslCommunication.Core;
using HslCommunication.Core.IMessage;
using HslCommunication.Core.Net;
using HslCommunication.BasicFramework;
using System.Net.Sockets;
using System.Net;
using HslCommunication.Reflection;
#if !NET35 && !NET20
using System.Threading.Tasks;
#endif

namespace HslCommunication.Profinet.Beckhoff
{
	/// <summary>
	/// 倍福的ADS协议，支持读取倍福的地址数据，关于端口号的选择，TwinCAT2，端口号801；TwinCAT3，端口号为851<br />
	/// Beckhoff ’s ADS protocol supports reading Beckhoff ’s address data. For the choice of port number, TwinCAT2, port number 801; TwinCAT3, port number 851
	/// </summary>
	/// <remarks>
	/// 支持的地址格式分三种，第一种是绝对的地址表示，比如M100，I100，Q100；第二种是字符串地址，采用s=aaaa;的表示方式；第三种是绝对内存地址采用i=1000000;的表示方式
	/// <br />
	/// <note type="important">
	/// 在实际的测试中，由于打开了VS软件对倍福PLC进行编程操作，会导致HslCommunicationDemo读取PLC发生间歇性读写失败的问题，此时需要关闭Visual Studio软件对倍福的
	/// 连接，之后HslCommunicationDemo就会读写成功，感谢QQ：1813782515 提供的解决思路。
	/// </note>
	/// </remarks>
	public class BeckhoffAdsNet : NetworkDeviceBase
	{
		#region Constructor

		/// <summary>
		/// 实例化一个默认的对象<br />
		/// Instantiate a default object
		/// </summary>
		public BeckhoffAdsNet( )
		{
			this.WordLength        = 2;
			this.targetAMSNetId[4] = 1;
			this.targetAMSNetId[5] = 1;
			this.targetAMSNetId[6] = 0x21;
			this.targetAMSNetId[7] = 0x03;
			this.sourceAMSNetId[4] = 1;
			this.sourceAMSNetId[5] = 1;
			this.ByteTransform     = new RegularByteTransform( );
		}

		/// <summary>
		/// 通过指定的ip地址以及端口号实例化一个默认的对象<br />
		/// Instantiate a default object with the specified IP address and port number
		/// </summary>
		/// <param name="ipAddress">IP地址信息</param>
		/// <param name="port">端口号</param>
		public BeckhoffAdsNet( string ipAddress, int port ) : this( )
		{
			this.IpAddress         = ipAddress;
			this.Port              = port;
		}

		/// <inheritdoc/>
		protected override INetMessage GetNewNetMessage( ) => new AdsNetMessage( );

		#endregion

		#region Ip Port Override

		///<inheritdoc/>
		[HslMqttApi( HttpMethod = "GET", Description = "Get or set the IP address of the remote server. If it is a local test, then it needs to be set to 127.0.0.1" )]
		public override string IpAddress 
		{ 
			get => base.IpAddress;
			set { 
				base.IpAddress = value;
				string[] ip = base.IpAddress.Split( new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries );
				for (int i = 0; i < ip.Length; i++)
				{
					targetAMSNetId[i] = byte.Parse( ip[i] );
				}
			}
		}

		#endregion

		#region Public Properties

		/// <summary>
		/// 是否使用标签的名称缓存功能，默认为 <c>False</c><br />
		/// Whether to use tag name caching. The default is <c>False</c>
		/// </summary>
		public bool UseTagCache
		{
			get => useTagCache;
			set => useTagCache = value;
		}

		#endregion

		#region AdsNetId

		private byte[] targetAMSNetId = new byte[8];
		private byte[] sourceAMSNetId = new byte[8];
		private string senderAMSNetId = string.Empty;

		/// <summary>
		/// 目标的地址，举例 192.168.0.1.1.1；也可以是带端口号 192.168.0.1.1.1:801<br />
		/// The address of the destination, for example 192.168.0.1.1.1; it can also be the port number 192.168.0.1.1.1: 801
		/// </summary>
		/// <remarks>
		/// Port：1: AMS Router; 2: AMS Debugger; 800: Ring 0 TC2 PLC; 801: TC2 PLC Runtime System 1; 811: TC2 PLC Runtime System 2; <br />
		/// 821: TC2 PLC Runtime System 3; 831: TC2 PLC Runtime System 4; 850: Ring 0 TC3 PLC; 851: TC3 PLC Runtime System 1<br />
		/// 852: TC3 PLC Runtime System 2; 853: TC3 PLC Runtime System 3; 854: TC3 PLC Runtime System 4; ...
		/// </remarks>
		/// <param name="amsNetId">AMSNet Id地址</param>
		public void SetTargetAMSNetId( string amsNetId )
		{
			if (!string.IsNullOrEmpty( amsNetId ))
			{
				StrToAMSNetId( amsNetId ).CopyTo( targetAMSNetId, 0 );
			}
		}

		/// <summary>
		/// 设置原目标地址 举例 192.168.0.100.1.1；也可以是带端口号 192.168.0.100.1.1:34567<br />
		/// Set the original destination address Example: 192.168.0.100.1.1; it can also be the port number 192.168.0.100.1.1: 34567
		/// </summary>
		/// <param name="amsNetId">原地址</param>
		public void SetSenderAMSNetId( string amsNetId )
		{
			if (!string.IsNullOrEmpty( amsNetId ))
			{
				StrToAMSNetId( amsNetId ).CopyTo( sourceAMSNetId, 0 );
				senderAMSNetId = amsNetId;
			}
		}

		/// <summary>
		/// 获取当前发送的AMS的网络ID信息
		/// </summary>
		/// <returns></returns>
		public string GetSenderAMSNetId( ) => GetAmsNetIdString( sourceAMSNetId, 0 );

		#endregion

		#region Initialization Override

		/// <inheritdoc/>
		protected override OperateResult InitializationOnConnect( Socket socket )
		{
            if (string.IsNullOrEmpty( senderAMSNetId ))
            {
                IPEndPoint iPEndPoint = (IPEndPoint)socket.LocalEndPoint;
                sourceAMSNetId[6] = BitConverter.GetBytes( iPEndPoint.Port )[0];
                sourceAMSNetId[7] = BitConverter.GetBytes( iPEndPoint.Port )[1];
                iPEndPoint.Address.GetAddressBytes( ).CopyTo( sourceAMSNetId, 0 );
            }
            return base.InitializationOnConnect( socket );

            // 请求 AMS 信息，这个在 TC3 上才支持
            //OperateResult<byte[]> read1 = ReadFromCoreServer( socket, PackAmsTcpHelper( AmsTcpHeaderFlags.GetLocalNetId, new byte[4] ) );
            //if (!read1.IsSuccess) return read1;

            //OperateResult<byte[]> read2 = ReadFromCoreServer( socket, PackAmsTcpHelper( AmsTcpHeaderFlags.PortConnect, new byte[2] ) );
            //if (!read2.IsSuccess) return read2;

            //read2.Content.SelectLast( 8 ).CopyTo( sourceAMSNetId, 0 );
            //return OperateResult.CreateSuccessResult( );
        }
#if !NET35 && !NET20
		/// <inheritdoc/>
		protected override async Task<OperateResult> InitializationOnConnectAsync( Socket socket )
		{
            if (string.IsNullOrEmpty( senderAMSNetId ))
            {
                IPEndPoint iPEndPoint = (IPEndPoint)socket.LocalEndPoint;
                sourceAMSNetId[6] = BitConverter.GetBytes( iPEndPoint.Port )[0];
                sourceAMSNetId[7] = BitConverter.GetBytes( iPEndPoint.Port )[1];
                iPEndPoint.Address.GetAddressBytes( ).CopyTo( sourceAMSNetId, 0 );
            }
            return await base.InitializationOnConnectAsync( socket );

            // 请求 AMS 信息
            //OperateResult<byte[]> read1 = await ReadFromCoreServerAsync( socket, PackAmsTcpHelper( AmsTcpHeaderFlags.GetLocalNetId, new byte[4] ) );
            //if (!read1.IsSuccess) return read1;

            //OperateResult<byte[]> read2 = await ReadFromCoreServerAsync( socket, PackAmsTcpHelper( AmsTcpHeaderFlags.PortConnect, new byte[2] ) );
            //if (!read2.IsSuccess) return read2;

            //read2.Content.SelectLast( 8 ).CopyTo( sourceAMSNetId, 0 );
            //return OperateResult.CreateSuccessResult( );
        }
#endif
		#endregion

		/// <summary>
		/// 根据当前标签的地址获取到内存偏移地址<br />
		/// Get the memory offset address based on the address of the current label
		/// </summary>
		/// <param name="address">带标签的地址信息，例如s=A,那么标签就是A</param>
		/// <returns>内存偏移地址</returns>
		public OperateResult<uint> ReadValueHandle( string address )
		{
			if (!address.StartsWith( "s=" )) return new OperateResult<uint>( StringResources.Language.SAMAddressStartWrong );

			OperateResult<byte[]> build = BuildReadWriteCommand( address, 4, false, StrToAdsBytes( address.Substring( 2 ) ) );
			if (!build.IsSuccess) return OperateResult.CreateFailedResult<uint>( build );

			OperateResult<byte[]> read = ReadFromCoreServer( build.Content );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<uint>( read );

			OperateResult check = CheckResponse( read.Content );
			if (!check.IsSuccess) return OperateResult.CreateFailedResult<uint>( check );

			return OperateResult.CreateSuccessResult( BitConverter.ToUInt32( read.Content, 46 ) );
		}

		/// <summary>
		/// 将字符串的地址转换为内存的地址，其他地址则不操作<br />
		/// Converts the address of a string to the address of a memory, other addresses do not operate
		/// </summary>
		/// <param name="address">地址信息，s=A的地址转换为i=100000的形式</param>
		/// <returns>地址</returns>
		public OperateResult<string> TransValueHandle( string address )
		{
			if (address.StartsWith( "s=" ))
			{
				if (useTagCache)
				{
					lock (tagLock)
					{
						if (tagCaches.ContainsKey( address ))
						{
							return OperateResult.CreateSuccessResult( $"i={tagCaches[address]}" );
						}
					}
				}
				OperateResult<uint> read = ReadValueHandle( address );
				if (!read.IsSuccess) return OperateResult.CreateFailedResult<string>( read );

				if (useTagCache)
				{
					lock (tagLock)
					{
						if (!tagCaches.ContainsKey( address ))
						{
							tagCaches.Add( address, read.Content );
						}
					}
				}
				return OperateResult.CreateSuccessResult( $"i={read.Content}" );
			}
			else
				return OperateResult.CreateSuccessResult( address );
		}

		/// <summary>
		/// 读取Ads设备的设备信息。主要是版本号，设备名称<br />
		/// Read the device information of the Ads device. Mainly version number, device name
		/// </summary>
		/// <returns>设备信息</returns>
		[HslMqttApi( "ReadAdsDeviceInfo", "读取Ads设备的设备信息。主要是版本号，设备名称" )]
		public OperateResult<AdsDeviceInfo> ReadAdsDeviceInfo( )
		{
			OperateResult<byte[]> build = BuildReadDeviceInfoCommand( );
			if (!build.IsSuccess) return OperateResult.CreateFailedResult<AdsDeviceInfo>( build );

			OperateResult<byte[]> read = ReadFromCoreServer( build.Content );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<AdsDeviceInfo>( read );

			OperateResult check = CheckResponse( read.Content );
			if (!check.IsSuccess) return OperateResult.CreateFailedResult<AdsDeviceInfo>( check );

			return OperateResult.CreateSuccessResult( new AdsDeviceInfo( read.Content.RemoveBegin( 42 ) ) );
		}

		/// <summary>
		/// 读取Ads设备的状态信息，其中<see cref="OperateResult{T1, T2}.Content1"/>是Ads State，<see cref="OperateResult{T1, T2}.Content2"/>是Device State<br />
		/// Read the status information of the Ads device, where <see cref="OperateResult{T1, T2}.Content1"/> is the Ads State, and <see cref="OperateResult{T1, T2}.Content2"/> is the Device State
		/// </summary>
		/// <returns>设备状态信息</returns>
		[HslMqttApi( "ReadAdsState", "读取Ads设备的状态信息" )]
		public OperateResult<ushort, ushort> ReadAdsState( )
		{
			OperateResult<byte[]> build = BuildReadStateCommand( );
			if (!build.IsSuccess) return OperateResult.CreateFailedResult<ushort, ushort>( build );

			OperateResult<byte[]> read = ReadFromCoreServer( build.Content );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<ushort, ushort>( read );

			OperateResult check = CheckResponse( read.Content );
			if (!check.IsSuccess) return OperateResult.CreateFailedResult<ushort, ushort>( check );

			return OperateResult.CreateSuccessResult( BitConverter.ToUInt16( read.Content, 42 ), BitConverter.ToUInt16( read.Content, 44 ) );
		}

		/// <summary>
		/// 写入Ads的状态，可以携带数据信息，数据可以为空<br />
		/// Write the status of Ads, can carry data information, and the data can be empty
		/// </summary>
		/// <param name="state">ads state</param>
		/// <param name="deviceState">device state</param>
		/// <param name="data">数据信息</param>
		/// <returns>是否写入成功</returns>
		[HslMqttApi( "WriteAdsState", "写入Ads的状态，可以携带数据信息，数据可以为空" )]
		public OperateResult WriteAdsState( short state, short deviceState, byte[] data )
		{
			OperateResult<byte[]> build = BuildWriteControlCommand( state, deviceState, data );
			if (!build.IsSuccess) return build;

			OperateResult<byte[]> read = ReadFromCoreServer( build.Content );
			if (!read.IsSuccess) return read;

			OperateResult check = CheckResponse( read.Content );
			if (!check.IsSuccess) return check;

			return OperateResult.CreateSuccessResult( );
		}

		/// <summary>
		/// 释放当前的系统句柄，该句柄是通过<see cref="ReadValueHandle(string)"/>获取的
		/// </summary>
		/// <param name="handle">句柄</param>
		/// <returns>是否释放成功</returns>
		public OperateResult ReleaseSystemHandle( uint handle )
		{
			OperateResult<byte[]> build = BuildReleaseSystemHandle( handle );
			if (!build.IsSuccess) return build;

			OperateResult<byte[]> read = ReadFromCoreServer( build.Content );
			if (!read.IsSuccess) return read;

			OperateResult check = CheckResponse( read.Content );
			if (!check.IsSuccess) return check;

			return OperateResult.CreateSuccessResult( );
		}
#if !NET35 && !NET20
		/// <inheritdoc cref="ReadValueHandle(string)"/>
		public async Task<OperateResult<uint>> ReadValueHandleAsync( string address )
		{
			if (!address.StartsWith( "s=" )) return new OperateResult<uint>( StringResources.Language.SAMAddressStartWrong );

			OperateResult<byte[]> build = BuildReadWriteCommand( address, 4, false, StrToAdsBytes( address.Substring( 2 ) ) );
			if (!build.IsSuccess) return OperateResult.CreateFailedResult<uint>( build );

			OperateResult<byte[]> read = await ReadFromCoreServerAsync( build.Content );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<uint>( read );

			OperateResult check = CheckResponse( read.Content );
			if (!check.IsSuccess) return OperateResult.CreateFailedResult<uint>( check );

			return OperateResult.CreateSuccessResult( BitConverter.ToUInt32( read.Content, 46 ) );
		}

		/// <inheritdoc cref="TransValueHandle(string)"/>
		public async Task<OperateResult<string>> TransValueHandleAsync( string address )
		{
			if (address.StartsWith( "s=" ))
			{
				if (useTagCache)
				{
					lock (tagLock)
					{
						if (tagCaches.ContainsKey( address ))
						{
							return OperateResult.CreateSuccessResult( $"i={tagCaches[address]}" );
						}
					}
				}
				OperateResult<uint> read = await ReadValueHandleAsync( address );
				if (!read.IsSuccess) return OperateResult.CreateFailedResult<string>( read );

				if (useTagCache)
				{
					lock (tagLock)
					{
						if (!tagCaches.ContainsKey( address ))
						{
							tagCaches.Add( address, read.Content );
						}
					}
				}
				return OperateResult.CreateSuccessResult( $"i={read.Content}" );
			}
			else
				return OperateResult.CreateSuccessResult( address );
		}

		/// <inheritdoc cref="ReadAdsDeviceInfo"/>
		public async Task<OperateResult<AdsDeviceInfo>> ReadAdsDeviceInfoAsync( )
		{
			OperateResult<byte[]> build = BuildReadDeviceInfoCommand( );
			if (!build.IsSuccess) return OperateResult.CreateFailedResult<AdsDeviceInfo>( build );

			OperateResult<byte[]> read = await ReadFromCoreServerAsync( build.Content );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<AdsDeviceInfo>( read );

			OperateResult check = CheckResponse( read.Content );
			if (!check.IsSuccess) return OperateResult.CreateFailedResult<AdsDeviceInfo>( check );

			return OperateResult.CreateSuccessResult( new AdsDeviceInfo( read.Content.RemoveBegin( 42 ) ) );
		}

		/// <inheritdoc cref="ReadAdsState"/>
		public async Task<OperateResult<ushort, ushort>> ReadAdsStateAsync( )
		{
			OperateResult<byte[]> build = BuildReadStateCommand( );
			if (!build.IsSuccess) return OperateResult.CreateFailedResult<ushort, ushort>( build );

			OperateResult<byte[]> read = await ReadFromCoreServerAsync( build.Content );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<ushort, ushort>( read );

			OperateResult check = CheckResponse( read.Content );
			if (!check.IsSuccess) return OperateResult.CreateFailedResult<ushort, ushort>( check );

			return OperateResult.CreateSuccessResult( BitConverter.ToUInt16( read.Content, 42 ), BitConverter.ToUInt16( read.Content, 44 ) );
		}

		/// <inheritdoc cref="WriteAdsState(short, short, byte[])"/>
		public async Task<OperateResult> WriteAdsStateAsync( short state, short deviceState, byte[] data )
		{
			OperateResult<byte[]> build = BuildWriteControlCommand( state, deviceState, data );
			if (!build.IsSuccess) return build;

			OperateResult<byte[]> read = await ReadFromCoreServerAsync( build.Content );
			if (!read.IsSuccess) return read;

			OperateResult check = CheckResponse( read.Content );
			if (!check.IsSuccess) return check;

			return OperateResult.CreateSuccessResult( );
		}

		/// <inheritdoc cref="ReleaseSystemHandle(uint)"/>
		public async Task<OperateResult> ReleaseSystemHandleAsync( uint handle )
		{
			OperateResult<byte[]> build = BuildReleaseSystemHandle( handle );
			if (!build.IsSuccess) return build;

			OperateResult<byte[]> read = await ReadFromCoreServerAsync( build.Content );
			if (!read.IsSuccess) return read;

			OperateResult check = CheckResponse( read.Content );
			if (!check.IsSuccess) return check;

			return OperateResult.CreateSuccessResult( );
		}
#endif

		#region Read Write Override

		/// <summary>
		/// 读取PLC的数据，地址共有三种格式，一：I,Q,M数据信息，举例M0,M100；二：内存地址，i=100000；三：标签地址，s=A<br />
		/// Read PLC data, there are three formats of address, one: I, Q, M data information, such as M0, M100; two: memory address, i = 100000; three: tag address, s = A
		/// </summary>
		/// <param name="address">地址信息，地址共有三种格式，一：I,Q,M数据信息，举例M0,M100；二：内存地址，i=100000；三：标签地址，s=A</param>
		/// <param name="length">长度</param>
		/// <returns>包含是否成功的结果对象</returns>
		[HslMqttApi( "ReadByteArray", "" )]
		public override OperateResult<byte[]> Read( string address, ushort length )
		{
			// 先检查地址
			OperateResult<string> addressCheck = TransValueHandle( address );
			if (!addressCheck.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( addressCheck );

			address = addressCheck.Content;

			OperateResult<byte[]> build = BuildReadCommand( address, length, false );
			if (!build.IsSuccess) return build;

			OperateResult<byte[]> read = ReadFromCoreServer( build.Content );
			if (!read.IsSuccess) return read;

			OperateResult check = CheckResponse( read.Content );
			if (!check.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( check );

			return OperateResult.CreateSuccessResult( SoftBasic.ArrayRemoveBegin( read.Content, 46 ) );
		}

		/// <summary>
		/// 写入PLC的数据，地址共有三种格式，一：I,Q,M数据信息，举例M0,M100；二：内存地址，i=100000；三：标签地址，s=A<br />
		/// There are three formats for the data written into the PLC. One: I, Q, M data information, such as M0, M100; two: memory address, i = 100000; three: tag address, s = A
		/// </summary>
		/// <param name="address">地址信息，地址共有三种格式，一：I,Q,M数据信息，举例M0,M100；二：内存地址，i=100000；三：标签地址，s=A</param>
		/// <param name="value">数据值</param>
		/// <returns>是否写入成功</returns>
		[HslMqttApi( "WriteByteArray", "" )]
		public override OperateResult Write( string address, byte[] value )
		{
			OperateResult<string> addressCheck = TransValueHandle( address );
			if (!addressCheck.IsSuccess) return addressCheck;

			address = addressCheck.Content;

			OperateResult<byte[]> build = BuildWriteCommand( address, value, false );
			if (!build.IsSuccess) return build;

			OperateResult<byte[]> read = ReadFromCoreServer( build.Content );
			if (!read.IsSuccess) return read;

			OperateResult check = CheckResponse( read.Content );
			if (!check.IsSuccess) return check;

			return OperateResult.CreateSuccessResult( );
		}

		/// <summary>
		/// 读取PLC的数据，地址共有三种格式，一：I,Q,M数据信息，举例M0,M100；二：内存地址，i=100000；三：标签地址，s=A<br />
		/// Read PLC data, there are three formats of address, one: I, Q, M data information, such as M0, M100; two: memory address, i = 100000; three: tag address, s = A
		/// </summary>
		/// <param name="address">PLC的地址信息，例如 M10</param>
		/// <param name="length">数据长度</param>
		/// <returns>包含是否成功的结果对象</returns>
		[HslMqttApi( "ReadBoolArray", "" )]
		public override OperateResult<bool[]> ReadBool( string address, ushort length )
		{
			OperateResult<string> addressCheck = TransValueHandle( address );
			if (!addressCheck.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( addressCheck );

			address = addressCheck.Content;

			OperateResult<byte[]> build = BuildReadCommand( address, length, true );
			if (!build.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( build );

			OperateResult<byte[]> read = ReadFromCoreServer( build.Content );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( read ); ;

			OperateResult check = CheckResponse( read.Content );
			if (!check.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( check );

			return OperateResult.CreateSuccessResult( SoftBasic.ByteToBoolArray( SoftBasic.ArrayRemoveBegin( read.Content, 46 ) ) );
		}

		/// <summary>
		/// 写入PLC的数据，地址共有三种格式，一：I,Q,M数据信息，举例M0,M100；二：内存地址，i=100000；三：标签地址，s=A<br />
		/// There are three formats for the data written into the PLC. One: I, Q, M data information, such as M0, M100; two: memory address, i = 100000; three: tag address, s = A
		/// </summary>
		/// <param name="address">地址信息</param>
		/// <param name="value">数据值</param>
		/// <returns>是否写入成功</returns>
		[HslMqttApi( "WriteBoolArray", "" )]
		public override OperateResult Write( string address, bool[] value )
		{
			OperateResult<string> addressCheck = TransValueHandle( address );
			if (!addressCheck.IsSuccess) return addressCheck;

			address = addressCheck.Content;

			OperateResult<byte[]> build = BuildWriteCommand( address, value, true );
			if (!build.IsSuccess) return build;

			OperateResult<byte[]> read = ReadFromCoreServer( build.Content );
			if (!read.IsSuccess) return read;

			OperateResult check = CheckResponse( read.Content );
			if (!check.IsSuccess) return check;

			return OperateResult.CreateSuccessResult( );
		}

		/// <summary>
		/// 读取PLC的数据，地址共有三种格式，一：I,Q,M数据信息，举例M0,M100；二：内存地址，i=100000；三：标签地址，s=A<br />
		/// Read PLC data, there are three formats of address, one: I, Q, M data information, such as M0, M100; two: memory address, i = 100000; three: tag address, s = A
		/// </summary>
		/// <param name="address">地址信息</param>
		/// <returns>包含是否成功的结果对象</returns>
		[HslMqttApi( "ReadByte", "" )]
		public OperateResult<byte> ReadByte( string address ) => ByteTransformHelper.GetResultFromArray( Read( address, 1 ) );

		/// <summary>
		/// 写入PLC的数据，地址共有三种格式，一：I,Q,M数据信息，举例M0,M100；二：内存地址，i=100000；三：标签地址，s=A<br />
		/// There are three formats for the data written into the PLC. One: I, Q, M data information, such as M0, M100; two: memory address, i = 100000; three: tag address, s = A
		/// </summary>
		/// <param name="address">地址信息</param>
		/// <param name="value">数据值</param>
		/// <returns>是否写入成功</returns>
		[HslMqttApi( "WriteByte", "" )]
		public OperateResult Write( string address, byte value ) => Write( address, new byte[] { value } );

		#endregion

		#region Read Write Async Override
#if !NET35 && !NET20
		/// <inheritdoc cref="Read(string, ushort)"/>
		public override async Task<OperateResult<byte[]>> ReadAsync( string address, ushort length )
		{
			// 先检查地址
			OperateResult<string> addressCheck = await TransValueHandleAsync( address );
			if (!addressCheck.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( addressCheck );

			address = addressCheck.Content;

			OperateResult<byte[]> build = BuildReadCommand( address, length, false );
			if (!build.IsSuccess) return build;

			OperateResult<byte[]> read = await ReadFromCoreServerAsync( build.Content );
			if (!read.IsSuccess) return read;

			OperateResult check = CheckResponse( read.Content );
			if (!check.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( check );

			return OperateResult.CreateSuccessResult( SoftBasic.ArrayRemoveBegin( read.Content, 46 ) );
		}

		/// <inheritdoc cref="Write(string, byte[])"/>
		public override async Task<OperateResult> WriteAsync( string address, byte[] value )
		{
			OperateResult<string> addressCheck = await TransValueHandleAsync( address );
			if (!addressCheck.IsSuccess) return addressCheck;

			address = addressCheck.Content;

			OperateResult<byte[]> build = BuildWriteCommand( address, value, false );
			if (!build.IsSuccess) return build;

			OperateResult<byte[]> read = await ReadFromCoreServerAsync( build.Content );
			if (!read.IsSuccess) return read;

			OperateResult check = CheckResponse( read.Content );
			if (!check.IsSuccess) return check;

			return OperateResult.CreateSuccessResult( );
		}

		/// <inheritdoc cref="ReadBool(string, ushort)"/>
		public override async Task<OperateResult<bool[]>> ReadBoolAsync( string address, ushort length )
		{
			OperateResult<string> addressCheck = await TransValueHandleAsync( address );
			if (!addressCheck.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( addressCheck );

			address = addressCheck.Content;

			OperateResult<byte[]> build = BuildReadCommand( address, length, true );
			if (!build.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( build );

			OperateResult<byte[]> read = await ReadFromCoreServerAsync( build.Content );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( read ); ;

			OperateResult check = CheckResponse( read.Content );
			if (!check.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( check );

			return OperateResult.CreateSuccessResult( SoftBasic.ByteToBoolArray( SoftBasic.ArrayRemoveBegin( read.Content, 46 ) ) );
		}

		/// <inheritdoc cref="Write(string, bool[])"/>
		public override async Task<OperateResult> WriteAsync( string address, bool[] value )
		{
			OperateResult<string> addressCheck = await TransValueHandleAsync( address );
			if (!addressCheck.IsSuccess) return addressCheck;

			address = addressCheck.Content;

			OperateResult<byte[]> build = BuildWriteCommand( address, value, true );
			if (!build.IsSuccess) return build;

			OperateResult<byte[]> read = await ReadFromCoreServerAsync( build.Content );
			if (!read.IsSuccess) return read;

			OperateResult check = CheckResponse( read.Content );
			if (!check.IsSuccess) return check;

			return OperateResult.CreateSuccessResult( );
		}

		/// <inheritdoc cref="ReadByte(string)"/>
		public async Task<OperateResult<byte>> ReadByteAsync( string address ) => ByteTransformHelper.GetResultFromArray( await ReadAsync( address, 1 ) );

		/// <inheritdoc cref="Write(string, byte)"/>
		public async Task<OperateResult> WriteAsync( string address, byte value ) => await WriteAsync( address, new byte[] { value } );
#endif
		#endregion

		#region Build Command

		/// <summary>
		/// 根据命令码ID，消息ID，数据信息组成AMS的命令码
		/// </summary>
		/// <param name="commandId">命令码ID</param>
		/// <param name="data">数据内容</param>
		/// <returns>打包之后的数据信息，没有填写AMSNetId的Target和Source内容</returns>
		public byte[] BuildAmsHeaderCommand( ushort commandId, byte[] data )
		{
			if (data == null) data = new byte[0];

			uint invokeId = (uint)incrementCount.GetCurrentValue( );

			byte[] buffer = new byte[32 + data.Length];
			targetAMSNetId.CopyTo( buffer, 0 );
			sourceAMSNetId.CopyTo( buffer, 8 );
			buffer[16] = BitConverter.GetBytes( commandId )[0];           // Command ID
			buffer[17] = BitConverter.GetBytes( commandId )[1];
			buffer[18] = 0x04;                                            // flag: Tcp, Request
			buffer[19] = 0x00;
			buffer[20] = BitConverter.GetBytes( data.Length )[0];         // Size of the data range. The unit is byte
			buffer[21] = BitConverter.GetBytes( data.Length )[1];
			buffer[22] = BitConverter.GetBytes( data.Length )[2];
			buffer[23] = BitConverter.GetBytes( data.Length )[3];
			buffer[24] = 0x00;                                            // AMS error number
			buffer[25] = 0x00;
			buffer[26] = 0x00;
			buffer[27] = 0x00;
			buffer[28] = BitConverter.GetBytes( invokeId )[0];
			buffer[29] = BitConverter.GetBytes( invokeId )[1];
			buffer[30] = BitConverter.GetBytes( invokeId )[2];
			buffer[31] = BitConverter.GetBytes( invokeId )[3];
			data.CopyTo( buffer, 32 );

			return PackAmsTcpHelper( AmsTcpHeaderFlags.Command, buffer );
		}

		/// <summary>
		/// 构建读取设备信息的命令报文
		/// </summary>
		/// <returns>报文信息</returns>
		public OperateResult<byte[]> BuildReadDeviceInfoCommand( )
		{
			return OperateResult.CreateSuccessResult( BuildAmsHeaderCommand( BeckhoffCommandId.ReadDeviceInfo, null ) );
		}

		/// <summary>
		/// 构建读取状态的命令报文
		/// </summary>
		/// <returns>报文信息</returns>
		public OperateResult<byte[]> BuildReadStateCommand( )
		{
			return OperateResult.CreateSuccessResult( BuildAmsHeaderCommand( BeckhoffCommandId.ReadState, null ) );
		}

		/// <summary>
		/// 构建写入状态的命令报文
		/// </summary>
		/// <param name="state">Ads state</param>
		/// <param name="deviceState">Device state</param>
		/// <param name="data">Data</param>
		/// <returns>报文信息</returns>
		public OperateResult<byte[]> BuildWriteControlCommand( short state, short deviceState, byte[] data )
		{
			if (data == null) data = new byte[0];
			byte[] buffer = new byte[8 + data.Length];

			return OperateResult.CreateSuccessResult( BuildAmsHeaderCommand( BeckhoffCommandId.WriteControl,
				SoftBasic.SpliceArray( BitConverter.GetBytes( state ), BitConverter.GetBytes( deviceState ), BitConverter.GetBytes( data.Length ), data ) ) );
		}

		/// <summary>
		/// 构建写入的指令信息
		/// </summary>
		/// <param name="address">地址信息</param>
		/// <param name="length">数据长度</param>
		/// <param name="isBit">是否是位信息</param>
		/// <returns>结果内容</returns>
		public OperateResult<byte[]> BuildReadCommand( string address, int length, bool isBit )
		{
			OperateResult<uint, uint> analysis = AnalysisAddress( address, isBit );
			if (!analysis.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( analysis );

			byte[] data = new byte[12];
			BitConverter.GetBytes( analysis.Content1 ).CopyTo( data, 0 );
			BitConverter.GetBytes( analysis.Content2 ).CopyTo( data, 4 );
			BitConverter.GetBytes( length ).CopyTo( data, 8 );
			return OperateResult.CreateSuccessResult( BuildAmsHeaderCommand( BeckhoffCommandId.Read, data ) );
		}

		/// <summary>
		/// 构建写入的指令信息
		/// </summary>
		/// <param name="address">地址信息</param>
		/// <param name="length">数据长度</param>
		/// <param name="isBit">是否是位信息</param>
		/// <param name="value">写入的数值</param>
		/// <returns>结果内容</returns>
		public OperateResult<byte[]> BuildReadWriteCommand( string address, int length, bool isBit, byte[] value )
		{
			OperateResult<uint, uint> analysis = AnalysisAddress( address, isBit );
			if (!analysis.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( analysis );

			byte[] data = new byte[16 + value.Length];
			BitConverter.GetBytes( analysis.Content1 ).CopyTo( data, 0 );
			BitConverter.GetBytes( analysis.Content2 ).CopyTo( data, 4 );
			BitConverter.GetBytes( length ).CopyTo( data, 8 );
			BitConverter.GetBytes( value.Length ).CopyTo( data, 12 );
			value.CopyTo( data, 16 );

			return OperateResult.CreateSuccessResult( BuildAmsHeaderCommand( BeckhoffCommandId.ReadWrite, data ) );
		}

		/// <summary>
		/// 构建写入的指令信息
		/// </summary>
		/// <param name="address">地址信息</param>
		/// <param name="value">数据</param>
		/// <param name="isBit">是否是位信息</param>
		/// <returns>结果内容</returns>
		public OperateResult<byte[]> BuildWriteCommand( string address, byte[] value, bool isBit )
		{
			OperateResult<uint, uint> analysis = AnalysisAddress( address, isBit );
			if (!analysis.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( analysis );

			byte[] data = new byte[12 + value.Length];
			BitConverter.GetBytes( analysis.Content1 ).CopyTo( data, 0 );
			BitConverter.GetBytes( analysis.Content2 ).CopyTo( data, 4 );
			BitConverter.GetBytes( value.Length ).CopyTo( data, 8 );
			value.CopyTo( data, 12 );

			return OperateResult.CreateSuccessResult( BuildAmsHeaderCommand( BeckhoffCommandId.Write, data ) );
		}

		/// <summary>
		/// 构建写入的指令信息
		/// </summary>
		/// <param name="address">地址信息</param>
		/// <param name="value">数据</param>
		/// <param name="isBit">是否是位信息</param>
		/// <returns>结果内容</returns>
		public OperateResult<byte[]> BuildWriteCommand( string address, bool[] value, bool isBit )
		{
			OperateResult<uint, uint> analysis = AnalysisAddress( address, isBit );
			if (!analysis.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( analysis );

			byte[] buffer = SoftBasic.BoolArrayToByte( value );
			byte[] data = new byte[12 + buffer.Length];
			BitConverter.GetBytes( analysis.Content1 ).CopyTo( data, 0 );
			BitConverter.GetBytes( analysis.Content2 ).CopyTo( data, 4 );
			BitConverter.GetBytes( buffer.Length ).CopyTo( data, 8 );
			buffer.CopyTo( data, 12 );

			return OperateResult.CreateSuccessResult( BuildAmsHeaderCommand( BeckhoffCommandId.Write, data ) );
		}

		/// <summary>
		/// 构建释放句柄的报文信息，当获取了变量的句柄后，这个句柄就被释放
		/// </summary>
		/// <param name="handle">句柄信息</param>
		/// <returns>报文的结果内容</returns>
		public OperateResult<byte[]> BuildReleaseSystemHandle( uint handle )
		{
			byte[] data = new byte[16];
			BitConverter.GetBytes( 0xF006 ).CopyTo( data, 0 );
			BitConverter.GetBytes( 0x0004 ).CopyTo( data, 8 );
			BitConverter.GetBytes( handle ).CopyTo( data, 12 );

			return OperateResult.CreateSuccessResult( BuildAmsHeaderCommand( BeckhoffCommandId.Write, data ) );
		}

		#endregion

		#region Private Member

		private bool useTagCache = false;
		private readonly Dictionary<string, uint> tagCaches = new Dictionary<string, uint>( );
		private readonly object tagLock = new object( );
		private readonly SoftIncrementCount incrementCount = new SoftIncrementCount( int.MaxValue, 1, 1 );

		#endregion

		#region Object Override

		/// <inheritdoc/>
		public override string ToString( ) => $"BeckhoffAdsNet[{IpAddress}:{Port}]";

		#endregion

		#region Static Method

		/// <summary>
		/// 检查从PLC的反馈的数据报文是否正确
		/// </summary>
		/// <param name="response">反馈报文</param>
		/// <returns>检查结果</returns>
		public static OperateResult CheckResponse( byte[] response )
		{
			try
			{
				int ams = BitConverter.ToInt32( response, 30 );
				if (ams > 0) return new OperateResult( ams, GetErrorCodeText( ams ) + Environment.NewLine + "Source:" + response.ToHexString( ' ' ) );

				int status = BitConverter.ToInt32( response, 38 );
				if (status != 0) return new OperateResult( status, StringResources.Language.UnknownError + " Source:" + response.ToHexString( ' ' ) );
			}
			catch (Exception ex)
			{
				return new OperateResult( ex.Message + " Source:" + response.ToHexString( ' ' ) );
			}

			return OperateResult.CreateSuccessResult( );
		}

		/// <summary>
		/// 将实际的包含AMS头报文和数据报文的命令，打包成实际可发送的命令
		/// </summary>
		/// <param name="headerFlags">命令头信息</param>
		/// <param name="command">命令信息</param>
		/// <returns>结果信息</returns>
		public static byte[] PackAmsTcpHelper( AmsTcpHeaderFlags headerFlags, byte[] command )
		{
			byte[] buffer = new byte[6 + command.Length];
			BitConverter.GetBytes( (ushort)headerFlags ).CopyTo( buffer, 0 );
			BitConverter.GetBytes( command.Length ).CopyTo( buffer, 2 );
			command.CopyTo( buffer, 6 );
			return buffer;
		}

		/// <summary>
		/// 分析当前的地址信息，根据结果信息进行解析出真实的偏移地址
		/// </summary>
		/// <param name="address">地址</param>
		/// <param name="isBit">是否位访问</param>
		/// <returns>结果内容</returns>
		public static OperateResult<uint, uint> AnalysisAddress( string address, bool isBit )
		{
			var result = new OperateResult<uint, uint>( );
			try
			{
				if (address.StartsWith( "i=" ))
				{
					result.Content1 = 0xF005;
					result.Content2 = uint.Parse( address.Substring( 2 ) );
				}
				else if (address.StartsWith( "s=" ))
				{
					result.Content1 = 0xF003;
					result.Content2 = 0x0000;
				}
				else
				{
					switch (address[0])
					{
						case 'M':
						case 'm':
							{
								if (isBit)
									result.Content1 = 0x4021;
								else
									result.Content1 = 0x4020;
								break;
							}
						case 'I':
						case 'i':
							{
								if (isBit)
									result.Content1 = 0xF021;
								else
									result.Content1 = 0xF020;
								break;
							}
						case 'Q':
						case 'q':
							{
								if (isBit)
									result.Content1 = 0xF031;
								else
									result.Content1 = 0xF030;
								break;
							}
						default: throw new Exception( StringResources.Language.NotSupportedDataType );

					}
					result.Content2 = uint.Parse( address.Substring( 1 ) );
				}
			}
			catch (Exception ex)
			{
				result.Message = ex.Message;
				return result;
			}

			result.IsSuccess = true;
			result.Message = StringResources.Language.SuccessText;
			return result;
		}

		/// <summary>
		/// 将字符串名称转变为ADS协议可识别的字节数组
		/// </summary>
		/// <param name="value">值</param>
		/// <returns>字节数组</returns>
		public static byte[] StrToAdsBytes( string value )
		{
			return SoftBasic.SpliceArray( Encoding.ASCII.GetBytes( value ), new byte[1] );
		}

		/// <summary>
		/// 将字符串的信息转换为AMS目标的地址
		/// </summary>
		/// <param name="amsNetId">目标信息</param>
		/// <returns>字节数组</returns>
		public static byte[] StrToAMSNetId( string amsNetId )
		{
			byte[] buffer;
			string ip = amsNetId;
			if (amsNetId.IndexOf( ':' ) > 0)
			{
				buffer = new byte[8];
				string[] ipPort = amsNetId.Split( new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries );
				ip = ipPort[0];
				buffer[6] = BitConverter.GetBytes( int.Parse( ipPort[1] ) )[0];
				buffer[7] = BitConverter.GetBytes( int.Parse( ipPort[1] ) )[1];
			}
			else
			{
				buffer = new byte[6];
			}
			string[] ips = ip.Split( new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries );
			for (int i = 0; i < ips.Length; i++)
			{
				buffer[i] = byte.Parse( ips[i] );
			}
			return buffer;
		}

		/// <summary>
		/// 根据byte数组信息提取出字符串格式的AMSNetId数据信息，方便日志查看
		/// </summary>
		/// <param name="data">原始的报文数据信息</param>
		/// <param name="index">起始的节点信息</param>
		/// <returns>Ams节点号信息</returns>
		public static string GetAmsNetIdString( byte[] data, int index )
		{
			StringBuilder sb = new StringBuilder( );
			sb.Append( data[index] );
			sb.Append( "." );
			sb.Append( data[index + 1] );
			sb.Append( "." );
			sb.Append( data[index + 2] );
			sb.Append( "." );
			sb.Append( data[index + 3] );
			sb.Append( "." );
			sb.Append( data[index + 4] );
			sb.Append( "." );
			sb.Append( data[index + 5] );
			sb.Append( ":" );
			sb.Append( BitConverter.ToUInt16( data, index + 6 ) );
			return sb.ToString( );
		}

		/// <summary>
		/// 根据AMS的错误号，获取到错误信息，错误信息来源于 wirshake 源代码文件 "..\wireshark\plugins\epan\ethercat\packet-ams.c"
		/// </summary>
		/// <param name="error">错误号</param>
		/// <returns>错误的描述信息</returns>
		public static string GetErrorCodeText( int error )
		{
			switch (error)
			{
				case  0: return "NO ERROR";
				case  1: return "INTERNAL";
				case  2: return "NO RTIME";
				case  3: return "ALLOC LOCKED MEM";
				case  4: return "INSERT MAILBOX";
				case  5: return "WRONGRECEIVEHMSG";
				case  6: return "TARGET PORT NOT FOUND";
				case  7: return "TARGET MACHINE NOT FOUND";
				case  8: return "UNKNOWN CMDID";
				case  9: return "BAD TASKID";
				case 10: return "NOIO";
				case 11: return "UNKNOWN AMSCMD";
				case 12: return "WIN32 ERROR";
				case 13: return "PORT NOT CONNECTED";
				case 14: return "INVALID AMS LENGTH";
				case 15: return "INVALID AMS NETID";
				case 16: return "LOW INST LEVEL";
				case 17: return "NO DEBUG INT AVAILABLE";
				case 18: return "PORT DISABLED";
				case 19: return "PORT ALREADY CONNECTED";
				case 20: return "AMSSYNC_W32ERROR";
				case 21: return "AMSSYNC_TIMEOUT";
				case 22: return "AMSSYNC_AMSERROR";
				case 23: return "AMSSYNC_NOINDEXINMAP";
				case 24: return "INVALID AMSPORT";
				case 25: return "NO MEMORY";
				case 26: return "TCP SEND";
				case 27: return "HOST UNREACHABLE";
				default: return StringResources.Language.UnknownError;
			}
		}

		#endregion

	}
}
