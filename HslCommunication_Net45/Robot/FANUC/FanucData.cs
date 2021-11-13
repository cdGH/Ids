using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using HslCommunication.Core;
using HslCommunication.BasicFramework;
#if !NET35 && !NET20
using System.Threading.Tasks;
#endif

namespace HslCommunication.Robot.FANUC
{
#pragma warning disable CS1591 // 缺少对公共可见类型或成员的 XML 注释
	/// <summary>
	/// Fanuc机器人的所有的数据信息
	/// </summary>
	public class FanucData
	{
		public FanucAlarm[] AlarmList { get; set; }

		public FanucAlarm AlarmCurrent { get; set; }

		public FanucAlarm AlarmPassword { get; set; }

		public FanucPose CurrentPose { get; set; }

		public FanucPose CurrentPoseUF { get; set; }

		public FanucPose CurrentPose2 { get; set; }

		public FanucPose CurrentPose3 { get; set; }

		public FanucPose CurrentPose4 { get; set; }

		public FanucPose CurrentPose5 { get; set; }

		public FanucTask Task { get; set; }

		public FanucTask TaskIgnoreMacro { get; set; }

		public FanucTask TaskIgnoreKarel { get; set; }

		public FanucTask TaskIgnoreMacroKarel { get; set; }

		public FanucPose[] PosRegGP1 { get; set; }

		public FanucPose[] PosRegGP2 { get; set; }

		public FanucPose[] PosRegGP3 { get; set; }

		public FanucPose[] PosRegGP4 { get; set; }

		public FanucPose[] PosRegGP5 { get; set; }

		public int FAST_CLOCK { get; set; }

		public int Timer10_TIMER_VAL { get; set; }

		public float MOR_GRP_CURRENT_ANG { get; set; }

		public float DUTY_TEMP { get; set; }

		public string TIMER10_COMMENT { get; set; }

		public string TIMER2_COMMENT { get; set; }

		public FanucPose MNUTOOL1_1 { get; set; }

		public string HTTPKCL_CMDS { get; set; }

		public int[] NumReg1 { get; set; }

		public float[] NumReg2 { get; set; }

		public FanucPose[] DataPosRegMG { get; set; }

		public string[] DIComment { get; set; }

		public string[] DOComment { get; set; }

		public string[] RIComment { get; set; }

		public string[] ROComment { get; set; }

		public string[] UIComment { get; set; }

		public string[] UOComment { get; set; }

		public string[] SIComment { get; set; }

		public string[] SOComment { get; set; }

		public string[] WIComment { get; set; }

		public string[] WOComment { get; set; }

		public string[] WSIComment { get; set; }

		public string[] AIComment { get; set; }

		public string[] AOComment { get; set; }

		public string[] GIComment { get; set; }

		public string[] GOComment { get; set; }
		public string[] STRREGComment { get; set; }

		public string[] STRREG_COMMENT_Comment { get; set; }

