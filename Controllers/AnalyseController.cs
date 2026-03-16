using Microsoft.AspNetCore.Mvc;
using YieldverdictApi.Data;
using YieldverdictApi.Models.Requests;
using YieldverdictApi.Models.Responses;
using YieldverdictApi.Services;

namespace YieldverdictApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AnalyseController : ControllerBase
{
    private readonly IPostcodeService _postcodeService;
    private readonly ILandRegistryService _landRegistryService;
    private readonly IPoliceService _policeService;
    private readonly IFloodRiskService _floodRiskService;
    private readonly CalculationService _calc;
    private readonly ILogger<AnalyseController> _logger;

    public AnalyseController(
        IPostcodeService postcodeService,
        ILandRegistryService landRegistryService,
        IPoliceService policeService,
        IFloodRiskService floodRiskService,
        CalculationService calc,
        ILogger<AnalyseController> logger)
    {
        _postcodeService = postcodeService;
        _landRegistryService = landRegistryService;
        _policeService = policeService;
        _floodRiskService = floodRiskService;
        _calc = calc;
        _logger = logger;
    }

    // GET /api/analyse/{postcode}?purchasePrice=250000&ltv=75&interestRate=4.75&...
    [HttpGet("{postcode}")]
    public async Task<IActionResult> AnalyseGet(
        string postcode,
        [FromQuery] decimal? purchasePrice,
        [FromQuery] int ltv = 75,
        [FromQuery] decimal interestRate = 5.0m,
        [FromQuery] bool isAdditionalProperty = true,
        [FromQuery] string taxBand = "higher",
        [FromQuery] int managementPct = 10,
        [FromQuery] int voidWeeks = 3,
        [FromQuery] decimal refurbBudget = 0,
        [FromQuery] decimal growthAssumption = 3.0m,
        [FromQuery] string mortgageType = "repayment",
        [FromQuery] int holdYears = 5)
    {
        var request = new AnalyseRequest
        {
            Postcode = postcode,
            Scenario = new ScenarioInput
            {
                PriceOverride = purchasePrice,
                Ltv = ltv,
                InterestRate = interestRate,
                IsAdditionalProperty = isAdditionalProperty,
                TaxBand = taxBand,
                ManagementPct = managementPct,
                VoidWeeks = voidWeeks,
                RefurbBudget = refurbBudget,
                GrowthAssumption = growthAssumption,
                MortgageType = mortgageType,
                HoldYears = holdYears
            }
        };
        return await Analyse(request);
    }

