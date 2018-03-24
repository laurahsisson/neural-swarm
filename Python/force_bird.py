from base_bird import *
import shapely.geometry as sh
import numpy as np

ATRACTION_MASS_EXPONENT = 1
ATTRACTION_DISTANCE_EXPONENT = 2
ATTRACTION_CONSTANT = 1
ATTRACTION_CUTOFF = 25

REPULSION_MASS_EXPONENT = 1
REPULSION_DISTANCE_EXPONENT = 1
REPULSION_CONSTANT = 1
REPULSION_CUTOFF = 5



class ForceBird(BaseBird):
    def needs_grid(self):
        return False
        
    def make_decision(self,bird_number):
        my_pos = sh.Point(self.ws.bird_positions[bird_number])

        new_direction = np.array([0,0])
        for i, bird in enumerate(self.ws.birds):
            if i == bird_number:
                continue

            if my_pos.distance(sh.Point(self.ws.bird_positions[i])) <= max(ATTRACTION_CUTOFF,REPULSION_CUTOFF):
                new_direction += bird_delta_force(bird_number,i)
                

        exit()

        return aim_at_position(self.ws.birds[bird_number],self.ws.goal_pos)

    # Compute the closest point on the other bird's shape to our center
    # To be more accurate, we could compute the closest point on the other bird to our shape
    # But that requires much more computation for less reward
    def closest_point(self,my_pos,other_number):
        b_shape = self.ws.bird_shapes[other_number]
        pol_ext = sh.LinearRing(b_shape.exterior.coords)
        d = pol_ext.project(my_pos)
        return pol_ext.interpolate(d)
      
    def atraction_force(self,bird_number,other_number,delta_to_other):
        my_mass = self.ws.birds[bird_number]["mass"]
        other_mass = self.ws.birds[other_number]["mass"]
        distance = np.linag.norm(delta_to_other)
        delta_norm = delta_to_other/distance
        mass_factors = np.power(my_mass,ATTRACTION_MASS_EXPONENT) * np.power(other_mass,ATTRACTION_MASS_EXPONENT)
        total_factor = ATTRACTION_CONSTANT * mass_factors / np.power(distance,ATTRACTION_DISTANCE_EXPONENT) 
        return direction_norm*factor

    def repulsion_force(self,bird_number,other_number,delta_from_other):
        my_mass = self.ws.birds[bird_number]["mass"]
        other_mass = self.ws.birds[other_number]["mass"]
        distance = np.linag.norm(delta_to_other)
        delta_norm = delta_to_other/distance
        mass_factors = np.power(my_mass,REPULSION_MASS_EXPONENT) * np.power(other_mass,REPULSION_MASS_EXPONENT)
        total_factor = REPULSION_CONSTANT * mass_factors / np.power(distance,REPULSION_DISTANCE_EXPONENT) 
        return direction_norm*factor

    def bird_delta_force(self,bird_number,other_number):
        my_pos = sh.Point(self.ws.bird_positions[bird_number])
        closest_point = self.closest_point(my_pos,other_number)
        # The force is towards the other bird from this bird
        delta_to_other = subtract_points(closest_point,my_pos)
        d = my_pos.distance(sh.Point(self.ws.bird_positions[i]))
        new_direction = np.array([0,0])
        if d <= ATTRACTION_CUTOFF:
            new_direction += self.atraction_force(bird_number,other_number,delta_to_other)
        if d <+ REPULSION_CUTOFF:
            new_direction += self.repulsion_force(bird_number,other_number,-1*delta_to_other)


def subtract_points(p1,p2):
    p1 = p1.coords[0]
    p2 = p2.coords[0]
    return np.array([p1[0]-p2[0],p1[1]-p2[1]])