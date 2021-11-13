using HslCommunication.Core;
using HslCommunication.Core.Address;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
#if !NET20 && !NET35
using System.Threading.Tasks;
#endif

namespace HslCommunication.Profinet.Melsec.Helper
{
	/// <summary>
	/// 基于MC协议的标准的设备接口，适用任何基于MC协议的PLC设备，主要是三菱，基恩士，松下的PLC设备。<br />
	/// The standard equipment interface based on MC protocol is suitable for any PLC equipment based on MC protocol, 
	/// mainly PLC equipment from Mitsubishi, Keyence, and Panasonic.
	/// </summary>
	public interface IReadWriteMc : IReadWriteDevice
	{
		/// <summary>
		/// 网络号，通常为0<br />
		/// Network number, usually 0
		/// </summary>
		/// <remarks>
		/// 依据PLC的配置而配置，如果PLC配置了1，那么此处也填0，如果PLC配置了2，此处就填2，测试不通的话，继续测试0
		/// </remarks>
		byte NetworkNumber { get; set; }

		/// <summary>
		/// 网络站号，通常为0<br />
		/// Network station number, usually 0
		/// </summary>
		/// <remarks>
		/// 依据PLC的配置而配置，如果PLC配置了1，那么此处也填0，如果PLC配置了2，此处就填2，测试不通的话，继续测试0
		/// </remarks>
		byte NetworkStationNumber { get; set; }

		/// <summary>
		/// 当前MC协议的分析地址的方法，对传入的字符串格式的地址进行数据解析。<br />
		/// The current MC protocol's address analysis method performs data parsing on the address of the incoming string format.
		/// </summary>
		/// <param name="address">地址信息</param>
		/// <param name="length">数据长度</param>
		/// <returns>解析后的数据信息</returns>
		OperateResult<McAddressData> McAnalysisAddress( string address, ushort length );

		/// <inheritdoc cref="HslCommunication.Core.Net.NetworkDoubleBase.ByteTransform"/>
		IByteTransform ByteTransform { get; set; }

		/// <summary>
		/// 当前的MC协议的格式类型<br />
		/// The format type of the current MC protocol
		/// </summary>
		McType McType { get; }

		/// <summary>
		/// 从PLC反馈的数据中提取出实际的数据内容，需要传入反馈数据，是否位读取
		/// </summary>
		/// <param name="response">反馈的数据内容</param>
		/// <param name="isBit">是否位读取</param>
		/// <returns>解析后的结果对象</returns>
		byte[] ExtractActualData( byte[] response, bool isBit );
	}
}
