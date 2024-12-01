using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using static UnityEngine.ParticleSystem;
using System.Reflection;


public class AgentController : Agent
{
    [SerializeField] float speed = 5.0f;
    private float turnSpeed = 180.0f;
    Vector3 spawnPoint;

    [SerializeField] Transform collectiblesParent;
    private List<Transform> collectibles = new List<Transform>();
    [SerializeField] Transform ChargingStationPos;

    [SerializeField] Transform cameraObj;
    Vector3 cameraPos;

    private int remainingCollectibles;
    private bool isMovingBackward;
    public override void OnEpisodeBegin()
    {
        collectibles.Clear();
        ChargingStationPos.gameObject.SetActive(false);
        foreach (Transform child in collectiblesParent)
        {
            collectibles.Add(child);
        }

        foreach (var collectible in collectibles)
        {
            collectible.gameObject.SetActive(true);
        }
        spawnPoint = new Vector3(6f, 0.01f, -8.5f);
        transform.localPosition = spawnPoint;
        transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
        remainingCollectibles = collectibles.Count;

        cameraPos = new Vector3(0f, 1.2f, -1.4f);
        cameraObj.transform.localPosition = cameraPos;
        cameraObj.transform.localRotation = Quaternion.Euler(25f, 1.3f, 0.2f);

        Debug.Log("Reamining dirty blobs: " + remainingCollectibles + ".");
    }
    public override void CollectObservations(VectorSensor sensor)
    {
        // Agent position
        sensor.AddObservation(transform.localPosition.x);
        sensor.AddObservation(transform.localPosition.z);
        sensor.AddObservation(transform.localRotation.y);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {

        var actionTaken = actions.ContinuousActions;

        float _actionSpeed = (actionTaken[0] + 1) / 2;
        //float _actionSpeed = Mathf.Max(0, actionTaken[0]);
        float _actionSteering = actionTaken[1];
        Vector3 movement = _actionSpeed * Vector3.forward * speed * Time.deltaTime;
        transform.Translate(movement);

        transform.Rotate(Vector3.up, _actionSteering * turnSpeed * Time.deltaTime);

        isMovingBackward = Vector3.Dot(movement, transform.forward) < 0;
        AddReward(-0.01f);

    }
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        ActionSegment<float> actions = actionsOut.ContinuousActions;
        actions[0] = -1; // Vertical
        actions[1] = 0; // Horizontal

        if (Input.GetKey(KeyCode.W))
        {
            actions[0] = +1;
        }
        if (Input.GetKey(KeyCode.S))
        {
            actions[0] = -1;
        }
        if (Input.GetKey(KeyCode.A))
        {
            actions[1] = -1;
        }
        if (Input.GetKey(KeyCode.D))
        {
            actions[1] = +1;
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Wall"))
        {
            AddReward(-20);
            EndEpisode();
        }
        if (other.CompareTag("Dust"))
        {
            AddReward(isMovingBackward ? -1 : 2);
            other.gameObject.SetActive(false);
            remainingCollectibles--;
            Debug.Log("Reamining dirty blobs: " + remainingCollectibles + ".");
            if (remainingCollectibles == 0)
            {
                AddReward(10);
                ChargingStationPos.gameObject.SetActive(true);
                Debug.Log("House cleaned! Returning to charging station.");
            }
        }
        if (other.CompareTag("Station"))
        {
            AddReward(20);
            Debug.Log("Returned to the charging station. Powering off...");
            EndEpisode();
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("POV"))
        {
            cameraPos = new Vector3(-0.02f, 0.5f, 0.65f);
            cameraObj.transform.localPosition = cameraPos;
            cameraObj.transform.localRotation = Quaternion.Euler(5.5f, -0.5f, -1f);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("POV"))
        {
            cameraPos = new Vector3(0f, 1.2f, -1.4f);
            cameraObj.transform.localPosition = cameraPos;
            cameraObj.transform.localRotation = Quaternion.Euler(25f, 1.3f, 0.2f);
        }
    }
}
