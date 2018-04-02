from base_bird import *
from unity_helper import xy_dict_to_vector
import shapely.geometry as sh
from math import pow, inf

ATTRACTION_MASS_EXPONENT = 1
ATTRACTION_DISTANCE_EXPONENT = 2
ATTRACTION_CONSTANT = 1
ATTRACTION_CUTOFF = 10

REPULSION_MASS_EXPONENT = 1
REPULSION_DISTANCE_EXPONENT = 1
REPULSION_CONSTANT = 10
REPULSION_CUTOFF = 5

# Actually the exponent for the bird's mass as the walls have infinite mass
OBSTACLE_MASS_EXPONENT = 1
OBSTACLE_DISTANCE_EXPONENT = 2
OBSTACLE_CONSTANT = 3
OBSTACLE_CUTOFF = 10

# As above, the exponent for the bird's mass in the attraction to the goal
GOAL_MASS_EXPONENT = -.025
GOAL_DISTANCE_EXPONENT = 1
GOAL_CONSTANT = 30
GOAL_CUTOFF = 40

ALIGNMENT_MASS_EXPONENT = 1
ALIGNMENT_SPEED_EXPONENT = 1
ALIGNMENT_DISTANCE_EXPONENT = 1
ALIGNMENT_CONSTANT = 3
ALIGNMENT_CUTOFF = 5

# We will only collection alignment and attraction force from a limited number of local birds
BIRD_COUNT_LIMIT = 5


class ForceBird(BaseBird):
    def needs_grid(self):
        return False

    def prepare_step(self):
        self.b2b_distance = dict()

    def parallelizable(self):
        return True

    def make_decision(self, bird_number):
        my_shape = self.ws.bird_shapes[bird_number]

        for other_number, other_shape in enumerate(self.ws.bird_shapes):
            if other_number == bird_number:
                continue
            # Warm up the cache
            birds = (min(bird_number, other_number), max(
                bird_number, other_number))
            if not birds in self.b2b_distance:
                self.b2b_distance[birds] = norm(
                    sub(self.ws.bird_positions[bird_number],
                        self.ws.bird_positions[other_number]))

        direction = zero_vector()
        direction = add(direction, attraction(self, bird_number))
        direction = add(direction, repulsion(self, bird_number))
        direction = add(direction, aligment(self, bird_number))
        direction = add(direction, walls(self, bird_number))
        direction = add(direction, goal(self,bird_number))

        # Must be normalized
        return normalize_to_speed(self,bird_number,direction)


def attraction(self, bird_number):
    position = zero_vector()
    count = 0
    for other_number in range(len(self.ws.birds)):
        if other_number == bird_number:
            continue
        birds = (min(bird_number, other_number), max(bird_number,
                                                     other_number))

        if self.b2b_distance[birds] <= ATTRACTION_CUTOFF:
            position = add(position, self.ws.bird_positions[other_number])
            count += 1
    if count == 0:
        return zero_vector()
    position = mult(position, 1 / count)
    return sub(position, self.ws.bird_positions[bird_number])


def repulsion(self, bird_number):
    force = zero_vector()
    count = 0
    for other_number in range(len(self.ws.birds)):
        if other_number == bird_number:
            continue
        birds = (min(bird_number, other_number), max(bird_number,
                                                     other_number))

        if self.b2b_distance[birds] <= REPULSION_CUTOFF:
            delta = sub(self.ws.bird_positions[bird_number],
                        self.ws.bird_positions[other_number])
            force = add(force, delta)
            count += 1
    if count == 0:
        return zero_vector()
    force = mult(force,1/count)
    return force


def aligment(self, bird_number):
    force = zero_vector()
    count = 0
    for other_number in range(len(self.ws.birds)):
        if other_number == bird_number:
            continue
        birds = (min(bird_number, other_number), max(bird_number,
                                                     other_number))

        if self.b2b_distance[birds] <= ALIGNMENT_CUTOFF:
            vel = xy_dict_to_vector(self.ws.birds[other_number]["velocity"])
            force = add(force, vel)
            count += 1
    if count == 0:
        return zero_vector()
    force = mult(force, 1 / count)
    return force


def walls(self, bird_number):
    force = zero_vector()

    my_shape = self.ws.bird_shapes[bird_number]
    count = 0
    for wall_shape in self.ws.wall_shapes:
        if my_shape.distance(wall_shape) <= OBSTACLE_CUTOFF:
            my_sh_pos = sh.Point(self.ws.bird_positions[bird_number])
            other_pos = list(closest_point(self, my_sh_pos, wall_shape).coords[0])
            delta = sub(
                other_pos, self.ws.bird_positions[bird_number])
            force = add(force, delta)
            count += 1
    if count == 0:
        return zero_vector()
    force = mult(force,1/count)
    return force

def goal(self, bird_number):
    goal_pos = list(self.ws.goal_pos)
    delta = sub(goal_pos,self.ws.bird_positions[bird_number])
    if norm(delta) > GOAL_CUTOFF:
        return zero_vector()
    delta_norm = normalize(delta)
    return mult(delta_norm,GOAL_CONSTANT)

def normalize_to_speed(self,bird_number,force):
    force_norm = normalize(force)
    return mult(force_norm,self.ws.birds[bird_number]["speed"])


# Compute the closest point on the other shape to our center
# To be more accurate, we could compute the closest point on the other bird to our shape
# But that requires much more computation for a marginal increase in accuracy
def closest_point(self, my_pos, other_shape):
    b_shape = other_shape
    pol_ext = sh.LinearRing(b_shape.exterior.coords)
    d = pol_ext.project(my_pos)
    return pol_ext.interpolate(d)


def norm(a):
    return pow(a[0] * a[0] + a[1] * a[1], .5)


def add(a, b):
    return [a[0] + b[0], a[1] + b[1]]


def sub(a, b):
    return [a[0] - b[0], a[1] - b[1]]


def mult(a, scalar):
    return [a[0] * scalar, a[1] * scalar]

def normalize(a):
    n = norm(a)
    if n == 0:
        return zero_vector()
    return mult(a,1/n)

def zero_vector():
    return [0, 0]
