using System;
using System.Collections.Generic;
using InControl;
using UnityEngine;
using com.mindshaft.overtime.model;

// Port Android: capa propia sobre el juego.
// - Aviso de logro conseguido.
// - Menú "Extras del port" (Logros, Imagen, Controles táctiles, Trucos, Copia de seguridad), que se abre desde Opciones.
// El texto se dibuja con la fuente del juego (DwsFont).
// Se maneja con cualquier mando (cruceta/stick, A acepta, B vuelve), con teclado o tocando la pantalla.
public class DwsOverlay : MonoBehaviour
{
	private enum Page
	{
		Main,
		Achievements,
		Image,
		Touch,
		Cheats,
		Backup
	}

	private static DwsOverlay _instance;

	private readonly Queue<Achievement> _toasts = new Queue<Achievement>();

	private Achievement _toast;

	private float _toastTime = -1f;

	private const float ToastDuration = 4f;

	private bool _open;

	private int _openFrame = -10;

	private int _closeFrame = -10;

	private Page _page;

	private int _sel;

	private float _scroll;

	private string _status = string.Empty;

	private float _statusTime;

	private Texture2D _white;

	private float _repeatTimer;

	private int _repeatDir;

	private readonly List<Rect> _rowRects = new List<Rect>();

	private Rect _backRect;

	public static bool MenuOpen
	{
		get { return _instance != null && _instance._open; }
	}

	// En la página de controles táctiles se enseñan los botones detrás del menú para ver el tamaño.
	public static bool PreviewTouch
	{
		get { return MenuOpen && _instance._page == Page.Touch; }
	}

