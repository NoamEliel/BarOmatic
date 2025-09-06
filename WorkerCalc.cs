using System;

namespace BarOmaticGUI2.ProjectCode
{
    internal class WorkerCalc
    {
        private const int GuestsPerWorker = 40;
        private const int HourlyRate = 45;

        public int NumberOfWorkers(int guestCount)
        {
            return (int)Math.Ceiling((double)guestCount / GuestsPerWorker);
        }

        public int TotalCost(int guestCount, double hours)
        {
            int workers = NumberOfWorkers(guestCount);
            return (int)(workers * hours * HourlyRate);
        }

        public void PrintSummary(int guestCount, double hours)
        {
            int workers = NumberOfWorkers(guestCount);
            int cost = TotalCost(guestCount, hours);

            Console.WriteLine("=== Event Worker Summary ===");
            Console.WriteLine($"Guests: {guestCount}");
            Console.WriteLine($"Event Duration: {hours} hours");
            Console.WriteLine($"Workers needed: {workers}");
            Console.WriteLine($"Total Worker Cost: {cost} ILS");
            Console.WriteLine("============================");
            Console.WriteLine();
            Console.WriteLine();
        }
    }
}
