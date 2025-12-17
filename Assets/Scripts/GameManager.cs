using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

public class GameManager : MonoBehaviour
{
/*
    [SerializeField] private GameObject hatchbackPrefab;
    [SerializeField] private GameObject pickupPrefab;
    [SerializeField] private GameObject policePrefab;
    [SerializeField] private GameObject taxiPrefab;
    [SerializeField] private GameObject towtruckPrefab;
    [SerializeField] private GameObject truckPrefab;
    [SerializeField] private GameObject vanPrefab;
    [SerializeField] private GameObject vanbigPrefab;
  */

    private int _score;
    [SerializeField] private TMP_Text scoreText;

    [SerializeField] private GameObject player;
    [SerializeField] private float verticalSpeed;
    [SerializeField] private float boostPower;
    [SerializeField] private float brakePower;
    [SerializeField] private float maxSpeed;
    private float _distance;
    
    [SerializeField] private GameObject[] vehiclePrefabs;
    private int _numPrefabs;
    [SerializeField] private GameObject vehicles;
    private Rigidbody _vehiclesRB;
    [SerializeField] private float vehicleSpeed;
    
    private Queue<GameObject> _forwardVehicles;
    private float _nextForwardSpawn;
    [SerializeField] private float spawnForwardMin;
    public float spawnForwardMax;
    private GameObject _lastForwardVehicle;
    private GameObject _lastBackwardVehicle;

    private Queue<GameObject> _backwardVehicles;
    private float _nextBackwardSpawn;
    [SerializeField] private float spawnBackwardMin;
    public float spawnBackwardMax;
    
    [SerializeField] private TMP_Text speedText;
    [SerializeField] private TMP_Text distanceText;
    [SerializeField] private GameObject pauseScreen;
    [SerializeField] private GameObject gameOverText;
    [SerializeField] private GameObject restartButton;
    [SerializeField] private GameObject resumeButton;
    [SerializeField] private GameObject saveButton;
    
    void Start()
    {
        _score = PlayerPrefs.GetInt("Score", 0);
        ChangeScore(0);
        _distance = PlayerPrefs.GetFloat("Distance", 0f);
        _numPrefabs = vehiclePrefabs.Length;
        _vehiclesRB = vehicles.GetComponent<Rigidbody>();
        _forwardVehicles = new Queue<GameObject>();
        _nextForwardSpawn = 0f; // forward spawn refers to z coord, while backward spawn refers to Time.time
        _lastForwardVehicle = player; // this allows the first non-player vehicle to be spawned since player z will always be less than 0
        _lastBackwardVehicle = player; // this allows the first non-player vehicle to be spawned since player z will always be less than 0
        _backwardVehicles = new Queue<GameObject>();
        _nextBackwardSpawn = 0f;
    }

    void FixedUpdate()
    {
        // Spawn Forward-facing vehicle
        if (_lastForwardVehicle.transform.position.z <= _nextForwardSpawn)
        {
            float spacing = Random.Range(spawnForwardMin, spawnForwardMax);
            _nextForwardSpawn = 25f - spacing;
            _lastForwardVehicle = Spawn(true, _forwardVehicles);
        }
        
        // Spawn Rear-facing vehicle
        if (_lastBackwardVehicle.transform.position.z <= _nextBackwardSpawn)
        {
            float spacing = Random.Range(spawnBackwardMin, spawnBackwardMax);
            _nextBackwardSpawn = 25f - spacing;
            _lastBackwardVehicle = Spawn(false, _backwardVehicles);
        }

        // Depending on input, move other vehicles forward/back to simulate acceleration/deceleration
        Status playerStatus = player.GetComponent<PlayerController>().Status;
        float mod = -2.5f;
        if (playerStatus == Status.BOOSTING)
        {
            mod = boostPower;
        }
        else if (playerStatus == Status.BRAKING)
        {
            mod = -brakePower;
        }
        float input = player.GetComponent<PlayerController>().VehicleShift;
        float speed = (mod + input) * verticalSpeed;
        _vehiclesRB.AddForce(speed * Vector3.back);
        if (_vehiclesRB.linearVelocity.magnitude > maxSpeed) _vehiclesRB.linearVelocity = _vehiclesRB.linearVelocity.normalized * maxSpeed;
        speedText.text = $"{(-_vehiclesRB.linearVelocity.z + 60f):F0} MPH";
        _distance += 0.0167f * Time.deltaTime; //roughly 60mph (0.01666) repeating, not gonna calculate actual speed and time difference for now.
        distanceText.text = $"{_distance:F1} Miles";

        ResetVehiclesObject();
    }

