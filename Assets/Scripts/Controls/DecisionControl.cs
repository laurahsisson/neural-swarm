using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DecisionControl : MonoBehaviour {

	public virtual void InitializeModel() {
	}

	public virtual void StartGeneration(FlockControl.UnityState us) {
	}

	public virtual Vector2[] MakeDecisions(FlockControl.UnityState us) {
		Vector2[] decisions = new Vector2[us.birds.Length];
		for (int i = 0; i < decisions.Length; i++) {
			decisions [i] = Vector2.zero;
		}
		return decisions;
	}

	public virtual void EndGeneration(StatsControl.GenerationStats gs) {
	}

}
