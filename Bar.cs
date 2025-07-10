using System;

namespace BarOmatic
{
    public class Bar
    {
        public string Name { get; set; }
        public double HourlyRateILS { get; set; }

        public double GetCost(int hours)
        {
            return HourlyRateILS * hours;
        }

        public override string ToString()
        {
            return $"{Name} (₪{HourlyRateILS}/hour)";
        }
    }
}
