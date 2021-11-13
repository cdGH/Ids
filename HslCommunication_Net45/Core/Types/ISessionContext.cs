using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HslCommunication.Core
{
	/// <summary>
	/// 连接会话信息的上下文，主要是对账户信息的验证
	/// </summary>
	public interface ISessionContext
	{
		/// <summary>
		/// 当前的用户名信息
		/// </summary>
		string UserName { get; set; }

		/// <summary>
		/// 当前的会话的ID信息
		/// </summary>
		string ClientId { get; set; }
	}
}
