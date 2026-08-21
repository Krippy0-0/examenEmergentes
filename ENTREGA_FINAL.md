# AIM-AR — estado de entrega final

## Funciones terminadas

- ImageTarget `target` desde `Vuforia/examenEmergentes.xml`.
- Colocación previa con `COLOCAR` y recuperación con `RECOLOCAR`.
- Modo Plano dentro del escenario rectangular.
- Modo 360° con dianas alrededor de la cámara e indicadores fuera de pantalla.
- Tres dificultades: Fácil, Media y Difícil.
- Sesiones de 60 segundos.
- Raycast único desde la ARCamera con `LayerMask Target`.
- Puntaje según dificultad, centro del impacto y reacción.
- Intentos, impactos, precisión, reacción promedio y rachas.
- Tiempo de vida de dianas y fallo por expiración.
- Respuesta de color, escala, partículas y sonido.
- Resultados, reinicio y récord histórico con PlayerPrefs.
- Escena `Entrenamiento` como única escena habilitada para build.

## Prueba final recomendada

1. Abrir `Assets/AIMAR/Scenes/Entrenamiento.unity`.
2. Entrar en Play sin errores rojos.
3. Mostrar la diana de la base `examenEmergentes`.
4. Probar Plano en cada dificultad.
5. Probar 360° y girar hasta ver una flecha indicadora.
6. Completar una sesión, revisar resultados y reiniciar.
7. Recolocar el campo y repetir.
8. Generar Android desde `AIM-AR > Build Android APK`.

## Evidencia que todavía requiere una persona

La evaluación con seis usuarios no puede sustituirse por datos inventados. Usar `EVALUACION_USUARIOS.md` para registrar dos sesiones reales por participante e incorporar el resumen al informe y a la presentación oral.
