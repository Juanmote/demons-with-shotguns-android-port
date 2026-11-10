package com.juanmote.demonswithshotguns;

import android.content.Context;
import android.content.Intent;
import android.net.Uri;
import android.os.Build;
import android.os.Handler;
import android.os.HandlerThread;
import android.os.VibrationEffect;
import android.os.Vibrator;
import android.view.InputDevice;
import android.view.KeyEvent;

import com.unity3d.player.UnityPlayer;
import com.unity3d.player.UnityPlayerActivity;

import java.io.File;
import java.io.ByteArrayOutputStream;
import java.io.FileWriter;
import java.io.InputStream;
import java.io.OutputStream;
import java.nio.charset.StandardCharsets;
import java.util.HashSet;
import java.util.Locale;

// Port Android: botones C y Z (Retroid Pocket Classic), vibración y copia de seguridad.
// Deja además un registro en Android/data/com.juanmote.demonswithshotguns/files/dws-diag.txt
public class DwsActivity extends UnityPlayerActivity {
    private static DwsActivity sInstance;
    private static int sDiagLines;
    private static int sVibrateLogs;
    private static final int MAX_DIAG_LINES = 600;

    private final HashSet<String> mSeenKeys = new HashSet<String>();
    private boolean mIsRetroidClassic;

    @Override
    protected void onCreate(android.os.Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        sInstance = this;
        String id = (Build.MANUFACTURER + " " + Build.BRAND + " " + Build.MODEL + " " + Build.DEVICE).toLowerCase(Locale.ROOT);
        mIsRetroidClassic = id.contains("classic") && (id.contains("retroid") || id.contains("moorechip") || id.contains("pocket"));
        startDiag();
    }

    // C hace de L1 (Dash) y Z de R1 (Melee). Se traduce antes de que Unity vea la tecla.
    @Override
    public boolean dispatchKeyEvent(KeyEvent event) {
        int code = event.getKeyCode();
        logKey(event);
        int mapped = code;
        if (code == KeyEvent.KEYCODE_BUTTON_C) mapped = KeyEvent.KEYCODE_BUTTON_L1;
        else if (code == KeyEvent.KEYCODE_BUTTON_Z) mapped = KeyEvent.KEYCODE_BUTTON_R1;
        else if (mIsRetroidClassic && code == KeyEvent.KEYCODE_BUTTON_THUMBL) mapped = KeyEvent.KEYCODE_BUTTON_L1;
        else if (mIsRetroidClassic && code == KeyEvent.KEYCODE_BUTTON_THUMBR) mapped = KeyEvent.KEYCODE_BUTTON_R1;
        if (mapped != code) {
            event = new KeyEvent(event.getDownTime(), event.getEventTime(), event.getAction(), mapped,
                    event.getRepeatCount(), event.getMetaState(), event.getDeviceId(), event.getScanCode(),
                    event.getFlags(), event.getSource());
        }
        return super.dispatchKeyEvent(event);
    }

    // Hilo aparte para la vibración y el registro, para no frenar el juego.
    private static Handler sWorker;

    // Lo llama AndroidRumble.cs. Hace vibrar la consola y, si lo tiene, el mando del jugador:
    // algunas consolas declaran su mando integrado como externo o con un motor que no hace nada.
    public static void rumble(final String padName, final int sameNameBefore, final long ms, final int amplitude) {
        if (sInstance == null) return;
        worker().post(new Runnable() {
            @Override
            public void run() {
                rumbleNow(padName, sameNameBefore, ms, amplitude);
            }
        });
    }

    private static synchronized Handler worker() {
        if (sWorker == null) {
            HandlerThread thread = new HandlerThread("dws-rumble");
            thread.start();
            sWorker = new Handler(thread.getLooper());
        }
        return sWorker;
    }

    private static void rumbleNow(String padName, int sameNameBefore, long ms, int amplitude) {
        DwsActivity activity = sInstance;
        if (activity == null) return;
        try {
            vibrateWith((Vibrator) activity.getSystemService(Context.VIBRATOR_SERVICE), ms, amplitude, "consola");
        } catch (Throwable t) {
            diag("Error consola: " + t);
        }
        try {
            Vibrator pad = findPadVibrator(padName, sameNameBefore);
            if (pad != null) vibrateWith(pad, ms, amplitude, "mando");
        } catch (Throwable t) {
            diag("Error mando: " + t);
        }
    }

