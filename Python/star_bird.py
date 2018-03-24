from unity_helper import *
from base_bird import *
import numpy as np

class Node:
    def __init__(self,pos,g,h,path):
        self.pos = pos
        self.g = g
        self.h = h
        self.d = g+h
        self.path = path

class LineBird(BaseBird):
    def make_decision(self,bird_number):
        return aim_at_position(self.ws.birds[bird_number],self.ws.goal_pos)
        
        path = self.a_star(bird_number)

        current = self.ws.bird_positions[bird_number]
        if path == -1:
            return [0,0]
        last = current
        dist = 0
        print(current)
        for gp in path:
            pos = self.ws.grid_to_unity(gp)
            print(pos)
            n_dist = euclidian(last,current) 
            dist += n_dist
            last = pos
            if dist > self.ws.birds[bird_number]["speed"]:
                # We should aim as far down the path as we can for now
                break

        aim_pos = self.ws.grid_to_unity(path[0])
        print()
        return aim_at_position(self.ws.birds[bird_number],aim_pos)

    def a_star(self,bird_number):
        start = self.ws.unity_to_grid(xy_dict_to_vector(self.ws.birds[bird_number]["position"]))
        goal = self.ws.unity_to_grid(self.ws.goal_pos)


        start_n = Node(start,0,manhattan(start,goal),[])
        # open_set contains full nodes
        open_set = set([start_n])
        # closed_set just contains positions so it can be accessed easily
        closed_set = set()

        while open_set:
            current = min(open_set,key=lambda n: n.d)
            open_set.remove(current)
            if current.pos == goal or value_at_node(self.ws.grid,current) == self.ws.GOAL:
                return current.path+[current.pos]

            ns = neighbors(current.pos,self.ws,bird_number,closed_set)
            for p in ns:
                closed_set.add(tuple(p))
                open_set.add(Node(p,current.g+euclidian(current.pos,p),manhattan(p,goal),current.path+[current.pos]))

        # If we find nothing, then just return our current position
        return -1

def neighbors(p1,ws,bird_number, closed_set):
    def check_valid(x,y):
        valid_node = ws.grid[x][y] == ws.GOAL or ws.grid[x][y] == ws.OPEN or ws.grid[x][y] == bird_number
        if not valid_node:
            return False
        return True

    ns = []
    for x in range(p1[0]-1,p1[0]+2):
        if x < 0 or x >= len(ws.grid):
            continue
        for y in range(p1[1]-1,p1[1]+2):
            if y < 0 or y >= len(ws.grid[0]):
                continue
            
            if (x,y) in closed_set:
                continue

            valid = check_valid(x,y)
            if not valid:
                closed_set.add((x,y))
                continue

            up = ws.grid_to_unity([x,y])
            start_pos = ws.bird_positions[bird_number]
            
            diff = [up[0]-start_pos[0],up[1]-start_pos[1]]
            new_shape = ws.translate_shape(ws.bird_shapes[bird_number],diff).buffer(ws.grid_step)
            contour = ws.get_contour(new_shape)
            
            valid = all([check_valid(x,y) for x,y in contour])
            if not valid:
                closed_set.add((x,y))

            ns.append([x,y])
    return ns

def manhattan(p1,p2):
    return abs(p1[0]-p2[0]) + abs(p1[1]-p2[1])

def value_at_node(grid,node):
    if len(node.pos) != 2:
        print(node.pos)
        assert len(node.pos) == 2
    return grid[node.pos[0]][node.pos[1]]

def euclidian(p1,p2):
    x_d = (p1[0]-p2[0])
    y_d = (p1[1]-p2[1])
    return np.power(x_d*x_d+y_d*y_d,.5)