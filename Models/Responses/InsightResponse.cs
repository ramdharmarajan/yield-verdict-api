namespace YieldverdictApi.Models.Responses;

public class InsightResponse
{
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Insight { get; set; } = string.Empty;
    public string Confidence { get; set; } = string.Empty;
}
