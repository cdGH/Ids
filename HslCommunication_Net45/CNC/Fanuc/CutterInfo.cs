using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HslCommunication.CNC.Fanuc
{
	/// <summary>
	/// 刀具信息
	/// </summary>
	public class CutterInfo
	{
		/// <summary>
		/// 长度形状补偿
		/// </summary>
		public double LengthSharpOffset { get; set; }

		/// <summary>
		/// 长度磨损补偿
		/// </summary>
		public double LengthWearOffset { get; set; }

		/// <summary>
		/// 半径形状补偿
		/// </summary>
		public double RadiusSharpOffset { get; set; }

		/// <summary>
		/// 半径磨损补偿
		/// </summary>
		public double RadiusWearOffset { get; set; }

		/// <inheritdoc/>
		public override string ToString( )
		{
			return $"LengthSharpOffset:{LengthSharpOffset:10} LengthWearOffset:{LengthWearOffset:10} RadiusSharpOffset:{RadiusSharpOffset:10} RadiusWearOffset:{RadiusWearOffset:10}";
		}
	}
}
