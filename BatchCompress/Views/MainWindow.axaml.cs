using Avalonia.Controls;
using CommunityToolkit.Mvvm.Messaging;
using static CommunityToolkit.Mvvm.Messaging.WeakReferenceMessenger;
using BatchCompress.Messages;
using BatchCompress.ViewModels;
using Avalonia.Platform.Storage;
using System;
using Avalonia;

namespace BatchCompress.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // 设置ViewModel的窗口引用
        SetWindowReference();
    }

    private void SetWindowReference()
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.CurrentWindow = this;
            vm.RefreshUI = () => 
            {
                CompressionItemsListBox?.InvalidateVisual();
                this.InvalidateVisual();
            };
        }
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        SetWindowReference();
    }
}