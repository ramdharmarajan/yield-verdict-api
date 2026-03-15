using YieldverdictApi.Models.Domain;
using YieldverdictApi.Models.Requests;

namespace YieldverdictApi.Models.Responses;

public class AnalysisResponse
{
    public string Postcode { get; set; } = string.Empty;
    public string Area { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;

    public decimal AvgPrice { get; set; }
    public decimal PriceGrowth1y { get; set; }
    public decimal PriceGrowth5y { get; set; }
    public decimal PriceGrowth10y { get; set; }

    public decimal AvgRent { get; set; }
    public decimal GrossYield { get; set; }
    public decimal NetYield { get; set; }
    public decimal CashOnCash { get; set; }

    public decimal MonthlyCashflow { get; set; }
    public decimal AnnualCashflow { get; set; }

    public decimal StampDuty { get; set; }
    public decimal Deposit { get; set; }
    public decimal TotalUpfront { get; set; }

    public decimal MonthlyMortgage { get; set; }
    public decimal AnnualMortgage { get; set; }

    public decimal Section24Tax { get; set; }
    public decimal PostTaxCashflow { get; set; }

    public int CrimeIndex { get; set; }
    public string FloodRisk { get; set; } = string.Empty;

    public List<RateStressTest> RateStressTests { get; set; } = new();
    public List<VoidStressTest> VoidStressTests { get; set; } = new();
    public List<RentStressTest> RentStressTests { get; set; } = new();
    public List<ExitCalc> ExitCalcs { get; set; } = new();

    public string Grade { get; set; } = string.Empty;
    public ScenarioInput Scenario { get; set; } = new();
}
