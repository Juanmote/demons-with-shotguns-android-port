using InControl;
using UnityEngine;

// Port Android: controles táctiles. Aparecen al tocar la pantalla, se ven semitransparentes
// cuando no se pulsan y se ocultan en cuanto se usa un mando físico. Funcionan como un mando
// más de InControl ("Pantalla táctil"), así que sirven en menús, lobby y partida.
public class DwsTouchControls : MonoBehaviour
{
	private enum B
	{
		Disparar, Saltar, Escudo, Golpe, Embestida, Granada, Burla, Pausa, Count
	}

	private static readonly string[] Textures = { "disparar", "saltar", "escudo", "golpe", "embestida", "granada", "burla", "pausa" };

	private static readonly InputControlType[] Targets =
	{
		InputControlType.Action3, InputControlType.Action1, InputControlType.Action2, InputControlType.RightBumper,
		InputControlType.LeftBumper, InputControlType.Action4, InputControlType.Select, InputControlType.Start
	};

	private const float IdleAlpha = 0.35f;
	private const float PressedAlpha = 0.9f;

	private static DwsTouchControls _instance;

	// Tamaño de los botones (Extras del port › Controles táctiles): 0 pequeño, 1 normal, 2 grande.
	public const string SizeKey = "DwsTouchSize";

	private static readonly float[] SizeScale = { 0.8f, 1f, 1.2f };

	private static int _size = -1;

	public static int Size
	{
		get
		{
			if (_size < 0)
			{
				_size = Mathf.Clamp(PlayerPrefs.GetInt(SizeKey, 1), 0, SizeScale.Length - 1);
			}
			return _size;
		}
		set
		{
			int n = SizeScale.Length;
			_size = ((value % n) + n) % n;
			PlayerPrefs.SetInt(SizeKey, _size);
			PlayerPrefs.Save();
			if (_instance != null) _instance._layoutW = 0;
		}
	}

	public static string SizeName
	{
		get
		{
			switch (Size)
			{
			case 0: return Loc.Spanish ? "Pequeño" : "Small";
			case 2: return Loc.Spanish ? "Grande" : "Large";
			default: return "Normal";
			}
		}
	}

	private readonly Texture2D[] _tex = new Texture2D[(int)B.Count];
	private readonly Rect[] _rects = new Rect[(int)B.Count];
	private readonly bool[] _pressed = new bool[(int)B.Count];
	private Texture2D _crossTex;
	private Rect _crossRect;
	private int _crossFinger = -1;
	private bool _up, _down, _left, _right;
	private bool _visible;
	private int _layoutW, _layoutH;
	private TouchDevice _device;
	private bool _loggedAttach;
	private bool _nativeTouchSpace;

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
	private static void Create()
	{
		if (_instance != null) return;
		GameObject go = new GameObject("DwsTouchControls");
		Object.DontDestroyOnLoad(go);
		_instance = go.AddComponent<DwsTouchControls>();
	}

	private void Awake()
	{
		useGUILayout = false;
		for (int i = 0; i < Textures.Length; i++) _tex[i] = Resources.Load<Texture2D>("touch/" + Textures[i]);
		_crossTex = Resources.Load<Texture2D>("touch/cruceta");
	}

	private void Layout()
	{
		int w = Screen.width;
		int h = Screen.height;
		if (w == _layoutW && h == _layoutH) return;
		_layoutW = w;
		_layoutH = h;
		float u = Mathf.Min(h, w * 0.75f);
		float k = SizeScale[Size];
		float d = u * 0.17f * k;
		float gap = d * 1.08f;
		float m = u * 0.035f;
		// Fila de abajo (la más cómoda para el pulgar): disparar, saltar, escudo.
		float yLow = h - m - d * 0.5f;
		float yHigh = yLow - gap;
		float xRight = w - m - d * 0.5f;
		Place(B.Disparar, xRight - gap * 2f, yLow, d);
		Place(B.Saltar, xRight - gap, yLow, d);
		Place(B.Escudo, xRight, yLow, d);
		Place(B.Golpe, xRight - gap * 2f, yHigh, d);
		Place(B.Embestida, xRight - gap, yHigh, d);
		Place(B.Granada, xRight, yHigh, d);
		Place(B.Burla, w - m - d * 0.4f, m + d * 0.4f, d * 0.8f);
		Place(B.Pausa, w * 0.5f, h - m - d * 0.35f, d * 0.7f);
		float c = u * 0.42f * k;
		_crossRect = new Rect(m, h - m - c, c, c);
	}

