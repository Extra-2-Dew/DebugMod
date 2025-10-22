using UnityEngine;

namespace ID2.DebugMod;

internal class CollisionDrawer
{
	public static LineRenderer DrawOBB(BC_OBB obb, Vector3 offset, Color color, Transform parent = null)
	{
		return DrawBox(obb.GetCorner, offset, color, parent);
	}

	public static LineRenderer DrawAABB(BC_AABB aabb, Vector3 offset, Color color, Transform parent = null)
	{
		return DrawBox(aabb.GetCorner, offset, color, parent);
	}

	public static LineRenderer DrawSphere(BC_Sphere S, Vector3 offset, Color color, int steps = 32, Transform parent = null)
	{
		// Create a GameObject for the sphere lines
		GameObject go = new GameObject("Sphere_Line");
		if (parent != null) go.transform.parent = parent;

		LineRenderer lr = go.AddComponent<LineRenderer>();
		lr.positionCount = steps * 6; // 3 circles, each with 'steps' segments, each segment has 2 vertices
		lr.startWidth = 0.02f;
		lr.endWidth = 0.02f;
		lr.material = new Material(Shader.Find("Sprites/Default"));
		lr.startColor = color;
		lr.endColor = color;
		lr.loop = false;

		Vector3[] vertices = new Vector3[steps * 6];
		int v = 0;

		// Define the 3 axes for orthogonal circles
		Vector3[] axesX = { Vector3.right, Vector3.up, Vector3.forward };
		Vector3[] axesY = { Vector3.up, Vector3.forward, Vector3.right };

		for (int c = 0; c < 3; c++)
		{
			Vector3 X = axesX[c] * S.R;
			Vector3 Y = axesY[c] * S.R;
			Vector3 from = S.P + offset + X;

			float step = 1f / steps;
			for (float t = step; t <= 1f; t += step)
			{
				float angle = t * Mathf.PI * 2f;
				Vector3 to = S.P + offset + X * Mathf.Cos(angle) + Y * Mathf.Sin(angle);
				vertices[v++] = from;
				vertices[v++] = to;
				from = to;
			}
		}

		lr.positionCount = v;
		lr.SetPositions(vertices);
		return lr;
	}

	public static LineRenderer DrawCylinder(BC_Cylinder8 cyl, Vector3 offset, Color color, Transform parent = null)
	{
		GameObject go = new GameObject("Cylinder_Line");
		if (parent != null) go.transform.parent = parent;

		LineRenderer lr = go.AddComponent<LineRenderer>();
		lr.startWidth = 0.02f;
		lr.endWidth = 0.02f;
		lr.material = new Material(Shader.Find("Sprites/Default"));
		lr.startColor = color;
		lr.endColor = color;
		lr.loop = false;

		// 16 corners total: 0-7 bottom, 8-15 top
		Vector3[] vertices = new Vector3[8 * 6]; // 3 lines per corner: bottom loop, top loop, vertical
		int v = 0;
		int num = 7;

		for (int i = 0; i < 8; i++)
		{
			Vector3 bottom = cyl.GetCorner(i) + offset;
			Vector3 top = cyl.GetCorner(i + 8) + offset;

			Vector3 nextBottom = cyl.GetCorner(num) + offset;
			Vector3 nextTop = cyl.GetCorner(num + 8) + offset;

			// bottom loop
			vertices[v++] = bottom;
			vertices[v++] = nextBottom;

			// top loop
			vertices[v++] = top;
			vertices[v++] = nextTop;

			// vertical
			vertices[v++] = bottom;
			vertices[v++] = top;

			num = i;
		}

		lr.positionCount = v;
		lr.SetPositions(vertices);
		return lr;
	}

	public static LineRenderer DrawCylinder(BC_CylinderN cyl, Vector3 offset, Color color, Transform parent = null)
	{
		GameObject go = new GameObject("CylinderN_Line");
		if (parent != null) go.transform.parent = parent;

		LineRenderer lr = go.AddComponent<LineRenderer>();
		lr.startWidth = 0.02f;
		lr.endWidth = 0.02f;
		lr.material = new Material(Shader.Find("Sprites/Default"));
		lr.startColor = color;
		lr.endColor = color;
		lr.loop = false;

		int numCorners = cyl.NumCorners / 2;
		Vector3[] vertices = new Vector3[numCorners * 6]; // bottom loop, top loop, verticals
		int v = 0;
		int prev = numCorners - 1;

		for (int i = 0; i < numCorners; i++)
		{
			Vector3 bottom = cyl.GetCorner(i) + offset;
			Vector3 top = cyl.GetCorner(i + numCorners) + offset;

			Vector3 prevBottom = cyl.GetCorner(prev) + offset;
			Vector3 prevTop = cyl.GetCorner(prev + numCorners) + offset;

			// bottom loop
			vertices[v++] = bottom;
			vertices[v++] = prevBottom;

			// top loop
			vertices[v++] = top;
			vertices[v++] = prevTop;

			// vertical
			vertices[v++] = bottom;
			vertices[v++] = top;

			prev = i;
		}

		lr.positionCount = v;
		lr.SetPositions(vertices);
		return lr;
	}

