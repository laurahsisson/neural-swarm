from base_bird import *
import shapely.geometry as sh
import numpy as np
from unity_helper import xy_dict_to_vector
from timeit import default_timer as timer

ATTRACTION_MASS_EXPONENT = 1
ATTRACTION_DISTANCE_EXPONENT = 2
ATTRACTION_CONSTANT = 1
ATTRACTION_CUTOFF = 10

REPULSION_MASS_EXPONENT = 1
REPULSION_DISTANCE_EXPONENT = 1
REPULSION_CONSTANT = 1
REPULSION_CUTOFF = 2

# Actually the exponent for the bird's mass as the walls have infinite mass
OBSTACLE_MASS_EXPONENT = 1
OBSTACLE_DISTANCE_EXPONENT = 2
OBSTACLE_CONSTANT = 3
OBSTACLE_CUTOFF = 3

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

function_to_time = dict()

def timerfunc(func):
    """
    A timer decorator
    """
    def function_timer(*args, **kwargs):
        global function_to_time
        """
        A nested function for timing other functions
        """
        start = timer()
        value = func(*args, **kwargs)
        end = timer()
        runtime = end - start

        n = func.__name__
        if not n in function_to_time:
            function_to_time[n] = 0
        function_to_time[n] += runtime

        return value
    return function_timer


class ForceBird(BaseBird):
    def needs_grid(self):
        return False

    def prepare_step(self):
        global function_to_time
        function_to_time = dict() 
        # self.b2b_distance = dict()
        self.b2b_delta = dict()

    def parallelizable(self):
        return True

    def end_step(self):
        pass
        # print(function_to_time)
        # print(function_to_time["bird_force"]/function_to_time["make_decision"])

    def make_decision(self, bird_number):
        new_direction = zero_vector()

        for other_number, bird in enumerate(self.ws.birds):
            if other_number == bird_number:
                continue
            new_direction += self.bird_force(bird_number,other_number)

        for wall in self.ws.wall_shapes:
            new_direction += self.wall_force(bird_number,wall)

        new_direction += self.goal_force(bird_number)

        velocity = self.normalize_to_speed(bird_number,new_direction)
        return list(velocity)

    def bird_force(self,bird_number,other_number):
        my_shape = self.ws.bird_shapes[bird_number]
        birds = (min(bird_number,other_number),max(bird_number,other_number))
        dist = 0
        total_force = zero_vector()

        dist = np.linalg.norm(self.ws.bird_positions[bird_number]-self.ws.bird_positions[other_number])

        if dist <= max(ATTRACTION_CUTOFF, REPULSION_CUTOFF):
            # The following 3 accesses to b2b_delta are thread-safe
            if birds in self.b2b_delta:
                force = self.b2b_delta[birds]
            else:
                force = self.bird_delta_force(bird_number, other_number)
                self.b2b_delta[birds] = force

            total_force += force

        if dist <= ALIGNMENT_CUTOFF:
            total_force += self.bird_alignment_force(bird_number, other_number)
        return total_force

    def wall_force(self,bird_number,wall):
        my_shape = self.ws.bird_shapes[bird_number]

        if my_shape.distance(wall) > OBSTACLE_CUTOFF:
            return zero_vector()
        return self.wall_delta_force(bird_number, wall)

    def goal_force(self,bird_number):
        dto = self.ws.goal_pos-self.ws.bird_positions[bird_number]
        distance = np.linalg.norm(dto)
        if distance > GOAL_CUTOFF:
            return zero_vector()
        return self.goal_delta_force(bird_number, self.ws.goal_shape)

    def normalize_to_speed(self,bird_number,new_direction):
        magnitude = np.linalg.norm(new_direction)
        if magnitude != 0:
            norm_direction = new_direction / magnitude
        else:
            norm_direction = new_direction

        velocity = norm_direction * self.ws.birds[bird_number]["speed"]
        isnan = any(np.isnan(velocity))
        if isnan:
            print(new_direction)
            print(velocity)
            exit()
        return velocity


    # Compute the closest point on the other shape to our center
    # To be more accurate, we could compute the closest point on the other bird to our shape
    # But that requires much more computation for less reward
    def closest_point(self, my_pos, other_shape):
        b_shape = other_shape
        pol_ext = sh.LinearRing(b_shape.exterior.coords)
        d = pol_ext.project(my_pos)
        return pol_ext.interpolate(d)

    def delta_norm(self, bird_number, other_shape):
        my_pos = sh.Point(self.ws.bird_positions[bird_number])
        closest_point = self.closest_point(my_pos, other_shape)
        # The force is towards the other_shape from this bird
        return subtract_points(closest_point, my_pos)

    def atraction_force(self, bird_number, other_number, delta_to_other,
                        distance):
        my_mass = self.ws.birds[bird_number]["mass"]
        other_mass = self.ws.birds[other_number]["mass"]
        mass_factors = np.power(my_mass, ATTRACTION_MASS_EXPONENT) * np.power(
            other_mass, ATTRACTION_MASS_EXPONENT)

        delta_norm = delta_to_other / distance
        total_factor = ATTRACTION_CONSTANT * mass_factors / np.power(
            distance, ATTRACTION_DISTANCE_EXPONENT)
        return delta_norm * total_factor

    def repulsion_force(self, bird_number, other_number, delta_from_other,
                        distance):
        my_mass = self.ws.birds[bird_number]["mass"]
        other_mass = self.ws.birds[other_number]["mass"]
        mass_factors = np.power(my_mass, REPULSION_MASS_EXPONENT) * np.power(
            other_mass, REPULSION_MASS_EXPONENT)

        delta_norm = delta_from_other / distance
        total_factor = REPULSION_CONSTANT * mass_factors / np.power(
            distance, REPULSION_DISTANCE_EXPONENT)
        return delta_norm * total_factor

    def bird_delta_force(self, bird_number, other_number):
        other_shape = self.ws.bird_shapes[other_number]
        dto = self.ws.bird_positions[other_number]-self.ws.bird_positions[bird_number]
        d = np.linalg.norm(dto)

        my_shape = self.ws.bird_shapes[bird_number]
        other_shape = self.ws.bird_shapes[other_number]

        delta = zero_vector()
        if d <= ATTRACTION_CUTOFF:
            delta += self.atraction_force(bird_number, other_number, dto, d)
        if d <= REPULSION_CUTOFF:
            delta += self.repulsion_force(bird_number, other_number, -1 * dto,
                                          d)
        return delta

    def bird_alignment_force(self, bird_number, other_number):
        velocity = xy_dict_to_vector(self.ws.birds[other_number]["velocity"])
        speed = np.linalg.norm(velocity)
        if speed == 0:
            return zero_vector()
        delta_norm = velocity / speed

        birds = (min(bird_number,other_number),max(bird_number,other_number))

        my_mass = self.ws.birds[bird_number]["mass"]
        other_mass = self.ws.birds[other_number]["mass"]
        mass_factors = np.power(my_mass, ALIGNMENT_MASS_EXPONENT) * np.power(
            other_mass, ALIGNMENT_MASS_EXPONENT)

        other_shape = self.ws.bird_shapes[other_number]
        distance = np.linalg.norm(self.ws.bird_positions[bird_number]-self.ws.bird_positions[other_number])

        total_factor = ALIGNMENT_CONSTANT * mass_factors * np.power(
            speed, ALIGNMENT_SPEED_EXPONENT) / np.power(
                distance, ALIGNMENT_DISTANCE_EXPONENT)
        return delta_norm * total_factor

    def wall_delta_force(self, bird_number, wall_shape):
        my_mass = self.ws.birds[bird_number]["mass"]
        mass_factors = np.power(my_mass, OBSTACLE_MASS_EXPONENT)

        dfo = -1 * self.delta_norm(bird_number, wall_shape)
        distance = np.linalg.norm(dfo)
        delta_norm = dfo / distance

        total_factor = OBSTACLE_CONSTANT * mass_factors / np.power(
            distance, OBSTACLE_DISTANCE_EXPONENT)
        return delta_norm * total_factor

    def goal_delta_force(self, bird_number, goal_shape):
        my_mass = self.ws.birds[bird_number]["mass"]
        mass_factors = np.power(my_mass, GOAL_MASS_EXPONENT)

        dto = self.ws.goal_pos-self.ws.bird_positions[bird_number]
        distance = np.linalg.norm(dto)
        delta_norm = dto / distance

        total_factor = GOAL_CONSTANT * mass_factors / np.power(
            distance, GOAL_DISTANCE_EXPONENT)
        return delta_norm * total_factor


