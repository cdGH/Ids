using HslCommunication.BasicFramework;
using HslCommunication.Serial;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

#if !NET20 && !NET35
using System.Threading.Tasks;
#endif

namespace HslCommunication.ModBus
{
	/// <summary>
	/// Modbus协议相关辅助类
	/// </summary>
	internal class ModbusHelper
	{
		public static OperateResult<byte[]> ExtraRtuResponseContent( byte[] send, byte[] response )
		{
			// 长度校验
			if (response.Length < 5) return new OperateResult<byte[]>( StringResources.Language.ReceiveDataLengthTooShort + "5" );

			// 检查crc
			if (!SoftCRC16.CheckCRC16( response )) return new OperateResult<byte[]>( StringResources.Language.ModbusCRCCheckFailed +
				SoftBasic.ByteToHexString( response, ' ' ) );

			// 发生了错误
			if ((send[1] + 0x80) == response[1]) return new OperateResult<byte[]>( response[2], ModbusInfo.GetDescriptionByErrorCode( response[2] ) );

			if (send[1] != response[1]) return new OperateResult<byte[]>( response[1], $"Receive Command Check Failed: " );

			// 移除CRC校验，返回真实数据
			return ModbusInfo.ExtractActualData( ModbusInfo.ExplodeRtuCommandToCore( response ) );
		}


