using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using com.mindshaft.overtime.model;

// Port Android: logros y estadísticas guardados en la consola (el juego original los guardaba en Steam).
// Las condiciones las sigue comprobando el código original (MonitorAchievementsCommand).
// Se guardan en files/dws-logros.txt, que entra en la copia de seguridad.
public static class DwsAchievements
{
	// Con algún truco activo no se consiguen logros ni avanzan sus contadores.
	public const bool CheatsBlockAchievements = true;

	private static bool Blocked
	{
		get { return CheatsBlockAchievements && DwsCheats.AnyActive; }
	}

	// Cómo se mide el progreso de un logro (para el contador de la lista).
	public enum Progress
	{
		None,
		Total,   // suma de todas las partidas (estadística guardada)
		InMatch  // en una sola partida: se enseña el mejor registro
	}

	public class Def
	{
		public Progress Kind;
		public int Target;
		public string Stat;
		public Achievement Id;
		public string NameEn;
		public string NameEs;
		public string DescEn;
		public string DescEs;
		public bool Hidden;

		public Def(Achievement id, string nameEn, string nameEs, string descEn, string descEs, bool hidden)
		{
			Id = id;
			NameEn = nameEn;
			NameEs = nameEs;
			DescEn = descEn;
			DescEs = descEs;
			Hidden = hidden;
		}

		public Def Total(string stat, int target)
		{
			Kind = Progress.Total;
			Stat = stat;
			Target = target;
			return this;
		}

		public Def Best(int target)
		{
			Kind = Progress.InMatch;
			Stat = "Mejor_" + Id;
			Target = target;
			return this;
		}

		public string Name
		{
			get { return Loc.Spanish ? NameEs : NameEn; }
		}

		public string Desc
		{
			get { return Loc.Spanish ? DescEs : DescEn; }
		}
	}

