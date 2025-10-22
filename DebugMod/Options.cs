namespace ID2.DebugMod;

internal static class Options
{
	private static bool showColliders;

	public static bool ShowColliders
	{
		get => showColliders;
		set
		{
			if (showColliders == value)
				return;

			showColliders = value;
			DebugMod.Instance.ToggleColliders(value);
		}
	}

	public static void Test()
	{
		ShowColliders = !ShowColliders;
	}
}