# Comprobaciones fuera de Assets

Requisitos: SDK .NET **8.0.408**, fijado por `global.json`. No hay dependencias NuGet: el compilador de sintaxis procede del propio SDK. Instalar SDK 8 sin fijar su selección permitía que CI usara Roslyn de SDK 10, incompatible con net8.0; el workflow comprueba la versión elegida.

```sh
dotnet run --project Verification/Poolrooms.Checks.csproj
python tools/package_assets.py --stage R14 --output /tmp/Poolrooms_R14_Desde_v39.zip
```

El ejecutable compila el núcleo real con C# 9; compara 108 geometrías de suelo/colisiones infantiles y 80 geometrías de otros biomas con huellas tomadas antes de modificar v39. Comprueba los búferes de las mallas, los lotes de acabado, los colores de luces y la sintaxis de todos los scripts bajo cuatro configuraciones. El modo `--capture-baseline` se utilizó únicamente sobre la base original; no regenerar las huellas para ocultar regresiones.

Esto no compila las APIs de Unity ni sustituye las pruebas PlayMode, PhysX o el render en Unity 6.3/HDRP 17.3. No copiar esta carpeta a `Assets`: usa Roslyn, que no forma parte del juego.

`--runtime-types` compila expresiones reales de `AttachNotice`, `CreateTool` y `Distant` contra los tipos reales del núcleo, y comprueba el conflicto heredado de `animation` con un sustituto reducido de Component. Reproduce y rechaza los cuatro diagnósticos originales CS0200/CS1061/CS0266/CS0108. Ejecuta los radios de las tres herramientas, 4.096 ráfagas de pasos y estaciones a seis elevaciones de centro comercial/casa. También se ejecuta mediante `IntegrationSourceContracts`; no equivale a compilar todos los componentes ni a comprobar las APIs nativas de Unity.

Los mapas nuevos se regeneran con `python tools/generate_play_finish.py` (NumPy y Pillow); su procedencia y SHA-256 se guardan en `play_finish_textures.json`.

Para regenerar fuentes externas/adaptaciones se necesitan las herramientas fijadas:

```sh
python -m pip install -r Verification/requirements-art-tools.txt
python tools/check_uv_baker.py
python tools/import_city_sources.py
```

`--only SheenChair` permite regenerar una fuente conservando las adaptaciones existentes de las demás; todas las fuentes siguen comprobando licencia y SHA. CI ejecuta el caso sintético completo de UV, color y normales; audita los resultados de las fuentes reales sin descargar ni regenerar todos los modelos en cada commit. Eso no certifica la apariencia de HDRP.

Las auditorías de `verify_city_sources.py`, `verify_aircraft_sources.py`, `verify_helicopter_sources.py` y `verify_interior_sources.py` validan cada catálogo/licencia por separado. Los importadores de helicóptero/interiores conservan originales y referencias fijadas; no ejecutan JavaScript de terceros. `import_interior_sources.py` también usa el horneador UV y mantiene la silla damasco como variante de una familia existente. Nuevas pruebas PlayMode de muebles y aeronaves requieren Unity y no se ejecutan con dotnet.

R11 separa las ruedas del coche mediante `tools/rig_city_car.py`, invocado por `python tools/import_city_sources.py --only ToyCar`. La auditoría comprueba sus cuatro grupos en los tres LOD, centros y radios. R12 añade comprobaciones de soporte y espacio para mobiliario real en centros comerciales y casas, sin introducir dependencias de Unity en el núcleo.

R14 audita tres fuentes adicionales con `python tools/verify_urban_sources.py`. El horneador conserva colores lineales, tangentes originales y WebP; `source_lods.py` separa normales opuestas. `check_native_runner.py` usa un proceso falso para verificar estados/resultados y NO ejecuta Unity. Para la suite real y mediciones, leer `Assets/PoolroomsProcedural/Documentation/VALIDACION_NATIVA_R13.md`.

`python tools/compact_urban_sources.py` suelda únicamente tuplas idénticas de posición/normal/UV en las fuentes urbanas. La auditoría vuelve a calcular el hash de atributos por triángulo para comprobar que la compactación conserva la geometría. No usar esto como medición de FPS.
