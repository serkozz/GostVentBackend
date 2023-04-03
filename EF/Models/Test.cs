using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Types.Interfaces;

namespace EF.Models;

public partial class Test : IDynamicallySettable
{

    [Key]
    public long Id { get; set; }
    public string Field1 { get; set; } = null!;
    public int? Field2 { get; set; } = null!;

    public Test() {}

    public Test(string[] fields)
    {
        PropertySetLooping(fields);
    }

    public void PropertySetLooping(string[] fields)
    {
        PropertyInfo[] properties = this.GetType().GetProperties();
        int int32;
        long int64;

        for (var i = 0; i < properties.Length; i++)
        {
            if (!properties[i].CanWrite)
                throw new IOException($"{properties[i]} cant be writen!!!");

            switch (i)
            {
                case 0:
                    Int64.TryParse(fields[i], out int64);
                    properties[i].SetValue(this, int64);
                    break;
                case 2:
                    Int32.TryParse(fields[i], out int32);
                    properties[i].SetValue(this, int32);
                    break;
                default:
                    properties[i].SetValue(this, fields[i]);
                    break;
            }
        }
    }
}