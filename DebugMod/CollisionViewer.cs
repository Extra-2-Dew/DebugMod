using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ID2.DebugMod;

internal class CollisionViewer : MonoBehaviour
{
	[Flags]
	public enum ColliderType
	{
		None = 0,
		Entity = 1 << 0,
		Hazard = 1 << 1,
		Pushable = 1 << 2,
		Static = 1 << 3,
		Transition = 1 << 4,
		Trigger = 1 << 5,
		All = Entity | Hazard | Pushable | Transition | Static | Trigger
	}

	private static readonly List<BC_Collider> trackedColliders = new();
	private static readonly Dictionary<BC_Collider, DrawnColliderInfo> drawnColliders = new();
	private static CollisionViewer instance;
	private static readonly int layerExclusions = LayerMask.GetMask("Floor", "Ignore Raycast", "Sand");
	private static readonly ColliderMapping[] colliderMappings =
	{
		new(ColliderType.Entity, t => t.GetComponent<Entity>() || t.Layer == LayerMask.NameToLayer("Enemy")),
		new(ColliderType.Hazard, t => t.GetComponent<DamageArea>() || t.GetComponent<EnvirodeathArea>()),
		new(ColliderType.Pushable, t => t.GetComponent<RoomPushable>()),
		new(ColliderType.Transition, t => t.GetComponent<SceneDoor>() || t.GetComponent<RoomDoor>()),
		new(ColliderType.Static, t => t.IsStatic),
		new(ColliderType.Trigger, t => t.IsTrigger),
	};
	private readonly int puzzleLayer = LayerMask.NameToLayer("Puzzle");
	private readonly int dynamiteLayer = LayerMask.NameToLayer("Dynamite");
	private Transform lineHolder;
	private Transform lineHolderEntity;
	private Transform lineHolderHazard;
	private Transform lineHolderPushable;
	private Transform lineHolderStatic;
	private Transform lineHolderTransition;
	private Transform lineHolderTrigger;

	public static CollisionViewer Instance => instance;

	private void Awake()
	{
		instance = this;
	}

	private void OnEnable()
	{
		//Events.OnPlayerSpawn += OnPlayerSpawn;
		Init();

		Logger.Log("Collision viewer is enabled.");
	}

	private void OnDisable()
	{
		//Events.OnPlayerSpawn -= OnPlayerSpawn;

		drawnColliders.Clear();
		Destroy(lineHolder.gameObject);

		Logger.Log("Collision viewer is disabled.");
	}

	private void Update()
	{
		foreach (BC_Collider collider in trackedColliders)
			DrawOrUpdateCollider(collider);
	}

	private static void TrackCollider(BC_Collider collider)
	{
		trackedColliders.Add(collider);
	}

	private static void UntrackCollider(BC_Collider collider)
	{
		trackedColliders.Remove(collider);

		if (!drawnColliders.ContainsKey(collider))
			return;

		Destroy(drawnColliders[collider].lineRenderer);
		drawnColliders.Remove(collider);
	}

