using System;

namespace BarOmaticGUI2.ProjectCode
{
    // Base class for all bar types
    class EventBarClass
    {
        public string Name { get; }
        public double PopularityScore { get; }

        // Constructor
        public EventBarClass(string name, double popularityScore)
        {
            this.Name = name;
            this.PopularityScore = popularityScore;
        }

        // for inheritance
        public virtual string[] PrintBarSummary(int guests, double hours, double drinks)
        {
            return new string[0]; // default empty array
        }

        // Helper method to get the time-of-day popularity multiplier for the bar
        private double GetTimeMultiplier(string timeOfDay)
        {
            string barName = Name.ToLower();
            double multiplier = 1.0;

            if (barName == "espresso bar")
            {
                if (timeOfDay == "morning") { multiplier = 3.0; } // Increased for morning
                else if (timeOfDay == "afternoon") { multiplier = 1.3; }
                else if (timeOfDay == "evening") { multiplier = 0.5; }
            }
            else if (barName == "easy drinks")
            {
                if (timeOfDay == "morning") { multiplier = 1.2; }
                else if (timeOfDay == "afternoon") { multiplier = 1.0; }
                else if (timeOfDay == "evening") { multiplier = 0.8; }
            }
            else if (barName == "soda drinks")
            {
                if (timeOfDay == "morning") { multiplier = 0.8; }
                else if (timeOfDay == "afternoon") { multiplier = 0.9; }
                else if (timeOfDay == "evening") { multiplier = 0.7; }
            }
            else if (barName == "shakes")
            {
                if (timeOfDay == "morning") { multiplier = 0.8; }
                else if (timeOfDay == "afternoon") { multiplier = 1.0; }
                else if (timeOfDay == "evening") { multiplier = 1.1; }
            }
            else if (barName == "cocktails")
            {
                if (timeOfDay == "morning") { multiplier = 0.01; } // Drastically reduced for morning
                else if (timeOfDay == "afternoon") { multiplier = 0.6; }
                else if (timeOfDay == "evening") { multiplier = 1.0; }
            }
            else if (barName == "cocktails (no alcohol)")
            {
                if (timeOfDay == "morning") { multiplier = 0.7; }
                else if (timeOfDay == "afternoon") { multiplier = 1.0; }
                else if (timeOfDay == "evening") { multiplier = 1.0; }
            }
            else if (barName == "classic alcohol bar - gold" || barName == "classic alcohol bar - premium")
            {
                if (timeOfDay == "morning") { multiplier = 0.04; } // Drastically reduced for morning
                else if (timeOfDay == "afternoon") { multiplier = 0.8; }
                else if (timeOfDay == "evening") { multiplier = 1.0; }
            }
            else if (barName == "beer")
            {
                if (timeOfDay == "morning") { multiplier = 0.04; } // Drastically reduced for morning
                else if (timeOfDay == "afternoon") { multiplier = 0.5; }
                else if (timeOfDay == "evening") { multiplier = 1.1; }
            }
            else if (barName == "wine")
            {
                if (timeOfDay == "morning") { multiplier = 0.05; } // Drastically reduced for morning
                else if (timeOfDay == "afternoon") { multiplier = 0.5; }
                else if (timeOfDay == "evening") { multiplier = 1.1; }
            }
            else if (barName == "ice / barad")
            {
                if (timeOfDay == "morning") { multiplier = 1.2; }
                else if (timeOfDay == "afternoon") { multiplier = 1.3; }
                else if (timeOfDay == "evening") { multiplier = 0.8; }
            }
            return multiplier;
        }

        // The main method to get the final adjusted popularity score
        public double GetAdjustedPopularity(string timeOfDay, bool isSocial)
        {
            string barName = Name.ToLower();
            double timeMultiplier = GetTimeMultiplier(timeOfDay);
            double eventMultiplier = 1.0;

            // Apply social / professional multiplier
            if (isSocial)
            {
                if (barName.Contains("espresso")) eventMultiplier = 1.05;
                else if (barName.Contains("easy drinks")) eventMultiplier = 1.20;
                else if (barName.Contains("soda drinks")) eventMultiplier = 1.20;
                else if (barName.Contains("shakes")) eventMultiplier = 1.10;
                else if (barName.Contains("cocktails")) eventMultiplier = 1.18;
                else if (barName.Contains("cocktails (no alcohol)")) eventMultiplier = 1.15;
                else if (barName.Contains("classic alcohol")) eventMultiplier = 1.20;
                else if (barName.Contains("beer") || barName.Contains("wine")) eventMultiplier = 1.20;
                else if (barName.Contains("ice / barad")) eventMultiplier = 1.15;
            }
            else // isProfessional
            {
                if (barName.Contains("espresso")) eventMultiplier = 1.3;
                else if (barName.Contains("easy drinks")) eventMultiplier = 1.30;
                else if (barName.Contains("soda drinks")) eventMultiplier = 1.30;
                else if (barName.Contains("shakes")) eventMultiplier = 0.85;
                else if (barName.Contains("cocktails")) eventMultiplier = 0.90;
                else if (barName.Contains("cocktails (no alcohol)")) eventMultiplier = 1.15;
                else if (barName.Contains("classic alcohol")) eventMultiplier = 0.85;
                else if (barName.Contains("beer") || barName.Contains("wine")) eventMultiplier = 0.90;
                else if (barName.Contains("ice / barad")) eventMultiplier = 0.80;
            }

            // Combine both multipliers
            return this.PopularityScore * timeMultiplier * eventMultiplier;
        }
    }
}
