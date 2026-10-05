// Alan Medeiros
using AcademiaDoZe.Application.Enums;
using AcademiaDoZe.Presentation.AppMaui.Message;
using CommunityToolkit.Mvvm.Messaging;

namespace AcademiaDoZe.Presentation.AppMaui.Views;

public partial class ConfigPage : ContentPage
{
    public ConfigPage()
    {
        InitializeComponent();
        CarregarTema();
        CarregarBanco();
    }

    #region Tema

    private void CarregarTema()
    {
        // uso de expressão switch para carregar o índice selecionado
        TemaPicker.SelectedIndex = Preferences.Get("Tema", "system") switch { "light" => 0, "dark" => 1, _ => 2, };
    }

    private async void OnSalvarTemaClicked(object? sender, EventArgs e)
    {
        string selectedTheme = TemaPicker.SelectedIndex switch { 0 => "light", 1 => "dark", _ => "system" };
        Preferences.Set("Tema", selectedTheme);

        // Disparar mensagem para uso na recarga dinâmica
        WeakReferenceMessenger.Default.Send(new TemaPreferencesUpdatedMessage("TemaAlterado"));

        await DisplayAlertAsync("Sucesso", "Dados salvos com sucesso!", "OK");

        // Navegar para dashboard
        await Shell.Current.GoToAsync("//dashboard");
    }

    #endregion

    #region Banco de Dados

    private void CarregarBanco()
    {
        DatabaseTypePicker.Items.Clear();
        foreach (var tipo in Enum.GetValues<AppDatabaseType>())
        {
            DatabaseTypePicker.Items.Add(tipo.ToString());
        }

        var bancoAtual = Preferences.Get("DatabaseType", AppDatabaseType.Sqlite.ToString());
        DatabaseTypePicker.SelectedItem = bancoAtual;

        AtualizarInterfacePorTipoBanco();
    }

    private void OnDatabaseTypeChanged(object? sender, EventArgs? e)
    {
        AtualizarInterfacePorTipoBanco();
    }

    private void AtualizarInterfacePorTipoBanco()
    {
        if (DatabaseTypePicker.SelectedItem is not string selectedTypeStr ||
            !Enum.TryParse<AppDatabaseType>(selectedTypeStr, out var selectedType))
        {
            return;
        }

        switch (selectedType)
        {
            case AppDatabaseType.Sqlite:
                SqliteInfoCard.IsVisible = true;
                SqliteContainer.IsVisible = true;
                ServidorBancoGrid.IsVisible = false;
                CredenciaisGrid.IsVisible = false;

                ComplementoLabel.Text = "Complemento (ex: Default Timeout=5;)";
                ComplementoEntry.Placeholder = "Default Timeout=5;";

                SqliteCaminhoEntry.Text = Preferences.Get("Sqlite_Caminho", ConfiguracaoPadraoSqlite());
                ComplementoEntry.Text = Preferences.Get("Sqlite_Complemento", "Default Timeout=5;");
                break;

            case AppDatabaseType.SqlServer:
                SqliteInfoCard.IsVisible = false;
                SqliteContainer.IsVisible = false;
                ServidorBancoGrid.IsVisible = true;
                CredenciaisGrid.IsVisible = true;

                ServidorEntry.Placeholder = "Ex: 10.0.2.2 ou localhost";
                BancoEntry.Placeholder = "Ex: db_academia_do_ze";
                UsuarioEntry.Placeholder = "Ex: sa";
                ComplementoLabel.Text = "Complemento (SSL / Timeout / Criptografia)";
                ComplementoEntry.Placeholder = "TrustServerCertificate=True;Encrypt=True;Connect Timeout=5;Connection Timeout=5;";

                ServidorEntry.Text = Preferences.Get("SqlServer_Servidor", "10.0.2.2");
                BancoEntry.Text = Preferences.Get("SqlServer_Banco", "db_academia_do_ze");
                UsuarioEntry.Text = Preferences.Get("SqlServer_Usuario", "sa");
                SenhaEntry.Text = Preferences.Get("SqlServer_Senha", string.Empty);
                ComplementoEntry.Text = Preferences.Get("SqlServer_Complemento", "TrustServerCertificate=True;Encrypt=True;Connect Timeout=5;Connection Timeout=5;");
                break;

            case AppDatabaseType.MySql:
                SqliteInfoCard.IsVisible = false;
                SqliteContainer.IsVisible = false;
                ServidorBancoGrid.IsVisible = true;
                CredenciaisGrid.IsVisible = true;

                ServidorEntry.Placeholder = "Ex: 10.0.2.2 ou localhost";
                BancoEntry.Placeholder = "Ex: db_academia_do_ze";
                UsuarioEntry.Placeholder = "Ex: root";
                ComplementoLabel.Text = "Complemento (Porta / Timeout)";
                ComplementoEntry.Placeholder = "Connection Timeout=5;Default Command Timeout=30;";

                ServidorEntry.Text = Preferences.Get("MySql_Servidor", "10.0.2.2");
                BancoEntry.Text = Preferences.Get("MySql_Banco", "db_academia_do_ze");
                UsuarioEntry.Text = Preferences.Get("MySql_Usuario", "root");
                SenhaEntry.Text = Preferences.Get("MySql_Senha", string.Empty);
                ComplementoEntry.Text = Preferences.Get("MySql_Complemento", "Connection Timeout=5;Default Command Timeout=30;");
                break;
        }
    }

