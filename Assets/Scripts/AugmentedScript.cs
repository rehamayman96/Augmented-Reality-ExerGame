using UnityEngine;
using System.Collections;
using TMPro;

public class AugmentedScript : MonoBehaviour
{
    private float originalLatitude;
    private float originalLongitude;
    private float currentLongitude;
    private float currentLatitude;

    public TextMeshProUGUI distanceText;
    public TextMeshProUGUI timeText;
    public TextMeshProUGUI speedText;
    private double distance;

    public GameObject sign;

    private bool setOriginalValues = true;
    private bool startedCalculation;

    private Vector3 targetPosition;
    private Vector3 originalPosition;

    private float speed = 0.1f;
    private int markersCount = 0;
    private ArrayList distanceList;
    private ArrayList timeList;
    private ArrayList speedList;
    private bool finished = true;

    private float originalTime;
    public GameObject startButton;

    private bool lockTracking;
    private float currentLock;
    private float maxLock;

    void Start()
    {
        StartCoroutine("GetCoordinates");
        //initialize target and original position
        targetPosition = transform.position;
        originalPosition = transform.position;

        distanceList = new ArrayList();
        timeList = new ArrayList();
        speedList = new ArrayList();

        lockTracking = false;
    }

    void Update()
    {

        if (lockTracking)
        {
            currentLock += Time.deltaTime;

            if (currentLock > maxLock)
            {
                lockTracking = false;
            }

        }

        //overwrite current lat and lon everytime
        currentLatitude = Input.location.lastData.latitude;
        currentLongitude = Input.location.lastData.longitude;

        //calculate the distance between where the player was when the app started and where they are now.
        Calc(originalLatitude, originalLongitude, currentLatitude, currentLongitude);

            


        if (DefaultTrackableEventHandler.scanned && startedCalculation && !lockTracking)
        {

            if(markersCount == 0)
            {
                sign.SetActive(true);
                StartCoroutine("setActiveSign");
            }

            if(markersCount == 1)
            {
                sign.transform.rotation = Quaternion.Euler(0, 10, -90);
                sign.SetActive(true);
                StartCoroutine("setActiveSign");
            }



            if(markersCount == 2)
            {
                finished = true;
                DefaultTrackableEventHandler.scanned = false;
                lockTracking = true;
                currentLock = 0;
                startedCalculation = false;
                float currentTime = Time.time;
                float timeDifference = currentTime - originalTime;
                float playerSpeed = (float)distance / timeDifference;
                distanceList.Add((float)distance);
                timeList.Add(timeDifference);
                speedList.Add(playerSpeed);

                float totalDistance = 0f;
                float totalTime = 0f;
                float totalSpeed = 0f;
                float avgSpeed = 0f;

                for(int i = 0; i < distanceList.Count; i++)
                {
                    totalDistance += (float)distanceList[i];
                }

                for (int i = 0; i < timeList.Count; i++)
                {
                    totalTime += (float)timeList[i];
                }

                for (int i = 0; i < speedList.Count; i++)
                {
                    totalSpeed += (float)speedList[i];
                }

                avgSpeed = totalSpeed / speedList.Count;

                distanceText.text = "Total Distance: " + ((float)totalDistance).ToString("F2") + " m";
                timeText.text = "Total Time: " + totalTime.ToString("F2") + " sec";
                speedText.text = "Average Speed: " + avgSpeed.ToString("F2") + " m/sec";
                startButton.SetActive(true);
                markersCount = 0;
                distanceList = new ArrayList();
                timeList = new ArrayList();
                speedList = new ArrayList();
            }
            else
            {
                finished = true;
                markersCount++;
                DefaultTrackableEventHandler.scanned = false;
                lockTracking = true;
                currentLock = 0;
                startedCalculation = false;
                float currentTime = Time.time;
                float timeDifference = currentTime - originalTime;
                float playerSpeed = (float)distance / timeDifference;
                distanceList.Add((float)distance);
                timeList.Add(timeDifference);
                speedList.Add(playerSpeed);

                distanceText.text = "Distance: " + ((float)distance).ToString("F2") + " m";
                timeText.text = "Time: " + timeDifference.ToString("F2") + " sec";
                speedText.text = "Speed: " + playerSpeed.ToString("F2") + " m/sec";
                starTracking();
            }
        }
    }

    IEnumerator setActiveSign()
    {
        yield return new WaitForSeconds(3);
        sign.SetActive(false);
    }

        IEnumerator GetCoordinates()
    {
        //while true so this function keeps running once started.
        while (true)
        {
            // check if user has location service enabled
            if (!Input.location.isEnabledByUser)
                yield break;

            // Start service before querying location
            Input.location.Start(1f, .1f);

            // Wait until service initializes
            int maxWait = 20;
            while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
            {
                yield return new WaitForSeconds(1);
                maxWait--;
            }

            // Service didn't initialize in 20 seconds
            if (maxWait < 1)
            {
                print("Timed out");
                yield break;
            }

            // Connection has failed
            if (Input.location.status == LocationServiceStatus.Failed)
            {
                print("Unable to determine device location");
                yield break;
            }
            else
            {
                // Access granted and location value could be retrieved
                print("Location: " + Input.location.lastData.latitude + " " + Input.location.lastData.longitude + " " + Input.location.lastData.altitude + " " + Input.location.lastData.horizontalAccuracy + " " + Input.location.lastData.timestamp);

                //if original value has not yet been set save coordinates of player on app start
                if (setOriginalValues)
                {
                    originalLatitude = Input.location.lastData.latitude;
                    originalLongitude = Input.location.lastData.longitude;
                    setOriginalValues = false;
                }
            }
            Input.location.Stop();
        }
    }

    //calculates distance between two sets of coordinates, taking into account the curvature of the earth.
    public void Calc(float lat1, float lon1, float lat2, float lon2)
    {

        var R = 6378.137; // Radius of earth in KM
        var dLat = lat2 * Mathf.PI / 180 - lat1 * Mathf.PI / 180;
        var dLon = lon2 * Mathf.PI / 180 - lon1 * Mathf.PI / 180;
        float a = Mathf.Sin(dLat / 2) * Mathf.Sin(dLat / 2) +
        Mathf.Cos(lat1 * Mathf.PI / 180) * Mathf.Cos(lat2 * Mathf.PI / 180) *
        Mathf.Sin(dLon / 2) * Mathf.Sin(dLon / 2);
        var c = 2 * Mathf.Atan2(Mathf.Sqrt(a), Mathf.Sqrt(1 - a));
        distance = R * c;
        distance = distance * 1000f; // meters

        //convert distance from double to float
        float distanceFloat = (float)distance;
        //set the target position of the ufo, this is where we lerp to in the update function
        targetPosition = originalPosition - new Vector3(0, 0, distanceFloat * 12);
        //distance was multiplied by 12 so I didn't have to walk that far to get the UFO to show up closer

        if (!finished)
        {
            distanceText.text = "Distance: " + distanceFloat.ToString("F2") + " m";

            float currentTime = Time.time;
            float timeDifference = currentTime - originalTime;
            Debug.Log(timeDifference);
            timeText.text = "Time: " + timeDifference.ToString("F2") + " sec";
        }
    }

    public void starTracking()
    {
        finished = false;
        originalLatitude = Input.location.lastData.latitude;
        originalLongitude = Input.location.lastData.longitude;
        originalTime = Time.time;
        distanceText.text = "Distance: 0 m";
        timeText.text = "Time: 0 sec";
        speedText.text = "Speed: 0 m/sec";
        startedCalculation = true;
    }
}
