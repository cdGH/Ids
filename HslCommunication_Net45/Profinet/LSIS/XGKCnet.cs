using HslCommunication.BasicFramework;
using HslCommunication.Core;
using HslCommunication.Serial;
using HslCommunication.Reflection;
using System.Collections.Generic;
using System;
using System.Text;
#if !NET35 && !NET20
using System.Threading.Tasks;
#endif


namespace HslCommunication.Profinet.LSIS
{
	/// <summary>
	/// XGk Cnet I/F module supports Serial Port.
	/// </summary>
	/// <remarks>
	/// XGB 主机的通道 0 仅支持 1:1 通信。 对于具有主从格式的 1:N 系统，在连接 XGL-C41A 模块的通道 1 或 XGB 主机中使用 RS-485 通信。 XGL-C41A 模块支持 RS-422/485 协议。
	/// </remarks>
	public class XGKCnet : SerialDeviceBase
	{
		#region Constructor

		/// <summary>
		/// Instantiate a Default object
		/// </summary>
		public XGKCnet()
		{
			ByteTransform = new RegularByteTransform();
			WordLength = 2;
		}

		#endregion

		#region Public Member

		/// <inheritdoc cref="XGBCnetOverTcp.Station"/>
		public byte Station { get; set; } = 0x05;

		#endregion

		#region Read Write Byte

		/// <inheritdoc cref="XGBCnetOverTcp.ReadByte(string)"/>
		[HslMqttApi("ReadByte", "")]
		public OperateResult<byte> ReadByte(string address) => ByteTransformHelper.GetResultFromArray(Read(address, 2));

		/// <inheritdoc cref="XGBCnetOverTcp.Write(string, byte)"/>
		[HslMqttApi("WriteByte", "")]
		public OperateResult Write(string address, byte value) => Write(address, new byte[] { value });

		#endregion

		#region Read Write Bool

