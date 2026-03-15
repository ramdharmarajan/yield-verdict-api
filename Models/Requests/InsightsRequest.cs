using YieldverdictApi.Models.Responses;

namespace YieldverdictApi.Models.Requests;

public class InsightsRequest
{
    public required AnalysisResponse Analysis { get; set; }
}
