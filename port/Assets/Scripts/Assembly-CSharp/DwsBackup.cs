using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using com.mindshaft.overtime;

// Port Android: copia de seguridad de partidas y perfil en un solo archivo de texto.
// Incluye las opciones, controles, filtros y trucos (PlayerPrefs), los archivos de partida (avance de Fin de los tiempos),
// los logros y estadísticas (dws-logros.txt) y los trucos. El archivo lo guarda/abre Android (DwsActivity).
public static class DwsBackup
{
	private const string Header = "DWS-COPIA 1";

	private static readonly string[] StringKeys = new string[2] { "KeyboardBindings", "GamepadBindings" };

	private static readonly string[] ExtraIntKeys = new string[1] { "DwsLanguage" };

	private static AndroidJavaClass Activity
	{
		get { return new AndroidJavaClass("com.juanmote.demonswithshotguns.DwsActivity"); }
	}

	public static void StartExport()
	{
		string content = Build();
		string name = "DemonsWithShotguns_copia_" + DateTime.Now.ToString("yyyy-MM-dd") + ".dws";
		if (Application.platform != RuntimePlatform.Android)
		{
			File.WriteAllText(Path.Combine(Application.persistentDataPath, name), content);
			return;
		}
		try
		{
			Activity.CallStatic("exportBackup", content, name);
		}
		catch (Exception e)
		{
			DwsLog.Write("Exportar: " + e);
		}
	}

	public static void StartImport()
	{
		if (Application.platform != RuntimePlatform.Android)
		{
			return;
		}
		try
		{
			Activity.CallStatic("importBackup");
		}
		catch (Exception e)
		{
			DwsLog.Write("Importar: " + e);
		}
	}

	private static List<string> IntKeys()
	{
		List<string> keys = new List<string>();
		foreach (PlayerPrefsKeys key in Enum.GetValues(typeof(PlayerPrefsKeys)))
		{
			keys.Add(key.ToString());
		}
		keys.AddRange(ExtraIntKeys);
		keys.AddRange(DwsCheats.Keys);
		keys.Add(DwsFilters.FilterKey);
		keys.Add(DwsFilters.ColorsKey);
		keys.Add(DwsTouchControls.SizeKey);
		return keys;
	}

	private static string B64(string s)
	{
		return Convert.ToBase64String(Encoding.UTF8.GetBytes(s));
	}

	private static string UnB64(string s)
	{
		return Encoding.UTF8.GetString(Convert.FromBase64String(s));
	}

	private static string Build()
	{
		StringBuilder sb = new StringBuilder();
		sb.Append(Header).Append('\n');
		sb.Append("# ").Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm")).Append('\n');
		foreach (string key in IntKeys())
		{
			if (PlayerPrefs.HasKey(key))
			{
				sb.Append("P i ").Append(key).Append(' ').Append(PlayerPrefs.GetInt(key)).Append('\n');
			}
		}
		foreach (string key in StringKeys)
		{
			if (PlayerPrefs.HasKey(key))
			{
				sb.Append("P s ").Append(key).Append(' ').Append(B64(PlayerPrefs.GetString(key))).Append('\n');
			}
		}
		string root = Application.persistentDataPath;
		int files = 0;
		foreach (string path in Directory.GetFiles(root, "*", SearchOption.AllDirectories))
		{
			string rel = path.Substring(root.Length).TrimStart('/', '\\').Replace('\\', '/');
			if (!Include(rel))
			{
				continue;
			}
			sb.Append("F ").Append(B64(rel)).Append(' ').Append(Convert.ToBase64String(File.ReadAllBytes(path))).Append('\n');
			files++;
		}
		DwsLog.Write("Copia creada: " + files + " archivos");
		return sb.ToString();
	}

	private static bool Include(string rel)
	{
		if (rel.StartsWith("Unity/") || rel.StartsWith("il2cpp/") || rel.EndsWith(".tmp") || rel.EndsWith(".temp"))
		{
			return false;
		}
		return rel != "dws-diag.txt";
	}

	// Devuelve el mensaje para el usuario. Si todo va bien, reinicia el juego.
	public static string Import(string content)
	{
		bool es = Loc.Spanish;
		if (string.IsNullOrEmpty(content))
		{
			return es ? "No se ha importado nada." : "Nothing was imported.";
		}
		string[] lines = content.Replace("\r", string.Empty).Split('\n');
		if (lines.Length == 0 || lines[0].Trim() != Header)
		{
			return es ? "Ese archivo no es una copia de Demons with Shotguns." : "That file is not a Demons with Shotguns backup.";
		}
		try
		{
			string root = Application.persistentDataPath;
			int files = 0;
			foreach (string raw in lines)
			{
				string[] p = raw.Split(' ');
				if (p.Length == 4 && p[0] == "P" && p[1] == "i")
				{
					int v;
					if (int.TryParse(p[3], out v))
					{
						PlayerPrefs.SetInt(p[2], v);
					}
				}
				else if (p.Length == 4 && p[0] == "P" && p[1] == "s")
				{
					PlayerPrefs.SetString(p[2], UnB64(p[3]));
				}
				else if (p.Length == 3 && p[0] == "F")
				{
					string rel = UnB64(p[1]);
					if (rel.Contains("..") || !Include(rel))
					{
						continue;
					}
					string dest = Path.Combine(root, rel);
					Directory.CreateDirectory(Path.GetDirectoryName(dest));
					File.WriteAllBytes(dest, Convert.FromBase64String(p[2]));
					files++;
				}
			}
			PlayerPrefs.Save();
			DwsAchievements.Reload();
			DwsCheats.Reload();
			DwsFilters.Reload();
			DwsLog.Write("Copia importada: " + files + " archivos. Reiniciando.");
			if (Application.platform == RuntimePlatform.Android)
			{
				Activity.CallStatic("restartApp");
			}
			return es ? "Copia importada. Reiniciando el juego..." : "Backup imported. Restarting the game...";
		}
		catch (Exception e)
		{
			DwsLog.Write("Importar: " + e);
			return (es ? "No se pudo importar: " : "Import failed: ") + e.Message;
		}
	}
}
