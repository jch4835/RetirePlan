using System.Collections.ObjectModel;

namespace RetirePlanner.Models
{
    public class PublicPension
    {
        public string Name { get; set; } = "국민연금";
        public double AnnualAmount { get; set; } = 0;  // 은퇴시점 연간 금액(원)
        public string Note { get; set; } = "";
    }

    public class RetirementAsset
    {
        public string Name { get; set; } = "연금저축/IRP";
        public double CurrentBalance { get; set; } = 0;       // 원
        public double AnnualContribution { get; set; } = 0;    // 원
        public double AfterTaxReturnPct { get; set; } = 4.0;   // %
        public string Note { get; set; } = "";
    }

    public class Assumptions
    {
        public int CurrentAge { get; set; } = 53;
        public int RetirementAge { get; set; } = 57;
        public int RetirementYears { get; set; } = 20;

        public double InflationRatePct { get; set; } = 3.0;       // %
        public double AccumReturnRatePct { get; set; } = 3.0;      // %
        public double RetirementReturnRatePct { get; set; } = 3.0; // %

        public double TargetIncomeToday { get; set; } = 50_000_000; // 연간, 오늘가치

        public ObservableCollection<PublicPension> PublicPensions { get; private set; }
        public ObservableCollection<RetirementAsset> RetirementAssets { get; private set; }

        public Assumptions()
        {
            PublicPensions = new ObservableCollection<PublicPension>();
            RetirementAssets = new ObservableCollection<RetirementAsset>();
        }
    }

    public class CalculationResult
    {
        public int YearsToRetirement { get; set; }

        public double TargetIncomeAtRetirement { get; set; }
        public double TotalPublicPensionAtRet { get; set; }
        public double AnnualIncomeGap { get; set; }

        public double TotalLumpSumNeeded { get; set; }
        public double AssetsFutureValue { get; set; }
        public double AdditionalLumpSumNeeded { get; set; }

        public double RequiredAnnualSaving { get; set; }
        public double RequiredMonthlySaving => RequiredAnnualSaving / 12.0;

        // 간단 제안용
        public double EquityWeight { get; set; }
        public double BondWeight { get; set; }
        public string EquityWeightDisplay => $"{EquityWeight:P0}";
        public string BondWeightDisplay => $"{BondWeight:P0}";
    }
}
