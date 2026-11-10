using UnityEngine;

// Port Android: filtros de imagen y colores (como en Master of Darkness).
// Una cámara propia, que se dibuja después de las del juego, pinta un rectángulo con DwsFilter.shader:
// recoge la pantalla ya dibujada y la vuelve a pintar con el filtro. Los controles táctiles y el menú
// de extras se dibujan después y no se ven afectados.
public class DwsFilters : MonoBehaviour
{
	public const string FilterKey = "DwsFilter";

	public const string ColorsKey = "DwsColors";

	public static readonly string[] FilterNamesEs = new string[3] { "Ninguno", "Scanlines", "CRT" };

	public static readonly string[] FilterNamesEn = new string[3] { "None", "Scanlines", "CRT" };

	public static readonly string[] ColorNamesEs = new string[6] { "Original", "Tele antigua", "Vivos", "Nocturna", "Sepia", "Pantalla verde" };

	public static readonly string[] ColorNamesEn = new string[6] { "Original", "Old TV", "Vivid", "Night", "Sepia", "Green screen" };

	private const int Layer = 31;

	private static readonly int ModeId = Shader.PropertyToID("_Mode");

	private static readonly int LinesId = Shader.PropertyToID("_Lines");

	private static readonly int RId = Shader.PropertyToID("_R");

	private static readonly int GId = Shader.PropertyToID("_G");

	private static readonly int BId = Shader.PropertyToID("_B");

	// Última configuración aplicada: el material solo se actualiza cuando cambia algo.
	private int _appliedFilter = -1;

	private int _appliedColors = -1;

	private int _appliedW;

	private int _appliedH;

	private static DwsFilters _instance;

	private Camera _camera;

	private Transform _quad;

	private Material _material;

	private static int _filter = -1;

	private static int _colors = -1;

	public static int Filter
	{
		get
		{
			if (_filter < 0)
			{
				_filter = Mathf.Clamp(PlayerPrefs.GetInt(FilterKey, 0), 0, FilterNamesEs.Length - 1);
			}
			return _filter;
		}
		set
		{
			_filter = (value % FilterNamesEs.Length + FilterNamesEs.Length) % FilterNamesEs.Length;
			PlayerPrefs.SetInt(FilterKey, _filter);
			PlayerPrefs.Save();
		}
	}

	public static int Colors
	{
		get
		{
			if (_colors < 0)
			{
				_colors = Mathf.Clamp(PlayerPrefs.GetInt(ColorsKey, 0), 0, ColorNamesEs.Length - 1);
			}
			return _colors;
		}
		set
		{
			_colors = (value % ColorNamesEs.Length + ColorNamesEs.Length) % ColorNamesEs.Length;
			PlayerPrefs.SetInt(ColorsKey, _colors);
			PlayerPrefs.Save();
		}
	}

	public static string FilterName
	{
		get { return (Loc.Spanish ? FilterNamesEs : FilterNamesEn)[Filter]; }
	}

	public static string ColorsName
	{
		get { return (Loc.Spanish ? ColorNamesEs : ColorNamesEn)[Colors]; }
	}

