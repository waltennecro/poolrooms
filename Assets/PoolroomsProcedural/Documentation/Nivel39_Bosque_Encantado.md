# Nivel 39 · Bosque encantado

Adaptación tranquila de **Level 39 — Enchanted Forest**, de **Ferraru**, The Backrooms Wiki, CC BY-SA 3.0. La investigación fijada, autoría y diferencias deliberadas constan en `LevelResearch/Level39.json`. No se copian fotografías ni pasajes de la wiki. Se conservan el bosque de robles, los senderos sinuosos, musgo, ramas, setas, pequeños estanques y luz de crepúsculo; se omiten hipnosis, hambre letal y tormentas del límite.

## Explorar

Actualiza tu única carpeta `PoolroomsProcedural` con Unity detenido y regenera la demo. Exploración unificada habilita **EnableForest39** automáticamente. Los perfiles personalizados pueden desactivarlo; también existe el modo dedicado `EnchantedForest39`. Es el bioma añadido 64: conserva los 63 identificadores anteriores y consume una antigua repetición de la distribución caminable.

La región comparte suelo y caminos entre sectores. Hay doce programas de claros, senderos curvos, paseos circulares, colinas, refugios, bancos, herbario, lugares para leer y miradores con escalones humanos. Los robles emplean tres LOD; los troncos físicos permanecen iguales al cambiar el detalle visual. No cambia la distancia de renderizado.

## Usar

**E** permite recoger hojas y bellotas, leer el cuaderno con caminos reales, sentarse y ajustar cuatro estudios de hojas, equilibrio, orientación y agua. Las fichas terminadas se acumulan en el inventario; puedes volver a sacar los tres recuerdos como objetos físicos. También hay comida y agua de almendra sobre una mesa con licencia. No añade hambre, sed ni HUD obligatorio. Progreso y objetos pertenecen a la sesión; persistencia en disco sigue pendiente.

## Materiales y sonido

Tres escaneos ambientCG CC0 conservan su resolución original: suelo 4096², corteza 2230×4096 y roca 4096², con color, normal, máscara HDRP y relieve. Ground037 ya existía a 2K; se usa aquí su fuente nativa 4K. El follaje recortado usa otro escaneo CC0 de 512² sin ampliar: hojas caducifolias adaptadas a ramas de roble originales, no una réplica botánica exacta. La vegetación pequeña conserva geometría original. No hay archivos de relleno, modelos descargados nuevos ni texturas ampliadas artificialmente.

Los estanques tienen fondo real, profundidad menor de medio metro y superficie transparente recortada contra los triángulos de la orilla. Reutilizan el agua cristalina existente. HDRP reutiliza el único sol regional y la configuración de reflejos/ray tracing disponible; necesita hardware y plataforma compatibles.

Cuatro clips originales CC0 de viento/hojas emplean dos fuentes por oyente, posiciones en árboles cercanos, mezcla gradual, volumen, pausa y filtro bajo el agua. No contienen voces, susurros, animales, sobresaltos ni música forzada.

## Verificación

`--forest39` comprueba generación, bordes, apoyos, rutas, programas, agua, LOD, inventario y estados. `python tools/verify_forest39_resources.py` audita licencias, hashes, dimensiones, alfa y audio. Las pruebas nativas están en `Forest39PlayModeTests.cs`. Las vistas de Blender son inspecciones técnicas, no capturas del juego. **Unity/PlayMode, HDRP, aspecto, ray tracing, física nativa, escucha y FPS requieren probarse en Unity.**
