import numpy as np
from scipy import interpolate
import shapely.geometry as sh
from timeit import default_timer as timer

will_print = True
grid_step = .1


class FlockControl():
    def __init__(self,num_birds):
        # Eventually, when we make each bird a class we can initialize an array here
        self.num_birds = num_birds

    def make_decisions(self, world_state):
        global will_print
        start = timer()

        birds = world_state["birds"]
        bird_shapes = [sh.Polygon(corner_struct_to_tuples(bird["rectCorners"])) for bird in birds]

        goal_pos = xy_dict_to_vector(world_state["goalPosition"])

        walls = [corner_struct_to_tuples(wall_struct) for wall_struct in world_state["walls"]]
        wall_shapes = [sh.Polygon(wall) for wall in walls]

        goal_shape = sh.Point(goal_pos).buffer(world_state["goalDiameter"]/2)
        
        # grid = make_grid(world_state["roomWidth"],world_state["roomHeight"],goal_shape,wall_shapes,bird_shapes)

        grid = make_grid_2(world_state["roomWidth"],world_state["roomHeight"],goal_shape,wall_shapes,bird_shapes)
        # for x in grid:
        #     print(x)
        will_print = False


        # for a in wall_shapes[0].boundary:
        #     print(a)

        print("TIME:",timer()-start)  
        exit()

        return (world_state["generation"],[point_to_goal(bird,goal_pos) for bird in birds])
        

def mark_boundary(shape,marker,grid):
    global will_print,grid_step
    # Mark every point on the line between p1 and p2
    def points_in_line(p1,p2, axis = 0):
        print(p1,p2)
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


def make_grid_2(width,height,goal_shape,wall_shapes,bird_shapes):
    global will_print, grid_step

    width_points = int(width/grid_step)
    height_points = int(height/grid_step)
    grid = [0]*width_points
    grid_points = [0]*width_points
    for x in range(width_points):
        grid[x] = ['0']*height_points

    mark_boundary(goal_shape,'G',grid)

    for ws in wall_shapes:
        mark_boundary(ws,'W',grid)

    for i, bs in enumerate(bird_shapes):
        mark_boundary(bs,'B',grid)
    return grid


def make_grid(width,height,goal_shape, wall_shapes, bird_shapes):
    global will_print, grid_step

    # Given a Shapely Shape, fills the interior of the Shape on the grid with the given marker
    def fill_bound(object_shape,object_marker):
      for grid_x in range(int(object_shape.bounds[0]/grid_step),int(object_shape.bounds[2]/grid_step)+2):
        x = grid_x*grid_step
        if (grid_x>=width_points-1):
            break
        for grid_y in range(int(object_shape.bounds[1]/grid_step),int(object_shape.bounds[3]/grid_step)+2):
            if (grid_y>=height_points-1):
                break

            y = grid_y*grid_step
            # Though we are creating multiple sh.Points for each x,y it is faster than storing somewhere
            p = sh.Point(x,y)
            to_object = p.distance(object_shape)
            # If the point is close to the object, count it as being inside the object, even if it is not
            if to_object < grid_step:
                grid[grid_x][grid_y] = object_marker

    width_points = int(width/grid_step)
    height_points = int(height/grid_step)
    grid = [0]*width_points
    grid_points = [0]*width_points
    for x in range(width_points):
        grid[x] = ['0']*height_points


    for i, bs in enumerate(bird_shapes):
        fill_bound(bs,("BIRD",i))
    
    fill_bound(goal_shape,("GOAL",))
    for ws in wall_shapes:
        fill_bound(ws,("WALL",))
    
    return grid 

# Given a bird, returns a vector point from the bird to goal with proper size
def point_to_goal(bird,goal_pos):
    bird_pos = xy_dict_to_vector(bird["position"])
    poss_diff = goal_pos-bird_pos
    length = np.linalg.norm(poss_diff)
    return list(poss_diff/length*bird["speed"])

def xy_dict_to_vector(xy):
    return np.asarray([xy["x"],xy["y"]])

# Given a cornerStruct as described in CornerStruct.cs, turns it into a list of 4 xy tuples
def corner_struct_to_tuples(corner_struct):
    return [xy_dict_to_vector(corner_struct["topLeft"]),xy_dict_to_vector(corner_struct["topRight"]),
        xy_dict_to_vector(corner_struct["bottomLeft"]),xy_dict_to_vector(corner_struct["bottomRight"])]