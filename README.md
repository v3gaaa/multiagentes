# Multi-Agent Surveillance System 🎮 🤖 🎥

A coordinated multi-agent system that simulates security patrolling in a warehouse environment using a drone, static surveillance cameras, and a security personnel. The system implements computer vision using YOLO and agent coordination to detect and respond to potential security threats.

## Overview 🔍

This project implements a surveillance system where multiple agents (drones, cameras, and security personnel) work together to monitor and secure facilities. The system features:

- Autonomous drone patrolling
- Computer vision-based threat detection
- Multi-agent coordination
- Real-time response protocols
- YOLO model integration for object detection
- Threats or suspicious activities

## Installation Requirements 📋

- Unity Hub
- Python 3.x
- Git
- Required Python packages (specified in requirements.txt)

## Setup Instructions 🚀

### 1. Repository Setup
```bash
# Clone the repository
git clone https://github.com/v3gaaa/multiagentes

# Navigate to project directory
cd multiagentes
```

### 2. Unity Setup
1. Open Unity Hub
2. Select "Add project from disk"
3. Navigate to and select the `multiagentes` folder
4. Choose "UnityEvidencia2"
5. Open the project

### 3. Server Setup
```bash
# Navigate to Unity project
cd multiagentes

# Install Python dependencies
pip install -r requirements.txt

# Start the server
cd Server
python server.py
```

### 4. Running the Simulation
1. In Unity, navigate to:
   - Assets → Scenes
2. Select "WarehouseDon"
3. Click the Play button to start the simulation after running the server on the terminal

## System Functionality 🔄

### Detection Phase
- **Primary Detection**: Either through:
  - Fixed cameras detecting suspicious activity
  - Drone's onboard camera direct detection (using YOLO model with confidence threshold >0.7)

### Alert Protocol
1. **Initial Alert**:
   - Camera detection: Alerts the drone
   - Drone detection: Alerts the security guard

2. **Drone Response**:
   - Navigates to detection coordinates
   - Performs detailed area inspection
   - Confirms threat presence

3. **Security Guard Protocol**:
   - Goes to the control station upon alert
   - Assumes drone control once it has reached the station
   - Maintains position at control station during investigation

### Emergency Procedure
- Red warning lights activation
- Drone returns to landing position
- Simulation termination sequence