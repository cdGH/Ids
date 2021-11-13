using HslCommunication.BasicFramework;
using HslCommunication.Core;
using HslCommunication.Core.IMessage;
using HslCommunication.Core.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using HslCommunication.Reflection;
#if !NET35 && !NET20
using System.Threading.Tasks;
#endif

/*********************************************************************************************
 * 
 *    thanks: 江阴-  ∮溪风-⊙_⌒ 提供了测试的PLC
 *    thanks: 上海-null 给了大量改进的意见和说明
 *    
 *    感谢一个开源的java项目支持才使得本项目顺利开发：https://github.com/Tulioh/Ethernetip4j
 *    尽管本项目的ab类实现的功能已经超过上面的开源库很多了，还是由衷的感谢
 * 
 ***********************************************************************************************/

namespace HslCommunication.Profinet.AllenBradley
{
	/// <summary>
	/// AB PLC的数据通信类，使用CIP协议实现，适用1756，1769等型号，支持使用标签的形式进行读写操作，支持标量数据，一维数组，二维数组，三维数组等等。如果是局部变量，那么使用 Program:MainProgram.[变量名]。<br />
	/// The data communication class of AB PLC is implemented using the CIP protocol. It is suitable for 1756, 1769 and other models. 
	/// It supports reading and writing in the form of tags, scalar data, one-dimensional array, two-dimensional array, 
	/// three-dimensional array, and so on. If it is a local variable, use the Program:MainProgram.[Variable name].
	/// </summary>
	/// <remarks>
	/// thanks 江阴-  ∮溪风-⊙_⌒ help test the dll
	/// <br />
	/// thanks 上海-null 测试了这个dll
	/// <br />
	/// <br />
	/// 默认的地址就是PLC里的TAG名字，比如A，B，C；如果你需要读取的数据是一个数组，那么A就是默认的A[0]，如果想要读取偏移量为10的数据，那么地址为A[10]，
	/// 多维数组同理，使用A[10,10,10]的操作。
	/// <br />
	/// <br />
	/// 假设你读取的是局部变量，那么使用 Program:MainProgram.变量名<br />
	/// 目前适用的系列为1756 ControlLogix, 1756 GuardLogix, 1769 CompactLogix, 1769 Compact GuardLogix, 1789SoftLogix, 5069 CompactLogix, 5069 Compact GuardLogix, Studio 5000 Logix Emulate
	/// <br />
	/// <br />
	/// 如果你有个Bool数组要读取，变量名为 A, 那么读第0个位，可以通过 ReadBool("A")，但是第二个位需要使用<br />
	/// ReadBoolArray("A[0]")   // 返回32个bool长度，0-31的索引，如果我想读取32-63的位索引，就需要 ReadBoolArray("A[1]") ，以此类推。
	/// <br />
	/// <br />
	/// 地址可以携带站号信息，只要在前面加上slot=2;即可，这就是访问站号2的数据了，例如 slot=2;AAA
	/// </remarks>
	public class AllenBradleyNet : NetworkDeviceBase // <AllenBradleyMessage, RegularByteTransform>
	{
		#region Constructor

		/// <summary>
		/// Instantiate a communication object for a Allenbradley PLC protocol
		/// </summary>
		public AllenBradleyNet( )
		{
			WordLength          = 2;
			ByteTransform       = new RegularByteTransform( );
		}

		/// <summary>
		/// Instantiate a communication object for a Allenbradley PLC protocol
		/// </summary>
		/// <param name="ipAddress">PLC IpAddress</param>
		/// <param name="port">PLC Port</param>
		public AllenBradleyNet( string ipAddress, int port = 44818 ) : this( )
		{
			IpAddress          = ipAddress;
			Port               = port;
		}

		/// <inheritdoc/>
		protected override INetMessage GetNewNetMessage( ) => new AllenBradleyMessage( );

		/// <inheritdoc/>
		protected override byte[] PackCommandWithHeader( byte[] command )
		{
			return AllenBradleyHelper.PackRequestHeader( CipCommand, SessionHandle, command );
		}

		#endregion

		#region Public Properties

		/// <summary>
		/// The current session handle, which is determined by the PLC when communicating with the PLC handshake
		/// </summary>
		public uint SessionHandle { get; protected set; }

		/// <summary>
		/// Gets or sets the slot number information for the current plc, which should be set before connections
		/// </summary>
		public byte Slot { get; set; } = 0;

		/// <summary>
		/// port and slot information
		/// </summary>
		public byte[] PortSlot { get; set; }

		/// <summary>
		/// 获取或设置整个交互指令的控制码，默认为0x6F，通常不需要修改<br />
		/// Gets or sets the control code of the entire interactive instruction. The default is 0x6F, and usually does not need to be modified.
		/// </summary>
		public ushort CipCommand { get; set; } = 0x6F;

		#endregion

		#region Double Mode Override

		/// <inheritdoc/>
		protected override OperateResult InitializationOnConnect( Socket socket )
		{
			// Registering Session Information
			OperateResult<byte[]> read = ReadFromCoreServer( socket, AllenBradleyHelper.RegisterSessionHandle( ), hasResponseData: true, usePackAndUnpack: false );
			if (!read.IsSuccess) return read;

			// Check the returned status
			OperateResult check = AllenBradleyHelper.CheckResponse( read.Content );
			if (!check.IsSuccess) return check;

			// Extract session ID
			SessionHandle = ByteTransform.TransUInt32( read.Content, 4 );

			return OperateResult.CreateSuccessResult( );
		}

		/// <inheritdoc/>
		protected override OperateResult ExtraOnDisconnect( Socket socket )
		{
			// Unregister session Information
			OperateResult<byte[]> read = ReadFromCoreServer( socket, AllenBradleyHelper.UnRegisterSessionHandle( SessionHandle ), hasResponseData: true, usePackAndUnpack: false );
			if (!read.IsSuccess) return read;

			return OperateResult.CreateSuccessResult( );
		}

		#endregion

		#region Async Double Mode Override
#if !NET35 && !NET20
		/// <inheritdoc/>
		protected override async Task<OperateResult> InitializationOnConnectAsync( Socket socket )
		{
			// Registering Session Information
			OperateResult<byte[]> read = await ReadFromCoreServerAsync( socket, AllenBradleyHelper.RegisterSessionHandle( ), hasResponseData: true, usePackAndUnpack: false );
			if (!read.IsSuccess) return read;

			// Check the returned status
			OperateResult check = AllenBradleyHelper.CheckResponse( read.Content );
			if (!check.IsSuccess) return check;

			// Extract session ID
			SessionHandle = ByteTransform.TransUInt32( read.Content, 4 );

			return OperateResult.CreateSuccessResult( );
		}

		/// <inheritdoc/>
		protected override async Task<OperateResult> ExtraOnDisconnectAsync( Socket socket )
		{
			// Unregister session Information
			OperateResult<byte[]> read = await ReadFromCoreServerAsync( socket, AllenBradleyHelper.UnRegisterSessionHandle( SessionHandle ), hasResponseData: true, usePackAndUnpack: false );
			if (!read.IsSuccess) return read;

			return OperateResult.CreateSuccessResult( );
		}
#endif
		#endregion

		#region Build Command

		/// <summary>
		/// 创建一个读取标签的报文指定，标签地址可以手动动态指定slot编号，例如 slot=2;AAA<br />
		/// Build a read command bytes, The label address can manually specify the slot number dynamically, for example slot=2;AAA
		/// </summary>
		/// <param name="address">the address of the tag name</param>
		/// <param name="length">Array information, if not arrays, is 1 </param>
		/// <returns>Message information that contains the result object </returns>
		public virtual OperateResult<byte[]> BuildReadCommand( string[] address, int[] length )
		{
			if (address == null || length == null) return new OperateResult<byte[]>( "address or length is null" );
			if (address.Length != length.Length) return new OperateResult<byte[]>( "address and length is not same array" );

			try
			{
				byte slotTmp = this.Slot;
				List<byte[]> cips = new List<byte[]>( );
				for (int i = 0; i < address.Length; i++)
				{
					slotTmp = (byte)HslHelper.ExtractParameter( ref address[i], "slot", this.Slot );
					cips.Add( AllenBradleyHelper.PackRequsetRead( address[i], length[i] ) );
				}
				byte[] commandSpecificData = AllenBradleyHelper.PackCommandSpecificData( new byte[4], PackCommandService( PortSlot ?? new byte[] { 0x01, slotTmp }, cips.ToArray( ) ) );

				return OperateResult.CreateSuccessResult( commandSpecificData );
			}
			catch (Exception ex)
			{
				return new OperateResult<byte[]>( "Address Wrong:" + ex.Message );
			}
		}

