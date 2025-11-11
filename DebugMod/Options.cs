using BepInEx.Configuration;
using ID2.ModCore;
using System;
using UnityEngine;

namespace ID2.DebugMod;

internal class Options
{
	private Vector2 sceneScroll;
	private Vector2 roomScroll;
	private ConfigEntry<bool> warpMenu;
	private readonly ConfigFile configFile;

	public bool HasSetup { get; private set; }

	public Options()
	{
		configFile = Plugin.Instance.Config;
	}

	public void Setup()
	{
		SetupDebugOverlayOptions();
		SetupCollisionViewerOptions();

		HasSetup = true;
	}

	private void SetupDebugOverlayOptions()
	{
		DebugOverlay.Options.GenerateFontList();
		string section = "Debug Overlay";

		DebugOverlay.Options.toggleEntry = Helpers.CreateOption
		(
			configFile: configFile,
			section: section,
			key: "Toggle Debug Overlay",
			description: "Toggles the display of the debug overlay.",
			order: 11,
			defaultValue: DebugOverlay.Options.Defaults.Toggle,
			onChanged: value => DebugOverlay.Options.ToggleChanged(value),
			neverSave: true
		);

		DebugOverlay.Options.toggleHotkeyEntry = Helpers.CreateOption
		(
			configFile: configFile,
			section: section,
			key: "Toggle Debug Overlay Hotkey",
			description: "Pressing this key will toggle the debug overlay.",
			order: 10,
			defaultValue: DebugOverlay.Options.Defaults.ToggleHotkey,
			onHotkeyPressed: () => { DebugOverlay.Options.toggleEntry.Value ^= true; }
		);

		DebugOverlay.Options.infoShownEntry = Helpers.CreateOption
		(
			configFile: configFile,
			section: section,
			key: "Info to Show",
			description: "Select which info to show in debug overlay.",
			order: 9,
			defaultValue: DebugOverlay.Options.Defaults.InfoShown,
			onChanged: value => DebugOverlay.Options.InfoShownChanged(value)
		);

		DebugOverlay.Options.overlayPositionEntry = Helpers.CreateOption
		(
			configFile: configFile,
			section: section,
			key: "Overlay Position",
			description: "The position for the debug overlay.",
			order: 8,
			defaultValue: DebugOverlay.Options.Defaults.OverlayPosition,
			onChanged: value => DebugOverlay.Options.OverlayPositionChanged(value)
		);

		DebugOverlay.Options.fontEntry = Helpers.CreateOption
		(
			configFile: configFile,
			section: section,
			key: "Font",
			description: "The font for the text of the debug overlay.",
			order: 7,
			defaultValue: DebugOverlay.Options.Defaults.Font,
			onChanged: value => DebugOverlay.Options.FontChanged(value),
			acceptableValues: new AcceptableValueList<string>(DebugOverlay.Options.Ranges.Fonts.ToArray())
		);

		DebugOverlay.Options.fontSizeEntry = Helpers.CreateOption
		(
			configFile: configFile,
			section: section,
			key: "Font Size",
			description: "The size for the text of the debug overlay.",
			order: 6,
			defaultValue: DebugOverlay.Options.Defaults.FontSize,
			onChanged: value => DebugOverlay.Options.FontSizeChanged(value),
			attributes: new ConfigurationManagerAttributes() { ShowRangeAsPercent = false },
			acceptableValues: new AcceptableValueRange<int>(5, 100)
		);

		DebugOverlay.Options.updateIntervalEntry = Helpers.CreateOption
		(
			configFile: configFile,
			section: section,
			key: "Update Interval",
			description: "The amount of time in milliseconds for each update of the debug overlay.",
			order: 5,
			defaultValue: DebugOverlay.Options.Defaults.UpdateInterval
		);

		DebugOverlay.Options.toggleOutlineEntry = Helpers.CreateOption
		(
			configFile: configFile,
			section: section,
			key: "Toggle Text Outline",
			description: "Toggles the text outlines for the debug overlay.",
			order: 4,
			defaultValue: DebugOverlay.Options.Defaults.ToggleOutline,
			onChanged: value => DebugOverlay.Options.OutlineToggled(value)
		);

		DebugOverlay.Options.textColorEntry = Helpers.CreateOption
		(
			configFile: configFile,
			section: section,
			key: "Text Color",
			description: "The text color for the debug overlay.",
			order: 3,
			defaultValue: DebugOverlay.Options.Defaults.TextColor,
			onChanged: value => DebugOverlay.Options.TextColorChanged(value)
		);

		DebugOverlay.Options.outlineColorEntry = Helpers.CreateOption
		(
			configFile: configFile,
			section: section,
			key: "Outline Color",
			description: "The text outline color for the debug overlay.",
			order: 2,
			defaultValue: DebugOverlay.Options.Defaults.OutlineColor,
			onChanged: value => DebugOverlay.Options.OutlineColorChanged(value)
		);

		DebugOverlay.Options.droptableShowItemsToggleEntry = Helpers.CreateOption
		(
			configFile: configFile,
			section: section,
			key: "Show Next Droptable Item",
			description: "Toggles between showing the next droptable item or the counts for each tier.",
			order: 1,
			defaultValue: DebugOverlay.Options.Defaults.DroptableShowItemToggle,
			onChanged: value => DebugOverlay.Options.DroptableShowItemToggle(value)
		);
	}

