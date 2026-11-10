using UnityEngine;
using UnityEngine.SceneManagement;

// Port Android: limitador de volumen en la mezcla final.
// Con muchas explosiones, disparos y objetos a la vez la suma de sonidos pasaba de 1 y Android la
// recortaba de golpe, que es el petardeo. Este componente va en el AudioListener: baja el volumen
// de toda la mezcla solo en esos picos (bajada rápida, subida lenta) y redondea lo poco que quede.
// Fuera de los picos no cambia nada.
public class DwsAudioLimiter : MonoBehaviour
{
	// A partir de aquí empieza a bajar el volumen.
	private const float Threshold = 0.8f;

	// Tiempo para recuperar el volumen tras un pico, en segundos.
	private const float ReleaseSeconds = 0.25f;

	// Tiempo de bajada, en segundos (casi instantáneo).
	private const float AttackSeconds = 0.002f;

	private float _gain = 1f;

	private float _attack;

	private float _release;

	private static bool _hooked;

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
	private static void Init()
	{
		if (!_hooked)
		{
			_hooked = true;
			SceneManager.sceneLoaded += delegate { Attach(); };
		}
		Attach();
	}

	// El juego crea el AudioListener al cargar cada escena; se le añade el limitador si no lo tiene.
	public static void Attach()
	{
		AudioListener listener = Object.FindObjectOfType<AudioListener>();
		if (listener != null && listener.GetComponent<DwsAudioLimiter>() == null)
		{
			listener.gameObject.AddComponent<DwsAudioLimiter>();
			DwsLog.Write("Limitador de sonido activo (" + AudioSettings.outputSampleRate + " Hz)");
		}
	}

	private void Awake()
	{
		float rate = Mathf.Max(8000, AudioSettings.outputSampleRate);
		_attack = 1f - Mathf.Exp(-1f / (AttackSeconds * rate));
		_release = 1f - Mathf.Exp(-1f / (ReleaseSeconds * rate));
	}

	// Se ejecuta en el hilo de audio con la mezcla de todo lo que suena.
	private void OnAudioFilterRead(float[] data, int channels)
	{
		float gain = _gain;
		for (int i = 0; i < data.Length; i += channels)
		{
			float peak = 0f;
			for (int c = 0; c < channels; c++)
			{
				float a = Mathf.Abs(data[i + c]);
				if (a > peak)
				{
					peak = a;
				}
			}
			// Ganancia que deja este instante justo en el umbral (1 si no hace falta bajar nada).
			float target = peak > Threshold ? Threshold / peak : 1f;
			gain += (target - gain) * (target < gain ? _attack : _release);
			for (int c = 0; c < channels; c++)
			{
				data[i + c] = SoftClip(data[i + c] * gain);
			}
		}
		_gain = gain;
	}

	// Por debajo de 0,8 no toca nada; por encima redondea suavemente hacia 1 en vez de recortar.
	private static float SoftClip(float x)
	{
		float a = x < 0f ? -x : x;
		if (a <= Threshold)
		{
			return x;
		}
		float k = 1f - Threshold;
		float y = Threshold + k * (1f - Mathf.Exp(-(a - Threshold) / k));
		return x < 0f ? -y : y;
	}
}
