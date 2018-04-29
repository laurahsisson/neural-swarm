from base_bird import *

class LineBird(BaseBird):
    def needs_grid(self):
        return False
        
    def make_decision(self,bird_number):
        return aim_at_position(self.ws.birds[bird_number],self.ws.goal_pos)

