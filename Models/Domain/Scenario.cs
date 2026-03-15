namespace YieldverdictApi.Models.Domain;

public class RateStressTest
{
    public decimal Rate { get; set; }
    public decimal MonthlyMortgage { get; set; }
    public decimal MonthlyCashflow { get; set; }
}

public class VoidStressTest
{
    public int VoidWeeks { get; set; }
    public decimal EffectiveRent { get; set; }
    public decimal MonthlyCashflow { get; set; }
}

public class RentStressTest
{
    public int ChangePercent { get; set; }
    public decimal NewRent { get; set; }
    public decimal MonthlyCashflow { get; set; }
}
