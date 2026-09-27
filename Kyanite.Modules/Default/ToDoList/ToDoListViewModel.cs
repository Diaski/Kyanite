using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using Kyanite.Core.Dialogs;
using Kyanite.Database;
using Kyanite.Modules.Default.ToDoList.Dialogs;
using Kyanite.Services;
using System.Collections.ObjectModel;

namespace Kyanite.Modules.Default.ToDoList;

internal partial class ToDoListViewModel : Module
{
    readonly IDialogService _dialogService;
    readonly NotificationServiceProvider _notificationServiceProvider;
    readonly DispatcherTimer _overdueCheckTimer;

    [Synchronize] ToDoSettings _settings = new();
    public ToDoSettings Settings => _settings;

    [Synchronize] ObservableCollection<ToDoElement> _toDoElements = new();
    public ObservableCollection<ToDoElement> ToDoElements => _toDoElements;

    public ToDoListViewModel(
        ModuleInformation moduleInformation,
        IDialogService dialogService,
        NotificationServiceProvider notificationServiceProvider)
        : base(moduleInformation)
    {
        _dialogService = dialogService;
        _notificationServiceProvider = notificationServiceProvider;
        _overdueCheckTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMinutes(1),
            IsEnabled = true
        };

        _overdueCheckTimer.Tick += (_, _) =>
        {
            foreach (var element in _toDoElements)
            {
                element.RefreshOverdueStatus();

                if (!Settings.EnableNotifications || element.IsCompleted || element.DueDate is null)
                    continue;

                if (Settings.NotifyOnOverdue && !element.OverdueNotified && element.IsOverdue)
                {
                    ShowNotification("Overdue", $"\"{element.Name}\" is overdue.");
                    element.OverdueNotified = true;
                    element.DueSoonNotified = true; // Prevent due soon notification after overdue notification
                }

                if (!element.DueSoonNotified && element.IsDueSoon(Settings.DueSoonThresholdMinutes))
                {
                    ShowNotification("Reminder", $"\"{element.Name}\" is due in {Settings.DueSoonThresholdMinutes} minutes.");
                    element.DueSoonNotified = true;
                }
            }
        };
    }

    /* This should work when you change .net version to proper one */
    void ShowNotification(string title, string message)
    {
        _notificationServiceProvider.ActiveService.Show(title, message);
    }

    [RelayCommand]
    void OpenSettingsDialog()
    {
        var dialog = new DialogBuilder()
            .WithTitle("To-Do Settings")
            .WithSize(420, 260)
            .WithViewModel(new ToDoSettingsViewModel(Settings))
            .SetOnClose(OnSettingsDialogClosed)
            .Build();

        _dialogService.Show(dialog);
    }

    void OnSettingsDialogClosed(Dialog dialog)
    {
        if (dialog.ViewModel is not ToDoSettingsViewModel vm)
            return;
        Settings.EnableNotifications = vm.EnableNotifications;
        Settings.NotifyOnOverdue = vm.NotifyOnOverdue;
        Settings.DueSoonThresholdMinutes = vm.DueSoonThresholdMinutes;
    }

    [RelayCommand]
    void ToggleComplete(ToDoElement element)
    {
        element.CompletedAt = element.IsCompleted ? DateTime.Now : null;
        ApplySorting();
    }

    [RelayCommand]
    void OpenAddNewDialog()
    {
        var dialog = new DialogBuilder()
            .WithTitle("Add new Taks todo")
            .WithSize(1050, 200)
            .WithViewModel(new AddNewToDoElementViewModel())
            .SetOnClose(OnAddDialogClosed)
            .Build();

        _dialogService.Show(dialog);
    }

    [RelayCommand]
    void TogglePin(ToDoElement element)
    {
        element.IsPinned = !element.IsPinned;
        ApplySorting();
    }

    [RelayCommand]
    void RemoveToDoElement(ToDoElement element)
    {
        ToDoElements.Remove(element);
    }

    void OnAddDialogClosed(Dialog dialog)
    {
        if (dialog.ViewModel is not AddNewToDoElementViewModel vm ||
            string.IsNullOrWhiteSpace(vm.ToDoElementName))
            return;

        var dueDate = vm.DueDate;
        var dueTime = vm.DueTime;

        if (dueDate == null) dueDate = DateTime.Today + TimeSpan.FromHours(24);
        if (dueTime == null) dueTime = TimeSpan.Zero;

        ToDoElements.Insert(0, new ToDoElement
        {
            Name = vm.ToDoElementName.Trim(),
            DueDate = dueDate.Value.Date + dueTime.Value,
            CreatedAt = DateTime.Now
        });

        ApplySorting();
    }

    void ApplySorting()
    {
        var sorted = ToDoElements
            .OrderByDescending(e => e.IsPinned)
            .ThenBy(e => e.IsCompleted)
            .ThenByDescending(e => e.DisplayDate)
            .ToList();

        for (int targetIndex = 0; targetIndex < sorted.Count; targetIndex++)
        {
            var item = sorted[targetIndex];
            int currentIndex = ToDoElements.IndexOf(item);

            if (currentIndex != targetIndex)
                ToDoElements.Move(currentIndex, targetIndex);
        }
    }
}