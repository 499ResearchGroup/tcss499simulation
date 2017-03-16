using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

static class Config {

    public const int NUMBER_OF_RUNS = 1000;
    public const int SIMULATION_SPEED_MULTIPLIER = 75; // 1-100
    public const string FILE_PATH = "c:\\temp\\";

    /* Predator Config values */
    public const float PREDATOR_SPREAD = 50.0f;
    public const float PREDATOR_DISTANCE = -75.0f;
    public const int PREDATOR_COUNT = 6;
    public const float PREDATOR_WALK_SPEED = 2.22f;
    public const float PREDATOR_RUN_SPEED = 17.78f;
    public const float PREDATOR_VISION_RADIUS = 150.0f;
    public const bool PREDATOR_DIFFERENT_STARTING_DIRECTION = false;
    public const int PREDATOR_VARIANT_STARTING_DIRECTION = 8;
    public const int PREDATOR_STARTING_DIRECTION = 1;

    /* Prey Config values */
    public const float PREY_SPREAD = 100.0f;
    public const float PREY_DISTANCE = 75.0f;
    public const int PREY_COUNT = 50;
    public const float PREY_WALK_SPEED = 1.39f;
    public const float PREY_RUN_SPEED = 22.22f;
    public const float PREY_MIN_RAND_SPEED = 15.0f; // minimum run speed possible when using random speed generation
    public const float PREY_MAX_RAND_SPEED = 20.0f; // maximum run speed possible when using random speed generation
    public const float PREY_VISION_RADIUS = 100.0f;
    public const bool PREY_USE_RANDOM_SPEEDS = false; // when true, uses random speed generation for each prey
    public const bool PREY_DIFFERENT_STARTING_DIRECTION = true;
    public const int PREY_VARIANT_STARTING_DIRECTION = 12;
    public const int PREY_STARTING_DIRECTION = 0;
    public const float PREY_NO_SIGHT_FLEE_DURATION = 10.0f; // duration in seconds of how long the prey will flee when losing sight with predators

    /* Values for control over weaknesses in the prey group */
    /* Percentage reflects the amount the prey is weakened by */
    public const int WEAK_ENDURANCE_PREY_COUNT = 1;
    public const float WEAK_ENDURANCE_PERCENT = 0.90f;

    public const int WEAK_MAXSPEED_PREY_COUNT = 0;
    public const float WEAK_MAXSPEED_PERCENT = 0.90f;

    public const int WEAK_BOTH_PREY_COUNT = 0;
    public const float WEAK_BOTH_PERCENT = 0.90f;

    public const int ENDURANCE_INDEX = 0; // Do not change. indices need to be unique and 0-3
    public const int MAXSPEED_INDEX = 1;  // Do not change.
    public const int BOTH_INDEX = 2;      // Do not change.
    public const int HEALTHY_INDEX = 3;   // Do not change.

    /* Controls if the simulation will be initialized
     * with random seed, or a provided seed.
     */
    public const bool GEN_RANDOM_SEED = true;
    public const int SEED = 38293423; /* If GEN_RANDOM_SEED is true, this value is irrelevant.         */
                                      /* Will be used to control all random aspects of initialization. */

    /* Constants. Do not change unless there is a good reason. */
    public const float HEIGHT_PLANE = -25.8267f; // The y-axis coordinate

    public const char DELIMITER = ',';
}

public class SimulationController : MonoBehaviour {

    public GameObject prey;
    public GameObject predator;

    //public PredatorAgent[] predators;
    //public PreyAgent[] preys;
    private GameObject[] predators;
    private GameObject[] preys;

    private StringBuilder myDataReport;

    private int runCount;
    private int mySuccesses;
    private int myFailures;

    private int[] mySuccessTargetCounts;

    Stopwatch watch;
    String myFileName;

    private bool myPreyHitWall;

