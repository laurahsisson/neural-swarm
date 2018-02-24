# neural-swarm
Cyber Physical Systems project simulating swarm of agents cooperating towards a common goal.

Instructions for running:
First make sure you have downloaded Unity.
Now make sure you have numpy and zmq installed. Check by typing pip freeze 
Otherwise install using pip. For example: pip install numpy

1. Download the repository and navigate to: neural-swarm-master\Assets
    - Double click main_2.unity, a Unity scene. This should open in Unity. 
2. Under the "Scene" tab, click "2D" to see the simulation easier. 
3. Run the scene by clicking the play button in the top center part of the screen. 
4. The Scene will run, but nothing will happen. So navigate to: neural-swarm-master\Python 
    - Run client_server.py 
5. The "birds" should now move towards the "goal"

Inspiration for network code from [Unity-ZeroMQ-Example](https://github.com/valkjsaaa/Unity-ZeroMQ-Example)
