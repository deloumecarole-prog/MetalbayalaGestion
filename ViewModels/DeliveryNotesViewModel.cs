using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MetalBayalaGestion.Data;
using MetalBayalaGestion.Models;
using MetalBayalaGestion.Services;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace MetalBayalaGestion.ViewModels;

public partial class DeliveryNotesViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<DeliveryNote> _deliveryNotes = new();

    [ObservableProperty]
    private DeliveryNote? _selectedDeliveryNote;

    [ObservableProperty]
    private string _searchText = "";

    private readonly AppDbContext _context;
    private readonly IDialogService _dialogService;
    private readonly INumberingService _numberingService;
    private readonly IPdfService _pdfService;

    public DeliveryNotesViewModel(AppDbContext context, IDialogService dialogService, INumberingService numberingService, IPdfService pdfService)
    {
        _context = context;
        _dialogService = dialogService;
        _numberingService = numberingService;
        _pdfService = pdfService;
        LoadDeliveryNotes();
    }

    partial void OnSearchTextChanged(string value) => FilterDeliveryNotes();

    private void LoadDeliveryNotes()
    {
        DeliveryNotes.Clear();
        var list = _context.DeliveryNotes.Where(d => !d.IsDeleted).Include(d => d.Client).OrderByDescending(d => d.Date).ToList();
        foreach (var d in list) DeliveryNotes.Add(d);
    }

    private void FilterDeliveryNotes()
    {
        DeliveryNotes.Clear();
        var query = _context.DeliveryNotes.Where(d => !d.IsDeleted).Include(d => d.Client).AsQueryable();
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var s = SearchText.ToLower();
            query = query.Where(d => d.Number.ToLower().Contains(s) || d.Client.Name.ToLower().Contains(s));
        }
        foreach (var d in query.OrderByDescending(d => d.Date).ToList()) DeliveryNotes.Add(d);
    }

    [RelayCommand]
    private async Task AddDeliveryNote()
    {
        var number = await _numberingService.GetNextDeliveryNoteNumberAsync();
        var vm = new DeliveryNoteEditViewModel(new DeliveryNote { Number = number, Date = DateTime.Now }, _context, _dialogService);
        var window = new Views.DeliveryNoteEditWindow { DataContext = vm };
        if (window.ShowDialog() == true)
        {
            _context.DeliveryNotes.Add(vm.DeliveryNote);
            foreach (var line in vm.Lines)
            {
                line.DeliveryNote = vm.DeliveryNote; // navigation : EF Core resout DeliveryNoteId apres l'insertion
                _context.DeliveryNoteLines.Add(line);
            }
            await _context.SaveChangesAsync();
            LoadDeliveryNotes();
        }
    }

    [RelayCommand]
    private async Task EditDeliveryNote()
    {
        if (SelectedDeliveryNote == null) return;
        var vm = new DeliveryNoteEditViewModel(SelectedDeliveryNote, _context, _dialogService);
        var window = new Views.DeliveryNoteEditWindow { DataContext = vm };
        if (window.ShowDialog() == true)
        {
            _context.DeliveryNotes.Update(vm.DeliveryNote);
            var existing = _context.DeliveryNoteLines.Where(l => l.DeliveryNoteId == vm.DeliveryNote.Id).ToList();
            _context.DeliveryNoteLines.RemoveRange(existing);
            foreach (var line in vm.Lines)
            {
                line.DeliveryNoteId = vm.DeliveryNote.Id;
                _context.DeliveryNoteLines.Add(line);
            }
            await _context.SaveChangesAsync();
            LoadDeliveryNotes();
        }
    }

    [RelayCommand]
    private async Task DeleteDeliveryNote()
    {
        if (SelectedDeliveryNote == null) return;
        if (!await _dialogService.ShowConfirmAsync("Confirmation", $"Supprimer le bon de sortie {SelectedDeliveryNote.Number} ?")) return;
        SelectedDeliveryNote.IsDeleted = true;
        await _context.SaveChangesAsync();
        LoadDeliveryNotes();
    }

    [RelayCommand]
    private async Task PrintDeliveryNote()
    {
        if (SelectedDeliveryNote == null) return;
        var path = await _dialogService.ShowSaveFileDialogAsync("Exporter PDF", "PDF|*.pdf", $"{SelectedDeliveryNote.Number}.pdf");
        if (path != null)
        {
            await _pdfService.GenerateDeliveryNotePdfAsync(SelectedDeliveryNote, path);
            await _dialogService.ShowInfoAsync("Succès", "PDF généré avec succès.");
        }
    }

    [RelayCommand]
    private void Refresh() => LoadDeliveryNotes();
}

public partial class DeliveryNoteEditViewModel : ObservableObject
{
    [ObservableProperty]
    private DeliveryNote _deliveryNote;

    [ObservableProperty]
    private ObservableCollection<DeliveryNoteLine> _lines = new();

    [ObservableProperty]
    private ObservableCollection<Client> _clients = new();

    [ObservableProperty]
    private ObservableCollection<Product> _products = new();

    [ObservableProperty]
    private Product? _selectedProduct;

    [ObservableProperty]
    private decimal _lineQuantity = 1;

    [ObservableProperty]
    private ObservableCollection<string> _statuses = new() { "Préparé", "Expédié", "Livré", "Annulé" };

    private readonly AppDbContext _context;
    private readonly IDialogService _dialogService;

    public DeliveryNoteEditViewModel(DeliveryNote deliveryNote, AppDbContext context, IDialogService dialogService)
    {
        _deliveryNote = deliveryNote;
        _context = context;
        _dialogService = dialogService;
        Clients = new ObservableCollection<Client>(context.Clients.Where(c => !c.IsDeleted).OrderBy(c => c.Name).ToList());
        Products = new ObservableCollection<Product>(context.Products.Where(p => !p.IsDeleted && p.IsActive).OrderBy(p => p.Designation).ToList());
        if (deliveryNote.Id > 0)
        {
            Lines = new ObservableCollection<DeliveryNoteLine>(context.DeliveryNoteLines.Where(l => l.DeliveryNoteId == deliveryNote.Id).Include(l => l.Product).ToList());
        }
    }

    [RelayCommand]
    private void AddLine()
    {
        if (SelectedProduct == null) return;
        var line = new DeliveryNoteLine
        {
            ProductId = SelectedProduct.Id,
            Designation = SelectedProduct.Designation,
            Quantity = LineQuantity,
            Unit = SelectedProduct.Unit
        };
        Lines.Add(line);
        SelectedProduct = null;
        LineQuantity = 1;
    }

    [RelayCommand]
    private void RemoveLine(DeliveryNoteLine line) => Lines.Remove(line);

    [RelayCommand]
    private void Save(Window window)
    {
        if (DeliveryNote.ClientId == 0)
        {
            _dialogService.ShowErrorAsync("Erreur", "Veuillez sélectionner un client.").Wait();
            return;
        }
        if (!Lines.Any())
        {
            _dialogService.ShowErrorAsync("Erreur", "Ajoutez au moins une ligne.").Wait();
            return;
        }
        window.DialogResult = true;
        window.Close();
    }

    [RelayCommand]
    private void Cancel(Window window)
    {
        window.DialogResult = false;
        window.Close();
    }
}
