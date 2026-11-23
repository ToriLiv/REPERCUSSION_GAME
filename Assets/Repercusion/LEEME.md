# Repercusión: UI y Sala 1

Este paquete trae:

- **Menú principal** con botones Jugar, Cómo jugar y Salir.
- **HUD** con la barra de luz, las 3 llaves, el contador de ecos y los mensajes en pantalla.
- **Menú de pausa** con Continuar, Reiniciar sala y Menú principal.
- **La mecánica de ecos**, lista para usar.
- **Sala 1** con placa, puerta, llave y una pista.
- **Peligros, brasero y poción**, para la luz.

La UI es en **blanco y negro**: fondo negro con círculos blancos. Los botones se invierten a blanco con letras negras cuando los seleccionas.

Todo se arma solo desde un menú nuevo llamado **Repercusión** que aparece arriba en Unity.

---

## Instalar (5 minutos)

1. **Haz un commit o una copia de tu proyecto antes.** Por si acaso.
2. Arrastra la carpeta **Repercusion** a la ventana **Project** de Unity, dentro de `Assets`.
3. Espera a que compile. Arriba, junto a *Window* y *Help*, va a aparecer el menú **Repercusión**.

> Si te sale un error de que `CameraFollow` está duplicado, es porque tienes `CameraFollow.cs` y `CameraFollow_v2.cs` con la misma clase. Borra uno de los dos.

---

## Paso 1: menú principal

**Repercusión → 1. Crear escena del Menú principal**

- La primera vez puede decirte que está importando *TextMeshPro*. Espera a que termine y vuelve a darle al mismo comando.
- Crea la escena `MenuPrincipal` en `Repercusion/Escenas` y la pone **primera** en la lista de Build.

## Paso 1b: intro

**Repercusión → 1b. Crear escena de Intro**

- Crea la escena `Intro` y la pone **después del menú** en la lista de Build. El orden queda: MenuPrincipal → Intro → tu juego.
- Son 6 frases que se escriben letra por letra sobre el fondo de círculos, con un golpe de glitch en "Y el tiempo se rompió".
- **Para cambiar los textos:** objeto `UI_Intro` → componente *Intro Secuencia* → lista *Diapositivas*. A cada una le puedes poner un dibujo tuyo en *Imagen*; si lo tiene, el texto baja solo.
- Clic o Espacio avanza; Esc salta toda la intro.

## Paso 2: HUD y pausa en tu escena del juego

1. Abre **tu escena del juego**, la que tiene el mapa y a Eco.
2. Dale **Repercusión → 2. Agregar HUD, pausa y GameManager a esta escena**.

Esto hace:

- Crea el HUD y la pausa, ya conectados.
- A **Eco** le agrega `LuzEco` y `EcoGrabadora`, y pone Gravity Scale en 0. Si tenía `PlayerEnergy`, lo desactiva porque ahora la luz la maneja `LuzEco`.
- A la **Main Camera** le agrega `CamaraTopDown`, que sigue a Eco en X y en Y. Desactiva tu `CameraFollow`, que solo seguía en X.
- Crea el **GameManager** y un **PuntoCentro**. Mueve PuntoCentro al centro del mapa: ahí reaparece Eco cuando se queda sin luz.
- Agrega la escena a la lista de Build, después del menú.

Guarda con **Ctrl+S**.

> Busca a Eco por el Tag **Player** o por el script `TopDownMovement`. Si no la encuentra, selecciónala y usa **Repercusión → Configurar a Eco (seleccionada)**.

## Paso 3: Sala 1

1. En la vista **Scene**, mira hacia donde va la sala 1 de tu mapa.
2. Dale **Repercusión → 3. Crear Sala 1 (placa + puerta + llave)**.
3. Acomódala:
   - **Mueve** el objeto `Sala1_ElPrimerEco` a su lugar.
   - **Área de la sala:** en su *Box Collider 2D*, usa *Edit Collider* para que cubra **toda** la sala.
   - **Puerta:** que tape el único paso hacia la llave. La placa tiene que quedar lejos de la puerta, para que no se pueda pisar la placa y pasar a tiempo.
   - **PuntoInicio:** en la entrada, **dentro** del área de la sala.

## Paso 4 (opcional): luz

- **Repercusión → 4. Crear peligro, brasero y poción de prueba**
- **Repercusión → 5. Crear puerta de salida (centro)**: se abre cuando llegas con las 3 llaves.

## Probar

Abre `MenuPrincipal` y dale **Play**.

| Tecla | Qué hace |
|---|---|
| WASD / flechas | Moverse |
| **R** | Nuevo intento (deja un eco) |
| Esc o P | Pausa |

**Cómo se resuelve la Sala 1:** te paras en la placa (la puerta se abre) y presionas **R**. Vuelves al inicio y tu eco repite lo que hiciste: camina hasta la placa y se queda ahí. Con la puerta abierta, pasas por la llave.

---

## Cómo funciona el eco (para explicarlo en la presentación)

1. Al entrar a una `SalaPuzle`, `EcoGrabadora` empieza a grabar la posición de Eco 50 veces por segundo.
2. Al presionar **R**, esa grabación se convierte en un **eco**: una copia translúcida de Eco que usa sus mismas animaciones.
3. Eco vuelve al inicio y **todos los ecos repiten su camino al mismo tiempo que ella**.
4. Cuando un eco termina su camino, **se queda quieto**. Por eso puede sostener una placa.
5. Cada sala define cuántos ecos permite (`Max Ecos`): la sala 1 permite 1. Si haces más intentos, se reemplaza el eco más viejo.
6. Las placas revisan si los **pies** de Eco o de un eco están encima. Las puertas se abren cuando **todas** sus placas están presionadas.

Para las salas 2 y 3, duplica la sala 1 y cambia:

- **Sala 2 (2 placas):** `Max Ecos` = 2 y agrega la segunda placa a la lista `Placas` de la puerta.
- **Sala 3 (puertas en serie):** varias puertas, cada una con su propia placa.

---

## Cambiar el arte por el tuyo

Los dibujos son **provisionales**. Para cambiarlos:

- **Reemplaza el PNG con el mismo nombre** en `UI/Sprites` o `Mundo/Sprites`. Luego usa **Repercusión → 0. Configurar sprites**.
- **Tamaño:** los sprites del mundo van a **128 px por casilla**. La puerta mide 1 × 3 casillas (128 × 384 px).
- **Efecto glitch al hacer un intento:** en Eco, en `EcoGrabadora → Al Nuevo Intento`, dale **+**, arrastra el objeto con tu `GlitchPulse` y elige **Pulso()**.

## Si algo falla

- **No se ven los textos:** importa TextMeshPro desde *Window → TextMeshPro → Import TMP Essential Resources*.
- **Los botones no responden o hay un error del EventSystem:** ve a *Edit → Project Settings → Player → Active Input Handling* y elige **Both**.
- **No se ve la placa (queda debajo del piso):** en su *Sprite Renderer*, sube *Order in Layer* uno arriba del piso.
- **Al presionar R el eco desaparece enseguida:** PuntoInicio está fuera del área de la sala. Muévelo adentro.
- **El eco no se anima:** `EcoFantasma` usa los parámetros `IsMoving`, `MoveX` y `MoveY`, los mismos de tu Animator. Si los renombraste, cámbialos en `EcoFantasma`.
- **Para las pruebas con jugadores "sin ayuda":** desactiva el objeto `Pista` dentro de la sala.
