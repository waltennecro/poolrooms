# Nivel 11 dreamcore y provisiones

La ciudad conserva sus calles transitables, vehículos, aeródromos, edificios importados y tres edificios con interiores y escaleras por manzana. El cuarto solar no importado pasa a ser un pabellón público abierto: soportales, jardín con pérgola o galería de espera. Hay arcos profundos, huecos solares reales, franjas de vidrio, bancos y pequeños jardines. Los acabados salvia/arcilla usan los mapas PBR existentes; el sol regional existente recibe una orientación e intensidad de tarde, sin añadir otro sol ni alterar los controles de ray tracing.

## Comida real en el mundo

**103 tipos de comida, con 117 presentaciones, y 8 bebidas** tienen IDs propios `food:0`…`food:124`. Agua de almendra: `food:124`. Proceden de **125 modelos distintos** del Food Kit 2.0 de Kenney, CC0. No se cuentan colores, instancias ni LODs como modelos nuevos. Se quitaron caras duplicadas del origen, se redondearon bordes y se adaptaron normales/UV; cada modelo tiene un LOD. En 124 modelos reduce triángulos; las papas fritas conservan su malla ya pequeña de 562 triángulos. Cuatro juegos originales compartidos de albedo/normal/máscara a 1024px y una etiqueta original completan los materiales. Son formas estilizadas mejoradas, no escaneos fotográficos ni una certificación visual AAA.

Las habitaciones de los **63 biomas** son elegibles. También están integrados ambos sistemas de centros comerciales por sector/piso y las casas detalladas del Nivel94. Una mesa de modelo licenciado puede aparecer con cuatro objetos, siempre con agua de almendra. La variedad se distribuye por semilla y coordenadas; no hay que visitar el modo maestro. El sistema exige suelo real, nueve muestras de apoyo, espacio de pie y volumen libre. Rechaza huecos, escalones, piscinas y pasos principales; **no promete una mesa en cada habitación**. Máximo 36 candidatos y una mesa por sector, construcción escalonada y cuerpos físicos dormidos hasta moverlos. La mesa también es movible: al retirarla, los alimentos que quedan empiezan a caer.

## Uso

- **E** sobre comida o bebida: guardar directamente en el inventario existente.
- **T**: tomar físicamente; **Z**: guardar lo que llevas; **Q**: soltar.
- **Tab**: inventario; usar/comer/beber desde su ficha. La barra existente se muestra u oculta con **5**.

Las unidades del mismo alimento se acumulan en una pila de precisión arbitraria. Se conservan cinco páginas de cincuenta espacios. No hay HUD nuevo, hambre, sed ni obligación de comer. Un inventario lleno sigue aceptando unidades de pilas existentes; no borra objetos de tipos nuevos que no caben. El inventario continúa siendo de sesión: la persistencia a disco sigue pospuesta.

## Instalación y comprobaciones

Actualiza la única carpeta `PoolroomsProcedural` dentro de `Assets`, conservando `.meta` y los demás recursos. Regenera la demo para sustituir geometría horneada. Los perfiles nativos y la exploración unificada habilitan `dreamCity11` y `enableProvisions`; los fixtures históricos de Core mantienen ambos desactivados.

`--city-provisions` verifica los programas públicos, suelo/paso, límites y determinismo, catálogo, distribución elegible e inventario. `python tools/verify_food_sources.py` comprueba licencias, hashes, geometría y mapas. Las pruebas nativas están en `ProvisionWorldTests`. **No hay Editor Unity en este entorno**: importación nativa, shaders/HDRP, física de recogida/apoyo, aspecto y FPS requieren PlayMode. Las comprobaciones del núcleo y del código no sustituyen esa revisión.

Investigación: The Backrooms Wiki, Level11, Stretchster/Boring Talking; se consultó un espejo inglés fijado por revisión porque el sitio original respondió 403. No se copian fotos ni pasajes. La adaptación dreamcore es original; referencias y límites en `Documentation/References/CityDream11/research.json` del repositorio. Recursos y licencias completos en Créditos y `FoodSources/Licenses`.
