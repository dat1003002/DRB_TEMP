namespace DRB_TEMP.Models
{
    public class TemperatureDailyLog
    {
        public int Id { get; set; }

        public double? NhietDo { get; set; }

        public double? DoAm { get; set; }

        public DateTime LogDate { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
