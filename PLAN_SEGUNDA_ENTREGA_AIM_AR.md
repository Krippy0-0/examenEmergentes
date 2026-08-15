# AIM-AR — Plan de trabajo para la segunda entrega

## 1. Objetivo de esta entrega

La segunda entrega no exige terminar AIM-AR. Su objetivo es demostrar, desde el editor de Unity y en modo **Play**, que ya existe un prototipo técnico coherente y funcional.

Para aspirar al nivel **Excelente** de la rúbrica, el video debe permitir comprobar claramente:

1. Una escena de Realidad Aumentada desarrollada y reconocible.
2. Objetos creados o importados incorporados a la escena.
3. Una interacción funcional entre varios GameObjects mediante scripts.
4. Una interfaz gráfica funcional, aunque todavía no tenga diseño definitivo.
5. Un avance técnico estable y suficiente para esta etapa.
6. Una demostración de entre 2 y 4 minutos, ejecutada en modo Play desde Unity.

## 2. Decisión de alcance: qué se construirá ahora

### Versión mínima que debe quedar funcionando

El prototipo de esta entrega tendrá un solo flujo:

1. Unity entra en modo Play.
2. Vuforia reconoce el marcador mediante la cámara configurada para las pruebas.
3. Sobre el `ImageTarget` aparece un campo de entrenamiento simple.
4. Se ven al menos tres dianas virtuales con colliders.
5. Una retícula permanece en el centro de la pantalla.
6. El usuario apunta moviendo la cámara y presiona el botón `FUEGO`.
7. `ShooterController` lanza un raycast desde el centro de la cámara.
8. Si el rayo alcanza una diana, el `RaycastHit` permite identificarla directamente.
9. La diana entrega una respuesta visible y el puntaje aumenta.
10. El HUD muestra, al menos, puntaje y tiempo restante.

Este flujo es suficiente para demostrar escena, objetos, interacción entre GameObjects, entrada del usuario, scripts e interfaz.

### Funciones que se postergan

No se deben comenzar mientras el flujo mínimo anterior no funcione de principio a fin y sin errores:

- Tres dificultades.
- Persistencia con `PlayerPrefs`.
- Récord histórico.
- Cálculo avanzado de precisión según distancia al centro.
- Tiempo de reacción promedio y rachas.
- Menú y pantalla de resultados completos.
- Sonido y partículas elaboradas.
- Modelos o materiales definitivos.
- APK final.
- Evaluación con seis usuarios.
- Informe final y presentación final.

La evaluación con usuarios queda expresamente fuera de la ruta crítica de esta entrega. Solo se retomará en la entrega final si el prototipo ya es estable y queda tiempo.

## 3. Corrección técnica indicada por el profesor

El disparo se resolverá únicamente con raycast. No se combinarán `Physics.Raycast`, una colisión física y un trigger para detectar el mismo impacto.

Flujo recomendado:

```csharp
public void Shoot()
{
    gameManager.RegisterShot();

    Ray ray = new Ray(arCamera.transform.position, arCamera.transform.forward);

    if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, targetLayer))
    {
        Target target = hit.collider.GetComponentInParent<Target>();

        if (target != null)
        {
            target.ReceiveHit(hit.point);
            return;
        }
    }

    gameManager.RegisterMiss();
}
```

La diana sí necesita un `Collider`, porque el raycast debe tener una superficie que alcanzar. No necesita `Rigidbody`, `OnCollisionEnter` ni `OnTriggerEnter` para resolver el disparo. Un trigger solo tendría sentido para otra mecánica independiente, por ejemplo detectar que un objeto abandonó una zona.

Para reducir dependencias, el botón del Canvas llamará directamente a `ShooterController.Shoot()`. Así, el mismo botón funciona con clic en el editor y con toque en una futura prueba móvil.

## 4. Organización del proyecto

Se recomienda mantener una estructura pequeña y predecible:

```text
Assets/
└── AIMAR/
    ├── Scenes/
    │   └── Entrenamiento.unity
    ├── Scripts/
    │   ├── ShooterController.cs
    │   ├── Target.cs
    │   ├── FloatingTarget.cs
    │   └── GameManager.cs
    ├── Prefabs/
    │   └── Target.prefab
    ├── Materials/
    ├── UI/
    └── Images/
```

