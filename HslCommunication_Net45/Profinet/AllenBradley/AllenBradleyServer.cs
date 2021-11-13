using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using HslCommunication.BasicFramework;
using HslCommunication.Core;
using HslCommunication.Core.Net;
using HslCommunication.Core.IMessage;
using HslCommunication.Reflection;
#if !NET35 && !NET20
using System.Threading.Tasks;
#endif

namespace HslCommunication.Profinet.AllenBradley
{
	/// <summary>
	/// <b>[商业授权]</b> AB PLC的虚拟服务器，仅支持和HSL组件的完美通信，可以手动添加一些节点。<br />
	/// <b>[Authorization]</b> AB PLC's virtual server only supports perfect communication with HSL components. You can manually add some nodes.
	/// </summary>
	/// <remarks>
	/// 本AB的虚拟PLC仅限商业授权用户使用，感谢支持。
	/// </remarks>
	public class AllenBradleyServer : NetworkDataServerBase
	{
		#region Constructor

		/// <summary>
		/// 实例化一个AB PLC协议的服务器<br />
		/// Instantiate an AB PLC protocol server
		/// </summary>
		public AllenBradleyServer( )
		{
			WordLength = 2;                                                             // 每个地址占1个字节的数据
			ByteTransform = new RegularByteTransform( );                                // 解析数据的类型
			Port = 44818;                                                               // 端口的默认值
			simpleHybird = new SimpleHybirdLock( );                                     // 词典锁
			abValues = new Dictionary<string, AllenBradleyItemValue>( );                // 数据
		}

		#endregion

		#region Public Properties

		/// <summary>
		/// 获取或设置当前的服务器的数据字节排序情况<br />
		/// Gets or sets the data byte ordering of the current server
		/// </summary>
		public DataFormat DataFormat
		{
			get => ByteTransform.DataFormat;
			set => ByteTransform.DataFormat = value;
		}

		#endregion

		#region Add Tag

		/// <summary>
		/// 向服务器新增一个新的Tag值<br />
		/// Add a new tag value to the server
		/// </summary>
		/// <param name="key">Tag名称</param>
		/// <param name="value">值</param>
		public void AddTagValue( string key, AllenBradleyItemValue value )
		{
			simpleHybird.Enter( );

			if (abValues.ContainsKey( key ))
				abValues[key] = value;
			else
				abValues.Add( key, value );

			simpleHybird.Leave( );
		}

		/// <summary>
		/// 向服务器新增一个新的bool类型的Tag值，并赋予初始化的值<br />
		/// Add a new tag value of type bool to the server and assign the initial value
		/// </summary>
		/// <param name="key">Tag名称</param>
		/// <param name="value">值信息</param>
		public void AddTagValue( string key, bool value )
		{
			AddTagValue( key, new AllenBradleyItemValue( )
			{
				IsArray = false,
				Buffer = value ? new byte[2] { 0xFF, 0xFF } : new byte[2] { 0x00, 0x00 },
				TypeLength = 2
			} );
		}

		/// <summary>
		/// 向服务器新增一个新的short类型的Tag值，并赋予初始化的值<br />
		/// Add a new short tag value to the server and assign the initial value
		/// </summary>
		/// <param name="key">Tag名称</param>
		/// <param name="value">值信息</param>
		public void AddTagValue( string key, short value )
		{
			AddTagValue( key, new AllenBradleyItemValue( )
			{
				IsArray = false,
				Buffer = ByteTransform.TransByte( value ),
				TypeLength = 2
			} );
		}

		/// <summary>
		/// 向服务器新增一个新的short数组的Tag值，并赋予初始化的值<br />
		/// Add a new short array Tag value to the server and assign the initial value
		/// </summary>
		/// <param name="key">Tag名称</param>
		/// <param name="value">值信息</param>
		public void AddTagValue( string key, short[] value )
		{
			AddTagValue( key, new AllenBradleyItemValue( )
			{
				IsArray = true,
				Buffer = ByteTransform.TransByte( value ),
				TypeLength = 2
			} );
		}

		/// <summary>
		/// 向服务器新增一个新的ushort类型的Tag值，并赋予初始化的值<br />
		/// Add a new tag value of ushort type to the server and assign the initial value
		/// </summary>
		/// <param name="key">Tag名称</param>
		/// <param name="value">值信息</param>
		public void AddTagValue( string key, ushort value )
		{
			AddTagValue( key, new AllenBradleyItemValue( )
			{
				IsArray = false,
				Buffer = ByteTransform.TransByte( value ),
				TypeLength = 2
			} );
		}

		/// <summary>
		/// 向服务器新增一个新的ushort数组的Tag值，并赋予初始化的值<br />
		/// Add a new ushort array Tag value to the server and assign the initial value
		/// </summary>
		/// <param name="key">Tag名称</param>
		/// <param name="value">值信息</param>
		public void AddTagValue( string key, ushort[] value )
		{
			AddTagValue( key, new AllenBradleyItemValue( )
			{
				IsArray = true,
				Buffer = ByteTransform.TransByte( value ),
				TypeLength = 2
			} );
		}

		/// <summary>
		/// 向服务器新增一个新的int类型的Tag值，并赋予初始化的值<br />
		/// Add a new Tag value of type int to the server and assign the initialized value
		/// </summary>
		/// <param name="key">Tag名称</param>
		/// <param name="value">值信息</param>
		public void AddTagValue( string key, int value )
		{
			AddTagValue( key, new AllenBradleyItemValue( )
			{
				IsArray = false,
				Buffer = ByteTransform.TransByte( value ),
				TypeLength = 4
			} );
		}

