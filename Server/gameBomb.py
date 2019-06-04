import time
import numpy
import GameObject

from gamePacket import Packet

class GameBomb(GameObject.GameObject):

    server = None

    def __init__(self, address, x, y, z):
        super().__init__(x, y, z)
        self.address = address
        self.location = numpy.array((0.0, 0.0, 0.0))
        self.timer_dead = 3
        self.radius = 4
        self._server = GameBomb.server
        self.dead = False


    def onCollisionEnter(self, collider):
        print(collider)
        print("COLLISIONEEEEEEEEEEEEEEEEEEEEEEEEEE")

    #TODO
    #Get deltaTime from self._server
    #Check collision bomb

    def tick(self, deltaTime):
        if self.dead == True:
            return

        self.timer_dead -= deltaTime
        if self.timer_dead < 0 and self.dead == False:
            self.set_collider_circle(self.radius, self.onCollisionEnter)
            print("One bomb is dead")
            self.dead = True
            timer_packet = Packet(False, self.address, "=BIf",6, super().id, self.timer_dead)
            self._server.send_all_queue.append(timer_packet)
        else:
            timer_packet = Packet(False, self.address, "=BIf",6, super().id, self.timer_dead)
            self._server.send_all_queue.append(timer_packet)
