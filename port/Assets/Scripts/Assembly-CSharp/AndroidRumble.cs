using System;
using System.Collections.Generic;
using UnityEngine;

// Port Android: vibración real. El trabajo lo hace DwsActivity.java (Plugins/Android).
public static class AndroidRumble
{
	private static AndroidJavaClass _activity;

	private static readonly Dictionary<int, float> BusyUntil = new Dictionary<int, float>();

	// Evento de partida: respeta la opción del juego y no corta una vibración más larga en curso.
	public static void Event(InControl.InputDevice device, float seconds, float intensity)
	{
		if (PlayerPrefs.GetInt(com.mindshaft.overtime.PlayerPrefsKeys.VibrationOn.ToString(), 1) == 0) return;
		int key = device != null ? device.GetHashCode() : 0;
		float now = Time.unscaledTime;
		float until;
		if (BusyUntil.TryGetValue(key, out until) && now + seconds < until) return;
		BusyUntil[key] = now + seconds;
		Vibrate(device, seconds, intensity);
	}

	// Explosión: vibra para cada jugador cercano y para quien la provocó.
	public static void Explosion(Vector3 position, GameObject wielder)
	{
		GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
		for (int i = 0; i < players.Length; i++)
		{
			com.mindshaft.overtime.view.PlayerView view = players[i].GetComponent<com.mindshaft.overtime.view.PlayerView>();
			if (view == null || view.IsClone) continue;
			if (players[i] != wielder && Vector3.Distance(players[i].transform.position, position) > 12f) continue;
			Event(view.GamePlayerActions != null ? view.GamePlayerActions.Device : null, 0.4f, 1f);
		}
	}

	public static void Vibrate(InControl.InputDevice device, float seconds, float intensity)
	{
		if (Application.platform != RuntimePlatform.Android || intensity <= 0f) return;
		try
		{
			long ms = (long)Mathf.Clamp(seconds * 1000f, 60f, 2000f);
			// Por debajo de cierta fuerza algunos motores de consola ni siquiera arrancan.
			int amp = Mathf.Clamp(Mathf.RoundToInt(180f + 75f * intensity), 1, 255);
			string padName = string.Empty;
			int sameNameBefore = 0;
			InControl.UnityInputDevice ud = device as InControl.UnityInputDevice;
			string[] names = Input.GetJoystickNames();
			if (ud != null && ud.JoystickId >= 1 && ud.JoystickId <= names.Length)
			{
				padName = names[ud.JoystickId - 1];
				for (int i = 0; i < ud.JoystickId - 1; i++) if (names[i] == padName) sameNameBefore++;
			}
			if (_activity == null) _activity = new AndroidJavaClass("com.juanmote.demonswithshotguns.DwsActivity");
			_activity.CallStatic("rumble", padName, sameNameBefore, ms, amp);
		}
		catch (Exception e)
		{
			Debug.LogWarning("[DWS] Vibración: " + e.Message);
		}
	}
}
