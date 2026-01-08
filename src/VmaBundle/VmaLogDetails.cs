namespace VmaBundle;

/// <summary>
/// VMA_LOG_DETAILS
/// <para />
/// Default: 0
/// For VMA_TRACELEVEL >= 4, this value defaults to 2.
/// </summary>
public enum VmaLogDetails
{
    /// <summary>
    /// Basic log line
    /// </summary>
    Basic = 0,

    /// <summary>
    /// With ThreadId
    /// </summary>
    WithThreadId = 1,

    /// <summary>
    ///  With ProcessId and ThreadId
    /// </summary>
    WithProcessIdAndThreadId = 2,

    /// <summary>
    /// With Time, ProcessId, and ThreadId (Time is the amount of milliseconds from the start of the process)
    /// </summary>
    WithTimeProcessIdAndThreadId = 3
}
