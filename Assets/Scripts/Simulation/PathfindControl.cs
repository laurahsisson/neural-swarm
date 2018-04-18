using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class PathfindControl : MonoBehaviour {
	private readonly float GRID_STEP = 1f;
	private readonly int NUM_COLLIDERS = 10; // The max number of colliders we will check against
	private readonly float CUTOFF_DIST = 1.5f; // Overestimation of sqrt(2). If we are within one unit square of the goal we are good to stop

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
			this.d = g+h;
			path = new List<Vector2>();
			path.Add(pos);
		}

		public Node(Vector2 pos, float g, float h, Node parent) : this(pos,g,h) {
			path = new List<Vector2>(parent.path);
			path.Add(pos);
		}
	}

	public void InitializeGrid(FlockControl.UnityState us) {
		CircleCollider2D cd = GetComponent<CircleCollider2D>();
		gameObject.transform.localScale=new Vector3(us.maxSize*2,us.maxSize*2);
		ContactFilter2D cf = new ContactFilter2D();
		cf.useTriggers=true;
		cf.layerMask = LayerMask.GetMask("Wall");
		cf.useLayerMask = true;
		Collider2D[] others = new Collider2D[NUM_COLLIDERS];
		grid = new bool[(int)(us.roomWidth/GRID_STEP),(int)(us.roomHeight/GRID_STEP)];
			
		for (float x = 0; x < us.roomWidth; x+=GRID_STEP) {
			for (float y = 0; y < us.roomHeight; y+=GRID_STEP) {
				transform.position = new Vector2(x,y);
				int hit = cd.OverlapCollider(cf,others);
				int gx = (int)(x/GRID_STEP);
				int gy = (int)(y/GRID_STEP);
				if (gx == grid.GetLength(0) || gy == grid.GetLength(1)) {
					continue;
				}
				try {
					grid[gx,gy] = hit==0;
				} catch (System.Exception ex) {
					Debug.Log(gx + "<" + gy);
					Debug.Log(grid.GetLength(0) + "<" + grid.GetLength(1));
				}
			}
		}
	}

	public Vector2[] CalculatePath(FlockControl.UnityState us, BirdControl me) {
		Vector2 myGridPos = me.transform.position*GRID_STEP;
		Vector2 goalGridPos = us.goal.transform.position*GRID_STEP;
		Node start = new Node(myGridPos,0,Vector2.Distance(myGridPos,goalGridPos));

		HashSet<Node> open = new HashSet<Node>();
		open.Add(start);
		HashSet<Vector2> examined = new HashSet<Vector2>();
		examined.Add(myGridPos);

		Node final = new Node();
		bool found = false;

		while (open.Count > 0) {
			float d = Mathf.Infinity;
			Node cur = new Node();
			foreach (Node n in open) {
				if (n.d < d) {
					d = n.d;
					cur = n;
				}
			}
			if (d > Mathf.Sqrt(us.roomWidth*us.roomWidth+us.roomHeight+us.roomHeight)){
				// If we traverse more than the entire room in length, it is a good indicator we will be unable to find a path
				print("Maxed out on distance");
				break;
			}

			open.Remove(cur);
			if (Vector2.Distance(cur.gridPos,goalGridPos)<CUTOFF_DIST) {
				cur.path.Add(goalGridPos);
				found = true;
				final = cur;
				break;
			}

			List<Vector2> ns = neighbors(cur,examined);
			foreach (Vector2 p in ns) {
				float ng = Vector2.Distance(cur.gridPos,p); 
				float h = Vector2.Distance(p,goalGridPos);
				Node n = new Node(p,cur.g+ng,h,cur);
				open.Add(n);
			}
		}

		if (!found) {
			Debug.Log("Could not find path to goal!");
			return new Vector2[0];
		}

		Vector2[] positions = new Vector2[final.path.Count];
		int i = 0;
		foreach (Vector2 p in final.path) {
			positions[i]=p/GRID_STEP;
			i++;
		}
		for (i = 0; i < positions.Length - 1; i++) {
			Debug.DrawLine(positions[i],positions[i+1],Color.red);
		}

		return positions;
	}

	private List<Vector2> neighbors(Node next, HashSet<Vector2> examined) {
		List<Vector2> ns = new List<Vector2>(8); // At most 8 neighbors
		for (int xo = -1; xo <= 1; xo++) {
			for (int yo = -1; yo <= 1; yo++) {
				int x = (int)next.gridPos.x+xo;
				int y = (int)next.gridPos.y+yo;

				Vector2 pos = new Vector2(x,y);
				if (examined.Contains(pos)) {
					continue;
				}
				examined.Add(pos);

				if (x<0 || x>= grid.GetLength(0)) {
					continue;
				}
				if (y<0 || y>= grid.GetLength(1)) {
					continue;
				}

				if (grid[x,y]) {
					ns.Add(pos);
				}
			}
		}
		return ns;
	}

}
