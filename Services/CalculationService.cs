using YieldverdictApi.Models.Domain;

namespace YieldverdictApi.Services;

public class CalculationService
{
    public decimal CalcStampDuty(decimal price, bool isAdditional)
    {
        if (isAdditional)
        {
            decimal duty = 0;
            duty += Math.Min(price, 250000m) * 0.03m;
            if (price > 250000m)
                duty += Math.Min(price - 250000m, 675000m) * 0.08m;
            if (price > 925000m)
                duty += Math.Min(price - 925000m, 575000m) * 0.13m;
            if (price > 1500000m)
                duty += (price - 1500000m) * 0.15m;
            return Math.Round(duty, 2);
        }
        else
        {
            decimal duty = 0;
            if (price > 250000m)
                duty += Math.Min(price - 250000m, 675000m) * 0.05m;
            if (price > 925000m)
                duty += Math.Min(price - 925000m, 575000m) * 0.10m;
            if (price > 1500000m)
                duty += (price - 1500000m) * 0.12m;
            return Math.Round(duty, 2);
        }
    }

    public decimal CalcMonthlyRepayment(decimal principal, decimal annualRate, int years)
    {
        if (annualRate == 0) return principal / (years * 12);
        var r = (double)(annualRate / 100m / 12m);
        var n = years * 12;
        var payment = (double)principal * (r * Math.Pow(1 + r, n)) / (Math.Pow(1 + r, n) - 1);
        return Math.Round((decimal)payment, 2);
    }

    public decimal CalcInterestOnly(decimal principal, decimal annualRate)
    {
        return Math.Round(principal * (annualRate / 100m) / 12m, 2);
    }

    public decimal CalcIncomeTax(decimal profit, string taxBand)
    {
        const decimal personalAllowance = 12570m;
        const decimal basicLimit = 50270m;

        return taxBand.ToLower() switch
        {
            "basic" => Math.Max(0, Math.Min(profit, basicLimit) - personalAllowance) * 0.20m,
            "higher" => Math.Max(0, profit - basicLimit) * 0.40m,
            "additional" => Math.Max(0, profit - 125140m) * 0.45m,
            _ => 0
        };
    }

    public decimal CalcNI(decimal profit)
    {
        const decimal class2 = 179m;
        const decimal lower = 12570m;
        const decimal upper = 50270m;

        if (profit <= lower) return 0;
        var class4 = Math.Min(profit, upper) - lower;
        var class4High = Math.Max(0, profit - upper);
        return class2 + class4 * 0.09m + class4High * 0.02m;
    }

    public decimal CalcSection24Tax(decimal annualRent, decimal annualInterest, decimal otherCosts, string taxBand)
    {
        var taxableIncome = annualRent - otherCosts;
        var rate = taxBand.ToLower() switch
        {
            "higher" => 0.40m,
            "additional" => 0.45m,
            _ => 0.20m
        };
        var taxOnIncome = taxableIncome * rate;
        var interestCredit = annualInterest * 0.20m;
        return Math.Max(0, taxOnIncome - interestCredit);
    }

    public decimal CalcCGT(decimal gain, string taxBand)
    {
        const decimal annualExemption = 3000m;
        var taxableGain = Math.Max(0, gain - annualExemption);
        var rate = taxBand.ToLower() == "basic" ? 0.18m : 0.24m;
        return Math.Round(taxableGain * rate, 2);
    }

    public string CalcGrade(decimal grossYield, decimal priceGrowth5y, int transport,
        int crimeIndex, decimal monthlyCashflow, bool hasRegeneration, bool hasArticle4)
    {
        int score = 0;

        score += grossYield > 6.5m ? 22 : grossYield > 5m ? 16 : grossYield > 4m ? 10 : 4;
        score += priceGrowth5y > 30m ? 18 : priceGrowth5y > 20m ? 13 : priceGrowth5y > 10m ? 8 : 3;
        score += transport >= 9 ? 12 : transport >= 7 ? 8 : 4;
        score += crimeIndex < 30 ? 12 : crimeIndex < 50 ? 8 : 3;
        score += monthlyCashflow > 400m ? 14 : monthlyCashflow > 0m ? 9 : monthlyCashflow > -200m ? 3 : 0;
        if (hasRegeneration) score += 9;
        if (!hasArticle4) score += 7;

        return score >= 84 ? "A" : score >= 70 ? "B" : score >= 55 ? "C" : score >= 40 ? "D" : "E";
    }

