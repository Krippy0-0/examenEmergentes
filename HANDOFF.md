# AIM-AR — Estado del proyecto y relevo para la sesión 3

Documento de traspaso de **Ariel Van Kilsdonk** (sesión 2) a **Mattias Morales** (sesión 3).

Para el seguimiento paso a paso contra la rúbrica y el plan, ver `ESTADO_AVANCE.md`.

## Entorno fijado

- Unity: `6000.5.1f1` — no actualizar durante la entrega.
- Vuforia Engine: `11.4.4`, como paquete local en `Packages/`.
- Rama: `entrega-2`.
- Escena principal: `Assets/AIMAR/Scenes/Entrenamiento.unity`.

---

## 1. Cómo poner el proyecto en marcha

**No descargues el repositorio como ZIP.** El paquete de Vuforia pesa 132 MB y está en
Git LFS; GitHub no resuelve LFS en las descargas ZIP y te entrega un puntero de texto de
134 bytes. Sin ese archivo el proyecto no compila y el menú `AIM-AR >` no aparece.

```bash
git lfs install
git clone -b entrega-2 https://github.com/Krippy0-0/examenEmergentes.git
```

Comprobá que el tarball pese unos 132 MB:

```bash
ls -l Packages/com.ptc.vuforia.engine-11.4.4.tgz
```

Si ya lo bajaste mal, la alternativa es extraer el `.tgz` del unitypackage
`add-vuforia-package-11-4-4.unitypackage` de developer.vuforia.com y copiarlo a `Packages/`
**antes** de abrir Unity. Tiene que ser antes: si el proyecto no compila, el script de
migración de Vuforia tampoco corre.

El registro `https://registry.packages.developer.vuforia.com/` **no sirve** para esto:
solo publica hasta la versión `9.6.3`. Verificado.

---

## 2. Regla más importante del proyecto

**La escena se genera por código.** El comando `AIM-AR > Construir prototipo de la segunda
entrega` **destruye y reconstruye** `ARContent`, el HUD, el `GameManager` y el
`ShooterController`.

Cualquier ajuste hecho a mano en el editor se pierde en la siguiente ejecución. Los cambios
de escena se hacen en `Assets/AIMAR/Editor/AIMARSceneBuilder.cs` y después se vuelve a
ejecutar el comando.

Puntos que probablemente quieras tocar:

| Qué | Dónde |
|---|---|
| Posición, escala y movimiento de las dianas | array `Targets`, al principio del builder |
| Área donde reaparecen tras un impacto | `RelocationAreaMin` / `RelocationAreaMax` |
| Duración de la sesión y puntos por acierto | Inspector del `GameManager` |
| Tamaño y disposición del HUD | método `CreateHud` |

---

## 3. Qué está funcionando

Verificado compilando en Unity `6000.5.1f1` en batchmode: **0 errores**, escena generada
correctamente. Todo esto está en el commit de la sesión 2.

### Escena y objetos
- `ARCamera` de Vuforia e `ImageTarget_AIMAR` en modo INSTANT.
- Plataforma compuesta con base y tres rieles.
- Decorado: tres cajas apiladas, mástil y banderín.
- **Tres dianas** (`Target_01`, `Target_02`, `Target_03`) con posiciones, escalas y
  parámetros de movimiento distintos.

### Scripts
| Archivo | Qué hace |
|---|---|
| `ShooterController.cs` | Raycast desde la cámara. Única fuente de impacto. |
| `Target.cs` | Recibe el impacto, responde visualmente, reaparece en otra posición. |
| `FloatingTarget.cs` | Rotación, oscilación vertical y órbita por script. |
| `GameManager.cs` | Puntaje, intentos, impactos, precisión, tiempo y panel final. |
| `TrackingStatusHud.cs` | Texto de estado del marcador. |

### Interacción e interfaz
- Ciclo completo: `FUEGO` → `Physics.Raycast` → `RaycastHit` → `Target` → puntaje → HUD.
- Respuesta al acierto: cambio de color y escala, y reubicación de la diana.
- HUD con banda de contraste, retícula, puntaje, tiempo, estado e instrucción.
- Panel final con puntaje, impactos, intentos y **precisión** `hits / shots * 100`.
- Botón `REINICIAR` conectado a `GameManager.ResetSession()`.
- Al llegar el tiempo a cero se bloquean los disparos.

### Corrección del profesor, respetada
El disparo se resuelve **solo** con `Physics.Raycast` y `RaycastHit`. Las dianas tienen
`Collider` pero **no** usan `Rigidbody`, `OnCollisionEnter` ni `OnTriggerEnter`. El
`LayerMask` incluye únicamente la capa 8 (`Target`), así que plataforma, decorado e
interfaz quedan excluidos.

---

## 4. ⚠️ Problema abierto — bloquea la demostración

**El `ImageTarget` en modo INSTANT no se detecta nunca.** Sin esto no hay demo: la primera
fila de la rúbrica exige que se vea el reconocimiento del marcador.

### Qué ya está descartado

| Hipótesis | Estado |
|---|---|
| Licencia inválida | **Descartada.** Los targets de base de datos sí rastrean. |
| Cámara insuficiente | **Descartada.** Misma cámara, mismos targets, funcionan. |
| Contenido de la imagen | **Descartado por Ariel.** Ninguna imagen propia funciona. |

### El síntoma real