	private void SetupCollisionViewerOptions()
	{
		string section = "Collision Viewer";

		CollisionViewer.Options.toggleEntry = Helpers.CreateOption
		(
			configFile: configFile,
			section: section,
			key: "Toggle Collision Viewer",
			description: "Toggle the display of colliders/hitboxes.",
			order: 9,
			defaultValue: false,
			onChanged: value => CollisionViewer.Options.ToggleChanged(value),
			neverSave: true
		);

		CollisionViewer.Options.showColliderTypesEntry = Helpers.CreateOption
		(
			configFile: configFile,
			section: section,
			key: "Collider Types to Show",
			description: "Select which collider types to show.",
			order: 8,
			defaultValue: CollisionViewer.ColliderType.All,
			onChanged: value => CollisionViewer.Options.SelectedColliderTypesUpdated(value)
		);

		CollisionViewer.Options.toggleHotkeyEntry = Helpers.CreateOption
		(
			configFile: configFile,
			section: section,
			key: "Toggle Hotkey",
			description: "Pressing this key will toggle collision viewer.",
			order: 7,
			defaultValue: CollisionViewer.Options.Defaults.ToggleHotkeyDefault,
			onHotkeyPressed: () => { CollisionViewer.Options.toggleEntry.Value ^= true; }
		);

		CollisionViewer.Options.entityColorEntry = Helpers.CreateOption
		(
			configFile: configFile,
			section: section,
			key: "Color for Entity Colliders",
			description: "",
			order: 6,
			defaultValue: Utility.ConvertHexToColor(CollisionViewer.Options.Defaults.DefaultColliderColors[CollisionViewer.ColliderType.Entity]),
			onChanged: value => CollisionViewer.Options.ColorUpdated(CollisionViewer.ColliderType.Entity, value)
		);
		CollisionViewer.Options.ColliderColors[CollisionViewer.ColliderType.Entity] = CollisionViewer.Options.entityColorEntry.Value;

		CollisionViewer.Options.hazardColorEntry = Helpers.CreateOption
		(
			configFile: configFile,
			section: section,
			key: "Color for Hazard Colliders",
			description: "",
			order: 5,
			defaultValue: Utility.ConvertHexToColor(CollisionViewer.Options.Defaults.DefaultColliderColors[CollisionViewer.ColliderType.Hazard]),
			onChanged: value => CollisionViewer.Options.ColorUpdated(CollisionViewer.ColliderType.Hazard, value)
		);
		CollisionViewer.Options.ColliderColors[CollisionViewer.ColliderType.Hazard] = CollisionViewer.Options.hazardColorEntry.Value;

		CollisionViewer.Options.pushableColorEntry = Helpers.CreateOption
		(
			configFile: configFile,
			section: section,
			key: "Color for Pushable Colliders",
			description: "",
			order: 4,
			defaultValue: Utility.ConvertHexToColor(CollisionViewer.Options.Defaults.DefaultColliderColors[CollisionViewer.ColliderType.Pushable]),
			onChanged: value => CollisionViewer.Options.ColorUpdated(CollisionViewer.ColliderType.Pushable, value)
		);
		CollisionViewer.Options.ColliderColors[CollisionViewer.ColliderType.Pushable] = CollisionViewer.Options.pushableColorEntry.Value;

		CollisionViewer.Options.staticColorEntry = Helpers.CreateOption
		(
			configFile: configFile,
			section: section,
			key: "Color for Static Colliders",
			description: "",
			order: 3,
			defaultValue: Utility.ConvertHexToColor(CollisionViewer.Options.Defaults.DefaultColliderColors[CollisionViewer.ColliderType.Static]),
			onChanged: value => CollisionViewer.Options.ColorUpdated(CollisionViewer.ColliderType.Static, value)
		);
		CollisionViewer.Options.ColliderColors[CollisionViewer.ColliderType.Static] = CollisionViewer.Options.staticColorEntry.Value;

		CollisionViewer.Options.transitionColorEntry = Helpers.CreateOption
		(
			configFile: configFile,
			section: section,
			key: "Color for Transition Colliders",
			description: "",
			order: 2,
			defaultValue: Utility.ConvertHexToColor(CollisionViewer.Options.Defaults.DefaultColliderColors[CollisionViewer.ColliderType.Transition]),
			onChanged: value => CollisionViewer.Options.ColorUpdated(CollisionViewer.ColliderType.Transition, value)
		);
		CollisionViewer.Options.ColliderColors[CollisionViewer.ColliderType.Transition] = CollisionViewer.Options.transitionColorEntry.Value;

		CollisionViewer.Options.triggerColorEntry = Helpers.CreateOption
		(
			configFile: configFile,
			section: section,
			key: "Color for Trigger Colliders",
			description: "",
			order: 1,
			defaultValue: Utility.ConvertHexToColor(CollisionViewer.Options.Defaults.DefaultColliderColors[CollisionViewer.ColliderType.Trigger]),
			onChanged: value => CollisionViewer.Options.ColorUpdated(CollisionViewer.ColliderType.Trigger, value)
		);
		CollisionViewer.Options.ColliderColors[CollisionViewer.ColliderType.Trigger] = CollisionViewer.Options.triggerColorEntry.Value;
	}

