using UnityEngine;
using System.Linq;

public class ForceDNA {
	static readonly float MUTATION_CHANCE = .05f;

	// How close an object has to be to exert a force
	static readonly float DIST_MIN = 1;
	static readonly float DIST_MAX = 15;
	static readonly float DIST_MUT = 2;

	// The maximum value the sum of a force may exert
	static readonly float FORCE_MIN = 1;
	static readonly float FORCE_MAX = 10;
	static readonly float FORCE_MUT = 1;

	// The constant times each individual object's force
	// The higher const is the more equally weighted near and far objects will be
	static readonly float CONSTANT_MIN = 20;
	static readonly float CONSTANT_MAX = 150;
	static readonly float CONSTANT_MUT = 20;

	// The maximum force an individual object may exert
	// The higher the asymptote is the more strongly weighted very close objects are (distance of less than 1)
	static readonly float ASYMPTOTE_MIN = 50;
	static readonly float ASYMPTOTE_MAX = 300;
	static readonly float ASMYPTOTE_MUT = 40;

	// The number of steps ahead pathfinding will consider
	static readonly float STEPS_MIN = 1;
	static readonly float STEPS_MAX = 8;
	static readonly float STEPS_MUT = 1;

	// The probability that we will be given a pathfind token, give we received one last frame
	static readonly float CARRY_MIN = 0;
	static readonly float CARRY_MAX = 1;
	static readonly float CARRY_MUT = .1f;

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
		Debug.Log(gs.completed.Sum() + "," + gs.birdCollisions.Sum() + "," + gs.wallCollisions.Sum() + ":" + sum);
		// Score for a bird is that individual birds score plus average score. In better performing generations, all birds are more equall likely to reproduce.
		float ave = sum/scores.Length;
		for (int i = 0; i < scores.Length; i++) {
			scores[i] += ave;
		}
		sum *= 2;

		Genome[] newGenomes = new Genome[genomes.Length];

		for (int i = 0; i < genomes.Length; i++) {
			Genome p1 = selectParent(scores,sum);
			Genome p2 = selectParent(scores,sum);
			newGenomes[i] = new Genome(p1,p2);
		}
	}

	private Genome selectParent(float[] scores, float sum) {
		float s = Random.Range(0,sum);
		int parent = -1;
		while (s > 0) {
			parent++;
			s -= scores[parent];
		}
		return genomes[parent];
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
			Flock = new FlockChrom(Random.value>.5f ? p1.Flock : p2.Flock);
			Repulse = new ComplexChrom(Random.value>.5f ? p1.Repulse : p2.Repulse);
			Obstacle = new ComplexChrom(Random.value>.5f ? p1.Obstacle : p2.Obstacle);
			Boundary = new ComplexChrom(Random.value>.5f ? p1.Boundary : p2.Boundary);
			Reward = new ComplexChrom(Random.value>.5f ? p1.Reward : p2.Reward);
			Pathfind = new PathfindChrom(Random.value>.5f ? p1.Pathfind : p2.Pathfind);
		}
	}

	public class Chrom {
		public readonly float Distance;

		public Chrom() {
			this.Distance = Random.Range(DIST_MIN, DIST_MAX);
		}

		public Chrom(Chrom parent) {
			if (Random.value < MUTATION_CHANCE) {
				this.Distance = parent.Distance + Random.Range(-DIST_MUT, DIST_MUT);
			}
		}

	}

	public class FlockChrom: Chrom {
		public readonly float AlignForce;
		public readonly float CohesForce;

		public FlockChrom() : base() {
			this.AlignForce = Random.Range(FORCE_MIN, FORCE_MAX);
			this.CohesForce = Random.Range(FORCE_MIN, FORCE_MAX);
		}

		public FlockChrom(FlockChrom parent) : base(parent) {
			if (Random.value < MUTATION_CHANCE) {
				this.AlignForce = parent.AlignForce + Random.Range(-FORCE_MUT, FORCE_MUT);
			}
			if (Random.value < MUTATION_CHANCE) {
				this.CohesForce = parent.CohesForce + Random.Range(-FORCE_MUT, FORCE_MUT);
			}
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

		public ComplexChrom(ComplexChrom parent) : base(parent) {
			if (Random.value < MUTATION_CHANCE) {
				this.Force = parent.Force + Random.Range(-FORCE_MUT, FORCE_MUT);
			}
			if (Random.value < MUTATION_CHANCE) {
				this.Constant = parent.Constant + Random.Range(-CONSTANT_MUT, CONSTANT_MUT);
			}
			if (Random.value < MUTATION_CHANCE) {
				this.Asymptote = parent.Asymptote + Random.Range(-ASMYPTOTE_MUT, ASMYPTOTE_MUT);
			}
		}
	}

	public class PathfindChrom: ComplexChrom {
		public readonly float Steps;
		public readonly float Carryover;

		public PathfindChrom() : base() {
			this.Steps = Random.Range(STEPS_MIN, STEPS_MAX);
			this.Carryover = Random.Range(CARRY_MIN, CARRY_MAX);
		}

		public PathfindChrom(PathfindChrom parent) : base(parent) {
			if (Random.value < MUTATION_CHANCE) {
				this.Steps = parent.Steps + Random.Range(-STEPS_MUT, STEPS_MUT);
			}
			if (Random.value < MUTATION_CHANCE) {
				this.Carryover = parent.Carryover + Random.Range(-CARRY_MUT, CARRY_MUT);
			}
		}
	}
}