    // no desktop o caminho completo é editável; nos demais basta o nome do arquivo,
    // que é procurado na pasta de dados do aplicativo
    private static string ConfiguracaoPadraoSqlite()
    {
        return DeviceInfo.Platform == DevicePlatform.WinUI
            ? Path.Combine("C:", "DEV", "AcademiaDoZe", "db_academia_do_ze.db")
            : "db_academia_do_ze.db";
    }

    private async void OnSalvarBdClicked(object? sender, EventArgs e)
    {
        if (DatabaseTypePicker.SelectedItem is not string selectedTypeStr ||
            !Enum.TryParse<AppDatabaseType>(selectedTypeStr, out var selectedType))
        {
            await DisplayAlertAsync("Aviso", "Selecione um tipo de banco de dados válido.", "OK");
            return;
        }

        if (selectedType == AppDatabaseType.Sqlite)
        {
            if (string.IsNullOrWhiteSpace(SqliteCaminhoEntry.Text))
            {
                await DisplayAlertAsync("Validação", "Informe o caminho do arquivo do banco SQLite.", "OK");
                return;
            }

            Preferences.Set("Sqlite_Caminho", SqliteCaminhoEntry.Text.Trim());
            Preferences.Set("Sqlite_Complemento", ComplementoEntry.Text?.Trim() ?? string.Empty);
        }
        else
        {
            if (string.IsNullOrWhiteSpace(ServidorEntry.Text))
            {
                await DisplayAlertAsync("Validação", "Informe o servidor do banco de dados.", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(BancoEntry.Text))
            {
                await DisplayAlertAsync("Validação", "Informe o nome do banco de dados.", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(UsuarioEntry.Text))
            {
                await DisplayAlertAsync("Validação", "Informe o usuário do banco de dados.", "OK");
                return;
            }

            var prefixo = selectedType == AppDatabaseType.SqlServer ? "SqlServer" : "MySql";
            Preferences.Set($"{prefixo}_Servidor", ServidorEntry.Text.Trim());
            Preferences.Set($"{prefixo}_Banco", BancoEntry.Text.Trim());
            Preferences.Set($"{prefixo}_Usuario", UsuarioEntry.Text.Trim());
            Preferences.Set($"{prefixo}_Senha", SenhaEntry.Text ?? string.Empty);
            Preferences.Set($"{prefixo}_Complemento", ComplementoEntry.Text?.Trim() ?? string.Empty);
        }

        Preferences.Set("DatabaseType", selectedType.ToString());

        // Disparar a mensagem para recarga dinâmica (processada pelo ConfigurationHelper)
        WeakReferenceMessenger.Default.Send(new BancoPreferencesUpdatedMessage("BancoAlterado"));

        await DisplayAlertAsync("Sucesso", $"Configurações do banco de dados ({selectedType}) salvas com sucesso!", "OK");

        // Navegar para dashboard
        await Shell.Current.GoToAsync("//dashboard");
    }

    #endregion

    private async void OnCancelarClicked(object? sender, EventArgs e)
    {
        // retornar para dashboard
        await Shell.Current.GoToAsync("//dashboard");
    }

    // Ao fechar a página, chama WeakReferenceMessenger.Default.UnregisterAll(this); para evitar vazamentos de memória - memory leaks
    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        // Desinscreve o mensageiro para evitar memory leaks
        WeakReferenceMessenger.Default.UnregisterAll(this);
    }
}
