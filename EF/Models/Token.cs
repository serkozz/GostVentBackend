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
    public TimeSpan Expires { get; set; }
    public long UserId { get; set; }
    public User User { get; set; }
}