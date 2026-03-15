namespace YieldverdictApi.Models.Requests;

public class AnalyseRequest
{
    public required string Postcode { get; set; }
    public ScenarioInput Scenario { get; set; } = new();
}

public class ScenarioInput
{
    public decimal? PriceOverride { get; set; }
    public int Ltv { get; set; } = 75;
    public decimal InterestRate { get; set; } = 4.75m;
    public bool IsAdditionalProperty { get; set; } = true;
    public string TaxBand { get; set; } = "higher";
    public int ManagementPct { get; set; } = 10;
    public int VoidWeeks { get; set; } = 3;
    public decimal RefurbBudget { get; set; } = 5000;
    public decimal GrowthAssumption { get; set; } = 3.0m;
    public string MortgageType { get; set; } = "repayment";
    public int HoldYears { get; set; } = 5;
}
