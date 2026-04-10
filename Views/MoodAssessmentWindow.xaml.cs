using System.Windows;
using System.Windows.Controls;

namespace Balanced_Gaming.Views
{
    public partial class MoodAssessmentWindow : Window
    {
        public bool IsPreGame { get; set; }

        // Results
        public int MoodValue { get; private set; }
        public int StressValue { get; private set; }
        public int EnergyValue { get; private set; }
        public string MotivationValue { get; private set; }
        public int FocusValue { get; private set; }
        public int SatisfactionValue { get; private set; }
        public int ImpactValue { get; private set; }

        public MoodAssessmentWindow(bool isPreGame)
        {
            InitializeComponent();
            IsPreGame = isPreGame;
            ConfigureForType();
        }

        private void ConfigureForType()
        {
            if (IsPreGame)
            {
                titleText.Text = "How are you feeling?";
                subtitleText.Text = "Please answer these questions before starting your session";

                // Show pre-game questions
                motivationPanel.Visibility = Visibility.Visible;
                focusPanel.Visibility = Visibility.Visible;

                // Hide post-game questions
                satisfactionPanel.Visibility = Visibility.Collapsed;
                impactPanel.Visibility = Visibility.Collapsed;
            }
            else
            {
                titleText.Text = "How was your session?";
                subtitleText.Text = "Please answer these questions about your gaming session";

                // Hide pre-game questions
                motivationPanel.Visibility = Visibility.Collapsed;
                focusPanel.Visibility = Visibility.Collapsed;

                // Show post-game questions
                satisfactionPanel.Visibility = Visibility.Visible;
                impactPanel.Visibility = Visibility.Visible;
            }
        }

        private void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            // Get mood value
            if (mood1.IsChecked == true) MoodValue = 1;
            else if (mood2.IsChecked == true) MoodValue = 2;
            else if (mood3.IsChecked == true) MoodValue = 3;
            else if (mood4.IsChecked == true) MoodValue = 4;
            else if (mood5.IsChecked == true) MoodValue = 5;

            // Get stress value
            if (stress1.IsChecked == true) StressValue = 1;
            else if (stress2.IsChecked == true) StressValue = 2;
            else if (stress3.IsChecked == true) StressValue = 3;
            else if (stress4.IsChecked == true) StressValue = 4;
            else if (stress5.IsChecked == true) StressValue = 5;

            // Get energy value
            if (energy1.IsChecked == true) EnergyValue = 1;
            else if (energy2.IsChecked == true) EnergyValue = 2;
            else if (energy3.IsChecked == true) EnergyValue = 3;
            else if (energy4.IsChecked == true) EnergyValue = 4;
            else if (energy5.IsChecked == true) EnergyValue = 5;

            if (IsPreGame)
            {
                // Get motivation
                var selectedMotivation = ((ComboBoxItem)motivationComboBox.SelectedItem).Content.ToString();
                if (selectedMotivation.StartsWith("Other") && !string.IsNullOrWhiteSpace(otherMotivationTextBox.Text))
                {
                    MotivationValue = otherMotivationTextBox.Text.Trim();
                }
                else
                {
                    MotivationValue = selectedMotivation;
                }
                // Get focus value
                if (focus1.IsChecked == true) FocusValue = 1;
                else if (focus2.IsChecked == true) FocusValue = 2;
                else if (focus3.IsChecked == true) FocusValue = 3;
                else if (focus4.IsChecked == true) FocusValue = 4;
                else if (focus5.IsChecked == true) FocusValue = 5;
            }
            else
            {
                // Get satisfaction value
                if (satisfaction1.IsChecked == true) SatisfactionValue = 1;
                else if (satisfaction2.IsChecked == true) SatisfactionValue = 2;
                else if (satisfaction3.IsChecked == true) SatisfactionValue = 3;
                else if (satisfaction4.IsChecked == true) SatisfactionValue = 4;
                else if (satisfaction5.IsChecked == true) SatisfactionValue = 5;

                // Get impact value
                if (impact1.IsChecked == true) ImpactValue = 1;
                else if (impact2.IsChecked == true) ImpactValue = 2;
                else if (impact3.IsChecked == true) ImpactValue = 3;
                else if (impact4.IsChecked == true) ImpactValue = 4;
                else if (impact5.IsChecked == true) ImpactValue = 5;
            }

            DialogResult = true;
            this.Close();
        }
        private void MotivationComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Add null check - textbox might not be loaded yet
            if (motivationComboBox.SelectedItem != null && otherMotivationTextBox != null)
            {
                var selected = ((ComboBoxItem)motivationComboBox.SelectedItem).Content.ToString();
                if (selected.StartsWith("Other"))
                {
                    otherMotivationTextBox.Visibility = Visibility.Visible;
                    otherMotivationTextBox.Focus();
                }
                else
                {
                    otherMotivationTextBox.Visibility = Visibility.Collapsed;
                }
            }
        }
        private void SkipButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            this.Close();
        }

    }
}