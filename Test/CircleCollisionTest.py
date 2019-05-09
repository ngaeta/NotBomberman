from GameObject import GameObject


def test_circle_rect_collision():
    go1 = GameObject(4.0, 4.0, 0)
    go2 = GameObject(4.0, 4.0, 0)
    go1.set_collider_circle(2.0, None)
    go2.set_collider_rect(2.0, 2.0, None)
    assert go1.circle.collides_with_rect(go2.rect) is True

    go1 = GameObject(20.0, 4.0, 0)
    go2 = GameObject(4.0, 4.0, 0)
    go1.set_collider_circle(2.0, None)
    go2.set_collider_rect(2.0, 2.0, None)
    assert go1.circle.collides_with_rect(go2.rect) is False

    go1 = GameObject(20.0, 4.0, 0)
    go2 = GameObject(4.0, 4.0, 0)
    go1.set_collider_circle(100, None)
    go2.set_collider_rect(2.0, 2.0, None)
    assert go1.circle.collides_with_rect(go2.rect) is True

    go1 = GameObject(20.0, 4.0, 0)
    go2 = GameObject(4.0, 4.0, 0)
    go1.set_collider_circle(2.0, None)
    go2.set_collider_rect(50.0, 2.0, None)
    assert go1.circle.collides_with_rect(go2.rect) is True


def test_circle_point_collision():
    go1 = GameObject(4, 4, 0)
    go1.set_collider_circle(3.0, None)
    assert go1.circle.contains(2, 2) is True

    go1 = GameObject(4, 4, 0)
    go1.set_collider_circle(2.0, None)
    assert go1.circle.contains(10, 4)is False

    go1 = GameObject(4, 4, 0)
    go1.set_collider_circle(7.0, None)
    assert go1.circle.contains(10, 4) is True


test_circle_rect_collision()
test_circle_point_collision()

