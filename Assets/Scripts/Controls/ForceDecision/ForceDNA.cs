using UnityEngine;

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

	public readonly FlockGenome Flock;
	public readonly ComplexGenome Repulse;
	public readonly ComplexGenome Obstacle;
	public readonly ComplexGenome Boundary;
	public readonly ComplexGenome Reward;
	public readonly PathfindGenome Pathfind;

	public ForceDNA() {
		Flock = new FlockGenome();
		Repulse = new ComplexGenome();
		Obstacle = new ComplexGenome();
		Boundary = new ComplexGenome();
		Reward = new ComplexGenome();
		Pathfind = new PathfindGenome();
	}

	public class Genome {
		public readonly float Distance;

		public Genome() {
			this.Distance = Random.Range(DIST_MIN, DIST_MAX);
		}

		public Genome(Genome parent) {
			if (Random.value < MUTATION_CHANCE) {
				this.Distance = parent.Distance + Random.Range(-DIST_MUT, DIST_MUT);
			}
		}

	}

	public class FlockGenome: Genome {
		public readonly float AlignForce;
		public readonly float CohesForce;

		public FlockGenome() : base() {
			this.AlignForce = Random.Range(FORCE_MIN, FORCE_MAX);
			this.CohesForce = Random.Range(FORCE_MIN, FORCE_MAX);
		}

		public FlockGenome(FlockGenome parent) : base(parent) {
			if (Random.value < MUTATION_CHANCE) {
				this.AlignForce = parent.AlignForce + Random.Range(-FORCE_MUT, FORCE_MUT);
			}
			if (Random.value < MUTATION_CHANCE) {
				this.CohesForce = parent.CohesForce + Random.Range(-FORCE_MUT, FORCE_MUT);
			}
		}
	}


	public class ComplexGenome: Genome {
		public readonly float Force;
		public readonly float Constant;
		public readonly float Asymptote;

		public ComplexGenome() : base() {
			this.Force = Random.Range(FORCE_MIN, FORCE_MAX);
			this.Constant = Random.Range(CONSTANT_MIN, CONSTANT_MAX);
			this.Asymptote = Random.Range(ASYMPTOTE_MIN, ASYMPTOTE_MAX);
		}

		public ComplexGenome(ComplexGenome parent) : base(parent) {
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

	public class PathfindGenome: ComplexGenome {
		public readonly float Steps;
		public readonly float Carryover;

		public PathfindGenome() : base() {
			this.Steps = Random.Range(STEPS_MIN, STEPS_MAX);
			this.Carryover = Random.Range(CARRY_MIN, CARRY_MAX);
		}

		public PathfindGenome(PathfindGenome parent) : base(parent) {
			if (Random.value < MUTATION_CHANCE) {
				this.Steps = parent.Steps + Random.Range(-STEPS_MUT, STEPS_MUT);
			}
			if (Random.value < MUTATION_CHANCE) {
				this.Carryover = parent.Carryover + Random.Range(-CARRY_MUT, CARRY_MUT);
			}
		}
	}
}
