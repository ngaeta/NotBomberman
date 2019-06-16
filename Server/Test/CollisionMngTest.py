from GameObject import GameObject
from ColliisonsDetection.CollisionMng import CollisionMng

def clear_collision_mng():
    CollisionMng.__colliders__.clear()


def test_list_colliders():
    clear_collision_mng()
    go1 = GameObject(4.0, 4.0, 0)
    go2 = GameObject(4.0, 4.0, 0)
    go1.set_collider_circle(2.0, lambda x: x.id * x.id)
    assert len(CollisionMng.__colliders__) is 1

    clear_collision_mng()
    go1 = GameObject(4.0, 4.0, 0)
    go2 = GameObject(4.0, 4.0, 0)
    go1.set_collider_circle(2.0, lambda x: x.id * x.id)
    go2.set_collider_rect(2, 2, lambda x: print(x.id))
    assert len(CollisionMng.__colliders__) is 2

    clear_collision_mng()
    go1 = GameObject(4.0, 4.0, 0)
    go2 = GameObject(4.0, 4.0, 0)
    assert len(CollisionMng.__colliders__) is 0

def test_collision_mng_update() :
    clear_collision_mng()
    collider_id = None

    def on_collision(collider):
        collider_id = collider.id

    go1 = GameObject(40, 40, 0)
    go1.set_collider_circle(20, on_collision)
    go2 = GameObject(40, 40, 0)
    go2.set_collider_rect(20, 20, None)
    CollisionMng.update()
    assert collider_id is go2.id


#test_list_colliders()
test_collision_mng_update()