	// El menú de Opciones del juego no debe reaccionar mientras está abierto (ni el fotograma en que se cierra).
	public static bool Blocking
	{
		get { return _instance != null && (_instance._open || Time.frameCount - _instance._closeFrame <= 1); }
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
	private static void Create()
	{
		Ensure();
	}

	private void Awake()
	{
		useGUILayout = false; // todo se dibuja con GUI directo, sin GUILayout
	}

	private static DwsOverlay Ensure()
	{
		if (_instance == null)
		{
			GameObject go = new GameObject("DwsOverlay");
			UnityEngine.Object.DontDestroyOnLoad(go);
			_instance = go.AddComponent<DwsOverlay>();
		}
		return _instance;
	}

	public static void ShowAchievement(Achievement id)
	{
		Ensure()._toasts.Enqueue(id);
	}

	public static void OpenMenu()
	{
		DwsAchievements.Flush();
		DwsOverlay o = Ensure();
		o._open = true;
		o._openFrame = Time.frameCount;
		o._page = Page.Main;
		o._sel = 0;
		o._status = string.Empty;
	}

	// Respuesta de DwsActivity tras exportar o importar (UnitySendMessage).
	public void OnBackupResult(string message)
	{
		SetStatus(message);
	}

	public void OnImportData(string content)
	{
		SetStatus(DwsBackup.Import(content));
	}

	private void SetStatus(string text)
	{
		_status = text;
		_statusTime = Time.unscaledTime;
	}

	private void Update()
	{
		if (_toastTime < 0f && _toasts.Count > 0)
		{
			_toast = _toasts.Dequeue();
			_toastTime = Time.unscaledTime;
		}
		else if (_toastTime >= 0f && Time.unscaledTime - _toastTime > ToastDuration)
		{
			_toastTime = -1f;
		}
		if (!_open || Time.frameCount == _openFrame)
		{
			return;
		}
		HandleInput();
	}

	// ----- Entrada -----

	private void HandleInput()
	{
		InputDevice d = InputManager.ActiveDevice;
		bool up = Input.GetKeyDown(KeyCode.UpArrow);
		bool down = Input.GetKeyDown(KeyCode.DownArrow);
		bool accept = Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space);
		bool back = Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Backspace);
		bool left = Input.GetKeyDown(KeyCode.LeftArrow);
		bool right = Input.GetKeyDown(KeyCode.RightArrow);
		int held = 0;
		if (d != null)
		{
			left |= d.DPadLeft.WasPressed || d.LeftStickLeft.WasPressed;
			right |= d.DPadRight.WasPressed || d.LeftStickRight.WasPressed;
			up |= d.DPadUp.WasPressed || d.LeftStickUp.WasPressed;
			down |= d.DPadDown.WasPressed || d.LeftStickDown.WasPressed;
			accept |= d.Action1.WasPressed;
			back |= d.Action2.WasPressed || d.GetControl(InputControlType.Back).WasPressed || d.GetControl(InputControlType.Select).WasPressed;
			if (d.DPadUp.IsPressed || d.LeftStickUp.IsPressed)
			{
				held = -1;
			}
			else if (d.DPadDown.IsPressed || d.LeftStickDown.IsPressed)
			{
				held = 1;
			}
		}
		// Repetición al mantener pulsado.
		if (held != 0 && held == _repeatDir)
		{
			_repeatTimer -= Time.unscaledDeltaTime;
			if (_repeatTimer <= 0f)
			{
				_repeatTimer = 0.12f;
				if (held < 0)
				{
					up = true;
				}
				else
				{
					down = true;
				}
			}
		}
		else
		{
			_repeatDir = held;
			_repeatTimer = 0.4f;
		}
		int count = RowCount();
		if (up)
		{
			Move(-1, count);
		}
		if (down)
		{
			Move(1, count);
		}
		if (left || right)
		{
			Change(_sel, right ? 1 : -1);
		}
		if (accept)
		{
			Activate(_sel);
		}
		else if (back)
		{
			Back();
		}
		// Toques: un toque en una fila la activa; tocar "Volver" vuelve.
		for (int i = 0; i < Input.touchCount; i++)
		{
			UnityEngine.Touch t = Input.GetTouch(i);
			if (t.phase != UnityEngine.TouchPhase.Began)
			{
				continue;
			}
			Vector2 p = new Vector2(t.position.x, Screen.height - t.position.y);
			if (_backRect.Contains(p))
			{
				Back();
				return;
			}
			for (int r = 0; r < _rowRects.Count; r++)
			{
				if (_rowRects[r].Contains(p))
				{
					_sel = r;
					Activate(r);
					return;
				}
			}
		}
	}

	private void Move(int dir, int count)
	{
		if (count <= 0)
		{
			return;
		}
		_sel = Mathf.Clamp(_sel + dir, 0, count - 1);
	}

	private int RowCount()
	{
		switch (_page)
		{
		case Page.Main:
			return 6;
		case Page.Image:
			return 3;
		case Page.Touch:
			return 2;
		case Page.Achievements:
			return DwsAchievements.All.Length;
		case Page.Cheats:
			return 4;
		case Page.Backup:
			return 3;
		}
		return 0;
	}

	private void Back()
	{
		if (_page == Page.Main)
		{
			_open = false;
			_closeFrame = Time.frameCount;
			return;
		}
		Page from = _page;
		_page = Page.Main;
		_sel = from == Page.Achievements ? 0 : (from == Page.Image ? 1 : (from == Page.Touch ? 2 : (from == Page.Cheats ? 3 : 4)));
		_status = string.Empty;
	}

	private void Go(Page page)
	{
		_page = page;
		_sel = 0;
		_scroll = 0f;
		_status = string.Empty;
	}

	private void Activate(int row)
	{
		switch (_page)
		{
		case Page.Main:
			if (row == 0)
			{
				Go(Page.Achievements);
			}
			else if (row == 1)
			{
				Go(Page.Image);
			}
			else if (row == 2)
			{
				Go(Page.Touch);
			}
			else if (row == 3)
			{
				Go(Page.Cheats);
			}
			else if (row == 4)
			{
				Go(Page.Backup);
			}
			else
			{
				Back();
			}
			break;
		case Page.Touch:
			if (row == 0)
			{
				Change(row, 1);
			}
			else
			{
				Back();
			}
			break;
		case Page.Image:
			if (row < 2)
			{
				Change(row, 1);
			}
			else
			{
				Back();
			}
			break;
		case Page.Cheats:
			if (row == 0)
			{
				DwsCheats.InfiniteAmmo = !DwsCheats.InfiniteAmmo;
			}
			else if (row == 1)
			{
				DwsCheats.Invulnerable = !DwsCheats.Invulnerable;
			}
			else if (row == 2)
			{
				DwsCheats.InfiniteLives = !DwsCheats.InfiniteLives;
			}
			else
			{
				Back();
			}
			break;
		case Page.Backup:
			if (row == 0)
			{
				SetStatus(L("Elige dónde guardar la copia...", "Choose where to save the backup..."));
				DwsBackup.StartExport();
			}
			else if (row == 1)
			{
				SetStatus(L("Elige el archivo de copia...", "Choose the backup file..."));
				DwsBackup.StartImport();
			}
			else
			{
				Back();
			}
			break;
		}
	}

	// Izquierda/derecha (o A) cambian el valor de las opciones de Imagen.
	private void Change(int row, int dir)
	{
		if (_page == Page.Touch)
		{
			if (row == 0)
			{
				DwsTouchControls.Size += dir;
			}
			return;
		}
		if (_page != Page.Image)
		{
			return;
		}
		if (row == 0)
		{
			DwsFilters.Filter += dir;
		}
		else if (row == 1)
		{
			DwsFilters.Colors += dir;
		}
	}

	// ----- Dibujo -----

	private static readonly Color Red = new Color(0.9f, 0.12f, 0.1f);

	private static readonly Color Grey = new Color(0.8f, 0.8f, 0.8f);

	private static string L(string es, string en)
	{
		return Loc.Spanish ? es : en;
	}

	private static int Unit
	{
		get { return Mathf.Max(12, Screen.height / 24); }
	}

	private void EnsureStyles()
	{
		if (_white == null)
		{
			_white = new Texture2D(1, 1);
			_white.SetPixel(0, 0, Color.white);
			_white.Apply();
		}
	}

	private void Fill(Rect r, Color c)
	{
		Color old = GUI.color;
		GUI.color = c;
		GUI.DrawTexture(r, _white);
		GUI.color = old;
	}

	// Recorta un rectángulo a la zona visible (para la lista de logros, que se desplaza).
	private static Rect Clip(Rect r, Rect clip)
	{
		float x0 = Mathf.Max(r.x, clip.x);
		float y0 = Mathf.Max(r.y, clip.y);
		float x1 = Mathf.Min(r.xMax, clip.xMax);
		float y1 = Mathf.Min(r.yMax, clip.yMax);
		return new Rect(x0, y0, Mathf.Max(0f, x1 - x0), Mathf.Max(0f, y1 - y0));
	}

	private void OnApplicationPause(bool paused)
	{
		if (paused)
		{
			DwsAchievements.Flush();
		}
	}

	private void OnApplicationQuit()
	{
		DwsAchievements.Flush();
	}

	private void OnGUI()
	{
		if (!_open && _toastTime < 0f)
		{
			return;
		}
		EnsureStyles();
		GUI.depth = -2000;
		if (_open)
		{
			DrawMenu();
		}
		if (_toastTime >= 0f)
		{
			DrawToast();
		}
	}

	private void DrawToast()
	{
		float t = Time.unscaledTime - _toastTime;
		float slide = Mathf.Clamp01(Mathf.Min(t, ToastDuration - t) / 0.3f);
		int u = Unit;
		float w = Mathf.Min(Screen.width * 0.8f, u * 18f);
		float h = u * 3.2f;
		Rect box = new Rect((Screen.width - w) * 0.5f, Mathf.Lerp(-h, u * 0.6f, slide), w, h);
		Fill(box, new Color(0f, 0f, 0f, 0.88f));
		Fill(new Rect(box.x, box.yMax - u * 0.12f, box.width, u * 0.12f), new Color(0.8f, 0.1f, 0.08f));
		Texture2D icon = DwsAchievements.Icon(_toast, true);
		float pad = u * 0.4f;
		float isz = h - pad * 2f;
		if (icon != null)
		{
			GUI.DrawTexture(new Rect(box.x + pad, box.y + pad, isz, isz), icon);
		}
		DwsAchievements.Def def = DwsAchievements.Get(_toast);
		float tx = box.x + pad * 2f + isz;
		float tw = box.xMax - tx - pad;
		DwsFont.Draw(new Rect(tx, box.y + pad, tw, u * 0.9f), L("¡Logro conseguido!", "Achievement unlocked!"), u * 0.8f, Red, TextAnchor.MiddleLeft, false);
		DwsFont.Draw(new Rect(tx, box.y + pad + u * 1.05f, tw, u * 1.3f), def != null ? def.Name : _toast.ToString(), u * 1.1f, Color.white, TextAnchor.MiddleLeft, false);
	}

	private void DrawMenu()
	{
		int u = Unit;
		// En Imagen y en Controles táctiles el fondo es más transparente para ver el efecto detrás.
		bool see = _page == Page.Image || _page == Page.Touch;
		Fill(new Rect(0f, 0f, Screen.width, Screen.height), new Color(0f, 0f, 0f, see ? 0.45f : 0.9f));
		float w = Mathf.Min(Screen.width - u * 2f, u * 30f);
		float x = (Screen.width - w) * 0.5f;
		string title = L("EXTRAS DEL PORT", "PORT EXTRAS");
		switch (_page)
		{
		case Page.Achievements:
			title = L("LOGROS", "ACHIEVEMENTS") + "  " + DwsAchievements.UnlockedCount + "/" + DwsAchievements.All.Length;
			break;
		case Page.Image:
			title = L("IMAGEN", "PICTURE");
			break;
		case Page.Touch:
			title = L("CONTROLES TÁCTILES", "TOUCH CONTROLS");
			break;
		case Page.Cheats:
			title = L("TRUCOS", "CHEATS");
			break;
		case Page.Backup:
			title = L("COPIA DE SEGURIDAD", "BACKUP");
			break;
		}
		DwsFont.Draw(new Rect(x, u * 0.5f, w, u * 2f), title, u * 1.6f, Red, TextAnchor.MiddleCenter, false);
		string backText = "< " + L("Volver", "Back");
		_backRect = new Rect(u * 0.5f, u * 0.5f, DwsFont.Width(backText, u) + u * 0.8f, u * 2f);
		Fill(_backRect, new Color(1f, 1f, 1f, 0.08f));
		DwsFont.Draw(_backRect, backText, u, Color.white, TextAnchor.MiddleCenter, false);
		float top = u * 3f;
		float bottom = Screen.height - u * 1.6f;
		_rowRects.Clear();
		if (_page == Page.Achievements)
		{
			DrawAchievements(x, w, top, bottom, u);
		}
		else
		{
			string[] rows = MenuRows();
			float rh = u * 1.9f;
			for (int i = 0; i < rows.Length; i++)
			{
				Rect r = new Rect(x, top + i * rh, w, rh * 0.9f);
				_rowRects.Add(r);
				bool sel = i == _sel;
				Fill(r, sel ? new Color(0.75f, 0.08f, 0.06f, 0.85f) : new Color(1f, 1f, 1f, 0.06f));
				DwsFont.Draw(new Rect(r.x + u * 0.6f, r.y, r.width - u, r.height), rows[i], u * 1.05f, Color.white, TextAnchor.MiddleLeft, false);
			}
			string note = PageNote();
			float ny = top + rows.Length * rh + u * 0.3f;
			if (!string.IsNullOrEmpty(note))
			{
				ny += DwsFont.Draw(new Rect(x, ny, w, u * 4f), note, u * 0.8f, Grey, TextAnchor.UpperLeft, true) + u * 0.4f;
			}
			if (!string.IsNullOrEmpty(_status) && _page == Page.Backup)
			{
				DwsFont.Draw(new Rect(x, ny, w, u * 4f), _status, u * 0.8f, Color.white, TextAnchor.UpperLeft, true);
			}
		}
		bool changes = _page == Page.Image || _page == Page.Touch;
		if (_page == Page.Achievements && DwsAchievements.CheatsBlockAchievements && DwsCheats.AnyActive)
		{
			DwsFont.Draw(new Rect(0f, bottom, Screen.width, u * 1.4f), L("Trucos activos: logros desactivados", "Cheats on: achievements disabled"), u * 0.8f, Red, TextAnchor.MiddleCenter, false);
			return;
		}
		DwsFont.Draw(new Rect(0f, bottom, Screen.width, u * 1.4f), changes ? L("Izquierda/derecha: cambiar    B: volver", "Left/right: change    B: back") : L("A: aceptar    B: volver", "A: select    B: back"), u * 0.8f, new Color(0.65f, 0.65f, 0.65f), TextAnchor.MiddleCenter, false);
	}

	private string[] MenuRows()
	{
		switch (_page)
		{
		case Page.Main:
			return new string[6]
			{
				L("Logros", "Achievements") + "  (" + DwsAchievements.UnlockedCount + "/" + DwsAchievements.All.Length + ")",
				L("Imagen", "Picture"),
				L("Controles táctiles", "Touch controls"),
				L("Trucos", "Cheats"),
				L("Copia de seguridad", "Backup"),
				L("Volver", "Back")
			};
		case Page.Image:
			return new string[3]
			{
				L("Filtro:  < ", "Filter:  < ") + DwsFilters.FilterName + " >",
				L("Colores:  < ", "Colors:  < ") + DwsFilters.ColorsName + " >",
				L("Volver", "Back")
			};
		case Page.Touch:
			return new string[2]
			{
				L("Tamaño:  < ", "Size:  < ") + DwsTouchControls.SizeName + " >",
				L("Volver", "Back")
			};
		case Page.Cheats:
			return new string[4]
			{
				L("Munición infinita: ", "Infinite ammo: ") + OnOff(DwsCheats.InfiniteAmmo),
				L("Invulnerable: ", "Invulnerable: ") + OnOff(DwsCheats.Invulnerable),
				L("Vidas infinitas: ", "Infinite lives: ") + OnOff(DwsCheats.InfiniteLives),
				L("Volver", "Back")
			};
		case Page.Backup:
			return new string[3]
			{
				L("Exportar partidas y perfil", "Export saves and profile"),
				L("Importar partidas y perfil", "Import saves and profile"),
				L("Volver", "Back")
			};
		}
		return new string[0];
	}

	private string PageNote()
	{
		if (_page == Page.Cheats)
		{
			string note = L("Invulnerable y vidas infinitas solo funcionan en Fin de los tiempos. Los precipicios siguen matando.",
				"Invulnerable and infinite lives only work in End of Times. Pits still kill.");
			if (DwsAchievements.CheatsBlockAchievements)
			{
				note += L(" Con algún truco activo no se consiguen logros ni avanzan sus contadores.", " While any cheat is on, achievements and their progress are disabled.");
			}
			return note;
		}
		if (_page == Page.Image)
		{
			return L("Izquierda/derecha cambian el valor. El efecto se ve detrás de este menú y en todo el juego.",
				"Left/right change the value. The effect shows behind this menu and in the whole game.");
		}
		if (_page == Page.Touch)
		{
			return L("Izquierda/derecha cambian el tamaño de los botones táctiles. Se ven detrás de este menú.",
				"Left/right change the size of the touch buttons. They show behind this menu.");
		}
		if (_page == Page.Backup)
		{
			return L("Guarda en un solo archivo las opciones, los controles, el avance de Fin de los tiempos, los logros y los trucos. Al importar, el juego se reinicia.",
				"Saves options, controls, End of Times progress, achievements and cheats in a single file. Importing restarts the game.");
		}
		return null;
	}

	private static string OnOff(bool on)
	{
		return on ? L("Sí", "On") : L("No", "Off");
	}

	private void DrawAchievements(float x, float w, float top, float bottom, int u)
	{
		DwsAchievements.Def[] all = DwsAchievements.All;
		float visible = bottom - top;
		Rect clip = new Rect(x, top, w, visible);
		float pad = u * 0.3f;
		float isz = u * 2.2f;
		float namePx = u * 1.05f;
		float descPx = u * 0.78f;
		float tw = w - pad * 3f - isz;
		// Cada fila mide lo que necesite su descripción.
		float[] ys = new float[all.Length + 1];
		for (int i = 0; i < all.Length; i++)
		{
			float textH = namePx + DwsFont.Height(DescOf(all[i]), descPx, tw) + pad * 2f;
			if (DwsAchievements.ProgressText(all[i]).Length > 0)
			{
				textH += descPx * 1.6f;
			}
			ys[i + 1] = ys[i] + Mathf.Max(isz + pad * 2f, textH) + u * 0.25f;
		}
		float total = ys[all.Length];
		// Desplazamiento suave para que la fila elegida quede a la vista.
		float selMid = (ys[_sel] + ys[_sel + 1]) * 0.5f;
		float target = Mathf.Clamp(selMid - visible * 0.5f, 0f, Mathf.Max(0f, total - visible));
		_scroll = Mathf.Lerp(_scroll, target, Mathf.Clamp01(Time.unscaledDeltaTime * 12f));
		for (int i = 0; i < all.Length; i++)
		{
			float y = top + ys[i] - _scroll;
			Rect r = new Rect(x, y, w, ys[i + 1] - ys[i] - u * 0.25f);
			_rowRects.Add(Clip(r, clip));
			if (r.yMax < top || r.y > bottom)
			{
				continue;
			}
			bool unlocked = DwsAchievements.IsUnlocked(all[i].Id);
			Fill(Clip(r, clip), i == _sel ? new Color(0.75f, 0.08f, 0.06f, 0.6f) : new Color(1f, 1f, 1f, unlocked ? 0.1f : 0.04f));
			Texture2D icon = DwsAchievements.Icon(all[i].Id, unlocked);
			Rect ir = new Rect(r.x + pad, r.y + pad, isz, isz);
			Rect ic = Clip(ir, clip);
			if (icon != null && ic.height > 0f)
			{
				Rect uv = new Rect((ic.x - ir.x) / ir.width, 1f - (ic.yMax - ir.y) / ir.height, ic.width / ir.width, ic.height / ir.height);
				GUI.DrawTextureWithTexCoords(ic, icon, uv);
			}
			float tx = r.x + pad * 2f + isz;
			float a = unlocked ? 1f : 0.6f;
			DwsFont.Draw(new Rect(tx, r.y + pad, tw, namePx), all[i].Name, namePx, new Color(1f, 1f, 1f, a), TextAnchor.MiddleLeft, false, clip);
			float dh = DwsFont.Draw(new Rect(tx, r.y + pad + namePx, tw, r.height), DescOf(all[i]), descPx, new Color(Grey.r, Grey.g, Grey.b, a), TextAnchor.UpperLeft, true, clip);
			string prog = DwsAchievements.ProgressText(all[i]);
			if (prog.Length > 0)
			{
				// Contador: barra de progreso con el número al lado.
				float py = r.y + pad + namePx + dh + descPx * 0.3f;
				float bw = tw * 0.45f;
				Rect bar = new Rect(tx, py + descPx * 0.3f, bw, descPx * 0.6f);
				Fill(Clip(bar, clip), new Color(1f, 1f, 1f, 0.15f));
				Rect done = new Rect(bar.x, bar.y, bar.width * DwsAchievements.ProgressFraction(all[i]), bar.height);
				Fill(Clip(done, clip), new Color(0.85f, 0.12f, 0.08f, 0.95f));
				DwsFont.Draw(new Rect(bar.xMax + descPx * 0.6f, py, tw - bw - descPx * 0.6f, descPx * 1.2f), prog, descPx, Color.white, TextAnchor.MiddleLeft, false, clip);
			}
		}
	}

	private static string DescOf(DwsAchievements.Def def)
	{
		string desc = def.Desc;
		if (DwsAchievements.IsUnlocked(def.Id))
		{
			desc += "  (" + DwsAchievements.UnlockDate(def.Id).ToString("dd/MM/yyyy") + ")";
		}
		return desc;
	}
}
