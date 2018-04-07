using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ForceDecisionControl : DecisionControl {
	private static readonly float FLOCK_DISTANCE = 10;
	private static readonly float COHESION_FORCE = 1; // The maximum force all cohesion can apply
	private static readonly float ALIGN_FORCE = 1.5F;

	private static readonly float REPULSE_DISTANCE = 3;
	private static readonly float REPULSE_FORCE = 3f;
	private static readonly float REPULSE_CONST = 100; // The constant times each individual birds exertion
	private static readonly float REPULSE_ASYMPTOTE = 200; // The maximum force an individual bird may exert

	private static readonly float OBSTACLE_DISTANCE = 3;
	private static readonly float OBSTACLE_FORCE = 4f;
	private static readonly float OBSTACLE_CONST = 100; 
	private static readonly float OBSTACLE_ASYMPTOTE = 200; 

	private static readonly float REWARD_DISTANCE = 40f;
	private static readonly float REWARD_FORCE = 10f;
	private static readonly float REWARD_CONST = 100; 

	private static readonly float BOUNDARY_DISTANCE = 15f;
	private static readonly float BOUNDARY_FORCE = 6; 
	private static readonly float BOUNDARY_CONST = 50; 


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
		return Vector2.ClampMagnitude(force, COHESION_FORCE);
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
			if (delta.magnitude > REPULSE_DISTANCE) {
				continue;
			}
			force += individualForce(delta,REPULSE_CONST,REPULSE_ASYMPTOTE);
		}
		return Vector2.ClampMagnitude(force, REPULSE_FORCE);
	}


	private Vector2 obstacle(GameObject[] walls, BirdControl me) {
		Vector2 force = Vector2.zero;
		int count = 0;
		foreach (GameObject wall in walls) {
			ColliderDistance2D cd = me.GetComponent<Collider2D>().Distance(wall.GetComponent<Collider2D>());
			if (cd.distance > OBSTACLE_DISTANCE) {
				continue;
			}
			Vector2 delta = cd.pointA - cd.pointB;
			force += individualForce(delta,OBSTACLE_CONST,OBSTACLE_ASYMPTOTE);
		}
		return Vector2.ClampMagnitude(force, OBSTACLE_FORCE);
	}

	private Vector2 reward(GameObject goal, BirdControl me) {
		Vector2 delta = (goal.transform.position - me.transform.position);
		float dist = delta.magnitude;
		if (dist>REWARD_DISTANCE) {
			return Vector2.zero;
		}
		// As there is only one reward, force does not have an asymptote, and indvidual force will clamp for us
		return individualForce(delta,REWARD_CONST,REWARD_FORCE);
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
		Vector2 norm = delta/dist;
		float force = 0;
		if (dist == 0) {
			force = asymptote;
		} else {
			force = (dist*dist)*constant;
			force = Mathf.Min(force,asymptote);
		}
		return norm*force;
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

