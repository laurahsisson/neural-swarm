using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class NewPathfindControl : MonoBehaviour {
	// Convert from world to grid divide by GRID_STEP
	// Convert from grid to world multiply by GRID_STEP
	// The smaller GRID_STEP is, the more granular our map

	// When GRID_STEP is 1, there is no difference between world coordinates and grid coordinates.


	// Overestimation of sqrt(2). If we are within one GRID_STEP square of the goal we are good to stop
	private readonly float CUTOFF_DIST = 1.5f;
	// The max number of colliders we will check against
	private readonly float CHECK_DISTANCE = 2f;

	private ContactFilter2D cf;

	private bool[,] grid;

	private List<GraphPoint> graph;
	private GraphPoint goalPoint;

	private Dictionary<GraphPoint,PathDistance> memoized;

	private struct PathDistance {
		public Vector2[] path;
		public float dist;
	}

	private struct GraphPoint {
		public Vector2 pos;
		public List<GraphPoint> neighbors;

		public GraphPoint(Vector2 pos) {
			this.pos = pos;
			neighbors = new List<GraphPoint>();
		}
	}

	private struct Node {
		public GraphPoint point;
		public float g;
		public float h;
		public float d;
		public List<GraphPoint> path;

		public Node(GraphPoint point, float g, float h) {
			this.point = point;
			this.g = g;
			this.h = h;
			this.d = g + h;
			path = new List<GraphPoint>();
			path.Add(point);
		}

		public Node(GraphPoint point, float g, float h, Node parent) : this(point, g, h) {
			path = new List<GraphPoint>(parent.path);
			path.Add(point);
		}
	}

	public void InitializeGrid(FlockControl.UnityState us) {
		graph = new List<GraphPoint>();
		memoized = new Dictionary<GraphPoint, PathDistance>();

		CircleCollider2D cd = GetComponent<CircleCollider2D>();
		gameObject.transform.localScale = new Vector3(CHECK_DISTANCE * us.maxSize, CHECK_DISTANCE * us.maxSize);

		initCF(us);

		Collider2D[] others = new Collider2D[1];
		grid = new bool[(int)(us.roomWidth), (int)(us.roomHeight)];


		for (int x = 0; x < us.roomWidth; x++) {
			for (int y = 0; y < us.roomHeight; y++) {
				if (Vector2.SqrMagnitude(new Vector2(x, y) - (Vector2)us.goal.transform.position) < us.goal.transform.localScale.x * us.goal.transform.localScale.x) {
					grid [(int)x, (int)y] = true;
					continue;
				}

				transform.position = new Vector2(x, y);
				int hit = cd.OverlapCollider(cf, others);
				grid [x, y] = hit == 0;
			}
		}

		initGraph(us.goal.transform.position);
	}

	private void initCF(FlockControl.UnityState us) {
		gameObject.transform.localScale = new Vector3(CHECK_DISTANCE * us.maxSize, CHECK_DISTANCE * us.maxSize);
		cf = new ContactFilter2D();
		cf.useTriggers = true;
		cf.layerMask = LayerMask.GetMask("Wall");
		cf.useLayerMask = true;
	}

	private void initGraph(Vector2 goalPos) {
		for (int x = 1; x < grid.GetLength(0)-1; x++) {
			for (int y = 1; y < grid.GetLength(1)-1; y++) {
				if (!hasBlockedNeighbor(x, y) || !gridAt(x,y)) {
					continue;
				}

				GraphPoint gp = new GraphPoint(new Vector2(x, y));
				setupPoint(gp, true);
			}
		}

		goalPoint = new GraphPoint(goalPos);
		setupPoint(goalPoint, true);
	}

	private void setupPoint(GraphPoint gp, bool addToGraph) {
		foreach (GraphPoint other in graph) {
			RaycastHit2D[] hs = new RaycastHit2D[1];
			Vector2 dir = other.pos - gp.pos;
			int hit = Physics2D.Raycast(gp.pos, dir.normalized, cf, hs, dir.magnitude);
			if (hit != 0) {
				continue;
			}

			gp.neighbors.Add(other);
			if (addToGraph) {
				other.neighbors.Add(gp);
			}
		}
		if (addToGraph) {
			graph.Add(gp);
		}
	}

	public Vector2[] CalculatePath(BirdControl me) {
		GraphPoint s = new GraphPoint(me.transform.position);
		setupPoint(s, false);

		Node start = new Node(s, 0, Vector2.Distance(s.pos, goalPoint.pos));

		Vector2 dir = goalPoint.pos - s.pos;
		RaycastHit2D[] hs = new RaycastHit2D[1];
		int hit = Physics2D.Raycast(s.pos, dir.normalized, cf, hs,dir.magnitude);
		if (hit == 0) {
			// Distance does not matter as we have found the goal
			Node done = new Node(s,-1,-1); 
			return getWorldPath(done.path);
		}

		HashSet<Node> open = new HashSet<Node>();
		open.Add(start);
		HashSet<GraphPoint> examined = new HashSet<GraphPoint>();
		examined.Add(s);

		Node final = new Node();
		bool found = false;

		while (open.Count > 0 && !found) {

			float d = Mathf.Infinity;
			Node cur = new Node();
			foreach (Node n in open) {
				if (n.d < d) {
					d = n.d;
					cur = n;
				}
			}

			open.Remove(cur);


			foreach (GraphPoint gp in cur.point.neighbors) {
				if (examined.Contains(gp)) {
					continue;
				}
				float ng = Vector2.Distance(cur.point.pos, gp.pos); 
				float h = Vector2.Distance(gp.pos, goalPoint.pos);
				Node n = new Node(gp, cur.g + ng, h, cur);

				if (gp.pos == goalPoint.pos) {
					final = n;
					found = true;
				}

				open.Add(n);
				examined.Add(gp);

				if (memoized.ContainsKey(gp)) {
					Vector2[] memoPath = memoized[gp].path;
					float td = memoized[gp].dist + Vector2.Distance(memoPath[0],cur.point.pos);
					if (td > cur.d) {
						// The memoized path would not save us time
						continue;
					}

					return getFromMemo(cur,memoized[gp]);
				}
			}
		}

		if (!found) {
			return new Vector2[0];
		}

		addMemo(final.path);

		Vector2[] positions = getWorldPath(final.path);
		return positions;
	}

	private Vector2[] getFromMemo(Node cur, PathDistance memo) {
		Vector2[] mp = memo.path;
		Vector2[] worldPath = new Vector2[cur.path.Count+mp.Length];
		getWorldPath(cur.path,worldPath);
		for (int i = 0; i < mp.Length; i++) {
			worldPath[cur.path.Count+i] = mp[i];
		}
		return worldPath;
	}

	private void addMemo(List<GraphPoint> finalPath){ 
		// We did not hit the memoization cache, so we must fill it
		List<GraphPoint> rev = new List<GraphPoint>(finalPath);
		rev.Reverse();
		List<GraphPoint> toGoal = new List<GraphPoint>();
		// Work backwards from the end, setting the memoization at a GraphPoint to be the path from the GraphPoint to the goal
		float dist = 0;
		foreach (GraphPoint gp in rev) {
			// We must move every item over so this operation is O(n)
			// If we were to use a LinkedList, we would have to copy over anyway O(n), so this is cleaner
			toGoal.Insert(0,gp);
			Vector2[] path = getWorldPath(toGoal);
			if (path.Length >= 2) {
				dist += Vector2.Distance(path[0],path[1]);
			}
			PathDistance pd = new PathDistance();
			pd.dist = dist;
			pd.path = path;
			memoized[gp] = pd;
		}
	}

	private Vector2[] getWorldPath(List<GraphPoint> path, Vector2[] positions = null) {
		positions = positions ?? new Vector2[path.Count];
		int i = 0;
		foreach (GraphPoint gp in path) {
			positions [i] = gp.pos;
			i++;
		}
		return positions;
	}

	private void drawPath(Vector2[] positions, Color c) {
		for (int i = 0; i < positions.Length - 1; i++) {
			Debug.DrawLine(positions [i], positions [i + 1], c);
		}
	}

	private bool hasBlockedNeighbor(int x, int y) {
		for (int xo = -1; xo <= 1; xo++) {
			for (int yo = -1; yo <= 1; yo++) {
				int nx = x + xo;
				int ny = y + yo;
				if (!gridAt(nx, ny)) {
					return true;
				}
			}
		}
		return false;
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
