import numpy as np
from unity_helper import xy_dict_to_vector, corner_struct_to_tuples

class BaseBird:
    def __init__(self,ws):
        # WorldState
        self.ws = ws

    # How granular the grid is
    def get_grid_step(self):
        return .5

    # Called once before make_decision is called
    def prepare_step(self):
        pass

    # Called once after make_decision is finished for every bird
    def end_step(self):
        pass

    # Called once each step for every bird
    def make_decision(self,bird_number):
        return [0,0]

    # If True, flock_control will set up the grid for this bird
    def needs_grid(self):
        return True

# Given a bird, returns a vector pointing from the bird to the aim position with proper size
def aim_at_position(bird,aim_pos):
    bird_pos = xy_dict_to_vector(bird["position"])
    poss_diff = aim_pos-bird_pos
    length = np.linalg.norm(poss_diff)
    assert length != 0
    return list(poss_diff/length*bird["speed"])