    private static void vibrateWith(Vibrator vibrator, long ms, int amplitude, String label) {
        boolean has = vibrator != null && vibrator.hasVibrator();
        if (sVibrateLogs < 4) {
            sVibrateLogs++;
            diag("Vibrar " + label + ": motor=" + has + " ms=" + ms + " fuerza=" + amplitude);
        }
        if (!has) return;
        if (Build.VERSION.SDK_INT >= 26) {
            int amp = vibrator.hasAmplitudeControl() ? amplitude : VibrationEffect.DEFAULT_AMPLITUDE;
            vibrator.vibrate(VibrationEffect.createOneShot(ms, amp));
        } else {
            vibrator.vibrate(ms);
        }
    }

    private static Vibrator findPadVibrator(String padName, int sameNameBefore) {
        if (padName == null || padName.length() == 0) return null;
        int[] ids = InputDevice.getDeviceIds();
        java.util.Arrays.sort(ids);
        int seen = 0;
        for (int id : ids) {
            InputDevice device = InputDevice.getDevice(id);
            if (device == null || !padName.equals(device.getName())) continue;
            int sources = device.getSources();
            boolean pad = (sources & InputDevice.SOURCE_GAMEPAD) == InputDevice.SOURCE_GAMEPAD
                    || (sources & InputDevice.SOURCE_JOYSTICK) == InputDevice.SOURCE_JOYSTICK;
            if (pad && seen++ == sameNameBefore) return device.getVibrator();
        }
        return null;
    }

    private void logKey(KeyEvent event) {
        if (event.getAction() != KeyEvent.ACTION_DOWN) return;
        InputDevice device = event.getDevice();
        String name = device != null ? device.getName() : "?";
        String key = event.getKeyCode() + "/" + event.getScanCode() + "/" + name;
        if (mSeenKeys.add(key)) {
            diag("Tecla " + KeyEvent.keyCodeToString(event.getKeyCode()) + " codigo=" + event.getKeyCode()
                    + " scan=" + event.getScanCode() + " dispositivo=" + name);
        }
    }

    private void startDiag() {
        try {
            File file = diagFile();
            if (file != null && file.exists()) file.delete();
            sDiagLines = 0;
            diag("Equipo: " + Build.MANUFACTURER + " | " + Build.BRAND + " | " + Build.MODEL + " | " + Build.DEVICE
                    + " | Android " + Build.VERSION.RELEASE + " | Retroid Classic=" + mIsRetroidClassic);
            Vibrator vibrator = (Vibrator) getSystemService(Context.VIBRATOR_SERVICE);
            diag("Motor de la consola: " + (vibrator != null && vibrator.hasVibrator()));
            for (int id : InputDevice.getDeviceIds()) {
                InputDevice device = InputDevice.getDevice(id);
                if (device == null) continue;
                diag("Dispositivo: " + device.getName() + " fuentes=" + Integer.toHexString(device.getSources())
                        + " motor=" + device.getVibrator().hasVibrator());
            }
        } catch (Throwable t) {
            diag("Error diagnóstico: " + t);
        }
    }

    private static File diagFile() {
        DwsActivity activity = sInstance;
        File dir = activity != null ? activity.getExternalFilesDir(null) : null;
        return dir != null ? new File(dir, "dws-diag.txt") : null;
    }

    // Lo llama DwsLog.cs para dejar mensajes y errores de Unity en el registro.
    public static void log(String line) {
        diag(line);
    }

    private static void diag(final String line) {
        worker().post(new Runnable() {
            @Override
            public void run() {
                diagNow(line);
            }
        });
    }

    private static synchronized void diagNow(String line) {
        if (sDiagLines >= MAX_DIAG_LINES) return;
        sDiagLines++;
        android.util.Log.i("DWS", line);
        try {
            File file = diagFile();
            if (file == null) return;
            FileWriter writer = new FileWriter(file, true);
            writer.write(line + "\n");
            writer.close();
        } catch (Throwable ignored) {
        }
    }

    // ----- Copia de seguridad (DwsBackup.cs): Android pregunta dónde guardar o qué abrir -----
    private static final int REQ_EXPORT = 7101;
    private static final int REQ_IMPORT = 7102;
    private static String sPendingExport;

