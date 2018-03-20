import numpy as np
from unity_helper import xy_dict_to_vector, corner_struct_to_tuples

class BaseBird:
    def __init__(self,ws):
        self.ws = ws

    def make_decision(bird_number):
        return [0,0]

# Given a bird, returns a vector point from the bird to goal with proper size
def point_to_goal(bird,goal_pos):
    bird_pos = xy_dict_to_vector(bird["position"])
    poss_diff = goal_pos-bird_pos
    length = np.linalg.norm(poss_diff)
    return list(poss_diff/length*bird["speed"])

