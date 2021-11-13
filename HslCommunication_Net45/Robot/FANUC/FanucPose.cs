using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HslCommunication.BasicFramework;
using HslCommunication.Core;
#if !NET35 && !NET20
using System.Threading.Tasks;
#endif

namespace HslCommunication.Robot.FANUC
{
	/// <summary>
	/// 机器人的姿态数据
	/// </summary>
	public class FanucPose
	{
		/// <summary>
		/// Xyzwpr
		/// </summary>
		public float[] Xyzwpr { get; set; }

		/// <summary>
		/// Config
		/// </summary>
		public string[] Config { get; set; }

		/// <summary>
		/// Joint
		/// </summary>
		public float[] Joint { get; set; }

		/// <summary>
		/// UF
		/// </summary>
		public short UF { get; set; }

		/// <summary>
		/// UT
		/// </summary>
		public short UT { get; set; }

		/// <summary>
		/// ValidC
		/// </summary>
		public short ValidC { get; set; }

		/// <summary>
		/// ValidJ
		/// </summary>
		public short ValidJ { get; set; }

		/// <summary>
		/// 从原始数据解析出当前的姿态数据
		/// </summary>
		/// <param name="byteTransform">字节变化内容</param>
		/// <param name="content">原始的内容</param>
		/// <param name="index">索引位置</param>
		public void LoadByContent( IByteTransform byteTransform, byte[] content, int index )
		{
			Xyzwpr = new float[9];
			for (int i = 0; i < Xyzwpr.Length; i++)
			{
				Xyzwpr[i] = BitConverter.ToSingle( content, index + 4 * i );
			}
			Config = TransConfigStringArray( byteTransform.TransInt16( content, index + 36, 7 ) );

			Joint = new float[9];
			for (int i = 0; i < Joint.Length; i++)
			{
				Joint[i] = BitConverter.ToSingle( content, index + 52 + 4 * i );
			}

			ValidC = BitConverter.ToInt16( content, index + 50 );
			ValidJ = BitConverter.ToInt16( content, index + 88 );
			UF = BitConverter.ToInt16( content, index + 90 );
			UT = BitConverter.ToInt16( content, index + 92 );
		}

		/// <inheritdoc/>
		public override string ToString( )
		{
			StringBuilder sb = new StringBuilder( $"FanucPose UF={UF} UT={UT}" );
			if (ValidC != 0)
			{
				sb.Append( $"\r\nXyzwpr={SoftBasic.ArrayFormat( Xyzwpr )}\r\nConfig={SoftBasic.ArrayFormat( Config )}" );
			}
			if (ValidJ != 0)
			{
				sb.Append( $"\r\nJOINT={SoftBasic.ArrayFormat( Joint )}" );
			}
			return sb.ToString( );
		}

		/// <summary>
		/// 从原始的字节数据创建一个新的姿态数据
		/// </summary>
		/// <param name="byteTransform"></param>
		/// <param name="content">原始的内容</param>
		/// <param name="index">索引位置</param>
		/// <returns>姿态数据</returns>
		public static FanucPose PraseFrom( IByteTransform byteTransform, byte[] content, int index )
		{
			FanucPose fanucPose = new FanucPose( );
			fanucPose.LoadByContent( byteTransform, content, index );
			return fanucPose;
		}

		/// <summary>
		/// 将short类型的config数组转换成string数组类型的config
		/// </summary>
		/// <param name="value">short数组的值</param>
		/// <returns>string数组的值</returns>
		public static string[] TransConfigStringArray( short[] value )
		{
			string[] array = new string[7];
			array[0] = value[0] != 0 ? "F" : "N";
			array[1] = value[1] != 0 ? "L" : "R";
			array[2] = value[2] != 0 ? "U" : "D";
			array[3] = value[3] != 0 ? "T" : "B";
			array[4] = value[4].ToString( );
			array[5] = value[5].ToString( );
			array[6] = value[6].ToString( );
			return array;
		}
	}
}
