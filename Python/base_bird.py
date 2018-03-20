import numpy as np
from unity_helper import xy_dict_to_vector, corner_struct_to_tuples

class BaseBird:
    def __init__(self,ws):
        self.ws = ws

    def get_grid_step(self):
        return .1

    def make_decision(bird_number):
        return [0,0]

# Given a bird, returns a vector pointing from the bird to the aim position with proper size
def aim_at_position(bird,aim_pos):
    bird_pos = xy_dict_to_vector(bird["position"])
    poss_diff = aim_pos-bird_pos
    length = np.linalg.norm(poss_diff)
    return list(poss_diff/length*bird["speed"])

