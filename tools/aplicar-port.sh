#!/bin/bash
# Convierte el proyecto extraído con AssetRipper (UnityProject/) en el port de Android.
# Se ejecuta desde la raíz del repositorio, después de extraer el proyecto.
set -euo pipefail
cd "$(dirname "$0")/.."

if [ ! -d UnityProject/Assets ]; then
  echo "No existe UnityProject/Assets: primero hay que extraer el proyecto del juego." >&2
  exit 1
fi

echo "1/3 Aplicando los cambios a los archivos del juego"
if ! git apply --whitespace=nowarn patches/dws-android.patch; then
  echo "El parche no encaja. ¿Es la versión de Steam del juego y se extrajo con AssetRipper 2.0.0?" >&2
  exit 1
fi

echo "2/3 Copiando los archivos propios del port"
cp -r port/. UnityProject/

echo "3/3 Añadiendo acentos, ñ, ¿ y ¡ a las fuentes del juego"
python3 tools/patch_fonts.py

echo "Port aplicado."
