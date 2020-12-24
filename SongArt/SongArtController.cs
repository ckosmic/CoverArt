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
	/// <summary>
	/// Monobehaviours (scripts) are added to GameObjects.
	/// For a full list of Messages a Monobehaviour can receive from the game, see https://docs.unity3d.com/ScriptReference/MonoBehaviour.html.
	/// </summary>
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

		// These methods are automatically called by Unity, you should remove any you aren't using.
		#region Monobehaviour Messages
		/// <summary>
		/// Only ever called once, mainly used to initialize variables.
		/// </summary>
		private void Awake() {
			// For this particular MonoBehaviour, we only want one instance to exist at any time, so store a reference to it in a static property
			//   and destroy any that are created while one already exists.
			if (Instance != null) {
				Plugin.Log?.Warn($"Instance of {GetType().Name} already exists, destroying.");
				GameObject.DestroyImmediate(this);
				return;
			}
			GameObject.DontDestroyOnLoad(this); // Don't destroy this object on scene changes
			Instance = this;
			Plugin.Log?.Debug($"{name}: Awake()");
		}
		/// <summary>
		/// Only ever called once on the first frame the script is Enabled. Start is called after any other script's Awake() and before Update().
		/// </summary>
		private void Start() {
			AssetBundle bundle = AssetBundleManager.LoadAssetBundleFromResource($"SongArt.songart");
			coverShader = bundle.LoadAsset<Shader>("Assets/Shaders/CoverArt.shader");
		}

		/// <summary>
		/// Called every frame if the script is enabled.
		/// </summary>
		private void Update() {
			if (coverQuad != null) {
				// Perform fading
				if (PluginConfig.Instance.fadeEnabled && fadeTimer > 0 && Time.time >= fadeTimer) {
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

				// Spaghet... it works tho
				// Averages the light colors and tints the cover art that color
				// It's a close enough approximation to the ambient color of the scene
				Vector4 values = Vector4.zero;
				foreach (EnvironmentLight light in lights) {
					light.UpdateLight();
					values.x += light.LightColor.r;
					values.y += light.LightColor.g;
					values.z += light.LightColor.b;
					values.w += light.LightColor.a;
				}
				values.x /= lights.Length;
				values.y /= lights.Length;
				values.z /= lights.Length;
				values.w /= lights.Length;
				values.w *= 100;
				values.w *= values.w;
				values.w /= 100;
				values.w = Mathf.Clamp01(values.w);
				Color tintColor = new Color(values.x, values.y, values.z, values.w);
				coverMaterial.SetColor("_TintColor", tintColor);
			}
		}

		/// <summary>
		/// Called every frame after every other enabled script's Update().
		/// </summary>
		private void LateUpdate() {

		}

		/// <summary>
		/// Called when the script becomes enabled and active
		/// </summary>
		private void OnEnable() {
			
		}

		/// <summary>
		/// Called when the script becomes disabled or when it is being destroyed.
		/// </summary>
		private void OnDisable() {

		}

		/// <summary>
		/// Called when the script is being destroyed.
		/// </summary>
		private void OnDestroy() {
			Plugin.Log?.Debug($"{name}: OnDestroy()");
			if (Instance == this)
				Instance = null; // This MonoBehaviour is being destroyed, so set the static instance property to null.

		}
		#endregion

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
			if(PluginConfig.Instance.mulBlending)
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
					case 3: // Light turns on to blue and fades out
						lights[lightId].State = 1;
						lights[lightId].LightColor = envColor0;
						break;
					case 5: // Light turns on to red
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
