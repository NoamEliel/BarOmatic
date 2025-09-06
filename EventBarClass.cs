using System;

namespace BarOmaticGUI2.ProjectCode
{
    // Base class for all bar types
    class EventBarClass
    {
        public string Name { get; }             // Name of the bar type
        public double PopularityScore { get; } // Popularity score between 0.1 and 1.0

        // Constructor to initialize name and popularity score
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


        //adjust popularity score based on the time of day:
        public double GetAdjustedPopularity(string timeOfDay)
        {
            double multiplier = 1.0;

            // Normalize input
            string time = timeOfDay.ToLower();

            if (Name == "Espresso bar")
            {
                if (time == "morning") multiplier = 1.7;
                else if (time == "afternoon") multiplier = 1.3;
                else if (time == "evening") multiplier = 0.5;

            }
            else if (Name == "Easy drinks")
            {
                if (time == "morning") multiplier = 1.5;
                else if (time == "afternoon") multiplier = 1.5;
                else if (time == "evening") multiplier = 1.2;
            }
            else if (Name == "Soda drinks")
            {
                if (time == "morning") multiplier = 1;
                else if (time == "afternoon") multiplier = 1.0;
                else if (time == "evening") multiplier = 0.9;
            }
            else if (Name == "Shakes")
            {
                if (time == "morning") multiplier = 1.2;
                else if (time == "afternoon") multiplier = 1.0;
                else if (time == "evening") multiplier = 0.7;
            }
            else if (Name == "Cocktails")
            {
                if (time == "morning") multiplier = 0.5;
                else if (time == "afternoon") multiplier = 0.9;
                else if (time == "evening") multiplier = 1.3;
            }
            else if (Name == "Cocktails (no alcohol)")
            {
                if (time == "morning") multiplier = 0.7;
                else if (time == "afternoon") multiplier = 1.0;
                else if (time == "evening") multiplier = 1.0;
            }
            else if (Name == "Classic Alcohol Bar - Gold" || Name == "Classic Alcohol Bar - Premium")
            {
                if (time == "morning") multiplier = 0.4;
                else if (time == "afternoon") multiplier = 1.0;
                else if (time == "evening") multiplier = 1.3;
            }
            else if (Name == "Beer")
            {
                if (time == "morning") multiplier = 0.6;
                else if (time == "afternoon") multiplier = 0.9;
                else if (time == "evening") multiplier = 1.4;
            }
            else if (Name == "Wine")
            {
                if (time == "morning") multiplier = 0.5;
                else if (time == "afternoon") multiplier = 0.7;
                else if (time == "evening") multiplier = 1.4;
            }
            else if (Name == "Ice / Barad")
            {
                if (time == "morning") multiplier = 1.2;
                else if (time == "afternoon") multiplier = 1.3;
                else if (time == "evening") multiplier = 0.8;
            }

            return this.PopularityScore * multiplier;
        }
    }
}
