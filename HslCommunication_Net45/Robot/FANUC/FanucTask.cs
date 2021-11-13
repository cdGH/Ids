using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HslCommunication.Core;

namespace HslCommunication.Robot.FANUC
{
	/// <summary>
	/// Fanuc机器人的任务类
	/// </summary>
	public class FanucTask
	{
		/// <summary>
		/// ProgramName
		/// </summary>
		public string ProgramName { get; set; }

		/// <summary>
		/// LineNumber
		/// </summary>
		public short LineNumber { get; set; }

		/// <summary>
		/// State
		/// </summary>
		public short State { get; set; }

		/// <summary>
		/// ParentProgramName
		/// </summary>
		public string ParentProgramName { get; set; }

		/// <summary>
		/// 从原始的数据对象加载数据信息
		/// </summary>
		/// <param name="byteTransform">字节变换</param>
		/// <param name="content">原始的字节数据</param>
		/// <param name="index">索引信息</param>
		/// <param name="encoding">编码</param>
		public void LoadByContent( IByteTransform byteTransform, byte[] content, int index, Encoding encoding )
		{
			ProgramName       = encoding.GetString( content, index, 16 ).Trim( '\u0000' );
			LineNumber        = BitConverter.ToInt16( content, index + 16 );
			State             = BitConverter.ToInt16( content, index + 18 );
			ParentProgramName = encoding.GetString( content, index + 20, 16 ).Trim( '\u0000' );
		}

		/// <inheritdoc/>
		public override string ToString( ) => $"ProgramName[{ProgramName}] LineNumber[{LineNumber}] State[{State}] ParentProgramName[{ParentProgramName}]";

		/// <summary>
		/// 从原始的数据信息初始化一个任务对象
		/// </summary>
		/// <param name="byteTransform">字节变换</param>
		/// <param name="content">原始的字节数据</param>
		/// <param name="index">索引信息</param>
		/// <param name="encoding">编码</param>
		/// <returns>任务对象</returns>
		public static FanucTask PraseFrom( IByteTransform byteTransform, byte[] content, int index, Encoding encoding )
		{
			FanucTask fanucTask = new FanucTask( );
			fanucTask.LoadByContent( byteTransform, content, index, encoding );
			return fanucTask;
		}
	}
}
