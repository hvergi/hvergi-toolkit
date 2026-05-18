using Godot;
using Godot.Collections;

public partial class GodotCopyrightDisplay : RichTextLabel
{
    public override void _Ready()
    {
        BbcodeEnabled = true;
        
        System.Text.StringBuilder sb = new System.Text.StringBuilder();

		// 1. Get the Main Godot License Text
        sb.Append("\n[font_size=24]Godot Engine License[/font_size]\n\n");
        sb.Append($"[code]{Engine.GetLicenseText()}[/code]\n\n");
        sb.Append("[hr]\n\n");

        // 2. Get Components & Contributors
        sb.Append("[font_size=24]Engine Components & Copyrights[/font_size]\n\n");
        AppendCopyrightInfo(sb);

        // 3. Get Third-Party Licenses Info
        sb.Append("[font_size=24]Third-Party Licenses[/font_size]\n\n");
        AppendThirdPartyLicenses(sb);

        Text = sb.ToString();
    }

    private void AppendCopyrightInfo(System.Text.StringBuilder sb)
    {
        Array<Dictionary> copyrightList = Engine.GetCopyrightInfo();

        foreach (Dictionary component in copyrightList)
        {
            if (component.ContainsKey("name"))
            {
                string name = component["name"].AsString();
                sb.Append($"[b][color=magenta]{name}[/color][/b]\n");
            }

            if (component.ContainsKey("parts"))
            {
                Array<Dictionary> parts = (Array<Dictionary>)component["parts"];
                foreach (Dictionary part in parts)
                {
                    if (part.ContainsKey("files"))
                    {
                        Array<string> files = new Array<string>((Array)part["files"]);
                        sb.Append($"[i]Files:[/i] {string.Join(", ", files)}\n");
                    }

                    if (part.ContainsKey("copyright"))
                    {
                        Array<string> copyrights = new Array<string>((Array)part["copyright"]);
                        foreach (string copyright in copyrights)
                        {
                            sb.Append($"© {copyright}\n");
                        }
                    }

                    if (part.ContainsKey("license"))
                    {
                        string license = part["license"].AsString();
                        sb.Append($"[i]License:[/i] {license}\n");
                    }
                    sb.Append("\n");
                }
            }
            sb.Append("[hr]\n"); 
        }
    }

    private void AppendThirdPartyLicenses(System.Text.StringBuilder sb)
    {
        // Engine.GetLicenseInfo() returns a Dictionary where:
        // Key = License name (e.g., "Expat", "BSD-3-Clause")
        // Value = The actual full text of that license
        Dictionary licenseInfo = Engine.GetLicenseInfo();

        foreach (Variant key in licenseInfo.Keys)
        {
            string licenseName = key.AsString();
            string licenseText = licenseInfo[key].AsString();

            sb.Append($"[b][color=aqua]{licenseName}[/color][/b]\n\n");
            sb.Append($"[code]{licenseText}[/code]\n\n");
            sb.Append("[hr]\n");
        }
    }
}