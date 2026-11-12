package com.juanmote.demonswithshotguns;

import java.io.BufferedOutputStream;
import java.io.BufferedReader;
import java.io.File;
import java.io.FileOutputStream;
import java.io.IOException;
import java.io.InputStream;
import java.io.InputStreamReader;
import java.io.OutputStream;
import java.nio.ByteBuffer;
import java.nio.channels.FileChannel;
import java.nio.charset.StandardCharsets;
import java.util.ArrayList;
import java.util.List;
import java.util.zip.CRC32;

// Genera la OBB de la APK genérica a partir del molde, el manifiesto y los archivos de Steam.
// No depende de Android (se puede probar en un PC). La usa DwsSetupActivity.
public class DwsObbBuilder {
    public interface Io {
        InputStream molde(String name) throws IOException;
        FileChannel source(String name) throws IOException;
        void progress(long done, long total);
    }

    final boolean mEs;
    final Io mIo;

    public DwsObbBuilder(boolean es, Io io) { mEs = es; mIo = io; }

    String t(String es, String en) { return mEs ? es : en; }

    static final class Src { String name; long size; }
    static final class Cmd { long dst, len; String src; long srcOff, srcLen; String mode; int w, h; }
    static final class Entry { String name; long size; long crc; String molde; final List<Cmd> cmds = new ArrayList<Cmd>(); }

    String mId = "";
    final List<Src> mSrcs = new ArrayList<Src>();
    final List<Entry> mEntries = new ArrayList<Entry>();

    // Lee el manifiesto; devuelve false si no es válido
    public boolean parse(InputStream in) {
        try {
            BufferedReader r = new BufferedReader(new InputStreamReader(in, StandardCharsets.UTF_8));
            Entry cur = null;
            String line;
            while ((line = r.readLine()) != null) {
                if (line.length() == 0 || line.charAt(0) == '#') continue;
                String[] p = line.split("\t");
                switch (p[0]) {
                    case "V": mId = p[1]; break;
                    case "S": { Src s = new Src(); s.name = p[1]; s.size = Long.parseLong(p[2]); mSrcs.add(s); break; }
                    case "F": {
                        cur = new Entry(); cur.name = p[1]; cur.size = Long.parseLong(p[2]);
                        cur.crc = Long.parseLong(p[3], 16); cur.molde = p[4]; mEntries.add(cur); break;
                    }
                    case "C": {
                        Cmd c = new Cmd(); c.dst = Long.parseLong(p[1]); c.len = Long.parseLong(p[2]); c.src = p[3];
                        c.srcOff = Long.parseLong(p[4]); c.srcLen = Long.parseLong(p[5]); c.mode = p[6];
                        c.w = Integer.parseInt(p[7]); c.h = Integer.parseInt(p[8]);
                        cur.cmds.add(c); break;
                    }
                }
            }
            r.close();
            return !mEntries.isEmpty();
        } catch (Exception e) {
            return false;
        }
    }

    void readFully(String name, long off, byte[] buf, int len) throws IOException {
        FileChannel ch = mIo.source(name);
        ByteBuffer bb = ByteBuffer.wrap(buf, 0, len);
        long pos = off;
        while (bb.hasRemaining()) {
            int n = ch.read(bb, pos);
            if (n < 0) throw new IOException("EOF " + name);
            pos += n;
        }
    }

    // ----- Escritor ZIP (STORED, datos alineados a 16) -----
    static int headerLen(Entry e) {
        return 30 + e.name.getBytes(StandardCharsets.UTF_8).length;
    }

    public long zipSize() {
        long pos = 0, cd = 0;
        for (Entry e : mEntries) {
            long h = headerLen(e);
            long pad = (16 - ((pos + h + 4) % 16)) % 16;
            pos += h + 4 + pad + e.size;
            cd += 46 + e.name.getBytes(StandardCharsets.UTF_8).length;
        }
        return pos + cd + 22;
    }

    long mDone, mTotal, mLastShown;
    CRC32 mCrc;
    OutputStream mOut;
    long mPos;

    void out(byte[] b, int off, int len) throws IOException {
        mOut.write(b, off, len);
        mCrc.update(b, off, len);
        mPos += len;
        mDone += len;
        if (mDone - mLastShown > (4L << 20)) { mLastShown = mDone; mIo.progress(mDone, mTotal); }
    }

    static void le16(OutputStream o, int v) throws IOException { o.write(v & 255); o.write((v >> 8) & 255); }
    static void le32(OutputStream o, long v) throws IOException { le16(o, (int) (v & 0xFFFF)); le16(o, (int) ((v >> 16) & 0xFFFF)); }