	private void WarpingDrawer(ConfigEntryBase entry)
	{
		GUILayout.BeginVertical();
		//Cheats.warpMenuToggled = GUILayout.Toggle(Cheats.warpMenuToggled, $"{(Cheats.warpMenuToggled ? "▼ Warping" : "► Warping")}", "Button");

		//if (Cheats.warpMenuToggled)
		//{
		//GUILayout.BeginVertical("box");

		// Scene column
		GUILayout.BeginHorizontal();
		GUILayout.BeginVertical();
		GUILayout.Label("Scene", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter });
		sceneScroll = GUILayout.BeginScrollView(sceneScroll, GUILayout.Height(100));
		string scene = Cheats.Goto.Options.sceneEntry.Value;
		int index = Array.IndexOf(Cheats.Goto.scenes, scene);
		index = GUILayout.SelectionGrid(index, Cheats.Goto.scenes, 1);
		Cheats.Goto.Options.sceneEntry.Value = Cheats.Goto.scenes[index];
		GUILayout.EndScrollView();
		GUILayout.EndVertical();

		// Room column
		string[] rooms = Cheats.Goto.Options.sceneEntry.Value switch
		{
			"Fluffy Fields" => Cheats.Goto.fluffyFieldsRooms,
			"Pillow Fort" => Cheats.Goto.pillowFortRooms,
			_ => new string[0]
		};

		GUILayout.BeginVertical();
		GUILayout.Label("Room", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter });
		roomScroll = GUILayout.BeginScrollView(roomScroll, GUILayout.Height(100));
		index = Array.IndexOf(rooms, Cheats.Goto.Options.roomEntry.Value);
		index = Mathf.Clamp(index, 0, rooms.Length - 1);
		index = GUILayout.SelectionGrid(index, rooms, 1);
		Cheats.Goto.Options.roomEntry.Value = rooms[index];
		GUILayout.EndScrollView();
		GUILayout.EndVertical();
		GUILayout.EndHorizontal();

		GUILayout.Space(10);

		if (GUILayout.Button("Go!"))
			Cheats.Goto.DoWarp();

		if (GUILayout.Button("Reload current scene"))
			Cheats.Goto.Reload();

			//GUILayout.EndVertical();
		//}

		GUILayout.EndVertical();
	}
}