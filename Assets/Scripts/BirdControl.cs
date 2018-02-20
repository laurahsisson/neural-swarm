using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BirdControl : MonoBehaviour {
	private float size;
	private float speed;

	public void Setup(float size, float speed) {
		this.size = size;
		this.speed = speed;
		this.transform.localScale*=size;

	}
	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
