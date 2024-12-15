using DbProvider.Helpers;
using DbProvider.Models;
using Npgsql;


namespace DbProvider;

public class UserRepositoryService(DbConfig dbConfig)
{
    private readonly NpgsqlConnection? _connection = dbConfig.dbConnection;
    private readonly string? _dbSchema = dbConfig.dbSchema;

    public UserModel? GetUser(string? login)
    {
        UserModel? user = null;
        NpgsqlConnection? connection = null;

        try
        {
            connection = _connection;
            connection?.Open();
            string query = $@"SET search_path = {_dbSchema}; 
                          SELECT username, uid, password, salt FROM users WHERE login=@login";

            using (var command = new NpgsqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@login", login ?? (object)DBNull.Value);
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        user = new UserModel
                        {
                            UserName = reader.GetString(0),
                            Uid = reader.GetGuid(1),
                            Password = reader.GetString(2),
                            SaltPassword = reader.GetString(3),
                        };
                    }
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
        finally
        {
            if (connection != null && connection.State == System.Data.ConnectionState.Open)
            {
                connection.Close();
            }
        }

        return user;
    }

    public bool AddSessionUser(UserModel? user)
    {
        NpgsqlConnection? connection = null;
        try
        {
            connection = _connection;
            connection?.Open();

            string query = $@"SET search_path = {_dbSchema}; 
                          CALL ""AddSession"" (@userUid, @refreshJtiToken, @expiration)";

            using (var command = new NpgsqlCommand(query, connection))
            {
                if (user != null)
                {
                    command.Parameters.AddWithValue("@userUid", user.Uid);
                    command.Parameters.AddWithValue("@refreshJtiToken", user.JwtTokens.RefreshTokenJti!);
                    command.Parameters.AddWithValue("@expiration", user.JwtTokens.RefreshTokenExpiration);
                }

                command.ExecuteNonQuery();
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return false;
        }
        finally
        {
            if (connection != null && connection.State == System.Data.ConnectionState.Open)
            {
                connection.Close();
            }
        }

        return true;
    }

    public bool UpdateSessionUser(UserModel? user, Guid oldRefreshTokenJti)
    {
        NpgsqlConnection? connection = null;
        try
        {
            connection = _connection;
            connection?.Open();

            string query = $@"SET search_path = {_dbSchema}; 
                          CALL ""UpdateSession"" (@userUid, @refreshJtiToken, @expiration, @oldRefreshJtiToken)";
            using (var command = new NpgsqlCommand(query, connection))
            {
                if (user != null)
                {
                    command.Parameters.AddWithValue("@userUid", user.Uid);
                    command.Parameters.AddWithValue("@refreshJtiToken", user.JwtTokens.RefreshTokenJti!);
                    command.Parameters.AddWithValue("@expiration", user.JwtTokens.RefreshTokenExpiration);
                    command.Parameters.AddWithValue("@oldRefreshJtiToken", oldRefreshTokenJti);
                }

                command.ExecuteNonQuery();
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return false;
        }
        finally
        {
            if (connection != null && connection.State == System.Data.ConnectionState.Open)
            {
                connection.Close();
            }
        }

        return true;
    }

    public bool DeleteSessionUser(Guid uidUser, Guid refreshTokenJti)
    {
        NpgsqlConnection? connection = null;
        try
        {
            connection = _connection;
            connection?.Open();

            string query = $@"SET search_path = {_dbSchema}; 
                          CALL ""DeleteSession"" (@userUid, @refreshJtiToken)";
            using (var command = new NpgsqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@userUid", uidUser);
                command.Parameters.AddWithValue("@refreshJtiToken", refreshTokenJti);
                command.ExecuteNonQuery();
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return false;
        }
        finally
        {
            if (connection != null && connection.State == System.Data.ConnectionState.Open)
            {
                connection.Close();
            }
        }

        return true;
    }

    public SessionModel? GetSessionUser(Guid refreshTokenJti)
    {
        SessionModel? session = null;
        NpgsqlConnection? connection = null;
        try
        {
            connection = _connection;
            connection?.Open();
            string query = $@"SET search_path = {_dbSchema}; 
                          SELECT refresh_token, expires_in FROM sessions 
                          WHERE refresh_token=@token";
            using (var command = new NpgsqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@token", refreshTokenJti);
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        session = new SessionModel
                        {
                            JwtTokens =
                            {
                                RefreshTokenJti = reader.GetGuid(0),
                                RefreshTokenExpiration = reader.GetDateTime(1)
                            }
                        };
                    }
                }
            }

            return session;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
        finally
        {
            if (connection != null && connection.State == System.Data.ConnectionState.Open)
            {
                connection.Close();
            }
        }

        return session;
    }
}