	private void Place(B b, float cx, float cy, float size)
	{
		_rects[(int)b] = new Rect(cx - size * 0.5f, cy - size * 0.5f, size, size);
	}

	private void Update()
	{
		Layout();
		if (DwsOverlay.MenuOpen)
		{
			// El menú de extras se maneja tocando sus filas: los botones no se ven ni pulsan.
			_up = _down = _left = _right = false;
			for (int i = 0; i < _pressed.Length; i++) _pressed[i] = false;
			return;
		}
		int count = Input.touchCount;
		if (count > 0 && !_visible)
		{
			_visible = true;
		}
		else if (count == 0 && _visible && Input.anyKeyDown && !Input.GetMouseButton(0))
		{
			_visible = false; // se ha usado un mando o teclado
		}
		// Se reintenta en cada fotograma: si el primer toque llega antes de que exista InControl
		// (pasaba en la Odin 3) o InControl se reinicia, el mando táctil se quedaba sin conectar.
		if (_visible) AttachDevice();
		// Los menús del juego también reaccionan a los toques: se desactivan mientras se ven los botones
		// para que un toque en un botón no pulse a la vez lo que haya debajo.
		tk2dUIManager ui = tk2dUIManager.Instance__NoCreate;
		if (ui != null && ui.InputEnabled == _visible) ui.InputEnabled = !_visible;

		for (int i = 0; i < _pressed.Length; i++) _pressed[i] = false;
		_up = _down = _left = _right = false;
		if (!_visible) return;

		bool crossHeld = false;
		for (int t = 0; t < count; t++)
		{
			UnityEngine.Touch touch = Input.GetTouch(t);
			if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
			{
				if (touch.fingerId == _crossFinger) _crossFinger = -1;
				continue;
			}
			Vector2 p = ToGui(touch.position);
			if (touch.fingerId == _crossFinger || (touch.phase == TouchPhase.Began && InCrossZone(p)))
			{
				_crossFinger = touch.fingerId;
				crossHeld = true;
				Direction(p);
				continue;
			}
			int best = -1;
			float bestDist = float.MaxValue;
			for (int i = 0; i < _rects.Length; i++)
			{
				float dist = Vector2.Distance(p, _rects[i].center);
				if (dist < _rects[i].width * 0.6f && dist < bestDist)
				{
					best = i;
					bestDist = dist;
				}
			}
			if (best >= 0) _pressed[best] = true;
		}
		if (!crossHeld) _crossFinger = -1;
	}

	// Pasa la posición de un toque a coordenadas de dibujo (origen arriba a la izquierda).
	// En algunas consolas Unity da los toques en la resolución física de la pantalla aunque dibuje
	// a otra menor; entonces los botones no coincidían con lo que se ve (botones cambiados o muertos).
	private Vector2 ToGui(Vector2 raw)
	{
		int sw = Display.main.systemWidth;
		int sh = Display.main.systemHeight;
		bool differs = sw > Screen.width + 2 || sh > Screen.height + 2;
		if (differs && !_nativeTouchSpace && (raw.x > Screen.width + 2 || raw.y > Screen.height + 2))
		{
			_nativeTouchSpace = true;
			DwsLog.Write("Táctil: los toques llegan en resolución física " + sw + "x" + sh + "; se reescalan a " + Screen.width + "x" + Screen.height);
		}
		if (_nativeTouchSpace && sw > 0 && sh > 0)
		{
			raw = new Vector2(raw.x * Screen.width / sw, raw.y * Screen.height / sh);
		}
		return new Vector2(raw.x, Screen.height - raw.y);
	}

	private bool InCrossZone(Vector2 p)
	{
		Rect zone = new Rect(_crossRect.x - _crossRect.width * 0.15f, _crossRect.y - _crossRect.height * 0.15f, _crossRect.width * 1.3f, _crossRect.height * 1.3f);
		return zone.Contains(p);
	}

