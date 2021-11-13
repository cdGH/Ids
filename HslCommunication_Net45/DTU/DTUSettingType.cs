using HslCommunication.Core.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using HslCommunication.Profinet.Melsec;
using HslCommunication.ModBus;
using HslCommunication.Profinet.Siemens;
using HslCommunication.Profinet.Omron;
using HslCommunication.Profinet.AllenBradley;

namespace HslCommunication.DTU
{
	/// <summary>
	/// DTU的类型设置器
	/// </summary>
	public class DTUSettingType
	{
		/// <summary>
		/// 设备的唯一ID信息
		/// </summary>
		public string DtuId { get; set; }

		/// <summary>
		/// 当前的设备的类型
		/// </summary>
		public string DtuType { get; set; } = "ModbusRtuOverTcp";

		/// <summary>
		/// 额外的参数都存放在json里面
		/// </summary>
		public string JsonParameter { get; set; } = "{}";

		/// <inheritdoc/>
		public override string ToString( ) => $"{DtuId} [{DtuType}]";

		/// <summary>
		/// 根据类型，获取连接对象
		/// </summary>
		/// <returns>获取设备的连接对象</returns>
		public virtual NetworkDeviceBase GetClient( )
		{
			JObject json = JObject.Parse( JsonParameter );
			if      (DtuType == nameof( ModbusRtuOverTcp ))      return new ModbusRtuOverTcp(      "127.0.0.1", 502, json["Station"].Value<byte>( ) ) { ConnectionId = DtuId };
			else if (DtuType == nameof( ModbusTcpNet ))          return new ModbusTcpNet(          "127.0.0.1", 502, json["Station"].Value<byte>( ) ) { ConnectionId = DtuId };
			else if (DtuType == nameof( MelsecMcNet ))           return new MelsecMcNet(           "127.0.0.1", 5000 ) { ConnectionId = DtuId };
			else if (DtuType == nameof( MelsecMcAsciiNet ))      return new MelsecMcAsciiNet(      "127.0.0.1", 5000 ) { ConnectionId = DtuId };
			else if (DtuType == nameof( MelsecA1ENet ))          return new MelsecA1ENet(          "127.0.0.1", 5000 ) { ConnectionId = DtuId };
			else if (DtuType == nameof( MelsecA1EAsciiNet ))     return new MelsecA1EAsciiNet(     "127.0.0.1", 5000 ) { ConnectionId = DtuId };
			else if (DtuType == nameof( MelsecA3CNetOverTcp ))   return new MelsecA3CNetOverTcp(   "127.0.0.1", 5000 ) { ConnectionId = DtuId };
			else if (DtuType == nameof( MelsecFxLinksOverTcp ))  return new MelsecFxLinksOverTcp(  "127.0.0.1", 5000 ) { ConnectionId = DtuId };
			else if (DtuType == nameof( MelsecFxSerialOverTcp )) return new MelsecFxSerialOverTcp( "127.0.0.1", 5000 ) { ConnectionId = DtuId };
			else if (DtuType == nameof( SiemensS7Net ))          return new SiemensS7Net( (SiemensPLCS)Enum.Parse( typeof( SiemensPLCS ),json["SiemensPLCS"].Value<string>( ) ) ) { ConnectionId = DtuId };
			else if (DtuType == nameof( SiemensFetchWriteNet ))  return new SiemensFetchWriteNet(  "127.0.0.1", 5000 ) { ConnectionId = DtuId };
			else if (DtuType == nameof( SiemensPPIOverTcp ))     return new SiemensPPIOverTcp(     "127.0.0.1", 5000 ) { ConnectionId = DtuId };
			else if (DtuType == nameof( OmronFinsNet ))          return new OmronFinsNet(          "127.0.0.1", 5000 ) { ConnectionId = DtuId };
			else if (DtuType == nameof( OmronHostLinkOverTcp ))  return new OmronHostLinkOverTcp(  "127.0.0.1", 5000 ) { ConnectionId = DtuId };
			else if (DtuType == nameof( AllenBradleyNet ))       return new AllenBradleyNet(       "127.0.0.1", 5000 ) { ConnectionId = DtuId };

			else throw new NotImplementedException( );
		}
	}
}