	public static void Reload()
	{
		_filter = -1;
		_colors = -1;
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
	private static void Create()
	{
		if (_instance != null)
		{
			return;
		}
		Shader shader = Resources.Load<Shader>("DwsFilter");
		if (shader == null || !shader.isSupported)
		{
			DwsLog.Write("Filtros: shader no disponible (" + (shader == null ? "no encontrado" : "no soportado") + ")");
			return;
		}
		GameObject go = new GameObject("DwsFilters");
		Object.DontDestroyOnLoad(go);
		go.transform.position = new Vector3(50000f, 50000f, -10f);
		_instance = go.AddComponent<DwsFilters>();
		_instance.Setup(shader);
	}

	private void Setup(Shader shader)
	{
		_camera = base.gameObject.AddComponent<Camera>();
		_camera.clearFlags = CameraClearFlags.Nothing;
		_camera.cullingMask = 1 << Layer;
		_camera.orthographic = true;
		_camera.orthographicSize = 1f;
		_camera.nearClipPlane = 0.1f;
		_camera.farClipPlane = 50f;
		_camera.depth = 1000f;
		_camera.allowHDR = false;
		_camera.allowMSAA = false;
		_camera.useOcclusionCulling = false;
		GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
		Collider col = quad.GetComponent<Collider>();
		if (col != null)
		{
			Object.Destroy(col);
		}
		quad.name = "DwsFilterQuad";
		quad.layer = Layer;
		quad.transform.SetParent(base.transform, false);
		quad.transform.localPosition = new Vector3(0f, 0f, 10f);
		_quad = quad.transform;
		_material = new Material(shader);
		MeshRenderer mr = quad.GetComponent<MeshRenderer>();
		mr.sharedMaterial = _material;
		mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
		mr.receiveShadows = false;
		DwsLog.Write("Filtros listos");
	}

	private void LateUpdate()
	{
		if (_camera == null)
		{
			return;
		}
		bool active = Filter != 0 || Colors != 0;
		if (_camera.enabled != active)
		{
			_camera.enabled = active;
		}
		if (!active)
		{
			return;
		}
		if (Filter == _appliedFilter && Colors == _appliedColors && Screen.width == _appliedW && Screen.height == _appliedH)
		{
			return;
		}
		_appliedFilter = Filter;
		_appliedColors = Colors;
		_appliedW = Screen.width;
		_appliedH = Screen.height;
		float aspect = (float)Screen.width / Mathf.Max(1, Screen.height);
		_quad.localScale = new Vector3(aspect * 2f + 0.1f, 2.1f, 1f);
		_material.SetFloat(ModeId, Filter);
		// Unas 270 líneas a cualquier resolución: el tamaño de los píxeles del juego más o menos.
		_material.SetFloat(LinesId, Mathf.Max(120f, Mathf.Round(Screen.height / 4f)));
		Vector4 r;
		Vector4 g;
		Vector4 b;
		Matrix(Colors, out r, out g, out b);
		_material.SetVector(RId, r);
		_material.SetVector(GId, g);
		_material.SetVector(BId, b);
	}

	// Cada fila: cuánto aporta R, G y B al canal, más un desplazamiento.
	private static void Matrix(int colors, out Vector4 r, out Vector4 g, out Vector4 b)
	{
		switch (colors)
		{
		case 1: // Tele antigua: algo apagada, cálida y con menos contraste
			r = new Vector4(0.62f, 0.25f, 0.05f, 0.05f);
			g = new Vector4(0.15f, 0.68f, 0.07f, 0.04f);
			b = new Vector4(0.12f, 0.2f, 0.52f, 0.03f);
			break;
		case 2: // Vivos: más saturación
		{
			float s = 1.35f;
			float a = (1f - s) * 0.299f;
			float c = (1f - s) * 0.587f;
			float d = (1f - s) * 0.114f;
			r = new Vector4(a + s, c, d, 0f);
			g = new Vector4(a, c + s, d, 0f);
			b = new Vector4(a, c, d + s, 0f);
			break;
		}
		case 3: // Nocturna: oscura y azulada
			r = new Vector4(0.45f, 0.1f, 0.05f, 0f);
			g = new Vector4(0.08f, 0.55f, 0.1f, 0f);
			b = new Vector4(0.1f, 0.2f, 0.85f, 0.03f);
			break;
		case 4: // Sepia
			r = new Vector4(0.393f, 0.769f, 0.189f, 0f);
			g = new Vector4(0.349f, 0.686f, 0.168f, 0f);
			b = new Vector4(0.272f, 0.534f, 0.131f, 0f);
			break;
		case 5: // Pantalla verde (monocromo de fósforo verde)
			r = new Vector4(0.06f, 0.12f, 0.02f, 0f);
			g = new Vector4(0.3f, 0.59f, 0.11f, 0.03f);
			b = new Vector4(0.06f, 0.12f, 0.02f, 0f);
			break;
		default:
			r = new Vector4(1f, 0f, 0f, 0f);
			g = new Vector4(0f, 1f, 0f, 0f);
			b = new Vector4(0f, 0f, 1f, 0f);
			break;
		}
	}
}