		/// <summary>
		/// 从原始的数据内容加载数据
		/// </summary>
		/// <param name="content">原始的内容</param>
		public void LoadByContent( byte[] content )
		{
			IByteTransform byteTransform = new RegularByteTransform( );
			Encoding encoding;
			try
			{
				encoding = Encoding.GetEncoding( "shift_jis", EncoderFallback.ReplacementFallback, new DecoderReplacementFallback( ) );
			}
			catch
			{
				encoding = Encoding.UTF8;
			}

			string[] cmds = FanucHelper.GetFanucCmds( );
			int[] indexs = new int[cmds.Length - 1];
			int[] length = new int[cmds.Length - 1];
			for (int i = 1; i < cmds.Length; i++)
			{
				var matches = Regex.Matches( cmds[i], "[0-9]+" );
				indexs[i - 1] = (int.Parse( matches[0].Value ) - 1) * 2;
				length[i - 1] = int.Parse( matches[1].Value ) * 2;
			}

			AlarmList            = GetFanucAlarmArray(         byteTransform, content, indexs[0], 5, encoding );
			AlarmCurrent         = FanucAlarm.PraseFrom( byteTransform, content, indexs[1], encoding );
			AlarmPassword        = FanucAlarm.PraseFrom( byteTransform, content, indexs[2], encoding );
			CurrentPose          = FanucPose.PraseFrom(  byteTransform, content, indexs[3] );
			CurrentPoseUF        = FanucPose.PraseFrom(  byteTransform, content, indexs[4] );
			CurrentPose2         = FanucPose.PraseFrom(  byteTransform, content, indexs[5] );
			CurrentPose3         = FanucPose.PraseFrom(  byteTransform, content, indexs[6] );
			CurrentPose4         = FanucPose.PraseFrom(  byteTransform, content, indexs[7] );
			CurrentPose5         = FanucPose.PraseFrom(  byteTransform, content, indexs[8] );
			Task                 = FanucTask.PraseFrom(  byteTransform, content, indexs[9], encoding );
			TaskIgnoreMacro      = FanucTask.PraseFrom(  byteTransform, content, indexs[10], encoding );
			TaskIgnoreKarel      = FanucTask.PraseFrom(  byteTransform, content, indexs[11], encoding );
			TaskIgnoreMacroKarel = FanucTask.PraseFrom(  byteTransform, content, indexs[12], encoding );
			PosRegGP1            = GetFanucPoseArray(          byteTransform, content, indexs[13], 10, encoding );
			PosRegGP2            = GetFanucPoseArray(          byteTransform, content, indexs[14], 4, encoding );
			PosRegGP3            = GetFanucPoseArray(          byteTransform, content, indexs[15], 10, encoding );
			PosRegGP4            = GetFanucPoseArray(          byteTransform, content, indexs[16], 10, encoding );
			PosRegGP5            = GetFanucPoseArray(          byteTransform, content, indexs[17], 10, encoding );
			FAST_CLOCK           = BitConverter.ToInt32(       content, indexs[18] );
			Timer10_TIMER_VAL    = BitConverter.ToInt32(       content, indexs[19] );
			MOR_GRP_CURRENT_ANG  = BitConverter.ToSingle(      content, indexs[20] );
			DUTY_TEMP            = BitConverter.ToSingle(      content, indexs[21] );
			TIMER10_COMMENT      = encoding.GetString(         content, indexs[22], 80 ).Trim( '\u0000' );
			TIMER2_COMMENT       = encoding.GetString(         content, indexs[23], 80 ).Trim( '\u0000' );
			MNUTOOL1_1           = FanucPose.PraseFrom(  byteTransform, content, indexs[24] );
			HTTPKCL_CMDS         = encoding.GetString(         content, indexs[25], 80 ).Trim( '\u0000' );
			NumReg1              = byteTransform.TransInt32(   content, indexs[26], 5 );
			NumReg2              = byteTransform.TransSingle(  content, indexs[27], 5 );
			DataPosRegMG = new FanucPose[10];
			for (int i = 0; i < DataPosRegMG.Length; i++)
			{
				DataPosRegMG[i] = new FanucPose( );
				DataPosRegMG[i].Xyzwpr = byteTransform.TransSingle( content, indexs[29] + i * 50, 9 );
				DataPosRegMG[i].Config = FanucPose.TransConfigStringArray( byteTransform.TransInt16( content, indexs[29] + 36 + i * 50, 7 ) );
				DataPosRegMG[i].Joint = byteTransform.TransSingle( content, indexs[30] + i * 36, 9 );
			}
			DIComment              = GetStringArray(           content, indexs[31], 80, 3, encoding );
			DOComment              = GetStringArray(           content, indexs[32], 80, 3, encoding );
			RIComment              = GetStringArray(           content, indexs[33], 80, 3, encoding );
			ROComment              = GetStringArray(           content, indexs[34], 80, 3, encoding );
			UIComment              = GetStringArray(           content, indexs[35], 80, 3, encoding );
			UOComment              = GetStringArray(           content, indexs[36], 80, 3, encoding );
			SIComment              = GetStringArray(           content, indexs[37], 80, 3, encoding );
			SOComment              = GetStringArray(           content, indexs[38], 80, 3, encoding );
			WIComment              = GetStringArray(           content, indexs[39], 80, 3, encoding );
			WOComment              = GetStringArray(           content, indexs[40], 80, 3, encoding );
			WSIComment             = GetStringArray(           content, indexs[41], 80, 3, encoding );
			AIComment              = GetStringArray(           content, indexs[42], 80, 3, encoding );
			AOComment              = GetStringArray(           content, indexs[43], 80, 3, encoding );
			GIComment              = GetStringArray(           content, indexs[44], 80, 3, encoding );
			GOComment              = GetStringArray(           content, indexs[45], 80, 3, encoding );
			STRREGComment          = GetStringArray(           content, indexs[46], 80, 3, encoding );
			STRREG_COMMENT_Comment = GetStringArray(           content, indexs[47], 80, 3, encoding );

			isIni = true;
		}

