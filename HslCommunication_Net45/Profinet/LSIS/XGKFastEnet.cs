using HslCommunication.BasicFramework;
using HslCommunication.Core;
using HslCommunication.Core.IMessage;
using HslCommunication.Core.Net;
using HslCommunication.Reflection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
#if !NET35 && !NET20
using System.Threading.Tasks;
#endif

namespace HslCommunication.Profinet.LSIS
{
    /// <summary>
    /// XGK Fast Enet I/F module supports open Ethernet. It provides network configuration that is to connect LSIS and other company PLC, PC on network
    /// </summary>
    /// <remarks>
    /// Address example likes the follow
    /// <list type="table">
    ///   <listheader>
    ///     <term>地址名称</term>
    ///     <term>地址代号</term>
    ///     <term>示例</term>
    ///     <term>地址进制</term>
    ///     <term>字操作</term>
    ///     <term>位操作</term>
    ///     <term>备注</term>
    ///   </listheader>
    ///   <item>
    ///     <term>*</term>
    ///     <term>P</term>
    ///     <term>PX100,PB100,PW100,PD100,PL100</term>
    ///     <term>10</term>
    ///     <term>√</term>
    ///     <term>√</term>
    ///     <term></term>
    ///   </item>
    ///   <item>
    ///     <term>*</term>
    ///     <term>M</term>
    ///     <term>MX100,MB100,MW100,MD100,ML100</term>
    ///     <term>10</term>
    ///     <term>√</term>
    ///     <term>√</term>
    ///     <term></term>
    ///   </item>
    ///   <item>
    ///     <term>*</term>
    ///     <term>L</term>
    ///     <term>LX100,LB100,LW100,LD100,LL100</term>
    ///     <term>10</term>
    ///     <term>√</term>
    ///     <term>√</term>
    ///     <term></term>
    ///   </item>
    ///   <item>
    ///     <term>*</term>
    ///     <term>K</term>
    ///     <term>KX100,KB100,KW100,KD100,KL100</term>
    ///     <term>10</term>
    ///     <term>√</term>
    ///     <term>√</term>
    ///     <term></term>
    ///   </item>
    ///   <item>
    ///     <term>*</term>
    ///     <term>F</term>
    ///     <term>FX100,FB100,FW100,FD100,FL100</term>
    ///     <term>10</term>
    ///     <term>√</term>
    ///     <term>√</term>
    ///     <term></term>
    ///   </item>
    ///   <item>
    ///     <term></term>
    ///     <term>T</term>
    ///     <term>TX100,TB100,TW100,TD100,TL100</term>
    ///     <term>10</term>
    ///     <term>√</term>
    ///     <term>√</term>
    ///     <term></term>
    ///   </item>
    ///   <item>
    ///     <term></term>
    ///     <term>C</term>
    ///     <term>CX100,CB100,CW100,CD100,CL100</term>
    ///     <term>10</term>
    ///     <term>√</term>
    ///     <term>√</term>
    ///     <term></term>
    ///   </item>
    ///   <item>
    ///     <term></term>
    ///     <term>D</term>
    ///     <term>DX100,DB100,DW100,DD100,DL100</term>
    ///     <term>10</term>
    ///     <term>√</term>
    ///     <term>√</term>
    ///     <term></term>
    ///   </item>
    ///   <item>
    ///     <term></term>
    ///     <term>S</term>
    ///     <term>SX100,SB100,SW100,SD100,SL100</term>
    ///     <term>10</term>
    ///     <term>√</term>
    ///     <term>√</term>
    ///     <term></term>
    ///   </item>
    ///   <item>
    ///     <term></term>
    ///     <term>Q</term>
    ///     <term>QX100,QB100,QW100,QD100,QL100</term>
    ///     <term>10</term>
    ///     <term>√</term>
    ///     <term>√</term>
    ///     <term></term>
    ///   </item>
    ///   <item>
    ///     <term></term>
    ///     <term>I</term>
    ///     <term>IX100,IB100,IW100,ID100,IL100</term>
    ///     <term>10</term>
    ///     <term>√</term>
    ///     <term>√</term>
    ///     <term></term>
    ///   </item>
    ///   <item>
    ///     <term></term>
    ///     <term>N</term>
    ///     <term>NX100,NB100,NW100,ND100,NL100</term>
    ///     <term>10</term>
    ///     <term>√</term>
    ///     <term>√</term>
    ///     <term></term>
    ///   </item>
    ///   <item>
    ///     <term></term>
    ///     <term>U</term>
    ///     <term>UX100,UB100,UW100,UD100,UL100</term>
    ///     <term>10</term>
    ///     <term>√</term>
    ///     <term>√</term>
    ///     <term></term>
    ///   </item>
    ///   <item>
    ///     <term></term>
    ///     <term>Z</term>
    ///     <term>ZX100,ZB100,ZW100,ZD100,ZL100</term>
    ///     <term>10</term>
    ///     <term>√</term>
    ///     <term>√</term>
    ///     <term></term>
    ///   </item>
    ///   <item>
    ///     <term></term>
    ///     <term>R</term>
    ///     <term>RX100,RB100,RW100,RD100,RL100</term>
    ///     <term>10</term>
    ///     <term>√</term>
    ///     <term>√</term>
    ///     <term></term>
    ///   </item>
    /// </list>
    /// </remarks>
    public class XGKFastEnet : NetworkDeviceBase
    {
        #region Const Value

        private string CompanyID1 = "LSIS-XGT";
        // private string CompanyID2 = "LGIS-GLOGA";
        private LSCpuInfo cpuInfo = LSCpuInfo.XGK;

        private byte baseNo = 0;
        private byte slotNo = 3;

        #endregion
        #region Constractor

        /// <summary>
        /// Instantiate a Default object
        /// </summary>
        public XGKFastEnet()
        {
            WordLength = 2;
            IpAddress = "127.0.0.1";
            Port = 2004;
            this.ByteTransform = new RegularByteTransform();
        }

