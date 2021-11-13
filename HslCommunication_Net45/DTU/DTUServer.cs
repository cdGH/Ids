using HslCommunication.Core;
using HslCommunication.Core.IMessage;
using HslCommunication.Core.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
#if !NET35 && !NET20
using System.Threading.Tasks;
#endif

namespace HslCommunication.DTU
{
	/// <summary>
	/// DTU的服务器信息，本服务器支持任意的hsl支持的网络对象，包括plc信息，modbus设备等等，通过DTU来连接，
	/// 然后支持多个连接对象。如果需要支持非hsl的注册报文，需要重写相关的方法<br />
	/// DTU server information, the server supports any network objects supported by hsl, 
	/// including plc information, modbus devices, etc., connected through DTU, and then supports multiple connection objects. 
	/// If you need to support non-HSL registration messages, you need to rewrite the relevant methods
	/// </summary>
	/// <remarks>
	/// 针对异形客户端进行扩展信息
	/// </remarks>
	public class DTUServer : NetworkAlienClient
	{
		#region Constructor

		/// <summary>
		/// 根据配置的列表信息来实例化相关的DTU服务器<br />
		/// Instantiate the relevant DTU server according to the configured list information
		/// </summary>
		/// <param name="dTUSettings">DTU的配置信息</param>
		public DTUServer( List<DTUSettingType> dTUSettings )
		{
			devices = new Dictionary<string, NetworkDeviceBase>( );
			// 输入规定的DTU ID信息
			SetTrustClients( dTUSettings.Select( m => m.DtuId ).ToArray( ) );
			for (int i = 0; i < dTUSettings.Count; i++)
			{
				devices.Add( dTUSettings[i].DtuId, dTUSettings[i].GetClient( ) );
				devices[dTUSettings[i].DtuId].ConnectServer( new AlienSession( ) { DTU = dTUSettings[i].DtuId, IsStatusOk = false } );
			}

			OnClientConnected += DTUServer_OnClientConnected;
		}

		/// <summary>
		/// 根据配置的列表信息来实例化相关的DTU服务器<br />
		/// Instantiate the relevant DTU server according to the configured list information
		/// </summary>
		/// <param name="dtuId">Dtu信息</param>
		/// <param name="networkDevices">设备信息</param>
		public DTUServer( string[] dtuId, NetworkDeviceBase[] networkDevices)
		{
			devices = new Dictionary<string, NetworkDeviceBase>( );
			// 输入规定的DTU ID信息
			SetTrustClients( dtuId );
			for (int i = 0; i < dtuId.Length; i++)
			{
				devices.Add( dtuId[i], networkDevices[i] );
				devices[dtuId[i]].ConnectServer( new AlienSession( ) { DTU = dtuId[i], IsStatusOk = false } );
			}
		}

		#endregion

		#region NetworkAlienClient Override

		/// <inheritdoc/>
		protected override void CloseAction( )
		{
			foreach(var device in devices)
			{
				AlienSession session = device.Value.AlienSession;
				if (session != null)
				{
					session.IsStatusOk = false;
					session.Socket?.Close( );
				}
			}
		}

		/// <inheritdoc/>
		public override int IsClientOnline( AlienSession session )
		{
			if(devices[session.DTU].AlienSession == null)
			{
				// 初次登陆
				return StatusOk;
			}
			else if(devices[session.DTU].AlienSession.IsStatusOk == true)
			{
				// 已登录，并且网络状态是好的情况，就拒绝登录
				return StatusLoginRepeat;
			}
			else
			{
				// 已登录，但是网络异常，重新设置新的
				return StatusOk;
			}
		}

		private void DTUServer_OnClientConnected( AlienSession session )
		{
			// 上线之后更新到对应的设备里面
			devices[session.DTU].ConnectServer( session );
		}

		#endregion

		/// <summary>
		/// 根据DTU信息获取设备的连接对象<br />
		/// Obtain the connection object of the device according to the DTU information
		/// </summary>
		/// <param name="dtuId">设备的id信息</param>
		/// <returns>设备的对象</returns>
		public NetworkDeviceBase this[string dtuId] => devices.ContainsKey( dtuId ) ? devices[dtuId] : null;

		/// <summary>
		/// 获取所有的会话信息，是否在线，上线的基本信息<br />
		/// Get all the session information, whether it is online, online basic information
		/// </summary>
		/// <returns>会话列表</returns>
		public AlienSession[] GetAlienSessions( ) => devices.Values.Select( m => m.AlienSession ).ToArray( );

		/// <summary>
		/// 获取所有的设备的信息，可以用来读写设备的数据信息<br />
		/// Get all device information, can be used to read and write device data information
		/// </summary>
		/// <returns>设备数组</returns>
		public NetworkDeviceBase[] GetDevices( ) => devices.Values.ToArray( );

		#region Object Override

		/// <inheritdoc/>
		public override string ToString( ) => $"Dtu[{Port}]";

		#endregion

		#region Private Member

		private Dictionary<string, NetworkDeviceBase> devices;              // 关于dtu的所有的设备的列表

		#endregion
	}
}
