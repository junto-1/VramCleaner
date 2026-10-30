namespace AmdVramCleaner;

public static class L
{
    public static string Lang = "es";

    static readonly Dictionary<string, string> Es = new()
    {
        ["cfg"] = "CONFIGURACIÓN", ["closeToTray"] = "Cerrar al área de notificación", ["closeAfter"] = "Cerrar después de la optimización",
        ["startup"] = "Ejecutar al inicio", ["lowPriority"] = "Ejecutar con baja prioridad", ["startMin"] = "Iniciar minimizado",
        ["showShared"] = "Mostrar memoria compartida", ["notify"] = "Mostrar notificaciones de optimización", ["onTop"] = "Siempre encima",
        ["areas"] = "ÁREAS DE MEMORIA GRÁFICA", ["areaVram"] = "Desalojo de VRAM (presión D3D11)", ["areaWs"] = "Working set de procesos con GPU",
        ["areaShader"] = "Caché de shaders AMD (Dx/GL/Vk)", ["areaStandby"] = "Lista de espera (memoria compartida)", ["areaDriver"] = "Reiniciar driver gráfico (parpadeo)",
        ["hotkey"] = "ATAJO DE TECLADO PARA OPTIMIZAR", ["tray"] = "ICONO DE LA BANDEJA",
        ["trayMiddle"] = "Optimizar al hacer clic en el botón central del ratón", ["trayUsage"] = "Mostrar uso de VRAM", ["trayTransparent"] = "Usar fondo transparente",
        ["back"] = "Fondo", ["text"] = "Texto", ["optimized"] = "Optimizado", ["warning"] = "Nivel de advertencia", ["danger"] = "Nivel de peligro",
        ["excluded"] = "PROCESOS EXCLUIDOS DE LA OPTIMIZACIÓN", ["auto"] = "OPTIMIZACIÓN AUTOMÁTICA",
        ["every"] = "Cada {0}", ["disabled"] = "Desactivado", ["freeBelow"] = "Cuando la VRAM libre es inferior al {0}%",
        ["vram"] = "MEMORIA DE VIDEO ({0})", ["shared"] = "MEMORIA COMPARTIDA GPU ({0})", ["used"] = "Usado", ["free"] = "Libre",
        ["optimize"] = "Optimizar", ["optimizing"] = "Optimizando...", ["show"] = "Mostrar", ["exit"] = "Salir",
        ["doneTitle"] = "Optimización completada", ["done"] = "VRAM: {0} → {1} ({2} liberados). Procesos: {3}. Shaders: {4}.",
        ["noGpu"] = "No se detectó ninguna GPU compatible", ["notAmd"] = "GPU no AMD detectada: {0}", ["gpu"] = "GPU: {0}   Driver: {1}",
        ["driverConfirm"] = "Se reiniciará el driver gráfico: la pantalla parpadeará unos segundos y los juegos abiertos pueden verse afectados.\n¿Continuar?",
        ["removeHint"] = "Doble clic para quitar", ["hours"] = "h", ["minutes"] = "min",
    };

    static readonly Dictionary<string, string> En = new()
    {
        ["cfg"] = "SETTINGS", ["closeToTray"] = "Close to the notification area", ["closeAfter"] = "Close after optimization",
        ["startup"] = "Run on startup", ["lowPriority"] = "Run on low priority", ["startMin"] = "Start minimized",
        ["showShared"] = "Show shared memory", ["notify"] = "Show optimization notifications", ["onTop"] = "Always on top",
        ["areas"] = "GRAPHICS MEMORY AREAS", ["areaVram"] = "VRAM eviction (D3D11 pressure)", ["areaWs"] = "Working set of GPU processes",
        ["areaShader"] = "AMD shader cache (Dx/GL/Vk)", ["areaStandby"] = "Standby list (shared memory)", ["areaDriver"] = "Restart graphics driver (flicker)",
        ["hotkey"] = "OPTIMIZATION HOTKEY", ["tray"] = "TRAY ICON",
        ["trayMiddle"] = "Optimize on middle mouse click", ["trayUsage"] = "Show VRAM usage", ["trayTransparent"] = "Use transparent background",
        ["back"] = "Background", ["text"] = "Text", ["optimized"] = "Optimized", ["warning"] = "Warning level", ["danger"] = "Danger level",
        ["excluded"] = "PROCESSES EXCLUDED FROM OPTIMIZATION", ["auto"] = "AUTO OPTIMIZATION",
        ["every"] = "Every {0}", ["disabled"] = "Disabled", ["freeBelow"] = "When free VRAM is below {0}%",
        ["vram"] = "VIDEO MEMORY ({0})", ["shared"] = "GPU SHARED MEMORY ({0})", ["used"] = "Used", ["free"] = "Free",
        ["optimize"] = "Optimize", ["optimizing"] = "Optimizing...", ["show"] = "Show", ["exit"] = "Exit",
        ["doneTitle"] = "Optimization completed", ["done"] = "VRAM: {0} → {1} ({2} freed). Processes: {3}. Shaders: {4}.",
        ["noGpu"] = "No compatible GPU detected", ["notAmd"] = "Non-AMD GPU detected: {0}", ["gpu"] = "GPU: {0}   Driver: {1}",
        ["driverConfirm"] = "The graphics driver will be restarted: the screen will flicker for a few seconds and running games may be affected.\nContinue?",
        ["removeHint"] = "Double click to remove", ["hours"] = "h", ["minutes"] = "min",
    };

    public static string T(string key, params object[] args)
    {
        var d = Lang == "en" ? En : Es;
        var s = d.TryGetValue(key, out var v) ? v : key;
        return args.Length == 0 ? s : string.Format(s, args);
    }
}
