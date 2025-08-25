using RetirePlanner;
using System.Windows;

namespace RetirePlanner
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = new RetirementPlannerViewModel(); // 한 줄로 끝
        }
    }
}
