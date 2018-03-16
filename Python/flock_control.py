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
        walls = [corner_struct_to_tuples(wall_struct) for wall_struct in world_state["walls"]]
        wall_shapes = [sh.Polygon(wall) for wall in walls]

        goal_shape = sh.Point(goal_pos).buffer(world_state["goalDiameter"]/2)
        makeGrid(step,world_state["roomWidth"],world_state["roomHeight"],goal_shape,wall_shapes,birds)
        will_print = False
        return (world_state["generation"],[point_to_goal(bird,goal_pos) for bird in birds])
        

def makeGrid(step,width,height,goal_shape, wall_shapes, birds):
    global will_print

    width_points = int(width/step)
    height_points = int(height/step)
    grid = [0]*width_points
    for x in range(width_points):
        grid[x] = ['0']*height_points

    bird_shapes = [sh.Polygon(corner_struct_to_tuples(bird["rectCorners"])) for bird in birds]


    for x in range(width_points):
        for y in range(height_points):
            p = sh.Point(x,y)
            to_goal = p.distance(goal_shape)
            if to_goal == 0:
                grid[x][y] = "G"

            for ws in wall_shapes:
                to_wall = p.distance(ws)
                if to_wall == 0:
                    grid[x][y] = "W"

            for i,bs in enumerate(bird_shapes):
                to_bird = p.distance(bs)
                if to_bird == 0:
                    grid[x][y] = "B" + str(i)
        
def point_to_goal(bird,goal_pos):
    bird_pos = xy_dict_to_vector(bird["position"])
    poss_diff = goal_pos-bird_pos
    length = np.linalg.norm(poss_diff)
    return list(poss_diff/length*bird["speed"])

def xy_dict_to_vector(xy):
    return np.asarray([xy["x"],xy["y"]])

def corner_struct_to_tuples(wall_struct):
    return [xy_dict_to_vector(wall_struct["topLeft"]),xy_dict_to_vector(wall_struct["topRight"]),
        xy_dict_to_vector(wall_struct["bottomLeft"]),xy_dict_to_vector(wall_struct["bottomRight"])]