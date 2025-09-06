using BarOmaticGUI2.ProjectCode;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace BarOmaticGUI2
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            EventComboBox.ItemsSource = new List<string>
            {
                "Wedding", "Singles Party", "Party", "Performance", "Pool Party",
                "Bar Mitzva", "Convention", "Company Convention", "Security Convention",
                "Sales Event", "Launch"
            };

            BarComboBox.ItemsSource = new List<string>
            {
                "Espresso Bar", "Easy Drinks", "Soda Drinks", "Shakes",
                "Cocktails", "Cocktails (no alcohol)", "Classic Alcohol Bar - Gold",
                "Classic Alcohol Bar - Premium", "Beer", "Wine", "Ice / Barad"
            };

            TimeOfDayComboBox.SelectedIndex = 0; // default 

            EventComboBox.SelectionChanged += InputChanged_UpdatePanel;
            BarComboBox.SelectionChanged += InputChanged_UpdatePanel;
            TimeOfDayComboBox.SelectionChanged += InputChanged_UpdatePanel;
            GuestCountTextBox.TextChanged += InputChanged_UpdatePanel;
            EventHoursTextBox.TextChanged += InputChanged_UpdatePanel;
        }
        private void InputChanged_UpdatePanel(object sender, EventArgs e)
        {
            if (!int.TryParse(GuestCountTextBox.Text, out int guests) || guests <= 0 || guests > 30000)
                return;

            if (!double.TryParse(EventHoursTextBox.Text, out double hours) || hours <= 0 || hours > 24.0)
                return;

            string timeOfDay = (TimeOfDayComboBox.SelectedItem as ComboBoxItem)?.Content.ToString().ToLower();
            if (string.IsNullOrEmpty(timeOfDay))
                return;

            List<string> selectedBars = BarComboBox.SelectedItems.Cast<string>().ToList();
            if (selectedBars.Count == 0)
                return;

            string selectedEvent = EventComboBox.SelectedItem as string;
            if (string.IsNullOrEmpty(selectedEvent))
                return;

            bool isSocial = EventComboBox.SelectedIndex >= 0 && EventComboBox.SelectedIndex <= 4;
            bool isProfessional = !isSocial;

            Event liveEvent = new Event(
                guests,
                hours,
                timeOfDay,
                MakeNodeFromList(selectedBars),
                isSocial,
                isProfessional
            );

            UpdateRightPanel(liveEvent, selectedBars);
            PrintSummary(liveEvent);
        }
        private Grid CreateRowGrid(Event liveEvent, List<(EventBarClass bar, double totalDrinks, double drinksPerGuest)> barInfoList)
        {
            Grid rowGrid = new Grid();
            int numBars = barInfoList.Count;

            for (int i = 0; i < numBars; i++)
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition());

            for (int i = 0; i < numBars; i++)
            {
                var (barInstance, totalDrinks, drinksPerGuest) = barInfoList[i];

                // Fix infinities
                if (double.IsInfinity(totalDrinks) || double.IsNaN(totalDrinks))
                    totalDrinks = 0;
                if (double.IsInfinity(drinksPerGuest) || double.IsNaN(drinksPerGuest))
                    drinksPerGuest = 0;

                TextBox textBox = new TextBox
                {
                    TextWrapping = TextWrapping.NoWrap,
                    Margin = new Thickness(5),
                    FontSize = 14,
                    IsReadOnly = true,
                    FontFamily = new FontFamily("Consolas"),
                    Background = Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                };

                // Use liveEvent's GuestCount and DurationHours
                var displayLines = new List<string>
                {
                    barInstance.Name + ":",
                    drinksPerGuest.ToString("0.00") + " drinks per guest, Total drinks: " + Math.Round(totalDrinks)
                };
                displayLines.AddRange(barInstance.PrintBarSummary(
                    Math.Max(1, liveEvent.GuestCount),
                    liveEvent.DurationHours,
                    totalDrinks));

                textBox.Text = string.Join(Environment.NewLine, displayLines);

                ScrollViewer scrollViewer = new ScrollViewer
                {
                    VerticalScrollBarVisibility = ScrollBarVisibility.Hidden,
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                    Content = textBox
                };

                Border border = new Border
                {
                    BorderBrush = Brushes.Black,
                    BorderThickness = new Thickness(2),
                    Child = scrollViewer,
                    Margin = new Thickness(0)
                };

                Grid.SetColumn(border, i);
                rowGrid.Children.Add(border);
            }

            return rowGrid;
        }
        private Node<string> MakeNodeFromList(List<string> list)
        {
            Node<string> head = null;
            foreach (var item in list)
                head = Node<string>.Append(head, item);
            return head;
        }
        private void UpdateRightPanel(Event liveEvent, List<string> selectedBars)
        {
            RightPanelGrid.Children.Clear();
            RightPanelGrid.ColumnDefinitions.Clear();
            RightPanelGrid.RowDefinitions.Clear();

            int numBars = selectedBars.Count;
            bool twoRows = numBars > 3;

            // Set up rows
            RightPanelGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            if (twoRows)
                RightPanelGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            int topCount = twoRows ? (numBars + 1) / 2 : numBars;

            // Convert selected bar names to EventBarClass nodes
            Node<EventBarClass> allBars = liveEvent.ConvertSelectedBars();

            // Build drinks info for all bars
            Node<(EventBarClass, double, double)> allBarsInfo = BuildBarDrinkInfo(liveEvent, allBars);

            // Separate top and bottom bars info
            var topBarList = new List<(EventBarClass bar, double totalDrinks, double drinksPerGuest)>();
            var bottomBarList = new List<(EventBarClass bar, double totalDrinks, double drinksPerGuest)>();

            int index = 0;
            Node<(EventBarClass, double, double)> cursor = allBarsInfo;
            while (cursor != null)
            {
                var val = cursor.GetValue();

                double safeTotal = double.IsInfinity(val.Item2) || double.IsNaN(val.Item2) ? 0.0 : val.Item2;
                double safePerGuest = double.IsInfinity(val.Item3) || double.IsNaN(val.Item3) ? 0.0 : val.Item3;

                if (index < topCount)
                    topBarList.Add((val.Item1, safeTotal, safePerGuest));
                else
                    bottomBarList.Add((val.Item1, safeTotal, safePerGuest));

                index++;
                cursor = cursor.GetNext();
            }

            // Create grids
            Grid topGrid = CreateRowGrid(liveEvent, topBarList);
            Grid.SetRow(topGrid, 0);
            RightPanelGrid.Children.Add(topGrid);

            if (twoRows)
            {
                Grid bottomGrid = CreateRowGrid(liveEvent, bottomBarList);
                Grid.SetRow(bottomGrid, 1);
                RightPanelGrid.Children.Add(bottomGrid);
            }
        }
        private Node<(EventBarClass, double, double)> BuildBarDrinkInfo(Event liveEvent, Node<EventBarClass> allBars)
        {
            Node<(EventBarClass, double, double)> head = null;

            // The key insight: liveEvent.CalculateDrinksPerGuest() already gives us the 
            // TOTAL drinks per guest considering all bar interactions.
            // We just need to see how that total gets split among the individual bars.

            double totalDrinksPerGuest = liveEvent.CalculateDrinksPerGuest();
            double totalDrinks = totalDrinksPerGuest * liveEvent.GuestCount;

            if (double.IsInfinity(totalDrinks) || double.IsNaN(totalDrinks))
                totalDrinks = 0.0;

            // The Event.CalculateDrinksPerGuest() method already calculated how much each bar contributes
            // We need to reverse-engineer the individual bar contributions from the total

            // Step 1: Calculate each bar's individual contribution using Event's logic
            Node<EventBarClass> tmp = allBars;
            var individualBarContributions = new List<(EventBarClass bar, double contribution)>();

            while (tmp != null)
            {
                EventBarClass bar = tmp.GetValue();

                // Calculate what this bar would contribute to drinks per guest
                // This replicates the core logic from Event.CalculateDrinksPerGuest()

                // Base rate depending on time of day
                double baseRate;
                if (liveEvent.TimeOfDay == "morning") baseRate = 0.7;
                else if (liveEvent.TimeOfDay == "afternoon") baseRate = 0.9;
                else baseRate = 1.2; // evening

                // Duration scaling (matching Event logic)
                double h = liveEvent.DurationHours;
                double barDrinks = 0.0;
                barDrinks += Math.Min(h, 3.0) * baseRate; // Peak consumption first 3 hours
                barDrinks += Math.Min(Math.Max(h - 3.0, 0.0), 3.0) * baseRate * 0.6; // Hours 3-6: moderate decline
                barDrinks += Math.Max(h - 6.0, 0.0) * baseRate * 0.25; // After 6 hours: major decline

                // Apply bar-specific popularity and social/professional preference
                double adjustedWeight = bar.GetAdjustedPopularity(liveEvent.TimeOfDay);
                double pref = 1.0;

                if (liveEvent.isSocial)
                {
                    if (bar.Name == "Easy drinks") pref = 1.3;
                    else if (bar.Name == "Soda drinks") pref = 1.02;
                    else if (bar.Name == "Cocktails" || bar.Name == "Cocktails (no alcohol)") pref = 1.15;
                    else if (bar.Name == "Classic Alcohol Bar - Gold" || bar.Name == "Classic Alcohol Bar - Premium" ||
                             bar.Name == "Beer" || bar.Name == "Wine" || bar.Name == "Ice / Barad")
                        pref = 1.3;
                }
                else if (liveEvent.isProfessional)
                {
                    if (bar.Name == "Espresso bar") pref = (liveEvent.TimeOfDay == "morning") ? 1.5 : 1.2;
                    else if (bar.Name == "Easy drinks") pref = 0.9;
                    else if (bar.Name == "Soda drinks") pref = 0.95;
                    else if (bar.Name == "Classic Alcohol Bar - Gold" || bar.Name == "Classic Alcohol Bar - Premium" ||
                             bar.Name == "Beer" || bar.Name == "Wine" || bar.Name == "Ice / Barad")
                        pref = 0.8;
                }

                barDrinks *= adjustedWeight * pref;
                individualBarContributions.Add((bar, barDrinks));
                tmp = tmp.GetNext();
            }

            // Step 2: The actual drinks per guest is distributed proportionally to individual contributions
            double totalIndividualContributions = 0.0;
            for (int i = 0; i < individualBarContributions.Count; i++)
            {
                totalIndividualContributions += individualBarContributions[i].contribution;
            }

            if (totalIndividualContributions <= 0.0)
                totalIndividualContributions = 1.0; // avoid division by zero

            // Step 3: Scale each bar's drinks based on the actual total drinks per guest
            for (int i = 0; i < individualBarContributions.Count; i++)
            {
                EventBarClass bar = individualBarContributions[i].bar;
                double contribution = individualBarContributions[i].contribution;

                // This bar gets its proportional share of the actual total drinks per guest
                double barDrinksPerGuest = totalDrinksPerGuest * (contribution / totalIndividualContributions);
                double barTotalDrinks = barDrinksPerGuest * liveEvent.GuestCount;

                head = Node<(EventBarClass, double, double)>.Append(head, (bar, barTotalDrinks, barDrinksPerGuest));
            }

            return head;
        }
        private void PrintSummary(Event liveEvent)
        {
            // Clear any previous summary text
            LeftSummaryPanel.Children.Clear();

            WorkerCalc workerCalc = new WorkerCalc();
            int workers = workerCalc.NumberOfWorkers(liveEvent.GuestCount);
            int workerCost = workerCalc.TotalCost(liveEvent.GuestCount, liveEvent.DurationHours);
            double totalDrinksPerGuest = liveEvent.CalculateDrinksPerGuest();
            int totalBars = Node<string>.Count(liveEvent.GetSelectedBarNames());

            // Calculate total drinks for the event
            int totalDrinks = (int)Math.Round(totalDrinksPerGuest * liveEvent.GuestCount);

            string summaryText =
                $"======= Event Summary =======\n" +
                $"Total Drinks Per Guest: {totalDrinksPerGuest:0.00}\n" +
                $"Total Drinks Needed: {totalDrinks}\n" +
                $"Event Duration: {liveEvent.DurationHours} hours\n" +
                $"Time of Day: {liveEvent.TimeOfDay}\n" +
                $"Total Bars: {totalBars}\n" +
                $"Guests: {liveEvent.GuestCount}\n\n" +
                $"======= Staffing & Cost =======\n" +
                $"Total Worker Cost: {workerCost} ILS\n" +
                $"Workers needed: {workers}\n";

            TextBox summaryTextBox = new TextBox
            {
                Text = summaryText,
                FontSize = 14,
                FontFamily = new FontFamily("Consolas"),
                Margin = new Thickness(5),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                TextAlignment = TextAlignment.Center,
                IsReadOnly = true, // Key change: makes text selectable but not editable
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0)
            };

            // Add the TextBox to the new StackPanel in the left panel
            LeftSummaryPanel.Children.Add(summaryTextBox);
        }
    }
}
