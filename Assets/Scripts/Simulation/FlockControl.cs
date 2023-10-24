using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlockControl : MonoBehaviour {
	public GameObject goalPrefab;
	public GameObject birdPrefab;
	public GameObject wallPrefab;
	public GameObject background;

	public DecisionControl decisionControl;
	public ScoreControl scoreControl;

	public delegate void RandomDelegate(int x);

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

	public static float SIMULATION_SPEED = 1f;
	private readonly float FRAMES_PER_SECOND = 60f;

	private GameObject goal;
	private float startTime = 0;
	private bool hasReceivedStart = false;
	private int generation = 0;
	private int reachedGoal;
	private readonly float MAX_TIME = 30;

	private Dictionary<int, MapState> maps = new Dictionary<int, MapState>();

	// Sent to other classes to give them an idea of what the world looks like
	public struct UnityState {
		public BirdControl[] birds;
		public GameObject[] walls;
		public GameObject goal;
		public float roomWidth;
		public float roomHeight;
		public float maxSize;
	}

	// Used by this class to generate maps and go back to them on command
	public struct MapState {
		public BirdState[] birds;
		public WallState[] walls;
		public Vector2 goal;
	}

	public struct BirdState {
		public Vector3 startPos;
		public Vector2 velocity;
		public float size;
		public float speed;
		public Color color;
	}

	public struct WallState {
		public Vector2 postion;
		public Vector3 scale;
		public Quaternion rotation;
	}


	public void Start() {
		Application.targetFrameRate = (int)(FRAMES_PER_SECOND * SIMULATION_SPEED);
		Time.timeScale = SIMULATION_SPEED;
		Application.runInBackground = true;

		decisionControl.InitializeModel(NUM_BIRDS, randomizePositions);

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
		randomizePositions(0);
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
		decisionControl.EndGeneration(scoreControl.GetScore(gs));
		resetSimulation();
	}

	private void randomizePositions(int m) {
		if (maps.ContainsKey(m)) {
			loadFromMapState(maps[m]);
			return;
		}

		MapState ms = new MapState();

		ms.walls = new WallState[NUM_RANDOM_WALLS];
		for (int i = 0; i < NUM_RANDOM_WALLS; i++) {
			WallState ws = new WallState();

			float width = Random.Range(WALL_MIN_WIDTH, WALL_MAX_WIDTH);
			float area = Random.Range(WALL_MIN_AREA, WALL_MAX_AREA);
			walls [i].transform.localScale = new Vector3(width, area / width, 1f);
			findPlacement(walls [i]);

			ws.postion = walls [i].transform.position;
			ws.scale = walls [i].transform.localScale;
			ws.rotation = walls [i].transform.rotation;
			ms.walls [i] = ws;
		}

		findPlacement(goal);
		ms.goal = goal.transform.position;

		ms.birds = new BirdState[NUM_BIRDS];
		for (int i = 0; i < NUM_BIRDS; i++) {
			BirdControl bird = birdControls [i];
			findPlacement(bird.gameObject);

			BirdState bs = new BirdState();
			bs.size = Random.Range(MIN_SIZE, MAX_SIZE);
			bs.speed = Random.Range(MIN_SPEED, MAX_SPEED);
			bs.color = new Color(Random.Range(.5f, 1f), Random.Range(.5f, 1f), Random.Range(.5f, 1f));
			bs.startPos = bird.transform.position;
			bs.velocity = new Vector2(Random.value - .5f, Random.value - .5f).normalized * bird.Speed;
			ms.birds [i] = bs;

			bird.Setup(bs, i, NUM_BIRDS, walls.Length);

			startPositions [i] = bird.transform.position;
		}

		maps[m] = ms;
	}

	private void loadFromMapState(MapState ms) {
		for (int i = 0; i < NUM_RANDOM_WALLS; i++) {
			walls[i].transform.position = ms.walls[i].postion;
			walls[i].transform.localScale = ms.walls[i].scale;
			walls[i].transform.rotation = ms.walls[i].rotation;
		}

		goal.transform.position = ms.goal;

		for (int i = 0; i < ms.birds.Length; i++) {
			birdControls[i].Setup(ms.birds[i],i,NUM_BIRDS,walls.Length);
		}

	}

	// Resets the walls, goal and all birds.
	private void resetSimulation() {
		reachedGoal = 0;
		for (int i = 0; i < NUM_BIRDS; i++) {
			BirdControl bird = birdControls [i];
			bird.transform.position = startPositions [i];
			bird.Reset();
		}

		scoreControl.Setup(NUM_BIRDS);

		statsControl.Setup(NUM_BIRDS, MAX_TIME);

		uiControl.AwaitingText();

		decisionControl.StartGeneration(buildUnityState());

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

	// Moves and rotates the vector randomly around on the grid until a valid placement is found.
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
			int hit = cld.OverlapCollider(cf, others);
			hitOthers = hit != 0;
		}
		go.transform.localScale = origScale;
	}

	private Vector3 randomPosition() {
		return new Vector3(Random.Range(0, ROOM_WIDTH), Random.Range(0, ROOM_HEIGHT), 0);
	}

	private void setupDistCache() {
		for (int i = 0; i < birdControls.Length; i++) {
			for (int j = i + 1; j < birdControls.Length; j++) {
				BirdControl b1 = birdControls [i];
				BirdControl b2 = birdControls [j];
				ColliderDistance2D cd = b1.GetComponent<Collider2D>().Distance(b2.GetComponent<Collider2D>());
				Vector2 delta = (cd.pointB - cd.pointA);
				b1.SetDistance(b2, delta);
				b2.SetDistance(b1, -1 * delta);
			}
		}

		for (int i = 0; i < birdControls.Length; i++) {
			for (int j = 0; j < walls.Length; j++) {
				BirdControl b1 = birdControls [i];
				GameObject wall = walls [j];
				ColliderDistance2D cd = b1.GetComponent<Collider2D>().Distance(wall.GetComponent<Collider2D>());
				Vector2 delta = (cd.pointB - cd.pointA);
				b1.SetWallDist(j, delta);
			}	
		}
	}

	public Rect GetWorldBound() {
		return new Rect(0, 0, ROOM_WIDTH, ROOM_HEIGHT);
	}

	private void Update() {
		setupDistCache();

		UnityState us = buildUnityState();
		scoreControl.SetScore(us);

		Vector2[] forces = decisionControl.MakeDecisions(us);
		Debug.Assert(forces.Length == birdControls.Length);
		for (int i = 0; i < forces.Length; i++) {
			birdControls [i].SetForce(forces [i]);
		}

		float remainTime = MAX_TIME - (Time.time - startTime);
		uiControl.SetTime(generation, remainTime);

		if (remainTime < 0) {
			endSimulation();
		}
	}
}
