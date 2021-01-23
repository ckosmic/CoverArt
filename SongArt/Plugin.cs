using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using IPA;
using IPA.Config;
using IPA.Config.Stores;
using UnityEngine.SceneManagement;
using UnityEngine;
using IPALogger = IPA.Logging.Logger;
using BeatSaberMarkupLanguage.Settings;

namespace SongArt
{

	[Plugin(RuntimeOptions.SingleStartInit)]
	public class Plugin
	{
		internal static Plugin Instance { get; private set; }
		internal static IPALogger Log { get; private set; }

		[Init]
		/// <summary>
		/// Called when the plugin is first loaded by IPA (either when the game starts or when the plugin is enabled if it starts disabled).
		/// [Init] methods that use a Constructor or called before regular methods like InitWithConfig.
		/// Only use [Init] with one Constructor.
		/// </summary>
		public void Init(IPALogger logger, Config config) {
			Instance = this;
			Log = logger;

			PluginConfig.Instance = config.Generated<PluginConfig>();
			BSMLSettings.instance.AddSettingsMenu("Cover Art", $"SongArt.Settings.bsml", SettingsController.instance);

			Log.Info("CoverArt initialized.");
		}

		#region BSIPA Config
		//Uncomment to use BSIPA's config
		/*
        [Init]
        public void InitWithConfig(Config conf)
        {
            Configuration.PluginConfig.Instance = conf.Generated<Configuration.PluginConfig>();
            Log.Debug("Config loaded");
        }
        */
		#endregion

		[OnStart]
		public void OnApplicationStart() {
			Log.Debug("OnApplicationStart");
			new GameObject("SongArtController").AddComponent<SongArtController>();

			BS_Utils.Utilities.BSEvents.gameSceneLoaded += SongArtController.Instance.OnGameSceneLoaded;
			BS_Utils.Utilities.BSEvents.LevelFinished += SongArtController.Instance.OnLevelDidFinish;
			BS_Utils.Utilities.BSEvents.levelSelected += SongArtController.Instance.OnLevelSelected;
			BS_Utils.Utilities.BSEvents.beatmapEvent += SongArtController.Instance.OnBeatmapEvent;
		}

		[OnExit]
		public void OnApplicationQuit() {
			Log.Debug("OnApplicationQuit");

			BS_Utils.Utilities.BSEvents.gameSceneLoaded -= SongArtController.Instance.OnGameSceneLoaded;
			BS_Utils.Utilities.BSEvents.LevelFinished -= SongArtController.Instance.OnLevelDidFinish;
			BS_Utils.Utilities.BSEvents.levelSelected -= SongArtController.Instance.OnLevelSelected;
			BS_Utils.Utilities.BSEvents.beatmapEvent -= SongArtController.Instance.OnBeatmapEvent;
		}
	}
}
