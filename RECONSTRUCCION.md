# Reconstrucción desde v39

Este trabajo reconstruye funciones solicitadas después de v39. No recupera exactamente los archivos desaparecidos de v40–v47 ni presupone que su apariencia hubiera sido validada. Base original: `20311f824ebda9ecbf553f28915f100e8e252995`; inventario inmutable de origen: `RECUPERACION_V39.json`.

## Lo que ya contiene v39

- Generación por semilla, streaming y mundo conectado; Poolrooms originales, solares y amplias; interiores de oficinas, hotel, espera y comedor; supermercados pequeños y centros comerciales completos de varios pisos.
- Nivel 94, variantes de casas, interiores e interacción, pasto con ajustes de calidad; biomas infantiles anteriores y el nuevo SoftPlay multinivel transitable.
- Agua HDRP, iluminación regional, Rendimiento/Alto/Cinemático RT con alternativa compatible, cámara y destellos, sonido, pausa, configuración y acceso F7 a renderizado.
- Modo maestro/chat, vuelo y TP, linterna, objetos agarrables, doble salto y deslizamiento.

## Funciones posteriores: inventario y estado

| Solicitud del historial | Contraste con v39 / siguiente trabajo | Estado |
| --- | --- | --- |
| Acabado visual del nuevo bioma infantil (v40 mencionada) | R1: 21 mapas originales PBR de 1024 px; redes oscuras separadas, fundas de postes, esferas con normales suaves, piso continuo y tonalidad coherente de luminarias. | Implementado; validación visual Unity pendiente |
| Calidad gráfica adicional y RT mejorado sin quitar los modos previos | R2 añade Ultra: hasta 8 muestras, dos rebotes de reflejos y cuatro de GI; alternativa SSR/SSGI sin RT, sombras 2048, sin cambiar API gráfica. | Implementado; render Unity pendiente |
| Nivel 11: ciudad extensa, 300 edificios/zonas, 500 espacios, 400 muebles, 2000 objetos y 35 vehículos, incluido avión/helicóptero; recursos en línea con licencia | No existe City11 en v39. Los números no prueban modelos distintos ni interacción. Requiere catálogo con procedencia/licencias, generación, interiores, controles y pruebas por partes. | Pendiente |
| Nivel 94 más compacto y más pasto con tres opciones | Ya hay casas/pasto/ajustes; comparar densidades y mejorar cobertura sin multiplicar cuerpos físicos. | Pendiente |
| Mejor física al caer objetos | R2 añade contactos por material, menor resistencia al aire, interpolación, CCD, solver y herencia limitada del movimiento al soltar. | Implementado; PhysX pendiente |
| Dash con Shift y correr con `|` | R2: tres cargas, recarga, avance planar con colisiones y gravedad originales; cambio en ambos sistemas de entrada y ayuda. | Implementado; controles nativos pendientes |
| 150 % más contenido y 100 % más decoración realista global (v47 mencionada) | No hay sistema de decoración global ni catálogos City11. Definir mediciones respecto de esta base; los porcentajes no implican igual coste ni garantizan variedad. | Pendiente |
| Biblioteca inspirada en 906, patio residencial 188 y costa 48; entradas naturales; preparar entradas antiguas sin modificarlas aún | Solo existen diseño y tres modelos fuente en `Preparacion_V48`. No hay niveles integrados. City11 aún no existe como origen de transición. | Pendiente |

No se puede determinar el reparto exacto de funciones entre los números v41–v47, ni recuperar sus pruebas o apariencia originales. Las cifras recordadas de catálogos de versiones perdidas sirven como contexto, no como código recuperado ni requisitos nuevos.

## Reglas de continuidad

Exploración tranquila, sin muerte, enemigos, caras, ojos, hongos ni desafíos. Conservar piscinas, escaleras, variedad, iluminación propia por bioma, modos económicos, configuración y controles salvo el cambio expresamente pedido. No modificar la linterna. Añadir IDs serializados al final. Respetar los recorridos, suelo y carga segura antes de activar transiciones.

Cada etapa debe registrar archivos, pruebas ejecutadas, límites, commit y paquete reproducible; respaldar en GitHub sin reescribir historial. `tools/verify_recovery.py` comprueba la copia **original**, no una versión modificada: conservar el manifiesto original y publicar manifiestos nuevos para reconstrucciones.

## Verificación disponible

Integridad de los 326 archivos v39 comprobada nuevamente. Unity Editor no está instalado y faltan `Packages`/`ProjectSettings`: todavía no hay compilación Unity/HDRP, importación de shaders, PhysX, capturas reales ni medición de FPS. Los resultados históricos incluidos en v39 no cuentan como pruebas ejecutadas nuevamente.

### Etapa R1

Cambios reales en `Core/SoftPlayArchitecture.cs`, temas y materiales independientes, importación de mapas y carga de dos lotes visuales por sala. Se conservan los identificadores anteriores, las bibliotecas personalizadas, los apoyos, las escaleras, las barreras y la linterna. El detalle del piso usa dos triángulos; las redes respetan la calidad de curvas del perfil.

Verificación nueva: núcleo compilado con .NET 8/C# 9; comparación contra las huellas v39 de 108 salas infantiles y 80 muestras de otros biomas; validación de mallas, colores de fuentes y sintaxis de los scripts. Mapas nuevos reproducibles mediante `tools/generate_play_finish.py`, paquete completo mediante `tools/package_assets.py`. No equivale a una certificación de acabado AAA ni ausencia de errores visuales: falta ejecutar Unity.

### Etapa R2

Dash de 16 m/s durante 0,17 s, tres cargas y recarga de 0,8 s. No añade invulnerabilidad ni modifica el doble salto: el avance sigue pasando por CharacterController y los bloqueos de carga. Se cancela al pausar, entrar en menús, volar o esperar suelo. `|` mantiene la opción de alternar carrera; Input System busca la tecla en la distribución activa y usa barra inversa como alternativa. El gestor antiguo admite las posiciones habituales de barra, acento y tecla ISO.

Ultra se añade al final de la enumeración y conserva los tres modos previos. Solicita mayor muestreo solo cuando el hardware/API/Asset satisfacen la política existente. En GPU incompatible usa SSR/SSGI; no es una afirmación de RT ejecutado. Los rebotes configurados dependen del modo de trazado admitido por el HDRP Asset. El agua sigue usando su sistema HDRP nativo.

Los objetos sueltos reciben perfiles de contacto distintos para embalaje, rígidos, espuma, pelota y goma. Se conserva el límite de 32 objetos sueltos y la física se activa solo al soltar. Nuevas pruebas PlayMode de caída y pared incluidas, **no ejecutadas**. Verificación real: 9.754.412 comprobaciones del núcleo/mallas/huellas y sintaxis de 168 scripts en cuatro configuraciones. APIs de teclado contrastadas con Input System 1.11.2 y PhysicsMaterial con UnityCsReference 6000.3; esto no sustituye la compilación en Unity.

Respaldos ya publicados: `709436f` (R1) y `66be421` (exclusión de binarios de comprobación). Paquete R1 completo generado desde un árbol limpio: 347 archivos, 51.349.127 bytes, SHA-256 `5c619b2e028f0eccc62b3e163ab907bb0577025c85abd23bff2fa48997d3049f`. No incluye Roslyn ni herramientas dentro de Assets.
