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
total_rewards = [data['totalReward'] for data in episode_data]
impossible_moves = [data['impossibleMoves'] for data in episode_data]
unwanted_moves = [data['unwantedMoves'] for data in episode_data]
exploration_amounts = [data['exploration'] for data in episode_data]
exploitation_amounts = [data['exploitation'] for data in episode_data]

# Calculate Total Reward per Distance Covered
reward_per_distance = [total_rewards[i]/distance_covered[i] if distance_covered[i] != 0 else 0 for i in range(len(total_rewards))]

# Plotting
plt.figure(figsize=(15, 9))

# Plot 1: Destination Reached per Episode
plt.subplot(3, 2, 1)
plt.plot(episode_numbers, destination_reached, marker='o')
plt.title("Destination Reached per Episode")
plt.xlabel("Episode Number")
plt.ylabel("Destination Reached")

# Plot 2: Distance Covered and Scalar Distance per Episode
plt.subplot(3, 2, 2)
plt.plot(episode_numbers, distance_covered, marker='o', label='Distance Covered')
plt.axhline(y=distance, color='r', linestyle='-', label='Scalar Distance = {}'.format(round(distance,2)))
plt.title("Distance Covered and Scalar Distance per Episode")
plt.xlabel("Episode Number")
plt.ylabel("Distance")
plt.legend()

# Plot 3: Epsilon Value per Episode
plt.subplot(3, 2, 3)
plt.plot(episode_numbers, epsilon_values, marker='o')
plt.title("Epsilon Value per Episode")
plt.xlabel("Episode Number")
plt.ylabel("Epsilon Value")

# Plot 4: Exploration and Exploitation Amounts per Episode

plt.subplot(3, 2, 4)
plt.plot(episode_numbers, exploration_amounts, marker='o', label='Exploration')
plt.plot(episode_numbers, exploitation_amounts, marker='x', label='Exploitation')
plt.title("Exploration and Exploitation per Episode")
plt.xlabel("Episode Number")
plt.ylabel("Amount")
plt.legend()

# Plot 5: Total Reward per Distance Covered per Episode
plt.subplot(3, 2, 5)
plt.plot(episode_numbers, reward_per_distance, marker='o', color='green')
plt.title("Total Reward per Distance Covered per Episode")
plt.xlabel("Episode Number")
plt.ylabel("Reward per Distance")

# Plot 6: Number of Impossible and Unwanted Moves per Episode
plt.subplot(3, 2, 6)
plt.plot(episode_numbers, impossible_moves, marker='o', label='Impossible Moves')
plt.plot(episode_numbers, unwanted_moves, marker='x', label='Unwanted Moves')
plt.title("Impossible and Unwanted Moves per Episode")
plt.xlabel("Episode Number")
plt.ylabel("Number of Moves")
plt.legend()

plt.tight_layout()
plt.show()
