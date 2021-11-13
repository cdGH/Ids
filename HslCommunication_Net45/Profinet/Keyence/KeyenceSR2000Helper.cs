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
	/// 当前SR2000的辅助函数
	/// </summary>
	internal class KeyenceSR2000Helper
	{
		/// <summary>
		/// 读取条码信息，返回最终读取的条码数据<br />
		/// Read the barcode information and return the finally read barcode data
		/// </summary>
		/// <param name="readCore">核心交互的方法</param>
		/// <returns>条码信息</returns>
		public static OperateResult<string> ReadBarcode( Func<byte[], OperateResult<byte[]>> readCore )
		{
			OperateResult<string> read = ReadCustomer( "LON", readCore );
			if (!read.IsSuccess)
			{
				// 回发LOFF指令
				if (read.ErrorCode < 0) ReadCustomer( "LOFF", readCore );
			}
			return read;
		}

		/// <summary>
		/// 复位命令，响应后，进行复位动作。<br />
		/// Reset command, after responding, reset action.
		/// </summary>
		/// <param name="readCore">核心交互的方法</param>
		/// <returns>返回是否成功的结果对象</returns>
		public static OperateResult Reset( Func<byte[], OperateResult<byte[]>> readCore ) => ReadCustomer( "RESET", readCore );

		/// <summary>
		/// 打开指示灯<br />
		/// Turn on the indicator
		/// </summary>
		/// <param name="readCore">核心交互的方法</param>
		/// <returns>返回是否成功的结果对象</returns>
		public static OperateResult OpenIndicator( Func<byte[], OperateResult<byte[]>> readCore ) => ReadCustomer( "AMON", readCore );

		/// <summary>
		/// 关闭指示灯<br />
		/// Turn off the indicator
		/// </summary>
		/// <param name="readCore">核心交互的方法</param>
		/// <returns>返回是否成功的结果对象</returns>
		public static OperateResult CloseIndicator( Func<byte[], OperateResult<byte[]>> readCore ) => ReadCustomer( "AMOFF", readCore );

		/// <summary>
		/// 读取基恩士的版本<br />
		/// Read Keyence's version
		/// </summary>
		/// <param name="readCore">核心交互的方法</param>
		/// <returns>版本信息</returns>
		public static OperateResult<string> ReadVersion( Func<byte[], OperateResult<byte[]>> readCore ) => ReadCustomer( "KEYENCE", readCore );

		/// <summary>
		/// 读取基恩士的命令状态，none:不处理；wait：等待设置反映；update：正在更新<br />
		/// Read the command status of Keyence, none: do not process; wait: wait for the setting to reflect; update: update
		/// </summary>
		/// <param name="readCore">核心交互的方法</param>
		/// <returns>返回是否成功的结果对象</returns>
		public static OperateResult<string> ReadCommandState( Func<byte[], OperateResult<byte[]>> readCore ) => ReadCustomer( "CMDSTAT", readCore );

		/// <summary>
		/// 读取基恩士的错误状态<br />
		/// Read the error status of Keyence
		/// </summary>
		/// <param name="readCore">核心交互的方法</param>
		/// <returns>返回是否成功的结果对象</returns>
		public static OperateResult<string> ReadErrorState( Func<byte[], OperateResult<byte[]>> readCore ) => ReadCustomer( "ERRSTAT", readCore );

		/// <summary>
		/// 读取IN端子的状况，需要传入哪个IN端子，返回是否通断<br />
		/// Read the status of the IN terminal, which IN terminal needs to be passed in, and return whether it is on or off
		/// </summary>
		/// <param name="number">端子的信息</param>
		/// <param name="readCore">核心交互的方法</param>
		/// <returns>返回是否成功的结果对象</returns>
		public static OperateResult<bool> CheckInput( int number, Func<byte[], OperateResult<byte[]>> readCore )
		{
			OperateResult<string> read = ReadCustomer( "INCHK," + number, readCore );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<bool>( read );

			if (read.Content == "ON") return OperateResult.CreateSuccessResult( true );
			else if (read.Content == "OFF") return OperateResult.CreateSuccessResult( false );
			else return new OperateResult<bool>( read.Content );
		}

		/// <summary>
		/// 设置OUT端子的使能，需要传入哪个OUT端子，返回是否设置成功！<br />
		/// Set the enable of the OUT terminal, which OUT terminal needs to be passed in, and return whether the setting is successful!
		/// </summary>
		/// <param name="number">端子的索引，1，2，3</param>
		/// <param name="value">是否通断</param>
		/// <param name="readCore">核心交互的方法</param>
		/// <returns>返回是否成功的结果对象</returns>
		public static OperateResult SetOutput( int number, bool value, Func<byte[], OperateResult<byte[]>> readCore ) => ReadCustomer( (value ? "OUTON," : "OUTOFF,") + number, readCore );

		/// <summary>
		/// 读取扫码器的扫码记录，返回数组数据，分别是成功次数，失败次数，ERROR次数，稳定次数，时机输入次数<br />
		/// Read the scan code record of the scanner and return the array data, which are the number of successes, failures, ERRORs, stable times, and timing input times.
		/// </summary>
		/// <param name="readCore">核心交互的方法</param>
		/// <returns>返回是否成功的结果对象</returns>
		public static OperateResult<int[]> ReadRecord( Func<byte[], OperateResult<byte[]>> readCore )
		{
			OperateResult<string> read = ReadCustomer( "NUM", readCore );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<int[]>( read );

			return OperateResult.CreateSuccessResult( read.Content.Split( ',' ).Select( n => int.Parse( n ) ).ToArray( ) );
		}

		/// <summary>
		/// 锁定扫码设备<br />
		/// Lock scanning device
		/// </summary>
		/// <param name="readCore">核心交互的方法</param>
		/// <returns>返回是否成功的结果对象</returns>
		public static OperateResult Lock( Func<byte[], OperateResult<byte[]>> readCore ) => ReadCustomer( "LOCK", readCore );

		/// <summary>
		/// 解除锁定的扫码设备<br />
		/// Unlock Scanning Device
		/// </summary>
		/// <param name="readCore">核心交互的方法</param>
		/// <returns>返回是否成功的结果对象</returns>
		public static OperateResult UnLock( Func<byte[], OperateResult<byte[]>> readCore ) => ReadCustomer( "UNLOCK", readCore );

		/// <summary>
		/// 读取自定义的命令，例如LON，如果是包含其他参数的，比如打开OUT端子，OUTON,1<br />
		/// Read custom commands, such as LON, if it contains other parameters, such as open OUT terminal, OUTON,1
		/// </summary>
		/// <param name="command">自定义的命令</param>
		/// <param name="readCore">核心的数据交互方法</param>
		/// <returns>返回是否成功的结果对象</returns>
		public static OperateResult<string> ReadCustomer( string command, Func<byte[], OperateResult<byte[]>> readCore )
		{
			byte[] buffer = Encoding.ASCII.GetBytes( $"{command}\r" );

			string cmd = command;
			if (command.IndexOf( ',' ) > 0) cmd = command.Substring( 0, command.IndexOf( ',' ) );

			OperateResult<byte[]> read = readCore( buffer );
			if (!read.IsSuccess) return read.Convert<string>( null );

			string result = Encoding.ASCII.GetString( read.Content ).Trim( '\r' );
			if (result.StartsWith( $"ER,{cmd}," )) return new OperateResult<string>( GetErrorDescription( result.Substring( 4 + cmd.Length ) ) );

			if (result.StartsWith( $"OK,{cmd}" ))
			{
				if (result.Length > (4 + cmd.Length))
				{
					return OperateResult.CreateSuccessResult( result.Substring( 4 + cmd.Length ) );
				}
			}

			return OperateResult.CreateSuccessResult( result );
		}

#if !NET20 && !NET35

#endif
		/// <summary>
		/// 获取操作代码包含的错误文本信息<br />
		/// Get the error text information contained in the operation code
		/// </summary>
		/// <param name="error">错误代码</param>
		/// <returns>真是的错误信息</returns>
		public static string GetErrorDescription( string error )
		{
			switch (error)
			{
				case "00": return StringResources.Language.KeyenceSR2000Error00;
				case "01": return StringResources.Language.KeyenceSR2000Error01;
				case "02": return StringResources.Language.KeyenceSR2000Error02;
				case "03": return StringResources.Language.KeyenceSR2000Error03;
				case "04": return StringResources.Language.KeyenceSR2000Error04;
				case "05": return StringResources.Language.KeyenceSR2000Error05;
				case "10": return StringResources.Language.KeyenceSR2000Error10;
				case "11": return StringResources.Language.KeyenceSR2000Error11;
				case "12": return StringResources.Language.KeyenceSR2000Error12;
				case "13": return StringResources.Language.KeyenceSR2000Error13;
				case "14": return StringResources.Language.KeyenceSR2000Error14;
				case "20": return StringResources.Language.KeyenceSR2000Error20;
				case "21": return StringResources.Language.KeyenceSR2000Error21;
				case "22": return StringResources.Language.KeyenceSR2000Error22;
				case "23": return StringResources.Language.KeyenceSR2000Error23;
				case "99": return StringResources.Language.KeyenceSR2000Error99;
				default: return StringResources.Language.UnknownError + " :" + error;
			}
		}
	}
}
