using UnityEngine;

namespace ID2.DebugMod;

internal static class Utility
{
	public static Color ConvertHexToColor(string colorHex)
	{
		ColorUtility.TryParseHtmlString(colorHex, out Color color);
		return color;
	}
}