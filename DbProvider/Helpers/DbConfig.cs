using System.Text;
using Npgsql;

namespace DbProvider.Helpers;

public class DbConfig
{
    private readonly Dictionary<string, string> ConfigParameters = [];
    public NpgsqlConnection? dbConnection;
    public string? dbSchema;

    public DbConfig()
    {
        _ = ReadConfig() ? true : throw new Exception("Error: no config file");
        (dbConnection, dbSchema) = InitDbConnection();
    }
    private bool ReadConfig()
    {
        string configFilePath = $@"{AppContext.BaseDirectory}dbConfig.conf";

        if (!File.Exists(configFilePath))
            return false;

        string[] propertyText;
        string proprtyName, propertyValue;
        char[] _separator = ['='];

        ConfigParameters.Clear();

        foreach (string configString in File.ReadAllLines(configFilePath, Encoding.UTF8))
        {
            if (string.IsNullOrEmpty(configString) || configString.Trim()[0] == '#')
                continue;

            propertyText = configString.Split(_separator, StringSplitOptions.RemoveEmptyEntries);

            if (propertyText.Length < 2)
                continue;

            proprtyName = propertyText[0].Trim();
            propertyValue = propertyText[1].Trim();

            if (string.IsNullOrEmpty(proprtyName) || 
                string.IsNullOrWhiteSpace(proprtyName) || 
                string.IsNullOrEmpty(propertyValue) || 
                string.IsNullOrWhiteSpace(propertyValue))
                continue;

            _ = ConfigParameters.TryAdd(proprtyName, propertyValue);
        }
        return ConfigParameters.Count > 0;
    }
    private (NpgsqlConnection?, string?) InitDbConnection()
    {
        NpgsqlConnection? _connectionString = null;
        string? _dbSchema = null;
        try
        {
            if (ConfigParameters.TryGetValue("db_address", out string? dbAddress) &&
                ConfigParameters.TryGetValue("db_port", out string? dbPort) &&
                ConfigParameters.TryGetValue("db_user", out string? dbUsername) &&
                ConfigParameters.TryGetValue("db_password", out string? dbPassword) &&
                ConfigParameters.TryGetValue("db_name", out string? dbName) &&
                ConfigParameters.TryGetValue("db_schema", out string? dbSchema))
            {
                _connectionString = new($"Server={dbAddress}; Port={dbPort}; User Id={dbUsername}; Password={dbPassword}; Database={dbName}; Timeout=30;");
                _dbSchema = new($"\"{dbSchema}\"");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Detailed description of the error: {ex}");
            throw;
        }
        return (_connectionString, _dbSchema);
    }
}