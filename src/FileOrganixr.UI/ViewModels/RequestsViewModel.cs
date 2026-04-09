using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using FileOrganixr.Core.Runtime.ActionRequests;
using FileOrganixr.UI.Commands;
using System.Windows.Data;
using System.Windows.Input;

namespace FileOrganixr.UI.ViewModels;
public sealed class RequestsViewModel : ViewModelBase
{
    public const string AllStatusesOption = "All Statuses";
    public const string SortByFileNameOption = "File Name (A-Z)";
    public const string SortByNewestOption = "Last Update (Newest)";
    public const string SortByOldestOption = "Last Update (Oldest)";
    public const string SortByStatusOption = "Status (A-Z)";

    private readonly DelegateCommand _clearFiltersCommand;
    private string _searchText = string.Empty;
    private ActionRequestItemViewModel? _selectedItem;
    private string _selectedSortOption = SortByNewestOption;
    private string _selectedStatusFilter = AllStatusesOption;

    public event EventHandler<ActionRequestItemViewModel?>? SelectedItemChanged;

    public ObservableCollection<ActionRequestItemViewModel> Items { get; } = [];

    public RequestsViewModel()
    {
        View = CollectionViewSource.GetDefaultView(Items);
        View.Filter = item => item is ActionRequestItemViewModel typed && MatchesCurrentFilters(typed);
        Items.CollectionChanged += OnItemsChanged;

        _clearFiltersCommand = new DelegateCommand(ClearFilters, CanClearFilters);
        ApplySort();
        UpdateViewState();
    }

    public ICommand ClearFiltersCommand => _clearFiltersCommand;

    public bool HasActiveFilters =>
        !string.IsNullOrWhiteSpace(SearchText) ||
        !string.Equals(SelectedStatusFilter, AllStatusesOption, StringComparison.Ordinal);

    public string EmptyStateText =>
        Items.Count == 0
            ? "No requests have been detected yet."
            : "No requests match the current filters.";

    public bool IsEmptyStateVisible => !HasVisibleItems;

    public ActionRequestItemViewModel? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (!SetProperty(ref _selectedItem, value))
            {
                return;
            }

            SelectedItemChanged?.Invoke(this, value);
        }
    }

    public string SelectedSortOption
    {
        get => _selectedSortOption;
        set
        {
            if (!SetProperty(ref _selectedSortOption, value))
            {
                return;
            }

            ApplySort();
        }
    }

    public string SelectedStatusFilter
    {
        get => _selectedStatusFilter;
        set
        {
            if (!SetProperty(ref _selectedStatusFilter, value))
            {
                return;
            }

            RefreshView();
            OnPropertyChanged(nameof(HasActiveFilters));
            _clearFiltersCommand.RaiseCanExecuteChanged();
        }
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (!SetProperty(ref _searchText, value))
            {
                return;
            }

            RefreshView();
            OnPropertyChanged(nameof(HasActiveFilters));
            _clearFiltersCommand.RaiseCanExecuteChanged();
        }
    }

    public IReadOnlyList<string> SortOptions { get; } =
    [
        SortByNewestOption,
        SortByOldestOption,
        SortByFileNameOption,
        SortByStatusOption
    ];

    public IReadOnlyList<string> StatusFilterOptions { get; } =
    [
        AllStatusesOption,
        "Detected",
        "NoRuleMatched",
        "RuleMatched",
        "PendingApproval",
        "Approved",
        "Rejected",
        "Queued",
        "Processing",
        "Completed",
        "Failed"
    ];

    public ICollectionView View { get; }

    public bool HasVisibleItems => VisibleCount > 0;

    public string ResultCountText => $"{VisibleCount} shown / {Items.Count} total";

    public int VisibleCount { get; private set; }

    public ActionRequestItemViewModel? FindById(Guid requestId)
    {
        return Items.FirstOrDefault(item => item.Id == requestId);
    }

    public void ReplaceItems(IEnumerable<ActionRequest> requests)
    {
        ArgumentNullException.ThrowIfNull(requests);

        Items.Clear();
        foreach (var request in requests)
        {
            Items.Add(ActionRequestItemViewModel.FromActionRequest(request));
        }

        RefreshView();
    }

    public void Upsert(ActionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var existing = FindById(request.Id);
        if (existing is null)
        {
            Items.Add(ActionRequestItemViewModel.FromActionRequest(request));
            RefreshView();
            return;
        }

        existing.UpdateFrom(request);
        RefreshView();
    }

    private void ApplySort()
    {
        using (View.DeferRefresh())
        {
            View.SortDescriptions.Clear();

            switch (SelectedSortOption)
            {
                case SortByOldestOption:
                    View.SortDescriptions.Add(
                        new SortDescription(nameof(ActionRequestItemViewModel.LastStatusTimestampUtc), ListSortDirection.Ascending));
                    break;
                case SortByFileNameOption:
                    View.SortDescriptions.Add(
                        new SortDescription(nameof(ActionRequestItemViewModel.FileName), ListSortDirection.Ascending));
                    break;
                case SortByStatusOption:
                    View.SortDescriptions.Add(
                        new SortDescription(nameof(ActionRequestItemViewModel.CurrentStatusText), ListSortDirection.Ascending));
                    break;
                case SortByNewestOption:
                default:
                    View.SortDescriptions.Add(
                        new SortDescription(nameof(ActionRequestItemViewModel.LastStatusTimestampUtc), ListSortDirection.Descending));
                    break;
            }
        }
    }

    private bool CanClearFilters()
    {
        return HasActiveFilters;
    }

    private void ClearFilters()
    {
        SearchText = string.Empty;
        SelectedStatusFilter = AllStatusesOption;
    }

    private bool MatchesCurrentFilters(ActionRequestItemViewModel item)
    {
        if (!string.Equals(SelectedStatusFilter, AllStatusesOption, StringComparison.Ordinal) &&
            !string.Equals(item.CurrentStatusText, SelectedStatusFilter, StringComparison.Ordinal))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(SearchText))
        {
            return true;
        }

        var term = SearchText.Trim();

        return Contains(item.FileName, term)
               || Contains(item.FullPath, term)
               || Contains(item.MonitoredFolderName, term)
               || Contains(item.RuleName, term)
               || Contains(item.LatestNote, term)
               || Contains(item.Id.ToString(), term);
    }

    private static bool Contains(string? source, string term)
    {
        return !string.IsNullOrWhiteSpace(source) &&
               source.Contains(term, StringComparison.OrdinalIgnoreCase);
    }

    private void RefreshView()
    {
        View.Refresh();
        UpdateViewState();
    }

    private void OnItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        UpdateViewState();
    }

    private void UpdateViewState()
    {
        VisibleCount = View.Cast<object>().Count();
        OnPropertyChanged(nameof(VisibleCount));
        OnPropertyChanged(nameof(HasVisibleItems));
        OnPropertyChanged(nameof(IsEmptyStateVisible));
        OnPropertyChanged(nameof(EmptyStateText));
        OnPropertyChanged(nameof(ResultCountText));
    }
}
