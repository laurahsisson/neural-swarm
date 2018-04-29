import shapely.geometry as sh
import shapely.affinity as affin
from unity_helper import xy_dict_to_vector, corner_struct_to_tuples

# Grid is width/grid_step by height/grid_step
# Every shape is represented by its contour/perimeter on the goal (its center is not filled in)
# At a particular grid[x][y] can be the following
    # WALL - if it has a wall
    # GOAL - if it has the goal
    # OPEN - if it is open
    # the bird's number as an integer - if it has the bird ()


class WorldState:
    def __init__(self,unity_state):
        self.width = unity_state["roomWidth"]
        self.height = unity_state["roomHeight"]
        self.birds = unity_state["birds"]

        self.unity_state = unity_state

        self.bird_positions = [xy_dict_to_vector(bird["position"]) for bird in unity_state["birds"]]
        self.bird_shapes = [sh.Polygon(corner_struct_to_tuples(bird["rectCorners"])) for bird in unity_state["birds"]]

        self.goal_pos = xy_dict_to_vector(unity_state["goalPosition"])
        self.goal_shape = sh.Point(self.goal_pos).buffer(unity_state["goalDiameter"]/2)

        self.wall_shapes = [sh.Polygon(corner_struct_to_tuples(wall)) for wall in unity_state["walls"]]
        self.GOAL = "GOAL"
        self.WALL = "WALL"
        self.OPEN = "OPEN"


    def get_contour(self,shape):
        contour = []
         # Mark every point on the line between p1 and p2
        def points_in_line(p1,p2, axis = 0):
            x_diff = p2[0]-p1[0]
            y_diff = p2[1]-p1[1]
            max_diff = max(abs(x_diff),abs(y_diff))
            max_steps = abs(max_diff/self.grid_step)
            
            assert max_steps > 0

            for s in range(int(max_steps)+1):
                theta = s / max_steps
                x = p1[0] + x_diff * theta
                y = p1[1] + y_diff * theta

                p = self.unity_to_grid([x,y])
                grid_x, grid_y = p

                if grid_x < 0 or grid_x >= len(self.grid):
                    continue
                if grid_y < 0 or grid_y >= len(self.grid[0]):
                    continue
                
                contour.append(p)
                # self.grid[grid_x][grid_y] = marker

        boundary = shape.boundary.coords
        for i in range(len(boundary)-1):
            points_in_line(boundary[i],boundary[i+1])
        return contour

    def _mark_boundary(self,shape,marker):
       contour = self.get_contour(shape)
       for x,y in contour:
           self.grid[x][y] = marker


    # every bird has a copy of the grid as  their "decision"

    def make_grid(self,grid_step):
        self.grid_step = grid_step
        width_points = int(self.width/grid_step)
        height_points = int(self.height/grid_step)
        self.grid = [0]*width_points
        for x in range(width_points):
            self.grid[x] = [self.OPEN]*height_points

        self._mark_boundary(self.goal_shape,self.GOAL)

        for wl in self.wall_shapes:
            self._mark_boundary(wl,self.WALL)

        for i, bd in enumerate(self.bird_shapes):
            if not self.birds[i]["active"]:
                continue
            self._mark_boundary(bd,i)

    def translate_shape(self,shape,offset):
        return affin.translate(shape,xoff=offset[0],yoff=offset[1])

    # up = unity posiiton (im guessing)
    def unity_to_grid(self,up):
        point = [up[0]/self.grid_step,up[1]/self.grid_step]
        return [int(point[0]),int(point[1])]
        
    def grid_to_unity(self,gp):
        return [gp[0]*self.grid_step,gp[1]*self.grid_step]