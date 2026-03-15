namespace YieldverdictApi.Models.Responses;

public class PostcodeResult
{
    public bool IsValid { get; set; }
    public string Postcode { get; set; } = string.Empty;
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public string District { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string AdminDistrict { get; set; } = string.Empty;
}
