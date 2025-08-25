using RetirePlanner.Models;
using RetirePlanner.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace RetirePlanner
{
    public class RetirementPlannerViewModel : INotifyPropertyChanged
    {
        // 탭 전환
        private int _selectedTabIndex = 0;
        public int SelectedTabIndex { get => _selectedTabIndex; set { _selectedTabIndex = value; OnPropertyChanged(); } }

        // 입력(Assumptions)
        public int CurrentAge { get => _assumptions.CurrentAge; set { _assumptions.CurrentAge = value; OnPropertyChanged(); } }
        public int RetirementAge { get => _assumptions.RetirementAge; set { _assumptions.RetirementAge = value; OnPropertyChanged(); } }
        public int RetirementYears { get => _assumptions.RetirementYears; set { _assumptions.RetirementYears = value; OnPropertyChanged(); } }

        public double InflationRatePct { get => _assumptions.InflationRatePct; set { _assumptions.InflationRatePct = value; OnPropertyChanged(); } }
        public double AccumReturnRatePct { get => _assumptions.AccumReturnRatePct; set { _assumptions.AccumReturnRatePct = value; OnPropertyChanged(); } }
        public double RetirementReturnRatePct { get => _assumptions.RetirementReturnRatePct; set { _assumptions.RetirementReturnRatePct = value; OnPropertyChanged(); } }

        public double TargetIncomeToday { get => _assumptions.TargetIncomeToday; set { _assumptions.TargetIncomeToday = value; OnPropertyChanged(); } }

        public ObservableCollection<PublicPension> PublicPensions => _assumptions.PublicPensions;
        public ObservableCollection<RetirementAsset> RetirementAssets => _assumptions.RetirementAssets;

        private PublicPension _selectedPension;
        public PublicPension SelectedPension { get => _selectedPension; set { _selectedPension = value; OnPropertyChanged(); } }

        private RetirementAsset _selectedAsset;
        public RetirementAsset SelectedAsset { get => _selectedAsset; set { _selectedAsset = value; OnPropertyChanged(); } }

        // 기존 코드: private CalculationResult _result = new();
        // 수정 코드:
        private CalculationResult _result = new CalculationResult();
        public CalculationResult Result { get => _result; set { _result = value; OnPropertyChanged(); } }

        // Commands
        public ICommand AddPensionCommand { get; }
        public ICommand RemovePensionCommand { get; }
        public ICommand AddAssetCommand { get; }
        public ICommand RemoveAssetCommand { get; }
        public ICommand CalculateCommand { get; }
        public ICommand GoToInputCommand { get; }

        // 기존 코드:
        // private readonly Assumptions _assumptions = new();

        // 수정 코드:
        private readonly Assumptions _assumptions = new Assumptions();

        public RetirementPlannerViewModel()
        {
            // 샘플 행 1개
            _assumptions.PublicPensions.Add(new PublicPension { Name = "국민연금(예상)", AnnualAmount = 25_000_000 });
            _assumptions.RetirementAssets.Add(new RetirementAsset { Name = "세제지원개인연금", CurrentBalance = 200_000_000, AnnualContribution = 3_600_000, AfterTaxReturnPct = 3.0 });
            _assumptions.RetirementAssets.Add(new RetirementAsset { Name = "퇴직연금", CurrentBalance = 200_000_000, AnnualContribution = 3_600_000, AfterTaxReturnPct = 3.0 });
            _assumptions.RetirementAssets.Add(new RetirementAsset { Name = "MG연금저축", CurrentBalance = 30_000_000, AnnualContribution = 6_000_000, AfterTaxReturnPct = 3.0 });
            _assumptions.RetirementAssets.Add(new RetirementAsset { Name = "우리은행IRP", CurrentBalance = 15_000_000, AnnualContribution = 3_000_000, AfterTaxReturnPct = 3.0 });

            AddPensionCommand = new RelayCommand(_ => PublicPensions.Add(new PublicPension()));
            RemovePensionCommand = new RelayCommand(_ => { if (SelectedPension != null) PublicPensions.Remove(SelectedPension); });
            AddAssetCommand = new RelayCommand(_ => RetirementAssets.Add(new RetirementAsset()));
            RemoveAssetCommand = new RelayCommand(_ => { if (SelectedAsset != null) RetirementAssets.Remove(SelectedAsset); });

            CalculateCommand = new RelayCommand(_ => Calculate());
            GoToInputCommand = new RelayCommand(_ => SelectedTabIndex = 0);
        }

        private void Calculate()
        {
            int yearsToRet = CalculationService.YearsToRetirement(CurrentAge, RetirementAge);

            // 1) 목표은퇴소득(은퇴시점가치)
            double targetAtRet = CalculationService.AdjustToFuture(TargetIncomeToday, InflationRatePct, yearsToRet);

            // 2) 공적연금 합계(은퇴시점 연간액)
            double pensionSum = CalculationService.SumPublicPension(System.Linq.Enumerable.Select(PublicPensions, p => p.AnnualAmount).ToArray());

            // 3) 연간 부족액
            double gap = targetAtRet - pensionSum;
            if (gap < 0) gap = 0;

            // 4) 총은퇴일시금(은퇴시점 현가) - 실질수익률
            double rReal = CalculationService.RealRate(RetirementReturnRatePct, InflationRatePct);
            double totalLump = CalculationService.PresentValueOfAnnuity(gap, rReal, RetirementYears);

            // 5) 은퇴자산 순미래가치 평가(은퇴시점)
            var assetTuples = System.Linq.Enumerable.Select(RetirementAssets, a => (a.CurrentBalance, a.AnnualContribution, a.AfterTaxReturnPct)).ToArray();
            var (fvSum, _) = CalculationService.FutureValueOfAssets(assetTuples, yearsToRet);

            // 6) 추가 필요 은퇴일시금
            double addNeed = totalLump - fvSum;
            if (addNeed < 0) addNeed = 0;

            // 7) 연간 저축액(역산, 적립기 수익률)
            double annualSave = CalculationService.RequiredAnnualSaving(addNeed, AccumReturnRatePct, yearsToRet);

            // 8) 간단 자산배분 제안(샘플)
            var (eqW, bdW) = CalculationService.SimpleGlidePath(yearsToRet);

            Result = new CalculationResult
            {
                YearsToRetirement = yearsToRet,
                TargetIncomeAtRetirement = Round0(targetAtRet),
                TotalPublicPensionAtRet = Round0(pensionSum),
                AnnualIncomeGap = Round0(gap),
                TotalLumpSumNeeded = Round0(totalLump),
                AssetsFutureValue = Round0(fvSum),
                AdditionalLumpSumNeeded = Round0(addNeed),
                RequiredAnnualSaving = Round0(annualSave),
                EquityWeight = eqW,
                BondWeight = bdW
            };

            SelectedTabIndex = 1; // 결과 탭으로 이동
        }

        private static double Round0(double v) => System.Math.Round(v, 0);

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class RelayCommand : ICommand
    {
        private readonly System.Action<object> _execute;
        private readonly System.Predicate<object> _canExecute;
        public RelayCommand(System.Action<object> execute, System.Predicate<object> canExecute = null)
        { _execute = execute; _canExecute = canExecute; }
        public bool CanExecute(object parameter) => _canExecute == null || _canExecute(parameter);
        public void Execute(object parameter) => _execute(parameter);
        public event System.EventHandler CanExecuteChanged
        { add { CommandManager.RequerySuggested += value; } remove { CommandManager.RequerySuggested -= value; } }
    }
}
