using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StatsControl : MonoBehaviour {
	private int wallCollisions;
	private int birdCollisions;
	private float[] completionTimes;
	private float startTime;

	public void Setup(int numBirds, float maxTime) {
		wallCollisions = 0;
		birdCollisions = 0;
		completionTimes = new float[numBirds];
		startTime = Time.time;
		// Because we won't call complete on birds that do not complete the goal, assume they took maxTime
		for (int i = 0; i < completionTimes.Length; i++) {
			completionTimes[i] = maxTime;
		}
	}

	public void Complete(int number) {
		completionTimes [number] = Time.time - startTime;
	}

	public void AddWallCollision() {
		wallCollisions++;
	}

	public void AddBirdCollision() {
		birdCollisions++;
	}

	public void PrintStats() {
		Debug.Log("Bird/Wall Collisions: " + wallCollisions);
		Debug.Log("Bird/Wall Collisions: " + birdCollisions);
		float maxTime = 0;
		float minTime = Mathf.Infinity;
		float totalTime = 0;
		for (int i = 0; i < completionTimes.Length; i++) {
			float time = completionTimes[i];
			if (time > maxTime) {
				maxTime = time;
			}
			if (time < minTime) {
				minTime = time;
			}
			totalTime += time;
		}
		float averageTime = totalTime/completionTimes.Length;
		float stdDevSum = 0;
		for (int i = 0; i < completionTimes.Length; i++) {
			stdDevSum = Mathf.Pow(completionTimes[i],2f);
		}
		float standardDeviation = Mathf.Pow(stdDevSum/(completionTimes.Length-1),.5f);
		Debug.Log("Minimum Completion Time: " + minTime);
		Debug.Log("Maximum Completion Time: " + maxTime);
		Debug.Log("Average Completion Time: " + averageTime);
		Debug.Log("Standard Deviation of Completion Time: " + standardDeviation);

	}
}
