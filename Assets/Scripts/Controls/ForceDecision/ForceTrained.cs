using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ForceTrained : ForceDNA {
	private FlockControl.RandomDelegate randomizePositions;
	private Genome trainedGenome;
	int map = 0;

	public ForceTrained(int numBirds, FlockControl.RandomDelegate rp) {
		randomizePositions = rp;
		Chromosome ali = new Chromosome(17.845380783081056f,4.233492851257324f,125.07916259765625f);
		Chromosome coh = new Chromosome(18.35961151123047f,6.671069145202637f,240.59176635742188f);
		Chromosome rep = new Chromosome(71.6546401977539f,8.883309364318848f,152.1246337890625f);
		Chromosome obs = new Chromosome(100.060951232910159f,7.151443481445313f,167.235451232f);
		Chromosome bou = new Chromosome(0.0f,6.893937110900879f,159.00930786132813f); 
		Chromosome rew = new Chromosome(66.051513671875f,1.0f,354.618896484375f);
		PathChrom pf = new PathChrom(); // Not using pathfinding
		trainedGenome = new Genome(ali,coh,rep,obs,bou,rew,pf);
			
	}
	public override Genome Next() {
		return trainedGenome;
	}
	public override void Score(float score) {
		map++;
		randomizePositions(map);
	}
}
