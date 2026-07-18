namespace RevitMCPCommandSet.Models.Common;

public class AIResult<T>
{
    /// <summary>
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// </summary>
    public string Message { get; set; }

    /// <summary>
    /// </summary>
    public T Response { get; set; }
}