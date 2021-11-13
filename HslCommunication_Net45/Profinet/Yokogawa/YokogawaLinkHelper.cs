using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HslCommunication.Profinet.Yokogawa
{
	/// <summary>
	/// 横河PLC的通信辅助类。
	/// </summary>
	public class YokogawaLinkHelper
	{
		/// <summary>
		/// 获取横河PLC的错误的具体描述信息
		/// </summary>
		/// <param name="code">错误码</param>
		/// <returns></returns>
		public static string GetErrorMsg(byte code )
		{
			switch (code)
			{
				case 0x01: return StringResources.Language.YokogawaLinkError01;
				case 0x02: return StringResources.Language.YokogawaLinkError02;
				case 0x03: return StringResources.Language.YokogawaLinkError03;
				case 0x04: return StringResources.Language.YokogawaLinkError04;
				case 0x05: return StringResources.Language.YokogawaLinkError05;
				case 0x06: return StringResources.Language.YokogawaLinkError06;
				case 0x07: return StringResources.Language.YokogawaLinkError07;
				case 0x08: return StringResources.Language.YokogawaLinkError08;
				case 0x41: return StringResources.Language.YokogawaLinkError41;
				case 0x42: return StringResources.Language.YokogawaLinkError42;
				case 0x43: return StringResources.Language.YokogawaLinkError43;
				case 0x44: return StringResources.Language.YokogawaLinkError44;
				case 0x51: return StringResources.Language.YokogawaLinkError51;
				case 0x52: return StringResources.Language.YokogawaLinkError52;
				case 0xF1: return StringResources.Language.YokogawaLinkErrorF1;
				default: return StringResources.Language.UnknownError;
			}
		}
	}
}
