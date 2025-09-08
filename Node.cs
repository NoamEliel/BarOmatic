namespace BarOmaticGUI2.ProjectCode
{
    public class Node<T>
    {
        private T value;
        private Node<T> next;

        public Node(T value, Node<T> next = null!)
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
        public static int Count(Node<T> head)
        {
            int count = 0;
            Node<T> current = head;
            while (current != null)
            {
                count++;
                current = current.GetNext();
            }
            return count;
        }
    }
}