		/// <summary>
		/// 创建一个读取多标签的报文<br />
		/// Build a read command bytes
		/// </summary>
		/// <param name="address">The address of the tag name </param>
		/// <returns>Message information that contains the result object </returns>
		public OperateResult<byte[]> BuildReadCommand( string[] address )
		{
			if (address == null) return new OperateResult<byte[]>( "address or length is null" );

			int[] length = new int[address.Length];
			for (int i = 0; i < address.Length; i++)
			{
				length[i] = 1;
			}

			return BuildReadCommand( address, length );
		}

		/// <summary>
		/// Create a written message instruction
		/// </summary>
		/// <param name="address">The address of the tag name </param>
		/// <param name="typeCode">Data type</param>
		/// <param name="data">Source Data </param>
		/// <param name="length">In the case of arrays, the length of the array </param>
		/// <returns>Message information that contains the result object</returns>
		protected virtual OperateResult<byte[]> BuildWriteCommand( string address, ushort typeCode, byte[] data, int length = 1 )
		{
			try
			{
				byte slotTmp = (byte)HslHelper.ExtractParameter( ref address, "slot", this.Slot );
				byte[] cip = AllenBradleyHelper.PackRequestWrite( address, typeCode, data, length );
				byte[] commandSpecificData = AllenBradleyHelper.PackCommandSpecificData( new byte[4], PackCommandService( PortSlot ?? new byte[] { 0x01, slotTmp }, cip ) );

				return OperateResult.CreateSuccessResult( commandSpecificData );
			}
			catch (Exception ex)
			{
				return new OperateResult<byte[]>( "Address Wrong:" + ex.Message );
			}
		}

		/// <summary>
		/// Create a written message instruction
		/// </summary>
		/// <param name="address">The address of the tag name </param>
		/// <param name="data">Bool Data </param>
		/// <returns>Message information that contains the result object</returns>
		public OperateResult<byte[]> BuildWriteCommand( string address, bool data )
		{
			try
			{
				byte slotTmp = (byte)HslHelper.ExtractParameter( ref address, "slot", this.Slot );
				byte[] cip = AllenBradleyHelper.PackRequestWrite( address, data );
				byte[] commandSpecificData = AllenBradleyHelper.PackCommandSpecificData( new byte[4], PackCommandService( PortSlot ?? new byte[] { 0x01, slotTmp }, cip ) );

				return OperateResult.CreateSuccessResult( commandSpecificData );
			}
			catch (Exception ex)
			{
				return new OperateResult<byte[]>( "Address Wrong:" + ex.Message );
			}
		}

		#endregion

		#region Override Read

		/// <summary>
		/// Read data information, data length for read array length information
		/// </summary>
		/// <param name="address">Address format of the node</param>
		/// <param name="length">In the case of arrays, the length of the array </param>
		/// <returns>Result data with result object </returns>
		[HslMqttApi( "ReadByteArray", "" )]
		public override OperateResult<byte[]> Read( string address, ushort length )
		{
			if (length > 1)
				return ReadSegment( address, 0, length );
			else
				return Read( new string[] { address }, new int[] { length } );
		}

		/// <summary>
		/// Bulk read Data information
		/// </summary>
		/// <param name="address">Name of the node </param>
		/// <returns>Result data with result object </returns>
		[HslMqttApi( "ReadAddress", "" )]
		public OperateResult<byte[]> Read( string[] address )
		{
			if (address == null) return new OperateResult<byte[]>( "address can not be null" );

			int[] length = new int[address.Length];
			for (int i = 0; i < length.Length; i++)
			{
				length[i] = 1;
			}

			return Read( address, length );
		}

		/// <summary>
		/// <b>[商业授权]</b> 批量读取多地址的数据信息，例如我可以读取两个标签的数据 "A","B[0]"， 长度为 [1, 5]，返回的是一整个的字节数组，需要自行解析<br />
		/// <b>[Authorization]</b> Read the data information of multiple addresses in batches. For example, I can read the data "A", "B[0]" of two tags, 
		/// the length is [1, 5], and the return is an entire byte array, and I need to do it myself Parsing
		/// </summary>
		/// <param name="address">节点的名称 -> Name of the node </param>
		/// <param name="length">如果是数组，就为数组长度 -> In the case of arrays, the length of the array </param>
		/// <returns>带有结果对象的结果数据 -> Result data with result object </returns>
		public OperateResult<byte[]> Read( string[] address, int[] length )
		{
			if(address?.Length > 1)
			{
				if (!Authorization.asdniasnfaksndiqwhawfskhfaiw( )) return new OperateResult<byte[]>( StringResources.Language.InsufficientPrivileges );
			}

			OperateResult<byte[], ushort, bool> read = ReadWithType( address, length );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( read );

			return OperateResult.CreateSuccessResult( read.Content1 );
		}

		private OperateResult<byte[], ushort, bool> ReadWithType( string[] address, int[] length )
		{
			// 指令生成 -> Instruction Generation
			OperateResult<byte[]> command = BuildReadCommand( address, length );
			if (!command.IsSuccess) return OperateResult.CreateFailedResult<byte[], ushort, bool>( command ); ;

			// 核心交互 -> Core Interactions
			OperateResult<byte[]> read = ReadFromCoreServer( command.Content );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<byte[], ushort, bool>( read ); ;

			// 检查反馈 -> Check Feedback
			OperateResult check = AllenBradleyHelper.CheckResponse( read.Content );
			if (!check.IsSuccess) return OperateResult.CreateFailedResult<byte[], ushort, bool>( check );

			// 提取数据 -> Extracting data
			return AllenBradleyHelper.ExtractActualData( read.Content, true );
		}

