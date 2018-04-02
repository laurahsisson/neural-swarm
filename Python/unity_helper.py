import numpy as np

def xy_dict_to_vector(xy):
    return np.asarray( [ xy["x"],xy["y"] ] )

# Given a cornerStruct as described in CornerStruct.cs, turns it into a list of 4 xy tuples
def corner_struct_to_tuples(corner_struct):
    return [xy_dict_to_vector(corner_struct["topLeft"]),xy_dict_to_vector(corner_struct["topRight"]),
        xy_dict_to_vector(corner_struct["bottomLeft"]),xy_dict_to_vector(corner_struct["bottomRight"])]