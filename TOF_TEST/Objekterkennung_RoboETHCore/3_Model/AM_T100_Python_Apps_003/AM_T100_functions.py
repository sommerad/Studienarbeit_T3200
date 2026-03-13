import csv
import time
import numpy as np
import pyvista as pv


# Function to write the camera parameters, their type, symbolics, and current value in .csv file
def read_camera_parameters(ia):
    # Open a CSV file to write the parameters, their type, symbolics, and current value
    with open('camera_parameters.csv', mode='w', newline='') as file:
        writer = csv.writer(file)
        
        # Write the header row
        writer.writerow(['Parameter Name', 'Type', 'Symbolics', 'Current Value'])
        
        # Use dir to list all available parameters
        parameters = dir(ia.remote_device.node_map)
        
        # Iterate through parameters and write each one into the CSV if it starts with a capital letter and has no underscores
        for param in parameters:
            if param[0].isupper() and "_" not in param:  # Filter based on capital letter and no underscores
                try:
                    # Access the node and retrieve the type of the parameter
                    node = ia.remote_device.node_map.get_node(param)
                    if node:
                        param_class = node.__class__.__name__  # Get the type of the parameter
                        
                        # Initialize symbolics and current_value as empty strings
                        symbolics = ''
                        current_value = ''
                        
                        # Check if the parameter is of type IEnumeration and get symbolics
                        if param_class == 'IEnumeration':
                            try:
                                symbolics = ', '.join(node.symbolics) if hasattr(node, 'symbolics') else ''
                            except Exception as e:
                                print(f"Error fetching symbolics for {param}: {e}")
                                symbolics = 'Error'

                        # Get the current value of the parameter, if available and readable
                        try:
                            current_value = node.value if hasattr(node, 'value') else ''
                        except Exception as e:
                            print(f"Error fetching current value for {param}: {e}")
                            current_value = 'Error'
                        
                        # Write parameter name, type, symbolics, and current value to the CSV
                        writer.writerow([param, param_class, symbolics, current_value])
                    else:
                        writer.writerow([param, 'Type Not Found', '', ''])
                except Exception as e:
                    print(f"Error for {param}: {e}")
                    writer.writerow([param, 'Error', '', ''])



# Function to prepare the camera for image acquisition
def prepare_image_acquisition(ia):
    # Mandatory parameter settings for image acquisition
    ia.remote_device.node_map.TriggerSelector.value = 'FrameStart'
    ia.remote_device.node_map.TriggerMode.value = "Off"



# Function to define the buffer data settings
def define_buffer_data(ia):
    ia.remote_device.node_map.TransmitConfidenceImage.value = 0
    ia.remote_device.node_map.TransmitIntensityImage.value = 0
    ia.remote_device.node_map.TransmitRangeImage.value = 1



def fetch_buffer_data(ia):
    # Time delay
    time.sleep(1)
    
    # Start acquisition
    ia.start()
    
    # Fetch the buffer
    buffer = ia.fetch(timeout=500)
    
    # Retrieve the data from the buffer (assuming Coord3D_C32f)
    component = buffer.payload.components[0]
    
    # Convert raw binary data from camera buffer into a NumPy array of floating-point numbers
    raw_data = np.frombuffer(component.data, dtype=np.float32)
    
    # Stop acquisition
    ia.stop()
    
    # Ensure the data size is valid for reshaping to Nx3
    if raw_data.size % 3 != 0:
        raise ValueError("Buffer data length is not a multiple of 3, cannot reshape to Nx3 matrix.")

    raw_data = raw_data.reshape(-1, 3)
    
    # Filter out invalid values (e.g., NaN, inf, extreme ranges)
    # valid_indices = np.all(np.isfinite(raw_data), axis=1)
    valid_indices = np.all(np.isfinite(raw_data) & (raw_data >= -15000) & (raw_data <= 15000), axis=1)
    raw_data = raw_data[valid_indices]
    print(f"Filtered raw_data shape: {raw_data.shape}")

    return raw_data