		/// <inheritdoc/>
		[HslMqttApi("ReadBoolArray", "")]
		public override OperateResult<bool[]> ReadBool(string address, ushort length)
		{
			List<XGTAddressData> lstAdress = new List<XGTAddressData>();
			string[] ArrayAdress = address.Split(new string[] { ";", "," }, StringSplitOptions.RemoveEmptyEntries);

			foreach (string item in ArrayAdress)
			{
				XGTAddressData addrData = new XGTAddressData();
				var DataTypeResult1 = XGKFastEnet.GetDataTypeToAddress(item);
				if (DataTypeResult1.Content2) addrData.Address = item.Substring(1);
				else addrData.Address = item.Substring(2);
				lstAdress.Add(addrData);
			}
			var analysisResult = XGKFastEnet.AnalysisAddress(ArrayAdress[0]);
			if (!analysisResult.IsSuccess) return OperateResult.CreateFailedResult<bool[]>(analysisResult);

			var DataTypeResult = XGKFastEnet.GetDataTypeToAddress(ArrayAdress[0]);
			if (!DataTypeResult.IsSuccess) return OperateResult.CreateFailedResult<bool[]>(DataTypeResult);
			
			// build read command
			OperateResult<byte[]> coreResult = Read(DataTypeResult.Content1, lstAdress, analysisResult.Content, 1);
			if (!coreResult.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( coreResult );

			OperateResult<byte[]> read = ReadFromCoreServer(coreResult.Content);
			if (!read.IsSuccess) return OperateResult.CreateFailedResult<bool[]>(read);

			return OperateResult.CreateSuccessResult(SoftBasic.ByteToBoolArray(ExtractActualData(read.Content, true).Content, length));
		}

		/// <inheritdoc cref="XGBCnetOverTcp.ReadCoil(string)"/>
		public OperateResult<bool> ReadCoil(string address) => ReadBool(address);

		/// <inheritdoc cref="XGBCnetOverTcp.ReadCoil(string, ushort)"/>
		public OperateResult<bool[]> ReadCoil(string address, ushort length) => ReadBool(address, length);

		/// <inheritdoc cref="WriteCoil(string, bool)"/>
		public OperateResult WriteCoil(string address, bool value) => Write(address, value);

		/// <inheritdoc/>
		[HslMqttApi("WriteBool", "")]
		public override OperateResult Write(string address, bool value) => Write(address, new byte[] { (byte)(value ? 0x01 : 0x00) });

		#endregion

		#region Async Read Write Bool
#if !NET35 && !NET20
		/// <inheritdoc/>
		public override async Task<OperateResult> WriteAsync(string address, bool value) => await WriteAsync(address, new byte[] { (byte)(value ? 0x01 : 0x00) });
#endif
		#endregion

		#region Read Write Support

		/// <inheritdoc/>
		[HslMqttApi("ReadByteArray", "")]
		public override OperateResult<byte[]> Read(string address, ushort length)
		{
			List<XGTAddressData> lstAdress = new List<XGTAddressData>();
			string[] ArrayAdress = address.Split(new string[] { ";", "," }, StringSplitOptions.RemoveEmptyEntries);

			foreach (string item in ArrayAdress)
			{

				XGTAddressData addrData = new XGTAddressData();
				var DataTypeResult1 = XGKFastEnet.GetDataTypeToAddress(item);
				if (DataTypeResult1.Content2) addrData.Address = item.Substring(1);
				else addrData.Address = item.Substring(2);
				lstAdress.Add(addrData);
			}
			var analysisResult = XGKFastEnet.AnalysisAddress(ArrayAdress[0]);
			if (!analysisResult.IsSuccess) return OperateResult.CreateFailedResult<byte[]>(analysisResult);

			var DataTypeResult = XGKFastEnet.GetDataTypeToAddress(ArrayAdress[0]);
			if (!DataTypeResult.IsSuccess) return OperateResult.CreateFailedResult<byte[]>(DataTypeResult);
			// build read command
			OperateResult<byte[]> coreResult = Read(DataTypeResult.Content1, lstAdress, analysisResult.Content, length);
			if (!coreResult.IsSuccess) return coreResult;

			OperateResult<byte[]> read = ReadFromCoreServer(coreResult.Content);
			if (!read.IsSuccess) return read;

			return ExtractActualData(read.Content, true);
		}

		/// <inheritdoc/>
		[HslMqttApi("WriteByteArray", "")]
		public override OperateResult Write(string address, byte[] value)
		{
			List<XGTAddressData> lstAdress = new List<XGTAddressData>();
			string[] ArrayAdress = address.Split(new string[] { ";", "," }, StringSplitOptions.RemoveEmptyEntries);

			foreach (string item in ArrayAdress)
			{
				XGTAddressData addrData = new XGTAddressData();
				var DataTypeResult1 = XGKFastEnet.GetDataTypeToAddress(item);
				if (DataTypeResult1.Content2) addrData.Address = item.Substring(1);
				else addrData.Address = item.Substring(2);
				addrData.DataByteArray = value;

				lstAdress.Add(addrData);
			}
			var analysisResult = XGKFastEnet.AnalysisAddress(address);
			if (!analysisResult.IsSuccess) return OperateResult.CreateFailedResult<byte[]>(analysisResult);

			var DataTypeResult = XGKFastEnet.GetDataTypeToAddress(address);
			if (!DataTypeResult.IsSuccess) return OperateResult.CreateFailedResult<byte[]>(DataTypeResult);
			// build write command
			OperateResult<byte[]> coreResult = Write(DataTypeResult.Content1, lstAdress, analysisResult.Content);
			if (!coreResult.IsSuccess) return coreResult;

			OperateResult<byte[]> read = ReadFromCoreServer(coreResult.Content);
			if (!read.IsSuccess) return read;

			return ExtractActualData(read.Content, false);
		}

		#endregion
		/// <summary>
		/// Read
		/// </summary>
		/// <param name="pDataType"></param>
		/// <param name="pAddress"></param>
		/// <param name="pMemtype"></param>
		/// <param name="pDataCount"></param>
		/// <returns></returns>
		public OperateResult<byte[]> Read(XGT_DataType pDataType, List<XGTAddressData> pAddress, XGT_MemoryType pMemtype, int pDataCount = 0)
		{
			if (pAddress.Count > 16)
			{
				return new OperateResult<byte[]>("You cannot read more than 16 pieces.");
			}
			else
			{
				try
				{
					byte[] tcpFrame = CreateReadDataFormat(Station, XGT_Request_Func.Read, pDataType, pAddress, pMemtype, pDataCount);
					return OperateResult.CreateSuccessResult(tcpFrame);
				}
				catch (Exception ex)
				{
					return new OperateResult<byte[]>("ERROR:" + ex.Message.ToString());
				}

			}
		   
		}
		/// <summary>
		/// Write
		/// </summary>
		/// <param name="pDataType"></param>
		/// <param name="pAddressList"></param>
		/// <param name="pMemtype"></param>
		/// <param name="pDataCount"></param>
		/// <returns></returns>
		public OperateResult<byte[]> Write(XGT_DataType pDataType, List<XGTAddressData> pAddressList, XGT_MemoryType pMemtype, int pDataCount = 0)
		{
			try
			{
				byte[] tcpFrame = CreateWriteDataFormat(Station, XGT_Request_Func.Write, pDataType, pAddressList, pMemtype, pDataCount);
				return OperateResult.CreateSuccessResult(tcpFrame);
			}
			catch (Exception ex)
			{
				return new OperateResult<byte[]>("ERROR:" + ex.Message.ToString());
			}
		}
		private byte[] CreateReadDataFormat
			(byte station, XGT_Request_Func emFunc, XGT_DataType emDatatype, List<XGTAddressData> pAddressList, XGT_MemoryType emMemtype, int pDataCount)
		{
			List<XGTAddressData> lstAddress = new List<XGTAddressData>();

			foreach (XGTAddressData addr in pAddressList)
			{
				string vAddress = new XGKFastEnet().CreateValueName(emDatatype, emMemtype, addr.Address);
				XGTAddressData XgtAddr = new XGTAddressData();
				XgtAddr.AddressString = vAddress;

				lstAddress.Add(XgtAddr);
			}
			List<byte> command = new List<byte>();
			if (XGT_DataType.Continue == emDatatype && XGT_Request_Func.Read == emFunc)
			{
				command.Add(0x05);    // ENQ
				command.AddRange(SoftBasic.BuildAsciiBytesFrom(station));
				command.Add(0x72);    // command r
				command.Add(0x53);    // command type: SB
				command.Add(0x42);

				foreach (XGTAddressData addr in lstAddress)
				{
					command.AddRange(SoftBasic.BuildAsciiBytesFrom((byte)addr.AddressString.Length));
					command.AddRange(Encoding.ASCII.GetBytes(addr.AddressString));
					command.AddRange(SoftBasic.BuildAsciiBytesFrom((byte)pDataCount));
				}
				command.Add(0x04);    // EOT

				int sum = 0;
				for (int i = 0; i < command.Count; i++)
				{
					sum += command[i];
				}
				command.AddRange(SoftBasic.BuildAsciiBytesFrom((byte)sum));
			}
			else
			{
				command.Add(0x05);    // ENQ
				command.AddRange(SoftBasic.BuildAsciiBytesFrom(station));
				command.Add(0x72);    // command r
				command.Add(0x53);    // command type: SS
				command.Add(0x53);
				command.Add(0x30);    // Number of blocks
				command.Add(0x31);
				foreach (XGTAddressData addr in lstAddress)
				{
					command.AddRange(SoftBasic.BuildAsciiBytesFrom((byte)addr.AddressString.Length));
					command.AddRange(Encoding.ASCII.GetBytes(addr.AddressString));
				}
				command.Add(0x04);    // EOT

				int sum = 0;
				for (int i = 0; i < command.Count; i++)
				{
					sum += command[i];
				}
				command.AddRange(SoftBasic.BuildAsciiBytesFrom((byte)sum));
			}
			return command.ToArray();
		}


		//Create application data WRITE format
		private byte[] CreateWriteDataFormat
			(byte station, XGT_Request_Func emFunc, XGT_DataType emDatatype, List<XGTAddressData> pAddressList, XGT_MemoryType emMemtype, int pDataCount)
		{
			List<XGTAddressData> lstAddress = new List<XGTAddressData>();

			foreach (XGTAddressData addr in pAddressList)
			{
				string vAddress = new XGKFastEnet().CreateValueName(emDatatype, emMemtype, addr.Address);

				addr.AddressString = vAddress;
				lstAddress.Add(addr);
			}
			List<byte> command = new List<byte>();
			if (XGT_DataType.Continue == emDatatype && XGT_Request_Func.Write == emFunc)
			{
				command.Add(0x05);    // ENQ
				command.AddRange(SoftBasic.BuildAsciiBytesFrom(station));
				command.Add(0x77);    // command w
				command.Add(0x53);    // command type: S
				command.Add(0x42);       // command type: B

				foreach (XGTAddressData addr in lstAddress)
				{
					command.AddRange(SoftBasic.BuildAsciiBytesFrom((byte)addr.AddressString.Length));
					command.AddRange(Encoding.ASCII.GetBytes(addr.AddressString));
					command.AddRange(SoftBasic.BuildAsciiBytesFrom((byte)addr.AddressByteArray.Length));
					command.AddRange(SoftBasic.BytesToAsciiBytes(addr.AddressByteArray));
				}
				command.Add(0x04);    // EOT

				int sum = 0;
				for (int i = 0; i < command.Count; i++)
				{
					sum += command[i];
				}
				command.AddRange(SoftBasic.BuildAsciiBytesFrom((byte)sum));
			}
			else
			{
				command.Add(0x05);    // ENQ
				command.AddRange(SoftBasic.BuildAsciiBytesFrom(station));
				command.Add(0x77);    // command w
				command.Add(0x53);    // command type: S
				command.Add(0x53);    // command type: S
				command.Add(0x30);    // Number of blocks
				command.Add(0x31);
				foreach (XGTAddressData addr in lstAddress)
				{

					command.AddRange(SoftBasic.BuildAsciiBytesFrom((byte)addr.AddressString.Length));
					command.AddRange(Encoding.ASCII.GetBytes(addr.AddressString));
					command.AddRange(SoftBasic.BytesToAsciiBytes(addr.AddressByteArray));

				}
				command.Add(0x04);    // EOT

				int sum = 0;
				for (int i = 0; i < command.Count; i++)
				{
					sum += command[i];
				}
				command.AddRange(SoftBasic.BuildAsciiBytesFrom((byte)sum));
			}
			return command.ToArray();

		}
		/// <summary>
		/// Extract actual data form plc response
		/// </summary>
		/// <param name="response">response data</param>
		/// <param name="isRead">read</param>
		/// <returns>result</returns>
		public static OperateResult<byte[]> ExtractActualData(byte[] response, bool isRead)
		{
			try
			{
				if (isRead)
				{
					if (response[0] == 0x06)
					{
						byte[] buffer = new byte[response.Length - 13];
						Array.Copy(response, 10, buffer, 0, buffer.Length);
						return OperateResult.CreateSuccessResult(SoftBasic.AsciiBytesToBytes(buffer));
					}
					else
					{
						byte[] buffer = new byte[response.Length - 9];
						Array.Copy(response, 6, buffer, 0, buffer.Length);
						return new OperateResult<byte[]>(BitConverter.ToUInt16(SoftBasic.AsciiBytesToBytes(buffer), 0), "Data:" + SoftBasic.ByteToHexString(response));
					}
				}
				else
				{
					if (response[0] == 0x06)
					{
						return OperateResult.CreateSuccessResult(new byte[0]);
					}
					else
					{
						byte[] buffer = new byte[response.Length - 9];
						Array.Copy(response, 6, buffer, 0, buffer.Length);
						return new OperateResult<byte[]>(BitConverter.ToUInt16(SoftBasic.AsciiBytesToBytes(buffer), 0), "Data:" + SoftBasic.ByteToHexString(response));
					}
				}
			}
			catch (Exception ex)
			{
				return new OperateResult<byte[]>(ex.Message);
			}
		}

		#region Object Override

		/// <inheritdoc/>
		public override string ToString() => $"XGKCnet[{PortName}:{BaudRate}]";

		#endregion
	}
}
