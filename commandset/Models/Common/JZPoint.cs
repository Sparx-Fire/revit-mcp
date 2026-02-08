using Newtonsoft.Json;

namespace RevitMCPCommandSet.Models.Common;

/// <summary>
///     三维点
/// </summary>
public class JZPoint
{
    /// <summary>
    ///     构造函数
    /// </summary>
    public JZPoint()
    {
    }

    /// <summary>
    ///     构造函数
    /// </summary>
    public JZPoint(double x, double y, double z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    /// <summary>
    ///     构造函数
    /// </summary>
    public JZPoint(double x, double y)
    {
        X = x;
        Y = y;
        Z = 0;
    }

    [JsonProperty("x")] public double X { get; set; }

    [JsonProperty("y")] public double Y { get; set; }

    [JsonProperty("z")] public double Z { get; set; }

    /// <summary>
    ///     转换为Revit的XYZ点
    ///     单位转换：mm -> ft
    /// </summary>
    public static XYZ ToXYZ(JZPoint jzPoint)
    {
        return new XYZ(jzPoint.X / 304.8, jzPoint.Y / 304.8, jzPoint.Z / 304.8);
    }
}