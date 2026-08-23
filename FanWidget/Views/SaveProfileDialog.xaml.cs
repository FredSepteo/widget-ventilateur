using System.Windows;

namespace FanWidget.Views;

public partial class SaveProfileDialog : Window
{
    public string ResultName { get; private set; } = string.Empty;

    public SaveProfileDialog(string? suggestedName = null)
    {
        InitializeComponent();
        if (!string.IsNullOrWhiteSpace(suggestedName))
            NameBox.Text = suggestedName;
        NameBox.SelectAll();
        NameBox.Focus();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var name = NameBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            System.Windows.MessageBox.Show(this, "Le nom du profil ne peut pas être vide.", "Profil",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        ResultName = name;
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
