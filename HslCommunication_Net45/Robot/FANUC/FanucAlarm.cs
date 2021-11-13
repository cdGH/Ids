using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HslCommunication.Core;
#if !NET35 && !NET20
using System.Threading.Tasks;
#endif

namespace HslCommunication.Robot.FANUC
{
	/// <summary>
	/// Fanuc机器人的报警对象
	/// </summary>
	public class FanucAlarm
	{
		/// <summary>
		/// AlarmID
		/// </summary>
		public short AlarmID { get; set; }

		/// <summary>
		/// AlarmNumber
		/// </summary>
		public short AlarmNumber { get; set; }

		/// <summary>
		/// CauseAlarmID
		/// </summary>
		public short CauseAlarmID { get; set; }

		/// <summary>
		/// CauseAlarmNumber
		/// </summary>
		public short CauseAlarmNumber { get; set; }

		/// <summary>
		/// Severity
		/// </summary>
		public short Severity { get; set; }

		/// <summary>
		/// Time
		/// </summary>
		public DateTime Time { get; set; }

		/// <summary>
		/// AlarmMessage
		/// </summary>
		public string AlarmMessage { get; set; }

		/// <summary>
		/// CauseAlarmMessage
		/// </summary>
		public string CauseAlarmMessage { get; set; }

		/// <summary>
		/// SeverityMessage
		/// </summary>
		public string SeverityMessage { get; set; }

		/// <summary>
		/// 从字节数据加载真实的信息
		/// </summary>
		/// <param name="byteTransform">字节变换</param>
		/// <param name="content">原始的字节内容</param>
		/// <param name="index">索引</param>
		/// <param name="encoding">编码</param>
		public void LoadByContent( IByteTransform byteTransform, byte[] content, int index, Encoding encoding )
		{
			AlarmID             = BitConverter.ToInt16( content, index );
			AlarmNumber         = BitConverter.ToInt16( content, index + 2 );
			CauseAlarmID        = BitConverter.ToInt16( content, index + 4 );
			CauseAlarmNumber    = BitConverter.ToInt16( content, index + 6 );
			Severity            = BitConverter.ToInt16( content, index + 8 );

			if (BitConverter.ToInt16( content, index + 10 ) > 0)
			{
				Time = new DateTime( BitConverter.ToInt16( content, index + 10 ), BitConverter.ToInt16( content, index + 12 ), BitConverter.ToInt16( content, index + 14 ),
					BitConverter.ToInt16( content, index + 16 ), BitConverter.ToInt16( content, index + 18 ), BitConverter.ToInt16( content, index + 20 ) );
			}

			AlarmMessage        = encoding.GetString( content, index + 22, 80 ).Trim( '\u0000' );
			CauseAlarmMessage   = encoding.GetString( content, index + 102, 80 ).Trim( '\u0000' );
			SeverityMessage     = encoding.GetString( content, index + 182, 18 ).Trim( '\u0000' );
		}

		/// <inheritdoc/>
		public override string ToString( ) => $"FanucAlarm ID[{AlarmID},{AlarmNumber},{CauseAlarmID},{CauseAlarmNumber},{Severity}]{Environment.NewLine}{AlarmMessage}{Environment.NewLine}{CauseAlarmMessage}{Environment.NewLine}{SeverityMessage}";

		/// <summary>
		/// 从数据内容创建报警信息
		/// </summary>
		/// <param name="byteTransform">字节变换</param>
		/// <param name="content">原始的字节内容</param>
		/// <param name="index">索引</param>
		/// <param name="encoding">编码</param>
		/// <returns>报警信息</returns>
		public static FanucAlarm PraseFrom( IByteTransform byteTransform, byte[] content, int index, Encoding encoding )
		{
			FanucAlarm fanucAlarm = new FanucAlarm( );
			fanucAlarm.LoadByContent( byteTransform, content, index, encoding );
			return fanucAlarm;
		}
	}
}
