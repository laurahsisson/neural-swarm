using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ForceDNA {
	static readonly float MUTATION_CHANCE = .05f;

	static readonly float DIST_MIN = 1;
	static readonly float DIST_MAX = 15;
	static readonly float DIST_MUTATOR = 2;

	static readonly float FORCE_MIN = 1;
	static readonly float FORCE_MAX = 10;
	static readonly float FORCE_MUTATOR = 1;

	static readonly float CONSTANT_MIN = 20;
	static readonly float CONSTANT_MAX = 150;
	static readonly float CONSTANT_MUTATOR = 20;

	static readonly float ASYMPTOTE_MIN = 50;
	static readonly float ASYMPTOTE_MAX = 300;
	static readonly float ASMYPTOTE_MUTATOR = 40;

	static readonly float STEPS_MIN = 1;
	static readonly float STEPS_MAX = 8;
	static readonly float STEPS_MUTATOR = 1;

	public readonly FlockGenome Flock;
	public readonly ComplexGenome Repulse;
	public readonly ComplexGenome Obstacle;
	public readonly ComplexGenome Boundary;
	public readonly ComplexGenome Reward;
	public readonly PathfindGenome Pathfind;

	public ForceDNA(){
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
			this.Distance = parent.Distance + Random.Range(-DIST_MUTATOR, DIST_MUTATOR);
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
			this.AlignForce = parent.AlignForce + Random.Range(-FORCE_MUTATOR, FORCE_MUTATOR);
			this.CohesForce = parent.AlignForce + Random.Range(-FORCE_MUTATOR, FORCE_MUTATOR);
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
			this.Force = parent.Force + Random.Range(-FORCE_MUTATOR, FORCE_MUTATOR);
			;
			this.Constant = parent.Constant + Random.Range(-CONSTANT_MUTATOR, CONSTANT_MUTATOR);
			this.Asymptote = parent.Asymptote + Random.Range(-ASMYPTOTE_MUTATOR, ASMYPTOTE_MUTATOR);
		}
	}

	public class PathfindGenome: ComplexGenome {
		public readonly float Steps;

		public PathfindGenome() : base() {
			this.Steps = Random.Range(STEPS_MIN, STEPS_MAX);
		}

		public PathfindGenome(PathfindGenome parent) : base(parent) {
			this.Steps = parent.Steps + Random.Range(-STEPS_MUTATOR, STEPS_MUTATOR);
		}
	}

}
