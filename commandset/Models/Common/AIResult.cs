namespace RevitMCPCommandSet.Models.Common;

public class AIResult<T>
{
    /// <summary>
    ///     是否成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    ///     消息
    /// </summary>
    public string Message { get; set; }

    /// <summary>
    ///     返回数据
    /// </summary>
    public T Response { get; set; }
}