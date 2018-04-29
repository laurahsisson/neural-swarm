#! /usr/local/bin/python3
import zmq
import time
import random
import json
import flock_control as fc

# How long to wait after not receiving a message from Unity before closing and opening socket
TIMEOUT = 10*1000 # in milliseconds
# How often we poll
POLL_TIME = 1/30 # in seconds
# How long after timing out to wait before reopening socket
WAIT_TIME = 10*1 # in seconds
flock_control = None

# Listens in on the listening socket
def getEventFromSocket(socket_listen):
    socket_listen.send_string("request")
    poller = zmq.Poller()
    poller.register(socket_listen, zmq.POLLIN)
    evt = dict(poller.poll(TIMEOUT))
    return evt


# Parses information given by listening socket and passes information back along reply socket
def handleEvent(evt,socket_listen, socket_reply): 
    global flock_control
    if evt.get(socket_listen) != zmq.POLLIN:
        return False

    
    response_raw = socket_listen.recv(zmq.NOBLOCK)
    response = json.loads(response_raw.decode("utf-8"))
    if not flock_control:
        flock_control = fc.FlockControl(len(response["birds"]))
    command = flock_control.make_decisions(response)
    socket_reply.send_string(json.dumps(command))
    return True


def closeSocketAndWait(socket_listen,context):
    print("Timed out. Waiting " + str(WAIT_TIME) + " seconds then reopening socket.")
    socket_listen.close()
    time.sleep(WAIT_TIME)
    socket_listen = context.socket(zmq.REQ)
    socket_listen.connect("tcp://localhost:12346")
    return socket_listen

# Main loop handling timing of various socket operations
def main():
    context = zmq.Context()
    socket_listen = context.socket(zmq.REQ)
    socket_listen.connect("tcp://localhost:12346")

    socket_reply = context.socket(zmq.PUB)
    socket_reply.bind("tcp://*:12345")
    while True:
        evt = getEventFromSocket(socket_listen)
        if evt and handleEvent(evt,socket_listen,socket_reply):
            time.sleep(POLL_TIME)
            continue
        socket_listen = closeSocketAndWait(socket_listen,context)
main()