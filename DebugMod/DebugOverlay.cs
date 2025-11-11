using BepInEx.Configuration;
using HarmonyLib;
using ID2.ModCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ID2.DebugMod;

internal class DebugOverlay : MonoBehaviour
{
	[Flags]
	public enum Section
	{
		None = 0,
		Player = 1 << 0,
		Scene = 1 << 1,
		Droptable = 1 << 2,
		Other = 1 << 3,
		All = Player | Scene | Droptable | Other
	}

	private enum LineType
	{
		Heading,

		// Player section
		PlayerPosition,
		PlayerAngle,
		PlayerSpeed,
		PlayerAction,
		PlayerIFrames,
		PlayerDead,
		PlayerKeepOnFloor,

		// Scene section
		SceneName,
		RoomName,
		SpawnPoint,
		TimeOfDay,

		// Droptable section
		DTTier1,
		DTTier2,
		DTTier3,
		DTTier4,
		DTSuperCounter,
		DTNoHitCounter,

		// Other Section
		GamePaused,
	}

	private readonly float textStackSpacing = 20;
	private readonly int outlineThickness = 2; // Don't go higher than 2 to avoid vertex limit
	private readonly int decimalPrecision = 2; // # of decimal places to show
	private readonly static Dictionary<Section, Text> textObjects = new();
	private static DebugOverlay instance;
	private static GameObject overlay;
	private VerticalLayoutGroup textStack;
	private List<LineData> playerSectionLines;
	private List<LineData> sceneSectionLines;
	private List<LineData> droptableSectionLines;
	private List<LineData> otherSectionLines;
	private float updateTimer;

	private LevelTimeUpdater timeUpdater;
	private DroptableData droptableJsonData;
	private EntityDropHandler dropHandler;

	private static DebugOverlay Instance => instance;
	private Entity Player => Globals.Player;
	private SaverOwner MainSaver => Globals.MainSaver;
	private static List<Section> AllSections
	{
		get
		{
			return Enum.GetValues(typeof(Section))
			.Cast<Section>()
			.Where(s => s != Section.None && s != Section.All)
			.ToList();
		}
	}
	private static List<Section> EnabledSections
	{
		get
		{
			Section value = Options.infoShownEntry.Value;

			if (value == Section.None)
				return [];

			if (value == Section.All)
				return AllSections;

			return AllSections
				.Where(s => (value & s) != 0)
				.ToList();
		}
	}

	private void Awake()
	{
		instance = this;
	}

	private void OnEnable()
	{
		timeUpdater = LevelTime.Instance.GetComponentInChildren<LevelTimeUpdater>();

		Events.OnSceneLoaded += OnSceneLoaded;

		if (overlay == null)
			CreateOverlay();
		else
			overlay.SetActive(true);

		// Update droptable info once upon overlay getting created
		UpdateDroptableInfo(Section.Droptable, null);
	}

	private void OnDisable()
	{
		Events.OnSceneLoaded -= OnSceneLoaded;
		overlay?.SetActive(false);
	}

	private void Update()
	{
		updateTimer += Time.deltaTime;

		if (updateTimer >= Options.updateIntervalEntry.Value / 1000)
		{
			UpdateEverything();
			updateTimer = 0;
		}
	}

	private void UpdateEverything()
	{
		foreach (Section section in AllSections)
		{
			Action<Section> action = section switch
			{
				Section.Player => UpdatePlayerInfo,
				Section.Scene => UpdateSceneInfo,
				Section.Other => UpdateOtherInfo,
				_ => _ => { }
			};

			bool enabled = EnabledSections.Contains(section);

			if (enabled)
				action(section);

			//if (!textObjects.ContainsKey(section))
			if (ShouldCreateTextObject(section))
				continue;

			textObjects[section].gameObject.SetActive(enabled);
		}

		UpdateOverlayText();
	}

