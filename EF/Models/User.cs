using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Types.Interfaces;
// using Newtonsoft.Json;

namespace EF.Models;

public partial class User : IDynamicallySettable, IDynamicallyUpdatable<User>
{
    [Key]
    public long Id { get; set; }
    public string Username { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string? Token { get; set; }
    public string? Role { get; set; }

    public User() { }

    public User(string[] fields)
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
                default:
                    properties[i].SetValue(this, fields[i]);
                    break;
            }
        }
    }

    public void UpdateSelfDynamically(User other)
    {
        PropertyInfo[] props = this.GetType().GetProperties();

        for (var i = 0; i < props.Length; i++)
        {
            props[i].SetValue(this, props[i].GetValue(other));
        }
    }

    // public override string ToString()
    // {
    //     return JsonConvert.SerializeObject(this);
    // }
}

public partial class UserShort
{
    public string Email { get; set; }
    public string Password { get; set; }

    // public override string ToString()
    // {
    //     return JsonConvert.SerializeObject(this);
    // }
}