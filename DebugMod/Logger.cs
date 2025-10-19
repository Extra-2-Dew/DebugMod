using System;
using System.Linq;

namespace ID2.DebugMod;
/// <summary>
/// A BepInEx logging utility class.
/// </summary>
internal static class Logger
{
	/// <summary>
	/// Logs a message to the BepInEx console.
	/// </summary>
	/// <param name="message">The message to log.</param>
	public static void Log(string message)
	{
		Plugin.Logger.LogMessage(message);
	}

	/// <summary>
	/// Logs a warning to the BepInEx console.
	/// </summary>
	/// <param name="message">The message to log.</param>
	public static void LogWarning(string message)
	{
		Plugin.Logger.LogWarning(message);
	}

	/// <summary>
	/// Logs an error to the BepInEx console.
	/// </summary>
	/// <param name="message">The message to log.</param>
	public static void LogError(string message, bool includeTrace = true)
	{
		Plugin.Logger.LogError(includeTrace ? message + "\n" + GetStackTrace() : message);
	}

	/// <summary>
	/// Logs an exception to the BepInEx console.
	/// </summary>
	/// <param name="ex">The exception to log.</param>
	public static void LogError(System.Exception ex)
	{
		LogError(ex.ToString(), includeTrace: false);
	}

	private static string GetStackTrace()
	{
		string stack = Environment.StackTrace;
		string[] lines = stack.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
		return string.Join("\n", lines.Skip(3).ToArray());
	}
}