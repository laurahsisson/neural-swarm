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
	public ScoreControl scoreControl;

	public delegate void RandomDelegate();

	public GameObject[] staticWalls;

	private StatsControl statsControl;
	private UIControl uiControl;

	private readonly float ROOM_WIDTH = 80;
	private readonly float ROOM_HEIGHT = 60;

	private BirdControl[] birdControls;
	private readonly int NUM_BIRDS = 50;
	private Vector2[] startPositions;

	private readonly float MIN_SIZE = .8f;
	private readonly float MAX_SIZE = 1.1f;

	private readonly float MIN_SPEED = 7f;
	private readonly float MAX_SPEED = 9f;

	private GameObject[] walls;
	private readonly int NUM_RANDOM_WALLS = 10;
	private readonly float WALL_MAX_WIDTH = 10f;
	private readonly float WALL_MIN_WIDTH = 2f;
	// Walls are constrained to have fixed area, so width = area/height
	private readonly float WALL_MAX_AREA = 12f;
	private readonly float WALL_MIN_AREA = 8f;

	private readonly float SIMULATION_SPEED = 2f;
	private readonly float FRAMES_PER_SECOND = 30f;

	private GameObject goal;
	private float startTime = 0;
	private bool hasReceivedStart = false;
	private int generation = 0;
	private int reachedGoal;
	private readonly float MAX_TIME = 20;


	public struct UnityState {
		public BirdControl[] birds;
		public GameObject[] walls;
		public GameObject goal;
		public float roomWidth;
		public float roomHeight;
		public float maxSize;
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
			
			decisionControl.InitializeModel(NUM_BIRDS, randomizePositions);
			Destroy(GameObject.FindGameObjectWithTag("ClientServer"));
		}
		Application.targetFrameRate = (int) (FRAMES_PER_SECOND*SIMULATION_SPEED);
		Time.timeScale = SIMULATION_SPEED;
		Application.runInBackground = true;

		// Set the background based on room settings
		background.transform.position = new Vector3(ROOM_WIDTH / 2, ROOM_HEIGHT / 2, 5);
		Camera.main.transform.position = new Vector3(ROOM_WIDTH / 2, ROOM_HEIGHT / 2, -10);
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

		walls = new GameObject[NUM_RANDOM_WALLS];
		for (int i = 0; i < walls.Length; i++) {
			walls [i] = Instantiate<GameObject>(wallPrefab);
		}
			
		startPositions = new Vector2[NUM_BIRDS];
		randomizePositions();
		resetSimulation();
	}

	public void IncrementGoal() {
		reachedGoal++;
		if (reachedGoal == NUM_BIRDS) {
			endSimulation();
		}
	}

	private void endSimulation() {
		StatsControl.GenerationStats gs = statsControl.CalculateStats();
		if (!callingPython) {
			decisionControl.EndGeneration(scoreControl.GetScore(gs));
		}
		resetSimulation();
	}

	private void randomizePositions() {
		for (int i = 0; i < NUM_RANDOM_WALLS; i++) {
			float width = Random.Range(WALL_MIN_WIDTH, WALL_MAX_WIDTH);
			float area = Random.Range(WALL_MIN_AREA, WALL_MAX_AREA);
			walls [i].transform.localScale = new Vector3(width, area / width, 1f);
			findPlacement(walls[i]);
		}

		findPlacement(goal);

		for (int i = 0; i < NUM_BIRDS; i++) {
			BirdControl bird = birdControls [i];
			float size = Random.Range(MIN_SIZE, MAX_SIZE);
			float speed = Random.Range(MIN_SPEED, MAX_SPEED);
			bird.Setup(size, speed, i, NUM_BIRDS, walls.Length);

			bird.SetForce(new Vector2(Random.value-.5f,Random.value-.5f).normalized*bird.Speed);

			bird.GetComponent<Renderer>().material.color = new Color(Random.Range(.5f, 1f), Random.Range(.5f, 1f), Random.Range(.5f, 1f));

			findPlacement(bird.gameObject);
			startPositions[i] = bird.transform.position;
		}
	}

	// Resets the walls, goal and all birds.
	private void resetSimulation() {
		reachedGoal = 0;
		for (int i = 0; i < NUM_BIRDS; i++) {
			BirdControl bird = birdControls [i];
			bird.transform.position = startPositions[i];
			bird.Reset();
		}

		scoreControl.Setup(NUM_BIRDS);
		statsControl.Setup(NUM_BIRDS, MAX_TIME);
		uiControl.AwaitingText();
		if (!callingPython) {
			decisionControl.StartGeneration(buildUnityState());
		}
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
		us.maxSize = MAX_SIZE;
		return us;
	}

	public string Serialize() {
		BirdControl.Bird[] birds = new BirdControl.Bird[NUM_BIRDS];
		for (int i = 0; i < NUM_BIRDS; i++) {
			birds [i] = birdControls [i].ToStruct();
		}
		RectCorners[] wallStates = new RectCorners[walls.Length + staticWalls.Length];
		for (int i = 0; i < walls.Length; i++) {
			wallStates [i] = new RectCorners(walls [i].GetComponent<RectTransform>());
		}
		for (int j = 0; j < staticWalls.Length; j++) {
			wallStates [j + walls.Length] = new RectCorners(staticWalls [j].GetComponent<RectTransform>());
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
		string[] generationListsOfListsSplit = rawCommand.Split(new char[]{ ',' });
		int rg = int.Parse(generationListsOfListsSplit [0]);
		if (rg != generation) {
			// Do not accept commands from other generations
			return;
		}
		try {
			rawCommand = rawCommand.Substring(generationListsOfListsSplit [0].Length + 2, rawCommand.Length - 3);		
		} catch (System.Exception ex) {
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

	private void findPlacement(GameObject go) {
		Collider2D cld = go.GetComponent<Collider2D>();
		Vector3 origScale = go.transform.localScale;
		// Scale us up by a buffer factor
		go.transform.localScale *= 2;
		ContactFilter2D cf = new ContactFilter2D();
		cf.useTriggers = true;
		// We don't actually care about collecting other colliders, just want to count
		Collider2D[] others = new Collider2D[1];

		bool hitOthers = true;
		while (hitOthers) {
			go.transform.rotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));
			go.transform.position = randomPosition();
			int hit = cld.OverlapCollider(cf,others);
			hitOthers = hit != 0;
		}
		go.transform.localScale = origScale;
	}

	private Vector3 randomPosition() {
		return new Vector3(Random.Range(0, ROOM_WIDTH), Random.Range(0, ROOM_HEIGHT), 0);
	}

	private void setupDistCache() {
		for (int i = 0; i < birdControls.Length; i++) {
			for (int j = i+1; j < birdControls.Length; j++) {
				BirdControl b1 = birdControls [i];
				BirdControl b2 = birdControls [j];
				ColliderDistance2D cd = b1.GetComponent<Collider2D>().Distance(b2.GetComponent<Collider2D>());
				Vector2 delta = (cd.pointB - cd.pointA);
				b1.SetDistance(b2,delta);
				b2.SetDistance(b1,-1*delta);
			}
		}

		for (int i = 0; i < birdControls.Length; i++) {
			for (int j = 0; j < walls.Length; j++) {
				BirdControl b1 = birdControls [i];
				GameObject wall = walls[j];
				ColliderDistance2D cd = b1.GetComponent<Collider2D>().Distance(wall.GetComponent<Collider2D>());
				Vector2 delta = (cd.pointB - cd.pointA);
				b1.SetWallDist(j,delta);
			}	
		}
	}


	private void Update() {
		if (!hasReceivedStart && callingPython) {
			startTime = Time.time;
			return;
		}
		if (!callingPython) {
			setupDistCache();

			float updateStart = Time.realtimeSinceStartup;
			UnityState us = buildUnityState();
			scoreControl.SetScore(us);
			Vector2[] forces = decisionControl.MakeDecisions(us);
			Debug.Assert(forces.Length == birdControls.Length);
			for (int i = 0; i < forces.Length; i++) {
				birdControls [i].SetForce(forces [i]);
			}
//			print(Time.realtimeSinceStartup-updateStart);
		}


		float remainTime = MAX_TIME - (Time.time - startTime);
		uiControl.SetTime(generation, remainTime);

		if (remainTime < 0) {
			endSimulation();
		}
	}
}
