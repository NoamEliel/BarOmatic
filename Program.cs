using BarOmatic;
using System;
using System.Security.Cryptography;

public class Program
{
    public class Node<T>
    {
        private T value;
        private Node<T> next;

        public Node(T value, Node<T> next = null)
        {
            this.value = value;
            this.next = next;
        }

        public void SetNext(Node<T> next) => this.next = next;
        public void SetValue(T value) => this.value = value;
        public T GetValue() => this.value;
        public Node<T> GetNext() => this.next;

        public static Node<T> Append(Node<T> head, T newValue)
        {
            Node<T> newNode = new Node<T>(newValue);

            if (head == null)
                return newNode;

            Node<T> current = head;
            while (current.GetNext() != null)
            {
                current = current.GetNext();
            }

            current.SetNext(newNode);
            return head;
        }
    }
    


    //functions!

    // Print the values in the linked list
    public static void Print<T>(Node<T> list)
    {
        Node<T> p = list;
        while (p != null)
        {
            Console.WriteLine(p.GetValue());
            p = p.GetNext();
        }
    }
    // Returns the length of the linked list
    public static int FindListLength<T>(Node<T> head)
    {
        int length = 0;
        while (head != null)
        {
            length++;
            head = head.GetNext();
        }
        return length;
    }
    // Deletes the last node of the list
    public static Node<T> DeleteLast<T>(Node<T> list)
    {
        if (list == null)
            return null;

        if (list.GetNext() == null) // Only one node
            return null;

        Node<T> current = list;
        while (current.GetNext().GetNext() != null)
        {
            current = current.GetNext();
        }

        current.SetNext(null);
        return list;
    }
    // Create a linked list from an array, returning the head of the list
    public static Node<T> MakeNodeList<T>(T[] array)
    {
        if (array == null || array.Length == 0)
            return null;

        Node<T> head = new Node<T>(array[0], null);
        Node<T> current = head;

        for (int i = 1; i < array.Length; i++)
        {
            current.SetNext(new Node<T>(array[i], null));
            current = current.GetNext();
        }

        return head;
    }
    //checks and handles invalid int inputs within range.
    static int ReadIntInRange(string prompt, int min, int max)
    {
        while (true)
        {
            string input = Console.ReadLine();

            // Try to convert input to an integer
            if (int.TryParse(input, out int value) && value >= min && value <= max)
            {
                return value; // valid input, return it
            }
            Console.WriteLine($"Please enter a valid number between {min} and {max}.");
        }
    }
    //checks and handles invalid int inputs.
    static int ReadPositiveInt(string prompt)
    {
        while (true)
        {
            string input = Console.ReadLine();
            if (int.TryParse(input, out int value) && value > 0)
                return value;
            Console.WriteLine("Please enter a valid positive number.");
        }
    }



    //main!
    static void Main(string[] args)
    {
        Console.WriteLine("What event type?");
        string eType = Console.ReadLine();
        bool endBarSeletion = false;
        Node<Bar> barList = null;
        bool first = true;

        Bar[] possibleBars = new Bar[10]
         {
            new Bar { Name = "Espresso Bar", HourlyRateILS = 160 },                             // Simple setup, low maintenance
            new Bar { Name = "Easy Drinks", HourlyRateILS = 140 },                              // Water, juices — minimal effort
            new Bar { Name = "Soda Drinks", HourlyRateILS = 150 },                              // Needs soda machine, but cheap input
            new Bar { Name = "Shakes", HourlyRateILS = 240 },                                   // Fruits, blenders, perishables
            new Bar { Name = "Cocktails (with printing)", HourlyRateILS = 360 },                // Alcohol + garnish + printing tech
            new Bar { Name = "Cocktails (no alcohol, with printing)", HourlyRateILS = 310 },    // No alcohol, still fancy
            new Bar { Name = "Classic Alcohol Bar - Gold", HourlyRateILS = 400 },               // Mid-range alcohol
            new Bar { Name = "Classic Alcohol Bar - Premium", HourlyRateILS = 480 },            // High-end alcohol
            new Bar { Name = "Beer - Normal/Special", HourlyRateILS = 260 },                    // Fridge, variety of beer types
            new Bar { Name = "Wine - Normal/Special", HourlyRateILS = 290 }                     // Wine preservation, higher per-glass cost
         };

        while (!endBarSeletion)
        {
            if (first)
            {

                Console.WriteLine("What bars would you like to order?:");
                Console.WriteLine("1. Espresso bar");
                Console.WriteLine("2. Easy Drinks");
                Console.WriteLine("3. Soda Drinks");
                Console.WriteLine("4. Shakes");
                Console.WriteLine("5. Cocktails (with printing)");
                Console.WriteLine("6. Cocktails (no alcohol, with printing");
                Console.WriteLine("7. Classic Alcohol Bar - Gold");
                Console.WriteLine("8. Classic Alcohol Bar - Premium");
                Console.WriteLine("9. Beer - Normal/Special");
                Console.WriteLine("10. Wine - Normal/Special");

                Console.WriteLine("0 - end bar selection");
                first = false;
            }
            int choice = ReadIntInRange("Please choose a bar (1-10):", 0, 10);
            if (!(choice == 0))
            {
                barList = Node<Bar>.Append(barList, possibleBars[choice - 1]);
            }
            else { endBarSeletion = true; }
        }
        Console.WriteLine("How many hours will the event be?");
        int hours = ReadPositiveInt("How many hours will the event be?");
        Console.WriteLine("What's the guest count?");
        int guestCount = ReadPositiveInt("What's the guest count?");


        Console.WriteLine("\nChoose a profit margin (%). Type 'ok' to lock one in.\n");
        double finalMargin = -1;
        while (true)
        {
            Console.Write("\nEnter a profit margin percentage (or type 'ok' to lock it in): ");
            string input = Console.ReadLine();

            if (input.Trim().ToLower() == "ok")
            {
                if (finalMargin != -1)
                {
                    EventType event_ = new EventType(barList, hours, guestCount, finalMargin);
                    event_.PrintSummary();
                    break;
                }
                else
                {
                    Console.WriteLine("You need to test at least one margin before confirming. Try a percentage first.");
                }
            }
            else if (double.TryParse(input, out double margin) && margin >= 0)
            {
                EventType preview = new EventType(barList, hours, guestCount, margin);
                Console.WriteLine($"\n→ Base Cost (before profit): ₪{preview.CalculateBaseCost()}");
                Console.WriteLine($"→ Customer Price (with {margin}% profit): ₪{preview.CalculateTotalCost()}\n");

                finalMargin = margin; // store last valid margin
            }
            else
            {
                Console.WriteLine("Invalid input. Please enter a valid percentage or type 'ok' to finish.");
            }
        }



    }
}
    

