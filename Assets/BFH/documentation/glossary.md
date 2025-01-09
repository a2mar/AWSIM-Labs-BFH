# Glossary

## HD Map:

- map with logical (and vector) information
- does not contain pointclouds
- called `lanelet` in _Autoware_

## Point Cloud Map
The manually created pointcloud map of a 3D-space.
In Autoware, it has to be provided to the launch script next to the _HD Map_ (_lanelet_).

## Pointcloud stream
A pointcloud stream that is produced at runtime, typically with a _Lidar_ sensor.

## path-following AV (Autonomous Vehicle)
A vehicle that's driving autonomously along a given path

## simulated system:
The scope of the The simulated system consists of:
- a simulation of the path-following _AV_, that consits of the complete virtualized sensor-kits
- traffic, including pedestriancs, other vehicles (trucks, buses, cars, bikes)
- traffic signals, including lights, traffic signs, and road markings

## to add:

- NPC vehicles
- NPC pedestrians
- HD map
- Digital Twin Simulation
- ROS2