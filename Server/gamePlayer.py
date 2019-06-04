import time
import numpy
import GameObject

class GamePlayer(GameObject.GameObject):

    def __init__(self, name, address, x, y, z):
        super().__init__(x, y, z)
        self.name = name
        self.address = address
        self.last_packet_timestamp = time.perf_counter()
        self.location = numpy.array((0.0, 0.0, 0.0))
        self.velocity = numpy.array((0.0, 0.0, 0.0))
        self.player_velocity_tick = 0
        self.malus = 0
        self.send_queue = []
        self.color_player = -1
        self.start_pos = [x,y,z]
        self.set_collider_rect(0.4,0.4, None)
        

    def tick(self):
        #print("{} ticked".format(self.name))
        self.malus = 0
        super().update()