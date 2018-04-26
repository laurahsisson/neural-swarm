using UnityEngine;
using System.Linq;

public abstract class ForceDNA {
	protected static readonly float CONST_MIN = 0;
	protected static readonly float CONST_MAX = 100;

	protected static readonly float EXP_MIN = 1;
	protected static readonly float EXP_MAX = 10;

	protected static readonly float STEP_MIN = 0;
	protected static readonly float STEP_MAX = 50;

	protected static readonly float CARRY_MIN = 0;
	protected static readonly float CARRY_MAX = 100;

	protected static readonly float DIST_MIN = 0;
	protected static readonly float DIST_MAX = 50;

	protected static readonly float VIEW_MIN = 0;
	protected static readonly float VIEW_MAX = 360;

	public abstract Genome Next();

	public abstract void Score(float score);

	public class Genome {
		public readonly Chromosome Align;
		public readonly Chromosome Cohesion;
		public readonly Chromosome Repulse;
		public readonly Chromosome Obstacle;
		public readonly Chromosome Boundary;
		public readonly Chromosome Reward;
		public readonly PathChrom Pathfind;

		public Genome() {
			Align = new Chromosome();
			Cohesion = new Chromosome();
			Repulse = new Chromosome();
			Obstacle = new Chromosome();
			Boundary = new Chromosome();
			Reward = new Chromosome();
			Pathfind = new PathChrom();
		}

		public Genome(Chromosome ali, Chromosome coh, Chromosome rep, Chromosome obs, Chromosome bou, Chromosome rew, PathChrom pf) {
			Align = ali;
			Cohesion = coh;
			Repulse = rep;
			Obstacle = obs;
			Boundary = bou;
			Reward = rew;
			Pathfind = pf;
		}
	}

	public class Chromosome {
		public readonly float Constant;
		public readonly float Exponent;

		public Chromosome() {
			Constant = Random.Range(CONST_MIN, CONST_MAX);
			Exponent = Random.Range(EXP_MIN, EXP_MAX);
		}

		public Chromosome(float c, float e) {
			Constant = c;
			Exponent = e;
		}

	}


	public class PathChrom {
		public readonly Chromosome Chrom;
		public readonly float Steps;
		public readonly float Carryover;
		public readonly float Distance;
		public readonly float View;

		public float Constant {
			get {
				return Chrom.Constant;
			}
		}

		public float Exponent {
			get {
				return Chrom.Exponent;
			}
		}

		public PathChrom() {
			Chrom = new Chromosome();
			Steps = Random.Range(STEP_MIN, STEP_MAX);
			Carryover = Random.Range(CARRY_MIN, CARRY_MAX);
			Distance = Random.Range(DIST_MIN, DIST_MAX);
			View = Random.Range(VIEW_MIN, VIEW_MAX);
		}

		public PathChrom(Chromosome cr, float s, float c, float d, float v) {
			Chrom = cr;
			Steps = s;
			Distance = d;
			View = v;
			Carryover = c;
		}
	}
}
