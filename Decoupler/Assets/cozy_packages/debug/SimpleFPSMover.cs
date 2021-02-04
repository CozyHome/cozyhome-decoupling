using UnityEngine;

using com.cozyhome.Vectors;
using com.cozyhome.Actors;
using com.cozyhome.Systems;

public class SimpleFPSMover : MonoBehaviour, ActorHeader.IActorReceiver
{
    [SerializeField] private bool _pushrigidbodies;
    [SerializeField] [Range(0, 89.9F)] float _MaxVerticalAngle = 85F;
    [SerializeField] float _MaxSpeed = 12F;
    [SerializeField] ActorHeader.Actor _Actor;
    [SerializeField] Transform _View;
    [SerializeField] AudioSource _src;

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

            if (_Actor.Ground.stable)
            {
                Vector3 _rit = Vector3.Cross(_wishvel, R * new Vector3(0, 1, 0));
                _rit.Normalize();

                Vector3 _fwd = Vector3.Cross(_Actor.Ground.normal, _rit);
                _fwd.Normalize();

                _wishvel = _fwd * (_wishvel.magnitude);

                _Actor.SetVelocity(_wishvel * _MaxSpeed);
            }
            else
                _Actor.SetVelocity(_Actor._velocity - Vector3.up * GlobalTime.FDT * 39.62F);

            if (_Actor.Ground.stable && Input.GetAxis("Fire1") > 0)
            {
                _Actor.SetSnapEnabled(false);
                _Actor.SetVelocity(_Actor._velocity + Vector3.up * 10F);
            }

            ActorHeader.Move(this, _Actor, GlobalTime.FDT);

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

    public void OnGroundHit(in ActorHeader.GroundHit _ground, in ActorHeader.GroundHit _lastground, LayerMask _gfilter)
    { }

    public void OnTraceHit(in RaycastHit _trace, in Vector3 _position, in Vector3 _velocity)
    {
        bool _stbl = _Actor.DeterminePlaneStability(_trace.normal, _trace.collider);

        if (_stbl)
            _Actor.SetSnapEnabled(true);

        /*
        else
        {
            if (_pushrigidbodies)
            {
                Rigidbody _r = _trace.rigidbody;
                if (_r)
                {
                    const float _simulatedmass = 1F;
                    float _mr = (_simulatedmass / _r.mass);
                    _r.AddForceAtPosition(
                        _mr * VectorHeader.ProjectVector(_velocity, _trace.normal),
                        _trace.point,
                        ForceMode.Impulse
                    );

                    _src.PlayOneShot(_src.clip, 1F);
                }
            }
        }
        */
    }
}
