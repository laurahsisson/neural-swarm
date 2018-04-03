using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ForceDecisionControl : DecisionControl {
	private readonly int NUM_DNA = 10;
	private int currentDNA;
	private Dictionary<ForceDNA.Factor, float> currentFactor;
	private Dictionary<ForceDNA.Factor, float>[] allFactors;
	private float[] scores;

	private readonly float CompletedMult = 1000;
	private readonly float AverageTimeMult = -100;
	private readonly float BirdCollisionMult = -2;
	private readonly float WallCollisionMult = -5;


	public override void InitializeModel() {
		currentDNA = 0;
		allFactors = new Dictionary<ForceDNA.Factor, float>[NUM_DNA];
		for (int i = 0; i < allFactors.Length; i++) {
			allFactors [i] = ForceDNA.GenerateFactorDict();
		}
		scores = new float[NUM_DNA];
	}

	public override void StartGeneration() {
		currentFactor = allFactors [currentDNA];
	}

	public override Vector2[] MakeDecisions(FlockControl.UnityState us) {
		Vector2[] forces = new Vector2[us.birds.Length];
		for (int i = 0; i < forces.Length; i++) {
			forces [i] = getForce(us, i);
		}
		return forces;
	}

	public override void EndGeneration(StatsControl.GenerationStats gs) {
		// The higher the score the score the better
		float sc = (gs.completed * CompletedMult) + (gs.averageTime * AverageTimeMult) + (gs.birdCollisions * BirdCollisionMult) + (gs.wallCollisions * WallCollisionMult);
		scores [currentDNA] = sc;
		currentDNA++;
		if (currentDNA == NUM_DNA) {
			currentDNA = 0;
			evolve();
			scores = new float[NUM_DNA];
		}
	}

	private void evolve() {
		float sum = 0;
		for (int i = 0; i < scores.Length; i++) {
			sum += scores [i];
		}

		float[] adjustedScores = new float[scores.Length];

		// Divide each score by the sum so the new total of the scores is 1.
		for (int i = 0; i < adjustedScores.Length; i++) {
			adjustedScores [i] = scores [i] / sum;
		}
		print(adjustedScores);

		float[][] newFactors = new float[NUM_DNA][];
		for (int currentChild = 0; currentChild < NUM_DNA; currentChild++) {
			int p1 = selectParent(adjustedScores);
			int p2 = selectParent(adjustedScores);

			float[] parent1 = ForceDNA.FactorDictToList(allFactors [p1]);
			float[] parent2 = ForceDNA.FactorDictToList(allFactors [p2]);
			float[] child = new float[parent1.Length];
			// Using uniform crossover, so half of the genes come from parent1 and the other half comes from parent2, chosen uniformally at random
			for (int i = 0; i < child.Length; i++) {
				if (Random.value <= .5f) {
					child [i] = parent1 [i];
				} else {
					child [i] = parent2 [i];
				}
			}
			newFactors [currentChild] = child;
		}

		// Now every child has been selected, so it is time for mutation
		for (int currentChild = 0; currentChild < NUM_DNA; currentChild++) {
			for (int i = 0; i < newFactors [currentChild].Length; i++) {
				if (Random.value > ForceDNA.MUTATION_CHANCE) {
					continue;
				}
				newFactors [currentChild] [i] += Random.Range(ForceDNA.MIN_FACTOR_MUTATION, ForceDNA.MAX_FACTOR_MUTATION);
			}
		}
	}

	private int selectParent(float[] adjustedScores) {
		// Because the sum of all scores is 1, we will select a value from 0...1 and walk across the DNA list until we reach the end.
		// In this way, each DNA has a chance of being selected, but the higher the adjusted score the more likely it is to reproduce
		float scoreSelected = Random.value;
		int parent = -1;
		while (scoreSelected > 0) {
			parent++;
			scoreSelected -= adjustedScores [parent];
		}
		return parent;
	}

	private Vector2 getForce(FlockControl.UnityState us, int birdNumber) {
		BirdControl me = us.birds [birdNumber];
		Vector2 align = aligment(us.birds, me);
		Vector2 cohes = cohesion(us.birds, me);
		Vector2 repul = repulsion(us.birds, me);
		Vector2 obstcl = obstacle(us.walls, me);
		Vector2 rewrd = reward(us.goal, me);
		Vector2 force = align + cohes + repul + obstcl + rewrd;
		return force.normalized * me.Speed;
	}

	private Vector2 cohesion(BirdControl[] birds, BirdControl me) {
		Vector2 force = Vector2.zero;
		foreach (BirdControl b in birds) {
			if (b.Equals(me)) {
				continue;
			}
			Vector2 delta = (Vector2)(b.transform.position - me.transform.position);
			float dist = delta.magnitude;
			if (dist > currentFactor [ForceDNA.Factor.CohesCutoff]) {
				continue;
			}
			Vector2 norm = delta / dist;
			float massFactor = Mathf.Pow(b.Mass, currentFactor [ForceDNA.Factor.CohesMassExp]);
			float distFactor = Mathf.Pow(dist, currentFactor [ForceDNA.Factor.CohesDistExp]);
			force += norm * massFactor * distFactor;
		}
		float myMassFactor = Mathf.Pow(me.Mass, currentFactor [ForceDNA.Factor.CohesMassExp]);
		force *= currentFactor [ForceDNA.Factor.CohesConst] * myMassFactor;
		return force;
	}

	private Vector2 repulsion(BirdControl[] birds, BirdControl me) {
		Vector2 force = Vector2.zero;
		foreach (BirdControl b in birds) {
			if (b.Equals(me)) {
				continue;
			}
			Vector2 delta = (Vector2)(b.transform.position - me.transform.position);
			float dist = delta.magnitude;
			if (dist > currentFactor [ForceDNA.Factor.RepulsCutoff]) {
				continue;
			}
			Vector2 norm = delta / dist;
			float massFactor = Mathf.Pow(b.Mass, currentFactor [ForceDNA.Factor.RepulsMassExp]);
			float distFactor = Mathf.Pow(dist, currentFactor [ForceDNA.Factor.RepulsDistExp]);
			force += norm * massFactor * distFactor;
		}
		float myMassFactor = Mathf.Pow(me.Mass, currentFactor [ForceDNA.Factor.RepulsMassExp]);
		force *= currentFactor [ForceDNA.Factor.RepulsConst] * myMassFactor;
		return force;
	}

	private Vector2 aligment(BirdControl[] birds, BirdControl me) {
		Vector2 force = Vector2.zero;
		foreach (BirdControl b in birds) {
			if (b.Equals(me)) {
				continue;
			}
			Vector2 delta = (Vector2)(b.transform.position - me.transform.position);
			float dist = delta.magnitude;
			if (dist > currentFactor [ForceDNA.Factor.AlignCutoff]) {
				continue;
			}
			float speed = b.Velocity.magnitude;
			if (speed == 0) {
				continue;
			}
			Vector2 norm = b.Velocity / speed;
			float massFactor = Mathf.Pow(b.Mass, currentFactor [ForceDNA.Factor.AlignMassExp]);
			float distFactor = Mathf.Pow(dist, currentFactor [ForceDNA.Factor.AlignDistExp]);
			float speedFactor = Mathf.Pow(speed, currentFactor [ForceDNA.Factor.AlignSpeedExp]);
			force += norm * massFactor * distFactor * speedFactor;
		}
		float myMassFactor = Mathf.Pow(me.Mass, currentFactor [ForceDNA.Factor.AlignMassExp]);
		force *= currentFactor [ForceDNA.Factor.AlignConst] * myMassFactor;
		return force;
	}

	private Vector2 obstacle(GameObject[] walls, BirdControl me) {
		Vector2 force = Vector2.zero;
		foreach (GameObject w in walls) {
			ColliderDistance2D cd = me.gameObject.GetComponent<Collider2D>().Distance(w.GetComponent<Collider2D>());
			Vector2 delta = (cd.pointB - (Vector2)me.transform.position);
			float dist = delta.magnitude;
			if (dist > currentFactor [ForceDNA.Factor.ObstclCutoff]) {
				continue;
			}
			Vector2 norm = delta / dist;
			float distFactor = Mathf.Pow(dist, currentFactor [ForceDNA.Factor.ObstclDistExp]);
			force += norm * distFactor;
		}
		float myMassFactor = Mathf.Pow(me.Mass, currentFactor [ForceDNA.Factor.ObstclMassExp]);
		force *= currentFactor [ForceDNA.Factor.ObstclConst] * myMassFactor;
		return force;
	}

	private Vector2 reward(GameObject goal, BirdControl me) {
		Vector2 delta = (Vector2)(goal.transform.position - me.transform.position);
		float dist = delta.magnitude;
		if (dist > currentFactor [ForceDNA.Factor.RewardCutoff]) {
			return Vector2.zero;
		}
		Vector2 norm = delta / dist;
		float distFactor = Mathf.Pow(dist, currentFactor [ForceDNA.Factor.RewardDistExp]);
		float myMassFactor = Mathf.Pow(me.Mass, currentFactor [ForceDNA.Factor.RewardMassExp]);
		return norm * distFactor * myMassFactor * currentFactor [ForceDNA.Factor.RewardConst];
	}

	private class ForceDNA {
		public static readonly float MIN_FACTOR_VALUE = -2f;
		public static readonly float MAX_FACTOR_VALUE = 2f;

		public static readonly float MIN_CUTOFF_VALUE = 5f;
		public static readonly float MAX_CUTOFF_VALUE = 25f;

		public static readonly float MIN_FACTOR_MUTATION = -.25f;
		public static readonly float MAX_FACTOR_MUTATION = .25f;

		public static readonly float MUTATION_CHANCE = .05f;

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
				fl [i] = Random.Range(MIN_FACTOR_VALUE, MAX_FACTOR_VALUE);
			}

			// Though we could also seed this factors the same as the others, they represent something inherently different and starting them higher
			// increases our chances that some forces actually start to influence the birds
			fl [(int)Factor.CohesCutoff] = Random.Range(MIN_CUTOFF_VALUE, MAX_CUTOFF_VALUE);
			fl [(int)Factor.RepulsCutoff] = Random.Range(MIN_CUTOFF_VALUE, MAX_CUTOFF_VALUE);
			fl [(int)Factor.RewardCutoff] = Random.Range(MIN_CUTOFF_VALUE, MAX_CUTOFF_VALUE);
			fl [(int)Factor.ObstclCutoff] = Random.Range(MIN_CUTOFF_VALUE, MAX_CUTOFF_VALUE);
			fl [(int)Factor.AlignCutoff] = Random.Range(MIN_CUTOFF_VALUE, MAX_CUTOFF_VALUE);

			return FactorListToDict(fl);
		}
	}
}

