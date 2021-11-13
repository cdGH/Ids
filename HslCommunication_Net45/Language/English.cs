using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HslCommunication.Language
{
	/// <summary>
	/// English Version Text
	/// </summary>
	public class English : DefaultLanguage
	{
#pragma warning disable CS1591 // 缺少对公共可见类型或成员的 XML 注释
		/***********************************************************************************
		 * 
		 *    Normal Info
		 * 
		 ************************************************************************************/

		public override string TimeDescriptionSecond => " Second";
		public override string TimeDescriptionMinute => " Minute";
		public override string TimeDescriptionHour => " Hour";
		public override string TimeDescriptionDay => " Day";
		public override string AuthorizationFailed => "System authorization failed, need to use activation code authorization, thank you for your support. Active device number：" + Authorization.iahsduiwikaskfhishfdi;
		public override string InsufficientPrivileges => "The current method interface or class is only open to commercial authorized users with insufficient permissions. Thank you for your support. If you need commercial authorization, please contact QQ200962190, WeChat: 13516702732, Email: hsl200909@163.com";
		public override string ConnectedFailed => "Connected Failed: ";
		public override string ConnectedSuccess => "Connect Success!";
		public override string ConnectTimeout => "Connected Timeout: {0}";
		public override string UnknownError => "Unknown Error";
		public override string ErrorCode => "Error Code: ";
		public override string TextDescription => "Description: ";
		public override string ExceptionMessage => "Exception Info: ";
		public override string ExceptionSource => "Exception Source：";
		public override string ExceptionType => "Exception Type：";
		public override string ExceptionStackTrace => "Exception Stack: ";
		public override string ExceptionTargetSite => "Exception Method: ";
		public override string ExceptionCustomer => "Error in user-defined method: ";
		public override string SuccessText => "Success";
		public override string TwoParametersLengthIsNotSame => "Two Parameter Length is not same";
		public override string NotSupportedDataType => "Unsupported DataType, input again";
		public override string NotSupportedFunction => "The current feature logic does not support";
		public override string DataLengthIsNotEnough => "Receive length is not enough，Should:{0},Actual:{1}";
		public override string ReceiveDataTimeout => "Receive timeout: ";
		public override string ReceiveDataLengthTooShort => "Receive length is too short: ";
		public override string MessageTip => "Message prompt:";
		public override string Close => "Close";
		public override string Time => "Time:";
		public override string SoftWare => "Software:";
		public override string BugSubmit => "Bug submit";
		public override string MailServerCenter => "Mail Center System";
		public override string MailSendTail => "Mail Service system issued automatically, do not reply";
		public override string IpAddressError => "IP address input exception, format is incorrect";
		public override string Send => "Send";
		public override string Receive => "Receive";
		public override string CheckDataTimeout => "When waiting to check the data, a timeout occurred. The timeout period is:";

		/***********************************************************************************
		 * 
		 *    System about
		 * 
		 ************************************************************************************/

		public override string SystemInstallOperater => "Install new software: ip address is";
		public override string SystemUpdateOperater => "Update software: ip address is";


		/***********************************************************************************
		 * 
		 *    Socket-related Information description
		 * 
		 ************************************************************************************/

		public override string SocketIOException => "Socket transport error: ";
		public override string SocketSendException => "Synchronous Data Send exception: ";
		public override string SocketHeadReceiveException => "Command header receive exception: ";
		public override string SocketContentReceiveException => "Content Data Receive exception: ";
		public override string SocketContentRemoteReceiveException => "Recipient content Data Receive exception: ";
		public override string SocketAcceptCallbackException => "Asynchronously accepts an incoming connection attempt: ";
		public override string SocketReAcceptCallbackException => "To re-accept incoming connection attempts asynchronously";
		public override string SocketSendAsyncException => "Asynchronous Data send Error: ";
		public override string SocketEndSendException => "Asynchronous data end callback send Error";
		public override string SocketReceiveException => "Asynchronous Data send Error: ";
		public override string SocketEndReceiveException => "Asynchronous data end receive instruction header error";
		public override string SocketRemoteCloseException => "An existing connection was forcibly closed by the remote host";


		/***********************************************************************************
		 * 
		 *    File related information
		 * 
		 ************************************************************************************/


		public override string FileDownloadSuccess => "File Download Successful";
		public override string FileDownloadFailed => "File Download exception";
		public override string FileUploadFailed => "File Upload exception";
		public override string FileUploadSuccess => "File Upload Successful";
		public override string FileDeleteFailed => "File Delete exception";
		public override string FileDeleteSuccess => "File deletion succeeded";
		public override string FileReceiveFailed => "Confirm File Receive exception";
		public override string FileNotExist => "File does not exist";
		public override string FileSaveFailed => "File Store failed";
		public override string FileLoadFailed => "File load failed";
		public override string FileSendClientFailed => "An exception occurred when the file was sent";
		public override string FileWriteToNetFailed => "File Write Network exception";
		public override string FileReadFromNetFailed => "Read file exceptions from the network";
		public override string FilePathCreateFailed => "Folder path creation failed: ";
		public override string FileRemoteNotExist => "The other file does not exist, cannot receive!";

		/***********************************************************************************
		 * 
		 *    Engine-related data for the server
		 * 
		 ************************************************************************************/

		public override string TokenCheckFailed => "Receive authentication token inconsistency";
		public override string TokenCheckTimeout => "Receive authentication timeout: ";
		public override string CommandHeadCodeCheckFailed => "Command header check failed";
		public override string CommandLengthCheckFailed => "Command length check failed";
		public override string NetClientAliasFailed => "Client's alias receive failed: ";
		public override string NetEngineStart => "Start engine";
		public override string NetEngineClose => "Shutting down the engine";
		public override string NetClientAccountTimeout => "Wait for account check timeout：";
		public override string NetClientOnline => "Online";
		public override string NetClientOffline => "Offline";
		public override string NetClientBreak => "Abnormal offline";
		public override string NetClientFull => "The server hosts the upper limit and receives an exceeded request connection.";
		public override string NetClientLoginFailed => "Error in Client logon: ";
		public override string NetHeartCheckFailed => "Heartbeat Validation exception: ";
		public override string NetHeartCheckTimeout => "Heartbeat verification timeout, force offline: ";
		public override string DataSourceFormatError => "Data source format is incorrect";
		public override string ServerFileCheckFailed => "Server confirmed file failed, please re-upload";
		public override string ClientOnlineInfo => "Client [ {0} ] Online";
		public override string ClientOfflineInfo => "Client [ {0} ] Offline";
		public override string ClientDisableLogin => "Client [ {0} ] is not trusted, login forbidden";

		/***********************************************************************************
		 * 
		 *    Client related
		 * 
		 ************************************************************************************/

		public override string ReConnectServerSuccess => "Re-connect server succeeded";
		public override string ReConnectServerAfterTenSeconds => "Reconnect the server after 10 seconds";
		public override string KeyIsNotAllowedNull => "The keyword is not allowed to be empty";
		public override string KeyIsExistAlready => "The current keyword already exists";
		public override string KeyIsNotExist => "The keyword for the current subscription does not exist";
		public override string ConnectingServer => "Connecting to Server...";
		public override string ConnectFailedAndWait => "Connection disconnected, wait {0} seconds to reconnect";
		public override string AttemptConnectServer => "Attempting to connect server {0} times";
		public override string ConnectServerSuccess => "Connection Server succeeded";
		public override string GetClientIpAddressFailed => "Client IP Address acquisition failed";
		public override string ConnectionIsNotAvailable => "The current connection is not available";
		public override string DeviceCurrentIsLoginRepeat => "ID of the current device duplicate login";
		public override string DeviceCurrentIsLoginForbidden => "The ID of the current device prohibits login";
		public override string PasswordCheckFailed => "Password validation failed";
		public override string DataTransformError => "Data conversion failed, source data: ";
		public override string RemoteClosedConnection => "Remote shutdown of connection";
		
		/***********************************************************************************
		 * 
		 *    Log related
		 * 
		 ************************************************************************************/
		public override string LogNetDebug => "Debug";
		public override string LogNetInfo => "Info";
		public override string LogNetWarn => "Warn";
		public override string LogNetError => "Error";
		public override string LogNetFatal => "Fatal";
		public override string LogNetAbandon => "Abandon";
		public override string LogNetAll => "All";


		/***********************************************************************************
		 * 
		 *    Modbus related
		 * 
		 ************************************************************************************/

		public override string ModbusTcpFunctionCodeNotSupport => "Unsupported function code";
		public override string ModbusTcpFunctionCodeOverBound => "Data read out of bounds";
		public override string ModbusTcpFunctionCodeQuantityOver => "Read length exceeds maximum value";
		public override string ModbusTcpFunctionCodeReadWriteException => "Read and Write exceptions";
		public override string ModbusTcpReadCoilException => "Read Coil anomalies";
		public override string ModbusTcpWriteCoilException => "Write Coil exception";
		public override string ModbusTcpReadRegisterException => "Read Register exception";
		public override string ModbusTcpWriteRegisterException => "Write Register exception";
		public override string ModbusAddressMustMoreThanOne => "The address value must be greater than 1 in the case where the start address is 1";
		public override string ModbusAsciiFormatCheckFailed => "Modbus ASCII command check failed, not MODBUS-ASCII message";
		public override string ModbusCRCCheckFailed => "The CRC checksum check failed for Modbus";
		public override string ModbusLRCCheckFailed => "The LRC checksum check failed for Modbus";
		public override string ModbusMatchFailed => "Not the standard Modbus protocol";
		public override string ModbusBitIndexOverstep => "The index of the bit access is out of range, it should be between 0-15";


		/***********************************************************************************
		 * 
		 *    Melsec PLC related
		 * 
		 ************************************************************************************/
		public override string MelsecPleaseReferToManualDocument => "Please check Mitsubishi's communication manual for details of the alarm.";
		public override string MelsecReadBitInfo => "The read bit variable array can only be used for bit soft elements, if you read the word soft component, call the Read method";
		public override string MelsecCurrentTypeNotSupportedWordOperate => "The current type does not support word read and write";
		public override string MelsecCurrentTypeNotSupportedBitOperate => "The current type does not support bit read and write";
		public override string MelsecFxReceiveZero => "The received data length is 0";
		public override string MelsecFxAckNagative => "Invalid data from PLC feedback";
		public override string MelsecFxAckWrong => "PLC Feedback Signal Error: ";
		public override string MelsecFxCrcCheckFailed => "PLC Feedback message and check failed!";

		public override string MelsecError02 => "The specified range of the \"read/write\" (in/out) device is incorrect.";
		public override string MelsecError51 => "When using random access buffer memory for communication, the start address specified by the external device is set outside the range of 0-6143. Solution: Check and correct the specified start address.";
		public override string MelsecError52 => "1. When using random access buffer memory for communication, the start address + data word count specified by the external device (depending on the setting when reading) is outside the range of 0-6143. \r\n2. Data of the specified word count (text) cannot be sent in one frame. (The data length value and the total text of the communication are not within the allowed range.)";
		public override string MelsecError54 => "When \"ASCII Communication\" is selected in [Operation Settings]-[Communication Data Code] via GX Developer, ASCII codes from external devices that cannot be converted to binary codes are received.";
		public override string MelsecError55 => "When [Operation Settings]-[Cannot Write in Run Time] cannot be set by GX Developer (No check mark), if the PLCCPU is in the running state, the external device requests to write data.";
		public override string MelsecError56 => "The device specified from the outside is incorrect.";
		public override string MelsecError58 => @"1. The command start address (start device number and start step number) specified by the external device can be set outside the specified range.
2. The block number specified for the extended file register does not exist.
3. File register (R) cannot be specified.
4. Specify the word device for the bit device command.
5. The start number of the bit device is specified by a certain value. This value is not a multiple of 16 in the word device command.";
		public override string MelsecError59 => "The register of the extension file cannot be specified.";
		public override string MelsecErrorC04D => "In the information received by the Ethernet module through automatic open UDP port communication or out-of-order fixed buffer communication, the data length specified in the application domain is incorrect.";
		public override string MelsecErrorC050 => "When the operation setting of ASCII code communication is performed in the Ethernet module, ASCII code data that cannot be converted into binary code is received.";
		public override string MelsecErrorC051_54 => "The number of read/write points is outside the allowable range.";
		public override string MelsecErrorC055 => "The number of file data read/write points is outside the allowable range.";
		public override string MelsecErrorC056 => "The read/write request exceeded the maximum address.";
		public override string MelsecErrorC057 => "The length of the requested data does not match the data count of the character area (partial text).";
		public override string MelsecErrorC058 => "After the ASCII binary conversion, the length of the requested data does not match the data count of the character area (partial text).";
		public override string MelsecErrorC059 => "The designation of commands and subcommands is incorrect.";
		public override string MelsecErrorC05A_B => "The Ethernet module cannot read and write to the specified device.";
		public override string MelsecErrorC05C => "The requested content is incorrect. (Request to read/write to word device in bits.)";
		public override string MelsecErrorC05D => "Monitoring registration is not performed.";
		public override string MelsecErrorC05E => "The communication time between the Ethernet module and the PLC CPU exceeds the time of the CPU watchdog timer.";
		public override string MelsecErrorC05F => "The request cannot be executed on the target PLC.";
		public override string MelsecErrorC060 => "The requested content is incorrect. (Incorrect data is specified for the bit device, etc.)";
		public override string MelsecErrorC061 => "The length of the requested data does not match the number of data in the character area (partial text).";
		public override string MelsecErrorC062 => "When the online correction is prohibited, the remote protocol I/O station (QnA compatible 3E frame or 4E frame) write operation is performed by the MC protocol.";
		public override string MelsecErrorC070 => "Cannot specify the range of device memory for the target station";
		public override string MelsecErrorC072 => "The requested content is incorrect. (Request to write to word device in bit units.) ";
		public override string MelsecErrorC074 => "The target PLC does not execute the request. The network number and PC number need to be corrected.";

		/***********************************************************************************
		 * 
		 *    Siemens PLC related
		 * 
		 ************************************************************************************/

		public override string SiemensDBAddressNotAllowedLargerThan255 => "DB block data cannot be greater than 255";
		public override string SiemensReadLengthMustBeEvenNumber => "The length of the data read must be an even number";
		public override string SiemensWriteError => "Writes the data exception, the code name is: ";
		public override string SiemensReadLengthCannotLargerThan19 => "The number of arrays read does not allow greater than 19";
		public override string SiemensDataLengthCheckFailed => "Block length checksum failed, please check if Put/get is turned on and DB block optimization is turned off";
		public override string SiemensFWError => "An exception occurred, the specific information to find the Fetch/write protocol document";
		public override string SiemensReadLengthOverPlcAssign => "The range of data read exceeds the setting of the PLC";
		public override string SiemensError000A => "Object does not exist:  Occurs when trying to request a Data Block that does not exist.";
		public override string SiemensError0006 => "The data type of the current operation is not supported";

		/***********************************************************************************
		 * 
		 *    Omron PLC related
		 * 
		 ************************************************************************************/

		public override string OmronAddressMustBeZeroToFifteen => "The bit address entered can only be between 0-15";
		public override string OmronReceiveDataError => "Data Receive exception";
		public override string OmronStatus0 => "Communication is normal.";
		public override string OmronStatus1 => "The message header is not fins";
		public override string OmronStatus2 => "Data length too long";
		public override string OmronStatus3 => "This command does not support";
		public override string OmronStatus20 => "Exceeding connection limit";
		public override string OmronStatus21 => "The specified node is already in the connection";
		public override string OmronStatus22 => "Attempt to connect to a protected network node that is not yet configured in the PLC";
		public override string OmronStatus23 => "The current client's network node exceeds the normal range";
		public override string OmronStatus24 => "The current client's network node is already in use";
		public override string OmronStatus25 => "All network nodes are already in use";



		/***********************************************************************************
		 * 
		 *    AB PLC related
		 * 
		 ************************************************************************************/


		public override string AllenBradley04 => "The IOI could not be deciphered. Either it was not formed correctly or the match tag does not exist."; 
		public override string AllenBradley05 => "The particular item referenced (usually instance) could not be found.";
		public override string AllenBradley06 => "The amount of data requested would not fit into the response buffer. Partial data transfer has occurred.";
		public override string AllenBradley0A => "An error has occurred trying to process one of the attributes.";
		public override string AllenBradley13 => "Not enough command data / parameters were supplied in the command to execute the service requested.";
		public override string AllenBradley1C => "An insufficient number of attributes were provided compared to the attribute count.";
		public override string AllenBradley1E => "A service request in this service went wrong.";
		public override string AllenBradley20 => "The data type of the parameter in the command is inconsistent with the data type of the actual parameter.";
		public override string AllenBradley26 => "The IOI word length did not match the amount of IOI which was processed.";

		public override string AllenBradleySessionStatus00 => "success";
		public override string AllenBradleySessionStatus01 => "The sender issued an invalid or unsupported encapsulation command.";
		public override string AllenBradleySessionStatus02 => "Insufficient memory resources in the receiver to handle the command. This is not an application error. Instead, it only results if the encapsulation layer cannot obtain memory resources that it need.";
		public override string AllenBradleySessionStatus03 => "Poorly formed or incorrect data in the data portion of the encapsulation message.";
		public override string AllenBradleySessionStatus64 => "An originator used an invalid session handle when sending an encapsulation message.";
		public override string AllenBradleySessionStatus65 => "The target received a message of invalid length.";
		public override string AllenBradleySessionStatus69 => "Unsupported encapsulation protocol revision.";

		/***********************************************************************************
		 * 
		 *    Panasonic PLC related
		 * 
		 ************************************************************************************/
		public override string PanasonicReceiveLengthMustLargerThan9 => "The received data length must be greater than 9";
		public override string PanasonicAddressParameterCannotBeNull => "Address parameter is not allowed to be empty";
		public override string PanasonicAddressBitStartMulti16       => "The starting address for bit writing needs to be a multiple of 16, for example: R0.0, R2.0, L3.0, Y4.0";
		public override string PanasonicBoolLengthMulti16            => "The data length written in batch bool needs to be a multiple of 16, otherwise it cannot be written";
		public override string PanasonicMewStatus20                  => "Error unknown";
		public override string PanasonicMewStatus21                  => "Nack error, the remote unit could not be correctly identified, or a data error occurred.";
		public override string PanasonicMewStatus22                  => "WACK Error: The receive buffer for the remote unit is full.";
		public override string PanasonicMewStatus23                  => "Multiple port error: The remote unit number (01 to 16) is set to repeat with the local unit.";
		public override string PanasonicMewStatus24                  => "Transport format error: An attempt was made to send data that does not conform to the transport format, or a frame data overflow or a data error occurred.";
		public override string PanasonicMewStatus25                  => "Hardware error: Transport system hardware stopped operation.";
		public override string PanasonicMewStatus26                  => "Unit Number error: The remote unit's numbering setting exceeds the range of 01 to 63.";
		public override string PanasonicMewStatus27                  => "Error not supported: Receiver data frame overflow. An attempt was made to send data of different frame lengths between different modules.";
		public override string PanasonicMewStatus28                  => "No answer error: The remote unit does not exist. (timeout).";
		public override string PanasonicMewStatus29                  => "Buffer Close error: An attempt was made to send or receive a buffer that is in a closed state.";
		public override string PanasonicMewStatus30                  => "Timeout error: Persisted in transport forbidden State.";
		public override string PanasonicMewStatus40                  => "BCC Error: A transmission error occurred in the instruction data.";
		public override string PanasonicMewStatus41                  => "Malformed: The sent instruction information does not conform to the transmission format.";
		public override string PanasonicMewStatus42                  => "Error not supported: An unsupported instruction was sent. An instruction was sent to a target station that was not supported.";
		public override string PanasonicMewStatus43                  => "Processing Step Error: Additional instructions were sent when the transfer request information was suspended.";
		public override string PanasonicMewStatus50                  => "Link Settings Error: A link number that does not actually exist is set.";
		public override string PanasonicMewStatus51                  => "Simultaneous operation error: When issuing instructions to other units, the transmit buffer for the local unit is full.";
		public override string PanasonicMewStatus52                  => "Transport suppression Error: Unable to transfer to other units.";
		public override string PanasonicMewStatus53                  => "Busy error: Other instructions are being processed when the command is received.";
		public override string PanasonicMewStatus60                  => "Parameter error: Contains code that cannot be used in the directive, or the code does not have a zone specified parameter (X, Y, D), and so on.";
		public override string PanasonicMewStatus61                  => "Data error: Contact number, area number, Data code format (BCD,HEX, etc.) overflow, overflow, and area specified error.";
		public override string PanasonicMewStatus62                  => "Register ERROR: Excessive logging of data in an unregistered state of operations (Monitoring records, tracking records, etc.). )。";
		public override string PanasonicMewStatus63                  => "PLC mode error: When an instruction is issued, the run mode is not able to process the instruction.";
		public override string PanasonicMewStatus65                  => "Protection Error: Performs a write operation to the program area or system register in the storage protection state.";
		public override string PanasonicMewStatus66                  => "Address Error: Address (program address, absolute address, etc.) Data encoding form (BCD, hex, etc.), overflow, underflow, or specified range error.";
		public override string PanasonicMewStatus67                  => "Missing data error: The data to be read does not exist. (reads data that is not written to the comment register.)";

		// MC 协议相关的内容
		public override string PanasonicMc4031 => "Address out of range (starting device + number of writing points)";
		public override string PanasonicMcC051 => "Outside the specified range of equipment points";
		public override string PanasonicMcC056 => "Outside the specified range of the starting device";
		public override string PanasonicMcC059 => "Command search When there is no command consistent with the received data command in the MC protocol command table";
		public override string PanasonicMcC05B => "Outside the specified range of the equipment code";
		public override string PanasonicMcC05C => "When the slave command is a bit unit (0001) and the device code is a word device";
		public override string PanasonicMcC05F => "1. \"Network number\" check \r\n2. \"PC number\" check \r\n3. \"Request target unit IO number\" check \r\n4. The number of received write data is abnormal";
		public override string PanasonicMcC060 => "Write contact data abnormal (other than 0/1)";
		public override string PanasonicMcC061 => "1. The number of received data has not reached the minimum number of bytes received for the start character content check \r\n 2. The number of received data has not reached the minimum number of bytes received";


		/***********************************************************************************
		 * 
		 *   Fatek PLC 永宏PLC相关
		 * 
		 ************************************************************************************/
		public override string FatekStatus02 => "Illegal value";
		public override string FatekStatus03 => "Write disabled";
		public override string FatekStatus04 => "Invalid command code";
		public override string FatekStatus05 => "Cannot be activated (down RUN command but Ladder Checksum does not match)";
		public override string FatekStatus06 => "Cannot be activated (down RUN command but PLC ID ≠ Ladder ID)";
		public override string FatekStatus07 => "Cannot be activated (down RUN command but program syntax error)";
		public override string FatekStatus09 => "Cannot be activated (down RUN command, but the ladder program command PLC cannot be executed)";
		public override string FatekStatus10 => "Illegal address";



		/***********************************************************************************
		 * 
		 *   Fuji PLC 富士PLC相关
		 * 
		 ************************************************************************************/
		public override string FujiSpbStatus01 => "Write to the ROM";
		public override string FujiSpbStatus02 => "Received undefined commands or commands that could not be processed";
		public override string FujiSpbStatus03 => "There is a contradiction in the data part (parameter exception)";
		public override string FujiSpbStatus04 => "Unable to process due to transfer interlocks from other programmers";
		public override string FujiSpbStatus05 => "The module number is incorrect";
		public override string FujiSpbStatus06 => "Search item not found";
		public override string FujiSpbStatus07 => "An address that exceeds the module range (when writing) is specified";
		public override string FujiSpbStatus09 => "Unable to execute due to faulty program (RUN)";
		public override string FujiSpbStatus0C => "Inconsistent password";


		/***********************************************************************************
		 * 
		 *   MQTT相关
		 * 
		 ************************************************************************************/
		public override string MQTTDataTooLong => "The current data length exceeds the limit of the agreement";
		public override string MQTTStatus01 => "unacceptable protocol version";
		public override string MQTTStatus02 => "identifier rejected";
		public override string MQTTStatus03 => "server unavailable";
		public override string MQTTStatus04 => "bad user name or password";
		public override string MQTTStatus05 => "not authorized";


		/***********************************************************************************
		 * 
		 *   SAM相关
		 * 
		 ************************************************************************************/
		public override string SAMReceiveLengthMustLargerThan8 => "Received data length is less than 8, must be greater than 8";
		public override string SAMHeadCheckFailed => "Data frame header check failed for SAM。";
		public override string SAMLengthCheckFailed => "Data length header check failed for SAM。";
		public override string SAMSumCheckFailed => "SAM's data checksum check failed.";
		public override string SAMAddressStartWrong => "SAM string address identification error.";
		public override string SAMStatus90 => "Successful operation";
		public override string SAMStatus91 => "No content in the card";
		public override string SAMStatus9F => "Find card success";
		public override string SAMStatus10 => "Received data checksum error";
		public override string SAMStatus11 => "Received data length error";
		public override string SAMStatus21 => "Receive data command error";
		public override string SAMStatus23 => "Unauthorized operation";
		public override string SAMStatus24 => "Unrecognized error";
		public override string SAMStatus31 => "Card authentication SAM failed";
		public override string SAMStatus32 => "SAM certificate / card failed";
		public override string SAMStatus33 => "Information validation error";
		public override string SAMStatus40 => "Unrecognized card type";
		public override string SAMStatus41 => "ID / card operation failed";
		public override string SAMStatus47 => "Random number failed";
		public override string SAMStatus60 => "SAM Self-test failed";
		public override string SAMStatus66 => "SAM unauthorized";
		public override string SAMStatus80 => "Failed to find card";
		public override string SAMStatus81 => "Select card failed";

		/***********************************************************************************
		 * 
		 *   DLT645 相关
		 * 
		 ************************************************************************************/
		public override string DLTAddressCannotNull => "Address information cannot be empty or have a length of 0";
		public override string DLTAddressCannotMoreThan12 => "Address information length cannot be greater than 12";
		public override string DLTAddressMatchFailed => "Address format failed to match, please check whether it is less than 12 words, and are all addresses composed of 0-9 or A digits";
		public override string DLTErrorInfoBit0 => "Other errors";
		public override string DLTErrorInfoBit1 => "No data requested";
		public override string DLTErrorInfoBit2 => "Incorrect password / unauthorized";
		public override string DLTErrorInfoBit3 => "The communication rate cannot be changed";
		public override string DLTErrorInfoBit4 => "Annual time zone exceeded";
		public override string DLTErrorInfoBit5 => "Day time slot exceeded";
		public override string DLTErrorInfoBit6 => "Rates exceeded";
		public override string DLTErrorInfoBit7 => "Reserve";
		public override string DLTErrorWriteReadCheckFailed => "Verify that the data after writing is consistent with the previous data fails";


		/***********************************************************************************
		 * 
		 *   Keyence 相关
		 * 
		 ************************************************************************************/
		public override string KeyenceSR2000Error00 => "Receive undefined command";
		public override string KeyenceSR2000Error01 => "The command format does not match. (The number of parameters is wrong)";
		public override string KeyenceSR2000Error02 => "Out of parameter 1 setting range";
		public override string KeyenceSR2000Error03 => "Out of parameter 2 setting range";
		public override string KeyenceSR2000Error04 => "Parameter 2 is not set in HEX (hexadecimal) code";
		public override string KeyenceSR2000Error05 => "Parameter 2 belongs to HEX (hexadecimal) code, but it exceeds the setting range";
		public override string KeyenceSR2000Error10 => "There are more than two preset data! The preset data is wrong";
		public override string KeyenceSR2000Error11 => "The area specified data is incorrect";
		public override string KeyenceSR2000Error12 => "The specified file does not exist";
		public override string KeyenceSR2000Error13 => "Exceeds the setting range of %Tmm-LON, bb command mm";
		public override string KeyenceSR2000Error14 => "Cannot confirm communication with %Tmm-KEYENCE command";
		public override string KeyenceSR2000Error20 => "This command is not allowed to be executed in the current mode (execution error)";
		public override string KeyenceSR2000Error21 => "The buffer is full and the command cannot be executed";
		public override string KeyenceSR2000Error22 => "An error occurred while loading or saving parameters";
		public override string KeyenceSR2000Error23 => "Since AutoID Netwoerk Navigator is being connected, the command sent by RS-232C cannot be received";
		public override string KeyenceSR2000Error99 => "If you think the SR-2000 series is abnormal, please contact KEYENCE";
		public override string KeyenceNanoE0 => $"1. The specified device number, bank number, unit number, and address are out of range. {Environment.NewLine} 2. Specify the numbers of timers, counters, CTH and CTC that are not used by the program. {Environment.NewLine} 3. The monitor is not logged in, but the monitor needs to be read.";
		public override string KeyenceNanoE1 => $"1. A command not supported by the CPU Unit was sent. {Environment.NewLine} 2. The method of the specified instruction is wrong. {Environment.NewLine} 3. Before communication was established, a command other than CR was sent.";
		public override string KeyenceNanoE2 => $"1. The \"M1 (switch to RUN mode)\" command was sent when the CPU unit did not store a program. {Environment.NewLine} 2. When the RUN/PROG switch of the CPU unit is in the PROG state, the \"M1 (switch to RUN mode)\" command is sent.";
		public override string KeyenceNanoE4 => $"Want to change the set values of timers, counters, and CTCs written in the disable program.";
		public override string KeyenceNanoE5 => $"When the CPU unit error has not been eliminated, the \"M1 (switch to RUN mode)\" command was sent.";
		public override string KeyenceNanoE6 => $"Read from the device selected by the \"RDC\" instruction.";


		/***********************************************************************************
		 * 
		 *   横河Yokogawa 相关
		 * 
		 ************************************************************************************/
		public override string YokogawaLinkError01 => " The CPU number is outside the range of 1 to 4";
		public override string YokogawaLinkError02 => "The command does not exist or the command is not executable.";
		public override string YokogawaLinkError03 => "The device name does not exist or A relay device is incorrectly specified for read/write access in word units.";
		public override string YokogawaLinkError04 => "Value outside the setting range: 1. Characters other than 0 and 1 are used for bit setting. 2. Word setting is out of the valid range of 0000 to FFFF. 3. The specified starting position in a command, such as Load/Save, is out of the valid address range.";
		public override string YokogawaLinkError05 => "Data count out of range: 1. The specified bit count, word count, etc. exceeded the specifications range. 2. The specified data count and the device parameter count, etc. do not match.";
		public override string YokogawaLinkError06 => "Attempted to execute monitoring without having specified a monitor command( BRS, WRS)";
		public override string YokogawaLinkError07 => "Not a BASIC CPU";
		public override string YokogawaLinkError08 => "A parameter is invalid for a reason other than those given above.";
		public override string YokogawaLinkError41 => "An error has occurred during communication";
		public override string YokogawaLinkError42 => "Value of checksum differs. (Bit omitted or changed characters)";
		public override string YokogawaLinkError43 => "The amount of data received exceeded stipulated value.";
		public override string YokogawaLinkError44 => "Timeout while receiving characters: 1. No End character or ETX was received. 2. Timeout duration is 5 seconds";
		public override string YokogawaLinkError51 => "Timeout error: 1. No end-of-process response is returned from the CPU for reasons such as CPU power failure.(timeout) 2. Sequence CPU hardware failure. 3. Sequence CPU is not accepting commands. 4. Insufficient sequence CPU service time";
		public override string YokogawaLinkError52 => "The CPU has detected an error during processing. ";
		public override string YokogawaLinkErrorF1 => "Internal error: 1. A Cancel (PLC) command was issued during execution of a command other than a Load( PLD) or Save( PSV) command. 2. An internal error was detected.";


		/***********************************************************************************
		 * 
		 *   GE 相关
		 * 
		 ************************************************************************************/
		public override string GeSRTPNotSupportBitReadWrite => "The current address data does not support read and write operations in bit units";
		public override string GeSRTPAddressCannotBeZero => "The starting address of the current address cannot be 0, it needs to start from 1";
		public override string GeSRTPNotSupportByteReadWrite => "The current address data does not support read and write operations in byte units, and can only be read and written in word units";
		public override string GeSRTPWriteLengthMustBeEven => "The length of the data written to the current address must be an even number";


		/***********************************************************************************
		 * 
		 *   Yamatake 山武
		 * 
		 ************************************************************************************/
		public override string YamatakeDigitronCPL40 => "Command format error";
		public override string YamatakeDigitronCPL41 => "The number of data exceeds 16 (including the number of data for RS commands)";
		public override string YamatakeDigitronCPL42 => "The address is out of range, all messages are discarded";
		public override string YamatakeDigitronCPL43 => "The value in the data section is abnormal, and all messages are discarded";
		public override string YamatakeDigitronCPL44 => "The value of the data part exceeds the range, and continue processing except for the current address";
		public override string YamatakeDigitronCPL45 => "Can not write according to the machine status, write to prohibited addresses";
		public override string YamatakeDigitronCPL46 => "CPL communication writing permission/prohibition (function setting item C27) is writing prohibition. When writing via communication, please set it as writing permission";
		public override string YamatakeDigitronCPL47 => "Cannot switch the mode (other items with high priority are valid and cannot be changed through communication)";
		public override string YamatakeDigitronCPL48 => "In the programming operation of the programmer, please send the command again after the programmer has finished writing and the machine returns to the basic display screen";
		public override string YamatakeDigitronCPL99 => "Undecided order";


		/***********************************************************************************
		 * 
		 *   Lsis
		 * 
		 ************************************************************************************/
		public override string LsisCnet0003 => "Number of blocks exceeds 16 at Individual Read/Write Request";
		public override string LsisCnet0004 => "Variable Length exceeds the max. size of 16";
		public override string LsisCnet0007 => "Other data type than X,B,W,D,L received";
		public override string LsisCnet0011 => "1.Data length area information incorrect\r\n2.n case % is unavailable to start with\r\n" +
			"3.Variable’s area value wrong\r\n4.Other value is written for Bit Write than 00 or 01";
		public override string LsisCnet0090 => "Unregistered monitor execution requested";
		public override string LsisCnet0190 => "Reg. No. range exceeded";
		public override string LsisCnet0290 => "Reg. No. range exceeded";
		public override string LsisCnet1132 => "Other letter than applicable device is input";
		public override string LsisCnet1232 => "Request exceeds the max range of 60 Words to read or write at a time";
		public override string LsisCnet1234 => "Unnecessary details exist as added.";
		public override string LsisCnet1332 => "All the blocks shall be requested of the identical data type in the case of Individual Read/Write";
		public override string LsisCnet1432 => "Data value unavailable to convert to Hex";
		public override string LsisCnet7132 => "Request exceeds the area each device supports.";

#pragma warning restore CS1591 // 缺少对公共可见类型或成员的 XML 注释

	}
}
