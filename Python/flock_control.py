import numpy as np

class FlockControl():
    def __init__(self,num_birds):
        # Eventually, when we make each bird a class we can initialize an array here
        self.num_birds = num_birds

    def make_decisions(self, world_state):
        
        birds = world_state["birds"]
        a = np.asarray([3,4])
        assert len(birds) == 1
        bird = birds[0]
        bird_pos = xy_dict_to_vector(bird["position"])
        goal_pos = xy_dict_to_vector(world_state["goalPosition"])
        print(goal_pos-bird_pos)
        return ""

def xy_dict_to_vector(xy):
    return np.asarray([xy["y"],xy["y"]])