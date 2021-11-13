using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using HslCommunication.BasicFramework;
using HslCommunication.Core;
using HslCommunication.Serial;
using HslCommunication.Reflection;

namespace HslCommunication.Profinet.IDCard
{
	/// <summary>
	/// 基于SAM协议的串口通信类，支持读取身份证的数据信息，详细参见API文档<br />
	/// Network class implemented by Tcp based on the SAM protocol, which supports reading ID card data information, 
	/// see API documentation for details
	/// </summary>
	/// <example>
	/// 在使用之前需要实例化当前的对象，然后根据实际的情况填写好串口的信息，否则连接不上去。
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Profinet\SAMSerialSample.cs" region="Sample1" title="实例化操作" />
	/// 在实际的读取，我们一般放在后台进行循环扫描的操作，参见下面的代码
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Profinet\SAMSerialSample.cs" region="Sample2" title="基本的读取操作" />
	/// </example>
	public class SAMSerial : SerialBase
	{
		#region Constrcutor

		/// <inheritdoc cref="SAMTcpNet()"/>
		public SAMSerial( )
		{

		}

		#endregion

		#region SerialBase Override

		/// <inheritdoc/>
		protected override OperateResult<byte[]> SPReceived( SerialPort serialPort, bool awaitData )
		{
			List<byte> content = new List<byte>( );
			while (true)
			{
				OperateResult<byte[]> rece = base.SPReceived( serialPort, awaitData );
				if (!rece.IsSuccess) return rece;

				content.AddRange( rece.Content );
				if (CheckADSCommandCompletion( content )) return OperateResult.CreateSuccessResult( content.ToArray( ) );
			}
		}

		#endregion

		#region Public Method

		/// <inheritdoc cref="SAMTcpNet.ReadSafeModuleNumber"/>
		[HslMqttApi]
		public OperateResult<string> ReadSafeModuleNumber( )
		{
			byte[] command = PackToSAMCommand( BuildReadCommand( 0x12, 0xFF, null ) );

			OperateResult<byte[]> read = ReadFromCoreServer( command );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<string>( read );

			OperateResult check = CheckADSCommandAndSum( read.Content );
			if (!check.IsSuccess) return OperateResult.CreateFailedResult<string>( check );

			return ExtractSafeModuleNumber( read.Content );
		}

		/// <inheritdoc cref="SAMTcpNet.CheckSafeModuleStatus"/>
		[HslMqttApi]
		public OperateResult CheckSafeModuleStatus( )
		{
			byte[] command = PackToSAMCommand( BuildReadCommand( 0x12, 0xFF, null ) );

			OperateResult<byte[]> read = ReadFromCoreServer( command );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<string>( read );

			OperateResult check = CheckADSCommandAndSum( read.Content );
			if (!check.IsSuccess) return OperateResult.CreateFailedResult<string>( check );

			if (read.Content[9] != 0x90) return new OperateResult( GetErrorDescription( read.Content[9] ) );
			return OperateResult.CreateSuccessResult( );
		}

		/// <inheritdoc cref="SAMTcpNet.SearchCard"/>
		[HslMqttApi]
		public OperateResult SearchCard( )
		{
			byte[] command = PackToSAMCommand( BuildReadCommand( 0x20, 0x01, null ) );

			OperateResult<byte[]> read = ReadFromCoreServer( command );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<string>( read );

			OperateResult check = CheckADSCommandAndSum( read.Content );
			if (!check.IsSuccess) return OperateResult.CreateFailedResult<string>( check );

			if (read.Content[9] != 0x9F) return new OperateResult( GetErrorDescription( read.Content[9] ) );
			return OperateResult.CreateSuccessResult( );
		}

		/// <inheritdoc cref="SAMTcpNet.SelectCard"/>
		[HslMqttApi]
		public OperateResult SelectCard( )
		{
			byte[] command = PackToSAMCommand( BuildReadCommand( 0x20, 0x02, null ) );

			OperateResult<byte[]> read = ReadFromCoreServer( command );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<string>( read );

			OperateResult check = CheckADSCommandAndSum( read.Content );
			if (!check.IsSuccess) return OperateResult.CreateFailedResult<string>( check );

			if (read.Content[9] != 0x90) return new OperateResult( GetErrorDescription( read.Content[9] ) );
			return OperateResult.CreateSuccessResult( );
		}

		/// <inheritdoc cref="SAMTcpNet.ReadCard"/>
		[HslMqttApi]
		public OperateResult<IdentityCard> ReadCard( )
		{
			byte[] command = PackToSAMCommand( BuildReadCommand( 0x30, 0x01, null ) );

			OperateResult<byte[]> read = ReadFromCoreServer( command );
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<IdentityCard>( read );

			OperateResult check = CheckADSCommandAndSum( read.Content );
			if (!check.IsSuccess) return OperateResult.CreateFailedResult<IdentityCard>( check );

			return ExtractIdentityCard( read.Content );
		}

		#endregion

		#region Object Override

		/// <inheritdoc/>
		public override string ToString( ) => $"SAMSerial[{PortName}:{BaudRate}]";

