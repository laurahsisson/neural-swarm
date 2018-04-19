using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ForceDecisionControl : DecisionControl {
	public PathfindControl pf;

	private static readonly float FLOCK_DISTANCE = 10;
	private static readonly float COHESION_FORCE = 1;
	// The maximum sum force of cohesion
	private static readonly float ALIGN_FORCE = 1.5F;

	private static readonly float REPULSE_DISTANCE = 3;

	private static readonly float REPULSE_FORCE = 3f;

	private static readonly float REPULSE_CONST = 100;
	// The constant times each individual birds exertion
	// The higher const is the more equally weighted near and far objects will be

	private static readonly float REPULSE_ASYMPTOTE = 200;
	// The maximum force an individual bird may exert
	// The higher the asymptote is the more strongly weighted very close objects are (distance of less than 1)

	private static readonly float OBSTACLE_DISTANCE = 3;
	private static readonly float OBSTACLE_FORCE = 4f;
	private static readonly float OBSTACLE_CONST = 100;
	private static readonly float OBSTACLE_ASYMPTOTE = 200;

	// The percentage of birds who should use pathfinding
	private static readonly float REWARD_PERCENT = .30f;
	// The probability that we will receive a token regardless of our situation if we received one last frame
	private static readonly float REWARD_CARRYOVER = .85f;

	private static readonly float REWARD_DISTANCE = 15;
	private static readonly float REWARD_FORCE = 8f;
	private static readonly float REWARD_CONST = 100;
	private static readonly float REWARD_ASYMPTOTE = 200;

	private static readonly float BOUNDARY_DISTANCE = 15f;
	private static readonly float BOUNDARY_FORCE = 6;
	private static readonly float BOUNDARY_CONST = 50;

	private static readonly float VIEW_ANGLE = 45f;
	// The angle within which we check to see if anyone is leading

	private Dictionary<BirdTuple,Vector2> btDistances = new Dictionary<BirdTuple,Vector2>();
	private List<BirdControl>[] nearbyBirds;
	private int[] numPathFrames;

	private Vector2[] rewardForces;
	private bool[] gotRewardToken;


	private struct BirdTuple {
		public int b1;
		public int b2;

		public BirdTuple(int bl, int br) {
			b1 = Mathf.Min(bl, br);
			b2 = Mathf.Max(bl, br);
		}
	}

	public override void StartGeneration(FlockControl.UnityState us) {
		pf.InitializeGrid(us);
		numPathFrames = new int[us.birds.Length];
		gotRewardToken = new bool[us.birds.Length];
	}

	public override void EndGeneration(StatsControl.GenerationStats gs) {
		print(gs.completed + "," + gs.birdCollisions);
	}

	public override Vector2[] MakeDecisions(FlockControl.UnityState us) {
		nearbyBirds = new List<BirdControl>[us.birds.Length];
		for (int i = 0; i < us.birds.Length; i++) {
			nearbyBirds [i] = new List<BirdControl>();
		}

		float maxRange = Mathf.Max(REPULSE_DISTANCE, FLOCK_DISTANCE);
		for (int i = 0; i < us.birds.Length; i++) {
			BirdControl b1 = us.birds [i];
			if (!b1.Moving) {
				continue;
			}

			for (int j = i + 1; j < us.birds.Length; j++) {
				BirdControl b2 = us.birds [j];
				if (!b2.Moving) {
					continue;
				}

				Vector2 d = minDelta(b1, b2);
				btDistances [new BirdTuple(i, j)] = d;
		
				if (d.magnitude > maxRange) {
					continue;
				}
				nearbyBirds [i].Add(b2);
				nearbyBirds [j].Add(b1);
			}
		}


		generateRewards(us);

		Vector2[] forces = new Vector2[us.birds.Length];
		for (int i = 0; i < forces.Length; i++) {
			forces [i] = getForce(us, i);
		}
		return forces;
	}

	private Vector2 getForce(FlockControl.UnityState us, int birdNumber) {
		BirdControl me = us.birds [birdNumber];

		Vector2 align = aligment(us.birds, me);
		Vector2 cohes = cohesion(us.birds, me);
		Vector2 repul = repulsion(us.birds, me);
		Vector2 obstcl = obstacle(us.walls, me);
		Vector2 goal = rewardForces[me.Number];//reward(us.goal, me);
		Vector2 bndry = boundary(us, me);
		Vector2 force = align + cohes + repul + obstcl + goal + bndry;

		// We want to steer our current velocity towards our aim velocity, so take the average of the two and reflect it over the goal
		// That way we aim in a way that slows us down only in the desired dimension, and speeds us up in the correct dimension.

		Vector2 vel = me.Velocity.normalized;
		Vector2 aim = force.normalized;
		Debug.DrawRay(me.transform.position,aim*10,Color.green);

		Vector2 ave = ((vel + aim) / 2).normalized;
		Vector2 adjustment = Vector2.zero;
		// If our aim velocity is close to orthogonal to our current velocity, just steer using our current velocity
		if (Vector2.Dot(vel, aim) > -.5f && vel != Vector2.zero) {
			adjustment = Vector2.Reflect(-1 * ave, -1 * aim).normalized;
		} else {
			adjustment = aim;
		}
		return adjustment.normalized * me.Speed;
	}

	private void generateRewards(FlockControl.UnityState us) {
		int pathfindTokens = (int)(us.birds.Length*REWARD_PERCENT);
		rewardForces = new Vector2[us.birds.Length];
		bool[] nextGotToken = new bool[us.birds.Length];
		/* Let n be the total number of birds. Our goal in this function to get REWARD_PERCENT of total birds using pathfinding. 
		 * We generate a number of tokes equal to REWARD_PERCENT * n.
		 * We prioritize the birds that previously received tokens, then birds with no leaders, but want to make sure that as the number of active birds falls,
		 * we still give away all of our tokens to birds that are active. If we have more tokens than active birds, every bird should use pathfinding.
		 */

		// First we give priority to birds that are already holding tokens
		for (int bird = 0; bird < us.birds.Length; bird++) {
			BirdControl me = us.birds[bird];
			if (!gotRewardToken[bird] || !me.Moving) {
				continue;
			}
			if (Random.value>REWARD_CARRYOVER) {
				continue;
			}
			// This bird had a token and carries it over in this frame
			pathfindTokens = giveToken(us,bird,pathfindTokens,nextGotToken);
		}

		// Then we prioritize birds with no leaders
		int[] birdIndex = range(us.birds.Length);
		shuffle(birdIndex);
		for (int i = 0; i < birdIndex.Length && pathfindTokens>0; i++) {
			int bird = birdIndex[i];
			BirdControl me = us.birds[bird];
			if (!me.Moving||nextGotToken[bird]) {
				continue;
			}

			bool hasLeader = false;
			foreach (BirdControl b in nearbyBirds[me.Number]) {
				hasLeader = hasLeader || inView(b, me);
			}

			if (hasLeader) {
				continue;
			}
			// This bird has no leader so use a token
			pathfindTokens = giveToken(us,bird,pathfindTokens,nextGotToken);
		}

		// Now we give out the rest of the tokens randomly
		birdIndex = range(us.birds.Length);
		shuffle(birdIndex);
		for (int i = 0; i < birdIndex.Length && pathfindTokens>0; i++) {
			int bird = birdIndex[i];
			BirdControl me = us.birds[bird];
			if (!me.Moving||nextGotToken[bird]) {
				continue;
			}
			// This bird has received a token through random chance
			pathfindTokens = giveToken(us,bird,pathfindTokens,nextGotToken);
		}

		// The remainder of the birds do not receive tokens and so just do simple pathfinding towards the goal
		for (int bird = 0; bird < us.birds.Length; bird++) {
			BirdControl me = us.birds[bird];
			if (!me.Moving) {
				continue;
			}
			if (rewardForces[bird]!=Vector2.zero) {
				// This bird already received a token
				continue;
			}

			rewardForces[bird] = rewardSimple(us.goal,me);
		}

		int activeBirds = 0;
		foreach (BirdControl b in us.birds) {
			if (b.Moving) {
				activeBirds++;
			}
		}
		print(pathfindTokens + "," + (int)(us.birds.Length*REWARD_PERCENT) + "," + activeBirds);

		gotRewardToken = nextGotToken;
	}

	private int giveToken(FlockControl.UnityState us, int bird, int tokens, bool[] nextGotToken) {
		nextGotToken[bird]=true;
		BirdControl me = us.birds[bird];
		Vector2[] path = pf.CalculatePath(us.goal.transform.position, me);
		rewardForces[bird] = rewardPathfind(path, me);
		return tokens - 1;
	}

	private int[] range(int len) {
		int[] arr = new int[len];
		for (int i = 0; i < len; i++) {
			arr[i]=i;
		}
		return arr;
	}

	private void shuffle(int[] arr) {
		int count = arr.Length;
		int last = count - 1;
		for (int i = 0; i < last; ++i) {
			int r = Random.Range(i, count);
			int tmp = arr[i];
			arr[i] = arr[r];
			arr[r] = tmp;
		}
	}

	private Vector2 cohesion(BirdControl[] birds, BirdControl me) {
		Vector2 sumPosition = Vector2.zero;
		int count = 0;
		foreach (BirdControl b in nearbyBirds[me.Number]) {
			BirdTuple bt = new BirdTuple(me.Number, b.Number);
			Vector2 delta = getDelta(bt, me.Number);
			if (delta.magnitude > FLOCK_DISTANCE) {
				continue;
			}
			sumPosition += (Vector2)b.transform.position;
			count++;
		}
		if (count == 0) {
			return Vector2.zero;
		}
		Vector2 averagePosition = sumPosition / count;
		Vector2 force = (averagePosition - (Vector2)me.transform.position);
		return Vector2.ClampMagnitude(force, COHESION_FORCE);
	}

	private Vector2 aligment(BirdControl[] birds, BirdControl me) {
		Vector2 force = Vector2.zero;
		foreach (BirdControl b in nearbyBirds[me.Number]) {
			BirdTuple bt = new BirdTuple(me.Number, b.Number);
			Vector2 delta = getDelta(bt, me.Number);
			if (delta.magnitude > FLOCK_DISTANCE) {
				continue;
			}
			force += b.Velocity.normalized;
		}
		return Vector2.ClampMagnitude(force, ALIGN_FORCE);
	}

	private Vector2 repulsion(BirdControl[] birds, BirdControl me) {
		Vector2 force = Vector2.zero;
		foreach (BirdControl b in nearbyBirds[me.Number]) {
			BirdTuple bt = new BirdTuple(me.Number, b.Number);
			Vector2 delta = -1 * getDelta(bt, me.Number); // getDelta points to the other birds, so reverse it
			if (delta.magnitude > REPULSE_DISTANCE) {
				continue;
			}
			force += individualForce(delta, REPULSE_CONST, REPULSE_ASYMPTOTE);
		}
		return Vector2.ClampMagnitude(force, REPULSE_FORCE);
	}


	private Vector2 obstacle(GameObject[] walls, BirdControl me) {
		Vector2 force = Vector2.zero;
		foreach (GameObject wall in walls) {
			ColliderDistance2D cd = me.GetComponent<Collider2D>().Distance(wall.GetComponent<Collider2D>());
			if (cd.distance > OBSTACLE_DISTANCE) {
				continue;
			}
			Vector2 delta = cd.pointA - cd.pointB;
			force += individualForce(delta, OBSTACLE_CONST, OBSTACLE_ASYMPTOTE);
		}
		return Vector2.ClampMagnitude(force, OBSTACLE_FORCE);
	}

	private Vector2 boundary(FlockControl.UnityState us, BirdControl me) {
		float xForce = 0;
		float yForce = 0;
		if (me.transform.position.x <= BOUNDARY_DISTANCE) {
			float xDist = me.transform.position.x - 0;
			xForce = BOUNDARY_CONST / (xDist * xDist);
			xForce = Mathf.Clamp(xForce, -BOUNDARY_FORCE, BOUNDARY_FORCE);
		}

		if (me.transform.position.y <= BOUNDARY_DISTANCE) {
			float yDist = me.transform.position.y - 0;
			yForce = BOUNDARY_CONST / (yDist * yDist);
			yForce = Mathf.Clamp(yForce, -BOUNDARY_FORCE, BOUNDARY_FORCE);
		}

		if (me.transform.position.x >= us.roomWidth - BOUNDARY_DISTANCE) {
			float xDist = me.transform.position.x - us.roomWidth;
			// We lose the sign of the force by squaring the distance so we need to add it back here
			xForce = -1 * BOUNDARY_CONST / (xDist * xDist); 
			xForce = Mathf.Clamp(xForce, -BOUNDARY_FORCE, BOUNDARY_FORCE);
		}

		if (me.transform.position.y >= us.roomHeight - BOUNDARY_DISTANCE) {
			float yDist = me.transform.position.y - us.roomHeight;
			yForce = -1 * BOUNDARY_CONST / (yDist * yDist);
			yForce = Mathf.Clamp(yForce, -BOUNDARY_FORCE, BOUNDARY_FORCE);
		}

		Vector2 force = new Vector2(xForce, yForce);
		force = Vector2.ClampMagnitude(force, BOUNDARY_FORCE);
		return force;
	}

	private Vector2 rewardPathfind(Vector2[] path, BirdControl me) {
		if (path.Length == 0) {
			return Vector2.zero;
		}
		Vector2 delta = (path [1] - (Vector2)me.transform.position);
		float dist = delta.magnitude;
		if (dist > REWARD_DISTANCE) {
			return Vector2.zero;
		}
		// As there is only one reward, force does not have an asymptote, and indvidual force will clamp for us
		return individualForce(delta, REWARD_CONST, REWARD_FORCE);
	}

	private Vector2 rewardSimple(GameObject goal, BirdControl me) {
		Vector2 delta = (Vector2)(goal.transform.position - me.transform.position);
		float dist = delta.magnitude;
		if (dist > REWARD_DISTANCE) {
			return Vector2.zero;
		}
		return individualForce(delta, REWARD_CONST, REWARD_FORCE);
	}




	private Vector2 minDelta(BirdControl b1, BirdControl b2) {
		ColliderDistance2D cd = b1.GetComponent<Collider2D>().Distance(b2.GetComponent<Collider2D>());

		Vector2 b1FuturePos = cd.pointA + b1.Velocity * Time.deltaTime;
		Vector2 b2FuturePos = cd.pointB + b2.Velocity * Time.deltaTime;

		Vector2 current = (cd.pointB - cd.pointA);
		Vector2 b1Future = (cd.pointB - b1FuturePos);
		Vector2 b2Future = (b2FuturePos - cd.pointA);
		Vector2 future = (b2FuturePos - b1FuturePos);
		Vector2[] ds = new Vector2[]{ current, b1Future, b2Future, future };

		float minDist = Mathf.Infinity;

		Vector2 minD = Vector2.zero;
		for (int i = 0; i < ds.Length; i++) {
			float mag = ds [i].magnitude;
			if (mag < minDist) {
				minDist = mag;
				minD = ds [i];
			}
		}
		return minD;
	}

	private Vector2 individualForce(Vector2 delta, float constant, float asymptote) {
		float dist = delta.magnitude;
		Vector2 norm = delta / dist;
		float force = 0;
		if (dist == 0) {
			force = asymptote;
		} else {
			force = (dist * dist) * constant;
			force = Mathf.Min(force, asymptote);
		}
		return norm * force;
	}

	private bool inView(BirdControl other, BirdControl me) {	
		float dir = Mathf.Rad2Deg * Mathf.Atan2(me.Velocity.y, me.Velocity.x);
		Vector3 delta = other.transform.position - me.transform.position;
		float angleTo = Mathf.Rad2Deg * Mathf.Atan2(delta.y, delta.x);
		return withinAngle(dir, angleTo, VIEW_ANGLE);
	}

	static bool withinAngle(float a, float b, float dist) {
		return (360 - Mathf.Abs(a - b) % 360 < dist || Mathf.Abs(a - b) % 360 < dist);
	}

	private Vector2 getDelta(BirdTuple bt, int number) {
		Vector2 delta = btDistances [bt];
		if (number == bt.b1) {
			return delta;
		} else {
			return -1 * delta;
		}
	}

}

