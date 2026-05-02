using Vizcon.OSC;

namespace MustangGongShow.TestConsole
{
    internal class Program
    {
        // Make the sender a static or class-level field
        static UDPSender s_sender = new("127.0.0.1", 6449);

        static void Main(string[] args)
        {
            if (args.Length > 0 && int.TryParse(args[0], out int noteValue))
            {
                SendOscMessage(noteValue);

                // For a console app, keep it alive so the message actually sends
                Thread.Sleep(500);
            }
            else
            {
                Console.WriteLine("Please provide a valid integer argument.");
            }
        }

        static void SendOscMessage(int noteValue)
        {
            // Just use the existing sender
            object[] oscArgs = [noteValue];
            var message = new OscMessage("/wpf/note", oscArgs);

            s_sender.Send(message);
            Console.WriteLine($"Sent: {noteValue}");
        }
    }
}

//using Vizcon.OSC;

//namespace MustangGongShow.TestConsole
//{
//    internal class Program
//    {
//        static void Main(string[] args)
//        {
//            // Just pass the integer directly
//            SendOscMessage(7);
//        }

//        static void SendOscMessage(int noteValue)
//        {
//            // 1. Create a flat object array.
//            // Vizcon/SharpOSC expects: (string address, object[] arguments)
//            object[] oscArgs = new object[] { noteValue };

//            // 2. Create the message
//            var message = new OscMessage("/wpf/note", oscArgs);

//            // 3. Send it
//            using (var sender = new UDPSender("127.0.0.1", 6449))
//            {
//                sender.Send(message);
//            }
//        }
//    }
//}

//using Vizcon.OSC;

//namespace MustangGongShow.TestConsole
//{
//    internal class Program
//    {
//        static void Main(string[] args)
//        {
//            SendOscMessage([7]);
//        }

//        static void SendOscMessage(IEnumerable<object> noteValue)
//        {
//            // Create an explicit object array. 
//            // This prevents the library from trying to "wrap" your int in a custom list type.
//            object[] args = [noteValue];

//            var message = new OscMessage("/wpf/note", args);
//            var sender = new UDPSender("127.0.0.1", 6449);

//            sender.Send(message);
//        }
//    }
//}