		#endregion

		#region Static Helper

		/// <summary>
		/// 将指令进行打包成可以发送的数据对象
		/// </summary>
		/// <param name="command">命令信息</param>
		/// <returns>字节数组</returns>
		public static byte[] PackToSAMCommand(byte[] command )
		{
			byte[] buffer = new byte[command.Length + 8];
			buffer[0] = 0xAA;
			buffer[1] = 0xAA;
			buffer[2] = 0xAA;
			buffer[3] = 0x96;
			buffer[4] = 0x69;
			buffer[5] = BitConverter.GetBytes( buffer.Length - 7 )[1];
			buffer[6] = BitConverter.GetBytes( buffer.Length - 7 )[0];
			command.CopyTo( buffer, 7 );
			
			int count = 0;
			for (int i = 5; i < buffer.Length - 1; i++)
			{
				count ^= buffer[i];
			}
			buffer[buffer.Length - 1] = (byte)count;
			return buffer;
		}

		/// <summary>
		/// 根据SAM的实际的指令，来生成实际的指令信息
		/// </summary>
		/// <param name="cmd">命令码</param>
		/// <param name="para">参数信息</param>
		/// <param name="data">数据内容</param>
		/// <returns>字符串的结果信息</returns>
		public static byte[] BuildReadCommand(byte cmd, byte para, byte[] data )
		{
			if (data == null) data = new byte[0];
			byte[] buffer = new byte[2 + data.Length];
			buffer[0] = cmd;
			buffer[1] = para;
			data.CopyTo( buffer, 2 );
			return buffer;
		}

		/// <summary>
		/// 检查当前的接收数据信息是否一条完整的数据信息
		/// </summary>
		/// <param name="input">输入的信息</param>
		/// <returns>是否接收完成</returns>
		public static bool CheckADSCommandCompletion( List<byte> input )
		{
			if (input?.Count < 8) return false;
			if ((input[5] * 256 + input[6]) > (input.Count - 7)) return false;
			return true;
		}

		/// <summary>
		/// 检查当前的指令是否是正确的
		/// </summary>
		/// <param name="input">输入的指令信息</param>
		/// <returns>是否校验成功</returns>
		public static OperateResult CheckADSCommandAndSum( byte[] input )
		{
			if (input?.Length < 8) return new OperateResult( StringResources.Language.SAMReceiveLengthMustLargerThan8 );
			if (input[0] != 0xAA || input[1] != 0xAA || input[2] != 0xAA || input[3] != 0x96 || input[4] != 0x69)
				return new OperateResult( StringResources.Language.SAMHeadCheckFailed );
			if ((input[5] * 256 + input[6]) != (input.Length - 7))
				return new OperateResult( StringResources.Language.SAMLengthCheckFailed );

			int count = 0;
			for (int i = 5; i < input.Length - 1; i++)
			{
				count ^= input[i];
			}

			if (count != input[input.Length - 1])
				return new OperateResult( StringResources.Language.SAMSumCheckFailed );
			else
				return OperateResult.CreateSuccessResult( );
		}

		/// <summary>
		/// 提炼安全的模块数据信息
		/// </summary>
		/// <param name="data">数据</param>
		/// <returns>结果对象</returns>
		public static OperateResult<string> ExtractSafeModuleNumber( byte[] data )
		{
			try
			{
				if (data[9] != 0x90)
					return new OperateResult<string>( GetErrorDescription( data[9] ) );

				StringBuilder sb = new StringBuilder( );
				sb.Append( data[10].ToString( "D2" ) );
				sb.Append( "." );
				sb.Append( data[12].ToString( "D2" ) );
				sb.Append( "-" );
				sb.Append( BitConverter.ToInt32( data, 14 ).ToString( ) );
				sb.Append( "-" );
				sb.Append( BitConverter.ToInt32( data, 18 ).ToString( "D9" ) );
				sb.Append( "-" );
				sb.Append( BitConverter.ToInt32( data, 22 ).ToString( "D9" ) );

				return OperateResult.CreateSuccessResult( sb.ToString( ) );
			}
			catch(Exception ex)
			{
				return new OperateResult<string>( "Error:" + ex.Message + "  Source Data: " + SoftBasic.ByteToHexString( data ) );
			}
		}

