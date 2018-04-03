using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ForceDecisionControl : DecisionControl {

	private static float COHESION_MASS_EXPONENT = 1;
	private static float COHESION_DISTANCE_EXPONENT = 2;
	private static float COHESION_CONSTANT = 1;
	private static float COHESION_CUTOFF = 10;

	private static float REPULSION_MASS_EXPONENT = 1;
	private static float REPULSION_DISTANCE_EXPONENT = 1;
	private static float REPULSION_CONSTANT = 1;
	private static float REPULSION_CUTOFF = 2;

	// Actually the exponent for the bird's mass as the walls have infinite mass
	private static float OBSTACLE_MASS_EXPONENT = 1;
	private static float OBSTACLE_DISTANCE_EXPONENT = 2;
	private static float OBSTACLE_CONSTANT = 3;
	private static float OBSTACLE_CUTOFF = 3;

	// As above, the exponent for the bird's mass in the attraction to the goal
	private static float REWARD_MASS_EXPONENT = -.025f;
	private static float REWARD_DISTANCE_EXPONENT = 1;
	private static float REWARD_CONSTANT = 30;
	private static float REWARD_CUTOFF = 40;

	private static float ALIGNMENT_MASS_EXPONENT = 1;
	private static float ALIGNMENT_SPEED_EXPONENT = 1;
	private static float ALIGNMENT_DISTANCE_EXPONENT = 1;
	private static float ALIGNMENT_CONSTANT = 3;
	private static float ALIGNMENT_CUTOFF = 5;

	public override Vector2[] MakeDecisions(FlockControl.UnityState us) {
		Vector2[] forces = new Vector2[us.birds.Length];
		for (int i = 0; i < forces.Length; i++) {
			forces [i] = getForce(us, i);
		}
		return forces;
	}

	private Vector2 getForce(FlockControl.UnityState us, int birdNumber) {
		Vector2 force = Vector2.zero;

		BirdControl me = us.birds[birdNumber];
		force += aligment(us.birds, me);
		force += cohesion(us.birds, me);
		force += repulsion(us.birds, me);
		force += obstacle(us.walls, me);
		force += reward(us.goal, me);
		force = force.normalized * me.Speed;
		return force;
	}

	private Vector2 repulsion(BirdControl[] birds, BirdControl me) {
		Vector2 force = Vector2.zero;
		foreach (BirdControl b in birds) {
			Vector2 delta = (Vector2)(me.transform.position - b.transform.position);
			float dist = delta.magnitude;
			if (dist > REPULSION_CUTOFF) {
				continue;
			}
			force += (Vector2)(me.transform.position - b.transform.position).normalized;
		}
		return force.normalized;
	}

	private Vector2 cohesion(BirdControl[] birds, BirdControl me) {
		int count = 0;
		Vector2 sumPosition = Vector2.zero;
		foreach (BirdControl b in birds) {
			Vector2 delta = (Vector2)(me.transform.position - b.transform.position);
			float dist = delta.magnitude;
			if (dist > COHESION_CUTOFF) {
				continue;
			}
			sumPosition += (Vector2)b.transform.position;
			count++;
		}
		if (count == 0) {
			return Vector2.zero;
		}
		Vector2 center = sumPosition / count;
		Vector2 force = (center - (Vector2)me.transform.position);
		return force.normalized;
	}

	private Vector2 aligment(BirdControl[] birds, BirdControl me) {
		Vector2 force = Vector2.zero;
		foreach (BirdControl b in birds) {
			Vector2 delta = (Vector2)(me.transform.position - b.transform.position);
			float dist = delta.magnitude;
			if (dist > ALIGNMENT_CUTOFF) {
				continue;
			}
			force += b.Velocity;
		}
		return force.normalized;
	}

	private Vector2 obstacle(GameObject[] walls, BirdControl me) {
		Vector2 force = Vector2.zero;
		foreach (GameObject w in walls) {
			ColliderDistance2D cd = me.gameObject.GetComponent<Collider2D>().Distance(w.GetComponent<Collider2D>());
			float dist = cd.distance;
			if (dist > OBSTACLE_CUTOFF) {
				continue;
			}
			force += ((Vector2)me.transform.position - cd.pointB).normalized;
		}
		return force.normalized;
	}

	private Vector2 reward(GameObject goal, BirdControl me) {
		Vector2 delta = (Vector2)(goal.transform.position - me.transform.position);
		float dist = delta.magnitude;
		if (dist > REWARD_CUTOFF) {
			return Vector2.zero;
		}
		return delta.normalized;
	}
}
