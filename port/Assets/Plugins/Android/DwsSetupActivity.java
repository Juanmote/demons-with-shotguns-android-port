package com.juanmote.demonswithshotguns;

import android.app.Activity;
import android.content.ContentResolver;
import android.content.Intent;
import android.content.SharedPreferences;
import android.content.pm.PackageInfo;
import android.content.res.AssetManager;
import android.database.Cursor;
import android.graphics.Color;
import android.net.Uri;
import android.os.Bundle;
import android.os.ParcelFileDescriptor;
import android.provider.DocumentsContract;
import android.util.TypedValue;
import android.view.Gravity;
import android.view.View;
import android.view.WindowManager;
import android.widget.Button;
import android.widget.LinearLayout;
import android.widget.ProgressBar;
import android.widget.TextView;

import java.io.BufferedReader;
import java.io.BufferedOutputStream;
import java.io.File;
import java.io.FileInputStream;
import java.io.FileOutputStream;
import java.io.IOException;
import java.io.InputStream;
import java.io.InputStreamReader;
import java.io.OutputStream;
import java.nio.ByteBuffer;
import java.nio.channels.FileChannel;
import java.nio.charset.StandardCharsets;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Locale;
import java.util.zip.CRC32;

// APK genérica: la primera vez pide la carpeta del juego de Steam y genera con ella
// la OBB (los datos del juego) en Android/obb/<paquete>/. Después abre el juego directamente.
// Si la APK no lleva manifiesto (compilación completa con los datos dentro), no hace nada.
public class DwsSetupActivity extends Activity {
    static final String MANIFEST = "dws/manifest.txt";
    static final String MOLDE = "dws/molde/";
    static final int REQ_TREE = 4242;
    static final String PREFS = "dws_setup";
    static final String KEY_TREE = "tree";
    static final String MARKER = "dws-obb.ok";

    boolean mEs;
    TextView mText;
    Button mButton;
    ProgressBar mBar;
    volatile boolean mBusy;

    @Override
    protected void onCreate(Bundle b) {
        super.onCreate(b);
        mEs = "es".equals(Locale.getDefault().getLanguage());
        if (!loadManifest()) { launchGame(); return; }
        if (obbReady()) { launchGame(); return; }
        buildUi();
        Uri saved = savedTree();
        if (saved != null) startGeneration(saved);
        else showChoose(null);
    }

    String t(String es, String en) { return mEs ? es : en; }

    // ----- Arranque del juego -----
    void launchGame() {
        Intent i = new Intent();
        i.setClassName(this, "com.juanmote.demonswithshotguns.DwsActivity");
        if (getIntent() != null && getIntent().getExtras() != null) i.putExtras(getIntent().getExtras());
        startActivity(i);
        overridePendingTransition(0, 0);
        finish();
    }

    int versionCode() {
        try {
            PackageInfo p = getPackageManager().getPackageInfo(getPackageName(), 0);
            return p.versionCode;
        } catch (Exception e) { return 0; }
    }

    File obbFile() {
        return new File(getObbDir(), "main." + versionCode() + "." + getPackageName() + ".obb");
    }

    String markerText() {
        return versionCode() + " " + mB.mId + " " + zipSize();
    }

    boolean obbReady() {
        File obb = obbFile();
        File mk = new File(getObbDir(), MARKER);
        if (!obb.isFile() || !mk.isFile()) return false;
        if (obb.length() != zipSize()) return false;
        try {
            byte[] d = readAll(new FileInputStream(mk));
            return new String(d, StandardCharsets.UTF_8).trim().equals(markerText());
        } catch (IOException e) { return false; }
    }

    // ----- Interfaz -----
    int dp(int v) { return (int) TypedValue.applyDimension(TypedValue.COMPLEX_UNIT_DIP, v, getResources().getDisplayMetrics()); }

    void buildUi() {
        getWindow().addFlags(WindowManager.LayoutParams.FLAG_KEEP_SCREEN_ON);
        LinearLayout root = new LinearLayout(this);
        root.setOrientation(LinearLayout.VERTICAL);
        root.setGravity(Gravity.CENTER);
        root.setBackgroundColor(Color.rgb(16, 12, 14));
        root.setPadding(dp(32), dp(24), dp(32), dp(24));

        TextView title = new TextView(this);
        title.setText("Demons with Shotguns");
        title.setTextColor(Color.rgb(230, 70, 50));
        title.setTextSize(TypedValue.COMPLEX_UNIT_SP, 26);
        title.setGravity(Gravity.CENTER);
        root.addView(title);

        mText = new TextView(this);
        mText.setTextColor(Color.rgb(225, 225, 225));
        mText.setTextSize(TypedValue.COMPLEX_UNIT_SP, 16);
        mText.setGravity(Gravity.CENTER);
        mText.setPadding(0, dp(16), 0, dp(16));
        root.addView(mText, new LinearLayout.LayoutParams(LinearLayout.LayoutParams.MATCH_PARENT, LinearLayout.LayoutParams.WRAP_CONTENT));

        mBar = new ProgressBar(this, null, android.R.attr.progressBarStyleHorizontal);
        mBar.setMax(1000);
        root.addView(mBar, new LinearLayout.LayoutParams(dp(420), LinearLayout.LayoutParams.WRAP_CONTENT));

        mButton = new Button(this);
        mButton.setText(t("Seleccionar carpeta del juego", "Select game folder"));
        mButton.setOnClickListener(new View.OnClickListener() {
            @Override public void onClick(View v) { pickFolder(); }
        });
        LinearLayout.LayoutParams bl = new LinearLayout.LayoutParams(LinearLayout.LayoutParams.WRAP_CONTENT, LinearLayout.LayoutParams.WRAP_CONTENT);
        bl.topMargin = dp(12);
        root.addView(mButton, bl);
        setContentView(root);
    }

