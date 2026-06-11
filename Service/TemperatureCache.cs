namespace DRB_TEMP.Service
{
    public class TemperatureCache
    {
        private double? _nhietDo;
        private double? _doAm;
        private DateTime _updatedAt = DateTime.MinValue;
        private volatile bool _dailyLogUpdated = false;

        public void Update(double? nhietDo, double? doAm)
        {
            _nhietDo = nhietDo;
            _doAm = doAm;
            _updatedAt = DateTime.Now;
        }

        public void MarkDailyLogUpdated() => _dailyLogUpdated = true;

        // Trả về true và reset flag — chỉ true 1 lần cho mỗi lần update
        public bool ConsumeDailyLogUpdated()
        {
            if (!_dailyLogUpdated) return false;
            _dailyLogUpdated = false;
            return true;
        }

        public (double? NhietDo, double? DoAm, DateTime UpdatedAt) Get()
            => (_nhietDo, _doAm, _updatedAt);
    }
}
