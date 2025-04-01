using UnityEngine;
using UnityEditor;

namespace MeasurementTool
{
    public class Measurements : MonoBehaviour
    {
        public enum MeasurementUnit
        {
            UnityUnits,
            Centimeters,
            Meters,
            Kilometers,
            Inches,
            Feet,
            Yards,
            Miles
        }

        public enum MeasurementSource
        {
            Collider,
            Renderer,
            Mesh
        }

        public MeasurementUnit measurementUnit = MeasurementUnit.Feet;
        public MeasurementSource measurementSource = MeasurementSource.Collider;
        public GameObject distanceObject;

        internal Vector3 dimensions;
        internal float centerToCenter;
        internal float edgeToEdge;

        void Update()
        {
            CalculateDimensions();
            if (distanceObject != null)
            {
                CalculateDistances();
            }
        }

        void CalculateDimensions()
        {
            Bounds bounds = new Bounds();

            switch (measurementSource)
            {
                case MeasurementSource.Collider:
                    Collider collider = GetComponent<Collider>();
                    if (collider != null) bounds = collider.bounds;
                    break;
                case MeasurementSource.Renderer:
                    Renderer renderer = GetComponent<Renderer>();
                    if (renderer != null) bounds = renderer.bounds;
                    break;
                case MeasurementSource.Mesh:
                    MeshFilter meshFilter = GetComponent<MeshFilter>();
                    if (meshFilter != null && meshFilter.mesh != null) bounds = meshFilter.mesh.bounds;
                    break;
            }

            dimensions = ConvertToSelectedUnit(bounds.size);
        }

        void CalculateDistances()
        {
            Vector3 thisPosition = transform.position;
            Vector3 otherPosition = distanceObject.transform.position;

            centerToCenter = ConvertToSelectedUnit(Vector3.Distance(thisPosition, otherPosition));

            Bounds thisBounds = GetComponent<Collider>().bounds;
            Bounds otherBounds = distanceObject.GetComponent<Collider>().bounds;

            Vector3 closestPoint1 = thisBounds.ClosestPoint(otherPosition);
            Vector3 closestPoint2 = otherBounds.ClosestPoint(thisPosition);

            edgeToEdge = ConvertToSelectedUnit(Vector3.Distance(closestPoint1, closestPoint2));
        }

        float ConvertToSelectedUnit(float unityUnits)
        {
            switch (measurementUnit)
            {
                case MeasurementUnit.Centimeters:
                    return unityUnits * 100f;
                case MeasurementUnit.Meters:
                    return unityUnits;
                case MeasurementUnit.Kilometers:
                    return unityUnits / 1000f;
                case MeasurementUnit.Inches:
                    return unityUnits * 39.3701f;
                case MeasurementUnit.Feet:
                    return unityUnits * 3.28084f;
                case MeasurementUnit.Yards:
                    return unityUnits * 1.09361f;
                case MeasurementUnit.Miles:
                    return unityUnits * 0.000621371f;
                default:
                    return unityUnits;
            }
        }

        Vector3 ConvertToSelectedUnit(Vector3 unityUnits)
        {
            return new Vector3(
                ConvertToSelectedUnit(unityUnits.x),
                ConvertToSelectedUnit(unityUnits.y),
                ConvertToSelectedUnit(unityUnits.z)
            );
        }
    }

    #if UNITY_EDITOR
    [CustomEditor(typeof(Measurements))]
    public class MeasurementsEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            Measurements measurements = (Measurements)target;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Dimensions", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Width\t\t\t<b>{measurements.dimensions.x:F6} {measurements.measurementUnit}</b>", new GUIStyle(EditorStyles.label) { richText = true });
            EditorGUILayout.LabelField($"Height\t\t\t<b>{measurements.dimensions.y:F6} {measurements.measurementUnit}</b>", new GUIStyle(EditorStyles.label) { richText = true });
            EditorGUILayout.LabelField($"Depth\t\t\t<b>{measurements.dimensions.z:F6} {measurements.measurementUnit}</b>", new GUIStyle(EditorStyles.label) { richText = true });

            if (measurements.distanceObject != null)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField($"Distance to {measurements.distanceObject.name}", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"Center to Center\t\t<b>{measurements.centerToCenter:F6} {measurements.measurementUnit}</b>", new GUIStyle(EditorStyles.label) { richText = true });
                EditorGUILayout.LabelField($"Edge to Edge\t\t<b>{measurements.edgeToEdge:F6} {measurements.measurementUnit}</b>", new GUIStyle(EditorStyles.label) { richText = true });
            }

            // This will update the inspector view every frame
            if (Application.isPlaying)
            {
                Repaint();
            }
        }
    }
    #endif
}