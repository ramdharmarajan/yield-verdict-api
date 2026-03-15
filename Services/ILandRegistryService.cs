namespace YieldverdictApi.Services;

public class PriceData
{
    public decimal AvgPrice { get; set; }
    public decimal PriceGrowth1y { get; set; }
    public decimal PriceGrowth5y { get; set; }
    public decimal PriceGrowth10y { get; set; }
}

public interface ILandRegistryService
{
    Task<PriceData> GetPriceDataAsync(string postcode);
}
