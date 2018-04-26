using UnityEngine;
using System.Linq;

public class ForceDNA {
	// Use a high mutation rate because we are not flipping bits but modifying floats
	static readonly float MUTATION_CHANCE = .25f;
	// How far in our range we travel in one mutation
	static readonly float MUTATION_RATE = .5f;


	// How close an object has to be to exert a force
	static readonly float DIST_MIN = 1;
	static readonly float DIST_MAX = 20;
	static readonly float DIST_MUT = (DIST_MIN+DIST_MAX)*MUTATION_RATE;

	// The maximum value the sum of a force may exert
	static readonly float FORCE_MIN = 1;
	static readonly float FORCE_MAX = 15;
	static readonly float FORCE_MUT = (FORCE_MIN+FORCE_MAX)*MUTATION_RATE;

	// The constant times each individual object's force
	// The higher const is the more equally weighted near and far objects will be
	static readonly float CONSTANT_MIN = 20;
	static readonly float CONSTANT_MAX = 150;
	static readonly float CONSTANT_MUT = (CONSTANT_MIN+CONSTANT_MAX)*MUTATION_RATE;

	// The maximum force an individual object may exert
	// The higher the asymptote is the more strongly weighted very close objects are (distance of less than 1)
	static readonly float ASYMPTOTE_MIN = 50;
	static readonly float ASYMPTOTE_MAX = 300;
	static readonly float ASYMPTOTE_MUT = (ASYMPTOTE_MIN+ASYMPTOTE_MAX)*MUTATION_RATE;

	// The number of steps ahead pathfinding will consider
	static readonly float STEPS_MIN = 3;
	static readonly float STEPS_MAX = 8;
	static readonly float STEPS_MUT = (STEPS_MIN+STEPS_MAX)*MUTATION_RATE;

	// The probability that we will be given a pathfind token, give we received one last frame
	static readonly float CARRY_MIN = 0;
	static readonly float CARRY_MAX = 1;
	static readonly float CARRY_MUT = (CARRY_MIN+CARRY_MAX)*MUTATION_RATE;

	static readonly float COMPLETED_MULT = 500;
	static readonly float BIRD_MULT = 2;
	static readonly float WALL_MULT = 1;


	private Genome[] genomes;
	private float[] scores;

	public ForceDNA(int numBirds) {
		genomes = new Genome[numBirds];
		for (int i = 0; i < numBirds; i++) {
			genomes[i] = new Genome();
		}
	}

	public Genome[] Next() {
		// Not a deep copy, but Genomes and Chroms are immutable, so we are safe
		return (Genome[]) genomes.Clone();
	}

	public void Evolve(StatsControl.GenerationStats gs) {
		float[] scores = new float[gs.completed.Length];
		float sum = 0;
		for (int i = 0; i < scores.Length; i++) {
			scores[i] = Mathf.Max(1,(gs.completed[i]*COMPLETED_MULT) - (gs.birdCollisions[i]*BIRD_MULT+gs.wallCollisions[i]*WALL_MULT));
			sum += scores[i];
		}

		float s = 0;
		for (int i = 0; i < genomes.Length; i++) {
			s+=genomes[i].Flock.AlignForce*scores[i];
		}
		Debug.Log(s/sum);

//		Debug.Log(gs.completed.Sum() + "," + gs.birdCollisions.Sum() + "," + gs.wallCollisions.Sum() + ":" + sum);
		// Score for a bird is that individual birds score plus average score. In better performing generations, all birds are more equall likely to reproduce.
		float ave = sum/scores.Length;
		for (int i = 0; i < scores.Length; i++) {
			scores[i] += ave;
		}
		sum *= 2;

		Genome[] newGenomes = new Genome[genomes.Length];

		for (int i = 0; i < genomes.Length; i++) {
			int p1 = selectParent(scores,sum);
			int p2 = selectParent(scores,sum);
			Genome g1 = genomes[p1];
			Genome g2 = genomes[p2];
			newGenomes[i] = new Genome(g1,g2);
		}
	}

	private int selectParent(float[] scores, float sum) {
		float s = Random.Range(0,sum);
		int parent = -1;
		while (s > 0) {
			parent++;
			s -= scores[parent];
		}
		return parent;
	}

	public class Genome {
		public readonly FlockChrom Flock;
		public readonly ComplexChrom Repulse;
		public readonly ComplexChrom Obstacle;
		public readonly ComplexChrom Boundary;
		public readonly ComplexChrom Reward;
		public readonly PathfindChrom Pathfind;