	// Use this for initialization
	void Start () {
        watch = new Stopwatch();
        runCount = 0;
        mySuccesses = 0;
        myFailures = 0;
        mySuccessTargetCounts = new int[4] { 0, 0, 0, 0 };
        Time.timeScale = 1.0f * (float) Config.SIMULATION_SPEED_MULTIPLIER;
        //Time.fixedDeltaTime = 1.0f * (float) Config.SIMULATION_SPEED_MULTIPLIER;

        initEntities();
        myDataReport = new StringBuilder();
        initReport();
        initFile();
    }

    void OnGUI() {
        if (runCount >= Config.NUMBER_OF_RUNS) {
            GUI.color = Color.red;
            GUI.Label(new Rect(10, 10, 500, 20), "SIMULATION OVER");
        }
    }

    /**
     * @return Returns the list of prey currently in the simulation.
     */ 
    public GameObject[] getPreyList() { 
        return preys;
    }


    private void initEntities()
    {
        myPreyHitWall = false;
        /* create prey */

        preys = initGroup(prey,
                          Config.PREY_COUNT,
                          Config.PREY_SPREAD,
                          Config.PREY_DISTANCE,
                          Config.GEN_RANDOM_SEED,
                          Config.PREY_VARIANT_STARTING_DIRECTION,
                          Config.PREY_DIFFERENT_STARTING_DIRECTION,
                          Config.PREY_STARTING_DIRECTION);

        // Initialize the prey
        int length = preys.Length;
        for (int i = 0; i < length; i++)
        {
            preys[i].gameObject.GetComponent<PreyAgent>().Initialize();
        }

        /* Apply weaknesses to individual prey */
        int count = 0;
        

        for (int i = 0; i < Config.WEAK_ENDURANCE_PREY_COUNT && count < length; i++)
        {
            //Debug.Log("endurance " + preys[count].gameObject.GetComponent<PreyAgent>().endurance);
            preys[count].gameObject.GetComponent<PreyAgent>().endurance = Config.WEAK_ENDURANCE_PERCENT;
            //Debug.Log("endurance " + preys[count].gameObject.GetComponent<PreyAgent>().endurance);
            preys[count].gameObject.GetComponent<PreyAgent>().isWeakened[Config.ENDURANCE_INDEX] = true;
            preys[count].gameObject.GetComponent<PreyAgent>().isWeakened[Config.HEALTHY_INDEX] = false;
            count++;
        }

        for (int i = 0; i < Config.WEAK_MAXSPEED_PREY_COUNT && count < length; i++)
        {
            preys[count].gameObject.GetComponent<PreyAgent>().maxRunSpeed *= Config.WEAK_MAXSPEED_PERCENT;
            preys[count].gameObject.GetComponent<PreyAgent>().maxWalkSpeed *= Config.WEAK_MAXSPEED_PERCENT;

            preys[count].gameObject.GetComponent<PreyAgent>().isWeakened[Config.MAXSPEED_INDEX] = true;
            preys[count].gameObject.GetComponent<PreyAgent>().isWeakened[Config.HEALTHY_INDEX] = false;
            count++;
        }

        for (int i = 0; i < Config.WEAK_BOTH_PREY_COUNT && count < length; i++)
        {
            preys[count].gameObject.GetComponent<PreyAgent>().endurance *= Config.WEAK_BOTH_PERCENT;
            preys[count].gameObject.GetComponent<PreyAgent>().maxRunSpeed *= Config.WEAK_BOTH_PERCENT;
            preys[count].gameObject.GetComponent<PreyAgent>().maxWalkSpeed *= Config.WEAK_BOTH_PERCENT;

            preys[count].gameObject.GetComponent<PreyAgent>().isWeakened[Config.BOTH_INDEX] = true;
            preys[count].gameObject.GetComponent<PreyAgent>().isWeakened[Config.HEALTHY_INDEX] = false;
            count++;
        }


        /* initialize predators */
        predators = initGroup(predator,
                          Config.PREDATOR_COUNT,
                          Config.PREDATOR_SPREAD,
                          Config.PREDATOR_DISTANCE,
                          Config.GEN_RANDOM_SEED,
                          Config.PREDATOR_VARIANT_STARTING_DIRECTION,
                          Config.PREDATOR_DIFFERENT_STARTING_DIRECTION,
                          Config.PREDATOR_STARTING_DIRECTION);

        // Start timing the simulation
        watch.Reset();
        watch.Start();
    }
    
