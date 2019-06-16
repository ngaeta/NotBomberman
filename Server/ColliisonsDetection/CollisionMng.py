
class CollisionMng:

    __colliders__ = []
    __removecolliders__ = []

    @staticmethod
    def update():
        list_count = len(CollisionMng.__colliders__)
        for i in range(0, list_count):
            first = CollisionMng.__colliders__[i]
            for j in range(i + 1, list_count):
                second = CollisionMng.__colliders__[j]

                check_first = False
                check_second = False

                if first.on_collision_enter is not None:
                    if first.circle is not None and second.rect is not None:
                        check_first = first.circle.collides_with_rect(second.rect)
                    elif first.rect is not None and second.rect is not None:
                        check_first = first.rect.colliderect(second.rect)

                if second.on_collision_enter is not None:
                    if second.circle is not None and first.rect is not None:
                        check_second = second.circle.collides_with_rect(first.rect)
                    elif second.rect is not None and first.rect is not None:
                        check_second = second.rect.colliderect(first.rect)

                if check_first:
                    first.on_collision_enter(second)
                if check_second:
                    second.on_collision_enter(first)

        for i in range(0, len(CollisionMng.__removecolliders__)):
            CollisionMng.__colliders__.remove(CollisionMng.__removecolliders__[i])
        CollisionMng.__removecolliders__.clear()

    @staticmethod
    def add_collider(collider):
        CollisionMng.__colliders__.append(collider)

    @staticmethod
    def remove_collider(collider):
        print("Remove " + str(collider.__class__.__name__))
        CollisionMng.__removecolliders__.append(collider)
