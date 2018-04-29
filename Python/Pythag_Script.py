import numpy as np
from unity_helper import xy_dict_to_vector, corner_struct_to_tuples
import shapely.geometry as sh
import flock_control
import base_bird
import Wall_bird

def make_decision(ws,bird_number):
    flag = False
    for i in range(len(ws.wall_shapes)):
        if rect_in_path(ws.wall_shapes[i],ws.birds[bird_number],ws.goal_pos) is True:
            flag = True
            aim_pos = pythag_path(ws.birds[bird_number], ws.wall_shapes[i], ws.goal_pos)
            
    if(flag):
        return aim_at_position(ws.birds[bird_number], aim_pos)
    else:
        return aim_at_position(ws.birds[bird_number], ws.goal_pos)

def pythag_path(bird, rect, goal_pos):
    #cases
    flag = False
    flag1 = False
    flag2 = False
    flag3 = False

    #relative data
    wall_coords = list(rect.exterior.coords)
    bird_position = xy_dict_to_vector(bird["position"])
    bird_point = sh.Point(bird_pos[0],bird_pos[1])
    goal_point = sh.Point(goal_pos[0],goal_pos[1])
    tragectoryTl = sh.LineString([bird_point, wall_coords[0]])
    tragectoryTr = sh.LineString([bird_point, wall_coords[1]])
    tragectoryBl = sh.LineString([bird_point, wall_coords[2]])                                
    tragectoryBr = sh.LineString([bird_point, wall_coords[3]])
    possibilities = [tragectoryTl, tragectoryTr, tragectoryBl, tragectoryBr]
    
    #Find 2 shortests paths to edge of obstacle
    m1 = 100000
    m2 = 100000
    for i in possibilities:
        if(possibilities[i].length < m1):
            m1 = i
        elif(possibilities[i].length < m2):
            if(m1 != possibilities[i].length):
                m2 = i

    #force the bird to choose either the top or the bottom of an object, two corners on the same side of the object were chosen using corner that is closest.

    #topleft and bottom left            
    if(m1 == 0 and m2 == 2):
        #topright
        m2 = 1

    #bottomleft and topleft
    if(m1 == 2 and m2 == 0):
        #bottomright
        m2 = 3

    #topright and bottomright    
    if(m1 == 1 and m2 == 3):
        #topleft
        m2 = 0

    #bottomright and topright
    if(m1 == 3 and m2 == 1):
        #bottomleft
        m2 == 2
                
    x1 = possibilities[m1].coords[1][0]
    x2 = possibilities[m2].coords[1][0]
    y1 = possibilities[m1].coords[1][1]
    y2 = possibilities[m2].coords[1][1]
    
    #Check if direction is positive, negative or zero for x and y coordinates.
    #check y coordinates
    if((y2 - bird_point[1]) > 0):
        y_diff = y2 - bird_point[1]
        #default case, diff is positive (move up)                               
        
    elif((y2 - bird_point[1]) = 0):
        flag = True
        y_diff = 0
        #no difference in vertical position, obstacle is directly in the way, use y1.
        
    else:
        #negative vertical movement (move down)
        flag1 = True
        y_diff = y2 - bird_point[1]
        
    #check x coordinates
    if((x2 - bird_point[0]) > 0):
        x_diff = x2 - bird_point[0]
        #default case, diff is positive (move right)
        
    elif((x2 - bird_point[0]) = 0):
        #no difference in linear position, obstacle is directly in the way, use x1.
        flag2 = True
        x_diff = 0
        
    else:
        #negative linear movement (move left)
        flag3 = True
        x_diff = x2 - bird_point[0]


    #Use Triangle legs under each flag case.

    #up, right    
    if(!flag and !flag1 and !flag2 and !flag3):
        #check if going right first results in wall collision
        temp = sh.Point(bird_point[0] + x_diff, bird_point[1])
        if(rect_in_path(rect, bird, temp) is True):
            #go up first
            target = sh.Point(bird_point[0], bird_point[1] + y_diff + bird.height) #revisit here
            return target
        else:
            #go right first
            target = sh.Point(bird_point[0] + x_diff + bird.width, bird_point[1]) #revisit here
            return target

    #up, left
    if(!flag and !flag1 and !flag2 and flag3):
        #check if going left first results in wall collision
        temp = sh.Point(bird_point[0] + x_diff, bird_point[1])
        if(rect_in_path(rect, bird, temp) is True):
            #go up first
            target = sh.Point(bird_point[0], bird_point[1] + y_diff + bird.height) #revisit here
            return target
        else:
            #go left first
            target = sh.Point(bird_point[0] + x_diff + bird.width, bird_point[1]) #revisit here
            return target

    #down, right
    if(!flag and flag1 and !flag2 and !flag3):
        #check if going right first results in wall collision
        temp = sh.Point(bird_point[0] + x_diff, bird_point[1])
        if(rect_in_path(rect, bird, temp) is True):
            #go down first
            target = sh.Point(bird_point[0], bird_point[1] + y_diff + bird.height) #revisit here
            return target
        else:
            #go right first
            target = sh.Point(bird_point[0] + x_diff + bird.width, bird_point[1]) #revisit here
            return target

    #down, left
    if(!flag and flag1 and !flag2 and flag3):
        #check if going right first results in wall collision
        temp = sh.Point(bird_point[0] + x_diff, bird_point[1])
        if(rect_in_path(rect, bird, temp) is True):
            #go down first
            target = sh.Point(bird_point[0], bird_point[1] + y_diff + bird.height) #revisit here
            return target
        else:
            #go left first
            target = sh.Point(bird_point[0] + x_diff + bird.width, bird_point[1]) #revisit here
        
    #right only
    if(flag and !flag1 and !flag2 and !flag3):
        #wall is directly in b/w bird and goal so compute new path around using a triangle.
        temp = sh.Point(bird_point[0] + x_diff, bird_point[1] - bird.height)
        if(rect_in_path(rect, bird, temp) is True):
            #go up first
            target = sh.Point(bird_point[0], bird_point[1] + bird.height) #revisit here
            return target
        else:
            #go down first
            target = sh.Point(bird_point[0], bird_point[1] - bird.height) #revisit here

    #up only    
    if(!flag and !flag1 and flag2 and !flag3):
        #wall is directly in b/w bird and goal so compute new path around using a triangle.
        temp = sh.Point(bird_point[0] + bird.width, bird_point[1] + y_diff)
        if(rect_in_path(rect, bird, temp) is True):
            #go left first
            target = sh.Point(bird_point[0] - bird.width, bird_point[1]) #revisit here
            return target
        else:
            #go right first
            target = sh.Point(bird_point[0] + bird.width, bird_point[1]) #revisit here
            return target

    #left only
    if(flag and !flag1 and !flag2 and flag3):
        #wall is directly in b/w bird and goal so compute new path around using a triangle.
        temp = sh.Point(bird_point[0] + x_diff, bird_point[1] - bird.height)
        if(rect_in_path(rect, bird, temp) is True):
            #go up first
            target = sh.Point(bird_point[0], bird_point[1] + bird.height) #revisit here
            return target
        else:
            #go down first
            target = sh.Point(bird_point[0], bird_point[1] - bird.height) #revisit here
            return target

    #down only
    if(!flag and flag1 and flag2 and !flag3):
        #wall is directly in b/w bird and goal so compute new path around using a triangle.
        temp = sh.Point(bird_point[0] + bird.width, bird_point[1] + y_diff)
        if(rect_in_path(rect, bird, temp) is True):
            #go left first
            target = sh.Point(bird_point[0] - bird.width, bird_point[1]) #revisit here
            return target
        else:
            #go right first
            target = sh.Point(bird_point[0] + bird.width, bird_point[1]) #revisit here
            return target

    #not possible
    #if(flag and !flag1 and flag2 and !flag3):
