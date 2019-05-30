import pygame
from CollisionsDetection.Circle import Circle
from CollisionsDetection.CollisionMng import CollisionMng


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
        """Set gameobject position"""
        pass

    @property
    def id(self):
        return self.__id

    def set_collider_rect(self, width, height, on_collision_enter):
        """Set rect collider and remove the others"""
        self.circle = None
        if self.rect is None:
            CollisionMng.go_list.append(self)
        self.rect = pygame.Rect(self.x, self.y, width, height)
        self.on_collision_enter = on_collision_enter

    def set_collider_circle(self, radius, on_collision_enter):
        """Set circle collider and remove the others"""
        self.rect = None
        if self.circle is None:
            CollisionMng.go_list.append(self)
        self.circle = Circle(self.x, self.y, radius)
        self.on_collision_enter = on_collision_enter

    def remove_collider(self):
        self.rect = self.circle = None
        self.on_collision_enter = None
        CollisionMng.go_list.remove(self)

        