		/// <summary>
		/// 从数据中提取出真实的身份证信息
		/// </summary>
		/// <param name="data">原始数据内容</param>
		/// <returns>包含结果对象的身份证数据</returns>
		public static OperateResult<IdentityCard> ExtractIdentityCard( byte[] data )
		{
			try
			{
				if (data[9] != 0x90) return new OperateResult<IdentityCard>( GetErrorDescription( data[9] ) );

				string strContent = Encoding.Unicode.GetString( data, 14, 256 );
				byte[] imageContent = SoftBasic.ArraySelectMiddle( data, 270, 1024 );
				IdentityCard identityCard         = new IdentityCard( );
				identityCard.Name                 = strContent.Substring( 0, 15 );
				identityCard.Sex                  = strContent.Substring( 15, 1 ) == "1" ? "男" : strContent.Substring( 15, 1 ) == "2" ? "女" : "未知";
				identityCard.Nation               = GetNationText( Convert.ToInt32( strContent.Substring( 16, 2 ) ) );
				identityCard.Birthday             = new DateTime( int.Parse( strContent.Substring( 18, 4 ) ), int.Parse( strContent.Substring( 22, 2 ) ), int.Parse( strContent.Substring( 24, 2 ) ) );
				identityCard.Address              = strContent.Substring( 26, 35 );
				identityCard.Id                   = strContent.Substring( 61, 18 );
				identityCard.Organ                = strContent.Substring( 79, 15 );
				identityCard.ValidityStartDate    = new DateTime( int.Parse( strContent.Substring( 94, 4 ) ), int.Parse( strContent.Substring( 98, 2 ) ), int.Parse( strContent.Substring( 100, 2 ) ) );
				identityCard.ValidityEndDate      = new DateTime( int.Parse( strContent.Substring( 102, 4 ) ), int.Parse( strContent.Substring( 106, 2 ) ), int.Parse( strContent.Substring( 108, 2 ) ) );
				identityCard.Portrait             = imageContent;
				return OperateResult.CreateSuccessResult( identityCard );
			}
			catch(Exception ex)
			{
				return new OperateResult<IdentityCard>( ex.Message );
			}
		}

		/// <summary>
		/// 根据民族的代号来获取到民族的文本描述信息
		/// </summary>
		/// <param name="nation">民族代码</param>
		/// <returns>民族的文本信息</returns>
		public static string GetNationText( int nation )
		{
			switch (nation)
			{
				case 01: return "汉";
				case 02: return "蒙古";
				case 03: return "回";
				case 04: return "藏";
				case 05: return "维吾尔";
				case 06: return "苗";
				case 07: return "彝";
				case 08: return "壮";
				case 09: return "布依";
				case 10: return "朝鲜";
				case 11: return "满";
				case 12: return "侗";
				case 13: return "瑶";
				case 14: return "白";
				case 15: return "土家";
				case 16: return "哈尼";
				case 17: return "哈萨克";
				case 18: return "傣";
				case 19: return "黎";
				case 20: return "傈僳";
				case 21: return "佤";
				case 22: return "畲";
				case 23: return "高山";
				case 24: return "拉祜";
				case 25: return "水";
				case 26: return "东乡";
				case 27: return "纳西";
				case 28: return "景颇";
				case 29: return "柯尔克孜";
				case 30: return "土";
				case 31: return "达斡尔";
				case 32: return "仫佬";
				case 33: return "羌";
				case 34: return "布朗";
				case 35: return "撒拉";
				case 36: return "毛南";
				case 37: return "仡佬";
				case 38: return "锡伯";
				case 39: return "阿昌";
				case 40: return "普米";
				case 41: return "塔吉克";
				case 42: return "怒";
				case 43: return "乌孜别克";
				case 44: return "俄罗斯";
				case 45: return "鄂温克";
				case 46: return "德昂";
				case 47: return "保安";
				case 48: return "裕固";
				case 49: return "京";
				case 50: return "塔塔尔";
				case 51: return "独龙";
				case 52: return "鄂伦春";
				case 53: return "赫哲";
				case 54: return "门巴";
				case 55: return "珞巴";
				case 56: return "基诺";
				case 97: return "其他";
				case 98: return "外国血统中国籍人士";
				default: return string.Empty;
			}
		}

		/// <summary>
		/// 枚举当前的所有的民族信息，共计五十六个民族
		/// </summary>
		/// <returns>枚举信息</returns>
		public static IEnumerator<string> GetNationEnumerator( )
		{
			for (int i = 1; i < 57; i++)
			{
				yield return GetNationText( i );
			}
		}

		/// <summary>
		/// 获取错误的文本信息
		/// </summary>
		/// <param name="err">错误号</param>
		/// <returns>错误信息</returns>
		public static string GetErrorDescription( int err )
		{
			switch (err)
			{
				case 0x91: return StringResources.Language.SAMStatus91;
				case 0x10: return StringResources.Language.SAMStatus10;
				case 0x11: return StringResources.Language.SAMStatus11;
				case 0x21: return StringResources.Language.SAMStatus21;
				case 0x23: return StringResources.Language.SAMStatus23;
				case 0x24: return StringResources.Language.SAMStatus24;
				case 0x31: return StringResources.Language.SAMStatus31;
				case 0x32: return StringResources.Language.SAMStatus32;
				case 0x33: return StringResources.Language.SAMStatus33;
				case 0x40: return StringResources.Language.SAMStatus40;
				case 0x41: return StringResources.Language.SAMStatus41;
				case 0x47: return StringResources.Language.SAMStatus47;
				case 0x60: return StringResources.Language.SAMStatus60;
				case 0x66: return StringResources.Language.SAMStatus66;
				case 0x80: return StringResources.Language.SAMStatus80;
				case 0x81: return StringResources.Language.SAMStatus81;
				default: return StringResources.Language.UnknownError;
			}
		}

		#endregion

	}
}