    private void ResetVehiclesObject()
    {
        // As time goes on the parent object with the rigidbody moving all of the vehicles will go infinitely far back.
        // This is attempting to reset it once it reaches 100 so that there are no floating point errors

        float offset = vehicles.transform.position.z;
        if (offset < -100f)
        {
            Vector3 pos = vehicles.transform.position;
            vehicles.transform.position = new Vector3(pos.x, pos.y, 100f);
            foreach (GameObject vehicle in _forwardVehicles) vehicle.transform.Translate(Vector3.back * (-offset + 100f));
            foreach (GameObject vehicle in _backwardVehicles) vehicle.transform.Translate(Vector3.forward * (-offset + 100f));
        }
    }

    public void ChangeScore(int score)
    {
        _score += score;
        scoreText.text = $"Score: {_score}";
    }

    private GameObject Spawn(bool forward, Queue<GameObject> vehiclesQ)
    {
        int i = Random.Range(0, _numPrefabs);
        
        float xOffset = (forward ? 2.5f : -2.5f);
        float randOffset = Random.Range(-1f, 1f);
        Vector3 pos = new Vector3(xOffset + randOffset, 0f, 25f);
        float yRotation = (forward ? 0f : 180f);
        Quaternion rot = Quaternion.Euler(0f, yRotation, 0f);
        GameObject vehicle = Instantiate(vehiclePrefabs[i], vehicles.transform);
        vehicle.transform.position = pos;
        vehicle.transform.rotation = rot;
        
        Vehicle vehicleScript = vehicle.GetComponent<Vehicle>(); 
        vehicleScript.GameManager = this;
        vehicleScript.Forward = forward;
        vehicleScript.Speed = vehicleSpeed; //only matters for backwards cars
        
        vehiclesQ.Enqueue(vehicle);

        return vehicle;
    }

    public void Despawn(bool forward)
    {
        GameObject vehicle;
        if (forward)
        {
            vehicle = _forwardVehicles.Dequeue();
        }
        else
        {
            vehicle = _backwardVehicles.Dequeue();
        }

        Destroy(vehicle);
    }

    private void DespawnAll()
    {
        foreach (GameObject vehicle in _forwardVehicles)
        {
            Destroy(vehicle);
        }
        _forwardVehicles.Clear();
        _nextForwardSpawn = 0f;
        _lastForwardVehicle = player;
        foreach (GameObject vehicle in _backwardVehicles) 
        {
            Destroy(vehicle);
        }
        _backwardVehicles.Clear();
        _nextBackwardSpawn = 0f;
        _lastBackwardVehicle = player;
    }

    public void GameOver()
    {
        Time.timeScale = 0;
        // ChangeScore(-_score);
        _score = 0;
        // display distance at end of game, but reset in background so it cant be recovered from playerprefs
        float temp = _distance;
        _distance = 0f;
        PlayerPrefs.DeleteAll();
        pauseScreen.SetActive(true);
        gameOverText.SetActive(true);
        restartButton.SetActive(true);
        resumeButton.SetActive(false);
        saveButton.SetActive(false);
        distanceText.text = $"{temp:F1} Miles";
    }

    public void Restart()
    {
        DespawnAll();
        ChangeScore(0);
        vehicles.GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
        vehicles.GetComponent<Rigidbody>().Sleep();
        vehicles.GetComponent<Rigidbody>().WakeUp();
        player.GetComponent<PlayerController>().Reset();
        gameOverText.SetActive(false);
        restartButton.SetActive(false);
        resumeButton.SetActive(true);
        saveButton.SetActive(true);
        pauseScreen.SetActive(false);
        Time.timeScale = 1;
    }

    public void ReturnToMenu()
    {
        SavePrefs();
        Time.timeScale = 1;
        SceneManager.LoadScene("Main Menu");
    }

    public void PauseGame()
    {
        Time.timeScale = 0;
        pauseScreen.SetActive(true);
    }

    public void ResumeGame()
    {
        Time.timeScale = 1;
        pauseScreen.SetActive(false);
    }

    public void SavePrefs()
    {
        // Shouldn't use this normally since it is not secure, anyone can go in and change their values but for this i trust you.
        PlayerPrefs.SetInt("Score", _score);
        PlayerPrefs.SetFloat("Distance", _distance);
        player.GetComponent<PlayerController>().SavePrefs();
        PlayerPrefs.Save();
    }
}
