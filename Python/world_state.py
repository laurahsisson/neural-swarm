import shapely.geometry as sh
from unity_helper import xy_dict_to_vector, corner_struct_to_tuples

class WorldState:
    def __init__(self,unity_state):
        self.width = unity_state["roomWidth"]
        self.height = unity_state["roomHeight"]
        self.birds = unity_state["birds"]

        self.unity_state = unity_state

        self.bird_shapes = [sh.Polygon(corner_struct_to_tuples(bird["rectCorners"])) for bird in unity_state["birds"]]

        self.goal_pos = xy_dict_to_vector(unity_state["goalPosition"])
        self.goal_shape = sh.Point(self.goal_pos).buffer(unity_state["goalDiameter"]/2)

        self.wall_shapes = [sh.Polygon(corner_struct_to_tuples(wall)) for wall in unity_state["walls"]]


    def _mark_boundary(self,shape,marker,grid_step):
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
                if grid_x < 0 or grid_x >= len(self.grid):
                    continue
                if grid_y < 0 or grid_y >= len(self.grid[0]):
                    continue
                self.grid[grid_x][grid_y] = marker

     
        boundary = shape.boundary.coords
        for i in range(len(boundary)-1):
            points_in_line(boundary[i],boundary[i+1])


    def make_grid(self,grid_step):
        width_points = int(self.width/grid_step)
        height_points = int(self.height/grid_step)
        self.grid = [0]*width_points
        for x in range(width_points):
            self.grid[x] = ['0']*height_points

        self._mark_boundary(self.goal_shape,'G',grid_step)

        for wl in self.wall_shapes:
            self._mark_boundary(wl,'W',grid_step)

        for i, bd in enumerate(self.bird_shapes):
            if not self.birds[i]["active"]:
                continue
            self._mark_boundary(bd,'B',grid_step)
        
