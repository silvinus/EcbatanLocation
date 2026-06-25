using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using EcbatanLocation.Application.DTOs;
using EcbatanLocation.Domain.Enums;
using EcbatanLocation.Web.Services;

namespace EcbatanLocation.Web.Components.Pages;

public partial class Home : IDisposable
{
    [CascadingParameter]
    private Task<AuthenticationState>? AuthState { get; set; }

    // Injected directly (not cascaded): the layout renders as static SSR, so a
    // cascaded service would neither cross the interactivity boundary nor share
    // this circuit's DI scope. Injecting here binds to the circuit-scoped instance.
    [Inject]
    private ViewportService Viewport { get; set; } = default!;

    private int Year { get; set; }
    private int Month { get; set; }
    private Guid? SelectedStudioId { get; set; }
    private ReservationStatus? SelectedStatus { get; set; }
    private Guid? SelectedOwnerId { get; set; }

    private MonthlyPlanningDto? _planning;
    private IReadOnlyList<StudioDto>? _studios;
    private IReadOnlyList<OwnerDto>? _owners;
    private DateOnly? _selectedDate;
    private DateOnly? _rangeStart;
    private DateOnly? _rangeEnd;
    private DailyOccupationDto? _occupation;
    private RangeOccupationDto? _rangeOccupation;
    private MonthlyOccupationDto? _monthlyOccupation;
    private ReservationDetailDto? _selectedReservation;

    private bool _isAuthenticated;
    private OwnerDto? _currentOwner;

    private bool _showFormModal;
    private ReservationDetailDto? _editingReservation;
    private bool _showDeleteModal;
    private ReservationDetailDto? _deletingReservation;
    private bool _loading;
    private ViewMode _viewMode = ViewMode.Month;
    private DateOnly _weekStart;
    private bool _viewExplicitlyChosen;

    private bool _showFilters;
    private bool _showHelp;

    private int ActiveFilterCount =>
        (SelectedStudioId is not null ? 1 : 0)
        + (SelectedStatus is not null ? 1 : 0)
        + (SelectedOwnerId is not null ? 1 : 0);

    private enum ViewMode { Month, Week, List, Agenda }

    private bool IsMobile => Viewport.IsMobile;

    private void ToggleFilters()
    {
        _showFilters = !_showFilters;
        _showHelp = false;
    }

    private void CloseFilters() => _showFilters = false;

    private void ToggleHelp()
    {
        _showHelp = !_showHelp;
        _showFilters = false;
    }

    private void CloseHelp() => _showHelp = false;

    protected override async Task OnInitializedAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        Year = today.Year;
        Month = today.Month;
        _selectedDate = today;

        _studios = await Mediator.Send(new EcbatanLocation.Application.Queries.GetStudios.GetStudiosQuery());
        _owners = await Mediator.Send(new EcbatanLocation.Application.Queries.GetOwners.GetOwnersQuery());

        await ResolveCurrentOwner();
        await LoadPlanning();
        await LoadOccupation();
        await LoadMonthlyOccupation();

