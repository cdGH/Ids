using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
#if !NET35 && !NET20
using System.Threading.Tasks;
#endif

namespace HslCommunication.Instrument.DLT
{
	/// <summary>
	/// 基本的控制码信息
	/// </summary>
	public class DLTControl
	{
		/// <summary>
		/// 保留
		/// </summary>
		public const byte Retain = 0;

		/// <summary>
		/// 广播
		/// </summary>
		public const byte Broadcast = 0x08;

		/// <summary>
		/// 读数据
		/// </summary>
		public const byte ReadData = 0x11;

		/// <summary>
		/// 读后续数据
		/// </summary>
		public const byte ReadFollowData = 0x12;

		/// <summary>
		/// 读通信地址
		/// </summary>
		public const byte ReadAddress = 0x13;

		/// <summary>
		/// 写数据
		/// </summary>
		public const byte WriteData = 0x14;

		/// <summary>
		/// 写通信地址
		/// </summary>
		public const byte WriteAddress = 0x15;

		/// <summary>
		/// 冻结命令
		/// </summary>
		public const byte FreezeCommand = 0x16;

		/// <summary>
		/// 更改通信速率
		/// </summary>
		public const byte ChangeBaudRate = 0x17;

		/// <summary>
		/// 修改密码
		/// </summary>
		public const byte ChangePassword = 0x18;

		/// <summary>
		/// 最大需求量清零
		/// </summary>
		public const byte ClearMaxQuantityDemanded = 0x19;

		/// <summary>
		/// 电表清零
		/// </summary>
		public const byte ElectricityReset = 0x1A;

		/// <summary>
		/// 事件清零
		/// </summary>
		public const byte EventReset = 0x1B;

		/// <summary>
		/// 跳合闸、报警、保电
		/// </summary>
		public const byte ClosingAlarmPowerpProtection = 0x1C;

		/// <summary>
		/// 多功能端子输出控制命令
		/// </summary>
		public const byte MultiFunctionTerminalOutputControlCommand = 0x1D;

		/// <summary>
		/// 安全认证命令
		/// </summary>
		public const byte SecurityAuthenticationCommand = 0x03;
	}
}