	// Nombres de Steam; las descripciones explican la condición real (sacada del código del juego).
	public static readonly Def[] All = new Def[]
	{
		new Def(Achievement.ACH_OLD_TESTAMENT, "Old Testament", "Antiguo Testamento", "Play your first match, in any mode.", "Juega tu primera partida, en cualquier modo.", true),
		new Def(Achievement.ACH_LADY_LUCK, "Lady Luck", "La dama de la suerte", "Pick up 10 tarot cards (the card-shaped power-ups) in one match. Turn tarot cards on in the match settings of the versus modes.", "Recoge 10 cartas del tarot (los potenciadores con forma de carta) en una misma partida. En los modos versus se activan en los ajustes de partida.", false).Best(10),
		new Def(Achievement.ACH_SHOOTING_BLANKS, "Shooting Blanks", "Disparos de fogueo", "Pull the trigger with an empty shotgun 20 times in one match: when you run out of shells, keep pressing shoot.", "Aprieta el gatillo con la escopeta vacía 20 veces en una misma partida: cuando te quedes sin cartuchos, sigue pulsando disparar.", false).Best(20),
		new Def(Achievement.ACH_WING_ENVY, "Wing Envy", "Envidia de alas", "Jump 100 times in one match. Every jump counts.", "Salta 100 veces en una misma partida. Cuenta cada salto.", false).Best(100),
		new Def(Achievement.ACH_BORN_EVIL, "Born Evil", "Nacido del mal", "Get 12 kills in a row without dying in one match (Antichrist streak: King at 3, King of Kings at 6, Messiah at 9).", "Encadena 12 muertes seguidas sin morir en una partida (racha Anticristo: Rey a las 3, Rey de reyes a las 6, Mesías a las 9).", false).Best(12),
		new Def(Achievement.ACH_SOUL_COLLECTOR, "Soul Collector", "Coleccionista de almas", "Collect 500 souls in total, adding up all your matches (soul modes and End of Times).", "Recoge 500 almas en total, sumando todas tus partidas (modos de almas y Fin de los tiempos).", false).Total("SoulsCaptured", 500),
		new Def(Achievement.ACH_FLAWLESS_VICTORY, "Flawless Victory", "Victoria impecable", "Finish a versus match without dying even once.", "Termina una partida versus sin morir ni una sola vez.", false),
		new Def(Achievement.ACH_ONE_FOR_EACH_OF_YEA, "One For Each of Ya", "Uno para cada uno", "Get 5 Double Kills in total: kill two players less than 3 seconds apart.", "Consigue 5 dobles muertes en total: mata a dos jugadores con menos de 3 segundos entre una muerte y otra.", false).Total("DoubleKills", 5),
		new Def(Achievement.ACH_SWEATING_BLOOD, "Sweating Blood", "Sudando sangre", "Get 3 Monster Kills in total: kill 5 players in a row with less than 3 seconds between each kill. Easier with 4 or more players.", "Consigue 3 muertes monstruosas en total: mata a 5 jugadores seguidos con menos de 3 segundos entre cada muerte. Es más fácil con 4 jugadores o más.", false).Total("MonsterKills", 3),
		new Def(Achievement.ACH_SUICIDE, "How Emo", "Qué emo", "Kill yourself 10 times in one match (for example, with your own grenade).", "Mátate tú mismo 10 veces en una misma partida (por ejemplo, con tu propia granada).", false).Best(10),
		new Def(Achievement.ACH_AHOLE, "A' Hole", "Capullo", "Taunt 5 times in one match (taunt button, standing still).", "Haz 5 burlas en una misma partida (botón de burla, estando quieto).", false).Best(5),
		new Def(Achievement.ACH_EOT_CEMETERY, "Ghosts n' Nomeds", "Fantasmas y Nomeds", "Clear the first area of End of Times (cemetery).", "Supera la primera zona de Fin de los tiempos (el cementerio).", false),
		new Def(Achievement.ACH_EOT_METRO, "Water Tower Terror", "Terror en el depósito", "Clear the second area of End of Times (water tower).", "Supera la segunda zona de Fin de los tiempos (el depósito de agua).", true),
		new Def(Achievement.ACH_EOT_CHURCH, "Sacrament", "Sacramento", "Clear the third area of End of Times (church).", "Supera la tercera zona de Fin de los tiempos (la iglesia).", true),
		new Def(Achievement.ACH_EOT_HELL, "At The Gates", "A las puertas", "Clear the fourth area of End of Times (the gates of hell).", "Supera la cuarta zona de Fin de los tiempos (las puertas del infierno).", true),
		new Def(Achievement.ACH_EOT_EOT, "End of Times", "Fin de los tiempos", "Beat the final boss and complete End of Times.", "Vence al jefe final y completa Fin de los tiempos.", true)
	};

	public const string FileName = "dws-logros.txt";

	private static Dictionary<string, long> _unlocked;

	private static Dictionary<string, int> _stats;

	private static readonly Dictionary<Achievement, Texture2D> _icons = new Dictionary<Achievement, Texture2D>();

	private static readonly Dictionary<Achievement, Texture2D> _iconsOff = new Dictionary<Achievement, Texture2D>();

	public static string FilePath
	{
		get { return Path.Combine(Application.persistentDataPath, FileName); }
	}

	public static Def Get(Achievement id)
	{
		foreach (Def def in All)
		{
			if (def.Id == id)
			{
				return def;
			}
		}
		return null;
	}

	public static int UnlockedCount
	{
		get
		{
			Load();
			int n = 0;
			foreach (Def def in All)
			{
				if (_unlocked.ContainsKey(def.Id.ToString()))
				{
					n++;
				}
			}
			return n;
		}
	}

	public static bool IsUnlocked(Achievement id)
	{
		Load();
		return _unlocked.ContainsKey(id.ToString());
	}

	public static DateTime UnlockDate(Achievement id)
	{
		Load();
		long t;
		if (_unlocked.TryGetValue(id.ToString(), out t))
		{
			return DateTimeOffset.FromUnixTimeSeconds(t).LocalDateTime;
		}
		return DateTime.MinValue;
	}

	// Lo llama el código del juego cuando se cumple la condición. Devuelve false si no se ha concedido.
	public static bool Unlock(Achievement id)
	{
		Load();
		if (_unlocked.ContainsKey(id.ToString()))
		{
			return true;
		}
		if (Blocked)
		{
			return false;
		}
		_unlocked[id.ToString()] = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
		Save();
		DwsLog.Write("Logro conseguido: " + id);
		DwsOverlay.ShowAchievement(id);
		return true;
	}

