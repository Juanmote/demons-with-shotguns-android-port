using System.Collections.Generic;
using UnityEngine;

// Port Android: escribe en dws-diag.txt (a través de DwsActivity) y recoge los errores de Unity,
// para poder ver qué falla en la consola sin cable ni adb.
public static class DwsLog
{
	private const int MaxErrors = 60;

	private static readonly HashSet<string> _seen = new HashSet<string>();

	private static int _errors;

	private static AndroidJavaClass _activity;

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
	private static void Init()
	{
		Application.logMessageReceived += OnLog;
	}

	public static void Write(string line)
	{
		if (Application.platform != RuntimePlatform.Android)
		{
			Debug.Log("[DWS] " + line);
			return;
		}
		try
		{
			if (_activity == null)
			{
				_activity = new AndroidJavaClass("com.juanmote.demonswithshotguns.DwsActivity");
			}
			_activity.CallStatic("log", line);
		}
		catch (System.Exception)
		{
		}
	}

	private static void OnLog(string condition, string stackTrace, LogType type)
	{
		if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert)
		{
			return;
		}
		if (_errors >= MaxErrors || !_seen.Add(condition))
		{
			return;
		}
		_errors++;
		string trace = stackTrace ?? string.Empty;
		if (trace.Length > 600)
		{
			trace = trace.Substring(0, 600);
		}
		Write("Error Unity (" + type + "): " + condition + "\n    " + trace.Replace("\n", "\n    "));
	}
}