    [HttpPost]
    public async Task<IActionResult> Analyse([FromBody] AnalyseRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var postcodeResult = await _postcodeService.ValidateAsync(request.Postcode);
        if (postcodeResult == null || !postcodeResult.IsValid)
            return BadRequest(new { error = "Invalid postcode" });

        var s = request.Scenario;

        // Parallel external calls
        var priceTask = _landRegistryService.GetPriceDataAsync(postcodeResult.Postcode);
        var crimeTask = _policeService.GetCrimeIndexAsync(postcodeResult.Latitude, postcodeResult.Longitude);
        var floodTask = _floodRiskService.GetFloodRiskAsync(postcodeResult.Latitude, postcodeResult.Longitude);

        await Task.WhenAll(priceTask, crimeTask, floodTask);

        var priceData = priceTask.Result;
        var crimeIndex = crimeTask.Result;
        var floodRisk = floodTask.Result;

        var price = s.PriceOverride ?? priceData.AvgPrice;
        var avgRent = RentalData.GetRent(postcodeResult.Postcode);

        // Mortgage calcs
        var loan = price * s.Ltv / 100m;
        var deposit = price - loan;
        var stampDuty = _calc.CalcStampDuty(price, s.IsAdditionalProperty);
        var totalUpfront = deposit + stampDuty + s.RefurbBudget;

        var monthlyMortgage = s.MortgageType == "interest-only"
            ? _calc.CalcInterestOnly(loan, s.InterestRate)
            : _calc.CalcMonthlyRepayment(loan, s.InterestRate, 25);

        var annualMortgage = monthlyMortgage * 12;
        var annualInterest = s.MortgageType == "interest-only"
            ? annualMortgage
            : _calc.CalcInterestOnly(loan, s.InterestRate) * 12;

        // Rent calcs
        var voidAdjustedRent = avgRent * (52m - s.VoidWeeks) / 52m;
        var managementCost = avgRent * s.ManagementPct / 100m;
        var otherMonthlyCosts = managementCost;
        var otherAnnualCosts = otherMonthlyCosts * 12;

        var monthlyCashflow = voidAdjustedRent - monthlyMortgage - otherMonthlyCosts;
        var annualCashflow = monthlyCashflow * 12;

        // Yields
        var grossYield = price > 0 ? (avgRent * 12m / price) * 100m : 0;
        var netYield = price > 0 ? ((voidAdjustedRent - otherMonthlyCosts) * 12m / price) * 100m : 0;
        var cashOnCash = totalUpfront > 0 ? (annualCashflow / totalUpfront) * 100m : 0;

        // Section 24 tax
        var section24Tax = _calc.CalcSection24Tax(avgRent * 12m, annualInterest, otherAnnualCosts, s.TaxBand);
        var postTaxCashflow = annualCashflow - section24Tax;

        // Stress tests
        var rateStress = _calc.CalcRateStress(loan, s.InterestRate, voidAdjustedRent, otherMonthlyCosts, s.MortgageType);
        var voidStress = _calc.CalcVoidStress(avgRent, monthlyMortgage, otherMonthlyCosts);
        var rentStress = _calc.CalcRentStress(voidAdjustedRent, monthlyMortgage, otherMonthlyCosts);

        // Exit calcs
        var exitCalcs = _calc.CalcExitStrategy(price, loan, deposit, totalUpfront, annualCashflow,
            s.GrowthAssumption, s.TaxBand);

        // Grade (transport defaulted to 5, no regeneration/article4 data from free APIs)
        var grade = _calc.CalcGrade(grossYield, priceData.PriceGrowth5y, 5, crimeIndex, monthlyCashflow, false, false);

        var response = new AnalysisResponse
        {
            Postcode = postcodeResult.Postcode,
            Area = postcodeResult.AdminDistrict,
            Region = postcodeResult.Region,
            AvgPrice = Math.Round(price, 0),
            PriceGrowth1y = priceData.PriceGrowth1y,
            PriceGrowth5y = priceData.PriceGrowth5y,
            PriceGrowth10y = priceData.PriceGrowth10y,
            AvgRent = avgRent,
            GrossYield = Math.Round(grossYield, 2),
            NetYield = Math.Round(netYield, 2),
            CashOnCash = Math.Round(cashOnCash, 2),
            MonthlyCashflow = Math.Round(monthlyCashflow, 2),
            AnnualCashflow = Math.Round(annualCashflow, 2),
            StampDuty = stampDuty,
            Deposit = Math.Round(deposit, 2),
            TotalUpfront = Math.Round(totalUpfront, 2),
            MonthlyMortgage = monthlyMortgage,
            AnnualMortgage = Math.Round(annualMortgage, 2),
            Section24Tax = Math.Round(section24Tax, 2),
            PostTaxCashflow = Math.Round(postTaxCashflow, 2),
            CrimeIndex = crimeIndex,
            FloodRisk = floodRisk,
            RateStressTests = rateStress,
            VoidStressTests = voidStress,
            RentStressTests = rentStress,
            ExitCalcs = exitCalcs,
            Grade = grade,
            Scenario = s,
        };

        _logger.LogInformation("Analysis complete for {Postcode}: Grade={Grade}", postcodeResult.Postcode, grade);

        return Ok(response);
    }
}
