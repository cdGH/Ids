using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
#if !NET35 && !NET20
using System.Threading.Tasks;
#endif

namespace HslCommunication.Reflection
{
	/// <summary>
	/// 对应redis的一个键值信息的内容
	/// </summary>
	[AttributeUsage( AttributeTargets.Property, AllowMultiple = false )]
	public class HslRedisKeyAttribute : Attribute
	{
		/// <summary>
		/// 键值的名称
		/// </summary>
		public string KeyName { get; set; }

		/// <summary>
		/// 根据键名来读取写入当前的数据信息
		/// </summary>
		/// <param name="key">键名</param>
		public HslRedisKeyAttribute(string key )
		{
			KeyName = key;
		}
	}

	internal class PropertyInfoKeyName
	{
		public PropertyInfoKeyName( PropertyInfo property, string key )
		{
			PropertyInfo = property;
			KeyName = key;
		}
		public PropertyInfoKeyName( PropertyInfo property, string key, string value )
		{
			PropertyInfo = property;
			KeyName = key;
			Value = value;
		}

		public PropertyInfo PropertyInfo { get; set; }
		public string KeyName { get; set; }
		public string Value { get; set; }
	}

	internal class PropertyInfoHashKeyName : PropertyInfoKeyName
	{
		public PropertyInfoHashKeyName( PropertyInfo property, string key, string field ) : base( property, key )
		{
			Field = field;
		}
		public PropertyInfoHashKeyName( PropertyInfo property, string key, string field , string value) : base( property, key, value )
		{
			Field = field;
		}
		public string Field { get; set; }
	}

	/// <summary>
	/// 对应redis的一个列表信息的内容
	/// </summary>
	[AttributeUsage( AttributeTargets.Property, AllowMultiple = false )]
	public class HslRedisListItemAttribute : Attribute
	{
		/// <summary>
		/// 列表键值的名称
		/// </summary>
		public string ListKey { get; set; }

		/// <summary>
		/// 当前的位置的索引
		/// </summary>
		public long Index { get; set; }

		/// <summary>
		/// 根据键名来读取写入当前的列表中的单个信息
		/// </summary>
		/// <param name="listKey">列表键名</param>
		/// <param name="index">当前的索引位置</param>
		public HslRedisListItemAttribute( string listKey, long index )
		{
			ListKey = listKey;
			Index = index;
		}
	}

	/// <summary>
	/// 对应redis的一个列表信息的内容
	/// </summary>
	[AttributeUsage( AttributeTargets.Property, AllowMultiple = false )]
	public class HslRedisListAttribute : Attribute
	{
		/// <summary>
		/// 列表键值的名称
		/// </summary>
		public string ListKey { get; set; }

		/// <summary>
		/// 当前的位置的索引
		/// </summary>
		public long StartIndex { get; set; }

		/// <summary>
		/// 当前位置的结束索引
		/// </summary>
		public long EndIndex { get; set; } = -1;

		/// <summary>
		/// 根据键名来读取写入当前的列表中的多个信息
		/// </summary>
		/// <param name="listKey">列表键名</param>
		public HslRedisListAttribute( string listKey )
		{
			ListKey = listKey;
		}

		/// <summary>
		/// 根据键名来读取写入当前的列表中的多个信息
		/// </summary>
		/// <param name="listKey">列表键名</param>
		/// <param name="startIndex">开始的索引信息</param>
		public HslRedisListAttribute( string listKey, long startIndex )
		{
			ListKey = listKey;
			StartIndex = startIndex;
		}

		/// <summary>
		/// 根据键名来读取写入当前的列表中的多个信息
		/// </summary>
		/// <param name="listKey">列表键名</param>
		/// <param name="startIndex">开始的索引信息</param>
		/// <param name="endIndex">结束的索引位置，-1为倒数第一个，以此类推。</param>
		public HslRedisListAttribute( string listKey, long startIndex, long endIndex )
		{
			ListKey = listKey;
			StartIndex = startIndex;
			EndIndex = endIndex;
		}
	}

	/// <summary>
	/// 对应redis的一个哈希信息的内容
	/// </summary>
	[AttributeUsage( AttributeTargets.Property, AllowMultiple = false )]
	public class HslRedisHashFieldAttribute : Attribute
	{
		/// <summary>
		/// 哈希键值的名称
		/// </summary>
		public string HaskKey { get; set; }

		/// <summary>
		/// 当前的哈希域名称
		/// </summary>
		public string Field { get; set; }

		/// <summary>
		/// 根据键名来读取写入当前的哈希的单个信息
		/// </summary>
		/// <param name="hashKey">哈希键名</param>
		/// <param name="filed">哈希域名称</param>
		public HslRedisHashFieldAttribute( string hashKey, string filed )
		{
			HaskKey = hashKey;
			Field = filed;
		}
	}
}