	public static LineRenderer DrawPrism(BC_Prism prism, Vector3 offset, Color color, Transform parent = null)
	{
		GameObject go = new GameObject("Prism_Line");
		if (parent != null) go.transform.parent = parent;

		LineRenderer lr = go.AddComponent<LineRenderer>();
		lr.startWidth = lr.endWidth = 0.02f;
		lr.material = new Material(Shader.Find("Sprites/Default"));
		lr.startColor = lr.endColor = color;
		lr.loop = false;

		Vector3[] c = new Vector3[6];
		for (int i = 0; i < 6; i++)
			c[i] = prism.GetCorner(i) + offset;

		Vector3[] edges = {
			c[0], c[1], c[1], c[2], c[2], c[0], // bottom triangle
			c[3], c[4], c[4], c[5], c[5], c[3], // top triangle
			c[0], c[3], c[1], c[4], c[2], c[5]  // verticals
		};

		lr.positionCount = edges.Length;
		lr.SetPositions(edges);
		return lr;
	}

	public static LineRenderer DrawPlane(BC_Plane plane, Vector3 offset, Color color, Transform parent = null)
	{
		GameObject go = new GameObject("Plane_Line");
		if (parent != null) go.transform.parent = parent;

		LineRenderer lr = go.AddComponent<LineRenderer>();
		lr.startWidth = lr.endWidth = 0.02f;
		lr.material = new Material(Shader.Find("Sprites/Default"));
		lr.startColor = lr.endColor = color;
		lr.loop = false;

		// 4 corners, 4 edges (each edge has 2 vertices)
		Vector3[] vertices = new Vector3[8];
		int i = 3;
		int v = 0;
		for (int j = 0; j < 4; j++)
		{
			vertices[v++] = plane.GetCorner(j) + offset;
			vertices[v++] = plane.GetCorner(i) + offset;
			i = j;
		}

		lr.positionCount = vertices.Length;
		lr.SetPositions(vertices);

		return lr;
	}

	public static LineRenderer DrawPoint(BC_Point point, Vector3 offset, Color color, float radius = 0.05f, int steps = 16, Transform parent = null)
	{
		GameObject go = new GameObject("Point_Line");
		if (parent != null) go.transform.parent = parent;

		LineRenderer lr = go.AddComponent<LineRenderer>();
		lr.startWidth = lr.endWidth = 0.02f;
		lr.material = new Material(Shader.Find("Sprites/Default"));
		lr.startColor = lr.endColor = color;
		lr.loop = false;

		Vector3 center = point.P + offset;
		Vector3[] vertices = new Vector3[steps * 6];
		int v = 0;

		Vector3[] axesX = { Vector3.right, Vector3.up, Vector3.forward };
		Vector3[] axesY = { Vector3.up, Vector3.forward, Vector3.right };

		for (int c = 0; c < 3; c++)
		{
			Vector3 X = axesX[c] * radius;
			Vector3 Y = axesY[c] * radius;
			Vector3 from = center + X;
			float step = 1f / steps;

			for (float t = step; t <= 1f; t += step)
			{
				float angle = t * Mathf.PI * 2f;
				Vector3 to = center + X * Mathf.Cos(angle) + Y * Mathf.Sin(angle);
				vertices[v++] = from;
				vertices[v++] = to;
				from = to;
			}
		}

		lr.positionCount = v;
		lr.SetPositions(vertices);

		return lr;
	}

	private static LineRenderer DrawBox(System.Func<int, Vector3> getCorner, Vector3 offset, Color color, Transform parent = null)
	{
		GameObject go = new GameObject("Box_Line");
		if (parent != null) go.transform.parent = parent;

		LineRenderer lr = go.AddComponent<LineRenderer>();
		lr.positionCount = 24; // 12 edges * 2
		lr.startWidth = 0.02f;
		lr.endWidth = 0.02f;
		lr.material = new Material(Shader.Find("Sprites/Default"));
		lr.startColor = color;
		lr.endColor = color;
		lr.loop = false;

		Vector3[] c = new Vector3[8];
		for (int i = 0; i < 8; i++)
			c[i] = getCorner(i) + offset;

		// 12 edges
		Vector3[] edges = {
			c[0], c[1], c[1], c[2], c[2], c[3], c[3], c[0], // bottom
			c[4], c[5], c[5], c[6], c[6], c[7], c[7], c[4], // top
			c[0], c[4], c[1], c[5], c[2], c[6], c[3], c[7]  // verticals
		};

		lr.SetPositions(edges);
		return lr;
	}
}