    public static void exportBackup(final String content, final String fileName) {
        final DwsActivity a = sInstance;
        if (a == null) return;
        sPendingExport = content;
        a.runOnUiThread(new Runnable() {
            @Override public void run() {
                try {
                    Intent i = new Intent(Intent.ACTION_CREATE_DOCUMENT);
                    i.addCategory(Intent.CATEGORY_OPENABLE);
                    i.setType("application/octet-stream");
                    i.putExtra(Intent.EXTRA_TITLE, fileName);
                    a.startActivityForResult(i, REQ_EXPORT);
                } catch (Exception e) {
                    diag("Exportar: " + e);
                    send("OnBackupResult", es() ? "No se pudo abrir el selector de archivos." : "Could not open the file picker.");
                }
            }
        });
    }

    public static void importBackup() {
        final DwsActivity a = sInstance;
        if (a == null) return;
        a.runOnUiThread(new Runnable() {
            @Override public void run() {
                try {
                    Intent i = new Intent(Intent.ACTION_OPEN_DOCUMENT);
                    i.addCategory(Intent.CATEGORY_OPENABLE);
                    i.setType("*/*");
                    a.startActivityForResult(i, REQ_IMPORT);
                } catch (Exception e) {
                    diag("Importar: " + e);
                    send("OnBackupResult", es() ? "No se pudo abrir el selector de archivos." : "Could not open the file picker.");
                }
            }
        });
    }

    @Override
    protected void onActivityResult(int requestCode, int resultCode, Intent data) {
        if (requestCode != REQ_EXPORT && requestCode != REQ_IMPORT) {
            super.onActivityResult(requestCode, resultCode, data);
            return;
        }
        Uri uri = (resultCode == RESULT_OK && data != null) ? data.getData() : null;
        if (uri == null) {
            send("OnBackupResult", es() ? "Cancelado." : "Cancelled.");
            sPendingExport = null;
            return;
        }
        if (requestCode == REQ_EXPORT) {
            try {
                OutputStream out = getContentResolver().openOutputStream(uri, "wt");
                out.write(sPendingExport.getBytes(StandardCharsets.UTF_8));
                out.close();
                diag("Copia exportada a " + uri);
                send("OnBackupResult", es() ? "Copia guardada." : "Backup saved.");
            } catch (Exception e) {
                diag("Exportar: " + e);
                send("OnBackupResult", (es() ? "No se pudo guardar: " : "Could not save: ") + e.getMessage());
            }
            sPendingExport = null;
        } else {
            try {
                InputStream in = getContentResolver().openInputStream(uri);
                ByteArrayOutputStream buf = new ByteArrayOutputStream();
                byte[] chunk = new byte[16384];
                int n;
                while ((n = in.read(chunk)) > 0) buf.write(chunk, 0, n);
                in.close();
                diag("Copia leída de " + uri + " (" + buf.size() + " bytes)");
                send("OnImportData", new String(buf.toByteArray(), StandardCharsets.UTF_8));
            } catch (Exception e) {
                diag("Importar: " + e);
                send("OnBackupResult", (es() ? "No se pudo leer: " : "Could not read: ") + e.getMessage());
            }
        }
    }

    private static void send(String method, String message) {
        try {
            UnityPlayer.UnitySendMessage("DwsOverlay", method, message);
        } catch (Throwable t) {
            diag("UnitySendMessage: " + t);
        }
    }

    private static boolean es() {
        return Locale.getDefault().getLanguage().startsWith("es");
    }

    // Tras importar una copia: vuelve a abrir el juego para que cargue todo de nuevo.
    public static void restartApp() {
        final DwsActivity a = sInstance;
        if (a == null) return;
        a.runOnUiThread(new Runnable() {
            @Override public void run() {
                new Handler().postDelayed(new Runnable() {
                    @Override public void run() {
                        try {
                            Intent launch = a.getPackageManager().getLaunchIntentForPackage(a.getPackageName());
                            launch.addFlags(Intent.FLAG_ACTIVITY_NEW_TASK | Intent.FLAG_ACTIVITY_CLEAR_TASK);
                            a.startActivity(launch);
                        } catch (Exception e) {
                            diag("Reiniciar: " + e);
                        }
                        Runtime.getRuntime().exit(0);
                    }
                }, 1500);
            }
        });
    }
}
