using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SongArt
{
	public class EnvironmentLight
	{
		public Color LightColor { get; set; }

		public int State { get; set; }

		public EnvironmentLight() {
			LightColor = Color.clear;
			State = 0;
		}

		public void UpdateLight() {
			if (State == 1) {
				LightColor = Color.Lerp(LightColor, Color.clear, Time.deltaTime * 8);
			}
		}
	}
}
