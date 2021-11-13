using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HslCommunication.Profinet.Melsec
{
	/// <summary>
	/// 三菱PLC的数据类型，此处包含了几个常用的类型
	/// </summary>
	public class MelsecA1EDataType
	{
		/// <summary>
		/// 如果您清楚类型代号，可以根据值进行扩展
		/// </summary>
		/// <param name="code">数据类型的代号</param>
		/// <param name="type">0或1，默认为0</param>
		/// <param name="asciiCode">ASCII格式的类型信息</param>
		/// <param name="fromBase">指示地址的多少进制的，10或是16</param>
		public MelsecA1EDataType( ushort code, byte type, string asciiCode, int fromBase )
		{
			DataCode = code;
			AsciiCode = asciiCode;
			FromBase = fromBase;
			if (type < 2) DataType = type;
		}

		/// <summary>
		/// 类型的代号值（软元件代码，用于区分软元件类型，如：D，R）
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

		/// <summary>
		/// X输入寄存器
		/// </summary>
		public readonly static MelsecA1EDataType X = new MelsecA1EDataType( 0x5820, 0x01, "X*", 16 );
		/// <summary>
		/// Y输出寄存器
		/// </summary>
		public readonly static MelsecA1EDataType Y = new MelsecA1EDataType( 0x5920, 0x01, "Y*", 16 );
		/// <summary>
		/// M中间寄存器
		/// </summary>
		public readonly static MelsecA1EDataType M = new MelsecA1EDataType( 0x4D20, 0x01, "M*", 10 );
		/// <summary>
		/// S状态寄存器
		/// </summary>
		public readonly static MelsecA1EDataType S = new MelsecA1EDataType( 0x5320, 0x01, "S*", 10 );
		/// <summary>
		/// F报警器
		/// </summary>
		public readonly static MelsecA1EDataType F = new MelsecA1EDataType( 0x4620, 0x01, "F*", 10 );
		/// <summary>
		/// B连接继电器
		/// </summary>
		public readonly static MelsecA1EDataType B = new MelsecA1EDataType( 0x4220, 0x01, "B*", 16 );
		/// <summary>
		/// TS定时器触点
		/// </summary>
		public readonly static MelsecA1EDataType TS = new MelsecA1EDataType( 0x5453, 0x01, "TS", 10 );
		/// <summary>
		/// TC定时器线圈
		/// </summary>
		public readonly static MelsecA1EDataType TC = new MelsecA1EDataType( 0x5443, 0x01, "TC", 10 );
		/// <summary>
		/// TN定时器当前值
		/// </summary>
		public readonly static MelsecA1EDataType TN = new MelsecA1EDataType( 0x544E, 0x00, "TN", 10 );
		/// <summary>
		/// CS计数器触点
		/// </summary>
		public readonly static MelsecA1EDataType CS = new MelsecA1EDataType( 0x4353, 0x01, "CS", 10 );
		/// <summary>
		/// CC计数器线圈
		/// </summary>
		public readonly static MelsecA1EDataType CC = new MelsecA1EDataType( 0x4343, 0x01, "CC", 10 );
		/// <summary>
		/// CN计数器当前值
		/// </summary>
		public readonly static MelsecA1EDataType CN = new MelsecA1EDataType( 0x434E, 0x00, "CN", 10 );
		/// <summary>
		/// D数据寄存器
		/// </summary>
		public readonly static MelsecA1EDataType D = new MelsecA1EDataType( 0x4420, 0x00, "D*", 10 );
		/// <summary>
		/// W链接寄存器
		/// </summary>
		public readonly static MelsecA1EDataType W = new MelsecA1EDataType( 0x5720, 0x00, "W*", 16 );
		/// <summary>
		/// R文件寄存器
		/// </summary>
		public readonly static MelsecA1EDataType R = new MelsecA1EDataType( 0x5220, 0x00, "R*", 10 );
	}
}
