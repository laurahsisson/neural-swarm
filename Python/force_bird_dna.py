__all_factors = [
    "cohes_mass_exp", "cohes_dist_exp", "cohes_const", "cohes_cutoff",
    "repuls_mass_exp", "repuls_dist_exp", "repuls_force_exp", "repuls_const", "repuls_cutoff",
    "reward_mass_exp", "reward_dist_exp", "reward_const", "reward_cutoff",
    "obstcl_mass_exp", "obstcl_dist_exp", "obstcl_const", "obstcl_cutoff", 
    "align_mass_exp", "align_dist_exp", "align_speed_exp", "align_const", "align_cutoff"
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