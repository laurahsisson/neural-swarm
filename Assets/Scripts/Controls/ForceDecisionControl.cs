using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ForceDecisionControl : DecisionControl {
	private static readonly float FLOCK_DISTANCE = 10;
	private static readonly float COHES_CONST = 1;
	private static readonly float ALIGN_CONST = 1;

	private static readonly float REPUL_DISTANCE = 3;
	private static readonly float REPUL_CONST = 3f;
	private static readonly float MAX_REPUL_FORCE = 100;

	private static readonly float OBSTCL_DISTANCE = 3;
	private static readonly float OBSTCL_CONST = 4f;
	private static readonly float MAX_OBTSCL_FORCE = 100;

	private static readonly float REWARD_CONST = 5f;

	private static readonly float BOUNDARY_PERCENT = .2f;
	private static readonly float BOUNDARY_CONST = 4f;
	private static readonly float MAX_BOUNDARY_FORCE = 100;

	private Dictionary<BirdTuple,Vector2> btDistances = new Dictionary<BirdTuple,Vector2>();

	private struct BirdTuple {
		public int b1;
		public int b2;
		public BirdTuple(int bl, int br){
			b1 = Mathf.Min(bl,br);
			b2 = Mathf.Max(bl,br);
		}
	}

	public override void EndGeneration(StatsControl.GenerationStats gs) {
		print(gs.completed + "," + gs.birdCollisions);
	}

	public override Vector2[] MakeDecisions(FlockControl.UnityState us) {
		for (int i = 0; i < us.birds.Length; i++) {
			for (int j = i+1; j < us.birds.Length; j++) {
				BirdControl b1 = us.birds[i];
				BirdControl b2 = us.birds[j];
				btDistances[new BirdTuple(i,j)] = minDelta(b1,b2);
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
		Vector2 force = align + cohes + repul + obstcl + goal;
		if (birdNumber == 0) {
			Debug.DrawRay(me.transform.position,me.Velocity);
		}
		return force.normalized * me.Speed;
	}

	private Vector2 cohesion(BirdControl[] birds, BirdControl me) {
		Vector2 sumPosition = Vector2.zero;
		int count = 0;
		foreach (BirdControl b in birds) {
			if (b.Equals(me) || !b.Moving) {
				continue;
			}
			BirdTuple bt = new BirdTuple(me.Number,b.Number);
			Vector2 delta = getDelta(bt,me.Number);
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
		return force.normalized * COHES_CONST;
	}

	private Vector2 repulsion(BirdControl[] birds, BirdControl me) {
		Vector2 force = Vector2.zero;
		int count = 0;
		foreach (BirdControl b in birds) {
			if (b.Equals(me) || !b.Moving) {
				continue;
			}
			BirdTuple bt = new BirdTuple(me.Number,b.Number);
			Vector2 delta = getDelta(bt,me.Number);
			if (delta.magnitude > REPUL_DISTANCE) {
				continue;
			}
			float distFactor = 0;
			if (delta.magnitude == 0) {
				distFactor = 1 / MAX_REPUL_FORCE;
			} else {
				distFactor = Mathf.Pow(delta.magnitude,2);
			}
			Vector2 newForce = -1*delta.normalized / distFactor;
			force += newForce;
			if (me.Number == 0){
				Debug.DrawRay(me.transform.position,newForce*10, Color.red);
				Debug.DrawRay(b.transform.position,-1*newForce*10, Color.red);
			}
		}
		return force.normalized * REPUL_CONST;
	}

	private Vector2 aligment(BirdControl[] birds, BirdControl me) {
		Vector2 force = Vector2.zero;
		foreach (BirdControl b in birds) {
			if (b.Equals(me) || !b.Moving) {
				continue;
			}
			BirdTuple bt = new BirdTuple(me.Number,b.Number);
			Vector2 delta = getDelta(bt,me.Number);
			if (delta.magnitude > FLOCK_DISTANCE) {
				continue;
			}
			force += b.Velocity.normalized;
		}
		return force.normalized * ALIGN_CONST;
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
				distFactor = 1 / MAX_OBTSCL_FORCE;
			} else {
				distFactor = Mathf.Pow(cd.distance,2);
			}
			force += ((Vector2)me.transform.position - cd.pointB).normalized / distFactor;
		}
		return force.normalized * OBSTCL_CONST;
	}

	private Vector2 reward(GameObject goal, BirdControl me) {
		return (goal.transform.position-me.transform.position).normalized*REWARD_CONST;
	}

	private Vector2 minDelta(BirdControl b1, BirdControl b2) {	
		Vector2 b1FuturePos = (Vector2)b1.transform.position+b1.Velocity*Time.deltaTime;
		Vector2 b2FuturePos = (Vector2)b2.transform.position+b2.Velocity*Time.deltaTime;

		Vector2 current = (Vector2)(b2.transform.position-b1.transform.position);
		Vector2 b1Future = ((Vector2)b2.transform.position-b1FuturePos);
		Vector2 b2Future = (b2FuturePos-(Vector2)b1.transform.position);
		Vector2 future = (b2FuturePos-b1FuturePos);
		Vector2[] ds = new Vector2[]{current,b1Future,b2Future,future};

		float minDist = Mathf.Infinity;
		Vector2 minD = Vector2.zero;
		for (int i = 0; i < ds.Length; i++) {
			float mag = ds[i].magnitude;
			if (mag<minDist) {
				minDist = mag;
				minD = ds[i];
			}
		}
		return minD;
	}


	private Vector2 getDelta(BirdTuple bt, int number) {
		Vector2 delta = btDistances[bt];
		if (number == bt.b1) {
			return delta;
		} else {
			return -1*delta;
		}
	}

}

