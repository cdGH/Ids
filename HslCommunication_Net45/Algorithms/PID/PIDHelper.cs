using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HslCommunication.Algorithms.PID
{
	/// <summary>
	/// 一个PID的辅助类，可以设置 P,I,D 三者的值，用来模拟信号波动的时候，信号的收敛情况
	/// </summary>
	public class PIDHelper
	{
		#region Constructor

		/// <summary>
		/// 实例化一个默认的对象
		/// </summary>
		public PIDHelper( )
		{
			PidInit( );
		}

		#endregion

		#region Private Method

		/// <summary>
		/// 初始化PID的数据信息
		/// </summary>
		private void PidInit( )
		{
			prakp = 0;
			praki = 0;
			prakd = 0;
			prvalue = 0;
			err = 0;
			err_last = 0;
			err_next = 0;
			MAXLIM = double.MaxValue;
			MINLIM = double.MinValue;
			UMAX = 310;
			UMIN = -100;
			deadband = 2;
		}

		#endregion

		#region Public Properties

		/// <summary>
		/// -rando
		/// 比例的参数信息
		/// </summary>
		public double Kp
		{
			set => prakp = value;
			get => prakp;
		}

		/// <summary>
		/// 积分的参数信息
		/// </summary>
		public double Ki
		{
			set => praki = value;
			get => praki;
		}

		/// <summary>
		/// 微分的参数信息
		/// </summary>
		public double Kd
		{
			set => prakd = value;
			get => prakd;
		}

		/// <summary>
		/// 获取或设置死区的值
		/// </summary>
		public double DeadBand
		{
			set => deadband = value;
			get => deadband;
		}

		/// <summary>
		/// 获取或设置输出的上限，默认为没有设置
		/// </summary>
		public double MaxLimit
		{
			set => MAXLIM = value;
			get => MAXLIM;
		}

		/// <summary>
		/// 获取或设置输出的下限，默认为没有设置
		/// </summary>
		public double MinLimit
		{
			set => MINLIM = value;
			get => MINLIM;
		}

		/// <summary>
		/// 获取或设置当前设置的值
		/// </summary>
		public double SetValue
		{
			set => setValue = value;
			get => setValue;
		}

		#endregion

		#region Public Method

		/// <summary>
		/// 计算Pid数据的值
		/// </summary>
		/// <returns>计算值</returns>
		public double PidCalculate( )
		{
			err_next = err_last;        //前两次的误差  
			err_last = err;             //前一次的误差  
			err = SetValue - prvalue;   //现在的误差  

			//增量式计算  
			prvalue += prakp * ((err - err_last) + praki * err + prakd * (err - 2 * err_last + err_next));
			//输出上下限值  
			if (prvalue > MAXLIM) prvalue = MAXLIM;
			if (prvalue < MINLIM) prvalue = MINLIM;

			return prvalue;

#pragma warning disable CS0162 // 检测到无法访问的代码
			err_next = err_last;
			err_last = err;
			err = setValue - prvalue;
			// 抗积分饱和  
			if (prvalue > UMAX)
			{

				if (err < 0)
					index = 1;
				else
					index = 0;
			}
			else if (prvalue < UMIN)
			{
				if (err > 0)
					index = 1;
				else
					index = 0;
			}
			// 积分分离  
			else
			{
				if (Math.Abs( err ) > 0.8 * setValue)
					index = 0;
				else
					index = 1;
			}
			// 死区  
			if (Math.Abs( err ) > deadband)
				prvalue += prakp * ((err - err_last) + index * praki * err + prakd * (err - 2 * err_last + err_next));

			else
				prvalue += 0;
			// 输出上下限制  
			if (prvalue > MAXLIM)
				prvalue = MAXLIM;
			if (prvalue < MINLIM)
				prvalue = MINLIM;

			return prvalue;
#pragma warning restore CS0162 // 检测到无法访问的代码
		}

		#endregion

		#region Private Member

		private double prakp, praki, prakd, prvalue, err, err_last, err_next, setValue, deadband, MAXLIM, MINLIM;
		private int index, UMAX, UMIN;

		#endregion
	}
}
