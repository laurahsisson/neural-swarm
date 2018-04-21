using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class PathfindControl : MonoBehaviour {
	// Convert from world to grid divide by GRID_STEP
	// Convert from grid to world multiply by GRID_STEP
	// The smaller GRID_STEP is, the more granular our map


	private readonly float GRID_STEP = 1f;
	// Overestimation of sqrt(2). If we are within one GRID_STEP square of the goal we are good to stop
	private readonly float CUTOFF_DIST = 1.5f;
	// The max number of colliders we will check against
	private readonly int NUM_COLLIDERS = 10;
	// The distances at which we will check if we hit more than 1 collider
	private readonly float[] CHECK_DISTANCES = new float[]{ 1, 3, 4 };


	private bool[,] grid;

	private struct Node {
		public Vector2 gridPos;
		public float g;
		public float h;
		public float d;
		public List<Vector2> path;

		public Node(Vector2 pos, float g, float h) {
			this.gridPos = pos;
			this.g = g;
			this.h = h;
			this.d = g + h;
			path = new List<Vector2>();
			path.Add(pos);
		}

		public Node(Vector2 pos, float g, float h, Node parent) : this(pos, g, h) {
			path = new List<Vector2>(parent.path);
			path.Add(pos);
		}
	}

	public void InitializeGrid(FlockControl.UnityState us) {
		CircleCollider2D cd = GetComponent<CircleCollider2D>();
		gameObject.transform.localScale = new Vector3(CHECK_DISTANCES [0] * us.maxSize, CHECK_DISTANCES [0] * us.maxSize);
		ContactFilter2D cf = new ContactFilter2D();
		cf.useTriggers = true;
		cf.layerMask = LayerMask.GetMask("Wall");
		cf.useLayerMask = true;
		Collider2D[] others = new Collider2D[NUM_COLLIDERS];
		grid = new bool[(int)(us.roomWidth / GRID_STEP), (int)(us.roomHeight / GRID_STEP)];
			
		for (float x = 0; x < us.roomWidth; x += GRID_STEP) {
			for (float y = 0; y < us.roomHeight; y += GRID_STEP) {
				transform.position = new Vector2(x, y);
				int hit = cd.OverlapCollider(cf, others);
				int gx = (int)(x / GRID_STEP);
				int gy = (int)(y / GRID_STEP);
				if (Vector2.SqrMagnitude(new Vector2(x, y) - (Vector2)us.goal.transform.position) < us.goal.transform.localScale.x * us.goal.transform.localScale.x) {
					grid [gx, gy] = true;
					continue;
				}


				bool foundSpace = false;
				for (int i = CHECK_DISTANCES.Length - 1; i >= 0; i--) {
					float s = CHECK_DISTANCES [i];
					gameObject.transform.localScale = new Vector3(s * us.maxSize, s * us.maxSize);
					hit = cd.OverlapCollider(cf, others);
					if (hit == 0) {
						foundSpace = true;
						break;
					}
					if (hit == 1) {
						foundSpace = false;
						break;
					}
				}
				grid [gx, gy] = foundSpace;
			}
		}
	}

	public Vector2[] CalculatePath(Vector2 goalPos, BirdControl me) {
		Vector2 myGridPos = me.transform.position / GRID_STEP;
		myGridPos = nearestValidPos((int)myGridPos.x, (int)myGridPos.y);

		Vector2 goalGridPos = goalPos / GRID_STEP;
		Node start = new Node(myGridPos, 0, Vector2.Distance(myGridPos, goalGridPos));


		HashSet<Node> open = new HashSet<Node>();
		open.Add(start);
		HashSet<Vector2> examined = new HashSet<Vector2>();
		examined.Add(myGridPos);

		Node final = new Node();
		bool found = false;

		Node minNode = new Node();
		float minD = Mathf.Infinity;

		while (open.Count > 0) {
			float d = Mathf.Infinity;
			Node cur = new Node();
			foreach (Node n in open) {
				if (n.d < d) {
					d = n.d;
					cur = n;
				}
			}

			if (d > 2 * (grid.GetLength(0) + grid.GetLength(1))) {
				// If we traverse more than the entire perimeter of the room in length, it is a good indicator we will be unable to find a path
				break;
			}

			open.Remove(cur);
			float cd = Vector2.Distance(cur.gridPos, goalGridPos);
			if (cd < minD) {
				minD = cd;
				minNode = cur;
			}


			if (Vector2.Distance(cur.gridPos, goalGridPos) < CUTOFF_DIST * GRID_STEP) { 
				cur.path.Add(goalGridPos);
				found = true;
				final = cur;
				break;
			}

			List<Vector2> ns = neighbors(cur, examined);
			foreach (Vector2 p in ns) {
				float ng = Vector2.Distance(cur.gridPos, p); 
				float h = Vector2.Distance(p, goalGridPos);
				Node n = new Node(p, cur.g + ng, h, cur);
				open.Add(n);
			}
		}

		if (!found) {
			me.gameObject.GetComponent<Renderer>().material.color = Color.black;
			drawPath(getWorldPath(minNode), Color.green);
			return new Vector2[0];
		}

		Vector2[] positions = getWorldPath(final);

		return positions;
	}

	private Vector2[] getWorldPath(Node n) {
		Vector2[] positions = new Vector2[n.path.Count];
		int i = 0;
		foreach (Vector2 p in n.path) {
			positions [i] = p * GRID_STEP;
			i++;
		}
		return positions;
	}

	private void drawPath(Vector2[] positions, Color c) {
		for (int i = 0; i < positions.Length - 1; i++) {
			Debug.DrawLine(positions [i], positions [i + 1], Color.red);
		}
	}

	private Vector2 nearestValidPos(int x, int y) {
		if (gridAt(x,y)) {
			return new Vector2(x, y);
		}
		int dist = 1;
		while (dist < grid.GetLength(0) && dist < grid.GetLength(1)) {
			for (int xo = -dist; xo < dist; xo++) {
				int nx = x + xo;
				if (gridAt(nx,y-dist)) {
					return new Vector2(nx, y - dist);
				}
				if (gridAt(nx,y+dist)) {
					return new Vector2(nx, y + dist);
				}
			}


			for (int yo = -dist; yo < dist; yo++) {
				int ny = y + yo;
				if (gridAt(x-dist,ny)) {
					return new Vector2(x - dist, ny);
				}
				if (gridAt(x+dist,ny)) {
					return new Vector2(x + dist, ny);
				}
			}
			dist++;
		}
		// Could not find anywhere to go, just use where we are.
		return new Vector2(x, y); 
	}

	private List<Vector2> neighbors(Node next, HashSet<Vector2> examined) {
		List<Vector2> ns = new List<Vector2>(8); // At most 8 neighbors
		for (int xo = -1; xo <= 1; xo++) {
			for (int yo = -1; yo <= 1; yo++) {
				int x = (int)next.gridPos.x + xo;
				int y = (int)next.gridPos.y + yo;

				Vector2 pos = new Vector2(x, y);
				if (examined.Contains(pos)) {
					continue;
				}
				examined.Add(pos);

				if (gridAt(x,y)) {
					ns.Add(pos);
				}
			}
		}
		return ns;
	}

	private bool gridAt(int x, int y) {
		if (x < 0 || x >= grid.GetLength(0)) {
			return false;
		}
		if (y < 0 || y >= grid.GetLength(1)) {
			return false;
		}
		return grid [x, y];
	}

}
