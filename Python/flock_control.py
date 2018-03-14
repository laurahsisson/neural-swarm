import numpy as np
import shapely.geometry as sh

will_print = True

class FlockControl():
    def __init__(self,num_birds):
        # Eventually, when we make each bird a class we can initialize an array here
        self.num_birds = num_birds

    def make_decisions(self, world_state):
        global will_print

        step = 2

        birds = world_state["birds"]
        goal_pos = xy_dict_to_vector(world_state["goalPosition"])

        # print(world_state["roomWidth"],world_state["roomHeight"])
        walls = [wall_struct_to_tuple_list(wall_struct) for wall_struct in world_state["walls"]]
        wall_shapes = [sh.Polygon(wall) for wall in walls]
        if will_print:
            print(wall_shapes)

        goal_shape = sh.Point(goal_pos).buffer(world_state["goalDiameter"]/2)
        makeGrid(step,world_state["roomWidth"],world_state["roomHeight"],goal_shape,wall_shapes)
        will_print = False
        return (world_state["generation"],[point_to_goal(bird,goal_pos) for bird in birds])
        

def makeGrid(step,width,height,goal_shape, wall_shapes):
    global will_print

    width_points = int(width/step)
    height_points = int(height/step)
    grid = [0]*width_points
    for x in range(width_points):
        grid[x] = [0]*height_points

    for x in range(width_points):
        for y in range(height_points):
            p = sh.Point(x,y)
            to_goal = p.distance(goal_shape)
            if to_goal == 0:
                grid[x][y] = "GOAL"

            for ws in wall_shapes:
                to_wall = p.distance(ws)
                if to_wall == 0:
                    grid[x][y] = "WALL"


        
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