using System.ComponentModel.DataAnnotations;
using OneOf;
using Types.Classes;

namespace EF.Models;

public class StatisticsReport
{
    [Key]
    public long Id { get; set; }
    public string Name { get; set; }
    public DateOnly CreationDate { get; set; }
    public List<StatisticsData> Data  { get; set; }
    public bool IsDataSingle  { get; set; }

    [Obsolete("For EF Core. DONT USE IT")]
    public StatisticsReport() { }

    public StatisticsReport(string name, IEnumerable<StatisticsData> data, DateOnly? createdAt = null)
    {
        Name = name;
        if (createdAt is not null)
            CreationDate = (DateOnly)createdAt;
        else
            CreationDate = DateOnly.FromDateTime(DateTime.Now);
        Data = data.ToList();
        IsDataSingle = Data.Count == 1 ? true : false;
    }

    public void CopyFrom(StatisticsReport other)
    {
        this.Name = other.Name;
        this.CreationDate = other.CreationDate;
        this.Data = other.Data;
        this.IsDataSingle = other.IsDataSingle;
    }

    public OneOf<StatisticsReport, ErrorInfo> Difference(StatisticsReport other)
    {
        if (this.Name != other.Name)
            return new ErrorInfo(System.Net.HttpStatusCode.NotFound, "Невозможно сравнить разную статистику");

        if (this.Data.Count != other.Data.Count)
            return new ErrorInfo(System.Net.HttpStatusCode.NotFound, "Количество данных сравниваемых отчетов не совпадает");

        List<StatisticsData> dataList = new List<StatisticsData>();

        foreach (var data in other.Data)
        {
            dataList.Add(new StatisticsData(data.Name, data.Value - this.Data[dataList.Count].Value));
        }

        StatisticsReport difference = new StatisticsReport(this.Name, dataList);

        return difference;
    }
}