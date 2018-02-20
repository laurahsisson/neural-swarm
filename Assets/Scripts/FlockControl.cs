using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class FlockControl : MonoBehaviour {
	public GameObject goalPrefab;
	public GameObject birdPrefab;
	public GameObject background;

	private GameObject goal;
	private BirdControl[] birds;

	private readonly int NUM_BIRDS = 50;
	private readonly float ROOM_WIDTH = 50;
	private readonly float ROOM_HEIGHT = 40;

	[System.Serializable]
	struct Bird {
		public Vector2 position;
		public Vector2 velocity;
	}

	[System.Serializable]
	struct WorldState {
		public Bird[] birds;
		public Vector2 goalPosition;
		public WorldState(Bird[] birds, Vector2 goalPosition) {
			this.birds = birds;
			this.goalPosition = goalPosition;
		}
	}



	// Use this for initialization
	void Start () {
		// Reset the backgrounds each position based on room settings
		background.transform.position = new Vector3(ROOM_WIDTH/2,ROOM_HEIGHT/2,5);
		background.transform.localScale = new Vector3(ROOM_WIDTH+5,ROOM_HEIGHT+5,1);
		background.GetComponent<Renderer>().material.color = Color.black;
		// Generate goal's position
		Vector2 goalPosition = randomPosition();
		goal = Instantiate<GameObject>(goalPrefab);
		goal.transform.position = goalPosition;

		birds = new BirdControl[NUM_BIRDS];
		for (int i = 0; i < NUM_BIRDS; i++) {
			BirdControl bird = Instantiate<GameObject>(birdPrefab).GetComponent<BirdControl>();
			bird.transform.position = randomPosition();
			bird.Setup(this,1,5,i);
			bird.GetComponent<Renderer>().material.color = new Color(Random.value,Random.value,Random.value);
			birds[i] = bird;
		}

		foreach (BirdControl bird in birds) {
			Vector2 speed = new Vector2(Random.Range(-1f,1f),Random.Range(-1f,1f)).normalized;
			bird.SetAcceleration(speed*5);
		}



//		Bird b = new Bird();
//		b.position = Vector2.up;
//		b.velocity = Vector2.left;
//		Bird[] birds = new Bird[2];
//		birds[0] = b;
//
//		GameState gs = new GameState(birds);
//		Debug.Log(gs);
//		Debug.Log(JsonUtility.ToJson(gs));
//		//Debug.Log(JsonUtility.ToJson(gs));
	}
	
	// Update is called once per frame
	void Update () {

	}


	public Rect GetWorldBound() {
		return new Rect(0,0,ROOM_WIDTH,ROOM_HEIGHT);
	}

	private Vector3 randomPosition() {
		return new Vector3(Random.Range(0,ROOM_WIDTH),Random.Range(0,ROOM_HEIGHT),0);
	}
}
