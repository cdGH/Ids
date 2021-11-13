using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HslCommunication.Profinet.Yamatake.Helper
{
	/// <summary>
	/// 辅助类方法
	/// </summary>
	public class DigitronCPLHelper
	{
		/// <summary>
		/// 构建写入操作的报文信息
		/// </summary>
		/// <param name="station">站号</param>
		/// <param name="address">地址</param>
		/// <param name="length">长度的长度</param>
		/// <returns>报文内容</returns>
		public static OperateResult<byte[]> BuildReadCommand( byte station, string address, ushort length )
		{
			try
			{
				StringBuilder sb = new StringBuilder( );
				sb.Append( '\u0002' );
				sb.Append( station.ToString( "X2" ) );
				sb.Append( "00XRS," );
				sb.Append( ushort.Parse( address ).ToString( ) );
				sb.Append( "W," );
				sb.Append( length.ToString( ) );
				sb.Append( '\u0003' );

				int sum = 0;
				for (int i = 0; i < sb.Length; i++)
				{
					sum += sb[i];
				}
				byte check = (byte)(256 - sum % 256);
				sb.Append( check.ToString( "X2" ) );
				sb.Append( "\u000D\u000A" );
				return OperateResult.CreateSuccessResult( Encoding.ASCII.GetBytes( sb.ToString( ) ) );
			}
			catch(Exception ex)
			{
				return new OperateResult<byte[]>( "Address wrong: " + ex.Message );
			}
		}

		/// <summary>
		/// 构建写入操作的命令报文
		/// </summary>
		/// <param name="station">站号信息</param>
		/// <param name="address">数据的地址</param>
		/// <param name="value">等待写入的值</param>
		/// <returns>写入的报文命令</returns>
		public static OperateResult<byte[]> BuildWriteCommand( byte station, string address, byte[] value )
		{
			try
			{
				StringBuilder sb = new StringBuilder( );
				sb.Append( '\u0002' );
				sb.Append( station.ToString( "X2" ) );
				sb.Append( "00XWS," );
				sb.Append( ushort.Parse( address ).ToString( ) );
				sb.Append( "W" );
				for (int i = 0; i < value.Length / 2; i++)
				{
					short tmp = BitConverter.ToInt16( value, i * 2 );
					sb.Append( "," );
					sb.Append( tmp.ToString( ) );
				}
				sb.Append( '\u0003' );

				int sum = 0;
				for (int i = 0; i < sb.Length; i++)
				{
					sum += sb[i];
				}
				byte check = (byte)(256 - sum % 256);
				sb.Append( check.ToString( "X2" ) );
				sb.Append( "\u000D\u000A" );
				return OperateResult.CreateSuccessResult( Encoding.ASCII.GetBytes( sb.ToString( ) ) );
			}
			catch (Exception ex)
			{
				return new OperateResult<byte[]>( "Address wrong: " + ex.Message );
			}
		}

		/// <summary>
		/// 用于服务器反馈的数据的报文打包操作
		/// </summary>
		/// <param name="station">站号</param>
		/// <param name="err">错误码，如果为0则表示正常</param>
		/// <param name="value">原始数据值信息</param>
		/// <param name="dataType">数据类型</param>
		/// <returns>打包的报文数据信息</returns>
		public static byte[] PackResponseContent( byte station, int err, byte[] value, byte dataType  )
		{
			StringBuilder sb = new StringBuilder( );
			sb.Append( '\u0002' );
			sb.Append( station.ToString( "X2" ) );
			sb.Append( "00X" );
			sb.Append( err.ToString( "D2" ) );
			if (err == 0)
			{
				if(value != null)
				{
					for (int i = 0; i < value.Length / 2; i++)
					{
						if(dataType == 0x57)
						{
							short tmp = BitConverter.ToInt16( value, i * 2 );
							sb.Append( "," );
							sb.Append( tmp.ToString( ) );
						}
						else
						{
							ushort tmp = BitConverter.ToUInt16( value, i * 2 );
							sb.Append( "," );
							sb.Append( tmp.ToString( ) );
						}
					}
				}
			}
			sb.Append( '\u0003' );

			int sum = 0;
			for (int i = 0; i < sb.Length; i++)
			{
				sum += sb[i];
			}
			byte check = (byte)(256 - sum % 256);
			sb.Append( check.ToString( "X2" ) );
			sb.Append( "\u000D\u000A" );
			return Encoding.ASCII.GetBytes( sb.ToString( ) );
		}

		/// <summary>
		/// 根据错误码获取到相关的错误代号信息
		/// </summary>
		/// <param name="err">错误码</param>
		/// <returns>错误码对应的文本描述信息</returns>
		public static string GetErrorText(int err )
		{
			switch (err)
			{
				case 40: return StringResources.Language.YamatakeDigitronCPL40;
				case 41: return StringResources.Language.YamatakeDigitronCPL41;
				case 42: return StringResources.Language.YamatakeDigitronCPL42;
				case 43: return StringResources.Language.YamatakeDigitronCPL43;
				case 44: return StringResources.Language.YamatakeDigitronCPL44;
				case 45: return StringResources.Language.YamatakeDigitronCPL45;
				case 46: return StringResources.Language.YamatakeDigitronCPL46;
				case 47: return StringResources.Language.YamatakeDigitronCPL47;
				case 48: return StringResources.Language.YamatakeDigitronCPL48;
				case 99: return StringResources.Language.YamatakeDigitronCPL99;
				default: return StringResources.Language.UnknownError;
			}
		}

		/// <summary>
		/// 从反馈的数据内容中解析出真实的数据信息
		/// </summary>
		/// <param name="response">仪表反馈的真实的数据信息</param>
		/// <returns>解析之后的实际数据信息</returns>
		public static OperateResult<byte[]> ExtraActualResponse( byte[] response )
		{
			try
			{
				int err = Convert.ToInt32( Encoding.ASCII.GetString( response, 6, 2 ) );
				if (err > 0) return new OperateResult<byte[]>( err, GetErrorText( err ) );

				int endIndex = 8;
				for (int i = 8; i < response.Length; i++)
				{
					if (response[i] == 0x03)
					{
						endIndex = i;
						break;
					}
				}
				int startIndex = response[8] == 0x2C ? 9 : 8;
				if (endIndex - startIndex > 0)
				{
					string[] splits = Encoding.ASCII.GetString( response, startIndex, endIndex - startIndex ).Split( new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries );
					short[] values = splits.Select( m => short.Parse( m ) ).ToArray( );
					byte[] buffer = new byte[values.Length * 2];
					for (int i = 0; i < values.Length; i++)
					{
						BitConverter.GetBytes( values[i] ).CopyTo( buffer, i * 2 );
					}
					return OperateResult.CreateSuccessResult( buffer );
				}
				return OperateResult.CreateSuccessResult( new byte[0] );
			}
			catch (Exception ex)
			{
				return new OperateResult<byte[]>( "Data wrong: " + ex.Message + Environment.NewLine + "Source: " + response.ToHexString( ' ' ) );
			}
		}
	}
}
