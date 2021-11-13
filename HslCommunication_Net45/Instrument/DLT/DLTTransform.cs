using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
#if !NET35 && !NET20
using System.Threading.Tasks;
#endif

namespace HslCommunication.Instrument.DLT
{
	/// <summary>
	/// DTL数据转换
	/// </summary>
	public class DLTTransform
	{
		/// <summary>
		/// Byte[]转ToHexString
		/// </summary>
		/// <param name="content">原始的字节内容</param>
		/// <param name="length">长度信息</param>
		/// <returns></returns>
		public static OperateResult<string> TransStringFromDLt( byte[] content, ushort length )
		{
			OperateResult<string> result;
			try
			{
				string empty = string.Empty;
				byte[] buffer = content.SelectBegin( length ).Reverse( ).ToArray( );
				for (int i = 0; i < buffer.Length; i++)
				{
					buffer[i] = (byte)(buffer[i] - 0x33);
				}
				result = OperateResult.CreateSuccessResult( Encoding.ASCII.GetString( buffer ) );
			}
			catch (Exception ex)
			{
				result = new OperateResult<string>( ex.Message + " Reason: " + content.ToHexString( ' ' ) );
			}
			return result;
		}

		/// <summary>
		/// Byte[]转Dlt double[]
		/// </summary>
		/// <param name="content">原始的字节数据</param>
		/// <param name="length">需要转换的数据长度</param>
		/// <param name="format">当前数据的解析格式</param>
		/// <returns>结果内容</returns>
		public static OperateResult<double[]> TransDoubleFromDLt( byte[] content, ushort length, string format = "XXXXXX.XX" )
		{
			try
			{
				format = format.ToUpper( );
				int count = format.Count( m => m == 'X' ) / 2;
				int decimalCount = format.IndexOf( '.' ) >= 0 ? format.Length - format.IndexOf( '.' ) - 1 : 0;

				double[] values = new double[length];
				for (int i = 0; i < values.Length; i++)
				{
					byte[] buffer = content.SelectMiddle( i * count, count ).Reverse( ).ToArray( );
					for (int j = 0; j < buffer.Length; j++)
					{
						buffer[j] = (byte)(buffer[j] - 0x33);
					}

					values[i] = Convert.ToDouble( buffer.ToHexString( ) ) / Math.Pow( 10, decimalCount );
				}
				return OperateResult.CreateSuccessResult( values );
			}
			catch (Exception ex)
			{
				return new OperateResult<double[]>( ex.Message );
			}
		}
	}
}
