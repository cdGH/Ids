using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HslCommunication.Algorithms.Fourier
{
	/// <summary>
	/// 一个基于傅立叶变换的一个滤波算法
	/// </summary>
	/// <remarks>
	/// 非常感谢来自北京的monk网友，提供了完整的解决方法。
	/// </remarks>
	public class FFTFilter
	{
		/// <summary>
		/// 对指定的数据进行填充，方便的进行傅立叶计算
		/// </summary>
		/// <typeparam name="T">数据的数据类型</typeparam>
		/// <param name="source">数据源</param>
		/// <param name="putLength">输出的长度</param>
		/// <returns>填充结果</returns>
		public static List<T> FillDataArray<T>( List<T> source, out int putLength)
		{
			int length = (int)(Math.Pow( 2d, Convert.ToString( source.Count, 2 ).Length ) - source.Count);
			length = length / 2 + 1;
			putLength = length;
			T begin = source[0];
			T end = source[source.Count - 1];
			for (int i = 0; i < length; i++)
			{
				source.Insert( 0, begin );
			}
			for (int i = 0; i < length; i++)
			{
				source.Add( end );
			}
			return source;
		}

		/// <summary>
		/// 对指定的原始数据进行滤波，并返回成功的数据值
		/// </summary>
		/// <param name="source">数据源，数组的长度需要为2的n次方。</param>
		/// <param name="filter">滤波值：最大值为1，不能低于0，越接近1，滤波强度越强，也可能会导致失去真实信号，为0时没有滤波效果。</param>
		/// <returns>滤波后的数据值</returns>
		public static double[] FilterFFT( double[] source, double filter )
		{
			var r = new double[source.Length];
			var Y = FillDataArray( new List<double>( source ), out int putlen );
			var y = Filter( Y.ToArray( ), filter );

			for (int i = 0; i < r.Length; i++)
				r[i] = y[i + putlen];
			return r;
		}

		/// <summary>
		/// 对指定的原始数据进行滤波，并返回成功的数据值
		/// </summary>
		/// <param name="source">数据源，数组的长度需要为2的n次方。</param>
		/// <param name="filter">滤波值：最大值为1，不能低于0，越接近1，滤波强度越强，也可能会导致失去真实信号，为0时没有滤波效果。</param>
		/// <returns>滤波后的数据值</returns>
		public static float[] FilterFFT( float[] source, double filter )
		{
			var r = new float[source.Length];
			var Y = FillDataArray( new List<float>( source ), out int putlen );
			var y = Filter( Y.ToArray( ), filter );

			for (int i = 0; i < r.Length; i++)
				r[i] = y[i + putlen];
			return r;
		}

		#region

		/// <summary>
		/// 对指定的原始数据进行滤波，并返回成功的数据值
		/// </summary>
		/// <param name="source">数据源，数组的长度需要为2的n次方。</param>
		/// <param name="filter">滤波值：最大值为1，不能低于0，越接近1，滤波强度越强，也可能会导致失去真实信号，为0时没有滤波效果。</param>
		/// <returns>滤波后的数据值</returns>
		private static float[] Filter( float[] source, double filter )
		{
			return Filter( source.Select( m => (double)m ).ToArray( ), filter );
		}

		/// <summary>
		/// 对指定的原始数据进行滤波，并返回成功的数据值
		/// </summary>
		/// <param name="source">数据源，数组的长度需要为2的n次方。</param>
		/// <param name="filter">滤波值：最大值为1，不能低于0，越接近1，滤波强度越强，也可能会导致失去真实信号，为0时没有滤波效果。</param>
		/// <returns>滤波后的数据值</returns>
		private static float[] Filter( double[] source, double filter )
		{
			if (filter > 1) filter = 1;
			if (filter < 0) filter = 0;

			// a是实部、b是虚部
			double[] a = new double[source.Length];
			double[] b = new double[source.Length];
			List<double> ans = new List<double>( );
			for (int i = 0; i < source.Length; i++)
			{
				a[i] = source[i];
				b[i] = 0.0f;
			}

			// int length = FFTHelper.FFT( a, b );
			double[] fft = FFTHelper.FFTValue( a, b );
			int length = fft.Length;
			double fftMax = fft.Max( );
			for (int i = 0; i < fft.Length; i++)
			{
				if(fft[i] < fftMax * filter)
				{
					a[i] = 0d;
					b[i] = 0d;
				}
			}

			length = FFTHelper.IFFT( a, b );
			for (int i = 0; i < length; i++)
			{
				ans.Add( Math.Sqrt( a[i] * a[i] + b[i] * b[i] ) );
			}

			return ans.Select( m => (float)m ).ToArray( );
		}

		#endregion

	}
}
