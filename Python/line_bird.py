import numpy as np
from unity_helper import xy_dict_to_vector, corner_struct_to_tuples

def make_decision(ws,bird_number):
    return point_to_goal(ws.birds[bird_number],ws.goal_pos)

# Given a bird, returns a vector point from the bird to goal with proper size
def point_to_goal(bird,goal_pos):
    bird_pos = xy_dict_to_vector(bird["position"])
    poss_diff = goal_pos-bird_pos
    length = np.linalg.norm(poss_diff)
    return list(poss_diff/length*bird["speed"])

