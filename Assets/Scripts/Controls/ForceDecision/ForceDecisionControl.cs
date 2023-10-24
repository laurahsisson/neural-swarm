using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ForceDecisionControl : DecisionControl {
	public NewPathfindControl pf;

	// The total number of birds that will use pathfinding
	private static readonly int PATHFIND_TOKENS = 0;
	private static readonly float MAX_FORCE = 1000000;

	private ForceDNA dna;
	private ForceDNA.Genome genome;

	private Vector2[] rewardForces;
	private bool[] gotRewardToken;


	public override void InitializeModel(int numBirds, FlockControl.RandomDelegate  rp) {
		dna = new ForceTrained(numBirds, rp);
	}

	public override void StartGeneration(FlockControl.UnityState us) {
		pf.InitializeGrid(us);
		genome = dna.Next();
		gotRewardToken = new bool[us.birds.Length];
	}

	public override void EndGeneration(float score) {
		dna.Score(score);
	}

	public override Vector2[] MakeDecisions(FlockControl.UnityState us) {
		generateRewards(us);


		Vector2[] sfs = new Vector2[us.birds.Length];
		float[] mags = new float[sfs.Length];
		for (int i = 0; i < us.birds.Length; i++) {
			if (!us.birds [i].Moving) {
				sfs [i] = Vector2.zero;
				mags [i] = 0;
				continue;
			}
			sfs [i] = getStaticForces(us, i);
			mags[i] = sfs[i].magnitude;
		}

		Vector2[] forces = new Vector2[us.birds.Length];
		for (int i = 0; i < us.birds.Length; i++) {
			if (!us.birds [i].Moving) {
				forces [i] = Vector2.zero;
				continue;
			}
			Vector2 df = repulsion(us.birds, mags, us.birds [i]) + aligment(us.birds, mags, us.birds [i]);
			Vector2 f = sfs [i] + df;
			forces [i] = steer(us.birds [i], f);
		}

		return forces;
	}

	private Vector2 getStaticForces(FlockControl.UnityState us, int birdNumber) {
		BirdControl me = us.birds [birdNumber];

		Vector2 cohes = cohesion(us.birds, me);

		Vector2 obstcl = obstacle(us.walls, me);

		Vector2 goal = rewardForces [me.Number];

		Vector2 bndry = boundary(us, me); 
		return cohes + obstcl + goal + bndry;
	}

	private Vector2 steer(BirdControl me, Vector2 force) {
		/* We want to steer our current velocity towards our aim velocity (the force on our bird), so take the average of the two
         * and reflect it over the goal/ That way we aim in a way that slows us down only in the desired dimension, and speeds 
         * us up in the correct dimension.
         */
		Vector2 vel = me.Velocity.normalized;
		Vector2 aim = force.normalized;

		Vector2 ave = ((vel + aim) / 2).normalized;
		Vector2 adjustment = Vector2.zero;
		// If our aim velocity is close to orthogonal to our genome velocity, just steer using our genome velocity
		if (Vector2.Dot(vel, aim) > -.5f && vel != Vector2.zero) {
			adjustment = Vector2.Reflect(-1 * ave, -1 * aim).normalized;
		} else {
			adjustment = aim;
		}
		return adjustment.normalized * me.Speed;
	}

	private void generateRewards(FlockControl.UnityState us) {
		int pathfindTokens = PATHFIND_TOKENS;
		rewardForces = new Vector2[us.birds.Length];
		bool[] nextGotToken = new bool[us.birds.Length];

		/* In this function, we give away a number of tokens equal to PATHFIND_TOKENS to birds so that they use the more computational expensive pathfinding.
         * We prioritize the birds that previously received tokens, then birds with no leaders, but we want to make sure that as the number of active birds falls,
         * we still give away all of our tokens to birds that are active. If we have more tokens than active birds, every bird should use pathfinding.
         */

		// First we give priority to birds that are already holding tokens
		for (int bird = 0; bird < us.birds.Length; bird++) {
			BirdControl me = us.birds [bird];
			if (!gotRewardToken [bird] || !me.Moving) {
				continue;
			}
			if (Random.value > genome.Pathfind.Carryover) {
				continue;
			}
			// This bird had a token and carries it over in this frame
			pathfindTokens = giveToken(us, bird, pathfindTokens, nextGotToken);
		}

		// Then we prioritize birds with no leaders
		int[] birdIndex = range(us.birds.Length);
		shuffle(birdIndex);
		for (int i = 0; i < birdIndex.Length && pathfindTokens > 0; i++) {
			int bird = birdIndex [i];
			BirdControl me = us.birds [bird];
			if (!me.Moving || nextGotToken [bird]) {
				continue;
			}

			bool hasLeader = false;
			foreach (BirdControl b in us.birds) {
				if (b.Equals(me) || !b.Moving) {
					continue;
				}

				float dist = me.GetDistance(b).dist;
				// Distance here is used a bit differently than in other cases, but it is trained just the same and does not result in any issues.
				if (dist > genome.Pathfind.Distance) {
					continue;
				}

				hasLeader = hasLeader || inView(b, me);
			}

			if (hasLeader) {
				continue;
			}
			// This bird has no leader so use a token
			pathfindTokens = giveToken(us, bird, pathfindTokens, nextGotToken);
		}

		// Now we give out the rest of the tokens randomly
		birdIndex = range(us.birds.Length);
		shuffle(birdIndex);
		for (int i = 0; i < birdIndex.Length && pathfindTokens > 0; i++) {
			int bird = birdIndex [i];
			BirdControl me = us.birds [bird];
			if (!me.Moving || nextGotToken [bird]) {
				continue;
			}
			// This bird has received a token through random chance
			pathfindTokens = giveToken(us, bird, pathfindTokens, nextGotToken);
		}

		// The remainder of the birds do not receive tokens and so just do simple pathfinding towards the goal
		for (int bird = 0; bird < us.birds.Length; bird++) {
			BirdControl me = us.birds [bird];
			if (!me.Moving) {
				continue;
			}
			if (nextGotToken [bird]) {
				// This bird already received a token
				continue;
			}
			rewardForces [bird] = rewardSimple(us.goal.transform.position, me);
		}

		gotRewardToken = nextGotToken;
	}

	private int giveToken(FlockControl.UnityState us, int bird, int tokens, bool[] nextGotToken) {
		nextGotToken [bird] = true;
		BirdControl me = us.birds [bird];
		Vector2[] path = pf.CalculatePath(me);
		rewardForces [bird] = rewardPathfind(path, me);
		return tokens - 1;
	}

	private int[] range(int len) {
		int[] arr = new int[len];
		for (int i = 0; i < len; i++) {
			arr [i] = i;
		}
		return arr;
	}

	private void shuffle(int[] arr) {
		int count = arr.Length;
		int last = count - 1;
		for (int i = 0; i < last; ++i) {
			int r = Random.Range(i, count);
			int tmp = arr [i];
			arr [i] = arr [r];
			arr [r] = tmp;
		}
	}

	private Vector2 cohesion(BirdControl[] birds, BirdControl me) {
		Vector2 force = Vector2.zero;
		foreach (BirdControl b in birds) {
			if (b.Equals(me) || !b.Moving) {
				continue;
			}
			BirdControl.CachedDelta cd = me.GetDistance(b);
			if (cd.dist > genome.Cohesion.Distance) {
				continue;
			}
			force += calcForce(cd.norm, cd.dist, genome.Cohesion);
		}
		return force;
	}

	private Vector2 aligment(BirdControl[] birds, float[] forces, BirdControl me) {
		Vector2 force = Vector2.zero;
		foreach (BirdControl b in birds) {
			if (b.Equals(me) || !b.Moving) {
				continue;
			}

			BirdControl.CachedDelta cd = me.GetDistance(b);
			if (cd.dist > genome.Align.Distance) {
				continue;
			}
			float mag = calcForce(cd.norm, cd.dist, genome.Align).magnitude;// * forces[b.Number];
			force += b.Velocity.normalized * mag;
		}
		return force;
	}

	private Vector2 repulsion(BirdControl[] birds, float[] forces, BirdControl me) {
		Vector2 force = Vector2.zero;
		foreach (BirdControl b in birds) {
			if (b.Equals(me) || !b.Moving) {
				continue;
			}

			BirdControl.CachedDelta cd = me.GetDistance(b);
			if (cd.dist > genome.Repulse.Distance) {
				continue;
			}
			force += calcForce(-1 * cd.norm, cd.dist, genome.Repulse);// * forces[b.Number];
		}
		return force;
	}

	private Vector2 obstacle(GameObject[] walls, BirdControl me) {
		Vector2 force = Vector2.zero;
		for (int i = 0; i < walls.Length; i++) {
			BirdControl.CachedDelta cd = me.WallDistance(i);
			if (cd.dist > genome.Obstacle.Distance) {
				continue;
			}
			force += calcForce(-1 * cd.norm, cd.dist, genome.Cohesion);
		}
		return force;
	}

	private Vector2 boundary(FlockControl.UnityState us, BirdControl me) {
		float xForce = 0;
		float yForce = 0;
		// Also, in this case, we are limiting the individual forces in the x and y dimensions to the force and distance of the genomes boundary
		// Whereas in other forces we limit the vector(x,y) to the corresponding parameters
		if (me.transform.position.x < us.roomWidth / 2) {
			float xDist = me.transform.position.x;
			if (xDist <= genome.Boundary.Distance) {
				xForce = genome.Boundary.Constant / Mathf.Pow(xDist, genome.Boundary.Exponent);
			}
		} else {
			float xDist = us.roomWidth - me.transform.position.x;
			if (xDist <= genome.Boundary.Distance) {
				xForce = -1 * genome.Boundary.Constant / Mathf.Pow(xDist, genome.Boundary.Exponent);
			}
		}

		if (me.transform.position.y < us.roomHeight / 2) {
			float yDist = me.transform.position.y;
			if (yDist <= genome.Boundary.Distance) {
				yForce = genome.Boundary.Constant / Mathf.Pow(yDist, genome.Boundary.Exponent);
			}
		} else {
			float yDist = us.roomHeight - me.transform.position.y;
			if (yDist <= genome.Boundary.Distance) {
				yForce = -1 * genome.Boundary.Constant / Mathf.Pow(yDist, genome.Boundary.Exponent);
			}
		}
		return new Vector2(xForce, yForce);
	}

	private Vector2 rewardPathfind(Vector2[] path, BirdControl me) {
		if (path.Length == 0) {
			return Vector2.zero;
		}

		Vector2 force = Vector2.zero;
		for (int i = 0; i < genome.Pathfind.Steps && i < path.Length; i++) {
			Vector2 delta = (path [i] - (Vector2)me.transform.position);
			force += calcForce(delta.normalized, i + 1, genome.Pathfind.Chrom);
		}
		return force;
	}

	private Vector2 rewardSimple(Vector2 goalPos, BirdControl me) {
		Vector2 delta = goalPos - (Vector2)me.transform.position;
		if (delta.magnitude > genome.Reward.Distance) {
			return Vector2.zero;
		}
		return calcForce(delta, genome.Reward);
	}

	private Vector2 calcForce(Vector2 delta, ForceDNA.Chromosome cr) {
		float dist = delta.magnitude;
		Vector2 norm = Vector2.zero;
		if (dist != 0) {
			// We will throw a divide by zero error here on purpose
			norm = delta / dist;
		}
		return calcForce(norm, dist, cr);
	}

	private Vector2 calcForce(Vector2 norm, float dist, ForceDNA.Chromosome cr) {
		float f = 0;
		if (dist == 0) {
			f = MAX_FORCE; 
		} else {
			f = Mathf.Min(cr.Constant / Mathf.Pow(dist, cr.Exponent), MAX_FORCE);
		}
		return norm * f;
	}

	private bool inView(BirdControl other, BirdControl me) {    
		float dir = Mathf.Rad2Deg * Mathf.Atan2(me.Velocity.y, me.Velocity.x);
		Vector3 delta = other.transform.position - me.transform.position;
		float angleTo = Mathf.Rad2Deg * Mathf.Atan2(delta.y, delta.x);
		return withinAngle(dir, angleTo, genome.Pathfind.View);
	}

	static bool withinAngle(float a, float b, float dist) {
		return (360 - Mathf.Abs(a - b) % 360 < dist || Mathf.Abs(a - b) % 360 < dist);
	}

}

