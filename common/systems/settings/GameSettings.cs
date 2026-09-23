using System;
using Core.Utilities.Logging;
using Godot;

namespace Core.Settings;

/// <summary>
/// The player's game options (GDD p.10, p.20): music and sound volume, and screen brightness.
/// Values are applied as soon as they change and saved to disk, so they survive between sessions.
/// </summary>
/// <remarks>
/// Volumes are linear, from 0 (silent) to 1 (full), and are written to the Music and SFX audio
/// buses. Brightness multiplies the final image, from 0.5 (dark) to 1.5 (bright).
/// </remarks>
public static class GameSettings
{
	/// <summary>
	/// Raised whenever a setting changes, so the brightness overlay and any open menu can follow.
	/// </summary>
	public static event Action? Changed;

	/// <summary>Music bus volume, 0 to 1.</summary>
	public static float MusicVolume { get; private set; } = 1f;

	/// <summary>Sound effects bus volume, 0 to 1.</summary>
	public static float SfxVolume { get; private set; } = 1f;

	/// <summary>Screen brightness multiplier, <see cref="MinBrightness"/> to <see cref="MaxBrightness"/>.</summary>
	public static float Brightness { get; private set; } = 1f;

	public const float MinBrightness = 0.5f;
	public const float MaxBrightness = 1.5f;

	public const string MusicBus = "Music";
	public const string SfxBus = "SFX";

	private const string ConfigPath = "user://settings.cfg";
	private const string Section = "options";

	private static bool _loaded;

	/// <summary>
	/// Reads the saved options and applies them. Safe to call more than once.
	/// </summary>
	public static void Load()
	{
		if (_loaded) return;
		_loaded = true;

		ConfigFile config = new();

		if (config.Load(ConfigPath) is Error.Ok)
		{
			MusicVolume = Mathf.Clamp((float)config.GetValue(Section, "music_volume", 1f), 0f, 1f);
			SfxVolume = Mathf.Clamp((float)config.GetValue(Section, "sfx_volume", 1f), 0f, 1f);
			Brightness = Mathf.Clamp((float)config.GetValue(Section, "brightness", 1f), MinBrightness, MaxBrightness);
		}
		else
		{
			LoggerService.Info("No saved options found, using the defaults.");
		}

		ApplyVolume(MusicBus, MusicVolume);
		ApplyVolume(SfxBus, SfxVolume);
		Changed?.Invoke();
	}

	/// <summary>Sets the music volume and saves it.</summary>
	/// <param name="volume">Linear volume from 0 to 1.</param>
	public static void SetMusicVolume(float volume)
	{
		MusicVolume = Mathf.Clamp(volume, 0f, 1f);
		ApplyVolume(MusicBus, MusicVolume);
		SaveAndNotify();
	}

	/// <summary>Sets the sound effects volume and saves it.</summary>
	/// <param name="volume">Linear volume from 0 to 1.</param>
	public static void SetSfxVolume(float volume)
	{
		SfxVolume = Mathf.Clamp(volume, 0f, 1f);
		ApplyVolume(SfxBus, SfxVolume);
		SaveAndNotify();
	}

	/// <summary>Sets the screen brightness and saves it.</summary>
	/// <param name="brightness">Multiplier applied to the final image.</param>
	public static void SetBrightness(float brightness)
	{
		Brightness = Mathf.Clamp(brightness, MinBrightness, MaxBrightness);
		SaveAndNotify();
	}

	private static void SaveAndNotify()
	{
		Save();
		Changed?.Invoke();
	}

	/// <summary>
	/// Writes the current options to disk.
	/// </summary>
	public static void Save()
	{
		ConfigFile config = new();
		config.SetValue(Section, "music_volume", MusicVolume);
		config.SetValue(Section, "sfx_volume", SfxVolume);
		config.SetValue(Section, "brightness", Brightness);

		if (config.Save(ConfigPath) is not Error.Ok)
			LoggerService.Warning($"Could not save the options to {ConfigPath}.");
	}

	/// <summary>
	/// Applies a linear volume to an audio bus, muting it outright at zero.
	/// </summary>
	/// <param name="busName">Name of the bus.</param>
	/// <param name="volume">Linear volume from 0 to 1.</param>
	private static void ApplyVolume(string busName, float volume)
	{
		int bus = AudioServer.GetBusIndex(busName);

		if (bus < 0)
		{
			LoggerService.Warning($"Audio bus <{busName}> not found, volume not applied.");
			return;
		}

		AudioServer.SetBusMute(bus, volume <= 0f);
		AudioServer.SetBusVolumeDb(bus, volume <= 0f ? -80f : Mathf.LinearToDb(volume));
	}
}
