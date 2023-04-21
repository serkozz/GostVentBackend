using System.ComponentModel.DataAnnotations;
using Types.Enums;

namespace EF.Models;

public partial class Token
{
    [Key]
    public long Id { get; set; }
    public TokenType Type { get; set; }
    public string Value { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public long UserId { get; set; }
    public User User { get; set; }

    [Obsolete("Не использовать, нужен для создания модели в EF")]
    public Token() { }

    public Token(User user, TokenType tokenType)
    {
        User = user;
        UserId = user.Id;
        Type = tokenType;

        if (tokenType == TokenType.RestorePassword)
            Value = CreateCode(6);
        if (tokenType == TokenType.DeleteAccount)
            Value = CreateMD5(DateTime.Now + user.Email);

        CreatedAt = DateTime.Now;
        ExpiresAt = CreatedAt + new TimeSpan(0, 30, 0);
    }

    private string CreateMD5(string input)
    {
        // Use input string to calculate MD5 hash
        using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
        {
            byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
            byte[] hashBytes = md5.ComputeHash(inputBytes);
            return Convert.ToHexString(hashBytes);
        }
    }

    private string CreateCode(int length)
    {
        const string chars = "0123456789";
        // const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[Random.Shared.Next(s.Length)]).ToArray());
    }
}