	private void DrawOrUpdateCollider(BC_Collider collider)
	{
		Vector3 pos = collider.transform.position;
		Quaternion rot = collider.transform.rotation;
		bool hasInfo = drawnColliders.TryGetValue(collider, out DrawnColliderInfo drawInfo);

		// If collider has already been drawn and it hasn't updated, don't redraw it
		if (hasInfo && drawInfo.lastPosition == pos && drawInfo.lastRotation == rot)
			return;

		// If collider has already been drawn, delete the old drawing before redrawing it
		if (hasInfo && drawInfo.lineRenderer != null)
			Destroy(drawInfo.lineRenderer.gameObject);

		ColliderType? colType = GetColliderType(collider);

		if (colType == null || !Options.EnabledColliders.Contains((ColliderType)colType))
			return;

		Color color = GetColorForColliderType((ColliderType)colType);

		// Update ice block's position with each spawn
		if (collider.Shape is BC_AABB ice && collider.Layer == puzzleLayer && collider.name.StartsWith("SliceIceBlock"))
		{
			ice.P = collider.transform.position;
			ice.P.y += 0.5f;
		}
		// Update dynamite's position with each spawn
		else if (collider.Shape is BC_OBB dyna && collider.Layer == dynamiteLayer)
		{
			dyna.P = collider.transform.position;
			dyna.P.y += 0.5f;
		}

		Transform lineParent = colType switch
		{
			ColliderType.Entity => lineHolderEntity,
			ColliderType.Hazard => lineHolderHazard,
			ColliderType.Pushable => lineHolderPushable,
			ColliderType.Static => lineHolderStatic,
			ColliderType.Transition => lineHolderTransition,
			ColliderType.Trigger => lineHolderTrigger,
			_ => lineHolder
		};

		LineRenderer newLineRenderer = collider.Shape switch
		{
			BC_AABB aabb => CollisionDrawer.DrawAABB(aabb, Vector3.zero, color, lineParent),
			BC_OBB obb => CollisionDrawer.DrawOBB(obb, Vector3.zero, color, lineParent),
			BC_Cylinder8 cylinder8 => CollisionDrawer.DrawCylinder(cylinder8, Vector3.zero, color, lineParent),
			BC_CylinderN cylinderN => CollisionDrawer.DrawCylinder(cylinderN, Vector3.zero, color, lineParent),
			BC_Sphere sphere => CollisionDrawer.DrawSphere(sphere, Vector3.zero, color, 32, lineParent),
			BC_Prism prism => CollisionDrawer.DrawPrism(prism, Vector3.zero, color, lineParent),
			BC_Plane plane => CollisionDrawer.DrawPlane(plane, Vector3.zero, color, lineParent),
			BC_Point point => CollisionDrawer.DrawPoint(point, Vector3.zero, color, 0.05f, 16, lineParent),
			_ => null
		};

		drawInfo.lineRenderer = newLineRenderer;
		drawInfo.lastPosition = pos;
		drawInfo.lastRotation = rot;
		drawnColliders[collider] = drawInfo;
	}

	private void Init()
	{
		// Setup line parents
		lineHolder = new GameObject("DebugMod_CollisionViewerLines").transform;
		lineHolderEntity = new GameObject("LineHolder_Entity").transform;
		lineHolderEntity.SetParent(lineHolder);
		lineHolderHazard = new GameObject("LineHolder_Hazard").transform;
		lineHolderHazard.SetParent(lineHolder);
		lineHolderPushable = new GameObject("LineHolder_Pushable").transform;
		lineHolderPushable.SetParent(lineHolder);
		lineHolderStatic = new GameObject("LineHolder_Static").transform;
		lineHolderStatic.SetParent(lineHolder);
		lineHolderTransition = new GameObject("LineHolder_Transition").transform;
		lineHolderTransition.SetParent(lineHolder);
		lineHolderTrigger = new GameObject("LineHolder_Trigger").transform;
		lineHolderTrigger.SetParent(lineHolder);
	}

	private void OnPlayerSpawn(Entity _, GameObject __, PlayerController ___)
	{
		Init();
	}

	private ColliderType? GetColliderType(BC_Collider collider)
	{
		// Skip layer exclusions
		if ((layerExclusions & (1 << collider.Layer)) != 0)
			return null;

		for (int i = 0; i < colliderMappings.Length; i++)
		{
			if (colliderMappings[i].condition(collider))
				return colliderMappings[i].type;
		}

		return null;
	}

	private Color GetColorForColliderType(ColliderType colType)
	{
		if (Options.ColliderColors.TryGetValue(colType, out Color color))
			return color;

		return Color.green;
	}

