import numpy as np
import shapely.geometry as sh


class FlockControl():
    def __init__(self,num_birds):
        # Eventually, when we make each bird a class we can initialize an array here
        self.num_birds = num_birds

    def make_decisions(self, world_state):
        step = 10

        birds = world_state["birds"]
        a = np.asarray([3,4])
        goal_pos = xy_dict_to_vector(world_state["goalPosition"])

        # print(world_state["roomWidth"],world_state["roomHeight"])
        walls = [wall_struct_to_tuple_list(wall_struct) for wall_struct in world_state["walls"]]

        goal_shape = sh.Point(goal_pos).buffer(world_state["goalDiameter"]/2)
        makeGrid(step,world_state["roomWidth"],world_state["roomHeight"],goal_shape)
        return (world_state["generation"],[point_to_goal(bird,goal_pos) for bird in birds])
        

def makeGrid(step,width,height,goal_shape):
    width_points = int(width/step)
    height_points = int(height/step)
    grid = [0]*width_points
    for x in range(width_points):
        grid[x] = [0]*height_points
        for y in range(height_points):
            grid[x][y] = i

            
        
def point_to_goal(bird,goal_pos):
    bird_pos = xy_dict_to_vector(bird["position"])
    poss_diff = goal_pos-bird_pos
    length = np.linalg.norm(poss_diff)
    return list(poss_diff/length*bird["speed"])

def xy_dict_to_vector(xy):
    return np.asarray([xy["x"],xy["y"]])

def wall_struct_to_tuple_list(wall_struct):
    return [xy_dict_to_vector(wall_struct["topLeft"]),xy_dict_to_vector(wall_struct["topRight"]),
        xy_dict_to_vector(wall_struct["bottomLeft"]),xy_dict_to_vector(wall_struct["bottomRight"])]