from harvesters.core import Harvester
from AM_T100_functions import prepare_image_acquisition, define_buffer_data, fetch_buffer_data, visualize_point_cloud, save_pcd

def main(ctiPath, dataPath,rangeSetting):
    # Initialize Harvester object
    h = Harvester()

    # Add .cti-file
    h.add_file(ctiPath)

    # Update device list
    h.update()

    # List devices found in network (optional)
    print(h.device_info_list)

    # Create image acquisition object to connect to the first camera on the device list
    ia = h.create()
    ia.remote_device.node_map.RangeMode=rangeSetting

    try:
        # ---------- START YOUR APPLICATION HERE ----------
        # Prepare camera for image acquisition
        prepare_image_acquisition(ia)

        # Define buffer data
        define_buffer_data(ia)

        # Fetch buffer data from camera
        raw_data = fetch_buffer_data(ia)
        save_pcd(raw_data, dataPath)
        #visualize_point_cloud(raw_data)
        return raw_data

        

        # ---------- END YOUR APPLICATION HERE ----------
    finally:
        # Clean up resources
        ia.destroy()
        h.reset()