	public struct Options
	{
		public static ConfigEntry<bool> toggleEntry;
		public static ConfigEntry<ColliderType> showColliderTypesEntry;
		public static ConfigEntry<KeyboardShortcut> toggleHotkeyEntry;
		public static ConfigEntry<Color> entityColorEntry;
		public static ConfigEntry<Color> hazardColorEntry;
		public static ConfigEntry<Color> pushableColorEntry;
		public static ConfigEntry<Color> staticColorEntry;
		public static ConfigEntry<Color> transitionColorEntry;
		public static ConfigEntry<Color> triggerColorEntry;

		public static List<ColliderType> EnabledColliders
		{
			get
			{
				ColliderType value = showColliderTypesEntry.Value;

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

		public readonly struct Defaults
		{
			public static Dictionary<ColliderType, string> DefaultColliderColors { get; } = new()
			{
				{ ColliderType.Entity, "#ffff00" },
				{ ColliderType.Hazard, "#ff0000" },
				{ ColliderType.Pushable, "#ff7b00" },
				{ ColliderType.Static, "#00ff00" },
				{ ColliderType.Transition, "#ffffff" },
				{ ColliderType.Trigger, "#00ffff" },
			};
			public static KeyboardShortcut ToggleHotkeyDefault { get; } = new(KeyCode.F2);
		}

		public static void ToggleChanged(bool value)
		{
			DebugMod.Instance.ToggleColliders(value);
		}

		public static void SelectedColliderTypesUpdated(ColliderType value)
		{
			if (!toggleEntry.Value)
				return;

			Instance.lineHolderEntity.gameObject.SetActive((value & ColliderType.Entity) != 0);
			Instance.lineHolderHazard.gameObject.SetActive((value & ColliderType.Hazard) != 0);
			Instance.lineHolderPushable.gameObject.SetActive((value & ColliderType.Pushable) != 0);
			Instance.lineHolderStatic.gameObject.SetActive((value & ColliderType.Static) != 0);
			Instance.lineHolderTransition.gameObject.SetActive((value & ColliderType.Transition) != 0);
			Instance.lineHolderTrigger.gameObject.SetActive((value & ColliderType.Trigger) != 0);
		}

		public static void ColorUpdated(ColliderType type, Color value)
		{
			ColliderColors[type] = value;

			Transform lineParent = type switch
			{
				ColliderType.Entity => Instance.lineHolderEntity,
				ColliderType.Hazard => Instance.lineHolderHazard,
				ColliderType.Pushable => Instance.lineHolderPushable,
				ColliderType.Static => Instance.lineHolderStatic,
				ColliderType.Transition => Instance.lineHolderTransition,
				ColliderType.Trigger => Instance.lineHolderTrigger,
				_ => null
			};

			if (lineParent == null)
				return;

			foreach (LineRenderer line in lineParent.GetComponentsInChildren<LineRenderer>())
			{
				line.startColor = value;
				line.endColor = value;
			}
		}
	}

	private struct DrawnColliderInfo
	{
		public LineRenderer lineRenderer;
		public Vector3 lastPosition;
		public Quaternion lastRotation;
	}

	private readonly struct ColliderMapping
	{
		public readonly ColliderType type;
		public readonly Func<BC_Collider, bool> condition;

		public ColliderMapping(ColliderType type, Func<BC_Collider, bool> condition)
		{
			this.type = type;
			this.condition = condition;
		}
	}

	[HarmonyPatch]
	private class Patches
	{
		[HarmonyPostfix, HarmonyPatch(typeof(BC_ColliderHolder), nameof(BC_ColliderHolder.AddCollider))]
		private static void TrackColliders(ref BC_Collider collider)
		{
			TrackCollider(collider);
		}

		[HarmonyPostfix, HarmonyPatch(typeof(BC_ColliderHolder), nameof(BC_ColliderHolder.RemoveCollider))]
		private static void UntrackColliders(ref BC_Collider collider)
		{
			UntrackCollider(collider);
		}
	}
}