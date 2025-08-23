namespace TokenyRefresh.Utils;

using Microsoft.Data.Sqlite;

public class SqliteDbService
{
    private readonly string _connectionString;

    public SqliteDbService(string connectionString)
    {
        _connectionString = connectionString;
        InitializeDatabase(); // Llama a la inicialización al crear el servicio
    }

    private void InitializeDatabase()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS Users (
                Id INTEGER PRIMARY KEY,
                Username TEXT UNIQUE NOT NULL,
                PasswordHash TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS RefreshTokens (
                Token TEXT PRIMARY KEY,
                UserId INTEGER NOT NULL,
                ExpiryDate TEXT NOT NULL,
                FOREIGN KEY (UserId) REFERENCES Users(Id)
            );";

        command.ExecuteNonQuery();
    }

    public SqliteConnection GetConnection()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        return connection;
    }
}
