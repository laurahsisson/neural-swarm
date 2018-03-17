import numpy as np
import shapely.geometry as sh
from timeit import default_timer as timer
from unity_helper import xy_dict_to_vector, corner_struct_to_tuples
import line_bird as lb

will_print = True

class WorldState:
    def __init__(self,unity_state):
        self.width = unity_state["roomWidth"]
        self.height = unity_state["roomHeight"]
        self.birds = unity_state["birds"]

        self.grid_step = .1

        self.unity_state = unity_state

        self.bird_shapes = [sh.Polygon(corner_struct_to_tuples(bird["rectCorners"])) for bird in unity_state["birds"]]

        self.goal_pos = xy_dict_to_vector(unity_state["goalPosition"])
        self.goal_shape = sh.Point(self.goal_pos).buffer(unity_state["goalDiameter"]/2)

        self.wall_shapes = [sh.Polygon(corner_struct_to_tuples(wall)) for wall in unity_state["walls"]]


class FlockControl:
    def __init__(self,num_birds):
        # Eventually, when we make each bird a class we can initialize an array here
        self.num_birds = num_birds

    def make_decisions(self, unity_state):
        global will_print
        ws = WorldState(unity_state)

        start = timer()
        ws.grid = make_grid(ws)
        print("Grid in :",timer()-start, "seconds")  

        start = timer()
        decisions = [[0,0]]*len(ws.birds)
        for b, bird in enumerate(ws.birds):
            if not bird["active"]:
                continue
            decisions[b] = lb.make_decision(ws,b)
        print("Decisions in :",timer()-start, "seconds")  


        return (unity_state["generation"],decisions)
        

def mark_boundary(shape,marker,grid,grid_step):
    global will_print
    # Mark every point on the line between p1 and p2
    def points_in_line(p1,p2, axis = 0):
        x_diff = p2[0]-p1[0]
        y_diff = p2[1]-p1[1]
        max_diff = max(abs(x_diff),abs(y_diff))
        max_steps = abs(max_diff/grid_step)
        
        assert max_steps > 0

        for s in range(int(max_steps)+1):
            theta = s / max_steps
            x = p1[0] + x_diff * theta
            y = p1[1] + y_diff * theta
            grid_x = int(x/grid_step)
            grid_y = int(y/grid_step)
            if grid_x < 0 or grid_x >= len(grid):
                continue
            if grid_y < 0 or grid_y >= len(grid[0]):
                continue
            grid[grid_x][grid_y] = marker

 
    boundary = shape.boundary.coords
    for i in range(len(boundary)-1):
        points_in_line(boundary[i],boundary[i+1])


def make_grid(ws):
    global will_print
    width_points = int(ws.width/ws.grid_step)
    height_points = int(ws.height/ws.grid_step)
    grid = [0]*width_points
    grid_points = [0]*width_points
    for x in range(width_points):
        grid[x] = ['0']*height_points

    mark_boundary(ws.goal_shape,'G',grid,ws.grid_step)

    for wl in ws.wall_shapes:
        mark_boundary(wl,'W',grid,ws.grid_step)

    for i, bd in enumerate(ws.bird_shapes):
        if not ws.birds[i]["active"]:
            continue
        mark_boundary(bd,'B',grid,ws.grid_step)
    
    return grid