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
		float max_time = 0;
		float min_time = Mathf.Infinity;
		float total_time = 0;
		for (int i = 0; i < completionTimes.Length; i++) {
			float time = completionTimes[i];
			if (time > max_time) {
				max_time = time;
			}
			if (time < min_time) {
				min_time = time;
			}
			total_time += time;
		}
		Debug.Log("Minimum Completion Time: " + min_time);
		Debug.Log("Maximum Completion Time: " + max_time);
		Debug.Log("Average Completion Time: " + (total_time/completionTimes.Length));
	}
}
