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

## Compilar
```cmd
dotnet publish -c Release -r win-x64 --self-contained true -o publish\self-contained
dotnet publish -c Release -r win-x64 --self-contained false -o publish\framework-dependent
```
La variante *framework-dependent* requiere .NET 8 Desktop Runtime.
