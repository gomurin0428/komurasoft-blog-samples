/* 読めないアドレスと偽りの長さを渡したとき、境界の関数が何を返すかを見る */
#include <stddef.h>
#include <stdint.h>
#include <stdio.h>

typedef struct { int threshold; int max_defect_pixels; } inspect_params;
typedef struct {
    int64_t defect_pixels;
    int min_x, min_y, max_x, max_y;
    int verdict;
} inspect_result;

int inspect_dark_defects(const uint8_t *pixels, size_t length,
                         int width, int height, int stride,
                         const inspect_params *params, inspect_result *result);

int main(void)
{
    inspect_params p = { 64, 50 };
    inspect_result r;
    /* 0x1000 は読めないアドレス。長さは寸法に合わせて偽る */
    int status = inspect_dark_defects((const uint8_t *)0x1000, (size_t)640 * 480,
                                      640, 480, 640, &p, &r);
    printf("bad pointer status=%d\n", status);
    return 0;
}