No es necesario crear una segunda escena para esta entrega. El menú puede representarse con un panel inicial dentro de `Entrenamiento.unity`, o incluso quedar postergado si el HUD y la interacción ya funcionan.

## 5. Reglas para trabajar por relevos

El trabajo se realizará en este orden:

1. **Sesión 1: Carlos Orellana**.
2. **Sesión 2: Ariel Van Kilsdonk**.
3. **Sesión 3: Mattias Morales**.

Cada sesión comienza desde el estado estable entregado por la persona anterior. Por eso es importante respetar tanto el orden técnico como el traspaso mediante Git y `HANDOFF.md`.

Reglas comunes:

- Usar todos la misma versión de Unity y no actualizarla durante la entrega.
- Trabajar sobre una rama compartida, por ejemplo `entrega-2`, porque no habrá trabajo simultáneo.
- Antes de comenzar: descargar los últimos cambios y abrir el proyecto sin modificar paquetes.
- Al terminar: guardar la escena, salir de Play, comprobar que no existan errores rojos en Console, cerrar Unity, registrar los cambios y subirlos.
- No versionar carpetas generadas como `Library`, `Temp`, `Logs` u `obj`.
- No dejar referencias del Inspector en estado `None` cuando sean necesarias para ejecutar la escena.
- No comenzar una función opcional si existe un error en el flujo principal.

Cada relevo debe dejar un archivo breve `HANDOFF.md` con:

- Versión de Unity utilizada.
- Escena que se debe abrir.
- Pasos exactos para ejecutar la prueba.
- Qué quedó funcionando.
- Qué falta.
- Errores o limitaciones conocidas.
- Último commit que se debe descargar.

## 6. Sesión 1 — Carlos: base técnica y primer ciclo funcional

**Resultado obligatorio:** una escena de RA que entra en Play, reconoce el marcador, muestra al menos una diana y permite acertarle mediante raycast.

### Paso 1. Recuperar y fijar el proyecto base

1. Abrir el proyecto iniciado para la primera entrega.
2. Confirmar la versión de Unity utilizada y escribirla en `HANDOFF.md`.
3. Verificar que Vuforia está instalado y que el proyecto abre sin errores de compilación.
4. Confirmar que la licencia y la base de datos del `ImageTarget` están asignadas.
5. Evitar actualizar Unity, Vuforia u otros paquetes si el proyecto ya abre correctamente.
6. Crear o comprobar la rama `entrega-2` y un `.gitignore` apropiado para Unity.

### Paso 2. Preparar la escena principal

1. Crear o limpiar `Entrenamiento.unity`.
2. Dejar una sola cámara activa: la cámara configurada para Vuforia.
3. Agregar el `ImageTarget` y seleccionar el marcador correcto.
4. Crear un GameObject vacío llamado `ARContent` como hijo del `ImageTarget`.
5. Dentro de `ARContent`, crear una plataforma simple con primitivas de Unity.
6. Ajustar escala y posición para que el contenido aparezca sobre el marcador y no dentro de él.
7. Entrar en Play y comprobar que el contenido aparece, desaparece o se reposiciona de forma coherente con el seguimiento del marcador.

### Paso 3. Crear una diana mínima

1. Construir una diana con primitivas simples, por ejemplo cilindros o discos concéntricos.
2. Asignarle un material visible y con suficiente contraste.
3. Agregar un `Collider` que cubra correctamente la superficie.
4. Crear una capa llamada `Target` y asignarla a la diana y sus hijos relevantes.
5. Convertirla en `Target.prefab`.
6. Colocar una instancia como hija de `ARContent`.

### Paso 4. Implementar la interacción principal

1. Crear `GameManager.cs` con variables mínimas: `score`, `shots`, `hits` y `timeRemaining`.
2. Crear `Target.cs` con un método público `ReceiveHit(Vector3 hitPoint)`.
3. En `ReceiveHit`, impedir impactos duplicados durante la misma respuesta.
4. Al recibir un impacto, llamar a `GameManager.RegisterHit()`.
5. Dar una respuesta visible sencilla: cambiar de color, reducir la escala o desactivar brevemente el renderer.
6. Crear `ShooterController.cs` en la cámara o en un GameObject controlador.
7. Asignar por Inspector la cámara, el `GameManager`, una distancia máxima razonable y el `LayerMask` de `Target`.
8. Implementar `Shoot()` con `Physics.Raycast(..., out RaycastHit hit, ..., targetLayer)`.
9. Obtener `Target` desde `hit.collider.GetComponentInParent<Target>()` y llamar a `ReceiveHit(hit.point)`.
10. Registrar el fallo si el raycast no alcanza una diana.

