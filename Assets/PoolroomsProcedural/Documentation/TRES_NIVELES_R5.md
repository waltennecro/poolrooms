# Tres ambientes tranquilos — reconstrucción R5

Esta etapa integra biblioteca 906, patio residencial 188 y costa 48 sobre R1–R4. Son interpretaciones procedurales, no reproducciones exactas de niveles canónicos ni recuperación de archivos desaparecidos.

## Explorar

Crear demo utiliza el perfil con `enableQuietLevels` y cuota regional `quietLevelShare = 0.18`. Los ambientes se seleccionan por semilla en regiones agrupadas; no tienen una secuencia obligatoria. También aparecen como destinos del TP maestro y modos individuales del generador. Las cuotas compiten con los demás ambientes: no significan que exactamente el 18 % de todas las habitaciones sean nuevas.

Los umbrales reales del grafo conectan los ambientes a cota cero: biblioteca mediante vestíbulos con arcos, patio mediante pasos bajo las viviendas y costa mediante pasos que desembocan en una pasarela seca. No se usa un portal negro ni se reemplazan los accesos anteriores. En las salas antiguas solo se guardan tres candidatos de transición inactivos; no abren agujeros, no teletransportan y no son una transición ya implementada.

## Arquitectura y objetos

- Biblioteca: sala central alta, estanterías, mesas, libros agarrables, dos galerías, dos escaleras de retorno con huecos reales, puentes y tragaluces de vidrio. Algunas variantes añaden columnas o bancos.
- Patio: viviendas amuebladas alrededor de un espacio abierto al cielo, de tres a cinco plantas, galerías protegidas, dos escaleras, puente posterior, camas con bastidor/almohadas, mesas, bancos y maceteros. Las primeras crujías quedan libres para los desembarcos.
- Costa: una línea de costa compartida por distrito, arena, pendiente hacia el fondo, agua registrada en el sistema HDRP existente y pasarelas transitables. Algunos tramos secos tienen un pabellón con banco, mesa, farol y botellas agarrables. No se repite una playa aislada por celda.

Las geometrías y tres mapas PBR de arena de 1024 px son originales; `tools/generate_quiet_sand.py` reproduce los mapas. Se reutilizan los catálogos existentes y las fuentes CC0 verificadas de R4. El farol ahora tiene colocación efectiva: siguen siendo **cuatro modelos descargados distintos**, tres objetos y un coche, con dos derivados LOD del coche. No se cuentan muebles procedurales, colores ni repeticiones como nuevos modelos externos.

## Luz y coste

Patio y ciudad comparten un único sol regional; la costa cambia su ángulo y tonalidad al atardecer. La biblioteca usa luminarias cálidas y luz de sus tragaluces. La exposición de estos ambientes se adapta suavemente según refugio geométrico; la linterna permanece intacta. RT sigue sujeto al hardware y al HDRP Asset, con la alternativa previa sin RT. No hay una sonda de reflejos ni una simulación de agua adicional por sala. Los acabados usan temas propios y conservan los IDs anteriores.

## Comprobaciones ejecutadas

Núcleo compilado con SDK 8.0.408/C# 9; 72 salas de tres tamaños, 300 apoyos de objetos, altura libre sobre peldaños, continuidad de costa, agua/pasarelas y candidatos inactivos comprobados. Se conservan 108 huellas infantiles y 80 de otros ambientes. Resultado acumulativo: 16.290.728 aserciones, sintaxis de 177 scripts en cuatro configuraciones, 24 mapas PBR y cuatro fuentes CC0 auditadas.

Se incluyen pruebas PlayMode de subida de escaleras y cruce sobre el mar, **no ejecutadas**. Faltan compilación de APIs Unity/HDRP, importación de shaders/materiales, PhysX, recorrido real, imágenes, memoria y FPS. No se certifica calidad AAA ni ausencia de errores visuales.
