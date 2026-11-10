using UnityEngine;

// Port Android: sustituto del GUITexture del fundido a negro entre pantallas.
public class ScreenFaderOverlay : MonoBehaviour
{
	public Color color = Color.clear;

	private void Awake()
	{
		useGUILayout = false; // sin pasada de maquetación: OnGUI se llama en cada fotograma
	}

	private void OnGUI()
	{
		if (color.a <= 0.001f)
		{
			return;
		}
		GUI.depth = -1000;
		Color old = GUI.color;
		GUI.color = color;
		GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
		GUI.color = old;
	}
}
