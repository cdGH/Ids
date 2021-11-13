using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HslCommunication.Reflection
{
	/// <summary>
	/// 应用于Hsl组件库读取的动态地址解析，具体用法为创建一个类，创建数据属性，如果这个属性需要绑定PLC的真实数据，就在属性的特性上应用本特性。<br />
	/// Applied to the dynamic address resolution read by the Hsl component library, the specific usage is to create a class and create data attributes. 
	/// If this attribute needs to be bound to the real data of the PLC, this feature is applied to the characteristics of the attribute.
	/// </summary>
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
	public class HslDeviceAddressAttribute : Attribute
	{
		/// <summary>
		/// 设备的类型，如果指定了特殊的PLC，那么该地址就可以支持多种不同PLC<br />
		/// The type of equipment, if a special PLC is specified, then the address can support a variety of different PLCs
		/// </summary>
		public Type DeviceType { get; set; }

		/// <summary>
		/// 数据的地址信息，真实的设备的地址信息<br />
		/// Data address information, real device address information
		/// </summary>
		public string Address { get; }

		/// <summary>
		/// 读取的数据长度<br />
		/// Length of data read
		/// </summary>
		public int Length { get; }

		/// <summary>
		/// 实例化一个地址特性，指定地址信息，用于单变量的数据<br />
		/// Instantiate an address feature, specify the address information, for single variable data
		/// </summary>
		/// <param name="address">真实的地址信息</param>
		public HslDeviceAddressAttribute(string address )
		{
			this.Address = address;
			this.Length = -1;
			this.DeviceType = null;
		}

		/// <summary>
		/// 实例化一个地址特性，指定地址信息，用于单变量的数据，并指定设备类型<br />
		/// Instantiate an address feature, specify address information, data for a single variable, and specify the device type
		/// </summary>
		/// <param name="address">真实的地址信息</param>
		/// <param name="deviceType">设备的地址信息</param>
		public HslDeviceAddressAttribute( string address, Type deviceType )
		{
			this.Address = address;
			this.Length = -1;
			this.DeviceType = deviceType;
		}

		/// <summary>
		/// 实例化一个地址特性，指定地址信息和数据长度，通常应用于数组的批量读取<br />
		/// Instantiate an address feature, specify address information and data length, usually used in batch reading of arrays
		/// </summary>
		/// <param name="address">真实的地址信息</param>
		/// <param name="length">读取的数据长度</param>
		public HslDeviceAddressAttribute(string address, int length )
		{
			this.Address = address;
			this.Length = length;
			this.DeviceType = null;
		}

		/// <summary>
		/// 实例化一个地址特性，指定地址信息和数据长度，通常应用于数组的批量读取，并指定设备的类型，可用于不同种类的PLC<br />
		/// Instantiate an address feature, specify address information and data length, usually used in batch reading of arrays, 
		/// and specify the type of equipment, which can be used for different types of PLCs
		/// </summary>
		/// <param name="address">真实的地址信息</param>
		/// <param name="length">读取的数据长度</param>
		/// <param name="deviceType">设备类型</param>
		public HslDeviceAddressAttribute( string address, int length, Type deviceType )
		{
			this.Address = address;
			this.Length = length;
			this.DeviceType = deviceType;
		}

		/// <inheritdoc/>
		public override string ToString( ) => $"HslDeviceAddressAttribute[{Address}:{Length}]";
	}
}
