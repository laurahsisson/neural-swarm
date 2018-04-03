using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlockControl : MonoBehaviour {
	public GameObject goalPrefab;
	public GameObject birdPrefab;
	public GameObject wallPrefab;
	public GameObject background;

	public bool callingPython;
	public DecisionControl decisionControl;

	public GameObject[] staticWalls;

	private StatsControl statsControl;
	private UIControl uiControl;

	private readonly float ROOM_WIDTH = 50;
	private readonly float ROOM_HEIGHT = 40;

	private BirdControl[] birdControls;
	private readonly int NUM_BIRDS = 75;

	private readonly float MIN_SIZE = .8f;
	private readonly float MAX_SIZE = 1.1f;

	private readonly float MIN_SPEED = 4f;
	private readonly float MAX_SPEED = 6f;

	private GameObject[] walls;
	private readonly int NUM_RANDOM_WALLS = 5;
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


	public struct UnityState {
		public BirdControl[] birds;
		public GameObject[] walls;
		public GameObject goal;
		public float roomWidth;
		public float roomHeight;
	}

	[System.Serializable]
	private struct WorldState {
		public int generation;
		public BirdControl.Bird[] birds;
		public RectCorners[] walls;
		public Vector2 goalPosition;
		public float goalDiameter;
		public float roomWidth;
		public float roomHeight;
	}


	public void Start() {
		if (!callingPython) {
			decisionControl.InitializeModel();
		}

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

		walls = new GameObject[NUM_RANDOM_WALLS+4];
		for (int i = 0; i < walls.Length; i++) {
			walls [i] = Instantiate<GameObject>(wallPrefab);
		}
		walls[NUM_RANDOM_WALLS].transform.position = new Vector3(0,ROOM_HEIGHT/2);
		walls[NUM_RANDOM_WALLS].transform.localScale = new Vector3(1,ROOM_HEIGHT,1);

		walls[NUM_RANDOM_WALLS+1].transform.position = new Vector3(ROOM_WIDTH,ROOM_HEIGHT/2);
		walls[NUM_RANDOM_WALLS+1].transform.localScale = new Vector3(1,ROOM_HEIGHT,1);

		walls[NUM_RANDOM_WALLS+2].transform.position = new Vector3(ROOM_WIDTH/2,0);
		walls[NUM_RANDOM_WALLS+2].transform.localScale = new Vector3(ROOM_WIDTH,1,1);

		walls[NUM_RANDOM_WALLS+3].transform.position = new Vector3(ROOM_WIDTH/2,ROOM_HEIGHT);
		walls[NUM_RANDOM_WALLS+3].transform.localScale = new Vector3(ROOM_WIDTH,1,1);

		resetSimulation();
	}
		
	public void IncrementGoal() {
		reachedGoal++;
		if (reachedGoal == NUM_BIRDS) {
			statsControl.PrintStats();
			resetSimulation();
		}
	}

	// Resets the walls, goal and all birds. 
	private void resetSimulation() {
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

		for (int i = 0; i < NUM_RANDOM_WALLS; i++) {
			float width = Random.Range(WALL_MIN_WIDTH, WALL_MAX_WIDTH);
			float area = Random.Range(WALL_MIN_AREA, WALL_MAX_AREA);
			walls [i].transform.localScale = new Vector3(width, area / width, 1f);
			walls [i].transform.position = randomPosition();
			walls [i].transform.rotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));
		}


		Collider2D goalCollider = goal.GetComponent<Collider2D>();
		bool hasOverlap = true;
		while (hasOverlap) {
			hasOverlap = false;
			for (int i = 0; i < NUM_RANDOM_WALLS; i++) {
				ColliderDistance2D distance = goalCollider.Distance(walls[i].GetComponent<Collider2D>());
				if (distance.distance<0) {
					hasOverlap = true;
					goal.transform.position = randomPosition();
					break;
				}
			}
		}

		statsControl.Setup(NUM_BIRDS, MAX_TIME);
		uiControl.AwaitingText();
		startTime = Time.time;
		hasReceivedStart = false;
		generation++;
	}

	private UnityState buildUnityState() {
		UnityState us = new UnityState();
		us.birds = birdControls;
		us.walls = walls;
		us.goal = goal;
		us.roomHeight = ROOM_HEIGHT;
		us.roomWidth = ROOM_WIDTH;
		return us;
	}

	public string Serialize() {
		BirdControl.Bird[] birds = new BirdControl.Bird[NUM_BIRDS];
		for (int i = 0; i < NUM_BIRDS; i++) {
			birds [i] = birdControls [i].ToStruct();
		}
		RectCorners[] wallStates = new RectCorners[walls.Length+staticWalls.Length];
		for (int i = 0; i < walls.Length; i++) {
			wallStates[i] = new RectCorners(walls[i].GetComponent<RectTransform>());
		}
		for (int j = 0; j < staticWalls.Length; j++) {
			wallStates[j+walls.Length] =  new RectCorners(staticWalls[j].GetComponent<RectTransform>());
		}

		WorldState ws = new WorldState();
		ws.generation = generation;
		ws.birds = birds;
		ws.walls = wallStates;

		ws.goalPosition = (Vector2)goal.transform.position;
		ws.goalDiameter = goal.transform.localScale.x;

		ws.roomWidth = ROOM_WIDTH;
		ws.roomHeight = ROOM_HEIGHT;
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
		try {
			rawCommand = rawCommand.Substring(generationListsOfListsSplit[0].Length+2,rawCommand.Length-3);		
		} catch (System.Exception ex){
			Debug.Log(rawCommand);
			throw ex;
		}
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
			birdControls [i - 1].SetForce(accel);
		}
		hasReceivedStart = true;
	}


	public Rect GetWorldBound() {
		return new Rect(0, 0, ROOM_WIDTH, ROOM_HEIGHT);
	}

	private Vector3 randomPosition() {
		return new Vector3(Random.Range(0, ROOM_WIDTH), Random.Range(0, ROOM_HEIGHT), 0);
	}

	private Vector3 random2Position(int i){
		if (i < 10) {
			return new Vector3 (12, 25 - i + 2, 0);
		} else if (i < 20) {
			return new Vector3 (30, 25 - i + 2, 0);
		} else {
			return new Vector3 (12 + i + 2, 35, 0);
		}
	}

	private void Update() {
		if (!hasReceivedStart&&callingPython) {
			startTime = Time.time;
			return;
		}
		if (!callingPython){
			float startCalculations = Time.realtimeSinceStartup;
			UnityState us = buildUnityState();
			Vector2[] forces = decisionControl.MakeDecisions(us);
			Debug.Assert(forces.Length == birdControls.Length);
			for (int i = 0; i < forces.Length; i++) {
				birdControls[i].SetForce(forces[i]);
			}
			float totalTime = Time.realtimeSinceStartup-startCalculations;
			Debug.Log("Calculated decisions in " + totalTime.ToString("G") + " seconds.");
		}


		float remainTime = MAX_TIME - (Time.time - startTime);
		uiControl.SetTime(remainTime);

		if (remainTime < 0) {
			statsControl.PrintStats();
			resetSimulation();
		}
	}
}
