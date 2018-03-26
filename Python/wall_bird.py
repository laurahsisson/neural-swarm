import numpy as np
from unity_helper import xy_dict_to_vector, corner_struct_to_tuples
import shapely.geometry as sh
from base_bird import *

class WallBird(BaseBird):
    def make_decision(self,bird_number):
        for i in range(len(self.ws.wall_shapes)):
            if WallBird.rect_in_path(self.ws.wall_shapes[i],self.ws.birds[bird_number],self.ws.goal_pos) is True:
                return WallBird.point_away_rect(self.ws.wall_shapes[i],self.ws.birds[bird_number])
        return WallBird.point_to_goal(self.ws.birds[bird_number],self.ws.goal_pos)

    def rect_in_path(rect,bird,goal_pos):
        bird_pos = xy_dict_to_vector(bird["position"])
        bird_point = sh.Point(bird_pos[0],bird_pos[1])
        goal_point = sh.Point(goal_pos[0],goal_pos[1])
        bird_goal_line = sh.LineString([bird_point, goal_point])
        return bird_goal_line.intersects(rect)

    # Given a bird, returns a vector point from the bird to goal with proper size
    def point_to_goal(bird,goal_pos):
        bird_pos = xy_dict_to_vector(bird["position"])
        poss_diff = goal_pos-bird_pos
        length = np.linalg.norm(poss_diff)
        return list(poss_diff/length*bird["speed"])  # returns direction of bird

    def point_away_rect(rect,bird):
        rect_coords = list(rect.exterior.coords)
        bird_pos = xy_dict_to_vector(bird["position"])
        min_dist = sum([rect_coords[0][0] - bird_pos[0], rect_coords[0][1] - bird_pos[1]])
        min_point = rect_coords[0]
        for i in range(len(rect_coords)):
            cur_min = sum([rect_coords[i][0] - bird_pos[0], rect_coords[i][1] - bird_pos[1]])
            if cur_min < min_dist:
                min_dist = cur_min
                min_point = rect_coords[i]
        if sh.Point([min_point[0] + bird['size'],min_point[1] + bird['size']]).intersects(rect) is False:
            new_point = sh.Point([min_point[0] + bird['size'],min_point[1] + bird['size']])
        else:
            new_point = sh.Point([min_point[0] - bird['size'],min_point[1] - bird['size']])
        return [new_point.x,new_point.y]