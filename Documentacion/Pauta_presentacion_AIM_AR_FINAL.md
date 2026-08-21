# AIM-AR — pauta final para presentar

Tiempo recomendado: 9 minutos de presentación y hasta 3 minutos para preguntas. La demostración debe concentrarse en el funcionamiento; no lean las diapositivas ni recorran el Inspector sin un propósito claro.

## Reparto sugerido

| Persona | Tiempo | Parte |
|---|---:|---|
| Mattias Morales | 0:00–2:15 | Problema, objetivo, tecnología y alcance final. |
| Carlos Orellana | 2:15–5:00 | Arquitectura, modos Plano/360°, dificultades y flujo técnico. |
| Ariel Van Kilsdonk | 5:00–9:00 | Métricas, interfaz, demo, resultados y cierre. |
| Los tres | Preguntas | Cada integrante responde desde su área y complementa cuando sea necesario. |

## Guion completo

### 1. Apertura — Mattias

“Somos Mattias Morales, Carlos Orellana y Ariel Van Kilsdonk. AIM-AR convierte un marcador impreso en un campo de entrenamiento de puntería en realidad aumentada. El usuario apunta moviendo el teléfono, dispara desde una retícula central y recibe métricas objetivas de su desempeño.”

### 2. Problema, objetivo y tecnología — Mattias

“Los entrenadores de puntería tradicionales requieren computador, mouse y un puesto fijo; la realidad virtual agrega el costo de un visor. AIM-AR utiliza un teléfono Android, Unity y Vuforia para incorporar movimiento físico en una experiencia accesible.”

“La imagen `aimar` pertenece a la Device Database `examenEmergentes` de Vuforia. Al reconocer el ImageTarget, Vuforia entrega la pose de referencia para presentar el contenido virtual. Desde el menú principal entramos a la configuración, elegimos las opciones y confirmamos con COLOCAR.”

“La versión final incorpora un menú principal, sesiones de 60 segundos, tres dificultades, dos disposiciones espaciales, dificultad adaptativa opcional, métricas de precisión y reacción, récord histórico, feedback audiovisual y una interfaz completa de configuración y resultados.”

### 3. Modos de entrenamiento — Carlos

“Antes de comenzar se puede elegir entre modo Plano y modo 360°. En Plano, la plataforma y las tres dianas permanecen dentro del campo rectangular proyectado desde el marcador. Es el modo más directo para entrenar precisión frontal.”

“En 360°, TargetSpawner desacopla ARContent del marcador, lo centra en la posición inicial de la cámara y distribuye las dianas sobre un cilindro fijo de 2,2 metros de radio alrededor del jugador. Las dianas se separan angularmente y miran hacia el centro. No agregamos otra lectura de giroscopio: Vuforia ya actualiza la pose de la ARCamera, por lo que al girar el teléfono la cámara recorre naturalmente ese cilindro. Si una diana queda fuera de cámara, OffscreenTargetIndicator usa WorldToScreenPoint y muestra una flecha en el borde.”

“Las dificultades modifican tamaño y velocidad. Fácil usa dianas más grandes y lentas; Medio mantiene valores equilibrados; Difícil reduce el tamaño y acelera el movimiento. Las dianas no desaparecen por tiempo ni cambian de lugar ante un fallo: solamente se reubican después de un impacto válido.”

“El modo adaptativo es opcional. GameManager conserva una ventana de los últimos ocho resultados y comienza a ajustar después de cuatro disparos. Si la tasa reciente de aciertos supera 60 %, reduce progresivamente la escala; si cae bajo 45 %, la aumenta. La escala queda limitada entre 62 % y 128 %, y se muestra en el HUD.”

### 4. Flujo técnico — Carlos

“El botón FUEGO llama directamente a ShooterController.Shoot(). El script primero pregunta al GameManager si la sesión está activa y registra un único intento. Luego crea un rayo desde la posición de la ARCamera usando su dirección forward.”

“Physics.RaycastAll utiliza exclusivamente el LayerMask Target. Los resultados se ordenan por distancia, así que si dos dianas se superponen se procesa primero la más cercana a la cámara. El collider es la superficie detectable, pero no usamos OnCollisionEnter ni OnTriggerEnter. ShooterController obtiene Target con GetComponentInParent y llama a ReceiveHit; si ningún impacto válido responde, registra el fallo.”

“Target transforma el punto de impacto a coordenadas locales para estimar qué tan cerca quedó del centro. También mide el tiempo desde su última activación. Envía esos valores al GameManager, desactiva temporalmente sus colliders para evitar impactos duplicados, reproduce color, escala, partículas y sonido, y recién entonces solicita una nueva posición al TargetSpawner.”

“GameManager coordina los estados Setup, Playing y Finished; calcula puntaje, impactos, intentos, precisión, reacción promedio y mejor racha. ScoreRepository guarda con PlayerPrefs los mejores resultados entre sesiones.”

### 5. Interfaz y resultados — Ariel

“La aplicación comienza en un hub principal. En Setup se puede alternar modo, dificultad y adaptación. Al presionar COLOCAR comienza una sesión de 60 segundos. El HUD muestra puntaje, tiempo, impactos, intentos, precisión, racha y, si corresponde, escala adaptativa. En 360° también aparecen indicadores direccionales fuera de cámara. CANCELAR termina la ronda y vuelve al hub.”

“Al terminar, el panel final muestra la configuración utilizada, el resultado de la sesión, reacción promedio, mejor racha y récord histórico. REINICIAR conserva la colocación; RECOLOCAR vuelve a enganchar el campo al marcador.”

### 6. Secuencia de demostración — Ariel

