// Alan Medeiros
using AcademiaDoZe.Presentation.AppMaui.Message;
using AcademiaDoZe.Presentation.AppMaui.ViewModels;
using CommunityToolkit.Mvvm.Messaging;

namespace AcademiaDoZe.Presentation.AppMaui.Views;

public partial class AcessoPage : ContentPage
{
    private readonly AcessoViewModel _viewModel;
    public AcessoPage(AcessoViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        WeakReferenceMessenger.Default.Register<AcessoPage, BancoPreferencesUpdatedMessage>(this,
            (pagina, mensagem) => MainThread.BeginInvokeOnMainThread(async () => await pagina._viewModel.CarregarCommand.ExecuteAsync(null)));
        await _viewModel.CarregarCommand.ExecuteAsync(null);
    }
    protected override void OnDisappearing()
    {
        WeakReferenceMessenger.Default.UnregisterAll(this);
        base.OnDisappearing();
    }
    private async void OnTipoChanged(object? sender, EventArgs e)
    {
        if (BindingContext is AcessoViewModel vm) await vm.CarregarCommand.ExecuteAsync(null);
    }
    private async void OnPessoaChanged(object? sender, EventArgs e)
    {
        if (BindingContext is AcessoViewModel vm) await vm.ConsultarCommand.ExecuteAsync(null);
    }
}
