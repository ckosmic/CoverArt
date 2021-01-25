using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using IPA.Utilities;

namespace SongArt
{
	internal class SpectrogramData : MonoBehaviour {
		public static SpectrogramData Instance { get; private set; }

		public BasicSpectrogramData spectrogramData;

		private void Awake() {
			DontDestroyOnLoad(this);
			Instance = this;
		}

		public List<float> GetProcessedSamples() { 
			if(spectrogramData != null)
				return spectrogramData.GetField<List<float>, BasicSpectrogramData>("_processedSamples");
			return null;
		}

		public void GetBasicSpectrumData() {
			StartCoroutine(IEGetBasicSpectrumData());
		}

		IEnumerator IEGetBasicSpectrumData() {
			yield return new WaitUntil(() => Resources.FindObjectsOfTypeAll<BasicSpectrogramData>().Any());
			spectrogramData = Resources.FindObjectsOfTypeAll<BasicSpectrogramData>().FirstOrDefault();
		}
	}
}
