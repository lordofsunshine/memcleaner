using System.ComponentModel;
using System.Windows;
using MemCleaner.ViewModels;

namespace MemCleaner;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Closing += OnClosing;
    }

    public bool ForceClose { get; set; }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (ForceClose)
        {
            return;
        }

        if (DataContext is MainViewModel { MinimizeToTray: true })
        {
            e.Cancel = true;
            Hide();
        }
    }
}