        /// <summary>
        /// Instantiate a object by ipaddress and port
        /// </summary>
        /// <param name="ipAddress">the ip address of the plc</param>
        /// <param name="port">the port of the plc, default is 2004</param>
        public XGKFastEnet(string ipAddress, int port)
        {
            WordLength = 2;
            IpAddress = ipAddress;
            Port = port;
            this.ByteTransform = new RegularByteTransform();
        }

        /// <summary>
        /// Instantiate a object by ipaddress, port, cpuType, slotNo
        /// </summary>
        /// <param name="CpuType">CpuType</param>
        /// <param name="ipAddress">the ip address of the plc</param>
        /// <param name="port">he port of the plc, default is 2004</param>
        /// <param name="slotNo">slot number</param>
        public XGKFastEnet(string CpuType, string ipAddress, int port, byte slotNo)
        {
            this.SetCpuType = CpuType;
            WordLength = 2;
            IpAddress = ipAddress;
            Port = port;
            this.slotNo = slotNo;
            this.ByteTransform = new RegularByteTransform();
        }

        /// <inheritdoc/>
        protected override INetMessage GetNewNetMessage() => new LsisFastEnetMessage();

        #endregion
        #region Public Properties

        /// <summary>
        /// set plc
        /// </summary>
        public string SetCpuType { get; set; }

        /// <summary>
        /// CPU TYPE
        /// </summary>
        public string CpuType { get; private set; }

        /// <summary>
        /// Cpu is error
        /// </summary>
        public bool CpuError { get; private set; }

        /// <summary>
        /// RUN, STOP, ERROR, DEBUG
        /// </summary>
        public LSCpuStatus LSCpuStatus { get; private set; }

        /// <summary>
        /// FEnet I/F module’s Base No.
        /// </summary>
        public byte BaseNo
        {
            get => baseNo;
            set => baseNo = value;
        }

        /// <summary>
        /// FEnet I/F module’s Slot No.
        /// </summary>
        public byte SlotNo
        {
            get => slotNo;
            set => slotNo = value;
        }
        /// <summary>
        /// 
        /// </summary>
        public LSCpuInfo CpuInfo { get => cpuInfo; set => cpuInfo = value; }
        /// <summary>
        /// 
        /// </summary>
        public string CompanyID { get => CompanyID1; set => CompanyID1 = value; }

        #endregion
        #region Async Read Write Byte
#if !NET35 && !NET20
        /// <inheritdoc/>
        public override async Task<OperateResult<bool[]>> ReadBoolAsync(string address, ushort length)
        {
            OperateResult<byte[]> coreResult = null;
               List <XGTAddressData> lstAdress = new List<XGTAddressData>();
            string[] ArrayAdress = address.Split(new string[] { ";", "," }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string item in ArrayAdress)
            {
                XGTAddressData addrData = new XGTAddressData();
                 var DataTypeResult1 = GetDataTypeToAddress(item);
                if (DataTypeResult1.Content2) addrData.Address = item.Substring(1);
                  else addrData.Address = item.Substring(2);
                lstAdress.Add(addrData);
            }
            var analysisResult = AnalysisAddress(ArrayAdress[0]);
            if (!analysisResult.IsSuccess) return OperateResult.CreateFailedResult<bool[]>(analysisResult);

            var DataTypeResult = GetDataTypeToAddress(ArrayAdress[0]);
            if (!DataTypeResult.IsSuccess) return OperateResult.CreateFailedResult<bool[]>(DataTypeResult);
            // build read command
            if (DataTypeResult.Content1 == XGT_DataType.Continue)
            {
                coreResult = Read(DataTypeResult.Content1, lstAdress, analysisResult.Content, 1, length);
            }
            else
            {
                coreResult = Read(DataTypeResult.Content1, lstAdress, analysisResult.Content, 1);
            }

            // communication
            var read = await ReadFromCoreServerAsync(coreResult.Content);
            if (!read.IsSuccess) return OperateResult.CreateFailedResult<bool[]>(read);

            if (lstAdress.Count > 1)
            {
                OperateResult<bool[]> extract = ExtractActualDataBool(read.Content);

                if (!extract.IsSuccess) return OperateResult.CreateFailedResult<bool[]>(extract);
                return OperateResult.CreateSuccessResult(extract.Content);
            }
            else
            {
                OperateResult<byte[]> extract = ExtractActualData(read.Content);
                if (!extract.IsSuccess) return OperateResult.CreateFailedResult<bool[]>(extract);

                return OperateResult.CreateSuccessResult(SoftBasic.ByteToBoolArray(extract.Content));
            }

        }

        /// <inheritdoc cref="ReadCoil(string)"/>
        public async Task<OperateResult<bool>> ReadCoilAsync(string address) => await ReadBoolAsync(address);

        /// <inheritdoc cref="ReadCoil(string, ushort)"/>
        public async Task<OperateResult<bool[]>> ReadCoilAsync(string address, ushort length) => await ReadBoolAsync(address, length);

        /// <inheritdoc cref="ReadByte(string)"/>
        public async Task<OperateResult<byte>> ReadByteAsync(string address) => ByteTransformHelper.GetResultFromArray(await ReadAsync(address, 1));

        /// <inheritdoc cref="Write(string, byte)"/>
        public async Task<OperateResult> WriteAsync(string address, byte value) => await WriteAsync(address, new byte[] { value });

        /// <inheritdoc cref="WriteCoil(string, bool)"/>
        public async Task<OperateResult> WriteCoilAsync(string address, bool value) => await WriteAsync(address, new byte[] { (byte)(value == true ? 0x01 : 0x00), 0x00 });

