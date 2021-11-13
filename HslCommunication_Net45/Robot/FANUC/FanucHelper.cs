using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HslCommunication.BasicFramework;

namespace HslCommunication.Robot.FANUC
{
	/// <summary>
	/// Fanuc的辅助方法信息
	/// </summary>
	public class FanucHelper
	{
		/// <summary>
		/// Q区数据
		/// </summary>
		public const byte SELECTOR_Q = 72;

		/// <summary>
		/// I区数据
		/// </summary>
		public const byte SELECTOR_I = 70;

		/// <summary>
		/// AQ区数据
		/// </summary>
		public const byte SELECTOR_AQ = 12;

		/// <summary>
		/// AI区数据
		/// </summary>
		public const byte SELECTOR_AI = 10;

		/// <summary>
		/// M区数据
		/// </summary>
		public const byte SELECTOR_M = 76;

		/// <summary>
		/// D区数据
		/// </summary>
		public const byte SELECTOR_D = 8;

		/// <summary>
		/// 命令数据
		/// </summary>
		public const byte SELECTOR_G = 56;

		/// <summary>
		/// 从FANUC机器人地址进行解析数据信息，地址为D,I,Q,M,AI,AQ区<br />
		/// Parse data information from FANUC robot address, the address is D, I, Q, M, AI, AQ area
		/// </summary>
		/// <param name="address">fanuc机器人的地址信息</param>
		/// <returns>解析结果</returns>
		public static OperateResult<byte, ushort> AnalysisFanucAddress( string address )
		{
			try
			{
				if (address.StartsWith( "aq" ) || address.StartsWith( "AQ" ))
					return OperateResult.CreateSuccessResult( SELECTOR_AQ, ushort.Parse( address.Substring( 2 ) ) );
				else if (address.StartsWith( "ai" ) || address.StartsWith( "AI" ))
					return OperateResult.CreateSuccessResult( SELECTOR_AI, ushort.Parse( address.Substring( 2 ) ) );
				else if (address.StartsWith( "sr" ) || address.StartsWith( "SR" ))
				{
					ushort offset = ushort.Parse( address.Substring( 2 ) );
					if (offset < 1 || offset > 6) return new OperateResult<byte, ushort>( "SR type address only support SR1 - SR6" );
					return OperateResult.CreateSuccessResult( SELECTOR_D, (ushort)(5891 + (offset - 1) * 40) );
				}
				else if (address.StartsWith( "i" ) || address.StartsWith( "I" ))
					return OperateResult.CreateSuccessResult( SELECTOR_I, ushort.Parse( address.Substring( 1 ) ) );
				else if (address.StartsWith( "q" ) || address.StartsWith( "Q" ))
					return OperateResult.CreateSuccessResult( SELECTOR_Q, ushort.Parse( address.Substring( 1 ) ) );
				else if (address.StartsWith( "m" ) || address.StartsWith( "M" ))
					return OperateResult.CreateSuccessResult( SELECTOR_M, ushort.Parse( address.Substring( 1 ) ) );
				else if (address.StartsWith( "d" ) || address.StartsWith( "D" ))
					return OperateResult.CreateSuccessResult( SELECTOR_D, ushort.Parse( address.Substring( 1 ) ) );
				else if (address.StartsWith( "r" ) || address.StartsWith( "R" ))
				{
					ushort offset = ushort.Parse( address.Substring( 1 ) );
					if (offset < 1 || offset > 10) return new OperateResult<byte, ushort>( "R type address only support R1 - R10" );
					return OperateResult.CreateSuccessResult( SELECTOR_D, (ushort)(3451 + (offset - 1) * 2) );
				}
				else
					return new OperateResult<byte, ushort>( StringResources.Language.NotSupportedDataType );
			}
			catch (Exception ex)
			{
				return new OperateResult<byte, ushort>( ex.Message );
			}
		}


