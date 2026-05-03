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
                        AddChuckFile(input[(parts[0].Length + 1)..].Trim());
                    else
                        Console.WriteLine("Usage: add <filename.ck> [param1=value1 param2=value2 ...]");
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
                case "clear":
                    ClearChuckFiles();
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
                SendOscMessage([command]);
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
                var parameters = new Dictionary<string, object>();
                Tuple<string, IDictionary<string, object>> entry;
                string key = string.Empty;

                switch (command)
                {
                    case "addfile":
                        // Add "add file" with parameters to s_scoreBuffer
                        if (parts.Length < 3)
                        {
                            Console.WriteLine("Usage: addfile <filename.ck> <key> [param1=value1 param2=value2 ...]");
                            break;
                        }

                        var filename = parts[1];
                        key = parts[2];

                        // Store the filename and entry key as a parameter
                        parameters["filename"] = filename;
                        parameters["key"] = key;

                        // Parse other parameters (param1=value1 param2=value2 ...)
                        for (int i = 3; i < parts.Length; i++)
                        {
                            var paramParts = parts[i].Split('=', 2);
                            if (paramParts.Length == 2)
                            {
                                parameters[paramParts[0]] = paramParts[1];
                            }
                            else
                            {
                                Console.WriteLine($"Warning: Invalid parameter format '{parts[i]}'. Expected format: param=value");
                            }
                        }

                        entry = new Tuple<string, IDictionary<string, object>>("add", parameters);
                        s_scoreBuffer.Add(entry);
                        Console.WriteLine($"Added to score buffer: {filename} ({key}) (entry #{s_scoreBuffer.Count})");

                        if (parameters.Count > 2) // More than just filename and key
                        {
                            Console.WriteLine("  Parameters:");
                            foreach (var param in parameters.Where(p => p.Key != "filename" && p.Key != "key"))
                            {
                                Console.WriteLine($"    {param.Key} = {param.Value}");
                            }
                        }
                        break;
                    case "addplay":
                        // Add "play" with parameters to s_scoreBuffer
                        if (parts.Length < 2)
                        {
                            Console.WriteLine("Usage: addplay <duration in seconds> [param1=value1 param2=value2 ...]");
                            break;
                        }

                        var duration = parts[1];

                        // Store the duration as a parameter
                        parameters["duration"] = duration;

                        // Parse parameters (param1=value1 param2=value2 ...)
                        for (int i = 2; i < parts.Length; i++)
                        {
                            var paramParts = parts[i].Split('=', 2);
                            if (paramParts.Length == 2)
                            {
                                parameters[paramParts[0]] = paramParts[1];
                            }
                            else
                            {
                                Console.WriteLine($"Warning: Invalid parameter format '{parts[i]}'. Expected format: param=value");
                            }
                        }

                        entry = new Tuple<string, IDictionary<string, object>>("play", parameters);
                        s_scoreBuffer.Add(entry);
                        Console.WriteLine($"Added to score buffer: {duration} (entry #{s_scoreBuffer.Count})");

                        if (parameters.Count > 1) // More than just duration
                        {
                            Console.WriteLine("  Parameters:");
                            foreach (var param in parameters.Where(p => p.Key != "duration"))
                            {
                                Console.WriteLine($"    {param.Key} = {param.Value}");
                            }
                        }
                        break;
                    case "remfile":
                        // Add "remove file" command that takes a key to remove a file + params from the machine on the server side
                        // NB: This is different from the "remove" command which removes entries from the local score buffer 
                        if (parts.Length < 2)
                        {
                            Console.WriteLine("Usage: remfile <key>");
                            break;
                        }

                        key = parts[1];

                        // Store the file key as a parameter
                        parameters["key"] = key;

                        // Parse other parameters (param1=value1 param2=value2 ...)
                        for (int i = 2; i < parts.Length; i++)
                        {
                            var paramParts = parts[i].Split('=', 2);
                            if (paramParts.Length == 2)
                            {
                                parameters[paramParts[0]] = paramParts[1];
                            }
                            else
                            {
                                Console.WriteLine($"Warning: Invalid parameter format '{parts[i]}'. Expected format: param=value");
                            }
                        }

                        entry = new Tuple<string, IDictionary<string, object>>("remove", parameters);
                        s_scoreBuffer.Add(entry);
                        Console.WriteLine($"Added to score buffer: {key} (entry #{s_scoreBuffer.Count})");

                        if (parameters.Count > 1) // More than just key
                        {
                            Console.WriteLine("  Parameters:");
                            foreach (var param in parameters.Where(p => p.Key != "key"))
                            {
                                Console.WriteLine($"    {param.Key} = {param.Value}");
                            }
                        }
                        break;
                    case "remove":
                        // Remove entries by index or range from s_scoreBuffer, i.e. "remove 1", "remove 1 3 5", "remove 1-3", "remove 1-3 5 7-9"
                        if (parts.Length < 2)
                        {
                            Console.WriteLine("Usage: remove <index|range> [index|range ...]");
                            Console.WriteLine("Examples: remove 1, remove 1 3 5, remove 1-3, remove 1-3 5 7-9");
                            break;
                        }

                        if (s_scoreBuffer.Count == 0)
                        {
                            Console.WriteLine("Score buffer is empty.");
                            break;
                        }

                        // Collect all indices to remove (convert to 0-based)
                        var indicesToRemove = new HashSet<int>();
                        bool hasError = false;

                        for (int i = 1; i < parts.Length; i++)
                        {
                            if (parts[i].Contains('-'))
                            {
                                // Handle range (e.g., "1-3")
                                var rangeParts = parts[i].Split('-', 2);
                                if (rangeParts.Length != 2 ||
                                    !int.TryParse(rangeParts[0], out int rangeStart) ||
                                    !int.TryParse(rangeParts[1], out int rangeEnd))
                                {
                                    Console.WriteLine($"Error: Invalid range format '{parts[i]}'. Use format: <start>-<end>");
                                    hasError = true;
                                    break;
                                }

                                // Convert to 0-based and validate
                                int startIdx = rangeStart - 1;
                                int endIdx = rangeEnd - 1;

                                if (startIdx < 0 || endIdx >= s_scoreBuffer.Count || startIdx > endIdx)
                                {
                                    Console.WriteLine($"Error: Invalid range '{parts[i]}'. Valid range is 1-{s_scoreBuffer.Count}");
                                    hasError = true;
                                    break;
                                }

                                // Add all indices in the range
                                for (int idx = startIdx; idx <= endIdx; idx++)
                                {
                                    indicesToRemove.Add(idx);
                                }
                            }
                            else
                            {
                                // Handle single index
                                if (!int.TryParse(parts[i], out int singleIndex))
                                {
                                    Console.WriteLine($"Error: '{parts[i]}' is not a valid number.");
                                    hasError = true;
                                    break;
                                }

                                // Convert to 0-based and validate
                                int idx = singleIndex - 1;
                                if (idx < 0 || idx >= s_scoreBuffer.Count)
                                {
                                    Console.WriteLine($"Error: Invalid index {singleIndex}. Valid range is 1-{s_scoreBuffer.Count}");
                                    hasError = true;
                                    break;
                                }

                                indicesToRemove.Add(idx);
                            }
                        }

                        if (hasError)
                        {
                            break;
                        }

                        // Sort indices in descending order to remove from end to start (avoids index shifting issues)
                        var sortedIndices = indicesToRemove.OrderByDescending(x => x).ToList();

                        // Remove entries and collect information for display
                        var removedEntries = new List<(int originalIndex, string commandName, string fileName)>();
                        foreach (var idx in sortedIndices)
                        {
                            var removedEntry = s_scoreBuffer[idx];
                            var removedFilename = removedEntry.Item2.ContainsKey("filename") ? removedEntry.Item2["filename"].ToString() : "";
                            removedEntries.Add((idx + 1, removedEntry.Item1, removedFilename ?? ""));
                            s_scoreBuffer.RemoveAt(idx);
                        }

                        // Display what was removed (in original order)
                        removedEntries.Reverse();
                        Console.WriteLine($"Removed {removedEntries.Count} entr{(removedEntries.Count == 1 ? "y" : "ies")}:");
                        foreach (var (originalIndex, commandName, fileName) in removedEntries)
                        {
                            Console.Write($"  [{originalIndex}] Command: {commandName}");
                            if (!string.IsNullOrEmpty(fileName))
                            {
                                Console.Write($" - {fileName}");
                            }
                            Console.WriteLine();
                        }
                        break;
                    case "move":
                        // Move entries from one index to another in s_scoreBuffer, i.e. "move 1 3" or "move 1-3 5"
                        if (parts.Length < 3)
                        {
                            Console.WriteLine("Usage: move <from-index> <to-index> or move <from-index>-<to-index> <destination>");
                            break;
                        }

                        if (s_scoreBuffer.Count == 0)
                        {
                            Console.WriteLine("Score buffer is empty.");
                            break;
                        }

                        // Check if it's a range move (e.g., "1-3")
                        if (parts[1].Contains('-'))
                        {
                            var rangeParts = parts[1].Split('-', 2);
                            if (rangeParts.Length != 2 ||
                                !int.TryParse(rangeParts[0], out int rangeStart) ||
                                !int.TryParse(rangeParts[1], out int rangeEnd) ||
                                !int.TryParse(parts[2], out int rangeDest))
                            {
                                Console.WriteLine("Error: Invalid range format. Use format: move <start>-<end> <destination>");
                                break;
                            }

                            // Convert to 0-based indices
                            int startIdx = rangeStart - 1;
                            int endIdx = rangeEnd - 1;
                            int destIdx = rangeDest - 1;

                            // Validate indices
                            if (startIdx < 0 || startIdx >= s_scoreBuffer.Count ||
                                endIdx < 0 || endIdx >= s_scoreBuffer.Count ||
                                startIdx > endIdx)
                            {
                                Console.WriteLine($"Error: Invalid range. Valid range is 1-{s_scoreBuffer.Count}");
                                break;
                            }

                            if (destIdx < 0 || destIdx > s_scoreBuffer.Count)
                            {
                                Console.WriteLine($"Error: Invalid destination. Valid range is 1-{s_scoreBuffer.Count}");
                                break;
                            }

                            // Extract the range of entries
                            int count = endIdx - startIdx + 1;
                            var movedEntries = new List<Tuple<string, IDictionary<string, object>>>();
                            for (int i = startIdx; i <= endIdx; i++)
                            {
                                movedEntries.Add(s_scoreBuffer[i]);
                            }

                            // Remove the entries from their original positions
                            for (int i = 0; i < count; i++)
                            {
                                s_scoreBuffer.RemoveAt(startIdx);
                            }

                            // Adjust destination index if needed (if destination was after the removed range)
                            if (destIdx > startIdx)
                            {
                                destIdx -= count;
                            }

                            // Insert at the new position
                            for (int i = 0; i < movedEntries.Count; i++)
                            {
                                s_scoreBuffer.Insert(destIdx + i, movedEntries[i]);
                            }

                            Console.WriteLine($"Moved entries [{rangeStart}-{rangeEnd}] to position {rangeDest}");
                        }
                        else
                        {
                            // Single entry move
                            if (!int.TryParse(parts[1], out int fromIndex) ||
                                !int.TryParse(parts[2], out int toIndex))
                            {
                                Console.WriteLine("Error: Indices must be valid numbers.");
                                break;
                            }

                            // Convert to 0-based indices
                            int fromIdx = fromIndex - 1;
                            int toIdx = toIndex - 1;

                            // Validate indices
                            if (fromIdx < 0 || fromIdx >= s_scoreBuffer.Count)
                            {
                                Console.WriteLine($"Error: Invalid from-index. Valid range is 1-{s_scoreBuffer.Count}");
                                break;
                            }

                            if (toIdx < 0 || toIdx >= s_scoreBuffer.Count)
                            {
                                Console.WriteLine($"Error: Invalid to-index. Valid range is 1-{s_scoreBuffer.Count}");
                                break;
                            }

                            if (fromIdx == toIdx)
                            {
                                Console.WriteLine("Source and destination are the same.");
                                break;
                            }

                            // Remove from old position
                            var movedEntry = s_scoreBuffer[fromIdx];
                            s_scoreBuffer.RemoveAt(fromIdx);

                            // Adjust toIndex if needed
                            if (toIdx > fromIdx)
                            {
                                toIdx--;
                            }

                            // Insert at new position
                            s_scoreBuffer.Insert(toIdx, movedEntry);

                            Console.Write($"Moved: [{fromIndex}] to [{toIndex}] - Command: {movedEntry.Item1}");
                            if (movedEntry.Item2.ContainsKey("filename"))
                            {
                                Console.Write($" - {movedEntry.Item2["filename"]}");
                            }
                            Console.WriteLine();
                        }
                        break;
                    case "copy":
                        // Take contents of s_chuckFiles and append to end of s_scoreBuffer as "add" commands with parameters
                        // Optional "clear" parameter will clear s_chuckFiles after copying
                        if (s_chuckFiles.Count == 0)
                        {
                            Console.WriteLine("No ChucK files to copy. Use 'add' command to add files first.");
                            break;
                        }

                        bool clearAfterCopy = parts.Length > 1 && parts[1].ToLower() == "clear";

                        int copiedCount = 0;
                        foreach (var chuckFileEntry in s_chuckFiles)
                        {
                            // Create a new dictionary with the filename and all parameters
                            var bufferParameters = new Dictionary<string, object>();
                            bufferParameters["filename"] = chuckFileEntry.Item1;

                            // Copy all parameters from the chuck file entry
                            foreach (var param in chuckFileEntry.Item2)
                            {
                                bufferParameters[param.Key] = param.Value;
                            }

                            // Add to score buffer as an "add" command
                            var bufferEntry = new Tuple<string, IDictionary<string, object>>("add", bufferParameters);
                            s_scoreBuffer.Add(bufferEntry);
                            copiedCount++;
                        }

                        Console.WriteLine($"Copied {copiedCount} entr{(copiedCount == 1 ? "y" : "ies")} from ChucK files to score buffer.");

                        if (clearAfterCopy)
                        {
                            s_chuckFiles.Clear();
                            Console.WriteLine("Cleared ChucK files collection.");
                        }
                        break;
                    case "clear":
                        // Clear all entries from score buffer
                        s_scoreBuffer.Clear();
                        Console.WriteLine("Cleared score buffer.");
                        break;
                    case "list":
                        // List contents of score buffer with index numbers
                        if (s_scoreBuffer.Count == 0)
                        {
                            Console.WriteLine("Score buffer is empty.");
                            Console.WriteLine("Use 'buffer addfile <filename.ck>' or 'buffer addplay <seconds>' to add entries.");
                            break;
                        }

                        Console.WriteLine($"Score buffer contents ({s_scoreBuffer.Count} total):");
                        Console.WriteLine();

                        for (int i = 0; i < s_scoreBuffer.Count; i++)
                        {
                            var bufferEntry = s_scoreBuffer[i];

                            // Display entry differently based on command type
                            if (bufferEntry.Item1 == "add" && bufferEntry.Item2.ContainsKey("filename"))
                            {
                                var keyDisplay = bufferEntry.Item2.ContainsKey("key") ? $" (key: {bufferEntry.Item2["key"]})" : "";
                                Console.WriteLine($"  [{i + 1}] ADD FILE: {bufferEntry.Item2["filename"]}{keyDisplay}");
                            }
                            else if (bufferEntry.Item1 == "play" && bufferEntry.Item2.ContainsKey("duration"))
                            {
                                var keyDisplay = bufferEntry.Item2.ContainsKey("key") ? $" (key: {bufferEntry.Item2["key"]})" : "";
                                Console.WriteLine($"  [{i + 1}] PLAY: {bufferEntry.Item2["duration"]} seconds{keyDisplay}");
                            }
                            else if (bufferEntry.Item1 == "remove" && bufferEntry.Item2.ContainsKey("key"))
                            {
                                Console.WriteLine($"  [{i + 1}] REMOVE FILE: key = {bufferEntry.Item2["key"]}");
                            }
                            else
                            {
                                Console.WriteLine($"  [{i + 1}] Command: {bufferEntry.Item1}");
                            }

                            if (bufferEntry.Item2.Count == 0)
                            {
                                Console.WriteLine("      Parameters: (none)");
                            }
                            else
                            {
                                Console.WriteLine("      Parameters:");
                                foreach (var param in bufferEntry.Item2)
                                {
                                    // Skip key indicators in detailed list since they're already shown in the header
                                    if ((bufferEntry.Item1 == "add" && (param.Key == "filename" || param.Key == "key")) ||
                                        (bufferEntry.Item1 == "play" && (param.Key == "duration" || param.Key == "key")) ||
                                        (bufferEntry.Item1 == "remove" && param.Key == "key"))
                                    {
                                        continue;
                                    }
                                    Console.WriteLine($"        {param.Key} = {param.Value}");
                                }
                            }
                            Console.WriteLine();
                        }
                        break;
                    case "flush":
                        // Send the score buffer entries to the server in one batch
                        if (s_scoreBuffer.Count == 0)
                        {
                            Console.WriteLine("Score buffer is empty. Nothing to flush.");
                            break;
                        }

                        // Build OSC arguments array
                        var flushArgs = new List<object>();
                        flushArgs.Add("score");  // 1st argument
                        flushArgs.Add(s_scoreBuffer.Count);  // 2nd argument (number of commands)

                        // Build command strings for each entry in the buffer
                        foreach (var bufferEntry in s_scoreBuffer)
                        {
                            var commandString = "";

                            if (bufferEntry.Item1 == "add")
                            {
                                // Format: "add <filename.ck> <key> param1=value1 param2=value2..."
                                if (bufferEntry.Item2.ContainsKey("filename") && bufferEntry.Item2.ContainsKey("key"))
                                {
                                    commandString = $"add {bufferEntry.Item2["filename"]} {bufferEntry.Item2["key"]}";

                                    // Add other parameters
                                    foreach (var param in bufferEntry.Item2)
                                    {
                                        if (param.Key != "filename" && param.Key != "key")
                                        {
                                            commandString += $" {param.Key}={param.Value}";
                                        }
                                    }
                                }
                            }
                            else if (bufferEntry.Item1 == "play")
                            {
                                // Format: "play <duration> param1=value1 param2=value2..."
                                if (bufferEntry.Item2.ContainsKey("duration"))
                                {
                                    commandString = $"play {bufferEntry.Item2["duration"]}";

                                    // Add other parameters
                                    foreach (var param in bufferEntry.Item2)
                                    {
                                        if (param.Key != "duration")
                                        {
                                            commandString += $" {param.Key}={param.Value}";
                                        }
                                    }
                                }
                            }
                            else if (bufferEntry.Item1 == "remove")
                            {
                                // Format: "remove <key>"
                                if (bufferEntry.Item2.ContainsKey("key"))
                                {
                                    commandString = $"remove {bufferEntry.Item2["key"]}";
                                }
                            }

                            if (!string.IsNullOrEmpty(commandString))
                            {
                                flushArgs.Add(commandString);
                            }
                        }

                        // Send the OSC message directly
                        var flushMessage = new OscMessage("/chuck-daemon/cmd", flushArgs.ToArray());
                        s_sender.Send(flushMessage);
                        Console.WriteLine($"Flushed {s_scoreBuffer.Count} command{(s_scoreBuffer.Count == 1 ? "" : "s")} to server.");
                        break;
                    case "save":
                        // Save score buffer to file with .mgs extension
                        if (parts.Length < 2)
                        {
                            Console.WriteLine("Usage: save <filename>");
                            break;
                        }

                        if (s_scoreBuffer.Count == 0)
                        {
                            Console.WriteLine("Score buffer is empty. Nothing to save.");
                            break;
                        }

                        try
                        {
                            var saveFilename = parts[1];

                            // Add .mgs extension if not present
                            if (!saveFilename.EndsWith(".mgs", StringComparison.OrdinalIgnoreCase))
                            {
                                saveFilename += ".mgs";
                            }

                            // Get the listener directory to save the file there
                            var listenerDirectory = Path.GetDirectoryName(s_oscListenerPath);
                            if (string.IsNullOrEmpty(listenerDirectory))
                            {
                                Console.WriteLine("Error: Could not determine listener directory.");
                                break;
                            }

                            var fullPath = Path.Combine(listenerDirectory, saveFilename);

                            // Build JSON content
                            var jsonLines = new List<string>();
                            jsonLines.Add("{");
                            jsonLines.Add("  \"scoreBuffer\": [");

                            for (int i = 0; i < s_scoreBuffer.Count; i++)
                            {
                                var bufferEntry = s_scoreBuffer[i];
                                jsonLines.Add("    {");
                                jsonLines.Add($"      \"index\": {i + 1},");
                                jsonLines.Add($"      \"command\": \"{bufferEntry.Item1}\",");
                                jsonLines.Add("      \"parameters\": {");

                                var paramList = new List<string>();
                                foreach (var param in bufferEntry.Item2)
                                {
                                    // Escape quotes in values
                                    var value = param.Value.ToString()?.Replace("\"", "\\\"") ?? "";
                                    paramList.Add($"        \"{param.Key}\": \"{value}\"");
                                }

                                jsonLines.Add(string.Join(",\n", paramList));
                                jsonLines.Add("      }");

                                if (i < s_scoreBuffer.Count - 1)
                                {
                                    jsonLines.Add("    },");
                                }
                                else
                                {
                                    jsonLines.Add("    }");
                                }
                            }

                            jsonLines.Add("  ]");
                            jsonLines.Add("}");

                            // Write to file
                            File.WriteAllText(fullPath, string.Join(Environment.NewLine, jsonLines));
                            Console.WriteLine($"Saved {s_scoreBuffer.Count} entr{(s_scoreBuffer.Count == 1 ? "y" : "ies")} to: {fullPath}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error saving score buffer: {ex.Message}");
                        }
                        break;
                    case "load":
                        // Load score buffer from .mgs file
                        if (parts.Length < 2)
                        {
                            Console.WriteLine("Usage: load <filename>");
                            break;
                        }

                        try
                        {
                            var loadFilename = parts[1];

                            // Add .mgs extension if not present
                            if (!loadFilename.EndsWith(".mgs", StringComparison.OrdinalIgnoreCase))
                            {
                                loadFilename += ".mgs";
                            }

                            // Get the listener directory to load the file from
                            var listenerDirectory = Path.GetDirectoryName(s_oscListenerPath);
                            if (string.IsNullOrEmpty(listenerDirectory))
                            {
                                Console.WriteLine("Error: Could not determine listener directory.");
                                break;
                            }

                            var fullPath = Path.Combine(listenerDirectory, loadFilename);

                            if (!File.Exists(fullPath))
                            {
                                Console.WriteLine($"Error: File not found: {fullPath}");
                                Console.WriteLine("Use 'buffer show' to see available .mgs files.");
                                break;
                            }

                            // Read and parse JSON file
                            var jsonContent = File.ReadAllText(fullPath);
                            using var jsonDoc = System.Text.Json.JsonDocument.Parse(jsonContent);

                            var root = jsonDoc.RootElement;
                            if (!root.TryGetProperty("scoreBuffer", out var scoreBufferArray))
                            {
                                Console.WriteLine("Error: Invalid .mgs file format - missing 'scoreBuffer' property.");
                                break;
                            }

                            // Clear existing buffer
                            s_scoreBuffer.Clear();

                            // Load entries
                            int loadedCount = 0;
                            foreach (var entryElement in scoreBufferArray.EnumerateArray())
                            {
                                if (!entryElement.TryGetProperty("command", out var commandElement))
                                {
                                    Console.WriteLine($"Warning: Skipping entry without 'command' property.");
                                    continue;
                                }

                                var commandName = commandElement.GetString();
                                if (string.IsNullOrEmpty(commandName))
                                {
                                    Console.WriteLine($"Warning: Skipping entry with empty command.");
                                    continue;
                                }

                                var entryParameters = new Dictionary<string, object>();

                                if (entryElement.TryGetProperty("parameters", out var paramsElement))
                                {
                                    foreach (var param in paramsElement.EnumerateObject())
                                    {
                                        entryParameters[param.Name] = param.Value.GetString() ?? "";
                                    }
                                }

                                var bufferEntry = new Tuple<string, IDictionary<string, object>>(commandName, entryParameters);
                                s_scoreBuffer.Add(bufferEntry);
                                loadedCount++;
                            }

                            Console.WriteLine($"Loaded {loadedCount} entr{(loadedCount == 1 ? "y" : "ies")} from: {fullPath}");
                        }
                        catch (System.Text.Json.JsonException ex)
                        {
                            Console.WriteLine($"Error parsing JSON file: {ex.Message}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error loading score buffer: {ex.Message}");
                        }
                        break;
                    case "show":
                        // Show MGS files in the listener directory
                        if (string.IsNullOrEmpty(s_oscListenerPath))
                        {
                            Console.WriteLine("Error: OscListener.ck path is not set.");
                            break;
                        }

                        try
                        {
                            var listenerDirectory = Path.GetDirectoryName(s_oscListenerPath);

                            if (string.IsNullOrEmpty(listenerDirectory) || !Directory.Exists(listenerDirectory))
                            {
                                Console.WriteLine($"Error: Directory not found: {listenerDirectory}");
                                break;
                            }

                            var mgsFiles = Directory.GetFiles(listenerDirectory, "*.mgs")
                                .Select(Path.GetFileName)
                                .OrderBy(f => f)
                                .ToList();

                            if (mgsFiles.Count == 0)
                            {
                                Console.WriteLine("No .mgs files found in the listener directory.");
                            }
                            else
                            {
                                Console.WriteLine($"MGS files in {listenerDirectory}:");
                                foreach (var file in mgsFiles)
                                {
                                    Console.WriteLine($"  {file}");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error listing MGS files: {ex.Message}");
                        }
                        break;
                    case "help":
                        // Provide help for buffer operations
                        Console.WriteLine("Buffer Operations Help");
                        Console.WriteLine("=====================");
                        Console.WriteLine();
                        Console.WriteLine("The score buffer allows you to stage commands that will be sent to the ChucK");
                        Console.WriteLine("server in a batch when you flush. You can add files, play commands, and remove");
                        Console.WriteLine("commands, then arrange them before sending.");
                        Console.WriteLine();
                        Console.WriteLine("Available buffer commands:");
                        Console.WriteLine();
                        Console.WriteLine("  addfile <filename.ck> <key> [param=value ...]");
                        Console.WriteLine("    Add a file to the buffer with a unique key and optional parameters");
                        Console.WriteLine("    Example: buffer addfile mysound.ck sound1 freq=440 gain=0.8");
                        Console.WriteLine();
                        Console.WriteLine("  addplay <duration> [key=value] [param=value ...]");
                        Console.WriteLine("    Add a play command with duration in seconds and optional parameters");
                        Console.WriteLine("    Example: buffer addplay 5 key=play1");
                        Console.WriteLine();
                        Console.WriteLine("  remfile <key>");
                        Console.WriteLine("    Add a remove file command to remove a file from the server by its key");
                        Console.WriteLine("    Example: buffer remfile sound1");
                        Console.WriteLine();
                        Console.WriteLine("  list");
                        Console.WriteLine("    Display all entries in the score buffer with their parameters");
                        Console.WriteLine();
                        Console.WriteLine("  remove <index|range> [index|range ...]");
                        Console.WriteLine("    Remove entries from the buffer by index or range");
                        Console.WriteLine("    Examples: buffer remove 2");
                        Console.WriteLine("              buffer remove 1 3 5");
                        Console.WriteLine("              buffer remove 1-3");
                        Console.WriteLine("              buffer remove 1-3 5 7-9");
                        Console.WriteLine();
                        Console.WriteLine("  move <from> <to>");
                        Console.WriteLine("    Move a single entry to a new position");
                        Console.WriteLine("    Example: buffer move 2 5");
                        Console.WriteLine();
                        Console.WriteLine("  move <start>-<end> <destination>");
                        Console.WriteLine("    Move a range of entries to a new position");
                        Console.WriteLine("    Example: buffer move 2-4 7");
                        Console.WriteLine();
                        Console.WriteLine("  copy [clear]");
                        Console.WriteLine("    Copy all ChucK files from the main collection to the buffer as 'add' commands");
                        Console.WriteLine("    Optional 'clear' parameter will clear the main collection after copying");
                        Console.WriteLine("    Example: buffer copy clear");
                        Console.WriteLine();
                        Console.WriteLine("  clear");
                        Console.WriteLine("    Clear all entries from the score buffer");
                        Console.WriteLine();
                        Console.WriteLine("  save <filename>");
                        Console.WriteLine("    Save the score buffer to a .mgs file");
                        Console.WriteLine("    Example: buffer save myscore");
                        Console.WriteLine();
                        Console.WriteLine("  load <filename>");
                        Console.WriteLine("    Load a score buffer from a .mgs file (replaces current buffer)");
                        Console.WriteLine("    Example: buffer load myscore");
                        Console.WriteLine();
                        Console.WriteLine("  show");
                        Console.WriteLine("    Show all available .mgs files in the listener directory");
                        Console.WriteLine();
                        Console.WriteLine("  flush");
                        Console.WriteLine("    Send all buffered commands to the ChucK server as a single batch");
                        Console.WriteLine();
                        Console.WriteLine("  help");
                        Console.WriteLine("    Display this help message");
                        Console.WriteLine();
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

        private static void SendOscMessage(IEnumerable<string> commands)
        {
            object[] oscArgs = [.. commands.Cast<object>()];
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

        static void AddChuckFile(string fileAndParams)
        {
            if (string.IsNullOrEmpty(s_oscListenerPath))
            {
                Console.WriteLine("Error: OscListener.ck path is not set.");
                return;
            }

            try
            {
                // Parse filename and parameters
                var parts = fileAndParams.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0)
                {
                    Console.WriteLine("Usage: add <filename.ck> [param1=value1 param2=value2 ...]");
                    return;
                }

                var filename = parts[0];
                var parameters = new Dictionary<string, object>();

                // Parse parameters (param1=value1 param2=value2 ...)
                for (int i = 1; i < parts.Length; i++)
                {
                    var paramParts = parts[i].Split('=', 2);
                    if (paramParts.Length == 2)
                    {
                        parameters[paramParts[0]] = paramParts[1];
                    }
                    else
                    {
                        Console.WriteLine($"Warning: Invalid parameter format '{parts[i]}'. Expected format: param=value");
                    }
                }

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

                // Add the file with parameters (allow duplicates for different parameterizations)
                var newEntry = new Tuple<string, IDictionary<string, object>>(
                    matchingFile, 
                    parameters);

                s_chuckFiles.Add(newEntry);
                Console.WriteLine($"Added: {matchingFile} (entry #{s_chuckFiles.Count})");

                if (parameters.Count > 0)
                {
                    Console.WriteLine("  Parameters:");
                    foreach (var param in parameters)
                    {
                        Console.WriteLine($"    {param.Key} = {param.Value}");
                    }
                }
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

        static void ClearChuckFiles()
        {
            if (s_chuckFiles.Count == 0)
            {
                Console.WriteLine("No ChucK files currently loaded.");
                return;
            }

            int count = s_chuckFiles.Count;
            s_chuckFiles.Clear();
            Console.WriteLine($"Cleared {count} ChucK file{(count == 1 ? "" : "s")}.");
        }

        static void PlayChuckFiles(int seconds)
        {
            // This is a temporary implementation that will use the SendOscMessage method to add files one at a time, then send a play command with the specified duration, then send the remove commands
            foreach (var entry in s_chuckFiles)
            {
                SendOscMessage([$"add {entry.Item1}"]);
            }

            SendOscMessage([$"play {seconds}"]);

            foreach (var entry in s_chuckFiles)
            {
                SendOscMessage([$"remove {entry.Item1}"]);
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
            Console.WriteLine("  clear             - Clear all loaded .ck files");
            Console.WriteLine("  play <seconds>    - Send loaded .ck files to server and play for specified seconds");
            Console.WriteLine("  buffer <cmd>      - Score buffer operations (addfile, addplay, remfile, remove, move, list, copy, clear, save, load, show)");


            Console.WriteLine("  exit              - Exit the utility");
            Console.WriteLine("  help              - Show this help message");
            Console.WriteLine();
            Console.WriteLine("Deprecated commands:");
            Console.WriteLine("  send <cmd> <args> - Send OSC message with command and arguments");
            Console.WriteLine();
        }
    }
}