def visualize_point_cloud(raw_data):
    if len(raw_data.shape) != 2 or raw_data.shape[1] != 3:
        raise ValueError("Input raw_data must be a NumPy array of shape (N, 3)")

    if raw_data.size == 0:
        raise ValueError("Input raw_data is empty.")

    # Convert raw data from millimeters to meters
    raw_data_in_meters = raw_data / 1000.0

    # Extract the Z-coordinate for coloring
    z_values = raw_data_in_meters[:, 2]  # Use Z-axis (3rd column) as the scalar

    # Create a PyVista point cloud
    point_cloud = pv.PolyData(raw_data_in_meters)

    # Add Z-values as a scalar field for coloring
    point_cloud['Z (m)'] = z_values

    # Start PyVista plotter
    plotter = pv.Plotter()

    # Calculate dynamic point size with a minimum size of 5
    point_size = max(2.5, min(10, 5000 // len(raw_data)))

    # Add the point cloud to the plot, Spectral, jet, turbo, rainbow, viridis, coolwarm
    plotter.add_mesh(
        point_cloud, scalars='Z (m)', cmap='Spectral', point_size=point_size
    )

    # Add bounding box for context
    plotter.add_bounding_box(color='grey')

    # Add coordinate axes
    plotter.add_axes(viewport=[0.0, 0.0, 0.2, 0.2])

    # Calculate dynamic camera position
    x_cam = (raw_data_in_meters[:, 0].max() - raw_data_in_meters[:, 0].min()) * 1.5
    y_cam = 0
    z_cam = (raw_data_in_meters[:, 2].max() - raw_data_in_meters[:, 2].min()) * 2

    # Set the initial camera perspective with Z-axis up, X-axis right, and Y-axis down
    plotter.camera_position = [
        (x_cam, y_cam, z_cam),  # Dynamic camera position
        (0, 0, 0),              # Look at origin
        (0, -1, 0)              # X-axis right, Y-axis down
    ]
    
    # Add a title showing metadata (with black text)
    # Calculate Z-axis range
    z_min = raw_data_in_meters[:, 2].min()
    z_max = raw_data_in_meters[:, 2].max()
    z_min_str = f"{z_min:.3f}"
    z_max_str = f"{z_max:.3f}"
    plotter.add_text(
        f"Point Cloud Visualization\n"
        f"Points: {len(raw_data)}\n"
        f"Z-Range: [{z_min_str}, {z_max_str}] meters",
        font_size=8,
        position="upper_left",
        color="black",  # Set text color to black
    )
    
    # Show the plot
    plotter.show()

   
def save_pcd(raw_data, filename):
    """
    Speichert die 3D-Punkte im PCD-Format (nur X, Y, Z ohne RGB).
    """
    # Validierung der raw_data
    if raw_data.size == 0:
        raise ValueError("raw_data ist leer!")

    # Filtere ungültige Werte (z.B. NaN, unendliche Werte)
    valid_indices = np.all(np.isfinite(raw_data) & (raw_data >= -15000) & (raw_data <= 15000), axis=1)
    raw_data = raw_data[valid_indices]

    # Konvertiere die Rohdaten in Meter (falls sie in Millimetern vorliegen)
    raw_data_in_meters = raw_data / 1  

    # Header für PCD-Datei (ohne RGB)
    header = """# .PCD v.7 - Point Cloud Data file format
VERSION .7
FIELDS x y z
SIZE 4 4 4
TYPE F F F
COUNT 1 1 1
WIDTH {width}
HEIGHT 1
VIEWPOINT 0 0 0 1 0 0 0
POINTS {num_points}
DATA ascii
""".format(width=len(raw_data_in_meters), num_points=len(raw_data_in_meters))

    # Speichern der PCD-Datei
    with open(filename, 'w') as f:
        # Schreibe den Header
        f.write(header)

        # Schreibe die Punkte als ASCII (nur X, Y, Z)
        for point in raw_data_in_meters:
            f.write(f"{point[0]} {point[1]} {point[2]}\n")

   

# Beispielaufruf:
# raw_data = np.array([...])  # Deine 3D-Daten als NumPy-Array
# save_pcd(raw_data, 'output.pcd')
   


# Function to fetch and queue the buffer
def fetch_and_queue_buffer(ia):
    # Fetch the buffer
    buffer = ia.fetch(timeout=500)
    
    # Retrieve the data from the buffer (assuming Coord3D_C32f)
    component = buffer.payload.components[0]
    
    # Convert raw binary data from camera buffer into a NumPy array of floating-point numbers
    raw_data = np.frombuffer(component.data, dtype=np.float32)  # Extract buffer data

    # Queue the buffer back to the buffer pool
    buffer.queue()

    # Return the raw buffer data for further processing
    return raw_data



# Function to process the point cloud data
def process_point_cloud(raw_data):
    
    # ---------- START YOUR cloud processing logic HERE ----------
    
    
    print(f"Processing point cloud data of size {raw_data.size}")
    

    # ---------- END YOUR cloud processing logic HERE ----------



# Function to retrieve and process buffers during image acquisition
def application_template(ia):
    # Initialize variables
    acquisition_time = 5
    start_time = time.time()
    previous_time = time.time()
    number = 0
    
    # Start image acquisition
    ia.start()

    while (time.time() - start_time) < acquisition_time:
        current_time = time.time()
        duration = current_time - previous_time
        
        # Fetch camera buffer data and process it
        raw_data = fetch_and_queue_buffer(ia)
        
        # Dummy function to work on point cloud data
        process_point_cloud(raw_data)
        
        number += 1
        print(f"Iteration: {number}, Time Interval: {duration}")
        previous_time = current_time
        
    # Stop image acquisition
    ia.stop()

