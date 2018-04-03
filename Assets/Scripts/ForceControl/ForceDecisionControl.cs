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

	private static float OBSTACLE_MASS_EXPONENT = 1;
	private static float OBSTACLE_DISTANCE_EXPONENT = 2;
	private static float OBSTACLE_CONSTANT = 3;
	private static float OBSTACLE_CUTOFF = 3;

	private static float REWARD_MASS_EXPONENT = -.025f;
	private static float REWARD_DISTANCE_EXPONENT = 1;
	private static float REWARD_CONSTANT = 30;
	private static float REWARD_CUTOFF = 40;

	private static float ALIGNMENT_MASS_EXPONENT = 1;
	private static float ALIGNMENT_SPEED_EXPONENT = 1;
	private static float ALIGNMENT_DISTANCE_EXPONENT = 1;
	private static float ALIGNMENT_CONSTANT = 3;
	private static float ALIGNMENT_CUTOFF = 5;

	private Dictionary<ForceDNA.Factor, float> factors;

	public override void InitializeModel() {
		factors = ForceDNA.GenerateFactorDict();
	}

	public override Vector2[] MakeDecisions(FlockControl.UnityState us) {
		Vector2[] forces = new Vector2[us.birds.Length];
		for (int i = 0; i < forces.Length; i++) {
			forces [i] = getForce(us, i);
		}
		return forces;
	}

	private Vector2 getForce(FlockControl.UnityState us, int birdNumber) {
		Vector2 force = Vector2.zero;
		BirdControl me = us.birds [birdNumber];
		force += aligment(us.birds, me);
		force += cohesion(us.birds, me);
		force += repulsion(us.birds, me);
		force += obstacle(us.walls, me);
		force += reward(us.goal, me);
		force = force.normalized * me.Speed;
		print(force);
		return force;
	}

	private Vector2 cohesion(BirdControl[] birds, BirdControl me) {
		Vector2 force = Vector2.zero;
		foreach (BirdControl b in birds) {
			Vector2 delta = (Vector2)(b.transform.position - me.transform.position);
			float dist = delta.magnitude;
			if (dist > factors [ForceDNA.Factor.CohesCutoff]) {
				continue;
			}
			Vector2 norm = delta / dist;
			float massFactor = Mathf.Pow(b.Mass, factors [ForceDNA.Factor.CohesMassExp]);
			float distFactor = Mathf.Pow(dist, factors [ForceDNA.Factor.CohesDistExp]);
			force += norm * massFactor * distFactor;
		}
		float myMassFactor = Mathf.Pow(me.Mass, factors [ForceDNA.Factor.CohesMassExp]);
		force *= factors [ForceDNA.Factor.CohesConst] * myMassFactor;
		return force;
	}

	private Vector2 repulsion(BirdControl[] birds, BirdControl me) {
		Vector2 force = Vector2.zero;
		foreach (BirdControl b in birds) {
			Vector2 delta = (Vector2)(b.transform.position - me.transform.position);
			float dist = delta.magnitude;
			if (dist > factors [ForceDNA.Factor.RepulsCutoff]) {
				continue;
			}
			Vector2 norm = delta / dist;
			float massFactor = Mathf.Pow(b.Mass, factors [ForceDNA.Factor.RepulsMassExp]);
			float distFactor = Mathf.Pow(dist, factors [ForceDNA.Factor.RepulsDistExp]);
			force += norm * massFactor * distFactor;
		}
		float myMassFactor = Mathf.Pow(me.Mass, factors [ForceDNA.Factor.RepulsMassExp]);
		force *= factors [ForceDNA.Factor.RepulsConst] * myMassFactor;
		return force;
	}

	private Vector2 aligment(BirdControl[] birds, BirdControl me) {
		Vector2 force = Vector2.zero;
		foreach (BirdControl b in birds) {
			Vector2 delta = (Vector2)(b.transform.position - me.transform.position);
			float dist = delta.magnitude;
			if (dist > factors [ForceDNA.Factor.AlignCutoff]) {
				continue;
			}
			float speed = b.Velocity.magnitude;
			Vector2 norm = b.Velocity / speed;
			float massFactor = Mathf.Pow(b.Mass, factors [ForceDNA.Factor.AlignMassExp]);
			float distFactor = Mathf.Pow(dist, factors [ForceDNA.Factor.AlignDistExp]);
			float speedFactor = Mathf.Pow(speed, factors [ForceDNA.Factor.AlignSpeedExp]);
			force += norm * massFactor * distFactor * speedFactor;
		}
		float myMassFactor = Mathf.Pow(me.Mass, factors [ForceDNA.Factor.AlignMassExp]);
		force *= factors [ForceDNA.Factor.AlignConst] * myMassFactor;
		return force;
	}

	private Vector2 obstacle(GameObject[] walls, BirdControl me) {
		Vector2 force = Vector2.zero;
		foreach (GameObject w in walls) {
			ColliderDistance2D cd = me.gameObject.GetComponent<Collider2D>().Distance(w.GetComponent<Collider2D>());
			Vector2 delta = (cd.pointB - (Vector2)me.transform.position);
			float dist = delta.magnitude;
			if (dist > factors [ForceDNA.Factor.ObstclCutoff]) {
				continue;
			}
			Vector2 norm = delta / dist;
			float distFactor = Mathf.Pow(dist, factors [ForceDNA.Factor.ObstclDistExp]);
			force += norm * distFactor;
		}
		float myMassFactor = Mathf.Pow(me.Mass, factors [ForceDNA.Factor.ObstclMassExp]);
		force *= factors [ForceDNA.Factor.ObstclConst] * myMassFactor;
		return force;
	}

	private Vector2 reward(GameObject goal, BirdControl me) {
		Vector2 delta = (Vector2)(goal.transform.position - me.transform.position);
		float dist = delta.magnitude;
		if (dist > factors [ForceDNA.Factor.RewardCutoff]) {
			return Vector2.zero;
		}
		Vector2 norm = delta / dist;
		float distFactor = Mathf.Pow(dist, factors [ForceDNA.Factor.RewardDistExp]);
		float myMassFactor = Mathf.Pow(me.Mass, factors [ForceDNA.Factor.RewardMassExp]);
		return norm * distFactor * myMassFactor * factors [ForceDNA.Factor.RewardConst];
	}

	private class ForceDNA {
		public enum Factor {
			CohesMassExp,
			CohesDistExp,
			CohesConst,
			CohesCutoff,

			RepulsMassExp,
			RepulsDistExp,
			RepulsForceExp,
			RepulsConst,
			RepulsCutoff,

			// Actually the exponent for the bird's mass as the goal has infinite mass
			RewardMassExp,
			RewardDistExp,
			RewardConst,
			RewardCutoff,

			// As above, the exponent for the bird's mass in the attraction to the obstacles
			ObstclMassExp,
			ObstclDistExp,
			ObstclConst,
			ObstclCutoff,

			AlignMassExp,
			AlignDistExp,
			AlignSpeedExp,
			AlignConst,
			AlignCutoff
		}

		public static Dictionary<Factor, float> FactorListToDict(float[] fl) {
			Dictionary<Factor, float> fd = new Dictionary<Factor, float>();
			for (int i = 0; i < fl.Length; i++) {
				Factor key = (Factor)i;
				fd.Add(key, fl [i]);
			}
			return fd;
		}

		public static float[] FactorDictToList(Dictionary<Factor, float> fd) {
			float[] fl = new float[System.Enum.GetNames(typeof(Factor)).Length];
			foreach (KeyValuePair<Factor, float> kv in fd) {
				int index = (int)kv.Key;
				fl [index] = kv.Value;
			}
			return fl;
		}

		public static Dictionary<Factor, float> GenerateFactorDict() {
			float[] fl = new float[System.Enum.GetNames(typeof(Factor)).Length];
			for (int i = 0; i < fl.Length; i++) {
				fl [i] = Random.Range(-1.5f, 1.5f);
			}

			// Though we could also seed this factors the same as the others, they represent something inherently different and starting them higher
			// increases our chances that some forces actually start to influence the birds
			fl [(int)Factor.CohesCutoff] = Random.Range(5, 25);
			fl [(int)Factor.RepulsCutoff] = Random.Range(5, 25);
			fl [(int)Factor.RewardCutoff] = Random.Range(5, 25);
			fl [(int)Factor.ObstclCutoff] = Random.Range(5, 25);
			fl [(int)Factor.AlignCutoff] = Random.Range(5, 25);

			return FactorListToDict(fl);
		}
	}
}

