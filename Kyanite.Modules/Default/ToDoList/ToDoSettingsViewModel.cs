using CommunityToolkit.Mvvm.ComponentModel;
using Kyanite.ViewModels;

namespace Kyanite.Modules.Default.ToDoList.Dialogs;


public partial class ToDoSettingsViewModel : ViewModelBase
{
    [ObservableProperty] int _dueSoonThresholdMinutes;
    [ObservableProperty] bool _enableNotifications;
    [ObservableProperty] bool _notifyOnOverdue;

    public ToDoSettingsViewModel(ToDoSettings currentSettings)
    {
        DueSoonThresholdMinutes = currentSettings.DueSoonThresholdMinutes;
        EnableNotifications = currentSettings.EnableNotifications;
        NotifyOnOverdue = currentSettings.NotifyOnOverdue;
    }
}