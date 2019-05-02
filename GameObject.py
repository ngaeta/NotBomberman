import pygame
from ServerCollisionsDetection.Circle import Circle
from ServerCollisionsDetection.CollisionMng import CollisionMng


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

    @property
    def id(self):
        return self.__id

    def set_collider_rect(self, width, height, on_collision_enter):
        self.rect = pygame.Rect(self.x, self.y, width, height)
        CollisionMng.go_list.append(self)
        if on_collision_enter is not None:
            if self.on_collision_enter is not None:
                CollisionMng.go_list.remove(self)
            self.on_collision_enter = on_collision_enter

    def set_collider_circle(self, radius, on_collision_enter):
        self.circle = Circle(self.x, self.y, radius)
        CollisionMng.go_list.append(self)
        if on_collision_enter is not None:
            if self.on_collision_enter is not None:
                CollisionMng.go_list.remove(self)
            self.on_collision_enter = on_collision_enter


        

