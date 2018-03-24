import numpy as np
from timeit import default_timer as timer
from world_state import WorldState
from line_bird import LineBird
from force_bird import ForceBird


class FlockControl:
    def __init__(self,num_birds):
        # Eventually, when we make each bird a class we can initialize an array here
        self.num_birds = num_birds

    def make_decisions(self, unity_state):
        # Generate the world state
        ws = WorldState(unity_state)

        # Select the bird control type we will be using
        bird_control = ForceBird(ws)
        if bird_control.needs_grid():
            start = timer()
            ws.make_grid(bird_control.get_grid_step())
            print("Grid in :",timer()-start, "seconds")  

        start = timer()
        decisions = [[0,0]]*len(ws.birds)
        for b, bird in enumerate(ws.birds):
            if not bird["active"]:
                continue
            decisions[b] = bird_control.make_decision(b)
        print("Decisions in :",timer()-start, "seconds")  


        return (unity_state["generation"],decisions)
