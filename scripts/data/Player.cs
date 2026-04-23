
using Godot;

public class Player
{
    public string Name { get; set; }
	public string Path { get; set; }
	public bool IsHidden { get; set; }
	public bool IsFav { get; set; }
    public LogType LogTypeVal { get; set; }


    public enum LogType
	{
		None,
		Single,
		Month,
		Day
	}

    public string GetWurmDir()
	{
		string[] parts = Path.Split("players/");
		return parts[0];
	}

	public string GetEventLog()
	{
		return CheckLog("_Event");
	}

	public string GetSkillLog()
	{
		return CheckLog("_Skills");
	}

	public string CheckLog(string prefix)
	{
		var dateTime = Time.GetDatetimeDictFromSystem();
		int year = dateTime["year"].AsInt32();
		int month = dateTime["month"].AsInt32();
		int day = dateTime["day"].AsInt32();

		switch (LogTypeVal)
		{
			case LogType.Single:
				return Path + "logs/" + prefix + ".txt";
			case LogType.Month:
				return $"{Path}logs/{prefix}.{year}-{month:D2}.txt";
			case LogType.Day:
				return $"{Path}logs/{prefix}.{year}-{month:D2}-{day:D2}.txt";
		}

		Terminal.WriteError($"Log type for {Name} is set to NONE, you need to change your profile to log events.");
		return "";
	}
}