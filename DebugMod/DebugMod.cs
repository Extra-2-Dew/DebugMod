using UnityEngine;

namespace ID2.DebugMod;

internal class DebugMod : MonoBehaviour
{
	private static DebugMod instance;
	private CollisionViewer colViewer;

	public static DebugMod Instance => instance;

	private void Awake()
	{
		instance = this;

		DontDestroyOnLoad(gameObject);
	}

	public void ToggleColliders(bool show)
	{
		colViewer ??= gameObject.AddComponent<CollisionViewer>();
		colViewer.enabled = show;
	}
}