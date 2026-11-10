using System.Collections.Generic;
using UnityEngine;

// Port Android: dibuja texto en OnGUI con la fuente de los menús del juego (8BitFont, la de Opciones
// y el lobby), para que el menú de extras y los avisos de logro se vean integrados.
// Coloca cada letra como tk2d (tk2dTextGeomGen) y la pinta con GUI.DrawTextureWithTexCoords
// (esta fuente no tiene letras giradas en el atlas). Si no se encuentra, usa la letra de Unity.
public static class DwsFont
{
	private const string FontName = "8BitFontdata";

	private const string TexturePath = "fonts/bitFont1_0";

	// Parte alta de la línea en unidades de la fuente: centra a la vista las letras en la línea.
	private const float LineTop = 1.575f;

	// Tamaño de un píxel del dibujo de la letra, en unidades de la fuente (3 téxeles de 0,05).
	private const float ArtPixel = 0.15f;

	private static Texture _tex;

	private static tk2dFontData _font;

	private static float _nextSearch;

	private static GUIStyle _fallback;

	private static readonly List<string> _lines = new List<string>();

	public static bool Ready
	{
		get { return Font != null; }
	}

	private static tk2dFontData Font
	{
		get
		{
			if (_font == null && Time.unscaledTime >= _nextSearch)
			{
				_nextSearch = Time.unscaledTime + 1f;
				_font = Find();
			}
			return _font;
		}
	}

	private static tk2dFontData Find()
	{
		try
		{
			foreach (tk2dFontData f in Resources.FindObjectsOfTypeAll<tk2dFontData>())
			{
				if (f != null && f.name == FontName)
				{
					return Prepare(f);
				}
			}
			// Si ninguna escena la ha cargado todavía, se saca de una ventana del juego que la usa.
			GameObject prefab = Resources.Load<GameObject>("prefabs/ui/ScoreText");
			if (prefab != null)
			{
				foreach (tk2dTextMesh t in prefab.GetComponentsInChildren<tk2dTextMesh>(true))
				{
					if (t.font != null && t.font.name == FontName)
					{
						return Prepare(t.font);
					}
				}
			}
		}
		catch (System.Exception e)
		{
			DwsLog.Write("Fuente del menú: " + e.Message);
		}
		return null;
	}

	private static tk2dFontData Prepare(tk2dFontData f)
	{
		tk2dFontData inst = f.inst;
		if (inst.useDictionary && inst.charDict == null)
		{
			inst.InitDictionary();
		}
		_tex = inst.material != null ? inst.material.mainTexture : null;
		if (_tex == null)
		{
			_tex = Resources.Load<Texture2D>(TexturePath);
		}
		DwsLog.Write("Fuente del menú lista: " + inst.name + " textura=" + (_tex != null ? _tex.name + " " + _tex.width + "x" + _tex.height : "ninguna"));
		return _tex != null ? inst : null;
	}

	private static tk2dFontChar Char(tk2dFontData f, char c)
	{
		int code = c;
		if (f.useDictionary)
		{
			tk2dFontChar ch;
			return f.charDict != null && f.charDict.TryGetValue(code, out ch) ? ch : null;
		}
		return code < f.chars.Length ? f.chars[code] : null;
	}

	// Ancho en píxeles de un texto de una línea con la altura de línea indicada.
	public static float Width(string text, float px)
	{
		tk2dFontData f = Font;
		if (f == null)
		{
			return Fallback(px).CalcSize(new GUIContent(text)).x;
		}
		float s = Snap(px) / f.lineHeight;
		float w = 0f;
		for (int i = 0; i < text.Length; i++)
		{
			tk2dFontChar ch = Char(f, text[i]);
			if (ch != null)
			{
				w += ch.advance * s;
			}
		}
		return w;
	}

	// Parte el texto en líneas que quepan en el ancho dado (por palabras).
	public static List<string> Wrap(string text, float px, float width)
	{
		List<string> result = new List<string>();
		foreach (string para in text.Split('\n'))
		{
			string line = string.Empty;
			foreach (string word in para.Split(' '))
			{
				string candidate = line.Length == 0 ? word : line + " " + word;
				if (line.Length > 0 && Width(candidate, px) > width)
				{
					result.Add(line);
					line = word;
				}
				else
				{
					line = candidate;
				}
			}
			result.Add(line);
		}
		return result;
	}

	// Altura que ocupará un texto ajustado al ancho.
	public static float Height(string text, float px, float width)
	{
		return Wrap(text, px, width).Count * px;
	}

