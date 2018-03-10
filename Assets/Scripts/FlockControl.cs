using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class FlockControl : MonoBehaviour {
	public GameObject goalPrefab;
	public GameObject birdPrefab;
	public GameObject wallPrefab;
	public GameObject background;

	private StatsControl statsControl;
	private UIControl uiControl;

	private readonly float ROOM_WIDTH = 50;
	private readonly float ROOM_HEIGHT = 40;

	private BirdControl[] birdControls;
	private readonly int NUM_BIRDS = 50;

	private readonly float MIN_SIZE = .75f;
	private readonly float MAX_SIZE = 1.3f;

	private readonly float MIN_SPEED = 4f;
	private readonly float MAX_SPEED = 6f;

	private GameObject[] walls;
	private readonly int NUM_WALLS = 5;
	private readonly float WALL_MAX_WIDTH = 10f;
	private readonly float WALL_MIN_WIDTH = 2f;
	// Walls are constrained to have fixed area, so width = area/height
	private readonly float WALL_MAX_AREA = 12f;
	private readonly float WALL_MIN_AREA = 8f;

	private GameObject goal;
	private float startTime = 0;
	private bool hasReceivedStart = false;
	private int generation = 0;
	private int reachedGoal;
	private readonly float MAX_TIME = 25f;


	[System.Serializable]
	struct WorldState {
		public int generation;
		public BirdControl.Bird[] birds;
		public WallState[] walls;
		public Vector2 goalPosition;
	}

	[System.Serializable]
	struct WallState {
		public Vector2 topLeft;
		public Vector2 topRight;
		public Vector2 bottomLeft;
		public Vector2 bottomRight;
	}

	public void Start() {
		// Set the background based on room settings
		background.transform.position = new Vector3(ROOM_WIDTH / 2, ROOM_HEIGHT / 2, 5);
		background.transform.localScale = new Vector3(ROOM_WIDTH + 5, ROOM_HEIGHT + 5, 1);
		background.GetComponent<Renderer>().material.color = Color.black;

		goal = Instantiate<GameObject>(goalPrefab);
		statsControl = FindObjectOfType<StatsControl>();
		uiControl = FindObjectOfType<UIControl>();

		// Generate birds
		birdControls = new BirdControl[NUM_BIRDS];
		for (int i = 0; i < NUM_BIRDS; i++) {
			BirdControl bird = Instantiate<GameObject>(birdPrefab).GetComponent<BirdControl>();
			birdControls [i] = bird;
		}

		walls = new GameObject[NUM_WALLS];
		for (int i = 0; i < NUM_WALLS; i++) {
			walls [i] = Instantiate<GameObject>(wallPrefab);
			walls[i].AddComponent<RectTransform>();
			walls[i].GetComponent<RectTransform>().sizeDelta = new Vector2(1,1);
		}

		resetBirds();
	}
		
	public void IncrementGoal() {
		reachedGoal++;
		if (reachedGoal == NUM_BIRDS) {
			statsControl.PrintStats();
			resetBirds();
		}
	}

	private void resetBirds() {
		goal.transform.position = randomPosition();

		reachedGoal = 0;
		for (int i = 0; i < NUM_BIRDS; i++) {
			BirdControl bird = birdControls [i];
			bird.transform.position = randomPosition();
			float size = Random.Range(MIN_SIZE, MAX_SIZE);
			float speed = Random.Range(MIN_SPEED, MAX_SPEED);
			bird.Setup(size, speed, i);
			bird.GetComponent<Renderer>().material.color = new Color(Random.Range(.5f, 1f), Random.Range(.5f, 1f), Random.Range(.5f, 1f));
		}

		for (int i = 0; i < NUM_WALLS; i++) {
			float width = Random.Range(WALL_MIN_WIDTH, WALL_MAX_WIDTH);
			float area = Random.Range(WALL_MIN_AREA, WALL_MAX_AREA);
			walls [i].transform.localScale = new Vector3(width, area / width, 1f);
			walls [i].transform.position = randomPosition();
			walls [i].transform.rotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));
//			RectTransform rectTransform = walls[i].GetComponent<RectTransform>();
		}

		statsControl.Setup(NUM_BIRDS, MAX_TIME);
		uiControl.AwaitingText();
		startTime = Time.time;
		hasReceivedStart = false;
		generation++;
	}

	public string Serialize() {
		BirdControl.Bird[] birds = new BirdControl.Bird[NUM_BIRDS];
		for (int i = 0; i < NUM_BIRDS; i++) {
			birds [i] = birdControls [i].ToStruct();
		}
		WallState[] wallStates = new WallState[walls.Length];
		for (int i = 0; i < NUM_WALLS; i++) {
			wallStates[i] = wallToWallState(walls[i]);
		}
		WorldState ws = new WorldState();
		ws.generation = generation;
		ws.birds = birds;
		ws.goalPosition = (Vector2)goal.transform.position;
		ws.walls = wallStates;
		return JsonUtility.ToJson(ws);
	}

	public void Deserialize(string rawCommand) {
		// Expected format is the generation follow by a Python list of lists of two numbers ie: [100,[[1,2],[3,4]]]
		rawCommand = rawCommand.Substring(1, rawCommand.Length - 2);
		string[] generationListsOfListsSplit = rawCommand.Split(new char[]{','});
		int rg =  int.Parse(generationListsOfListsSplit[0]);
		if (rg != generation) {
			// Do not accept commands from other generations
			return;
		}
		rawCommand = rawCommand.Substring(generationListsOfListsSplit[0].Length+2,rawCommand.Length-3);		

		// At this point we have [[1,2],[3,4]] so we must now remove the outermost brackets
		rawCommand = rawCommand.Substring(1, rawCommand.Length - 2);
		// If we know for sure we had more than 1 bird, we could split by '],[' but instead we must split by '[' 
		// in the case we have just one, we receive [100,[[1,2]]]
		string[] rawSplits = rawCommand.Split(new char[]{ '[' });

		for (int i = 0; i < rawSplits.Length; i++) {
			// The first string will always be "", so the bird we are on is i-1
			if (rawSplits [i] == "") {
				continue;
			}
			// Splitting by ']' and getting the first element in that split gives us 1,2
			// So split that by ',' to get the raw numbers
			string[] xy = rawSplits [i].Split(new char[]{ ']' }) [0].Split(new char[]{ ',' });
			Vector2 accel = new Vector2(float.Parse(xy [0]), float.Parse(xy [1]));
			birdControls [i - 1].SetAcceleration(accel);
		}
		hasReceivedStart = true;
	}


	public Rect GetWorldBound() {
		return new Rect(0, 0, ROOM_WIDTH, ROOM_HEIGHT);
	}

	private Vector3 randomPosition() {
		return new Vector3(Random.Range(0, ROOM_WIDTH), Random.Range(0, ROOM_HEIGHT), 0);
	}

	private void Update() {
		if (!hasReceivedStart) {
			startTime = Time.time;
			return;
		}
		float remainTime = MAX_TIME - (Time.time - startTime);
		uiControl.SetTime(remainTime);

		if (remainTime < 0) {
			statsControl.PrintStats();
			resetBirds();

		}
	}

	private WallState wallToWallState(GameObject wall) {
		Vector3[] corners = new Vector3[4];
		wall.GetComponent<RectTransform>().GetWorldCorners(corners);
		WallState ws = new WallState();
		ws.topLeft = corners[0];
		ws.topRight = corners[1];
		ws.bottomLeft = corners[2];
		ws.bottomRight = corners[3];
		return ws;
	}
}
