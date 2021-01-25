using BS_Utils.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using IPA.Utilities;

namespace SongArt
{
	public class SongArtController : MonoBehaviour {
		public static SongArtController Instance { get; private set; }

		private GameObject _coverQuad;
		private Texture2D _coverTexture;
		private Material _coverMaterial;
		private float _fadeTimer;
		private Shader _coverShader;
		private Color _envColor0;
		private Color _envColor1;
		private EnvironmentLight[] _lights;

		private void Awake() {
			if (Instance != null) {
				Plugin.Log?.Warn($"Instance of {GetType().Name} already exists, destroying.");
				GameObject.DestroyImmediate(this);
				return;
			}
			GameObject.DontDestroyOnLoad(this); // Don't destroy this object on scene changes
			Instance = this;
			Plugin.Log?.Debug($"{name}: Awake()");
		}

		private void Start() {
			AssetBundle bundle = AssetBundleManager.LoadAssetBundleFromResource($"SongArt.songart");
			_coverShader = bundle.LoadAsset<Shader>("Assets/Shaders/CoverArt.shader");
		}

		private void Update() {
			if (_coverQuad != null) {
				// Perform fading
				if (_fadeTimer > 0 && Time.time >= _fadeTimer && PluginConfig.Instance.fadeEnabled) {
					StartCoroutine(FadeOutCover());
					_fadeTimer = -1;
				}

				float timeDiff = _fadeTimer - Time.time;
				timeDiff = Mathf.Clamp01(timeDiff);

				// Spaghet... it works tho
				// Averages the light colors and tints the cover art that color
				// It's a close enough approximation to the ambient color of the scene
				Vector4 values = Vector4.zero;
				int lightCount = 0;
				foreach (EnvironmentLight light in _lights) {
					light.UpdateLight();
					if (light.LightColor.a > 0.5f) {
						values.x += light.LightColor.r;
						values.y += light.LightColor.g;
						values.z += light.LightColor.b;
						lightCount++;
					}
					values.w += light.LightColor.a;
				}
				if (lightCount > 0) {
					values.x /= lightCount;
					values.y /= lightCount;
					values.z /= lightCount;
				}
				values.w /= _lights.Length;
				values.w *= 100;
				values.w *= values.w;
				values.w /= 100;
				values.w = Mathf.Clamp01(values.w + timeDiff);

				Color tintColor = new Color(values.x, values.y, values.z, values.w);
				_coverMaterial.SetColor("_TintColor", tintColor);

				if (PluginConfig.Instance.reactEnabled) {
					List<float> processedSamples = SpectrogramData.Instance.GetProcessedSamples();
					if (processedSamples != null) {
						_coverQuad.transform.localScale = Vector3.one * (PluginConfig.Instance.scale + Mathf.Sqrt(processedSamples[5] * 50));
					}
				}
			}
		}

		private void OnDestroy() {
			Plugin.Log?.Debug($"{name}: OnDestroy()");
			if (Instance == this)
				Instance = null; // This MonoBehaviour is being destroyed, so set the static instance property to null.

		}