    // Escribe la OBB; devuelve null si todo va bien o el texto del error
    public String writeZip(File file) throws IOException {
        mTotal = 0;
        for (Entry e : mEntries) mTotal += e.size;
        mDone = 0; mLastShown = 0;
        FileOutputStream fos = new FileOutputStream(file);
        BufferedOutputStream bos = new BufferedOutputStream(fos, 1 << 20);
        long[] offsets = new long[mEntries.size()];
        long pos = 0;
        byte[] buf = new byte[1 << 20];
        try {
            for (int i = 0; i < mEntries.size(); i++) {
                Entry e = mEntries.get(i);
                byte[] nm = e.name.getBytes(StandardCharsets.UTF_8);
                int pad = (int) ((16 - ((pos + 30 + nm.length + 4) % 16)) % 16);
                offsets[i] = pos;
                // Cabecera local (CRC y tamaño ya conocidos por el manifiesto; se comprueban al final)
                le32(bos, 0x04034b50L); le16(bos, 10); le16(bos, 0); le16(bos, 0);
                le16(bos, 0); le16(bos, 0x21); // hora/fecha fijas
                le32(bos, e.crc); le32(bos, e.size); le32(bos, e.size);
                le16(bos, nm.length); le16(bos, 4 + pad);
                bos.write(nm);
                le16(bos, 0xD935); le16(bos, pad); // relleno de alineación (como zipalign)
                for (int k = 0; k < pad; k++) bos.write(0);
                pos += 30 + nm.length + 4 + pad;

                mOut = bos; mCrc = new CRC32(); mPos = 0;
                InputStream molde = "-".equals(e.molde) ? null : mIo.molde(e.molde);
                try {
                    for (Cmd c : e.cmds) {
                        copyMolde(molde, c.dst - mPos, buf);
                        skipMolde(molde, c.len, buf);
                        writeCmd(c, buf);
                    }
                    copyMolde(molde, e.size - mPos, buf);
                } finally {
                    if (molde != null) molde.close();
                }
                if (mPos != e.size) return "Tamaño incorrecto en " + e.name;
                if (mCrc.getValue() != e.crc) {
                    return t("Los archivos del juego no coinciden con la versión de Steam esperada (" + e.name + ").",
                            "The game files don't match the expected Steam version (" + e.name + ").");
                }
                pos += e.size;
            }
            long cdStart = pos;
            long cdLen = 0;
            for (int i = 0; i < mEntries.size(); i++) {
                Entry e = mEntries.get(i);
                byte[] nm = e.name.getBytes(StandardCharsets.UTF_8);
                le32(bos, 0x02014b50L); le16(bos, 10); le16(bos, 10); le16(bos, 0); le16(bos, 0);
                le16(bos, 0); le16(bos, 0x21);
                le32(bos, e.crc); le32(bos, e.size); le32(bos, e.size);
                le16(bos, nm.length); le16(bos, 0); le16(bos, 0); le16(bos, 0); le16(bos, 0);
                le32(bos, 0); le32(bos, offsets[i]);
                bos.write(nm);
                cdLen += 46 + nm.length;
            }
            le32(bos, 0x06054b50L); le16(bos, 0); le16(bos, 0);
            le16(bos, mEntries.size()); le16(bos, mEntries.size());
            le32(bos, cdLen); le32(bos, cdStart); le16(bos, 0);
            bos.flush();
            fos.getFD().sync();
        } finally {
            bos.close();
        }
        return null;
    }

    // Salta n bytes del molde (esa zona la rellenan los datos de Steam)
    static void skipMolde(InputStream molde, long n, byte[] buf) throws IOException {
        if (molde == null) return;
        while (n > 0) {
            int r = molde.read(buf, 0, (int) Math.min(buf.length, n));
            if (r < 0) throw new IOException("Molde corto");
            n -= r;
        }
    }

    // Copia n bytes del molde a la salida. Sin molde, escribe ceros.
    void copyMolde(InputStream molde, long n, byte[] buf) throws IOException {
        if (n < 0) throw new IOException("Manifiesto desordenado");
        while (n > 0) {
            int len = (int) Math.min(buf.length, n);
            if (molde == null) {
                java.util.Arrays.fill(buf, 0, len, (byte) 0);
            } else {
                len = molde.read(buf, 0, len);
                if (len < 0) throw new IOException("Molde corto");
            }
            out(buf, 0, len);
            n -= len;
        }
    }

