from base_bird import *
import random
from unity_helper import xy_dict_to_vector, corner_struct_to_tuples
from math import sqrt

# source: https://blog.sicara.com/getting-started-genetic-algorithms-python-tutorial-81ffa1dd72f9

class GeneticBird(BaseBird):
    #def init
        
    # this function is called several times per second
    def make_decision(self, bird_number):
        
        b = self.ws.birds[bird_number]
        bird_pos = xy_dict_to_vector(b["position"])
        current_position = self.ws.unity_to_grid(bird_pos)
        bird_x = current_position[0]
        bird_y = current_position[1]
        
        modify_grid(self.ws.grid)
        print(grid)
        return grid[i][j]

        # modify the world state, rewrite it into a list of lists with random vectors where there aren't walls or goals
        # look into world state, figure out which square you're in, return vector associated with that square
        # this becomes your "genes/alleles", so modify the rest of the alg aaccordingly.
       
        # and then, as for getting the position of the bird when it bumps into a wall, you either have to create a new flag (study the python and BirdControl.cs code)
        # or you can instead implement it in unity (and move this project over to cs)
        # G L


def modify_grid(grid):
    for i in range(len(grid)):
        for j in range(len(grid[i])):
            grid[i][j] = generate_random_vector()

# fitness is eucliedean distance
# returns euclidean distance between two points provided in a tuple
def euclidean(points_tup): # points_tup is ( (x1, x2), (y1, y2) )
    (x1,x2), (y1,y2) = points_tup
    return sqrt( ((y1-x1)**2) + ((y2-x2)**2) ) # distance formula

# how "fit" a bird is: how close it got to the goal before colliding
def fitness(end_position, goal_position):
    score = euclidean( (end_position, goal_position) )
    return score

# use random vectors for our initial population
# returns a list with some x and y
def generate_random_vector():
    random_x = random.uniform(-10, 10)
    random_y = random.uniform(-10, 10)
    return [random_x,random_y]

# get our first generation of birds
# returns a list of vectors (each vector represents a bird in our population)
def generate_first_population(size_population):
    population = []
    for i in range(size_population):
        population.append(generate_random_vector())
    return population

# our algorithm now needs to test each bird in our list "population"
# so now we have some list, population_end_positions, 
# where vector i in population = end position i in population_end_positions
# incomplete:
# problem 1: we don't have the list, "population_end_positions" (ask ben on how to get results from the make_decision call)
# problem 2: this way of sorting is incorrect.  
    # consider https://stackoverflow.com/questions/6618515/sorting-list-based-on-values-from-another-list
def compute_population_fitness(population, population_end_positions, goal_pos):
    population.sort() #( key = lambda x: fitness(population_end_positions[x], goal_pos) )


# select the fittest birds to breed, along with a few non-fit ones for genetic diversity
def select_from_population(sorted_population, best_sample, lucky_few):
    next_generation = []

    # select the best birds to breed
    for i in range(best_sample):
        next_generation.append(sorted_population[i])
    # select some random birds to breed
    for i in range(lucky_few):
        next_generation.append(random.choice(sorted_population))
    
    random.shuffle(next_generation)
    return next_generation

# given two different birds (bird = vector with x and y)
# flip a coin to decide which components are passed on to the child
# return a vector with an x and y, to represent child bird
def create_child(bird1, bird2):
    child_x = 0
    child_y = 0

    def flip_a_coin():
        return "heads" # chosen by fair coin flip, guaranteed to be random

    if flip_a_coin() == "heads":
        child_x = bird1[0]
    else: 
        child_x = bird2[0]
    
    if flip_a_coin() == "heads":
        child_y = bird1[1]
    else: 
        child_y = bird2[1]

    child_bird = [child_x, child_y]
    return child_bird

# takes in a list of birds who will pass on their "genes" (vectors)
# takes in the num of children that each parent will make
def create_children(breeders, num_children):
    next_generation = []
    return next_generation

# have natural mutation of the previous generations offspring
# individual has a small probability to see their DNA change a bit
def mutate_child():
    return

def mutate_population():
    return

