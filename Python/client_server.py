#! /usr/local/bin/python3
import zmq
import time
import random
context = zmq.Context()
socket_listen = context.socket(zmq.REQ)
socket_listen.connect("tcp://localhost:12346")

TIMEOUT = 10000

socket_reply = context.socket(zmq.PUB)
socket_reply.bind("tcp://*:12345")


while True:
    socket_listen.send_string("request")
    poller = zmq.Poller()
    poller.register(socket_listen, zmq.POLLIN)
    evt = dict(poller.poll(TIMEOUT))
    if evt:
        if evt.get(socket_listen) == zmq.POLLIN:
            response = socket_listen.recv(zmq.NOBLOCK)
            print(response)
            message = str(random.uniform(-1.0, 1.0)) + " " + str(random.uniform(-1.0, 1.0)) + " " + str(random.uniform(-1.0, 1.0))
            socket_reply.send_string(message)
            print(message)
            time.sleep(1)
            
            continue
    time.sleep(0.5)
    socket_listen.close()
    socket_listen = context.socket_listen(zmq.REQ)
    socket_listen.connect("tcp://localhost:12346")
