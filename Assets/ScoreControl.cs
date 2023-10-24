using UnityEngine;
using System.Linq;

public class ScoreControl : MonoBehaviour {
	private static readonly float ASYMPTOTE=1000;
	// Set this up so that score is above 0 for most generations
	private static readonly float BIRD_CONST=-.02f;
	private static readonly float WALL_CONST=-.01f;
	private static readonly float GOAL_CONST=10000;
	private static readonly float COMPLETED_CONST = 1000000;

	float[] scores;


	public void Setup(int numBirds) {
		scores = new float[numBirds];
	}

	public void SetScore(FlockControl.UnityState us) {
		for (int i = 0; i < us.birds.Length; i++) {
			scores[i] = calcScore(us, us.birds[i]);
		}
	}

	public float GetScore(StatsControl.GenerationStats gs) {
//		float totalScore = scores.Sum() + gs.completed.Sum()*COMPLETED_CONST;
//		Debug.Log(totalScore + " Completed: " + gs.completed.Sum() + " Birds: " + gs.birdCollisions.Sum() + " Walls: " + gs.wallCollisions.Sum());
		float totalScore = gs.completed.Sum()*1000-gs.birdCollisions.Sum()*2-gs.wallCollisions.Sum();
		return totalScore;
	}

	private float calcScore(FlockControl.UnityState us, BirdControl me) {
		float score = 0;

		float gd = (us.goal.transform.position-transform.position).magnitude;
		score += GOAL_CONST*Mathf.Min(ASYMPTOTE, 1/Mathf.Sqrt(gd));

		if (!me.Moving) {
			return score;
		}

		foreach (BirdControl b in us.birds) {
			if (b.Equals(me)||!b.Moving) {
				continue;
			}

			float d = me.GetDistance(b).dist;
			score += BIRD_CONST*Mathf.Min(ASYMPTOTE, 1/Mathf.Sqrt(d));
		}

		for (int i = 0; i < us.walls.Length; i++) {
			float d = me.WallDistance(i).dist;
			score += WALL_CONST*Mathf.Min(ASYMPTOTE, 1/Mathf.Sqrt(d));

		}

		return score;
	}
}
