# R18 — Reconstrucción y movimiento más tolerante

Esta etapa mejora la reconstrucción acumulativa R1–R17. No representa una
recuperación exacta de los archivos v40–v47 que no están disponibles.

## Cambios

- Tolerancia de 100 ms tras abandonar un borde: ese salto cuenta como el
  primero y conserva el segundo salto. Fuera de ese margen solo queda el salto aéreo.
- Memoria de pulsación de 120 ms cuando ambos saltos están gastados: si se
  aterriza a tiempo, la pulsación se ejecuta una sola vez.
- Pausa, chat y otras interfaces descartan la memoria de salto sin cambiar
  la velocidad vertical. Carga, teletransporte y entrada al agua también
  limpian los estados correspondientes.
- El contacto de suelo durante el ascenso no concede saltos adicionales.
- La reparación de una posición incrustada en el suelo comprueba ahora la
  cápsula completa de destino: no empuja al jugador hacia un techo bajo o
  un obstáculo. Consultas físicas saturadas se consideran inconclusas.
  Se usan buffers fijos y solo se consulta al reparar, sin coste por sala.
- Sin cambios de geometría, distancia de renderizado, calidad gráfica,
  materiales, catálogo, linterna ni IDs serializados.

## Verificación

Se compila el núcleo real y se comprueban huellas de geometría protegidas y
sintaxis C# en cuatro configuraciones mediante:

```
dotnet run --project Verification/Poolrooms.Checks.csproj
```

Resultado local: **24.043.831 aserciones**, 188 huellas protegidas y
202 scripts con sintaxis válida en las cuatro configuraciones. Las aserciones
son comprobaciones matemáticas del núcleo, no tests nativos de Unity.

Los casos nuevos comprueban el presupuesto de dos saltos, consumo único,
expiración de ambas tolerancias, agua y conservación de velocidad al abrir
interfaces a 20, 60 y 240 pasos por segundo.

Se añadió una regresión PlayMode con suelo y techo reales para la reparación
de cápsula; está escrita pero no ejecutada.

No se ha ejecutado Unity: quedan pendientes compilación de APIs HDRP,
PlayMode, recorrido, sensación del movimiento, shaders, física y FPS.
Para probar en Unity, importar únicamente `Assets/PoolroomsProcedural` en
un proyecto Unity 6.3/HDRP 17.3 y ejecutar los tests PlayMode existentes.