	private void CreateOverlay()
	{
		overlay = new GameObject("Debug Overlay");

		// Setup Canvas
		Canvas canvas = overlay.AddComponent<Canvas>();
		canvas.renderMode = RenderMode.ScreenSpaceOverlay;
		canvas.sortingOrder = 999;

		// Setup CanvasScaler
		CanvasScaler scaler = overlay.AddComponent<CanvasScaler>();
		scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
		scaler.referenceResolution = new Vector2(1920, 1080);

		// Setup Texts stack
		GameObject textStackObj = new GameObject("Texts");
		textStackObj.transform.SetParent(overlay.transform, false);
		RectTransform textStackRect = textStackObj.AddComponent<RectTransform>();
		textStackRect.pivot = new Vector2(0, 1);
		textStackRect.anchorMin = new Vector2(0, 1);
		textStackRect.anchorMax = new Vector2(0, 1);
		textStackRect.anchoredPosition = Options.overlayPositionEntry.Value;

		textStack = textStackObj.AddComponent<VerticalLayoutGroup>();
		textStack.childAlignment = TextAnchor.UpperLeft;
		textStack.childForceExpandHeight = false;
		textStack.childForceExpandWidth = true;
		textStack.spacing = textStackSpacing;

		// Setup ContentSizeFitter
		ContentSizeFitter fitter = textStackObj.AddComponent<ContentSizeFitter>();
		fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
		fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

		UpdateEverything();
	}

	private Text CreateNewText(Section section)
	{
		// Setup Text
		GameObject textObj = new GameObject($"{section}InfoText");
		textObj.transform.SetParent(textStack.transform, false);

		Text text = textObj.AddComponent<Text>();
		text.font = Options.Font;
		text.fontSize = Options.fontSizeEntry.Value;
		text.alignment = TextAnchor.UpperLeft;
		text.color = Options.textColorEntry.Value;
		text.text = $"{section} Section";

		// Setup text outline
		for (int i = 0; i < outlineThickness; i++)
		{
			Outline textOutline = textObj.AddComponent<Outline>();
			textOutline.effectColor = Options.outlineColorEntry.Value;
			textOutline.effectDistance = new Vector2(1f, -1f);
			textOutline.enabled = Options.toggleOutlineEntry.Value;
		}

		if (textObjects.ContainsKey(section))
			textObjects[section] = text;
		else
			textObjects.Add(section, text);

		return text;
	}

	private void UpdatePlayerInfo(Section section)
	{
		if (ShouldCreateTextObject(section))
		{
			playerSectionLines = new()
			{
				new LineData(LineType.Heading, "PLAYER", labelIsHeading: true),
				new LineData(LineType.PlayerPosition, "Position"),
				new LineData(LineType.PlayerAngle, "Angle"),
				new LineData(LineType.PlayerSpeed, "Speed"),
				new LineData(LineType.PlayerAction, "Action"),
				new LineData(LineType.PlayerIFrames, "Invincible"),
				new LineData(LineType.PlayerDead, "Dead"),
				new LineData(LineType.PlayerKeepOnFloor, "Keep on Floor"),
			};

			CreateNewText(section);
		}

		if (!EnabledSections.Contains(section) || Globals.IsPaused)
			return;

		LineData playerPosLine = playerSectionLines.Find(l => l.Type == LineType.PlayerPosition);
		LineData playerRotLine = playerSectionLines.Find(l => l.Type == LineType.PlayerAngle);
		LineData playerSpeedLine = playerSectionLines.Find(l => l.Type == LineType.PlayerSpeed);
		LineData playerActionLine = playerSectionLines.Find(l => l.Type == LineType.PlayerAction);
		LineData playerIFramesLine = playerSectionLines.Find(l => l.Type == LineType.PlayerIFrames);
		LineData playerDeadLine = playerSectionLines.Find(l => l.Type == LineType.PlayerDead);
		LineData playerKeepOnFloorLine = playerSectionLines.Find(l => l.Type == LineType.PlayerKeepOnFloor);

		RollAction rollAction = Player.GetComponentInChildren<RollAction>();
		Vector3 position = new Vector3(
			x: (float)Math.Round(Player.WorldPosition.x, decimalPrecision),
			y: (float)Math.Round(Player.WorldPosition.y, decimalPrecision),
			z: (float)Math.Round(Player.WorldPosition.z, decimalPrecision));
		double angle = Math.Round(Player.RealTransform.localEulerAngles.y, decimalPrecision);

		// Calculate real speed
		Vector3 rawVelocity = new(Mathf.Abs(Player.realBody.accumVel.x), Mathf.Abs(Player.realBody.accumVel.y), Mathf.Abs(Player.realBody.accumVel.z));
		float rawSpeed = Mathf.Abs(rawVelocity.x) + Mathf.Abs(rawVelocity.z);
		float forwardVector = Mathf.Floor(Player.CurrentMover.owner.ForwardVector.x) != 0 ?
			Mathf.Abs(Player.CurrentMover.owner.ForwardVector.x) :
			Mathf.Abs(Player.CurrentMover.owner.ForwardVector.z);
		double realSpeed = Math.Round(Mathf.Abs((rawSpeed * forwardVector)), decimalPrecision);
		double speedMultiplier = Math.Round(Player.LocalMods.MoveSpeedMultiplier, 2);

		playerPosLine.Text = position;
		playerRotLine.Text = angle;
		playerSpeedLine.Text = $"{realSpeed} ({speedMultiplier}x)";
		playerActionLine.Text = Player.CurrentAction == null ? "None" : Player.CurrentAction.ActionName;
		playerIFramesLine.Text = rollAction.hitDisable != null;
		playerDeadLine.Text = Player.InactiveOrDead;
		playerKeepOnFloorLine.Text = Player.keepOnFloor;
	}

