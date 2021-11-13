using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
#if !NET35 && !NET20
using System.Threading.Tasks;
#endif

namespace HslCommunication.Profinet.Beckhoff
{
	/// <summary>
	/// Ads设备的相关信息，主要是版本号，设备名称<br />
	/// Information about Ads devices, primarily version numbers, device names.
	/// </summary>
	public class AdsDeviceInfo
	{
		/// <summary>
		/// 实例化一个默认的对象<br />
		/// Instantiate a default object
		/// </summary>
		public AdsDeviceInfo( )
		{

		}

		/// <summary>
		/// 根据原始的数据内容来实例化一个对象<br />
		/// Instantiate an object based on the original data content
		/// </summary>
		/// <param name="data">原始的数据内容</param>
		public AdsDeviceInfo( byte[] data )
		{
			Major = data[0];
			Minor = data[1];
			Build = BitConverter.ToUInt16( data, 2 );
			DeviceName = Encoding.ASCII.GetString( data.RemoveBegin( 4 ) ).Trim( '\0', ' ' );
		}

		/// <summary>
		/// 主版本号<br />
		/// Main Version
		/// </summary>
		public byte Major { get; set; }

		/// <summary>
		/// 次版本号<br />
		/// Minor Version
		/// </summary>
		public byte Minor { get; set; }

		/// <summary>
		/// 构建版本号<br />
		/// Build version
		/// </summary>
		public ushort Build { get; set; }

		/// <summary>
		/// 设备的名字<br />
		/// Device Name
		/// </summary>
		public string DeviceName { get; set; }
	}
}
