// Alan Medeiros
using AcademiaDoZe.Application.DTOs;
using AcademiaDoZe.Application.Interfaces;
using AcademiaDoZe.Presentation.AppMaui.Message;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace AcademiaDoZe.Presentation.AppMaui.ViewModels;

public sealed record PessoaAcesso(int Id, string Nome, string Cpf)
{
    public string Descricao => $"{Nome} · {Cpf}";
}

public partial class AcessoViewModel(IRegistroAcessoService acessos, IAlunoService alunos, IColaboradorService colaboradores) : BaseViewModel
{
    private readonly List<PessoaAcesso> _cadastros = [];
    [ObservableProperty] private string busca = string.Empty;
    public ObservableCollection<string> Tipos { get; } = ["Aluno", "Colaborador"];
    public ObservableCollection<PessoaAcesso> Pessoas { get; } = [];
    public ObservableCollection<AcessoDto> Historico { get; } = [];
    [ObservableProperty] private string tipoSelecionado = "Aluno";
    [ObservableProperty] private PessoaAcesso? pessoaSelecionada;
    [ObservableProperty] private string mensagem = "Selecione uma pessoa para registrar a entrada ou a saída.";
    [ObservableProperty] private bool aviso;
    public bool TemPessoa => PessoaSelecionada is not null;

    partial void OnBuscaChanged(string value) => FiltrarPessoas();

    private void FiltrarPessoas()
    {
        PessoaSelecionada = null;
        Pessoas.Clear();
        var termo = Busca.Trim();
        var digitos = string.Concat(termo.Where(char.IsDigit));
        foreach (var pessoa in _cadastros.Where(p => termo.Length == 0 || p.Nome.Contains(termo, StringComparison.OrdinalIgnoreCase)
            || (digitos.Length > 0 && string.Concat(p.Cpf.Where(char.IsDigit)).Contains(digitos))))
            Pessoas.Add(pessoa);
    }

    partial void OnPessoaSelecionadaChanged(PessoaAcesso? value)
    {
        Historico.Clear();
        Mensagem = value is null ? "Selecione uma pessoa cadastrada." : $"{value.Nome}: consulte o histórico ou registre o acesso.";
        Aviso = false;
        OnPropertyChanged(nameof(TemPessoa));
    }

    [RelayCommand]
    private async Task CarregarAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            using var cts = new CancellationTokenSource(Timeout);
            PessoaSelecionada = null;
            Pessoas.Clear();
            Historico.Clear();
            _cadastros.Clear();
            Busca = string.Empty;
            if (TipoSelecionado == "Colaborador")
                foreach (var p in (await colaboradores.ObterTodosAsync(cts.Token)).OrderBy(p => p.Nome))
                    _cadastros.Add(new(p.Id, p.Nome, p.Cpf));
            else
                foreach (var p in (await alunos.ObterTodosAsync(cts.Token)).OrderBy(p => p.Nome))
                    _cadastros.Add(new(p.Id, p.Nome, p.Cpf));
            FiltrarPessoas();
            Mensagem = Pessoas.Count == 0 ? "Nenhuma pessoa cadastrada nesta categoria. Faça o cadastro pelo menu." : "Selecione a pessoa pelo nome e CPF.";
            Aviso = false;
        }
        catch (Exception ex) { Mensagem = $"Não foi possível carregar os cadastros: {ex.Message}"; Aviso = true; }
        finally { IsBusy = false; }
    }

    [RelayCommand] private Task EntradaAsync() => ExecutarAsync(true);
    [RelayCommand] private Task SaidaAsync() => ExecutarAsync(false);

    private async Task ExecutarAsync(bool entrada)
    {
        if (IsBusy || PessoaSelecionada is null) return;
        IsBusy = true;
        try
        {
            using var cts = new CancellationTokenSource(Timeout);
            var resultado = entrada
                ? await acessos.RegistrarEntradaAsync(TipoSelecionado == "Colaborador", PessoaSelecionada.Id, cts.Token)
                : await acessos.RegistrarSaidaAsync(TipoSelecionado == "Colaborador", PessoaSelecionada.Id, cts.Token);
            Mensagem = $"{resultado.Nome}\n{resultado.Mensagem}";
            Aviso = false;
            await AtualizarHistoricoAsync(cts.Token);
        }
        catch (Exception ex) { Mensagem = ex.Message; Aviso = true; }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task ConsultarAsync()
    {
        if (IsBusy || PessoaSelecionada is null) return;
        IsBusy = true;
        try
        {
            using var cts = new CancellationTokenSource(Timeout);
            await AtualizarHistoricoAsync(cts.Token);
        }
        catch (Exception ex) { Mensagem = ex.Message; Aviso = true; }
        finally { IsBusy = false; }
    }

    private async Task AtualizarHistoricoAsync(CancellationToken ct)
    {
        if (PessoaSelecionada is null) return;
        var registros = await acessos.ObterHistoricoAsync(TipoSelecionado == "Colaborador", PessoaSelecionada.Id, ct);
        Historico.Clear();
        foreach (var r in registros) Historico.Add(r);
    }
}
