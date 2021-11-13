using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HslCommunication.Reflection
{
	/// <summary>
	/// 可以指定方法的权限内容，可以限定MQTT会话的ClientID信息或是UserName内容<br />
	/// 
	/// </summary>
	[AttributeUsage( AttributeTargets.Method, AllowMultiple = false )]
	public class HslMqttPermissionAttribute : Attribute
	{
		/// <summary>
		/// ClientId的限定内容
		/// </summary>
		public string ClientID { get; set; }

		/// <summary>
		/// UserName的限定内容
		/// </summary>
		public string UserName { get; set; }

		/// <summary>
		/// 检查当前的客户端ID是否通过
		/// </summary>
		/// <param name="clientID">ID信息</param>
		/// <returns>是否检测成功</returns>
		public virtual bool CheckClientID( string clientID )
		{
			if (string.IsNullOrEmpty( ClientID )) return true;
			return ClientID == clientID;
		}

		/// <summary>
		/// 检查当前的用户名是否通过
		/// </summary>
		/// <param name="name">用户名</param>
		/// <returns>是否检测成功</returns>
		public virtual bool CheckUserName( string name )
		{
			if (string.IsNullOrEmpty( UserName )) return true;
			return UserName == name;
		}
	}
}
