using BepInEx.Configuration;
using ID2.DebugMod.Cheats;
using System;
using UnityEngine;

namespace ID2.DebugMod;

internal class Options
{
	public Options()
	{
		SetupCollisionViewerOptions();
	}

	public void CheckForHotkeys()
	{
		if (CollisionViewer.Options.toggleHotkeyEntry.Value.IsDown())
			CollisionViewer.Options.toggleEntry.Value = !CollisionViewer.Options.toggleEntry.Value;
	}

	private void SetupCollisionViewerOptions()
	{
		string section = "Collision Viewer";

		CollisionViewer.Options.toggleEntry = CreateOption
		(
			section: section,
			key: "Toggle Collision Viewer",
			description: "Toggle the display of colliders/hitboxes.",
			order: 9,
			defaultValue: false,
			onChanged: value => CollisionViewer.Options.ToggleChanged(value)
		);

		CollisionViewer.Options.showColliderTypesEntry = CreateOption
		(
			section: section,
			key: "Collider Types to Show",
			description: "Select which collider types to show.",
			order: 8,
			defaultValue: CollisionViewer.ColliderType.All,
			onChanged: value => CollisionViewer.Options.SelectedColliderTypesUpdated(value)
		);

		CollisionViewer.Options.toggleHotkeyEntry = CreateOption
		(
			section: section,
			key: "Toggle Hotkey",
			description: "Pressing this key will toggle collision viewer.",
			order: 7,
			defaultValue: new KeyboardShortcut(KeyCode.F2),
			alwaysDefault: false
		);

		CollisionViewer.Options.entityColorEntry = CreateOption
		(
			section: section,
			key: "Color for Entity Colliders",
			description: "",
			order: 6,
			defaultValue: Utility.ConvertHexToColor(CollisionViewer.Options.DefaultColliderColors[CollisionViewer.ColliderType.Entity]),
			onChanged: value => CollisionViewer.Options.ColorUpdated(CollisionViewer.ColliderType.Entity, value),
			alwaysDefault: false
		);
		CollisionViewer.Options.ColliderColors[CollisionViewer.ColliderType.Entity] = CollisionViewer.Options.entityColorEntry.Value;

		CollisionViewer.Options.hazardColorEntry = CreateOption
		(
			section: section,
			key: "Color for Hazard Colliders",
			description: "",
			order: 5,
			defaultValue: Utility.ConvertHexToColor(CollisionViewer.Options.DefaultColliderColors[CollisionViewer.ColliderType.Hazard]),
			onChanged: value => CollisionViewer.Options.ColorUpdated(CollisionViewer.ColliderType.Hazard, value),
			alwaysDefault: false
		);
		CollisionViewer.Options.ColliderColors[CollisionViewer.ColliderType.Hazard] = CollisionViewer.Options.hazardColorEntry.Value;

		CollisionViewer.Options.pushableColorEntry = CreateOption
		(
			section: section,
			key: "Color for Pushable Colliders",
			description: "",
			order: 4,
			defaultValue: Utility.ConvertHexToColor(CollisionViewer.Options.DefaultColliderColors[CollisionViewer.ColliderType.Pushable]),
			onChanged: value => CollisionViewer.Options.ColorUpdated(CollisionViewer.ColliderType.Pushable, value),
			alwaysDefault: false
		);
		CollisionViewer.Options.ColliderColors[CollisionViewer.ColliderType.Pushable] = CollisionViewer.Options.pushableColorEntry.Value;

		CollisionViewer.Options.staticColorEntry = CreateOption
		(
			section: section,
			key: "Color for Static Colliders",
			description: "",
			order: 3,
			defaultValue: Utility.ConvertHexToColor(CollisionViewer.Options.DefaultColliderColors[CollisionViewer.ColliderType.Static]),
			onChanged: value => CollisionViewer.Options.ColorUpdated(CollisionViewer.ColliderType.Static, value),
			alwaysDefault: false
		);
		CollisionViewer.Options.ColliderColors[CollisionViewer.ColliderType.Static] = CollisionViewer.Options.staticColorEntry.Value;

		CollisionViewer.Options.transitionColorEntry = CreateOption
		(
			section: section,
			key: "Color for Transition Colliders",
			description: "",
			order: 2,
			defaultValue: Utility.ConvertHexToColor(CollisionViewer.Options.DefaultColliderColors[CollisionViewer.ColliderType.Transition]),
			onChanged: value => CollisionViewer.Options.ColorUpdated(CollisionViewer.ColliderType.Transition, value),
			alwaysDefault: false
		);
		CollisionViewer.Options.ColliderColors[CollisionViewer.ColliderType.Transition] = CollisionViewer.Options.transitionColorEntry.Value;

		CollisionViewer.Options.triggerColorEntry = CreateOption
		(
			section: section,
			key: "Color for Trigger Colliders",
			description: "",
			order: 1,
			defaultValue: Utility.ConvertHexToColor(CollisionViewer.Options.DefaultColliderColors[CollisionViewer.ColliderType.Trigger]),
			onChanged: value => CollisionViewer.Options.ColorUpdated(CollisionViewer.ColliderType.Trigger, value),
			alwaysDefault: false
		);
		CollisionViewer.Options.ColliderColors[CollisionViewer.ColliderType.Trigger] = CollisionViewer.Options.triggerColorEntry.Value;
	}

	private ConfigEntry<T> CreateOption<T>(string section, string key, string description, int order, T defaultValue, Action<T> onChanged = null, bool alwaysDefault = true, ConfigurationManagerAttributes attributes = null, AcceptableValueBase acceptableValues = null)
	{
		ConfigurationManagerAttributes attr = attributes ?? new();
		attr.Order = order;
		ConfigDescription desc = new ConfigDescription(description, acceptableValues, attr);
		ConfigEntry<T> entry = Plugin.Instance.Config.Bind(section, key, defaultValue, desc);

		if (alwaysDefault)
			entry.Value = defaultValue;

		if (onChanged != null)
			entry.SettingChanged += (s, e) => onChanged(entry.Value);

		return entry;
	}
}