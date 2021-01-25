using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BeatSaberMarkupLanguage.Attributes;

namespace SongArt
{
	public class SettingsController : PersistentSingleton<SettingsController>
	{
		[UIValue("fade-enable")]
		public bool FadeEnabled { 
			get { return PluginConfig.Instance.fadeEnabled; }
			set { PluginConfig.Instance.fadeEnabled = value; }
		}

		[UIValue("fade-delay")]
		public int FadeDelay
		{
			get { return PluginConfig.Instance.fadeDelay; }
			set { PluginConfig.Instance.fadeDelay = value; }
		}

		[UIValue("transparency")]
		public float Transparency
		{
			get { return PluginConfig.Instance.transparency; }
			set { PluginConfig.Instance.transparency = value; }
		}

		[UIValue("distance")]
		public int Distance
		{
			get { return PluginConfig.Instance.distance; }
			set { PluginConfig.Instance.distance = value; }
		}

		[UIValue("y-offset")]
		public int YOffset
		{
			get { return PluginConfig.Instance.yOffset; }
			set { PluginConfig.Instance.yOffset = value; }
		}

		[UIValue("scale")]
		public int Scale
		{
			get { return PluginConfig.Instance.scale; }
			set { PluginConfig.Instance.scale = value; }
		}

		[UIValue("mul-blending")]
		public bool MulBlending
		{
			get { return PluginConfig.Instance.mulBlending; }
			set { PluginConfig.Instance.mulBlending = value; }
		}

		[UIValue("react-enable")]
		public bool ReactEnabled
		{
			get { return PluginConfig.Instance.reactEnabled; }
			set { PluginConfig.Instance.reactEnabled = value; }
		}

		[UIAction("#apply")]
		public void OnApply() { 
			
		}
	}
}