    void writeCmd(Cmd c, byte[] buf) throws IOException {
        if ("copy".equals(c.mode)) {
            long off = c.srcOff, n = c.len;
            while (n > 0) {
                int len = (int) Math.min(buf.length, n);
                readFully(c.src, off, buf, len);
                out(buf, 0, len);
                off += len; n -= len;
            }
        } else if ("dxt1".equals(c.mode) || "dxt5".equals(c.mode)) {
            boolean five = "dxt5".equals(c.mode);
            int bs = five ? 16 : 8;
            int bw = Math.max(1, (c.w + 3) / 4), bh = Math.max(1, (c.h + 3) / 4);
            byte[] src = new byte[bw * bs];
            byte[] row = new byte[c.w * 4 * 4];
            int[] px = new int[16];
            long off = c.srcOff;
            for (int by = 0; by < bh; by++) {
                readFully(c.src, off, src, src.length);
                off += src.length;
                for (int bx = 0; bx < bw; bx++) {
                    decodeBlock(src, bx * bs, five, px);
                    for (int y = 0; y < 4; y++) {
                        for (int x = 0; x < 4; x++) {
                            int X = bx * 4 + x;
                            if (X >= c.w) continue;
                            int p = px[y * 4 + x];
                            int o = (y * c.w + X) * 4;
                            row[o] = (byte) (p >> 16); row[o + 1] = (byte) (p >> 8);
                            row[o + 2] = (byte) p; row[o + 3] = (byte) (p >>> 24);
                        }
                    }
                }
                int rows = Math.min(4, c.h - by * 4);
                out(row, 0, rows * c.w * 4);
            }
        } else {
            throw new IOException("Modo desconocido " + c.mode);
        }
    }

    static int u16(byte[] b, int o) { return (b[o] & 255) | ((b[o + 1] & 255) << 8); }

    // Descomprime un bloque DXT1/DXT5 de 4x4 en ARGB
    static void decodeBlock(byte[] b, int o, boolean dxt5, int[] out) {
        int[] alpha = null;
        if (dxt5) {
            alpha = new int[8];
            int a0 = b[o] & 255, a1 = b[o + 1] & 255;
            alpha[0] = a0; alpha[1] = a1;
            if (a0 > a1) {
                for (int i = 1; i < 7; i++) alpha[i + 1] = ((7 - i) * a0 + i * a1) / 7;
            } else {
                for (int i = 1; i < 5; i++) alpha[i + 1] = ((5 - i) * a0 + i * a1) / 5;
                alpha[6] = 0; alpha[7] = 255;
            }
            long bits = 0;
            for (int i = 0; i < 6; i++) bits |= ((long) (b[o + 2 + i] & 255)) << (8 * i);
            for (int i = 0; i < 16; i++) out[i] = alpha[(int) ((bits >> (3 * i)) & 7)] << 24;
            o += 8;
        }
        int c0 = u16(b, o), c1 = u16(b, o + 2);
        int[] r = new int[4], g = new int[4], bl = new int[4], a = {255, 255, 255, 255};
        r[0] = ((c0 >> 11) & 31) * 255 / 31; g[0] = ((c0 >> 5) & 63) * 255 / 63; bl[0] = (c0 & 31) * 255 / 31;
        r[1] = ((c1 >> 11) & 31) * 255 / 31; g[1] = ((c1 >> 5) & 63) * 255 / 63; bl[1] = (c1 & 31) * 255 / 31;
        if (c0 > c1 || dxt5) {
            r[2] = (2 * r[0] + r[1]) / 3; g[2] = (2 * g[0] + g[1]) / 3; bl[2] = (2 * bl[0] + bl[1]) / 3;
            r[3] = (r[0] + 2 * r[1]) / 3; g[3] = (g[0] + 2 * g[1]) / 3; bl[3] = (bl[0] + 2 * bl[1]) / 3;
        } else {
            r[2] = (r[0] + r[1]) / 2; g[2] = (g[0] + g[1]) / 2; bl[2] = (bl[0] + bl[1]) / 2;
            r[3] = 0; g[3] = 0; bl[3] = 0; a[3] = 0;
        }
        long idx = (b[o + 4] & 255L) | ((b[o + 5] & 255L) << 8) | ((b[o + 6] & 255L) << 16) | ((b[o + 7] & 255L) << 24);
        for (int i = 0; i < 16; i++) {
            int k = (int) ((idx >> (2 * i)) & 3);
            int rgb = (r[k] << 16) | (g[k] << 8) | bl[k];
            if (dxt5) out[i] = (out[i] & 0xFF000000) | rgb;
            else out[i] = (a[k] << 24) | rgb;
        }
    }

}
