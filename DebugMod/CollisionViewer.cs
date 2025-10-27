using HarmonyLib;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ID2.DebugMod;

internal class CollisionViewer : MonoBehaviour
{
	private enum ColliderType
	{
		Entity,
		Hazard,
		Pushable,
		Transition,
		Static,
		Trigger,
	}

	private static readonly List<BC_Collider> trackedColliders = new();
	private static readonly Dictionary<BC_Collider, DrawnColliderInfo> drawnColliders = new();
	private readonly Dictionary<ColliderType, string> colliderColors = new()
	{
		{ ColliderType.Entity, "#ffff00" },
		{ ColliderType.Hazard, "#ff0000" },
		{ ColliderType.Pushable, "#ff7b00" },
		{ ColliderType.Transition, "#ffffff" },
		{ ColliderType.Static, "#00ff00" },
		{ ColliderType.Trigger, "#00ffff" },
	};
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
	private readonly int dynamiteLayer = LayerMask.NameToLayer("Dynamite");
	private Transform lineHolder;
	private Transform lineHolderAABB;
	private Transform lineHolderOBB;
	private Transform lineHolderCylinder8;
	private Transform lineHolderCylinderN;
	private Transform lineHolderSphere;
	private Transform lineHolderPrism;
	private Transform lineHolderPlane;
	private Transform lineHolderPoint;

	private void OnEnable()
	{
		Events.OnPlayerSpawn += OnPlayerSpawn;
		Init();

		Logger.Log("Collision viewer is enabled.");
	}

	private void OnDisable()
	{
		Events.OnPlayerSpawn -= OnPlayerSpawn;

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

		if (colType == null)
			return;

		Color color = GetColorForColliderType((ColliderType)colType);

		// Update dynamite's position with each spawn
		if (collider.Shape is BC_OBB dyna && collider.Layer == dynamiteLayer)
			dyna.P = new Vector3(collider.transform.position.x, dyna.P.y, collider.transform.position.z);

		LineRenderer newLineRenderer = collider.Shape switch
		{
			BC_AABB aabb => CollisionDrawer.DrawAABB(aabb, Vector3.zero, color, lineHolderAABB),
			BC_OBB obb => CollisionDrawer.DrawOBB(obb, Vector3.zero, color, lineHolderOBB),
			BC_Cylinder8 cylinder8 => CollisionDrawer.DrawCylinder(cylinder8, Vector3.zero, color, lineHolderCylinder8),
			BC_CylinderN cylinderN => CollisionDrawer.DrawCylinder(cylinderN, Vector3.zero, color, lineHolderCylinderN),
			BC_Sphere sphere => CollisionDrawer.DrawSphere(sphere, Vector3.zero, color, 32, lineHolderSphere),
			BC_Prism prism => CollisionDrawer.DrawPrism(prism, Vector3.zero, color, lineHolderPrism),
			BC_Plane plane => CollisionDrawer.DrawPlane(plane, Vector3.zero, color, lineHolderPlane),
			BC_Point point => CollisionDrawer.DrawPoint(point, Vector3.zero, color, 0.05f, 16, lineHolderPoint),
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
		lineHolderAABB = new GameObject("LineHolder_AABB").transform;
		lineHolderAABB.SetParent(lineHolder);
		lineHolderOBB = new GameObject("LineHolder_OBB").transform;
		lineHolderOBB.SetParent(lineHolder);
		lineHolderCylinder8 = new GameObject("LineHolder_Cylinder8").transform;
		lineHolderCylinder8.SetParent(lineHolder);
		lineHolderCylinderN = new GameObject("LineHolder_CylinderN").transform;
		lineHolderCylinderN.SetParent(lineHolder);
		lineHolderSphere = new GameObject("LineHolder_Sphere").transform;
		lineHolderSphere.SetParent(lineHolder);
		lineHolderPrism = new GameObject("LineHolder_Prism").transform;
		lineHolderPrism.SetParent(lineHolder);
		lineHolderPlane = new GameObject("LineHolder_Plane").transform;
		lineHolderPlane.SetParent(lineHolder);
		lineHolderPoint = new GameObject("LineHolder_Point").transform;
		lineHolderPoint.SetParent(lineHolder);
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
		Color color = Color.black;
		bool showCollider = colliderColors.TryGetValue(colType, out string colorHex);

		if (showCollider)
			ColorUtility.TryParseHtmlString(colorHex, out color);

		return color;
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