	// Texto del contador, p. ej. "37/500" o "Mejor partida: 63/100". Vacío si no aplica o ya está conseguido.
	public static string ProgressText(Def def)
	{
		if (def.Kind == Progress.None || IsUnlocked(def.Id))
		{
			return string.Empty;
		}
		int v = Mathf.Min(GetStat(def.Stat), def.Target);
		if (def.Kind == Progress.Total)
		{
			return v + "/" + def.Target;
		}
		return (Loc.Spanish ? "Mejor partida: " : "Best match: ") + v + "/" + def.Target;
	}

	public static float ProgressFraction(Def def)
	{
		if (def.Kind == Progress.None || def.Target <= 0)
		{
			return 0f;
		}
		return IsUnlocked(def.Id) ? 1f : Mathf.Clamp01((float)GetStat(def.Stat) / def.Target);
	}

	private static bool _dirty;

	// Mejor marca en una partida. Se guarda en memoria y se escribe al acabar la partida (Flush).
	public static void RecordBest(Achievement id, int value)
	{
		if (Blocked)
		{
			return;
		}
		Load();
		string key = "Mejor_" + id;
		int old;
		if (value > 0 && (!_stats.TryGetValue(key, out old) || value > old))
		{
			_stats[key] = value;
			_dirty = true;
		}
	}

	public static void Flush()
	{
		if (_dirty)
		{
			_dirty = false;
			Save();
		}
	}

	public static int GetStat(string name)
	{
		Load();
		int v;
		return _stats.TryGetValue(name, out v) ? v : 0;
	}

	public static void SetStat(string name, int value)
	{
		if (Blocked)
		{
			return;
		}
		Load();
		_stats[name] = value;
		Save();
	}

	public static Texture2D Icon(Achievement id, bool unlocked)
	{
		Dictionary<Achievement, Texture2D> cache = unlocked ? _icons : _iconsOff;
		Texture2D tex;
		if (!cache.TryGetValue(id, out tex))
		{
			tex = Resources.Load<Texture2D>("achievements/" + id + (unlocked ? string.Empty : "_off"));
			cache[id] = tex;
		}
		return tex;
	}

	// Vuelve a leer el archivo (después de importar una copia).
	public static void Reload()
	{
		_unlocked = null;
		_stats = null;
		Load();
	}

	private static void Load()
	{
		if (_unlocked != null)
		{
			return;
		}
		_unlocked = new Dictionary<string, long>();
		_stats = new Dictionary<string, int>();
		try
		{
			if (!File.Exists(FilePath))
			{
				return;
			}
			foreach (string raw in File.ReadAllLines(FilePath))
			{
				string[] parts = raw.Trim().Split(' ');
				if (parts.Length != 3)
				{
					continue;
				}
				if (parts[0] == "logro")
				{
					long t;
					if (long.TryParse(parts[2], out t))
					{
						_unlocked[parts[1]] = t;
					}
				}
				else if (parts[0] == "stat")
				{
					int v;
					if (int.TryParse(parts[2], out v))
					{
						_stats[parts[1]] = v;
					}
				}
			}
		}
		catch (Exception e)
		{
			DwsLog.Write("No se pudieron leer los logros: " + e.Message);
		}
	}

	private static void Save()
	{
		try
		{
			List<string> lines = new List<string>();
			foreach (KeyValuePair<string, long> pair in _unlocked)
			{
				lines.Add("logro " + pair.Key + " " + pair.Value);
			}
			foreach (KeyValuePair<string, int> pair in _stats)
			{
				lines.Add("stat " + pair.Key + " " + pair.Value);
			}
			string tmp = FilePath + ".tmp";
			File.WriteAllLines(tmp, lines.ToArray());
			if (File.Exists(FilePath))
			{
				File.Delete(FilePath);
			}
			File.Move(tmp, FilePath);
		}
		catch (Exception e)
		{
			DwsLog.Write("No se pudieron guardar los logros: " + e.Message);
		}
	}
}
