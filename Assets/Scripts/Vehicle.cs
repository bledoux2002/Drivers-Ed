using UnityEngine;

public class Vehicle : MonoBehaviour
{
    public GameManager GameManager { get; set; }
    public bool Forward { get; set; }
    public float Speed { get; set; }

    void Update()
    {
        if (!Forward) transform.Translate(Time.deltaTime * Speed * Vector3.forward);
        if (transform.position.z < -35f)
        {
            GameManager.Despawn(Forward);
        }
    }
}