        Viewport.OnChange += OnViewportChanged;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
            await Viewport.InitializeAsync();
    }

    // The viewport service initializes after the first interactive render, so the
    // mobile default is applied here rather than in OnInitializedAsync.
    private void OnViewportChanged()
    {
        if (!_viewExplicitlyChosen)
            _viewMode = IsMobile ? ViewMode.Agenda : ViewMode.Month;
        StateHasChanged();
    }

    private void SelectView(ViewMode mode)
    {
        _viewExplicitlyChosen = true;
        if (mode == ViewMode.Week)
            SwitchToWeekView();
        else
            _viewMode = mode;
    }

    public void Dispose()
    {
        Viewport.OnChange -= OnViewportChanged;
    }

    private async Task ResolveCurrentOwner()
    {
        if (AuthState is null) return;
        var state = await AuthState;
        _isAuthenticated = state.User.Identity?.IsAuthenticated == true;

        if (_isAuthenticated)
        {
            var userId = state.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId is not null)
            {
                _currentOwner = await Mediator.Send(
                    new EcbatanLocation.Application.Queries.GetOwnerByUserId.GetOwnerByUserIdQuery(userId));
            }
        }
    }

    private async Task LoadPlanning()
    {
        _loading = true;
        try
        {
            _planning = await Mediator.Send(
                new EcbatanLocation.Application.Queries.GetMonthlyPlanning.GetMonthlyPlanningQuery(
                    Year, Month, SelectedStudioId, SelectedStatus, SelectedOwnerId));
        }
        finally
        {
            _loading = false;
        }
    }

    private async Task LoadOccupation()
    {
        var date = _selectedDate ?? DateOnly.FromDateTime(DateTime.Today);
        _occupation = await Mediator.Send(
            new EcbatanLocation.Application.Queries.GetDailyOccupation.GetDailyOccupationQuery(date));
    }

    // Per-day occupation for the visible month, used to color the day header gradient.
    // Independent of the studio/status/owner filters, so it only needs to be reloaded
    // when the month changes or a reservation is created/edited/deleted.
    private async Task LoadMonthlyOccupation()
    {
        _monthlyOccupation = await Mediator.Send(
            new EcbatanLocation.Application.Queries.GetMonthlyOccupation.GetMonthlyOccupationQuery(Year, Month));
    }

    private async Task PreviousMonth()
    {
        if (Month == 1) { Month = 12; Year--; }
        else { Month--; }
        await ResetSelectionAndReload();
    }

    private async Task NextMonth()
    {
        if (Month == 12) { Month = 1; Year++; }
        else { Month++; }
        await ResetSelectionAndReload();
    }

    private async Task OnYearMonthChanged((int year, int month) value)
    {
        Year = value.year;
        Month = value.month;
        await ResetSelectionAndReload();
    }

    private async Task ResetSelectionAndReload()
    {
        _selectedDate = null;
        _rangeStart = null;
        _rangeEnd = null;
        _rangeOccupation = null;
        await LoadPlanning();
        await LoadOccupation();
        await LoadMonthlyOccupation();
    }

    private async Task OnStudioChanged(Guid? studioId)
    {
        SelectedStudioId = studioId;
        await LoadPlanning();
    }

    private async Task OnStatusChanged(ReservationStatus? status)
    {
        SelectedStatus = status;
        await LoadPlanning();
    }

    private async Task OnOwnerChanged(Guid? ownerId)
    {
        SelectedOwnerId = ownerId;
        await LoadPlanning();
    }

    private async Task OnDaySelected(DateOnly date)
    {
        // Range selection is disabled: clicking a day only selects that single
        // date and shows its daily occupation.
        _selectedDate = date;
        _rangeStart = null;
        _rangeEnd = null;
        _rangeOccupation = null;
        await LoadOccupation();
    }

    private async Task ClearRange()
    {
        _rangeStart = null;
        _rangeEnd = null;
        _rangeOccupation = null;
        await LoadOccupation();
    }

    private async Task LoadRangeOccupation()
    {
        if (_rangeStart.HasValue && _rangeEnd.HasValue)
        {
            _rangeOccupation = await Mediator.Send(
                new EcbatanLocation.Application.Queries.GetRangeOccupation.GetRangeOccupationQuery(
                    _rangeStart.Value, _rangeEnd.Value));
        }
    }

    private async Task OnReservationClicked(Guid reservationId)
    {
        _selectedReservation = await Mediator.Send(
            new EcbatanLocation.Application.Queries.GetReservationDetail.GetReservationDetailQuery(reservationId));
    }

    private void CloseDetailModal()
    {
        _selectedReservation = null;
    }

    private void OpenCreateModal()
    {
        _editingReservation = null;
        _showFormModal = true;
    }

    private void OpenEditModal(ReservationDetailDto reservation)
    {
        _editingReservation = reservation;
        _selectedReservation = null;
        _showFormModal = true;
    }

    private void CloseFormModal()
    {
        _showFormModal = false;
        _editingReservation = null;
    }

    private async Task OnFormSaved()
    {
        _showFormModal = false;
        _editingReservation = null;
        _selectedReservation = null;
        await LoadPlanning();
        await LoadOccupation();
        await LoadMonthlyOccupation();
        await LoadRangeOccupation();
    }

    private void OpenDeleteModal(ReservationDetailDto reservation)
    {
        _deletingReservation = reservation;
        _selectedReservation = null;
        _showDeleteModal = true;
    }

    private void CloseDeleteModal()
    {
        _showDeleteModal = false;
        _deletingReservation = null;
    }

    private async Task OnDeleted()
    {
        _showDeleteModal = false;
        _deletingReservation = null;
        _selectedReservation = null;
        await LoadPlanning();
        await LoadOccupation();
        await LoadMonthlyOccupation();
        await LoadRangeOccupation();
    }

    private async Task OnReservationStatusChanged()
    {
        if (_selectedReservation is not null)
        {
            _selectedReservation = await Mediator.Send(
                new EcbatanLocation.Application.Queries.GetReservationDetail.GetReservationDetailQuery(
                    _selectedReservation.Id));
        }
        await LoadPlanning();
        await LoadOccupation();
        await LoadMonthlyOccupation();
        await LoadRangeOccupation();
    }

    private void SwitchToWeekView()
    {
        _viewMode = ViewMode.Week;
        var reference = _selectedDate ?? DateOnly.FromDateTime(DateTime.Today);
        var dayOfWeek = ((int)reference.DayOfWeek + 6) % 7;
        _weekStart = reference.AddDays(-dayOfWeek);
    }

    private string GetMonthLabel()
    {
        var date = new DateTime(Year, Month, 1);
        var culture = new System.Globalization.CultureInfo("fr-FR");
        return date.ToString("MMMM yyyy", culture);
    }
}
