using System;

namespace RetirePlanner.Services
{
    public static class CalculationService
    {
        private static double PctToRate(double pct) => pct / 100.0;

        public static int YearsToRetirement(int currentAge, int retireAge)
            => Math.Max(0, retireAge - currentAge);

        public static double AdjustToFuture(double amountToday, double inflationPct, int years)
        {
            var π = PctToRate(inflationPct);
            return amountToday * Math.Pow(1 + π, years);
        }

        public static double PresentValueOfAnnuity(double payment, double realRate, int years)
        {
            if (years <= 0) return 0;
            if (Math.Abs(realRate) < 1e-9) return payment * years;
            return payment * (1 - Math.Pow(1 + realRate, -years)) / realRate;
        }

        public static double FutureValue(double present, double rate, int years)
            => present * Math.Pow(1 + rate, years);

        public static double FutureValueSeries(double paymentPerYear, double rate, int years)
        {
            if (years <= 0) return 0;
            if (Math.Abs(rate) < 1e-9) return paymentPerYear * years;
            return paymentPerYear * ((Math.Pow(1 + rate, years) - 1) / rate);
        }

        public static double SumPublicPension(double[] annuals)
        {
            if (annuals != null && annuals.Length > 0)
                return Math.Max(0, Sum(annuals));
            return 0;
        }
        private static double Sum(double[] arr) { double s = 0; foreach (var v in arr) s += v; return s; }

        public static (double fvSum, double[] each) FutureValueOfAssets((double current, double contrib, double pct)[] assets, int years)
        {
            var each = new double[assets.Length];
            double sum = 0;
            for (int i = 0; i < assets.Length; i++)
            {
                double r = PctToRate(assets[i].pct);
                var fv = FutureValue(assets[i].current, r, years) + FutureValueSeries(assets[i].contrib, r, years);
                each[i] = fv;
                sum += fv;
            }
            return (sum, each);
        }

        public static double RequiredAnnualSaving(double additionalLump, double accumPct, int years)
        {
            if (additionalLump <= 0 || years <= 0) return 0;
            var r = PctToRate(accumPct);
            if (Math.Abs(r) < 1e-9) return additionalLump / years;
            return additionalLump / ((Math.Pow(1 + r, years) - 1) / r);
        }

        public static (double equityW, double bondW) SimpleGlidePath(int yearsToRet)
        {
            if (yearsToRet >= 20) return (0.60, 0.40);
            if (yearsToRet >= 10) return (0.50, 0.50);
            return (0.35, 0.65);
        }

        public static double RealRate(double retirePct, double inflationPct)
        {
            var rp = PctToRate(retirePct);
            var π = PctToRate(inflationPct);
            return ((1 + rp) / (1 + π)) - 1;
        }
    }
}
