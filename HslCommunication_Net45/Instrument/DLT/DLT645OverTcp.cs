using System;
using System.Linq;
using System.Text;
using HslCommunication.BasicFramework;
using HslCommunication.Core;
using HslCommunication.Core.Net;
using HslCommunication.Core.IMessage;
#if !NET35 && !NET20
using System.Threading.Tasks;
#endif
namespace HslCommunication.Instrument.DLT
{
	/// <summary>
	/// 基于多功能电能表通信协议实现的通讯类，参考的文档是DLT645-2007，主要实现了对电表数据的读取和一些功能方法，
	/// 在点对点模式下，需要在连接后调用 <see cref="ReadAddress"/> 方法，数据标识格式为 00-00-00-00，具体参照文档手册。<br />
	/// The communication type based on the communication protocol of the multifunctional electric energy meter. 
	/// The reference document is DLT645-2007, which mainly realizes the reading of the electric meter data and some functional methods. 
	/// In the point-to-point mode, you need to call <see cref="ReadAddress" /> method after connect the device.
	/// the data identification format is 00-00-00-00, refer to the documentation manual for details.
	/// </summary>
	/// <remarks>
	/// 如果一对多的模式，地址可以携带地址域访问，例如 "s=2;00-00-00-00"，主要使用 <see cref="ReadDouble(string, ushort)"/> 方法来读取浮点数，
	/// <see cref="NetworkDeviceBase.ReadString(string, ushort)"/> 方法来读取字符串
	/// </remarks>
	public class DLT645OverTcp : NetworkDeviceBase
	{
		#region Constructor

		/// <summary>
		/// 指定IP地址，端口，地址域，密码，操作者代码来实例化一个对象<br />
		/// Specify the IP address, port, address field, password, and operator code to instantiate an object
		/// </summary>
		/// <param name="ipAddress">TcpServer的IP地址</param>
		/// <param name="port">TcpServer的端口</param>
		/// <param name="station">设备的站号信息</param>
		/// <param name="password">密码，写入的时候进行验证的信息</param>
		/// <param name="opCode">操作者代码</param>
		public DLT645OverTcp( string ipAddress, int port = 502, string station = "1", string password = "", string opCode = "" )
		{
			this.IpAddress       = ipAddress;
			this.Port            = port;
			base.WordLength      = 1;
			base.ByteTransform   = new ReverseWordTransform( );
			this.station         = station;
			this.password        = string.IsNullOrEmpty( password ) ? "00000000" : password;
			this.opCode          = string.IsNullOrEmpty( opCode ) ? "00000000" : opCode;
		}

		/// <inheritdoc/>
		protected override INetMessage GetNewNetMessage( ) => new DLT645Message( );

		#endregion

		#region Public Method

		/// <inheritdoc cref="DLT645.ActiveDeveice"/>
		public OperateResult ActiveDeveice( ) => ReadFromCoreServer( new byte[] { 0xFE, 0xFE, 0xFE, 0xFE }, false );