        /// <inheritdoc cref="Write(string, bool)"/>
        public override async Task<OperateResult> WriteAsync(string address, bool value) => await WriteCoilAsync(address, value);
#endif
        #endregion
        #region Read Write Support

        /// <inheritdoc/>
        [HslMqttApi("ReadByteArray", "")]
        public override OperateResult<byte[]> Read(string address, ushort length)
        {
            OperateResult<byte[]> coreResult = null;
            List <XGTAddressData> lstAdress = new List<XGTAddressData>();
            string[] ArrayAdress = address.Split(new string[] { ";", "," }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string item in ArrayAdress)
            {

                XGTAddressData addrData = new XGTAddressData();
                var DataTypeResult1 = GetDataTypeToAddress(item);
                if (DataTypeResult1.Content2) addrData.Address = item.Substring(1);
                else addrData.Address = item.Substring(2);
                lstAdress.Add(addrData);
            }
            var analysisResult = AnalysisAddress(ArrayAdress[0]);
            if (!analysisResult.IsSuccess) return OperateResult.CreateFailedResult<byte[]>(analysisResult);

            var DataTypeResult = GetDataTypeToAddress(ArrayAdress[0]);
            if (!DataTypeResult.IsSuccess) return OperateResult.CreateFailedResult<byte[]>(DataTypeResult);
 
            // build read command
            if (DataTypeResult.Content1 == XGT_DataType.Continue)
            {
                coreResult = Read(DataTypeResult.Content1, lstAdress, analysisResult.Content, 1, length);
            }
            else
            {
                coreResult = Read(DataTypeResult.Content1, lstAdress, analysisResult.Content, 1);
            }
            if (!coreResult.IsSuccess) return OperateResult.CreateFailedResult<byte[]>(coreResult);

            // communication
            var read = ReadFromCoreServer(coreResult.Content);
            if (!read.IsSuccess) return OperateResult.CreateFailedResult<byte[]>(read);


            if (lstAdress.Count > 1)
            {
                // analysis read result
                return ExtractActualDatabyte(read.Content);
            }
            else
            {
                // analysis read result
                return ExtractActualData(read.Content);
            }

            
        }

        /// <inheritdoc/>
        [HslMqttApi("WriteByteArray", "")]
        public override OperateResult Write(string address, byte[] value)
        {
            OperateResult<byte[]> coreResult = null;
            List<XGTAddressData> lstAdress = new List<XGTAddressData>();
            string[] ArrayAdress = address.Split(new string[] { ";", "," }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string item in ArrayAdress)
            {
                XGTAddressData addrData = new XGTAddressData();
                var DataTypeResult1 = GetDataTypeToAddress(item);
                if (DataTypeResult1.Content2) addrData.Address = item.Substring(1);
                else addrData.Address = item.Substring(2);
                addrData.DataByteArray = value;

                lstAdress.Add(addrData);
            }
            var analysisResult = AnalysisAddress(address);
            if (!analysisResult.IsSuccess) return OperateResult.CreateFailedResult<byte[]>(analysisResult);

            var DataTypeResult = GetDataTypeToAddress(address);
            if (!DataTypeResult.IsSuccess) return OperateResult.CreateFailedResult<byte[]>(DataTypeResult);
            // build write command
            if (DataTypeResult.Content1 == XGT_DataType.Continue)
            {
                coreResult = Write( DataTypeResult.Content1, lstAdress, analysisResult.Content, 1, value.Length );
            }
            else
            {
                coreResult = Write(DataTypeResult.Content1, lstAdress, analysisResult.Content, 1);
            }
         
            if (!coreResult.IsSuccess) return coreResult;

            // communication
            var read = ReadFromCoreServer(coreResult.Content);
            if (!read.IsSuccess) return OperateResult.CreateFailedResult<byte[]>(read);

            // analysis read result
            return ExtractActualData(read.Content);
        }

        #endregion
        #region Read Write Byte

        /// <inheritdoc/>
        [HslMqttApi("ReadBoolArray", "")]
        public override OperateResult<bool[]> ReadBool(string address, ushort length)
        {
            OperateResult<byte[]> coreResult = null;
            List<XGTAddressData> lstAdress = new List<XGTAddressData>();

            string[] ArrayAdress = address.Split(new string[] { ";", "," }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string item in ArrayAdress)
            {
                XGTAddressData addrData = new XGTAddressData();
                var DataTypeResult1 = GetDataTypeToAddress(item);
                if (DataTypeResult1.Content2) addrData.Address = item.Substring(1);
                  else addrData.Address = item.Substring(2);
                lstAdress.Add(addrData);
            }
            var analysisResult = AnalysisAddress(ArrayAdress[0]);
            if (!analysisResult.IsSuccess) return OperateResult.CreateFailedResult<bool[]>(analysisResult);

            var DataTypeResult = GetDataTypeToAddress(ArrayAdress[0]);
            if (!DataTypeResult.IsSuccess) return OperateResult.CreateFailedResult<bool[]>(DataTypeResult);

            // build read command
            if (DataTypeResult.Content1 == XGT_DataType.Continue)
            {
                coreResult = Read(DataTypeResult.Content1, lstAdress, analysisResult.Content, 1, length);
            }
            else
            {
                coreResult = Read(DataTypeResult.Content1, lstAdress, analysisResult.Content, 1);
            }
            if (!coreResult.IsSuccess) return OperateResult.CreateFailedResult<bool[]>(coreResult);

            // communication
            var read = ReadFromCoreServer(coreResult.Content);
            if (!read.IsSuccess) return OperateResult.CreateFailedResult<bool[]>(read);

            if (lstAdress.Count > 1)
            {
                OperateResult<bool[]> extract = ExtractActualDataBool(read.Content);

                if (!extract.IsSuccess) return OperateResult.CreateFailedResult<bool[]>(extract);
                return OperateResult.CreateSuccessResult(extract.Content);
            }
            else
            {
                OperateResult<byte[]> extract = ExtractActualData(read.Content);
                if (!extract.IsSuccess) return OperateResult.CreateFailedResult<bool[]>(extract);

                return OperateResult.CreateSuccessResult(SoftBasic.ByteToBoolArray(extract.Content));
            }
          

        }

