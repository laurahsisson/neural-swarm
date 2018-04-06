using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ForceDecisionControl : DecisionControl {
	private Text dnaText;

	private readonly int NUM_DNA = 1;
	private int currentDNA;
	private Dictionary<ForceDNA.Factor, float> currentFactor;
	private Dictionary<ForceDNA.Factor, float>[] allFactors;
	private StatsControl.GenerationStats[] gScores;


	// If less than this percentage reach goal, only the number reaching the goal is counted for the entirety of this section
	private readonly float CompletedPercentage = .8f;
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
		gScores = new StatsControl.GenerationStats[NUM_DNA];
	}

	public override void StartGeneration() {
		currentFactor = allFactors [currentDNA];
		dnaText.text = "\n\n\n";
		foreach (KeyValuePair<ForceDNA.Factor, float> kv in currentFactor) {
			dnaText.text += kv.Key.ToString() + ": " + kv.Value + "\n";
		}
	}

	public override Vector2[] MakeDecisions(FlockControl.UnityState us) {
		Vector2[] forces = new Vector2[us.birds.Length];
		for (int i = 0; i < forces.Length; i++) {
			forces [i] = getForce(us, i);
		}
		return forces;
	}

	public override void EndGeneration(StatsControl.GenerationStats gs) {
		gScores [currentDNA] = gs;
		currentDNA++;
		if (currentDNA == NUM_DNA) {
			currentDNA = 0;
//			evolve();
			Debug.LogError("Not evolving!");
			gScores = new StatsControl.GenerationStats[NUM_DNA];
		}
	}

	private float calcScore(StatsControl.GenerationStats gs, bool onlyGoalReached) {
		// The higher the score the score the better
		float s = 0;
		if (onlyGoalReached) {
			s = gs.completed;
		} else {
			s = (gs.completed * CompletedMult) + (gs.averageTime * AverageTimeMult) + (gs.birdCollisions * BirdCollisionMult) + (gs.wallCollisions * WallCollisionMult);
		}
		return Mathf.Max(1, s); // Score will always be greater than 0
	}

	private void evolve() {
		float sum = 0;
		bool onlyGoalReached = false;
		foreach (StatsControl.GenerationStats gs in gScores) {
			if (gs.completed < gs.numBirds * CompletedPercentage) {
				onlyGoalReached = true;
			}
		}

		float[] scores = new float[gScores.Length];
		for (int i = 0; i < scores.Length; i++) {
			scores [i] = calcScore(gScores [i], onlyGoalReached);
		}

		foreach (var x in scores) {
			print(x);
		}

		for (int i = 0; i < gScores.Length; i++) {
			sum += scores [i];
		}

		float[] adjustedScores = new float[gScores.Length];

		// Divide each score by the sum so the new total of the scores is 1.
		for (int i = 0; i < adjustedScores.Length; i++) {
			adjustedScores [i] = scores [i] / sum;
		}

		float[][] newFactors = new float[NUM_DNA][];
		for (int currentChild = 0; currentChild < NUM_DNA; currentChild++) {
			int p1 = selectParent(adjustedScores);
			int p2 = selectParent(adjustedScores);

			float[] parent1 = ForceDNA.FactorDictToList(allFactors [p1]);
			float[] parent2 = ForceDNA.FactorDictToList(allFactors [p2]);
			print(p1 + "," + p2);
			float[] child = new float[parent1.Length];
			// Using uniform crossover, so a genome has an equal chance to come from either parent
			foreach (ForceDNA.Factor[] genome in ForceDNA.genomes) {
				float p = Random.value;
				foreach (ForceDNA.Factor f in genome) {
					int i = (int)f;
					if (p <= .5f) {
						child [i] = parent1 [i];
					} else {
						child [i] = parent2 [i];
					}
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
		Vector2 sumPosition = Vector2.zero;
		foreach (BirdControl b in birds) {
			if (b.Equals(me)) {
				continue;
			}
			Vector2 delta = (Vector2)(b.transform.position - me.transform.position);
			float dist = delta.magnitude;
			if (dist > currentFactor [ForceDNA.Factor.CohesCutoff]) {
				continue;
			}
			sumPosition += (Vector2)b.transform.position;
		}
		Vector2 force = (sumPosition - (Vector2)me.transform.position);
		return force.normalized * currentFactor [ForceDNA.Factor.CohesConst];
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
			float distFactor = Mathf.Pow(dist, currentFactor [ForceDNA.Factor.RepulsDistExp]);
			force += norm * distFactor;
		}
		return force.normalized * currentFactor [ForceDNA.Factor.RepulsConst];
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
			force += norm;
		}
		return force.normalized * currentFactor [ForceDNA.Factor.AlignConst];
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
		return force.normalized * currentFactor [ForceDNA.Factor.ObstclConst];
	}

	private Vector2 reward(GameObject goal, BirdControl me) {
		Vector2 delta = (Vector2)(goal.transform.position - me.transform.position);
		float dist = delta.magnitude;
		if (dist > currentFactor [ForceDNA.Factor.RewardCutoff]) {
			return Vector2.zero;
		}
		Vector2 norm = delta / dist;
		return norm * currentFactor [ForceDNA.Factor.RewardConst];

	}

	private void Start() {
		dnaText = GetComponent<Text>();
	}


	private class ForceDNA {
		public static readonly float MIN_FACTOR_VALUE = -2f;
		public static readonly float MAX_FACTOR_VALUE = 2f;

		public static readonly float MIN_CUTOFF_VALUE = 5f;
		public static readonly float MAX_CUTOFF_VALUE = 25f;

		public static readonly float MIN_LIMIT_VALUE = 10f;
		public static readonly float MAX_LIMIT_VALUE = 50f;

		public static readonly float MIN_FACTOR_MUTATION = -.1f;
		public static readonly float MAX_FACTOR_MUTATION = .1f;

		public static readonly float MUTATION_CHANCE = .04f;

		public enum Factor {
			CohesMassExp,
			CohesDistExp,
			CohesConst,
			CohesCutoff,
			CohesLimit,

			RepulsMassExp,
			RepulsDistExp,
			RepulsForceExp,
			RepulsConst,
			RepulsCutoff,
			RepulsLimit,


			// Actually the exponent for the bird's mass as the goal has infinite mass
			RewardMassExp,
			RewardDistExp,
			RewardConst,
			RewardCutoff,
			RewardLimit,


			// As above, the exponent for the bird's mass in the attraction to the obstacles
			ObstclMassExp,
			ObstclDistExp,
			ObstclConst,
			ObstclCutoff,
			ObstclLimit,


			AlignMassExp,
			AlignDistExp,
			AlignSpeedExp,
			AlignConst,
			AlignCutoff,
			AlignLimit,

		}

		public static readonly Factor[][] genomes = new Factor[][] { 
			new Factor[] { 
				Factor.CohesMassExp,
				Factor.CohesDistExp,
				Factor.CohesConst,
				Factor.CohesCutoff,
				Factor.CohesLimit,
			}, 
			new Factor[] {
				Factor.RepulsMassExp,
				Factor.RepulsDistExp,
				Factor.RepulsForceExp,
				Factor.RepulsConst,
				Factor.RepulsCutoff,
				Factor.RepulsLimit,
			},      
			new Factor[] { 
				Factor.RewardMassExp,
				Factor.RewardDistExp,
				Factor.RewardConst,
				Factor.RewardCutoff,
				Factor.RewardLimit,
			}, 
			new Factor[] { 
				Factor.ObstclMassExp,
				Factor.ObstclDistExp,
				Factor.ObstclConst,
				Factor.ObstclCutoff,
				Factor.ObstclLimit,
			}, 
			new Factor[] {
				Factor.AlignMassExp,
				Factor.AlignDistExp,
				Factor.AlignSpeedExp,
				Factor.AlignConst,
				Factor.AlignCutoff,
				Factor.AlignLimit,
			},
		};


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
			Dictionary<Factor, float> fd = FactorListToDict(fl);

			fd[Factor.CohesConst] = 3;
			fd[Factor.CohesCutoff] = 10;

			fd[Factor.RepulsDistExp] = 1/2f;
			fd[Factor.RepulsConst] = -2;
			fd[Factor.RepulsCutoff] = 6;

			fd[Factor.AlignConst] = 1;
			fd[Factor.AlignCutoff] = 4;

			fd[Factor.ObstclDistExp] = 1/2f;
			fd[Factor.ObstclConst] = -4;
			fd[Factor.ObstclCutoff] = 6;

			fd[Factor.RewardConst] = 3;
			fd[Factor.RewardCutoff] = 50;


			return fd;
		}
	}
}