		/// <summary>
		/// 构建读取数据的报文内容
		/// </summary>
		/// <param name="sel">数据类别</param>
		/// <param name="address">偏移地址</param>
		/// <param name="length">长度</param>
		/// <returns>报文内容</returns>
		public static byte[] BulidReadData( byte sel, ushort address, ushort length )
		{
			byte[] read_req = new byte[56] { 2, 0, 6, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 6, 192, 0, 0, 0, 0, 16, 14, 0, 0, 1, 1, 4, 8, 0, 0, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
			read_req[43] = sel;
			read_req[44] = BitConverter.GetBytes( address - 1 )[0];
			read_req[45] = BitConverter.GetBytes( address - 1 )[1];
			read_req[46] = BitConverter.GetBytes( length )[0];
			read_req[47] = BitConverter.GetBytes( length )[1];
			return read_req;
		}

		/// <summary>
		/// 构建读取返回的数据信息
		/// </summary>
		/// <param name="data">数据</param>
		/// <returns>结果</returns>
		public static byte[] BuildReadResponseData( byte[] data )
		{
			byte[] buffer = SoftBasic.HexStringToBytes( @"
03 00 06 00 e4 2f 00 00 00 01 00 00 00 00 00 00
00 01 00 00 00 00 00 00 00 00 00 00 00 00 06 94
10 0e 00 00 30 3a 00 00 01 01 00 00 00 00 00 00
01 01 ff 04 00 00 7c 21" );
			if (data.Length > 6)
			{
				buffer = SoftBasic.SpliceArray( buffer, data );
				buffer[4] = BitConverter.GetBytes( data.Length )[0];
				buffer[5] = BitConverter.GetBytes( data.Length )[1];
				return buffer;
			}
			else
			{
				buffer[4] = 0x00;
				buffer[5] = 0x00;
				buffer[31] = 0xD4;
				data.CopyTo( buffer, 44 );
				return buffer;
			}
		}

		/// <summary>
		/// 构建写入的数据报文，需要指定相关的参数信息
		/// </summary>
		/// <param name="sel">数据类别</param>
		/// <param name="address">偏移地址</param>
		/// <param name="value">原始数据内容</param>
		/// <param name="length">写入的数据长度</param>
		/// <returns>报文内容</returns>
		public static byte[] BuildWriteData( byte sel, ushort address, byte[] value, int length )
		{
			if (value == null) value = new byte[0];
			if (value.Length > 6)
			{
				byte[] buffer = new byte[56 + value.Length];
				byte[] write_req = new byte[56] { 2, 0, 9, 0, 50, 0, 0, 0, 0, 2, 0, 0, 0, 0, 0, 0, 0, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 9, 128, 0, 0, 0, 0, 16, 14, 0, 0, 1, 1, 50, 0, 0, 0, 0, 0, 1, 1, 7, 8, 49, 0, 25, 0 };
				write_req.CopyTo( buffer, 0 );
				value.CopyTo( buffer, 56 );

				buffer[4] = BitConverter.GetBytes( value.Length )[0];
				buffer[5] = BitConverter.GetBytes( value.Length )[1];
				buffer[51] = sel;
				buffer[52] = BitConverter.GetBytes( address - 1 )[0];
				buffer[53] = BitConverter.GetBytes( address - 1 )[1];
				buffer[54] = BitConverter.GetBytes( length )[0];
				buffer[55] = BitConverter.GetBytes( length )[1];
				return buffer;
			}
			else
			{
				byte[] write_req = new byte[56] { 2, 0, 8, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 8, 192, 0, 0, 0, 0, 16, 14, 0, 0, 1, 1, 7, 8, 9, 0, 4, 0, 1, 0, 2, 0, 3, 0, 4, 0 };
				write_req[43] = sel;
				write_req[44] = BitConverter.GetBytes( address - 1 )[0];
				write_req[45] = BitConverter.GetBytes( address - 1 )[1];
				write_req[46] = BitConverter.GetBytes( length )[0];
				write_req[47] = BitConverter.GetBytes( length )[1];
				value.CopyTo( write_req, 48 );
				return write_req;
			}
		}

		/// <summary>
		/// 获取所有的命令信息<br />
		/// Get all command information
		/// </summary>
		/// <returns>命令数组</returns>
		public static string[] GetFanucCmds( )
		{
			return new string[]
			{
				"CLRASG",                                                 // 0.
				"SETASG 1 500 ALM[E1] 1",                                 // 1.
				"SETASG 501 100 ALM[1] 1",                                // 2.
				"SETASG 601 100 ALM[P1] 1",                               // 3.
				"SETASG 701 50 POS[15] 0.0",                              // 4.
				"SETASG 751 50 POS[15] 0.0",                              // 5.
				"SETASG 801 50 POS[G2: 15] 0.0",                          // 6.
				"SETASG 851 50 POS[G3: 0] 0.0",                           // 7.
				"SETASG 901 50 POS[G4:0] 0.0",                            // 8.
				"SETASG 951 50 POS[G5:0] 0.0",                            // 9.
				"SETASG 1001 18 PRG[1] 1",                                // 10.
				"SETASG 1019 18 PRG[M1] 1",                               // 11.
				"SETASG 1037 18 PRG[K1] 1",                               // 12.
				"SETASG 1055 18 PRG[MK1] 1",                              // 13.
				"SETASG 1073 500 PR[1] 0.0",                              // 14.
				"SETASG 1573 200 PR[G2:1] 0.0",                           // 15.
				"SETASG 1773 500 PR[G3:1] 0.0",                           // 16.
				"SETASG 2273 500 PR[G4: 1] 0.0",                          // 17.
				"SETASG 2773 500 PR[G5: 1] 0.0",                          // 18.
				"SETASG 3273 2 $FAST_CLOCK 1",                            // 19.
				"SETASG 3275 2 $TIMER[10].$TIMER_VAL 1",                  // 20.
				"SETASG 3277 2 $MOR_GRP[1].$CURRENT_ANG[1] 0",            // 21.
				"SETASG 3279 2 $DUTY_TEMP 0",                             // 22.
				"SETASG 3281 40 $TIMER[10].$COMMENT 1",                   // 23.
				"SETASG 3321 40 $TIMER[2].$COMMENT 1",                    // 24.
				"SETASG 3361 50 $MNUTOOL[1,1] 0.0",                       // 25.
				"SETASG 3411 40 $[HTTPKCL]CMDS[1] 1",                     // 26.
				"SETASG 3451 10 R[1] 1.0",                                // 27.
				"SETASG 3461 10 R[6] 0",                                  // 28.
				"SETASG 3471 250 PR[1]@1.25 0.0",                         // 29.
				"SETASG 3721 250 PR[1]@1.25 0.0",                         // 30.
				"SETASG 3971 120 PR[G2:1]@27.12 0.0",                     // 31.
				"SETASG 4091 120 DI[C1] 1",                               // 32.
				"SETASG 4211 120 DO[C1] 1",                               // 33.
				"SETASG 4331 120 RI[C1] 1",                               // 34.
				"SETASG 4451 120 RO[C1] 1",                               // 35.
				"SETASG 4571 120 UI[C1] 1",                               // 36.
				"SETASG 4691 120 UO[C1] 1",                               // 37.
				"SETASG 4811 120 SI[C1] 1",                               // 38.
				"SETASG 4931 120 SO[C1] 1",                               // 39.
				"SETASG 5051 120 WI[C1] 1",                               // 40.
				"SETASG 5171 120 WO[C1] 1",                               // 41.
				"SETASG 5291 120 WSI[C1] 1",                              // 42.
				"SETASG 5411 120 AI[C1] 1",                               // 43.
				"SETASG 5531 120 AO[C1] 1",                               // 44.
				"SETASG 5651 120 GI[C1] 1",                               // 45.
				"SETASG 5771 120 GO[C1] 1",                               // 46.
				"SETASG 5891 120 SR[1] 1",                                // 47.
				"SETASG 6011 120 SR[C1] 1",                               // 48.
				"SETASG 6131 10 R[1] 1.0",                                // 49.
				"SETASG 6141 2 $TIMER[1].$TIMER_VAL 1",                   // 50.
				"SETASG 6143 2 $TIMER[2].$TIMER_VAL 1",                   // 51.
				"SETASG 6145 2 $TIMER[3].$TIMER_VAL 1",                   // 52.
				"SETASG 6147 2 $TIMER[4].$TIMER_VAL 1",                   // 53.
				"SETASG 6149 2 $TIMER[5].$TIMER_VAL 1",                   // 54.
				"SETASG 6151 2 $TIMER[6].$TIMER_VAL 1",                   // 55.
				"SETASG 6153 2 $TIMER[7].$TIMER_VAL 1",                   // 56.
				"SETASG 6155 2 $TIMER[8].$TIMER_VAL 1",                   // 57.
				"SETASG 6157 2 $TIMER[9].$TIMER_VAL 1",                   // 58.
				"SETASG 6159 2 $TIMER[10].$TIMER_VAL 1",                  // 59
			};
		}

	}
}
