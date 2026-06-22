from harvesters.core import Harvester
from AM_T100_functions import read_camera_parameters


# Initialize Harvester object
h = Harvester()

# Add .cti-file
h.add_file('C:/Program Files/Schmersal/CONSAM-T/desktop/dmvc-producer.cti')

# Update device list
h.update()

# List devices found in network
h.device_info_list # (optional)

# Create image acquisition object to connect to first camera on device list
ia = h.create()


# ---------- START YOUR APPLICATION HERE ----------


# Call the function to read parameters, type, symbolics, and current value, and save to CSV
read_camera_parameters(ia)


# ---------- END YOUR APPLICATION HERE ----------

# Destroy image acquisition object and disconnect camera
ia.destroy()

# Reset the Harvester object
h.reset()
