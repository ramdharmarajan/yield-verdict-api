namespace YieldverdictApi.Services;

public interface IFloodRiskService
{
    Task<string> GetFloodRiskAsync(decimal lat, decimal lng);
}
