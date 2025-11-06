using BepInEx.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ColliderType = ID2.DebugMod.CollisionViewer.ColliderType;

namespace ID2.DebugMod;

internal class Options
{
	#region Collision Viewer
	private static ConfigEntry<bool> collisionViewer_toggle;
	private static ConfigEntry<KeyboardShortcut> collisionViewer_toggleHotkey;
	private static ConfigEntry<ColliderType> collisionViewer_showColliderTypes;
	private static ConfigEntry<Color> collisionViewer_EntityColor;
	private static ConfigEntry<Color> collisionViewer_HazardColor;
	private static ConfigEntry<Color> collisionViewer_PushableColor;
	private static ConfigEntry<Color> collisionViewer_StaticColor;
	private static ConfigEntry<Color> collisionViewer_TransitionColor;
	private static ConfigEntry<Color> collisionViewer_TriggerColor;
	#endregion

	public static bool CollisionViewerEnabled => collisionViewer_toggle.Value;
	public static List<ColliderType> CollisionViewerEnabledColliders
	{
		get
		{
			ColliderType value = collisionViewer_showColliderTypes.Value;

			if (value == ColliderType.None)
				return [];

			if (value == ColliderType.All)
				return Enum.GetValues(typeof(ColliderType))
					.Cast<ColliderType>()
					.Where(c => c != ColliderType.None && c != ColliderType.All)
					.ToList();

			return Enum.GetValues(typeof(ColliderType))
				.Cast<ColliderType>()
				.Where(c => c != ColliderType.None && c != ColliderType.All && (value & c) != 0)
				.ToList();
		}
	}
	public static Dictionary<ColliderType, Color> ColliderColors { get; set; } = new()
	{
		{ ColliderType.Entity, Color.green },
		{ ColliderType.Hazard, Color.green },
		{ ColliderType.Pushable, Color.green },
		{ ColliderType.Static, Color.green },
		{ ColliderType.Transition, Color.green },
		{ ColliderType.Trigger, Color.green },
	};

	public Options()
	{
		SetupCollisionViewerOptions();
	}

	public void CheckForHotkeys()
	{
		if (collisionViewer_toggleHotkey.Value.IsDown())
			collisionViewer_toggle.Value = !collisionViewer_toggle.Value;
	}

	private void SetupCollisionViewerOptions()
	{
		collisionViewer_showColliderTypes = BindConfig(
			section: "Collision Viewer",
			key: "Collider Types to Show",
			defaultValue: ColliderType.All,
			description: "Select which collider types to show.",
			onChanged: value =>
			{
				if (!collisionViewer_toggle.Value)
					return;

				ColliderType newValue = collisionViewer_showColliderTypes.Value;
				CollisionViewer.Instance.SelectedColliderTypesUpdated(newValue);
			}
		);

#pragma warning disable IDE0200 // Remove unnecessary lambda expression
		collisionViewer_toggle = BindConfig(
			section: "Collision Viewer",
			key: "Collision Viewer",
			defaultValue: false,
			description: "Toggles the display of colliders/hitboxes.",
			onChanged: value => { DebugMod.Instance.ToggleColliders(value); }
		);
#pragma warning restore IDE0200 // Remove unnecessary lambda expression

		collisionViewer_toggleHotkey = BindConfig(
			section: "Collision Viewer",
			key: "Collision Viewer Hotkey",
			defaultValue: new KeyboardShortcut(KeyCode.F2),
			description: "Toggls the display of colliders/hitboxes.",
			alwaysDefault: false
		);

		collisionViewer_EntityColor = BindConfig(
			section: "Collision Viewer",
			key: "Color for Entity Colliders",
			defaultValue: Utility.ConvertHexToColor(CollisionViewer.DefaultColliderColors[ColliderType.Entity]),
			alwaysDefault: false,
			onChanged: value =>
			{
				ColliderColors[ColliderType.Entity] = value;
				CollisionViewer.Instance.ColorUpdated(ColliderType.Entity, value);
			}
		);
		ColliderColors[ColliderType.Entity] = collisionViewer_EntityColor.Value;

		collisionViewer_HazardColor = BindConfig(
			section: "Collision Viewer",
			key: "Color for Hazard Colliders",
			defaultValue: Utility.ConvertHexToColor(CollisionViewer.DefaultColliderColors[ColliderType.Hazard]),
			alwaysDefault: false,
			onChanged: value =>
			{
				ColliderColors[ColliderType.Hazard] = value;
				CollisionViewer.Instance.ColorUpdated(ColliderType.Hazard, value);
			}
		);
		ColliderColors[ColliderType.Hazard] = collisionViewer_HazardColor.Value;

		collisionViewer_PushableColor = BindConfig(
			section: "Collision Viewer",
			key: "Color for Pushable Colliders",
			defaultValue: Utility.ConvertHexToColor(CollisionViewer.DefaultColliderColors[ColliderType.Pushable]),
			alwaysDefault: false,
			onChanged: value =>
			{
				ColliderColors[ColliderType.Pushable] = value;
				CollisionViewer.Instance.ColorUpdated(ColliderType.Pushable, value);
			}
		);
		ColliderColors[ColliderType.Pushable] = collisionViewer_PushableColor.Value;

		collisionViewer_StaticColor = BindConfig(
			section: "Collision Viewer",
			key: "Color for Static Colliders",
			defaultValue: Utility.ConvertHexToColor(CollisionViewer.DefaultColliderColors[ColliderType.Static]),
			alwaysDefault: false,
			onChanged: value =>
			{
				ColliderColors[ColliderType.Static] = value;
				CollisionViewer.Instance.ColorUpdated(ColliderType.Static, value);
			}
		);
		ColliderColors[ColliderType.Static] = collisionViewer_StaticColor.Value;

		collisionViewer_TransitionColor = BindConfig(
			section: "Collision Viewer",
			key: "Color for Transition Colliders",
			defaultValue: Utility.ConvertHexToColor(CollisionViewer.DefaultColliderColors[ColliderType.Transition]),
			alwaysDefault: false,
			onChanged: value =>
			{
				ColliderColors[ColliderType.Transition] = value;
				CollisionViewer.Instance.ColorUpdated(ColliderType.Transition, value);
			}
		);
		ColliderColors[ColliderType.Transition] = collisionViewer_TransitionColor.Value;

		collisionViewer_TriggerColor = BindConfig(
			section: "Collision Viewer",
			key: "Color for Trigger Colliders",
			defaultValue: Utility.ConvertHexToColor(CollisionViewer.DefaultColliderColors[ColliderType.Trigger]),
			alwaysDefault: false,
			onChanged: value =>
			{
				ColliderColors[ColliderType.Trigger] = value;
				CollisionViewer.Instance.ColorUpdated(ColliderType.Trigger, value);
			}
		);
		ColliderColors[ColliderType.Trigger] = collisionViewer_TriggerColor.Value;
	}

	private ConfigEntry<T> BindConfig<T>(string section, string key, T defaultValue, string description = "", bool alwaysDefault = true, Action<T> onChanged = null)
	{
		ConfigEntry<T> entry = Plugin.Instance.Config.Bind(section, key, defaultValue, description);

		if (alwaysDefault)
			entry.Value = defaultValue;

		if (onChanged != null)
			entry.SettingChanged += (s, e) => onChanged(entry.Value);

		return entry;
	}
}