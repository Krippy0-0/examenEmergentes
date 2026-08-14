# AIM-AR — Relevo de la segunda entrega

## Entorno fijado

- Unity: `6000.5.1f1`
- Vuforia Engine: `11.4.4`
- Rama de trabajo prevista: `entrega-2`
- Escena principal: `Assets/AIMAR/Scenes/Entrenamiento.unity`

## ⚠️ Bloqueante actual: archivos Git LFS sin resolver

El proyecto **no compila** en su estado actual. `Packages/com.ptc.vuforia.engine-11.4.4.tgz`
pesa 134 bytes en lugar de 138 MB: es un puntero de Git LFS sin resolver. Esto ocurre al
descargar el repositorio como ZIP desde GitHub, que no resuelve LFS.

Sin el paquete de Vuforia no compila `Assembly-CSharp-Editor` y el menú `AIM-AR >` no aparece.

Resolución (ver `PLAN_SEGUNDA_ENTREGA_AIM_AR.md` y las instrucciones del relevo):

1. Clonar el repositorio con `git lfs install` seguido de `git clone`, **o**
2. Descargar Vuforia Engine 11.4.4 desde `developer.vuforia.com` y reemplazar el `.tgz`.

El registro `https://registry.packages.developer.vuforia.com/` **no** sirve para esto:
solo publica hasta la versión `9.6.3`. Verificado.

## Preparación automática

El repositorio incluye el comando de Unity:

`AIM-AR > Construir prototipo de la segunda entrega`

Este comando crea o actualiza, sin modificar `SampleScene`:

- `ARCamera` de Vuforia e `ImageTarget_AIMAR` en modo INSTANT.
- `ARContent` con plataforma compuesta, decorado y **tres dianas**.
- Capa `Target` (capa 8).
- `Target.prefab` con `Target` + `FloatingTarget` y materiales.
- `GameManager`, `ShooterController`.
- HUD con banda de contraste, retícula, puntaje, tiempo, estado de seguimiento,
  instrucción, botón `FUEGO`, panel final y botón `REINICIAR`.
- Escena `Entrenamiento.unity` incluida en Build Settings.

`AIMAR_Marker.png` fue eliminado a propósito (era otro puntero LFS roto). El builder lo
regenera de forma procedural en cuanto el proyecto compile: al abrir Unity, el hook
`CompletePendingPrototypeAfterReload` detecta que falta y reconstruye la escena solo.

## Pasos manuales obligatorios antes de probar

1. Resolver el bloqueante de LFS descrito arriba.
2. Abrir el proyecto con Unity `6000.5.1f1` y esperar a que termine la importación.
3. Dejar que la reconstrucción automática se ejecute, o lanzar
   `AIM-AR > Construir prototipo de la segunda entrega` manualmente.
4. Abrir la configuración de Vuforia y comprobar que la licencia esté asignada.
5. Guardar la escena y salir de Play antes de registrar cambios.

> **Importante:** el comando del menú **destruye y reconstruye** `ARContent`, el HUD,
> `GameManager` y `ShooterController`. Cualquier ajuste hecho a mano en la escena se pierde.
> Los cambios de escena deben hacerse en `AIMARSceneBuilder.cs`, no en el editor.

## Prueba del relevo

1. Abrir `Assets/AIMAR/Scenes/Entrenamiento.unity`.
2. Entrar en Play.
3. Mostrar el marcador a la cámara y confirmar que el estado pasa de
   `Buscando marcador` a `Marcador detectado`.
4. Confirmar que aparecen la plataforma, el decorado y `Target_01`, `Target_02` y `Target_03`.
5. Comprobar que las tres dianas se mueven con velocidades y alturas distintas.
6. Alinear la retícula con una diana y presionar `FUEGO`.
7. Confirmar que la diana cambia de color y tamaño, reaparece en otra posición y que el
   puntaje aumenta en 100.
8. Disparar fuera de la diana y confirmar que el puntaje no aumenta.
9. Confirmar que el temporizador baja desde 30 y que al llegar a cero aparece el panel final
   con puntaje, impactos e intentos, y que no se puede disparar.
10. Presionar `REINICIAR` y confirmar que puntaje, tiempo y posiciones de las dianas vuelven
    al estado inicial.
11. Repetir el flujo tres veces y comprobar que no existan errores rojos en Console.

## Estado del traspaso

- La lógica de disparo usa `Physics.Raycast` y `RaycastHit` como única fuente de impacto.
- Las dianas necesitan `Collider`, pero no usan `Rigidbody`, `OnCollisionEnter` ni `OnTriggerEnter`.
- El botón `FUEGO` llama directamente a `ShooterController.Shoot()`.
- El `LayerMask` del disparo solo incluye la capa 8; plataforma, decorado y HUD quedan excluidos.
- `TrackingStatusHud` no depende de Vuforia: el builder conecta los eventos del observador
  por reflexión, de modo que `Assembly-CSharp` compila aunque el paquete falte.
- La licencia está en `Assets/Resources/VuforiaConfiguration.asset` (campo `ufoLicenseKey`,
  que es la clave en Base64). **No mostrarla en pantalla durante la grabación del video.**

## Pendiente de verificación

Nada de lo anterior pudo probarse en Unity todavía, porque el bloqueante de LFS lo impide.
El código está escrito y es coherente, pero la primera ejecución debe confirmar:

- Que la reconstrucción automática de la escena se completa sin errores.
- Que las tres dianas quedan dentro de la plataforma y son alcanzables con la retícula.
- Que los eventos `OnTargetFound` / `OnTargetLost` se conectaron (si no, aparece un
  `LogWarning` de AIM-AR y el texto de estado queda fijo; no afecta al resto).

## Pendiente para el siguiente relevo

- Precisión simple `hits / shots * 100` conectada al `GameManager` y al panel final.
- Ajuste fino de posiciones y velocidades tras la prueba real con el marcador.
- Control de calidad final y grabación del video de 2 a 4 minutos.
- Funciones opcionales (partículas, sonido) solamente después de validar el flujo principal.

## Último commit que debe descargarse

Usar el commit más reciente de la rama `entrega-2`. Comprobarlo con `git log -1 --oneline`
después de descargarla.
