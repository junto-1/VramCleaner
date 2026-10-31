# AMD VRAM Cleaner

Limpiador de memoria gráfica (VRAM) para Windows, inspirado en Windows Memory Cleaner, pensado para GPUs AMD Radeon
(funciona con cualquier GPU WDDM, pero la caché de shaders y la detección priorizan AMD).

## Qué hace
| Área | Técnica |
|---|---|
| Desalojo de VRAM | Crea buffers D3D11 hasta llenar la VRAM y los libera: WDDM expulsa a RAM las asignaciones inactivas de otros procesos |
| Working set de procesos con GPU | `EmptyWorkingSet` sobre los procesos que figuran en el contador `GPU Process Memory` |
| Caché de shaders AMD | Borra `%LOCALAPPDATA%\AMD\{DxCache,DxcCache,GLCache,VkCache}` y `%LOCALAPPDATA%\D3DSCache` |
| Lista de espera | `NtSetSystemInformation(MemoryPurgeStandbyList)` (memoria compartida de GPU vive en RAM) |
| Reiniciar driver gráfico | Envía Win+Ctrl+Shift+B (la pantalla parpadea; desactivado por defecto) |

Monitoreo: DXGI (VRAM total, LUID, fabricante) + contadores `GPU Adapter Memory` / `GPU Process Memory`.
Versión del driver: registro (`RadeonSoftwareVersion`).

## Uso
`AmdVramCleaner.exe` (pide permisos de administrador). Icono en la bandeja con % de VRAM usada,
atajo de teclado, optimización automática por tiempo o por VRAM libre, procesos excluidos, ES/EN.
Configuración en `%APPDATA%\AmdVramCleaner\settings.json`.

## Procesos, simulación, historial y registro (v1.1)

- **Procesos con memoria gráfica**: PID, VRAM dedicada, Δ en 5 min (marca `¿Fuga?` si crece >256 MB) y `Colgado` si la ventana no responde. Botones **Finalizar** (cierre normal), **Forzar** (kill del árbol) y **Excluir**. Los procesos críticos del sistema nunca se finalizan.
- **Simular (dry-run)**: muestra exactamente qué haría la optimización sin ejecutar nada.
- **Caché de archivos del sistema**: área opcional (`SetSystemFileCacheSize`), desactivada por defecto.
- **Historial de VRAM** (gráfico de 5 min) y **registro de sesión** exportable a `.txt`.

## dwm (v1.2)

`dwm.exe` (compositor de Windows) acumula superficies de todas las ventanas y con algunos drivers no las libera. El área **Reiniciar dwm** lo cierra y `winlogon` lo relanza en ~1 s (parpadeo breve, las aplicaciones siguen abiertas); solo actúa si dwm supera `DwmRestartMinMb` (1024 MB por defecto, editable en `settings.json`). dwm también entra en el recorte de working set. En la lista de procesos, **Finalizar** sobre dwm hace ese reinicio sin umbral.

## Línea de comandos

```cmd
AmdVramCleaner.exe --status                      (GPU, VRAM y procesos)
AmdVramCleaner.exe --clear [--dry-run] [--shaders] [--driver] [--no-pressure] [--dwm|--no-dwm] [--exclude a,b]
AmdVramCleaner.exe --kill <pid|nombre> [--force]
AmdVramCleaner.exe --help
```

Devuelve 0 si fue bien, 1 si hubo error.

## Compilar
```cmd
dotnet publish -c Release -r win-x64 --self-contained true -o publish\self-contained
dotnet publish -c Release -r win-x64 --self-contained false -o publish\framework-dependent
```
La variante *framework-dependent* requiere .NET 8 Desktop Runtime.

## Modo Extremo (1.3)

Pensado para jugar: cierra todas las aplicaciones de usuario y deja solo el juego y lo imprescindible del sistema; cuando el juego termina, reabre automáticamente lo que cerró.

- Selecciona el juego en la lista de procesos (o déjalo a pantalla completa) y pulsa **Activar modo Extremo** (también en el menú de la bandeja).
- Se cierran (primero con suavidad, forzado tras 5 s) los procesos de tu sesión con ventana o con memoria gráfica, salvo: el juego y los exes de su carpeta, procesos del sistema (`C:\Windows`), driver/Adrenalin de AMD, launchers (Steam, Epic, Battle.net, GOG, EA, Riot, Ubisoft), anti-cheats, Discord, OBS, software de periféricos y tus procesos excluidos.
- Tras cerrar ejecuta una optimización normal. La lista de apps cerradas se guarda en `%APPDATA%\AmdVramCleaner\extreme-session.json`; si el limpiador se cerrara en medio, al abrirlo de nuevo pregunta si reabrirlas.
- **Activar solo al detectar un juego a pantalla completa**: entra solo cuando la ventana en primer plano ocupa toda la pantalla y usa VRAM.
- Botón **Salir del modo Extremo** para reabrir todo sin esperar a que el juego cierre.
- El reinicio de dwm se omite mientras el modo Extremo está activo o hay un juego a pantalla completa (reiniciar el compositor bajo un juego puede dejar el cursor invisible hasta hacer Alt+Tab); tras cualquier reinicio de dwm se mueve el ratón un píxel para redibujar el cursor.

### 1.3.1
- **Aviso de VRAM al nivel de peligro**: notificación única cuando la VRAM dedicada supera el nivel de peligro (se rearma al bajar del nivel de aviso). Casilla dentro de *Optimización automática*.
- **Optimizar en 10 s** en el menú de bandeja: deja tiempo para volver al juego a pantalla completa.
- El modo Extremo recuerda los **argumentos de línea de comandos** de las apps al reabrirlas.
- Casilla **Mantener Discord, OBS y navegador**: al desmarcarla, el modo Extremo también cierra navegadores, Discord, OBS, chat y software de periféricos.
- Ventana redimensionable (el botón Optimizar se ancla abajo) y panel del modo Extremo corregido (textos dentro del marco).
