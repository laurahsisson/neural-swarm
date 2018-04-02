import numpy as np
from math import pow
from timeit import default_timer as timer


def norm(b):
    return pow(b[0]*b[0]+b[1]*b[1],.5)

start = timer()
for i in range(10000):
    a = np.array([134,-12])
    np.linalg.norm(a)
print(timer()-start)

start = timer()
for i in range(10000):
    a = [134,-12]
    norm(a)
print(timer()-start)
