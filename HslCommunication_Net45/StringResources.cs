using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace HslCommunication
{

	/*******************************************************************************
	 * 
	 *    根据当前系统的语言来切换hsl的自身的语言系统
	 *    Switch hsl's own language system according to the language of the current system
	 * 
	 *******************************************************************************/

	/// <summary>
	/// 系统的字符串资源及多语言管理中心<br />
	/// System string resource and multi-language management Center
	/// </summary>
	public static class StringResources
	{
		#region Constractor

		static StringResources( )
		{
			if (System.Globalization.CultureInfo.CurrentCulture.ToString( ).StartsWith( "zh" ))
				SetLanguageChinese( );
			else
				SeteLanguageEnglish( );
		}

		#endregion

		/// <summary>
		/// 获取或设置系统的语言选项<br />
		/// Gets or sets the language options for the system
		/// </summary>
		public static Language.DefaultLanguage Language = new Language.DefaultLanguage( );

		/// <summary>
		/// 将语言设置为中文<br />
		/// Set the language to Chinese
		/// </summary>
		public static void SetLanguageChinese( ) => Language = new Language.DefaultLanguage( );

		/// <summary>
		/// 将语言设置为英文<br />
		/// Set the language to English
		/// </summary>
		public static void SeteLanguageEnglish( ) => Language = new Language.English( );
	}
}
