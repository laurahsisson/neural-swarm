using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class FlockControl : MonoBehaviour {

	[System.Serializable]
	struct Bird {
		public Vector2 position;
		public Vector2 velocity;
	}

	[System.Serializable]
	struct GameState {
		public Bird[] birds;
		public GameState(Bird[] birds) {
			this.birds = birds;
		}
	}

	// Use this for initialization
	void Start () {
		Bird b = new Bird();
		b.position = Vector2.up;
		b.velocity = Vector2.left;
		Bird[] birds = new Bird[2];
		birds[0] = b;

		GameState gs = new GameState(birds);
		Debug.Log(gs);
		Debug.Log(JsonUtility.ToJson(gs));
		//Debug.Log(JsonUtility.ToJson(gs));
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
