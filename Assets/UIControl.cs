using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIControl : MonoBehaviour {
	private Text timeText;
	public void AwaitingText() {
		timeText.text = "Waiting for server response";
	}
	public void SetTime(float time) {
		timeText.text = time.ToString("N2");
	}

	private void Start() {
		timeText = GetComponent<Text>();
	}
}
