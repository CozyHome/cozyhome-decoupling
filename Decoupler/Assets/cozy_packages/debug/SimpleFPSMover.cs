using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using com.cozyhome.Actors;
using com.cozyhome.Systems;

public class SimpleFPSMover : MonoBehaviour
{
    [SerializeField] [Range(0, 89.9F)] float _MaxVerticalAngle = 85F;
    [SerializeField] float _MaxSpeed = 12F;
    [SerializeField] ActorHeader.Actor _Actor;
    [SerializeField] Transform _View;

    void FixedUpdate()
    {
        if (!_Actor)
            return;
        else
        {
            _Actor._position = transform.position;

            Vector2 _mouse = new Vector2(
                Input.GetAxisRaw("Mouse X"),
                Input.GetAxisRaw("Mouse Y")
            );

            Quaternion R = LookRotate(
                _View.rotation,
                _mouse,
                _MaxVerticalAngle
            );

            _View.rotation = R;

            Cursor.lockState = CursorLockMode.Locked;

            Vector2 _input =
            new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

            Vector3 _wishvel = _View.rotation * new Vector3(_input[0], 0, _input[1]);
            _wishvel = Vector3.ClampMagnitude(_wishvel, 1.0F);

            _Actor.SetVelocity(_wishvel * _MaxSpeed);
            ActorHeader.Actor.Move(_Actor, GlobalTime.FDT);

            transform.position = _Actor._position;
        }
    }

    private Quaternion LookRotate(Quaternion _previous, Vector2 _delta, float _maxvertical)
    {
        Quaternion R = _previous;

        // measure the angular difference and adjust to clamped angle if need be:
        float _px = 90F - Vector3.Angle(
            _previous * new Vector3(0, 0, 1),
            new Vector3(0, 1, 0)
        );

        _delta[0] *= (360F * GlobalTime.FDT);
        _delta[1] *= (360F * GlobalTime.FDT);

        Vector3 fwd = R * new Vector3(0, 0, 1);

        float _nextx = -_delta[1];

        // if (cur angle + delta angle) > clamp angle
        // subtract difference from delta and apply
        if (_px - _nextx > _maxvertical)
            _nextx = -(_maxvertical - _px);

        // do the same for the opposite axis
        else if (_px - _nextx < -_maxvertical)
            _nextx = -(-_maxvertical - _px);

        fwd = Quaternion.AngleAxis(
                _nextx,
                R * new Vector3(1, 0, 0)
            ) * fwd;

        fwd = Quaternion.AngleAxis(
            _delta[0],
            new Vector3(0, 1, 0)
        ) * fwd;

        R = Quaternion.LookRotation(fwd);

        return R;
    }
}
