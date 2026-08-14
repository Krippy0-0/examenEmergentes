# AIM-AR — Estado de avance de la segunda entrega

Seguimiento contra `PLAN_SEGUNDA_ENTREGA_AIM_AR.md`. Para el traspaso operativo ver `HANDOFF.md`.

- **Rama:** `entrega-2` · **Último commit:** `f83e789`
- **Unity:** `6000.5.1f1` · **Vuforia Engine:** `11.4.4`
- **Escena:** `Assets/AIMAR/Scenes/Entrenamiento.unity`

## Leyenda

| Marca | Significado |
|---|---|
| ✅ | Hecho y verificado |
| 🟡 | Hecho en código, falta confirmarlo en modo Play |
| ⛔ | Bloqueado por la detección del marcador |
| ⬜ | No empezado |

> **Qué significa «verificado» acá.** Se compiló el proyecto en Unity `6000.5.1f1` en
> batchmode con **0 errores** y se comprobó que la escena se genera con todo cableado
> (tres dianas, cuatro eventos conectados, `ImageTarget` con textura asignada).
> **No se pudo verificar ningún comportamiento en tiempo de ejecución**, porque el
> marcador no se detecta. Todo lo marcado 🟡 está escrito y compila, pero nadie lo vio funcionar.

---

## 1. Resumen

| Sesión | Responsable | Estado |
|---|---|---|
| 1 — Base técnica | Carlos Orellana | ✅ Completa |
| 2 — Escena, interfaz y estabilización | Ariel Van Kilsdonk | 🟡 Código completo, falta prueba en Play |
| 3 — Integración, calidad y video | Mattias Morales | ⬜ Por empezar |

**Bloqueante único y activo:** el `ImageTarget` en modo INSTANT no se detecta nunca,
mientras que los targets de base de datos sí. Descartadas licencia, cámara e imagen.
Detalle y salida propuesta en `HANDOFF.md`, sección 4.

---

## 2. Matriz de la rúbrica (sección 9 del plan)

| Criterio | Puntos | Construido | Demostrable en video |
|---|---:|---|---|
| Escena y objetos incorporados | 5 | ✅ Plataforma, decorado y tres dianas ancladas | ⛔ |
| Interacción funcional entre GameObjects | 7 | ✅ `ShooterController` → `Target` → `GameManager` | ⛔ |
| Avance de la interfaz gráfica | 4 | ✅ Retícula, puntaje, tiempo, estado, instrucción y `FUEGO` | ⛔ |
| Funcionamiento y avance del prototipo | 6 | 🟡 Compila limpio, ciclo sin probar | ⛔ |
| Demostración mediante video | 3 | ⬜ | ⬜ |

Las cuatro primeras filas están cubiertas en código. Ninguna puede acreditarse todavía,
porque la rúbrica evalúa que se vea funcionando y eso depende del marcador.

---

## 3. Sesión 1 — Carlos: base técnica

| Paso | Estado |
|---|---|
| 1.1–1.3 Proyecto base, versión de Unity fijada, Vuforia instalado | ✅ |
| 1.4 Licencia y fuente del `ImageTarget` asignadas | 🟡 Licencia sí; se optó por INSTANT en vez de base de datos |
| 1.5 No actualizar paquetes | ✅ |
| 1.6 Rama `entrega-2` y `.gitignore` de Unity | ✅ |
| 2. Escena principal, cámara única, `ARContent`, plataforma | ✅ |
| 3. Diana con material, `Collider`, capa `Target` y prefab | ✅ |
| 4. `GameManager`, `Target.ReceiveHit`, `ShooterController`, raycast | ✅ |
| 5. HUD mínimo: retícula, puntaje, tiempo, botón `FUEGO` | ✅ |
| 6. Prueba de salida de la sesión | ⛔ |

---

## 4. Sesión 2 — Ariel: escena, interfaz y estabilización

### Paso 1 — Validar el relevo anterior
| Punto | Estado |
|---|---|
| Descargar el último commit y leer el traspaso | ✅ |
| Abrir con la misma versión de Unity | ✅ |
| Ejecutar la prueba completa de la sesión 1 | ⛔ |

### Paso 2 — Escena y objetos
| Punto | Estado |
|---|---|
| 2.1 Plataforma mejorada | ✅ Base más tres rieles |
| 2.2 Objeto adicional incorporado | ✅ Cajas apiladas, mástil y banderín |
| 2.3 Tres instancias de `Target.prefab` | ✅ `Target_01`, `Target_02`, `Target_03` |
| 2.4 Distribuidas en posiciones y alturas distintas | ✅ |
| 2.5 Ancladas al `ImageTarget` | 🟡 Correcto por construcción, sin ver en Play |
| 2.6 Revisar colliders con gizmos | ⬜ Requiere inspección visual |

### Paso 3 — Movimiento por script
| Punto | Estado |
|---|---|
| 3.1 `FloatingTarget.cs` creado | ✅ |
| 3.2 Rotación estable con `Time.deltaTime` | ✅ |
| 3.3 Oscilación vertical y órbita | ✅ |
| 3.4 Velocidad y amplitud expuestas en el Inspector | ✅ |
| 3.5 Valores distintos en las tres dianas | ✅ Rotación 30 / −45 / 60 |
| 3.6 Evitar movimientos rápidos | ✅ |

### Paso 4 — Respuesta de impacto
| Punto | Estado |
|---|---|
| 4.1 Cambio inmediato de color o escala | ✅ |
| 4.2 Reactivar o reubicar tras una pausa | ✅ Reaparece en otra posición válida |
| 4.3 Impedir que un clic sume más de una vez | ✅ |
| 4.4 Collider sincronizado con la diana visible | ✅ |
| 4.5 `LayerMask` excluye plataforma, interfaz y decorado | ✅ Solo capa 8 |
| 4.6 Raycast como única fuente de verdad | ✅ |

