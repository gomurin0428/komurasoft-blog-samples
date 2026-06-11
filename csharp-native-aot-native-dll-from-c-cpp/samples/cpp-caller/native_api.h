/* native_api.h */
#pragma once
#include <stdint.h>

enum km_status
{
    KM_STATUS_OK = 0,
    KM_STATUS_INVALID_ARGUMENT = -1,
    KM_STATUS_INVALID_HANDLE = -2,
    KM_STATUS_UNEXPECTED_ERROR = -3
};

typedef int (__cdecl *km_accumulator_create_fn)(intptr_t* out_handle);
typedef int (__cdecl *km_accumulator_add_fn)(intptr_t handle, int value);
typedef int (__cdecl *km_accumulator_get_total_fn)(intptr_t handle, int64_t* out_total);
typedef int (__cdecl *km_accumulator_destroy_fn)(intptr_t handle);
