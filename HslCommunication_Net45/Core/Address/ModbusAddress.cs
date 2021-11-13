using HslCommunication.BasicFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HslCommunication.ModBus;

namespace HslCommunication.Core.Address
{
	/// <summary>
	/// Modbus协议地址格式，可以携带站号，功能码，地址信息<br />
	/// Modbus protocol address format, can carry station number, function code, address information
	/// </summary>
	public class ModbusAddress : DeviceAddressBase
	{
		#region Constructor

		/// <summary>
		/// 实例化一个默认的对象<br />
		/// Instantiate a default object
		/// </summary>
		public ModbusAddress( )
		{
			Station = -1;
			Function = -1;
			Address = 0;
		}

		/// <summary>
		/// 实例化一个对象，使用指定的地址初始化<br />
		/// Instantiate an object, initialize with the specified address
		/// </summary>
		/// <param name="address">传入的地址信息，支持富地址，例如s=2;x=3;100</param>
		public ModbusAddress( string address )
		{
			Station = -1;
			Function = -1;
			Address = 0;
			Parse( address );
		}

		/// <summary>
		/// 实例化一个对象，使用指定的地址及功能码初始化<br />
		/// Instantiate an object and initialize it with the specified address and function code
		/// </summary>
		/// <param name="address">传入的地址信息，支持富地址，例如s=2;x=3;100</param>
		/// <param name="function">默认的功能码信息</param>
		public ModbusAddress( string address, byte function )
		{
			Station = -1;
			Function = function;
			Address = 0;
			Parse( address );
		}

		/// <summary>
		/// 实例化一个对象，使用指定的地址，站号，功能码来初始化<br />
		/// Instantiate an object, use the specified address, station number, function code to initialize
		/// </summary>
		/// <param name="address">传入的地址信息，支持富地址，例如s=2;x=3;100</param>
		/// <param name="station">站号信息</param>
		/// <param name="function">默认的功能码信息</param>
		public ModbusAddress( string address, byte station ,byte function )
		{
			Station = -1;
			Function = function;
			Station = station;
			Address = 0;
			Parse( address );
		}

		#endregion

		#region Public Properties

		/// <summary>
		/// 获取或设置当前地址的站号信息<br />
		/// Get or set the station number information of the current address
		/// </summary>
		public int Station { get; set; }

		/// <summary>
		/// 获取或设置当前地址携带的功能码<br />
		/// Get or set the function code carried by the current address
		/// </summary>
		public int Function { get; set; }

		#endregion

		#region Analysis Address
		
		/// <inheritdoc/>
		public override void Parse( string address )
		{
			if (address.IndexOf( ';' ) < 0)
			{
				// 正常地址，功能码03
				Address = ushort.Parse( address );
			}
			else
			{
				// 带功能码的地址
				string[] list = address.Split( ';' );
				for (int i = 0; i < list.Length; i++)
				{
					if (list[i][0] == 's' || list[i][0] == 'S')
					{
						// 站号信息
						this.Station = byte.Parse( list[i].Substring( 2 ) );
					}
					else if (list[i][0] == 'x' || list[i][0] == 'X')
					{
						this.Function = byte.Parse( list[i].Substring(2) );
					}
					else
					{
						this.Address = ushort.Parse( list[i] );
					}
				}
			}
		}

		#endregion

		#region Address Operate

		/// <summary>
		/// 地址偏移指定的位置，返回一个新的地址对象<br />
		/// The address is offset by the specified position and a new address object is returned
		/// </summary>
		/// <param name="value">数据值信息</param>
		/// <returns>新增后的地址信息</returns>
		public ModbusAddress AddressAdd( int value )
		{
			return new ModbusAddress( )
			{
				Station = this.Station,
				Function = this.Function,
				Address = (ushort)(this.Address + value),
			};
		}

		/// <summary>
		/// 地址偏移1，返回一个新的地址对象<br />
		/// The address is offset by 1 and a new address object is returned
		/// </summary>
		/// <returns>新增后的地址信息</returns>
		public ModbusAddress AddressAdd( )
		{
			return AddressAdd( 1 );
		}
		
		#endregion

		#region Object Override
		
		/// <inheritdoc/>
		public override string ToString( )
		{
			StringBuilder sb = new StringBuilder( );
			if (Station >= 0) sb.Append( "s=" + Station + ";" );
			if (Function == ModbusInfo.ReadDiscrete || Function == ModbusInfo.ReadInputRegister) sb.Append( "x=" + Function + ";" );
			sb.Append( Address.ToString( ) );
			return sb.ToString( );
		}

		#endregion
	}
}
