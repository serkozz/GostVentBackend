using System.ComponentModel.DataAnnotations;
using Types.Classes;

namespace EF.Models;

public partial class StatisticsData
{
    [Key]
    public long Id { get; set; }
    public string Name { get; set; }
    public float Value { get; set; }

    public StatisticsData(string name, float value)
    {
        Name = name;
        Value = value;
    }

    [Obsolete("For EF Core, DONT USE IT")]
    public StatisticsData() { }
}