	private void Direction(Vector2 p)
	{
		Vector2 v = p - _crossRect.center;
		if (v.magnitude < _crossRect.width * 0.08f) return;
		// 8 direcciones: cada sector cubre 45 grados.
		float angle = Mathf.Atan2(-v.y, v.x) * Mathf.Rad2Deg;
		if (angle < 0f) angle += 360f;
		int sector = Mathf.RoundToInt(angle / 45f) % 8;
		_right = sector == 0 || sector == 1 || sector == 7;
		_up = sector == 1 || sector == 2 || sector == 3;
		_left = sector == 3 || sector == 4 || sector == 5;
		_down = sector == 5 || sector == 6 || sector == 7;
	}

	private void AttachDevice()
	{
		if (_device != null && _device.IsAttached) return;
		if (!InputManager.IsSetup) return;
		if (_device == null) _device = new TouchDevice(this);
		InputManager.AttachDevice(_device);
		if (!_loggedAttach)
		{
			_loggedAttach = true;
			DwsLog.Write("Táctil: mando táctil conectado=" + _device.IsAttached + " dispositivos=" + InputManager.Devices.Count);
		}
	}

	private void OnGUI()
	{
		if (Event.current.type != EventType.Repaint) return;
		// Con el menú de extras abierto solo se ven en la página de controles táctiles, para ver el tamaño.
		if (DwsOverlay.MenuOpen ? !DwsOverlay.PreviewTouch : !_visible) return;
		Layout();
		GUI.depth = -1000;
		Color old = GUI.color;
		if (_crossTex != null)
		{
			GUI.color = new Color(1f, 1f, 1f, IdleAlpha);
			GUI.DrawTexture(_crossRect, _crossTex);
			// Solo se iluminan los brazos que se pulsan (como en Master of Darkness).
			if (_up) DrawArm(0.33f, 0.00f, 0.67f, 0.42f);
			if (_down) DrawArm(0.33f, 0.58f, 0.67f, 1.00f);
			if (_left) DrawArm(0.00f, 0.33f, 0.42f, 0.67f);
			if (_right) DrawArm(0.58f, 0.33f, 1.00f, 0.67f);
		}
		for (int i = 0; i < _rects.Length; i++)
		{
			if (_tex[i] == null) continue;
			Rect r = _rects[i];
			if (_pressed[i])
			{
				float shrink = r.width * 0.05f;
				r = new Rect(r.x + shrink, r.y + shrink, r.width - shrink * 2f, r.height - shrink * 2f);
			}
			GUI.color = new Color(1f, 1f, 1f, _pressed[i] ? PressedAlpha : IdleAlpha);
			GUI.DrawTexture(r, _tex[i]);
		}
		GUI.color = old;
	}

	// Dibuja un trozo de la cruceta (coordenadas 0-1 con origen arriba a la izquierda) bien visible.
	private void DrawArm(float x0, float y0, float x1, float y1)
	{
		Rect r = _crossRect;
		Rect dst = new Rect(r.x + x0 * r.width, r.y + y0 * r.height, (x1 - x0) * r.width, (y1 - y0) * r.height);
		Rect uv = new Rect(x0, 1f - y1, x1 - x0, y1 - y0);
		GUI.color = Color.white;
		GUI.DrawTextureWithTexCoords(dst, _crossTex, uv);
		GUI.DrawTextureWithTexCoords(dst, _crossTex, uv);
	}

	private class TouchDevice : InputDevice
	{
		private readonly DwsTouchControls _owner;

		public TouchDevice(DwsTouchControls owner)
			: base("Pantalla táctil")
		{
			_owner = owner;
			AddControl(InputControlType.DPadUp, "Arriba");
			AddControl(InputControlType.DPadDown, "Abajo");
			AddControl(InputControlType.DPadLeft, "Izquierda");
			AddControl(InputControlType.DPadRight, "Derecha");
			for (int i = 0; i < Targets.Length; i++) AddControl(Targets[i], Textures[i]);
		}

		public override void Update(ulong updateTick, float deltaTime)
		{
			UpdateWithState(InputControlType.DPadUp, _owner._up, updateTick, deltaTime);
			UpdateWithState(InputControlType.DPadDown, _owner._down, updateTick, deltaTime);
			UpdateWithState(InputControlType.DPadLeft, _owner._left, updateTick, deltaTime);
			UpdateWithState(InputControlType.DPadRight, _owner._right, updateTick, deltaTime);
			for (int i = 0; i < Targets.Length; i++) UpdateWithState(Targets[i], _owner._pressed[i], updateTick, deltaTime);
		}
	}
}