		public Genome() {
			Flock = new FlockChrom();
			Repulse = new ComplexChrom();
			Obstacle = new ComplexChrom();
			Boundary = new ComplexChrom();
			Reward = new ComplexChrom();
			Pathfind = new PathfindChrom();
		}

		public Genome(Genome p1, Genome p2) {
			Flock = new FlockChrom(p1.Flock, p2.Flock);
			Repulse = new ComplexChrom(p1.Repulse, p2.Repulse);
			Obstacle = new ComplexChrom(p1.Obstacle, p2.Obstacle);
			Boundary = new ComplexChrom(p1.Boundary, p2.Boundary);
			Reward = new ComplexChrom(p1.Reward, p2.Reward);
			Pathfind = new PathfindChrom(p1.Pathfind, p2.Pathfind);
		}
	}

	public class Chrom {
		public readonly float Distance;

		public Chrom() {
			this.Distance = Random.Range(DIST_MIN, DIST_MAX);
		}

		public Chrom(Chrom p1, Chrom p2) {
			float dm = (Random.value < MUTATION_CHANCE) ? Random.Range(-DIST_MUT, DIST_MUT) : 0;
			this.Distance = (Random.value>.5f ? p1.Distance : p2.Distance) + dm;
		}

	}

	public class FlockChrom: Chrom {
		public readonly float AlignForce;
		public readonly float CohesForce;

		public FlockChrom() : base() {
			this.AlignForce = Random.Range(FORCE_MIN, FORCE_MAX);
			this.CohesForce = Random.Range(FORCE_MIN, FORCE_MAX);
		}

		public FlockChrom(FlockChrom p1, FlockChrom p2) : base(p1, p2) {
			float am = (Random.value < MUTATION_CHANCE) ? Random.Range(-FORCE_MUT, FORCE_MUT) : 0;
			this.AlignForce = (Random.value>.5f ? p1.AlignForce : p2.AlignForce) + am;

			float cm = (Random.value < MUTATION_CHANCE) ? Random.Range(-FORCE_MUT, FORCE_MUT) : 0;
			this.CohesForce = (Random.value>.5f ? p1.CohesForce : p2.CohesForce) + cm;
		}
	}


	public class ComplexChrom: Chrom {
		public readonly float Force;
		public readonly float Constant;
		public readonly float Asymptote;

		public ComplexChrom() : base() {
			this.Force = Random.Range(FORCE_MIN, FORCE_MAX);
			this.Constant = Random.Range(CONSTANT_MIN, CONSTANT_MAX);
			this.Asymptote = Random.Range(ASYMPTOTE_MIN, ASYMPTOTE_MAX);
		}

		public ComplexChrom(ComplexChrom p1, ComplexChrom p2) : base(p1, p2) {
			float fm = (Random.value < MUTATION_CHANCE) ? Random.Range(-FORCE_MUT, FORCE_MUT) : 0;
			this.Force = (Random.value>.5f ? p1.Force : p2.Force) + fm;

			float cm = (Random.value < MUTATION_CHANCE) ? Random.Range(-CONSTANT_MUT, CONSTANT_MUT) : 0;
			this.Constant = (Random.value>.5f ? p1.Constant : p2.Constant) + cm;

			float am = (Random.value < MUTATION_CHANCE) ? Random.Range(-ASYMPTOTE_MUT, ASYMPTOTE_MUT) : 0;
			this.Asymptote = (Random.value>.5f ? p1.Asymptote : p2.Asymptote) + am;

		}
	}

	public class PathfindChrom: ComplexChrom {
		public readonly float Steps;
		public readonly float Carryover;

		public PathfindChrom() : base() {
			this.Steps = Random.Range(STEPS_MIN, STEPS_MAX);
			this.Carryover = Random.Range(CARRY_MIN, CARRY_MAX);
		}

		public PathfindChrom(PathfindChrom p1, PathfindChrom p2) : base(p1, p2) {
			float sm = (Random.value < MUTATION_CHANCE) ? Random.Range(-STEPS_MUT, STEPS_MUT) : 0;
			this.Steps = (Random.value>.5f ? p1.Steps : p2.Steps) + sm;

			float cm = (Random.value < MUTATION_CHANCE) ? Random.Range(-CARRY_MUT, CARRY_MUT) : 0;
			this.Carryover = (Random.value>.5f ? p1.Carryover : p2.Carryover) + cm;
		}
	}
}