		public void OnGameSceneLoaded() {
			_fadeTimer = Time.time + PluginConfig.Instance.fadeDelay;

			// Create cover image quad
			_coverQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
			_coverQuad.transform.position = new Vector3(0, PluginConfig.Instance.yOffset, PluginConfig.Instance.distance);
			_coverQuad.transform.localScale = Vector3.one * PluginConfig.Instance.scale;
			_coverQuad.transform.localEulerAngles = new Vector3(0, 0, 0);
			_coverQuad.layer = 13;

			// Create cover material using the CoverArt shader and assign it to the cover quad
			MeshRenderer coverRenderer = _coverQuad.GetComponent<MeshRenderer>();
			_coverMaterial = new Material(_coverShader);
			_coverMaterial.SetTexture("_MainTex", _coverTexture);
			_coverMaterial.SetColor("_Color", new Color(1, 1, 1, PluginConfig.Instance.transparency));
			_coverMaterial.SetFloat("_Bloom", 0.2f);
			if (PluginConfig.Instance.mulBlending)
				_coverMaterial.EnableKeyword("MULTIPLICATIVE_BLENDING");
			coverRenderer.material = _coverMaterial;

			// Get environment colors
			SimpleColorSO[] simpleColors = Resources.FindObjectsOfTypeAll<SimpleColorSO>();
			for (int i = 0; i < simpleColors.Length; i++) {
				string name = simpleColors[i].name;
				if (name == "EnvironmentColor0") {
					_envColor0 = simpleColors[i].color;
				} else if (name == "EnvironmentColor1") {
					_envColor1 = simpleColors[i].color;
				}
			}

			InitializeLights();

			if(PluginConfig.Instance.reactEnabled)
				SpectrogramData.Instance.GetBasicSpectrumData();
		}

		public void OnLevelDidFinish(object sender, LevelFinishedEventArgs args) {
			if (_coverQuad != null) {
				StartCoroutine(FadeOutCover());
			}
		}

		public async void OnLevelSelected(LevelCollectionViewController levelCollectionViewController, IPreviewBeatmapLevel previewBeatmapLevel) {
			System.Threading.CancellationToken cancellationToken;
			Task<Sprite> coverImageTask = previewBeatmapLevel.GetCoverImageAsync(cancellationToken);

			Sprite coverSprite = await coverImageTask;
			_coverTexture = coverSprite.texture;
		}

		public void OnBeatmapEvent(BeatmapEventData data) {
			int lightId = (int)data.type;
			if (lightId >= 0 && lightId <= 5) {
				switch (data.value) {
					case 0: // Light turns off
						_lights[lightId].State = 0;
						_lights[lightId].LightColor = Color.clear;
						break;
					case 1: // Light turns on to blue
						_lights[lightId].State = 0;
						_lights[lightId].LightColor = _envColor0;
						break;
					case 2: // Light flashes blue, just repeat case 1
						_lights[lightId].State = 0;
						_lights[lightId].LightColor = _envColor0;
						break;
					case 3: // Light turns on to blue and fades out
						_lights[lightId].State = 1;
						_lights[lightId].LightColor = _envColor0;
						break;
					case 5: // Light turns on to red
						_lights[lightId].State = 0;
						_lights[lightId].LightColor = _envColor1;
						break;
					case 6: // Light flashes red, just repeat case 5
						_lights[lightId].State = 0;
						_lights[lightId].LightColor = _envColor1;
						break;
					case 7: // Light turns on to red and fades out
						_lights[lightId].State = 1;
						_lights[lightId].LightColor = _envColor1;
						break;
				}
			}
		}

		public void OnLevelFailed(StandardLevelScenesTransitionSetupDataSO so, LevelCompletionResults results) {
			if (_coverQuad != null) {
				StartCoroutine(FadeOutCover());
			}
		}

		public void InitializeLights() {
			_lights = new EnvironmentLight[6];
			for (int i = 0; i < _lights.Length; i++) {
				_lights[i] = new EnvironmentLight();
			}
		}

		private void CleanUpCover() {
			Destroy(_coverMaterial);
			Destroy(_coverQuad);
			_coverQuad = null;
			_coverMaterial = null;
		}

		IEnumerator FadeOutCover() {
			Color currentColor = _coverMaterial.GetColor("_Color");
			Color transparentColor = _coverMaterial.GetColor("_Color");
			transparentColor.a = 0;
			while (currentColor.a > 0.01f) {
				currentColor = Color.Lerp(currentColor, transparentColor, Time.deltaTime * 3);
				_coverMaterial.SetColor("_Color", currentColor);
				yield return new WaitForEndOfFrame();
			}
			_fadeTimer = -1;
			CleanUpCover();
		}
	}
}
