// Alan Medeiros
using AcademiaDoZe.Infrastructure.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;

namespace AcademiaDoZe.Infrastructure.Repositories;

public abstract class BaseRepository
{
    protected string ConnectionString { get; }

    protected DatabaseType DatabaseType { get; }

    protected BaseRepository(string connectionString, DatabaseType databaseType)
    {
        ConnectionString = connectionString;
        DatabaseType = databaseType;
    }

    protected async Task<DbCommand> CreateCommandAsync(
        string commandText,
        CancellationToken cancellationToken = default)
    {
        var connection = await DbProvider.CreateOpenConnectionAsync(
            ConnectionString,
            DatabaseType,
            cancellationToken);

        var command = connection.CreateCommand();
        command.CommandText = commandText;

        return new ConexaoDeComando(command, connection);
    }

    protected string FormatInsertQuery(string insertQuery) =>
        $"{insertQuery.TrimEnd().TrimEnd(';')}; {DbProvider.GetLastInsertIdQuery(DatabaseType)}";

    protected string GetCurrentDateFunction() =>
        DbProvider.GetCurrentDateFunction(DatabaseType);

    protected string GetDateAddDaysExpression(string dateExpression, string daysExpression) =>
        DbProvider.GetDateAddDaysExpression(DatabaseType, dateExpression, daysExpression);

    private sealed class ConexaoDeComando : DbCommand
    {
        private readonly DbCommand _command;
        private readonly DbConnection _connection;

        public ConexaoDeComando(DbCommand command, DbConnection connection)
        {
            _command = command;
            _connection = connection;
        }

        [AllowNull]
        public override string CommandText
        {
            get => _command.CommandText;
            set => _command.CommandText = value;
        }

        public override int CommandTimeout
        {
            get => _command.CommandTimeout;
            set => _command.CommandTimeout = value;
        }

        public override System.Data.CommandType CommandType
        {
            get => _command.CommandType;
            set => _command.CommandType = value;
        }

        public override bool DesignTimeVisible
        {
            get => _command.DesignTimeVisible;
            set => _command.DesignTimeVisible = value;
        }

        public override System.Data.UpdateRowSource UpdatedRowSource
        {
            get => _command.UpdatedRowSource;
            set => _command.UpdatedRowSource = value;
        }

        protected override DbConnection? DbConnection
        {
            get => _command.Connection;
            set => _command.Connection = value;
        }

        protected override DbParameterCollection DbParameterCollection => _command.Parameters;

        protected override DbTransaction? DbTransaction
        {
            get => _command.Transaction;
            set => _command.Transaction = value;
        }

        public override void Cancel() => _command.Cancel();

        public override int ExecuteNonQuery() => _command.ExecuteNonQuery();

        public override object? ExecuteScalar() => _command.ExecuteScalar();

        public override void Prepare() => _command.Prepare();

        protected override DbParameter CreateDbParameter() => _command.CreateParameter();

        protected override DbDataReader ExecuteDbDataReader(System.Data.CommandBehavior behavior) =>
            _command.ExecuteReader(behavior);

        protected override Task<DbDataReader> ExecuteDbDataReaderAsync(
            System.Data.CommandBehavior behavior,
            CancellationToken cancellationToken) =>
            _command.ExecuteReaderAsync(behavior, cancellationToken);

        public override Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken) =>
            _command.ExecuteNonQueryAsync(cancellationToken);

        public override Task<object?> ExecuteScalarAsync(CancellationToken cancellationToken) =>
            _command.ExecuteScalarAsync(cancellationToken);

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _command.Dispose();
                _connection.Dispose();
            }

            base.Dispose(disposing);
        }

        public override async ValueTask DisposeAsync()
        {
            await _command.DisposeAsync();
            await _connection.DisposeAsync();

            GC.SuppressFinalize(this);
        }
    }
}