		/// <summary>
		/// 向服务器新增一个新的int数组的Tag值，并赋予初始化的值<br />
		/// Add a new Tag value of the int array to the server and assign the initialized value
		/// </summary>
		/// <param name="key">Tag名称</param>
		/// <param name="value">值信息</param>
		public void AddTagValue( string key, int[] value )
		{
			AddTagValue( key, new AllenBradleyItemValue( )
			{
				IsArray = true,
				Buffer = ByteTransform.TransByte( value ),
				TypeLength = 4
			} );
		}

		/// <summary>
		/// 向服务器新增一个新的uint类型的Tag值，并赋予初始化的值<br />
		/// Add a new uint tag value to the server and assign the initial value
		/// </summary>
		/// <param name="key">Tag名称</param>
		/// <param name="value">值信息</param>
		public void AddTagValue( string key, uint value )
		{
			AddTagValue( key, new AllenBradleyItemValue( )
			{
				IsArray = false,
				Buffer = ByteTransform.TransByte( value ),
				TypeLength = 4
			} );
		}

		/// <summary>
		/// 向服务器新增一个新的uint数组的Tag值，并赋予初始化的值<br />
		/// Add a new uint array Tag value to the server and assign the initial value
		/// </summary>
		/// <param name="key">Tag名称</param>
		/// <param name="value">值信息</param>
		public void AddTagValue( string key, uint[] value )
		{
			AddTagValue( key, new AllenBradleyItemValue( )
			{
				IsArray = true,
				Buffer = ByteTransform.TransByte( value ),
				TypeLength = 4
			} );
		}

		/// <summary>
		/// 向服务器新增一个新的long类型的Tag值，并赋予初始化的值<br />
		/// Add a new Tag value of type long to the server and assign the initialized value
		/// </summary>
		/// <param name="key">Tag名称</param>
		/// <param name="value">值信息</param>
		public void AddTagValue( string key, long value )
		{
			AddTagValue( key, new AllenBradleyItemValue( )
			{
				IsArray = false,
				Buffer = ByteTransform.TransByte( value ),
				TypeLength = 8
			} );
		}

		/// <summary>
		/// 向服务器新增一个新的long数组的Tag值，并赋予初始化的值<br />
		/// Add a new Long array Tag value to the server and assign the initial value
		/// </summary>
		/// <param name="key">Tag名称</param>
		/// <param name="value">值信息</param>
		public void AddTagValue( string key, long[] value )
		{
			AddTagValue( key, new AllenBradleyItemValue( )
			{
				IsArray = true,
				Buffer = ByteTransform.TransByte( value ),
				TypeLength = 8
			} );
		}

		/// <summary>
		/// 向服务器新增一个新的ulong类型的Tag值，并赋予初始化的值<br />
		/// Add a new Ulong type Tag value to the server and assign the initial value
		/// </summary>
		/// <param name="key">Tag名称</param>
		/// <param name="value">值信息</param>
		public void AddTagValue( string key, ulong value )
		{
			AddTagValue( key, new AllenBradleyItemValue( )
			{
				IsArray = false,
				Buffer = ByteTransform.TransByte( value ),
				TypeLength = 8
			} );
		}

		/// <summary>
		/// 向服务器新增一个新的ulong数组的Tag值，并赋予初始化的值<br />
		/// Add a new Ulong array Tag value to the server and assign the initial value
		/// </summary>
		/// <param name="key">Tag名称</param>
		/// <param name="value">值信息</param>
		public void AddTagValue( string key, ulong[] value )
		{
			AddTagValue( key, new AllenBradleyItemValue( )
			{
				IsArray = true,
				Buffer = ByteTransform.TransByte( value ),
				TypeLength = 8
			} );
		}

		/// <summary>
		/// 向服务器新增一个新的float类型的Tag值，并赋予初始化的值<br />
		/// Add a new tag value of type float to the server and assign the initial value
		/// </summary>
		/// <param name="key">Tag名称</param>
		/// <param name="value">值信息</param>
		public void AddTagValue( string key, float value )
		{
			AddTagValue( key, new AllenBradleyItemValue( )
			{
				IsArray = false,
				Buffer = ByteTransform.TransByte( value ),
				TypeLength = 4
			} );
		}

		/// <summary>
		/// 向服务器新增一个新的float数组的Tag值，并赋予初始化的值<br />
		/// Add a new Tag value of the float array to the server and assign the initialized value
		/// </summary>
		/// <param name="key">Tag名称</param>
		/// <param name="value">值信息</param>
		public void AddTagValue( string key, float[] value )
		{
			AddTagValue( key, new AllenBradleyItemValue( )
			{
				IsArray = true,
				Buffer = ByteTransform.TransByte( value ),
				TypeLength = 4
			} );
		}

		/// <summary>
		/// 向服务器新增一个新的double类型的Tag值，并赋予初始化的值<br />
		/// Add a new tag value of type double to the server and assign the initialized value
		/// </summary>
		/// <param name="key">Tag名称</param>
		/// <param name="value">值信息</param>
		public void AddTagValue( string key, double value )
		{
			AddTagValue( key, new AllenBradleyItemValue( )
			{
				IsArray = false,
				Buffer = ByteTransform.TransByte( value ),
				TypeLength = 8
			} );
		}

		/// <summary>
		/// 向服务器新增一个新的double数组的Tag值，并赋予初始化的值<br />
		/// Add a new double array Tag value to the server and assign the initialized value
		/// </summary>
		/// <param name="key">Tag名称</param>
		/// <param name="value">值信息</param>
		public void AddTagValue( string key, double[] value )
		{
			AddTagValue( key, new AllenBradleyItemValue( )
			{
				IsArray = true,
				Buffer = ByteTransform.TransByte( value ),
				TypeLength = 8
			} );
		}

