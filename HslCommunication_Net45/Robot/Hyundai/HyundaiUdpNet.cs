using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HslCommunication.Core.Net;
using System.Threading;
using System.Net.Sockets;
using System.Net;
using HslCommunication.BasicFramework;
using HslCommunication.Reflection;
#if !NET35 && !NET20
using System.Threading.Tasks;
#endif

namespace HslCommunication.Robot.Hyundai
{
	/// <summary>
	/// 现代机器人的UDP通讯类，注意本类是服务器，需要等待机器人先配置好ip地址及端口，然后连接到本服务器才能正确的进行操作。详细参见api文档注释<br />
	/// The UDP communication class of modern robots. Note that this class is a server. You need to wait for the robot to configure the IP address and port first, 
	/// and then connect to this server to operate correctly. See api documentation for details
	/// </summary>
	/// <remarks>
	/// 为使用联机跟踪功能，通过JOB文件的 OnLTrack 命令激活本功能后对通信及位置增量命令 Filter 进行设置，必要时以 LIMIT 命令设置机器人的动作领域，速度限制项。
	/// 最后采用 OnLTrack 命令关闭联机跟踪功能以退出本功能。<br />
	/// 功能开始，通信及 Filter 设置，程序示例： OnLTrack ON,IP=192.168.1.254,PORT=7127,CRD=1,Bypass,Fn=10
	/// </remarks>
	public class HyundaiUdpNet : NetworkUdpServerBase
	{
		#region Contructor

		/// <summary>
		/// 实例化一个默认的对象<br />
		/// Instantiate a default object
		/// </summary>
		public HyundaiUdpNet( )
		{
			incrementCount = new SoftIncrementCount( int.MaxValue );
		}

		#endregion

		#region NetworkUdpServerBase Override

		/// <inheritdoc/>
		protected override void ThreadReceiveCycle( )
		{
			while (IsStarted)
			{
				IPEndPoint ipep = new IPEndPoint( IPAddress.Any, 0 );
				Remote = ipep;

				byte[] data = new byte[64];
				int length;
				try
				{
					length = CoreSocket.ReceiveFrom( data, ref Remote );
				}
				catch (Exception ex)
				{
					if(IsStarted) LogNet?.WriteException( "ThreadReceiveCycle", ex );
					continue;
				}

				if (length != 64)
				{
					LogNet?.WriteError( ToString( ), $"Receive Error Length[{length}]: {data.ToHexString( )}" );
					continue; 
				}
				else
				{
					LogNet?.WriteDebug( ToString( ), $"Receive: {data.ToHexString( )}" );
				}

				HyundaiData hyundaiReceive = new HyundaiData( data );
				if (hyundaiReceive.Command == 'S') // Start
				{
					HyundaiData hyundaiSend = new HyundaiData( data );
					hyundaiSend.Command = 'S';
					hyundaiSend.Count = 0;
					hyundaiSend.State = 1;
					Write( hyundaiSend );
					LogNet?.WriteDebug( ToString( ), $"Send: {hyundaiReceive.ToBytes( ).ToHexString( )}" );
					LogNet?.WriteDebug( ToString( ), "Online tracking is started by Hi5 controller." );
				}
				else if(hyundaiReceive.Command == 'P') // Play
				{
					hyundaiDataHistory = hyundaiReceive;
					OnHyundaiMessageReceive?.Invoke( hyundaiReceive );
				}
				else if (hyundaiReceive.Command == 'F') // Finish
				{
					LogNet?.WriteDebug( ToString( ), "Online tracking is finished by Hi5 controller." );
				}
			}
		}

		#endregion

		#region Public Method

		/// <summary>
		/// 将指定的增量写入机器人，需要指定6个参数，位置和角度信息，其中位置单位为mm，角度单位为°<br />
		/// To write the specified increment to the robot, you need to specify 6 parameters, 
		/// position and angle information, where the position unit is mm and the angle unit is °
		/// </summary>
		/// <param name="x">X轴增量信息，单位毫米</param>
		/// <param name="y">Y轴增量信息，单位毫米</param>
		/// <param name="z">Z轴增量信息，单位毫米</param>
		/// <param name="rx">X轴角度增量信息，单位角度</param>
		/// <param name="ry">Y轴角度增量信息，单位角度</param>
		/// <param name="rz">Z轴角度增量信息，单位角度</param>
		/// <returns>是否写入机器人成功</returns>
		[HslMqttApi]
		public OperateResult WriteIncrementPos( double x, double y, double z, double rx, double ry, double rz )
		{
			return WriteIncrementPos( new double[] { x, y, z, rx, ry, rz } );
		}

