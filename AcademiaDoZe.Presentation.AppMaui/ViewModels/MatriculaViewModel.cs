// Alan Medeiros
using AcademiaDoZe.Application.DTOs;
using AcademiaDoZe.Application.Enums;
using AcademiaDoZe.Application.Interfaces;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace AcademiaDoZe.Presentation.AppMaui.ViewModels;

[QueryProperty(nameof(MatriculaId), "Id")]
public partial class MatriculaViewModel : BaseViewModel
{
    private readonly IMatriculaService _matriculaService;
    private readonly IAlunoService _alunoService;

    private bool _sincronizando;
    private bool _inicializado;

    public IReadOnlyList<AppMatriculaPlano> PlanoTipos { get; } = [.. Enum.GetValues<AppMatriculaPlano>()];

    public ObservableCollection<RestricaoSelectable> RestricoesMulti { get; } = [];

    private MatriculaDto _matricula = CriarMatriculaPadrao();
    public MatriculaDto Matricula
    {
        get => _matricula;
        set
        {
            if (SetProperty(ref _matricula, value))
            {
                SincronizarSelecaoPelasFlags();
                NotificarVigencia();
            }
        }
    }

    private int _matriculaId;
    public int MatriculaId
    {
        get => _matriculaId;
        set => SetProperty(ref _matriculaId, value);
    }

    private bool _isEditMode;
    public bool IsEditMode
    {
        get => _isEditMode;
        set => SetProperty(ref _isEditMode, value);
    }

    public AppMatriculaPlano PlanoSelecionado
    {
        get => Matricula.Plano;
        set
        {
            if (Matricula.Plano == value) return;

            Matricula.Plano = value;
            RecalcularDataFim();
        }
    }

    public DateOnly DataInicioSelecionada
    {
        get => Matricula.DataInicio;
        set
        {
            if (Matricula.DataInicio == value) return;

            Matricula.DataInicio = value;
            RecalcularDataFim();
        }
    }

    public string DuracaoPlano => Matricula.Plano switch
    {
        AppMatriculaPlano.Mensal => "1 mês de vigência",
        AppMatriculaPlano.Trimestral => "3 meses de vigência",
        AppMatriculaPlano.Semestral => "6 meses de vigência",
        AppMatriculaPlano.Anual => "12 meses de vigência",
        _ => string.Empty
    };

    public string VigenciaResumo => $"{Matricula.DataInicio:dd/MM/yyyy} até {Matricula.DataFim:dd/MM/yyyy}";

    public bool HasAlunoVinculado => Matricula.AlunoMatricula != null && Matricula.AlunoMatricula.Id > 0;
    public bool HasNoAlunoVinculado => !HasAlunoVinculado;
    public bool IsMenor16 => HasAlunoVinculado
        && Matricula.AlunoMatricula.DataNascimento != default
        && Matricula.AlunoMatricula.DataNascimento > DateOnly.FromDateTime(DateTime.Today.AddYears(-16));
    public bool HasRestricoes => Matricula.RestricoesMedicas != AppMatriculaRestricoes.None;
    public bool ExigeLaudo => IsMenor16 || HasRestricoes;
    public bool HasLaudo => Matricula.LaudoMedico?.Conteudo != null && Matricula.LaudoMedico.Conteudo.Length > 0;
    public bool LaudoPendente => ExigeLaudo && !HasLaudo;
    public bool LaudoOpcional => !ExigeLaudo && !HasLaudo;

    public MatriculaViewModel(IMatriculaService matriculaService, IAlunoService alunoService)
    {
        _matriculaService = matriculaService;
        _alunoService = alunoService;
        Title = "Detalhes da Matrícula";

        MontarRestricoesMulti();
    }

    public async Task InitializeAsync()
    {
        if (_inicializado)
        {
            NotificarEstadoVisual();
            return;
        }

        _inicializado = true;

        if (MatriculaId > 0)
        {
            IsEditMode = true;
            Title = "Editar Matrícula";
            await LoadMatriculaAsync();
        }
        else
        {
            IsEditMode = false;
            Title = "Nova Matrícula";
            Matricula = CriarMatriculaPadrao();
            RecalcularDataFim();
        }

        NotificarEstadoVisual();
    }

