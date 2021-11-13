using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HslCommunication.Core
{
	/// <inheritdoc cref="ISessionContext"/>
	public class SessionContext : ISessionContext
	{
		/// <inheritdoc cref="ISessionContext.UserName"/>
		public string UserName { get ; set; }

		/// <inheritdoc cref="ISessionContext.ClientId"/>
		public string ClientId { get; set; }
	}
}
