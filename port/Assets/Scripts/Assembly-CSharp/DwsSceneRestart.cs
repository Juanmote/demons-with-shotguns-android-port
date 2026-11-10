using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using com.mindshaft.overtime;
using com.mindshaft.overtime.command;
using com.mindshaft.overtime.model;

// Port Android: reinicio limpio de una partida.
// Antes se recargaba la escena directamente y, mientras arrancaba la nueva, la anterior seguía viva:
// la nueva partida encontraba cámaras, HUD y puntos de aparición viejos (gráficos rotos y muertes en bucle).
// Ahora se destruye todo lo de la escena actual, se espera a que desaparezca y solo entonces se carga de nuevo.
public class DwsSceneRestart : MonoBehaviour
{
	private static bool _running;

	private string _level;

	private GameStateModel _gameState;

	public static void Restart(string level, GameStateModel gameState, EndOfTimesGameStateModel eotState)
	{
		if (_running)
		{
			return;
		}
		_running = true;
		if (string.IsNullOrEmpty(level))
		{
			level = SceneManager.GetActiveScene().name;
		}
		DwsLog.Write("Reinicio de escena: " + level);
		Time.timeScale = 1f;
		if (gameState != null)
		{
			gameState.Reset();
			gameState.IsPaused = false;
			gameState.MainCameraView = null;
			gameState.PlayerSpawnPoints = new System.Collections.Generic.List<GameObject>();
			gameState.RespawnRequests.Clear();
		}
		if (eotState != null)
		{
			eotState.Reset();
		}
		GameObject go = new GameObject("DwsSceneRestart");
		Object.DontDestroyOnLoad(go);
		DwsSceneRestart restart = go.AddComponent<DwsSceneRestart>();
		restart._level = level;
		restart._gameState = gameState;
		restart.StartCoroutine(restart.Run());
	}

	private IEnumerator Run()
	{
		// Todo lo que cuelga de la escena activa (los objetos permanentes viven en otra escena y no se tocan).
		GameObject[] roots = SceneManager.GetActiveScene().GetRootGameObjects();
		foreach (GameObject root in roots)
		{
			if (root != null && root != base.gameObject)
			{
				Object.Destroy(root);
			}
		}
		yield return null;
		yield return null;
		TeleportEntityCommand.DwsClearStatics();
		ZoomInSpriteCommand.DwsClearStatics();
		PlaySoundFxCommand.DwsClearStatics();
		yield return GameUtilities.CleanUpMemoryAndAssets();
		// La ventana de fin de partida pausaba el juego en cada fotograma hasta desaparecer.
		if (_gameState != null)
		{
			_gameState.IsPaused = false;
		}
		Time.timeScale = 1f;
		Application.LoadLevel(_level);
		yield return null;
		_running = false;
		Object.Destroy(base.gameObject);
	}
}