		/// <summary>
		/// 向服务器新增一个新的string类型的Tag值，并赋予初始化的值<br />
		/// Add a new Tag value of string type to the server and assign the initial value
		/// </summary>
		/// <param name="key">Tag名称</param>
		/// <param name="value">值信息</param>
		/// <param name="maxLength">字符串的最长值</param>
		public void AddTagValue( string key, string value, int maxLength )
		{
			byte[] strBuffer = Encoding.UTF8.GetBytes( value );
			AddTagValue( key, new AllenBradleyItemValue( )
			{
				IsArray = false,
				Buffer = SoftBasic.ArrayExpandToLength( SoftBasic.SpliceArray( new byte[2], BitConverter.GetBytes( strBuffer.Length ), Encoding.UTF8.GetBytes( value ) ), maxLength ),
				TypeLength = maxLength
			} );
		}

		/// <summary>
		/// 向服务器新增一个新的string数组的Tag值，并赋予初始化的值<br />
		/// Add a new String array Tag value to the server and assign the initialized value
		/// </summary>
		/// <param name="key">Tag名称</param>
		/// <param name="value">值信息</param>
		/// <param name="maxLength">字符串的最长值</param>
		public void AddTagValue( string key, string[] value, int maxLength )
		{
			byte[] buffer = new byte[maxLength * value.Length];
			for (int i = 0; i < value.Length; i++)
			{
				byte[] strBuffer = Encoding.UTF8.GetBytes( value[i] );
				BitConverter.GetBytes( strBuffer.Length ).CopyTo( buffer, maxLength * i + 2 );
				strBuffer.CopyTo( buffer, maxLength * i + 6 );
			}
			AddTagValue( key, new AllenBradleyItemValue( )
			{
				IsArray = true,
				Buffer = buffer,
				TypeLength = maxLength
			} );
		}

		#endregion

		#region NetworkDataServerBase Override

		/// <inheritdoc/>
		[HslMqttApi( "ReadByteArray", "" )]
		public override OperateResult<byte[]> Read( string address, ushort length )
		{
			int index = 0;
			try
			{
				int indexFirst = address.IndexOf( '[' );
				int indexSecond = address.IndexOf( ']' );
				if (indexFirst > 0 && indexSecond > 0 && indexSecond > indexFirst)
				{
					index = int.Parse( address.Substring( indexFirst + 1, indexSecond - indexFirst - 1 ) );
					address = address.Substring( 0, indexFirst );
				}
			}
			catch (Exception ex)
			{
				return new OperateResult<byte[]>( ex.Message );
			}

			byte[] ret = null;
			simpleHybird.Enter( );
			if (abValues.ContainsKey( address ))
			{
				AllenBradleyItemValue abValue = abValues[address];
				if (!abValue.IsArray)
				{
					ret = new byte[abValue.Buffer.Length];
					abValue.Buffer.CopyTo( ret, 0 );
				}
				else
				{
					if ((index * abValue.TypeLength + length * abValue.TypeLength) <= abValue.Buffer.Length)
					{
						ret = new byte[length * abValue.TypeLength];
						Array.Copy( abValue.Buffer, index * abValue.TypeLength, ret, 0, ret.Length );
					}
				}
			}
			simpleHybird.Leave( );
			if (ret == null) return new OperateResult<byte[]>( StringResources.Language.AllenBradley04 );

			return OperateResult.CreateSuccessResult( ret );
		}

		/// <inheritdoc/>
		[HslMqttApi( "WriteByteArray", "" )]
		public override OperateResult Write( string address, byte[] value )
		{
			int index = 0;
			try
			{
				int indexFirst = address.IndexOf( '[' );
				int indexSecond = address.IndexOf( ']' );
				if (indexFirst > 0 && indexSecond > 0 && indexSecond > indexFirst)
				{
					index = int.Parse( address.Substring( indexFirst + 1, indexSecond - indexFirst - 1 ) );
					address = address.Substring( 0, indexFirst );
				}
			}
			catch (Exception ex)
			{
				return new OperateResult( ex.Message );
			}

			bool isWrite = false;
			simpleHybird.Enter( );
			if (abValues.ContainsKey( address ))
			{
				AllenBradleyItemValue abValue = abValues[address];
				if (!abValue.IsArray)
				{
					if (abValue.Buffer.Length == value.Length)
					{
						abValue.Buffer = value;
						isWrite = true;
					}
				}
				else
				{
					if ((index * abValue.TypeLength + value.Length) <= abValue.Buffer.Length)
					{
						Array.Copy( value, 0, abValue.Buffer, index * abValue.TypeLength, value.Length );
						isWrite = true;
					}
				}
			}
			simpleHybird.Leave( );
			if (!isWrite) return new OperateResult( StringResources.Language.AllenBradley04 );
			return OperateResult.CreateSuccessResult( );
		}

		#endregion

		#region Byte Read Write

		/// <inheritdoc cref="AllenBradleyNet.ReadByte(string)"/>
		[HslMqttApi( "ReadByte", "" )]
		public OperateResult<byte> ReadByte( string address ) => ByteTransformHelper.GetResultFromArray( Read( address, 1 ) );

		/// <inheritdoc cref="AllenBradleyNet.Write(string, byte)"/>
		[HslMqttApi( "WriteByte", "" )]
		public OperateResult Write( string address, byte value ) => Write( address, new byte[] { value } );

		#endregion

		#region Bool Read Write Operate

