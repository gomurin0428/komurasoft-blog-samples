// main_linux.cpp
//
// Linux で .so を dlopen / dlsym で呼ぶ版です。
// 記事本文の main.cpp（Windows / LoadLibrary 版）と同じ流れを、
// POSIX の動的ロード API に置き換えています。
//
// ビルド例:
//   g++ -O2 -o caller main_linux.cpp -ldl
// 実行例（.so は publish 出力からカレントへコピーしておく）:
//   ./caller ./NativeAotSample.so
#include <cstdint>
#include <cstdlib>
#include <iostream>
#include <dlfcn.h>

// __cdecl は MSVC 拡張なので、Linux では空定義にして native_api.h を共用する。
#ifndef _WIN32
#define __cdecl
#endif

#include "native_api.h"

template <typename T>
T LoadSymbol(void* module, const char* name)
{
    void* proc = ::dlsym(module, name);
    if (proc == nullptr)
    {
        std::cerr << "dlsym failed: " << name << '\n';
        std::exit(EXIT_FAILURE);
    }

    return reinterpret_cast<T>(proc);
}

int main(int argc, char** argv)
{
    const char* libraryPath = argc > 1 ? argv[1] : "./NativeAotSample.so";

    void* module = ::dlopen(libraryPath, RTLD_NOW);
    if (module == nullptr)
    {
        std::cerr << "dlopen failed: " << ::dlerror() << '\n';
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
    // dlclose(module);

    return EXIT_SUCCESS;
}