    /*
     * Generates a group of entities with the given parameters
     * to determine the amount of entities in the group, the
     * individual starting positions, and the starting directions.
     *
     * @theObject             The type of entity being generated.
     * @theCount              The amount of the entity being generated.
     * @theSpread             The distance factor for how far apart entities are positioned.
     * @isRand                Determines if a new seed will be generated.
     * @theDirectionVariance  How much the starting direction angle will vary from other entities.
     * @isRandDirections      Determines if random directions will be generated.
     * @theStartingDirection  The starting direction of the entity if random ones aren't generated.
     *
     * @return                An array of GameObjects for the type of entity being generated.
     */
    private static GameObject[] initGroup(GameObject theObject, 
                                          int theCount, 
                                          float theSpread,
                                          float theDistance, 
                                          bool isRand,
                                          int theDirectionVariance, 
                                          bool isRandDirections,
                                          int theStartingDirection) {
        GameObject[] objects;
        objects = new GameObject[theCount];

        if (!isRand) {
            UnityEngine.Random.InitState(Config.SEED);
        }

        for (int i = 0; i < theCount; i++) {
            Vector3 position = getRandomPosition(theSpread, theDistance);
            Quaternion direction = getDirection(isRandDirections, theDirectionVariance, theStartingDirection);

            objects[i] = (GameObject) Instantiate(theObject, position, direction);
        }

        /* OLD CODE
        for (int i = 0; i < theCount; i++) {
            xPos = UnityEngine.Random.Range(-1 * theSpread / 2, theSpread / 2);
            zPos = UnityEngine.Random.Range(-1 * theSpread / 2, theSpread / 2) + theDistance;
            objects[i] = (GameObject) Instantiate(theObject,
                                                 new Vector3(xPos, Config.HEIGHT_PLANE, zPos),
                                                 Quaternion.identity);
        }
        */

        return objects;
    }



    /*
     * Helper function for calculating the starting positions.
     * Starting positions are randomized via polar coordinates.
     *
     */
    private static Vector3 getRandomPosition(float theSpread, float theDistance)
    {
        float xPos = 0;
        float zPos = 0 + theDistance;
        float ranAngle = UnityEngine.Random.Range(0, 359);
        float ranMagnitude = UnityEngine.Random.Range(0, theSpread / 2);

        xPos = xPos + (Mathf.Cos(ranAngle) * ranMagnitude);
        zPos = zPos + (Mathf.Sin(ranAngle) * ranMagnitude);
        
        return new Vector3(xPos, Config.HEIGHT_PLANE, zPos);
    }

    /*
     * Helper function for calculating the starting directions.
     * 
     */
    private static Quaternion getDirection(bool isRandDirections, int theDirectionVariance, int theStartingDirection)
    {
        float ranDirection;

        if (isRandDirections)
        {
            ranDirection = UnityEngine.Random.Range(0, 359);
        }
        else
        {
            ranDirection = theStartingDirection;
        }
        float ranVariance = UnityEngine.Random.Range(-1 * theDirectionVariance, theDirectionVariance);
        ranDirection += ranVariance;

        return Quaternion.Euler(0, ranDirection, 0);
    }


