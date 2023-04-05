using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Newtonsoft.Json;
using Types.Enums;
using Types.Interfaces;

namespace EF.Models;

public partial class Order : IDynamicallySettable, IDynamicallyUpdatable<Order>
{
    [Key]
    public long Id { get; set; }
    public string Name { get; set; }
    public ProductType ProductType { get; set; }
    public DateOnly CreationDate { get; set; }
    public int LeadDays { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Created;
    public int Price { get; set; }
    public long ClientId { get; set; }
    [Required]
    public User? Client { get; set; }

    public Order() { }

    public Order(string[] fields)
    {
        PropertySetLooping(fields);
    }

    public void PropertySetLooping(string[] fields)
    {
        PropertyInfo[] properties = this.GetType().GetProperties();

        for (var i = 0; i < properties.Length; i++)
        {
            if (!properties[i].CanWrite)
                throw new IOException($"{properties[i].Name} cant be writen!!!");

            switch (i)
            {
                case 0:
                    Int64.TryParse(fields[i], out long int64);
                    properties[i].SetValue(this, int64);
                    break;
                case 2:
                    Enum.TryParse(fields[i], out ProductType productType);
                    properties[i].SetValue(this, productType);
                    break;
                case 3:
                    DateOnly.TryParse(fields[i], out DateOnly date);
                    properties[i].SetValue(this, date);
                    break;
                case 4:
                    Int32.TryParse(fields[i], out int LeadDays);
                    properties[i].SetValue(this, LeadDays);
                    break;
                case 5:
                    Enum.TryParse(fields[i], out OrderStatus orderStatus);
                    properties[i].SetValue(this, orderStatus);
                    break;
                case 6:
                    Int32.TryParse(fields[i], out int price);
                    properties[i].SetValue(this, price);
                    break;
                case 7:
                    Int64.TryParse(fields[i], out long clientId);
                    properties[i].SetValue(this, clientId);
                    break;
                case 8:
                    break;
                default:
                    properties[i].SetValue(this, fields[i]);
                    break;
            }
        }
    }

    public void UpdateSelfDynamically(Order other)
    {
        PropertyInfo[] props = this.GetType().GetProperties();

        for (var i = 0; i < props.Length; i++)
        {
            props[i].SetValue(this, props[i].GetValue(other));
        }
    }

    public override string ToString()
    {
        return JsonConvert.SerializeObject(this);
    }
}