		/// <inheritdoc/>
		public override string ToString( )
		{
			if (!isIni) return "NULL";

			StringBuilder sb = new StringBuilder( );
			AppendStringBuilder( sb, "AlarmList",     AlarmList.Select( m => m.ToString( ) ).ToArray( ) );
			AppendStringBuilder( sb, "AlarmCurrent",  AlarmCurrent.ToString( ) );
			AppendStringBuilder( sb, "AlarmPassword", AlarmPassword.ToString( ) );
			AppendStringBuilder( sb, "CurrentPose",   CurrentPose.ToString( ) );
			AppendStringBuilder( sb, "CurrentPoseUF", CurrentPoseUF.ToString( ) );
			AppendStringBuilder( sb, "CurrentPose2", CurrentPose2.ToString( ) );
			AppendStringBuilder( sb, "CurrentPose3", CurrentPose3.ToString( ) );
			AppendStringBuilder( sb, "CurrentPose4", CurrentPose4.ToString( ) );
			AppendStringBuilder( sb, "CurrentPose5", CurrentPose5.ToString( ) );
			AppendStringBuilder( sb, "Task", Task.ToString( ) );
			AppendStringBuilder( sb, "TaskIgnoreMacro", TaskIgnoreMacro.ToString( ) );
			AppendStringBuilder( sb, "TaskIgnoreKarel", TaskIgnoreKarel.ToString( ) );
			AppendStringBuilder( sb, "TaskIgnoreMacroKarel", TaskIgnoreMacroKarel.ToString( ) );
			AppendStringBuilder( sb, "PosRegGP1", PosRegGP1.Select( m => m.ToString( ) ).ToArray( ) );
			AppendStringBuilder( sb, "PosRegGP2", PosRegGP2.Select( m => m.ToString( ) ).ToArray( ) );
			AppendStringBuilder( sb, "PosRegGP3", PosRegGP3.Select( m => m.ToString( ) ).ToArray( ) );
			AppendStringBuilder( sb, "PosRegGP4", PosRegGP4.Select( m => m.ToString( ) ).ToArray( ) );
			AppendStringBuilder( sb, "PosRegGP5", PosRegGP5.Select( m => m.ToString( ) ).ToArray( ) );
			AppendStringBuilder( sb, "FAST_CLOCK", FAST_CLOCK.ToString( ) );
			AppendStringBuilder( sb, "Timer10_TIMER_VAL", Timer10_TIMER_VAL.ToString( ) );
			AppendStringBuilder( sb, "MOR_GRP_CURRENT_ANG", MOR_GRP_CURRENT_ANG.ToString( ) );
			AppendStringBuilder( sb, "DUTY_TEMP", DUTY_TEMP.ToString( ) );
			AppendStringBuilder( sb, "TIMER10_COMMENT", TIMER10_COMMENT.ToString( ) );
			AppendStringBuilder( sb, "TIMER2_COMMENT", TIMER2_COMMENT.ToString( ) );
			AppendStringBuilder( sb, "MNUTOOL1_1", MNUTOOL1_1.ToString( ) );
			AppendStringBuilder( sb, "HTTPKCL_CMDS", HTTPKCL_CMDS.ToString( ) );
			AppendStringBuilder( sb, "NumReg1", SoftBasic.ArrayFormat( NumReg1 ) );
			AppendStringBuilder( sb, "NumReg2", SoftBasic.ArrayFormat( NumReg2 ) );
			AppendStringBuilder( sb, "DataPosRegMG", DataPosRegMG.Select( m => m.ToString( ) ).ToArray( ) );
			AppendStringBuilder( sb, "DIComment", SoftBasic.ArrayFormat( DIComment ) );
			AppendStringBuilder( sb, "DOComment", SoftBasic.ArrayFormat( DOComment ) );
			AppendStringBuilder( sb, "RIComment", SoftBasic.ArrayFormat( RIComment ) );
			AppendStringBuilder( sb, "ROComment", SoftBasic.ArrayFormat( ROComment ) );
			AppendStringBuilder( sb, "UIComment", SoftBasic.ArrayFormat( UIComment ) );
			AppendStringBuilder( sb, "UOComment", SoftBasic.ArrayFormat( UOComment ) );
			AppendStringBuilder( sb, "SIComment", SoftBasic.ArrayFormat( SIComment ) );
			AppendStringBuilder( sb, "SOComment", SoftBasic.ArrayFormat( SOComment ) );
			AppendStringBuilder( sb, "WIComment", SoftBasic.ArrayFormat( WIComment ) );
			AppendStringBuilder( sb, "WOComment", SoftBasic.ArrayFormat( WOComment ) );
			AppendStringBuilder( sb, "WSIComment", SoftBasic.ArrayFormat( WSIComment ) );
			AppendStringBuilder( sb, "AIComment", SoftBasic.ArrayFormat( AIComment ) );
			AppendStringBuilder( sb, "AOComment", SoftBasic.ArrayFormat( AOComment ) );
			AppendStringBuilder( sb, "GIComment", SoftBasic.ArrayFormat( GIComment ) );
			AppendStringBuilder( sb, "GOComment", SoftBasic.ArrayFormat( GOComment ) );
			AppendStringBuilder( sb, "STRREGComment", SoftBasic.ArrayFormat( STRREGComment ) );
			AppendStringBuilder( sb, "STRREG_COMMENT_Comment", SoftBasic.ArrayFormat( STRREG_COMMENT_Comment ) );

			return sb.ToString( );
		}

#pragma warning restore CS1591 // 缺少对公共可见类型或成员的 XML 注释

