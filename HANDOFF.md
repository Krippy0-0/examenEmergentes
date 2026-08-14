# AIM-AR — Relevo de la segunda entrega

## Entorno fijado

- Unity: `6000.5.1f1`
- Vuforia Engine: `11.4.4`
- Rama de trabajo prevista: `entrega-2`
- Escena principal: `Assets/AIMAR/Scenes/Entrenamiento.unity`

## Preparación automática

El repositorio incluye el comando de Unity:

`AIM-AR > Construir prototipo de la segunda entrega`

Este comando crea o actualiza, sin modificar `SampleScene`:

- `ARCamera` de Vuforia.
- `ImageTarget_AIMAR`.
- `ARContent` con plataforma y una diana.
- Capa `Target`.
- `Target.prefab` y materiales.
- `GameManager`, `ShooterController` y `Target`.
- HUD con retícula, puntaje, tiempo y botón `FUEGO`.
- Escena `Entrenamiento.unity` incluida en Build Settings.

## Pasos manuales obligatorios antes de probar

1. Abrir el proyecto con Unity `6000.5.1f1` y esperar a que termine la importación.
2. Ejecutar `AIM-AR > Construir prototipo de la segunda entrega`.
3. Con el objeto `ImageTarget_AIMAR` seleccionado, asignar la base de datos y el marcador del proyecto.
4. Abrir la configuración de Vuforia y comprobar que la licencia válida esté asignada.
5. Guardar la escena y salir de Play antes de registrar cambios.

## Prueba del relevo de Carlos

1. Abrir `Assets/AIMAR/Scenes/Entrenamiento.unity`.
2. Entrar en Play.
3. Mostrar el marcador a la cámara.
4. Confirmar que aparecen la plataforma y `Target_01` sobre el marcador.
5. Alinear la retícula con la diana y presionar `FUEGO`.
6. Confirmar que la diana cambia temporalmente de color y tamaño y que el puntaje aumenta en 100.
7. Disparar fuera de la diana y confirmar que el puntaje no aumenta.
8. Confirmar que el temporizador baja desde 30 y que no se puede disparar al llegar a cero.
9. Repetir el flujo tres veces y comprobar que no existan errores rojos en Console.

## Estado del traspaso

- Vuforia está instalado como paquete local en `Packages/com.ptc.vuforia.engine-11.4.4.tgz`.
- La lógica mínima usa `Physics.Raycast` y `RaycastHit` como única fuente de impacto.
- La diana necesita `Collider`, pero no usa `Rigidbody`, `OnCollisionEnter` ni `OnTriggerEnter`.
- El botón `FUEGO` llama directamente a `ShooterController.Shoot()`.
- La licencia y la fuente del `ImageTarget` no se guardaron automáticamente: deben verificarse manualmente para no publicar ni sobrescribir credenciales.

## Pendiente para el siguiente relevo

- Agregar dos dianas adicionales.
- Incorporar `FloatingTarget.cs` con movimiento lento.
- Mejorar la respuesta visual y el HUD.
- Bloqueo/panel final y reinicio estable.
- Funciones opcionales solamente después de validar el flujo principal.

## Último commit que debe descargarse

Usar el commit más reciente de la rama `entrega-2`. Comprobarlo con `git log -1 --oneline` después de descargarla.
