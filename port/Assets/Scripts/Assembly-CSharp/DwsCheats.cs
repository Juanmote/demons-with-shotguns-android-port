using UnityEngine;
using com.mindshaft.overtime;

// Port Android: trucos. Se guardan en PlayerPrefs (entran en la copia de seguridad).
// La invulnerabilidad y las vidas infinitas solo tienen efecto en Fin de los tiempos:
// en los modos versus nadie podría morir y la partida no acabaría.
public static class DwsCheats
{
	public const string AmmoKey = "DwsCheatAmmo";

	public const string InvulnerableKey = "DwsCheatInvulnerable";

	public const string LivesKey = "DwsCheatLives";

	public static readonly string[] Keys = new string[3] { AmmoKey, InvulnerableKey, LivesKey };

	private static int _ammo = -1;

	private static int _invulnerable = -1;

	private static int _lives = -1;

	public static bool InfiniteAmmo
	{
		get { return Get(ref _ammo, AmmoKey); }
		set { Set(ref _ammo, AmmoKey, value); }
	}

	public static bool Invulnerable
	{
		get { return Get(ref _invulnerable, InvulnerableKey); }
		set { Set(ref _invulnerable, InvulnerableKey, value); }
	}

	public static bool InfiniteLives
	{
		get { return Get(ref _lives, LivesKey); }
		set { Set(ref _lives, LivesKey, value); }
	}

	public static bool AnyActive
	{
		get { return InfiniteAmmo || Invulnerable || InfiniteLives; }
	}

	// Comprobación que usa el código del juego (la invulnerabilidad solo vale en Fin de los tiempos).
	public static bool InvulnerableIn(GameMode mode)
	{
		return mode == GameMode.EOT && Invulnerable;
	}

	public static void Reload()
	{
		_ammo = _invulnerable = _lives = -1;
	}

	private static bool Get(ref int cache, string key)
	{
		if (cache < 0)
		{
			cache = PlayerPrefs.GetInt(key, 0) != 0 ? 1 : 0;
		}
		return cache == 1;
	}

	private static void Set(ref int cache, string key, bool value)
	{
		cache = value ? 1 : 0;
		PlayerPrefs.SetInt(key, cache);
		PlayerPrefs.Save();
	}
}