		#region Private Member

		private bool isIni = false;

		#endregion

		#region Static Method

		/// <summary>
		/// 从字节数组解析出fanuc的数据信息
		/// </summary>
		/// <param name="content">原始的字节数组</param>
		/// <returns>fanuc数据</returns>
		public static OperateResult<FanucData> PraseFrom( byte[] content )
		{
			FanucData fanucData = new FanucData( );
			fanucData.LoadByContent( content );
			return OperateResult.CreateSuccessResult( fanucData );
		}

		private static void AppendStringBuilder(StringBuilder sb, string name, string value )
		{
			AppendStringBuilder( sb, name, new string[] { value } );
		}

		private static void AppendStringBuilder( StringBuilder sb, string name, string[] values )
		{
			sb.Append( name );
			sb.Append( ":" );
			if (values.Length > 1) sb.Append( Environment.NewLine );
			for (int i = 0; i < values.Length; i++)
			{
				sb.Append( values[i] );
				sb.Append( Environment.NewLine );
			}
			if (values.Length > 1) sb.Append( Environment.NewLine );
		}
		private static string[] GetStringArray( byte[] content, int index, int length, int arraySize, Encoding encoding )
		{
			string[] array = new string[arraySize];
			for (int i = 0; i < arraySize; i++)
				array[i] = encoding.GetString( content, index + length * i, length ).TrimEnd( '\u0000' );
			return array;
		}

		private static FanucPose[] GetFanucPoseArray( IByteTransform byteTransform, byte[] content, int index, int arraySize, Encoding encoding )
		{
			FanucPose[] array = new FanucPose[arraySize];
			for (int i = 0; i < arraySize; i++)
				array[i] = FanucPose.PraseFrom( byteTransform, content, index + i * 100 );
			return array;
		}

		private static FanucAlarm[] GetFanucAlarmArray( IByteTransform byteTransform, byte[] content, int index, int arraySize, Encoding encoding )
		{
			FanucAlarm[] array = new FanucAlarm[arraySize];
			for (int i = 0; i < arraySize; i++)
				array[i] = FanucAlarm.PraseFrom( byteTransform, content, index + 200 * i, encoding );
			return array;
		}

		#endregion

	}
}
