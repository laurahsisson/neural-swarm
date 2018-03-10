import numpy as np

class FlockControl():
    def __init__(self,num_birds):
        # Eventually, when we make each bird a class we can initialize an array here
        self.num_birds = num_birds

    def make_decisions(self, world_state):
        birds = world_state["birds"]
        a = np.asarray([3,4])
        goal_pos = xy_dict_to_vector(world_state["goalPosition"])

        return (world_state["generation"],[point_to_goal(bird,goal_pos) for bird in birds])
        
        
        
def point_to_goal(bird,goal_pos):
    bird_pos = xy_dict_to_vector(bird["position"])
    poss_diff = goal_pos-bird_pos
    length = np.linalg.norm(poss_diff)
    return list(poss_diff/length*bird["speed"])

def xy_dict_to_vector(xy):
    return np.asarray([xy["x"],xy["y"]])