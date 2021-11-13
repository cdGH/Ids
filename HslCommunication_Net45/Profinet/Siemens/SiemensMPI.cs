using HslCommunication.Core;
using HslCommunication.Core.Address;
using HslCommunication.Serial;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HslCommunication.BasicFramework;

namespace HslCommunication.Profinet.Siemens
{
	/// <summary>
	/// 西门子的MPI协议信息，注意：未测试通过，无法使用<br />
	/// Siemens MPI protocol information, note: it has not passed the test and cannot be used
	/// </summary>
	public class SiemensMPI : SerialDeviceBase
	{
		#region Constructor

		/// <summary>
		/// 实例化一个西门子的MPI协议对象<br />
		/// Instantiate a Siemens MPI protocol object
		/// </summary>
		public SiemensMPI( )
		{
			ByteTransform = new ReverseBytesTransform( );
			WordLength    = 2;
		}

		#endregion

		#region Public Properties

		/// <summary>
		/// 西门子PLC的站号信息<br />
		/// Siemens PLC station number information
		/// </summary>
		public byte Station
		{
			get => station;
			set
			{
				station = value;
				readConfirm[4] = (byte)(value + 0x80);
				writeConfirm[4] = (byte)(value + 0x80);

				int count1 = 0;
				int count2 = 0;
				for (int i = 4; i < 12; i++)
				{
					count1 += readConfirm[i];
					count2 += writeConfirm[i];
				}
				readConfirm[12] = (byte)count1;
				writeConfirm[12] = (byte)count2;
			}
		}

		#endregion

		#region Handle Plc

