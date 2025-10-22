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

	private static readonly List<ColliderTracker> activeColliders = new();
	private static readonly Dictionary<ColliderTracker, DrawnColliderInfo> drawnColliders = new();
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
		new(ColliderType.Static, t => t.Collider.IsStatic),
		new(ColliderType.Trigger, t => t.Collider.IsTrigger),
	};
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

		for (int i = activeColliders.Count - 1; i >= 0; i--)
			Unregister(activeColliders[i]);

		activeColliders.Clear();
		drawnColliders.Clear();
		Destroy(lineHolder.gameObject);

		Logger.Log("Collision viewer is disabled.");
	}

	private void Update()
	{
		foreach (ColliderTracker tracker in activeColliders)
			DrawOrUpdateCollider(tracker);
	}

	public static void Register(ColliderTracker tracker)
	{
		if (!activeColliders.Contains(tracker))
			activeColliders.Add(tracker);
	}

	public static void Unregister(ColliderTracker tracker)
	{
		if (!activeColliders.Contains(tracker))
			return;

		activeColliders.Remove(tracker);
		Hide(tracker);
		Destroy(tracker);
	}

	public static void Show(ColliderTracker tracker)
	{
		Register(tracker);
	}

	public static void Hide(ColliderTracker tracker)
	{
		activeColliders.Remove(tracker);

		if (!drawnColliders.ContainsKey(tracker))
			return;

		Destroy(drawnColliders[tracker].lineRenderer);
		drawnColliders.Remove(tracker);
	}

	private void DrawOrUpdateCollider(ColliderTracker tracker)
	{
		Vector3 pos = tracker.transform.position;
		Quaternion rot = tracker.transform.rotation;
		bool hasInfo = drawnColliders.TryGetValue(tracker, out DrawnColliderInfo drawInfo);

		// If collider has already been drawn and it hasn't updated, don't redraw it
		if (hasInfo && drawInfo.lastPosition == pos && drawInfo.lastRotation == rot)
			return;

		// If collider has already been drawn, delete the old drawing before redrawing it
		if (hasInfo && drawInfo.lineRenderer != null)
			Destroy(drawInfo.lineRenderer.gameObject);

		// Redraw the collider
		LineRenderer newLineRenderer = null;
		ColliderType? colType = GetColliderType(tracker);

		if (colType == null)
			return;

		Color color = GetColorForColliderType((ColliderType)colType);

		switch (tracker.Shape)
		{
			case BC_AABB aabb:
				newLineRenderer = CollisionDrawer.DrawAABB(aabb, Vector3.zero, color, lineHolderAABB);
				break;
			case BC_OBB obb:
				if (tracker.Layer == LayerMask.NameToLayer("Dynamite"))
					obb.P = new Vector3(tracker.transform.position.x, obb.P.y, tracker.transform.position.z);

				newLineRenderer = CollisionDrawer.DrawOBB(obb, Vector3.zero, color, lineHolderOBB);
				break;
			case BC_Cylinder8 cylinder8:
				newLineRenderer = CollisionDrawer.DrawCylinder(cylinder8, Vector3.zero, color, lineHolderCylinder8);
				break;
			case BC_CylinderN cylinderN:
				newLineRenderer = CollisionDrawer.DrawCylinder(cylinderN, Vector3.zero, color, lineHolderCylinderN);
				break;
			case BC_Sphere sphere:
				newLineRenderer = CollisionDrawer.DrawSphere(sphere, Vector3.zero, color, 32, lineHolderSphere);
				break;
			case BC_Prism prism:
				newLineRenderer = CollisionDrawer.DrawPrism(prism, Vector3.zero, color, lineHolderPrism);
				break;
			case BC_Plane plane:
				newLineRenderer = CollisionDrawer.DrawPlane(plane, Vector3.zero, color, lineHolderPlane);
				break;
			case BC_Point point:
				newLineRenderer = CollisionDrawer.DrawPoint(point, Vector3.zero, color, 0.05f, 16, lineHolderPoint);
				break;
		}

		drawInfo.lineRenderer = newLineRenderer;
		drawInfo.lastPosition = pos;
		drawInfo.lastRotation = rot;
		drawnColliders[tracker] = drawInfo;
	}

	private void Init()
	{
		foreach (BC_Collider collider in Resources.FindObjectsOfTypeAll<BC_Collider>())
		{
			if (collider.GetComponent<ColliderTracker>() == null)
				collider.gameObject.AddComponent<ColliderTracker>();
		}

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

	private ColliderType? GetColliderType(ColliderTracker tracker)
	{
		// Skip layer exclusions
		if ((layerExclusions & (1 << tracker.Layer)) != 0)
			return null;

		for (int i = 0; i < colliderMappings.Length; i++)
		{
			if (colliderMappings[i].condition(tracker))
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
		public readonly Func<ColliderTracker, bool> condition;

		public ColliderMapping(ColliderType type, Func<ColliderTracker, bool> condition)
		{
			this.type = type;
			this.condition = condition;
		}
	}
}