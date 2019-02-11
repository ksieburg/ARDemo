using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class SceneOrganizer : MonoBehaviour {

    /// <summary>
    /// Allows this class to behave like a singleton
    /// </summary>
    public static SceneOrganizer Instance;

    /// <summary>
    /// The cursor object attached to the Main Camera
    /// </summary>
    internal GameObject cursor;

    /// <summary>
    /// The label used to display the analysis on the objects in the real world
    /// </summary>
    public GameObject statusText;

    /// <summary>
    /// The hologram that will be shown to the user if an object is identified
    /// </summary>
    public GameObject tagFound;

    /// <summary>
    /// The tag's name that will be displayed in the hologram
    /// </summary>
    public GameObject tagName;

    /// <summary>
    /// Current threshold accepted for displaying the label
    /// Reduce this value to display the recognition more often
    /// </summary>
    internal float probabilityThreshold = 0.8f;

    /// <summary>
    /// The quad object hosting the imposed image captured
    /// </summary>
    private GameObject quad;

    /// <summary>
    /// Renderer of the quad object
    /// </summary>
    internal Renderer quadRenderer;

    /// <summary>
    /// Called on initialization
    /// </summary>
    private void Awake()
    {
        // Use this class instance as singleton
        Instance = this;

        // Add the ImageCapture class to this Gameobject
        gameObject.AddComponent<ImageCapture>();

        // Add the CustomVisionAnalyser class to this Gameobject
        gameObject.AddComponent<CustomVisionAnalyzer>();

        // Add the CustomVisionObjects class to this Gameobject
        gameObject.AddComponent<CustomVisionObjects>();
    }

    /// <summary>
    /// Instantiate a Label in the appropriate location relative to the Main Camera.
    /// </summary>
    public void InitCaptureQuad()
    {
        // Create a GameObject to which the texture can be applied
        quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quadRenderer = quad.GetComponent<Renderer>() as Renderer;
        Material m = new Material(Shader.Find("Legacy Shaders/Transparent/Diffuse"));
        quadRenderer.material = m;

        // Here you can set the transparency of the quad. Useful for debugging
        float transparency = 0f;
        quadRenderer.material.color = new Color(1, 1, 1, transparency);

        // Set the position and scale of the quad depending on user position
        quad.transform.parent = transform;
        quad.transform.rotation = transform.rotation;

        // The quad is positioned slightly forward in font of the user
        quad.transform.localPosition = new Vector3(0.0f, 0.0f, 3.0f);

        // The quad scale as been set with the following value following experimentation,  
        // to allow the image on the quad to be as precisely imposed to the real world as possible
        quad.transform.localScale = new Vector3(3f, 1.65f, 1f);
        quad.transform.parent = null;
    }

    /// <summary>
    /// Set the Tags as Text of the last label created. 
    /// </summary>
    public void ShowTag(AnalysisRootObject analysisObject)
    {
        if (analysisObject.predictions != null)
        {
            // Sort the predictions to locate the highest one. Note that for demo purposes, we are just going to show that tag alone.
            List<Prediction> sortedPredictions = new List<Prediction>();
            sortedPredictions = analysisObject.predictions.OrderBy(p => p.probability).ToList();
            Prediction bestPrediction = new Prediction();
            bestPrediction = sortedPredictions[sortedPredictions.Count - 1];

            if (bestPrediction.probability > probabilityThreshold)
            {
                quadRenderer = quad.GetComponent<Renderer>() as Renderer;
                Bounds quadBounds = quadRenderer.bounds;

                // Position the label as close as possible to the Bounding Box of the prediction 
                // At this point it will not consider depth
                this.tagFound.SetActive(true);
                this.tagFound.transform.parent = quad.transform;
                this.tagFound.transform.localPosition = CalculateBoundingBoxPosition(quadBounds, bestPrediction.boundingBox);
                this.tagName.GetComponent<Text>().text = bestPrediction.tagName;

                this.ShowObjectIdentified();
            }
            else
            {
                this.ShowNoObjectsIdentified();
            }
        }
        else
        {
            this.ShowNoObjectsIdentified();
        }

        // Stop the analysis process
        ImageCapture.Instance.ResetImageCapture();
    }

    private void ShowObjectIdentified()
    {
        if (this.statusText.activeInHierarchy)
        {
            this.statusText.GetComponent<Text>().text = "OBJECT IDENTIFIED";
        }
    }

    private void ShowNoObjectsIdentified()
    {
        if (this.statusText.activeInHierarchy)
        {
            this.statusText.GetComponent<Text>().text = "NO OBJECTS IDENTIFIED";
        }
    }

    /// <summary>
    /// This method hosts a series of calculations to determine the position 
    /// of the Bounding Box on the quad created in the real world
    /// by using the Bounding Box received back alongside the Best Prediction
    /// </summary>
    public Vector3 CalculateBoundingBoxPosition(Bounds b, BoundingBox boundingBox)
    {
        // Debug.Log($"BB: left {boundingBox.left}, top {boundingBox.top}, width {boundingBox.width}, height {boundingBox.height}");

        double centerFromLeft = boundingBox.left + (boundingBox.width / 2);
        double centerFromTop = boundingBox.top + (boundingBox.height / 2);
        // Debug.Log($"BB CenterFromLeft {centerFromLeft}, CenterFromTop {centerFromTop}");

        double quadWidth = b.size.normalized.x;
        double quadHeight = b.size.normalized.y;
        // Debug.Log($"Quad Width {b.size.normalized.x}, Quad Height {b.size.normalized.y}");

        double normalisedPos_X = (quadWidth * centerFromLeft) - (quadWidth / 2);
        double normalisedPos_Y = (quadHeight * centerFromTop) - (quadHeight / 2);

        return new Vector3((float)normalisedPos_X, (float)normalisedPos_Y, 0);
    }
}
