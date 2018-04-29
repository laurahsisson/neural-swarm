using UnityEngine;
using System;
using System.IO;
using System.Linq;

public class FileIO {

	public static ForceDNA.Genome[] ReadFromFile(string uuid, int gen) {
		string path = getPath(uuid,gen,false);
		// This text is added only once to the file.
		if (!File.Exists(path)) {
			return null;
		}
		// Open the file to read from.
		string readText = File.ReadAllText(path);
		System.Text.RegularExpressions.Regex rx = new System.Text.RegularExpressions.Regex("\n\n");
		string[] array = rx.Split(readText);
		ForceDNA.Genome[] genomes = new ForceDNA.Genome[array.Length-1];
		for (int x = 1; x < array.Length; x++) {
			string item = array[x];
			string[] split = item.Split('\n');
			string g = "";
			for (int i = 2; i < split.Length; i++) {
				g+=split[i]+"\n";
			}
			genomes[x-1]=ReadGenome(g);
		}
		return genomes;
	}

	public static void WriteToFile(string uuid, int gen, ForceDNA.Genome[] genomes, float[] scores) {
		string[] lines = new string[1 + genomes.Length];
		lines [0] = "Average Score: " + (scores.Sum() / genomes.Length);
		for (int i = 0; i < genomes.Length; i++) {
			lines [i + 1] = "\n\nScore: " + scores [i] + "\n" + genomes [i].ToString();
		}
		string path = getPath(uuid,gen,true);
		File.WriteAllLines(path, lines);
	}

	public static ForceDNA.Genome ReadGenome(string str) {
		string[] split = str.Split("\n".ToCharArray());
		ForceDNA.Chromosome ali = ReadChromosome(split[0]);
		ForceDNA.Chromosome coh = ReadChromosome(split[1]);
		ForceDNA.Chromosome rep = ReadChromosome(split[2]);
		ForceDNA.Chromosome obs = ReadChromosome(split[3]);
		ForceDNA.Chromosome bou = ReadChromosome(split[4]);
		ForceDNA.Chromosome rew = ReadChromosome(split[5]);
		ForceDNA.PathChrom pf = ReadPathChrom(split[6]);

		return new ForceDNA.Genome(ali, coh, rep, obs, bou, rew, pf);
	}

	public static ForceDNA.Chromosome ReadChromosome(string str) {
		string[] split = str.Split("\t".ToCharArray());
		float c = float.Parse(split [0].Split(":".ToCharArray()) [1]);
		float e = float.Parse(split [1].Split(":".ToCharArray()) [1]);
		return new ForceDNA.Chromosome(c, e);
	}

	public static ForceDNA.PathChrom ReadPathChrom(string str) {
		string[] split = str.Split("\t".ToCharArray());
		ForceDNA.Chromosome cr = ReadChromosome(split [0] + "\t" + split [1]);
		float s = float.Parse(split [2].Split(":".ToCharArray()) [1]);
		float c = float.Parse(split [3].Split(":".ToCharArray()) [1]);
		float d = float.Parse(split [4].Split(":".ToCharArray()) [1]);
		float v = float.Parse(split [5].Split(":".ToCharArray()) [1]);
		return new ForceDNA.PathChrom(cr, s, c, d, v);
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
