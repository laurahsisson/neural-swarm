using UnityEngine;
using System;
using System.IO;
using System.Linq;

public class FileIO {

	[System.Serializable]
	public class JSONGeneration {
		public float averageScore;
		public JSONGenome[] genomes;

		public JSONGeneration(JSONGenome[] genomes) {
			float s = 0;
			foreach (JSONGenome g in genomes) {
				s += g.score;
			}
			averageScore = s / genomes.Length;
			this.genomes = genomes;
		}

		public ForceDNA.Genome[] ToGenomes() {
			ForceDNA.Genome[] gs = new ForceDNA.Genome[genomes.Length];
			for (int i = 0; i < gs.Length; i++) {
				gs [i] = genomes [i].ToGenome();
			}
			return gs;
		}
	}

	[System.Serializable]
	public class JSONGenome {
		public float score;
		public JSONChrom align;
		public JSONChrom cohesion;
		public JSONChrom repulse;
		public JSONChrom obstacle;
		public JSONChrom boundary;
		public JSONChrom reward;
		public JSONPathChrom pathfind;

		public JSONGenome(ForceDNA.Genome gen, float score) {
			this.align = new JSONChrom(gen.Align);
			this.cohesion = new JSONChrom(gen.Cohesion);
			this.repulse = new JSONChrom(gen.Repulse);
			this.obstacle = new JSONChrom(gen.Obstacle);
			this.boundary = new JSONChrom(gen.Boundary);
			this.reward = new JSONChrom(gen.Reward);
			this.pathfind = new JSONPathChrom(gen.Pathfind);
			this.score = score;
		}

		public ForceDNA.Genome ToGenome() {
			return new ForceDNA.Genome(align.ToChromosome(), cohesion.ToChromosome(), repulse.ToChromosome(), 
				obstacle.ToChromosome(), boundary.ToChromosome(), reward.ToChromosome(), pathfind.ToPathChrom());
		}
	}

	[System.Serializable]
	public class JSONChrom {
		public float constant;
		public float exponent;
		public float distance;

		public JSONChrom(ForceDNA.Chromosome cr) {
			this.constant = cr.Constant;
			this.exponent = cr.Exponent;
			this.distance = cr.Distance;
		}

		public ForceDNA.Chromosome ToChromosome() {
			return new ForceDNA.Chromosome(constant, exponent, distance);
		}
	}

	// Note that JSONPathChrom IS a JSONChrom, while PathChrom HAS a Chromomsome. It looks more readable in JSON this way.
	[System.Serializable]
	public class JSONPathChrom : JSONChrom {
		public float steps;
		public float carryover;
		public float view;

		public JSONPathChrom(ForceDNA.PathChrom pcr) : base(pcr.Chrom) {
			this.steps = pcr.Steps;
			this.carryover = pcr.Carryover;
			this.view = pcr.View;
		}

		public ForceDNA.PathChrom ToPathChrom() {
			
			return new ForceDNA.PathChrom(ToChromosome(), steps, carryover, view);
		}
	}

	public static void WriteToFile(string uuid, int gen, ForceDNA.Genome[] genomes, float[] scores) {
		JSONGenome[] jgs = new JSONGenome[genomes.Length];
		for (int i = 0; i < genomes.Length; i++) {
			jgs [i] = new JSONGenome(genomes [i], scores [i]);
		}
		JSONGeneration jge = new JSONGeneration(jgs);
		string path = getPath(uuid, gen, true);
		string json = JsonUtility.ToJson(jge, true);
		File.WriteAllLines(path, new string[]{ json });
	}

	public static ForceDNA.Genome[] ReadFromFile(string uuid, int gen) {
		string path = getPath(uuid, gen, false);
		// This text is added only once to the file.
		if (!File.Exists(path)) {
			return null;
		}
		// Open the file to read from.
		string readText = File.ReadAllText(path);
		Debug.Log(readText);
		JSONGeneration jge = JsonUtility.FromJson<JSONGeneration>(readText);
		Debug.Log(jge);
		return jge.ToGenomes();
	}

	private static string getPath(string name, int gen, bool makeDirectory) {
		string dir = Application.persistentDataPath + "/Saves/" + name; 
		if (makeDirectory) {
			Directory.CreateDirectory(dir);
		}
		string title = gen + ".save";
		return dir + "/" + title;
	}
}
