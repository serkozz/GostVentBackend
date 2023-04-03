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
    public DateTime CreationDate { get; set; }
    public int LeadDays { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Created;
    public int Price { get; set; }
    public User? Client { get; set; }

    public Order() { }

    public Order(string[] fields)
    {
        PropertySetLooping(fields);
    }

    public void PropertySetLooping(string[] fields)
    {
        PropertyInfo[] properties = this.GetType().GetProperties();
        long int64;

        for (var i = 0; i < properties.Length; i++)
        {
            if (!properties[i].CanWrite)
                throw new IOException($"{properties[i].Name} cant be writen!!!");

            switch (i)
            {
                case 0:
                    Int64.TryParse(fields[i], out int64);
                    properties[i].SetValue(this, int64);
                    break;
                default:
                    properties[i].SetValue(this, fields[i]);
                    break;
            }
        }
    }

    public void UpdateSelfDynamically<Order>(Order other)
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