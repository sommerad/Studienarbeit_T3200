

"""

File name:  AM-T100_basics.py
Version:    1.0
Author:     Schmersal

"""

from harvesters.core import Harvester
import time


# Initialize Harvester object
h = Harvester()

# Add .cti-file
h.add_file('C:/Users/KOCH/Desktop/Schmersal_TOF/Python/AM_T100_Python_Apps_003/dmvc-producer.cti')

# Update device list
h.update()

# List devices found in network
h.device_info_list # (optional)

# Create image acquisition object to connect to first camera on device list
ia = h.create()


# ---------- START YOUR APPLICATION HERE ----------


time.sleep(5)


# ---------- END YOUR APPLICATION HERE ----------


# Destroy image acquisition object and disconnect camera
ia.destroy()

# Reset the Harvester object
h.reset()