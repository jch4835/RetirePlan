using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace RetirePlanner
{
    public class AcquisitionTaxViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<AcquisitionItem> Items { get; } = new();

        private AcquisitionItem _selectedItem;
        public AcquisitionItem SelectedItem
        {
            get => _selectedItem;
            set { _selectedItem = value; OnPropertyChanged(); }
        }

        // 특례 플래그(뷰에서 체크박스)
        private bool _enableGift12Rule;
        public bool EnableGift12Rule
        {
            get => _enableGift12Rule;
            set
            {
                _enableGift12Rule = value;
                foreach (var it in Items) { it.Gift12RuleEnabled = value; it.Recalc(); }
                OnPropertyChanged();
                RecalcSums();
            }
        }

        // 합계 바인딩
        private decimal _sumAcq, _sumRural, _sumEdu, _sumTotal;
        public decimal SumAcq   { get => _sumAcq;   private set { _sumAcq = value;   OnPropertyChanged(); } }
        public decimal SumRural { get => _sumRural; private set { _sumRural = value; OnPropertyChanged(); } }
        public decimal SumEdu   { get => _sumEdu;   private set { _sumEdu = value;   OnPropertyChanged(); } }
        public decimal SumTotal { get => _sumTotal; private set { _sumTotal = value; OnPropertyChanged(); } }

        public ICommand AddRowCommand { get; }
        public ICommand RemoveRowCommand { get; }

        public AcquisitionTaxViewModel()
        {
            AddRowCommand = new RelayCommand(_ => AddRow());
            RemoveRowCommand = new RelayCommand(_ => { if (SelectedItem != null) { Items.Remove(SelectedItem); RecalcSums(); } });

            // 샘플 한 줄
            AddRow(MainType.매매교환취득, SubType.토지, 100_000_000m);
        }

        private void AddRow() => AddRow(MainType.상속취득, SubType.토지, 0m);

        private void AddRow(MainType m, SubType s, decimal baseAmt)
        {
            var item = new AcquisitionItem { MainType = m, SubType = s, TaxBase = baseAmt, Gift12RuleEnabled = EnableGift12Rule };
            item.Recalc();
            item.PropertyChanged += (_, __) => RecalcSums();
            Items.Add(item);
            SelectedItem = item;
            RecalcSums();
        }

        private void RecalcSums()
        {
            SumAcq   = Items.Sum(i => i.TaxAcq);
            SumRural = Items.Sum(i => i.TaxRural);
            SumEdu   = Items.Sum(i => i.TaxEdu);
            SumTotal = Items.Sum(i => i.TaxTotal);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name=null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
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