		/// <summary>
		/// Read Segment Data Array form plc, use address tag name
		/// </summary>
		/// <param name="address">Tag name in plc</param>
		/// <param name="startIndex">array start index, uint byte index</param>
		/// <param name="length">array length, data item length</param>
		/// <returns>Results Bytes</returns>
		[HslMqttApi( "ReadSegment", "" )]
		public OperateResult<byte[]> ReadSegment( string address, int startIndex, int length )
		{
			try
			{
				List<byte> bytesContent = new List<byte>( );
				while (true)
				{
					OperateResult<byte[]> read = ReadCipFromServer( AllenBradleyHelper.PackRequestReadSegment( address, startIndex, length ) );
					if (!read.IsSuccess) return read;

					// 提取数据 -> Extracting data
					OperateResult<byte[], ushort, bool> analysis = AllenBradleyHelper.ExtractActualData( read.Content, true );
					if (!analysis.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( analysis );

					startIndex += analysis.Content1.Length;
					bytesContent.AddRange( analysis.Content1 );

					if (!analysis.Content3) break;
				}

				return OperateResult.CreateSuccessResult( bytesContent.ToArray( ) );
			}
			catch (Exception ex)
			{
				return new OperateResult<byte[]>( "Address Wrong:" + ex.Message );
			}
		}

		private OperateResult<byte[]> ReadByCips( params byte[][] cips )
		{
			OperateResult<byte[]> read = ReadCipFromServer( cips );
			if (!read.IsSuccess) return read;

			// 提取数据 -> Extracting data
			OperateResult<byte[], ushort, bool> analysis = AllenBradleyHelper.ExtractActualData( read.Content, true );
			if (!analysis.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( analysis );

			return OperateResult.CreateSuccessResult( analysis.Content1 );
		}

		/// <summary>
		/// 使用CIP报文和服务器进行核心的数据交换
		/// </summary>
		/// <param name="cips">Cip commands</param>
		/// <returns>Results Bytes</returns>
		public OperateResult<byte[]> ReadCipFromServer( params byte[][] cips )
		{
			byte[] commandSpecificData = AllenBradleyHelper.PackCommandSpecificData( new byte[4], PackCommandService( PortSlot ?? new byte[] { 0x01, Slot }, cips.ToArray( ) ) );

			// 核心交互 -> Core Interactions
			OperateResult<byte[]> read = ReadFromCoreServer( commandSpecificData );
			if (!read.IsSuccess) return read;

			// 检查反馈 -> Check Feedback
			OperateResult check = AllenBradleyHelper.CheckResponse( read.Content );
			if (!check.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( check );

			return OperateResult.CreateSuccessResult( read.Content );
		}

		/// <summary>
		/// 使用EIP报文和服务器进行核心的数据交换
		/// </summary>
		/// <param name="eip">eip commands</param>
		/// <returns>Results Bytes</returns>
		public OperateResult<byte[]> ReadEipFromServer( params byte[][] eip )
		{
			byte[] commandSpecificData = AllenBradleyHelper.PackCommandSpecificData( eip );

			// 核心交互 -> Core Interactions
			OperateResult<byte[]> read = ReadFromCoreServer( commandSpecificData );
			if (!read.IsSuccess) return read;

			// 检查反馈 -> Check Feedback
			OperateResult check = AllenBradleyHelper.CheckResponse( read.Content );
			if (!check.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( check );

			return OperateResult.CreateSuccessResult( read.Content );
		}

		/// <summary>
		/// 读取单个的bool数据信息，如果读取的是单bool变量，就直接写变量名，如果是由int组成的bool数组的一个值，一律带"i="开头访问，例如"i=A[0]" <br />
		/// Read a single bool data information, if it is a single bool variable, write the variable name directly, 
		/// if it is a value of a bool array composed of int, it is always accessed with "i=" at the beginning, for example, "i=A[0]"
		/// </summary>
		/// <param name="address">节点的名称 -> Name of the node </param>
		/// <returns>带有结果对象的结果数据 -> Result data with result info </returns>
		[HslMqttApi( "ReadBool", "" )]
		public override OperateResult<bool> ReadBool( string address )
		{
			if ( address.StartsWith( "i=" ) )
			{
				address = address.Substring( 2 );
				address = AllenBradleyHelper.AnalysisArrayIndex( address, out int bitIndex );

				string uintIndex = (bitIndex / 32) == 0 ? $"" : $"[{bitIndex / 32}]";
				OperateResult<bool[]> read = ReadBoolArray( address + uintIndex );
				if (!read.IsSuccess) return OperateResult.CreateFailedResult<bool>( read );

				return OperateResult.CreateSuccessResult( read.Content[bitIndex % 32] );
			}
			else
			{
				OperateResult<byte[]> read = Read( address, 1 );
				if (!read.IsSuccess) return OperateResult.CreateFailedResult<bool>( read );

				return OperateResult.CreateSuccessResult( ByteTransform.TransBool( read.Content, 0 ) );
			}
		}

		/// <inheritdoc/>
		[HslMqttApi( "ReadBoolArray", "" )]
		public override OperateResult<bool[]> ReadBool( string address, ushort length )
		{
			OperateResult<byte[]> read = Read( address, 1 );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( read );

			return OperateResult.CreateSuccessResult( SoftBasic.ByteToBoolArray( read.Content, length ) );
		}

		/// <summary>
		/// 批量读取的bool数组信息，如果你有个Bool数组变量名为 A, 那么读第0个位，可以通过 ReadBool("A")，但是第二个位需要使用 
		/// ReadBoolArray("A[0]")   // 返回32个bool长度，0-31的索引，如果我想读取32-63的位索引，就需要 ReadBoolArray("A[1]") ，以此类推。<br />
		/// For batch read bool array information, if you have a Bool array variable named A, then you can read the 0th bit through ReadBool("A"), 
		/// but the second bit needs to use ReadBoolArray("A[0]" ) // Returns the length of 32 bools, the index is 0-31, 
		/// if I want to read the bit index of 32-63, I need ReadBoolArray("A[1]"), and so on.
		/// </summary>
		/// <param name="address">节点的名称 -> Name of the node </param>
		/// <returns>带有结果对象的结果数据 -> Result data with result info </returns>
		[HslMqttApi( "ReadBoolArrayAddress", "" )]
		public OperateResult<bool[]> ReadBoolArray( string address )
		{
			OperateResult<byte[]> read = Read( address, 1 );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( read );

			return OperateResult.CreateSuccessResult( ByteTransform.TransBool( read.Content, 0, read.Content.Length ) );
		}

		/// <summary>
		/// 读取PLC的byte类型的数据<br />
		/// Read the byte type of PLC data
		/// </summary>
		/// <param name="address">节点的名称 -> Name of the node </param>
		/// <returns>带有结果对象的结果数据 -> Result data with result info </returns>
		[HslMqttApi( "ReadByte", "" )]
		public OperateResult<byte> ReadByte( string address ) => ByteTransformHelper.GetResultFromArray( Read( address, 1 ) );

		/// <summary>
		/// 从PLC里读取一个指定标签名的原始数据信息及其数据类型信息<br />
		/// Read the original data information of a specified tag name and its data type information from the PLC
		/// </summary>
		/// <remarks>
		/// 数据类型的定义，可以参考 <see cref="AllenBradleyHelper"/> 的常量资源信息。
		/// </remarks>
		/// <param name="address">PLC的标签地址信息</param>
		/// <param name="length">读取的数据长度</param>
		/// <returns>包含原始数据信息及数据类型的结果对象</returns>
		public OperateResult<ushort, byte[]> ReadTag( string address, int length = 1 )
		{
			OperateResult<byte[], ushort, bool> read = ReadWithType( new string[] { address }, new int[] { length } );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<ushort, byte[]>( read );

			return OperateResult.CreateSuccessResult( read.Content2, read.Content1 );
		}

		#endregion

		#region Async Override Read
#if !NET35 && !NET20
		/// <inheritdoc cref="Read(string, ushort)"/>
		public override async Task<OperateResult<byte[]>> ReadAsync( string address, ushort length )
		{
			if (length > 1)
				return await ReadSegmentAsync( address, 0, length );
			else
				return await ReadAsync( new string[] { address }, new int[] { length } );
		}

		/// <inheritdoc cref="Read(string[])"/>
		public async Task<OperateResult<byte[]>> ReadAsync( string[] address )
		{
			if (address == null) return new OperateResult<byte[]>( "address can not be null" );

			int[] length = new int[address.Length];
			for (int i = 0; i < length.Length; i++)
			{
				length[i] = 1;
			}

			return await ReadAsync( address, length );
		}

		/// <inheritdoc cref="Read(string[], int[])"/>
		public async Task<OperateResult<byte[]>> ReadAsync( string[] address, int[] length )
		{
			if (address?.Length > 1)
			{
				if (!Authorization.asdniasnfaksndiqwhawfskhfaiw( )) return new OperateResult<byte[]>( StringResources.Language.InsufficientPrivileges );
			}

			OperateResult<byte[], ushort, bool> read = await ReadWithTypeAsync( address, length );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( read );

			return OperateResult.CreateSuccessResult( read.Content1 );
		}

		private async Task<OperateResult<byte[], ushort, bool>> ReadWithTypeAsync( string[] address, int[] length )
		{
			// 指令生成 -> Instruction Generation
			OperateResult<byte[]> command = BuildReadCommand( address, length );
			if (!command.IsSuccess) return OperateResult.CreateFailedResult<byte[], ushort, bool>( command ); ;

			// 核心交互 -> Core Interactions
			OperateResult<byte[]> read = await ReadFromCoreServerAsync( command.Content );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<byte[], ushort, bool>( read ); ;

			// 检查反馈 -> Check Feedback
			OperateResult check = AllenBradleyHelper.CheckResponse( read.Content );
			if (!check.IsSuccess) return OperateResult.CreateFailedResult<byte[], ushort, bool>( check );

			// 提取数据 -> Extracting data
			return AllenBradleyHelper.ExtractActualData( read.Content, true );
		}

		/// <inheritdoc cref="ReadSegment(string, int, int)"/>
		public async Task<OperateResult<byte[]>> ReadSegmentAsync( string address, int startIndex, int length )
		{
			try
			{
				List<byte> bytesContent = new List<byte>( );
				while (true)
				{
					OperateResult<byte[]> read = await ReadCipFromServerAsync( AllenBradleyHelper.PackRequestReadSegment( address, startIndex, length ) );
					if (!read.IsSuccess) return read;

					// 提取数据 -> Extracting data
					OperateResult<byte[], ushort, bool> analysis = AllenBradleyHelper.ExtractActualData( read.Content, true );
					if (!analysis.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( analysis );

					startIndex += analysis.Content1.Length;
					bytesContent.AddRange( analysis.Content1 );

					if (!analysis.Content3) break;
				}

				return OperateResult.CreateSuccessResult( bytesContent.ToArray( ) );
			}
			catch (Exception ex)
			{
				return new OperateResult<byte[]>( "Address Wrong:" + ex.Message );
			}
		}

		/// <inheritdoc cref="ReadCipFromServer(byte[][])"/>
		public async Task<OperateResult<byte[]>> ReadCipFromServerAsync( params byte[][] cips )
		{
			byte[] commandSpecificData = AllenBradleyHelper.PackCommandSpecificData( new byte[4], PackCommandService( PortSlot ?? new byte[] { 0x01, Slot }, cips.ToArray( ) ) );

			// 核心交互 -> Core Interactions
			OperateResult<byte[]> read = await ReadFromCoreServerAsync( commandSpecificData );
			if (!read.IsSuccess) return read;

			// 检查反馈 -> Check Feedback
			OperateResult check = AllenBradleyHelper.CheckResponse( read.Content );
			if (!check.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( check );

			return OperateResult.CreateSuccessResult( read.Content );
		}

		/// <inheritdoc cref="ReadEipFromServer(byte[][])"/>
		public async Task<OperateResult<byte[]>> ReadEipFromServerAsync( params byte[][] eip )
		{
			byte[] commandSpecificData = AllenBradleyHelper.PackCommandSpecificData( eip );

			// 核心交互 -> Core Interactions
			OperateResult<byte[]> read = await ReadFromCoreServerAsync( commandSpecificData );
			if (!read.IsSuccess) return read;

			// 检查反馈 -> Check Feedback
			OperateResult check = AllenBradleyHelper.CheckResponse( read.Content );
			if (!check.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( check );

			return OperateResult.CreateSuccessResult( read.Content );
		}

		/// <inheritdoc cref="ReadBool(string)"/>
		public async override Task<OperateResult<bool>> ReadBoolAsync( string address )
		{
			if (address.StartsWith( "i=" ))
			{
				address = address.Substring( 2 );
				address = AllenBradleyHelper.AnalysisArrayIndex( address, out int bitIndex );

				string uintIndex = (bitIndex / 32) == 0 ? $"" : $"[{bitIndex / 32}]";
				OperateResult<bool[]> read = await ReadBoolArrayAsync( address + uintIndex );
				if (!read.IsSuccess) return OperateResult.CreateFailedResult<bool>( read );

				return OperateResult.CreateSuccessResult( read.Content[bitIndex % 32] );
			}
			else
			{
				OperateResult<byte[]> read = await ReadAsync( address, 1 );
				if (!read.IsSuccess) return OperateResult.CreateFailedResult<bool>( read );

				return OperateResult.CreateSuccessResult( ByteTransform.TransBool( read.Content, 0 ) );
			}
		}

		/// <inheritdoc/>
		public async override Task<OperateResult<bool[]>> ReadBoolAsync( string address, ushort length )
		{
			OperateResult<byte[]> read = await ReadAsync( address, 1 );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( read );

			return OperateResult.CreateSuccessResult( SoftBasic.ByteToBoolArray( read.Content, length ) );
		}

		/// <inheritdoc cref="ReadBoolArray(string)"/>
		public async Task<OperateResult<bool[]>> ReadBoolArrayAsync( string address )
		{
			OperateResult<byte[]> read = await ReadAsync( address, 1 );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( read );

			return OperateResult.CreateSuccessResult( ByteTransform.TransBool( read.Content, 0, read.Content.Length ) );
		}

		/// <inheritdoc cref="ReadByte(string)"/>
		public async Task<OperateResult<byte>> ReadByteAsync( string address ) => ByteTransformHelper.GetResultFromArray( await ReadAsync( address, 1 ) );


		/// <inheritdoc cref="ReadTag(string, int)"/>
		public async Task<OperateResult<ushort, byte[]>> ReadTagAsync( string address, int length = 1 )
		{
			OperateResult<byte[], ushort, bool> read = await ReadWithTypeAsync( new string[] { address }, new int[] { length } );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<ushort, byte[]>( read );

			return OperateResult.CreateSuccessResult( read.Content2, read.Content1 );
		}

#endif
		#endregion

		#region Tag Enumerator

		/// <summary>
		/// 枚举当前的所有的变量名字，包含结构体信息，除去系统自带的名称数据信息<br />
		/// Enumerate all the current variable names, including structure information, except the name data information that comes with the system
		/// </summary>
		/// <returns>结果对象</returns>
		public OperateResult<AbTagItem[]> TagEnumerator( )
		{
			List<AbTagItem> lists = new List<AbTagItem>( );
			ushort instansAddress = 0;

			while (true)
			{
				OperateResult<byte[]> readCip = ReadCipFromServer( AllenBradleyHelper.GetEnumeratorCommand( instansAddress ) );
				if (!readCip.IsSuccess) return OperateResult.CreateFailedResult<AbTagItem[]>( readCip );

				// 提取数据 -> Extracting data
				OperateResult<byte[], ushort, bool> analysis = AllenBradleyHelper.ExtractActualData( readCip.Content, true );
				if (!analysis.IsSuccess) return OperateResult.CreateFailedResult<AbTagItem[]>( analysis );

				if (readCip.Content.Length >= 43 && BitConverter.ToUInt16( readCip.Content, 40 ) == 0xD5)
				{
					int index = 44;
					while(index < readCip.Content.Length)
					{ 
						AbTagItem td = new AbTagItem( );
						td.InstanceID = BitConverter.ToUInt32( readCip.Content, index );

						instansAddress = (ushort)(td.InstanceID + 1);
						index += 4;

						ushort nameLen = BitConverter.ToUInt16( readCip.Content, index );
						index += 2;
						td.Name = Encoding.ASCII.GetString( readCip.Content, index, nameLen );
						index += nameLen;

						// 当为标量数据的时候 SymbolType 就是类型，当为结构体的时候， SymbolType 就是地址
						td.SymbolType = BitConverter.ToUInt16( readCip.Content, index );
						index += 2;

						// 去掉系统保留的数据信息
						if ((td.SymbolType & 0x1000) != 0x1000)
							// 去掉 __ 开头的变量名称
							if (!td.Name.StartsWith( "__" ))
								lists.Add( td );
					}

					if(!analysis.Content3) return OperateResult.CreateSuccessResult( lists.ToArray( ) );
				}
				else
					return new OperateResult<AbTagItem[]>( StringResources.Language.UnknownError );
			}
		}

#if !NET35 && !NET20
		/// <inheritdoc cref="TagEnumerator"/>
		public async Task<OperateResult<AbTagItem[]>> TagEnumeratorAsync( )
		{
			List<AbTagItem> lists = new List<AbTagItem>( );
			ushort instansAddress = 0;

			while (true)
			{
				OperateResult<byte[]> readCip = await ReadCipFromServerAsync( AllenBradleyHelper.GetEnumeratorCommand( instansAddress ) );
				if (!readCip.IsSuccess) return OperateResult.CreateFailedResult<AbTagItem[]>( readCip );

				// 提取数据 -> Extracting data
				OperateResult<byte[], ushort, bool> analysis = AllenBradleyHelper.ExtractActualData( readCip.Content, true );
				if (!analysis.IsSuccess) return OperateResult.CreateFailedResult<AbTagItem[]>( analysis );

				if (readCip.Content.Length >= 43 && BitConverter.ToUInt16( readCip.Content, 40 ) == 0xD5)
				{
					int index = 44;
					while (index < readCip.Content.Length)
					{
						AbTagItem td = new AbTagItem( );
						td.InstanceID = BitConverter.ToUInt32( readCip.Content, index );

						instansAddress = (ushort)(td.InstanceID + 1);
						index += 4;

						ushort nameLen = BitConverter.ToUInt16( readCip.Content, index );
						index += 2;
						td.Name = Encoding.ASCII.GetString( readCip.Content, index, nameLen );
						index += nameLen;

						// 当为标量数据的时候 SymbolType 就是类型，当为结构体的时候， SymbolType 就是地址
						td.SymbolType = BitConverter.ToUInt16( readCip.Content, index );
						index += 2;

						// 去掉系统保留的数据信息
						if ((td.SymbolType & 0x1000) != 0x1000)
							// 去掉 __ 开头的变量名称
							if (!td.Name.StartsWith( "__" ))
								lists.Add( td );
					}

					if (!analysis.Content3) return OperateResult.CreateSuccessResult( lists.ToArray( ) );
				}
				else
					return new OperateResult<AbTagItem[]>( StringResources.Language.UnknownError );
			}
		}
#endif

		/// <summary>
		/// 枚举结构体的方法
		/// </summary>
		/// <param name="structTag">结构体的标签</param>
		/// <returns>是否成功</returns>
		[Obsolete("未测试通过")]
		public OperateResult<AbTagItem[]> StructTagEnumerator( AbTagItem structTag )
		{
			OperateResult<AbStructHandle> readStruct = ReadTagStructHandle( structTag );
			if (!readStruct.IsSuccess) return OperateResult.CreateFailedResult<AbTagItem[]>( readStruct );

			OperateResult<byte[]> read = ReadCipFromServer( AllenBradleyHelper.GetStructItemNameType( structTag.SymbolType, readStruct.Content ) );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<AbTagItem[]>( read );

			if (read.Content.Length >= 43 && read.Content[40] == 0xCC && read.Content[41] == 0x00)
			{
				var buff = BitConverter.GetBytes( structTag.SymbolType ); // 去除高4位
				buff[1] = (byte)(buff[1] & 0x0F);
				if (buff[1] >= 0x0f)
					return OperateResult.CreateSuccessResult( EnumSysStructItemType( read.Content, readStruct.Content ).ToArray( ) );
				else
					return OperateResult.CreateSuccessResult( EnumUserStructItemType( read.Content, readStruct.Content ).ToArray( ) );
			}
			else
				return new OperateResult<AbTagItem[]>(StringResources.Language.UnknownError );
		}

		private OperateResult<AbStructHandle> ReadTagStructHandle( AbTagItem structTag )
		{
			OperateResult<byte[]> readCip = ReadByCips( AllenBradleyHelper.GetStructHandleCommand( structTag.SymbolType ) );
			if (!readCip.IsSuccess) return OperateResult.CreateFailedResult<AbStructHandle>( readCip );

			if (readCip.Content.Length >= 43 && BitConverter.ToInt32( readCip.Content, 40 ) == 0x83)
			{
				AbStructHandle structHandle = new AbStructHandle( );
				structHandle.Count                          = BitConverter.ToUInt16( readCip.Content, 44 + 0 );        //返回项数
				structHandle.TemplateObjectDefinitionSize   = BitConverter.ToUInt32( readCip.Content, 44 + 6 );        //结构体定义大小
				structHandle.TemplateStructureSize          = BitConverter.ToUInt32( readCip.Content, 44 + 14 );       //使用读取标记服务读取结构时在线路上传输的字节数
				structHandle.MemberCount                    = BitConverter.ToUInt16( readCip.Content, 44 + 22 );       //结构中定义的成员数
				structHandle.StructureHandle                = BitConverter.ToUInt16( readCip.Content, 44 + 28 );       //结构体Handle（CRC???）
				return OperateResult.CreateSuccessResult( structHandle );
			}
			else
				return new OperateResult<AbStructHandle>( StringResources.Language.UnknownError );
		}

		private List<AbTagItem> EnumSysStructItemType( byte[] Struct_Item_Type_buff, AbStructHandle structHandle )
		{
			List<AbTagItem> abTagItems = new List<AbTagItem>( );
			byte[] buff_data; // 去除包头的数组
			byte[] buff_tag_type_and_name; // 结构内部数据类型信息数组
			byte[] buff_tag_item_name; // 结构内部项目名数组
			if (Struct_Item_Type_buff.Length > 41 && Struct_Item_Type_buff[40] == 0XCC && Struct_Item_Type_buff[41] == 0 && Struct_Item_Type_buff[42] == 0)
			{
				var len = Struct_Item_Type_buff.Length - 40;
				buff_data = new byte[len - 4];
				Array.Copy( Struct_Item_Type_buff, 44, buff_data, 0, len - 4 );  // 去除EIP包头，只留下CIP部分(也去除了 0x83 0x00 0x00 0x00 )
				buff_tag_type_and_name = new byte[structHandle.MemberCount * 8];
				Array.Copy( buff_data, 0, buff_tag_type_and_name, 0, structHandle.MemberCount * 8 ); // 数组上半部分分离
				buff_tag_item_name = new byte[buff_data.Length - buff_tag_type_and_name.Length + 1];
				Array.Copy( buff_data, buff_tag_type_and_name.Length - 1, buff_tag_item_name, 0, buff_data.Length - buff_tag_type_and_name.Length + 1 ); // 数组下半部分分离

				var item_type_len = structHandle.MemberCount;
				for (int n = 0; n < item_type_len; n++) // 获取item数据类型
				{
					var item = new AbTagItem( );
					int x;
					item.SymbolType = BitConverter.ToUInt16( buff_tag_type_and_name, x = 8 * n + 2 );      // 数据类型
					abTagItems.Add( item );
				}

				var string_name_addr = new List<int>( );
				for (int m = 0; m < buff_tag_item_name.Length; m++)//获取每个字符串的起始地址
				{
					if (buff_tag_item_name[m] == 0x00)
					{
						string_name_addr.Add( m );
					}
				}
				string_name_addr.Add( buff_tag_item_name.Length );//添加数据最后一位到结尾防止缺少0x00
				for (int m2 = 0; m2 < string_name_addr.Count; m2++)
				{
					if (m2 == 0)
					{
					}
					else
					{
						int string_len = 0;
						if (m2 + 1 < string_name_addr.Count)
							string_len = string_name_addr[m2 + 1] - string_name_addr[m2] - 1;
						else
							string_len = 0;
						if (string_len > 0)
							abTagItems[m2 - 1].Name = Encoding.ASCII.GetString( buff_tag_item_name, string_name_addr[m2] + 1, string_len );//获取成员名称
					}
				}
			}
			return abTagItems;
		}

		private List<AbTagItem> EnumUserStructItemType( byte[] Struct_Item_Type_buff, AbStructHandle structHandle )
		{
			List<AbTagItem> abTagItems = new List<AbTagItem>( );
			byte[] buff_data; // 去除包头的数组
			byte[] buff_tag_type_and_name; // 结构内部数据类型信息数组
			byte[] buff_tag_item_name; // 结构内部项目名数组
			bool flag_last_stop = false;
			int last_0x00 = 0; // 结构类型和标签名的分界线
			if (Struct_Item_Type_buff.Length > 41 & Struct_Item_Type_buff[40] == 0XCC & Struct_Item_Type_buff[41] == 0 & Struct_Item_Type_buff[42] == 0)
			{


				var len = Struct_Item_Type_buff.Length - 40;
				buff_data = new byte[len - 4];
				Array.ConstrainedCopy( Struct_Item_Type_buff, 44, buff_data, 0, len - 4 ); // 去除EIP包头，只留下CIP部分(也去除了 0x83 0x00 0x00 0x00 )
				for (int i = 0; i < buff_data.Length; i++) // 将数组分成两部分
				{

					if (buff_data[i] == 0x00 & !flag_last_stop)
						last_0x00 = i;

					if (buff_data[i] == (byte)0x3b && buff_data[i + 1] == (byte)0x6e) // 找到结构体名
					{
						flag_last_stop = true;
						var Struct_Name_len = i - last_0x00 - 1; // 获取名字长度
						var Struct_Name_buff = new byte[Struct_Name_len];
						Array.Copy( buff_data, (last_0x00 + 1), Struct_Name_buff, 0, Struct_Name_len ); // 截取名字
						buff_tag_type_and_name = new byte[i + 1];
						Array.Copy( buff_data, 0, buff_tag_type_and_name, 0, i + 1 );// 数组上半部分分离
						buff_tag_item_name = new byte[buff_data.Length - i - 1];
						Array.Copy( buff_data, i + 1, buff_tag_item_name, 0, buff_data.Length - i - 1 ); // 数组下半部分分离
						if ((last_0x00 + 1) % 8 == 0)
						{
							var item_type_len = (last_0x00 + 1) / 8 - 1;
							for (int n = 0; n <= item_type_len; n++) // 获取item数据类型
							{
								var item = new AbTagItem( );
								int x;
								item.SymbolType = BitConverter.ToUInt16( buff_tag_type_and_name, x = 8 * n + 2 ); //数据类型
								abTagItems.Add( item );
							}

							var string_name_addr = new List<int>( );
							for (int m = 0; m < buff_tag_item_name.Length; m++) // 获取每个字符串的起始地址
							{
								if (buff_tag_item_name[m] == 0x00)
								{
									string_name_addr.Add( m );
								}
							}

							string_name_addr.Add( buff_tag_item_name.Length ); // 添加数据最后一位到结尾防止缺少0x00
							for (int m2 = 0; m2 < string_name_addr.Count; m2++)
							{
								int string_len = 0;
								if (m2 + 1 < string_name_addr.Count)
									string_len = string_name_addr[m2 + 1] - string_name_addr[m2] - 1;
								else
									string_len = 0;
								if (string_len > 0)
									abTagItems[m2].Name = Encoding.ASCII.GetString( buff_tag_item_name, string_name_addr[m2] + 1, string_len ); // 获取成员名称

							}

						}
						break;
					}
				}
			}
			return abTagItems;
		}

		#endregion

		#region Device Override

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
		[HslMqttApi( "ReadFloatArray", "" )]
		public override OperateResult<float[]> ReadFloat( string address, ushort length ) => ByteTransformHelper.GetResultFromBytes( Read( address, length ), m => ByteTransform.TransSingle( m, 0, length ) );

		/// <inheritdoc/>
		[HslMqttApi( "ReadInt64Array", "" )]
		public override OperateResult<long[]> ReadInt64( string address, ushort length ) => ByteTransformHelper.GetResultFromBytes( Read( address, length ), m => ByteTransform.TransInt64( m, 0, length ) );

		/// <inheritdoc/>
		[HslMqttApi( "ReadUInt64Array", "" )]
		public override OperateResult<ulong[]> ReadUInt64( string address, ushort length ) => ByteTransformHelper.GetResultFromBytes( Read( address, length ), m => ByteTransform.TransUInt64( m, 0, length ) );

		/// <inheritdoc/>
		[HslMqttApi( "ReadDoubleArray", "" )]
		public override OperateResult<double[]> ReadDouble( string address, ushort length ) => ByteTransformHelper.GetResultFromBytes( Read( address, length ), m => ByteTransform.TransDouble( m, 0, length ) );

		///<inheritdoc/>
		public OperateResult<string> ReadString( string address ) => ReadString( address, 1, Encoding.UTF8 );

		/// <inheritdoc/>
		public override OperateResult<string> ReadString( string address, ushort length, Encoding encoding )
		{
			OperateResult<byte[]> read = Read( address, length );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<string>( read );

			try
			{
				if (read.Content.Length >= 6)
				{
					int strLength = ByteTransform.TransInt32( read.Content, 2 );
					return OperateResult.CreateSuccessResult( encoding.GetString( read.Content, 6, strLength ) );
				}
				else
				{
					return OperateResult.CreateSuccessResult( encoding.GetString( read.Content ) );
				}
			}
			catch(Exception ex)
			{
				return new OperateResult<string>( ex.Message + " Source: " + read.Content.ToHexString( ' ' ) );
			}
		}

		/// <inheritdoc cref="AllenBradleyHelper.ReadPlcType(IReadWriteDevice)"/>
		[HslMqttApi( Description = "获取PLC的型号信息" )]
		public OperateResult<string> ReadPlcType( ) => AllenBradleyHelper.ReadPlcType( this );

		#endregion

		#region Async Device Override
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
		public override async Task<OperateResult<float[]>> ReadFloatAsync( string address, ushort length ) => ByteTransformHelper.GetResultFromBytes( await ReadAsync( address, length ), m => ByteTransform.TransSingle( m, 0, length ) );

		/// <inheritdoc/>
		public override async Task<OperateResult<long[]>> ReadInt64Async( string address, ushort length ) => ByteTransformHelper.GetResultFromBytes( await ReadAsync( address, length ), m => ByteTransform.TransInt64( m, 0, length ) );

		/// <inheritdoc/>
		public override async Task<OperateResult<ulong[]>> ReadUInt64Async( string address, ushort length ) => ByteTransformHelper.GetResultFromBytes( await ReadAsync( address, length ), m => ByteTransform.TransUInt64( m, 0, length ) );

		/// <inheritdoc/>
		public override async Task<OperateResult<double[]>> ReadDoubleAsync( string address, ushort length ) => ByteTransformHelper.GetResultFromBytes( await ReadAsync( address, length ), m => ByteTransform.TransDouble( m, 0, length ) );

		/// <inheritdoc/>
		public async Task<OperateResult<string>> ReadStringAsync( string address ) => await ReadStringAsync( address, 1, Encoding.UTF8 );

		/// <inheritdoc/>
		public override async Task<OperateResult<string>> ReadStringAsync( string address, ushort length, Encoding encoding )
		{
			OperateResult<byte[]> read = await ReadAsync( address, length );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<string>( read );

			if (read.Content.Length >= 6)
			{
				int strLength = ByteTransform.TransInt32( read.Content, 2 );
				return OperateResult.CreateSuccessResult( encoding.GetString( read.Content, 6, strLength ) );
			}
			else
			{
				return OperateResult.CreateSuccessResult( encoding.GetString( read.Content ) );
			}
		}

		/// <inheritdoc cref="AllenBradleyHelper.ReadPlcType(IReadWriteDevice)"/>
		public async Task<OperateResult<string>> ReadPlcTypeAsync( ) => await AllenBradleyHelper.ReadPlcTypeAsync( this );

#endif
		#endregion

		#region Write Support

		/// <summary>
		/// 当前的PLC不支持该功能，需要调用 <see cref="WriteTag(string, ushort, byte[], int)"/> 方法来实现。<br />
		/// The current PLC does not support this function, you need to call the <see cref = "WriteTag (string, ushort, byte [], int)" /> method to achieve it.
		/// </summary>
		/// <param name="address">地址</param>
		/// <param name="value">值</param>
		/// <returns>写入结果值</returns>
		[HslMqttApi( "WriteByteArray", "" )]
		public override OperateResult Write( string address, byte[] value ) => new OperateResult( StringResources.Language.NotSupportedFunction + " Please refer to use WriteTag instead " );

		/// <summary>
		/// 使用指定的类型写入指定的节点数据<br />
		/// Writes the specified node data with the specified type
		/// </summary>
		/// <param name="address">节点的名称 -> Name of the node </param>
		/// <param name="typeCode">类型代码，详细参见<see cref="AllenBradleyHelper"/>上的常用字段 ->  Type code, see the commonly used Fields section on the <see cref= "AllenBradleyHelper"/> in detail</param>
		/// <param name="value">实际的数据值 -> The actual data value </param>
		/// <param name="length">如果节点是数组，就是数组长度 -> If the node is an array, it is the array length </param>
		/// <returns>是否写入成功 -> Whether to write successfully</returns>
		public virtual OperateResult WriteTag( string address, ushort typeCode, byte[] value, int length = 1 )
		{
			OperateResult<byte[]> command = BuildWriteCommand( address, typeCode, value, length );
			if (!command.IsSuccess) return command;

			OperateResult<byte[]> read = ReadFromCoreServer( command.Content );
			if (!read.IsSuccess) return read;

			OperateResult check = AllenBradleyHelper.CheckResponse( read.Content );
			if (!check.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( check );

			return AllenBradleyHelper.ExtractActualData( read.Content, false );
		}

		#endregion

		#region Async Write Support
#if !NET35 && !NET20
		/// <inheritdoc cref="Write(string, byte[])"/>
		public override async Task<OperateResult> WriteAsync( string address, byte[] value ) => await Task.Run( ( ) => Write( address, value ) );

		/// <inheritdoc cref="WriteTag(string, ushort, byte[], int)"/>
		public virtual async Task<OperateResult> WriteTagAsync( string address, ushort typeCode, byte[] value, int length = 1 )
		{
			OperateResult<byte[]> command = BuildWriteCommand( address, typeCode, value, length );
			if (!command.IsSuccess) return command;

			OperateResult<byte[]> read = await ReadFromCoreServerAsync( command.Content );
			if (!read.IsSuccess) return read;

			OperateResult check = AllenBradleyHelper.CheckResponse( read.Content );
			if (!check.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( check );

			return AllenBradleyHelper.ExtractActualData( read.Content, false );
		}
#endif
		#endregion

		#region Write Override

		/// <inheritdoc/>
		[HslMqttApi( "WriteInt16Array", "" )]
		public override OperateResult Write( string address, short[] values ) => WriteTag( address, AllenBradleyHelper.CIP_Type_Word, ByteTransform.TransByte( values ), values.Length );

		/// <inheritdoc/>
		[HslMqttApi( "WriteUInt16Array", "" )]
		public override OperateResult Write( string address, ushort[] values ) => WriteTag( address, AllenBradleyHelper.CIP_Type_UInt, ByteTransform.TransByte( values ), values.Length );

		/// <inheritdoc/>
		[HslMqttApi( "WriteInt32Array", "" )]
		public override OperateResult Write( string address, int[] values ) => WriteTag( address, AllenBradleyHelper.CIP_Type_DWord, ByteTransform.TransByte( values ), values.Length );

		/// <inheritdoc/>
		[HslMqttApi( "WriteUInt32Array", "" )]
		public override OperateResult Write( string address, uint[] values ) => WriteTag( address, AllenBradleyHelper.CIP_Type_UDint, ByteTransform.TransByte( values ), values.Length );

		/// <inheritdoc/>
		[HslMqttApi( "WriteFloatArray", "" )]
		public override OperateResult Write( string address, float[] values ) => WriteTag( address, AllenBradleyHelper.CIP_Type_Real, ByteTransform.TransByte( values ), values.Length );

		/// <inheritdoc/>
		[HslMqttApi( "WriteInt64Array", "" )]
		public override OperateResult Write( string address, long[] values ) => WriteTag( address, AllenBradleyHelper.CIP_Type_LInt, ByteTransform.TransByte( values ), values.Length );

		/// <inheritdoc/>
		[HslMqttApi( "WriteUInt64Array", "" )]
		public override OperateResult Write( string address, ulong[] values ) => WriteTag( address, AllenBradleyHelper.CIP_Type_ULint, ByteTransform.TransByte( values ), values.Length );

		/// <inheritdoc/>
		[HslMqttApi( "WriteDoubleArray", "" )]
		public override OperateResult Write( string address, double[] values ) => WriteTag( address, AllenBradleyHelper.CIP_Type_Double, ByteTransform.TransByte( values ), values.Length );

		/// <inheritdoc/>
		public override OperateResult Write( string address, string value, Encoding encoding )
		{
			if (string.IsNullOrEmpty( value )) value = string.Empty;

			byte[] data = encoding.GetBytes( value );
			OperateResult write = Write( $"{address}.LEN", data.Length );
			if (!write.IsSuccess) return write;

			byte[] buffer = SoftBasic.ArrayExpandToLengthEven( data );
			return WriteTag( $"{address}.DATA[0]", AllenBradleyHelper.CIP_Type_Byte, buffer, data.Length );
		}

		/// <summary>
		/// 写入单个Bool的数据信息。如果读取的是单bool变量，就直接写变量名，如果是bool数组的一个值，一律带下标访问，例如a[0]<br />
		/// Write the data information of a single Bool. If the read is a single bool variable, write the variable name directly, 
		/// if it is a value of the bool array, it will always be accessed with a subscript, such as a[0]
		/// </summary>
		/// <param name="address">标签的地址数据</param>
		/// <param name="value">bool数据值</param>
		/// <returns>是否写入成功</returns>
		[HslMqttApi( "WriteBool", "" )]
		public override OperateResult Write( string address, bool value )
		{
			if (Regex.IsMatch( address, @"\[[0-9]+\]$" ))
			{
				OperateResult<byte[]> command = BuildWriteCommand( address, value );
				if (!command.IsSuccess) return command;

				OperateResult<byte[]> read = ReadFromCoreServer( command.Content );
				if (!read.IsSuccess) return read;

				OperateResult check = AllenBradleyHelper.CheckResponse( read.Content );
				if (!check.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( check );

				return AllenBradleyHelper.ExtractActualData( read.Content, false );
			}
			else
			{
				return WriteTag( address, AllenBradleyHelper.CIP_Type_Bool, value ? new byte[] { 0xFF, 0xFF } : new byte[] { 0x00, 0x00 } );
			}
		}

		/// <summary>
		/// 写入Byte数据，返回是否写入成功<br />
		/// Write Byte data and return whether the writing is successful
		/// </summary>
		/// <param name="address">标签的地址数据</param>
		/// <param name="value">Byte数据</param>
		/// <returns>是否写入成功</returns>
		[HslMqttApi( "WriteByte", "" )]
		public virtual OperateResult Write( string address, byte value ) => WriteTag( address, AllenBradleyHelper.CIP_Type_Byte, new byte[] { value, 0x00 } );

		#endregion

		#region Async Write Override
#if !NET35 && !NET20
		/// <inheritdoc cref="Write(string, short[])"/>
		public override async Task<OperateResult> WriteAsync( string address, short[] values ) => await WriteTagAsync( address, AllenBradleyHelper.CIP_Type_Word, ByteTransform.TransByte( values ), values.Length );

		/// <inheritdoc cref="Write(string, ushort[])"/>
		public override async Task<OperateResult> WriteAsync( string address, ushort[] values ) => await WriteTagAsync( address, AllenBradleyHelper.CIP_Type_UInt, ByteTransform.TransByte( values ), values.Length );

		/// <inheritdoc cref="Write(string, int[])"/>
		public override async Task<OperateResult> WriteAsync( string address, int[] values ) => await WriteTagAsync( address, AllenBradleyHelper.CIP_Type_DWord, ByteTransform.TransByte( values ), values.Length );

		/// <inheritdoc cref="Write(string, uint[])"/>
		public override async Task<OperateResult> WriteAsync( string address, uint[] values ) => await WriteTagAsync( address, AllenBradleyHelper.CIP_Type_UDint, ByteTransform.TransByte( values ), values.Length );

		/// <inheritdoc cref="Write(string, float[])"/>
		public override async Task<OperateResult> WriteAsync( string address, float[] values ) => await WriteTagAsync( address, AllenBradleyHelper.CIP_Type_Real, ByteTransform.TransByte( values ), values.Length );

		/// <inheritdoc cref="Write(string, long[])"/>
		public override async Task<OperateResult> WriteAsync( string address, long[] values ) => await WriteTagAsync( address, AllenBradleyHelper.CIP_Type_LInt, ByteTransform.TransByte( values ), values.Length );

		/// <inheritdoc cref="Write(string, ulong[])"/>
		public override async Task<OperateResult> WriteAsync( string address, ulong[] values ) => await WriteTagAsync( address, AllenBradleyHelper.CIP_Type_ULint, ByteTransform.TransByte( values ), values.Length );

		/// <inheritdoc cref="Write(string, double[])"/>
		public override async Task<OperateResult> WriteAsync( string address, double[] values ) => await WriteTagAsync( address, AllenBradleyHelper.CIP_Type_Double, ByteTransform.TransByte( values ), values.Length );

		/// <inheritdoc/>
		public override async Task<OperateResult> WriteAsync( string address, string value, Encoding encoding )
		{
			if (string.IsNullOrEmpty( value )) value = string.Empty;

			byte[] data = encoding.GetBytes( value );
			OperateResult write = await WriteAsync( $"{address}.LEN", data.Length );
			if (!write.IsSuccess) return write;

			byte[] buffer = SoftBasic.ArrayExpandToLengthEven( data );
			return await WriteTagAsync( $"{address}.DATA[0]", AllenBradleyHelper.CIP_Type_Byte, buffer, data.Length );
		}

		/// <inheritdoc cref="Write(string, bool)"/>
		public override async Task<OperateResult> WriteAsync( string address, bool value )
		{
			if (Regex.IsMatch( address, @"\[[0-9]+\]$" ))
			{
				OperateResult<byte[]> command = BuildWriteCommand( address, value );
				if (!command.IsSuccess) return command;

				OperateResult<byte[]> read = await ReadFromCoreServerAsync( command.Content );
				if (!read.IsSuccess) return read;

				OperateResult check = AllenBradleyHelper.CheckResponse( read.Content );
				if (!check.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( check );

				return AllenBradleyHelper.ExtractActualData( read.Content, false );
			}
			else
			{
				return await WriteTagAsync( address, AllenBradleyHelper.CIP_Type_Bool, value ? new byte[] { 0xFF, 0xFF } : new byte[] { 0x00, 0x00 } );
			}
		}

		/// <inheritdoc cref="Write(string, byte)"/>
		public virtual async Task<OperateResult> WriteAsync( string address, byte value ) => await WriteTagAsync( address, AllenBradleyHelper.CIP_Type_Byte, new byte[] { value, 0x00 } );
#endif
		#endregion

		#region Date ReadWrite

		/// <summary>
		/// 读取指定地址的日期数据，最小日期为 1970年1月1日，当PLC的变量类型为 "Date" 和 "TimeAndDate" 时，都可以用本方法读取。<br />
		/// Read the date data of the specified address. The minimum date is January 1, 1970. When the PLC variable type is "Date" and "TimeAndDate", this method can be used to read.
		/// </summary>
		/// <param name="address">PLC里变量的地址</param>
		/// <returns>日期结果对象</returns>
		public OperateResult<DateTime> ReadDate( string address )
		{
			OperateResult<long> read = ReadInt64( address );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<DateTime>( read );

			long tick = read.Content / 100;
			DateTime dateTime = new DateTime( 1970, 1, 1 );
			return OperateResult.CreateSuccessResult( dateTime.AddTicks( tick ) );
		}

		/// <summary>
		/// 使用日期格式（Date）将指定的数据写入到指定的地址里，PLC的地址类型变量必须为 "Date"，否则写入失败。<br />
		/// Use the date format (Date) to write the specified data to the specified address. The PLC address type variable must be "Date", otherwise the writing will fail.
		/// </summary>
		/// <param name="address">PLC里变量的地址</param>
		/// <param name="date">时间信息</param>
		/// <returns>是否写入成功</returns>
		public OperateResult WriteDate( string address, DateTime date )
		{
			long tick = (date.Date - new DateTime( 1970, 1, 1 )).Ticks * 100;
			return WriteTag( address, AllenBradleyHelper.CIP_Type_DATE, ByteTransform.TransByte( tick ) );
		}

		/// <inheritdoc cref="WriteDate(string, DateTime)"/>
		public OperateResult WriteTimeAndDate( string address, DateTime date )
		{
			long tick = (date - new DateTime( 1970, 1, 1 )).Ticks * 100;
			return WriteTag( address, AllenBradleyHelper.CIP_Type_TimeAndDate, ByteTransform.TransByte( tick ) );
		}

		/// <summary>
		/// 读取指定地址的时间数据，最小时间为 0，如果获取秒，可以访问 <see cref="TimeSpan.TotalSeconds"/>，当PLC的变量类型为 "Time" 和 "TimeOfDate" 时，都可以用本方法读取。<br />
		/// Read the time data of the specified address. The minimum time is 0. If you get seconds, you can access <see cref="TimeSpan.TotalSeconds"/>. 
		/// When the PLC variable type is "Time" and "TimeOfDate", you can use this Method to read.
		/// </summary>
		/// <param name="address">PLC里变量的地址</param>
		/// <returns>时间的结果对象</returns>
		public OperateResult<TimeSpan> ReadTime( string address )
		{
			OperateResult<long> read = ReadInt64( address );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<TimeSpan>( read );

			long tick = read.Content / 100;
			return OperateResult.CreateSuccessResult( TimeSpan.FromTicks( tick ) );
		}

		/// <summary>
		/// 使用时间格式（TIME）将时间数据写入到PLC中指定的地址里去，PLC的地址类型变量必须为 "TIME"，否则写入失败。<br />
		/// Use the time format (TIME) to write the time data to the address specified in the PLC. The PLC address type variable must be "TIME", otherwise the writing will fail.
		/// </summary>
		/// <param name="address">PLC里变量的地址</param>
		/// <param name="time">时间参数变量</param>
		/// <returns>是否写入成功</returns>
		public OperateResult WriteTime( string address, TimeSpan time )
		{
			return WriteTag( address, AllenBradleyHelper.CIP_Type_TIME, ByteTransform.TransByte( time.Ticks * 100 ) );
		}

		/// <summary>
		/// 使用时间格式（TimeOfDate）将时间数据写入到PLC中指定的地址里去，PLC的地址类型变量必须为 "TimeOfDate"，否则写入失败。<br />
		/// Use the time format (TimeOfDate) to write the time data to the address specified in the PLC. The PLC address type variable must be "TimeOfDate", otherwise the writing will fail.
		/// </summary>
		/// <param name="address">PLC里变量的地址</param>
		/// <param name="timeOfDate">时间参数变量</param>
		/// <returns>是否写入成功</returns>
		public OperateResult WriteTimeOfDate( string address, TimeSpan timeOfDate )
		{
			return WriteTag( address, AllenBradleyHelper.CIP_Type_TimeOfDate, ByteTransform.TransByte( timeOfDate.Ticks * 100 ) );
		}
#if !NET20 && !NET35
		/// <inheritdoc cref="ReadDate(string)"/>
		public async Task<OperateResult<DateTime>> ReadDateAsync( string address )
		{
			OperateResult<long> read = await ReadInt64Async( address );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<DateTime>( read );

			long tick = read.Content / 100;
			DateTime dateTime = new DateTime( 1970, 1, 1 );
			return OperateResult.CreateSuccessResult( dateTime.AddTicks( tick ) );
		}
		/// <inheritdoc cref="WriteDate(string, DateTime)"/>
		public async Task<OperateResult> WriteDateAsync( string address, DateTime date )
		{
			long tick = (date.Date - new DateTime( 1970, 1, 1 )).Ticks * 100;
			return await WriteTagAsync( address, AllenBradleyHelper.CIP_Type_DATE, ByteTransform.TransByte( tick ) );
		}
		/// <inheritdoc cref="WriteTimeAndDate(string, DateTime)"/>
		public async Task<OperateResult> WriteTimeAndDateAsync( string address, DateTime date )
		{
			long tick = (date - new DateTime( 1970, 1, 1 )).Ticks * 100;
			return await WriteTagAsync( address, AllenBradleyHelper.CIP_Type_TimeAndDate, ByteTransform.TransByte( tick ) );
		}
		/// <inheritdoc cref="ReadTime(string)"/>
		public async Task<OperateResult<TimeSpan>> ReadTimeAsync( string address )
		{
			OperateResult<long> read = await ReadInt64Async( address );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<TimeSpan>( read );

			long tick = read.Content / 100;
			return OperateResult.CreateSuccessResult( TimeSpan.FromTicks( tick ) );
		}
		/// <inheritdoc cref="WriteTime(string, TimeSpan)"/>
		public async Task<OperateResult> WriteTimeAsync( string address, TimeSpan time )
		{
			return await WriteTagAsync( address, AllenBradleyHelper.CIP_Type_TIME, ByteTransform.TransByte( time.Ticks * 100 ) );
		}
		/// <inheritdoc cref="WriteTimeOfDate(string, TimeSpan)"/>
		public async Task<OperateResult> WriteTimeOfDateAsync( string address, TimeSpan timeOfDate )
		{
			return await WriteTagAsync( address, AllenBradleyHelper.CIP_Type_TimeOfDate, ByteTransform.TransByte( timeOfDate.Ticks * 100 ) );
		}
#endif
		#endregion

		#region PackCommandService

		/// <inheritdoc cref="AllenBradleyHelper.PackCommandService(byte[], byte[][])"/>
		protected virtual byte[] PackCommandService( byte[] portSlot, params byte[][] cips ) => AllenBradleyHelper.PackCommandService( portSlot, cips );

		#endregion

		#region Object Override

		/// <inheritdoc/>
		public override string ToString( ) => $"AllenBradleyNet[{IpAddress}:{Port}]";

		#endregion
	}
}
