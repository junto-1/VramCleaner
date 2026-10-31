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
        ["areaShader"] = "Caché de shaders AMD (Dx/GL/Vk)", ["areaStandby"] = "Lista de espera (memoria compartida)", ["areaDriver"] = "Reiniciar driver gráfico (parpadeo)", ["areaDwm"] = "Reiniciar dwm si acapara VRAM (>1 GB)", ["ram"] = "RAM del sistema ( {0} )", ["vramAlert"] = "Avisar al alcanzar el nivel de peligro", ["vramAlertTitle"] = "VRAM llena", ["vramAlertBody"] = "VRAM dedicada al {0}%. Considera optimizar o activar el modo Extremo.", ["optimizeDelay"] = "Optimizar en 10 s (deja tiempo para volver al juego)", ["delayS"] = "Optimización programada en {0} s", ["extremeKeepUser"] = "Mantener Discord, OBS y navegador", ["extremeAutoShort"] = "Auto-activar con juego a pantalla completa", ["extreme"] = "MODO EXTREMO", ["extremeEnter"] = "Activar modo Extremo", ["extremeExit"] = "Salir del modo Extremo (reabrir apps)", ["extremeAuto"] = "Auto con pant. completa", ["extremeIdle"] = "Cierra todas las apps salvo el juego; las reabre al terminar.", ["extremeOn"] = "Activo · juego: {0} · {1} apps cerradas (se reabrirán al cerrar el juego)", ["extremeNoGame"] = "Selecciona el juego en la lista de procesos o ponlo a pantalla completa.", ["extremeConfirm"] = "Se cerrarán todas las aplicaciones abiertas excepto {0} y el sistema. Se reabrirán al cerrar el juego.\n¿Continuar?", ["logExtremeOn"] = "Modo Extremo: {0} apps cerradas, juego {1}", ["logExtremeOff"] = "Modo Extremo terminado: {0} apps reabiertas", ["extremeRestoreTitle"] = "Modo Extremo pendiente", ["extremeRestore"] = "La sesión anterior terminó en modo Extremo. ¿Reabrir las {0} apps cerradas?", ["dwmConfirm"] = "Se reiniciará dwm (compositor de Windows): la pantalla parpadeará ~1 s, las aplicaciones siguen abiertas.\n¿Continuar?", ["logDwm"] = "dwm reiniciado: VRAM {0} → {1}",
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
        ["areaFileCache"] = "Caché de archivos del sistema", ["procs"] = "PROCESOS CON MEMORIA GRÁFICA", ["procNote"] = "* dwm cuenta las superficies de todas las ventanas (también contadas en su dueño); la suma puede superar el total.", ["colProc"] = "Proceso", ["colPid"] = "PID",
        ["colVram"] = "VRAM", ["colShared"] = "Compartida", ["colDelta"] = "Δ 5 min", ["colState"] = "Estado", ["hung"] = "Colgado", ["leak"] = "¿Fuga?",
        ["kill"] = "Finalizar", ["forceKill"] = "Forzar", ["exclude"] = "Excluir", ["dryRun"] = "Simular", ["history"] = "HISTORIAL DE VRAM (5 min)",
        ["log"] = "REGISTRO DE SESIÓN", ["export"] = "Exportar", ["killConfirm"] = "¿Finalizar {0} (PID {1})?", ["killFail"] = "No se pudo finalizar {0}",
        ["dryRunTitle"] = "Simulación (no se ejecutó nada)", ["logDone"] = "Optimización: VRAM {0} → {1} ({2}), procesos {3}, shaders {4}",
        ["logKilled"] = "Proceso finalizado: {0} (PID {1})", ["logStart"] = "Inicio · GPU: {0}", ["logExcluded"] = "Excluido: {0}",
    };

    static readonly Dictionary<string, string> En = new()
    {
        ["cfg"] = "SETTINGS", ["closeToTray"] = "Close to the notification area", ["closeAfter"] = "Close after optimization",
        ["startup"] = "Run on startup", ["lowPriority"] = "Run on low priority", ["startMin"] = "Start minimized",
        ["showShared"] = "Show shared memory", ["notify"] = "Show optimization notifications", ["onTop"] = "Always on top",
        ["areas"] = "GRAPHICS MEMORY AREAS", ["areaVram"] = "VRAM eviction (D3D11 pressure)", ["areaWs"] = "Working set of GPU processes",
        ["areaShader"] = "AMD shader cache (Dx/GL/Vk)", ["areaStandby"] = "Standby list (shared memory)", ["areaDriver"] = "Restart graphics driver (flicker)", ["areaDwm"] = "Restart dwm when it hogs VRAM (>1 GB)", ["ram"] = "System RAM ( {0} )", ["vramAlert"] = "Warn when the danger level is reached", ["vramAlertTitle"] = "VRAM full", ["vramAlertBody"] = "Dedicated VRAM at {0}%. Consider optimizing or enabling Extreme mode.", ["optimizeDelay"] = "Optimize in 10 s (time to get back to the game)", ["delayS"] = "Optimization scheduled in {0} s", ["extremeKeepUser"] = "Keep browsers, Discord, OBS and chat", ["extremeAutoShort"] = "Auto-enable on fullscreen game", ["extreme"] = "EXTREME MODE", ["extremeEnter"] = "Enter Extreme mode", ["extremeExit"] = "Leave Extreme mode (reopen apps)", ["extremeAuto"] = "Enter automatically when a fullscreen game is detected", ["extremeIdle"] = "Closes every app except the game; reopens them when it ends.", ["extremeOn"] = "Active · game: {0} · {1} apps closed (reopened when the game exits)", ["extremeNoGame"] = "Select the game in the process list or put it fullscreen.", ["extremeConfirm"] = "Every open application except {0} and the system will be closed. They reopen when the game exits.\nContinue?", ["logExtremeOn"] = "Extreme mode: {0} apps closed, game {1}", ["logExtremeOff"] = "Extreme mode ended: {0} apps reopened", ["extremeRestoreTitle"] = "Pending Extreme mode", ["extremeRestore"] = "Last session ended in Extreme mode. Reopen the {0} closed apps?", ["dwmConfirm"] = "dwm (Windows compositor) will be restarted: the screen flickers ~1 s, applications stay open.\nContinue?", ["logDwm"] = "dwm restarted: VRAM {0} → {1}",
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
        ["areaFileCache"] = "System file cache", ["procs"] = "PROCESSES USING GRAPHICS MEMORY", ["procNote"] = "* dwm counts every window surface (also counted in its owner); process sum may exceed total usage.", ["colProc"] = "Process", ["colPid"] = "PID",
        ["colVram"] = "VRAM", ["colShared"] = "Shared", ["colDelta"] = "Δ 5 min", ["colState"] = "State", ["hung"] = "Hung", ["leak"] = "Leak?",
        ["kill"] = "End task", ["forceKill"] = "Force", ["exclude"] = "Exclude", ["dryRun"] = "Dry run", ["history"] = "VRAM HISTORY (5 min)",
        ["log"] = "SESSION LOG", ["export"] = "Export", ["killConfirm"] = "End {0} (PID {1})?", ["killFail"] = "Could not end {0}",
        ["dryRunTitle"] = "Dry run (nothing was executed)", ["logDone"] = "Optimization: VRAM {0} → {1} ({2}), processes {3}, shaders {4}",
        ["logKilled"] = "Process ended: {0} (PID {1})", ["logStart"] = "Start · GPU: {0}", ["logExcluded"] = "Excluded: {0}",
    };

    public static string T(string key, params object[] args)
    {
        var d = Lang == "en" ? En : Es;
        var s = d.TryGetValue(key, out var v) ? v : key;
        return args.Length == 0 ? s : string.Format(s, args);
    }
}
