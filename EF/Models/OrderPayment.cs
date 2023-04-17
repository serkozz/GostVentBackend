using System.ComponentModel.DataAnnotations;
using Types.Interfaces;

namespace EF.Models;

public partial class OrderPayment
{
    [Key]
    public long Id { get; set; }
    public Guid PaymentId { get; set; }
    public long OrderId { get; set; }
    public Order Order { get; set; }
}