1. Mostrar brevemente la jerarquía: `ImageTarget_AIMAR`, `ARContent`, `TargetSpawner`, `GameManager`, `ShooterController` y `AIMAR_HUD`.
2. Entrar en Play y mostrar `Buscando marcador` → `Marcador detectado`.
3. Desde el hub, entrar a entrenamiento; en modo Plano elegir dificultad y activar `ADAPTATIVO`.
4. Presionar `COLOCAR`, mostrar una diana en movimiento, un acierto y un fallo.
5. Señalar que cambian intentos, precisión, racha, puntaje, tiempo y escala adaptativa.
6. Comprobar que el fallo no reubica dianas y que el acierto sí activa color, escala, partículas, sonido y una nueva posición.
7. Usar `CANCELAR` para volver al hub; entrar nuevamente y seleccionar `MODO: 360°`.
8. Girar la cámara para mostrar el cilindro fijo, las dianas alrededor y una flecha direccional.
9. Cuando dos dianas coincidan en pantalla, explicar que `RaycastAll` prioriza la más cercana.
10. Terminar o acelerar una sesión de respaldo para enseñar el panel final y el récord.

### 7. Cierre — Ariel

“AIM-AR cumple el ciclo propuesto: reconocimiento mediante Vuforia, configuración desde un hub, colocación espacial, interacción con RaycastAll, modo Plano, cilindro 360°, tres dificultades, adaptación opcional, feedback audiovisual, métricas y persistencia local. La arquitectura separa detección, reglas, distribución espacial, respuesta de las dianas e interfaz, permitiendo extender el proyecto sin duplicar la lógica de impacto.”

## Preguntas probables y respuestas

### ¿Por qué Vuforia y no ARCore?

Porque el ImageTarget aporta una referencia visual conocida y repetible. Para este proyecto permite colocar el escenario con una imagen impresa y demostrar el mismo flujo en editor y Android sin depender de la detección de superficies.

### ¿El contenido permanece unido al marcador?

Durante Setup sí: `ARContent` es hijo del `ImageTarget` y sigue su pose. Al presionar `COLOCAR`, `GameManager` usa `SetParent(null, true)` para conservar la pose mundial. En modo Plano queda fijo en esa ubicación; en 360° `TargetSpawner` lo recentra alrededor de la cámara.

### ¿Cómo funciona el disparo?

`FUEGO` llama a `ShooterController.Shoot()`. Se registra el intento y se ejecuta `Physics.RaycastAll` desde la ARCamera con la capa `Target`. Los impactos se ordenan por distancia y se prueba primero la diana más cercana. `RaycastHit` permite obtener `Target` y enviarle el punto de impacto.

### ¿Por qué no usan colisiones o triggers?

Porque el raycast es la única fuente de verdad del disparo. El collider sigue siendo necesario como superficie detectable, pero agregar otra detección produciría impactos duplicados o estados difíciles de sincronizar.

### ¿Cómo se calcula el puntaje?

El puntaje combina una base según la dificultad con dos bonificaciones: cercanía al centro de la diana y rapidez de reacción. Los fallos reducen la precisión y reinician la racha. Las dianas no expiran ni generan disparos fantasma.

### ¿Qué cambia entre dificultades?

Cambian el tamaño de la diana y la velocidad de movimiento. La arquitectura mantiene las mismas reglas y modifica solamente parámetros controlados.

### ¿Cómo funciona el modo 360°?

Al iniciar, `TargetSpawner` crea un cilindro fijo centrado en la posición inicial de la cámara, con dianas distribuidas angularmente y orientadas hacia el jugador. La rotación visible proviene de la pose de la ARCamera entregada por Vuforia; no se aplica un segundo giro manual. `OffscreenTargetIndicator` usa `WorldToScreenPoint` y coloca flechas en el borde cuando corresponde.

### ¿Cómo funciona el modo adaptativo?

`GameManager` guarda hasta ocho resultados recientes. Desde el cuarto disparo calcula la proporción de aciertos: sobre 60 % reduce el tamaño hacia 62 %; bajo 45 % lo aumenta hasta 128 %. Puede desactivarse antes de comenzar.

### ¿Qué pasa si dos dianas se superponen?

`Physics.RaycastAll` obtiene todos los impactos sobre la capa `Target` y los ordena por distancia. El primer `Target` válido es el más cercano a la cámara, que coincide con la diana visualmente frontal.

### ¿Qué se guarda entre sesiones?

`ScoreRepository` utiliza `PlayerPrefs` para conservar mejor puntaje, mejor precisión, menor reacción promedio y mejor racha.

### ¿Qué ocurre si se pierde el marcador?

Antes de colocar, la interfaz vuelve a indicar que está buscando el marcador. Después de confirmar, el escenario ya conserva su transformación mundial, por lo que una pérdida breve del seguimiento no reinicia la sesión.

## Checklist de la entrega final

- Abrir `Assets/AIMAR/Scenes/Entrenamiento.unity`.
- Confirmar que `Entrenamiento` sea la única escena habilitada en Build Settings.
- Probar Plano y 360° en modo Play.
- Probar Fácil, Medio y Difícil.
- Probar el adaptativo activado y desactivado.
- Completar o acelerar una sesión y revisar todas las métricas.
- Ejecutar `REINICIAR` y `RECOLOCAR`.
- Probar `CANCELAR` y confirmar el regreso al hub.
- Confirmar cero errores rojos en Console.
- Probar el APK con la diana de la base `examenEmergentes`.
- Grabar un video de respaldo con una ejecución estable.
- No mostrar la licencia de Vuforia, tokens ni información privada.
- Registrar resultados reales de las pruebas con usuarios; no inventarlos.