    public List<RateStressTest> CalcRateStress(decimal principal, decimal baseRate,
        decimal monthlyRent, decimal otherMonthlyCosts, string mortgageType)
    {
        var results = new List<RateStressTest>();
        foreach (var bump in new[] { 0m, 1m, 2m, 3m })
        {
            var rate = baseRate + bump;
            var mortgage = mortgageType == "interest-only"
                ? CalcInterestOnly(principal, rate)
                : CalcMonthlyRepayment(principal, rate, 25);
            results.Add(new RateStressTest
            {
                Rate = rate,
                MonthlyMortgage = mortgage,
                MonthlyCashflow = monthlyRent - mortgage - otherMonthlyCosts
            });
        }
        return results;
    }

    public List<VoidStressTest> CalcVoidStress(decimal grossRentPcm, decimal monthlyMortgage, decimal otherMonthlyCosts)
    {
        var results = new List<VoidStressTest>();
        foreach (var voidWeeks in new[] { 0, 3, 6, 8, 12 })
        {
            var effectiveRent = grossRentPcm * (52 - voidWeeks) / 52m;
            results.Add(new VoidStressTest
            {
                VoidWeeks = voidWeeks,
                EffectiveRent = Math.Round(effectiveRent, 2),
                MonthlyCashflow = Math.Round(effectiveRent - monthlyMortgage - otherMonthlyCosts, 2)
            });
        }
        return results;
    }

    public List<RentStressTest> CalcRentStress(decimal grossRentPcm, decimal monthlyMortgage, decimal otherMonthlyCosts)
    {
        var results = new List<RentStressTest>();
        foreach (var change in new[] { -20, -10, 0, 10, 20 })
        {
            var newRent = grossRentPcm * (1 + change / 100m);
            results.Add(new RentStressTest
            {
                ChangePercent = change,
                NewRent = Math.Round(newRent, 2),
                MonthlyCashflow = Math.Round(newRent - monthlyMortgage - otherMonthlyCosts, 2)
            });
        }
        return results;
    }

    public List<ExitCalc> CalcExitStrategy(decimal price, decimal loan, decimal deposit,
        decimal totalInvested, decimal annualCashflow, decimal growthAssumption, string taxBand)
    {
        var results = new List<ExitCalc>();
        foreach (var years in new[] { 3, 5, 7, 10 })
        {
            var futurePrice = price * (decimal)Math.Pow(1 + (double)(growthAssumption / 100m), years);
            var gain = futurePrice - price;
            var cgt = CalcCGT(gain, taxBand);
            var agentFees = futurePrice * 0.015m;
            var netProceeds = futurePrice - loan - cgt - agentFees;
            var totalRentIncome = annualCashflow * years;
            var totalReturn = netProceeds - deposit + totalRentIncome;
            var roi = totalInvested > 0 ? totalReturn / totalInvested * 100m : 0m;

            results.Add(new ExitCalc
            {
                Years = years,
                FuturePrice = Math.Round(futurePrice, 0),
                Gain = Math.Round(gain, 0),
                Cgt = Math.Round(cgt, 0),
                AgentFees = Math.Round(agentFees, 0),
                NetProceeds = Math.Round(netProceeds, 0),
                TotalRentIncome = Math.Round(totalRentIncome, 0),
                TotalReturn = Math.Round(totalReturn, 0),
                Roi = Math.Round(roi, 1),
            });
        }
        return results;
    }
}