		private OperateResult<byte[]> ReadWithAddress( string address, byte[] dataArea )
		{
			OperateResult<byte[]> command = DLT645.BuildEntireCommand( address, DLTControl.ReadData, dataArea );
			if (!command.IsSuccess) return command;

			OperateResult<byte[]> read = ReadFromCoreServer( command.Content );
			if (!read.IsSuccess) return read;

			OperateResult check = DLT645.CheckResponse( read.Content );
			if (!check.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( check );

			if (read.Content.Length < 16) return OperateResult.CreateSuccessResult( new byte[0] );
			return OperateResult.CreateSuccessResult( read.Content.SelectMiddle( 14, read.Content.Length - 16 ) );
		}

		/// <inheritdoc cref="DLT645.Read(string, ushort)"/>
		public override OperateResult<byte[]> Read( string address, ushort length )
		{
			OperateResult<string, byte[]> analysis = DLT645.AnalysisBytesAddress( address, this.station, length );
			if (!analysis.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( analysis );

			return ReadWithAddress( analysis.Content1, analysis.Content2 );
		}

		/// <inheritdoc/>
		public override OperateResult<double[]> ReadDouble(string address, ushort length)
		{
			OperateResult<string, byte[]> analysis = DLT645.AnalysisBytesAddress( address, this.station, length );
			if (!analysis.IsSuccess) return OperateResult.CreateFailedResult<double[]>( analysis );

			OperateResult<byte[]> read = ReadWithAddress( analysis.Content1, analysis.Content2 );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<double[]>( read );

			return DLTTransform.TransDoubleFromDLt( read.Content, length, DLT645.GetFormatWithDataArea( analysis.Content2 ) );
		}

		/// <inheritdoc/>
		public override OperateResult<string> ReadString( string address, ushort length, Encoding encoding )
		{
			OperateResult<byte[]> read = Read( address, 1 );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<string>( read );

			return DLTTransform.TransStringFromDLt( read.Content, length );
		}

#if !NET35 && !NET20

		/// <inheritdoc cref="DLT645.ActiveDeveice"/>
		public async Task<OperateResult> ActiveDeveiceAsync( ) => await ReadFromCoreServerAsync( new byte[] { 0xFE, 0xFE, 0xFE, 0xFE }, false );

		private async Task<OperateResult<byte[]>> ReadWithAddressAsync( string address, byte[] dataArea )
		{
			OperateResult<byte[]> command = DLT645.BuildEntireCommand( address, DLTControl.ReadData, dataArea );
			if (!command.IsSuccess) return command;

			OperateResult<byte[]> read = await ReadFromCoreServerAsync( command.Content );
			if (!read.IsSuccess) return read;

			OperateResult check = DLT645.CheckResponse( read.Content );
			if (!check.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( check );

			if (read.Content.Length < 16) return OperateResult.CreateSuccessResult( new byte[0] );
			return OperateResult.CreateSuccessResult( read.Content.SelectMiddle( 14, read.Content.Length - 16 ) );
		}

		/// <inheritdoc cref="DLT645.Read(string, ushort)"/>
		public async override Task<OperateResult<byte[]>> ReadAsync( string address, ushort length )
		{
			OperateResult<string, byte[]> analysis = DLT645.AnalysisBytesAddress( address, this.station, length );
			if (!analysis.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( analysis );

			return await ReadWithAddressAsync( analysis.Content1, analysis.Content2 );
		}

		/// <inheritdoc cref="ReadDouble(string, ushort)"/>
		public async override Task<OperateResult<double[]>> ReadDoubleAsync( string address, ushort length )
		{
			OperateResult<string, byte[]> analysis = DLT645.AnalysisBytesAddress( address, this.station, length );
			if (!analysis.IsSuccess) return OperateResult.CreateFailedResult<double[]>( analysis );

			OperateResult<byte[]> read = await ReadWithAddressAsync( analysis.Content1, analysis.Content2 );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<double[]>( read );

			return DLTTransform.TransDoubleFromDLt( read.Content, length, DLT645.GetFormatWithDataArea( analysis.Content2 ) );
		}

		/// <inheritdoc/>
		public async override Task<OperateResult<string>> ReadStringAsync( string address, ushort length, Encoding encoding )
		{
			OperateResult<byte[]> read = await ReadAsync( address, 1 );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<string>( read );

			return DLTTransform.TransStringFromDLt( read.Content, length );
		}
#endif
		/// <inheritdoc cref="DLT645.Write(string, byte[])"/>
		public override OperateResult Write( string address, byte[] value )
		{
			OperateResult<string, byte[]> analysis = DLT645.AnalysisBytesAddress( address, this.station );
			if (!analysis.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( analysis );

			byte[] content = SoftBasic.SpliceArray<byte>( analysis.Content2, password.ToHexBytes( ), opCode.ToHexBytes( ), value );

			OperateResult<byte[]> command = DLT645.BuildEntireCommand( analysis.Content1, DLTControl.WriteAddress, content );
			if (!command.IsSuccess) return command;
			OperateResult<byte[]> read = ReadFromCoreServer( command.Content );
			if (!read.IsSuccess) return read;

			return DLT645.CheckResponse( read.Content );
		}

		/// <inheritdoc cref="DLT645.ReadAddress"/>
		public OperateResult<string> ReadAddress( )
		{
			OperateResult<byte[]> command = DLT645.BuildEntireCommand( "AAAAAAAAAAAA", DLTControl.ReadAddress, null );
			if (!command.IsSuccess) return OperateResult.CreateFailedResult<string>( command );

			OperateResult<byte[]> read = base.ReadFromCoreServer( command.Content );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<string>( read );

			OperateResult check = DLT645.CheckResponse( read.Content );
			if (!check.IsSuccess) return OperateResult.CreateFailedResult<string>( check );

			this.station = read.Content.SelectMiddle( 1, 6 ).Reverse( ).ToArray( ).ToHexString( );
			return OperateResult.CreateSuccessResult( read.Content.SelectMiddle( 1, 6 ).Reverse( ).ToArray( ).ToHexString( ) );
		}

		/// <inheritdoc cref="DLT645.WriteAddress(string)"/>
		public OperateResult WriteAddress(string address)
		{
			OperateResult<byte[]> add = DLT645.GetAddressByteFromString(address);
			if (!add.IsSuccess) return add;

			OperateResult<byte[]> command = DLT645.BuildEntireCommand("AAAAAAAAAAAA", DLTControl.WriteAddress, add.Content);
			if (!command.IsSuccess) return command;

			OperateResult<byte[]> read = ReadFromCoreServer(command.Content);
			OperateResult check = DLT645.CheckResponse(read.Content);
			if (!check.IsSuccess) return check;

			if (SoftBasic.IsTwoBytesEquel(read.Content.SelectMiddle(1, 6), DLT645.GetAddressByteFromString(address).Content))
				return OperateResult.CreateSuccessResult();
			else
				return new OperateResult(StringResources.Language.DLTErrorWriteReadCheckFailed);
		}

		/// <inheritdoc cref="DLT645.BroadcastTime(DateTime)"/>
		public OperateResult BroadcastTime(DateTime dateTime)
		{
			string hex = $"{dateTime.Second:D2}{dateTime.Minute:D2}{dateTime.Hour:D2}{dateTime.Day:D2}{dateTime.Month:D2}{dateTime.Year % 100:D2}";

			OperateResult<byte[]> command = DLT645.BuildEntireCommand("999999999999", DLTControl.Broadcast, hex.ToHexBytes());
			if (!command.IsSuccess) return command;

			return ReadFromCoreServer(command.Content, false);
		}

		/// <inheritdoc cref="DLT645.FreezeCommand(string)"/>
		public OperateResult FreezeCommand( string dataArea )
		{
			OperateResult<string, byte[]> analysis = DLT645.AnalysisBytesAddress( dataArea, this.station );
			if (!analysis.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( analysis );

			OperateResult<byte[]> command = DLT645.BuildEntireCommand( analysis.Content1, DLTControl.FreezeCommand, analysis.Content2 );
			if (!command.IsSuccess) return command;

			if (analysis.Content1 == "999999999999")
			{
				// 广播操作
				return ReadFromCoreServer( command.Content, false );
			}
			else
			{
				// 点对点操作
				OperateResult<byte[]> read = ReadFromCoreServer( command.Content );
				if (!read.IsSuccess) return read;

				return DLT645.CheckResponse( read.Content );
			}
		}

		/// <inheritdoc cref="DLT645.ChangeBaudRate(string)"/>
		public OperateResult ChangeBaudRate( string baudRate )
		{
			OperateResult<string, int> analysis = DLT645.AnalysisIntegerAddress( baudRate, this.station );
			if (!analysis.IsSuccess) return analysis;

			byte code = 0x00;
			switch (analysis.Content2)
			{
				case 600:   code = 0x02; break;
				case 1200:  code = 0x04; break;
				case 2400:  code = 0x08; break;
				case 4800:  code = 0x10; break;
				case 9600:  code = 0x20; break;
				case 19200: code = 0x40; break;
				default: return new OperateResult( StringResources.Language.NotSupportedFunction );
			}

			OperateResult<byte[]> command = DLT645.BuildEntireCommand( analysis.Content1, DLTControl.ChangeBaudRate, new byte[] { code } );
			if (!command.IsSuccess) return command;

			OperateResult<byte[]> read = ReadFromCoreServer( command.Content );
			if (!read.IsSuccess) return read;

			OperateResult check = DLT645.CheckResponse( read.Content );
			if (!check.IsSuccess) return check;

			if (read.Content[10] == code)
				return OperateResult.CreateSuccessResult( );
			else
				return new OperateResult( StringResources.Language.DLTErrorWriteReadCheckFailed );
		}

#if !NET20 && !NET35
		/// <inheritdoc cref="Write(string, byte[])"/>
		public async override Task<OperateResult> WriteAsync( string address, byte[] value )
		{
			OperateResult<string, byte[]> analysis = DLT645.AnalysisBytesAddress( address, this.station );
			if (!analysis.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( analysis );

			byte[] content = SoftBasic.SpliceArray( analysis.Content2, password.ToHexBytes( ), opCode.ToHexBytes( ), value );

			OperateResult<byte[]> command = DLT645.BuildEntireCommand( analysis.Content1, DLTControl.WriteAddress, content );
			if (!command.IsSuccess) return command;
			OperateResult<byte[]> read = await ReadFromCoreServerAsync( command.Content );
			if (!read.IsSuccess) return read;

			return DLT645.CheckResponse( read.Content );
		}

		/// <inheritdoc cref="DLT645.ReadAddress"/>
		public async Task<OperateResult<string>> ReadAddressAsync( )
		{
			OperateResult<byte[]> command = DLT645.BuildEntireCommand( "AAAAAAAAAAAA", DLTControl.ReadAddress, null );
			if (!command.IsSuccess) return OperateResult.CreateFailedResult<string>( command );

			OperateResult<byte[]> read = await ReadFromCoreServerAsync( command.Content );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<string>( read );

			OperateResult check = DLT645.CheckResponse( read.Content );
			if (!check.IsSuccess) return OperateResult.CreateFailedResult<string>( check );

			this.station = read.Content.SelectMiddle( 1, 6 ).Reverse( ).ToArray( ).ToHexString( );
			return OperateResult.CreateSuccessResult( read.Content.SelectMiddle( 1, 6 ).Reverse( ).ToArray( ).ToHexString( ) );
		}

		/// <inheritdoc cref="DLT645.WriteAddress(string)"/>
		public async Task<OperateResult> WriteAddressAsync( string address )
		{
			OperateResult<byte[]> add = DLT645.GetAddressByteFromString( address );
			if (!add.IsSuccess) return add;

			OperateResult<byte[]> command = DLT645.BuildEntireCommand( "AAAAAAAAAAAA", DLTControl.WriteAddress, add.Content );
			if (!command.IsSuccess) return command;

			OperateResult<byte[]> read = await ReadFromCoreServerAsync( command.Content );
			OperateResult check = DLT645.CheckResponse( read.Content );
			if (!check.IsSuccess) return check;

			if (SoftBasic.IsTwoBytesEquel( read.Content.SelectMiddle( 1, 6 ), DLT645.GetAddressByteFromString( address ).Content ))
				return OperateResult.CreateSuccessResult( );
			else
				return new OperateResult( StringResources.Language.DLTErrorWriteReadCheckFailed );
		}

		/// <inheritdoc cref="DLT645.BroadcastTime(DateTime)"/>
		public async Task<OperateResult> BroadcastTimeAsync( DateTime dateTime )
		{
			string hex = $"{dateTime.Second:D2}{dateTime.Minute:D2}{dateTime.Hour:D2}{dateTime.Day:D2}{dateTime.Month:D2}{dateTime.Year % 100:D2}";

			OperateResult<byte[]> command = DLT645.BuildEntireCommand( "999999999999", DLTControl.Broadcast, hex.ToHexBytes( ) );
			if (!command.IsSuccess) return command;

			return await ReadFromCoreServerAsync( command.Content, false );
		}

		/// <inheritdoc cref="DLT645.FreezeCommand(string)"/>
		public async Task<OperateResult> FreezeCommandAsync( string dataArea )
		{
			OperateResult<string, byte[]> analysis = DLT645.AnalysisBytesAddress( dataArea, this.station );
			if (!analysis.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( analysis );

			OperateResult<byte[]> command = DLT645.BuildEntireCommand( analysis.Content1, DLTControl.FreezeCommand, analysis.Content2 );
			if (!command.IsSuccess) return command;

			if (analysis.Content1 == "999999999999")
			{
				// 广播操作
				return await ReadFromCoreServerAsync( command.Content, false );
			}
			else
			{
				// 点对点操作
				OperateResult<byte[]> read = await ReadFromCoreServerAsync( command.Content );
				if (!read.IsSuccess) return read;

				return DLT645.CheckResponse( read.Content );
			}
		}

		/// <inheritdoc cref="DLT645.ChangeBaudRate(string)"/>
		public async Task<OperateResult> ChangeBaudRateAsync( string baudRate )
		{
			OperateResult<string, int> analysis = DLT645.AnalysisIntegerAddress( baudRate, this.station );
			if (!analysis.IsSuccess) return analysis;

			byte code = 0x00;
			switch (analysis.Content2)
			{
				case 600:   code = 0x02; break;
				case 1200:  code = 0x04; break;
				case 2400:  code = 0x08; break;
				case 4800:  code = 0x10; break;
				case 9600:  code = 0x20; break;
				case 19200: code = 0x40; break;
				default: return new OperateResult( StringResources.Language.NotSupportedFunction );
			}

			OperateResult<byte[]> command = DLT645.BuildEntireCommand( analysis.Content1, DLTControl.ChangeBaudRate, new byte[] { code } );
			if (!command.IsSuccess) return command;

			OperateResult<byte[]> read = await ReadFromCoreServerAsync( command.Content );
			if (!read.IsSuccess) return read;

			OperateResult check = DLT645.CheckResponse( read.Content );
			if (!check.IsSuccess) return check;

			if (read.Content[10] == code)
				return OperateResult.CreateSuccessResult( );
			else
				return new OperateResult( StringResources.Language.DLTErrorWriteReadCheckFailed );
		}
#endif
		#endregion

		#region Public Property

		/// <inheritdoc cref="DLT645.Station"/>
		public string Station { get => this.station; set => this.station = value; }

		#endregion

		#region Private Member

		private string station = "1";                  // 地址域信息
		private string password = "00000000";          // 密码
		private string opCode = "00000000";            // 操作者代码

		#endregion

		#region Object Override

		/// <inheritdoc/>
		public override string ToString() => $"DLT645OverTcp[{IpAddress}:{Port}]";

		#endregion

	}
}
