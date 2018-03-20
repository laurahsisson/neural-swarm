from base_bird import *
import numpy as np

class LineBird(BaseBird):
    def make_decision(self,bird_number):
        return aim_at_position(self.ws.birds[bird_number],self.ws.goal_pos)

