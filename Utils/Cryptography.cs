using System.Security.Cryptography;
using System.Text;

namespace Utils;

public static class Cryptography
{
    /// <summary>
    /// Метод для хэширования пароля
    /// </summary>
    /// <param name="password">Пароль</param>
    /// <returns>Хэш пароля</returns>
    public static string? HashPassword(string? password)
    {
        byte[] passwordBytes = Encoding.UTF8.GetBytes(password ?? string.Empty);
        using var hash = new SHA256CryptoServiceProvider();
        byte[] hashBytes = hash.ComputeHash(passwordBytes);
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
    }
    /// <summary>
    /// Метод для добавления соли после хэширования пароля
    /// </summary>
    /// <param name="hashedPassword">Хэш пароля</param>
    /// <param name="salt">Соль</param>
    /// <returns>Хэш пароля с солью</returns>
    public static string ApplySalt(string? hashedPassword, string? salt)
    {
        byte[] hashedPasswordBytes = Encoding.UTF8.GetBytes(hashedPassword ?? string.Empty);
        byte[] saltBytes = Encoding.UTF8.GetBytes(salt ?? string.Empty);
        
        // Создаем массив для перемешанных байтов
        byte[] saltedPassword = new byte[saltBytes.Length + hashedPasswordBytes.Length];
        int minLength = Math.Min(saltBytes.Length, hashedPasswordBytes.Length);
        
        // Чередуем байты из хэшированного пароля и соли
        int i = 0;
        for (; i < minLength; i++)
        {
            saltedPassword[2 * i] = hashedPasswordBytes[i];
            saltedPassword[2 * i + 1] = saltBytes[i];
        }
        
        // Добавляем оставшиеся байты, если длины пароля и соли разные
        if (hashedPasswordBytes.Length > saltBytes.Length)
        {
            Array.Copy(hashedPasswordBytes, i, saltedPassword, 2 * i, hashedPasswordBytes.Length - i);
        }
        else if (saltBytes.Length > hashedPasswordBytes.Length)
        {
            Array.Copy(saltBytes, i, saltedPassword, 2 * i, saltBytes.Length - i);
        }

        // Возвращаем результат в виде строки
        return BitConverter.ToString(saltedPassword).Replace("-", "").ToLower();
    }
}