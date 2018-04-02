
__all_factors = [
    "attract_mass_exp", "attract_dist_exp", "attract_const", "attract_cutoff",
    "repulse_mass_exp", "repulse_dist_exp", "repulse_const", "repulse_cutoff",
    "wall_mass_exp", "wall_dist_exp", "wall_const", "wall_cutoff",
    "obstacle_mass_exp", "obstacle_dist_exp", "obstacle_const",
    "obstacle_cutoff", "align_mass_exp", "align_dist_exp", "align_speed_exp",
    "align_const", "align_cutoff"
]


def factor_dict_to_list(factor_dict):
    global __all_factors

    factor_list = [0] * len(__all_factors)
    for factor, val in factor_dict.items():
        i = __all_factors.index(factor)
        factor_list[i] = factor_dict[factor]
    return factor_list


def factor_list_to_dict(factor_list):
    global __all_factors

    factor_dict = dict()
    for i, factor in enumerate(factor_list):
        factor_dict[__all_factors[i]] = factor
    return factor_dict


def random_factor_list():
    global __all_factors

    factor_list = [0] * len(__all_factors)
    for i in range(len(factor_list)):
        factor_list[i] = np.random.random_sample()
    return factor_list