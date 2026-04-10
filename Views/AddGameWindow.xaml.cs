using System;
using System.Windows;
using System.Windows.Controls;
using Balanced_Gaming.Data;
using Balanced_Gaming.Models;

namespace Balanced_Gaming.Views
{
    public partial class AddGameWindow : Window
    {
        public AddGameWindow()
        {
            InitializeComponent();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Validate game name
            if (string.IsNullOrWhiteSpace(gameNameTextBox.Text))
            {
                MessageBox.Show("Please enter a game name!", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Validate genre selection
            if (genreComboBox.SelectedItem == null)
            {
                MessageBox.Show("Please select a genre!", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (var db = new AppDbContext())
                {
                    // Get expected playtime if selected
                    string expectedTime = expectedPlaytimeComboBox.SelectedItem != null
                        ? ((ComboBoxItem)expectedPlaytimeComboBox.SelectedItem).Content.ToString()
                        : "";

                    var newGame = new Game
                    {
                        gameName = gameNameTextBox.Text.Trim(),
                        genre = ((ComboBoxItem)genreComboBox.SelectedItem).Content.ToString(),
                        processName = expectedTime, // Storing expected playtime here
                        addedAt = DateTime.Now
                    };

                    db.games.Add(newGame);
                    db.SaveChanges();
                }

                DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving game: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            this.Close();
        }
    }
}