    // Update is called once per frame
    void Update () {

        if (preys != null && predators != null && runCount < Config.NUMBER_OF_RUNS )
        {
            bool isOver = true;
            bool wasSuccess = true;
            PreyAgent caughtPrey = null;


            for (int i = 0; i < predators.Length; i++)
            {
                if (predators[i].gameObject.GetComponent<PredatorAgent>().areTargets)
                {
                    // A predator can still see a prey, simulation isn't over yet.
                    isOver = false;
                    break;
                }
            }

            // Only check prey if simulation wasn't determined to be over from predator checks.
            if (!isOver)
            {
                for (int i = 0; i < preys.Length; i++)
                {
                    if (!myPreyHitWall && preys[i].gameObject.GetComponent<PreyAgent>().transform.position.z >= 8000-5)
                    {
                        myPreyHitWall = true;
                    }

                    if (preys[i].gameObject.GetComponent<PreyAgent>().health <= 0)
                    {
                        // A prey is dead, simulation is over.
                        caughtPrey = preys[i].gameObject.GetComponent<PreyAgent>();

                        // check what weakened state the prey was in if in any.
                        int length = preys[i].gameObject.GetComponent<PreyAgent>().isWeakened.Length;
                        for (int j = 0; j < length; j++) {
                            if (preys[i].gameObject.GetComponent<PreyAgent>().isWeakened[j])
                            {
                                // increment the count of the weakened state the prey was in.
                                mySuccessTargetCounts[j]++;
                            }
                        }

                        isOver = true;
                        wasSuccess = true;
                        mySuccesses++;
                        break;
                    }
                }
            }
            else
            {
                // Predators failed hunt, can't see anymore prey.
                wasSuccess = false;
                myFailures++;
            }

            // If simulation is over, reload the scene.
            if (isOver)
            {
                reloadScene(wasSuccess, caughtPrey);
            }
        }

        // for built versions of the simulation escape will exit the application
        if (Input.GetKey(KeyCode.Escape)) 
        {
            Application.Quit();
        }

    }

    private void reloadScene(bool wasSuccess, PreyAgent theCaughtPrey)
    {
        watch.Stop();
        double seconds = watch.Elapsed.TotalSeconds;
        seconds = seconds * Config.SIMULATION_SPEED_MULTIPLIER;
        seconds = Math.Round(seconds, 1);

        runCount++;
        // Transcribe data here.
        updateReport(wasSuccess, theCaughtPrey, seconds);
        appendToFile(myDataReport.ToString());
        myDataReport.Length = 0;

        if (runCount % (Config.NUMBER_OF_RUNS / 10) == 0) UnityEngine.Debug.Log("Runs: " + ((double)runCount / Config.NUMBER_OF_RUNS * 100) + "%");
        // Only reload the scene if there are still runs to do, otherwise freeze simulation.
        if (runCount < Config.NUMBER_OF_RUNS)
        {
            //SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            for (int i = 0; i < predators.Length; i++)
            {
                Destroy(predators[i]);
            }
            for (int i = 0; i < preys.Length; i++)
            {
                Destroy(preys[i]);
            }
            initEntities();
        }
        else
        {
            for (int i = 0; i < predators.Length; i++) {
                Destroy(predators[i]);
            }
            for (int i = 0; i < preys.Length; i++) {
                Destroy(preys[i]);
            }
            // Out of runs, freeze simulation and write report to file.
            Time.timeScale = 1.0f;
            //Time.fixedDeltaTime = 0.0f;
            /*
            string dataReport = generateReport(mySuccesses, myFailures, 
                                               mySuccessTargetCounts[Config.ENDURANCE_INDEX],
                                               mySuccessTargetCounts[Config.MAXSPEED_INDEX],
                                               mySuccessTargetCounts[Config.BOTH_INDEX],
                                               mySuccessTargetCounts[Config.HEALTHY_INDEX]);
            */
                       
        }
        
    }


