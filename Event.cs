using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace BarOmaticGUI2.ProjectCode
{
    class Event
    {
        public int GuestCount;
        public double DurationHours;
        public string TimeOfDay;

        public bool isSocial;
        public bool isProfessional;

        private Node<string> SelectedBarNames;
        private const double MorningBaseRate = 0.7;
        private const double AfternoonBaseRate = 0.9;
        private const double EveningBaseRate = 1.2;

        // Constructor
        public Event(int guestCount, double durationHours, string timeOfDay, Node<string> selectedBarNames, bool isSocial, bool isProfessional)
        {
            GuestCount = guestCount;
            DurationHours = durationHours;
            TimeOfDay = timeOfDay.ToLower();
            if (TimeOfDay != "morning" && TimeOfDay != "afternoon" && TimeOfDay != "evening")
                throw new ArgumentException("TimeOfDay must be morning, afternoon, or evening");
            SelectedBarNames = selectedBarNames;

            this.isSocial = isSocial;
            this.isProfessional = isProfessional;
        }

        // === Converters ===
        internal Node<EventBarClass>? ConvertSelectedBars()
        {
            Node<EventBarClass> head = null!;
            Node<string> current = SelectedBarNames;

            while (current != null)
            {
                EventBarClass ewBar = CreateEventBarFromName(current.GetValue())!;
                if (ewBar != null)
                {
                    head = Node<EventBarClass>.Append(head, ewBar);
                }
                current = current.GetNext();
            }
            return head;
        }
        private EventBarClass? CreateEventBarFromName(string name)
        {
            switch (name)
            {
                case "Espresso Bar": return new EspressoBar();
                case "Easy Drinks": return new EasyDrinksBar();
                case "Soda Drinks": return new SodaDrinksBar();
                case "Shakes": return new ShakesBar();
                case "Cocktails": return new CocktailsBar();
                case "Cocktails (no alcohol)": return new CocktailsNoAlcoholBar();
                case "Classic Alcohol Bar - Gold": return new ClassicAlcoholGoldBar();
                case "Classic Alcohol Bar - Premium": return new ClassicAlcoholPremiumBar();
                case "Beer": return new BeerBar();
                case "Wine": return new WineBar();
                case "Ice / Barad": return new IceBaradBar();
                default: return null;
            }
        }

        // === Calculators ===
        public double CalculateDrinksPerGuest()
        {
            Node<EventBarClass> bars = ConvertSelectedBars()!;
            if (bars == null) { return 0.0; }

            // Check for single bar event and use the specialized cap.
            if (Node<EventBarClass>.Count(bars) == 1)
            {
                return GetSingleBarCap(bars.GetValue());
            }

            double baseRate;
            if (TimeOfDay == "morning") baseRate = MorningBaseRate;
            else if (TimeOfDay == "afternoon") baseRate = AfternoonBaseRate;
            else baseRate = EveningBaseRate;

            // The drinks per guest now starts with a base and grows with each bar
            double drinksPerGuestTotal = 0.0;
            Node<EventBarClass> current = bars;

            while (current != null)
            {   
                EventBarClass bar = current.GetValue();
                double adjustedScore = bar.GetAdjustedPopularity(TimeOfDay, isSocial);

                // Apply a portion of the adjusted popularity to the total.
                // The factor of 0.5 is a new tuning parameter to prevent the total from becoming too high.
                drinksPerGuestTotal += adjustedScore * 0.5;

                current = current.GetNext();
            }

            // This block calculates the duration multiplier based on the event hours.
            // It is now applied to the accumulated popularity.
            double h = DurationHours;
            double durationMultiplier = 0.0;
            durationMultiplier += (Math.Min(h, 1.0) * baseRate * 2.0);
            durationMultiplier += (Math.Min(Math.Max(h - 1.0, 0.0), 2.0) * baseRate);
            durationMultiplier += Math.Min(Math.Max(h - 3.0, 0.0), 3.0) * baseRate * 0.6;
            durationMultiplier += Math.Max(h - 6.0, 0.0) * baseRate * 0.25;

            drinksPerGuestTotal += durationMultiplier;

            // Apply dampening for multiple strong bars
            int strongBars = 0;
            Node<EventBarClass> tmp = bars;
            while (tmp != null)
            {
                if (IsStrongBar(tmp.GetValue().Name)) strongBars++;
                tmp = tmp.GetNext();
            }

            if (strongBars >= 1)
            {
                drinksPerGuestTotal *= 0.8;
            }

            return drinksPerGuestTotal;
        }
        private bool IsStrongBar(string barName)
        {
            // A bar is considered "strong" if it contains any of these keywords in its name.
            return barName.Contains("Cocktail") ||
                   barName.Contains("Classic Alcohol") ||
                   barName.Contains("Beer") ||
                   barName.Contains("Wine") ||
                   barName.Contains("Alcohol") || barName.Contains("Espresso");     
        }
        private double GetSingleBarCap(EventBarClass bar)
        {
            // Base caps for a 4-hour event (approximate drinks per guest)
            double baseCap;
            if (bar == null)
            {
                return 0;
            }        

            if (bar.Name.Contains("Espresso")) { baseCap = 0.4; }
            else if (bar.Name.Contains("Cocktail") && !bar.Name.Contains("no alcohol")) { baseCap = 2.5; }
            else if (bar.Name.Contains("Cocktail (no alcohol)")) { baseCap = 2.5; }
            else if (bar.Name.Contains("Classic Alcohol")) { baseCap = 3.2; }
            else if (bar.Name.Contains("Beer") || bar.Name.Contains("Wine")) { baseCap = 2.8; }
            else if (bar.Name.Contains("Easy") || bar.Name.Contains("Soda")) { baseCap = 2.7; }
            else if (bar.Name.Contains("Shakes")) { baseCap = 1.1; }
            else if (bar.Name.Contains("Ice / Barad")) { baseCap = 1.4; }
            else { baseCap = 2.0; }

            // Normalize baseCap based on event duration relative to a standard 4-hour event.
            // This makes the cap correct for any duration provided.
            double normalizedCap = baseCap * (DurationHours / 4.0);

            // Get the bar's popularity, which now includes both time of day and event type adjustments.
            double adjustedPopularity = bar.GetAdjustedPopularity(TimeOfDay, isSocial);

            // The multiplier is the ratio of the adjusted popularity to the base popularity.
            double multiplier = adjustedPopularity / bar.PopularityScore;

            // Apply the multiplier to the normalized cap.
            double finalCap = normalizedCap * multiplier;

            // Ensure final cap is not negative.
            if (finalCap < 0.0) finalCap = 0.0;

            return finalCap;
        
        }
        public Node<string> GetSelectedBarNames()
        {
            return SelectedBarNames;
        }
        // In the Event.cs file, add this method
        public double GetDampeningFactor()
        {
            int strongBars = 0;
            Node<EventBarClass> tmp = ConvertSelectedBars()!;
            while (tmp != null)
            {
                if (IsStrongBar(tmp.GetValue().Name)) strongBars++;
                tmp = tmp.GetNext();
            }
            return (strongBars >= 1) ? 0.8 : 1.0;
        }

        //nested bar classes:
        public class EspressoBar : EventBarClass
        {
            public EspressoBar() : base("Espresso bar", 0.1) { }

            public override string[] PrintBarSummary(int guests, double hours, double drinks)
            {
                // Equipment
                int coffeeMachines = (int)Math.Ceiling((double)guests / 150);
                int coffeeMachineHandles = coffeeMachines * 2;
                int coffeeMachineWipers = coffeeMachines;
                int milkFridge = 1;
                int waterBoiler = coffeeMachines;
                int fireSources = (int)Math.Ceiling(waterBoiler * hours);
                int milkContainer = (int)Math.Ceiling((double)guests / 40);
                int coffeeMachineBowls = (int)Math.Ceiling(coffeeMachines * 5.0);
                int grinder = coffeeMachines;
                int longSpoons = (int)Math.Ceiling((double)guests / 40);
                int trashCan = 1;
                int coffeeMachineBrushes = coffeeMachines;
                int waterJerikan = waterBoiler;
                int meichamPlay = 1;
                int meichamDisplay = 1;
                int leftOverBowl = 1;
                int hotDrinkCups = (int)Math.Ceiling(drinks * 1.1); // 10% buffer

                // Consumables
                int groundCoffeeGrams = (int)Math.Ceiling(drinks * 9);
                int nesCafe100gPackages = (int)Math.Ceiling(drinks / 1000.0);
                int teaBags = (int)Math.Ceiling(drinks / 10.0);
                int herbalTeaBags = (int)Math.Ceiling(drinks / 18.0);
                int brownSugarGrams = (int)Math.Ceiling(drinks * 0.6);
                int whiteSugarGrams = (int)Math.Ceiling(drinks * 1.5);
                int sucrazitTablets = (int)Math.Ceiling(drinks / 6.0);
                int nanaBranches = (int)Math.Ceiling(drinks / 20.0);
                int lemons = (int)Math.Ceiling(drinks / 10.0);

                int normalMilkL = (int)Math.Ceiling(drinks * 100.0 / 1000.0);
                int soyMilkL = (int)Math.Ceiling(drinks * 40.0 / 1000.0);
                int almondMilkL = (int)Math.Ceiling(drinks * 30.0 / 1000.0);
                int oatMilkL = (int)Math.Ceiling(drinks * 13.0 / 1000.0);
                int onePercentMilkL = (int)Math.Ceiling(drinks * 7.0 / 1000.0);

                // Helper for formatting
                string FormatLine(string name, object value) => string.Format("{0,-23}: {1,6}", name, value);

                // Build the array
                string[] lines =
                {
            "\n=== Espresso Bar Summary ===\n",
            "\n--- Consumables ---",
            FormatLine("Ground Coffee (kg)", groundCoffeeGrams / 1000),
            FormatLine("NesCafe 100g pkgs", nesCafe100gPackages),
            FormatLine("Tea Bags", teaBags),
            FormatLine("Herbal Tea Bags", herbalTeaBags),
            FormatLine("Brown Sugar (g)", brownSugarGrams),
            FormatLine("White Sugar (g)", whiteSugarGrams),
            FormatLine("Sucrazit Tablets", sucrazitTablets),
            FormatLine("Nana Branches", nanaBranches),
            FormatLine("Lemons", lemons),
            "\n--- Milks (L) ---",
            FormatLine("Normal Milk", normalMilkL),
            FormatLine("Soy Milk", soyMilkL),
            FormatLine("Almond Milk", almondMilkL),
            FormatLine("Oat Milk", oatMilkL),
            FormatLine("1% Milk", onePercentMilkL),
            "\n--- Equipment ---",
            FormatLine("Coffee Machines", coffeeMachines),
            FormatLine("Coffee Machine Handles", coffeeMachineHandles),
            FormatLine("Coffee Machine Wipers", coffeeMachineWipers),
            FormatLine("Milk Fridge", milkFridge),
            FormatLine("Water Boiler", waterBoiler),
            FormatLine("Fire Sources", fireSources),
            FormatLine("Milk Container", milkContainer),
            FormatLine("Coffee Machine Bowls", coffeeMachineBowls),
            FormatLine("Grinders", grinder),
            FormatLine("Long Spoons", longSpoons),
            FormatLine("Trash Can", trashCan),
            FormatLine("Coffee Machine Brushes", coffeeMachineBrushes),
            FormatLine("Water Jerikans", waterJerikan),
            FormatLine("Meicham Play", meichamPlay),
            FormatLine("Meicham Display", meichamDisplay),
            FormatLine("Leftover Bowls", leftOverBowl),
            FormatLine("Hot Drink Cups", hotDrinkCups),
            ""
        };

                return lines;
            }
        }
        public class EasyDrinksBar : EventBarClass
        {
            public EasyDrinksBar() : base("Easy drinks", 0.4) { }

            public override string[] PrintBarSummary(int guests, double hours, double drinks)
            {
                // Equipment
                int dualContraption = 1;
                int container = 1;
                int whisk = 1;
                int pitcher = 1;
                int knifeAndCuttingBoard = 1;
                int icePackages = (int)Math.Ceiling(drinks / 36.0);
                int cups200ml = (int)Math.Ceiling(drinks * 1.1);
                int straws = cups200ml;

                // Consumables
                int orangeConcentrateLiters = (int)Math.Ceiling(drinks / 25.0);
                int lemonMintConcentrateLiters = (int)Math.Ceiling(drinks / 25.0);
                int lemonsDecoration = (int)Math.Ceiling(cups200ml / 5.0);
                int orangesDecoration = (int)Math.Ceiling(cups200ml / 5.0);
                int mintBranches = (int)Math.Ceiling(cups200ml / 5.0);

                // Helper for formatting
                string FormatLine(string name, object value) => string.Format("{0,-29}: {1,6}", name, value);

                // Build the array
                string[] lines =
                {
            "\n=== Easy Drinks Bar Summary ===\n",
            "\n--- Consumables ---",
            FormatLine("Orange Concentrate (L)", orangeConcentrateLiters),
            FormatLine("Lemon-Mint Concentrate (L)", lemonMintConcentrateLiters),
            FormatLine("Lemons for Decoration", lemonsDecoration),
            FormatLine("Oranges for Decoration", orangesDecoration),
            FormatLine("Mint Branches for Decoration", mintBranches),
            "\n--- Equipment ---",
            FormatLine("Dual Contraption", dualContraption),
            FormatLine("Container", container),
            FormatLine("Whisk", whisk),
            FormatLine("Pitcher", pitcher),
            FormatLine("Knife & Cutting Board", knifeAndCuttingBoard),
            FormatLine("Ice Packages", icePackages),
            FormatLine("200ML Cups", cups200ml),
            FormatLine("Straws", straws),
            ""
        };

                return lines;
            }
        }
        public class SodaDrinksBar : EventBarClass
        {
            public SodaDrinksBar() : base("Soda drinks", 0.3) { }

            public override string[] PrintBarSummary(int guests, double hours, double drinks)
            {
                // Equipment
                int cups250 = (int)Math.Ceiling(drinks * 1.1);
                int icePila = 1;
                int iceSpoon = 2;
                int straws = cups250;
                int fridge = 1;
                const int cupsPerBottle = 6; // 1.5L bottle / 250ml cups

                // Consumables (bottles)
                int zeroColaBottles = Math.Max(1, (int)Math.Ceiling(drinks * 0.35 / cupsPerBottle));
                int normalColaBottles = Math.Max(1, (int)Math.Ceiling(drinks * 0.25 / cupsPerBottle));
                int spriteBottles = Math.Max(1, (int)Math.Ceiling(drinks * 0.18 / cupsPerBottle));
                int spriteZeroBottles = Math.Max(1, (int)Math.Ceiling(drinks * 0.10 / cupsPerBottle));
                int fantaBottles = Math.Max(1, (int)Math.Ceiling(drinks * 0.07 / cupsPerBottle));
                int eshcoliotBottles = Math.Max(1, (int)Math.Ceiling(drinks * 0.04 / cupsPerBottle));
                int sodaBottles = Math.Max(1, (int)Math.Ceiling(drinks * 0.01 / cupsPerBottle));

                // Helper for formatting
                string FormatLine(string name, object value) => string.Format("{0,-23}: {1,4}", name, value);

                // Build the array
                string[] lines =
                {
            "\n=== Soda Drinks Bar Summary ===\n",
            "\n--- Consumables ---",
            FormatLine("Zero Cola (bottles)", zeroColaBottles),
            FormatLine("Normal Cola (bottles)", normalColaBottles),
            FormatLine("Sprite (bottles)", spriteBottles),
            FormatLine("Sprite Zero (bottles)", spriteZeroBottles),
            FormatLine("Fanta (bottles)", fantaBottles),
            FormatLine("Eshcoliot (bottles)", eshcoliotBottles),
            FormatLine("Soda (bottles)", sodaBottles),
            "\n--- Equipment ---",
            FormatLine("250ML Cups", cups250),
            FormatLine("Ice Pila", icePila),
            FormatLine("Ice Spoons", iceSpoon),
            FormatLine("Straws", straws),
            FormatLine("Fridge", fridge),
            ""
        };

                return lines;
            }
        }
        public class ShakesBar : EventBarClass
        {
            public ShakesBar() : base("Shakes", 0.3) { }

            public override string[] PrintBarSummary(int guests, double hours, double drinks)
            {
                // Equipment / Tools
                int blenders = (int)Math.Ceiling(guests / 30.0);
                int blenderContainers = blenders;
                int condensingPoles = blenders;
                int cups = (int)Math.Ceiling(drinks * 1.1);
                int straws = cups;
                int freezer = 1;
                int waterContainer = 1;
                int levelledDisplayTool = 1;
                int decorationSigns = 1;

                // Consumables (kg / L)
                double banana_kg = drinks * 68 / 1000.0;
                double strawberry_kg = drinks * 17.5 / 1000.0;
                double mango_kg = drinks * 14 / 1000.0;
                double pineapple_kg = drinks * 10.5 / 1000.0;
                double blueberries_kg = drinks * 8.4 / 1000.0;
                double melon_kg = drinks * 7 / 1000.0;
                double date_kg = drinks * 5.6 / 1000.0;
                double kiwi_kg = drinks * 4.2 / 1000.0;
                double petel_kg = drinks * 2.1 / 1000.0;
                double pecan_kg = drinks * 0.7 / 1000.0;

                double milk_l = drinks * 67.2 / 1000.0;
                double soy_l = drinks * 28 / 1000.0;
                double water_l = drinks * 16.8 / 1000.0;
                double sugar_kg = drinks * 4 / 1000.0;
                double condensedOrange_l = drinks * 4.5 / 1000.0;
                double decorFruit_kg = drinks * 3 / 1000.0;

                // Unit sizes for reference
                const double BANANA_UNIT = 120;
                const double STRAWBERRY_UNIT = 20;
                const double MANGO_UNIT = 200;
                const double PINEAPPLE_UNIT = 900;
                const double BLUEBERRIES_UNIT = 125;
                const double MELON_UNIT = 2000;
                const double DATE_UNIT = 8;
                const double KIWI_UNIT = 75;
                const double PETEL_UNIT = 100;
                const double PECAN_UNIT = 100;
                const double MILK_CARTON = 1000;
                const double SOYMILK_CARTON = 1000;
                const double WATER_BOTTLE = 1500;
                const double SUGAR_PACK = 1000;
                const double COND_ORANGE_BOTTLE = 1000;
                const double DECOR_FRUIT_UNIT = 50;

                // Helper for formatting
                string FormatLine(string name, object value) => string.Format("{0,-23}: {1,6}", name, value);

                // Build the array
                string[] lines =
                {
            "\n=== Shakes Bar Summary ===\n",
            "\n--- Consumables ---",
            FormatLine("Banana ", Math.Ceiling(banana_kg) + " kg (" + Math.Ceiling(banana_kg * 1000 / BANANA_UNIT) + " bananas)"),
            FormatLine("Strawberry ", Math.Ceiling(strawberry_kg) + " kg (" + Math.Ceiling(strawberry_kg * 1000 / STRAWBERRY_UNIT) + " strawberries)"),
            FormatLine("Mango ", Math.Ceiling(mango_kg) + " kg (" + Math.Ceiling(mango_kg * 1000 / MANGO_UNIT) + " mangos)"),
            FormatLine("Pineapple ", Math.Ceiling(pineapple_kg) + " kg (" + Math.Ceiling(pineapple_kg * 1000 / PINEAPPLE_UNIT) + " pineapples)"),
            FormatLine("Blueberries ", Math.Ceiling(blueberries_kg) + " kg (" + Math.Ceiling(blueberries_kg * 1000 / BLUEBERRIES_UNIT) + " punnets)"),
            FormatLine("Melon ", Math.Ceiling(melon_kg) + " kg (" + Math.Ceiling(melon_kg * 1000 / MELON_UNIT) + " melons)"),
            FormatLine("Date ", Math.Ceiling(date_kg) + " kg (" + Math.Ceiling(date_kg * 1000 / DATE_UNIT) + " dates)"),
            FormatLine("Kiwi ", Math.Ceiling(kiwi_kg) + " kg (" + Math.Ceiling(kiwi_kg * 1000 / KIWI_UNIT) + " kiwis)"),
            FormatLine("Petel ", Math.Ceiling(petel_kg) + " kg (" + Math.Ceiling(petel_kg * 1000 / PETEL_UNIT) + " pieces)"),
            FormatLine("Sugary pecan ", Math.Ceiling(pecan_kg) + " kg (" + Math.Ceiling(pecan_kg * 1000 / PECAN_UNIT) + " packs)"),
            FormatLine("Normal milk ", Math.Ceiling(milk_l) + " L (" + Math.Ceiling(milk_l * 1000 / MILK_CARTON) + " cartons)"),
            FormatLine("Soy milk ", Math.Ceiling(soy_l) + " L (" + Math.Ceiling(soy_l * 1000 / SOYMILK_CARTON) + " cartons)"),
            FormatLine("Water ", Math.Ceiling(water_l) + " L (" + Math.Ceiling(water_l * 1000 / WATER_BOTTLE) + " bottles)"),
            FormatLine("Sugar ", Math.Ceiling(sugar_kg) + " kg (" + Math.Ceiling(sugar_kg * 1000 / SUGAR_PACK) + " packs)"),
            FormatLine("Tarkiz orange juice ", Math.Ceiling(condensedOrange_l) + " L (" + Math.Ceiling(condensedOrange_l * 1000 / COND_ORANGE_BOTTLE) + " bottles)"),
            FormatLine("Decoration fruit ", Math.Ceiling(decorFruit_kg) + " kg (" + Math.Ceiling(decorFruit_kg * 1000 / DECOR_FRUIT_UNIT) + " pieces)"),
            "\n--- Equipment / Tools ---",
            FormatLine("Blenders", blenders),
            FormatLine("Blender containers", blenderContainers),
            FormatLine("Condensing poles", condensingPoles),
            FormatLine("Freezer", freezer),
            FormatLine("Water container/Smovar", waterContainer),
            FormatLine("250ml cups", cups),
            FormatLine("Straws", straws),
            FormatLine("Levelled display tool", levelledDisplayTool),
            FormatLine("Decoration signs", decorationSigns),
            ""
        };

                return lines;
            }
        }
        public class CocktailsBar : EventBarClass
        {
            public CocktailsBar() : base("Cocktails", 0.4) { }

            public override string[] PrintBarSummary(int guests, double hours, double drinks)
            {
                // Equipment / Tools
                int shakers = (int)Math.Ceiling(guests / 30.0);
                int filters = shakers;
                int thinFilters = shakers;
                int jiggers = shakers * 2;
                int longSpoons = shakers;
                int tweezers = shakers;
                int woodenContainers = 3;
                int servingPlates = 3;
                int iceCrusher = 1;
                int brenerBurner = 1;
                int cups = (int)Math.Ceiling(drinks * 1.1); // avg 180ml cup
                int straws = cups;
                int strawsContainer = 1;
                int dualContraption = 1;
                int purerim = 1;

                // Consumables
                double vodka_l = drinks * 30 / 1000.0;
                double gin_l = drinks * 20 / 1000.0;
                double rum_l = drinks * 15 / 1000.0;
                double tequila_l = drinks * 10 / 1000.0;
                double whisky_l = drinks * 10 / 1000.0;
                double tripleSec_l = drinks * 3 / 1000.0;
                double aperol_l = drinks * 5 / 1000.0;
                double campary_l = drinks * 5 / 1000.0;
                double cara_l = drinks * 5 / 1000.0;
                double gingerBeer_l = drinks * 10 / 1000.0;
                double angostura_l = drinks * 5 / 1000.0;

                double naturalJuice_l = drinks * 8 / 1000.0;
                double soda_l = drinks * 5 / 1000.0;
                double flavorSyrup_l = drinks * 1 / 1000.0;
                double limeJuice_l = drinks * 1 / 1000.0;
                double ice_kg = drinks * 150 / 1000.0;
                double lemons_kg = drinks * 10 / 1000.0;
                double cinnamonStick_kg = drinks * 2 / 1000.0;
                double coconutChips_kg = drinks * 2 / 1000.0;
                double driedOrange_kg = drinks * 3 / 1000.0;
                double nana_kg = drinks * 2 / 1000.0;
                double driedLemons_kg = drinks * 3 / 1000.0;
                double anisStars_kg = drinks * 1 / 1000.0;
                double edibleFlowers_kg = drinks * 2 / 1000.0;

                // Helper for formatting
                string FormatLine(string name, object value) => string.Format("{0,-20}: {1,6}", name, value);

                // Build the array
                string[] lines =
                {
                    "\n=== Cocktails Bar Summary ===\n",
                    "\n--- Alcohols ---",
                    FormatLine("Vodka (L)", Math.Ceiling(vodka_l)),
                    FormatLine("Gin (L)", Math.Ceiling(gin_l)),
                    FormatLine("Rum (L)", Math.Ceiling(rum_l)),
                    FormatLine("Tequila (L)", Math.Ceiling(tequila_l)),
                    FormatLine("Whisky (L)", Math.Ceiling(whisky_l)),
                    FormatLine("Ginger Beer (L)", Math.Ceiling(gingerBeer_l)),
                    FormatLine("Aperol (L)", Math.Ceiling(aperol_l)),
                    FormatLine("Campary (L)", Math.Ceiling(campary_l)),
                    FormatLine("Cara (L)", Math.Ceiling(cara_l)),
                    FormatLine("Triple Sec (L)", Math.Ceiling(tripleSec_l)),
                    FormatLine("Angostura (L)", Math.Ceiling(angostura_l)),
                    "\n--- Mixers ---",
                    FormatLine("Natural Juice (L)", Math.Ceiling(naturalJuice_l)),
                    FormatLine("Soda (L)", Math.Ceiling(soda_l)),
                    FormatLine("Flavor Syrup (L)", Math.Ceiling(flavorSyrup_l)),
                    FormatLine("Lime Juice (L)", Math.Ceiling(limeJuice_l)),
                    FormatLine("Ice (kg)", Math.Ceiling(ice_kg)),
                    FormatLine("Lemons (kg)", Math.Ceiling(lemons_kg)),
                    FormatLine("Dried Orange (kg)", Math.Ceiling(driedOrange_kg)),
                    FormatLine("Dried Lemons (kg)", Math.Ceiling(driedLemons_kg)),
                    FormatLine("Cinnamon Stick (kg)", Math.Ceiling(cinnamonStick_kg)),
                    FormatLine("Coconut Chips (kg)", Math.Ceiling(coconutChips_kg)),
                    FormatLine("Nana (kg)", Math.Ceiling(nana_kg)),
                    FormatLine("Edible Flowers (kg)", Math.Ceiling(edibleFlowers_kg)),
                    FormatLine("Anis Stars (kg)", Math.Ceiling(anisStars_kg)),
                    "\n--- Equipment ---",
                    FormatLine("Shakers", shakers),
                    FormatLine("Filters", filters),
                    FormatLine("Thin Filters", thinFilters),
                    FormatLine("Jiggers", jiggers),
                    FormatLine("Long Spoons", longSpoons),
                    FormatLine("Tweezers", tweezers),
                    FormatLine("Wooden Containers", woodenContainers),
                    FormatLine("Serving Plates", servingPlates),
                    FormatLine("Ice Crusher", iceCrusher),
                    FormatLine("Brener Burner", brenerBurner),
                    FormatLine("180ml Cups", cups),
                    FormatLine("Straws", straws),
                    FormatLine("Straws Container", strawsContainer),
                    FormatLine("Dual Contraption", dualContraption),
                    FormatLine("Purerim", purerim),
                    ""
                };

                return lines;
            }
        }
        public class CocktailsNoAlcoholBar : EventBarClass
        {
            public CocktailsNoAlcoholBar() : base("Cocktails (no alcohol)", 0.3) { }

            public override string[] PrintBarSummary(int guests, double hours, double drinks)
            {
                // Equipment / Tools
                int shakers = (int)Math.Ceiling(guests / 30.0);
                int filters = shakers;
                int thinFilters = shakers;
                int jiggers = shakers * 2;
                int longSpoons = shakers;
                int tweezers = shakers;
                int woodenContainers = 3;
                int servingPlates = 3;
                int iceCrusher = 1;
                int brenerBurner = 1;
                int cups = (int)Math.Ceiling(drinks * 1.1); // avg 180ml cup
                int straws = cups;
                int strawsContainer = 1;
                int dualContraption = 1;
                int purerim = 1;

                // Consumables / Mixers
                double naturalJuice_l = drinks * 8 / 1000.0;
                double soda_l = drinks * 5 / 1000.0;
                double flavorSyrup_l = drinks * 1 / 1000.0;
                double limeJuice_l = drinks * 1 / 1000.0;
                double ice_kg = drinks * 150 / 1000.0;
                double lemons_kg = drinks * 10 / 1000.0;
                double cinnamonStick_kg = drinks * 2 / 1000.0;
                double coconutChips_kg = drinks * 2 / 1000.0;
                double driedOrange_kg = drinks * 3 / 1000.0;
                double nana_kg = drinks * 2 / 1000.0;
                double driedLemons_kg = drinks * 3 / 1000.0;
                double anisStars_kg = drinks * 1 / 1000.0;
                double edibleFlowers_kg = drinks * 2 / 1000.0;

                // Helper for formatting
                string FormatLine(string name, object value) => string.Format("{0,-23}: {1,6}", name, value);

                // Build the array
                string[] lines =
                {
            "\n=== Non-Alcoholic Cocktail Bar Summary ===\n",
            "\n--- Mixers & Addons ---",
            FormatLine("Natural Juice (L)", Math.Ceiling(naturalJuice_l)),
            FormatLine("Soda (L)", Math.Ceiling(soda_l)),
            FormatLine("Flavor Syrup (L)", Math.Ceiling(flavorSyrup_l)),
            FormatLine("Lime Juice (L)", Math.Ceiling(limeJuice_l)),
            FormatLine("Ice (kg)", Math.Ceiling(ice_kg)),
            FormatLine("Lemons (kg)", Math.Ceiling(lemons_kg)),
            FormatLine("Dried Orange (kg)", Math.Ceiling(driedOrange_kg)),
            FormatLine("Dried Lemons (kg)", Math.Ceiling(driedLemons_kg)),
            FormatLine("Cinnamon Stick (kg)", Math.Ceiling(cinnamonStick_kg)),
            FormatLine("Coconut Chips (kg)", Math.Ceiling(coconutChips_kg)),
            FormatLine("Nana (kg)", Math.Ceiling(nana_kg)),
            FormatLine("Edible Flowers (kg)", Math.Ceiling(edibleFlowers_kg)),
            FormatLine("Anis Stars (kg)", Math.Ceiling(anisStars_kg)),
            "\n--- Equipment ---",
            FormatLine("Shakers", shakers),
            FormatLine("Filters", filters),
            FormatLine("Thin Filters", thinFilters),
            FormatLine("Jiggers", jiggers),
            FormatLine("Long Spoons", longSpoons),
            FormatLine("Tweezers", tweezers),
            FormatLine("Wooden Containers", woodenContainers),
            FormatLine("Serving Plates", servingPlates),
            FormatLine("Ice Crusher", iceCrusher),
            FormatLine("Brener Burner", brenerBurner),
            FormatLine("180ml Cups", cups),
            FormatLine("Straws", straws),
            FormatLine("Straws Container", strawsContainer),
            FormatLine("Dual Contraption", dualContraption),
            FormatLine("Purerim", purerim),
            ""
        };

                return lines;
            }
        }
        public class ClassicAlcoholGoldBar      : EventBarClass
        {
            public ClassicAlcoholGoldBar() : base("Classic Alcohol Bar - Gold", 0.7) { }

            public override string[] PrintBarSummary(int guests, double hours, double drinks)
            {
                // Equipment
                int icePila = 1;
                int iceSpoon = 2;
                int straws = (int)Math.Ceiling(drinks * 1.1);
                int cups250 = (int)Math.Ceiling(drinks * 1.1);
                int whiskyCups = (int)Math.Ceiling(drinks * 0.5);
                int chaserCups = (int)Math.Ceiling(drinks * 0.3);
                int goomyBar = 1;
                int champanyera = 1;
                int shaker = (int)Math.Ceiling(guests / 30.0);
                int purerim = 1;

                // Consumables
                double smirnoff_l = drinks * 0.20 * 0.04;
                double vodkaStoly_l = drinks * 0.10 * 0.04;
                double vodkaRusky_l = drinks * 0.05 * 0.04;
                double whiskyBlondy_l = drinks * 0.10 * 0.04;
                double whiskyJameson_l = drinks * 0.10 * 0.04;
                double rum_l = drinks * 0.08 * 0.04;
                double shibasWhisky_l = drinks * 0.03 * 0.04;
                double jackDaniels_l = drinks * 0.05 * 0.04;
                double tequila_l = drinks * 0.07 * 0.04;
                double coherboGold_l = drinks * 0.02 * 0.04;
                double bushmils_l = drinks * 0.03 * 0.04;
                double arakElit_l = drinks * 0.015 * 0.04;
                double arakShalit_l = drinks * 0.015 * 0.04;
                double ginGourdon_l = drinks * 0.05 * 0.04;
                double ginLondon_l = drinks * 0.05 * 0.04;
                double martini_l = drinks * 0.12 * 0.04;
                double campari_l = drinks * 0.025 * 0.04;
                double paperoll_l = drinks * 0.01 * 0.04;
                double tubi_l = drinks * 0.01 * 0.04;
                double excel_l = drinks * 0.02 * 0.04;
                double russian_l = drinks * 0.02 * 0.04;
                double vodkaVangoh_l = drinks * 0.015 * 0.04;
                double kasasha_l = drinks * 0.015 * 0.04;
                double tripleSec_l = drinks * 0.03 * 0.04;

                // Helper for formatting
                string FormatLine(string name, object value) => string.Format("{0,-23}: {1,6}", name, value);

                // Build the array
                string[] lines =
                {
            "\n=== Classic Alcohol Bar - Gold Summary ===\n",
            "\n--- Consumables ---",
            FormatLine("Smirnoff (L)", Math.Ceiling(smirnoff_l)),
            FormatLine("Vodka Stoly (L)", Math.Ceiling(vodkaStoly_l)),
            FormatLine("Vodka Rusky (L)", Math.Ceiling(vodkaRusky_l)),
            FormatLine("Whisky Blondy (L)", Math.Ceiling(whiskyBlondy_l)),
            FormatLine("Whisky Jameson (L)", Math.Ceiling(whiskyJameson_l)),
            FormatLine("Rum (L)", Math.Ceiling(rum_l)),
            FormatLine("Shibas Whisky (L)", Math.Ceiling(shibasWhisky_l)),
            FormatLine("Jack Daniels (L)", Math.Ceiling(jackDaniels_l)),
            FormatLine("Tequila (L)", Math.Ceiling(tequila_l)),
            FormatLine("Coherbo Gold (L)", Math.Ceiling(coherboGold_l)),
            FormatLine("Bushmils (L)", Math.Ceiling(bushmils_l)),
            FormatLine("Arak Elit (L)", Math.Ceiling(arakElit_l)),
            FormatLine("Arak Shalit (L)", Math.Ceiling(arakShalit_l)),
            FormatLine("Gin Gourdon (L)", Math.Ceiling(ginGourdon_l)),
            FormatLine("Gin London (L)", Math.Ceiling(ginLondon_l)),
            FormatLine("Martini (L)", Math.Ceiling(martini_l)),
            FormatLine("Campari (L)", Math.Ceiling(campari_l)),
            FormatLine("Paperoll (L)", Math.Ceiling(paperoll_l)),
            FormatLine("Tubi (L)", Math.Ceiling(tubi_l)),
            FormatLine("Excel (L)", Math.Ceiling(excel_l)),
            FormatLine("Russian (L)", Math.Ceiling(russian_l)),
            FormatLine("Vodka Vangoh (L)", Math.Ceiling(vodkaVangoh_l)),
            FormatLine("Kasasha (L)", Math.Ceiling(kasasha_l)),
            FormatLine("Triple Sec (L)", Math.Ceiling(tripleSec_l)),
            "\n--- Equipment ---",
            FormatLine("Ice Pila", icePila),
            FormatLine("Ice Spoon", iceSpoon),
            FormatLine("Straws", straws),
            FormatLine("250ML Cups", cups250),
            FormatLine("Whisky Cups", whiskyCups),
            FormatLine("Chaser Cups", chaserCups),
            FormatLine("Goomy Bar", goomyBar),
            FormatLine("Champanyera", champanyera),
            FormatLine("Shakers", shaker),
            FormatLine("Purerim", purerim),
            ""
        };

                return lines;
            }
        }
        public class ClassicAlcoholPremiumBar : EventBarClass
        {
            public ClassicAlcoholPremiumBar() : base("Classic Alcohol Bar - Premium", 0.8) { }

            public override string[] PrintBarSummary(int guests, double hours, double drinks)
            {
                // Equipment / Tools
                int icePila = 1;
                int iceSpoon = 2;
                int straws = (int)Math.Ceiling(drinks * 1.1);
                int cups250 = (int)Math.Ceiling(drinks * 1.1);
                int whiskyCups = (int)Math.Ceiling(drinks * 0.5);
                int chaserCups = (int)Math.Ceiling(drinks * 0.3);
                int goomyBar = 1;
                int champanyera = 1;
                int shaker = (int)Math.Ceiling(guests / 30.0);
                int purerim = 1;
                int display = 1;

                // Consumables
                double smirnoff_l = drinks * 0.20 * 0.04;
                double vodkaStoly_l = drinks * 0.10 * 0.04;
                double vodkaRusky_l = drinks * 0.05 * 0.04;
                double whiskyBlondy_l = drinks * 0.10 * 0.04;
                double whiskyJameson_l = drinks * 0.10 * 0.04;
                double rum_l = drinks * 0.08 * 0.04;
                double shibasWhisky_l = drinks * 0.03 * 0.04;
                double jackDaniels_l = drinks * 0.05 * 0.04;
                double tequila_l = drinks * 0.07 * 0.04;
                double coherboGold_l = drinks * 0.02 * 0.04;
                double bushmils_l = drinks * 0.03 * 0.04;
                double arakElit_l = drinks * 0.015 * 0.04;
                double arakShalit_l = drinks * 0.015 * 0.04;
                double ginGourdon_l = drinks * 0.05 * 0.04;
                double ginLondon_l = drinks * 0.05 * 0.04;
                double martini_l = drinks * 0.12 * 0.04;
                double campari_l = drinks * 0.025 * 0.04;
                double paperoll_l = drinks * 0.01 * 0.04;
                double tubi_l = drinks * 0.01 * 0.04;
                double excel_l = drinks * 0.02 * 0.04;
                double russian_l = drinks * 0.02 * 0.04;
                double vodkaVangoh_l = drinks * 0.015 * 0.04;
                double kasasha_l = drinks * 0.015 * 0.04;
                double tripleSec_l = drinks * 0.03 * 0.04;

                double belugaVodka_l = drinks * 0.03 * 0.04;
                double gregosVodka_l = drinks * 0.02 * 0.04;
                double vodkaBalbader_l = drinks * 0.02 * 0.04;
                double vodkaKettlewine_l = drinks * 0.01 * 0.04;
                double cmparia_l = drinks * 0.02 * 0.04;
                double peroll_l = drinks * 0.01 * 0.04;
                double whiskyGlenlivette_l = drinks * 0.03 * 0.04;
                double pounders_l = drinks * 0.02 * 0.04;
                double johnnyWalkerBlack_l = drinks * 0.03 * 0.04;
                double johnnyWalkerBlondy_l = drinks * 0.02 * 0.04;
                double glenivetteKaribian_l = drinks * 0.01 * 0.04;
                double shivas12_l = drinks * 0.02 * 0.04;
                double tequilaPatron_l = drinks * 0.02 * 0.04;
                double tequilaQuoerbogold_l = drinks * 0.01 * 0.04;
                double rumAvanalab_l = drinks * 0.02 * 0.04;
                double rumCaptainMorgan_l = drinks * 0.02 * 0.04;
                double jameBombay_l = drinks * 0.02 * 0.04;
                double jinAndrix_l = drinks * 0.01 * 0.04;
                double whiskyGentlemanJack_l = drinks * 0.02 * 0.04;
                double amaretto_l = drinks * 0.02 * 0.04;
                double bitterlemon_l = drinks * 0.02 * 0.04;

                // Helper for formatting
                string FormatLine(string name, object value) => string.Format("{0,-23}: {1,6}", name, value);

                // Build the array
                string[] lines =
                {
            "\n=== Classic Alcohol Bar - Premium Summary ===\n",
            "\n--- Consumables ---",
            FormatLine("Smirnoff (L)", Math.Ceiling(smirnoff_l)),
            FormatLine("Vodka Stoly (L)", Math.Ceiling(vodkaStoly_l)),
            FormatLine("Vodka Rusky (L)", Math.Ceiling(vodkaRusky_l)),
            FormatLine("Whisky Blondy (L)", Math.Ceiling(whiskyBlondy_l)),
            FormatLine("Whisky Jameson (L)", Math.Ceiling(whiskyJameson_l)),
            FormatLine("Rum (L)", Math.Ceiling(rum_l)),
            FormatLine("Shibas Whisky (L)", Math.Ceiling(shibasWhisky_l)),
            FormatLine("Jack Daniels (L)", Math.Ceiling(jackDaniels_l)),
            FormatLine("Tequila (L)", Math.Ceiling(tequila_l)),
            FormatLine("Coherbo Gold (L)", Math.Ceiling(coherboGold_l)),
            FormatLine("Bushmils (L)", Math.Ceiling(bushmils_l)),
            FormatLine("Arak Elit (L)", Math.Ceiling(arakElit_l)),
            FormatLine("Arak Shalit (L)", Math.Ceiling(arakShalit_l)),
            FormatLine("Gin Gourdon (L)", Math.Ceiling(ginGourdon_l)),
            FormatLine("Gin London (L)", Math.Ceiling(ginLondon_l)),
            FormatLine("Martini (L)", Math.Ceiling(martini_l)),
            FormatLine("Campari (L)", Math.Ceiling(campari_l)),
            FormatLine("Paperoll (L)", Math.Ceiling(paperoll_l)),
            FormatLine("Tubi (L)", Math.Ceiling(tubi_l)),
            FormatLine("Excel (L)", Math.Ceiling(excel_l)),
            FormatLine("Russian (L)", Math.Ceiling(russian_l)),
            FormatLine("Vodka Vangoh (L)", Math.Ceiling(vodkaVangoh_l)),
            FormatLine("Kasasha (L)", Math.Ceiling(kasasha_l)),
            FormatLine("Triple Sec (L)", Math.Ceiling(tripleSec_l)),
            FormatLine("Beluga Vodka (L)", Math.Ceiling(belugaVodka_l)),
            FormatLine("Gregos Vodka (L)", Math.Ceiling(gregosVodka_l)),
            FormatLine("Vodka Balbader (L)", Math.Ceiling(vodkaBalbader_l)),
            FormatLine("Vodka Kettlewine (L)", Math.Ceiling(vodkaKettlewine_l)),
            FormatLine("Cmparia (L)", Math.Ceiling(cmparia_l)),
            FormatLine("Peroll (L)", Math.Ceiling(peroll_l)),
            FormatLine("Whisky Glenlivette (L)", Math.Ceiling(whiskyGlenlivette_l)),
            FormatLine("Pounders (L)", Math.Ceiling(pounders_l)),
            FormatLine("Johnny Walker Black (L)", Math.Ceiling(johnnyWalkerBlack_l)),
            FormatLine("Johnny Walker Blondy (L)", Math.Ceiling(johnnyWalkerBlondy_l)),
            FormatLine("Glenivette Karibian (L)", Math.Ceiling(glenivetteKaribian_l)),
            FormatLine("Shivas 12 (L)", Math.Ceiling(shivas12_l)),
            FormatLine("Tequila Patron (L)", Math.Ceiling(tequilaPatron_l)),
            FormatLine("Tequila Quoerbogold (L)", Math.Ceiling(tequilaQuoerbogold_l)),
            FormatLine("Rum Avanalab (L)", Math.Ceiling(rumAvanalab_l)),
            FormatLine("Rum Captain Morgan (L)", Math.Ceiling(rumCaptainMorgan_l)),
            FormatLine("Jame Bombay (L)", Math.Ceiling(jameBombay_l)),
            FormatLine("Jin Andrix (L)", Math.Ceiling(jinAndrix_l)),
            FormatLine("Whisky Gentleman Jack (L)", Math.Ceiling(whiskyGentlemanJack_l)),
            FormatLine("Amaretto (L)", Math.Ceiling(amaretto_l)),
            FormatLine("Bitterlemon (L)", Math.Ceiling(bitterlemon_l)),
            "\n--- Equipment ---",
            FormatLine("Ice Pila", icePila),
            FormatLine("Ice Spoon", iceSpoon),
            FormatLine("Straws", straws),
            FormatLine("250ML Cups", cups250),
            FormatLine("Whisky Cups", whiskyCups),
            FormatLine("Chaser Cups", chaserCups),
            FormatLine("Goomy Bar", goomyBar),
            FormatLine("Champanyera", champanyera),
            FormatLine("Shakers", shaker),
            FormatLine("Purerim", purerim),
            FormatLine("Display", display),
            ""
        };

                return lines;
            }
        }
        public class BeerBar : EventBarClass
        {
            public BeerBar() : base("Beer", 0.5) { }
            public override string[] PrintBarSummary(int guests, double hours, double drinks)
            {
                // Consumables
                double casbergPercent = 0.40; // % of beer drinks served as Casberg
                double tubourgPercent = 0.60; // % of beer drinks served as Tubourg
                const int cupsPerBarrel = 60; // 1 barrel = 60 cups/drinks
                int casbergCups = (int)Math.Ceiling(drinks * casbergPercent);
                int tubourgCups = (int)Math.Ceiling(drinks * tubourgPercent);
                int casbergBarrels = Math.Max(1, (int)Math.Ceiling(casbergCups / (double)cupsPerBarrel));
                int tubourgBarrels = Math.Max(1, (int)Math.Ceiling(tubourgCups / (double)cupsPerBarrel));
                int totalBarrels = casbergBarrels + tubourgBarrels;

                // Equipment / Tools
                int cups250 = (int)Math.Ceiling(drinks * 1.1); // 250ml cups
                int gasBalloons = Math.Max(1, (int)Math.Ceiling(totalBarrels / 5.0)); // Gas balloons: 1 per 5 barrels
                int beerBerez = (int)Math.Ceiling((double)guests / 150);
                int champanyeraTrashBowl = 1;
                int champanyera = 1;
                int goomyBar = 1;

                // Helper for formatting
                string FormatLine(string name, object value)
                {
                    return string.Format("{0,-23}: {1,6}", name, value);
                }

                // Build the array
                string[] lines =
                {
            "\n=== Beer Bar Summary ===\n",
            "\n--- Consumables ---",
            FormatLine("Casberg barrels (60 cups each)", casbergBarrels),
            FormatLine("Tubourg barrels (60 cups each)", tubourgBarrels),
            FormatLine("Total barrels", totalBarrels),
            "\n--- Equipment ---",
            FormatLine("250ML Cups", cups250),
            FormatLine("Gas Balloons (1 per 5 barrels)", gasBalloons),
            FormatLine("Beer Berez", beerBerez),
            FormatLine("Champanyera trash bowl", champanyeraTrashBowl),
            FormatLine("Champanyera", champanyera),
            FormatLine("Goomy Bar", goomyBar),
            ""
        };

                return lines;
            }
        }
        public class WineBar : EventBarClass
        {
            public WineBar() : base("Wine", 0.4) { }
            public override string[] PrintBarSummary(int guests, double hours, double drinks)
            {
                // ----- Consumables (bottles) -----
                int whiteWineBottles = Math.Max(1, (int)Math.Ceiling(drinks * 0.35 / 5.0));   // 35% of drinks, 5 glasses per bottle
                int redWineBottles = Math.Max(1, (int)Math.Ceiling(drinks * 0.40 / 5.0));     // 40% of drinks
                int rosetWineBottles = Math.Max(1, (int)Math.Ceiling(drinks * 0.15 / 5.0));   // 15% of drinks
                int cavaWineBottles = Math.Max(1, (int)Math.Ceiling(drinks * 0.10 / 5.0));    // 10% of drinks

                // ----- Equipment / Tools -----
                int wineOpeners = (int)Math.Ceiling((guests / 50.0) * 1.5);
                int fancyWineCups = (int)Math.Ceiling(drinks * 1.1);
                int champanyera = 1;
                int fridge = 1;
                int wineContraption = 1;

                // Helper for formatting
                string FormatLine(string name, object value)
                {
                    return string.Format("{0,-23}: {1,6}", name, value);
                }

                // Build the array
                string[] lines =
                {
            "\n=== Wine Bar Summary ===\n",
            "\n--- Consumables ---",
            FormatLine("White Wine Bottles", whiteWineBottles),
            FormatLine("Red Wine Bottles", redWineBottles),
            FormatLine("Roset Wine Bottles", rosetWineBottles),
            FormatLine("Cava Wine Bottles", cavaWineBottles),
            "\n--- Equipment ---",
            FormatLine("Wine Openers", wineOpeners),
            FormatLine("Fancy Wine Cups", fancyWineCups),
            FormatLine("Champanyera", champanyera),
            FormatLine("Fridge", fridge),
            FormatLine("Wine Contraption", wineContraption),
            ""
        };

                return lines;
            }

        }
        public class IceBaradBar : EventBarClass
        {
            public IceBaradBar() : base("Ice / Barad", 0.3) { }
            public override string[] PrintBarSummary(int guests, double hours, double drinks)
            {
                // ----- Equipment / Tools -----
                int machines = (int)Math.Ceiling(guests / 60.0);          // 1 machine per 60 guests
                int minerostaBuckets = machines;                           // 1 bucket per machine
                int matrefaUnits = machines;                               // 1 matrefa per machine
                int cups250 = (int)Math.Ceiling(drinks * 1.1);             // 250ml cups, 10% extra
                int straws = cups250;                                      // 1 straw per cup

                // ----- Consumables -----
                int iceCoffeePowder = (int)Math.Ceiling(drinks * 12);      // grams
                int iceVanillaPowder = (int)Math.Ceiling(drinks * 10);     // grams
                int milkMl = (int)Math.Ceiling(drinks * 80);               // ml
                int waterJerikans = (int)Math.Ceiling(drinks * 200 / 1000.0); // liters

                // Helper for formatting
                string FormatLine(string name, object value)
                {
                    return string.Format("{0,-23}: {1,6}", name, value);
                }

                // Build the array
                string[] lines =
                {
                    "\n=== Ice / Barad Bar Summary ===\n",
                    "\n--- Equipment ---",
                    FormatLine("Machines", machines),
                    FormatLine("Minerosta Buckets", minerostaBuckets),
                    FormatLine("Matrefa Units", matrefaUnits),
                    FormatLine("250ML Cups", cups250),
                    FormatLine("Straws", straws),
                    "\n--- Consumables ---",
                    FormatLine("Ice Coffee Powder (grams)", iceCoffeePowder),
                    FormatLine("Ice Vanilla Powder (grams)", iceVanillaPowder),
                    FormatLine("Milk (ml)", milkMl),
                    FormatLine("Water Jerikans (liters)", waterJerikans),
                    ""
                };

                return lines;
            }
        }
    }
}
