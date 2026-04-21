namespace SpeedAlert.Application.Models;

public class SpeedLimitResult
{
    public double SpeedLimitKph { get; set; }
    public string RoadName { get; set; } = "Unknown";
    public string Source { get; set; } = "Unknown";
    public double Confidence { get; set; } = 1.0;
}