	private void UpdateSceneInfo(Section section)
	{
		if (ShouldCreateTextObject(section))
		{
			sceneSectionLines = new()
			{
				new LineData(LineType.Heading, "SCENE", labelIsHeading: true),
				new LineData(LineType.SceneName, "Scene"),
				new LineData(LineType.RoomName, "Room"),
				new LineData(LineType.SpawnPoint, "Spawn"),
				new LineData(LineType.TimeOfDay, "ToD"),
			};

			CreateNewText(section);
		}

		if (!EnabledSections.Contains(section) || Globals.IsPaused)
			return;

		LineData sceneNameLine = sceneSectionLines.Find(l => l.Type == LineType.SceneName);
		LineData roomNameLine = sceneSectionLines.Find(l => l.Type == LineType.RoomName);
		LineData spawnPointLine = sceneSectionLines.Find(l => l.Type == LineType.SpawnPoint);
		LineData timeOfDayLine = sceneSectionLines.Find(l => l.Type == LineType.TimeOfDay);

		float hoursPerMinute = timeUpdater._hoursPerMinute;

		string sceneName = Globals.CurrentScene;
		string roomName = Globals.CurrentRoom.RoomName;
		string spawnPoint = Globals.SpawnPoint;
		string timeOfDay = $"{GetTime()} ({hoursPerMinute} h/m)";

		sceneNameLine.Text = sceneName;
		roomNameLine.Text = roomName;
		spawnPointLine.Text = spawnPoint;
		timeOfDayLine.Text = timeOfDay;
	}

