namespace YieldverdictApi.Services;

public interface IPoliceService
{
    Task<int> GetCrimeIndexAsync(decimal lat, decimal lng);
}
