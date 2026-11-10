#!/bin/bash
# Se ejecuta dentro del contenedor unityci/editor.
# Activa una licencia Unity Personal con la cuenta (como hace game-ci cuando
# el .ulf no coincide con la máquina), compila y devuelve la licencia al final.
LC=/opt/unity/Editor/Data/Resources/Licensing/Client/Unity.Licensing.Client
mkdir -p /work/logs

if "$LC" --help 2>&1 | grep -q -- '--include-personal'; then
  echo "Activando con el cliente de licencias" | tee /work/logs/unity-activate.log
  "$LC" --activate-all --include-personal --username "$UNITY_EMAIL" --password "$UNITY_PASSWORD" >> /work/logs/unity-activate.log 2>&1
  return_license() {
    echo "Devolviendo licencia" >> /work/logs/unity-activate.log
    # La plaza Personal es de tipo "entitlement": se devuelve a través del editor con la cuenta
    unity-editor -batchmode -nographics -quit -returnlicense -username "$UNITY_EMAIL" -password "$UNITY_PASSWORD" \
      -projectPath /work/UnityProject -logFile /work/logs/unity-return.log > /dev/null 2>&1 || true
    grep -iE "return|seat|licen" /work/logs/unity-return.log | tail -15 >> /work/logs/unity-activate.log
    rm -f /work/logs/unity-return.log
  }
else
  echo "Activando a través del editor" | tee /work/logs/unity-activate.log
  unity-editor -batchmode -nographics -quit -username "$UNITY_EMAIL" -password "$UNITY_PASSWORD" -logFile - >> /work/logs/unity-activate.log 2>&1
  return_license() { unity-editor -batchmode -nographics -quit -returnlicense -username "$UNITY_EMAIL" -password "$UNITY_PASSWORD" -projectPath /work/UnityProject -logFile - >> /work/logs/unity-activate.log 2>&1 || true; }
fi
trap return_license EXIT

unity-editor -batchmode -nographics -projectPath /work/UnityProject -buildTarget Android \
  -executeMethod BuildAndroid.Build -logFile /work/logs/unity-build.log

