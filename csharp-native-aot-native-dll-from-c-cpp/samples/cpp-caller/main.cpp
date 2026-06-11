// main.cpp
#include <cstdint>
#include <cstdlib>
#include <iostream>
#include <windows.h>

#include "native_api.h"

template <typename T>
T LoadSymbol(HMODULE module, const char* name)
{
    FARPROC proc = ::GetProcAddress(module, name);
    if (proc == nullptr)
    {
        std::cerr << "GetProcAddress failed: " << name << '\n';
        std::exit(EXIT_FAILURE);
    }

    return reinterpret_cast<T>(proc);
}

int main()
{
    HMODULE module = ::LoadLibraryW(L"NativeAotSample.dll");
    if (module == nullptr)
    {
        std::cerr << "LoadLibraryW failed" << '\n';
        return EXIT_FAILURE;
    }

    auto create = LoadSymbol<km_accumulator_create_fn>(module, "km_accumulator_create");
    auto add = LoadSymbol<km_accumulator_add_fn>(module, "km_accumulator_add");
    auto getTotal = LoadSymbol<km_accumulator_get_total_fn>(module, "km_accumulator_get_total");
    auto destroy = LoadSymbol<km_accumulator_destroy_fn>(module, "km_accumulator_destroy");

    intptr_t handle = 0;
    if (create(&handle) != KM_STATUS_OK)
    {
        std::cerr << "create failed" << '\n';
        return EXIT_FAILURE;
    }

    if (add(handle, 10) != KM_STATUS_OK)
    {
        std::cerr << "add(10) failed" << '\n';
        return EXIT_FAILURE;
    }

    if (add(handle, 20) != KM_STATUS_OK)
    {
        std::cerr << "add(20) failed" << '\n';
        return EXIT_FAILURE;
    }

    std::int64_t total = 0;
    if (getTotal(handle, &total) != KM_STATUS_OK)
    {
        std::cerr << "get_total failed" << '\n';
        return EXIT_FAILURE;
    }

    std::cout << "total = " << total << '\n';

    if (destroy(handle) != KM_STATUS_OK)
    {
        std::cerr << "destroy failed" << '\n';
        return EXIT_FAILURE;
    }

    handle = 0;

    // Native AOT の共有ライブラリはアンロード前提では使わない。
    // FreeLibrary(module);

    return EXIT_SUCCESS;
}
