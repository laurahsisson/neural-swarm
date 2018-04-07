using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ForceDecisionControl : DecisionControl {
	

	private static readonly float FLOCK_DISTANCE = 10;
	private static readonly float COHES_FORCE = 1;
	private static readonly float ALIGN_FORCE = 1;

	private static readonly float REPUL_DISTANCE = 3;
	private static readonly float REPUL_CONST = 3f;
	private static readonly float REPUL_MAX_FORCE = 100; // Max force an individual bird may exert

	private static readonly float OBSTCL_DISTANCE = 3;
	private static readonly float OBSTCL_CONST = 4f;
	private static readonly float MAX_OBSTCL_FORCE = 100; // As above

	private static readonly float REWARD_DISTANCE = 40f;
	private static readonly float REWARD_CONST = 100f;
	private static readonly float REWARD_MAX_FORCE = 10f;

	private static readonly float BOUNDARY_PERCENT = .05f;
	private static readonly float BOUNDARY_CONST = 100f;
	private static readonly float BOUNDARY_MAX_FORCE = 10; // As above

	private Dictionary<BirdTuple,Vector2> btDistances = new Dictionary<BirdTuple,Vector2>();

	private struct BirdTuple {
		public int b1;
		public int b2;

		public BirdTuple(int bl, int br) {
			b1 = Mathf.Min(bl, br);
			b2 = Mathf.Max(bl, br);
		}
	}

	public override void EndGeneration(StatsControl.GenerationStats gs) {
		print(gs.completed + "," + gs.birdCollisions);
	}

	public override Vector2[] MakeDecisions(FlockControl.UnityState us) {
		for (int i = 0; i < us.birds.Length; i++) {
			for (int j = i + 1; j < us.birds.Length; j++) {
				BirdControl b1 = us.birds [i];
				BirdControl b2 = us.birds [j];
				btDistances [new BirdTuple(i, j)] = minDelta(b1, b2);
			}
		}
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
		Vector2 goal = reward(us.goal, me);
		Vector2 bndry = boundary(us, me);
		Vector2 force = align + cohes + repul + obstcl + goal + bndry;

		// We want to steer our current velocity towards our aim velocity, so take the average of the two and reflect it over the goal
		// That way we aim in a way that slows us down only in the desired dimension, and speeds us up in the correct dimension.

		Vector2 vel = me.Velocity.normalized;
		Vector2 aim = force.normalized;
		Vector2 ave = ((vel+aim)/2).normalized;
		Vector2 adjustment = Vector2.zero;
		// If our aim velocity is close to orthogonal to our current velocity, just steer using our current velocity
		if (Vector2.Dot(vel,aim) > -.5f && vel != Vector2.zero) {
			adjustment = Vector2.Reflect(-1*ave,-1*aim).normalized;
		} else {
			adjustment = aim;
		}
		return adjustment.normalized * me.Speed;
	}

	private Vector2 cohesion(BirdControl[] birds, BirdControl me) {
		Vector2 sumPosition = Vector2.zero;
		int count = 0;
		foreach (BirdControl b in birds) {
			if (b.Equals(me) || !b.Moving) {
				continue;
			}
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
		return Vector2.ClampMagnitude(force, COHES_FORCE);
	}
		
	private Vector2 aligment(BirdControl[] birds, BirdControl me) {
		Vector2 force = Vector2.zero;
		foreach (BirdControl b in birds) {
			if (b.Equals(me) || !b.Moving) {
				continue;
			}
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
		int count = 0;
		foreach (BirdControl b in birds) {
			if (b.Equals(me) || !b.Moving) {
				continue;
			}
			BirdTuple bt = new BirdTuple(me.Number, b.Number);
			Vector2 delta = -1 * getDelta(bt, me.Number); // getDelta points to the other birds, so reverse it
			if (delta.magnitude > REPUL_DISTANCE) {
				continue;
			}
			float distFactor = 0;
			if (delta.magnitude == 0) {
				distFactor = 1 / REPUL_MAX_FORCE;
			} else {
				distFactor = Mathf.Pow(delta.magnitude, 2);
			}
			Vector2 newForce = delta.normalized / distFactor;
			force += newForce;
		}
		return Vector2.ClampMagnitude(force, REPUL_CONST);
	}


	private Vector2 obstacle(GameObject[] walls, BirdControl me) {
		Vector2 force = Vector2.zero;
		int count = 0;
		foreach (GameObject wall in walls) {
			ColliderDistance2D cd = me.GetComponent<Collider2D>().Distance(wall.GetComponent<Collider2D>());
			if (cd.distance > OBSTCL_DISTANCE) {
				continue;
			}
			float distFactor = 0;
			if (cd.distance == 0) {
				distFactor = 1 / MAX_OBSTCL_FORCE;
			} else {
				distFactor = Mathf.Pow(cd.distance, 2);
			}
			force += ((Vector2)cd.pointA - cd.pointB).normalized / distFactor;
		}
		return Vector2.ClampMagnitude(force, OBSTCL_CONST);
	}

	private Vector2 reward(GameObject goal, BirdControl me) {
		Vector2 delta = (goal.transform.position - me.transform.position);
		float dist = delta.magnitude;
		if (dist>REWARD_DISTANCE) {
			return Vector2.zero;
		}

		float distFactor = 0;
		if (delta.magnitude == 0) {
			distFactor = 1 / REWARD_MAX_FORCE;
		} else {
			distFactor = Mathf.Pow(delta.magnitude, 2);
		}
		Vector2 force = delta.normalized / distFactor;
		return Vector2.ClampMagnitude(force, REWARD_CONST);
	}

	private Vector2 boundary(FlockControl.UnityState us, BirdControl me) {
		float xForce = 0;
		float yForce = 0;
		if (me.transform.position.x < us.roomWidth * BOUNDARY_PERCENT) {
			float xDelta = me.transform.position.x - 0;
			xForce = BOUNDARY_CONST / (xDelta * xDelta);
			xForce = Mathf.Clamp(xForce, -BOUNDARY_MAX_FORCE, BOUNDARY_MAX_FORCE);
		}

		if (me.transform.position.y < us.roomHeight * BOUNDARY_PERCENT) {
			float yDelta = me.transform.position.y - 0;
			yForce = BOUNDARY_CONST / (yDelta * yDelta);
			yForce = Mathf.Clamp(yForce, -BOUNDARY_MAX_FORCE, BOUNDARY_MAX_FORCE);
		}

		if (me.transform.position.x > us.roomWidth * (1 - BOUNDARY_PERCENT)) {
			float xDelta = me.transform.position.x - us.roomWidth;
			xForce = -1 * BOUNDARY_CONST / (xDelta * xDelta);
			xForce = Mathf.Clamp(xForce, -BOUNDARY_MAX_FORCE, BOUNDARY_MAX_FORCE);
		}

		if (me.transform.position.y > us.roomHeight * (1 - BOUNDARY_PERCENT)) {
			float yDelta = me.transform.position.y - us.roomHeight;
			yForce = -1 * BOUNDARY_CONST / (yDelta * yDelta);
			yForce = Mathf.Clamp(yForce, -BOUNDARY_MAX_FORCE, BOUNDARY_MAX_FORCE);
		}

		Vector2 force = new Vector2(xForce, yForce);
		force = Vector2.ClampMagnitude(force, BOUNDARY_MAX_FORCE);
		return force;
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


	private Vector2 getDelta(BirdTuple bt, int number) {
		Vector2 delta = btDistances [bt];
		if (number == bt.b1) {
			return delta;
		} else {
			return -1 * delta;
		}
	}

}