### Paso 5. Crear un HUD mínimo

1. Agregar un Canvas en modo `Screen Space - Overlay`.
2. Crear una retícula sencilla en el centro.
3. Crear un texto `Puntaje: 0`.
4. Crear un texto `Tiempo: 30`.
5. Crear un botón grande `FUEGO`.
6. Conectar el evento `OnClick` del botón a `ShooterController.Shoot()`.
7. Conectar los textos al `GameManager` para actualizarlos durante Play.

Se recomienda usar 30 segundos en esta entrega: permite mostrar una sesión completa dentro del video. La duración final de 60 segundos puede restablecerse más adelante.

### Paso 6. Prueba de salida de la sesión 1

Antes del traspaso, repetir tres veces:

1. Abrir `Entrenamiento.unity`.
2. Entrar en Play.
3. Mostrar el marcador a la cámara.
4. Ver la plataforma y la diana.
5. Apuntar la retícula a la diana.
6. Presionar `FUEGO`.
7. Confirmar respuesta visual y aumento del puntaje.
8. Disparar fuera de la diana y confirmar que no aumenta el puntaje.
9. Verificar que el temporizador disminuye.
10. Confirmar cero errores rojos en Console.

### Entregables del relevo 1

- Proyecto base estable.
- Escena `Entrenamiento.unity`.
- Vuforia e `ImageTarget` configurados.
- Plataforma y una diana visible.
- Raycast funcional y corregido según la observación del profesor.
- HUD mínimo conectado.
- `HANDOFF.md` actualizado.
- Cambios guardados, registrados y subidos.

## 7. Sesión 2 — Ariel: escena completa, interfaz y estabilización

**Resultado obligatorio:** un prototipo visualmente reconocible, con varias dianas, movimiento, retroalimentación clara, HUD funcional y un ciclo de juego demostrable.

### Paso 1. Validar el relevo anterior

1. Descargar el último commit de `entrega-2`.
2. Leer `HANDOFF.md` antes de abrir Unity.
3. Abrir el proyecto con la misma versión de Unity.
4. Ejecutar la prueba completa de la sesión 1 sin cambiar nada.
5. Si falla, corregir primero el error existente y documentar la causa.

### Paso 2. Desarrollar la escena y los objetos

1. Mejorar la plataforma con primitivas, materiales y una distribución clara.
2. Incorporar al menos un objeto adicional creado o importado, sin depender de recursos pesados.
3. Crear tres instancias de `Target.prefab` como hijas de `ARContent`.
4. Distribuirlas a diferentes posiciones y alturas locales alrededor del marcador.
5. Confirmar que todas permanecen correctamente ancladas al `ImageTarget`.
6. Revisar los colliders con la visualización de gizmos para evitar zonas de impacto engañosas.

### Paso 3. Agregar movimiento mediante scripts

1. Crear `FloatingTarget.cs`.
2. Implementar una rotación lenta y estable usando `Time.deltaTime`.
3. Agregar, solo si no rompe el seguimiento visual, una oscilación vertical u órbita pequeña.
4. Exponer velocidad y amplitud en el Inspector.
5. Usar valores distintos en las tres dianas para evidenciar que los objetos son controlados por scripts.
6. Evitar movimientos rápidos que dificulten la demostración.

### Paso 4. Completar la respuesta de impacto

1. Hacer que una diana acertada cambie de color o escala inmediatamente.
2. Después de una pausa breve, reactivarla o moverla a otra posición local válida.
3. Impedir que un mismo clic sume más de una vez.
4. Mantener el collider sincronizado con la diana visible.
5. Comprobar que el `LayerMask` excluye plataforma, interfaz y objetos decorativos.
6. Mantener el raycast como única fuente de verdad del disparo; no agregar `OnTriggerEnter` ni `OnCollisionEnter` para el impacto.

### Paso 5. Completar la interfaz funcional