    private static LogradouroDto CriarEnderecoVazio() => new()
    {
        Cep = string.Empty,
        Nome = string.Empty,
        Bairro = string.Empty,
        Cidade = string.Empty,
        Estado = string.Empty,
        Pais = "Brasil"
    };

    private static AlunoDto CriarAlunoVazio() => new()
    {
        Nome = string.Empty,
        Cpf = string.Empty,
        DataNascimento = DateOnly.FromDateTime(DateTime.Today.AddYears(-18)),
        Telefone = string.Empty,
        Endereco = CriarEnderecoVazio(),
        Numero = string.Empty
    };

    private static MatriculaDto CriarMatriculaPadrao()
    {
        var hoje = DateOnly.FromDateTime(DateTime.Today);

        return new MatriculaDto
        {
            AlunoMatricula = CriarAlunoVazio(),
            Plano = AppMatriculaPlano.Mensal,
            DataInicio = hoje,
            DataFim = hoje.AddMonths(1),
            Objetivo = string.Empty,
            RestricoesMedicas = AppMatriculaRestricoes.None,
            ObservacoesRestricoes = string.Empty,
            LaudoMedico = null
        };
    }

    private static DateOnly CalcularDataFim(AppMatriculaPlano plano, DateOnly inicio) => plano switch
    {
        AppMatriculaPlano.Mensal => inicio.AddMonths(1),
        AppMatriculaPlano.Trimestral => inicio.AddMonths(3),
        AppMatriculaPlano.Semestral => inicio.AddMonths(6),
        AppMatriculaPlano.Anual => inicio.AddMonths(12),
        _ => inicio
    };

    private void RecalcularDataFim()
    {
        Matricula.DataFim = CalcularDataFim(Matricula.Plano, Matricula.DataInicio);
        NotificarVigencia();
    }

    private void NotificarVigencia()
    {
        OnPropertyChanged(nameof(PlanoSelecionado));
        OnPropertyChanged(nameof(DataInicioSelecionada));
        OnPropertyChanged(nameof(DuracaoPlano));
        OnPropertyChanged(nameof(VigenciaResumo));
        OnPropertyChanged(nameof(Matricula));
    }

    private void NotificarEstadoVisual()
    {
        OnPropertyChanged(nameof(HasAlunoVinculado));
        OnPropertyChanged(nameof(HasNoAlunoVinculado));
        OnPropertyChanged(nameof(IsMenor16));
        OnPropertyChanged(nameof(HasRestricoes));
        OnPropertyChanged(nameof(ExigeLaudo));
        OnPropertyChanged(nameof(HasLaudo));
        OnPropertyChanged(nameof(LaudoPendente));
        OnPropertyChanged(nameof(LaudoOpcional));
    }

    private void MontarRestricoesMulti()
    {
        RestricoesMulti.Clear();

        foreach (var restricao in Enum.GetValues<AppMatriculaRestricoes>())
        {
            if (restricao == AppMatriculaRestricoes.None) continue;

            var item = new RestricaoSelectable
            {
                Value = restricao,
                IsSelected = Matricula.RestricoesMedicas.HasFlag(restricao)
            };

            item.PropertyChanged += RestricaoAlterada;
            RestricoesMulti.Add(item);
        }
    }

    private void RestricaoAlterada(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(RestricaoSelectable.IsSelected)) return;
        if (_sincronizando) return;

