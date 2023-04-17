using System.ComponentModel.DataAnnotations;
using Types.Interfaces;

namespace EF.Models;

public partial class OrderRating
{
    [Key]
    public long Id { get; set; }
    public int Rating { get; set; }
    public long OrderId { get; set; }
    public Order Order { get; set; }
}