Los targets que vienen de una **base de datos de Vuforia** (Astronaut, Drone del conjunto
`VuforiaMars_Images`) se rastrean sin problema. Ningún target **INSTANT** se detecta,
sea cual sea la imagen. El problema está en el modo INSTANT, no en las imágenes.

### Camino más corto para resolverlo

Vuforia ya desempaquetó sus assets de muestra dentro del proyecto al importar:

- `Assets/Editor/Vuforia/ForPrint/ImageTargets/target_images_USLetter.pdf` — PDF listo para
  imprimir con los targets oficiales.
- `Assets/Editor/Vuforia/ImageTargetTextures/` — las imágenes de esos targets.

O sea que **no hace falta descargar nada**. La vía de menor riesgo es cambiar el
`ImageTarget` de `INSTANT` a `PREDEFINED` y apuntarlo a la base `VuforiaMars_Images`,
que es la configuración que ya se sabe que funciona en la máquina de Ariel.

Eso implica tocar `ConfigureInstantImageTarget` en el builder, o asignar la base a mano en
el Inspector y **no volver a ejecutar el comando del menú** (ver sección 2).

### Si preferís investigar el modo INSTANT

Hipótesis por orden de probabilidad:
1. Los Instant Image Targets podrían no operar en Play Mode con webcam en el editor, y
   funcionar solo en dispositivo.
2. Podría faltar una llamada de inicialización en runtime que el `ImageTargetBehaviour`
   configurado desde el editor no hace por sí solo.
3. La textura podría necesitar ajustes de importación distintos. Ya se probó sin comprimir
   y legible desde CPU, sin resultado.

---

## 5. Lo que falta para cerrar la entrega

### Paso 1 — Verificar el estado recibido
Abrir el proyecto, comprobar que compila sin errores y entrar en Play.

### Paso 2 — Lógica e interfaz
Casi todo está cerrado. Queda por **confirmar en Play** (no pudo probarse por el problema
del marcador):

- [ ] `shots`, `hits`, `score` y `timeRemaining` se reinician correctamente.
- [ ] Cada disparo suma un intento una sola vez.
- [ ] Cada acierto suma puntaje una sola vez.
- [ ] La precisión se calcula bien y aparece en el panel final.
- [ ] `REINICIAR` restaura dianas, HUD y estado.
- [ ] Ningún panel intercepta los controles.

### Paso 3 — Control de calidad final
La checklist de 12 puntos de la sección 8 del plan. **Nada de esto pudo verificarse**
mientras el marcador no se detecte.

### Paso 4 — Video de 2 a 4 minutos
Guion de siete bloques en la sección 8 del plan. Dos avisos concretos:

- **Revisá el modo de Play de Vuforia** (`Window > Vuforia Configuration`, sección Play
  Mode). Si está en Simulator no se usa la webcam y el video no mostraría reconocimiento
  real, que es justo lo que evalúa la primera fila de la rúbrica.
- **No abras la configuración de Vuforia con la grabación andando.** La licencia queda
  visible en el Inspector y el plan lo prohíbe expresamente.
- Poné el Game View en **1920×1080** antes de grabar. A esa resolución el `CanvasScaler`
  da factor de escala exactamente 1.0, así que nada se rompe; el único efecto es que la
  banda oscura superior ocupa cerca del 21% de la altura y se ve algo pesada. Si molesta,
  se baja la altura en el método `CreateHud` del builder.

### Paso 5 — Entrega y respaldo
Ver el video completo tras exportarlo, subirlo, abrirlo desde otra sesión para confirmar
permisos y guardar copia local.

---

## 6. Cosas del repositorio que conviene saber

- **`Assets/TutorialInfo/Icons/URP.png` aparece siempre como modificado.** No es un cambio
  real: en la historia está guardado como blob normal aunque `.gitattributes` mande los
  `.png` por LFS, así que el filtro lo reporta como distinto. Ignoralo; si lo commiteás lo
  convertís a LFS sin querer.
- **`examenEmergentes-main.slnx`** puede reaparecer si tu carpeta se llama así. Es un
  artefacto de Unity, no lo commitees. `.gitignore` cubre `*.sln` pero no `*.slnx`.
- **`Assets/Editor/Vuforia/`, `Assets/StreamingAssets/` y `QCAR/`** las genera Vuforia sola
  al importar. Están sin trackear a propósito.
- La licencia de Vuforia está en `Assets/Resources/VuforiaConfiguration.asset`.

---

## 7. Criterio de "listo para entregar"

De las siete condiciones de la sección 10 del plan, hoy solo se cumple una:

| # | Condición | Estado |
|---|---|---|
| 1 | El flujo mínimo funciona tres veces seguidas | ❌ bloqueado por el marcador |
| 2 | Sin errores rojos en Console | ⚠️ compila limpio, falta probar en Play |
| 3 | El disparo se resuelve por `RaycastHit` | ✅ |
| 4 | El video cubre las cinco filas de la rúbrica | ❌ |
| 5 | El video dura entre 2 y 4 minutos | ❌ |
| 6 | El archivo final fue abierto y revisado | ❌ |
| 7 | Último commit y video respaldados | ❌ |

**La prioridad absoluta es la sección 4.** Mientras el marcador no se detecte, nada del
resto puede verificarse ni grabarse.

---

## Último commit que debe descargarse

El más reciente de la rama `entrega-2`. Comprobalo con `git log -1 --oneline`.
