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

        static readonly IList<Tuple<string /*Filename*/, IDictionary<string, object> /*File parameters*/>> s_chuckFiles = [];
        static readonly IList<Tuple<string /*Buffer command (add, remove, play)*/, IDictionary<string, object> /*Command args (filename + file params [for add], index [for remove], or play duration [for play])*/>> s_scoreBuffer = [];


        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Error: Path to Listener ck file is required.");
                Console.WriteLine("Usage: MustangGongShow.TestConsole.exe <path-to-listener.ck>");
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
                case "show":
                    ShowChuckFiles();
                    break;
                case "add":
                    if (parts.Length > 1)
                        AddChuckFile(parts[1]);
                    else
                        Console.WriteLine("Usage: add <filename.ck>");
                    break;
                case "list":
                    ListChuckFiles();
                    break;
                case "remove":
                    if (parts.Length > 1 && int.TryParse(parts[1], out int index))
                        RemoveChuckFile(index);
                    else
                        Console.WriteLine("Usage: remove <index>");
                    break;
                case "play":
                    if (parts.Length > 1 && int.TryParse(parts[1], out int seconds))
                        PlayChuckFiles(seconds);
                    else
                        Console.WriteLine("Usage: play <seconds>");
                    break;
                case "buffer":
                    if (parts.Length > 1)
                        BufferOperation(input[(parts[0].Length + 1)..].Trim());
                    else
                        Console.WriteLine("Usage: send <command>");
                    break;
                case "send":
                    if (parts.Length > 1)
                        SendMessage(input[(parts[0].Length + 1)..].Trim());
                    else
                        Console.WriteLine("Usage: send <command>");
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
                    Console.WriteLine($"Error: File not found at: {s_oscListenerPath}");
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

        static void SendMessage(string command)
        {
            try
            {
                SendOscMessage(command);
                Console.WriteLine($"Sent OSC message: command = {command}");
                //Thread.Sleep(100);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending message: {ex.Message}");
            }
        }

        static void BufferOperation(string commandAndArgs)
        {
            try
            {
                var parts = commandAndArgs.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0)
                    return;

                var command = parts[0].ToLower();

                switch (command)
                {
                    case "add":
                        // TODO Add file with parameters, i.e. "add filename.ck param1=value1 param2=value2" 
                        break;
                    case "remove":
                        // TODO Remove file by index, i.e. "remove 1"
                        break;
                    case "copy":
                        // TODO Take contents of s_chuckFiles and append to end of s_scoreBuffer as "add" commands with parameters, clearing s_chuckFiles if optional parameter is passes as "clear", i.e. "copy" would take each entry in s_chuckFiles and create a corresponding entry in s_scoreBuffer with the command "add" and the same parameters, then clear s_chuckFiles if "clear" is included as an optional parameter, i.e. "copy clear"
                        break;
                    case "list":
                        // TODO List contents of score buffer with index numbers, i.e. "list" would print out each entry in s_scoreBuffer with its index number and command and parameters, similar to how ListChuckFiles prints out the contents of s_chuckFiles
                        break;
                    case "flush":
                        // TODO TBD - Don't do anything here yet
                        break;
                    case "save":
                        // TODO Save score buffer to file, i.e. "save filename.txt" would save the contents of s_scoreBuffer to a text file in a human-readable format that includes the command and parameters for each entry, along with the index number, similar to how ListChuckFiles prints out the contents of s_chuckFiles but in a format that can be easily read and parsed later when loading, i.e. each line in the file could represent one entry in s_scoreBuffer with the index number, command, and parameters all included in a structured format that can be parsed when loading, i.e. "1: add filename.ck param1=value1 param2=value2"
                        break;
                    case "load":
                        // TODO Load score buffer from file, i.e. "load filename.txt" would read a text file in the same format as the "save" command outputs and populate s_scoreBuffer with entries based on the contents of the file, parsing out the index number, command, and parameters for each entry and creating corresponding entries in s_scoreBuffer, i.e. each line in the file would be parsed to extract the command and parameters and create an entry in s_scoreBuffer with that information
                        break;
                    default:
                        Console.WriteLine($"Unknown command: {command}. Type 'help' for available commands.");
                        break;
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error performing buffer operation: {ex.Message}");
            }
        }

        private static void SendOscMessage(string command)
        {
            object[] oscArgs = new object[] { command };
            var message = new OscMessage("/chuck-daemon/cmd", oscArgs);
            s_sender.Send(message);
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

        static void ShowChuckFiles()
        {
            if (string.IsNullOrEmpty(s_oscListenerPath))
            {
                Console.WriteLine("Error: OscListener.ck path is not set.");
                return;
            }

            try
            {
                var listenerDirectory = Path.GetDirectoryName(s_oscListenerPath);
                var listenerFileName = Path.GetFileName(s_oscListenerPath);

                if (string.IsNullOrEmpty(listenerDirectory) || !Directory.Exists(listenerDirectory))
                {
                    Console.WriteLine($"Error: Directory not found: {listenerDirectory}");
                    return;
                }

                var ckFiles = Directory.GetFiles(listenerDirectory, "*.ck")
                    .Select(Path.GetFileName)
                    .Where(f => !string.Equals(f, listenerFileName, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(f => f)
                    .ToList();

                if (ckFiles.Count == 0)
                {
                    Console.WriteLine("No other .ck files found in the listener directory.");
                }
                else
                {
                    Console.WriteLine($"ChucK files in {listenerDirectory}:");
                    foreach (var file in ckFiles)
                    {
                        Console.WriteLine($"  {file}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error listing ChucK files: {ex.Message}");
            }
        }

        static void AddChuckFile(string filename)
        {
            if (string.IsNullOrEmpty(s_oscListenerPath))
            {
                Console.WriteLine("Error: OscListener.ck path is not set.");
                return;
            }

            try
            {
                var listenerDirectory = Path.GetDirectoryName(s_oscListenerPath);
                var listenerFileName = Path.GetFileName(s_oscListenerPath);

                if (string.IsNullOrEmpty(listenerDirectory) || !Directory.Exists(listenerDirectory))
                {
                    Console.WriteLine($"Error: Directory not found: {listenerDirectory}");
                    return;
                }

                // Get available .ck files (excluding the listener)
                var availableFiles = Directory.GetFiles(listenerDirectory, "*.ck")
                    .Select(Path.GetFileName)
                    .Where(f => !string.Equals(f, listenerFileName, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                // Check if the filename exists in the directory
                var matchingFile = availableFiles.FirstOrDefault(f => 
                    string.Equals(f, filename, StringComparison.OrdinalIgnoreCase));

                if (matchingFile == null)
                {
                    Console.WriteLine($"Error: File '{filename}' not found in the listener directory.");
                    Console.WriteLine("Use 'show' command to see available files.");
                    return;
                }

                // Add the file with an empty dictionary (allow duplicates for different parameterizations)
                var newEntry = new Tuple<string, IDictionary<string, object>>(
                    matchingFile, 
                    new Dictionary<string, object>());

                s_chuckFiles.Add(newEntry);
                Console.WriteLine($"Added: {matchingFile} (entry #{s_chuckFiles.Count})");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding ChucK file: {ex.Message}");
            }
        }

        static void ListChuckFiles()
        {
            if (s_chuckFiles.Count == 0)
            {
                Console.WriteLine("No ChucK files currently loaded.");
                Console.WriteLine("Use 'add <filename>' to add files.");
                return;
            }

            Console.WriteLine($"Loaded ChucK files ({s_chuckFiles.Count} total):");
            Console.WriteLine();

            for (int i = 0; i < s_chuckFiles.Count; i++)
            {
                var entry = s_chuckFiles[i];
                Console.WriteLine($"  [{i + 1}] {entry.Item1}");

                if (entry.Item2.Count == 0)
                {
                    Console.WriteLine("      Parameters: (none)");
                }
                else
                {
                    Console.WriteLine("      Parameters:");
                    foreach (var param in entry.Item2)
                    {
                        Console.WriteLine($"        {param.Key} = {param.Value}");
                    }
                }
                Console.WriteLine();
            }
        }

        static void RemoveChuckFile(int index)
        {
            if (s_chuckFiles.Count == 0)
            {
                Console.WriteLine("No ChucK files currently loaded.");
                return;
            }

            // Convert from 1-based user input to 0-based array index
            int arrayIndex = index - 1;

            if (arrayIndex < 0 || arrayIndex >= s_chuckFiles.Count)
            {
                Console.WriteLine($"Error: Invalid index. Valid range is 1-{s_chuckFiles.Count}");
                return;
            }

            var removedFile = s_chuckFiles[arrayIndex];
            s_chuckFiles.RemoveAt(arrayIndex);
            Console.WriteLine($"Removed: [{index}] {removedFile.Item1}");
        }

        static void PlayChuckFiles(int seconds)
        {
            // This is a temporary implementation that will use the SendOscMessage method to add files one at a time, then send a play command with the specified duration, then send the remove commands
            foreach (var entry in s_chuckFiles)
            {
                SendOscMessage($"add {entry.Item1}");
                //Thread.Sleep(100);
            }

            SendOscMessage($"play {seconds}");

            // Wait for the specified duration before sending remove commands
            //Thread.Sleep(seconds * 1000);

            foreach (var entry in s_chuckFiles)
            {
                SendOscMessage($"remove {entry.Item1}");
                //Thread.Sleep(100);
            }
        }

        static void ShowHelp()
        {
            Console.WriteLine("Available commands:");
            Console.WriteLine("  start             - Start ChucK listener process");
            Console.WriteLine("  stop              - Stop ChucK listener process");
            Console.WriteLine("  status            - Get ChucK listener status");
            Console.WriteLine("  show              - Show all .ck files in the listener directory");
            Console.WriteLine("  add <filename>    - Add a .ck file to the collection");
            Console.WriteLine("  remove <index>    - Remove a .ck file by its index number");
            Console.WriteLine("  list              - List currently loaded .ck files and their parameters");
            Console.WriteLine("  play <seconds>    - Send loaded .ck files to server and play for specified seconds");
            Console.WriteLine("  send <cmd> <args> - Send OSC message with command and arguments (DEPRECATED)");
            Console.WriteLine("  exit              - Exit the utility");
            Console.WriteLine("  help              - Show this help message");
            Console.WriteLine();
        }
    }
}