		/// <summary>
		/// 与PLC进行握手<br />
		/// Handshake with PLC
		/// </summary>
		/// <returns>是否握手成功</returns>
		public OperateResult Handle( )
		{
			while (true)
			{
				// 令牌帧接收
				OperateResult<byte[]> receiveResult = SPReceived( sP_ReadData, true );
				if (!receiveResult.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( receiveResult );

				if (receiveResult.Content[0] == 0xDC && receiveResult.Content[1] == 0x02 && receiveResult.Content[2] == 0x02)
				{
					OperateResult sendResult = SPSend( sP_ReadData, new byte[] { 0xDC, 0x00, 0x00 } );
					if (!sendResult.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( sendResult );
				}
				else if (receiveResult.Content[0] == 0xDC && receiveResult.Content[1] == 0x00 && receiveResult.Content[2] == 0x02)
				{
					OperateResult sendResult = SPSend( sP_ReadData, new byte[] { 0xDC, 0x02, 0x00 } );
					if (!sendResult.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( sendResult );

					return OperateResult.CreateSuccessResult( );
				}
			}
		}

		#endregion

		#region Read Write Override

		/// <summary>
		/// 从西门子的PLC中读取数据信息，地址为"M100","AI100","I0","Q0","V100","S100"等，详细请参照API文档<br />
		/// Read data information from Siemens PLC, the address is "M100", "AI100", "I0", "Q0", "V100", "S100", etc., please refer to the API documentation
		/// </summary>
		/// <param name="address">西门子的地址数据信息</param>
		/// <param name="length">数据长度</param>
		/// <returns>带返回结果的结果对象</returns>
		public override OperateResult<byte[]> Read( string address, ushort length )
		{
			// 解析指令
			OperateResult<byte[]> command = BuildReadCommand( station, address, length, false );
			if (!command.IsSuccess) return command;

			if (IsClearCacheBeforeRead) ClearSerialCache( );

			// 第一次发送
			OperateResult sendResult = SPSend( sP_ReadData, command.Content );
			if (!sendResult.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( sendResult );

			// 第一次接收
			OperateResult<byte[]> receiveResult = SPReceived( sP_ReadData, true );
			if (!receiveResult.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( receiveResult );
			if (receiveResult.Content[14] != 0xE5) return new OperateResult<byte[]>( "PLC Receive Check Failed:" + SoftBasic.ByteToHexString( receiveResult.Content ) );

			// 第二次接收
			receiveResult = SPReceived( sP_ReadData, true );
			if (!receiveResult.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( receiveResult );
			if (receiveResult.Content[19] != 0x00) return new OperateResult<byte[]>( "PLC Receive Check Failed:" + receiveResult.Content[19] );

			// 确认信息
			sendResult = SPSend( sP_ReadData, readConfirm );
			if (!sendResult.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( sendResult );

			// 数据提取
			byte[] buffer = new byte[length];
			if (receiveResult.Content[25] == 0xFF && receiveResult.Content[26] == 0x04)
			{
				Array.Copy( receiveResult.Content, 29, buffer, 0, length );
			}
			return OperateResult.CreateSuccessResult( buffer );
		}

		/// <summary>
		/// 从西门子的PLC中读取bool数据信息，地址为"M100.0","AI100.1","I0.3","Q0.6","V100.4","S100"等，详细请参照API文档<br />
		/// Read the bool data information from Siemens PLC. The addresses are "M100.0", "AI100.1", "I0.3", "Q0.6", "V100.4", "S100", etc. For details, please Refer to API documentation
		/// </summary>
		/// <param name="address">西门子的地址数据信息</param>
		/// <param name="length">数据长度</param>
		/// <returns>带返回结果的结果对象</returns>
		public override OperateResult<bool[]> ReadBool( string address, ushort length )
		{
			// 解析指令
			OperateResult<byte[]> command = BuildReadCommand( station, address, length, true );
			if (!command.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( command );

			// 第一次发送
			OperateResult sendResult = SPSend( sP_ReadData, command.Content );
			if (!sendResult.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( sendResult );

			// 第一次接收
			OperateResult<byte[]> receiveResult = SPReceived( sP_ReadData, true );
			if (!receiveResult.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( receiveResult );
			if (receiveResult.Content[14] != 0xE5) return new OperateResult<bool[]>( "PLC Receive Check Failed:" + SoftBasic.ByteToHexString( receiveResult.Content ) );

			// 第二次接收
			receiveResult = SPReceived( sP_ReadData, true );
			if (!receiveResult.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( receiveResult );
			if (receiveResult.Content[19] != 0x00) return new OperateResult<bool[]>( "PLC Receive Check Failed:" + receiveResult.Content[19] );

			// 确认信息
			sendResult = SPSend( sP_ReadData, readConfirm );
			if (!sendResult.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( sendResult );

			// 数据提取
			byte[] buffer = new byte[receiveResult.Content.Length - 31];
			if (receiveResult.Content[21] == 0xFF && receiveResult.Content[22] == 0x03)
			{
				Array.Copy( receiveResult.Content, 28, buffer, 0, buffer.Length );
			}

			return OperateResult.CreateSuccessResult( SoftBasic.ByteToBoolArray( buffer, length ) );
		}

		/// <summary>
		/// 将字节数据写入到西门子PLC中，地址为"M100.0","AI100.1","I0.3","Q0.6","V100.4","S100"等，详细请参照API文档<br />
		/// Write byte data to Siemens PLC, the address is "M100.0", "AI100.1", "I0.3", "Q0.6", "V100.4", "S100", etc. Refer to API documentation
		/// </summary>
		/// <param name="address">西门子的地址数据信息</param>
		/// <param name="value">数据长度</param>
		/// <returns>带返回结果的结果对象</returns>
		public override OperateResult Write( string address, byte[] value )
		{
			// 解析指令
			OperateResult<byte[]> command = BuildWriteCommand( station, address, value );
			if (!command.IsSuccess) return command;

			if (IsClearCacheBeforeRead) ClearSerialCache( );

			// 第一次发送
			OperateResult sendResult = SPSend( sP_ReadData, command.Content );
			if (!sendResult.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( sendResult );

			// 第一次接收
			OperateResult<byte[]> receiveResult = SPReceived( sP_ReadData, true );
			if (!receiveResult.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( receiveResult );
			if (receiveResult.Content[14] != 0xE5) return new OperateResult<byte[]>( "PLC Receive Check Failed:" + SoftBasic.ByteToHexString( receiveResult.Content ) );

			// 第二次接收
			receiveResult = SPReceived( sP_ReadData, true );
			if (!receiveResult.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( receiveResult );
			if (receiveResult.Content[25] != 0xFF) return new OperateResult<byte[]>( "PLC Receive Check Failed:" + receiveResult.Content[25] );

			// 确认信息
			sendResult = SPSend( sP_ReadData, writeConfirm );
			if (!sendResult.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( sendResult );

			// 数据提取
			return OperateResult.CreateSuccessResult( );
		}

		#endregion

		#region Byte Read Write

		/// <summary>
		/// 从西门子的PLC中读取byte数据信息，地址为"M100.0","AI100.1","I0.3","Q0.6","V100.4","S100"等，详细请参照API文档<br />
		/// Read byte data information from Siemens PLC. The addresses are "M100.0", "AI100.1", "I0.3", "Q0.6", "V100.4", "S100", etc. For details, please Refer to API documentation
		/// </summary>
		/// <param name="address">西门子的地址数据信息</param>
		/// <returns>带返回结果的结果对象</returns>
		public OperateResult<byte> ReadByte( string address ) => ByteTransformHelper.GetResultFromArray( Read( address, 1 ) );

		/// <summary>
		/// 将byte数据写入到西门子PLC中，地址为"M100.0","AI100.1","I0.3","Q0.6","V100.4","S100"等，详细请参照API文档<br />
		/// Write byte data to Siemens PLC, the address is "M100.0", "AI100.1", "I0.3", "Q0.6", "V100.4", "S100", etc. API documentation
		/// </summary>
		/// <param name="address">西门子的地址数据信息</param>
		/// <param name="value">数据长度</param>
		/// <returns>带返回结果的结果对象</returns>
		public OperateResult Write( string address, byte value ) => Write( address, new byte[] { value } );

		#endregion

		#region Private Member

		private byte station = 0x02;            // PLC的站号信息
		private byte[] readConfirm = new byte[] { 0x68, 0x08, 0x08, 0x68, 0x82, 0x80, 0x5C, 0x16, 0x02, 0xB0, 0x07, 0x00, 0x2D, 0x16, 0xE5 };
		private byte[] writeConfirm = new byte[] { 0x68, 0x08, 0x08, 0x68, 0x82, 0x80, 0x7C, 0x16, 0x02, 0xB0, 0x07, 0x00, 0x4D, 0x16, 0xE5 };

		#endregion

		#region Object Override

		/// <inheritdoc/>
		public override string ToString( ) => $"SiemensMPI[{PortName}:{BaudRate}]";

		#endregion

		#region Static Method

		/// <summary>
		/// 生成一个读取字数据指令头的通用方法<br />
		/// A general method for generating a command header to read a Word data
		/// </summary>
		/// <param name="station">设备的站号信息 -> Station number information for the device</param>
		/// <param name="address">起始地址，例如M100，I0，Q0，V100 ->
		/// Start address, such as M100,I0,Q0,V100</param>
		/// <param name="length">读取数据长度 -> Read Data length</param>
		/// <param name="isBit">是否为位读取</param>
		/// <returns>包含结果对象的报文 -> Message containing the result object</returns>
		public static OperateResult<byte[]> BuildReadCommand( byte station, string address, ushort length, bool isBit )
		{
			OperateResult<S7AddressData> analysis = S7AddressData.ParseFrom( address, length );
			if (!analysis.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( analysis );

			byte[] _PLCCommand = new byte[38];
			_PLCCommand[0] = 0x68;
			_PLCCommand[1] = BitConverter.GetBytes( _PLCCommand.Length - 6 )[0];
			_PLCCommand[2] = BitConverter.GetBytes( _PLCCommand.Length - 6 )[0];
			_PLCCommand[3] = 0x68;
			_PLCCommand[4] = (byte)(station + 0x80);
			_PLCCommand[5] = (byte)(0x00 + 0x80);
			_PLCCommand[6] = 0x7C;
			_PLCCommand[7] = 0x16;
			_PLCCommand[8] = 0x01;
			_PLCCommand[9] = 0xF1;
			_PLCCommand[10] = 0x00;
			_PLCCommand[11] = 0x32;
			_PLCCommand[12] = 0x01;
			_PLCCommand[13] = 0x00;
			_PLCCommand[14] = 0x00;
			_PLCCommand[15] = 0x33;
			_PLCCommand[16] = 0x02;
			_PLCCommand[17] = 0x00;
			_PLCCommand[18] = 0x0E;
			_PLCCommand[19] = 0x00;
			_PLCCommand[20] = 0x00;
			_PLCCommand[21] = 0x04;
			_PLCCommand[22] = 0x01;
			_PLCCommand[23] = 0x12;
			_PLCCommand[24] = 0x0A;
			_PLCCommand[25] = 0x10;

			_PLCCommand[26] = isBit ? (byte)0x01 : (byte)0x02;
			_PLCCommand[27] = BitConverter.GetBytes( length )[1];
			_PLCCommand[28] = BitConverter.GetBytes( length )[0];
			_PLCCommand[29] = BitConverter.GetBytes( analysis.Content.DbBlock )[1];
			_PLCCommand[30] = BitConverter.GetBytes( analysis.Content.DbBlock )[0];
			_PLCCommand[31] = analysis.Content.DataCode;
			_PLCCommand[32] = BitConverter.GetBytes( analysis.Content.AddressStart )[2];
			_PLCCommand[33] = BitConverter.GetBytes( analysis.Content.AddressStart )[1];
			_PLCCommand[34] = BitConverter.GetBytes( analysis.Content.AddressStart )[0];

			int count = 0;
			for (int i = 4; i < 35; i++)
			{
				count += _PLCCommand[i];
			}
			_PLCCommand[35] = BitConverter.GetBytes( count )[0];
			_PLCCommand[36] = 0x16;
			_PLCCommand[37] = 0xE5;

			return OperateResult.CreateSuccessResult( _PLCCommand );
		}

		/// <summary>
		/// 生成一个写入PLC数据信息的报文内容<br />
		/// Generate a message content to write PLC data information
		/// </summary>
		/// <param name="station">PLC的站号</param>
		/// <param name="address">地址</param>
		/// <param name="values">数据值</param>
		/// <returns>是否写入成功</returns>
		public static OperateResult<byte[]> BuildWriteCommand( byte station, string address, byte[] values )
		{
			OperateResult<S7AddressData> analysis = S7AddressData.ParseFrom( address );
			if (!analysis.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( analysis );

			int length = values.Length;
			// 68 21 21 68 02 00 6C 32 01 00 00 00 00 00 0E 00 00 04 01 12 0A 10
			byte[] _PLCCommand = new byte[42 + values.Length];
			_PLCCommand[0] = 0x68;
			_PLCCommand[1] = BitConverter.GetBytes( _PLCCommand.Length - 6 )[0];
			_PLCCommand[2] = BitConverter.GetBytes( _PLCCommand.Length - 6 )[0];
			_PLCCommand[3] = 0x68;
			_PLCCommand[4] = (byte)(station + 0x80);
			_PLCCommand[5] = (byte)(0x00 + 0x80);
			_PLCCommand[6] = 0x5C;
			_PLCCommand[7] = 0x16;
			_PLCCommand[8] = 0x02;
			_PLCCommand[9] = 0xF1;
			_PLCCommand[10] = 0x00;
			_PLCCommand[11] = 0x32;
			_PLCCommand[12] = 0x01;
			_PLCCommand[13] = 0x00;
			_PLCCommand[14] = 0x00;
			_PLCCommand[15] = 0x43;
			_PLCCommand[16] = 0x02;
			_PLCCommand[17] = 0x00;
			_PLCCommand[18] = 0x0E;
			_PLCCommand[19] = 0x00;
			_PLCCommand[20] = (byte)(values.Length + 4);
			_PLCCommand[21] = 0x05;
			_PLCCommand[22] = 0x01;
			_PLCCommand[23] = 0x12;
			_PLCCommand[24] = 0x0A;
			_PLCCommand[25] = 0x10;

			_PLCCommand[26] = 0x02;
			_PLCCommand[27] = BitConverter.GetBytes( length )[0];
			_PLCCommand[28] = BitConverter.GetBytes( length )[1];
			_PLCCommand[29] = BitConverter.GetBytes( analysis.Content.DbBlock )[0];
			_PLCCommand[30] = BitConverter.GetBytes( analysis.Content.DbBlock )[1];
			_PLCCommand[31] = analysis.Content.DataCode;
			_PLCCommand[32] = BitConverter.GetBytes( analysis.Content.AddressStart )[2];
			_PLCCommand[33] = BitConverter.GetBytes( analysis.Content.AddressStart )[1];
			_PLCCommand[34] = BitConverter.GetBytes( analysis.Content.AddressStart )[0];

			_PLCCommand[35] = 0x00;
			_PLCCommand[36] = 0x04;
			_PLCCommand[37] = BitConverter.GetBytes( length * 8 )[1];
			_PLCCommand[38] = BitConverter.GetBytes( length * 8 )[0];

			values.CopyTo( _PLCCommand, 39 );

			int count = 0;
			for (int i = 4; i < _PLCCommand.Length - 3; i++)
			{
				count += _PLCCommand[i];
			}
			_PLCCommand[_PLCCommand.Length - 3] = BitConverter.GetBytes( count )[0];
			_PLCCommand[_PLCCommand.Length - 2] = 0x16;
			_PLCCommand[_PLCCommand.Length - 1] = 0xE5;


			return OperateResult.CreateSuccessResult( _PLCCommand );
		}

		/// <summary>
		/// 根据错误信息，获取到文本信息
		/// </summary>
		/// <param name="code">状态</param>
		/// <returns>消息文本</returns>
		public static string GetMsgFromStatus( byte code )
		{
			switch (code)
			{
				case 0xFF: return "No error";
				case 0x01: return "Hardware fault";
				case 0x03: return "Illegal object access";
				case 0x05: return "Invalid address(incorrent variable address)";
				case 0x06: return "Data type is not supported";
				case 0x0A: return "Object does not exist or length error";
				default: return StringResources.Language.UnknownError;
			}
		}

		/// <summary>
		/// 根据错误信息，获取到文本信息
		/// </summary>
		/// <param name="errorClass">错误类型</param>
		/// <param name="errorCode">错误代码</param>
		/// <returns>错误信息</returns>
		public static string GetMsgFromStatus( byte errorClass, byte errorCode )
		{
			if (errorClass == 0x80 && errorCode == 0x01)
			{
				return "Switch in wrong position for requested operation";
			}
			else if (errorClass == 0x81 && errorCode == 0x04)
			{
				return "Miscellaneous structure error in command.  Command is not supportedby CPU";
			}
			else if (errorClass == 0x84 && errorCode == 0x04)
			{
				return "CPU is busy processing an upload or download CPU cannot process command because of system fault condition";
			}
			else if (errorClass == 0x85 && errorCode == 0x00)
			{
				return "Length fields are not correct or do not agree with the amount of data received";
			}
			else if (errorClass == 0xD2)
			{
				return "Error in upload or download command";
			}
			else if (errorClass == 0xD6)
			{
				return "Protection error(password)";
			}
			else if (errorClass == 0xDC && errorCode == 0x01)
			{
				return "Error in time-of-day clock data";
			}
			else
			{
				return StringResources.Language.UnknownError;
			}
		}

		#endregion
	}
}