	private void UpdateDroptableInfo(Section section, DropTableContext.TableState tableState)
	{
		if (ShouldCreateTextObject(section))
		{
			droptableSectionLines = new()
			{
				new LineData(LineType.Heading, "DROPTABLES", labelIsHeading: true),
				new LineData(LineType.DTTier1, "Tier 1"),
				new LineData(LineType.DTTier2, "Tier 2"),
				new LineData(LineType.DTTier3, "Tier 3"),
				new LineData(LineType.DTTier4, "Tier 4"),
				new LineData(LineType.DTSuperCounter, "Until Super"),
				new LineData(LineType.DTNoHitCounter, "Until Hitless"),
			};

			CreateNewText(section);
		}

		if (!EnabledSections.Contains(section))
			return;

		dropHandler ??= Player.GetEntityComponent<EntityDropHandler>();
		LineData superCounterLine = droptableSectionLines.Find(l => l.Type == LineType.DTSuperCounter);
		LineData noHitCounterLine = droptableSectionLines.Find(l => l.Type == LineType.DTNoHitCounter);
		int untilSuper = dropHandler._dropsForSuper - dropHandler.state.dropCounter;
		int untilHitlessSuper = dropHandler._noHitsForSuper - dropHandler.state.noHitCounter;

		LineData tier1Line = droptableSectionLines.Find(l => l.Type == LineType.DTTier1);
		LineData tier2Line = droptableSectionLines.Find(l => l.Type == LineType.DTTier2);
		LineData tier3Line = droptableSectionLines.Find(l => l.Type == LineType.DTTier3);
		LineData tier4Line = droptableSectionLines.Find(l => l.Type == LineType.DTTier4);
		IDataSaver playerSaver = Player._saveLink.LinkLocalStorage;
		IDataSaver droptablesSaver = playerSaver.GetLocalSaver("droptables");
		Dictionary<string, LineData> tierLines = new()
		{
			["DT_Tier1"] = tier1Line,
			["DT_Tier2"] = tier2Line,
			["DT_Tier3"] = tier3Line,
			["DT_Tier4"] = tier4Line
		};
		bool showNextDropItem = Options.droptableShowItemsToggleEntry.Value;

		if (showNextDropItem && droptableJsonData == null)
		{
			string json = Helpers.LoadEmbeddedResource<string>("ID2.Resources.droptableDrops.json");
			droptableJsonData = Helpers.DeserializeJsonObject<DroptableData>(json);
		}

		var activeTables = dropHandler.owner.DropContext.tables;

		// If doing initial update, load data from save file
		if (tableState == null)
		{
			string[] allKeys = droptablesSaver.GetAllDataKeys();
			int dtTier = 1;
			Array.Sort(allKeys, StringComparer.OrdinalIgnoreCase);

			foreach (string key in allKeys)
			{
				if (!tierLines.TryGetValue(key, out LineData line))
					continue;

				var unsavedDT = activeTables.FirstOrDefault(t => t.Key.name == key);
				var unsavedSuperDT = activeTables.FirstOrDefault(t => t.Key.name == $"{key}Super");
				bool useUnsavedData = unsavedDT.Key != null;
				bool useUnsavedSuperData = unsavedSuperDT.Key != null;

				int baseValue = useUnsavedData ? unsavedDT.Value.Position : droptablesSaver.LoadInt(key);
				int superValue = useUnsavedSuperData ? unsavedSuperDT.Value.Position : droptablesSaver.LoadInt($"{key}Super");
				string text = $"{baseValue} (Super: {superValue})";

				if (showNextDropItem)
				{
					string nextItem = droptableJsonData.GetNextItemName(dtTier++, baseValue, superValue, untilSuper, untilHitlessSuper);
					text += $" ({nextItem})";
				}

				line.Text = text;
			}
		}
		// Load data from EntityDroppable
		else
		{
			string name = tableState.table.name;
			string baseName = name.Replace("Super", "");
			bool isSuper = name.EndsWith("Super");

			if (showNextDropItem)
			{
				int dtTier = 1;

				foreach (var kvp in tierLines)
				{
					var unsavedDT = activeTables.FirstOrDefault(t => t.Key.name == kvp.Key);
					var unsavedSuperDT = activeTables.FirstOrDefault(t => t.Key.name == $"{kvp.Key}Super");
					bool useUnsavedData = unsavedDT.Key != null;
					bool useUnsavedSuperData = unsavedSuperDT.Key != null;

					int baseValue = useUnsavedData ? unsavedDT.Value.Position : droptablesSaver.LoadInt(kvp.Key);
					int superValue = useUnsavedSuperData ? unsavedSuperDT.Value.Position : droptablesSaver.LoadInt($"{kvp.Key}Super");

					string nextItem = droptableJsonData.GetNextItemName(dtTier++, baseValue, superValue, untilSuper, untilHitlessSuper);
					kvp.Value.Text = nextItem;
				}
			}
			else
			{
				if (!tierLines.TryGetValue(baseName, out LineData line))
					return;

				int value = dropHandler.owner.DropContext.tables[tableState.table].Position;
				string text = line.Text.ToString();
				MatchCollection matches = Regex.Matches(text, @"\d+");
				int digitIndexToReplace = isSuper ? 2 : 1;

				if (digitIndexToReplace < matches.Count)
				{
					Match m = matches[digitIndexToReplace];
					text = text.Substring(0, m.Index)
						+ value.ToString()
						+ text.Substring(m.Index + m.Value.Length);
					line.Text = text.Substring(8);
				}
			}
		}

		superCounterLine.Text = untilSuper;
		noHitCounterLine.Text = untilHitlessSuper;
	}