        /// <summary>
        /// ReadCoil
        /// </summary>
        /// <param name="address">Start address</param>
        /// <returns>Whether to read the successful</returns>
        public OperateResult<bool> ReadCoil(string address) => ReadBool(address);

        /// <summary>
        /// ReadCoil
        /// </summary>
        /// <param name="address">Start address</param>
        /// <param name="length">read address length</param>
        /// <returns>Whether to read the successful</returns>
        public OperateResult<bool[]> ReadCoil(string address, ushort length) => ReadBool(address, length);

        /// <summary>
        /// Read single byte value from plc
        /// </summary>
        /// <param name="address">Start address</param>
        /// <returns>Whether to write the successful</returns>
        [HslMqttApi("ReadByte", "")]
        public OperateResult<byte> ReadByte(string address) => ByteTransformHelper.GetResultFromArray(Read(address, 1));

        /// <summary>
        /// Write single byte value to plc
        /// </summary>
        /// <param name="address">Start address</param>
        /// <param name="value">value</param>
        /// <returns>Whether to write the successful</returns>
        [HslMqttApi("WriteByte", "")]
        public OperateResult Write(string address, byte value) => Write(address, new byte[] { value });

        /// <summary>
        /// WriteCoil
        /// </summary>
        /// <param name="address">Start address</param>
        /// <param name="value">bool value</param>
        /// <returns>Whether to write the successful</returns>
        public OperateResult WriteCoil(string address, bool value)
        {
            return Write(address, new byte[] { (byte)(value == true ? 0x01 : 0x00), 0x00 });
        }

        /// <summary>
        /// WriteCoil
        /// </summary>
        /// <param name="address">Start address</param>
        /// <param name="value">bool value</param>
        /// <returns>Whether to write the successful</returns>
        [HslMqttApi("WriteBool", "")]
        public override OperateResult Write(string address, bool value) => WriteCoil(address, value);

