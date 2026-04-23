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
        Output.AppendText("[" + Time.GetTimeStringFromSystem() + "]: " + text + "\n");
    }

    public static void WriteWarning(string text)
    {
        if (Output == null)
        {
            GD.Print("NO Output: " + text);
            return;
        }
        Output.AppendText("[color=yellow][" + Time.GetTimeStringFromSystem() + "]WARNING: " + text + "[/color]\n");
    }

    public static void WriteError(string text)
    {
        if (Output == null)
        {
            GD.PrintErr("NO Output: " + text);
            return;
        }
        Output.AppendText("[color=red][" + Time.GetTimeStringFromSystem() + "]ERROR: " + text + "[/color]\n");
    }
}
