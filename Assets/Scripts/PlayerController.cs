using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public enum Status
{
    COASTING,
    BRAKING,
    BOOSTING,
    RECOVERING
}

public class PlayerController : MonoBehaviour
{
    private GameManager gm;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private float speed;
    public float VehicleShift { get; set; }
    private float boostFuel;
    [SerializeField] private float boostDrain;
    [SerializeField] private float boostRecover;
    [SerializeField] private float minBoost;
    [SerializeField] private TMP_Text boostText;
    public Status Status { get; set; }

    private InputAction moveAction;
    private InputAction boostAction;
    private InputAction brakeAction;
    
    [SerializeField] private AudioSource engineSound;
    [SerializeField] private AudioSource crashSound;
    
    void Start()
    {
        gm = GameObject.Find("GameManager").GetComponent<GameManager>();
        
        boostFuel = 100f;
        Status = Status.COASTING;
        
        moveAction = InputSystem.actions.FindAction("Move");
        boostAction = InputSystem.actions.FindAction("Boost");
        brakeAction = InputSystem.actions.FindAction("Brake");
    }

    void Update()
    {
        Move();
        CheckBounds();
    }

    private void Move()
    {
        Vector2 input = moveAction.ReadValue<Vector2>();
        VehicleShift = input.y;
        Vector3 movement = new Vector3(input.x * speed, 0, 0);
        rb.AddForce(movement);

        bool boostPressed = boostAction.IsPressed();
        if (boostPressed && boostFuel > 0f)
        {
            if (boostFuel >= minBoost && Status != Status.RECOVERING && Status != Status.BRAKING)
            {
                Status = Status.BOOSTING;
                boostFuel -= boostDrain * Time.deltaTime;
                if (engineSound.pitch < 1f) engineSound.pitch += Time.deltaTime;
                if (engineSound.pitch > 1f) engineSound.pitch = 1f;
                if (boostFuel < 0f)
                {
                    boostFuel = 0f;
                    Status = Status.RECOVERING;
                }
            }
        }
        else
        {
            bool brakePressed = brakeAction.IsPressed();
            if (boostFuel < 100f) boostFuel += boostRecover * Time.deltaTime;
            if (boostFuel > 100f) boostFuel = 100f;
            if (brakePressed)
            {
                Status = Status.BRAKING;
            }
            else if (boostFuel < minBoost)
            {
                Status = Status.RECOVERING;
            }
            else
            {
                Status = Status.COASTING;
            }
            if (engineSound.pitch > 0.75f) engineSound.pitch -= Time.deltaTime;
            if (engineSound.pitch < 0.75f) engineSound.pitch = 0.75f;
        }
        boostText.text = $"{(int)boostFuel}";
    }

    private void CheckBounds()
    {
        if (transform.position.x > 5f || transform.position.x < -5f)
        {
            gm.GameOver();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Vehicle"))
        {
            crashSound.Play();
            engineSound.Stop();
            gm.GameOver();
        }
    }

    public void Reset()
    {
        transform.position = new Vector3(2.5f, 0f, -25f);
        rb.linearVelocity = Vector3.zero;
        boostFuel = 100f;
        Status = Status.COASTING;
        engineSound.Play();
        engineSound.pitch = 0.75f;
    }
}
