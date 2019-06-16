import GameObject


class GameExplosion(GameObject.GameObject):

    def __init__(self, x, y, z, radius):
        super().__init__(x, y, z)
        self.set_collider_circle(radius, self.onCollisionEnter)
        self.time_explosion = 0.5

    #call tick in gameserver
    def tick(self, delta_time):
        if self.time_explosion > 0:
            self.time_explosion -= delta_time
            if self.timer_dead <= 0:
                print("EXplosione finita")
                self.destroy()

    def onCollisionEnter(self, collider):
        print("COLLISIONEEEEEEEEEEEEEEEEEEEEEEEEEE")
        #invio pacchetto ai client morti
        #collider.destroy()