		/// <summary>
		/// 将指定的增量写入机器人，需要指定6个参数，位置和角度信息，其中位置单位为mm，角度单位为°<br />
		/// To write the specified increment to the robot, you need to specify 6 parameters, position and angle information, where the position unit is mm and the angle unit is °
		/// </summary>
		/// <param name="pos">增量的数组信息</param>
		/// <returns>是否写入机器人成功</returns>
		[HslMqttApi]
		public OperateResult WriteIncrementPos(double[] pos )
		{
			HyundaiData hyundaiReceive = new HyundaiData
			{
				Command = 'P',
				State = 2,
				Count = (int)incrementCount.GetCurrentValue( )
			};
			for (int i = 0; i < hyundaiReceive.Data.Length; i++)
			{
				hyundaiReceive.Data[i] = pos[i];
			}
			return Write( hyundaiReceive );
		}

		/// <summary>
		/// 将指定的命令写入机器人，该命令是完全自定义的，需要遵循机器人的通讯协议，在写入之前，需要调用<see cref="NetworkUdpServerBase.ServerStart(int)"/> 方法<br />
		/// Write the specified command to the robot. The command is completely customized and needs to follow the robot's communication protocol. 
		/// Before writing, you need to call the <see cref="NetworkUdpServerBase.ServerStart(int)" />
		/// </summary>
		/// <param name="data">机器人数据</param>
		/// <returns>是否写入成功</returns>
		[HslMqttApi]
		public OperateResult Write( HyundaiData data )
		{
			if (IsStarted == false) return new OperateResult( "Please Start Server First!" );
			if (Remote == null) return new OperateResult( "Please Wait Robot Connect!" );
			try
			{
				CoreSocket.SendTo( data.ToBytes( ), Remote );
				return OperateResult.CreateSuccessResult( );
			}
			catch(Exception ex)
			{
				return new OperateResult( ex.Message );
			}
		}

		/// <summary>
		/// 机器人在X轴上移动一小段距离，单位毫米<br />
		/// The robot moves a short distance on the X axis, in millimeters
		/// </summary>
		/// <param name="value">移动距离，单位毫米</param>
		/// <returns>是否写入成功</returns>
		[HslMqttApi]
		public OperateResult MoveX( double value ) => WriteIncrementPos( value, 0, 0, 0, 0, 0 );

		/// <summary>
		/// 机器人在Y轴上移动一小段距离，单位毫米<br />
		/// The robot moves a short distance on the Y axis, in millimeters
		/// </summary>
		/// <param name="value">移动距离，单位毫米</param>
		/// <returns>是否写入成功</returns>
		[HslMqttApi]
		public OperateResult MoveY( double value ) => WriteIncrementPos( 0, value, 0, 0, 0, 0 );

		/// <summary>
		/// 机器人在Z轴上移动一小段距离，单位毫米<br />
		/// The robot moves a short distance on the Z axis, in millimeters
		/// </summary>
		/// <param name="value">移动距离，单位毫米</param>
		/// <returns>是否写入成功</returns>
		[HslMqttApi]
		public OperateResult MoveZ( double value ) => WriteIncrementPos( 0, 0, value, 0, 0, 0 );

		/// <summary>
		/// 机器人在X轴方向上旋转指定角度，单位角度<br />
		/// The robot rotates the specified angle in the X axis direction, the unit angle
		/// </summary>
		/// <param name="value">旋转角度，单位角度</param>
		/// <returns>是否写入成功</returns>
		[HslMqttApi]
		public OperateResult RotateX( double value ) => WriteIncrementPos( 0, 0, 0, value, 0, 0 );

		/// <summary>
		/// 机器人在Y轴方向上旋转指定角度，单位角度<br />
		/// The robot rotates the specified angle in the Y axis direction, the unit angle
		/// </summary>
		/// <param name="value">旋转角度，单位角度</param>
		/// <returns>是否写入成功</returns>
		[HslMqttApi]
		public OperateResult RotateY( double value ) => WriteIncrementPos( 0, 0, 0, 0, value, 0 );

		/// <summary>
		/// 机器人在Z轴方向上旋转指定角度，单位角度<br />
		/// The robot rotates the specified angle in the Z axis direction, the unit angle
		/// </summary>
		/// <param name="value">旋转角度，单位角度</param>
		/// <returns>是否写入成功</returns>
		[HslMqttApi]
		public OperateResult RotateZ( double value ) => WriteIncrementPos( 0, 0, 0, 0, 0, value );

		#endregion

		#region Event Handle

		/// <summary>
		/// 收到机器人消息的事件委托
		/// </summary>
		/// <param name="data">机器人消息</param>
		public delegate void OnHyundaiMessageReceiveDelegate( HyundaiData data );

		/// <summary>
		/// 当接收到机器人数据的时候触发的事件
		/// </summary>
		public event OnHyundaiMessageReceiveDelegate OnHyundaiMessageReceive;

		#endregion

		#region Private Member

		private HyundaiData hyundaiDataHistory;
		private SoftIncrementCount incrementCount;
		private EndPoint Remote;

		#endregion

		#region Object Override

		/// <inheritdoc/>
		public override string ToString( ) => $"HyundaiUdpNet[{Port}]";

		#endregion
	}
}