		/// <inheritdoc/>
		[HslMqttApi( "ReadBoolArray", "" )]
		public override OperateResult<bool[]> ReadBool( string address, ushort length ) => new OperateResult<bool[]>( StringResources.Language.NotSupportedFunction );

		/// <inheritdoc/>
		[HslMqttApi( "WriteBoolArray", "" )]
		public override OperateResult Write( string address, bool[] value ) => new OperateResult( StringResources.Language.NotSupportedFunction );

		/// <inheritdoc/>
		[HslMqttApi( "ReadBool", "" )]
		public override OperateResult<bool> ReadBool( string address )
		{
			bool isExist = false;
			bool ret = false;
			simpleHybird.Enter( );
			if (abValues.ContainsKey( address ))
			{
				isExist = true;
				AllenBradleyItemValue abValue = abValues[address];
				if (abValue.Buffer?.Length > 0) ret = abValue.Buffer[0] != 0x00;
			}
			simpleHybird.Leave( );
			if (isExist == false) return new OperateResult<bool>( StringResources.Language.AllenBradley04 );

			return OperateResult.CreateSuccessResult( ret );
		}

		/// <inheritdoc/>
		[HslMqttApi( "WriteBool", "" )]
		public override OperateResult Write( string address, bool value )
		{
			bool isExist = false;
			simpleHybird.Enter( );
			if (abValues.ContainsKey( address ))
			{
				isExist = true;
				AllenBradleyItemValue abValue = abValues[address];
				if (abValue.Buffer?.Length > 0) abValue.Buffer[0] = value ? (byte)0xFF : (byte)0x00;
				if (abValue.Buffer?.Length > 1) abValue.Buffer[1] = value ? (byte)0xFF : (byte)0x00;
			}
			simpleHybird.Leave( );
			if (isExist == false) return new OperateResult( StringResources.Language.AllenBradley04 );

			return OperateResult.CreateSuccessResult( );
		}

		/// <inheritdoc/>
		public override OperateResult<string> ReadString( string address, ushort length, Encoding encoding )
		{
			OperateResult<byte[]> read = Read( address, length );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<string>( read );

			if (read.Content.Length >= 6)
			{
				int strLength = BitConverter.ToInt32( read.Content, 2 );
				return OperateResult.CreateSuccessResult( encoding.GetString( read.Content, 6, strLength ) );
			}
			else
			{
				return OperateResult.CreateSuccessResult( encoding.GetString( read.Content ) );
			}
		}

		/// <inheritdoc/>
		public override OperateResult Write( string address, string value, Encoding encoding )
		{
			bool isExist = false;
			int index = 0;
			int indexFirst = address.IndexOf( '[' );
			int indexSecond = address.IndexOf( ']' );
			if (indexFirst > 0 && indexSecond > 0 && indexSecond > indexFirst)
			{
				index = int.Parse( address.Substring( indexFirst + 1, indexSecond - indexFirst - 1 ) );
				address = address.Substring( 0, indexFirst );
			}

			simpleHybird.Enter( );
			if (abValues.ContainsKey( address ))
			{
				isExist = true;
				AllenBradleyItemValue abValue = abValues[address];
				if (abValue.Buffer?.Length >= 6)
				{
					byte[] buffer = encoding.GetBytes( value );
					BitConverter.GetBytes( buffer.Length ).CopyTo( abValue.Buffer, 2 + index * abValue.TypeLength );
					if (buffer.Length > 0) Array.Copy( buffer, 0, abValue.Buffer, 6 + index * abValue.TypeLength, Math.Min( buffer.Length, abValue.Buffer.Length - 6 ) );
				}
			}
			simpleHybird.Leave( );
			if (isExist == false) return new OperateResult<bool>( StringResources.Language.AllenBradley04 );

			return OperateResult.CreateSuccessResult( );
		}

		#endregion

		#region NetServer Override

