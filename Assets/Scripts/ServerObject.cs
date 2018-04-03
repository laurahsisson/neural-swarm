﻿using System.Diagnostics;
using System.Threading;
using NetMQ;
using NetMQ.Sockets;
using UnityEngine;

public class NetMqPublisher {
	private readonly Thread _listenerWorker;

	private bool _listenerCancelled;

	public delegate string MessageDelegate(string message);

	private readonly MessageDelegate _messageDelegate;

	private readonly Stopwatch _contactWatch;

	private const long ContactThreshold = 1000;

	public bool Connected;

	private void ListenerWork() {
		AsyncIO.ForceDotNet.Force();
		using (var server = new ResponseSocket()) {
			server.Bind("tcp://*:12346");

			while (!_listenerCancelled) {
				Connected = _contactWatch.ElapsedMilliseconds < ContactThreshold;
				string message;
				if (!server.TryReceiveFrameString(out message))
					continue;
				_contactWatch.Restart();
				var response = _messageDelegate(message);
				server.SendFrame(response);
			}
		}
		NetMQConfig.Cleanup();
	}

	public NetMqPublisher(MessageDelegate messageDelegate) {
		_messageDelegate = messageDelegate;
		_contactWatch = new Stopwatch();
		_contactWatch.Start();
		_listenerWorker = new Thread(ListenerWork);
	}

	public void Start() {
		_listenerCancelled = false;
		_listenerWorker.Start();
	}

	public void Stop() {
		_listenerCancelled = true;
		_listenerWorker.Join();
	}
}

public class ServerObject : MonoBehaviour {
	public FlockControl flockControl;
	public bool Connected;
	private NetMqPublisher _netMqPublisher;
	private string _response;

	private void Start() {
		Application.runInBackground = true;
		_netMqPublisher = new NetMqPublisher(HandleMessage);
		_netMqPublisher.Start();
	}

	private void Update() {
		var position = transform.position;
		_response = flockControl.Serialize();
		Connected = _netMqPublisher.Connected;
	}

	private string HandleMessage(string message) {
		// Not on main thread
		return _response;
	}

	private void OnDestroy() {
		if (!flockControl.callingPython) {
			return;
		}
		_netMqPublisher.Stop();
	}
}
