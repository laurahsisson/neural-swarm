using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SpeedControl : MonoBehaviour {
	public InputField input; 
	public InputField ok;
	public float speed;
	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		if (ok.text!="") {
			speed = float.Parse(input.text);
			FlockControl.SIMULATION_SPEED = speed;
			SceneManager.LoadScene(1);
		}
	}
}
