using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Balanced_Gaming.ViewModels
{
    public partial class MoodAssessmentViewModel : ObservableObject 
    {
        public const string OtherOption = "Other (specify below)";
        
        public MoodAssessmentViewModel(bool isPreGame) 
        {
            IsPreGame = isPreGame;
        }
        public bool IsPreGame { get; set; }
        public string Title => IsPreGame
            ? "How are you feeling?"
            : "How was your session?";
        public string Subtitle => IsPreGame
            ? "Please answer these questions before starting your session"
            : "Please answer these questions about your gaming session";
        public IReadOnlyList<string> MotivationOptions { get; } = new[]
        {
             "Relaxation",
            "Fun and Entertainment",
            "Social Connection",
            "Escape/Distraction",
            "Boredom",
            "Challenge/Achievement",
            OtherOption,
        };

        [ObservableProperty] private int _moodValue = 3;
        [ObservableProperty] private int _stressValue = 3;
        [ObservableProperty] private int _energyValue = 3;
        [ObservableProperty] private int _focusValue = 3;
        [ObservableProperty] private int _satisfactionValue = 3;
        [ObservableProperty] private int _impactValue = 3;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsOtherMotivationVisible))]
        private string _selectedMotivation = "Relaxation";

        [ObservableProperty] private string _otherMotivation = string.Empty;

        public bool IsOtherMotivationVisible => SelectedMotivation == OtherOption;

        public string MotivationValue => IsOtherMotivationVisible && !string.IsNullOrWhiteSpace(OtherMotivation)
            ? OtherMotivation.Trim()
            : SelectedMotivation;

        public bool DialogResult {  get; private set; }

        public event EventHandler? CloseRequested;

        [RelayCommand]
        private void Submit()
        {
            DialogResult = true;
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }
        [RelayCommand]
        private void Skip()
        {
            DialogResult = false;
            CloseRequested?.Invoke(this, EventArgs.Empty);

        }
    }
}
