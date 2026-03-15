namespace YieldverdictApi.Models.Domain;

public class ExitCalc
{
    public int Years { get; set; }
    public decimal FuturePrice { get; set; }
    public decimal Gain { get; set; }
    public decimal Cgt { get; set; }
    public decimal AgentFees { get; set; }
    public decimal NetProceeds { get; set; }
    public decimal TotalRentIncome { get; set; }
    public decimal TotalReturn { get; set; }
    public decimal Roi { get; set; }
}
