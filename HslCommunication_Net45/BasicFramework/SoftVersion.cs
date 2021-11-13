using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HslCommunication.BasicFramework
{
	/****************************************************************************
	 * 
	 *    系统的版本号类，版本号命名规则，应遵循兼容性规则
	 * 
	 * 
	 * 
	 ***************************************************************************/



	/// <summary>
	/// 系统版本类，由三部分组成，包含了一个大版本，小版本，修订版，还有一个开发者维护的内部版<br />
	/// System version class, consisting of three parts, including a major version, minor version, revised version, and an internal version maintained by the developer
	/// </summary>
	[Serializable]
	public sealed class SystemVersion
	{
		#region Constructor

		/// <summary>
		/// 根据格式化字符串的版本号初始化，例如：1.0或1.0.0或1.0.0.0503<br />
		/// Initialize according to the version number of the formatted string, for example: 1.0 or 1.0.0 or 1.0.0.0503
		/// </summary>
		/// <param name="VersionString">格式化的字符串，例如：1.0或1.0.0或1.0.0.0503</param>
		public SystemVersion( string VersionString )
		{
			string[] temp = VersionString.Split( new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries );

			if (temp.Length >= 1) m_MainVersion = Convert.ToInt32( temp[0] );
			if (temp.Length >= 2) m_SecondaryVersion = Convert.ToInt32( temp[1] );
			if (temp.Length >= 3) m_EditVersion = Convert.ToInt32( temp[2] );
			if (temp.Length >= 4) m_InnerVersion = Convert.ToInt32( temp[3] );
		}

		/// <summary>
		/// 根据指定的主版本，次版本，修订版来实例化一个对象<br />
		/// Instantiate an object based on the specified major, minor, and revision
		/// </summary>
		/// <param name="main">主版本</param>
		/// <param name="sec">次版本</param>
		/// <param name="edit">修订版</param>
		public SystemVersion( int main, int sec, int edit )
		{
			m_MainVersion = main;
			m_SecondaryVersion = sec;
			m_EditVersion = edit;
		}

		/// <summary>
		/// 根据指定的主版本，次版本，修订版，内部版本来实例化一个对象<br />
		/// Instantiate an object based on the specified major, minor, revision, and build
		/// </summary>
		/// <param name="main">主版本</param>
		/// <param name="sec">次版本</param>
		/// <param name="edit">修订版</param>
		/// <param name="inner">内部版本号</param>
		public SystemVersion( int main, int sec, int edit, int inner )
		{
			m_MainVersion = main;
			m_SecondaryVersion = sec;
			m_EditVersion = edit;
			m_InnerVersion = inner;
		}

		#endregion

		#region Private Member

		private int m_MainVersion = 2;                // 主版本
		private int m_SecondaryVersion = 0;           // 次版本
		private int m_EditVersion = 0;                // 修订版
		private int m_InnerVersion = 0;               // 内部版

		#endregion

		#region Public Properties

		/// <summary>
		/// 主版本
		/// </summary>
		public int MainVersion => m_MainVersion;

		/// <summary>
		/// 次版本
		/// </summary>
		public int SecondaryVersion => m_SecondaryVersion;

		/// <summary>
		/// 修订版
		/// </summary>
		public int EditVersion => m_EditVersion;

		/// <summary>
		/// 内部版本号，或者是版本号表示为年月份+内部版本的表示方式
		/// </summary>
		public int InnerVersion => m_InnerVersion;

		#endregion

		#region Object Override

		/// <summary>
		/// 根据格式化为支持返回的不同信息的版本号<br />
		/// C返回1.0.0.0<br />
		/// N返回1.0.0<br />
		/// S返回1.0
		/// </summary>
		/// <param name="format">格式化信息</param>
		/// <returns>版本号信息</returns>
		public string ToString( string format )
		{
			if (format == "C")
			{
				return $"{MainVersion}.{SecondaryVersion}.{EditVersion}.{InnerVersion}";
			}

			if (format == "N")
			{
				return $"{MainVersion}.{SecondaryVersion}.{EditVersion}";
			}

			if (format == "S")
			{
				return $"{MainVersion}.{SecondaryVersion}";
			}

			return ToString( );
		}

		/// <summary>
		/// 获取版本号的字符串形式，如果内部版本号为0，则显示时不携带
		/// </summary>
		/// <returns>版本号信息</returns>
		public override string ToString( )
		{
			if (InnerVersion == 0)
			{
				return ToString( "N" );
			}
			else
			{
				return ToString( "C" );
			}
		}

		/// <summary>
		/// 判断两个实例是否相等
		/// </summary>
		/// <param name="obj">版本号</param>
		/// <returns>是否一致</returns>
		public override bool Equals( object obj )
		{
			return base.Equals( obj );
		}

		/// <summary>
		/// 获取哈希值
		/// </summary>
		/// <returns>哈希值</returns>
		public override int GetHashCode( )
		{
			return base.GetHashCode( );
		}

		#endregion

		#region operator implementation

		/// <summary>
		/// 判断是否相等
		/// </summary>
		/// <param name="SV1">第一个版本</param>
		/// <param name="SV2">第二个版本</param>
		/// <returns>是否相同</returns>
		public static bool operator ==( SystemVersion SV1, SystemVersion SV2 )
		{
			if (SV1.MainVersion != SV2.MainVersion)
			{
				return false;
			}

			if (SV1.SecondaryVersion != SV2.SecondaryVersion)
			{
				return false;
			}

			if (SV1.m_EditVersion != SV2.m_EditVersion)
			{
				return false;
			}

			if (SV1.InnerVersion != SV2.InnerVersion)
			{
				return false;
			}

			return true;
		}

		/// <summary>
		/// 判断是否不相等
		/// </summary>
		/// <param name="SV1">第一个版本号</param>
		/// <param name="SV2">第二个版本号</param>
		/// <returns>是否相同</returns>
		public static bool operator !=( SystemVersion SV1, SystemVersion SV2 )
		{
			if (SV1.MainVersion != SV2.MainVersion)
			{
				return true;
			}

			if (SV1.SecondaryVersion != SV2.SecondaryVersion)
			{
				return true;
			}

			if (SV1.m_EditVersion != SV2.m_EditVersion)
			{
				return true;
			}

			if (SV1.InnerVersion != SV2.InnerVersion)
			{
				return true;
			}

			return false;
		}

		/// <summary>
		/// 判断一个版本是否大于另一个版本
		/// </summary>
		/// <param name="SV1">第一个版本</param>
		/// <param name="SV2">第二个版本</param>
		/// <returns>是否相同</returns>
		public static bool operator >( SystemVersion SV1, SystemVersion SV2 )
		{
			if (SV1.MainVersion > SV2.MainVersion)
			{
				return true;
			}
			if (SV1.MainVersion < SV2.MainVersion)
			{
				return false;
			}



			if (SV1.SecondaryVersion > SV2.SecondaryVersion)
			{
				return true;
			}
			if (SV1.SecondaryVersion < SV2.SecondaryVersion)
			{
				return false;
			}



			if (SV1.EditVersion > SV2.EditVersion)
			{
				return true;
			}
			if (SV1.EditVersion < SV2.EditVersion)
			{
				return false;
			}

			if (SV1.InnerVersion > SV2.InnerVersion)
			{
				return true;
			}
			if (SV1.InnerVersion < SV2.InnerVersion)
			{
				return false;
			}

			return false;
		}

		/// <summary>
		/// 判断第一个版本是否小于第二个版本
		/// </summary>
		/// <param name="SV1">第一个版本号</param>
		/// <param name="SV2">第二个版本号</param>
		/// <returns>是否小于</returns>
		public static bool operator <( SystemVersion SV1, SystemVersion SV2 )
		{
			if (SV1.MainVersion < SV2.MainVersion)
			{
				return true;
			}
			if (SV1.MainVersion > SV2.MainVersion)
			{
				return false;
			}

			if (SV1.SecondaryVersion < SV2.SecondaryVersion)
			{
				return true;
			}
			if (SV1.SecondaryVersion > SV2.SecondaryVersion)
			{
				return false;
			}

			if (SV1.EditVersion < SV2.EditVersion)
			{
				return true;
			}
			if (SV1.EditVersion > SV2.EditVersion)
			{
				return false;
			}

			if (SV1.InnerVersion < SV2.InnerVersion)
			{
				return true;
			}
			if (SV1.InnerVersion > SV2.InnerVersion)
			{
				return false;
			}

			return false;
		}

		#endregion

	}

	/// <summary>
	/// 版本信息类，用于展示版本发布信息
	/// </summary>
	public sealed class VersionInfo
	{
		/// <summary>
		/// 版本的发行日期
		/// </summary>
		public DateTime ReleaseDate { get; set; } = DateTime.Now;

		/// <summary>
		/// 版本的更新细节
		/// </summary>
		public StringBuilder UpdateDetails { get; set; } = new StringBuilder( );

		/// <summary>
		/// 版本号
		/// </summary>
		public SystemVersion VersionNum { get; set; } = new SystemVersion( 1, 0, 0 );

		/// <inheritdoc/>
		public override string ToString( ) => VersionNum.ToString( );
	}
}
