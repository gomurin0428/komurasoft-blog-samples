#include <stdint.h>
#include <string.h>

void c_binarize(const uint8_t *src, uint8_t *dst,
                int width, int height, uint8_t threshold)
{
    for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
            dst[y * width + x] = src[y * width + x] >= threshold ? 255 : 0;
}

void c_smooth_3x3(const uint8_t *src, uint8_t *dst, int width, int height)
{
    memcpy(dst, src, (size_t)width * (size_t)height);
    for (int y = 1; y < height - 1; y++)
        for (int x = 1; x < width - 1; x++) {
            const uint8_t *p = src + y * width + x;
            int sum = p[-width - 1] + p[-width] + p[-width + 1]
                    + p[-1] + p[0] + p[1]
                    + p[width - 1] + p[width] + p[width + 1];
            dst[y * width + x] = (uint8_t)(sum / 9);
        }
}

static int32_t find(const int32_t *parent, int32_t l)
{
    while (parent[l] != l)
        l = parent[l];
    return l;
}

/* parent は max_labels + 1 個の要素を持つ。戻り値は成分の数、ラベルが足りなければ -1 */
int c_label_components(const uint8_t *src, int32_t *labels, int32_t *parent,
                       int width, int height, int32_t max_labels)
{
    int32_t next = 1;
    for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++) {
            int i = y * width + x;
            if (src[i] == 0) {
                labels[i] = 0;
                continue;
            }
            int32_t up = y > 0 ? labels[i - width] : 0;
            int32_t left = x > 0 ? labels[i - 1] : 0;
            if (up == 0 && left == 0) {
                if (next > max_labels)
                    return -1;
                parent[next] = next;
                labels[i] = next++;
            } else if (left == 0) {
                labels[i] = up;
            } else if (up == 0) {
                labels[i] = left;
            } else {
                int32_t ru = find(parent, up), rl = find(parent, left);
                if (ru < rl)
                    parent[rl] = ru;
                else if (rl < ru)
                    parent[ru] = rl;
                labels[i] = ru < rl ? ru : rl;
            }
        }
    for (int i = 0; i < width * height; i++)
        if (labels[i] != 0)
            labels[i] = find(parent, labels[i]);
    int count = 0;
    for (int32_t l = 1; l < next; l++)
        if (parent[l] == l)
            count++;
    return count;
}
