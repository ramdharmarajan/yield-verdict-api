using YieldverdictApi.Models.Responses;

namespace YieldverdictApi.Services;

public interface IInsightsService
{
    Task<List<InsightResponse>> GenerateInsightsAsync(AnalysisResponse analysis);
}
