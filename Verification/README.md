# Comprobaciones fuera de Assets

Requisitos: SDK .NET **8.0.408**, fijado por `global.json`. No hay dependencias NuGet: el compilador de sintaxis procede del propio SDK. Instalar SDK 8 sin fijar su selección permitía que CI usara Roslyn de SDK 10, incompatible con net8.0; el workflow comprueba la versión elegida.

```sh
dotnet run --project Verification/Poolrooms.Checks.csproj
python tools/package_assets.py --stage R1 --output /tmp/Poolrooms_R1_Desde_v39.zip
```

El ejecutable compila el núcleo real con C# 9; compara 108 geometrías de suelo/colisiones infantiles y 80 geometrías de otros biomas con huellas tomadas antes de modificar v39. Comprueba los búferes de las mallas, los lotes de acabado, los colores de luces y la sintaxis de todos los scripts bajo cuatro configuraciones. El modo `--capture-baseline` se utilizó únicamente sobre la base original; no regenerar las huellas para ocultar regresiones.

Esto no compila las APIs de Unity ni sustituye las pruebas PlayMode, PhysX o el render en Unity 6.3/HDRP 17.3. No copiar esta carpeta a `Assets`: usa Roslyn, que no forma parte del juego.

Los mapas nuevos se regeneran con `python tools/generate_play_finish.py` (NumPy y Pillow); su procedencia y SHA-256 se guardan en `play_finish_textures.json`.
