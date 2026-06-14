# 🎵 Reproductor Musical y Visualizador Dinámico

Un reproductor de música interactivo escrito en **C# (WinForms)** enfocado en la visualización gráfica de audio en tiempo real. La aplicación analiza los datos del espectro de audio (FFT) y genera impresionantes representaciones geométricas y de partículas que reaccionan de manera precisa y orgánica a la música.

## 🚀 Características Principales

*   **Reproductor de Audio:** Soporte para cargar y reproducir varios formatos de audio (MP3, WAV, FLAC, etc.) con controles intuitivos, barra de progreso interactiva y control de volumen.
*   **Motor Gráfico a 60 FPS:** El bucle de renderizado (`MainForm.cs`) implementa una técnica de *Doble Buffer optimizado*, que reutiliza los Bitmaps en memoria para evitar el trabajo excesivo del Recolector de Basura (Garbage Collector), asegurando una animación sin cortes ni lag, garantizando 60 fotogramas por segundo fluidos.
*   **Audio-Reactividad Sensible:** Todos los visualizadores reaccionan directamente a diferentes bandas de frecuencias analizadas (bajos, medios y altos). Usan técnicas de *interpolación lineal (Lerp)* para movimientos suaves en la rotación, pero conservan la energía cruda (raw) en los saltos de tamaño para generar un impacto rítmico (parpadeo o *pulsating*).

## 🎨 Modos de Visualización (`Visualizer.cs`)

El visualizador cuenta con varios modos que pueden seleccionarse desde la interfaz gráfica:

1.  **Espectro de Barras:** Una representación clásica con barras verticales que reflejan la intensidad por bandas de frecuencia usando un degradado dinámico.
2.  **Partículas (`ParticleSystem.cs`):** Un emisor parametrizable y altamente reactivo que funciona desde cero: si no hay música, no hay partículas. Cuando entran los bajos y medios, se generan explosiones agresivas de figuras geométricas (círculos, diamantes, líneas y triángulos) que salen despedidas desde el centro y giran según las notas más agudas.
3.  **Onda Circular:** Una onda concéntrica en constante movimiento. Cuenta con picos reactivos que crecen fuertemente con la frecuencia general de la canción y un centro palpitante que reacciona a los "golpes" de la música.
4.  **Pulso Geométrico:** Animación geométrica renovada con figuras en forma de estrella concéntricas que reaccionan armando "telarañas" y mutando la cantidad de vértices según los tonos agudos de la pista. El tamaño general palpita intensa y repentinamente con los bajos de la canción, manteniendo una rotación fluida y constante.
5.  **Onda Rellenada (Filled Spectrum Wave):** Una representación fluida que dibuja el espectro como una onda suave y rellena, creando un efecto visual continuo y vibrante.
6.  **Osciloscopio:** Una animación que simula un osciloscopio clásico, mostrando las ondas de audio en tiempo real con líneas continuas y fluidas.

## 🛠 Arquitectura del Código

La base de código respeta fuertemente principios de **Clean Code** y **Reglas Locales (`/codigoEspañol`)**:

*   **`Forms/MainForm.cs`:** Maneja la interfaz de usuario, el loop principal de dibujado gráfico (Timer a 16ms) usando buffers reutilizables, y el enrutamiento de los eventos del usuario hacia el motor de audio.
*   **`Core/AudioEngine.cs`:** (Motor interno) Gestiona la decodificación de los archivos de audio, el control de la pista y notifica los resultados de la Transformada Rápida de Fourier (FFT) listos para la visualización.
*   **`Graphics/Visualizer.cs`:** Recibe los datos FFT y orquesta los diferentes modos gráficos. Contiene el mapeo matemático que traduce `_bassEnergy`, `_midEnergy` y `_highEnergy` en radios, rotaciones y matices HSV.
*   **`Graphics/ParticleSystem.cs`:** Un micro-motor dedicado a mantener el ciclo de vida, la física, el movimiento (Velocidad `VX/VY`) y el dibujo individual de cientos de partículas simultáneas.

## 📝 Actualización Continua (Documentación Viva)
*Este README se actualizará de forma automática tras nuevas integraciones significativas siguiendo la regla `/readmeVivo`.*