    private void initReport()
    {
        myDataReport.Append("Prey Count,Pred Count,Weak End,Weak Spd,Weak Both,,Pred Spread,Pred Distance,Pred Walk,Pred Run,Pred Vision,Prey Spread,Prey Distance,Prey Walk,Prey Run,Prey Vision,Weak End %,Weak Spd %,Weak Both %" + Environment.NewLine);
        myDataReport.Append(Config.PREY_COUNT);
        myDataReport.Append(Config.DELIMITER);
        myDataReport.Append(Config.PREDATOR_COUNT);
        myDataReport.Append(Config.DELIMITER);
        myDataReport.Append(Config.WEAK_ENDURANCE_PREY_COUNT);
        myDataReport.Append(Config.DELIMITER);
        myDataReport.Append(Config.WEAK_MAXSPEED_PREY_COUNT);
        myDataReport.Append(Config.DELIMITER);
        myDataReport.Append(Config.WEAK_BOTH_PREY_COUNT);
        myDataReport.Append(Config.DELIMITER);
        myDataReport.Append("");
        myDataReport.Append(Config.DELIMITER);
        myDataReport.Append(Config.PREDATOR_SPREAD);
        myDataReport.Append(Config.DELIMITER);
        myDataReport.Append(Config.PREDATOR_DISTANCE);
        myDataReport.Append(Config.DELIMITER);
        myDataReport.Append(Config.PREDATOR_WALK_SPEED);
        myDataReport.Append(Config.DELIMITER);
        myDataReport.Append(Config.PREDATOR_RUN_SPEED);
        myDataReport.Append(Config.DELIMITER);
        myDataReport.Append(Config.PREDATOR_VISION_RADIUS);
        myDataReport.Append(Config.DELIMITER);
        myDataReport.Append(Config.PREY_SPREAD);
        myDataReport.Append(Config.DELIMITER);
        myDataReport.Append(Config.PREY_DISTANCE);
        myDataReport.Append(Config.DELIMITER);
        myDataReport.Append(Config.PREY_WALK_SPEED);
        myDataReport.Append(Config.DELIMITER);
        myDataReport.Append(Config.PREY_RUN_SPEED);
        myDataReport.Append(Config.DELIMITER);
        myDataReport.Append(Config.PREY_VISION_RADIUS);
        myDataReport.Append(Config.DELIMITER);
        myDataReport.Append(Config.WEAK_ENDURANCE_PERCENT);
        myDataReport.Append(Config.DELIMITER);
        myDataReport.Append(Config.WEAK_MAXSPEED_PERCENT);
        myDataReport.Append(Config.DELIMITER);
        myDataReport.Append(Config.WEAK_BOTH_PERCENT);
        myDataReport.Append(Environment.NewLine);
        myDataReport.Append(Environment.NewLine);
        myDataReport.Append("Run ID,Success/Failure,Time to completion,Class of prey caught,Endurance of prey caught, Prey Hit Wall" + Environment.NewLine);
    }

    private void updateReport(bool wasSuccess, PreyAgent theCaughtPrey, double theTime)
    {
        // Run ID
        myDataReport.Append(runCount);
        myDataReport.Append(Config.DELIMITER);

        // Success / Failure
        myDataReport.Append(wasSuccess);
        myDataReport.Append(Config.DELIMITER);

        // Time to Completion
        myDataReport.Append(theTime);
        myDataReport.Append(Config.DELIMITER);

        // Class of prey caught
        if (wasSuccess) {
            myDataReport.Append(getWeakenedState(theCaughtPrey));
        } else {
            myDataReport.Append("");
        }
        myDataReport.Append(Config.DELIMITER);

        // Endurance of prey caught
        if (wasSuccess) {
            myDataReport.Append(Math.Round(theCaughtPrey.endurance, 2));
        } else {
            myDataReport.Append("");
        }
        myDataReport.Append(Config.DELIMITER);

        if (myPreyHitWall)
            myDataReport.Append(myPreyHitWall);

        if (runCount < Config.NUMBER_OF_RUNS) myDataReport.Append(Environment.NewLine);
    }



