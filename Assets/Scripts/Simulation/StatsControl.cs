using UnityEngine;
using UnityEngine.UI;

public class StatsControl : MonoBehaviour {
	public Text text;

	private int wallCollisions;
	private int birdCollisions;
	private float[] completionTimes;
	private float startTime;
	private int completed;

	public struct GenerationStats {
		public int completed;
		public int wallCollisions;
		public int birdCollisions;
		public float averageTime;
		public int numBirds;
	}

	public struct PartialStats {
		public int completed;
		public int wallCollisions;
		public int birdCollisions;
		public int numBirds;
	}

	public void Setup(int numBirds, float maxTime) {
		wallCollisions = 0;
		birdCollisions = 0;
		completed = 0;
		completionTimes = new float[numBirds];
		startTime = Time.time;
		// Because we won't call complete on birds that do not complete the goal, assume they took maxTime
		for (int i = 0; i < completionTimes.Length; i++) {
			completionTimes [i] = maxTime;
		}
	}

	public void Complete(int number) {
		completionTimes [number] = Time.time - startTime;
		completed++;
	}

	public void AddWallCollision() {
		wallCollisions++;
	}

	public void AddBirdCollision() {
		birdCollisions++;
	}

	public GenerationStats CalculateStats(bool shouldPrint) {
		float maxTime = 0;
		float minTime = Mathf.Infinity;
		float totalTime = 0;
		for (int i = 0; i < completionTimes.Length; i++) {
			float time = completionTimes [i];
			if (time > maxTime) {
				maxTime = time;
			}
			if (time < minTime) {
				minTime = time;
			}
			totalTime += time;
		}
		float averageTime = totalTime / completionTimes.Length;
		float stdDevSum = 0;
		for (int i = 0; i < completionTimes.Length; i++) {
			stdDevSum = Mathf.Pow(completionTimes [i], 2f);
		}
		float standardDeviation = Mathf.Pow(stdDevSum / (completionTimes.Length - 1), .5f);

		if (shouldPrint) {
			Debug.Log("Birds Completed: " + completed);
			Debug.Log("Bird/Wall Collisions: " + wallCollisions);
			Debug.Log("Bird/Wall Collisions: " + birdCollisions);
			Debug.Log("Minimum Completion Time: " + minTime);
			Debug.Log("Maximum Completion Time: " + maxTime);
			Debug.Log("Average Completion Time: " + averageTime);
			Debug.Log("Standard Deviation of Completion Time: " + standardDeviation);
		}
		GenerationStats gs = new GenerationStats();
		gs.completed = completed;
		gs.birdCollisions = birdCollisions;
		gs.wallCollisions = wallCollisions;
		gs.averageTime = averageTime;
		gs.numBirds = completionTimes.Length;
		return gs;
	}

	public PartialStats CalculatePartial() {
		PartialStats ps = new PartialStats();
		ps.completed = completed;
		ps.birdCollisions = birdCollisions;
		ps.wallCollisions = wallCollisions;
		ps.numBirds = completionTimes.Length;
		return ps;
	}

	private void Update() {
		text.text = "Completed: " + completed.ToString() + "\t\t" + "Bird\\Bird: " + birdCollisions + "\t\t" + "Bird\\Wall: " + wallCollisions;
	}

}
