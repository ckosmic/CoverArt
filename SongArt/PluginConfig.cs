using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using IPA.Config.Stores;

[assembly: InternalsVisibleTo(GeneratedStore.AssemblyVisibilityTarget)]
namespace SongArt
{
	internal class PluginConfig
	{
		public static PluginConfig Instance { get; set; }
		public bool fadeEnabled { get; set; } = false;
		public int fadeDelay { get; set; } = 5;
		public float transparency { get; set; } = 0.8f;
		public int distance { get; set; } = 80;
		public int yOffset { get; set; } = 4;
		public int scale { get; set; } = 8;
		public bool mulBlending { get; set; } = false;
	}
}
