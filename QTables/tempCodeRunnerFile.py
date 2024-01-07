import json
import matplotlib.pyplot as plt
from math import sqrt

# Load session data
with open('sessionData.json', 'r') as file:
    session_data = json.load(file)

# Calculate the scalar distance from initialPosition to destination
initial_pos = session_data['initialPosition']
destination = session_data['destination']
distance = sqrt((destination['x'] - initial_pos['x'])**2 + 
                (destination['y'] - initial_pos['y'])**2 + 
                (destination['z'] - initial_pos['z'])**2)

# Load episode data
with open('episodeData.json', 'r') as file:
    episode_data = [json.loads(line) for line in file]

# Extracting data
episode_numbers = [data['episodeNumber'] for data in episode_data]
destination_reached = [data['destinationReached'] for data in episode_data]
distance_covered = [data['distanceCovered'] for data in episode_data]
epsilon_values = [data['epsilon'] for data in episode_data]

# Plotting
plt.figure(figsize=(12, 8))

plt.subplot(2, 2, 1)
plt.plot(episode_numbers, destination_reached, marker='o')
plt.title("Destination Reached per Episode")
plt.xlabel("Episode Number")
plt.ylabel("Destination Reached")

plt.subplot(2, 2, 2)
plt.plot(episode_numbers, distance_covered, marker='o')
plt.title("Distance Covered per Episode")
plt.xlabel("Episode Number")
plt.ylabel("Distance Covered")

plt.subplot(2, 2, 3)
plt.plot(episode_numbers, epsilon_values, marker='o')
plt.title("Epsilon Value per Episode")
plt.xlabel("Episode Number")
plt.ylabel("Epsilon Value")

plt.subplot(2, 2, 4)
plt.axhline(y=distance, color='r', linestyle='-')
plt.title("Scalar Distance from InitialPosition to Destination")
plt.xlabel("Episode Number")
plt.ylabel("Scalar Distance")
plt.ylim(0, distance + 1)  # Adjust y-axis to show the line clearly

plt.tight_layout()
plt.show()
