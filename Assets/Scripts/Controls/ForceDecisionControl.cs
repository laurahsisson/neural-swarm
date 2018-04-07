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

	public override Vector2[] MakeDecisions(FlockControl.UnityState us) {
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
		return force.normalized * me.Speed;
	}

	private Vector2 cohesion(BirdControl[] birds, BirdControl me) {
		Vector2 sumPosition = Vector2.zero;
		int count = 0;
		foreach (BirdControl b in birds) {
			if (b.Equals(me)) {
				continue;
			}
			ColliderDistance2D cd = me.GetComponent<Collider2D>().Distance(b.GetComponent<Collider2D>());
			if (cd.distance > FLOCK_DISTANCE) {
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
			if (b.Equals(me)) {
				continue;
			}
			ColliderDistance2D cd = me.GetComponent<Collider2D>().Distance(b.GetComponent<Collider2D>());
			if (cd.distance > REPUL_DISTANCE) {
				continue;
			}
			float distFactor = 0;
			if (cd.distance == 0) {
				distFactor = 1 / MAX_REPUL_FORCE;
			} else {
				distFactor = Mathf.Pow(cd.distance,2);
			}
			force += ((Vector2)me.transform.position - cd.pointB).normalized / distFactor;
		}
		return force.normalized * REPUL_CONST;
	}

	private Vector2 aligment(BirdControl[] birds, BirdControl me) {
		Vector2 force = Vector2.zero;
		foreach (BirdControl b in birds) {
			if (b.Equals(me)) {
				continue;
			}
			ColliderDistance2D cd = me.GetComponent<Collider2D>().Distance(b.GetComponent<Collider2D>());
			if (cd.distance > FLOCK_DISTANCE) {
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

}

