using HslCommunication.Reflection;
using HslCommunication.Serial;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HslCommunication.Profinet.Keyence
{
	/// <inheritdoc cref="KeyenceSR2000SeriesTcp"/>
	public class KeyenceSR2000Serial : SerialBase, IKeyenceSR2000Series
	{
		/// <inheritdoc cref="KeyenceSR2000SeriesTcp()"/>
		public KeyenceSR2000Serial( ) : base( ) { ReceiveTimeout = 10_000; SleepTime = 20; }

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

		/// <inheritdoc/>
		public override string ToString( ) => $"KeyenceSR2000Serial[{PortName}:{BaudRate}]";
	}
}
