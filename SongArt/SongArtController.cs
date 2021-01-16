using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SongArt
{
	public class SongArtController : MonoBehaviour {
		public static SongArtController Instance { get; private set; }

		private GameObject coverQuad;
		private Texture2D coverTexture;
		private Material coverMaterial;
		private float fadeTimer;
		private Shader coverShader;
		private Color envColor0;
		private Color envColor1;
		private EnvironmentLight[] lights;

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
			coverShader = bundle.LoadAsset<Shader>("Assets/Shaders/CoverArt.shader");
		}

		private void Update() {
			if (coverQuad != null) {
				// Perform fading
				if (fadeTimer > 0 && Time.time >= fadeTimer) {
					if (PluginConfig.Instance.fadeEnabled) {
						Color currentColor = coverMaterial.GetColor("_Color");
						Color transparentColor = coverMaterial.GetColor("_Color");
						transparentColor.a = 0;
						if (currentColor.a <= 0) {
							fadeTimer = -1;
							currentColor.a = 0;
							CleanUpCover();
						}
						coverMaterial.SetColor("_Color", Color.Lerp(currentColor, transparentColor, Time.deltaTime * 3));
					}
				}
				float timeDiff = fadeTimer - Time.time;
				timeDiff = Mathf.Clamp01(timeDiff);

				// Spaghet... it works tho
				// Averages the light colors and tints the cover art that color
				// It's a close enough approximation to the ambient color of the scene
				Vector4 values = Vector4.zero;
				int lightCount = 0;
				foreach (EnvironmentLight light in lights) {
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
				values.w /= lights.Length;
				values.w *= 100;
				values.w *= values.w;
				values.w /= 100;
				values.w = Mathf.Clamp01(values.w);

				Color tintColor = new Color(values.x, values.y, values.z, values.w + timeDiff);
				coverMaterial.SetColor("_TintColor", tintColor);
			}
		}

		private void OnDestroy() {
			Plugin.Log?.Debug($"{name}: OnDestroy()");
			if (Instance == this)
				Instance = null; // This MonoBehaviour is being destroyed, so set the static instance property to null.

		}

		public void OnGameSceneLoaded() {
			fadeTimer = Time.time + PluginConfig.Instance.fadeDelay;

			// Create cover image quad
			coverQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
			coverQuad.transform.position = new Vector3(0, PluginConfig.Instance.yOffset, PluginConfig.Instance.distance);
			coverQuad.transform.localScale = Vector3.one * PluginConfig.Instance.scale;
			coverQuad.transform.localEulerAngles = new Vector3(0, 0, 0);
			coverQuad.layer = 13;

			// Create cover material using the CoverArt shader and assign it to the cover quad
			MeshRenderer coverRenderer = coverQuad.GetComponent<MeshRenderer>();
			coverMaterial = new Material(coverShader);
			coverMaterial.SetTexture("_MainTex", coverTexture);
			coverMaterial.SetColor("_Color", new Color(1, 1, 1, PluginConfig.Instance.transparency));
			coverMaterial.SetFloat("_Bloom", 0.2f);
			if (PluginConfig.Instance.mulBlending)
				coverMaterial.EnableKeyword("MULTIPLICATIVE_BLENDING");
			coverRenderer.material = coverMaterial;

			// Get environment colors
			SimpleColorSO[] simpleColors = Resources.FindObjectsOfTypeAll<SimpleColorSO>();
			for (int i = 0; i < simpleColors.Length; i++) {
				string name = simpleColors[i].name;
				if (name == "EnvironmentColor0") {
					envColor0 = simpleColors[i].color;
				} else if (name == "EnvironmentColor1") {
					envColor1 = simpleColors[i].color;
				}
			}

			InitializeLights();
		}

		public void OnLevelDidFinish(StandardLevelScenesTransitionSetupDataSO scene, LevelCompletionResults result) {
			if (coverQuad != null) {
				CleanUpCover();
			}
		}

		public async void OnLevelSelected(LevelCollectionViewController levelCollectionViewController, IPreviewBeatmapLevel previewBeatmapLevel) {
			System.Threading.CancellationToken cancellationToken;
			Task<Sprite> coverImageTask = previewBeatmapLevel.GetCoverImageAsync(cancellationToken);

			Sprite coverSprite = await coverImageTask;
			coverTexture = coverSprite.texture;
		}

		public void OnBeatmapEvent(BeatmapEventData data) {
			int lightId = (int)data.type;
			if (lightId >= 0 && lightId <= 5) {
				switch (data.value) {
					case 0: // Light turns off
						lights[lightId].State = 0;
						lights[lightId].LightColor = Color.clear;
						break;
					case 1: // Light turns on to blue
						lights[lightId].State = 0;
						lights[lightId].LightColor = envColor0;
						break;
					case 2: // Light flashes blue, just repeat case 1
						lights[lightId].State = 0;
						lights[lightId].LightColor = envColor0;
						break;
					case 3: // Light turns on to blue and fades out
						lights[lightId].State = 1;
						lights[lightId].LightColor = envColor0;
						break;
					case 5: // Light turns on to red
						lights[lightId].State = 0;
						lights[lightId].LightColor = envColor1;
						break;
					case 6: // Light flashes red, just repeat case 5
						lights[lightId].State = 0;
						lights[lightId].LightColor = envColor1;
						break;
					case 7: // Light turns on to red and fades out
						lights[lightId].State = 1;
						lights[lightId].LightColor = envColor1;
						break;
				}
			}
		}

		public void InitializeLights() {
			lights = new EnvironmentLight[6];
			for (int i = 0; i < lights.Length; i++) {
				lights[i] = new EnvironmentLight();
			}
		}

		private void CleanUpCover() {
			Destroy(coverMaterial);
			Destroy(coverQuad);
			coverQuad = null;
			coverMaterial = null;
		}
	}
}
