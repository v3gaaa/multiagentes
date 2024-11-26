from roboflow import Roboflow
import os

# Create a models directory if it doesn't exist
if not os.path.exists("models"):
    os.makedirs("models")

# Initialize Roboflow
rf = Roboflow(api_key="5Pdz8tW7hi78Qf6oXAQt")

# Get your project and download dataset
project = rf.workspace("marce").project("thieforsusdetection")
version = project.version(3)

# Download the dataset to the project directory
dataset = version.download("yolov8", location="./dataset")

print("Dataset downloaded successfully!")
print(f"Dataset location: {dataset.location}")