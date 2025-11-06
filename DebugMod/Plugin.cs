using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.Collections;
using UnityEngine;

namespace ID2.DebugMod;
[BepInPlugin("id2.DebugMod", "DebugMod", "0.1.0")]
[BepInDependency("ModCore")]
[BepInDependency("com.bepis.bepinex.configurationmanager", BepInDependency.DependencyFlags.SoftDependency)]
public class Plugin : BaseUnityPlugin
{
	internal static new ManualLogSource Logger;
	private static Plugin instance;
	private Options options;
	private bool initialized;

	public static Plugin Instance => instance;

	private void Awake()
	{
		instance = this;
		Logger = base.Logger;

		Logger.LogInfo($"Plugin DebugMod (id2.DebugMod) is loaded!");

		try
		{
			// Mod initialization code here
			options = new();
			DebugMod debugMod = new GameObject("DebugMod").AddComponent<DebugMod>();

			var harmony = new Harmony("id2.DebugMod");
			harmony.PatchAll();

			initialized = true;
		}
		catch (System.Exception err)
		{
			Logger.LogError(err);
		}
	}

	private void Update()
	{
		if (!initialized)
			return;

		options.CheckForHotkeys();
	}

	/// <summary>
	/// Starts a Coroutine on the Plugin MonoBehaviour.<br/>
	/// This is useful for if you need to start a Coroutine<br/>from a non-MonoBehaviour class.
	/// </summary>
	/// <param name="routine">The routine to start.</param>
	/// <returns>The started Coroutine.</returns>
	public static Coroutine StartRoutine(IEnumerator routine)
	{
		return Instance.StartCoroutine(routine);
	}
}