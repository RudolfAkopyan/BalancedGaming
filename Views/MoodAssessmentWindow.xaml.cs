using Balanced_Gaming.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace Balanced_Gaming.Views
{
    public partial class MoodAssessmentWindow : Window
    {
        public MoodAssessmentViewModel ViewModel { get; }
        public MoodAssessmentWindow(bool isPreGame)
        {
            InitializeComponent();
            ViewModel = new MoodAssessmentViewModel(isPreGame);
            ViewModel.CloseRequested += (_, _) => DialogResult = ViewModel.DialogResult;
            DataContext = ViewModel;
        }
    }
}
