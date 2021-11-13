using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HslCommunication.Core.Address
{
	/// <summary>
	/// 所有设备通信类的地址基础类<br />
	/// Address basic class of all device communication classes
	/// </summary>
	public class DeviceAddressBase
	{
		/// <summary>
		/// 获取或设置起始地址<br />
		/// Get or set the starting address
		/// </summary>
		public ushort Address { get; set; }

		/// <summary>
		/// 解析字符串的地址<br />
		/// Parse the address of the string
		/// </summary>
		/// <param name="address">地址信息</param>
		public virtual void Parse( string address )
		{
			Address = ushort.Parse( address );
		}

		/// <inheritdoc/>
		public override string ToString( ) => Address.ToString( );
	}
}
