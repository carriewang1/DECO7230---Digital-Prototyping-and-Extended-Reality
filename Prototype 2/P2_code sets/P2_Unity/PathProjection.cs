// This script was made by my self
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathProjection : MonoBehaviour
{
    LineRenderer lr;
    Rigidbody rb;
    Vector3 startPosition;
    Vector3 startVelocity;
    float InitialForce = 15;
    float InitialAngle = -45;
    Quaternion rot;
    int i = 0;
    int NumberOfPoints = 50;
    float timer = 0.1f;

    // Start is called before the first frame update
    void Start()
    {
        lr = GetComponent<LineRenderer>();
        rb = GetComponent<Rigidbody>();
        rot = Quaternion.Euler(InitialAngle, 0, 0);

    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            drawline();
        }
        if (Input.GetKeyUp(KeyCode.Space))
        {
            rb.AddForce(rot * (InitialForce * transform.forward), ForceMode.Impulse);
            //lr.enabled=false;
        }

    }

    private void drawline()
    {
        i = 0;
        lr.positionCount = NumberOfPoints;
        lr.enabled = true;
        startPosition = transform.position;
        startVelocity = rot * (InitialForce * transform.forward) / rb.mass;
        lr.SetPosition(i, startPosition);
        for (float j = 0; i < lr.positionCount - 1; j += timer)
        {
            i++;
            Vector3 linePosition = startPosition + j * startVelocity;
            linePosition.y = startPosition.y + startVelocity.y * j + 0.5f * Physics.gravity.y * j * j;
            lr.SetPosition(i, linePosition);
        }
    }
}