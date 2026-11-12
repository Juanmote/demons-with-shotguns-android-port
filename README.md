# Demons with Shotguns – port para Android

Port no oficial de **Demons with Shotguns** (Mindshaft Games) para consolas y móviles Android (arm64),
traducido al castellano y con controles físicos, táctiles, vibración, logros y extras.

**Este repositorio no contiene el juego.** Para jugar necesitas tu propia copia de
[Demons with Shotguns en Steam](https://store.steampowered.com/app/370940/Demons_with_Shotguns/).
Cómpralo y apoya a sus creadores: <https://www.mindshaftgames.com>

Port: **Juanmote**. Juego original: **Mindshaft Games, LLC**. Este proyecto no está afiliado a Mindshaft Games.

## Qué trae el port

### Adaptación a Android
- APK arm64 (Android 7 o superior), horizontal y a pantalla completa con la resolución nativa; se ajusta a cualquier relación de aspecto.
- 60 fps.
- Las partidas y opciones se guardan en la consola (sin Steam).
- Gráficos corregidos para Android (shaders reconstruidos).
- Icono propio (normal, redondo y adaptativo).

### Mandos físicos
- Funciona con el mando integrado de las consolas y con mandos externos.
- Perfil genérico para cualquier mando: L1/R1 = Embestida/Golpe, gatillos = Escudo/Disparo.
- Botones **C** y **Z** de la Retroid Pocket Classic: C = Embestida, Z = Golpe.
- Select = Burla (además del clic del stick derecho).
- Probado en AYN Odin 3 y Retroid Pocket Classic.

### Controles táctiles
- Aparecen al tocar la pantalla, semitransparentes, y se ocultan al usar un mando.
- Sirven en menús, lobby y partida.
- Cruceta de 8 direcciones, Disparar, Saltar, Escudo, Golpe, Embestida, Granada, Pausa y Burla.
- Tres tamaños: Pequeño, Normal y Grande.

### Vibración
- Vibra la consola y también el mando si tiene motor: embestida, muerte, golpe, disparo, bloqueo y explosiones. Respeta la opción del juego.

### Sonido
- Limitador en la mezcla final: sin petardeo cuando suenan muchos efectos a la vez.

### Traducción al castellano
- Menús, opciones, lobby, mensajes de partida y aviso inicial traducidos.
- Fuentes del juego ampliadas con acentos, ñ, ¿ y ¡.
- Idioma seleccionable en Opciones (Español / English).

### Reinicio de partida
- «Reiniciar» y «Revancha» recargan la partida de forma limpia (sin el arreglo se rompían los gráficos al reiniciar).

### Extras del port (Opciones › Extras del port)
- **Logros**: los 16 de Steam con sus iconos, guardados en la consola, con aviso al conseguirlos y contador de progreso.
- **Imagen**: filtros Scanlines / CRT y colores Tele antigua / Vivos / Nocturna / Sepia / Pantalla verde.
- **Controles táctiles**: tamaño de los botones.
- **Trucos**: munición infinita, invulnerable y vidas infinitas. Con cualquier truco activo no se consiguen logros.
- **Copia de seguridad**: exporta e importa en un archivo `.dws` las opciones, controles, avance y logros.

### Diagnóstico
- Si algo falla, el archivo `Android/data/com.juanmote.demonswithshotguns/files/dws-diag.txt` recoge el equipo, los mandos y los errores. Adjúntalo al avisar del problema.

## Instalación

1. Descarga `DemonsWithShotguns.apk` de la última versión en **[Releases](../../releases/latest)** e instálala.
2. Copia a la consola la carpeta del juego de Steam (la que contiene `dws_Data`). En Steam: clic derecho en el juego › Administrar › Ver archivos locales.
3. Abre el juego, pulsa **Seleccionar carpeta del juego** y elige esa carpeta (vale también la propia `dws_Data`).
4. Espera a que se preparen los datos (necesita unos 450 MB libres). Solo se hace la primera vez; al actualizar la APK se repite solo.

La APK no lleva gráficos, sonido, música ni fuentes del juego: los coge de tu copia de Steam al prepararlos.
Hace falta la versión de Windows actual de Steam; si tus archivos no coinciden, el juego avisa.
Después puedes borrar la carpeta copiada.

## Alternativa: compilar tu propia APK

Todo se hace desde la web de GitHub; no hace falta instalar nada. La APK se compila en los
servidores de GitHub (gratis) con tus archivos del juego.

1. **Haz una copia privada de este repositorio.** Pulsa **Use this template › Create a new repository**
   y marca **Private**. Tiene que ser privada: los archivos del juego no se pueden publicar
   (el proceso se niega a funcionar en un repositorio público).
2. **Crea una cuenta gratuita de Unity** en <https://id.unity.com> si no la tienes. Hace falta para compilar.
3. En tu copia: **Settings › Secrets and variables › Actions › New repository secret** y crea:
   - `UNITY_EMAIL`: el correo de tu cuenta de Unity
   - `UNITY_PASSWORD`: su contraseña
4. **Sube los archivos del juego.** En Steam: clic derecho en el juego › Administrar › Ver archivos locales.
   Comprime en un `.zip` o `.7z` todo lo que hay en esa carpeta (`dws.exe`, `dws_Data`, …) y súbelo de una de estas formas:
   - **Recomendado:** en tu copia, **Releases › Create a new release**, etiqueta `juego`, y adjunta el archivo. Admite archivos grandes.
   - Si pesa menos de 25 MB por archivo (por ejemplo, un 7z en volúmenes): súbelo a la carpeta `game/`.
5. **Compila:** pestaña **Actions › Compilar APK › Run workflow**. Tarda entre 20 y 40 minutos.
6. La APK aparece en **Releases** (`Demons with Shotguns Android 1.0.x`). Descárgala desde la consola e instálala.

Si la compilación falla, descarga los **registros** de esa ejecución (al final de la página de la ejecución) y mira `errores.txt` y `licencia.txt`.

Opcional: para firmar siempre con la misma clave (y poder actualizar sin desinstalar), añade los secretos
`ANDROID_KEYSTORE_BASE64` (tu keystore en base64), `ANDROID_KEYSTORE_PASS` y, si el alias no es `dws`, `ANDROID_KEY_ALIAS`.

## Cómo funciona la APK de Releases

- Se compila con Unity en modo *split binary*: la APK solo lleva el código y una escena de arranque vacía, y el resto de datos va en una OBB.
- `tools/make_generic.py` convierte esa OBB en un «molde» con todo lo que viene de Steam (texturas, audio y fuentes) puesto a cero, más un manifiesto con las órdenes para rellenarlo. Ambos van dentro de la APK.
- Al abrirla, `DwsSetupActivity` pide la carpeta del juego y `DwsObbBuilder` rellena el molde con tus archivos de Steam (comprobando cada archivo con su CRC) y genera la OBB en `Android/obb/com.juanmote.demonswithshotguns/`.
- Solo quedan dentro de la APK la estructura de los niveles y los gráficos propios del port (controles táctiles e iconos de logros).

## Cómo funciona por dentro (compilación propia)

1. Se extrae el proyecto Unity de tu copia del juego con [AssetRipper](https://github.com/AssetRipper/AssetRipper) 2.0.0.
2. `tools/aplicar-port.sh` lo convierte en el port:
   - aplica `patches/dws-android.patch` (los cambios hechos a los archivos del juego),
   - copia `port/` (todo el código y los gráficos propios del port),
   - añade los caracteres del castellano a las fuentes del juego (`tools/patch_fonts.py`).
3. Se descargan de Steam los iconos de los logros.
4. Se compila con Unity 2021.3 (IL2CPP, arm64) y se publica la APK en Releases.

El port se hizo con la versión de Steam actual del juego; si tu copia es distinta, el proceso avisa.

| Carpeta | Contenido |
|---|---|
| `port/Assets/Scripts/Assembly-CSharp/` | Código propio: controles táctiles (`DwsTouchControls`), menú de extras (`DwsOverlay`), logros, trucos, filtros, copia de seguridad, limitador de sonido, reinicio de escena, traducción (`Loc`), vibración |
| `port/Assets/Plugins/Android/` | Actividad Android (botones C/Z, vibración, selector de archivos de la copia de seguridad) y pantalla de arranque que genera los datos con tu copia de Steam (`DwsSetupActivity`, `DwsObbBuilder`) |
| `port/Assets/Resources/` | Gráficos de los controles táctiles y shader de filtros |
| `patches/` | Cambios a los archivos del juego extraído |
| `tools/` | Aplicación del port, parche de fuentes, script de compilación y generador del molde de la APK de Releases (`make_generic.py`, `texformats.tsv`) |
