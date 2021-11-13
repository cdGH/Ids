using System;
using System.Text;

namespace HslCommunication.Profinet.LSIS
{
#pragma warning disable CS1591 // 缺少对公共可见类型或成员的 XML 注释
	/// <summary>
	/// It is determined to be the XGK/I/R series through a reserved area
	/// </summary>
	public enum LSCpuInfo
	{
		XGK = 1,
		XGI,
		XGR,
		XGB_MK,
		XGB_IEC,
		XGB
	}

	/// <summary>
	/// Cpu status
	/// </summary>
	public enum LSCpuStatus
	{
		/// <summary>
		/// 运行中
		/// </summary>
		RUN = 1,
		/// <summary>
		/// 运行停止
		/// </summary>
		STOP,
		/// <summary>
		/// 错误状态
		/// </summary>
		ERROR,
		/// <summary>
		/// 调试模式
		/// </summary>
		DEBUG
	}

	/// <summary>
	/// using FlagBit in Marker for Byte<br />
	/// M0.0=1;M0.1=2;M0.2=4;M0.3=8;==========================>M0.7=128
	/// </summary>
	public enum FlagBit
	{
		Flag1 = 1,
		Flag2 = 2,
		Flag4 = 4,
		Flag8 = 8,
		Flag16 = 16,
		Flag32 = 32,
		Flag64 = 64,
		Flag128 = 128,
		Flag256 = 256,
	}
    public enum XGT_Request_Func
    {
        Read = 84,  //0x54
        ReadResponse = 85, //0x55
        Write = 88,  //0x58
        WriteResponse = 89 //0x59
    }
    public enum XGT_DataType
    {
        Bit = 0x0000,
        Byte = 0x0001,
        Word = 0x0002,
        DWord = 0x0003,
        LWord = 0x0004,
        Continue = 0x0014
    }
    public enum XGT_MemoryType
    {
        /// <summary>입출력(Bit)</summary>
        IO = 0,
        /// <summary>보조릴레이(Bit)</summary>
        SubRelay = 1,
        /// <summary>링크릴레이(Bit)</summary>
        LinkRelay = 2,
        /// <summary>Keep릴레이(Bit)</summary>
        KeepRelay = 3,
        /// <summary>특수릴레이(Bit)</summary>
        EtcRelay = 4,
        /// <summary>타이머(현재값)(Word)</summary>
        Timer = 5,
        /// <summary>카운터(현재값)(Word)</summary>
        Counter = 6,
        // <summary>데이터레지스터(Word)</summary>
        DataRegister = 7,
        /// <summary>통신 데이터레지스터(Word)</summary>
        ComDataRegister = 8,
        /// <summary>파일 레지스터(Word)</summary>
        FileDataRegister = 9,
        /// <summary>파일 레지스터(Word)</summary>
        StepRelay = 10,
        /// <summary>파일 레지스터(Word)</summary>
        SpecialRegister = 11,


    }

    public static class XGT_Data_TypeClass
    {
        public const string Bit = "X";
        public const string Byte = "B";
        public const string Word = "W";
        public const string DWord = "D";
        public const string LWord = "L";
    }

    public static class XGT_Memory_TypeClass
    {
        /// <summary>입출력(Bit)</summary>
        public const string IO = "P";
        /// <summary>보조릴레이(Bit)</summary>
        public const string SubRelay = "M";
        /// <summary>링크릴레이(Bit)</summary>
        public const string LinkRelay = "L";
        /// <summary>Keep릴레이(Bit)</summary>
        public const string KeepRelay = "K";
        /// <summary>특수릴레이(Bit)</summary>
        public const string EtcRelay = "F";
        /// <summary>타이머(현재값)(Word)</summary>
        public const string Timer = "T";
        /// <summary>카운터(현재값)(Word)</summary>
        public const string Counter = "C";
        /// <summary>데이터레지스터(Word)</summary>
        public const string DataRegister = "D";
        /// <summary>통신 데이터레지스터(Word)</summary>
        public const string ComDataRegister = "N";
        /// <summary>파일 레지스터(Word)</summary>
        public const string FileDataRegister = "R";
        /// <summary>파일 레지스터(Word)</summary>
        public const string StepRelay = "S";
        /// <summary>파일 레지스터(Word)</summary>
        public const string SpecialRegister = "U";

    }

    public class XGTAddressData
    {
        public string Address { get; set; }
        public string Data { get; set; }
        public byte[] DataByteArray { get; set; }
        /// <summary>
        /// 주소 문자열 표현, EX) %DW1100
        /// </summary>
        public string AddressString { get; set; }
        /// <summary>
        /// AddressString 을 바이트 배열로 변환
        /// </summary>
        public byte[] AddressByteArray
        {
            get
            {
                byte[] value = Encoding.ASCII.GetBytes(AddressString);
                return value;
            }
        }
        /// <summary>
        /// AddressByteArray 바이트 배열의 수(2byte)
        /// </summary>
        public byte[] LengthByteArray
        {
            get
            {
                byte[] value = BitConverter.GetBytes((short)AddressByteArray.Length);
                return value;
            }

        }
    }
#pragma warning restore CS1591 // 缺少对公共可见类型或成员的 XML 注释
}
