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
    private float _boostFuel;
    [SerializeField] private float boostDrain;
    [SerializeField] private float boostRecover;
    [SerializeField] private float minBoost;
    [SerializeField] private TMP_Text boostText;
    public Status Status { get; set; }

    private InputAction _moveAction;
    private InputAction _boostAction;
    private InputAction _brakeAction;
    private InputAction _pauseAction;
    
    [SerializeField] private AudioSource engineSound;
    [SerializeField] private AudioSource crashSound;

    private bool _crashed;
    
    void Start()
    {
        gm = GameObject.Find("GameManager").GetComponent<GameManager>();
        
        _boostFuel = PlayerPrefs.GetFloat("Boost", 100f);
        // saving enum to playerprefs is annoying
        string savedStatus = PlayerPrefs.GetString("Status");
        if (!Enum.TryParse<Status>(savedStatus, out Status result))
        {
            Status = Status.COASTING;
        }
        
        _moveAction = InputSystem.actions.FindAction("Move");
        _boostAction = InputSystem.actions.FindAction("Boost");
        _brakeAction = InputSystem.actions.FindAction("Brake");
        _pauseAction = InputSystem.actions.FindAction("Pause");

        _crashed = false;
    }

    void Update()
    {
        if (_pauseAction.WasPressedThisDynamicUpdate())
        {
            gm.PauseGame();
        }
    }

    void FixedUpdate()
    {
        Move();
        CheckBounds();
    }

    private void Move()
    {
        Vector2 input = _moveAction.ReadValue<Vector2>();
        VehicleShift = input.y * speed;
        Vector3 movement = new Vector3(input.x * speed, 0, 0);
        rb.AddForce(movement);

        bool boostPressed = _boostAction.IsPressed();
        if (boostPressed && _boostFuel > 0f)
        {
            if (Status != Status.RECOVERING && Status != Status.BRAKING)
            {
                Status = Status.BOOSTING;
                _boostFuel -= boostDrain * Time.deltaTime;
                if (engineSound.pitch < 1f) engineSound.pitch += Time.deltaTime;
                if (engineSound.pitch > 1f) engineSound.pitch = 1f;
                if (_boostFuel < 0f)
                {
                    _boostFuel = 0f;
                    Status = Status.RECOVERING;
                }
            }
        }
        else
        {
            bool brakePressed = _brakeAction.IsPressed();
            if (_boostFuel < 100f) _boostFuel += boostRecover * Time.deltaTime;
            if (_boostFuel > 100f) _boostFuel = 100f;
            if (brakePressed)
            {
                Status = Status.BRAKING;
                if (engineSound.pitch > 0.65f) engineSound.pitch -= Time.deltaTime;
                if (engineSound.pitch < 0.65f) engineSound.pitch = 0.65f;
            }
            else
            {
                if (engineSound.pitch > 0.75f) engineSound.pitch -= Time.deltaTime;
                if (engineSound.pitch < 0.75f) engineSound.pitch = 0.75f;
                if (_boostFuel < minBoost)
                {
                    Status = Status.RECOVERING;
                }
                else
                {
                    Status = Status.COASTING;
                }
            }
        }
        boostText.text = $"{(int)_boostFuel}";
    }

    private void CheckBounds()
    {
        if (!_crashed && (transform.position.x > 5f || transform.position.x < -5f))
        {
            Crash();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Vehicle"))
        {
            Crash();
        }
    }

    private void Crash()
    {
        _crashed = true;
        crashSound.Play();
        engineSound.Stop();
        gm.GameOver();
    }

    public void Reset()
    {
        rb.linearVelocity = Vector3.zero;
        rb.Sleep();
        rb.WakeUp();
        transform.position = new Vector3(2.5f, 0f, -25f);
        _boostFuel = 100f;
        Status = Status.COASTING;
        _crashed = false;
        engineSound.pitch = 0.75f;
        engineSound.Play();
    }

    public void SavePrefs()
    {
        PlayerPrefs.SetFloat("Boost", _boostFuel);
        PlayerPrefs.SetString("Status", Status.ToString());
    }
}
