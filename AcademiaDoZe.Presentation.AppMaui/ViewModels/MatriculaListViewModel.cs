// Alan Medeiros
using AcademiaDoZe.Application.DTOs;
using AcademiaDoZe.Application.Interfaces;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace AcademiaDoZe.Presentation.AppMaui.ViewModels;

public partial class MatriculaListViewModel : BaseViewModel
{
    private readonly IMatriculaService _matriculaService;

    public ObservableCollection<string> FilterTypes { get; } = ["Ativas", "Todas", "Id", "Id Aluno", "Vencendo em dias"];

    private ObservableCollection<MatriculaDto> _matriculas = [];
    public ObservableCollection<MatriculaDto> Matriculas
    {
        get => _matriculas;
        set => SetProperty(ref _matriculas, value);
    }

    private MatriculaDto? _selectedMatricula;
    public MatriculaDto? SelectedMatricula
    {
        get => _selectedMatricula;
        set => SetProperty(ref _selectedMatricula, value);
    }

    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set => SetProperty(ref _searchText, value);
    }

    private string _selectedFilterType = "Ativas";
    public string SelectedFilterType
    {
        get => _selectedFilterType;
        set => SetProperty(ref _selectedFilterType, value);
    }

    public MatriculaListViewModel(IMatriculaService matriculaService)
    {
        _matriculaService = matriculaService;
        Title = "Matrículas";
    }

    [RelayCommand]
    private static async Task AddMatriculaAsync()
    {
        try
        {
            await Shell.Current.GoToAsync("matricula");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Erro", $"Erro ao navegar para tela de cadastro: {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    private static async Task EditMatriculaAsync(MatriculaDto matricula)
    {
        try
        {
            if (matricula == null) return;

            await Shell.Current.GoToAsync($"matricula?Id={matricula.Id}");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Erro", $"Erro ao navegar para tela de edição: {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsRefreshing = true;
        await LoadMatriculasAsync();
    }

    [RelayCommand]
    private async Task LoadMatriculasAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                Matriculas.Clear();
                OnPropertyChanged(nameof(Matriculas));
            });

            using var cts = new CancellationTokenSource(Timeout);
            var matriculasList = await _matriculaService.ObterTodasAsync(cts.Token);

            if (matriculasList != null)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    foreach (var matricula in matriculasList)
                    {
                        Matriculas.Add(matricula);
                    }

                    OnPropertyChanged(nameof(Matriculas));
                });
            }
        }
        catch (OperationCanceledException)
        {
            await Shell.Current.DisplayAlertAsync("Tempo Esgotado", "O carregamento das matrículas expirou. Verifique a conexão.", "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Erro", $"Erro ao carregar matrículas: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    private async Task SearchMatriculasAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                Matriculas.Clear();
                OnPropertyChanged(nameof(Matriculas));
            });

            using var cts = new CancellationTokenSource(Timeout);
            var termo = SearchText?.Trim() ?? string.Empty;
            IEnumerable<MatriculaDto> resultados = [];

            switch (SelectedFilterType)
            {
                case "Todas":
                    resultados = await _matriculaService.ObterTodasAsync(cts.Token);
                    break;

                case "Id":
                    if (!int.TryParse(termo, out int id))
                    {
                        await Shell.Current.DisplayAlertAsync("Validação", "Para busca por Id, informe um número inteiro.", "OK");
                        return;
                    }
                    var porId = await _matriculaService.ObterPorIdAsync(id, cts.Token);
                    resultados = porId != null ? [porId] : [];
                    break;

                case "Id Aluno":
                    if (!int.TryParse(termo, out int alunoId))
                    {
                        await Shell.Current.DisplayAlertAsync("Validação", "Para busca por Id do Aluno, informe um número inteiro.", "OK");
                        return;
                    }
                    resultados = await _matriculaService.ObterPorAlunoIdAsync(alunoId, cts.Token);
                    break;

                case "Vencendo em dias":
                    if (!int.TryParse(termo, out int dias))
                    {
                        await Shell.Current.DisplayAlertAsync("Validação", "Para busca por vencimento, informe a quantidade de dias.", "OK");
                        return;
                    }
                    resultados = await _matriculaService.ObterVencendoEmDiasAsync(dias, cts.Token);
                    break;

                default:
                    int.TryParse(termo, out int alunoDasAtivas);
                    resultados = await _matriculaService.ObterAtivasAsync(alunoDasAtivas, cts.Token);
                    break;
            }

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                foreach (var matricula in resultados)
                {
                    Matriculas.Add(matricula);
                }

                OnPropertyChanged(nameof(Matriculas));
            });
        }
        catch (OperationCanceledException)
        {
            await Shell.Current.DisplayAlertAsync("Tempo Esgotado", "A busca das matrículas expirou. Verifique a conexão.", "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Erro", $"Erro ao buscar matrículas: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task DeleteMatriculaAsync(MatriculaDto matricula)
    {
        if (matricula == null) return;

        bool confirmou = await Shell.Current.DisplayAlertAsync(
            "Confirmar exclusão",
            $"Deseja realmente excluir a matrícula {matricula.Id} de {matricula.AlunoMatricula?.Nome}?",
            "Sim", "Não");

        if (!confirmou) return;

        try
        {
            IsBusy = true;
            using var cts = new CancellationTokenSource(Timeout);
            bool removeu = await _matriculaService.RemoverAsync(matricula.Id, cts.Token);

            if (removeu)
            {
                Matriculas.Remove(matricula);
                OnPropertyChanged(nameof(Matriculas));
                await Shell.Current.DisplayAlertAsync("Sucesso", "Matrícula excluída com sucesso!", "OK");
            }
            else
            {
                await Shell.Current.DisplayAlertAsync("Erro", "Não foi possível excluir a matrícula.", "OK");
            }
        }
        catch (OperationCanceledException)
        {
            await Shell.Current.DisplayAlertAsync("Tempo Esgotado", "A exclusão da matrícula expirou. Verifique a conexão.", "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Erro", $"Erro ao excluir matrícula: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
