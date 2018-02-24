using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class FlockControl : MonoBehaviour {
	public GameObject goalPrefab;
	public GameObject birdPrefab;
	public GameObject background;

	private GameObject goal;
	private BirdControl[] birdControls;

	private readonly int NUM_BIRDS = 5;
	private readonly float ROOM_WIDTH = 50;
	private readonly float ROOM_HEIGHT = 40;

	[System.Serializable]
	struct WorldState {
		public BirdControl.Bird[] birds;
		public Vector2 goalPosition;
	}

	public void Start () {
		// Set the background based on room settings
		background.transform.position = new Vector3(ROOM_WIDTH/2,ROOM_HEIGHT/2,5);
		background.transform.localScale = new Vector3(ROOM_WIDTH+5,ROOM_HEIGHT+5,1);
		background.GetComponent<Renderer>().material.color = Color.black;

		// Generate goal's position
		Vector2 goalPosition = randomPosition();
		goal = Instantiate<GameObject>(goalPrefab);
		goal.transform.position = goalPosition;

		// Generate birds
		birdControls = new BirdControl[NUM_BIRDS];
		for (int i = 0; i < NUM_BIRDS; i++) {
			BirdControl bird = Instantiate<GameObject>(birdPrefab).GetComponent<BirdControl>();
			bird.transform.position = randomPosition();
			bird.Setup(this,1,5,i);
			bird.GetComponent<Renderer>().material.color = new Color(Random.Range(.5f,1f),Random.Range(.5f,1f),Random.Range(.5f,1f));
			birdControls[i] = bird;
		}

//		foreach (BirdControl bird in birdControls) {
//			Vector2 speed = new Vector2(Random.Range(-1f,1f),Random.Range(-1f,1f)).normalized;
//			bird.SetAcceleration(speed*5);
//		}
	}
	
	public string Serialize() {
		BirdControl.Bird[] birds = new BirdControl.Bird[NUM_BIRDS];
		for (int i = 0; i < NUM_BIRDS; i++) {
			birds[i] = birdControls[i].ToStruct();
		}
		WorldState ws = new WorldState();
		ws.birds = birds;
		ws.goalPosition = (Vector2)goal.transform.position;
		return JsonUtility.ToJson(ws);
	}

	public void Deserialize(string rawCommand) {
		// Expected format is a Python list of lists of two numbers ie [[1,2],[3,4]]
		// Removed the outermost brackets
		rawCommand = rawCommand.Substring(1,rawCommand.Length-2);
		// If we know for sure we had more than 1 bird, we could split by '],[' but instead we must split by '['
		string[] rawSplits = rawCommand.Split(new char[]{'['});

		for (int i = 0; i < rawSplits.Length; i++) {
			// The first string will always be "", so the bird we are on is i-1
			if (rawSplits[i] == "") {
				continue;
			}
			// Splitting by ']' and getting the first element in that split gives us 1,2
			// So split that by ',' to get the raw numbers
			string[] xy = rawSplits[i].Split(new char[]{']'})[0].Split(new char[]{','});
			Vector2 accel = new Vector2(float.Parse(xy[0]),float.Parse(xy[1]));
			birdControls[i-1].SetAcceleration(accel);
		}
	}


	public Rect GetWorldBound() {
		return new Rect(0,0,ROOM_WIDTH,ROOM_HEIGHT);
	}

	private Vector3 randomPosition() {
		return new Vector3(Random.Range(0,ROOM_WIDTH),Random.Range(0,ROOM_HEIGHT),0);
	}
}