    private static string getWeakenedState(PreyAgent thePrey)
    {
        string theState = "";

        int length = thePrey.gameObject.GetComponent<PreyAgent>().isWeakened.Length;
        for (int j = 0; j < length; j++)
        {
            if (thePrey.gameObject.GetComponent<PreyAgent>().isWeakened[j])
            {
                switch (j)
                {
                    case Config.ENDURANCE_INDEX:
                        theState = "Endurance";
                        break;
                    case Config.MAXSPEED_INDEX:
                        theState = "MaxSpeed";
                        break;
                    case Config.BOTH_INDEX:
                        theState = "Both";
                        break;
                    case Config.HEALTHY_INDEX:
                        theState = "Healthy";
                        break;
                    default:
                        theState = "";
                        break;
                }
            }
        }

        return theState;
    }


    private string generateFileName()
    {
        StringBuilder fileName = new StringBuilder();

        fileName.Append(Config.FILE_PATH);
        /* Dynamic Part of file name */
        fileName.Append("Prey");
        fileName.Append(Config.PREY_COUNT);
        fileName.Append("_Pred");
        fileName.Append(Config.PREDATOR_COUNT);
        fileName.Append("_E");
        fileName.Append(Config.WEAK_ENDURANCE_PREY_COUNT);
        if (Config.WEAK_ENDURANCE_PREY_COUNT > 0)
            fileName.Append("(" + Config.WEAK_ENDURANCE_PERCENT*100 + "%)");

        fileName.Append("_S");
        fileName.Append(Config.WEAK_MAXSPEED_PREY_COUNT);
        if (Config.WEAK_MAXSPEED_PREY_COUNT > 0)
            fileName.Append("(" + Config.WEAK_MAXSPEED_PERCENT*100 + "%)");

        fileName.Append("_B");
        fileName.Append(Config.WEAK_BOTH_PREY_COUNT);
        if (Config.WEAK_BOTH_PREY_COUNT > 0)
            fileName.Append("(" + Config.WEAK_BOTH_PERCENT*100 + "%)");

        fileName.Append("_" + getHashID());
        fileName.Append(".csv");

        return fileName.ToString();
    }

    private int getHashID()
    {
        int hashCode = 1;
        hashCode = 31 * hashCode + (int)Config.PREDATOR_SPREAD;
        hashCode = 31 * hashCode + (int)Config.PREDATOR_DISTANCE;
        hashCode = 31 * hashCode + (int)Config.PREDATOR_WALK_SPEED;
        hashCode = 31 * hashCode + (int)Config.PREDATOR_RUN_SPEED;
        hashCode = 31 * hashCode + (int)Config.PREDATOR_VISION_RADIUS;
        hashCode = 31 * hashCode + (int)Config.PREY_SPREAD;
        hashCode = 31 * hashCode + (int)Config.PREY_DISTANCE;
        hashCode = 31 * hashCode + (int)Config.PREY_RUN_SPEED;
        hashCode = 31 * hashCode + (int)Config.PREY_WALK_SPEED;
        hashCode = 31 * hashCode + (int)Config.PREY_VISION_RADIUS;
        hashCode = 31 * hashCode + (int)Config.WEAK_ENDURANCE_PERCENT;
        hashCode = 31 * hashCode + (int)Config.WEAK_MAXSPEED_PERCENT;
        hashCode = 31 * hashCode + (int)Config.WEAK_BOTH_PERCENT;

        return Math.Abs(hashCode);
    }