1. Hacer legibles el puntaje, el tiempo y el botón `FUEGO`.
2. Agregar un texto corto de estado: `Buscando marcador` o `Marcador detectado`, si puede conectarse sin desestabilizar el proyecto.
3. Agregar una indicación breve: `Apunta con la retícula y presiona FUEGO`.
4. Al llegar el tiempo a cero, bloquear nuevos disparos.
5. Mostrar un panel final sencillo con puntaje e impactos, sin implementar todavía una pantalla de resultados completa.
6. Agregar un botón `Reiniciar` solo si puede reiniciar variables y dianas de forma confiable.

### Paso 6. Preparar el prototipo para ser grabado

1. Ordenar y nombrar los GameObjects de la jerarquía.
2. Eliminar objetos duplicados, cámaras sobrantes y scripts sin usar.
3. Resolver todos los errores rojos de Console.
4. Confirmar que los textos no se cortan en la resolución de Game View que se grabará.
5. Configurar una demostración fácil: blancos visibles, movimiento lento y sesión de 30 segundos.
6. Ejecutar tres sesiones consecutivas sin reiniciar Unity.
7. Ensayar la secuencia del video una vez y anotar cualquier punto confuso.

### Funciones opcionales, solo si todo lo anterior está estable

- Un efecto de partículas breve.
- Un sonido de impacto.
- Una animación de aparición.

No incorporar más de una función opcional si el tiempo es limitado.

### Entregables del relevo 2

- Escena desarrollada con plataforma, elementos decorativos y al menos tres dianas.
- Objetos creados o importados claramente visibles.
- Movimiento por script.
- Ciclo disparo → `RaycastHit` → diana → puntaje → HUD totalmente funcional.
- Respuesta visible para acierto y fallo.
- Panel final mínimo o bloqueo al terminar el tiempo.
- Prototipo ensayado y listo para grabar.
- `HANDOFF.md` actualizado.
- Cambios guardados, registrados y subidos.

## 8. Sesión 3 — Mattias: integración final, control de calidad y video

**Resultado obligatorio:** cerrar el ciclo completo de la sesión, comprobar que las métricas sean coherentes, dejar el prototipo estable y producir el video final de 2 a 4 minutos.

### Paso 1. Verificar el estado recibido

1. Descargar el último commit y leer `HANDOFF.md`.
2. Abrir el proyecto con la misma versión de Unity.
3. Ejecutar el ensayo completo preparado por Ariel.
4. Confirmar qué elementos del ciclo ya funcionan antes de realizar cambios.
5. Si aparece un error crítico, identificar primero en qué paso se rompe el flujo y aplicar una corrección acotada.

### Paso 2. Cerrar la lógica y la interfaz de la sesión

1. Revisar que `shots`, `hits`, `score` y `timeRemaining` se reinicien correctamente al comenzar una nueva sesión.
2. Comprobar que cada disparo incremente una sola vez el contador de intentos.
3. Confirmar que cada acierto sume puntaje una sola vez y que los fallos no lo modifiquen.
4. Incorporar la precisión simple `hits / shots * 100` si todavía no está conectada al `GameManager`.
5. Completar el cierre del temporizador para bloquear nuevos disparos cuando llegue a cero.
6. Revisar el panel final para mostrar puntaje, impactos e intentos; agregar precisión si ya funciona correctamente.
7. Verificar que el botón `Reiniciar`, si existe, restaure las dianas, el HUD y el estado de juego.
8. Alinear retícula, textos y botones y mejorar su contraste y tamaño.
9. Comprobar que ningún panel bloquee las dianas ni intercepte accidentalmente los controles.
10. Mantener el diseño simple y evitar cambios de arquitectura o identidad visual que no aporten a la rúbrica.

### Paso 3. Ejecutar el control final

Usar esta lista antes de grabar:

- [ ] Se abre la escena correcta.
- [ ] Unity entra en Play sin errores rojos.
- [ ] La cámara reconoce el marcador.
- [ ] La escena virtual queda anclada.
- [ ] Se ven al menos tres dianas y otros objetos de la escena.
- [ ] Las dianas se mueven mediante `FloatingTarget`.
- [ ] El botón `FUEGO` ejecuta el raycast.
- [ ] Un acierto produce respuesta visual y suma puntaje.
- [ ] Un fallo no suma puntaje.
- [ ] El temporizador cambia y detiene la sesión.
- [ ] El HUD es legible.
- [ ] El flujo puede repetirse sin reiniciar el proyecto.

### Paso 4. Grabar el video

