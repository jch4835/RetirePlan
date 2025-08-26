using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RetirePlanner
{
    public enum MainType
    {
        상속취득,     // 토지 / 토지이외
        증여등무상취득, // 단일(세부 구분 없음) - 표의 3.5%
        원시취득,     // 단일(농특 0.2 포함)
        매매교환취득  // 토지 / 토지이외
    }

    public enum SubType
    {
        (없음),
        토지,
        토지이외
    }

    public class AcquisitionItem : INotifyPropertyChanged
    {
        private MainType _mainType;
        public MainType MainType
        {
            get => _mainType; 
            set { _mainType = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsSubTypeEnabled)); Recalc(); }
        }

        private SubType _subType = SubType.(없음);
        public SubType SubType
        {
            get => _subType;
            set { _subType = value; OnPropertyChanged(); Recalc(); }
        }

        private decimal _taxBase;
        public decimal TaxBase
        {
            get => _taxBase;
            set { _taxBase = value; OnPropertyChanged(); Recalc(); }
        }

        // 세율(%) 표시용
        private decimal _rateAcq, _rateRural, _rateEdu;
        public decimal RateAcq { get => _rateAcq; private set { _rateAcq = value; OnPropertyChanged(); } }
        public decimal RateRural { get => _rateRural; private set { _rateRural = value; OnPropertyChanged(); } }
        public decimal RateEdu  { get => _rateEdu; private set { _rateEdu  = value; OnPropertyChanged(); } }

        // 금액(원)
        private decimal _taxAcq, _taxRural, _taxEdu, _taxTotal;
        public decimal TaxAcq  { get => _taxAcq;  private set { _taxAcq  = value; OnPropertyChanged(); } }
        public decimal TaxRural{ get => _taxRural;private set { _taxRural= value; OnPropertyChanged(); } }
        public decimal TaxEdu  { get => _taxEdu;  private set { _taxEdu  = value; OnPropertyChanged(); } }
        public decimal TaxTotal{ get => _taxTotal;private set { _taxTotal= value; OnPropertyChanged(); } }

        // View 바인딩용: 세부항목 활성화 여부
        public bool IsSubTypeEnabled => MainType == MainType.상속취득 || MainType == MainType.매매교환취득;

        // 특례 플래그(뷰모델에서 내려줌)
        public bool Gift12RuleEnabled { get; set; } = false;
        // 특례와 '주택' 여부·금액 조건은 실제 서비스에서 분리 입력을 권장(v1은 간단화)

        public void Recalc()
        {
            // 기본 세율 설정(표 기준)
            (decimal acq, decimal rural, decimal edu) rates = MainType switch
            {
                MainType.상속취득 => SubType == SubType.토지
                    ? (2.3m, 0.0m, 0.06m)
                    : (2.8m, 0.0m, 0.16m),

                MainType.증여등무상취득 => (3.5m, 0.0m, 0.30m),

                MainType.원시취득 => (2.8m, 0.2m, 0.16m),

                MainType.매매교환취득 => SubType == SubType.토지
                    ? (3.0m, 0.0m, 0.20m)
                    : (4.0m, 0.0m, 0.40m),

                _ => (0, 0, 0)
            };

            // (선택) 조정대상지역 3억↑ 주택 증여 12% 특례(간단 적용)
            // 실제 제도는 '취득세' 12% 규정으로 해석되나, v1에서는 총세율로 강제하는 옵션이 아닌,
            // '취득세율'만 12%로 override (교육세 등 별도 규정은 미적용)
            if (Gift12RuleEnabled && MainType == MainType.증여등무상취득 && TaxBase >= 300_000_000m)
            {
                rates.acq = 12.0m;
                // 필요시 edu/rural 처리 규칙을 추가 가능
            }

            RateAcq  = rates.acq;
            RateRural= rates.rural;
            RateEdu  = rates.edu;

            // 금액 계산
            TaxAcq   = Round(TaxBase * RateAcq  / 100m);
            TaxRural = Round(TaxBase * RateRural/ 100m);
            TaxEdu   = Round(TaxBase * RateEdu  / 100m);
            TaxTotal = TaxAcq + TaxRural + TaxEdu;
        }

        private static decimal Round(decimal v) => decimal.Round(v, 0);

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name=null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    // 콤보박스 바인딩 소스
    public class ComboSource
    {
        public ObservableCollection<MainType> MainTypes { get; } =
            new ObservableCollection<MainType>((MainType[])System.Enum.GetValues(typeof(MainType)));
        public ObservableCollection<SubType> SubTypes { get; } =
            new ObservableCollection<SubType>((SubType[])System.Enum.GetValues(typeof(SubType)));
    }
}