		/// <inheritdoc/>
		protected override void ThreadPoolLoginAfterClientCheck( Socket socket, System.Net.IPEndPoint endPoint )
		{
			// 开始接收数据信息
			OperateResult<byte[]> read1 = ReceiveByMessage( socket, 5000, new AllenBradleyMessage( ) );
			if (!read1.IsSuccess) return;

			// 构建随机的SessionID
			byte[] sessionId = new byte[4];
			random.NextBytes( sessionId );

			OperateResult send1 = Send( socket, AllenBradleyHelper.PackRequestHeader( 0x65, this.ByteTransform.TransUInt32(sessionId, 0), new byte[0] ) );
			if (!send1.IsSuccess) return;

			AppSession appSession = new AppSession( );
			appSession.IpEndPoint = endPoint;
			appSession.WorkSocket = socket;
			appSession.LoginAlias = this.ByteTransform.TransUInt32( sessionId, 0 ).ToString( );
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
#if NET20 || NET35
					OperateResult<byte[]> read1 = ReceiveByMessage( session.WorkSocket, 5000, new AllenBradleyMessage( ) );
#else
					OperateResult<byte[]> read1 = await ReceiveByMessageAsync( session.WorkSocket, 5000, new AllenBradleyMessage( ) );
#endif
					if (!read1.IsSuccess) { RemoveClient( session ); return; };

					if (!Authorization.asdniasnfaksndiqwhawfskhfaiw( )) { RemoveClient( session ); return; };

					LogNet?.WriteDebug( ToString( ), $"[{session.IpEndPoint}] Tcp {StringResources.Language.Receive}：{read1.Content.ToHexString( ' ' )}" );

					// 校验SessionID
					string sessionID = this.ByteTransform.TransUInt32( read1.Content, 4 ).ToString( );
					if (sessionID != session.LoginAlias)
					{
						LogNet?.WriteDebug( ToString( ), $"SessionID 不一致的请求，要求ID：{session.LoginAlias} 实际ID：{sessionID}" );
						Send( session.WorkSocket, AllenBradleyHelper.PackRequestHeader( 0x66, 0x64, this.ByteTransform.TransUInt32( read1.Content, 4 ), new byte[0] ) );
						RemoveClient( session );
						return;
					}

					byte[] back = ReadFromCipCore( read1.Content );

					Array.Copy( read1.Content, 4, back, 4, 4 ); // 修复会话ID
					if (back != null) session.WorkSocket.Send( back );
					else { RemoveClient( session ); return; }

					LogNet?.WriteDebug( ToString( ), $"[{session.IpEndPoint}] Tcp {StringResources.Language.Send}：{back.ToHexString( ' ' )}" );

					session.HeartTime = DateTime.Now;
					RaiseDataReceived( session, read1.Content );
					session.WorkSocket.BeginReceive( new byte[0], 0, 0, SocketFlags.None, new AsyncCallback( SocketAsyncCallBack ), session );
				}
				catch
				{
					RemoveClient( session );
				}
			}
		}

		/// <summary>
		/// 当收到mc协议的报文的时候应该触发的方法，允许继承重写，来实现自定义的返回，或是数据监听。
		/// </summary>
		/// <param name="cipAll">mc报文</param>
		/// <returns>返回的报文信息</returns>
		protected virtual byte[] ReadFromCipCore( byte[] cipAll )
		{
			if (BitConverter.ToInt16( cipAll, 2 ) == 0x66)
			{
				// 关闭连接。
				return AllenBradleyHelper.PackCommandResponse( new byte[0], true );
			}

			byte[] specificData = SoftBasic.ArrayRemoveBegin( cipAll, 24 );
			if (specificData[26] == 0x0A && specificData[27] == 0x02 && specificData[28] == 0x20 && specificData[29] == 0x02 && specificData[30] == 0x24 && specificData[31] == 0x01)
			{
				// 多项的读取的情况暂时进行屏蔽
				return null;
			}

			byte[] cipCore = ByteTransform.TransByte( specificData, 26, BitConverter.ToInt16( specificData, 24 ) );

			if (cipCore[0] == AllenBradleyHelper.CIP_READ_DATA || cipCore[0] == AllenBradleyHelper.CIP_READ_FRAGMENT)
			{
				// 读数据
				return AllenBradleyHelper.PackRequestHeader( 0x66, 0x10, AllenBradleyHelper.PackCommandSpecificData( new byte[] { 0x00, 0x00, 0x00, 0x00 }, AllenBradleyHelper.PackCommandSingleService( ReadByCommand( cipCore ) ) ) );
			}
			else if (cipCore[0] == AllenBradleyHelper.CIP_WRITE_DATA)
			{
				// 写数据
				return AllenBradleyHelper.PackRequestHeader( 0x66, 0x10, AllenBradleyHelper.PackCommandSpecificData( new byte[] { 0x00, 0x00, 0x00, 0x00 }, AllenBradleyHelper.PackCommandSingleService( WriteByMessage( cipCore ) ) ) );
			}
			else if (cipCore[0] == AllenBradleyHelper.CIP_READ_LIST)
			{
				// 读数据列表
				return AllenBradleyHelper.PackRequestHeader( 0x6F, 0x10, AllenBradleyHelper.PackCommandSpecificData( new byte[] { 0x00, 0x00, 0x00, 0x00 }, AllenBradleyHelper.PackCommandSingleService( ReadList( cipCore ) ) ) );
			}
			else
			{
				return null;
			}
		}

		private byte[] ReadList( byte[] cipCore )
		{
			ushort start = BitConverter.ToUInt16( cipCore, 6 );
			if (start == 0)
			{
				return SoftBasic.HexStringToBytes( @"
d5 00
06 00 77 00 00 00 15 00 43 4f 4d 4d 5f 33 5f 31
5f 50 4c 43 5f 31 5f 42 75 66 66 65 72 c4 00 8e
01 00 00 0e 00 4b 65 79 73 77 69 74 63 68 41 6c
61 72 6d c4 00 1f 02 00 00 0e 00 44 61 74 65 54
69 6d 65 53 79 6e 63 68 31 c4 20 7a 0c 00 00 11
00 4d 53 47 5f 33 5f 31 5f 50 4c 43 5f 31 5f 54
4d 52 83 8f 9a 10 00 00 11 00 4d 53 47 5f 33 5f
31 5f 50 4c 43 5f 31 5f 43 54 4c ff 8f f2 17 00
00 10 00 4c 5f 43 50 55 5f 4d 65 6d 55 73 65 44
61 74 61 c3 20 c4 1a 00 00 11 00 4d 53 47 5f 33
5f 34 5f 50 4c 43 5f 31 5f 43 54 4c ff 8f 99 1b
00 00 0e 00 4c 69 6e 6b 53 74 61 74 75 73 57 6f
72 64 c4 20 fa 1b 00 00 13 00 50 72 6f 67 72 61
6d 3a 4d 61 69 6e 50 72 6f 67 72 61 6d 68 10 20
24 00 00 09 00 4d 61 70 3a 4c 6f 63 61 6c 69 10
8d 2a 00 00 13 00 4c 5f 43 50 55 5f 4d 73 67 47
65 74 43 6f 6e 6e 55 73 65 ff 8f fc 2b 00 00 12
00 4c 5f 43 50 55 5f 4d 73 67 47 65 74 4d 65 6d
55 73 65 ff 8f 21 2c 00 00 17 00 4c 5f 43 50 55
5f 4d 73 67 47 65 74 54 72 65 6e 64 4f 62 6a 55
73 65 ff 8f 6f 30 00 00 12 00 4c 5f 43 50 55 5f
54 61 73 6b 54 69 6d 65 44 61 74 61 c4 20 68 31
00 00 07 00 54 61 73 6b 3a 54 32 70 10 53 32 00
00 15 00 44 61 74 65 54 69 6d 65 53 79 6e 63 68
52 65 71 75 65 73 74 31 c1 00 9e 32 00 00 07 00
4d 53 47 5f 54 4d 52 83 8f 81 36 00 00 0d 00 4d
65 73 73 61 67 65 5f 54 69 6d 65 72 83 8f ae 37
00 00 13 00 53 74 73 5f 53 43 50 55 5f 52 65 64
75 6e 64 61 6e 63 79 c3 00 2b 38 00 00 15 00 43
4f 4d 4d 5f 33 5f 34 5f 50 4c 43 5f 31 5f 42 75
66 66 65 72 c4 00 3d 38 00 00 03 00 58 58 58 23
82
" );
			}
			else if (start == 0x383e)
			{
				return SoftBasic.HexStringToBytes( @"
d5 00
06 00 55 3e 00 00 11 00 4d 53 47 5f 33 5f 33 5f
50 4c 43 5f 31 5f 54 4d 52 83 8f 67 41 00 00 0f
00 53 74 73 5f 4e 61 6d 65 43 68 61 73 73 69 73
c3 00 a3 41 00 00 15 00 4d 6f 64 75 6c 65 52 65
64 75 6e 64 61 6e 63 79 53 74 61 74 65 c3 00 8a
44 00 00 0b 00 53 74 73 5f 43 50 55 54 69 6d 65
c4 20 f5 45 00 00 11 00 4d 53 47 5f 33 5f 33 5f
50 4c 43 5f 31 5f 43 54 4c ff 8f 43 4c 00 00 12
00 4c 5f 43 50 55 5f 4d 73 67 53 65 74 57 69 6e
64 6f 77 ff 8f ec 50 00 00 04 00 78 78 78 32 fa
8e 7b 55 00 00 09 00 4c 6f 63 61 6c 5f 4d 53 47
ce af 24 59 00 00 11 00 5f 5f 44 45 46 56 41 4c
5f 30 30 30 30 30 39 32 30 20 89 78 5f 00 00 0c
00 53 59 53 5f 53 65 74 5f 74 69 6d 65 c4 20 85
60 00 00 09 00 44 4c 52 5f 52 41 5f 4f 4b c1 00
02 66 00 00 0b 00 50 61 72 74 6e 65 72 4d 6f 64
65 c4 00 6a 68 00 00 09 00 44 41 54 45 5f 54 49
4d 45 c4 20 34 6a 00 00 12 00 53 74 73 5f 4d 52
65 64 75 6e 64 61 6e 63 79 5f 4f 4b c1 00 a6 6a
00 00 17 00 51 75 61 6c 69 66 69 63 61 74 69 6f
6e 49 6e 50 72 6f 67 72 65 73 73 c3 00 10 6d 00
00 14 00 44 61 74 65 54 69 6d 65 53 79 6e 63 68
52 65 71 75 65 73 74 c1 00 e8 6d 00 00 10 00 50
61 72 74 6e 65 72 4b 65 79 73 77 69 74 63 68 c4
00 51 6f 00 00 0f 00 52 65 64 75 6e 64 61 6e 63
79 53 74 61 74 65 c3 00 96 6f 00 00 10 00 4c 5f
43 50 55 5f 57 69 6e 64 6f 77 54 69 6d 65 c4 00
69 72 00 00 1d 00 50 61 72 74 6e 65 72 43 68 61
73 73 69 73 52 65 64 75 6e 64 61 6e 63 79 53 74
61 74 65 c3 00 4b 7c 00 00 0c 00 50 72 6f 67 72
61 6d 3a 54 45 53 54 68 10
" );
			}
			else if (start == 0x7c4c)
			{
				return SoftBasic.HexStringToBytes( @"
d5 00
06 00 b1 81 00 00 18 00 44 61 74 65 54 69 6d 65
53 79 6e 63 68 48 6f 75 72 42 75 66 66 65 72 31
c4 00 84 90 00 00 06 00 4d 61 70 3a 73 64 69 10
a0 90 00 00 0f 00 4d 65 73 73 61 67 65 5f 54 69
6d 65 72 5f 31 83 af cb 96 00 00 12 00 50 61 72
74 6e 65 72 4d 69 6e 6f 72 46 61 75 6c 74 73 c4
00 b0 9b 00 00 04 00 78 78 78 78 23 82 17 9d 00
00 19 00 4d 53 47 5f 46 5a 5f 35 5f 31 5f 46 4d
43 53 5f 50 4c 43 5f 31 5f 54 4d 52 c4 00 7e a4
00 00 12 00 43 4f 4d 4d 5f 33 5f 31 5f 50 4c 43
5f 31 5f 54 4d 52 83 8f 14 a5 00 00 0a 00 44 4c
52 5f 53 74 61 74 75 73 c2 20 ed a6 00 00 12 00
4c 5f 43 50 55 5f 50 6f 72 74 43 61 70 79 44 61
74 61 c3 20 30 a8 00 00 0c 00 52 69 6e 67 5f 41
5f 46 61 75 6c 74 c1 00 c0 b3 00 00 19 00 4c 5f
43 50 55 5f 4d 73 67 47 65 74 55 73 65 72 54 61
73 6b 54 69 6d 65 73 ff 8f 6c b5 00 00 11 00 50
68 79 73 69 63 61 6c 43 68 61 73 73 69 73 49 44
c3 00 23 b7 00 00 0d 00 54 61 73 6b 3a 4d 61 69
6e 54 61 73 6b 70 10 3b b8 00 00 12 00 4c 5f 43
50 55 5f 54 72 65 6e 64 4f 62 6a 44 61 74 61 c3
20 0f b9 00 00 17 00 4c 5f 43 50 55 5f 4d 73 67
47 65 74 4f 53 54 61 73 6b 54 69 6d 65 73 ff 8f
b6 b9 00 00 14 00 4c 5f 43 50 55 5f 4d 73 67 47
65 74 53 63 61 6e 54 69 6d 65 ff 8f d5 bf 00 00
0b 00 55 44 49 3a 53 45 46 45 54 59 32 38 13 26
c2 00 00 03 00 54 54 54 83 af bf c2 00 00 0d 00
50 72 69 6d 61 72 79 53 74 61 74 75 73 c3 00 bd
c6 00 00 1c 00 50 61 72 74 6e 65 72 4d 6f 64 75
6c 65 52 65 64 75 6e 64 61 6e 63 79 53 74 61 74
65 c3 00
" );
			}
			else if (start == 0xc6be)
			{
				return SoftBasic.HexStringToBytes( @"
d5 00
00 00 12 c8 00 00 0f 00 43 48 31 5f 4d 53 47 5f
50 56 5f 31 5f 44 49 ff 8f 86 c8 00 00 17 00 44
61 74 65 54 69 6d 65 53 79 6e 63 68 48 6f 75 72
42 75 66 66 65 72 c4 00 c9 c8 00 00 15 00 43 4f
4d 4d 5f 33 5f 33 5f 50 4c 43 5f 31 5f 42 75 66
66 65 72 c4 00 b7 c9 00 00 08 00 4d 53 47 5f 52
69 6e 67 ff 8f 1d cb 00 00 14 00 43 6f 6d 70 61
74 69 62 69 6c 69 74 79 52 65 73 75 6c 74 73 c3
00 39 cd 00 00 0d 00 53 59 53 5f 52 65 61 64 5f
74 69 6d 65 c4 20 c7 dc 00 00 0e 00 44 50 5f 31
5f 32 5f 52 48 57 5f 31 5f 31 ca 00 e2 dd 00 00
12 00 43 4f 4d 4d 5f 33 5f 34 5f 50 4c 43 5f 31
5f 54 4d 52 83 8f 25 e0 00 00 12 00 53 74 73 5f
4d 43 50 55 5f 52 65 64 75 6e 64 61 63 79 c3 00
4c e4 00 00 0d 00 44 61 74 65 54 69 6d 65 53 79
6e 63 68 c4 20 c8 e6 00 00 12 00 43 4f 4d 4d 5f
33 5f 33 5f 50 4c 43 5f 31 5f 54 4d 52 83 8f a4
ed 00 00 11 00 4d 53 47 5f 33 5f 34 5f 50 4c 43
5f 31 5f 54 4d 52 83 8f 58 f0 00 00 13 00 4c 69
6e 6b 53 74 61 74 75 73 57 6f 72 64 5f 54 45 53
54 c2 20 dc f2 00 00 12 00 4c 5f 43 50 55 5f 53
63 61 6e 54 69 6d 65 44 61 74 61 c3 20 f3 f4 00
00 0d 00 53 52 4d 53 6c 6f 74 4e 75 6d 62 65 72
c3 00 1b fc 00 00 04 00 54 53 45 54 c4 00
" );
			}
			else
			{
				return null;
			}
		}

		private byte[] ReadByCommand( byte[] cipCore )
		{
			byte[] requestPath = ByteTransform.TransByte( cipCore, 2, cipCore[1] * 2 );
			string address = AllenBradleyHelper.ParseRequestPathCommand( requestPath );
			ushort length = BitConverter.ToUInt16( cipCore, 2 + requestPath.Length );

			return AllenBradleyHelper.PackCommandResponse( Read( address, length ).Content, true );
		}

		private byte[] WriteByMessage( byte[] cipCore )
		{
			// 先判断是否有写入的权利，没有的话，直接返回写入异常
			if (!this.EnableWrite) return AllenBradleyHelper.PackCommandResponse( null, false );

			byte[] requestPath = ByteTransform.TransByte( cipCore, 2, cipCore[1] * 2 );
			string address = AllenBradleyHelper.ParseRequestPathCommand( requestPath );

			if (address.EndsWith( ".LEN" )) return AllenBradleyHelper.PackCommandResponse( new byte[0], false );
			if (address.EndsWith( ".DATA[0]" ))
			{
				address = address.Replace( ".DATA[0]", "" );
				byte[] strValue = ByteTransform.TransByte( cipCore, 6 + requestPath.Length, cipCore.Length - 6 - requestPath.Length );
				if (Write( address, Encoding.ASCII.GetString( strValue ).TrimEnd( '\u0000' ) ).IsSuccess)
					return AllenBradleyHelper.PackCommandResponse( new byte[0], false );
				else
					return AllenBradleyHelper.PackCommandResponse( null, false );
			}

			ushort typeCode = BitConverter.ToUInt16( cipCore, 2 + requestPath.Length );
			ushort length = BitConverter.ToUInt16( cipCore, 4 + requestPath.Length );
			byte[] value = ByteTransform.TransByte( cipCore, 6 + requestPath.Length, cipCore.Length - 6 - requestPath.Length );

			if (Write( address, value ).IsSuccess)
				return AllenBradleyHelper.PackCommandResponse( new byte[0], false );
			else
				return AllenBradleyHelper.PackCommandResponse( null, false );
		}

		#endregion

		#region IDisposable Support

		/// <inheritdoc/>
		protected override void Dispose( bool disposing )
		{
			if (disposing)
			{
				simpleHybird.Dispose( );
			}
			base.Dispose( disposing );
		}

		#endregion

		#region Read Override

		/// <inheritdoc/>
		[HslMqttApi( "ReadInt16Array", "" )]
		public override OperateResult<short[]> ReadInt16( string address, ushort length ) => ByteTransformHelper.GetResultFromBytes( Read( address, length ), m => ByteTransform.TransInt16( m, 0, length ) );

		/// <inheritdoc/>
		[HslMqttApi( "ReadUInt16Array", "" )]
		public override OperateResult<ushort[]> ReadUInt16( string address, ushort length ) => ByteTransformHelper.GetResultFromBytes( Read( address, length ), m => ByteTransform.TransUInt16( m, 0, length ) );

		/// <inheritdoc/>
		[HslMqttApi( "ReadInt32Array", "" )]
		public override OperateResult<int[]> ReadInt32( string address, ushort length ) => ByteTransformHelper.GetResultFromBytes( Read( address, length ), m => ByteTransform.TransInt32( m, 0, length ) );

		/// <inheritdoc/>
		[HslMqttApi( "ReadUInt32Array", "" )]
		public override OperateResult<uint[]> ReadUInt32( string address, ushort length ) => ByteTransformHelper.GetResultFromBytes( Read( address, length ), m => ByteTransform.TransUInt32( m, 0, length ) );

		/// <inheritdoc/>
		[HslMqttApi( "ReadInt64Array", "" )]
		public override OperateResult<long[]> ReadInt64( string address, ushort length ) => ByteTransformHelper.GetResultFromBytes( Read( address, length ), m => ByteTransform.TransInt64( m, 0, length ) );

		/// <inheritdoc/>
		[HslMqttApi( "ReadUInt64Array", "" )]
		public override OperateResult<ulong[]> ReadUInt64( string address, ushort length ) => ByteTransformHelper.GetResultFromBytes( Read( address, length ), m => ByteTransform.TransUInt64( m, 0, length ) );

		/// <inheritdoc/>
		[HslMqttApi( "ReadFloatArray", "" )]
		public override OperateResult<float[]> ReadFloat( string address, ushort length ) => ByteTransformHelper.GetResultFromBytes( Read( address, length ), m => ByteTransform.TransSingle( m, 0, length ) );

		/// <inheritdoc/>
		[HslMqttApi( "ReadDoubleArray", "" )]
		public override OperateResult<double[]> ReadDouble( string address, ushort length ) => ByteTransformHelper.GetResultFromBytes( Read( address, length ), m => ByteTransform.TransDouble( m, 0, length ) );
#if !NET35 && !NET20
		/// <inheritdoc/>
		public override async Task<OperateResult<short[]>> ReadInt16Async( string address, ushort length ) => ByteTransformHelper.GetResultFromBytes( await ReadAsync( address, length ), m => ByteTransform.TransInt16( m, 0, length ) );

		/// <inheritdoc/>
		public override async Task<OperateResult<ushort[]>> ReadUInt16Async( string address, ushort length ) => ByteTransformHelper.GetResultFromBytes( await ReadAsync( address, length ), m => ByteTransform.TransUInt16( m, 0, length ) );

		/// <inheritdoc/>
		public override async Task<OperateResult<int[]>> ReadInt32Async( string address, ushort length ) => ByteTransformHelper.GetResultFromBytes( await ReadAsync( address, length ), m => ByteTransform.TransInt32( m, 0, length ) );

		/// <inheritdoc/>
		public override async Task<OperateResult<uint[]>> ReadUInt32Async( string address, ushort length ) => ByteTransformHelper.GetResultFromBytes( await ReadAsync( address, length ), m => ByteTransform.TransUInt32( m, 0, length ) );

		/// <inheritdoc/>
		public override async Task<OperateResult<long[]>> ReadInt64Async( string address, ushort length ) => ByteTransformHelper.GetResultFromBytes( await ReadAsync( address, length ), m => ByteTransform.TransInt64( m, 0, length ) );

		/// <inheritdoc/>
		public override async Task<OperateResult<ulong[]>> ReadUInt64Async( string address, ushort length ) => ByteTransformHelper.GetResultFromBytes( await ReadAsync( address, length ), m => ByteTransform.TransUInt64( m, 0, length ) );

		/// <inheritdoc/>
		public override async Task<OperateResult<float[]>> ReadFloatAsync( string address, ushort length ) => ByteTransformHelper.GetResultFromBytes( await ReadAsync( address, length ), m => ByteTransform.TransSingle( m, 0, length ) );

		/// <inheritdoc/>
		public override async Task<OperateResult<double[]>> ReadDoubleAsync( string address, ushort length ) => ByteTransformHelper.GetResultFromBytes( await ReadAsync( address, length ), m => ByteTransform.TransDouble( m, 0, length ) );
#endif
		#endregion

		#region Private Member

		private const int DataPoolLength = 65536;                          // 数据的长度
		private Dictionary<string, AllenBradleyItemValue> abValues;        // 词典
		private SimpleHybirdLock simpleHybird;                             // 词典锁
		private Random random = new Random( );                             // 随机数

		#endregion

		#region Object Override

		/// <inheritdoc/>
		public override string ToString( ) => $"AllenBradleyServer[{Port}]";

		#endregion
	}
}
