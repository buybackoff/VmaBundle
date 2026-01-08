namespace VmaBundle;

/// <summary>
/// VMA_TRACELEVEL
/// </summary>
public enum VmaTraceLevel
{
    /// <summary>
    /// PANIC = 0 – Panic level logging.This trace level causes fatal behavior and halts the application, typically caused by memory allocation problems. PANIC level is rarely used.
    /// </summary>
    Panic = 0,

    /// <summary>
    /// ERROR = 1 – Runtime errors in VMA.Typically, this trace level assists you to identify internal logic errors, such as errors from underlying OS or InfiniBand verb calls, and internal double mapping/unmapping of objects.
    /// </summary>
    Error = 1,

    /// <summary>
    /// WARN = WARNING = 2– Runtime warning that does not disrupt the application workflow.A warning may indicate problems in the setup or in the overall setup configuration. For example, address resolution failures (due to an incorrect routing setup configuration), corrupted IP packets in the receive path, or unsupported functions requested by the user application.
    /// </summary>
    Warning = 2,

    /// <summary>
    /// INFO = INFORMATION = 3– General information passed to the user of the application. This trace level includes configuration logging or general information to assist you with better use of the VMA library.
    /// </summary>
    Info = 3,

    /// <summary>
    /// DEBUG = 4 – High-level insight to the operations performed in VMA.In this logging level all socket API calls are logged, and internal high-level control channels log their activity.
    /// </summary>
    Debug = 4,

    /// <summary>
    /// FINE = FUNC = 5 – Low-level runtime logging of activity.This logging level includes basic Tx and Rx logging in the fast path. Note that using this setting lowers application performance. We recommend that you use this level with the VMA_LOG_FILE parameter.
    /// </summary>
    Fine = 5,

    /// <summary>
    /// FINER = FUNC_ALL = 6 – Very low-level runtime logging of activity. This logging level drastically lowers application performance. We recommend that you use this level with the VMA_LOG_FILE parameter.
    /// </summary>
    Finer = 8
}
