using System.Windows;

namespace FanWidget.Views;

public partial class RenameFanDialog : Window
{
    public string ResultLabel { get; private set; } = string.Empty;

    public RenameFanDialog(string hardwareName, string currentLabel)
    {
        InitializeComponent();
        HardwareHint.Text = $"Capteur : {hardwareName}";
        LabelBox.Text = currentLabel;
        LabelBox.SelectAll();
        LabelBox.Focus();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var text = LabelBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            System.Windows.MessageBox.Show(this, "Le libellé ne peut pas être vide.", "Renommage",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        ResultLabel = text;
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
