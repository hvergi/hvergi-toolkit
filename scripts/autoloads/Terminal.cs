using Godot;
using System;

public partial class Terminal : Node
{
	public static RichTextLabel Output {get; set;}
	
    public enum Flags
    {
        Info,
        Warning,
        Error
    }

    public static void Write(string text)
    {
        if (Output == null)
        {
            GD.Print("NO Output: " + text);
            return;
        }
        string timestamp = Time.GetTimeStringFromSystem();
        Output.CallDeferred(RichTextLabel.MethodName.AppendText, $"[{timestamp}]: {text}\n");
    }

    public static void WriteWarning(string text)
    {
        if (Output == null)
        {
            GD.Print("NO Output: " + text);
            return;
        }
        string timestamp = Time.GetTimeStringFromSystem();
        Output.CallDeferred(RichTextLabel.MethodName.AppendText, $"[color=yellow][{timestamp}]WARNING: {text}[/color]\n");
    }

    public static void WriteError(string text)
    {
        if (Output == null)
        {
            GD.PrintErr("NO Output: " + text);
            return;
        }
        string timestamp = Time.GetTimeStringFromSystem();
        Output.CallDeferred(RichTextLabel.MethodName.AppendText, $"[color=red][{timestamp}]ERROR: {text}[/color]\n");
    }
}
