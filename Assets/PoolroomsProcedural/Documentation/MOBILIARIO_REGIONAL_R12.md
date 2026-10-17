# Reconstrucción R12: mobiliario PBR en centros comerciales y nivel 94

El sofá de terciopelo y las dos tapicerías de silla ya adaptadas se incorporan a más zonas del mundo. No se descargan ni se cuentan modelos adicionales: se mantienen nueve fuentes/adaptaciones, ocho familias geométricas y una variante de silla. Autores, licencias y mapas originales siguen en los catálogos de R7/R10.

## Colocación e interacción

- **Centros comerciales refinados:** hasta dos asientos por sector y planta, sustituyendo parcelas de bancos existentes. Se orientan hacia el atrio, fuera de la galería central y dejando más de 80 cm ante el frente comercial. Cada esquina comprueba las placas de suelo para evitar huecos del atrio y escaleras. Funciona tanto en centros conectados como en el interior heredado.
- **Casas variadas del nivel 94:** hasta dos asientos por casa detallada. Sustituyen mobiliario provisional de salones o salas de espera en planta baja; nunca ocupan el módulo de escalera, sótanos o áticos. Las casas lejanas y las casas rectangulares antiguas conservan su implementación anterior.
- Una comprobación conservadora de los límites de triángulos rechaza colisiones dudosas con la arquitectura ya producida; permanece el banco/asiento procedural cuando no cabe el recurso. No es una simulación PhysX.
- E permite agarrar el mueble y Q soltarlo con la física existente. Su clave estable evita reponerlo al recargar su sector durante la sesión. Las casas revalidan las claves antes de publicar una sustitución de chunk para evitar duplicados durante la carga.

## Coste y compatibilidad

No se añaden luces ni scripts por cuadro a los muebles. Los recursos comparten mallas/materiales y sus LOD o distancia de ocultación. No tienen Rigidbody hasta soltarlos. El límite de dos muebles es por casa detallada o sector, **no un presupuesto global de instancias**: aumentar la distancia de renderizado puede aumentar el total cargado. El modo Rendimiento conserva los LOD existentes; memoria y FPS deben medirse en Unity.

Los IDs antiguos y las claves de productos se conservan. Los asientos de casas usan slots 8000–8015 y los del centro comercial un espacio de claves `source-seat` separado. No cambian linterna, entradas ni reglas de exploración pacífica.

## Comprobación

El núcleo comprueba 34 asientos en 36 casas generadas y 60 en 30 sectores de centros comerciales de varias plantas: soporte bajo nueve puntos, huecos, escaleras, mobiliario final, frentes y rutas. Se conservan las 188 huellas geométricas protegidas. Las pruebas nativas de penetración, importación y altura real de plantas están añadidas en `SourceFurnitureWorldTests`, pero no se han ejecutado sin Unity. No se certifican shaders ni apariencia mediante estas comprobaciones.
