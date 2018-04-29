using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class ForceGenetic : ForceDNA {
	// Use a high mutation rate because we are not flipping bits but modifying floats
	private static readonly float MUTATION_CHANCE = .25f;
	// How far in our range we travel in one mutation
	private static readonly float MUTATION_RATE = .5f;

	private static readonly float CONST_MUT = (CONST_MIN+CONST_MAX)/2*MUTATION_RATE;

	private static readonly float EXP_MUT = (EXP_MIN+EXP_MAX)/2*MUTATION_RATE;

	private static readonly float STEP_MUT = (STEP_MIN+STEP_MAX)/2*MUTATION_RATE;

	private static readonly float CARRY_MUT = (CARRY_MIN+CARRY_MAX)/2*MUTATION_RATE;

	private static readonly float DIST_MUT = (DIST_MIN+DIST_MAX)/2*MUTATION_RATE;

	private static readonly float VIEW_MUT = (VIEW_MIN+VIEW_MAX)/2*MUTATION_RATE;

	private static readonly int NUM_SPECIES = 30;

	private static readonly float SCORE_CUTOFF = .1f;

	private float lastScore = 0;

	private Genome[] genomes;
	private float[] scores;
	private int current;

	private string uuid;
	private int generation = 1;

	private FlockControl.RandomDelegate randomizePositions;

	public ForceGenetic(int numBirds,  FlockControl.RandomDelegate rp) {
		genomes = new Genome[NUM_SPECIES];
		for (int i = 0; i < NUM_SPECIES; i++) {
			genomes[i] = new Genome();
		}
		scores = new float[NUM_SPECIES];
		current = -1;
		randomizePositions = rp;
		Debug.Log(Application.persistentDataPath);
		uuid = System.Guid.NewGuid().ToString("N");
	}

	public override Genome Next() {
		current++;
		if (current>=genomes.Length) {
			current = 0;
			save();
			evolve();
		}
		return genomes[current];
	}

	private void evolve() {
		generation++;

		Genome[] newGenomes = new Genome[genomes.Length];

		for (int i = 0; i < genomes.Length; i++) {
			int p1 = selectParent(scores);
			int p2 = selectParent(scores);
			Genome g1 = genomes[p1];
			Genome g2 = genomes[p2];
			newGenomes[i] = Crossover(g1,g2);
		}

		genomes = newGenomes;

		float s = scores.Sum();
		float diff = (s-lastScore);
		lastScore = s;

		if (diff/s<SCORE_CUTOFF) {
			lastScore = 0;
			randomizePositions();
		}
	}

	private void save() {
		string[] lines = new string[1+genomes.Length];
		lines[0] = "Average Score: " + (scores.Sum()/genomes.Length);
		for (int i = 0; i < genomes.Length; i++) {
			lines[i+1]="\n\nScore: " + scores[i] + "\n" + genomes[i].ToString();
		}
		string dir =Application.persistentDataPath+"/Saves/"+uuid; 
		System.IO.Directory.CreateDirectory(dir);
		string title = generation+".save";
		System.IO.File.WriteAllLines(dir+"/"+title, lines);
	}

	private int selectParent(float[] adjusted) {
		float sum = adjusted.Sum();
		float s = Random.Range(0,sum);
		int parent = -1;
		while (s > 0) {
			parent++;
			s -= adjusted[parent];
		}
		return parent;
	}

	public override void Score(float score) {
		scores[current] = score;
	}

	public Genome Crossover(Genome g1, Genome g2) {
		Chromosome ali = Crossover(g1.Align, g2.Align);
		Chromosome coh = Crossover(g1.Cohesion, g2.Cohesion);
		Chromosome rep = Crossover(g1.Repulse, g2.Repulse);
		Chromosome obs = Crossover(g1.Obstacle, g2.Obstacle);
		Chromosome bou = Crossover(g1.Boundary, g2.Boundary);
		Chromosome rew = Crossover(g1.Reward, g2.Reward);
		PathChrom pf = Crossover(g1.Pathfind, g2.Pathfind);
		return new Genome(ali, coh, rep, obs, bou, rew, pf);
	}

	public Chromosome Crossover(Chromosome p1, Chromosome p2) {
		float cm = (Random.value < MUTATION_CHANCE) ? Random.Range(-CONST_MUT, CONST_MUT) : 0;
		float c = (Random.value>.5f ? p1.Constant : p2.Constant) + cm;

		float em = (Random.value < MUTATION_CHANCE) ? Random.Range(-EXP_MUT, EXP_MUT) : 0;
		float e = (Random.value>.5f ? p1.Exponent : p2.Exponent) + em;
		return new Chromosome(c,e);
	}

	public PathChrom Crossover(PathChrom p1, PathChrom p2) {
		float sm = (Random.value < MUTATION_CHANCE) ? Random.Range(-STEP_MUT, STEP_MUT) : 0;
		float s = (Random.value>.5f ? p1.Steps : p2.Steps) + sm;

		float cm = (Random.value < MUTATION_CHANCE) ? Random.Range(-CARRY_MUT, CARRY_MUT) : 0;
		float c = (Random.value>.5f ? p1.Carryover : p2.Carryover) + cm;

		float dm = (Random.value < MUTATION_CHANCE) ? Random.Range(-DIST_MUT, DIST_MUT) : 0;
		float d = (Random.value>.5f ? p1.Carryover : p2.Carryover) + dm;

		float vm = (Random.value < MUTATION_CHANCE) ? Random.Range(-VIEW_MUT, VIEW_MUT) : 0;
		float v = (Random.value>.5f ? p1.Carryover : p2.Carryover) + vm;

		Chromosome cr = Crossover(p1.Chrom,p2.Chrom);
		return new PathChrom(cr,s,c,d,v);
	}
}