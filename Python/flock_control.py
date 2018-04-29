import numpy as np
from timeit import default_timer as timer
from world_state import WorldState
from line_bird import LineBird
from force_bird import ForceBird
from multiprocessing import Pool


generation = 1

class FlockControl:
    def __init__(self,num_birds):
        # Eventually, when we make each bird a class we can initialize an array here
        self.num_birds = num_birds
        self.decision_time = 0
        self.pool = Pool()         


    def make_decisions(self, unity_state):
        global generation
        new_generation = unity_state["generation"]
        if new_generation != generation:
            self.end_generation() 
            generation = new_generation
        # Generate the world state
        ws = WorldState(unity_state)

        bird_control = ForceBird(ws)
        # Select the bird control type we will be using
        if bird_control.needs_grid():
            start = timer()
            ws.make_grid(bird_control.get_grid_step())
            print("Grid in :",timer()-start, "seconds")  

        start = timer()


        bird_control.prepare_step()
        if bird_control.parallelizable():
            bird_numbers = range(len(ws.birds))
            parallel_inputs = [(ws,bird_control,bird_number) for bird_number in bird_numbers]
            decisions = self.pool.starmap(parallel_helper, parallel_inputs)
        else:
            decisions = [[0,0]]*len(ws.birds)
            for b, bird in enumerate(ws.birds):
                if not bird["active"]:
                    continue
                decisions[b] = bird_control.make_decision(b)
        bird_control.end_step()
        self.decision_time += timer() - start

        # print("Decisions in :",timer()-start, "seconds")  


        return (unity_state["generation"],decisions)

    def end_generation(self):
        print(self.decision_time)
        self.decision_time = 0

def parallel_helper(ws,bird_control,bird_number):
    if ws.birds[bird_number]["active"]:
        return bird_control.make_decision(bird_number)
    else:
        return [0,0]