	private void UpdateOtherInfo(Section section)
	{
		if (ShouldCreateTextObject(section))
		{
			otherSectionLines = new()
			{
				new LineData(LineType.Heading, "OTHER", labelIsHeading: true),
				new LineData(LineType.GamePaused, "Paused"),
			};

			CreateNewText(section);
		}

		if (!EnabledSections.Contains(section))
			return;

		LineData gamePausedLine = otherSectionLines.Find(l => l.Type == LineType.GamePaused);

		gamePausedLine.Text = Globals.IsPaused;
	}

	private void UpdateOverlayText()
	{
		foreach (Section section in EnabledSections)
		{
			List<LineData> lineData = GetLineDataForSection(section);

			if (!textObjects.ContainsKey(section))
				continue;

			textObjects[section].text = string.Join("\n", lineData.Select(l => l.Text.ToString()).ToArray());
		}
	}

	private void OnSceneLoaded(Scene _, LoadSceneMode __)
	{
		dropHandler = null;

		if (overlay == null)
			CreateOverlay();

		// Update droptable info once upon overlay getting created
		UpdateDroptableInfo(Section.Droptable, null);
	}

	private List<LineData> GetLineDataForSection(Section section)
	{
		return section switch
		{
			Section.Player => playerSectionLines,
			Section.Scene => sceneSectionLines,
			Section.Droptable => droptableSectionLines,
			Section.Other => otherSectionLines,
			_ => null
		};
	}

	private bool ShouldCreateTextObject(Section section)
	{
		return !textObjects.ContainsKey(section) || textObjects[section] == null;
	}

	private string GetTime()
	{
		double time = Math.Round(LevelTime.Instance.GetTime("currTime"), decimalPrecision);
		int hours = (int)Math.Floor(time);
		int minutes = (int)Math.Round((time - hours) * 60);

		if (minutes == 60)
		{
			minutes = 0;
			hours = (hours + 1) % 24;
		}

		return $"{hours:D2}:{minutes:D2}";
	}

	public struct Options
	{
		public static ConfigEntry<bool> toggleEntry;
		public static ConfigEntry<KeyboardShortcut> toggleHotkeyEntry;
		public static ConfigEntry<Section> infoShownEntry;
		public static ConfigEntry<Vector2> overlayPositionEntry;
		public static ConfigEntry<string> fontEntry;
		public static ConfigEntry<int> fontSizeEntry;
		public static ConfigEntry<float> updateIntervalEntry;
		public static ConfigEntry<bool> toggleOutlineEntry;
		public static ConfigEntry<Color> textColorEntry;
		public static ConfigEntry<Color> outlineColorEntry;
		public static ConfigEntry<bool> droptableShowItemsToggleEntry;

		private static readonly Dictionary<string, Font> fontMappings = new();

		public static bool Enabled
		{
			get => toggleEntry.Value;
		}
		public static Font Font
		{
			get => fontMappings[fontEntry.Value];
		}

		public readonly struct Defaults
		{
			public static bool Toggle { get; } = false;
			public static KeyboardShortcut ToggleHotkey { get; } = new KeyboardShortcut(KeyCode.F3);
			public static Section InfoShown { get; } = Section.All;
			public static Vector2 OverlayPosition { get; } = new Vector2(100, -170);
			public static string Font { get; } = "Vanilla";
			public static int FontSize { get; } = 18;
			public static float UpdateInterval { get; } = 0f;
			public static bool ToggleOutline { get; } = false;
			public static Color TextColor { get; } = Utility.ConvertHexToColor("#ffffff");
			public static Color OutlineColor { get; } = Utility.ConvertHexToColor("#2b2b2b85");
			public static bool DroptableShowItemToggle { get; } = true;
		}

		public readonly struct Ranges
		{
			public static List<string> Fonts { get; } = new();
		}

		public static void ToggleChanged(bool value)
		{
			DebugMod.Instance.ToggleDebugOverlay(value);
		}

		public static void InfoShownChanged(Section value)
		{
			if (!Enabled)
				return;

			foreach (Section section in AllSections)
			{
				if (EnabledSections.Contains(section))
					textObjects[section].gameObject.SetActive(true);
				else
					textObjects[section].gameObject.SetActive(false);
			}
		}

