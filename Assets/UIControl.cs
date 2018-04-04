using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIControl : MonoBehaviour {
	private Text timeText;
	Queue<float> framesPerSecond = new Queue<float>();
	int maxFramesRecorded = 30;

	public void AwaitingText() {
		timeText.text = "Waiting for server response";

	}

	public void SetTime(int generation, float time) {
		framesPerSecond.Enqueue(1 / Time.deltaTime);
		if (framesPerSecond.Count > maxFramesRecorded) {
			framesPerSecond.Dequeue();
		}
		float[] fps = framesPerSecond.ToArray();
		float sum = 0;
		foreach (float t in fps) {
			sum += t;
		}

		timeText.text = generation + "\t\t\t\t" + time.ToString("N2") + "\t\t\t\t" + ((int)sum / fps.Length);
	}

	private void Start() {
		timeText = GetComponent<Text>();
	}

}
