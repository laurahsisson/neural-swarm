using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BirdControl : MonoBehaviour {
	private FlockControl flockControl;
	private StatsControl statsControl;

	private Vector3 lastPos;
	private Vector2 force;

	private Vector3 defaultScale = new Vector3(1f, 2.5f, 1f);

	private Vector2 velocity;

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

	[System.Serializable]
	public struct Bird {
		public Vector2 position;
		public RectCorners rectCorners;
		public Vector2 velocity;
		public float size;
		public float speed;
		public float mass;
		public bool active;
	}

	public void Setup(float size, float speed, int number) {
		this.flockControl = FindObjectOfType<FlockControl>();
		this.statsControl = FindObjectOfType<StatsControl>();
		this.velocity = Vector2.zero;
		this.force = Vector2.zero;
		this.size = size;
		this.transform.localScale = defaultScale * size;
		this.speed = speed;
		this.mass = size * size;
		this.number = number;
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
			transform.position = new Vector3(transform.position.x, transform.position.y, 10);
			return;
		}

		lastPos = transform.position;

		// accel = force/mass (F=M*A)
		velocity += force / mass * Time.deltaTime;
		velocity = Vector2.ClampMagnitude(velocity, speed);
		transform.position += (Vector3)velocity * Time.deltaTime;

		updateRotation();
		handleOutOfBounds(flockControl.GetWorldBound(), lastPos);

	}

	public void OnTriggerEnter2D(Collider2D collider) {
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

	public Bird ToStruct() {
		Bird b = new Bird();
		b.mass = mass;
		b.position = transform.position;
		b.rectCorners = new RectCorners(gameObject.GetComponent<RectTransform>());
		b.size = size;
		b.speed = speed;
		b.velocity = velocity;
		b.active = moving;
		return b;
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