        RecalcularFlagsDasRestricoes();
    }

    private void RecalcularFlagsDasRestricoes()
    {
        var flags = AppMatriculaRestricoes.None;

        foreach (var item in RestricoesMulti)
        {
            if (item.IsSelected) flags |= item.Value;
        }

        Matricula.RestricoesMedicas = flags;
        OnPropertyChanged(nameof(Matricula));
        NotificarEstadoVisual();
    }

    private void SincronizarSelecaoPelasFlags()
    {
        _sincronizando = true;

        try
        {
            foreach (var item in RestricoesMulti)
            {
                item.IsSelected = Matricula.RestricoesMedicas.HasFlag(item.Value);
            }
        }
        finally
        {
            _sincronizando = false;
        }

        NotificarEstadoVisual();
    }

    [RelayCommand]
    private static async Task CancelAsync()
    {
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    public async Task LoadMatriculaAsync()
    {
        if (MatriculaId <= 0) return;

        try
        {
            IsBusy = true;
            using var cts = new CancellationTokenSource(Timeout);
            var matriculaData = await _matriculaService.ObterPorIdAsync(MatriculaId, cts.Token);

            if (matriculaData != null)
            {
                matriculaData.AlunoMatricula ??= CriarAlunoVazio();
                matriculaData.AlunoMatricula.Endereco ??= CriarEnderecoVazio();
                Matricula = matriculaData;
            }
        }
        catch (OperationCanceledException)
        {
            await Shell.Current.DisplayAlertAsync("Tempo Esgotado", "O carregamento da matrícula expirou. Verifique a conexão.", "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Erro", $"Erro ao carregar matrícula: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task SearchByCpfAsync()
    {
        var cpfDigitado = Matricula.AlunoMatricula?.Cpf;

        if (string.IsNullOrWhiteSpace(cpfDigitado))
        {
            await Shell.Current.DisplayAlertAsync("Aviso", "Informe o CPF do aluno para realizar a busca.", "OK");
            return;
        }

        var cpfLimpo = new string([.. cpfDigitado.Where(char.IsDigit)]);

        if (cpfLimpo.Length != 11)
        {
            await Shell.Current.DisplayAlertAsync("Validação", "O CPF deve conter exatamente 11 dígitos numéricos.", "OK");
            return;
        }

        try
        {
            IsBusy = true;
            using var cts = new CancellationTokenSource(Timeout);
            var alunoData = await _alunoService.ObterPorCpfAsync(cpfLimpo, cts.Token);

            if (alunoData != null)
            {
                alunoData.Endereco ??= CriarEnderecoVazio();
                Matricula.AlunoMatricula = alunoData;
                OnPropertyChanged(nameof(Matricula));
                NotificarEstadoVisual();
            }
            else
            {
                await Shell.Current.DisplayAlertAsync("Aviso", "Aluno não encontrado. Cadastre o aluno antes de matriculá-lo.", "OK");
            }
        }
        catch (OperationCanceledException)
        {
            await Shell.Current.DisplayAlertAsync("Tempo Esgotado", "A busca pelo CPF expirou. Verifique a conexão.", "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Erro", $"Erro ao buscar o aluno: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task SelecionarLaudoAsync()
    {
        try
        {
            string escolha = await Shell.Current.DisplayActionSheetAsync("Origem do laudo médico", "Cancelar", "Remover Laudo", "Galeria", "Câmera");

            if (escolha == "Remover Laudo")
            {
                RemoverLaudo();
                return;
            }

            FileResult? result = null;

            if (escolha == "Galeria")
            {
                result = await FilePicker.Default.PickAsync(new PickOptions
                {
                    PickerTitle = "Selecione o laudo médico",
                    FileTypes = FilePickerFileType.Images
                });
            }
            else if (escolha == "Câmera")
            {
                if (MediaPicker.Default.IsCaptureSupported)
                {
                    result = await MediaPicker.Default.CapturePhotoAsync();
                }
                else
                {
                    await Shell.Current.DisplayAlertAsync("Erro", "Captura de foto não suportada neste dispositivo.", "OK");
                    return;
                }
            }

            if (result != null)
            {
                using var stream = await result.OpenReadAsync();
                using var ms = new MemoryStream();
                await stream.CopyToAsync(ms);

                Matricula.LaudoMedico = new ArquivoDto { Conteudo = ms.ToArray() };
                OnPropertyChanged(nameof(Matricula));
                NotificarEstadoVisual();
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Erro", $"Erro ao selecionar o laudo: {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    public void RemoverLaudo()
    {
        Matricula.LaudoMedico = null;
        OnPropertyChanged(nameof(Matricula));
        NotificarEstadoVisual();
    }

    [RelayCommand]
    public async Task SaveMatriculaAsync()
    {
        if (IsBusy) return;

        RecalcularFlagsDasRestricoes();
        RecalcularDataFim();

        if (!await ValidateMatriculaAsync(Matricula)) return;

        try
        {
            IsBusy = true;
            using var cts = new CancellationTokenSource(Timeout);

            AlunoDto? alunoData = null;

            if (Matricula.AlunoMatricula?.Id > 0)
            {
                alunoData = await _alunoService.ObterPorIdAsync(Matricula.AlunoMatricula.Id, cts.Token);
            }
            else if (!string.IsNullOrWhiteSpace(Matricula.AlunoMatricula?.Cpf))
            {
                var cpfLimpo = new string([.. Matricula.AlunoMatricula.Cpf.Where(char.IsDigit)]);
                alunoData = await _alunoService.ObterPorCpfAsync(cpfLimpo, cts.Token);
            }

            if (alunoData == null)
            {
                await Shell.Current.DisplayAlertAsync("Aluno Obrigatório", "O aluno informado não foi encontrado. Busque um aluno válido pelo CPF.", "OK");
                return;
            }

            alunoData.Endereco ??= CriarEnderecoVazio();
            Matricula.AlunoMatricula = alunoData;
            NotificarEstadoVisual();

            if (!await ValidateMatriculaAsync(Matricula)) return;

            Matricula.Objetivo = Matricula.Objetivo.Trim();
            Matricula.ObservacoesRestricoes = string.IsNullOrWhiteSpace(Matricula.ObservacoesRestricoes)
                ? null
                : Matricula.ObservacoesRestricoes.Trim();

            if (IsEditMode)
            {
                await _matriculaService.AtualizarAsync(Matricula, cts.Token);
                await Shell.Current.DisplayAlertAsync("Sucesso", "Matrícula atualizada com sucesso!", "OK");
            }
            else
            {
                await _matriculaService.AdicionarAsync(Matricula, cts.Token);
                await Shell.Current.DisplayAlertAsync("Sucesso", "Matrícula criada com sucesso!", "OK");
            }

            await Shell.Current.GoToAsync("..");
        }
        catch (OperationCanceledException)
        {
            await Shell.Current.DisplayAlertAsync("Tempo Esgotado", "A gravação da matrícula expirou. Verifique a conexão.", "OK");
        }
        catch (InvalidOperationException ex)
        {
            await Shell.Current.DisplayAlertAsync("Regra de Negócio", ex.Message, "OK");
        }
        catch (ArgumentException ex)
        {
            await Shell.Current.DisplayAlertAsync("Validação", ex.Message, "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Erro", $"Erro ao salvar matrícula: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static async Task<bool> ValidateMatriculaAsync(MatriculaDto matricula)
    {
        var errors = new List<string>();

        if (matricula.AlunoMatricula == null || (matricula.AlunoMatricula.Id <= 0 && string.IsNullOrWhiteSpace(matricula.AlunoMatricula.Cpf)))
        {
            errors.Add("• Aluno é obrigatório. Informe o CPF e clique em Buscar.");
        }

        if (matricula.DataInicio == default)
        {
            errors.Add("• Data de início é obrigatória.");
        }

        if (matricula.DataFim <= matricula.DataInicio)
        {
            errors.Add("• A data de término precisa ser posterior à data de início.");
        }

        if (string.IsNullOrWhiteSpace(matricula.Objetivo))
        {
            errors.Add("• Objetivo da matrícula é obrigatório.");
        }

        if (!Enum.IsDefined(matricula.Plano))
        {
            errors.Add("• Plano da matrícula é inválido.");
        }

        bool menorDe16 = matricula.AlunoMatricula != null
            && matricula.AlunoMatricula.DataNascimento != default
            && matricula.AlunoMatricula.DataNascimento > DateOnly.FromDateTime(DateTime.Today.AddYears(-16));

        bool possuiLaudo = matricula.LaudoMedico?.Conteudo != null && matricula.LaudoMedico.Conteudo.Length > 0;

        if (menorDe16 && !possuiLaudo)
        {
            errors.Add("• Alunos de 12 a 16 anos devem apresentar laudo médico que autorize a prática de atividades físicas.");
        }

        if (matricula.RestricoesMedicas != AppMatriculaRestricoes.None && !possuiLaudo)
        {
            errors.Add("• Alunos com restrições de saúde registradas devem apresentar parecer médico autorizando a prática de atividades físicas.");
        }

        if (errors.Count > 0)
        {
            var mensagem = "Por favor, corrija os seguintes campos:\n\n" + string.Join("\n", errors);
            await Shell.Current.DisplayAlertAsync("Erros de Validação", mensagem, "OK");
            return false;
        }

        return true;
    }
}
