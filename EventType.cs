using System;
using static Program;

namespace BarOmatic
{
    class EventType
    {

        private const double EMPLOYEE_HOURLY_WAGE = 40; 
        private const int GUESTS_PER_EMPLOYEE = 40;

        private readonly double profitMarginPercent;
        private readonly Node<Bar> bars;
        private readonly int hours;
        private readonly int guestCount;

        public EventType(Node<Bar> bars, int hours, int guestCount, double profitMarginPercent)
        {
            this.bars = bars ?? throw new ArgumentNullException(nameof(bars));
            this.hours = hours > 0 ? hours : throw new ArgumentOutOfRangeException(nameof(hours));
            this.guestCount = guestCount > 0 ? guestCount : throw new ArgumentOutOfRangeException(nameof(guestCount));
            this.profitMarginPercent = profitMarginPercent >= 0 ? profitMarginPercent : throw new ArgumentOutOfRangeException(nameof(profitMarginPercent));
        }


        private int GetEmployeeCount()
        {
            return (guestCount + GUESTS_PER_EMPLOYEE - 1) / GUESTS_PER_EMPLOYEE;
        }

        private double CalculateEmployeeWage()
        {
            return GetEmployeeCount() * EMPLOYEE_HOURLY_WAGE * hours;
        }

        public double CalculateBaseCost()
        {
            double total = 0;
            Node<Bar> current = bars;
            while (current != null)
            {
                total += current.GetValue().GetCost(hours);
                current = current.GetNext();
            }
            return total + CalculateEmployeeWage();
        }

        private double CalculateProfit()
        {
            return CalculateBaseCost() * (profitMarginPercent / 100.0);
        }

        public double CalculateTotalCost()
        {
            return CalculateBaseCost() + CalculateProfit();
        }

        public void PrintSummary()
        {
            Console.WriteLine("\n========== BarOmatic Event Summary ==========");
            Console.WriteLine($" Duration: {hours} hours");
            Console.WriteLine($" Guest Count: {guestCount}");
            Console.WriteLine($" Employees Needed: {GetEmployeeCount()}");
            Console.WriteLine($" Employee Wage per Hour: ₪{EMPLOYEE_HOURLY_WAGE}");
            Console.WriteLine($" Total Employee Wages: ₪{CalculateEmployeeWage()}");
            Console.WriteLine();
            Console.WriteLine(" ----------- ");
            Console.WriteLine();
            Console.WriteLine("\n Selected Bars:");
            Node<Bar> current = bars;
            while (current != null)
            {
                Bar bar = current.GetValue();
                Console.WriteLine($" - {bar.Name}: ₪{bar.HourlyRateILS}/hour → ₪{bar.GetCost(hours)} total");
                current = current.GetNext();
            }

            double baseCost = CalculateBaseCost();
            double profit = CalculateProfit();
            double finalTotal = CalculateTotalCost();
            Console.WriteLine();
            Console.WriteLine(" ----------- ");
            Console.WriteLine();
            Console.WriteLine("\n---------- Pricing Breakdown ----------");
            Console.WriteLine($" Base Cost (bars + employees): ₪{baseCost}");
            Console.WriteLine($" Profit (@ {profitMarginPercent}%): ₪{profit}");
            Console.WriteLine($" → Total Customer Price: ₪{finalTotal}");
            Console.WriteLine("==========================================\n");
        }
    }
}