__all_factors = [
    "attract_mass_exp", "attract_dist_exp", "attract_const", "attract_cutoff",
    "repulse_mass_exp", "repulse_dist_exp", "repulse_const", "repulse_cutoff",
    "wall_mass_exp", "wall_dist_exp", "wall_const", "wall_cutoff",
    "obstacle_mass_exp", "obstacle_dist_exp", "obstacle_const",
    "obstacle_cutoff", "align_mass_exp", "align_dist_exp", "align_speed_exp",
    "align_const", "align_cutoff"
]


def factor_dict_to_list(factor_dict):
    global __all_factors

    factor_list = [0] * len(__all_factors)
    for factor, val in factor_dict.items():
        i = __all_factors.index(factor)
        factor_list[i] = factor_dict[factor]
    return factor_list


def factor_list_to_dict(factor_list):
    global __all_factors

    factor_dict = dict()
    for i, factor in enumerate(factor_list):
        factor_dict[__all_factors[i]] = factor
    return factor_dict


def random_factor_list():
    global __all_factors

    factor_list = [0] * len(__all_factors)
    for i in range(len(factor_list)):
        factor_list[i] = np.random.random_sample()
    return factor_list

def zero_vector():
    return np.array([0.0,0.0])

def subtract_points(p1, p2):
    p1 = p1.coords[0]
    p2 = p2.coords[0]
    return np.array([p1[0] - p2[0], p1[1] - p2[1]])


