namespace DRB_TEMP.Models
{
    public class TemperatureIntradayLog
    {
        public int Id { get; set; }

        public double? NhietDo { get; set; }

        public double? DoAm { get; set; }

        public DateTime RecordedAt { get; set; }
    }
}
