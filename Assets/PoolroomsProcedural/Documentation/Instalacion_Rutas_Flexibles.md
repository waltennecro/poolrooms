# Instalación en carpetas anidadas

El archivo `Ceramic_Albedo.png` estaba incluido, pero Crear demo buscaba exclusivamente en `Assets/PoolroomsProcedural/Textures`. Extraer el ZIP dentro de otra carpeta de Assets provocaba FileNotFoundException. Otras rutas fijas impedían preparar correctamente los materiales de biomas y del Nivel 94.

Ahora el Editor localiza su propia instalación mediante un MonoScript anclado a `PoolroomsPackagePaths`. Texturas y bibliotecas de materiales se resuelven dentro de esa misma instalación, aunque tenga carpetas contenedoras, espacios o un nombre diferente. La caché se invalida cuando la carpeta anterior deja de existir. No busca una copia arbitraria por todo el proyecto ni traslada recursos automáticamente.

La plantilla de terreno ya generada por instaladores anteriores se reutiliza mediante Resources para no crear una segunda plantilla con el mismo nombre.

Crear demo comprueba las nueve texturas antes de sustituir la escena. Si falta realmente un archivo, el diagnóstico muestra su ruta real. No se inventan texturas de reemplazo.

## Actualizar

El parche acumulativo contiene diez archivos de producción: los cinco del arreglo anterior de compilación y cinco de Editor para las rutas. Con Unity cerrado, copia Core, Runtime y Editor del parche **dentro de tu carpeta PoolroomsProcedural existente**, sobrescribiendo los archivos correspondientes. Conserva los demás archivos y sus .meta. No agregues otra copia del código.

También se proporciona un paquete completo. Mantén una sola copia de PoolroomsProcedural dentro de Assets. No necesitas borrar tus escenas ni volver a importar las texturas para aplicar el parche.

Como solución inmediata sin parche, mueve PoolroomsProcedural directamente a Assets desde la ventana Project de Unity para que conserve las referencias.

## Verificación

El localizador real se compila fuera de Unity con sustitutos reducidos de AssetDatabase y ScriptableObject; se ejercitan cuatro instalaciones físicas y sus cuatro movimientos, rutas Unicode, caché, archivos ausentes y limpieza. Se revisa que el Editor no contenga rutas de instalación fijas y que las nueve texturas estén incluidas. Se conservan los contratos de tipos del arreglo anterior y se añaden pruebas EditMode nativas, pendientes de ejecución.

No cambia el generador, Runtime, recursos ni shaders. La compilación e importación nativas de Unity/HDRP siguen pendientes; las pruebas reducidas no las sustituyen.