### Paso 5 — Interfaz funcional
| Punto | Estado |
|---|---|
| 5.1 Puntaje, tiempo y botón legibles | ✅ Banda de contraste y contorno |
| 5.2 Texto de estado del marcador | ✅ `TrackingStatusHud` conectado |
| 5.3 Indicación de uso | ✅ |
| 5.4 Bloquear disparos al llegar a cero | ✅ |
| 5.5 Panel final con puntaje e impactos | ✅ Incluye intentos y precisión |
| 5.6 Botón `Reiniciar` | ✅ Restaura estado y dianas |

### Paso 6 — Preparar para grabar
| Punto | Estado |
|---|---|
| 6.1 Jerarquía ordenada y nombrada | ✅ |
| 6.2 Sin duplicados, cámaras sobrantes ni scripts sin usar | ✅ |
| 6.3 Sin errores rojos en Console | 🟡 Compila limpio, falta Play |
| 6.4 Textos no se cortan en la resolución de grabación | ⬜ Ver nota más abajo |
| 6.5 Demostración fácil: blancos visibles, sesión de 30 s | 🟡 |
| 6.6 Tres sesiones consecutivas | ⛔ |
| 6.7 Ensayo de la secuencia del video | ⛔ |

### Funciones opcionales
Partículas, sonido y animación de aparición: ⬜ **no iniciadas a propósito**. La sección 10
del plan prohíbe empezarlas antes de que el flujo principal esté estable.

---

## 5. Sesión 3 — Mattias: integración, calidad y video

### Paso 1 — Verificar el estado recibido
⬜ Por empezar.

### Paso 2 — Cerrar lógica e interfaz
| Punto | Estado |
|---|---|
| 2.1 Reinicio correcto de `shots`, `hits`, `score` y `timeRemaining` | 🟡 |
| 2.2 Cada disparo suma un intento una sola vez | 🟡 |
| 2.3 Cada acierto suma una sola vez; los fallos no | 🟡 |
| 2.4 Precisión `hits / shots * 100` conectada | ✅ `GameManager.Accuracy` |
| 2.5 Bloqueo de disparos al agotarse el tiempo | 🟡 |
| 2.6 Panel final con puntaje, impactos, intentos y precisión | ✅ |
| 2.7 `Reiniciar` restaura dianas, HUD y estado | 🟡 |
| 2.8 Retícula, textos y botones alineados y con contraste | ✅ |
| 2.9 Ningún panel bloquea dianas ni intercepta controles | 🟡 |
| 2.10 Mantener el diseño simple | ✅ |

**El Paso 2 está cerrado en código.** Lo que queda es confirmarlo en Play.

### Paso 3 — Control final (12 puntos)
⛔ Ninguno verificable mientras el marcador no se detecte.

### Paso 4 — Video de 2 a 4 minutos
⬜ Por grabar. Avisos en `HANDOFF.md`, sección 5.

### Paso 5 — Entrega y respaldo
⬜ Por hacer.

---

## 6. Criterio de «listo para entregar» (sección 10 del plan)

| # | Condición | Estado |
|---|---|---|
| 1 | El flujo mínimo funciona tres veces seguidas | ⛔ |
| 2 | No hay errores rojos en Console | 🟡 Compila limpio, falta Play |
| 3 | El disparo se resuelve por `RaycastHit` | ✅ |
| 4 | El video cubre las cinco filas de la rúbrica | ⬜ |
| 5 | El video dura entre 2 y 4 minutos | ⬜ |
| 6 | El archivo o enlace final fue abierto y revisado | ⬜ |
| 7 | Último commit y video respaldados | 🟡 Commit sí, video no |

**1 de 7 cumplida.** La condición 1 arrastra a las demás.

---

## 7. Riesgos abiertos

| Riesgo | Impacto | Estado |
|---|---|---|
| El `ImageTarget` INSTANT no se detecta | Sin demo no hay entrega | ⛔ Activo, prioridad máxima |
| Modo de Play de Vuforia en Simulator | El video no mostraría reconocimiento real | ⬜ Verificar antes de grabar |
| Banda superior del HUD ocupa ~21% de la altura en 16:9 | Estético; a 1920×1080 el `CanvasScaler` da factor 1.0 y nada se rompe | ⬜ Decidir si se baja |
| La licencia queda visible al abrir la configuración de Vuforia | El plan prohíbe mostrarla en el video | ⬜ Cuidar en la grabación |
| El comando del menú rehace la escena y borra ajustes manuales | Pérdida de trabajo | ✅ Documentado en `HANDOFF.md` |

---

## 8. Inventario de código

| Archivo | Líneas | Rol |
|---|---:|---|
| `Assets/AIMAR/Scripts/GameManager.cs` | 145 | Puntaje, intentos, impactos, precisión, tiempo, panel final |
| `Assets/AIMAR/Scripts/Target.cs` | 182 | Impacto, respuesta visual, reubicación, reinicio |
| `Assets/AIMAR/Scripts/FloatingTarget.cs` | 79 | Rotación, oscilación y órbita |
| `Assets/AIMAR/Scripts/ShooterController.cs` | 39 | Raycast desde la cámara |
| `Assets/AIMAR/Scripts/TrackingStatusHud.cs` | 52 | Estado del marcador en el HUD |
| `Assets/AIMAR/Editor/AIMARSceneBuilder.cs` | 778 | Genera la escena completa |
