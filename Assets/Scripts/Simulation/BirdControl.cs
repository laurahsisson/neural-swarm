using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BirdControl : MonoBehaviour {
	private FlockControl flockControl;
	private StatsControl statsControl;

	private Vector3 lastPos;
	private Vector2 force;

	private Vector3 defaultScale = new Vector3(1f, 2.5f, 1f);

	private CachedDelta[] birdDeltas;
	private CachedDelta[] wallDeltas;

	private Vector2 velocity;

	private bool countCollision;

	public Vector2 Velocity {
		get {
			return velocity;
		}
	}

	private float size;

	public float Size {
		get {
			return size;
		}
	}
	// The maximum speed a bird can have. Also, the maximum amount of a force a bird can apply in a second.
	private float speed;

	public float Speed {
		get {
			return speed;
		}
	}

	private float mass;

	public float Mass {
		get {
			return mass;
		}
	}

	private int number = -1;

	public int Number {
		get {
			return number;
		}
	}

	private bool moving = false;

	public bool Moving {
		get {
			return moving;
		}
	}

	public struct CachedDelta {
		public readonly Vector2 norm;
		public readonly float dist;
		public CachedDelta(Vector2 delta) {
			dist = delta.magnitude;
			if (dist == 0) {
				norm = delta;
			} else {
				norm = delta/dist;
			}
		}
	}

	public void Setup(FlockControl.BirdState bs, int number, int numBirds, int numWalls) {
		this.flockControl = FindObjectOfType<FlockControl>();
		this.statsControl = FindObjectOfType<StatsControl>();
		this.velocity = Vector2.zero;
		this.force = Vector2.zero;

		this.transform.localScale = defaultScale * bs.size;
		this.speed = bs.speed;
		this.size = bs.size;
		this.mass = bs.size * bs.size;
		gameObject.GetComponent<Renderer>().material.color = bs.color;
		this.velocity = bs.velocity;

		this.number = number;
		this.lastPos = transform.position;
		gameObject.GetComponent<Collider2D>().enabled = true;
		birdDeltas = new CachedDelta[numBirds];
		wallDeltas = new CachedDelta[numWalls];
		countCollision = false;

	}

	public void SetDistance(BirdControl other, Vector2 delta) {
		birdDeltas[other.number]=new CachedDelta(delta);
	}

	public CachedDelta GetDistance(BirdControl other) {
		return birdDeltas[other.number];
	}

	public void SetWallDist(int i, Vector2 delta) {
		wallDeltas[i] = new CachedDelta(delta);
	}

	public CachedDelta WallDistance(int i) {
		return wallDeltas[i];
	}

	public void Reset() {
		this.velocity = Vector2.zero;
		this.force = Vector2.zero;
		this.lastPos = transform.position;
		moving = true;
		gameObject.GetComponent<Collider2D>().enabled = true;
	}

	public void SetForce(Vector2 force) {
		if (force.magnitude > speed * 1.001f) {
			force = Vector2.ClampMagnitude(force, speed);
		}
		this.force = force;
	}

	// Update is called once per frame
	public void Update() {
		if (!moving) {
			return;
		}
		countCollision = true;

		lastPos = transform.position;

		velocity += force / mass * Time.deltaTime; // F=MA
		velocity = Vector2.ClampMagnitude(velocity, speed);
		transform.position += (Vector3)velocity * Time.deltaTime;

		updateRotation();
		handleOutOfBounds(flockControl.GetWorldBound(), lastPos);

	}

	public void OnTriggerEnter2D(Collider2D collider) {
		if (!countCollision) {
			return;
		}

		if (collider.gameObject.tag == "Bird") {
			BirdControl other = collider.GetComponent<BirdControl>();
			if (other.number < number || number == -1) {
				return;
			}
			handleBirdCollision(other);
		}
		if (collider.gameObject.tag == "Goal") {
			moving = false;
			gameObject.GetComponent<Collider2D>().enabled = false;
			transform.position = new Vector3(collider.transform.position.x,collider.transform.position.y,10);
			statsControl.Complete(number);
			flockControl.IncrementGoal();
		}
		if (collider.gameObject.tag == "Wall") {
			handleWallCollision(collider);
		}
	}

	public void OnTriggerStay2D(Collider2D collider) {
		if (collider.gameObject.tag == "Bird") {
			ColliderDistance2D dist = gameObject.GetComponent<Collider2D>().Distance(collider);
			transform.position += (Vector3)dist.normal * dist.distance / 2;
			collider.gameObject.transform.position -= (Vector3)dist.normal * dist.distance / 2;
		}
		if (collider.gameObject.tag == "Wall") {
			ColliderDistance2D dist = gameObject.GetComponent<Collider2D>().Distance(collider);
			transform.position += (Vector3)dist.normal * dist.distance;
		}
	}

	private void updateRotation() {
		float rotation = Mathf.Atan2(velocity.y, velocity.x);
		transform.rotation = Quaternion.Euler(new Vector3(0, 0, rotation * Mathf.Rad2Deg - 90));
	}

	private void handleOutOfBounds(Rect worldBound, Vector3 lastPos) {
		if (worldBound.Contains(transform.position)) {
			return;
		}

		if (transform.position.x < worldBound.xMin) {
			velocity = new Vector2(-velocity.x, velocity.y);
			transform.position = new Vector3(.01f, transform.position.y);
		}
		if (transform.position.x > worldBound.xMax) {
			velocity = new Vector2(-velocity.x, velocity.y);
			transform.position = new Vector3(worldBound.xMax - .01f, transform.position.y);
		}

		if (transform.position.y < worldBound.yMin) {
			velocity = new Vector2(velocity.x, -velocity.y);
			transform.position = new Vector3(transform.position.x, .01f);
		}

		if (transform.position.y > worldBound.yMax) {
			velocity = new Vector2(velocity.x, -velocity.y);
			transform.position = new Vector3(transform.position.x, worldBound.yMax - .01f);
		}

		force = Vector2.zero;
	}

	private void handleBirdCollision(BirdControl other) {
		velocity = getResultantVelocity(transform.position, other.transform.position, mass, other.mass, velocity, other.velocity);
		updateRotation();

		other.velocity = getResultantVelocity(other.transform.position, transform.position, other.mass, mass, other.velocity, velocity);
		other.updateRotation();
		statsControl.AddBirdCollision(number);
		statsControl.AddBirdCollision(other.number);

	}

	// Given two objects, each with a position, mass, and velocity calculates the new velocity of object 1.
	private static Vector2 getResultantVelocity(Vector2 position1, Vector2 position2, float mass1, float mass2, Vector2 velocity1, Vector2 velocity2) {
		Vector2 posDifference = (Vector2)(position1 - position2);
		float relativeMass;
		if (mass2 == Mathf.Infinity) {
			relativeMass = 2f;
		} else {
			relativeMass = (2 * mass2) / (mass1 + mass2);
		}
		float vPosDot = Vector2.Dot(velocity1 - velocity2, posDifference);
		float posDistance = Mathf.Pow(posDifference.magnitude, 2);
		Vector2 myNewVelocity = velocity1 - relativeMass * vPosDot / posDistance * posDifference;
		return myNewVelocity;
	}

	private void handleWallCollision(Collider2D other) {
		ColliderDistance2D dist = gameObject.GetComponent<Collider2D>().Distance(other);
		transform.position += (Vector3)dist.normal * dist.distance;
		velocity = getResultantVelocity(transform.position, dist.pointB, mass, Mathf.Infinity, velocity, Vector2.zero);
		statsControl.AddWallCollision(number);
	}
}