    private static string generateReport(int successCount, int failureCount, 
                                         int enduranceCount, int speedCount,
                                         int bothCount, int healthyCount)
    {
        StringBuilder report = new StringBuilder();

        report.Append(String.Format("{0,-20}", "Number of Runs"));
        report.Append(Config.NUMBER_OF_RUNS);
        report.AppendLine();

        report.Append(String.Format("{0,-20}", "Starting Distance"));
        float distance = Mathf.Abs(Config.PREDATOR_DISTANCE) + Mathf.Abs(Config.PREY_DISTANCE);
        report.Append(distance);
        report.AppendLine();

        report.Append("Weakened Prey: Endurance=" + Config.WEAK_ENDURANCE_PREY_COUNT +
                                        " Speed=" + Config.WEAK_MAXSPEED_PREY_COUNT +
                                        " Both=" + Config.WEAK_BOTH_PREY_COUNT);
        report.AppendLine();
        report.AppendLine();



        report.Append(String.Format("{0,-15}{1,10}{2,10}", "", "Predator", "Prey"));
        report.AppendLine();

        report.Append(String.Format("{0,15}{1,10}{2,10}", "Count", Config.PREDATOR_COUNT, Config.PREY_COUNT));
        report.AppendLine();

        report.Append(String.Format("{0,15}{1,10}{2,10}", "Walk Speed", Config.PREDATOR_WALK_SPEED, Config.PREY_WALK_SPEED));
        report.AppendLine();

        report.Append(String.Format("{0,15}{1,10}{2,10}", "Run Speed", Config.PREDATOR_RUN_SPEED, Config.PREY_RUN_SPEED));
        report.AppendLine();





        /* Success/Failure */
        report.Append("---------------------------------------");

        float tempPercent = 0.0f;

        report.AppendLine();
        report.Append(String.Format("{0, -15}", "Success"));
        report.Append(String.Format("{0, -5}", successCount));
        tempPercent = (float) successCount / Config.NUMBER_OF_RUNS;
        report.Append(String.Format("{0,-5:P1}", tempPercent));


        report.AppendLine();
        report.Append(String.Format("{0, -15}", "Failure"));
        report.Append(String.Format("{0, -5}", failureCount));
        tempPercent = (float) failureCount / Config.NUMBER_OF_RUNS;
        report.Append(String.Format("{0,-5:P1}", tempPercent));

        report.AppendLine();
        report.AppendLine();
        report.Append("Weakened States of prey caught");

        tempPercent = 0.0f; // reset to 0

        report.AppendLine();
        report.Append(String.Format("{0, -15}", "None"));
        report.Append(String.Format("{0, -5}", healthyCount));
        if (successCount != 0) tempPercent = (float) healthyCount / successCount;
        report.Append(String.Format("{0,-5:P1}", tempPercent));


        report.AppendLine();
        report.Append(String.Format("{0, -15}", "Endurance"));
        report.Append(String.Format("{0, -5}", enduranceCount));
        if (successCount != 0) tempPercent = (float) enduranceCount / successCount;
        report.Append(String.Format("{0,-5:P1}", tempPercent));


        report.AppendLine();
        report.Append(String.Format("{0, -15}", "Max Speed"));
        report.Append(String.Format("{0, -5}", speedCount));
        if (successCount != 0) tempPercent = (float) speedCount / successCount;
        report.Append(String.Format("{0,-5:P1}", tempPercent));


        report.AppendLine();
        report.Append(String.Format("{0, -15}", "Both"));
        report.Append(String.Format("{0, -5}", bothCount));
        if (successCount != 0) tempPercent = (float) bothCount / successCount;
        report.Append(String.Format("{0,-5:P1}", tempPercent));

        report.AppendLine();
        report.AppendLine();
        report.Append("*************************************************");
        report.AppendLine();

        return report.ToString();
    }

    private void initFile()
    {
        myFileName = generateFileName();
        //UnityEngine.Debug.Log("File: " + myFileName);
        File.WriteAllText(myFileName, myDataReport.ToString());
        //UnityEngine.Debug.Log("Data: " + myDataReport.ToString());
        myDataReport.Length = 0;
    }



    private void appendToFile(string theData)
    {
        File.AppendAllText(myFileName, theData);
        
        /*
        if (File.Exists(file))
        {
            File.Delete(file);
        }
        */

        //string createText = "Simulation Data" + Environment.NewLine + Environment.NewLine;
        //string createText = "";
        //File.WriteAllText(file, theData);

        //File.AppendAllText(file, theData) + Environment.NewLine);

    }
}
