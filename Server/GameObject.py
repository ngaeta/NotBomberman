import pygame
from ColliisonsDetection.Circle import Circle
from ColliisonsDetection.CollisionMng import CollisionMng


class GameObject:

    static_id_counter = 0

    def __init__(self, x, y, z):
        self.x = x
        self.y = y
        self.z = z
        self.rect = None
        self.circle = None
        self.on_collision_enter = None
        self.__id = GameObject.static_id_counter
        GameObject.static_id_counter = GameObject.static_id_counter + 1

    def update(self):
        #print('{} {} {}'.format(self.__class__.__name__, self.x, self.z))

        if self.circle is not None:
            self.circle.x = round(self.x)
            self.circle.y = round(self.z)
            #print('Circle pos: {} {}'.format(self.circle.x, self.circle.y))

        if self.rect is not None:
            self.rect.x = round(self.x)
            self.rect.y = round(self.z)
            #print('Rect pos: {} {}'.format(self.rect.x, self.rect.y))


    @property
    def id(self):
        return self.__id

    def set_collider_rect(self, width, height, on_collision_enter):
        """Set rect collider and remove the others"""
        self.circle = None
        if self.rect is None:
            CollisionMng.add_collider(self)
        self.rect = pygame.Rect(round(self.x), round(self.z), width, height)
        self.on_collision_enter = on_collision_enter

    def set_collider_circle(self, radius, on_collision_enter):
        """Set circle collider and remove the others"""
        self.rect = None
        if self.circle is None:
            CollisionMng.add_collider(self)
        self.circle = Circle(round(self.x), round(self.z), radius)
        self.on_collision_enter = on_collision_enter

    def remove_collider(self):
        self.rect = self.circle = None
        self.on_collision_enter = None
        CollisionMng.remove_collider(self)

    #override this for destroy object
    def destroy(self):
        self.remove_collider()

        

