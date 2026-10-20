# Agua cristalina

La actualización mejora automáticamente las apariencias claras de piscinas, costa, baños termales y parque acuático. No hace falta crear otra demo. En **Ajustes → Gráficos → Apariencia del agua → Cristalina** puedes elegir un acabado aún más neutro y transparente. «Cerámica clara», «Turquesa suave» y «Porcelana azul» también dejan ver mejor el fondo. La apariencia «Personalizada» mantiene sus colores, absorción y refracción manuales.

Se reduce la absorción por canal y la dispersión que daba al agua un aspecto verdoso o lechoso. La profundidad sigue atenuando el fondo; la refracción es más suave, las ondas de viento menos agitadas y los reflejos conservan su respuesta Fresnel. El agua no emite luz ni aclara artificialmente una piscina sin iluminación.

El material ligero, usado sin Water nativo de HDRP, tiene menor opacidad, microondas suaves y reflexión de las sondas existentes. Limpia mapas y emisión heredados de materiales antiguos, no proyecta sombras opacas y se ve desde ambos lados. Si falta el material de previsualización, crea uno compatible con el pipeline activo. Sigue siendo una aproximación Lit; no equivale a la refracción por profundidad de Water de HDRP.

Se conservan presupuestos de ondas, teselación, cáusticas y capturas. No se añaden cámaras, pases de ray tracing ni texturas nuevas. Se mantienen geometría, colisiones, nado, distancia de renderizado y linterna.

La óptica se comprueba con el núcleo real: transmisión monótona con la profundidad, claridad en piscinas poco profundas, Fresnel y estabilidad de los IDs de ajustes. Dos pruebas PlayMode revisan el material ligero y la ausencia del material de previsualización. Unity no está instalado en el entorno de desarrollo: estas pruebas nativas, shaders, apariencia y FPS siguen pendientes en el proyecto real.
