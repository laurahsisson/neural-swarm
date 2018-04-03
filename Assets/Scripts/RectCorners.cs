using UnityEngine;

[System.Serializable]
public struct RectCorners {
	public Vector2 topLeft;
	public Vector2 topRight;
	public Vector2 bottomLeft;
	public Vector2 bottomRight;
	public RectCorners(RectTransform rt) {
		Vector3[] corners = new Vector3[4];
		rt.GetWorldCorners(corners);
		topLeft = corners[0];
		topRight = corners[1];
		bottomLeft = corners[2];
		bottomRight = corners[3];
	}
}
