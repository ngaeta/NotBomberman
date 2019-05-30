import pygame


class Circle:

    def __init__(self, x, y, radius):
        self.x = x
        self.y = y
        self.radius = radius

    def contains(self, x, y):
        me = pygame.math.Vector2(self.x, self.y)
        point = pygame.math.Vector2(x, y)
        dist = pygame.math.Vector2.distance_to(point, me)
        return dist <= self.radius

    def collides_with_rect(self, x, y, width, height):
        left = x - width/2
        right = x + width/2
        top = y - height/2
        bottom = y + height/2

        nearest_x = max(left, min(self.x, right))
        nearest_y = max(top, min(self.y, bottom))
        delta_x = self.x - nearest_x
        delta_y = self.y - nearest_y

        return (delta_x * delta_x + delta_y * delta_y) <= self.radius * self.radius

    def collides_with_rect(self, rect):
        half_width = rect.w / 2
        half_height = rect.h / 2

        left = rect.x - half_width
        right = rect.x + half_width
        top = rect.y - half_height
        bottom = rect.y + half_height

        nearest_x = max(left, min(self.x, right))
        nearest_y = max(top, min(self.y, bottom))
        delta_x = self.x - nearest_x
        delta_y = self.y - nearest_y

        return (delta_x * delta_x + delta_y * delta_y) <= self.radius * self.radius

