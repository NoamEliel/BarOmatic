using BarOmaticGUI2.ProjectCode;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BarOmaticGUI2.Properties;

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

            // Handle possible null ComboBoxItem safely
            var selectedItem = TimeOfDayComboBox.SelectedItem as ComboBoxItem;
            if (selectedItem?.Content == null)
                return;

            string timeOfDay = selectedItem.Content.ToString()!.ToLower();
            if (string.IsNullOrEmpty(timeOfDay))
                return;

            // Use OfType<string>() to avoid nullability issues
            List<string> selectedBars = BarComboBox.SelectedItems.OfType<string>().ToList();
            if (selectedBars.Count == 0)
                return;

            // Nullable string for safety
            string? selectedEvent = EventComboBox.SelectedItem as string;
            if (string.IsNullOrEmpty(selectedEvent))
                return;

            bool isSocial = EventComboBox.SelectedIndex >= 0 && EventComboBox.SelectedIndex <= 4;
            bool isProfessional = !isSocial;

            // `MakeNodeFromList` may return null, so use `!` only after ensuring it's safe
            var node = MakeNodeFromList(selectedBars);
            if (node == null)
                return;

            Event liveEvent = new Event(
                guests,
                hours,
                timeOfDay,
                node,
                isSocial,
                isProfessional
            );

            UpdateRightPanel(liveEvent, selectedBars);
            PrintSummary(liveEvent);
        }
        private Node<string>? MakeNodeFromList(List<string> list)
        {
            Node<string> head = null!;
            foreach (var item in list)
                head = Node<string>.Append(head, item);
            return head;
        }
        private void UpdateRightPanel(Event liveEvent, List<string> selectedBars)
        {
            // Clear existing layout
            RightPanelGrid.Children.Clear();
            RightPanelGrid.ColumnDefinitions.Clear();
            RightPanelGrid.RowDefinitions.Clear();

            if (selectedBars.Count == 0) return;

            int numBars = selectedBars.Count;
            bool twoRows = numBars > 3;

            // Set up row definitions
            RightPanelGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            if (twoRows)
            {
                RightPanelGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(5) }); // Horizontal Splitter Row
                RightPanelGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            }

            // Add horizontal splitter if needed
            if (twoRows)
            {
                GridSplitter horizontalSplitter = new GridSplitter
                {
                    Height = 5,
                    Background = Brushes.Black,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetRow(horizontalSplitter, 1);
                RightPanelGrid.Children.Add(horizontalSplitter);
            }

            // Convert selected bar names to EventBarClass nodes
            Node<EventBarClass> allBars = liveEvent.ConvertSelectedBars()!;

            // Build drinks info for all bars
            Node<(EventBarClass, double, double)> allBarsInfo = BuildBarDrinkInfo(liveEvent, allBars)!;

            // Store bar info in a list for easier access
            var allBarInfoList = new List<(EventBarClass bar, double totalDrinks, double drinksPerGuest)>();
            Node<(EventBarClass, double, double)> cursor = allBarsInfo;
            while (cursor != null)
            {
                allBarInfoList.Add(cursor.GetValue());
                cursor = cursor.GetNext();
            }

            // Determine the number of bars in each row
            int topRowCount = twoRows ? (numBars + 1) / 2 : numBars;

            // Split the list of bars
            var topBarList = allBarInfoList.Take(topRowCount).ToList();
            var bottomBarList = allBarInfoList.Skip(topRowCount).ToList();

            // Create and place the top row grid
            Grid topGrid = CreateRowGridWithSplitters(liveEvent, topBarList);
            Grid.SetRow(topGrid, 0);
            RightPanelGrid.Children.Add(topGrid);

            // Create and place the bottom row grid if needed
            if (twoRows)
            {
                Grid bottomGrid = CreateRowGridWithSplitters(liveEvent, bottomBarList);
                Grid.SetRow(bottomGrid, 2);
                RightPanelGrid.Children.Add(bottomGrid);
            }
        }
        // In the MainWindow.xaml.cs file, update the BuildBarDrinkInfo method.
        private Node<(EventBarClass, double, double)>? BuildBarDrinkInfo(Event liveEvent, Node<EventBarClass> allBars)
        {
            Node<(EventBarClass, double, double)> head = null!;
            double totalDrinksPerGuest = liveEvent.CalculateDrinksPerGuest();
            double totalDrinks = totalDrinksPerGuest * liveEvent.GuestCount;

            if (double.IsInfinity(totalDrinks) || double.IsNaN(totalDrinks))
                totalDrinks = 0.0;

            // Special Case: Single Bar Event
            if (Node<EventBarClass>.Count(allBars) == 1)
            {
                EventBarClass singleBar = allBars.GetValue();
                head = Node<(EventBarClass, double, double)>.Append(head, (singleBar, totalDrinks, totalDrinksPerGuest));
                return head;
            }

            // Main Logic: Multiple Bars
            Node<EventBarClass> tmp = allBars;
            var individualBarContributions = new List<(EventBarClass bar, double contribution)>();

            // Get the dampening factor once for the entire event
            double dampeningFactor = liveEvent.GetDampeningFactor();

            while (tmp != null)
            {
                EventBarClass bar = tmp.GetValue();
                double rawPopularity = bar.GetAdjustedPopularity(liveEvent.TimeOfDay, liveEvent.isSocial);
                // Apply the dampening factor directly to the raw popularity score
                double finalContribution = rawPopularity * dampeningFactor;
                individualBarContributions.Add((bar, finalContribution));
                tmp = tmp.GetNext();
            }

            double totalIndividualContributions = 0.0;
            foreach (var contribution in individualBarContributions)
            {
                totalIndividualContributions += contribution.contribution;
            }

            if (totalIndividualContributions <= 0.0)
                totalIndividualContributions = 1.0;

            // Distribute the TOTAL drinks per guest (already dampened) based on the new, dampened contributions
            foreach (var contribution in individualBarContributions)
            {
                double barDrinksPerGuest = totalDrinksPerGuest * (contribution.contribution / totalIndividualContributions);
                double barTotalDrinks = barDrinksPerGuest * liveEvent.GuestCount;
                head = Node<(EventBarClass, double, double)>.Append(head, (contribution.bar, barTotalDrinks, barDrinksPerGuest));
            }

            return head;
        }
        // Helper method to create the content for a bar's summary panel
        private ScrollViewer CreateBarSummaryPanel(EventBarClass barInstance, Event liveEvent, double totalDrinks, double drinksPerGuest)
        {
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

            return new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Hidden,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                Content = textBox
            };
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
        private Grid CreateRowGridWithSplitters(Event liveEvent, List<(EventBarClass bar, double totalDrinks, double drinksPerGuest)> barInfoList)
        {
            Grid rowGrid = new Grid();
            int numBars = barInfoList.Count;

            // Create a column for each bar and a splitter column in between
            for (int i = 0; i < numBars; i++)
            {
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                if (i < numBars - 1)
                {
                    rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(5) });
                }
            }

            // Populate the grid with bar content and vertical splitters
            for (int i = 0; i < numBars; i++)
            {
                var (barInstance, totalDrinks, drinksPerGuest) = barInfoList[i];

                Border border = new Border
                {
                    BorderBrush = Brushes.Black,
                    BorderThickness = new Thickness(2),
                    Margin = new Thickness(0),
                    Child = CreateBarSummaryPanel(barInstance, liveEvent, totalDrinks, drinksPerGuest)
                };
                Grid.SetColumn(border, i * 2);
                rowGrid.Children.Add(border);

                if (i < numBars - 1)
                {
                    GridSplitter verticalSplitter = new GridSplitter
                    {
                        Width = 5,
                        Background = Brushes.Black,
                        VerticalAlignment = VerticalAlignment.Stretch,
                        HorizontalAlignment = HorizontalAlignment.Center
                    };
                    Grid.SetColumn(verticalSplitter, (i * 2) + 1);
                    rowGrid.Children.Add(verticalSplitter);
                }
            }

            return rowGrid;
        }

    }
}
