using UnityEngine;

namespace ID2.DebugMod;

internal class ColliderTracker : MonoBehaviour
{
	public BC_Collider Collider { get; private set; }
	public BC_Shape Shape { get; private set; }
	public int Layer { get; private set; }

	private void Awake()
	{
		Collider = GetComponent<BC_Collider>();
		Shape = Collider.Shape;
		Layer = gameObject.layer;
	}

	private void OnEnable() => CollisionViewer.Show(this);

	private void OnDisable() => CollisionViewer.Hide(this);
}