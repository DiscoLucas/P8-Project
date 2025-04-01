# Measurements Tool for Unity
## Documentation

### Table of Contents
1. Introduction
2. Features
3. Installation
4. Usage
   4.1 Adding the Measurements Component
   4.2 Configuring Measurements
   4.3 Reading Measurements
5. Technical Details
   5.1 Measurement Units
   5.2 Measurement Sources
   5.3 Distance Calculations
6. Troubleshooting
7. API Reference
8. Version History
9. License

### 1. Introduction
The Measurements Tool is a Unity script that provides real-time measurements of object dimensions and distances within the Unity Editor. It's designed to assist developers and designers in accurately placing and sizing objects in their scenes.

### 2. Features
- Real-time measurement of object dimensions (width, height, depth)
- Distance calculations between objects (center-to-center and edge-to-edge)
- Support for multiple measurement units
- Flexible measurement source options
- Custom Inspector display for easy reading of measurements

### 3. Installation
1. Copy the `Measurements.cs` file into your Unity project's `Assets` folder.
2. The script will be automatically compiled by Unity.

### 4. Usage

#### 4.1 Adding the Measurements Component
1. Select the GameObject you want to measure in the Unity Hierarchy.
2. In the Inspector, click "Add Component"
3. Search for "Measurements" and select it

#### 4.2 Configuring Measurements
In the Inspector, you can configure the following settings:
- Measurement Unit: Choose from Unity Units, Centimeters, Meters, Kilometers, Inches, Feet, Yards, or Miles
- Measurement Source: Select Collider, Renderer, or Mesh
- Distance Object: Optionally assign another GameObject to measure distances to

#### 4.3 Reading Measurements
The Measurements component will display in the Inspector:
- Dimensions (Width, Height, Depth)
- Distances to the assigned Distance Object (if set):
  - Center to Center distance
  - Edge to Edge distance

### 5. Technical Details

#### 5.1 Measurement Units
The tool supports conversion between Unity units and various real-world units. Conversion factors are applied in the `ConvertToSelectedUnit` method.

#### 5.2 Measurement Sources
- Collider: Uses the bounds of the object's Collider component
- Renderer: Uses the bounds of the object's Renderer component
- Mesh: Uses the bounds of the object's Mesh component

#### 5.3 Distance Calculations
- Center to Center: Calculates the distance between the transform positions of the two objects
- Edge to Edge: Uses the `ClosestPoint` method of the Collider to find the nearest points between the two objects

### 6. Troubleshooting
- Ensure the measured object has the appropriate component (Collider, Renderer, or MeshFilter) based on your selected Measurement Source
- For distance measurements, both objects need Collider components
- If measurements are zero, check that your object has a non-zero size

### 7. API Reference

```csharp
public enum MeasurementUnit
public enum MeasurementSource
public MeasurementUnit measurementUnit
public MeasurementSource measurementSource
public GameObject distanceObject
internal Vector3 dimensions
internal float centerToCenter
internal float edgeToEdge
void CalculateDimensions()
void CalculateDistances()
float ConvertToSelectedUnit(float unityUnits)
Vector3 ConvertToSelectedUnit(Vector3 unityUnits)
```

### 8. Version History
- v1.0: Initial release

### 9. License
This tool is released under the MIT License. See the LICENSE file for details.
