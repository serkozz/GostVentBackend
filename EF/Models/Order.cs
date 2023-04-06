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
        try
        {
            PropertySetLooping(fields);
        }
        catch (System.Exception)
        {
            /// Устанавливаем несуществующий айди,
            /// это признак неверной динамической инициализации объекта
            this.Id = -1;
        }
    }

    public void PropertySetLooping(string[] fields)
    {
        PropertyInfo[] properties = this.GetType().GetProperties();
        bool parseRes = true;

        for (var i = 0; i < properties.Length; i++)
        {
            if (!properties[i].CanWrite)
                throw new IOException($"{properties[i].Name} cant be writen!!!");
            switch (i)
            {
                case 0:
                    parseRes = Int64.TryParse(fields[i], out long int64);
                    if (int64 < 0)
                    {
                        parseRes = false;
                        break;
                    }
                    properties[i].SetValue(this, int64);
                    break;
                case 2:
                    parseRes = Int32.TryParse(fields[i], out int productTypeInt);
                    if (productTypeInt < 0 || productTypeInt >= Enum.GetNames(typeof(ProductType)).Length)
                    {
                        parseRes = false;
                        break;
                    }
                    parseRes = Enum.TryParse(fields[i], out ProductType productType);
                    properties[i].SetValue(this, productType);
                    break;
                case 3:
                    parseRes = DateOnly.TryParse(fields[i], out DateOnly date);
                    properties[i].SetValue(this, date);
                    break;
                case 4:
                    parseRes = Int32.TryParse(fields[i], out int leadDays);
                    if (leadDays <= 0)
                    {
                        parseRes = false;
                        break;
                    }
                    properties[i].SetValue(this, Math.Abs(leadDays));
                    break;
                case 5:
                    parseRes = Int32.TryParse(fields[i], out int orderStatusInt);
                    if (orderStatusInt < 0 || orderStatusInt >= Enum.GetNames(typeof(OrderStatus)).Length)
                    {
                        parseRes = false;
                        break;
                    }
                    parseRes = Enum.TryParse(fields[i], out OrderStatus orderStatus);
                    properties[i].SetValue(this, orderStatus);
                    break;
                case 6:
                    parseRes = Int32.TryParse(fields[i], out int price);
                    if (price < 0)
                    {
                        parseRes = false;
                        break;
                    }
                    properties[i].SetValue(this, Math.Abs(price));
                    break;
                case 7:
                    parseRes = Int64.TryParse(fields[i], out long clientId);
                    if (clientId < 0)
                    {
                        parseRes = false;
                        break;
                    }
                    properties[i].SetValue(this, clientId);
                    break;
                case 8:
                    break;
                default:
                    properties[i].SetValue(this, fields[i]);
                    break;
            }

            if (parseRes == false)
                throw new InvalidCastException($"Свойство {properties[i].Name} имеет тип {properties[i].PropertyType}, назначаемое значение: {fields[i]} имеет отличный тип");
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
}