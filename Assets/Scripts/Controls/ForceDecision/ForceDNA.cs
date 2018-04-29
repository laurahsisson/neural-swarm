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

		public override string ToString() {
			return Align.ToString() + "\n" + Cohesion.ToString() + "\n" + Repulse.ToString() + "\n" + Obstacle.ToString() + 
				"\n" + Boundary.ToString() + "\n" + Reward.ToString() + "\n" + Pathfind.ToString();
		}

		public static Genome FromString(string str) {
			string[] split = str.Split("\n".ToCharArray());
			Chromosome ali = Chromosome.FromString(split[0]);
			Chromosome coh = Chromosome.FromString(split[1]);
			Chromosome rep = Chromosome.FromString(split[2]);
			Chromosome obs = Chromosome.FromString(split[3]);
			Chromosome bou = Chromosome.FromString(split[4]);
			Chromosome rew = Chromosome.FromString(split[5]);
			PathChrom pf = PathChrom.FromString(split[6]);

			return new Genome(ali, coh, rep, obs, bou, rew, pf);
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
			Constant = Mathf.Clamp(c, CONST_MIN, CONST_MAX);
			Exponent = Mathf.Clamp(e, EXP_MIN, EXP_MAX);
		}

		public override string ToString() {
			string c = "Const: " + Constant;
			string e = "Exp: " + Exponent;
			return c + "\t" + e;
		}

		public static Chromosome FromString(string str) {
			string[] split = str.Split("\t".ToCharArray());
			Debug.Log(str);
			float c = float.Parse(split [0].Split(":".ToCharArray()) [1]);
			float e = float.Parse(split [1].Split(":".ToCharArray()) [1]);
			return new Chromosome(c, e);
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
			Steps = Mathf.Clamp(s, STEP_MIN, STEP_MAX);
			Carryover = Mathf.Clamp(c, CARRY_MIN, CARRY_MAX);
			Distance = Mathf.Clamp(d, DIST_MIN, DIST_MAX);
			View = Mathf.Clamp(v, VIEW_MIN, VIEW_MAX);
		}

		public override string ToString() {
			string b = Chrom.ToString();
			string s = "Step: " + Steps;
			string c = "Carry: " + Carryover;
			string d = "Dist: " + Distance;
			string v = "View: " + View;
			return b + "\t" + s + "\t" + c + "\t" + d + "\t" + v;
		}

		public static PathChrom FromString(string str) {
			string[] split = str.Split("\t".ToCharArray());
			Chromosome cr = Chromosome.FromString(split [0] + "\t" + split [1]);
			float s = float.Parse(split [2].Split(":".ToCharArray()) [1]);
			float c = float.Parse(split [3].Split(":".ToCharArray()) [1]);
			float d = float.Parse(split [4].Split(":".ToCharArray()) [1]);
			float v = float.Parse(split [5].Split(":".ToCharArray()) [1]);
			return new PathChrom(cr, s, c, d, v);
		}


	}
}
