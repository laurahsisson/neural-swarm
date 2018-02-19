using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

using NetMQ;
using UnityEngine;

public class FlockControl : MonoBehaviour {
	public void Update() {
		Debug.Log("HELLO");
	}
//
//	private readonly Thread _listenerWorker;
//
//	private bool _listenerCancelled;
//
//	public delegate void MessageDelegate(string message);
//
//	private readonly MessageDelegate _messageDelegate;
//
//	private readonly ConcurrentQueue<string> _messageQueue = new ConcurrentQueue<string>();
//
//	private void ListenerWork()
//	{
//		AsyncIO.ForceDotNet.Force();
//		using (var subSocket = new SubscriberSocket())
//		{
//			subSocket.Options.ReceiveHighWatermark = 1000;
//			subSocket.Connect("tcp://localhost:12345");
//			subSocket.Subscribe("");
//			while (!_listenerCancelled)
//			{
//				string frameString;
//				if (!subSocket.TryReceiveFrameString(out frameString)) continue;
//				Debug.Log(frameString);
//				_messageQueue.Enqueue(frameString);
//			}
//			subSocket.Close();
//		}
//		NetMQConfig.Cleanup();
//	}
//
//	public void Update()
//	{
//		while (!_messageQueue.IsEmpty)
//		{
//			string message;
//			if (_messageQueue.TryDequeue(out message))
//			{
//				_messageDelegate(message);
//			}
//			else
//			{
//				break;
//			}
//		}
//	}
//
//	public NetMqListener(MessageDelegate messageDelegate)
//	{
//		_messageDelegate = messageDelegate;
//		_listenerWorker = new Thread(ListenerWork);
//	}
//
//	public void Start()
//	{
//		_listenerCancelled = false;
//		_listenerWorker.Start();
//	}
//
//	public void Stop()
//	{
//		_listenerCancelled = true;
//		_listenerWorker.Join();
//	}
}
