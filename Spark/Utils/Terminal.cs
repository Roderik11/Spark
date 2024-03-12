using System;
using System.Collections.Generic;
using System.Diagnostics;

public enum EventPriority
{
    None,
    Low,
    Normal,
    High,
    Highest
}

public static class Terminal
{
    private static Dictionary<string, TerminalCommand> Commands = new Dictionary<string, TerminalCommand>();

    public delegate void EventFiredHandler(string text, EventPriority priority);

    public delegate void CommandExecuteHandler(params string[] values);

    public static event EventFiredHandler OnEvent;

    public class TerminalCommand
    {
        public event CommandExecuteHandler OnExecute;

        internal TerminalCommand() { }

        internal void Execute(params string[] values)
        {
            OnExecute?.Invoke(values);
        }
    }

    public static void Event(string text)
    {
        OnEvent?.Invoke(text, EventPriority.Highest);
    }

    public static void Event(string text, EventPriority priority)
    {
        OnEvent?.Invoke(text, priority);
    }

    public static TerminalCommand Command(string name)
    {
        if (Commands.ContainsKey(name))
            return Commands[name];

        TerminalCommand cmd = new TerminalCommand();
        Commands.Add(name, cmd);

        return cmd;
    }

    public static bool Execute(string raw)
    {
        string[] arr = raw.Split(new char[1] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

        if (arr.Length < 1)
            return false;

        string cmd = arr[0];

        raw = raw.Replace(cmd, "");
        arr = raw.Split(new char[1] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

        if (!Commands.ContainsKey(cmd))
            return false;

        TerminalCommand command = Commands[cmd];
        command.Execute(arr);

        return true;
    }
}