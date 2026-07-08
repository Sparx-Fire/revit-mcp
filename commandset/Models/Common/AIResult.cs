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

    /// <summary>
    ///     符合过滤条件的元素总数（分页前）
    /// </summary>
    public int? TotalCount { get; set; }

    /// <summary>
    ///     是否还有更多元素可加载
    /// </summary>
    public bool? HasMore { get; set; }

    /// <summary>
    ///     当前分页偏移
    /// </summary>
    public int? Offset { get; set; }

    /// <summary>
    ///     当前分页大小
    /// </summary>
    public int? Limit { get; set; }
}