using System;
using System.Collections.Generic;
using System.Linq;
using HslCommunication.Core.Net;
using System.Text;
using HslCommunication.Reflection;
#if !NET20 && !NET35
using System.Threading.Tasks;
#endif

namespace HslCommunication.Profinet.Keyence
{
	/// <summary>
	/// 基恩士的SR2000的扫码设备，可以进行简单的交互
	/// </summary>
	/// <remarks>
	/// 当使用 "LON","LOFF","PRON","PROFF" 命令时，在发送时和发生错误时，将不会收到扫码设备的回发命令，而是输出读取结果。
	/// 如果也希望获取上述命令的响应时，请在以下位置进行设置。[设置列表]-[其他]-"指定基本命令的响应字符串"
	/// </remarks>
	public class KeyenceSR2000SeriesTcp : NetworkDoubleBase, IKeyenceSR2000Series
	{
		#region Constructor

		/// <summary>
		/// 实例化基恩士的SR2000的扫码设备通讯对象<br />
		/// Instantiate keyence's SR2000 scan code device communication object
		/// </summary>
		public KeyenceSR2000SeriesTcp( ) : base( ) { receiveTimeOut = 10_000; SleepTime = 20; }

		/// <summary>
		/// 指定ip地址及端口号来实例化一个基恩士的SR2000的扫码设备通讯对象<br />
		/// Specify the ip address and port number to instantiate a keyence SR2000 scan code device communication object
		/// </summary>
		/// <param name="ipAddress">PLC的Ip地址</param>
		/// <param name="port">PLC的端口</param>
		public KeyenceSR2000SeriesTcp( string ipAddress, int port )
		{
			IpAddress = ipAddress;
			Port = port;
			receiveTimeOut = 10_000;
			SleepTime = 20;
		}

		#endregion

		/// <inheritdoc cref="KeyenceSR2000Helper.ReadBarcode(Func{byte[], OperateResult{byte[]}})"/>
		[HslMqttApi]
		public OperateResult<string> ReadBarcode( ) => KeyenceSR2000Helper.ReadBarcode( ReadFromCoreServer );

		/// <inheritdoc cref="KeyenceSR2000Helper.Reset(Func{byte[], OperateResult{byte[]}})"/>
		[HslMqttApi]
		public OperateResult Reset( ) => KeyenceSR2000Helper.Reset( ReadFromCoreServer );

		/// <inheritdoc cref="KeyenceSR2000Helper.OpenIndicator(Func{byte[], OperateResult{byte[]}})"/>
		[HslMqttApi]
		public OperateResult OpenIndicator( ) => KeyenceSR2000Helper.OpenIndicator( ReadFromCoreServer );

		/// <inheritdoc cref="KeyenceSR2000Helper.CloseIndicator(Func{byte[], OperateResult{byte[]}})"/>
		[HslMqttApi]
		public OperateResult CloseIndicator( ) => KeyenceSR2000Helper.CloseIndicator( ReadFromCoreServer );

		/// <inheritdoc cref="KeyenceSR2000Helper.ReadVersion(Func{byte[], OperateResult{byte[]}})"/>
		[HslMqttApi]
		public OperateResult<string> ReadVersion( ) => KeyenceSR2000Helper.ReadVersion( ReadFromCoreServer );

		/// <inheritdoc cref="KeyenceSR2000Helper.ReadCommandState(Func{byte[], OperateResult{byte[]}})"/>
		[HslMqttApi]
		public OperateResult<string> ReadCommandState( ) => KeyenceSR2000Helper.ReadCommandState( ReadFromCoreServer );

		/// <inheritdoc cref="KeyenceSR2000Helper.ReadErrorState(Func{byte[], OperateResult{byte[]}})"/>
		[HslMqttApi]
		public OperateResult<string> ReadErrorState( ) => KeyenceSR2000Helper.ReadErrorState( ReadFromCoreServer );

		/// <inheritdoc cref="KeyenceSR2000Helper.CheckInput(int, Func{byte[], OperateResult{byte[]}})"/>
		[HslMqttApi]
		public OperateResult<bool> CheckInput( int number ) => KeyenceSR2000Helper.CheckInput( number, ReadFromCoreServer );

		/// <inheritdoc cref="KeyenceSR2000Helper.SetOutput(int, bool, Func{byte[], OperateResult{byte[]}})"/>
		[HslMqttApi]
		public OperateResult SetOutput( int number, bool value ) => KeyenceSR2000Helper.SetOutput( number, value, ReadFromCoreServer );

		/// <inheritdoc cref="KeyenceSR2000Helper.ReadRecord(Func{byte[], OperateResult{byte[]}})"/>
		[HslMqttApi]
		public OperateResult<int[]> ReadRecord( ) => KeyenceSR2000Helper.ReadRecord( ReadFromCoreServer );

		/// <inheritdoc cref="KeyenceSR2000Helper.Lock(Func{byte[], OperateResult{byte[]}})"/>
		[HslMqttApi]
		public OperateResult Lock( ) => KeyenceSR2000Helper.Lock( ReadFromCoreServer );

		/// <inheritdoc cref="KeyenceSR2000Helper.UnLock(Func{byte[], OperateResult{byte[]}})"/>
		[HslMqttApi]
		public OperateResult UnLock( ) => KeyenceSR2000Helper.UnLock( ReadFromCoreServer );

		/// <inheritdoc cref="KeyenceSR2000Helper.ReadCustomer(string, Func{byte[], OperateResult{byte[]}})"/>
		[HslMqttApi]
		public OperateResult<string> ReadCustomer( string command ) => KeyenceSR2000Helper.ReadCustomer( command, ReadFromCoreServer );

#if !NET20 && !NET35

#endif
		/// <inheritdoc/>
		public override string ToString( ) => $"KeyenceSR2000SeriesTcp[{IpAddress}:{Port}]";

	}
}
