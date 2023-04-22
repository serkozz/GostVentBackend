namespace Types.Classes;

public class StatisticsReport
{
    public string Name { get; set; }
    public DateOnly CreationDate { get; set; }
    public List<float> Data  { get; set; }
    public bool IsDataSingle  { get; set; }

    public StatisticsReport(string name, IEnumerable<float> data)
    {
        Name = name;
        CreationDate = DateOnly.FromDateTime(DateTime.Now);
        Data = data.ToList();
        IsDataSingle = Data.Count == 1 ? true : false;
    }
}