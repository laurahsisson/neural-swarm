﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BirdControl : MonoBehaviour {
	private Vector3 lastPos;


	private Vector2 velocity;
	private Vector2 accel;

	private FlockControl flockControl;
	private float size;
	private float speed;
	private float mass;
	private int number = -1;



	public void Setup(FlockControl flockControl, float size, float speed, int number) {
		this.flockControl = flockControl;
		this.velocity = Vector2.zero;
		this.size = size;
		this.transform.localScale*=size;
		this.speed = speed;
		this.mass = size*size;
		this.number = number;
		this.lastPos = transform.position;
	}


	public void SetAcceleration(Vector2 accel) {
		if (accel.magnitude > speed*1.001f) {
			Debug.Log("Setting acceleration too high:" + accel.magnitude + "," + speed);
			accel = Vector2.ClampMagnitude(accel,speed);
		}
		this.accel = accel;
	}

	// Update is called once per frame
	void Update () {
		lastPos = transform.position;

		velocity += accel/mass*Time.deltaTime;
		velocity = Vector2.ClampMagnitude(velocity,speed);
		transform.position += (Vector3)velocity*Time.deltaTime;

		updateRotation();
		handleOutOfBounds(flockControl.GetWorldBound(),lastPos);

	}

	private void updateRotation() {
		float rotation = Mathf.Atan2(velocity.y,velocity.x);
		transform.rotation = Quaternion.Euler(new Vector3(0,0,rotation*Mathf.Rad2Deg-90));
	}

	private void handleOutOfBounds(Rect worldBound,Vector3 lastPos) {
		if (worldBound.Contains(transform.position)) {
			return;
		}

		if (transform.position.x < worldBound.xMin || transform.position.x > worldBound.xMax) {
			velocity = new Vector2(-velocity.x,velocity.y);
		}

		if (transform.position.y < worldBound.yMin || transform.position.y > worldBound.yMax) {
			velocity = new Vector2(velocity.x,-velocity.y);
		}

		transform.position = lastPos;
	}

	void OnTriggerEnter2D(Collider2D collider) {
		BirdControl other = collider.GetComponent<BirdControl>();
		if (other.number < number || number == -1) {
			return;
		}
		Vector2 posDifference = (Vector2) (transform.position - other.transform.position);
		float relativeMass = (2*other.mass)/(mass + other.mass);
		float vPosDot = Vector2.Dot(velocity-other.velocity,posDifference);
		float posDistance = Mathf.Pow(posDifference.magnitude,2);
		Vector2 myNewVelocity = velocity - relativeMass * vPosDot/posDistance * posDifference;

		posDifference = (Vector2) (other.transform.position - transform.position);
		relativeMass = (2*mass)/(mass + other.mass);
		vPosDot = Vector2.Dot(other.velocity-velocity,posDifference);
		posDistance = Mathf.Pow(posDifference.magnitude,2);
		Vector2 otherNewVelocity = other.velocity - relativeMass * vPosDot/posDistance * posDifference;

		velocity = myNewVelocity;
		updateRotation();
		transform.position = lastPos;


		other.velocity = otherNewVelocity;
		other.updateRotation();
		other.transform.position = other.lastPos;


	}

}
