using System;
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
    [SerializeField] private float spawnForwardMax;
    private GameObject _lastVehicle;

    private Queue<GameObject> _backwardVehicles;
    private float _nextBackwardSpawn;
    [SerializeField] private float spawnBackwardMin;
    [SerializeField] private float spawnBackwardMax;
    
    [SerializeField] private TMP_Text speedText;
    [SerializeField] private TMP_Text distanceText;
    [SerializeField] private GameObject gameOverScreen;
    
    void Start()
    {
        _distance = 0f;
        _numPrefabs = vehiclePrefabs.Length;
        _vehiclesRB = vehicles.GetComponent<Rigidbody>();
        _forwardVehicles = new Queue<GameObject>();
        _nextForwardSpawn = 0f; // forward spawn refers to z coord, while backward spawn refers to Time.time
        _lastVehicle = player; // this allows the first non-player vehicle to be spawned since player z will always be less than 0
        _backwardVehicles = new Queue<GameObject>();
        _nextBackwardSpawn = 0f;
    }

    void Update()
    {
        // Spawn Forward-facing vehicle
        if (_lastVehicle.transform.position.z <= _nextForwardSpawn)
        {
            float spacing = Random.Range(spawnForwardMin, spawnForwardMax);
            _nextForwardSpawn = 25f - spacing;
            _lastVehicle = Spawn(true, _forwardVehicles);
        }
        
        // Spawn Rear-facing vehicle
        if (Time.time >= _nextBackwardSpawn)
        {
            float spacing = Random.Range(spawnBackwardMin, spawnBackwardMax);
            _nextBackwardSpawn = Time.time + spacing;
            Spawn(false, _backwardVehicles);
        }

        // Depending on input, move other vehicles forward/back to simulate acceleration/deceleration
        Status playerStatus = player.GetComponent<PlayerController>().Status;
        float mod = 1f;
        if (playerStatus == Status.BOOSTING)
        {
            mod = boostPower;
        }
        else if (playerStatus == Status.BRAKING)
        {
            mod = -brakePower;
        }

        float speed = mod * player.GetComponent<PlayerController>().VehicleShift * verticalSpeed;
        _vehiclesRB.AddForce(speed * Vector3.back);
        if (_vehiclesRB.linearVelocity.magnitude > maxSpeed) _vehiclesRB.linearVelocity = _vehiclesRB.linearVelocity.normalized * maxSpeed;
        // _vehiclesRB.linearVelocity = Vector3.back * speed;
        speedText.text = $"{(-_vehiclesRB.linearVelocity.z + 60f):F0} MPH";
        _distance += 0.0167f * Time.deltaTime; //roughly 60mph (0.01666) repeating, not gonna calculate actual speed and time difference for now.
        distanceText.text = $"{MathF.Round(_distance, 2):F1} Miles";

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

    private GameObject Spawn(bool forward, Queue<GameObject> vehiclesQ)
    {
        int i = Random.Range(0, _numPrefabs);
        
        float xOffset = (forward ? 2.5f : -2.5f);
        Vector3 pos = new Vector3(xOffset, 0f, 25f);
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
        _lastVehicle = player;
        foreach (GameObject vehicle in _backwardVehicles) 
        {
            Destroy(vehicle);
        }
        _backwardVehicles.Clear();
        _nextBackwardSpawn = Time.time;
    }

    public void GameOver()
    {
        Time.timeScale = 0;
        gameOverScreen.SetActive(true);
    }

    public void Restart()
    {
        DespawnAll();
        vehicles.GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
        _distance = 0f;
        player.GetComponent<PlayerController>().Reset();
        gameOverScreen.SetActive(false);
        Time.timeScale = 1;
    }

    public void ReturnToMenu()
    {
        Time.timeScale = 1;
        SceneManager.LoadScene("Main Menu");
    }
}
