using HslCommunication.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
#if !NET20 && !NET35
using System.Threading.Tasks;
#endif

namespace HslCommunication.Profinet.Keyence
{
	/// <summary>
	/// 基恩士SR2000系列扫码设备的通用接口
	/// </summary>
	interface IKeyenceSR2000Series
	{
		/// <inheritdoc cref="KeyenceSR2000Helper.ReadBarcode(Func{byte[], OperateResult{byte[]}})"/>
		[HslMqttApi]
		OperateResult<string> ReadBarcode( );

		/// <inheritdoc cref="KeyenceSR2000Helper.Reset(Func{byte[], OperateResult{byte[]}})"/>
		[HslMqttApi]
		OperateResult Reset( );

		/// <inheritdoc cref="KeyenceSR2000Helper.OpenIndicator(Func{byte[], OperateResult{byte[]}})"/>
		[HslMqttApi]
		OperateResult OpenIndicator( );

		/// <inheritdoc cref="KeyenceSR2000Helper.CloseIndicator(Func{byte[], OperateResult{byte[]}})"/>
		[HslMqttApi]
		OperateResult CloseIndicator( );

		/// <inheritdoc cref="KeyenceSR2000Helper.ReadVersion(Func{byte[], OperateResult{byte[]}})"/>
		[HslMqttApi]
		OperateResult<string> ReadVersion( );

		/// <inheritdoc cref="KeyenceSR2000Helper.ReadCommandState(Func{byte[], OperateResult{byte[]}})"/>
		[HslMqttApi]
		OperateResult<string> ReadCommandState( );

		/// <inheritdoc cref="KeyenceSR2000Helper.ReadErrorState(Func{byte[], OperateResult{byte[]}})"/>
		[HslMqttApi]
		OperateResult<string> ReadErrorState( );

		/// <inheritdoc cref="KeyenceSR2000Helper.CheckInput(int, Func{byte[], OperateResult{byte[]}})"/>
		[HslMqttApi]
		OperateResult<bool> CheckInput( int number );

		/// <inheritdoc cref="KeyenceSR2000Helper.SetOutput(int, bool, Func{byte[], OperateResult{byte[]}})"/>
		[HslMqttApi]
		OperateResult SetOutput( int number, bool value );

		/// <inheritdoc cref="KeyenceSR2000Helper.ReadRecord(Func{byte[], OperateResult{byte[]}})"/>
		[HslMqttApi]
		OperateResult<int[]> ReadRecord( );

		/// <inheritdoc cref="KeyenceSR2000Helper.Lock(Func{byte[], OperateResult{byte[]}})"/>
		[HslMqttApi]
		OperateResult Lock( );

		/// <inheritdoc cref="KeyenceSR2000Helper.UnLock(Func{byte[], OperateResult{byte[]}})"/>
		[HslMqttApi]
		OperateResult UnLock( );

		/// <inheritdoc cref="KeyenceSR2000Helper.ReadCustomer(string, Func{byte[], OperateResult{byte[]}})"/>
		[HslMqttApi]
		OperateResult<string> ReadCustomer( string command );

	}
}
