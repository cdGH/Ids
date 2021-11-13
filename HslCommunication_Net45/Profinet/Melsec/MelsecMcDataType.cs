using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HslCommunication.Profinet.Melsec
{
	/// <summary>
	/// 三菱PLC的数据类型，此处包含了几个常用的类型<br />
	/// Data types of Mitsubishi PLC, here contains several commonly used types
	/// </summary>
	public class MelsecMcDataType
	{
		/// <summary>
		/// 实例化一个三菱数据类型对象，如果您清楚类型代号，可以根据值进行扩展<br />
		/// Instantiate a Mitsubishi data type object, if you know the type code, you can expand according to the value
		/// </summary>
		/// <param name="code">数据类型的代号</param>
		/// <param name="type">0或1，默认为0</param>
		/// <param name="asciiCode">ASCII格式的类型信息</param>
		/// <param name="fromBase">指示地址的多少进制的，10或是16</param>
		public MelsecMcDataType( ushort code, byte type, string asciiCode, int fromBase )
		{
			DataCode = code;
			AsciiCode = asciiCode;
			FromBase = fromBase;
			if (type < 2) DataType = type;
		}

		/// <summary>
		/// 类型的代号值
		/// </summary>
		public ushort DataCode { get; private set; } = 0x00;

		/// <summary>
		/// 数据的类型，0代表按字，1代表按位
		/// </summary>
		public byte DataType { get; private set; } = 0x00;

		/// <summary>
		/// 当以ASCII格式通讯时的类型描述
		/// </summary>
		public string AsciiCode { get; private set; }

		/// <summary>
		/// 指示地址是10进制，还是16进制的
		/// </summary>
		public int FromBase { get; private set; }

		#region 三菱MC协议地址

		/// <summary>
		/// X输入继电器
		/// </summary>
		public readonly static MelsecMcDataType X = new MelsecMcDataType( 0x9C, 0x01, "X*", 16 );

		/// <summary>
		/// Y输出继电器
		/// </summary>
		public readonly static MelsecMcDataType Y = new MelsecMcDataType( 0x9D, 0x01, "Y*", 16 );

		/// <summary>
		/// M内部继电器
		/// </summary>
		public readonly static MelsecMcDataType M = new MelsecMcDataType( 0x90, 0x01, "M*", 10 );

		/// <summary>
		/// SM特殊继电器
		/// </summary>
		public readonly static MelsecMcDataType SM = new MelsecMcDataType( 0x91, 0x01, "SM", 10 );

		/// <summary>
		/// S步进继电器
		/// </summary>
		public readonly static MelsecMcDataType S = new MelsecMcDataType( 0x98, 0x01, "S*", 10 );

		/// <summary>
		/// L锁存继电器
		/// </summary>
		public readonly static MelsecMcDataType L = new MelsecMcDataType( 0x92, 0x01, "L*", 10 );

		/// <summary>
		/// F报警器
		/// </summary>
		public readonly static MelsecMcDataType F = new MelsecMcDataType( 0x93, 0x01, "F*", 10 );

		/// <summary>
		/// V边沿继电器
		/// </summary>
		public readonly static MelsecMcDataType V = new MelsecMcDataType( 0x94, 0x01, "V*", 10 );

		/// <summary>
		/// B链接继电器
		/// </summary>
		public readonly static MelsecMcDataType B = new MelsecMcDataType( 0xA0, 0x01, "B*", 16 );

		/// <summary>
		/// SB特殊链接继电器
		/// </summary>
		public readonly static MelsecMcDataType SB = new MelsecMcDataType( 0xA1, 0x01, "SB", 16 );

		/// <summary>
		/// DX直接访问输入
		/// </summary>
		public readonly static MelsecMcDataType DX = new MelsecMcDataType( 0xA2, 0x01, "DX", 16 );

		/// <summary>
		/// DY直接访问输出
		/// </summary>
		public readonly static MelsecMcDataType DY = new MelsecMcDataType( 0xA3, 0x01, "DY", 16 );



		/// <summary>
		/// D数据寄存器
		/// </summary>
		public readonly static MelsecMcDataType D = new MelsecMcDataType( 0xA8, 0x00, "D*", 10 );

		/// <summary>
		/// 特殊链接存储器
		/// </summary>
		public readonly static MelsecMcDataType SD = new MelsecMcDataType( 0xA9, 0x00, "SD", 10 );

		/// <summary>
		/// W链接寄存器
		/// </summary>
		public readonly static MelsecMcDataType W = new MelsecMcDataType( 0xB4, 0x00, "W*", 16 );

		/// <summary>
		/// SW特殊链接寄存器
		/// </summary>
		public readonly static MelsecMcDataType SW = new MelsecMcDataType( 0xB5, 0x00, "SW", 16 );

		/// <summary>
		/// R文件寄存器
		/// </summary>
		public readonly static MelsecMcDataType R = new MelsecMcDataType( 0xAF, 0x00, "R*", 10 );

		/// <summary>
		/// 变址寄存器
		/// </summary>
		public readonly static MelsecMcDataType Z = new MelsecMcDataType( 0xCC, 0x00, "Z*", 10 );

		/// <summary>
		/// 文件寄存器ZR区
		/// </summary>
		public readonly static MelsecMcDataType ZR = new MelsecMcDataType( 0xB0, 0x00, "ZR", 10 );



		/// <summary>
		/// 定时器的当前值
		/// </summary>
		public readonly static MelsecMcDataType TN = new MelsecMcDataType( 0xC2, 0x00, "TN", 10 );

		/// <summary>
		/// 定时器的触点
		/// </summary>
		public readonly static MelsecMcDataType TS = new MelsecMcDataType( 0xC1, 0x01, "TS", 10 );

		/// <summary>
		/// 定时器的线圈
		/// </summary>
		public readonly static MelsecMcDataType TC = new MelsecMcDataType( 0xC0, 0x01, "TC", 10 );



		/// <summary>
		/// 累计定时器的触点
		/// </summary>
		public readonly static MelsecMcDataType SS = new MelsecMcDataType( 0xC7, 0x01, "SS", 10 );

		/// <summary>
		/// 累计定时器的线圈
		/// </summary>
		public readonly static MelsecMcDataType SC = new MelsecMcDataType( 0xC6, 0x01, "SC", 10 );

		/// <summary>
		/// 累计定时器的当前值
		/// </summary>
		public readonly static MelsecMcDataType SN = new MelsecMcDataType( 0xC8, 0x00, "SN", 100 );


		/// <summary>
		/// 计数器的当前值
		/// </summary>
		public readonly static MelsecMcDataType CN = new MelsecMcDataType( 0xC5, 0x00, "CN", 10 );

		/// <summary>
		/// 计数器的触点
		/// </summary>
		public readonly static MelsecMcDataType CS = new MelsecMcDataType( 0xC4, 0x01, "CS", 10 );

		/// <summary>
		/// 计数器的线圈
		/// </summary>
		public readonly static MelsecMcDataType CC = new MelsecMcDataType( 0xC3, 0x01, "CC", 10 );


		#endregion

		#region 三菱MC协议R系列PLC的地址


		/// <summary>
		/// X输入继电器
		/// </summary>
		public readonly static MelsecMcDataType R_X = new MelsecMcDataType(  0x009C , 0x01, "X***", 16 );

		/// <summary>
		/// Y输入继电器
		/// </summary>
		public readonly static MelsecMcDataType R_Y = new MelsecMcDataType( 0x009D, 0x01, "Y***", 16 );

		/// <summary>
		/// M内部继电器
		/// </summary>
		public readonly static MelsecMcDataType R_M = new MelsecMcDataType( 0x0090, 0x01, "M***", 10 );

		/// <summary>
		/// 特殊继电器
		/// </summary>
		public readonly static MelsecMcDataType R_SM = new MelsecMcDataType( 0x0091, 0x01, "SM**", 10 );

		/// <summary>
		/// 锁存继电器
		/// </summary>
		public readonly static MelsecMcDataType R_L = new MelsecMcDataType( 0x0092, 0x01, "L***", 10 );

		/// <summary>
		/// 报警器
		/// </summary>
		public readonly static MelsecMcDataType R_F = new MelsecMcDataType( 0x0093, 0x01, "F***", 10 );

		/// <summary>
		/// 变址继电器
		/// </summary>
		public readonly static MelsecMcDataType R_V = new MelsecMcDataType( 0x0094, 0x01, "V***", 10 );

		/// <summary>
		/// S步进继电器
		/// </summary>
		public readonly static MelsecMcDataType R_S = new MelsecMcDataType( 0x0098, 0x01, "S***", 10 );

		/// <summary>
		/// 链接继电器
		/// </summary>
		public readonly static MelsecMcDataType R_B = new MelsecMcDataType( 0x00A0, 0x01, "B***", 16 );

		/// <summary>
		/// 特殊链接继电器
		/// </summary>
		public readonly static MelsecMcDataType R_SB = new MelsecMcDataType( 0x00A1, 0x01, "SB**", 16 );

		/// <summary>
		/// 直接访问输入继电器
		/// </summary>
		public readonly static MelsecMcDataType R_DX = new MelsecMcDataType( 0x00A2, 0x01, "DX**", 16 );

		/// <summary>
		/// 直接访问输出继电器
		/// </summary>
		public readonly static MelsecMcDataType R_DY = new MelsecMcDataType( 0x00A3, 0x01, "DY**", 16 );

		/// <summary>
		/// 数据寄存器
		/// </summary>
		public readonly static MelsecMcDataType R_D = new MelsecMcDataType( 0x00A8, 0x00, "D***", 10 );

		/// <summary>
		/// 特殊数据寄存器
		/// </summary>
		public readonly static MelsecMcDataType R_SD = new MelsecMcDataType( 0x00A9, 0x00, "SD**", 10 );

		/// <summary>
		/// 链接寄存器
		/// </summary>
		public readonly static MelsecMcDataType R_W = new MelsecMcDataType( 0x00B4, 0x00, "W***", 16 );

		/// <summary>
		/// 特殊链接寄存器
		/// </summary>
		public readonly static MelsecMcDataType R_SW = new MelsecMcDataType( 0x00B5, 0x00, "SW**", 16 );

		/// <summary>
		/// 文件寄存器
		/// </summary>
		public readonly static MelsecMcDataType R_R = new MelsecMcDataType( 0x00AF, 0x00, "R***", 10 );

		/// <summary>
		/// 变址寄存器
		/// </summary>
		public readonly static MelsecMcDataType R_Z = new MelsecMcDataType( 0x00CC, 0x00, "Z***", 10 );




		/// <summary>
		/// 长累计定时器触点
		/// </summary>
		public readonly static MelsecMcDataType R_LSTS = new MelsecMcDataType( 0x0059, 0x01, "LSTS", 10 );
		/// <summary>
		/// 长累计定时器线圈
		/// </summary>
		public readonly static MelsecMcDataType R_LSTC = new MelsecMcDataType( 0x0058, 0x01, "LSTC", 10 );
		/// <summary>
		/// 长累计定时器当前值
		/// </summary>
		public readonly static MelsecMcDataType R_LSTN = new MelsecMcDataType( 0x005A, 0x00, "LSTN", 10 );

		/// <summary>
		/// 累计定时器触点
		/// </summary>
		public readonly static MelsecMcDataType R_STS = new MelsecMcDataType( 0x00C7, 0x01, "STS*", 10 );
		/// <summary>
		/// 累计定时器线圈
		/// </summary>
		public readonly static MelsecMcDataType R_STC = new MelsecMcDataType( 0x00C6, 0x01, "STC*", 10 );
		/// <summary>
		/// 累计定时器当前值
		/// </summary>
		public readonly static MelsecMcDataType R_STN = new MelsecMcDataType( 0x00C8, 0x00, "STN*", 10 );

		/// <summary>
		/// 长定时器触点
		/// </summary>
		public readonly static MelsecMcDataType R_LTS = new MelsecMcDataType( 0x0051, 0x01, "LTS*", 10 );
		/// <summary>
		/// 长定时器线圈
		/// </summary>
		public readonly static MelsecMcDataType R_LTC = new MelsecMcDataType( 0x0050, 0x01, "LTC*", 10 );
		/// <summary>
		/// 长定时器当前值
		/// </summary>
		public readonly static MelsecMcDataType R_LTN = new MelsecMcDataType( 0x0052, 0x00, "LTN*", 10 );

		/// <summary>
		/// 定时器触点
		/// </summary>
		public readonly static MelsecMcDataType R_TS = new MelsecMcDataType( 0x00C1, 0x01, "TS**", 10 );
		/// <summary>
		/// 定时器线圈
		/// </summary>
		public readonly static MelsecMcDataType R_TC = new MelsecMcDataType( 0x00C0, 0x01, "TC**", 10 );
		/// <summary>
		/// 定时器当前值
		/// </summary>
		public readonly static MelsecMcDataType R_TN = new MelsecMcDataType( 0x00C2, 0x00, "TN**", 10 );

		/// <summary>
		/// 长计数器触点
		/// </summary>
		public readonly static MelsecMcDataType R_LCS = new MelsecMcDataType( 0x0055, 0x01, "LCS*", 10 );
		/// <summary>
		/// 长计数器线圈
		/// </summary>
		public readonly static MelsecMcDataType R_LCC = new MelsecMcDataType( 0x0054, 0x01, "LCC*", 10 );
		/// <summary>
		/// 长计数器当前值
		/// </summary>
		public readonly static MelsecMcDataType R_LCN = new MelsecMcDataType( 0x0056, 0x00, "LCN*", 10 );

		/// <summary>
		/// 计数器触点
		/// </summary>
		public readonly static MelsecMcDataType R_CS = new MelsecMcDataType( 0x00C4, 0x01, "CS**", 10 );
		/// <summary>
		/// 计数器线圈
		/// </summary>
		public readonly static MelsecMcDataType R_CC = new MelsecMcDataType( 0x00C3, 0x01, "CC**", 10 );
		/// <summary>
		/// 计数器当前值
		/// </summary>
		public readonly static MelsecMcDataType R_CN = new MelsecMcDataType( 0x00C5, 0x00, "CN**", 10 );


		#endregion

		#region 基恩士MC协议的地址

		/// <summary>
		/// X输入继电器
		/// </summary>
		public readonly static MelsecMcDataType Keyence_X = new MelsecMcDataType( 0x9C, 0x01, "X*", 16 );
		/// <summary>
		/// Y输出继电器
		/// </summary>
		public readonly static MelsecMcDataType Keyence_Y = new MelsecMcDataType( 0x9D, 0x01, "Y*", 16 );
		/// <summary>
		/// 链接继电器
		/// </summary>
		public readonly static MelsecMcDataType Keyence_B = new MelsecMcDataType( 0xA0, 0x01, "B*", 16 );
		/// <summary>
		/// 内部辅助继电器
		/// </summary>
		public readonly static MelsecMcDataType Keyence_M = new MelsecMcDataType( 0x90, 0x01, "M*", 10 );
		/// <summary>
		/// 锁存继电器
		/// </summary>
		public readonly static MelsecMcDataType Keyence_L = new MelsecMcDataType( 0x92, 0x01, "L*", 10 );
		/// <summary>
		/// 控制继电器
		/// </summary>
		public readonly static MelsecMcDataType Keyence_SM = new MelsecMcDataType( 0x91, 0x01, "SM", 10 );
		/// <summary>
		/// 控制存储器
		/// </summary>
		public readonly static MelsecMcDataType Keyence_SD = new MelsecMcDataType( 0xA9, 0x00, "SD", 10 );
		/// <summary>
		/// 数据存储器
		/// </summary>
		public readonly static MelsecMcDataType Keyence_D = new MelsecMcDataType( 0xA8, 0x00, "D*", 10 );
		/// <summary>
		/// 文件寄存器
		/// </summary>
		public readonly static MelsecMcDataType Keyence_R = new MelsecMcDataType( 0xAF, 0x00, "R*", 10 );
		/// <summary>
		/// 文件寄存器
		/// </summary>
		public readonly static MelsecMcDataType Keyence_ZR = new MelsecMcDataType( 0xB0, 0x00, "ZR", 10 );
		/// <summary>
		/// 链路寄存器
		/// </summary>
		public readonly static MelsecMcDataType Keyence_W = new MelsecMcDataType( 0xB4, 0x00, "W*", 16 );
		/// <summary>
		/// 计时器（当前值）
		/// </summary>
		public readonly static MelsecMcDataType Keyence_TN = new MelsecMcDataType( 0xC2, 0x00, "TN", 10 );
		/// <summary>
		/// 计时器（接点）
		/// </summary>
		public readonly static MelsecMcDataType Keyence_TS = new MelsecMcDataType( 0xC1, 0x01, "TS", 10 );
		/// <summary>
		/// 计时器（线圈）
		/// </summary>
		public readonly static MelsecMcDataType Keyence_TC = new MelsecMcDataType( 0xC0, 0x01, "TC", 10 );
		/// <summary>
		/// 计数器（当前值）
		/// </summary>
		public readonly static MelsecMcDataType Keyence_CN = new MelsecMcDataType( 0xC5, 0x00, "CN", 10 );
		/// <summary>
		/// 计数器（接点）
		/// </summary>
		public readonly static MelsecMcDataType Keyence_CS = new MelsecMcDataType( 0xC4, 0x01, "CS", 10 );
		/// <summary>
		/// 计数器（线圈）
		/// </summary>
		public readonly static MelsecMcDataType Keyence_CC = new MelsecMcDataType( 0xC3, 0x01, "CC", 10 );

		#endregion

		#region 松下MC协议的地址

		/// <summary>
		/// 输入继电器
		/// </summary>
		public readonly static MelsecMcDataType Panasonic_X = new MelsecMcDataType( 0x9C, 0x01, "X*", 10 );
		/// <summary>
		/// 输出继电器
		/// </summary>
		public readonly static MelsecMcDataType Panasonic_Y = new MelsecMcDataType( 0x9D, 0x01, "Y*", 10 );
		/// <summary>
		/// 链接继电器
		/// </summary>
		public readonly static MelsecMcDataType Panasonic_L = new MelsecMcDataType( 0xA0, 0x01, "L*", 10 );
		/// <summary>
		/// 内部继电器
		/// </summary>
		public readonly static MelsecMcDataType Panasonic_R = new MelsecMcDataType( 0x90, 0x01, "R*", 10 );
		/// <summary>
		/// 数据存储器
		/// </summary>
		public readonly static MelsecMcDataType Panasonic_DT = new MelsecMcDataType( 0xA8, 0x00, "D*", 10 );
		/// <summary>
		/// 链接存储器
		/// </summary>
		public readonly static MelsecMcDataType Panasonic_LD = new MelsecMcDataType( 0xB4, 0x00, "W*", 10 );
		/// <summary>
		/// 计时器（当前值）
		/// </summary>
		public readonly static MelsecMcDataType Panasonic_TN = new MelsecMcDataType( 0xC2, 0x00, "TN", 10 );
		/// <summary>
		/// 计时器（接点）
		/// </summary>
		public readonly static MelsecMcDataType Panasonic_TS = new MelsecMcDataType( 0xC1, 0x01, "TS", 10 );
		/// <summary>
		/// 计数器（当前值）
		/// </summary>
		public readonly static MelsecMcDataType Panasonic_CN = new MelsecMcDataType( 0xC5, 0x00, "CN", 10 );
		/// <summary>
		/// 计数器（接点）
		/// </summary>
		public readonly static MelsecMcDataType Panasonic_CS = new MelsecMcDataType( 0xC4, 0x01, "CS", 10 );
		/// <summary>
		/// 特殊链接继电器
		/// </summary>
		public readonly static MelsecMcDataType Panasonic_SM = new MelsecMcDataType( 0x91, 0x01, "SM", 10 );
		/// <summary>
		/// 特殊链接存储器
		/// </summary>
		public readonly static MelsecMcDataType Panasonic_SD = new MelsecMcDataType( 0xA9, 0x00, "SD", 10 );

		#endregion

	}
}