        #endregion
        #region Static Helper
        /// <summary>
        /// Read
        /// </summary>
        /// <param name="pDataType"></param>
        /// <param name="pAddress"></param>
        /// <param name="pMemtype"></param>
        /// <param name="pInvokeID"></param>
        /// <param name="pDataCount"></param>
        /// <returns></returns>
        public OperateResult<byte[]> Read(XGT_DataType pDataType, List<XGTAddressData> pAddress, XGT_MemoryType pMemtype, int pInvokeID, int pDataCount = 0)
        {


            if (pAddress.Count > 16)
            {
                return new OperateResult<byte[]>("You cannot read more than 16 pieces.");
            }
            else
            {


                try
                {
                    byte[] data = CreateReadDataFormat(XGT_Request_Func.Read, pDataType, pAddress, pMemtype, pDataCount);
                    byte[] header = CreateHeader(pInvokeID, data.Length);

                    //FRAME TO TRANSMIT
                    byte[] tcpFrame = new byte[header.Length + data.Length];

                    //CREATE A TRANSMISSION FRAME BY COMBINING APPLICAITON HEADER AND DATA INFORMATION.
                    int idx = 0;

                    AddByte(header, ref idx, ref tcpFrame);
                    AddByte(data, ref idx, ref tcpFrame);


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
        /// <param name="pInvokeID"></param>
        /// <param name="pDataCount"></param>
        /// <returns></returns>
        public OperateResult<byte[]> Write(XGT_DataType pDataType, List<XGTAddressData> pAddressList, XGT_MemoryType pMemtype, int pInvokeID, int pDataCount = 0)
        {
           

            try
            {
                byte[] data = CreateWriteDataFormat(XGT_Request_Func.Write, pDataType, pAddressList, pMemtype, pDataCount);
                byte[] header = CreateHeader(pInvokeID, data.Length);


                //FRAME TO TRANSMIT
                byte[] tcpFrame = new byte[header.Length + data.Length];

                //CREATE A TRANSMISSION FRAME BY COMBINING APPLICAITON HEADER AND DATA INFORMATION.
                int idx = 0;

                AddByte(header, ref idx, ref tcpFrame);
                AddByte(data, ref idx, ref tcpFrame);



                return OperateResult.CreateSuccessResult(tcpFrame);
            }
            catch (Exception ex)
            {
                return new OperateResult<byte[]>("ERROR:" + ex.Message.ToString());
            }

           
        }

        /// <summary>
        /// CreateHeader
        /// </summary>
        /// <param name="pInvokeID"></param>
        /// <param name="pDataByteLenth"></param>
        /// <returns></returns>
        public byte[] CreateHeader(int pInvokeID, int pDataByteLenth)
        {

            byte[] CompanyID = Encoding.ASCII.GetBytes(this.CompanyID);  //Company ID (8 Byte)
            byte[] Reserved = BitConverter.GetBytes((short)0);  //Reserved 예약영역  2 Byte (고정값  value is fix)
            byte[] PLCInfo = BitConverter.GetBytes((short)0); // PLC Info >> Client 0x00;
            byte[] CPUInfo = new byte[1];
            switch (cpuInfo)
            {
                case LSCpuInfo.XGK: CPUInfo[0] = 0xA0; break;
                case LSCpuInfo.XGI: CPUInfo[0] = 0xA4; break;
                case LSCpuInfo.XGR: CPUInfo[0] = 0xA8; break;
                case LSCpuInfo.XGB_MK: CPUInfo[0] = 0xB0; break;
                case LSCpuInfo.XGB_IEC: CPUInfo[0] = 0xB4; break;
                default: break;
            }
            //CPUInfo[0] = 0xA0;            //CPU INFO 1 Byte
            byte[] SOF = new byte[1];
            SOF[0] = 0x33;                //Source of Frame ( 고정값 value is fix)
            byte[] InvokeID = BitConverter.GetBytes((short)pInvokeID);

            byte[] Length = BitConverter.GetBytes((short)pDataByteLenth); //Application Data Format 바이트 크기
            byte[] FEnetPosition = new byte[1];


            FEnetPosition[0] = (byte)(baseNo * 16 + slotNo);     //Bit0~3 : 이더넷 모듈의 슬롯 번호 ,  Bit4~7 : 이더넷 모듈의 베이스 번호


            byte[] Reserved2 = new byte[1];
            Reserved2[0] = 0x00;

            //헤더 프레임의 길이 계산.
            int vLenth = CompanyID.Length + Reserved.Length + PLCInfo.Length + CPUInfo.Length + SOF.Length
                                  + InvokeID.Length + Length.Length + FEnetPosition.Length + Reserved2.Length;

            byte[] header = new byte[vLenth];

            int idx = 0;
            AddByte(CompanyID, ref idx, ref header);
            AddByte(Reserved, ref idx, ref header);
            AddByte(PLCInfo, ref idx, ref header);
            AddByte(CPUInfo, ref idx, ref header);
            AddByte(SOF, ref idx, ref header);
            AddByte(InvokeID, ref idx, ref header);
            AddByte(Length, ref idx, ref header);
            AddByte(FEnetPosition, ref idx, ref header);
            AddByte(Reserved2, ref idx, ref header);


            return header;
        }



        //Create application data READ format
        private byte[] CreateReadDataFormat
            (XGT_Request_Func emFunc, XGT_DataType emDatatype, List<XGTAddressData> pAddressList, XGT_MemoryType emMemtype, int pDataCount)
        {
            List<XGTAddressData> lstAddress = new List<XGTAddressData>();
            int vLenth = 0;  //데이타 포맷 프레임의 크기

            byte[] command = BitConverter.GetBytes((short)emFunc); //StringToByteArray((int)emFunc, true);  //명령어 읽기,쓰기
            byte[] dataType = BitConverter.GetBytes((short)emDatatype);//StringToByteArray((int)emDatatype, true);  //데이터 타입

            byte[] reserved = BitConverter.GetBytes((short)0);  //예약영역 고정(0x0000)
            byte[] blockcount = BitConverter.GetBytes((short)pAddressList.Count); //블록수 

            //프레임 크기 설정 :  명령어(2) + 데이터타입(2) + 예약영역(2) + 블록수 (?) + 변수길이(?) + 변수(?)
            vLenth = command.Length + dataType.Length + reserved.Length + blockcount.Length;

            foreach (XGTAddressData addr in pAddressList)
            {
                string vAddress = CreateValueName(emDatatype, emMemtype, addr.Address);

                //byte[] value = Encoding.ASCII.GetBytes(vAddress);
                //byte[] valueLength = BitConverter.GetBytes((short)value.Length);

                XGTAddressData XgtAddr = new XGTAddressData();
                XgtAddr.AddressString = vAddress;

                lstAddress.Add(XgtAddr);


                vLenth += XgtAddr.AddressByteArray.Length + XgtAddr.LengthByteArray.Length;
            }

            if (XGT_DataType.Continue == emDatatype && XGT_Request_Func.Read == emFunc)
            {
                vLenth += 2;  //연속읽기 인 경우 2바이트 추가.(데이터 갯수)
            }

            byte[] data = new byte[vLenth];


            int idx = 0;
            AddByte(command, ref idx, ref data);
            AddByte(dataType, ref idx, ref data);
            AddByte(reserved, ref idx, ref data);
            AddByte(blockcount, ref idx, ref data);

            foreach (XGTAddressData addr in lstAddress)
            {
                AddByte(addr.LengthByteArray, ref idx, ref data);
                AddByte(addr.AddressByteArray, ref idx, ref data);
            }

            /* 연속 읽기의 경우 읽을 갯수 지정. */
            if (XGT_DataType.Continue == emDatatype)
            {
                //데이터 타입이 연속 읽기 인 경우.
                byte[] vDataCount = BitConverter.GetBytes((short)pDataCount);
                AddByte(vDataCount, ref idx, ref data);
            }


            return data;
        }
        //Create application data WRITE format
        private byte[] CreateWriteDataFormat
            (XGT_Request_Func emFunc, XGT_DataType emDatatype, List<XGTAddressData> pAddressList, XGT_MemoryType emMemtype, int pDataCount)
        {

            int vLenth = 0; //Data format frame size

            byte[] command = BitConverter.GetBytes((short)emFunc);
            byte[] dataType = BitConverter.GetBytes((short)emDatatype);

            byte[] reserved = BitConverter.GetBytes((short)0);
            byte[] blockcount = BitConverter.GetBytes((short)pAddressList.Count);

            //Set frame size: command (2) + data type (2) + reserved area (2) + number of blocks (?) + variable length (?) + variable (?)
            vLenth = command.Length + dataType.Length + reserved.Length + blockcount.Length;

            List<XGTAddressData> lstAddress = new List<XGTAddressData>();

            foreach (XGTAddressData addr in pAddressList)
            {
                string vAddress = CreateValueName(emDatatype, emMemtype, addr.Address);

                addr.AddressString = vAddress;
                int oDataLength = 0;
                oDataLength = ((byte[])addr.DataByteArray).Length;

                vLenth += addr.AddressByteArray.Length + addr.LengthByteArray.Length + 2 + oDataLength; //Number of data + data length

                lstAddress.Add(addr);
            }

            if (XGT_DataType.Continue == emDatatype && XGT_Request_Func.Read == emFunc)
            {
                vLenth += 2; // In case of continuous reading, 2 bytes are added. (Number of data)
            }

            byte[] data = new byte[vLenth];


            int idx = 0;
            AddByte(command, ref idx, ref data);
            AddByte(dataType, ref idx, ref data);
            AddByte(reserved, ref idx, ref data);
            AddByte(blockcount, ref idx, ref data);


            foreach (XGTAddressData addr in lstAddress)
            {
                AddByte(addr.LengthByteArray, ref idx, ref data);
                AddByte(addr.AddressByteArray, ref idx, ref data);
            }



            foreach (XGTAddressData addr in lstAddress)
            {
                // In case of writing data
                byte[] count = BitConverter.GetBytes((short)addr.DataByteArray.Length);

                AddByte(count, ref idx, ref data);
                AddByte(addr.DataByteArray, ref idx, ref data);
            }


            return data;
        }

        /// <summary>
        ///Create a memory address variable name.
        /// </summary>
        /// <param name="dataType">데이터타입</param>
        /// <param name="memType">메모리타입</param>
        /// <param name="pAddress">주소번지</param>
        /// <returns></returns>
        public  string CreateValueName(XGT_DataType dataType, XGT_MemoryType memType, string pAddress)
        {
            string vReturn = string.Empty;

            string vMemTypeChar = this.GetMemTypeChar(memType); //Memory type
            string vDataTypeChar = this.GetTypeChar(dataType);  //Data type


            if (dataType == XGT_DataType.Continue)
            {
                //In case of continuous reading, it can only be expressed in byte units, so to read memory in word unit, you need to do address value *2.
                //2Byte = 1Word  
                pAddress = (Convert.ToInt32(pAddress) * 2).ToString();
            }

            if (dataType == XGT_DataType.Bit)
            {
                /*
                     When accessing the bit area in the variable name expression method, the data of the memory device
                     It must be expressed in the order of type units. To write the Cth bit of M172, M is
                     Since it is a WORD device, the process of calculating it in bit type is required as follows.
                     Incorrect expression: %MX172C
                     Correct representation: 172 x 16 (WORD) + 12 (BIT) = 2764
                      %MX2764
                  */
                int vSEQ = 0;
                string vAddress = pAddress.Substring( 0, pAddress.Length - 1 );  //Address address up to the last digit of the entered address value
                string Last = pAddress.Substring( pAddress.Length - 1 );  // The last digit of the input address is the bit position
                vSEQ = Convert.ToInt32( Last, 16 );
                if (string.IsNullOrEmpty( vAddress ))
                    pAddress = vSEQ.ToString( );
                else
                    pAddress = (Convert.ToInt32( vAddress ) * 16 + vSEQ).ToString( );
            }


            return $"%{vMemTypeChar}{vDataTypeChar}{pAddress}";
        }

        /// <summary>
        /// Char return according to data type
        /// </summary>
        /// <param name="type">데이터타입</param>
        /// <returns></returns>
        private string GetTypeChar(XGT_DataType type)
        {
            string vReturn = string.Empty; // 기본값은  Bit

            switch (type)
            {
                case XGT_DataType.Bit:
                    vReturn = XGT_Data_TypeClass.Bit;
                    break;
                case XGT_DataType.Byte:
                    vReturn = XGT_Data_TypeClass.Byte;
                    break;
                case XGT_DataType.Word:
                    vReturn = XGT_Data_TypeClass.Word;
                    break;
                case XGT_DataType.DWord:
                    vReturn = XGT_Data_TypeClass.DWord;
                    break;
                case XGT_DataType.LWord:
                    vReturn = XGT_Data_TypeClass.LWord;
                    break;
                case XGT_DataType.Continue:  // 연속읽기에는 ByteType만... 
                    vReturn = XGT_Data_TypeClass.Byte;
                    break;
                default:
                    vReturn = XGT_Data_TypeClass.Bit; ;
                    break;
            }

            return vReturn;
        }

        /// <summary>
        /// Char return according to memory type
        /// </summary>
        /// <param name="type">메모리타입</param>
        /// <returns></returns>
        private string GetMemTypeChar(XGT_MemoryType type)
        {
            string vReturn = string.Empty;
            switch (type)
            {
                case XGT_MemoryType.IO:
                    vReturn = XGT_Memory_TypeClass.IO;
                    break;
                case XGT_MemoryType.SubRelay:
                    vReturn = XGT_Memory_TypeClass.SubRelay;
                    break;
                case XGT_MemoryType.LinkRelay:
                    vReturn = XGT_Memory_TypeClass.LinkRelay;
                    break;
                case XGT_MemoryType.KeepRelay:
                    vReturn = XGT_Memory_TypeClass.KeepRelay;
                    break;
                case XGT_MemoryType.EtcRelay:
                    vReturn = XGT_Memory_TypeClass.EtcRelay;
                    break;
                case XGT_MemoryType.Timer:
                    vReturn = XGT_Memory_TypeClass.Timer;
                    break;
                case XGT_MemoryType.DataRegister:
                    vReturn = XGT_Memory_TypeClass.DataRegister;
                    break;
                case XGT_MemoryType.Counter:
                    vReturn = XGT_Memory_TypeClass.Counter;
                    break;
                case XGT_MemoryType.ComDataRegister:
                    vReturn = XGT_Memory_TypeClass.ComDataRegister;
                    break;
                case XGT_MemoryType.FileDataRegister:
                    vReturn = XGT_Memory_TypeClass.FileDataRegister;
                    break;
                case XGT_MemoryType.StepRelay:
                    vReturn = XGT_Memory_TypeClass.StepRelay;
                    break;
                case XGT_MemoryType.SpecialRegister:
                    vReturn = XGT_Memory_TypeClass.SpecialRegister;
                    break;
            }

            return vReturn;
        }




        /// <summary>
        /// 바이트 합치기
        /// </summary>
        /// <param name="item">개별바이트</param>
        /// <param name="idx">전체바이트에 개별바이트를 합칠 인덱스</param>
        /// <param name="header">전체바이트</param>
        /// <returns>전체 바이트 </returns>
        private byte[] AddByte(byte[] item, ref int idx, ref byte[] header)
        {
            Array.Copy(item, 0, header, idx, item.Length);
            idx += item.Length;

            return header;
        }
        /// <summary>
        /// AnalysisAddress XGT_MemoryType
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public static OperateResult<XGT_MemoryType> AnalysisAddress(string address)
        {
            // P,M,L,K,F,T
            // P,M,L,K,F,T,C,D,S
            XGT_MemoryType sb = new XGT_MemoryType();
            try
            {

                char[] types = new char[] { 'P', 'M', 'L', 'K', 'F', 'T', 'C', 'D', 'S', 'Q', 'I', 'N', 'U', 'Z', 'R' };
                bool exsist = false;

                for (int i = 0; i < types.Length; i++)
                {
                    if (types[i] == address[0])
                    {


                        switch (address[0])
                        {
                            case 'P': sb = XGT_MemoryType.IO; break;
                            case 'M': sb = XGT_MemoryType.SubRelay; break;
                            case 'L': sb = XGT_MemoryType.LinkRelay; break;
                            case 'K': sb = XGT_MemoryType.KeepRelay; break;
                            case 'F': sb = XGT_MemoryType.EtcRelay; break;
                            case 'T': sb = XGT_MemoryType.Timer; break;
                            case 'C': sb = XGT_MemoryType.Counter; break;
                            case 'D': sb = XGT_MemoryType.DataRegister; break;
                            case 'N': sb = XGT_MemoryType.ComDataRegister; break;
                            case 'R': sb = XGT_MemoryType.FileDataRegister; break;
                            case 'S': sb = XGT_MemoryType.StepRelay; break;
                            case 'U': sb = XGT_MemoryType.SpecialRegister; break;




                            default:
                                break;
                        }
                        exsist = true;
                        break;
                    }
                }

                if (!exsist) throw new Exception(StringResources.Language.NotSupportedDataType);
            }
            catch (Exception ex)
            {
                return new OperateResult<XGT_MemoryType>(ex.Message);
            }

            return OperateResult.CreateSuccessResult(sb);
        }
        /// <summary>
        /// GetDataTypeToAddress
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public static OperateResult<XGT_DataType,bool> GetDataTypeToAddress(string address)
        {
            // P,M,L,K,F,T
            // P,M,L,K,F,T,C,D,S
            XGT_DataType sb = new XGT_DataType();
             bool NoDataType=false;
            try
            {

                char[] types = new char[] { 'P', 'M', 'L', 'K', 'F', 'T', 'C', 'D', 'S', 'Q', 'I', 'N', 'U', 'Z', 'R' };
                bool exsist = false;

                for (int i = 0; i < types.Length; i++)
                {
                    if (types[i] == address[0])
                    {


                        switch (address[1])
                        {
                            case 'X':
                               
                                sb = XGT_DataType.Bit;
                                break;
                            case 'W':
                                
                                sb = XGT_DataType.Word;
                                break;
                            case 'D':
                                
                                sb = XGT_DataType.DWord;
                                break;
                            case 'L':
                             
                                sb = XGT_DataType.LWord;
                                break;
                            case 'B':
                               
                                sb = XGT_DataType.Byte;
                                break;
                            case 'C':
                               
                                sb = XGT_DataType.Continue;
                                break;
                            default:
                                NoDataType = true;
                                sb = XGT_DataType.Continue;
                                break;
                        }
                
                        exsist = true;
                        break;
                    }
                }

                if (!exsist) throw new Exception(StringResources.Language.NotSupportedDataType);
            }
            catch (Exception ex)
            {
                return new OperateResult<XGT_DataType,bool>(ex.Message);
            }

            return OperateResult.CreateSuccessResult(sb, NoDataType);
        }
        /// <summary>
        /// Returns true data content, supports read and write returns
        /// </summary>
        /// <param name="response">response data</param>
        /// <returns>real data</returns>
        public OperateResult<byte[]> ExtractActualData(byte[] response)
        {
            OperateResult<bool> read = GetCpuTypeToPLC(response);
            if (!read.IsSuccess) return new OperateResult<byte[]>(read.Message);
            if (response[20] == 0x59) return OperateResult.CreateSuccessResult(new byte[0]);  // write

            if (response[20] == 0x55)  // read
            {
                try
                {
                    ushort length = BitConverter.ToUInt16(response, 30);
                    byte[] content = new byte[length];
                    Array.Copy(response, 32, content, 0, length);
                    return OperateResult.CreateSuccessResult(content);
                }
                catch (Exception ex)
                {
                    return new OperateResult<byte[]>(ex.Message);
                }
            }

            return new OperateResult<byte[]>(StringResources.Language.NotSupportedFunction);
        }
        /// <summary>
        /// SetCpuType
        /// </summary>
        /// <param name="response"></param>
        /// <returns></returns>
        public OperateResult<bool> GetCpuTypeToPLC(byte[] response)
        {
            try
            {
                if (response.Length < 20) return new OperateResult<bool>("Length is less than 20:" + SoftBasic.ByteToHexString(response));

                ushort plcInfo = BitConverter.ToUInt16(response, 10);
                BitArray array_plcInfo = new BitArray(BitConverter.GetBytes(plcInfo));
                var p = plcInfo % 32;
                switch (plcInfo % 32)
                {
                    case 1: CpuType = "XGK/R-CPUH"; break;
                    case 2: CpuType = "XGK-CPUS"; break;
                    case 4: CpuType = "XGK-CPUE"; break;
                    case 5: CpuType = "XGK/R-CPUH"; break;
                    case 6: CpuType = "XGB/XBCU"; break;
                }

                CpuError = array_plcInfo[7];
                if (array_plcInfo[8]) LSCpuStatus = LSCpuStatus.RUN;
                if (array_plcInfo[9]) LSCpuStatus = LSCpuStatus.STOP;
                if (array_plcInfo[10]) LSCpuStatus = LSCpuStatus.ERROR;
                if (array_plcInfo[11]) LSCpuStatus = LSCpuStatus.DEBUG;

                if (response.Length < 28) return new OperateResult<bool>("Length is less than 28:" + SoftBasic.ByteToHexString(response));
                ushort error = BitConverter.ToUInt16(response, 26);
                if (error > 0) return new OperateResult<bool>(response[28], "Error:" + GetErrorDesciption(response[28]));

            }
            catch (Exception ex)
            {

                return new OperateResult<bool>(ex.Message);
            }
            return OperateResult.CreateSuccessResult(true);
        }
        /// <summary>
        /// Returns true data content, supports read and write returns
        /// </summary>
        /// <param name="response">response data</param>
        /// <returns>real data</returns>
        public OperateResult<bool[]> ExtractActualDataBool(byte[] response)
        {
            OperateResult<bool> read = GetCpuTypeToPLC(response);
            if (!read.IsSuccess) return new OperateResult<bool[]>(read.Message);
            if (response[20] == 0x59) return OperateResult.CreateSuccessResult(new bool[0]);  // write


            int BlockCount;

            if (response[20] == 0x55)  // read
            {


                // Defined as data from index 28
                int index = 28;


                byte[] blockCount = new byte[2];  //Block counter
                byte[] dataByteCount = new byte[2];  //Data size
                byte[] data = new byte[2];  //Block counter

                Array.Copy(response, index, blockCount, 0, 2);
                BlockCount = BitConverter.ToInt16(blockCount, 0);
                List<bool> content = new List<bool>();
                index = index + 2;

                // There is the number of data as much as the block counter.


                try
                {
                    for (int i = 0; i < BlockCount; i++)
                    {
                        Array.Copy(response, index, dataByteCount, 0, 2);
                        int biteSize = BitConverter.ToInt16(dataByteCount, 0); //Data size.

                        index = index + 2;
                        data = new byte[biteSize];
                        Array.Copy(response, index, data, 0, biteSize);

                        index = index + biteSize;  //Next index
                        content.Add(BitConverter.ToBoolean(data, 0));
                    }

                    return OperateResult.CreateSuccessResult(content.ToArray());
                }
                catch (Exception ex)
                {
                    return new OperateResult<bool[]>(ex.Message);
                }
            }

            return new OperateResult<bool[]>(StringResources.Language.NotSupportedFunction);
        }

        /// <summary>
        /// Returns true data content, supports read and write returns
        /// </summary>
        /// <param name="response">response data</param>
        /// <returns>real data</returns>
        public OperateResult<byte[]> ExtractActualDatabyte(byte[] response)
        {
            OperateResult<bool> read =  GetCpuTypeToPLC(response);
            if (!read.IsSuccess) return new OperateResult<byte[]>(read.Message);
            
            if (response[20] == 0x59) return OperateResult.CreateSuccessResult(new byte[0]);  // write


            int BlockCount;

            if (response[20] == 0x55)  // read
            {


                // Defined as data from index 28
                int index = 28;


                byte[] blockCount = new byte[2];  //Block counter
                byte[] dataByteCount = new byte[2];  //Data size
                byte[] data = new byte[2];  //Block counter

                Array.Copy(response, index, blockCount, 0, 2);
                BlockCount = BitConverter.ToInt16(blockCount, 0);
                List<byte> content = new List<byte>();
                index = index + 2;

                // There is the number of data as much as the block counter.


                try
                {
                    for (int i = 0; i < BlockCount; i++)
                    {
                        Array.Copy(response, index, dataByteCount, 0, 2);
                        int biteSize = BitConverter.ToInt16(dataByteCount, 0); //Data size.

                        index = index + 2;
                        //data = new byte[biteSize];
                        Array.Copy(response, index, data, 0, biteSize);

                        index = index + biteSize;  //Next index
                        
                        content.AddRange(data);
                    }
                   

                    return OperateResult.CreateSuccessResult(content.ToArray());
                }
                catch (Exception ex)
                {
                    return new OperateResult<byte[]>(ex.Message);
                }
            }

            return new OperateResult<byte[]>(StringResources.Language.NotSupportedFunction);
        }
        /// <summary>
        /// get the description of the error code meanning
        /// </summary>
        /// <param name="code">code value</param>
        /// <returns>string information</returns>
        public static string GetErrorDesciption(byte code)
        {
            switch (code)
            {
                case 0: return "Normal";
                case 1: return "Physical layer error (TX, RX unavailable)";
                case 3: return "There is no identifier of Function Block to receive in communication channel";
                case 4: return "Mismatch of data type";
                case 5: return "Reset is received from partner station";
                case 6: return "Communication instruction of partner station is not ready status";
                case 7: return "Device status of remote station is not desirable status";
                case 8: return "Access to some target is not available";
                case 9: return "Can’ t deal with communication instruction of partner station by too many reception";
                case 10: return "Time Out error";
                case 11: return "Structure error";
                case 12: return "Abort";
                case 13: return "Reject(local/remote)";
                case 14: return "Communication channel establishment error (Connect/Disconnect)";
                case 15: return "High speed communication and connection service error";
                case 33: return "Can’t find variable identifier";
                case 34: return "Address error";
                case 50: return "Response error";
                case 113: return "Object Access Unsupported";
                case 187: return "Unknown error code (communication code of other company) is received";
                default: return "Unknown error";
            }
        }

        #endregion
        #region Override

        /// <inheritdoc/>
        public override string ToString() => $"XGkFastEnet[{IpAddress}:{Port}]";

        #endregion

    }


}