		/// <inheritdoc cref="ModbusTcpNet.Read(string, ushort)"/>
		public static OperateResult<byte[]> Read( IModbus modbus, string address, ushort length )
		{
			OperateResult<string> modbusAddress = modbus.TranslateToModbusAddress( address, ModbusInfo.ReadRegister );
			if (!modbusAddress.IsSuccess) return modbusAddress.ConvertFailed<byte[]>( );

			OperateResult<byte[][]> command = ModbusInfo.BuildReadModbusCommand( modbusAddress.Content, length, modbus.Station, modbus.AddressStartWithZero, ModbusInfo.ReadRegister );
			if (!command.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( command );

			List<byte> resultArray = new List<byte>( );
			for (int i = 0; i < command.Content.Length; i++)
			{
				OperateResult<byte[]> read = modbus.ReadFromCoreServer( command.Content[i] );
				if (!read.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( read );

				resultArray.AddRange( read.Content );
			}

			return OperateResult.CreateSuccessResult( resultArray.ToArray( ) );
		}

#if !NET20 && !NET35
		/// <inheritdoc cref="Read(IModbus, string, ushort)"/>
		public static async Task<OperateResult<byte[]>> ReadAsync( IModbus modbus, string address, ushort length )
		{
			OperateResult<string> modbusAddress = modbus.TranslateToModbusAddress( address, ModbusInfo.ReadRegister );
			if (!modbusAddress.IsSuccess) return modbusAddress.ConvertFailed<byte[]>( );

			OperateResult<byte[][]> command = ModbusInfo.BuildReadModbusCommand( modbusAddress.Content, length, modbus.Station, modbus.AddressStartWithZero, ModbusInfo.ReadRegister );
			if (!command.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( command );

			List<byte> resultArray = new List<byte>( );
			for (int i = 0; i < command.Content.Length; i++)
			{
				OperateResult<byte[]> read = await modbus.ReadFromCoreServerAsync( command.Content[i] );
				if (!read.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( read );

				resultArray.AddRange( read.Content );
			}

			return OperateResult.CreateSuccessResult( resultArray.ToArray( ) );
		}
#endif
		/// <inheritdoc cref="ModbusTcpNet.Write(string, byte[])"/>
		public static OperateResult Write( IModbus modbus, string address, byte[] value )
		{
			OperateResult<string> modbusAddress = modbus.TranslateToModbusAddress( address, ModbusInfo.WriteRegister );
			if (!modbusAddress.IsSuccess) return modbusAddress;

			OperateResult<byte[]> command = ModbusInfo.BuildWriteWordModbusCommand( modbusAddress.Content, value, modbus.Station, modbus.AddressStartWithZero, ModbusInfo.WriteRegister );
			if (!command.IsSuccess) return command;

			return modbus.ReadFromCoreServer( command.Content );
		}
#if !NET20 && !NET35
		/// <inheritdoc cref="Write(IModbus, string, byte[])"/>
		public static async Task<OperateResult> WriteAsync( IModbus modbus, string address, byte[] value )
		{
			OperateResult<string> modbusAddress = modbus.TranslateToModbusAddress( address, ModbusInfo.WriteRegister );
			if (!modbusAddress.IsSuccess) return modbusAddress;

			OperateResult<byte[]> command = ModbusInfo.BuildWriteWordModbusCommand( modbusAddress.Content, value, modbus.Station, modbus.AddressStartWithZero, ModbusInfo.WriteRegister );
			if (!command.IsSuccess) return command;

			return await modbus.ReadFromCoreServerAsync( command.Content );
		}
#endif
		/// <inheritdoc cref="ModbusTcpNet.Write(string, short)"/>
		public static OperateResult Write( IModbus modbus, string address, short value )
		{
			OperateResult<string> modbusAddress = modbus.TranslateToModbusAddress( address, ModbusInfo.WriteOneRegister );
			if (!modbusAddress.IsSuccess) return modbusAddress;

			OperateResult<byte[]> command = ModbusInfo.BuildWriteWordModbusCommand( modbusAddress.Content, value, modbus.Station, modbus.AddressStartWithZero, ModbusInfo.WriteOneRegister );
			if (!command.IsSuccess) return command;

			return modbus.ReadFromCoreServer( command.Content );
		}

#if !NET20 && !NET35
		/// <inheritdoc cref="Write(IModbus, string, short)"/>
		public static async Task<OperateResult> WriteAsync( IModbus modbus, string address, short value )
		{
			OperateResult<string> modbusAddress = modbus.TranslateToModbusAddress( address, ModbusInfo.WriteOneRegister );
			if (!modbusAddress.IsSuccess) return modbusAddress;

			OperateResult<byte[]> command = ModbusInfo.BuildWriteWordModbusCommand( modbusAddress.Content, value, modbus.Station, modbus.AddressStartWithZero, ModbusInfo.WriteOneRegister );
			if (!command.IsSuccess) return command;

			return await modbus.ReadFromCoreServerAsync( command.Content );
		}
#endif
		/// <inheritdoc cref="ModbusTcpNet.Write(string, ushort)"/>
		public static OperateResult Write( IModbus modbus, string address, ushort value )
		{
			OperateResult<string> modbusAddress = modbus.TranslateToModbusAddress( address, ModbusInfo.WriteOneRegister );
			if (!modbusAddress.IsSuccess) return modbusAddress;

			OperateResult<byte[]> command = ModbusInfo.BuildWriteWordModbusCommand( modbusAddress.Content, value, modbus.Station, modbus.AddressStartWithZero, ModbusInfo.WriteOneRegister );
			if (!command.IsSuccess) return command;

			return modbus.ReadFromCoreServer( command.Content );
		}

#if !NET20 && !NET35
		/// <inheritdoc cref="Write(IModbus, string, ushort)"/>
		public static async Task<OperateResult> WriteAsync( IModbus modbus, string address, ushort value )
		{
			OperateResult<string> modbusAddress = modbus.TranslateToModbusAddress( address, ModbusInfo.WriteOneRegister );
			if (!modbusAddress.IsSuccess) return modbusAddress;

			OperateResult<byte[]> command = ModbusInfo.BuildWriteWordModbusCommand( modbusAddress.Content, value, modbus.Station, modbus.AddressStartWithZero, ModbusInfo.WriteOneRegister );
			if (!command.IsSuccess) return command;

			return await modbus.ReadFromCoreServerAsync( command.Content );
		}
#endif
		/// <inheritdoc cref="ModbusTcpNet.WriteMask(string, ushort, ushort)"/>
		public static OperateResult WriteMask( IModbus modbus, string address, ushort andMask, ushort orMask )
		{
			OperateResult<string> modbusAddress = modbus.TranslateToModbusAddress( address, ModbusInfo.WriteMaskRegister );
			if (!modbusAddress.IsSuccess) return modbusAddress;

			OperateResult<byte[]> command = ModbusInfo.BuildWriteMaskModbusCommand( modbusAddress.Content, andMask, orMask, modbus.Station, modbus.AddressStartWithZero, ModbusInfo.WriteMaskRegister );
			if (!command.IsSuccess) return command;

			return modbus.ReadFromCoreServer( command.Content );
		}

#if !NET20 && !NET35
		/// <inheritdoc cref="WriteMask(IModbus, string, ushort, ushort)"/>
		public static async Task<OperateResult> WriteMaskAsync( IModbus modbus, string address, ushort andMask, ushort orMask )
		{
			OperateResult<string> modbusAddress = modbus.TranslateToModbusAddress( address, ModbusInfo.WriteMaskRegister );
			if (!modbusAddress.IsSuccess) return modbusAddress;

			OperateResult<byte[]> command = ModbusInfo.BuildWriteMaskModbusCommand( modbusAddress.Content, andMask, orMask, modbus.Station, modbus.AddressStartWithZero, ModbusInfo.WriteMaskRegister );
			if (!command.IsSuccess) return command;

			return await modbus.ReadFromCoreServerAsync( command.Content );
		}
#endif
		public static OperateResult<bool[]> ReadBoolHelper( IModbus modbus, string address, ushort length, byte function )
		{
			if (address.IndexOf( '.' ) > 0)
			{
				string[] addressSplits = address.SplitDot( );
				int bitIndex = 0;
				try
				{
					bitIndex = Convert.ToInt32( addressSplits[1] );
				}
				catch(Exception ex)
				{
					return new OperateResult<bool[]>( "Bit Index format wrong, " + ex.Message );
				}
				ushort len = (ushort)((length + bitIndex + 15) / 16);

				OperateResult<byte[]> read = modbus.Read( addressSplits[0], len );
				if (!read.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( read );

				return OperateResult.CreateSuccessResult( SoftBasic.BytesReverseByWord( read.Content ).ToBoolArray( ).SelectMiddle( bitIndex, length ) );
			}
			else
			{
				OperateResult<string> modbusAddress = modbus.TranslateToModbusAddress( address, function );
				if (!modbusAddress.IsSuccess) return modbusAddress.ConvertFailed<bool[]>( );

				OperateResult<byte[][]> command = ModbusInfo.BuildReadModbusCommand( modbusAddress.Content, length, modbus.Station, modbus.AddressStartWithZero, function );
				if (!command.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( command );

				List<bool> resultArray = new List<bool>( );
				for (int i = 0; i < command.Content.Length; i++)
				{
					OperateResult<byte[]> read = modbus.ReadFromCoreServer( command.Content[i] );
					if (!read.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( read );

					int bitLength = command.Content[i][4] * 256 + command.Content[i][5];
					resultArray.AddRange( SoftBasic.ByteToBoolArray( read.Content, bitLength ) );
				}

				return OperateResult.CreateSuccessResult( resultArray.ToArray( ) );
			}
		}

#if !NET20 && !NET35
		internal static  async Task<OperateResult<bool[]>> ReadBoolHelperAsync( IModbus modbus, string address, ushort length, byte function )
		{
			if (address.IndexOf( '.' ) > 0)
			{
				string[] addressSplits = address.SplitDot( );
				int bitIndex = 0;
				try
				{
					bitIndex = Convert.ToInt32( addressSplits[1] );
				}
				catch (Exception ex)
				{
					return new OperateResult<bool[]>( "Bit Index format wrong, " + ex.Message );
				}
				ushort len = (ushort)((length + bitIndex + 15) / 16);

				OperateResult<byte[]> read = await modbus.ReadAsync( addressSplits[0], len );
				if (!read.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( read );

				return OperateResult.CreateSuccessResult( SoftBasic.BytesReverseByWord( read.Content ).ToBoolArray( ).SelectMiddle( bitIndex, length ) );
			}
			else
			{
				OperateResult<string> modbusAddress = modbus.TranslateToModbusAddress( address, function );
				if (!modbusAddress.IsSuccess) return modbusAddress.ConvertFailed<bool[]>( );

				OperateResult<byte[][]> command = ModbusInfo.BuildReadModbusCommand( modbusAddress.Content, length, modbus.Station, modbus.AddressStartWithZero, function );
				if (!command.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( command );

				List<bool> resultArray = new List<bool>( );
				for (int i = 0; i < command.Content.Length; i++)
				{
					OperateResult<byte[]> read = await modbus.ReadFromCoreServerAsync( command.Content[i] );
					if (!read.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( read );

					int bitLength = command.Content[i][4] * 256 + command.Content[i][5];
					resultArray.AddRange( SoftBasic.ByteToBoolArray( read.Content, bitLength ) );
				}

				return OperateResult.CreateSuccessResult( resultArray.ToArray( ) );
			}
		}

#endif
		/// <inheritdoc cref="ModbusTcpNet.Write(string, bool[])"/>
		public static OperateResult Write( IModbus modbus, string address, bool[] values )
		{
			OperateResult<string> modbusAddress = modbus.TranslateToModbusAddress( address, ModbusInfo.WriteCoil );
			if (!modbusAddress.IsSuccess) return modbusAddress;

			OperateResult<byte[]> command = ModbusInfo.BuildWriteBoolModbusCommand( modbusAddress.Content, values, modbus.Station, modbus.AddressStartWithZero, ModbusInfo.WriteCoil );
			if (!command.IsSuccess) return command;

			return modbus.ReadFromCoreServer( command.Content );
		}

#if !NET20 && !NET35
		/// <inheritdoc cref="Write(IModbus, string, bool[])" />
		public static async Task<OperateResult> WriteAsync( IModbus modbus, string address, bool[] values )
		{
			OperateResult<string> modbusAddress = modbus.TranslateToModbusAddress( address, ModbusInfo.WriteCoil );
			if (!modbusAddress.IsSuccess) return modbusAddress;

			OperateResult<byte[]> command = ModbusInfo.BuildWriteBoolModbusCommand( modbusAddress.Content, values, modbus.Station, modbus.AddressStartWithZero, ModbusInfo.WriteCoil );
			if (!command.IsSuccess) return command;

			return await modbus.ReadFromCoreServerAsync( command.Content );
		}
#endif
		/// <inheritdoc cref="ModbusTcpNet.Write(string, bool)"/>
		public static OperateResult Write( IModbus modbus, string address, bool value )
		{
			OperateResult<string> modbusAddress = modbus.TranslateToModbusAddress( address, ModbusInfo.WriteOneCoil );
			if (!modbusAddress.IsSuccess) return modbusAddress;

			OperateResult<byte[]> command = ModbusInfo.BuildWriteBoolModbusCommand( modbusAddress.Content, value, modbus.Station, modbus.AddressStartWithZero, ModbusInfo.WriteOneCoil );
			if (!command.IsSuccess) return command;

			return modbus.ReadFromCoreServer( command.Content );
		}

#if !NET20 && !NET35
		/// <inheritdoc cref="Write(IModbus, string, bool)"/>
		public static async Task<OperateResult> WriteAsync( IModbus modbus, string address, bool value )
		{
			OperateResult<string> modbusAddress = modbus.TranslateToModbusAddress( address, ModbusInfo.WriteOneCoil );
			if (!modbusAddress.IsSuccess) return modbusAddress;

			OperateResult<byte[]> command = ModbusInfo.BuildWriteBoolModbusCommand( modbusAddress.Content, value, modbus.Station, modbus.AddressStartWithZero, ModbusInfo.WriteOneCoil );
			if (!command.IsSuccess) return command;

			return await modbus.ReadFromCoreServerAsync( command.Content );
		}
#endif
	}
}
