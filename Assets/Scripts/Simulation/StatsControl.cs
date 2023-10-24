using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class StatsControl : MonoBehaviour {
	public Text text;

	private int[] wallCollisions;
	private int[] birdCollisions;
	private int[] completed;

	public struct GenerationStats {
		public int[] completed;
		public int[] wallCollisions;
		public int[] birdCollisions;
	}

	public void Setup(int numBirds, float maxTime) {
		wallCollisions = new int[numBirds];
		birdCollisions = new int[numBirds];
		completed = new int[numBirds];
	}

	public void Complete(int b) {
		completed[b] = 1;
	}

	public void AddWallCollision(int b) {
		wallCollisions[b]++;
	}

	// Called twice for each collision (once for each bird involved)
	public void AddBirdCollision(int b) {
		birdCollisions[b]++;
	}

	public GenerationStats CalculateStats() {
		GenerationStats gs = new GenerationStats();
		gs.birdCollisions = (int[]) birdCollisions.Clone();
		gs.wallCollisions = (int[]) wallCollisions.Clone();
		gs.completed = (int[]) completed.Clone();
		return gs;
	}

	private void Update() {
		text.text = "";
//		text.text = "Completed: " + completed.Sum().ToString() + "\t\t" + "Bird\\Bird: " + birdCollisions.Sum().ToString() + "\t\t" + "Bird\\Wall: " + wallCollisions.Sum().ToString();
	}

}
