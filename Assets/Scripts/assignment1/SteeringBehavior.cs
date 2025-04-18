using UnityEngine;
using System.Collections.Generic;
using TMPro;
using System.Collections;
using System;
using UnityEngine.Rendering;

public class SteeringBehavior : MonoBehaviour
{
    public Vector3 target;
    public KinematicBehavior kinematic;
    public List<Vector3> path;
    // you can use this label to show debug information,
    // like the distance to the (next) target
    public TextMeshProUGUI label;
    public TextMeshProUGUI Speedometer;

    public float MaxSpeed = 15.0f;
    public float HairPinMaxSpeed = 5.0f;
    public float HairPinAngle = 150.0f;
    public float MinSpeed = -4.0f;
    public float Acceleration = 4f;
    public float TurnRate = 10f;
    public float DeltaTurnRate = 5f;
    public float WayPointRadius = 10f;

    private string state = "stopped";
    private float speed = 0;
    private float turnRate = 0;
    private float heading = 0;
    private float targetheading = 0;
    
    private float currentspeed = 0;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        kinematic = GetComponent<KinematicBehavior>();
        target = transform.position;
        path = null;
        EventBus.OnSetMap += SetMap;
    }

    // Update is called once per frame
    void Update()
    {
        // Assignment 1: If a single target was set, move to that target
        //If a path was set, follow that path ("tightly")
        heading = this.transform.rotation.eulerAngles.y;
        
        float roundedspeed = (float)System.Math.Round(kinematic.speed, 2);
        Speedometer.text =  roundedspeed + " m/s";


        targetheading = Vector3.SignedAngle(new Vector3(0, 0, 1), target - transform.position, new Vector3(0, 1, 0));
        
        if (targetheading < 0){
            targetheading += 360;
        }


        float dif = angleDif(heading, targetheading);
        //Debug.Log(System.Math.Abs(dif));
        float dir = 0;

        if (dif > 0) { 
            dir = dif/System.Math.Abs(dif);
        }

        //Debug.Log("heading: " + heading);
        //Debug.Log("target: " + targetheading);
        //Debug.Log("dif: " + dif);

        if (path != null && path.Count > 0)
        {
            Debug.Log("Found path of length " + path.Count + ", going to next waypoint...");
        }

            switch (state)
        {
            case "stopped":
                if(DistanceToWaypoint(target) > WayPointRadius)
                {
                    state = "driving";
                    
                    Debug.Log("Driving to target");
                }

                else if(path != null && path.Count > 0)
                {
                    
                    //Debug.Log("Found path of length " + path.Count + ", going to next waypoint...");
                    target = NextWaypoint();
                    //Debug.Log(path.Count + " waypoints remain...");
                }
                
                break;


            case "driving":

                float speedDif = MaxSpeed - HairPinMaxSpeed;
                float turnFactor = System.Math.Min(System.Math.Abs(dif), HairPinAngle)/ HairPinAngle;
                //speed = Tween(speed, MaxSpeed, Acceleration);
                kinematic.SetDesiredSpeed(MaxSpeed - (speedDif * turnFactor));

                if (System.Math.Abs(dif) > 1)
                {
                    //Debug.Log("Dif: " + dif);
                    turnRate = Tween(turnRate, TurnRate, DeltaTurnRate);
                    
                    //add rotatetween function
                    heading = RotateTween(heading, targetheading, turnRate * kinematic.speed);
                    transform.eulerAngles = new Vector3(0, heading, 0);
                }
                else
                {
                    turnRate = 0;
                }

                if (DistanceToWaypoint(target) < WayPointRadius)
                {
                    //Debug.Log("Arriving at target: " + DistanceToWaypoint(target) );
                    state = "arriving";
                    currentspeed = kinematic.speed;
                    
                }

                break;
                
                
            case "arriving":
                kinematic.SetDesiredSpeed(0);
                kinematic.speed = Math.Min(currentspeed * DistanceToWaypoint(target) / WayPointRadius, MaxSpeed);
               
                if(kinematic.speed <= currentspeed * 0.1)
                {
                    turnRate = 0;
                    state = "stopped";
                    currentspeed = 0;
                    speed = 0;
                    kinematic.SetDesiredSpeed(0);
                    //break;
                    
                }
                
                if (System.Math.Abs(dif) > 0.1)
                {
                    //Debug.Log("Dif: " + dif);
                    turnRate = Tween(turnRate, TurnRate, DeltaTurnRate);
                    heading = RotateTween(heading, targetheading, turnRate * kinematic.speed);
                    transform.eulerAngles = new Vector3(0, heading, 0);
                }
                else
                {
                    turnRate = 0;
                }

                if (path != null && path.Count > 0)
                {
                    Debug.Log("Found path of length " + path.Count + ", going to next waypoint...");
                    target = NextWaypoint();

                    //Debug.Log(path.Count + " waypoints remain...");
                }

                if (DistanceToWaypoint(target) > WayPointRadius)
                {
                    state = "driving";

                    Debug.Log("Driving to target");
                }

                break;


            default:
                break;
            
        }
        Debug.Log(state);    

        // you can use kinematic.SetDesiredSpeed(...) and kinematic.SetDesiredRotationalVelocity(...)
        //    to "request" acceleration/decceleration to a target speed/rotational velocity
    }

    private float DistanceToWaypoint(Vector3 waypoint)
    {
        return ( (float)( System.Math.Sqrt(System.Math.Pow( (gameObject.transform.position.x - waypoint.x), 2) + System.Math.Pow( (gameObject.transform.position.z - waypoint.z), 2) )) );
    }

    private float Tween(float current, float target, float rate) {
        float newVal = 0;
        float dif =  target - current;
        if (dif == 0)
        {
            return current;
        }
        
        float dir = dif / System.Math.Abs(dif);
        //Debug.Log("Dir: " + dir);

        newVal = current + (rate * dir * Time.deltaTime);

        if (newVal * dir > target * dir)
        {
            newVal = target;
        }


        return newVal;
    }

    private float RotateTween(float current, float target, float rate) {
        float newVal = 0;


        if (current > 180)
        {
            current -= 360;
        }


        float dif =  target - current;
        
        if (dif == 0)
        {
            return current;
        }

        if (dif > 180){
            //Debug.Log(dif);
            dif -= 360;
            target -= 360;
        }
        
        float dir = dif / System.Math.Abs(dif);
        //Debug.Log("Dir: " + dir);

        //Debug.Log("heading: " + current + ", target heading: " + target + ", difference: " + dif);

        newVal = current + (rate * dir * Time.deltaTime);

        if (newVal * dir > target * dir)
        {
            newVal = target;
        }

        if (newVal < 0)
        {
            newVal += 360;
        }

        return newVal;
    }

    private float angleDif(float current, float target) {
        if (current > 180)
        {
            current -= 360;
        }


        float dif = target - current;

        if (dif > 180)
        {
            //Debug.Log(dif);
            dif -= 360;
            target -= 360;
        }

        return dif;
    }
    /*private void Accelerate(float target)
    {
        speed = speed 
        
        kinematic.SetDesiredSpeed(speed);
    }*/


    public void SetTarget(Vector3 target)
    {
        this.target = target;
        EventBus.ShowTarget(target);
    }

    public void SetPath(List<Vector3> path)
    {
        this.path = path;
    }

    public void SetMap(List<Wall> outline)
    {
        this.path = null;
        this.target = transform.position;
    }

    public Vector3 NextWaypoint() 
    {
        if(path.Count == 0)
        {
            throw new Exception("Path is empty!");
        }
        Vector3 next = path[0];
        path.RemoveAt(0);
        if (path.Count == 0)
        {
            path = null;
        }
        return next;
    }

}