		public static void OverlayPositionChanged(Vector2 value)
		{
			if (!Enabled)
				return;

			overlay.transform.Find("Texts").GetComponent<RectTransform>().anchoredPosition = value;
		}

		public static void FontChanged(string value)
		{
			if (!Enabled)
				return;

			if (fontMappings.TryGetValue(value, out Font font))
			{
				foreach (Text text in textObjects.Values)
					text.font = font;
			}
		}

		public static void FontSizeChanged(int value)
		{
			if (!Enabled)
				return;

			foreach (Text text in textObjects.Values)
				text.fontSize = value;
		}

		public static void OutlineToggled(bool value)
		{
			foreach (Text text in textObjects.Values)
			{
				foreach (Outline outline in text.GetComponents<Outline>())
					outline.enabled = value;
			}
		}

		public static void TextColorChanged(Color value)
		{
			if (!Enabled)
				return;

			foreach (Text text in textObjects.Values)
				text.color = value;
		}

		public static void OutlineColorChanged(Color value)
		{
			if (!Enabled || !toggleOutlineEntry.Value)
				return;

			foreach (Text text in textObjects.Values)
			{
				foreach (Outline outline in text.GetComponents<Outline>())
					outline.effectColor = value;
			}
		}

		public static void DroptableShowItemToggle(bool value)
		{
			Instance.UpdateDroptableInfo(Section.Droptable, null);
		}

		public static void GenerateFontList()
		{
			fontMappings.Add("Vanilla", Resources.Load<FontMaterialMap>("FontMaterialMap")._data[0].font);
			fontMappings.Add("Arial", Resources.GetBuiltinResource<Font>("Arial.ttf"));

			foreach (string fontName in fontMappings.Keys)
				Ranges.Fonts.Add(fontName);
		}
	}

	private class TextData
	{
		public Text Text { get; private set; }
		public List<LineData> LineData { get; private set; }

		public TextData(Text text, List<LineData> lineData)
		{
			Text = text;
			LineData = lineData;
		}
	}

	private class LineData
	{
		private object text;

		public LineType Type { get; private set; }
		public string Label { get; private set; }
		public object Text
		{
			get => text;
			set => text = Label + value;
		}

		public LineData(LineType type, string label, string text = "", bool labelIsHeading = false)
		{
			Type = type;
			Label = !labelIsHeading ? label + ": " : label;
			Text = text;
		}
	}

	private class DroptableData
	{
		public Dictionary<int, string> tier1;
		public Dictionary<int, string> tier1Super;
		public Dictionary<int, string> tier2;
		public Dictionary<int, string> tier2Super;
		public Dictionary<int, string> tier3;
		public Dictionary<int, string> tier3Super;
		public Dictionary<int, string> tier4;
		public Dictionary<int, string> tier4Super;

		public string GetNextItemName(int tier, int baseValue, int superValue, int untilSuper, int untilHitless)
		{
			bool isSuper = untilSuper == 1 || untilHitless == 1;
			int currentValue = isSuper ? superValue : baseValue;

			Dictionary<int, string> tierData = tier switch
			{
				1 => isSuper ? tier1Super : tier1,
				2 => isSuper ? tier2Super : tier2,
				3 => isSuper ? tier3Super : tier3,
				4 => isSuper ? tier4Super : tier4,
				_ => null
			};

			if (tierData == null)
				return $"{tier} is an invalid droptable tier";

			return string.IsNullOrEmpty(tierData[currentValue]) ? "None" : tierData[currentValue];
		}
	}

	[HarmonyPatch]
	private static class Patches
	{
		[HarmonyPostfix, HarmonyPatch(typeof(DropTableContext.TableState), nameof(DropTableContext.TableState.AdvancePosition))]
		private static void DroptablePositionAdvanced(DropTableContext.TableState __instance)
		{
			Instance.UpdateDroptableInfo(Section.Droptable, __instance);
		}

		[HarmonyPostfix, HarmonyPatch(typeof(EntityDropHandler), nameof(EntityDropHandler.ResetHitCounter))]
		private static void DroptableHitlessCounterReset()
		{
			Instance.UpdateDroptableInfo(Section.Droptable, null);
		}
	}
} 