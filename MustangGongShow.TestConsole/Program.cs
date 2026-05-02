using System.Diagnostics;
using Vizcon.OSC;

namespace MustangGongShow.TestConsole
{
    internal class Program
    {
        static readonly UDPSender s_sender = new("127.0.0.1", 6449);
        static Process? s_chuckProcess = null;
        static bool s_running = true;
        static string? s_oscListenerPath;

        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Error: Path to OscListener.ck file is required.");
                Console.WriteLine("Usage: MustangGongShow.TestConsole.exe <path-to-osclistener.ck>");
                return;
            }

            s_oscListenerPath = args[0].Replace(@"\", @"\\");

            Console.WriteLine("ChucK Listener Control Utility");
            Console.WriteLine("==============================");
            Console.WriteLine($"OscListener.ck path: {args[0]}");
            Console.WriteLine();

            ShowHelp();

            while (s_running)
            {
                Console.Write("> ");
                var input = Console.ReadLine()?.Trim();

                if (string.IsNullOrEmpty(input))
                    continue;

                ProcessCommand(input);
            }
        }

        static void ProcessCommand(string input)
        {
            var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
                return;

            var command = parts[0].ToLower();

            switch (command)
            {
                case "start":
                    StartChuckListener();
                    break;
                case "stop":
                    StopChuckListener();
                    break;
                case "status":
                    GetListenerStatus();
                    break;
                case "send":
                    if (parts.Length > 1 && int.TryParse(parts[1], out int noteValue))
                        SendMessage(noteValue);
                    else
                        Console.WriteLine("Usage: send <note_value>");
                    break;
                case "exit":
                case "quit":
                    Exit();
                    break;
                case "help":
                    ShowHelp();
                    break;
                default:
                    Console.WriteLine($"Unknown command: {command}. Type 'help' for available commands.");
                    break;
            }
        }

        static void StartChuckListener()
        {
            if (s_chuckProcess != null && !s_chuckProcess.HasExited)
            {
                Console.WriteLine("ChucK listener is already running.");
                return;
            }

            try
            {
                if (!File.Exists(s_oscListenerPath))
                {
                    Console.WriteLine($"Error: osclistener.ck not found at: {s_oscListenerPath}");
                    return;
                }

                var startInfo = new ProcessStartInfo
                {
                    FileName = "chuck",
                    Arguments = $"\"{s_oscListenerPath}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                s_chuckProcess = Process.Start(startInfo);

                if (s_chuckProcess != null)
                {
                    Console.WriteLine($"ChucK listener started (PID: {s_chuckProcess.Id})");
                    Console.WriteLine($"Script: {s_oscListenerPath}");
                }
                else
                {
                    Console.WriteLine("Failed to start ChucK listener.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error starting ChucK listener: {ex.Message}");
            }
        }

        static void StopChuckListener()
        {
            if (s_chuckProcess == null || s_chuckProcess.HasExited)
            {
                Console.WriteLine("ChucK listener is not running.");
                return;
            }

            try
            {
                s_chuckProcess.Kill();
                s_chuckProcess.WaitForExit(5000);
                s_chuckProcess.Dispose();
                s_chuckProcess = null;
                Console.WriteLine("ChucK listener stopped.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error stopping ChucK listener: {ex.Message}");
            }
        }

        static void GetListenerStatus()
        {
            if (s_chuckProcess == null)
            {
                Console.WriteLine("Status: Not started");
            }
            else if (s_chuckProcess.HasExited)
            {
                Console.WriteLine($"Status: Stopped (Exit code: {s_chuckProcess.ExitCode})");
            }
            else
            {
                Console.WriteLine($"Status: Running (PID: {s_chuckProcess.Id})");
                Console.WriteLine($"Start time: {s_chuckProcess.StartTime}");
                Console.WriteLine($"CPU time: {s_chuckProcess.TotalProcessorTime}");
            }
        }

        static void SendMessage(int noteValue)
        {
            try
            {
                object[] oscArgs = [noteValue];
                var message = new OscMessage("/wpf/note", oscArgs);

                s_sender.Send(message);
                Console.WriteLine($"Sent OSC message: note = {noteValue}");
                Thread.Sleep(100);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending message: {ex.Message}");
            }
        }

        static void Exit()
        {
            Console.WriteLine("Shutting down...");

            if (s_chuckProcess != null && !s_chuckProcess.HasExited)
            {
                Console.WriteLine("Stopping ChucK listener...");
                StopChuckListener();
            }

            s_running = false;
        }

        static void ShowHelp()
        {
            Console.WriteLine("Available commands:");
            Console.WriteLine("  start             - Start ChucK listener process with OscListener.ck");
            Console.WriteLine("  stop              - Stop ChucK listener process");
            Console.WriteLine("  status            - Get ChucK listener status");
            Console.WriteLine("  send <note>       - Send OSC message with note value");
            Console.WriteLine("  exit              - Exit the utility");
            Console.WriteLine("  help              - Show this help message");
            Console.WriteLine();
        }
    }
}