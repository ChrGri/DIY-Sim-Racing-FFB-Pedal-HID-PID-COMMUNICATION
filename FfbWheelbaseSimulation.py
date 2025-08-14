import numpy as np
import matplotlib.pyplot as plt

def simulate_ffb_wheelbase_with_pid(
    # Wheelbase parameters
    max_torque=18.0,        # Maximum torque in Nm
    slew_rate=1000.0,        # Torque Slew Rate in Nm/s
    max_speed_rpm=3000.0,   # Maximum rotational speed in RPM
    inertia=0.01,           # Rotational inertia of motor and wheel in kg*m^2
    # PID Controller parameters
    Kp=10.0,                # Proportional gain
    Ki=10.0,                # Integral gain (A non-zero value is needed to see windup)
    Kd=0.1,                 # Derivative gain
    # Simulation parameters
    sim_time=2.0,           # Total simulation time in seconds
    dt=0.0001               # Simulation time step in seconds (10 kHz)
):
    """
    Simulates the physics of an FFB wheelbase with a PID position controller,
    including an anti-windup mechanism.

    Args:
        max_torque (float): The absolute maximum torque the motor can produce (Nm).
        slew_rate (float): The maximum rate of change of torque (Nm/s).
        max_speed_rpm (float): The speed at which the motor can no longer generate torque (RPM).
        inertia (float): The rotational inertia of the system (kg*m^2).
        Kp (float): Proportional gain for the PID controller.
        Ki (float): Integral gain for the PID controller.
        Kd (float): Derivative gain for the PID controller.
        sim_time (float): The duration of the simulation (s).
        dt (float): The time step for the discrete simulation (s).
    """
    # --- 1. Initialization ---

    # Convert max speed from RPM to rad/s for calculations
    max_speed_rad_s = max_speed_rpm * (2 * np.pi) / 60.0

    # Create the time vector for the simulation
    t = np.arange(0, sim_time, dt)
    num_steps = len(t)

    # Initialize state arrays to store results at each time step
    torque = np.zeros(num_steps)
    velocity = np.zeros(num_steps)
    position = np.zeros(num_steps)
    acceleration = np.zeros(num_steps)

    # PID controller variables
    integral_error = 0.0
    last_error = 0.0

    # --- 2. Define Setpoint (Target Position) ---

    # We'll use a square wave for the target position.
    # The goal is to move to pi radians (180 degrees) at t=0.1s.
    target_position = np.zeros(num_steps)
    start_index = int(0.1 / dt)
    target_position[start_index:] = np.pi

    # --- 3. Simulation Loop ---

    for i in range(1, num_steps):
        # --- PID Controller Calculation ---

        # Calculate the error between the target position and the current position
        error = target_position[i] - position[i-1]

        # Calculate the derivative of the error (for damping)
        derivative_error = (error - last_error) / dt
        last_error = error

        # Calculate the desired torque from the PID controller
        # Note: The integral term is added *after* the anti-windup check below.
        pid_output_torque = (Kp * error) + (Ki * integral_error) + (Kd * derivative_error)

        # --- Torque Application (Slew Rate & Max Torque Limiting) ---

        # The actual torque cannot change instantly. It's limited by the slew rate.
        max_delta_torque = slew_rate * dt
        required_delta_torque = pid_output_torque - torque[i-1]
        actual_delta_torque = np.clip(required_delta_torque, -max_delta_torque, max_delta_torque)
        current_torque = torque[i-1] + actual_delta_torque

        # Clamp the final torque to the absolute maximum the wheelbase can produce.
        torque[i] = np.clip(current_torque, -max_torque, max_torque)
        
        # --- PID Anti-Windup (Conditional Integration) ---
        # Only accumulate integral error if the motor is NOT saturated at its max torque.
        # This prevents the integral term from "winding up" when the system can't respond,
        # which would otherwise cause a massive overshoot.
        if abs(torque[i]) < max_torque:
            integral_error += error * dt

        # --- Dynamics Calculation (Inertia and Max Speed) ---

        # The effective torque decreases as the motor approaches its max speed (Back-EMF).
        speed_factor = max(0, 1 - abs(velocity[i-1]) / max_speed_rad_s)
        effective_torque = torque[i] * speed_factor

        # Newton's second law for rotation: α = τ / J
        angular_acceleration = effective_torque / inertia
        acceleration[i] = angular_acceleration

        # Integrate acceleration to get velocity
        velocity[i] = velocity[i-1] + angular_acceleration * dt

        # Integrate velocity to get position
        position[i] = position[i-1] + velocity[i] * dt

    # --- 4. Plotting Results ---

    fig, (ax1, ax2, ax3, ax4) = plt.subplots(4, 1, figsize=(12, 12), sharex=True)
    fig.suptitle('FFB Wheelbase Simulation with PID Controller and Anti-Windup', fontsize=16)

    # Plot 1: Position vs. Time
    ax1.plot(t, target_position, 'r--', label='Target Position', alpha=0.7)
    ax1.plot(t, position, 'm-', label='Actual Position')
    ax1.set_ylabel('Position (rad)')
    ax1.set_title('Position Tracking')
    ax1.legend()
    ax1.grid(True)

    # Plot 2: Torque vs. Time
    ax2.plot(t, torque, 'b-', label='Actual Torque (from PID)')
    ax2.hlines(y=max_torque, xmin=np.min(t), xmax=np.max(t), linewidth=1, colors='r', linestyles='--', label='Max Torque')
    ax2.hlines(y=-max_torque, xmin=np.min(t), xmax=np.max(t), linewidth=1, colors='r', linestyles='--')
    ax2.set_ylabel('Torque (Nm)')
    ax2.set_title('Torque Response')
    ax2.legend()
    ax2.grid(True)

    # Plot 3: Velocity vs. Time
    ax3.plot(t, velocity, 'g-', label='Velocity')
    ax3.hlines(y=max_speed_rad_s, xmin=np.min(t), xmax=np.max(t), linewidth=1, colors='r', linestyles='--', label='Max Speed')
    ax3.hlines(y=-max_speed_rad_s, xmin=np.min(t), xmax=np.max(t), linewidth=1, colors='r', linestyles='--')
    ax3.set_ylabel('Velocity (rad/s)')
    ax3.set_title('Velocity Response')
    ax3.legend()
    ax3.grid(True)

    # Plot 4: Acceleration vs. Time
    ax4.plot(t, acceleration, 'c-', label='Acceleration')
    ax4.set_xlabel('Time (s)')
    ax4.set_ylabel('Acceleration (rad/s²)')
    ax4.set_title('Acceleration Response')
    ax4.legend()
    ax4.grid(True)

    plt.tight_layout(rect=[0, 0.03, 1, 0.95])
    plt.show()


if __name__ == '__main__':
    # You can run the simulation and tune the PID gains here.
    # To see the effect of windup, set Ki to a large value and comment out the anti-windup check.
    simulate_ffb_wheelbase_with_pid()