Duración objetivo: **2 minutos 45 segundos a 3 minutos 15 segundos**.

Guion recomendado:

1. **0:00–0:15 — Presentación.** Nombre del proyecto, integrantes y objetivo de esta entrega.
2. **0:15–0:35 — Estado del proyecto.** Mostrar brevemente la jerarquía y señalar `ImageTarget`, `ARContent`, las dianas, controladores y Canvas.
3. **0:35–0:55 — Inicio en Play.** Presionar Play de forma visible y mostrar el reconocimiento del marcador.
4. **0:55–1:25 — Escena y objetos.** Recorrer visualmente la plataforma, los objetos incorporados y las dianas en movimiento.
5. **1:25–2:15 — Interacción.** Mostrar un acierto, un fallo, el cambio de la diana, el puntaje y el temporizador.
6. **2:15–2:40 — Interfaz.** Mostrar la retícula, instrucciones, botón y panel final o cierre de sesión.
7. **2:40–3:00 — Cierre.** Indicar qué está funcional y qué se completará en la entrega final.

Condiciones de grabación:

- El botón Play de Unity debe verse activado durante la demostración.
- La aplicación debe ser el foco principal del video.
- No ocultar fallos con cortes rápidos; grabar una ejecución estable.
- La narración debe explicar qué GameObjects interactúan y qué script coordina el impacto.
- Evitar mostrar claves, licencias o datos sensibles en Inspector o Console.
- Confirmar que el archivo final dura entre 2 y 4 minutos y que el texto se puede leer.

### Paso 5. Entrega y respaldo

1. Ver el video completo después de exportarlo.
2. Confirmar audio, imagen, duración y legibilidad.
3. Subirlo al medio solicitado por el profesor.
4. Abrir el enlace o archivo desde otra sesión para confirmar que tiene permisos de visualización.
5. Guardar una copia local del video.
6. Registrar y subir únicamente cualquier ajuste final necesario del proyecto.

### Entregables del relevo 3

- Contadores y métricas revisados.
- Cierre y reinicio de la sesión comprobados.
- Interfaz final de avance, simple y legible.
- Prueba final completada.
- Video de 2 a 4 minutos revisado.
- Enlace o archivo comprobado.
- Proyecto final de la segunda entrega respaldado.

## 9. Matriz de comprobación contra la rúbrica

| Criterio | Evidencia que debe aparecer en el video | Objetivo |
|---|---|---:|
| Escena y objetos incorporados | Marcador reconocido, plataforma, objetos decorativos y al menos tres dianas ancladas | 5/5 |
| Interacción funcional entre GameObjects | `ShooterController` alcanza una diana, `Target` responde y `GameManager` actualiza el puntaje | 7/7 |
| Avance de la interfaz gráfica | Retícula, puntaje, temporizador, instrucción y botón `FUEGO` funcionales | 4/4 |
| Funcionamiento y avance del prototipo | Ciclo completo estable en Play, sin errores críticos, repetido más de una vez | 6/6 |
| Demostración mediante video | Duración de 2 a 4 minutos y evidencia clara de estado, funciones e interacciones | 3/3 |

## 10. Criterio de “listo para entregar”

La segunda entrega está lista solamente cuando se cumplen simultáneamente estas condiciones:

1. El flujo mínimo funciona tres veces consecutivas.
2. No hay errores rojos en Console.
3. El disparo se resuelve directamente mediante `RaycastHit`.
4. El video muestra Unity en Play y cubre las cinco filas de la rúbrica.
5. El video dura entre 2 y 4 minutos.
6. El archivo o enlace final fue abierto y revisado después de subirlo.
7. El último commit y el video tienen una copia de respaldo.

Si queda tiempo después de cumplir estas siete condiciones, se puede agregar una sola mejora opcional. Antes de cumplirlas, cualquier función adicional aumenta el riesgo de la entrega y debe postergarse.

## 11. Referencia técnica utilizada para la decisión del raycast

La documentación oficial de Unity confirma que `Physics.Raycast` puede devolver un `RaycastHit` y limitar la detección mediante un `LayerMask`. Por eso la arquitectura de esta entrega usa el collider de la diana como superficie detectable, obtiene el componente `Target` desde el objeto alcanzado y evita una segunda detección mediante colisión o trigger.

- Unity Manual: [Layers and ray casting](https://docs.unity3d.com/Manual/use-layers.html)
