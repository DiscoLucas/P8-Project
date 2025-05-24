import pyxdf
import pandas as pd
import numpy as np

def extract_all_streams_to_dataframes(filepath):
    """
    Loads an XDF file and extracts all streams into a dictionary of Pandas DataFrames.

    Args:
        filepath (str): The path to the .xdf file.

    Returns:
        dict: A dictionary where keys are stream names (suffixed if duplicates exist)
              and values are Pandas DataFrames. Each DataFrame contains the
              time series data for that stream, with timestamps as the index
              and channel labels as columns.
              Returns an empty dictionary if the file cannot be loaded,
              no streams are found, or no streams can be successfully processed.
    """
    all_streams_data = {}
    try:
        streams, header = pyxdf.load_xdf(filepath)
    except Exception as e:
        print(f"Error loading XDF file {filepath}: {e}")
        return all_streams_data

    if not streams:
        print(f"No streams found in {filepath}.")
        return all_streams_data

    for stream_idx, stream in enumerate(streams):
        # 1. Get Stream Name
        stream_name_list = stream.get('info', {}).get('name')
        if stream_name_list and isinstance(stream_name_list, list) and len(stream_name_list) > 0:
            base_stream_name = str(stream_name_list[0])
        else:
            base_stream_name = f'UnnamedStream_{stream_idx + 1}'

        # 2. Get Time Series and Timestamps
        time_series = stream.get('time_series')
        time_stamps = stream.get('time_stamps')

        if time_series is None or time_stamps is None:
            print(f"Warning: Stream '{base_stream_name}' is missing time_series or time_stamps. Skipping.")
            continue
        
        if not isinstance(time_series, np.ndarray):
            time_series = np.array(time_series)
        if not isinstance(time_stamps, np.ndarray):
            time_stamps = np.array(time_stamps)

        if time_stamps.size == 0:
            if base_stream_name == 'DummyStream' or "UnityMarkers":
                continue
            
            print(f"Warning: Stream '{base_stream_name}' has empty time_stamps. Skipping.")
            continue
        if time_series.size == 0: # Empty time_series can occur (e.g. marker stream with no markers)
             # Create an empty DataFrame for such streams if timestamps are present
            if time_stamps.size > 0:
                print(f"Warning: Stream '{base_stream_name}' has empty time_series but valid timestamps. Creating empty DataFrame.")
                # We'll proceed to create an empty DF with an index later
            else: # Both empty
                print(f"Warning: Stream '{base_stream_name}' has empty time_series and time_stamps. Skipping.")
                continue
            
        # Ensure time_series is 2D (N_samples x N_channels)
        # If time_series became empty (e.g. marker stream with no markers), shape will be (0,) or similar
        if time_series.ndim == 1 and time_series.size > 0 : # Only reshape if it's 1D and has data
            time_series = time_series.reshape(-1, 1)
        elif time_series.size == 0 and time_stamps.size > 0: # Handle empty data, non-empty timestamps
             # Define num_channels_data as 0, num_samples_data from timestamps
             num_samples_data = len(time_stamps)
             num_channels_data = 0
             time_series = np.empty((num_samples_data, 0)) # Create a 2D array with 0 columns
        elif time_series.ndim == 0 and time_series.size == 1: # scalar case
            time_series = time_series.reshape(1,1)


        if time_series.ndim != 2: # If it's still not 2D (e.g. completely empty, shape (0,))
            if time_series.size == 0 and num_channels_data == 0: # Expected for empty stream
                pass
            else:
                print(f"Warning: Stream '{base_stream_name}' time_series could not be shaped to 2D. Shape is {time_series.shape}. Skipping.")
                continue

        num_samples_data, num_channels_data = time_series.shape

        if len(time_stamps) != num_samples_data:
            # This check might be problematic if num_samples_data is 0 for an empty stream
            if num_samples_data == 0 and num_channels_data == 0 and len(time_stamps) > 0:
                 # This is the case for an empty stream with timestamps, allow it
                 pass
            else:
                print(f"Warning: Mismatch between number of timestamps ({len(time_stamps)}) and samples ({num_samples_data}) in stream '{base_stream_name}'. Skipping.")
                continue

        # 3. Get Channel Labels
        channel_labels = []
        labels_from_desc_found_fully = False
        if num_channels_data > 0: # Only try to get labels if there are channels
            try:
                desc = stream.get('info', {}).get('desc')
                if desc and isinstance(desc, list) and len(desc) > 0:
                    channels_outer = desc[0].get('channels')
                    if channels_outer and isinstance(channels_outer, list) and len(channels_outer) > 0:
                        channel_info_list = channels_outer[0].get('channel')
                        if channel_info_list and isinstance(channel_info_list, list):
                            temp_labels = []
                            for i in range(len(channel_info_list)): # Iterate through available channel descriptions
                                if isinstance(channel_info_list[i], dict):
                                    label_list = channel_info_list[i].get('label')
                                    if label_list and isinstance(label_list, list) and len(label_list) > 0:
                                        temp_labels.append(str(label_list[0]))
                                    else:
                                        temp_labels.append(f"Ch{i+1}_desc") 
                                else:
                                    temp_labels.append(f"Ch{i+1}_desc")
                            
                            # If num_channels_data matches temp_labels, use them
                            if len(temp_labels) == num_channels_data:
                                channel_labels = temp_labels
                                labels_from_desc_found_fully = True
                            # If more data channels than labels, use parsed ones and fill rest
                            elif len(temp_labels) < num_channels_data and len(temp_labels) > 0 :
                                channel_labels = temp_labels
                                print(f"Warning: Stream '{base_stream_name}' has {num_channels_data} data channels but only {len(temp_labels)} labels in description. Filling remaining.")
                                for i in range(len(temp_labels), num_channels_data):
                                    channel_labels.append(f"Channel_{i+1}")
                                labels_from_desc_found_fully = True # Partially found and completed
                            # If fewer data channels than labels, truncate labels (less common)
                            elif len(temp_labels) > num_channels_data:
                                channel_labels = temp_labels[:num_channels_data]
                                print(f"Warning: Stream '{base_stream_name}' has {num_channels_data} data channels but {len(temp_labels)} labels in description. Truncating labels.")
                                labels_from_desc_found_fully = True


            except Exception as e_label:
                print(f"Info: Error parsing channel labels for stream '{base_stream_name}': {e_label}. Will use generic labels.")
                pass # Fall through to generic label generation

        if not labels_from_desc_found_fully:
            if num_channels_data > 0 : # Only generate if there are channels
                 channel_labels = [f"Channel_{j+1}" for j in range(num_channels_data)]
            else: # No channels, no labels
                 channel_labels = []


        # 4. Create DataFrame
        try:
            df_index = pd.Series(time_stamps, name='Timestamp')
            # Ensure columns are unique if generated labels somehow clash (highly unlikely with Ch_N)
            if len(channel_labels) != len(set(channel_labels)) and num_channels_data > 0 :
                print(f"Warning: Duplicate channel labels generated for '{base_stream_name}'. Appending indices.")
                channel_labels = [f"{label}_{i}" for i, label in enumerate(channel_labels)]

            df_stream = pd.DataFrame(data=time_series, index=df_index, columns=channel_labels)
            
            current_stream_name_to_store = base_stream_name
            suffix_counter = 1
            while current_stream_name_to_store in all_streams_data:
                current_stream_name_to_store = f"{base_stream_name}_{suffix_counter}"
                suffix_counter += 1
            
            all_streams_data[current_stream_name_to_store] = df_stream
            # print(f"Successfully processed stream: {current_stream_name_to_store} ({num_samples_data} samples, {num_channels_data} channels).")

        except Exception as e_df:
            print(f"Error creating DataFrame for stream '{base_stream_name}': {e_df}")
            
    if not all_streams_data:
        print("No streams were successfully processed into DataFrames.")
    return all_streams_data

if __name__ == '__main__':
    # This is an example of how to use the function.
    # You would replace 'your_file.xdf' with the actual path to an XDF file.
    # Create a dummy XDF file for testing if you don't have one.
    # Due to the complexity of creating a valid XDF file programmatically here,
    # this example assumes you have a test file.
    
    # Example usage:
    # test_xdf_file = '/path/to/your/test_file.xdf' 
    # streams_dict = extract_all_streams_to_dataframes(test_xdf_file)
    # if streams_dict:
    #     for stream_name, df in streams_dict.items():
    #         print(f"\nStream: {stream_name}")
    #         print(df.head())
    # else:
    #     print("No dataframes were extracted.")
    print("xdf_utils.py loaded. To use, call extract_all_streams_to_dataframes('path/to/file.xdf')")