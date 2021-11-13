using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace HslCommunication
{
	/// <summary>
	/// 系统的基本授权类
	/// </summary>
	public class Authorization
	{
		static Authorization( )
		{
			niahdiahduasdbubfas = iashdagsdawbdawda( );
			if (naihsdadaasdasdiwasdaid != 0)
			{
				naihsdadaasdasdiwasdaid = 0;
			}

			if (nuasgdawydaishdgas != 24)
			{
				nuasgdawydaishdgas = 24;
			}

			if (nuasgdaaydbishdgas != 0)
			{
				nuasgdaaydbishdgas = 0;
			}

			if (nuasgdawydbishdgas != 24)
			{
				nuasgdawydbishdgas = 24;
			}
			//System.Threading.ThreadPool.QueueUserWorkItem( new System.Threading.WaitCallback( asidhiahfaoaksdnasoif ), null );
		}

		private static void asidhiahfaoaksdnasoif(object obj )
		{
			for (int i = 0; i < 3600; i++)
			{
				System.Threading.Thread.Sleep( 1000 );
				if (naihsdadaasdasdiwasdaid == niasdhasdguawdwdad && nuasgdaaydbishdgas > 0)
				{
					return;
				}
			}

			Enthernet.NetSimplifyClient simplifyClient = new Enthernet.NetSimplifyClient( "118.24.36.220", 18467 );
			simplifyClient.ReadCustomerFromServer( 500, BasicFramework.SoftBasic.FrameworkVersion.ToString( ) );
		}

		internal static bool nzugaydgwadawdibbas( )
		{
			moashdawidaisaosdas++;
			// 如果需要移除验证，这里返回true即可。
			// return true;
			if (naihsdadaasdasdiwasdaid == niasdhasdguawdwdad && nuasgdaaydbishdgas > 0) return nuasduagsdwydbasudasd( );
			if ((iashdagsdawbdawda( ) - niahdiahduasdbubfas).TotalHours < nuasgdawydaishdgas) // .TotalHours < nuasgdawydaishdgas)
			{
				return nuasduagsdwydbasudasd( );
			}

			return asdhuasdgawydaduasdgu( );
		}

		/// <summary>
		/// 商业授权则返回true，否则返回false
		/// </summary>
		/// <returns>是否成功进行商业授权</returns>
		internal static bool asdniasnfaksndiqwhawfskhfaiw( )
		{
			// 这里是一些只能商业授权对象使用的接口方法
			// 如果需要移除验证，这里返回true即可。
			// return true;
			if (naihsdadaasdasdiwasdaid == niasdhasdguawdwdad && nuasgdaaydbishdgas > 0) return nuasduagsdwydbasudasd( );
			if ((iashdagsdawbdawda( ) - niahdiahduasdbubfas).TotalHours < nuasgdawydbishdgas) // .TotalHours < nuasgdawydbishdgas)
			{
				return nuasduagsdwydbasudasd( );
			}
			return asdhuasdgawydaduasdgu( );
		}

		internal static bool nuasduagsdwydbasudasd( )
		{
			return true;
		}

		internal static bool asdhuasdgawydaduasdgu( )
		{
			return false;
		}

		internal static bool ashdadgawdaihdadsidas( )
		{
			return niasdhasdguawdwdad == 12345;
		}

		internal static DateTime iashdagsdawbdawda( )
		{
			return DateTime.Now;
		}
		internal static DateTime iashdagsaawbdawda( )
		{
			return DateTime.Now.AddDays(1);
		}

		internal static DateTime iashdagsaawadawda( )
		{
			return DateTime.Now.AddDays( 2 );
		}

		internal static void oasjodaiwfsodopsdjpasjpf( )
		{
			System.Threading.Interlocked.Increment( ref iahsduiwikaskfhishfdi );
		}

		internal static string nasduabwduadawdb( string miawdiawduasdhasd )
		{
			StringBuilder asdnawdawdawd = new StringBuilder( );
			MD5 asndiawdniad = MD5.Create( );
			byte[] asdadawdawdas = asndiawdniad.ComputeHash( Encoding.Unicode.GetBytes( miawdiawduasdhasd ) );
			asndiawdniad.Clear( );
			for (int andiawbduawbda = 0; andiawbduawbda < asdadawdawdas.Length; andiawbduawbda++)
			{
				asdnawdawdawd.Append( (255 - asdadawdawdas[andiawbduawbda]).ToString( "X2" ) );
			}
			return asdnawdawdawd.ToString( );
		}

		/// <summary>
		/// 设置本组件系统的授权信息，如果激活失败，只能使用24小时，24小时后所有的网络通信不会成功<br />
		/// Set the authorization information of this component system. If the activation fails, it can only be used for 8 hours. All network communication will not succeed after 8 hours
		/// </summary>
		/// <param name="code">授权码</param>
		public static bool SetAuthorizationCode( string code )
		{
			if (nasduabwduadawdb( code ) == "9CE5761DE8D41BDAC5AB6EF26AAA55DB")         // 普通vip群
			{
				nuasgdaaydbishdgas = 1;
				nuasgdawydbishcgas = 286512937;
				nuasgdawydaishdgas = 24 * 365 * 10;
				return nuasduagsdwydbasudasd( );
			}
			else if (nasduabwduadawdb( code ) == "B7D40F4D8B229E02AC463A096BCD5707")    // 高级测试用户，实例化没有上限，时间延长到3个月
			{
				nuasgdaaydbishdgas = 1;
				nuasgdawydbishcgas = 286512937;
				nuasgdawydaishdgas = 24 * 90;
				return nuasduagsdwydbasudasd( );
			}
			else if (nasduabwduadawdb( code ) == "2765FFFDDE2A8465A9522442F5A15593")    // 超级vip群的固定的激活码
			{
				nuasgdaaydbishdgas = 10000;
				nuasgdawydbishcgas = nuasgdawydbishdgas;
				naihsdadaasdasdiwasdaid = niasdhasdguawdwdad;
				return nuasduagsdwydbasudasd( );
			}
			return asdhuasdgawydaduasdgu( );
		}

#pragma warning disable CS0414 // 删除未使用的私有成员
		private static readonly string Declaration = "禁止对本组件进行反编译，篡改源代码，如果用于商业用途，将追究法律责任，如需注册码，请联系作者，QQ号：200962190，邮箱：hsl200909@163.com，欢迎企业合作。";
#pragma warning restore CS0414 // 删除未使用的私有成员

		private static DateTime niahdiahduasdbubfas = DateTime.Now;
		internal static long naihsdadaasdasdiwasdaid = 0;
		internal static long moashdawidaisaosdas = 0;
		internal static double nuasgdawydbishcgas = 8;
		internal static int nuasgdaaydbishdgas = 0;
		internal static int nuasgdawydbishdgas = 8;
		internal static double nuasgdawydaishdgas = 24;
		internal static int nasidhadguawdbasd = 1000;
		internal static int niasdhasdguawdwdad = 12345;
		internal static int hidahwdauushduasdhu = 23456;
		internal static long iahsduiwikaskfhishfdi = 0;
		internal static int zxnkasdhiashifshfsofh = 0;

		// 超级vip 激活码 f562cc4c-4772-4b32-bdcd-f3e122c534e3
		// 高级测试激活码 baf61559-184a-4a57-8308-3cc50bab168e

		// V8.0.0 激活码：883da682-1dd3-4ad3-9bbe-bccdd696cf7a
		// V8.0.1 激活码：e503efed-e657-4714-a254-7391f82e6cc2
		// V8.0.2 激活码：4f3e9053-0672-40ff-a70c-99748c380116
		// V8.0.3 激活码：e8347b41-0473-45cf-9ede-df129d88d191
		// V8.1.0 激活码：5984db31-7461-4239-8226-529ba96e27aa
		// V8.1.1 激活码：5984db31-7461-4239-8226-529ba96e27aa
		// V8.1.2 激活码：7a6b1f26-ae6e-4399-b2d4-e09246644028
		// V8.1.3 激活码：b32c095c-8d87-4ae5-90d7-35199d08a3ba
		// V8.2.0 激活码：1701775c-1d4f-4282-ac87-969aa8cd0f44
		// V8.2.1 激活码：80edf720-d9b9-4359-a747-74d932d7f098
		// V8.2.2 激活码：ee51cd0c-d763-47af-b335-59e07b125a4f
		// V9.0.0 激活码：f41e3068-9d50-4442-a18e-7924c975c7d6
		// V9.0.1 激活码：a93c6a5f-3c4d-48e6-8b99-24fc36d8e9f3
		// V9.0.2 激活码：a93c6a5f-3c4d-48e6-8b99-24fc36d8e9f3
		// V9.0.3 激活码：cbeab983-9627-406f-b63c-bce8496f3186
		// V9.1.0 激活码：7e2ddc95-83b7-413d-afe6-8238a5987bfa
		// V9.1.1 激活码：af6045d3-1a15-46ab-90c6-0295820ceadf
		// V9.1.2 激活码：7eac156e-9ef8-46e6-be93-310ecdb2cce2
		// V9.1.3 激活码：a6ffdfde-48bd-4684-b37b-5c2fe7c2e4c6
		// V9.1.4 激活码：4d874e3e-ed32-4db8-9266-212683a2230c
		// V9.2.0 激活码：c0155237-616f-4023-853e-e853a6a62148
		// V9.2.1 激活码：a5161c03-ad0b-48c5-9dd5-a11688d41e82
		// V9.2.2 激活码：60434107-b8a8-4f0b-8f3f-a5b1dbd0c1c8
		// V9.2.3 激活码：b302669a-6b8c-4c9f-b72b-53953f9e5efd
		// V9.2.4 激活码：3ab67b8a-b31f-4250-8076-99209beca142
		// V9.2.5 激活码：67b34c58-4638-4086-8a4a-7b2a92936f5e
		// V9.2.6 激活码：1841f64d-ba51-44b5-922c-40d54d6dd518
		// V9.2.7 激活码：3f13f4d0-85a2-435f-873e-1206b9198a21
		// V9.2.8 激活码：6e5a94ee-8672-4c74-96a5-1cca6a46e0fb
		// V9.3.0 激活码：80b4f667-3c33-4f32-9bfd-aeb10cf98096
		// V9.3.1 激活码：2487cfb8-334f-4b04-bd32-122453cfeb37
		// V9.3.2 激活码：2c3d4f59-fcf9-4d7a-b08f-0729ea87bcda
		// V9.5.0 激活码：c8d5ebc3-66b3-4ae8-9f12-e93ca91d5c10
		// V9.5.1 激活码：92db0877-b41e-4445-a594-7be68e32a5ee
		// V9.5.2 激活码：ca2d7655-d9a4-4fbd-8af0-1b7580cbd0e6
		// V9.5.3 激活码：40d98e6e-dfca-4a0f-aaea-488d34d2d7ab
		// V9.6.0 激活码：6803a1a2-d614-40b4-b85b-52aee4dce075
		// V9.6.1 激活码：61667bc8-3d02-4532-b64b-5f0285a86ef6
		// V9.6.2 激活码：b5f8f9f7-c075-4913-ae6e-d7d2cc6f2de1
		// V9.6.3 激活码：492ccd24-703f-4df1-bb0e-ef194d6b99dd
		// V9.6.4 激活码: 52781e9d-d88e-4c9a-aa07-a2e2c381c254
		// V9.6.5 激活码：6f590267-cd64-4b78-a976-cb8e037f4e9d
		// V9.6.6 激活码：e9d4e3a6-79e4-4b0f-bb93-1f0c70a92656
		// V9.7.0 激活码：a4908a2c-4fb6-4a14-a418-d183c5a6e448
		// V9.8.0 激活码: 68d2cdb8-58f7-4b5f-a894-e14cbba7c1ed
		// V10.0.0 激活码：dc7a17d2-862a-47e5-bbe5-1491c773a6ac
		// V10.0.1 激活码：f44b3795-d88e-4d48-b7c9-d38e3c48441e
		// V10.0.2 激活码：f76cedca-b611-4875-bd99-0cda9c69bfed
		// V10.1.0 激活码：5a68072e-345c-496b-a3ce-52994713d095
		// V10.1.1 激活码：2b22bc59-5cd8-4e92-b339-77c0edd34152
		// V10.1.2 激活码：4772a1a2-72cf-4fcb-9a8e-9cbdd5984253
		// V10.1.3 激活码：af7ecd84-9b5e-4002-a14d-6348d063e829
		// V10.1.4 激活码：afd55951-169d-4766-b151-e6509d1bbf1c
		// V10.2.0 激活码：24742e53-a574-43e3-bde9-c835a89690c6
		// V10.2.1 激活码：30c15272-3960-4853-9fab-3087392ee5cd
		// V10.2.2 激活码：98141214-0241-46e4-9dfe-a06f71c7edf4
	}
}
