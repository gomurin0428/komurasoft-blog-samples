/* Linux で共有ライブラリを確かめる C のテストドライバー */
#include <stdint.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

typedef struct { int threshold; int max_defect_pixels; } inspect_params;
typedef struct {
    int64_t defect_pixels;
    int min_x, min_y, max_x, max_y;
    int verdict;
} inspect_result;

int inspect_dark_defects(const uint8_t *pixels, size_t length,
                         int width, int height, int stride,
                         const inspect_params *params, inspect_result *result);
int inspect_result_size(void);
int inspect_elaborated(void);

static int failures = 0;

static uint8_t *make_frame(int width, int height, int stride, uint8_t bg, uint8_t pad)
{
    uint8_t *buf = malloc((size_t)stride * height);
    for (int y = 0; y < height; y++) {
        memset(buf + (size_t)y * stride, bg, width);
        memset(buf + (size_t)y * stride + width, pad, stride - width);
    }
    return buf;
}

static void draw_square(uint8_t *buf, int stride, int x0, int y0, int size, uint8_t v)
{
    for (int y = y0; y < y0 + size; y++)
        memset(buf + (size_t)y * stride + x0, v, size);
}

static void check(const char *name, int status, const inspect_result *r,
                  int exp_status, long long exp_defects, int exp_verdict)
{
    printf("%-18s status=%d defects=%lld bbox=(%d,%d)-(%d,%d) verdict=%d\n",
           name, status, (long long)r->defect_pixels,
           r->min_x, r->min_y, r->max_x, r->max_y, r->verdict);
    if (status != exp_status
        || (exp_defects >= 0 && r->defect_pixels != exp_defects)
        || (exp_verdict >= 0 && r->verdict != exp_verdict)) {
        printf("  UNEXPECTED\n");
        failures++;
    }
}

int main(void)
{
    const int w = 640, h = 480;
    inspect_result r;
    inspect_params p = { 64, 50 };

    printf("elaborated=%d result size: C=%zu Ada=%d\n",
           inspect_elaborated(), sizeof(inspect_result), inspect_result_size());
    if (inspect_elaborated() != 1 || (int)sizeof(inspect_result) != inspect_result_size())
        failures++;

    uint8_t *f = make_frame(w, h, w, 200, 200);
    draw_square(f, w, 100, 50, 10, 20);
    check("ng (max 50)", inspect_dark_defects(f, (size_t)w * h, w, h, w, &p, &r), &r, 0, 100, 1);
    p.max_defect_pixels = 200;
    check("ok (max 200)", inspect_dark_defects(f, (size_t)w * h, w, h, w, &p, &r), &r, 0, 100, 0);
    free(f);

    /* stride が幅より大きく、詰め物の部分が 0 (暗い) */
    f = make_frame(w, h, 644, 200, 0);
    draw_square(f, 644, 100, 50, 10, 20);
    check("padded stride", inspect_dark_defects(f, (size_t)644 * h, w, h, 644, &p, &r), &r, 0, 100, 0);
    check("buffer too small", inspect_dark_defects(f, (size_t)644 * h - 1, w, h, 644, &p, &r), &r, 3, -1, -1);
    check("width > stride", inspect_dark_defects(f, (size_t)644 * h, 700, h, 644, &p, &r), &r, 2, -1, -1);
    p.threshold = 300;
    check("threshold 300", inspect_dark_defects(f, (size_t)644 * h, w, h, 644, &p, &r), &r, 4, -1, -1);
    p.threshold = 64;
    p.max_defect_pixels = -1;
    check("max -1", inspect_dark_defects(f, (size_t)644 * h, w, h, 644, &p, &r), &r, 4, -1, -1);
    check("null pixels", inspect_dark_defects(NULL, 0, w, h, 644, &p, &r), &r, 1, -1, -1);
    free(f);

    if (failures == 0)
        printf("ALL PASSED\n");
    else
        printf("FAILURES: %d\n", failures);
    return failures == 0 ? 0 : 1;
}
