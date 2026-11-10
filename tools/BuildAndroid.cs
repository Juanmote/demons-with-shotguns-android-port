// Script de compilación para Android (lo llama el workflow 2).
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.Android;
using UnityEngine;

public static class BuildAndroid
{
    // Icono del port (cara del demonio)
    static void SetIcon()
    {
        Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/PortAndroid/icon.png");
        Texture2D bg = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/PortAndroid/icon_adaptive_bg.png");
        Texture2D fg = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/PortAndroid/icon_transparent.png");
        if (icon == null) { Debug.LogWarning("[DWS] Sin icono"); return; }
        PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Unknown, new[] { icon });
        foreach (PlatformIconKind kind in new PlatformIconKind[] { AndroidPlatformIconKind.Legacy, AndroidPlatformIconKind.Round, AndroidPlatformIconKind.Adaptive })
        {
            PlatformIcon[] icons = PlayerSettings.GetPlatformIcons(BuildTargetGroup.Android, kind);
            foreach (PlatformIcon pi in icons)
            {
                if (kind == AndroidPlatformIconKind.Adaptive) pi.SetTextures(bg, fg);
                else pi.SetTexture(icon);
            }
            PlayerSettings.SetPlatformIcons(BuildTargetGroup.Android, kind, icons);
        }
        Debug.Log("[DWS] Icono aplicado");
    }

    public static void Build()
    {
        try
        {
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            PlayerSettings.companyName = "MindShaft Games";
            PlayerSettings.productName = "Demons with Shotguns";
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.juanmote.demonswithshotguns");
            string ver = Environment.GetEnvironmentVariable("APK_VERSION") ?? "1";
            int code; if (!int.TryParse(ver, out code)) code = 1;
            PlayerSettings.bundleVersion = "1.0." + code;
            PlayerSettings.Android.bundleVersionCode = code;

            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetApiCompatibilityLevel(BuildTargetGroup.Android, ApiCompatibilityLevel.NET_4_6);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3 });
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.Android.androidIsGame = true;
            PlayerSettings.Android.androidTVCompatibility = true;
            PlayerSettings.Android.startInFullscreen = true;
            PlayerSettings.Android.renderOutsideSafeArea = true;
            PlayerSettings.SplashScreen.show = false;
            EditorUserBuildSettings.buildAppBundle = false;

            // Firma fija opcional (secretos del repo). Sin ella Unity usa una firma de depuración.
            string ks = Environment.GetEnvironmentVariable("ANDROID_KEYSTORE_PATH");
            string kp = Environment.GetEnvironmentVariable("ANDROID_KEYSTORE_PASS");
            if (!string.IsNullOrEmpty(ks) && File.Exists(ks) && !string.IsNullOrEmpty(kp))
            {
                PlayerSettings.Android.useCustomKeystore = true;
                PlayerSettings.Android.keystoreName = ks;
                PlayerSettings.Android.keystorePass = kp;
                PlayerSettings.Android.keyaliasName = Environment.GetEnvironmentVariable("ANDROID_KEY_ALIAS") ?? "dws";
                PlayerSettings.Android.keyaliasPass = kp;
                Debug.Log("[DWS] Firmando con keystore propio");
            }

            SetIcon();

            string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            Debug.Log("[DWS] Escenas: " + scenes.Length);
            string outPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../../build/DemonsWithShotguns.apk"));
            Directory.CreateDirectory(Path.GetDirectoryName(outPath));
            BuildReport r = BuildPipeline.BuildPlayer(scenes, outPath, BuildTarget.Android, BuildOptions.None);
            Debug.Log("[DWS] Resultado: " + r.summary.result + " errores=" + r.summary.totalErrors + " tamaño=" + r.summary.totalSize);
            EditorApplication.Exit(r.summary.result == BuildResult.Succeeded ? 0 : 1);
        }
        catch (Exception e)
        {
            Debug.LogError("[DWS] Excepción: " + e);
            EditorApplication.Exit(1);
        }
    }
}
