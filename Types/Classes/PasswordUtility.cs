using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Types.Classes;

public static class PasswordUtility
{
    private static RandomNumberGenerator provider = RandomNumberGenerator.Create();
    private static readonly int SaltSize = 16;
    private static readonly int HashSize = 20;
    private static readonly int Iterations = 10000;

    public static string HashPassword(string password)
    {
        byte[] salt;
        provider.GetBytes(salt = new byte[SaltSize]);
        var key = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
        var hash = key.GetBytes(HashSize);

        var hashBytes = new byte[SaltSize + HashSize];
        Array.Copy(salt, 0, hashBytes, 0, SaltSize);
        Array.Copy(hash, 0, hashBytes, SaltSize, HashSize);

        var base64String = Convert.ToBase64String(hashBytes);
        return base64String;
    }

    public static bool VerifyPassword(string password, string base64String)
    {
        var hashBytes = Convert.FromBase64String(base64String);

        var salt = new byte[SaltSize];
        Array.Copy(hashBytes, 0, salt, 0, SaltSize);

        var key = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
        byte[] hash = key.GetBytes(HashSize);

        for (var i = 0; i < HashSize; i++)
        {
            if (hashBytes[i + SaltSize] != hash[i])
                return false;
        }

        return true;
    }

    /// <summary>
    /// Проводит проверку на силу пароля
    /// </summary>
    /// <param name="password">Пароль</param>
    /// <returns>Строки с непройдеными проверками (длина,содержание)</returns>
    public static string CheckPasswordStrength(string password)
    {
        StringBuilder sb = new StringBuilder();

        if (password.Length < 8)
            sb.AppendLine("Пароль должен быть не менее 8 символов в длину");
        if (!(Regex.IsMatch(password, "[a-z]", RegexOptions.IgnoreCase) || !Regex.IsMatch(password, "[а-я]", RegexOptions.IgnoreCase)))
            sb.AppendLine("Пароль должен содержать буквы");
        if (!Regex.IsMatch(password, "[0-9]"))
            sb.AppendLine("Пароль должен содержать числа");
        if (!Regex.IsMatch(password, "[!@#$%&*()_+=|<>?{}\\[\\]~-]"))
            sb.AppendLine("Пароль должен содержать специальные символы");
        return sb.ToString();
    }
}