	// Dibuja el texto dentro de r. px es la altura de línea. Lo que quede fuera de clip no se dibuja.
	// Devuelve la altura ocupada.
	public static float Draw(Rect r, string text, float px, Color color, TextAnchor anchor, bool wrap, Rect clip)
	{
		if (string.IsNullOrEmpty(text))
		{
			return 0f;
		}
		List<string> lines;
		if (wrap)
		{
			lines = Wrap(text, px, r.width);
		}
		else
		{
			_lines.Clear();
			_lines.AddRange(text.Split('\n'));
			lines = _lines;
		}
		float total = lines.Count * px;
		if (Event.current.type != EventType.Repaint)
		{
			return total;
		}
		float y = r.y;
		int row = (int)anchor / 3; // 0 arriba, 1 centro, 2 abajo
		int col = (int)anchor % 3; // 0 izquierda, 1 centro, 2 derecha
		if (row == 1)
		{
			y = r.y + (r.height - total) * 0.5f;
		}
		else if (row == 2)
		{
			y = r.yMax - total;
		}
		tk2dFontData f = Font;
		if (f == null)
		{
			GUIStyle st = Fallback(px);
			st.alignment = anchor;
			st.wordWrap = wrap;
			Color prev = GUI.color;
			GUI.color = color;
			GUI.Label(r, text, st);
			GUI.color = prev;
			return total;
		}
		float s = Snap(px) / f.lineHeight;
		float shadow = Mathf.Max(1f, Mathf.Round(ArtPixel * s));
		Color old = GUI.color;
		for (int pass = 0; pass < 2; pass++)
		{
			// Primero una sombra oscura para que se lea sobre cualquier fondo.
			GUI.color = pass == 0 ? new Color(0f, 0f, 0f, color.a * 0.8f) : color;
			float off = pass == 0 ? shadow : 0f;
			for (int li = 0; li < lines.Count; li++)
			{
				string line = lines[li];
				float x = r.x;
				if (col != 0)
				{
					float lw = Width(line, px);
					x = col == 1 ? r.x + (r.width - lw) * 0.5f : r.xMax - lw;
				}
				x = Mathf.Round(x);
				float top = Mathf.Round(y + li * px);
				for (int i = 0; i < line.Length; i++)
				{
					tk2dFontChar ch = Char(f, line[i]);
					if (ch == null)
					{
						continue;
					}
					if (ch.p1.x > ch.p0.x)
					{
						Glyph(ch, x + off, top + off, s, clip);
					}
					x += ch.advance * s;
				}
			}
		}
		GUI.color = old;
		return total;
	}

	public static float Draw(Rect r, string text, float px, Color color, TextAnchor anchor, bool wrap)
	{
		return Draw(r, text, px, color, anchor, wrap, new Rect(0f, 0f, Screen.width, Screen.height));
	}

	private static void Glyph(tk2dFontChar ch, float x, float lineTop, float s, Rect clip)
	{
		float l = Mathf.Round(x + ch.p0.x * s);
		float rr = Mathf.Round(x + ch.p1.x * s);
		float t = Mathf.Round(lineTop + (LineTop - ch.p0.y) * s);
		float b = Mathf.Round(lineTop + (LineTop - ch.p1.y) * s);
		if (t < clip.y || b > clip.yMax || rr < clip.x || l > clip.xMax)
		{
			return;
		}
		// uv0 es la esquina de arriba a la izquierda de la letra y uv1 la de abajo a la derecha.
		Rect uv = new Rect(ch.uv0.x, ch.uv1.y, ch.uv1.x - ch.uv0.x, ch.uv0.y - ch.uv1.y);
		GUI.DrawTextureWithTexCoords(new Rect(l, t, rr - l, b - t), _tex, uv);
	}

	// Ajusta el tamaño para que cada píxel de la letra ocupe un número entero de píxeles de pantalla.
	private static float Snap(float px)
	{
		tk2dFontData f = _font;
		if (f == null)
		{
			return px;
		}
		float step = f.lineHeight / ArtPixel;
		return Mathf.Max(1f, Mathf.Round(px / step)) * step;
	}

	private static GUIStyle Fallback(float px)
	{
		if (_fallback == null)
		{
			_fallback = new GUIStyle(GUI.skin.label);
			_fallback.normal.textColor = Color.white;
		}
		_fallback.fontSize = Mathf.Max(8, Mathf.RoundToInt(px * 0.8f));
		return _fallback;
	}
}
