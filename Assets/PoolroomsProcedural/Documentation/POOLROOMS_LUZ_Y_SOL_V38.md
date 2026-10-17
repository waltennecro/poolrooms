# v38 — Poolrooms con luz tenue y sol físico HDRP

Entrega acumulativa sobre v37. Aparece en la exploración normal de tu escena; no se añade otra demo ni se necesita un plugin.

## Qué cambia

Las Poolrooms azules, cian, parcialmente apagadas, de fluorescentes aislados, luz guía y pozos fríos reciben más potencia y alcance en sus lámparas. Mantienen sus colores y paneles apagados. Una sola luz amplia de apoyo por habitación, situada bajo el techo alto, aporta visibilidad en el centro y las aproximaciones a las salidas. Su alcance y tamaño caben dentro de la habitación para evitar atravesar las paredes sin sombras. Aporta luz difusa y no crea el reflejo de una lámpara flotando en el agua. Usa el presupuesto de luces existente y no añade geometría, sondas ni sombras propias. En las regiones Dim del atlas se refuerzan también las lámparas y se distribuyen en dos sectores de la sala.

Las Poolrooms solares reciben un Light direccional HDRP real y un Physically Based Sky: disco solar, halo moderado, atmósfera azul, luz difusa o tonos cálidos según la región. Se orienta con la dirección usada al generar las ventanas y tragaluces. Las sombras de paredes, arcos, columnas y celosías dejan pasar la luz por las aberturas construidas. La exposición fija de la zona solar compensa la intensidad física, conserva el ajuste de brillo y evita que el cambio dependa de encender la linterna. La luz de visibilidad del modo maestro usa esa exposición solar para conservar su efecto. El ajuste visual definitivo requiere probar las imágenes dentro de Unity.

Hay como máximo un sol y un perfil de cielo propios por mundo. El sol consume una luz y una sombra del presupuesto existente. Cuando está activo se desactivan los focos solares de sustitución para evitar iluminar dos veces las mismas aperturas. Al entrar en una Poolroom cerrada se desactivan este sol y su perfil; centros comerciales y Nivel 94 conservan sus sistemas. Si el sol del Nivel 94 ya está activo en una región próxima, se mantiene ese sol y la iluminación localizada existente, evitando dos soles globales simultáneos. Los focos de luz solar no se encienden sin sombras a través de paredes opacas.

La iluminación clásica continúa siendo una opción explícita de compatibilidad. Los colores, materiales y potencias base de los demás biomas no se han aumentado. El código de linterna, sus intensidades, movimiento y efectos de cámara permanece idéntico a v37. La distribución de salas, piscinas y conexiones también se conserva.

## Calidad gráfica

- Rendimiento: sol con sombras convencionales a 1024, luces de apoyo y reflejos existentes; sin DXR. Conserva la limitación de volumetría e iluminación indirecta de este modo.
- Alto: sombras solares a 2048, SSR/SSGI y volumetría cuando el HDRP Asset y tus ajustes los permiten.
- Cinemático RT: en equipos compatibles solicita sombras solares RT con 8 muestras y filtrado, reflejos con 4 muestras e iluminación indirecta con 4 muestras y hasta 3 rebotes en el modo Quality de HDRP. HDRP Performance puede usar sus propios límites. Los objetos del mundo infinito usan DynamicTransform para que las estructuras de ray tracing sigan el desplazamiento del origen.

El diagnóstico exige HDRP activo, Direct3D12, hardware/controlador con RT y las funciones correspondientes del Asset. Las sombras solares RT requieren además Screen Space Shadows en el Asset y la cámara. Si ese ajuste falta, se usan sombras solares convencionales y se indica en el estado de Gráficos. La función existente «Preparar HDRP Asset (agua y reflejos)» habilita Screen Space Shadows si el Asset ya tiene RT activado; no cambia la API gráfica ni fuerza DXR. Para configurar una GPU compatible usa el HDRP Wizard y comprueba Rendering Debugger. El agua sigue utilizando el sistema nativo HDRP, SSR y las sondas existentes; no se afirma que su malla participe en los reflejos DXR.

En el i3 N305 con gráficos integrados, empieza con Rendimiento. Conservas el sol físico y las sombras sin exigir ray tracing. F7 mantiene su acceso a Gráficos > Renderizado. No se han aumentado los presupuestos de chunks, luces o sondas; no se promete una mejora de FPS sin medirla.

## Cómo actualizar

1. Sal de Play y cierra Unity. Extrae el ZIP en Assets: la carpeta final debe ser Assets/PoolroomsProcedural.
2. Sobrescribe los archivos de esa carpeta y conserva los .meta de tu proyecto. Evita mantener otra copia de los mismos scripts dentro de Assets. El ZIP no contiene los proyectos .NET de Verification.
3. Abre tu escena habitual y juega. PoolroomsHDRP instala automáticamente PoolroomsSolarAtmosphere durante Play. No hace falta recrear la demo ni activar herramientas separadas para encontrar estas salas.
4. Si antes elegiste iluminación clásica, usa Tools > Poolrooms > Iluminación > Ambientes por zona (regenerar al jugar) para adoptar la iluminación nueva. Deja al menos una luz y una sombra disponibles para el sol direccional.

## Comprobaciones

El núcleo .NET compila sin errores ni advertencias. Se verifican las 170 zonas originales, 1020 combinaciones de zonas oscuras, 240 salas del atlas, cinco climas, límites físicos de la luz de apoyo, conservación de la geometría y colores, iluminación clásica y aislamiento de los demás biomas. Las suites existentes recorren las 768 recetas solares, accesos, conexiones y tamaños, distribución común original/solar y tragaluces de centros comerciales de 2–6 pisos. El diagnóstico RT prueba las 64 combinaciones de capacidades.

Se parsean 160 scripts en cuatro configuraciones de símbolos C# 9 y se revisan los seis asmdef. Se comprobaron 21 declaraciones/referencias nuevas de API contra las fuentes oficiales de HDRP 17.3.0. Eso no sustituye la compilación con las DLL de Unity ni un render. Resultados exactos: VERIFICACION_V38.txt.

Este entorno no tiene Unity Editor: compilación Unity/HDRP, shaders, apariencia, PhysX y FPS siguen pendientes. Para comprobarlo en tu PC, recorre una Poolroom oscura con la linterna apagada; observa después una sala solar y sus sombras junto a un tragaluz; cambia Rendimiento/Alto/RT y el brillo; entra a una zona cerrada, un centro comercial y Nivel 94; comprueba que el sol nuevo se apaga al cambiar de ámbito. Repite el cambio de Play y los presupuestos de sombras 0/1/2; verifica que no quedan soles o perfiles duplicados ni errores en la consola. En Rendering Debugger comprueba qué efectos RT se renderizan realmente. Mide FPS con tus ajustes habituales y registra la resolución y los presupuestos utilizados.
