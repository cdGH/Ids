using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HslCommunication.Core;

namespace HslCommunication.Instrument.Delixi
{
	/// <summary>
	/// 电参数类
	/// </summary>
	public class ElectricalParameters
	{
		/// <summary>
		/// A相电压，单位V
		/// </summary>
		public float VoltageA { get; set; }

		/// <summary>
		/// B相电压，单位V
		/// </summary>
		public float VoltageB { get; set; }

		/// <summary>
		/// C相电压，单位V
		/// </summary>
		public float VoltageC { get; set; }

		/// <summary>
		/// A相电流，单位A
		/// </summary>
		public float CurrentA { get; set; }

		/// <summary>
		/// B相电流，单位A
		/// </summary>
		public float CurrentB { get; set; }

		/// <summary>
		/// C相电流，单位A
		/// </summary>
		public float CurrentC { get; set; }

		/// <summary>
		/// 瞬时A相有功功率，单位 kw
		/// </summary>
		public float InstantaneousActivePowerA { get; set; }

		/// <summary>
		/// 瞬时B相有功功率，单位 kw
		/// </summary>
		public float InstantaneousActivePowerB { get; set; }

		/// <summary>
		/// 瞬时C相有功功率，单位 kw
		/// </summary>
		public float InstantaneousActivePowerC { get; set; }

		/// <summary>
		/// 瞬时总有功功率，单位 kw
		/// </summary>
		public float InstantaneousTotalActivePower { get; set; }

		/// <summary>
		/// 瞬时A相无功功率，单位 kvar
		/// </summary>
		public float InstantaneousReactivePowerA { get; set; }

		/// <summary>
		/// 瞬时B相无功功率，单位 kvar
		/// </summary>
		public float InstantaneousReactivePowerB { get; set; }

		/// <summary>
		/// 瞬时C相无功功率，单位 kvar
		/// </summary>
		public float InstantaneousReactivePowerC { get; set; }

		/// <summary>
		/// 瞬时总无功功率，单位 kvar
		/// </summary>
		public float InstantaneousTotalReactivePower { get; set; }

		/// <summary>
		/// 瞬时A相视在功率，单位 kVA
		/// </summary>
		public float InstantaneousApparentPowerA { get; set; }

		/// <summary>
		/// 瞬时B相视在功率，单位 kVA
		/// </summary>
		public float InstantaneousApparentPowerB { get; set; }

		/// <summary>
		/// 瞬时C相视在功率，单位 kVA
		/// </summary>
		public float InstantaneousApparentPowerC { get; set; }

		/// <summary>
		/// 瞬时总视在功率，单位 kVA
		/// </summary>
		public float InstantaneousTotalApparentPower { get; set; }

		/// <summary>
		/// A相功率因数
		/// </summary>
		public float PowerFactorA { get; set; }

		/// <summary>
		/// B相功率因数
		/// </summary>
		public float PowerFactorB { get; set; }

		/// <summary>
		/// C相功率因数
		/// </summary>
		public float PowerFactorC { get; set; }

		/// <summary>
		/// 总功率因数
		/// </summary>
		public float TotalPowerFactor { get; set; }

		/// <summary>
		/// 频率，Hz
		/// </summary>
		public float Frequency { get; set; }

		#region Static Parse

		/// <summary>
		/// 根据德力西电表的原始字节数据，解析出真实的电量参数信息
		/// </summary>
		/// <param name="data">原始的字节数据</param>
		/// <param name="byteTransform">字节变换操作</param>
		/// <returns>掂量参数信息</returns>
		public static ElectricalParameters ParseFromDelixi( byte[] data, IByteTransform byteTransform )
		{
			ElectricalParameters electrical = new ElectricalParameters( );
			electrical.VoltageA = byteTransform.TransInt16( data,  0 ) / 10f;
			electrical.VoltageB = byteTransform.TransInt16( data,  2 ) / 10f;
			electrical.VoltageC = byteTransform.TransInt16( data,  4 ) / 10f;
			electrical.CurrentA = byteTransform.TransInt16( data,  6 ) / 100f;
			electrical.CurrentB = byteTransform.TransInt16( data,  8 ) / 100f;
			electrical.CurrentC = byteTransform.TransInt16( data, 10 ) / 100f;
			electrical.InstantaneousActivePowerA = byteTransform.TransInt16( data, 12 ) / 100f;
			electrical.InstantaneousActivePowerB = byteTransform.TransInt16( data, 14 ) / 100f;
			electrical.InstantaneousActivePowerC = byteTransform.TransInt16( data, 16 ) / 100f;
			electrical.InstantaneousTotalActivePower = byteTransform.TransInt16( data, 18 ) / 100f;
			electrical.InstantaneousReactivePowerA = byteTransform.TransInt16( data, 20 ) / 100f;
			electrical.InstantaneousReactivePowerB = byteTransform.TransInt16( data, 22 ) / 100f;
			electrical.InstantaneousReactivePowerC = byteTransform.TransInt16( data, 24 ) / 100f;
			electrical.InstantaneousTotalReactivePower = byteTransform.TransInt16( data, 26 ) / 100f;
			electrical.InstantaneousApparentPowerA = byteTransform.TransInt16( data, 28 ) / 100f;
			electrical.InstantaneousApparentPowerB = byteTransform.TransInt16( data, 30 ) / 100f;
			electrical.InstantaneousApparentPowerC = byteTransform.TransInt16( data, 32 ) / 100f;
			electrical.InstantaneousTotalApparentPower = byteTransform.TransInt16( data, 34 ) / 100f;
			electrical.PowerFactorA = byteTransform.TransInt16( data, 36 ) / 1000f;
			electrical.PowerFactorB = byteTransform.TransInt16( data, 38 ) / 1000f;
			electrical.PowerFactorC = byteTransform.TransInt16( data, 40 ) / 1000f;
			electrical.TotalPowerFactor = byteTransform.TransInt16( data, 42 ) / 1000f;
			electrical.Frequency = byteTransform.TransInt16( data, 44 ) / 100f;
			return electrical;
		}

		#endregion
	}
}