    void showChoose(String error) {
        String msg = t("Para jugar necesitas los archivos de tu copia de Demons with Shotguns de Steam (versión de Windows).\n\n"
                        + "Copia a la consola la carpeta del juego (la que contiene dws_Data) y selecciónala. "
                        + "Solo hay que hacerlo una vez.",
                "To play you need the files from your Steam copy of Demons with Shotguns (Windows version).\n\n"
                        + "Copy the game folder (the one containing dws_Data) to your device and select it. "
                        + "You only need to do this once.");
        if (error != null) msg = error + "\n\n" + msg;
        mText.setText(msg);
        mBar.setVisibility(View.GONE);
        mButton.setVisibility(View.VISIBLE);
        mButton.requestFocus();
    }

    void showProgress(final long done, final long total) {
        runOnUiThread(new Runnable() {
            @Override public void run() {
                int p = total > 0 ? (int) (done * 1000 / total) : 0;
                mBar.setProgress(p);
                mText.setText(t("Preparando los datos del juego… ", "Preparing game data… ") + (p / 10) + " %\n\n"
                        + t("No cierres la aplicación.", "Don't close the app."));
            }
        });
    }

    void pickFolder() {
        if (mBusy) return;
        Intent i = new Intent(Intent.ACTION_OPEN_DOCUMENT_TREE);
        i.addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION | Intent.FLAG_GRANT_PERSISTABLE_URI_PERMISSION);
        try {
            startActivityForResult(i, REQ_TREE);
        } catch (Exception e) {
            showChoose(t("Este sistema no tiene selector de carpetas.", "This system has no folder picker."));
        }
    }

    @Override
    protected void onActivityResult(int req, int res, Intent data) {
        super.onActivityResult(req, res, data);
        if (req != REQ_TREE || res != RESULT_OK || data == null || data.getData() == null) return;
        Uri tree = data.getData();
        try {
            getContentResolver().takePersistableUriPermission(tree, Intent.FLAG_GRANT_READ_URI_PERMISSION);
        } catch (Exception ignored) { }
        startGeneration(tree);
    }

    Uri savedTree() {
        String s = getSharedPreferences(PREFS, MODE_PRIVATE).getString(KEY_TREE, null);
        return s == null ? null : Uri.parse(s);
    }

    void saveTree(Uri tree) {
        SharedPreferences.Editor e = getSharedPreferences(PREFS, MODE_PRIVATE).edit();
        e.putString(KEY_TREE, tree.toString());
        e.apply();
    }

    void startGeneration(final Uri tree) {
        if (mBusy) return;
        mBusy = true;
        mButton.setVisibility(View.GONE);
        mBar.setVisibility(View.VISIBLE);
        showProgress(0, 1);
        new Thread(new Runnable() {
            @Override public void run() {
                String err;
                try {
                    err = generate(tree);
                } catch (Throwable e) {
                    err = t("Error al preparar los datos: ", "Error while preparing data: ") + e;
                }
                final String fe = err;
                runOnUiThread(new Runnable() {
                    @Override public void run() {
                        mBusy = false;
                        if (fe == null) { saveTree(tree); launchGame(); }
                        else showChoose(fe);
                    }
                });
            }
        }, "dws-setup").start();
    }

    // ----- Manifiesto -----
    DwsObbBuilder mB;

    boolean loadManifest() {
        mB = new DwsObbBuilder(mEs, new DwsObbBuilder.Io() {
            @Override public InputStream molde(String name) throws IOException {
                return getAssets().open(MOLDE + name, AssetManager.ACCESS_STREAMING);
            }
            @Override public FileChannel source(String name) throws IOException { return channel(name); }
            @Override public void progress(long done, long total) { showProgress(done, total); }
        });
        try {
            return mB.parse(getAssets().open(MANIFEST));
        } catch (IOException e) {
            return false;
        }
    }

    long zipSize() { return mB.zipSize(); }

    // ----- Generación de la OBB -----
    HashMap<String, Uri> mSrcUris;
    HashMap<String, FileChannel> mOpen = new HashMap<String, FileChannel>();
    List<ParcelFileDescriptor> mPfds = new ArrayList<ParcelFileDescriptor>();

    String generate(Uri tree) throws Exception {
        String err = findSources(tree);
        if (err != null) return err;

        File dir = getObbDir();
        dir.mkdirs();
        long need = zipSize();
        long free = dir.getUsableSpace();
        File[] old = dir.listFiles();
        if (old != null) for (File f : old) if (f.getName().endsWith(".obb")) free += f.length();
        if (free < need + (32L << 20)) {
            return t("No hay espacio suficiente. Hacen falta ", "Not enough free space. Required: ") + (need >> 20) + " MB.";
        }
        if (old != null) for (File f : old) if (f.getName().endsWith(".obb") || f.getName().equals(MARKER)) f.delete();

        File tmp = new File(dir, "dws-tmp.obb.part");
        String res;
        try {
            res = mB.writeZip(tmp);
        } finally {
            for (FileChannel c : mOpen.values()) try { c.close(); } catch (IOException ignored) { }
            for (ParcelFileDescriptor p : mPfds) try { p.close(); } catch (IOException ignored) { }
            mOpen.clear(); mPfds.clear();
        }
        if (res != null) { tmp.delete(); return res; }
        File obb = obbFile();
        if (!tmp.renameTo(obb)) { tmp.delete(); return t("No se pudo guardar la OBB.", "Could not save the OBB."); }
        FileOutputStream mk = new FileOutputStream(new File(dir, MARKER));
        mk.write(markerText().getBytes(StandardCharsets.UTF_8));
        mk.close();
        return null;
    }

    // Lista los hijos de un documento del árbol: nombre -> {docId, mime, size}
    HashMap<String, String[]> listChildren(Uri tree, String docId) {
        HashMap<String, String[]> out = new HashMap<String, String[]>();
        Uri kids = DocumentsContract.buildChildDocumentsUriUsingTree(tree, docId);
        Cursor c = null;
        try {
            c = getContentResolver().query(kids, new String[]{
                    DocumentsContract.Document.COLUMN_DOCUMENT_ID,
                    DocumentsContract.Document.COLUMN_DISPLAY_NAME,
                    DocumentsContract.Document.COLUMN_MIME_TYPE,
                    DocumentsContract.Document.COLUMN_SIZE}, null, null, null);
            while (c != null && c.moveToNext()) {
                out.put(c.getString(1), new String[]{c.getString(0), c.getString(2), c.isNull(3) ? "-1" : c.getString(3)});
            }
        } finally {
            if (c != null) c.close();
        }
        return out;
    }

    String findSources(Uri tree) {
        HashMap<String, String[]> root;
        try {
            root = listChildren(tree, DocumentsContract.getTreeDocumentId(tree));
        } catch (Exception e) {
            return t("No se puede leer la carpeta guardada. Vuelve a seleccionarla.", "Can't read the saved folder. Please select it again.");
        }
        HashMap<String, String[]> data = root;
        if (!root.containsKey("resources.assets")) {
            String[] d = root.get("dws_Data");
            if (d == null) {
                return t("En esa carpeta no está dws_Data. Elige la carpeta del juego (o la propia dws_Data).",
                        "dws_Data was not found there. Choose the game folder (or dws_Data itself).");
            }
            data = listChildren(tree, d[0]);
        }
        mSrcUris = new HashMap<String, Uri>();
        for (DwsObbBuilder.Src s : mB.mSrcs) {
            String[] f = data.get(s.name);
            if (f == null) return t("Falta el archivo ", "Missing file ") + "dws_Data/" + s.name;
            long size = Long.parseLong(f[2]);
            if (size >= 0 && size != s.size) {
                return t("El archivo dws_Data/" + s.name + " no coincide con la versión de Steam esperada.",
                        "dws_Data/" + s.name + " doesn't match the expected Steam version.");
            }
            mSrcUris.put(s.name, DocumentsContract.buildDocumentUriUsingTree(tree, f[0]));
        }
        return null;
    }

    FileChannel channel(String name) throws IOException {
        FileChannel ch = mOpen.get(name);
        if (ch == null) {
            ParcelFileDescriptor pfd = getContentResolver().openFileDescriptor(mSrcUris.get(name), "r");
            mPfds.add(pfd);
            ch = new FileInputStream(pfd.getFileDescriptor()).getChannel();
            mOpen.put(name, ch);
        }
        return ch;
    }

    static byte[] readAll(InputStream in) throws IOException {
        java.io.ByteArrayOutputStream bo = new java.io.ByteArrayOutputStream();
        byte[] b = new byte[8192];
        int n;
        while ((n = in.read(b)) > 0) bo.write(b, 0, n);
        in.close();
        return bo.toByteArray();
    }
}
